using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Mortimer the Awakener's physical component: the ZOMBIE token. Milestone 9.8a.
    ///
    /// <para><b>A bare p.19 token that stays.</b> "All tokens are obstacles", and no face gives a
    /// Zombie a property or an aura, so the definition declares none: an impassable obstacle, never
    /// removed on Mortimer's defeat, and any hero's p.13 Clear or a card's Remove can take it away. It
    /// does not leave at the end of the round: Awaken! prints "Zombie tokens are not removed at the
    /// end of round", and the user settled on 2026-09-29 (MO-Q4) that the sentence is a standing
    /// property of every Zombie, whichever card placed it - so the definition declares
    /// <see cref="TokenRemovalCondition.RetainedAtEndOfRound"/>, as Wuk's Tree does.</para>
    ///
    /// <para><b>Four of them</b> (MO-Q1). A placement that needs more than the pile holds meets
    /// p.18's short-supply step through <see cref="ReclaimZombieV1"/>, which every placing face
    /// composes before each placement it makes.</para>
    ///
    /// <para><b>Nothing here is registered</b> until his dealing block (9.8f).</para>
    /// </summary>
    public static class MortimerComponentContent
    {
        /// <summary>USER-SETTLED 2026-09-29 (MO-Q1): Mortimer's Zombie supply is four.</summary>
        public const int ZombieSupplyCount = 4;

        public static TokenDefinitionId ZombieId { get; } = new TokenDefinitionId("mortimer-zombie");

        public static TokenDefinitionReference ZombieReference { get; } =
            new TokenDefinitionReference(ZombieId, 1);

        public static TokenDefinition Zombie { get; } = new TokenDefinition(
            ZombieId,
            1,
            "Zombie",
            ZombieSupplyCount,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.RetainedAtEndOfRound);

        public static IEnumerable<TokenDefinition> Tokens() => new[] { Zombie };

        /// <summary>
        /// Awaken!'s one Place: a Zombie from the supply into an empty space in the card's printed
        /// Radius that is a spawn point or adjacent to Mortimer (MO-Q2).
        /// </summary>
        public static EffectCapabilityReference PlaceZombieV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mortimer-place-zombie"), 1);

        /// <summary>
        /// p.18's short-supply step before one Zombie placement: it declines on its own while the pile
        /// holds a Zombie, and otherwise asks which standing Zombie comes back.
        /// </summary>
        public static EffectCapabilityReference ReclaimZombieV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mortimer-reclaim-zombie"), 1);

        /// <summary>
        /// The Zombie's own capabilities: the placement and its short-supply step, the milestone 8.37
        /// hero-token handlers bound to the Zombie. Where a Zombie may go is the placing node's own
        /// declaration (<see cref="SyntheticTargetingCapabilities.SpawnPointOrAdjacentParameterName"/>),
        /// so the handler stays the family's.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceZombieV1, Zombie, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimZombieV1, Zombie)
            };
    }
}
