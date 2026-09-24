using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The two printed targeting patterns shared by Trinkets' five Red Attacks.</summary>
    public static partial class TrinketsEffectContent
    {
        public static EffectDefinition RedAttack(
            CardDefinitionId cardId, string revision, bool dualRange)
        {
            var root = new EffectNodeId("trinkets-red-root");
            var target = new EffectNodeId("trinkets-red-target");
            var bind = new EffectNodeId("trinkets-red-bind");
            var children = new[] { target, bind };
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource)
            };
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: children),
                new EffectNodeDefinition(
                    target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    dualRange
                        ? TrinketsComponentContent.DualRangeTurretUnitV1
                        : TrinketsComponentContent.AdjacentTurretUnitV1,
                    inputs: inputs,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion)
                    }),
                new EffectNodeDefinition(
                    bind, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticContinuationCapabilities.SetAttackTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticContinuationCapabilities.HeroParameter,
                            target, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticContinuationCapabilities.MinionParameter,
                            target, SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                            EffectValueKind.Minion)
                    })
            };

            return new EffectDefinition(
                new EffectDefinitionId($"{cardId.Value}-before-attack"),
                revision, root, nodes);
        }

        public static EffectDefinition RedRepeat(
            CardDefinitionId cardId, string revision, int maximumDifferentUnitRepeats)
        {
            if (maximumDifferentUnitRepeats <= 0)
                return null;
            var repeat = new EffectNodeId("trinkets-red-repeat");
            return new EffectDefinition(
                new EffectDefinitionId($"{cardId.Value}-after-attack"), revision, repeat,
                new[]
                {
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: maximumDifferentUnitRepeats,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }
    }
}
