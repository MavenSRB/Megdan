using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public sealed class MatchSession : IMatchSession
    {
        private readonly MatchSessionInitialState initialState;

        /// <summary>
        /// Internal rather than private so a fixture can assert what a live session's aggregate
        /// does. Constructing an aggregate directly covers its contract; this covers the one thing
        /// that needs a real session behind it - that real commands leave the streams uncopied.
        /// </summary>
        internal readonly MatchAggregate aggregate;
        private readonly DirectorCommandHistory history = new DirectorCommandHistory();
        private readonly bool deferAcceptedProjectionRefresh;

        /// <summary>
        /// Milestone 7.9b. Every this many accepted commands, the live session retains the
        /// aggregate's pre-image, so an Undo replays at most this many commands from the nearest
        /// retained point instead of the whole prefix - which is `F-ER2-08`'s fix. Retaining one
        /// costs three references: the pre-image is the memoized capture and the two stream
        /// snapshots, all of which already exist.
        /// </summary>
        internal const int UndoCheckpointInterval = 32;

        /// <summary>
        /// The retained points, ascending by command count. A derived cache in the acceptance
        /// checks' sense: each entry is a serialized description of a prefix state, never a second
        /// answer to what the match is - a reconstruction that starts from one deserializes it
        /// through the same restore path a rollback uses, and discarding them all would change the
        /// cost of the next Undo and nothing else. A snapshot restore discards them with the
        /// history; an adopted reconstruction truncates them to the surviving prefix.
        /// </summary>
        private readonly List<UndoCheckpoint> checkpoints = new List<UndoCheckpoint>();

        private long version;
        private bool canRecordHistory;
        private Exception nextProjectionFaultForTest;

        /// <summary>
        /// Every session verifies every refusal, in every build configuration. Milestone 6.1i made
        /// that verification optional through a diagnostics mode; the mode is gone, because the
        /// check is what delivers the aggregate's atomicity guarantee rather than a report about it.
        /// </summary>
        public MatchSession(MatchSessionInitialState initialState)
            : this(initialState, true, false)
        {
        }

        private MatchSession(
            MatchSessionInitialState initialState,
            bool useProvidedConfiguration,
            bool deferAcceptedProjectionRefresh)
        {
            this.initialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            this.deferAcceptedProjectionRefresh = deferAcceptedProjectionRefresh;
            aggregate = new MatchAggregate(this.initialState);
            RebuildInitialState(useProvidedConfiguration);
            Projection = CreateProjection();
        }

        // The authoritative graphs live in the aggregate; these read-only forwards keep their former
        // field names so that every one of the reads below is unchanged by the move, and the diff
        // that introduced the aggregate shows the structural change rather than a rename sweep. The
        // two rule-event streams deliberately get no forward: their few remaining reads name the
        // aggregate, because only its commit boundary may append to them.
        private MatchConfiguration configuration => aggregate.Configuration;
        private AdjacentMoveAuthority board => aggregate.Board;
        private RoundResolutionAuthority round => aggregate.Round;

        public MatchSessionProjection Projection { get; private set; }

        public MatchCommandResult Execute(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // The application transaction begins before the aggregate mutates. The aggregate owns
            // the authoritative graphs and event streams; this outer boundary additionally owns
            // accepted history, version/checkpoints, and publication of their one immutable view.
            var rollback = aggregate.CapturePreImage();
            var rollbackHistory = history.Commands;
            var rollbackVersion = version;
            var outcome = aggregate.Commit(() => Dispatch(command));
            if (outcome.Status == MatchCommitStatus.Faulted)
            {
                // A throw is a defect in this engine, not a rule saying no. Naming the exception
                // type puts that distinction in the log as well as in the reported reason, so a
                // NullReferenceException can never be read back as a rules outcome.
                return Rejected(
                    command,
                    MatchSessionRejectionReason.EngineFault,
                    $"{outcome.Fault.GetType().Name}: {outcome.Fault.Message}");
            }

            if (outcome.Status == MatchCommitStatus.AtomicityViolation)
            {
                return Rejected(
                    command,
                    MatchSessionRejectionReason.AtomicityViolation,
                    "A rejected command attempted to mutate authoritative state and was rolled back. " +
                    "It was refused as " + outcome.Plan.RejectionReason +
                    "/" + outcome.Plan.TurnRejectionReason +
                    (string.IsNullOrEmpty(outcome.Plan.Detail)
                        ? string.Empty
                        : ": " + outcome.Plan.Detail));
            }

            var dispatch = outcome.Plan;
            if (outcome.Status == MatchCommitStatus.Rejected)
            {
                return new MatchCommandResult(
                    false,
                    command,
                    dispatch.RejectionReason,
                    dispatch.Detail,
                    dispatch.RoundRejectionReason,
                    dispatch.CommitmentRejectionReason,
                    dispatch.TurnRejectionReason,
                    dispatch.CardActionRejectionReason,
                    Array.Empty<RoundResolutionEvent>(),
                    Projection);
            }

            try
            {
                if (canRecordHistory)
                {
                    history.Append(command);
                }

                version++;
                MatchSessionProjection nextProjection = Projection;
                UndoCheckpoint nextCheckpoint = null;
                if (!deferAcceptedProjectionRefresh)
                {
                    nextProjection = CreateProjection();
                    // After the refresh, so the pre-image reads the memoized capture the projection
                    // just took rather than serializing again. Replay candidates defer their
                    // projections and take no checkpoints: an adopted candidate's prefix states are
                    // already covered by the checkpoints the live session kept.
                    if (canRecordHistory && history.Count % UndoCheckpointInterval == 0)
                    {
                        nextCheckpoint = new UndoCheckpoint(
                            history.Count, aggregate.CapturePreImage());
                    }
                }

                if (nextCheckpoint != null)
                {
                    checkpoints.Add(nextCheckpoint);
                }

                Projection = nextProjection;
            }
            catch (Exception exception)
            {
                aggregate.Rollback(rollback);
                history.ReplaceWith(rollbackHistory);
                version = rollbackVersion;
                return Rejected(
                    command,
                    MatchSessionRejectionReason.EngineFault,
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            return new MatchCommandResult(
                true,
                command,
                MatchSessionRejectionReason.None,
                string.Empty,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
                CardActionResolutionRejectionReason.None,
                dispatch.Events,
                Projection);
        }

        public MatchSessionOperationResult Undo()
        {
            if (history.Count == 0)
            {
                return OperationRejected(MatchSessionRejectionReason.CommandUnavailable, "No accepted command is available to undo.");
            }

            return Reconstruct(history.WithoutLatest(), "Undo");
        }

        public MatchSessionOperationResult Restart()
        {
            if (history.Count == 0)
            {
                return OperationRejected(MatchSessionRejectionReason.CommandUnavailable, "No accepted command is available to restart.");
            }

            return Reconstruct(Array.Empty<DirectorCommand>(), "Restart");
        }

        /// <summary>
        /// Installs the state a snapshot describes, or refuses and leaves this session exactly as it
        /// was. There is no third outcome: a restore this session cannot complete never damages the
        /// match that was being played.
        /// </summary>
        /// <remarks>
        /// <para><b>Milestone 8.35z43: the rollback here REINSTATES rather than
        /// re-deserializes.</b> It used to carry a pre-image and rebuild from it, which meant the
        /// failure path parsed a payload - and a failed restore is exactly the situation in which the
        /// state being described may be one this build cannot read back. The second throw then escaped
        /// this method uncaught, leaving the session holding graphs from a half-finished restore.
        /// Milestone 8.35z23 recorded that after fixing the three causes that could reach it.</para>
        /// <para>Reinstatement is available here and nowhere else, which is why this is not a change
        /// to the aggregate's general rollback: a restore REPLACES the graphs rather than mutating
        /// them, so the ones it displaces are still intact and putting them back cannot fail. A
        /// command mutates in place, and for that a description really is all there is.</para>
        /// </remarks>
        public MatchSessionOperationResult RestoreSnapshot(string payload)
        {
            var reinstatement = aggregate.Capture();
            var rollbackHistory = history.Commands;
            var rollbackCheckpoints = checkpoints.ToArray();
            var rollbackCanRecordHistory = canRecordHistory;
            var rollbackVersion = version;
            try
            {
                // A snapshot restore begins a fresh round position, so it installs no prior round
                // events. A rollback goes through the aggregate instead, putting back the graphs it
                // was holding, because a refused operation must leave that stream as it found it.
                aggregate.Restore(payload);
            }
            catch (Exception exception)
            {
                aggregate.Adopt(reinstatement);
                return OperationRejected(MatchSessionRejectionReason.SnapshotRejected, exception.Message);
            }

            try
            {
                history.Clear();
                checkpoints.Clear();
                canRecordHistory = false;
                version++;
                var nextProjection = CreateProjection();
                Projection = nextProjection;
                return new MatchSessionOperationResult(
                    true,
                    MatchSessionRejectionReason.None,
                    string.Empty,
                    Projection);
            }
            catch (Exception exception)
            {
                aggregate.Adopt(reinstatement);
                history.ReplaceWith(rollbackHistory);
                checkpoints.Clear();
                checkpoints.AddRange(rollbackCheckpoints);
                canRecordHistory = rollbackCanRecordHistory;
                version = rollbackVersion;
                return OperationRejected(
                    MatchSessionRejectionReason.EngineFault,
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        public string CaptureSnapshot()
        {
            return aggregate.CaptureSnapshot();
        }

        /// <summary>
        /// Milestone 9.7. TEST ONLY: starts the named action even where the offer set greyed it for
        /// being unable to do anything, through the ordinary command path so the attempt is atomic and
        /// recorded like any other. See
        /// <see cref="TurnResolutionAuthority.SkipsFeasibilityJudgementForInternalTest"/>.
        /// </summary>
        internal MatchCommandResult StartActionIgnoringFeasibilityForInternalTest(
            CardInstanceId sourceCardInstanceId, ActionDefinitionId actionDefinitionId)
        {
            var turn = round.CurrentTurnAuthority;
            if (turn == null)
            {
                return Execute(DirectorCommand.StartAction(sourceCardInstanceId, actionDefinitionId));
            }

            turn.SkipsFeasibilityJudgementForInternalTest = true;
            try
            {
                return Execute(DirectorCommand.StartAction(sourceCardInstanceId, actionDefinitionId));
            }
            finally
            {
                turn.SkipsFeasibilityJudgementForInternalTest = false;
                if (round.CurrentTurnAuthority != null)
                {
                    round.CurrentTurnAuthority.SkipsFeasibilityJudgementForInternalTest = false;
                }
            }
        }

        private DispatchResult Dispatch(DirectorCommand command)
        {
            switch (command.Kind)
            {
                case DirectorCommandKind.CommitCard:
                    return DispatchResult.From(round.TryCommitCard(command.HeroId, command.CardInstanceId));
                case DirectorCommandKind.Pass:
                    return DispatchResult.From(round.TryPass(command.HeroId));
                case DirectorCommandKind.ChooseTiedActor:
                    return DispatchResult.From(round.TryChooseTiedActor(command.TeamSide, command.CardInstanceId));
                case DirectorCommandKind.PerformHold:
                    return DispatchResult.From(round.TryPerformHold(command.CardInstanceId));
                case DirectorCommandKind.StartAction:
                    return StartAction(command.CardInstanceId, command.ActionDefinitionId);
                case DirectorCommandKind.ChooseMovementDestination:
                    return AnswerMovement(command, false);
                case DirectorCommandKind.StopMovement:
                    return AnswerMovement(command, true);
                case DirectorCommandKind.ChooseAttackTarget:
                    return AnswerAttackTarget(command);
                case DirectorCommandKind.ChooseDefenseCard:
                    return AnswerDefense(command, false);
                case DirectorCommandKind.DeclineDefense:
                    return AnswerDefense(command, true);
                case DirectorCommandKind.AdvanceEndOfRound:
                    return DispatchResult.From(round.TryAdvanceEndOfRound());
                case DirectorCommandKind.ReleaseTurn:
                    return DispatchResult.From(round.TryReleaseTurn());
                case DirectorCommandKind.PerformFastTravel:
                    return PerformFastTravel(command.ActionDefinitionId, command.Coordinate);
                case DirectorCommandKind.PerformClear:
                    return PerformClear(command);
                case DirectorCommandKind.SelectMinionBattleRemoval:
                    return AnswerMinionBattle(command, MinionBattleChoiceCommandKind.Select);
                case DirectorCommandKind.DeselectMinionBattleRemoval:
                    return AnswerMinionBattle(command, MinionBattleChoiceCommandKind.Deselect);
                case DirectorCommandKind.ConfirmMinionBattleRemovals:
                    return AnswerMinionBattle(command, MinionBattleChoiceCommandKind.Confirm);
                case DirectorCommandKind.AnswerEffectChoice:
                    return AnswerEffectChoice(command);
                case DirectorCommandKind.AnswerRemovalPrevention:
                    return AnswerRemovalPrevention(command);
                case DirectorCommandKind.ChooseCorrectionSubject:
                    return AnswerCorrection(command,
                        GuardsDigital.Core.Rules.Displacement
                            .DisplacedMinionCorrectionCommandKind.SelectSubject);
                case DirectorCommandKind.ChooseCorrectionDestination:
                    return AnswerCorrection(command,
                        GuardsDigital.Core.Rules.Displacement
                            .DisplacedMinionCorrectionCommandKind.SelectDestination);
                case DirectorCommandKind.RespawnHero:
                    return DispatchResult.From(round.TryAnswerHeroRespawn(new HeroRespawnCommand(
                        HeroRespawnCommandKind.Respawn,
                        command.HeroId,
                        command.CardInstanceId,
                        command.Coordinate)));
                case DirectorCommandKind.ResolveCardWithoutRespawn:
                    return DispatchResult.From(round.TryAnswerHeroRespawn(new HeroRespawnCommand(
                        HeroRespawnCommandKind.ResolveWithoutAction,
                        command.HeroId,
                        command.CardInstanceId)));
                case DirectorCommandKind.BeginSystemTransition:
                    return DispatchResult.From(
                        round.TryBeginSystemTransition(command.SystemTransitionDefinitionId));
                case DirectorCommandKind.AnswerSystemTransitionStep:
                    return DispatchResult.From(
                        round.TryAnswerSystemTransitionStep(command.SystemTransitionChoice));
                default:
                    return DispatchResult.Unavailable("The command kind is not supported by this match session.");
            }
        }

        private DispatchResult StartAction(
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId)
        {
            var turn = round.CurrentTurnAuthority;
            if (turn == null || !turn.SelectedActor.IsDefined)
            {
                return DispatchResult.Unavailable("No current turn actor can start an action.");
            }

            // Milestone 8.29f. The identity guard every answer command here already carries, checked
            // when the caller supplied one: an action definition id is printed on every card in the
            // game, so a start command that named only one bound to whoever Initiative had selected
            // when it arrived. A player pressing a printed region on a card whose action has since
            // finished would have started the *next* hero's Movement.
            if (sourceCardInstanceId.IsDefined &&
                !sourceCardInstanceId.Equals(turn.SelectedActor.CardInstanceId))
            {
                return DispatchResult.Unavailable(
                    "The action's source card is stale or is not the card the turn selected.");
            }

            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    turn.SelectedActor.ActorId,
                    turn.SelectedActor.CardInstanceId)
                .Where(offer => offer.ActionDefinitionId.Equals(actionDefinitionId))
                .ToArray();
            if (offers.Length != 1)
            {
                return DispatchResult.Unavailable("The requested action is not one current authoritative offer.");
            }

            var offer = offers[0];
            var definition = configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(offer.SourceDefinitionReference));
            var action = definition.ActionDefinitions.Single(value => value.Id.Equals(actionDefinitionId));
            var resolutionId = new ActionResolutionId(
                $"session-r{round.RoundIndex}-t{round.TurnIndex}-{turn.SelectedActor.CardInstanceId.Value}-{actionDefinitionId.Value}");
            if (action.Kind == ActionKind.Movement)
            {
                return DispatchResult.From(round.TryStartMovement(resolutionId, offer));
            }

            if (action.Kind == ActionKind.Attack)
            {
                return DispatchResult.From(round.TryStartAttack(resolutionId, offer));
            }

            if (action.Kind == ActionKind.Skill)
            {
                return DispatchResult.From(round.TryStartSkill(resolutionId, offer));
            }

            return DispatchResult.Unavailable("The requested action kind is not supported by this session.");
        }

        private DispatchResult AnswerEffectChoice(DirectorCommand command)
        {
            var choice = round.CurrentTurnAuthority?.PendingEffectChoice;
            if (choice == null)
            {
                return DispatchResult.Unavailable("No effect choice is pending.");
            }

            if (command.EffectChoice == null ||
                !command.EffectChoice.ResolutionId.Equals(choice.ResolutionId) ||
                !command.EffectChoice.ChoiceId.Equals(choice.ChoiceId))
            {
                return DispatchResult.Unavailable(
                    "The effect choice identity is stale or does not match the pending choice.");
            }

            return DispatchResult.From(round.TryAnswerEffectChoice(command.EffectChoice));
        }

        /// <summary>
        /// Milestone 8.19c1. Closes the standing removal-prevention decision. The identity is checked
        /// here for the reason an effect choice's is: a stale answer arriving after the decision moved
        /// on is refused as stale rather than as a generic unavailable command.
        /// </summary>
        private DispatchResult AnswerRemovalPrevention(DirectorCommand command)
        {
            var standing = round.PendingRemovalPrevention;
            if (standing == null)
            {
                return DispatchResult.Unavailable("No removal-prevention decision is pending.");
            }

            if (command.RemovalPrevention == null ||
                !string.Equals(
                    command.RemovalPrevention.RequestId, standing.RequestId, StringComparison.Ordinal))
            {
                return DispatchResult.Unavailable(
                    "The removal-prevention identity is stale or does not match the pending decision.");
            }

            return DispatchResult.From(round.TryAnswerRemovalPrevention(command.RemovalPrevention));
        }

        private DispatchResult AnswerCorrection(
            DirectorCommand command,
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommandKind kind)
        {
            var pending = round.PendingDisplacedMinionCorrection;
            if (pending == null)
            {
                return DispatchResult.Unavailable("No displaced-minion correction is pending.");
            }

            if (!command.ResolutionId.Equals(pending.ResolutionId) ||
                !command.ChoiceId.Equals(pending.ChoiceId))
            {
                return DispatchResult.Unavailable(
                    "The correction identity is stale or does not match the pending choice.");
            }

            return DispatchResult.From(round.TryAnswerDisplacedMinionCorrection(
                new GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand(
                    kind,
                    command.ResolutionId,
                    pending.ChoiceId,
                    command.TeamSide,
                    command.MinionId,
                    command.Coordinate)));
        }

        private DispatchResult AnswerMovement(DirectorCommand command, bool stop)
        {
            var choice = round.CurrentTurnAuthority?.PendingMovementChoice;
            if (choice == null)
            {
                return DispatchResult.Unavailable("No Movement choice is pending.");
            }

            if ((command.ResolutionId.IsDefined && !command.ResolutionId.Equals(choice.ResolutionId)) ||
                (command.ChoiceId.IsDefined && !command.ChoiceId.Equals(choice.ChoiceId)))
            {
                return DispatchResult.Unavailable("The Movement choice identity is stale or does not match the pending choice.");
            }

            var movementCommand = stop
                ? MovementChoiceCommand.Stop(choice.ResolutionId, choice.ChoiceId)
                : MovementChoiceCommand.SelectDestination(choice.ResolutionId, choice.ChoiceId, command.Coordinate);
            return DispatchResult.From(round.TryAnswerMovementChoice(movementCommand));
        }

        private DispatchResult PerformFastTravel(
            ActionDefinitionId replacedMovementActionDefinitionId,
            GuardsDigital.Core.Board.HexCoordinate destination)
        {
            var turn = round.CurrentTurnAuthority;
            if (turn == null || !turn.SelectedActor.IsDefined)
            {
                return DispatchResult.Unavailable("No current turn actor can perform Fast Travel.");
            }

            var replacementOffer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                replacedMovementActionDefinitionId,
                turn.SelectedActor.CardInstanceId,
                turn.SelectedActor.ActorId,
                configuration.CardDefinitions,
                configuration.CardInstances,
                turn.CurrentModifications());
            if (!replacementOffer.HasValue)
            {
                return DispatchResult.Unavailable(
                    "The requested Movement action is not currently eligible for Fast Travel replacement.");
            }

            var resolutionId = new ActionResolutionId(
                $"session-r{round.RoundIndex}-t{round.TurnIndex}-{turn.SelectedActor.CardInstanceId.Value}-fast-travel-{replacedMovementActionDefinitionId.Value}");
            return DispatchResult.From(round.TryPerformFastTravel(
                resolutionId,
                replacementOffer.Value,
                replacedMovementActionDefinitionId,
                destination,
                board.Minions));
        }

        private DispatchResult PerformClear(DirectorCommand command)
        {
            var turn = round.CurrentTurnAuthority;
            if (turn == null || !turn.SelectedActor.IsDefined)
            {
                return DispatchResult.Unavailable("No current turn actor can perform Clear.");
            }

            var replacementOffer = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                command.ActionDefinitionId,
                turn.SelectedActor.CardInstanceId,
                turn.SelectedActor.ActorId,
                configuration.CardDefinitions,
                configuration.CardInstances,
                turn.CurrentModifications());
            if (!replacementOffer.HasValue)
            {
                return DispatchResult.Unavailable(
                    "The requested Attack action is not currently eligible for Clear replacement.");
            }

            var resolutionId = new ActionResolutionId(
                $"session-r{round.RoundIndex}-t{round.TurnIndex}-{turn.SelectedActor.CardInstanceId.Value}-clear-{command.ActionDefinitionId.Value}");
            return DispatchResult.From(round.TryPerformClear(
                resolutionId,
                replacementOffer.Value,
                command.ActionDefinitionId,
                command.TokenInstanceIds));
        }

        private DispatchResult AnswerAttackTarget(DirectorCommand command)
        {
            var choice = round.CurrentTurnAuthority?.PendingAttackTargetChoice;
            if (choice == null)
            {
                return DispatchResult.Unavailable("No Attack target choice is pending.");
            }

            if ((command.ResolutionId.IsDefined && !command.ResolutionId.Equals(choice.ResolutionId)) ||
                (command.ChoiceId.IsDefined && !command.ChoiceId.Equals(choice.ChoiceId)))
            {
                return DispatchResult.Unavailable("The Attack target choice identity is stale or does not match the pending choice.");
            }

            if (command.MinionId.IsDefined)
            {
                return DispatchResult.From(round.TryAnswerAttackTarget(
                    new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, command.MinionId)));
            }

            return DispatchResult.From(round.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, command.HeroId)));
        }

        private DispatchResult AnswerDefense(DirectorCommand command, bool decline)
        {
            var choice = round.CurrentTurnAuthority?.PendingDefenseChoice;
            if (choice == null)
            {
                return DispatchResult.Unavailable("No Defense response is pending.");
            }

            if ((command.ResolutionId.IsDefined && !command.ResolutionId.Equals(choice.ResolutionId)) ||
                (command.ChoiceId.IsDefined && !command.ChoiceId.Equals(choice.ChoiceId)))
            {
                return DispatchResult.Unavailable("The Defense choice identity is stale or does not match the pending choice.");
            }

            var defenseCommand = decline
                ? DefenseResponseChoiceCommand.Decline(choice.ResolutionId, choice.ChoiceId)
                : DefenseResponseChoiceCommand.SelectCards(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    command.CardInstanceId);
            return DispatchResult.From(round.TryAnswerDefenseResponse(defenseCommand));
        }

        private DispatchResult AnswerMinionBattle(
            DirectorCommand command,
            MinionBattleChoiceCommandKind kind)
        {
            var choice = round.PendingMinionBattleChoice;
            if (choice == null)
            {
                return DispatchResult.Unavailable("No Minion Battle removal choice is pending.");
            }

            var invocation = new MinionBattleChoiceCommand(
                kind,
                command.EndOfRoundResolutionId,
                command.EndOfRoundChoiceId,
                command.TeamSide,
                command.HeroId.IsDefined
                    ? RuleObject.ForHero(command.HeroId)
                    : command.MinionId.IsDefined
                        ? RuleObject.ForMinion(command.MinionId)
                        : RuleObject.None);
            return DispatchResult.From(round.TryAnswerMinionBattleChoice(invocation));
        }

        private MatchSessionOperationResult Reconstruct(
            IReadOnlyList<DirectorCommand> commands,
            string operation)
        {
            // Milestone 8.35z43. The reinstatement rollback, for the same reason
            // `RestoreSnapshot` uses one: `Adopt` REPLACES the graphs rather than mutating them, so
            // the ones it displaces are still intact and putting them back is an assignment that
            // cannot fail. Re-deserializing a pre-image here could, and an Undo whose rollback threw
            // would leave the session holding an adopted candidate it had just refused.
            var reinstatement = aggregate.Capture();
            // The history's snapshot survives any replacement, so holding it is the rollback copy.
            var rollbackHistory = history.Commands;
            var rollbackCheckpoints = checkpoints.ToArray();
            var oldVersion = version;
            ReplayCandidate first;
            try
            {
                // Milestone 7.9b: reconstruct from the latest retained point at or before the
                // surviving prefix, so the replay is bounded by the checkpoint interval instead of
                // by the match length. Both candidates start from the same retained point, so the
                // determinism comparison still proves the replayed segment reproduces itself.
                var checkpoint = LatestCheckpointCovering(commands.Count);
                first = Replay(commands, checkpoint);
                var second = Replay(commands, checkpoint);
                if (!string.Equals(first.Snapshot, second.Snapshot, StringComparison.Ordinal) ||
                    !first.Commands.SequenceEqual(second.Commands))
                {
                    throw new InvalidOperationException(
                        "Replaying the same initial scenario and command prefix produced different state, events, or accepted history.");
                }

            }
            catch (Exception exception)
            {
                return OperationRejected(
                    MatchSessionRejectionReason.ReplayFailed,
                    $"{operation} replay failed without changing the match: {exception.Message}");
            }

            try
            {
                Adopt(first);
                version = oldVersion + 1;
                var nextProjection = CreateProjection();
                checkpoints.RemoveAll(retained => retained.CommandCount > commands.Count);
                Projection = nextProjection;
                return new MatchSessionOperationResult(
                    true,
                    MatchSessionRejectionReason.None,
                    string.Empty,
                    Projection);
            }
            catch (Exception exception)
            {
                aggregate.Adopt(reinstatement);
                history.ReplaceWith(rollbackHistory);
                checkpoints.Clear();
                checkpoints.AddRange(rollbackCheckpoints);
                version = oldVersion;
                return OperationRejected(
                    MatchSessionRejectionReason.EngineFault,
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private UndoCheckpoint LatestCheckpointCovering(int commandCount)
        {
            for (var index = checkpoints.Count - 1; index >= 0; index--)
            {
                if (checkpoints[index].CommandCount <= commandCount)
                {
                    return checkpoints[index];
                }
            }

            return null;
        }

        private ReplayCandidate Replay(
            IReadOnlyList<DirectorCommand> commands,
            UndoCheckpoint checkpoint)
        {
            // Replay still traverses the normal typed Execute path and retains its atomic rollback
            // checks. Its intermediate immutable projections are private and unobserved, so defer
            // rebuilding them until the adopted live session publishes the final projection.
            var candidate = new MatchSession(initialState, false, true);
            var start = 0;
            if (checkpoint != null)
            {
                // The retained point is a serialized pre-image, so starting from one is the same
                // restore path a rollback takes: the graphs are rebuilt by deserializing, and both
                // event streams are installed exactly as they stood - which restore-and-continue
                // equivalence proves indistinguishable from having replayed to it.
                candidate.aggregate.Rollback(checkpoint.State);
                candidate.history.ReplaceWith(commands.Take(checkpoint.CommandCount));
                start = checkpoint.CommandCount;
            }

            for (var index = start; index < commands.Count; index++)
            {
                var result = candidate.Execute(commands[index]);
                if (!result.Accepted)
                {
                    throw new InvalidOperationException(
                        $"Accepted command {candidate.history.Count + 1} ({commands[index].Kind}) was rejected: {result.Detail}");
                }
            }

            return new ReplayCandidate(candidate);
        }

        private void Adopt(ReplayCandidate candidate)
        {
            aggregate.Adopt(candidate.Graphs);
            history.ReplaceWith(candidate.Commands);
        }

        private void RebuildInitialState(bool useProvidedConfiguration)
        {
            aggregate.RebuildInitialState(useProvidedConfiguration);
            history.Clear();
            canRecordHistory = true;
        }

        private MatchSessionProjection CreateProjection()
        {
            var turn = round.CurrentTurnAuthority;
            var commitment = round.CurrentCommitmentAuthority;
            var commitments = configuration.Participants.Heroes
                .OrderBy(hero => hero.Id.Value, StringComparer.Ordinal)
                .Select(hero => new MatchCommitmentProjection(
                    hero.Id,
                    commitment?.GetStatus(hero.Id) ?? TurnCardCommitmentStatus.AwaitingResponse,
                    commitment != null && commitment.CanPass(hero.Id),
                    commitment?.GetRevealedCardInstanceId(hero.Id)))
                .ToArray();
            // Milestone 6.5af: the turn authority's judged offer set rather than the card query's raw
            // one, so an action the rules can prove would do nothing arrives already marked. It is the
            // same judgement the start command is refused by, asked once.
            var availableActions = turn != null && turn.SelectedActor.IsDefined &&
                turn.PendingHeroRespawnChoice == null
                ? turn.GetTurnActionOffers()
                : Array.Empty<ActionOffer>();
            // The same modifications the turn authority resolves commands against, so an offer the
            // rules would refuse never appears in a projection as available.
            var turnModifications = turn?.CurrentModifications() ?? RuleModificationSet.Empty;
            var hasPendingAction = turn?.PendingChoice != null ||
                turn?.PendingHeroRespawnChoice != null ||
                turn?.PendingMovementChoice != null || turn?.PendingMovementResolution != null ||
                turn?.PendingAttackTargetChoice != null || turn?.PendingDefenseChoice != null ||
                turn?.PendingAttackResolution != null ||
                turn?.PendingSkillResolution != null || turn?.PendingEffectChoice != null;
            var availableFastTravelOptions = turn != null &&
                turn.SelectedActor.IsDefined && !hasPendingAction
                ? availableActions.Select(offer =>
                    {
                        var definition = configuration.CardDefinitions.Single(value =>
                            value.Reference.Equals(offer.SourceDefinitionReference));
                        var action = definition.ActionDefinitions.Single(value =>
                            value.Id.Equals(offer.ActionDefinitionId));
                        if (action.Kind != ActionKind.Movement || !action.DefaultPolicy.CanBeReplaced)
                        {
                            return null;
                        }

                        var replacementOffer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                            action.Id,
                            offer.SourceCardInstanceId,
                            offer.ActorId,
                            configuration.CardDefinitions,
                            configuration.CardInstances,
                            turnModifications);
                        if (!replacementOffer.HasValue)
                        {
                            return null;
                        }

                        var destinations = FastTravelActionQuery.FindLegalDestinations(
                            replacementOffer.Value,
                            action.Id,
                            configuration.CardDefinitions,
                            configuration.CardInstances,
                            board,
                            configuration.Participants,
                            board.Minions,
                            turnModifications);
                        return destinations.Count == 0
                            ? null
                            : new FastTravelOptionProjection(
                                replacementOffer.Value,
                                action.Id,
                                destinations);
                    })
                    .Where(option => option != null)
                    .ToArray()
                : Array.Empty<FastTravelOptionProjection>();
            // Milestone 6.3a: the same shape one line up, over the other replaceable action. It asks
            // the two queries the `PerformClear` dispatch already asks - which Clear offer stands, and
            // which adjacent tokens it admits - so the surface and the command agree by construction
            // rather than by both being written to agree. An option with no adjacent token is kept:
            // Milestone 8.7. p.12's Hold, decided here rather than by whatever draws it. The offer now
            // consults the rules in effect, and two derivations of one rules answer is exactly the
            // shape the 7.9a review found as F-PRES-01 - a control drawn live beside a command that
            // refuses it.
            var availableHoldOffer = turn != null && turn.SelectedActor.IsDefined
                ? GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                    turn.SelectedActor.CardInstanceId,
                    turn.SelectedActor.ActorId,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    turnModifications)
                : null;

            // p.13 permits clearing none, and milestone 6.3 accepts the empty selection, so an
            // available replaceable Attack always offers the replacement.
            // Milestone 7.8a1: the Attack's own availability is deliberately not consulted here. A
            // Clear replaces an Attack, but its legality is its own - rulebook p.13 lets a Clear take
            // any number of adjacent tokens including none - so an Attack that currently has nobody to
            // target is still an Attack a Clear may replace, and dropping the replacement with it would
            // take away a legal action for a reason that is not about it. `GetAvailableClearOffer`
            // re-derives from the card's own state and answers null when the action is not offered at
            // all, which is the question that does belong here.
            var availableClearOptions = turn != null &&
                turn.SelectedActor.IsDefined && !hasPendingAction
                ? availableActions.Select(offer =>
                    {
                        var definition = configuration.CardDefinitions.Single(value =>
                            value.Reference.Equals(offer.SourceDefinitionReference));
                        var action = definition.ActionDefinitions.Single(value =>
                            value.Id.Equals(offer.ActionDefinitionId));
                        if (action.Kind != ActionKind.Attack || !action.DefaultPolicy.CanBeReplaced)
                        {
                            return null;
                        }

                        var replacementOffer = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                            action.Id,
                            offer.SourceCardInstanceId,
                            offer.ActorId,
                            configuration.CardDefinitions,
                            configuration.CardInstances,
                            turnModifications);
                        if (!replacementOffer.HasValue)
                        {
                            return null;
                        }

                        var legalTokenIds = ClearActionQuery.FindLegalTokenIds(
                            replacementOffer.Value,
                            action.Id,
                            configuration.CardDefinitions,
                            configuration.CardInstances,
                            board,
                            turnModifications);
                        return new ClearOptionProjection(
                            replacementOffer.Value,
                            action.Id,
                            legalTokenIds.Select(id =>
                                board.PlacedTokens.Single(token => token.Id.Equals(id))));
                    })
                    .Where(option => option != null)
                    .ToArray()
                : Array.Empty<ClearOptionProjection>();
            var defensePreview = CreateDefensePreview(
                turn, turnModifications, round.HeroItems, round.Markers);
            var effectChoicePreview = turn?.DescribePendingEffectChoice(
                    configuration.CardDefinitions, turnModifications) ??
                Array.Empty<EffectChoiceCandidatePreview>();
            var attackTargetDefensePreviews = CreateAttackTargetDefensePreviews(
                turn, effectChoicePreview);
            // Reads the aggregate's memo rather than re-serializing state the previous command
            // already captured. Neither the payload string nor the diagnostic envelope is built
            // here at all: the projection assembles each on demand from the same immutable capture,
            // so a reader pays for what it reads and a command does not.
            var authoritative = aggregate.CaptureAuthoritative();
            // Milestone 9.10. The board's units and tokens are projected in the order the snapshot
            // writes them - by id - rather than the board's own setup-and-arrival order, which a
            // restore, a replay from a retained point and a reconnecting seat cannot reproduce. The
            // projection is a reading of the authoritative state, so it may not depend on history the
            // state does not keep.
            var projection = new MatchSessionProjection(
                version,
                initialState.Setup,
                board.MapDefinition,
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                board.Occupancies.OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal),
                board.ObstacleOccupancies.OrderBy(obstacle => obstacle.ObstacleId.Value, StringComparer.Ordinal),
                round.RoundIndex,
                round.TurnIndex,
                round.Phase,
                round.TieBreakerState,
                round.TurnSlots,
                round.CompletedRounds,
                round.CurrentEndOfRoundStage,
                commitments,
                turn?.SelectedActor ?? default,
                turn?.PendingChoice,
                turn?.IsStarted ?? false,
                turn?.IsEnded ?? false,
                turn?.PendingMovementResolution,
                turn?.PendingMovementChoice,
                turn?.PendingAttackResolution,
                turn?.PendingAttackTargetChoice,
                turn?.PendingDefenseChoice,
                availableActions,
                history.Commands,
                aggregate.RoundEvents,
                aggregate.EventLogEntries,
                authoritative,
                canRecordHistory,
                board.Minions.OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal),
                board.PlacedTokens.OrderBy(token => token.Id.Value, StringComparer.Ordinal),
                availableHoldOffer,
                availableFastTravelOptions,
                defensePreview,
                round.PendingMinionBattleChoice,
                turn?.PendingHeroRespawnChoice,
                round.PendingLanePushRequest,
                turn?.PendingSkillResolution,
                turn?.PendingEffectChoice,
                round.PendingDisplacedMinionCorrection,
                effectChoicePreview,
                availableClearOptions,
                turn?.PendingImmediateLanePush,
                round.SystemTransitions.PendingStep,
                round.TeamLives.Counters,
                round.Outcome,
                CreateHeroProgression(round),
                // Milestone 7.8a. The pause and what it means are read in the same breath, from the same
                // authority, so a reader can never hold a description of one pause beside another one.
                round.SystemTransitions.DescribePendingSteps(),
                CreateActiveEffectSourceCards(round),
                // Milestone 8.21. The same pairing the effect choice has above: the pause and the
                // affordable answers to it are read in one breath, from the authority that holds both.
                turn?.DescribeRemovalPrevention(turnModifications),
                // Milestone 8.23a. The register itself, not a copy of what it said: the same object
                // p.8's cleanup and p.13's Defeat return markers to, so a reading of it can never
                // outlive the attachment it describes. Milestone 9.10: in the snapshot's id order, for
                // the reason the board lists above are.
                round?.Markers.Attachments.OrderBy(marker => marker.Id.Value, StringComparer.Ordinal),
                attackTargetDefensePreviews,
                // Milestone 8.35g. The finished turn waiting for its own player's release, read
                // from the turn authority that holds it beside every other pause it publishes.
                turn?.PendingTurnRelease,
                // Milestone 8.35z60. p.8's live Wave-counter pool, read off the same
                // Lane-Push authority that flips it - the top status strip's WAVES indicator binds
                // this rather than the setup's starting total, which never moves.
                round.LanePush?.LanePush.RemainingWaveCounters,
                // Milestone 8.53. The victory screen's board, read off the authority that
                // accumulates it at the commit boundary - beside the Levels above, because the
                // screen prints the two together.
                round.Scoreboard.Entries);
            if (nextProjectionFaultForTest != null)
            {
                var fault = nextProjectionFaultForTest;
                nextProjectionFaultForTest = null;
                throw fault;
            }

            return projection;
        }

        /// <summary>
        /// One-shot friend-assembly seam for proving the application transaction. It is deliberately
        /// internal, instance-scoped, and exception-only: no runtime mode or public construction path
        /// can suppress projection validation or alter a successful projection.
        /// </summary>
        internal void FaultNextProjectionForTest(Exception fault)
        {
            nextProjectionFaultForTest = fault ?? throw new ArgumentNullException(nameof(fault));
        }

        /// <summary>
        /// Milestone 8.35z43. Makes the next pre-image this session's aggregate captures
        /// report an unreadable payload, so a fixture can state what a rollback does when the state it
        /// would have re-deserialized cannot be read back.
        /// </summary>
        /// <remarks>
        /// The property under test is that <see cref="RestoreSnapshot"/> no longer cares: it rolls
        /// back by reinstating the graphs it was holding, so nothing on its failure path asks the
        /// serializer to read anything. The seam is one-shot and internal on the same terms as
        /// <see cref="FaultNextProjectionForTest"/>.
        /// </remarks>
        internal void CorruptNextPreImagePayloadForTest(string payload)
        {
            aggregate.CorruptNextPreImagePayloadForTest(payload);
        }

        /// <summary>
        /// Milestone 8.5b. Which card instances currently source a live Active effect, read off the
        /// round authority's own lifecycle.
        ///
        /// <para>The round holds the lifecycle and hands the same object to each turn, so this is the
        /// one place the answer exists whether or not a turn is running - which matters, because an
        /// effect that outlives the turn that made it is exactly the kind a player needs to see. Only
        /// <see cref="GuardsDigital.Core.Timing.ActiveEffectPhase.Active"/> counts: a dormant instance has not begun and an ended
        /// one is over. The list is distinct and ordered by instance id, so one card appears once
        /// however many of its instances are live and the sequence is a fact rather than an iteration
        /// accident.</para>
        /// </summary>
        private static IReadOnlyList<CardInstanceId> CreateActiveEffectSourceCards(
            RoundResolutionAuthority round)
        {
            if (round == null)
            {
                return Array.Empty<CardInstanceId>();
            }

            return Array.AsReadOnly(round.Timing.Effects
                .Where(effect => effect.Phase == GuardsDigital.Core.Timing.ActiveEffectPhase.Active)
                .Select(effect => effect.SourceCardInstanceId)
                .Distinct()
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// Milestone 7.8a. Every hero's Level, coins and items as the round authority currently holds
        /// them, ordered by hero id so the sequence is a fact rather than an iteration accident.
        ///
        /// Items are reduced to a count per icon here rather than in presentation, because "how many
        /// items raise Attack" is a question about authoritative item state and its answer must not
        /// depend on who asked. Nothing about p.16's composition into a card's total happens here.
        /// </summary>
        private static IReadOnlyList<HeroProgressionProjection> CreateHeroProgression(
            RoundResolutionAuthority round)
        {
            if (round == null)
            {
                return Array.Empty<HeroProgressionProjection>();
            }

            var coins = round.CoinBalances;
            return Array.AsReadOnly(round.HeroProgression.Levels
                .Select(entry => new HeroProgressionProjection(
                    entry.Key,
                    entry.Value.Value,
                    coins.FirstOrDefault(balance => balance.Key.Equals(entry.Key)).Value,
                    round.RoundLeveling.HasLeveled(entry.Key),
                    round.HeroItems.GetItems(entry.Key)
                        .GroupBy(item => item.Icon)
                        .OrderBy(group => (int)group.Key)
                        .Select(group => new KeyValuePair<PrintedValueChannel, int>(
                            group.Key, group.Count()))))
                .ToArray());
        }

        /// <summary>
        /// Milestone 8.40g: the marker register arrives beside the item state, for the reason that one
        /// does - a preview that consulted a different copy of the board's markers than the resolution
        /// does would predict a Block the Attack then refuses, which is the disagreement milestone
        /// 8.11a exists to prevent. It is the round's own register, the same object the dispatcher
        /// hands the Attack backend, so the two cannot drift apart.
        /// </summary>
        private PendingDefensePreviewProjection CreateDefensePreview(
            TurnResolutionAuthority turn,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            GuardsDigital.Core.Components.MarkerAttachmentRegister markers)
        {
            var choice = turn?.PendingDefenseChoice;
            var attack = turn?.PendingAttackResolution;
            if (choice == null || attack == null ||
                attack.Phase != AttackResolutionPhase.DefenseResponse || attack.AttackValue == null)
            {
                return null;
            }

            var minionContributions = DefenseValueQuery.CalculateMinionContributions(
                choice.ChooserId,
                configuration.Participants,
                board);
            var projectedChoices = new List<DefenseChoicePreviewProjection>();
            foreach (var offer in choice.LegalDefenseOffers)
            {
                // Milestone 8.11a. A Block is previewed as a Block. Milestone 8.11 taught the resolver
                // that p.14's condition replaces the Defense comparison, but left this preview
                // calculating a Defense value for every offer - so a Block previewed as a card worth
                // zero, predicted the defender losing an attack it in fact stops, and gave the prompt
                // no printed Defense icon to draw itself from.
                // Milestone 8.57e. The attacker and the live set arrive beside the register, for the
                // reason the register arrived beside the item state: the attacking card's printed
                // classification is no longer the whole of p.13's answer - Rowenna's Fabled Lance
                // makes every Attack of hers Ranged - and a preview reading the card alone while the
                // resolution reads the declaration would predict the wrong verdict for the one card a
                // defender is choosing from.
                // Milestone 8.61b. The board, the items and the capability catalog arrive beside the
                // register for the reason the register arrived beside the item state: Min the
                // Dragonmonk's three Block faces resolve by whether her own Smoke bomb stands inside
                // the card's printed Range, and the piece that condition asks about is named by the
                // very capability her Defense text's mandatory swap consumes. All three are the
                // configured match's own objects - the same board the dispatcher mutates, the same item
                // state the round holds, the same catalog every backend was composed against - so the
                // preview and the resolution cannot read a different board, a different item or a
                // different piece.
                var blockResult = DefenseBlockQuery.Evaluate(
                    offer,
                    configuration.CardDefinitions,
                    attack.SourceDefinitionReference,
                    attack.ActionDefinitionId,
                    markers,
                    attack.ActorId,
                    modifications,
                    board,
                    heroItems,
                    configuration.ContentCatalogs.Capabilities,
                    round.TurnIndex);
                if (blockResult != DefenseBlockResult.None)
                {
                    projectedChoices.Add(new DefenseChoicePreviewProjection(
                        offer.SourceCardInstanceId,
                        GetCardDisplayName(offer.SourceCardInstanceId),
                        Array.Empty<ValueContributionProjection>(),
                        null,
                        blockResult,
                        AttackOutcomeComparison.Create(
                            attack.AttackTotal, null, blockResult).Outcome));
                    continue;
                }

                var defense = DefenseValueQuery.Calculate(
                    offer,
                    configuration.CardDefinitions,
                    choice.ChooserId,
                    configuration.Participants,
                    board,
                    modifications,
                    heroItems);
                var comparison = AttackOutcomeComparison.Create(
                    attack.AttackTotal,
                    defense.Total,
                    DefenseBlockResult.None);
                projectedChoices.Add(new DefenseChoicePreviewProjection(
                    offer.SourceCardInstanceId,
                    GetCardDisplayName(offer.SourceCardInstanceId),
                    defense.Contributions.Select(ProjectContribution),
                    defense.Total,
                    comparison.BlockResult,
                    comparison.Outcome,
                    // Milestone 8.20a: the offer's own declaration, so the preview says whether an
                    // absent minion modifier is the card's text or a dropped contribution.
                    SuppressesMinionDefenseModifiers(offer)));
            }

            if (choice.CanDecline)
            {
                var comparison = AttackOutcomeComparison.Create(
                    attack.AttackTotal,
                    null,
                    DefenseBlockResult.None);
                projectedChoices.Add(new DefenseChoicePreviewProjection(
                    null,
                    "Decline Defense",
                    Array.Empty<ValueContributionProjection>(),
                    null,
                    comparison.BlockResult,
                    comparison.Outcome));
            }

            return new PendingDefensePreviewProjection(
                choice.ResolutionId,
                choice.ChoiceId,
                attack.AttackValue.Contributions.Select(ProjectContribution),
                attack.AttackTotal,
                projectedChoices,
                minionContributions.Select(ProjectContribution));
        }

        private IReadOnlyList<AttackTargetDefensePreviewProjection>
            CreateAttackTargetDefensePreviews(
                TurnResolutionAuthority turn,
                IReadOnlyList<EffectChoiceCandidatePreview> effectChoicePreviews)
        {
            var heroIds = new List<HeroId>();
            var attackChoice = turn?.PendingAttackTargetChoice;
            if (attackChoice != null)
            {
                heroIds.AddRange(attackChoice.LegalTargetHeroIds);
            }

            var effectChoice = turn?.PendingEffectChoice;
            if (effectChoice != null)
            {
                heroIds.AddRange((effectChoicePreviews ??
                        throw new ArgumentNullException(nameof(effectChoicePreviews)))
                    .Where(preview =>
                        preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Attack &&
                        preview.Occupant.Kind == BoardObjectKind.Hero &&
                        effectChoice.SelectInvocations.Any(invocation =>
                            invocation.Candidate.Equals(
                                EffectChoiceCandidate.Value_(preview.CandidateValue))))
                    .Select(preview => preview.Occupant.HeroId));
            }

            return Array.AsReadOnly(heroIds.Distinct()
                .Select(heroId => new AttackTargetDefensePreviewProjection(
                    heroId,
                    DefenseValueQuery.CalculateMinionContributions(
                            heroId,
                            configuration.Participants,
                            board)
                        .Select(ProjectContribution)))
                .ToArray());
        }

        private ValueContributionProjection ProjectContribution(ValueContribution contribution)
        {
            string label;
            if (contribution.Kind == ValueContributionKind.ItemModifier &&
                contribution.SourceCardInstanceId.IsDefined &&
                configuration.CardInstances.All(value => !value.Id.Equals(contribution.SourceCardInstanceId)))
            {
                // Milestone 9.13. p.16's "use any spare card from the box" item (Dodger's Darkest
                // Ritual with his Ultimate) names a box identity, not a dealt card; the lookup below
                // threw on it, so the first Attack or Defense preview after the grant faulted.
                label = $"{contribution.Channel} item from the box";
            }
            else if (contribution.SourceCardInstanceId.IsDefined)
            {
                label = GetCardDisplayName(contribution.SourceCardInstanceId);
            }
            else if (contribution.SourceMinionId.IsDefined)
            {
                var minion = board.Minions.Single(value =>
                    value.MinionId.Equals(contribution.SourceMinionId));
                label = $"{minion.TeamSide} {minion.MinionType} minion ({minion.MinionId.Value})";
            }
            else if (contribution.SourceActiveEffectId.IsDefined)
            {
                label = $"Active effect {contribution.SourceActiveEffectId.Value}";
            }
            else if (!string.IsNullOrEmpty(contribution.SourceContinuousSourceId))
            {
                // Milestone 7.9. The fourth typed source rulebook p.19 keeps apart from Active effects:
                // a placed token's property today, an unlocked Ultimate since milestone 7.3, a marker
                // later. Before this branch existed, every one of them reached the `else` and threw, so
                // a Defense preview for the hero controlling a placed Hazard - which grants exactly this
                // kind of contribution - made the match unplayable rather than merely unlabelled. The
                // complete-loop driver found it by playing; no earlier fixture had ever attacked a hero
                // who controlled a token.
                //
                // The id is read off the contribution rather than resolved back to its source, exactly
                // as the Active-effect branch above reads its id. A lookup would need both the board's
                // token property sources and the unlocked-Ultimate set, which would make this projection
                // the place that knows what kinds of continuous source exist - and a fifth kind would
                // then throw here again for the same reason this one did.
                label = $"Continuous source {contribution.SourceContinuousSourceId}";
            }
            else
            {
                throw new InvalidOperationException("A value contribution has no readable typed source.");
            }

            return new ValueContributionProjection(contribution, label);
        }

        /// <summary>
        /// Milestone 8.20a. Whether this Defense offer's printed action declares p.9's item I - "Ignore
        /// all minion defense modifiers", which Arien's three Duelist cards print and
        /// <c>MinionDefenseContributionPolicy.SuppressAll</c> states.
        ///
        /// <para>The preview needs it because an absent minion contribution has two meanings, and only
        /// the card distinguishes them. The definition is found the way every other reading here finds
        /// one - through the configuration's own definition list - rather than through Core's internal
        /// offer helpers, which this assembly cannot see.</para>
        /// </summary>
        private bool SuppressesMinionDefenseModifiers(ActionOffer offer) =>
            configuration.CardDefinitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .MinionDefenseContributions == MinionDefenseContributionPolicy.SuppressAll;

        private string GetCardDisplayName(CardInstanceId cardId)
        {
            var card = configuration.CardInstances.Single(value => value.Id.Equals(cardId));
            return configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).DisplayName;
        }

        private MatchCommandResult Rejected(
            DirectorCommand command,
            MatchSessionRejectionReason reason,
            string detail)
        {
            return new MatchCommandResult(
                false,
                command,
                reason,
                detail,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
                CardActionResolutionRejectionReason.None,
                Array.Empty<RoundResolutionEvent>(),
                Projection);
        }

        private MatchSessionOperationResult OperationRejected(
            MatchSessionRejectionReason reason,
            string detail)
        {
            return new MatchSessionOperationResult(false, reason, detail, Projection);
        }

        private sealed class DispatchResult : IMatchCommitPlan
        {
            private DispatchResult(
                bool accepted,
                MatchSessionRejectionReason rejectionReason,
                string detail,
                RoundResolutionRejectionReason roundRejectionReason,
                TurnCardCommitmentRejectionReason commitmentRejectionReason,
                TurnResolutionRejectionReason turnRejectionReason,
                CardActionResolutionRejectionReason cardActionRejectionReason,
                IReadOnlyList<RoundResolutionEvent> events)
            {
                Accepted = accepted;
                RejectionReason = rejectionReason;
                Detail = detail;
                RoundRejectionReason = roundRejectionReason;
                CommitmentRejectionReason = commitmentRejectionReason;
                TurnRejectionReason = turnRejectionReason;
                CardActionRejectionReason = cardActionRejectionReason;
                Events = events;
            }

            public bool Accepted { get; }
            public MatchSessionRejectionReason RejectionReason { get; }
            public string Detail { get; }
            public RoundResolutionRejectionReason RoundRejectionReason { get; }
            public TurnCardCommitmentRejectionReason CommitmentRejectionReason { get; }
            public TurnResolutionRejectionReason TurnRejectionReason { get; }
            public CardActionResolutionRejectionReason CardActionRejectionReason { get; }
            public IReadOnlyList<RoundResolutionEvent> Events { get; }

            public static DispatchResult From(RoundResolutionResult result)
            {
                return new DispatchResult(
                    result.Accepted,
                    result.Accepted ? MatchSessionRejectionReason.None : MatchSessionRejectionReason.DomainRejected,
                    result.Accepted
                        ? string.Empty
                        : FormatRejection(result),
                    result.RejectionReason,
                    result.CommitmentRejectionReason,
                    result.TurnRejectionReason,
                    result.CardActionRejectionReason,
                    result.Events);
            }

            public static DispatchResult Unavailable(string detail)
            {
                return new DispatchResult(
                    false,
                    MatchSessionRejectionReason.CommandUnavailable,
                    detail,
                    RoundResolutionRejectionReason.None,
                    TurnCardCommitmentRejectionReason.None,
                    TurnResolutionRejectionReason.None,
                    CardActionResolutionRejectionReason.None,
                    Array.Empty<RoundResolutionEvent>());
            }

            private static string FormatRejection(RoundResolutionResult result)
            {
                if (result.CommitmentRejectionReason != TurnCardCommitmentRejectionReason.None)
                {
                    return $"{result.RejectionReason} ({result.CommitmentRejectionReason})";
                }

                if (result.TurnRejectionReason != TurnResolutionRejectionReason.None)
                {
                    // Milestone 8.25a. The backend's own reason, where it gave one. It is appended
                    // rather than substituted because the two say different things - which command was
                    // refused, and what the rules said about it - and a reader that lost either would
                    // be back where the diagnosis of that milestone's stall started.
                    return result.CardActionRejectionReason == CardActionResolutionRejectionReason.None
                        ? $"{result.RejectionReason} ({result.TurnRejectionReason})"
                        : $"{result.RejectionReason} ({result.TurnRejectionReason}: " +
                            $"{result.CardActionRejectionReason})";
                }

                return result.RejectionReason.ToString();
            }
        }

        /// <summary>One retained point: the state after its first <see cref="CommandCount"/> accepted commands.</summary>
        private sealed class UndoCheckpoint
        {
            public UndoCheckpoint(int commandCount, MatchPreImage state)
            {
                CommandCount = commandCount;
                State = state ?? throw new ArgumentNullException(nameof(state));
            }

            public int CommandCount { get; }
            public MatchPreImage State { get; }
        }

        private sealed class ReplayCandidate
        {
            public ReplayCandidate(MatchSession source)
            {
                Graphs = source.aggregate.Capture();
                Commands = source.history.Commands;
                Snapshot = source.CaptureSnapshot();
            }

            public MatchGraphSet Graphs { get; }
            public IReadOnlyList<DirectorCommand> Commands { get; }
            public string Snapshot { get; }
        }
    }
}
