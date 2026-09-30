using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.21k. Razzle dealt into a loaded scene, on the terms of Mortimer's 9.8f fixture: both
    /// staged boards' scene-composition arms, his prepared pick card on the rail, and Stunt Doubles'
    /// spawned bodies standing as standees that wear his prepared team face with their own marks. Each
    /// case also writes captured frames.
    /// </summary>
    public sealed class RazzleDealingPlayModeTests
    {
        private static readonly MinionId RedRanged = new MinionId("red-ranged-01");

        [UnityTest]
        public IEnumerator HisPickCardShowsOnTheRail_AndStuntDoublesSpawnsStandeesWearingHisPreparedFace()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedRazzle);
            var root = Root();
            var runtime = root.Runtime;
            var razzle = RazzleUiJourneyPlayModeTests.Razzle(runtime);
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // His pick card: the prepared selection front, on hovering his rail portrait.
            var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                element.name == "gd-hero-rail" && element.userData is HeroId id && id.Equals(razzle));
            UiPlayer.PointAt(rail.Q("gd-hero-medallion"));
            yield return null;
            var face = runtime.HeroOverviewsView.HoverPreviewRoot.Q<Image>("gd-hero-profile-preview");
            Assert.That(face, Is.Not.Null, "His rail portrait previews his pick card.");
            Assert.That(face.image, Is.Not.Null);
            Assert.That(face.image, Is.SameAs(Resources.Load<Texture2D>(
                "Private/HeroSelection/" + QuickGameSetupConfiguration.RazzleHeroIdValue)));
            yield return UiPlayer.CaptureScreen("tmp/9-21k-razzle-pick-card-1920x1080.png");
            UiPlayer.PointAway(rail.Q("gd-hero-medallion"));
            yield return null;

            var standee = RazzleUiJourneyPlayModeTests.Standee(razzle, 1);
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, standee.transform.position), Is.True);
            yield return null;
            yield return UiPlayer.CaptureScreen("tmp/9-21k-razzle-starting-hand-1920x1080.png");

            // The engaged staging puts Red's ranged minion beside him; Stunt Doubles' basic Attack
            // defeats it and then spawns a body on each empty space in Radius 1.
            yield return CommitEverySeatForRazzle(root, razzle, RazzleCardContent.StuntDoubles);
            yield return GarrusUiJourneyTests.WaitForActor(root, razzle);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Has.Member(RedRanged), Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, MinionView(RedRanged).transform.position), Is.True);
            yield return null;
            for (var budget = 0; budget < 6 && runtime.PendingEffectChoice != null; budget++)
            {
                var space = runtime.PendingEffectChoice.Candidates
                    .Where(value => value.Value.Kind == EffectValueKind.Coordinate)
                    .Select(value => value.Value.Coordinate)
                    .ToArray();
                var at = FirstClickableSpace(pointer, space);
                var node = runtime.PendingEffectChoice.NodeId;
                Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(at).transform.position), Is.True);
                yield return null;
                yield return ConfirmIfStillStanding(root, node);
            }

            var standees = RazzleUiJourneyPlayModeTests.DrawnStandees(razzle);
            Assert.That(standees.Select(view => view.Body), Is.EqualTo(new[] { 1, 2, 3, 4 }),
                "Stunt Doubles spawns up to three more of him: four bodies, four standees. " + Remaining(runtime));
            foreach (var view in standees)
            {
                Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(RazzleCardContent.ContentSetId));
                Assert.That(view.HasTokenVisual, Is.True, "Body " + view.Body + " wears his prepared team face.");
                Assert.That(view.BodyMark.VisiblePips, Is.EqualTo(view.Body), "Body " + view.Body + " wears its own mark.");
            }

            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Count(value => value.HeroId.Equals(razzle)), Is.EqualTo(1));
            yield return UiPlayer.CaptureScreen("tmp/9-21k-razzle-four-standees-1920x1080.png");
            RazzleUiJourneyPlayModeTests.Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TheTierThreeHand_WithTwinStrikeFaceup_StartsStuntDoublesFromItsControl()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedRazzle);
            var root = Root();
            var runtime = root.Runtime;
            var razzle = RazzleUiJourneyPlayModeTests.Razzle(runtime);
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Twin Strike stands face up on his rail.");
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud,
                RazzleUiJourneyPlayModeTests.Standee(razzle, 1).transform.position), Is.True);
            yield return null;
            yield return UiPlayer.CaptureScreen("tmp/9-21k-razzle-tier3-hand-1920x1080.png");

            yield return CommitEverySeatForRazzle(root, razzle, RazzleCardContent.StuntDoubles);
            yield return GarrusUiJourneyTests.WaitForActor(root, razzle);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "Stunt Doubles started from the real control and asks for its target. " + Remaining(runtime));
            yield return UiPlayer.CaptureScreen("tmp/9-21k-razzle-tier3-stunt-doubles-1920x1080.png");
            RazzleUiJourneyPlayModeTests.Healthy(runtime);
        }

        private static IEnumerator CommitEverySeatForRazzle(MatchSceneCompositionRoot root, HeroId razzle,
            CardDefinitionId face)
        {
            var runtime = root.Runtime;
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (!runtime.ViewedHeroId.Equals(razzle))
            {
                Assert.That(UiPlayer.ClickBoard(pointer, root.Hud,
                    RazzleUiJourneyPlayModeTests.DrawnStandees(razzle).First().transform.position), Is.True);
                yield return null;
            }

            yield return CommitThroughTray(root, NameOf(runtime, face));
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId)
                .Where(hero => !hero.Equals(razzle)).ToArray())
            {
                yield return PointTrayAt(root, seat);
                yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }
        }
    }
}
