using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
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
        /// Milestone 8.37's <c>synthetic-source-card-in-discard@1</c>: whether the card
        /// whose text is currently running is itself standing in the discard pile, read live at the
        /// moment the predicate is asked.
        ///
        /// <para><b>Why this is a second capability rather than a parameter on
        /// <see cref="HeroCondition"/>.</b> That one asks about the ACTOR's own standing condition -
        /// "if enraged" - which is a fact about a hero. This asks about the SOURCE CARD's own lifecycle
        /// state - "if this card is (not) in the discard" - which is a fact about the card, and the two
        /// have different subjects and different readers. Tali's set is the first to print a condition a
        /// card asks about itself: three Winter Attacks and two Spirit Attacks read it directly, and
        /// Guardian Spirit and Warrior Spirit each pair it with <c>retrieve-card@1</c>.</para>
        ///
        /// <para><b>It names no card, and it takes exactly one parameter that is not a card either.</b>
        /// "This card" is never a choice - it is always the one card whose text is executing, which
        /// <see cref="EffectCapabilityContext.SourceCardInstanceId"/> already names, so widening this to
        /// ask about a card other than its own would be a reading no printed sentence needs and the
        /// self-referential framing forbids. The one parameter it does take is
        /// <see cref="NegateParameter"/>: her own faces print BOTH directions of the same question
        /// ("if this card is NOT in the discard" on the three Winter Attacks and Spirit Wolf/Bear's
        /// bullet, "if this card IS in the discard" on Guardian Spirit and Warrior Spirit) and a
        /// <see cref="EffectNodeKind.Conditional"/> is structurally a then-branch with an OPTIONAL
        /// else - it cannot express "run this when false, run nothing when true" without one - so the
        /// question is asked once and read either way by the one boolean flip a Conditional cannot
        /// itself express, rather than by a second capability duplicating the read.</para>
        ///
        /// <para><b>Reads <see cref="CardLocation.DiscardPile"/> off the live card state rather than off
        /// a captured fact</b>, because Commune with Spirits' borrow performs a discarded card's own text
        /// while that card is still standing in the discard (RULING 22): the same predicate, asked while
        /// a borrowed Winter Dagger executes, must answer <c>true</c> even though nothing about
        /// <em>this</em> milestone's node table changed - it is the card's own location that differs
        /// between the two callers, not the question.</para>
        /// </summary>
        public static EffectCapabilityId SourceCardInDiscard { get; } =
            new EffectCapabilityId("synthetic-source-card-in-discard");

        public static EffectCapabilityReference SourceCardInDiscardV1 =>
            new EffectCapabilityReference(SourceCardInDiscard, 1);

        /// <summary>
        /// Whether the source card is standing in the discard pile right now - or its negation, when
        /// <see cref="NegateParameter"/> is bound true. The output name never changes with the reading:
        /// a Conditional binds it as "the fact to branch on", whichever fact that composition asked for.
        /// </summary>
        public const string InDiscardOutput = "inDiscard";

        /// <summary>
        /// Optional, defaulting to false (the plain "in discard" reading). Bind
        /// <see cref="EffectValue.Boolean"/> true for "is NOT in the discard" - Winter Dagger, Winter
        /// Spear, Winter Scepter and both Spirit Attacks' bullet - and leave it unbound for "IS in the
        /// discard" - Guardian Spirit and Warrior Spirit's retrieval clause.
        /// </summary>
        public const string NegateParameter = "negate";

        /// <summary>
        /// Milestone 8.37a. Optional, defaulting to false. Bind <see cref="EffectValue.Boolean"/> true
        /// to count a card the controller has PLAYED AND RESOLVED as being in their discard.
        ///
        /// <para><b>Why this is a parameter and not the plain reading.</b> This build keeps a played
        /// card in <see cref="CardLocation.TurnArea"/> for the round and reserves
        /// <see cref="CardLocation.DiscardPile"/> for a card SPENT - one given as a p.14 Defense, or
        /// taken by a discard clause. The two are different piles to this engine and the same pile at
        /// the table, and the roster reads them differently already:
        /// <c>RosterCardRetrievalKind.OwnedDiscardedCard</c> admits only the spent card, while the
        /// basic-Skill retrieval beside it admits a resolved played one too.
        ///
        /// <para>Every reading that existed before this milestone wants the narrow one and keeps it:
        /// the three Winter Attacks and both Spirit Attacks ask while the card is mid-resolution, when
        /// it is neither spent nor resolved, and Guardian and Warrior Spirit's own clause is about a
        /// card being run FROM the discard by Commune's borrow. Blizzard is the first face to ask
        /// about itself AFTER its own resolution has finished - "If Blizzard is in your discard, End
        /// of turn:" - where the narrow reading is false by construction and would make the clause
        /// dead text. See open question 34: the widening is an interpretation, so it is opt-in and
        /// named at its one call site rather than applied to everybody.</para>
        /// </summary>
        public const string CountResolvedPlayedParameter = "countResolvedPlayed";

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
            new IEffectCapabilityHandler[]
            {
                new HeroConditionHandler(),
                // Milestone 8.37, the milestone that deals Tali the Spiritcaller. Seven of her
                // nineteen faces read their own discard state, so the predicate is now started by a
                // dealt card and the gate that held it below is satisfied on both halves.
                new SourceCardInDiscardHandler()
            };

        /// <summary>
        /// The handlers this file owns, for the milestone that deals Ursafar or Tali to register beside
        /// their own content - and for the offline tests that compose a catalog of their own to prove
        /// them. <c>hero-condition@1</c> is Ursafar's; <c>synthetic-source-card-in-discard@1</c> is
        /// Tali's; both are deliberately generic and either hero's dealing may register whichever of the
        /// two it needs first without waiting on the other.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// A pure predicate over the card state the resolution was given: it mutates nothing, asks
        /// nobody, and publishes one boolean read from the source card's own current location.
        /// </summary>
        private sealed class SourceCardInDiscardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SourceCardInDiscardV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            NegateParameter, EffectValueKind.Boolean, isRequired: false),
                        new EffectCapabilityParameter(
                            CountResolvedPlayedParameter, EffectValueKind.Boolean, isRequired: false)
                    },
                    new[] { new EffectOutputBinding(InDiscardOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool TryFindSourceCard(EffectCapabilityContext context, out CardInstance card)
            {
                card = null;
                if (!context.SourceCardInstanceId.IsDefined || !context.CardState.IsAvailable)
                {
                    return false;
                }

                card = context.CardState.Instances.FirstOrDefault(
                    instance => instance.Id.Equals(context.SourceCardInstanceId));
                return card != null;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryFindSourceCard(context, out var card))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var inDiscard = card.State.Location == CardLocation.DiscardPile ||
                    (context.Input(CountResolvedPlayedParameter).BooleanValue &&
                        card.State.Location == CardLocation.TurnArea &&
                        card.State.HasBeenPlayed &&
                        card.State.ResolutionStatus == CardResolutionStatus.Resolved);
                var negate = context.Input(NegateParameter).BooleanValue;
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        InDiscardOutput, EffectValue.Boolean(negate ? !inDiscard : inDiscard))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryFindSourceCard(context, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

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
