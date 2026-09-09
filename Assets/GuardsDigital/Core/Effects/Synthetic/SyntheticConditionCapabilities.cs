using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The one capability that reads a named hero condition - milestone 8.35z31's
    /// <c>hero-condition@1</c>.
    ///
    /// <para><b>Why it is its own capability rather than a boolean somebody else publishes.</b> Every
    /// Conditional in the build so far branches on a fact an earlier node produced - a hero was named,
    /// a card was discarded, an attack took a hero - and <c>synthetic-is-flagged@1</c> exists to present
    /// that fact in the shape a Conditional reads. A condition is not that: nothing earlier in the card
    /// produced it, and on most of Ursafar's faces the conditional clause is the card's <em>first</em>
    /// step. So the question has to be asked of the match rather than of the frame, which is what a
    /// capability is for. The effect definition validator admits any capability with a Boolean output
    /// as a predicate, so this is named directly by the Conditional and no adapter stands between
    /// them.</para>
    ///
    /// <para><b>Deferred, not registered.</b> The only content that names it is Ursafar's, and he is not
    /// dealt by this build. The registered capability list is serialized into every snapshot, so
    /// registering a capability before its hero is dealt moves pinned bytes and reads at the far end
    /// like a serialization defect rather than like content arriving. <see cref="RegisteredHandlers"/>
    /// is therefore deliberately empty and <see cref="DeferredHandlers"/> carries the real one, which
    /// the milestone that deals him moves across in the same change.</para>
    /// </summary>
    public static class SyntheticConditionCapabilities
    {
        public static EffectCapabilityId HeroCondition { get; } =
            new EffectCapabilityId("hero-condition");

        /// <summary>
        /// `hero-condition@1`. Whether the acting hero is currently in the named condition.
        ///
        /// <para>It reads the actor rather than taking a hero parameter because all nineteen printed
        /// readings in the roster are about the hero performing the action - "<b>If enraged</b>", with
        /// no other subject anywhere. A hero parameter would be a reading no face prints, and the
        /// first face that prints one widens this to a second revision rather than the meaning being
        /// guessed at now.</para>
        /// </summary>
        public static EffectCapabilityReference HeroConditionV1 =>
            new EffectCapabilityReference(HeroCondition, 1);

        /// <summary>The condition being asked about, as a closed vocabulary.</summary>
        public const string ConditionParameter = "condition";

        /// <summary>Whether the actor is in it.</summary>
        public const string PresentOutput = "present";

        /// <summary>
        /// Milestone 8.35z50 registered this file's one handler, beside the hero whose dealt cards
        /// start it.
        ///
        /// <para><b>It is shared with Tali and registers here anyway</b>, which is ledger row 25's rule
        /// rather than an exception to it: milestone 7.9's gate asks whether a DEALT card starts a
        /// contract, so a contract two heroes name is deferred only until the FIRST of its sharers is
        /// dealt. Ursafar is dealt and Tali is not, so `hero-condition@1` crosses with him; her own
        /// dealing finds it already registered and adds nothing, exactly as Silverarrow's dealing found
        /// `choose-line-destination@1` already registered by Misa's.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[] { new HeroConditionHandler() };

        /// <summary>
        /// Nothing is deferred here any longer - milestone 8.35z50 moved this file's one handler across.
        /// The member survives because the offline content tests compose catalogs of their own from the
        /// deferred lists, and a hero composed but not yet dealt still needs that seam.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// A pure predicate over the match's own timing state: it mutates nothing, asks nobody, and
        /// publishes one boolean.
        /// </summary>
        private sealed class HeroConditionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => HeroConditionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            ConditionParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(HeroConditionKind.None))
                    },
                    new[] { new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool CanRead(EffectCapabilityContext context)
            {
                var condition = (HeroConditionKind)context.Input(ConditionParameter).IntegerValue;
                return context.ActorId.IsDefined &&
                    context.Timing != null &&
                    condition != HeroConditionKind.None &&
                    Enum.IsDefined(typeof(HeroConditionKind), condition);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanRead(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var condition = (HeroConditionKind)context.Input(ConditionParameter).IntegerValue;
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        PresentOutput,
                        EffectValue.Boolean(HeroConditionQuery.IsInCondition(
                            condition, context.ActorId, context.Timing?.Effects,
                            context.ActorContinuousSources)))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanRead(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
