using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.22. The action a rulebook p.13 Defeat is being run <em>about</em>, when the Defeat
    /// was asked for by an effect rather than by an Attack's own target.
    ///
    /// <para><b>Why this exists.</b> p.13's list is one chain, deliberately - milestone 6.6b closed the
    /// disagreement that two copies produced - and that chain has always been reached through a
    /// <see cref="PendingAttackResolution"/>, because until now every printed text that could escalate
    /// into it was printed on an Attack. Wasp's Electroblast and Brogan's Counterattack are Attack
    /// text; Sabina's Covering Fire is a <em>Skill</em>, and a Skill has no Attack resolution to name.
    /// What the chain actually needs is smaller than an Attack: an identity for the events, the hero
    /// whose action it is, and the activity p.18 judges the removal under. That is this.</para>
    ///
    /// <para><b>The activity is not decoration.</b> p.18 asks immunity about the activity being
    /// performed, so a Defeat asked for by a Skill has to be judged as a Skill. Reading
    /// <see cref="RuleActivity.Attack"/> off a shared helper would have made a Skill's Defeat answer an
    /// Attack's question, which is the same class of silent substitution the shared chain exists to
    /// prevent.</para>
    ///
    /// <para>The Attack's own fields - its total, the Defense card and total - are carried because the
    /// events that report a Defeat inside an Attack have always carried them, and an effect-invoked
    /// Defeat outside one carries their named absence instead.</para>
    /// </summary>
    public readonly struct EffectInvokedDefeatSource : IEquatable<EffectInvokedDefeatSource>
    {
        public EffectInvokedDefeatSource(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionResolutionStepId currentStepId,
            RuleActivity activity,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None,
            int attackTotal = 0,
            CardInstanceId defenseCardInstanceId = default,
            int defenseTotal = 0)
        {
            if (!resolutionId.IsDefined || !actorId.IsDefined || !currentStepId.IsDefined)
            {
                throw new ArgumentException(
                    "An effect-invoked Defeat names the resolution, the acting hero and the step it " +
                    "is being run at.");
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException(
                    "An effect-invoked Defeat states the activity rulebook p.18 judges its removal " +
                    "under.",
                    nameof(activity));
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            CurrentStepId = currentStepId;
            Activity = activity;
            Outcome = outcome;
            AttackTotal = attackTotal;
            DefenseCardInstanceId = defenseCardInstanceId;
            DefenseTotal = defenseTotal;
        }

        public ActionResolutionId ResolutionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public HeroId ActorId { get; }
        public ActionResolutionStepId CurrentStepId { get; }

        /// <summary>The activity rulebook p.18 asks its immunity question about.</summary>
        public RuleActivity Activity { get; }

        public AttackResolutionOutcome Outcome { get; }
        public int AttackTotal { get; }
        public CardInstanceId DefenseCardInstanceId { get; }
        public int DefenseTotal { get; }

        public bool IsDefined => ResolutionId.IsDefined;

        /// <summary>
        /// The Attack this Defeat is being run inside, which is every printing of the construction
        /// before Sabina's two Green Tier II/III faces.
        /// </summary>
        public static EffectInvokedDefeatSource FromAttack(PendingAttackResolution attack)
        {
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            return new EffectInvokedDefeatSource(
                attack.ResolutionId,
                attack.SourceCardInstanceId,
                attack.ActionDefinitionId,
                attack.ActorId,
                attack.CurrentStepId,
                RuleActivity.Attack,
                AttackResolutionOutcome.AttackSuccessful,
                attack.AttackTotal,
                attack.DefenseOffer.HasValue
                    ? attack.DefenseOffer.Value.SourceCardInstanceId
                    : default,
                attack.DefenseTotal ?? 0);
        }

        /// <summary>
        /// The Skill this Defeat is being run inside. No Attack happened, so the page's numbers are
        /// absent rather than zero-by-accident, and p.18's question is asked about a Skill.
        /// </summary>
        public static EffectInvokedDefeatSource FromSkill(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId) =>
            new EffectInvokedDefeatSource(
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId("effect-invoked-hero-defeat"),
                RuleActivity.Skill);

        public bool Equals(EffectInvokedDefeatSource other) =>
            ResolutionId.Equals(other.ResolutionId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            ActionDefinitionId.Equals(other.ActionDefinitionId) &&
            ActorId.Equals(other.ActorId) &&
            CurrentStepId.Equals(other.CurrentStepId) &&
            Activity == other.Activity &&
            Outcome == other.Outcome &&
            AttackTotal == other.AttackTotal &&
            DefenseCardInstanceId.Equals(other.DefenseCardInstanceId) &&
            DefenseTotal == other.DefenseTotal;

        public override bool Equals(object obj) =>
            obj is EffectInvokedDefeatSource other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ResolutionId.GetHashCode();
                hash = (hash * 397) ^ ActorId.GetHashCode();
                hash = (hash * 397) ^ CurrentStepId.GetHashCode();
                hash = (hash * 397) ^ (int)Activity;
                return hash;
            }
        }

        public override string ToString() =>
            $"{ResolutionId.Value}:{ActorId.Value}:{Activity}";
    }
}
