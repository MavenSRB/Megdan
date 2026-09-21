using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's one new contract: the transition that REWRITES a printed Initiative icon.
    /// Milestone 8.71a.
    ///
    /// <para><b>The printed sentence.</b> Hurry up!, his Silver basic: "Set the printed [Initiative]
    /// Initiative value of an unresolved card of a hero in range to 11, until it is resolved or
    /// otherwise changes state. ( This may change the initiative order and tie breakers )."</para>
    ///
    /// <para><b>Why the SELECTION is not here.</b> Reuse-first, and the printed words decide: "a hero
    /// in range" is <c>synthetic-range-hero-target@1</c> word for word - p.9's printed Range icon with
    /// p.16's Range items composed on, p.18's immunity asked at the offer, and the actor left out
    /// unless a card says otherwise. The only thing that selection could not say is "holding an
    /// unresolved card", and that is one optional Boolean on the arm the one face printing it uses
    /// (<c>SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName</c>) rather than a whole
    /// second selection restating p.9 and p.18. Either team is the relation the definition binds; the
    /// sentence names no side, and p.10 keeps Hanu himself out because the text does not say he is
    /// affected (HA-Q1).</para>
    ///
    /// <para><b>Why the REWRITE is a contract of his.</b> Nothing in this build replaced a printed
    /// value. <c>InitiativeResolutionAuthority.QuerySelection</c> composes three sources - the printed
    /// icon, p.16's Initiative items and a hero-scoped <c>ValueContributions</c> on
    /// <c>PrintedValueChannel.Initiative</c> - and all three are ADDITIVE, with the two that are not
    /// the icon keyed on the HERO. The sentence names a CARD and it SETS rather than adds, so there
    /// was no shipped contract whose meaning this is. It is carried on the card instance
    /// (<see cref="CardInstance.InitiativeOverride"/>) and written through the lifecycle authority
    /// that owns every other change to a card, which is what makes "until it is resolved or otherwise
    /// changes state" a consequence of the state change rather than a second rule to remember.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Hanu
    /// content until 8.71h deals him - milestone 7.9's gate, which the twenty heroes before him all
    /// passed the same way: a contract registered before a dealt card can start it moves pinned bytes
    /// while no content moved. <see cref="HurryUpDeferredHandlers"/> is the whole list until then,
    /// <see cref="HurryUpRegisteredHandlers"/> is empty, and <c>HanuHurryUpTests</c> asserts the
    /// absence from both sides.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// "Set the printed [Initiative] Initiative value of an unresolved card of [the chosen hero]
        /// to 11" - the rewrite itself, performed on the card p.10 says that hero has not resolved.
        ///
        /// <para>It chooses nothing. The hero is an input, bound to the selection that named them, so
        /// this node is the sentence's verb and never its object - the division every displacement and
        /// exchange in this build already draws between a selection and what consumes it.</para>
        /// </summary>
        public static EffectCapabilityId HurryUpInitiative { get; } =
            new EffectCapabilityId("hanu-hurry-up-initiative");

        public static EffectCapabilityReference HurryUpInitiativeV1 =>
            new EffectCapabilityReference(HurryUpInitiative, 1);

        /// <summary>The hero whose card is being rewritten, bound from the selection that named them.</summary>
        public const string HurryUpHeroParameterName = "hero";

        /// <summary>
        /// The number the icon is set to - the card's own printed 11, declared at the call site rather
        /// than written into this handler.
        ///
        /// <para>It follows <c>presence-count@1</c>'s <c>minimumPresent</c> and Bain's
        /// <c>minimumCardsInHand</c>: a printed number belongs to the card that prints it, so a later
        /// face printing a different one would be a different argument rather than a different rule.
        /// It is REQUIRED, which is the whole reason it can be an Integer at all - an unbound optional
        /// Integer reads as zero, and a definition that forgot to bind this would quietly set every
        /// card it touched to Initiative 0 and act first for the rest of the round.</para>
        /// </summary>
        public const string HurryUpPrintedInitiativeParameterName = "printedInitiative";

        /// <summary>
        /// The hero whose card this node rewrote.
        ///
        /// <para><b>It is published by the node that PERFORMED the rewrite and not by the selection
        /// that named the hero</b>, and milestone 8.71b is why: The Ultimate Trick reads "You choose
        /// the next action, and how it is performed for a hero you target with the 'Hurry Up!'", so
        /// what it needs is the hero a rewrite actually happened to. A selection can be answered and
        /// the step after it still fail; this fact is only published on the path where the sentence
        /// took effect.</para>
        /// </summary>
        public const string TargetedHeroOutputName = "targetedHero";

        /// <summary>
        /// The card instance whose printed icon now reads the new number, published beside the hero
        /// for a consumer that needs to watch that card rather than that hero - the rewrite lapses
        /// with the CARD (HA-Q1, HA-Q2).
        /// </summary>
        public const string RewrittenCardOutputName = "rewrittenCard";

        /// <summary>
        /// Nothing, until 8.71h deals him. Stated rather than omitted, on the terms every hero's
        /// component partial before his states them.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> HurryUpRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>The one declaration this block withholds from production registration.</summary>
        public static IEnumerable<IEffectCapabilityHandler> HurryUpDeferredHandlers() =>
            new IEffectCapabilityHandler[] { new HurryUpInitiativeHandler() };

        /// <summary>
        /// The rewrite, whole: find the named hero's unresolved card and set its printed Initiative.
        /// </summary>
        private sealed class HurryUpInitiativeHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => HurryUpInitiativeV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            HurryUpHeroParameterName, EffectValueKind.Hero),
                        new EffectCapabilityParameter(
                            HurryUpPrintedInitiativeParameterName, EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding(TargetedHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(RewrittenCardOutputName, EffectValueKind.CardInstance)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// One step, performed the moment it is reached: the sentence asks nothing, so there is no
            /// <c>Resume</c> path and no answer to re-derive.
            ///
            /// <para>The card is found LIVE rather than carried from the selection, which is milestone
            /// 6.8l's rule - what was true when a choice opened is not what its answer applies to. A
            /// hero who was holding an unresolved card when the offer was built and is not holding one
            /// now is p.10's impossible mandatory step, which stops the action rather than rewriting
            /// something else.</para>
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryFindSubject(context, out var card, out var hero) ||
                    !CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                        card, PrintedInitiative(context), context.SourceCardInstanceId))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(TargetedHeroOutputName, EffectValue.Hero(hero)),
                    new EffectOutcomeFact(RewrittenCardOutputName, EffectValue.Card(card.Id))
                });
            }

            /// <summary>Nothing is ever asked, so nothing is ever resumed.</summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryFindSubject(context, out _, out _) && PrintedInitiative(context) >= 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// The card the sentence is about: the named hero's own unresolved card, read through
            /// <see cref="HeroUnresolvedCardQuery"/> - the same reading the selection grey-outs and the
            /// turn order both take, so a hero this build offers is a hero this build can rewrite.
            /// </summary>
            private static bool TryFindSubject(
                EffectCapabilityContext context, out CardInstance card, out HeroId hero)
            {
                card = null;
                hero = default;
                var input = context.Input(HurryUpHeroParameterName);
                if (input.Kind != EffectValueKind.Hero || !input.HeroId.IsDefined ||
                    !context.CardState.IsAvailable)
                {
                    return false;
                }

                hero = input.HeroId;
                return HeroUnresolvedCardQuery.TryFind(context.CardState.Instances, hero, out card);
            }

            private static int PrintedInitiative(EffectCapabilityContext context) =>
                context.Input(HurryUpPrintedInitiativeParameterName).Kind == EffectValueKind.Integer
                    ? context.Input(HurryUpPrintedInitiativeParameterName).IntegerValue
                    : -1;
        }
    }
}
