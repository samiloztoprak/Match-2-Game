using System;
using System.Collections.Generic;
using Match2.Data;
using Match2.Model;
using Match2.View;
using UnityEngine;

namespace Match2.Controller
{
    /// <summary>
    /// Orchestrates one full turn: model rules -> view animation -> game
    /// state update. The only class that talks to both Model and View.
    /// </summary>
    public class GameFlowController
    {
        private const int ScorePerBlock = 10;
        private const int RocketGroupThreshold = 6;
        private const int BombGroupThreshold = 8;
        private const int BallGroupThreshold = 10;
        private const int BombAreaRadius = 1; // 1 = 3x3
        private const int ComboBombAreaRadius = 2; // Bomb+Bomb combo = 5x5
        private const int ComboThickCrossRadius = 1; // Bomb+Rocket combo = 3 rows + 3 columns

        /// <summary>What a cluster of touching power-ups does when tapped together (same effect size no matter how many pieces are in the cluster, except <see cref="ComboEffect.ClearBoard"/> and the transform combos, which scale with the board by design).</summary>
        private enum ComboEffect
        {
            None,
            WideArea,
            Cross,
            ThickCross,
            TransformToBomb,
            TransformToRocket,
            ClearBoard
        }

        private readonly GridModel gridModel;
        private readonly GameStateModel gameState;
        private readonly GridView gridView;
        private readonly LevelData levelData;

        /// <summary>
        /// True from the moment a tap starts a turn until its animations
        /// (blast, merge, gravity, refill — everything up to the final
        /// preview refresh) fully finish. Without this guard, a tap during
        /// that window would start a second turn concurrently: a pooled
        /// block reused mid-tween kills its in-flight animation without
        /// firing that animation's completion callback, which permanently
        /// stalls the first turn's <see cref="Utils.CallbackBarrier"/> chain
        /// — the board looks frozen forever, since nothing after that point
        /// (gravity, refill, score, preview refresh) ever runs.
        /// </summary>
        private bool isTurnInProgress;

        public GameFlowController(GridModel gridModel, GameStateModel gameState, GridView gridView, LevelData levelData)
        {
            this.gridModel = gridModel;
            this.gameState = gameState;
            this.gridView = gridView;
            this.levelData = levelData;
        }

        public void TryBlastAt(int index)
        {
            if (gameState.IsLevelOver || isTurnInProgress)
                return;

            IGridPiece tappedPiece = gridModel.GetPiece(index);
            if (tappedPiece != null && !tappedPiece.IsMatchable)
            {
                if (!IsPowerUp(tappedPiece))
                    return; // an obstacle (e.g. Box) - tapping it directly does nothing, only a power-up effect reaching it clears it

                isTurnInProgress = true;
                ActivatePowerUp(index);
                return;
            }

            IReadOnlyList<int> group = gridModel.FindConnectedGroup(index);
            if (!GridModel.IsBlastable(group))
                return;

            isTurnInProgress = true;

            int matchedColorId = ((ColorBlockPiece)gridModel.GetPiece(index)).ColorId;
            int scoreDelta = group.Count * ScorePerBlock;
            IGridPiece powerUp = CreatePowerUpForGroup(group.Count, matchedColorId);
            IReadOnlyList<int> clearedIndices = gridModel.Blast(group);

            if (powerUp != null)
            {
                gridModel.PlacePiece(index, powerUp);

                var otherMatchedIndices = new List<int>(clearedIndices);
                otherMatchedIndices.Remove(index);

                gridView.PlayMergeBlast(index, otherMatchedIndices, clearedIndices, () =>
                {
                    gridView.ShowPowerUpCreated(index, powerUp);
                    FinishTurnAfterBlastAnimation(scoreDelta);
                });
            }
            else
            {
                ContinueTurnAfterBlast(clearedIndices, scoreDelta);
            }
        }

        public IReadOnlyList<GridSpawn> PopulateInitialBoard()
        {
            PlaceInitialBoxes(levelData.InitialBoxCount);

            IReadOnlyList<int> allIndices = gridModel.GetEmptyIndices();
            return gridModel.Refill(allIndices, CreateRandomPiece);
        }

        /// <summary>True for a piece the player can tap directly to activate (as opposed to an obstacle like <see cref="BoxPiece"/>, which is also non-matchable but only clears when a power-up effect reaches it).</summary>
        private static bool IsPowerUp(IGridPiece piece)
        {
            return piece is RocketPiece || piece is BombPiece || piece is BallPiece;
        }

        /// <summary>Scatters <paramref name="count"/> Box obstacles across random empty cells before the rest of the board is dealt color blocks — obstacles only ever exist at level start, nothing spawns a new one mid-level.</summary>
        private void PlaceInitialBoxes(int count)
        {
            if (count <= 0)
                return;

            var availableIndices = new List<int>(gridModel.GetEmptyIndices());
            IGridPiece box = new BoxPiece();

            for (int i = 0; i < count && availableIndices.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, availableIndices.Count);
                gridModel.PlacePiece(availableIndices[pick], box);
                availableIndices.RemoveAt(pick);
            }
        }

        /// <summary>
        /// Recomputes, for every cell currently on the board, which power-up
        /// (if any) tapping it would create — the caller (the View) uses this
        /// to show a small preview icon on cells that are "ready" to turn
        /// into a power-up. Call once whenever the board settles (after the
        /// initial deal, and after every completed turn) — not per-tap.
        /// </summary>
        public void RefreshPowerUpPreviews()
        {
            gridView.ShowPowerUpPreviews(ComputePowerUpPreviews());
        }

        /// <summary>
        /// Tapping a power-up gathers every power-up touching it (orthogonally,
        /// transitively — a whole cluster, not just one neighbor), merges them
        /// visually into the tapped cell, and blasts their combined effect.
        /// If that blast happens to clear another power-up sitting outside the
        /// cluster, that one chain-activates too (its own effect gets folded
        /// in), and so on — a bomb next to a rocket's blast radius, for
        /// example, still goes off even though it was never tapped or adjacent
        /// to the tapped cell.
        /// </summary>
        private void ActivatePowerUp(int index)
        {
            List<int> cluster = FindPowerUpCluster(index);
            ComboEffect effect = cluster.Count > 1 ? DetermineClusterEffect(GetPiecesAt(cluster)) : ComboEffect.None;

            if (effect == ComboEffect.TransformToBomb || effect == ComboEffect.TransformToRocket)
            {
                ActivateColorTransformCombo(index, cluster, effect == ComboEffect.TransformToBomb ? PowerUpKind.Bomb : PowerUpKind.Rocket);
                return;
            }

            var affected = new HashSet<int>(ComputeClusterFootprint(index, cluster));
            affected.UnionWith(cluster); // every gathered power-up is consumed even if it falls outside the fixed effect radius

            var triggered = new HashSet<int>(cluster); // these already contributed their effect via the cluster footprint above
            ExpandWithChainReactions(affected, triggered);

            int scoreDelta = affected.Count * ScorePerBlock;
            IReadOnlyList<int> clearedIndices = gridModel.Blast(new List<int>(affected));

            var mergingIndices = new List<int>(cluster);
            mergingIndices.Remove(index);

            gridView.PlayMergeBlast(index, mergingIndices, clearedIndices, () => FinishTurnAfterBlastAnimation(scoreDelta));
        }

        /// <summary>
        /// A Ball meeting a Bomb or Rocket doesn't just blast a fixed area —
        /// the tapped pair merges away immediately, then every block sharing
        /// the Ball's color is swept top row to bottom row: each row's blocks
        /// visually turn into the target power-up and detonate together
        /// (with their own area/line effect, chaining into anything else they
        /// catch) before the next row starts, rather than the whole board
        /// going off at once.
        /// </summary>
        private void ActivateColorTransformCombo(int tappedIndex, List<int> cluster, PowerUpKind transformKind)
        {
            // Read the Ball's target color before the cluster is blasted away — once blasted, the Ball is gone and there's nothing left to read it from.
            int targetColorId = FindBallTargetColorId(cluster);
            List<List<int>> rowsTopToBottom = GroupByRowTopToBottom(gridModel.GetAllWithMatchKey(targetColorId));

            var clusterAffected = new HashSet<int>(cluster);
            ExpandWithChainReactions(clusterAffected, new HashSet<int>(cluster));

            int clusterScore = clusterAffected.Count * ScorePerBlock;
            IReadOnlyList<int> clusterCleared = gridModel.Blast(new List<int>(clusterAffected));

            var mergingIndices = new List<int>(cluster);
            mergingIndices.Remove(tappedIndex);

            gridView.PlayMergeBlast(tappedIndex, mergingIndices, clusterCleared,
                () => PlayTransformRowsSequentially(rowsTopToBottom, transformKind, rowIndex: 0, accumulatedScore: clusterScore));
        }

        /// <summary>Groups cell indices into rows ordered top (highest y) to bottom (lowest y); a row's own cells keep their natural left-to-right order.</summary>
        private List<List<int>> GroupByRowTopToBottom(IReadOnlyList<int> cellIndices)
        {
            var rowsByDescendingY = new SortedDictionary<int, List<int>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            foreach (int index in cellIndices)
            {
                GridUtility.ToCoords(index, gridModel.Width, out _, out int y);
                if (!rowsByDescendingY.TryGetValue(y, out List<int> row))
                {
                    row = new List<int>();
                    rowsByDescendingY[y] = row;
                }

                row.Add(index);
            }

            return new List<List<int>>(rowsByDescendingY.Values);
        }

        /// <summary>Recursively plays one row of a color-transform combo, then moves to the next, finishing the turn once every row is done.</summary>
        private void PlayTransformRowsSequentially(List<List<int>> rows, PowerUpKind transformKind, int rowIndex, int accumulatedScore)
        {
            if (rowIndex >= rows.Count)
            {
                FinishTurnAfterBlastAnimation(accumulatedScore);
                return;
            }

            List<int> row = rows[rowIndex];
            gridView.PlayColorToPowerUpTransform(row, transformKind, () =>
            {
                var rowAffected = new HashSet<int>();
                foreach (int cellIndex in row)
                    rowAffected.UnionWith(ComputeEffectCellsForKind(cellIndex, transformKind));

                ExpandWithChainReactions(rowAffected, new HashSet<int>());

                int rowScore = rowAffected.Count * ScorePerBlock;
                IReadOnlyList<int> rowCleared = gridModel.Blast(new List<int>(rowAffected));

                gridView.PlayBlast(rowCleared, () =>
                    PlayTransformRowsSequentially(rows, transformKind, rowIndex + 1, accumulatedScore + rowScore));
            });
        }

        /// <summary>Every power-up reachable from <paramref name="startIndex"/> by orthogonal power-up neighbors, transitively (a whole touching cluster, not just one hop).</summary>
        private List<int> FindPowerUpCluster(int startIndex)
        {
            var visited = new HashSet<int> { startIndex };
            var queue = new Queue<int>();
            queue.Enqueue(startIndex);
            var cluster = new List<int> { startIndex };

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int neighbor in GridUtility.GetNeighbors(current, gridModel.Width, gridModel.Height))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    IGridPiece piece = gridModel.GetPiece(neighbor);
                    if (piece == null || piece.IsMatchable)
                        continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cluster.Add(neighbor);
                }
            }

            return cluster;
        }

        /// <summary>
        /// The effect footprint for a tapped cluster: a lone power-up just
        /// uses its own effect; a cluster of one recognized combo kind (any
        /// count — three bombs together still make one 5x5, not a bigger one,
        /// per design) uses that combo's effect centered on the tapped cell;
        /// a cluster with no defined combo rule falls back to every member
        /// firing its own effect, unioned together.
        /// </summary>
        private IReadOnlyList<int> ComputeClusterFootprint(int centerIndex, IReadOnlyList<int> cluster)
        {
            if (cluster.Count == 1)
                return ComputeEffectCells(centerIndex, gridModel.GetPiece(centerIndex));

            switch (DetermineClusterEffect(GetPiecesAt(cluster)))
            {
                case ComboEffect.WideArea:
                    return gridModel.GetArea(centerIndex, ComboBombAreaRadius);
                case ComboEffect.Cross:
                    return gridModel.GetCross(centerIndex, radius: 0);
                case ComboEffect.ThickCross:
                    return gridModel.GetCross(centerIndex, ComboThickCrossRadius);
                case ComboEffect.ClearBoard:
                    return gridModel.GetAllMatchableIndices();
                default:
                    var union = new HashSet<int>();
                    foreach (int clusterIndex in cluster)
                        union.UnionWith(ComputeEffectCells(clusterIndex, gridModel.GetPiece(clusterIndex)));
                    return new List<int>(union);
            }
        }

        /// <summary>The <see cref="IGridPiece"/> at each of the given indices, in order — a small helper so callers that need "what's in this cluster" don't each repeat the same loop.</summary>
        private List<IGridPiece> GetPiecesAt(IReadOnlyList<int> indices)
        {
            var pieces = new List<IGridPiece>(indices.Count);
            foreach (int index in indices)
                pieces.Add(gridModel.GetPiece(index));

            return pieces;
        }

        /// <summary>
        /// Which combo effect a cluster's mix of power-up kinds maps to, if
        /// any. A Ball combined with another kind turns every block of the
        /// Ball's own color into that kind (Bomb takes priority over Rocket
        /// when a cluster somehow has all three); two or more Balls with no
        /// other kind present clears every ordinary block on the board;
        /// Bomb+Rocket with no Ball is a thick cross (3 rows + 3 columns),
        /// checked before the plain-Bomb/plain-Rocket cases.
        /// </summary>
        private static ComboEffect DetermineClusterEffect(IReadOnlyList<IGridPiece> pieces)
        {
            bool hasRocket = false, hasBomb = false, hasBall = false;
            foreach (IGridPiece piece in pieces)
            {
                hasRocket |= piece is RocketPiece;
                hasBomb |= piece is BombPiece;
                hasBall |= piece is BallPiece;
            }

            if (hasBall)
            {
                if (hasBomb)
                    return ComboEffect.TransformToBomb;
                if (hasRocket)
                    return ComboEffect.TransformToRocket;
                return ComboEffect.ClearBoard;
            }

            if (hasBomb && hasRocket)
                return ComboEffect.ThickCross;
            if (hasBomb)
                return ComboEffect.WideArea;
            if (hasRocket)
                return ComboEffect.Cross;
            return ComboEffect.None;
        }

        /// <summary>What a block at <paramref name="index"/> would clear if it were the given power-up kind — used by the color-transform combo, where an ordinary color block is treated as a Bomb or Rocket without ever actually becoming one in the model.</summary>
        private IReadOnlyList<int> ComputeEffectCellsForKind(int index, PowerUpKind kind)
        {
            return kind == PowerUpKind.Bomb
                ? gridModel.GetArea(index, BombAreaRadius)
                : gridModel.GetLine(index, RandomOrientation());
        }

        private int FindBallTargetColorId(IReadOnlyList<int> cluster)
        {
            foreach (int index in cluster)
            {
                if (gridModel.GetPiece(index) is BallPiece ball)
                    return ball.TargetColorId;
            }

            return -1; // never reached: callers only look this up when DetermineClusterEffect already found a Ball in the cluster
        }

        /// <summary>A single power-up's own blast footprint, regardless of whether it was tapped directly, merged into a combo, or chain-triggered.</summary>
        private IReadOnlyList<int> ComputeEffectCells(int index, IGridPiece piece)
        {
            switch (piece)
            {
                case RocketPiece rocket:
                    return gridModel.GetLine(index, rocket.Orientation);
                case BombPiece:
                    return gridModel.GetArea(index, BombAreaRadius);
                case BallPiece ball:
                    return new List<int>(gridModel.GetAllWithMatchKey(ball.TargetColorId)) { index };
                default:
                    return Array.Empty<int>();
            }
        }

        /// <summary>
        /// Grows <paramref name="affected"/> in place: any power-up caught
        /// inside it that hasn't already contributed its effect (tracked via
        /// <paramref name="triggered"/>) chain-activates — its own effect
        /// cells are folded in too, which may in turn catch further
        /// power-ups, and so on until nothing new is found.
        /// </summary>
        private void ExpandWithChainReactions(HashSet<int> affected, HashSet<int> triggered)
        {
            var queue = new Queue<int>(affected);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                IGridPiece piece = gridModel.GetPiece(current);
                if (piece == null || piece.IsMatchable || !triggered.Add(current))
                    continue;

                foreach (int extra in ComputeEffectCells(current, piece))
                {
                    if (affected.Add(extra))
                        queue.Enqueue(extra);
                }
            }
        }

        /// <summary>Which power-up kind (if any) a match of this size creates. Larger thresholds win when several are met.</summary>
        private static PowerUpKind? DeterminePowerUpKind(int groupSize)
        {
            if (groupSize >= BallGroupThreshold)
                return PowerUpKind.Ball;
            if (groupSize >= BombGroupThreshold)
                return PowerUpKind.Bomb;
            if (groupSize >= RocketGroupThreshold)
                return PowerUpKind.Rocket;
            return null;
        }

        private static IGridPiece CreatePowerUpForGroup(int groupSize, int matchedColorId)
        {
            switch (DeterminePowerUpKind(groupSize))
            {
                case PowerUpKind.Ball:
                    return new BallPiece(matchedColorId);
                case PowerUpKind.Bomb:
                    return new BombPiece();
                case PowerUpKind.Rocket:
                    return new RocketPiece(RandomOrientation());
                default:
                    return null;
            }
        }

        private Dictionary<int, PowerUpKind> ComputePowerUpPreviews()
        {
            var previews = new Dictionary<int, PowerUpKind>();
            foreach (KeyValuePair<int, int> entry in gridModel.ComputeGroupSizes())
            {
                PowerUpKind? kind = DeterminePowerUpKind(entry.Value);
                if (kind.HasValue)
                    previews[entry.Key] = kind.Value;
            }

            return previews;
        }

        /// <summary>
        /// The shared back half of a turn — blast, gravity, refill, score —
        /// used by both a normal group blast and a rocket line-clear.
        /// If a power-up was just created (<paramref name="powerUpIndex"/> set), it
        /// pops into place only after the blast animation finishes, so it never
        /// overlaps the pieces disappearing at the same spot.
        /// </summary>
        private void ContinueTurnAfterBlast(IReadOnlyList<int> clearedIndices, int scoreDelta, int? powerUpIndex = null, IGridPiece powerUpPiece = null)
        {
            gridView.PlayBlast(clearedIndices, () => FinishTurnAfterBlastAnimation(scoreDelta, powerUpIndex, powerUpPiece));
        }

        /// <summary>Gravity, refill, score, and preview refresh — the tail every kind of turn (solo activation, combo, or normal match) shares once its blast animation has finished.</summary>
        private void FinishTurnAfterBlastAnimation(int scoreDelta, int? powerUpIndex = null, IGridPiece powerUpPiece = null)
        {
            if (powerUpIndex.HasValue)
                gridView.ShowPowerUpCreated(powerUpIndex.Value, powerUpPiece);

            IReadOnlyList<GridMove> moves = gridModel.ApplyGravity();
            gridView.PlayMoves(moves, () =>
            {
                IReadOnlyList<int> emptyIndices = gridModel.GetEmptyIndices();
                IReadOnlyList<GridSpawn> spawns = gridModel.Refill(emptyIndices, CreateRandomPiece);
                gridView.PlaySpawns(spawns, () =>
                {
                    gameState.RegisterMove(scoreDelta);
                    RefreshPowerUpPreviews();
                    isTurnInProgress = false;
                });
            });
        }

        private static RocketOrientation RandomOrientation()
        {
            return UnityEngine.Random.value < 0.5f ? RocketOrientation.Horizontal : RocketOrientation.Vertical;
        }

        private IGridPiece CreateRandomPiece(int index)
        {
            IReadOnlyList<BlockTypeData> blockTypes = levelData.BlockTypes;
            BlockTypeData chosen = blockTypes[UnityEngine.Random.Range(0, blockTypes.Count)];
            return new ColorBlockPiece(chosen.ColorId);
        }
    }
}
