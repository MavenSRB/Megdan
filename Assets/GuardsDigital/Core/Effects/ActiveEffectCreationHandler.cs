using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// A capability whose whole content is one declared Active effect, created on the acting hero.
    ///
    /// <para>Milestone 6.8 wrote this shape once, privately, for the five synthetic modification
    /// families; milestone 8.9 lifted it out so Wasp's two basic cards register through the same code
    /// rather than through a second copy of it. What each registration supplies is its
    /// <see cref="ActiveEffectDefinition"/> - the scope, the duration, the checkpoints, the
    /// cancellations, and the rules the effect carries while it runs - and nothing else, which is the
    /// division milestone 6.2a drew: the definition says what the effect is, the lifecycle says when it
    /// applies, and this says only that performing the action creates it.</para>
    ///
    /// <para>One registration per definition, never one with a mode selector, because
    /// <see cref="IEffectCapabilityHandler.ActiveEffectDefinitions"/> means "every definition this
    /// capability can create" - a selector would make that list a lie or smuggle the choice through an
    /// untyped payload.</para>
    ///
    /// <para>Creating an effect that already exists is a legal no-op rather than a failure, which is
    /// what <c>Applied</c> means: the identity milestone 6.8d mints is the same for a re-executed node
    /// inside one resolution, and the lifecycle's own uniqueness rule refuses a duplicate
    /// independently.</para>
    /// </summary>
    public sealed class ActiveEffectCreationHandler : IEffectCapabilityHandler
    {
        /// <summary>The acting hero, published so a later node can name whose effect this is.</summary>
        public const string AffectedOutputName = "affected";

        private readonly ActiveEffectDefinition definition;

        public ActiveEffectCreationHandler(
            EffectCapabilityReference reference,
            ActiveEffectDefinition definition)
        {
            if (!reference.IsDefined)
            {
                throw new ArgumentException(
                    "An Active-effect capability requires a defined reference.", nameof(reference));
            }

            Reference = reference;
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ActiveEffectDefinitions = Array.AsReadOnly(new[] { definition });
        }

        public EffectCapabilityReference Reference { get; }

        public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
            outputs: new[] { new EffectOutputBinding(AffectedOutputName, EffectValueKind.Hero) });

        public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

        public EffectExecutionResult Execute(EffectCapabilityContext context)
        {
            var affected = new[]
            {
                new EffectOutcomeFact(AffectedOutputName, EffectValue.Hero(context.ActorId))
            };
            if (context.Timing == null || context.Participants == null)
            {
                // Outside a live match there is no lifecycle to create into. A legal rules no-op.
                return EffectExecutionResult.Applied(affected);
            }

            var effectId = ActiveEffectInstanceIdentity.Mint(context, definition);
            if (context.Timing.Effects.All(effect => !effect.Id.Equals(effectId)))
            {
                context.Timing.Create(
                    effectId,
                    definition,
                    context.ActorId,
                    context.Participants.GetTeamForHero(context.ActorId).Side,
                    default,
                    context.SourceCardInstanceId,
                    context.ActionDefinitionId,
                    Math.Max(context.RoundIndex, 1),
                    Math.Max(context.TurnIndex, 1),
                    BoundActor(context));
            }

            return EffectExecutionResult.Applied(affected);
        }

        /// <summary>
        /// The hero this occurrence's declarations point at with "this", read from the moment the
        /// definition names. Milestone 8.20.
        ///
        /// <para>Nothing is parsed and nothing is chosen: the definition states which already-recorded
        /// fact is the referent, and this reads that fact. An undefined answer is legal and means the
        /// occurrence bound nobody - which is what
        /// <see cref="Rules.Modifications.RestrictedActorScope.ExceptsTheBoundActor"/> reads as
        /// excepting nobody, so the card's named classes stand exactly as printed.</para>
        /// </summary>
        private GuardsDigital.Core.Units.Heroes.HeroId BoundActor(EffectCapabilityContext context)
        {
            switch (definition.BoundActorSource)
            {
                case ActiveEffectBoundActorSource.None:
                    return default;
                case ActiveEffectBoundActorSource.EnclosingAttacker:
                    return context.EnclosingAttack.ActorId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(context),
                        definition.BoundActorSource,
                        "Unknown Active-effect bound-actor source.");
            }
        }

        public EffectExecutionResult Resume(
            EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
            throw new InvalidOperationException("This capability never requests a choice.");

        /// <summary>
        /// Always applies, so the answer needs to read nothing - which is the point of asking here
        /// rather than executing: this handler's own execution creates the effect and mints its
        /// instance identity, and a probe that ran it would grant the modification it was asked
        /// whether the actor could grant.
        /// </summary>
        public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
            EffectExecutionResultKind.Applied;
    }
}
