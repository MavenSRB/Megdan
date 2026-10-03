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
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// 9.13c: p.18's generic obstacles must participate in the same first-obstacle
    /// ray as units, tokens, and terrain. Cutter may name them; Bain may not name
    /// them but still cannot pull a unit through them. Off-map void is not a target.
    /// </summary>
    [TestFixture]
    public sealed class RangeObstacleGenericOccupancyTests
    {
        private static readonly CardDefinitionId Card = new CardDefinitionId("generic-obstacle-range-probe");
        private static readonly ActionDefinitionId Action = new ActionDefinitionId("generic-obstacle-range-action");
        private static readonly CardInstanceId Instance = new CardInstanceId("generic-obstacle-range-instance");
        private static HexCoordinate East(int distance) => PullLineBoard.At(HexDirection.East, distance);
        private static HexCoordinate West(int distance) => PullLineBoard.At(HexDirection.West, distance);

        [Test]
        public void CutterObstacleSelection_OffersGenericObstacleAndPublishesItsApproach()
        {
            var board = Board(withGenericObstacle: true, withEnemyBehind: true);
            var context = Context(board, unitsAndTokensOnly: false);
            var handler = Handler();
            var result = handler.Execute(context);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates.Select(value => value.Coordinate), Has.Member(East(2)));
            Assert.That(result.Choice.Candidates.Select(value => value.Coordinate), Has.No.Member(East(3)),
                "The farther hero has an obstacle between it and Cutter.");
            Assert.That(((IEffectSelectionCapabilityHandler)handler).AnticipateCandidates(context)
                .Select(value => value.Coordinate), Has.Member(East(2)),
                "The offer and the executed question must agree.");

            var answer = handler.Resume(context, new[] { EffectValue.Space(East(2)) });
            Assert.That(answer.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(answer.Outcomes.Single(fact => fact.OutputName ==
                SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName).Value.Coordinate,
                Is.EqualTo(East(1)));
        }

        [Test]
        public void BainUnitOrTokenSelection_GenericObstacleBlocksRayWithoutBecomingATarget()
        {
            var board = Board(withGenericObstacle: true, withEnemyBehind: true, withWestControl: true);
            var context = Context(board, unitsAndTokensOnly: true);
            var handler = Handler();
            var result = handler.Execute(context);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates.Select(value => value.Coordinate),
                Is.EquivalentTo(new[] { West(2) }),
                "The generic obstacle is not a unit/token, but still prevents selecting the hero behind it.");
            Assert.That(((IEffectSelectionCapabilityHandler)handler).AnticipateCandidates(context)
                .Select(value => value.Coordinate), Is.EquivalentTo(new[] { West(2) }));
            Assert.That(handler.Resume(context, new[] { EffectValue.Space(East(3)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Answer revalidation must not admit a unit through the blocking obstacle.");
        }

        [Test]
        public void CutterObstacleSelection_OffMapVoidDoesNotBecomeAnObstacleCandidate()
        {
            var board = Board(withGenericObstacle: false, withEnemyBehind: false, terrainRing: false);
            var context = Context(board, unitsAndTokensOnly: false, range: 8);
            var handler = Handler();

            Assert.That(handler.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(((IEffectSelectionCapabilityHandler)handler).AnticipateCandidates(context), Is.Empty);
            Assert.That(handler.Resume(context, new[] { EffectValue.Space(East(5)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The empty map ends at radius four; the void outside it is not printed terrain.");
        }

        private static IEffectCapabilityHandler Handler() => SyntheticTargetingCapabilities.Handlers()
            .Single(handler => handler.Reference.Equals(SyntheticTargetingCapabilities.RangeObstacleTargetV1));

        private static AdjacentMoveAuthority Board(
            bool withGenericObstacle, bool withEnemyBehind, bool withWestControl = false, bool terrainRing = true)
        {
            var map = PullLineBoard.CreateMap();
            if (!terrainRing)
            {
                map = new BoardMapDefinition(
                    PullLineBoard.Map, 1, "Generic obstacle finite board",
                    map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable).ToArray(),
                    new[] { new BoardMapZone(PullLineBoard.Battle, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
            }

            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(PullLineBoard.Actor, PullLineBoard.Origin)
            };
            if (withEnemyBehind) heroes.Add(new HeroOccupancy(PullLineBoard.Enemies[0], East(3)));
            if (withWestControl) heroes.Add(new HeroOccupancy(PullLineBoard.Enemies[1], West(2)));
            return new AdjacentMoveAuthority(map, heroes,
                withGenericObstacle
                    ? new[] { new ObstacleOccupancy(new ObstacleId("range-generic-wall"), East(2)) }
                    : Array.Empty<ObstacleOccupancy>());
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, bool unitsAndTokensOnly, int range = 5)
        {
            var definition = new CardDefinition(
                Card, "probe-v1", "Generic obstacle range probe", new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(Action, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, null,
                        PrintedTextComposition.DeferredUntil("Direct capability test; range-only definition."))
                });
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.All)
            };
            if (unitsAndTokensOnly)
                inputs[SyntheticTargetingCapabilities.ExcludeTerrainParameterName] = EffectValue.Boolean(true);

            return new EffectCapabilityContext(
                new EffectNodeId("generic-obstacle-selection"), EffectFailurePolicy.Mandatory,
                PullLineBoard.Actor, PullLineBoard.Actor, Instance,
                new CardDefinitionReference(Card, "probe-v1"), Action, inputs, 1, 1,
                PullLineBoard.Participants(), null, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()), ActionKind.Skill,
                new MatchComponents(SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board, board),
                declaredOutputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                        EffectValueKind.Coordinate),
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName,
                        EffectValueKind.Coordinate)
                });
        }
    }
}
