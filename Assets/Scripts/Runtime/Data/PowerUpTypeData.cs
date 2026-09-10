using UnityEngine;

namespace Match2.Data
{
    public enum PowerUpKind
    {
        Rocket,
        Bomb,
        Ball
    }

    /// <summary>
    /// Visuals for one power-up kind. A game-wide constant (unlike
    /// <see cref="BlockTypeData"/>, which is chosen per level).
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpTypeData", menuName = "Match2/Power-Up Type")]
    public class PowerUpTypeData : ScriptableObject
    {
        [SerializeField] private PowerUpKind kind;
        [SerializeField] private Sprite sprite;

        public PowerUpKind Kind => kind;
        public Sprite Sprite => sprite;
    }
}
