using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// One immutable rule supplied by a placed token for exactly as long as that token remains on the
    /// board. This is definition content: a snapshot names the token definition revision, and the
    /// trusted component catalog supplies these semantics again on restore.
    /// </summary>
    public sealed class TokenPropertyDefinition
    {
        public TokenPropertyDefinition(
            string propertyId,
            string ruleKind,
            string applicabilityKind,
            RuleModification modification = null)
        {
            if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(ruleKind) ||
                string.IsNullOrWhiteSpace(applicabilityKind))
            {
                throw new ArgumentException(
                    "A token property requires stable identity, rule, and applicability.");
            }

            PropertyId = propertyId;
            RuleKind = ruleKind;
            ApplicabilityKind = applicabilityKind;
            Modification = modification;
        }

        public string PropertyId { get; }

        public string RuleKind { get; }

        public string ApplicabilityKind { get; }

        /// <summary>
        /// The typed rule this property supplies, or <c>null</c> for a property that only declares its
        /// source and lifetime.
        ///
        /// Milestone 6.3 established the declaration without a concrete rule because none was verified
        /// yet; milestone 6.8 supplies the general typed families it can now carry. The affected object is
        /// the token's controller, which is the hero the derived
        /// <c>ContinuousRuleSource</c> already names - a token property that modifies something else needs
        /// the verified text that says so, not a member added ahead of it.
        /// </summary>
        public RuleModification Modification { get; }
    }

    /// <summary>
    /// When a marker leaves its holder and goes back to the supply.
    ///
    /// <see cref="GeneralCleanup"/> is the rulebook's baseline and currently the only member: markers
    /// return at the end of every round (p.8, "Remove Tokens, Return Markers") and every marker held by
    /// a defeated hero is returned (p.13). <c>STATE_CAPABILITIES.md</c> records that exact effects may
    /// add an *earlier* return condition but cannot weaken those two checkpoints, so this enum is the
    /// declared seam a later verified card extends. It is deliberately not widened ahead of that text:
    /// an invented member would be an invented rule.
    ///
    /// Milestone 6.2 records the condition and implements the return transition. It wires no automatic
    /// trigger to either checkpoint; end-of-round cleanup belongs to milestone 6.12.
    /// </summary>
    public enum MarkerReturnCondition
    {
        None = 0,

        /// <summary>Returned at end of round, and when the holding hero is defeated.</summary>
        GeneralCleanup = 1
    }

    /// <summary>
    /// The immutable, catalog-owned description of one kind of token: its identity and how many exist.
    ///
    /// The supply count is content, not runtime state. Rulebook p.19 says "tokens are intentionally
    /// limited", and p.18 makes the limit load-bearing - running out is what forces the reclamation
    /// choice rather than a silent failure. Holding the total here and deriving what remains from the
    /// board means the two can never disagree: there is no second number to keep in step.
    ///
    /// Milestone 6.3 adds the explicit property collection. A property is not an Active effect and has
    /// no card/hero cancellation condition; a placed instance exposes it as a token-owned continuous
    /// source until that instance is removed.
    /// </summary>
    public sealed class TokenDefinition
    {
        public TokenDefinition(
            TokenDefinitionId definitionId,
            int revision,
            string displayName,
            int supplyCount,
            IEnumerable<TokenPropertyDefinition> properties)
        {
            Reference = new TokenDefinitionReference(definitionId, revision);

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A token definition requires a display name.", nameof(displayName));
            }

            if (supplyCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supplyCount),
                    "A token definition's supply is finite and non-empty; a token type nobody can place is not content.");
            }

            var materializedProperties = (properties ?? throw new ArgumentNullException(nameof(properties)))
                .ToArray();
            if (materializedProperties.Any(property => property == null) ||
                materializedProperties.Select(property => property.PropertyId)
                    .Distinct(StringComparer.Ordinal).Count() != materializedProperties.Length)
            {
                throw new ArgumentException(
                    "Token properties must be non-null and have unique IDs inside their definition.",
                    nameof(properties));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            Properties = new ReadOnlyCollection<TokenPropertyDefinition>(materializedProperties);
        }

        public TokenDefinitionReference Reference { get; }

        public TokenDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this token exist in the match, in total, placed or not.</summary>
        public int SupplyCount { get; }

        /// <summary>Persistent token-owned rules, queried as continuous sources while placed.</summary>
        public IReadOnlyList<TokenPropertyDefinition> Properties { get; }

        public override string ToString() => $"{Reference} x{SupplyCount}";
    }

    /// <summary>
    /// The immutable, catalog-owned description of one kind of marker.
    ///
    /// Structurally the token definition's twin, and deliberately so: rulebook p.18 describes markers as
    /// components given to heroes rather than placed on the board, which is a statement about where they
    /// go, not about whether they are finite or identified. What differs is that this definition carries
    /// a return condition and no board concept at all.
    /// </summary>
    public sealed class MarkerDefinition
    {
        public MarkerDefinition(
            MarkerDefinitionId definitionId,
            int revision,
            string displayName,
            int supplyCount,
            MarkerReturnCondition returnCondition)
        {
            Reference = new MarkerDefinitionReference(definitionId, revision);

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A marker definition requires a display name.", nameof(displayName));
            }

            if (supplyCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supplyCount),
                    "A marker definition's supply is finite and non-empty.");
            }

            if (returnCondition == MarkerReturnCondition.None)
            {
                throw new ArgumentException(
                    "A marker definition requires an explicit return condition; a marker that never returns " +
                    "would contradict the general cleanup the rulebook applies to every marker.",
                    nameof(returnCondition));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            ReturnCondition = returnCondition;
        }

        public MarkerDefinitionReference Reference { get; }

        public MarkerDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this marker exist in the match, in total, held or not.</summary>
        public int SupplyCount { get; }

        public MarkerReturnCondition ReturnCondition { get; }

        public override string ToString() => $"{Reference} x{SupplyCount}";
    }
}
