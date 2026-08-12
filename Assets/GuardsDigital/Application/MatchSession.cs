using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;

namespace GuardsDigital.Application
{
    public sealed class MatchSession : IMatchSession
    {
        private readonly MatchSessionInitialState initialState;
        private readonly DirectorCommandHistory history = new DirectorCommandHistory();
        private readonly RuleEventLogProjection eventLog = new RuleEventLogProjection();
        private readonly List<RoundResolutionEvent> roundEvents = new List<RoundResolutionEvent>();
        private readonly bool deferAcceptedProjectionRefresh;

        private PlaceholderOneVsOneTestConfiguration configuration;
        private AdjacentMoveAuthority board;
        private CardActionResolutionAuthority cardActions;
        private RoundResolutionAuthority round;
        private long version;
        private bool canRecordHistory;

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
            RebuildInitialState(useProvidedConfiguration);
            Projection = CreateProjection();
        }

        public MatchSessionProjection Projection { get; private set; }

        public MatchCommandResult Execute(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var before = CaptureSnapshot();
            DispatchResult dispatch;
            try
            {
                dispatch = Dispatch(command);
            }
            catch (Exception exception)
            {
                RestoreInternal(before, false);
                Projection = CreateProjection();
                return Rejected(command, MatchSessionRejectionReason.DomainRejected, exception.Message);
            }

            if (!dispatch.Accepted)
            {
                var after = CaptureSnapshot();
                if (!string.Equals(before, after, StringComparison.Ordinal))
                {
                    RestoreInternal(before, false);
                    Projection = CreateProjection();
                    return Rejected(
                        command,
                        MatchSessionRejectionReason.AtomicityViolation,
                        "A rejected command attempted to mutate authoritative state and was rolled back.");
                }

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

            roundEvents.AddRange(dispatch.Events);
            eventLog.Append(dispatch.Events);
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
            var rollback = CaptureSnapshot();
            try
            {
                RestoreInternal(payload, true);
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
                RestoreInternal(rollback, false);
                Projection = CreateProjection();
                return OperationRejected(MatchSessionRejectionReason.SnapshotRejected, exception.Message);
            }
        }

        public string CaptureSnapshot()
        {
            var authoritative = AuthoritativeSnapshotSerializer.Serialize(round, board);
            return RuleEventLogSnapshotEnvelope.Serialize(authoritative, eventLog.Entries);
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

            return DispatchResult.Unavailable("The requested action kind is not supported by this session.");
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
                configuration.CardInstances);
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

        private MatchSessionOperationResult Reconstruct(
            IReadOnlyList<DirectorCommand> commands,
            string operation)
        {
            var rollback = CaptureSnapshot();
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
                RestoreInternal(rollback, false);
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
            configuration = candidate.Configuration;
            board = candidate.Board;
            cardActions = candidate.CardActions;
            round = candidate.Round;
            history.ReplaceWith(candidate.Commands);
            eventLog.Restore(candidate.EventLogEntries);
            roundEvents.Clear();
            roundEvents.AddRange(candidate.RoundEvents);
        }

        private void RebuildInitialState(bool useProvidedConfiguration)
        {
            configuration = useProvidedConfiguration && initialState.InitialConfiguration != null
                ? initialState.InitialConfiguration
                : initialState.Scenario.CreateConfiguration();
            board = initialState.Scenario.CreateBoard(initialState.AvailableMaps, initialState.Setup.Minions);
            cardActions = new CardActionResolutionAuthority(board);
            round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                initialState.Scenario.InitialTieBreakerState,
                cardActions: cardActions,
                timing: initialState.CreateTiming());
            history.Clear();
            canRecordHistory = true;
            roundEvents.Clear();
            eventLog.ResetToSetup(configuration.Participants, initialState.Scenario.InitialTieBreakerState);
        }

        private void RestoreInternal(string payload, bool clearHistory)
        {
            var hasEnvelope = RuleEventLogSnapshotEnvelope.TryDeserialize(payload, out var envelope);
            var authoritativePayload = hasEnvelope ? envelope.AuthoritativePayload : payload;
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                authoritativePayload,
                initialState.Scenario.CardDefinitions,
                initialState.AvailableMaps,
                initialState.Setup.Minions);

            if (!restored.Board.SelectedMap.Equals(initialState.Setup.SelectedMap))
            {
                throw new InvalidOperationException("The restored snapshot does not match the configured setup map.");
            }

            configuration = PlaceholderOneVsOneTestConfiguration.Restore(
                restored.Participants,
                initialState.Scenario.CardDefinitions,
                restored.Cards,
                restored.Round.TieBreakerState);
            board = restored.Board;
            cardActions = restored.CardActions;
            round = restored.Round;
            roundEvents.Clear();
            if (hasEnvelope)
            {
                eventLog.Restore(envelope.Entries);
            }
            else
            {
                eventLog.ResetToSetup(restored.Participants, restored.Round.TieBreakerState);
            }

            if (clearHistory)
            {
                history.Clear();
            }
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
            var availableActions = turn != null && turn.SelectedActor.IsDefined
                ? ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    turn.SelectedActor.ActorId,
                    turn.SelectedActor.CardInstanceId)
                : Array.Empty<ActionOffer>();
            var hasPendingAction = turn?.PendingChoice != null ||
                turn?.PendingMovementChoice != null || turn?.PendingMovementResolution != null ||
                turn?.PendingAttackTargetChoice != null || turn?.PendingDefenseChoice != null ||
                turn?.PendingAttackResolution != null;
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
                            configuration.CardInstances);
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
                            board.Minions);
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
            var defensePreview = CreateDefensePreview(turn);
            var authoritative = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var diagnostic = RuleEventLogSnapshotEnvelope.Serialize(authoritative, eventLog.Entries);
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
                roundEvents,
                eventLog.Entries,
                authoritative,
                diagnostic,
                availableFastTravelOptions,
                board.Minions,
                defensePreview);
        }

        private PendingDefensePreviewProjection CreateDefensePreview(TurnResolutionAuthority turn)
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
                    board);
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

        private sealed class DispatchResult
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
                Configuration = source.configuration;
                Board = source.board;
                CardActions = source.cardActions;
                Round = source.round;
                Commands = source.history.Commands.ToArray();
                EventLogEntries = source.eventLog.Entries.ToArray();
                RoundEvents = source.roundEvents.ToArray();
                Snapshot = source.CaptureSnapshot();
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority CardActions { get; }
            public RoundResolutionAuthority Round { get; }
            public IReadOnlyList<DirectorCommand> Commands { get; }
            public IReadOnlyList<RuleEventLogEntry> EventLogEntries { get; }
            public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }
            public string Snapshot { get; }
        }
    }
}
