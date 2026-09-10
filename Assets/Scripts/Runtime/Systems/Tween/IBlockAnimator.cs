using System;
using UnityEngine;

namespace Match2.Systems.Tween
{
    /// <summary>
    /// Everything a block visually does (spawn, fall into place, blast) goes
    /// through this seam so exactly one implementation touches the tween
    /// library in use. Swapping tween libraries later means writing one new
    /// class here, not touching View/Controller code.
    /// </summary>
    public interface IBlockAnimator
    {
        /// <summary>
        /// Cancels any in-flight tween on <paramref name="target"/>. Called before
        /// a pooled block is reused, so a leftover animation (e.g. a blast's
        /// shrink) can never bleed into its next life.
        /// </summary>
        void ResetState(Transform target);

        void PlayMove(Transform target, Vector3 targetPosition, Action onComplete);
        void PlayBlast(Transform target, Action onComplete);

        /// <summary>Pop-in appearance for a piece created in place (e.g. a new power-up), as opposed to falling in.</summary>
        void PlayAppear(Transform target);

        /// <summary>A quick punch-scale pulse marking a piece changing identity in place (e.g. a color block turning into a Bomb), distinct from <see cref="PlayAppear"/>'s pop-in since nothing is newly created here.</summary>
        void PlayTransform(Transform target, Action onComplete);

        /// <summary>
        /// Moves a UI element to an anchored position (as opposed to <see cref="PlayMove"/>,
        /// which moves a world-space <see cref="Transform"/> by its absolute position) —
        /// used for HUD/menu elements like the level-win banner, where the
        /// destination is naturally expressed relative to the element's anchor.
        /// </summary>
        void PlayUIMove(RectTransform target, Vector2 anchoredPosition, Action onComplete);
    }
}
