using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 7.1a: the capabilities that read and move card state from inside an effect.
    ///
    /// This is the first handler family whose candidates come out of a hand rather than off the board,
    /// and it is what makes milestone 7.1's <see cref="ColorRestrictedDiscard"/> reachable from a card:
    /// that type composed the characteristic query, the frame's own pending-choice shape, and the
    /// in-hand discard, but nothing could call it, because the frame carried card definitions and not
    /// card instances once a Skill had started. It has a production consumer now, and this is it - the
    /// composition is not duplicated here, only bound.
    ///
    /// It is proved on a fixture Skill card rather than a configured one, exactly as milestones 6.2,
    /// 6.4 and 6.7 proved their capabilities before any configured card exposed them. No configured
    /// card prints a discard, milestone 7.9 gates the complete ten-card matrix, and the verified hero
    /// content that needs this is milestone 8.1's. The registration is what matters for 7.9's own
    /// proof: the reusable seam exists before the verified card that binds it, rather than arriving
    /// with it.
    /// </summary>
    public static class SyntheticCardStateCapabilities
    {
        public static EffectCapabilityId DiscardChosenCard { get; } =
            new EffectCapabilityId("synthetic-discard-chosen-card");

        public static EffectCapabilityReference DiscardChosenCardV1 =>
            new EffectCapabilityReference(DiscardChosenCard, 1);

        /// <summary>The parameter naming the hero whose hand is read and whose decision this is.</summary>
        public const string HeroParameter = "hero";

        /// <summary>The card this capability discarded, so a later node can bind it.</summary>
        public const string DiscardedOutput = "discarded";

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new DiscardChosenCardHandler();
        }

        /// <summary>
        /// Requires the hero its input names to discard one card of their own choosing.
        ///
        /// Three things about it are the milestone rather than incidental. The chooser is the named
        /// hero and not the actor, so the pending choice records whose decision it is and the resolver
        /// refuses an answer submitted for anyone else. The candidates are derived in that hero's own
        /// context - their hand, read through the same query, with the same information suppression -
        /// rather than in the actor's. And nothing is persisted: the set is derived when the choice
        /// opens, derived again when the answer arrives, and derived a third time when a snapshot
        /// restores the pause, so a card that left the hand in between is not discarded on the strength
        /// of an offer that has gone stale.
        /// </summary>
        private sealed class DiscardChosenCardHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => DiscardChosenCardV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero) },
                new[] { new EffectOutputBinding(DiscardedOutput, EffectValueKind.CardInstance) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var offer = ColorRestrictedDiscard.Offer(
                    Criteria(chooser),
                    EffectChooserRole.None,
                    context.CardState.Definitions,
                    context.CardState.Instances,
                    chooser);

                // Rulebook `if able`: a hero with no card to give does not give one, and the action
                // goes on. This is deliberately not ImpossibleMandatory - that would stop the enclosing
                // sequence, which is what "cannot be done" means for a step that had to happen, and an
                // empty hand is an ordinary legal outcome rather than a failure. The two stay
                // distinguishable in the recorded stream without a fifth result kind: declining here
                // opens no choice, so no choice-requested fact is written, while a chooser who declines
                // an optional step they could have taken has one recorded before the decline.
                return offer == null
                    ? EffectExecutionResult.DeclinedOptional()
                    : EffectExecutionResult.NeedsChoice(offer);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.CardInstance)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived, not trusted. The answer is checked against the hand as it stands now, so
                // a card discarded, played, or otherwise moved between the offer and the answer is
                // refused rather than discarded a second time. Nothing is mutated on that path.
                var result = ColorRestrictedDiscard.Resolve(
                    Criteria(Chooser(context)),
                    selected[0].CardInstanceId,
                    context.CardState.Definitions,
                    context.CardState.Instances);

                return result.Accepted
                    ? EffectExecutionResult.Applied(
                        new[]
                        {
                            new EffectOutcomeFact(
                                DiscardedOutput, EffectValue.Card(result.DiscardedCardId))
                        })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.DeclinedOptional
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                if (!chooser.IsDefined)
                {
                    return Array.Empty<EffectValue>();
                }

                return CardCharacteristicQuery
                    .FindCandidates(
                        Criteria(chooser),
                        context.CardState.Definitions,
                        context.CardState.Instances)
                    .Select(EffectValue.Card)
                    .ToArray();
            }

            /// <summary>
            /// The hero whose hand this reads. Their own input, not the actor: the whole point of the
            /// capability is that the hero who must discard is someone the card names.
            /// </summary>
            private static HeroId Chooser(EffectCapabilityContext context)
            {
                var named = context.Input(HeroParameter);
                return named.Kind == EffectValueKind.Hero ? named.HeroId : default;
            }

            /// <summary>
            /// One statement of what may be discarded, so the offer, the probe, and the commit cannot
            /// disagree. An unplayed card in that hero's own hand - the source state rulebook p.15's
            /// discard starts from, and the state the in-hand mutation will accept.
            /// </summary>
            private static CardCharacteristicCriteria Criteria(HeroId chooser) =>
                new CardCharacteristicCriteria(
                    ownerId: chooser,
                    locations: new[] { CardLocation.Hand },
                    hasBeenPlayed: false);
        }
    }
}
