using System.Collections.Generic;
using Match2.Controller;
using Match2.Data;
using Match2.Model;
using Match2.Systems.Events;
using Match2.Systems.Tween;
using Match2.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Match2.Tests
{
    /// <summary>
    /// Exercises <see cref="GameFlowController"/> end to end (tap -> model
    /// mutation -> score) using a real <see cref="GridView"/> driven by the
    /// deterministic <see cref="InstantBlockAnimator"/>, so every animation
    /// resolves synchronously and outcomes can be asserted immediately.
    /// </summary>
    public class GameFlowControllerTests
    {
        private const int Width = 5;
        private const int Height = 5;
        private const int ScorePerBlock = 10;

        private GridModel gridModel;
        private GameStateModel gameState;
        private GridView gridView;
        private GameFlowController flow;

        private GameObject gridViewGO;
        private GameObject blockPrefabGO;
        private LevelData levelData;
        private BlockTypeData blockType;
        private BlockPaletteData palette;

        [SetUp]
        public void SetUp()
        {
            blockType = ScriptableObject.CreateInstance<BlockTypeData>();
            var blockTypeSo = new SerializedObject(blockType);
            blockTypeSo.FindProperty("colorId").intValue = 1;
            blockTypeSo.FindProperty("tintColor").colorValue = Color.white;
            blockTypeSo.ApplyModifiedPropertiesWithoutUndo();

            palette = ScriptableObject.CreateInstance<BlockPaletteData>();
            var paletteSo = new SerializedObject(palette);
            SerializedProperty blockTypesProp = paletteSo.FindProperty("blockTypes");
            blockTypesProp.arraySize = 2;
            blockTypesProp.GetArrayElementAtIndex(0).objectReferenceValue = blockType;
            blockTypesProp.GetArrayElementAtIndex(1).objectReferenceValue = blockType;
            paletteSo.ApplyModifiedPropertiesWithoutUndo();

            levelData = ScriptableObject.CreateInstance<LevelData>();
            var levelSo = new SerializedObject(levelData);
            levelSo.FindProperty("gridWidth").intValue = Width;
            levelSo.FindProperty("gridHeight").intValue = Height;
            levelSo.FindProperty("moveLimit").intValue = 999;
            levelSo.FindProperty("targetScore").intValue = 999999;
            levelSo.FindProperty("palette").objectReferenceValue = palette;
            levelSo.FindProperty("colorCount").intValue = 2;
            levelSo.ApplyModifiedPropertiesWithoutUndo();

            gridViewGO = new GameObject("TestGridView");
            gridView = gridViewGO.AddComponent<GridView>();

            blockPrefabGO = new GameObject("TestBlockPrefab");
            SpriteRenderer spriteRenderer = blockPrefabGO.AddComponent<SpriteRenderer>();
            var previewGO = new GameObject("Preview");
            previewGO.transform.SetParent(blockPrefabGO.transform);
            SpriteRenderer previewRenderer = previewGO.AddComponent<SpriteRenderer>();
            BlockView blockView = blockPrefabGO.AddComponent<BlockView>();
            var blockViewSo = new SerializedObject(blockView);
            blockViewSo.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            blockViewSo.FindProperty("previewRenderer").objectReferenceValue = previewRenderer;
            blockViewSo.ApplyModifiedPropertiesWithoutUndo();

            IBlockAnimator animator = new InstantBlockAnimator();
            gridView.Initialize(levelData, blockView, animator);

            gridModel = new GridModel(Width, Height);
            BuildFlow(moveLimit: 999, targetScore: 999999);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridViewGO);
            Object.DestroyImmediate(blockPrefabGO);
            Object.DestroyImmediate(levelData);
            Object.DestroyImmediate(blockType);
            Object.DestroyImmediate(palette);
        }

        [Test]
        public void TryBlastAt_MatchesGroup_IncreasesScoreAndDecrementsMoves()
        {
            gridModel.PlacePiece(0, new ColorBlockPiece(0));
            gridModel.PlacePiece(1, new ColorBlockPiece(0));
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            int movesBefore = gameState.MovesLeft;

            flow.TryBlastAt(0);

            Assert.AreEqual(scoreBefore + 2 * ScorePerBlock, gameState.Score);
            Assert.AreEqual(movesBefore - 1, gameState.MovesLeft);
        }

        [Test]
        public void TryBlastAt_GroupOfSix_CreatesRocketAtTappedCell()
        {
            PlaceConnectedGroup(colorId: 0, count: 6);
            FillRemainingWithColor(1);
            ShowBoard();

            flow.TryBlastAt(0);

            Assert.IsInstanceOf<RocketPiece>(gridModel.GetPiece(0));
        }

        [Test]
        public void TryBlastAt_GroupOfEight_CreatesBombAtTappedCell()
        {
            PlaceConnectedGroup(colorId: 0, count: 8);
            FillRemainingWithColor(1);
            ShowBoard();

            flow.TryBlastAt(0);

            Assert.IsInstanceOf<BombPiece>(gridModel.GetPiece(0));
        }

        [Test]
        public void TryBlastAt_GroupOfTen_CreatesBallAtTappedCell()
        {
            PlaceConnectedGroup(colorId: 0, count: 10);
            FillRemainingWithColor(1);
            ShowBoard();

            flow.TryBlastAt(0);

            Assert.IsInstanceOf<BallPiece>(gridModel.GetPiece(0));
        }

        [Test]
        public void TryBlastAt_BombPlusBomb_ClearsFixedFiveByFiveArea()
        {
            gridModel.PlacePiece(12, new BombPiece());
            gridModel.PlacePiece(11, new BombPiece());
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            flow.TryBlastAt(12);

            // GetArea(12, radius:2) on a fully-occupied 5x5 board covers the whole board.
            Assert.AreEqual(scoreBefore + 25 * ScorePerBlock, gameState.Score);
        }

        [Test]
        public void TryBlastAt_RocketPlusRocket_ClearsFullRowAndColumn()
        {
            gridModel.PlacePiece(12, new RocketPiece(RocketOrientation.Horizontal));
            gridModel.PlacePiece(11, new RocketPiece(RocketOrientation.Vertical));
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            flow.TryBlastAt(12);

            // Row (5) + column (5) - 1 shared center cell = 9.
            Assert.AreEqual(scoreBefore + 9 * ScorePerBlock, gameState.Score);
        }

        [Test]
        public void TryBlastAt_BombPlusRocket_ClearsThickCrossInBothDirections()
        {
            // Regression test: Bomb+Rocket previously had no combo rule and fell back to
            // each piece firing its own single-line/area effect. It must clear a full
            // thick cross instead — 3 rows AND 3 columns, both directions at once.
            gridModel.PlacePiece(12, new RocketPiece(RocketOrientation.Horizontal));
            gridModel.PlacePiece(11, new BombPiece());
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            flow.TryBlastAt(12);

            // GetCross(12, radius:1) on a fully-occupied 5x5 board = 3*5 + 3*5 - 3*3 = 21.
            Assert.AreEqual(scoreBefore + 21 * ScorePerBlock, gameState.Score);
        }

        [Test]
        public void TryBlastAt_ThreeBombCluster_StaysSameSizeAsTwoBombCombo()
        {
            // Confirms a same-kind cluster of 3+ doesn't scale the effect up.
            gridModel.PlacePiece(10, new BombPiece());
            gridModel.PlacePiece(11, new BombPiece());
            gridModel.PlacePiece(12, new BombPiece());
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            flow.TryBlastAt(10);

            // GetArea(10, radius:2) clipped to the board edge (x in [0,2], y in [0,4]) = 15 cells,
            // identical to what a 2-bomb combo at the same position would clear.
            Assert.AreEqual(scoreBefore + 15 * ScorePerBlock, gameState.Score);
        }

        [Test]
        public void PopulateInitialBoard_WithBoxes_IncludesThemInTheSpawnList()
        {
            // Regression test: PlaceInitialBoxes used to place Box pieces directly in the
            // model without ever including them in the returned spawn list, so the View
            // never created a BlockView for them at all — the obstacle was invisible even
            // though GridView had a perfectly valid box sprite wired up.
            var levelSo = new SerializedObject(levelData);
            levelSo.FindProperty("initialBoxCount").intValue = 3;
            levelSo.ApplyModifiedPropertiesWithoutUndo();

            IReadOnlyList<GridSpawn> spawns = flow.PopulateInitialBoard();

            int boxSpawnCount = 0;
            foreach (GridSpawn spawn in spawns)
                if (spawn.Piece is BoxPiece)
                    boxSpawnCount++;

            Assert.AreEqual(3, boxSpawnCount);
            Assert.AreEqual(gridModel.CellCount, spawns.Count);
        }

        [Test]
        public void TryBlastAt_TappedDirectly_DoesNothing()
        {
            gridModel.PlacePiece(12, new BoxPiece());
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            int movesBefore = gameState.MovesLeft;

            flow.TryBlastAt(12);

            Assert.AreEqual(scoreBefore, gameState.Score);
            Assert.AreEqual(movesBefore, gameState.MovesLeft);
            Assert.IsInstanceOf<BoxPiece>(gridModel.GetPiece(12));
        }

        [Test]
        public void TryBlastAt_BoxCaughtInPowerUpArea_IsCleared()
        {
            gridModel.PlacePiece(12, new BombPiece());
            gridModel.PlacePiece(13, new BoxPiece()); // inside the Bomb's 3x3 area
            FillRemainingWithColor(1);
            ShowBoard();

            flow.TryBlastAt(12);

            Assert.IsFalse(gridModel.GetPiece(13) is BoxPiece);
        }

        [Test]
        public void TryBlastAt_ChainReaction_CascadesThroughMultipleUntappedPowerUps()
        {
            // Two bombs blast a 5x5 area that happens to contain a rocket; that rocket's
            // own line in turn happens to contain a ball — neither was tapped or part of
            // the original cluster, but both must still fire.
            gridModel.PlacePiece(10, new BombPiece());
            gridModel.PlacePiece(11, new BombPiece());
            gridModel.PlacePiece(1, new RocketPiece(RocketOrientation.Horizontal));
            gridModel.PlacePiece(3, new BallPiece(targetColorId: 0));
            FillRemainingWithColor(1);
            ShowBoard();

            int scoreBefore = gameState.Score;
            flow.TryBlastAt(10);

            // 15-cell area + the rocket's row (0,1,2,3,4) contributes 2 new cells (3 and 4).
            Assert.AreEqual(scoreBefore + 17 * ScorePerBlock, gameState.Score);
            Assert.IsFalse(gridModel.GetPiece(1) is RocketPiece);
            Assert.IsFalse(gridModel.GetPiece(3) is BallPiece);
        }

        [Test]
        public void TryBlastAt_ScoreReachesTarget_RaisesLevelWon()
        {
            BuildFlow(moveLimit: 10, targetScore: 15);
            gridModel.PlacePiece(0, new ColorBlockPiece(0));
            gridModel.PlacePiece(1, new ColorBlockPiece(0));
            FillRemainingWithColor(1);
            ShowBoard();

            bool won = false;
            void OnWon() => won = true;
            GameEvents.OnLevelWon += OnWon;
            try
            {
                flow.TryBlastAt(0); // 2 cells * 10 = 20 >= target of 15
                Assert.IsTrue(won);
                Assert.IsTrue(gameState.IsLevelOver);
            }
            finally
            {
                GameEvents.OnLevelWon -= OnWon;
            }
        }

        [Test]
        public void TryBlastAt_MovesExhaustedBeforeTarget_RaisesLevelLost()
        {
            BuildFlow(moveLimit: 1, targetScore: 999999);
            gridModel.PlacePiece(0, new ColorBlockPiece(0));
            gridModel.PlacePiece(1, new ColorBlockPiece(0));
            FillRemainingWithColor(1);
            ShowBoard();

            bool lost = false;
            void OnLost() => lost = true;
            GameEvents.OnLevelLost += OnLost;
            try
            {
                flow.TryBlastAt(0);
                Assert.IsTrue(lost);
            }
            finally
            {
                GameEvents.OnLevelLost -= OnLost;
            }
        }

        [Test]
        public void TryBlastAt_AfterLevelOver_IsANoOp()
        {
            BuildFlow(moveLimit: 10, targetScore: 1);
            gridModel.PlacePiece(0, new ColorBlockPiece(0));
            gridModel.PlacePiece(1, new ColorBlockPiece(0));
            gridModel.PlacePiece(3, new ColorBlockPiece(0));
            gridModel.PlacePiece(4, new ColorBlockPiece(0));
            FillRemainingWithColor(1);
            ShowBoard();

            flow.TryBlastAt(0); // wins immediately
            int scoreAfterWin = gameState.Score;

            flow.TryBlastAt(3);

            Assert.AreEqual(scoreAfterWin, gameState.Score);
        }

        private void BuildFlow(int moveLimit, int targetScore)
        {
            gameState = new GameStateModel(moveLimit, targetScore);
            flow = new GameFlowController(gridModel, gameState, gridView, levelData);
        }

        private void PlaceConnectedGroup(int colorId, int count)
        {
            for (int i = 0; i < count; i++)
                gridModel.PlacePiece(i, new ColorBlockPiece(colorId));
        }

        private void FillRemainingWithColor(int colorId)
        {
            IGridPiece filler = new ColorBlockPiece(colorId);
            for (int i = 0; i < gridModel.CellCount; i++)
                if (gridModel.GetPiece(i) == null)
                    gridModel.PlacePiece(i, filler);
        }

        private void ShowBoard()
        {
            var spawns = new List<GridSpawn>();
            for (int i = 0; i < gridModel.CellCount; i++)
                spawns.Add(new GridSpawn(i, gridModel.GetPiece(i)));
            gridView.ShowInitialBoard(spawns);
        }
    }
}
