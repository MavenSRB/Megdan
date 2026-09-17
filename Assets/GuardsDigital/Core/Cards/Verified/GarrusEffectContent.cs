using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Source-backed Garrus clauses; no definitions are dealt or registered by this factory.</summary>
    public static class GarrusEffectContent
    {
        // Trace / Chase / Hunt Down: the discard condition gates only the Move, not hero targeting.
        public static EffectDefinition PursuitAttack(CardDefinitionId card, string revision, int distance) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-attack-text"), revision, Id("mode"), new[]
            {
                new EffectNodeDefinition(Id("mode"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { Id("advance"), Id("ordinary") },
                    chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1),
                Sequence("advance", "discard-gate", "hero-target", "hero-bind"),
                new EffectNodeDefinition(Id("discard-gate"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.OwnedCardPresentV1, childNodeIds: new[] { Id("advance-move") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("hero", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary("location", CardLocation.DiscardPile)
                    }, outputs: new[] { Output("present", EffectValueKind.Boolean) }),
                Sequence("advance-move", "actor-space", "move"),
                ActorSpace(),
                Move(distance, SyntheticDisplacementCapabilities.DirectMoveV3),
                Target("hero-target", RosterUnitKind.Hero),
                Bind("hero-bind", "hero-target", false),
                Sequence("ordinary", "unit-target", "unit-bind"),
                Target("unit-target", RosterUnitKind.Unit),
                Bind("unit-bind", "unit-target", true)
            });

        // Blunt Force / Send Flying: GA-Q1 admits choosing the current space, but not an implicit
        // hero. After the move the ordinary hero choice explicitly names which adjacent enemy to push.
        public static EffectDefinition MovePushAfterAttack(CardDefinitionId card, string revision, int distance) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-attack-text"), revision, Id("may-advance"), new[]
            {
                new EffectNodeDefinition(Id("may-advance"), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { Id("advance-push") }, chooser: EffectChooserRole.Actor),
                Sequence("advance-push", "actor-space", "move", "push-target", "push"),
                ActorSpace(),
                Move(distance, SyntheticDisplacementCapabilities.MoveAdjacentToEnemyHeroForPushV1),
                Target("push-target", RosterUnitKind.Hero, forPush: true),
                new EffectNodeDefinition(Id("push"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.PushTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("target", Id("push-target"),
                            SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                        EffectInputBinding.FromLiteral(SyntheticDisplacementCapabilities.LeastDistanceParameterName, EffectValue.Integer(3)),
                        EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(true)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName, PushEligiblePieceKind.Unit)
                    }, outputs: new[] { Output("moved", EffectValueKind.Integer) })
            });

        private static EffectNodeId Id(string value) => new EffectNodeId(value);
        private static EffectOutputBinding Output(string name, EffectValueKind kind) => new EffectOutputBinding(name, kind);
        private static EffectNodeDefinition Sequence(string id, params string[] children) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: System.Array.ConvertAll(children, Id));

        private static EffectNodeDefinition ActorSpace() =>
            new EffectNodeDefinition(Id("actor-space"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[] { Output("space", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition Move(int distance, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(Id("move"), EffectNodeKind.Capability, EffectFailurePolicy.UpTo, capability,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("mover", Id("actor-space"), "space", EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromClosedVocabulary(SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                }, outputs: new[] { Output("moved", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition Target(string id, RosterUnitKind kind, bool forPush = false) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.UnitKindParameter, kind),
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromClosedVocabulary("activity", forPush ? RuleActivity.ObjectPush : RuleActivity.Attack)
                }, outputs: forPush
                    ? new[] { Output(SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        Output(SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate) }
                    : kind == RosterUnitKind.Hero
                        ? new[] { Output(SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero) }
                        : new[] { Output(SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            Output(SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion) });

        private static EffectNodeDefinition Bind(string id, string target, bool admitsMinion) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: admitsMinion
                    ? new[]
                    {
                        EffectInputBinding.FromNodeOutput(SyntheticContinuationCapabilities.HeroParameter, Id(target),
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(SyntheticContinuationCapabilities.MinionParameter, Id(target),
                            SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                    }
                    : new[] { EffectInputBinding.FromNodeOutput(SyntheticContinuationCapabilities.HeroParameter,
                        Id(target), SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero) },
                outputs: admitsMinion
                    ? new[] { Output(SyntheticContinuationCapabilities.BoundHeroTargetOutput, EffectValueKind.Hero),
                        Output(SyntheticContinuationCapabilities.BoundMinionTargetOutput, EffectValueKind.Minion) }
                    : new[] { Output(SyntheticContinuationCapabilities.BoundHeroTargetOutput, EffectValueKind.Hero) });
    }
}
