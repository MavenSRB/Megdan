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

        public static EffectCapabilityReference FutureProofImmunityV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-future-proof-immunity"), 1);

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

        public static ActiveEffectDefinition FutureProofImmunityDefinition { get; } =
            new ActiveEffectDefinition(
                "emmitt-future-proof-immunity-effect", ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations, false, "EmmittFutureProofImmunity", "emmitt-future-proof-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForImmunity(
                        new[] { RuleActivity.All },
                        RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
                },
                null, DeclaredRadius.PrintedIcon(4), SourceTeamRelation.AlliedWithSource);

        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } =
            Array.AsReadOnly(new[]
            {
                ReverseTimeDefinition, TemporalPunchDefinition, FutureProofImmunityDefinition
            });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ReverseTimeV1, ReverseTimeDefinition),
            new ActiveEffectCreationHandler(TemporalPunchV1, TemporalPunchDefinition),
            new ActiveEffectCreationHandler(FutureProofImmunityV1, FutureProofImmunityDefinition)
        };
    }
}
