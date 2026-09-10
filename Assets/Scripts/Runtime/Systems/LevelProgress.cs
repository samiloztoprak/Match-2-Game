using UnityEngine;

namespace Match2.Systems
{
    /// <summary>
    /// Which level the player is currently on, persisted across sessions.
    /// A single flat counter (not per-level unlock data) since the game is
    /// strictly linear — completing a level always advances to the next one.
    /// </summary>
    public static class LevelProgress
    {
        private const string CurrentLevelKey = "CurrentLevel";
        private const int FirstLevel = 1;

        public static int CurrentLevel => PlayerPrefs.GetInt(CurrentLevelKey, FirstLevel);

        public static void AdvanceToNextLevel()
        {
            PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevel + 1);
        }
    }
}
