using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string TimeWarpText =
            "Choose one — Swap with an enemy hero in range who has already resolved a card this turn. " +
            "Or: An enemy hero in range swaps their unresolved card with their resolved card of their choice.";

        public static CardDefinition TimeWarpDefinition { get; } = BlueSkill(
            TimeWarp, "Time Warp", CardTier.III, 9, 4, 4, PrintedValueChannel.Radius,
            Array.Empty<CardDefinitionId>(), TimeWarpEffect(), TimeWarpText);

        private static EffectDefinition TimeWarpEffect()
        {
            var mode = new EffectNodeId("time-warp-mode");
            var swapArm = new EffectNodeId("time-warp-swap-arm");
            var swapTarget = new EffectNodeId("time-warp-swap-target");
            var swap = new EffectNodeId("time-warp-swap");
            var cardArm = new EffectNodeId("time-warp-card-arm");
            var cardTarget = new EffectNodeId("time-warp-card-target");
            var cardSwap = new EffectNodeId("time-warp-card-swap");
            return new EffectDefinition(new EffectDefinitionId(TimeWarp.Value + "-skill"),
                ContentRevision, mode, new[]
                {
                    new EffectNodeDefinition(mode, EffectNodeKind.Choose,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { swapArm, cardArm },
                        chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1),
                    new EffectNodeDefinition(swapArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { swapTarget, swap }),
                    new EffectNodeDefinition(swapTarget, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ResolvedCardThisTurnParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticExchangeCapabilities.SwapWithActorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("subject", swapTarget,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(cardArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { cardTarget, cardSwap }),
                    new EffectNodeDefinition(cardTarget, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(cardSwap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(cardSwap, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, EmmittTimeWarpContent.SwapWithResolvedV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(EmmittTimeWarpContent.OwnerParameter,
                                cardTarget, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(EmmittTimeWarpContent.SwappedOutOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(EmmittTimeWarpContent.SwappedInOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }
    }
}
