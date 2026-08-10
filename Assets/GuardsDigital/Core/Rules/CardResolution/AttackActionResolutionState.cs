using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum AttackResolutionPhase
    {
        None = 0,
        ChooseTarget = 1,
        DefenseResponse = 2,
        CompareOutcome = 3
    }

    public enum AttackResolutionOutcome
    {
        None = 0,
        SuccessfullyDefended = 1,
        AttackSuccessful = 2
    }

    public sealed class PendingAttackResolution
    {
        public PendingAttackResolution(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionInvocationContext invocationContext,
            ActionResolutionStepId currentStepId,
            AttackResolutionPhase phase,
            HeroId targetHeroId,
            PrintedValueResult attackValue,
            ActionOffer? defenseOffer = null,
            PrintedValueResult defenseValue = null)
        {
            if (!resolutionId.IsDefined ||
                !sourceCardInstanceId.IsDefined ||
                !sourceDefinitionReference.IsDefined ||
                !actionDefinitionId.IsDefined ||
                !actorId.IsDefined ||
                !currentStepId.IsDefined)
            {
                throw new ArgumentException("Pending Attack resolution requires defined stable identities.");
            }

            if (invocationContext != ActionInvocationContext.PrintedPrimary &&
                invocationContext != ActionInvocationContext.PrintedSecondary)
            {
                throw new ArgumentException("Pending Attack resolution requires a printed invocation context.", nameof(invocationContext));
            }

            if (phase == AttackResolutionPhase.ChooseTarget)
            {
                if (targetHeroId.IsDefined || attackValue != null || defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException("Target-selection state cannot already contain a target or Attack Total.");
                }
            }
            else if (phase == AttackResolutionPhase.DefenseResponse)
            {
                if (!targetHeroId.IsDefined || attackValue == null || attackValue.Channel != PrintedValueChannel.Attack ||
                    defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException("Defense-response state requires a target and typed Attack Total.");
                }
            }
            else if (phase == AttackResolutionPhase.CompareOutcome)
            {
                if (!targetHeroId.IsDefined || attackValue == null || attackValue.Channel != PrintedValueChannel.Attack ||
                    defenseOffer.HasValue != (defenseValue != null) ||
                    (defenseValue != null && defenseValue.Channel != PrintedValueChannel.Defense) ||
                    (defenseOffer.HasValue &&
                        (!defenseOffer.Value.IsAvailable ||
                         !defenseOffer.Value.ActorId.Equals(targetHeroId) ||
                         defenseOffer.Value.InvocationContext != ActionInvocationContext.Reaction)))
                {
                    throw new ArgumentException("Outcome-comparison state requires a target, typed Attack Total, and an optional typed Defense response.");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Pending Attack resolution requires a supported phase.");
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            CurrentStepId = currentStepId;
            Phase = phase;
            TargetHeroId = targetHeroId;
            AttackValue = attackValue;
            DefenseOffer = defenseOffer;
            DefenseValue = defenseValue;
        }

        public ActionResolutionId ResolutionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public HeroId ActorId { get; }
        public ActionInvocationContext InvocationContext { get; }
        public ActionResolutionStepId CurrentStepId { get; }
        public AttackResolutionPhase Phase { get; }
        public HeroId TargetHeroId { get; }
        public PrintedValueResult AttackValue { get; }
        public int AttackTotal => AttackValue?.Total ?? 0;
        public ActionOffer? DefenseOffer { get; }
        public PrintedValueResult DefenseValue { get; }
        public bool HasDefenseTotal => DefenseValue != null;
        public int? DefenseTotal => DefenseValue?.Total;
    }

    public sealed class PendingAttackTargetChoice
    {
        public PendingAttackTargetChoice(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId chooserId,
            IEnumerable<HeroId> legalTargetHeroIds)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !chooserId.IsDefined)
            {
                throw new ArgumentException("Pending Attack target choice requires defined stable identities.");
            }

            if (legalTargetHeroIds == null)
            {
                throw new ArgumentNullException(nameof(legalTargetHeroIds));
            }

            var targetIds = legalTargetHeroIds.ToArray();
            if (targetIds.Any(targetId => !targetId.IsDefined) || targetIds.Distinct().Count() != targetIds.Length)
            {
                throw new ArgumentException("Pending Attack target choice requires unique defined targets.", nameof(legalTargetHeroIds));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            LegalTargetHeroIds = Array.AsReadOnly(targetIds);
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public HeroId ChooserId { get; }
        public IReadOnlyList<HeroId> LegalTargetHeroIds { get; }
    }

    public sealed class PendingDefenseResponseChoice
    {
        public PendingDefenseResponseChoice(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId chooserId,
            HeroId attackerId,
            int attackTotal,
            IEnumerable<ActionOffer> legalDefenseOffers,
            bool canDecline)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !chooserId.IsDefined || !attackerId.IsDefined)
            {
                throw new ArgumentException("Pending Defense response choice requires defined stable identities.");
            }

            if (attackTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackTotal), attackTotal, "Attack Total cannot be negative.");
            }

            if (legalDefenseOffers == null)
            {
                throw new ArgumentNullException(nameof(legalDefenseOffers));
            }

            var offers = legalDefenseOffers.ToArray();
            if (offers.Any(offer =>
                !offer.IsAvailable ||
                !offer.ActorId.Equals(chooserId) ||
                offer.InvocationContext != ActionInvocationContext.Reaction))
            {
                throw new ArgumentException("Pending Defense response choice requires available defender-owned reaction offers.", nameof(legalDefenseOffers));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            AttackerId = attackerId;
            AttackTotal = attackTotal;
            LegalDefenseOffers = Array.AsReadOnly(offers);
            CanDecline = canDecline;
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public HeroId ChooserId { get; }
        public HeroId AttackerId { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<ActionOffer> LegalDefenseOffers { get; }
        public bool CanDecline { get; }
    }

    public readonly struct AttackTargetChoiceCommand
    {
        public AttackTargetChoiceCommand(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId targetHeroId)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !targetHeroId.IsDefined)
            {
                throw new ArgumentException("Attack target command requires defined stable identities.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            TargetHeroId = targetHeroId;
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public HeroId TargetHeroId { get; }
    }

    public enum DefenseResponseChoiceKind
    {
        None = 0,
        SelectCards = 1,
        Decline = 2
    }

    public sealed class DefenseResponseChoiceCommand
    {
        private DefenseResponseChoiceCommand(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            DefenseResponseChoiceKind choiceKind,
            IEnumerable<CardInstanceId> selectedCardInstanceIds)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("Defense response command requires defined resolution and choice IDs.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoiceKind = choiceKind;
            SelectedCardInstanceIds = Array.AsReadOnly(
                (selectedCardInstanceIds ?? throw new ArgumentNullException(nameof(selectedCardInstanceIds))).ToArray());
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public DefenseResponseChoiceKind ChoiceKind { get; }
        public IReadOnlyList<CardInstanceId> SelectedCardInstanceIds { get; }

        public static DefenseResponseChoiceCommand SelectCards(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            params CardInstanceId[] selectedCardInstanceIds)
        {
            return new DefenseResponseChoiceCommand(
                resolutionId,
                choiceId,
                DefenseResponseChoiceKind.SelectCards,
                selectedCardInstanceIds ?? Array.Empty<CardInstanceId>());
        }

        public static DefenseResponseChoiceCommand Decline(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId)
        {
            return new DefenseResponseChoiceCommand(
                resolutionId,
                choiceId,
                DefenseResponseChoiceKind.Decline,
                Array.Empty<CardInstanceId>());
        }
    }
}
