using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Rulebook p.18's Push geometry, milestone 6.4. Every case names the sentence it pins.
    ///
    /// The boards are a single East-West file so the push direction is unambiguous and the interesting
    /// variable is what stands on the line. The pusher sits West of the target throughout, so "directly
    /// away from the pushing unit" is East.
    /// </summary>
    public sealed class PushPathQueryTests
    {
        private static readonly HeroId Pusher = new HeroId("pusher-hero");
        private static readonly HeroId Target = new HeroId("target-hero");
        private static readonly HeroId Blocker = new HeroId("blocker-hero");

        private static readonly HexCoordinate PusherAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate TargetAt = new HexCoordinate(1, 0);

        [Test]
        public void AnUnobstructedPush_MovesTheFullRequestedDistance()
        {
            var board = CreateLine(-1, 6);

            var result = Resolve(board, requestedDistance: 3);

            Assert.That(result.HasDirection, Is.True);
            Assert.That(result.ActualDistance, Is.EqualTo(3));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(4, 0)));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.ReachedRequestedDistance));
            Assert.That(result.Moves, Is.True);
        }

        [Test]
        public void AnObstacleShortOfTheDistance_MovesAsFarAsPossible()
        {
            // p.18: "If the target cannot be moved the full distance, move it as far as possible".
            var board = CreateLine(-1, 6, blockerAt: new HexCoordinate(4, 0));

            var result = Resolve(board, requestedDistance: 3);

            Assert.That(result.ActualDistance, Is.EqualTo(2));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.BlockedByObstacle));
        }

        [Test]
        public void AnObstacleImmediatelyBehindTheTarget_PushesZeroSpaces()
        {
            // p.18: "this can result in pushing it 0 spaces" - a legal outcome, not a refusal.
            var board = CreateLine(-1, 6, blockerAt: new HexCoordinate(2, 0));

            var result = Resolve(board, requestedDistance: 3);

            Assert.That(result.HasDirection, Is.True, "A zero-space push still has a direction.");
            Assert.That(result.ActualDistance, Is.EqualTo(0));
            Assert.That(result.Endpoint, Is.EqualTo(TargetAt));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.BlockedByObstacle));
            Assert.That(result.Moves, Is.False);
        }

        [Test]
        public void TheEdgeOfTheMap_StopsThePushAndIsNotAnObstacle()
        {
            var board = CreateLine(-1, 3);

            var result = Resolve(board, requestedDistance: 4);

            Assert.That(result.ActualDistance, Is.EqualTo(2));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(
                result.StoppingReason,
                Is.EqualTo(PushStoppingReason.LeftBoard),
                "Off the map is nothing rather than an obstacle, so it reports its own reason.");
        }

        [Test]
        public void APushIgnoringObstacles_CrossesThemAndLandsOnTheFirstEmptySpaceBeyond()
        {
            // p.18: "If the push is 'ignoring obstacles' ... the pushed target is moved through
            // obstacles as long as it can reach an empty space."
            var board = CreateLine(-1, 6, blockerAt: new HexCoordinate(2, 0));

            var result = Resolve(board, requestedDistance: 3, pushIgnoresObstacles: true);

            Assert.That(result.ActualDistance, Is.EqualTo(3));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(4, 0)));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.ReachedRequestedDistance));
        }

        [Test]
        public void ATargetHoldingMoveThrough_CrossesObstaclesWithoutTheEffectIgnoringThem()
        {
            // p.18 lists the two as alternatives in one sentence: the push ignoring obstacles, OR the
            // obstacle being one the target can move through.
            var board = CreateLine(-1, 6, blockerAt: new HexCoordinate(2, 0));

            var result = PushPathQuery.Resolve(
                board,
                PusherAt,
                TargetAt,
                3,
                RuleObject.ForHero(Target),
                pushIgnoresObstacles: false,
                modifications: MoveThroughFor(Target));

            Assert.That(result.ActualDistance, Is.EqualTo(3));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(4, 0)));
        }

        [Test]
        public void APermittedTargetWhoseFullDistanceLandsOnAnObstacle_KeepsTheLastEmptySpaceCrossed()
        {
            // The permission grants transit, never an endpoint: p.18 still allows no unit to end its
            // movement on an obstacle, so "as long as it can reach an empty space" backs the target off.
            var board = CreateLine(-1, 6, blockerAt: new HexCoordinate(4, 0));

            var result = Resolve(board, requestedDistance: 3, pushIgnoresObstacles: true);

            Assert.That(result.ActualDistance, Is.EqualTo(2));
            Assert.That(result.Endpoint, Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.BlockedByObstacle));
        }

        [Test]
        public void TerrainBlocksAnOrdinaryPushAndIsCrossedByAPermittedOne()
        {
            var board = CreateTerrainLine();

            var ordinary = Resolve(board, requestedDistance: 3);
            Assert.That(ordinary.ActualDistance, Is.EqualTo(0));
            Assert.That(ordinary.StoppingReason, Is.EqualTo(PushStoppingReason.BlockedByObstacle));

            var permitted = Resolve(board, requestedDistance: 3, pushIgnoresObstacles: true);
            Assert.That(
                permitted.ActualDistance,
                Is.EqualTo(3),
                "Milestone 6.8e makes terrain a crossable obstacle rather than off-board.");
            Assert.That(permitted.Endpoint, Is.EqualTo(new HexCoordinate(4, 0)));
        }

        [Test]
        public void ATargetOffEveryFileFromThePusher_IsRefusedRatherThanPushedZero()
        {
            var board = CreateLine(-1, 6);

            var result = PushPathQuery.Resolve(
                board,
                PusherAt,
                new HexCoordinate(3, -1),
                3,
                RuleObject.ForHero(Target),
                pushIgnoresObstacles: false,
                modifications: RuleModificationSet.Empty);

            Assert.That(result.HasDirection, Is.False);
            Assert.That(
                result.StoppingReason,
                Is.EqualTo(PushStoppingReason.NotInStraightLine),
                "No direction is a different outcome from a direction that cannot be used.");
            Assert.That(result.ActualDistance, Is.EqualTo(0));
        }

        [Test]
        public void AZeroDistanceRequest_ResolvesToTheTargetsOwnSpace()
        {
            var board = CreateLine(-1, 6);

            var result = Resolve(board, requestedDistance: 0);

            Assert.That(result.HasDirection, Is.True);
            Assert.That(result.ActualDistance, Is.EqualTo(0));
            Assert.That(result.Endpoint, Is.EqualTo(TargetAt));
            Assert.That(result.StoppingReason, Is.EqualTo(PushStoppingReason.ReachedRequestedDistance));
        }

        [Test]
        public void EveryDirectionIsSupported_NotOnlyTheEastWestFixtureAxis()
        {
            foreach (var direction in new[]
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            })
            {
                var pusher = new HexCoordinate(0, 0);
                var target = pusher.GetNeighbor(direction);
                var coordinates = Enumerable.Range(0, 5)
                    .Select(step => StraightLineQuery.Step(pusher, direction, step))
                    .ToArray();
                var board = new AdjacentMoveAuthority(
                    coordinates,
                    new[] { new HeroOccupancy(Pusher, pusher), new HeroOccupancy(Target, target) });

                var result = PushPathQuery.Resolve(
                    board, pusher, target, 2, RuleObject.ForHero(Target), false, RuleModificationSet.Empty);

                Assert.That(result.ActualDistance, Is.EqualTo(2), $"Direction {direction}.");
                Assert.That(result.Endpoint, Is.EqualTo(StraightLineQuery.Step(pusher, direction, 3)));
            }
        }

        [Test]
        public void ANullModificationSet_IsRefusedRatherThanTreatedAsNoModifications()
        {
            var board = CreateLine(-1, 6);

            Assert.That(
                () => PushPathQuery.Resolve(
                    board, PusherAt, TargetAt, 1, RuleObject.ForHero(Target), false, null),
                Throws.ArgumentNullException,
                "Milestone 6.8f/6.8i: RuleModificationSet.Empty is the named absence, not null.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static PushPathResult Resolve(
            AdjacentMoveAuthority board,
            int requestedDistance,
            bool pushIgnoresObstacles = false) =>
            PushPathQuery.Resolve(
                board,
                PusherAt,
                TargetAt,
                requestedDistance,
                RuleObject.ForHero(Target),
                pushIgnoresObstacles,
                RuleModificationSet.Empty);

        /// <summary>
        /// A playable East-West file from (<paramref name="fromQ"/>,0) to (<paramref name="toQ"/>,0)
        /// with the pusher at (0,0) and the target at (1,0), plus an optional third hero standing on
        /// the line as an ordinary obstacle.
        /// </summary>
        private static AdjacentMoveAuthority CreateLine(int fromQ, int toQ, HexCoordinate? blockerAt = null)
        {
            var coordinates = Enumerable.Range(fromQ, toQ - fromQ + 1)
                .Select(q => new HexCoordinate(q, 0))
                .ToArray();
            var occupancies = new System.Collections.Generic.List<HeroOccupancy>
            {
                new HeroOccupancy(Pusher, PusherAt),
                new HeroOccupancy(Target, TargetAt)
            };
            if (blockerAt.HasValue)
            {
                occupancies.Add(new HeroOccupancy(Blocker, blockerAt.Value));
            }

            return new AdjacentMoveAuthority(coordinates, occupancies);
        }

        /// <summary>
        /// The same file, but (2,0) is a declared terrain cell rather than a playable space.
        /// </summary>
        private static AdjacentMoveAuthority CreateTerrainLine()
        {
            var playable = new[]
            {
                new HexCoordinate(-1, 0), PusherAt, TargetAt,
                new HexCoordinate(3, 0), new HexCoordinate(4, 0), new HexCoordinate(5, 0)
            };
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("synthetic-push-terrain-line"),
                1,
                playable,
                new[] { new HexCoordinate(2, 0) });
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Pusher, PusherAt), new HeroOccupancy(Target, TargetAt) },
                Array.Empty<ObstacleOccupancy>());
        }

        /// <summary>
        /// A live set carrying only the Move-Through permission, built the way the match builds it -
        /// from an Active effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet MoveThroughFor(HeroId heroId)
        {
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"push-move-through-{heroId.Value}"),
                GuardsDigital.Core.Effects.Synthetic.SyntheticModificationCapabilities
                    .MoveThroughPermissionDefinition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("push-move-through-source-card"),
                new ActionDefinitionId("push-move-through-source-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                PlaceholderOneVsOneParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None);
            Assert.That(
                set.HasPermission(RuleObject.ForHero(heroId), RulePermissionKind.MoveThroughObstacles),
                Is.True,
                "The fixture must actually hold the permission, or the case below proves nothing.");
            return set;
        }
    }
}
