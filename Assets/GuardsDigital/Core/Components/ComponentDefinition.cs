using System;

namespace GuardsDigital.Core.Components
{
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
    /// Milestone 6.2 owns identity, supply, and occupancy only. The continuous property sources p.19
    /// grants a placed token ("retains all its properties until that token is removed") belong to
    /// milestone 6.3, so this definition declares none.
    /// </summary>
    public sealed class TokenDefinition
    {
        public TokenDefinition(TokenDefinitionId definitionId, int revision, string displayName, int supplyCount)
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

            DisplayName = displayName;
            SupplyCount = supplyCount;
        }

        public TokenDefinitionReference Reference { get; }

        public TokenDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this token exist in the match, in total, placed or not.</summary>
        public int SupplyCount { get; }

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
