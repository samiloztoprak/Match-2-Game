using Match2.Systems.Events;
using TMPro;
using UnityEngine;

namespace Match2.View
{
    /// <summary>Displays score/moves and reacts only to <see cref="GameEvents"/> — no game logic.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnMovesChanged += HandleMovesChanged;
            GameEvents.OnLevelWon += HandleLevelWon;
            GameEvents.OnLevelLost += HandleLevelLost;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnMovesChanged -= HandleMovesChanged;
            GameEvents.OnLevelWon -= HandleLevelWon;
            GameEvents.OnLevelLost -= HandleLevelLost;
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
        }

        private void HandleMovesChanged(int movesLeft)
        {
            if (movesText != null)
                movesText.text = movesLeft.ToString();
        }

        private void HandleLevelWon()
        {
            if (winPanel != null)
                winPanel.SetActive(true);
        }

        private void HandleLevelLost()
        {
            if (losePanel != null)
                losePanel.SetActive(true);
        }
    }
}
