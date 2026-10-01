using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.23, queued by 9.21f: <c>choose-target@2</c> publishes the identity of the unit it
    /// named whether or not the node declares it, the resolution records every published outcome,
    /// and the snapshot reader refuses an outcome the node did not declare - so a match paused at
    /// the push of Min's Crane, Tiger or Dragon Stance, whose subject node declared only the chosen
    /// space, could not be restored (an Undo or a reconnect during her turn). Each stance is driven
    /// on a staged session to its push question, captured there, restored, and played on.
    /// </summary>
    [TestFixture]
    public sealed class MinStanceRestoreTests
    {
        private static readonly CardDefinitionId[] PushingStances =
        {
            MinCardContent.CraneStance, MinCardContent.TigerStance, MinCardContent.DragonStance
        };

        [TestCase(1, MinUpgradeBranch.FirstSibling, "min-crane-stance")]
        [TestCase(2, MinUpgradeBranch.FirstSibling, "min-tiger-stance")]
        [TestCase(3, MinUpgradeBranch.FirstSibling, "min-dragon-stance")]
        public void AStancePausedAtItsPush_RestoresAndPlaysOn(
            int tier, MinUpgradeBranch branch, string expectedStance)
        {
            var session = StageMinBesideWaspAndAMinion(tier, branch, out var min, out var wasp);
            var stance = session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(min) && card.State.Location == CardLocation.Hand &&
                PushingStances.Contains(card.DefinitionReference.DefinitionId));
            Assert.That(stance.DefinitionReference.DefinitionId.Value, Is.EqualTo(expectedStance),
                "the staged hand at this tier holds this one pushing stance");
            CommitSoThatMinActsFirst(session, min, wasp, stance);
            AttackAnAdjacentEnemyMinion(session);

            var subject = session.Projection.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, "after the attack the stance asks for the unit to push");
            Assert.That(subject.NodeId.Value, Does.EndWith("-subject"));
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session,
                subject.SelectInvocations.Single(value => value.Candidate.Value.HeroId.Equals(wasp)));

            var push = session.Projection.PendingEffectChoice;
            Assert.That(push, Is.Not.Null, "the push's distance is the next question");
            Assert.That(push.NodeId.Value, Does.EndWith("-push"));

            var payload = session.CaptureSnapshot();
            var restored = session.RestoreSnapshot(payload);
            Assert.That(restored.Accepted, Is.True,
                "a match paused at the push restores; refused with: " + restored.Detail);
            var again = session.Projection.PendingEffectChoice;
            Assert.That(again, Is.Not.Null, "the push question stands after the restore");
            Assert.That(again.NodeId, Is.EqualTo(push.NodeId));

            var waspBefore = session.Projection.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session,
                again.SelectInvocations.Single(value => value.Candidate.Value.IntegerValue == 1));
            var waspAfter = session.Projection.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(waspBefore, waspAfter), Is.EqualTo(1),
                "and the restored push still pushes, one space directly away from her");
        }

        // ---- Staging: Min adjacent to Wasp and to one of his Red minions, so her stance's Attack
        // defeats the minion and the after-attack push has Wasp to name.

        private static MatchSession StageMinBesideWaspAndAMinion(
            int tier, MinUpgradeBranch branch, out HeroId min, out HeroId wasp)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedMinForInternalTest(TeamSide.Red)
                : tier == 2 ? MatchConfiguration.CreateStagedMinTierTwoForInternalTest(TeamSide.Red, branch)
                : MatchConfiguration.CreateStagedMinTierThreeForInternalTest(TeamSide.Red, branch);
            var participants = configuration.Participants;
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var engaged = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusMinOneVsOne(),
                    participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var minSetup = engaged.Heroes.Single(hero =>
                hero.ContentId.Value == MinCardContent.ContentSetId);
            var waspSetup = engaged.Heroes.Single(hero => !hero.HeroId.Equals(minSetup.HeroId));
            min = minSetup.HeroId;
            wasp = waspSetup.HeroId;
            // Min is Blue in this staging, so the engaged minion is her own side; what her Attack
            // needs beside her is one of Wasp's Red minions, and the push needs Wasp himself.
            var redMinions = engaged.Minions
                .Where(value => value.TeamSide == TeamSide.Red)
                .Select(value => value.Coordinate)
                .ToArray();
            var occupied = engaged.Minions.Select(value => value.Coordinate)
                .Concat(new[] { waspSetup.Coordinate })
                .ToHashSet();
            var minAt = waspSetup.Coordinate.GetNeighbors()
                .Where(at => redMinions.Any(red => HexDistanceQuery.Calculate(at, red) == 1))
                .Where(at => !occupied.Contains(at))
                .Where(at => map.Spaces.Any(space =>
                    space.Coordinate.Equals(at) && space.Kind == BoardMapSpaceKind.Playable))
                .OrderBy(at => at.Q).ThenBy(at => at.R)
                .FirstOrDefault();
            Assert.That(minAt, Is.Not.EqualTo(default(HexCoordinate)),
                "a free playable space beside both Wasp and one of his minions exists on this map");

            var heroes = engaged.Heroes.Select(hero =>
            {
                var coordinate = hero.HeroId.Equals(minSetup.HeroId) ? minAt : hero.Coordinate;
                var space = map.Spaces.Single(value => value.Coordinate.Equals(coordinate));
                return new QuickGameHeroSetupState(
                    hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                    hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id), coordinate);
            }).ToArray();
            var setup = new QuickGameSetupState(
                engaged.SelectedMap, engaged.WaveCount, engaged.TieBreakerState, engaged.Teams,
                heroes, engaged.Minions);
            var scenario = new InitialScenarioDescription(
                participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static void CommitSoThatMinActsFirst(
            MatchSession session, HeroId min, HeroId wasp, CardInstance stance)
        {
            var stanceInitiative = session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(stance.DefinitionReference))
                .Initiative.Value;
            var waspCard = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(wasp) && card.State.Location == CardLocation.Hand)
                .Select(card => new
                {
                    Card = card,
                    Initiative = session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value
                })
                .Where(value => value.Initiative < stanceInitiative)
                .OrderBy(value => value.Initiative)
                .First().Card;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(min, stance.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(wasp, waspCard.Id));
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(stance.Id),
                "the stance's printed Initiative puts Min first against Wasp's lower card");
        }

        private static void AttackAnAdjacentEnemyMinion(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
            {
                var action = session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(candidate.SourceDefinitionReference))
                    .ActionDefinitions.Single(value => value.Id.Equals(candidate.ActionDefinitionId));
                return action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == ActionKind.Attack;
            });
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "the stance's Attack asks for its target");
            Assert.That(choice.LegalTargetMinionIds, Is.Not.Empty,
                "a Red minion beside her is a legal target");
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, choice.LegalTargetMinionIds.First()));
        }
    }
}
