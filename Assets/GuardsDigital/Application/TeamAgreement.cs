using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public enum TeamAgreementKind
    {
        None = 0,
        Initiative = 1,
        MinionBattle = 2
    }

    public enum TeamAgreementCommandKind
    {
        None = 0,
        PreferInitiativeCandidate = 1,
        AgreeToMinionBattleSelection = 2,
        WithdrawMinionBattleAgreement = 3
    }

    /// <summary>
    /// The identity of one host-published proposal. It is deliberately opaque to presentation and
    /// binds a statement to the exact pending family, candidate set and (for Minion Battle) selected
    /// units that were standing when the host published it.
    /// </summary>
    public readonly struct TeamAgreementProposalId : IEquatable<TeamAgreementProposalId>
    {
        internal TeamAgreementProposalId(string value) { Value = value; }
        public string Value { get; }
        public bool IsDefined => !string.IsNullOrEmpty(Value);
        public bool Equals(TeamAgreementProposalId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is TeamAgreementProposalId other && Equals(other);
        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public sealed class TeamAgreementCommand : IEquatable<TeamAgreementCommand>
    {
        private TeamAgreementCommand(
            TeamAgreementProposalId proposalId,
            TeamAgreementCommandKind kind,
            CardInstanceId preferredCardInstanceId)
        {
            if (!proposalId.IsDefined)
                throw new ArgumentException("An agreement command requires a proposal.", nameof(proposalId));
            ProposalId = proposalId;
            Kind = kind;
            PreferredCardInstanceId = preferredCardInstanceId;
        }

        internal static TeamAgreementCommand Prefer(
            TeamAgreementProposalId proposalId, CardInstanceId cardInstanceId) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.PreferInitiativeCandidate, cardInstanceId);

        internal static TeamAgreementCommand Agree(TeamAgreementProposalId proposalId) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.AgreeToMinionBattleSelection, default);

        internal static TeamAgreementCommand Withdraw(TeamAgreementProposalId proposalId) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.WithdrawMinionBattleAgreement, default);

        public TeamAgreementProposalId ProposalId { get; }
        public TeamAgreementCommandKind Kind { get; }
        public CardInstanceId PreferredCardInstanceId { get; }

        public bool Equals(TeamAgreementCommand other) =>
            other != null && ProposalId.Equals(other.ProposalId) && Kind == other.Kind &&
            PreferredCardInstanceId.Equals(other.PreferredCardInstanceId);
        public override bool Equals(object obj) => Equals(obj as TeamAgreementCommand);
        public override int GetHashCode() =>
            ((ProposalId.GetHashCode() * 397) ^ (int)Kind) * 397 ^
            PreferredCardInstanceId.GetHashCode();
    }

    public sealed class TeamAgreementCandidateProjection
    {
        internal TeamAgreementCandidateProjection(CardInstanceId cardInstanceId, HeroId heroId)
        {
            CardInstanceId = cardInstanceId;
            HeroId = heroId;
        }

        public CardInstanceId CardInstanceId { get; }
        public HeroId HeroId { get; }
    }

    public sealed class TeamAgreementParticipantProjection
    {
        internal TeamAgreementParticipantProjection(
            HeroId heroId, bool hasStatement, CardInstanceId preferredCardInstanceId, bool agreed)
        {
            HeroId = heroId;
            HasStatement = hasStatement;
            PreferredCardInstanceId = preferredCardInstanceId;
            Agreed = agreed;
        }

        public HeroId HeroId { get; }
        public bool HasStatement { get; }
        public CardInstanceId PreferredCardInstanceId { get; }
        public bool Agreed { get; }
    }

    /// <summary>
    /// One seat's view of a public team agreement. Public statements and the proposal are identical
    /// for all six seats; only <see cref="AuthorizedCommands"/> differs.
    /// </summary>
    public sealed class TeamAgreementProjection
    {
        internal TeamAgreementProjection(
            TeamAgreementKind kind,
            TeamAgreementProposalId proposalId,
            TeamSide choosingSide,
            IEnumerable<TeamAgreementCandidateProjection> candidates,
            IEnumerable<TeamAgreementParticipantProjection> participants,
            IEnumerable<RuleObject> selectedUnits,
            int requiredSelectionCount,
            bool selectionIsConfirmable,
            IEnumerable<TeamAgreementCommand> authorizedCommands)
        {
            Kind = kind;
            ProposalId = proposalId;
            ChoosingSide = choosingSide;
            Candidates = Array.AsReadOnly((candidates ??
                Enumerable.Empty<TeamAgreementCandidateProjection>()).ToArray());
            Participants = Array.AsReadOnly((participants ??
                throw new ArgumentNullException(nameof(participants))).ToArray());
            SelectedUnits = Array.AsReadOnly((selectedUnits ?? Enumerable.Empty<RuleObject>()).ToArray());
            RequiredSelectionCount = requiredSelectionCount;
            SelectionIsConfirmable = selectionIsConfirmable;
            AuthorizedCommands = Array.AsReadOnly((authorizedCommands ??
                Enumerable.Empty<TeamAgreementCommand>()).ToArray());
        }

        public TeamAgreementKind Kind { get; }
        public TeamAgreementProposalId ProposalId { get; }
        public TeamSide ChoosingSide { get; }
        public IReadOnlyList<TeamAgreementCandidateProjection> Candidates { get; }
        public IReadOnlyList<TeamAgreementParticipantProjection> Participants { get; }
        public IReadOnlyList<RuleObject> SelectedUnits { get; }
        public int RequiredSelectionCount { get; }
        public bool SelectionIsConfirmable { get; }
        public IReadOnlyList<TeamAgreementCommand> AuthorizedCommands { get; }
    }

    public sealed class TeamAgreementSubmission
    {
        public TeamAgreementSubmission(
            SubmissionId id, long expectedVersion, TeamAgreementCommand command)
        {
            if (!id.IsDefined) throw new ArgumentException("A submission requires an identity.", nameof(id));
            if (expectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(expectedVersion));
            Id = id;
            ExpectedVersion = expectedVersion;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public SubmissionId Id { get; }
        public long ExpectedVersion { get; }
        public TeamAgreementCommand Command { get; }
    }

    public enum TeamAgreementSubmissionRejectionReason
    {
        None = 0,
        UnknownSeat = 1,
        StaleProjection = 2,
        DuplicateSubmission = 3,
        StaleProposal = 4,
        IneligibleSeat = 5,
        UnauthorizedStatement = 6,
        HostRefused = 7,
        FinalRulesChoiceRefused = 8,

        /// <summary>
        /// Milestone 8.34. A seat of this match is away and the session is holding still for it,
        /// so no statement is recorded and no proposal moves. The agreement half of
        /// <see cref="SeatSubmissionRejectionReason.SessionPaused"/>.
        /// </summary>
        SessionPaused = 9
    }

    public sealed class TeamAgreementSubmissionResult
    {
        internal TeamAgreementSubmissionResult(
            bool accepted,
            TeamAgreementSubmissionRejectionReason rejectionReason,
            string detail,
            long version,
            long sequence,
            SeatProjection projection)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Detail = detail ?? string.Empty;
            Version = version;
            Sequence = sequence;
            Projection = projection;
        }

        public bool Accepted { get; }
        public TeamAgreementSubmissionRejectionReason RejectionReason { get; }
        public string Detail { get; }
        public long Version { get; }
        public long Sequence { get; }
        public SeatProjection Projection { get; }
    }

    internal sealed class TeamAgreementApplicationResult
    {
        public TeamAgreementApplicationResult(bool accepted, string detail, DirectorCommand finalCommand)
        {
            Accepted = accepted;
            Detail = detail ?? string.Empty;
            FinalCommand = finalCommand;
        }

        public bool Accepted { get; }
        public string Detail { get; }
        public DirectorCommand FinalCommand { get; }
    }

    /// <summary>
    /// Host-local, transport-neutral public agreement state. It consumes immutable pending choices
    /// and emits the existing rules command only at unanimity; it is neither snapshot nor replay state.
    /// </summary>
    internal sealed class TeamAgreementCoordinator
    {
        private TeamAgreementProposalId proposalId;
        private TeamAgreementKind kind;
        private readonly Dictionary<HeroId, CardInstanceId> initiativePreferences =
            new Dictionary<HeroId, CardInstanceId>();
        private readonly HashSet<HeroId> minionAgreements = new HashSet<HeroId>();

        public TeamAgreementProjection ProjectionFor(
            MatchSessionProjection source, MatchSeatBinding seats, MatchSeat viewer)
        {
            Refresh(source);
            if (!proposalId.IsDefined)
                return null;

            if (kind == TeamAgreementKind.Initiative)
                return InitiativeProjection(source, viewer);
            return MinionBattleProjection(source, seats, viewer);
        }

        public TeamAgreementApplicationResult Apply(
            MatchSessionProjection source,
            MatchSeatBinding seats,
            MatchSeat author,
            TeamAgreementCommand command)
        {
            Refresh(source);
            if (!proposalId.IsDefined || !proposalId.Equals(command.ProposalId))
                return new TeamAgreementApplicationResult(false, "The agreement proposal has changed.", null);

            if (kind == TeamAgreementKind.Initiative)
                return ApplyInitiative(source, author, command);
            return ApplyMinionBattle(source, seats, author, command);
        }

        public void Clear()
        {
            proposalId = default;
            kind = TeamAgreementKind.None;
            initiativePreferences.Clear();
            minionAgreements.Clear();
        }

        private void Refresh(MatchSessionProjection source)
        {
            var current = BuildProposalId(source, out var currentKind);
            if (!current.Equals(proposalId))
            {
                Clear();
                proposalId = current;
                kind = currentKind;
            }
        }

        private TeamAgreementProjection InitiativeProjection(
            MatchSessionProjection source, MatchSeat viewer)
        {
            var candidates = InitiativeCandidates(source).ToArray();
            var participantHeroes = candidates.Select(value => value.HeroId).Distinct().ToArray();
            var participants = participantHeroes.Select(heroId =>
            {
                var has = initiativePreferences.TryGetValue(heroId, out var preference);
                return new TeamAgreementParticipantProjection(heroId, has, preference, false);
            });
            var commands = participantHeroes.Contains(viewer.HeroId)
                ? candidates.Select(candidate =>
                    TeamAgreementCommand.Prefer(proposalId, candidate.CardInstanceId))
                : Enumerable.Empty<TeamAgreementCommand>();
            return new TeamAgreementProjection(
                kind, proposalId, source.PendingInitiativeChoice.OwningSide,
                candidates, participants, null, 0, false, commands);
        }

        private TeamAgreementProjection MinionBattleProjection(
            MatchSessionProjection source, MatchSeatBinding seats, MatchSeat viewer)
        {
            var choice = source.PendingMinionBattleChoice;
            var participantHeroes = seats.Seats
                .Where(seat => seat.PlaysFor(choice.ChoosingSide))
                .Select(seat => seat.HeroId)
                .ToArray();
            var participants = participantHeroes.Select(heroId =>
                new TeamAgreementParticipantProjection(
                    heroId, minionAgreements.Contains(heroId), default,
                    minionAgreements.Contains(heroId)));
            IEnumerable<TeamAgreementCommand> commands = Enumerable.Empty<TeamAgreementCommand>();
            if (viewer.PlaysFor(choice.ChoosingSide) && choice.CanConfirm)
            {
                commands = minionAgreements.Contains(viewer.HeroId)
                    ? new[] { TeamAgreementCommand.Withdraw(proposalId) }
                    : new[] { TeamAgreementCommand.Agree(proposalId) };
            }

            return new TeamAgreementProjection(
                kind, proposalId, choice.ChoosingSide, null, participants,
                choice.SelectedUnits, choice.RequiredRemovalCount, choice.CanConfirm, commands);
        }

        private TeamAgreementApplicationResult ApplyInitiative(
            MatchSessionProjection source, MatchSeat author, TeamAgreementCommand command)
        {
            var candidates = InitiativeCandidates(source).ToArray();
            if (!candidates.Any(candidate => candidate.HeroId.Equals(author.HeroId)))
                return new TeamAgreementApplicationResult(false, "Only tied candidate owners may state a preference.", null);
            if (command.Kind != TeamAgreementCommandKind.PreferInitiativeCandidate ||
                !candidates.Any(candidate =>
                    candidate.CardInstanceId.Equals(command.PreferredCardInstanceId)))
                return new TeamAgreementApplicationResult(false, "That preference is not offered by this proposal.", null);

            initiativePreferences[author.HeroId] = command.PreferredCardInstanceId;
            var participantHeroes = candidates.Select(value => value.HeroId).Distinct().ToArray();
            if (participantHeroes.All(heroId => initiativePreferences.ContainsKey(heroId)) &&
                participantHeroes.Select(heroId => initiativePreferences[heroId]).Distinct().Count() == 1)
            {
                var selected = initiativePreferences[participantHeroes[0]];
                var final = source.DecisionContract.Commands.Single(value =>
                    value.Kind == DirectorCommandKind.ChooseTiedActor &&
                    value.CardInstanceId.Equals(selected));
                return new TeamAgreementApplicationResult(true, string.Empty, final);
            }

            return new TeamAgreementApplicationResult(true, string.Empty, null);
        }

        private TeamAgreementApplicationResult ApplyMinionBattle(
            MatchSessionProjection source,
            MatchSeatBinding seats,
            MatchSeat author,
            TeamAgreementCommand command)
        {
            var choice = source.PendingMinionBattleChoice;
            if (!author.PlaysFor(choice.ChoosingSide))
                return new TeamAgreementApplicationResult(false, "Only the choosing team may agree.", null);
            if (!choice.CanConfirm)
                return new TeamAgreementApplicationResult(false, "The current selection is not confirmable.", null);

            if (command.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection)
                minionAgreements.Add(author.HeroId);
            else if (command.Kind == TeamAgreementCommandKind.WithdrawMinionBattleAgreement)
                minionAgreements.Remove(author.HeroId);
            else
                return new TeamAgreementApplicationResult(false, "That statement is not offered by this proposal.", null);

            var participantHeroes = seats.Seats
                .Where(seat => seat.PlaysFor(choice.ChoosingSide))
                .Select(seat => seat.HeroId)
                .ToArray();
            if (participantHeroes.All(heroId => minionAgreements.Contains(heroId)))
            {
                var final = source.DecisionContract.Commands.Single(value =>
                    value.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals);
                return new TeamAgreementApplicationResult(true, string.Empty, final);
            }

            return new TeamAgreementApplicationResult(true, string.Empty, null);
        }

        private static IEnumerable<TeamAgreementCandidateProjection> InitiativeCandidates(
            MatchSessionProjection source) =>
            source.PendingInitiativeChoice.CandidateCardInstanceIds.Select(cardId =>
            {
                var card = source.Cards.Single(value => value.Id.Equals(cardId));
                return new TeamAgreementCandidateProjection(cardId, card.OwnerId);
            });

        private static TeamAgreementProposalId BuildProposalId(
            MatchSessionProjection source, out TeamAgreementKind proposalKind)
        {
            if (source.PendingInitiativeChoice != null)
            {
                proposalKind = TeamAgreementKind.Initiative;
                return new TeamAgreementProposalId(
                    "initiative|" + source.Version + "|" + source.PendingInitiativeChoice.OwningSide +
                    "|" + string.Join(",", source.PendingInitiativeChoice.CandidateCardInstanceIds
                        .Select(value => value.Value)));
            }

            if (source.PendingMinionBattleChoice != null)
            {
                var choice = source.PendingMinionBattleChoice;
                proposalKind = TeamAgreementKind.MinionBattle;
                return new TeamAgreementProposalId(
                    "minion-battle|" + source.Version + "|" + choice.ResolutionId.Value + "|" +
                    choice.ChoiceId.Value + "|" + string.Join(",", choice.SelectedUnits.Select(value =>
                        value.Kind + ":" +
                        (value.Kind == RuleObjectKind.Hero
                            ? value.HeroId.Value
                            : value.MinionId.Value))));
            }

            proposalKind = TeamAgreementKind.None;
            return default;
        }
    }
}
