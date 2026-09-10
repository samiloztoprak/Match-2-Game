using UnityEngine;

namespace Match2.View
{
    /// <summary>
    /// Sizes an orthographic camera so a board of the given world-space
    /// dimensions always fits fully on screen, whichever dimension (width or
    /// height) is the tighter constraint for the current aspect ratio.
    /// Re-checks every frame and only recomputes when the screen size
    /// actually changes (device rotation, window resize in the Editor).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField] private float margin = 0.5f;

        private Camera targetCamera;
        private float boardWidth;
        private float boardHeight;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool hasBoardSize;

        /// <summary>Call once the board's world-space size is known (grid size * cell size).</summary>
        public void Fit(float width, float height)
        {
            targetCamera = GetComponent<Camera>();
            boardWidth = width;
            boardHeight = height;
            hasBoardSize = true;

            ApplyFit();
        }

        private void Update()
        {
            if (!hasBoardSize)
                return;

            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
                return;

            ApplyFit();
        }

        private void ApplyFit()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float sizeToFitHeight = boardHeight / 2f + margin;
            float sizeToFitWidth = (boardWidth / 2f + margin) / targetCamera.aspect;

            targetCamera.orthographicSize = Mathf.Max(sizeToFitHeight, sizeToFitWidth);
        }
    }
}
