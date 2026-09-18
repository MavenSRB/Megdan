using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    public enum AnchorTargetShape { Adjacent, StraightLine }

    /// <summary>Intersects an actor-origin printed Range with geometry from a separately bound space.</summary>
    public static class SyntheticAnchoredTargetCapabilities
    {
        public static IEffectCapabilityHandler Handler(EffectCapabilityReference reference, RosterUnitKind kind,
            SourceTeamRelation relation, AnchorTargetShape shape, bool withinPrintedRange) =>
            new AnchoredTargetHandler(reference, kind, relation, shape, withinPrintedRange);

        private sealed class AnchoredTargetHandler : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly RosterUnitKind kind;
            private readonly SourceTeamRelation relation;
            private readonly AnchorTargetShape shape;
            private readonly bool withinPrintedRange;
            public AnchoredTargetHandler(EffectCapabilityReference reference, RosterUnitKind kind,
                SourceTeamRelation relation, AnchorTargetShape shape, bool withinPrintedRange)
            {
                if (kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    relation == SourceTeamRelation.None || !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    !Enum.IsDefined(typeof(AnchorTargetShape), shape)) throw new ArgumentException("Invalid anchored target declaration.");
                Reference = reference; this.kind = kind; this.relation = relation;
                this.shape = shape; this.withinPrintedRange = withinPrintedRange;
            }
            public EffectCapabilityReference Reference { get; }
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(new[]
            {
                new EffectCapabilityParameter("anchor", EffectValueKind.Coordinate),
                new EffectCapabilityParameter(EffectSelectionDeclaration.ConsumerParameterName, EffectValueKind.Node, isRequired: false)
            }, new[]
            {
                new EffectOutputBinding("hero", EffectValueKind.Hero),
                new EffectOutputBinding("minion", EffectValueKind.Minion),
                new EffectOutputBinding("space", EffectValueKind.Coordinate)
            });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } = Array.Empty<ActiveEffectDefinition>();
            public bool PublishesWithoutApplying => true;
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(ToValue).ToArray();
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> chosen)
            {
                if (chosen == null || chosen.Count != 1) return Array.Empty<EffectOutcomeFact>();
                var target = Candidates(context).FirstOrDefault(t => ToValue(t).Equals(chosen[0]));
                if (!target.IsDefined || !context.Components.Board.TryGetObjectCoordinate(target, out var at))
                    return Array.Empty<EffectOutcomeFact>();
                var result = new List<EffectOutcomeFact>();
                if (context.Publishes("space")) result.Add(new EffectOutcomeFact("space", EffectValue.Space(at)));
                if (target.Kind == BoardObjectKind.Hero && context.Publishes("hero"))
                    result.Add(new EffectOutcomeFact("hero", EffectValue.Hero(target.HeroId)));
                if (target.Kind == BoardObjectKind.Minion && context.Publishes("minion"))
                    result.Add(new EffectOutcomeFact("minion", EffectValue.Minion(target.MinionId)));
                return result;
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var choices = AnticipateCandidates(context);
                return choices.Count == 0 ? EffectExecutionResult.ImpossibleMandatory() :
                    EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(EffectChooserRole.Actor, choices, 1, 1));
            }
            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = AnticipateOutcomes(context, selected);
                return facts.Count == 0 ? EffectExecutionResult.ImpossibleMandatory() : EffectExecutionResult.Applied(facts);
            }
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ? EffectExecutionResultKind.ImpossibleMandatory : EffectExecutionResultKind.NeedsChoice;

            private IReadOnlyList<BoardObjectReference> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || context.Participants == null || context.Input("anchor").Kind != EffectValueKind.Coordinate ||
                    !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<BoardObjectReference>();
                var anchor = context.Input("anchor").Coordinate;
                var reach = 1;
                if (withinPrintedRange)
                {
                    if (!context.CardState.IsAvailable) return Array.Empty<BoardObjectReference>();
                    reach = RangeValueQuery.Calculate(context.SourceCardInstanceId, context.SourceDefinitionReference,
                        context.ActionDefinitionId, context.CardState.Definitions, context.ActorId, context.HeroItems,
                        context.Modifications, ActionValueApplicabilityQuery.For(context.SourceDefinitionReference,
                            context.ActionDefinitionId, context.CardState.Definitions)).Total;
                    if (reach <= 0) return Array.Empty<BoardObjectReference>();
                }
                return RosterPresenceQuery.FindUnitsWithin(withinPrintedRange ? actorAt : anchor, reach, kind,
                        relation, context.ActorId, board, context.Participants, context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId))
                    // Typed Hero/Minion consumers name that miniature, as the printed-icon selectors do.
                    .Where(t => kind == RosterUnitKind.Unit || (kind == RosterUnitKind.Hero
                        ? t.Kind == BoardObjectKind.Hero : t.Kind == BoardObjectKind.Minion))
                    .Where(t => !TargetQuery.IsPrevented(t.AsRuleObject(), context.EnclosingActivity,
                        RuleObject.ForHero(context.ActorId), board, context.Participants, context.Modifications, context.ImmunityOverrides))
                    .Where(t => board.TryGetObjectCoordinate(t, out var at) &&
                        (shape == AnchorTargetShape.Adjacent ? HexDistanceQuery.Calculate(anchor, at) == 1 :
                            StraightLineQuery.AreInStraightLine(anchor, at)))
                    .ToArray();
            }
            private static EffectValue ToValue(BoardObjectReference target) => target.Kind == BoardObjectKind.Hero
                ? EffectValue.Hero(target.HeroId) : EffectValue.Minion(target.MinionId);
        }
    }
}
