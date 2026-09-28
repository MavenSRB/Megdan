using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Wuk the Grove Keeper's physical component: the TREE token. Milestone 8.114a.
    ///
    /// <para><b>A bare p.19 token that stays.</b> "All tokens are obstacles", and no face gives a Tree
    /// a property or an aura, so the definition declares none: it is an impassable obstacle (no
    /// <c>MayBeMovedThrough</c> clause), never removed on Wuk's defeat (p.19), and any hero's p.13
    /// Clear or a card's Remove can take it away. What it does NOT do is leave at the end of the
    /// round: Mystic Saplings prints "Tree tokens are not removed at the end of round", and the user
    /// settled on 2026-09-28 (WU-Q2) that the sentence is a standing property of every Tree token,
    /// whichever card placed it - the reading Snorri's identical rune-marker sentence has. So the
    /// definition declares <see cref="TokenRemovalCondition.RetainedAtEndOfRound"/>, p.19's "unless
    /// the card text says otherwise", exactly as Widget's Turret does.</para>
    ///
    /// <para><b>Three of them.</b> The user settled the supply on 2026-09-28 (WU-Q1). A card that
    /// needs more than the pile holds meets p.18's short-supply step through
    /// <see cref="ReclaimTreeV1"/>, the shared hero-token reclamation Ignatia's magma uses, which
    /// every placing face composes before each placement it makes.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Wuk sits behind milestone 7.9's gate until his
    /// dealing block (8.114f) folds these into the build's catalogs.</para>
    /// </summary>
    public static class WukComponentContent
    {
        /// <summary>USER-SETTLED 2026-09-28 (WU-Q1): Wuk's Tree supply is three.</summary>
        public const int TreeSupplyCount = 3;

        public static TokenDefinitionId TreeId { get; } = new TokenDefinitionId("wuk-tree");

        public static TokenDefinitionReference TreeReference { get; } =
            new TokenDefinitionReference(TreeId, 1);

        public static TokenDefinition Tree { get; } = new TokenDefinition(
            TreeId,
            1,
            "Tree",
            TreeSupplyCount,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.RetainedAtEndOfRound);

        public static IEnumerable<TokenDefinition> Tokens() => new[] { Tree };

        /// <summary>"Place a Tree token in radius" - one supply Place reaching the card's printed Radius.</summary>
        public static EffectCapabilityReference PlaceTreeInRadiusV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-place-tree-in-radius"), 1);

        /// <summary>
        /// p.18's short-supply step before one Tree placement: it declines on its own while the pile
        /// holds a Tree, and otherwise asks which standing Tree comes back.
        /// </summary>
        public static EffectCapabilityReference ReclaimTreeV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-reclaim-tree"), 1);

        /// <summary>
        /// The Tree's own capabilities: the radius placement and its short-supply step, the two
        /// milestone 8.37 hero-token handlers bound to the Tree. Every face that places a Tree composes
        /// the pair itself (Mystic Saplings three times, March of Nature once), so no standalone
        /// reclamation exists that no printed face names.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceTreeInRadiusV1, Tree, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimTreeV1, Tree)
            };

        /// <summary>
        /// Every capability Wuk's composed faces name that the roster does not already own. Milestone
        /// 8.114a starts it with the Tree's two; later blocks add theirs.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Handlers().ToArray();
    }
}
