using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherBoardPartitionRestrictionTests
    {
        private static readonly HeroId Controller = new HeroId("partition-controller");
        private static readonly HeroId Actor = new HeroId("partition-actor");

        [TestCase(false)]
        [TestCase(true)]
        public void OppositeSidesCannotInteractButLineTilesBelongToNeitherSide(bool separatesController)
        {
            var restriction = new BoardPartitionRestriction(
                new BoardPartitionLine(new HexCoordinate(0, 0), BoardPartitionAxis.EastWest),
                Controller, separatesController);
            var actor = RuleObject.ForHero(Actor);
            var target = RuleObject.ForHero(new HeroId("partition-target"));
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(0, 1)), Is.True);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(2, -1)), Is.False);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(2, 0)), Is.False);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(2, 0), target, new HexCoordinate(0, 1)), Is.False);
            Assert.That(restriction.BlocksSpaceInteraction(
                actor, new HexCoordinate(0, -1), new HexCoordinate(0, 1)), Is.True);
            Assert.That(restriction.BlocksSpaceInteraction(
                actor, new HexCoordinate(0, -1), new HexCoordinate(2, 0)), Is.False);
        }

        [Test]
        public void ShiftRealityMakesControllerAbsentFromEitherSideAfterControllerMoves()
        {
            var restriction = new BoardPartitionRestriction(
                new BoardPartitionLine(new HexCoordinate(0, 0), BoardPartitionAxis.EastWest),
                Controller, separatesController: true);
            var actor = RuleObject.ForHero(Actor);
            var target = RuleObject.ForHero(Controller);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(3, -1)), Is.True);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(3, 0)), Is.True);
            Assert.That(restriction.BlocksInteraction(
                actor, new HexCoordinate(3, 0), target, new HexCoordinate(3, 0)), Is.False);

            var weaker = new BoardPartitionRestriction(
                restriction.Line, Controller, separatesController: false);
            Assert.That(weaker.BlocksInteraction(
                actor, new HexCoordinate(0, -1), target, new HexCoordinate(3, -1)), Is.False);
        }

        [Test]
        public void AUnitMayEnterLineSpacesButCannotReachTheOppositeSide()
        {
            var hero = new HeroOccupancy(Actor, new HexCoordinate(0, 0));
            var map = BoardMapDefinition.CreateSynthetic(new MapId("partition-move-map"), 1,
                new[] { new HexCoordinate(-1, 0), hero.Coordinate,
                    new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var board = new AdjacentMoveAuthority(map, new[] { hero },
                Array.Empty<ObstacleOccupancy>());
            var rules = new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(), Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed,
                targetExclusions: Array.Empty<ActionTargetExclusion>(),
                boardPartitions: new[] { new BoardPartitionRestriction(
                    new BoardPartitionLine(new HexCoordinate(1, 0),
                        BoardPartitionAxis.NorthWestSouthEast),
                    Controller, separatesController: false) },
                actingObject: RuleObject.ForHero(Actor),
                actingSpace: hero.Coordinate);

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero, board, 2, rules);
            Assert.That(destinations, Has.Some.Property("Destination")
                .EqualTo(new HexCoordinate(1, 0)));
            Assert.That(destinations, Has.None.Property("Destination")
                .EqualTo(new HexCoordinate(2, 0)));
            Assert.That(ObstacleQuery.BlocksTransit(
                board, new HexCoordinate(2, 0), RuleObject.ForHero(Actor), rules,
                moverIgnoresObstacles: true), Is.True,
                "Obstacle-crossing permission cannot bypass an absent side.");

            var session = new MovementSession(board, Actor, 2);
            Assert.That(session.TryCommitStep(new HexCoordinate(1, 0), rules).Accepted, Is.True);
            var crossed = session.TryCommitStep(new HexCoordinate(2, 0), rules);
            Assert.That(crossed.Accepted, Is.False,
                "Taking a separate step from a neutral line tile cannot cross in one Movement action.");
            Assert.That(session.CurrentOccupancy.Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)));
        }

        [Test]
        public void ActiveEffectCloneKeepsItsChosenLineAndDerivesTheCorrectVariant()
        {
            var controller = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var line = new BoardPartitionLine(
                new HexCoordinate(4, -2), BoardPartitionAxis.NorthEastSouthWest);
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("partition-test-active"), NebKherComponentContent.ShiftReality,
                controller, TeamSide.Red, default, new CardInstanceId("partition-test-card"),
                new ActionDefinitionId("partition-test-action"), 1, 1,
                boardPartitionLine: line);

            Assert.That(timing.Effects.Single().BoardPartitionLine, Is.EqualTo(line));
            var cloned = timing.Clone();
            Assert.That(cloned.Effects.Single().BoardPartitionLine, Is.EqualTo(line));
            Assert.That(cloned.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));

            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("partition-lifecycle-map"), 1,
                    new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }),
                new[] { new HeroOccupancy(controller, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var rules = RuleModificationQuery.Build(
                cloned, board, StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red), UnlockedUltimateSet.None,
                RuleObject.ForHero(controller), null, null);
            Assert.That(rules.BoardPartitions, Has.Count.EqualTo(1));
            Assert.That(rules.BoardPartitions[0].Line, Is.EqualTo(line));
            Assert.That(rules.BoardPartitions[0].SeparatesController, Is.True);
            Assert.That(rules.ActingSpace, Is.EqualTo(new HexCoordinate(0, 0)));

            cloned.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 1, 1,
                controller, new CardInstanceId("partition-test-card")),
                new TieBreakerState(TeamSide.Red));
            Assert.That(RuleModificationQuery.Build(
                cloned, board, StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red), UnlockedUltimateSet.None,
                RuleObject.ForHero(controller), null, null).BoardPartitions, Is.Empty);
        }

        [Test]
        public void RadiusPresenceOmitsTheOtherSideAndShiftController()
        {
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("partition-presence-map"), 1,
                    new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, 0),
                        new HexCoordinate(1, 0), new HexCoordinate(2, 0) }),
                new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Controller, new HexCoordinate(2, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var line = new BoardPartitionLine(new HexCoordinate(1, 0),
                BoardPartitionAxis.NorthWestSouthEast);
            var rules = new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(), Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed,
                targetExclusions: Array.Empty<ActionTargetExclusion>(),
                boardPartitions: new[] { new BoardPartitionRestriction(
                    line, Controller, separatesController: false) },
                actingObject: RuleObject.ForHero(Actor),
                actingSpace: new HexCoordinate(0, 0));
            Assert.That(UnitPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 2, EffectiveUnitKind.Hero, board, rules),
                Has.No.Member(RuleObject.ForHero(Controller)));
            Assert.That(RosterPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 2, RosterUnitKind.Hero,
                SourceTeamRelation.AnyTeam, Actor, board,
                StagedBoardParticipants.Create(), rules),
                Has.None.Property("HeroId").EqualTo(Controller),
                "Roster-driven radius clauses must obey the same divided board.");
            Assert.That(UnitPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 2, EffectiveUnitKind.Hero, board,
                RuleModificationSet.Empty), Has.Member(RuleObject.ForHero(Controller)));

            var shifted = new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(), Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed,
                targetExclusions: Array.Empty<ActionTargetExclusion>(),
                boardPartitions: new[] { new BoardPartitionRestriction(
                    line, Controller, separatesController: true) },
                actingObject: RuleObject.ForHero(Actor),
                actingSpace: new HexCoordinate(0, 0));
            Assert.That(shifted.BlocksCurrentActorInteraction(
                RuleObject.ForHero(Controller), new HexCoordinate(-1, 0)), Is.True,
                "Shift also makes its controller absent on the same side.");
        }

        [Test]
        public void AxisChoiceCapturesTheActingHerosCurrentSpaceAsAnActiveEffectBinding()
        {
            var hero = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var at = new HexCoordinate(1, 0);
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("partition-choice-map"), 1,
                    new[] { at, new HexCoordinate(2, 0) }),
                new[] { new HeroOccupancy(hero, at) },
                Array.Empty<ObstacleOccupancy>());
            var timing = new ActiveEffectLifecycle();
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(), Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board, board);
            var context = new EffectCapabilityContext(
                new EffectNodeId("partition-choice"), EffectFailurePolicy.Mandatory,
                hero, hero, new CardInstanceId("partition-choice-card"), default,
                new ActionDefinitionId("partition-choice-action"),
                new Dictionary<string, EffectValue>(), 1, 1,
                StagedBoardParticipants.Create(), timing, RuleModificationSet.Empty,
                EffectCardState.Absent, ActionKind.Skill, components);
            var handler = new BoardPartitionCreationHandler(
                NebKherComponentContent.CrackRealityV1,
                NebKherComponentContent.CrackReality);
            var offered = handler.Execute(context);
            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.BoardPartitionAxis));
            Assert.That(offered.Choice.Candidates, Has.Count.EqualTo(3));
            Assert.That(handler.Resume(context,
                new[] { EffectValue.Integer((int)BoardPartitionAxis.EastWest) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(timing.Effects.Single().BoardPartitionLine,
                Is.EqualTo(new BoardPartitionLine(at, BoardPartitionAxis.EastWest)));
            Assert.That(handler.Resume(context,
                new[] { EffectValue.Integer(99) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(timing.Effects, Has.Count.EqualTo(1));
        }
    }
}
