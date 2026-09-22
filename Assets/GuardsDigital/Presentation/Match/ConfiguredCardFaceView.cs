using System;
using System.Collections.Generic;
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
    /// The one complete-card builder. A prepared verified front is the whole visible card at its
    /// source aspect; when it is unavailable, the readable generated face remains the per-card
    /// fallback.
    /// </summary>
    public static class ConfiguredCardFaceView
    {
        public static VisualElement Build(VisualElement surface, TrayCardViewModel card)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (card == null) throw new ArgumentNullException(nameof(card));

            surface.userData = card.DefinitionReference;
            surface.style.flexDirection = FlexDirection.Column;
            surface.style.alignItems = Align.Stretch;
            surface.style.justifyContent = Justify.FlexStart;
            surface.style.overflow = Overflow.Visible;
            surface.style.backgroundColor = Color.clear;
            HudElements.Padding(surface, 0f);
            HudElements.Border(surface, Color.clear, 0f, 0f);
            CardFootprint.Apply(surface);

            AddExteriorOutline(
                surface,
                CardFootprint.ExteriorOutlineWidth,
                CornerRadius(CardFootprint.Width) + CardFootprint.ExteriorOutlineWidth);

            // The perimeter must be allowed to paint beyond the unchanged content rectangle, while
            // the printed scan must still clip to its rounded corners. A dedicated inner clip gives
            // those two layers independent overflow contracts.
            var content = new VisualElement
            {
                name = "gd-card-content-clip",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            content.style.position = Position.Absolute;
            content.style.left = 0f;
            content.style.top = 0f;
            content.style.right = 0f;
            content.style.bottom = 0f;
            content.style.flexDirection = FlexDirection.Column;
            content.style.alignItems = Align.Stretch;
            content.style.justifyContent = Justify.FlexStart;
            content.style.overflow = Overflow.Hidden;
            HudElements.Padding(content, 0f);
            HudElements.Border(content, Color.clear, 0f, CornerRadius(CardFootprint.Width));
            surface.Add(content);

            if (ConfiguredCardArtwork.TryLoadTexture(
                    card.DefinitionReference,
                    out var texture,
                    out _))
            {
                BuildArtwork(content, card, texture);
                if (card.IsSelected)
                {
                    AddSelectionOutline(surface);
                }
            }
            else
            {
                BuildGeneratedFallback(content, card);
                HudElements.MarkSelectedWithoutResizing(surface, card.IsSelected);
            }

            // Milestone 8.35m (the SEVENTH amendment's card scope and resolution focus). Added here,
            // the one shared builder every card face goes through - the hand tray's own cards, a
            // Defense/effect-choice/removal-prevention/leveling offer, and a hover reveal alike - so a
            // card cannot shine on one surface and not another the way two independent readings could
            // drift apart. The glow sits outside the black exterior perimeter and the cyan selection
            // outline as its own concentric ring, so a card a player has already selected and a card
            // that is the awaited answer both stay visible at once rather than one treatment replacing
            // the other.
            if (card.IsAwaitedAnswer)
            {
                AddAwaitedShineOutline(surface);
            }

            // Milestone 8.75e. NOTHING is drawn here for a card whose printed text the Tie Breaker
            // coin selects. Milestone 8.75b put a `SUN` / `MOON` corner badge on every such face; the
            // user, reading her cards in the first live playtest of the twenty-two-hero build, asked
            // what it was and then settled it - "Drop the badge and rely on the coin strip alone".
            // The coin is on screen at all times in the rail's own tie-breaker banner, and that banner
            // IS the reading: one thing saying which face is up, rather than the same fact repeated in
            // the corner of every card in the hand. So every card of every hero goes through this
            // builder and comes out exactly as it did before 8.75b - the declaration
            // (`CardDefinition.PrintedTextSelection`) and the mapping (`CoinSelectedTextQuery`) stay,
            // because the content branches on them and the decision window's face choice names them,
            // but no card surface reads either one.

            // The rest of the hand while the viewer's own card resolves - never the resolving card
            // itself (excluded by construction, see `TrayCardViewModel.IsDimmedByResolution`) and never
            // an offered candidate on a different surface, which is what keeps a mid-resolution Defense
            // choice shining rather than dimmed without this method needing to ask which surface it is
            // drawing for.
            if (card.IsDimmedByResolution)
            {
                surface.style.opacity = MatchHudTheme.DimmedByResolutionOpacity;
            }

            return surface;
        }

        /// <summary>
        /// The SEVENTH amendment's gold "shine", the same warm family the top bar and every hero tray
        /// wear while the match waits on the viewer - <see cref="MatchHudTheme.ShineGlow"/> - drawn as
        /// its own ring outside <see cref="AddExteriorOutline"/>'s black perimeter, wide enough that it
        /// never competes with <see cref="AddSelectionOutline"/> sitting at the content edge.
        /// </summary>
        private static void AddAwaitedShineOutline(VisualElement surface)
        {
            var shine = new VisualElement
            {
                name = "gd-card-awaited-shine",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            // Milestone 8.35z9. The shared marker, so the circling sweep can find this ring without
            // knowing it is a card - see MatchHudTheme.AwaitedShineClass for why a class and not a name.
            shine.AddToClassList(MatchHudTheme.AwaitedShineClass);
            var inset = -(CardFootprint.ExteriorOutlineWidth + 3f);
            shine.style.position = Position.Absolute;
            shine.style.left = inset;
            shine.style.top = inset;
            shine.style.right = inset;
            shine.style.bottom = inset;
            shine.style.backgroundColor = Color.clear;
            // Milestone 8.35z47. THE BAND FILLS THE SPACE IT ALREADY RESERVED, and that is
            // the whole change: the ring's box has always stood `-inset` units clear of the card
            // surface, and it drew a 3-unit line inside that space and left the rest empty.
            //
            // A reference unit is one pixel at 2560x1440, so that line was 2.25 real pixels at
            // 1920x1080 and 1.5 in a typical Editor Game View. The breath moves its brightness by
            // 2.22x - correctly, and measurably - but a 2.22x swing carried on one-and-a-half pixels
            // is not a light a player can see, which is exactly what the user reported looking at:
            // "Saw no breathing, just solid colors." Nothing about the clock or the curve was wrong.
            //
            // Drawing the band at the full width of its own reserve costs no geometry: the element is
            // absolutely positioned and its box does not move, so the hand's overlap, the card's own
            // footprint and every bound measured on either are untouched. Only the number of pixels
            // the breath is written across changes.
            HudElements.Border(
                shine,
                MatchHudTheme.ShineGlow,
                -inset,
                CornerRadius(CardFootprint.Width) + CardFootprint.ExteriorOutlineWidth + 3f);
            surface.Add(shine);
        }

        private static void AddSelectionOutline(VisualElement surface)
        {
            var selection = new VisualElement
            {
                name = "gd-card-selection-outline",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            selection.style.position = Position.Absolute;
            selection.style.left = 0f;
            selection.style.top = 0f;
            selection.style.right = 0f;
            selection.style.bottom = 0f;
            HudElements.Border(
                selection, MatchHudTheme.SelectedOutline, 3f,
                CornerRadius(CardFootprint.ArtworkWidth));
            surface.Add(selection);
        }

        /// <summary>
        /// Adds the one pointer-transparent perimeter used by complete faces and scaled compact card
        /// renderings. The negative inset keeps the entire line outside the existing content box.
        /// </summary>
        internal static VisualElement AddExteriorOutline(
            VisualElement surface,
            float width,
            float radius)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            var outline = new VisualElement
            {
                name = "gd-card-exterior-outline",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            outline.style.position = Position.Absolute;
            outline.style.left = -width;
            outline.style.top = -width;
            outline.style.right = -width;
            outline.style.bottom = -width;
            outline.style.backgroundColor = Color.clear;
            HudElements.Border(outline, Color.black, width, radius);
            surface.Add(outline);
            outline.SendToBack();
            return outline;
        }

        /// <summary>
        /// The printed cards have rounded corners; the scans bake them over a dark square. The
        /// face clips itself to the printed radius (the user's correction), proportional to the
        /// source corner arc - about 4.8% of the card's width.
        /// </summary>
        public static float CornerRadius(float width) => width * 0.048f;

        private static void BuildArtwork(
            VisualElement surface,
            TrayCardViewModel card,
            Texture2D texture)
        {
            CardFootprint.ApplyArtwork(surface);
            surface.style.backgroundColor = Color.clear;
            HudElements.Border(surface, Color.clear, 0f, CornerRadius(CardFootprint.ArtworkWidth));

            var artwork = new Image
            {
                name = "gd-card-artwork",
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                tintColor = Color.white,
                pickingMode = PickingMode.Ignore,
                focusable = false,
                tooltip = card.AssistiveLabel,
                userData = card.DefinitionReference
            };
            artwork.style.width = Length.Percent(100f);
            artwork.style.height = Length.Percent(100f);
            artwork.style.minWidth = Length.Percent(100f);
            artwork.style.minHeight = Length.Percent(100f);
            artwork.style.maxWidth = Length.Percent(100f);
            artwork.style.maxHeight = Length.Percent(100f);
            artwork.style.flexGrow = 0f;
            artwork.style.flexShrink = 0f;
            surface.Add(artwork);

        }

        private static void BuildGeneratedFallback(
            VisualElement surface,
            TrayCardViewModel card)
        {
            CardFootprint.Apply(surface);
            surface.style.backgroundColor = MatchHudTheme.Face(card.Color);
            HudElements.Border(surface, MatchHudTheme.PanelBorder, 3f, 8f);
            surface.Add(DirectorHandsUnityView.BuildGeneratedCardFrame(card));
        }
    }
}
