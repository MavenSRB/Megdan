using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// One source offering to stop one removal, and the cards its controller could pay with right now.
    /// Milestone 8.19c1.
    /// </summary>
    public sealed class RemovalPreventionOption
    {
        public RemovalPreventionOption(
            string sourceId,
            HeroId decidingHeroId,
            RemovalPreventionCost cost,
            IEnumerable<CardInstanceId> payableCards)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "A removal-prevention option names the source offering it.", nameof(sourceId));
            }

            if (!decidingHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention option names the hero who decides.", nameof(decidingHeroId));
            }

            if (!cost.IsDeclared)
            {
                throw new ArgumentException(
                    "A removal-prevention option carries the price its source declared.", nameof(cost));
            }

            var cards = (payableCards ?? throw new ArgumentNullException(nameof(payableCards))).ToArray();
            if (cards.Any(card => !card.IsDefined) || cards.Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Payable cards are defined and named once each.", nameof(payableCards));
            }

            SourceId = sourceId;
            DecidingHeroId = decidingHeroId;
            Cost = cost;
            PayableCards = new ReadOnlyCollection<CardInstanceId>(cards);
        }

        public string SourceId { get; }

        /// <summary>The hero controlling the source; rulebook p.10 gives the decision to that hero.</summary>
        public HeroId DecidingHeroId { get; }

        public RemovalPreventionCost Cost { get; }

        /// <summary>
        /// The cards that could pay this option's price, re-derived at the moment the decision opens.
        /// Empty means the source is offering something its controller cannot afford, which is why
        /// <see cref="RemovalPreventionQuery"/> drops such an option rather than offering it.
        /// </summary>
        public IReadOnlyList<CardInstanceId> PayableCards { get; }
    }

    /// <summary>
    /// Milestone 8.19c1. Which sources are offering to stop a removal of one object, and what each of
    /// them would cost.
    ///
    /// <para>It is the one directed asker of the <see cref="RuleModificationKind.RemovalPrevention"/>
    /// family, in the same sense <see cref="ImmunityQuery"/> is the one directed asker of immunity: the
    /// set can say which declarations cover an object, and only something holding the cards can say
    /// whether their price can be paid. <see cref="RuleModificationSet"/> deliberately holds neither
    /// the cards nor the participants.</para>
    ///
    /// <para>Candidates are derived here rather than carried on the declaration because the hand moves.
    /// A pause opened when its controller held three payable cards, restored after two of them were
    /// played, must offer the one that is left - so the offer is a function of live state at the moment
    /// the decision is asked, and a restore asks it again.</para>
    /// </summary>
    public static class RemovalPreventionQuery
    {
        /// <summary>
        /// Every option offered for <paramref name="subject"/>, in the set's canonical order, dropping
        /// any whose price its controller cannot currently pay.
        /// </summary>
        public static IReadOnlyList<RemovalPreventionOption> Offered(
            RuleObject subject,
            RuleModificationSet modifications,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (!subject.IsDefined)
            {
                return Array.Empty<RemovalPreventionOption>();
            }

            var cardDefinitions = definitions.Where(definition => definition != null).ToArray();
            var instances = cards.Where(card => card != null).ToArray();
            var offered = new List<RemovalPreventionOption>();
            foreach (var applied in modifications.For(subject))
            {
                if (applied.Modification.Kind != RuleModificationKind.RemovalPrevention)
                {
                    continue;
                }

                var payable = PayableCards(
                    applied.ControllerHeroId, applied.Modification.RemovalPreventionCost,
                    cardDefinitions, instances);
                if (payable.Count == 0)
                {
                    // A price nobody can pay is not an offer. Opening a decision with no legal answer
                    // would stop the game on a question whose only outcome was the removal that was
                    // going to happen anyway.
                    continue;
                }

                offered.Add(new RemovalPreventionOption(
                    applied.SourceId,
                    applied.ControllerHeroId,
                    applied.Modification.RemovalPreventionCost,
                    payable));
            }

            return offered;
        }

        /// <summary>
        /// The cards one hero could pay one price with. The narrowing is the declaration's own -
        /// location, colours, classification - read through the shared card characteristics query so
        /// that "a Silver card" means here exactly what it means everywhere else.
        /// </summary>
        public static IReadOnlyList<CardInstanceId> PayableCards(
            HeroId decidingHeroId,
            RemovalPreventionCost cost,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (!cost.IsDeclared || !decidingHeroId.IsDefined)
            {
                return Array.Empty<CardInstanceId>();
            }

            return CardCharacteristicQuery.FindCandidates(
                new CardCharacteristicCriteria(
                    ownerId: decidingHeroId,
                    colors: cost.AdmittedColors.Count == 0 ? null : cost.AdmittedColors,
                    classification: cost.AdmittedClassification == CardClassification.None
                        ? (CardClassification?)null
                        : cost.AdmittedClassification,
                    locations: new[] { cost.FromLocation }),
                definitions,
                cards);
        }
    }
}
