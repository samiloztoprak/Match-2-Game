using Match2.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Match2.Controller
{
    /// <summary>Main menu: shows the player's current level on the Play button and starts the game scene when tapped. Built with UI Toolkit (see <c>Assets/UI/MainMenu.uxml</c>), not uGUI.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const string PlayButtonName = "play-button";

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            var playButton = root.Q<Button>(PlayButtonName);
            playButton.text = $"Level {LevelProgress.CurrentLevel}";
            playButton.clicked += StartGame;
        }

        private static void StartGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
