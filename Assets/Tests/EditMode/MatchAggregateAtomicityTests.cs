using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
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
    /// This branch had no coverage at all before this fixture, which is how milestone 6.1i was able
    /// to gate it behind a diagnostics mode without any test noticing that the shipping
    /// configuration had stopped honouring the guarantee.
    ///
    /// The commit boundary is `internal` and there is no `InternalsVisibleTo`, so the boundary is
    /// driven reflectively rather than through `IMatchSession`. That is deliberate and not a
    /// workaround for missing public API: no *live* command path is known to mutate and then refuse,
    /// so a plan that does so has to be synthesized. The alternative - waiting for a real mutating
    /// refusal to appear - is exactly the state that left this branch untested.
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
            var applicationAssembly = typeof(MatchSession).Assembly;

            Assert.That(
                applicationAssembly.GetTypes().Where(type => type.Name.Contains("DiagnosticsMode")),
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
            var session = CreateSession();
            var aggregate = GetAggregate(session);
            var board = (AdjacentMoveAuthority)AggregateProperty(aggregate, "Board");

            var origin = board.Occupancies[0];
            var destination = FindFreeNeighbour(board, origin);
            var occupanciesBefore = DescribeOccupancies(board);

            // Asserted after the boundary returns, not inside the plan: a throw inside the plan is
            // caught and reported as a fault, which would quietly replace the outcome under test.
            var mutated = false;
            var outcome = Commit(aggregate, () =>
                mutated = board.TryCommit(new AdjacentMoveCommand(
                    origin.HeroId,
                    origin.Coordinate,
                    destination)).Accepted);

            Assert.That(mutated, Is.True,
                "The probe has to actually mutate authoritative state, or it proves nothing.");
            Assert.That(StatusName(outcome), Is.EqualTo("AtomicityViolation"),
                "A refusal that moved state is not a clean refusal, and reporting it as one would " +
                "hand a caller a rejection reason for a defect in the engine.");

            var restoredBoard = (AdjacentMoveAuthority)AggregateProperty(GetAggregate(session), "Board");
            Assert.That(DescribeOccupancies(restoredBoard), Is.EqualTo(occupanciesBefore),
                "The rollback restores the pre-image, so the move the refused plan made must be gone.");
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

        // The boundary is internal, so the plan it takes is a private nested type this assembly
        // cannot name. The delegate is therefore built as an expression tree: the mutation runs, and
        // the block's value is a refusing plan produced by the type's own factory.
        private static object Commit(object aggregate, Action mutate)
        {
            var applicationAssembly = typeof(MatchSession).Assembly;
            var planType = applicationAssembly.GetType("GuardsDigital.Application.MatchSession+DispatchResult", true);
            var unavailable = planType.GetMethod("Unavailable", BindingFlags.Public | BindingFlags.Static);
            Assert.That(unavailable, Is.Not.Null, "DispatchResult.Unavailable is the refusing factory.");

            var body = Expression.Block(
                Expression.Call(Expression.Constant(mutate), typeof(Action).GetMethod(nameof(Action.Invoke))),
                Expression.Call(unavailable, Expression.Constant("Synthetic mutating refusal.")));
            var plan = Expression.Lambda(typeof(Func<>).MakeGenericType(planType), body).Compile();

            var commit = aggregate.GetType()
                .GetMethod("Commit", BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(planType);
            return commit.Invoke(aggregate, new object[] { plan });
        }

        private static string StatusName(object outcome)
        {
            return outcome.GetType().GetProperty("Status").GetValue(outcome).ToString();
        }

        private static object GetAggregate(MatchSession session)
        {
            var field = typeof(MatchSession).GetField("aggregate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "MatchSession holds its aggregate in a private field.");
            return field.GetValue(session);
        }

        private static object AggregateProperty(object aggregate, string name)
        {
            return aggregate.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(aggregate);
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
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
    }
}
