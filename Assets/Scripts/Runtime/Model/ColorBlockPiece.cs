namespace Match2.Model
{
    /// <summary>A plain colored block — the only piece type in this pass.</summary>
    public readonly struct ColorBlockPiece : IGridPiece
    {
        public ColorBlockPiece(int colorId)
        {
            ColorId = colorId;
        }

        public int ColorId { get; }

        public bool IsMatchable => true;
        public int MatchKey => ColorId;
    }
}
