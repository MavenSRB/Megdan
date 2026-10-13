using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>
    /// Why a mid-turn Lane Push would be owed. Rulebook p.8 names exactly two causes and rules the
    /// rest out in the same breath: "If you defeat, or remove, the last remaining enemy minion in the
    /// Battle Zone, the Lane is immediately Pushed ... Note: A Lane Push can never be triggered by
    /// moving, pushing, or swapping a minion. It must be either defeated, or removed."
    ///
    /// The enum has both members since milestone 6.6b. Milestone 6.6 declared only the first, because
    /// p.13's Defeat reached its removal through a stage that hard-coded the removed result and asked
    /// p.8 nothing, so a Defeat cause would have been a member with no producer. Routing that stage
    /// through the same shared decision gave it one.
    /// </summary>
    public enum LanePushCauseKind
    {
        None = 0,

        /// <summary>A standalone Remove took the last unit counting as a minion off a side (p.19, p.8).</summary>
        Removal = 1,

        /// <summary>
        /// p.13's Defeat took the last unit counting as a minion off a side (p.8). It is a separate
        /// member rather than a shared "something left the board", because p.8 names the two causes
        /// separately - "It must be either defeated, or removed" - and a later milestone reading the
        /// recorded cause is entitled to know which sentence it is answering.
        /// </summary>
        Defeat = 2
    }

    /// <summary>
    /// The recorded cause of a mid-turn Lane Push, or the named absence.
    ///
    /// It records a cause and never a request: p.8's push is a system transition with a winner, a
    /// losing side, a Wave counter, and a whole board rearrangement, and milestone 6.9 owns emitting
    /// and resolving it. What this milestone owes 6.9 is the one fact only the removing operation can
    /// know - that this particular removal, rather than the board simply being empty, is what p.8
    /// qualifies.
    /// </summary>
    public readonly struct LanePushQualification : IEquatable<LanePushQualification>
    {
        private LanePushQualification(LanePushCauseKind cause, TeamSide losingSide, ZoneId battleZoneId)
        {
            Cause = cause;
            LosingSide = losingSide;
            BattleZoneId = battleZoneId;
        }

        /// <summary>The absence of a cause: this operation qualifies nothing.</summary>
        public static LanePushQualification None { get; } =
            new LanePushQualification(LanePushCauseKind.None, TeamSide.None, default);

        public LanePushCauseKind Cause { get; }

        /// <summary>The side left with no minion in the Battle Zone.</summary>
        public TeamSide LosingSide { get; }

        public ZoneId BattleZoneId { get; }

        public bool Qualifies => Cause != LanePushCauseKind.None;

        /// <summary>
        /// The recorded cause, named by the operation that took the piece off the board. The cause is
        /// supplied rather than inferred: nothing about the emptied zone distinguishes p.8's two
        /// sentences from each other, and the only thing that knows which one applies is the operation
        /// that ran.
        /// </summary>
        public static LanePushQualification For(
            LanePushCauseKind cause, TeamSide losingSide, ZoneId battleZoneId)
        {
            if (cause != LanePushCauseKind.Removal && cause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cause), cause, "p.8 recognizes a removal or a Defeat and nothing else.");
            }

            if (losingSide != TeamSide.Red && losingSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(losingSide), losingSide, "A Lane-Push cause names the side that lost its minions.");
            }

            if (string.IsNullOrWhiteSpace(battleZoneId.Value))
            {
                throw new ArgumentException(
                    "A Lane-Push cause names the Battle Zone it emptied.", nameof(battleZoneId));
            }

            return new LanePushQualification(cause, losingSide, battleZoneId);
        }

        public bool Equals(LanePushQualification other) =>
            Cause == other.Cause && LosingSide == other.LosingSide &&
            BattleZoneId.Equals(other.BattleZoneId);

        public override bool Equals(object obj) => obj is LanePushQualification other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Cause;
                hash = (hash * 397) ^ (int)LosingSide;
                return (hash * 397) ^ BattleZoneId.GetHashCode();
            }
        }

        public override string ToString() =>
            Qualifies ? $"{Cause} emptied {LosingSide} in {BattleZoneId}" : "No Lane-Push cause";
    }

    /// <summary>
    /// Whether a completed removal is p.8's last-minion Lane-Push cause.
    ///
    /// One question, asked in one place, by the one operation the rulebook allows to ask it. That is
    /// the whole of this milestone's fourth acceptance check: direct Move, Object Push, and Swap do not
    /// call this and have no member to hold its answer, so "moving, pushing, or swapping the last
    /// Battle-Zone minion never qualifies" is a structural property rather than three tested cases, and
    /// a conformance rule holds it that way.
    ///
    /// The set it counts is <see cref="BattleZoneQuery.FindEffectiveMinionsInside"/>, the same one the
    /// Minion Battle counts. p.8 says the mid-turn push happens "in the same way as it would during a
    /// Minion Battle", so counting a different set here would make one rulebook phrase mean two things
    /// in adjacent rules - the defect milestone 6.5ad corrected for optional distances.
    ///
    /// It is asked <em>after</em> the removal has committed, because the question p.8 poses is about
    /// what remains, not about what left. Since milestone 6.9 the operation's own subject is excluded
    /// from what remains, which changes nothing for a piece that actually left and is what lets a
    /// Defeat whose removal was prevented answer p.8's first sentence.
    /// </summary>
    public static class LanePushQualificationQuery
    {
        public static LanePushQualification AfterRemoval(
            LanePushCauseKind cause,
            RuleObject removedUnit,
            TeamSide removedSide,
            HexCoordinate removedFrom,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            // A token is not a unit (p.19), so removing one empties nothing p.8 counts, and the named
            // absence is the honest answer rather than a side with no minions.
            if (!removedUnit.IsDefined ||
                (removedSide != TeamSide.Red && removedSide != TeamSide.Blue))
            {
                return LanePushQualification.None;
            }

            // Milestone 7.9c, finding F-BOARD-02. p.8's subject is "the last remaining enemy minion",
            // so what was taken has to have been one. Every minion miniature answers yes intrinsically,
            // which is why this changes nothing for the path that has always worked; a hero answers yes
            // only while p.17's replacement makes it count as a minion, and no otherwise. Without this
            // the query read an emptied side as a qualifying one whoever had just left it, so an
            // ordinary hero's Defeat in a Battle Zone its side had already emptied would have spent a
            // Wave and advanced the Zone - the exact reading `HeroDefeatResolution.LanePush` says it
            // routes here to avoid, and the reason milestone 7.9c corrects this before giving that
            // record its consumer.
            if (!EffectiveUnitKindQuery.Is(removedUnit, EffectiveUnitKind.Minion, modifications))
            {
                return LanePushQualification.None;
            }

            if (!BattleZoneQuery.TryGetCurrentBattleZone(board, removedFrom, out var battleZone))
            {
                // A board with no current Battle Zone - every synthetic one - has no lane to push,
                // which is the same reading the displaced-minion correction takes.
                return LanePushQualification.None;
            }

            // p.8 asks about "the last remaining enemy minion in the Battle Zone", so a removal that
            // happened outside it qualifies nothing even when that side has none inside: the zone is
            // no emptier than it was before the piece was taken.
            // The operation's own subject never counts towards what remains. For a completed removal
            // that is a no-op - the piece has already left the board - and for p.13's Defeat whose
            // removal was prevented it is the whole of milestone 6.9's third acceptance check: p.8
            // names two independent triggers, "It must be either defeated, or removed", and p.13's own
            // Bolster example makes a Defeat whose removal is prevented a complete Defeat that still
            // collects its coins. So the Defeat sentence is answered by the Defeat, and the miniature
            // still standing there is the one this rule just accounted for.
            var remaining = BattleZoneQuery
                .FindEffectiveMinionsInside(board, battleZone.Id, participants, modifications)
                .Any(minion => minion.TeamSide == removedSide && !minion.Unit.Equals(removedUnit));
            return remaining
                ? LanePushQualification.None
                : LanePushQualification.For(cause, removedSide, battleZone.Id);
        }
    }
}
