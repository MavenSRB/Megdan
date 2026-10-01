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
    /// asking a player to choose a spell (approved mockup A: "SUNBURST - 5 OUT: ATTACK 5, RANGE 5",
    /// items included) states the values the cast would use rather than computing its own.
    ///
    /// <para><b>One count.</b> <see cref="OtherRemoved"/> is <see cref="SpellbookQuery.Removed"/> with
    /// the spell's own definition excluded - lesson 27's "other spell cards removed", the number
    /// <see cref="SpellbookQuery.OtherRemoved"/> reads at the resolution once the spell has left the
    /// book. It is asked of the faceup pile alone, so every seat shown the choice reads the same
    /// number and no facedown spell is consulted.</para>
    ///
    /// <para><b>The value the cast will use.</b> <see cref="CastValues"/> composes a channel the way the
    /// cast's own totals do: the printed icon (<see cref="PrintedValueQuery"/>, a printed "0+" is an
    /// icon of 0), the caster's p.16 items on it (<see cref="ItemBonusQuery"/>, from the projected
    /// public item counts the rail's "6+1" badge reads - R1: Dimension Door's Radius icon reads the
    /// Radius item), and each count contribution the action declares on that channel
    /// (<see cref="RosterValueContributionDefinition.AmountPerMatch"/> per spell, a card-text
    /// contribution exactly as <c>RosterDynamicValueQuery</c> mints it, which items never raise). Coins
    /// are the counting step's <see cref="PerEvery"/>, which the step itself divides by; no item
    /// raises them.</para>
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
        /// The effective value of each channel <paramref name="spell"/>'s text raises by the count -
        /// Sunburst's Attack and Range, Dimension Door's Radius - as a cast now would use it, in the
        /// order the action prints them: the printed icon, the caster's matching p.16 items on it, and
        /// the count contributions for <paramref name="others"/>. Empty for a spell that raises none.
        /// </summary>
        /// <param name="casterItems">
        /// The caster's public item counts by icon - <c>HeroProgressionProjection.ItemBonuses</c>.
        /// </param>
        /// <param name="bonusPerItem">
        /// What one matching item adds right now, as <see cref="ItemBonusQuery.ProjectedItemBonus"/>
        /// takes it (p.16's <see cref="ItemBonusQuery.BonusPerItem"/> unless a source doubles it).
        /// </param>
        public static IReadOnlyList<KeyValuePair<PrintedValueChannel, int>> CastValues(
            CardDefinition spell,
            int others,
            IEnumerable<KeyValuePair<PrintedValueChannel, int>> casterItems,
            int bonusPerItem)
        {
            var primary = Primary(spell);
            if (primary == null)
            {
                return Array.Empty<KeyValuePair<PrintedValueChannel, int>>();
            }

            var items = (casterItems ?? Enumerable.Empty<KeyValuePair<PrintedValueChannel, int>>()).ToArray();
            // The reading names no instance - a facedown spell's is not every seat's to know - so the
            // printed icon is attributed to the definition, which only labels the contribution.
            var attribution = new CardInstanceId(spell.Id.Value);
            var raised = primary.RosterValueContributions.Where(ReadsTheCount).ToArray();
            return Array.AsReadOnly(primary.BaseValues
                .Where(value => raised.Any(contribution => contribution.Channel == value.Channel))
                .Select(value =>
                {
                    var printed = PrintedValueQuery.CalculatePrintedValue(
                        attribution, spell.Reference, primary.Id, new[] { spell }, value.Channel);
                    var withItems = ItemBonusQuery.TotalWithProjectedItems(
                        printed,
                        items.Where(item => item.Key == value.Channel).Sum(item => item.Value),
                        bonusPerItem);
                    // As the roster query does, no contribution at all for a count of nothing.
                    var counted = raised
                        .Where(contribution => contribution.Channel == value.Channel && others > 0)
                        .Select(contribution => new ValueContribution(
                            ValueContributionKind.CardTextModifier, attribution, primary.Id, value.Channel,
                            checked(Math.Max(0, others) * contribution.AmountPerMatch)));
                    return new KeyValuePair<PrintedValueChannel, int>(
                        value.Channel, checked(withItems + counted.Sum(contribution => contribution.Amount)));
                })
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
