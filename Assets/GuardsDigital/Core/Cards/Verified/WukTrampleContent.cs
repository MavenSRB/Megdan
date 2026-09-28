using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114b1. Trample and Angry Stampede: "If you move in a straight line: You may ignore
    /// obstacles; each enemy hero you moved through discards a card, or is defeated; defeat up to one
    /// (two) minion(s) you moved through." - reading (A) of WU-Q5 (user, 2026-09-28): the Movement
    /// action itself tramples.
    ///
    /// <para><b>Two bodies either side of the walk.</b> "You may ignore obstacles" changes the walk,
    /// so it is p.11's default moment before it: a This Turn effect granting Wuk
    /// <see cref="RulePermissionKind.MoveThroughObstaclesInAStraightLine"/> for Movement, which
    /// <see cref="StraightLineCrossingQuery"/> reads on the walk itself. "Moved through" exists only
    /// once the walk is over, so the rest is the action's second body, after it, reading the board's
    /// persisted record of the completed walk (<see cref="CompletedMovementWalk"/>). A walk that bent
    /// performs none of the clause: the permission offered it nothing, and the after body finds no
    /// straight line.</para>
    ///
    /// <para><b>Who is affected.</b> p.19 makes a unit this action affects one it targets, so an enemy
    /// hero or minion immune to Wuk's Movement is not affected (the discard is not asked of an immune
    /// hero; an immune minion is not offered). "discards a card, or is defeated" is ledger question
    /// 13's one mandatory step with two ways to complete - Brogan's Counterattack composition. "Defeat
    /// up to one (two) minion(s)" is any minion, Wuk's own included (WU-Q4), chosen by Wuk - Dodger's
    /// "up to N" selection with one <c>if able</c> consequence per slot.</para>
    /// </summary>
    public static class WukTrampleContent
    {
        public static EffectCapabilityReference TrampleV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-trample"), 1);

        public static EffectCapabilityReference TrampledEnemyHeroesV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-trampled-enemy-heroes"), 1);

        public static EffectCapabilityReference ChooseTrampledMinionsV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-choose-trampled-minions"), 1);

        public const int MostHeroesMovedThrough = 3;
        public const string MaximumParameter = "maximum";
        public const string CountOutput = "count";
        public const string MinionSelectNode = "trample-minions";

        public static string HeroOutput(int slot) => "hero" + slot;

        public static string HasHeroOutput(int slot) => "hasHero" + slot;

        public static string MinionOutput(int slot) => "minion" + slot;

        public static string DiscardNode(int slot) => "trample-discard-" + slot;

        private static readonly ActiveEffectCancellationCondition[] TurnCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>"If you move in a straight line: You may ignore obstacles" - for this turn's walk.</summary>
        public static ActiveEffectDefinition TrampleDefinition { get; } = new ActiveEffectDefinition(
            "wuk-trample-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            TurnCancellations,
            false,
            "WukTrample",
            "wuk-trample",
            Array.Empty<TimingCheckpoint>(),
            new[]
            {
                RuleModification.ForPermission(
                    RulePermissionKind.MoveThroughObstaclesInAStraightLine, RuleActivity.Movement)
            });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(TrampleV1, TrampleDefinition),
            new TrampledEnemyHeroesHandler(),
            new ChooseTrampledMinionsHandler()
        };

        /// <summary>The body before the walk: grant the straight-line permission.</summary>
        public static EffectDefinition BeforeText(CardDefinitionId card)
        {
            var source = new EffectNodeId("trample-permission");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-before-movement"), WukCardContent.ContentRevision, source,
                new[]
                {
                    new EffectNodeDefinition(
                        source, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, TrampleV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>The body after the walk: the heroes and minions it moved through.</summary>
        public static EffectDefinition AfterText(CardDefinitionId card, int mostMinions)
        {
            var root = new EffectNodeId("trample-root");
            var heroes = new EffectNodeId("trample-heroes");
            var minions = new EffectNodeId(MinionSelectNode);
            var children = new List<EffectNodeId> { heroes };
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    heroes, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, TrampledEnemyHeroesV1,
                    outputs: Enumerable.Range(1, MostHeroesMovedThrough).SelectMany(slot => new[]
                    {
                        new EffectOutputBinding(HeroOutput(slot), EffectValueKind.Hero),
                        new EffectOutputBinding(HasHeroOutput(slot), EffectValueKind.Boolean)
                    }).ToArray())
            };

            for (var slot = 1; slot <= MostHeroesMovedThrough; slot++)
            {
                var gate = new EffectNodeId("trample-hero-" + slot);
                var clause = new EffectNodeId("trample-hero-clause-" + slot);
                var discard = new EffectNodeId(DiscardNode(slot));
                var escalation = new EffectNodeId("trample-escalation-" + slot);
                var defeat = new EffectNodeId("trample-defeat-hero-" + slot);
                children.Add(gate);
                nodes.Add(new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { clause },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("flag", heroes, HasHeroOutput(slot), EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }));
                nodes.Add(new EffectNodeDefinition(
                    clause, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { discard, escalation }));
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, heroes, HeroOutput(slot),
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance),
                        new EffectOutputBinding(SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard, SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }));
                nodes.Add(new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, heroes, HeroOutput(slot), EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    }));
            }

            children.Add(minions);
            nodes.Add(new EffectNodeDefinition(
                minions, EffectNodeKind.Capability, EffectFailurePolicy.UpTo, ChooseTrampledMinionsV1,
                inputs: new[] { EffectInputBinding.FromLiteral(MaximumParameter, EffectValue.Integer(mostMinions)) },
                outputs: new[] { new EffectOutputBinding(CountOutput, EffectValueKind.Integer) }
                    .Concat(Enumerable.Range(1, mostMinions).Select(slot =>
                        new EffectOutputBinding(MinionOutput(slot), EffectValueKind.Minion)))
                    .ToArray()));
            for (var slot = 1; slot <= mostMinions; slot++)
            {
                var defeat = new EffectNodeId("trample-defeat-minion-" + slot);
                children.Add(defeat);
                nodes.Add(new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticConsequenceCapabilities.DefeatMinionV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.MinionParameter, minions, MinionOutput(slot),
                            EffectValueKind.Minion)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticConsequenceCapabilities.DefeatedMinionOutput, EffectValueKind.Minion)
                    }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: children.ToArray()));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-after-movement"), WukCardContent.ContentRevision, root, nodes);
        }

        /// <summary>
        /// The actor's last completed walk when it is this Movement's own and one straight line - the
        /// sentence's "if" - with the units standing on the spaces it moved through, in walk order.
        /// </summary>
        private static IReadOnlyList<BoardObjectReference> Trampled(EffectCapabilityContext context)
        {
            var board = context.Components?.Board;
            var walk = (board as ICompletedMovementWalkRecord)?.LastCompletedMovementWalk;
            if (board == null || walk == null || !walk.Mover.Equals(context.ActorId) ||
                !StraightLineQuery.IsStraightLine(walk.Route))
            {
                return Array.Empty<BoardObjectReference>();
            }

            var actor = BoardObjectReference.ForHero(context.ActorId).AsRuleObject();
            var result = new List<BoardObjectReference>();
            foreach (var space in walk.SpacesMovedThrough)
            {
                if (board.TryGetObjectAt(space, out var occupant) &&
                    (occupant.Kind == BoardObjectKind.Hero || occupant.Kind == BoardObjectKind.Minion) &&
                    !TargetQuery.IsPrevented(
                        occupant.AsRuleObject(), RuleActivity.Movement, actor, board,
                        context.Participants, context.Modifications, context.ImmunityOverrides))
                {
                    result.Add(occupant);
                }
            }

            return result;
        }

        /// <summary>"each enemy hero you moved through" - published, never asked; up to three slots.</summary>
        private sealed class TrampledEnemyHeroesHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => TrampledEnemyHeroesV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                Array.Empty<EffectCapabilityParameter>(),
                Enumerable.Range(1, MostHeroesMovedThrough).SelectMany(slot => new[]
                {
                    new EffectOutputBinding(HeroOutput(slot), EffectValueKind.Hero),
                    new EffectOutputBinding(HasHeroOutput(slot), EffectValueKind.Boolean)
                }).ToArray());

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var heroes = Trampled(context)
                    .Where(unit => unit.Kind == BoardObjectKind.Hero && TargetQuery.StandsInRelationTo(
                        SourceTeamRelation.OpposingSource, unit, context.ActorId, context.Participants,
                        context.Components.Board))
                    .Take(MostHeroesMovedThrough)
                    .ToArray();
                var facts = new List<EffectOutcomeFact>();
                for (var slot = 1; slot <= MostHeroesMovedThrough; slot++)
                {
                    var present = slot <= heroes.Length;
                    facts.Add(new EffectOutcomeFact(HasHeroOutput(slot), EffectValue.Boolean(present)));
                    if (present)
                    {
                        facts.Add(new EffectOutcomeFact(HeroOutput(slot), EffectValue.Hero(heroes[slot - 1].HeroId)));
                    }
                }

                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();
        }

        /// <summary>
        /// "defeat up to one (two) minion(s) you moved through" - an ENEMY minion, Wuk's choice. The
        /// rulebook's Defeat entry forbids defeating a friendly unit with no exceptions (ruling W4,
        /// milestone 9.2, replacing WU-Q4's either-side reading).
        /// </summary>
        private sealed class ChooseTrampledMinionsHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseTrampledMinionsV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(MaximumParameter, EffectValueKind.Integer, true) },
                new[]
                {
                    new EffectOutputBinding(CountOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(MinionOutput(1), EffectValueKind.Minion),
                    new EffectOutputBinding(MinionOutput(2), EffectValueKind.Minion)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(CountOutput, EffectValue.Integer(0))
                    });
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(EffectValue.Minion).ToArray(),
                    0,
                    Math.Min(Maximum(context), candidates.Count)));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = (selected ?? Array.Empty<EffectValue>()).ToArray();
                if (chosen.Length > Maximum(context) ||
                    chosen.Any(value => value.Kind != EffectValueKind.Minion) ||
                    chosen.Select(value => value.MinionId).Distinct().Count() != chosen.Length ||
                    chosen.Any(value => !candidates.Contains(value.MinionId)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(CountOutput, EffectValue.Integer(chosen.Length))
                };
                for (var slot = 1; slot <= chosen.Length; slot++)
                {
                    facts.Add(new EffectOutcomeFact(MinionOutput(slot), chosen[slot - 1]));
                }

                return EffectExecutionResult.Applied(facts);
            }

            private static int Maximum(EffectCapabilityContext context) =>
                context.Inputs.TryGetValue(MaximumParameter, out var value) ? value.IntegerValue : 0;

            private static IReadOnlyList<MinionId> Candidates(EffectCapabilityContext context) =>
                Trampled(context)
                    .Where(unit => unit.Kind == BoardObjectKind.Minion && TargetQuery.StandsInRelationTo(
                        SourceTeamRelation.OpposingSource, unit, context.ActorId, context.Participants,
                        context.Components.Board))
                    .Select(unit => unit.MinionId)
                    .ToArray();
        }
    }
}
