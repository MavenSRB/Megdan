using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>PVE-D34. Original content composed from the existing unqualified Immune rule.</summary>
    public static class PveSkillContent
    {
        public const string ImmuneText = "This unit is immune this turn.";
        public static readonly EffectCapabilityReference ImmuneCapability =
            new EffectCapabilityReference(new EffectCapabilityId("pve-immune-this-turn"), 1);

        public static readonly ActiveEffectDefinition ImmuneDefinition = new ActiveEffectDefinition(
            "pve-immune-this-turn-effect", ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated, ActiveEffectCancellationCondition.CardsRetrieved },
            false, "PveImmuneThisTurn", "pve-immune-this-turn", Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForImmunity(new[] { RuleActivity.All }, RestrictedActorScope.Of(
                RestrictedActorRelation.HeroesOpposingSource, RestrictedActorRelation.HeroesAlliedWithSource)) },
            null, DeclaredRadius.None, SourceTeamRelation.None);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new[] { new ActiveEffectCreationHandler(ImmuneCapability, ImmuneDefinition) };

        public static EffectDefinition ImmuneEffect()
        {
            var node = new EffectNodeId("grant-immunity");
            return new EffectDefinition(new EffectDefinitionId("pve-immune-this-turn"), PveCardFactory.ContentRevision,
                node, new[] { new EffectNodeDefinition(node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    ImmuneCapability, outputs: new[] { new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero) }) });
        }
    }
}
