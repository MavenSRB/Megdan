using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionAuthority
    {
        private readonly MovementCardActionStrategyBackend movementBackend;
        private readonly AttackDefenseCardActionStrategyBackend attackDefenseBackend;
        private readonly FastTravelCardActionStrategyBackend fastTravelBackend;
        private readonly ClearCardActionStrategyBackend clearBackend;
        private readonly SkillCardActionStrategyBackend skillBackend;
        private readonly MatchComponents components;
        private readonly AdjacentMoveAuthority board;

        public CardActionResolutionAuthority(AdjacentMoveAuthority adjacentMoveAuthority)
            : this(adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, null)
        {
        }

        public CardActionResolutionAuthority(
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions)
            : this(adjacentMoveAuthority, movementActionStartRestrictions, null)
        {
        }

        /// <summary>
        /// The configured match's shape: default movement restrictions, plus the marker register the
        /// round authority also holds. The default evaluator is internal to this assembly, so the
        /// application layer composes a match through this overload rather than naming it.
        /// </summary>
        public CardActionResolutionAuthority(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers)
            : this(adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, markers)
        {
        }

        public CardActionResolutionAuthority(
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions,
            MarkerAttachmentRegister markers)
            : this(
                new MovementCardActionStrategyBackend(
                    adjacentMoveAuthority,
                    movementActionStartRestrictions ?? throw new ArgumentNullException(
                        nameof(movementActionStartRestrictions))),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                new SkillCardActionStrategyBackend(SyntheticEffectCapabilities.CreateCatalog()),
                CreateComponents(adjacentMoveAuthority, markers),
                adjacentMoveAuthority)
        {
        }

        private PendingDisplacedMinionCorrection pendingCorrection;

        private CardActionResolutionAuthority(
            MovementCardActionStrategyBackend movementBackend,
            AttackDefenseCardActionStrategyBackend attackDefenseBackend,
            FastTravelCardActionStrategyBackend fastTravelBackend,
            ClearCardActionStrategyBackend clearBackend,
            SkillCardActionStrategyBackend skillBackend,
            MatchComponents components,
            AdjacentMoveAuthority board)
        {
            this.movementBackend = movementBackend ?? throw new ArgumentNullException(nameof(movementBackend));
            this.attackDefenseBackend = attackDefenseBackend ?? throw new ArgumentNullException(nameof(attackDefenseBackend));
            this.fastTravelBackend = fastTravelBackend ?? throw new ArgumentNullException(nameof(fastTravelBackend));
            this.clearBackend = clearBackend ?? throw new ArgumentNullException(nameof(clearBackend));
            this.skillBackend = skillBackend ?? throw new ArgumentNullException(nameof(skillBackend));
            this.components = components ?? throw new ArgumentNullException(nameof(components));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>
        /// The component collaborators a Skill's capabilities may reach. The board doubles as the token
        /// placement port, because tokens are board occupants and the board owns the one occupancy index
        /// milestone 6.1r established, and since milestone 6.4 it is also the displacement port, since
        /// milestone 6.5 the unit Spawn port, and since milestone 6.6 the exchange port, for the same
        /// reason: every one of them mutates the
        /// same occupancy, and that is the same authority's business. They stay named ports on one
        /// object rather than one wide one, because placing from a supply, displacing something already
        /// placed, and taking a miniature out of the supply are different rules questions. The marker
        /// register arrives from the caller rather than being created here whenever a live match is
        /// composing, so that the register this dispatcher hands to capabilities and the register the
        /// round authority persists are the same object.
        /// </summary>
        private static MatchComponents CreateComponents(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                adjacentMoveAuthority,
                markers ?? new MarkerAttachmentRegister(),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                adjacentMoveAuthority);

        /// <summary>
        /// The same collaborators, for the snapshot reader that has to re-ask a paused capability what
        /// it offered. Exposed rather than duplicated so a restored choice is derived against exactly
        /// the composition a live one was.
        /// </summary>
        internal static MatchComponents CreateRestoredComponents(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            CreateComponents(adjacentMoveAuthority, markers);

        public PendingCardActionResolution PendingResolution => movementBackend.PendingResolution;

        public PendingMovementChoice PendingChoice => movementBackend.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => attackDefenseBackend.PendingResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => attackDefenseBackend.PendingTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => attackDefenseBackend.PendingDefenseChoice;

        public DefeatRewardAccounting DefeatRewards => attackDefenseBackend.DefeatRewards;

        internal AdjacentMoveAuthority Board => board;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            skillBackend.PendingResolution;

        public GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice PendingEffectChoice =>
            skillBackend.PendingChoice;

        /// <summary>
        /// Milestone 6.5c: what each space-valued candidate of the pending effect choice currently
        /// means on the board. This dispatcher answers it because it is the one component holding both
        /// the paused frame and the component composition capabilities execute against, so the preview
        /// a reader sees is derived from exactly the state the next command would resolve against.
        /// Empty whenever no effect choice is pending or none of its candidates name a space.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            DescribePendingEffectChoice(
                IEnumerable<CardDefinition> definitions,
                RuleModificationSet modifications)
        {
            var pending = skillBackend.PendingResolution;
            var choice = skillBackend.PendingChoice;
            if (pending == null || choice == null)
            {
                return Array.Empty<
                    GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>();
            }

            var materialized = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            return GuardsDigital.Core.Rules.EffectResolution.EffectChoicePreviewQuery.Describe(
                SkillCardActionStrategyBackend.FindEffectRoot(materialized, pending),
                choice,
                pending.ActorId,
                components,
                modifications);
        }

        /// <summary>
        /// Milestone 6.4a's paused after-action correction, or <c>null</c>. It is dispatcher-owned
        /// rather than backend-owned because the correction belongs to the action boundary itself:
        /// every displacement-capable operation the Skill backend hosts - and every later one - reaches
        /// it through the same completion path, which is the reuse the roadmap requires.
        /// </summary>
        public PendingDisplacedMinionCorrection PendingDisplacedMinionCorrection => pendingCorrection;

        /// <summary>
        /// The modifications in effect right now, derived from the supplied timing state and this
        /// authority's board. Built per command rather than cached: an Active effect can expire and a
        /// token can be removed between two commands, and a cached set would answer for the wrong moment.
        /// </summary>
        public RuleModificationSet CurrentModifications(
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            MatchParticipants participants,
            TieBreakerState tieBreaker) =>
            RuleModificationQuery.Build(timing, board, participants, tieBreaker);

        public bool HasPendingResolution =>
            movementBackend.HasPendingResolution || attackDefenseBackend.HasPendingResolution ||
            skillBackend.HasPendingResolution || pendingCorrection != null;

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Skill,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            var result = skillBackend.StartTurnSkill(
                resolutionId, offer, prepared.Definitions, prepared.Instances, participants,
                timing, modifications, components, roundIndex, turnIndex);
            return RunCorrectionAfterAction(
                result, resolutionId, offer.SourceCardInstanceId, offer.ActionDefinitionId,
                offer.ActorId, participants, tieBreaker);
        }

        /// <summary>
        /// Milestone 6.5af: whether starting this offer's Skill could apply anything. The components
        /// this authority owns are what make the question answerable at all, which is why it is asked
        /// here rather than in the card-state-only offer query.
        /// </summary>
        public bool CanSkillApplyAnything(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex) =>
            skillBackend.CanApplyAnything(
                offer, definitions, participants, timing, modifications, components,
                roundIndex, turnIndex);

        public CardActionResolutionResult AnswerEffectChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            // Read the owning action's identity before the answer: a completing answer clears the
            // frame, and the correction facts still have to name the action they close.
            var frame = skillBackend.PendingResolution;
            var result = skillBackend.AnswerEffectChoice(
                command, definitions, participants, timing, modifications, components, roundIndex, turnIndex);
            if (frame == null)
            {
                return result;
            }

            return RunCorrectionAfterAction(
                result, new ActionResolutionId(frame.ResolutionId.Value), frame.SourceCardInstanceId,
                frame.ActionDefinitionId, frame.ActorId, participants, tieBreaker);
        }

        /// <summary>
        /// Answers the paused after-action correction. Everything is validated against a fresh
        /// derivation from the live board rather than the stored candidates - the same discipline the
        /// Minion Battle answer applies - so a stale or tampered answer is refused and the pending
        /// choice survives untouched.
        /// </summary>
        public CardActionResolutionResult AnswerDisplacedMinionCorrection(
            DisplacedMinionCorrectionCommand command,
            MatchParticipants participants,
            TieBreakerState tieBreaker)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (pendingCorrection == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingCorrection, null, null);
            }

            if (!command.ResolutionId.Equals(pendingCorrection.ResolutionId) ||
                !command.ChoiceId.Equals(pendingCorrection.ChoiceId) ||
                command.ChoosingSide != pendingCorrection.ChoosingSide)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.StaleCorrectionChoice, null, null);
            }

            var pending = pendingCorrection;
            var steps = new List<ResolutionStep>();
            if (pending.Stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                if (command.Kind != DisplacedMinionCorrectionCommandKind.SelectSubject)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var derived = DisplacedMinionCorrectionPlanner.DeriveNext(
                    board, participants, tieBreaker, pending.ResolutionId,
                    pending.SourceCardInstanceId, pending.ActionDefinitionId, pending.ActorId);
                if (derived.Kind != DisplacedMinionCorrectionStepKind.Choice ||
                    derived.Pending.Stage != DisplacedMinionCorrectionStage.SelectSubject ||
                    !derived.Pending.SubjectCandidates.Any(subject =>
                        subject.MinionId.Equals(command.MinionId)))
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var subject = board.Minions.Single(minion => minion.MinionId.Equals(command.MinionId));
                var zoneCoordinates = BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition);
                var destinationStep = DisplacedMinionCorrectionPlanner.DeriveDestinationStep(
                    board, participants, pending.BattleZoneId, zoneCoordinates, subject,
                    pending.ResolutionId, pending.SourceCardInstanceId,
                    pending.ActionDefinitionId, pending.ActorId);
                if (destinationStep.Kind == DisplacedMinionCorrectionStepKind.Choice)
                {
                    pendingCorrection = destinationStep.Pending;
                    steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                        CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested,
                        pending.ResolutionId, pending.SourceCardInstanceId, pending.ActionDefinitionId,
                        pending.ActorId, destinationStep.Pending.ChoiceId.Value)));
                    return CardActionResolutionResult.Accept(false, null, null, steps);
                }

                CommitCorrection(steps, pending, destinationStep);
            }
            else
            {
                if (command.Kind != DisplacedMinionCorrectionCommandKind.SelectDestination ||
                    !command.MinionId.Equals(pending.SubjectMinionId))
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var subject = board.Minions.SingleOrDefault(minion =>
                    minion.MinionId.Equals(pending.SubjectMinionId));
                var zoneCoordinates = BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition);
                var candidates = subject.MinionId.IsDefined &&
                    !BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, subject.OccupiedSpace.SpaceId)
                        ? DisplacedMinionCorrectionPlanner.DeriveCandidates(
                            board, zoneCoordinates, subject, out var kind)
                        : Array.Empty<DisplacedMinionCorrectionCandidate>();
                var chosen = candidates.Where(candidate =>
                    candidate.Destination.Equals(command.Destination)).ToArray();
                if (chosen.Length != 1)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                CommitCorrection(
                    steps, pending,
                    DisplacedMinionCorrectionStep.Apply(subject, pending.Kind, chosen[0]));
            }

            pendingCorrection = null;
            var complete = AppendCorrectionSteps(
                steps, pending.ResolutionId, pending.SourceCardInstanceId, pending.ActionDefinitionId,
                pending.ActorId, participants, tieBreaker, true);
            return CardActionResolutionResult.Accept(complete, null, null, steps);
        }

        /// <summary>
        /// Runs the after-action displaced-minion correction once a Skill's effect nodes and batches
        /// have all finished (p.18: "after you perform an action"). Appends nothing at all when no
        /// minion miniature ended up outside the Battle Zone, so every existing action's fact stream
        /// is untouched by the seam existing.
        /// </summary>
        private CardActionResolutionResult RunCorrectionAfterAction(
            CardActionResolutionResult result,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            MatchParticipants participants,
            TieBreakerState tieBreaker)
        {
            if (!result.Accepted || !result.IsComplete)
            {
                return result;
            }

            var steps = new List<ResolutionStep>(result.Steps);
            var complete = AppendCorrectionSteps(
                steps, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                participants, tieBreaker, false);
            return complete && steps.Count == result.Steps.Count
                ? result
                : CardActionResolutionResult.Accept(complete, null, null, steps);
        }

        /// <summary>
        /// Applies corrections until the board is clean, a team must decide, or only uncorrectable
        /// strays remain. Every iteration re-derives from the live board, because an earlier correction
        /// can free or block a later one's shortest path.
        /// </summary>
        private bool AppendCorrectionSteps(
            List<ResolutionStep> steps,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            bool correctionAlreadyStarted)
        {
            var emitted = correctionAlreadyStarted;
            while (true)
            {
                var step = DisplacedMinionCorrectionPlanner.DeriveNext(
                    board, participants, tieBreaker, resolutionId, sourceCardInstanceId,
                    actionDefinitionId, actorId);
                switch (step.Kind)
                {
                    case DisplacedMinionCorrectionStepKind.Apply:
                        EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                        CommitCorrection(steps, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId, step);
                        continue;

                    case DisplacedMinionCorrectionStepKind.Choice:
                        EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                        pendingCorrection = step.Pending;
                        steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                            CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested,
                            resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                            step.Pending.ChoiceId.Value)));
                        return false;

                    default:
                        if (step.LeftInPlace.Count > 0)
                        {
                            EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                            foreach (var stranded in step.LeftInPlace)
                            {
                                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                                    CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace,
                                    resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                                    $"correction-left-{stranded.MinionId.Value}",
                                    new DisplacedMinionCorrectionEvent(
                                        stranded.MinionId, DisplacedMinionCorrectionKind.None,
                                        stranded.Coordinate, stranded.Coordinate))));
                            }
                        }

                        if (emitted)
                        {
                            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                                CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted,
                                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                                "correction-complete")));
                        }

                        return true;
                }
            }
        }

        private void EmitStartedOnce(
            List<ResolutionStep> steps,
            ref bool emitted,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId)
        {
            if (emitted)
            {
                return;
            }

            emitted = true;
            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                CardActionResolutionEventKind.DisplacedMinionCorrectionStarted,
                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId, "correction-start")));
        }

        private void CommitCorrection(
            List<ResolutionStep> steps,
            PendingDisplacedMinionCorrection pending,
            DisplacedMinionCorrectionStep step) =>
            CommitCorrection(
                steps, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.ActionDefinitionId, pending.ActorId, step);

        private void CommitCorrection(
            List<ResolutionStep> steps,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            DisplacedMinionCorrectionStep step)
        {
            var origin = step.Subject.Coordinate;
            var subject = BoardObjectReference.ForMinion(step.Subject.MinionId);

            // p.18 gives the correction two different operations, so this commits through two: a
            // shortest-path return is a relocation, and the no-path fallback is a Place. Since
            // milestone 6.5 the fallback is the shared general Place rather than a minimum path of its
            // own, which is what makes the eligible piece kind, the destination question, and the
            // different-space requirement one implementation with every other Place.
            var accepted = step.CorrectionKind == DisplacedMinionCorrectionKind.Place
                ? PlaceOperation.PlaceFromBoard(
                    DisplacedMinionCorrectionPlanner.FallbackPlacePolicy,
                    board,
                    board,
                    subject,
                    step.Destination.Destination,
                    BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition)).Accepted
                : board.CommitValidatedDisplacement(subject, step.Destination.Destination).Accepted;
            if (!accepted)
            {
                throw new InvalidOperationException(
                    "A derived correction destination could not be committed; the planner and the " +
                    "board disagree about the same state.");
            }

            var kind = step.CorrectionKind == DisplacedMinionCorrectionKind.Place
                ? CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced
                : CardActionResolutionEventKind.DisplacedMinionCorrectionMoved;
            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                kind, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                $"correction-{step.Subject.MinionId.Value}",
                new DisplacedMinionCorrectionEvent(
                    step.Subject.MinionId, step.CorrectionKind, origin, step.Destination.Destination))));
        }

        public CardActionResolutionRestoreState CaptureRestoreState()
        {
            var rewards = DefeatRewards == null
                ? DefeatRewardRestoreStateHolder.Absent()
                : DefeatRewardRestoreStateHolder.Present(DefeatRewards.CaptureRestoreState());
            if (PendingResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingMovement(PendingResolution, rewards);
            }

            if (PendingAttackResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingAttack(PendingAttackResolution, rewards);
            }

            if (PendingSkillResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingSkill(PendingSkillResolution, rewards);
            }

            return pendingCorrection != null
                ? CardActionResolutionRestoreState.WithPendingCorrection(pendingCorrection, rewards)
                : CardActionResolutionRestoreState.WithoutPendingAction(rewards);
        }

        /// <summary>
        /// Restores the single card-action authority described by <paramref name="state"/>.
        /// </summary>
        /// <param name="verifiedMovementChoice">
        /// An independently supplied pending movement choice to cross-check, or
        /// <c>null</c> to accept the one derived from the resolution.
        /// </param>
        /// <param name="verifiedTargetChoice">
        /// An independently supplied Attack target choice to cross-check, or <c>null</c>.
        /// </param>
        /// <param name="verifiedDefenseChoice">
        /// An independently supplied Defense response choice to cross-check, or <c>null</c>.
        /// </param>
        /// <param name="markers">
        /// The marker register the round is restoring, so the dispatcher's capabilities and the round's
        /// persisted state share one object. Required rather than optional: a caller that supplied its
        /// own fresh register here would restore a match whose markers exist for the snapshot and not
        /// for the effects that can return them.
        /// </param>
        public static CardActionResolutionAuthority Restore(
            AdjacentMoveAuthority adjacentMoveAuthority,
            CardActionResolutionRestoreState state,
            PendingMovementChoice verifiedMovementChoice,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var defeatRewards = state.DefeatRewards.IsPresent
                ? DefeatRewardAccounting.Restore(participants, state.DefeatRewards.State)
                : null;
            var components = CreateComponents(adjacentMoveAuthority, markers);

            switch (state.ContinuationKind)
            {
                case CardActionContinuationKind.Movement:
                    return new CardActionResolutionAuthority(
                        MovementCardActionStrategyBackend.Restore(
                            adjacentMoveAuthority,
                            new MovementBackendRestoreState(state.PendingMovementResolution),
                            verifiedMovementChoice,
                            modifications),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(SyntheticEffectCapabilities.CreateCatalog()),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Attack:
                    return new CardActionResolutionAuthority(
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        AttackDefenseCardActionStrategyBackend.RestoreAttack(
                            adjacentMoveAuthority,
                            new AttackDefenseBackendRestoreState(state.PendingAttackResolution),
                            verifiedTargetChoice,
                            verifiedDefenseChoice,
                            definitions,
                            instances,
                            participants,
                            defeatRewards,
                            modifications),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(SyntheticEffectCapabilities.CreateCatalog()),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Skill:
                    return new CardActionResolutionAuthority(
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        SkillCardActionStrategyBackend.Restore(
                            SyntheticEffectCapabilities.CreateCatalog(),
                            new SkillBackendRestoreState(state.PendingSkillResolution)),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Correction:
                    var restored = new CardActionResolutionAuthority(
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(SyntheticEffectCapabilities.CreateCatalog()),
                        components,
                        adjacentMoveAuthority);
                    restored.pendingCorrection = state.PendingCorrection;
                    return restored;

                default:
                    return new CardActionResolutionAuthority(
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(SyntheticEffectCapabilities.CreateCatalog()),
                        components,
                        adjacentMoveAuthority);
            }
        }

        public CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Movement,
                false);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            return movementBackend.StartMovement(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    modifications);
        }

        public CardActionResolutionResult StartTurnMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Movement,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            return movementBackend.StartTurnMovement(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    modifications);
        }

        public CardActionResolutionResult AnswerMovementChoice(
            MovementChoiceCommand command, RuleModificationSet modifications) =>
            movementBackend.AnswerMovement(command, modifications);

        public CardActionResolutionResult PerformTurnFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications)
        {
            if (HasPendingResolution)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    PendingResolution,
                    PendingChoice);
            }

            return fastTravelBackend.PerformTurnFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                definitions,
                instances,
                participants,
                minions,
                modifications);
        }

        public CardActionResolutionResult PerformTurnClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (HasPendingResolution)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    PendingResolution,
                    PendingChoice);
            }

            return clearBackend.PerformTurnClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds,
                definitions,
                instances);
        }

        public AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Attack,
                false);
            if (prepared.RejectionReason.HasValue)
            {
                return AttackActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingAttackResolution,
                    PendingAttackTargetChoice,
                    PendingDefenseChoice);
            }

            return attackDefenseBackend.StartAttack(
                resolutionId,
                offer,
                prepared.Definitions,
                prepared.Instances,
                participants,
                modifications);
        }

        public AttackActionResolutionResult StartTurnAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Attack,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return AttackActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingAttackResolution,
                    PendingAttackTargetChoice,
                    PendingDefenseChoice);
            }

            return attackDefenseBackend.StartTurnAttack(
                resolutionId,
                offer,
                prepared.Definitions,
                prepared.Instances,
                participants,
                modifications);
        }

        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.AnswerAttackTarget(
                command, definitions, instances, participants, modifications);

        public AttackActionResolutionResult AnswerDefenseResponse(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.AnswerDefense(
                command, definitions, instances, participants, modifications);

        public AttackActionResolutionResult ResolveAttackOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.ResolveOutcome(definitions, instances, participants, modifications);

        private PreparedStart PrepareStart(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            ActionKind expectedKind,
            bool turnOnly)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting a card action resolution requires a resolution ID.", nameof(resolutionId));
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            if (HasPendingResolution)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    materializedDefinitions,
                    materializedInstances);
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext,
                    materializedDefinitions,
                    materializedInstances);
            }

            var authoritativeOffers = turnOnly
                ? ActionOfferQuery.GetAvailableTurnActionOffers(
                    materializedDefinitions,
                    materializedInstances,
                    offer.ActorId,
                    offer.SourceCardInstanceId)
                : ActionOfferQuery.GetAvailableOffers(
                    materializedDefinitions,
                    materializedInstances,
                    offer.ActorId,
                    ActionOfferWindow.NormalPlay);
            if (!offer.IsAvailable || !authoritativeOffers.Contains(offer))
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer,
                    materializedDefinitions,
                    materializedInstances);
            }

            if (ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions).Kind != expectedKind)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnsupportedActionKind,
                    materializedDefinitions,
                    materializedInstances);
            }

            return PreparedStart.Accept(materializedDefinitions, materializedInstances);
        }

        private readonly struct PreparedStart
        {
            private PreparedStart(
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                CardActionResolutionRejectionReason? rejectionReason)
            {
                Definitions = definitions;
                Instances = instances;
                RejectionReason = rejectionReason;
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }

            internal IReadOnlyList<CardInstance> Instances { get; }

            internal CardActionResolutionRejectionReason? RejectionReason { get; }

            internal static PreparedStart Accept(
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances) =>
                new PreparedStart(definitions, instances, null);

            internal static PreparedStart Reject(
                CardActionResolutionRejectionReason reason,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances) =>
                new PreparedStart(definitions, instances, reason);
        }
    }
}
