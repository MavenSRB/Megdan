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
    /// Milestone 6.4's two capabilities, executed as the production handler objects the catalog
    /// registers rather than as re-implementations of them.
    ///
    /// This fixture exists for the paths a single fixture card cannot reach. Two of them are the ones
    /// the project has been caught on twice before, at 6.8c and 6.8b: a rule whose only caller is a
    /// test is not implemented, so the destination policy and the Object Push immunity are proved
    /// here through the registered handlers. `RuleActivity.ObjectPush` has been an inert enum member
    /// since milestone 6.8 and this is the milestone that reads it; milestone 6.8b's Blue Feint
    /// immunity is scheduled behind 6.4 for exactly that reason.
    /// </summary>
    public sealed class DisplacementCapabilityTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MinionId Minion = new MinionId("displacement-capability-minion");
        private static readonly MinionId AlliedMinion =
            new MinionId("displacement-capability-allied-minion");
        private static readonly HeroId SecondOther = new HeroId("displacement-capability-second-hero");
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

            var handler = Handler(SyntheticDisplacementCapabilities.ObjectPushV3);
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

        // ---------------------------------------------------------------------------------------
        // Milestone 6.4c: which kind of piece a declaration admits
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ADeclaredUnitPush_LeavesAnAdjacentTokenStandingWhileEveryUnitTravels()
        {
            // The same board and the same batch as the case above, declaring `unit` instead of p.18's
            // own unit-or-token breadth. This is the whole of the milestone at the capability level:
            // the rule is unchanged and the declaration is what narrowed.
            var board = CreateStarBoard();
            var token = PlaceToken(board, ActorAt.GetNeighbor(HexDirection.West));
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            var otherAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                Context(board, EffectFailurePolicy.UpTo, 2, RuleModificationSet.Empty,
                    PushEligiblePieceKind.Unit),
                new[] { EffectValue.Integer(2) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(2),
                "Two units travelled and the token did not, where p.18's own breadth moved three.");
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(token.Coordinate),
                "p.19 states that tokens are not units, so text saying unit never names one.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(otherAt, HexDirection.East, 2)));
            Assert.That(
                board.Minions.Single().Coordinate,
                Is.EqualTo(StraightLineQuery.Step(minionAt, HexDirection.NorthEast, 2)));
        }

        [Test]
        public void ADeclaredUnitPush_NamingATokenIsRefusedRatherThanResolvedToNothing()
        {
            var board = CreateStarBoard();
            var token = PlaceToken(board, ActorAt.GetNeighbor(HexDirection.West));

            var refused = Handler(SyntheticDisplacementCapabilities.PushTargetV3).Execute(
                NamedContext(board, token.Coordinate, PushEligiblePieceKind.Unit));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The named space holds a piece this declaration cannot take, so the step stops rather " +
                "than pushing nothing and reporting success. Milestone 6.8l re-reads the answer when " +
                "it arrives, so a stale or forged selection is refused here too.");
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(token.Coordinate));

            var admitted = Handler(SyntheticDisplacementCapabilities.PushTargetV3).Execute(
                NamedContext(board, token.Coordinate, PushEligiblePieceKind.UnitOrToken));

            Assert.That(admitted.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The same token under p.18's own breadth is an ordinary target, which is what makes " +
                "the refusal above the card's narrowing rather than a rule this milestone invented.");
        }

        [Test]
        public void EveryPushDeclaration_ProbesTheSameEligibilityItPerforms()
        {
            // Milestone 6.6a found the opposite of this in `RemoveHandler`: a probe reported a piece
            // removable that the operation then refused, because the eligibility reading had been
            // written twice. The two answers are asserted equal rather than each separately, because
            // milestone 6.8l reads the probe as the offer a player is shown.
            var board = CreateStarBoard();
            var token = PlaceToken(board, ActorAt.GetNeighbor(HexDirection.West));

            foreach (var eligiblePiece in new[]
            {
                PushEligiblePieceKind.UnitOrToken,
                PushEligiblePieceKind.Unit
            })
            {
                var handler = Handler(SyntheticDisplacementCapabilities.PushTargetV3);
                var anticipated = handler.Anticipate(
                    NamedContext(board, token.Coordinate, eligiblePiece));
                var executed = handler.Execute(NamedContext(board, token.Coordinate, eligiblePiece));

                Assert.That(
                    anticipated == EffectExecutionResultKind.ImpossibleMandatory,
                    Is.EqualTo(executed.Kind == EffectExecutionResultKind.ImpossibleMandatory),
                    $"Declaring {eligiblePiece}, the probe and the operation disagreed about whether " +
                    "this piece may be pushed.");
            }
        }

        [Test]
        public void AnUndeclaredEligibleKind_AdmitsNothing()
        {
            // The declaration is required rather than defaulted, so a definition that states no kind
            // pushes nothing instead of quietly meaning the widest thing.
            foreach (var kind in new[]
            {
                BoardObjectKind.Hero, BoardObjectKind.Minion, BoardObjectKind.Token
            })
            {
                Assert.That(PushEligibility.Admits(PushEligiblePieceKind.None, kind), Is.False);
                Assert.That(PushEligibility.Admits(PushEligiblePieceKind.UnitOrToken, kind), Is.True,
                    "p.18 pushes a unit or a token, and a hero is a unit.");
            }

            Assert.That(PushEligibility.Admits(PushEligiblePieceKind.Unit, BoardObjectKind.Hero), Is.True);
            Assert.That(PushEligibility.Admits(PushEligiblePieceKind.Unit, BoardObjectKind.Minion), Is.True);
            Assert.That(PushEligibility.Admits(PushEligiblePieceKind.Unit, BoardObjectKind.Token), Is.False);
        }

        [Test]
        public void AChosenDistanceOfZero_AppliesAndMovesNothing()
        {
            var board = CreateStarBoard();
            var otherAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
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

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
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
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV4),
                Actor, Actor, new CardInstanceId("push-wall-source"), wall));
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
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

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, moverAt: new HexCoordinate(3, 0), distance: 2, DirectMoveDestinationPolicy.TowardActor));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(
                spaces,
                Is.EqualTo(new[]
                {
                    new HexCoordinate(1, 0), new HexCoordinate(2, 0), new HexCoordinate(3, 0)
                }),
                "Only the two spaces closer to the actor than (3,0) qualify as travel, in axial order, " +
                "beside the mover's own space that milestone 6.5ad's optional distance admits.");
            Assert.That(spaces, Has.No.Member(new HexCoordinate(4, 0)),
                "A space farther from the actor is refused by a `towards` policy.");
        }

        [Test]
        public void AMoveTowardsTheActor_StillAdmitsStandingStill()
        {
            // Milestone 6.5ad. `Closer` is a rule about where the mover may end up if it travels, and
            // the mover's own space is never closer to the actor than itself, so filtering the whole
            // admitted set through it would leave `move a unit up to X towards you` unable to express
            // p.10's own "up to". Admitting no distance at all is a property of the wording, so it is
            // decided before the geometry and added to what the geometry admits.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2, DirectMoveDestinationPolicy.TowardActor));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(3, 0)));
        }

        [Test]
        public void AnOrbitMove_OffersOnlySpacesTheSameDistanceFromTheActorAsTheMoversOwn()
        {
            // Milestone 8.6. `without moving it away from you or closer to you` is the third declared
            // destination policy, and this is its production consumer. The star board is what makes the
            // case say anything: on a line there is nowhere to orbit to, which is a true answer about a
            // line and no evidence at all about the policy.
            var board = CreateStarBoard();
            var moverAt = ActorAt.GetNeighbor(HexDirection.East);

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(
                    board, EffectFailurePolicy.Mandatory, moverAt, 1,
                    DirectMoveDestinationPolicy.EquidistantFromActor));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Is.EqualTo(new[] { new HexCoordinate(0, 1) }),
                "The mover stands one space from the actor, so the orbit admits the ring of one - " +
                "and of its two spaces, the North-East one holds the fixture's minion, which is the " +
                "occupancy question rather than this policy's.");
            Assert.That(spaces, Has.No.Member(ActorAt),
                "The step towards the actor is refused, exactly as `away` would be.");
            Assert.That(spaces, Has.No.Member(new HexCoordinate(2, 0)),
                "and so is the step away from them.");
            Assert.That(spaces, Has.No.Member(moverAt),
                "A mandatory one-space move walks exactly one space (p.11), so standing still is not " +
                "admitted here - which is the failure policy's decision, not the orbit's.");
        }

        [Test]
        public void AnOrbitMove_IsImpossibleWhileTheActorStandsNowhere()
        {
            // Both narrowing policies measure from the acting hero, so an actor who is not on the board
            // - a hero awaiting respawn - can satisfy neither. The unrestricted reading needs no such
            // position and is deliberately not refused for lacking one.
            var map = BoardMapDefinition.CreateSynthetic(
                Map, 1, Enumerable.Range(-2, 9).Select(q => new HexCoordinate(q, 0)).ToArray());
            var actorless = new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Other, new HexCoordinate(3, 0)) },
                Array.Empty<ObstacleOccupancy>());

            Assert.That(
                Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(MoveContext(
                    actorless, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.EquidistantFromActor)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(MoveContext(
                    actorless, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.Unrestricted)).Kind,
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "A card that says only how far still resolves with the actor off the board.");
        }

        [Test]
        public void ADirectMove_RefusesADestinationPolicyItsDeclarationDoesNotName()
        {
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            Assert.That(
                Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(MoveContext(
                    board, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.None)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "An undeclared policy is refused rather than read as the unrestricted one: p.11 gives " +
                "card text the narrowing, so silence is a content fault and not a permission.");
        }

        [Test]
        public void AnUpToDirectMove_AdmitsTheMoversOwnSpaceTheWayAnUpToPushAdmitsDistanceZero()
        {
            // The defect milestone 6.5ad corrects. p.10 lists `up to` among the non-mandatory wordings
            // and p.11 requires the exact number of spaces only where the distance is not optional, so
            // `up to 2 spaces` admits none at all. Object Push read that sentence correctly and offered
            // 0..X; direct Move offered only 1..X, not by decision but because its candidates come from
            // the Movement action's reachability walk, which never emits the mover's own space.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2, DirectMoveDestinationPolicy.Unrestricted));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(3, 0)),
                "The mover's own space is the destination that moves it no spaces at all.");
        }

        [Test]
        public void AnUpToDirectMove_WithAPositiveLeastDistance_OffersOneThroughTheBudgetOnly()
        {
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));
            var moverAt = new HexCoordinate(3, 0);
            var context = BuildContext(
                board,
                EffectFailurePolicy.UpTo,
                RuleModificationSet.Empty,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                new Dictionary<string, EffectValue>
                {
                    ["mover"] = EffectValue.Space(moverAt),
                    ["distance"] = EffectValue.Integer(2),
                    [SyntheticDisplacementCapabilities.LeastDistanceParameterName] =
                        EffectValue.Integer(1),
                    [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                        EffectValue.Integer((int)DirectMoveDestinationPolicy.Unrestricted)
                });

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(context);
            var distances = offered.Choice.Candidates
                .Select(candidate => HexDistanceQuery.Calculate(moverAt, candidate.Coordinate))
                .Distinct()
                .OrderBy(distance => distance)
                .ToArray();

            Assert.That(distances, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.No.Member(moverAt),
                "A positive typed floor removes standing still without changing ordinary up-to moves.");
            Assert.That(
                Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Schema.Parameters
                    .Single(parameter => parameter.Name ==
                        SyntheticDisplacementCapabilities.LeastDistanceParameterName).IsRequired,
                Is.False,
                "Older Direct Move declarations omit the compatible floor and retain zero as before.");
        }

        [Test]
        public void ChoosingTheMoversOwnSpace_RelocatesNothingAndIsAnAppliedResult()
        {
            // The same distinction p.18's zero-space push already draws: standing still is a legal
            // applied outcome, not a decline and not a failure, and the node publishes the space the
            // mover is still on so later text reading `if that unit moved` sees the truth.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));
            var moverAt = new HexCoordinate(3, 0);

            var applied = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Resume(
                MoveContext(board, EffectFailurePolicy.UpTo, moverAt, distance: 2, DirectMoveDestinationPolicy.Unrestricted),
                new[] { EffectValue.Space(moverAt) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").Coordinate, Is.EqualTo(moverAt));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(moverAt),
                "Nothing was relocated, and the occupancy index was never asked to move anything.");
        }

        [Test]
        public void AnExactDistance_StillRefusesTheMoversOwnSpace()
        {
            // The control. p.11 requires the exact number of spaces wherever the distance is not
            // optional, so the correction above must not reach a mandatory node.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.Mandatory, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.Unrestricted));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.No.Member(new HexCoordinate(3, 0)));
        }

        [Test]
        public void AMoverWithNowhereToGo_CanStandStillRatherThanBeingRefusedOutright()
        {
            // Acceptance check 5, and the difference a player sees: milestone 6.5ac's option walk asks
            // this capability what it would report, so a `up to` branch whose subject is walled in used
            // to be judged unperformable and greyed. It is performable - declining the distance is
            // something the chooser may do - and the walk reads the widened set without changing.
            var board = CreateLineBoard(otherAt: new HexCoordinate(-2, 0));
            PlaceToken(board, new HexCoordinate(-1, 0));
            var handler = Handler(SyntheticDisplacementCapabilities.DirectMoveV3);
            var context = MoveContext(
                board, EffectFailurePolicy.UpTo, new HexCoordinate(-2, 0), distance: 2, DirectMoveDestinationPolicy.Unrestricted);

            var offered = handler.Execute(context);

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EqualTo(new[] { new HexCoordinate(-2, 0) }),
                "The board's west end is walled in by the token, so the only admitted distance is none.");
            Assert.That(handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "The probe reads the same derivation, so the option is offered rather than greyed.");
        }

        [Test]
        public void TheSameMoveWithoutThatPolicy_OffersBothDirections()
        {
            // The negative twin: without it the case above cannot tell filtering from a short board.
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, moverAt: new HexCoordinate(3, 0), distance: 2, DirectMoveDestinationPolicy.Unrestricted));

            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(4, 0)));
        }

        [Test]
        public void AMandatoryDistance_OffersOnlyTheExactNumberOfSpaces()
        {
            // p.11: "unless the distance is optional ... you must move the exact number of spaces".
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.Mandatory, new HexCoordinate(3, 0), distance: 2, DirectMoveDestinationPolicy.Unrestricted));

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

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), distance: 2, DirectMoveDestinationPolicy.Unrestricted));

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

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, new HexCoordinate(2, 0), distance: 3, DirectMoveDestinationPolicy.Unrestricted));

            var spaces = offered.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            Assert.That(spaces, Has.No.Member(ActorAt), "The actor occupies its own space.");
            Assert.That(spaces, Has.No.Member(new HexCoordinate(-1, 0)),
                "The mover cannot pass through the actor to reach the far side, which is the ordinary " +
                "obstacle rule a Movement action would apply.");
        }

        [Test]
        public void AMinionSubject_IsMovedByItsSpaceThroughTheSameCapability()
        {
            // 6.8k: the subject parameter is a coordinate, so a minion is nameable. The reachability
            // consulted is unchanged - the port has served every kind since 6.4a widened it.
            var board = CreateStarBoard();
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            var destination = StraightLineQuery.Step(minionAt, HexDirection.NorthEast, 1);

            var applied = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Resume(
                MoveContext(board, EffectFailurePolicy.UpTo, minionAt, distance: 1, DirectMoveDestinationPolicy.Unrestricted),
                new[] { EffectValue.Space(destination) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.Minions.Single().Coordinate, Is.EqualTo(destination),
                "The minion standing on the named space is the subject that moved.");
            Assert.That(board.IsEmptyPlayableSpace(minionAt), Is.True);
        }

        [Test]
        public void ATokenSubject_IsMovedByItsSpaceThroughTheSameCapability()
        {
            var board = CreateStarBoard();
            var tokenAt = ActorAt.GetNeighbor(HexDirection.West);
            var token = PlaceToken(board, tokenAt);
            var destination = StraightLineQuery.Step(tokenAt, HexDirection.West, 1);

            var applied = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Resume(
                MoveContext(board, EffectFailurePolicy.UpTo, tokenAt, distance: 1, DirectMoveDestinationPolicy.Unrestricted),
                new[] { EffectValue.Space(destination) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.PlacedTokens.Single(placed => placed.Id.Equals(token.Id)).Coordinate,
                Is.EqualTo(destination),
                "p.11 names a token as a direct-Move subject; naming its space reaches it.");
        }

        [Test]
        public void AnEmptySubjectSpace_NamesNothingAndTheNodeIsImpossible()
        {
            var board = CreateLineBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(board, EffectFailurePolicy.UpTo, new HexCoordinate(1, 0), distance: 2, DirectMoveDestinationPolicy.Unrestricted));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A coordinate no object stands on names no subject; the space is re-read, not remembered.");
        }

        [Test]
        public void AMoveThroughPermission_LetsAHeroSubjectCrossWhatATokenSubjectCannot()
        {
            // Check 4 of 6.8k: a token subject holds no Move-Through permission of its own and reaches
            // the transit predicate as the named absence, because p.19 states tokens are not units. The
            // same modification set is supplied to both subjects; a permission is declared for a rules
            // object, and a token has none to declare it for.
            var withPermission = MoveThroughFor(Other);

            var heroBoard = CreateLineBoard(otherAt: new HexCoordinate(3, 0));
            PlaceToken(heroBoard, new HexCoordinate(4, 0));
            var heroOffered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(heroBoard, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.Unrestricted, withPermission));
            Assert.That(
                heroOffered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(5, 0)),
                "The hero holds the permission, so it crosses the token and lands on the empty space beyond.");

            var tokenBoard = CreateLineBoard(otherAt: new HexCoordinate(-2, 0));
            PlaceToken(tokenBoard, new HexCoordinate(3, 0));
            PlaceToken(tokenBoard, new HexCoordinate(4, 0), SyntheticComponentDefinitions.WardV1);
            var tokenOffered = Handler(SyntheticDisplacementCapabilities.DirectMoveV3).Execute(
                MoveContext(tokenBoard, EffectFailurePolicy.UpTo, new HexCoordinate(3, 0), 2,
                    DirectMoveDestinationPolicy.Unrestricted, withPermission));
            Assert.That(tokenOffered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                tokenOffered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.No.Member(new HexCoordinate(5, 0)),
                "The token reaches the predicate as the named absence, which no declared permission matches.");
            Assert.That(
                tokenOffered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Has.Member(new HexCoordinate(2, 0)),
                "Away from the wall the same token subject moves freely, so the refusal above is the permission, not the token.");
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 8.8: the three things a card can now say about a batch, and what it publishes
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ADeclaredEnemyRelation_LeavesTheActorsOwnTeamStandingWhileEveryEnemyTravels()
        {
            // "up to 2 enemy units adjacent to you". p.19's relationship vocabulary read from the
            // card's perspective, which is the acting hero's: her own minion is not among her targets.
            var board = CreateMixedTeamBoard();
            var alliedAt = ActorAt.GetNeighbor(HexDirection.West);
            var enemyHeroAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Execute(Context(
                board, EffectFailurePolicy.Mandatory, 2, RuleModificationSet.Empty,
                PushEligiblePieceKind.Unit, SourceTeamRelation.OpposingSource,
                maximumTargets: 0, leastDistance: 2));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(2),
                "The enemy hero and the enemy minion travelled; the allied minion did not.");
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(AlliedMinion)).Coordinate,
                Is.EqualTo(alliedAt),
                "A unit on the actor's own team is not an enemy unit, however close it stands.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(enemyHeroAt, HexDirection.East, 2)));

            var unnarrowed = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Execute(Context(
                CreateMixedTeamBoard(), EffectFailurePolicy.Mandatory, 2, RuleModificationSet.Empty,
                PushEligiblePieceKind.Unit, SourceTeamRelation.AnyTeam,
                maximumTargets: 0, leastDistance: 2));
            Assert.That(Output(unnarrowed, "moved").IntegerValue, Is.EqualTo(3),
                "The same board with no side named pushes all three, which is what makes the case " +
                "above the card's narrowing rather than a rule this milestone invented.");
        }

        [Test]
        public void ADeclaredMaximum_MakesWhichUnitsArePushedTheChoosersDecision()
        {
            var board = CreateMixedTeamBoard();
            var enemyHeroAt = ActorAt.GetNeighbor(HexDirection.East);
            var enemyMinionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            var handler = Handler(SyntheticDisplacementCapabilities.ObjectPushV3);

            var offered = handler.Execute(KineticContext(board, maximumTargets: 2, distance: 3));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EquivalentTo(new[] { enemyHeroAt, enemyMinionAt }),
                "A push names its targets by space, and only the enemy units are offered.");
            Assert.That(offered.Choice.MinimumSelections, Is.EqualTo(0),
                "`up to 2` includes none, so the selection may close on nothing.");
            Assert.That(offered.Choice.MaximumSelections, Is.EqualTo(2));

            var applied = handler.Resume(
                KineticContext(board, maximumTargets: 2, distance: 3),
                new[] { EffectValue.Space(enemyMinionAt) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(1));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(enemyHeroAt),
                "The unit the chooser did not name stays exactly where it stood.");
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(Minion)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(enemyMinionAt, HexDirection.NorthEast, 3)));
        }

        [Test]
        public void ChoosingNoUnitAtAll_IsTheCountTheCardAllowsRatherThanAFailedStep()
        {
            var board = CreateMixedTeamBoard();

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                KineticContext(board, maximumTargets: 2, distance: 3),
                Array.Empty<EffectValue>());

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "p.10: an `up to` step nobody takes is not taken, which is not the same as one that " +
                "could not be.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(ActorAt.GetNeighbor(HexDirection.East)));
        }

        [Test]
        public void ADistanceTheCardWritesAsAChoice_IsAskedAfterTheUnitsAndAppliesToThoseUnits()
        {
            // Kinetic Blast: "Push up to 2 enemy units adjacent to you 3 or 4 spaces." p.18 settles the
            // distance over "all those units", so the set is named first and one distance is chosen for
            // it. Both answers reach the same node, which is why the second one has to be able to read
            // the first.
            var board = CreateMixedTeamBoard();
            var enemyHeroAt = ActorAt.GetNeighbor(HexDirection.East);
            var handler = Handler(SyntheticDisplacementCapabilities.ObjectPushV3);

            var chosenUnits = handler.Resume(
                KineticContext(board, maximumTargets: 2, distance: 4, leastDistance: 3),
                new[] { EffectValue.Space(enemyHeroAt) });

            Assert.That(chosenUnits.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                chosenUnits.Choice.Candidates.Select(candidate => candidate.IntegerValue),
                Is.EqualTo(new[] { 3, 4 }),
                "The card names two numbers and nothing between them and zero.");

            var applied = handler.Resume(
                KineticContext(
                    board, maximumTargets: 2, distance: 4, leastDistance: 3,
                    answeredEarlier: new[] { EffectValue.Space(enemyHeroAt) }),
                new[] { EffectValue.Integer(4) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "distance").IntegerValue, Is.EqualTo(4));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(1),
                "Only the unit the first answer named travelled, at the distance the second chose.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(enemyHeroAt, HexDirection.East, 4)));
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(Minion)).Coordinate,
                Is.EqualTo(ActorAt.GetNeighbor(HexDirection.NorthEast)),
                "The enemy unit the first answer left out is still standing beside the actor.");
        }

        [Test]
        public void ADistanceTheCardNamesExactly_OpensNoSecondQuestionEvenUnderAnOptionalCount()
        {
            var board = CreateMixedTeamBoard();

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                KineticContext(board, maximumTargets: 2, distance: 3, leastDistance: 3),
                new[] { EffectValue.Space(ActorAt.GetNeighbor(HexDirection.East)) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Kinetic Repulse's `up to` is over the units; the spaces are one number, so nobody is " +
                "asked how far.");
            Assert.That(Output(applied, "distance").IntegerValue, Is.EqualTo(3));
        }

        [Test]
        public void TheStopCause_IsPublishedPerPushedHeroInCommitOrder()
        {
            // One batch, two heroes, two different outcomes. The facts are what the card's rider reads,
            // and the rider is about a hero rather than about the push, so each hero gets a pair.
            var board = CreateTwoPushedHeroBoard();
            var stoppedAt = ActorAt.GetNeighbor(HexDirection.East);
            var freeAt = ActorAt.GetNeighbor(HexDirection.West);
            PlaceToken(board, StraightLineQuery.Step(ActorAt, HexDirection.East, 3));

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                KineticContext(
                    board, maximumTargets: 2, distance: 3, relation: SourceTeamRelation.AnyTeam),
                new[] { EffectValue.Space(stoppedAt), EffectValue.Space(freeAt) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName)
                    .BooleanValue,
                Is.True);
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.FirstStoppedHeroOutputName).HeroId,
                Is.EqualTo(Other),
                "An obstacle two spaces along its file stopped this one short of the three it asked for.");
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.SecondHeroWasStoppedOutputName)
                    .BooleanValue,
                Is.False,
                "The other hero travelled its whole distance, so nothing stopped it.");
            Assert.That(
                applied.Outcomes.Any(outcome => string.Equals(
                    outcome.OutputName,
                    SyntheticDisplacementCapabilities.SecondStoppedHeroOutputName,
                    StringComparison.Ordinal)),
                Is.False,
                "A fact naming nobody would be a worse answer than no fact: the boolean is what the " +
                "rider branches on, and it already says there was no second hero.");
        }

        [Test]
        public void APushedHeroStoppedByTheBoardsEdge_IsPublishedAsStoppedByAnObstacle()
        {
            // Ledger question W-04, at the seam a card reads it through. The board here ends two spaces
            // East of the target, so the push leaves the map rather than meeting anything on it.
            var board = CreateShortEdgeBoard();
            var targetAt = ActorAt.GetNeighbor(HexDirection.East);

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                KineticContext(board, maximumTargets: 2, distance: 3),
                new[] { EffectValue.Space(targetAt) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(StraightLineQuery.Step(ActorAt, HexDirection.East, 3)),
                "p.18 moves it as far as it can, which is up to the last playable space.");
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName)
                    .BooleanValue,
                Is.True,
                "W-04: the water p.19 calls terrain is what it stopped against, so the card's sentence " +
                "applies even though the model answers off-map as nothing.");
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.FirstStoppedHeroOutputName).HeroId,
                Is.EqualTo(Other));
        }

        [Test]
        public void APushedMinionStoppedByAnObstacle_PublishesNoStopCause()
        {
            // The card's rider says "if a pushed hero"; a minion has no hand to answer one with, so the
            // fact stays a hero's until a card prints something a minion can be told to do.
            var board = CreateMixedTeamBoard();
            var minionAt = ActorAt.GetNeighbor(HexDirection.NorthEast);
            PlaceToken(board, StraightLineQuery.Step(ActorAt, HexDirection.NorthEast, 2));

            var applied = Handler(SyntheticDisplacementCapabilities.ObjectPushV3).Resume(
                KineticContext(board, maximumTargets: 2, distance: 3),
                new[] { EffectValue.Space(minionAt) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(applied, "moved").IntegerValue, Is.EqualTo(0),
                "The obstacle sits immediately behind it, which is p.18's zero-space push.");
            Assert.That(
                Output(applied, SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName)
                    .BooleanValue,
                Is.False);
        }

        [Test]
        public void ANamedPush_DeclaresNeitherACountNorASideAndPublishesOneStopCause()
        {
            var handler = Handler(SyntheticDisplacementCapabilities.PushTargetV3);
            var batch = Handler(SyntheticDisplacementCapabilities.ObjectPushV3);

            Assert.That(
                handler.Schema.Parameters.Select(parameter => parameter.Name),
                Has.No.Member(SyntheticDisplacementCapabilities.MaximumTargetsParameterName),
                "Its one object came from a selection node that already answered how many.");
            Assert.That(
                handler.Schema.Parameters.Select(parameter => parameter.Name),
                Has.Member(SyntheticDisplacementCapabilities.LeastDistanceParameterName),
                "The distance reading is shared, so both declarations carry it.");
            Assert.That(
                handler.Schema.Outputs.Select(output => output.OutputName),
                Has.No.Member(SyntheticDisplacementCapabilities.SecondStoppedHeroOutputName),
                "One named target can stop one hero.");
            Assert.That(
                batch.Schema.Outputs.Select(output => output.OutputName),
                Has.Member(SyntheticDisplacementCapabilities.SecondStoppedHeroOutputName));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// A live set carrying only the Move-Through permission, built the way the match builds it:
        /// from an Active effect in its active phase, not hand-assembled.
        /// </summary>
        private static RuleModificationSet MoveThroughFor(HeroId heroId)
        {
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"displacement-move-through-{heroId.Value}"),
                SyntheticModificationCapabilities.MoveThroughPermissionDefinition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("displacement-move-through-source"),
                new ActionDefinitionId("displacement-move-through-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null,
                null);
            Assert.That(
                set.HasPermission(RuleObject.ForHero(heroId), RulePermissionKind.MoveThroughObstacles),
                Is.True,
                "The fixture must actually hold the permission, or the pair above proves nothing.");
            return set;
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticEffectCapabilities.CreateCatalog().Load(reference);

        private static EffectExecutionResult Push(
            AdjacentMoveAuthority board, int distance, RuleModificationSet modifications) =>
            Handler(SyntheticDisplacementCapabilities.ObjectPushV3)
                .Execute(Context(board, EffectFailurePolicy.Mandatory, distance, modifications));

        private static EffectValue Output(EffectExecutionResult result, string name) =>
            result.Outcomes.Single(outcome =>
                string.Equals(outcome.OutputName, name, StringComparison.Ordinal)).Value;

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            int distance,
            RuleModificationSet modifications) =>
            Context(board, policy, distance, modifications, PushEligiblePieceKind.UnitOrToken);

        /// <summary>
        /// Milestone 6.4c: every case above this one is about what happens to a target the declaration
        /// already admits - geometry, immunity, distance - so they declare p.18's own unit-or-token
        /// breadth and are unchanged in meaning. The cases about the declaration itself pass their own.
        /// </summary>
        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            int distance,
            RuleModificationSet modifications,
            PushEligiblePieceKind eligiblePiece) =>
            Context(
                board, policy, distance, modifications, eligiblePiece,
                SourceTeamRelation.AnyTeam, maximumTargets: 0, leastDistance: 0);

        /// <summary>
        /// Milestone 8.8's full batch declaration. The three defaults above are the reading every Push
        /// had before it: no printed floor under the distance, no side of the board named, and no
        /// maximum, which is p.18's plain batch over every adjacent object. Cases about those three
        /// state their own.
        /// </summary>
        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            int distance,
            RuleModificationSet modifications,
            PushEligiblePieceKind eligiblePiece,
            SourceTeamRelation relation,
            int maximumTargets,
            int leastDistance) =>
            BuildContext(board, policy, modifications,
                SyntheticDisplacementCapabilities.ObjectPushV3,
                new Dictionary<string, EffectValue>
                {
                    ["distance"] = EffectValue.Integer(distance),
                    [SyntheticDisplacementCapabilities.LeastDistanceParameterName] =
                        EffectValue.Integer(leastDistance),
                    ["ignoresObstacles"] = EffectValue.Boolean(false),
                    [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)eligiblePiece),
                    [SyntheticDisplacementCapabilities.TargetRelationParameterName] =
                        EffectValue.Integer((int)relation),
                    [SyntheticDisplacementCapabilities.MaximumTargetsParameterName] =
                        EffectValue.Integer(maximumTargets)
                });

        /// <summary>
        /// Milestone 8.8's kinetic declaration: enemy units only, a maximum the card names, and a
        /// distance whose floor is its own number unless a case states otherwise.
        /// </summary>
        private static EffectCapabilityContext KineticContext(
            AdjacentMoveAuthority board,
            int maximumTargets,
            int distance,
            int leastDistance = -1,
            IReadOnlyList<EffectValue> answeredEarlier = null,
            SourceTeamRelation relation = SourceTeamRelation.OpposingSource) =>
            BuildContext(
                board,
                EffectFailurePolicy.UpTo,
                RuleModificationSet.Empty,
                SyntheticDisplacementCapabilities.ObjectPushV3,
                new Dictionary<string, EffectValue>
                {
                    ["distance"] = EffectValue.Integer(distance),
                    [SyntheticDisplacementCapabilities.LeastDistanceParameterName] =
                        EffectValue.Integer(leastDistance < 0 ? distance : leastDistance),
                    ["ignoresObstacles"] = EffectValue.Boolean(false),
                    [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)PushEligiblePieceKind.Unit),
                    [SyntheticDisplacementCapabilities.TargetRelationParameterName] =
                        EffectValue.Integer((int)relation),
                    [SyntheticDisplacementCapabilities.MaximumTargetsParameterName] =
                        EffectValue.Integer(maximumTargets)
                },
                answeredEarlier == null
                    ? null
                    : new[] { answeredEarlier });

        /// <summary>Milestone 6.5a's named Push, at a fixed distance of 2 with obstacles observed.</summary>
        private static EffectCapabilityContext NamedContext(
            AdjacentMoveAuthority board,
            HexCoordinate target,
            PushEligiblePieceKind eligiblePiece) =>
            BuildContext(
                board, EffectFailurePolicy.Mandatory, RuleModificationSet.Empty,
                SyntheticDisplacementCapabilities.PushTargetV3,
                new Dictionary<string, EffectValue>
                {
                    ["target"] = EffectValue.Space(target),
                    ["distance"] = EffectValue.Integer(2),
                    [SyntheticDisplacementCapabilities.LeastDistanceParameterName] =
                        EffectValue.Integer(2),
                    ["ignoresObstacles"] = EffectValue.Boolean(false),
                    [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)eligiblePiece)
                });

        private static EffectCapabilityContext MoveContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            HexCoordinate moverAt,
            int distance,
            DirectMoveDestinationPolicy destinationPolicy) =>
            MoveContext(board, policy, moverAt, distance, destinationPolicy, RuleModificationSet.Empty);

        /// <summary>
        /// Since milestone 6.8k the subject is the space the mover stands on, the same coordinate
        /// identity Object Push uses for its targets, so these contexts name a coordinate rather than
        /// a hero.
        /// </summary>
        private static EffectCapabilityContext MoveContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            HexCoordinate moverAt,
            int distance,
            DirectMoveDestinationPolicy destinationPolicy,
            RuleModificationSet modifications) =>
            BuildContext(board, policy, modifications,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                new Dictionary<string, EffectValue>
                {
                    ["mover"] = EffectValue.Space(moverAt),
                    ["distance"] = EffectValue.Integer(distance),
                    [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                        EffectValue.Integer((int)destinationPolicy)
                });

        private static EffectCapabilityContext BuildContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            RuleModificationSet modifications,
            EffectCapabilityReference capability,
            IReadOnlyDictionary<string, EffectValue> inputs) =>
            BuildContext(board, policy, modifications, capability, inputs, null);

        private static EffectCapabilityContext BuildContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            RuleModificationSet modifications,
            EffectCapabilityReference capability,
            IReadOnlyDictionary<string, EffectValue> inputs,
            IReadOnlyList<IReadOnlyList<EffectValue>> answersAlreadyGiven) =>
            new EffectCapabilityContext(
                new EffectNodeId("displacement-capability-node"),
                policy,
                Actor,
                Actor,
                new CardInstanceId("displacement-capability-card"),
                new CardDefinitionReference(new CardDefinitionId("fixture-radius-context"), "fixture-rev-1"),
                new ActionDefinitionId("displacement-capability-action"),
                inputs,
                1,
                1,
                StagedBoardParticipants.Create(),
                null,
                modifications,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(),
                    board, board, board),
                null,
                answersAlreadyGiven,
                declaredOutputs: Handler(capability).Schema.Outputs);

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
                        new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-capability")),
                        new ConfiguredSpaceReference(
                            Map, new SpaceId($"q{minionAt.Q}_r{minionAt.R}")),
                        minionAt)
                });
        }

        /// <summary>
        /// Milestone 8.8. A wider star board with one more minion, on the actor's own team, standing
        /// to the West - so a declaration that names a side has something to leave alone. It is wider
        /// than the one above because a four-space push needs somewhere to land.
        /// </summary>
        private static AdjacentMoveAuthority CreateMixedTeamBoard() =>
            CreateStarBoard(
                new[]
                {
                    new HeroOccupancy(Actor, ActorAt),
                    new HeroOccupancy(Other, ActorAt.GetNeighbor(HexDirection.East))
                },
                new[]
                {
                    MinionBeside(Minion, ActorAt.GetNeighbor(HexDirection.NorthEast), TeamSide.Blue),
                    MinionBeside(AlliedMinion, ActorAt.GetNeighbor(HexDirection.West), TeamSide.Red)
                },
                radius: 6);

        /// <summary>
        /// Milestone 8.8. Two pushable heroes on opposite files from the actor, so one batch can stop
        /// one of them and not the other and the published stop causes have two heroes to tell apart.
        ///
        /// The third hero is not one this configuration's match lists, so the declaration that reaches
        /// it names no side of the board. Which side a unit stands on is the case above's subject;
        /// this one is about what a push publishes once it has its targets.
        /// </summary>
        private static AdjacentMoveAuthority CreateTwoPushedHeroBoard() =>
            CreateStarBoard(
                new[]
                {
                    new HeroOccupancy(Actor, ActorAt),
                    new HeroOccupancy(Other, ActorAt.GetNeighbor(HexDirection.East)),
                    new HeroOccupancy(SecondOther, ActorAt.GetNeighbor(HexDirection.West))
                },
                Array.Empty<QuickGameMinionSetupState>(),
                radius: 4);

        /// <summary>
        /// Milestone 8.8. The same arrangement on a board that simply ends three spaces East of the
        /// actor, which is the edge W-04 is about - nothing stands on the line at all.
        /// </summary>
        private static AdjacentMoveAuthority CreateShortEdgeBoard()
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -4; q <= 3; q++)
            {
                for (var r = Math.Max(-4, -q - 4); r <= Math.Min(4, -q + 4); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, coordinates),
                new[]
                {
                    new HeroOccupancy(Actor, ActorAt),
                    new HeroOccupancy(Other, ActorAt.GetNeighbor(HexDirection.East))
                },
                Array.Empty<ObstacleOccupancy>());
        }

        private static AdjacentMoveAuthority CreateStarBoard(
            IReadOnlyList<HeroOccupancy> heroes,
            IReadOnlyList<QuickGameMinionSetupState> minions,
            int radius)
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = Math.Max(-radius, -q - radius); r <= Math.Min(radius, -q + radius); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, coordinates),
                heroes,
                Array.Empty<ObstacleOccupancy>(),
                minions);
        }

        private static QuickGameMinionSetupState MinionBeside(
            MinionId minionId, HexCoordinate coordinate, TeamSide side) =>
            new QuickGameMinionSetupState(
                minionId,
                new TeamId(side == TeamSide.Blue
                    ? StagedBoardParticipants.BlueTeamIdValue
                    : StagedBoardParticipants.RedTeamIdValue),
                side,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-capability")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        private static PlacedToken PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            PlaceToken(board, coordinate, SyntheticComponentDefinitions.HazardV4);

        /// <summary>
        /// Places one token of the named kind. The kind is a parameter because the Hazard supply is one
        /// since milestone 6.12, so a case needing two tokens standing at once takes the second from the
        /// Ward's supply. Nothing here depends on which token it is: both are obstacles, and these cases
        /// are about what a displacement does to a token rather than about any token's own properties.
        /// </summary>
        private static PlacedToken PlaceToken(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            TokenDefinitionReference definition)
        {
            var token = new PlacedToken(
                new TokenInstanceId($"push-token-{coordinate.Q}-{coordinate.R}"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(definition),
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
                timing,
                null,
                StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null,
                null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the case below proves nothing.");
            return set;
        }
    }
}
