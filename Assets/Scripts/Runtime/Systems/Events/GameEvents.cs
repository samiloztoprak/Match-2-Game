using System;

namespace Match2.Systems.Events
{
    /// <summary>
    /// Central, typed pub/sub for cross-module notifications (Observer
    /// pattern) so Model/View/Controller don't need direct references to
    /// each other. Explicit named events rather than a generic bus, so
    /// every notification is discoverable and type-safe at a glance.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnMovesChanged;
        public static event Action OnLevelWon;
        public static event Action OnLevelLost;

        public static void RaiseScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
        public static void RaiseMovesChanged(int movesLeft) => OnMovesChanged?.Invoke(movesLeft);
        public static void RaiseLevelWon() => OnLevelWon?.Invoke();
        public static void RaiseLevelLost() => OnLevelLost?.Invoke();
    }
}
