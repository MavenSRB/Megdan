using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.21f. The two printed ideas the moves and swaps of a many-bodied hero needed and the
    /// build did not yet have. Everything else those faces print - the friendly-hero and minion
    /// selections, p.19's Swap with the actor, p.18's named Push, card-text direct Move and p.11's
    /// "after movement" moment - is composed on the vocabulary that already existed.
    ///
    /// <para><b>"Another one of you" / "one other you"</b> (<see cref="ChooseOtherActorBodyHandler"/>):
    /// one standing body of the ACTING hero other than the body performing the action, offered as the
    /// space it stands on and published as that space, for a following direct Move to move. It is a
    /// selection of the actor's own bodies, which no selection offered: the printed-icon and roster
    /// selections leave every body of the actor out (RZ-Q5 - none is "a unit" or "a friendly hero" for
    /// his own text), and "another one of you" is exactly those bodies. Its reach is the whole board
    /// (RZ-Q8, RZ-Q9: "any other body anywhere"); "other" is measured from the acting body (RZ-Q4).
    /// It names its consumer, so a body the Move could not move is never offered (8.35l).</para>
    ///
    /// <para><b>"Move ... in the opposite direction", as far as the line allows</b>
    /// (<see cref="ContinuedLineEndHandler"/>): the space a given number of spaces further along the
    /// line from a named space through the acting body, or - where the line is blocked first - the
    /// last space before the block (RZ-Q10: "Blocked, it stops (p.11)"). It asks nothing: the distance
    /// is a count the card fixes ("for every space the target moved"), not a choice, so it is the
    /// destination a direct Move under <c>NamedSpace</c> then walks, the same division Cutter's
    /// Evasive Shot makes between <c>choose-line-destination@1</c> and the Move - without the question,
    /// because Cutter's "up to" is a choice and this count is not. A count of nought, or a line blocked
    /// at its first space, has no destination: the step is not performed.</para>
    ///
    /// <para>Nothing here names a card or a hero, and nothing registers these until the hero whose
    /// content composes them is dealt.</para>
    /// </summary>
    public static class SyntheticHeroBodyMoveCapabilities
    {
        /// <summary>The space the chosen other body stands on, for the Move that moves it.</summary>
        public const string OtherBodyOutput = "body";

        /// <summary>The space the line on the other side of the acting body leads from.</summary>
        public const string ContinuedLineFromParameter = "continuedLineFrom";

        /// <summary>How many spaces the acting body travels along that line, at most.</summary>
        public const string DistanceParameter = "distance";

        /// <summary>Where the acting body's travel along the line ends.</summary>
        public const string DestinationOutput = "destination";

        /// <summary>"Another one of you": one body of the acting hero other than the acting body.</summary>
        public static IEffectCapabilityHandler ChooseOtherActorBodyHandler(EffectCapabilityReference reference) =>
            new ChooseOtherActorBody(reference);

        /// <summary>
        /// The end of the acting body's straight travel of up to <see cref="DistanceParameter"/> spaces
        /// along the line from <see cref="ContinuedLineFromParameter"/> through it, stopping before the
        /// first space a card-text move (p.11) cannot take.
        /// </summary>
        public static IEffectCapabilityHandler ContinuedLineEndHandler(EffectCapabilityReference reference) =>
            new ContinuedLineEnd(reference);

        private sealed class ChooseOtherActorBody : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public ChooseOtherActorBody(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        EffectSelectionDeclaration.ConsumerParameterName, EffectValueKind.Node,
                        isRequired: false)
                },
                new[] { new EffectOutputBinding(OtherBodyOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>A selection publishes where the body stands and moves nothing itself.</summary>
            public bool PublishesWithoutApplying => true;

            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                OtherBodies(context).Select(EffectValue.Space).ToArray();

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1 && chosen[0].Kind == EffectValueKind.Coordinate &&
                    OtherBodies(context).Contains(chosen[0].Coordinate)
                    ? new[] { new EffectOutcomeFact(OtherBodyOutput, EffectValue.Space(chosen[0].Coordinate)) }
                    : Array.Empty<EffectOutcomeFact>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = AnticipateCandidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            // Re-derived rather than trusted: the body named must still stand, and still not be the
            // one acting, when the answer arrives.
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = AnticipateOutcomes(context, selected);
                return facts.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                OtherBodies(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The spaces of the actor's standing bodies other than the acting body, in body order -
            /// the one derivation the offer, the answer and the probe ask. None for a one-bodied
            /// actor, whose only body is the one acting.
            /// </summary>
            private static IReadOnlyList<HexCoordinate> OtherBodies(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive ||
                    !components.Tokens.TryGetHeroCoordinate(context.ActorId, out var acting))
                {
                    return Array.Empty<HexCoordinate>();
                }

                return components.Tokens.GetHeroBodies(context.ActorId)
                    .Where(body => !body.Coordinate.Equals(acting))
                    .OrderBy(body => body.Body)
                    .Select(body => body.Coordinate)
                    .ToArray();
            }
        }

        private sealed class ContinuedLineEnd : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public ContinuedLineEnd(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(ContinuedLineFromParameter, EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer)
                },
                new[] { new EffectOutputBinding(DestinationOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>It names a space and moves nothing; the Move after it walks there.</summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Execute(context).Outcomes ?? Array.Empty<EffectOutcomeFact>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                TryFindEnd(context, out var end)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(DestinationOutput, EffectValue.Space(end))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryFindEnd(context, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// The farthest space, at most <see cref="DistanceParameter"/> steps along the heading from
            /// the named space through the acting body, that a card-text move reaches by walking that
            /// straight line - which is where p.11 stops it when something blocks the line sooner. The
            /// walk is the board's own direct-move reachability, so obstacles, terrain and the board's
            /// edge stop it exactly as they stop every card-text move. False for a count of nought, an
            /// unbound space or count, a named space that fixes no heading, or a first step blocked.
            /// </summary>
            private static bool TryFindEnd(EffectCapabilityContext context, out HexCoordinate end)
            {
                end = default;
                var components = context.Components;
                var from = context.Input(ContinuedLineFromParameter);
                var count = context.Input(DistanceParameter);
                if (components == null || !components.CanDisplace ||
                    from.Kind != EffectValueKind.Coordinate || count.Kind != EffectValueKind.Integer ||
                    count.IntegerValue < 1)
                {
                    return false;
                }

                var actor = BoardObjectReference.ForHero(context.ActorId);
                if (!components.Board.TryGetObjectCoordinate(actor, out var actorAt) ||
                    !ContinuedLineQuery.TryGetHeading(from.Coordinate, actorAt, out var heading))
                {
                    return false;
                }

                var reached = components.Board
                    .FindDirectMoveDestinations(actor, count.IntegerValue, context.Modifications, false)
                    .Where(route => IsStraightAlong(actorAt, heading, route.Route))
                    .OrderByDescending(route => route.Cost)
                    .Take(1)
                    .ToArray();
                if (reached.Length == 0)
                {
                    return false;
                }

                end = reached[0].Destination;
                return true;
            }

            private static bool IsStraightAlong(
                HexCoordinate origin, HexDirection heading, IReadOnlyList<HexCoordinate> route)
            {
                if (route == null || route.Count < 2 || !route[0].Equals(origin))
                {
                    return false;
                }

                for (var index = 1; index < route.Count; index++)
                {
                    if (!route[index].Equals(StraightLineQuery.Step(origin, heading, index)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
