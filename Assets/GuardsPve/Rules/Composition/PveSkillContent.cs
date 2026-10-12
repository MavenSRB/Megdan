using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>PVE-D34/D36. Original content using existing immunity and timed value modifiers.</summary>
    public static class PveSkillContent
    {
        public const string ImmuneText = "This unit is immune this turn.";
        public const string RangeText = "Next turn, have +1 Range.";
        public static readonly EffectCapabilityReference RangeCapability =
            new EffectCapabilityReference(new EffectCapabilityId("pve-range-next-turn"), 1);
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

        public static readonly ActiveEffectDefinition RangeDefinition = new ActiveEffectDefinition(
            "pve-range-next-turn-effect", ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn, TimingCheckpoint.ActionStarted, TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated, ActiveEffectCancellationCondition.CardsRetrieved },
            true, "PveRangeNextTurn", "pve-range-next-turn", Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForValue(PrintedValueChannel.Range, 1) });

        public static IEnumerable<IEffectCapabilityHandler> Handlers(IEnumerable<PveSkillKind> skills)
        {
            foreach (var skill in skills.Distinct())
            {
                if (skill == PveSkillKind.None) continue;
                yield return new ActiveEffectCreationHandler(Capability(skill),
                    skill == PveSkillKind.ImmuneThisTurn ? ImmuneDefinition : RangeDefinition);
            }
        }

        private static EffectCapabilityReference Capability(PveSkillKind skill)
        {
            switch (skill)
            {
                case PveSkillKind.ImmuneThisTurn: return ImmuneCapability;
                case PveSkillKind.RangeNextTurn: return RangeCapability;
                default: throw new ArgumentOutOfRangeException(nameof(skill));
            }
        }

        public static string Text(PveSkillKind skill)
        {
            switch (skill)
            {
                case PveSkillKind.ImmuneThisTurn: return ImmuneText;
                case PveSkillKind.RangeNextTurn: return RangeText;
                default: throw new ArgumentOutOfRangeException(nameof(skill));
            }
        }

        public static EffectDefinition Effect(PveSkillKind skill)
        {
            var capability = Capability(skill);
            var node = new EffectNodeId(skill == PveSkillKind.ImmuneThisTurn ? "grant-immunity" : "grant-range");
            return new EffectDefinition(new EffectDefinitionId(skill == PveSkillKind.ImmuneThisTurn
                    ? "pve-immune-this-turn" : "pve-range-next-turn"), PveCardFactory.ContentRevision,
                node, new[] { new EffectNodeDefinition(node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    capability, outputs: new[] { new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero) }) });
        }
    }
}
