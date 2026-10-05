using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// NebKher's three physical Illusions. This declaration remains outside the configured
    /// catalogs until the eighteen playable fronts are dealt at milestone 9.53.
    /// </summary>
    public static class NebKherComponentContent
    {
        public const int IllusionSupplyCount = 3;

        public static TokenDefinition Illusion { get; } = new TokenDefinition(
            new TokenDefinitionId("nebkher-illusion"), 1, "Illusion", IllusionSupplyCount,
            Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfRoundCleanup);

        public static EffectCapabilityReference PlaceIllusionV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("nebkher-place-illusion"), 1);

        public static EffectCapabilityReference ReclaimIllusionV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("nebkher-reclaim-illusion"), 1);

        public static EffectCapabilityReference IllusionsCountAsMinionsV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("nebkher-illusions-count-as-minions"), 1);

        public static ActiveEffectDefinition IllusionsCountAsMinions { get; } =
            new ActiveEffectDefinition(
                "nebkher-illusions-count-as-minions-effect",
                ActiveEffectAffectedScopeKind.TokensOfKindWhileControllerActs,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved,
                    ActiveEffectCancellationCondition.SourceCardStateChanged
                },
                false, "IllusionsCountAsMinions", "nebkher-illusions-count-as-minions",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForEffectiveMinionTypeOverlay(
                        GuardsDigital.Core.Setup.StandardMinionType.Melee)
                },
                affectedTokenKind: Illusion.Id);

        /// <summary>
        /// The printed Green placements use their Radius icon; an adjacent-only placement can
        /// compose the same token operation through its own adjacent-space selection.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimIllusionV1, Illusion),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceIllusionV1, Illusion, PrintedValueChannel.Radius),
                new ActiveEffectCreationHandler(
                    IllusionsCountAsMinionsV1, IllusionsCountAsMinions)
            };
    }
}
