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

        /// <summary>
        /// Revision 2, minted at milestone 8.10: the same discard, reporting whether it happened.
        ///
        /// <para>Both revisions stay live, the way <c>ChooseTargetV3</c> and <c>V4</c> do, because they
        /// are two different contracts rather than two versions of one. Revision 1 is rulebook p.17's
        /// `if able`: a hero with no card to give simply gives none, the step declines, and nothing
        /// downstream is told - which is exactly right for Shock and Electrocute, whose sentence ends
        /// there. Revision 2 is for a sentence that does <em>not</em> end there. Electroblast prints
        /// "discards a card, <b>or is defeated</b>", which ledger question 13 reads as one mandatory
        /// step with two ways to complete, so the step always applies and says which way it went.</para>
        ///
        /// <para>That is why revision 2 never declines. A declined step publishes nothing by design,
        /// and a rider whose second arm has to know that the first one could not happen needs to be
        /// told.</para>
        /// </summary>
        public static EffectCapabilityReference DiscardChosenCardV2 =>
            new EffectCapabilityReference(DiscardChosenCard, 2);

        /// <summary>The parameter naming the hero whose hand is read and whose decision this is.</summary>
        public const string HeroParameter = "hero";

        /// <summary>The card this capability discarded, so a later node can bind it.</summary>
        public const string DiscardedOutput = "discarded";

        /// <summary>
        /// Revision 2's answer to whether the named hero had a card to give at all. Milestone 8.10.
        /// </summary>
        public const string UnableToDiscardOutput = "unableToDiscard";

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new DiscardChosenCardHandler(DiscardChosenCardV1, reportsInability: false);
            yield return new DiscardChosenCardHandler(DiscardChosenCardV2, reportsInability: true);
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
            private readonly bool reportsInability;

            public DiscardChosenCardHandler(
                EffectCapabilityReference reference, bool reportsInability)
            {
                Reference = reference;
                this.reportsInability = reportsInability;
                Schema = new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero) },
                    reportsInability
                        ? new[]
                        {
                            new EffectOutputBinding(DiscardedOutput, EffectValueKind.CardInstance),
                            new EffectOutputBinding(UnableToDiscardOutput, EffectValueKind.Boolean)
                        }
                        : new[] { new EffectOutputBinding(DiscardedOutput, EffectValueKind.CardInstance) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);

                // A SENTENCE WHOSE SUBJECT WAS NEVER NAMED HAS NOTHING TO DO, AND IT HAS TO SAY SO
                // RATHER THAN THROW. This capability's hero is routinely bound to the output of a
                // preceding selection - Misa's Power Overwhelming composes "an enemy hero adjacent to
                // you discards a card, if able" as exactly that - and such a selection is written
                // `EffectFailurePolicy.IfAble` precisely because it may name nobody. When it does, this
                // input arrives unbound, and <see cref="Criteria"/> refuses an undefined owner by
                // construction.
                //
                // A THROW IS NOT A FAILURE RESULT, so the node's own failure policy cannot absorb one.
                // It escapes the whole resolution, faults the command at the aggregate's commit
                // boundary, and the match is rolled back to a state byte-identical to the one the
                // player was already looking at - which is indistinguishable, at the surface, from an
                // offered control that simply does nothing when pressed. The answer was legal and had
                // already resolved; only the trigger that ran after it faulted.
                //
                // <see cref="AnticipateCandidates"/> has stated this same rule since milestone 8.21,
                // which is why every OFFER derived from an unnamed subject was already correct and only
                // the executing path faulted. This is that rule stated on the path that resolves, and
                // it is the whole of the fix.
                //
                // DeclinedOptional rather than the empty-hand arm below, and the difference is the
                // point: an empty hand is a real hero who was unable, which revision 2 reports through
                // UnableToDiscardOutput; no hero at all is nobody to report anything about, so this
                // publishes no fact and a reader binding that output reads the absence it already reads
                // as false.
                if (!chooser.IsDefined)
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

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
                if (offer != null)
                {
                    return EffectExecutionResult.NeedsChoice(offer);
                }

                // Milestone 8.10. Revision 2 answers the same empty hand differently, and the
                // difference is the whole of what it exists for: the step applies, reporting that the
                // hero had nothing to give, so the sentence's second arm can read it. Ledger question
                // 13 is why that is not a decline - "discards a card, or is defeated" is one mandatory
                // step with two ways to complete, and a step that declined would have completed
                // neither.
                return reportsInability
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(UnableToDiscardOutput, EffectValue.Boolean(true))
                    })
                    : EffectExecutionResult.DeclinedOptional();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.CardInstance)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The same unnamed subject, refused rather than thrown. This path is only reached when
                // Execute opened a choice, which now requires a defined chooser - so an answer arriving
                // against an unbound frame is a stale or tampered one, and it is refused loudly instead
                // of faulting the command that carries it.
                var chooser = Chooser(context);
                if (!chooser.IsDefined)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived, not trusted. The answer is checked against the hand as it stands now, so
                // a card discarded, played, or otherwise moved between the offer and the answer is
                // refused rather than discarded a second time. Nothing is mutated on that path.
                var result = ColorRestrictedDiscard.Resolve(
                    Criteria(chooser),
                    selected[0].CardInstanceId,
                    context.CardState.Definitions,
                    context.CardState.Instances);

                if (!result.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(DiscardedOutput, EffectValue.Card(result.DiscardedCardId))
                };
                if (reportsInability)
                {
                    // Published on both arms rather than only on the empty one, so a reader binding it
                    // gets an answer whatever happened instead of an absence it has to interpret.
                    facts.Add(new EffectOutcomeFact(
                        UnableToDiscardOutput, EffectValue.Boolean(false)));
                }

                return EffectExecutionResult.Applied(facts);
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

            /// <summary>One card, which is what the discard offer requests. Milestone 8.35l.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

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
