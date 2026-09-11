namespace Match2.Model
{
    /// <summary>
    /// A simple obstacle: blocks its cell and is never part of a color match
    /// (<see cref="IsMatchable"/> is false, same seam power-ups use), but
    /// unlike a power-up it does nothing when tapped directly — only a
    /// power-up effect (a Bomb's area, a Rocket's line, a chain reaction)
    /// whose footprint reaches its cell clears it, in a single hit. Placed at
    /// level start only (see <see cref="Data.LevelData.InitialBoxCount"/>);
    /// nothing ever spawns a new one mid-level.
    /// </summary>
    public readonly struct BoxPiece : IGridPiece
    {
        public bool IsMatchable => false;
        public int MatchKey => -1;
    }
}
