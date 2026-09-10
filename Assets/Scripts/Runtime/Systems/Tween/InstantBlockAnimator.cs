using System;
using UnityEngine;

namespace Match2.Systems.Tween
{
    /// <summary>
    /// No-animation, dependency-free fallback: applies the end state
    /// immediately. Useful as a test double or a debug "no juiciness" mode.
    /// </summary>
    public class InstantBlockAnimator : IBlockAnimator
    {
        public void ResetState(Transform target)
        {
        }

        public void PlayMove(Transform target, Vector3 targetPosition, Action onComplete)
        {
            target.position = targetPosition;
            onComplete?.Invoke();
        }

        public void PlayBlast(Transform target, Action onComplete)
        {
            onComplete?.Invoke();
        }

        public void PlayAppear(Transform target)
        {
        }

        public void PlayTransform(Transform target, Action onComplete)
        {
            onComplete?.Invoke();
        }

        public void PlayUIMove(RectTransform target, Vector2 anchoredPosition, Action onComplete)
        {
            target.anchoredPosition = anchoredPosition;
            onComplete?.Invoke();
        }
    }
}
