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
                new GiveMarkerHandler(),
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

                var reference = SyntheticComponentDefinitions.HazardV3;
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
                var token = SameTypeTokens(components, SyntheticComponentDefinitions.HazardV3)
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

                var reference = SyntheticComponentDefinitions.HazardV3;
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
                PlaceNoDestinationPolicy.Impossible);

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

                var reference = SyntheticComponentDefinitions.HazardV3;
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

                var reference = SyntheticComponentDefinitions.HazardV3;
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
                    Array.Empty<HexCoordinate>());
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

                var reference = SyntheticComponentDefinitions.HazardV3;
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
                        Policy, components.Tokens, origin, Array.Empty<HexCoordinate>())
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
        private sealed class GiveMarkerHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => GiveMarkerV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
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

                var reference = SyntheticComponentDefinitions.FocusV1;
                if (components.Catalog.CountAvailableMarkers(
                        reference, components.Markers.Attachments) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
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
            private static bool CanGive(EffectCapabilityContext context, out EffectValue[] candidates)
            {
                candidates = Array.Empty<EffectValue>();
                var components = context.Components;
                if (components == null || !components.IsLive || context.Participants == null)
                {
                    return false;
                }

                if (components.Catalog.CountAvailableMarkers(
                        SyntheticComponentDefinitions.FocusV1, components.Markers.Attachments) < 1)
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
        /// </summary>
        private sealed class ReturnMarkerHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ReturnMarkerV1;

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

            /// <summary>The attachment this card would take back, or null when it gave none.</summary>
            private static MarkerAttachment FindAttachment(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return null;
                }

                return components.Markers
                    .GivenBy(context.SourceCardInstanceId)
                    .OrderBy(candidate => candidate.Id.Value, StringComparer.Ordinal)
                    .FirstOrDefault();
            }
        }
    }
}
