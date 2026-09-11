using Match2.Systems.Events;
using UnityEngine;

namespace Match2.Systems.Audio
{
    /// <summary>Plays a short sound effect for each <see cref="GameEvents"/> notification — no game logic, just a reaction, same role as <see cref="View.HudView"/> but for audio instead of text.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxController : MonoBehaviour
    {
        [SerializeField] private AudioClip blastClip;
        [SerializeField] private AudioClip levelWonClip;
        [SerializeField] private AudioClip levelLostClip;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnLevelWon += HandleLevelWon;
            GameEvents.OnLevelLost += HandleLevelLost;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnLevelWon -= HandleLevelWon;
            GameEvents.OnLevelLost -= HandleLevelLost;
        }

        private void HandleScoreChanged(int newScore)
        {
            audioSource.PlayOneShot(blastClip);
        }

        private void HandleLevelWon()
        {
            audioSource.PlayOneShot(levelWonClip);
        }

        private void HandleLevelLost()
        {
            audioSource.PlayOneShot(levelLostClip);
        }
    }
}
