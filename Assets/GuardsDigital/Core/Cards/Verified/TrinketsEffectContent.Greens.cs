using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Trinkets' five Green primary Skills, composed but not registered until dealing.</summary>
    public static partial class TrinketsEffectContent
    {
        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        private static EffectOutputBinding Output(string name, EffectValueKind kind) =>
            new EffectOutputBinding(name, kind);

        private static EffectDefinition Green(
            CardDefinitionId card, string revision, IEnumerable<EffectNodeDefinition> nodes) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), revision,
                Id("root"), nodes);

        private static EffectNodeDefinition Sequence(string name, params string[] children) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: Array.ConvertAll(children, Id));

        private static EffectNodeDefinition TurretSpace(string name) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, TrinketsComponentContent.TurretSpaceV1,
                outputs: new[] { Output(SyntheticObstacleCapabilities.SpaceOutput,
                    EffectValueKind.Coordinate) });

        private static EffectNodeDefinition RemoveTurret(string name) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, TrinketsComponentContent.RemoveTurretV1,
                outputs: new[] { Output(SyntheticObstacleCapabilities.RemovedOutput,
                    EffectValueKind.Coordinate) });

        private static EffectNodeDefinition SelectTurretObject(string name,
            string swapNode) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                TrinketsComponentContent.TurretRadiusObjectV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName,
                        EffectValue.Boolean(false)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                        PrintedIconObjectKind.UnitsAndTokens),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(Id(swapNode)))
                },
                outputs: new[] { Output("chosen", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition Swap(string name, string selectedNode) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapWithActorV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("subject", Id(selectedNode),
                        "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName,
                        SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    Output("actorAt", EffectValueKind.Coordinate),
                    Output("subjectAt", EffectValueKind.Coordinate)
                });

        public static EffectDefinition PrototypeSkill(
            CardDefinitionId card, string revision, bool removeTurret)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", "radius", "inside", "if-inside"),
                new EffectNodeDefinition(Id("radius"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticValueCapabilities.CardRadiusV1,
                    outputs: new[] { Output("radius", EffectValueKind.Integer) }),
                new EffectNodeDefinition(Id("inside"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    TrinketsComponentContent.ActorInTurretRadiusV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("radius", Id("radius"),
                            "radius", EffectValueKind.Integer)
                    },
                    outputs: new[] { Output("inside", EffectValueKind.Boolean) }),
                new EffectNodeDefinition(Id("if-inside"), EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { Id("action") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("flag", Id("inside"),
                            "inside", EffectValueKind.Boolean)
                    }, outputs: new[] { Output("value", EffectValueKind.Boolean) }),
                Sequence("action", removeTurret
                    ? new[] { "turret", "target", "swap", "remove" }
                    : new[] { "turret", "target", "swap" }),
                TurretSpace("turret"),
                SelectTurretObject("target", "swap"),
                Swap("swap", "target")
            };
            if (removeTurret) nodes.Add(RemoveTurret("remove"));
            return Green(card, revision, nodes);
        }

        public static EffectDefinition PerfectedSkill(CardDefinitionId card, string revision) =>
            Green(card, revision, new[]
            {
                Sequence("root", "radius", "inside", "if-inside"),
                new EffectNodeDefinition(Id("radius"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticValueCapabilities.CardRadiusV1,
                    outputs: new[] { Output("radius", EffectValueKind.Integer) }),
                new EffectNodeDefinition(Id("inside"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    TrinketsComponentContent.ActorInTurretRadiusV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("radius", Id("radius"),
                            "radius", EffectValueKind.Integer)
                    }, outputs: new[] { Output("inside", EffectValueKind.Boolean) }),
                new EffectNodeDefinition(Id("if-inside"), EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { Id("mode") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("flag", Id("inside"),
                            "inside", EffectValueKind.Boolean)
                    }, outputs: new[] { Output("value", EffectValueKind.Boolean) }),
                new EffectNodeDefinition(Id("mode"), EffectNodeKind.Choose,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { Id("swap-arm"), Id("place-arm") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1, maximumSelections: 1),
                Sequence("swap-arm", "turret-swap", "target", "swap"),
                TurretSpace("turret-swap"),
                SelectTurretObject("target", "swap"),
                Swap("swap", "target"),
                Sequence("place-arm", "actor", "turret-place", "place"),
                new EffectNodeDefinition(Id("actor"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[] { Output("space", EffectValueKind.Coordinate) }),
                TurretSpace("turret-place"),
                new EffectNodeDefinition(Id("place"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    TrinketsComponentContent.PlaceSelfInTurretRadiusV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("subject", Id("actor"),
                            "space", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.GamePiece),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None),
                    }, outputs: new[] { Output("placed", EffectValueKind.Coordinate) })
            });

        private static EffectNodeDefinition SelectEnemyHero(
            string name, string excludedNode = null)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false)),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
            };
            if (excludedNode != null)
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                    Id(excludedNode), SyntheticTargetingCapabilities.ChosenHeroOutputName,
                    EffectValueKind.Hero));
            return new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                TrinketsComponentContent.TurretRadiusEnemyHeroV1,
                inputs: inputs,
                outputs: new[]
                {
                    Output(SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                });
        }

        public static EffectDefinition DetonationSkill(
            CardDefinitionId card, string revision, bool defeatIfUnable)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", "turret", "optional-first", "remove"),
                TurretSpace("turret"),
                new EffectNodeDefinition(Id("optional-first"), EffectNodeKind.Optional,
                    EffectFailurePolicy.UpTo, childNodeIds: new[] { Id("first") },
                    chooser: EffectChooserRole.Actor),
                Sequence("first", defeatIfUnable
                    ? new[] { "first-hero", "first-discard", "first-defeat-if-needed",
                        "optional-second" }
                    : new[] { "first-hero", "first-discard", "optional-second" }),
                SelectEnemyHero("first-hero"),
                Discard("first-discard", "first-hero", defeatIfUnable),
                new EffectNodeDefinition(Id("optional-second"), EffectNodeKind.Optional,
                    EffectFailurePolicy.UpTo, childNodeIds: new[] { Id("second") },
                    chooser: EffectChooserRole.Actor),
                Sequence("second", defeatIfUnable
                    ? new[] { "second-hero", "second-discard", "second-defeat-if-needed" }
                    : new[] { "second-hero", "second-discard" }),
                SelectEnemyHero("second-hero", "first-hero"),
                Discard("second-discard", "second-hero", defeatIfUnable),
                RemoveTurret("remove")
            };
            if (defeatIfUnable)
            {
                nodes.Add(DefeatIfNeeded("first-defeat-if-needed", "first-discard",
                    "first-defeat"));
                nodes.Add(Defeat("first-defeat", "first-hero"));
                nodes.Add(DefeatIfNeeded("second-defeat-if-needed", "second-discard",
                    "second-defeat"));
                nodes.Add(Defeat("second-defeat", "second-hero"));
            }
            return Green(card, revision, nodes);
        }

        private static EffectNodeDefinition Discard(
            string name, string heroNode, bool defeatIfUnable) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                defeatIfUnable ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                defeatIfUnable
                    ? SyntheticCardStateCapabilities.DiscardChosenCardV2
                    : SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter,
                        Id(heroNode), SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                },
                outputs: defeatIfUnable
                    ? new[]
                    {
                        Output(SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        Output(SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    }
                    : new[]
                    {
                        Output(SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    });

        private static EffectNodeDefinition DefeatIfNeeded(
            string name, string discardNode, string defeatNode) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Conditional,
                EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { Id(defeatNode) },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("flag", Id(discardNode),
                        SyntheticCardStateCapabilities.UnableToDiscardOutput,
                        EffectValueKind.Boolean)
                }, outputs: new[] { Output("value", EffectValueKind.Boolean) });

        private static EffectNodeDefinition Defeat(string name, string heroNode) =>
            new EffectNodeDefinition(Id(name), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, SyntheticConsequenceCapabilities.DefeatHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticConsequenceCapabilities.HeroParameter,
                        Id(heroNode), SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                }, outputs: new[]
                {
                    Output(SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                        EffectValueKind.Hero)
                });
    }
}
