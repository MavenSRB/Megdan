using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z31's <c>borrow-primary-action@1</c>: the one capability that names a card
    /// whose printed primary action is about to be performed off it.
    ///
    /// <para>It is one capability and not three, although three faces of Ursafar's print the clause and
    /// a fourth of Tali's prints it again, because the user's ruling made all four the same rule. What
    /// differs between them is the pool and the colour narrowing, and both of those are inputs.
    /// <see cref="BorrowedPrimaryActionDeclaration"/> is where the rule itself is written down.</para>
    ///
    /// <para><b>What this capability does and deliberately does not do.</b> It chooses the card. It does
    /// not start the action - a capability handler mutates the frame it is in, and a borrowed primary is
    /// a whole action instance with its own resolution, its own pauses and its own card-text seams, so
    /// starting it belongs to the same authority that starts every other action. The shape is milestone
    /// 6.7's exactly: <see cref="EffectNodeKind.RepeatAction"/> "performs nothing itself and invokes no
    /// capability: it records the request on the frame, and the card-action dispatcher creates the
    /// second instance". This records its request the same way, and
    /// <see cref="BorrowedActionQuery"/> is how the dispatcher reads it back - the direct twin of
    /// <c>RepeatedActionQuery.Requested</c>, and separate from it on purpose, because a borrow must be
    /// invisible to every repeat reader.</para>
    ///
    /// <para><b>Deferred, not registered</b>, for the reason
    /// <see cref="SyntheticConditionCapabilities"/> records: the registered set is serialized, and the
    /// only content naming this is a hero this build does not deal.</para>
    /// </summary>
    public static class SyntheticBorrowedActionCapabilities
    {
        public static EffectCapabilityId BorrowPrimaryAction { get; } =
            new EffectCapabilityId("borrow-primary-action");

        public static EffectCapabilityReference BorrowPrimaryActionV1 =>
            new EffectCapabilityReference(BorrowPrimaryAction, 1);

        /// <summary>Which pool the clause reaches, as a closed vocabulary.</summary>
        public const string PoolParameter = "pool";

        /// <summary>
        /// The colour the clause narrows its pool to, as a closed vocabulary including
        /// <see cref="CardColor.None"/> for a face that narrows nothing. It is a required parameter
        /// rather than an optional one so that a composition cannot narrow nothing by accident.
        /// </summary>
        public const string ColorParameter = "color";

        /// <summary>The card whose printed primary the borrowing hero is about to perform.</summary>
        public const string BorrowedCardOutput = "borrowedCard";

        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[] { new BorrowPrimaryActionHandler() };

        /// <summary>
        /// The candidate set one borrow node would offer right now, as a function of state alone.
        ///
        /// <para>Exposed beside the handler because feasibility asks the same question the offer does:
        /// milestone 8.35z15's rule is that a card-text step with no legal subject is not offered, and
        /// Angry Roar played with an empty pool is exactly that step. Both readings therefore come from
        /// one function, which is what stops an offer and its greying from disagreeing.</para>
        /// </summary>
        public static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
        {
            if (context == null || !context.CardState.IsAvailable || !context.ActorId.IsDefined)
            {
                return Array.Empty<CardInstance>();
            }

            var pool = (BorrowedActionPool)context.Input(PoolParameter).IntegerValue;
            var color = (CardColor)context.Input(ColorParameter).IntegerValue;
            if (pool == BorrowedActionPool.None ||
                !Enum.IsDefined(typeof(BorrowedActionPool), pool) ||
                !Enum.IsDefined(typeof(CardColor), color))
            {
                return Array.Empty<CardInstance>();
            }

            var declaration = pool == BorrowedActionPool.OwnedDiscardedCards
                ? BorrowedPrimaryActionDeclaration.FromDiscardedCards(color)
                : BorrowedPrimaryActionDeclaration.FromActiveCardsWithAnActiveEffect();

            return BorrowedPrimaryActionQuery.EligibleCards(
                declaration,
                context.ActorId,
                context.CardState.Definitions,
                context.CardState.Instances,
                context.Timing == null ? null : context.Timing.Effects,
                context.Capabilities,
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive(
                    context.ActorId,
                    context.Timing == null ? null : context.Timing.ContinuousSources));
        }

        private sealed class BorrowPrimaryActionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BorrowPrimaryActionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            PoolParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(BorrowedActionPool.None)),
                        new EffectCapabilityParameter(
                            ColorParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<CardColor>())
                    },
                    new[]
                    {
                        new EffectOutputBinding(BorrowedCardOutput, EffectValueKind.CardInstance)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// An empty pool is impossible rather than declined, and that is the printed reading rather
            /// than a convenience. None of the three faces prints "you may", "up to" or "if able" on
            /// this step, so p.10 makes it mandatory; the enclosing "If enraged" is what makes the whole
            /// clause skippable, and the user's general conditional ruling is explicit that a false
            /// condition skips its clause and the card continues. So the mandatory step reports that it
            /// cannot happen, and the Conditional above it is what decides whether it was ever asked.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1,
                        1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected != null && selected.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                        ? Candidates(context).SingleOrDefault(card =>
                            card.Id.Equals(selected[0].CardInstanceId))
                        : null;
                if (chosen == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The card is named and nothing else happens here. It is not moved, not flipped and not
                // resolved - the contract's "the borrowed card does not change state" is true because
                // this handler is the only thing that touches it and it touches nothing.
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(BorrowedCardOutput, EffectValue.Card(chosen.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }
    }
}
