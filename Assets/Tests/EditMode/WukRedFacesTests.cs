using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114b. Wuk's Red faces through a configured round: the Nature's Attacks' two
    /// bullets ("a hero adjacent to you" / "a unit in range adjacent to a Tree token"), Champion's
    /// "one, or both, on different targets", and the trampling moves.
    /// </summary>
    public sealed class WukRedFacesTests
    {
        private static CardDefinition Protector => Face(WukRedFaces.NaturesProtector);
        private static CardDefinition Guardian => Face(WukRedFaces.NaturesGuardian);
        private static CardDefinition Champion => Face(WukRedFaces.NaturesChampion);

        private static CardDefinition Face(CardDefinitionId id) =>
            WukCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private const string AdjacentBullet = WukRedFaces.NatureAdjacentBulletNode;
        private const string TreeBullet = WukRedFaces.NatureTreeBulletNode;

        // --- The Nature's Attacks -------------------------------------------------------------------

        [Test]
        public void Protector_TheTreeBulletNamesOnlyUnitsInRangeBesideOneOfWuksTrees()
        {
            // A Tree at (2,0). The minion at (2,-1) stands beside it, 2 from Wuk. The minion at
            // (-2,0) is in Range but beside no Tree; the enemy hero at (0,2) is in Range and stands
            // beside the ENEMY's rock at (0,3), which is not a Tree.
            var round = new WukRound(Protector, enemyAt: H(0, 2),
                enemyMinionsAt: new[] { H(2, -1), H(-2, 0) },
                treesAt: new[] { H(2, 0) }, enemyRocksAt: new[] { H(0, 3) });
            round.StartPrimaryAttack();
            Assert.That(round.PendingNode, Is.EqualTo(WukRedFaces.NatureModalNode));
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { AdjacentBullet }),
                "nobody stands beside Wuk, so \"Target a hero adjacent to you\" names nobody.");
            round.TakeBullets(TreeBullet);

            Assert.That(round.LegalTargetMinions(), Is.EqualTo(new[] { EnemyMinion(0) }));
            Assert.That(round.LegalTargetHeroes(), Is.Empty,
                "\"adjacent to a Tree token\": a rock of another hero's is not one.");
            round.Target(EnemyMinion(0));
            Assert.That(round.Stands(EnemyMinion(0)), Is.False, "p.13: an attacked minion is defeated.");
        }

        [Test]
        public void Protector_TheAdjacentBulletNamesHeroesAndNeverAMinion()
        {
            var round = new WukRound(Protector, enemyAt: H(1, 0), enemyMinionsAt: new[] { H(0, 1) });
            round.StartPrimaryAttack();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { TreeBullet }),
                "no Tree stands anywhere, so the second bullet names nobody and is greyed.");
            round.TakeBullets(AdjacentBullet);
            Assert.That(round.LegalTargetHeroes(), Is.EqualTo(new[] { Enemy }));
            Assert.That(round.LegalTargetMinions(), Is.Empty, "\"Target a HERO adjacent to you\".");
            round.Target(Enemy);
            Assert.That(round.AttackTotal, Is.EqualTo(5), "Nature's Protector's printed Attack 5.");
            round.AssertDefenseRestoreReplays();
        }

        [Test]
        public void ProtectorReachesTwo_GuardianReachesItsPrintedThree()
        {
            var board = new
            {
                enemyMinionsAt = new[] { H(3, -1) },
                treesAt = new[] { H(3, 0) }
            };

            var protector = new WukRound(Protector,
                enemyMinionsAt: board.enemyMinionsAt, treesAt: board.treesAt);
            Assert.That(protector.IsOffered(WukCardContent.PrimaryRangedAttack), Is.False,
                "the minion beside the Tree is 3 from Wuk: outside Protector's Range 2, and nobody is " +
                "adjacent - nothing the face could do, so it is not offered.");

            var guardian = new WukRound(Guardian,
                enemyMinionsAt: board.enemyMinionsAt, treesAt: board.treesAt);
            guardian.StartPrimaryAttack();
            guardian.TakeBullets(TreeBullet);
            Assert.That(guardian.LegalTargetMinions(), Is.EqualTo(new[] { EnemyMinion(0) }),
                "Guardian prints Range 3.");
        }

        [Test]
        public void Champion_TakesBothBulletsOnDifferentTargets()
        {
            // The enemy hero at (1,0) is adjacent to Wuk AND beside the Tree at (2,0); the minion at
            // (2,-1) is beside the Tree too.
            var round = new WukRound(Champion, enemyAt: H(1, 0),
                enemyMinionsAt: new[] { H(2, -1) }, treesAt: new[] { H(2, 0) });
            round.StartPrimaryAttack();
            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { AdjacentBullet, TreeBullet }));
            round.TakeBullets(AdjacentBullet, TreeBullet);

            Assert.That(round.LegalTargetHeroes(), Is.EqualTo(new[] { Enemy }),
                "the adjacent bullet's own sequence runs first");
            round.Target(Enemy);
            Assert.That(round.AttackTotal, Is.EqualTo(6), "Nature's Champion's printed Attack 6.");
            round.Defend();

            Assert.That(round.LegalTargetHeroes(), Is.Empty,
                "\"on different targets\": the hero the first Attack named is not named again, even " +
                "though he stands beside the Tree.");
            Assert.That(round.LegalTargetMinions(), Is.EqualTo(new[] { EnemyMinion(0) }));
            round.Target(EnemyMinion(0));
            Assert.That(round.Stands(EnemyMinion(0)), Is.False);
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Protector_APausedTargetQuestionRestoresAndConvergesByteForByte()
        {
            var round = new WukRound(Protector, enemyAt: H(1, 0));
            round.StartPrimaryAttack();
            round.TakeBullets(AdjacentBullet);

            round.AssertTargetRestoreReplays(Enemy);
        }
    }
}
