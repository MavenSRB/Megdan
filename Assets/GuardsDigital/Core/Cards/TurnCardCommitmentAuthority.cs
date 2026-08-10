using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards
{
    public enum TurnCardCommitmentRejectionReason
    {
        None = 0,
        TurnAlreadyRevealed = 1,
        SideAlreadyResponded = 2,
        CardNotFound = 3,
        CardNotOwnedBySide = 4,
        CardNotEligible = 5,
        PassNotAllowed = 6,
        CommittedCardStateChanged = 7
    }

    public enum TurnCardCommitmentEventKind
    {
        None = 0,
        CardCommittedFacedown = 1,
        SidePassed = 2,
        CardRevealed = 3,
        RevealCompleted = 4
    }

    public enum TurnCardCommitmentStatus
    {
        AwaitingResponse = 0,
        CardCommittedFacedown = 1,
        Passed = 2,
        Revealed = 3
    }

    public readonly struct TurnCardCommitmentEvent
    {
        public TurnCardCommitmentEvent(
            TurnCardCommitmentEventKind kind,
            TeamSide teamSide,
            CardInstanceId cardInstanceId = default)
        {
            if (kind == TurnCardCommitmentEventKind.None)
            {
                throw new ArgumentException("A turn-card event requires a defined kind.", nameof(kind));
            }

            if (teamSide != TeamSide.None && teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(teamSide), teamSide, "Turn-card event side is invalid.");
            }

            if (kind == TurnCardCommitmentEventKind.CardRevealed && !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A revealed-card event requires a card instance ID.", nameof(cardInstanceId));
            }

            if (kind != TurnCardCommitmentEventKind.CardRevealed && cardInstanceId.IsDefined)
            {
                throw new ArgumentException("Only revealed-card events disclose a card instance ID.", nameof(cardInstanceId));
            }

            Kind = kind;
            TeamSide = teamSide;
            CardInstanceId = cardInstanceId;
        }

        public TurnCardCommitmentEventKind Kind { get; }

        public TeamSide TeamSide { get; }

        public CardInstanceId CardInstanceId { get; }

        public bool HasCardInstanceId => CardInstanceId.IsDefined;
    }

    public readonly struct TurnCardCommitmentResult
    {
        private TurnCardCommitmentResult(
            bool accepted,
            TurnCardCommitmentRejectionReason rejectionReason,
            IReadOnlyList<TurnCardCommitmentEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
        }

        public bool Accepted { get; }

        public TurnCardCommitmentRejectionReason RejectionReason { get; }

        public IReadOnlyList<TurnCardCommitmentEvent> Events { get; }

        public static TurnCardCommitmentResult Accept(
            IEnumerable<TurnCardCommitmentEvent> events)
        {
            return new TurnCardCommitmentResult(
                true,
                TurnCardCommitmentRejectionReason.None,
                Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray()));
        }

        public static TurnCardCommitmentResult Reject(
            TurnCardCommitmentRejectionReason reason)
        {
            if (reason == TurnCardCommitmentRejectionReason.None)
            {
                throw new ArgumentException("A rejected turn-card command requires a reason.", nameof(reason));
            }

            return new TurnCardCommitmentResult(
                false,
                reason,
                Array.Empty<TurnCardCommitmentEvent>());
        }
    }

    public sealed class TurnCardCommitmentAuthority
    {
        private readonly Dictionary<TeamSide, CardInstanceId> committedCards =
            new Dictionary<TeamSide, CardInstanceId>();
        private readonly HashSet<TeamSide> passedSides = new HashSet<TeamSide>();

        public bool IsRevealed { get; private set; }

        public TurnCardCommitmentStatus GetStatus(TeamSide side)
        {
            ValidateSide(side);
            if (passedSides.Contains(side))
            {
                return TurnCardCommitmentStatus.Passed;
            }

            if (!committedCards.ContainsKey(side))
            {
                return TurnCardCommitmentStatus.AwaitingResponse;
            }

            return IsRevealed
                ? TurnCardCommitmentStatus.Revealed
                : TurnCardCommitmentStatus.CardCommittedFacedown;
        }

        public CardInstanceId? GetRevealedCardInstanceId(TeamSide side)
        {
            ValidateSide(side);
            return IsRevealed && committedCards.TryGetValue(side, out var cardInstanceId)
                ? cardInstanceId
                : (CardInstanceId?)null;
        }

        public bool CanPass(
            TeamSide side,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            ValidateSide(side);
            var instanceArray = GetInputs(instances, participants);
            return GetCommonRejection(side, instanceArray) ==
                    TurnCardCommitmentRejectionReason.None &&
                !instanceArray.Any(instance => IsEligibleForSide(instance, side, participants));
        }

        public TurnCardCommitmentResult TryCommitCard(
            TeamSide side,
            CardInstanceId cardInstanceId,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            ValidateSide(side);
            var instanceArray = GetInputs(instances, participants);
            var commonRejection = GetCommonRejection(side, instanceArray);
            if (commonRejection != TurnCardCommitmentRejectionReason.None)
            {
                return TurnCardCommitmentResult.Reject(commonRejection);
            }

            var card = instanceArray.SingleOrDefault(candidate => candidate.Id.Equals(cardInstanceId));
            if (card == null)
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotFound);
            }

            if (!IsOwnedBySide(card, side, participants))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotOwnedBySide);
            }

            if (!IsEligible(card))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotEligible);
            }

            if (!CardLifecycleAuthority.TryPlayFacedown(card))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotEligible);
            }

            committedCards.Add(side, card.Id);
            var events = new List<TurnCardCommitmentEvent>
            {
                new TurnCardCommitmentEvent(
                    TurnCardCommitmentEventKind.CardCommittedFacedown,
                    side)
            };
            CompleteRevealIfReady(instanceArray, events);
            return TurnCardCommitmentResult.Accept(events);
        }

        public TurnCardCommitmentResult TryPass(
            TeamSide side,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            ValidateSide(side);
            var instanceArray = GetInputs(instances, participants);
            var commonRejection = GetCommonRejection(side, instanceArray);
            if (commonRejection != TurnCardCommitmentRejectionReason.None)
            {
                return TurnCardCommitmentResult.Reject(commonRejection);
            }

            if (instanceArray.Any(instance => IsEligibleForSide(instance, side, participants)))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.PassNotAllowed);
            }

            passedSides.Add(side);
            var events = new List<TurnCardCommitmentEvent>
            {
                new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.SidePassed, side)
            };
            CompleteRevealIfReady(instanceArray, events);
            return TurnCardCommitmentResult.Accept(events);
        }

        private TurnCardCommitmentRejectionReason GetCommonRejection(
            TeamSide side,
            IReadOnlyList<CardInstance> instances)
        {
            if (IsRevealed)
            {
                return TurnCardCommitmentRejectionReason.TurnAlreadyRevealed;
            }

            if (HasResponded(side))
            {
                return TurnCardCommitmentRejectionReason.SideAlreadyResponded;
            }

            foreach (var committedCardId in committedCards.Values)
            {
                var committedCard = instances.SingleOrDefault(card => card.Id.Equals(committedCardId));
                if (committedCard == null || !IsFacedownCommitted(committedCard))
                {
                    return TurnCardCommitmentRejectionReason.CommittedCardStateChanged;
                }
            }

            return TurnCardCommitmentRejectionReason.None;
        }

        private void CompleteRevealIfReady(
            IReadOnlyList<CardInstance> instances,
            ICollection<TurnCardCommitmentEvent> events)
        {
            if (!HasResponded(TeamSide.Red) || !HasResponded(TeamSide.Blue))
            {
                return;
            }

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                if (!committedCards.TryGetValue(side, out var cardId))
                {
                    continue;
                }

                var card = instances.Single(candidate => candidate.Id.Equals(cardId));
                if (!CardLifecycleAuthority.TryReveal(card))
                {
                    throw new InvalidOperationException("A validated facedown commitment could not be revealed.");
                }

                events.Add(new TurnCardCommitmentEvent(
                    TurnCardCommitmentEventKind.CardRevealed,
                    side,
                    cardId));
            }

            IsRevealed = true;
            events.Add(new TurnCardCommitmentEvent(
                TurnCardCommitmentEventKind.RevealCompleted,
                TeamSide.None));
        }

        private bool HasResponded(TeamSide side)
        {
            return committedCards.ContainsKey(side) || passedSides.Contains(side);
        }

        private static CardInstance[] GetInputs(
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var instanceArray = instances.ToArray();
            if (instanceArray.Any(instance => instance == null))
            {
                throw new ArgumentException("Turn-card instances must not contain null entries.", nameof(instances));
            }

            return instanceArray;
        }

        private static bool IsEligibleForSide(
            CardInstance card,
            TeamSide side,
            MatchParticipants participants)
        {
            return IsOwnedBySide(card, side, participants) && IsEligible(card);
        }

        private static bool IsOwnedBySide(
            CardInstance card,
            TeamSide side,
            MatchParticipants participants)
        {
            try
            {
                return participants.GetTeamForHero(card.OwnerId).Side == side;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        private static bool IsEligible(CardInstance card)
        {
            return card.State.Equals(CardStateFacts.InitialInHand());
        }

        private static bool IsFacedownCommitted(CardInstance card)
        {
            return card.State.Location == CardLocation.TurnArea &&
                card.State.HasBeenPlayed &&
                card.State.ResolutionStatus == CardResolutionStatus.Unresolved &&
                card.State.FaceOrientation == CardFaceOrientation.FaceDown;
        }

        private static void ValidateSide(TeamSide side)
        {
            if (side != TeamSide.Red && side != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Turn-card side must be Red or Blue.");
            }
        }
    }
}
