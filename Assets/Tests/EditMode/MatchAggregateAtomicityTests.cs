using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The aggregate's rejected-command atomicity guarantee: a refused operation that had already
    /// moved authoritative state is rolled back and reported as such, in every build configuration.
    /// </summary>
    /// <remarks>
    /// This branch had no coverage at all before milestone 6.1o, which is how 6.1i was able to gate
    /// it behind a diagnostics mode without any test noticing that the shipping configuration had
    /// stopped honouring the guarantee.
    ///
    /// The plan these tests commit is written rather than found, and that is the nature of the
    /// subject rather than a shortcut: the boundary promises something about *any* plan that refuses
    /// after mutating, so a contract test states the plan instead of hunting for one. No live command
    /// path is known to mutate and then refuse. Waiting for one to appear is what left the branch
    /// untested.
    /// </remarks>
    public sealed class MatchAggregateAtomicityTests
    {
        /// <summary>
        /// The type-level half, and the one that closes the finding. The defect was not that the
        /// comparison was wrong; it was that a build could be configured to skip it. That is now
        /// unrepresentable rather than merely discouraged, which is the same shape milestone 6.1g
        /// gave snapshot restore.
        /// </summary>
        [Test]
        public void MatchSession_ExposesNoConfigurationThatCanSkipTheAtomicityCheck()
        {
            Assert.That(
                typeof(MatchSession).Assembly.GetTypes().Where(type => type.Name.Contains("DiagnosticsMode")),
                Is.Empty,
                "A diagnostics mode that gates the rejected-command comparison also gates the " +
                "rollback, because the comparison is what decides whether to roll back. Milestone " +
                "6.1i introduced `MatchDiagnosticsMode` for this and selected `Off` for every " +
                "non-development build; it was removed rather than re-defaulted.");

            var constructors = typeof(MatchSession).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(constructors, Has.Length.EqualTo(1),
                "One public constructor means there is one session shape, so no caller can select " +
                "a weaker guarantee.");
            Assert.That(constructors[0].GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(MatchSessionInitialState) }));
        }

        /// <summary>
        /// The behavioural half. A plan that moves a hero and then refuses must leave the board
        /// exactly as it found it, and must say that it did so rather than reporting a clean refusal.
        /// </summary>
        [Test]
        public void RejectedPlanThatMutatedState_IsRolledBackAndReportedAsAnAtomicityViolation()
        {
            var aggregate = CreateAggregate();
            var board = aggregate.Board;
            var origin = board.Occupancies[0];
            var destination = FindFreeNeighbour(board, origin);
            var occupanciesBefore = DescribeOccupancies(board);

            var mutated = false;
            var outcome = aggregate.Commit<MutatingRefusal>(() =>
            {
                mutated = board.TryCommit(new AdjacentMoveCommand(
                    origin.HeroId,
                    origin.Coordinate,
                    destination)).Accepted;
                return new MutatingRefusal();
            });

            Assert.That(mutated, Is.True,
                "The plan has to actually mutate authoritative state, or it proves nothing.");
            Assert.That(outcome.Status, Is.EqualTo(MatchCommitStatus.AtomicityViolation),
                "A refusal that moved state is not a clean refusal, and reporting it as one would " +
                "hand a caller a rejection reason for a defect in the engine.");
            Assert.That(DescribeOccupancies(aggregate.Board), Is.EqualTo(occupanciesBefore),
                "The rollback restores the pre-image, so the move the refused plan made must be gone.");
        }

        /// <summary>
        /// A refusal that moved nothing must stay an ordinary refusal. Without this, a boundary that
        /// reported every rejection as an atomicity violation would pass the test above.
        /// </summary>
        [Test]
        public void RejectedPlanThatMovedNothing_IsReportedAsAnOrdinaryRefusal()
        {
            var aggregate = CreateAggregate();

            var outcome = aggregate.Commit<MutatingRefusal>(() => new MutatingRefusal());

            Assert.That(outcome.Status, Is.EqualTo(MatchCommitStatus.Rejected));
        }

        /// <summary>
        /// `RISK-STATE-002`, made mechanical. The rollback restores the four graphs by deserializing
        /// a snapshot, so it preserves exactly the state that snapshot round-trips; anything else the
        /// aggregate holds has to be carried in the pre-image explicitly, as the two rule-event
        /// streams are.
        /// </summary>
        /// <remarks>
        /// The register's mitigation for that residual is a sentence - "any new unpersisted aggregate
        /// state must be added to the pre-image in the same milestone that introduces it" - which
        /// depends on somebody remembering. This is that sentence as a check.
        ///
        /// What it catches: a field added without a decision. What it does not catch: a field
        /// classified as a serialized graph that the serializer does not in fact persist. The
        /// classification is the author's claim; this fixture only insists that the claim be made.
        /// </remarks>
        [Test]
        public void MatchAggregate_HoldsNoStateThatARollbackWouldSilentlyDiscard()
        {
            // Restored by deserializing the pre-image's authoritative payload.
            var serializedGraphs = new[] { "configuration", "board", "cardActions", "round" };

            // Not restored by the payload, so carried in `MatchPreImage` by name.
            var carriedStreams = new[] { "eventLog", "roundEvents" };

            // Never authoritative: supplied at construction, or derived from state that is.
            var constructionInputs = new[] { "initialState" };
            // The memoized capture, and milestone 7.9b's completed-round fragment cache it reads
            // through: both are re-derivable from the graphs, and the cache re-validates itself
            // against the round authority's own records on every capture, so a rollback that
            // ignores both is exactly as correct as one that rebuilds them.
            var derivedCaches = new[] { "memoizedAuthoritative", "completedRoundsCache" };

            // Milestone 8.35z43. Not state the aggregate holds ABOUT THE MATCH at all: a
            // one-shot arming flag for a friend-assembly seam, armed by a fixture and consumed by
            // the very next capture. It is deliberately its own category rather than filed under
            // `derivedCaches`, because a cache is re-derivable from authoritative state and this is
            // not derivable from anything - it is an instruction that has not been carried out yet.
            //
            // The classification decision this fixture asks for: a rollback must NOT preserve it.
            // Nothing about the match depends on it, and a rollback that carried it forward would
            // re-arm a seam the capture had already spent. It is null in every shipped path.
            var testSeams = new[] { "nextPreImagePayloadCorruptionForTest" };

            var classified = new HashSet<string>(
                serializedGraphs.Concat(carriedStreams).Concat(constructionInputs)
                    .Concat(derivedCaches).Concat(testSeams),
                StringComparer.Ordinal);
            var actual = typeof(MatchAggregate)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(field => field.Name)
                .ToArray();

            Assert.That(actual, Is.Not.Empty, "Reading no fields would pass by looking at nothing.");
            Assert.That(classified.Except(actual, StringComparer.Ordinal), Is.Empty,
                "This fixture classifies a field the aggregate no longer has. Remove the stale name.");
            Assert.That(actual.Where(name => !classified.Contains(name)), Is.Empty,
                "A new field on MatchAggregate has to be classified here before it can be trusted " +
                "through a rollback. Decide which it is: restored by the authoritative payload " +
                "(add it to the serializer and to `serializedGraphs`); authoritative but not " +
                "persisted (carry it in `MatchPreImage` and add it to `carriedStreams`); or neither " +
                "authoritative nor persisted (`constructionInputs`, `derivedCaches` or " +
                "`testSeams`). Choosing wrongly loses the state at the next refused command, which " +
                "is `RISK-STATE-002`.");
        }

        /// <summary>
        /// The behavioural companion to the classification above, over state a real command sequence
        /// can reach: everything the aggregate holds is restored by a rollback, not merely the graphs.
        /// </summary>
        [Test]
        public void Rollback_RestoresTheAuthoritativePayloadAndBothEventStreamsExactly()
        {
            var session = CreateSession();
            var cards = EligibleHandCards(session);
            Assert.That(cards.Count, Is.GreaterThanOrEqualTo(2), "The scenario needs two committable cards.");
            Assert.That(session.Execute(DirectorCommand.CommitCard(cards[0].OwnerId, cards[0].Id)).Accepted, Is.True);

            var aggregate = session.aggregate;
            var preImage = aggregate.CapturePreImage();
            var payloadBefore = preImage.AuthoritativePayload;
            var eventLogBefore = preImage.EventLogEntries.ToArray();
            var roundEventsBefore = preImage.RoundEvents.ToArray();
            Assert.That(roundEventsBefore, Is.Not.Empty,
                "The pre-image must span state in both streams, or the round-event half proves nothing.");

            Assert.That(session.Execute(DirectorCommand.CommitCard(cards[1].OwnerId, cards[1].Id)).Accepted, Is.True);
            Assert.That(aggregate.CaptureAuthoritativeSnapshot(), Is.Not.EqualTo(payloadBefore),
                "The further commands must actually move state, or the rollback restores nothing.");

            aggregate.Rollback(preImage);

            Assert.That(aggregate.CaptureAuthoritativeSnapshot(), Is.EqualTo(payloadBefore));
            Assert.That(aggregate.EventLogEntries, Is.EqualTo(eventLogBefore),
                "The whole-match rule-event log is not carried by the authoritative payload, so a " +
                "rollback that only restored the graphs would silently truncate or keep it.");
            Assert.That(aggregate.RoundEvents, Is.EqualTo(roundEventsBefore),
                "The current round's ordered events are the state `RISK-STATE-002` was raised for.");
        }

        /// <summary>A plan that refuses, so the boundary must keep nothing it did.</summary>
        private sealed class MutatingRefusal : IMatchCommitPlan
        {
            public bool Accepted => false;

            public IReadOnlyList<RoundResolutionEvent> Events => Array.Empty<RoundResolutionEvent>();
        }

        private static HexCoordinate FindFreeNeighbour(AdjacentMoveAuthority board, HeroOccupancy origin)
        {
            var occupied = board.Occupancies.Select(occupancy => occupancy.Coordinate)
                .Concat(board.ObstacleOccupancies.Select(occupancy => occupancy.Coordinate))
                .Concat(board.Minions.Select(minion => minion.Coordinate))
                .ToArray();

            // `HexCoordinate` is a struct whose default is a real board space, so emptiness is
            // tested rather than compared against `default`.
            var free = origin.Coordinate.GetNeighbors()
                .Where(neighbour => board.BoardCoordinates.Contains(neighbour))
                .Where(neighbour => !occupied.Contains(neighbour))
                .ToArray();

            Assert.That(free, Is.Not.Empty,
                "The configured setup must leave the first hero somewhere to legally move.");
            return free[0];
        }

        // Ordered by hero, not by the authority's own list order: the rollback rebuilds the board by
        // deserializing the pre-image, and the snapshot does not promise to restore occupancy order.
        // Placement is what this asserts, and ordering it here keeps the failure message readable.
        private static string[] DescribeOccupancies(AdjacentMoveAuthority board)
        {
            return board.Occupancies
                .Select(occupancy => $"{occupancy.HeroId.Value}@{occupancy.Coordinate}")
                .OrderBy(description => description, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<CardInstance> EligibleHandCards(MatchSession session)
        {
            return session.Projection.Cards
                .Where(card => card.State.IsInitialInHand)
                .GroupBy(card => session.Projection.Participants.GetTeamForHero(card.OwnerId).Side)
                .Select(group => group.OrderBy(card => card.Id.Value, StringComparer.Ordinal).First())
                .ToArray();
        }

        /// <summary>The aggregate alone, which is all a boundary contract test needs.</summary>
        private static MatchAggregate CreateAggregate()
        {
            var aggregate = new MatchAggregate(CreateInitialState());
            aggregate.RebuildInitialState(true);
            return aggregate;
        }

        private static MatchSession CreateSession()
        {
            return new MatchSession(CreateInitialState());
        }

        private static MatchSessionInitialState CreateInitialState()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }
    }
}
