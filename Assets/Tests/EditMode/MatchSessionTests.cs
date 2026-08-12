using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MatchSessionTests
    {
        [Test]
        public void Session_RejectsAtomicallyAndPublishesOneVersionedProjectionPerAcceptedCommand()
        {
            var session = CreateSession();
            var initial = session.Projection;
            Assert.That(initial.Setup.Heroes.Select(value => value.Coordinate),
                Is.EquivalentTo(initial.Heroes.Select(value => value.Coordinate)));
            var rejected = session.Execute(DirectorCommand.StopMovement());

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable));
            Assert.That(session.Projection.Version, Is.EqualTo(initial.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(initial.AuthoritativeSnapshot));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(initial.DiagnosticSnapshot));
            Assert.That(session.Projection.AcceptedCommands, Is.Empty);

            var redCard = GetCards(session, TeamSide.Red)[0];
            var accepted = session.Execute(DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));

            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            Assert.That(accepted.Projection, Is.SameAs(session.Projection));
            Assert.That(session.Projection.Version, Is.EqualTo(initial.Version + 1));
            Assert.That(session.Projection.AcceptedCommands, Is.EqualTo(new[] { accepted.Command }));
            Assert.That(accepted.Events, Is.Not.Empty);
            Assert.That(session.Projection.DiagnosticEvents.Count, Is.GreaterThan(initial.DiagnosticEvents.Count));
        }

        [Test]
        public void RestoreSnapshot_MalformedEnvelopeRejectsWithoutPartialRestore()
        {
            var session = CreateSession();
            var redCard = GetCards(session, TeamSide.Red)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id)).Accepted, Is.True);
            var before = session.Projection;
            var bytes = Convert.FromBase64String(session.CaptureSnapshot());
            var malformed = Convert.ToBase64String(bytes.Concat(new byte[] { 0x4d }).ToArray());

            var result = session.RestoreSnapshot(malformed);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.SnapshotRejected));
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(before.DiagnosticSnapshot));
            Assert.That(session.Projection.AcceptedCommands, Is.EqualTo(before.AcceptedCommands));
        }

        [Test]
        public void Session_DispatchesEveryConfiguredLiveCommandFamilyThroughOnePort()
        {
            var session = CreateSession(useAdjacentSyntheticBoard: true);
            var kinds = new HashSet<DirectorCommandKind>();
            var red = GetCards(session, TeamSide.Red);
            var blue = GetCards(session, TeamSide.Blue);

            Commit(session, red[3], blue[3], kinds);
            StartCurrentAction(session, ActionKind.Movement, kinds);
            var stoppedChoice = session.Projection.PendingMovementChoice;
            Accept(session, DirectorCommand.StopMovement(
                stoppedChoice.ResolutionId,
                stoppedChoice.ChoiceId), kinds);
            HoldCurrentActor(session, kinds);

            Commit(session, red[2], blue[2], kinds);
            StartCurrentAction(session, ActionKind.Attack, kinds);
            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single()), kinds);
            var defense = session.Projection.PendingDefenseChoice.LegalDefenseOffers.First();
            var defenseChoice = session.Projection.PendingDefenseChoice;
            Accept(session, DirectorCommand.ChooseDefenseCard(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId,
                defense.SourceCardInstanceId), kinds);
            HoldCurrentActor(session, kinds);

            Commit(
                session,
                GetEligibleHandCards(session, TeamSide.Red).First(),
                GetEligibleHandCards(session, TeamSide.Blue).First(),
                kinds);
            StartCurrentAction(session, ActionKind.Movement, kinds);
            var destination = session.Projection.PendingMovementChoice.LegalDestinations
                .OrderBy(value => value.Cost)
                .First();
            var destinationChoice = session.Projection.PendingMovementChoice;
            AssertAtomicRejection(session, DirectorCommand.ChooseMovementDestination(
                destinationChoice.ResolutionId,
                new GuardsDigital.Core.Rules.CardResolution.ActionResolutionStepId("stale-session-choice"),
                destination.Destination));
            Accept(session, DirectorCommand.ChooseMovementDestination(
                destinationChoice.ResolutionId,
                destinationChoice.ChoiceId,
                destination.Destination), kinds);
            if (session.Projection.PendingMovementChoice != null)
            {
                var continuingChoice = session.Projection.PendingMovementChoice;
                Accept(session, DirectorCommand.StopMovement(
                    continuingChoice.ResolutionId,
                    continuingChoice.ChoiceId), kinds);
            }
            HoldCurrentActor(session, kinds);
            Commit(
                session,
                GetEligibleHandCards(session, TeamSide.Red).First(),
                GetEligibleHandCards(session, TeamSide.Blue).First(),
                kinds);
            HoldCurrentActor(session, kinds);
            HoldCurrentActor(session, kinds);
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound(), kinds);
            }

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(session.Projection.CompletedRounds, Has.Count.EqualTo(1));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.CommitCard));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.StartAction));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.ChooseMovementDestination));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.StopMovement));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.ChooseDefenseCard));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.PerformHold));
            Assert.That(kinds, Does.Contain(DirectorCommandKind.AdvanceEndOfRound));

            AssertAtomicRejection(session, DirectorCommand.Pass(red[0].OwnerId));
            AssertAtomicRejection(session, DirectorCommand.ChooseTiedActor(TeamSide.Red, red[0].Id));
            AssertAtomicRejection(session, DirectorCommand.DeclineDefense());
        }

        [Test]
        public void FastTravelCommand_ReplaysDeterministicallyAcrossUndo()
        {
            var session = CreateSession();
            var red = GetCards(session, TeamSide.Red);
            var blue = GetCards(session, TeamSide.Blue);
            Commit(session, red[3], blue[3], null);
            var actorId = session.Projection.SelectedActor.ActorId;
            var origin = session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate;
            var movement = GetCurrentAction(session, ActionKind.Movement);
            var originSpace = session.Projection.SelectedMap.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(origin));
            var originZone = session.Projection.SelectedMap.GetZoneForSpace(originSpace.Id).Id;
            var destination = session.Projection.SelectedMap.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Where(space => session.Projection.SelectedMap.GetZoneForSpace(space.Id).Id.Equals(originZone))
                .Select(space => space.Coordinate)
                .First(coordinate =>
                    session.Projection.Heroes.All(hero => !hero.Coordinate.Equals(coordinate)) &&
                    session.Projection.Setup.Minions.All(minion => !minion.Coordinate.Equals(coordinate)) &&
                    session.Projection.Obstacles.All(obstacle => !obstacle.Coordinate.Equals(coordinate)));
            var command = DirectorCommand.PerformFastTravel(movement.ActionDefinitionId, destination);

            var first = Accept(session, command, null);
            var expectedSnapshot = first.Projection.AuthoritativeSnapshot;
            Assert.That(first.Events.Any(value =>
                value.TurnEvent.Kind == TurnResolutionEventKind.FastTravelActionProgressed), Is.True);
            Assert.That(session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformFastTravel));

            var undo = session.Undo();
            Assert.That(undo.Accepted, Is.True, undo.Detail);
            Assert.That(session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(origin));

            var replayed = Accept(session, command, null);
            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(expectedSnapshot));
        }

        [Test]
        public void OfficialMapMovement_RulebookPage11CommitsShortestAndLongerWaypointRoutesToSameDestination()
        {
            var shortestSession = CreateSession();
            StartOfficialMapMovementFour(shortestSession);
            var shortestStart = shortestSession.Projection.PendingMovementChoice;
            var origin = shortestStart.Origin;
            var target = shortestStart.LegalDestinations
                .Where(value => value.Cost == 1)
                .OrderBy(value => value.Destination.Q)
                .ThenBy(value => value.Destination.R)
                .First(value => shortestStart.LegalDestinations.Any(candidate =>
                    candidate.Cost == 1 &&
                    !candidate.Destination.Equals(value.Destination) &&
                    candidate.Destination.GetNeighbors().Contains(value.Destination)));
            var shortestResult = Accept(
                shortestSession,
                DirectorCommand.ChooseMovementDestination(
                    shortestStart.ResolutionId,
                    shortestStart.ChoiceId,
                    target.Destination),
                null);

            var longerSession = CreateSession();
            StartOfficialMapMovementFour(longerSession);
            var longerStart = longerSession.Projection.PendingMovementChoice;
            var waypoint = longerStart.LegalDestinations
                .Where(value => value.Cost == 1 && !value.Destination.Equals(target.Destination))
                .OrderBy(value => value.Destination.Q)
                .ThenBy(value => value.Destination.R)
                .First(value => value.Destination.GetNeighbors().Contains(target.Destination));
            Accept(
                longerSession,
                DirectorCommand.ChooseMovementDestination(
                    longerStart.ResolutionId,
                    longerStart.ChoiceId,
                    waypoint.Destination),
                null);
            var continuation = longerSession.Projection.PendingMovementChoice;
            Assert.That(continuation.LegalDestinations.Select(value => value.Destination),
                Does.Contain(target.Destination));
            var longerResult = Accept(
                longerSession,
                DirectorCommand.ChooseMovementDestination(
                    continuation.ResolutionId,
                    continuation.ChoiceId,
                    target.Destination),
                null);

            Assert.That(shortestSession.Projection.SelectedMap.Reference,
                Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(shortestResult.Projection.PendingMovementResolution.MovementSessionState.Route,
                Is.EqualTo(new[] { origin, target.Destination }));
            Assert.That(longerResult.Projection.PendingMovementResolution.MovementSessionState.Route,
                Is.EqualTo(new[] { origin, waypoint.Destination, target.Destination }));
            Assert.That(shortestResult.Projection.Heroes.Single(value =>
                    value.HeroId.Equals(shortestResult.Projection.SelectedActor.ActorId)).Coordinate,
                Is.EqualTo(target.Destination));
            Assert.That(longerResult.Projection.Heroes.Single(value =>
                    value.HeroId.Equals(longerResult.Projection.SelectedActor.ActorId)).Coordinate,
                Is.EqualTo(target.Destination));
            Assert.That(
                longerResult.Projection.PendingMovementResolution.MovementSessionState.StepEvents
                    .Select(value => (value.Origin, value.Destination)),
                Is.EqualTo(new[]
                {
                    (origin, waypoint.Destination),
                    (waypoint.Destination, target.Destination)
                }));
        }

        [Test]
        public void ReplayOracle_MatchesLiveFreshUndoAndSnapshotContinuationEvidence()
        {
            var live = CreateSession();
            var commands = BuildCompletedRoundCommandSequence(live);
            var liveSnapshots = new List<string> { CreateSession().Projection.DiagnosticSnapshot };
            var liveResults = new List<MatchCommandResult>();
            var replayForPrefixes = CreateSession();
            foreach (var command in commands)
            {
                var result = replayForPrefixes.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
                liveResults.Add(result);
                liveSnapshots.Add(result.Projection.DiagnosticSnapshot);
            }

            var fresh = CreateSession();
            for (var index = 0; index < commands.Count; index++)
            {
                var replayed = fresh.Execute(commands[index]);
                Assert.That(replayed.Accepted, Is.EqualTo(liveResults[index].Accepted));
                Assert.That(replayed.RejectionReason, Is.EqualTo(liveResults[index].RejectionReason));
                Assert.That(replayed.Command, Is.EqualTo(liveResults[index].Command));
                Assert.That(replayed.Events.Select(value => value.Kind),
                    Is.EqualTo(liveResults[index].Events.Select(value => value.Kind)));
                Assert.That(replayed.Projection.Version, Is.EqualTo(liveResults[index].Projection.Version));
                Assert.That(replayed.Projection.DiagnosticSnapshot, Is.EqualTo(liveSnapshots[index + 1]));
                Assert.That(replayed.Projection.AcceptedCommands, Is.EqualTo(commands.Take(index + 1)));
            }

            for (var prefix = commands.Count - 1; prefix >= 0; prefix--)
            {
                var undo = fresh.Undo();
                Assert.That(undo.Accepted, Is.True, undo.Detail);
                Assert.That(undo.Projection.DiagnosticSnapshot, Is.EqualTo(liveSnapshots[prefix]));
                Assert.That(undo.Projection.AcceptedCommands, Is.EqualTo(commands.Take(prefix)));
            }

            var split = commands.Count / 2;
            var prefixSession = CreateSession();
            foreach (var command in commands.Take(split))
            {
                Assert.That(prefixSession.Execute(command).Accepted, Is.True);
            }

            var restored = CreateSession();
            var restore = restored.RestoreSnapshot(prefixSession.CaptureSnapshot());
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            var continuationCommands = new List<DirectorCommand>();
            foreach (var command in commands.Skip(split))
            {
                var continuation = restored.Execute(command);
                Assert.That(continuation.Accepted, Is.True);
                continuationCommands.Add(continuation.Command);
            }

            Assert.That(restored.Projection.DiagnosticSnapshot, Is.EqualTo(liveSnapshots.Last()));
            Assert.That(restored.Projection.CompletedRounds, Has.Count.EqualTo(1));
            Assert.That(continuationCommands, Is.EqualTo(commands.Skip(split)));
            Assert.That(restored.Projection.AcceptedCommands, Is.Empty,
                "A restored mid-match snapshot has no replay ancestry and must not create lossy Undo history.");
        }

        [TestCase(440401)]
        [TestCase(440409)]
        [TestCase(440417)]
        public void FixedSeedInvariants_PreserveOwnershipOccupancyChoicesRevealAndHistory(int seed)
        {
            var random = new Random(seed);
            var session = CreateSession();
            var red = GetCards(session, TeamSide.Red);
            var blue = GetCards(session, TeamSide.Blue);
            var initialHeroes = session.Projection.Heroes.Select(value => value.HeroId).OrderBy(value => value.Value).ToArray();

            var redCard = red[random.Next(red.Count)];
            var blueCard = blue[random.Next(blue.Count)];
            Assert.That(session.Execute(DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id)).Accepted, Is.True);
            var afterRed = session.Projection;
            Assert.That(afterRed.Commitments.Count(value =>
                value.Status == TurnCardCommitmentStatus.CardCommittedFacedown), Is.EqualTo(1));
            Assert.That(afterRed.Commitments.Any(value =>
                value.RevealedCardInstanceId.HasValue), Is.False);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id)).Accepted, Is.True);

            var projection = session.Projection;
            Assert.That(projection.Commitments.All(value =>
                value.Status == TurnCardCommitmentStatus.Revealed), Is.True);
            Assert.That(projection.Cards.All(card =>
                projection.Participants.Heroes.Any(hero => hero.Id.Equals(card.OwnerId))), Is.True);
            Assert.That(projection.Heroes.Select(value => value.HeroId).OrderBy(value => value.Value),
                Is.EqualTo(initialHeroes));
            Assert.That(projection.Heroes.Select(value => value.Coordinate).Distinct().Count(),
                Is.EqualTo(projection.Heroes.Count));
            Assert.That(projection.PendingInitiativeChoice == null ||
                projection.PendingInitiativeChoice.CandidateCardInstanceIds.All(id =>
                    projection.Cards.Any(card => card.Id.Equals(id))), Is.True);
            Assert.That(projection.AcceptedCommands, Has.Count.EqualTo(2));

            AssertAtomicRejection(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(TestContext.CurrentContext.Test.Name, Does.Contain(seed.ToString()));
        }

        private static MatchSession CreateSession(bool useAdjacentSyntheticBoard = false)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            if (useAdjacentSyntheticBoard)
            {
                return CreateAdjacentSyntheticSession(configuration);
            }

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static MatchSession CreateAdjacentSyntheticSession(
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("application-session-command-fixture"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(2, 0);
            var redSpace = new ConfiguredSpaceReference(map.Id, new SpaceId("fixture-red-space"));
            var blueSpace = new ConfiguredSpaceReference(map.Id, new SpaceId("fixture-blue-space"));
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("fixture-red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("fixture-blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("synthetic-red"),
                        "Synthetic Red",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-red-spawn")),
                        redSpace,
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("synthetic-blue"),
                        "Synthetic Blue",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-blue-spawn")),
                        blueSpace,
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static IReadOnlyList<CardInstance> GetCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards
                .Where(card => session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void StartOfficialMapMovementFour(MatchSession session)
        {
            var redCard = GetCardByInitiative(session, TeamSide.Red, 5);
            var blueCard = GetCardByInitiative(session, TeamSide.Blue, 8);
            Accept(session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id), null);
            Accept(session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id), null);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(blueCard.OwnerId));
            StartCurrentAction(session, ActionKind.Movement, null);
            Assert.That(session.Projection.PendingMovementResolution.MovementAllowance, Is.EqualTo(4));
        }

        private static CardInstance GetCardByInitiative(
            MatchSession session,
            TeamSide side,
            int initiative)
        {
            return GetCards(session, side).Single(card => session.Projection.Definitions.Single(
                definition => definition.Reference.Equals(card.DefinitionReference)).Initiative.Value == initiative);
        }

        private static IReadOnlyList<CardInstance> GetEligibleHandCards(MatchSession session, TeamSide side)
        {
            return GetCards(session, side)
                .Where(card => card.State.IsInitialInHand)
                .ToArray();
        }

        private static void Commit(
            MatchSession session,
            CardInstance red,
            CardInstance blue,
            ISet<DirectorCommandKind> kinds)
        {
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id), kinds);
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id), kinds);
            if (session.Projection.PendingInitiativeChoice != null)
            {
                var choice = session.Projection.PendingInitiativeChoice;
                Accept(session, DirectorCommand.ChooseTiedActor(
                    choice.OwningSide,
                    choice.CandidateCardInstanceIds[0]), kinds);
            }
        }

        private static void StartCurrentAction(
            MatchSession session,
            ActionKind actionKind,
            ISet<DirectorCommandKind> kinds)
        {
            var action = GetCurrentAction(session, actionKind);
            Accept(session, DirectorCommand.StartAction(action.ActionDefinitionId), kinds);
        }

        private static ActionOffer GetCurrentAction(MatchSession session, ActionKind actionKind)
        {
            return session.Projection.AvailableTurnActions.Single(offer =>
            {
                var definition = session.Projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference));
                return definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId)).Kind == actionKind;
            });
        }

        private static void HoldCurrentActor(MatchSession session, ISet<DirectorCommandKind> kinds)
        {
            Accept(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId), kinds);
        }

        private static MatchCommandResult Accept(
            MatchSession session,
            DirectorCommand command,
            ISet<DirectorCommandKind> kinds)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            kinds?.Add(command.Kind);
            return result;
        }

        private static void AssertAtomicRejection(MatchSession session, DirectorCommand command)
        {
            var before = session.Projection;
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Projection, Is.SameAs(session.Projection));
            Assert.That(result.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(result.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
            Assert.That(result.Projection.DiagnosticSnapshot, Is.EqualTo(before.DiagnosticSnapshot));
            Assert.That(result.Projection.AcceptedCommands, Is.EqualTo(before.AcceptedCommands));
        }

        private static IReadOnlyList<DirectorCommand> BuildCompletedRoundCommandSequence(MatchSession session)
        {
            var commands = new List<DirectorCommand>();
            var red = GetCards(session, TeamSide.Red);
            var blue = GetCards(session, TeamSide.Blue);
            for (var turn = 0; turn < RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                commands.Add(DirectorCommand.CommitCard(red[turn].OwnerId, red[turn].Id));
                commands.Add(DirectorCommand.CommitCard(blue[turn].OwnerId, blue[turn].Id));
                Assert.That(session.Execute(commands[commands.Count - 2]).Accepted, Is.True);
                Assert.That(session.Execute(commands[commands.Count - 1]).Accepted, Is.True);
                while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    var hold = DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId);
                    commands.Add(hold);
                    Assert.That(session.Execute(hold).Accepted, Is.True);
                }
            }

            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                var advance = DirectorCommand.AdvanceEndOfRound();
                commands.Add(advance);
                Assert.That(session.Execute(advance).Accepted, Is.True);
            }

            Assert.That(session.Projection.CompletedRounds, Has.Count.EqualTo(1));
            return commands;
        }
    }
}
