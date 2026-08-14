using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Units.Minions;

namespace GuardsDigital.Presentation.Match
{
    public readonly struct MinionDefenseCueViewModel
    {
        public MinionDefenseCueViewModel(MinionId minionId, int amount)
        {
            if (!minionId.IsDefined || (amount != -1 && amount != 1))
            {
                throw new ArgumentException("A minion Defense cue requires a stable identity and signed unit contribution.");
            }

            MinionId = minionId;
            Amount = amount;
        }

        public MinionId MinionId { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// What a board cue says about one minion in a pending minion choice. Both visible kinds point
    /// at the same thing - this minion - so the board draws one marker for both and distinguishes
    /// them by color: a selected minion keeps its marker while the pointer moves across the other
    /// controls, and a highlighted one answers "which minion is this control".
    /// </summary>
    public readonly struct MinionChoiceCueViewModel
    {
        public MinionChoiceCueViewModel(MinionId minionId, MinionChoiceCueKind kind)
        {
            if (!minionId.IsDefined || kind == MinionChoiceCueKind.None)
            {
                throw new ArgumentException(
                    "A minion choice cue requires a stable identity and a visible kind.");
            }

            MinionId = minionId;
            Kind = kind;
        }

        public MinionId MinionId { get; }
        public MinionChoiceCueKind Kind { get; }
    }

    /// <summary>
    /// The one place the minion-choice prompt names a candidate. The ordinal is the candidate's
    /// position in the projection's own ordinal candidate order, so two candidates of the same team
    /// and type stay distinguishable by text rather than by color.
    /// </summary>
    public static class MinionChoiceLabels
    {
        public static string TeamLabel(TeamSide side) =>
            side == TeamSide.Red ? "[R] RED" : "[B] BLUE";

        public static string CandidateLabel(TeamSide side, StandardMinionType minionType, int ordinal)
        {
            if (ordinal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return $"{TeamLabel(side)} {minionType.ToString().ToUpperInvariant()} #{ordinal}";
        }
    }

    /// <summary>
    /// The one place the effect-choice prompt names a candidate. Milestone 6.8a added it when the first
    /// configured card carried a Skill: a mode is identified by its ordinal in the projection's own
    /// candidate order and by the effect node's stable ID, both definition data. Printed card text is
    /// never read here, and no label may become the thing a rule branches on.
    /// </summary>
    public static class EffectChoiceLabels
    {
        public static string CandidateLabel(EffectNodeId optionNodeId, int ordinal)
        {
            if (ordinal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return optionNodeId.IsDefined
                ? $"MODE #{ordinal} {optionNodeId.Value.ToUpperInvariant()}"
                : $"OPTION #{ordinal}";
        }

        public static string DeselectLabel(EffectNodeId optionNodeId, int ordinal) =>
            $"UNDO {CandidateLabel(optionNodeId, ordinal)}";

        public const string ConfirmLabel = "CONFIRM CHOICE";
        public const string DeclineLabel = "DECLINE";
    }

    public sealed class BoardInteractionViewModel
    {
        public BoardInteractionViewModel(
            long version,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<HexCoordinate> movementDestinations,
            IEnumerable<HeroId> attackTargets,
            IEnumerable<MinionId> minionAttackTargets,
            HeroId focusedHeroId,
            IEnumerable<HexCoordinate> fastTravelDestinations = null,
            IEnumerable<MinionDefenseCueViewModel> minionDefenseCues = null,
            IEnumerable<MinionChoiceCueViewModel> minionChoiceCues = null)
        {
            Version = version;
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
            Minions = Array.AsReadOnly((minions ?? throw new ArgumentNullException(nameof(minions))).ToArray());
            MovementDestinations = Array.AsReadOnly((movementDestinations ??
                throw new ArgumentNullException(nameof(movementDestinations))).ToArray());
            AttackTargets = Array.AsReadOnly((attackTargets ??
                throw new ArgumentNullException(nameof(attackTargets))).ToArray());
            MinionAttackTargets = Array.AsReadOnly((minionAttackTargets ??
                throw new ArgumentNullException(nameof(minionAttackTargets))).ToArray());
            FocusedHeroId = focusedHeroId;
            FastTravelDestinations = Array.AsReadOnly((fastTravelDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            MinionDefenseCues = Array.AsReadOnly((minionDefenseCues ??
                Enumerable.Empty<MinionDefenseCueViewModel>()).ToArray());
            MinionChoiceCues = Array.AsReadOnly((minionChoiceCues ??
                Enumerable.Empty<MinionChoiceCueViewModel>()).ToArray());
            if (MinionChoiceCues.Select(value => value.MinionId).Distinct().Count() !=
                MinionChoiceCues.Count)
            {
                throw new ArgumentException(
                    "One minion carries at most one choice cue.",
                    nameof(minionChoiceCues));
            }
        }

        public long Version { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }
        public IReadOnlyList<HexCoordinate> MovementDestinations { get; }
        public IReadOnlyList<HeroId> AttackTargets { get; }
        public IReadOnlyList<MinionId> MinionAttackTargets { get; }
        public HeroId FocusedHeroId { get; }
        public IReadOnlyList<HexCoordinate> FastTravelDestinations { get; }
        public IReadOnlyList<MinionDefenseCueViewModel> MinionDefenseCues { get; }
        public IReadOnlyList<MinionChoiceCueViewModel> MinionChoiceCues { get; }
    }

    public interface IBoardInteractionView
    {
        void Render(BoardInteractionViewModel model);
    }

    public sealed class BoardInteractionPresenter
    {
        private readonly IMatchSession session;
        private readonly IBoardInteractionView view;
        private MatchSessionProjection lastProjection;
        private BoardInteractionViewModel lastViewModel;
        private MatchSessionProjection fastTravelSelectionProjection;
        private ActionDefinitionId? selectedFastTravelActionDefinitionId;
        private MatchSessionProjection choiceHighlightProjection;
        private MinionId highlightedChoiceMinionId;

        public BoardInteractionPresenter(IMatchSession session, IBoardInteractionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, fastTravelSelectionProjection))
            {
                selectedFastTravelActionDefinitionId = null;
                fastTravelSelectionProjection = null;
            }

            // A pointed-at candidate belongs to the projection it was read from. Anything that
            // rebuilds authoritative state - an accepted command, a rejection, restore, replay, or
            // Undo - drops it, so the board never keeps a cue for a choice that has moved on.
            if (!ReferenceEquals(projection, choiceHighlightProjection))
            {
                highlightedChoiceMinionId = default;
                choiceHighlightProjection = null;
            }

            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = new BoardInteractionViewModel(
                    projection.Version,
                    projection.Heroes,
                    projection.Minions,
                    projection.PendingMovementChoice?.LegalDestinations.Select(value => value.Destination) ??
                        Enumerable.Empty<HexCoordinate>(),
                    projection.PendingAttackTargetChoice?.LegalTargetHeroIds ?? Array.Empty<HeroId>(),
                    projection.PendingAttackTargetChoice?.LegalTargetMinionIds ?? Array.Empty<MinionId>(),
                    projection.SelectedActor.IsDefined ? projection.SelectedActor.ActorId : default,
                    GetSelectedFastTravelOption(projection)?.LegalDestinations ??
                        Array.Empty<HexCoordinate>(),
                    projection.PendingDefensePreview?.MinionContributions.Select(value =>
                        new MinionDefenseCueViewModel(value.SourceMinionId, value.Amount)) ??
                        Enumerable.Empty<MinionDefenseCueViewModel>(),
                    CreateMinionChoiceCues(
                        projection.PendingMinionBattleChoice,
                        highlightedChoiceMinionId));
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Marks one candidate of the pending minion choice as pointed at, so the board can say which
        /// minion a hovered or focused prompt control refers to. Returns false when nothing changed,
        /// which is what stops a per-frame hover notification from rebuilding the board every repaint.
        /// </summary>
        public bool HighlightMinionChoiceCandidate(MinionId minionId)
        {
            var projection = session.Projection;
            var choice = projection.PendingMinionBattleChoice;
            var resolved = choice != null && minionId.IsDefined &&
                choice.Candidates.Any(candidate => candidate.MinionId.Equals(minionId))
                    ? minionId
                    : default;
            if (resolved.Equals(highlightedChoiceMinionId) &&
                (!resolved.IsDefined || ReferenceEquals(projection, choiceHighlightProjection)))
            {
                return false;
            }

            highlightedChoiceMinionId = resolved;
            choiceHighlightProjection = resolved.IsDefined ? projection : null;
            lastProjection = null;
            return true;
        }

        private static IReadOnlyList<MinionChoiceCueViewModel> CreateMinionChoiceCues(
            PendingMinionBattleChoice choice,
            MinionId highlightedMinionId)
        {
            if (choice == null)
            {
                return Array.Empty<MinionChoiceCueViewModel>();
            }

            var cues = new List<MinionChoiceCueViewModel>();
            foreach (var candidate in choice.Candidates)
            {
                var kind = choice.SelectedMinionIds.Contains(candidate.MinionId)
                    ? MinionChoiceCueKind.Selected
                    : candidate.MinionId.Equals(highlightedMinionId)
                        ? MinionChoiceCueKind.Highlighted
                        : MinionChoiceCueKind.None;
                if (kind == MinionChoiceCueKind.None)
                {
                    continue;
                }

                cues.Add(new MinionChoiceCueViewModel(candidate.MinionId, kind));
            }

            return cues;
        }

        public bool ActivateFastTravel(ActionDefinitionId replacedMovementActionDefinitionId)
        {
            var projection = session.Projection;
            var matches = projection.AvailableFastTravelOptions
                .Where(option => option.ReplacedMovementActionDefinitionId.Equals(
                    replacedMovementActionDefinitionId) && option.LegalDestinations.Count > 0)
                .ToArray();
            if (matches.Length != 1)
            {
                return false;
            }

            selectedFastTravelActionDefinitionId = replacedMovementActionDefinitionId;
            fastTravelSelectionProjection = projection;
            lastProjection = null;
            return true;
        }

        public bool SubmitDestination(HexCoordinate destination)
        {
            var choice = session.Projection.PendingMovementChoice;
            if (choice == null || choice.LegalDestinations.Count(value => value.Destination.Equals(destination)) != 1)
            {
                var fastTravelOption = GetSelectedFastTravelOption(session.Projection);
                if (fastTravelOption == null || !fastTravelOption.LegalDestinations.Contains(destination))
                {
                    return false;
                }

                return Execute(DirectorCommand.PerformFastTravel(
                    fastTravelOption.ReplacedMovementActionDefinitionId,
                    destination));
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

        public bool SubmitMinion(MinionId minionId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetMinionIds.Contains(minionId))
            {
                return false;
            }

            return Execute(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId,
                choice.ChoiceId,
                minionId));
        }

        private FastTravelOptionProjection GetSelectedFastTravelOption(MatchSessionProjection projection)
        {
            if (!ReferenceEquals(projection, fastTravelSelectionProjection) ||
                !selectedFastTravelActionDefinitionId.HasValue)
            {
                return null;
            }

            return projection.AvailableFastTravelOptions.SingleOrDefault(option =>
                option.ReplacedMovementActionDefinitionId.Equals(
                    selectedFastTravelActionDefinitionId.Value) && option.LegalDestinations.Count > 0);
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
            HeaderLabel = teamSide == TeamSide.Red
                ? $"[R] RED {HeroDisplayName.ToUpperInvariant()}"
                : $"[B] BLUE {HeroDisplayName.ToUpperInvariant()}";
            CommitmentLabel = FormatCommitmentLabel();
            var labels = new Dictionary<CardInstanceId, string>();
            foreach (var card in Cards)
            {
                var definition = Definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
                labels.Add(card.Id, $"{definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}");
            }

            CardButtonLabels = new ReadOnlyDictionary<CardInstanceId, string>(labels);
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
        public string HeaderLabel { get; }
        public string CommitmentLabel { get; }
        public IReadOnlyDictionary<CardInstanceId, string> CardButtonLabels { get; }

        private string FormatCommitmentLabel()
        {
            switch (CommitmentStatus)
            {
                case TurnCardCommitmentStatus.AwaitingResponse:
                    return "AWAITING CARD";
                case TurnCardCommitmentStatus.CardCommittedFacedown:
                    return "FACEDOWN COMMITTED";
                case TurnCardCommitmentStatus.Passed:
                    return "PASS - NO CARD";
                case TurnCardCommitmentStatus.Revealed:
                    if (RevealedCard != null)
                    {
                        var definition = Definitions.Single(value =>
                            value.Reference.Equals(RevealedCard.DefinitionReference));
                        return $"REVEALED: {definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}";
                    }

                    return "REVEALED";
                default:
                    return CommitmentStatus.ToString().ToUpperInvariant();
            }
        }
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
        private MatchSessionProjection lastProjection;
        private CardInstanceId? lastSelectedCardInstanceId;
        private DirectorHandsViewModel lastViewModel;

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

            if (!ReferenceEquals(projection, lastProjection) ||
                !Nullable.Equals(selectedCardInstanceId, lastSelectedCardInstanceId))
            {
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
                lastProjection = projection;
                lastSelectedCardInstanceId = selectedCardInstanceId;
                lastViewModel = new DirectorHandsViewModel(projection.Version, hands);
            }

            view.Render(lastViewModel);
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
        public TypedInvocationViewModel(
            string label,
            DirectorCommand command,
            DefenseBreakdownViewModel defenseBreakdown = null)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A typed invocation requires a label.", nameof(label))
                : label;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            DefenseBreakdown = defenseBreakdown;
        }

        public string Label { get; }
        public DirectorCommand Command { get; }
        public DefenseBreakdownViewModel DefenseBreakdown { get; }
    }

    public sealed class DefenseBreakdownViewModel
    {
        public DefenseBreakdownViewModel(
            long version,
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            CardInstanceId? cardInstanceId,
            string title,
            string text,
            AttackResolutionOutcome outcome)
        {
            Version = version;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            CardInstanceId = cardInstanceId;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("A Defense breakdown requires a title.", nameof(title))
                : title;
            Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException("A Defense breakdown requires text.", nameof(text))
                : text;
            Outcome = outcome;
        }

        public long Version { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public CardInstanceId? CardInstanceId { get; }
        public string Title { get; }
        public string Text { get; }
        public AttackResolutionOutcome Outcome { get; }
    }

    public sealed class FastTravelOfferViewModel
    {
        public FastTravelOfferViewModel(
            string label,
            ActionDefinitionId replacedMovementActionDefinitionId,
            int legalDestinationCount)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Fast Travel offer requires a label.", nameof(label))
                : label;
            if (!replacedMovementActionDefinitionId.IsDefined || legalDestinationCount <= 0)
            {
                throw new ArgumentException("A Fast Travel offer requires its replaced action and legal destinations.");
            }

            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinationCount = legalDestinationCount;
        }

        public string Label { get; }
        public ActionDefinitionId ReplacedMovementActionDefinitionId { get; }
        public int LegalDestinationCount { get; }
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
            IEnumerable<TypedInvocationViewModel> defenseChoices,
            IEnumerable<FastTravelOfferViewModel> fastTravelOffers = null,
            IEnumerable<TypedInvocationViewModel> effectChoices = null)
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
            FastTravelOffers = Array.AsReadOnly((fastTravelOffers ??
                Enumerable.Empty<FastTravelOfferViewModel>()).ToArray());
            EffectChoices = Array.AsReadOnly((effectChoices ??
                Enumerable.Empty<TypedInvocationViewModel>()).ToArray());
            HudHeader = $"MIRRORED WASP QUICK GAME - DIRECTOR MODE - ROUND {roundIndex} - " +
                $"TURN {turnIndex}/{RoundResolutionAuthority.TurnsPerRound} - {phase.ToString().ToUpperInvariant()}";
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
        public IReadOnlyList<FastTravelOfferViewModel> FastTravelOffers { get; }

        /// <summary>
        /// The decision a paused effect frame is waiting on, as the exact invocations the projection
        /// already built. Empty whenever no effect choice is pending.
        /// </summary>
        public IReadOnlyList<TypedInvocationViewModel> EffectChoices { get; }

        public string HudHeader { get; }
    }

    public interface IRoundActionView
    {
        void Render(RoundActionViewModel model);
    }

    public sealed class RoundActionPresenter
    {
        private readonly IMatchSession session;
        private readonly IRoundActionView view;
        private MatchSessionProjection lastProjection;
        private RoundActionViewModel lastViewModel;

        public RoundActionPresenter(IMatchSession session, IRoundActionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;
        public event Action<FastTravelOfferViewModel> FastTravelSelectionRequested;

        public void Refresh()
        {
            view.Render(GetOrCreateViewModel(session.Projection));
        }

        public bool Submit(TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legal = current.Actions
                .Concat(current.InitiativeChoices)
                .Concat(current.DefenseChoices)
                .Concat(current.EffectChoices)
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

        public bool SelectFastTravel(FastTravelOfferViewModel selection)
        {
            if (selection == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legalSelection = current.FastTravelOffers.SingleOrDefault(option =>
                option.ReplacedMovementActionDefinitionId.Equals(
                    selection.ReplacedMovementActionDefinitionId) &&
                option.LegalDestinationCount == selection.LegalDestinationCount);
            if (legalSelection == null)
            {
                return false;
            }

            FastTravelSelectionRequested?.Invoke(legalSelection);
            return true;
        }

        public static RoundActionViewModel CreateViewModel(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            // A paused Skill frame counts here for the same reason paused Movement and Attack do:
            // Core refuses a second action while one is in progress, so an offer shown during the
            // pause is a control that can only produce a rejection.
            var hasPendingResolution = projection.PendingInitiativeChoice != null ||
                projection.PendingMovementChoice != null || projection.PendingMovementResolution != null ||
                projection.PendingAttackTargetChoice != null || projection.PendingDefenseChoice != null ||
                projection.PendingAttackResolution != null ||
                projection.PendingSkillResolution != null || projection.PendingEffectChoice != null;
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
            var fastTravelOffers = hasPendingResolution
                ? Array.Empty<FastTravelOfferViewModel>()
                : projection.AvailableFastTravelOptions
                    .Select(option => new FastTravelOfferViewModel(
                        "FAST TRAVEL",
                        option.ReplacedMovementActionDefinitionId,
                        option.LegalDestinations.Count))
                    .ToArray();

            return new RoundActionViewModel(
                projection.Version,
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase,
                FormatStatus(projection),
                actions,
                CreateEndTurnInvocation(projection),
                // A pending removal choice is mandatory: the authority rejects an advance while it
                // stands, so the control that would produce that rejection is not offered.
                projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.PendingMinionBattleChoice == null
                    ? new TypedInvocationViewModel("CONTINUE END OF ROUND", DirectorCommand.AdvanceEndOfRound())
                    : null,
                initiativeChoices,
                defenseChoices,
                fastTravelOffers,
                CreateEffectChoices(projection.PendingEffectChoice));
        }

        /// <summary>
        /// Forwards a paused effect frame's decision without deciding anything. Every invocation here
        /// is one the projection already constructed and bounded - which candidates may still be
        /// selected, whether the selection satisfies the minimum, whether declining is allowed - so
        /// this method chooses only the order they are shown in, which is the projection's own.
        /// </summary>
        private static IReadOnlyList<TypedInvocationViewModel> CreateEffectChoices(
            PendingEffectChoice choice)
        {
            if (choice == null)
            {
                return Array.Empty<TypedInvocationViewModel>();
            }

            var ordinals = new Dictionary<EffectChoiceCandidate, int>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                ordinals[choice.Candidates[index]] = index + 1;
            }

            var invocations = new List<TypedInvocationViewModel>();
            foreach (var invocation in choice.SelectInvocations)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.CandidateLabel(
                        invocation.Candidate.OptionNodeId, ordinals[invocation.Candidate]),
                    DirectorCommand.AnswerEffectChoice(invocation)));
            }

            foreach (var invocation in choice.DeselectInvocations)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeselectLabel(
                        invocation.Candidate.OptionNodeId, ordinals[invocation.Candidate]),
                    DirectorCommand.AnswerEffectChoice(invocation)));
            }

            if (choice.ConfirmInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.ConfirmLabel,
                    DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation)));
            }

            if (choice.DeclineInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeclineLabel,
                    DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation)));
            }

            return invocations;
        }

        private RoundActionViewModel GetOrCreateViewModel(MatchSessionProjection projection)
        {
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            return lastViewModel;
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
                projection.PendingMovementResolution != null ||
                projection.PendingSkillResolution != null || projection.PendingEffectChoice != null;
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
            var preview = projection.PendingDefensePreview;
            if (choice == null || preview == null ||
                !preview.ResolutionId.Equals(choice.ResolutionId) ||
                !preview.ChoiceId.Equals(choice.ChoiceId))
            {
                return Array.Empty<TypedInvocationViewModel>();
            }

            var values = choice.LegalDefenseOffers
                .Select(offer =>
                {
                    var choicePreview = preview.Choices.Single(value =>
                        value.CardInstanceId.HasValue &&
                        value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));
                    var printed = choicePreview.DefenseContributions.Single(value =>
                        value.Kind == ValueContributionKind.PrintedIcon);
                    return new TypedInvocationViewModel(
                        $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                        $"PRINTED DEFENSE {printed.Amount}",
                        DirectorCommand.ChooseDefenseCard(
                            choice.ResolutionId,
                            choice.ChoiceId,
                            offer.SourceCardInstanceId),
                        CreateDefenseBreakdown(projection.Version, preview, choicePreview));
                })
                .ToList();
            if (choice.CanDecline)
            {
                var declinePreview = preview.Choices.Single(value => value.IsDecline);
                values.Add(new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    DirectorCommand.DeclineDefense(choice.ResolutionId, choice.ChoiceId),
                    CreateDefenseBreakdown(projection.Version, preview, declinePreview)));
            }

            return values;
        }

        public static DefenseBreakdownViewModel CreateDefenseBreakdown(
            long version,
            PendingDefensePreviewProjection preview,
            DefenseChoicePreviewProjection choice)
        {
            var lines = new List<string> { "ATTACK:" };
            lines.AddRange(preview.AttackContributions.Select(FormatContribution));
            lines.Add($"ATTACK TOTAL: {preview.AttackTotal}");
            lines.Add(string.Empty);
            lines.Add("DEFENSE:");
            if (!choice.IsDecline)
            {
                lines.Add($"  CARD: {choice.ChoiceLabel.ToUpperInvariant()}");
                lines.AddRange(choice.DefenseContributions.Select(FormatContribution));
            }
            if (choice.BlockResult == DefenseBlockResult.Blocked)
            {
                lines.Add("  BLOCK");
                lines.Add("RESULT: BLOCK - DEFENDED");
            }
            else if (!choice.DefenseTotal.HasValue)
            {
                lines.Add("  NO DEFENSE TOTAL");
                lines.Add("RESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS");
            }
            else
            {
                lines.Add($"DEFENSE TOTAL: {choice.DefenseTotal.Value}");
                lines.Add(choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                    ? "RESULT: DEFENDED"
                    : "RESULT: ATTACK SUCCEEDS");
            }

            return new DefenseBreakdownViewModel(
                version,
                preview.ResolutionId,
                preview.ChoiceId,
                choice.CardInstanceId,
                choice.IsDecline
                    ? "DEFENSE PREVIEW: DECLINE"
                    : $"DEFENSE PREVIEW: {choice.ChoiceLabel.ToUpperInvariant()}",
                string.Join("\n", lines),
                choice.Outcome);
        }

        private static string FormatContribution(ValueContributionProjection contribution)
        {
            var sign = contribution.Amount >= 0 ? "+" : string.Empty;
            return $"  {sign}{contribution.Amount} — {contribution.SourceLabel}";
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

    public sealed class MinionChoiceCandidateViewModel
    {
        public MinionChoiceCandidateViewModel(
            MinionId minionId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command)
        {
            if (!minionId.IsDefined || ordinal < 1)
            {
                throw new ArgumentException(
                    "A minion choice candidate requires a stable identity and its projected ordinal.");
            }

            MinionId = minionId;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A minion choice candidate requires a label.", nameof(label))
                : label;
            Ordinal = ordinal;
            IsSelected = isSelected;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public MinionId MinionId { get; }
        public string Label { get; }
        public int Ordinal { get; }

        /// <summary>
        /// Whether this candidate is currently part of the authoritative selection. The control it
        /// carries is then the projected deselect invocation rather than the select one.
        /// </summary>
        public bool IsSelected { get; }
        public DirectorCommand Command { get; }
    }

    /// <summary>
    /// The contextual prompt for a pending minion choice. It holds only what the authoritative
    /// pending choice already decided - chooser identity, required count, current selection, the
    /// candidates that carry a legal control right now, and the exact typed invocations - so the view
    /// never compares minion counts, orders Heavy removals, or advances the owning continuation.
    /// </summary>
    public sealed class MinionChoiceViewModel
    {
        public MinionChoiceViewModel(
            long version,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            int requiredRemovalCount,
            int selectedCount,
            IEnumerable<MinionChoiceCandidateViewModel> candidates,
            TypedInvocationViewModel confirmInvocation)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !choosingTeamId.IsDefined)
            {
                throw new ArgumentException(
                    "A minion choice prompt requires the authoritative resolution, choice, and team identities.");
            }

            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(choosingSide));
            }

            if (requiredRemovalCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredRemovalCount));
            }

            if (selectedCount < 0 || selectedCount > requiredRemovalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedCount));
            }

            Version = version;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoosingTeamId = choosingTeamId;
            ChoosingSide = choosingSide;
            RequiredRemovalCount = requiredRemovalCount;
            SelectedCount = selectedCount;
            Candidates = Array.AsReadOnly((candidates ??
                throw new ArgumentNullException(nameof(candidates))).ToArray());
            if (Candidates.Select(value => value.MinionId).Distinct().Count() != Candidates.Count)
            {
                throw new ArgumentException(
                    "A minion choice prompt offers at most one control per minion.",
                    nameof(candidates));
            }

            ConfirmInvocation = confirmInvocation;
            PromptLabel = $"MINION BATTLE - {MinionChoiceLabels.TeamLabel(choosingSide)} REMOVES " +
                $"{requiredRemovalCount} {(requiredRemovalCount == 1 ? "MINION" : "MINIONS")}";
            StatusLabel = $"SELECTED {selectedCount} OF {requiredRemovalCount}";
        }

        public long Version { get; }
        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }
        public TeamId ChoosingTeamId { get; }
        public TeamSide ChoosingSide { get; }
        public int RequiredRemovalCount { get; }
        public int SelectedCount { get; }
        public IReadOnlyList<MinionChoiceCandidateViewModel> Candidates { get; }

        /// <summary>Present only while the authoritative choice reports it can be confirmed.</summary>
        public TypedInvocationViewModel ConfirmInvocation { get; }
        public string PromptLabel { get; }
        public string StatusLabel { get; }
    }

    /// <summary>
    /// The passive port for the minion-choice prompt. A null model means no minion choice is pending
    /// and therefore no control may be shown.
    /// </summary>
    public interface IMinionChoiceView
    {
        void Render(MinionChoiceViewModel model);
    }

    public sealed class MinionChoicePresenter
    {
        private readonly IMatchSession session;
        private readonly IMinionChoiceView view;
        private MatchSessionProjection lastProjection;
        private MinionChoiceViewModel lastViewModel;

        public MinionChoicePresenter(IMatchSession session, IMinionChoiceView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Forwards the exact projected select or deselect invocation the candidate carries. A control
        /// left over from an earlier projection matches nothing current, so it submits nothing and the
        /// view is rebuilt from the authoritative pending choice instead.
        /// </summary>
        public bool Submit(MinionChoiceCandidateViewModel candidate)
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (candidate == null || current == null ||
                !current.Candidates.Any(value => value.Command.Equals(candidate.Command)))
            {
                Refresh();
                return false;
            }

            return Execute(candidate.Command);
        }

        public bool Confirm()
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (current?.ConfirmInvocation == null)
            {
                Refresh();
                return false;
            }

            return Execute(current.ConfirmInvocation.Command);
        }

        public static MinionChoiceViewModel CreateViewModel(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var choice = projection.PendingMinionBattleChoice;
            if (choice == null)
            {
                return null;
            }

            var candidates = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                var candidate = choice.Candidates[index];
                var select = choice.SelectInvocations.SingleOrDefault(value =>
                    value.MinionId.Equals(candidate.MinionId));
                var deselect = choice.DeselectInvocations.SingleOrDefault(value =>
                    value.MinionId.Equals(candidate.MinionId));
                if (select == null && deselect == null)
                {
                    // The authority currently offers neither invocation for this minion - a Heavy
                    // while a non-Heavy is still unselected is the live case. Presentation offers no
                    // control rather than reproducing the ordering rule that withheld it.
                    continue;
                }

                var isSelected = deselect != null;
                var ordinal = index + 1;
                candidates.Add(new MinionChoiceCandidateViewModel(
                    candidate.MinionId,
                    $"{(isSelected ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.CandidateLabel(choice.ChoosingSide, candidate.MinionType, ordinal)}",
                    ordinal,
                    isSelected,
                    isSelected
                        ? DirectorCommand.DeselectMinionBattleRemoval(deselect)
                        : DirectorCommand.SelectMinionBattleRemoval(select)));
            }

            return new MinionChoiceViewModel(
                projection.Version,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.LosingTeamId,
                choice.ChoosingSide,
                choice.RequiredRemovalCount,
                choice.SelectedMinionIds.Count,
                candidates,
                choice.CanConfirm
                    ? new TypedInvocationViewModel(
                        choice.RequiredRemovalCount == 1 ? "CONFIRM REMOVAL" : "CONFIRM REMOVALS",
                        DirectorCommand.ConfirmMinionBattleRemovals(choice.ConfirmInvocation))
                    : null);
        }

        private MinionChoiceViewModel GetOrCreateViewModel(MatchSessionProjection projection)
        {
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            return lastViewModel;
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Execute(command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }
            else
            {
                // A refusal leaves the authoritative choice standing and hands back the projection
                // that still holds it. Rebuilding from that is what puts the visible selection back
                // in step with authority instead of with the control the player just used.
                Refresh();
            }

            return result.Accepted;
        }
    }

    public sealed class UndoRestartViewModel
    {
        public UndoRestartViewModel(
            long version,
            bool canUndo,
            bool canRestart,
            int acceptedCommandCount,
            bool isHistoryAvailable)
        {
            Version = version;
            CanUndo = canUndo;
            CanRestart = canRestart;
            AcceptedCommandCount = acceptedCommandCount;
            IsHistoryAvailable = isHistoryAvailable;
        }

        public long Version { get; }
        public bool CanUndo { get; }
        public bool CanRestart { get; }
        public int AcceptedCommandCount { get; }

        /// <summary>
        /// False once a snapshot has been loaded, which permanently disables history. Both states
        /// leave <see cref="CanUndo"/> false, but only this one means no future command will ever
        /// make it true, so a view can say "history unavailable" rather than "nothing to undo".
        /// </summary>
        public bool IsHistoryAvailable { get; }
    }

    public interface IUndoRestartView
    {
        void Render(UndoRestartViewModel model);
    }

    public sealed class UndoRestartPresenter
    {
        private readonly IMatchSession session;
        private readonly IUndoRestartView view;
        private MatchSessionProjection lastProjection;
        private UndoRestartViewModel lastViewModel;

        public UndoRestartPresenter(IMatchSession session, IUndoRestartView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = new UndoRestartViewModel(
                    projection.Version,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count,
                    projection.IsHistoryAvailable);
            }

            view.Render(lastViewModel);
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
            ToggleLabel = IsExpanded ? "HIDE EVENT LOG" : $"EVENT LOG ({Entries.Count})";
            PanelLabel = $"INTERNAL RULE-EVENT LOG - {Entries.Count} ACCEPTED EVENTS";
        }

        public long Version { get; }
        public bool IsExpanded { get; }
        public IReadOnlyList<RuleEventLogEntry> Entries { get; }
        public string ToggleLabel { get; }
        public string PanelLabel { get; }
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
        private MatchSessionProjection lastProjection;
        private bool lastExpanded;
        private DiagnosticLogViewModel lastViewModel;

        public DiagnosticLogPresenter(IMatchSession session, IDiagnosticLogView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection) || isExpanded != lastExpanded)
            {
                lastProjection = projection;
                lastExpanded = isExpanded;
                lastViewModel = new DiagnosticLogViewModel(
                    projection.Version,
                    isExpanded,
                    projection.DiagnosticEvents);
            }

            view.Render(lastViewModel);
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
        private readonly MinionChoicePresenter minionChoice;

        public MatchFeatureCoordinator(
            BoardInteractionPresenter board,
            DirectorHandsPresenter hands,
            RoundActionPresenter round,
            UndoRestartPresenter undoRestart,
            DiagnosticLogPresenter diagnostics,
            MinionChoicePresenter minionChoice,
            Action<MatchSessionProjection> afterSessionChange = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hands = hands ?? throw new ArgumentNullException(nameof(hands));
            this.round = round ?? throw new ArgumentNullException(nameof(round));
            this.undoRestart = undoRestart ?? throw new ArgumentNullException(nameof(undoRestart));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            this.minionChoice = minionChoice ?? throw new ArgumentNullException(nameof(minionChoice));

            void Changed(MatchSessionProjection projection)
            {
                afterSessionChange?.Invoke(projection);
                RefreshAll();
            }

            board.SessionProjectionChanged += Changed;
            hands.SessionProjectionChanged += Changed;
            round.SessionProjectionChanged += Changed;
            undoRestart.SessionProjectionChanged += Changed;
            minionChoice.SessionProjectionChanged += Changed;
        }

        public void RefreshAll()
        {
            board.Refresh();
            hands.Refresh();
            round.Refresh();
            undoRestart.Refresh();
            diagnostics.Refresh();
            minionChoice.Refresh();
        }
    }
}
