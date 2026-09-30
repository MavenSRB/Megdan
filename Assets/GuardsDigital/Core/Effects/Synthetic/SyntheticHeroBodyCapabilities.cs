using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.21d. p.19's Spawn of one more body of the acting hero - the first card text that
    /// adds a body to a hero standing on several spaces (the many-bodies model, 9.21a).
    ///
    /// <para><b>What it does.</b> It takes the lowest body left in the actor's declared supply
    /// (<see cref="IUnitSpawnAuthority.FindHeroBodySupply"/>; RZ-Q1, no reclaim - p.19's Spawn "must be
    /// taken from the supply and cannot be taken from the board") and puts it into an empty space the
    /// actor chooses within the action's printed Radius, raised by Radius items (R1), measured from
    /// the acting body (RZ-Q4: <see cref="ITokenPlacementAuthority.TryGetHeroCoordinate"/> answers the
    /// body settled for the action). The body is a <see cref="Units.Heroes.HeroOccupancy"/> of the same
    /// hero, so presence, targeting and a Defeat of any body (RZ-Q3) already see it.</para>
    ///
    /// <para><b>One body per node,</b> for the reason the token placement is one token per node: "up
    /// to 3" is the composition's three optional nodes, each asking again once the previous body
    /// stands. A node that finds the supply empty or no space in reach is impossible, which an
    /// optional node records as a legal empty step (W5: "up to N" admits 0).</para>
    ///
    /// <para>Nothing here names a card or a hero; nothing registers it until the hero whose content
    /// composes it is dealt.</para>
    /// </summary>
    public static class SyntheticHeroBodyCapabilities
    {
        /// <summary>The space the spawned body stands on, for a following node to read.</summary>
        public const string SpawnedOutput = "spawned";

        /// <summary>One handler under the reference the composing content names.</summary>
        public static IEffectCapabilityHandler SpawnActorBodyHandler(EffectCapabilityReference reference) =>
            new SpawnActorBodyInRadiusHandler(reference);

        private sealed class SpawnActorBodyInRadiusHandler : IEffectCapabilityHandler
        {
            public SpawnActorBodyInRadiusHandler(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                null, new[] { new EffectOutputBinding(SpawnedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = LegalSpaces(context).Select(EffectValue.Space).ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived rather than trusted: a space legal when the question opened may have been
                // taken before the answer arrived.
                var destination = selected[0].Coordinate;
                if (!LegalSpaces(context).Contains(destination) ||
                    !context.Components.Units.CommitValidatedHeroBodySpawn(
                        context.ActorId, destination, out var spawned))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SpawnedOutput, EffectValue.Space(spawned.Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                LegalSpaces(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The empty spaces within the printed Radius (with items) of the acting body, or none when
            /// no body is left in the supply. The one derivation Execute, Resume and Anticipate ask.
            /// </summary>
            private static IReadOnlyList<HexCoordinate> LegalSpaces(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || !components.CanSpawnUnits ||
                    components.Units.FindHeroBodySupply(context.ActorId).Count == 0 ||
                    !components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actingBody))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications).Total;
                if (radius < 1)
                {
                    return Array.Empty<HexCoordinate>();
                }

                return PlaceDestinationQuery.FindDestinations(
                    new PlacePolicy(
                        PlaceSourceContainer.Supply,
                        PlaceEligiblePieceKind.GamePiece,
                        PlaceDestinationScope.EmptySpacesWithinRadius,
                        radius,
                        PlaceChooserKind.ActingHero,
                        PlaceNoDestinationPolicy.Impossible,
                        PlaceDestinationAnchor.ActingHero),
                    components.Tokens,
                    actingBody,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications);
            }
        }
    }
}
