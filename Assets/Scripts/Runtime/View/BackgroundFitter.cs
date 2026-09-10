using UnityEngine;

namespace Match2.View
{
    /// <summary>
    /// Scales a background <see cref="SpriteRenderer"/> so it exactly covers
    /// an orthographic camera's viewport, regardless of the sprite's native
    /// size or the camera's current <see cref="Camera.orthographicSize"/>.
    /// Call <see cref="Fit"/> again whenever the camera's size changes (e.g.
    /// after <see cref="BoardCameraFitter.Fit"/> runs) so the background
    /// never falls out of sync with the visible viewport.
    /// </summary>
    public class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Camera targetCamera;

        public void Fit()
        {
            float viewportHeight = targetCamera.orthographicSize * 2f;
            float viewportWidth = viewportHeight * targetCamera.aspect;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            transform.localScale = new Vector3(viewportWidth / spriteSize.x, viewportHeight / spriteSize.y, 1f);
        }
    }
}
