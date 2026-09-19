using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The Movement allowance p.11 spends: the printed Movement icon, p.16's matching items, and
    /// milestone 8.19c's source-declared contributions.
    ///
    /// Promoted for the same reason and at the same milestone as <see cref="AttackValueQuery"/>.
    /// Xargatha's Metamorphosis gives "+1 Movement for each enemy unit adjacent to you" from an
    /// Ultimate, so the allowance stopped being decided only by the card being played.
    ///
    /// The order is the one milestone 7.2a settled and for its stated reason: everything that raises
    /// the allowance is composed before p.11's action-start restriction judges it, so a prevented action
    /// cannot be re-enabled afterwards by a contribution the restriction never saw.
    /// </summary>
    public static class MovementValueQuery
    {
        public static ValueResult Calculate(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (!offer.ActorId.IsDefined)
            {
                throw new ArgumentException(
                    "A Movement allowance requires the hero whose action it is.", nameof(offer));
            }

            var materialized = definitions as IReadOnlyList<CardDefinition> ?? definitions.ToArray();
            var printed = PrintedValueQuery.CalculatePrintedValue(
                offer, materialized, PrintedValueChannel.Movement);
            var composed = heroItems == null
                ? printed
                : ItemBonusQuery.Apply(printed, heroItems, offer.ActorId, modifications);
            var declared = modifications.ValueContributions(
                RuleObject.ForHero(offer.ActorId),
                PrintedValueChannel.Movement,
                ActionValueApplicabilityQuery.ForOffer(offer, materialized));
            return declared.Count == 0
                ? composed
                : new ValueResult(
                    PrintedValueChannel.Movement, composed.Contributions.Concat(declared));
        }
    }
}
