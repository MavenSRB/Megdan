using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z31. Ursafar's Tear - "if you defeated a hero, that hero spends 1
    /// additional [Life] Life counter" - and the ninth modification family it forced.
    ///
    /// <para>This touches a victory condition, so what it must get right is not that a counter is added
    /// but that it is added <b>once</b>, into the number rulebook p.13 already computes. The user's
    /// 2026-09-06 ruling settles the arithmetic: the cost is Tier + 1, computed once and spent once at
    /// the defeat, with the card's "After the attack:" placement saying when the condition is checked
    /// rather than when the counter is paid. A team on its last counters therefore loses them in the
    /// single combined spend, rather than losing the last one to p.13 and never reaching the clause.
    /// </para>
    /// </summary>
    public sealed class AdditionalDefeatLifeCostTests
    {
        private static readonly HeroId Attacker = new HeroId("ursafar-1");
        private static readonly HeroId Bystander = new HeroId("ally-1");

        [TestCase(CardTier.I, 1)]
        [TestCase(CardTier.II, 2)]
        [TestCase(CardTier.III, 3)]
        public void WithNoDeclaration_TheChargeIsExactlyThePagesOwnNumber(CardTier tier, int expected)
        {
            Assert.That(HeroDefeatRewardSchedule.LifeCounters(tier), Is.EqualTo(expected));
            Assert.That(HeroDefeatRewardSchedule.LifeCounters(tier, 0), Is.EqualTo(expected),
                "Every hero but Ursafar reads the number p.13 prints, which is what makes this " +
                "change invisible to the rest of the roster.");
        }

        [TestCase(CardTier.I, 2)]
        [TestCase(CardTier.II, 3)]
        [TestCase(CardTier.III, 4)]
        public void WithTearsDeclaration_TheChargeIsTierPlusOne_AsOneNumber(
            CardTier tier, int expected)
        {
            Assert.That(HeroDefeatRewardSchedule.LifeCounters(tier, 1), Is.EqualTo(expected));
        }

        [TestCase(CardTier.I, 0, 1)]
        [TestCase(CardTier.II, 0, 1)]
        [TestCase(CardTier.III, 0, 1)]
        [TestCase(CardTier.I, 1, 2)]
        [TestCase(CardTier.II, 1, 2)]
        [TestCase(CardTier.III, 2, 3)]
        public void ExperimentalBaseOne_AppliesBeforeEveryExistingAdditionalCost(
            CardTier tier, int additional, int expected)
        {
            Assert.That(
                HeroDefeatCostQuery.LifeCounters(tier, additional, baseCostIsOne: true),
                Is.EqualTo(expected));
            Assert.That(
                HeroDefeatCostQuery.LifeCounters(tier, additional, baseCostIsOne: false),
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier, additional)),
                "Disabling the experiment preserves p.13's official Tier-derived base.");
        }

        [Test]
        public void TwoDeclarationsChargeTwo_SoNeitherSourceIsSilentlySwallowed()
        {
            var set = Declared(Attacker, 2);
            Assert.That(
                set.AdditionalDefeatLifeCost(RuleObject.ForHero(Attacker)), Is.EqualTo(2));
            Assert.That(
                HeroDefeatRewardSchedule.LifeCounters(CardTier.II,
                    set.AdditionalDefeatLifeCost(RuleObject.ForHero(Attacker))),
                Is.EqualTo(4));
        }

        [Test]
        public void TheDeclarationIsAboutTheAttacker_AndReachesNobodyElse()
        {
            var set = Declared(Attacker, 1);

            Assert.That(set.AdditionalDefeatLifeCost(RuleObject.ForHero(Attacker)), Is.EqualTo(1));
            Assert.That(set.AdditionalDefeatLifeCost(RuleObject.ForHero(Bystander)), Is.EqualTo(0),
                "'if YOU defeated a hero' names the attacker, so a hero standing beside him charges " +
                "nothing extra.");
            Assert.That(RuleModificationSet.Empty.AdditionalDefeatLifeCost(
                RuleObject.ForHero(Attacker)), Is.EqualTo(0));
        }

        [Test]
        public void NothingPrintedReducesThePagesCharge()
        {
            Assert.That(
                () => HeroDefeatRewardSchedule.LifeCounters(CardTier.II, -1),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// The declaration Tear actually makes, taken from its own content rather than rebuilt here:
        /// one modification of the new family, on the acting hero, for the turn.
        /// </summary>
        [Test]
        public void TearsOwnDeclaration_IsOneAdditionalCostOnTheControllerForTheTurn()
        {
            var definition = UrsafarActiveEffectContent.AdditionalDefeatLifeCostDefinition;

            Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
            Assert.That(
                definition.AffectedScope, Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
            Assert.That(
                definition.Modifications.Single().Kind,
                Is.EqualTo(RuleModificationKind.AdditionalDefeatLifeCost));

            // Standing before p.14's step 6, which is where the one payment happens - see
            // UrsafarEffectContent for why the card's printed step-7 placement is when the condition is
            // checked rather than when the counter is paid.
            Assert.That(definition.CreationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
            Assert.That(definition.ActivationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
        }

        /// <summary>
        /// The equivalence the composition rests on, pinned rather than assumed: checking "if enraged"
        /// at p.14's step 1 gives the same answer as checking it at step 7, because nothing can enrage
        /// him in between. His own grant is dormant for the whole of its own action, and that is the
        /// only source that could fire inside one.
        /// </summary>
        [Test]
        public void CheckingEnragedAtStepOneRatherThanStepSeven_ChangesNoAnswer()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(
                new ActiveEffectId("tear-grant"),
                UrsafarActiveEffectContent.DefinitionFor(UrsafarCardContent.Tear),
                Attacker,
                TeamSide.Red,
                Attacker,
                new CardInstanceId("ursafar-1-tear"),
                new ActionDefinitionId("primary-attack"),
                1,
                1);

            foreach (var checkpoint in new[]
                {
                    TimingCheckpoint.BeforeAttack,
                    TimingCheckpoint.DefenseResponse,
                    TimingCheckpoint.AttackResultComparison,
                    TimingCheckpoint.AfterAttack
                })
            {
                lifecycle.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(checkpoint, 1, 1),
                    new TieBreakerState(TeamSide.Red));
                Assert.That(
                    GuardsDigital.Core.Rules.CardResolution.HeroConditionQuery.IsInCondition(
                        GuardsDigital.Core.Rules.CardResolution.HeroConditionKind.Enraged,
                        Attacker,
                        lifecycle),
                    Is.False,
                    $"Tear's own grant is still dormant at {checkpoint}.");
            }
        }

        private static RuleModificationSet Declared(HeroId hero, int declarations) =>
            new RuleModificationSet(
                Enumerable.Range(0, declarations).Select(index =>
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForAdditionalDefeatLifeCost(),
                        RuleObject.ForHero(hero),
                        new ActiveEffectId("tear-cost-" + index),
                        hero,
                        TeamSide.Red,
                        index)),
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);
    }
}
