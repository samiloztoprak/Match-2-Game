using System;
using Match2.Systems.Tween;
using TMPro;
using UnityEngine;

namespace Match2.View
{
    /// <summary>
    /// A short-lived "+N" label that pops in at a blasted cell's position and
    /// flies toward the score display, then is destroyed. Purely cosmetic —
    /// the actual score change already happened in the model; this only
    /// reinforces it visually.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class ScorePopupView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;

        private void Awake()
        {
            if (label == null)
                label = GetComponent<TextMeshPro>();
        }

        public void Play(int value, Vector3 destination, IBlockAnimator animator, Action onComplete)
        {
            label.text = $"+{value}";
            animator.PlayAppear(transform);
            animator.PlayMove(transform, destination, onComplete);
        }
    }
}
