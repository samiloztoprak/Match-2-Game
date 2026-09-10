using System.Collections.Generic;
using UnityEngine;

namespace Match2.Data
{
    /// <summary>
    /// Per-level design values: board size, win condition, and how many
    /// block colors are in play. Keeping this as data (rather than
    /// constants in code) is what lets grid size/color count vary per level —
    /// an early level might use 2 colors and a later one 5, all drawing from
    /// the same shared <see cref="BlockPaletteData"/> in a fixed order.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Match2/Level")]
    public class LevelData : ScriptableObject
    {
        [SerializeField, Min(2)] private int gridWidth = 8;
        [SerializeField, Min(2)] private int gridHeight = 8;
        [SerializeField, Min(1)] private int moveLimit = 20;
        [SerializeField, Min(1)] private int targetScore = 1000;
        [SerializeField] private BlockPaletteData palette;
        [SerializeField, Min(2), Tooltip("How many colors from the palette are in play this level, starting from the first entry.")]
        private int colorCount = 5;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int MoveLimit => moveLimit;
        public int TargetScore => targetScore;

        public IReadOnlyList<BlockTypeData> BlockTypes
        {
            get
            {
                IReadOnlyList<BlockTypeData> allColors = palette.BlockTypes;
                int count = Mathf.Clamp(colorCount, 2, allColors.Count);
                var activeColors = new List<BlockTypeData>(count);
                for (int i = 0; i < count; i++)
                    activeColors.Add(allColors[i]);

                return activeColors;
            }
        }
    }
}
