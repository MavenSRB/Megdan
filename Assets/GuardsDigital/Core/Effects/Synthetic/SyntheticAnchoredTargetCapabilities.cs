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
        /// <summary>
        /// Milestone 8.61f. The hero a second pass over one anchor must not name again, declared only
        /// by an instance that says it reads one.
        ///
        /// <para><b>Why the exclusion is a parameter and not a second selection contract.</b> Holy
        /// Death Grenade prints "Up to <b>2</b> enemy heroes adjacent to that token" - two performances
        /// of one sentence over one anchor, which rulebook p.10 gives Min to choose one at a time - and
        /// the only thing that distinguishes the second offer from the first is that the hero already
        /// named is no longer among "2 enemy heroes". That is a narrowing of who may be NAMED, which is
        /// the same kind of thing <c>excludeAdjacent</c> and <c>excludeStraightLine</c> already are on
        /// the printed-icon selections, and it is bound to a node output rather than to a constant
        /// because the card names the hero one node earlier rather than in its own words.</para>
        ///
        /// <para><b>It is declared per instance rather than on every one.</b> Widget's four
        /// registered instances print no such clause, and a parameter they do not read would widen four
        /// pinned schemas for a sentence only Min's Blue III prints - so the parameter appears in
        /// <see cref="EffectCapabilityParameterSchema.Parameters"/> exactly when the instance was
        /// constructed to honour it, and those four are byte-identical to what milestone 8.35z32
        /// registered.</para>
        /// </summary>
        public const string ExcludedHeroParameterName = "excludeHero";

        /// <param name="excludesNamedHero">
        /// Milestone 8.61f. Whether this instance declares <see cref="ExcludedHeroParameterName"/> and
        /// drops the hero it names from its candidates. Defaults to the reading every instance made
        /// before it: no such clause, and no such parameter on the schema.
        /// </param>
        public static IEffectCapabilityHandler Handler(EffectCapabilityReference reference, RosterUnitKind kind,
            SourceTeamRelation relation, AnchorTargetShape shape, bool withinPrintedRange,
            bool excludesNamedHero = false) =>
            new AnchoredTargetHandler(reference, kind, relation, shape, withinPrintedRange, excludesNamedHero);

        private sealed class AnchoredTargetHandler : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly RosterUnitKind kind;
            private readonly SourceTeamRelation relation;
            private readonly AnchorTargetShape shape;
            private readonly bool withinPrintedRange;
            private readonly bool excludesNamedHero;
            public AnchoredTargetHandler(EffectCapabilityReference reference, RosterUnitKind kind,
                SourceTeamRelation relation, AnchorTargetShape shape, bool withinPrintedRange,
                bool excludesNamedHero)
            {
                if (kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    relation == SourceTeamRelation.None || !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    !Enum.IsDefined(typeof(AnchorTargetShape), shape)) throw new ArgumentException("Invalid anchored target declaration.");
                if (excludesNamedHero && kind == RosterUnitKind.Minion)
                    throw new ArgumentException("A minion selection has no hero to exclude.", nameof(excludesNamedHero));
                Reference = reference; this.kind = kind; this.relation = relation;
                this.shape = shape; this.withinPrintedRange = withinPrintedRange;
                this.excludesNamedHero = excludesNamedHero;
                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("anchor", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(EffectSelectionDeclaration.ConsumerParameterName, EffectValueKind.Node, isRequired: false)
                };
                // Optional, so a first pass that named nobody - a declined "up to" - leaves it unbound
                // and excludes nobody, which is what "up to 2" means when the first is declined.
                if (excludesNamedHero) parameters.Add(new EffectCapabilityParameter(
                    ExcludedHeroParameterName, EffectValueKind.Hero, isRequired: false));
                Schema = new EffectCapabilityParameterSchema(parameters, new[]
                {
                    new EffectOutputBinding("hero", EffectValueKind.Hero),
                    new EffectOutputBinding("minion", EffectValueKind.Minion),
                    new EffectOutputBinding("space", EffectValueKind.Coordinate)
                });
            }
            public EffectCapabilityReference Reference { get; }
            public EffectCapabilityParameterSchema Schema { get; }
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } = Array.Empty<ActiveEffectDefinition>();
            public bool PublishesWithoutApplying => true;
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;
            // Milestone 9.21c: a hero with several bodies beside the anchor is one hero (RZ-Q2).
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(ToValue).Distinct().ToArray();
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
                if (kind == RosterUnitKind.Minion && target.Kind == BoardObjectKind.Hero && context.Publishes("minion") &&
                    context.Modifications?.ModeRules?.HasMinionCategory(target.HeroId) == true)
                    result.Add(new EffectOutcomeFact("minion", EffectValue.Hero(target.HeroId)));
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
                        BoardObjectReference.ForHero(context.ActorId),
                        // Milestone 9.21c: every body, each tested against the anchor from its own space.
                        everyBody: true)
                    // Typed Hero/Minion consumers name that miniature, as the printed-icon selectors do.
                    .Where(t => kind == RosterUnitKind.Unit || (kind == RosterUnitKind.Hero
                        ? t.Kind == BoardObjectKind.Hero : t.Kind == BoardObjectKind.Minion ||
                            (t.Kind == BoardObjectKind.Hero && context.Modifications?.ModeRules?.HasMinionCategory(t.HeroId) == true)))
                    .Where(t => !TargetQuery.IsPrevented(t.AsRuleObject(), context.EnclosingActivity,
                        RuleObject.ForHero(context.ActorId), board, context.Participants, context.Modifications, context.ImmunityOverrides))
                    // Milestone 8.61f. The hero a previous pass over this anchor already named, where
                    // the instance declares that clause and the pass that would have named one ran.
                    .Where(t => !excludesNamedHero || t.Kind != BoardObjectKind.Hero ||
                        context.Input(ExcludedHeroParameterName).Kind != EffectValueKind.Hero ||
                        !t.HeroId.Equals(context.Input(ExcludedHeroParameterName).HeroId))
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
