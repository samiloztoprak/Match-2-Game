using Match2.Systems;
using Match2.Systems.Events;
using Match2.Systems.Tween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match2.View
{
    /// <summary>
    /// Reacts to <see cref="GameEvents.OnLevelWon"/>: flies the win logo in,
    /// holds briefly, flies it back out, then advances to the next level and
    /// returns to the main menu. Kept separate from <see cref="HudView"/>
    /// (which only ever mirrors live score/moves) since this owns a whole
    /// scene-transition flow, not just a label update.
    /// </summary>
    public class LevelWinView : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private RectTransform logo;
        [SerializeField] private Vector2 offScreenStartPosition = new(0f, 1400f);
        [SerializeField] private Vector2 onScreenPosition = Vector2.zero;
        [SerializeField] private Vector2 offScreenEndPosition = new(0f, -1400f);
        [SerializeField] private float holdDuration = 1f;

        private IBlockAnimator animator;

        public void Initialize(IBlockAnimator blockAnimator)
        {
            animator = blockAnimator;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelWon -= HandleLevelWon;
        }

        private void HandleLevelWon()
        {
            logo.anchoredPosition = offScreenStartPosition;
            animator.PlayUIMove(logo, onScreenPosition, () => Invoke(nameof(FlyOut), holdDuration));
        }

        private void FlyOut()
        {
            animator.PlayUIMove(logo, offScreenEndPosition, () =>
            {
                LevelProgress.AdvanceToNextLevel();
                SceneManager.LoadScene(MainMenuSceneName);
            });
        }
    }
}
