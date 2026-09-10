using Match2.Systems.Events;

namespace Match2.Model
{
    /// <summary>
    /// Tracks moves left and score against a level's win condition, and
    /// raises the corresponding <see cref="GameEvents"/> as they change.
    /// </summary>
    public class GameStateModel
    {
        private readonly int targetScore;

        public GameStateModel(int moveLimit, int targetScore)
        {
            MovesLeft = moveLimit;
            this.targetScore = targetScore;
        }

        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public bool IsLevelOver { get; private set; }

        /// <summary>Call once per player move (a successful blast).</summary>
        public void RegisterMove(int scoreDelta)
        {
            if (IsLevelOver)
                return;

            Score += scoreDelta;
            MovesLeft--;

            GameEvents.RaiseScoreChanged(Score);
            GameEvents.RaiseMovesChanged(MovesLeft);

            if (Score >= targetScore)
            {
                IsLevelOver = true;
                GameEvents.RaiseLevelWon();
            }
            else if (MovesLeft <= 0)
            {
                IsLevelOver = true;
                GameEvents.RaiseLevelLost();
            }
        }
    }
}
