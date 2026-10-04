using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string TimeCapsuleText =
            "You, and friendly heroes in radius, may retrieve all cards discarded this turn.";

        public static CardDefinition TimeCapsuleDefinition { get; } = GreenSkill(
            TimeCapsule, "Time Capsule", CardTier.II, 5, 2, 4,
            PrintedValueChannel.Initiative, new[] { BackToTheFuture, FutureProof },
            TimeCapsuleEffect(), TimeCapsuleText, PrintedValueChannel.Radius);

        private static EffectDefinition TimeCapsuleEffect()
        {
            var root = new EffectNodeId("time-capsule-root");
            var nodes = new List<EffectNodeDefinition>();
            AppendTimeCapsuleNodes(nodes, "time-capsule", root);
            return new EffectDefinition(new EffectDefinitionId(TimeCapsule.Value + "-skill"),
                ContentRevision, root, nodes);
        }

        /// <summary>The actor and each of at most two teammates decide independently on their own seat.</summary>
        private static void AppendTimeCapsuleNodes(
            List<EffectNodeDefinition> nodes, string prefix, EffectNodeId root)
        {
            var selfMay = new EffectNodeId(prefix + "-self-may");
            var selfRetrieve = new EffectNodeId(prefix + "-self-retrieve");
            var first = new EffectNodeId(prefix + "-first-friend");
            var firstMay = new EffectNodeId(prefix + "-first-may");
            var firstRetrieve = new EffectNodeId(prefix + "-first-retrieve");
            var second = new EffectNodeId(prefix + "-second-friend");
            var secondMay = new EffectNodeId(prefix + "-second-may");
            var secondRetrieve = new EffectNodeId(prefix + "-second-retrieve");

            nodes.Add(new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { selfMay, first, firstMay, second, secondMay }));
            nodes.Add(new EffectNodeDefinition(selfMay, EffectNodeKind.Optional,
                EffectFailurePolicy.May, childNodeIds: new[] { selfRetrieve },
                chooser: EffectChooserRole.Actor));
            nodes.Add(RetrieveCurrentTurnDiscards(selfRetrieve,
                EffectInputBinding.FromRole(EmmittTimeCapsuleContent.OwnerParameter,
                    EffectRoleKind.Actor, EffectValueKind.Hero)));
            nodes.Add(FriendlyWithRecentDiscard(first));
            nodes.Add(new EffectNodeDefinition(firstMay, EffectNodeKind.Optional,
                EffectFailurePolicy.May, childNodeIds: new[] { firstRetrieve },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(EffectNodeDefinition.ChooserParameterName,
                        first, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                }, chooser: EffectChooserRole.BoundHero));
            nodes.Add(RetrieveCurrentTurnDiscards(firstRetrieve,
                EffectInputBinding.FromNodeOutput(EmmittTimeCapsuleContent.OwnerParameter,
                    first, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                    EffectValueKind.Hero)));
            nodes.Add(FriendlyWithRecentDiscard(second, first));
            nodes.Add(new EffectNodeDefinition(secondMay, EffectNodeKind.Optional,
                EffectFailurePolicy.May, childNodeIds: new[] { secondRetrieve },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(EffectNodeDefinition.ChooserParameterName,
                        second, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                }, chooser: EffectChooserRole.BoundHero));
            nodes.Add(RetrieveCurrentTurnDiscards(secondRetrieve,
                EffectInputBinding.FromNodeOutput(EmmittTimeCapsuleContent.OwnerParameter,
                    second, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                    EffectValueKind.Hero)));
        }

        private static EffectNodeDefinition RetrieveCurrentTurnDiscards(
            EffectNodeId id, EffectInputBinding owner) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                EmmittTimeCapsuleContent.RetrieveCurrentTurnDiscardsV1,
                inputs: new[] { owner },
                outputs: new[]
                {
                    new EffectOutputBinding(EmmittTimeCapsuleContent.RetrievedCountOutput,
                        EffectValueKind.Integer)
                });

        private static EffectNodeDefinition FriendlyWithRecentDiscard(
            EffectNodeId id, EffectNodeId exclude = default)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.AlliedWithSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName,
                    EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.DiscardedCardThisTurnParameterName,
                    EffectValue.Boolean(true))
            };
            if (exclude.IsDefined)
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                    exclude, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                    EffectValueKind.Hero));
            return new EffectNodeDefinition(id, EffectNodeKind.Capability,
                EffectFailurePolicy.IfAble,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                });
        }
    }
}
