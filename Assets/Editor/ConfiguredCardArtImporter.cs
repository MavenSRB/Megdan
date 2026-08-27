using System;
using UnityEditor;

namespace GuardsDigital.Editor
{
    /// <summary>
    /// Keeps locally prepared private card fronts at their verified source dimensions. Unity's
    /// default NPOT conversion can otherwise turn a 372-by-520 front into a power-of-two texture and
    /// change the proportions the UI receives.
    /// </summary>
    public sealed class ConfiguredCardArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Private/Cards/";

        public override uint GetVersion() => 1u;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
        }
    }
}
