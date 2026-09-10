using System.Collections.Generic;
using Match2.Model;
using NUnit.Framework;

namespace Match2.Tests
{
    public class GridUtilityTests
    {
        [Test]
        public void ToIndex_And_ToCoords_AreInverse()
        {
            const int width = 8;

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = GridUtility.ToIndex(x, y, width);
                    GridUtility.ToCoords(index, width, out int resultX, out int resultY);

                    Assert.AreEqual(x, resultX);
                    Assert.AreEqual(y, resultY);
                }
            }
        }

        [Test]
        public void GetNeighbors_CornerCell_ReturnsOnlyTwoInBoundsNeighbors()
        {
            const int width = 4;
            const int height = 4;
            int cornerIndex = GridUtility.ToIndex(0, 0, width);

            var neighbors = new List<int>(GridUtility.GetNeighbors(cornerIndex, width, height));

            Assert.AreEqual(2, neighbors.Count);
        }

        [Test]
        public void GetNeighbors_CenterCell_ReturnsFourNeighbors()
        {
            const int width = 4;
            const int height = 4;
            int centerIndex = GridUtility.ToIndex(1, 1, width);

            var neighbors = new List<int>(GridUtility.GetNeighbors(centerIndex, width, height));

            Assert.AreEqual(4, neighbors.Count);
        }
    }
}
