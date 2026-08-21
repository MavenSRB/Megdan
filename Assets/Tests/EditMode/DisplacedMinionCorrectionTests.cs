using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4a's after-action displaced-minion correction, derived from board position at the
    /// action boundary. The triggering action is a test-local instant Skill - one mandatory
    /// <c>MarkActorV1</c> node, the 6.8j shape - because the correction belongs to the action
    /// boundary, not to any particular displacement: whatever the action did, whoever ends up outside
    /// the Battle Zone afterwards is corrected. Minions are repositioned through the setup state and
    /// paths are shaped with obstacles, so each case pins one branch of the correction exactly.
    /// </summary>
    public sealed class DisplacedMinionCorrectionTests
    {
        private static readonly CardInstanceId InstantSkillCardId =
            new CardInstanceId("correction-boundary-skill-card");

        private static readonly ActionDefinitionId InstantSkill =
            new ActionDefinitionId("correction-boundary-skill");

        private static readonly HexCoordinate DisplacedRedAt = new HexCoordinate(-3, -1);
        private static readonly HexCoordinate SingleEntry = new HexCoordinate(-2, -1);
        private static readonly HexCoordinate OtherEntry = new HexCoordinate(-3, 0);

        [Test]
        public void ACompletedActionWithNothingDisplaced_AppendsNoCorrectionFacts()
        {
            // Every hero stands outside the Battle Zone for the whole match, so this is also the
            // structural proof that heroes are never correction subjects.
            var session = CreateSession(TeamSide.Red);
            CommitBothSides(session);

            Accept(session, DirectorCommand.StartAction(InstantSkill));

            Assert.That(CorrectionFacts(session), Is.Empty,
                "No minion miniature ended up outside the zone, so the seam appends nothing at all.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
        }

        [Test]
        public void ADisplacedMinion_FollowsItsOneShortestEmptyPathAutomatically()
        {
            var session = CreateSession(
                TeamSide.Red,
                Reposition("red-melee-01", DisplacedRedAt),
                new[] { OtherEntry });
            CommitBothSides(session);

            Accept(session, DirectorCommand.StartAction(InstantSkill));

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "One shortest endpoint is no choice at all, so nothing pauses.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(SingleEntry),
                "The minion followed its one shortest empty path back into the zone.");
            Assert.That(
                CorrectionFacts(session).Select(fact => fact.Kind),
                Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.DisplacedMinionCorrectionStarted,
                    CardActionResolutionEventKind.DisplacedMinionCorrectionMoved,
                    CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted
                }));
            var moved = CorrectionFacts(session).Single(fact =>
                fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionMoved);
            Assert.That(moved.CorrectionEvent.Origin, Is.EqualTo(DisplacedRedAt));
            Assert.That(moved.CorrectionEvent.Destination, Is.EqualTo(SingleEntry));
            Assert.That(moved.CorrectionEvent.CorrectionKind, Is.EqualTo(DisplacedMinionCorrectionKind.Move));
            AssertActionResolvedAndTurnAdvanced(session);
        }

        [Test]
        public void EquallyShortPaths_PauseOnTheTeamsRouteChoice_AndRefuseIllegalAnswers()
        {
            // Without the wall, (-3,-1) has two cost-1 entries: the team chooses (p.18 via the
            // amendment), and stale or illegal input preserves the authoritative pending choice.
            var session = CreateSession(TeamSide.Red, Reposition("red-melee-01", DisplacedRedAt));
            CommitBothSides(session);
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Stage, Is.EqualTo(DisplacedMinionCorrectionStage.SelectDestination));
            Assert.That(pending.Kind, Is.EqualTo(DisplacedMinionCorrectionKind.Move));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(
                pending.DestinationCandidates.Select(candidate => candidate.Destination),
                Is.EqualTo(new[] { OtherEntry, SingleEntry }),
                "Both cost-1 endpoints, in axial order, each with its one disclosed route.");
            Assert.That(
                pending.DestinationCandidates.Select(candidate =>
                    (candidate.Route.Count, candidate.Route.First(), candidate.Route.Last())),
                Is.EqualTo(pending.DestinationCandidates.Select(candidate =>
                    (2, DisplacedRedAt, candidate.Destination))),
                "Each disclosed route runs origin to endpoint, one coordinate per movement point.");

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.ChooseCorrectionDestination(
                new DisplacedMinionCorrectionCommand(
                    DisplacedMinionCorrectionCommandKind.SelectDestination,
                    pending.ResolutionId, pending.ChoiceId, pending.ChoosingSide,
                    pending.SubjectMinionId, new HexCoordinate(99, 99))));
            Assert.That(illegal.Accepted, Is.False,
                "A destination the derivation never offered is refused.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal is atomic; the pending choice is exactly what it was.");

            var wrongSide = session.Execute(DirectorCommand.ChooseCorrectionDestination(
                new DisplacedMinionCorrectionCommand(
                    DisplacedMinionCorrectionCommandKind.SelectDestination,
                    pending.ResolutionId, pending.ChoiceId, TeamSide.Blue,
                    pending.SubjectMinionId, SingleEntry)));
            Assert.That(wrongSide.Accepted, Is.False,
                "Only the displaced minion's own team may answer.");

            Accept(session, DirectorCommand.ChooseCorrectionDestination(
                pending.SelectInvocations.Single(invocation =>
                    invocation.Destination.Equals(SingleEntry))));
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(SingleEntry));
            AssertActionResolvedAndTurnAdvanced(session);
        }

        [Test]
        public void SeveralDisplacedMinionsOfOneTeam_PauseOnThatTeamsSubjectChoice()
        {
            // The 2026-08-16 amendment: the owning team chooses which of its minions corrects next.
            var session = CreateSession(
                TeamSide.Red,
                Reposition(
                    ("red-melee-01", DisplacedRedAt),
                    ("red-melee-02", new HexCoordinate(-1, -3))),
                new[] { OtherEntry });
            CommitBothSides(session);
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Stage, Is.EqualTo(DisplacedMinionCorrectionStage.SelectSubject));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(
                pending.SubjectCandidates.Select(subject => subject.MinionId.Value),
                Is.EqualTo(new[] { "red-melee-01", "red-melee-02" }),
                "Both correctable displaced minions, in canonical identity order.");

            Accept(session, DirectorCommand.ChooseCorrectionSubject(
                pending.SelectInvocations.Single(invocation =>
                    invocation.MinionId.Value == "red-melee-02")));

            Assert.That(Coordinate(session, "red-melee-02"), Is.EqualTo(new HexCoordinate(-1, -2)),
                "The chosen subject corrected first, along its one shortest path.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(SingleEntry),
                "The remaining subject was then the only one left, so it corrected without a prompt.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            AssertActionResolvedAndTurnAdvanced(session);
        }

        [Test]
        public void WithNoEmptyPath_TheMinionIsPlacedIntoANearestEmptyZoneSpace()
        {
            // Every zone space bordering the outside is walled, so no empty path enters the zone at
            // all; the fallback Places the minion into a nearest empty interior space and emits Place
            // rather than Movement facts.
            var session = CreateSession(
                TeamSide.Red,
                Reposition("red-melee-01", DisplacedRedAt),
                ZonePerimeterWall());
            CommitBothSides(session);
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null, "The interior offers several equally near spaces.");
            Assert.That(pending.Kind, Is.EqualTo(DisplacedMinionCorrectionKind.Place));
            Assert.That(pending.DestinationCandidates.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(
                pending.DestinationCandidates.Select(candidate => candidate.Route.Count),
                Is.All.EqualTo(0),
                "Place is not movement (p.18): a Place candidate carries no route at all.");

            var destination = pending.DestinationCandidates[0].Destination;
            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[0]));

            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(destination));
            Assert.That(
                CorrectionFacts(session).Select(fact => fact.Kind),
                Does.Contain(CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced));
            Assert.That(
                CorrectionFacts(session).Select(fact => fact.Kind),
                Has.None.EqualTo(CardActionResolutionEventKind.DisplacedMinionCorrectionMoved),
                "The fallback emits Place rather than Movement facts.");
            AssertActionResolvedAndTurnAdvanced(session);
        }

        [Test]
        public void TheFallbackPlace_IsTheGeneralPlaceRatherThanAMinimumOfItsOwn()
        {
            // Milestone 6.5's fifth acceptance check. The fallback used to derive and commit its own
            // minimum placement; it now states a PlacePolicy against the shared operation, so the
            // candidates a player is offered are exactly what the general destination query answers
            // for that declaration - board source, minion miniature, nearest empty spaces of the
            // declared Battle Zone - and the verified outcome above is unchanged.
            var session = CreateSession(
                TeamSide.Red,
                Reposition("red-melee-01", DisplacedRedAt),
                ZonePerimeterWall());
            CommitBothSides(session);
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Kind, Is.EqualTo(DisplacedMinionCorrectionKind.Place));

            var board = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions).Board;
            var declared = new PlacePolicy(
                PlaceSourceContainer.Board,
                PlaceEligiblePieceKind.MinionMiniature,
                PlaceDestinationScope.NearestEmptySpacesInScope,
                0,
                PlaceChooserKind.OwningTeam,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);

            Assert.That(
                pending.DestinationCandidates.Select(candidate => candidate.Destination),
                Is.EqualTo(PlaceDestinationQuery.FindDestinations(
                    declared,
                    board,
                    DisplacedRedAt,
                    BattleZoneQuery.FindCurrentBattleZoneCoordinates(board))),
                "The correction offers exactly the general Place's own answer for its declaration.");
        }

        [Test]
        public void WithNoEmptyZoneSpaceEither_TheMinionStaysWhereItEndedUp()
        {
            // Open rules question 10's pinned behavior: a full Battle Zone strands the minion as a
            // typed outcome rather than a fault, and the action still completes.
            var session = CreateSession(
                TeamSide.Red,
                Reposition("red-melee-01", DisplacedRedAt),
                FullZoneWall());
            CommitBothSides(session);
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(DisplacedRedAt),
                "Nothing could correct it, so it stays where it ended up.");
            var facts = CorrectionFacts(session);
            Assert.That(facts.Select(fact => fact.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.DisplacedMinionCorrectionStarted,
                CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace,
                CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted
            }));
            var stranded = facts.Single(fact =>
                fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace);
            Assert.That(stranded.CorrectionEvent.Origin, Is.EqualTo(stranded.CorrectionEvent.Destination),
                "A left-in-place outcome records the one coordinate twice: nothing moved.");
            AssertActionResolvedAndTurnAdvanced(session);
        }

        [TestCase(TeamSide.Red)]
        [TestCase(TeamSide.Blue)]
        public void CrossTeamCorrections_ResolveTheTieBreakerSideFirst_WithoutFlippingTheCoin(
            TeamSide revealedSide)
        {
            // One displaced minion per team, each with a single shortest endpoint so both corrections
            // run automatically and only the order can differ. Running the same board under both
            // revealed sides proves the order follows the coin rather than any fixed team order.
            var session = CreateSession(
                revealedSide,
                Reposition(
                    ("red-melee-01", DisplacedRedAt),
                    ("blue-melee-02", new HexCoordinate(1, 3))),
                new[] { OtherEntry, new HexCoordinate(0, 3) });
            CommitBothSides(session);
            var tieBreakerBefore = session.Projection.TieBreakerState;
            Accept(session, DirectorCommand.StartAction(InstantSkill));

            var moved = CorrectionFacts(session)
                .Where(fact => fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionMoved)
                .Select(fact => fact.CorrectionEvent.MinionId.Value)
                .ToArray();
            var expectedFirst = revealedSide == TeamSide.Red ? "red-melee-01" : "blue-melee-02";
            var expectedSecond = revealedSide == TeamSide.Red ? "blue-melee-02" : "red-melee-01";
            Assert.That(moved, Is.EqualTo(new[] { expectedFirst, expectedSecond }),
                "The Tie-Breaker side's minion resolves first.");
            Assert.That(session.Projection.TieBreakerState.RevealedSide,
                Is.EqualTo(tieBreakerBefore.RevealedSide),
                "Ordering reads the coin; it never flips it.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static void AssertActionResolvedAndTurnAdvanced(MatchSession session)
        {
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(InstantSkillCardId))
                    .State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "The source card resolves only when the whole action, correction included, completes.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId,
                Is.Not.EqualTo(InstantSkillCardId),
                "Initiative advances after the correction completes, exactly as after any action.");
        }

        private static HexCoordinate Coordinate(MatchSession session, string minionId) =>
            session.Projection.Minions.Single(minion => minion.MinionId.Value == minionId).Coordinate;

        private static CardActionResolutionEvent[] CorrectionFacts(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.SkillActionProgressed)
                .Select(value => value.TurnEvent.CardActionEvent)
                .Where(fact =>
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionStarted ||
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested ||
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionMoved ||
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced ||
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace ||
                    fact.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted)
                .ToArray();

        private static (string, HexCoordinate)[] Reposition(string minionId, HexCoordinate at) =>
            new[] { (minionId, at) };

        private static (string, HexCoordinate)[] Reposition(
            params (string, HexCoordinate)[] moves) => moves;

        /// <summary>Every Battle-Zone space that borders outside playable ground, minus minion homes.</summary>
        private static HexCoordinate[] ZonePerimeterWall()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var zone = new HashSet<HexCoordinate>(BattleZoneQuery.FindStartingBattleZoneCoordinates(map));
            var playable = new HashSet<HexCoordinate>(map.PlayableCoordinates);
            return zone
                .Where(space => space.GetNeighbors().Any(neighbor =>
                    playable.Contains(neighbor) && !zone.Contains(neighbor)))
                .ToArray();
        }

        /// <summary>Every Battle-Zone space, so nothing empty remains for the fallback Place.</summary>
        private static HexCoordinate[] FullZoneWall()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return BattleZoneQuery.FindStartingBattleZoneCoordinates(map).ToArray();
        }

        private static MatchSession CreateSession(
            TeamSide revealedSide,
            (string, HexCoordinate)[] minionMoves = null,
            IEnumerable<HexCoordinate> obstacles = null)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(revealedSide);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var moves = (minionMoves ?? Array.Empty<(string, HexCoordinate)>())
                .ToDictionary(move => move.Item1, move => move.Item2, StringComparer.Ordinal);
            var minions = fullSetup.Minions.Select(minion =>
                moves.TryGetValue(minion.MinionId.Value, out var at)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId, minion.TeamId, minion.TeamSide, minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(
                            minion.OccupiedSpace.MapId, new SpaceId($"q{at.Q}_r{at.R}")),
                        at)
                    : minion).ToArray();
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                minions);

            var occupied = new HashSet<HexCoordinate>(minions.Select(minion => minion.Coordinate));
            var walls = (obstacles ?? Enumerable.Empty<HexCoordinate>())
                .Where(coordinate => !occupied.Contains(coordinate))
                .Select((coordinate, index) => new ObstacleOccupancy(
                    new ObstacleId($"correction-wall-{index}"), coordinate));

            var definition = CreateInstantSkillDefinition();
            var instance = CardInstance.CreateInHand(
                InstantSkillCardId,
                definition.Reference,
                configuration.RedHand.Instances[0].OwnerId);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions.Concat(new[] { definition }),
                configuration.CardInstances.Concat(new[] { instance }),
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                walls);
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static void CommitBothSides(MatchSession session)
        {
            var owner = session.Projection.Cards
                .Single(card => card.Id.Equals(InstantSkillCardId)).OwnerId;
            Accept(session, DirectorCommand.CommitCard(owner, InstantSkillCardId));

            var other = session.Projection.Cards.First(card =>
                !card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(other.OwnerId, other.Id));

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "Initiative 30 against a placeholder card cannot tie.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId,
                Is.EqualTo(InstantSkillCardId));
        }

        /// <summary>
        /// One mandatory MarkActor node: the action completes at its own start, so the correction is
        /// the only thing that can pause it - which isolates exactly the boundary under test.
        /// </summary>
        private static CardDefinition CreateInstantSkillDefinition()
        {
            var root = new EffectNodeId("correction-boundary-root");
            var effect = new EffectDefinition(
                new EffectDefinitionId("correction-boundary-skill-effect"),
                "phase-6-4a-test-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.MarkActorV1,
                        outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) })
                });

            return new CardDefinition(
                new CardDefinitionId("correction-boundary-skill-test"),
                "phase-6-4a-test-rev-1",
                "TEST - Correction Boundary Skill",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        InstantSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }
    }
}
