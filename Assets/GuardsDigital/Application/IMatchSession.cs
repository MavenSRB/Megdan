using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Application
{
    public enum MatchSessionRejectionReason
    {
        None = 0,
        CommandUnavailable = 1,
        DomainRejected = 2,
        AtomicityViolation = 3,
        ReplayFailed = 4,
        SnapshotRejected = 5,

        /// <summary>
        /// A defect in the engine, not a rule refusal: something in the command path threw. The
        /// session rolled back and is usable, but the command was never a rules question. Kept
        /// distinct from <see cref="DomainRejected"/> so a caller cannot present a
        /// `NullReferenceException` to a player as though a rule said no.
        /// </summary>
        EngineFault = 6
    }

    public sealed class MatchCommandResult
    {
        internal MatchCommandResult(
            bool accepted,
            DirectorCommand command,
            MatchSessionRejectionReason rejectionReason,
            string detail,
            RoundResolutionRejectionReason roundRejectionReason,
            TurnCardCommitmentRejectionReason commitmentRejectionReason,
            TurnResolutionRejectionReason turnRejectionReason,
            CardActionResolutionRejectionReason cardActionRejectionReason,
            IReadOnlyList<RoundResolutionEvent> events,
            MatchSessionProjection projection)
        {
            Accepted = accepted;
            Command = command;
            RejectionReason = rejectionReason;
            Detail = detail;
            RoundRejectionReason = roundRejectionReason;
            CommitmentRejectionReason = commitmentRejectionReason;
            TurnRejectionReason = turnRejectionReason;
            CardActionRejectionReason = cardActionRejectionReason;
            Events = events;
            Projection = projection;
        }

        public bool Accepted { get; }
        public DirectorCommand Command { get; }
        public MatchSessionRejectionReason RejectionReason { get; }
        public string Detail { get; }
        public RoundResolutionRejectionReason RoundRejectionReason { get; }
        public TurnCardCommitmentRejectionReason CommitmentRejectionReason { get; }
        public TurnResolutionRejectionReason TurnRejectionReason { get; }

        /// <summary>
        /// Milestone 8.25a. The reason the card-action backend gave for a refused Movement, Skill,
        /// Attack or correction command, and <c>None</c> for every refusal that did not come from one.
        ///
        /// <para>The three <see cref="TurnResolutionRejectionReason"/> command members say which
        /// command the rules refused; this says what they said about it. Diagnosing that milestone's
        /// stall took retrying all sixteen offered destinations one at a time, because the answer -
        /// that the refusal was about the resolution and not about any destination - was thrown away
        /// one layer below this one.</para>
        /// </summary>
        public CardActionResolutionRejectionReason CardActionRejectionReason { get; }
        public IReadOnlyList<RoundResolutionEvent> Events { get; }
        public MatchSessionProjection Projection { get; }
    }

    public sealed class MatchSessionOperationResult
    {
        internal MatchSessionOperationResult(
            bool accepted,
            MatchSessionRejectionReason rejectionReason,
            string detail,
            MatchSessionProjection projection)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Detail = detail;
            Projection = projection;
        }

        public bool Accepted { get; }
        public MatchSessionRejectionReason RejectionReason { get; }
        public string Detail { get; }
        public MatchSessionProjection Projection { get; }
    }

    public interface IMatchSession
    {
        MatchSessionProjection Projection { get; }
        MatchCommandResult Execute(DirectorCommand command);
        MatchSessionOperationResult Undo();
        MatchSessionOperationResult Restart();
        MatchSessionOperationResult RestoreSnapshot(string payload);
        string CaptureSnapshot();
    }
}
