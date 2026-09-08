using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Which printed badge of a card face a shine overlay lights. Milestone 8.30d: positions are
    /// baked into the prepared per-card overlay, so a slot is a meaning, never a rectangle.
    /// </summary>
    public enum CardShineSlot
    {
        None = 0,
        Primary = 1,
        SecondaryDefense = 2,
        SecondaryMovement = 3,

        /// <summary>
        /// The card's own printed lines around its primary section - the type ribbon's box and
        /// the rules-text box - lit as one ring. The user's final 8.30d direction for an offered
        /// primary: no invented lines and no icon shine; the existing print glows. The badge
        /// slot above stays for the Defense prompt, which lights the shield itself.
        /// </summary>
        PrimaryRing = 4
    }

    /// <summary>
    /// Resolves the optional prepared shine overlays milestone 8.30d's Tools/Prepare-ShineSprites.ps1
    /// derives beside the private card art, without making them authority or a runtime requirement.
    ///
    /// <para>An overlay is a full-card transparent image tracing one printed badge's own silhouette
    /// (rim, radiance, halo), so the runtime composites it aligned with the card artwork and never
    /// analyses an image or holds per-card geometry. Like the artwork itself, the overlays are
    /// ignored files: a clean checkout has none, and every caller degrades to the treatment it used
    /// before this milestone.</para>
    /// </summary>
    public static class ConfiguredCardShine
    {
        private static readonly HashSet<string> ReportedDiagnostics =
            new HashSet<string>(StringComparer.Ordinal);

        private static Func<string, Texture2D> textureLoaderOverride;

        /// <summary>
        /// Installs a bounded texture provider, exactly as
        /// <see cref="ConfiguredCardArtwork.UseTextureLoader"/> does for the fronts: tests prove
        /// both the overlay and the clean-checkout paths without depending on private files.
        /// </summary>
        public static IDisposable UseTextureLoader(Func<string, Texture2D> textureLoader)
        {
            if (textureLoader == null) throw new ArgumentNullException(nameof(textureLoader));
            var previous = textureLoaderOverride;
            textureLoaderOverride = textureLoader;
            return new TextureLoaderScope(previous);
        }

        /// <summary>
        /// The resource path a card's shine overlay for <paramref name="slot"/> lives at, derived
        /// from the tracked artwork catalog's own mapping - or false when the card has no catalog
        /// entry at all.
        /// </summary>
        public static bool TryGetResourcePath(
            CardDefinitionReference reference,
            CardShineSlot slot,
            out string resourcePath)
        {
            resourcePath = string.Empty;
            if (slot == CardShineSlot.None ||
                !ConfiguredCardArtwork.TryGetResourcePath(reference, out var artworkPath))
            {
                return false;
            }

            var split = artworkPath.LastIndexOf('/');
            if (split < 0)
            {
                return false;
            }

            resourcePath =
                artworkPath.Substring(0, split) + "/_shine" +
                artworkPath.Substring(split) + "--" + SlotSuffix(slot);
            return true;
        }

        public static bool TryLoadTexture(
            CardDefinitionReference reference,
            CardShineSlot slot,
            out Texture2D texture)
        {
            texture = null;
            if (!TryGetResourcePath(reference, slot, out var resourcePath))
            {
                return false;
            }

            try
            {
                texture = textureLoaderOverride != null
                    ? textureLoaderOverride(resourcePath)
                    : Resources.Load<Texture2D>(resourcePath);
            }
            catch (Exception exception)
            {
                ReportOnce(
                    $"load:{reference}:{slot}:{exception.GetType().Name}",
                    $"Configured card shine could not load '{reference}' slot '{slot}' " +
                    $"({exception.GetType().Name}); the prior offer treatment is being used.");
                texture = null;
                return false;
            }

            if (texture != null) return true;

            ReportOnce(
                $"missing:{reference}:{slot}",
                $"Configured card shine has no prepared overlay for '{reference}' slot '{slot}'; " +
                "the prior offer treatment is being used.");
            return false;
        }

        private static string SlotSuffix(CardShineSlot slot)
        {
            switch (slot)
            {
                case CardShineSlot.Primary: return "primary";
                case CardShineSlot.PrimaryRing: return "primary-ring";
                case CardShineSlot.SecondaryDefense: return "secondary-defense";
                case CardShineSlot.SecondaryMovement: return "secondary-movement";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        /// <summary>Offline-measured print geometry; missing private data keeps the generic fallback.</summary>
        public static bool TryGetPrimaryTitleTop(CardDefinitionReference reference, out float sourceY)
        {
            sourceY = 0f;
            if (!TryGetResourcePath(reference, CardShineSlot.Primary, out var path)) return false;
            var asset = Resources.Load<TextAsset>(path + "-title");
            return asset != null && float.TryParse(asset.text.Trim(),
                System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture,
                out sourceY) && sourceY >= 260f && sourceY <= 450f;
        }

        private static void ReportOnce(string key, string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ReportedDiagnostics.Add(key))
            {
                Debug.Log(message);
            }
#endif
        }

        private sealed class TextureLoaderScope : IDisposable
        {
            private readonly Func<string, Texture2D> previous;
            private bool disposed;

            public TextureLoaderScope(Func<string, Texture2D> previousLoader)
            {
                previous = previousLoader;
            }

            public void Dispose()
            {
                if (disposed) return;
                textureLoaderOverride = previous;
                disposed = true;
            }
        }
    }
}
