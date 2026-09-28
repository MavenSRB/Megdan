using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114b1. Claim Dominance and Assert Dominance - "This round: Up to 1 (2) enemy
    /// minion(s) adjacent to you does (do) not count towards the minion total during minion battle,
    /// regardless of immunity" - under the user's WU-Q3 (chosen at the battle, adjacency then) and
    /// WU-Q6 (an excluded minion may still be removed).
    /// </summary>
    public sealed class WukDominanceTests
    {
        private static CardDefinition Claim => WukCardContent.Definition(WukBlueFaces.ClaimDominance);

        private static CardDefinition Assert3 => WukCardContent.Definition(WukBlueFaces.AssertDominance);

        private static WukRound PlayedAndRunToBattle(
            CardDefinition face, HexCoordinate[] enemies, HexCoordinate[] friends)
        {
            var round = new WukRound(face, enemyMinionsAt: enemies, friendlyMinionsAt: friends, battleZone: true);
            round.StartPrimarySkill();
            Assert.That(round.ActionEnded, Is.True, "the Skill chooses nothing when it resolves (WU-Q3)");
            Assert.That(round.Board.MinionsExcludedFromMinionBattle, Is.Empty);
            Assert.That(round.RunToMinionBattle().Accepted, Is.True);
            return round;
        }

        [Test]
        public void Claim_AsksAtTheBattle_ForUpToOneEnemyMinionAdjacentThen()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(1, 0), H(2, 0) }, new[] { H(-3, 0) });

            var asked = round.EndOfRoundChoice;
            Assert.That(asked, Is.Not.Null, "the pre-battle moment asks Wuk");
            Assert.That(asked.NodeId.Value, Is.EqualTo(WukDominanceContent.ExcludeNode));
            Assert.That(asked.ChooserId, Is.EqualTo(Wuk));
            Assert.That(asked.Candidates.Select(candidate => candidate.Value.MinionId),
                Is.EqualTo(new[] { EnemyMinion(0) }),
                "only the enemy minion adjacent to him now; the one two spaces away is not offered");
            Assert.That(asked.MinimumSelections, Is.EqualTo(0), "\"Up to\"");
            Assert.That(asked.MaximumSelections, Is.EqualTo(1));
        }

        [Test]
        public void Claim_TheChosenMinionDoesNotCount_SoALostBattleIsATie()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(1, 0), H(0, 1) }, new[] { H(-3, 0) });

            round.AnswerEndOfRound(EnemyMinion(0));
            Assert.That(round.Board.MinionsExcludedFromMinionBattle, Is.EqualTo(new[] { EnemyMinion(0) }));
            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            Assert.That(round.Round.PendingMinionBattleChoice, Is.Null);
            Assert.That(round.Stands(FriendlyMinion(0)), Is.True,
                "two enemy minions, one not counted, against one friendly: a tie removes nothing");
            Assert.That(round.Stands(EnemyMinion(0)) && round.Stands(EnemyMinion(1)), Is.True);
        }

        [Test]
        public void Claim_ChoosingNone_CountsEveryMinion()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(1, 0), H(0, 1) }, new[] { H(-3, 0) });

            round.AnswerEndOfRound();
            Assert.That(round.Board.MinionsExcludedFromMinionBattle, Is.Empty);
            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            Assert.That(round.Stands(FriendlyMinion(0)), Is.False, "two against one: Wuk's side loses one");
        }

        [Test]
        public void Assert_ExcludesUpToTwo()
        {
            var round = PlayedAndRunToBattle(
                Assert3, new[] { H(1, 0), H(0, 1), H(-1, 1) }, new[] { H(-3, 0) });

            Assert.That(round.EndOfRoundChoice.MaximumSelections, Is.EqualTo(2));
            round.AnswerEndOfRound(EnemyMinion(0), EnemyMinion(2));
            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            Assert.That(round.Stands(FriendlyMinion(0)), Is.True, "three enemy minions, two not counted: a tie");
        }

        /// <summary>
        /// WU-Q6 (user, 2026-09-28): the sentence changes the count, not which minions stand in the
        /// Battle Zone, so when the enemy side loses anyway the excluded minion is still one it removes.
        /// </summary>
        [Test]
        public void TheExcludedMinion_MayStillBeRemovedWhenItsSideLoses()
        {
            var round = PlayedAndRunToBattle(
                Claim, new[] { H(1, 0), H(0, 1) }, new[] { H(-3, 0), H(-3, 1), H(-4, 1) });

            round.AnswerEndOfRound(EnemyMinion(0));
            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            Assert.That(round.Stands(EnemyMinion(0)), Is.False,
                "one counted enemy minion against three: the difference is two, and both enemy minions go");
            Assert.That(round.Stands(EnemyMinion(1)), Is.False);
        }

        [Test]
        public void TheExclusions_SurviveASnapshot_AndAreClearedWithTheRoundsCleanup()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(1, 0), H(0, 1) }, new[] { H(-3, 0) });
            round.AnswerEndOfRound(EnemyMinion(1));

            var bytes = round.Snapshot();
            var restored = round.RestoreFrom(bytes);
            Assert.That(restored.Board.MinionsExcludedFromMinionBattle, Is.EqualTo(new[] { EnemyMinion(1) }),
                "the choice and the battle are separate commands, so the exclusion is persisted state");
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(bytes));

            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            Assert.That(restored.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(round.Snapshot()), "the restored battle resolves exactly as the live one");
            Assert.That(round.Stands(FriendlyMinion(0)), Is.True);

            Assert.That(round.Round.TryAdvanceEndOfRound().Accepted, Is.True, "p.8's cleanup");
            Assert.That(round.Board.MinionsExcludedFromMinionBattle, Is.Empty,
                "the battle is over, so nothing it excluded stays excluded");
        }

        [Test]
        public void TheQuestionItself_RestoresAndConvergesOnTheSameAnswer()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(1, 0), H(0, 1) }, new[] { H(-3, 0) });
            var restored = round.RestoreFrom(round.Snapshot());
            Assert.That(restored.CardActions.PendingEffectChoice?.NodeId,
                Is.EqualTo(round.EndOfRoundChoice.NodeId));

            round.AnswerEndOfRound(EnemyMinion(0));
            AnswerEndOfRound(restored.Round, restored.CardActions, new[] { EnemyMinion(0) });
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(round.Snapshot()));
        }

        [Test]
        public void WithNoEnemyMinionAdjacent_NothingIsAsked()
        {
            var round = PlayedAndRunToBattle(Claim, new[] { H(3, 0) }, new[] { H(-3, 0) });

            Assert.That(round.EndOfRoundChoice, Is.Null);
            Assert.That(round.Board.MinionsExcludedFromMinionBattle, Is.Empty);
        }
    }
}
