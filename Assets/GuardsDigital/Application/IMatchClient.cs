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
    /// Two implementations, and a presenter cannot tell them apart: the Director submits every
    /// command as itself, and a seat submits its own inside an envelope the host authorizes. That is
    /// what lets the proven HUD be scoped to a seat by composition rather than by rewriting it.
    /// </remarks>
    public interface IMatchClient
    {
        IMatchReadModel Projection { get; }

        MatchClientResult Submit(DirectorCommand command);

        MatchClientResult SubmitAgreement(TeamAgreementCommand command);
    }

    /// <summary>
    /// Milestone 8.35b. One stable client reference for presenters whose authority changes once:
    /// from the locally composed content projection behind the lobby to the admitted network seat.
    /// The presenters never learn that a transport exists and therefore keep one rules-facing path.
    /// </summary>
    public sealed class SwitchableMatchClient : IMatchClient
    {
        private IMatchClient current;

        public SwitchableMatchClient(IMatchClient initial) =>
            current = initial ?? throw new ArgumentNullException(nameof(initial));

        public IMatchReadModel Projection => current.Projection;

        public void Replace(IMatchClient replacement) =>
            current = replacement ?? throw new ArgumentNullException(nameof(replacement));

        public MatchClientResult Submit(DirectorCommand command) => current.Submit(command);

        public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
            current.SubmitAgreement(command);
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
    public sealed class SeatMatchClient : IMatchClient
    {
        private static int nextSubmissionOrdinal;

        private readonly ISeatClient client;

        public SeatMatchClient(ISeatClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public MatchSeat Seat => client.Seat;

        /// <summary>This seat's authorized view, which is also what the HUD renders.</summary>
        public SeatProjection SeatProjection => client.Projection;

        public IMatchReadModel Projection => client.Projection;

        public MatchClientResult Submit(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var projection = client.Projection;
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

        private static SubmissionId NextSubmissionId() =>
            new SubmissionId("local-" + Interlocked.Increment(ref nextSubmissionOrdinal)
                .ToString(CultureInfo.InvariantCulture));
    }
}
