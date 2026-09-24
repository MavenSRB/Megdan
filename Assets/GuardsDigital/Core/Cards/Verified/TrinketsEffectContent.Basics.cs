using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class TrinketsEffectContent
    {
        public const string UnlimitedFirepowerRuleId = "trinkets-unlimited-firepower";
        public const string UnlimitedFirepowerRuleKind = "TrinketsUnlimitedFirepower";

        public static IReadOnlyList<RuleModification> UnlimitedFirepowerModifications { get; } =
            Array.AsReadOnly(new[]
            {
                RuleModification.ForValue(PrintedValueChannel.Range, 1),
                RuleModification.ForValue(PrintedValueChannel.Radius, 1)
            });

        public static EffectDefinition RapidRedeployment(CardDefinitionId card, string revision) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), revision,
                Id("root"), new[]
                {
                    Choice("root", "move-place", "defeat-arm"),
                    Sequence("move-place", "actor", "move", "place"),
                    ActorSpace("actor"),
                    Move("move", "actor"),
                    PlaceTurret("place"),
                    Sequence("defeat-arm", "minion", "defeat"),
                    AdjacentMinion("minion"),
                    new EffectNodeDefinition(Id("defeat"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.MinionParameter,
                                Id("minion"), SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        },
                        outputs: new[] { Output(
                            SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                            EffectValueKind.Minion) })
                });

        public static EffectDefinition SalvageParts(CardDefinitionId card, string revision) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), revision,
                Id("root"), new[]
                {
                    Choice("root", "place-arm", "move-arm", "retrieve-arm"),
                    Sequence("place-arm", "place"),
                    PlaceTurret("place"),
                    Sequence("move-arm", "remove-move", "actor", "move"),
                    RemoveTurret("remove-move"),
                    ActorSpace("actor"),
                    Move("move", "actor"),
                    Sequence("retrieve-arm", "remove-retrieve", "may-retrieve"),
                    RemoveTurret("remove-retrieve"),
                    new EffectNodeDefinition(Id("may-retrieve"), EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { Id("retrieve") },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(Id("retrieve"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole("owner", EffectRoleKind.Actor,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary("kind",
                                RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[] { Output("retrieved", EffectValueKind.CardInstance) })
                });

        private static EffectNodeDefinition Choice(string name, params string[] arms) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Choose,
                EffectFailurePolicy.Mandatory, childNodeIds: Array.ConvertAll(arms, Id),
                chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1);

        private static EffectNodeDefinition ActorSpace(string name) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[] { Output("space", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition Move(string name, string actorNode) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.UpTo, SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("mover", Id(actorNode), "space",
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                    EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                        EffectValue.Integer(0)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { Output("moved", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition PlaceTurret(string name) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                TrinketsComponentContent.PlaceTurretAdjacentV1,
                outputs: new[] { Output(SyntheticObstacleCapabilities.PlacedOutput,
                    EffectValueKind.Coordinate) });

        private static EffectNodeDefinition AdjacentMinion(string name) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                },
                outputs: new[] { Output(SyntheticRosterCapabilities.ChosenMinionOutput,
                    EffectValueKind.Minion) });
    }
}
