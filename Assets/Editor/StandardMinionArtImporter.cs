using UnityEditor;

namespace GuardsDigital.Editor
{
    public sealed class StandardMinionArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Minions/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
