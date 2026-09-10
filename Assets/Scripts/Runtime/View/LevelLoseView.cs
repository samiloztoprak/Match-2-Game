using Match2.Systems.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match2.View
{
    /// <summary>
    /// Reacts to <see cref="GameEvents.OnLevelLost"/> by returning directly
    /// to the main menu — no celebration and no <see cref="Systems.LevelProgress"/>
    /// advance, since a failed level means the player retries the same one.
    /// Kept separate from <see cref="LevelWinView"/> since the win path has a
    /// whole animated sequence to play out first; a loss just leaves immediately.
    /// </summary>
    public class LevelLoseView : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        private void OnEnable()
        {
            GameEvents.OnLevelLost += HandleLevelLost;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelLost -= HandleLevelLost;
        }

        private static void HandleLevelLost()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}
