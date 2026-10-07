using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GydionUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.30j. Gydion dealt into a loaded scene, on the terms of Razzle's and Mortimer's
    /// dealing fixtures: scenes 142 and 143 through BOTH scene-composition arms (the configuration arm
    /// deals his hand and book, the setup arm names his seat and draws his standee) - which 9.30i
    /// recorded had not yet run in Unity - and a capture case for the user's review at 2560x1440.
    /// </summary>
    public sealed class GydionDealingPlayModeTests
    {
        [UnityTest]
        public IEnumerator HandSpellbookShortcutSharesTheDrawerAndClearsDirectorAndDockedPrompt()
        {
            var oldWidth = (uint)Screen.width; var oldHeight = (uint)Screen.height;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = Root();
                var printed = RegisteredHeroRoster.PrintedLineup(3);
                var gydion = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "gydion");
                root.ComposeForLineup(new HeroLineup(new[] { gydion, printed.Red[1], printed.Red[2] }, printed.Blue));
                yield return null; yield return null;
                var runtime = root.Runtime;
                var hero = runtime.Setup.Heroes.Single(value => value.ContentId.Value == "gydion").HeroId;
                Assert.That(runtime.SelectViewedHero(hero), Is.True);
                foreach (var width in new[] { 1280u, 1920u })
                {
                    UiPlayer.SetPlayerViewport(width, width * 9 / 16, "9.64h spellbook shortcut");
                    runtime.DecisionWindowView.Render(new DecisionWindowViewModel(
                        1L, DecisionWindowFamily.MinionBattleRemoval, DecisionWindowState.Docked,
                        "964h-shortcut-layout", "REMOVE 2 MINIONS", "MINION BATTLE",
                        "Click minions on the board until 2 are chosen.", "1 of 2 chosen"));
                    root.Hud.Layout(); yield return null; yield return null;
                    var shortcut = root.Hud.RootElement.Q<Button>("gd-hand-spellbook");
                    var director = root.Hud.RootElement.Q<Button>("gd-director-toggle");
                    var prompt = root.Hud.RootElement.Q("gd-decision-window-docked");
                    var trayChip = root.Hud.RootElement.Q<Button>("gd-spellbook-chip");
                    Assert.That(UiPlayer.IsShown(shortcut), Is.True);
                    Assert.That(shortcut.text, Is.EqualTo(trayChip.text),
                        "The hand control carries the tray chip's live SPELLBOOK n | m reading.");
                    Assert.That(shortcut.resolvedStyle.backgroundColor, Is.EqualTo(trayChip.resolvedStyle.backgroundColor));
                    Assert.That(shortcut.resolvedStyle.color, Is.EqualTo(trayChip.resolvedStyle.color));
                    Assert.That(UiPlayer.IsShown(prompt), Is.True);
                    Assert.That(shortcut.worldBound.Overlaps(director.worldBound), Is.False);
                    Assert.That(shortcut.worldBound.Overlaps(prompt.worldBound), Is.False);
                    foreach (var rail in root.Hud.RootElement.Query<VisualElement>("gd-hero-rail").ToList())
                        Assert.That(shortcut.worldBound.Overlaps(rail.worldBound), Is.False,
                            "The shortcut must also clear every hero tray: " + shortcut.worldBound + " / " + rail.worldBound);
                    foreach (var card in runtime.HandsView.Root.Query<VisualElement>("gd-card-slot").ToList())
                        Assert.That(shortcut.worldBound.yMax, Is.LessThanOrEqualTo(card.worldBound.yMin));
                    var panel = ConfiguredMatchHudRenderer.PanelViewport;
                    Assert.That(shortcut.worldBound.Overlaps(ConfiguredMatchHudRenderer.CalculateOptionalStepControlRect(panel.x, panel.y)), Is.False);
                    Assert.That(shortcut.worldBound.Overlaps(ConfiguredMatchHudRenderer.CalculateTeamAgreementControlRect(panel.x, panel.y)), Is.False);
                    Assert.That(root.Hud.BlocksPointerAt(MatchHudPanelScaling.PanelPointToScreen(
                        shortcut.worldBound.center, Screen.width, Screen.height)), Is.True);
                    UiPlayer.Click(director); yield return null;
                    Assert.That(root.Hud.IsDirectorDrawerOpen, Is.True);
                    Assert.That(shortcut.worldBound.Overlaps(root.Hud.RootElement.Q("gd-director-drawer").worldBound), Is.False);
                    yield return UiPlayer.CaptureScreen("tmp/964h-shortcut-" + width + ".png");
                    var version = runtime.Host.Version;
                    UiPlayer.Click(shortcut); yield return null; yield return null;
                    Assert.That(runtime.HeroOverviewsView.IsSpellbookOpen, Is.True);
                    Assert.That(runtime.HeroOverviewsView.OpenSpellbookHeroId, Is.EqualTo(hero));
                    Assert.That(runtime.HeroOverviewsView.SpellbookDrawer.Query<VisualElement>("gd-spellbook-spell").ToList().Count, Is.EqualTo(22));
                    Assert.That(runtime.Host.Version, Is.EqualTo(version));
                    UiPlayer.Click(runtime.HeroOverviewsView.SpellbookDrawer.Q<Button>("gd-spellbook-close")); yield return null;
                    Assert.That(runtime.HeroOverviewsView.IsSpellbookOpen, Is.False);
                    UiPlayer.Click(director); yield return null;
                    UiPlayer.PointAway(director); yield return new WaitForSecondsRealtime(0.25f);
                    var directorPosition = director.worldBound.position;
                    root.Hud.SetDirectorToolsVisible(false); yield return null; yield return null;
                    UiPlayer.PointAway(shortcut); yield return new WaitForSecondsRealtime(0.25f);
                    Assert.That(UiPlayer.IsShown(shortcut), Is.True, "Ordinary players keep their book shortcut.");
                    Assert.That(shortcut.worldBound.x, Is.EqualTo(directorPosition.x).Within(1f));
                    Assert.That(shortcut.worldBound.y, Is.EqualTo(directorPosition.y).Within(1f));
                    Assert.That(shortcut.worldBound.Overlaps(prompt.worldBound), Is.False);
                    yield return UiPlayer.CaptureScreen("tmp/964h-shortcut-player-" + width + ".png");
                    UiPlayer.Click(shortcut); yield return null; yield return null;
                    Assert.That(runtime.HeroOverviewsView.IsSpellbookOpen, Is.True);
                    UiPlayer.Click(runtime.HeroOverviewsView.SpellbookDrawer.Q<Button>("gd-spellbook-close")); yield return null;
                    root.Hud.SetDirectorToolsVisible(true);
                    runtime.DecisionWindowPresenter.Refresh();
                    yield return null; yield return null;
                    Assert.That(runtime.SelectCommitmentCard(runtime.HandsView.Model.ViewedHand.Cards.First().Id), Is.True);
                    yield return null; yield return null;
                    var confirm = root.Hud.RootElement.Q("gd-confirm-control");
                    Assert.That(UiPlayer.IsShown(confirm), Is.True);
                    Assert.That(confirm.worldBound.Overlaps(shortcut.worldBound), Is.False);
                    Assert.That(confirm.worldBound.Overlaps(ConfiguredMatchHudRenderer.CalculateDecisionWindowDockedRect(panel.x, panel.y)), Is.False);
                    foreach (var card in runtime.HandsView.Root.Query<VisualElement>("gd-card-slot").ToList())
                        Assert.That(confirm.worldBound.yMax, Is.LessThanOrEqualTo(card.worldBound.yMin));
                    yield return UiPlayer.CaptureScreen("tmp/964h-shortcut-confirm-" + width + ".png");
                    Assert.That(runtime.SelectViewedHero(hero), Is.True);
                    yield return null; yield return null;
                }
                Assert.That(runtime.SelectViewedHero(runtime.Setup.Heroes.First(value => !value.HeroId.Equals(hero)).HeroId), Is.True);
                yield return null; yield return null;
                Assert.That(UiPlayer.IsShown(root.Hud.RootElement.Q("gd-hand-spellbook")), Is.False,
                    "A hero without a book has no shortcut.");
                Assert.That(runtime.SelectViewedHero(hero), Is.True);
                yield return null; yield return null;
                Assert.That(UiPlayer.IsShown(root.Hud.RootElement.Q("gd-hand-spellbook")), Is.True);
                Assert.That(runtime.SelectCommitmentCard(runtime.HandsView.Model.ViewedHand.Cards.First().Id), Is.True);
                yield return null; yield return null;
                var beforeCommit = runtime.Host.Version;
                UiPlayer.Click(root.Hud.RootElement.Q("gd-confirm-control"));
                yield return null; yield return null;
                Assert.That(runtime.Host.Version, Is.EqualTo(beforeCommit + 1), "The relocated confirmation still submits exactly once.");
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "9.64h restore");
            }
        }

        [UnityTest]
        public IEnumerator GydionsJoinedNonHostSeatSeesAndOpensTheSameSpellbookShortcut()
        {
            var oldWidth = (uint)Screen.width;
            var oldHeight = (uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(1920u, 1080u, "9.74 castable spell hover captures");
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                var directorRuntime = Root().Runtime;
                var joinedSeatId = directorRuntime.Host.Seats.FindByHero(Gydion(directorRuntime)).Id;
                MatchSceneCompositionRoot.NextComposedSeat = joinedSeatId;
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                var root = Root();
                yield return null;
                yield return null;

                var runtime = root.Runtime;
                var seat = runtime.Host.Seats.Seats.Single(value => value.Id.Equals(joinedSeatId));
                Assert.That(runtime.Setup.Heroes.Single(value => value.HeroId.Equals(seat.HeroId)).ContentId.Value,
                    Is.EqualTo(GydionCardContent.ContentSetId), "the joined seat is Gydion's, not a host simulation");
                Assert.That(runtime.Seat?.Id, Is.EqualTo(joinedSeatId),
                    "the runtime reads and submits through Gydion's owner-scoped seat client");
                // Local seat fixtures retain Director debug controls. Online admission calls this same
                // HUD transition from AttachOnlineClient, after binding the admitted seat above.
                root.Hud.SetDirectorToolsVisible(false);
                yield return null;
                var director = root.Hud.RootElement.Q<Button>("gd-director-toggle");
                Assert.That(UiPlayer.IsShown(director), Is.False, "joined players do not receive Director tools");

                var shortcut = root.Hud.RootElement.Q<Button>("gd-hand-spellbook");
                var trayChip = root.Hud.RootElement.Q<Button>("gd-spellbook-chip");
                Assert.That(UiPlayer.IsShown(shortcut), Is.True, "Gydion's joined seat keeps the hand shortcut");
                Assert.That(shortcut.text, Is.EqualTo("SPELLBOOK 22 | 0"));
                Assert.That(shortcut.text, Is.EqualTo(trayChip.text));
                Assert.That(shortcut.resolvedStyle.backgroundColor, Is.EqualTo(trayChip.resolvedStyle.backgroundColor));
                Assert.That(shortcut.resolvedStyle.color, Is.EqualTo(trayChip.resolvedStyle.color));

                var cantrip = runtime.Cards.Single(card => card.OwnerId.Equals(seat.HeroId) &&
                    card.DefinitionReference.DefinitionId.Equals(GydionCardContent.Cantrip));
                var cantripModel = runtime.HandsView.Model.ViewedHand.TrayCards
                    .Single(card => card.CardInstanceId.Equals(cantrip.Id));
                Assert.That(cantripModel.CastableSpells.Count, Is.EqualTo(3),
                    "the seat-scoped hand model carries Cantrip's typed cast arms");
                var cantripSlot = runtime.HandsView.Root.Query<VisualElement>("gd-card-slot").ToList()
                    .Single(slot => slot.userData is CardInstanceId id && id.Equals(cantrip.Id));
                var cantripButton = cantripSlot.Q<Button>("gd-card-choice");
                UiPlayer.PointAt(cantripButton);
                yield return new WaitForSecondsRealtime(0.25f);
                var castable = UiPlayer.Descendants(root.Hud.RootElement)
                    .SingleOrDefault(element => element.name == "gd-castable-spells" && UiPlayer.IsShown(element));
                Assert.That(castable, Is.Not.Null,
                    "the joined Gydion player sees Cantrip's declared spells on pointer rest");
                Assert.That(castable.Query<VisualElement>("gd-spell-face").ToList().Count, Is.EqualTo(3));
                Assert.That(castable.Query<VisualElement>("gd-spell-greyed").ToList(), Is.Empty,
                    "all three spells begin in the book");
                var castableFaces = castable.Query<VisualElement>("gd-spell-face").ToList();
                Assert.That(castable.worldBound.yMax,
                    Is.LessThanOrEqualTo(runtime.HandsView.HoverPreviewRoot.Q("gd-card-preview").worldBound.yMin + 1f),
                    "the stack stands above and clears the casting card");
                Assert.That(castableFaces.Select(face => face.worldBound.center.x).Max() -
                    castableFaces.Select(face => face.worldBound.center.x).Min(), Is.LessThan(1f),
                    "the declared spell faces form one vertical column");
                yield return UiPlayer.CaptureScreen("tmp/9-74-gydion-cast-hover-all-in-book.png");

                var removedIndex = 0;
                var removedDefinition = runtime.Host.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(cantripModel.CastableSpells[removedIndex].DefinitionReference));
                var oneRemovedSpells = cantripModel.CastableSpells.Select((spell, index) =>
                    index == removedIndex
                        ? new SpellCardViewModel(removedDefinition, isGreyed: true,
                            caption: removedDefinition.DisplayName.ToUpperInvariant() + " - REMOVED")
                        : spell).ToArray();
                var oneRemovedCantrip = new TrayCardViewModel(
                    cantripModel.CardInstanceId,
                    cantripModel.DefinitionReference,
                    cantripModel.DisplayName,
                    cantripModel.Color,
                    cantripModel.Face,
                    cantripModel.Group,
                    cantripModel.PlayedTurnIndex,
                    cantripModel.IsSelectable,
                    cantripModel.IsSelected,
                    cantripModel.IsActiveEffectSource,
                    cantripModel.IsResolving,
                    cantripModel.IsAwaitedAnswer,
                    cantripModel.IsDimmedByResolution,
                    cantripModel.IsFacedown,
                    oneRemovedSpells);
                var populatePeek = typeof(DirectorHandsUnityView).GetMethod(
                    "PopulatePeekPreview",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Assert.That(populatePeek, Is.Not.Null);
                populatePeek.Invoke(null, new object[]
                {
                    runtime.HandsView.HoverPreviewRoot,
                    oneRemovedCantrip,
                    cantripSlot,
                    false,
                    false
                });
                yield return null;
                yield return null;
                castable = runtime.HandsView.HoverPreviewRoot.Q("gd-castable-spells");
                Assert.That(castable.Query<VisualElement>("gd-spell-greyed").ToList(), Has.Count.EqualTo(1));
                Assert.That(castable.Query<Label>("gd-castable-spell-caption").ToList(), Is.Empty);
                Assert.That(castable.Q<Label>("gd-castable-spells-label"), Is.Null);
                Assert.That(castable.Query<VisualElement>("gd-spell-face").ToList()
                    .All(face => face.worldBound.width > 200f), Is.True,
                    "the three printed spell faces are large enough to read");
                Assert.That(castable.worldBound.yMin,
                    Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight),
                    "the larger vertical stack stays below the status header");
                yield return UiPlayer.CaptureScreen("tmp/9-80-gydion-cast-hover-large-one-dimmed.png");
                UiPlayer.PointAway(cantripButton);
                yield return new WaitForSecondsRealtime(0.25f);

                var version = runtime.Host.Version;
                UiPlayer.Click(shortcut);
                yield return null;
                yield return null;
                Assert.That(runtime.HeroOverviewsView.IsSpellbookOpen, Is.True);
                Assert.That(runtime.HeroOverviewsView.OpenSpellbookHeroId, Is.EqualTo(seat.HeroId));
                Assert.That(runtime.Host.Version, Is.EqualTo(version), "opening the book is local presentation only");
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "9.74 restore");
            }
        }

        [UnityTest]
        public IEnumerator SpellbookCardsUseTheSharedHoverBumpWithoutAHoverPopup()
        {
            var oldWidth = (uint)Screen.width;
            var oldHeight = (uint)Screen.height;
            try
            {
                foreach (var width in new[] { 1280u, 1920u })
                {
                    UiPlayer.SetPlayerViewport(width, width * 9 / 16, "9.64d spellbook");
                    MatchSceneCompositionRoot.NextComposedSeat = null;
                    yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                    var root = Root();
                    var view = root.Runtime.HeroOverviewsView;
                    Assert.That(view.OpenSpellbook(Gydion(root.Runtime)), Is.True);
                    root.Hud.Layout();
                    yield return null;
                    yield return null;
                    var cards = UiPlayer.Descendants(view.SpellbookDrawer)
                        .Where(e => e.name == "gd-spellbook-spell").ToArray();
                    Assert.That(cards.Length, Is.EqualTo(22));
                    var version = root.Runtime.Host.Version;
                    foreach (var card in cards.Take(3))
                    {
                        var layout = card.layout;
                        var bounds = card.worldBound;
                        var neighbor = cards.First(other => other != card);
                        var neighborBounds = neighbor.worldBound;
                        UiPlayer.PointAt(card);
                        yield return new WaitForSecondsRealtime(0.25f);
                        Assert.That(view.HoverPreviewRoot.resolvedStyle.display,
                            Is.EqualTo(DisplayStyle.None), "No detached duplicate face.");
                        Assert.That(card.layout, Is.EqualTo(layout), "Hover must not reflow the book.");
                        Assert.That(card.worldBound.width,
                            Is.EqualTo(bounds.width * MatchHudTheme.ButtonHoverScale).Within(0.5f));
                        Assert.That(card.worldBound.height,
                            Is.EqualTo(bounds.height * MatchHudTheme.ButtonHoverScale).Within(0.5f));
                        Assert.That(neighbor.worldBound, Is.EqualTo(neighborBounds));
                        if (card == cards[1])
                            yield return UiPlayer.CaptureScreen("tmp/964d-spellbook-hover-" + width + ".png");
                        UiPlayer.PointAway(card);
                        yield return new WaitForSecondsRealtime(0.25f);
                        Assert.That(card.worldBound.width, Is.EqualTo(bounds.width).Within(0.5f));
                        Assert.That(card.worldBound.height, Is.EqualTo(bounds.height).Within(0.5f));
                        Assert.That(root.Runtime.Host.Version, Is.EqualTo(version),
                            "Inspecting a spell must not submit gameplay.");
                    }
                    UiPlayer.Click(view.SpellbookDrawer.Q<Button>("gd-spellbook-close"));
                    yield return null;
                    Assert.That(view.IsSpellbookOpen, Is.False);
                }
            }
            finally { UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "9.64d restore viewport"); }
        }

        [UnityTest]
        public IEnumerator SceneOneFortyTwo_DealsHisStartingHandAFullBookAndHisPreparedFaces()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
            var root = Root();
            var runtime = root.Runtime;

            // The setup arm: his seat is named Gydion and stands on the engaged board against Wasp.
            var seat = runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId);
            // A staged one-vs-one seat is named short (QuickGameSetupConfiguration.CreateOneVsOne(..., "Gydion"));
            // the full title is the roster's, which the picker shows.
            Assert.That(seat.DisplayName, Is.EqualTo("Gydion"));
            Assert.That(RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId).DisplayName, Is.EqualTo(HisName));
            var gydion = seat.HeroId;
            var wasp = Wasp(runtime);
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(GydionSpace));
            Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(WaspSpace));
            var standee = HeroView(gydion);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(GydionCardContent.ContentSetId));
            Assert.That(standee.HasTokenVisual, Is.True, "His standee wears his prepared team face.");

            // The configuration arm: p.6's five in hand, thirteen in the deck, twenty-two in the book.
            Assert.That(HandOf(runtime, gydion).Select(definition => definition.Id),
                Is.EquivalentTo(GydionCardContent.StartingHand));
            Assert.That(runtime.Cards.Count(card => card.OwnerId.Equals(gydion) && card.State.Location == CardLocation.Deck),
                Is.EqualTo(13));
            Assert.That(InTheBook(runtime), Is.EqualTo(22), "GY-Q1: every spell, Wish included.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));
            Assert.That(runtime.Cards.Any(card => card.DefinitionReference.DefinitionId.Equals(GydionCardContent.CheapTrick)),
                Is.False, "Cheap Trick stays boxed (p.20).");

            // His pick card: the prepared selection front, on hovering his rail portrait.
            var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                element.name == "gd-hero-rail" && element.userData is HeroId id && id.Equals(gydion));
            UiPlayer.PointAt(rail.Q("gd-hero-medallion"));
            yield return null;
            var face = runtime.HeroOverviewsView.HoverPreviewRoot.Q<Image>("gd-hero-profile-preview");
            Assert.That(face?.image, Is.Not.Null, "His rail portrait previews his pick card.");
            Assert.That(face.image, Is.SameAs(Resources.Load<Texture2D>(
                "Private/HeroSelection/" + QuickGameSetupConfiguration.GydionHeroIdValue)));
            UiPlayer.PointAway(rail.Q("gd-hero-medallion"));
            yield return null;
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator SceneOneFortyThree_DealsTheTierThreeHandWithTheArchwizardFaceupAndWishInTheBook()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedGydion);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            Assert.That(HandOf(runtime, gydion).Select(definition => definition.Id), Is.EquivalentTo(new[]
            {
                GydionCardContent.Cantrip, GydionCardContent.PrepareSpells, GydionCardContent.GreaterEvocation,
                GydionCardContent.GreaterTransmutation, GydionCardContent.GreaterConjuration
            }));
            Assert.That(Card(runtime, gydion, GydionCardContent.TheArchwizard).State.Location, Is.Not.EqualTo(CardLocation.Deck),
                "The Archwizard is unlocked.");
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "The Archwizard stands faceup in his items strip.");
            Assert.That(Spell(runtime, GydionCardContent.Wish).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));

            // Prepare Spells from its control offers the refill or Wish instead (GY-Q7).
            yield return Begin(root, GydionCardContent.PrepareSpells);
            Assert.That(SpellOption(runtime, GydionCardContent.Wish)?.IsAnswerable, Is.True, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice));
            Healthy(runtime);
        }

        /// <summary>
        /// The user's review captures at 2560x1440, in <c>tmp/9-30j-gydion/</c>: his starting hand on his
        /// tray, and a cast in progress - Cantrip's three spell faces in the decision window, then Magic
        /// Missile's target lit on the board. The hero picker row is captured by
        /// <c>PrivateLobbyPlayModeTests.GydionsPickerRow_IsCapturedAt2560x1440</c>, the lobby's fixture.
        /// </summary>
        [UnityTest]
        public IEnumerator CapturesHisStartingHandAndACastInProgressAt2560x1440()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(2560, 1440, "9.30j Gydion captures");
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                var root = Root();
                var runtime = root.Runtime;
                var gydion = Gydion(runtime);
                yield return PointTrayAt(root, gydion);
                root.Hud.Layout();
                yield return null;
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/starting-hand-2560x1440.png");

                yield return Begin(root, GydionCardContent.Cantrip);
                Assert.That(runtime.DecisionWindowView.Model.Controls.Count(control => control.Spell != null), Is.EqualTo(3),
                    Remaining(runtime));
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/cast-choice-2560x1440.png");

                yield return PickSpellFace(root, GydionCardContent.MagicMissile);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/cast-target-2560x1440.png");
                Healthy(runtime);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "9.30j restore the viewport");
            }
        }
    }
}
