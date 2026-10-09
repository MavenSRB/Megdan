using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>Pointer-transparent Shift reading for the modifier already printed on a hero token.</summary>
    public sealed class HeroDefenseModifierBreakdownUnityView
    {
        public const float Width = 252f;
        public const float PointerGap = 16f;
        public const float SafeMargin = 8f;
        private const float RowHeight = 23f;
        private const float TotalDividerHeight = 9f;

        public HeroDefenseModifierBreakdownUnityView()
        {
            Root = HudElements.Panel(RoundActionUnityView.DefensePreviewBackgroundColor);
            Root.name = "gd-hero-defense-modifier-breakdown";
            Root.pickingMode = PickingMode.Ignore;
            Root.style.position = Position.Absolute;
            Root.style.display = DisplayStyle.None;
            Root.style.width = Width;
            Root.style.justifyContent = Justify.Center;
            Root.style.paddingLeft = 10f;
            Root.style.paddingRight = 10f;
            // Live UI Toolkit bounds leave the centred child stack two pixels low with symmetric
            // declared padding. Preserve the same 18-pixel budget while optically centring it.
            Root.style.paddingTop = 7f;
            Root.style.paddingBottom = 11f;
            HudElements.Border(Root, MatchHudTheme.PanelBorder, 1f, 4f);
        }

        public VisualElement Root { get; }
        public bool IsVisible => Root.style.display.value == DisplayStyle.Flex;
        public Rect LastRect { get; private set; }

        public void Show(HeroDefenseModifierProjection model, Vector2 panelPointer, Rect safeRegion)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Root.Clear();
            var title = HudElements.Text("DEFENSE MODIFIER", 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            title.name = "gd-hero-defense-modifier-title";
            title.style.height = 15f;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            title.style.marginBottom = 5f;
            Root.Add(title);

            var displayed = model.Contributions
                .Where(value => value.Kind != ValueContributionKind.ItemModifier)
                .ToArray();
            foreach (var contribution in displayed)
                AddRow(contribution.SourceLabel, contribution.Amount, ContributionRowName(contribution));

            var itemTotal = model.Contributions
                .Where(value => value.Kind == ValueContributionKind.ItemModifier)
                .Sum(value => value.Amount);
            if (itemTotal != 0)
                AddRow("Items", itemTotal, "gd-hero-defense-items-row");

            var divider = new VisualElement { name = "gd-hero-defense-total-divider" };
            divider.style.height = 1f;
            divider.style.marginTop = 4f;
            divider.style.marginBottom = 4f;
            divider.style.backgroundColor = MatchHudTheme.PanelBorder;
            Root.Add(divider);
            AddRow("Total", model.Total, "gd-hero-defense-total-row", true);
            IgnorePicking(Root);

            var height = 7f + 15f + 5f +
                (displayed.Length + (itemTotal != 0 ? 1 : 0) + 1) * RowHeight + TotalDividerHeight + 11f;
            LastRect = CalculateRect(panelPointer, safeRegion, height);
            Root.style.left = LastRect.x;
            Root.style.top = LastRect.y;
            Root.style.height = LastRect.height;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        public void Hide()
        {
            Root.Clear();
            Root.style.display = DisplayStyle.None;
        }

        private static string ContributionRowName(ValueContributionProjection contribution)
        {
            if (contribution.Kind == ValueContributionKind.MinionDefenseModifier ||
                contribution.Kind == ValueContributionKind.ContinuousSourceModifier &&
                contribution.SourceContinuousSourceId.StartsWith("effective-minion-token:",
                    StringComparison.Ordinal))
                return "gd-hero-defense-creep-row";
            if (contribution.Kind == ValueContributionKind.ItemModifier)
                return "gd-hero-defense-items-row";
            if (contribution.Kind == ValueContributionKind.ActiveEffectModifier)
                return "gd-hero-defense-active-effect-row";
            return "gd-hero-defense-card-effect-row";
        }

        private void AddRow(string label, int value, string name, bool bold = false)
        {
            var row = HudElements.Row(8f);
            row.name = name;
            row.style.height = RowHeight;
            row.style.minHeight = RowHeight;
            row.style.flexShrink = 0f;
            row.style.justifyContent = Justify.SpaceBetween;
            var source = HudElements.Text(label, 11f, MatchHudTheme.PrimaryText,
                bold ? FontStyle.Bold : FontStyle.Normal);
            source.style.flexGrow = 1f;
            source.style.height = RowHeight;
            source.style.unityTextAlign = TextAnchor.MiddleLeft;
            source.style.whiteSpace = WhiteSpace.NoWrap;
            source.style.overflow = Overflow.Hidden;
            var color = value >= 0
                ? new Color(0.30f, 0.90f, 0.48f, 1f)
                : new Color(1f, 0.30f, 0.20f, 1f);
            var amount = HudElements.Text(MatchHudTheme.Signed(value), 12f, color, FontStyle.Bold);
            amount.style.flexShrink = 0f;
            amount.style.height = RowHeight;
            amount.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(source);
            row.Add(amount);
            Root.Add(row);
        }

        public static Rect CalculateRect(Vector2 pointer, Rect safeRegion, float height)
        {
            var minimumX = safeRegion.xMin + SafeMargin;
            var maximumX = safeRegion.xMax - SafeMargin - Width;
            var minimumY = safeRegion.yMin + SafeMargin;
            var maximumY = safeRegion.yMax - SafeMargin - height;
            var x = pointer.x < safeRegion.center.x
                ? pointer.x + PointerGap
                : pointer.x - PointerGap - Width;
            x = Mathf.Clamp(x, minimumX, maximumX);
            var y = Mathf.Clamp(pointer.y - height * 0.5f, minimumY, maximumY);
            return new Rect(x, y, Width, height);
        }

        private static void IgnorePicking(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                IgnorePicking(child);
        }
    }
}
