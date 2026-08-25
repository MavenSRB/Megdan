using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.SabinaBoardFixture;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.22. Sabina's three After-Attack riders, on boards built to ask each printed clause
    /// a question the staged position cannot.
    ///
    /// <para><b>What these three faces print, and why they are one fixture.</b> Point Blank Shot says
    /// "After the attack: If the target <b>is</b> adjacent to you, push it 1 space"; Shootout says "If
    /// the target <b>was</b> adjacent to you remove up to one enemy minion adjacent to you"; Bullet
    /// Hell says the same with "up to two". They share one composition and differ in two places only -
    /// the tense of the adjacency question and the maximum of the selection - so what is worth proving
    /// is exactly those two differences and the consequences that hang off them. The tense is not a
    /// wording preference: rulebook p.14 step 6 removes a defeated minion before step 7 runs, so a
    /// target that died answers <em>no</em> to Point Blank Shot's question and <em>yes</em> to
    /// Shootout's, and the two capabilities behind them (<c>target-is-adjacent@1</c> and
    /// <c>pre-consequence-adjacency@1</c>) exist to keep those two answers apart.</para>
    ///
    /// <para>Composition itself - which node carries which capability, what each declares - is
    /// <see cref="SabinaVerifiedContentTests"/>'s and is not repeated here. This file only asks what a
    /// resolved Attack does on a board. <see cref="SabinaBoardFixture"/> owns the board.</para>
    /// </summary>
    public sealed class SabinaAfterAttackRemovalTests
    {
        // -----------------------------------------------------------------------------------------
        // The tense of the adjacency question
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Point Blank Shot's rider reads live occupancy, so the minion its own Attack defeated cannot
        /// be pushed.
        ///
        /// <para>This is not a missing push. p.14 resolves the Attack's consequence at step 6 and the
        /// card's "After the attack:" text at step 7, and the printed question is asked in the present
        /// tense - "If the target <b>is</b> adjacent to you". A piece that is no longer on the board is
        /// not adjacent to anything, and pushing it would be pushing a piece out of the box.</para>
        /// </summary>
        [Test]
        public void PointBlankShot_PushesNothingWhenItsOwnAttackTookTheTargetOffTheBoard()
        {
            var fixture = Create(
                minionPlacements: At("red-melee-01", FirstAdjacent));
            var target = fixture.MinionAt(FirstAdjacent);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, target);

            Assert.That(fixture.Choice, Is.Null,
                "The push is a fixed one-space displacement with no decision in it, so a standing " +
                "choice here would mean the rider had found something else to ask.");
            Assert.That(fixture.MinionIds(), Has.No.Member(target),
                "p.14 step 6 defeats a targeted enemy minion outright, and this one had nothing " +
                "keeping it on the board.");
            Assert.That(fixture.HeroSpace(fixture.Sabina), Is.EqualTo(SabinaAt),
                "A push moves the target and never the pusher.");
        }

        /// <summary>
        /// The same rider on a target the Attack leaves standing: the enemy hero, who defends.
        ///
        /// <para>Point Blank Shot prints Range 1, so the hero has to be adjacent to be targeted at all,
        /// which makes this the board on which the present-tense question can answer yes. p.13 applies
        /// the text "after the target either defends, or is defeated and removed", so a successful
        /// Defense is not an escape from it.</para>
        /// </summary>
        [Test]
        public void PointBlankShot_PushesASurvivingAdjacentTargetOneSpaceAway()
        {
            var fixture = Create(
                heroPlacements: At("primitive-hero", FirstAdjacent));
            var before = fixture.HeroSpace(fixture.Wasp);
            Assert.That(HexDistanceQuery.Calculate(before, SabinaAt), Is.EqualTo(1),
                "The board under test is the one where the enemy hero is inside Range 1.");

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Wasp);

            var after = fixture.HeroSpace(fixture.Wasp);
            Assert.That(after, Is.Not.EqualTo(before),
                "The target survived the Attack and is still adjacent, so the present-tense " +
                "condition holds and the printed push applies.");
            Assert.That(HexDistanceQuery.Calculate(after, SabinaAt), Is.EqualTo(2),
                "One space, directly away: p.19's push increases the distance from the pusher by " +
                "exactly the pushed distance.");
        }

        /// <summary>
        /// Shootout's rider reads the state before the consequence, so the minion its own Attack
        /// defeated still answers "was adjacent" and the removal clause runs.
        ///
        /// <para>This is the difference the two capabilities exist for, and it is visible on one
        /// board: the same shape of Attack on the same kind of target opens a removal selection here
        /// and pushes nothing in
        /// <see cref="PointBlankShot_PushesNothingWhenItsOwnAttackTookTheTargetOffTheBoard"/>.</para>
        /// </summary>
        [Test]
        public void Shootout_RunsItsRemovalAfterDefeatingTheAdjacentTargetItAskedAbout()
        {
            var fixture = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = FirstAdjacent,
                    ["red-melee-02"] = SecondAdjacent
                });
            var target = fixture.MinionAt(FirstAdjacent);
            var survivor = fixture.MinionAt(SecondAdjacent);

            fixture.PlayAttackAt(SabinaCardContent.Shootout, target);

            Assert.That(fixture.Choice, Is.Not.Null,
                "The defeated target was adjacent when the Attack was declared, which is the tense " +
                "this face prints, so the removal clause is reached.");
            Assert.That(fixture.CandidateMinions(fixture.Choice), Is.EqualTo(new[] { survivor }),
                "The removable minions are named from the board as it now stands: the target has " +
                "already left it, and the second enemy is the one still standing beside her.");
            Assert.That(fixture.Choice.MaximumSelections, Is.EqualTo(1),
                "\"up to one\" is the printed maximum, and it is the only number that differs " +
                "between this face and Bullet Hell.");

            fixture.SelectAndConfirm(survivor);
            Assert.That(fixture.MinionIds(), Has.No.Member(survivor));
        }

        /// <summary>
        /// The negative half of the same question: a target two spaces away was never adjacent, so the
        /// clause is not reached - even though an enemy minion is standing beside her.
        /// </summary>
        [Test]
        public void Shootout_RemovesNothingWhenTheTargetWasNeverAdjacent()
        {
            var fixture = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = FirstAdjacent,
                    ["red-melee-02"] = TwoAway
                });

            fixture.PlayAttackAt(SabinaCardContent.Shootout, fixture.MinionAt(TwoAway));

            Assert.That(fixture.Choice, Is.Null,
                "The conditional gates the whole sentence. An adjacent enemy minion exists and is " +
                "deliberately not offered, because the card asks about the target rather than " +
                "about the neighbourhood.");
            Assert.That(fixture.MinionIds(), Has.Member(fixture.MinionAt(FirstAdjacent)),
                "Nothing was removed, which is what an unmet condition means.");
        }

        // -----------------------------------------------------------------------------------------
        // Bullet Hell's up-to-two selection
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The chosen set's legality: enemy minions, adjacent to her, at most two, and never the piece
        /// the Attack already took or a friendly one standing in the same disk.
        /// </summary>
        [Test]
        public void BulletHell_OffersOnlyAdjacentEnemyMinions_AndAtMostTheTwoItPrints()
        {
            var fixture = ThreeEnemiesAndAFriendBesideHer();

            fixture.PlayAttackAt(SabinaCardContent.BulletHell, fixture.MinionAt(FirstAdjacent));

            var choice = fixture.Choice;
            Assert.That(choice, Is.Not.Null,
                "The target was adjacent, which is the tense this face prints.");
            Assert.That(choice.MaximumSelections, Is.EqualTo(2), "\"up to two\" is the printed maximum.");
            Assert.That(choice.MinimumSelections, Is.EqualTo(0),
                "\"up to\" is p.10's optional quantity: naming none is an answer rather than a " +
                "failure to answer.");

            var offered = fixture.CandidateMinions(choice);
            Assert.That(offered.All(minion => fixture.SideOf(minion) == TeamSide.Red), Is.True,
                "\"enemy minion\" is measured from the acting hero, and Sabina is Blue - so the " +
                "friendly minion standing beside her is not offered.");
            Assert.That(
                offered.All(minion =>
                    HexDistanceQuery.Calculate(fixture.SpaceOf(minion), SabinaAt) == 1),
                Is.True,
                "\"adjacent to you\" is one space from her own, which p.17 measures from wherever " +
                "she is standing when the text runs.");
            Assert.That(offered, Has.Member(fixture.MinionAt(SecondAdjacent)),
                "The other Red Melee minion beside her is exactly what the clause names.");
        }

        /// <summary>
        /// p.18's immunity keeps a removal from being offered at all, and the same minion becomes
        /// removable once the immunity lapses.
        ///
        /// <para>The immunity is asked twice - once when the candidates are derived and again at the
        /// removal stage itself, because the board can change between a paused choice and its answer.
        /// The visible consequence is the one asserted here: an immune Heavy minion standing beside
        /// her is not among the candidates, so the card cannot offer a removal it would then have to
        /// refuse. Ready and Waiting is the one face of hers that overrides this, and it overrides it
        /// for a Swap rather than for a removal - which is
        /// <see cref="SabinaMinionCommandTests"/>'s.</para>
        /// </summary>
        [Test]
        public void BulletHell_NeverOffersAnImmuneHeavyMinion_AndOffersItOnceTheImmunityLapses()
        {
            var immune = ThreeEnemiesAndAFriendBesideHer();
            var heavyWhileImmune = immune.MinionAt(FourthAdjacent);
            immune.PlayAttackAt(SabinaCardContent.BulletHell, immune.MinionAt(FirstAdjacent));
            Assert.That(immune.CandidateMinions(immune.Choice), Has.No.Member(heavyWhileImmune),
                "The Heavy minion is standing beside her and is immune, because its own side still " +
                "keeps a non-Heavy minion in the current starting Battle Zone.");

            var exposed = ThreeEnemiesAndAFriendBesideHer(exposeTheHeavy: true);
            var heavy = exposed.MinionAt(FourthAdjacent);
            exposed.PlayAttackAt(SabinaCardContent.BulletHell, exposed.MinionAt(FirstAdjacent));

            Assert.That(exposed.CandidateMinions(exposed.Choice), Has.Member(heavy),
                "With no friendly non-Heavy minion left in the zone the immunity lapses and the " +
                "same Heavy minion becomes removable - so the absence above is p.18's rule rather " +
                "than a candidate list that cannot see Heavy minions at all.");
        }

        /// <summary>
        /// Both removals apply, in the order the selection published them, and neither pays a coin.
        ///
        /// <para>The printed reminder is explicit - "You gain no coins for removing a minion, only for
        /// defeating" - and it is the whole reason this text is a Remove rather than a second Defeat.
        /// The coin the hero does hold afterwards is the one p.13 paid for the Attack's own target,
        /// which this case reads before and after so that the two are not confused.</para>
        /// </summary>
        [Test]
        public void BulletHell_AppliesBothRemovalsInOrder_AndPaysNoCoinForEither()
        {
            var fixture = TwoEnemiesBesideHerAndOneToAttack();
            var first = fixture.MinionAt(SecondAdjacent);
            var second = fixture.MinionAt(ThirdAdjacent);

            fixture.PlayAttackAt(SabinaCardContent.BulletHell, fixture.MinionAt(FirstAdjacent));
            var afterTheAttack = fixture.Coins(fixture.Sabina);
            Assert.That(afterTheAttack, Is.EqualTo(2),
                "p.13 pays 2 coins for defeating a Melee minion, and that Defeat is the Attack's " +
                "own rather than the rider's.");

            fixture.SelectAndConfirm(first, second);

            Assert.That(fixture.MinionIds(), Has.No.Member(first));
            Assert.That(fixture.MinionIds(), Has.No.Member(second));
            Assert.That(fixture.Coins(fixture.Sabina), Is.EqualTo(afterTheAttack),
                "The printed reminder says so: removing pays nothing, and two removals pay nothing " +
                "twice.");
            Assert.That(
                fixture.SequenceOfAppliedNode("shootout-remove"),
                Is.LessThan(fixture.SequenceOfAppliedNode("shootout-remove-second")),
                "The selection publishes a first and a second space and the two Remove nodes read " +
                "one each, so the order the removals apply in is the order they were named rather " +
                "than whatever order the board enumerates.");
        }

        /// <summary>
        /// The same board answered with one name instead of two: the second Remove has nothing bound,
        /// and that is an answer rather than a step that failed.
        /// </summary>
        [Test]
        public void BulletHell_AcceptsOneNameWhereItAllowsTwo_AndLeavesTheOtherMinionStanding()
        {
            var fixture = TwoEnemiesBesideHerAndOneToAttack();
            var kept = fixture.MinionAt(ThirdAdjacent);

            fixture.PlayAttackAt(SabinaCardContent.BulletHell, fixture.MinionAt(FirstAdjacent));
            fixture.SelectAndConfirm(fixture.MinionAt(SecondAdjacent));

            Assert.That(fixture.MinionIds(), Has.Member(kept),
                "\"up to two\" is a maximum, and the second Remove node is `if able` for exactly " +
                "this: a chooser who named one minion left the second node nothing to read.");
            Assert.That(fixture.Choice, Is.Null, "The card is finished. A node with no input is not a question.");
        }

        /// <summary>
        /// Confirming with nobody named removes nothing, which p.10's optional quantity allows.
        /// </summary>
        [Test]
        public void BulletHell_CanBeAnsweredWithNobodyNamed_AndThenRemovesNothing()
        {
            var fixture = TwoEnemiesBesideHerAndOneToAttack();

            fixture.PlayAttackAt(SabinaCardContent.BulletHell, fixture.MinionAt(FirstAdjacent));
            var standing = fixture.MinionIds();
            Assert.That(fixture.Choice.ConfirmInvocation, Is.Not.Null,
                "An `up to` selection offers a confirm from its first moment, because naming none " +
                "of them is one of the answers it takes.");
            fixture.Confirm();

            Assert.That(fixture.MinionIds(), Is.EqualTo(standing),
                "\"None of them\" leaves the board where it was.");
        }

        // -----------------------------------------------------------------------------------------
        // p.8's transition, mid-rider
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A removal that empties the enemy side out of the Battle Zone records p.8's cause, and the
        /// Lane pushes mid-turn rather than at the end of the round.
        ///
        /// <para>This is what makes the removal clause more than a board tidy-up: p.19's Remove carries
        /// the same lane-push qualification p.13's Defeat does, so "remove the last remaining enemy
        /// minion" reaches p.8 by the standalone path as well as by the Defeat one. The board is built
        /// so the Attack's own Defeat does not empty the zone and the removal does, which is what keeps
        /// the case about the rider.</para>
        /// </summary>
        [Test]
        public void BulletHell_RemovingTheLastEnemyMinionInTheZone_PushesTheLaneMidTurn()
        {
            var fixture = Create(
                tier: 3, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: At("primitive-hero", SecondAdjacent),
                banishedMinions: new[]
                {
                    "red-melee-02", "red-melee-03", "red-melee-04", "red-ranged-01", "red-heavy-01"
                });
            Assert.That(fixture.Session.Projection.PendingLanePushRequest, Is.Null,
                "Nothing has emptied a side yet.");

            fixture.PlayAttackAt(SabinaCardContent.BulletHell, fixture.Wasp);
            fixture.SelectAndConfirm(fixture.MinionAt(FirstAdjacent));

            Assert.That(fixture.Session.Projection.PendingLanePushRequest, Is.Not.Null,
                "The removal took the last Red minion out of the current starting Battle Zone, " +
                "which is p.8's own sentence, and a mid-turn cause becomes a mid-turn request.");
        }

        // -----------------------------------------------------------------------------------------
        // Boards
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Two Red Melee minions and the Red Heavy standing beside her, with a Blue minion in the same
        /// disk. <paramref name="exposeTheHeavy"/> takes the Red side's other non-Heavy minions out of
        /// the setup, which is what lets p.18's immunity lapse.
        /// </summary>
        private static SabinaBoardFixture ThreeEnemiesAndAFriendBesideHer(bool exposeTheHeavy = false)
        {
            var placements = new Dictionary<string, HexCoordinate>
            {
                ["red-melee-01"] = FirstAdjacent,
                ["red-heavy-01"] = FourthAdjacent,
                ["blue-melee-01"] = AdjacentAgainstTerrain
            };
            if (!exposeTheHeavy)
            {
                placements["red-melee-02"] = SecondAdjacent;
                placements["red-melee-03"] = ThirdAdjacent;
            }

            return Create(
                tier: 3, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: placements,
                banishedMinions: exposeTheHeavy
                    ? new[] { "red-melee-02", "red-melee-03", "red-melee-04", "red-ranged-01" }
                    : null);
        }

        /// <summary>
        /// Three Red Melee minions beside her: one to attack and two for the removal clause to name.
        /// </summary>
        private static SabinaBoardFixture TwoEnemiesBesideHerAndOneToAttack() =>
            Create(
                tier: 3, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = FirstAdjacent,
                    ["red-melee-02"] = SecondAdjacent,
                    ["red-melee-03"] = ThirdAdjacent
                });
    }
}
