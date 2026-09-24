using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The three Blue disruption disks, measured from the live owned Turret.</summary>
    public static class TrinketsActiveEffectContent
    {
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved,
            ActiveEffectCancellationCondition.Explicit
        };

        public static EffectCapabilityReference JoltV1 => Reference("jolt");
        public static EffectCapabilityReference PulseV1 => Reference("pulse");
        public static EffectCapabilityReference GridV1 => Reference("grid");

        public static ActiveEffectDefinition Jolt => Create("jolt", 3);
        public static ActiveEffectDefinition Pulse => Create("pulse", 4);
        public static ActiveEffectDefinition Grid => Create("grid", 4);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new ActiveEffectCreationHandler(JoltV1, Jolt);
            yield return new ActiveEffectCreationHandler(PulseV1, Pulse);
            yield return new ActiveEffectCreationHandler(GridV1, Grid);
        }

        private static EffectCapabilityReference Reference(string face) =>
            new EffectCapabilityReference(
                new EffectCapabilityId("trinkets-disruptor-" + face + "-active"), 1);

        private static ActiveEffectDefinition Create(string face, int printedRadius) =>
            new ActiveEffectDefinition(
                "trinkets-disruptor-" + face + "-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "BeforeEnemyPrimaryActionDiscard",
                "trinkets-disruptor-" + face,
                new[] { TimingCheckpoint.BeforePrimaryAction },
                radius: DeclaredRadius.PrintedIcon(printedRadius),
                affectedUnitRelation: SourceTeamRelation.OpposingSource,
                triggeredEffect: new EffectDefinitionReference(
                    new EffectDefinitionId("trinkets-disruptor-" + face + "-trigger"),
                    TrinketsCardContent.ContentRevision),
                ownedObstacleRadiusAnchorKind: TrinketsComponentContent.TurretKind);
    }
}
