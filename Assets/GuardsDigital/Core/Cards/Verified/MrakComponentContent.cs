using System;
using System.Collections.Generic;
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

        /// <summary>p.18's short-supply step over his rocks, for every face that places them.</summary>
        public static EffectCapabilityReference ReclaimRockV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mrak-reclaim-rock"), 1);

        public static IEnumerable<TokenDefinition> Tokens() => new[] { Rock };

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimRockV1, Rock)
            };
    }
}
