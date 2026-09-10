using System;
using System.Collections.Generic;
using Match2.Data;
using Match2.Model;
using Match2.Systems.Pooling;
using Match2.Systems.Tween;
using Match2.Utils;
using UnityEngine;

namespace Match2.View
{
    /// <summary>
    /// Mirrors <see cref="GridModel"/> visually: spawns/positions pooled
    /// <see cref="BlockView"/>s and plays the animations for a blast/gravity/
    /// refill step. Takes commands from <see cref="Controller.GameFlowController"/>
    /// — it never reads game rules itself.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        private const float VerticalRocketRotationDegrees = 90f;

        [SerializeField] private float cellSize = 1f;
        [SerializeField] private List<PowerUpTypeData> powerUpTypes = new();
        [SerializeField] private ScorePopupView scorePopupPrefab;
        [SerializeField] private RectTransform scorePopupTarget;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int scorePerBlock = 10;

        private readonly Dictionary<int, BlockView> viewsByIndex = new();
        private readonly Dictionary<int, BlockTypeData> blockTypesByColorId = new();
        private readonly Dictionary<PowerUpKind, PowerUpTypeData> powerUpTypesByKind = new();

        private LevelData levelData;
        private IBlockAnimator animator;
        private BlockPoolService pool;

        public float CellSize => cellSize;
        public float BoardWidth => levelData.GridWidth * cellSize;
        public float BoardHeight => levelData.GridHeight * cellSize;

        public void Initialize(LevelData level, BlockView blockPrefab, IBlockAnimator blockAnimator)
        {
            levelData = level;
            animator = blockAnimator;
            pool = new BlockPoolService(blockPrefab, transform);

            blockTypesByColorId.Clear();
            foreach (BlockTypeData blockType in level.BlockTypes)
                blockTypesByColorId[blockType.ColorId] = blockType;

            powerUpTypesByKind.Clear();
            foreach (PowerUpTypeData powerUpType in powerUpTypes)
                powerUpTypesByKind[powerUpType.Kind] = powerUpType;
        }

        public Vector3 WorldPositionForIndex(int index)
        {
            GridUtility.ToCoords(index, levelData.GridWidth, out int x, out int y);
            return transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0f);
        }

        public bool TryGetIndexAtWorldPosition(Vector3 worldPosition, out int index)
        {
            Vector3 local = worldPosition - transform.position;
            int x = Mathf.FloorToInt(local.x / cellSize);
            int y = Mathf.FloorToInt(local.y / cellSize);

            if (!GridUtility.IsInBounds(x, y, levelData.GridWidth, levelData.GridHeight))
            {
                index = -1;
                return false;
            }

            index = GridUtility.ToIndex(x, y, levelData.GridWidth);
            return true;
        }

        /// <summary>
        /// Places the starting board directly at its final positions — the
        /// scene should open with a filled board, not a cascade. Only blocks
        /// that appear later (refills after a blast) fall in from above.
        /// </summary>
        public void ShowInitialBoard(IReadOnlyList<GridSpawn> spawns)
        {
            foreach (GridSpawn spawn in spawns)
            {
                (Sprite sprite, float rotation, Color tint) = ResolveVisual(spawn.Piece);
                BlockView view = pool.Get();
                view.Init(spawn.Index, sprite, rotation, tint, animator, WorldPositionForIndex(spawn.Index));
                viewsByIndex[spawn.Index] = view;
            }
        }

        /// <summary>Shows a power-up (e.g. a newly created Rocket) popping into place at <paramref name="index"/>, rather than falling in.</summary>
        public void ShowPowerUpCreated(int index, IGridPiece piece)
        {
            (Sprite sprite, float rotation, Color tint) = ResolveVisual(piece);
            BlockView view = pool.Get();
            view.Init(index, sprite, rotation, tint, animator, WorldPositionForIndex(index));
            viewsByIndex[index] = view;
            view.PlayAppearAnimation();
        }

        /// <summary>
        /// Shows a small overlay icon on every cell that would create a power-up
        /// if tapped right now, and clears it from every other cell. Called
        /// whenever the board settles (see <see cref="Controller.GameFlowController.RefreshPowerUpPreviews"/>).
        /// </summary>
        public void ShowPowerUpPreviews(IReadOnlyDictionary<int, PowerUpKind> previewKindByIndex)
        {
            foreach (KeyValuePair<int, BlockView> entry in viewsByIndex)
            {
                if (previewKindByIndex.TryGetValue(entry.Key, out PowerUpKind kind)
                    && powerUpTypesByKind.TryGetValue(kind, out PowerUpTypeData previewType))
                {
                    entry.Value.SetPreviewIcon(previewType.Sprite);
                }
                else
                {
                    entry.Value.ClearPreviewIcon();
                }
            }
        }

        public void PlayBlast(IReadOnlyList<int> indices, Action onComplete)
        {
            var barrier = new CallbackBarrier(indices.Count, onComplete);
            BlastIndices(indices, barrier);
        }

        /// <summary>
        /// Every cell in <paramref name="mergingIndices"/> visually slides into
        /// <paramref name="centerIndex"/>'s position first (so they appear to merge
        /// there); only once they all arrive does everything in <paramref name="clearedIndices"/>
        /// blast together (cells in <paramref name="clearedIndices"/> that aren't
        /// merging — e.g. ordinary blocks caught in a Bomb's blast radius —
        /// just blast in place like a normal blast). Used both when a match
        /// creates a power-up (every other matched block merges into it) and
        /// when two power-ups combo (the adjacent one merges into the tapped one).
        /// </summary>
        public void PlayMergeBlast(int centerIndex, IReadOnlyList<int> mergingIndices, IReadOnlyList<int> clearedIndices, Action onComplete)
        {
            var mergingIndexSet = new HashSet<int>(mergingIndices);
            var movers = new List<BlockView>();
            foreach (int index in mergingIndexSet)
            {
                if (viewsByIndex.TryGetValue(index, out BlockView view))
                {
                    viewsByIndex.Remove(index);
                    movers.Add(view);
                }
            }

            if (movers.Count == 0)
            {
                PlayBlast(clearedIndices, onComplete);
                return;
            }

            Vector3 centerPosition = WorldPositionForIndex(centerIndex);
            var moveBarrier = new CallbackBarrier(movers.Count, () =>
            {
                var blastBarrier = new CallbackBarrier(clearedIndices.Count, onComplete);

                foreach (BlockView mover in movers)
                {
                    Vector3 moverOrigin = mover.transform.position;
                    mover.PlayBlast(() =>
                    {
                        pool.Release(mover);
                        blastBarrier.ReportComplete();
                    });
                    SpawnScorePopup(moverOrigin);
                }

                var remaining = new List<int>(clearedIndices);
                remaining.RemoveAll(index => mergingIndexSet.Contains(index));
                BlastIndices(remaining, blastBarrier);
            });

            foreach (BlockView mover in movers)
                mover.PlayMergeSlide(centerPosition, moveBarrier.ReportComplete);
        }

        /// <summary>
        /// Visually turns every block at <paramref name="indices"/> into the
        /// given power-up kind in place (sprite swap + a small identity-change
        /// pulse) without blasting or removing them — used by the Ball+Bomb/
        /// Ball+Rocket color-transform combo, which converts a whole row of
        /// same-colored blocks before that row detonates.
        /// </summary>
        public void PlayColorToPowerUpTransform(IReadOnlyList<int> indices, PowerUpKind kind, Action onComplete)
        {
            if (indices.Count == 0 || !powerUpTypesByKind.TryGetValue(kind, out PowerUpTypeData typeData))
            {
                onComplete?.Invoke();
                return;
            }

            var barrier = new CallbackBarrier(indices.Count, onComplete);
            foreach (int index in indices)
            {
                if (!viewsByIndex.TryGetValue(index, out BlockView view))
                {
                    barrier.ReportComplete();
                    continue;
                }

                view.PlayTransformInto(typeData.Sprite, 0f, Color.white, barrier.ReportComplete);
            }
        }

        private void BlastIndices(IReadOnlyList<int> indices, CallbackBarrier barrier)
        {
            foreach (int index in indices)
            {
                if (!viewsByIndex.TryGetValue(index, out BlockView view))
                {
                    barrier.ReportComplete();
                    continue;
                }

                Vector3 origin = view.transform.position;
                viewsByIndex.Remove(index);
                view.PlayBlast(() =>
                {
                    pool.Release(view);
                    barrier.ReportComplete();
                });
                SpawnScorePopup(origin);
            }
        }

        /// <summary>
        /// Pops a "+N" label at <paramref name="origin"/> that flies toward
        /// the score display, mirroring the score the model just added for
        /// this cell. Skipped silently if the popup prefab or target isn't
        /// wired up (e.g. in a test harness with no HUD).
        /// </summary>
        private void SpawnScorePopup(Vector3 origin)
        {
            if (scorePopupPrefab == null || scorePopupTarget == null)
                return;

            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
                return;

            float depth = Mathf.Abs(camera.transform.position.z - origin.z);
            Vector3 destination = camera.ScreenToWorldPoint(new Vector3(scorePopupTarget.position.x, scorePopupTarget.position.y, depth));

            ScorePopupView popup = Instantiate(scorePopupPrefab, origin, Quaternion.identity, transform);
            popup.Play(scorePerBlock, destination, animator, () => Destroy(popup.gameObject));
        }

        public void PlayMoves(IReadOnlyList<GridMove> moves, Action onComplete)
        {
            var barrier = new CallbackBarrier(moves.Count, onComplete);

            foreach (GridMove move in moves)
            {
                if (!viewsByIndex.TryGetValue(move.FromIndex, out BlockView view))
                {
                    barrier.ReportComplete();
                    continue;
                }

                viewsByIndex.Remove(move.FromIndex);
                viewsByIndex[move.ToIndex] = view;
                view.MoveTo(move.ToIndex, WorldPositionForIndex(move.ToIndex), barrier.ReportComplete);
            }
        }

        public void PlaySpawns(IReadOnlyList<GridSpawn> spawns, Action onComplete)
        {
            DropSpawns(spawns, onComplete);
        }

        /// <summary>
        /// Places each new block above the visible board — stacked in spawn
        /// order per column, so simultaneous refills in the same column queue
        /// up rather than overlapping — then lets it fall into place exactly
        /// like a gravity move. Used for post-blast refills only.
        /// </summary>
        private void DropSpawns(IReadOnlyList<GridSpawn> spawns, Action onComplete)
        {
            CallbackBarrier barrier = onComplete != null ? new CallbackBarrier(spawns.Count, onComplete) : null;
            IReadOnlyDictionary<int, int> stackIndexByGridIndex = ComputeColumnStackIndices(spawns);

            foreach (GridSpawn spawn in spawns)
            {
                (Sprite sprite, float rotation, Color tint) = ResolveVisual(spawn.Piece);
                BlockView view = pool.Get();
                Vector3 spawnPosition = AboveBoardPositionForIndex(spawn.Index, stackIndexByGridIndex[spawn.Index]);
                Vector3 targetPosition = WorldPositionForIndex(spawn.Index);

                view.Init(spawn.Index, sprite, rotation, tint, animator, spawnPosition);
                viewsByIndex[spawn.Index] = view;
                view.MoveTo(spawn.Index, targetPosition, () => barrier?.ReportComplete());
            }
        }

        /// <summary>For each spawn, how many other spawns in the same column land below it (0 = lowest).</summary>
        private Dictionary<int, int> ComputeColumnStackIndices(IReadOnlyList<GridSpawn> spawns)
        {
            var indicesByColumn = new Dictionary<int, List<int>>();
            foreach (GridSpawn spawn in spawns)
            {
                GridUtility.ToCoords(spawn.Index, levelData.GridWidth, out int x, out _);
                if (!indicesByColumn.TryGetValue(x, out List<int> columnIndices))
                {
                    columnIndices = new List<int>();
                    indicesByColumn[x] = columnIndices;
                }

                columnIndices.Add(spawn.Index);
            }

            var stackIndexByGridIndex = new Dictionary<int, int>();
            foreach (List<int> columnIndices in indicesByColumn.Values)
            {
                columnIndices.Sort(); // within a column, ascending index means ascending row (lowest first)
                for (int stackIndex = 0; stackIndex < columnIndices.Count; stackIndex++)
                    stackIndexByGridIndex[columnIndices[stackIndex]] = stackIndex;
            }

            return stackIndexByGridIndex;
        }

        private Vector3 AboveBoardPositionForIndex(int index, int stackIndexInColumn)
        {
            GridUtility.ToCoords(index, levelData.GridWidth, out int x, out _);
            float y = levelData.GridHeight + stackIndexInColumn;
            return transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0f);
        }

        /// <summary>The sprite, rotation, and tint to render for any piece type the board can contain.</summary>
        private (Sprite sprite, float rotationDegrees, Color tint) ResolveVisual(IGridPiece piece)
        {
            switch (piece)
            {
                case ColorBlockPiece colorPiece when blockTypesByColorId.TryGetValue(colorPiece.ColorId, out BlockTypeData blockType):
                    return (blockType.Sprite, 0f, Color.white);

                case RocketPiece rocket when powerUpTypesByKind.TryGetValue(PowerUpKind.Rocket, out PowerUpTypeData rocketType):
                    float rotation = rocket.Orientation == RocketOrientation.Vertical ? VerticalRocketRotationDegrees : 0f;
                    return (rocketType.Sprite, rotation, Color.white);

                case BombPiece when powerUpTypesByKind.TryGetValue(PowerUpKind.Bomb, out PowerUpTypeData bombType):
                    return (bombType.Sprite, 0f, Color.white);

                case BallPiece ball when powerUpTypesByKind.TryGetValue(PowerUpKind.Ball, out PowerUpTypeData ballType):
                    Color tint = blockTypesByColorId.TryGetValue(ball.TargetColorId, out BlockTypeData targetType) ? targetType.TintColor : Color.white;
                    return (ballType.Sprite, 0f, tint);

                default:
                    return (null, 0f, Color.white);
            }
        }
    }
}
