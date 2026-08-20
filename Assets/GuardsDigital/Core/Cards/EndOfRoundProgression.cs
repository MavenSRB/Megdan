using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum EndOfRoundStage
    {
        MinionBattle = 0,
        RemoveTokensAndReturnMarkers = 1,
        ResolveRequestedLanePush = 2,
        RetrieveCardsAndCancelActiveEffects = 3,
        LevelUpAndUpgrade = 4,
        CollectPityCoins = 5,
        Complete = 6
    }

    public enum EndOfRoundStageOutcomeKind
    {
        Completed = 0,

        /// <summary>
        /// This match does not run the step at all - not "the step ran and had nothing to do", which
        /// is <see cref="Completed"/>.
        ///
        /// Milestone 3.8c introduced it for the systems p.8 names and the build did not have yet, and
        /// milestone 7.8 built the last of them. No configured match can report it any more:
        /// <see cref="EndOfRoundStageAvailability.ConfiguredMatch"/> declares all six ordered steps.
        /// It remains reachable, and meant, for the board-free synthetic round, which has no Lane, no
        /// board and no space a token could stand on.
        /// </summary>
        NotApplicable = 1,
        Paused = 2,
        GameEnded = 3
    }

    /// <summary>
    /// Which of rulebook p.8's end-of-round stages a match runs, declared rather than inferred.
    ///
    /// Milestone 6.13's naming audit renamed this from `EndOfRoundFixtureCapabilities`: a configured
    /// production match selects <see cref="ConfiguredMatch"/> through
    /// <see cref="RoundResolutionComposition.ForMatch"/>, so the type was never fixture-only and the
    /// qualifier was a claim about reach that had stopped being true. The one factory that is still
    /// fixture-only keeps its name and states its limit at the declaration.
    /// </summary>
    public sealed class EndOfRoundStageAvailability
    {
        private readonly HashSet<EndOfRoundStage> availableStages;

        public EndOfRoundStageAvailability(IEnumerable<EndOfRoundStage> availableStages)
        {
            if (availableStages == null)
            {
                throw new ArgumentNullException(nameof(availableStages));
            }

            this.availableStages = new HashSet<EndOfRoundStage>(availableStages);
            if (this.availableStages.Contains(EndOfRoundStage.Complete) ||
                this.availableStages.Any(stage => !Enum.IsDefined(typeof(EndOfRoundStage), stage)))
            {
                throw new ArgumentException(
                    "End-of-round stage availability may declare only executable stages.",
                    nameof(availableStages));
            }
        }

        /// <summary>
        /// The board-free fixture shape, and the one declaration whose `Synthetic` qualifier milestone
        /// 6.13 deliberately kept, because the limitation it names is real and precise: it admits the
        /// retrieval stage alone. A Minion Battle needs a board, the cleanup needs spaces a token could
        /// stand on, and a Lane Push needs a Lane, so a synthetic repeated round declares none of them
        /// and proves retrieval on its own. No configured match reaches this factory.
        /// </summary>
        public static EndOfRoundStageAvailability SyntheticRepeatedRound()
        {
            return new EndOfRoundStageAvailability(new[]
            {
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
            });
        }

        public static EndOfRoundStageAvailability ConfiguredMatch()
        {
            return new EndOfRoundStageAvailability(new[]
            {
                EndOfRoundStage.MinionBattle,

                // Milestone 6.12. Declared here and not in the board-free factory below: p.8 runs the
                // cleanup in every round, but a fixture with no board has no space a token could be on,
                // and the synthetic round deliberately proves the retrieval stage alone.
                EndOfRoundStage.RemoveTokensAndReturnMarkers,

                // Milestone 7.8. p.8's push step, which milestone 6.11 implemented and this
                // declaration then contradicted: the stage ran whenever the Minion Battle had asked
                // for a push, because the cursor routed it on the request rather than on this list,
                // and a round that awarded none reported the step *unavailable* instead. A configured
                // match has a Lane, Wave counters and a push rule, so the step is available in every
                // round of it; whether there is a push to resolve is the step's own answer.
                EndOfRoundStage.ResolveRequestedLanePush,
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,

                // Milestone 7.5. p.8 lists "Level Up" among the end-of-round steps and p.15 makes it
                // mandatory; the stage has been ordered here since milestone 6.1 and unavailable until
                // now. It follows retrieval because p.15's Upgrade a Card step chooses from the hand,
                // and the cards a hero played this round are only back in it once retrieval has run.
                EndOfRoundStage.LevelUpAndUpgrade,

                // Milestone 7.6. p.8's last end-of-round step, and the last one that was ordered and
                // unavailable. It follows the Level Up above it because its own question is about that
                // step's outcome - "All players who have NOT leveled up this round" - so a Pity Coin
                // paid before leveling would be paid on an answer nobody had yet.
                EndOfRoundStage.CollectPityCoins
            });
        }

        public bool IsAvailable(EndOfRoundStage stage)
        {
            return availableStages.Contains(stage);
        }
    }

    public readonly struct CardRetrievalFact
    {
        public CardRetrievalFact(CardInstanceId cardInstanceId, HeroId ownerId)
        {
            if (!cardInstanceId.IsDefined || !ownerId.IsDefined)
            {
                throw new ArgumentException("A card retrieval fact requires defined card and owner IDs.");
            }

            CardInstanceId = cardInstanceId;
            OwnerId = ownerId;
        }

        public CardInstanceId CardInstanceId { get; }

        public HeroId OwnerId { get; }
    }

    public readonly struct EndOfRoundStageResolution
    {
        private EndOfRoundStageResolution(
            EndOfRoundStageOutcomeKind outcomeKind,
            IEnumerable<CardRetrievalFact> retrievedCards,
            bool activeEffectCancellationBoundaryReached)
        {
            OutcomeKind = outcomeKind;
            RetrievedCards = Array.AsReadOnly(
                (retrievedCards ?? throw new ArgumentNullException(nameof(retrievedCards))).ToArray());
            ActiveEffectCancellationBoundaryReached = activeEffectCancellationBoundaryReached;
        }

        public EndOfRoundStageOutcomeKind OutcomeKind { get; }

        public IReadOnlyList<CardRetrievalFact> RetrievedCards { get; }

        public bool ActiveEffectCancellationBoundaryReached { get; }

        public static EndOfRoundStageResolution Completed(
            IEnumerable<CardRetrievalFact> retrievedCards = null,
            bool activeEffectCancellationBoundaryReached = false)
        {
            return new EndOfRoundStageResolution(
                EndOfRoundStageOutcomeKind.Completed,
                retrievedCards ?? Array.Empty<CardRetrievalFact>(),
                activeEffectCancellationBoundaryReached);
        }

        public static EndOfRoundStageResolution Paused()
        {
            return new EndOfRoundStageResolution(
                EndOfRoundStageOutcomeKind.Paused,
                Array.Empty<CardRetrievalFact>(),
                false);
        }

        public static EndOfRoundStageResolution NotApplicable()
        {
            return new EndOfRoundStageResolution(
                EndOfRoundStageOutcomeKind.NotApplicable,
                Array.Empty<CardRetrievalFact>(),
                false);
        }

        public static EndOfRoundStageResolution GameEnded()
        {
            return new EndOfRoundStageResolution(
                EndOfRoundStageOutcomeKind.GameEnded,
                Array.Empty<CardRetrievalFact>(),
                false);
        }
    }

    public readonly struct EndOfRoundStageOutcome
    {
        public EndOfRoundStageOutcome(EndOfRoundStage stage, EndOfRoundStageOutcomeKind kind)
        {
            if (stage == EndOfRoundStage.Complete)
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            Stage = stage;
            Kind = kind;
        }

        public EndOfRoundStage Stage { get; }

        public EndOfRoundStageOutcomeKind Kind { get; }
    }

    public sealed class EndOfRoundStageContext
    {
        public EndOfRoundStageContext(
            int roundIndex,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants)
        {
            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            RoundIndex = roundIndex;
            Instances = instances ?? throw new ArgumentNullException(nameof(instances));
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
        }

        public int RoundIndex { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public MatchParticipants Participants { get; }
    }

    public interface IEndOfRoundStageHandler
    {
        EndOfRoundStageResolution TryResolve(EndOfRoundStageContext context);
    }

    /// <summary>
    /// Rulebook p.8's "Retrieve Cards": every card a hero resolved or discarded this round returns to
    /// that hero's hand, in stable order, with one fact per card that moved.
    ///
    /// Milestone 3.8c wrote this behaviour and milestone 6.13 renamed it from
    /// `SyntheticCardRetrievalHandler` without touching it. The audit's finding was that the qualifier
    /// described the handler's callers at the time it was written rather than its callers now: a
    /// configured match composes this handler and no other, and the configured scene has depended on it
    /// to play repeated rounds since Phase 3. What is general here is the whole of it - the ordering,
    /// the owner validation, the two eligible shapes, and the one cancellation boundary it reports.
    /// </summary>
    public sealed class CardRetrievalHandler : IEndOfRoundStageHandler
    {
        public EndOfRoundStageResolution TryResolve(EndOfRoundStageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var retrieved = new List<CardRetrievalFact>();
            foreach (var card in context.Instances.OrderBy(instance => instance.Id.Value, StringComparer.Ordinal))
            {
                context.Participants.GetHero(card.OwnerId);
                if (!CardLifecycleAuthority.TryRetrieveEligibleCard(card))
                {
                    continue;
                }

                retrieved.Add(new CardRetrievalFact(card.Id, card.OwnerId));
            }

            return EndOfRoundStageResolution.Completed(retrieved, true);
        }
    }

    public sealed class CompletedRoundRecord
    {
        public CompletedRoundRecord(
            int roundIndex,
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<EndOfRoundStageOutcome> endOfRoundOutcomes)
        {
            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            if (turnSlots == null)
            {
                throw new ArgumentNullException(nameof(turnSlots));
            }

            var slots = turnSlots.ToArray();
            if (slots.Length != RoundResolutionAuthority.TurnsPerRound ||
                slots.Any(slot => slot == null || slot.RoundIndex != roundIndex) ||
                !slots.Select(slot => slot.TurnIndex).SequenceEqual(
                    Enumerable.Range(1, RoundResolutionAuthority.TurnsPerRound)))
            {
                throw new ArgumentException(
                    "A completed round record requires four ordered slots from the same round.",
                    nameof(turnSlots));
            }

            if (endOfRoundOutcomes == null)
            {
                throw new ArgumentNullException(nameof(endOfRoundOutcomes));
            }

            var outcomes = endOfRoundOutcomes.ToArray();
            var executableStages = Enum.GetValues(typeof(EndOfRoundStage))
                .Cast<EndOfRoundStage>()
                .Where(stage => stage != EndOfRoundStage.Complete)
                .ToArray();
            if (outcomes.Length != executableStages.Length ||
                !outcomes.Select(outcome => outcome.Stage).SequenceEqual(executableStages) ||
                outcomes.Any(outcome =>
                    outcome.Kind == EndOfRoundStageOutcomeKind.Paused ||
                    outcome.Kind == EndOfRoundStageOutcomeKind.GameEnded))
            {
                throw new ArgumentException(
                    "A completed round record requires one terminal outcome for every ordered stage.",
                    nameof(endOfRoundOutcomes));
            }

            RoundIndex = roundIndex;
            TurnSlots = Array.AsReadOnly(slots);
            EndOfRoundOutcomes = Array.AsReadOnly(outcomes);
        }

        public int RoundIndex { get; }

        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }

        public IReadOnlyList<EndOfRoundStageOutcome> EndOfRoundOutcomes { get; }
    }
}
