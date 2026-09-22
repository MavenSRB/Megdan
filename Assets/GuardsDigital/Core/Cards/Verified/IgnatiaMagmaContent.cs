using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's MAGMA subsystem - milestone 8.75e's deferred half, now composed. Path of
    /// Ashes, Path of Cinders and Path of Flames drop MAGMA tokens: impassable obstacles she leaves in
    /// empty spaces, either along her own straight-line move (the sun face) or scattered inside her
    /// printed Radius (the moon face).
    ///
    /// <para><b>The token.</b> A magma token is an ordinary p.18 obstacle - "tokens ... are obstacles" -
    /// and it is IMPASSABLE, which on this board is the ABSENCE of Min's move-through property rather than
    /// a property of its own: <c>ObstacleQuery.DeclaresMayBeMovedThrough</c> is the one reader of that
    /// clause, and a token that declares it not is one a unit cannot cross. So the definition carries no
    /// standing properties at all - it is a bare obstacle, the way Tali's totem is - and its whole rule
    /// is "something is in this space." Its lifetime is p.19's baseline, <see cref="TokenRemovalCondition
    /// .EndOfRoundCleanup"/> ("Unless the card text says otherwise, tokens are removed at the end of
    /// round"), because none of the three faces says otherwise.</para>
    ///
    /// <para><b>Two placements, mirroring the existing hero-token handlers.</b> The moon face's "Place up
    /// to N magma tokens in radius" reuses milestone 8.37's <c>PlaceHeroTokenHandler</c> exactly as Min's
    /// mines do - one supply Place reaching the card's printed Radius, bound to the magma definition - and
    /// the composition strings N of them together as N optional nodes, which is the shape that handler's
    /// own remarks name for "up to". The sun face's "Place a magma token in each empty space you moved
    /// through, or out of" is the one thing milestone 8.75e reported the engine could not express: a
    /// placement along a direct Move's WAKE. <see cref="PlaceMagmaWakeHandler"/> is that capability - it
    /// reads the move's two endpoints, walks p.19's straight line between them, and drops a magma token in
    /// every empty space from the origin (moved out of) up to but not including the destination (moved
    /// through), leaving the space the actor now stands on untouched.</para>
    ///
    /// <para><b>Nothing here is registered by this file.</b> Ignatia sits behind milestone 7.9's gate
    /// until her dealing block flips it, so these handlers reach the build through
    /// <see cref="Handlers"/> - which the coordinator folds into
    /// <c>IgnatiaComponentContent.DeferredHandlers</c> at the dealing - and the token reaches it through
    /// <see cref="MagmaTokens"/>. The structural probe builds a catalog from
    /// <see cref="Handlers"/> concatenated with that deferred list and validates the three composed
    /// bodies offline.</para>
    /// </summary>
    public static class IgnatiaMagmaContent
    {
        /// <summary>
        /// The physical magma supply. USER-SETTLED 2026-09-22: "Magma token count is 3". Her faces
        /// print "up to N" placements (Path of Flames' "up to 4"), which p.18's short-supply step
        /// bounds to what is available - so a card printing "up to 4" places at most these 3 when the
        /// pile is drained, and the tokens return to supply at end of round (their cleanup).
        /// </summary>
        public const int MagmaSupplyCount = 3;

        /// <summary>The one physical pile every magma placement of hers draws from.</summary>
        public const string MagmaSupplyGroupId = "ignatia-magma-tokens";

        public static TokenDefinitionId Magma { get; } = new TokenDefinitionId("ignatia-magma");

        /// <summary>
        /// The magma token: an impassable p.18 obstacle spawned from her supply. It declares no standing
        /// properties, which is exactly what makes it impassable - it carries no
        /// <c>MayBeMovedThrough</c> clause, so <c>ObstacleQuery</c> answers that a unit cannot cross it,
        /// and no aura, screen or save, because the printed faces give it none. p.19's end-of-round
        /// cleanup is its lifetime, none of the three faces printing otherwise.
        /// </summary>
        public static TokenDefinition MagmaTokenDefinition { get; } = new TokenDefinition(
            Magma,
            1,
            "Magma",
            MagmaSupplyCount,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.EndOfRoundCleanup,
            MagmaSupplyGroupId);

        /// <summary>"Place up to N magma tokens in radius" - one supply Place per node, reaching the card's Radius.</summary>
        public static EffectCapabilityId PlaceMagmaInRadius { get; } =
            new EffectCapabilityId("ignatia-place-magma-in-radius");

        /// <summary>"Place a magma token in each empty space you moved through, or out of" - the wake trail.</summary>
        public static EffectCapabilityId PlaceMagmaWake { get; } =
            new EffectCapabilityId("ignatia-place-magma-wake");

        public static EffectCapabilityReference PlaceMagmaInRadiusV1 =>
            new EffectCapabilityReference(PlaceMagmaInRadius, 1);

        public static EffectCapabilityReference PlaceMagmaWakeV1 =>
            new EffectCapabilityReference(PlaceMagmaWake, 1);

        /// <summary>The move-origin the wake reads - the actor's space before her Move, "moved out of".</summary>
        public const string WakeOriginParameterName = "origin";

        /// <summary>The move-destination the wake reads - where the Move left her, the one space kept clear.</summary>
        public const string WakeDestinationParameterName = "destination";

        /// <summary>How many magma tokens the wake actually dropped, published for a test or a rider.</summary>
        public const string WakePlacedCountOutput = "placedCount";

        /// <summary>Her one magma token definition, for the content fixtures and the dealing's registration.</summary>
        public static IEnumerable<TokenDefinition> MagmaTokens() => new[] { MagmaTokenDefinition };

        /// <summary>
        /// The two magma placement handlers, bound to the magma definition. The radius Place is milestone
        /// 8.37's <c>PlaceHeroTokenHandler</c> reused exactly as Min's mines reuse it; the wake Place is
        /// this file's own. The coordinator folds these into
        /// <c>IgnatiaComponentContent.DeferredHandlers</c> at the dealing.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceMagmaInRadiusV1, MagmaTokenDefinition, PrintedValueChannel.Radius),
                new PlaceMagmaWakeHandler(PlaceMagmaWakeV1, MagmaTokenDefinition)
            };

        /// <summary>
        /// "Place a magma token in each empty space you moved through, or out of." The capability the
        /// engine had no form of before her - a placement along a direct Move's wake.
        ///
        /// <para>It runs after the Move has committed, reads the two endpoints the composition binds -
        /// the actor's space before the Move (<see cref="WakeOriginParameterName"/>) and where the Move
        /// left her (<see cref="WakeDestinationParameterName"/>) - and, because a direct Move is p.11's
        /// straight line, walks p.19's file between them. It drops a magma token in every empty space
        /// from the origin up to but NOT including the destination: the origin is the space "moved out
        /// of", the spaces between are the ones "moved through", and the destination is where she now
        /// stands, which no obstacle may share.</para>
        ///
        /// <para><b>It asks no question.</b> "Place a magma token in EACH empty space" is p.10's
        /// mandatory automatic step, so the actor chooses nothing - the spaces are determined by the
        /// Move she already made - and the node applies at once, placing as many tokens as the wake and
        /// the supply allow. A space that is off the map, occupied, or made an obstacle by an overlay is
        /// simply not empty and takes none, which <see cref="PlaceOperation.PlaceFromSupply"/> answers by
        /// refusing that one coordinate rather than the whole step.</para>
        /// </summary>
        private sealed class PlaceMagmaWakeHandler : IEffectCapabilityHandler
        {
            private readonly TokenDefinition definition;

            public PlaceMagmaWakeHandler(EffectCapabilityReference reference, TokenDefinition definition)
            {
                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(WakeOriginParameterName, EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(
                        WakeDestinationParameterName, EffectValueKind.Coordinate)
                },
                new[]
                {
                    new EffectOutputBinding(WakePlacedCountOutput, EffectValueKind.Integer)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context?.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var originInput = context.Input(WakeOriginParameterName);
                var destinationInput = context.Input(WakeDestinationParameterName);
                if (originInput.Kind != EffectValueKind.Coordinate ||
                    destinationInput.Kind != EffectValueKind.Coordinate)
                {
                    // A wake with no resolved pair of endpoints is a completed step that placed nothing
                    // rather than a refusal: the Move it trails may have declined its own optional
                    // distance, in which case there is no space she moved out of at all.
                    return Applied(0);
                }

                var origin = originInput.Coordinate;
                var destination = destinationInput.Coordinate;

                var placed = 0;
                foreach (var space in WakeSpaces(origin, destination))
                {
                    if (TryPlaceMagma(context, components, origin, destination, space))
                    {
                        placed++;
                    }
                }

                return Applied(placed);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException(
                    "ignatia-place-magma-wake@1 places every empty wake space at once and asks nothing.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context?.Components;
                // A mandatory automatic placement always completes - placing zero tokens is still a
                // discharged step - so the arm it closes is judged performable whenever the board is
                // live, and never blocked on how far a preceding optional Move will turn out to go.
                return components != null && components.IsLive
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            private static EffectExecutionResult Applied(int placedCount) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(WakePlacedCountOutput, EffectValue.Integer(placedCount))
                });

            /// <summary>
            /// The spaces a straight-line Move from <paramref name="origin"/> to
            /// <paramref name="destination"/> passed through or left: the origin itself and every space
            /// strictly between, excluding the destination she now stands on. A Move that went nowhere,
            /// or a pair that is not on one file, leaves no wake.
            /// </summary>
            private static IEnumerable<HexCoordinate> WakeSpaces(
                HexCoordinate origin, HexCoordinate destination)
            {
                if (origin.Equals(destination) ||
                    !StraightLineQuery.TryGetDirection(
                        origin, destination, out var direction, out var distance))
                {
                    yield break;
                }

                for (var step = 0; step < distance; step++)
                {
                    yield return StraightLineQuery.Step(origin, direction, step);
                }
            }

            /// <summary>
            /// One magma token from the supply onto <paramref name="space"/>, or nothing where that
            /// space is not an empty playable one within the wake or the pile is spent.
            /// <see cref="PlaceOperation.PlaceFromSupply"/> re-derives the admitted destinations from the
            /// live board and refuses a coordinate that is off the map, occupied, overlaid, or beyond the
            /// supply, so the caller need only offer each wake space and count what lands.
            /// </summary>
            private bool TryPlaceMagma(
                EffectCapabilityContext context,
                MatchComponents components,
                HexCoordinate origin,
                HexCoordinate destination,
                HexCoordinate space)
            {
                var reach = HexDistanceQuery.Calculate(origin, destination);
                if (reach < 1)
                {
                    return false;
                }

                var policy = new PlacePolicy(
                    PlaceSourceContainer.Supply,
                    PlaceEligiblePieceKind.Token,
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                    reach,
                    PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    // The piece comes from the supply and stands nowhere, so the reach is measured from
                    // the space she moved out of - the origin passed below - rather than from where she
                    // is now.
                    PlaceDestinationAnchor.ActingHero);

                var token = new PlacedToken(
                    // Deterministic under replay and unique across the several tokens this one node
                    // places: the space's own coordinate distinguishes each drop, since a wake node runs
                    // once and mints many.
                    new TokenInstanceId(
                        $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                        $":{definition.Id.Value}" +
                        $":r{context.RoundIndex}t{context.TurnIndex}" +
                        $":{space.Q}:{space.R}"),
                    definition,
                    context.ActorId,
                    context.ControllerId,
                    context.SourceCardInstanceId,
                    space);

                return PlaceOperation.PlaceFromSupply(
                    policy,
                    components.Tokens,
                    components.Catalog,
                    token,
                    origin,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications).Accepted;
            }
        }
    }
}
