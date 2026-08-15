using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4's two capabilities, executed as the production handler objects the catalog
    /// registers rather than as re-implementations of them.
    ///
    /// This fixture exists for the paths a single fixture card cannot reach. Two of them are the ones
    /// the project has been caught on twice before, at 6.8c and 6.8b: a rule whose only caller is a
    /// test is not implemented, so the `towardActor` policy and the Object Push immunity are proved
    /// here through the registered handlers. `RuleActivity.ObjectPush` has been an inert enum member
    /// since milestone 6.8 and this is the milestone that reads it; milestone 6.8b's Blue Feint
    /// immunity is scheduled behind 6.4 for exactly that reason.
    /// </summary>
    public sealed class DisplacementCapabilityTests
    {
        private static readonly HeroId Actor = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
        private static readonly MinionId Minion = new MinionId("displacement-capability-minion");
        private static readonly MapId Map = new MapId("synthetic-displacement-capability-map");

        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, 0);

        // ---------------------------------------------------------------------------------------
        // Object Push
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ADeclaredPushImmunity_RemovesTheUnitFromTheTargetSet()
        {
            // p.18: "If a unit is immune, you cannot target that unit." This is the first production
            // read of RuleActivity.ObjectPush.
            var board = CreateLineBoard(otherAt: new HexCoordinate(1, 0));
            var immune = PushImmunityFor(Other);

            var result = Push(board, distance: 2, modifications: immune);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The only adjacent object is immune, so the push has no legal target at all.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)),
                "An immune unit is not moved.");
        }

        [Test]
        public void WithoutThatImmunity_TheSameUnitIsPushed()
        {
            // The negative twin. Without it the case above would pass on any implementation that
            // simply never pushes anything.
            var board = CreateLineBoard(otherAt: new HexCoordinate(1, 0));

            var result = Push(board, distance: 2, modifications: RuleModificationSet.Empty);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(Output(result, "moved").IntegerValue, Is.EqualTo(1));
            Assert.That(Output(result, "distance").IntegerValue, Is.EqualTo(2));
        }

        [Test]
        public void APushImmunityNamingAnotherActivity_DoesNotRefuseThePush()
        {
            // Immunity is per activity (p.18): "If a unit is immune to a specific action type, it is
            // not affected ... by actions of that type ... but is affected by other effects."
            var board = CreateLineBoard(otherAt: new HexCoordinate(1, 0));

            var result = Push(board, distance: 2, modifications: ImmunityFor(Other, RuleActivity.Attack));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(new HexCoordinate(3, 0)));
        }

        [Test]
        public void EveryAdjacentUnitAndToken_IsPushedInOneBatchAtOneChosenDistance()
        {
            // p.18: "If a card allows you to push multiple targets up to X spaces, you choose the
            // distance and then push all those units that many spaces at the same time."
            var board = CreateStarBoard();
            var token = PlaceToken(board, ActorAt.GetNeighbor(HexDirection.West));
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            var otherAt = ActorAt.GetNeighbor(HexDirection.East);

            var handler = Handler(SyntheticDisplacementCapabilities.ObjectPushV1);
            var offered = handler.Execute(Context(
                board, EffectFailurePolicy.UpTo, distance: 2, modifications: RuleModificationSet.Empty));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.IntegerValue),
                Is.EqualTo(new[] { 0, 1, 2 }),
                "Zero is a legal chosen distance, so the range starts there.");

            var applied = handler.Resume(
                Context(board, EffectFailurePolicy.UpTo, 2, RuleModificationSet.Empty),
                new[] { EffectValue.Integer(2) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(3),
                "All three adjacent objects travelled, in one batch at the one chosen distance.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(otherAt, HexDirection.East, 2)));
            Assert.That(
                board.Minions.Single().Coordinate,
                Is.EqualTo(StraightLineQuery.Step(minionAt, HexDirection.NorthEast, 2)));
            Assert.That(
                board.PlacedTokens.Single().Coordinate,
                Is.EqualTo(StraightLineQuery.Step(token.Coordinate, HexDirection.West, 2)),
                "p.18 names a token as a push target; p.19 keeps it out of the unit rules only.");
            Assert.That(board.IsEmptyPlayableSpace(otherAt), Is.True);
            Assert.That(board.IsEmptyPlayableSpace(minionAt), Is.True);
        }

        [Test]
        public void AChosenDistanceOfZero_AppliesAndMovesNothing()
        {
            var board = CreateStarBoard();
            var otherAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV1).Resume(
                Context(board, EffectFailurePolicy.UpTo, 2, RuleModificationSet.Empty),
                new[] { EffectValue.Integer(0) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(0));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(otherAt));
        }

        [Test]
        public void AChosenDistanceAboveTheBound_IsRefusedWithoutMovingAnything()
        {
            var board = CreateStarBoard();
            var otherAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV1).Resume(
                Context(board, EffectFailurePolicy.UpTo, 2, RuleModificationSet.Empty),
                new[] { EffectValue.Integer(5) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(otherAt),
                "A rejected answer applies nothing: validation is side-effect free.");
        }

        [Test]
        public void ABlockedTargetPushesZeroWhileItsNeighbourTravels()
        {
            // One batch, two different legal outcomes. The blocked target is not a failure that stops
            // the others: p.18 moves each as far as it can.
            var board = CreateStarBoard();
            var blockedFile = ActorAt.GetNeighbor(HexDirection.East);
            var wall = StraightLineQuery.Step(ActorAt, HexDirection.East, 2);
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("push-wall-token"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV3),
                Actor, Actor, new CardInstanceId("push-wall-source"), wall));
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV1).Resume(
                Context(board, EffectFailurePolicy.UpTo, 2, RuleModificationSet.Empty),
                new[] { EffectValue.Integer(2) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(blockedFile),
                "The obstacle sits immediately behind this target, so it is pushed 0 spaces.");
            Assert.That(
                board.Minions.Single().Coordinate,
                Is.EqualTo(StraightLineQuery.Step(minionAt, HexDirection.NorthEast, 2)),
                "The unobstructed target still travels the full chosen distance.");
        }

        // ---------------------------------------------------------------------------------------
        // Direct Move
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AMoveTowardsTheActor_OffersOnlySpacesCloserThanTheMoversOwn()
        {
            // `move a unit towards you` is this policy plus a distance, not a separate Pull keyword.
            // This is the production consumer of CloserQuery.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV1).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, mover: Other, distance: 2, towardActor: true));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Is.EqualTo(new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) }),
                "Only the two spaces closer to the actor than (3,0) qualify, in axial order.");
            Assert.That(spaces, Has.No.Member(new HexCoordinate(4, 0)),
                "A space farther from the actor is refused by a `towards` policy.");
        }

        [Test]
        public void TheSameMoveWithoutThatPolicy_OffersBothDirections()
        {
            // The negative twin: without it the case above cannot tell filtering from a short board.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV1).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, mover: Other, distance: 2, towardActor: false));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(4, 0)));
        }

        [Test]
        public void AMandatoryDistance_OffersOnlyTheExactNumberOfSpaces()
        {
            // p.11: "unless the distance is optional ... you must move the exact number of spaces".
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV1).Execute(
                MoveContext(board, EffectFailurePolicy.Mandatory, Other, distance: 2, towardActor: false));

            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Has.Member(new HexCoordinate(1, 0)));
            Assert.That(spaces, Has.Member(new HexCoordinate(5, 0)));
            Assert.That(spaces, Has.No.Member(new HexCoordinate(2, 0)),
                "One space away is a shorter move than the exact distance this node requires.");
        }

        [Test]
        public void AnUpToDistance_AlsoOffersEveryShorterSpace()
        {
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV1).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, Other, distance: 2, towardActor: false));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(2, 0)),
                "`up to` makes the distance itself optional, so a shorter destination is legal.");
        }

        [Test]
        public void ADirectMove_CannotEndOnAnOccupiedSpaceAndDoesNotBecomeFastTravel()
        {
            // p.11 keeps the ordinary movement restrictions, and the actor's own space is occupied.
            var board = CreateLineBoard(otherAt: new HexCoordinate(2, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV1).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, Other, distance: 3, towardActor: false));

            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Has.No.Member(ActorAt), "The actor occupies its own space.");
            Assert.That(spaces, Has.No.Member(new HexCoordinate(-1, 0)),
                "The mover cannot pass through the actor to reach the far side, which is the ordinary " +
                "obstacle rule a Movement action would apply.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticEffectCapabilities.CreateCatalog().Load(reference);

        private static EffectExecutionResult Push(
            AdjacentMoveAuthority board, int distance, RuleModificationSet modifications) =>
            Handler(SyntheticDisplacementCapabilities.ObjectPushV1)
                .Execute(Context(board, EffectFailurePolicy.Mandatory, distance, modifications));

        private static EffectValue Output(EffectExecutionResult result, string name) =>
            result.Outcomes.Single(outcome =>
                string.Equals(outcome.OutputName, name, StringComparison.Ordinal)).Value;

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            int distance,
            RuleModificationSet modifications) =>
            BuildContext(board, policy, modifications, new Dictionary<string, EffectValue>
            {
                ["distance"] = EffectValue.Integer(distance),
                ["ignoresObstacles"] = EffectValue.Boolean(false)
            });

        private static EffectCapabilityContext MoveContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            HeroId mover,
            int distance,
            bool towardActor) =>
            BuildContext(board, policy, RuleModificationSet.Empty, new Dictionary<string, EffectValue>
            {
                ["mover"] = EffectValue.Hero(mover),
                ["distance"] = EffectValue.Integer(distance),
                ["towardActor"] = EffectValue.Boolean(towardActor)
            });

        private static EffectCapabilityContext BuildContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            RuleModificationSet modifications,
            IReadOnlyDictionary<string, EffectValue> inputs) =>
            new EffectCapabilityContext(
                new EffectNodeId("displacement-capability-node"),
                policy,
                Actor,
                Actor,
                new CardInstanceId("displacement-capability-card"),
                new ActionDefinitionId("displacement-capability-action"),
                inputs,
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                modifications,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(), board));

        /// <summary>A playable East-West file (-2,0)..(6,0) with the actor at (0,0).</summary>
        private static AdjacentMoveAuthority CreateLineBoard(HexCoordinate otherAt)
        {
            var map = BoardMapDefinition.CreateSynthetic(
                Map, 1, Enumerable.Range(-2, 9).Select(q => new HexCoordinate(q, 0)).ToArray());
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Actor, ActorAt), new HeroOccupancy(Other, otherAt) },
                Array.Empty<ObstacleOccupancy>());
        }

        /// <summary>
        /// A radius-4 board with the actor at the centre, a hero to its East, a minion to its
        /// North-East, and room in every direction for a two-space push.
        /// </summary>
        private static AdjacentMoveAuthority CreateStarBoard()
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -4; q <= 4; q++)
            {
                for (var r = Math.Max(-4, -q - 4); r <= Math.Min(4, -q + 4); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            var map = BoardMapDefinition.CreateSynthetic(Map, 1, coordinates);
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            return new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Actor, ActorAt),
                    new HeroOccupancy(Other, ActorAt.GetNeighbor(HexDirection.East))
                },
                Array.Empty<ObstacleOccupancy>(),
                new[]
                {
                    new QuickGameMinionSetupState(
                        Minion,
                        new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-capability")),
                        new ConfiguredSpaceReference(
                            Map, new SpaceId($"q{minionAt.Q}_r{minionAt.R}")),
                        minionAt)
                });
        }

        private static PlacedToken PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            var token = new PlacedToken(
                new TokenInstanceId($"push-token-{coordinate.Q}-{coordinate.R}"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV3),
                Actor,
                Actor,
                new CardInstanceId("push-token-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static RuleModificationSet PushImmunityFor(HeroId heroId) =>
            ImmunityFor(heroId, RuleActivity.ObjectPush);

        /// <summary>
        /// A live set carrying one immunity, built the way the match builds it - from an Active effect
        /// in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            // The registered content set declares exactly one immunity, and it names Object Push, so a
            // case about a different activity supplies a test-local definition rather than pretending
            // production content has one.
            var definition = activity == RuleActivity.ObjectPush
                ? SyntheticModificationCapabilities.PushImmunityDefinition
                : new ActiveEffectDefinition(
                    $"test-local-{activity}-immunity-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.CardsPlayedAndRevealed,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    true,
                    $"TestLocal{activity}Immunity",
                    $"test-local-{activity}-immunity",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"displacement-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("displacement-immunity-source"),
                new ActionDefinitionId("displacement-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing, null, PlaceholderOneVsOneParticipants.Create(), new TieBreakerState(TeamSide.Red));
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the case below proves nothing.");
            return set;
        }
    }
}
