using System.Collections.Generic;
using UnityEngine;

namespace Match2.Data
{
    /// <summary>
    /// The ordered sequence of levels the game progresses through, keyed by
    /// the 1-based level number <see cref="Systems.LevelProgress"/> tracks.
    /// Kept as one asset (rather than <see cref="GameBootstrapper"/> holding
    /// a raw list) so the level order is data, not scene wiring.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelSet", menuName = "Match2/Level Set")]
    public class LevelSet : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new();

        /// <summary>
        /// The level to play for a given 1-based level number, clamped to the
        /// last defined level once the player has progressed past all of them
        /// (so the game keeps replaying the hardest level rather than
        /// crashing once content runs out).
        /// </summary>
        public LevelData GetLevel(int levelNumber)
        {
            int index = Mathf.Clamp(levelNumber - 1, 0, levels.Count - 1);
            return levels[index];
        }
    }
}
