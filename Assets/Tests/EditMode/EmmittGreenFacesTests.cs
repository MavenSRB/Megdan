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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittGreenFacesTests
    {
        [Test]
        public void BackToTheFuture_PlacesTheSelectedUnitAtItsRecordedTurnStartSpace()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var target = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var start = new HexCoordinate(2, 0);
            var moved = new HexCoordinate(3, 0);
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(0, 5).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(target, start)
                });
            board.RecordTurnStart();
            Assert.That(board.TryCommit(new AdjacentMoveCommand(target, start, moved)).Accepted, Is.True);
            var handler = MatchContentCatalogs.ForThisBuild().Capabilities.Load(
                EmmittBackToFutureContent.PlaceAtTurnStartV1);
            var context = new EffectCapabilityContext(
                new EffectNodeId("back-to-future-place-outcome"), EffectFailurePolicy.Mandatory,
                actor, actor, new CardInstanceId("emmitt-back-future-instance"),
                EmmittCardContent.BackToTheFutureDefinition.Reference,
                EmmittCardContent.PrimarySkill,
                new Dictionary<string, EffectValue>
                {
                    [EmmittBackToFutureContent.HeroParameter] = EffectValue.Hero(target),
                    [EmmittBackToFutureContent.SelectedSpaceParameter] = EffectValue.Space(moved)
                },
                1, 1, StagedBoardParticipants.Create(), new ActiveEffectLifecycle(),
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill,
                new MatchComponents(SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board));
            Assert.That(handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(handler.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryGetHeroCoordinate(target, out var at), Is.True);
            Assert.That(at, Is.EqualTo(start));
            Assert.That(handler.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void BackToTheFuture_PlacesTheChosenSecondBodyWithoutMovingTheFirst()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var target = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var first = new HexCoordinate(1, 0);
            var secondStart = new HexCoordinate(3, 0);
            var secondMoved = new HexCoordinate(4, 0);
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("emmitt-second-body"), 1,
                    Enumerable.Range(0, 6).Select(q => new HexCoordinate(q, 0))),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(target, 1, first),
                    new HeroOccupancy(target, 2, secondStart)
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>(),
                new[] { new HeroBodySupply(target, 2) });
            board.RecordTurnStart();
            Assert.That(board.TryCommit(new AdjacentMoveCommand(
                target, secondStart, secondMoved)).Accepted, Is.True);
            var handler = MatchContentCatalogs.ForThisBuild().Capabilities.Load(
                EmmittBackToFutureContent.PlaceAtTurnStartV1);
            var context = new EffectCapabilityContext(
                new EffectNodeId("back-to-future-second-body"), EffectFailurePolicy.Mandatory,
                actor, actor, new CardInstanceId("emmitt-back-future-body-instance"),
                EmmittCardContent.BackToTheFutureDefinition.Reference,
                EmmittCardContent.PrimarySkill,
                new Dictionary<string, EffectValue>
                {
                    [EmmittBackToFutureContent.HeroParameter] = EffectValue.Hero(target),
                    [EmmittBackToFutureContent.SelectedSpaceParameter] = EffectValue.Space(secondMoved)
                },
                1, 1, StagedBoardParticipants.Create(), new ActiveEffectLifecycle(),
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill,
                new MatchComponents(SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board));
            Assert.That(handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(handler.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryGetHeroBodyCoordinate(target, 1, out var firstAfter), Is.True);
            Assert.That(firstAfter, Is.EqualTo(first));
            Assert.That(board.TryGetHeroBodyCoordinate(target, 2, out var secondAfter), Is.True);
            Assert.That(secondAfter, Is.EqualTo(secondStart));
        }

        [Test]
        public void WalkAndForward_PrintTheirValuesAndValidateExactTwoSpaceMove()
        {
            AssertFace(EmmittCardContent.TimeWalkDefinition, 6, 2, 3,
                CardTier.I, PrintedValueChannel.None);
            AssertFace(EmmittCardContent.FastForwardDefinition, 5, 2, 4,
                CardTier.II, PrintedValueChannel.Attack);
            Assert.That(EmmittCardContent.TimeWalkDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.FastForward, EmmittCardContent.TimeCapsule }));
            Assert.That(EmmittCardContent.FastForwardDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.BackToTheFuture, EmmittCardContent.FutureProof }));
        }

        [Test]
        public void FutureProofImmunity_IsThisTurnForFriendlyHeroesInThePrintedRadius()
        {
            var effect = EmmittActiveEffectContent.FutureProofImmunityDefinition;
            Assert.That(effect.AffectedScope, Is.EqualTo(ActiveEffectAffectedScopeKind.HeroesInRadius));
            Assert.That(effect.AffectedUnitRelation, Is.EqualTo(SourceTeamRelation.AlliedWithSource));
            Assert.That(effect.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
            Assert.That(effect.CreationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
            Assert.That(effect.Modifications, Has.Count.EqualTo(1));
        }

        [Test]
        public void FutureProof_ProtectsOnlyFriendlyHeroesInRadiusFromEnemyActionsThisTurn()
        {
            var source = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var friend = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 8).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(source, new HexCoordinate(0, 0)),
                    new HeroOccupancy(friend, new HexCoordinate(1, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                });
            var timing = new ActiveEffectLifecycle();
            timing.Create(new ActiveEffectId("future-proof-live"),
                EmmittActiveEffectContent.FutureProofImmunityDefinition,
                source, TeamSide.Blue, default,
                new CardInstanceId("future-proof-card"), EmmittCardContent.PrimarySkill, 1, 1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionStarted, 1, 1),
                new TieBreakerState(TeamSide.Blue));
            var set = RuleModificationQuery.Build(timing, board, participants,
                TieBreakerState.Unrevealed, UnlockedUltimateSet.None, RuleObject.None,
                null, null);
            Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(friend), RuleActivity.Attack,
                RuleObject.ForHero(enemy), board, participants, set), Is.True);
            Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(friend), RuleActivity.Skill,
                RuleObject.ForHero(enemy), board, participants, set), Is.True,
                "enemy actions, not only attacks");
            Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(friend), RuleActivity.Attack,
                RuleObject.ForHero(source), board, participants, set), Is.False,
                "friendly actions are not barred");
            Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(source), RuleActivity.Attack,
                RuleObject.ForHero(enemy), board, participants, set), Is.False,
                "p.17 friendly does not include Emmitt himself");
            Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(enemy), RuleActivity.Attack,
                RuleObject.ForHero(source), board, participants, set), Is.False,
                "the enemy is not an affected friendly hero");
        }

        [Test]
        public void TimeCapsule_UsesCurrentTurnDiscardHistory_AndEachOwnerGetsAMay()
        {
            var card = EmmittCardContent.TimeCapsuleDefinition;
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(card.Initiative.Value, Is.EqualTo(5));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            Assert.That(skill.EffectRoot.Nodes.Count(node => node.Kind == EffectNodeKind.Optional &&
                node.FailurePolicy == EffectFailurePolicy.May), Is.EqualTo(3));
            Assert.That(skill.EffectRoot.Nodes.Count(node => node.Capability.Equals(
                EmmittTimeCapsuleContent.RetrieveCurrentTurnDiscardsV1)), Is.EqualTo(3));
            Assert.That(skill.EffectRoot.Nodes.Count(node => node.Capability.Equals(
                SyntheticTargetingCapabilities.RadiusHeroTargetV1) &&
                node.Inputs.Any(input => input.ParameterName ==
                    SyntheticTargetingCapabilities.DiscardedCardThisTurnParameterName)),
                Is.EqualTo(2));
            Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
        }

        [Test]
        public void BackToTheFuture_OffersHistoricalPlaceOrTheExactTimeWalkClause()
        {
            var card = EmmittCardContent.BackToTheFutureDefinition;
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(card.Initiative.Value, Is.EqualTo(5));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            Assert.That(skill.EffectRoot.Nodes.Single(node => node.Id.Equals(skill.EffectRoot.RootNodeId))
                .Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(skill.EffectRoot.Nodes.Any(node => node.Capability.Equals(
                EmmittBackToFutureContent.PlaceAtTurnStartV1)), Is.True);
            Assert.That(skill.EffectRoot.Nodes.Any(node => node.Capability.Equals(
                SyntheticTargetingCapabilities.RangeHeroTargetV1) &&
                node.Inputs.Any(input => input.ParameterName ==
                    SyntheticTargetingCapabilities.RemainedSincePreviousTurnParameterName)), Is.True);
            Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
        }

        [Test]
        public void FutureProof_ChoosesBetweenTurnDiscardsAndThisTurnImmunity()
        {
            var card = EmmittCardContent.FutureProofDefinition;
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(card.Initiative.Value, Is.EqualTo(5));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            Assert.That(skill.EffectRoot.Nodes.Single(node => node.Id.Equals(skill.EffectRoot.RootNodeId))
                .Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(skill.EffectRoot.Nodes.Count(node => node.Capability.Equals(
                EmmittTimeCapsuleContent.RetrieveCurrentTurnDiscardsV1)), Is.EqualTo(3));
            Assert.That(skill.EffectRoot.Nodes.Any(node => node.Capability.Equals(
                EmmittActiveEffectContent.FutureProofImmunityV1)), Is.True);
            Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
        }

        private static void AssertFace(CardDefinition card, int initiative, int defense,
            int range, CardTier tier, PrintedValueChannel item)
        {
            var action = card.ActionDefinitions.Single(value =>
                value.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(action.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(action.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(action.BaseValues.Single().Amount, Is.EqualTo(range));
            Assert.That(card.ActionDefinitions.Single(value => value.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(defense));
            Assert.That(card.ActionDefinitions.Single(value => value.Kind == ActionKind.Movement)
                .BaseValues.Single().Amount, Is.EqualTo(2));
            Assert.That(EffectDefinitionValidator.Validate(action.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
            Assert.That(action.EffectRoot.Nodes.Any(node =>
                node.Capability.Equals(SyntheticTargetingCapabilities.RangeHeroTargetV1) &&
                node.Inputs.Any(input => input.ParameterName ==
                    SyntheticTargetingCapabilities.RemainedSincePreviousTurnParameterName)), Is.True);
        }
    }
}
