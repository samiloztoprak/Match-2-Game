using System.Collections.Generic;
using UnityEngine;

namespace Match2.Data
{
    /// <summary>
    /// The full, ordered set of block colors the game can ever draw from.
    /// A single game-wide asset (not per-level, unlike <see cref="LevelData.ColorCount"/>)
    /// so every level picks its colors from the same fixed order, and adding
    /// a new color to the game only means editing this one list.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockPaletteData", menuName = "Match2/Block Palette")]
    public class BlockPaletteData : ScriptableObject
    {
        [SerializeField] private List<BlockTypeData> blockTypes = new();

        public IReadOnlyList<BlockTypeData> BlockTypes => blockTypes;
    }
}
