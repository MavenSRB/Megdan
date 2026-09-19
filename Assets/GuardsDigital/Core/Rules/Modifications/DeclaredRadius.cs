using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Where a declared radius gets its number from. Rulebook p.16 makes the distinction a rule rather
    /// than a detail: "Items never affect values in the card text, only the value of the Initiative,
    /// Attack, Defense, Movement, Range, or Radius icons on a card are affected."
    /// </summary>
    public enum DeclaredRadiusSource
    {
        /// <summary>No radius is declared at all, which is what a definition drawing no disk states.</summary>
        None = 0,

        /// <summary>
        /// p.9's printed Radius icon: "If the card text mentions Range/Radius, it refers to this
        /// value." An item matching the icon raises it, exactly as it raises the same icon when the
        /// card is being performed.
        /// </summary>
        PrintedIcon = 1,

        /// <summary>
        /// A number the sentence itself writes down. p.16 never raises one of these, which is the
        /// distinction milestone 7.2b exists to make representable and which milestone 8.8 already
        /// applied to a written-down push distance.
        /// </summary>
        WrittenInText = 2
    }

    /// <summary>
    /// The radius one Active-effect declaration draws its disk at, as definition content.
    ///
    /// <para>Milestone 8.9. Wasp's two basic cards are the first verified texts whose persistent
    /// effects are scoped by a disk - Magnetic Dagger's "Enemy units in radius" and Static Barrier's
    /// "spaces in radius" - and both of them mean the icon printed on the card. So the declaration
    /// says which number it means rather than carrying a bare integer that could be either, and the
    /// resolution below is the one place p.16's Item Bonus reaches an Active effect.</para>
    ///
    /// <para>Nothing here is stored on an instance. p.17 makes the <em>anchor</em> the moving part -
    /// "an Active effect's Radius moves with its hero" - and this is the other half: the number is
    /// re-read at every derivation from the card the effect names and the items its controller holds,
    /// so a restored effect draws exactly the same disk again and no payload restates it.</para>
    /// </summary>
    public readonly struct DeclaredRadius : IEquatable<DeclaredRadius>
    {
        private DeclaredRadius(DeclaredRadiusSource source, int value)
        {
            Source = source;
            Value = value;
        }

        /// <summary>
        /// The named absence: this declaration draws no disk. It is a value rather than a null for the
        /// reason every other absence in this family is one - a definition that scopes nothing must say
        /// so, and a consumer that asks gets an answer rather than a dereference.
        /// </summary>
        public static DeclaredRadius None => default;

        /// <summary>p.9's printed Radius icon on the card this effect's source action belongs to.</summary>
        public static DeclaredRadius PrintedIcon(int printedValue)
        {
            if (printedValue < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(printedValue),
                    printedValue,
                    "A printed Radius icon states a positive value; p.9's smallest is 1, which reaches " +
                    "adjacent targets only.");
            }

            return new DeclaredRadius(DeclaredRadiusSource.PrintedIcon, printedValue);
        }

        /// <summary>A distance the sentence writes down, which p.16 never raises.</summary>
        public static DeclaredRadius WrittenInText(int writtenValue)
        {
            if (writtenValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(writtenValue),
                    writtenValue,
                    "A written radius is the inclusive disk p.9 defines and cannot be negative.");
            }

            return new DeclaredRadius(DeclaredRadiusSource.WrittenInText, writtenValue);
        }

        public DeclaredRadiusSource Source { get; }

        /// <summary>The declared number before p.16's items; zero when nothing is declared.</summary>
        public int Value { get; }

        public bool IsDefined => Source != DeclaredRadiusSource.None;

        /// <summary>
        /// The disk this declaration draws right now, for the hero whose card it is.
        ///
        /// The printed arm composes p.16's Item Bonus through <see cref="ItemBonusQuery"/>, the same
        /// query every printed total goes through, so an item that raises a card's Radius icon raises
        /// the effect that card's text created and nothing else has to know that. The written arm is
        /// returned untouched, which is the page's other sentence.
        /// </summary>
        /// <param name="items">
        /// The match's items, or <c>null</c> in a composition that holds none - the same named absence
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.RadiusValueQuery"/> expresses.
        /// </param>
        /// <param name="bonusPerItem">
        /// Milestone 8.61h. What one matching item adds right now -
        /// <see cref="ItemBonusQuery.BonusPerItem"/> for rulebook p.16 unmodified, or the multiple
        /// <c>ItemBonusMultiplierQuery</c> reads while Min's "This round: Double your item bonuses"
        /// stands.
        ///
        /// <para><b>It is a number rather than the live rules set, and the reason is where this is
        /// asked from.</b> <c>RuleModificationQuery</c> resolves an Active effect's disk while it is
        /// DERIVING that very set, so handing it one would be a circular question. It derives the
        /// multiplier from the same pass over the same effects instead, which is a local fact about
        /// declarations it is already reading, and hands the answer here. Every other caller composes
        /// its disk through <c>RadiusValueQuery</c>, which does take the set.</para>
        /// </param>
        public int Resolve(
            HeroId controllerHeroId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            HeroItemState items,
            int bonusPerItem)
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException(
                    "A declaration that draws no disk has no radius to resolve; ask IsDefined first.");
            }

            if (Source == DeclaredRadiusSource.WrittenInText || items == null)
            {
                return Value;
            }

            if (!controllerHeroId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !sourceActionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "A printed Radius icon belongs to one action on one card held by one hero; p.16's " +
                    "Item Bonus cannot be asked without all three.",
                    nameof(controllerHeroId));
            }

            var printed = new ValueResult(
                PrintedValueChannel.Radius,
                new[]
                {
                    new ValueContribution(
                        ValueContributionKind.PrintedIcon,
                        sourceCardInstanceId,
                        sourceActionDefinitionId,
                        PrintedValueChannel.Radius,
                        Value)
                });
            return ItemBonusQuery.Apply(printed, items, controllerHeroId, bonusPerItem).Total;
        }

        public bool Equals(DeclaredRadius other) => Source == other.Source && Value == other.Value;

        public override bool Equals(object obj) => obj is DeclaredRadius other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Source * 397) ^ Value;
            }
        }

        public override string ToString()
        {
            switch (Source)
            {
                case DeclaredRadiusSource.PrintedIcon:
                    return $"printed Radius {Value}";
                case DeclaredRadiusSource.WrittenInText:
                    return $"radius {Value} written in the text";
                default:
                    return "no radius";
            }
        }
    }
}
