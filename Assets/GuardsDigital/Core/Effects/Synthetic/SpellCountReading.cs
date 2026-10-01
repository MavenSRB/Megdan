using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.30h. What a spell still in the book would count if it were cast now - "+1 Attack
    /// and +1 Range for each other spell card removed", "Gain 1 coin for every two other spell cards
    /// removed" - read from the spell's own declarations and the public removed pile, so a surface
    /// asking a player to choose a spell (approved mockup A: "SUNBURST - 5 OUT: ATTACK 5, RANGE 5")
    /// states the values the cast would use rather than computing its own.
    ///
    /// <para><b>One count.</b> <see cref="OtherRemoved"/> is <see cref="SpellbookQuery.Removed"/> with
    /// the spell's own definition excluded - lesson 27's "other spell cards removed", the number
    /// <see cref="SpellbookQuery.OtherRemoved"/> reads at the resolution once the spell has left the
    /// book. It is asked of the faceup pile alone, so every seat shown the choice reads the same
    /// number and no facedown spell is consulted.</para>
    ///
    /// <para><b>One arithmetic.</b> A value is the printed value plus each count contribution the
    /// action declares on that channel (<see cref="RosterValueContributionDefinition.AmountPerMatch"/>
    /// per spell, as <c>RosterDynamicValueQuery</c> applies it), and coins are the counting step's
    /// <see cref="PerEvery"/>, which the step itself divides by. p.16's items are the caster's and are
    /// drawn on his plaque, not counted here: this is the card's own value read now.</para>
    /// </summary>
    public static class SpellCountReading
    {
        /// <summary>
        /// The other spells <paramref name="ownerId"/>'s book has removed, as <paramref name="spell"/>
        /// would count them if cast now: the faceup pile, the spell itself excluded.
        /// </summary>
        public static int OtherRemoved(
            IEnumerable<CardInstance> instances, HeroId ownerId, CardDefinitionId spell)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            return SpellbookQuery.Removed(instances, ownerId)
                .Count(card => !card.DefinitionReference.DefinitionId.Equals(spell));
        }

        /// <summary>"For every <paramref name="every"/>", rounded down; the counting step divides by this.</summary>
        public static int PerEvery(int others, int every) => every < 1 || others < 0 ? 0 : others / every;

        /// <summary>Whether <paramref name="spell"/>'s printed primary reads the count of other spells removed.</summary>
        public static bool Counts(CardDefinition spell) =>
            Primary(spell) is ActionDefinition primary &&
            (primary.RosterValueContributions.Any(ReadsTheCount) || CountingStepDivisor(primary) > 0);

        /// <summary>
        /// The printed values of the channels <paramref name="spell"/>'s text raises by the count, with
        /// <paramref name="others"/> applied - Sunburst's Attack and Range, Dimension Door's Radius - in
        /// the order the action prints them. Empty for a spell that raises none.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<PrintedValueChannel, int>> CountedValues(
            CardDefinition spell, int others)
        {
            var primary = Primary(spell);
            if (primary == null)
            {
                return Array.Empty<KeyValuePair<PrintedValueChannel, int>>();
            }

            var raised = primary.RosterValueContributions.Where(ReadsTheCount).ToArray();
            return Array.AsReadOnly(primary.BaseValues
                .Where(value => raised.Any(contribution => contribution.Channel == value.Channel))
                .Select(value => new KeyValuePair<PrintedValueChannel, int>(
                    value.Channel,
                    value.Amount + raised
                        .Where(contribution => contribution.Channel == value.Channel)
                        .Sum(contribution => contribution.AmountPerMatch * Math.Max(0, others))))
                .ToArray());
        }

        /// <summary>
        /// The coins <paramref name="spell"/>'s counting step would gain for <paramref name="others"/>
        /// - Midas' Touch - or null for a spell whose text counts no coins.
        /// </summary>
        public static int? CountedCoins(CardDefinition spell, int others)
        {
            var primary = Primary(spell);
            var every = primary == null ? 0 : CountingStepDivisor(primary);
            return every > 0 ? PerEvery(others, every) : (int?)null;
        }

        private static bool ReadsTheCount(RosterValueContributionDefinition contribution) =>
            contribution.Query == RosterValueQueryKind.OtherSpellsRemovedFromSpellbook;

        private static ActionDefinition Primary(CardDefinition spell) =>
            spell?.ActionDefinitions.FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        /// <summary>
        /// The divisor of a counting step whose count a coin gain binds, or 0 where the action has none:
        /// <see cref="SyntheticSpellbookCapabilities.CountOtherRemovedSpellsV1"/> with a literal
        /// <c>every</c>, feeding <c>gain-resource@1</c>'s amount.
        /// </summary>
        private static int CountingStepDivisor(ActionDefinition primary)
        {
            var nodes = primary.EffectRoot?.Nodes;
            if (nodes == null)
            {
                return 0;
            }

            foreach (var count in nodes.Where(node =>
                node.Capability.Equals(SyntheticSpellbookCapabilities.CountOtherRemovedSpellsV1)))
            {
                var every = count.Inputs.Where(input =>
                    input.ParameterName == SyntheticSpellbookCapabilities.EveryParameter &&
                    input.SourceKind == EffectBindingSourceKind.Literal).ToArray();
                var feedsCoins = nodes.Any(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.GainResourceV1) &&
                    node.Inputs.Any(input =>
                        input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                        input.SourceNodeId.Equals(count.Id)));
                if (every.Length == 1 && feedsCoins && every[0].Literal.Kind == EffectValueKind.Integer)
                {
                    return every[0].Literal.IntegerValue;
                }
            }

            return 0;
        }
    }
}
