using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Effects;
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

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, null)
        {
        }

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions)
            : this(catalogs, adjacentMoveAuthority, movementActionStartRestrictions, null)
        {
        }

        /// <summary>
        /// The configured match's shape: default movement restrictions, plus the marker register the
        /// round authority also holds. The default evaluator is internal to this assembly, so the
        /// application layer composes a match through this overload rather than naming it.
        /// </summary>
        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, markers)
        {
        }

        /// <summary>
        /// Milestone 7.4. A dispatcher composed without a round authority, stating the two states
        /// rulebook p.13's hero-Defeat consequences read and write.
        ///
        /// A configured match never uses this: there the round owns both, is composed after this
        /// dispatcher, and binds them down. This exists for the compositions that have no round at all,
        /// and it takes them at construction rather than through a setter so such a composition states
        /// what a Defeat is priced against once and cannot silently exchange it later. A dispatcher
        /// built through any other overload and never bound refuses to resolve a hero Defeat rather
        /// than pricing one against Levels and counters nobody records.
        /// </summary>
        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers,
            GuardsDigital.Core.Rules.Progression.HeroProgressionState heroLevels,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState teamLives)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, markers)
        {
            if (heroLevels == null) throw new ArgumentNullException(nameof(heroLevels));
            if (teamLives == null) throw new ArgumentNullException(nameof(teamLives));
            BindDefeatConsequenceState(heroLevels, teamLives);
        }

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions,
            MarkerAttachmentRegister markers)
            : this(
                catalogs ?? throw new ArgumentNullException(nameof(catalogs)),
                new MovementCardActionStrategyBackend(
                    adjacentMoveAuthority,
                    movementActionStartRestrictions ?? throw new ArgumentNullException(
                        nameof(movementActionStartRestrictions))),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, null, markers),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                new SkillCardActionStrategyBackend(catalogs.Capabilities),
                CreateComponents(catalogs, adjacentMoveAuthority, markers),
                adjacentMoveAuthority)
        {
        }

        private PendingDisplacedMinionCorrection pendingCorrection;
        private PendingRepeatedAction pendingRepeat;
        private PendingImmediateLanePushRequest pendingLanePush;
        private PendingAttackCardTextResolution pendingCardText;
        private PendingAttackResolution owedAttackRepeat;
        private AttackTarget owedAttackRepeatExclusion;

        private CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            MovementCardActionStrategyBackend movementBackend,
            AttackDefenseCardActionStrategyBackend attackDefenseBackend,
            FastTravelCardActionStrategyBackend fastTravelBackend,
            ClearCardActionStrategyBackend clearBackend,
            SkillCardActionStrategyBackend skillBackend,
            MatchComponents components,
            AdjacentMoveAuthority board)
        {
            ContentCatalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
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
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            new MatchComponents(
                catalogs.Components,
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
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            CreateComponents(catalogs, adjacentMoveAuthority, markers);

        /// <summary>
        /// Milestone 8.3. The trusted content this dispatcher was composed with, exposed so the write
        /// path can record what a match actually ran on instead of asking the build a second time. It
        /// is the same object every backend and every capability here holds.
        /// </summary>
        public MatchContentCatalogs ContentCatalogs { get; }

        public PendingCardActionResolution PendingResolution => movementBackend.PendingResolution;

        public PendingMovementChoice PendingChoice => movementBackend.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => attackDefenseBackend.PendingResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => attackDefenseBackend.PendingTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => attackDefenseBackend.PendingDefenseChoice;

        public HeroCoinLedger CoinLedger => attackDefenseBackend.CoinLedger;

        /// <summary>
        /// Milestone 7.5. The match's one coin ledger, created on first ask. The round authority asks
        /// when it composes itself, because p.15's level purchase now spends from the same balances
        /// p.13's Defeat fills and two ledgers would be two answers to what a hero can afford.
        /// </summary>
        internal HeroCoinLedger EnsureCoinLedger(MatchParticipants participants) =>
            attackDefenseBackend.EnsureCoinLedger(participants);

        /// <summary>
        /// Milestone 7.2. Hands the backends the match's item state, which the round authority owns.
        /// It is a binding rather than a constructor argument because the dispatcher is composed before
        /// the round that owns the state, and the one thing that must not happen is a second copy: a
        /// Defense total derived from items other than the ones the match records would be a number no
        /// restore or replay could reproduce.
        /// </summary>
        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            attackDefenseBackend.BindHeroItems(heroItems);

            // Milestone 7.2a. The Movement backend joins it, so an item raises the Movement Total that
            // gates the action and bounds its reachability through the same state and the same query.
            movementBackend.BindHeroItems(heroItems);

            // Milestone 7.2b. The Skill backend joins them, so a capability reading its card's printed
            // Radius composes the same items into the same total every other channel does.
            skillBackend.BindHeroItems(heroItems);
        }

        /// <summary>
        /// Milestone 7.4. Hands the Attack backend the two states rulebook p.13's hero-Defeat
        /// consequences read and write: every hero's Level and every team's Life counters. Only that
        /// backend needs them, because a hero Defeat can only be produced by an Attack - p.19's Remove
        /// is explicitly not a Defeat and "Do not collect any coins" - so binding them more widely would
        /// hand two other backends state no rule of theirs reads.
        /// </summary>
        internal void BindDefeatConsequenceState(
            GuardsDigital.Core.Rules.Progression.HeroProgressionState levels,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState lives)
        {
            attackDefenseBackend.BindDefeatConsequenceState(levels, lives);
        }

        internal AdjacentMoveAuthority Board => board;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            skillBackend.PendingResolution;

        /// <summary>
        /// The question an effect frame is waiting on, whichever frame is holding it. Milestone 8.10
        /// added the second source: card text applied inside rulebook p.14 pauses on the same frame a
        /// Skill does, so a reader that already knew how to draw and answer one needs nothing new to
        /// draw and answer the other, and a match can only ever be holding one of the two.
        /// </summary>
        public GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice PendingEffectChoice =>
            pendingCardText?.Frame.PendingChoice ?? skillBackend.PendingChoice;

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
            var pending = pendingCardText?.Frame ?? skillBackend.PendingResolution;
            var choice = pending?.PendingChoice;
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
        /// Milestone 6.7's owed second action instance, or <c>null</c>. Non-null only while the
        /// correction between the two instances is paused on a team choice: a correction that needs no
        /// choice starts the instance inside the same command, so there is nothing to hold.
        /// </summary>
        public PendingRepeatedAction PendingRepeatedAction => pendingRepeat;

        /// <summary>
        /// Milestone 6.9's mid-turn Lane-Push request, or <c>null</c>. It is dispatcher-owned for the
        /// reason the correction is: p.8's push is a consequence of the action boundary rather than of
        /// any one backend, and both operations the rulebook allows to cause one - p.19's standalone
        /// Remove inside a Skill and p.13's Defeat inside an Attack - reach this object.
        ///
        /// Nothing here consumes it. The turn refuses to hand Initiative on while it is set, which is
        /// the whole of this milestone's stop; milestones 6.10 and 6.11 own the transition that clears
        /// it.
        /// </summary>
        public PendingImmediateLanePushRequest PendingImmediateLanePush => pendingLanePush;

        /// <summary>
        /// Milestone 8.10's Attack stopped inside rulebook p.14's card text, or <c>null</c>. Non-null
        /// only while the text applied at one of the page's moments is waiting on an answer; text that
        /// asks nothing finishes inside the command that reached it, exactly as it did before.
        /// </summary>
        public PendingAttackCardTextResolution PendingCardTextResolution => pendingCardText;

        /// <summary>
        /// Milestone 8.10. Whether an Attack's own card text asked for that Attack to happen again -
        /// Thunder Boomerang's "may repeat once on a different target" - and the instance that asked
        /// has finished the page.
        ///
        /// <para>It is not persisted, and does not need to be: the request is recorded and consumed
        /// inside one command, between the card text completing and the turn deciding what to do with a
        /// finished Attack. Every pause the repeated instance then opens is the ordinary Attack's own,
        /// which already crosses the wire.</para>
        /// </summary>
        public bool HasOwedAttackRepeat => owedAttackRepeat != null;

        /// <summary>
        /// The modifications in effect right now, derived from the supplied timing state and this
        /// authority's board. Built per command rather than cached: an Active effect can expire, a
        /// token can be removed, and milestone 7.3's hero can leave the board between two commands, and
        /// a cached set would answer for the wrong moment.
        ///
        /// The unlocked Ultimates arrive from the caller rather than being derived here, because they
        /// are read off the match's card instances and this authority holds none; the callers that do
        /// pass what they hold.
        ///
        /// Milestone 8.9's items go the other way, and for the mirror-image reason: the match's one
        /// item state is already bound into this authority's backends, so taking it from a caller
        /// would be the second copy <see cref="BindHeroItems"/> exists to prevent.
        /// </summary>
        public RuleModificationSet CurrentModifications(
            ActiveEffectLifecycle timing,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates,
            RuleObject actingObject) =>
            RuleModificationQuery.Build(
                timing, board, participants, tieBreaker, ultimates, actingObject, skillBackend.HeroItems);

        public bool HasPendingResolution =>
            movementBackend.HasPendingResolution || attackDefenseBackend.HasPendingResolution ||
            skillBackend.HasPendingResolution || pendingCorrection != null || pendingRepeat != null ||
            pendingCardText != null;

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
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
                timing, modifications, components, roundIndex, turnIndex, out var repeat,
                out var lanePushCause);
            return CloseActionBoundary(
                result, repeat, lanePushCause, resolutionId, offer.SourceCardInstanceId,
                offer.ActionDefinitionId, offer.ActorId,
                new ActionBoundaryContext(
                    prepared.Definitions, prepared.Instances, participants, timing, tieBreaker,
                    roundIndex, turnIndex));
        }

        /// <summary>
        /// Milestone 6.5af: whether starting this offer's Skill could apply anything. The components
        /// this authority owns are what make the question answerable at all, which is why it is asked
        /// here rather than in the card-state-only offer query.
        /// </summary>
        public bool CanSkillApplyAnything(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex) =>
            skillBackend.CanApplyAnything(
                offer, definitions, instances, participants, timing, modifications, components,
                roundIndex, turnIndex);

        /// <summary>
        /// Milestone 7.8a1: whether an Attack offer has a legal target, asked of the same backend that
        /// builds the target set when the Attack starts. The counterpart of
        /// <see cref="CanSkillApplyAnything"/>, and true for anything that is not an Attack.
        /// </summary>
        public bool CanAttackFindTarget(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.CanFindLegalTarget(offer, definitions, participants, modifications);

        public CardActionResolutionResult AnswerEffectChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            // Read the owning action's identity before the answer: a completing answer clears the
            // frame, and the correction facts still have to name the action they close.
            var frame = skillBackend.PendingResolution;
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var result = skillBackend.AnswerEffectChoice(
                command, materializedDefinitions, materializedInstances, participants, timing,
                modifications, components, roundIndex, turnIndex, out var repeat, out var lanePushCause);
            if (frame == null)
            {
                return result;
            }

            return CloseActionBoundary(
                result, repeat, lanePushCause, new ActionResolutionId(frame.ResolutionId.Value),
                frame.SourceCardInstanceId, frame.ActionDefinitionId, frame.ActorId,
                new ActionBoundaryContext(
                    materializedDefinitions, materializedInstances, participants, timing, tieBreaker,
                    roundIndex, turnIndex));
        }

        /// <summary>
        /// Answers the paused after-action correction. Everything is validated against a fresh
        /// derivation from the live board rather than the stored candidates - the same discipline the
        /// Minion Battle answer applies - so a stale or tampered answer is refused and the pending
        /// choice survives untouched.
        /// </summary>
        public CardActionResolutionResult AnswerDisplacedMinionCorrection(
            DisplacedMinionCorrectionCommand command,
            ActionBoundaryContext boundary)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var participants = boundary.Participants ??
                throw new ArgumentException("A correction answer requires the match participants.", nameof(boundary));
            var tieBreaker = boundary.TieBreaker;
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
                var zoneCoordinates = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
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
                var zoneCoordinates = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
                var candidates = subject.MinionId.IsDefined &&
                    !BattleZoneQuery.IsInCurrentBattleZone(board, subject.OccupiedSpace.SpaceId)
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
            return complete
                ? StartOwedRepeat(steps, boundary)
                : CardActionResolutionResult.Accept(false, null, null, steps);
        }

        /// <summary>
        /// Milestone 6.7: closes one action instance's boundary, and starts the next instance when this
        /// one asked for it.
        ///
        /// The order is the acceptance check: the instance's own effect nodes finish, p.18's
        /// after-action correction runs to completion, and only then does a repeated instance begin.
        /// Nothing starts while the correction is paused - the request is held instead, and the pause
        /// is what the next command answers.
        ///
        /// A still-resolving action is returned untouched: an instance that has not completed cannot
        /// have requested anything, because the request is read from the completing frame.
        /// </summary>
        private CardActionResolutionResult CloseActionBoundary(
            CardActionResolutionResult result,
            PendingRepeatedAction repeat,
            LanePushCauseKind lanePushCause,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionBoundaryContext boundary)
        {
            if (!result.Accepted || !result.IsComplete)
            {
                return result;
            }

            // Milestone 6.9: p.8 pushes the Lane immediately, so the request is minted here - before
            // p.18's after-action correction runs - rather than once the boundary is finally clean. A
            // minion the correction returns to the Battle Zone was not standing there when the last one
            // was removed, and reading the board after it would answer a question about a later moment.
            RecordImmediateLanePush(
                lanePushCause, resolutionId, boundary.Participants,
                CurrentModifications(
                    boundary.Timing,
                    boundary.Participants,
                    boundary.TieBreaker,
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        boundary.Definitions, boundary.Instances),
                    RuleObject.ForHero(actorId)));

            pendingRepeat = repeat;
            var steps = new List<ResolutionStep>(result.Steps);
            var complete = AppendCorrectionSteps(
                steps, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                boundary.Participants, boundary.TieBreaker, false);
            if (!complete)
            {
                return CardActionResolutionResult.Accept(false, null, null, steps);
            }

            if (pendingRepeat == null)
            {
                return steps.Count == result.Steps.Count
                    ? result
                    : CardActionResolutionResult.Accept(true, null, null, steps);
            }

            return StartOwedRepeat(steps, boundary);
        }

        /// <summary>
        /// Starts the instance a completed action asked for, once its boundary is clean, and closes
        /// that instance's own boundary in turn.
        ///
        /// The recursion is bounded by the rule itself rather than by a guard here: each instance
        /// carries how many repeats its chain has already run, and the Repeat node it reaches is
        /// eligible only while that count is below the maximum the node declares - so the chain ends by
        /// the last instance reporting no request, exactly as milestone 6.7's single repeat did when
        /// the maximum was always one.
        /// </summary>
        private CardActionResolutionResult StartOwedRepeat(
            List<ResolutionStep> steps,
            ActionBoundaryContext boundary)
        {
            var repeat = pendingRepeat;
            if (repeat == null)
            {
                return CardActionResolutionResult.Accept(true, null, null, steps);
            }

            pendingRepeat = null;
            var repeated = skillBackend.StartRepeatedSkill(
                repeat.ResolutionId,
                repeat,
                boundary.Definitions,
                boundary.Instances,
                boundary.Participants,
                boundary.Timing,
                CurrentModifications(
                    boundary.Timing,
                    boundary.Participants,
                    boundary.TieBreaker,
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        boundary.Definitions, boundary.Instances),
                    RuleObject.ForHero(repeat.ActorId)),
                components,
                boundary.RoundIndex,
                boundary.TurnIndex,
                out var furtherRepeat,
                out var repeatedLanePushCause);
            if (!repeated.Accepted)
            {
                throw new InvalidOperationException(
                    "A repeated action instance could not start from the definition its own action " +
                    "just resolved from; the trusted definitions and the recorded repeat disagree.");
            }

            steps.AddRange(repeated.Steps);
            return CloseActionBoundary(
                CardActionResolutionResult.Accept(repeated.IsComplete, null, null, steps),
                furtherRepeat,
                repeatedLanePushCause,
                repeat.ResolutionId,
                repeat.SourceCardInstanceId,
                repeat.ActionDefinitionId,
                repeat.ActorId,
                boundary);
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
                    origin,
                    step.Destination.Destination,
                    BattleZoneQuery.FindCurrentBattleZoneCoordinates(board),
                    // The planner derived this destination with no modifications in effect, for the
                    // reason recorded there; the commit asks the same question the same way, or the
                    // two would disagree about the same state.
                    RuleModificationSet.Empty).Accepted
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
            var ledger = CoinLedger == null
                ? HeroCoinLedgerRestoreStateHolder.Absent()
                : HeroCoinLedgerRestoreStateHolder.Present(CoinLedger.CaptureRestoreState());
            // Milestone 8.10 first, and deliberately: at p.14's step 4 the attack's own resolution is
            // still pending underneath the paused text, so asking about the attack before asking about
            // the text would capture the attack alone and lose the frame that is holding the page.
            if (pendingCardText != null)
            {
                return CardActionResolutionRestoreState.WithPendingCardText(
                    pendingCardText, ledger, pendingLanePush);
            }

            if (PendingResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingMovement(
                    PendingResolution, ledger, pendingLanePush);
            }

            if (PendingAttackResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingAttack(
                    PendingAttackResolution, ledger, pendingLanePush);
            }

            if (PendingSkillResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingSkill(
                    PendingSkillResolution, ledger, pendingLanePush);
            }

            return pendingCorrection != null
                ? CardActionResolutionRestoreState.WithPendingCorrection(
                    pendingCorrection, ledger, pendingRepeat, pendingLanePush)
                : CardActionResolutionRestoreState.WithoutPendingAction(ledger, pendingLanePush);
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
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            CardActionResolutionRestoreState state,
            PendingMovementChoice verifiedMovementChoice,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            GuardsDigital.Core.Rules.Progression.HeroProgressionState heroLevels = null,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState teamLives = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Milestone 6.9. Restored outside the continuation switch because a mid-turn Lane-Push
            // request is not one of the continuations: it accompanies whichever one the action left
            // behind, including none at all.
            var restoredAuthority = RestoreContinuation(
                catalogs, adjacentMoveAuthority, state, verifiedMovementChoice, verifiedTargetChoice,
                verifiedDefenseChoice, definitions, instances, participants, markers, modifications,
                heroItems);
            restoredAuthority.pendingLanePush = state.PendingImmediateLanePush;

            // Every continuation, not only the Attack one that needed the items to verify itself: the
            // restored dispatcher answers the next command, and that command may be the Defense whose
            // total the items belong in.
            restoredAuthority.BindHeroItems(heroItems);

            // Milestone 7.4. Same reason, one milestone later: the restored dispatcher answers the next
            // command, and that command may be the Attack outcome whose Defeat p.13 prices. A round
            // restoring a whole match rebinds both from its own restored state a few lines later, so
            // this argument is for the compositions that restore a dispatcher alone.
            if (heroLevels != null && teamLives != null)
            {
                restoredAuthority.BindDefeatConsequenceState(heroLevels, teamLives);
            }

            return restoredAuthority;
        }

        private static CardActionResolutionAuthority RestoreContinuation(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            CardActionResolutionRestoreState state,
            PendingMovementChoice verifiedMovementChoice,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            var coinLedger = state.CoinLedger.IsPresent
                ? HeroCoinLedger.Restore(participants, state.CoinLedger.State)
                : null;
            var components = CreateComponents(catalogs, adjacentMoveAuthority, markers);

            switch (state.ContinuationKind)
            {
                case CardActionContinuationKind.Movement:
                    return new CardActionResolutionAuthority(
                        catalogs,
                        MovementCardActionStrategyBackend.Restore(
                            adjacentMoveAuthority,
                            new MovementBackendRestoreState(state.PendingMovementResolution),
                            verifiedMovementChoice,
                            modifications),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(catalogs.Capabilities),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Attack:
                    return new CardActionResolutionAuthority(
                        catalogs,
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        AttackDefenseCardActionStrategyBackend.RestoreAttack(
                            adjacentMoveAuthority,
                            new AttackDefenseBackendRestoreState(state.PendingAttackResolution),
                            verifiedTargetChoice,
                            verifiedDefenseChoice,
                            definitions,
                            instances,
                            participants,
                            coinLedger,
                            markers,
                            modifications,
                            heroItems),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(catalogs.Capabilities),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Skill:
                    return new CardActionResolutionAuthority(
                        catalogs,
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        SkillCardActionStrategyBackend.Restore(
                            catalogs.Capabilities,
                            new SkillBackendRestoreState(state.PendingSkillResolution)),
                        components,
                        adjacentMoveAuthority);

                case CardActionContinuationKind.Correction:
                    var restored = new CardActionResolutionAuthority(
                        catalogs,
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(catalogs.Capabilities),
                        components,
                        adjacentMoveAuthority);
                    restored.pendingCorrection = state.PendingCorrection;
                    restored.pendingRepeat = state.PendingRepeat;
                    return restored;

                // Milestone 8.10. Nothing about the attack underneath is rebuilt here: what the page
                // has left to do is regenerated from the continuation's own identity when the question
                // is answered, so a restored pause owns exactly what the live one owned.
                case CardActionContinuationKind.AttackCardText:
                    var restoredCardText = new CardActionResolutionAuthority(
                        catalogs,
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(catalogs.Capabilities),
                        components,
                        adjacentMoveAuthority);
                    restoredCardText.pendingCardText = state.PendingCardText;
                    return restoredCardText;

                default:
                    return new CardActionResolutionAuthority(
                        catalogs,
                        new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                        new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                        new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                        new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                        new SkillCardActionStrategyBackend(catalogs.Capabilities),
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

        /// <summary>
        /// Milestone 8.8b: whether this Clear could remove anything, asked of the same backend that
        /// validates the selection when the Clear runs. The counterpart of
        /// <see cref="CanAttackFindTarget"/> for p.13's other replacement.
        /// </summary>
        public bool CanClearRemoveAnything(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications) =>
            clearBackend.CanRemoveAnything(
                replacementOffer,
                replacedAttackActionDefinitionId,
                definitions,
                instances,
                modifications);

        public CardActionResolutionResult PerformTurnClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
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
                instances,
                modifications);
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


        /// <summary>
        /// Applies one card's printed text at the moment rulebook p.14 reached, and returns the ordered
        /// facts it produced.
        ///
        /// <para>Milestone 8.5, closing the 7.9a review's F-CARD-01. The Attack backend owns p.14's
        /// order and says where each of the page's three card-text steps falls; this is what actually
        /// composes one, because the effect catalog, the component ports and the item state live here
        /// rather than in a backend that resolves numbers. The enclosing action's kind goes into the
        /// resolution context, so a capability that asks p.18's immunity asks about the Attack it is
        /// running inside rather than about a Skill (F-BOARD-03).</para>
        ///
        /// <para><b>Card text applied inside an Attack may pause, at two of p.14's three moments.</b>
        /// Milestone 8.10. What the page leaves after step 4 and after step 7 is a function of the
        /// attack's own identity, so a question standing at either is held in a
        /// <see cref="PendingAttackCardTextResolution"/> and the rest of the page is rebuilt from that
        /// identity when it is answered. What follows step 1 is not such a function - on the minion arm
        /// it contains the facts of a Defeat this backend has already applied - so text that could ask
        /// there is still refused at configuration by
        /// <see cref="GuardsDigital.Core.Effects.EffectDefinitionValidator"/>, and the throw below is
        /// the case that rule failed to catch. It stays a content fault rather than a rules
        /// outcome.</para>
        /// </summary>
        public IReadOnlyList<ResolutionStep> ApplyCardText(
            ResolutionStep step,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (step.Kind != ResolutionStepKind.CardText)
            {
                throw new ArgumentException(
                    "Only a card-text step applies card text.", nameof(step));
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var card = (instances ?? throw new ArgumentNullException(nameof(instances)))
                .Single(candidate => candidate.Id.Equals(step.CheckpointSourceCardInstanceId));
            var definition = materializedDefinitions.Single(candidate =>
                candidate.Reference.Equals(card.DefinitionReference));
            var action = definition.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(step.CardTextActionDefinitionId));

            var context = new EffectResolutionContext(
                action.EffectRoot,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                action.Kind,
                components: components,
                heroItems: skillBackend.HeroItems,
                // Milestone 8.10, widened at 8.11. What this text may ask about the Attack it is
                // printed inside: who is attacking, what the Attack took, and p.13.s subtype. Every
                // field is undefined at p.14.s step 1, because the page has not chosen a target yet.
                enclosingAttack: EnclosingAttackOf(step.CardTextRemainderResolution, materializedDefinitions));
            var result = EffectResolutionSession.Start(
                new EffectResolutionId(
                    $"card-text-{card.Id.Value}-{action.Id.Value}-{step.CardTextTiming}"),
                card.Id,
                card.DefinitionReference,
                action.Id,
                step.CardTextActorId,
                step.CardTextActorId,
                ActionInvocationContext.PrintedPrimary,
                context);
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"Card text on '{definition.Id}' action '{action.Id}' was refused at resolution " +
                    $"({result.RejectionReason}); the effect-definition validator admits only text a " +
                    "match can apply.");
            }

            if (result.Frame?.PendingChoice != null)
            {
                if (step.CardTextRemainderResolution == null)
                {
                    throw new InvalidOperationException(
                        $"Card text on '{definition.Id}' action '{action.Id}' asked a question at " +
                        $"rulebook p.14's {step.CardTextTiming}, whose remaining steps no identity can " +
                        "reproduce. The effect-definition validator refuses such a composition at " +
                        "configuration; this build loaded one it should have rejected.");
                }

                // Milestone 8.10. The page stops here. What is kept is the frame that asked, the moment
                // it asked at, and the attack the rest of the page belongs to - never the steps
                // themselves, which are regenerated from that identity when the answer arrives.
                pendingCardText = new PendingAttackCardTextResolution(
                    result.Frame,
                    step.CardTextTiming,
                    step.CardTextRemainderResolution,
                    step.CardTextRemainderOutcome);
            }

            var steps = result.Events
                .Select(value => CardActionResolutionEvent.CardTextApplied(
                    card.Id, action.Id, step.CardTextActorId, step.CardTextTiming, value))
                .Select(ResolutionStep.FromFact)
                .ToList();
            AppendRequestedHeroDefeats(
                result, step.CardTextRemainderResolution, materializedDefinitions, instances,
                participants, modifications, steps);
            RecordOwedAttackRepeat(result, step.CardTextRemainderResolution);
            return Array.AsReadOnly(steps.ToArray());
        }

        /// <summary>
        /// Milestone 8.10. Runs rulebook p.13's Defeat list for every hero this card text asked it to be
        /// run about, in the order the frame recorded them, and splices its facts into the same stream.
        ///
        /// <para>The requests are read only from a step that <em>completed</em>, which is the same rule
        /// milestone 6.9's Lane-Push cause is read under and for the same reason: a frame carries its
        /// outcomes cumulatively, so a request seen while the text is still paused would be seen again
        /// on the next step and the page would be run twice about one hero. Electroblast's defeat is
        /// the last node of its rider, so completion is exactly where it belongs.</para>
        ///
        /// <para>It is the Attack's own resolver that runs the list - not a second copy of p.13 - which
        /// is why the attack this text is printed inside has to be the one identity handed over.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.10. Remembers that this Attack's card text asked for the Attack again, and what
        /// the repeated instance may not take.
        ///
        /// <para>The exclusion is read from the declaration the request carries rather than from the
        /// effect nodes, which this dispatcher does not inspect: a repeat that declared nothing chooses
        /// freely by p.10, and only <c>DifferentTarget</c> - which is what Thunder Boomerang prints -
        /// takes the finished instance's target out of the set.</para>
        /// </summary>
        /// <summary>
        /// What card text applied inside rulebook p.14 may ask about the Attack it is running inside.
        /// Milestone 8.10 for the hero the Attack took; milestone 8.11 for the attacker and p.13's
        /// printed subtype, which Wasp's three Block cards are conditioned on.
        ///
        /// <para>The subtype is read from the trusted definition the resolution names rather than
        /// stored on the resolution, for the reason every other printed reading is: the definition is
        /// the content the match was configured with, and a copy on a continuation would be a second
        /// place the same fact could be wrong.</para>
        /// </summary>
        private static GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts EnclosingAttackOf(
            PendingAttackResolution attack,
            IReadOnlyList<CardDefinition> definitions)
        {
            if (attack == null)
            {
                return GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.None;
            }

            var action = definitions
                .FirstOrDefault(definition => definition.Reference.Equals(attack.SourceDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(attack.ActionDefinitionId));
            return new GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts(
                attack.ActorId, attack.TargetHeroId, action?.AttackRange ?? AttackRangeKind.None);
        }

        private void RecordOwedAttackRepeat(
            EffectResolutionStepResult result,
            PendingAttackResolution attack)
        {
            if (!result.IsComplete || !result.RepeatRequested.IsRequested || attack == null)
            {
                return;
            }

            owedAttackRepeat = attack;
            owedAttackRepeatExclusion =
                result.RepeatRequested.Binding == RepeatTargetBinding.DifferentTarget
                    ? attack.Target
                    : default;
        }

        /// <summary>
        /// Milestone 8.10. Starts the Attack instance the finished one asked for, once that instance's
        /// page is closed.
        ///
        /// <para>The repeated instance is an ordinary Attack from here on: it opens its own target
        /// choice, takes its own Defense response, and runs p.13's consequences and p.14's order for
        /// itself. It may also refuse to start at all - a board with nobody left the card admits is a
        /// repeat that simply does not happen, because p.10 offers a repeat rather than owing one - and
        /// that is reported as a completed action rather than as a rejected command.</para>
        /// </summary>
        public CardActionResolutionResult StartOwedAttackRepeat(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var attack = owedAttackRepeat;
            var excluded = owedAttackRepeatExclusion;
            owedAttackRepeat = null;
            owedAttackRepeatExclusion = default;
            if (attack == null)
            {
                return CardActionResolutionResult.Accept(
                    true, null, null, Array.Empty<ResolutionStep>());
            }

            var repeated = attackDefenseBackend.StartRepeatedAttack(
                attack, excluded, definitions, instances, participants, modifications);
            return repeated.Accepted
                ? CardActionResolutionResult.Accept(false, null, null, repeated.Steps)
                : CardActionResolutionResult.Accept(true, null, null, Array.Empty<ResolutionStep>());
        }

        private void AppendRequestedHeroDefeats(
            EffectResolutionStepResult result,
            PendingAttackResolution attack,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> steps)
        {
            if (!result.IsComplete || result.RequestedHeroDefeats.Count == 0)
            {
                return;
            }

            var materializedInstances = instances as IReadOnlyList<CardInstance> ?? instances.ToArray();
            foreach (var heroId in result.RequestedHeroDefeats)
            {
                var defeat = attackDefenseBackend.ResolveEffectInvokedHeroDefeat(
                    attack, heroId, definitions, materializedInstances, participants, modifications,
                    steps);
                if (defeat == null)
                {
                    throw new InvalidOperationException(
                        $"Card text asked for hero '{heroId.Value}' to be defeated, and this " +
                        "composition cannot price a Defeat. A card that can reach p.13's list is " +
                        "dealt only into a match that binds the Levels and Life counters it spends.");
                }
            }
        }
        /// <summary>
        /// Milestone 8.10. Answers the question rulebook p.14's card text stopped on, and - when the
        /// text has finished - hands back the rest of the page.
        ///
        /// <para>The frame resumes through the same <c>EffectResolutionSession.Answer</c> a paused
        /// Skill resumes through, and the effect itself is resolved from the trusted card definitions
        /// rather than from anything the continuation carried, which is why the continuation carries
        /// identity alone. Text that asks a second question simply pauses again: the continuation is
        /// replaced and no steps are produced, so Wasp's riders - which choose a hero and then let that
        /// hero choose a card - stop twice inside one attack without either half being special.</para>
        ///
        /// <para>The steps returned when it finishes are
        /// <see cref="AttackDefenseCardActionStrategyBackend.StepsAfterCardText"/>'s, the same
        /// generator that emits them when nothing paused at all.</para>
        /// </summary>
        public CardActionResolutionResult AnswerCardTextChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (pendingCardText == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingResolution, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var effectRoot = SkillCardActionStrategyBackend.FindEffectRoot(
                materializedDefinitions, pendingCardText.Frame);
            if (effectRoot == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer, null, null);
            }

            var context = new EffectResolutionContext(
                effectRoot,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                ActionKind.Attack,
                components: components,
                heroItems: skillBackend.HeroItems,
                enclosingAttack: EnclosingAttackOf(pendingCardText.Attack, materializedDefinitions));
            var step = EffectResolutionSession.Answer(pendingCardText.Frame, command, context);
            if (!step.Accepted)
            {
                switch (step.RejectionReason)
                {
                    case EffectResolutionRejectionReason.StaleChoice:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.StaleChoice, null, null);
                    case EffectResolutionRejectionReason.WrongChooser:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.WrongChooser, null, null);
                    default:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.InvalidChoice, null, null);
                }
            }

            var paused = pendingCardText;
            var facts = step.Events
                .Select(value => CardActionResolutionEvent.CardTextApplied(
                    paused.Frame.SourceCardInstanceId,
                    paused.Frame.ActionDefinitionId,
                    paused.Frame.ActorId,
                    paused.Moment,
                    value))
                .Select(ResolutionStep.FromFact)
                .ToList();

            if (step.Frame?.PendingChoice != null)
            {
                pendingCardText = new PendingAttackCardTextResolution(
                    step.Frame, paused.Moment, paused.Attack, paused.Outcome);
                return CardActionResolutionResult.Accept(false, null, null, facts);
            }

            pendingCardText = null;
            AppendRequestedHeroDefeats(
                step, paused.Attack, materializedDefinitions, instances, participants, modifications,
                facts);
            RecordOwedAttackRepeat(step, paused.Attack);
            facts.AddRange(AttackDefenseCardActionStrategyBackend.StepsAfterCardText(
                paused.Moment, paused.Attack, paused.Outcome));
            return CardActionResolutionResult.Accept(true, null, null, facts);
        }

        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            // Read the identity before the answer: an answer that resolves the Attack clears the
            // pending resolution, and the request has to name the action instance that caused it.
            var resolutionId = attackDefenseBackend.PendingResolution?.ResolutionId ?? default;
            var result = attackDefenseBackend.AnswerAttackTarget(
                command, definitions, instances, participants, modifications);
            return RecordAttackLanePush(result, resolutionId, participants, modifications);
        }

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
            RuleModificationSet modifications)
        {
            var resolutionId = attackDefenseBackend.PendingResolution?.ResolutionId ?? default;
            var result = attackDefenseBackend.ResolveOutcome(
                definitions, instances, participants, modifications);
            return RecordAttackLanePush(result, resolutionId, participants, modifications);
        }

        /// <summary>
        /// Milestone 6.9: records the cause a completed Attack's Defeat stage reported.
        ///
        /// p.13's Defeat is not an effect node, so it cannot publish its answer into an effect frame the
        /// way p.19's Remove does; it carries a whole <c>LanePushQualification</c> on its resolution
        /// instead, and this is where that record becomes p.8's request. An Attack has no after-action
        /// correction boundary of its own - nothing it does displaces a minion - so the completion of
        /// its outcome is the boundary.
        ///
        /// Milestone 7.9c, finding F-BOARD-01: both Defeat arms are read. Only the minion arm was, so a
        /// hero Defeat's qualification was computed by the shared removal stage, recorded on the
        /// resolution, and consumed by nothing - and a hero that p.17 makes count as a minion is exactly
        /// the case where p.8's sentence applies to the hero arm. Each arm is asked separately rather
        /// than by picking one: an Attack resolves against a single target, so at most one is present,
        /// and <see cref="RecordImmediateLanePush"/> keeps the first request either way.
        /// </summary>
        private AttackActionResolutionResult RecordAttackLanePush(
            AttackActionResolutionResult result,
            ActionResolutionId resolutionId,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (!result.Accepted)
            {
                return result;
            }

            if (result.MinionDefeat != null)
            {
                RecordImmediateLanePush(
                    result.MinionDefeat.LanePush.Cause, resolutionId, participants, modifications);
            }

            if (result.HeroDefeat != null)
            {
                RecordImmediateLanePush(
                    result.HeroDefeat.LanePush.Cause, resolutionId, participants, modifications);
            }

            return result;
        }

        /// <summary>
        /// The one place a mid-turn Lane-Push request is produced.
        ///
        /// A conformance rule holds it to being the only one, in the shape milestone 6.6 used for p.8's
        /// question itself: moving, pushing, swapping, placing, spawning and replacing record no cause,
        /// so they cannot reach this, and nothing else can build a request.
        ///
        /// A request already held is never replaced. The turn is stopped from the moment the first one
        /// is recorded, so a second cause could only arise inside the same action instance, and p.8
        /// describes one push per emptied side rather than one per piece taken.
        /// </summary>
        /// <summary>
        /// Forgets the mid-turn request p.8's transition has just finished (milestone 6.11).
        ///
        /// Consuming it is the round's business rather than this authority's - the transition is a
        /// round-scoped mechanism and the request is only held here because the action that caused it
        /// was - so this says nothing about when, and only the checkpoint that ran the push calls it.
        /// A call with nothing held is a no-op rather than a failure, because the end-of-round
        /// checkpoint clears its own sibling request through the same path.
        /// </summary>
        internal void ConsumeImmediateLanePush()
        {
            pendingLanePush = null;
        }

        private void RecordImmediateLanePush(
            LanePushCauseKind cause,
            ActionResolutionId resolutionId,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (cause == LanePushCauseKind.None || pendingLanePush != null || !resolutionId.IsDefined)
            {
                return;
            }

            pendingLanePush = ImmediateLanePushQuery.From(
                cause, resolutionId, board, participants, modifications);
        }

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
