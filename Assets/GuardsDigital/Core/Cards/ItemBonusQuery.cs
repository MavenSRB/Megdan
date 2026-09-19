using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Rulebook p.16's Item Bonus, and the only place it is decided.
    ///
    /// The box states three things and this type implements exactly those three:
    ///
    /// <list type="bullet">
    /// <item>"Each item increases the value of the matching icon on each of your Action cards by +1." The
    /// amount is always one and is never scaled, and the match is between the icon the item supplies and
    /// the channel being asked for. It is the icon rather than the slot since milestone 7.2a: p.16's
    /// fourth dashboard slot accepts Range, Movement and Radius together, so two items under one slot
    /// can raise different channels and the slot alone cannot say which.</item>
    /// <item>"If an Action card has no matching icon, the item has no effect on that particular Action
    /// card." This is the condition that makes the query need the printed result rather than just the
    /// channel: a Melee Attack card has no Range icon, so a +1 Range item leaves it with no Range at all
    /// rather than with a Range of 1. The page spells out that exact case - "if the card has no Range
    /// icon, a +1 Range item would have no effect".</item>
    /// <item>"Items never affect values in the card text, only the value of the Initiative, Attack,
    /// Defense, Movement, Range, or Radius icons on a card are affected." Nothing here reads or produces
    /// a card-text contribution: the condition is evaluated over
    /// <see cref="ValueContributionKind.PrintedIcon"/> contributions alone, so a total whose only entry
    /// in a channel came from card text gains no item bonus, and a card-text number is never itself
    /// raised.</item>
    /// </list>
    ///
    /// It is a pure query over state it is handed. It does not decide which items a hero has, which is
    /// <see cref="HeroItemState"/>'s, and it does not decide what a card prints, which is
    /// <see cref="PrintedValueQuery"/>'s. Every contribution it produces names the card instance that
    /// became the item, so a total can say which item raised it rather than merely being larger.
    /// </summary>
    public static class ItemBonusQuery
    {
        /// <summary>
        /// p.16: "Each item increases the value of the matching icon ... by +1."
        ///
        /// <para>Milestone 8.61h. This stays the page's own number and stays a constant. What Min the
        /// Dragonmonk's "This round: Double your item bonuses" changes is how many of these one item
        /// contributes right now, which is a fact about the board rather than about p.16 - so it
        /// arrives as the <c>bonusPerItem</c> argument every method below takes, derived once by
        /// <see cref="ItemBonusMultiplierQuery"/>. A caller composing the unmodified page names this
        /// constant, and one composing a live total names that query.</para>
        /// </summary>
        public const int BonusPerItem = 1;

        /// <summary>
        /// Read-only total from a public per-icon item count. Uses the same matching-icon rule as
        /// attributed item contributions; presentation need not reconstruct item state or own p.16.
        /// </summary>
        /// <param name="bonusPerItem">
        /// What one matching item adds right now - <see cref="BonusPerItem"/> for the page unmodified,
        /// or <see cref="ItemBonusMultiplierQuery.BonusPerItem"/> where a live rules set can be read.
        /// Stated rather than defaulted, by milestone 6.8f's rule: a reading that omitted it would
        /// silently report the printed bonus while a source had already doubled it.
        /// </param>
        public static int TotalWithProjectedItems(
            ValueResult printedValue, int matchingItemCount, int bonusPerItem)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            return checked(printedValue.Total +
                ProjectedItemBonus(printedValue, matchingItemCount, bonusPerItem));
        }

        /// <summary>
        /// What those projected items actually add to this printed value - zero whenever p.16's
        /// matching-icon condition fails, so a card with no icon of the channel gains nothing.
        ///
        /// <para>Milestone 8.55. The badge in the hero rail shows the printed number and the item
        /// bonus as two readings ("6+1") rather than their sum, so presentation needs the addend
        /// itself. It asks for it here rather than subtracting the printed total out of
        /// <see cref="TotalWithProjectedItems"/>: p.16's condition is decided in exactly one place,
        /// and the sum above is now that condition plus the printed total rather than a second
        /// derivation of the same rule.</para>
        ///
        /// <para>Milestone 8.61h. <paramref name="bonusPerItem"/> is what one item adds right now, so
        /// the badge reads `6+2` while Min's "Double your item bonuses" stands and `6+1` when it does
        /// not - the same two numbers, with the rule that scales them decided in one place rather than
        /// re-derived by the rail.</para>
        /// </summary>
        public static int ProjectedItemBonus(
            ValueResult printedValue, int matchingItemCount, int bonusPerItem)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            if (matchingItemCount < 0) throw new ArgumentOutOfRangeException(nameof(matchingItemCount));
            RequireBonusPerItem(bonusPerItem);
            return checked(PrintsMatchingIcon(printedValue) ? matchingItemCount * bonusPerItem : 0);
        }

        /// <summary>
        /// One item's contribution is p.16's own +1 or a multiple of it, never nothing and never
        /// negative: <see cref="RuleModificationKind.ItemBonusMultiplier"/> refuses a multiplier below
        /// two and this refuses anything that arrived here below one, so an item that reached this
        /// query can only ever raise an icon.
        /// </summary>
        private static void RequireBonusPerItem(int bonusPerItem)
        {
            if (bonusPerItem < BonusPerItem)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bonusPerItem),
                    bonusPerItem,
                    "Rulebook p.16 raises a matching icon by at least one point per item; a bonus " +
                    "below that is not a reading the page admits.");
            }
        }

        private static bool PrintsMatchingIcon(ValueResult value) =>
            value.Contributions.Any(contribution =>
                contribution.Kind == ValueContributionKind.PrintedIcon &&
                contribution.Channel == value.Channel);

        /// <summary>
        /// The item contributions one hero's items add to an already-computed printed value, in the
        /// hero's own item order. Empty whenever the hero has no item supplying that icon, or the card
        /// prints no icon of that channel.
        /// </summary>
        /// <param name="bonusPerItem">
        /// What one matching item adds right now - <see cref="BonusPerItem"/> for p.16 unmodified, or
        /// <see cref="ItemBonusMultiplierQuery.BonusPerItem"/> where a live rules set can be read. The
        /// amount lands on each contribution rather than on the total, so a doubled bonus stays
        /// attributable to the item card that supplied it exactly as a printed one is.
        /// </param>
        public static IReadOnlyList<ValueContribution> Contributions(
            ValueResult printedValue,
            HeroItemState items,
            HeroId ownerId,
            int bonusPerItem)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "An item bonus query requires the hero whose items are asked for.", nameof(ownerId));
            }

            RequireBonusPerItem(bonusPerItem);

            // "If an Action card has no matching icon, the item has no effect on that particular Action
            // card." A card prints an icon in this channel exactly when the printed result carries a
            // printed-icon contribution for it; a channel present only through card text or another
            // modifier is not a printed icon, and p.16 raises icons rather than numbers.
            if (!PrintsMatchingIcon(printedValue))
            {
                return Array.Empty<ValueContribution>();
            }

            return Array.AsReadOnly(items.GetItems(ownerId)
                .Where(item => item.Icon == printedValue.Channel)
                .Select(item => new ValueContribution(
                    ValueContributionKind.ItemModifier,
                    item.SourceCardInstanceId,
                    printedValue.Channel,
                    bonusPerItem))
                .ToArray());
        }

        /// <summary>
        /// The same contributions for a caller holding the live rules set, which is every rules query
        /// that composes a printed total. Milestone 8.61h.
        /// </summary>
        public static IReadOnlyList<ValueContribution> Contributions(
            ValueResult printedValue,
            HeroItemState items,
            HeroId ownerId,
            RuleModificationSet modifications) =>
            Contributions(
                printedValue, items, ownerId,
                ItemBonusMultiplierQuery.BonusPerItem(ownerId, modifications));

        /// <summary>
        /// The same printed value with its owner's item bonuses composed onto it. The printed
        /// contributions are kept and the item ones appended, so the total moves while every part of it
        /// stays separately attributable - the composition <see cref="ValueResult"/> exists for.
        /// </summary>
        public static ValueResult Apply(
            ValueResult printedValue, HeroItemState items, HeroId ownerId, int bonusPerItem)
        {
            var contributions = Contributions(printedValue, items, ownerId, bonusPerItem);
            return contributions.Count == 0
                ? printedValue
                : new ValueResult(
                    printedValue.Channel,
                    printedValue.Contributions.Concat(contributions));
        }

        /// <summary>
        /// The same composition for a caller holding the live rules set. Milestone 8.61h: this is the
        /// overload every value query uses, so Min's "This round: Double your item bonuses" reaches the
        /// Attack, Defense, Movement, Range, Radius and Initiative totals alike without any of them
        /// naming a multiplier.
        /// </summary>
        public static ValueResult Apply(
            ValueResult printedValue,
            HeroItemState items,
            HeroId ownerId,
            RuleModificationSet modifications) =>
            Apply(
                printedValue, items, ownerId,
                ItemBonusMultiplierQuery.BonusPerItem(ownerId, modifications));
    }
}
