using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's Decoy and his two mountain guides - milestone 8.69c - as the contracts they
    /// need rather than as the sentences they compose.
    ///
    /// <para><b>This block declares NO capability of its own, and that is its headline result.</b>
    /// Decoy moves an enemy unit one space and the guides move a friendly unit up to N; every piece of
    /// that is already a live contract - <c>synthetic-radius-minion-target@1</c>,
    /// <c>synthetic-radius-hero-target@1</c> with milestone 8.69a's narrowing Boolean,
    /// <c>synthetic-radius-unit-target@1</c>, <c>choose-target@2</c> and
    /// <c>synthetic-direct-move@3</c>, over 8.69a's own presence predicate and its conditional gate.
    /// What the three faces needed that this build did not have is ONE clause - "on different targets"
    /// and "a different friendly unit" - and that is a NARROWING of who may be named, so it is two
    /// optional parameters on the printed-icon selections (<c>excludeHero</c> and
    /// <c>excludeMinion</c>) rather than a contract of his. See
    /// <see cref="GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities.ExcludedHeroParameterName"/>.</para>
    ///
    /// <para>The empty lists below are therefore the honest answer rather than a placeholder: there is
    /// nothing of this block's for milestone 7.9's gate to withhold, and <c>BrynnDecoyAndGuidesTests</c>
    /// asserts that from both sides so a later block cannot quietly add one here without the fixture
    /// noticing.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// Nothing, because this block declares no capability at all - see the type's own remarks.
        /// The member exists beside <see cref="ObstacleDeferredHandlers"/> so a "registered or
        /// deferred" sweep keeps its shape and so the fixture asking the question compiles.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> GuidesDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>Nothing, for the reason 8.69a's own registered list is empty: 8.69g deals him.</summary>
        public static IEnumerable<IEffectCapabilityHandler> GuidesRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}
