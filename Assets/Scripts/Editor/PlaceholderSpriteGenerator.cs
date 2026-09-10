using System.IO;
using UnityEditor;
using UnityEngine;

namespace Match2.EditorTools
{
    /// <summary>
    /// Generates simple procedural power-up icons to use until real art is
    /// sourced — the imported Kenney "Puzzle Pack 2" has no rocket/bomb art
    /// (it's a tile-connector pack), so this fills the gap with something
    /// legible that's easy to swap out later.
    /// </summary>
    public static class PlaceholderSpriteGenerator
    {
        private const string OutputFolder = "Assets/Art/Sprites/Generated";
        private const int Width = 256;
        private const int Height = 128;

        private const float BodyLeft = 0.18f;
        private const float BodyRight = 0.62f;
        private const float BodyTop = 0.34f;
        private const float BodyBottom = 0.66f;
        private const float FinLeft = 0.06f;
        private const float FinSpread = 0.16f;

        [MenuItem("Match2/Generate Rocket Placeholder Sprite")]
        public static void GenerateRocketSprite()
        {
            Directory.CreateDirectory(OutputFolder);

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float nx = x / (float)Width;
                    float ny = y / (float)Height;
                    texture.SetPixel(x, y, ClassifyPixel(nx, ny));
                }
            }

            texture.Apply();

            string pngPath = $"{OutputFolder}/RocketPlaceholder.png";
            File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Height;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Debug.Log($"PlaceholderSpriteGenerator: generated {pngPath}");
        }

        [MenuItem("Match2/Generate Bomb Placeholder Sprite")]
        public static void GenerateBombSprite()
        {
            Directory.CreateDirectory(OutputFolder);

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size;
                    float ny = y / (float)size;
                    texture.SetPixel(x, y, ClassifyBombPixel(nx, ny));
                }
            }

            texture.Apply();

            string pngPath = $"{OutputFolder}/BombPlaceholder.png";
            File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Debug.Log($"PlaceholderSpriteGenerator: generated {pngPath}");
        }

        [MenuItem("Match2/Generate Ball Placeholder Sprite")]
        public static void GenerateBallSprite()
        {
            Directory.CreateDirectory(OutputFolder);

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size;
                    float ny = y / (float)size;
                    texture.SetPixel(x, y, ClassifyBallPixel(nx, ny));
                }
            }

            texture.Apply();

            string pngPath = $"{OutputFolder}/BallPlaceholder.png";
            File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Debug.Log($"PlaceholderSpriteGenerator: generated {pngPath}");
        }

        /// <summary>
        /// A neutral-grey shiny orb, deliberately left uncolored — <c>GridView</c>
        /// tints it to the Ball's target color at runtime via <c>SpriteRenderer.color</c>,
        /// so the base texture must stay lighter than white can't go (the body is
        /// grey, not white) for the highlight to read as brighter after tinting,
        /// and the outline stays near-black so it stays dark under any tint.
        /// </summary>
        private static Color ClassifyBallPixel(float nx, float ny)
        {
            var body = new Color(0.75f, 0.75f, 0.78f);
            var highlight = Color.white;
            var outline = new Color(0.05f, 0.05f, 0.07f);
            var clear = new Color(0f, 0f, 0f, 0f);

            var point = new Vector2(nx, ny);
            float bodyDist = Vector2.Distance(point, new Vector2(0.5f, 0.5f));
            float highlightDist = Vector2.Distance(point, new Vector2(0.37f, 0.63f));

            bool inOutline = bodyDist <= 0.44f && bodyDist > 0.38f;
            bool inBody = bodyDist <= 0.38f;
            bool inHighlight = inBody && highlightDist <= 0.11f;

            if (inHighlight)
                return highlight;
            if (inBody)
                return body;
            if (inOutline)
                return outline;

            return clear;
        }

        private static Color ClassifyBombPixel(float nx, float ny)
        {
            var bodyDark = new Color(0.16f, 0.17f, 0.20f);
            var highlight = new Color(0.34f, 0.36f, 0.40f);
            var fuseColor = new Color(0.55f, 0.38f, 0.22f);
            var sparkColor = new Color(0.98f, 0.75f, 0.15f);
            var clear = new Color(0f, 0f, 0f, 0f);

            var point = new Vector2(nx, ny);
            float bodyDist = Vector2.Distance(point, new Vector2(0.5f, 0.46f));
            float highlightDist = Vector2.Distance(point, new Vector2(0.40f, 0.36f));
            float sparkDist = Vector2.Distance(point, new Vector2(0.56f, 0.05f));

            bool inFuse = nx >= 0.52f && nx <= 0.58f && ny >= 0.04f && ny <= 0.14f;
            bool inBody = bodyDist <= 0.34f;
            bool inHighlight = inBody && highlightDist <= 0.09f;

            if (sparkDist <= 0.045f)
                return sparkColor;
            if (inFuse)
                return fuseColor;
            if (inHighlight)
                return highlight;
            if (inBody)
                return bodyDark;

            return clear;
        }

        private static Color ClassifyPixel(float nx, float ny)
        {
            var body = new Color(0.90f, 0.90f, 0.94f);
            var outline = new Color(0.55f, 0.57f, 0.62f);
            var tip = new Color(0.88f, 0.24f, 0.24f);
            var fin = new Color(0.30f, 0.52f, 0.86f);
            var clear = new Color(0f, 0f, 0f, 0f);

            bool inBody = nx >= BodyLeft && nx <= BodyRight && ny >= BodyTop && ny <= BodyBottom;

            bool inNose = false;
            if (nx > BodyRight && nx <= 1f)
            {
                float t = (nx - BodyRight) / (1f - BodyRight);
                float halfHeight = Mathf.Lerp((BodyBottom - BodyTop) / 2f, 0f, t);
                inNose = ny >= 0.5f - halfHeight && ny <= 0.5f + halfHeight;
            }

            bool inUpperFin = false;
            bool inLowerFin = false;
            if (nx >= FinLeft && nx < BodyLeft)
            {
                float t = (nx - FinLeft) / (BodyLeft - FinLeft); // 0 at the tail tip, 1 at the body
                float upperEdge = Mathf.Lerp(BodyTop, BodyTop - FinSpread, 1f - t);
                float lowerEdge = Mathf.Lerp(BodyBottom, BodyBottom + FinSpread, 1f - t);
                inUpperFin = ny >= upperEdge && ny <= BodyTop;
                inLowerFin = ny <= lowerEdge && ny >= BodyBottom;
            }

            if (inNose)
                return tip;
            if (inUpperFin || inLowerFin)
                return fin;
            if (inBody)
                return body;

            const float outlineMargin = 0.015f;
            bool nearBody = nx >= BodyLeft - outlineMargin && nx <= BodyRight + outlineMargin &&
                            ny >= BodyTop - outlineMargin && ny <= BodyBottom + outlineMargin;
            if (nearBody)
                return outline;

            return clear;
        }
    }
}
