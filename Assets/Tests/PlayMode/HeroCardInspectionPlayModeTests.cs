using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HeroCardInspectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator EscapeClosesInspectionAndSpellbookWithoutSubmittingACommand()
        {
            var keyboardAdded = false;
            Keyboard keyboard = null;
            try
            {
                var registered = RegisteredHeroRoster.ForThisBuild;
                var gydion = registered.Single(candidate => candidate.ContentId.Value == "gydion");
                var printed = RegisteredHeroRoster.PrintedLineup(3);
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                    new[] { gydion }.Concat(printed.Red.Where(candidate =>
                        candidate.ContentId.Value != "gydion").Take(2)), printed.Blue);
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = ArienUiJourneyPlayModeTests.Root();
                var runtime = root.Runtime;
                var view = runtime.HeroOverviewsView;
                view.CardInspection.UseTrustedLocalHeroPerspective = true;
                var hero = runtime.Host.Projection.Setup.Heroes.Single(candidate =>
                    candidate.ContentId.Value == "gydion").HeroId;
                var version = runtime.Host.Version;
                yield return null;
                keyboardAdded = Keyboard.current == null;
                keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                Assert.That(root.Hud.isActiveAndEnabled, Is.True, "the HUD must poll Escape in this scene");

                Assert.That(view.OpenCardInspection(hero), Is.True);
                view.CardInspection.Root.Q<Button>("gd-hero-card-close").Focus();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(keyboard.escapeKey.isPressed, Is.True, "the real Input System key event reached the device");
                Assert.That(view.CardInspection.IsOpen, Is.False,
                    "Escape closes inspection even while a child owns keyboard focus.");
                Assert.That(view.OpenSpellbook(hero), Is.True);
                root.Hud.RootElement.Q<Button>("gd-director-toggle").Focus();
                yield return null;
                yield return null;
                Assert.That(view.IsSpellbookOpen, Is.True,
                    "Holding the same Escape press must not close a second screen.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(view.IsSpellbookOpen, Is.False,
                    "Escape closes the drawer even when focus returned to the board HUD.");
                Assert.That(runtime.Host.Version, Is.EqualTo(version),
                    "Neither close action is a gameplay command.");
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedLineup = null;
                if (keyboard != null)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    if (keyboardAdded) InputSystem.RemoveDevice(keyboard);
                }
            }
        }

        [UnityTest] public IEnumerator PortraitKeepsHandAndUnavailableAboveTheSeparateGoldBoxAtBothSizes()
        {
            var oldWidth = (uint)Screen.width; var oldHeight = (uint)Screen.height;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = ArienUiJourneyPlayModeTests.Root();
                root.ConfigureExperiments(new ExperimentalMatchOptions(teammateCoordination: true, enemyCardInformation: true));
                var roster = RegisteredHeroRoster.ForThisBuild;
                var printed = RegisteredHeroRoster.PrintedLineup(3);
                var gydion = roster.Single(h => h.ContentId.Value == "gydion");
                var takahide = roster.Single(h => h.ContentId.Value == "takahide");
                MatchSceneCompositionRoot.NextComposedSeat = new SeatId("red-player-01");
                root.ComposeForLineup(new HeroLineup(new[] { gydion, takahide, printed.Red.Last() }, printed.Blue));
                yield return null; yield return null;
                var runtime = root.Runtime; var host = runtime.Host; var view = runtime.HeroOverviewsView;
                var own = host.Seats.Seats.First(s => s.Id.Value == "red-player-01");
                var ally = host.Projection.Setup.Heroes.Single(h => h.ContentId.Value == "takahide").HeroId;
                var enemy = host.Seats.Seats.First(s => s.TeamSide != own.TeamSide).HeroId;
                var version = host.Version;
                foreach (var width in new[] { 1280u, 1920u })
                {
                    UiPlayer.SetPlayerViewport(width, width * 9 / 16, "current card inspection");
                    root.Hud.Layout(); yield return null; yield return null;
                    if (width == 1920u)
                    {
                        var token = runtime.BoardView.HeroViews.Single(candidate =>
                            candidate.Occupancy.HeroId.Equals(ally));
                        Assert.That(token.CardStateIndicator, Is.Not.Null);
                        Assert.That(token.CardStateIndicator.PipCount, Is.EqualTo(5),
                            "Takahide begins with five bottom-row hand pips and no current-card pip.");
                        yield return BoardPieceCapturePlayModeTests.Capture(
                            runtime.BoardView,
                            "tmp/9-83-token-pips-rectangular-centered-1920",
                            new[] { token.Coordinate });

                        var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                        var pip = token.CardStateIndicator.transform.GetChild(2);
                        var screenPoint = pointerInput.EventCamera.WorldToScreenPoint(pip.position);
                        var versionBeforePreview = host.Version;
                        var addedKeyboard = Keyboard.current == null;
                        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                        var addedMouse = Mouse.current == null;
                        var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                        InputSystem.QueueStateEvent(
                            mouse, new MouseState { position = screenPoint });
                        InputSystem.QueueStateEvent(
                            keyboard, new KeyboardState(Key.LeftShift));
                        yield return null;
                        Assert.That(root.Hud.CardPipPreview.IsVisible, Is.True);
                        Assert.That(root.Hud.CardPipPreview.DefinitionReference,
                            Is.EqualTo(token.CardStateIndicator.Pips[2].DefinitionReference));
                        Assert.That(host.Version, Is.EqualTo(versionBeforePreview),
                            "Shift-hovering a token pip is inspection-only.");
                        yield return UiPlayer.CaptureScreen(
                            "tmp/9-83-shift-hover-card-pip-1920.png");

                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        yield return null;
                        Assert.That(root.Hud.CardPipPreview.IsVisible, Is.False);
                        if (addedMouse) InputSystem.RemoveDevice(mouse);
                        if (addedKeyboard) InputSystem.RemoveDevice(keyboard);
                    }
                    foreach (var hero in new[] { own.HeroId, ally, enemy })
                    {
                        var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(e => e.name == "gd-hero-rail" && e.userData is HeroId id && id.Equals(hero));
                        UiPlayer.Click(rail.Q("gd-hero-medallion"));
                        yield return null; yield return null;
                        var window = view.CardInspection.Root;
                        Assert.That(UiPlayer.IsShown(window), Is.True);
                        Assert.That(view.CardInspection.Hero, Is.EqualTo(hero));
                        Assert.That(window.Query<DropdownField>().ToList(), Is.Empty);
                        Assert.That(window.Q("gd-hero-card-filters"), Is.Null);
                        Assert.That(window.Query<VisualElement>("gd-inspected-card").ToList().Count,
                            Is.EqualTo(hero.Equals(ally) ? 7 : 5));
                        var first = window.Q("gd-inspected-card");
                        var original = first.worldBound;
                        UiPlayer.PointAt(first); yield return new WaitForSecondsRealtime(0.25f);
                        Assert.That(first.worldBound.width, Is.EqualTo(original.width * MatchHudTheme.ButtonHoverScale).Within(0.6f));
                        UiPlayer.Click(first); yield return null;
                        Assert.That(host.Version, Is.EqualTo(version), "Card faces have no gameplay actions.");
                        UiPlayer.PointAway(first); yield return new WaitForSecondsRealtime(0.25f);
                        if (hero.Equals(ally))
                        {
                            var scroll = window.Q<ScrollView>("gd-hero-card-scroll");
                            var row = window.Q("gd-hero-card-row");
                            var hand = window.Q("gd-hero-card-hand");
                            var other = window.Q("gd-hero-card-other");
                            var golds = window.Q("gd-hero-card-other-golds");
                            Assert.That(other.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                                "No ordinary unavailable card exists in Takahide's opening state.");
                            Assert.That(other.Q<Label>("gd-hero-card-other-heading").text,
                                Is.EqualTo("UNAVAILABLE CARDS"));
                            Assert.That(golds.Q<Label>("gd-hero-card-other-golds-heading").text, Is.EqualTo("OTHER GOLDS"));
                            Assert.That(hand.Query<VisualElement>("gd-inspected-card").ToList(), Has.Count.EqualTo(5));
                            Assert.That(golds.Query<VisualElement>("gd-inspected-card").ToList(), Has.Count.EqualTo(2));
                            Assert.That(hand.Q("gd-hero-card-hand-row").resolvedStyle.flexWrap, Is.EqualTo(Wrap.NoWrap));
                            Assert.That(golds.Q("gd-hero-card-other-golds-row").resolvedStyle.flexWrap, Is.EqualTo(Wrap.NoWrap));
                            Assert.That(hand.Query<VisualElement>("gd-inspected-card").ToList()
                                    .Max(card => card.worldBound.center.y) -
                                hand.Query<VisualElement>("gd-inspected-card").ToList()
                                    .Min(card => card.worldBound.center.y),
                                Is.LessThan(2f), "The available hand remains one horizontal row.");
                            Assert.That(golds.worldBound.yMin, Is.GreaterThan(hand.worldBound.yMin + 1f),
                                "Takahide's Other Golds box sits below his available hand.");
                            Assert.That(golds.worldBound.xMin, Is.EqualTo(hand.worldBound.xMin).Within(1f),
                                "The lower Other Golds box belongs to the left available-card column.");
                            Assert.That(golds.resolvedStyle.borderLeftWidth, Is.GreaterThan(0f),
                                "Other Golds has its own visible box.");
                            Assert.That(row.worldBound.width, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.width - 1f));
                            Assert.That(scroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Auto));

                            var visibleFace = hand.Query<VisualElement>("gd-inspected-card").ToList().First();
                            var lowerPoint = new Vector2(visibleFace.worldBound.center.x,
                                Mathf.Min(visibleFace.worldBound.yMax - 10, scroll.worldBound.yMax - 10));
                            Assert.That(window.Contains(window.panel.Pick(lowerPoint)), Is.True,
                                "Ordinary hand cards must not cover or intercept the inspected card's visible bottom edge.");
                            Assert.That(root.Hud.BlocksPointerAt(MatchHudPanelScaling.PanelPointToScreen(window.worldBound.center, Screen.width, Screen.height)), Is.True,
                                "Clicks in the inspection window cannot fall through to the board.");
                            var retained = window.Q("gd-inspected-card");
                            var beforeMarker = ((MatchHost)host).ProjectionForSeat(own.Id);
                            var previous = beforeMarker.TeammateMarkers.FirstOrDefault(m => m.Sender.Equals(own.HeroId));
                            Assert.That(((MatchHost)host).SubmitCoordinationForSeat(own.Id, new CoordinationCommand(previous?.Revision ?? 0,
                                CoordinationTargetKind.Hex, false, runtime.ReadModel.SelectedMap.Reference,
                                runtime.ReadModel.SelectedMap.Spaces.First().Coordinate, generation: beforeMarker.CoordinationGeneration)), Is.True);
                            yield return null; yield return null;
                            Assert.That(view.CardInspection.IsOpen, Is.True);
                            Assert.That(window.Q("gd-inspected-card"), Is.SameAs(retained),
                                "A marker update must preserve the open inspection window and its card faces.");
                            runtime.RefreshClockDisplay(); yield return null;
                            Assert.That(window.Q("gd-inspected-card"), Is.SameAs(retained),
                                "Clock ticks cannot rebuild the window.");
                            yield return UiPlayer.CaptureScreen("tmp/9-77-takahide-current-cards-" + width + ".png");
                        }
                        UiPlayer.Click(window.Q<Button>("gd-hero-card-close")); yield return null;
                        Assert.That(view.CardInspection.IsOpen, Is.False);
                    }
                    var menu = new PrivateLobbyUnityView(); menu.ShowPage(PrivateLobbyEntryPage.Host);
                    root.Hud.RootElement.Add(menu.Root);
                    menu.Root.style.position = Position.Absolute; menu.Root.style.left = 0; menu.Root.style.top = 0;
                    menu.Root.style.width = Length.Percent(100); menu.Root.style.height = Length.Percent(100);
                    yield return null; yield return null;
                    UiPlayer.Click(menu.Root.Q<Button>("experimental-button")); yield return null; yield return null;
                    UiPlayer.Click(menu.Root.Q<Toggle>("experimental-enemy-info"));
                    Assert.That(menu.CurrentEntry().Experiments.EnemyCardInformation, Is.True);
                    Assert.That(menu.CurrentEntry().Experiments.TeammateCoordination, Is.False);
                    UiPlayer.Click(menu.Root.Q<Toggle>("experimental-timer"));
                    yield return null; yield return null;
                    Assert.That(UiPlayer.IsShown(menu.Root.Q<IntegerField>("experimental-duration-4")), Is.True);
                    menu.Root.RemoveFromHierarchy();
                }
                Assert.That(host.Version, Is.EqualTo(version));
                root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "restore after inspection");
            }
        }
        [UnityTest]
        public IEnumerator GydionShowsHisUnavailableCardsInTheUpperRightGroup()
        {
            var oldWidth = (uint)Screen.width;
            var oldHeight = (uint)Screen.height;
            try
            {
                yield return GydionUiJourneyPlayModeTests.Stage(
                    SceneCardContent.VerifiedEngagedStagedGydion,
                    new[]
                    {
                        GydionCardContent.Cantrip,
                        GydionCardContent.PrepareSpells,
                        GydionCardContent.ElementaryEvocation
                    });
                var root = ArienUiJourneyPlayModeTests.Root();
                var runtime = root.Runtime;
                var gydion = GydionUiJourneyPlayModeTests.Gydion(runtime);
                var owner = runtime.Host.Seats.Seats.Single(seat => seat.Owns(gydion));
                var model = HeroCardInspection.ForSeat(
                        ((MatchHost)runtime.Host).Projection,
                        owner,
                        new ExperimentalMatchOptions(teammateCoordination: true))
                    .Single(row => row.Hero.Equals(gydion));
                Assert.That(model.HandCards, Has.Count.EqualTo(3));
                Assert.That(model.OtherCards, Has.Count.EqualTo(2));
                Assert.That(model.OtherGoldCards, Is.Empty);

                UiPlayer.SetPlayerViewport(1920, 1080, "Gydion unavailable-card inspection");
                root.Hud.Layout();
                yield return null;
                yield return null;
                var inspection = runtime.HeroOverviewsView.CardInspection;
                Assert.That(inspection.Open(model, GydionUiJourneyPlayModeTests.HisName), Is.True);
                yield return null;
                yield return null;

                var hand = inspection.Root.Q("gd-hero-card-hand");
                var unavailable = inspection.Root.Q("gd-hero-card-other");
                var golds = inspection.Root.Q("gd-hero-card-other-golds");
                Assert.That(unavailable.Q<Label>("gd-hero-card-other-heading").text,
                    Is.EqualTo("UNAVAILABLE CARDS"));
                Assert.That(hand.Query<VisualElement>("gd-inspected-card").ToList(), Has.Count.EqualTo(3));
                Assert.That(unavailable.Query<VisualElement>("gd-inspected-card").ToList(), Has.Count.EqualTo(2));
                Assert.That(unavailable.worldBound.xMin, Is.GreaterThan(hand.worldBound.xMin),
                    "Gydion's discarded cards occupy the upper-right group.");
                Assert.That(unavailable.worldBound.yMin, Is.EqualTo(hand.worldBound.yMin).Within(1f));
                Assert.That(golds.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                var visibleCards = inspection.Root.Query<VisualElement>("gd-inspected-card").ToList();
                Assert.That(inspection.Root.worldBound.width, Is.GreaterThan(1000f));
                Assert.That(visibleCards, Has.Count.EqualTo(5));
                Assert.That(visibleCards.All(card => UiPlayer.IsShown(card) &&
                    card.worldBound.width > 200f && inspection.Root.worldBound.Overlaps(card.worldBound)), Is.True,
                    "Every requested card must be visibly rendered inside the full-size production modal.");
                yield return UiPlayer.CaptureScreen("tmp/9-79-card-screen-three-available-two-unavailable-1920.png");
                inspection.Close();
                GydionUiJourneyPlayModeTests.Healthy(runtime);
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "restore after Gydion unavailable-card inspection");
            }
        }
    }
}
