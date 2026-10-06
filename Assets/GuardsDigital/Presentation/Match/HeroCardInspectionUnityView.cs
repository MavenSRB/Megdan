using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>A read-only current-set window. Receives faces, never command or hidden lifecycle data.</summary>
    public sealed class HeroCardInspectionUnityView
    {
        private readonly Func<IMatchReadModel> read;
        private readonly Label title;
        private readonly Label count;
        private readonly DropdownField colors;
        private readonly DropdownField tiers;
        private readonly VisualElement grid;
        private readonly ScrollView scroll;
        private readonly List<CardColor?> colorValues = new List<CardColor?>();
        private static readonly CardTier?[] TierValues =
            { null, CardTier.None, CardTier.I, CardTier.II, CardTier.III, CardTier.IV };
        private HeroCardInspection inspection;
        private HeroId hero;
        private CardColor? color;
        private CardTier? tier;
        public VisualElement Root { get; }
        public bool IsOpen => hero.IsDefined;
        public HeroId Hero => hero;

        public HeroCardInspectionUnityView(Func<IMatchReadModel> read)
        {
            this.read = read;
            Root = HudElements.Panel(MatchHudTheme.PanelBackground);
            Root.name = "gd-hero-card-window";
            Root.style.position = Position.Absolute;
            Root.style.display = DisplayStyle.None;
            Root.style.flexDirection = FlexDirection.Column;
            Root.focusable = true;
            HudElements.Padding(Root, 18f);
            HudElements.Border(Root, MatchHudTheme.PanelBorder, 2f, 12f);
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.flexShrink = 0;
            title = HudElements.Text("CURRENT CARDS", 20f, Color.white, FontStyle.Bold);
            title.style.flexGrow = 1;
            header.Add(title);
            var close = HudElements.Control("CLOSE", Close, 28f);
            close.name = "gd-hero-card-close";
            header.Add(close);
            Root.Add(header);
            var filters = new VisualElement { name = "gd-hero-card-filters" };
            filters.style.flexDirection = FlexDirection.Row;
            filters.style.flexShrink = 0;
            filters.style.marginTop = 10f;
            filters.style.marginBottom = 10f;
            colors = new DropdownField("Color", new List<string> { "All" }, 0)
                { name = "gd-hero-card-color" };
            tiers = new DropdownField("Tier", new List<string> { "All", "Basic", "I", "II", "III", "IV / Ultimate" }, 0)
                { name = "gd-hero-card-tier" };
            foreach (var field in new[] { colors, tiers })
            {
                field.style.width = 260f;
                field.style.marginRight = 14f;
                field.labelElement.style.minWidth = 45f;
                field.labelElement.style.color = Color.white;
                field.style.color = Color.black;
                filters.Add(field);
            }
            count = HudElements.Text(string.Empty, 13f, MatchHudTheme.MutedText);
            count.style.alignSelf = Align.Center;
            filters.Add(count);
            Root.Add(filters);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "gd-hero-card-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            grid = new VisualElement { name = "gd-hero-card-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            HudElements.Padding(grid, 8f);
            scroll.Add(grid);
            Root.Add(scroll);
            colors.RegisterValueChangedCallback(_ =>
            {
                color = colors.index >= 0 && colors.index < colorValues.Count ? colorValues[colors.index] : null;
                DrawCards();
            });
            tiers.RegisterValueChangedCallback(_ =>
            {
                tier = tiers.index >= 0 && tiers.index < TierValues.Length ? TierValues[tiers.index] : null;
                DrawCards();
            });
            Root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape) return;
                Close();
                evt.StopPropagation();
            });
        }

        public bool Open(HeroId id, string displayName)
        {
            var model = Find(id);
            if (model == null) return false;
            hero = id;
            inspection = model;
            color = null;
            tier = null;
            title.text = displayName.ToUpperInvariant() + " - CURRENT CARDS";
            tiers.SetValueWithoutNotify("All");
            UpdateColors();
            DrawCards();
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.Focus();
            return true;
        }

        public void Close()
        {
            hero = default;
            inspection = null;
            grid.Clear();
            Root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            if (!IsOpen) return;
            var next = Find(hero);
            if (next == null) { Close(); return; }
            if (inspection.Cards.SequenceEqual(next.Cards)) return;
            inspection = next;
            UpdateColors();
            DrawCards();
        }

        private HeroCardInspection Find(HeroId id)
        {
            var projection = read?.Invoke();
            if (projection == null) return null;
            return projection is IExperimentalReadModel experimental
                ? experimental.CardInspections.FirstOrDefault(value => value.Hero.Equals(id))
                : HeroCardInspection.ForHero(projection, id);
        }

        private CardDefinition[] Definitions() => inspection == null ? Array.Empty<CardDefinition>() :
            read().Definitions.Where(value => inspection.Cards.Contains(value.Reference))
                .OrderBy(value => value.Progression.Color).ThenBy(value => value.Progression.Tier)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray();

        private void UpdateColors()
        {
            colorValues.Clear();
            colorValues.Add(null);
            colorValues.AddRange(Definitions().Select(value => (CardColor?)value.Progression.Color).Distinct());
            if (!colorValues.Contains(color)) color = null;
            colors.choices = colorValues.Select(value => !value.HasValue ? "All" :
                value.Value == CardColor.None ? "Colorless" : MatchHudTheme.ColorName(value.Value)).ToList();
            colors.SetValueWithoutNotify(colors.choices[colorValues.IndexOf(color)]);
        }

        private void DrawCards()
        {
            grid.Clear();
            var all = Definitions();
            var selected = all.Where(value => (!color.HasValue || value.Progression.Color == color.Value) &&
                (!tier.HasValue || value.Progression.Tier == tier.Value)).ToArray();
            count.text = selected.Length + " / " + all.Length + " cards";
            foreach (var definition in selected)
            {
                // Presentation-only identity: this window has no card submission/suggestion callbacks.
                var model = new TrayCardViewModel(new CardInstanceId("inspection:" + definition.Id.Value),
                    definition.Reference, definition.DisplayName, definition.Progression.Color,
                    CurrentRoundCards.ReadFace(definition), TrayCardGroup.Available, 0, false, false);
                var face = new VisualElement { name = "gd-inspected-card", tooltip = definition.DisplayName };
                ConfiguredCardFaceView.Build(face, model);
                face.style.marginRight = 18f;
                face.style.marginBottom = 18f;
                HudElements.ApplyButtonMotion(face);
                grid.Add(face);
            }
            if (selected.Length == 0)
                grid.Add(HudElements.Text("No cards match these filters.", 16f, MatchHudTheme.MutedText));
            scroll.scrollOffset = Vector2.zero;
        }
    }
}
