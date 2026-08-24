using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>Which Defeat is standing at its removal stage.</summary>
    public enum RemovalPreventionOrigin
    {
        None = 0,

        /// <summary>
        /// Rulebook p.14's own minion arm, which p.13 states as "when you target an enemy minion with
        /// an attack, it is simply defeated". The remainder owed is the rest of the page.
        /// </summary>
        AttackMinionDefeat = 1,

        /// <summary>
        /// p.13's Defeat run by card text rather than by an Attack - milestone 8.10's effect-invoked
        /// Defeat. The remainder owed is the Defeat's own facts and p.8's recorded cause.
        /// </summary>
        EffectInvokedMinionDefeat = 2
    }

    /// <summary>
    /// Milestone 8.19c1. A Defeat stopped at its removal stage, because a trusted source offered to
    /// stop the removal at a price and the hero controlling it has not answered yet.
    ///
    /// <para><b>Where it stands.</b> Rulebook p.13's Defeat has already been recorded and its reward
    /// has already been awarded - the coins have moved - and the miniature is still on the board. That
    /// is the page's own ordering, stated in its Bolster example: the removal is prevented and the
    /// attacking hero collects the coins for the Defeat in the same breath. Only the removal is
    /// outstanding, which is why this continuation carries the Defeat and not a half-run one.</para>
    ///
    /// <para><b>What it carries and what it re-derives.</b> The Defeat, the reward the ledger actually
    /// paid, the subject, the hero deciding, and the identity the remainder is rebuilt from. It carries
    /// no options and no candidate cards: which sources are still offering, and which cards could still
    /// pay them, are questions about live state, so they are asked again when the answer arrives -
    /// exactly as milestone 6.8l re-derives a paused selection's eligibility rather than trusting the
    /// offer. A hand that changed between the question and the answer therefore changes the answer's
    /// legality, which is what it should do.</para>
    ///
    /// <para>The reward's coin operations are carried because they cannot be re-derived: they record
    /// the balances before and after a payment that has already happened, and re-running the award
    /// against the ledger would be refused as a second payment for one outcome.</para>
    /// </summary>
    public sealed class PendingRemovalPreventionDecision
    {
        public PendingRemovalPreventionDecision(
            RemovalPreventionOrigin origin,
            PendingAttackResolution resolution,
            MinionId subjectMinionId,
            DefeatOutcome defeat,
            IEnumerable<CoinResourceOperation> rewardOperations,
            HeroId decidingHeroId,
            GuardsDigital.Core.Rules.Modifications.RuleActivity activity)
        {
            if (origin == RemovalPreventionOrigin.None ||
                !Enum.IsDefined(typeof(RemovalPreventionOrigin), origin))
            {
                throw new ArgumentException(
                    "A removal-prevention decision states which Defeat is standing at its removal " +
                    "stage, because the two owe different remainders.",
                    nameof(origin));
            }

            if (resolution == null)
            {
                throw new ArgumentNullException(
                    nameof(resolution),
                    "A removal-prevention decision names the resolution whose remainder it owes.");
            }

            if (!subjectMinionId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention decision names the miniature still standing on the board.",
                    nameof(subjectMinionId));
            }

            if (defeat == null) throw new ArgumentNullException(nameof(defeat));
            if (!defeat.HasRewardAmounts)
            {
                throw new ArgumentException(
                    "A removal-prevention decision stands after the reward, so its Defeat carries the " +
                    "amounts that were paid.",
                    nameof(defeat));
            }

            if (!decidingHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention decision names the hero rulebook p.10 gives it to.",
                    nameof(decidingHeroId));
            }

            if (activity == GuardsDigital.Core.Rules.Modifications.RuleActivity.None)
            {
                throw new ArgumentException(
                    "A removal stage is performed by a named activity (p.18).", nameof(activity));
            }

            var operations =
                (rewardOperations ?? throw new ArgumentNullException(nameof(rewardOperations))).ToArray();
            if (operations.Any(operation => !operation.SourceOutcomeId.Equals(defeat.Id)))
            {
                throw new ArgumentException(
                    "Every carried coin operation belongs to the Defeat this decision stands inside.",
                    nameof(rewardOperations));
            }

            Origin = origin;
            Resolution = resolution;
            SubjectMinionId = subjectMinionId;
            Defeat = defeat;
            RewardOperations = new ReadOnlyCollection<CoinResourceOperation>(operations);
            DecidingHeroId = decidingHeroId;
            Activity = activity;
        }

        public RemovalPreventionOrigin Origin { get; }

        /// <summary>The resolution the remainder is produced from, exactly as milestone 8.10's card-text
        /// continuation carries the attack whose page it owes.</summary>
        public PendingAttackResolution Resolution { get; }

        public MinionId SubjectMinionId { get; }

        /// <summary>p.13's recorded Defeat, complete and already rewarded.</summary>
        public DefeatOutcome Defeat { get; }

        /// <summary>The coin operations the ledger performed for that Defeat.</summary>
        public IReadOnlyList<CoinResourceOperation> RewardOperations { get; }

        /// <summary>The hero whose sources are offering, and who answers.</summary>
        public HeroId DecidingHeroId { get; }

        /// <summary>The p.18 activity the removal stage was asked under.</summary>
        public GuardsDigital.Core.Rules.Modifications.RuleActivity Activity { get; }

        /// <summary>
        /// The decision's stable identity, composed from the resolution and the subject rather than
        /// stored. One Defeat has one removal stage, so nothing else could produce the same string, and
        /// a restore reproduces it without carrying it.
        /// </summary>
        public string RequestId =>
            $"{Resolution.ResolutionId.Value}:removal-prevention:{SubjectMinionId.Value}";

        /// <summary>The reward as the ledger reported it, rebuilt from the operations it performed.</summary>
        public DefeatRewardResult Reward => DefeatRewardResult.Accept(RewardOperations);
    }
}
