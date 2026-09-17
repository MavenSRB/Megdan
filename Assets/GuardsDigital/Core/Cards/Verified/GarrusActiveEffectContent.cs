using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    public static class GarrusActiveEffectContent
    {
        public static EffectCapabilityReference ChillingHowlV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("garrus-chilling-howl-aura"), 1);
        public static ActiveEffectDefinition ChillingHowlDefinition { get; } = new ActiveEffectDefinition(
            "garrus-chilling-howl-effect", ActiveEffectAffectedScopeKind.HeroesInRadius,
            ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd, new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated,
                ActiveEffectCancellationCondition.CardsRetrieved, ActiveEffectCancellationCondition.SourceCardStateChanged },
            false, "ChillingHowl", "garrus-chilling-howl", Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForRestriction(RuleActivity.FastTravel), RuleModification.ForActivityLimit(RuleActivity.Movement, 2) },
            null, DeclaredRadius.PrintedIcon(3), SourceTeamRelation.OpposingSource);
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[] { new ActiveEffectCreationHandler(ChillingHowlV1, ChillingHowlDefinition) };
    }
}
