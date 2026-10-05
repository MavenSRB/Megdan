using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z32. The two capabilities a hero's own token line needs, and the first token
    /// handlers in the engine bound to <em>verified</em> content rather than to milestone 6.2's
    /// synthetic hazard.
    ///
    /// <para><b>Why they are new capabilities and not parameters on the synthetic pair.</b> Milestone
    /// 6.2's <c>synthetic-place-token@1</c> and <c>synthetic-reclaim-token@1</c> hardcode one token
    /// definition and a radius of 1 measured from the actor, and both of those are exactly what a
    /// printed card decides: Glacial Barrier places Ice "in radius" and its Radius icon says 3, while
    /// Ancestral Totem places a Totem "in range" at 2. Changing the synthetics would have moved a
    /// registered capability every existing snapshot names.</para>
    ///
    /// <para><b>One handler per token definition, which is Tigerclaw's marker pattern reused.</b> The
    /// definition is bound to the handler rather than passed on the wire, for the reason milestone 8.23
    /// gives: a payload can then name a node but never a different token, and the registered list a
    /// snapshot pins says exactly which tokens this build can put on the board. It also avoids widening
    /// <see cref="EffectValue"/> with a token-definition arm that only content would ever supply.</para>
    ///
    /// <para><b>Registered at milestone 8.37</b>, the milestone that deals Tali. Her own six
    /// instances are content of hers and register from <c>TaliEffectContent</c>; what registers HERE
    /// is the pair of showcase instances milestone 7.9's gate asks for, because both classes are
    /// handler TYPES this build had never registered and the gate refuses a type that arrives for the
    /// first time inside a verified hero's slice. They are bound to milestone 6.2's own synthetic
    /// hazard token rather than to a new definition, so the showcase proves the TYPE without adding a
    /// component to the registered set that no card places.</para>
    /// </summary>
    public static class SyntheticTokenCapabilities
    {
        /// <summary>
        /// The physical token this action would place. A trusted enclosing borrow may replace
        /// every token kind it would have placed; supply and placement must read this same answer.
        /// </summary>
        public static TokenDefinition EffectiveDefinition(
            EffectCapabilityContext context, TokenDefinition printed)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (printed == null) throw new ArgumentNullException(nameof(printed));
            var kind = context.EnclosingBorrow.SubstituteTokenKind;
            if (!kind.IsDefined)
                return printed;
            var catalog = context.Components?.Catalog;
            if (catalog == null)
                return null;
            var reference = catalog.RegisteredTokens.SingleOrDefault(candidate =>
                candidate.DefinitionId.Equals(kind));
            return reference.IsDefined ? catalog.LoadToken(reference) : null;
        }

        /// <summary>
        /// The space a placement put its token on, published so a following node can read it - the
        /// same output name milestone 6.2's synthetic placement publishes, because it is the same fact.
        /// </summary>
        public const string PlacedOutput = "placed";

        /// <summary>The space a p.18 short-supply reclamation took a token back from.</summary>
        public const string ReclaimedOutput = "reclaimed";

        /// <summary>
        /// Milestone 8.37's showcase instances of both handler types, over milestone 6.2's synthetic
        /// hazard. The reach channel is Radius because that is the channel the showcase Skill that
        /// names them prints, and the reclaim's supply group is the hazard's own.
        /// </summary>
        public static EffectCapabilityId ShowcasePlaceHazard { get; } =
            new EffectCapabilityId("showcase-place-hero-token");

        public static EffectCapabilityId ShowcaseReclaimHazard { get; } =
            new EffectCapabilityId("showcase-reclaim-hero-token");

        public static EffectCapabilityReference ShowcasePlaceHazardV1 =>
            new EffectCapabilityReference(ShowcasePlaceHazard, 1);

        public static EffectCapabilityReference ShowcaseReclaimHazardV1 =>
            new EffectCapabilityReference(ShowcaseReclaimHazard, 1);

        /// <summary>See the type remarks: the two showcase instances milestone 7.9's gate asks for.</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new[]
            {
                PlaceHandler(
                    ShowcasePlaceHazardV1,
                    Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition,
                    PrintedValueChannel.Radius),
                ReclaimHandler(
                    ShowcaseReclaimHazardV1,
                    Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition)
            };

        /// <summary>
        /// One placement handler for one token definition, reaching the distance that definition's
        /// placing card prints on the named channel.
        /// </summary>
        /// <param name="instanceIdentityLabel">
        /// Milestone 8.61d. What this placement writes into the minted instance ID where it would
        /// otherwise write the definition's own ID, or <c>null</c> for the definition's ID - which is
        /// every instance written before that milestone and leaves their IDs byte-identical.
        ///
        /// <para><b>Why a placement ever needs this.</b> The instance ID is the handle the wire writes
        /// on every token row, and a facedown token's row is the one place a seat could read a kind the
        /// per-seat masking has just taken out of the definition ID. Min's mines therefore mint
        /// <c>...:mine:...</c> for a blast and for a dud alike. Uniqueness is unharmed: the segment the
        /// definition contributed exists to tell two ROLES of one pile apart when they land on the same
        /// space in the same turn, and Min's placements are one node each, so the node ID the ID already
        /// carries tells them apart on its own.</para>
        /// </param>
        public static IEffectCapabilityHandler PlaceHandler(
            EffectCapabilityReference reference,
            TokenDefinition definition,
            PrintedValueChannel reachChannel,
            string instanceIdentityLabel = null,
            string adjacentToObstacleKind = null,
            int minimumSameKindHexDistance = 0) =>
            new PlaceHeroTokenHandler(reference, definition, reachChannel,
                instanceIdentityLabel, adjacentToObstacleKind, minimumSameKindHexDistance);

        /// <summary>
        /// One p.18 short-supply handler for one token definition. It offers back every token in that
        /// definition's <em>supply group</em>, which is the sentence's "tokens of the same type" read
        /// as RULING 44 reads it for a hero whose roles share one physical pile.
        /// </summary>
        public static IEffectCapabilityHandler ReclaimHandler(
            EffectCapabilityReference reference, TokenDefinition definition) =>
            new ReclaimHeroTokenHandler(reference, definition);

        /// <summary>
        /// Milestone 9.8e. p.19's Replace of the minion standing on <c>subject</c> with one token of
        /// <paramref name="definition"/> from its supply - Mortimer's "Replace an enemy minion ... with
        /// a Zombie token". Which minion is the selection's question; this handler performs the
        /// substitution through <see cref="Rules.BoardObjects.ReplaceOperation.ReplaceMinion"/> and
        /// publishes where it happened and whether it is p.8's Lane-Push cause, as a Remove does. A
        /// supply with nothing left is the short-supply step's to fix, composed before this node.
        /// </summary>
        public static IEffectCapabilityHandler ReplaceMinionHandler(
            EffectCapabilityReference reference, TokenDefinition definition) =>
            new ReplaceMinionWithTokenHandler(reference, definition);

        /// <summary>The space a minion-to-token Replace happened in.</summary>
        public const string ReplacedOutput = "replaced";

        private sealed class ReplaceMinionWithTokenHandler : IEffectCapabilityHandler
        {
            private static readonly Rules.BoardObjects.ReplacePolicy Policy = new Rules.BoardObjects.ReplacePolicy(
                Rules.BoardObjects.ReplaceIncomingSource.Supply,
                Rules.BoardObjects.ReplaceOutgoingDestination.Supply,
                Rules.BoardObjects.ReplacePropertyTransferPolicy.NoTransfer,
                Rules.BoardObjects.ReplaceObservationPolicy.ReplacementOnly);

            private readonly TokenDefinition definition;

            internal ReplaceMinionWithTokenHandler(EffectCapabilityReference reference, TokenDefinition definition)
            {
                if (!reference.IsDefined) throw new ArgumentException("A handler names its capability.", nameof(reference));
                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("subject", EffectValueKind.Coordinate) },
                new[]
                {
                    new EffectOutputBinding(ReplacedOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(
                        Rules.EffectResolution.RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryResolve(context, out var minion, out var at))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var components = context.Components;
                var placedDefinition = EffectiveDefinition(context, definition);
                if (placedDefinition == null)
                    return EffectExecutionResult.ImpossibleMandatory();
                var outcome = Rules.BoardObjects.ReplaceOperation.ReplaceMinion(
                    Policy,
                    components.Exchange,
                    components.Board,
                    components.Tokens,
                    components.Catalog,
                    minion,
                    new PlacedToken(
                        new TokenInstanceId(
                            $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                            $":{placedDefinition.Id.Value}:r{context.RoundIndex}t{context.TurnIndex}:{at.Q}:{at.R}"),
                        placedDefinition,
                        context.ActorId,
                        context.ControllerId,
                        context.SourceCardInstanceId,
                        at),
                    Stage(context));
                if (!outcome.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ReplacedOutput, EffectValue.Space(outcome.Coordinate)),
                    new EffectOutcomeFact(
                        Rules.EffectResolution.RecordedLanePushQuery.OutcomeFactName,
                        EffectValue.Boolean(outcome.QualifiesForLanePush))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// The same reading <see cref="Execute"/> makes, without the substitution - except that a
            /// token is obtainable while one stands on the board as well as in the supply. p.18's
            /// short-supply step ("take one from the board") is composed before every such Replace,
            /// so asked before that step, an empty pile beside a placed token is not a refusal.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var placedDefinition = EffectiveDefinition(context, definition);
                return placedDefinition != null &&
                    TryResolve(context, out var minion, out _) &&
                    (context.Components.Catalog.CountAvailableTokens(
                            placedDefinition.Reference, context.Components.Tokens.PlacedTokens) > 0 ||
                        context.Components.Tokens.PlacedTokens.Any(token =>
                            token.Definition.Reference.Equals(placedDefinition.Reference))) &&
                    !Stage(context).IsPrevented(BoardObjectReference.ForMinion(minion))
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.ImpossibleMandatory;
            }

            private static bool TryResolve(
                EffectCapabilityContext context, out Setup.MinionId minion, out HexCoordinate at)
            {
                minion = default;
                at = default;
                var components = context.Components;
                var subject = context.Input("subject");
                if (components == null || !components.IsLive || subject.Kind != EffectValueKind.Coordinate ||
                    !components.Board.TryGetObjectAt(subject.Coordinate, out var occupant) ||
                    occupant.Kind != BoardObjectKind.Minion)
                {
                    return false;
                }

                minion = occupant.MinionId;
                at = subject.Coordinate;
                return true;
            }

            private static Rules.BoardObjects.RemovalStageDecision Stage(EffectCapabilityContext context) =>
                new Rules.BoardObjects.RemovalStageDecision(
                    context.EnclosingActivity,
                    Rules.Modifications.RuleObject.ForHero(context.ActorId),
                    context.Components.Board,
                    context.Components.Units,
                    context.Participants,
                    context.Modifications,
                    GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.Removal);
        }

        /// <summary>
        /// Places one token from the supply into an empty playable space within the placing card's
        /// printed reach of the acting hero (rulebook p.18's Place).
        ///
        /// <para>The destination bound is the card's own printed icon composed with p.16's items, read
        /// through the same value queries every printed-icon selection reads - so a +1 Radius item
        /// makes Pack Ice reach 4 without this handler knowing an item exists. p.9 is what licenses
        /// that: "the value on the Range/Radius icon determines the maximum Range/Radius of your
        /// action. If the card text mentions Range/Radius, it refers to this value" - and both of her
        /// token families' texts do say "in radius" and "in range" respectively.</para>
        ///
        /// <para>It places <b>one</b> token. "Place up to 2" and "up to 3" are the composition's job -
        /// two and three optional nodes - for the same reason milestone 6.2 split p.18's short-supply
        /// step into its own node: a handler is stateless and cannot tell a second resumption from a
        /// first, so a second decision belongs to a second node.</para>
        /// </summary>
        private sealed class PlaceHeroTokenHandler
            : IEffectCapabilityHandler, IPlacedTokenCapabilityHandler
        {
            private readonly TokenDefinition definition;
            private readonly PrintedValueChannel reachChannel;
            private readonly string instanceIdentityLabel;
            private readonly string adjacentToObstacleKind;
            private readonly int minimumSameKindHexDistance;

            public PlaceHeroTokenHandler(
                EffectCapabilityReference reference,
                TokenDefinition definition,
                PrintedValueChannel reachChannel,
                string instanceIdentityLabel = null,
                string adjacentToObstacleKind = null,
                int minimumSameKindHexDistance = 0)
            {
                if (reachChannel != PrintedValueChannel.Radius && reachChannel != PrintedValueChannel.Range)
                {
                    throw new ArgumentException(
                        "A token placement reaches a printed Radius or a printed Range; p.9 gives a " +
                        "card no third reach.",
                        nameof(reachChannel));
                }

                if (instanceIdentityLabel != null && string.IsNullOrWhiteSpace(instanceIdentityLabel))
                {
                    throw new ArgumentException(
                        "An instance-identity label is named or absent; a blank one is neither.",
                        nameof(instanceIdentityLabel));
                }

                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
                this.reachChannel = reachChannel;
                this.instanceIdentityLabel = instanceIdentityLabel;
                if (adjacentToObstacleKind != null && string.IsNullOrWhiteSpace(adjacentToObstacleKind))
                    throw new ArgumentException("Obstacle kind is named or absent.", nameof(adjacentToObstacleKind));
                this.adjacentToObstacleKind = adjacentToObstacleKind;
                if (minimumSameKindHexDistance < 0)
                    throw new ArgumentOutOfRangeException(nameof(minimumSameKindHexDistance));
                this.minimumSameKindHexDistance = minimumSameKindHexDistance;
            }

            public EffectCapabilityReference Reference { get; }

            /// <summary>
            /// Milestone 8.88a. The space clauses the printed-icon family knows, each optional -
            /// "place a rock token into an empty space adjacent to the target" binds data here.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                SyntheticTargetingCapabilities.PlacementClauseParameters(),
                new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Milestone 8.61j. The definition this placement puts on the board, read-only, so a walk
            /// following what a CARD can reach can follow a token's own declarations too - see
            /// <see cref="IPlacedTokenCapabilityHandler"/> for why that route exists.
            /// </summary>
            public TokenDefinition PlacedToken => definition;

            private TokenDefinition PlacedDefinition(EffectCapabilityContext context)
                => EffectiveDefinition(context, definition);

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var placedDefinition = PlacedDefinition(context);
                if (placedDefinition == null ||
                    components.Catalog.CountAvailableTokens(
                        placedDefinition.Reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var candidates = LegalSpaces(context, components).Select(EffectValue.Space).ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actorAt))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var origin = SyntheticTargetingCapabilities.MeasuredFromOr(context, actorAt);
                var coordinate = selected[0].Coordinate;

                // Milestone 8.88a. The space clauses are asked again of the answer, as the operation
                // below re-asks everything else.
                if (!new SyntheticTargetingCapabilities.PrintedIconSpaceClauses(
                        context, components.Board, origin, ResolveReach(context)).Admit(coordinate))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!AdjacentToLiveObstacle(context, components, coordinate))
                    return EffectExecutionResult.ImpossibleMandatory();

                var placedDefinition = PlacedDefinition(context);
                if (placedDefinition == null || !TokenPlacementSeparationQuery.Admits(
                        coordinate, placedDefinition.Reference, components.Tokens.PlacedTokens,
                        minimumSameKindHexDistance))
                    return EffectExecutionResult.ImpossibleMandatory();

                // The operation re-derives the admitted spaces and re-asks the supply rather than
                // trusting the paused choice: a space legal when the choice opened may have been taken
                // before the answer arrived, and the last copy of the shared pile may have been placed
                // elsewhere in the meantime.
                var outcome = PlaceOperation.PlaceFromSupply(
                    Policy(context, components),
                    components.Tokens,
                    components.Catalog,
                    new PlacedToken(
                        // Deterministic under replay and unique across placements: the round and turn
                        // distinguish a token from a later one that happens to occupy the space the
                        // first was reclaimed from, and the definition distinguishes two roles of the
                        // one pile placed on the same space in the same turn - unless the placing
                        // content supplied a label of its own, which milestone 8.61d added for a
                        // facedown token whose kind the instance ID must not carry to another seat.
                        new TokenInstanceId(
                            $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                            $":{instanceIdentityLabel ?? placedDefinition.Id.Value}" +
                            $":r{context.RoundIndex}t{context.TurnIndex}" +
                            $":{coordinate.Q}:{coordinate.R}"),
                        placedDefinition,
                        context.ActorId,
                        context.ControllerId,
                        context.SourceCardInstanceId,
                        coordinate),
                    origin,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications);
                if (!outcome.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PlacedOutput, EffectValue.Space(outcome.Destination))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var placedDefinition = PlacedDefinition(context);
                if (placedDefinition == null ||
                    components.Catalog.CountAvailableTokens(
                        placedDefinition.Reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return LegalSpaces(context, components).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// This node's Place, built from the card rather than held as a constant: the four fixed
            /// declarations are the ones p.18 fixes for every supply placement, and the reach is the
            /// one thing the printed face decides.
            /// </summary>
            private PlacePolicy Policy(EffectCapabilityContext context, MatchComponents components) =>
                new PlacePolicy(
                    PlaceSourceContainer.Supply,
                    PlaceEligiblePieceKind.Token,
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                    ResolveReach(context),
                    PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    // The piece comes from the supply and stands nowhere, so the only space the reach
                    // can be measured from is the acting hero's - which is also what both of her token
                    // families' texts mean by "in radius" and "in range".
                    PlaceDestinationAnchor.ActingHero);

            private int ResolveReach(EffectCapabilityContext context) =>
                (reachChannel == PrintedValueChannel.Radius
                    ? RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications)
                    : RangeValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications,
                        ActionValueApplicabilityQuery.For(
                            context.SourceDefinitionReference,
                            context.ActionDefinitionId,
                            context.CardState.Definitions))).Total;

            /// <summary>
            /// The empty spaces in reach, measured from the actor or from where the declaration moved
            /// the centre, with the declared space clauses applied. Milestone 8.88a.
            /// </summary>
            private IEnumerable<HexCoordinate> LegalSpaces(
                EffectCapabilityContext context, MatchComponents components)
            {
                var placedDefinition = PlacedDefinition(context);
                if (placedDefinition == null)
                    return Array.Empty<HexCoordinate>();
                if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actorAt))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var origin = SyntheticTargetingCapabilities.MeasuredFromOr(context, actorAt);
                var clauses = new SyntheticTargetingCapabilities.PrintedIconSpaceClauses(
                    context, components.Board, origin, ResolveReach(context));
                return PlaceDestinationQuery.FindDestinations(
                        Policy(context, components), components.Tokens, origin,
                        Array.Empty<HexCoordinate>(), context.Modifications)
                    .Where(clauses.Admit)
                    .Where(space => AdjacentToLiveObstacle(context, components, space))
                    .Where(space => TokenPlacementSeparationQuery.Admits(
                        space, placedDefinition.Reference, components.Tokens.PlacedTokens,
                        minimumSameKindHexDistance))
                    .ToArray();
            }

            private bool AdjacentToLiveObstacle(
                EffectCapabilityContext context, MatchComponents components, HexCoordinate space)
            {
                if (adjacentToObstacleKind == null) return true;
                return components.Obstacles != null &&
                    components.Obstacles.TryGetObstacleCoordinate(
                        OwnedObstacleIdentity.For(
                            adjacentToObstacleKind, context.ActorId), out var obstacleAt) &&
                    HexDistanceQuery.Calculate(space, obstacleAt) == 1;
            }
        }

        /// <summary>
        /// Rulebook p.18's short-supply step for a hero-owned token, and only that step: "If you need
        /// to place more tokens than are available in the supply, first remove up to that number of
        /// tokens of the same type from the board, if able."
        ///
        /// <para><b>The one thing it does differently from milestone 6.2's synthetic twin</b> is what
        /// "of the same type" reaches. For a token with its own supply it is that definition's tokens,
        /// unchanged. For Tali it is her whole supply group, because RULING 44 says her three roles are
        /// one token - so a Pack Ice needing a third copy may take back a standing totem, and a Totem
        /// placement may take back standing Ice. That is not a liberty taken here: it is the mechanical
        /// consequence of one shared type, and the ruling states it as such.</para>
        ///
        /// <para>It declines rather than fails when the supply can already cover the placement, because
        /// a legal "nothing needed to happen" is <see cref="EffectExecutionResultKind.DeclinedOptional"/>
        /// and not a refusal; and it reports impossible when the pile is empty and nothing of the type
        /// stands on the board, which under the <c>if able</c> policy its node declares means the step
        /// is simply skipped.</para>
        /// </summary>
        private sealed class ReclaimHeroTokenHandler : IEffectCapabilityHandler
        {
            private readonly TokenDefinition definition;

            private TokenDefinition ReclaimedDefinition(EffectCapabilityContext context)
                => EffectiveDefinition(context, definition);

            public ReclaimHeroTokenHandler(EffectCapabilityReference reference, TokenDefinition definition)
            {
                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(ReclaimedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reclaimedDefinition = ReclaimedDefinition(context);
                if (reclaimedDefinition == null)
                    return EffectExecutionResult.ImpossibleMandatory();
                if (components.Catalog.CountAvailableTokens(
                        reclaimedDefinition.Reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

                var candidates = SameTypeTokens(components, reclaimedDefinition)
                    .Select(token => EffectValue.Space(token.Coordinate))
                    .ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var coordinate = selected[0].Coordinate;
                var reclaimedDefinition = ReclaimedDefinition(context);
                if (reclaimedDefinition == null)
                    return EffectExecutionResult.ImpossibleMandatory();
                var token = SameTypeTokens(components, reclaimedDefinition)
                    .FirstOrDefault(candidate => candidate.Coordinate.Equals(coordinate));
                if (token == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reclaimed = components.Tokens.CommitValidatedTokenReclamation(token.Id);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ReclaimedOutput, EffectValue.Space(reclaimed.Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var reclaimedDefinition = ReclaimedDefinition(context);
                if (reclaimedDefinition == null)
                    return EffectExecutionResultKind.ImpossibleMandatory;
                if (components.Catalog.CountAvailableTokens(
                        reclaimedDefinition.Reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResultKind.DeclinedOptional;
                }

                return SameTypeTokens(components, reclaimedDefinition).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// p.18's "tokens of the same type", in a stable board order so the offer a player sees is
            /// a fact rather than an accident of collection order.
            /// </summary>
            private IEnumerable<PlacedToken> SameTypeTokens(
                MatchComponents components, TokenDefinition reclaimedDefinition) =>
                components.Catalog
                    .ReclaimableForPlacement(
                        reclaimedDefinition.Reference, components.Tokens.PlacedTokens)
                    .OrderBy(token => token.Coordinate.Q)
                    .ThenBy(token => token.Coordinate.R);
        }
    }
}
