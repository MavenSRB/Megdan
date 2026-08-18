using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.6's board-object capabilities: rulebook p.19's Swap, its Replace, and its Remove.
    ///
    /// Three capability IDs rather than one, because p.19 defines them as three things and
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> makes treating any of them as interchangeable a content-load
    /// rejection. What they share is beneath them, in the three operations and the one board port: an
    /// exchange of two occupied spaces, a substitution inside one space, and a piece leaving the board
    /// are different transactions, and none of them is a Movement or a Place.
    ///
    /// None of the three owns a decision of its own. Each takes the space of the piece it works on, and
    /// which piece that is comes from an earlier node - milestone 6.8k's target selection when a player
    /// chooses, milestone 6.5's actor-space bridge when the card text names the acting hero. That is the
    /// same division 6.8k settled for direct Move rather than a second selection prompt per operation.
    ///
    /// Milestone 6.6a moved the Swap and the Remove to revision 2, where each declares the kind of
    /// piece it may take. p.19 gives both of them their widest reading and a card may say something
    /// narrower - Blue Sentinel's three modes name a unit, a Hazard token, and a token - so the
    /// narrowing is definition data on the node, exactly as the activity Red Needle's Push asks
    /// immunity for is. It is declared on the operation rather than on the selection above it because
    /// milestone 6.8l settled that question: a legal target and a target the consuming step can act on
    /// are different questions, and the consumer owns the second one, so declaring it here is what
    /// makes the offer name only the pieces the mode can work on.
    ///
    /// Registered into the one shared catalog for the reason milestone 6.2 records: a match has one
    /// capability contract set, and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticExchangeCapabilities
    {
        public static EffectCapabilityId Swap { get; } = new EffectCapabilityId("synthetic-swap");

        public static EffectCapabilityId ReplaceToken { get; } =
            new EffectCapabilityId("synthetic-replace-token");

        public static EffectCapabilityId Remove { get; } = new EffectCapabilityId("synthetic-remove");

        /// <summary>
        /// Milestone 6.6a's second declaration of p.19's Swap, beside the one that names two spaces:
        /// this one exchanges the acting hero with one other object, which is how card text that says
        /// `your hero` reads.
        ///
        /// It is a separate capability rather than a revision because it declares something different
        /// rather than something more - the same division milestone 6.5a made when p.18's Push gained
        /// a named-target declaration beside its adjacency batch - and because that difference is what
        /// lets a selection above it be narrowed. A Swap between two named spaces takes its first
        /// object from a node that must run before the selection, and a walk judging an option has no
        /// outcomes yet, so a consumer reading one can never be asked. A Swap whose first object is
        /// the actor reads it from the frame, so the same question is answerable both when the option
        /// is judged and when the choice is offered.
        /// </summary>
        public static EffectCapabilityId SwapWithActor { get; } =
            new EffectCapabilityId("synthetic-swap-actor");

        /// <summary>
        /// The name a definition declares its eligible piece kind under. It lives here rather than in
        /// each definition so the three capabilities and every card naming them share one spelling, in
        /// the shape milestone 6.8l gave the selection's consumer parameter.
        /// </summary>
        public const string EligiblePieceParameterName = "eligiblePiece";

        /// <summary>Revision 2, minted at milestone 6.6a: revision 1's declaration plus the eligible kind.</summary>
        public static EffectCapabilityReference SwapV2 => new EffectCapabilityReference(Swap, 2);

        public static EffectCapabilityReference SwapWithActorV1 =>
            new EffectCapabilityReference(SwapWithActor, 1);

        public static EffectCapabilityReference ReplaceTokenV1 =>
            new EffectCapabilityReference(ReplaceToken, 1);

        /// <summary>Revision 2, minted at milestone 6.6a, for the same reason the Swap's is.</summary>
        public static EffectCapabilityReference RemoveV2 => new EffectCapabilityReference(Remove, 2);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new SwapHandler(),
                new SwapWithActorHandler(),
                new ReplaceTokenHandler(),
                new RemoveHandler()
            };

        /// <summary>
        /// The activity every one of these capabilities declares. Card text has no activity of its own -
        /// p.11 makes that explicit for movement written on a card, and nothing on p.19 gives Swap,
        /// Replace, or Remove one either - so each asks the enclosing Skill's activity, which is the
        /// reading milestone 6.5b settled when it converted card-text direct Move.
        /// </summary>
        private const RuleActivity Activity = RuleActivity.Skill;

        /// <summary>
        /// Rulebook p.19's Swap: "Two objects on the board swap places with each other. This does not
        /// count as movement or placement."
        ///
        /// Both objects arrive as spaces, which one-obstacle-per-space (p.19) makes an unambiguous
        /// identity for every kind at once and which keeps a token expressible without widening the
        /// typed value union - the same reasoning direct Move and Object Push already record.
        ///
        /// Immunity is asked about both. p.18 makes it a targeting rule and a Swap affects each object
        /// as much as the other, so asking only about the one a definition happens to name second would
        /// let an immune hero be dragged out of its space by naming it first. The declared eligible
        /// kind is read about both for the same reason.
        /// </summary>
        private sealed class SwapHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapV2;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("first", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter("second", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(
                        EligiblePieceParameterName, EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                PerformSwap(context, First(context), context.Input("second"));

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// The same resolution and the same two immunity questions <see cref="Execute"/> asks,
            /// without the exchange. This is a capability the probe exists for: its execution applies
            /// rather than pausing, so asking by running it would perform the Swap it asked about.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateSwap(context, First(context), context.Input("second"));

            private static EffectValue First(EffectCapabilityContext context) =>
                context.Input("first");
        }

        /// <summary>
        /// The same p.19 Swap with its first object fixed to the acting hero, which is how card text
        /// that says `your hero` reads. Everything below the two handlers is shared, so this is one
        /// declaration of one operation rather than a second implementation of it.
        ///
        /// The difference that matters is where the first object comes from. A space arrives from a
        /// node that has to run before the choice, and a walk judging an option has run nothing yet,
        /// so a selection declaring this consumer could never be narrowed - the branch would be
        /// offered whenever anything at all stood beside the actor, and choosing a piece the Swap
        /// cannot take would spend the action on a stop. The actor is in the frame, so the same
        /// question is answerable when the option is judged and again when the choice is offered.
        /// </summary>
        private sealed class SwapWithActorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapWithActorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(
                        EligiblePieceParameterName, EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var applied = PerformSwap(context, ActorSpace(context), context.Input("subject"));
                if (applied.Kind != EffectExecutionResultKind.Applied)
                {
                    return applied;
                }

                // The shared routine publishes the pair under the two-space capability's own names;
                // this declaration names them after what each object is, which is the only thing
                // about the outcome that differs.
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        "actorAt",
                        applied.Outcomes.Single(fact => fact.OutputName == "firstAt").Value),
                    new EffectOutcomeFact(
                        "subjectAt",
                        applied.Outcomes.Single(fact => fact.OutputName == "secondAt").Value)
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateSwap(context, ActorSpace(context), context.Input("subject"));

            /// <summary>
            /// The space the acting hero stands in, or the undefined value when it stands nowhere -
            /// which the shared resolution reads as no first object rather than as a fault.
            /// </summary>
            private static EffectValue ActorSpace(EffectCapabilityContext context)
            {
                var components = context.Components;
                return components != null && components.CanExchangeObjects &&
                    components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var at)
                    ? EffectValue.Space(at)
                    : default;
            }
        }

        /// <summary>
        /// p.19's Swap, performed on two spaces that the calling declaration resolved however it
        /// declares them.
        /// </summary>
        private static EffectExecutionResult PerformSwap(
            EffectCapabilityContext context, EffectValue firstSpace, EffectValue secondSpace)
        {
            if (!TryResolvePair(context, firstSpace, secondSpace, out var first, out var second))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var outcome = SwapOperation.Swap(
                DeclaredEligiblePiece(context), context.Components.Exchange,
                context.Components.Board, first, second);
            if (!outcome.Accepted)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            // Each object's published space is the one the other vacated, which is what a Swap
            // means; a later node binding these reads positions rather than a route.
            return EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact("firstAt", EffectValue.Space(outcome.SecondOrigin)),
                new EffectOutcomeFact("secondAt", EffectValue.Space(outcome.FirstOrigin))
            });
        }

        private static EffectExecutionResultKind AnticipateSwap(
            EffectCapabilityContext context, EffectValue firstSpace, EffectValue secondSpace) =>
            TryResolvePair(context, firstSpace, secondSpace, out _, out _)
                ? EffectExecutionResultKind.Applied
                : EffectExecutionResultKind.ImpossibleMandatory;

        /// <summary>
        /// The two objects a Swap names, or nothing when the pair cannot legally be swapped. Both
        /// spaces are read once, from one state, before either is judged.
        /// </summary>
        private static bool TryResolvePair(
            EffectCapabilityContext context,
            EffectValue firstSpace,
            EffectValue secondSpace,
            out BoardObjectReference first,
            out BoardObjectReference second)
        {
            first = default;
            second = default;

            var components = context.Components;
            var eligible = DeclaredEligiblePiece(context);
            if (components == null || !components.CanExchangeObjects ||
                eligible == SwapEligiblePieceKind.None)
            {
                return false;
            }

            if (firstSpace.Kind != EffectValueKind.Coordinate ||
                secondSpace.Kind != EffectValueKind.Coordinate ||
                !components.Board.TryGetObjectAt(firstSpace.Coordinate, out first) ||
                !components.Board.TryGetObjectAt(secondSpace.Coordinate, out second) ||
                first.Equals(second) ||
                !SwapOperation.Admits(eligible, first.Kind) ||
                !SwapOperation.Admits(eligible, second.Kind))
            {
                return false;
            }

            return !IsImmune(context, first) && !IsImmune(context, second);
        }

        /// <summary>
        /// The kind of piece this node declared it may exchange, or the named absence when the
        /// declaration is not a defined member. An undefined kind is refused rather than widened
        /// to p.19's breadth, because a definition that says nothing legible about what it may
        /// take should take nothing.
        /// </summary>
        private static SwapEligiblePieceKind DeclaredEligiblePiece(EffectCapabilityContext context)
        {
            var declared = (SwapEligiblePieceKind)context
                .Input(EligiblePieceParameterName).IntegerValue;
            return Enum.IsDefined(typeof(SwapEligiblePieceKind), declared)
                ? declared
                : SwapEligiblePieceKind.None;
        }

        /// <summary>
        /// Rulebook p.19's Replace: "Swap an object (such as a miniature or a token on the board) with a
        /// different object."
        ///
        /// The declared <see cref="ReplacePolicy"/> states the four things a replacement has to state,
        /// and the substitute is a Ward taken from its own finite supply - a different definition from
        /// the Hazard it replaces, because p.19 requires "a different object" and a swap between two
        /// instances of one definition changes nothing the rules can see.
        ///
        /// Which token is replaced is the only variable, and it arrives as a space from an earlier node.
        /// </summary>
        private sealed class ReplaceTokenHandler : IEffectCapabilityHandler
        {
            /// <summary>
            /// This capability's declared Replace. Constant rather than derived, because none of the
            /// four depends on the node: the substitute always comes from the supply, the replaced
            /// token always goes back to its own, nothing transfers, and one replacement is what
            /// observers see.
            /// </summary>
            private static readonly ReplacePolicy Policy = new ReplacePolicy(
                ReplaceIncomingSource.Supply,
                ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.NoTransfer,
                ReplaceObservationPolicy.ReplacementOnly);

            public EffectCapabilityReference Reference => ReplaceTokenV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("subject", EffectValueKind.Coordinate) },
                new[] { new EffectOutputBinding("replaced", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                var named = context.Input("subject");
                if (components == null || !components.CanExchangeObjects || !components.IsLive ||
                    named.Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var applied = new[]
                {
                    new EffectOutcomeFact("replaced", EffectValue.Space(named.Coordinate))
                };
                var incomingId = SubstituteId(context, named.Coordinate);
                if (components.Tokens.PlacedTokens.Any(token => token.Id.Equals(incomingId)))
                {
                    // This activation's substitute is already standing, so re-executing the node inside
                    // its own resolution is a legal no-op rather than a second Replace - the same shape
                    // milestone 6.8d gave every identity-minting capability. It is tested before the
                    // supply is, because what the supply is short of here is this activation's own copy.
                    return EffectExecutionResult.Applied(applied);
                }

                if (!TryResolve(context, out var outgoing, out var coordinate))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var outcome = ReplaceOperation.ReplaceToken(
                    Policy,
                    context.Components.Exchange,
                    context.Components.Tokens,
                    context.Components.Catalog,
                    outgoing,
                    Substitute(context, coordinate));
                return outcome.Accepted
                    ? EffectExecutionResult.Applied(applied)
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// The same subject resolution and the same supply reading, without the substitution and
            /// without composing an identity: a probe reads state and mints nothing at all.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryResolve(context, out _, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// The token this node replaces and the space it stands in, or nothing when there is no
            /// such token or no substitute left to bring in.
            /// </summary>
            private static bool TryResolve(
                EffectCapabilityContext context,
                out TokenInstanceId outgoing,
                out HexCoordinate coordinate)
            {
                outgoing = default;
                coordinate = default;

                var components = context.Components;
                if (components == null || !components.CanExchangeObjects || !components.IsLive)
                {
                    return false;
                }

                var named = context.Input("subject");
                if (named.Kind != EffectValueKind.Coordinate)
                {
                    return false;
                }

                var replaced = components.Tokens.PlacedTokens.FirstOrDefault(
                    token => token.Coordinate.Equals(named.Coordinate) &&
                        token.Definition.Reference.Equals(SyntheticComponentDefinitions.HazardV4));
                if (replaced == null)
                {
                    return false;
                }

                var incoming = SyntheticComponentDefinitions.WardV1;
                if (components.Catalog.CountAvailableTokens(incoming, components.Tokens.PlacedTokens) < 1)
                {
                    return false;
                }

                outgoing = replaced.Id;
                coordinate = named.Coordinate;
                return true;
            }

            /// <summary>The substitute this activation brings in, with the identity a replay reproduces.</summary>
            private static PlacedToken Substitute(
                EffectCapabilityContext context, HexCoordinate coordinate) =>
                new PlacedToken(
                    SubstituteId(context, coordinate),
                    context.Components.Catalog.LoadToken(SyntheticComponentDefinitions.WardV1),
                    context.ActorId,
                    context.ControllerId,
                    context.SourceCardInstanceId,
                    coordinate);

            /// <summary>
            /// Deterministic under replay - the same command prefix produces the same identity - and
            /// unique across activations, because the round and turn distinguish this substitute from a
            /// later one in the same space. The same composition milestone 6.2 gave a placed token.
            /// </summary>
            private static TokenInstanceId SubstituteId(
                EffectCapabilityContext context, HexCoordinate coordinate) =>
                new TokenInstanceId(
                    $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                    $":r{context.RoundIndex}t{context.TurnIndex}:{coordinate.Q}:{coordinate.R}");
        }

        /// <summary>
        /// Rulebook p.19's Remove: "Remove a game piece (such as a miniature, or a token) from the
        /// board. Do not collect any coins."
        ///
        /// The declared <see cref="RemovePolicy"/> takes its piece back to the supply. Which kind of
        /// piece it may take is the node's own declaration since milestone 6.6a: p.19's own breadth is
        /// either kind of game piece, and it is what makes the Lane-Push question worth asking here -
        /// removing a token empties nothing p.8 counts, while removing the last minion of a side out of
        /// the Battle Zone is one of the two causes p.8 recognizes - while a card that says token takes
        /// only a token and never reaches that question at all.
        ///
        /// The published <c>lanePush</c> fact is that cause, in the frame where a later node can bind
        /// it. It is a recorded cause and not a request: milestone 6.9 owns emitting and resolving p.8's
        /// mid-turn push, and this milestone owes it the one fact only the removing operation knows.
        /// </summary>
        private sealed class RemoveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RemoveV2;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(
                        EligiblePieceParameterName, EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                    new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryResolve(context, out var subject))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var outcome = RemoveOperation.Remove(
                    DeclaredPolicy(context),
                    context.Components.Exchange,
                    context.Components.Board,
                    subject,
                    RemovalStage(context));
                if (!outcome.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // A prevented removal is an applied step whose rules answer was "it stays". The piece
                // is still in its space, and the Lane-Push fact is false because nothing was removed.
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("removed", EffectValue.Space(outcome.Origin)),
                    new EffectOutcomeFact(
                        RecordedLanePushQuery.OutcomeFactName,
                        EffectValue.Boolean(outcome.LanePush.Qualifies))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// The same subject resolution <see cref="Execute"/> makes, without the removal. This is
            /// squarely what the probe exists for: executing to find out whether a Remove can do
            /// anything would take the piece off the board.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryResolve(context, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// The piece standing in the named space, or nothing when it is not removable.
            ///
            /// Eligibility is asked of the declared policy rather than restated here, which is what
            /// keeps this reading and the operation's the same one. Before milestone 6.6a it refused
            /// a hero directly, which agreed with the operation only while the declared kind was
            /// p.19's widest; under a narrower declaration a probe reading it that way would report a
            /// minion removable and the operation would then refuse it.
            /// </summary>
            private static bool TryResolve(
                EffectCapabilityContext context, out BoardObjectReference subject)
            {
                subject = default;

                var components = context.Components;
                if (components == null || !components.CanExchangeObjects || !components.IsLive ||
                    !components.CanSpawnUnits || context.Participants == null ||
                    !TryReadDeclaredPolicy(context, out var policy))
                {
                    return false;
                }

                var named = context.Input("subject");
                return named.Kind == EffectValueKind.Coordinate &&
                    components.Board.TryGetObjectAt(named.Coordinate, out subject) &&
                    policy.Admits(subject.Kind);
            }

            /// <summary>
            /// The Remove this node declared: its own eligible kind, and p.19's supply, which every
            /// declaration of this capability shares because a token rejoins its finite supply and a
            /// miniature the reserve a Respawn draws from.
            /// </summary>
            private static RemovePolicy DeclaredPolicy(EffectCapabilityContext context) =>
                TryReadDeclaredPolicy(context, out var policy)
                    ? policy
                    : throw new InvalidOperationException(
                        "A Remove node declares a defined eligible piece kind; this one does not, " +
                        "and the resolution should have refused before reaching the operation.");

            /// <summary>
            /// An undefined declared kind is refused rather than widened to p.19's breadth, for the
            /// reason the Swap's is: a definition that says nothing legible about what it may take
            /// should take nothing.
            /// </summary>
            private static bool TryReadDeclaredPolicy(
                EffectCapabilityContext context, out RemovePolicy policy)
            {
                policy = null;
                var declared = (RemoveEligiblePieceKind)context
                    .Input(EligiblePieceParameterName).IntegerValue;
                if (declared == RemoveEligiblePieceKind.None ||
                    !Enum.IsDefined(typeof(RemoveEligiblePieceKind), declared))
                {
                    return false;
                }

                policy = new RemovePolicy(declared, RemoveDestination.Supply);
                return true;
            }

            private static RemovalStageDecision RemovalStage(EffectCapabilityContext context) =>
                new RemovalStageDecision(
                    Activity,
                    RuleObject.ForHero(context.ActorId),
                    context.Components.Board,
                    context.Components.Units,
                    context.Participants,
                    context.Modifications,
                    LanePushCauseKind.Removal);
        }

        /// <summary>
        /// p.18's immunity for one exchanged object, asked with the enclosing Skill's activity. A token
        /// is never immune - p.19 states that tokens are not units - so it reports the named absence
        /// rather than a unit with nothing declared about it.
        /// </summary>
        private static bool IsImmune(EffectCapabilityContext context, BoardObjectReference subject)
        {
            var unit = subject.AsRuleObject();
            return unit.IsDefined && context.Participants != null && ImmunityQuery.IsImmuneTo(
                unit,
                Activity,
                RuleObject.ForHero(context.ActorId),
                context.Components.Board,
                context.Participants,
                context.Modifications);
        }
    }
}
