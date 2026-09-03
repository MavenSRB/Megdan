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
    public sealed class NetworkMatchClient : IMatchClient
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

        public MatchClientResult Submit(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
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
            }

            return projection;
        }

        /// <summary>
        /// The local HUD immediately forwards Confirm after a Select fills an effect choice. A
        /// network submission returns before the host's result and projection exist, so polling the
        /// adapter completes the same two-command continuation only after both authoritative facts
        /// arrive. The continuation is cleared before sending, making repeated renders idempotent.
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
                choice.ConfirmInvocation == null)
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
    public sealed class NetworkMatchReadModel : IMatchReadModel
    {
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

            wire = accepted;
            actionable = nextActionable;
            effectChoiceMaterialized = false;
            pendingEffectChoice = null;
            removalPreventionMaterialized = false;
            pendingRemovalPrevention = null;
            decisionContractMaterialized = false;
            decisionContract = null;
        }

        public long Version => wire?.Version ?? trusted.Version;
        public bool IsInputEnabled => actionable;

        /// <summary>
        /// Milestone 8.35f. The host's own answer, carried on the seat record, and withheld while
        /// this view is not actionable - a waiting, stale, resynchronizing or disconnected client
        /// may not mint a rewind any more than it may mint a command.
        /// </summary>
        public bool IsSeatUndoAvailable => actionable && (wire?.IsSeatUndoAvailable ?? false);
        public QuickGameSetupState Setup => trusted.Setup;
        public BoardMapDefinition SelectedMap => trusted.SelectedMap;
        public MatchParticipants Participants => trusted.Participants;
        public IReadOnlyList<CardDefinition> Definitions => trusted.Definitions;

        public IReadOnlyList<HeroOccupancy> Heroes => wire == null ? trusted.Heroes :
            Array.AsReadOnly(wire.Heroes.Select(row =>
                new HeroOccupancy(new HeroId(row.HeroId), new HexCoordinate(row.Q, row.R))).ToArray());

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
                    new CardInstanceId(row.RevealedCardInstanceId))).ToArray());

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

        public IReadOnlyList<HeroProgressionProjection> HeroProgression => wire == null
            ? trusted.HeroProgression
            : Array.AsReadOnly(wire.HeroProgression.Select(row => new HeroProgressionProjection(
                new HeroId(row.HeroId), row.Level, row.Coins, row.LeveledThisRound,
                row.ItemBonuses.Select(value => new KeyValuePair<PrintedValueChannel, int>(
                    (PrintedValueChannel)value.Key, value.Value)))).ToArray());

        public MatchOutcome Outcome => wire?.Outcome == null || wire.Outcome.Cause == 0
            ? MatchOutcome.Unfinished
            : MatchOutcome.Won((MatchVictoryCause)wire.Outcome.Cause,
                new TeamId(wire.Outcome.WinningTeamId), new TeamId(wire.Outcome.LosingTeamId));

        public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds => wire == null
            ? trusted.ActiveEffectSourceCardInstanceIds
            : Array.AsReadOnly(wire.ActiveEffectSourceCardInstanceIds.Select(
                value => new CardInstanceId(value)).ToArray());

        public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
            ActiveEffectSourceCardInstanceIds.Contains(cardInstanceId);

        public IReadOnlyList<CardInstance> Cards => wire == null ? trusted.Cards :
            Array.AsReadOnly(wire.OwnCards.Concat(wire.PublicCards).Select(ToCard).ToArray());

        private NetworkProjectionDetailRecord PublicDetail => wire?.Detail;
        private NetworkProjectionDetailRecord Detail => actionable ? PublicDetail : null;

        public TeamAgreementProjection TeamAgreement =>
            ToAgreement(PublicDetail?.TeamAgreement, actionable);
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
                    new HeroId(row.HeroId), row.MinionContributions.Select(ToContribution))).ToArray());
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
                    row.Level, row.PaidCoins, row.RemainingCoins);
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
                row.HasStopInvocation);
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

        private CardInstance ToCard(NetworkCardRow row) => new CardInstance(
            new CardInstanceId(row.InstanceId),
            new CardDefinitionReference(new CardDefinitionId(row.DefinitionId), row.ContentRevision),
            new HeroId(row.OwnerHeroId),
            new CardStateFacts((CardLocation)row.Location, row.HasBeenPlayed,
                (CardResolutionStatus)row.ResolutionStatus, (CardFaceOrientation)row.FaceOrientation));

        private RoundTurnSlot ToTurnSlot(NetworkTurnSlotRow row)
        {
            var assignments = row.Assignments.Select(value => new TurnCardAssignment(
                new HeroId(value.HeroId),
                value.CardInstanceId == null ? (CardInstanceId?)null :
                    new CardInstanceId(value.CardInstanceId),
                value.PassedWithEmptyHand)).ToArray();
            var visible = Cards;
            var validationCards = assignments
                .Where(value => value.CardInstanceId.HasValue)
                .Select(value => value.CardInstanceId.Value)
                .Distinct()
                .Select(id => visible.FirstOrDefault(card => card.Id.Equals(id)) ??
                    trusted.Cards.Single(card => card.Id.Equals(id)))
                .ToArray();
            return new RoundTurnSlot(
                row.RoundIndex, row.TurnIndex, assignments, validationCards, Participants);
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
                new ActionDefinitionId(row.SourceActionDefinitionId));
        }

        private static HexCoordinate ToCoordinate(NetworkCoordinateRow row) =>
            new HexCoordinate(row.Q, row.R);

        private static AttackTarget ToAttackTarget(NetworkAttackTargetRow row) =>
            (AttackTargetKind)row.Kind == AttackTargetKind.Hero
                ? AttackTarget.Hero(new HeroId(row.HeroId))
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
                transition);
        }

        private static TeamAgreementProjection ToAgreement(
            NetworkTeamAgreementRow row,
            bool includeCommands)
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
                        new CardInstanceId(value.PreferredCardInstanceId), value.Agreed)),
                row.SelectedUnits.Select(ToUnit),
                row.RequiredSelectionCount,
                row.SelectionIsConfirmable,
                includeCommands
                    ? row.AuthorizedStatements
                    : Array.Empty<TeamAgreementCommand>());
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
