using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;

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

        private long version;
        private bool canRecordHistory;

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
        private PlaceholderOneVsOneTestConfiguration configuration => aggregate.Configuration;
        private AdjacentMoveAuthority board => aggregate.Board;
        private RoundResolutionAuthority round => aggregate.Round;

        public MatchSessionProjection Projection { get; private set; }

        public MatchCommandResult Execute(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // One boundary decides whether this command's state and its ordered events are kept.
            var outcome = aggregate.Commit(() => Dispatch(command));
            if (outcome.Status == MatchCommitStatus.Faulted)
            {
                // A throw is a defect in this engine, not a rule saying no. Naming the exception
                // type puts that distinction in the log as well as in the reported reason, so a
                // NullReferenceException can never be read back as a rules outcome.
                Projection = CreateProjection();
                return Rejected(
                    command,
                    MatchSessionRejectionReason.EngineFault,
                    $"{outcome.Fault.GetType().Name}: {outcome.Fault.Message}");
            }

            if (outcome.Status == MatchCommitStatus.AtomicityViolation)
            {
                Projection = CreateProjection();
                return Rejected(
                    command,
                    MatchSessionRejectionReason.AtomicityViolation,
                    "A rejected command attempted to mutate authoritative state and was rolled back.");
            }

            var dispatch = outcome.Plan;
            if (outcome.Status == MatchCommitStatus.Rejected)
            {
                Projection = CreateProjection();
                return new MatchCommandResult(
                    false,
                    command,
                    dispatch.RejectionReason,
                    dispatch.Detail,
                    dispatch.RoundRejectionReason,
                    dispatch.CommitmentRejectionReason,
                    dispatch.TurnRejectionReason,
                    Array.Empty<RoundResolutionEvent>(),
                    Projection);
            }

            if (canRecordHistory)
            {
                history.Append(command);
            }
            version++;
            if (!deferAcceptedProjectionRefresh)
            {
                Projection = CreateProjection();
            }
            return new MatchCommandResult(
                true,
                command,
                MatchSessionRejectionReason.None,
                string.Empty,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
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

        public MatchSessionOperationResult RestoreSnapshot(string payload)
        {
            var rollback = aggregate.CapturePreImage();
            try
            {
                // A snapshot restore begins a fresh round position, so it installs no prior round
                // events. A rollback goes through the aggregate instead, carrying the pre-image it
                // captured, because a refused operation must leave that stream as it found it.
                aggregate.Restore(payload);
                history.Clear();
                canRecordHistory = false;
                version++;
                Projection = CreateProjection();
                return new MatchSessionOperationResult(
                    true,
                    MatchSessionRejectionReason.None,
                    string.Empty,
                    Projection);
            }
            catch (Exception exception)
            {
                aggregate.Rollback(rollback);
                Projection = CreateProjection();
                return OperationRejected(MatchSessionRejectionReason.SnapshotRejected, exception.Message);
            }
        }

        public string CaptureSnapshot()
        {
            return aggregate.CaptureSnapshot();
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
                    return StartAction(command.ActionDefinitionId);
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

        private DispatchResult StartAction(ActionDefinitionId actionDefinitionId)
        {
            var turn = round.CurrentTurnAuthority;
            if (turn == null || !turn.SelectedActor.IsDefined)
            {
                return DispatchResult.Unavailable("No current turn actor can start an action.");
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
                configuration.CardInstances);
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
            var rollback = aggregate.CapturePreImage();
            var rollbackHistory = history.Commands.ToArray();
            var oldVersion = version;
            try
            {
                var first = Replay(commands);
                var second = Replay(commands);
                if (!string.Equals(first.Snapshot, second.Snapshot, StringComparison.Ordinal) ||
                    !first.Commands.SequenceEqual(second.Commands))
                {
                    throw new InvalidOperationException(
                        "Replaying the same initial scenario and command prefix produced different state, events, or accepted history.");
                }

                Adopt(first);
                version = oldVersion + 1;
                Projection = CreateProjection();
                return new MatchSessionOperationResult(
                    true,
                    MatchSessionRejectionReason.None,
                    string.Empty,
                    Projection);
            }
            catch (Exception exception)
            {
                aggregate.Rollback(rollback);
                history.ReplaceWith(rollbackHistory);
                version = oldVersion;
                Projection = CreateProjection();
                return OperationRejected(
                    MatchSessionRejectionReason.ReplayFailed,
                    $"{operation} replay failed without changing the match: {exception.Message}");
            }
        }

        private ReplayCandidate Replay(IEnumerable<DirectorCommand> commands)
        {
            // Replay still traverses the normal typed Execute path and retains its atomic rollback
            // checks. Its intermediate immutable projections are private and unobserved, so defer
            // rebuilding them until the adopted live session publishes the final projection.
            var candidate = new MatchSession(initialState, false, true);
            foreach (var command in commands)
            {
                var result = candidate.Execute(command);
                if (!result.Accepted)
                {
                    throw new InvalidOperationException(
                        $"Accepted command {candidate.history.Count + 1} ({command.Kind}) was rejected: {result.Detail}");
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
            // p.13 permits clearing none, and milestone 6.3 accepts the empty selection, so an
            // available replaceable Attack always offers the replacement.
            var availableClearOptions = turn != null &&
                turn.SelectedActor.IsDefined && !hasPendingAction
                ? availableActions.Where(offer => offer.IsAvailable).Select(offer =>
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
                            configuration.CardInstances);
                        if (!replacementOffer.HasValue)
                        {
                            return null;
                        }

                        var legalTokenIds = ClearActionQuery.FindLegalTokenIds(
                            replacementOffer.Value,
                            action.Id,
                            configuration.CardDefinitions,
                            configuration.CardInstances,
                            board);
                        return new ClearOptionProjection(
                            replacementOffer.Value,
                            action.Id,
                            legalTokenIds.Select(id =>
                                board.PlacedTokens.Single(token => token.Id.Equals(id))));
                    })
                    .Where(option => option != null)
                    .ToArray()
                : Array.Empty<ClearOptionProjection>();
            var defensePreview = CreateDefensePreview(turn, turnModifications, round.HeroItems);
            // Reads the aggregate's memo rather than re-serializing state the previous command
            // already encoded. The diagnostic envelope is not built here at all: the projection
            // computes it on demand from these same immutable inputs, so a reader pays for it and a
            // command does not.
            var authoritative = aggregate.CaptureAuthoritativeSnapshot();
            return new MatchSessionProjection(
                version,
                initialState.Setup,
                board.MapDefinition,
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                board.Occupancies,
                board.ObstacleOccupancies,
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
                board.Minions,
                board.PlacedTokens,
                availableFastTravelOptions,
                defensePreview,
                round.PendingMinionBattleChoice,
                turn?.PendingHeroRespawnChoice,
                round.PendingLanePushRequest,
                turn?.PendingSkillResolution,
                turn?.PendingEffectChoice,
                round.PendingDisplacedMinionCorrection,
                turn?.DescribePendingEffectChoice(configuration.CardDefinitions, turnModifications),
                availableClearOptions,
                turn?.PendingImmediateLanePush,
                round.SystemTransitions.PendingStep,
                round.TeamLives.Counters);
        }

        private PendingDefensePreviewProjection CreateDefensePreview(
            TurnResolutionAuthority turn,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
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
                    comparison.Outcome));
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

        private ValueContributionProjection ProjectContribution(ValueContribution contribution)
        {
            string label;
            if (contribution.SourceCardInstanceId.IsDefined)
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
            else
            {
                throw new InvalidOperationException("A value contribution has no readable typed source.");
            }

            return new ValueContributionProjection(contribution, label);
        }

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
                IReadOnlyList<RoundResolutionEvent> events)
            {
                Accepted = accepted;
                RejectionReason = rejectionReason;
                Detail = detail;
                RoundRejectionReason = roundRejectionReason;
                CommitmentRejectionReason = commitmentRejectionReason;
                TurnRejectionReason = turnRejectionReason;
                Events = events;
            }

            public bool Accepted { get; }
            public MatchSessionRejectionReason RejectionReason { get; }
            public string Detail { get; }
            public RoundResolutionRejectionReason RoundRejectionReason { get; }
            public TurnCardCommitmentRejectionReason CommitmentRejectionReason { get; }
            public TurnResolutionRejectionReason TurnRejectionReason { get; }
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
                    return $"{result.RejectionReason} ({result.TurnRejectionReason})";
                }

                return result.RejectionReason.ToString();
            }
        }

        private sealed class ReplayCandidate
        {
            public ReplayCandidate(MatchSession source)
            {
                Graphs = source.aggregate.Capture();
                Commands = source.history.Commands.ToArray();
                Snapshot = source.CaptureSnapshot();
            }

            public MatchGraphSet Graphs { get; }
            public IReadOnlyList<DirectorCommand> Commands { get; }
            public string Snapshot { get; }
        }
    }
}
