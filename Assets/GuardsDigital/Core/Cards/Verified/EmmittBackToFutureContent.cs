using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Back to the Future places a named unit into its recorded turn-start space.</summary>
    public static class EmmittBackToFutureContent
    {
        public const string HeroParameter = "hero";
        public const string MinionParameter = "minion";
        public const string SelectedSpaceParameter = "selectedSpace";
        public const string PlacedOutput = "placed";

        public static EffectCapabilityReference PlaceAtTurnStartV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-place-at-turn-start"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new PlaceHandler();
        }

        private sealed class PlaceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlaceAtTurnStartV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero, isRequired: false),
                    new EffectCapabilityParameter(MinionParameter, EffectValueKind.Minion, isRequired: false),
                    new EffectCapabilityParameter(SelectedSpaceParameter, EffectValueKind.Coordinate)
                },
                new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryPlan(context, out _, out _, out _, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryPlan(context, out var subject, out var origin,
                        out var destination, out var policy))
                    return EffectExecutionResult.ImpossibleMandatory();
                var outcome = PlaceOperation.PlaceFromBoard(policy,
                    context.Components.Board, context.Components.Tokens,
                    subject, origin, destination, new[] { destination },
                    context.Modifications, context.Components.Units);
                return outcome.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(PlacedOutput, EffectValue.Space(outcome.Destination))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("The printed turn-start destination asks no choice.");

            private static bool TryPlan(EffectCapabilityContext context,
                out BoardObjectReference subject, out HexCoordinate origin,
                out HexCoordinate destination, out PlacePolicy policy)
            {
                subject = default;
                origin = default;
                destination = default;
                policy = null;
                var hero = context.Input(HeroParameter);
                var minion = context.Input(MinionParameter);
                var selectedSpace = context.Input(SelectedSpaceParameter);
                if (selectedSpace.Kind != EffectValueKind.Coordinate ||
                    context.Components?.Board == null ||
                    !context.Components.Board.TryGetObjectAt(selectedSpace.Coordinate, out subject))
                    return false;
                if (hero.Kind == EffectValueKind.Hero && hero.HeroId.IsDefined &&
                    minion.Kind == EffectValueKind.None)
                {
                    if (subject.Kind != BoardObjectKind.Hero || !subject.HeroId.Equals(hero.HeroId))
                        return false;
                }
                else if (minion.Kind == EffectValueKind.Minion && minion.MinionId.IsDefined &&
                    hero.Kind == EffectValueKind.None)
                {
                    if (subject.Kind != BoardObjectKind.Minion || !subject.MinionId.Equals(minion.MinionId))
                        return false;
                }
                else
                    return false;

                var components = context.Components;
                if (components == null || !components.CanDisplace || components.Tokens == null ||
                    !(components.Board is IUnitTurnHistory history) ||
                    !components.Board.TryGetObjectCoordinate(subject, out origin) ||
                    !history.TryGetTurnStartPosition(subject, out destination) ||
                    origin.Equals(destination))
                    return false;

                var unit = subject.AsRuleObject();
                if (TargetQuery.IsPrevented(unit, context.EnclosingActivity,
                        RuleObject.ForHero(context.ActorId), components.Board,
                        context.Participants, context.Modifications) ||
                    TargetQuery.IsPrevented(unit, RuleActivity.Place,
                        RuleObject.ForHero(context.ActorId), components.Board,
                        context.Participants, context.Modifications) ||
                    RestrictionQuery.TryGetRestriction(unit, RuleActivity.Place,
                        RuleObject.ForHero(context.ActorId), context.Participants,
                        context.Modifications, out _))
                    return false;

                policy = new PlacePolicy(PlaceSourceContainer.Board,
                    PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    HexDistanceQuery.Calculate(origin, destination), PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece);
                return PlaceDestinationQuery.FindDestinations(policy, components.Tokens,
                        origin, new[] { destination }, context.Modifications,
                        components.Units, components.Board, unit).Contains(destination);
            }
        }
    }
}
