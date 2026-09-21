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
        /// <summary>
        /// The hero this occurrence is about, published so a later node can name whose effect this is.
        ///
        /// <para>It is the acting hero for every scope whose membership the definition already
        /// decides, and the NAMED hero for
        /// <see cref="ActiveEffectAffectedScopeKind.Hero"/> - which is the one scope where the acting
        /// hero is not who the effect is about. Milestone 8.71d; see
        /// <see cref="AffectedHeroParameterName"/>.</para>
        /// </summary>
        public const string AffectedOutputName = "affected";

        /// <summary>
        /// Milestone 8.71d. The one hero an
        /// <see cref="ActiveEffectAffectedScopeKind.Hero"/>-scoped occurrence affects, as an input
        /// bound from the node that named them.
        ///
        /// <para><b>Why the parameter exists only on that scope.</b> Every other scope decides its own
        /// membership from the definition and the controller - the controller himself, his team, his
        /// disk, everybody - so there is nothing for a caller to say. The Hero scope is the one that
        /// names somebody, <see cref="ActiveEffectInstance"/> refuses an occurrence of it without a
        /// hero, and until this milestone the only way to create one was to reach past this handler
        /// and call the lifecycle directly. Hanu's three journeys - "Swap with an enemy hero in
        /// radius. <b>This turn:</b> That hero is immune." - are the first printed content whose
        /// Active effect is about somebody the card chose, so the selection's output is where the hero
        /// comes from.</para>
        ///
        /// <para>It is REQUIRED on that scope rather than optional, which is the same invariant the
        /// instance states said from the other side: an occurrence of a Hero-scoped definition with no
        /// hero is not a weaker effect, it is a state this build refuses. Declaring it optional would
        /// have moved that refusal from definition validation - where an unbound required input is
        /// caught before a match ever runs - to the middle of a resolution.</para>
        /// </summary>
        public const string AffectedHeroParameterName = "affectedHero";

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
            NamesAHero = definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero;
            Schema = NamesAHero
                ? new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            AffectedHeroParameterName, EffectValueKind.Hero)
                    },
                    new[] { new EffectOutputBinding(AffectedOutputName, EffectValueKind.Hero) })
                : new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(AffectedOutputName, EffectValueKind.Hero)
                    });
        }

        public EffectCapabilityReference Reference { get; }

        /// <summary>Whether this registration's definition is the one scope that names a hero.</summary>
        private bool NamesAHero { get; }

        public EffectCapabilityParameterSchema Schema { get; }

        public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

        public EffectExecutionResult Execute(EffectCapabilityContext context)
        {
            var namedHero = NamedHero(context);
            if (NamesAHero && !namedHero.IsDefined)
            {
                // Milestone 8.71d. A Hero-scoped occurrence with nobody named is not a weaker effect,
                // so it is not created as one: the step cannot be performed and p.10 stops there.
                // Definition validation refuses an unbound required input long before this, so the
                // path reached here is a node whose selection published no hero at all.
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var affected = new[]
            {
                new EffectOutcomeFact(
                    AffectedOutputName,
                    EffectValue.Hero(NamesAHero ? namedHero : context.ActorId))
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
                    namedHero,
                    context.SourceCardInstanceId,
                    context.ActionDefinitionId,
                    Math.Max(context.RoundIndex, 1),
                    Math.Max(context.TurnIndex, 1),
                    BoundActor(context));
            }

            return EffectExecutionResult.Applied(affected);
        }

        /// <summary>
        /// The hero a Hero-scoped occurrence affects, read from the input the node bound, or the
        /// undefined hero for every other scope - which is the value the lifecycle has always been
        /// handed and the value <see cref="ActiveEffectInstance"/> requires for those scopes.
        /// </summary>
        private GuardsDigital.Core.Units.Heroes.HeroId NamedHero(EffectCapabilityContext context)
        {
            if (!NamesAHero)
            {
                return default;
            }

            var named = context.Input(AffectedHeroParameterName);
            return named.Kind == EffectValueKind.Hero ? named.HeroId : default;
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
        ///
        /// <para><b>Milestone 8.71d gives it one thing to read, and it reads the same member the
        /// execution reads.</b> A Hero-scoped creation can now refuse - a named hero who turns out to
        /// be nobody is not a weaker effect - and a probe that answered from a second derivation, or
        /// from none, is exactly the disagreement milestone 6.5ac's conformance rule exists to stop.
        /// It costs nothing before the hero is chosen: <c>affectedHero</c> is a REQUIRED parameter, so
        /// the feasibility walk treats a node whose binding has not been published yet as undecidable
        /// and never asks this at all. Every other scope still answers the bare constant it always
        /// did, because every other scope decides its own membership and has no refusal to
        /// derive.</para>
        /// </summary>
        public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
            NamesAHero && !NamedHero(context).IsDefined
                ? EffectExecutionResultKind.ImpossibleMandatory
                : EffectExecutionResultKind.Applied;
    }
}
