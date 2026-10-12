using System;
using UnityEditor;

namespace GuardsDigital.Editor
{
    /// <summary>
    /// Keeps locally prepared private card fronts at their verified source dimensions. Unity's
    /// default NPOT conversion can otherwise turn a 372-by-520 front into a power-of-two texture and
    /// change the proportions the UI receives.
    ///
    /// <para>Milestone 9.20 adds the prepared turn-slot markers: Snorri's 27-by-22 rune tiles were
    /// imported at 32 by 16, so every rune was drawn squashed to about two thirds of its printed
    /// height.</para>
    ///
    /// <para>Milestone 9.30h1 adds the prepared spell fronts and backs: a 372-by-260 spell was
    /// imported at a power-of-two size and every spell face was drawn squeezed (the user: "scaling is
    /// a bit off for those spells").</para>
    ///
    /// <para>Milestone 9.32h adds the prepared card backs (a withheld facedown gold's hover shows its
    /// owner's 372-by-520 back).</para>
    /// </summary>
    public sealed class ConfiguredCardArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Private/Cards/";

        public override uint GetVersion() => 5u;

        private void OnPreprocessTexture()
        {
            if ((!assetPath.StartsWith(ArtFolder, StringComparison.Ordinal) &&
                 !assetPath.StartsWith("Assets/Resources/Private/PveCards/", StringComparison.Ordinal) &&
                 !assetPath.StartsWith("Assets/Resources/Private/HeroSelection/", StringComparison.Ordinal) &&
                 !assetPath.StartsWith("Assets/Resources/Private/Markers/", StringComparison.Ordinal) &&
                 !assetPath.StartsWith("Assets/Resources/Private/Spells/", StringComparison.Ordinal) &&
                 !assetPath.StartsWith("Assets/Resources/Private/CardBacks/", StringComparison.Ordinal)) ||
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
