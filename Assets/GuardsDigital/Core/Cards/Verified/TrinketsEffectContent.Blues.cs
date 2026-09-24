using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class TrinketsEffectContent
    {
        public static EffectDefinition DisruptorSkill(
            CardDefinitionId cardId, string revision,
            EffectCapabilityReference createEffect)
        {
            var root = new EffectNodeId("disruptor-create");
            return new EffectDefinition(
                new EffectDefinitionId(cardId.Value + "-skill"), revision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        createEffect,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        public static EffectDefinition DisruptorTrigger(
            CardDefinitionId cardId, string revision, bool defeatIfUnable)
        {
            var root = new EffectNodeId("disruptor-trigger-root");
            var discard = new EffectNodeId("disruptor-discard");
            var escalation = new EffectNodeId("disruptor-escalation");
            var defeat = new EffectNodeId("disruptor-defeat");
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: defeatIfUnable
                        ? new[] { discard, escalation }
                        : new[] { discard }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability,
                    defeatIfUnable ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    defeatIfUnable
                        ? SyntheticCardStateCapabilities.DiscardChosenCardV2
                        : SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticCardStateCapabilities.HeroParameter,
                            EffectRoleKind.OpposingHero, EffectValueKind.Hero)
                    },
                    outputs: defeatIfUnable
                        ? new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        }
                        : new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
            };
            if (defeatIfUnable)
            {
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard,
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticConsequenceCapabilities.HeroParameter,
                            EffectRoleKind.OpposingHero, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    }));
            }
            return new EffectDefinition(
                new EffectDefinitionId(cardId.Value + "-trigger"), revision, root, nodes);
        }

        /// <summary>
        /// At least one Barrier is adjacent to the live Turret; each further copy is the printed
        /// "up to" choice. Each pair reclaims if needed before placing, in p.18's supply order.
        /// </summary>
        public static EffectDefinition BarrierSkill(
            CardDefinitionId cardId, string revision, int maximumPlacements)
        {
            if (maximumPlacements < 1 || maximumPlacements > 3)
                throw new ArgumentOutOfRangeException(nameof(maximumPlacements));
            var root = new EffectNodeId("trinkets-barrier-root");
            var placements = new EffectNodeId("trinkets-barrier-placements");
            var children = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            for (var index = 0; index < maximumPlacements; index++)
            {
                var pair = new EffectNodeId($"trinkets-barrier-pair-{index}");
                var reclaim = new EffectNodeId($"trinkets-barrier-reclaim-{index}");
                var place = new EffectNodeId($"trinkets-barrier-place-{index}");
                nodes.Add(new EffectNodeDefinition(
                    pair, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { reclaim, place }));
                nodes.Add(new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    TrinketsComponentContent.ReclaimBarrierV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    index == 0
                        ? TrinketsComponentContent.PlaceFirstBarrierV1
                        : TrinketsComponentContent.PlaceBarrierV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }));
                if (index == 0)
                {
                    children.Add(pair);
                }
                else
                {
                    var optional = new EffectNodeId($"trinkets-barrier-optional-{index}");
                    nodes.Add(new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                        childNodeIds: new[] { pair }, chooser: EffectChooserRole.Actor));
                    children.Add(optional);
                }
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                childNodeIds: new[] { placements }, chooser: EffectChooserRole.Actor));
            nodes.Insert(1, new EffectNodeDefinition(
                placements, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children));
            return new EffectDefinition(
                new EffectDefinitionId($"{cardId.Value}-skill"), revision, root, nodes);
        }
    }
}
