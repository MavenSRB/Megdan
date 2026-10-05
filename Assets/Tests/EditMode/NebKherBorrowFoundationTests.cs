using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherBorrowFoundationTests
    {
        [Test]
        public void TrustedBorrowNodeReDerivesReplacementKindAndMarkerPolicy()
        {
            var nodeId = new EffectNodeId("mind-grip-borrow");
            var cardId = new CardInstanceId("enemy-previous-card");
            var node = new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPreviousSlotBorrowCapabilities.BorrowPrintedActionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticPreviousSlotBorrowCapabilities.SubstituteTokenKindParameter,
                        EffectValue.TokenKind(NebKherComponentContent.Illusion.Id))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                        EffectValueKind.CardInstance),
                    new EffectOutputBinding(
                        SyntheticPreviousSlotBorrowCapabilities.BorrowedActionIndexOutput,
                        EffectValueKind.Integer)
                });
            var effect = new EffectDefinition(
                new EffectDefinitionId("mind-grip-test-text"), "rev-1", nodeId,
                new[] { node });
            var outcome = new EffectNodeOutcome(
                nodeId, EffectExecutionResultKind.Applied,
                new[]
                {
                    new EffectOutcomeFact(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                        EffectValue.Card(cardId)),
                    new EffectOutcomeFact(
                        SyntheticPreviousSlotBorrowCapabilities.BorrowedActionIndexOutput,
                        EffectValue.Integer(2))
                });
            var request = BorrowedActionQuery.Requested(effect, new[] { outcome });
            Assert.That(request.BorrowedCardId, Is.EqualTo(cardId));
            Assert.That(request.PrintedActionIndex, Is.EqualTo(2));
            Assert.That(request.SubstituteTokenKind,
                Is.EqualTo(NebKherComponentContent.Illusion.Id));
            Assert.That(request.SuppressMarkers, Is.True);
        }

        [Test]
        public void PrintedActionChoicePublishesTrustedCardAndActionIndex_AndRejectsStaleAnswers()
        {
            var match = MatchConfiguration.CreateStagedEmmittForInternalTest(TeamSide.Red);
            var source = match.RedHand.Instances[0];
            var enemyCard = match.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(EmmittCardContent.ReverseTime));
            var slot = Slot(match, source.OwnerId, enemyCard.OwnerId, source.Id, enemyCard.Id);
            var sourceDefinition = match.CardDefinitions.Single(definition =>
                definition.Reference.Equals(source.DefinitionReference));
            var context = new EffectCapabilityContext(
                new EffectNodeId("previous-slot-action"), EffectFailurePolicy.Mandatory,
                source.OwnerId, source.OwnerId, source.Id, source.DefinitionReference,
                sourceDefinition.ActionDefinitions[0].Id,
                new System.Collections.Generic.Dictionary<string, EffectValue>
                {
                    [SyntheticPreviousSlotBorrowCapabilities.EnemyParameter] =
                        EffectValue.Hero(enemyCard.OwnerId)
                },
                1, 2, match.Participants, null, RuleModificationSet.Empty,
                EffectCardState.Of(match.CardDefinitions, match.CardInstances),
                ActionKind.Skill,
                rosterState: EffectRosterState.Of(new[] { slot },
                    new RoundHeroDefeatHistory(match.Participants),
                    new HeroCoinLedger(match.Participants)),
                enclosingBorrow: BorrowRequest.ForPreviousSlotPrintedAction(
                    enemyCard.Id, 2, NebKherComponentContent.Illusion.Id));
            var handler = SyntheticPreviousSlotBorrowCapabilities.Handler();
            Assert.That(handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(handler.Execute(context).Choice.Candidates, Is.EqualTo(new[]
            {
                EffectValue.Integer(0), EffectValue.Integer(1), EffectValue.Integer(2)
            }));
            var defense = handler.Resume(context, new[] { EffectValue.Integer(2) });
            Assert.That(defense.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(defense.Outcomes.Single(fact =>
                    fact.OutputName == SyntheticBorrowedActionCapabilities.BorrowedCardOutput)
                .Value.CardInstanceId, Is.EqualTo(enemyCard.Id));
            Assert.That(defense.Outcomes.Single(fact =>
                    fact.OutputName == SyntheticPreviousSlotBorrowCapabilities.BorrowedActionIndexOutput)
                .Value.IntegerValue, Is.EqualTo(2));
            Assert.That(handler.Resume(context, new[] { EffectValue.Integer(3) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            var giveMarker = new SyntheticComponentCapabilities.MarkerGivingHandler(
                SyntheticComponentCapabilities.GiveMarkerV1,
                SyntheticComponentDefinitions.FocusDefinition);
            Assert.That(giveMarker.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional));
            Assert.That(giveMarker.Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "A borrowed action with the printed no-marker policy does not open a give choice.");
        }

        [Test]
        public void EnemyPreviousSlotOffersPrimaryAndBothPrintedSecondaryActions()
        {
            var match = MatchConfiguration.CreateStagedEmmittForInternalTest(TeamSide.Red);
            var actor = match.RedHand.Instances[0].OwnerId;
            var enemyCard = match.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(EmmittCardContent.ReverseTime));
            var enemy = enemyCard.OwnerId;
            var slot = Slot(match, actor, enemy,
                match.RedHand.Instances[0].Id, enemyCard.Id);

            var choices = PreviousSlotBorrowedActionQuery.Eligible(
                actor, enemy, 1, 2, match.Participants, new[] { slot },
                match.CardDefinitions, match.CardInstances);

            Assert.That(choices.Select(choice => choice.Kind), Is.EqualTo(new[]
            {
                ActionKind.Attack, ActionKind.Movement, ActionKind.Defense
            }));
            Assert.That(choices.Select(choice => choice.ActionId), Is.EqualTo(new[]
            {
                EmmittCardContent.PrimaryAttack,
                EmmittCardContent.SecondaryMovement,
                EmmittCardContent.SecondaryDefense
            }));
            Assert.That(choices.Select(choice => choice.PrintedIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(choices.All(choice => choice.CardId.Equals(enemyCard.Id)), Is.True);
            Assert.That(PreviousSlotBorrowedActionQuery.Eligible(
                    actor, enemy, 1, 1, match.Participants, new[] { slot },
                    match.CardDefinitions, match.CardInstances), Is.Empty,
                "The first turn has no previous turn slot.");
            Assert.That(PreviousSlotBorrowedActionQuery.Eligible(
                    enemy, enemy, 1, 2, match.Participants, new[] { slot },
                    match.CardDefinitions, match.CardInstances), Is.Empty,
                "Only an opposing hero's card qualifies.");
        }

        [Test]
        public void FacedownAndEmptyPreviousSlotsOfferNoAction()
        {
            var match = MatchConfiguration.CreateStagedEmmittForInternalTest(TeamSide.Red);
            var actor = match.RedHand.Instances[0].OwnerId;
            var enemyCard = match.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(EmmittCardContent.ReverseTime));
            var enemy = enemyCard.OwnerId;
            var facedown = new RoundTurnSlot(1, 1, new[]
            {
                new TurnCardAssignment(actor, match.RedHand.Instances[0].Id, false),
                new TurnCardAssignment(enemy, enemyCard.Id, false, true)
            }, match.CardInstances, match.Participants);
            var passed = new RoundTurnSlot(1, 1, new[]
            {
                new TurnCardAssignment(actor, match.RedHand.Instances[0].Id, false),
                new TurnCardAssignment(enemy, null, true)
            }, match.CardInstances, match.Participants);

            foreach (var slot in new[] { facedown, passed })
                Assert.That(PreviousSlotBorrowedActionQuery.Eligible(
                    actor, enemy, 1, 2, match.Participants, new[] { slot },
                    match.CardDefinitions, match.CardInstances), Is.Empty);
        }

        private static RoundTurnSlot Slot(
            MatchConfiguration match, HeroId actor, HeroId enemy,
            CardInstanceId actorCard, CardInstanceId enemyCard) =>
            new RoundTurnSlot(1, 1, new[]
            {
                new TurnCardAssignment(actor, actorCard, false),
                new TurnCardAssignment(enemy, enemyCard, false)
            }, match.CardInstances, match.Participants);
    }
}
