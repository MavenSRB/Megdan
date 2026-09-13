using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.2's token and marker capabilities: the first handlers in the engine that mutate
    /// authoritative board and component state.
    ///
    /// Each is one node with at most one decision, which is why the rulebook's short-supply rule on p.18
    /// - "first remove up to that number of tokens of the same type from the board, if able" and only
    /// then place - is two capabilities rather than one that pauses twice. A handler is stateless and
    /// cannot tell a second resumption from a first, so a second decision belongs to a second node. The
    /// effect definition orders them, which is exactly the job milestone 6.2a's frame exists to do.
    ///
    /// They are registered into the same catalog as 6.2a's handlers, because a match has one capability
    /// contract set and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticComponentCapabilities
    {
        public static EffectCapabilityId ReclaimToken { get; } =
            new EffectCapabilityId("synthetic-reclaim-token");

        public static EffectCapabilityId PlaceToken { get; } =
            new EffectCapabilityId("synthetic-place-token");

        public static EffectCapabilityId GiveMarker { get; } =
            new EffectCapabilityId("synthetic-give-marker");

        public static EffectCapabilityId ReturnMarker { get; } =
            new EffectCapabilityId("synthetic-return-marker");

        public static EffectCapabilityReference ReclaimTokenV1 =>
            new EffectCapabilityReference(ReclaimToken, 1);

        public static EffectCapabilityReference PlaceTokenV1 =>
            new EffectCapabilityReference(PlaceToken, 1);

        public static EffectCapabilityReference GiveMarkerV1 =>
            new EffectCapabilityReference(GiveMarker, 1);

        public static EffectCapabilityReference ReturnMarkerV1 =>
            new EffectCapabilityReference(ReturnMarker, 1);

        /// <summary>The handlers this milestone contributes to the shared capability catalog.</summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ReclaimTokenHandler(),
                new PlaceTokenHandler(),
                new MarkerGivingHandler(
                    GiveMarkerV1, SyntheticComponentDefinitions.FocusDefinition),
                new ReturnMarkerHandler()
            };

        /// <summary>
        /// The rulebook's p.18 short-supply step, and only that step. It declines when the supply can
        /// already cover a placement, because a legal "nothing needed to happen" is
        /// <see cref="EffectExecutionResultKind.DeclinedOptional"/> and not a failure; it reports
        /// impossible when the supply is empty and no same-type token is on the board to take back,
        /// which under the `if able` policy the definition gives it means the step is simply skipped.
        ///
        /// A candidate is the space a token stands on rather than a token instance ID, for two reasons:
        /// a space is the disclosed fact a player actually picks, and one obstacle per space (p.19) makes
        /// it an unambiguous identity without widening <see cref="EffectValue"/>.
        /// </summary>
        private sealed class ReclaimTokenHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ReclaimTokenV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("reclaimed", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reference = SyntheticComponentDefinitions.HazardV4;
                if (components.Catalog.CountAvailableTokens(reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

                var candidates = SameTypeTokens(components, reference)
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
                var token = SameTypeTokens(components, SyntheticComponentDefinitions.HazardV4)
                    .FirstOrDefault(candidate => candidate.Coordinate.Equals(coordinate));
                if (token == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reclaimed = components.Tokens.CommitValidatedTokenReclamation(token.Id);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("reclaimed", EffectValue.Space(reclaimed.Coordinate))
                });
            }

            /// <summary>
            /// The same three readings <see cref="Execute"/> makes, in the same order and without the
            /// choice it would open: no live components, a supply that already covers a placement, and
            /// then whether any same-type token stands on the board to take back.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var reference = SyntheticComponentDefinitions.HazardV4;
                if (components.Catalog.CountAvailableTokens(reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResultKind.DeclinedOptional;
                }

                return SameTypeTokens(components, reference).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            private static IEnumerable<PlacedToken> SameTypeTokens(
                MatchComponents components,
                TokenDefinitionReference reference) =>
                components.Tokens.PlacedTokens
                    .Where(token => token.Definition.Reference.Equals(reference))
                    .OrderBy(token => token.Coordinate.Q)
                    .ThenBy(token => token.Coordinate.R);
        }

        /// <summary>
        /// Places one token from the supply into an empty space (rulebook p.18).
        ///
        /// The candidate spaces are the actor's empty neighbours. That bound is synthetic, not a rule:
        /// which spaces a real effect may target comes from its card text through the reusable targeting
        /// contracts milestone 6.8 owns, and this milestone has no verified text to read. What is not
        /// synthetic is that the space must be empty and playable, which p.18 states outright.
        ///
        /// Since milestone 6.5 it is the supply arm of the one shared <see cref="PlaceOperation"/>
        /// rather than a second implementation of the same p.18 sentence: it declares the same five
        /// things every Place declares - supply source, token piece, a radius-1 destination question
        /// measured from the actor, the acting hero choosing, and impossible without a destination -
        /// and the operation validates and commits. Its observable behaviour is unchanged, because
        /// empty playable spaces within radius 1 of the actor, excluding the actor's own space, is
        /// exactly the neighbour set it derived before.
        /// </summary>
        private sealed class PlaceTokenHandler : IEffectCapabilityHandler
        {
            /// <summary>
            /// This capability's declared Place. Constant rather than derived, because none of the
            /// five depends on the node: a token placement is always a supply Place of a token into
            /// an adjacent empty space, and p.18's short-supply step is the separate reclamation node
            /// beside it rather than a policy of this one.
            /// </summary>
            private static readonly PlacePolicy Policy = new PlacePolicy(
                PlaceSourceContainer.Supply,
                PlaceEligiblePieceKind.Token,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                1,
                PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible,
                // The piece comes from the supply and stands nowhere, so the only space this radius
                // can be measured from is the acting hero's - which is what the handler has always
                // passed and now declares.
                PlaceDestinationAnchor.ActingHero);

            public EffectCapabilityReference Reference => PlaceTokenV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reference = SyntheticComponentDefinitions.HazardV4;
                if (components.Catalog.CountAvailableTokens(reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var candidates = LegalSpaces(context, components)
                    .Select(EffectValue.Space)
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

                var reference = SyntheticComponentDefinitions.HazardV4;
                var coordinate = selected[0].Coordinate;
                if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var origin))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The operation re-derives the admitted spaces and re-asks the supply rather than
                // trusting the paused choice: a space legal when the choice opened may have been taken
                // before the answer arrived, and the last copy may have been placed elsewhere.
                var outcome = PlaceOperation.PlaceFromSupply(
                    Policy,
                    components.Tokens,
                    components.Catalog,
                    new PlacedToken(
                        // Deterministic under replay - the same command prefix produces the same
                        // identity - and unique across placements, because the round and turn
                        // distinguish a token from a later one that happens to occupy the space the
                        // first was reclaimed from.
                        new TokenInstanceId(
                            $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                            $":r{context.RoundIndex}t{context.TurnIndex}:{coordinate.Q}:{coordinate.R}"),
                        components.Catalog.LoadToken(reference),
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
                    new EffectOutcomeFact("placed", EffectValue.Space(outcome.Destination))
                });
            }

            /// <summary>
            /// The supply reading and the destination question <see cref="Execute"/> asks, without the
            /// choice it would open. The destination query reads the board and commits nothing.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var reference = SyntheticComponentDefinitions.HazardV4;
                if (components.Catalog.CountAvailableTokens(reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return LegalSpaces(context, components).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            private static IEnumerable<HexCoordinate> LegalSpaces(
                EffectCapabilityContext context,
                MatchComponents components) =>
                components.Tokens.TryGetHeroCoordinate(context.ActorId, out var origin)
                    ? PlaceDestinationQuery.FindDestinations(
                        Policy, components.Tokens, origin, Array.Empty<HexCoordinate>(),
                        context.Modifications)
                    : Array.Empty<HexCoordinate>();
        }

        /// <summary>
        /// Gives a marker from the supply to a chosen hero (rulebook p.18: markers "are usually given to
        /// other heroes as part of the card's effect").
        ///
        /// Who holds it is the decision, so this is the milestone's marker-attachment choice. Every hero
        /// in the match is a candidate, including the actor: "usually" describes what cards tend to say,
        /// not a restriction the engine may impose on text it has not read.
        ///
        /// The attachment's identity comes from <see cref="MarkerInstanceIdentity"/> since milestone
        /// 6.8h, so two activations of one card give two attachments that can be told apart.
        /// </summary>
        public sealed class MarkerGivingHandler : IEffectCapabilityHandler
        {
            /// <summary>The hero a card that already chose one binds here. Milestone 8.23.</summary>
            public const string HolderParameterName = "holder";

            /// <summary>
            /// BA-D1's arm, and milestone 8.40b. Optional and default-off: a node that binds
            /// <see cref="EffectValue.Boolean"/> true says that when the supply is empty because the
            /// one copy is already on the board, giving it MOVES it rather than reporting impossible -
            /// which is what happens at a physical table, and what Bain's Dead or Alive needs from its
            /// second turn onwards with a supply of one.
            ///
            /// <para>Every node written before this parameter binds nothing and is unaffected, and the
            /// move arm still refuses wherever the marker that would move is ambiguous, so Tigerclaw's
            /// six-copy Poison keeps today's refusal even if a later face were to bind it.</para>
            /// </summary>
            public const string MovesWhenSupplyIsEmptyParameterName = "movesWhenSupplyIsEmpty";

            private readonly MarkerDefinition definition;

            public MarkerGivingHandler(
                EffectCapabilityReference reference, MarkerDefinition definition)
            {
                if (!reference.IsDefined)
                {
                    throw new ArgumentException(
                        "A marker-giving capability requires a defined reference.", nameof(reference));
                }

                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            /// <summary>
            /// Milestone 8.23 adds the optional holder, and it defaults to what milestone 6.2's
            /// consumer already meant by declaring nothing: this node asks who receives the marker.
            /// A card whose own sentence bounds the recipient - Tigerclaw's "Give a hero <b>in
            /// range</b> a Poison marker" - binds the hero its selection chose, and this node then
            /// asks nothing, because asking again would let a player name a hero the card cannot
            /// reach.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        HolderParameterName, EffectValueKind.Hero, isRequired: false),
                    new EffectCapabilityParameter(
                        MovesWhenSupplyIsEmptyParameterName,
                        EffectValueKind.Boolean,
                        isRequired: false)
                },
                new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var named = context.Input(HolderParameterName);
                if (named.Kind == EffectValueKind.Hero && named.HeroId.IsDefined)
                {
                    return Resume(context, new[] { named });
                }

                if (!CanGive(context, out var candidates))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(
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

                var holder = selected[0].HeroId;
                if (!holder.IsDefined)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var given = new[] { new EffectOutcomeFact("holder", EffectValue.Hero(holder)) };
                var instanceId = MarkerInstanceIdentity.Mint(context, holder);
                if (components.Markers.Attachments.Any(
                        attachment => attachment.Id.Equals(instanceId)))
                {
                    // This activation's marker is already attached, so re-executing the node inside
                    // its own resolution is a legal no-op rather than a second give - the same shape
                    // the Active-effect creators use. It must precede the supply test, because what
                    // the supply is short of at this point is this activation's own copy.
                    return EffectExecutionResult.Applied(given);
                }

                var reference = definition.Reference;
                if (components.Catalog.CountAvailableMarkers(
                        reference, components.Markers.Attachments) < 1)
                {
                    // BA-D1's arm, milestone 8.40b. The supply is empty because the copy this node
                    // would give is already on the board, so the give moves that copy - keeping its
                    // instance identity, because it is the same physical piece changing hands. It is
                    // still a give, which is what makes the Ultimate's "after YOU give" read it and
                    // the transfer beside it not.
                    var held = MovesWhenSupplyIsEmpty(context)
                        ? TheOneAttachmentOf(definition, components)
                        : null;
                    if (held == null)
                    {
                        return EffectExecutionResult.ImpossibleMandatory();
                    }

                    components.Markers.Transfer(held.Id, holder);
                    return EffectExecutionResult.Applied(given);
                }

                components.Markers.Give(new MarkerAttachment(
                    instanceId,
                    components.Catalog.LoadMarker(reference),
                    context.ActorId,
                    holder,
                    context.SourceCardInstanceId,
                    context.ActionDefinitionId));

                return EffectExecutionResult.Applied(given);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanGive(context, out _)
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// Whether a marker can be given at all, and to whom. Extracted so the probe reads the
            /// supply and the hero set exactly as the execution does; neither reading mints an
            /// attachment identity, which is what the give itself does on resume.
            /// </summary>
            private bool CanGive(EffectCapabilityContext context, out EffectValue[] candidates)
            {
                candidates = Array.Empty<EffectValue>();
                var components = context.Components;
                if (components == null || !components.IsLive || context.Participants == null)
                {
                    return false;
                }

                if (components.Catalog.CountAvailableMarkers(
                        definition.Reference, components.Markers.Attachments) < 1 &&
                    !(MovesWhenSupplyIsEmpty(context) &&
                        TheOneAttachmentOf(definition, components) != null))
                {
                    return false;
                }

                candidates = context.Participants.Heroes
                    .Select(hero => hero.Id)
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Select(EffectValue.Hero)
                    .ToArray();
                return candidates.Length > 0;
            }

            private static bool MovesWhenSupplyIsEmpty(EffectCapabilityContext context)
            {
                var declared = context.Input(MovesWhenSupplyIsEmptyParameterName);
                return declared.Kind == EffectValueKind.Boolean && declared.BooleanValue;
            }
        }

        /// <summary>
        /// The one attachment of a declared definition currently on the board, or <c>null</c> where
        /// there is none - or where there is more than one. Milestone 8.40b, and the single reading the
        /// three operations that say "<b>the</b> marker" share.
        ///
        /// <para><b>Ambiguity refuses rather than picks.</b> Every printed sentence these operations
        /// serve is singular - "the marker", "retrieve the marker", "that hero gives the marker to
        /// you" - because the supply behind them is one. Choosing among several would state a rule no
        /// face prints and open no decision for the player to make, so a definition with two copies out
        /// is declined here and the node reports impossible, which is exactly what a six-copy supply
        /// already did before this milestone.</para>
        /// </summary>
        private static MarkerAttachment TheOneAttachmentOf(
            MarkerDefinition definition, MatchComponents components)
        {
            var held = components.Markers.OfDefinition(definition.Reference).Take(2).ToArray();
            return held.Length == 1 ? held[0] : null;
        }

        /// <summary>
        /// Moves one attached marker of a declared definition from the hero holding it to another hero,
        /// as ONE operation. Milestone 8.40b, for Bain's Close Call: "If a hero in play has a bounty
        /// marker, block the attack and <b>that hero gives the marker to you</b>."
        ///
        /// <para><b>It is deliberately not a give, and not a return followed by one.</b> His Ultimate
        /// fires "after <em>you</em> give a hero the bounty marker" (BA-D5), and Close Call's sentence
        /// is another hero giving it to him, so a composition that ran a give would fire a permanent
        /// ability on a clause that never says he gave anything. Running one would also mint a second
        /// <see cref="MarkerInstanceIdentity"/> for a piece that never left the table, and that identity
        /// is what a restore rebinds against.</para>
        ///
        /// <para>Like the give, who receives it is the node's decision unless the card's own sentence
        /// already bound a hero - Close Call's is the defender - and unlike the give it takes nothing
        /// from the supply, because what it moves is already out of it.</para>
        /// </summary>
        public sealed class MarkerTransferHandler : IEffectCapabilityHandler
        {
            /// <summary>The hero the marker moves TO, where the card's own sentence names one.</summary>
            public const string HolderParameterName = "holder";

            /// <summary>The hero it moved FROM, published beside the hero it moved to.</summary>
            public const string PreviousHolderOutputName = "previousHolder";

            private readonly MarkerDefinition definition;

            public MarkerTransferHandler(
                EffectCapabilityReference reference, MarkerDefinition definition)
            {
                if (!reference.IsDefined)
                {
                    throw new ArgumentException(
                        "A marker-transfer capability requires a defined reference.", nameof(reference));
                }

                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        HolderParameterName, EffectValueKind.Hero, isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding("holder", EffectValueKind.Hero),
                    new EffectOutputBinding(PreviousHolderOutputName, EffectValueKind.Hero)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var named = context.Input(HolderParameterName);
                if (named.Kind == EffectValueKind.Hero && named.HeroId.IsDefined)
                {
                    return Resume(context, new[] { named });
                }

                if (!CanTransfer(context, out var candidates))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(
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

                var holder = selected[0].HeroId;
                if (!holder.IsDefined)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-read rather than trust the paused choice: the marker this node asked about may
                // have been returned before the answer arrived, which is the same discipline the token
                // placement takes with a space.
                var moving = TheOneAttachmentOf(definition, components);
                if (moving == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var previousHolder = moving.HolderHeroId;
                components.Markers.Transfer(moving.Id, holder);

                var facts = new List<EffectOutcomeFact>();
                if (context.Publishes("holder"))
                {
                    facts.Add(new EffectOutcomeFact("holder", EffectValue.Hero(holder)));
                }

                if (context.Publishes(PreviousHolderOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        PreviousHolderOutputName, EffectValue.Hero(previousHolder)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            /// <summary>
            /// What <see cref="Execute"/> would do, without doing it: a bound recipient applies, an
            /// unbound one asks, and a definition with no single attachment out cannot do either.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                if (!CanTransfer(context, out _))
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var named = context.Input(HolderParameterName);
                return named.Kind == EffectValueKind.Hero && named.HeroId.IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
            }

            /// <summary>
            /// Whether a marker of this definition can move at all, and to whom. Extracted so the probe
            /// reads the register and the hero set exactly as the execution does.
            /// </summary>
            private bool CanTransfer(EffectCapabilityContext context, out EffectValue[] candidates)
            {
                candidates = Array.Empty<EffectValue>();
                var components = context.Components;
                if (components == null || !components.IsLive || context.Participants == null)
                {
                    return false;
                }

                if (TheOneAttachmentOf(definition, components) == null)
                {
                    return false;
                }

                candidates = context.Participants.Heroes
                    .Select(hero => hero.Id)
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Select(EffectValue.Hero)
                    .ToArray();
                return candidates.Length > 0;
            }
        }

        /// <summary>
        /// Detaches a marker this source card gave and returns it to the supply.
        ///
        /// Deliberately not a choice. A return is a transition, not a decision - the rulebook's returns
        /// on p.8 and p.13 happen to every applicable marker rather than to one a player picks - so this
        /// handler applies or reports that there was nothing to return.
        ///
        /// It is the return *operation*, not a cleanup trigger. Wiring it to end of round belongs to
        /// milestone 6.12, and the defeat return on p.13 is unwired for the same reason; the attachment's
        /// <see cref="MarkerReturnCondition"/> is what records that both are still owed.
        ///
        /// <para><b>Milestone 8.40b gives it a second reading, bound to a definition rather than to a
        /// giving card.</b> Bain's Narrow Escape prints "retrieve the marker" - the rulebook's own word
        /// for returning one to the supply, p.8's "Return all markers back into the supply" and p.13's
        /// "Any markers they had are returned" - about a marker that face never gave. An instance
        /// constructed with a definition therefore returns the one attachment of THAT definition
        /// wherever it sits; the parameterless instance milestone 6.2 registered is unchanged and still
        /// returns what its own source card gave. Which reading an instance takes is fixed by its
        /// constructor and can never be named by a payload, exactly as the give's definition is.</para>
        /// </summary>
        public sealed class ReturnMarkerHandler : IEffectCapabilityHandler
        {
            private readonly MarkerDefinition definition;

            /// <summary>Milestone 6.2's reading: the marker this source card gave.</summary>
            public ReturnMarkerHandler()
            {
                Reference = ReturnMarkerV1;
            }

            /// <summary>
            /// Milestone 8.40b's reading: the one marker of this definition on the board, whatever card
            /// gave it.
            /// </summary>
            public ReturnMarkerHandler(
                EffectCapabilityReference reference, MarkerDefinition definition)
            {
                if (!reference.IsDefined)
                {
                    throw new ArgumentException(
                        "A marker-return capability requires a defined reference.", nameof(reference));
                }

                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("returned", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var attachment = FindAttachment(context);
                if (attachment == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var returned = context.Components.Markers.Return(attachment.Id);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("returned", EffectValue.Hero(returned.HolderHeroId))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// This is the capability the probe exists for. Its execution applies rather than pausing,
            /// and applying detaches a marker, so asking whether the node can do anything by running it
            /// would return the very marker the question was about.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                FindAttachment(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            /// <summary>
            /// The attachment this node would take back: the one of its declared definition wherever
            /// it sits, or - for milestone 6.2's parameterless instance, which declares none - the
            /// marker this source card gave. Null when there is nothing to return.
            /// </summary>
            private MarkerAttachment FindAttachment(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return null;
                }

                return definition == null
                    ? components.Markers
                        .GivenBy(context.SourceCardInstanceId)
                        .OrderBy(candidate => candidate.Id.Value, StringComparer.Ordinal)
                        .FirstOrDefault()
                    : TheOneAttachmentOf(definition, components);
            }
        }
    }
}
