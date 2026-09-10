using System;
using DG.Tweening;
using UnityEngine;

namespace Match2.Systems.Tween
{
    /// <summary>
    /// The only file in the project that touches DOTween. Implements the
    /// blast-puzzle "juiciness": a constant-speed fall (used for both
    /// gravity and new blocks dropping in from above the board) and a
    /// squash-then-shrink blast.
    /// </summary>
    public class DoTweenBlockAnimator : IBlockAnimator
    {
        private const float FallSpeedUnitsPerSecond = 14f;
        private const float MinMoveDuration = 0.08f;
        private const float BlastDuration = 0.18f;
        private const float AppearDuration = 0.25f;
        private const float TransformDuration = 0.2f;
        private const float UIMoveDuration = 0.6f;

        public void ResetState(Transform target)
        {
            target.DOKill();
        }

        public void PlayMove(Transform target, Vector3 targetPosition, Action onComplete)
        {
            // A constant fall speed (rather than a fixed duration) keeps blocks
            // stacked further above the board from overtaking ones below them.
            float distance = Vector3.Distance(target.position, targetPosition);
            float duration = Mathf.Max(MinMoveDuration, distance / FallSpeedUnitsPerSecond);

            target.DOMove(targetPosition, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayBlast(Transform target, Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOPunchScale(Vector3.one * 0.15f, 0.08f, vibrato: 1));
            sequence.Append(target.DOScale(Vector3.zero, BlastDuration).SetEase(Ease.InBack));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        public void PlayAppear(Transform target)
        {
            target.localScale = Vector3.zero;
            target.DOScale(Vector3.one, AppearDuration).SetEase(Ease.OutBack);
        }

        public void PlayTransform(Transform target, Action onComplete)
        {
            target.DOPunchScale(Vector3.one * 0.3f, TransformDuration, vibrato: 1)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayUIMove(RectTransform target, Vector2 anchoredPosition, Action onComplete)
        {
            // Uses the core DOTween.To(...) generic tweener directly rather than the
            // DOTweenModuleUI-specific DOAnchorPos extension, to avoid depending on
            // that loose plugin module file resolving correctly in every project setup.
            DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, anchoredPosition, UIMoveDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}
