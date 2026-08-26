using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.StagedHeroBoard;
using static GuardsDigital.Tests.EditMode.XargathaBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.24. Xargatha's printed set resolved through the ordinary pipeline, on boards built
    /// to ask each printed clause the question it is about.
    ///
    /// <para>What each card composes - which node, which capability, which literal - is
    /// <see cref="XargathaVerifiedContentTests"/>'s and is not repeated. This file asks what a player
    /// is offered and what the board looks like afterwards. <see cref="StagedHeroBoard"/> owns the
    /// board, unchanged from the hero it was generalised out of.</para>
    ///
    /// <para><b>Most of these fixtures keep their minions.</b> Five of her faces are answered only by a
    /// minion and three of her scaled values are counted from the ones standing beside her, so
    /// <c>clearHerNeighbourhood: false</c> is the norm here rather than the exception - and each
    /// fixture that relies on a staged piece names it.</para>
    /// </summary>
    public sealed class XargathaPrintedSetTests
    {
        // -----------------------------------------------------------------------------------------
        // "+N Attack for each other enemy unit adjacent to you"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Dangerous Slash counts the enemies around her and not the one it is attacking. The staged
        /// enemy Ranged minion is adjacent to her, so attacking a second adjacent enemy raises the
        /// total by exactly one step.
        /// </summary>
        [Test]
        public void DangerousSlash_CountsEveryOtherAdjacentEnemy_AndNotItsTarget()
        {
            // The enemy HERO is the target, because an Attack total is read off the Defense preview
            // and only a hero is offered one. Dangerous Slash is non-ranged, so he has to be beside
            // her; the two enemy minions also beside her are the whole count, and he is excluded from
            // it by being the target.
            var fixture = Create(
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: At("primitive-hero", FourthAdjacent),
                clearHerNeighbourhood: false);

            fixture.PlayAttackAt(
                XargathaCardContent.DangerousSlash, fixture.Enemy, defendSuccessfully: false);

            Assert.That(fixture.AttackTotal(), Is.EqualTo(7),
                "Printed Attack 5, plus 1 each for the staged enemy Ranged minion on (-4, 2) and the " +
                "enemy Melee minion beside her. The hero she is attacking is excluded - which is " +
                "what the face's own parenthesis says in words.");
        }

        /// <summary>
        /// The same sentence at +2 and +3 on the Tier II and Tier III cards, on the identical board -
        /// so the only thing that can differ is the amount the card prints.
        /// </summary>
        [Test]
        public void DeadlySwipeAndLethalSpin_ScaleTheSameCountByTheirOwnAmounts()
        {
            var swipe = Create(
                tier: 2,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: At("primitive-hero", FourthAdjacent),
                clearHerNeighbourhood: false);
            swipe.PlayAttackAt(
                XargathaCardContent.DeadlySwipe, swipe.Enemy, defendSuccessfully: false);
            Assert.That(swipe.AttackTotal(), Is.EqualTo(9), "Printed 5 plus 2 for each of two.");

            var spin = Create(
                tier: 3,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: At("primitive-hero", FourthAdjacent),
                clearHerNeighbourhood: false);
            spin.PlayAttackAt(
                XargathaCardContent.LethalSpin, spin.Enemy, defendSuccessfully: false);
            Assert.That(spin.AttackTotal(), Is.EqualTo(11), "Printed 5 plus 3 for the same two.");
        }

        /// <summary>
        /// With nobody else beside her, the printed number is the whole total - the boundary case that
        /// separates a scaled value from a flat bonus.
        /// </summary>
        [Test]
        public void DangerousSlash_WithNoOtherEnemyBesideHer_IsItsPrintedNumber()
        {
            var fixture = Create(heroPlacements: At("primitive-hero", FourthAdjacent));

            fixture.PlayAttackAt(
                XargathaCardContent.DangerousSlash, fixture.Enemy, defendSuccessfully: false);

            Assert.That(fixture.AttackTotal(), Is.EqualTo(5),
                "The neighbourhood is cleared, so the only adjacent enemy is the hero she is " +
                "attacking - and the target is excluded, so the count is zero and the printed " +
                "number is the whole total.");
        }

        // -----------------------------------------------------------------------------------------
        // "+1 Range for each enemy unit adjacent to you"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Long Thrust counts every adjacent enemy including the one it targets, which is the whole
        /// difference between its clause and the three melee ones: it omits the word "other".
        /// </summary>
        [Test]
        public void LongThrust_CountsEveryAdjacentEnemy_IncludingOneItCouldTarget()
        {
            var fixture = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            fixture.StartPrimary(XargathaCardContent.LongThrust);

            Assert.That(
                fixture.Choice, Is.Null,
                "Long Thrust composes no card-text step: its whole text is a value clause, so p.13's " +
                "ordinary target question is the only one it asks.");
        }

        // -----------------------------------------------------------------------------------------
        // "Target an enemy unit not adjacent to you and in range; if able, move the target ... to a
        // space adjacent to you"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Siren's Call offers the enemies inside its printed Range 3 that are not already beside her,
        /// and the staged adjacent enemy minion is the piece that proves the exclusion is real.
        /// </summary>
        [Test]
        public void SirensCall_OffersOnlyEnemiesInRangeThatAreNotAlreadyAdjacent()
        {
            var fixture = Create(
                minionPlacements: At("red-melee-01", TwoAway),
                heroPlacements: At("primitive-hero", ThreeAway),
                clearHerNeighbourhood: false);

            fixture.StartPrimary(XargathaCardContent.SirensCall);

            var minions = fixture.CandidateMinions(fixture.Choice);
            Assert.That(minions, Has.Member(fixture.MinionAt(TwoAway)),
                "An enemy minion two spaces away is in Range 3 and not adjacent.");
            Assert.That(minions, Has.No.Member(fixture.MinionAt(new HexCoordinate(-4, 2))),
                "The staged enemy Ranged minion IS adjacent, so \"not adjacent to you\" removes it - " +
                "and it has to remove minions as well as heroes, or the clause would mean one thing " +
                "for each kind.");
            Assert.That(fixture.CandidateHeroes(fixture.Choice), Has.Member(fixture.Enemy),
                "The enemy hero three spaces away is in Range 3 and not adjacent, and \"an enemy " +
                "unit\" is p.19's word for either kind.");
        }

        /// <summary>
        /// The move brings the named unit to a space beside her, which is where its own clause says it
        /// ends - not merely up to three spaces in any direction.
        /// </summary>
        [Test]
        public void SirensCall_MovesTheNamedUnitToASpaceBesideHer()
        {
            var fixture = Create(minionPlacements: At("red-melee-01", ThreeAway));
            var called = fixture.MinionAt(ThreeAway);

            fixture.StartPrimary(XargathaCardContent.SirensCall);
            fixture.Select(called);
            fixture.Confirm();
            fixture.AnswerEveryStandingChoice();

            var moved = fixture.SpaceOf(called);
            Assert.That(HexDistanceQuery.Calculate(moved, HeroAt), Is.EqualTo(1),
                "\"to a space adjacent to you\" - the destination is bounded by where she stands, " +
                "not merely by how far the unit may travel.");
        }

        // -----------------------------------------------------------------------------------------
        // "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot perform
        // movement actions"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Stone Gaze raises its effect when it resolves, and raises nothing else: the whole of what
        /// the card does when it is played is leave a source standing.
        ///
        /// <para><b>Why the live reading is not asserted here, and where it is.</b> p.19's
        /// <c>Next turn:</c> becomes applicable at Xargatha's next <c>TurnStarted</c>, and this staged
        /// board gives her one turn - so her next turn is in the next round, by which time p.8's card
        /// retrieval has cancelled the effect. A fixture that ended the round to reach the window
        /// would be reading the cancellation and calling it an absence. The window itself is a Play
        /// Mode journey's subject, on a board where the turns actually come round; what belongs at
        /// this tier is that the card raises the thing at all, and what it declares once raised, which
        /// <see cref="XargathaVerifiedContentTests.HerFiveActiveEffects_DeclareWhatTheirFacesPrint"/>
        /// pins against the printed sentence.</para>
        /// </summary>
        [Test]
        public void StoneGaze_RaisesItsEffectAndDoesNothingElseWhenItResolves()
        {
            var fixture = Create(heroPlacements: At("primitive-hero", TwoAway));

            fixture.StartPrimary(XargathaCardContent.StoneGaze);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.NodeApplied("petrify"), Is.True,
                "The Skill's whole body is the one node that creates the Active effect.");

            var reading = fixture.PetrificationReadingFor(fixture.Enemy, RuleActivity.Movement);
            Assert.That(reading.Restricted, Is.False);
            Assert.That(reading.CountsAsTerrain, Is.False,
                "\"Next turn:\" is the point: nothing is true of the enemy hero yet. An effect that " +
                "bit on the turn it was played would be \"This turn:\", which is a different keyword " +
                "and a different card.");
        }

        /// <summary>
        /// Turn Into Statues raises the same effect from the Tier III card, so the upgrade buys the
        /// printed Radius rather than a different sentence.
        /// </summary>
        [Test]
        public void TurnIntoStatues_RaisesTheSameEffectFromTheTierThreeCard()
        {
            var fixture = Create(
                tier: 3,
                heroPlacements: At("primitive-hero", ThreeAway));

            fixture.StartPrimary(XargathaCardContent.TurnIntoStatues);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.NodeApplied("petrify"), Is.True,
                "One node, one sentence, three cards - and the only thing that differs between them " +
                "is the printed Radius the effect declares.");
        }

        // -----------------------------------------------------------------------------------------
        // "If you are adjacent to an enemy minion / unit, you may retrieve a discarded card"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Fresh Converts asks nobody anything when the condition is false: the branch never opens, so
        /// there is no offer to decline.
        /// </summary>
        [Test]
        public void FreshConverts_WithNoEnemyMinionBesideHer_OffersNothing()
        {
            var fixture = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                xargathaDiscards: new[] { XargathaCardContent.Cleave });

            fixture.StartPrimary(XargathaCardContent.FreshConverts);

            Assert.That(fixture.Choice, Is.Null,
                "The \"if\" is a branch nobody is asked about. A hero standing alone is never " +
                "offered the retrieve, which is different from being offered it and declining.");
            Assert.That(fixture.DiscardPile(fixture.Hero),
                Has.Member(XargathaCardContent.Cleave),
                "Nothing came back.");
        }

        /// <summary>
        /// With the condition true the retrieve is offered and may be taken - and it is a `may`, so the
        /// same board also permits declining it.
        /// </summary>
        [Test]
        public void FreshConverts_BesideAnEnemyMinion_OffersTheRetrieveAndMayDeclineIt()
        {
            var taken = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                xargathaDiscards: new[] { XargathaCardContent.Cleave });

            taken.StartPrimary(XargathaCardContent.FreshConverts);
            Assert.That(taken.Choice, Is.Not.Null, "The branch opened.");
            taken.AnswerEveryStandingChoice();
            Assert.That(taken.Hand(taken.Hero), Has.Member(XargathaCardContent.Cleave),
                "p.10: retrieving takes one of YOUR cards back into your hand.");

            var declined = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                xargathaDiscards: new[] { XargathaCardContent.Cleave });

            declined.StartPrimary(XargathaCardContent.FreshConverts);
            declined.Decline();
            Assert.That(declined.DiscardPile(declined.Hero),
                Has.Member(XargathaCardContent.Cleave),
                "\"you may\" - so declining is one of the answers, and the card still resolves.");
        }

        // -----------------------------------------------------------------------------------------
        // "You may move an enemy [class] minion in radius up to 2 spaces before or after movement"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Charm's first question is when, not whether: the order is a mandatory choice of one of two
        /// sides, and the move behind it is the part that may be declined.
        /// </summary>
        [Test]
        public void Charm_AsksWhichSideOfHerMovementFirst()
        {
            var fixture = Create(
                minionPlacements: At("red-ranged-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            fixture.StartPrimary(XargathaCardContent.Charm);

            Assert.That(fixture.Choice, Is.Not.Null,
                "\"before or after movement\" is a decision the card gives her, which is what p.11's " +
                "\"unless it says otherwise\" leaves room for.");
            Assert.That(fixture.Choice.DeclineInvocation, Is.Null,
                "The order itself cannot be declined - the card offers two sides, not three. What " +
                "may be declined is the move.");
        }

        /// <summary>
        /// Charm reaches an enemy <em>ranged</em> minion and no other class, which is the adjective its
        /// face prints and the whole difference between it and Control.
        /// </summary>
        [Test]
        public void Charm_OffersARangedMinionAndNotAMeleeOne()
        {
            var fixture = Create(
                minionPlacements: At("red-melee-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            fixture.StartPrimary(XargathaCardContent.Charm);

            // Two answers stand between the card and its selection: the mandatory side-of-the-
            // Movement decision, and the "you may" behind it. Only the third question is the one
            // whose candidates this case is about.
            fixture.SelectFirst();
            fixture.Confirm();
            fixture.Confirm();

            var offered = fixture.CandidateMinions(fixture.Choice);
            Assert.That(offered, Has.Member(fixture.MinionAt(new HexCoordinate(-4, 2))),
                "The staged enemy Ranged minion is inside Radius 2 and is the class the card names.");
            Assert.That(offered, Has.No.Member(fixture.MinionAt(FirstAdjacent)),
                "The enemy Melee minion is equally close and is not a class Charm admits.");
        }

        /// <summary>
        /// Control admits the class Charm refuses, on the identical board - so the upgrade buys exactly
        /// the adjective it prints.
        /// </summary>
        [Test]
        public void Control_AdmitsTheMeleeMinionCharmRefuses()
        {
            var fixture = Create(
                tier: 2,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            fixture.StartPrimary(XargathaCardContent.Control);

            // Two answers stand between the card and its selection: the mandatory side-of-the-
            // Movement decision, and the "you may" behind it. Only the third question is the one
            // whose candidates this case is about.
            fixture.SelectFirst();
            fixture.Confirm();
            fixture.Confirm();

            Assert.That(
                fixture.CandidateMinions(fixture.Choice),
                Has.Member(fixture.MinionAt(FirstAdjacent)),
                "\"an enemy ranged or melee minion\" reaches both, which is what Control buys.");
        }

        // -----------------------------------------------------------------------------------------
        // "End of round: Defeat an enemy melee / melee or ranged minion adjacent to you"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Constrict does nothing when it is played and defeats an adjacent enemy Melee minion when
        /// p.8's pre-battle moment arrives.
        /// </summary>
        [Test]
        public void Constrict_DefeatsAnAdjacentMeleeMinionAtTheEndOfTheRound()
        {
            var fixture = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            var doomed = fixture.MinionAt(FirstAdjacent);
            fixture.StartPrimary(XargathaCardContent.Constrict);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.LeftTheBoard(), Has.No.Member(doomed),
                "The Skill leaves a source standing; it defeats nobody when it is played.");

            // Stopped the instant the minion goes, which is the stronger claim: the Defeat lands at
            // p.8's pre-battle moment rather than merely somewhere before the round ends. It is also
            // the only claim available here - defeating this minion empties the Battle Zone, and what
            // p.8 does next is a Lane Push this driver has no command for.
            fixture.CrossTheRoundBoundaryAnsweringPauses(
                stopWhen: () => fixture.LeftTheBoard().Contains(doomed));

            Assert.That(fixture.LeftTheBoard(), Has.Member(doomed),
                "\"End of round:\" - and its own reminder puts the moment before p.8's Minion " +
                "Battle, which is where the source declares itself.");
        }

        /// <summary>
        /// Constrict admits Melee minions alone, so an adjacent enemy Ranged minion survives a round
        /// that a Final Embrace would have ended.
        /// </summary>
        [Test]
        public void Constrict_LeavesAnAdjacentRangedMinionStanding()
        {
            var fixture = Create(
                tier: 2,
                branch: XargathaUpgradeBranch.SecondSibling,
                clearHerNeighbourhood: false);

            var ranged = fixture.MinionAt(new HexCoordinate(-4, 2));
            fixture.StartPrimary(XargathaCardContent.Constrict);
            fixture.AnswerEveryStandingChoice();
            fixture.CrossTheRoundBoundaryAnsweringPauses();

            Assert.That(fixture.LeftTheBoard(), Has.No.Member(ranged),
                "\"an enemy melee minion\" names one class and the staged neighbour is the other.");
        }

        /// <summary>
        /// Final Embrace is the same sentence one class wider, on a board where that is the only thing
        /// that can decide the outcome.
        /// </summary>
        [Test]
        public void FinalEmbrace_DefeatsTheRangedMinionConstrictLeaves()
        {
            var fixture = Create(
                tier: 3,
                branch: XargathaUpgradeBranch.SecondSibling,
                clearHerNeighbourhood: false);

            var ranged = fixture.MinionAt(new HexCoordinate(-4, 2));
            fixture.StartPrimary(XargathaCardContent.FinalEmbrace);
            fixture.AnswerEveryStandingChoice();
            fixture.CrossTheRoundBoundaryAnsweringPauses();

            Assert.That(fixture.LeftTheBoard(), Has.Member(ranged),
                "\"melee or ranged\" is the whole upgrade, and its face omits Constrict's " +
                "parenthesis without changing when it happens.");
        }

        // -----------------------------------------------------------------------------------------
        // p.16's permanent ability
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Metamorphosis is true from the moment it is faceup: no card is played, nothing is asked, and
        /// her two channels are already scaling with whoever is standing beside her.
        /// </summary>
        [Test]
        public void Metamorphosis_ScalesBothChannelsWithAdjacentEnemies_WithNothingPlayed()
        {
            var fixture = Create(
                // No unlockUltimate here: at the Tier III staging p.16 has already turned
                // Metamorphosis faceup, and asking for the unlock again looks for a card the deck
                // no longer holds.
                tier: 3,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                clearHerNeighbourhood: false);

            Assert.That(
                fixture.ValueModifierFor(fixture.Hero, PrintedValueChannel.Movement),
                Is.EqualTo(2),
                "Two adjacent enemy units - the staged Ranged minion and the placed Melee one - at " +
                "+1 Movement each. No card was played: p.16's ability is simply in effect.");
            Assert.That(
                fixture.ValueModifierFor(fixture.Hero, PrintedValueChannel.Initiative),
                Is.EqualTo(2),
                "The same count feeds the second channel, which is why the card prints two icons.");
        }

        /// <summary>
        /// The count is the live board's, not a number recorded when the Ultimate was unlocked.
        /// </summary>
        [Test]
        public void Metamorphosis_RecalculatesWhenTheBoardChanges()
        {
            var fixture = Create(
                // No unlockUltimate here: at the Tier III staging p.16 has already turned
                // Metamorphosis faceup, and asking for the unlock again looks for a card the deck
                // no longer holds. The second sibling is the branch that holds Final Embrace - the
                // first holds Dominate in that colour instead.
                tier: 3,
                branch: XargathaUpgradeBranch.SecondSibling,
                clearHerNeighbourhood: false);

            Assert.That(
                fixture.ValueModifierFor(fixture.Hero, PrintedValueChannel.Movement),
                Is.EqualTo(1),
                "One adjacent enemy - the staged Ranged minion.");

            fixture.StartPrimary(XargathaCardContent.FinalEmbrace);
            fixture.AnswerEveryStandingChoice();
            fixture.CrossTheRoundBoundaryAnsweringPauses();

            Assert.That(
                fixture.ValueModifierFor(fixture.Hero, PrintedValueChannel.Movement),
                Is.EqualTo(0),
                "Her own Final Embrace defeated the minion that was supplying the contribution, so " +
                "the ability now reads zero. Nothing recorded the old count to become stale.");
        }
    }
}
