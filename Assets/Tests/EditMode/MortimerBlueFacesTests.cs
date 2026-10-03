using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8d. Mortimer's five Blue Skills through a configured round: a Zombie in range moves
    /// up to one space (0 is doing it, W5), "that Zombie token may push a unit or a token adjacent to it
    /// 1 space" with the Zombie as the pusher, "Gain 1 coin" only when a Zombie is there to move
    /// (MO-Q3), and Mortimer's own "Move 1 space".
    /// </summary>
    public sealed class MortimerBlueFacesTests
    {
        private static readonly HeroId Mortimer = MortimerRound.Mortimer;
        private static readonly HeroId Enemy = MortimerRound.Enemy;

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static CardDefinition Face(CardDefinitionId id) =>
            MortimerBlueFaces.Definitions.Single(card => card.Id.Equals(id));

        // --- The Zombie push ------------------------------------------------------------------------

        [Test]
        public void CorpseSlam_AZombieThatStaysPut_PushesAUnitBesideItDirectlyAway()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.CorpseSlam), enemyAt: H(3, 0),
                zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(2, 0) }), "the Zombie in range.");
            round.PickSpaces(H(2, 0));

            Assert.That(round.OfferedSpaces(), Has.Member(H(2, 0)),
                "\"up to 1 space\": staying put is an answer (W5), not a decline.");
            round.PickSpaces(H(2, 0));
            round.Accept();
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(3, 0) }),
                "only the unit adjacent to the Zombie - Mortimer is two spaces away.");
            round.PickSpaces(H(3, 0));

            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(H(4, 0)), "p.18: directly away from the Zombie.");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void CorpseSlam_AMovedZombie_PushesFromWhereItNowStands()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.CorpseSlam), enemyAt: H(3, 1),
                zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            round.PickSpaces(H(2, 0));
            round.PickSpaces(H(2, 1));
            round.Accept();
            round.PickSpaces(H(3, 1));

            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(2, 1) }));
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(H(4, 1)),
                "\"that Zombie token\" pushes from its new space, away from it.");
        }

        [Test]
        public void CorpseSlam_TheZombieMayPushMortimerHimself()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.CorpseSlam), zombiesAt: new[] { H(1, 0) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(1, 0));
            round.Accept();
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(0, 0) }),
                "\"a unit\" is any unit, and he stands beside it.");
            round.PickSpaces(H(0, 0));

            Assert.That(round.SpaceOf(Mortimer), Is.EqualTo(H(-1, 0)));
        }

        [Test]
        public void CorpseSlam_ThePushIsAMay()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.CorpseSlam), enemyAt: H(3, 0),
                zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            round.PickSpaces(H(2, 0));
            round.PickSpaces(H(1, 0));
            round.Decline();

            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(H(3, 0)));
            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(1, 0) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void CorpseSlam_WithNoZombieInRange_OnlyTheStepIsAvailable()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.CorpseSlam), zombiesAt: new[] { H(0, 5) });
            round.StartPrimarySkill();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerBlueFaces.ShoveBullet(1) }),
                "Range 4: the Zombie five spaces away cannot be moved.");
            round.TakeBullets(MortimerBlueFaces.StepBullet(MortimerBlueFaces.SlamPrefix, 1));
            round.PickSpaces(H(0, 1));
            Assert.That(round.SpaceOf(Mortimer), Is.EqualTo(H(0, 1)), "\"Move 1 space\".");
        }

        /// <summary>
        /// Milestone 9.39, ruling W10's boundary: a decline INSIDE a pass is that pass's own business
        /// and closes no count. Morbid Mosh's "that Zombie token may push" declined after the move
        /// leaves the second "up to" pass asked as printed - the pass Sequence is `up to`, the push's
        /// parent is not - and declining THAT pass is what ends the card.
        /// </summary>
        [Test]
        public void MorbidMosh_DecliningThePushInsideAPass_StillAsksTheNextPass()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.MorbidMosh), enemyAt: H(3, 0),
                zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            round.PickSpaces(H(2, 0));
            round.PickSpaces(H(2, 0));
            round.Decline();

            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.UpToNode(MortimerBlueFaces.SlamPrefix, 2)),
                "the push's decline belonged to the first pass, not to the count.");
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(H(3, 0)), "nothing was pushed.");
            round.Decline();
            Assert.That(round.ActionEnded, Is.True, "and declining the second pass ends the card (W10).");
        }

        [Test]
        public void MorbidMosh_APausedPushRestoresAndReplays()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.MorbidMosh), enemyAt: H(3, 0),
                zombiesAt: new[] { H(2, 0) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerBlueFaces.ShoveBullet(1));
            round.PickSpaces(H(2, 0));
            round.PickSpaces(H(2, 0));
            round.Accept();

            round.AssertRestoreReplays(H(3, 0));
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(H(4, 0)));
        }

        // --- Robbing Zombies ------------------------------------------------------------------------

        [Test]
        public void RobbingZombies_EachZombieMoveGainsOneCoin_EvenStandingStill()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.RobbingZombies), zombiesAt: new[] { H(2, 0) });
            var before = round.Coins(Mortimer);
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(MortimerBlueFaces.CoinBullet(1));
            round.PickSpaces(H(2, 0));
            round.PickSpaces(H(2, 1));
            round.Accept();
            round.TakeBullets(MortimerBlueFaces.CoinBullet(2));
            round.PickSpaces(H(2, 1));
            round.PickSpaces(H(2, 1));

            Assert.That(round.Coins(Mortimer) - before, Is.EqualTo(2),
                "one coin a pass, the second for a move of 0 (W5).");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void RobbingZombies_WithNoZombieInRange_GainsNoCoin()
        {
            var round = new MortimerRound(Face(MortimerBlueFaces.RobbingZombies));
            round.StartPrimarySkill();
            round.Accept();

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { MortimerBlueFaces.CoinBullet(1) }),
                "MO-Q3: the move is a mandatory step with nothing to move, so p.10 stops the bullet " +
                "before the coin.");
        }

        // --- Offers ---------------------------------------------------------------------------------

        [Test]
        public void EveryBlue_IsOfferedWhereMortimerCanStep()
        {
            foreach (var face in MortimerBlueFaces.Definitions)
            {
                Assert.That(new MortimerRound(face).IsOffered(MortimerCardContent.PrimarySkill), Is.True,
                    face.DisplayName + ": \"Move 1 space\" can always be taken on an open board.");
            }
        }
    }
}
