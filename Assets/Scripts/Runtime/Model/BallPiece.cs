namespace Match2.Model
{
    /// <summary>
    /// Created when a color match is large enough (bigger than a Bomb's
    /// threshold). Remembers the color it was created from — since this
    /// project has no swap/drag interaction, that's the color it clears
    /// board-wide when tapped. Never joins a color flood-fill (<see cref="IsMatchable"/> is false).
    /// </summary>
    public readonly struct BallPiece : IGridPiece
    {
        public BallPiece(int targetColorId)
        {
            TargetColorId = targetColorId;
        }

        public int TargetColorId { get; }

        public bool IsMatchable => false;
        public int MatchKey => -1;
    }
}
