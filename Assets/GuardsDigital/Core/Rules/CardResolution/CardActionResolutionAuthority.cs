using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionAuthority
    {
        private readonly CardActionStrategyRegistry strategyRegistry;
        private readonly MovementCardActionStrategyBackend movementBackend;
        private readonly AttackDefenseCardActionStrategyBackend attackDefenseBackend;
        private readonly FastTravelCardActionStrategyBackend fastTravelBackend;

        public CardActionResolutionAuthority(AdjacentMoveAuthority adjacentMoveAuthority)
            : this(adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance)
        {
        }

        public CardActionResolutionAuthority(
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions)
            : this(
                new MovementCardActionStrategyBackend(
                    adjacentMoveAuthority,
                    movementActionStartRestrictions ?? throw new ArgumentNullException(
                        nameof(movementActionStartRestrictions))),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority))
        {
        }

        private CardActionResolutionAuthority(
            MovementCardActionStrategyBackend movementBackend,
            AttackDefenseCardActionStrategyBackend attackDefenseBackend,
            FastTravelCardActionStrategyBackend fastTravelBackend)
        {
            this.movementBackend = movementBackend ?? throw new ArgumentNullException(nameof(movementBackend));
            this.attackDefenseBackend = attackDefenseBackend ?? throw new ArgumentNullException(nameof(attackDefenseBackend));
            this.fastTravelBackend = fastTravelBackend ?? throw new ArgumentNullException(nameof(fastTravelBackend));
            strategyRegistry = new CardActionStrategyRegistry(new ICardActionStrategyHandler[]
            {
                new MovementCardActionStrategyHandler(movementBackend),
                new AttackDefenseCardActionStrategyHandler(attackDefenseBackend),
                new FastTravelCardActionStrategyHandler(fastTravelBackend)
            });
        }

        public PendingCardActionResolution PendingResolution => movementBackend.PendingResolution;

        public PendingMovementChoice PendingChoice => movementBackend.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => attackDefenseBackend.PendingResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => attackDefenseBackend.PendingTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => attackDefenseBackend.PendingDefenseChoice;

        public DefeatRewardAccounting DefeatRewards => attackDefenseBackend.DefeatRewards;

        public bool HasPendingResolution =>
            movementBackend.HasPendingResolution || attackDefenseBackend.HasPendingResolution;

        public static CardActionResolutionAuthority Restore(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice,
            DefeatRewardAccounting defeatRewards = null)
        {
            return new CardActionResolutionAuthority(
                MovementCardActionStrategyBackend.Restore(
                    adjacentMoveAuthority,
                    pendingResolution,
                    pendingChoice),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority));
        }

        internal static CardActionResolutionAuthority RestoreWithoutPendingAction(
            AdjacentMoveAuthority adjacentMoveAuthority,
            DefeatRewardAccounting defeatRewards)
        {
            return new CardActionResolutionAuthority(
                new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority));
        }

        public static CardActionResolutionAuthority Restore(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingCardActionResolution pendingResolution,
            DefeatRewardAccounting defeatRewards = null)
        {
            return new CardActionResolutionAuthority(
                MovementCardActionStrategyBackend.Restore(adjacentMoveAuthority, pendingResolution),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, defeatRewards),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority));
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

            return strategyRegistry.Dispatch<MovementCardActionStrategyHandler, CardActionResolutionResult>(
                ActionKind.Movement,
                handler => handler.Start(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances));
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

            return strategyRegistry.Dispatch<MovementCardActionStrategyHandler, CardActionResolutionResult>(
                ActionKind.Movement,
                handler => handler.StartTurn(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances));
        }

        public CardActionResolutionResult AnswerMovementChoice(MovementChoiceCommand command) =>
            strategyRegistry.Dispatch<MovementCardActionStrategyHandler, CardActionResolutionResult>(
                CardActionStrategyIds.Movement,
                handler => handler.Answer(command));

        public CardActionResolutionResult PerformTurnFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            if (HasPendingResolution)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    PendingResolution,
                    PendingChoice);
            }

            return strategyRegistry.Dispatch<FastTravelCardActionStrategyHandler, CardActionResolutionResult>(
                ActionKind.FastTravel,
                handler => handler.PerformTurn(
                    resolutionId,
                    replacementOffer,
                    replacedMovementActionDefinitionId,
                    destination,
                    definitions,
                    instances,
                    participants,
                    minions));
        }

        public AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
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

            return strategyRegistry.Dispatch<AttackDefenseCardActionStrategyHandler, AttackActionResolutionResult>(
                ActionKind.Attack,
                handler => handler.Start(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    participants));
        }

        public AttackActionResolutionResult StartTurnAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
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

            return strategyRegistry.Dispatch<AttackDefenseCardActionStrategyHandler, AttackActionResolutionResult>(
                ActionKind.Attack,
                handler => handler.StartTurn(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    participants));
        }

        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            strategyRegistry.Dispatch<AttackDefenseCardActionStrategyHandler, AttackActionResolutionResult>(
                CardActionStrategyIds.AttackDefense,
                handler => handler.AnswerTarget(command, definitions, instances, participants));

        public AttackActionResolutionResult AnswerDefenseResponse(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            strategyRegistry.Dispatch<AttackDefenseCardActionStrategyHandler, AttackActionResolutionResult>(
                CardActionStrategyIds.AttackDefense,
                handler => handler.AnswerDefense(command, definitions, instances, participants));

        public AttackActionResolutionResult ResolveAttackOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            strategyRegistry.Dispatch<AttackDefenseCardActionStrategyHandler, AttackActionResolutionResult>(
                ActionKind.Attack,
                handler => handler.ResolveOutcome(definitions, instances, participants));

        public static CardActionResolutionAuthority RestoreAttack(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            DefeatRewardAccounting defeatRewards = null)
        {
            return new CardActionResolutionAuthority(
                new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                AttackDefenseCardActionStrategyBackend.RestoreAttack(
                    adjacentMoveAuthority,
                    pendingResolution,
                    pendingTargetChoice,
                    pendingDefenseChoice,
                    definitions,
                    instances,
                    participants,
                    defeatRewards),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority));
        }

        public static CardActionResolutionAuthority RestoreAttack(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingAttackResolution pendingResolution,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            DefeatRewardAccounting defeatRewards = null)
        {
            return new CardActionResolutionAuthority(
                new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                AttackDefenseCardActionStrategyBackend.RestoreAttack(
                    adjacentMoveAuthority,
                    pendingResolution,
                    definitions,
                    instances,
                    participants,
                    defeatRewards),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority));
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

            if (FindActionDefinition(offer, materializedDefinitions).Kind != expectedKind)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnsupportedActionKind,
                    materializedDefinitions,
                    materializedInstances);
            }

            return PreparedStart.Accept(materializedDefinitions, materializedInstances);
        }

        private static ActionDefinition FindActionDefinition(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            var definition = definitionsByReference[offer.SourceDefinitionReference];
            return definition.ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));
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
