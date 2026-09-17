using System;
using System.Collections.Generic;
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
        public static EffectDefinition ChillingHowl(CardDefinitionId card, string revision) =>
            Skill(card, revision, new[]
            {
                Sequence("root", "may-discard", "aura"),
                May("may-discard", "discard"),
                new EffectNodeDefinition(Id("discard"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1,
                    outputs: new[] { Output("discarded", EffectValueKind.CardInstance) }),
                new EffectNodeDefinition(Id("aura"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    GarrusActiveEffectContent.ChillingHowlV1,
                    outputs: new[] { Output(ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero) })
            });

        public static EffectDefinition MoveUnit(CardDefinitionId card, string revision, bool friendly, int repeats)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", repeats == 0 ? new[] { "target", "unit-move" } : new[] { "target", "unit-move", "repeat" }),
                new EffectNodeDefinition(Id("target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeUnitTargetV1, inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("relation", friendly ? SourceTeamRelation.AlliedWithSource : SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(Id("unit-move")))
                    }, outputs: new[]
                    {
                        Output(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                        Output(SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion),
                        Output(SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(Id("unit-move"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.DirectMoveV3, inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("mover", Id("target"), SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            friendly ? DirectMoveDestinationPolicy.TowardActor : DirectMoveDestinationPolicy.AwayFromActor)
                    }, outputs: new[] { Output("moved", EffectValueKind.Coordinate) })
            };
            if (repeats > 0) nodes.Add(new EffectNodeDefinition(Id("repeat"), EffectNodeKind.RepeatAction,
                EffectFailurePolicy.May, chooser: EffectChooserRole.Actor, maximumRepeats: repeats));
            return Skill(card, revision, nodes);
        }

        public static EffectDefinition RetrieveInPresence(CardDefinitionId card, string revision, int count)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", "radius", "presence"),
                new EffectNodeDefinition(Id("radius"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticValueCapabilities.CardRadiusV1, outputs: new[] { Output("radius", EffectValueKind.Integer) }),
                new EffectNodeDefinition(Id("presence"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.PresenceCountV1, childNodeIds: new[] { Id("may-retrieve") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromNodeOutput(SyntheticRosterCapabilities.DistanceParameter, Id("radius"), "radius", EffectValueKind.Integer),
                        EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromLiteral("excludeActor", EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral("excludeTarget", EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(SyntheticRosterCapabilities.MinimumPresentParameter, EffectValue.Integer(2))
                    }, outputs: new[] { Output("count", EffectValueKind.Integer), Output("present", EffectValueKind.Boolean) }),
                May("may-retrieve", "retrievals"),
                Sequence("retrievals", count == 1 ? new[] { "retrieve-1" } : new[] { "retrieve-1", "may-retrieve-2" }),
                Retrieve("retrieve-1")
            };
            if (count == 2) { nodes.Add(May("may-retrieve-2", "retrieve-2")); nodes.Add(Retrieve("retrieve-2")); }
            return Skill(card, revision, nodes);
        }

        public static EffectDefinition Pilum(CardDefinitionId card, string revision, bool heavy)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", heavy ? new[] { "target", "discard", "if-unable", "may-move" }
                    : new[] { "target", "discard", "may-move" }),
                new EffectNodeDefinition(Id("target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1, inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("relation", SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName, EffectValue.Boolean(false))
                    }, outputs: new[] { Output("chosenHero", EffectValueKind.Hero) }),
                new EffectNodeDefinition(Id("discard"), EffectNodeKind.Capability,
                    heavy ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    heavy ? SyntheticCardStateCapabilities.DiscardChosenCardV2 : SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[] { EffectInputBinding.FromNodeOutput("hero", Id("target"), "chosenHero", EffectValueKind.Hero) },
                    outputs: heavy ? new[] { Output(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance),
                        Output(SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean) }
                        : new[] { Output(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance) }),
                May("may-move", "self-move"),
                Sequence("self-move", "actor-space", "move"),
                ActorSpace(),
                new EffectNodeDefinition(Id("move"), EffectNodeKind.Capability,
                    heavy ? EffectFailurePolicy.UpTo : EffectFailurePolicy.Mandatory, SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("mover", Id("actor-space"), "space", EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(heavy ? 2 : 1)),
                        EffectInputBinding.FromClosedVocabulary(SyntheticDisplacementCapabilities.DestinationPolicyParameterName, DirectMoveDestinationPolicy.Unrestricted)
                    }, outputs: new[] { Output("moved", EffectValueKind.Coordinate) })
            };
            if (heavy)
            {
                nodes.Add(new EffectNodeDefinition(Id("if-unable"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1, childNodeIds: new[] { Id("defeat") },
                    inputs: new[] { EffectInputBinding.FromNodeOutput("flag", Id("discard"),
                        SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean) },
                    outputs: new[] { Output("value", EffectValueKind.Boolean) }));
                nodes.Add(new EffectNodeDefinition(Id("defeat"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[] { EffectInputBinding.FromNodeOutput("hero", Id("target"), "chosenHero", EffectValueKind.Hero) },
                    outputs: new[] { Output(SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero) }));
            }
            return Skill(card, revision, nodes);
        }

        private static EffectDefinition Skill(CardDefinitionId card, string revision, IEnumerable<EffectNodeDefinition> nodes) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), revision, Id("root"), nodes);
        private static EffectNodeDefinition May(string id, string child) => new EffectNodeDefinition(Id(id), EffectNodeKind.Optional,
            EffectFailurePolicy.May, childNodeIds: new[] { Id(child) }, chooser: EffectChooserRole.Actor);
        private static EffectNodeDefinition Retrieve(string id) => new EffectNodeDefinition(Id(id), EffectNodeKind.Capability,
            EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.RetrieveCardV1,
            inputs: new[]
            {
                EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
            }, outputs: new[] { Output("retrieved", EffectValueKind.CardInstance) });

        // Trace / Chase / Hunt Down: the discard condition gates only the Move, not hero targeting.
        public static EffectDefinition PursuitAttack(CardDefinitionId card, string revision, int distance) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-attack-text"), revision, Id("mode"), new[]
            {
                new EffectNodeDefinition(Id("mode"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { Id("advance"), Id("ordinary") },
                    chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1),
                new EffectNodeDefinition(Id("advance"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.OwnedCardPresentV1, childNodeIds: new[] { Id("advance-move"), Id("without-move") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("hero", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary("location", CardLocation.DiscardPile)
                    }, outputs: new[] { Output("present", EffectValueKind.Boolean) }),
                // Each branch carries its binding: the offer probe can then judge the target
                // from the Move's prospective destinations, not from the actor's old space.
                Sequence("advance-move", "actor-space", "move", "hero-target", "hero-bind"),
                ActorSpace(),
                Move(distance, SyntheticDisplacementCapabilities.DirectMoveV3),
                Target("hero-target", RosterUnitKind.Hero),
                Bind("hero-bind", "hero-target", false),
                Sequence("without-move", "stationary-target", "stationary-bind"),
                Target("stationary-target", RosterUnitKind.Hero),
                Bind("stationary-bind", "stationary-target", false),
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

        public static EffectDefinition ResolvedDiscardPrimary(CardDefinitionId card, string revision) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-resolved-discard-primary"), revision,
                Id("may-perform-primary"), new[]
                {
                    new EffectNodeDefinition(Id("may-perform-primary"), EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { Id("perform-discarded-primary") },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(Id("perform-discarded-primary"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        capability: SyntheticNamedBorrowCapabilities.BorrowNamedDiscardedPrimaryV1,
                        inputs: new[] { EffectInputBinding.FromRole("card", EffectRoleKind.TriggeredCard,
                            EffectValueKind.CardInstance) },
                        outputs: new[] { Output(SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                            EffectValueKind.CardInstance) })
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
