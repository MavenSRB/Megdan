using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.21i. A many-bodied hero in the real scene, staged through the Director's one-shot
    /// body seam (9.21a's internal-test supply of four) on the engaged Wasp mirror: one standee per
    /// body with its quiet mark, "which of you acts" answered by clicking a standee, the acting cue on
    /// the body the engine settled, an Attack on the OTHER body, and every standee fading on the one
    /// Defeat - with the rail naming the hero once throughout. The captures the user judges the
    /// proposed mark and cue on are written under <c>tmp/9-21i-*</c>.
    /// </summary>
    public sealed class HeroBodyStandeesPlayModeTests
    {
        [UnityTest]
        public IEnumerator TwoStandees_ThePickByAClick_TheActingCue_AndBothFadeOnTheDefeat()
        {
            yield return LoadMirror();
            var first = Root().Runtime;
            var blue = first.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var red = first.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            var redAt = first.ReadModel.Heroes.Single(hero => hero.HeroId.Equals(red)).Coordinate;
            var spaces = FreeSpaces(first).ToArray();
            var bodyOneAt = spaces.Where(space => HexDistanceQuery.Calculate(space, redAt) == 1)
                .OrderBy(space => space.Q).ThenBy(space => space.R).First();
            var bodyTwoAt = spaces.Where(space =>
                    HexDistanceQuery.Calculate(space, bodyOneAt) == 2 &&
                    HexDistanceQuery.Calculate(space, redAt) >= 2)
                .OrderBy(space => space.Q).ThenBy(space => space.R).First();

            yield return LoadStaged(blue, bodyOneAt, new[] { new HeroOccupancy(blue, 2, bodyTwoAt) });
            var root = Root();
            var runtime = root.Runtime;
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var bodyOne = Standee(blue, 1);
            var bodyTwo = Standee(blue, 2);
            Assert.That(bodyOne.Coordinate, Is.EqualTo(bodyOneAt));
            Assert.That(bodyTwo.Coordinate, Is.EqualTo(bodyTwoAt));
            Assert.That(bodyTwo.HeroContentId, Is.EqualTo(bodyOne.HeroContentId), "One hero, one team face.");
            Assert.That(bodyTwo.HasTokenVisual, Is.EqualTo(bodyOne.HasTokenVisual));
            Assert.That(bodyOne.BodyMark.VisiblePips, Is.EqualTo(1));
            Assert.That(bodyTwo.BodyMark.VisiblePips, Is.EqualTo(2));
            AssertOneRailEntry(runtime, blue);

            // Blue's card resolves first with two bodies standing: "which of you acts" stands.
            Commit(runtime, blue, WaspCardContent.MagneticDagger);
            Commit(runtime, red, WaspCardContent.Shock);
            yield return null;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blue), Detail(runtime));
            Assert.That(runtime.ReadModel.PendingActingBodyChoice, Is.Not.Null, Detail(runtime));
            Assert.That(runtime.RoundView.Model.Status, Does.Contain("CHOOSE WHICH OF YOU ACTS"));
            var pickCues = runtime.BoardView.Model.PieceTargetCues
                .Where(cue => cue.Target.HeroId.Equals(blue)).ToArray();
            Assert.That(pickCues.Select(cue => cue.Target.HeroBody), Is.EquivalentTo(new[] { 1, 2 }),
                "Both standees are cued as the pick's answers.");
            Assert.That(runtime.BoardView.ActingHeroMarker == null ||
                !runtime.BoardView.ActingHeroMarker.IsVisible, Is.True,
                "No body acts yet, so the acting cue stands on none.");
            yield return BoardPieceCapturePlayModeTests.Capture(
                runtime.BoardView, "tmp/9-21i-pick", new[] { bodyOneAt, bodyTwoAt, redAt });

            // The pick is answered by clicking body 2's standee.
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, bodyTwo.transform.position), Is.True,
                UiPlayer.DescribeBoardRay(pointer, bodyTwo.transform.position));
            yield return null;
            Assert.That(runtime.ReadModel.PendingActingBodyChoice, Is.Null, Detail(runtime));
            Assert.That(runtime.ReadModel.ActingBodies.Single(), Is.EqualTo(new HeroActingBody(blue, 2)));
            Assert.That(runtime.BoardView.Model.FocusedHeroBody, Is.EqualTo(2));
            var marker = runtime.BoardView.ActingHeroMarker;
            Assert.That(marker != null && marker.IsVisible, Is.True, "The acting cue is drawn.");
            Assert.That(Flat(marker.transform.position), Is.EqualTo(Flat(bodyTwo.transform.position)),
                "The acting cue stands on the body that acts.");
            var faint = runtime.BoardView.FaintActingMarkers.Where(value => value != null && value.IsVisible).ToArray();
            Assert.That(faint.Select(value => Flat(value.transform.position)),
                Is.EqualTo(new[] { Flat(bodyOne.transform.position) }),
                "His other standing body wears the faint ring (user-settled): one bright, the rest faint.");
            yield return BoardPieceCapturePlayModeTests.Capture(
                runtime.BoardView, "tmp/9-21i-standees", new[] { bodyOneAt, bodyTwoAt, redAt });
            Healthy(runtime);

            // Blue's turn ends; Red attacks the OTHER body, declines nothing for Blue, and the one
            // Defeat removes every body (RZ-Q3): both standees leave, and both fade.
            yield return EndTurnsUntil(runtime, red);
            yield return BroganUiJourneyPlayModeTests.PointTrayAt(root, red);
            yield return BroganUiJourneyPlayModeTests.StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Detail(runtime));
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargets,
                Does.Contain(AttackTarget.HeroBodyOf(blue, 1)));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, bodyOne.transform.position), Is.True,
                UiPlayer.DescribeBoardRay(pointer, bodyOne.transform.position));
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Detail(runtime));
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.ReadModel.Heroes.Any(hero => hero.HeroId.Equals(blue)), Is.False,
                "One Defeat removes every body. " + Detail(runtime));
            Assert.That(bodyOne.gameObject.activeSelf, Is.False);
            Assert.That(bodyTwo.gameObject.activeSelf, Is.False);
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(bodyOne), Is.Not.Null,
                "The attacked body fades.");
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(bodyTwo), Is.Not.Null,
                "The other body fades with it.");
            AssertOneRailEntry(runtime, blue);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FourStandees_EachWithItsOwnMark_AndOneRailEntry()
        {
            yield return LoadMirror();
            var first = Root().Runtime;
            var blue = first.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var blueAt = first.ReadModel.Heroes.Single(hero => hero.HeroId.Equals(blue)).Coordinate;
            var others = FreeSpaces(first)
                .Where(space => HexDistanceQuery.Calculate(space, blueAt) == 2)
                .OrderBy(space => space.Q).ThenBy(space => space.R)
                .Where((space, index) => index % 2 == 0)
                .Take(3)
                .ToArray();
            Assert.That(others, Has.Length.EqualTo(3));

            yield return LoadStaged(blue, blueAt, others.Select((space, index) =>
                new HeroOccupancy(blue, index + 2, space)).ToArray());
            var runtime = Root().Runtime;
            var standees = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(blue) && view.gameObject.activeInHierarchy)
                .OrderBy(view => view.Body)
                .ToArray();
            Assert.That(standees.Select(view => view.Body), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(standees.Select(view => view.BodyMark.VisiblePips), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(standees.Select(view => view.Coordinate), Is.EqualTo(new[] { blueAt }.Concat(others)));
            Assert.That(standees.Select(view => view.GetComponent<PrimitiveHeroPointerSelector>())
                .All(selector => Object.FindAnyObjectByType<PrimitiveHeroPointerInput>().Selectors
                    .Contains(selector)), Is.True, "Every standee is offered to the pointer.");
            AssertOneRailEntry(runtime, blue);
            Healthy(runtime);

            // Body 3 is picked by a click: one bright ring on it, a faint ring on each other body.
            var red = runtime.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            Commit(runtime, blue, WaspCardContent.MagneticDagger);
            Commit(runtime, red, WaspCardContent.Shock);
            yield return null;
            Assert.That(runtime.ReadModel.PendingActingBodyChoice, Is.Not.Null, Detail(runtime));
            var root = Root();
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, standees[2].transform.position), Is.True,
                UiPlayer.DescribeBoardRay(pointer, standees[2].transform.position));
            yield return null;
            Assert.That(runtime.ReadModel.ActingBodies.Single(), Is.EqualTo(new HeroActingBody(blue, 3)));
            Assert.That(Flat(runtime.BoardView.ActingHeroMarker.transform.position),
                Is.EqualTo(Flat(standees[2].transform.position)));
            Assert.That(runtime.BoardView.FaintActingMarkers
                    .Where(value => value != null && value.IsVisible)
                    .Select(value => Flat(value.transform.position)),
                Is.EquivalentTo(new[] { standees[0], standees[1], standees[3] }
                    .Select(view => Flat(view.transform.position))));
            Healthy(runtime);
            yield return BoardPieceCapturePlayModeTests.Capture(
                runtime.BoardView, "tmp/9-21i-four", new[] { blueAt }.Concat(others).ToArray());
        }

        private static IEnumerable<HexCoordinate> FreeSpaces(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.ReadModel;
            var occupied = new HashSet<HexCoordinate>(model.Heroes.Select(value => value.Coordinate)
                .Concat(model.Minions.Select(value => value.Coordinate))
                .Concat(model.PlacedTokens.Select(value => value.Coordinate))
                .Concat(model.Obstacles.Select(value => value.Coordinate)));
            return model.SelectedMap.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable && !occupied.Contains(space.Coordinate))
                .Select(space => space.Coordinate);
        }

        private static IEnumerator LoadMirror()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return Load();
        }

        private static IEnumerator LoadStaged(HeroId hero, HexCoordinate bodyOne, IReadOnlyList<HeroOccupancy> others)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[] { new SandboxHeroStaging(hero, bodyOne) });
            MatchSceneCompositionRoot.NextComposedStagedHeroBodies = others;
            MatchSceneCompositionRoot.NextComposedStagedHeroBodySupplies = new[] { new HeroBodySupply(hero, 4) };
            yield return Load();
        }

        private static IEnumerator Load()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
            for (var frame = 0; frame < 3; frame++) yield return null;
        }

        private static void Commit(ConfiguredMatchSceneRuntime runtime, HeroId hero, CardDefinitionId face)
        {
            var card = runtime.Cards.Single(value => value.OwnerId.Equals(hero) &&
                value.State.Location == CardLocation.Hand && value.DefinitionReference.DefinitionId.Equals(face));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True, Detail(runtime));
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True, Detail(runtime));
        }

        private static IEnumerator EndTurnsUntil(ConfiguredMatchSceneRuntime runtime, HeroId hero)
        {
            for (var budget = 0; budget < 8; budget++)
            {
                if (runtime.PendingTurnRelease == null && runtime.SelectedActor.IsDefined &&
                    runtime.SelectedActor.ActorId.Equals(hero))
                {
                    yield break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True, Detail(runtime));
                yield return null;
            }

            Assert.Fail("The named seat never took a turn. " + Detail(runtime));
        }

        private static PrimitiveHeroView Standee(HeroId hero, int body) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(hero) && view.Body == body);

        private static void AssertOneRailEntry(ConfiguredMatchSceneRuntime runtime, HeroId hero)
        {
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Count(value => value.HeroId.Equals(hero)),
                Is.EqualTo(1), "The rail and the overview show one hero, however many bodies stand.");
        }

        /// <summary>Every projected body stands drawn where the projection puts it, and nothing faulted.</summary>
        private static void Healthy(ConfiguredMatchSceneRuntime runtime)
        {
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            foreach (var row in runtime.Heroes)
            {
                var view = Standee(row.HeroId, row.Body);
                Assert.That(view.gameObject.activeInHierarchy, Is.True, row.ToString());
                Assert.That(view.Coordinate, Is.EqualTo(row.Coordinate), row.ToString());
            }
        }

        private static Vector2 Flat(Vector3 position) =>
            new Vector2(Mathf.Round(position.x * 1000f) / 1000f, Mathf.Round(position.z * 1000f) / 1000f);

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static string Detail(ConfiguredMatchSceneRuntime runtime) =>
            $"[round {runtime.RoundIndex} turn {runtime.TurnIndex}; actor " +
            $"{(runtime.SelectedActor.IsDefined ? runtime.SelectedActor.ActorId.Value : "none")}; " +
            $"status '{runtime.RoundView.Model?.Status}'; last '{runtime.LastOperationDetail}']";
    }
}
