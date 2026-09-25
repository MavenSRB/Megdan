using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Staged Gold/Red Attack text. Every glyph clause is a separate live predicate over
    /// the current turn's physical rune, so Rune Mastery's later extra activation can
    /// compose without changing these effect graphs.
    /// </summary>
    public static class SnorriAttackEffectContent
    {
        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        public static EffectDefinition BeforeAttack(
            CardDefinitionId card, string revision, bool moveBefore, bool sigils, bool blaster)
        {
            if (!moveBefore && !sigils && !blaster)
                return null;

            var children = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            if (moveBefore)
            {
                children.Add(Id("rune-four-move-gate"));
                AddRuneGate(nodes, "rune-four-move-gate", 4, "optional-move");
                AddOptionalMove(nodes, "optional-move", 1, optional: true);
            }

            children.Add(Id("target"));
            children.Add(Id("bind"));
            nodes.Add(TargetNode(sigils, blaster));
            nodes.Add(BindTargetNode());

            if (sigils)
            {
                children.Add(Id("rune-three-coin-gate"));
                AddRuneGate(nodes, "rune-three-coin-gate", 3, "hero-coin-gate");
                nodes.Add(new EffectNodeDefinition(
                    Id("hero-coin-gate"), EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { Id("gain-three-coins") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", Id("target"), SnorriAttackTargetContent.TargetHeroOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }));
                nodes.Add(new EffectNodeDefinition(
                    Id("gain-three-coins"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(3))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                Id("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children));
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-before-attack"),
                revision, Id("root"), nodes);
        }

        public static EffectDefinition AfterAttack(
            CardDefinitionId card, string revision, bool gold, bool retrieve,
            bool battleaxe, bool ranged)
        {
            var children = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            if (gold)
            {
                children.Add(Id("repeat-gate"));
                AddRuneGate(nodes, "repeat-gate", 4, "repeat");
                nodes.Add(RepeatNode(
                    RepeatTargetBinding.DifferentEnemyHero, optional: false));
            }
            if (battleaxe)
            {
                children.Add(Id("repeat-gate"));
                AddRuneGate(nodes, "repeat-gate", 2, "repeat");
                nodes.Add(RepeatNode(
                    RepeatTargetBinding.DifferentEnemyMinion, optional: true));
            }
            if (retrieve)
            {
                children.Add(Id("retrieve-gate"));
                AddRuneGate(nodes, "retrieve-gate", 3, "optional-retrieve");
                nodes.Add(new EffectNodeDefinition(
                    Id("optional-retrieve"), EffectNodeKind.Optional,
                    EffectFailurePolicy.May,
                    childNodeIds: new[] { Id("retrieve") },
                    chooser: EffectChooserRole.Actor));
                nodes.Add(new EffectNodeDefinition(
                    Id("retrieve"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromRole(
                            "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                    }));
            }
            if (ranged)
            {
                children.Add(Id("rune-four-move-gate"));
                AddRuneGate(nodes, "rune-four-move-gate", 4, "optional-move");
                AddOptionalMove(nodes, "optional-move", 2, optional: false);
                children.Add(Id("rune-two-discard-gate"));
                AddRuneGate(nodes, "rune-two-discard-gate", 2, "discard-if-able");
                AddAdjacentHeroDiscardOrDefeat(nodes);
            }

            if (children.Count == 0)
                return null;
            nodes.Insert(0, new EffectNodeDefinition(
                Id("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children));
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-after-attack"),
                revision, Id("root"), nodes);
        }

        private static EffectNodeDefinition TargetNode(bool sigils, bool blaster)
        {
            var capability = sigils
                ? SnorriAttackTargetContent.SigilsTargetV1
                : blaster
                    ? SnorriAttackTargetContent.RuneblasterTargetV1
                    : SyntheticRosterCapabilities.ChooseTargetV2;
            var inputs = sigils || blaster
                ? new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.OpposingSource)
                }
                : new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource)
                };
            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion)
            };
            if (sigils)
                outputs.Add(new EffectOutputBinding(
                    SnorriAttackTargetContent.TargetHeroOutput, EffectValueKind.Boolean));

            return new EffectNodeDefinition(
                Id("target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability, inputs: inputs, outputs: outputs);
        }

        private static EffectNodeDefinition BindTargetNode() =>
            new EffectNodeDefinition(
                Id("bind"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter,
                        Id("target"), SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter,
                        Id("target"), SyntheticTargetingCapabilities.ChosenMinionOutputName,
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
                });

        private static void AddRuneGate(
            ICollection<EffectNodeDefinition> nodes, string id, int rune, string child) =>
            nodes.Add(new EffectNodeDefinition(
                Id(id), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SnorriComponentContent.ActiveRuneV1,
                childNodeIds: new[] { Id(child) },
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral("rune", EffectValue.Integer(rune))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("active", EffectValueKind.Boolean)
                }));

        private static EffectNodeDefinition RepeatNode(
            RepeatTargetBinding binding, bool optional) =>
            new EffectNodeDefinition(
                Id("repeat"), EffectNodeKind.RepeatAction,
                optional ? EffectFailurePolicy.May : EffectFailurePolicy.IfAble,
                chooser: optional ? EffectChooserRole.Actor : EffectChooserRole.None,
                maximumRepeats: 1,
                repeatTargetBinding: binding);

        private static void AddOptionalMove(
            ICollection<EffectNodeDefinition> nodes, string id, int distance,
            bool optional)
        {
            if (optional)
                nodes.Add(new EffectNodeDefinition(
                    Id(id), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { Id(id + "-steps") },
                    chooser: EffectChooserRole.Actor));
            nodes.Add(new EffectNodeDefinition(
                optional ? Id(id + "-steps") : Id(id),
                EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { Id(id + "-actor-space"), Id(id + "-direct") }));
            nodes.Add(new EffectNodeDefinition(
                Id(id + "-actor-space"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding("space", EffectValueKind.Coordinate)
                }));
            nodes.Add(new EffectNodeDefinition(
                Id(id + "-direct"), EffectNodeKind.Capability,
                EffectFailurePolicy.UpTo, SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", Id(id + "-actor-space"), "space",
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral(
                        "distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                }));
        }

        private static void AddAdjacentHeroDiscardOrDefeat(
            ICollection<EffectNodeDefinition> nodes)
        {
            nodes.Add(new EffectNodeDefinition(
                Id("discard-if-able"), EffectNodeKind.Optional,
                EffectFailurePolicy.IfAble,
                childNodeIds: new[] { Id("discard-chain") }));
            nodes.Add(new EffectNodeDefinition(
                Id("discard-chain"), EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory,
                childNodeIds: new[]
                {
                    Id("adjacent-hero"), Id("discard-card"), Id("unable-gate")
                }));
            nodes.Add(new EffectNodeDefinition(
                Id("adjacent-hero"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter,
                        RosterQueryOrigin.EnclosingTargetBeforeConsequences),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter,
                        EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.ExcludeTargetParameter,
                        EffectValue.Boolean(true))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                }));
            nodes.Add(new EffectNodeDefinition(
                Id("discard-card"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticCardStateCapabilities.DiscardChosenCardV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter,
                        Id("adjacent-hero"), SyntheticRosterCapabilities.ChosenHeroOutput,
                        EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticCardStateCapabilities.DiscardedOutput,
                        EffectValueKind.CardInstance),
                    new EffectOutputBinding(
                        SyntheticCardStateCapabilities.UnableToDiscardOutput,
                        EffectValueKind.Boolean)
                }));
            nodes.Add(new EffectNodeDefinition(
                Id("unable-gate"), EffectNodeKind.Conditional,
                EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { Id("defeat") },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag", Id("discard-card"),
                        SyntheticCardStateCapabilities.UnableToDiscardOutput,
                        EffectValueKind.Boolean)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("value", EffectValueKind.Boolean)
                }));
            nodes.Add(new EffectNodeDefinition(
                Id("defeat"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticConsequenceCapabilities.DefeatHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "hero", Id("adjacent-hero"),
                        SyntheticRosterCapabilities.ChosenHeroOutput,
                        EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                        EffectValueKind.Hero)
                }));
        }
    }
}
