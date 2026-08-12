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
        NotApplicable = 1,
        Paused = 2,
        GameEnded = 3
    }

    public sealed class EndOfRoundFixtureCapabilities
    {
        private readonly HashSet<EndOfRoundStage> availableStages;

        public EndOfRoundFixtureCapabilities(IEnumerable<EndOfRoundStage> availableStages)
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
                    "Fixture capabilities may declare only executable end-of-round stages.",
                    nameof(availableStages));
            }
        }

        public static EndOfRoundFixtureCapabilities SyntheticRepeatedRound()
        {
            return new EndOfRoundFixtureCapabilities(new[]
            {
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
            });
        }

        public static EndOfRoundFixtureCapabilities ConfiguredMatch()
        {
            return new EndOfRoundFixtureCapabilities(new[]
            {
                EndOfRoundStage.MinionBattle,
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
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

    public sealed class SyntheticCardRetrievalHandler : IEndOfRoundStageHandler
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
                if (!CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(card))
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
