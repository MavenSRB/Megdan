using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.61g's <c>borrow-after-attack-text@1</c>: the one capability that names the card
    /// whose "After the attack" text is about to be applied inside somebody else's Attack.
    ///
    /// <para><b>It chooses the card and stops there</b>, which is milestone 8.35z31's shape exactly and
    /// for the same stated reason: a capability handler mutates the frame it is in, and a borrowed
    /// rider is a whole rulebook p.14 card-text application with its own pauses and its own remainder,
    /// so applying it belongs to the authority that applies every other card text.
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.BorrowedCardTextQuery"/> is how that
    /// authority reads the request back - the direct twin of
    /// <c>BorrowedActionQuery.Requested</c>, and separate from it because the two answer different
    /// questions about a completed frame: one asks which card's ACTION to start, this one asks which
    /// card's TEXT to apply.</para>
    ///
    /// <para><b>It asks nothing when the clause names one card.</b> "the \"After the attack\" text of
    /// your resolved or discarded red card" is a definite description, not a choice: Min holds one card
    /// of the Red line in a round, so the pool is at most one and inventing a one-candidate prompt
    /// would put a question on the table that the card does not print. Where the pool is empty the step
    /// is p.10's impossible one - the printed clause carries no "may", no "up to" and no "if able" - and
    /// the Attack that carried it is completely unaffected, because an impossible step inside step 7's
    /// text stops that text and nothing else. Where the pool somehow held two, p.10 gives the
    /// undesignated decision to the card's controller and the choice is opened for exactly that.</para>
    ///
    /// <para><b>Deferred, not registered</b>, for the reason every unreachable capability in this build
    /// is: the registered set is serialized, and the only content naming this is a hero this build does
    /// not deal. 8.61j is the block that flips it, and <c>MinBorrowedRiderTests</c> asserts the gate
    /// from both sides until then.</para>
    /// </summary>
    public static class SyntheticBorrowedCardTextCapabilities
    {
        public static EffectCapabilityId BorrowAfterAttackText { get; } =
            new EffectCapabilityId("borrow-after-attack-text");

        public static EffectCapabilityReference BorrowAfterAttackTextV1 =>
            new EffectCapabilityReference(BorrowAfterAttackText, 1);

        /// <summary>Which pool the clause reaches, as a closed vocabulary.</summary>
        public const string PoolParameter = "pool";

        /// <summary>
        /// The colour the clause narrows its pool to, as a closed vocabulary including
        /// <see cref="CardColor.None"/> for a face that narrows nothing. Required rather than optional
        /// so that a composition cannot narrow nothing by accident.
        /// </summary>
        public const string ColorParameter = "color";

        /// <summary>The card whose printed "After the attack" text is about to be applied.</summary>
        public const string BorrowedCardOutput = "borrowedCard";

        /// <summary>
        /// The handler this build does NOT register. Kept in a deferred list the way
        /// <c>BainComponentContent.DeferredHandlers</c> and
        /// <c>SyntheticPlacementCapabilities.DeferredHandlers</c> are, so the offline content tests can
        /// compose a catalog of their own from it without any of it becoming reachable.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[] { new BorrowAfterAttackTextHandler() };

        /// <summary>
        /// The candidate set one rider-borrow node would name right now, as a function of state alone.
        ///
        /// <para>Exposed beside the handler for milestone 8.35z15's reason: a card-text step with no
        /// legal subject is not offered, and feasibility must ask the same function the execution does
        /// or the two can disagree.</para>
        /// </summary>
        public static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
        {
            if (context == null || !context.CardState.IsAvailable || !context.ActorId.IsDefined)
            {
                return Array.Empty<CardInstance>();
            }

            var pool = (BorrowedCardTextPool)context.Input(PoolParameter).IntegerValue;
            var color = (CardColor)context.Input(ColorParameter).IntegerValue;
            if (pool != BorrowedCardTextPool.OwnedResolvedOrDiscardedCards ||
                !Enum.IsDefined(typeof(CardColor), color))
            {
                return Array.Empty<CardInstance>();
            }

            return BorrowedCardTextDeclaration.EligibleCards(
                BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(color),
                context.ActorId,
                context.CardState.Definitions,
                context.CardState.Instances,
                context.SourceCardInstanceId);
        }

        private sealed class BorrowAfterAttackTextHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BorrowAfterAttackTextV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            PoolParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(BorrowedCardTextPool.None)),
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

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The definite description: one card is named, not chosen. See the type's remarks.
                if (candidates.Count == 1)
                {
                    return Named(candidates[0]);
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
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
                return chosen == null ? EffectExecutionResult.ImpossibleMandatory() : Named(chosen);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var count = Candidates(context).Count;
                if (count == 0) return EffectExecutionResultKind.ImpossibleMandatory;
                return count == 1
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
            }

            /// <summary>
            /// The card is named and nothing else happens here. It is not moved, not flipped and not
            /// resolved - the contract's "the borrowed card does not change state" is true because this
            /// handler is the only thing that touches it and it touches nothing.
            /// </summary>
            private static EffectExecutionResult Named(CardInstance card) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(BorrowedCardOutput, EffectValue.Card(card.Id))
                });
        }
    }
}
