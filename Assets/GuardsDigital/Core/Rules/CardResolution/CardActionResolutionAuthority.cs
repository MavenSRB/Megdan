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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
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
        /// milestone 6.1r established. The marker register arrives from the caller rather than being
        /// created here whenever a live match is composing, so that the register this dispatcher hands to
        /// capabilities and the register the round authority persists are the same object.
        /// </summary>
        private static MatchComponents CreateComponents(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                adjacentMoveAuthority,
                markers ?? new MarkerAttachmentRegister());

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

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            skillBackend.PendingResolution;

        public GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice PendingEffectChoice =>
            skillBackend.PendingChoice;

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
            skillBackend.HasPendingResolution;

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
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

            return skillBackend.StartTurnSkill(
                resolutionId, offer, prepared.Definitions, prepared.Instances, participants,
                timing, components, roundIndex, turnIndex);
        }

        public CardActionResolutionResult AnswerEffectChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            int roundIndex,
            int turnIndex) =>
            skillBackend.AnswerEffectChoice(
                command, definitions, participants, timing, components, roundIndex, turnIndex);

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

            return PendingSkillResolution != null
                ? CardActionResolutionRestoreState.WithPendingSkill(PendingSkillResolution, rewards)
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
            RuleModificationSet modifications = null)
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
                            verifiedMovementChoice),
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
            IEnumerable<CardInstance> instances)
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
                    prepared.Instances);
        }

        public CardActionResolutionResult StartTurnMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
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
                    prepared.Instances);
        }

        public CardActionResolutionResult AnswerMovementChoice(MovementChoiceCommand command) =>
            movementBackend.AnswerMovement(command);

        public CardActionResolutionResult PerformTurnFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications = null)
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
            RuleModificationSet modifications = null)
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
            RuleModificationSet modifications = null)
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
            RuleModificationSet modifications = null) =>
            attackDefenseBackend.AnswerAttackTarget(
                command, definitions, instances, participants, modifications);

        public AttackActionResolutionResult AnswerDefenseResponse(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications = null) =>
            attackDefenseBackend.AnswerDefense(
                command, definitions, instances, participants, modifications);

        public AttackActionResolutionResult ResolveAttackOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications = null) =>
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
