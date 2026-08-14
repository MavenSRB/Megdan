using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
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
        /// </summary>
        private sealed class PlaceTokenHandler : IEffectCapabilityHandler
        {
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
                // Re-checked against current state rather than trusted from the paused choice: a space
                // legal when the choice opened may have been taken before the answer arrived.
                if (!components.Tokens.IsEmptyPlayableSpace(coordinate) ||
                    components.Catalog.CountAvailableTokens(reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                components.Tokens.CommitValidatedTokenPlacement(new PlacedToken(
                    // Deterministic under replay - the same command prefix produces the same identity -
                    // and unique across placements, because the round and turn distinguish a token from
                    // a later one that happens to occupy the space the first was reclaimed from.
                    new TokenInstanceId(
                        $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                        $":r{context.RoundIndex}t{context.TurnIndex}:{coordinate.Q}:{coordinate.R}"),
                    components.Catalog.LoadToken(reference),
                    context.ActorId,
                    context.ControllerId,
                    context.SourceCardInstanceId,
                    coordinate));

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("placed", EffectValue.Space(coordinate))
                });
            }

            private static IEnumerable<HexCoordinate> LegalSpaces(
                EffectCapabilityContext context,
                MatchComponents components)
            {
                if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var origin))
                {
                    return Array.Empty<HexCoordinate>();
                }

                return origin.GetNeighbors()
                    .Where(components.Tokens.IsEmptyPlayableSpace)
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }
        }

        /// <summary>
        /// Gives a marker from the supply to a chosen hero (rulebook p.18: markers "are usually given to
        /// other heroes as part of the card's effect").
        ///
        /// Who holds it is the decision, so this is the milestone's marker-attachment choice. Every hero
        /// in the match is a candidate, including the actor: "usually" describes what cards tend to say,
        /// not a restriction the engine may impose on text it has not read.
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
                var components = context.Components;
                if (components == null || !components.IsLive || context.Participants == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reference = SyntheticComponentDefinitions.FocusV1;
                if (components.Catalog.CountAvailableMarkers(
                        reference, components.Markers.Attachments) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var candidates = context.Participants.Heroes
                    .Select(hero => hero.Id)
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Select(EffectValue.Hero)
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

                var reference = SyntheticComponentDefinitions.FocusV1;
                var holder = selected[0].HeroId;
                if (!holder.IsDefined ||
                    components.Catalog.CountAvailableMarkers(
                        reference, components.Markers.Attachments) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                components.Markers.Give(new MarkerAttachment(
                    new MarkerInstanceId(
                        $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}:{holder.Value}"),
                    components.Catalog.LoadMarker(reference),
                    context.ActorId,
                    holder,
                    context.SourceCardInstanceId,
                    context.ActionDefinitionId));

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("holder", EffectValue.Hero(holder))
                });
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
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var attachment = components.Markers
                    .GivenBy(context.SourceCardInstanceId)
                    .OrderBy(candidate => candidate.Id.Value, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (attachment == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var returned = components.Markers.Return(attachment.Id);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("returned", EffectValue.Hero(returned.HolderHeroId))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
        }
    }
}
