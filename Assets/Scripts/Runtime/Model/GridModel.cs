using System;
using System.Collections.Generic;

namespace Match2.Model
{
    /// <summary>
    /// Pure board state and rules for a match-2 blast grid, stored as a
    /// flattened 1D array. Has no knowledge of MonoBehaviours, rendering,
    /// or tweening — <see cref="GridUtility"/> handles all index/(x,y) math.
    /// </summary>
    public class GridModel
    {
        private readonly IGridPiece[] cells;

        public GridModel(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new IGridPiece[width * height];
        }

        public int Width { get; }
        public int Height { get; }
        public int CellCount => cells.Length;

        public IGridPiece GetPiece(int index) => cells[index];

        /// <summary>Iterative BFS over same-<see cref="IGridPiece.MatchKey"/> neighbors.</summary>
        public IReadOnlyList<int> FindConnectedGroup(int startIndex)
        {
            IGridPiece startPiece = cells[startIndex];
            if (startPiece == null || !startPiece.IsMatchable)
                return Array.Empty<int>();

            return FloodFillGroup(startIndex, new HashSet<int>());
        }

        /// <summary>
        /// Every matchable group on the board in one pass, keyed by each
        /// member cell's index -> that group's size. Lets the caller know,
        /// for every cell, what tapping it would do before the player taps
        /// anything (e.g. to preview which cells would create a power-up).
        /// </summary>
        public IReadOnlyDictionary<int, int> ComputeGroupSizes()
        {
            var sizeByIndex = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            for (int start = 0; start < cells.Length; start++)
            {
                if (visited.Contains(start))
                    continue;

                IGridPiece piece = cells[start];
                if (piece == null || !piece.IsMatchable)
                    continue;

                List<int> group = FloodFillGroup(start, visited);
                foreach (int index in group)
                    sizeByIndex[index] = group.Count;
            }

            return sizeByIndex;
        }

        /// <summary>Shared BFS core for <see cref="FindConnectedGroup"/> and <see cref="ComputeGroupSizes"/>.</summary>
        private List<int> FloodFillGroup(int startIndex, HashSet<int> visited)
        {
            IGridPiece startPiece = cells[startIndex];
            visited.Add(startIndex);
            var group = new List<int> { startIndex };
            var queue = new Queue<int>();
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int neighbor in GridUtility.GetNeighbors(current, Width, Height))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    IGridPiece neighborPiece = cells[neighbor];
                    if (neighborPiece == null || !neighborPiece.IsMatchable)
                        continue;
                    if (neighborPiece.MatchKey != startPiece.MatchKey)
                        continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    group.Add(neighbor);
                }
            }

            return group;
        }

        public static bool IsBlastable(IReadOnlyList<int> group) => group.Count >= 2;

        /// <summary>Directly places a piece into a cell — used to drop a newly created power-up where a match was tapped.</summary>
        public void PlacePiece(int index, IGridPiece piece)
        {
            cells[index] = piece;
        }

        /// <summary>Every currently-occupied index in <paramref name="index"/>'s full row or column.</summary>
        public IReadOnlyList<int> GetLine(int index, RocketOrientation orientation)
        {
            GridUtility.ToCoords(index, Width, out int x, out int y);
            var line = new List<int>();

            if (orientation == RocketOrientation.Horizontal)
            {
                for (int column = 0; column < Width; column++)
                    line.Add(GridUtility.ToIndex(column, y, Width));
            }
            else
            {
                for (int row = 0; row < Height; row++)
                    line.Add(GridUtility.ToIndex(x, row, Width));
            }

            line.RemoveAll(lineIndex => cells[lineIndex] == null);
            return line;
        }

        /// <summary>
        /// Every currently-occupied index in the square area centered on
        /// <paramref name="index"/>, extending <paramref name="radius"/> cells
        /// in each direction (radius 1 = 3x3), clamped to the board's edges.
        /// </summary>
        public IReadOnlyList<int> GetArea(int index, int radius)
        {
            GridUtility.ToCoords(index, Width, out int centerX, out int centerY);
            var area = new List<int>();

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!GridUtility.IsInBounds(x, y, Width, Height))
                        continue;

                    int cellIndex = GridUtility.ToIndex(x, y, Width);
                    if (cells[cellIndex] != null)
                        area.Add(cellIndex);
                }
            }

            return area;
        }

        /// <summary>
        /// Every currently-occupied cell in the <c>(2*radius+1)</c> rows and
        /// <c>(2*radius+1)</c> columns centered on <paramref name="index"/>'s
        /// coordinates. <paramref name="radius"/> 0 is a plain cross (one full
        /// row + one full column); 1 is a thick cross (3 rows + 3 columns).
        /// Used by power-up combos: Rocket+Rocket -> radius 0, Bomb+Ball -> radius 1.
        /// </summary>
        public IReadOnlyList<int> GetCross(int index, int radius)
        {
            GridUtility.ToCoords(index, Width, out int centerX, out int centerY);
            var result = new HashSet<int>();

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= Height)
                    continue;

                for (int x = 0; x < Width; x++)
                    AddIfOccupied(GridUtility.ToIndex(x, y, Width), result);
            }

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= Width)
                    continue;

                for (int y = 0; y < Height; y++)
                    AddIfOccupied(GridUtility.ToIndex(x, y, Width), result);
            }

            return new List<int>(result);
        }

        private void AddIfOccupied(int index, HashSet<int> destination)
        {
            if (cells[index] != null)
                destination.Add(index);
        }

        /// <summary>Every currently-occupied, matchable cell sharing the given <see cref="IGridPiece.MatchKey"/> — used by the Ball power-up to find every block of its target color, anywhere on the board.</summary>
        public IReadOnlyList<int> GetAllWithMatchKey(int matchKey)
        {
            var matches = new List<int>();
            foreach (int index in GetAllMatchableIndices())
            {
                if (cells[index].MatchKey == matchKey)
                    matches.Add(index);
            }

            return matches;
        }

        /// <summary>Every currently-occupied, matchable cell on the board, regardless of color — used by the Ball+Ball combo to clear every ordinary block at once.</summary>
        public IReadOnlyList<int> GetAllMatchableIndices()
        {
            var matches = new List<int>();
            for (int i = 0; i < cells.Length; i++)
            {
                IGridPiece piece = cells[i];
                if (piece != null && piece.IsMatchable)
                    matches.Add(i);
            }

            return matches;
        }

        /// <summary>Clears the given cells. Returns the same indices, now empty.</summary>
        public IReadOnlyList<int> Blast(IReadOnlyList<int> group)
        {
            foreach (int index in group)
                cells[index] = null;
            return group;
        }

        /// <summary>
        /// Compacts each column downward (toward y = 0) so pieces fall into
        /// gaps left by a blast. Returns the individual piece moves so the
        /// view can animate each one falling to its new position.
        /// </summary>
        public IReadOnlyList<GridMove> ApplyGravity()
        {
            var moves = new List<GridMove>();

            for (int x = 0; x < Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < Height; y++)
                {
                    int readIndex = GridUtility.ToIndex(x, y, Width);
                    IGridPiece piece = cells[readIndex];
                    if (piece == null)
                        continue;

                    int writeIndex = GridUtility.ToIndex(x, writeY, Width);
                    if (writeIndex != readIndex)
                    {
                        cells[writeIndex] = piece;
                        cells[readIndex] = null;
                        moves.Add(new GridMove(readIndex, writeIndex));
                    }

                    writeY++;
                }
            }

            return moves;
        }

        public IReadOnlyList<int> GetEmptyIndices()
        {
            var empties = new List<int>();
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null)
                    empties.Add(i);
            }

            return empties;
        }

        /// <summary>
        /// Places a new piece (from <paramref name="pieceFactory"/>) into each
        /// given index. Also used to populate the board initially, by refilling
        /// every index of a freshly created, all-empty grid.
        /// </summary>
        public IReadOnlyList<GridSpawn> Refill(IReadOnlyList<int> emptyIndices, Func<int, IGridPiece> pieceFactory)
        {
            var spawns = new List<GridSpawn>(emptyIndices.Count);
            foreach (int index in emptyIndices)
            {
                IGridPiece piece = pieceFactory(index);
                cells[index] = piece;
                spawns.Add(new GridSpawn(index, piece));
            }

            return spawns;
        }
    }
}
