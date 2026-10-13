using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35b. The network implementation of the one match-client door the proven HUD
    /// already uses. Presentation forwards only host-published commands; this object adds the
    /// wire identity/version envelope through <see cref="PrivateSessionClient"/>.
    /// </summary>
    public sealed class NetworkMatchClient : IMatchClient, ICoordinationClient
    {
        private sealed class PendingEffectAutoConfirm
        {
            public PendingEffectAutoConfirm(
                string submissionId,
                EffectChoiceCommand selection,
                NetworkSeatProjectionRecord origin,
                long projectionRevision,
                int connectionEpoch,
                int resyncRequestCount,
                int recoveredProjectionGapCount)
            {
                SubmissionId = submissionId;
                Selection = selection;
                Origin = origin;
                ProjectionRevision = projectionRevision;
                ConnectionEpoch = connectionEpoch;
                ResyncRequestCount = resyncRequestCount;
                RecoveredProjectionGapCount = recoveredProjectionGapCount;
            }

            public string SubmissionId { get; }
            public EffectChoiceCommand Selection { get; }
            public NetworkSeatProjectionRecord Origin { get; }
            public long ProjectionRevision { get; }
            public int ConnectionEpoch { get; }
            public int ResyncRequestCount { get; }
            public int RecoveredProjectionGapCount { get; }
        }

        private readonly PrivateSessionClient session;
        private readonly IMatchReadModel trustedContent;
        private readonly MatchContentCatalogs catalogs;
        private NetworkMatchReadModel projection;
        private NetworkSeatProjectionRecord projectedRecord;
        private long projectedRevision = -1;
        private long projectedLogRevision = -1;
        private bool projectedActionable;
        private PendingEffectAutoConfirm pendingEffectAutoConfirm;

        public NetworkMatchClient(
            PrivateSessionClient session,
            IMatchReadModel trustedContent,
            MatchContentCatalogs catalogs)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.trustedContent = trustedContent ??
                throw new ArgumentNullException(nameof(trustedContent));
            this.catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
        }

        public IMatchReadModel Projection
        {
            get
            {
                ContinueEffectAutoConfirm();
                return CurrentProjection();
            }
        }

        /// <summary>An admitted seat of a served match, never the Director.</summary>
        public bool IsDirector => false;
        public bool SubmitCoordination(CoordinationCommand command) => session.TrySubmitCoordination(command);

        public MatchClientResult Submit(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if((Projection as IExperimentalReadModel)?.Experiments.TurnTimer==true && SharedBoardChoice.RequiresVote(Projection,command))
            {var voted=session.TrySubmitBoardVote(command);return new MatchClientResult(voted,voted?"Team vote recorded.":"Vote unavailable.",Projection,!voted);}
            var origin = session.LatestProjection;
            var originRevision = session.ProjectionRevision;
            var sent = session.TrySubmitCommand(command, out var submissionId);
            if (sent &&
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice?.Kind == EffectChoiceCommandKind.Select)
            {
                pendingEffectAutoConfirm = new PendingEffectAutoConfirm(
                    submissionId,
                    command.EffectChoice,
                    origin,
                    originRevision,
                    session.ConnectionEpoch,
                    session.ResyncRequestCount,
                    session.RecoveredProjectionGapCount);
            }
            else if (sent)
            {
                pendingEffectAutoConfirm = null;
            }

            return new MatchClientResult(
                sent,
                sent ? "Sent to the host." : NetworkMatchFeedback.For(session),
                Projection,
                !sent);
        }

        public MatchClientResult SubmitAgreement(TeamAgreementCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var sent = session.TrySubmitAgreement(command, out _);
            return new MatchClientResult(
                sent,
                sent ? "Statement sent to the host." : NetworkMatchFeedback.For(session),
                Projection,
                !sent);
        }

        public MatchClientResult SubmitUndo()
        {
            var sent = session.TrySubmitUndo(out _);
            return new MatchClientResult(
                sent,
                sent ? "Rewind sent to the host." : NetworkMatchFeedback.For(session),
                Projection,
                !sent);
        }

        private IMatchReadModel CurrentProjection()
        {
            var record = session.LatestProjection;
            var revision = session.ProjectionRevision;
            var actionable = session.State == PrivateSessionClientState.Seated &&
                !session.IsWaitingForHost;
            if (projection == null ||
                !ReferenceEquals(record, projectedRecord) ||
                revision != projectedRevision ||
                actionable != projectedActionable)
            {
                projection = new NetworkMatchReadModel(
                    trustedContent, catalogs, record, actionable);
                projectedRecord = record;
                projectedRevision = revision;
                projectedActionable = actionable;
                projectedLogRevision = -1;
            }

            // Milestone 8.35r. The log has its own revision because it has its own frames: an
            // update can land between two projection pushes, and a view rebuilt only when the
            // projection moved would show a log several lines behind the board.
            if (projectedLogRevision != session.MatchLogRevision)
            {
                projection.ReplaceMatchLog(session.MatchLog);
                projectedLogRevision = session.MatchLogRevision;
            }

            projection.ReplaceCoordination(session.LatestCoordination);
            return projection;
        }

        /// <summary>
        /// The local HUD immediately forwards Confirm after a Select fills an effect choice. A
        /// network submission returns before the host's result and projection exist, so polling the
        /// adapter completes the same two-command continuation only after both authoritative facts
        /// arrive. The continuation is cleared before sending, making repeated renders idempotent.
        ///
        /// <para>Milestone 8.46b. A choice whose minimum and maximum are both one is now closed by
        /// the host inside the Select, so the projection that comes back carries no such pause and
        /// the guards below drop the continuation without sending anything - the one press really is
        /// one command on the wire too, which is what the seat's rewind needs it to be. The arming
        /// above still fires on any Select because the local client cannot know the host's verdict
        /// yet, and what is left for this to finish is an `up to 1` or a choose-several that a
        /// selection filled. No DTO, flag or revision moves for any of it.</para>
        /// </summary>
        private void ContinueEffectAutoConfirm()
        {
            var pending = pendingEffectAutoConfirm;
            if (pending == null)
            {
                return;
            }

            if (session.State != PrivateSessionClientState.Seated ||
                session.ConnectionEpoch != pending.ConnectionEpoch ||
                session.ResyncRequestCount != pending.ResyncRequestCount ||
                session.RecoveredProjectionGapCount != pending.RecoveredProjectionGapCount)
            {
                pendingEffectAutoConfirm = null;
                return;
            }

            var result = session.LastSubmissionResult;
            if (result == null ||
                !string.Equals(result.SubmissionId, pending.SubmissionId, StringComparison.Ordinal))
            {
                if (!session.IsWaitingForHost && result != null)
                {
                    pendingEffectAutoConfirm = null;
                }

                return;
            }

            if (!result.Accepted)
            {
                pendingEffectAutoConfirm = null;
                return;
            }

            var received = session.LatestProjection;
            if (session.IsWaitingForHost ||
                received == null ||
                ReferenceEquals(received, pending.Origin) ||
                session.ProjectionRevision <= pending.ProjectionRevision ||
                received.Version < result.Version)
            {
                return;
            }

            var acceptedProjection = new NetworkMatchReadModel(
                trustedContent, catalogs, received, true);
            var choice = acceptedProjection.PendingEffectChoice;
            if (choice == null ||
                !choice.ResolutionId.Equals(pending.Selection.ResolutionId) ||
                !choice.ChoiceId.Equals(pending.Selection.ChoiceId) ||
                !choice.SelectedCandidates.Contains(pending.Selection.Candidate) ||
                choice.MaximumSelections < 1 ||
                choice.SelectedCandidates.Count != choice.MaximumSelections ||
                choice.ConfirmInvocation == null ||
                // Milestone 8.93f: a rune-to-slot permutation waits for its own DONE, exactly as
                // the local presenters' EffectChoiceCompletion does.
                choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot)
            {
                pendingEffectAutoConfirm = null;
                return;
            }

            var confirm = acceptedProjection.DecisionContract?.Commands.SingleOrDefault(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation));
            pendingEffectAutoConfirm = null;
            if (confirm != null)
            {
                session.TrySubmitCommand(confirm, out _);
            }
        }
    }

    /// <summary>Bounded, player-facing wording for the online match state.</summary>
    public static class NetworkMatchFeedback
    {
        public const int MaxLength = 96;

        internal static string PlayerDetail(MatchSessionRejectionReason reason, string detail, string fallback) =>
            reason == MatchSessionRejectionReason.EngineFault || reason == MatchSessionRejectionReason.AtomicityViolation
                ? "Internal error. Action not completed; please report this."
                : string.IsNullOrWhiteSpace(detail) ? fallback : detail;

        public static string For(PrivateSessionClient client)
        {
            if (client == null) return Bound("Not connected.");
            return ForState(
                client.State,
                client.IsReconnecting,
                client.LatestProjection != null,
                client.IsWaitingForHost,
                client.MatchFeedback,
                client.RejectionReason.ToString(),
                client.IsHostResponseDelayed);
        }

        /// <summary>The deterministic wording table, exposed so every state can be tested directly.</summary>
        public static string ForState(
            PrivateSessionClientState state,
            bool reconnecting = false,
            bool hasProjection = false,
            bool waiting = false,
            string feedback = "",
            string rejection = "",
            bool delayed = false)
        {
            switch (state)
            {
                case PrivateSessionClientState.AwaitingChallenge:
                case PrivateSessionClientState.AwaitingWelcome:
                case PrivateSessionClientState.AwaitingSeat:
                    return Bound(reconnecting ? "Reconnecting to the host…" : "Joining the session…");
                case PrivateSessionClientState.Seated:
                    return Bound(!hasProjection
                        ? "Synchronizing the match…"
                        : waiting
                            ? delayed
                                ? "The host is taking longer than expected…"
                                : "Waiting for the host's response…"
                            : !string.IsNullOrEmpty(feedback)
                                ? "Host refused the input: " + feedback
                                : "Connected to the host.");
                case PrivateSessionClientState.Resynchronizing:
                    return Bound("View out of date — synchronizing with the host…");
                case PrivateSessionClientState.HostLost:
                    return Bound("Host connection lost. The match cannot continue here.");
                case PrivateSessionClientState.Disconnected:
                    return Bound(string.IsNullOrEmpty(feedback)
                        ? "Disconnected from the session."
                        : feedback);
                case PrivateSessionClientState.Rejected:
                    return Bound("Host refused the request: " + rejection + ".");
                case PrivateSessionClientState.Faulted:
                    return Bound("The session sent incompatible match data.");
                default:
                    return Bound("Not connected.");
            }
        }

        private static string Bound(string value) =>
            value.Length <= MaxLength ? value : value.Substring(0, MaxLength);
    }

    /// <summary>
    /// Rehydrates the bounded network DTO into the read contract shared by every existing match
    /// presenter. Static content comes only from the locally registered content whose digest the
    /// admission handshake already matched; mutable state comes only from the received owner-scoped
    /// record. While the session is not actionable the retained view is deliberately stripped of
    /// every offer and pending answer.
    /// </summary>
    public sealed class NetworkMatchReadModel : IMatchReadModel, ILaneWaveCounterReadModel, IExperimentalReadModel, ICardValueReadModel, IModeTurnRosterReadModel
    {
        public IReadOnlyList<HeroId> OptionalTurnHeroIds =>
            (trusted as IModeTurnRosterReadModel)?.OptionalTurnHeroIds ?? Array.Empty<HeroId>();
        private readonly IMatchReadModel trusted;
        private readonly MatchContentCatalogs catalogs;
        private NetworkSeatProjectionRecord wire;
        private bool actionable;
        private bool effectChoiceMaterialized;
        private PendingEffectChoice pendingEffectChoice;
        private bool removalPreventionMaterialized;
        private RemovalPreventionPrompt pendingRemovalPrevention;
        private bool decisionContractMaterialized;
        private PendingDecisionContract decisionContract;
        private IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> matchLog;

        /// <summary>
        /// Milestone 8.35r. Takes the log the session client has accumulated from the host's own
        /// channel. It is set separately from <see cref="Replace"/> because the two arrive in
        /// different frames: a projection push and a match-log update are not one message, and
        /// pretending otherwise would make the log stale whenever only one of them landed.
        /// </summary>
        public void ReplaceMatchLog(
            IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> log)
        {
            matchLog = log;
        }

        public NetworkMatchReadModel(IMatchReadModel trustedContent, MatchContentCatalogs catalogs)
        {
            trusted = trustedContent ?? throw new ArgumentNullException(nameof(trustedContent));
            this.catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
        }

        public NetworkMatchReadModel(
            IMatchReadModel trustedContent,
            MatchContentCatalogs catalogs,
            NetworkSeatProjectionRecord record,
            bool isActionable)
            : this(trustedContent, catalogs)
        {
            Replace(record, isActionable);
        }

        public void Replace(NetworkSeatProjectionRecord record, bool isActionable)
        {
            var accepted = wire;
            if (record != null && (wire == null || record.Version >= wire.Version))
            {
                accepted = record;
            }

            var nextActionable = isActionable && accepted != null;
            if (ReferenceEquals(accepted, wire) && nextActionable == actionable)
            {
                return;
            }

            if (!ReferenceEquals(wire, accepted)) coordinationDisplay = null;
            wire = accepted;
            actionable = nextActionable;
            effectChoiceMaterialized = false;
            pendingEffectChoice = null;
            removalPreventionMaterialized = false;
            pendingRemovalPrevention = null;
            decisionContractMaterialized = false;
            decisionContract = null;
        }

        public IReadOnlyList<HeroCardInspection> CardInspections => wire?.CardInspections ?? Array.Empty<HeroCardInspection>();
        public ExperimentalMatchOptions Experiments => wire?.Experiments ?? ExperimentalMatchOptions.Disabled;
        public IReadOnlyList<CardInstance> AlliedHandCards => wire == null ? Array.Empty<CardInstance>() : Array.AsReadOnly(wire.AlliedHandCards.Select(ToCard).ToArray());
        private CoordinationDisplayState coordinationDisplay;
        public void ReplaceCoordination(CoordinationDisplayState state) => coordinationDisplay = state?.Version == Version ? state : null;
        public IReadOnlyList<TeammateMarker> TeammateMarkers => coordinationDisplay?.Markers ?? wire?.TeammateMarkers ?? Array.Empty<TeammateMarker>();
        public long CoordinationRevision => coordinationDisplay?.Revision ?? wire?.CoordinationRevision ?? 0;
        public int CoordinationGeneration => coordinationDisplay?.Generation ?? wire?.CoordinationGeneration ?? 0;
        public MatchClockState Clock => wire?.Clock ?? MatchClockState.Disabled;
        public long Version => wire?.Version ?? trusted.Version;
        public bool IsInputEnabled => actionable;

        /// <summary>
        /// The host-published command surface for this network seat. Kept outside
        /// <see cref="IMatchReadModel"/> because local Director projections derive their own full
        /// surface; a network-aware journey may use this exact list instead of reconstructing one.
        /// </summary>
        public IReadOnlyList<DirectorCommand> AuthorizedCommands =>
            actionable && wire != null
                ? wire.AuthorizedCommands
                : Array.Empty<DirectorCommand>();

        /// <summary>
        /// Milestone 8.35f. The host's own answer, carried on the seat record, and withheld while
        /// this view is not actionable - a waiting, stale, resynchronizing or disconnected client
        /// may not mint a rewind any more than it may mint a command.
        /// </summary>
        public bool IsSeatUndoAvailable => actionable && (wire?.IsSeatUndoAvailable ?? false);

        /// <summary>
        /// Milestone 8.35r. Who the authority is waiting on, read off the record the host has been
        /// pushing to every seat since milestone 8.32 - this member is what finally exposes it to a
        /// surface. Unlike <see cref="IsSeatUndoAvailable"/> it is NOT withheld while the view is
        /// unactionable: "we are waiting for Blue" stays true, and stays worth showing, on a client
        /// that is resynchronizing or holding a stale view. Withholding it would blank the tray and
        /// the narration exactly when a player most needs to know what the table is waiting for.
        /// </summary>
        public PendingDecisionOwner DecisionOwner
        {
            get
            {
                var owner = wire?.DecisionOwner;
                if (owner == null || string.IsNullOrWhiteSpace(owner.FamilyId))
                {
                    return wire == null ? trusted.DecisionOwner : PendingDecisionOwner.None;
                }

                var family = new PendingDecisionFamilyId(owner.FamilyId);
                return owner.HeroIds.Count == 0
                    ? PendingDecisionOwner.ForTeam(family, (TeamSide)owner.TeamSide)
                    : PendingDecisionOwner.ForHeroes(
                        family, owner.HeroIds.Select(value => new HeroId(value)));
            }
        }

        /// <summary>
        /// Milestone 8.35r. The match log the host pushes on its own channel. It is the CLIENT's
        /// accumulated log rather than anything on the projection record, because the log arrives in
        /// the projection's frames - see <c>NetworkSeatProjectionMessage.MatchLog</c> for why it
        /// travels there rather than in the seat record or a frame of its own.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog =>
            matchLog ?? trusted.MatchLog;

        /// <summary>Milestone 8.35r. The standing rewind, carried on the detail record.</summary>
        public MatchUndoMarker UndoMarker
        {
            get
            {
                var marker = PublicDetail?.UndoMarker;
                return marker == null
                    ? (wire == null ? trusted.UndoMarker : MatchUndoMarker.None)
                    : MatchUndoMarker.Of(new HeroId(marker.HeroId), marker.Count);
            }
        }

        public QuickGameSetupState Setup => trusted.Setup;
        public BoardMapDefinition SelectedMap => trusted.SelectedMap;
        public MatchParticipants Participants => trusted.Participants;
        public IReadOnlyList<CardDefinition> Definitions => trusted.Definitions;

        /// <summary>
        /// Milestone 8.35z60. Reads the host's live count off the decoded frame, exactly as
        /// <see cref="TeamLifeCounters"/> does, and falls back to the locally composed match's own
        /// starting total only before the first frame has arrived - the same shape every other
        /// member of this class already has.
        /// </summary>
        /// <remarks>
        /// This member was authored in the worktree as an unconditional <c>trusted</c> fall-through,
        /// with the honest note that nothing on the wire carried a live pool and a remote seat would
        /// therefore read the frozen starting total after the first push anywhere at the table. The
        /// wave's integration pass paid that debt rather than shipping it: protocol version 11 adds
        /// the field, so this is now a real decoded read and the branch above is the ordinary
        /// pre-first-frame one. Every real playtest of this project is networked, so leaving the
        /// fall-through in place would have made milestone 8.35z60 correct only for the host.
        /// </remarks>
        public int RemainingWaveCounters =>
            wire == null ? trusted.RemainingWaveCounters : wire.RemainingWaveCounters;

        public IReadOnlyList<int> RemainingWaveCountersByLane =>
            wire == null
                ? ((ILaneWaveCounterReadModel)trusted).RemainingWaveCountersByLane
                : wire.RemainingWaveCountersByLane;

        public IReadOnlyList<HeroOccupancy> Heroes => wire == null ? trusted.Heroes :
            Array.AsReadOnly(wire.Heroes.Select(row =>
                new HeroOccupancy(new HeroId(row.HeroId), row.Body, new HexCoordinate(row.Q, row.R))).ToArray());

        public IReadOnlyList<MinionOccupancy> Minions => wire == null ? trusted.Minions :
            Array.AsReadOnly(wire.Minions.Select(row =>
            {
                var original = trusted.Minions.Single(value => value.MinionId.Value == row.MinionId);
                var coordinate = new HexCoordinate(row.Q, row.R);
                var space = SelectedMap.Spaces.Single(value => value.Coordinate.Equals(coordinate));
                return original.WithPosition(
                    new ConfiguredSpaceReference(SelectedMap.Id, space.Id), coordinate);
            }).ToArray());

        public IReadOnlyList<ObstacleOccupancy> Obstacles => wire == null ? trusted.Obstacles :
            Array.AsReadOnly(wire.Obstacles.Select(row => new ObstacleOccupancy(
                new ObstacleId(row.ObstacleId), new HexCoordinate(row.Q, row.R))).ToArray());

        public IReadOnlyList<PlacedToken> PlacedTokens => wire == null ? trusted.PlacedTokens :
            Array.AsReadOnly(wire.PlacedTokens.Select(ToToken).ToArray());

        public IReadOnlyList<MarkerAttachment> HeldMarkers => wire == null ? trusted.HeldMarkers :
            Array.AsReadOnly(wire.HeldMarkers.Select(ToMarker).ToArray());

        public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
            HeldMarkers.Where(value => value.HolderHeroId.Equals(holderHeroId));

        public int RoundIndex => wire?.RoundIndex ?? trusted.RoundIndex;
        public int TurnIndex => wire?.TurnIndex ?? trusted.TurnIndex;
        public RoundResolutionPhase Phase => wire == null ? trusted.Phase : (RoundResolutionPhase)wire.Phase;
        public TieBreakerState TieBreakerState => wire == null ? trusted.TieBreakerState :
            new TieBreakerState((TeamSide)wire.TieBreakerRevealedSide);
        public IReadOnlyList<RoundTurnSlot> TurnSlots => wire == null ? trusted.TurnSlots :
            Array.AsReadOnly(wire.TurnSlots.Select(ToTurnSlot).ToArray());
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => PublicDetail == null
            ? (wire == null ? trusted.CompletedRounds : Array.Empty<CompletedRoundRecord>())
            : Array.AsReadOnly(PublicDetail.CompletedRounds.Select(row => new CompletedRoundRecord(
                row.RoundIndex,
                row.TurnSlots.Select(ToTurnSlot),
                row.EndOfRoundOutcomes.Select(value => new EndOfRoundStageOutcome(
                    (EndOfRoundStage)value.Stage, (EndOfRoundStageOutcomeKind)value.Kind)))).ToArray());
        public EndOfRoundStage? CurrentEndOfRoundStage => wire?.EndOfRoundStage == null
            ? (wire == null ? trusted.CurrentEndOfRoundStage : null)
            : (EndOfRoundStage?)wire.EndOfRoundStage.Value;

        public IReadOnlyList<MatchCommitmentProjection> Commitments => wire == null ? trusted.Commitments :
            Array.AsReadOnly(wire.Commitments.Select(row => new MatchCommitmentProjection(
                new HeroId(row.HeroId),
                (TurnCardCommitmentStatus)row.Status,
                row.CanPass,
                row.RevealedCardInstanceId == null ? (CardInstanceId?)null :
                    new CardInstanceId(row.RevealedCardInstanceId),
                row.CanCommitTwoCards)).ToArray());

        public MatchCommitmentProjection GetCommitment(HeroId heroId) =>
            Commitments.FirstOrDefault(value => value.HeroId.Equals(heroId));

        public SelectedInitiativeActor SelectedActor => wire?.SelectedActor?.IsDefined == true
            ? new SelectedInitiativeActor(
                new CardInstanceId(wire.SelectedActor.CardInstanceId),
                new HeroId(wire.SelectedActor.ActorId),
                (TeamSide)wire.SelectedActor.TeamSide,
                wire.SelectedActor.InitiativeTotal,
                wire.SelectedActor.FlipTieBreakerAfterResolution)
            : default;

        public bool IsTurnActive => wire?.IsTurnActive ?? trusted.IsTurnActive;
        public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters => wire == null
            ? trusted.TeamLifeCounters
            : Array.AsReadOnly(wire.TeamLifeCounters.Select(row =>
                new KeyValuePair<TeamId, int>(new TeamId(row.TeamId), row.Count)).ToArray());

        /// <summary>
        /// Milestone 9.30g, protocol /32. The host's team-victory tallies, decoded row for row; before
        /// the first frame this reads the locally composed match's, as every carried member does.
        /// </summary>
        public IReadOnlyList<TeamVictoryTally> TeamVictoryTallies => wire == null
            ? trusted.TeamVictoryTallies
            : Array.AsReadOnly(wire.TeamVictoryTallies.Select(row =>
                new TeamVictoryTally(new TeamId(row.TeamId), row.Count, row.Threshold)).ToArray());

        public IReadOnlyList<HeroProgressionProjection> HeroProgression => wire == null
            ? trusted.HeroProgression
            : Array.AsReadOnly(wire.HeroProgression.Select(row => new HeroProgressionProjection(
                new HeroId(row.HeroId), row.Level, row.Coins, row.LeveledThisRound,
                row.ItemBonuses.Select(value => new KeyValuePair<PrintedValueChannel, int>(
                    (PrintedValueChannel)value.Key, value.Value)))).ToArray());

        /// <summary>
        /// Milestone 8.53, protocol 17. The host's own match scoreboard, decoded row for row. Before
        /// the first frame arrives this reads the locally composed match's board, which is every
        /// hero at zero - the same fall-back every other carried member takes, and the honest one
        /// here: a client that has been told nothing has watched nothing happen.
        /// </summary>
        public IReadOnlyList<HeroScoreboardEntry> Scoreboard => wire == null
            ? trusted.Scoreboard
            : Array.AsReadOnly(wire.Scoreboard.Select(row => new HeroScoreboardEntry(
                new HeroId(row.HeroId), row.Kills, row.Deaths, row.AttacksAtHeroes,
                row.TimesDefended, row.MinionsKilled, row.CoinsEarned)).ToArray());

        /// <summary>
        /// Milestone 8.39g. A seat reads the ending it was sent, in whichever of the two shapes it
        /// was sent in: an ending that names a hero is a solo ending, and one that names teams is one
        /// of the rulebook's three team sentences. A losing seat reads the same row the winner does.
        /// </summary>
        public MatchOutcome Outcome => wire?.Outcome == null || wire.Outcome.Cause == 0
            ? MatchOutcome.Unfinished
            : string.IsNullOrEmpty(wire.Outcome.WinningHeroId)
                ? MatchOutcome.Won((MatchVictoryCause)wire.Outcome.Cause,
                    new TeamId(wire.Outcome.WinningTeamId), new TeamId(wire.Outcome.LosingTeamId))
                : MatchOutcome.WonAlone((MatchVictoryCause)wire.Outcome.Cause,
                    new HeroId(wire.Outcome.WinningHeroId));

        public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds => wire == null
            ? trusted.ActiveEffectSourceCardInstanceIds
            : Array.AsReadOnly(wire.ActiveEffectSourceCardInstanceIds.Select(
                value => new CardInstanceId(value)).ToArray());

        public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
            ActiveEffectSourceCardInstanceIds.Contains(cardInstanceId);

        public IReadOnlyList<CardInstanceId> StandingActiveEffectSourceCardInstanceIds =>
            wire == null
                ? trusted.StandingActiveEffectSourceCardInstanceIds
                : Array.AsReadOnly(wire.StandingActiveEffectSourceCardInstanceIds.Select(
                    value => new CardInstanceId(value)).ToArray());

        public IReadOnlyList<CardInstance> Cards => wire == null ? trusted.Cards :
            Array.AsReadOnly(wire.OwnCards.Concat(wire.PublicCards).Select(ToCard).ToArray());

        /// <summary>
        /// Milestone 9.30b, protocol /31. The host's hand, deck and spellbook counts, decoded row
        /// for row. Before the first frame arrives this reads the locally composed match's counts,
        /// the same fall-back every other carried member takes.
        /// </summary>
        public IReadOnlyList<SeatHandSummary> HandSummaries => wire == null
            ? trusted.HandSummaries
            : Array.AsReadOnly(wire.HandSummaries.Select(row => new SeatHandSummary(
                new HeroId(row.HeroId), row.HandCount, row.DeckCount, row.SpellbookCount,
                row.SpellbookBacks, row.FacedownDiscardColors)).ToArray());

        private NetworkProjectionDetailRecord PublicDetail => wire?.Detail;
        private NetworkProjectionDetailRecord Detail => actionable ? PublicDetail : null;

        public TeamAgreementProjection TeamAgreement =>
            ToAgreement(PublicDetail?.TeamAgreement, actionable, wire?.HeroId);
        public PendingDecisionContract DecisionContract
        {
            get
            {
                if (!decisionContractMaterialized)
                {
                    decisionContract = BuildDecisionContract();
                    decisionContractMaterialized = true;
                }

                return decisionContract;
            }
        }

        // The detailed prompt conversion is filled by the same DTO-to-domain boundary. Until a
        // session is actionable every one is intentionally empty, which clears stale controls.
        public PendingInitiativeChoice PendingInitiativeChoice
        {
            get
            {
                var row = Detail?.PendingInitiativeChoice;
                return row == null ? null : new PendingInitiativeChoice(
                    (TeamSide)row.OwningSide,
                    row.InitiativeTotal,
                    row.CandidateCardInstanceIds.Select(value => new CardInstanceId(value)),
                    row.FlipTieBreakerAfterResolution);
            }
        }
        public PendingCardActionResolution PendingMovementResolution => null;
        public bool HasPendingMovementResolution =>
            Detail?.PendingMovementResolution != null;
        public PendingMovementChoice PendingMovementChoice
        {
            get
            {
                var row = Detail?.PendingMovementChoice;
                return row == null ? null : new PendingMovementChoice(
                    new ActionResolutionId(row.ResolutionId),
                    new ActionResolutionStepId(row.ChoiceId),
                    new HeroId(row.ChooserId),
                    new HeroId(row.MovingHeroId),
                    new HexCoordinate(row.OriginQ, row.OriginR),
                    row.RemainingAllowance,
                    row.CanStop,
                    row.LegalDestinations.Select(value => new ReachableMovementDestination(
                        new HexCoordinate(value.Q, value.R),
                        value.Route.Select(ToCoordinate), value.Cost)));
            }
        }
        public PendingAttackResolution PendingAttackResolution => null;
        public bool HasPendingAttackResolution =>
            Detail?.PendingAttackResolution != null;
        public PendingAttackTargetChoice PendingAttackTargetChoice
        {
            get
            {
                var row = Detail?.PendingAttackTargetChoice;
                return row == null ? null : new PendingAttackTargetChoice(
                    new ActionResolutionId(row.ResolutionId),
                    new ActionResolutionStepId(row.ChoiceId),
                    new HeroId(row.ChooserId),
                    row.LegalTargets.Select(ToAttackTarget));
            }
        }
        public IReadOnlyList<AttackTargetDefensePreviewProjection> PendingAttackTargetDefensePreviews =>
            Detail == null ? Array.Empty<AttackTargetDefensePreviewProjection>() :
            Array.AsReadOnly(Detail.PendingAttackTargetDefensePreviews.Select(row =>
                new AttackTargetDefensePreviewProjection(
                    new HeroId(row.HeroId), row.HeroBody, row.MinionContributions.Select(ToContribution))).ToArray());
        public IReadOnlyList<HeroDefenseModifierProjection> HeroDefenseModifiers =>
            PublicDetail == null ? Array.Empty<HeroDefenseModifierProjection>() :
            Array.AsReadOnly(PublicDetail.HeroDefenseModifiers.Select(row =>
                new HeroDefenseModifierProjection(
                    new HeroId(row.HeroId), row.HeroBody, row.Contributions.Select(ToContribution))).ToArray());
        public IReadOnlyList<CardValuePreview> CardValues =>
            PublicDetail?.CardValues ?? Array.Empty<CardValuePreview>();
        public PendingDefenseResponseChoice PendingDefenseChoice
        {
            get
            {
                var row = Detail?.PendingDefenseChoice;
                return row == null ? null : new PendingDefenseResponseChoice(
                    new ActionResolutionId(row.ResolutionId),
                    new ActionResolutionStepId(row.ChoiceId),
                    new HeroId(row.ChooserId),
                    new HeroId(row.AttackerId),
                    // Milestone 8.80. The attacking card, materialised here rather than looked up
                    // out of a frame this seat is never handed.
                    new CardInstanceId(row.SourceCardInstanceId),
                    new CardDefinitionReference(
                        new CardDefinitionId(row.SourceDefinitionId), row.SourceContentRevision),
                    new ActionDefinitionId(row.ActionDefinitionId),
                    row.AttackTotal,
                    row.LegalDefenseOffers.Select(ToOffer),
                    row.CanDecline);
            }
        }
        public PendingDefensePreviewProjection PendingDefensePreview
        {
            get
            {
                var row = Detail?.PendingDefensePreview;
                return row == null ? null : new PendingDefensePreviewProjection(
                    new ActionResolutionId(row.ResolutionId),
                    new ActionResolutionStepId(row.ChoiceId),
                    row.AttackContributions.Select(ToContribution),
                    row.AttackTotal,
                    row.Choices.Select(value => new DefenseChoicePreviewProjection(
                        value.CardInstanceId == null ? (CardInstanceId?)null :
                            new CardInstanceId(value.CardInstanceId),
                        value.ChoiceLabel,
                        value.DefenseContributions.Select(ToContribution),
                        value.HasDefenseTotal ? (int?)value.DefenseTotal : null,
                        (DefenseBlockResult)value.BlockResult,
                        (AttackResolutionOutcome)value.Outcome,
                        value.SuppressesMinionContributions)),
                    row.MinionContributions.Select(ToContribution));
            }
        }
        public PendingResolution PendingSkillResolution => null;
        public bool HasPendingSkillResolution =>
            Detail?.PendingSkillResolution != null;

        /// <summary>
        /// Milestone 8.58. WHICH card is resolving, which the wire has carried on its own resolution
        /// row since milestone 8.32 and this client threw away with the rest of the frame.
        ///
        /// <para>The frame itself stays <c>null</c> here - 8.40f's privacy arm, and the reason the
        /// lobby bot's own comment above says so out loud. This is the one field of it that is public
        /// by construction: the card left a hand and stands in the turn area where every seat can see
        /// it, and a decision window that could not name it drew a node id instead.</para>
        /// </summary>
        public CardInstanceId ResolvingSourceCardInstanceId
        {
            get
            {
                // The same three frames, in the same order, the host reads - see
                // `MatchSessionProjection.ResolvingSourceCardInstanceId`.
                //
                // Milestone 8.80 corrects what 8.58's comment here claimed - "Only the Skill frame is
                // withheld here; the other two already cross whole". NONE of the three crosses whole:
                // this client returns `null` from `PendingSkillResolution`, `PendingAttackResolution`
                // AND `PendingMovementResolution` alike, so the Attack slot below was asking a
                // property that is null by construction and a seat standing inside an Attack named no
                // card at all. It now reads the RECORD the wire delivered, exactly as the Skill slot
                // above it already did - the resolving card id has ridden its own row since 8.32.
                //
                // Milestone 9.11 (finding F1) reads the Movement slot from its row too. 8.80 left it
                // on the (always null) property on the reasoning that no surface read this member
                // during a Movement; the wire completeness walk showed a client inside Tigerclaw's
                // Blink Strike Movement answering "no card" where the host's view of the same seat
                // named it - the same half-carried field 8.80 closed for the Attack slot.
                var skill = Detail?.PendingSkillResolution?.SourceCardInstanceId;
                if (!string.IsNullOrWhiteSpace(skill))
                {
                    return new CardInstanceId(skill);
                }

                var attack = Detail?.PendingAttackResolution?.SourceCardInstanceId;
                if (!string.IsNullOrWhiteSpace(attack))
                {
                    return new CardInstanceId(attack);
                }

                var movement = Detail?.PendingMovementResolution?.SourceCardInstanceId;
                if (!string.IsNullOrWhiteSpace(movement))
                {
                    return new CardInstanceId(movement);
                }

                return PendingEffectChoice != null && SelectedActor.IsDefined
                    ? SelectedActor.CardInstanceId
                    : default;
            }
        }
        public PendingEffectChoice PendingEffectChoice
        {
            get
            {
                if (!effectChoiceMaterialized)
                {
                    pendingEffectChoice = BuildEffectChoice();
                    effectChoiceMaterialized = true;
                }

                return pendingEffectChoice;
            }
        }
        public IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview => Detail == null
            ? Array.Empty<EffectChoiceCandidatePreview>()
            : Array.AsReadOnly(Detail.PendingEffectChoicePreview.Select(row =>
                new EffectChoiceCandidatePreview(
                    ToEffectValue(row.CandidateValue),
                    new HexCoordinate(row.Q, row.R),
                    ToBoardObject(row),
                    row.ResultingCoordinate.HasValue
                        ? (HexCoordinate?)ToCoordinate(row.ResultingCoordinate.Value)
                        : null,
                    (EffectChoiceConsequenceIntent)row.ConsequenceIntent)).ToArray());
        public PendingDisplacedMinionCorrection PendingDisplacedMinionCorrection
        {
            get
            {
                var row = Detail?.PendingDisplacedMinionCorrection;
                return row == null ? null : new PendingDisplacedMinionCorrection(
                    new ActionResolutionId(row.ResolutionId),
                    new CardInstanceId(row.SourceCardInstanceId),
                    new ActionDefinitionId(row.ActionDefinitionId),
                    new HeroId(row.ActorId),
                    (DisplacedMinionCorrectionStage)row.Stage,
                    new TeamId(row.ChoosingTeamId),
                    (TeamSide)row.ChoosingSide,
                    row.BattleZoneId == null ? default : new ZoneId(row.BattleZoneId),
                    row.SubjectCandidates.Select(value => new DisplacedMinionCorrectionSubject(
                        new MinionId(value.MinionId), new HexCoordinate(value.Q, value.R))),
                    row.SubjectMinionId == null ? default : new MinionId(row.SubjectMinionId),
                    (DisplacedMinionCorrectionKind)row.Kind,
                    row.DestinationCandidates.Select(value =>
                        new DisplacedMinionCorrectionCandidate(
                            new HexCoordinate(value.Q, value.R),
                            value.Route.Select(ToCoordinate), value.Cost)));
            }
        }

        public PendingMinionBattleChoice PendingMinionBattleChoice
        {
            get
            {
                var row = Detail?.PendingMinionBattleChoice;
                return row == null ? null : new PendingMinionBattleChoice(
                    new EndOfRoundResolutionId(row.ResolutionId),
                    new EndOfRoundChoiceId(row.ChoiceId),
                    new TeamId(row.LosingTeamId),
                    (TeamSide)row.ChoosingSide,
                    new ZoneId(row.BattleZoneId),
                    row.RequiredRemovalCount,
                    row.Candidates.Select(value =>
                        value.Unit.Kind == (int)Core.Rules.Modifications.RuleObjectKind.Hero
                            ? new MinionBattleCandidate(new HeroId(value.Unit.HeroId))
                            : new MinionBattleCandidate(
                                new MinionId(value.Unit.MinionId),
                                (StandardMinionType)value.MinionType)),
                    row.SelectedUnits.Select(ToUnit));
            }
        }

        public PendingHeroRespawnChoice PendingHeroRespawnChoice
        {
            get
            {
                var row = Detail?.PendingHeroRespawnChoice;
                return row == null ? null : new PendingHeroRespawnChoice(
                    new HeroId(row.HeroId),
                    new CardInstanceId(row.CardInstanceId),
                    row.Destinations.Select(value => new HeroRespawnDestination(
                        new HexCoordinate(value.Q, value.R),
                        new SpawnPointId(value.AdjacentSpawnPointId), value.IsFallback)));
            }
        }
        /// <summary>
        /// Milestone 9.21b. Re-derived rather than carried: the owning seat's contract names the hero,
        /// the card and each standing body, and the hero rows hold where each body stands - so the
        /// pick needs no projection row, and a seat that does not own it has no contract to derive from.
        /// </summary>
        public PendingActingBodyChoice PendingActingBodyChoice
        {
            get
            {
                var contract = DecisionContract;
                if (contract == null || !contract.FamilyId.Equals(PendingDecisionFamilies.ActingBody))
                {
                    return null;
                }

                var hero = contract.Commands[0].HeroId;
                var bodies = contract.Commands.Select(command => command.HeroBody).ToArray();
                return new PendingActingBodyChoice(
                    hero,
                    contract.Commands[0].CardInstanceId,
                    Heroes.Where(value => value.HeroId.Equals(hero) && bodies.Contains(value.Body)));
            }
        }

        /// <summary>
        /// Milestone 9.21i, carried at 9.21j (protocol /28): the settled acting body of each many-bodied
        /// hero, read off the hero row that carries it, in hero order - so a network client draws the
        /// acting cue where the host's engine says the body acts.
        /// </summary>
        public IReadOnlyList<HeroActingBody> ActingBodies => wire == null ? trusted.ActingBodies :
            Array.AsReadOnly(wire.Heroes.Where(row => row.Acting)
                .Select(row => new HeroActingBody(new HeroId(row.HeroId), row.Body))
                .OrderBy(value => value.HeroId.Value, StringComparer.Ordinal)
                .ToArray());

        public PendingSystemTransitionStep PendingSystemTransitionStep
        {
            get
            {
                var row = Detail?.PendingSystemTransitionStep;
                return row == null ? null : new PendingSystemTransitionStep(
                    new SystemTransitionInstanceId(row.InstanceId),
                    new SystemTransitionDefinitionId(row.DefinitionId),
                    new SystemTransitionStepId(row.StepId),
                    row.ItemId == null ? default : new SystemTransitionItemId(row.ItemId),
                    (TeamSide)row.ChoosingSide,
                    row.Options.Select(value => new SystemTransitionOptionId(value)),
                    row.CompletedStepIds.Select(value => new SystemTransitionStepId(value)),
                    row.CompletedItemIds.Select(value => new SystemTransitionItemId(value)));
            }
        }

        public SystemTransitionStepDescription PendingSystemTransitionDescription
        {
            get
            {
                var row = Detail?.PendingSystemTransitionDescription;
                var pending = PendingSystemTransitionStep;
                if (row == null || pending == null) return null;
                return new SystemTransitionStepDescription(
                    (SystemTransitionPromptKind)row.Kind,
                    pending,
                    row.HeroId == null ? default : new HeroId(row.HeroId),
                    row.Options.Select(value => ToTransitionOption(
                        (SystemTransitionPromptKind)row.Kind, value)),
                    row.Level, row.PaidCoins, row.RemainingCoins,
                    // Milestone 9.11 (wire /25): the host's own item, never guessed from the kind.
                    row.ItemId == null ? default : new SystemTransitionItemId(row.ItemId));
            }
        }
        public RemovalPreventionPrompt PendingRemovalPrevention
        {
            get
            {
                if (!removalPreventionMaterialized)
                {
                    pendingRemovalPrevention = BuildRemovalPrevention();
                    removalPreventionMaterialized = true;
                }

                return pendingRemovalPrevention;
            }
        }
        public IReadOnlyList<ActionOffer> AvailableTurnActions => Detail == null
            ? Array.Empty<ActionOffer>()
            : Array.AsReadOnly(Detail.AvailableTurnActions.Select(ToOffer).ToArray());
        public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
            Detail == null ? Array.Empty<FastTravelOptionProjection>() :
            Array.AsReadOnly(Detail.AvailableFastTravelOptions.Select(row =>
                new FastTravelOptionProjection(
                    ToOffer(row.ReplacementOffer),
                    new ActionDefinitionId(row.ReplacedMovementActionDefinitionId),
                    row.LegalDestinations.Select(ToCoordinate))).ToArray());
        public IReadOnlyList<ClearOptionProjection> AvailableClearOptions => Detail == null
            ? Array.Empty<ClearOptionProjection>()
            : Array.AsReadOnly(Detail.AvailableClearOptions.Select(row => new ClearOptionProjection(
                ToOffer(row.ReplacementOffer),
                new ActionDefinitionId(row.ReplacedAttackActionDefinitionId),
                row.AdjacentTokenInstanceIds.Select(id =>
                    PlacedTokens.Single(token => token.Id.Value == id)))).ToArray());
        public ActionOffer? AvailableHoldOffer => Detail?.AvailableHoldOffer == null
            ? (ActionOffer?)null
            : ToOffer(Detail.AvailableHoldOffer);

        private PendingEffectChoice BuildEffectChoice()
        {
            var row = Detail?.PendingEffectChoice;
            if (row == null)
            {
                return null;
            }

            // Milestone 8.35z5. A `may` node's Decline no longer follows from
            // `MinimumSelections` alone - the host derives it from the node's own `EffectFailurePolicy`,
            // which this client never loaded any content to read. Until 9.11 this passed
            // `row.HasDeclineInvocation` - the OFFER - as that flag, which reproduced the offer but
            // not the flag; since wire /25 the row carries the node's own
            // `DeclinableRegardlessOfMinimum`, and the offer bits are still checked against the typed
            // choice below as the wire's ground truth.
            var result = new PendingEffectChoice(
                new EffectResolutionId(row.ResolutionId),
                new EffectChoiceId(row.ChoiceId),
                new EffectNodeId(row.NodeId),
                new HeroId(row.ChooserId),
                (EffectChoiceKind)row.Kind,
                row.Candidates.Select(ToEffectCandidate),
                row.MinimumSelections,
                row.MaximumSelections,
                row.SelectedCandidates.Select(ToEffectCandidate),
                row.UnavailableCandidates.Select(ToEffectCandidate),
                row.HasStopInvocation,
                // Milestone 9.11 (wire /25): the node's own flag. Passing HasDeclineInvocation here
                // reproduced the offer but misreported the flag wherever a zero minimum is what
                // offers the Decline; the agreement check below still proves the offer matches.
                row.DeclinableRegardlessOfMinimum,
                (EffectChoiceValueVocabulary)row.ValueVocabulary);
            if ((result.ConfirmInvocation != null) != row.HasConfirmInvocation ||
                (result.DeclineInvocation != null) != row.HasDeclineInvocation ||
                (result.StopInvocation != null) != row.HasStopInvocation)
            {
                throw new InvalidOperationException(
                    "The effect-choice DTO invocation flags disagree with its typed choice bounds.");
            }

            return result;
        }

        private RemovalPreventionPrompt BuildRemovalPrevention()
        {
            var row = Detail?.PendingRemovalPrevention;
            if (row == null)
            {
                return null;
            }

            var hero = new HeroId(row.DecidingHeroId);
            return RemovalPreventionPrompt.FromProjection(
                row.RequestId,
                hero,
                new MinionId(row.SubjectMinionId),
                (RemovalPreventionOrigin)row.Origin,
                row.Payments.Select(value =>
                {
                    var card = new CardInstanceId(value.PaymentCardInstanceId);
                    var cost = Core.Rules.Modifications.RemovalPreventionCost.DiscardOwnedCard(
                        (CardLocation)value.CostFromLocation,
                        value.CostAdmittedColors.Select(color => (CardColor)color),
                        (CardClassification)value.CostAdmittedClassification);
                    return new RemovalPreventionPayment(
                        value.SourceId, cost, card,
                        RemovalPreventionCommand.Accept(
                            row.RequestId, hero, value.SourceId, card));
                }));
        }

        /// <summary>
        /// Milestone 8.35g. The finished turn awaiting its own player, rebuilt from what the seat
        /// record already carries rather than from a new wire row.
        /// </summary>
        /// <remarks>
        /// It is CARRIED rather than derived from the standing decision owner. Deriving it would
        /// have worked for the two facts a surface reads today - who the table is waiting on, and
        /// that it is waiting at all - but the wire-growth rule's exclusion floor is a deliberate
        /// ratchet, and an excused member is a fact a client cannot see. The pause is read by the
        /// shared status derivation every seat's top bar is drawn from, so it is a fact every client
        /// gets in full rather than one reconstructed from a neighbouring field.
        /// </remarks>
        public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease
        {
            get
            {
                var row = Detail?.PendingTurnRelease;
                if (!actionable || row == null)
                {
                    return null;
                }

                return new GuardsDigital.Core.Cards.PendingTurnRelease(
                    new HeroId(row.ActorId),
                    new CardInstanceId(row.CardInstanceId),
                    (TeamSide)row.TeamSide);
            }
        }

        private PendingDecisionContract BuildDecisionContract()
        {
            if (!actionable || wire?.DecisionContractFamilyId == null)
            {
                return null;
            }

            return new PendingDecisionContract(
                new PendingDecisionFamilyId(wire.DecisionContractFamilyId),
                wire.DecisionContractCommands.Select(CanonicalizeDecisionCommand));
        }

        private DirectorCommand CanonicalizeDecisionCommand(DirectorCommand command)
        {
            if (command.Kind == DirectorCommandKind.AnswerEffectChoice)
            {
                var choice = PendingEffectChoice ?? throw new InvalidOperationException(
                    "An effect DecisionContract requires its typed effect prompt.");
                var matches = choice.SelectInvocations
                    .Concat(choice.DeselectInvocations)
                    .Concat(Optional(choice.ConfirmInvocation))
                    .Concat(Optional(choice.DeclineInvocation))
                    .Concat(Optional(choice.StopInvocation))
                    .Where(invocation => invocation.Equals(command.EffectChoice))
                    .ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        "An effect DecisionContract command must match exactly one prompt invocation.");
                }

                return DirectorCommand.AnswerEffectChoice(matches[0]);
            }

            if (command.Kind == DirectorCommandKind.AnswerRemovalPrevention)
            {
                var prompt = PendingRemovalPrevention ?? throw new InvalidOperationException(
                    "A removal-prevention DecisionContract requires its typed prompt.");
                var matches = prompt.Payments.Select(payment => payment.Invocation)
                    .Concat(new[] { prompt.DeclineInvocation })
                    .Where(invocation => invocation.Equals(command.RemovalPrevention))
                    .ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        "A removal-prevention DecisionContract command must match exactly one prompt invocation.");
                }

                return DirectorCommand.AnswerRemovalPrevention(matches[0]);
            }

            return command;
        }

        private static IEnumerable<EffectChoiceCommand> Optional(EffectChoiceCommand invocation) =>
            invocation == null
                ? Enumerable.Empty<EffectChoiceCommand>()
                : new[] { invocation };

        /// <summary>
        /// Milestone 8.71a. The rewritten printed Initiative is carried into the reconstructed
        /// instance rather than applied afterwards, so this seat's rail reads the same number the
        /// host ordered the round by through the same one query
        /// (<c>PrintedValueQuery.CalculatePrintedInitiative</c>) - never a second client-side rule.
        /// </summary>
        private CardInstance ToCard(NetworkCardRow row) => new CardInstance(
            new CardInstanceId(row.InstanceId),
            new CardDefinitionReference(new CardDefinitionId(row.DefinitionId), row.ContentRevision),
            new HeroId(row.OwnerHeroId),
            new CardStateFacts((CardLocation)row.Location, row.HasBeenPlayed,
                (CardResolutionStatus)row.ResolutionStatus, (CardFaceOrientation)row.FaceOrientation),
            row.RewrittenInitiative >= 0 && !string.IsNullOrWhiteSpace(row.RewrittenInitiativeSource)
                ? new PrintedInitiativeOverride(
                    row.RewrittenInitiative, new CardInstanceId(row.RewrittenInitiativeSource))
                : PrintedInitiativeOverride.None);

        private RoundTurnSlot ToTurnSlot(NetworkTurnSlotRow row)
        {
            var assignments = row.Assignments.Select(value => new TurnCardAssignment(
                new HeroId(value.HeroId),
                value.CardInstanceId == null ? (CardInstanceId?)null :
                    new CardInstanceId(value.CardInstanceId),
                value.PassedWithEmptyHand,
                value.IsFacedown,
                value.KnownColor)).ToArray();
            var visible = Cards;
            var validationCards = assignments
                .Where(value => value.CardInstanceId.HasValue)
                .Select(value => value.CardInstanceId.Value)
                .Distinct()
                .Select(id => visible.FirstOrDefault(card => card.Id.Equals(id)) ??
                    trusted.Cards.Single(card => card.Id.Equals(id)))
                .ToArray();
            return new RoundTurnSlot(
                row.RoundIndex, row.TurnIndex, assignments, validationCards, Participants,
                (trusted as IModeTurnRosterReadModel)?.OptionalTurnHeroIds);
        }

        private PlacedToken ToToken(NetworkTokenRow row)
        {
            var reference = catalogs.Components.RegisteredTokens.Single(value =>
                value.DefinitionId.Value == row.TokenDefinitionId);
            return new PlacedToken(
                new TokenInstanceId(row.TokenInstanceId), catalogs.Components.LoadToken(reference),
                new HeroId(row.OwnerHeroId), new HeroId(row.ControllerHeroId),
                new CardInstanceId(row.SourceCardInstanceId), new HexCoordinate(row.Q, row.R));
        }

        private MarkerAttachment ToMarker(NetworkMarkerRow row)
        {
            var reference = catalogs.Components.RegisteredMarkers.Single(value =>
                value.DefinitionId.Value == row.MarkerDefinitionId);
            return new MarkerAttachment(
                new MarkerInstanceId(row.MarkerInstanceId), catalogs.Components.LoadMarker(reference),
                new HeroId(row.OwnerHeroId), new HeroId(row.HolderHeroId),
                new CardInstanceId(row.SourceCardInstanceId),
                new ActionDefinitionId(row.SourceActionDefinitionId),
                row.TurnSlotIndex == 0 ? (int?)null : row.TurnSlotIndex,
                row.ActiveUntilHolderNextAction);
        }

        private static HexCoordinate ToCoordinate(NetworkCoordinateRow row) =>
            new HexCoordinate(row.Q, row.R);

        private static AttackTarget ToAttackTarget(NetworkAttackTargetRow row) =>
            (AttackTargetKind)row.Kind == AttackTargetKind.Hero
                ? AttackTarget.HeroBodyOf(new HeroId(row.HeroId), row.HeroBody)
                : AttackTarget.Minion(new MinionId(row.MinionId));

        private static ActionOffer ToOffer(NetworkActionOfferRow row)
        {
            var transition = row.HasCardStateTransition
                ? new ActionCardStateTransition(
                    (CardLocation)row.RequiredLocation, (CardLocation)row.DestinationLocation)
                : default;
            return new ActionOffer(
                new CardInstanceId(row.SourceCardInstanceId),
                new CardDefinitionReference(
                    new CardDefinitionId(row.SourceDefinitionId), row.SourceContentRevision),
                new ActionDefinitionId(row.ActionDefinitionId),
                new HeroId(row.ActorId),
                (ActionInvocationContext)row.InvocationContext,
                row.IsAvailable,
                (AttackRangeKind)row.AttackRange,
                transition,
                // Milestone 8.35l. Rebuilt from the host's own answer rather than re-derived from the
                // standing effects this seat can see: a seat that worked out for itself why an action
                // is unavailable would be a second reading of the rules.
                row.UnavailabilityKind == (int)ActionUnavailabilityKind.BlockedByRuleModification
                    ? ActionUnavailability.BlockedBy(
                        (RuleActivity)row.BlockedActivity,
                        new HeroId(row.BlockingHeroId),
                        new CardInstanceId(row.BlockingCardInstanceId),
                        new ActiveEffectId(row.BlockingActiveEffectId))
                    : ActionUnavailability.None);
        }

        private static TeamAgreementProjection ToAgreement(
            NetworkTeamAgreementRow row,
            bool includeCommands,
            string viewerHeroId)
        {
            if (row == null) return null;
            return new TeamAgreementProjection(
                (TeamAgreementKind)row.Kind,
                new TeamAgreementProposalId(row.ProposalId),
                (TeamSide)row.ChoosingSide,
                row.Candidates.Select(value => new TeamAgreementCandidateProjection(
                    new CardInstanceId(value.CardInstanceId), new HeroId(value.HeroId))),
                row.Participants.Select(value => new TeamAgreementParticipantProjection(
                    new HeroId(value.HeroId), value.HasStatement,
                    value.PreferredCardInstanceId == null ? default :
                        new CardInstanceId(value.PreferredCardInstanceId),
                    value.Picks.Select(ToUnit),
                    value.PicksComplete)),
                row.RequiredSelectionCount,
                includeCommands
                    ? row.AuthorizedStatements
                    : Array.Empty<TeamAgreementCommand>(),
                string.IsNullOrWhiteSpace(viewerHeroId) ? default : new HeroId(viewerHeroId));
        }

        private static Core.Rules.Modifications.RuleObject ToUnit(NetworkUnitRow row) =>
            (Core.Rules.Modifications.RuleObjectKind)row.Kind ==
                Core.Rules.Modifications.RuleObjectKind.Hero
                ? Core.Rules.Modifications.RuleObject.ForHero(new HeroId(row.HeroId))
                : Core.Rules.Modifications.RuleObject.ForMinion(new MinionId(row.MinionId));

        private static EffectChoiceCandidate ToEffectCandidate(NetworkEffectCandidateRow row) =>
            (EffectChoiceCandidateKind)row.Kind == EffectChoiceCandidateKind.Option
                ? EffectChoiceCandidate.Option(new EffectNodeId(row.OptionNodeId))
                : EffectChoiceCandidate.Value_(ToEffectValue(row.Value));

        private static EffectValue ToEffectValue(NetworkEffectValueRow row)
        {
            switch ((EffectValueKind)row.Kind)
            {
                case EffectValueKind.Boolean: return EffectValue.Boolean(row.BooleanValue);
                case EffectValueKind.Integer: return EffectValue.Integer(row.IntegerValue);
                case EffectValueKind.Hero: return EffectValue.Hero(new HeroId(row.HeroId));
                case EffectValueKind.Minion: return EffectValue.Minion(new MinionId(row.MinionId));
                case EffectValueKind.CardInstance:
                    return EffectValue.Card(new CardInstanceId(row.CardInstanceId));
                case EffectValueKind.Coordinate:
                    return EffectValue.Space(new HexCoordinate(row.Q, row.R));
                case EffectValueKind.Node: return EffectValue.Node(new EffectNodeId(row.NodeId));
                default: throw new InvalidOperationException("The decoded effect value is not defined.");
            }
        }

        private static BoardObjectReference ToBoardObject(NetworkEffectChoicePreviewRow row)
        {
            switch ((BoardObjectKind)row.OccupantKind)
            {
                case BoardObjectKind.Hero:
                    return BoardObjectReference.ForHero(new HeroId(row.OccupantHeroId));
                case BoardObjectKind.Minion:
                    return BoardObjectReference.ForMinion(new MinionId(row.OccupantMinionId));
                case BoardObjectKind.Token:
                    return BoardObjectReference.ForToken(new TokenInstanceId(row.OccupantTokenInstanceId));
                default:
                    return default;
            }
        }

        private static ValueContributionProjection ToContribution(NetworkValueContributionRow row)
        {
            var kind = (ValueContributionKind)row.Kind;
            var channel = (PrintedValueChannel)row.Channel;
            ValueContribution value;
            switch (kind)
            {
                case ValueContributionKind.PrintedIcon:
                case ValueContributionKind.CardTextModifier:
                    value = row.SourceActionDefinitionId == null
                        ? new ValueContribution(kind, new CardInstanceId(row.SourceCardInstanceId),
                            channel, row.Amount)
                        : new ValueContribution(kind, new CardInstanceId(row.SourceCardInstanceId),
                            new ActionDefinitionId(row.SourceActionDefinitionId), channel, row.Amount);
                    break;
                case ValueContributionKind.ItemModifier:
                    value = new ValueContribution(kind, new CardInstanceId(row.SourceCardInstanceId),
                        channel, row.Amount);
                    break;
                case ValueContributionKind.MinionDefenseModifier:
                    value = new ValueContribution(kind, new MinionId(row.SourceMinionId),
                        channel, row.Amount);
                    break;
                case ValueContributionKind.ActiveEffectModifier:
                    value = new ValueContribution(kind, new ActiveEffectId(row.SourceActiveEffectId),
                        channel, row.Amount);
                    break;
                case ValueContributionKind.ContinuousSourceModifier:
                    value = ValueContribution.FromContinuousSource(
                        row.SourceContinuousSourceId, channel, row.Amount);
                    break;
                default:
                    throw new InvalidOperationException("The decoded contribution is not supported.");
            }

            return new ValueContributionProjection(value, row.SourceLabel);
        }

        private static SystemTransitionOptionDescription ToTransitionOption(
            SystemTransitionPromptKind kind, NetworkSystemTransitionOptionRow row)
        {
            var id = new SystemTransitionOptionId(row.OptionId);
            if (kind == SystemTransitionPromptKind.HeroPlacement)
                return SystemTransitionOptionDescription.PlaceHero(id,
                    new HexCoordinate(row.DestinationQ, row.DestinationR), new SpaceId(row.DestinationSpaceId));
            if (kind == SystemTransitionPromptKind.LanePushRespawnPlace)
            {
                return SystemTransitionOptionDescription.Place(
                    id, new HexCoordinate(row.DestinationQ, row.DestinationR),
                    new SpaceId(row.DestinationSpaceId));
            }

            return SystemTransitionOptionDescription.Upgrade(
                id,
                new CardInstanceId(row.UpgradedCardInstanceId),
                new CardDefinitionReference(
                    new CardDefinitionId(row.UpgradedDefinitionId), row.UpgradedContentRevision),
                new CardInstanceId(row.TakenCardInstanceId),
                new CardDefinitionReference(
                    new CardDefinitionId(row.TakenDefinitionId), row.TakenContentRevision),
                row.SetAsideCardInstanceId == null ? default :
                    new CardInstanceId(row.SetAsideCardInstanceId),
                row.SetAsideDefinitionId == null ? default : new CardDefinitionReference(
                    new CardDefinitionId(row.SetAsideDefinitionId), row.SetAsideContentRevision),
                (PrintedValueChannel)row.ItemIcon,
                row.ItemSlotIndex == 0 ? default : new HeroDashboardSlotPosition(row.ItemSlotIndex));
        }
    }
}
