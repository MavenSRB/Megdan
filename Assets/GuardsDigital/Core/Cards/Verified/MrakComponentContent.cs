using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Mrak the Rockshaper's physical component: the ROCK token. Milestone 8.112a.
    ///
    /// <para><b>A bare p.19 token.</b> "All tokens are obstacles", and no face of his prints a
    /// property, an aura or a lifetime for a rock, so the definition declares none: it is an
    /// impassable obstacle (it carries no <c>MayBeMovedThrough</c> clause), removed at the end of
    /// the round by p.8 and p.19's baseline and never on his defeat, and any hero's p.13 Clear can
    /// take it away. The same absence-is-the-rule reading Ignatia's magma carries.</para>
    ///
    /// <para><b>Three of them.</b> The user settled the supply on 2026-09-27 (MR-Q1): "Mark has 3
    /// tokens in supply". A card that needs more than the pile holds meets p.18's short-supply step
    /// through <see cref="ReclaimRockV1"/>, the shared hero-token reclamation Tali's and Ignatia's
    /// tokens use.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Mrak sits behind milestone 7.9's gate until his
    /// dealing block (8.112g) folds these into the build's catalogs.</para>
    /// </summary>
    public static class MrakComponentContent
    {
        /// <summary>USER-SETTLED 2026-09-27 (MR-Q1): "Mark has 3 tokens in supply".</summary>
        public const int RockSupplyCount = 3;

        public static TokenDefinitionId RockId { get; } = new TokenDefinitionId("mrak-rock");

        public static TokenDefinitionReference RockReference { get; } =
            new TokenDefinitionReference(RockId, 1);

        public static TokenDefinition Rock { get; } = new TokenDefinition(
            RockId,
            1,
            "Rock",
            RockSupplyCount,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.EndOfRoundCleanup);

        public static IEnumerable<TokenDefinition> Tokens() => new[] { Rock };

        /// <summary>
        /// The rock's own capabilities. EMPTY since 8.112g: 8.112a declared a standalone
        /// `mrak-reclaim-rock@1`, but every face that places rocks carries p.18's short-supply step as
        /// its own node (8.112b's `MrakRockPlacementContent` reclaim steps, which measure the demand of
        /// the placement that follows), so no printed card names the standalone one - and the dealing's
        /// registration gate refuses a registered capability no card reaches. Kept as the list fixtures
        /// compose, so a later rock operation has one home.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Milestone 8.112g. Every capability Mrak's dealt set names that the roster does not already
        /// own: the six placement steps (three p.18 short-supply steps and three placements) and the two
        /// Carapace grants. The one list the build registers.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Handlers()
                .Concat(MrakRockPlacementContent.Handlers())
                .Concat(MrakRedFaces.Handlers());

        /// <summary>
        /// Milestone 8.112f held his contracts here until the dealing. 8.112g DEALT HIM, so it is empty and
        /// they arrive through <see cref="RegisteredHandlers"/> - an empty list is what a discharged
        /// deferral looks like.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}
