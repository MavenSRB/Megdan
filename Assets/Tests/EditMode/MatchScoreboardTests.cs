using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.53. The per-hero match scoreboard the victory screen prints, proved on the facts
    /// that feed each of its six columns and across every reconstruction the session has.
    /// </summary>
    /// <remarks>
    /// <para><b>Every case here drives a real configured match.</b> The columns are counted from the
    /// committed rule events and from the one coin ledger a composed match owns, so a case built out
    /// of a bare authority would be counting facts nothing had bound the board to - which is exactly
    /// the shape a fixture must not have when the thing under test is a wiring seam.</para>
    /// <para><b>What is deliberately NOT asserted:</b> a hero's Level. It is not on this board, and
    /// the screen reads it off <c>HeroProgressionProjection</c> where it already lives.</para>
    /// </remarks>
    public sealed class MatchScoreboardTests
    {
        // -----------------------------------------------------------------------------------------
        // The shape of an untouched board
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AFreshMatch_HasALineForEveryHeroAndNothingCountedOnAnyOfThem()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();

            var board = session.Projection.Scoreboard;
            Assert.That(
                board.Select(entry => entry.HeroId),
                Is.EqualTo(session.Projection.Participants.Heroes
                    .Select(hero => hero.Id)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal)),
                "Every participating hero has a line from the first command, ordered by hero id - " +
                "the victory screen prints a row per hero, not a row per hero something happened to.");
            Assert.That(
                board.All(entry =>
                    entry.Kills == 0 && entry.Deaths == 0 && entry.AttacksAtHeroes == 0 &&
                    entry.TimesDefended == 0 && entry.MinionsKilled == 0 && entry.CoinsEarned == 0),
                Is.True,
                "and nothing has happened yet, which is a board of zeroes rather than an empty board.");
        }

        // -----------------------------------------------------------------------------------------
        // Attacks at heroes, and the defense that answers one
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The column the mockup calls "Attacks at heroes" counts the ORDER, not its outcome: this
        /// attack is answered and stopped, and it still counts for the attacker - while the hero who
        /// answered it takes the "Times defended" column instead.
        /// </summary>
        [Test]
        public void ADefendedAttack_CountsForTheAttackerAndForTheHeroWhoStood()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            var attacker = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Red);
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            BlockTheAttack(session, defender);

            Assert.That(Line(session, attacker).AttacksAtHeroes, Is.EqualTo(1),
                "One p.14 order was staged against an enemy hero.");
            Assert.That(Line(session, defender).TimesDefended, Is.EqualTo(1),
                "and it ended AttackSuccessfullyDefended, which is what this column counts.");

            Assert.That(Line(session, attacker).Kills, Is.EqualTo(0));
            Assert.That(Line(session, defender).Deaths, Is.EqualTo(0),
                "Nobody was defeated, so neither of those columns moved.");
            Assert.That(Line(session, defender).AttacksAtHeroes, Is.EqualTo(0),
                "and the defender declared no attack of their own - answering one is not making one.");
            Assert.That(Line(session, attacker).TimesDefended, Is.EqualTo(0),
                "nor did the attacker defend anything.");
        }

        /// <summary>
        /// The other answer to the same page: the Block's condition is false, so p.14 defeats the
        /// hero who tried it. The attacker takes a Kill and the attack, the defender takes a Death
        /// and NOT a "Times defended" - they answered and did not stand.
        /// </summary>
        [Test]
        public void ADefeatingAttack_CountsAKillADeathAndTheCoinsItPaid()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var attacker = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Red);
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            var coinsBefore = Line(session, attacker).CoinsEarned;
            DefeatTheHero(session, defender);

            Assert.That(Line(session, attacker).Kills, Is.EqualTo(1),
                "p.13's Defeat credits the hero whose action asked for it.");
            Assert.That(Line(session, defender).Deaths, Is.EqualTo(1),
                "and the hero it happened to takes the Death.");
            Assert.That(Line(session, attacker).AttacksAtHeroes, Is.EqualTo(1),
                "The order is counted once whichever way it ends.");
            Assert.That(Line(session, defender).TimesDefended, Is.EqualTo(0),
                "The defender answered and was defeated, which is not a defense that stood.");

            Assert.That(Line(session, attacker).CoinsEarned, Is.GreaterThan(coinsBefore),
                "p.13 pays Defeat coins, and the ledger that credits them is what counts this column.");
            Assert.That(Line(session, attacker).CoinsEarned,
                Is.EqualTo(CoinsHeldBy(session, attacker)),
                "Nothing has been spent yet, so what was collected is still what is held - which is " +
                "the ONE moment in a match where the two numbers agree, and the reason the column " +
                "exists separately from the balance.");
            Assert.That(Line(session, defender).Kills, Is.EqualTo(0),
                "and being defeated credits the defeated hero with nothing.");
        }

        // -----------------------------------------------------------------------------------------
        // Minions, and the coins they pay
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ADefeatedMinion_CountsForTheHeroWhoDefeatedItAndPaysThatHeroItsCoin()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(
                out var redHeroId, out var target);
            var redCard = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blueCard = session.Projection.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id));

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == GuardsDigital.Core.Cards.ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var choice = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));
            Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)),
                Is.False, "p.13: a targeted minion is simply defeated.");

            Assert.That(Line(session, redHeroId).MinionsKilled, Is.EqualTo(1));
            Assert.That(Line(session, redHeroId).CoinsEarned, Is.GreaterThan(0),
                "and the minion's bounty is a coin this hero collected.");
            Assert.That(Line(session, redHeroId).AttacksAtHeroes, Is.EqualTo(0),
                "A minion is not a hero, which is the whole distinction this column draws.");
            Assert.That(Line(session, redHeroId).Kills, Is.EqualTo(0),
                "and a defeated minion is not a Kill - that column counts enemy HEROES.");
        }

        /// <summary>
        /// p.8's Pity Coin reaches the board through the same ledger everything else does, which is
        /// the point of counting coins there rather than on the event stream: nothing had to be told
        /// about this coin source for it to be counted.
        /// </summary>
        [Test]
        public void EveryCoinSourceInARound_ReachesTheCoinsEarnedColumn()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithACoinEarningRound(out var redHeroId);

            var board = session.Projection.Scoreboard;
            Assert.That(board.Sum(entry => entry.CoinsEarned), Is.GreaterThan(0));
            foreach (var entry in board)
            {
                Assert.That(entry.CoinsEarned, Is.GreaterThanOrEqualTo(CoinsHeldBy(session, entry.HeroId)),
                    $"{entry.HeroId.Value} cannot hold more coins than it ever collected: a balance " +
                    "only ever goes down from what was earned, because p.15's purchase spends it.");
            }

            Assert.That(Line(session, redHeroId).MinionsKilled, Is.EqualTo(1),
                "The round this fixture plays out defeats exactly one minion, with the Red hero.");
        }

        // -----------------------------------------------------------------------------------------
        // The board survives every reconstruction the session has
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheBoard_SurvivesACaptureAndRestore()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            DefeatTheHero(session, defender);

            var expected = session.Projection.Scoreboard.ToArray();
            var payload = session.CaptureSnapshot();
            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.Projection.Scoreboard.All(entry => entry.Kills == 0), Is.True,
                "A restart really does clear the board, or the restore below would prove nothing.");

            Assert.That(session.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(session.Projection.Scoreboard, Is.EqualTo(expected),
                "The scoreboard is authoritative state and rides the snapshot, so a restored match " +
                "reads the numbers it saved rather than re-deriving them from a bounded log.");
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(payload),
                "and it re-captures byte-identically, so the section reads back exactly as written.");
        }

        [Test]
        public void TheBoard_IsRebuiltByReplayingTheCommands()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            DefeatTheHero(session, defender);

            var expected = session.Projection.Scoreboard.ToArray();
            var prefix = session.Projection.AcceptedCommands.ToArray();
            Assert.That(session.Restart().Accepted, Is.True);
            foreach (var command in prefix)
            {
                Assert.That(session.Execute(command).Accepted, Is.True, command.Kind.ToString());
            }

            Assert.That(session.Projection.Scoreboard, Is.EqualTo(expected),
                "Every column is a count of committed facts and nothing else, so the same commands " +
                "in the same order produce the same board.");
        }

        /// <summary>
        /// The retained-prefix Undo, which is the reconstruction that would catch an accumulator
        /// counting anything a replay cannot reproduce: it rebuilds a fresh aggregate from the
        /// history minus its last command, so the defeat is un-counted and then counted again.
        /// </summary>
        [Test]
        public void TheBoard_StepsBackWithAnUndoAndForwardAgainWhenTheCommandIsReapplied()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var attacker = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Red);
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            DefeatTheHero(session, defender);

            var afterTheDefeat = session.Projection.Scoreboard.ToArray();
            var last = session.Projection.AcceptedCommands.Last();
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Line(session, attacker).Kills, Is.EqualTo(0),
                "The command that defeated the hero is out of the history, so the Kill is out of " +
                "the board - the accumulator is not a tally that only ever grows.");
            Assert.That(Line(session, defender).Deaths, Is.EqualTo(0));
            Assert.That(Line(session, attacker).AttacksAtHeroes, Is.EqualTo(1),
                "and the order that was staged before that command is still counted, which is what " +
                "says the rewind went back one command rather than to the beginning.");

            Assert.That(session.Execute(last).Accepted, Is.True);
            Assert.That(session.Projection.Scoreboard, Is.EqualTo(afterTheDefeat),
                "Reapplying it puts the board back exactly, with nothing counted twice.");
        }

        /// <summary>
        /// The refusal case. A rejected command never reaches the count, and the aggregate's own
        /// rollback would put the graphs back if it somehow had.
        /// </summary>
        [Test]
        public void ARefusedCommand_CountsNothing()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            DefeatTheHero(session, defender);

            var before = session.Projection.Scoreboard.ToArray();
            var payload = session.CaptureSnapshot();
            var refused = session.Execute(DirectorCommand.Pass(new HeroId("no-such-hero")));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(session.Projection.Scoreboard, Is.EqualTo(before));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(payload),
                "and the payload the board rides in is untouched too.");
        }

        // -----------------------------------------------------------------------------------------
        // Steps
        // -----------------------------------------------------------------------------------------

        private static HeroScoreboardEntry Line(MatchSession session, HeroId heroId) =>
            session.Projection.Scoreboard.Single(entry => entry.HeroId.Equals(heroId));

        private static int CoinsHeldBy(MatchSession session, HeroId heroId) =>
            session.Projection.HeroProgression.Single(entry => entry.HeroId.Equals(heroId)).Coins;

        /// <summary>
        /// Thunder Boomerang's Ranged attack answered by Stop Projectiles, whose Block condition is
        /// fulfilled - "you successfully defend, regardless of the attack value".
        /// </summary>
        private static void BlockTheAttack(MatchSession session, HeroId defender)
        {
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defender);
            var block = session.Projection.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId));
            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(defender)), Is.True,
                "The defender has to be standing, or this fixture is about a defeat.");
        }

        /// <summary>
        /// Magnetic Dagger's non-Ranged attack answered by the same Block, whose condition is
        /// therefore false - "If not, you are defeated." One accepted command carries the whole of
        /// p.13's list, which is what makes it the single command the Undo case steps back over.
        /// </summary>
        private static void DefeatTheHero(MatchSession session, HeroId defender)
        {
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.MagneticDagger, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defender);
            var block = session.Projection.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId));
            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(defender)), Is.False,
                "p.13's list removed the defeated hero, or this fixture is about a defense.");
        }
    }
}
