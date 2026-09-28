using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
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

        /// <summary>
        /// Milestone 9.2, ruling W4 (replacing WU-Q4): "defeat up to one minion you moved through"
        /// offers the ENEMY minions he moved through - the rulebook forbids defeating a friendly unit,
        /// "No exceptions." His own minion is walked through and left standing.
        /// </summary>
        [Test]
        public void UpToOneEnemyMinionMovedThrough_IsDefeated_AndHisOwnIsNotOffered()
        {
            var round = new WukRound(Trample,
                friendlyMinionsAt: new[] { H(1, 0) }, enemyMinionsAt: new[] { H(2, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();

            Assert.That(round.PendingNode, Is.EqualTo(WukTrampleContent.MinionSelectNode));
            Assert.That(round.OfferedMinions(), Is.EqualTo(new[] { EnemyMinion(0) }),
                "W4: only the enemy minion he moved through");
            round.PickMinions(EnemyMinion(0));

            Assert.That(round.Stands(EnemyMinion(0)), Is.False);
            Assert.That(round.Stands(FriendlyMinion(0)), Is.True);
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void AngryStampede_OffersOnlyEnemyMinions_UpToTwo()
        {
            var round = new WukRound(Stampede,
                friendlyMinionsAt: new[] { H(1, 0) }, enemyMinionsAt: new[] { H(2, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();

            Assert.That(round.OfferedMinions(), Is.EqualTo(new[] { EnemyMinion(0) }),
                "W4: his own minion is not offered.");
            Assert.That(round.Turn.PendingEffectChoice.MaximumSelections, Is.EqualTo(1),
                "\"up to two\", bounded by the one enemy minion he moved through");
            round.PickMinions(EnemyMinion(0));

            Assert.That(round.Stands(EnemyMinion(0)), Is.False);
            Assert.That(round.Stands(FriendlyMinion(0)), Is.True);
        }

        /// <summary>
        /// Milestone 8.118. Two ENEMY minions named by one Stampede are two Defeats, each paying p.13's
        /// reward - the pair used to share one outcome and the second award was refused.
        /// </summary>
        [Test]
        public void AngryStampede_DefeatsTwoEnemyMinions_AndPaysForEach()
        {
            var round = new WukRound(Stampede, enemyMinionsAt: new[] { H(1, 0), H(2, 0) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();
            round.PickMinions(EnemyMinion(0), EnemyMinion(1));

            Assert.That(round.Stands(EnemyMinion(0)) || round.Stands(EnemyMinion(1)), Is.False);
            Assert.That(round.ActionEnded, Is.True);
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

        /// <summary>
        /// Milestone 8.118. A guard over the enemy minions within one space of their hero: "When any
        /// friendly minion in radius is defeated, you may discard a card; if you do, the minion is not
        /// removed" - Brogan's Shield sentence at any card's price, so the fixture's own hand can pay.
        /// </summary>
        private static ActiveEffectDefinition Guard { get; } = new ActiveEffectDefinition(
            "wuk-test-guard-effect",
            ActiveEffectAffectedScopeKind.MinionsInRadius,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            new[] { ActiveEffectCancellationCondition.CardsRetrieved },
            false,
            "WukTestGuard",
            "wuk-test-guard",
            System.Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForRemovalPrevention(
                RemovalPreventionCost.DiscardOwnedCard(CardLocation.Hand)) },
            null,
            DeclaredRadius.PrintedIcon(1),
            SourceTeamRelation.AlliedWithSource);

        /// <summary>
        /// Wuk stampedes through two enemy minions and names both; the enemy hero at (1,-1) guards the
        /// first, at (1,0), and not the second, at (2,0), two spaces from them.
        /// </summary>
        private static WukRound TwoTrampledMinionsTheFirstGuarded()
        {
            var round = new WukRound(Stampede, enemyAt: H(1, -1),
                enemyMinionsAt: new[] { H(1, 0), H(2, 0) },
                standingDefinitions: new[] { (Enemy, Core.Participants.TeamSide.Blue, Guard) });
            round.StartPrimaryMovement();
            round.MoveTo(H(3, 0));
            round.StopMoving();
            round.PickMinions(EnemyMinion(0), EnemyMinion(1));
            Assert.That(round.Prevention, Is.Not.Null,
                "The guarded minion's removal stops on its controller's question.");
            Assert.That(round.Prevention.SubjectMinionId, Is.EqualTo(EnemyMinion(0)));
            return round;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AGuardedTrampledMinion_HoldsTheRest_UntilItsQuestionIsAnswered(bool pay)
        {
            var round = TwoTrampledMinionsTheFirstGuarded();
            Assert.That(round.Stands(EnemyMinion(1)), Is.True,
                "The second Defeat waits behind the question the first one stopped on.");

            round.AnswerPrevention(pay);

            Assert.That(round.Stands(EnemyMinion(0)), Is.EqualTo(pay),
                pay ? "a paid guard keeps its minion" : "a declined guard lets the removal finish");
            Assert.That(round.Stands(EnemyMinion(1)), Is.False,
                "and the unguarded minion Wuk also named is then defeated too.");
            Assert.That(round.Prevention, Is.Null);
            Assert.That(round.PendingNode, Is.Null);
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void AGuardedTrampledMinionsQuestion_RestoresAndStillOwesTheRest()
        {
            var round = TwoTrampledMinionsTheFirstGuarded();
            var restored = round.RestoreFrom(round.Snapshot());
            var restoredTurn = restored.Round.CurrentTurnAuthority;
            var prompt = restoredTurn.DescribeRemovalPrevention(restoredTurn.CurrentModifications());
            Assert.That(prompt, Is.Not.Null);
            Assert.That(restored.Round.TryAnswerRemovalPrevention(prompt.DeclineInvocation).Accepted,
                Is.True);

            round.AnswerPrevention(pay: false);

            Assert.That(round.Stands(EnemyMinion(1)), Is.False);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(round.Snapshot()),
                "The restored match owes the same remaining Defeat and converges on the same bytes.");
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
