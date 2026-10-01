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

        // 2 and 3 were 8.30n's AGREE / WITHDRAW over one shared Minion Battle selection, retired
        // at 9.29 when each seat's own picks replaced that selection. Not reused.

        /// <summary>Milestone 9.29. Adds a unit to this seat's own Minion Battle picks.</summary>
        PickMinionBattleRemoval = 4,

        /// <summary>Milestone 9.29. Takes a unit back out of this seat's own Minion Battle picks.</summary>
        UnpickMinionBattleRemoval = 5
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
            CardInstanceId preferredCardInstanceId,
            RuleObject unit)
        {
            if (!proposalId.IsDefined)
                throw new ArgumentException("An agreement command requires a proposal.", nameof(proposalId));
            ProposalId = proposalId;
            Kind = kind;
            PreferredCardInstanceId = preferredCardInstanceId;
            Unit = unit;
        }

        internal static TeamAgreementCommand Prefer(
            TeamAgreementProposalId proposalId, CardInstanceId cardInstanceId) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.PreferInitiativeCandidate, cardInstanceId, default);

        internal static TeamAgreementCommand Pick(TeamAgreementProposalId proposalId, RuleObject unit) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.PickMinionBattleRemoval, default, RequireUnit(unit));

        internal static TeamAgreementCommand Unpick(TeamAgreementProposalId proposalId, RuleObject unit) =>
            new TeamAgreementCommand(
                proposalId, TeamAgreementCommandKind.UnpickMinionBattleRemoval, default, RequireUnit(unit));

        public TeamAgreementProposalId ProposalId { get; }
        public TeamAgreementCommandKind Kind { get; }
        public CardInstanceId PreferredCardInstanceId { get; }

        /// <summary>Milestone 9.29. The unit a pick or unpick names; undefined on every other kind.</summary>
        public RuleObject Unit { get; }

        public bool Equals(TeamAgreementCommand other) =>
            other != null && ProposalId.Equals(other.ProposalId) && Kind == other.Kind &&
            PreferredCardInstanceId.Equals(other.PreferredCardInstanceId) &&
            Unit.Equals(other.Unit);
        public override bool Equals(object obj) => Equals(obj as TeamAgreementCommand);
        public override int GetHashCode() =>
            (((ProposalId.GetHashCode() * 397) ^ (int)Kind) * 397 ^
            PreferredCardInstanceId.GetHashCode()) * 397 ^ Unit.GetHashCode();

        private static RuleObject RequireUnit(RuleObject unit) =>
            unit.Kind == RuleObjectKind.Hero || unit.Kind == RuleObjectKind.Minion
                ? unit
                : throw new ArgumentException(
                    "A Minion Battle pick names a hero or a minion, the only units p.8 removes.", nameof(unit));
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
            HeroId heroId,
            bool hasStatement,
            CardInstanceId preferredCardInstanceId,
            IEnumerable<RuleObject> picks = null,
            bool picksComplete = false)
        {
            HeroId = heroId;
            HasStatement = hasStatement;
            PreferredCardInstanceId = preferredCardInstanceId;
            Picks = Array.AsReadOnly((picks ?? Enumerable.Empty<RuleObject>()).ToArray());
            PicksComplete = picksComplete;
        }

        public HeroId HeroId { get; }
        public bool HasStatement { get; }
        public CardInstanceId PreferredCardInstanceId { get; }

        /// <summary>
        /// Milestone 9.29. This hero's own Minion Battle picks, oldest first. Only the choosing side
        /// reads them: a seat on the other side receives every participant with none, because which
        /// minions a team is weighing up is that team's conversation.
        /// </summary>
        public IReadOnlyList<RuleObject> Picks { get; }

        /// <summary>
        /// Milestone 9.29. Whether this hero has picked as many units as the battle removes - read
        /// only by the choosing side, for the same reason as <see cref="Picks"/>.
        /// </summary>
        public bool PicksComplete { get; }
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
            int requiredSelectionCount,
            IEnumerable<TeamAgreementCommand> authorizedCommands,
            HeroId viewerHeroId = default)
        {
            Kind = kind;
            ProposalId = proposalId;
            ChoosingSide = choosingSide;
            Candidates = Array.AsReadOnly((candidates ??
                Enumerable.Empty<TeamAgreementCandidateProjection>()).ToArray());
            Participants = Array.AsReadOnly((participants ??
                throw new ArgumentNullException(nameof(participants))).ToArray());
            RequiredSelectionCount = requiredSelectionCount;
            AuthorizedCommands = Array.AsReadOnly((authorizedCommands ??
                Enumerable.Empty<TeamAgreementCommand>()).ToArray());
            ViewerHeroId = viewerHeroId;
        }

        public TeamAgreementKind Kind { get; }
        public TeamAgreementProposalId ProposalId { get; }
        public TeamSide ChoosingSide { get; }
        public IReadOnlyList<TeamAgreementCandidateProjection> Candidates { get; }
        public IReadOnlyList<TeamAgreementParticipantProjection> Participants { get; }

        /// <summary>How many units the Minion Battle removes; zero for a tied Initiative.</summary>
        public int RequiredSelectionCount { get; }
        public IReadOnlyList<TeamAgreementCommand> AuthorizedCommands { get; }

        /// <summary>
        /// Milestone 9.29. The hero of the seat this projection was served to, so a reader can tell its
        /// own picks from a teammate's without being handed its seat separately. Undefined where no seat
        /// is named.
        /// </summary>
        public HeroId ViewerHeroId { get; }

        /// <summary>Milestone 9.29. The viewer's own Minion Battle picks.</summary>
        public IReadOnlyList<RuleObject> ViewerPicks => PicksOf(ViewerHeroId);

        /// <summary>
        /// Milestone 9.29. One participant's own picks, or none when this projection does not name
        /// that hero or does not show the choosing side's picks to this viewer.
        /// </summary>
        public IReadOnlyList<RuleObject> PicksOf(HeroId heroId)
        {
            foreach (var participant in Participants)
            {
                if (participant.HeroId.Equals(heroId))
                {
                    return participant.Picks;
                }
            }

            return Array.Empty<RuleObject>();
        }
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
        public TeamAgreementApplicationResult(
            bool accepted,
            string detail,
            DirectorCommand finalCommand,
            IReadOnlyList<RuleObject> agreedRemovals = null)
        {
            Accepted = accepted;
            Detail = detail ?? string.Empty;
            FinalCommand = finalCommand;
            AgreedRemovals = agreedRemovals;
        }

        public bool Accepted { get; }
        public string Detail { get; }

        /// <summary>The one rules command a tied Initiative's unanimity submits.</summary>
        public DirectorCommand FinalCommand { get; }

        /// <summary>
        /// Milestone 9.29. The set every Minion Battle participant picked, in the order the host
        /// submits it: one existing Select per unit, then the existing Confirm. It is a set of units
        /// rather than a list of commands because each Select changes what the next one may be -
        /// p.8's non-Heavy-before-Heavy order is enforced by the authority one selection at a time -
        /// so the host reads every next command off the contract that stands at that moment
        /// (<see cref="TeamAgreementCoordinator.NextMinionBattleCommand"/>).
        /// </summary>
        public IReadOnlyList<RuleObject> AgreedRemovals { get; }

        public bool HasFinalAnswer => FinalCommand != null || AgreedRemovals != null;
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
        // Milestone 9.29. Each choosing-team hero's own picks, oldest first. They replace 8.30n's one
        // shared selection plus three agreements: the playtest showed three players toggling that one
        // selection under each other, with every click clearing everybody's agreement.
        private readonly Dictionary<HeroId, List<RuleObject>> minionPicks =
            new Dictionary<HeroId, List<RuleObject>>();

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
            minionPicks.Clear();
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
                return new TeamAgreementParticipantProjection(heroId, has, preference);
            });
            var commands = participantHeroes.Contains(viewer.HeroId)
                ? candidates.Select(candidate =>
                    TeamAgreementCommand.Prefer(proposalId, candidate.CardInstanceId))
                : Enumerable.Empty<TeamAgreementCommand>();
            return new TeamAgreementProjection(
                kind, proposalId, source.PendingInitiativeChoice.OwningSide,
                candidates, participants, 0, commands, viewer.HeroId);
        }

        /// <summary>
        /// Milestone 9.29. The choosing side sees every teammate's own picks and may change only its
        /// own; the other side sees who is choosing and nothing of what.
        /// </summary>
        private TeamAgreementProjection MinionBattleProjection(
            MatchSessionProjection source, MatchSeatBinding seats, MatchSeat viewer)
        {
            var choice = source.PendingMinionBattleChoice;
            var participantHeroes = ChoosingHeroes(seats, choice);
            var viewerChooses = viewer.PlaysFor(choice.ChoosingSide);
            var participants = participantHeroes.Select(heroId =>
            {
                var picks = viewerChooses ? PicksOf(heroId) : new List<RuleObject>();
                return new TeamAgreementParticipantProjection(
                    heroId, picks.Count > 0, default, picks,
                    picks.Count == choice.RequiredRemovalCount);
            });
            var commands = viewerChooses && participantHeroes.Contains(viewer.HeroId)
                ? PickCommands(choice, PicksOf(viewer.HeroId))
                : Enumerable.Empty<TeamAgreementCommand>();

            return new TeamAgreementProjection(
                kind, proposalId, choice.ChoosingSide, null, participants,
                choice.RequiredRemovalCount, commands, viewer.HeroId);
        }

        /// <summary>
        /// Every statement one picker may make: a Pick for each candidate it has not picked, where
        /// picking it is legal - on a full set, by replacing its oldest pick - and an Unpick for each
        /// it has, where taking that one back is legal.
        /// </summary>
        private IEnumerable<TeamAgreementCommand> PickCommands(
            PendingMinionBattleChoice choice, IReadOnlyList<RuleObject> picks)
        {
            var commands = new List<TeamAgreementCommand>();
            foreach (var candidate in choice.Candidates)
            {
                if (picks.Contains(candidate.Unit))
                {
                    if (MinionBattlePickRules.AfterUnpick(choice, picks, candidate.Unit) != null)
                        commands.Add(TeamAgreementCommand.Unpick(proposalId, candidate.Unit));
                }
                else if (MinionBattlePickRules.AfterPick(choice, picks, candidate.Unit) != null)
                {
                    commands.Add(TeamAgreementCommand.Pick(proposalId, candidate.Unit));
                }
            }

            return commands;
        }

        private List<RuleObject> PicksOf(HeroId heroId) =>
            minionPicks.TryGetValue(heroId, out var picks) ? picks : new List<RuleObject>();

        private static HeroId[] ChoosingHeroes(MatchSeatBinding seats, PendingMinionBattleChoice choice) =>
            seats.Seats
                .Where(seat => seat.PlaysFor(choice.ChoosingSide))
                .Select(seat => seat.HeroId)
                .ToArray();

        /// <summary>
        /// Milestone 9.29. The next rules command that carries an agreed set into the authority, read
        /// off the contract standing NOW: the Select for the first agreed unit not yet selected -
        /// non-Heavy units first, the authority's own order - and once all are selected, the Confirm.
        /// <c>null</c> when the contract offers neither, which the host reports as a refused final
        /// rules choice rather than guessing.
        /// </summary>
        public static DirectorCommand NextMinionBattleCommand(
            MatchSessionProjection source, IReadOnlyList<RuleObject> agreedRemovals)
        {
            var choice = source.PendingMinionBattleChoice;
            if (choice == null || source.DecisionContract == null)
                return null;

            // A selection the agreed set does not hold is taken back first. Nothing but this host
            // selects on a seat-served match, so this is only ever an earlier submission that stopped
            // part-way - and it must not stand under picks that do not name it.
            var stray = choice.SelectedUnits.LastOrDefault(unit => !agreedRemovals.Contains(unit));
            if (stray.IsDefined)
            {
                var invocation = choice.DeselectInvocations.FirstOrDefault(value => value.Unit.Equals(stray));
                if (invocation == null)
                    return null;

                var deselect = DirectorCommand.DeselectMinionBattleRemoval(invocation);
                return source.DecisionContract.Commands.FirstOrDefault(value => value.Equals(deselect));
            }

            var next = agreedRemovals
                .Where(unit => !choice.SelectedUnits.Contains(unit))
                .OrderBy(unit => choice.Candidates.Any(value => value.Unit.Equals(unit) && value.IsHeavy))
                .FirstOrDefault();
            if (next.IsDefined)
            {
                var invocation = choice.SelectInvocations.FirstOrDefault(value => value.Unit.Equals(next));
                if (invocation == null)
                    return null;

                var select = DirectorCommand.SelectMinionBattleRemoval(invocation);
                return source.DecisionContract.Commands.FirstOrDefault(value => value.Equals(select));
            }

            return choice.CanConfirm
                ? source.DecisionContract.Commands.FirstOrDefault(value =>
                    value.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals)
                : null;
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
                return new TeamAgreementApplicationResult(false, "Only the choosing team may pick.", null);

            var picks = PicksOf(author.HeroId);
            List<RuleObject> next;
            if (command.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval)
                next = MinionBattlePickRules.AfterPick(choice, picks, command.Unit);
            else if (command.Kind == TeamAgreementCommandKind.UnpickMinionBattleRemoval)
                next = MinionBattlePickRules.AfterUnpick(choice, picks, command.Unit);
            else
                next = null;
            if (next == null)
                return new TeamAgreementApplicationResult(false, "That pick is not offered by this proposal.", null);

            minionPicks[author.HeroId] = next;

            // Unanimity is every choosing hero holding a complete set, and all of them the same set.
            // Order is only how each player got there, so it is compared as a set.
            var participantHeroes = ChoosingHeroes(seats, choice);
            var agreed = next.Count == choice.RequiredRemovalCount &&
                participantHeroes.All(heroId =>
                {
                    var theirs = PicksOf(heroId);
                    return theirs.Count == next.Count && theirs.All(next.Contains);
                });
            return agreed
                ? new TeamAgreementApplicationResult(
                    true, string.Empty, null, Array.AsReadOnly(next.ToArray()))
                : new TeamAgreementApplicationResult(true, string.Empty, null);
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

    /// <summary>
    /// Milestone 9.29. What one Minion Battle picker may pick or take back: the authority's own
    /// order asked of that picker's set, so a seat is never offered a pick the host could not carry
    /// into the authority once every set matches.
    /// </summary>
    internal static class MinionBattlePickRules
    {
        /// <summary>
        /// The picks after adding one unit, or <c>null</c> where that is not a legal pick. On a full
        /// set the unit replaces the oldest pick whose removal leaves a set the authority would
        /// accept, which is what a second click on another minion means.
        /// </summary>
        internal static List<RuleObject> AfterPick(
            PendingMinionBattleChoice choice, IReadOnlyList<RuleObject> picks, RuleObject unit)
        {
            if (picks.Contains(unit) || !choice.Candidates.Any(value => value.Unit.Equals(unit)))
                return null;

            if (picks.Count < choice.RequiredRemovalCount)
            {
                var grown = picks.Concat(new[] { unit }).ToList();
                return IsOrderable(choice, grown) ? grown : null;
            }

            for (var replaced = 0; replaced < picks.Count; replaced++)
            {
                var swapped = picks.Where((_, index) => index != replaced)
                    .Concat(new[] { unit })
                    .ToList();
                if (IsOrderable(choice, swapped))
                    return swapped;
            }

            return null;
        }

        internal static List<RuleObject> AfterUnpick(
            PendingMinionBattleChoice choice, IReadOnlyList<RuleObject> picks, RuleObject unit)
        {
            if (!picks.Contains(unit))
                return null;

            var shrunk = picks.Where(value => !value.Equals(unit)).ToList();
            return IsOrderable(choice, shrunk) ? shrunk : null;
        }

        /// <summary>
        /// The authority's own order (<c>PendingMinionBattleChoice</c>'s "Heavy minions may be
        /// selected only after every non-Heavy candidate is selected"), asked of a picker's set
        /// instead of the shared selection: a Heavy stands in it only beside every non-Heavy
        /// candidate. This restates rulebook p.8's Heavy-last removal rather than deciding it; the
        /// authority still refuses anything else when the host submits.
        /// </summary>
        internal static bool IsOrderable(PendingMinionBattleChoice choice, ICollection<RuleObject> set)
        {
            var holdsAHeavy = choice.Candidates.Any(value => value.IsHeavy && set.Contains(value.Unit));
            return !holdsAHeavy ||
                choice.Candidates.Where(value => !value.IsHeavy).All(value => set.Contains(value.Unit));
        }
    }
}
