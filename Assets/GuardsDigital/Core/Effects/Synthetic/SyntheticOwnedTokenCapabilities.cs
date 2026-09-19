using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Read-only facts about one named, actor-owned token. The trusted declaration binds its
    /// definition and optional printed reach; an answer cannot substitute another token type.
    /// This deliberately narrow contract requires a single-piece supply, not a choice among tokens.
    /// </summary>
    public static class SyntheticOwnedTokenCapabilities
    {
        public const string PresentOutput = "present";
        public const string SpaceOutput = "space";

        public static EffectCapabilityReference OwnedHazardSpaceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("synthetic-owned-hazard-space"), 1);
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() => new[]
        {
            SpaceHandler(OwnedHazardSpaceV1, Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition)
        };

        public static IEffectCapabilityHandler PresenceHandler(
            EffectCapabilityReference reference, TokenDefinition definition) =>
            new OwnedTokenFactHandler(reference, definition, true, null);

        public static IEffectCapabilityHandler SpaceHandler(
            EffectCapabilityReference reference, TokenDefinition definition,
            PrintedValueChannel? reachChannel = null) =>
            new OwnedTokenFactHandler(reference, definition, false, reachChannel);

        private sealed class OwnedTokenFactHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly TokenDefinition definition;
            private readonly bool presence;
            private readonly PrintedValueChannel? reachChannel;

            public OwnedTokenFactHandler(EffectCapabilityReference reference, TokenDefinition definition,
                bool presence, PrintedValueChannel? reachChannel)
            {
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
                if (definition.SupplyCount != 1)
                    throw new ArgumentException("Named-token facts require one physical piece.", nameof(definition));
                if (reachChannel.HasValue && reachChannel != PrintedValueChannel.Range &&
                    reachChannel != PrintedValueChannel.Radius)
                    throw new ArgumentException("Token reach must be Range or Radius.", nameof(reachChannel));
                Reference = reference;
                this.presence = presence;
                this.reachChannel = reachChannel;
                Schema = new EffectCapabilityParameterSchema(outputs: new[]
                {
                    new EffectOutputBinding(presence ? PresentOutput : SpaceOutput,
                        presence ? EffectValueKind.Boolean : EffectValueKind.Coordinate)
                });
            }

            public EffectCapabilityReference Reference { get; }
            public EffectCapabilityParameterSchema Schema { get; }
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public bool PublishesWithoutApplying => true;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var facts = Observe(context);
                return facts.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A named-token fact requests no choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Observe(context).Count == 0 ? EffectExecutionResultKind.ImpossibleMandatory :
                    EffectExecutionResultKind.Applied;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> chosen) => Observe(context);

            private IReadOnlyList<EffectOutcomeFact> Observe(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                    return Array.Empty<EffectOutcomeFact>();
                var tokens = components.Tokens.PlacedTokens.Where(token =>
                    token.Definition.Reference.Equals(definition.Reference) &&
                    token.OwnerHeroId.Equals(context.ActorId)).Take(2).ToArray();
                // A malformed board must not choose an arbitrary copy of a singular piece.
                if (tokens.Length > 1) return Array.Empty<EffectOutcomeFact>();
                if (presence)
                    return new[] { new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(tokens.Length == 1)) };
                if (tokens.Length == 0) return Array.Empty<EffectOutcomeFact>();
                var tokenAt = tokens[0].Coordinate;
                if (reachChannel.HasValue)
                {
                    if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actorAt) ||
                        !context.CardState.IsAvailable)
                        return Array.Empty<EffectOutcomeFact>();
                    var reach = reachChannel == PrintedValueChannel.Radius
                        ? RadiusValueQuery.Calculate(context.SourceCardInstanceId,
                            context.SourceDefinitionReference, context.ActionDefinitionId,
                            context.CardState.Definitions, context.ActorId, context.HeroItems,
                            context.Modifications).Total
                        : RangeValueQuery.Calculate(context.SourceCardInstanceId,
                            context.SourceDefinitionReference, context.ActionDefinitionId,
                            context.CardState.Definitions, context.ActorId, context.HeroItems,
                            context.Modifications, ActionValueApplicabilityQuery.For(
                                context.SourceDefinitionReference, context.ActionDefinitionId,
                                context.CardState.Definitions)).Total;
                    if (HexDistanceQuery.Calculate(actorAt, tokenAt) > reach)
                        return Array.Empty<EffectOutcomeFact>();
                }
                return new[] { new EffectOutcomeFact(SpaceOutput, EffectValue.Space(tokenAt)) };
            }
        }
    }
}
