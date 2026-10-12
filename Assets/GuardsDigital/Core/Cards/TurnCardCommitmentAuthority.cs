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
        InvalidInHandCardState = 9,
        DuplicateCard = 10,
        NoTwoCardCommitment = 11,
        CardNotInTwoCardCommitment = 12,
        NotCurrentRetrievalHero = 13,
        NoFacedownCommitment = 14
    }

    public enum TurnCardCommitmentEventKind
    {
        None = 0,
        CardCommittedFacedown = 1,
        HeroPassed = 2,
        CardRevealed = 3,
        RevealCompleted = 4,
        CardRetrievedAfterReveal = 5,
        CommitmentRetracted = 6
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

            if ((kind == TurnCardCommitmentEventKind.CardRevealed ||
                kind == TurnCardCommitmentEventKind.CardRetrievedAfterReveal) && !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A revealed-card event requires a card instance ID.", nameof(cardInstanceId));
            }

            if (kind != TurnCardCommitmentEventKind.CardRevealed &&
                kind != TurnCardCommitmentEventKind.CardRetrievedAfterReveal && cardInstanceId.IsDefined)
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
        private readonly Dictionary<HeroId, CardInstanceId> secondCommittedCards =
            new Dictionary<HeroId, CardInstanceId>();
        private readonly HashSet<HeroId> passedHeroes = new HashSet<HeroId>();

        public TurnCardCommitmentAuthority(
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<HeroId> participatingHeroes = null)
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
            expectedHeroIds = Array.AsReadOnly((participatingHeroes ?? participants.Heroes.Select(hero => hero.Id))
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
            if (expectedHeroIds.Count == 0 || expectedHeroIds.Distinct().Count() != expectedHeroIds.Count)
                throw new ArgumentException("Turn participants must be nonempty and unique.", nameof(participatingHeroes));
            foreach (var hero in expectedHeroIds) participants.GetHero(hero);
        }

        public bool IsRevealed { get; private set; }

        public IReadOnlyList<HeroId> ExpectedHeroIds => expectedHeroIds;

        internal IReadOnlyDictionary<HeroId, CardInstanceId> CommittedCards => committedCards;

        internal IReadOnlyDictionary<HeroId, CardInstanceId> SecondCommittedCards => secondCommittedCards;

        public IReadOnlyList<CardInstanceId> GetRevealedCards(HeroId heroId)
        {
            if (!IsExpectedHero(heroId)) throw new KeyNotFoundException($"Unknown turn-card hero ID '{heroId}'.");
            if (!IsRevealed || !committedCards.TryGetValue(heroId, out var first))
                return Array.Empty<CardInstanceId>();
            return secondCommittedCards.TryGetValue(heroId, out var second)
                ? Array.AsReadOnly(new[] { first, second })
                : Array.AsReadOnly(new[] { first });
        }

        public HeroId? PendingRetrievalHero => IsRevealed && secondCommittedCards.Count > 0
            ? secondCommittedCards.Keys.OrderBy(hero => hero.Value, StringComparer.Ordinal).First()
            : (HeroId?)null;

        internal IReadOnlyCollection<HeroId> PassedHeroes => passedHeroes;

        internal TurnCardCommitmentRestoreState CaptureRestoreState() =>
            new TurnCardCommitmentRestoreState(
                committedCards.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray(),
                passedHeroes.OrderBy(heroId => heroId.Value, StringComparer.Ordinal).ToArray(),
                IsRevealed,
                secondCommittedCards.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray());

        internal static TurnCardCommitmentAuthority Restore(
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TurnCardCommitmentRestoreState state,
            IEnumerable<HeroId> participatingHeroes = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var authority = new TurnCardCommitmentAuthority(instances, participants, participatingHeroes);
            foreach (var pair in state.Commitments)
            {
                if (!authority.IsExpectedHero(pair.Key) || !pair.Value.IsDefined ||
                    authority.committedCards.ContainsKey(pair.Key))
                {
                    throw new ArgumentException("Restored commitments contain invalid or duplicate identities.", nameof(state));
                }

                authority.committedCards.Add(pair.Key, pair.Value);
            }

            foreach (var heroId in state.Passes)
            {
                if (!authority.IsExpectedHero(heroId) || authority.committedCards.ContainsKey(heroId) ||
                    !authority.passedHeroes.Add(heroId))
                {
                    throw new ArgumentException("Restored passes contain invalid or duplicate identities.", nameof(state));
                }
            }

            foreach (var pair in state.SecondCommitments)
            {
                if (!authority.committedCards.TryGetValue(pair.Key, out var first) ||
                    !pair.Value.IsDefined || pair.Value.Equals(first) ||
                    !authority.secondCommittedCards.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException("Restored second commitments contain invalid or duplicate identities.", nameof(state));
            }

            foreach (var pair in authority.secondCommittedCards)
            {
                var first = authority.instances.SingleOrDefault(card =>
                    card.Id.Equals(authority.committedCards[pair.Key]));
                var second = authority.instances.SingleOrDefault(card => card.Id.Equals(pair.Value));
                if (first == null || second == null ||
                    !first.OwnerId.Equals(pair.Key) || !second.OwnerId.Equals(pair.Key) ||
                    (state.IsRevealed
                        ? !CardLifecycleAuthority.IsResolvingCardInTurnArea(first) ||
                          !CardLifecycleAuthority.IsResolvingCardInTurnArea(second)
                        : !IsFacedownCommitted(first) || !IsFacedownCommitted(second)))
                    throw new ArgumentException(
                        "Restored two-card commitments disagree with card ownership or reveal state.", nameof(state));
            }

            if (state.IsRevealed && authority.expectedHeroIds.Any(heroId => !authority.HasResponded(heroId)))
            {
                throw new ArgumentException("A revealed restored commitment requires every hero response.", nameof(state));
            }

            authority.IsRevealed = state.IsRevealed;
            return authority;
        }

        /// <summary>One atomic response: both eligible cards enter the turn area before the shared reveal.</summary>
        public TurnCardCommitmentResult TryCommitTwoCards(
            HeroId heroId, CardInstanceId firstId, CardInstanceId secondId)
        {
            var rejection = GetCommonRejection(heroId);
            if (rejection != TurnCardCommitmentRejectionReason.None)
                return TurnCardCommitmentResult.Reject(rejection);
            if (firstId.Equals(secondId))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.DuplicateCard);
            if (GetOwnedHandState(heroId) == OwnedHandState.Invalid)
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            var first = instances.SingleOrDefault(card => card.Id.Equals(firstId));
            var second = instances.SingleOrDefault(card => card.Id.Equals(secondId));
            if (first == null || second == null)
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotFound);
            if (!first.OwnerId.Equals(heroId) || !second.OwnerId.Equals(heroId))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotOwnedByHero);
            if (!IsEligible(first) || !IsEligible(second))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotEligible);
            if (!CardLifecycleAuthority.TryPlayFacedown(first) ||
                !CardLifecycleAuthority.TryPlayFacedown(second))
                throw new InvalidOperationException("Validated double commitment could not enter the turn area.");
            committedCards.Add(heroId, firstId);
            secondCommittedCards.Add(heroId, secondId);
            var events = new List<TurnCardCommitmentEvent>
            {
                new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.CardCommittedFacedown, heroId)
            };
            CompleteRevealIfReady(events);
            return TurnCardCommitmentResult.Accept(events);
        }

        public TurnCardCommitmentResult TryRetrieveOneAfterReveal(HeroId heroId, CardInstanceId cardId)
        {
            if (!IsRevealed || !secondCommittedCards.TryGetValue(heroId, out var second) ||
                !committedCards.TryGetValue(heroId, out var first))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.NoTwoCardCommitment);
            if (!PendingRetrievalHero.Value.Equals(heroId))
                return TurnCardCommitmentResult.Reject(
                    TurnCardCommitmentRejectionReason.NotCurrentRetrievalHero);
            if (!cardId.Equals(first) && !cardId.Equals(second))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CardNotInTwoCardCommitment);
            var card = instances.Single(value => value.Id.Equals(cardId));
            if (!CardLifecycleAuthority.TryRetrieveUnresolvedAtReveal(card))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CommittedCardStateChanged);
            committedCards[heroId] = cardId.Equals(first) ? second : first;
            secondCommittedCards.Remove(heroId);
            return TurnCardCommitmentResult.Accept(new[]
            {
                new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.CardRetrievedAfterReveal, heroId, cardId)
            });
        }

        /// <summary>User-approved digital withdrawal before the final shared response.</summary>
        public TurnCardCommitmentResult TryRetractCommitment(HeroId heroId)
        {
            if (!IsExpectedHero(heroId)) return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.HeroNotFound);
            if (IsRevealed || expectedHeroIds.All(HasResponded))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.TurnAlreadyRevealed);
            if (!committedCards.TryGetValue(heroId, out var first))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.NoFacedownCommitment);
            var ids = secondCommittedCards.TryGetValue(heroId, out var second)
                ? new[] { first, second } : new[] { first };
            var cards = ids.Select(id => instances.Single(c => c.Id.Equals(id))).ToArray();
            if (cards.Any(c => !c.OwnerId.Equals(heroId) || !IsFacedownCommitted(c)))
                return TurnCardCommitmentResult.Reject(TurnCardCommitmentRejectionReason.CommittedCardStateChanged);
            foreach (var card in cards)
                if (!CardLifecycleAuthority.TryRetractFacedown(card))
                    throw new InvalidOperationException("Validated commitment could not return to hand.");
            committedCards.Remove(heroId); secondCommittedCards.Remove(heroId);
            return TurnCardCommitmentResult.Accept(new[] {
                new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.CommitmentRetracted, heroId) });
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

        /// <summary>
        /// Milestone 9.32b. p.10's Swap a Card moved this turn's revealed card for a hero to another
        /// card, which now carries its played state: Takahide's Sting like a Bee swapping itself with a
        /// deck card (9.32a), or a teammate swapping their unresolved card (Set an Example). The revealed
        /// record names the card that is played now, so every reader of it - the HUD's played card, the
        /// snapshot - names the same card the turn slot does. Asked only by the round's swaps.
        /// </summary>
        internal bool TryReplaceRevealedCard(HeroId heroId, CardInstanceId revealed, CardInstanceId replacement)
        {
            if (!IsRevealed || !replacement.IsDefined ||
                !committedCards.TryGetValue(heroId, out var current) || !current.Equals(revealed))
            {
                return false;
            }

            committedCards[heroId] = replacement;
            return true;
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

            foreach (var committedCardId in secondCommittedCards.Values)
            {
                var committedCard = instances.Single(card => card.Id.Equals(committedCardId));
                if (!IsFacedownCommitted(committedCard))
                    return TurnCardCommitmentRejectionReason.CommittedCardStateChanged;
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
                if (secondCommittedCards.TryGetValue(heroId, out var secondId))
                {
                    var second = instances.Single(candidate => candidate.Id.Equals(secondId));
                    if (!CardLifecycleAuthority.TryReveal(second))
                        throw new InvalidOperationException("A validated second commitment could not be revealed.");
                    events.Add(new TurnCardCommitmentEvent(
                        TurnCardCommitmentEventKind.CardRevealed, heroId, secondId));
                }
            }

            IsRevealed = true;
            events.Add(new TurnCardCommitmentEvent(TurnCardCommitmentEventKind.RevealCompleted));
        }

        /// <summary>W-022. Whether this hero has committed or passed this turn.</summary>
        public bool HasHeroResponded(HeroId heroId) => HasResponded(heroId);

        /// <summary>W-022. Whether any hero has committed or passed this turn.</summary>
        public bool HasAnyResponse => committedCards.Count > 0 || passedHeroes.Count > 0;

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
