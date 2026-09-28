using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114b1. Trample and Angry Stampede under WU-Q5 (A): the Movement action itself
    /// tramples. "If you move in a straight line: You may ignore obstacles; each enemy hero you moved
    /// through discards a card, or is defeated; defeat up to one (two) minion(s) you moved through."
    /// </summary>
    public sealed class WukTrampleTests
    {
        private static CardDefinition Trample => WukCardContent.Definition(WukRedFaces.Trample);

        private static CardDefinition Stampede => WukCardContent.Definition(WukRedFaces.AngryStampede);

        private static int Discarded(WukRound round, Core.Units.Heroes.HeroId hero) =>
            round.LocationsOf(hero).Count(location => location == CardLocation.DiscardPile);

        private static ReachableMovementDestinationView To(WukRound round, HexCoordinate space) =>
            new ReachableMovementDestinationView(round.Destinations().Single(value => value.Destination.Equals(space)));

        [Test]
        public void AStraightWalk_PassesThroughAnEnemyHero_WhoThenDiscards()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0));
            round.StartPrimaryMovement();

            var through = To(round, H(2, 0));
            Assert.That(through.Route, Is.EqualTo(new[] { H(0, 0), H(1, 0), H(2, 0) }),
                "the straight file through the enemy hero, at its straight cost");
            Assert.That(through.Cost, Is.EqualTo(2));

            round.MoveTo(H(2, 0));
            round.StopMoving();

            Assert.That(round.PendingNode, Is.EqualTo(WukTrampleContent.DiscardNode(1)));
            Assert.That(round.Chooser, Is.EqualTo(Enemy), "the trampled hero chooses the card");
            var before = Discarded(round, Enemy);
            round.PickCard(round.OfferedCards().First());
            Assert.That(Discarded(round, Enemy), Is.EqualTo(before + 1));
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(2, 0)));
        }

        [Test]
        public void AnObstacleThatIsNotAUnit_IsCrossedToo_ButNeverStoppedOn()
        {
            var round = new WukRound(Trample, treesAt: new[] { H(1, 0) });
            round.StartPrimaryMovement();

            Assert.That(round.Destinations().Select(value => value.Destination), Has.No.Member(H(1, 0)),
                "p.18: no walk ends on an obstacle");
            Assert.That(To(round, H(2, 0)).Route, Is.EqualTo(new[] { H(0, 0), H(1, 0), H(2, 0) }));
            round.MoveTo(H(2, 0));
            round.StopMoving();
            Assert.That(round.ActionEnded, Is.True, "a Tree is neither a hero nor a minion");
        }

        [Test]
        public void AnEnemyHeroImmuneToTheMovement_IsNotAskedToDiscard()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0), standingEffects: new[]
            {
                (Enemy, Core.Participants.TeamSide.Blue, RuleModification.ForImmunity(RuleActivity.Movement))
            });
            round.StartPrimaryMovement();
            round.MoveTo(H(2, 0));
            round.StopMoving();

            Assert.That(round.ActionEnded, Is.True, "p.19: an immune hero is not affected");
            Assert.That(Discarded(round, Enemy), Is.EqualTo(0));
        }

        [Test]
        public void AMovementRaise_LengthensTheStraightWalk()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0), standingEffects: new[]
            {
                (Wuk, Core.Participants.TeamSide.Red, RuleModification.ForValue(PrintedValueChannel.Movement, 1))
            });
            round.StartPrimaryMovement();

            Assert.That(round.Destinations().Select(value => value.Destination), Has.Member(H(5, 0)),
                "Movement 4 raised by one reaches five spaces along the file, through the hero");
        }

        [Test]
        public void AnEnemyHeroWithNoCardToDiscard_IsDefeated()
        {
            var round = new WukRound(Trample, enemyAllyAt: H(1, 0));
            round.StartPrimaryMovement();
            round.MoveTo(H(2, 0));
            round.StopMoving();

            Assert.That(round.OnBoard(EnemyAlly), Is.False,
                "\"discards a card, or is defeated\": a hand with nothing to give is the second way");
        }

        [Test]
        public void OnceAnObstacleIsCrossed_OnlyTheSameFileContinues()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0));
            round.StartPrimaryMovement();
            round.MoveTo(H(2, 0));

            Assert.That(round.Destinations().Select(value => value.Destination),
                Is.EquivalentTo(new[] { H(3, 0), H(4, 0) }),
                "bending now would make the crossing one the card never allowed");
        }

        [Test]
        public void AWalkThatBends_CrossesNothing()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 1));
            round.StartPrimaryMovement();
            round.MoveTo(H(0, 1));

            var onward = round.Destinations();
            Assert.That(onward.Any(value => value.Route.Contains(H(1, 1))), Is.False,
                "the walk left its file, so no route passes through the enemy hero");
            round.StopMoving();
            Assert.That(round.ActionEnded, Is.True, "nothing was moved through, so nothing is asked");
        }

        [Test]
        public void WithoutTheCard_TheSameWalkIsNotOffered()
        {
            var round = new WukRound(WukCardContent.Definition(WukRedFaces.NaturesProtector), enemyAt: H(1, 0));
            Assert.That(
                MovementReachabilityQuery.FindReachableDestinations(
                    new HeroOccupancy(Wuk, H(0, 0)), round.Board, 2,
                    Core.Rules.Modifications.RuleModificationSet.Empty)
                    .Any(value => value.Destination.Equals(H(2, 0))),
                Is.False, "the ordinary walk cannot reach the far side of the hero in two");
        }

        [Test]
        public void UpToOneMinionMovedThrough_OfEitherSide_IsDefeated()
        {
            var round = new WukRound(Trample,
                friendlyMinionsAt: new[] { H(1, 0) }, enemyMinionsAt: new[] { H(2, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();

            Assert.That(round.PendingNode, Is.EqualTo(WukTrampleContent.MinionSelectNode));
            Assert.That(round.OfferedMinions(), Is.EquivalentTo(new[] { FriendlyMinion(0), EnemyMinion(0) }),
                "any minion, his own included (WU-Q4)");
            Assert.That(round.Turn.PendingEffectChoice.MaximumSelections, Is.EqualTo(1));
            round.PickMinions(FriendlyMinion(0));

            Assert.That(round.Stands(FriendlyMinion(0)), Is.False);
            Assert.That(round.Stands(EnemyMinion(0)), Is.True);
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void AngryStampede_DefeatsUpToTwo()
        {
            var round = new WukRound(Stampede,
                friendlyMinionsAt: new[] { H(1, 0) }, enemyMinionsAt: new[] { H(2, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();

            Assert.That(round.Turn.PendingEffectChoice.MaximumSelections, Is.EqualTo(2));
            round.PickMinions(FriendlyMinion(0), EnemyMinion(0));

            Assert.That(round.Stands(FriendlyMinion(0)) || round.Stands(EnemyMinion(0)), Is.False);
        }

        [Test]
        public void ChoosingNoMinion_DefeatsNone()
        {
            var round = new WukRound(Trample, enemyMinionsAt: new[] { H(1, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(2, 0));
            round.StopMoving();
            round.PickMinions();

            Assert.That(round.Stands(EnemyMinion(0)), Is.True, "\"up to one\"");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void TheWalkIsBoardState_SoTheDiscardQuestionRestoresAndConverges()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0));
            round.StartPrimaryMovement();
            round.MoveTo(H(2, 0));
            round.StopMoving();

            var walk = round.Board.LastCompletedMovementWalk;
            Assert.That(walk.Mover, Is.EqualTo(Wuk));
            Assert.That(walk.Route, Is.EqualTo(new[] { H(0, 0), H(1, 0), H(2, 0) }));
            var restored = round.RestoreFrom(round.Snapshot());
            Assert.That(restored.Board.LastCompletedMovementWalk.Route, Is.EqualTo(walk.Route));

            round.AssertRestoreRepliesWithCard(round.OfferedCards().First());
        }

        [Test]
        public void StoppingWhereHeStands_AppliesNothingAfterTheWalk()
        {
            var round = new WukRound(Trample, enemyAt: H(1, 0));
            round.StartPrimaryMovement();
            round.StopMoving();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.SpaceOf(Wuk), Is.EqualTo(H(0, 0)));
        }

        /// <summary>A copy of one offered destination's route and cost, for readable assertions.</summary>
        private readonly struct ReachableMovementDestinationView
        {
            public ReachableMovementDestinationView(Core.Rules.Movement.ReachableMovementDestination value)
            {
                Route = value.Route.ToArray();
                Cost = value.Cost;
            }

            public HexCoordinate[] Route { get; }

            public int Cost { get; }
        }
    }
}
