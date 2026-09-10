namespace Match2.Model
{
    /// <summary>A piece that moved from one cell to another (gravity).</summary>
    public readonly struct GridMove
    {
        public GridMove(int fromIndex, int toIndex)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public int FromIndex { get; }
        public int ToIndex { get; }
    }

    /// <summary>A new piece placed into a previously empty cell (refill).</summary>
    public readonly struct GridSpawn
    {
        public GridSpawn(int index, IGridPiece piece)
        {
            Index = index;
            Piece = piece;
        }

        public int Index { get; }
        public IGridPiece Piece { get; }
    }
}
