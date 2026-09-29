using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8e. Mortimer's five Green Skills through a configured round: a Zombie moved exactly
    /// one space, a Swap with a Zombie, and "Once per turn: Replace an enemy minion in range adjacent to
    /// two or more Zombie tokens with a Zombie token" - removed rather than defeated, the Zombie from the
    /// supply after p.18's short-supply step, and at most once across the card's passes.
    /// </summary>
    public sealed class MortimerGreenFacesTests
    {
        private static readonly HeroId Mortimer = MortimerRound.Mortimer;

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static CardDefinition Face(CardDefinitionId id) =>
            MortimerGreenFaces.Definitions.Single(card => card.Id.Equals(id));

        // --- Stage Dive -----------------------------------------------------------------------------

        [Test]
        public void StageDive_MovesAZombieExactlyOneSpace()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.StageDive), zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerGreenFaces.MoveBullet(MortimerGreenFaces.DivePrefix, 1));
            round.PickSpaces(H(2, 0));

            Assert.That(round.OfferedSpaces(), Has.No.Member(H(2, 0)), "\"1 space\" is exact (p.11).");
            round.PickSpaces(H(3, 0));
            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(3, 0) }));
        }

        [Test]
        public void StageDive_SwapsMortimerWithAZombieInRange()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.StageDive), zombiesAt: new[] { H(0, 3), H(0, 5) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerGreenFaces.SwapBullet(1));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(0, 3) }), "Range 4: not the one five away.");
            round.PickSpaces(H(0, 3));

            Assert.That(round.SpaceOf(Mortimer), Is.EqualTo(H(0, 3)));
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(0, 0), H(0, 5) }), "p.19's Swap.");
        }

        // --- Gathering Horde and Army of Darkness ---------------------------------------------------

        [Test]
        public void GatheringHorde_ReplacesAnEnemyMinionBesideTwoZombies_WithoutAReward()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.GatheringHorde),
                enemyMinionsAt: new[] { H(2, 0) }, zombiesAt: new[] { H(3, 0), H(2, 1) });
            var coins = round.Coins(Mortimer);
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerGreenFaces.ReplaceBullet(1));
            Assert.That(round.OfferedMinions(), Is.EqualTo(new[] { MortimerRound.EnemyMinion(0) }));
            round.PickMinion(MortimerRound.EnemyMinion(0));

            Assert.That(round.Stands(MortimerRound.EnemyMinion(0)), Is.False, "the minion left the board");
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(3, 0), H(2, 1), H(2, 0) }),
                "and a Zombie from the supply stands in its space (p.19's Replace).");
            Assert.That(round.Coins(Mortimer), Is.EqualTo(coins), "removed, not defeated: no coins (p.13).");
        }

        [Test]
        public void GatheringHorde_AMinionBesideOneZombie_OrAFriendlyMinion_IsNotOffered()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.GatheringHorde),
                enemyMinionsAt: new[] { H(2, 0) }, friendlyMinionsAt: new[] { H(-2, 0) },
                zombiesAt: new[] { H(3, 0), H(-3, 0), H(-2, 1) });
            round.StartPrimarySkill();
            round.Accept();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerGreenFaces.ReplaceBullet(1) }),
                "the enemy minion has one Zombie beside it, and the one with two is friendly.");
        }

        [Test]
        public void ArmyOfDarkness_ReplacesOncePerTurn_ReclaimingWhenAllFourZombiesStand()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.ArmyOfDarkness),
                enemyMinionsAt: new[] { H(2, 0), H(-2, 0) },
                zombiesAt: new[] { H(3, 0), H(2, 1), H(-3, 0), H(-2, 1) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerGreenFaces.ReplaceBullet(1));
            Assert.That(round.OfferedMinions(), Is.EquivalentTo(new[]
            {
                MortimerRound.EnemyMinion(0), MortimerRound.EnemyMinion(1)
            }), "with all four Zombies out, the Replace is still on offer: p.18 takes one back.");
            round.PickMinion(MortimerRound.EnemyMinion(0));

            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(3, 0), H(2, 1), H(-3, 0), H(-2, 1) }),
                "the pile is empty, so which standing Zombie comes back is asked.");
            round.PickSpaces(H(3, 0));
            Assert.That(round.Stands(MortimerRound.EnemyMinion(0)), Is.False);
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(2, 1), H(-3, 0), H(-2, 1), H(2, 0) }));

            round.Accept();
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerGreenFaces.ReplaceBullet(2) }),
                "\"Once per turn\": the other minion still stands beside two Zombies, and is not offered.");
        }

        [Test]
        public void ArmyOfDarkness_APausedReclaimRestoresAndReplays()
        {
            var round = new MortimerRound(Face(MortimerGreenFaces.ArmyOfDarkness),
                enemyMinionsAt: new[] { H(2, 0) },
                zombiesAt: new[] { H(3, 0), H(2, 1), H(-3, 0), H(-2, 1) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerGreenFaces.ReplaceBullet(1));
            round.PickMinion(MortimerRound.EnemyMinion(0));

            round.AssertRestoreReplays(H(-3, 0));
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(3, 0), H(2, 1), H(-2, 1), H(2, 0) }));
        }

        // --- Offers ---------------------------------------------------------------------------------

        [Test]
        public void EveryGreen_IsOfferedWithAZombieInRange_AndNotWithout()
        {
            foreach (var face in MortimerGreenFaces.Definitions)
            {
                Assert.That(new MortimerRound(face, zombiesAt: new[] { H(2, 0) })
                    .IsOffered(MortimerCardContent.PrimarySkill), Is.True, face.DisplayName);
                Assert.That(new MortimerRound(face).IsOffered(MortimerCardContent.PrimarySkill), Is.False,
                    face.DisplayName + ": with no Zombie in range every bullet can do nothing.");
            }
        }
    }
}
