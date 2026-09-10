using System;
using Match2.Systems.Tween;
using UnityEngine;

namespace Match2.View
{
    /// <summary>
    /// Visual representation of one grid piece. Holds no game logic — it
    /// only displays a sprite and delegates all motion to an
    /// <see cref="IBlockAnimator"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlockView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer previewRenderer;

        private IBlockAnimator animator;

        public int GridIndex { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Places the piece at its starting position, rendering <paramref name="sprite"/>
        /// rotated by <paramref name="rotationDegrees"/> (most pieces use 0). Callers
        /// animate it in afterward — either falling via <see cref="MoveTo"/> (dropping
        /// from above) or popping in via <see cref="PlayAppearAnimation"/> (created in place).
        /// </summary>
        public void Init(int gridIndex, Sprite sprite, float rotationDegrees, Color tint, IBlockAnimator blockAnimator, Vector3 position)
        {
            GridIndex = gridIndex;
            animator = blockAnimator;
            animator.ResetState(transform);
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = tint;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));
            transform.localScale = Vector3.one; // undo the blast shrink from this instance's previous life in the pool
            ClearPreviewIcon(); // this instance's previous life in the pool may have left a preview showing
        }

        public void MoveTo(int newGridIndex, Vector3 targetPosition, Action onComplete)
        {
            GridIndex = newGridIndex;
            animator.PlayMove(transform, targetPosition, onComplete);
        }

        /// <summary>Slides toward another power-up's cell to visually merge before a combo blast. Doesn't update <see cref="GridIndex"/> — this instance is about to blast and return to the pool.</summary>
        public void PlayMergeSlide(Vector3 targetPosition, Action onComplete)
        {
            animator.PlayMove(transform, targetPosition, onComplete);
        }

        public void PlayBlast(Action onComplete)
        {
            animator.PlayBlast(transform, onComplete);
        }

        /// <summary>Swaps this piece's sprite in place (e.g. a color block becoming a Bomb for a Ball combo) and plays a small identity-change pulse, without touching <see cref="GridIndex"/> or the pool.</summary>
        public void PlayTransformInto(Sprite sprite, float rotationDegrees, Color tint, Action onComplete)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = tint;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            animator.PlayTransform(transform, onComplete);
        }

        public void PlayAppearAnimation()
        {
            animator.PlayAppear(transform);
        }

        /// <summary>Shows a small overlay icon (e.g. "this will become a Rocket") on top of the block.</summary>
        public void SetPreviewIcon(Sprite icon)
        {
            if (previewRenderer == null)
                return;

            previewRenderer.sprite = icon;
            previewRenderer.enabled = icon != null;
        }

        public void ClearPreviewIcon()
        {
            if (previewRenderer != null)
                previewRenderer.enabled = false;
        }
    }
}
