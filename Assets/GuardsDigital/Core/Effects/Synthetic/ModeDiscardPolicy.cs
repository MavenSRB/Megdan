using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// W-022, PVE-D19. A mode's forced-discard rule for one side: a hero of that side discards only a
    /// card that prints a Defense, and a hero holding none is defeated instead - the step publishes
    /// the same defeated-hero fact a Defeat step publishes, so p.13's ordinary Defeat follows.
    /// </summary>
    /// <remarks>
    /// It applies only while the match is bound to mode rules naming that side, which no existing
    /// match is; every discard step then behaves exactly as before.
    /// </remarks>
    internal static class ModeDiscardPolicy
    {
        public static bool Applies(EffectCapabilityContext context, HeroId chooser)
        {
            if (!chooser.IsDefined || context?.RosterState == null || !context.RosterState.IsAvailable)
            {
                return false;
            }

            var rules = context.RosterState.ModeRules;
            return rules != null &&
                rules.DefeatInsteadOfDiscardSide != TeamSide.None &&
                context.Participants.GetTeamForHero(chooser).Side == rules.DefeatInsteadOfDiscardSide;
        }

        /// <summary>The cards in the chooser's hand that print a Defense.</summary>
        public static IReadOnlyList<CardInstanceId> DefenseCardsInHand(EffectCapabilityContext context, HeroId chooser)
        {
            var definitions = context.CardState.Definitions;
            return context.CardState.Instances
                .Where(card => card != null &&
                    card.OwnerId.Equals(chooser) &&
                    card.State.Location == CardLocation.Hand &&
                    definitions.Any(definition => definition != null &&
                        definition.Reference.Equals(card.DefinitionReference) &&
                        definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense)))
                .Select(card => card.Id)
                .ToArray();
        }

        public static EffectExecutionResult DefeatInstead(HeroId chooser) =>
            EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact(SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValue.Hero(chooser))
            });

        /// <summary>The offer narrowed to the allowed cards, or <c>null</c> when none of them is offered.</summary>
        public static EffectChoiceRequest Restrict(EffectChoiceRequest offer, IReadOnlyList<CardInstanceId> allowed)
        {
            if (offer == null)
            {
                return null;
            }

            var kept = offer.Candidates
                .Where(candidate => candidate.Kind == EffectValueKind.CardInstance &&
                    allowed.Contains(candidate.CardInstanceId))
                .ToArray();
            return kept.Length == 0
                ? null
                : new EffectChoiceRequest(
                    offer.Chooser,
                    kept,
                    offer.MinimumSelections,
                    offer.MaximumSelections,
                    offer.ChooserHeroId,
                    offer.ValueVocabulary);
        }
    }
}
