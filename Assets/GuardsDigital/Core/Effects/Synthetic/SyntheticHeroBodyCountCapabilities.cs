using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.21g. The read-only count "other you in radius": how many bodies of the acting hero,
    /// other than the acting body, stand within the action's printed Radius (raised by Radius items,
    /// R1) of the acting body (RZ-Q4). It answers a printed "If there's another you in radius"
    /// (<see cref="PresentOutput"/> true when at least one stands) and, with <see cref="AtLeastParameter"/>,
    /// each step of a "for every other you in radius" (the n-th step's gate is true when at least n
    /// stand).
    ///
    /// <para><b>Why a new read rather than <c>presence-count@1</c>.</b> That count excludes the actor
    /// at hero grain (9.21c: every body compares equal to the hero), so it can never see his other
    /// bodies - which are exactly what this sentence counts. Nothing here names a card or a hero; it
    /// reads the many-bodies model (9.21a) and the acting body (9.21b), so a hero declaring one body
    /// always answers zero.</para>
    ///
    /// <para>A false answer is an answer, not an impossibility (RULING 53: a false printed "If" is a
    /// legal do-nothing), so it anticipates <see cref="EffectExecutionResultKind.Applied"/> and declares
    /// its outcome to the offer walk (milestone 9.6).</para>
    /// </summary>
    public static class SyntheticHeroBodyCountCapabilities
    {
        /// <summary>How many other bodies of the actor stand in radius.</summary>
        public const string CountOutput = "others";

        /// <summary>Whether at least <see cref="AtLeastParameter"/> (default one) other bodies stand in radius.</summary>
        public const string PresentOutput = "present";

        /// <summary>Optional: the least count <see cref="PresentOutput"/> answers true for (default 1).</summary>
        public const string AtLeastParameter = "atLeast";

        /// <summary>One handler under the reference the composing content names.</summary>
        public static IEffectCapabilityHandler ActorOtherBodiesInRadiusHandler(EffectCapabilityReference reference) =>
            new ActorOtherBodiesInRadius(reference);

        private sealed class ActorOtherBodiesInRadius : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public ActorOtherBodiesInRadius(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            /// <summary>A count reads the board and changes none of it (milestone 9.6).</summary>
            public bool PublishesWithoutApplying => true;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(AtLeastParameter, EffectValueKind.Integer, isRequired: false) },
                new[]
                {
                    new EffectOutputBinding(CountOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Execute(context).Outcomes;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var count = Count(context);
                var atLeast = context.Input(AtLeastParameter);
                var threshold = atLeast.Kind == EffectValueKind.Integer ? Math.Max(1, atLeast.IntegerValue) : 1;
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(CountOutput, EffectValue.Integer(count)),
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(count >= threshold))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;

            private static int Count(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || !components.CanSpawnUnits ||
                    !components.Tokens.TryGetHeroCoordinate(context.ActorId, out var acting))
                {
                    return 0;
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications).Total;
                // The one derivation the Defense contribution counts too
                // (RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius), from its own body.
                return RosterPresenceQuery.CountOtherBodiesWithin(
                    components.Units.GetHeroBodies(context.ActorId), acting, radius);
            }
        }
    }
}
