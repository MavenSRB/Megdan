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
        private readonly VisualElement handSection;
        private readonly VisualElement otherSection;
        private readonly VisualElement otherGoldSection;
        private readonly VisualElement handGrid;
        private readonly VisualElement otherGrid;
        private readonly VisualElement otherGoldGrid;
        private readonly ScrollView rowScroll;
        private HeroCardInspection inspection;
        private HeroId hero;
        private CardDefinition hoveredDefinition;
        private VisualElement hoveredFace;
        public HeroCardPipPreviewUnityView SpellPreview { get; } = new HeroCardPipPreviewUnityView();
        public VisualElement Root { get; }
        public bool IsOpen => hero.IsDefined;
        public HeroId Hero => hero;
        /// <summary>
        /// The local unseated host owns the authority and may inspect a hero through that hero's
        /// own perspective. Remote and seated views keep using their visibility-safe projection.
        /// </summary>
        public bool UseTrustedLocalHeroPerspective { get; set; }


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

            rowScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { name = "gd-hero-card-scroll" };
            rowScroll.style.flexGrow = 1;
            rowScroll.style.flexShrink = 1;
            rowScroll.style.minHeight = 0;
            rowScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            rowScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            var row = new VisualElement { name = "gd-hero-card-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.minWidth = Length.Percent(100f);
            row.style.minHeight = 0;

            var availableColumn = new VisualElement { name = "gd-hero-card-available-column" };
            availableColumn.style.flexDirection = FlexDirection.Column;
            availableColumn.style.alignItems = Align.FlexStart;
            availableColumn.style.flexShrink = 0;
            handSection = BuildSection("gd-hero-card-hand", "HAND", out handGrid);
            otherGoldSection = BuildSection(
                "gd-hero-card-other-golds", "OTHER GOLDS", out otherGoldGrid, true);
            otherGoldSection.style.marginTop = 14f;
            availableColumn.Add(handSection);
            availableColumn.Add(otherGoldSection);

            var spacer = new VisualElement { name = "gd-hero-card-spacer" };
            spacer.style.flexGrow = 1;
            spacer.style.flexShrink = 0;
            spacer.style.minWidth = 18f;
            otherSection = BuildSection("gd-hero-card-other", "UNAVAILABLE CARDS", out otherGrid);
            row.Add(availableColumn);
            row.Add(spacer);
            row.Add(otherSection);
            rowScroll.Add(row);
            Root.Add(rowScroll);
            Root.Add(SpellPreview.Root);
            rowScroll.RegisterCallback<WheelEvent>(_ => ClearSpellPreview());

        }

        public bool Open(HeroId id, string displayName) => Open(Find(id), displayName);

        public bool Open(HeroCardInspection model, string displayName)
        {
            if (model == null) return false;
            hero = model.Hero;
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
            ClearSpellPreview();
            hero = default;
            inspection = null;
            handGrid.Clear();
            otherGrid.Clear();
            otherGoldGrid.Clear();
            Root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            if (!IsOpen) return;
            var next = Find(hero);
            if (next == null) { Close(); return; }
            if (inspection.HandCards.SequenceEqual(next.HandCards) &&
                inspection.OtherCards.SequenceEqual(next.OtherCards) &&
                inspection.OtherGoldCards.SequenceEqual(next.OtherGoldCards) &&
                inspection.ConcealedHandCount == next.ConcealedHandCount &&
                inspection.ConcealedOtherCount == next.ConcealedOtherCount &&
                inspection.ConcealedOtherGoldCount == next.ConcealedOtherGoldCount)
            {
                RefreshSpellPreview();
                return;
            }
            inspection = next;
            DrawCards();
        }

        private HeroCardInspection Find(HeroId id)
        {
            var projection = read?.Invoke();
            if (projection == null) return null;
            if (UseTrustedLocalHeroPerspective)
                return HeroCardInspection.ForHero(projection, id);
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
            ClearSpellPreview();
            handGrid.Clear();
            otherGrid.Clear();
            otherGoldGrid.Clear();
            DrawSection(handGrid, Definitions(inspection.HandCards), inspection.ConcealedHandCount);
            DrawSection(otherGrid, Definitions(inspection.OtherCards), inspection.ConcealedOtherCount);
            DrawSection(otherGoldGrid, Definitions(inspection.OtherGoldCards), inspection.ConcealedOtherGoldCount);
            if (handGrid.childCount == 0)
                handGrid.Add(HudElements.Text("No cards in hand.", 16f, MatchHudTheme.MutedText));
            otherSection.style.display = otherGrid.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            otherGoldSection.style.display = otherGoldGrid.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            rowScroll.scrollOffset = Vector2.zero;
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
                HudElements.ApplyButtonMotion(face);
                face.userData = definition.Reference;
                HudElements.OnPointedAt(face, pointedAt =>
                {
                    if (!pointedAt) { ClearSpellPreview(); return; }
                    hoveredDefinition = definition;
                    hoveredFace = face;
                    RefreshSpellPreview();
                });
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
                target.Add(back);
            }
        }

        private void ClearSpellPreview()
        {
            hoveredDefinition = null;
            hoveredFace = null;
            SpellPreview.Hide();
        }

        private void RefreshSpellPreview()
        {
            if (hoveredDefinition == null || hoveredFace?.panel == null) return;
            var spells = SpellbookPresentation.CastableSpells(read(), hero, hoveredDefinition);
            if (spells.Count == 0) { SpellPreview.Hide(); return; }
            var bounds = Root.parent?.worldBound ?? Root.worldBound;
            var top = Mathf.Max(bounds.yMin, Root.worldBound.yMin + 48f);
            var origin = Root.WorldToLocal(new Vector2(bounds.xMin, top));
            SpellPreview.Show(hoveredDefinition, Root.WorldToLocal(hoveredFace.worldBound.center),
                new Rect(origin.x, origin.y, bounds.width, bounds.yMax - top), spells);
        }

        private static VisualElement BuildSection(string name, string heading, out VisualElement grid, bool framed = false)
        {
            var section = new VisualElement { name = name };
            section.style.flexDirection = FlexDirection.Column;
            section.style.alignItems = Align.Stretch;
            section.style.flexShrink = 0;
            section.style.minHeight = 0;
            if (framed)
            {
                section.style.backgroundColor = new Color(0.035f, 0.043f, 0.050f, 0.92f);
                HudElements.Padding(section, 8f);
                HudElements.Border(section, MatchHudTheme.PanelBorder, 1f, 8f);
            }
            var label = HudElements.Text(heading, 16f, Color.white, FontStyle.Bold);
            label.name = name + "-heading";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.alignSelf = Align.Stretch;
            section.Add(label);
            grid = new VisualElement { name = name + "-row" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.NoWrap;
            grid.style.flexShrink = 0;
            HudElements.Padding(grid, 8f);
            section.Add(grid);
            return section;
        }
    }
}
