using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// Which components a general cleanup checkpoint takes back, for both of the checkpoints the
    /// rulebook has.
    ///
    /// It answers questions and mutates nothing. The two registers own their own supplies - a token
    /// leaves through <see cref="ITokenPlacementAuthority.CommitValidatedTokenReclamation"/> and a marker
    /// through <see cref="MarkerAttachmentRegister.Return"/> - so this type exists to state the rule in
    /// one place rather than to become a third way of removing something.
    ///
    /// Milestone 6.12 wires both checkpoints. They live together here because they are the same sentence
    /// read at two moments: p.8's "Return all markers back into the supply" at the end of a round, and
    /// p.13's "Any markers they had are returned" when a hero is defeated. A marker declaring
    /// <see cref="MarkerReturnCondition.GeneralCleanup"/> is declaring both. Verified printed text may
    /// declare <see cref="MarkerReturnCondition.RetainedAtRoundEndAndDefeat"/> instead.
    /// </summary>
    public static class ComponentCleanupRules
    {
        /// <summary>
        /// The tokens rulebook p.8's "Remove all tokens from the board" takes, which is every placed
        /// token whose definition has not declared an override under p.19's "unless the card text says
        /// otherwise". A retained-round token stays in place with the same identity and properties;
        /// Clear, Spawn clearance and explicit Remove remain separate operations.
        /// </summary>
        public static IReadOnlyList<PlacedToken> TokensRemovedAtEndOfRound(
            IEnumerable<PlacedToken> placedTokens)
        {
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));
            return placedTokens
                .Where(token => token.Definition.RemovalCondition == TokenRemovalCondition.EndOfRoundCleanup)
                .ToArray();
        }

        /// <summary>
        /// The markers rulebook p.8's "Return all markers back into the supply" takes: every attachment
        /// declaring the general cleanup, whoever holds it. It is not a choice and not per-hero, which is
        /// why this takes the whole register's contents rather than a chooser.
        /// </summary>
        public static IReadOnlyList<MarkerAttachment> MarkersReturnedAtEndOfRound(
            IEnumerable<MarkerAttachment> attachments)
        {
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));
            return attachments
                .Where(attachment => attachment.ReturnCondition == MarkerReturnCondition.GeneralCleanup)
                .ToArray();
        }

        /// <summary>
        /// The markers rulebook p.13's "Any markers they had are returned" takes when a hero is defeated:
        /// the ones that hero <em>held</em>, not the ones its cards gave away. The page says "they had",
        /// and the attachment records holder and owner separately for exactly this reason.
        ///
        /// Tokens are deliberately absent. p.19's next sentence is explicit that "tokens are not removed
        /// when the hero who placed them is defeated", so a defeat that also cleared the board would be
        /// the one thing the page rules out.
        /// </summary>
        public static IReadOnlyList<MarkerAttachment> MarkersReturnedOnDefeat(
            IEnumerable<MarkerAttachment> attachments,
            HeroId defeatedHeroId)
        {
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));
            if (!defeatedHeroId.IsDefined)
            {
                throw new ArgumentException("A defeat return requires the defeated hero.", nameof(defeatedHeroId));
            }

            return attachments
                .Where(attachment =>
                    attachment.ReturnCondition == MarkerReturnCondition.GeneralCleanup &&
                    attachment.HolderHeroId.Equals(defeatedHeroId))
                .ToArray();
        }
    }
}
