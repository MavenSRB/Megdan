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

        /// <summary>
        /// Milestone 8.61b. The handler declares the piece it is bound to and the icon it bounds its
        /// position by, so rulebook p.14's Block condition can read the token off the very node its
        /// mandatory swap consumes rather than off a second declaration that could disagree with it -
        /// see <see cref="IOwnedTokenFactDeclaration"/> for why that second declaration has no home.
        /// </summary>
        private sealed class OwnedTokenFactHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler, IOwnedTokenFactDeclaration
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
            public TokenDefinition TokenDefinition => definition;
            public PrintedValueChannel? ReachChannel => reachChannel;
            public bool PublishesPresenceOnly => presence;
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

            /// <summary>
            /// Milestone 8.61b moved the reading itself into
            /// <see cref="OwnedTokenReachQuery"/> and left the frame's own guards here. Nothing about
            /// what this publishes changed: what changed is that p.14's Block condition now asks the
            /// identical question through the identical code, which is the only way a defence preview
            /// and the swap it predicts can be one verdict rather than two derivations.
            /// </summary>
            private IReadOnlyList<EffectOutcomeFact> Observe(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                    return Array.Empty<EffectOutcomeFact>();
                if (presence)
                {
                    var owned = OwnedTokenReachQuery.OwnedTokens(
                        definition, context.ActorId, components.Tokens);
                    // A malformed board must not answer about an arbitrary copy of a singular piece.
                    return owned.Count > 1
                        ? Array.Empty<EffectOutcomeFact>()
                        : new[]
                        {
                            new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(owned.Count == 1))
                        };
                }

                if (reachChannel.HasValue && !context.CardState.IsAvailable)
                    return Array.Empty<EffectOutcomeFact>();
                return OwnedTokenReachQuery.TryFindOwnedToken(
                    definition,
                    reachChannel,
                    context.ActorId,
                    components.Tokens,
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.IsAvailable ? context.CardState.Definitions : null,
                    context.HeroItems,
                    context.Modifications,
                    out var tokenAt)
                    ? new[] { new EffectOutcomeFact(SpaceOutput, EffectValue.Space(tokenAt)) }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }
    }
}
