using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionViewModel
    {
        public BoardInteractionViewModel(
            long version,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HexCoordinate> movementDestinations,
            IEnumerable<HeroId> attackTargets,
            HeroId focusedHeroId)
        {
            Version = version;
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
            MovementDestinations = Array.AsReadOnly((movementDestinations ??
                throw new ArgumentNullException(nameof(movementDestinations))).ToArray());
            AttackTargets = Array.AsReadOnly((attackTargets ??
                throw new ArgumentNullException(nameof(attackTargets))).ToArray());
            FocusedHeroId = focusedHeroId;
        }

        public long Version { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<HexCoordinate> MovementDestinations { get; }
        public IReadOnlyList<HeroId> AttackTargets { get; }
        public HeroId FocusedHeroId { get; }
    }

    public interface IBoardInteractionView
    {
        void Render(BoardInteractionViewModel model);
    }

    public sealed class BoardInteractionPresenter
    {
        private readonly IMatchSession session;
        private readonly IBoardInteractionView view;

        public BoardInteractionPresenter(IMatchSession session, IBoardInteractionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            view.Render(new BoardInteractionViewModel(
                projection.Version,
                projection.Heroes,
                projection.PendingMovementChoice?.LegalDestinations.Select(value => value.Destination) ??
                    Enumerable.Empty<HexCoordinate>(),
                projection.PendingAttackTargetChoice?.LegalTargetHeroIds ?? Array.Empty<HeroId>(),
                projection.SelectedActor.IsDefined ? projection.SelectedActor.ActorId : default));
        }

        public bool SubmitDestination(HexCoordinate destination)
        {
            var choice = session.Projection.PendingMovementChoice;
            if (choice == null || choice.LegalDestinations.Count(value => value.Destination.Equals(destination)) != 1)
            {
                return false;
            }

            return Execute(DirectorCommand.ChooseMovementDestination(
                choice.ResolutionId,
                choice.ChoiceId,
                destination));
        }

        public bool SubmitHero(HeroId heroId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetHeroIds.Contains(heroId))
            {
                return false;
            }

            return Execute(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId,
                choice.ChoiceId,
                heroId));
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Execute(command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

    public sealed class DirectorHandViewModel
    {
        public DirectorHandViewModel(
            TeamSide teamSide,
            HeroId heroId,
            string heroDisplayName,
            TurnCardCommitmentStatus commitmentStatus,
            bool canPass,
            CardInstanceId? revealedCardInstanceId,
            CardInstance revealedCard,
            IEnumerable<CardInstance> cards,
            IEnumerable<CardDefinition> definitions,
            CardInstanceId? selectedCardInstanceId)
        {
            TeamSide = teamSide;
            HeroId = heroId;
            HeroDisplayName = heroDisplayName ?? throw new ArgumentNullException(nameof(heroDisplayName));
            CommitmentStatus = commitmentStatus;
            CanPass = canPass;
            RevealedCardInstanceId = revealedCardInstanceId;
            RevealedCard = revealedCard;
            Cards = Array.AsReadOnly((cards ?? throw new ArgumentNullException(nameof(cards))).ToArray());
            Definitions = Array.AsReadOnly((definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            SelectedCardInstanceId = selectedCardInstanceId;
        }

        public TeamSide TeamSide { get; }
        public HeroId HeroId { get; }
        public string HeroDisplayName { get; }
        public TurnCardCommitmentStatus CommitmentStatus { get; }
        public bool CanPass { get; }
        public CardInstanceId? RevealedCardInstanceId { get; }
        public CardInstance RevealedCard { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public CardInstanceId? SelectedCardInstanceId { get; }
    }

    public sealed class DirectorHandsViewModel
    {
        public DirectorHandsViewModel(long version, IEnumerable<DirectorHandViewModel> hands)
        {
            Version = version;
            Hands = Array.AsReadOnly((hands ?? throw new ArgumentNullException(nameof(hands))).ToArray());
        }

        public long Version { get; }
        public IReadOnlyList<DirectorHandViewModel> Hands { get; }
    }

    public interface IDirectorHandsView
    {
        void Render(DirectorHandsViewModel model);
    }

    public sealed class DirectorHandsPresenter
    {
        private readonly IMatchSession session;
        private readonly IDirectorHandsView view;
        private CardInstanceId? selectedCardInstanceId;

        public DirectorHandsPresenter(IMatchSession session, IDirectorHandsView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (selectedCardInstanceId.HasValue && !projection.Cards.Any(card =>
                card.Id.Equals(selectedCardInstanceId.Value) && card.State.Location == CardLocation.Hand))
            {
                selectedCardInstanceId = null;
            }

            var hands = projection.Setup.Heroes
                .OrderBy(hero => hero.TeamSide)
                .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                .Select(hero =>
                {
                    var commitment = projection.GetCommitment(hero.HeroId);
                    return new DirectorHandViewModel(
                        hero.TeamSide,
                        hero.HeroId,
                        hero.DisplayName,
                        commitment.Status,
                        commitment.CanPass,
                        commitment.RevealedCardInstanceId,
                        commitment.RevealedCardInstanceId.HasValue
                            ? projection.Cards.Single(card =>
                                card.Id.Equals(commitment.RevealedCardInstanceId.Value))
                            : null,
                        projection.Cards.Where(card =>
                            card.OwnerId.Equals(hero.HeroId) && card.State.Location == CardLocation.Hand),
                        projection.Definitions,
                        selectedCardInstanceId);
                });
            view.Render(new DirectorHandsViewModel(projection.Version, hands));
        }

        public bool SelectCommitmentCard(CardInstanceId cardInstanceId)
        {
            var projection = session.Projection;
            var card = projection.Cards.SingleOrDefault(value => value.Id.Equals(cardInstanceId));
            if (projection.Phase != RoundResolutionPhase.AwaitingCommitments || card == null ||
                card.State.Location != CardLocation.Hand ||
                projection.GetCommitment(card.OwnerId).Status != TurnCardCommitmentStatus.AwaitingResponse)
            {
                return false;
            }

            selectedCardInstanceId = cardInstanceId;
            Refresh();
            return true;
        }

        public bool ConfirmSelectedCard()
        {
            if (!selectedCardInstanceId.HasValue)
            {
                return false;
            }

            var card = session.Projection.Cards.SingleOrDefault(value => value.Id.Equals(selectedCardInstanceId.Value));
            if (card == null)
            {
                return false;
            }

            var result = session.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public bool Pass(HeroId heroId)
        {
            var commitment = session.Projection.Commitments.SingleOrDefault(value => value.HeroId.Equals(heroId));
            if (!commitment.HeroId.IsDefined || !commitment.CanPass)
            {
                return false;
            }

            var result = session.Execute(DirectorCommand.Pass(heroId));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

    public sealed class TypedInvocationViewModel
    {
        public TypedInvocationViewModel(string label, DirectorCommand command)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A typed invocation requires a label.", nameof(label))
                : label;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public DirectorCommand Command { get; }
    }

    public sealed class RoundActionViewModel
    {
        public RoundActionViewModel(
            long version,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            string status,
            IEnumerable<TypedInvocationViewModel> actions,
            TypedInvocationViewModel endTurnInvocation,
            TypedInvocationViewModel continueEndOfRoundInvocation,
            IEnumerable<TypedInvocationViewModel> initiativeChoices,
            IEnumerable<TypedInvocationViewModel> defenseChoices)
        {
            Version = version;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Actions = Array.AsReadOnly((actions ?? throw new ArgumentNullException(nameof(actions))).ToArray());
            EndTurnInvocation = endTurnInvocation;
            ContinueEndOfRoundInvocation = continueEndOfRoundInvocation;
            InitiativeChoices = Array.AsReadOnly((initiativeChoices ??
                throw new ArgumentNullException(nameof(initiativeChoices))).ToArray());
            DefenseChoices = Array.AsReadOnly((defenseChoices ??
                throw new ArgumentNullException(nameof(defenseChoices))).ToArray());
        }

        public long Version { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public string Status { get; }
        public IReadOnlyList<TypedInvocationViewModel> Actions { get; }
        public TypedInvocationViewModel EndTurnInvocation { get; }
        public TypedInvocationViewModel ContinueEndOfRoundInvocation { get; }
        public IReadOnlyList<TypedInvocationViewModel> InitiativeChoices { get; }
        public IReadOnlyList<TypedInvocationViewModel> DefenseChoices { get; }
    }

    public interface IRoundActionView
    {
        void Render(RoundActionViewModel model);
    }

    public sealed class RoundActionPresenter
    {
        private readonly IMatchSession session;
        private readonly IRoundActionView view;

        public RoundActionPresenter(IMatchSession session, IRoundActionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            view.Render(CreateViewModel(session.Projection));
        }

        public bool Submit(TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return false;
            }

            var current = CreateViewModel(session.Projection);
            var legal = current.Actions
                .Concat(current.InitiativeChoices)
                .Concat(current.DefenseChoices)
                .Concat(current.EndTurnInvocation != null
                    ? new[] { current.EndTurnInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.ContinueEndOfRoundInvocation != null
                    ? new[] { current.ContinueEndOfRoundInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Any(value => value.Command.Equals(invocation.Command));
            if (!legal)
            {
                return false;
            }

            var result = session.Execute(invocation.Command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public static RoundActionViewModel CreateViewModel(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var hasPendingResolution = projection.PendingInitiativeChoice != null ||
                projection.PendingMovementChoice != null || projection.PendingMovementResolution != null ||
                projection.PendingAttackTargetChoice != null || projection.PendingDefenseChoice != null ||
                projection.PendingAttackResolution != null;
            var actions = (projection.IsTurnActive && !hasPendingResolution
                    ? projection.AvailableTurnActions
                    : Array.Empty<ActionOffer>())
                .Where(offer => offer.IsAvailable)
                .Select(offer => new TypedInvocationViewModel(
                    FormatActionLabel(projection, offer),
                    DirectorCommand.StartAction(offer.ActionDefinitionId)))
                .ToArray();
            var initiativeChoices = projection.PendingInitiativeChoice == null
                ? Array.Empty<TypedInvocationViewModel>()
                : projection.PendingInitiativeChoice.CandidateCardInstanceIds
                    .Select(cardId => new TypedInvocationViewModel(
                        $"ACT WITH {GetCardDisplayName(projection, cardId)}",
                        DirectorCommand.ChooseTiedActor(
                            projection.PendingInitiativeChoice.OwningSide,
                            cardId)))
                    .ToArray();
            var defenseChoices = CreateDefenseChoices(projection);

            return new RoundActionViewModel(
                projection.Version,
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase,
                FormatStatus(projection),
                actions,
                CreateEndTurnInvocation(projection),
                projection.Phase == RoundResolutionPhase.EndOfRound
                    ? new TypedInvocationViewModel("CONTINUE END OF ROUND", DirectorCommand.AdvanceEndOfRound())
                    : null,
                initiativeChoices,
                defenseChoices);
        }

        private static TypedInvocationViewModel CreateEndTurnInvocation(MatchSessionProjection projection)
        {
            var movementChoice = projection.PendingMovementChoice;
            if (movementChoice != null && movementChoice.CanStop)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.StopMovement(movementChoice.ResolutionId, movementChoice.ChoiceId));
            }

            var hasMandatoryChoice = projection.PendingInitiativeChoice != null ||
                projection.PendingAttackTargetChoice != null || projection.PendingDefenseChoice != null ||
                projection.PendingAttackResolution != null || projection.PendingMovementChoice != null ||
                projection.PendingMovementResolution != null;
            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined || hasMandatoryChoice)
            {
                return null;
            }

            var hold = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                projection.SelectedActor.CardInstanceId,
                projection.SelectedActor.ActorId,
                projection.Definitions,
                projection.Cards);
            return hold.HasValue && hold.Value.IsAvailable
                ? new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId))
                : null;
        }

        private static IReadOnlyList<TypedInvocationViewModel> CreateDefenseChoices(
            MatchSessionProjection projection)
        {
            var choice = projection.PendingDefenseChoice;
            if (choice == null)
            {
                return Array.Empty<TypedInvocationViewModel>();
            }

            var values = choice.LegalDefenseOffers
                .Select(offer => new TypedInvocationViewModel(
                    $"DISCARD {GetCardDisplayName(projection, offer.SourceCardInstanceId)}",
                    DirectorCommand.ChooseDefenseCard(
                        choice.ResolutionId,
                        choice.ChoiceId,
                        offer.SourceCardInstanceId)))
                .ToList();
            if (choice.CanDecline)
            {
                values.Add(new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    DirectorCommand.DeclineDefense(choice.ResolutionId, choice.ChoiceId)));
            }

            return values;
        }

        private static string FormatActionLabel(MatchSessionProjection projection, ActionOffer offer)
        {
            var definition = projection.Definitions.Single(value =>
                value.Reference.Equals(offer.SourceDefinitionReference));
            var action = definition.ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId));
            var channel = action.Kind == ActionKind.Attack
                ? PrintedValueChannel.Attack
                : action.Kind == ActionKind.Movement
                    ? PrintedValueChannel.Movement
                    : PrintedValueChannel.None;
            var value = channel == PrintedValueChannel.None
                ? string.Empty
                : $" {PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, channel).Total}";
            var range = action.Kind == ActionKind.Attack && offer.AttackRange == AttackRangeKind.Ranged
                ? $" RANGE {PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, PrintedValueChannel.Range).Total}"
                : string.Empty;
            var policy = action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                ? "text applies"
                : "text suppressed";
            return $"{FormatInvocationContext(offer.InvocationContext)} " +
                $"{action.Kind.ToString().ToUpperInvariant()}{value}{range}\n{policy}";
        }

        private static string FormatInvocationContext(ActionInvocationContext context)
        {
            return context == ActionInvocationContext.PrintedPrimary ? "PRIMARY" : "SECONDARY";
        }

        private static string GetCardDisplayName(MatchSessionProjection projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardId));
            return projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
        }

        private static string FormatStatus(MatchSessionProjection projection)
        {
            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                return $"ROUND {projection.RoundIndex} COMPLETE - END OF ROUND";
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                return $"Round {projection.RoundIndex}, turn {projection.TurnIndex}: awaiting commitments.";
            }

            if (projection.PendingInitiativeChoice != null)
            {
                return "Choose the tied Initiative actor.";
            }

            if (projection.PendingDefenseChoice != null)
            {
                return $"Defense response pending against Attack {projection.PendingDefenseChoice.AttackTotal}.";
            }

            if (projection.PendingAttackTargetChoice != null)
            {
                return "Choose a legal Attack target.";
            }

            if (projection.PendingMovementChoice != null)
            {
                return $"Movement has {projection.PendingMovementChoice.RemainingAllowance} remaining.";
            }

            return projection.SelectedActor.IsDefined
                ? $"Initiative {projection.SelectedActor.InitiativeTotal}: choose an action or End Turn."
                : "Awaiting the next authoritative choice.";
        }
    }

    public sealed class UndoRestartViewModel
    {
        public UndoRestartViewModel(long version, bool canUndo, bool canRestart, int acceptedCommandCount)
        {
            Version = version;
            CanUndo = canUndo;
            CanRestart = canRestart;
            AcceptedCommandCount = acceptedCommandCount;
        }

        public long Version { get; }
        public bool CanUndo { get; }
        public bool CanRestart { get; }
        public int AcceptedCommandCount { get; }
    }

    public interface IUndoRestartView
    {
        void Render(UndoRestartViewModel model);
    }

    public sealed class UndoRestartPresenter
    {
        private readonly IMatchSession session;
        private readonly IUndoRestartView view;

        public UndoRestartPresenter(IMatchSession session, IUndoRestartView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            view.Render(new UndoRestartViewModel(
                projection.Version,
                projection.AcceptedCommands.Count > 0,
                projection.AcceptedCommands.Count > 0,
                projection.AcceptedCommands.Count));
        }

        public bool Undo()
        {
            var result = session.Undo();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public bool Restart()
        {
            var result = session.Restart();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

    public sealed class DiagnosticLogViewModel
    {
        public DiagnosticLogViewModel(long version, bool isExpanded, IEnumerable<RuleEventLogEntry> entries)
        {
            Version = version;
            IsExpanded = isExpanded;
            Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
        }

        public long Version { get; }
        public bool IsExpanded { get; }
        public IReadOnlyList<RuleEventLogEntry> Entries { get; }
    }

    public interface IDiagnosticLogView
    {
        void Render(DiagnosticLogViewModel model);
    }

    public sealed class DiagnosticLogPresenter
    {
        private readonly IMatchSession session;
        private readonly IDiagnosticLogView view;
        private bool isExpanded;

        public DiagnosticLogPresenter(IMatchSession session, IDiagnosticLogView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Refresh()
        {
            var projection = session.Projection;
            view.Render(new DiagnosticLogViewModel(
                projection.Version,
                isExpanded,
                projection.DiagnosticEvents));
        }

        public void Toggle()
        {
            isExpanded = !isExpanded;
            Refresh();
        }
    }

    public sealed class MatchFeatureCoordinator
    {
        private readonly BoardInteractionPresenter board;
        private readonly DirectorHandsPresenter hands;
        private readonly RoundActionPresenter round;
        private readonly UndoRestartPresenter undoRestart;
        private readonly DiagnosticLogPresenter diagnostics;

        public MatchFeatureCoordinator(
            BoardInteractionPresenter board,
            DirectorHandsPresenter hands,
            RoundActionPresenter round,
            UndoRestartPresenter undoRestart,
            DiagnosticLogPresenter diagnostics,
            Action<MatchSessionProjection> afterSessionChange = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hands = hands ?? throw new ArgumentNullException(nameof(hands));
            this.round = round ?? throw new ArgumentNullException(nameof(round));
            this.undoRestart = undoRestart ?? throw new ArgumentNullException(nameof(undoRestart));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

            void Changed(MatchSessionProjection projection)
            {
                afterSessionChange?.Invoke(projection);
                RefreshAll();
            }

            board.SessionProjectionChanged += Changed;
            hands.SessionProjectionChanged += Changed;
            round.SessionProjectionChanged += Changed;
            undoRestart.SessionProjectionChanged += Changed;
        }

        public void RefreshAll()
        {
            board.Refresh();
            hands.Refresh();
            round.Refresh();
            undoRestart.Refresh();
            diagnostics.Refresh();
        }
    }
}
