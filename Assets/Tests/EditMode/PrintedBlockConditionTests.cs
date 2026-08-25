using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.23. Rulebook p.14's Block condition is the defending card's own printed word.
    ///
    /// <para>p.14 says "If the Block condition is fulfilled, you successfully defend, regardless of the
    /// attack value. If not, you are defeated", and never says what a condition is. Milestone 8.11
    /// evaluated the only one verified content printed - "Block a ranged attack", on all three of
    /// Wasp's Green cards - by reading the attacking action's classification directly. Tigerclaw's five
    /// Blue cards print both arms of that sentence, so this milestone moved the condition onto the
    /// definition and these cases hold it there.</para>
    ///
    /// <para>They are hero-neutral on purpose: what is under test is the contract rather than either
    /// hero's set, and both arms have to be answerable before a card printing the second one exists.
    /// Tigerclaw's own fixtures prove his five faces end to end.</para>
    /// </summary>
    public sealed class PrintedBlockConditionTests
    {
        // ---------------------------------------------------------------------------------------
        // The declaration is required, and only where it means something
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ABlock_WithoutAPrintedCondition_IsRefused()
        {
            var refusal = Assert.Throws<ArgumentException>(() => BlockAction(PrintedBlockCondition.None));

            Assert.That(refusal.ParamName, Is.EqualTo("blockCondition"),
                "A Block that names no condition has no p.14 resolution at all, so it is refused " +
                "rather than resolved by whatever a reader assumes.");
        }

        [Test]
        public void ANumericDefense_DeclaringABlockCondition_IsRefused()
        {
            var refusal = Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("numeric-defense"),
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                blockCondition: PrintedBlockCondition.AttackIsRanged));

            Assert.That(refusal.ParamName, Is.EqualTo("blockCondition"),
                "Only a printed Block prints a Block condition; a numeric Defense states a rule its " +
                "face does not carry by naming one.");
        }

        [Test]
        public void EveryVerifiedBlock_DeclaresAPrintedCondition()
        {
            var blocks = VerifiedBlocks().ToArray();

            Assert.That(blocks.Length, Is.GreaterThan(0),
                "The verified content this build deals contains printed Blocks.");
            foreach (var (cardId, action) in blocks)
            {
                Assert.That(action.BlockCondition, Is.Not.EqualTo(PrintedBlockCondition.None),
                    $"{cardId.Value} prints p.14's shield, so it prints a condition.");
            }
        }

        [Test]
        public void WaspsThreeBlocks_StillDeclareTheRangedArmTheirFacesPrint()
        {
            foreach (var cardId in new[]
                     {
                         WaspCardContent.StopProjectiles,
                         WaspCardContent.DeflectProjectiles,
                         WaspCardContent.ReflectProjectiles
                     })
            {
                var block = WaspCardContent.Definition(cardId).ActionDefinitions
                    .Single(action => action.Id.Equals(WaspCardContent.PrimaryBlock));

                Assert.That(block.BlockCondition, Is.EqualTo(PrintedBlockCondition.AttackIsRanged),
                    $"{cardId.Value} prints \"Block a ranged attack\"; moving the reading onto the " +
                    "card must not have changed which attacks she stops.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // The two arms answer their own question and not each other's
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheRangedArm_BlocksARangedAttackAndIsUnfulfilledByANonRangedOne()
        {
            Assert.That(
                Evaluate(PrintedBlockCondition.AttackIsRanged, AttackRangeKind.Ranged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "p.14: the condition is fulfilled, so the attack is defended regardless of its value.");
            Assert.That(
                Evaluate(PrintedBlockCondition.AttackIsRanged, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "p.14's other half: it is not fulfilled, and the defending hero is defeated.");
        }

        [Test]
        public void TheNonRangedArm_BlocksANonRangedAttackAndIsUnfulfilledByARangedOne()
        {
            Assert.That(
                Evaluate(PrintedBlockCondition.AttackIsNotRanged, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "Tigerclaw's Parry and Riposte print this arm, and it is the exact complement.");
            Assert.That(
                Evaluate(PrintedBlockCondition.AttackIsNotRanged, AttackRangeKind.Ranged),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "A card that stops melee does not stop a shot; before this milestone it stopped both " +
                "and neither, because the condition was not the card's to state.");
        }

        [Test]
        public void AnActionCarryingNoAttackClassification_FulfilsNeitherArm()
        {
            foreach (var condition in new[]
                     {
                         PrintedBlockCondition.AttackIsRanged,
                         PrintedBlockCondition.AttackIsNotRanged
                     })
            {
                Assert.That(
                    EvaluateAgainstASkill(condition),
                    Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                    "Stating both arms rather than negating one is what keeps an action that is not " +
                    "an Attack from fulfilling the non-ranged condition by falling through.");
            }
        }

        [Test]
        public void AnAttackDefinition_CannotCarryNoClassificationAtAll()
        {
            var refusal = Assert.Throws<ArgumentException>(
                () => AttackAction(new ActionDefinitionId("unclassified"), AttackRangeKind.None));

            Assert.That(refusal.ParamName, Is.EqualTo("AttackRange"),
                "p.13 classifies every printed Attack and the definition layer already requires it, " +
                "so the case above can only be reached by naming an action that is not an Attack. " +
                "That is why the query answers by stating both arms rather than by negating one.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The verdict for answering an attack of the named classification with a Block printing the
        /// named condition, asked through the one query both the resolver and the defender's preview
        /// use.
        /// </summary>
        private static DefenseBlockResult Evaluate(
            PrintedBlockCondition condition, AttackRangeKind attackRange)
        {
            var attackCard = new CardDefinitionId("block-condition-attacker");
            var attackAction = new ActionDefinitionId("printed-attack");
            var blockCard = new CardDefinitionId("block-condition-defender");
            var definitions = new[]
            {
                Card(attackCard, AttackAction(attackAction, attackRange)),
                Card(blockCard, BlockAction(condition))
            };

            return DefenseBlockQuery.Evaluate(
                BlockOffer(definitions[1]), definitions, definitions[0].Reference, attackAction);
        }

        private static ActionOffer BlockOffer(CardDefinition blockCard) =>
            new ActionOffer(
                new CardInstanceId("block-condition-block-instance"),
                blockCard.Reference,
                new ActionDefinitionId("printed-block"),
                new GuardsDigital.Core.Units.Heroes.HeroId("block-condition-defender-hero"),
                ActionInvocationContext.PrintedPrimary,
                true);

        /// <summary>
        /// The same verdict when the named action is not an Attack at all - the only way an action
        /// carrying no p.13 classification can reach this query, because the definition layer refuses
        /// an Attack that carries none.
        /// </summary>
        private static DefenseBlockResult EvaluateAgainstASkill(PrintedBlockCondition condition)
        {
            var movementCard = new CardDefinitionId("block-condition-movement");
            var movementAction = new ActionDefinitionId("printed-movement");
            var blockCard = new CardDefinitionId("block-condition-defender");
            var definitions = new[]
            {
                Card(movementCard, new ActionDefinition(
                    movementAction,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.TurnArea))),
                Card(blockCard, BlockAction(condition))
            };

            return DefenseBlockQuery.Evaluate(
                BlockOffer(definitions[1]), definitions, definitions[0].Reference, movementAction);
        }

        private static IEnumerable<(CardDefinitionId CardId, ActionDefinition Action)> VerifiedBlocks()
        {
            foreach (var cardId in WaspCardContent.AllDefinitions.Select(card => card.Reference.DefinitionId))
            {
                foreach (var action in WaspCardContent.Definition(cardId).ActionDefinitions
                    .Where(action => action.DefenseResolution == PrintedDefenseResolution.Block))
                {
                    yield return (cardId, action);
                }
            }
        }

        private static CardDefinition Card(CardDefinitionId id, ActionDefinition action) =>
            new CardDefinition(
                id,
                "block-condition-rev-1",
                id.Value,
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[] { action });

        private static ActionDefinition AttackAction(
            ActionDefinitionId id, AttackRangeKind attackRange) =>
            new ActionDefinition(
                id,
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                attackRange == AttackRangeKind.Ranged
                    ? new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                    }
                    : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                attackRange,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        private static ActionDefinition BlockAction(PrintedBlockCondition condition) =>
            new ActionDefinition(
                new ActionDefinitionId("printed-block"),
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                new EffectDefinition(
                    new EffectDefinitionId("block-condition-text"),
                    "block-condition-rev-1",
                    new EffectNodeId("note"),
                    new[]
                    {
                        new EffectNodeDefinition(
                            new EffectNodeId("note"),
                            EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory,
                            SyntheticEffectCapabilities.NoteActorV1)
                    }),
                defenseResolution: PrintedDefenseResolution.Block,
                cardTextTiming: CardTextTiming.DefenseAction,
                blockCondition: condition);
    }
}
