using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
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
        ResolveMinionDefeat = 4,

        /// <summary>
        /// Milestone 8.19a. Rulebook p.14's step 1 is being applied, and the rest of the page is owed.
        ///
        /// <para>It is a phase of its own because the page genuinely stops there now. Before this
        /// milestone every step from the target's validation to the Defense request was produced in the
        /// command that answered the target, so there was no moment between them for a state to be in;
        /// text that could have asked a question at step 1 was refused at content load instead. Two of
        /// the roster's printed shapes ask one - Arien's Noble Blade moves a unit beside the target,
        /// and Brogan's Mad Dash charges and only then names what it attacks - so the moment has to
        /// exist, and a state that can be snapshotted in it has to exist with it.</para>
        ///
        /// <para>A resolution in this phase carries its target when the Attack chose one at its start
        /// and no target at all when the card declares that its text binds one. It never carries an
        /// Attack Total: p.14 announces that at step 2, which is on the far side of this moment.</para>
        /// </summary>
        ApplyBeforeAttackText = 5
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
            AttackTarget excludedTarget = default,
            DefenseBlockResult blockResult = DefenseBlockResult.None,
            HexCoordinate? targetSpaceBeforeConsequences = null,
            HexCoordinate? actorSpaceAtActionStart = null)
            : this(resolutionId, sourceCardInstanceId, sourceDefinitionReference, actionDefinitionId,
                actorId, invocationContext, currentStepId, phase,
                targetHeroId.IsDefined ? AttackTarget.Hero(targetHeroId) : default,
                attackValue, defenseOffer, defenseValue, excludedTarget, blockResult,
                targetSpaceBeforeConsequences, actorSpaceAtActionStart)
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
            AttackTarget excludedTarget = default,
            DefenseBlockResult blockResult = DefenseBlockResult.None,
            HexCoordinate? targetSpaceBeforeConsequences = null,
            HexCoordinate? actorSpaceAtActionStart = null)
        {
            ExcludedTarget = excludedTarget;
            BlockResult = blockResult;
            TargetSpaceBeforeConsequences = targetSpaceBeforeConsequences;
            ActorSpaceAtActionStart = actorSpaceAtActionStart;
            if (targetSpaceBeforeConsequences.HasValue && !target.IsDefined)
            {
                throw new ArgumentException(
                    "A pre-consequence target space belongs to a target; an Attack with none has no " +
                    "space to record.",
                    nameof(targetSpaceBeforeConsequences));
            }

            if (!Enum.IsDefined(typeof(DefenseBlockResult), blockResult))
            {
                throw new ArgumentException(
                    "A pending Attack resolution states what rulebook p.14's Block did, or that no " +
                    "Block was involved.",
                    nameof(blockResult));
            }
            if (!resolutionId.IsDefined ||
                !sourceCardInstanceId.IsDefined ||
                !sourceDefinitionReference.IsDefined ||
                !actionDefinitionId.IsDefined ||
                !actorId.IsDefined ||
                !currentStepId.IsDefined)
            {
                throw new ArgumentException("Pending Attack resolution requires defined stable identities.");
            }

            // Milestone 8.25a admits the third. p.10's repeated instance of an Attack is that Attack
            // performed again, and the context is how it says so: the ActionEnded checkpoint reports
            // it, `PendingAfterActionRepeat.For` reads it, and that is what makes "You cannot repeat a
            // repeated action!" true of an Attack. Milestone 8.23's after-action arm and milestone
            // 8.24's `on a different enemy hero` narrowing were both already written against a
            // repeated Attack resolution; this guard is what neither of them could get past, and the
            // 8.23 arm threw here the first time a match reached it.
            if (invocationContext != ActionInvocationContext.PrintedPrimary &&
                invocationContext != ActionInvocationContext.PrintedSecondary &&
                invocationContext != ActionInvocationContext.Repeated)
            {
                throw new ArgumentException(
                    "Pending Attack resolution requires a printed or repeated invocation context.",
                    nameof(invocationContext));
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
                // Milestone 8.11. p.14's Block prints no Defense value and enters no comparison, so a
                // defense offer with no total is exactly right for one and malformed for anything else
                // - which is why the Block says so rather than being inferred from the absent value.
                var blockAccountsForTheMissingTotal =
                    blockResult != DefenseBlockResult.None && defenseOffer.HasValue && defenseValue == null;
                if (target.Kind != AttackTargetKind.Hero || attackValue == null || attackValue.Channel != PrintedValueChannel.Attack ||
                    (!blockAccountsForTheMissingTotal && defenseOffer.HasValue != (defenseValue != null)) ||
                    (defenseValue != null && defenseValue.Channel != PrintedValueChannel.Defense) ||
                    (defenseOffer.HasValue &&
                        (!defenseOffer.Value.IsAvailable ||
                         !defenseOffer.Value.ActorId.Equals(target.HeroId) ||
                         defenseOffer.Value.InvocationContext != ActionInvocationContext.Reaction)))
                {
                    throw new ArgumentException("Outcome-comparison state requires a target, typed Attack Total, and an optional typed Defense response.");
                }
            }
            else if (phase == AttackResolutionPhase.ApplyBeforeAttackText)
            {
                if (attackValue != null || defenseOffer.HasValue || defenseValue != null)
                {
                    throw new ArgumentException(
                        "Rulebook p.14 announces the Attack Total at step 2, so a resolution still " +
                        "applying step 1's text carries no total and no Defense response.");
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

        /// <summary>
        /// Milestone 8.11. What rulebook p.14's Block did, or <see cref="DefenseBlockResult.None"/>
        /// wherever no Block was performed - which is every attack in this build before Wasp's three
        /// Green cards. It is decided when the defender's card is revealed, at the page's step 3, and
        /// read at step 6, so it crosses the pause between them.
        /// </summary>
        public DefenseBlockResult BlockResult { get; }

        /// <summary>
        /// Milestone 8.19a. Where this Attack's target stood when the Attack bound it, or nothing where
        /// it has no target yet.
        ///
        /// <para>It is recorded because rulebook p.14 removes a defeated target at step 6 and applies
        /// "After the attack:" text at step 7, so the space a rider names is a space nothing is standing
        /// on by the time the rider runs. Brogan's Onslaught moves into exactly that space and Sabina's
        /// Point Blank Shot asks whether the target is still beside her, so the fact has to survive the
        /// consequences - and, since either rider may pause, a snapshot as well.</para>
        /// </summary>
        public HexCoordinate? TargetSpaceBeforeConsequences { get; }

        /// <summary>
        /// Milestone 8.23. Where the attacking hero stood when this Attack started, or nothing for an
        /// Attack whose card asks nothing about it.
        ///
        /// <para>Tigerclaw's Combat Reflexes is what needs it: "Before the attack: You may move 1
        /// space. ... After the attack: <b>If you did not move before the attack</b>, you may move 1
        /// space." The two clauses are p.14's step 1 and step 7, six numbered steps and the whole of
        /// the defender's response apart, and the frame that offered the first move is long gone by
        /// the time the second clause asks about it - so the fact has to be a property of the Attack
        /// rather than of either text, and has to survive every pause between them.</para>
        ///
        /// <para>It is the space rather than a "did the step apply" flag because that is what the
        /// printed question is about, and because a flag would have to be set by whichever node
        /// happened to move the hero. Nothing else in verified content relocates an attacker between
        /// those two steps - a Block's rider moves the <em>defender</em> - so the space and the
        /// clause answer the same question, and a later face that changes that would be read against
        /// this comment rather than silently inheriting it.</para>
        /// </summary>
        public HexCoordinate? ActorSpaceAtActionStart { get; }

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
