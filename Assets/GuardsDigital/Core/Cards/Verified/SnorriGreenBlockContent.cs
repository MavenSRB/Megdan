using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Content-owned rune/category verdict and turn immunity for the Oath Blocks.</summary>
    public static class SnorriGreenBlockContent
    {
        public static EffectCapabilityReference ConditionFor(CardDefinitionId card) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(card.Value + "-block-condition"), 1);

        public static EffectCapabilityReference ImmunityFor(CardDefinitionId card) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(card.Value + "-enemy-immunity"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            foreach (var card in SnorriGreenFaces.OathCards)
            {
                yield return new ConditionHandler(card);
                yield return new ActiveEffectCreationHandler(
                    ImmunityFor(card), ImmunityDefinitionFor(card));
            }
        }

        public static IEnumerable<ActiveEffectDefinition> Definitions()
        {
            foreach (var card in SnorriGreenFaces.OathCards)
                yield return ImmunityDefinitionFor(card);
        }

        private static ActiveEffectDefinition ImmunityDefinitionFor(CardDefinitionId card) =>
            new ActiveEffectDefinition(
                card.Value + "-enemy-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false, "SnorriOath", card.Value + "-enemy-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForImmunity(
                        new[] { RuleActivity.All },
                        RestrictedActorScope.Of(
                            RestrictedActorRelation.HeroesOpposingSource))
                });

        public sealed class ConditionHandler :
            IEffectCapabilityHandler, IPrintedBlockConditionDeclaration
        {
            private readonly CardDefinitionId card;

            public ConditionHandler(CardDefinitionId card)
            {
                if (!SnorriGreenFaces.IsOath(card))
                    throw new ArgumentException("Not an Oath Block.", nameof(card));
                this.card = card;
            }

            public EffectCapabilityReference Reference => ConditionFor(card);
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema();
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public bool Fulfils(
                HeroId defender, MarkerAttachmentRegister markers, int turnIndex,
                CardClassification attackClassification, AttackRangeKind attackRange)
            {
                var rune = SnorriComponentContent.ActiveRune(markers, defender, turnIndex);
                if (rune == null) return false;
                var ordinal = Array.FindIndex(
                    SnorriComponentContent.Runes,
                    definition => definition.Reference.Equals(rune.Definition.Reference)) + 1;
                return ordinal == 4 && attackClassification == CardClassification.Basic ||
                    ordinal == 2 && attackRange == AttackRangeKind.NonRanged ||
                    !card.Equals(SnorriGreenFaces.OathOfEndurance) &&
                        ordinal == 1 && attackRange == AttackRangeKind.Ranged ||
                    card.Equals(SnorriGreenFaces.OathOfPerseverance) &&
                        ordinal == 3 &&
                        (attackClassification == CardClassification.NonBasic ||
                            attackClassification == CardClassification.Ultimate);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A Block condition never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
