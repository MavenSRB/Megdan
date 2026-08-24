using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.5's placement capabilities: rulebook p.18's Place, its Spawn, and the small bridge
    /// that lets a definition name the acting hero's own space.
    ///
    /// Place and Spawn are two capability IDs rather than one, because p.18 defines the second by
    /// ruling out half of the first - "similar to place, but unlike 'Place', the spawned object must be
    /// taken from the supply and cannot be taken from the board" - and
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> makes treating them as interchangeable a content-load
    /// rejection. Neither emits a movement fact of any kind: p.18 says placing "does not count as
    /// movement", so a Place that published one would be stating a rule it never performed.
    ///
    /// Registered into the one shared catalog for the reason milestone 6.2 records: a match has one
    /// capability contract set, and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticPlacementCapabilities
    {
        public static EffectCapabilityId ActorSpace { get; } =
            new EffectCapabilityId("synthetic-actor-space");

        public static EffectCapabilityId Place { get; } =
            new EffectCapabilityId("synthetic-place");

        public static EffectCapabilityId Spawn { get; } =
            new EffectCapabilityId("synthetic-spawn");

        public static EffectCapabilityReference ActorSpaceV1 =>
            new EffectCapabilityReference(ActorSpace, 1);

        /// <summary>
        /// Revision 2, minted at milestone 8.6: revision 1's subject and radius plus the two things
        /// p.18 leaves to the card - which kind of piece this Place may take, and the space its
        /// destinations are measured from.
        ///
        /// Revision 1 is retired rather than kept beside it, exactly as direct Move retired its own
        /// when the subject widened. Both new parameters are declarations revision 1 made silently: it
        /// always took any game piece and always measured from the piece itself, so a definition
        /// carrying the old contract would be asking for one particular pair of answers while claiming
        /// to ask for none.
        /// </summary>
        public static EffectCapabilityReference PlaceV2 =>
            new EffectCapabilityReference(Place, 2);

        /// <summary>
        /// Milestone 8.20. The printed-Range twin of <see cref="Place"/>, for card text that measures
        /// its destinations by p.9's icon rather than by a number the definition writes down - Arien's
        /// Liquid Leap, Magical Current and Stranger Tide, "Place yourself into a space in range".
        ///
        /// <para>A second capability ID rather than a parameter on the one above, by exactly the rule
        /// <see cref="SyntheticTargetingCapabilities.RangeTarget"/> records for the selections: p.9
        /// gives the two readings two meanings and p.16 two consequences, and a definition that could
        /// name either through one parameter would let a card whose reach is printed be widened by an
        /// item it does not carry, or the reverse. The handler beneath is nevertheless shared, so
        /// p.18's Place, its immunity question and its destination query are written once.</para>
        ///
        /// <para>It declares one thing the written-radius arm does not, and only because a verified
        /// card prints it: which Spawn-point spaces the destination refuses.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRange { get; } =
            new EffectCapabilityId("synthetic-place-in-range");

        public static EffectCapabilityReference PlaceInRangeV1 =>
            new EffectCapabilityReference(PlaceInRange, 1);

        /// <summary>The eligible-piece parameter's name, spelled once for the reason the Push's is.</summary>
        public const string EligiblePieceParameterName = "eligiblePiece";

        /// <summary>The destination-anchor parameter's name, spelled once for the same reason.</summary>
        public const string DestinationAnchorParameterName = "destinationAnchor";

        /// <summary>
        /// The Spawn-point exclusion parameter's name, carrying a
        /// <see cref="PlaceSpawnPointExclusion"/>. Milestone 8.20.
        /// </summary>
        public const string SpawnPointExclusionParameterName = "spawnPointExclusion";

        public static EffectCapabilityReference SpawnV1 =>
            new EffectCapabilityReference(Spawn, 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActorSpaceHandler(),
                new PlaceHandler(PlaceV2, PrintedValueChannel.None),
                new PlaceHandler(PlaceInRangeV1, PrintedValueChannel.Range),
                new SpawnHandler()
            };

        /// <summary>
        /// Publishes the acting hero's own space as a typed coordinate, deciding nothing.
        ///
        /// It exists because of the gap milestone 6.4a recorded: role bindings resolve from the
        /// execution frame, which holds no board reference, so no role can supply a coordinate and the
        /// definition validator refuses one that tries. A subject therefore reaches a capability only
        /// through an earlier node's published output - and until now the only such node was a target
        /// selection, which is a decision. `Place your hero` is not a decision about which piece, so
        /// asking the actor to pick itself out of a one-candidate prompt would invent a choice the
        /// card text does not offer. This is that same bridge without the prompt.
        /// </summary>
        private sealed class ActorSpaceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ActorSpaceV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var at))
                {
                    // A defeated or off-board hero stands nowhere, and a capability must be able to
                    // tell that apart from standing somewhere uninteresting.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("space", EffectValue.Space(at))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Applies whenever the actor stands somewhere. Publishing a space is applying, so a branch
            /// beginning here is judged performable by this node and never by what consumes the space -
            /// the declared conservative boundary of milestone 6.5ac's walk.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                return components != null && components.CanDisplace &&
                    components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }
        }

        /// <summary>
        /// Rulebook p.18's Place, board arm: "Take a game piece (such as a miniature, or a token) from
        /// the board ... and put it into a different space. A game piece can only be placed into an
        /// empty space (this does not count as movement)."
        ///
        /// The subject is the piece standing at the bound coordinate, the same space-as-identity direct
        /// Move and Object Push use, which one-obstacle-per-space (p.19) makes unambiguous and which
        /// keeps a token expressible without widening the typed value union. The destination is the one
        /// decision, so this is one node.
        ///
        /// The radius bound is synthetic, not a rule: which spaces a real effect may place into comes
        /// from its card text. What is not synthetic is everything the declared
        /// <see cref="PlacePolicy"/> states - board source, any game piece, a geometric destination
        /// question rather than a route, the acting hero choosing, and what having nowhere to put it
        /// means - and the shared <see cref="PlaceOperation"/> beneath enforces all of it.
        /// </summary>
        private sealed class PlaceHandler : IEffectCapabilityHandler
        {
            /// <summary>
            /// Where this declaration's reach comes from: <see cref="PrintedValueChannel.None"/> for a
            /// number the definition writes down, and <see cref="PrintedValueChannel.Range"/> for
            /// p.9's printed icon. Milestone 8.20, and the same constructor argument
            /// <see cref="SyntheticTargetingCapabilities"/>' shared selection handler takes.
            /// </summary>
            private readonly PrintedValueChannel boundChannel;

            public PlaceHandler(EffectCapabilityReference reference, PrintedValueChannel boundChannel)
            {
                Reference = reference;
                this.boundChannel = boundChannel;

                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Coordinate)
                };
                if (boundChannel == PrintedValueChannel.None)
                {
                    parameters.Add(new EffectCapabilityParameter("radius", EffectValueKind.Integer));
                }

                parameters.Add(new EffectCapabilityParameter(
                    EligiblePieceParameterName, EffectValueKind.Integer));
                parameters.Add(new EffectCapabilityParameter(
                    DestinationAnchorParameterName, EffectValueKind.Integer));
                if (boundChannel != PrintedValueChannel.None)
                {
                    // Carried only by the printed arm, because only a verified card printing this
                    // reading prints the clause: a parameter nothing sets is the ambiguity the
                    // Range/Radius split exists to refuse.
                    parameters.Add(new EffectCapabilityParameter(
                        SpawnPointExclusionParameterName, EffectValueKind.Integer));
                }

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryDeclare(context, out var policy, out _, out var origin))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var destinations = PlaceDestinationQuery.FindDestinations(
                    policy, context.Components.Tokens, origin, Array.Empty<HexCoordinate>(),
                    context.Modifications, context.Components.Units);
                if (destinations.Count == 0)
                {
                    return policy.NoDestinationPolicy == PlaceNoDestinationPolicy.Decline
                        ? EffectExecutionResult.DeclinedOptional()
                        : EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    destinations.Select(EffectValue.Space).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate ||
                    !TryDeclare(context, out var policy, out var subject, out var measuredFrom))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The operation re-derives the admitted destinations itself, so a space that was legal
                // when the choice opened but has since been taken is refused rather than committed.
                var outcome = PlaceOperation.PlaceFromBoard(
                    policy,
                    context.Components.Board,
                    context.Components.Tokens,
                    subject,
                    measuredFrom,
                    selected[0].Coordinate,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications,
                    context.Components.Units);
                return outcome.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("placed", EffectValue.Space(outcome.Destination))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            /// <summary>
            /// The same declaration and destination question <see cref="Execute"/> asks, without the
            /// choice. The no-destination answer follows the node's own policy exactly as the
            /// execution's does, so a Place that would decline is reported declining rather than
            /// impossible.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                if (!TryDeclare(context, out var policy, out _, out var origin))
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var destinations = PlaceDestinationQuery.FindDestinations(
                    policy, context.Components.Tokens, origin, Array.Empty<HexCoordinate>(),
                    context.Modifications, context.Components.Units);
                if (destinations.Count > 0)
                {
                    return EffectExecutionResultKind.NeedsChoice;
                }

                return policy.NoDestinationPolicy == PlaceNoDestinationPolicy.Decline
                    ? EffectExecutionResultKind.DeclinedOptional
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// This node's declared Place, the piece it names, and the space the question is measured
            /// from. The no-destination policy is read from the node's own
            /// <see cref="EffectFailurePolicy"/> rather than a parameter of its own, which is the
            /// decision milestone 6.4 recorded for distance optionality: p.10's mandatory-versus-`may`
            /// distinction is already carried by the four failure policies, and a second parameter
            /// saying the same thing could disagree with them.
            ///
            /// Immunity is asked of the named piece and only of the named piece. p.18 makes it a
            /// targeting rule about units, and <see cref="BoardObjectReference.AsRuleObject"/> is the
            /// named absence for a token, which p.19 says is not a unit - so a token is placed without
            /// an immunity question rather than by one that silently answers no.
            /// </summary>
            private bool TryDeclare(
                EffectCapabilityContext context,
                out PlacePolicy policy,
                out BoardObjectReference subject,
                out HexCoordinate measuredFrom)
            {
                policy = null;
                subject = default;
                measuredFrom = default;

                var components = context.Components;
                if (components == null || !components.CanDisplace || !components.IsLive)
                {
                    return false;
                }

                // Milestone 8.20. The written arm reads the number the definition states; the printed
                // arm reads p.9's icon with p.16's items already composed onto it, through the same
                // RangeValueQuery every printed-Range selection goes through. A card printing no Range
                // icon has no such reach at all and p.16 does not invent one, which the zero total
                // below turns into an impossible Place rather than an adjacent-by-default one.
                var radius = boundChannel == PrintedValueChannel.None
                    ? context.Input("radius").IntegerValue
                    : PrintedReach(context);
                var named = context.Input("subject");
                if (radius < 1 || named.Kind != EffectValueKind.Coordinate ||
                    !components.Board.TryGetObjectAt(named.Coordinate, out subject))
                {
                    return false;
                }

                var eligiblePiece =
                    (PlaceEligiblePieceKind)context.Input(EligiblePieceParameterName).IntegerValue;
                var anchor =
                    (PlaceDestinationAnchor)context.Input(DestinationAnchorParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(PlaceEligiblePieceKind), eligiblePiece) ||
                    eligiblePiece == PlaceEligiblePieceKind.None ||
                    !Enum.IsDefined(typeof(PlaceDestinationAnchor), anchor) ||
                    anchor == PlaceDestinationAnchor.None)
                {
                    return false;
                }

                var unit = subject.AsRuleObject();
                if (unit.IsDefined && (
                        IsImmuneTo(context, unit, context.EnclosingActivity) ||
                        IsImmuneTo(context, unit, RuleActivity.Place)))
                {
                    return false;
                }

                // Milestone 8.7. Wasp's Magnetic Dagger forbids a unit being "swapped or placed by
                // themselves, or by enemy heroes", so the piece is asked whether this actor may place
                // it at all - here, where the piece is named, rather than at the commit, because a
                // piece the rules will not let this hero move is not one to offer and then refuse.
                if (unit.IsDefined && RestrictionQuery.IsRestricted(
                        unit,
                        RuleActivity.Place,
                        RuleObject.ForHero(context.ActorId),
                        context.Participants,
                        context.Modifications))
                {
                    return false;
                }

                if (anchor == PlaceDestinationAnchor.ActingHero)
                {
                    if (!components.Board.TryGetObjectCoordinate(
                            BoardObjectReference.ForHero(context.ActorId), out measuredFrom))
                    {
                        return false;
                    }
                }
                else
                {
                    measuredFrom = named.Coordinate;
                }

                var exclusion = boundChannel == PrintedValueChannel.None
                    ? PlaceSpawnPointExclusion.None
                    : (PlaceSpawnPointExclusion)context
                        .Input(SpawnPointExclusionParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(PlaceSpawnPointExclusion), exclusion))
                {
                    return false;
                }

                policy = new PlacePolicy(
                    PlaceSourceContainer.Board,
                    eligiblePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                    radius,
                    PlaceChooserKind.ActingHero,
                    context.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? PlaceNoDestinationPolicy.Impossible
                        : PlaceNoDestinationPolicy.Decline,
                    anchor,
                    exclusion);
                return true;
            }

            /// <summary>
            /// This card's printed reach for the declared channel, with p.16's items composed on -
            /// exactly the total <see cref="SyntheticTargetingCapabilities"/>' printed-icon selections
            /// derive, asked here so a card's Place and a card's selection cannot come to measure
            /// different disks. Re-derived on every ask and never captured, for the reason those
            /// selections re-derive theirs.
            /// </summary>
            private static int PrintedReach(EffectCapabilityContext context) =>
                context.CardState.IsAvailable
                    ? GuardsDigital.Core.Rules.CardResolution.RangeValueQuery.Calculate(
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
                            context.CardState.Definitions)).Total
                    : 0;

            /// <summary>
            /// p.18's immunity for the named piece, asked once per activity the question has: the
            /// action this Place sits inside, and p.18's Place itself. Milestone 8.7 adds the second
            /// for the reason milestone 6.4 gave the Push one - an effect family the rules name is
            /// something a unit can be immune to in its own right.
            /// </summary>
            private static bool IsImmuneTo(
                EffectCapabilityContext context, RuleObject unit, RuleActivity activity) =>
                ImmunityQuery.IsImmuneTo(
                    unit,
                    activity,
                    RuleObject.ForHero(context.ActorId),
                    context.Components.Board,
                    context.Participants,
                    context.Modifications);
        }

        /// <summary>
        /// Rulebook p.18's Spawn: "Similar to place, but unlike 'Place', the spawned object must be
        /// taken from the supply and cannot be taken from the board."
        ///
        /// The supply is the declared miniatures not currently standing, derived by the board port on
        /// every call. The destination is not a decision: this capability declares that a unit Spawns
        /// on its own configured Spawn point, which is the invoking rule's destination constraint here,
        /// and p.19 defines a Spawn point as a space "used to spawn minions and heroes". Which unit is
        /// therefore the one decision, so this is one node.
        ///
        /// It implements no eligibility rule of its own. p.18's Respawn adds one - a minion may return
        /// only while its type and colour have more Spawn points than miniatures present in the Battle
        /// Zone - and that specialized rule stays unimplemented and out of this milestone's scope, so
        /// this capability's tighter destination constraint is deliberately not the wider freedom
        /// Respawn would need. Offering only the acting hero's own team is a synthetic bound, like the
        /// adjacency bound Object Push uses: which units a real effect may Spawn comes from its card
        /// text through the reusable targeting contracts, and this milestone has no verified text.
        /// </summary>
        private sealed class SpawnHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SpawnV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[]
                {
                    new EffectOutputBinding("spawned", EffectValueKind.Minion),
                    new EffectOutputBinding("at", EffectValueKind.Coordinate)
                });

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
                    candidates.Select(candidate => EffectValue.Minion(candidate.MinionId)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Minion)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: a
                // Spawn point empty when the choice opened may have been taken before the answer
                // arrived, and a miniature in the supply then may be standing now.
                var chosen = selected[0].MinionId;
                if (!Candidates(context).Any(candidate => candidate.MinionId.Equals(chosen)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var committed = context.Components.Units.CommitValidatedUnitSpawn(chosen);
                if (!committed.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("spawned", EffectValue.Minion(chosen)),
                    new EffectOutcomeFact("at", EffectValue.Space(committed.Destination))
                });
            }

            /// <summary>
            /// Whether the supply holds a miniature this actor could Spawn onto an eligible point. This
            /// is the reading milestone 6.5ac exists for: on the first round every declared miniature
            /// stands, so the supply is empty, and a player who chose this mode before spent the card
            /// on nothing.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The supply miniatures this Spawn may take: the acting hero's own team, whose declared
            /// Spawn point resolves on the selected map and is either empty or covered by a token
            /// alone. p.19 clears a covering token as the Spawn's own first step, so a token-covered
            /// point is legal; any other obstacle is not, because the same page admits no second
            /// occupant in a space.
            /// </summary>
            private static IReadOnlyList<SpawnableUnit> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanSpawnUnits || !components.IsLive ||
                    context.Participants == null)
                {
                    return Array.Empty<SpawnableUnit>();
                }

                var side = context.Participants.GetTeamForHero(context.ActorId).Side;
                var found = new List<SpawnableUnit>();
                foreach (var candidate in components.Units.FindUnitSupply())
                {
                    if (candidate.TeamSide != side ||
                        !components.Units.TryGetSpawnPointCoordinate(candidate.SpawnPoint, out var at))
                    {
                        continue;
                    }

                    if (!components.Tokens.IsEmptyPlayableSpace(at) &&
                        !components.Tokens.PlacedTokens.Any(token => token.Coordinate.Equals(at)))
                    {
                        continue;
                    }

                    found.Add(candidate);
                }

                return found;
            }
        }
    }
}
