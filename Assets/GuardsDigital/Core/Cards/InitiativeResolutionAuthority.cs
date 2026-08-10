using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum InitiativeResolutionRejectionReason
    {
        None = 0,
        ActorAlreadySelected = 1,
        ChoicePending = 2,
        NoChoicePending = 3,
        ChoiceOwnedByOtherTeam = 4,
        CardNotInPendingChoice = 5,
        InitiativeStateChanged = 6,
        SelectedCardMismatch = 7,
        SelectedCardNotResolved = 8,
        SelectedCardStateChanged = 9
    }

    public enum InitiativeResolutionEventKind
    {
        None = 0,
        SameTeamChoiceRequested = 1,
        NextActorSelected = 2,
        TieBreakerFlipDeferred = 3,
        SelectedActorFinished = 4,
        TieBreakerFlipped = 5,
        NoUnresolvedCardsRemaining = 6
    }

    public readonly struct InitiativeResolutionEvent
    {
        public InitiativeResolutionEvent(
            InitiativeResolutionEventKind kind,
            TeamSide teamSide = TeamSide.None,
            CardInstanceId cardInstanceId = default,
            HeroId actorId = default,
            int initiativeTotal = 0)
        {
            if (kind == InitiativeResolutionEventKind.None)
            {
                throw new ArgumentException("An Initiative event requires a defined kind.", nameof(kind));
            }

            Kind = kind;
            TeamSide = teamSide;
            CardInstanceId = cardInstanceId;
            ActorId = actorId;
            InitiativeTotal = initiativeTotal;
        }

        public InitiativeResolutionEventKind Kind { get; }

        public TeamSide TeamSide { get; }

        public CardInstanceId CardInstanceId { get; }

        public HeroId ActorId { get; }

        public int InitiativeTotal { get; }
    }

    public sealed class PendingInitiativeChoice
    {
        public PendingInitiativeChoice(
            TeamSide owningSide,
            int initiativeTotal,
            IEnumerable<CardInstanceId> candidateCardInstanceIds,
            bool flipTieBreakerAfterResolution)
        {
            if (owningSide != TeamSide.Red && owningSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(owningSide));
            }

            var candidates = (candidateCardInstanceIds ??
                    throw new ArgumentNullException(nameof(candidateCardInstanceIds)))
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray();
            if (candidates.Length < 2 || candidates.Any(id => !id.IsDefined) ||
                candidates.Distinct().Count() != candidates.Length)
            {
                throw new ArgumentException(
                    "An Initiative choice requires at least two unique defined card candidates.",
                    nameof(candidateCardInstanceIds));
            }

            OwningSide = owningSide;
            InitiativeTotal = initiativeTotal;
            CandidateCardInstanceIds = Array.AsReadOnly(candidates);
            FlipTieBreakerAfterResolution = flipTieBreakerAfterResolution;
        }

        public TeamSide OwningSide { get; }

        public int InitiativeTotal { get; }

        public IReadOnlyList<CardInstanceId> CandidateCardInstanceIds { get; }

        public bool FlipTieBreakerAfterResolution { get; }
    }

    public readonly struct SelectedInitiativeActor
    {
        public SelectedInitiativeActor(
            CardInstanceId cardInstanceId,
            HeroId actorId,
            TeamSide teamSide,
            int initiativeTotal,
            bool flipTieBreakerAfterResolution)
        {
            CardInstanceId = cardInstanceId;
            ActorId = actorId;
            TeamSide = teamSide;
            InitiativeTotal = initiativeTotal;
            FlipTieBreakerAfterResolution = flipTieBreakerAfterResolution;
        }

        public CardInstanceId CardInstanceId { get; }

        public HeroId ActorId { get; }

        public TeamSide TeamSide { get; }

        public int InitiativeTotal { get; }

        public bool FlipTieBreakerAfterResolution { get; }

        public bool IsDefined => CardInstanceId.IsDefined && ActorId.IsDefined;
    }

    public readonly struct InitiativeResolutionResult
    {
        private InitiativeResolutionResult(
            bool accepted,
            InitiativeResolutionRejectionReason rejectionReason,
            IEnumerable<InitiativeResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray());
        }

        public bool Accepted { get; }

        public InitiativeResolutionRejectionReason RejectionReason { get; }

        public IReadOnlyList<InitiativeResolutionEvent> Events { get; }

        public static InitiativeResolutionResult Accept(IEnumerable<InitiativeResolutionEvent> events)
        {
            return new InitiativeResolutionResult(
                true,
                InitiativeResolutionRejectionReason.None,
                events);
        }

        public static InitiativeResolutionResult Reject(InitiativeResolutionRejectionReason reason)
        {
            if (reason == InitiativeResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected Initiative command requires a reason.", nameof(reason));
            }

            return new InitiativeResolutionResult(false, reason, Array.Empty<InitiativeResolutionEvent>());
        }
    }

    public sealed class InitiativeResolutionAuthority
    {
        public InitiativeResolutionAuthority(TieBreakerState initialTieBreakerState)
        {
            if (!initialTieBreakerState.IsDefined)
            {
                throw new ArgumentException("Initiative authority requires Tie Breaker state.", nameof(initialTieBreakerState));
            }

            TieBreakerState = initialTieBreakerState;
        }

        public TieBreakerState TieBreakerState { get; private set; }

        public PendingInitiativeChoice PendingChoice { get; private set; }

        public SelectedInitiativeActor SelectedActor { get; private set; }

        public InitiativeResolutionResult TrySelectNextActor(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (SelectedActor.IsDefined)
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.ActorAlreadySelected);
            }

            if (PendingChoice != null)
            {
                return InitiativeResolutionResult.Reject(InitiativeResolutionRejectionReason.ChoicePending);
            }

            var selection = QuerySelection(definitions, instances, participants);
            if (selection.Candidates.Count == 0)
            {
                return InitiativeResolutionResult.Accept(new[]
                {
                    new InitiativeResolutionEvent(
                        InitiativeResolutionEventKind.NoUnresolvedCardsRemaining)
                });
            }

            if (selection.Candidates.Count > 1)
            {
                PendingChoice = new PendingInitiativeChoice(
                    selection.SelectedSide,
                    selection.InitiativeTotal,
                    selection.Candidates.Select(candidate => candidate.Card.Id),
                    selection.CrossTeamTie);
                return InitiativeResolutionResult.Accept(new[]
                {
                    new InitiativeResolutionEvent(
                        InitiativeResolutionEventKind.SameTeamChoiceRequested,
                        selection.SelectedSide,
                        initiativeTotal: selection.InitiativeTotal)
                });
            }

            return Select(selection.Candidates[0], selection.InitiativeTotal, selection.CrossTeamTie);
        }

        public InitiativeResolutionResult TryChooseTiedActor(
            TeamSide choosingSide,
            CardInstanceId cardInstanceId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (SelectedActor.IsDefined)
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.ActorAlreadySelected);
            }

            if (PendingChoice == null)
            {
                return InitiativeResolutionResult.Reject(InitiativeResolutionRejectionReason.NoChoicePending);
            }

            if (choosingSide != PendingChoice.OwningSide)
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.ChoiceOwnedByOtherTeam);
            }

            if (!PendingChoice.CandidateCardInstanceIds.Contains(cardInstanceId))
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.CardNotInPendingChoice);
            }

            var selection = QuerySelection(definitions, instances, participants);
            var queriedIds = selection.Candidates.Select(candidate => candidate.Card.Id).ToArray();
            if (selection.SelectedSide != PendingChoice.OwningSide ||
                selection.InitiativeTotal != PendingChoice.InitiativeTotal ||
                selection.CrossTeamTie != PendingChoice.FlipTieBreakerAfterResolution ||
                !queriedIds.OrderBy(id => id.Value, StringComparer.Ordinal)
                    .SequenceEqual(PendingChoice.CandidateCardInstanceIds))
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.InitiativeStateChanged);
            }

            var chosen = selection.Candidates.Single(candidate => candidate.Card.Id.Equals(cardInstanceId));
            PendingChoice = null;
            return Select(chosen, selection.InitiativeTotal, selection.CrossTeamTie);
        }

        public InitiativeResolutionResult TryAcknowledgeSelectedCardResolved(
            CardInstanceId cardInstanceId,
            IEnumerable<CardInstance> instances)
        {
            if (!SelectedActor.IsDefined || !SelectedActor.CardInstanceId.Equals(cardInstanceId))
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.SelectedCardMismatch);
            }

            var cardArray = GetCardInstances(instances);
            var card = cardArray.SingleOrDefault(candidate => candidate.Id.Equals(cardInstanceId));
            if (card == null)
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.SelectedCardStateChanged);
            }

            if (card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Resolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return InitiativeResolutionResult.Reject(
                    InitiativeResolutionRejectionReason.SelectedCardNotResolved);
            }

            var completed = SelectedActor;
            var events = new List<InitiativeResolutionEvent>
            {
                new InitiativeResolutionEvent(
                    InitiativeResolutionEventKind.SelectedActorFinished,
                    completed.TeamSide,
                    completed.CardInstanceId,
                    completed.ActorId,
                    completed.InitiativeTotal)
            };
            SelectedActor = default;

            if (completed.FlipTieBreakerAfterResolution)
            {
                var nextSide = TieBreakerState.RevealedSide == TeamSide.Red
                    ? TeamSide.Blue
                    : TeamSide.Red;
                TieBreakerState = new TieBreakerState(nextSide);
                events.Add(new InitiativeResolutionEvent(
                    InitiativeResolutionEventKind.TieBreakerFlipped,
                    nextSide));
            }

            return InitiativeResolutionResult.Accept(events);
        }

        private InitiativeResolutionResult Select(
            InitiativeCandidate candidate,
            int initiativeTotal,
            bool flipTieBreakerAfterResolution)
        {
            SelectedActor = new SelectedInitiativeActor(
                candidate.Card.Id,
                candidate.Card.OwnerId,
                candidate.Side,
                initiativeTotal,
                flipTieBreakerAfterResolution);
            var events = new List<InitiativeResolutionEvent>
            {
                new InitiativeResolutionEvent(
                    InitiativeResolutionEventKind.NextActorSelected,
                    candidate.Side,
                    candidate.Card.Id,
                    candidate.Card.OwnerId,
                    initiativeTotal)
            };
            if (flipTieBreakerAfterResolution)
            {
                events.Add(new InitiativeResolutionEvent(
                    InitiativeResolutionEventKind.TieBreakerFlipDeferred,
                    candidate.Side,
                    candidate.Card.Id,
                    candidate.Card.OwnerId,
                    initiativeTotal));
            }

            return InitiativeResolutionResult.Accept(events);
        }

        private SelectionQueryResult QuerySelection(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var cardArray = GetCardInstances(instances);
            var definitionArray = ActionOfferQuery.BuildDefinitionLookup(definitions).Values.ToArray();
            var candidates = new List<InitiativeCandidate>();
            foreach (var card in cardArray.Where(IsFaceupUnresolved))
            {
                var side = participants.GetTeamForHero(card.OwnerId).Side;
                var value = PrintedValueQuery.CalculatePrintedInitiative(card, definitionArray);
                candidates.Add(new InitiativeCandidate(card, side, value.Total));
            }

            if (candidates.Count == 0)
            {
                return SelectionQueryResult.Empty;
            }

            var highest = candidates.Max(candidate => candidate.InitiativeTotal);
            var highestCandidates = candidates
                .Where(candidate => candidate.InitiativeTotal == highest)
                .ToArray();
            var tiedSides = highestCandidates.Select(candidate => candidate.Side).Distinct().ToArray();
            var crossTeamTie = tiedSides.Length > 1;
            var selectedSide = crossTeamTie ? TieBreakerState.RevealedSide : tiedSides[0];
            return new SelectionQueryResult(
                highestCandidates
                    .Where(candidate => candidate.Side == selectedSide)
                    .OrderBy(candidate => candidate.Card.Id.Value, StringComparer.Ordinal)
                    .ToArray(),
                selectedSide,
                highest,
                crossTeamTie);
        }

        private static CardInstance[] GetCardInstances(IEnumerable<CardInstance> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) || cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Initiative card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            return cards;
        }

        private static bool IsFaceupUnresolved(CardInstance card)
        {
            return card.State.Location == CardLocation.TurnArea &&
                card.State.HasBeenPlayed &&
                card.State.ResolutionStatus == CardResolutionStatus.Unresolved &&
                card.State.FaceOrientation == CardFaceOrientation.FaceUp;
        }

        private sealed class InitiativeCandidate
        {
            public InitiativeCandidate(CardInstance card, TeamSide side, int initiativeTotal)
            {
                Card = card;
                Side = side;
                InitiativeTotal = initiativeTotal;
            }

            public CardInstance Card { get; }

            public TeamSide Side { get; }

            public int InitiativeTotal { get; }
        }

        private sealed class SelectionQueryResult
        {
            public static readonly SelectionQueryResult Empty = new SelectionQueryResult(
                Array.Empty<InitiativeCandidate>(),
                TeamSide.None,
                0,
                false);

            public SelectionQueryResult(
                IReadOnlyList<InitiativeCandidate> candidates,
                TeamSide selectedSide,
                int initiativeTotal,
                bool crossTeamTie)
            {
                Candidates = candidates;
                SelectedSide = selectedSide;
                InitiativeTotal = initiativeTotal;
                CrossTeamTie = crossTeamTie;
            }

            public IReadOnlyList<InitiativeCandidate> Candidates { get; }

            public TeamSide SelectedSide { get; }

            public int InitiativeTotal { get; }

            public bool CrossTeamTie { get; }
        }
    }
}
