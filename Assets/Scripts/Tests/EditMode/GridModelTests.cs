using System.Collections.Generic;
using Match2.Model;
using NUnit.Framework;

namespace Match2.Tests
{
    public class GridModelTests
    {
        [Test]
        public void FindConnectedGroup_GroupsOnlySameColorOrthogonalNeighbors()
        {
            // 3x3 grid:
            // y2: A? no ->  B  B  B
            // y1:            A  B  B
            // y0:            A  A  B
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1, 3 }, _ => new ColorBlockPiece(colorId: 0)); // "A" group
            model.Refill(new[] { 2, 4, 5, 6, 7, 8 }, _ => new ColorBlockPiece(colorId: 1)); // "B" group

            IReadOnlyList<int> groupA = model.FindConnectedGroup(0);
            IReadOnlyList<int> groupB = model.FindConnectedGroup(2);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3 }, groupA);
            CollectionAssert.AreEquivalent(new[] { 2, 4, 5, 6, 7, 8 }, groupB);
        }

        [Test]
        public void IsBlastable_ReturnsFalseForSingleIsolatedPiece_TrueForGroupOfTwoOrMore()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 4 }, _ => new ColorBlockPiece(0)); // isolated among empty neighbors
            model.Refill(new[] { 0, 1 }, _ => new ColorBlockPiece(1)); // adjacent pair

            IReadOnlyList<int> isolatedGroup = model.FindConnectedGroup(4);
            IReadOnlyList<int> pairGroup = model.FindConnectedGroup(0);

            Assert.IsFalse(GridModel.IsBlastable(isolatedGroup));
            Assert.IsTrue(GridModel.IsBlastable(pairGroup));
        }

        [Test]
        public void Blast_ClearsGivenCells()
        {
            var model = new GridModel(2, 1);
            model.Refill(new[] { 0, 1 }, _ => new ColorBlockPiece(0));

            IReadOnlyList<int> group = model.FindConnectedGroup(0);
            model.Blast(group);

            Assert.IsNull(model.GetPiece(0));
            Assert.IsNull(model.GetPiece(1));
        }

        [Test]
        public void ApplyGravity_CompactsColumnDownward_AndReportsEachMove()
        {
            // Single column (width 1, height 4), pieces only at y=1 and y=3.
            var model = new GridModel(1, 4);
            model.Refill(new[] { 1, 3 }, index => new ColorBlockPiece(index == 1 ? 0 : 1));

            IReadOnlyList<GridMove> moves = model.ApplyGravity();

            Assert.AreEqual(2, moves.Count);
            Assert.AreEqual(0, ((ColorBlockPiece)model.GetPiece(0)).ColorId);
            Assert.AreEqual(1, ((ColorBlockPiece)model.GetPiece(1)).ColorId);
            Assert.IsNull(model.GetPiece(2));
            Assert.IsNull(model.GetPiece(3));
        }

        [Test]
        public void Refill_FillsEveryEmptyIndex_AndLeavesNoGaps()
        {
            var model = new GridModel(2, 2);

            IReadOnlyList<int> emptyIndices = model.GetEmptyIndices();
            IReadOnlyList<GridSpawn> spawns = model.Refill(emptyIndices, index => new ColorBlockPiece(index));

            Assert.AreEqual(4, emptyIndices.Count);
            Assert.AreEqual(4, spawns.Count);
            Assert.AreEqual(0, model.GetEmptyIndices().Count);
        }

        [Test]
        public void FindConnectedGroup_NeverIncludesANonMatchableRocketPiece()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1 }, _ => new ColorBlockPiece(0));
            model.PlacePiece(2, new RocketPiece(RocketOrientation.Horizontal));

            IReadOnlyList<int> group = model.FindConnectedGroup(0);

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, group);
        }

        [Test]
        public void PlacePiece_IsTreatedAsSolid_ByEmptyIndicesAndGravity()
        {
            var model = new GridModel(1, 3);
            model.PlacePiece(1, new RocketPiece(RocketOrientation.Vertical));

            CollectionAssert.AreEquivalent(new[] { 0, 2 }, model.GetEmptyIndices());

            model.ApplyGravity();

            Assert.IsInstanceOf<RocketPiece>(model.GetPiece(0));
            Assert.IsNull(model.GetPiece(1));
            Assert.IsNull(model.GetPiece(2));
        }

        [Test]
        public void GetLine_ReturnsFullRowOrColumn_DependingOnOrientation()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, _ => new ColorBlockPiece(0));

            IReadOnlyList<int> horizontalLine = model.GetLine(4, RocketOrientation.Horizontal);
            IReadOnlyList<int> verticalLine = model.GetLine(4, RocketOrientation.Vertical);

            CollectionAssert.AreEquivalent(new[] { 3, 4, 5 }, horizontalLine);
            CollectionAssert.AreEquivalent(new[] { 1, 4, 7 }, verticalLine);
        }

        [Test]
        public void FindConnectedGroup_NeverIncludesANonMatchableBombPiece()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1 }, _ => new ColorBlockPiece(0));
            model.PlacePiece(2, new BombPiece());

            IReadOnlyList<int> group = model.FindConnectedGroup(0);

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, group);
        }

        [Test]
        public void GetArea_ReturnsFull3x3AroundCenterCell()
        {
            var model = new GridModel(5, 5);
            model.Refill(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 }, _ => new ColorBlockPiece(0));

            IReadOnlyList<int> area = model.GetArea(index: 12, radius: 1); // center cell (2,2)

            CollectionAssert.AreEquivalent(new[] { 6, 7, 8, 11, 12, 13, 16, 17, 18 }, area);
        }

        [Test]
        public void GetArea_ClampsToBoardEdges_NearACorner()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, _ => new ColorBlockPiece(0));

            IReadOnlyList<int> area = model.GetArea(index: 0, radius: 1); // corner cell (0,0)

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3, 4 }, area);
        }

        [Test]
        public void FindConnectedGroup_NeverIncludesANonMatchableBallPiece()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1 }, _ => new ColorBlockPiece(0));
            model.PlacePiece(2, new BallPiece(targetColorId: 0));

            IReadOnlyList<int> group = model.FindConnectedGroup(0);

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, group);
        }

        [Test]
        public void GetAllWithMatchKey_FindsEveryScatteredCellOfThatColor_AcrossTheWholeBoard()
        {
            // Corners are color 0, edges are color 1, center is a non-matchable Bomb.
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 2, 6, 8 }, _ => new ColorBlockPiece(0));
            model.Refill(new[] { 1, 3, 5, 7 }, _ => new ColorBlockPiece(1));
            model.PlacePiece(4, new BombPiece());

            IReadOnlyList<int> colorZeroCells = model.GetAllWithMatchKey(0);
            IReadOnlyList<int> colorOneCells = model.GetAllWithMatchKey(1);

            CollectionAssert.AreEquivalent(new[] { 0, 2, 6, 8 }, colorZeroCells);
            CollectionAssert.AreEquivalent(new[] { 1, 3, 5, 7 }, colorOneCells);
        }

        [Test]
        public void ComputeGroupSizes_AssignsEachCellItsWholeGroupsSize_InOnePass()
        {
            // Same layout as FindConnectedGroup_GroupsOnlySameColorOrthogonalNeighbors:
            // "A" group (0, 1, 3) has 3 members; "B" group (2, 4, 5, 6, 7, 8) has 6.
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0, 1, 3 }, _ => new ColorBlockPiece(0));
            model.Refill(new[] { 2, 4, 5, 6, 7, 8 }, _ => new ColorBlockPiece(1));

            IReadOnlyDictionary<int, int> sizeByIndex = model.ComputeGroupSizes();

            foreach (int index in new[] { 0, 1, 3 })
                Assert.AreEqual(3, sizeByIndex[index], $"index {index} should report its group's size (3)");
            foreach (int index in new[] { 2, 4, 5, 6, 7, 8 })
                Assert.AreEqual(6, sizeByIndex[index], $"index {index} should report its group's size (6)");
        }

        [Test]
        public void ComputeGroupSizes_ExcludesNonMatchablePieces_AndIsolatedSingles()
        {
            var model = new GridModel(3, 3);
            model.Refill(new[] { 0 }, _ => new ColorBlockPiece(0)); // isolated single, not blastable
            model.Refill(new[] { 6, 7 }, _ => new ColorBlockPiece(1)); // a real pair
            model.PlacePiece(8, new RocketPiece(RocketOrientation.Horizontal));

            IReadOnlyDictionary<int, int> sizeByIndex = model.ComputeGroupSizes();

            Assert.AreEqual(1, sizeByIndex[0]);
            Assert.AreEqual(2, sizeByIndex[6]);
            Assert.AreEqual(2, sizeByIndex[7]);
            Assert.IsFalse(sizeByIndex.ContainsKey(8), "a non-matchable Rocket should never appear in the group-size map");
        }

        [Test]
        public void GetCross_RadiusZero_ReturnsPlainCross_OneFullRowPlusOneFullColumn()
        {
            var model = new GridModel(5, 5);
            FillEntireBoard(model);

            // Center cell index 12 = (x:2, y:2).
            IReadOnlyList<int> cross = model.GetCross(index: 12, radius: 0);

            CollectionAssert.AreEquivalent(new[] { 10, 11, 12, 13, 14, 2, 7, 17, 22 }, cross);
        }

        [Test]
        public void GetCross_RadiusOne_ReturnsThickCross_ThreeRowsPlusThreeColumns()
        {
            var model = new GridModel(5, 5);
            FillEntireBoard(model);

            IReadOnlyList<int> thickCross = model.GetCross(index: 12, radius: 1);

            Assert.AreEqual(21, thickCross.Count);
            CollectionAssert.Contains(thickCross, 5); // row-only region (x:0, y:1)
            CollectionAssert.Contains(thickCross, 1); // column-only region (x:1, y:0)
            CollectionAssert.Contains(thickCross, 12); // center
        }

        [Test]
        public void GetArea_RadiusTwo_ReturnsFull5x5AroundCenterCell()
        {
            var model = new GridModel(5, 5);
            FillEntireBoard(model);

            IReadOnlyList<int> area = model.GetArea(index: 12, radius: 2);

            Assert.AreEqual(25, area.Count); // the whole 5x5 board, centered exactly on the middle cell
        }

        private static void FillEntireBoard(GridModel model)
        {
            var allIndices = new List<int>();
            for (int i = 0; i < model.CellCount; i++)
                allIndices.Add(i);

            model.Refill(allIndices, _ => new ColorBlockPiece(0));
        }
    }
}
