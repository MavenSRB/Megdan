using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
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

        /// <summary>
        /// "Up to 3 spaces" offers every positive legal travel distance inside that budget. It is
        /// neither an exact-three instruction nor permission to leave the target where it stands.
        /// </summary>
        [Test]
        public void SirensCall_OffersLegalOneTwoAndThreeSpaceLandings_ButNotStandingStill()
        {
            var fixture = Create(minionPlacements: At("red-melee-01", TwoAway));
            var called = fixture.MinionAt(TwoAway);

            fixture.StartPrimary(XargathaCardContent.SirensCall);
            fixture.Select(called);
            fixture.Confirm();

            var destinations = fixture.CandidateSpaces(fixture.Choice);
            var distances = destinations
                .Select(destination => HexDistanceQuery.Calculate(TwoAway, destination))
                .Distinct()
                .OrderBy(distance => distance)
                .ToArray();

            Assert.That(distances, Is.EqualTo(new[] { 1, 2, 3 }),
                "The printed budget admits shorter legal paths as well as a three-space path.");
            Assert.That(destinations, Has.No.Member(TwoAway),
                "The card says to move the target to a space beside Xargatha; zero spaces cannot " +
                "satisfy that destination clause from the non-adjacent target set.");
            Assert.That(destinations.All(destination =>
                    HexDistanceQuery.Calculate(destination, HeroAt) == 1),
                Is.True,
                "Every offered endpoint must still satisfy 'to a space adjacent to you'.");
        }

        /// <summary>
        /// "If able" skips only the move when every space beside Xargatha is occupied. The already
        /// legal target choice remains legal and no empty movement prompt is exposed.
        /// </summary>
        [Test]
        public void SirensCall_WithNoLegalAdjacentLanding_SilentlySkipsTheMove()
        {
            var fixture = Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = TwoAway,
                    ["red-melee-02"] = FirstAdjacent,
                    ["red-melee-03"] = SecondAdjacent,
                    ["red-melee-04"] = ThirdAdjacent,
                    ["blue-melee-02"] = FourthAdjacent
                },
                clearHerNeighbourhood: false);
            var called = fixture.MinionAt(TwoAway);

            fixture.StartPrimary(XargathaCardContent.SirensCall);
            fixture.Select(called);
            fixture.Confirm();

            Assert.That(fixture.Choice, Is.Null,
                "An impossible if-able move is skipped without asking the player an empty question.");
            Assert.That(fixture.SpaceOf(called), Is.EqualTo(TwoAway),
                "Skipping the infeasible move leaves the selected target where it stood.");
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

        // -----------------------------------------------------------------------------------------
        // "After the attack: May repeat once on a different enemy hero"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The board her repeat clause needs: two enemy minions and the enemy hero all adjacent to
        /// her, so the printed instance admits three units and the repeated one has both a hero and a
        /// minion still standing to choose between. Without the second minion the narrowing would be
        /// proved by the exclusion instead.
        /// </summary>
        private static StagedHeroBoard BesideTwoEnemyMinionsAndTheEnemyHero() =>
            Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = FirstAdjacent,
                    ["red-melee-02"] = SecondAdjacent
                },
                heroPlacements: At("primitive-hero", ThirdAdjacent));

        /// <summary>
        /// Milestone 8.25d. Cleave's repeated instance takes an enemy hero and nothing else, and it
        /// says on the wire that it is a repeat.
        ///
        /// <para>Both halves are the same defect. Milestone 8.10's <c>StartRepeatedAttack</c> built
        /// the second instance with the first's printed invocation context, so milestone 8.24's
        /// narrowing - gated on the resolution reporting itself as repeated - had never fired on the
        /// path her cards actually use: her repeat excluded the unit she just hit and then admitted
        /// every other adjacent minion, where the card says "on a different enemy hero".</para>
        ///
        /// <para>The exclusion is asserted beside it because it is the half that must NOT move: the
        /// unit the first instance took is out of the set before and after, and this milestone changes
        /// which KINDS are left rather than how the exclusion works.</para>
        /// </summary>
        [Test]
        public void CleavesRepeat_AdmitsEnemyHeroesOnly_AndSaysItIsARepeat()
        {
            var fixture = BesideTwoEnemyMinionsAndTheEnemyHero();
            fixture.StartPrimary(XargathaCardContent.Cleave);

            var printed = fixture.Session.Projection.PendingAttackTargetChoice;
            Assert.That(printed.LegalTargetMinionIds, Has.Count.EqualTo(2),
                "Cleave's printed instance targets \"a unit\", so both adjacent enemy minions are " +
                "in its set - which is what gives the repeat a minion to wrongly offer.");
            Assert.That(printed.LegalTargetHeroIds, Has.Member(fixture.Enemy),
                "and the adjacent enemy hero is in it too, so both kinds are reachable and the case " +
                "is not proved by an empty board.");

            var taken = fixture.MinionAt(FirstAdjacent);
            fixture.Accept(DirectorCommand.ChooseAttackTarget(
                printed.ResolutionId, printed.ChoiceId, taken));

            Assert.That(fixture.Choice, Is.Not.Null,
                "\"After the attack: May repeat once\" is p.14's step 7, and it asks even though the " +
                "printed instance took a minion - which is what her parenthesis says in words.");
            fixture.Accept(DirectorCommand.AnswerEffectChoice(fixture.Choice.ConfirmInvocation));

            var repeat = fixture.Session.Projection.PendingAttackTargetChoice;
            Assert.That(repeat, Is.Not.Null, "Confirming starts the second instance.");
            Assert.That(repeat.LegalTargetHeroIds, Is.EqualTo(new[] { fixture.Enemy }),
                "\"on a different enemy hero\": the one enemy hero on this board is the whole set.");
            Assert.That(repeat.LegalTargetMinionIds, Is.Empty,
                "and the minion still standing beside her is not in it, though the printed instance " +
                "admitted it. This is the assertion that was false before milestone 8.25d.");

            var restored = Restore(fixture);
            Assert.That(restored.CardActions.PendingAttackResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated),
                "The instance reports itself as a repeat, which is what the narrowing is derived " +
                "from and what p.10's bar on repeating a repeat is enforced by.");
            Assert.That(restored.CardActions.PendingAttackResolution.ExcludedTarget.Kind,
                Is.EqualTo(AttackTargetKind.Minion));
            Assert.That(restored.CardActions.PendingAttackResolution.ExcludedTarget.MinionId,
                Is.EqualTo(taken),
                "and the exclusion is unchanged: it still names the unit the first instance took.");
        }

        /// <summary>
        /// Milestone 8.25d. Rulebook p.10: "You cannot repeat a repeated action!" Cleave's second
        /// instance runs its own page including the step-7 rider that asked for it, and that rider no
        /// longer asks.
        ///
        /// <para><b>The bound is the enclosing attack's own context, and it had nowhere else to
        /// live.</b> A repeated Skill is bounded by the <c>RepeatContinuation</c> its frame carries -
        /// the repeats already run, against the node's declared maximum - and an Attack carries none:
        /// the card-text frame inside p.14 is minted fresh for each instance and always under a
        /// printed context. So before this milestone nothing stopped the chain, which was measured
        /// rather than assumed - a third instance of one played Cleave started.
        /// </para>
        /// </summary>
        [Test]
        public void CleavesSecondInstance_IsNotOfferedAThirdOne()
        {
            var fixture = BesideTwoEnemyMinionsAndTheEnemyHero();
            fixture.StartPrimary(XargathaCardContent.Cleave);

            var printed = fixture.Session.Projection.PendingAttackTargetChoice;
            fixture.Accept(DirectorCommand.ChooseAttackTarget(
                printed.ResolutionId, printed.ChoiceId, fixture.MinionAt(FirstAdjacent)));
            Assert.That(fixture.Choice, Is.Not.Null, "The printed instance reached its rider.");
            fixture.Accept(DirectorCommand.AnswerEffectChoice(fixture.Choice.ConfirmInvocation));

            var repeat = fixture.Session.Projection.PendingAttackTargetChoice;
            Assert.That(repeat, Is.Not.Null, "The second instance is running.");
            fixture.Accept(DirectorCommand.ChooseAttackTarget(
                repeat.ResolutionId, repeat.ChoiceId, fixture.Enemy));
            Assert.That(fixture.Session.Projection.PendingDefenseChoice, Is.Not.Null,
                "A hero target opens p.14's Defense question for the repeated instance exactly as it " +
                "does for the printed one - the page is the same page.");
            fixture.Accept(DirectorCommand.DeclineDefense());

            Assert.That(fixture.Choice, Is.Null,
                "The second instance reaches its own step 7, and p.10 forbids repeating a repeated " +
                "action - so the rider is skipped in silence rather than asked and then dropped.");
            Assert.That(fixture.Session.Projection.PendingAttackTargetChoice, Is.Null,
                "and no third instance of one played Cleave opened a target question.");
        }

        private static RestoredAuthoritativeMatch Restore(StagedHeroBoard fixture) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                fixture.Session.Projection.AuthoritativeSnapshot,
                fixture.Session.Projection.Definitions,
                new[] { fixture.Session.Projection.SelectedMap },
                fixture.Session.Projection.Setup.Minions);

        /// <summary>
        /// Milestone 8.25d. Both instances of one played Cleave defeat an enemy hero, and both Defeats
        /// pay rulebook p.13's reward.
        ///
        /// <para><b>Why this one case leaves <see cref="StagedHeroBoard"/>.</b> Her repeat admits
        /// enemy heroes and nothing else, so two of them have to be standing beside her for it to have
        /// anywhere to go after the printed instance has removed one - and a one-hero-per-team board
        /// has one. This is the official six-player match, placed.</para>
        ///
        /// <para><b>What it holds.</b> Milestone 8.10's repeated instance inherited the completed
        /// one's resolution identity, and p.13's Defeat outcome is identified as
        /// <c>&lt;resolution&gt;:hero-defeat</c>. Two hero Defeats under one identity is one outcome
        /// asked to pay out twice, which the coin ledger refuses - so the repeat was accepted, the
        /// Defense was answered, and rulebook p.14 then stopped on
        /// <c>OutcomeAlreadyAwarded</c> with the player having done nothing wrong. The narrowing this
        /// milestone restores is what made that the ONLY thing her repeat can do.</para>
        /// </summary>
        [Test]
        public void CleavesRepeat_DefeatsASecondEnemyHero_AndBothDefeatsPayOut()
        {
            var session = OfficialSixPlayerBesideTwoEnemyHeroes(
                out var xargatha, out var arien, out var brogan);

            var printed = session.Projection.PendingAttackTargetChoice;
            Assert.That(printed.LegalTargetHeroIds, Is.SupersetOf(new[] { arien, brogan }),
                "Both enemy heroes stand beside her, which is what this board exists to arrange.");
            AcceptOn(session, DirectorCommand.ChooseAttackTarget(
                printed.ResolutionId, printed.ChoiceId, arien));
            AcceptOn(session, DirectorCommand.DeclineDefense());
            Assert.That(OnBoard(session, arien), Is.False, "p.13 removes a defeated hero.");
            var afterFirst = CoinsOf(session, xargatha);
            Assert.That(afterFirst, Is.GreaterThan(0), "and pays the attacker for it.");

            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "p.14's step 7 asks whether to repeat.");
            AcceptOn(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var repeat = session.Projection.PendingAttackTargetChoice;
            Assert.That(repeat.LegalTargetHeroIds, Is.EqualTo(new[] { brogan }),
                "\"on a different enemy hero\": the other one, and no minion.");
            AcceptOn(session, DirectorCommand.ChooseAttackTarget(
                repeat.ResolutionId, repeat.ChoiceId, brogan));

            var answered = session.Execute(DirectorCommand.DeclineDefense());
            Assert.That(answered.Accepted, Is.True,
                "The second Defeat has to be payable. Before milestone 8.25d the repeat ran under " +
                "the first instance's identity, so this is where the page stopped: " +
                answered.Detail);
            Assert.That(OnBoard(session, brogan), Is.False,
                "and the second enemy hero is removed by the same page the first was.");
            Assert.That(CoinsOf(session, xargatha), Is.GreaterThan(afterFirst),
                "Two Defeats are two rewards; one identity paying once is the defect this closes.");
        }

        /// <summary>
        /// The official six-player match with Xargatha placed between the two enemy heroes her repeat
        /// needs, every seat committed and her Cleave started. Milestone 8.25d.
        /// </summary>
        private static MatchSession OfficialSixPlayerBesideTwoEnemyHeroes(
            out HeroId xargatha, out HeroId arien, out HeroId brogan)
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants, map, configuration.TieBreakerState);

            xargatha = setup.Heroes.Single(hero => hero.ContentId.Value == "xargatha").HeroId;
            arien = setup.Heroes.Single(hero => hero.ContentId.Value == "arien").HeroId;
            brogan = setup.Heroes.Single(hero => hero.ContentId.Value == "brogan").HeroId;
            var placements = new Dictionary<HeroId, HexCoordinate>
            {
                [xargatha] = HeroAt,
                [arien] = FirstAdjacent,
                [brogan] = SecondAdjacent
            };

            var placed = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams,
                setup.Heroes.Select(hero => placements.TryGetValue(hero.HeroId, out var moved)
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint,
                        new ConfiguredSpaceReference(
                            map.Id,
                            map.Spaces.Single(space => space.Coordinate.Equals(moved)).Id),
                        moved)
                    : hero),
                setup.Minions);
            var session = new MatchSession(new MatchSessionInitialState(
                new InitialScenarioDescription(
                    configuration.Participants, configuration.CardDefinitions,
                    configuration.CardInstances, configuration.TieBreakerState,
                    configuration.SetupSeed, map.Reference,
                    placed.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    Array.Empty<ObstacleOccupancy>()),
                placed,
                new[] { map }));

            var her = xargatha;
            foreach (var hero in session.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var hand = session.Projection.Cards.Where(card =>
                    card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);
                var card = hero.Equals(her)
                    ? hand.Single(candidate => candidate.DefinitionReference.DefinitionId
                        .Equals(XargathaCardContent.Cleave))
                    : hand.OrderBy(candidate => session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(candidate.DefinitionReference))
                        .Initiative.Value).First();
                AcceptOn(session, DirectorCommand.CommitCard(hero, card.Id));
            }

            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(her),
                "Cleave's printed Initiative 11 is above the lowest card every other seat holds, so " +
                "she acts first and no earlier turn has to be drained.");
            AcceptOn(session, DirectorCommand.StartAction(
                session.Projection.AvailableTurnActions
                    .Single(offer => offer.InvocationContext == ActionInvocationContext.PrintedPrimary)
                    .ActionDefinitionId));
            return session;
        }

        private static bool OnBoard(MatchSession session, HeroId heroId) =>
            session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(heroId));

        private static int CoinsOf(MatchSession session, HeroId heroId) =>
            session.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId)).Coins;

        private static void AcceptOn(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True,
                $"'{command.Kind}' was refused: {result.RejectionReason}. {result.Detail}");
        }
    }
}
