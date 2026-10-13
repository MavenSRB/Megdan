using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{

    /// <summary>
    /// Which authoritative minion-choice family the one shared prompt is presenting. Milestone 6.1a
    /// built the interface for the Minion Battle removal; milestone 6.4b widened it by prompt kind for
    /// the displaced-minion correction rather than adding a second correction-only framework, so the
    /// presenter's submit, confirm, and stale-refresh paths never branch on this value - only labels
    /// and candidate identity do.
    /// </summary>
    public enum MinionChoicePromptKind
    {
        MinionBattleRemoval = 0,
        CorrectionSubject = 1,
        CorrectionMove = 2,
        CorrectionPlace = 3
    }

    public sealed class MinionChoiceCandidateViewModel
    {
        public MinionChoiceCandidateViewModel(
            MinionId minionId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command)
            : this(RuleObject.ForMinion(minionId), label, ordinal, isSelected, command, null, null)
        {
        }

        public MinionChoiceCandidateViewModel(
            HeroId heroId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command)
            : this(RuleObject.ForHero(heroId), label, ordinal, isSelected, command, null, null)
        {
        }

        /// <summary>
        /// Milestone 6.4b's destination-bearing form: a correction route endpoint or Place space. The
        /// minion is the displaced subject the destination would receive, so every candidate of one
        /// destination prompt shares it; the destination is what distinguishes them.
        /// </summary>
        public MinionChoiceCandidateViewModel(
            MinionId minionId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command,
            HexCoordinate? destination)
            : this(
                RuleObject.ForMinion(minionId), label, ordinal, isSelected, command, destination, null)
        {
        }

        /// <summary>
        /// Milestone 9.29's form: one of this seat's own Minion Battle picks, answered through the
        /// host agreement rather than a rules command. <paramref name="isSelected"/> is whether the
        /// unit is already among this seat's picks, in which case the statement takes it back.
        /// </summary>
        public MinionChoiceCandidateViewModel(
            RuleObject unit,
            string label,
            int ordinal,
            bool isSelected,
            TeamAgreementCommand agreementCommand)
            : this(
                unit, label, ordinal, isSelected, null, null,
                agreementCommand ?? throw new ArgumentNullException(nameof(agreementCommand)))
        {
        }

        private MinionChoiceCandidateViewModel(
            RuleObject unit,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command,
            HexCoordinate? destination,
            TeamAgreementCommand agreementCommand)
        {
            if (!unit.IsDefined || ordinal < 1)
            {
                throw new ArgumentException(
                    "A minion choice candidate requires a stable identity and its projected ordinal.");
            }

            Unit = unit;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A minion choice candidate requires a label.", nameof(label))
                : label;
            Ordinal = ordinal;
            IsSelected = isSelected;
            Command = command ?? (agreementCommand != null
                ? null
                : throw new ArgumentNullException(nameof(command)));
            AgreementCommand = agreementCommand;
            Destination = destination;
        }

        public RuleObject Unit { get; }
        public MinionId MinionId => Unit.Kind == RuleObjectKind.Minion ? Unit.MinionId : default;
        public HeroId HeroId => Unit.Kind == RuleObjectKind.Hero ? Unit.HeroId : default;
        public string Label { get; }
        public int Ordinal { get; }

        /// <summary>
        /// Whether this candidate is currently part of the authoritative selection. The control it
        /// carries is then the projected deselect invocation rather than the select one.
        /// </summary>
        public bool IsSelected { get; }

        /// <summary>The rules command this control submits; <c>null</c> on a seat's own pick.</summary>
        public DirectorCommand Command { get; }

        /// <summary>Milestone 9.29. The pick or unpick statement a seat's control submits instead.</summary>
        public TeamAgreementCommand AgreementCommand { get; }

        /// <summary>The correction destination this control selects, or <c>null</c> for a minion control.</summary>
        public HexCoordinate? Destination { get; }
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
            TypedInvocationViewModel confirmInvocation,
            TeamAgreementHudViewModel agreement = null)
            : this(
                version, MinionChoicePromptKind.MinionBattleRemoval, resolutionId, choiceId,
                default, default, default, choosingTeamId, choosingSide, requiredRemovalCount,
                selectedCount, candidates, confirmInvocation, agreement)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion Battle prompt requires the authoritative resolution and choice identities.");
            }
        }

        /// <summary>
        /// Milestone 6.4b's correction form of the same prompt: the displaced subject, the correction
        /// kind, and destination- or subject-valued candidates, addressed by the owning action's
        /// identity rather than an end-of-round one. Selection is the advance for a correction, so it
        /// carries no confirm invocation - the shared contract's confirm/advance slot is satisfied by
        /// each candidate's one-shot command.
        /// </summary>
        public static MinionChoiceViewModel ForCorrection(
            long version,
            MinionChoicePromptKind kind,
            ActionResolutionId actionResolutionId,
            ActionResolutionStepId actionChoiceId,
            MinionId subjectMinionId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            IEnumerable<MinionChoiceCandidateViewModel> candidates)
        {
            if (kind == MinionChoicePromptKind.MinionBattleRemoval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "The correction form presents correction prompt kinds only.");
            }

            if (!actionResolutionId.IsDefined || !actionChoiceId.IsDefined)
            {
                throw new ArgumentException(
                    "A correction prompt requires the owning action's resolution and choice identities.");
            }

            if (kind != MinionChoicePromptKind.CorrectionSubject && !subjectMinionId.IsDefined)
            {
                throw new ArgumentException(
                    "A correction destination prompt names its displaced subject.", nameof(subjectMinionId));
            }

            return new MinionChoiceViewModel(
                version, kind, default, default, actionResolutionId, actionChoiceId,
                subjectMinionId, choosingTeamId, choosingSide, 1, 0, candidates, null, null);
        }

        private MinionChoiceViewModel(
            long version,
            MinionChoicePromptKind kind,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            ActionResolutionId actionResolutionId,
            ActionResolutionStepId actionChoiceId,
            MinionId subjectMinionId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            int requiredRemovalCount,
            int selectedCount,
            IEnumerable<MinionChoiceCandidateViewModel> candidates,
            TypedInvocationViewModel confirmInvocation,
            TeamAgreementHudViewModel agreement)
        {
            if (!choosingTeamId.IsDefined)
            {
                throw new ArgumentException(
                    "A minion choice prompt requires the authoritative team identity.");
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
            Kind = kind;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ActionResolutionId = actionResolutionId;
            ActionChoiceId = actionChoiceId;
            SubjectMinionId = subjectMinionId;
            ChoosingTeamId = choosingTeamId;
            ChoosingSide = choosingSide;
            RequiredRemovalCount = requiredRemovalCount;
            SelectedCount = selectedCount;
            Candidates = Array.AsReadOnly((candidates ??
                throw new ArgumentNullException(nameof(candidates))).ToArray());

            // One control per distinguishing identity: minions for the battle and subject prompts,
            // destinations for a correction destination prompt, whose candidates all share the one
            // displaced subject.
            var distinct = kind == MinionChoicePromptKind.CorrectionMove ||
                kind == MinionChoicePromptKind.CorrectionPlace
                    ? Candidates.Select(value => (object)value.Destination).Distinct().Count()
                    : Candidates.Select(value => (object)value.MinionId).Distinct().Count();
            if (distinct != Candidates.Count)
            {
                throw new ArgumentException(
                    "A minion choice prompt offers at most one control per candidate identity.",
                    nameof(candidates));
            }

            ConfirmInvocation = confirmInvocation;
            Agreement = agreement;
            var team = MinionChoiceLabels.TeamLabel(choosingSide);
            switch (kind)
            {
                case MinionChoicePromptKind.CorrectionSubject:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION - {team} RETURNS A DISPLACED MINION";
                    break;
                case MinionChoicePromptKind.CorrectionMove:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION (MOVE) - {team} CHOOSES THE RETURN SPACE";
                    break;
                case MinionChoicePromptKind.CorrectionPlace:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION (PLACE) - {team} CHOOSES THE PLACE SPACE";
                    break;
                default:
                    SectionLabel = "Minion Battle";
                    PromptLabel = $"MINION BATTLE - {team} REMOVES " +
                        $"{requiredRemovalCount} {(requiredRemovalCount == 1 ? "MINION" : "MINIONS")}";
                    break;
            }

            StatusLabel = $"SELECTED {selectedCount} OF {requiredRemovalCount}";
        }

        public long Version { get; }
        public MinionChoicePromptKind Kind { get; }
        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }

        /// <summary>The owning action's identity when <see cref="Kind"/> is a correction prompt.</summary>
        public ActionResolutionId ActionResolutionId { get; }
        public ActionResolutionStepId ActionChoiceId { get; }

        /// <summary>The displaced subject of a correction destination prompt; undefined otherwise.</summary>
        public MinionId SubjectMinionId { get; }
        public TeamId ChoosingTeamId { get; }
        public TeamSide ChoosingSide { get; }
        public int RequiredRemovalCount { get; }
        public int SelectedCount { get; }
        public IReadOnlyList<MinionChoiceCandidateViewModel> Candidates { get; }

        /// <summary>Present only while the authoritative choice reports it can be confirmed.</summary>
        public TypedInvocationViewModel ConfirmInvocation { get; }
        public TeamAgreementHudViewModel Agreement { get; }

        public string PromptLabel { get; }
        public string StatusLabel { get; }

        /// <summary>The prompt family's short gutter label, so the view carries no literal of its own.</summary>
        public string SectionLabel { get; }
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
        private readonly IMatchClient session;
        private readonly IMinionChoiceView view;
        private IMatchReadModel lastProjection;
        private MinionChoiceViewModel lastViewModel;
        private bool? lastCanSubmitPlayerCommands;


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public MinionChoicePresenter(IMatchSession session, IMinionChoiceView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public MinionChoicePresenter(IMatchClient session, IMinionChoiceView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;

        public void Refresh()
        {
            var canSubmitPlayerCommands = MatchClientAuthority.CanSubmitPlayerCommands(session);
            if (lastCanSubmitPlayerCommands != canSubmitPlayerCommands)
            {
                lastCanSubmitPlayerCommands = canSubmitPlayerCommands;
                lastProjection = null;
            }

            if (!canSubmitPlayerCommands)
            {
                lastProjection = session.Projection;
                lastViewModel = null;
                view.Render(null);
                return;
            }

            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Milestone 8.11a, at the user's request. Answers the Minion Battle by clicking the unit it is
        /// asking about, which is the same click that takes an Attack target.
        ///
        /// <para>The board already identified every candidate before this milestone; the unified
        /// <see cref="PieceTargetCueViewModel"/> now carries that same fact. The piece a player was
        /// being asked about was already the piece they were looking at. What was missing was only
        /// that clicking it did anything.</para>
        /// </summary>
        public bool SubmitUnit(RuleObject unit)
        {
            if (!unit.IsDefined)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var candidate = current?.Candidates.SingleOrDefault(value => value.Unit.Equals(unit));
            return candidate != null && Answer(candidate);
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
                !current.Candidates.Any(value =>
                    Equals(value.Command, candidate.Command) &&
                    Equals(value.AgreementCommand, candidate.AgreementCommand)))
            {
                Refresh();
                return false;
            }

            return Answer(candidate);
        }

        private bool Answer(MinionChoiceCandidateViewModel candidate)
        {
            if (candidate.AgreementCommand == null)
            {
                return Execute(candidate.Command);
            }

            var result = session.SubmitAgreement(candidate.AgreementCommand);
            if (result.Accepted)
                SessionProjectionChanged?.Invoke(result.Projection);
            else
                Refresh();
            return result.Accepted;
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

        public bool SubmitAgreement(TeamAgreementChoiceViewModel choice)
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (choice == null || current?.Agreement?.Choices.Any(value =>
                    value.Command.Equals(choice.Command)) != true)
            {
                Refresh();
                return false;
            }

            var result = session.SubmitAgreement(choice.Command);
            if (result.Accepted)
                SessionProjectionChanged?.Invoke(result.Projection);
            else
                Refresh();
            return result.Accepted;
        }

        public static MinionChoiceViewModel CreateViewModel(IMatchReadModel projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var choice = projection.PendingMinionBattleChoice;
            if (projection.TeamAgreement?.Kind == TeamAgreementKind.MinionBattle)
            {
                // Milestone 9.29. A seat never answers the battle with a rules command any more; it
                // picks for itself through the agreement, and the host removes what every pick names.
                return CreatePickingViewModel(projection, choice);
            }

            if (choice == null)
            {
                // At most one minion-choice family is ever pending: the battle is end-of-round state
                // and the correction pauses mid-turn, so this is a route rather than a priority.
                return CreateCorrectionViewModel(projection);
            }

            var candidates = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                var candidate = choice.Candidates[index];
                var select = choice.SelectInvocations.SingleOrDefault(value =>
                    value.Unit.Equals(candidate.Unit));
                var deselect = choice.DeselectInvocations.SingleOrDefault(value =>
                    value.Unit.Equals(candidate.Unit));
                if (select == null && deselect == null)
                {
                    // The authority currently offers neither invocation for this minion - a Heavy
                    // while a non-Heavy is still unselected is the live case. Presentation offers no
                    // control rather than reproducing the ordering rule that withheld it.
                    continue;
                }

                var isSelected = deselect != null;
                var ordinal = index + 1;
                var typedInvocation = isSelected ? deselect : select;
                var expectedKind = isSelected
                    ? DirectorCommandKind.DeselectMinionBattleRemoval
                    : DirectorCommandKind.SelectMinionBattleRemoval;
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.MinionBattle,
                    candidateCommand => candidateCommand.Kind == expectedKind &&
                        candidateCommand.TeamSide == typedInvocation.ChoosingSide &&
                        candidateCommand.MinionId.Equals(typedInvocation.MinionId) &&
                        candidateCommand.HeroId.Equals(typedInvocation.HeroId) &&
                        candidateCommand.EndOfRoundResolutionId.Equals(
                            typedInvocation.ResolutionId) &&
                        candidateCommand.EndOfRoundChoiceId.Equals(typedInvocation.ChoiceId));
                var label = candidate.HeroId.IsDefined
                    ? $"{(isSelected ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.TeamLabel(choice.ChoosingSide)} HERO {candidate.HeroId.Value.ToUpperInvariant()}"
                    : $"{(isSelected ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.CandidateLabel(choice.ChoosingSide, candidate.MinionType, ordinal)}";
                candidates.Add(candidate.HeroId.IsDefined
                    ? new MinionChoiceCandidateViewModel(
                        candidate.HeroId, label, ordinal, isSelected, command)
                    : new MinionChoiceCandidateViewModel(
                        candidate.MinionId, label, ordinal, isSelected, command));
            }

            return new MinionChoiceViewModel(
                projection.Version,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.LosingTeamId,
                choice.ChoosingSide,
                choice.RequiredRemovalCount,
                choice.SelectedUnits.Count,
                candidates,
                projection.TeamAgreement == null && choice.CanConfirm
                    ? new TypedInvocationViewModel(
                        choice.RequiredRemovalCount == 1 ? "CONFIRM REMOVAL" : "CONFIRM REMOVALS",
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.MinionBattle,
                            command => command.Kind ==
                                    DirectorCommandKind.ConfirmMinionBattleRemovals &&
                                command.TeamSide == choice.ConfirmInvocation.ChoosingSide &&
                                command.EndOfRoundResolutionId.Equals(
                                    choice.ConfirmInvocation.ResolutionId) &&
                                command.EndOfRoundChoiceId.Equals(
                                    choice.ConfirmInvocation.ChoiceId)))
                    : null,
                TeamAgreementHudPresentation.Create(projection));
        }

        /// <summary>
        /// Milestone 9.29. One seat's own picks: a control for every unit this seat may pick or take
        /// back, each carrying the agreement statement that does it.
        /// </summary>
        /// <remarks>
        /// A seat on the other side is served no pending choice and no picks, and draws nothing here -
        /// the status line says the other side is choosing. So does a choosing seat whose own
        /// statement is in flight, for the one round trip its pending detail is withheld (8.35e's
        /// window): the board keeps what it last drew until the answer returns.
        /// </remarks>
        private static MinionChoiceViewModel CreatePickingViewModel(
            IMatchReadModel projection, PendingMinionBattleChoice choice)
        {
            var agreement = projection.TeamAgreement;
            if (choice == null)
            {
                return null;
            }

            var picks = agreement.ViewerPicks;
            var candidates = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                var candidate = choice.Candidates[index];
                var picked = picks.Contains(candidate.Unit);
                var statement = agreement.AuthorizedCommands.FirstOrDefault(command =>
                    command.Unit.Equals(candidate.Unit) &&
                    command.Kind == (picked
                        ? TeamAgreementCommandKind.UnpickMinionBattleRemoval
                        : TeamAgreementCommandKind.PickMinionBattleRemoval));
                if (statement == null)
                {
                    // Nothing this seat may say about this unit right now - a Heavy while a non-Heavy
                    // is unpicked is the live case. Presentation offers no control rather than
                    // restating the order that withheld it.
                    continue;
                }

                var ordinal = index + 1;
                var label = candidate.HeroId.IsDefined
                    ? $"{(picked ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.TeamLabel(choice.ChoosingSide)} HERO {candidate.HeroId.Value.ToUpperInvariant()}"
                    : $"{(picked ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.CandidateLabel(choice.ChoosingSide, candidate.MinionType, ordinal)}";
                candidates.Add(new MinionChoiceCandidateViewModel(
                    candidate.Unit, label, ordinal, picked, statement));
            }

            return new MinionChoiceViewModel(
                projection.Version,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.LosingTeamId,
                choice.ChoosingSide,
                choice.RequiredRemovalCount,
                picks.Count,
                candidates,
                null,
                TeamAgreementHudPresentation.Create(projection));
        }

        /// <summary>
        /// Milestone 6.4b: the displaced-minion correction rides the same prompt. Everything shown is
        /// read off the projected pending choice - candidates, kinds, invocations - so presentation
        /// calculates no path, distance, Battle-Zone membership, occupancy, or Tie-Breaker ordering.
        /// A subject label joins the projection's minion occupancy for team and intrinsic type, which
        /// is a lookup of projected state, not a rules answer.
        /// </summary>
        private static MinionChoiceViewModel CreateCorrectionViewModel(IMatchReadModel projection)
        {
            var pending = projection.PendingDisplacedMinionCorrection;
            if (pending == null)
            {
                return null;
            }

            if (pending.Stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                var subjects = new List<MinionChoiceCandidateViewModel>();
                for (var index = 0; index < pending.SubjectCandidates.Count; index++)
                {
                    var subject = pending.SubjectCandidates[index];
                    var occupancy = projection.Minions.Single(minion =>
                        minion.MinionId.Equals(subject.MinionId));
                    var invocation = pending.SelectInvocations.Single(value =>
                        value.MinionId.Equals(subject.MinionId));
                    var ordinal = index + 1;
                    subjects.Add(new MinionChoiceCandidateViewModel(
                        subject.MinionId,
                        $"RETURN {MinionChoiceLabels.CandidateLabel(occupancy.TeamSide, occupancy.MinionType, ordinal)}",
                        ordinal,
                        false,
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Correction,
                            command => command.Kind == DirectorCommandKind.ChooseCorrectionSubject &&
                                command.TeamSide == invocation.ChoosingSide &&
                                command.MinionId.Equals(invocation.MinionId) &&
                                command.ResolutionId.Equals(invocation.ResolutionId) &&
                                command.ChoiceId.Equals(invocation.ChoiceId))));
                }

                return MinionChoiceViewModel.ForCorrection(
                    projection.Version,
                    MinionChoicePromptKind.CorrectionSubject,
                    pending.ResolutionId,
                    pending.ChoiceId,
                    default,
                    pending.ChoosingTeamId,
                    pending.ChoosingSide,
                    subjects);
            }

            var kind = pending.Kind == DisplacedMinionCorrectionKind.Place
                ? MinionChoicePromptKind.CorrectionPlace
                : MinionChoicePromptKind.CorrectionMove;
            var verb = kind == MinionChoicePromptKind.CorrectionPlace ? "PLACE AT" : "MOVE TO";
            var destinations = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < pending.DestinationCandidates.Count; index++)
            {
                var candidate = pending.DestinationCandidates[index];
                var invocation = pending.SelectInvocations.Single(value =>
                    value.Destination.Equals(candidate.Destination));
                var ordinal = index + 1;
                destinations.Add(new MinionChoiceCandidateViewModel(
                    pending.SubjectMinionId,
                    $"{verb} ({candidate.Destination.Q},{candidate.Destination.R})",
                    ordinal,
                    false,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Correction,
                        command => command.Kind == DirectorCommandKind.ChooseCorrectionDestination &&
                            command.TeamSide == invocation.ChoosingSide &&
                            command.MinionId.Equals(invocation.MinionId) &&
                            command.Coordinate.Equals(invocation.Destination) &&
                            command.ResolutionId.Equals(invocation.ResolutionId) &&
                            command.ChoiceId.Equals(invocation.ChoiceId)),
                    candidate.Destination));
            }

            return MinionChoiceViewModel.ForCorrection(
                projection.Version,
                kind,
                pending.ResolutionId,
                pending.ChoiceId,
                pending.SubjectMinionId,
                pending.ChoosingTeamId,
                pending.ChoosingSide,
                destinations);
        }

        private MinionChoiceViewModel GetOrCreateViewModel(IMatchReadModel projection)
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
            var result = session.Submit(command);
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
}
