using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.88a. Placement destinations compose the space clauses milestone 8.88 gave the
    /// printed-icon family - geometry, a moved or second centre, and adjacency to terrain, the actor's
    /// own tokens or a named space with an at-least count - on every `synthetic-place-*` declaration and
    /// on the hero-token placement every token-placing hero uses.
    ///
    /// <para>The clauses are one reading: <c>SyntheticTargetingCapabilities.PrintedIconSpaceClauses</c>
    /// answers them for a selection's candidate and for a placement's destination alike. Every clause is
    /// optional, so no placement composed before 8.88a changes - the complete suites are that
    /// evidence - and the answer to a placement re-asks them, as the operation re-asks everything
    /// else.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class PlacementClauseTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("placement-clauses");
        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        private static readonly EffectCapabilityReference ProbeTokenPlacementV1 =
            new EffectCapabilityReference(new EffectCapabilityId("placement-clause-probe-token"), 1);

        // -------------------------------------------------------------------------------------------
        // Conformance
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryPlacementDeclaration_TakesEverySpaceClause_Optionally()
        {
            var placements = SyntheticPlacementCapabilities.RegisteredHandlers()
                .Where(handler => handler.Reference.CapabilityId.Value.StartsWith("synthetic-place", StringComparison.Ordinal))
                .Concat(MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                    .Select(reference => MatchContentCatalogs.ForThisBuild().Capabilities.Load(reference))
                    .Where(handler => handler is IPlacedTokenCapabilityHandler))
                .ToArray();

            Assert.That(placements.Length, Is.GreaterThan(12),
                "Every board-piece placement and every registered hero-token placement is swept.");
            foreach (var placement in placements)
            {
                foreach (var clause in SyntheticTargetingCapabilities.PlacementClauseParameterNames)
                {
                    var declared = placement.Schema.Parameters.Where(parameter => parameter.Name == clause).ToArray();
                    Assert.That(declared.Length, Is.EqualTo(1), $"{placement.Reference} declares '{clause}'.");
                    Assert.That(declared[0].IsRequired, Is.False, $"{placement.Reference} carries '{clause}' optionally.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // Mrak - "place a rock token into an empty space adjacent to the target"
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ATokenPlacement_AdjacentToTheTarget_OffersOnlyTheTargetsNeighbours()
        {
            var target = new HexCoordinate(2, 0);
            var board = Board(enemyAt: target, secondEnemyAt: new HexCoordinate(-3, 3));
            var handler = TokenPlacement();

            var offered = Candidates(handler, board, radius: 3, new Dictionary<string, EffectValue>
            {
                [SyntheticTargetingCapabilities.AdjacentToSpaceParameterName] = EffectValue.Space(target)
            });

            var expected = target.GetNeighbors()
                .Where(space => HexDistanceQuery.Calculate(Origin, space) <= 3 && !space.Equals(Origin))
                .Select(EffectValue.Space)
                .ToArray();
            Assert.That(offered, Is.EquivalentTo(expected),
                "Every empty space in reach beside the target, and no other.");
        }

        [Test]
        public void ATokenPlacement_WithNoClause_OffersTheWholeDisk()
        {
            var board = Board(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(-3, 3));
            var offered = Candidates(TokenPlacement(), board, radius: 1, new Dictionary<string, EffectValue>());

            Assert.That(offered, Is.EquivalentTo(Origin.GetNeighbors().Select(EffectValue.Space)),
                "Unbound clauses narrow nothing: the six empty spaces beside the actor.");
        }

        // -------------------------------------------------------------------------------------------
        // Trinkets - "place yourself into a space in radius of the Turret"
        // -------------------------------------------------------------------------------------------

        [Test]
        public void PlacingYourself_InRadiusOfTheTurret_MeasuresFromTheTurret()
        {
            var turret = new HexCoordinate(4, -1);
            var board = Board(enemyAt: new HexCoordinate(-2, 0), secondEnemyAt: new HexCoordinate(-3, 3));
            var handler = SyntheticPlacementCapabilities.RegisteredHandlers()
                .Single(candidate => candidate.Reference.Equals(SyntheticPlacementCapabilities.PlaceInRadiusV1));

            var result = handler.Execute(Context(board, radius: 1, SelfPlacement(turret)));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates, Is.EquivalentTo(turret.GetNeighbors().Select(EffectValue.Space)),
                "The six spaces beside the Turret - none beside Trinkets himself.");
        }

        [Test]
        public void AClause_IsAskedAgainOfTheAnswer()
        {
            var turret = new HexCoordinate(4, -1);
            var board = Board(enemyAt: new HexCoordinate(-2, 0), secondEnemyAt: new HexCoordinate(-3, 3));
            var handler = SyntheticPlacementCapabilities.RegisteredHandlers()
                .Single(candidate => candidate.Reference.Equals(SyntheticPlacementCapabilities.PlaceInRadiusV1));

            Assert.That(
                handler.Resume(Context(board, radius: 1, SelfPlacement(turret)),
                    new[] { EffectValue.Space(new HexCoordinate(1, 0)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A space beside Trinkets but not beside the Turret is refused at the answer.");
        }

        // -------------------------------------------------------------------------------------------
        // A geometry clause on an existing arm
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheStraightLineClause_ComposesOnTheRadiusPlacement()
        {
            var board = Board(enemyAt: new HexCoordinate(-4, 4), secondEnemyAt: new HexCoordinate(-3, 3));
            var handler = SyntheticPlacementCapabilities.RegisteredHandlers()
                .Single(candidate => candidate.Reference.Equals(SyntheticPlacementCapabilities.PlaceInRadiusV1));
            var inputs = SelfPlacement(null);
            inputs[SyntheticTargetingCapabilities.RequireStraightLineParameterName] = EffectValue.Boolean(true);

            var result = handler.Execute(Context(board, radius: 2, inputs));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates.Count, Is.EqualTo(12),
                "Two spaces along each of p.19's six files, and nothing off them.");
            Assert.That(result.Choice.Candidates.All(value => StraightLineQuery.AreInStraightLine(Origin, value.Coordinate)),
                Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler TokenPlacement() =>
            SyntheticTokenCapabilities.PlaceHandler(
                ProbeTokenPlacementV1,
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV4),
                PrintedValueChannel.Radius);

        private static Dictionary<string, EffectValue> SelfPlacement(HexCoordinate? measuredFrom)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["subject"] = EffectValue.Space(Origin),
                [SyntheticPlacementCapabilities.EligiblePieceParameterName] =
                    EffectValue.Integer((int)PlaceEligiblePieceKind.UnitOrToken),
                [SyntheticPlacementCapabilities.DestinationAnchorParameterName] =
                    EffectValue.Integer((int)PlaceDestinationAnchor.ActingHero),
                [SyntheticPlacementCapabilities.SpawnPointExclusionParameterName] =
                    EffectValue.Integer((int)PlaceSpawnPointExclusion.None)
            };
            if (measuredFrom.HasValue)
            {
                inputs[SyntheticTargetingCapabilities.MeasuredFromParameterName] = EffectValue.Space(measuredFrom.Value);
            }

            return inputs;
        }

        private static EffectValue[] Candidates(
            IEffectCapabilityHandler handler,
            AdjacentMoveAuthority board,
            int radius,
            IReadOnlyDictionary<string, EffectValue> inputs)
        {
            var result = handler.Execute(Context(board, radius, inputs));
            return result.Kind == EffectExecutionResultKind.NeedsChoice
                ? result.Choice.Candidates.ToArray()
                : Array.Empty<EffectValue>();
        }

        private static AdjacentMoveAuthority Board(HexCoordinate enemyAt, HexCoordinate secondEnemyAt)
        {
            var spaces = new List<HexCoordinate>();
            for (var q = -6; q <= 6; q++)
            {
                for (var r = -6; r <= 6; r++)
                {
                    if (Math.Abs(q + r) <= 6) spaces.Add(new HexCoordinate(q, r));
                }
            }

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces, Array.Empty<HexCoordinate>()),
                new[]
                {
                    new HeroOccupancy(Actor, Origin),
                    new HeroOccupancy(Enemy, enemyAt),
                    new HeroOccupancy(SecondEnemy, secondEnemyAt)
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, int radius, IReadOnlyDictionary<string, EffectValue> inputs)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var action = new ActionDefinition(
                new ActionDefinitionId("placement-clause-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                ProbeText());
            var definition = new CardDefinition(
                new CardDefinitionId("placement-clause-probe"),
                "rev-1",
                "TEST - placement clause probe",
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("placement-clause-instance"), definition.Reference, Actor);

            return new EffectCapabilityContext(
                new EffectNodeId("placement-clause-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                card.Id,
                card.DefinitionReference,
                action.Id,
                inputs,
                1,
                1,
                participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { definition }, new[] { card }),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                new HeroItemState(participants));
        }

        private static EffectDefinition ProbeText()
        {
            var node = new EffectNodeId("placement-clause-root");
            return new EffectDefinition(
                new EffectDefinitionId("placement-clause-text"),
                "rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        })
                });
        }
    }
}
