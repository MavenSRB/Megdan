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
        private readonly VisualElement handGrid;
        private readonly VisualElement otherGrid;
        private readonly ScrollView handScroll;
        private readonly ScrollView otherScroll;
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
            var sections = new VisualElement { name = "gd-hero-card-sections" };
            sections.style.flexDirection = FlexDirection.Row;
            sections.style.flexGrow = 1;
            sections.style.minHeight = 0;
            var hand = BuildSection("gd-hero-card-hand", "HAND", Align.FlexStart, out handScroll, out handGrid);
            hand.style.width = Length.Percent(45f);
            var other = BuildSection("gd-hero-card-other", null, Align.FlexEnd, out otherScroll, out otherGrid);
            other.style.width = Length.Percent(55f);
            sections.Add(hand);
            sections.Add(other);
            Root.Add(sections);
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
            handGrid.Clear();
            otherGrid.Clear();
            Root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            if (!IsOpen) return;
            var next = Find(hero);
            if (next == null) { Close(); return; }
            if (inspection.HandCards.SequenceEqual(next.HandCards) &&
                inspection.OtherCards.SequenceEqual(next.OtherCards) &&
                inspection.ConcealedHandCount == next.ConcealedHandCount &&
                inspection.ConcealedOtherCount == next.ConcealedOtherCount) return;
            inspection = next;
            DrawCards();
        }

        private HeroCardInspection Find(HeroId id)
        {
            var projection = read?.Invoke();
            if (projection == null) return null;
            if (!(projection is IExperimentalReadModel experimental) ||
                (!experimental.Experiments.TeammateCoordination &&
                 !experimental.Experiments.EnemyCardInformation))
                return null;
            return experimental.CardInspections.FirstOrDefault(value => value.Hero.Equals(id));
        }

        private CardDefinition[] Definitions(System.Collections.Generic.IReadOnlyList<CardDefinitionReference> cards) =>
            inspection == null ? Array.Empty<CardDefinition>() :
            read().Definitions.Where(value => cards.Contains(value.Reference))
                .OrderBy(value => value.Progression.Color).ThenBy(value => value.Progression.Tier)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray();

        private void DrawCards()
        {
            handGrid.Clear();
            otherGrid.Clear();
            DrawSection(handGrid, Definitions(inspection.HandCards), inspection.ConcealedHandCount);
            DrawSection(otherGrid, Definitions(inspection.OtherCards), inspection.ConcealedOtherCount);
            if (handGrid.childCount == 0)
                handGrid.Add(HudElements.Text("No cards in hand.", 16f, MatchHudTheme.MutedText));
            if (otherGrid.childCount == 0)
                otherGrid.Add(HudElements.Text("No other cards.", 16f, MatchHudTheme.MutedText));
            handScroll.scrollOffset = Vector2.zero;
            otherScroll.scrollOffset = Vector2.zero;
        }

        private void DrawSection(VisualElement target, CardDefinition[] definitions, int concealedCount)
        {
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
                target.Add(face);
            }

            var projection = read();
            var setup = projection.Setup.Heroes.FirstOrDefault(value => value.HeroId.Equals(hero));
            for (var index = 0; index < concealedCount; index++)
            {
                var back = HeroOverviewsUnityView.BuildCardBack(setup?.ContentId ?? default);
                back.name = "gd-inspected-card-back";
                back.tooltip = "Facedown card";
                back.style.marginRight = 18f;
                back.style.marginBottom = 18f;
                target.Add(back);
            }
        }

        private static VisualElement BuildSection(
            string name,
            string heading,
            Align alignment,
            out ScrollView scroll,
            out VisualElement grid)
        {
            var section = new VisualElement { name = name };
            section.style.flexDirection = FlexDirection.Column;
            section.style.alignItems = alignment;
            section.style.minHeight = 0;
            if (!string.IsNullOrEmpty(heading))
            {
                var label = HudElements.Text(heading, 16f, Color.white, FontStyle.Bold);
                label.name = "gd-hero-card-hand-heading";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.alignSelf = Align.Stretch;
                section.Add(label);
            }
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;
            scroll.style.alignSelf = Align.Stretch;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = alignment == Align.FlexEnd ? Justify.FlexEnd : Justify.FlexStart;
            HudElements.Padding(grid, 8f);
            scroll.Add(grid);
            section.Add(scroll);
            return section;
        }
    }
}
