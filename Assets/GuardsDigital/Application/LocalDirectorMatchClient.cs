using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30. The local director adapter's client of <see cref="IMatchHost"/>.
    /// </summary>
    /// <remarks>
    /// <para>It is an <see cref="IMatchSession"/> that owns no authority: every mutating call
    /// becomes a <see cref="MatchSubmission"/> and every answer comes back from the host. That is
    /// what puts the director on the same door a network host will use - the director's controls,
    /// the headless smoke driver and every presenter reach authority through one serialized executor
    /// rather than through a session reference of their own.</para>
    /// <para>Keeping the <see cref="IMatchSession"/> shape is deliberate. The point of this
    /// milestone is that the boundary moved, not that four thousand lines of presentation changed
    /// their vocabulary; a rewrite of every presenter would have obscured exactly the structural
    /// change worth reading. Milestone 8.30b re-scopes this surface to one authorized seat, which is
    /// the change that does belong in presentation.</para>
    /// </remarks>
    public sealed class LocalDirectorMatchClient : IMatchSession
    {
        private readonly IMatchHost host;

        public LocalDirectorMatchClient(IMatchHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// The port this client submits to. A consumer that needs the ordered result stream reads it
        /// here; nothing reaches the authority behind it.
        /// </summary>
        public IMatchHost Host => host;

        public ExperimentalMatchOptions Experiments =>
            (host as MatchHost)?.Experiments ?? ExperimentalMatchOptions.Disabled;

        public MatchSessionProjection Projection => host.Projection;

        public MatchCommandResult Execute(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var result = host.Submit(MatchSubmission.ForCommand(command));
            return result.CommandResult ?? HostRefusedCommand(command, result);
        }

        public MatchSessionOperationResult Undo() =>
            Operation(MatchSubmission.ForUndo());

        public MatchSessionOperationResult Restart() =>
            Operation(MatchSubmission.ForRestart());

        public MatchSessionOperationResult RestoreSnapshot(string payload) =>
            Operation(MatchSubmission.ForSnapshotRestore(payload));

        public string CaptureSnapshot() => host.CaptureSnapshot();

        private MatchSessionOperationResult Operation(MatchSubmission submission)
        {
            var result = host.Submit(submission);
            return result.OperationResult ?? HostRefusedOperation(result);
        }

        /// <summary>
        /// The host refused before the authority answered, so there is no authority result to
        /// return. It becomes <see cref="MatchSessionRejectionReason.EngineFault"/> for that
        /// member's stated reason: the caller must not be able to show a player a host refusal as
        /// though a rule said no. Reaching this locally means a caller submitted from inside a
        /// submission, which nothing in this composition does.
        /// </summary>
        private static MatchCommandResult HostRefusedCommand(
            DirectorCommand command, MatchHostResult result) =>
            new MatchCommandResult(
                false,
                command,
                MatchSessionRejectionReason.EngineFault,
                result.Detail,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
                CardActionResolutionRejectionReason.None,
                Array.Empty<RoundResolutionEvent>(),
                result.Projection);

        private static MatchSessionOperationResult HostRefusedOperation(MatchHostResult result) =>
            new MatchSessionOperationResult(
                false,
                MatchSessionRejectionReason.EngineFault,
                result.Detail,
                result.Projection);
    }
}
