using UnityEngine;

namespace Match2.Data
{
    /// <summary>
    /// One playable block color: identity, visuals, and feedback references.
    /// A level references a list of these instead of hardcoding colors.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockTypeData", menuName = "Match2/Block Type")]
    public class BlockTypeData : ScriptableObject
    {
        [SerializeField] private int colorId;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private ParticleSystem blastVfxPrefab;
        [SerializeField] private AudioClip blastSfx;

        public int ColorId => colorId;
        public Sprite Sprite => sprite;

        /// <summary>This color's representative RGB — used to tint the Ball power-up to whichever color it targets.</summary>
        public Color TintColor => tintColor;

        public ParticleSystem BlastVfxPrefab => blastVfxPrefab;
        public AudioClip BlastSfx => blastSfx;
    }
}
