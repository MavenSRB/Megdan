using System;
using GuardsDigital.Core.Cards;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.30h. The one builder every spell face and spell back goes through - the spell
    /// choice, the SPELLBOOK drawer and the drawer's hover reveal - so one spell cannot look like two.
    ///
    /// <para>A prepared private front (<see cref="ConfiguredSpellArtwork"/>) is the whole visible face
    /// at its verified 372:260 landscape ratio; a clean checkout, which has none, draws the generated
    /// face from the definition's typed regions instead (name, Tier, primary type and values, printed
    /// words) in the same box. Neither path reads a picture or parses a word.</para>
    /// </summary>
    public static class SpellCardFaceView
    {
        /// <summary>The wash laid over a greyed face: the printed colours read as removed, not as lit.</summary>
        public static readonly Color GreyedWash = new Color(0.42f, 0.44f, 0.46f, 0.62f);

        /// <summary>
        /// A complete spell face at <paramref name="scale"/> of <see cref="SpellCardFootprint"/>, named
        /// <c>gd-spell-face</c>, pointer-transparent throughout (the control it is drawn in takes the
        /// pointer) and carrying the spell's definition reference as its <c>userData</c>.
        /// </summary>
        public static VisualElement Build(SpellCardViewModel spell, float scale = 1f)
        {
            if (spell == null) throw new ArgumentNullException(nameof(spell));
            var surface = Surface("gd-spell-face", scale);
            surface.userData = spell.DefinitionReference;
            surface.tooltip = spell.AssistiveLabel;
            var content = Clip(surface, scale);
            if (ConfiguredSpellArtwork.TryLoadFront(spell.DefinitionReference, out var texture))
            {
                content.Add(Artwork("gd-spell-artwork", texture, spell.IsGreyed));
            }
            else
            {
                content.Add(GeneratedFace(spell, scale));
            }

            if (spell.IsGreyed)
            {
                var wash = new VisualElement { name = "gd-spell-greyed", pickingMode = PickingMode.Ignore };
                Fill(wash);
                wash.style.backgroundColor = GreyedWash;
                content.Add(wash);
            }

            DirectorHandsUnityView.IgnorePickingRecursively(surface);
            return surface;
        }

        /// <summary>
        /// One facedown spell back at <paramref name="scale"/>, named <c>gd-spell-back</c>: the prepared
        /// back for its Tier, or a generated back naming the Tier. It names no spell, because there is
        /// none to name.
        /// </summary>
        public static VisualElement BuildBack(CardTier back, float scale = 1f)
        {
            var surface = Surface("gd-spell-back", scale);
            surface.userData = back;
            var content = Clip(surface, scale);
            if (ConfiguredSpellArtwork.TryLoadBack(back, out var texture))
            {
                content.Add(Artwork("gd-spell-back-artwork", texture, false));
            }
            else
            {
                var generated = new VisualElement { name = "gd-spell-back-generated", pickingMode = PickingMode.Ignore };
                Fill(generated);
                generated.style.backgroundColor = back == CardTier.IV
                    ? MatchHudTheme.UltimateFace
                    : new Color(0.46f, 0.12f, 0.12f, 1f);
                generated.style.alignItems = Align.Center;
                generated.style.justifyContent = Justify.Center;
                HudElements.Border(generated, MatchHudTheme.CoinFace, 3f * scale, 8f * scale);
                var tier = HudElements.Text(
                    back == CardTier.IV ? "IV - WISH" : back.ToString(),
                    Mathf.Max(9f, 34f * scale), MatchHudTheme.CoinFace, FontStyle.Bold);
                tier.style.unityTextAlign = TextAnchor.MiddleCenter;
                generated.Add(tier);
                content.Add(generated);
            }

            DirectorHandsUnityView.IgnorePickingRecursively(surface);
            return surface;
        }

        private static VisualElement Surface(string name, float scale)
        {
            var surface = new VisualElement { name = name, pickingMode = PickingMode.Ignore, focusable = false };
            surface.style.flexDirection = FlexDirection.Column;
            surface.style.overflow = Overflow.Visible;
            surface.style.backgroundColor = Color.clear;
            HudElements.Padding(surface, 0f);
            SpellCardFootprint.Apply(surface, scale);
            var outline = Mathf.Max(1f, CardFootprint.ExteriorOutlineWidth * scale);
            ConfiguredCardFaceView.AddExteriorOutline(
                surface, outline,
                ConfiguredCardFaceView.CornerRadius(SpellCardFootprint.Width * scale) + outline);
            return surface;
        }

        private static VisualElement Clip(VisualElement surface, float scale)
        {
            var content = new VisualElement { name = "gd-spell-content-clip", pickingMode = PickingMode.Ignore };
            Fill(content);
            content.style.overflow = Overflow.Hidden;
            content.style.flexDirection = FlexDirection.Column;
            HudElements.Border(content, Color.clear, 0f,
                ConfiguredCardFaceView.CornerRadius(SpellCardFootprint.Width * scale));
            surface.Add(content);
            return content;
        }

        private static Image Artwork(string name, Texture2D texture, bool greyed)
        {
            var image = new Image
            {
                name = name,
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                // A greyed face is darkened as well as washed, so it never reads as the lit choice.
                tintColor = greyed ? new Color(0.72f, 0.72f, 0.72f, 1f) : Color.white,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            image.style.width = Length.Percent(100f);
            image.style.height = Length.Percent(100f);
            return image;
        }

        /// <summary>
        /// The clean-checkout face: the spell's name under its Tier, its primary type and values, and
        /// its printed words, on the spell's own printed colour - every region typed, none parsed.
        /// </summary>
        private static VisualElement GeneratedFace(SpellCardViewModel spell, float scale)
        {
            var ink = MatchHudTheme.FaceText(spell.Color);
            var face = new VisualElement { name = "gd-spell-generated", pickingMode = PickingMode.Ignore };
            Fill(face);
            face.style.backgroundColor = MatchHudTheme.Face(spell.Color);
            face.style.flexDirection = FlexDirection.Column;
            HudElements.Padding(face, 6f * scale);
            HudElements.Border(face, MatchHudTheme.PanelBorder, Mathf.Max(1f, 2f * scale), 8f * scale);

            var band = new VisualElement { name = "gd-spell-name-band", pickingMode = PickingMode.Ignore };
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.Center;
            band.style.backgroundColor = MatchHudTheme.FaceBand(spell.Color);
            HudElements.Padding(band, 4f * scale);
            var tier = HudElements.Text(spell.TierLabel, Mathf.Max(7f, 13f * scale), ink, FontStyle.Bold);
            tier.name = "gd-spell-tier";
            tier.style.marginRight = 8f * scale;
            var name = HudElements.Text(spell.DisplayName, Mathf.Max(8f, 17f * scale), ink, FontStyle.Bold);
            name.name = "gd-spell-name";
            name.style.flexGrow = 1f;
            name.style.flexShrink = 1f;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            band.Add(tier);
            band.Add(name);
            face.Add(band);

            var action = HudElements.Text(
                spell.Face.PrimaryActionLabel +
                (spell.Face.PrimaryValue == null ? string.Empty : "  " + spell.Face.PrimaryValue.Label) +
                (spell.Face.SpatialValue == null ? string.Empty : "  " + spell.Face.SpatialValue.Label),
                Mathf.Max(7f, 13f * scale), ink, FontStyle.Bold);
            action.name = "gd-spell-primary";
            action.style.unityTextAlign = TextAnchor.MiddleCenter;
            action.style.marginTop = 6f * scale;
            face.Add(action);

            var words = HudElements.Text(
                string.IsNullOrEmpty(spell.Face.PrintedText) ? spell.Face.EmptyTextLabel : spell.Face.PrintedText,
                Mathf.Max(7f, 13f * scale), ink);
            words.name = "gd-spell-text";
            words.style.unityTextAlign = TextAnchor.MiddleCenter;
            words.style.marginTop = 6f * scale;
            words.style.flexShrink = 1f;
            words.style.whiteSpace = WhiteSpace.Normal;
            face.Add(words);
            return face;
        }

        private static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }
    }
}
