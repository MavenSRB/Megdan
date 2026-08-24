using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Milestone 8.19c1. What one `before-removal-prevention@1` source charges, as definition content.
    ///
    /// <para>It is content rather than a payload field for the reason every capability declaration in
    /// this engine is: the trusted catalog says what the card costs, and a snapshot names the source
    /// rather than restating its price. A restored pause therefore re-derives which cards could pay
    /// from the same declaration the live one read - which is what makes a saved decision impossible to
    /// cheapen by editing the file.</para>
    ///
    /// <para>The candidates are narrowed by colour and classification because that is how the cards
    /// that print this rule narrow them, and by location because "discard a card" is a question about
    /// cards a hero is holding rather than about every card it owns. An empty colour list and an
    /// unstated classification mean the card names none, which is the widest the declaration goes -
    /// never a default chosen here.</para>
    /// </summary>
    public readonly struct RemovalPreventionCost : IEquatable<RemovalPreventionCost>
    {
        private RemovalPreventionCost(
            RemovalPreventionCostKind kind,
            CardLocation fromLocation,
            IReadOnlyList<CardColor> admittedColors,
            CardClassification admittedClassification)
        {
            Kind = kind;
            FromLocation = fromLocation;
            AdmittedColors = admittedColors;
            AdmittedClassification = admittedClassification;
        }

        /// <summary>The named absence: a declaration that charges nothing.</summary>
        public static RemovalPreventionCost Free { get; } = new RemovalPreventionCost(
            RemovalPreventionCostKind.None,
            CardLocation.None,
            new ReadOnlyCollection<CardColor>(Array.Empty<CardColor>()),
            CardClassification.None);

        /// <summary>
        /// One owned card discarded from <paramref name="fromLocation"/>, optionally narrowed to the
        /// colours and classification the card names.
        /// </summary>
        public static RemovalPreventionCost DiscardOwnedCard(
            CardLocation fromLocation,
            IEnumerable<CardColor> admittedColors = null,
            CardClassification admittedClassification = CardClassification.None)
        {
            if (fromLocation == CardLocation.None ||
                !Enum.IsDefined(typeof(CardLocation), fromLocation))
            {
                throw new ArgumentException(
                    "A discard cost names the place the card is discarded from.", nameof(fromLocation));
            }

            var colors = (admittedColors ?? Enumerable.Empty<CardColor>()).ToArray();
            if (colors.Any(color => color == CardColor.None) ||
                colors.Any(color => !Enum.IsDefined(typeof(CardColor), color)) ||
                colors.Distinct().Count() != colors.Length)
            {
                throw new ArgumentException(
                    "A discard cost names defined colours, each once.", nameof(admittedColors));
            }

            if (!Enum.IsDefined(typeof(CardClassification), admittedClassification))
            {
                throw new ArgumentException(
                    "A discard cost names a defined classification, or none at all.",
                    nameof(admittedClassification));
            }

            return new RemovalPreventionCost(
                RemovalPreventionCostKind.DiscardOwnedCard,
                fromLocation,
                new ReadOnlyCollection<CardColor>(colors),
                admittedClassification);
        }

        public RemovalPreventionCostKind Kind { get; }

        /// <summary>Where the discarded card comes from; <c>None</c> when nothing is charged.</summary>
        public CardLocation FromLocation { get; }

        /// <summary>The colours a payment may be; empty when the card names none.</summary>
        public IReadOnlyList<CardColor> AdmittedColors { get; }

        /// <summary>The classification a payment must be; <c>None</c> when the card names none.</summary>
        public CardClassification AdmittedClassification { get; }

        public bool IsDeclared => Kind != RemovalPreventionCostKind.None;

        public bool Equals(RemovalPreventionCost other) =>
            Kind == other.Kind &&
            FromLocation == other.FromLocation &&
            AdmittedClassification == other.AdmittedClassification &&
            (AdmittedColors ?? Array.Empty<CardColor>() as IReadOnlyList<CardColor>)
                .SequenceEqual(other.AdmittedColors ?? Array.Empty<CardColor>());

        public override bool Equals(object obj) => obj is RemovalPreventionCost other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ((int)Kind * 397) ^ (int)FromLocation;
                hash = (hash * 397) ^ (int)AdmittedClassification;
                foreach (var color in AdmittedColors ?? Array.Empty<CardColor>())
                {
                    hash = (hash * 397) ^ (int)color;
                }

                return hash;
            }
        }

        public override string ToString() =>
            IsDeclared ? $"{Kind} from {FromLocation}" : "no cost";
    }
}
