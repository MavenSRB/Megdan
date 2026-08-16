using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>Why a Remove was refused before it reached its removal stage.</summary>
    public enum RemoveRejectionReason
    {
        None = 0,

        /// <summary>The subject is not the kind of piece this Remove declared it may take.</summary>
        IneligiblePiece = 1,

        /// <summary>The named piece is not on the board.</summary>
        SubjectNotOnBoard = 2,

        /// <summary>The board refused the removal between derivation and commit.</summary>
        RemovalUnavailable = 3
    }

    /// <summary>
    /// What happened at the removal stage. <c>CARD_EFFECT_CAPABILITIES.md</c> requires a Remove to
    /// "record removed or removal-prevented outcome", and the two are different rules facts: the piece
    /// left the board, or it was still there afterwards because something stopped the removal.
    /// </summary>
    public enum RemoveResultKind
    {
        None = 0,

        /// <summary>The piece left the board and went to its declared destination.</summary>
        Removed = 1,

        /// <summary>
        /// The removal was attempted and did not happen. Milestone 6.6 reaches this through the one
        /// interception the engine can already express: p.18's immunity, asked immediately before the
        /// mutation as cross-cutting rule 6 requires of every unit-affecting capability. A target that
        /// was a legal candidate when the choice opened and immune when the answer arrived - a friendly
        /// non-Heavy minion arriving beside a Heavy one (p.13), or an Active effect coming into effect -
        /// is not removed, and nothing about the board changed.
        ///
        /// The general <c>PreventRemovalEffect</c> source family is a different producer of this same
        /// outcome and stays unimplemented: p.13's own example of it is card text ("Bolster"), no
        /// verified text is available, and the declared modification vocabulary reaches heroes alone
        /// while <c>TokenPropertyDefinition</c> carries a recorded decision against widening its
        /// affected object ahead of that text.
        /// </summary>
        RemovalPrevented = 2
    }

    /// <summary>
    /// The typed outcome of one Remove: the piece, the space it left, the declared destination, what
    /// the removal stage decided, and whether p.8 is owed a Lane Push because of it.
    ///
    /// It carries no reward, no Defeat outcome identity, and no reward eligibility, because p.19 states
    /// the rule outright - "Do not collect any coins" - and p.13's Defeat is the staged outcome that
    /// does award them. A caller that wants both asks for both; nothing here can be mistaken for a
    /// Defeat that skipped its rewards.
    /// </summary>
    public readonly struct RemoveOutcome
    {
        private RemoveOutcome(
            bool accepted,
            RemoveRejectionReason rejectionReason,
            RemoveResultKind resultKind,
            RemoveDestination destination,
            BoardObjectReference subject,
            HexCoordinate origin,
            LanePushQualification lanePush)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            ResultKind = resultKind;
            Destination = destination;
            Subject = subject;
            Origin = origin;
            LanePush = lanePush;
        }

        /// <summary>
        /// Whether the Remove ran its removal stage at all. A prevented removal is accepted: the
        /// operation did everything the rules asked and the rules answered that the piece stays.
        /// </summary>
        public bool Accepted { get; }

        public RemoveRejectionReason RejectionReason { get; }
        public RemoveResultKind ResultKind { get; }

        /// <summary>Where the piece went, as this Remove declared it (p.19's recorded destination).</summary>
        public RemoveDestination Destination { get; }

        public BoardObjectReference Subject { get; }

        /// <summary>The space the piece stood in; it still stands there when the removal was prevented.</summary>
        public HexCoordinate Origin { get; }

        /// <summary>
        /// p.8's recorded cause, or its named absence. A prevented removal always carries the absence,
        /// because the rule that qualifies is "remove the last remaining enemy minion" and nothing was
        /// removed.
        /// </summary>
        public LanePushQualification LanePush { get; }

        internal static RemoveOutcome Removed(
            RemoveDestination destination,
            BoardObjectReference subject,
            HexCoordinate origin,
            LanePushQualification lanePush) =>
            new RemoveOutcome(
                true, RemoveRejectionReason.None, RemoveResultKind.Removed, destination,
                subject, origin, lanePush);

        internal static RemoveOutcome Prevented(
            RemoveDestination destination, BoardObjectReference subject, HexCoordinate origin) =>
            new RemoveOutcome(
                true, RemoveRejectionReason.None, RemoveResultKind.RemovalPrevented, destination,
                subject, origin, LanePushQualification.None);

        internal static RemoveOutcome Reject(
            RemoveDestination destination, RemoveRejectionReason reason)
        {
            if (reason == RemoveRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Remove requires a reason.");
            }

            return new RemoveOutcome(
                false, reason, RemoveResultKind.None, destination, default, default,
                LanePushQualification.None);
        }
    }

    /// <summary>
    /// Rulebook p.19's Remove, as one operation every caller shares.
    ///
    /// "Remove a game piece (such as a miniature, or a token) from the board. Do not collect any
    /// coins."
    ///
    /// It runs in three named stages, which is what keeps it distinct from the two operations it is
    /// most easily confused with. The declared piece is resolved and checked for eligibility; the
    /// removal is attempted, and the immunity decision p.18 requires immediately before a mutation can
    /// stop it there; and only a removal that actually happened asks p.8 whether it emptied a side out
    /// of the Battle Zone. p.13's Clear is a caller of this stage rather than a mode of it - it is a
    /// replacement for an entire Attack action that removes a chosen subset of adjacent tokens - and
    /// p.13's Defeat is a different staged outcome that awards coins and opens reward eligibility
    /// before its own removal stage.
    /// </summary>
    public static class RemoveOperation
    {
        public static RemoveOutcome Remove(
            RemovePolicy policy,
            IBoardObjectExchangeAuthority exchange,
            IBoardDisplacementAuthority board,
            BoardObjectReference subject,
            RemovalStageDecision removalStage)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (removalStage == null) throw new ArgumentNullException(nameof(removalStage));

            if (!policy.Admits(subject.Kind))
            {
                return RemoveOutcome.Reject(policy.Destination, RemoveRejectionReason.IneligiblePiece);
            }

            if (!board.TryGetObjectCoordinate(subject, out var origin))
            {
                return RemoveOutcome.Reject(policy.Destination, RemoveRejectionReason.SubjectNotOnBoard);
            }

            if (removalStage.IsPrevented(subject))
            {
                return RemoveOutcome.Prevented(policy.Destination, subject, origin);
            }

            var committed = exchange.CommitValidatedRemoval(subject);
            if (!committed.Accepted)
            {
                return RemoveOutcome.Reject(policy.Destination, RemoveRejectionReason.RemovalUnavailable);
            }

            return RemoveOutcome.Removed(
                policy.Destination, subject, origin, removalStage.QualifyLanePush(subject, origin));
        }
    }

    /// <summary>
    /// The two questions the removal stage asks that only the calling capability can answer: whether
    /// something stops this removal, and - once it has happened - whether p.8 is owed a Lane Push.
    ///
    /// Both need the activity, the acting unit, the participants, and the live modification set.
    /// Threading four more parameters through <see cref="RemoveOperation"/> would widen an operation
    /// that decides nothing about any of them, which is the mistake milestone 6.4a recorded when it
    /// tried to make a card-state query reach the board. The capability binds them once here instead.
    ///
    /// Since milestone 6.6b it is shared rather than the standalone Remove's own: p.13's Defeat asks it
    /// the same first question before taking its target off the board, so a prevention source added
    /// later is honoured by both paths or by neither. Neither path may ask around it - the Lane-Push
    /// conformance rule already permits exactly one asker of p.8's question - which is what makes the
    /// sharing structural rather than a convention two call sites happen to follow.
    /// </summary>
    public sealed class RemovalStageDecision
    {
        private readonly RuleActivity activity;
        private readonly RuleObject actingUnit;
        private readonly IBoardDisplacementAuthority board;
        private readonly IUnitSpawnAuthority units;
        private readonly MatchParticipants participants;
        private readonly RuleModificationSet modifications;
        private readonly LanePushCauseKind lanePushCause;

        /// <param name="lanePushCause">
        /// Which of p.8's two sentences this operation answers when its removal empties a side out of
        /// the Battle Zone. It is declared by the caller rather than derived from
        /// <paramref name="activity"/>, because the activity says who is acting and the cause says what
        /// was done - a Skill can Remove and an Attack can Defeat, and neither reading follows from the
        /// other.
        /// </param>
        public RemovalStageDecision(
            RuleActivity activity,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            IUnitSpawnAuthority units,
            MatchParticipants participants,
            RuleModificationSet modifications,
            LanePushCauseKind lanePushCause)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException(
                    "A removal stage is performed by a named activity (p.18).", nameof(activity));
            }

            if (lanePushCause != LanePushCauseKind.Removal && lanePushCause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lanePushCause), lanePushCause,
                    "A removal stage declares which of p.8's two causes it would record.");
            }

            this.lanePushCause = lanePushCause;
            this.activity = activity;
            this.actingUnit = actingUnit;
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.units = units ?? throw new ArgumentNullException(nameof(units));
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            this.modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
        }

        /// <summary>
        /// Whether the removal is stopped where it stands. p.18's immunity is asked here rather than
        /// only when candidates were derived, because cross-cutting rule 6 requires both readings and
        /// the state can change between a paused choice and its answer.
        /// </summary>
        public bool IsPrevented(BoardObjectReference subject)
        {
            var unit = subject.AsRuleObject();
            return unit.IsDefined && ImmunityQuery.IsImmuneTo(
                unit, activity, actingUnit, board, participants, modifications);
        }

        /// <summary>p.8's cause, asked after the piece has actually left the board.</summary>
        public LanePushQualification QualifyLanePush(BoardObjectReference subject, HexCoordinate origin) =>
            LanePushQualificationQuery.AfterRemoval(
                lanePushCause, subject.AsRuleObject(), SideOf(subject), origin, board, participants,
                modifications);

        /// <summary>
        /// The side the removed piece belonged to, or <see cref="TeamSide.None"/> for a token, which
        /// p.19 leaves outside the unit rules and therefore outside p.8's count.
        /// </summary>
        private TeamSide SideOf(BoardObjectReference subject)
        {
            switch (subject.Kind)
            {
                case BoardObjectKind.Hero:
                    return participants.GetTeamForHero(subject.HeroId).Side;
                case BoardObjectKind.Minion:
                    return SideOfRemovedMinion(subject);
                default:
                    return TeamSide.None;
            }
        }

        /// <summary>
        /// A removed miniature is no longer among the board's minions, so its side is read from the
        /// reserve it just joined rather than from an occupancy that no longer exists.
        /// </summary>
        private TeamSide SideOfRemovedMinion(BoardObjectReference subject)
        {
            foreach (var reserved in units.FindUnitSupply())
            {
                if (reserved.MinionId.Equals(subject.MinionId))
                {
                    return reserved.TeamSide;
                }
            }

            return TeamSide.None;
        }
    }
}
