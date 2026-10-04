using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Emmitt's typed attack-local and next-turn rule declarations.</summary>
    public static class EmmittActiveEffectContent
    {
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityReference ReverseTimeV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-reverse-time"), 1);

        public static EffectCapabilityReference TemporalPunchV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-temporal-punch"), 1);

        public static ActiveEffectDefinition ReverseTimeDefinition { get; } =
            new ActiveEffectDefinition(
                "emmitt-reverse-time-order", ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurn, TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations, true, "EmmittReverseTime", "emmitt-reverse-time-order",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForAscendingInitiativeOrder() });

        public static ActiveEffectDefinition TemporalPunchDefinition { get; } =
            new ActiveEffectDefinition(
                "emmitt-temporal-punch-defense", ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.BeforeAttack,
                TimingCheckpoint.BeforeAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations, false, "EmmittTemporalPunch", "emmitt-temporal-punch-defense",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForDefendingCardUsesInitiative() });

        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } =
            Array.AsReadOnly(new[] { ReverseTimeDefinition, TemporalPunchDefinition });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ReverseTimeV1, ReverseTimeDefinition),
            new ActiveEffectCreationHandler(TemporalPunchV1, TemporalPunchDefinition)
        };
    }
}
