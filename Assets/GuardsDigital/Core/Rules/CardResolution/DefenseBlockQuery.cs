using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Rulebook p.14's Block condition, asked once and answered the same way everywhere.
    ///
    /// <para>Milestone 8.11 evaluated this inside the Attack resolver, which was the only place that
    /// needed it. Milestone 8.11a found the second place: the defence preview a defender is shown
    /// before choosing. That preview had been describing a Block as an ordinary Defense card worth
    /// nothing - total zero, no contributions, verdict <see cref="DefenseBlockResult.None"/> - so it
    /// predicted the defender losing an attack the Block in fact stops, and the prompt drawn from it
    /// looked for a printed Defense icon that a Block does not have and failed where it was drawn.</para>
    ///
    /// <para>Both callers ask this now. A preview that disagreed with the resolution would be worse
    /// than no preview at all: it is the thing a player decides from.</para>
    /// </summary>
    public static class DefenseBlockQuery
    {
        /// <summary>
        /// The verdict for answering an Attack with this Defense offer.
        /// <see cref="DefenseBlockResult.None"/> whenever the offer is not a printed Block, which is
        /// what says "this one is settled by its Defense value in the ordinary way".
        /// </summary>
        public static DefenseBlockResult Evaluate(
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference attackDefinitionReference,
            ActionDefinitionId attackActionDefinitionId)
        {
            if (definitions == null)
            {
                return DefenseBlockResult.None;
            }

            var action = ActionOfferQuery.FindActionDefinition(defenseOffer, definitions);
            if (action.DefenseResolution != PrintedDefenseResolution.Block)
            {
                return DefenseBlockResult.None;
            }

            // p.14's condition is read off the attacking action's own printed classification rather
            // than off anything the defender holds, which is why the attack has to be named to answer
            // this at all. Which classification fulfils it is the defending card's own printed word:
            // milestone 8.11 could assume "ranged" because all three of Wasp's Blocks print it, and
            // milestone 8.23 cannot, because Tigerclaw's Parry and Riposte print the other arm.
            var attack = definitions
                .FirstOrDefault(definition => definition.Reference.Equals(attackDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(attackActionDefinitionId));
            return attack != null && Fulfils(action.BlockCondition, attack.AttackRange)
                ? DefenseBlockResult.Blocked
                : DefenseBlockResult.ConditionUnfulfilled;
        }

        /// <summary>
        /// Whether one printed Block condition is fulfilled by one attack's printed classification.
        ///
        /// <para>Both arms are stated rather than one tested and the other left to fall through, for
        /// the reason <c>UltimateTriggeredEffectSourceQuery.Matches</c> states both of its: p.13
        /// classifies every printed Attack as Ranged or non-Ranged, so an action carrying neither is
        /// not an Attack at all and fulfils neither condition. A negation would have made such an
        /// action fulfil the non-Ranged arm, which is a rule no card prints.</para>
        /// </summary>
        private static bool Fulfils(PrintedBlockCondition condition, AttackRangeKind attackRange)
        {
            switch (condition)
            {
                case PrintedBlockCondition.AttackIsRanged:
                    return attackRange == AttackRangeKind.Ranged;
                case PrintedBlockCondition.AttackIsNotRanged:
                    return attackRange == AttackRangeKind.NonRanged;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether this offer is answered by a condition rather than by a number, which is what decides
        /// that no Defense value is calculated for it and no printed Defense icon is looked for.
        /// </summary>
        public static bool IsBlock(ActionOffer defenseOffer, IReadOnlyList<CardDefinition> definitions) =>
            definitions != null &&
            ActionOfferQuery.FindActionDefinition(defenseOffer, definitions).DefenseResolution ==
                PrintedDefenseResolution.Block;
    }
}
