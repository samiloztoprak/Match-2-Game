using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Match2.EditorTools
{
    /// <summary>
    /// One-off import tool for the Kenney "Puzzle Pack 2" tile spritesheets.
    /// Each color's PNG ships with a Sparrow/Starling-style XML atlas (top-left
    /// origin), which Unity's Sprite Editor can't read natively — this reads
    /// one named sub-rect per color out of the XML, converts it to Unity's
    /// bottom-left sprite-rect origin, and slices it as a single named sprite.
    /// </summary>
    public static class KenneySpriteSlicer
    {
        private const string SpriteFolder = "Assets/Art/Sprites/Kenney";

        [MenuItem("Match2/Import Kenney Cube Sprites")]
        public static void SliceCubeSprites()
        {
            SliceOne("Blue", "tileBlue_01.png", "BlueCube");
            SliceOne("Green", "tileGreen_01.png", "GreenCube");
            SliceOne("Red", "tileRed_01.png", "RedCube");
            SliceOne("Yellow", "tileYellow_01.png", "YellowCube");
            SliceOne("Pink", "tilePink_01.png", "PinkCube");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SliceOne(string color, string subTextureName, string spriteName)
        {
            string pngPath = $"{SpriteFolder}/spritesheet_tiles{color}.png";
            string xmlPath = $"{SpriteFolder}/spritesheet_tiles{color}.xml";

            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (importer == null || texture == null)
            {
                Debug.LogError($"KenneySpriteSlicer: texture not found at {pngPath}");
                return;
            }

            XElement subTexture = XDocument.Load(xmlPath).Root
                ?.Elements("SubTexture")
                .FirstOrDefault(e => (string)e.Attribute("name") == subTextureName);

            if (subTexture == null)
            {
                Debug.LogError($"KenneySpriteSlicer: '{subTextureName}' not found in {xmlPath}");
                return;
            }

            float xmlX = (float)subTexture.Attribute("x");
            float xmlY = (float)subTexture.Attribute("y");
            float width = (float)subTexture.Attribute("width");
            float height = (float)subTexture.Attribute("height");

            // The XML measures y from the top of the sheet; Unity sprite rects measure from the bottom.
            float unityY = texture.height - xmlY - height;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = height; // sprite renders as exactly one 1x1 grid cell
            importer.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = spriteName,
                    rect = new Rect(xmlX, unityY, width, height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                }
            };
            importer.SaveAndReimport();

            Debug.Log($"KenneySpriteSlicer: sliced '{spriteName}' from {pngPath}");
        }
    }
}
