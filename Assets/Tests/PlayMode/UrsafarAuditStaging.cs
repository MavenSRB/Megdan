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
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using static GuardsDigital.Tests.PlayMode.UrsafarUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    internal static class UrsafarAuditStaging
    {
        internal static IEnumerator Load(SceneCardContent content, int enemyDistance = 2,
            bool emptyEnemyHand = false, int? enemyLife = null, CardDefinitionId? discardOwn = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            var side = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(enemy)).TeamSide;
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var playable = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable).Select(space => space.Coordinate).ToArray();
            var origin = playable.Where(space => space.GetNeighbors().All(playable.Contains) &&
                playable.Contains(new HexCoordinate(space.Q + enemyDistance, space.R)) &&
                playable.Contains(new HexCoordinate(space.Q + 1, space.R + 1)))
                .OrderBy(space => HexDistanceQuery.Calculate(space, new HexCoordinate(0, 0))).First();
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(own, origin, hand: discardOwn.HasValue
                    ? runtime.Cards.Where(card => card.OwnerId.Equals(own) && card.State.Location == CardLocation.Hand)
                        .Select(card => card.DefinitionReference.DefinitionId).Where(id => !id.Equals(discardOwn.Value))
                    : null),
                new SandboxHeroStaging(enemy, new HexCoordinate(origin.Q + enemyDistance, origin.R),
                    level: enemyLife.HasValue ? 7 : (int?)null,
                    hand: emptyEnemyHand ? new CardDefinitionId[0] : null)
            }, removedMinions: runtime.Setup.Minions.Select(minion => minion.MinionId),
                spawnedMinions: new[] { new SandboxMinionStaging(new MinionId("audit-prey"), side, StandardMinionType.Melee,
                    new HexCoordinate(origin.Q + 1, origin.R + 1)) },
                teamLifeCounts: enemyLife.HasValue ? new[] { new KeyValuePair<TeamSide, int>(side, enemyLife.Value) } : null);
            yield return LoadStagedScene(content);
        }

        internal static IEnumerator EnrageThroughProwlingBrute()
        {
            var root = Root();
            var runtime = root.Runtime;
            var own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ProwlingBrute);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            yield return FinishMovement();
            var card = runtime.Cards.Single(value => value.OwnerId.Equals(own) && value.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.ProwlingBrute));
            Assert.That(runtime.IsActiveEffectSource(card.Id), Is.True, Remaining(runtime) + "\n" +
                string.Join("\n", runtime.DiagnosticEvents.Select(entry => entry.DisplayText)));
            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Walks Ursafar one space through the real board and asserts he ACTUALLY WALKED IT.
        ///
        /// <para><b>MILESTONE 8.86.</b> Six journeys of his reach a movement through this one step -
        /// the borrowed Movement primary, the modal borrow, the enrage set-up and the two Instinctive
        /// Reaction arms among them - and not one of them read the board afterwards. The step clicked
        /// a lit space and moved on, so a Movement that offered a destination and then put him back
        /// where he was would have been invisible to every one of them. The start is captured, the
        /// destination is refused if it is that same space, and the piece is read back.</para>
        /// </summary>
        internal static IEnumerator FinishMovement()
        {
            var root = Root();
            var runtime = root.Runtime;
            var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemy)).Coordinate;
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            var steps = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1 && !value.Destination.Equals(startedAt))
                .OrderBy(value => HexDistanceQuery.Calculate(value.Destination, enemyAt))
                .ToArray();
            Assert.That(steps, Is.Not.Empty,
                "p.10's movement session offers no one-space step that leaves the space he is " +
                "standing on, so this walk could not move him. " + Remaining(runtime));
            var destination = steps[0].Destination;
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                Object.FindObjectsByType<HexTileView>().Single(tile => tile.Coordinate.Equals(destination)).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.EqualTo(destination),
                "THE ASSERTION EVERY JOURNEY THROUGH THIS STEP WAS MISSING: he is standing on the " +
                "space that was clicked. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.Not.EqualTo(startedAt),
                "and he really left the space he set off from.");
            if (runtime.PendingMovementChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }
        }

        internal static IEnumerator AnswerDefense(bool survive)
        {
            var root = Root();
            var runtime = root.Runtime;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            if (survive)
            {
                var answer = SurvivingDefense(runtime);
                Assert.That(answer, Is.Not.Null, "The staged defender must be able to survive.");
                var card = runtime.Cards.Single(value => value.Id.Equals(answer.DefenseBreakdown.CardInstanceId));
                var name = runtime.Definitions.Single(value => value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
                var face = UiPlayer.NamedElementWithText(root.Hud, "gd-defense-card", name);
                UiPlayer.EnsureVisible(face);
                yield return null;
                UiPlayer.Click(face);
            }
            else
            {
                // Milestone 8.46e: the Defense is declined on the decision window's own corner
                // (`TAKE THE HIT`), read off its model - the strip no longer draws `DECLINE DEFENSE`.
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            }
            yield return null;
            yield return null;
        }
    }
}
