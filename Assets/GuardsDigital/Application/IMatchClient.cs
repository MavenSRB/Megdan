using System;
using System.Globalization;
using System.Threading;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30b. What one submission told the surface that made it.
    /// </summary>
    public sealed class MatchClientResult
    {
        internal MatchClientResult(
            bool accepted, string detail, IMatchReadModel projection, bool wasRefusedBeforeTheRules)
        {
            Accepted = accepted;
            Detail = detail;
            Projection = projection;
            WasRefusedBeforeTheRules = wasRefusedBeforeTheRules;
        }

        public bool Accepted { get; }
        public string Detail { get; }

        /// <summary>The reader's own view after the submission, refused or not.</summary>
        public IMatchReadModel Projection { get; }

        /// <summary>
        /// Whether the refusal happened before the rules were consulted - an unauthorized command, a
        /// view that had moved on, a repeated submission. A surface reports that differently from a
        /// rule saying no, which is check three's "rejected-input" and "stale-view" distinction.
        /// </summary>
        public bool WasRefusedBeforeTheRules { get; }
    }

    /// <summary>
    /// Milestone 8.30b. The one door a match surface submits through.
    /// </summary>
    /// <remarks>
    /// <para>Two implementations, and a presenter cannot tell them apart along the rules-facing
    /// path: the Director submits every command as itself, and a seat submits its own inside an
    /// envelope the host authorizes. That is what lets the proven HUD be scoped to a seat by
    /// composition rather than by rewriting it.</para>
    /// <para>The one thing a surface may ask is <see cref="IsDirector"/>, and only about controls
    /// that are not rules answers at all - milestone 8.35d's end-of-round continuation is the
    /// first. It is asked of the client rather than of the read model because milestone 8.35b's
    /// <see cref="SwitchableMatchClient"/> replaces a local client with an admitted network seat
    /// after composition, so a surface that decided this once at construction would decide it
    /// wrongly.</para>
    /// </remarks>
    public interface IMatchClient
    {
        IMatchReadModel Projection { get; }

        /// <summary>
        /// Whether this client submits as the Director - the direct surface with no seat, no
        /// envelope and no authorized set - rather than as one seat of a served match.
        /// </summary>
        bool IsDirector { get; }

        MatchClientResult Submit(DirectorCommand command);

        MatchClientResult SubmitAgreement(TeamAgreementCommand command);

        /// <summary>
        /// Milestone 8.35f. Steps this surface's own seat back by one accepted command, within the
        /// bounds <see cref="IMatchReadModel.IsSeatUndoAvailable"/> already published. The Director's
        /// client refuses it: its Undo is its drawer's, and is not seat-scoped.
        /// </summary>
        MatchClientResult SubmitUndo();
    }

    /// <summary>
    /// Milestone 8.35b. One stable client reference for presenters whose authority changes once:
    /// from the locally composed content projection behind the lobby to the admitted network seat.
    /// The presenters never learn that a transport exists and therefore keep one rules-facing path.
    /// </summary>
    public sealed class SwitchableMatchClient : IMatchClient, ICoordinationClient
    {
        private IMatchClient current;

        public SwitchableMatchClient(IMatchClient initial) =>
            current = initial ?? throw new ArgumentNullException(nameof(initial));

        public IMatchReadModel Projection => current.Projection;

        public bool IsDirector => current.IsDirector;
        public bool SubmitCoordination(CoordinationCommand command) =>
            current is ICoordinationClient communication && communication.SubmitCoordination(command);

        public void Replace(IMatchClient replacement) =>
            current = replacement ?? throw new ArgumentNullException(nameof(replacement));

        public MatchClientResult Submit(DirectorCommand command) => current.Submit(command);

        public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
            current.SubmitAgreement(command);

        public MatchClientResult SubmitUndo() => current.SubmitUndo();
    }

    /// <summary>
    /// Milestone 8.30b. The Director's client: every command, no envelope, the whole read model.
    /// </summary>
    public sealed class DirectorMatchClient : IMatchClient
    {
        private readonly IMatchSession session;

        public DirectorMatchClient(IMatchSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public IMatchReadModel Projection => session.Projection;

        public bool IsDirector => true;

        public MatchClientResult Submit(DirectorCommand command)
        {
            var result = session.Execute(command);
            return new MatchClientResult(
                result.Accepted,
                result.Detail,
                result.Projection,
                !result.Accepted &&
                result.RejectionReason == MatchSessionRejectionReason.CommandUnavailable);
        }

        public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
            new MatchClientResult(
                false,
                "Director mode resolves team choices directly.",
                session.Projection,
                true);

        public MatchClientResult SubmitUndo() =>
            new MatchClientResult(
                false,
                "Director mode undoes from its own drawer, not from a seat control.",
                session.Projection,
                true);
    }

    /// <summary>
    /// Milestone 8.30b. One seat's client: its own authorized view, and submissions the host
    /// authorizes against the surface that view published.
    /// </summary>
    /// <remarks>
    /// <para>The envelope is assembled here rather than by presentation, which is the point: a HUD
    /// forwards the typed invocation its projection gave it and never composes an identity, a version
    /// or a submission name. The version is always the one this client last read, so an answer
    /// decided against a board that has since moved is refused as stale instead of applied.</para>
    /// <para>Submission identities are local and monotonic. They exist so a repeat is recognizable;
    /// on the network path milestone 8.32's <see cref="PrivateSessionClient.TrySubmitCommand"/>
    /// mints the wire's own random identities instead, and nothing here depends on their shape.</para>
    /// </remarks>
    public sealed class SeatMatchClient : IMatchClient, ICoordinationClient
    {
        private static int nextSubmissionOrdinal;

        private readonly ISeatClient client;

        public SeatMatchClient(ISeatClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public MatchSeat Seat => client.Seat;
        public bool SubmitCoordination(CoordinationCommand command) =>
            client is ICoordinationClient communication && communication.SubmitCoordination(command);

        /// <summary>This seat's authorized view, which is also what the HUD renders.</summary>
        public SeatProjection SeatProjection => client.Projection;

        public IMatchReadModel Projection => client.Projection;

        public bool IsDirector => false;

        public MatchClientResult Submit(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var projection = client.Projection;
            if(projection.Experiments.TurnTimer && SharedBoardChoice.RequiresVote(projection,command) && client is IBoardVoteClient voter)
            {var voted=voter.SubmitBoardVote(command);return new MatchClientResult(voted,"Team vote recorded.",client.Projection,!voted);}
            var result = client.Submit(new SeatSubmission(NextSubmissionId(), projection.Version, command));
            return new MatchClientResult(
                result.Accepted,
                result.Detail,
                result.Projection,
                !result.Accepted &&
                result.RejectionReason != SeatSubmissionRejectionReason.SessionRefused);
        }

        public MatchClientResult SubmitAgreement(TeamAgreementCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var projection = client.Projection;
            var result = client.SubmitAgreement(new TeamAgreementSubmission(
                NextSubmissionId(), projection.Version, command));
            return new MatchClientResult(
                result.Accepted,
                result.Detail,
                result.Projection,
                !result.Accepted);
        }

        public MatchClientResult SubmitUndo()
        {
            var projection = client.Projection;
            var result = client.SubmitUndo(
                new SeatUndoSubmission(NextSubmissionId(), projection.Version));
            return new MatchClientResult(
                result.Accepted,
                result.Detail,
                result.Projection,
                !result.Accepted &&
                result.RejectionReason != SeatSubmissionRejectionReason.SessionRefused);
        }

        private static SubmissionId NextSubmissionId() =>
            new SubmissionId("local-" + Interlocked.Increment(ref nextSubmissionOrdinal)
                .ToString(CultureInfo.InvariantCulture));
    }
}
