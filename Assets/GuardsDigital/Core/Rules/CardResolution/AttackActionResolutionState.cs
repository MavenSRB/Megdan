using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum AttackResolutionPhase
    {
        None = 0,
        ChooseTarget = 1,
        DefenseResponse = 2,
        CompareOutcome = 3,
        ResolveMinionDefeat = 4
    }

    public enum AttackResolutionOutcome
    {
        None = 0,
        SuccessfullyDefended = 1,
        AttackSuccessful = 2
    }

    public enum AttackTargetKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }

    public readonly struct AttackTarget : IEquatable<AttackTarget>
    {
        private AttackTarget(AttackTargetKind kind, HeroId heroId, MinionId minionId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
        }

        public AttackTargetKind Kind { get; }
        public HeroId HeroId { get; }
        public MinionId MinionId { get; }
        public bool IsDefined => Kind == AttackTargetKind.Hero ? HeroId.IsDefined :
            Kind == AttackTargetKind.Minion && MinionId.IsDefined;

        public static AttackTarget Hero(HeroId heroId)
        {
            if (!heroId.IsDefined) throw new ArgumentException("Attack hero target requires a defined ID.", nameof(heroId));
            return new AttackTarget(AttackTargetKind.Hero, heroId, default);
        }

        public static AttackTarget Minion(MinionId minionId)
        {
            if (!minionId.IsDefined) throw new ArgumentException("Attack minion target requires a defined ID.", nameof(minionId));
            return new AttackTarget(AttackTargetKind.Minion, default, minionId);
        }

        public bool Equals(AttackTarget other) =>
            Kind == other.Kind && HeroId.Equals(other.HeroId) && MinionId.Equals(other.MinionId);

        public override bool Equals(object obj) => obj is AttackTarget other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return (((int)Kind * 397) ^ HeroId.GetHashCode()) * 397 ^ MinionId.GetHashCode(); }
        }
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
            ValueResult attackValue,
            ActionOffer? defenseOffer = null,
            ValueResult defenseValue = null,
            AttackTarget excludedTarget = default)
            : this(resolutionId, sourceCardInstanceId, sourceDefinitionReference, actionDefinitionId,
                actorId, invocationContext, currentStepId, phase,
                targetHeroId.IsDefined ? AttackTarget.Hero(targetHeroId) : default,
                attackValue, defenseOffer, defenseValue, excludedTarget)
        {
        }

        public PendingAttackResolution(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionInvocationContext invocationContext,
            ActionResolutionStepId currentStepId,
            AttackResolutionPhase phase,
            AttackTarget target,
            ValueResult attackValue,
            ActionOffer? defenseOffer = null,
            ValueResult defenseValue = null,
            AttackTarget excludedTarget = default)
        {
            ExcludedTarget = excludedTarget;
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
                if (target.IsDefined || attackValue != null || defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException("Target-selection state cannot already contain a target or Attack Total.");
                }
            }
            else if (phase == AttackResolutionPhase.DefenseResponse)
            {
                if (target.Kind != AttackTargetKind.Hero || attackValue == null || attackValue.Channel != PrintedValueChannel.Attack ||
                    defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException("Defense-response state requires a target and typed Attack Total.");
                }
            }
            else if (phase == AttackResolutionPhase.CompareOutcome)
            {
                if (target.Kind != AttackTargetKind.Hero || attackValue == null || attackValue.Channel != PrintedValueChannel.Attack ||
                    defenseOffer.HasValue != (defenseValue != null) ||
                    (defenseValue != null && defenseValue.Channel != PrintedValueChannel.Defense) ||
                    (defenseOffer.HasValue &&
                        (!defenseOffer.Value.IsAvailable ||
                         !defenseOffer.Value.ActorId.Equals(target.HeroId) ||
                         defenseOffer.Value.InvocationContext != ActionInvocationContext.Reaction)))
                {
                    throw new ArgumentException("Outcome-comparison state requires a target, typed Attack Total, and an optional typed Defense response.");
                }
            }
            else if (phase == AttackResolutionPhase.ResolveMinionDefeat)
            {
                if (target.Kind != AttackTargetKind.Minion || attackValue != null ||
                    defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException("Minion Defeat state requires a minion target and no hero-only Attack/Defense totals.");
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
            Target = target;
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
        public AttackTarget Target { get; }
        public HeroId TargetHeroId => Target.HeroId;
        public MinionId TargetMinionId => Target.MinionId;

        /// <summary>
        /// Milestone 8.10. The unit this instance may not take, or the undefined target on every
        /// instance that may take anything.
        ///
        /// <para>Thunder Boomerang prints "may repeat once on a <b>different target</b>", and the
        /// exclusion belongs to the repeated instance rather than to the node that asked for it: the
        /// legal target set is re-derived whenever it is asked - when the choice opens, when the answer
        /// arrives, and again when a snapshot restores the pause - so what makes the repeat's set
        /// smaller has to be state the instance carries rather than a filter someone remembered to
        /// apply.</para>
        /// </summary>
        public AttackTarget ExcludedTarget { get; }
        public ValueResult AttackValue { get; }
        public int AttackTotal => AttackValue?.Total ?? 0;
        public ActionOffer? DefenseOffer { get; }
        public ValueResult DefenseValue { get; }
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
            : this(resolutionId, choiceId, chooserId,
                (legalTargetHeroIds ?? throw new ArgumentNullException(nameof(legalTargetHeroIds)))
                    .Select(AttackTarget.Hero))
        {
        }

        public PendingAttackTargetChoice(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId chooserId,
            IEnumerable<AttackTarget> legalTargets)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !chooserId.IsDefined)
            {
                throw new ArgumentException("Pending Attack target choice requires defined stable identities.");
            }

            if (legalTargets == null)
            {
                throw new ArgumentNullException(nameof(legalTargets));
            }

            var targets = legalTargets.ToArray();
            if (targets.Any(target => !target.IsDefined) || targets.Distinct().Count() != targets.Length)
            {
                throw new ArgumentException("Pending Attack target choice requires unique defined targets.", nameof(legalTargets));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            LegalTargets = Array.AsReadOnly(targets);
            LegalTargetHeroIds = Array.AsReadOnly(targets
                .Where(target => target.Kind == AttackTargetKind.Hero)
                .Select(target => target.HeroId).ToArray());
            LegalTargetMinionIds = Array.AsReadOnly(targets
                .Where(target => target.Kind == AttackTargetKind.Minion)
                .Select(target => target.MinionId).ToArray());
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public HeroId ChooserId { get; }
        public IReadOnlyList<HeroId> LegalTargetHeroIds { get; }
        public IReadOnlyList<MinionId> LegalTargetMinionIds { get; }
        public IReadOnlyList<AttackTarget> LegalTargets { get; }
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
            : this(resolutionId, choiceId, AttackTarget.Hero(targetHeroId))
        {
        }

        public AttackTargetChoiceCommand(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            MinionId targetMinionId)
            : this(resolutionId, choiceId, AttackTarget.Minion(targetMinionId))
        {
        }

        public AttackTargetChoiceCommand(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            AttackTarget target)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !target.IsDefined)
            {
                throw new ArgumentException("Attack target command requires defined stable identities.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            Target = target;
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public AttackTarget Target { get; }
        public HeroId TargetHeroId => Target.HeroId;
        public MinionId TargetMinionId => Target.MinionId;
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
