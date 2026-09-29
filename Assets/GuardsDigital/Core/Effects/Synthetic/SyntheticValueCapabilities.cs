using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Capabilities that publish one of the acting card's own printed values as a typed fact, deciding
    /// nothing and mutating nothing.
    ///
    /// Milestone 7.2c added the first, for the reason milestone 6.5 added the actor's-space publisher:
    /// a definition can only hand a value to a consuming node through an earlier node's published
    /// output, and until now every published output came from a decision. A printed icon is not a
    /// decision - the value is on the card - so asking the actor to pick their own Radius out of a
    /// one-candidate prompt would invent a choice the card does not offer.
    ///
    /// Publishing it rather than teaching each consumer to read it is what keeps rulebook p.9's
    /// distinction visible in the definition itself. A card whose text says "Push it 3 spaces" writes
    /// the 3 down, because that is card text and p.16 never raises it; a card whose text says "within
    /// Radius" binds this node instead, because the page says that text "refers to this value" - the
    /// printed icon's - and p.16 does raise it. The two readings are then different bindings in the
    /// node table rather than the same integer parameter meaning different things.
    /// </summary>
    public static class SyntheticValueCapabilities
    {
        public static EffectCapabilityId CardRadius { get; } =
            new EffectCapabilityId("synthetic-card-radius");

        public static EffectCapabilityReference CardRadiusV1 =>
            new EffectCapabilityReference(CardRadius, 1);

        /// <summary>The published output's name, spelled once so a definition cannot mistype it.</summary>
        public const string RadiusOutput = "radius";

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[] { new CardRadiusHandler() };

        /// <summary>
        /// Publishes the acting card's Radius Total - p.9's printed icon with p.16's matching items
        /// composed onto it - as a typed integer.
        ///
        /// It derives nothing itself: <see cref="RadiusValueQuery"/> owns the composition, exactly as it
        /// does for the printed-Radius target selection, so a consumer bound to this node and a
        /// consumer that asks the query directly cannot disagree. A card that prints no Radius icon has
        /// no Radius to publish, and this reports that as impossible rather than publishing zero -
        /// a zero would silently bound a consumer to the actor's own space, which is a smaller answer
        /// rather than an absent one, and p.16 does not invent an icon a card does not print.
        /// </summary>
        private sealed class CardRadiusHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>
            /// Milestone 9.6: reading the printed radius changes nothing, so an offer is judged by the
            /// step that uses it - "If there are at least two enemy heroes in radius" was offered with
            /// nobody there because this read alone counted as doing something.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var total = Total(context);
                return total <= 0
                    ? Array.Empty<EffectOutcomeFact>()
                    : new[] { new EffectOutcomeFact(RadiusOutput, EffectValue.Integer(total)) };
            }

            public EffectCapabilityReference Reference => CardRadiusV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(RadiusOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var total = Total(context);
                if (total <= 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(RadiusOutput, EffectValue.Integer(total))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Applies whenever the card prints a Radius. Publishing a value is applying, so a branch
            /// beginning here is judged by this node and not by what consumes the value, which is
            /// milestone 6.5ac's declared conservative boundary.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Total(context) <= 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            private static int Total(EffectCapabilityContext context)
            {
                if (!context.CardState.IsAvailable)
                {
                    return 0;
                }

                return RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications).Total;
            }
        }
    }
}
