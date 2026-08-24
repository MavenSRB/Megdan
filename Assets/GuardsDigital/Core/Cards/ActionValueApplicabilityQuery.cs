using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Which class of action a value total is being composed for, so milestone 8.19c's qualified
    /// `modify-value@2` declarations can be admitted or left out.
    ///
    /// One reader of p.9's supertype and the action's own kind, for the reason
    /// <see cref="BasicActionQuery"/> is the one reader of the supertype after an action completes: the
    /// two facts are printed on the card and on the action, and a total that decided them for itself
    /// would be a second place a card could be read as basic.
    ///
    /// A composition holding no action - a Radius asked of an Active effect, a Defense total composed
    /// for a defender who has not chosen a card - answers <see cref="RuleValueApplicability.Always"/>,
    /// which admits exactly the unqualified declarations. That is the honest reading rather than an
    /// omission: a qualification about the action being performed cannot be met when none is.
    /// </summary>
    public static class ActionValueApplicabilityQuery
    {
        public static RuleValueApplicability ForOffer(
            ActionOffer offer, IEnumerable<CardDefinition> definitions) =>
            For(offer.SourceDefinitionReference, offer.ActionDefinitionId, definitions);

        /// <summary>
        /// The same answer for a caller holding the card and action identities rather than an offer -
        /// an effect node resolving inside an action, which asks about the action it is inside.
        /// </summary>
        public static RuleValueApplicability For(
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (!sourceDefinitionReference.IsDefined || !actionDefinitionId.IsDefined)
            {
                return RuleValueApplicability.Always;
            }

            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(sourceDefinitionReference)) continue;
                if (definition.Progression.Classification != CardClassification.Basic)
                {
                    return RuleValueApplicability.Always;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    if (!action.Id.Equals(actionDefinitionId)) continue;
                    return action.Kind == ActionKind.Attack
                        ? RuleValueApplicability.ControllerBasicAttackAction
                        : RuleValueApplicability.Always;
                }

                return RuleValueApplicability.Always;
            }

            return RuleValueApplicability.Always;
        }
    }
}
