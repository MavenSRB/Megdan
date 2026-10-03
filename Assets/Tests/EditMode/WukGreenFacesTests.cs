using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114d. Wuk's Green faces through a configured round: the throws ("Place a token, or
    /// an enemy unit, adjacent to you into a space in range", Monstrous Throw's repeat) and the Tree
    /// swaps (Into the Canopy's "Choose one", Treetop Ride's "Choose up to two times").
    /// </summary>
    public sealed class WukGreenFacesTests
    {
        private static CardDefinition Toss => Face(WukGreenFaces.TossAway);
        private static CardDefinition Mighty => Face(WukGreenFaces.MightyThrow);
        private static CardDefinition Monstrous => Face(WukGreenFaces.MonstrousThrow);
        private static CardDefinition Canopy => Face(WukGreenFaces.IntoTheCanopy);
        private static CardDefinition Treetop => Face(WukGreenFaces.TreetopRide);

        private static CardDefinition Face(CardDefinitionId id) =>
            WukCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        // --- The throws -----------------------------------------------------------------------------

        [Test]
        public void Toss_ThrowsAnAdjacentEnemyMinionIntoAnEmptySpaceInItsPrintedRange()
        {
            var round = new WukRound(Toss, enemyMinionsAt: new[] { H(1, 0) });
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo(WukGreenFaces.ThrowSubjectNode));
            round.PickSpaces(H(1, 0));
            Assert.That(round.PendingNode, Is.EqualTo(WukGreenFaces.ThrowPlaceNode));
            var destinations = round.OfferedSpaces();
            Assert.That(destinations, Has.All.Matches<HexCoordinate>(space =>
                    HexDistanceQuery.Calculate(space, H(0, 0)) <= 3),
                "Toss Away prints Range 3, measured from Wuk (p.9).");
            Assert.That(destinations, Has.Member(H(-3, 0)));
            Assert.That(destinations, Has.No.Member(H(0, 0)), "only an empty space (p.18's Place).");

            round.PickSpaces(H(-3, 0));
            Assert.That(round.SpaceOf(EnemyMinion(0)), Is.EqualTo(H(-3, 0)));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Toss_ASubjectIsATokenOfAnyOwnerOrAnEnemyUnit_NeverAFriendlyOne()
        {
            var round = new WukRound(Toss, enemyAt: H(0, 1), allyAt: H(-1, 0),
                friendlyMinionsAt: new[] { H(1, -1) }, treesAt: new[] { H(1, 0) },
                enemyRocksAt: new[] { H(-1, 1) });
            round.StartPrimarySkill();

            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(0, 1), H(1, 0), H(-1, 1) }),
                "\"a token, or an enemy unit\": the enemy hero, his own Tree and the enemy's rock - " +
                "not his ally, not his own minion.");
        }

        [Test]
        public void Toss_ReachesThree_MightyThrowReachesItsPrintedFour()
        {
            var toss = new WukRound(Toss, enemyMinionsAt: new[] { H(1, 0) });
            toss.StartPrimarySkill();
            toss.PickSpaces(H(1, 0));
            Assert.That(toss.OfferedSpaces(), Has.No.Member(H(-4, 0)));

            var mighty = new WukRound(Mighty, enemyMinionsAt: new[] { H(1, 0) });
            mighty.StartPrimarySkill();
            mighty.PickSpaces(H(1, 0));
            Assert.That(mighty.OfferedSpaces(), Has.Member(H(-4, 0)), "Mighty Throw prints Range 4.");
        }

        [Test]
        public void Toss_WithNothingAdjacentToThrow_IsNotOffered()
        {
            var round = new WukRound(Toss, enemyAt: H(0, 2));

            Assert.That(round.IsOffered(WukCardContent.PrimarySkill), Is.False);
        }

        [Test]
        public void Monstrous_MayRepeatOnTheSameSubject()
        {
            var round = new WukRound(Monstrous, enemyMinionsAt: new[] { H(1, 0) });
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo(WukGreenFaces.ThrowRepeatNode),
                "\"May repeat once\"");
            round.Accept();

            Assert.That(round.OfferedSpaces(), Has.Member(H(0, 1)),
                "the same minion - still beside him - may be thrown again (no \"different target\").");
            round.PickSpaces(H(0, 1));
            round.PickSpaces(H(0, -4));
            Assert.That(round.SpaceOf(EnemyMinion(0)), Is.EqualTo(H(0, -4)));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Toss_APausedDestinationRestoresAndReplaysToTheSameBytes()
        {
            var round = new WukRound(Toss, enemyMinionsAt: new[] { H(1, 0) });
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));

            round.AssertRestoreReplays(H(-2, 0));
        }

        // --- The Tree swaps -------------------------------------------------------------------------

        [Test]
        public void Canopy_WukSwapsWithATreeInItsPrintedRadius()
        {
            var round = new WukRound(Canopy, treesAt: new[] { H(0, 2), H(0, 3) });
            round.StartPrimarySkill();
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { WukGreenFaces.SwapFriendBulletNode(0) }),
                "no friendly unit stands in Radius 2.");
            round.TakeBullets(WukGreenFaces.SwapWithTreeBulletNode(0));

            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(0, 2) }),
                "Into the Canopy prints Radius 2: the Tree 3 away is out of reach.");
            round.PickSpaces(H(0, 2));
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 2)));
            Assert.That(round.Trees(), Is.EquivalentTo(new[] { H(0, 0), H(0, 3) }),
                "p.19's Swap: the two exchange places.");
        }

        [Test]
        public void Canopy_AFriendlyUnitInRadiusSwapsWithATree()
        {
            var round = new WukRound(Canopy, allyAt: H(-1, 0), treesAt: new[] { H(0, 2) });
            round.StartPrimarySkill();
            round.TakeBullets(WukGreenFaces.SwapFriendBulletNode(0));

            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(-1, 0), H(0, 2) }),
                "one question for both pieces: a friendly unit in radius - his ally, never Wuk himself " +
                "(p.17) - and a Tree in radius.");
            round.PickSpaces(H(-1, 0), H(0, 2));
            Assert.That(round.SpaceOf(Ally), Is.EqualTo(H(0, 2)));
            Assert.That(round.Trees(), Is.EqualTo(new[] { H(-1, 0) }));
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 0)));
        }

        [Test]
        public void Canopy_WithATreeButNoFriendlyUnitInRadius_OffersOnlyTheSwapWithWuk()
        {
            var round = new WukRound(Canopy, allyAt: H(0, 4), treesAt: new[] { H(0, 1) });
            round.StartPrimarySkill();

            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { WukGreenFaces.SwapWithTreeBulletNode(0) }));
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { WukGreenFaces.SwapFriendBulletNode(0) }),
                "the friendly swap needs both a friendly unit and a Tree in radius.");
        }

        [Test]
        public void Canopy_WithNoTreeInRadius_IsNotOffered()
        {
            var round = new WukRound(Canopy, allyAt: H(-1, 0), treesAt: new[] { H(0, 4) });

            Assert.That(round.IsOffered(WukCardContent.PrimarySkill), Is.False);
        }

        [Test]
        public void Treetop_TakesTheSameBulletTwice()
        {
            var round = new WukRound(Treetop, treesAt: new[] { H(0, 2), H(2, 0) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(WukGreenFaces.SwapWithTreeBulletNode(0));
            round.PickSpaces(H(0, 2));
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 2)));

            round.Accept();
            round.TakeBullets(WukGreenFaces.SwapWithTreeBulletNode(1));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(0, 0), H(2, 0) }),
                "the second performance re-reads the board: the Tree now standing where he started " +
                "is 2 away, and so is the other one.");
            round.PickSpaces(H(2, 0));
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(2, 0)));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Treetop_UpToTwo_MayStopAfterOne()
        {
            var round = new WukRound(Treetop, treesAt: new[] { H(0, 2) });
            round.StartPrimarySkill();
            round.Accept();
            round.TakeBullets(WukGreenFaces.SwapWithTreeBulletNode(0));
            round.PickSpaces(H(0, 2));

            round.Decline();
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 2)));
        }

        /// <summary>
        /// Milestone 9.39, ruling W10: "Choose up to two times" is one count, so declining the FIRST
        /// pass asks no second. Before it the second "up to" was still opened.
        /// </summary>
        [Test]
        public void Treetop_DecliningTheFirstPass_AsksNoSecond()
        {
            var round = new WukRound(Treetop, treesAt: new[] { H(0, 2) });
            round.StartPrimarySkill();
            Assert.That(round.PendingNode, Is.EqualTo("swap-0-up-to"));
            round.Decline();

            Assert.That(round.PendingNode, Is.Null, "W10: no second pass after the first is declined.");
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 0)), "nothing moved.");
        }
    }
}
