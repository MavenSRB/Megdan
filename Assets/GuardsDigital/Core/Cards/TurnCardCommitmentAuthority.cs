using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum TurnCardCommitmentRejectionReason
    {
        None = 0,
        TurnAlreadyRevealed = 1,
        HeroAlreadyResponded = 2,
        CardNotFound = 3,
        CardNotOwnedByHero = 4,
        CardNotEligible = 5,
        PassNotAllowed = 6,
        CommittedCardStateChanged = 7,
        HeroNotFound = 8,
        InvalidInHandCardState = 9
    }

    public enum TurnCardCommitmentEventKind
    {
        None = 0,
        CardCommittedFacedown = 1,
        HeroPassed = 2,
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
            HeroId heroId = default,
            CardInstanceId cardInstanceId = default)
        {
            if (kind == TurnCardCommitmentEventKind.None)
            {
                throw new ArgumentException("A turn-card event requires a defined kind.", nameof(kind));
            }

            var isHeroEvent = kind != TurnCardCommitmentEventKind.RevealCompleted;
            if (isHeroEvent != heroId.IsDefined)
            {
                throw new ArgumentException(
                    "Every per-hero turn-card event requires one canonical hero ID, while reveal completion does not.",
                    nameof(heroId));
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
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
        }

        public TurnCardCommitmentEventKind Kind { get; }

        public HeroId HeroId { get; }

        public CardInstanceId CardInstanceId { get; }

        public bool HasHeroId => HeroId.IsDefined;

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

        public static TurnCardCommitmentResult Accept(IEnumerable<TurnCardCommitmentEvent> events)
        {
            return new TurnCardCommitmentResult(
                true,
                TurnCardCommitmentRejectionReason.None,
                Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray()));
        }

        public static TurnCardCommitmentResult Reject(TurnCardCommitmentRejectionReason reason)
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
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly IReadOnlyList<HeroId> expectedHeroIds;
        private readonly Dictionary<HeroId, CardInstanceId> committedCards =
            new Dictionary<HeroId, CardInstanceId>();
        private readonly HashSet<HeroId> passedHeroes = new HashSet<HeroId>();

        public TurnCardCommitmentAuthority(
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

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) ||
                cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Turn-card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            foreach (var card in cards)
            {
                participants.GetHero(card.OwnerId);
            }

            this.instances = Array.AsReadOnly(cards);
            expectedHeroIds = Array.AsReadOnly(participants.Heroes
                .Select(hero => hero.Id)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public bool IsRevealed { get; private set; }

        public IReadOnlyList<HeroId> ExpectedHeroIds => expectedHeroIds;

        internal IReadOnlyDictionary<HeroId, CardInstanceId> CommittedCards => committedCards;

        internal IReadOnlyCollection<HeroId> PassedHeroes => passedHeroes;

        internal static TurnCardCommitmentAuthority Restore(
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<KeyValuePair<HeroId, CardInstanceId>> commitments,
            IEnumerable<HeroId> passes,
            bool isRevealed)
        {
            var authority = new TurnCardCommitmentAuthority(instances, participants);
            foreach (var pair in commitments ?? throw new ArgumentNullException(nameof(commitments)))
            {
                if (!authority.IsExpectedHero(pair.Key) || !pair.Value.IsDefined ||
                    authority.committedCards.ContainsKey(pair.Key))
                {
                    throw new ArgumentException("Restored commitments contain invalid or duplicate identities.", nameof(commitments));
                }

                authority.committedCards.Add(pair.Key, pair.Value);
            }

            foreach (var heroId in passes ?? throw new ArgumentNullException(nameof(passes)))
            {
                if (!authority.IsExpectedHero(heroId) || authority.committedCards.ContainsKey(heroId) ||
                    !authority.passedHeroes.Add(heroId))
                {
                    throw new ArgumentException("Restored passes contain invalid or duplicate identities.", nameof(passes));
                }
            }

            if (isRevealed && authority.expectedHeroIds.Any(heroId => !authority.HasResponded(heroId)))
            {
                throw new ArgumentException("A revealed restored commitment requires every hero response.", nameof(isRevealed));
            }

            authority.IsRevealed = isRevealed;
            return authority;
        }

        public TurnCardCommitmentStatus GetStatus(HeroId heroId)
        {
            if (!IsExpectedHero(heroId))
            {
                throw new KeyNotFoundException($"Unknown turn-card hero ID '{heroId}'.");
            }

            if (passedHeroes.Contains(heroId))
            {
                return TurnCardCommitmentStatus.Passed;
            }

            if (!committedCards.ContainsKey(heroId))
            {
                return TurnCardCommitmentStatus.AwaitingResponse;
            }

            return IsRevealed
                ? TurnCardCommitmentStatus.Revealed
                : TurnCardCommitmentStatus.CardCommittedFacedown;
        }

        public CardInstanceId? GetRevealedCardInstanceId(HeroId heroId)
        {
            if (!IsExpectedHero(heroId))
            {
                throw new KeyNotFoundException($"Unknown turn-card hero ID '{heroId}'.");
            }

            return IsRevealed && committedCards.TryGetValue(heroId, out var cardInstanceId)
                ? cardInstanceId
                : (CardInstanceId?)null;
        }

        public bool CanPass(HeroId heroId)
        {
            if (GetCommonRejection(heroId) != TurnCardCommitmentRejectionReason.None)
            {
                return false;
            }

            return GetOwnedHandState(heroId) == OwnedHandState.Empty;
        }

        public TurnCardCommitmentResult TryCommitCard(
            HeroId heroId,
            CardInstanceId cardInstanceId)
        {
            var commonRejection = GetCommonRejection(heroId);
            if (commonRejection != TurnCardCommitmentRejectionReason.None)
            {
                return TurnCardCommitmentResult.Reject(commonRejection);
            }

            if (GetOwnedHandState(heroId) == OwnedHandState.Invalid)
            {
                return TurnCardCommitmentResult.Reject(
                    TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            }

            var card = instances.SingleOrDefault(candidate => candidate.Id.Equals(cardInstanceId));
            if (card == null)
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotFound);
            }

            if (!card.OwnerId.Equals(heroId))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotOwnedByHero);
            }

            if (card.State.Location == CardLocation.Hand && !IsEligible(card))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            }

            if (!IsEligible(card) || !CardLifecycleAuthority.TryPlayFacedown(card))
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotEligible);
            }

            committedCards.Add(heroId, card.Id);
            var events = new List<TurnCardCommitmentEvent>
            {
                new TurnCardCommitmentEvent(
                    TurnCardCommitmentEventKind.CardCommittedFacedown,
                    heroId)
            };
            CompleteRevealIfReady(events);
            return TurnCardCommitmentResult.Accept(events);
        }

        public TurnCardCommitmentResult TryPass(HeroId heroId)
        {
            var commonRejection = GetCommonRejection(heroId);
            if (commonRejection != TurnCardCommitmentRejectionReason.None)
            {
                return TurnCardCommitmentResult.Reject(commonRejection);
            }

            var handState = GetOwnedHandState(heroId);
            if (handState == OwnedHandState.Invalid)
            {
                return TurnCardCommitmentResult.Reject(
                    TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            }

            if (handState == OwnedHandState.HasEligibleCard)
            {
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.PassNotAllowed);
            }

            passedHeroes.Add(heroId);
            var events = new List<TurnCardCommitmentEvent>
            {
                new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.HeroPassed, heroId)
            };
            CompleteRevealIfReady(events);
            return TurnCardCommitmentResult.Accept(events);
        }

        private TurnCardCommitmentRejectionReason GetCommonRejection(HeroId heroId)
        {
            if (IsRevealed)
            {
                return TurnCardCommitmentRejectionReason.TurnAlreadyRevealed;
            }

            if (!IsExpectedHero(heroId))
            {
                return TurnCardCommitmentRejectionReason.HeroNotFound;
            }

            if (HasResponded(heroId))
            {
                return TurnCardCommitmentRejectionReason.HeroAlreadyResponded;
            }

            foreach (var committedCardId in committedCards.Values)
            {
                var committedCard = instances.Single(card => card.Id.Equals(committedCardId));
                if (!IsFacedownCommitted(committedCard))
                {
                    return TurnCardCommitmentRejectionReason.CommittedCardStateChanged;
                }
            }

            return TurnCardCommitmentRejectionReason.None;
        }

        private void CompleteRevealIfReady(ICollection<TurnCardCommitmentEvent> events)
        {
            if (expectedHeroIds.Any(heroId => !HasResponded(heroId)))
            {
                return;
            }

            foreach (var heroId in expectedHeroIds)
            {
                if (!committedCards.TryGetValue(heroId, out var cardId))
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
                    heroId,
                    cardId));
            }

            IsRevealed = true;
            events.Add(new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.RevealCompleted));
        }

        private bool HasResponded(HeroId heroId)
        {
            return committedCards.ContainsKey(heroId) || passedHeroes.Contains(heroId);
        }

        private bool IsExpectedHero(HeroId heroId)
        {
            return heroId.IsDefined && expectedHeroIds.Contains(heroId);
        }

        private OwnedHandState GetOwnedHandState(HeroId heroId)
        {
            var inHand = instances
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand)
                .ToArray();
            if (inHand.Any(card => !IsEligible(card)))
            {
                return OwnedHandState.Invalid;
            }

            return inHand.Length > 0
                ? OwnedHandState.HasEligibleCard
                : OwnedHandState.Empty;
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

        private enum OwnedHandState
        {
            Empty = 0,
            HasEligibleCard = 1,
            Invalid = 2
        }
    }
}
