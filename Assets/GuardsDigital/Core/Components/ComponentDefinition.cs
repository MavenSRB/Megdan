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
    /// One immutable rule supplied by a marker for exactly as long as a hero holds it. Milestone 8.23.
    ///
    /// <para><b>The twin of <see cref="TokenPropertyDefinition"/>, and the one difference is the whole
    /// reason it is a separate type.</b> A token property affects the token's <em>controller</em>: p.19
    /// puts the token on the board and the hero who placed it keeps it, so "you" in its text is that
    /// hero. A marker property affects its <em>holder</em>: p.18 describes markers as components
    /// "usually given to other heroes", and Tigerclaw's Poisoned Dagger says so in as many words - "The
    /// hero with a poison marker has -1 Initiative, -1 Attack and -1 Defense". The affected object is
    /// the hero the marker was given to, who is normally the opponent of the hero who owns it.</para>
    ///
    /// <para>Sharing the token type and reinterpreting its affected object per call site would have made
    /// one declaration mean two things depending on where it was read, which is exactly the ambiguity
    /// <c>PrintedBlockCondition</c> was minted to remove elsewhere in this milestone.</para>
    /// </summary>
    public sealed class MarkerPropertyDefinition
    {
        public MarkerPropertyDefinition(
            string propertyId,
            string ruleKind,
            string applicabilityKind,
            RuleModification modification = null)
        {
            if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(ruleKind) ||
                string.IsNullOrWhiteSpace(applicabilityKind))
            {
                throw new ArgumentException(
                    "A marker property requires stable identity, rule, and applicability.");
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
        /// The typed rule this property supplies to the marker's holder, or <c>null</c> for a property
        /// that only declares its source and lifetime.
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
    /// When a placed token leaves the board and goes back to the supply on its own.
    ///
    /// The token twin of <see cref="MarkerReturnCondition"/>, and deliberately narrower than it looks.
    /// Rulebook p.19 states the rule with its condition attached - "Unless the card text says otherwise,
    /// tokens are removed at the end of round (See Page 8)" - so <see cref="EndOfRoundCleanup"/> is the
    /// baseline every token declares and the "otherwise" is a member added with the verified card text
    /// that requires it, never ahead of it. No current definition needs one, so none exists.
    ///
    /// The same page's next sentence is the negative this enum must not be read as covering: "Tokens are
    /// not removed when the hero who placed them is defeated." That is already structural - a token's
    /// owner is provenance rather than lifetime, which <see cref="PlacedToken"/> says outright - so the
    /// p.13 defeat checkpoint returns markers and consults this enum not at all.
    ///
    /// Milestone 6.12 both declares this and wires the cleanup that reads it.
    /// </summary>
    public enum TokenRemovalCondition
    {
        None = 0,

        /// <summary>Removed to supply at the end of every round (p.8, "Remove Tokens, Return Markers").</summary>
        EndOfRoundCleanup = 1
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
            IEnumerable<TokenPropertyDefinition> properties,
            TokenRemovalCondition removalCondition)
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

            if (removalCondition == TokenRemovalCondition.None)
            {
                throw new ArgumentException(
                    "A token definition requires an explicit removal condition; a token that never " +
                    "leaves the board would contradict the end-of-round cleanup rulebook p.19 applies " +
                    "to every token whose card text does not say otherwise.",
                    nameof(removalCondition));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            Properties = new ReadOnlyCollection<TokenPropertyDefinition>(materializedProperties);
            RemovalCondition = removalCondition;
        }

        public TokenDefinitionReference Reference { get; }

        public TokenDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this token exist in the match, in total, placed or not.</summary>
        public int SupplyCount { get; }

        /// <summary>Persistent token-owned rules, queried as continuous sources while placed.</summary>
        public IReadOnlyList<TokenPropertyDefinition> Properties { get; }

        /// <summary>When this token leaves the board on its own (p.19's "unless the card text says otherwise").</summary>
        public TokenRemovalCondition RemovalCondition { get; }

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
            MarkerReturnCondition returnCondition,
            IEnumerable<MarkerPropertyDefinition> properties = null)
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

            var materializedProperties =
                (properties ?? Enumerable.Empty<MarkerPropertyDefinition>()).ToArray();
            if (materializedProperties.Any(property => property == null) ||
                materializedProperties.Select(property => property.PropertyId)
                    .Distinct(StringComparer.Ordinal).Count() != materializedProperties.Length)
            {
                throw new ArgumentException(
                    "A marker definition's properties are non-null and uniquely identified.",
                    nameof(properties));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            ReturnCondition = returnCondition;
            Properties = new ReadOnlyCollection<MarkerPropertyDefinition>(materializedProperties);
        }

        public MarkerDefinitionReference Reference { get; }

        public MarkerDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this marker exist in the match, in total, held or not.</summary>
        public int SupplyCount { get; }

        public MarkerReturnCondition ReturnCondition { get; }

        /// <summary>
        /// The rules this marker supplies to whichever hero holds it, for as long as they hold it.
        /// Empty for a marker that carries no rule of its own. Milestone 8.23.
        /// </summary>
        public IReadOnlyList<MarkerPropertyDefinition> Properties { get; }

        public override string ToString() => $"{Reference} x{SupplyCount}";
    }
}
