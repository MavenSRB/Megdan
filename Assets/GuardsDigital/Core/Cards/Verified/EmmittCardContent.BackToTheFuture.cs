using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string BackToTheFutureText =
            "Choose one — Place a unit in range into the space where that unit was at the start " +
            "of this turn. Or: Move an enemy hero in range, who remained in the same space since " +
            "the last turn, 2 spaces in a straight line.";

        public static CardDefinition BackToTheFutureDefinition { get; } = GreenSkill(
            BackToTheFuture, "Back to the Future", CardTier.III, 5, 3, 4,
            PrintedValueChannel.Attack, Array.Empty<CardDefinitionId>(),
            BackToTheFutureEffect(), BackToTheFutureText);

        private static EffectDefinition BackToTheFutureEffect()
        {
            var modal = new EffectNodeId("back-to-the-future-mode");
            var placeArm = new EffectNodeId("back-to-the-future-place-arm");
            var unit = new EffectNodeId("back-to-the-future-unit");
            var place = new EffectNodeId("back-to-the-future-place");
            var movement = MoveStationaryEnemy(BackToTheFuture);
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(modal, EffectNodeKind.Choose,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { placeArm, movement.RootNodeId },
                    chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1),
                new EffectNodeDefinition(placeArm, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { unit, place }),
                new EffectNodeDefinition(unit, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeUnitTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AnyTeam),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(place))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(place, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    EmmittBackToFutureContent.PlaceAtTurnStartV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            EmmittBackToFutureContent.HeroParameter, unit,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            EmmittBackToFutureContent.MinionParameter, unit,
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion),
                        EffectInputBinding.FromNodeOutput(
                            EmmittBackToFutureContent.SelectedSpaceParameter, unit,
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(EmmittBackToFutureContent.PlacedOutput,
                            EffectValueKind.Coordinate)
                    })
            };
            nodes.AddRange(movement.Nodes);
            return new EffectDefinition(new EffectDefinitionId(BackToTheFuture.Value + "-skill"),
                ContentRevision, modal, nodes);
        }
    }
}
