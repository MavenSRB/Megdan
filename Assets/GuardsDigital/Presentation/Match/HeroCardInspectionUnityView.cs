using System;
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
        private readonly VisualElement grid;
        private readonly ScrollView scroll;
        private HeroCardInspection inspection;
        private HeroId hero;
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
            title.text = displayName.ToUpperInvariant() + " - CURRENT CARDS";
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

        // Only printed properties determine order; card lifecycle never changes the faces or layout.
        private CardDefinition[] Definitions() => inspection == null ? Array.Empty<CardDefinition>() :
            read().Definitions.Where(value => inspection.Cards.Contains(value.Reference))
                .OrderBy(value => value.Progression.Color).ThenBy(value => value.Progression.Tier)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray();

        private void DrawCards()
        {
            grid.Clear();
            var definitions = Definitions();
            foreach (var definition in definitions)
            {
                // Presentation-only identity: this window has no card submission/suggestion callbacks.
                var model = new TrayCardViewModel(new CardInstanceId("inspection:" + definition.Id.Value),
                    definition.Reference, definition.DisplayName, definition.Progression.Color,
                    CurrentRoundCards.ReadFace(definition), TrayCardGroup.Available, 0, false, false);
                var face = new VisualElement { name = "gd-inspected-card", tooltip = definition.DisplayName };
                ConfiguredCardFaceView.Build(face, model);
                // Shared hand faces include lifecycle captions/tooltips; inspection shows only printed faces.
                face.Q("gd-card-history")?.RemoveFromHierarchy();
                var artwork = face.Q("gd-card-artwork");
                if (artwork != null) artwork.tooltip = definition.DisplayName;
                face.style.marginRight = 18f;
                face.style.marginBottom = 18f;
                HudElements.ApplyButtonMotion(face);
                grid.Add(face);
            }
            if (definitions.Length == 0)
                grid.Add(HudElements.Text("No cards to show.", 16f, MatchHudTheme.MutedText));
            scroll.scrollOffset = Vector2.zero;
        }
    }
}
