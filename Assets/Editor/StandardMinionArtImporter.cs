using UnityEditor;

namespace GuardsDigital.Editor
{
    public sealed class StandardMinionArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Minions/";
        private const string StandeeFolder = "Assets/Resources/Private/Megdan/MinionStandees/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(StandeeFolder, System.StringComparison.Ordinal))
            {
                // Veljko's resized sources are intentionally non-power-of-two crops. Unity's
                // default ToNearest import would stretch 1021x883 to 1024x1024 and 359x398 to
                // 256x512, distorting the figure before the silhouette mesh applies its aspect.
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true;
                return;
            }

            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
