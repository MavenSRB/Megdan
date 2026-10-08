using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestones 9.30k and 9.74. The pointer-rest companion for an action card that declares typed
    /// spell casts. The own-hand companion stacks above its face; read-only inspection places the
    /// same column beside the face. The casting card remains the pointer anchor in both layouts.
    /// </summary>
    internal static class CastableSpellHoverView
    {
        internal const float FaceScale = 0.70f;
        internal const float Gap = 7f;
        private const float Padding = 7f;
        private const float FaceGap = 5f;

        internal static float WidthFor(IReadOnlyList<SpellCardViewModel> spells) =>
            spells == null || spells.Count == 0
                ? 0f
                : Padding * 2f + SpellCardFootprint.Width * FaceScale;

        internal static float HeightFor(IReadOnlyList<SpellCardViewModel> spells) =>
            spells == null || spells.Count == 0
                ? 0f
                : Padding * 2f +
                    spells.Count * SpellCardFootprint.Height * FaceScale +
                    Math.Max(0, spells.Count - 1) * FaceGap;

        internal static void Attach(
            VisualElement cardFace,
            IReadOnlyList<SpellCardViewModel> spells)
        {
            Detach(cardFace);
            if (cardFace == null || spells == null || spells.Count == 0)
            {
                return;
            }

            var width = WidthFor(spells);
            var height = HeightFor(spells);
            var shelf = new VisualElement
            {
                name = "gd-castable-spells",
                pickingMode = PickingMode.Ignore,
                tooltip = "Spells this card can cast: " + string.Join(", ",
                    spells.Select(spell => spell.Caption))
            };
            shelf.style.position = Position.Absolute;
            shelf.style.left = (CardFootprint.Width - width) / 2f;
            shelf.style.top = -height - Gap;
            shelf.style.width = width;
            shelf.style.height = height;
            shelf.style.flexDirection = FlexDirection.Column;
            shelf.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.Padding(shelf, Padding);
            HudElements.Border(shelf, MatchHudTheme.PanelBorder, 1f, 7f);

            var row = new VisualElement { name = "gd-castable-spells-row", pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.Center;
            for (var index = 0; index < spells.Count; index++)
            {
                var spell = spells[index];
                var host = new VisualElement { name = "gd-castable-spell", pickingMode = PickingMode.Ignore, tooltip = spell.AssistiveLabel };
                host.style.width = SpellCardFootprint.Width * FaceScale;
                host.style.marginBottom = index + 1 < spells.Count ? FaceGap : 0f;
                host.Add(SpellCardFaceView.Build(spell, FaceScale));
                row.Add(host);
            }

            shelf.Add(row);
            cardFace.style.overflow = Overflow.Visible;
            cardFace.Add(shelf);
            DirectorHandsUnityView.IgnorePickingRecursively(shelf);
        }

        internal static void Detach(VisualElement cardFace)
        {
            cardFace?.Q<VisualElement>("gd-castable-spells")?.RemoveFromHierarchy();
        }

        internal static void PlaceBeside(VisualElement cardFace)
        {
            var shelf = cardFace?.Q<VisualElement>("gd-castable-spells");
            if (shelf == null) return;
            shelf.style.left = CardFootprint.Width + Gap;
            shelf.style.top = 0f;
        }

    }
}
