using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct CardActionResolutionEvent : IEquatable<CardActionResolutionEvent>
    {
        private CardActionResolutionEvent(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionDefinitionId replacedActionDefinitionId,
            HeroId actorId,
            HeroId targetHeroId,
            ActionResolutionStepId stepId,
            int remainingAllowance,
            int attackTotal,
            CardInstanceId defenseCardInstanceId,
            int defenseTotal,
            AttackResolutionOutcome attackOutcome,
            AdjacentMoveEvent movementEvent,
            FastTravelEvent fastTravelEvent,
            MinionDefeatFactKind minionDefeatFactKind = MinionDefeatFactKind.None,
            MinionId targetMinionId = default,
            DefeatOutcomeId defeatOutcomeId = default)
        {
            if (kind == CardActionResolutionEventKind.None)
            {
                throw new ArgumentException("Card action resolution event requires a kind.", nameof(kind));
            }

            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a resolution ID.", nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an action definition ID.", nameof(actionDefinitionId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an actor ID.", nameof(actorId));
            }

            if (!stepId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a step ID.", nameof(stepId));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ReplacedActionDefinitionId = replacedActionDefinitionId;
            ActorId = actorId;
            TargetHeroId = targetHeroId;
            StepId = stepId;
            RemainingAllowance = remainingAllowance;
            AttackTotal = attackTotal;
            DefenseCardInstanceId = defenseCardInstanceId;
            DefenseTotal = defenseTotal;
            AttackOutcome = attackOutcome;
            MovementEvent = movementEvent;
            FastTravelEvent = fastTravelEvent;
            MinionDefeatFactKind = minionDefeatFactKind;
            TargetMinionId = targetMinionId;
            DefeatOutcomeId = defeatOutcomeId;
        }

        public CardActionResolutionEventKind Kind { get; }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public ActionDefinitionId ReplacedActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public HeroId TargetHeroId { get; }

        public ActionResolutionStepId StepId { get; }

        public int RemainingAllowance { get; }

        public int AttackTotal { get; }

        public CardInstanceId DefenseCardInstanceId { get; }

        public int DefenseTotal { get; }

        public AttackResolutionOutcome AttackOutcome { get; }

        public AdjacentMoveEvent MovementEvent { get; }

        public FastTravelEvent FastTravelEvent { get; }
        public MinionDefeatFactKind MinionDefeatFactKind { get; }
        public MinionId TargetMinionId { get; }
        public DefeatOutcomeId DefeatOutcomeId { get; }

        public static CardActionResolutionEvent MovementActionStarted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionStarted, resolution, default);
        }

        public static CardActionResolutionEvent MovementChoiceRequested(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementChoiceRequested, resolution, default);
        }

        public static CardActionResolutionEvent MovementStepCommitted(
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            return FromResolution(CardActionResolutionEventKind.MovementStepCommitted, resolution, movementEvent);
        }

        public static CardActionResolutionEvent MovementActionCompleted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionCompleted, resolution, default);
        }

        public static CardActionResolutionEvent FastTravelActionStarted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelActionStarted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-start");

        public static CardActionResolutionEvent FastTravelCommitted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelCommitted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-commit");

        public static CardActionResolutionEvent FastTravelActionCompleted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelActionCompleted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-complete");

        public static CardActionResolutionEvent AttackTargetChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTargetChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent AttackStarted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackStarted, resolution);
        }

        public static CardActionResolutionEvent AttackTargetValidated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTargetValidated, resolution);
        }

        public static CardActionResolutionEvent AttackTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent DefenseCardDiscarded(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseCardDiscarded, resolution);
        }

        public static CardActionResolutionEvent DefenseTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseDeclined(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseDeclined, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseCompleted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseCompleted, resolution);
        }

        public static CardActionResolutionEvent AttackOutcomeDetermined(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            if (outcome != AttackResolutionOutcome.SuccessfullyDefended &&
                outcome != AttackResolutionOutcome.AttackSuccessful)
            {
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Attack outcome must be final.");
            }

            var kind = outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? CardActionResolutionEventKind.AttackSuccessfullyDefended
                : CardActionResolutionEventKind.AttackSuccessful;
            return FromAttackResolution(kind, resolution, outcome);
        }

        public static CardActionResolutionEvent AttackResolutionCompleted(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            return FromAttackResolution(
                CardActionResolutionEventKind.AttackResolutionCompleted,
                resolution,
                outcome);
        }

        public static CardActionResolutionEvent HeroDefeatRecorded(
            PendingAttackResolution resolution,
            HeroDefeatResolution defeat)
        {
            if (resolution == null || defeat == null ||
                !resolution.TargetHeroId.IsDefined ||
                !string.Equals(defeat.Defeat.TargetId.Value, resolution.TargetHeroId.Value, StringComparison.Ordinal))
            {
                throw new ArgumentException("Hero Defeat event requires the Attack's exact hero target.");
            }

            return new CardActionResolutionEvent(
                CardActionResolutionEventKind.HeroDefeatRecorded,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                resolution.TargetHeroId,
                resolution.CurrentStepId,
                0,
                resolution.AttackTotal,
                resolution.DefenseOffer.HasValue
                    ? resolution.DefenseOffer.Value.SourceCardInstanceId
                    : default,
                resolution.DefenseTotal ?? 0,
                AttackResolutionOutcome.AttackSuccessful,
                default,
                default,
                MinionDefeatFactKind.None,
                default,
                defeat.Defeat.Id);
        }

        public static CardActionResolutionEvent MinionDefeatFact(
            PendingAttackResolution resolution,
            MinionDefeatResolution defeat,
            MinionDefeatFactKind factKind)
        {
            if (resolution == null || defeat == null ||
                !defeat.Facts.Contains(factKind) || !resolution.TargetMinionId.IsDefined)
            {
                throw new ArgumentException("Minion Defeat event requires one recorded typed fact.");
            }

            CardActionResolutionEventKind eventKind;
            switch (factKind)
            {
                case MinionDefeatFactKind.DefeatRecorded: eventKind = CardActionResolutionEventKind.MinionDefeatRecorded; break;
                case MinionDefeatFactKind.RewardAwarded: eventKind = CardActionResolutionEventKind.MinionRewardAwarded; break;
                case MinionDefeatFactKind.RemovalAttempted: eventKind = CardActionResolutionEventKind.MinionRemovalAttempted; break;
                case MinionDefeatFactKind.RemovalCompleted: eventKind = CardActionResolutionEventKind.MinionRemovalCompleted; break;
                default: throw new ArgumentOutOfRangeException(nameof(factKind));
            }

            return new CardActionResolutionEvent(
                eventKind, resolution.ResolutionId, resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId, default, resolution.ActorId, default,
                resolution.CurrentStepId, 0, resolution.AttackTotal, default, 0,
                AttackResolutionOutcome.AttackSuccessful, default, default,
                factKind, resolution.TargetMinionId, defeat.Defeat.Id);
        }

        public bool Equals(CardActionResolutionEvent other)
        {
            return Kind == other.Kind &&
                ResolutionId.Equals(other.ResolutionId) &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ReplacedActionDefinitionId.Equals(other.ReplacedActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                TargetHeroId.Equals(other.TargetHeroId) &&
                StepId.Equals(other.StepId) &&
                RemainingAllowance == other.RemainingAllowance &&
                AttackTotal == other.AttackTotal &&
                DefenseCardInstanceId.Equals(other.DefenseCardInstanceId) &&
                DefenseTotal == other.DefenseTotal &&
                AttackOutcome == other.AttackOutcome &&
                MovementEvent.Equals(other.MovementEvent) &&
                FastTravelEvent.Equals(other.FastTravelEvent) &&
                MinionDefeatFactKind == other.MinionDefeatFactKind &&
                TargetMinionId.Equals(other.TargetMinionId) &&
                DefeatOutcomeId.Equals(other.DefeatOutcomeId);
        }

        public override bool Equals(object obj)
        {
            return obj is CardActionResolutionEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ ResolutionId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ReplacedActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ RemainingAllowance;
                hashCode = (hashCode * 397) ^ AttackTotal;
                hashCode = (hashCode * 397) ^ DefenseCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ DefenseTotal;
                hashCode = (hashCode * 397) ^ (int)AttackOutcome;
                hashCode = (hashCode * 397) ^ MovementEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ FastTravelEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MinionDefeatFactKind;
                hashCode = (hashCode * 397) ^ TargetMinionId.GetHashCode();
                return (hashCode * 397) ^ DefeatOutcomeId.GetHashCode();
            }
        }

        private static CardActionResolutionEvent FromResolution(
            CardActionResolutionEventKind kind,
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                default,
                resolution.CurrentStepId,
                resolution.RemainingAllowance,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                movementEvent,
                default);
        }

        /// <summary>
        /// Milestone 6.2a. Skill facts name the effect node they concern through the ordinary step
        /// identity, and say what happened through the event kind rather than by packing an outcome
        /// into a field that means something else.
        /// </summary>
        public static CardActionResolutionEvent SkillActionStarted(
            ActionResolutionId resolutionId,
            ActionOffer offer) =>
            Skill(
                CardActionResolutionEventKind.SkillActionStarted,
                resolutionId,
                offer.SourceCardInstanceId,
                offer.ActionDefinitionId,
                offer.ActorId,
                new ActionResolutionStepId("skill-start"));

        public static CardActionResolutionEvent SkillEffectNode(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            string nodeId)
        {
            if (kind != CardActionResolutionEventKind.SkillEffectNodeApplied &&
                kind != CardActionResolutionEventKind.SkillEffectNodeDeclined &&
                kind != CardActionResolutionEventKind.SkillEffectNodeStopped)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Not a Skill effect-node fact kind.");
            }

            return Skill(
                kind,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId(nodeId));
        }

        public static CardActionResolutionEvent SkillChoiceRequested(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            string choiceId) =>
            Skill(
                CardActionResolutionEventKind.SkillChoiceRequested,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId(choiceId));

        public static CardActionResolutionEvent SkillActionCompleted(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId) =>
            Skill(
                CardActionResolutionEventKind.SkillActionCompleted,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId("skill-complete"));

        private static CardActionResolutionEvent Skill(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionResolutionStepId stepId) =>
            new CardActionResolutionEvent(
                kind,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                default,
                actorId,
                default,
                stepId,
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                default);

        private static CardActionResolutionEvent FromAttackResolution(
            CardActionResolutionEventKind kind,
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                resolution.TargetHeroId,
                resolution.CurrentStepId,
                0,
                resolution.AttackTotal,
                resolution.DefenseOffer.HasValue
                    ? resolution.DefenseOffer.Value.SourceCardInstanceId
                    : default,
                resolution.DefenseTotal ?? 0,
                outcome,
                default,
                default);
        }

        private static CardActionResolutionEvent FromFastTravel(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent,
            string stepId)
        {
            if (offer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedActionDefinitionId.IsDefined ||
                !fastTravelEvent.HeroId.Equals(offer.ActorId))
            {
                throw new ArgumentException("A Fast Travel event requires its replacement offer, replaced action, and actor move.");
            }

            return new CardActionResolutionEvent(
                kind,
                resolutionId,
                offer.SourceCardInstanceId,
                offer.ActionDefinitionId,
                replacedActionDefinitionId,
                offer.ActorId,
                default,
                new ActionResolutionStepId(stepId),
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                fastTravelEvent);
        }
    }
}
