using Match2.Controller;
using Match2.Data;
using Match2.Model;
using Match2.Systems;
using Match2.Systems.Tween;
using Match2.View;
using UnityEngine;

namespace Match2
{
    /// <summary>
    /// Scene-root wiring: builds the Model, hands it to the View/Controller,
    /// and populates the initial board. Plain manual dependency wiring —
    /// no DI framework needed at this scale.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private LevelSet levelSet;
        [SerializeField] private BlockView blockPrefab;
        [SerializeField] private GridView gridView;
        [SerializeField] private GridInputController inputController;
        [SerializeField] private BoardCameraFitter cameraFitter;
        [SerializeField] private BackgroundFitter backgroundFitter;
        [SerializeField] private LevelWinView levelWinView;

        private void Start()
        {
            LevelData levelData = levelSet.GetLevel(LevelProgress.CurrentLevel);
            var gridModel = new GridModel(levelData.GridWidth, levelData.GridHeight);
            var gameState = new GameStateModel(levelData.MoveLimit, levelData.TargetScore);
            IBlockAnimator animator = new DoTweenBlockAnimator();

            gridView.Initialize(levelData, blockPrefab, animator);
            cameraFitter.Fit(gridView.BoardWidth, gridView.BoardHeight);
            backgroundFitter.Fit(); // after cameraFitter.Fit() so the background matches the camera's final orthographic size
            levelWinView.Initialize(animator);

            var flowController = new GameFlowController(gridModel, gameState, gridView, levelData);
            var initialSpawns = flowController.PopulateInitialBoard();
            gridView.ShowInitialBoard(initialSpawns);
            flowController.RefreshPowerUpPreviews();

            inputController.Initialize(flowController);
        }
    }
}
