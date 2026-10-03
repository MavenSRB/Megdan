using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8c. Knife of the Living Dead and the five Red Attacks through a configured round:
    /// the Zombie moves and removals before the attack, "if you do, gain +1 Attack" once per Zombie
    /// removed, "another enemy hero ... adjacent to a Zombie token, discards a card, if able" after it
    /// with "each enemy hero can only be targeted once", the printed "If" before Braains' retrieve, and
    /// restore/replay inside each chain.
    /// </summary>
    public sealed class MortimerAttackFacesTests
    {
        private static readonly HeroId Mortimer = MortimerRound.Mortimer;
        private static readonly HeroId Enemy = MortimerRound.Enemy;
        private static readonly HeroId EnemyAlly = MortimerRound.EnemyAlly;

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static CardDefinition Face(CardDefinitionId id) =>
            MortimerAttackFaces.Definitions.Single(card => card.Id.Equals(id));

        private static string Up(string prefix, int pass) => MortimerRepeatedChoice.UpToNode(prefix, pass);

        // --- Knife of the Living Dead ---------------------------------------------------------------

        [Test]
        public void Knife_EachZombieRemovedBesideTheTarget_AddsOneAttack()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(2, 0), H(1, -1), H(3, -3) });
            round.StartPrimaryAttack();
            round.Target(Enemy);

            Assert.That(round.PendingNode, Is.EqualTo(Up(MortimerAttackFaces.KnifePrefix, 1)),
                "\"Before the attack, Choose up to three times\" - the target is already bound.");
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.RemoveBullet(1));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(2, 0), H(1, -1) }),
                "only the Zombies adjacent to the target, not the one three spaces away.");
            round.PickSpaces(H(2, 0));

            round.Accept();
            round.TakeBullets(MortimerAttackFaces.RemoveBullet(2));
            round.PickSpaces(H(1, -1));
            round.Decline();

            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(3, -3) }), "p.19: removed to the supply.");
            Assert.That(round.AttackTotal, Is.EqualTo(6), "Basic Attack 4, +1 for each Zombie removed.");
            round.AssertDefenseRestoreReplays();
        }

        /// <summary>
        /// Regression, milestone 9.8c: a removal that ENDS the text, with no question after it, still
        /// counts. p.14's step 3 totals the Attack after step 1's text, and the page used to be
        /// resumed with the modifications the last command was handed before the text ran - so the
        /// third +1 here was lost and the total read 6.
        /// </summary>
        [Test]
        public void Knife_ARemovalThatEndsTheText_StillAddsItsAttack()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(2, 0), H(1, -1), H(1, 1) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            foreach (var (pass, zombie) in new[] { (1, H(2, 0)), (2, H(1, -1)), (3, H(1, 1)) })
            {
                round.Accept();
                round.TakeBullets(MortimerAttackFaces.RemoveBullet(pass));
                round.PickSpaces(zombie);
            }

            Assert.That(round.AttackTotal, Is.EqualTo(7), "three Zombies removed, +3.");
        }

        [Test]
        public void Knife_WithNoZombieBesideTheTarget_TheRemoveBulletIsUnavailable()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(0, 3) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Accept();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerAttackFaces.RemoveBullet(1) }));
            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { MortimerAttackFaces.MoveBullet("knife", 1) }));
        }

        [Test]
        public void Knife_AZombieMovedBesideTheTarget_CanThenBeRemoved()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(1, 2) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.MoveBullet("knife", 1));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(1, 2) }), "the Zombie in radius.");
            round.PickSpaces(H(1, 2));
            Assert.That(round.OfferedSpaces(), Has.Member(H(1, 1)), "exactly one space, to any empty neighbour.");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(1, 0)), "never onto the target.");
            round.PickSpaces(H(1, 1));

            round.Accept();
            Assert.That(round.AvailableBullets(), Has.Member(MortimerAttackFaces.RemoveBullet(2)),
                "the second pass reads the board the first one left.");
            round.TakeBullets(MortimerAttackFaces.RemoveBullet(2));
            round.PickSpaces(H(1, 1));

            Assert.That(round.Turn.PendingEffectChoice, Is.Null,
                "no Zombie is left to move or remove, so a third pass is never asked.");
            Assert.That(round.AttackTotal, Is.EqualTo(5));
        }

        [Test]
        public void Knife_WithMasterOfPuppets_FourRemovalsOnFivePassesAddFour()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(2, 0), H(1, -1), H(1, 1), H(2, -1) }, withUltimate: true);
            round.StartPrimaryAttack();
            round.Target(Enemy);
            // W10 (9.39): a declined pass would close the count, so the four removals are the first
            // four passes - the printed three and the Ultimate's fourth - and the fifth, with no Zombie
            // left to remove or move, has nothing to offer and closes on its own.
            foreach (var (pass, zombie) in new[] { (1, H(2, 0)), (2, H(1, -1)), (3, H(1, 1)), (4, H(2, -1)) })
            {
                round.Accept();
                round.TakeBullets(MortimerAttackFaces.RemoveBullet(pass));
                round.PickSpaces(zombie);
            }

            Assert.That(round.Zombies(), Is.Empty);
            if (!round.ActionEnded && round.PendingNode == MortimerRepeatedChoice.UpToNode(MortimerAttackFaces.KnifePrefix, 5))
            {
                round.Decline();
            }
            Assert.That(round.AttackTotal, Is.EqualTo(8),
                "every Zombie removed adds one, on the Ultimate's extra passes too.");
        }

        [Test]
        public void Knife_APauseInsideTheChain_RestoresAndReplays()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.KnifeOfTheLivingDead), enemyAt: H(1, 0),
                zombiesAt: new[] { H(2, 0), H(1, -1) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.RemoveBullet(1));
            round.PickSpaces(H(2, 0));
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.RemoveBullet(2));

            round.AssertRestoreReplays(H(1, -1));
        }

        // --- Crawling, Walking and Racing Dead ------------------------------------------------------

        [Test]
        public void Crawling_AnotherEnemyHeroBesideAZombie_DiscardsAfterTheAttack()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.CrawlingDead), enemyAt: H(1, 0),
                enemyAllyAt: H(0, 3), zombiesAt: new[] { H(2, 0), H(0, 4) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Defend();

            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.ModalNode(MortimerAttackFaces.DeadPrefix, 1)),
                "\"After the attack, choose one\".");
            round.TakeBullets(MortimerAttackFaces.DiscardBullet(1));
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { EnemyAlly }),
                "\"ANOTHER enemy hero\": the target stands beside a Zombie too, and is not offered.");
            round.PickHero(EnemyAlly);
            Assert.That(round.Chooser, Is.EqualTo(EnemyAlly), "the discarding hero chooses the card.");
            round.PickCard(round.OfferedCards().Single());

            Assert.That(round.LocationsOf(EnemyAlly), Has.Member(CardLocation.DiscardPile));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Crawling_AnEnemyHeroNotBesideAZombie_IsNotOffered()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.CrawlingDead), enemyAt: H(1, 0),
                enemyAllyAt: H(0, 3), zombiesAt: new[] { H(3, 0) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Defend();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerAttackFaces.DiscardBullet(1) }));
        }

        [Test]
        public void Walking_EachEnemyHeroCanOnlyBeTargetedOnce()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.WalkingDead), enemyAt: H(0, 3),
                enemyAllyAt: H(-3, 3), enemyMinionsAt: new[] { H(1, 0) },
                zombiesAt: new[] { H(0, 4), H(-3, 4) });
            round.StartPrimaryAttack();
            round.Target(MortimerRound.EnemyMinion(0));

            round.Accept();
            round.TakeBullets(MortimerAttackFaces.DiscardBullet(1));
            Assert.That(round.OfferedHeroes(), Is.EquivalentTo(new[] { Enemy, EnemyAlly }));
            round.PickHero(Enemy);
            round.PickCard(round.OfferedCards().First());

            round.Accept();
            round.TakeBullets(MortimerAttackFaces.DiscardBullet(2));
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { EnemyAlly }),
                "\"each enemy hero can only be targeted once\": the first pass's hero is gone from the offer.");

            var bytes = round.Snapshot();
            var restored = round.RestoreFrom(bytes).Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(restored.Candidates.Select(candidate => candidate.Value.HeroId),
                Is.EqualTo(new[] { EnemyAlly }), "a restored pause re-derives the group from its accepted choices.");
        }

        [Test]
        public void Walking_WithOnlyTheOneOtherHeroTaken_TheSecondDiscardIsUnavailable()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.WalkingDead), enemyAt: H(1, 0),
                enemyAllyAt: H(0, 3), zombiesAt: new[] { H(2, 0), H(0, 4) });
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Defend();
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.DiscardBullet(1));
            round.PickHero(EnemyAlly);
            round.PickCard(round.OfferedCards().First());

            round.Accept();
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerAttackFaces.DiscardBullet(2) }),
                "the target is never another hero, and the ally has been targeted once.");
        }

        // --- Braains...! ----------------------------------------------------------------------------

        [Test]
        public void Braains_WithAnEnemyHeroInRadiusBesideAZombie_RetrievesADiscardedCard()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.Braains), enemyAt: H(1, 0),
                zombiesAt: new[] { H(2, 0) }, mortimerDiscards: 1);
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Accept();
            round.TakeBullets(MortimerAttackFaces.RetrieveBullet(1));
            round.PickCard(round.OfferedCards().Single());

            Assert.That(round.LocationsOf(Mortimer).Count(location => location == CardLocation.DiscardPile),
                Is.EqualTo(0), "the discarded card came back.");
        }

        [Test]
        public void Braains_WithNoEnemyHeroBesideAZombie_TheRetrieveBulletIsTakenAndDoesNothing()
        {
            var round = new MortimerRound(Face(MortimerAttackFaces.Braains), enemyAt: H(1, 0),
                zombiesAt: new[] { H(0, 3) }, mortimerDiscards: 1);
            round.StartPrimaryAttack();
            round.Target(Enemy);
            round.Accept();

            Assert.That(round.AvailableBullets(), Has.Member(MortimerAttackFaces.RetrieveBullet(1)),
                "8.35z35: a bullet that can be taken and applies nothing is offered - here a false " +
                "printed \"If\" (RULING 53) - and only a bullet a mandatory step blocks is greyed.");
            round.TakeBullets(MortimerAttackFaces.RetrieveBullet(1));

            Assert.That(round.LocationsOf(Mortimer).Count(location => location == CardLocation.DiscardPile),
                Is.EqualTo(1), "the \"If\" is false: nothing is retrieved.");
        }

        // --- Offers ---------------------------------------------------------------------------------

        [Test]
        public void EveryFace_IsOfferedWithAUnitBesideHim_WhateverTheZombiesDo()
        {
            foreach (var face in MortimerAttackFaces.Definitions)
            {
                Assert.That(new MortimerRound(face, enemyAt: H(1, 0)).IsOffered(MortimerAttackFaces.PrimaryAttack),
                    Is.True, face.DisplayName + ": the Attack itself does something.");
                Assert.That(new MortimerRound(face, enemyAt: H(3, 0)).IsOffered(MortimerAttackFaces.PrimaryAttack),
                    Is.False, face.DisplayName + ": nobody adjacent to target.");
            }
        }
    }
}
