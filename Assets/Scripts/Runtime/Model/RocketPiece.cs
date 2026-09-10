namespace Match2.Model
{
    public enum RocketOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Created when a color match is large enough. Sits on the board like any
    /// other piece until tapped, at which point it clears its full row or
    /// column. Never joins a color flood-fill (<see cref="IsMatchable"/> is false).
    /// </summary>
    public readonly struct RocketPiece : IGridPiece
    {
        public RocketPiece(RocketOrientation orientation)
        {
            Orientation = orientation;
        }

        public RocketOrientation Orientation { get; }

        public bool IsMatchable => false;
        public int MatchKey => -1;
    }
}
