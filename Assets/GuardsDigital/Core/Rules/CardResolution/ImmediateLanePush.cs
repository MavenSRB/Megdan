using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Rulebook p.8's mid-turn Lane Push, requested and not yet resolved.
    ///
    /// "A Lane can be Pushed both at the end of round, and in the middle of a turn. If you defeat, or
    /// remove, the last remaining enemy minion in the Battle Zone, the Lane is immediately Pushed in the
    /// same way as it would during a Minion Battle ... If the Lane is pushed in the middle of a turn,
    /// the turn continues in the Initiative order as normal."
    ///
    /// It is a sibling of the end-of-round <see cref="PendingLanePushRequest"/> rather than a reuse of
    /// it, in the shape milestone 6.8h used for marker identity. The two agree on everything the
    /// transition needs - the zone, the winner, the loser - and disagree on the only two things that
    /// decide when they are consumed: this one names the turn it interrupted and which of p.8's two
    /// sentences produced it, while the end-of-round request names a Minion Battle resolution and
    /// consumes at an <see cref="EndOfRoundStage"/> that does not exist mid-turn.
    ///
    /// Milestone 6.9 emits it and stops there. Resolving it - the Wave counter, the Battle-Zone
    /// advance, the removal and respawn - belongs to milestones 6.10 and 6.11, and this milestone's own
    /// acceptance text forbids exposing a partial transition, so a match holding one of these is stopped
    /// rather than partly pushed.
    ///
    /// It carries no round or turn cursor, unlike its end-of-round sibling. One exists only while the
    /// turn that produced it is stopped, so the authority holding it already knows which turn that is,
    /// and a second copy of that cursor could only ever disagree with the first.
    /// </summary>
    public sealed class PendingImmediateLanePushRequest
    {
        public PendingImmediateLanePushRequest(
            LanePushRequestId requestId,
            LanePushCauseKind cause,
            ZoneId battleZoneId,
            TeamId winningTeamId,
            TeamSide winningSide,
            TeamId losingTeamId,
            TeamSide losingSide,
            ActionResolutionId causeResolutionId)
        {
            if (!requestId.IsDefined || string.IsNullOrWhiteSpace(battleZoneId.Value) ||
                !winningTeamId.IsDefined || !losingTeamId.IsDefined || !causeResolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "A mid-turn Lane-Push request requires stable typed identities.");
            }

            if (cause != LanePushCauseKind.Removal && cause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cause), cause,
                    "p.8 recognizes a Defeat or a removal and nothing else as a mid-turn cause.");
            }

            if (winningTeamId.Equals(losingTeamId) || winningSide == losingSide ||
                (winningSide != TeamSide.Red && winningSide != TeamSide.Blue) ||
                (losingSide != TeamSide.Red && losingSide != TeamSide.Blue))
            {
                throw new ArgumentException(
                    "A mid-turn Lane-Push request requires two distinct valid teams.");
            }

            RequestId = requestId;
            Cause = cause;
            BattleZoneId = battleZoneId;
            WinningTeamId = winningTeamId;
            WinningSide = winningSide;
            LosingTeamId = losingTeamId;
            LosingSide = losingSide;
            CauseResolutionId = causeResolutionId;
        }

        public LanePushRequestId RequestId { get; }

        /// <summary>Which of p.8's two sentences produced it. Supplied by the operation, never derived.</summary>
        public LanePushCauseKind Cause { get; }

        public ZoneId BattleZoneId { get; }

        public TeamId WinningTeamId { get; }

        public TeamSide WinningSide { get; }

        public TeamId LosingTeamId { get; }

        public TeamSide LosingSide { get; }

        /// <summary>The action instance whose operation recorded the cause.</summary>
        public ActionResolutionId CauseResolutionId { get; }
    }

    /// <summary>
    /// Turns a recorded p.8 cause into the mid-turn request, or reports that the rule's own conditions
    /// are not met.
    ///
    /// The division of labour is the one milestone 6.6 established and this milestone keeps. The
    /// <em>cause</em> is supplied by the operation that ran and is the only thing that can be: nothing
    /// about an emptied zone distinguishes p.8's two sentences from each other, and a moved, pushed or
    /// swapped minion leaves an identical board while triggering nothing. The <em>winner and loser</em>
    /// are read from the board, because that is how p.8 states them - "The team with one or more minions
    /// still remaining in the Battle Zone wins the push" - so deriving them anywhere else would invent a
    /// second answer to a question the rulebook answers by looking.
    ///
    /// It counts <see cref="BattleZoneQuery.FindEffectiveMinionsInside"/>, the set the Minion Battle and
    /// the recorded cause already count, so p.8's "in the same way as it would during a Minion Battle"
    /// stays one reading rather than three.
    ///
    /// Exactly one caller is permitted, and a conformance rule holds it to that, in the shape milestone
    /// 6.6 used for the cause itself. That is what keeps the excluded displacements excluded: they
    /// record no cause, so they cannot reach this, and this is the only thing that can produce a
    /// request.
    /// </summary>
    public static class ImmediateLanePushQuery
    {
        /// <summary>
        /// The request a recorded cause owes on this board, or <c>null</c>.
        ///
        /// Two boards produce nothing. One where the map flags no single Battle Zone - every synthetic
        /// board - has no lane to push, which is the reading the recorded cause and the displaced-minion
        /// correction already take. And one where p.8's winner sentence has no subject: if neither side
        /// holds a minion in the zone, no team has "one or more minions still remaining", and reporting
        /// nothing is the honest answer rather than awarding the push to nobody.
        /// </summary>
        public static PendingImmediateLanePushRequest From(
            LanePushCauseKind cause,
            ActionResolutionId causeResolutionId,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (cause != LanePushCauseKind.Removal && cause != LanePushCauseKind.Defeat)
            {
                return null;
            }

            if (!BattleZoneQuery.TryGetCurrentBattleZone(board, out var battleZone))
            {
                return null;
            }

            return From(
                cause, battleZone.Id, null,
                causeResolutionId, board, participants, modifications);
        }

        public static PendingImmediateLanePushRequest From(
            LanePushQualification qualification,
            ActionResolutionId causeResolutionId,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            From(
                qualification.Cause, qualification.BattleZoneId, qualification.LosingSide,
                causeResolutionId, board, participants, modifications);

        private static PendingImmediateLanePushRequest From(
            LanePushCauseKind cause,
            ZoneId battleZoneId,
            TeamSide? recordedLosingSide,
            ActionResolutionId causeResolutionId,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if ((cause != LanePushCauseKind.Removal && cause != LanePushCauseKind.Defeat) ||
                !board.CurrentBattleZoneIds.Contains(battleZoneId))
                return null;

            var remaining = BattleZoneQuery.FindEffectiveMinionsInside(
                board, battleZoneId, participants, modifications);
            var redHolds = remaining.Any(minion => minion.TeamSide == TeamSide.Red);
            var blueHolds = remaining.Any(minion => minion.TeamSide == TeamSide.Blue);
            if (redHolds == blueHolds)
            {
                return null;
            }

            var winningTeam = FindTeam(participants, redHolds ? TeamSide.Red : TeamSide.Blue);
            var losingTeam = FindTeam(participants, redHolds ? TeamSide.Blue : TeamSide.Red);
            if (winningTeam == null || losingTeam == null)
            {
                return null;
            }

            if (recordedLosingSide.HasValue && losingTeam.Side != recordedLosingSide.Value)
                return null;

            var requestSuffix = board.CurrentBattleZoneIds.Count > 1
                ? $"-{battleZoneId.Value}"
                : string.Empty;
            return new PendingImmediateLanePushRequest(
                new LanePushRequestId($"{causeResolutionId.Value}-lane-push{requestSuffix}"),
                cause,
                battleZoneId,
                winningTeam.Id,
                winningTeam.Side,
                losingTeam.Id,
                losingTeam.Side,
                causeResolutionId);
        }

        private static Team FindTeam(MatchParticipants participants, TeamSide side) =>
            participants.Teams.FirstOrDefault(team => team.Side == side);
    }
}
