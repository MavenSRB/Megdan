using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>p.16: "Each item increases the value of the matching icon ... by +1."</summary>
        public const int BonusPerItem = 1;

        /// <summary>
        /// Read-only total from a public per-icon item count. Uses the same matching-icon rule as
        /// attributed item contributions; presentation need not reconstruct item state or own p.16.
        /// </summary>
        public static int TotalWithProjectedItems(ValueResult printedValue, int matchingItemCount)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            return checked(printedValue.Total + ProjectedItemBonus(printedValue, matchingItemCount));
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
        /// </summary>
        public static int ProjectedItemBonus(ValueResult printedValue, int matchingItemCount)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            if (matchingItemCount < 0) throw new ArgumentOutOfRangeException(nameof(matchingItemCount));
            return checked(PrintsMatchingIcon(printedValue) ? matchingItemCount * BonusPerItem : 0);
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
        public static IReadOnlyList<ValueContribution> Contributions(
            ValueResult printedValue,
            HeroItemState items,
            HeroId ownerId)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "An item bonus query requires the hero whose items are asked for.", nameof(ownerId));
            }

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
                    BonusPerItem))
                .ToArray());
        }

        /// <summary>
        /// The same printed value with its owner's item bonuses composed onto it. The printed
        /// contributions are kept and the item ones appended, so the total moves while every part of it
        /// stays separately attributable - the composition <see cref="ValueResult"/> exists for.
        /// </summary>
        public static ValueResult Apply(ValueResult printedValue, HeroItemState items, HeroId ownerId)
        {
            var contributions = Contributions(printedValue, items, ownerId);
            return contributions.Count == 0
                ? printedValue
                : new ValueResult(
                    printedValue.Channel,
                    printedValue.Contributions.Concat(contributions));
        }
    }
}
