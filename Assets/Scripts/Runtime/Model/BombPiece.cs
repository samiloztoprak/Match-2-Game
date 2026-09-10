namespace Match2.Model
{
    /// <summary>
    /// Created when a color match is large enough (bigger than a Rocket's
    /// threshold). Sits on the board like any other piece until tapped, at
    /// which point it clears a square area around itself. Never joins a
    /// color flood-fill (<see cref="IsMatchable"/> is false).
    /// </summary>
    public readonly struct BombPiece : IGridPiece
    {
        public bool IsMatchable => false;
        public int MatchKey => -1;
    }
}
