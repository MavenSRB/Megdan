using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.8k's target-selection capability: an effect definition's typed way to name another
    /// board object.
    ///
    /// It is an ordinary registered capability rather than a new node kind because the acceptance text
    /// requires the chosen space to reach a later node through the existing node-output binding - the
    /// selection pauses through the same capability-choice seam every other chooser-owned decision uses,
    /// and publishes one <see cref="EffectValueKind.Coordinate"/> output. The coordinate is the subject
    /// identity milestone 6.4 chose for displacement: one obstacle per space makes the object standing
    /// there nameable without widening the typed value union, which is how a token - unrepresentable as
    /// a rules object by p.19 - stays expressible.
    ///
    /// Its candidate set comes from <see cref="TargetQuery"/>, the reusable targeting contract, rather
    /// than effect-local geometry. Immunity therefore filters selection - p.18 makes it a targeting
    /// rule - while a presence question over the same spaces still counts the immune unit. The activity
    /// consulted is declared by revision 4. Revision 3 is bound to Skill; definitions feeding a
    /// consuming effect with its own activity use revision 4 so the offer and final operation ask the
    /// identical immunity question.
    ///
    /// Milestone 6.8l replaced revisions 1 and 2 with 3 and 4, which declare one further optional
    /// parameter: the node this selection selects for. A legal target and a target the consuming step
    /// can act on are different questions, and until the selection could name its consumer only the
    /// first was asked - so a branch behind one was offered whatever the step after it would do. The
    /// revisions moved rather than being joined because the registration list records contracts, the
    /// same reason milestone 6.8k retired direct Move revision 1 when it widened the subject.
    ///
    /// This is the seam 6.5b and 6.6a reuse: Red Needle's Push, Red Bombard's direct Move of another
    /// adjacent unit, and Blue Sentinel's Swap all name their object through this one selection rather
    /// than each inventing a binding.
    /// </summary>
    public static class SyntheticTargetingCapabilities
    {
        public static EffectCapabilityId ChooseTarget { get; } =
            new EffectCapabilityId("synthetic-choose-target");

        /// <summary>
        /// Revision 3, minted at milestone 6.8l: revision 1's declaration plus an optional consumer.
        /// The enclosing action's activity is what it asks immunity for.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV3 =>
            new EffectCapabilityReference(ChooseTarget, 3);

        /// <summary>
        /// Revision 4, minted at milestone 6.8l: revision 2's declaration plus an optional consumer.
        /// The activity it asks immunity for is the definition's, for an operation p.18 gives one.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV4 =>
            new EffectCapabilityReference(ChooseTarget, 4);

        /// <summary>
        /// Milestone 7.2b. The same selection bounded by rulebook p.9's printed Radius icon instead of
        /// a number the definition writes down.
        ///
        /// A second capability ID rather than a third revision of the one above, for the reason the
        /// named Push is a second ID beside the batch Push: these are two live declarations of
        /// different content, not two versions of one. Where a card's text says "choose a unit within
        /// 1 space" the range is card text and the definition states it; where the card prints a Radius
        /// icon, p.9 says "the value on the Range/Radius icon determines the maximum Range/Radius of
        /// your action. If the card text mentions Range/Radius, it refers to this value" - so the bound
        /// is printed content the capability reads, and p.16's items raise it. A card cannot hold both
        /// readings of one parameter, which is exactly the ambiguity the definition validator refuses.
        /// </summary>
        public static EffectCapabilityId RadiusTarget { get; } =
            new EffectCapabilityId("synthetic-radius-target");

        public static EffectCapabilityReference RadiusTargetV1 =>
            new EffectCapabilityReference(RadiusTarget, 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ChooseTargetHandler(ChooseTargetV3, declaresActivity: false),
                new ChooseTargetHandler(ChooseTargetV4, declaresActivity: true),
                new RadiusTargetHandler()
            };

        /// <summary>
        /// Milestone 7.2b's printed-Radius selection: the same chooser-owned target selection, whose
        /// bound is the acting card's Radius Total rather than an integer the definition carries.
        ///
        /// It states no range parameter at all, which is the whole point - there is nothing for a
        /// definition to write down, because p.9 puts the value on the card. What it does state is the
        /// activity it targets under, since p.18 declares immunity per activity and this selection
        /// filters immunity through the same <see cref="TargetQuery"/> the capability above uses. The
        /// disk itself comes from <see cref="RadiusQuery"/>, and the total from
        /// <see cref="RadiusValueQuery"/>, so neither the geometry nor the item composition is repeated
        /// here.
        /// </summary>
        private sealed class RadiusTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public RadiusTargetHandler()
            {
                Schema = new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("includeSelf", EffectValueKind.Boolean),
                        new EffectCapabilityParameter("activity", EffectValueKind.Integer),
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node,
                            isRequired: false)
                    },
                    new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference => RadiusTargetV1;

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            /// <summary>
            /// The spaces of every object the acting hero may target inside its card's Radius, in
            /// canonical axial order.
            ///
            /// The total is re-derived on every ask rather than captured, which is what makes the
            /// offered disk and the disk the answer is checked against the same one: an item gained
            /// between the two would widen both, and neither is written to the snapshot.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace || !context.CardState.IsAvailable)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var activity = (RuleActivity)context.Input("activity").IntegerValue;
                if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var board = components.Board;
                var selector = BoardObjectReference.ForHero(context.ActorId);
                if (!board.TryGetObjectCoordinate(selector, out var origin))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var radiusTotal = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems);
                if (radiusTotal.Total <= 0)
                {
                    // A card printing no Radius icon has no Radius at all, and p.16 does not give it
                    // one: an item raises a printed icon and never invents one. The selection is
                    // therefore impossible rather than adjacent-by-default.
                    return Array.Empty<HexCoordinate>();
                }

                var targets = RadiusQuery.Targets(
                    origin,
                    radiusTotal,
                    context.Input("includeSelf").BooleanValue,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications);

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                return spaces;
            }
        }

        /// <summary>
        /// Offers every targetable object within range of the acting hero as an authoritative
        /// chooser-owned selection and publishes the chosen object's space.
        ///
        /// It always pauses, even on a single candidate: that is the codebase's uniform capability
        /// precedent - direct Move pauses on one destination and an `up to 0` push pauses on one
        /// distance - and it keeps "chooser-owned" honest. Restore re-derives the candidates by
        /// re-asking this handler, exactly as every other capability choice restores. Selecting mutates
        /// nothing; the consuming node does whatever the effect does.
        /// </summary>
        private sealed class ChooseTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            private readonly bool declaresActivity;

            public ChooseTargetHandler(
                EffectCapabilityReference reference,
                bool declaresActivity)
            {
                Reference = reference;
                this.declaresActivity = declaresActivity;
                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("range", EffectValueKind.Integer),
                    new EffectCapabilityParameter("includeSelf", EffectValueKind.Boolean)
                };
                if (declaresActivity)
                {
                    parameters.Add(new EffectCapabilityParameter("activity", EffectValueKind.Integer));
                }

                // Milestone 6.8l. Optional, because a selection whose result no single later node
                // consumes still has a legal shape, and one that names nothing keeps exactly the
                // conservative reading milestone 6.5ac chose. Nothing here reads this parameter: the
                // handler cannot see the definition, so the resolver owns the narrowing and this
                // declaration is what lets a definition state it.
                parameters.Add(new EffectCapabilityParameter(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValueKind.Node,
                    isRequired: false));

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: an
                // object targetable when the choice opened may have moved or become immune before the
                // answer arrived.
                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                });
            }

            /// <summary>
            /// The same targeting question <see cref="Execute"/> asks, without opening the selection.
            /// Selecting mutates nothing even when it runs, so what this avoids is not a side effect
            /// but a pause: a probe that executed would leave a pending choice nobody asked for.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Milestone 6.8l: the same spaces <see cref="Execute"/> would offer, as the same typed
            /// values, from the same derivation. The resolver asks for them so it can put each one to
            /// the node this selection selects for; what it does with the answer is not this handler's
            /// business, and could not be - a handler sees its own context, never the definition
            /// around it.
            /// </summary>
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            /// <summary>
            /// The spaces of every object the acting hero may target, in canonical axial order. Nothing
            /// here walks the board or invents geometry: the reusable targeting contract owns both.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var range = context.Input("range").IntegerValue;
                if (range < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var board = components.Board;
                var selector = BoardObjectReference.ForHero(context.ActorId);
                if (!board.TryGetObjectCoordinate(selector, out var origin))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var activity = declaresActivity
                    ? (RuleActivity)context.Input("activity").IntegerValue
                    : RuleActivity.Skill;
                if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var targets = TargetQuery.FindTargetsWithin(
                    origin,
                    range,
                    context.Input("includeSelf").BooleanValue,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications);

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                return spaces;
            }
        }
    }
}
