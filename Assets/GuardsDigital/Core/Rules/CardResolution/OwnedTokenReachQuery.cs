using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Where one hero's own named token stands, and whether it stands inside the reach the performing
    /// card prints. Milestone 8.61b.
    ///
    /// <para><b>Why this is a query rather than a handler's private method, which is what it was.</b>
    /// Milestone 8.35z32's <c>OwnedTokenFactHandler</c> answered it for the effect frame alone, because
    /// the effect frame was the only thing that asked. Min the Dragonmonk's Poof!, Vanish and Ruse print
    /// "Swap with a [Smoke bomb] Smoke bomb token in range; if you do, block the attack", and rulebook
    /// p.14's Block condition is that swap's own precondition - so the defence preview a defender
    /// decides from and the Attack resolution that answers them both have to ask the identical question,
    /// before any frame exists to ask it in.</para>
    ///
    /// <para><b>One reading, three askers.</b> The handler's bounded space fact, the defence preview and
    /// the Attack backend all land here, which is milestone 8.40g's law applied to a board fact instead
    /// of a register: "the preview and resolver reach ONE verdict by construction, never two". A second
    /// implementation of "is her bomb in range" could grey a Block that in fact holds, or promise one
    /// that the swap then cannot perform - the exact disagreement milestone 8.11a exists to prevent.</para>
    ///
    /// <para><b>The reach is the card's own printed icon with p.16's items on it</b>, read through the
    /// same <see cref="RangeValueQuery"/> and <see cref="RadiusValueQuery"/> every other printed reach in
    /// this build is, so a Range item raises Poof!'s 3 to 4 without anything here knowing an item
    /// exists.</para>
    /// </summary>
    public static class OwnedTokenReachQuery
    {
        /// <summary>
        /// The placed copies of <paramref name="definition"/> this hero owns, capped at two.
        ///
        /// <para>The cap is the whole point: this contract is milestone 8.35z32's deliberately narrow
        /// one, for a token whose supply is a single physical piece, so what a caller needs to know is
        /// "none, one, or a malformed board carrying more" rather than a count. Nothing may pick an
        /// arbitrary copy of a singular piece.</para>
        /// </summary>
        public static IReadOnlyList<PlacedToken> OwnedTokens(
            TokenDefinition definition, HeroId actorId, ITokenPlacementAuthority tokens)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (tokens == null)
            {
                return Array.Empty<PlacedToken>();
            }

            return Array.AsReadOnly(tokens.PlacedTokens
                .Where(token => token.Definition.Reference.Equals(definition.Reference) &&
                    token.OwnerHeroId.Equals(actorId))
                .Take(2)
                .ToArray());
        }

        /// <summary>
        /// Where the hero's one copy of this token stands, when it stands inside the performing card's
        /// printed <paramref name="reachChannel"/> of the hero - or <c>false</c>, which is p.10's
        /// impossible step for a mandatory clause and an unfulfilled Block condition for p.14's.
        /// </summary>
        /// <param name="reachChannel">
        /// <see cref="PrintedValueChannel.Range"/> or <see cref="PrintedValueChannel.Radius"/> for a
        /// clause that prints one - "in range", "in radius" - or <c>null</c> for a clause that names the
        /// piece without bounding it, which is what Cobra's and Ruse's re-place read the standing bomb
        /// through. p.9 gives the two icons two meanings and p.16 two consequences, so they are two
        /// answers rather than one with a parameter that could be forgotten.
        /// </param>
        public static bool TryFindOwnedToken(
            TokenDefinition definition,
            PrintedValueChannel? reachChannel,
            HeroId actorId,
            ITokenPlacementAuthority tokens,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications,
            out HexCoordinate tokenAt)
        {
            tokenAt = default;
            if (reachChannel.HasValue &&
                reachChannel != PrintedValueChannel.Range &&
                reachChannel != PrintedValueChannel.Radius)
            {
                throw new ArgumentException(
                    "A printed reach is p.9's Range or Radius icon.", nameof(reachChannel));
            }

            var owned = OwnedTokens(definition, actorId, tokens);

            // A malformed board must not choose an arbitrary copy of a singular piece; neither arm of
            // this query is willing to guess which of two is "the" token.
            if (owned.Count != 1)
            {
                return false;
            }

            var at = owned[0].Coordinate;
            if (!reachChannel.HasValue)
            {
                tokenAt = at;
                return true;
            }

            if (definitions == null ||
                !tokens.TryGetHeroCoordinate(actorId, out var actorAt))
            {
                // A hero who is not standing anywhere measures no reach. It is stated rather than
                // assumed because the same derivation is re-run by p.14's own checks.
                return false;
            }

            var reach = reachChannel == PrintedValueChannel.Radius
                ? RadiusValueQuery.Calculate(
                    sourceCardInstanceId, sourceDefinitionReference, actionDefinitionId, definitions,
                    actorId, heroItems,
                    // Milestone 8.61h made the modification set a required argument (the item
                    // multiplier); merged here from 8.61b, which predates it.
                    modifications ?? RuleModificationSet.Empty).Total
                : RangeValueQuery.Calculate(
                    sourceCardInstanceId, sourceDefinitionReference, actionDefinitionId, definitions,
                    actorId, heroItems,
                    modifications ?? RuleModificationSet.Empty,
                    ActionValueApplicabilityQuery.For(
                        sourceDefinitionReference, actionDefinitionId, definitions)).Total;
            if (HexDistanceQuery.Calculate(actorAt, at) > reach)
            {
                return false;
            }

            tokenAt = at;
            return true;
        }
    }

    /// <summary>
    /// What a capability that publishes one hero's own named token declares about the piece it is bound
    /// to. Milestone 8.61b.
    ///
    /// <para><b>Why a capability declares this and a card definition does not.</b> Min's three Block
    /// faces resolve by whether her Smoke bomb is in range, and something has to say which token
    /// "her Smoke bomb" is. The card cannot: the only place a component reference could live on one is a
    /// new public property on <see cref="ActionDefinition"/>, and milestone 8.38d measured exactly what
    /// that costs - the content-identity walker encodes every public property of every object it
    /// reaches, so every dealt hero's digest moves while no hero's printed content did.</para>
    ///
    /// <para><b>And a second declaration would be a second register.</b> The card's Defense text already
    /// names the piece, in the node whose bounded fact the mandatory swap consumes; a token named again
    /// beside it could come to disagree with the swap that actually runs, which is precisely what p.14's
    /// "if you do" forbids. So the Block condition reads the token off that node - one statement, and
    /// the clause's own.</para>
    /// </summary>
    public interface IOwnedTokenFactDeclaration
    {
        /// <summary>The single-piece token definition this capability is bound to.</summary>
        TokenDefinition TokenDefinition { get; }

        /// <summary>
        /// The printed icon this capability bounds the piece's position by, or <c>null</c> where it
        /// publishes the position unbounded.
        /// </summary>
        PrintedValueChannel? ReachChannel { get; }

        /// <summary>
        /// Whether this capability publishes only whether the piece is placed, rather than where it
        /// stands. A presence fact bounds nothing and answers no reach question.
        /// </summary>
        bool PublishesPresenceOnly { get; }
    }
}
