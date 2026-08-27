using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Resolves optional prepared card artwork without making it authority or a runtime requirement.
    /// </summary>
    public static class ConfiguredCardArtwork
    {
        private static readonly HashSet<string> ReportedDiagnostics =
            new HashSet<string>(StringComparer.Ordinal);

        private static Func<string, Texture2D> textureLoaderOverride;

        /// <summary>
        /// Installs a bounded texture provider. Tests use this to prove both artwork and clean-checkout
        /// paths without depending on private files; production uses Resources when no scope is active.
        /// </summary>
        public static IDisposable UseTextureLoader(Func<string, Texture2D> textureLoader)
        {
            if (textureLoader == null) throw new ArgumentNullException(nameof(textureLoader));
            var previous = textureLoaderOverride;
            textureLoaderOverride = textureLoader;
            return new TextureLoaderScope(previous);
        }

        public static bool TryGetResourcePath(
            CardDefinitionReference reference,
            out string resourcePath)
        {
            resourcePath = string.Empty;
            try
            {
                if (!reference.IsDefined ||
                    !ConfiguredCardArtworkCatalog.Catalog.TryGet(reference, out var entry))
                {
                    ReportOnce(
                        $"mapping:{reference}",
                        $"Configured card artwork has no catalog mapping for '{reference}'; " +
                        "the generated face is being used.");
                    return false;
                }

                resourcePath = entry.ResourcePath;
                return true;
            }
            catch (Exception exception)
            {
                ReportOnce(
                    $"catalog:{reference}:{exception.GetType().Name}",
                    $"Configured card artwork could not resolve '{reference}' because the tracked " +
                    $"catalog is invalid ({exception.GetType().Name}); the generated face is being used.");
                return false;
            }
        }

        public static bool TryLoadTexture(
            CardDefinitionReference reference,
            out Texture2D texture,
            out string resourcePath)
        {
            texture = null;
            if (!TryGetResourcePath(reference, out resourcePath))
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
                    $"load:{reference}:{resourcePath}:{exception.GetType().Name}",
                    $"Configured card artwork could not load '{reference}' from prepared resource " +
                    $"'{resourcePath}' ({exception.GetType().Name}); the generated face is being used.");
                texture = null;
                return false;
            }

            if (texture != null) return true;

            ReportOnce(
                $"missing:{reference}:{resourcePath}",
                $"Configured card artwork found '{reference}' but prepared resource '{resourcePath}' " +
                "is absent; the generated face is being used.");
            return false;
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

    /// <summary>
    /// The one complete-card builder. It draws prepared verified artwork when present and otherwise
    /// leaves the existing generated face visible, at the same fixed footprint and with the same state
    /// treatments in either case.
    /// </summary>
    public static class ConfiguredCardFaceView
    {
        public static VisualElement Build(VisualElement surface, TrayCardViewModel card)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (card == null) throw new ArgumentNullException(nameof(card));

            CardFootprint.Apply(surface);
            surface.userData = card.DefinitionReference;
            surface.style.flexDirection = FlexDirection.Column;
            surface.style.alignItems = Align.Stretch;
            surface.style.justifyContent = Justify.FlexStart;
            surface.style.backgroundColor = MatchHudTheme.Face(card.Color);
            surface.style.overflow = Overflow.Hidden;
            HudElements.Padding(surface, 0f);
            HudElements.Border(surface, MatchHudTheme.PanelBorder, 3f, 8f);

            var frame = DirectorHandsUnityView.BuildGeneratedCardFrame(card);
            if (ConfiguredCardArtwork.TryLoadTexture(
                    card.DefinitionReference,
                    out var texture,
                    out _))
            {
                ApplyArtwork(frame, card, texture);
            }

            surface.Add(frame);
            return surface;
        }

        private static void ApplyArtwork(
            VisualElement generatedFrame,
            TrayCardViewModel card,
            Texture2D texture)
        {
            // Keep the generated construction in this same builder as the per-card fallback and as the
            // assistive typed reading, but remove its four printed regions from layout while artwork is
            // available. The lifecycle band stays below the aspect-contained image, in the vertical
            // letterbox space the fixed 186x420 footprint leaves around a 372x520 source, so no printed
            // value or sentence is covered by a state label.
            foreach (var child in generatedFrame.Children()
                         .Where(element => element.name != "gd-card-history")
                         .ToArray())
            {
                child.style.display = DisplayStyle.None;
            }

            generatedFrame.style.justifyContent = Justify.FlexEnd;
            var artwork = new VisualElement
            {
                name = "gd-card-artwork",
                pickingMode = PickingMode.Ignore,
                focusable = false,
                tooltip = card.AssistiveLabel,
                userData = card.DefinitionReference
            };
            artwork.style.position = Position.Absolute;
            artwork.style.left = 0f;
            artwork.style.top = 0f;
            artwork.style.right = 0f;
            artwork.style.bottom = 0f;
            artwork.style.backgroundImage = new StyleBackground(texture);
            artwork.style.backgroundSize =
                new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            artwork.style.backgroundPositionX =
                new StyleBackgroundPosition(
                    new BackgroundPosition(BackgroundPositionKeyword.Center));
            artwork.style.backgroundPositionY =
                new StyleBackgroundPosition(
                    new BackgroundPosition(BackgroundPositionKeyword.Center));
            generatedFrame.Insert(0, artwork);
        }
    }
}
