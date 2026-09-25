using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriGreenBlockTests
    {
        private static readonly HeroId Defender = new HeroId("snorri-block-defender");
        private static readonly HeroId Attacker = new HeroId("snorri-block-attacker");

        [TestCase(0, 4, 1, DefenseBlockResult.Blocked)]
        [TestCase(0, 2, 1, DefenseBlockResult.ConditionUnfulfilled)]
        [TestCase(0, 2, 2, DefenseBlockResult.Blocked)]
        [TestCase(0, 1, 1, DefenseBlockResult.ConditionUnfulfilled)]
        [TestCase(1, 1, 1, DefenseBlockResult.Blocked)]
        [TestCase(1, 3, 2, DefenseBlockResult.ConditionUnfulfilled)]
        [TestCase(2, 3, 2, DefenseBlockResult.Blocked)]
        [TestCase(2, 4, 2, DefenseBlockResult.ConditionUnfulfilled)]
        public void BlockVerdictReadsRuneAndAttackCategory(
            int oathIndex, int rune, int attackIndex, DefenseBlockResult expected)
        {
            var oath = SnorriGreenFaces.Definitions
                .Where(card => SnorriGreenFaces.IsOath(card.Reference.DefinitionId))
                .ElementAt(oathIndex);
            var attack = attackIndex == 1
                ? SnorriAttackFaces.Definitions[0]
                : SnorriAttackFaces.Definitions[1];
            var attackAction = attack.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Attack);
            var markers = new MarkerAttachmentRegister();
            markers.Give(new MarkerAttachment(
                new MarkerInstanceId("snorri-green-rune-" + rune),
                SnorriComponentContent.Runes[rune - 1],
                Defender, Defender, new CardInstanceId("inscribe"),
                SnorriSilverFace.PrimarySkill, 2));
            var offer = new ActionOffer(
                new CardInstanceId("oath"), oath.Reference,
                SnorriGreenFaces.PrimaryDefense, Defender,
                ActionInvocationContext.PrintedPrimary, true);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                SnorriComponentContent.Handlers()
                    .Concat(SnorriGreenBlockContent.Handlers()));
            Assert.That(DefenseBlockQuery.Evaluate(
                offer, new[] { oath, attack }, attack.Reference, attackAction.Id,
                markers, Attacker, RuleModificationSet.Empty,
                null, null, catalog, 2), Is.EqualTo(expected));
            Assert.That(DefenseBlockQuery.Evaluate(
                offer, new[] { oath, attack }, attack.Reference, attackAction.Id,
                markers, Attacker, RuleModificationSet.Empty,
                null, null, catalog, 1),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "A rune in another turn slot cannot Block this attack.");
        }

        [Test]
        public void OathsAndDisruptionFacesKeepTheirPrintedShapes()
        {
            Assert.That(SnorriGreenFaces.Definitions.Count, Is.EqualTo(5));
            foreach (var oath in SnorriGreenFaces.Definitions.Where(card =>
                SnorriGreenFaces.IsOath(card.Reference.DefinitionId)))
            {
                var action = oath.ActionDefinitions.Single(value =>
                    value.Id.Equals(SnorriGreenFaces.PrimaryDefense));
                Assert.That(action.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Block));
                Assert.That(action.BlockCondition,
                    Is.EqualTo(PrintedBlockCondition.DeclaredByDefenseTextCapability));
                Assert.That(action.BaseValues.Any(value =>
                    value.Channel == PrintedValueChannel.Defense), Is.False);
                var ribbon = oath.ActionDefinitions.Single(value =>
                    value.Id.Equals(SnorriGreenFaces.SecondaryDefense));
                Assert.That(ribbon.DefenseResolution,
                    Is.EqualTo(PrintedDefenseResolution.Value));
                Assert.That(ribbon.BaseValues.Single(value =>
                    value.Channel == PrintedValueChannel.Defense).Amount,
                    Is.EqualTo(2));
            }
            Assert.That(SnorriGreenFaces.Definitions.Count(card =>
                card.ActionDefinitions.Any(action =>
                    action.Id.Equals(SnorriGreenFaces.PrimarySkill))), Is.EqualTo(2));
        }

        [Test]
        public void NonBasicRuneAlsoBlocksAnUltimateAttack()
        {
            var condition = new SnorriGreenBlockContent.ConditionHandler(
                SnorriGreenFaces.OathOfPerseverance);
            var markers = new MarkerAttachmentRegister();
            markers.Give(new MarkerAttachment(
                new MarkerInstanceId("ultimate-rune"),
                SnorriComponentContent.Runes[2], Defender, Defender,
                new CardInstanceId("inscribe"), SnorriSilverFace.PrimarySkill, 3));
            Assert.That(condition.Fulfils(
                Defender, markers, 3, CardClassification.Ultimate,
                AttackRangeKind.Ranged), Is.True);
            Assert.That(condition.Fulfils(
                Defender, markers, 3, CardClassification.Basic,
                AttackRangeKind.Ranged), Is.False);
        }

        [TestCase(false, 1, true)]
        [TestCase(true, 3, true)]
        [TestCase(false, 1, false)]
        public void GreenSkillMakesEnemyDiscardTheActiveRunesColor(
            bool runebomb, int rune, bool eligible)
        {
            var actor = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var friend = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var face = SnorriGreenFaces.Definitions.Single(card =>
                card.Reference.DefinitionId.Equals(runebomb
                    ? SnorriGreenFaces.InscribedRunebomb
                    : SnorriGreenFaces.ChargedRunestone));
            var skill = face.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Skill);
            var blue = SnorriBlueMovementFaces.Definitions[0];
            var gold = SnorriAttackFaces.Definitions[0];
            var source = CardInstance.CreateInHand(
                new CardInstanceId("green-source"), face.Reference, actor);
            var enemyBlue = eligible
                ? CardInstance.CreateInHand(
                    new CardInstanceId("enemy-blue"), blue.Reference, enemy)
                : new CardInstance(
                    new CardInstanceId("enemy-blue"), blue.Reference, enemy,
                    CardStateFacts.DiscardedUnplayed());
            var enemyGold = CardInstance.CreateInHand(
                new CardInstanceId("enemy-gold"), gold.Reference, enemy);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                    new HexCoordinate(-1, 0)
                },
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(1, 0)),
                    new HeroOccupancy(friend, new HexCoordinate(-1, 0))
                });
            var markers = new MarkerAttachmentRegister();
            markers.Give(new MarkerAttachment(
                new MarkerInstanceId("green-active-rune"),
                SnorriComponentContent.Runes[rune - 1], actor, actor,
                new CardInstanceId("inscribe"), SnorriSilverFace.PrimarySkill, 1));
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers()),
                board, markers, board, board, board);
            var context = new EffectResolutionContext(
                skill.EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers()
                        .Concat(new IEffectCapabilityHandler[]
                        {
                            new SnorriGreenEffectContent.SelectRuneHandler()
                        })),
                StagedBoardParticipants.CreateWithBlueAlly(),
                new ActiveEffectLifecycle(), 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { face, blue, gold },
                    new[] { source, enemyBlue, enemyGold }),
                ActionKind.Skill, components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("green-discard"),
                source.Id, face.Reference, skill.Id, actor, actor,
                ActionInvocationContext.PrintedPrimary, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            for (var guard = 0; guard < 8 && step.Frame?.PendingChoice != null; guard++)
            {
                var choice = step.Frame.PendingChoice;
                var invocation = choice.SelectInvocations.FirstOrDefault(command =>
                    command.Candidate.Value.Equals(EffectValue.Hero(enemy)));
                if (invocation == null)
                    invocation = choice.SelectInvocations.FirstOrDefault(command =>
                        command.Candidate.Value.Kind == EffectValueKind.CardInstance);
                Assert.That(invocation, Is.Not.Null, "The enemy or eligible card is offered.");
                step = EffectResolutionSession.Answer(step.Frame, invocation, context);
                Assert.That(step.Accepted, Is.True,
                    step.RejectionReason + " at " +
                    string.Join(",", step.Frame?.Continuation.Select(
                        value => value.NodeId.Value) ?? Array.Empty<string>()));
            }
            Assert.That(step.Frame?.PendingChoice, Is.Null);
            Assert.That((runebomb ? enemyGold : enemyBlue).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(enemyGold.State.Location,
                Is.EqualTo(runebomb ? CardLocation.DiscardPile : CardLocation.Hand));
        }

        [TestCase(0, 0)]
        [TestCase(4, 1)]
        public void OathTextCreatesTurnImmunityOnlyWithAnActiveRune(
            int rune, int expectedEffects)
        {
            var actor = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var friend = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var face = SnorriGreenFaces.Definitions[0];
            var defense = face.ActionDefinitions.Single(action =>
                action.Id.Equals(SnorriGreenFaces.PrimaryDefense));
            var source = CardInstance.CreateInHand(
                new CardInstanceId("oath-source"), face.Reference, actor);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                    new HexCoordinate(-1, 0)
                },
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(1, 0)),
                    new HeroOccupancy(friend, new HexCoordinate(-1, 0))
                });
            var markers = new MarkerAttachmentRegister();
            if (rune > 0)
                markers.Give(new MarkerAttachment(
                    new MarkerInstanceId("oath-rune"),
                    SnorriComponentContent.Runes[rune - 1], actor, actor,
                    new CardInstanceId("inscribe"), SnorriSilverFace.PrimarySkill, 1));
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers()),
                board, markers, board, board, board);
            var timing = new ActiveEffectLifecycle();
            var context = new EffectResolutionContext(
                defense.EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers()
                        .Concat(SnorriGreenBlockContent.Handlers())),
                StagedBoardParticipants.CreateWithBlueAlly(),
                timing, 1, 1, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { face }, new[] { source }),
                ActionKind.Defense, components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("oath-defense-text"),
                source.Id, face.Reference, defense.Id, actor, actor,
                ActionInvocationContext.PrintedPrimary, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(step.IsComplete, Is.True);
            Assert.That(timing.Effects.Count, Is.EqualTo(expectedEffects));
        }
    }
}
