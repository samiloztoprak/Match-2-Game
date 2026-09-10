namespace Match2.Model
{
    /// <summary>
    /// A single occupant of a grid cell. Color blocks are the only
    /// implementation today; obstacles (Box, Balloon) implement this
    /// same interface later without requiring changes to GridModel.
    /// </summary>
    public interface IGridPiece
    {
        /// <summary>
        /// Whether this piece can be grouped with same-key neighbors and blasted.
        /// Obstacles that only react to *adjacent* blasts (e.g. a Box) return false.
        /// </summary>
        bool IsMatchable { get; }

        /// <summary>
        /// The value used to decide if two matchable pieces belong to the same group.
        /// </summary>
        int MatchKey { get; }
    }
}
