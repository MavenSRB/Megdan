using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
namespace GuardsDigital.Tests.PlayMode
{
    public sealed class ExperimentalCoordinationPlayModeTests
    {
        [UnityTest] public IEnumerator InspectSuggestReturnAndBoardOverlayPreserveGameplay()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = ArienUiJourneyPlayModeTests.Root(); var seat = root.Runtime.Host.Seats.Seats[0];
            MatchSceneCompositionRoot.NextComposedSeat = seat.Id;
            root.ConfigureExperiments(new ExperimentalMatchOptions(teammateCoordination: true));
            yield return null; yield return null;
            var runtime = root.Runtime;
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var ally = runtime.Host.Seats.Seats.First(s => s.TeamSide == seat.TeamSide && !s.Id.Equals(seat.Id));
            var version = runtime.ReadModel.Version;
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud,
                BroganEngagedUiJourneyPlayModeTests.HeroView(ally.HeroId).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(ally.HeroId));
            UiPlayer.Click(root.Hud.RootElement.Q<Button>("my-cards"));
            yield return null;
            var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(e => e.name == "gd-hero-rail" &&
                e.userData is HeroId id && id.Equals(ally.HeroId));
            UiPlayer.Click(rail.Q("gd-hero-identity-plate"));
            yield return null; yield return null;
            Assert.That(runtime.HandsView.Model.ViewedHand.IsReadOnly, Is.True);
            var cardId = runtime.HandsView.Model.ViewedHand.PlayableTrayCards.Last().CardInstanceId;
            runtime.HandsView.SetContextOnly(true);
            yield return null; yield return null;
            var cardButton = runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().Last();
            Assert.That(UiPlayer.IsShown(cardButton), Is.True);
            RightClick(cardButton);
            yield return null; yield return null;
            Assert.That(runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().Last(), Is.SameAs(cardButton),
                "Sending a card suggestion must preserve the actual input control.");
            var host = (MatchHost)runtime.Host;
            Assert.That(host.ProjectionForSeat(seat.Id).TeammateMarkers.Single().Card, Is.EqualTo(cardId));
            Assert.That(runtime.HandsView.Root.Q("teammate-card-signal"), Is.Not.Null);
            cardButton = runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().Last();
            UiPlayer.PointAt(cardButton); yield return null; yield return null;
            var popped = runtime.HandsView.HoverPreviewRoot;
            Assert.That(popped.Q("teammate-card-signal"), Is.Not.Null, "The enlarged card keeps its white outline.");
            Assert.That(popped.Q("teammate-card-owners"), Is.Not.Null, "The marker owner follows the enlarged card.");
            Assert.That(popped.Q("teammate-card-signal").worldBound.width,
                Is.GreaterThan(cardButton.worldBound.width), "The suggestion must use the enlarged card footprint.");
            var face=popped.Q("gd-card-preview");var signal=popped.Q("teammate-card-signal");
            runtime.RefreshClockDisplay();yield return null;
            Assert.That(runtime.HandsView.HoverPreviewRoot.Q("gd-card-preview"),Is.SameAs(face));
            Assert.That(popped.Q("teammate-card-signal"),Is.SameAs(signal),"A clock-only draw must leave hover and its highlight untouched.");
            var allyView = host.ProjectionForSeat(ally.Id);
            Assert.That(host.SubmitCoordinationForSeat(ally.Id, new CoordinationCommand(0, CoordinationTargetKind.Hex,
                false, runtime.ReadModel.SelectedMap.Reference, runtime.ReadModel.SelectedMap.Spaces.First().Coordinate,
                generation: allyView.CoordinationGeneration)), Is.True);
            yield return null; yield return null;
            Assert.That(runtime.HandsView.HoverPreviewRoot.Q("gd-card-preview"), Is.SameAs(face),
                "A teammate's suggestion must preserve the hovered card face.");
            Assert.That(runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().Last(), Is.SameAs(cardButton));
            yield return UiPlayer.CaptureScreen("tmp/964a-hover-card.png");
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            UiPlayer.Click(root.Hud.RootElement.Q<Button>("my-cards"));
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));
            for(var frame=0;frame<20 && root.Hud.RootElement.Q("allied-hand-inspection")!=null;frame++) yield return null;
            Assert.That(root.Hud.RootElement.Q("allied-hand-inspection"), Is.Null);
            var tile = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Where(t => t.IsPlayable).OrderBy(t => t.Coordinate.Q * t.Coordinate.Q + t.Coordinate.R * t.Coordinate.R).First();
            var screen = pointer.EventCamera.WorldToScreenPoint(tile.transform.position);
            var originalMaterial = tile.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(pointer.TrySuggestAtScreen(screen), Is.True);
            yield return null; yield return null;
            Assert.That(root.Hud.RootElement.Q("teammate-hex-outline"), Is.Not.Null);
            Assert.That(root.Hud.RootElement.Q("teammate-hex-owners").Q("gd-hero-icon"), Is.Not.Null);
            Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(originalMaterial));
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            Assert.That(runtime.SelectViewedHero(ally.HeroId), Is.True);
            foreach (var viewport in new[] { (1280u, 720u), (1920u, 1080u) })
            {
                UiPlayer.SetPlayerViewport(viewport.Item1, viewport.Item2, "experimental teammate signals");
                yield return null; yield return null;
                root.Hud.Layout();
                yield return null; yield return null; yield return null;
                Assert.That(root.Hud.StatusBarRect.height, Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
                Assert.That(UiPlayer.IsShown(root.Hud.RootElement.Q<Button>("my-cards")), Is.True);
                var cardBadge = root.Hud.RootElement.Q("teammate-card-owners").Q("gd-hero-medallion");
                var boardBadge = root.Hud.RootElement.Q("teammate-hex-owners").Q("gd-hero-medallion");
                Assert.That(cardBadge.worldBound.height, Is.GreaterThan(boardBadge.worldBound.height * 1.2f),
                    "Board markers use the approved smaller token size.");
                for (var parent = cardBadge.parent; parent != null; parent = parent.parent)
                    if (parent.style.overflow.value == Overflow.Hidden || parent is ScrollView)
                    {
                        Assert.That(parent.worldBound.Contains(cardBadge.worldBound.min), Is.True, "The full card portrait must escape clipping.");
                        Assert.That(parent.worldBound.Contains(cardBadge.worldBound.max), Is.True, "The full card portrait must escape clipping.");
                    }
                var identity = root.Hud.RootElement.Q("allied-hand-inspection").Q<Label>();
                Assert.That(identity.worldBound.Overlaps(cardBadge.worldBound), Is.False, "The suggestion portrait cannot cover the ally identity. Label=" + identity.worldBound + "; badge=" + cardBadge.worldBound);
                Assert.That(identity.resolvedStyle.color.r, Is.GreaterThan(0.7f), "Ally identity must contrast against the dark HUD.");
                yield return UiPlayer.CaptureScreen("tmp/964a-coordination-" + viewport.Item1 + ".png");
            }
            screen = pointer.EventCamera.WorldToScreenPoint(tile.transform.position);
            Assert.That(pointer.TrySuggestAtScreen(screen), Is.True);
            yield return null;
            Assert.That(host.ProjectionForSeat(seat.Id).TeammateMarkers.Single().Hex, Is.Null);
            root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
        }
        [UnityTest] public IEnumerator BoardSignalsStayBelowTheSpellbookAndEveryHudWindow()
        {
            var oldWidth=(uint)Screen.width;var oldHeight=(uint)Screen.height;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat=null;
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root=ArienUiJourneyPlayModeTests.Root();
                root.ConfigureExperiments(new ExperimentalMatchOptions(teammateCoordination:true));
                var printed=RegisteredHeroRoster.PrintedLineup(3);
                var gydion=RegisteredHeroRoster.ForThisBuild.Single(h=>h.ContentId.Value=="gydion");
                MatchSceneCompositionRoot.NextComposedSeat=new SeatId("red-player-01");
                root.ComposeForLineup(new HeroLineup(new[]{gydion}.Concat(printed.Red.Skip(1)),printed.Blue));
                yield return null;yield return null;
                var hero=GydionUiJourneyPlayModeTests.Gydion(root.Runtime);
                var pointer=Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                var tile=Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Where(t=>t.IsPlayable)
                    .OrderBy(t=>t.Coordinate.Q*t.Coordinate.Q+t.Coordinate.R*t.Coordinate.R).First();
                Assert.That(pointer.TrySuggestAtScreen(pointer.EventCamera.WorldToScreenPoint(tile.transform.position)),Is.True);
                yield return null;yield return null;
                var view=root.Runtime.HeroOverviewsView;
                Assert.That(view.OpenSpellbook(hero),Is.True);
                foreach(var viewport in new[]{(1280u,720u),(1920u,1080u)})
                {
                    UiPlayer.SetPlayerViewport(viewport.Item1,viewport.Item2,"board signal occlusion");
                    root.Hud.Layout();yield return null;yield return null;
                    var layer=root.Hud.RootElement.Q("teammate-board-signals");
                    var badge=layer.Q("teammate-hex-owners");
                    Assert.That(badge,Is.Not.Null);
                    Assert.That(view.SpellbookDrawer.worldBound.Overlaps(badge.worldBound),Is.True,
                        "The scenario must put the marker behind the actual window.");
                    Assert.That(root.Hud.RootElement.IndexOf(layer),Is.Zero,
                        "Board highlights and their owner tokens must paint below every HUD window.");
                    yield return UiPlayer.CaptureScreen("tmp/956-signals-under-spellbook-"+viewport.Item1+".png");
                }
                UiPlayer.Click(view.SpellbookDrawer.Q<Button>("gd-spellbook-close"));yield return null;
                Assert.That(root.Hud.RootElement.Q("teammate-hex-owners"),Is.Not.Null,
                    "Closing the window restores the unchanged board suggestion.");
                root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            }
            finally {UiPlayer.SetPlayerViewport(oldWidth,oldHeight,"restore board signal viewport");}
        }

        private static void RightClick(VisualElement element)
        {
            Assert.That(element.panel, Is.Not.Null);
            var point = element.worldBound.center;
            var target = element.panel.Pick(point);
            Assert.That(target == element || element.Contains(target), Is.True, "The card is reachable by a player: bounds=" + element.worldBound + "; hit=" + target?.name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                mousePosition = point, button = 1, clickCount = 1 }))
            { down.target = target; target.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                mousePosition = point, button = 1, clickCount = 1 }))
            { up.target = target; target.SendEvent(up); }
        }
    }
}
