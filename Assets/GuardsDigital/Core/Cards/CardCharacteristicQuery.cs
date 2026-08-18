using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The typed predicates one card query asks. Every field is an explicit narrowing and an omitted
    /// field means "do not narrow on this", so a criteria that names nothing selects every card.
    ///
    /// Colour is a list rather than a single value because a verified effect may name more than one,
    /// and classification and ownership are nullable rather than using their zero members: for
    /// <see cref="CardClassification"/> the zero member is the positive fact that a definition states
    /// no classification, so a criteria has to be able to select exactly those and cannot spend the
    /// same value on "any".
    /// </summary>
    public sealed class CardCharacteristicCriteria
    {
        public CardCharacteristicCriteria(
            HeroId? ownerId = null,
            IEnumerable<CardColor> colors = null,
            CardClassification? classification = null,
            IEnumerable<CardTier> tiers = null,
            IEnumerable<CardLocation> locations = null,
            bool? hasBeenPlayed = null,
            CardResolutionStatus? resolutionStatus = null)
        {
            OwnerId = ownerId;
            Colors = Array.AsReadOnly((colors ?? Enumerable.Empty<CardColor>()).Distinct().ToArray());
            Classification = classification;
            Tiers = Array.AsReadOnly((tiers ?? Enumerable.Empty<CardTier>()).Distinct().ToArray());
            Locations = Array.AsReadOnly((locations ?? Enumerable.Empty<CardLocation>()).Distinct().ToArray());
            HasBeenPlayed = hasBeenPlayed;
            ResolutionStatus = resolutionStatus;

            if (OwnerId.HasValue && !OwnerId.Value.IsDefined)
            {
                throw new ArgumentException("A criteria owner must be a defined hero.", nameof(ownerId));
            }

            if (Colors.Any(color => color == CardColor.None))
            {
                throw new ArgumentException(
                    "CardColor.None is not a colour a criteria can select; select the classification " +
                    "instead to reach definitions that state no progression.",
                    nameof(colors));
            }

            if (Locations.Any(location => location == CardLocation.None))
            {
                throw new ArgumentException("A criteria location must be defined.", nameof(locations));
            }
        }

        public HeroId? OwnerId { get; }
        public IReadOnlyList<CardColor> Colors { get; }
        public CardClassification? Classification { get; }
        public IReadOnlyList<CardTier> Tiers { get; }
        public IReadOnlyList<CardLocation> Locations { get; }
        public bool? HasBeenPlayed { get; }
        public CardResolutionStatus? ResolutionStatus { get; }

        /// <summary>
        /// Whether this criteria asks anything about the card's printed characteristics rather than
        /// only about its state. It is what decides whether face orientation matters: a card whose
        /// rules-facing information is unavailable can still be selected by location or ownership -
        /// a facedown card is somewhere and belongs to someone - and can never be selected by colour,
        /// Tier, or classification.
        /// </summary>
        public bool AsksAboutPrintedCharacteristics =>
            Colors.Count > 0 || Classification.HasValue || Tiers.Count > 0;
    }

    /// <summary>
    /// Derives card candidates from immutable definitions and current instance state. Pure: it holds
    /// nothing, mutates nothing, and every answer is a function of the arguments.
    ///
    /// It is the query a colour-restricted choice, condition, count, cost, or effect composes with,
    /// and it is deliberately the only place that reads a colour. The mutation that follows - a
    /// discard, an upgrade transition - is handed the card that was chosen and re-derives none of
    /// this, which is what keeps one candidate set rather than two that can disagree.
    ///
    /// Two rules apply to every answer:
    ///
    /// <para>Determinism. Candidates come back ordered by instance id, so the same authoritative state
    /// and the same criteria produce the same ordered set whatever order the caller supplied its
    /// collections in. An empty result is an ordinary answer and never an error.</para>
    ///
    /// <para>Information availability. A facedown card has no rules-facing colour, Tier, or
    /// classification, so it cannot satisfy a predicate about any of them - the card system's contract
    /// puts it that way, and the point is that immutable content data remaining inspectable is not the
    /// same as the acting rules context being allowed to read it. In this build the facedown states a
    /// card can be in are a played-and-unrevealed turn card and a card set aside by p.15, and both are
    /// genuinely unreadable at the moment they are facedown.</para>
    /// </summary>
    public static class CardCharacteristicQuery
    {
        public static IReadOnlyList<CardInstanceId> FindCandidates(
            CardCharacteristicCriteria criteria,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            var definitionsByReference = BuildDefinitionIndex(definitions);

            return Array.AsReadOnly((instances ?? throw new ArgumentNullException(nameof(instances)))
                .Where(instance => instance != null && Matches(criteria, instance, definitionsByReference))
                .Select(instance => instance.Id)
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// The lowest Tier among the non-Basic cards this criteria selects - p.15's "lowest card Tier",
        /// which its own reminder says the Basic pair is "not considered when checking".
        ///
        /// The criteria carries the scope, and deliberately so. p.15 asks the question of the cards in
        /// the player's hand, and milestone 7.4 asks it of a hero being defeated mid-round, whose cards
        /// are scattered across the hand, the turn area and the discard pile. Which of those a reading
        /// covers is the asking rule's to state, so this answers whatever it is given rather than
        /// choosing a scope of its own and making every caller inherit it.
        ///
        /// False means no non-Basic card was selected, which is an answer rather than a failure: a hero
        /// holding only Basic cards has no lowest Tier.
        /// </summary>
        public static bool TryGetLowestNonBasicTier(
            CardCharacteristicCriteria criteria,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            out CardTier lowestTier)
        {
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            var definitionsByReference = BuildDefinitionIndex(definitions);

            lowestTier = CardTier.None;
            var found = false;
            foreach (var instance in instances ?? throw new ArgumentNullException(nameof(instances)))
            {
                if (instance == null || !Matches(criteria, instance, definitionsByReference))
                {
                    continue;
                }

                if (!definitionsByReference.TryGetValue(instance.DefinitionReference, out var definition) ||
                    !HasRulesFacingInformation(instance) ||
                    !definition.Progression.IsNonBasic)
                {
                    continue;
                }

                var tier = definition.Progression.Tier;
                if (!found || (int)tier < (int)lowestTier)
                {
                    lowestTier = tier;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// The definitions p.15 sends the player to their deck for: the same-colour, one-Tier-higher
        /// cards the named card states, resolved against this content set and returned in the order the
        /// card states them.
        ///
        /// A Basic card and a definition stating no progression both return empty, because neither has
        /// candidates to state - which is acceptance check one's exclusion, enforced at the profile
        /// rather than re-decided here. A candidate the set does not supply is skipped rather than
        /// throwing: <see cref="CardProgressionValidator"/> is what refuses such content at setup, and a
        /// query is not the place to fail a match over it.
        /// </summary>
        public static IReadOnlyList<CardDefinition> FindUpgradeCandidates(
            CardDefinitionId definitionId,
            IEnumerable<CardDefinition> definitions)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException(
                    "An upgrade query requires a defined definition ID.", nameof(definitionId));
            }

            var byId = new Dictionary<CardDefinitionId, CardDefinition>();
            foreach (var definition in definitions ?? throw new ArgumentNullException(nameof(definitions)))
            {
                if (definition != null && !byId.ContainsKey(definition.Id))
                {
                    byId.Add(definition.Id, definition);
                }
            }

            if (!byId.TryGetValue(definitionId, out var source))
            {
                return Array.Empty<CardDefinition>();
            }

            return Array.AsReadOnly(source.Progression.NextTierCandidateIds
                .Where(candidateId => byId.ContainsKey(candidateId))
                .Select(candidateId => byId[candidateId])
                .ToArray());
        }

        /// <summary>
        /// Whether this card's printed colour, Tier and classification are available to a rules
        /// question right now. Face orientation is the whole of it, and the reason it is asked here
        /// rather than left to each caller is that a colour predicate must never be satisfiable by
        /// content data the acting context cannot see.
        /// </summary>
        private static bool HasRulesFacingInformation(CardInstance instance) =>
            instance.State.FaceOrientation == CardFaceOrientation.FaceUp;

        private static Dictionary<CardDefinitionReference, CardDefinition> BuildDefinitionIndex(
            IEnumerable<CardDefinition> definitions)
        {
            var index = new Dictionary<CardDefinitionReference, CardDefinition>();
            foreach (var definition in definitions ?? throw new ArgumentNullException(nameof(definitions)))
            {
                if (definition != null && !index.ContainsKey(definition.Reference))
                {
                    index.Add(definition.Reference, definition);
                }
            }

            return index;
        }

        private static bool Matches(
            CardCharacteristicCriteria criteria,
            CardInstance instance,
            IReadOnlyDictionary<CardDefinitionReference, CardDefinition> definitionsByReference)
        {
            if (criteria.OwnerId.HasValue && !instance.OwnerId.Equals(criteria.OwnerId.Value))
            {
                return false;
            }

            if (criteria.Locations.Count > 0 && !criteria.Locations.Contains(instance.State.Location))
            {
                return false;
            }

            if (criteria.HasBeenPlayed.HasValue &&
                instance.State.HasBeenPlayed != criteria.HasBeenPlayed.Value)
            {
                return false;
            }

            if (criteria.ResolutionStatus.HasValue &&
                instance.State.ResolutionStatus != criteria.ResolutionStatus.Value)
            {
                return false;
            }

            if (!criteria.AsksAboutPrintedCharacteristics)
            {
                return true;
            }

            if (!HasRulesFacingInformation(instance) ||
                !definitionsByReference.TryGetValue(instance.DefinitionReference, out var definition))
            {
                // An unresolvable definition reference is treated exactly as unavailable information
                // rather than as a match: a card whose content this set does not supply cannot be
                // known to be of any colour.
                return false;
            }

            var progression = definition.Progression;
            if (criteria.Colors.Count > 0 && !criteria.Colors.Contains(progression.Color))
            {
                return false;
            }

            if (criteria.Classification.HasValue &&
                progression.Classification != criteria.Classification.Value)
            {
                return false;
            }

            return criteria.Tiers.Count == 0 || criteria.Tiers.Contains(progression.Tier);
        }
    }
}
