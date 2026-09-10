using System.Collections.Generic;

namespace Match2.Model
{
    /// <summary>
    /// The single place that knows how a flattened 1D grid array maps to
    /// (x, y) coordinates and neighbors. Everything else just deals with
    /// plain int indices.
    /// </summary>
    public static class GridUtility
    {
        public static int ToIndex(int x, int y, int width) => y * width + x;

        public static void ToCoords(int index, int width, out int x, out int y)
        {
            x = index % width;
            y = index / width;
        }

        public static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// Orthogonal (up/down/left/right) neighbor indices of <paramref name="index"/>
        /// that are within the grid bounds.
        /// </summary>
        public static IEnumerable<int> GetNeighbors(int index, int width, int height)
        {
            ToCoords(index, width, out int x, out int y);

            if (IsInBounds(x + 1, y, width, height)) yield return ToIndex(x + 1, y, width);
            if (IsInBounds(x - 1, y, width, height)) yield return ToIndex(x - 1, y, width);
            if (IsInBounds(x, y + 1, width, height)) yield return ToIndex(x, y + 1, width);
            if (IsInBounds(x, y - 1, width, height)) yield return ToIndex(x, y - 1, width);
        }
    }
}
