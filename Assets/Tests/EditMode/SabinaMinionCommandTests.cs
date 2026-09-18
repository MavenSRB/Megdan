using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.SabinaBoard;
using static GuardsDigital.Tests.EditMode.StagedHeroBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.22. The fifteen Sabina faces that command pieces rather than attack them, plus the
    /// three Red Attacks whose printed value depends on what their target played.
    ///
    /// <para>Her Blue and Green kit is what makes her different from the two heroes dealt before her:
    /// every face of it names minions - swap with one, swap two of them, move one and repeat, or count
    /// them before anything happens at all. Those are five printed clauses across fifteen cards, and
    /// each is asked here on a board built to answer it, because the staged engagement answers most of
    /// them the same way by accident.</para>
    ///
    /// <para>What each card composes - which node, which capability, which literal - is
    /// <see cref="SabinaVerifiedContentTests"/>'s and is not repeated. This file asks what a player is
    /// offered and what the board looks like afterwards. <see cref="StagedHeroBoard"/> owns the
    /// board.</para>
    /// </summary>
    public sealed class SabinaMinionCommandTests
    {
        // -----------------------------------------------------------------------------------------
        // "Swap with a friendly minion in radius"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Back to Back exchanges Sabina's own space with the friendly minion she names, and offers
        /// only friendly minions inside its printed Radius 2.
        /// </summary>
        [Test]
        public void BackToBack_SwapsHerWithAFriendlyMinion_AndOffersNoEnemyAndNothingOutOfRadius()
        {
            var fixture = Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = FirstAdjacent,
                    ["blue-ranged-01"] = ThreeAway,
                    ["red-melee-01"] = SecondAdjacent
                });
            var partner = fixture.MinionAt(FirstAdjacent);

            fixture.StartPrimary(SabinaCardContent.BackToBack);

            var offered = fixture.CandidateMinions(fixture.Choice);
            Assert.That(offered, Is.EqualTo(new[] { partner }),
                "\"a friendly minion in radius\": the enemy Melee minion beside her is not friendly " +
                "and the friendly minion three spaces away is outside the printed Radius 2.");

            fixture.SelectAndConfirm(partner);

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(FirstAdjacent),
                "A Swap is an exchange rather than a move: she ends on the space the minion held.");
            Assert.That(fixture.SpaceOf(partner), Is.EqualTo(HeroAt),
                "and the minion ends on hers.");
        }

        // -----------------------------------------------------------------------------------------
        // "Swap two minions in radius", and the one face that ignores Heavy immunity
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Listen Up prints Radius 1 and Roger Roger prints Radius 2, and the difference is the
        /// candidates a player is offered - minions of either side, because the text says "two minions"
        /// rather than two friendly ones.
        /// </summary>
        [Test]
        public void TheSwapPair_OffersBothSidesMinions_WithinEachFacesOwnPrintedRadius()
        {
            var placements = new Dictionary<string, HexCoordinate>
            {
                ["blue-melee-01"] = FirstAdjacent,
                ["red-melee-01"] = SecondAdjacent,
                ["red-melee-02"] = TwoAway
            };

            var listenUp = Create(minionPlacements: placements);
            listenUp.StartPrimary(SabinaCardContent.ListenUp);
            Assert.That(
                listenUp.CandidateMinions(listenUp.Choice),
                Is.EquivalentTo(new[]
                {
                    listenUp.MinionAt(FirstAdjacent), listenUp.MinionAt(SecondAdjacent)
                }),
                "Radius 1 reaches the two minions standing beside her and not the one two spaces " +
                "away, and both sides are named because the text says \"two minions\".");

            var rogerRoger = Create(tier: 2, minionPlacements: placements);
            rogerRoger.StartPrimary(SabinaCardContent.RogerRoger);
            Assert.That(
                rogerRoger.CandidateMinions(rogerRoger.Choice),
                Has.Member(rogerRoger.MinionAt(TwoAway)),
                "Radius 2 is the Tier II upgrade, and the minion two spaces away is what it buys.");
        }

        /// <summary>
        /// The exchange itself: the two minions named end on each other's spaces and Sabina does not
        /// move.
        /// </summary>
        [Test]
        public void TheSwapPair_ExchangesTheTwoMinionsNamed_AndLeavesSabinaWhereSheStood()
        {
            var fixture = Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = FirstAdjacent,
                    ["red-melee-01"] = SecondAdjacent
                });
            var friendly = fixture.MinionAt(FirstAdjacent);
            var enemy = fixture.MinionAt(SecondAdjacent);

            fixture.StartPrimary(SabinaCardContent.ListenUp);
            fixture.SelectAndConfirm(friendly, enemy);

            Assert.That(fixture.SpaceOf(friendly), Is.EqualTo(SecondAdjacent));
            Assert.That(fixture.SpaceOf(enemy), Is.EqualTo(FirstAdjacent));
            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(HeroAt),
                "She is not one of the two pieces the text names.");
        }

        /// <summary>
        /// Ready and Waiting's printed override, stated as the difference it makes to the candidates.
        ///
        /// <para>Its Tier II sibling is the same selection at the same Radius, so an immune Heavy
        /// minion inside the disk is the one thing that tells them apart: Roger Roger cannot name it
        /// and Ready and Waiting can, because "ignoring heavy minion immunity" is what the Tier III
        /// card buys.</para>
        /// </summary>
        [Test]
        public void ReadyAndWaiting_NamesAnImmuneHeavyMinion_WhereItsSiblingCannot()
        {
            var placements = new Dictionary<string, HexCoordinate>
            {
                ["blue-melee-01"] = FirstAdjacent,
                ["red-heavy-01"] = SecondAdjacent
            };

            var sibling = Create(tier: 2, minionPlacements: placements);
            sibling.StartPrimary(SabinaCardContent.RogerRoger);
            Assert.That(
                sibling.CandidateMinions(sibling.Choice),
                Has.No.Member(sibling.MinionAt(SecondAdjacent)),
                "p.18 makes the Heavy minion immune while its own side keeps a non-Heavy minion in " +
                "the Battle Zone, and a face that does not override the immunity cannot name it.");

            var override_ = Create(tier: 3, minionPlacements: placements);
            override_.StartPrimary(SabinaCardContent.ReadyAndWaiting);
            var heavy = override_.MinionAt(SecondAdjacent);
            Assert.That(override_.CandidateMinions(override_.Choice), Has.Member(heavy),
                "\"ignoring heavy minion immunity\" - the same selection, at the same Radius, with " +
                "the one clause that differs between the two faces.");

            override_.SelectAndConfirm(override_.MinionAt(FirstAdjacent), heavy);
            Assert.That(override_.SpaceOf(heavy), Is.EqualTo(FirstAdjacent),
                "and the override reaches the exchange rather than only the offer.");
        }

        // -----------------------------------------------------------------------------------------
        // "Move a friendly minion in radius 1 space, to a space in radius. May repeat..."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The two bounds the sentence prints twice and means twice: the minion must start inside her
        /// disk, and the space it moves to must be inside it as well - while the step itself is one
        /// space.
        /// </summary>
        [Test]
        public void TroopMovement_MovesAFriendlyMinionOneSpace_AndOnlyToASpaceInsideHerOwnRadius()
        {
            var fixture = Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = FirstAdjacent,
                    ["blue-ranged-01"] = SecondThreeAway,
                    ["red-melee-01"] = SecondAdjacent
                });
            var subject = fixture.MinionAt(FirstAdjacent);

            fixture.StartPrimary(SabinaCardContent.TroopMovement);
            Assert.That(fixture.CandidateMinions(fixture.Choice), Is.EqualTo(new[] { subject }),
                "\"a friendly minion in radius\" - the enemy minion beside her is not friendly and " +
                "the friendly one three spaces away is outside the printed Radius 2.");

            fixture.SelectAndConfirm(subject);

            var destinations = fixture.CandidateSpaces(fixture.Choice);
            Assert.That(destinations, Is.Not.Empty, "The move opens its own question.");
            Assert.That(
                destinations.All(space =>
                    HexDistanceQuery.Calculate(space, FirstAdjacent) == 1),
                Is.True,
                "\"1 space\" is one step from where the minion is standing.");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, HeroAt) <= 2),
                Is.True,
                "\"to a space in radius\" is the second bound, measured from her rather than from " +
                "the minion - so a lateral step out of her disk is not offered even though it is " +
                "one space.");
        }

        /// <summary>
        /// The repeat, and its decline. Troop Movement and Marching Orders print "May repeat once" and
        /// Path to Victory prints "May repeat up to two times", so the number of times the card comes
        /// back round is what separates the Tier III face from its two ancestors.
        /// </summary>
        [Test]
        public void TheDirectMove_RepeatsAsOftenAsItsFacePrints_AndTheRepeatCanBeDeclined()
        {
            Assert.That(RepeatsTaken(1, SabinaCardContent.TroopMovement), Is.EqualTo(2),
                "\"May repeat once\": the printed move plus one repetition is two moves.");
            Assert.That(RepeatsTaken(2, SabinaCardContent.MarchingOrders), Is.EqualTo(2),
                "The Tier II face prints the same sentence with a wider Radius.");
            Assert.That(RepeatsTaken(3, SabinaCardContent.PathToVictory), Is.EqualTo(3),
                "\"May repeat up to two times\" is the Tier III upgrade, and it is a count rather " +
                "than a different kind of repetition.");

            var declined = Create(minionPlacements: At("blue-melee-01", FirstAdjacent));
            var subject = declined.MinionAt(FirstAdjacent);
            declined.StartPrimary(SabinaCardContent.TroopMovement);
            declined.SelectAndConfirm(subject);
            var firstStep = declined.CandidateSpaces(declined.Choice).First();
            declined.Select(firstStep);
            declined.Confirm();

            Assert.That(declined.Choice, Is.Not.Null, "The repeat asks before it happens.");
            Assert.That(declined.Choice.DeclineInvocation, Is.Not.Null,
                "\"May\" is p.10's optional step, and the standing ruling on this build is that a " +
                "printed may has to be declinable.");
            declined.Decline();

            Assert.That(declined.SpaceOf(subject), Is.EqualTo(firstStep),
                "The declined repetition leaves the minion where the first move put it.");
            Assert.That(declined.Choice, Is.Null, "and the card is finished.");
        }

        /// <summary>
        /// THE REGRESSION GUARD FOR THE PHANTOM DECLARATION. These three faces declared
        /// <c>RepeatTargetBinding.DifferentTarget</c> until it was removed, and not one of them prints
        /// a clause supporting it: "Move a friendly minion in radius 1 space, to a space in radius. May
        /// repeat once / up to two times." names no subject for the repetition at all.
        ///
        /// <para><b>Why this case exists and the one above it does not cover the question.</b>
        /// <see cref="TheDirectMove_RepeatsAsOftenAsItsFacePrints_AndTheRepeatCanBeDeclined"/> counts
        /// repetitions on a board deliberately holding ONE friendly minion in the disk - its own
        /// comment says so, because a repetition that moved somebody else would read as this one
        /// standing still. A one-minion board cannot tell a free repeat from a constrained one: with
        /// nobody else to name, both offer the same single answer. So it is blind to exactly the claim
        /// the removed declaration made. This board holds TWO, which is the smallest board on which the
        /// question has a visible answer.</para>
        ///
        /// <para><b>What it pins.</b> The repeated instance chooses from scratch, so the minion the
        /// printed instance just moved is offered AGAIN and can be moved a second time. That is the
        /// plain reading of a sentence that names no subject, and it is p.10's default
        /// (<c>RepeatTargetBinding.Unbound</c>) rather than anything these cards declare. Had
        /// the phantom ever been given a route into Skill repeats - the change the original finding
        /// invited - this case would fail, which is the whole point of writing it down.</para>
        /// </summary>
        [Test]
        public void TheRepeat_OffersEveryLegalMinionAgain_IncludingTheOneItJustMoved()
        {
            var fixture = Create(
                tier: 3,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = FirstAdjacent,
                    ["blue-ranged-01"] = SecondAdjacent
                },
                banishedMinions: new[] { "blue-heavy-01" });

            var moved = fixture.MinionAt(FirstAdjacent);
            var other = fixture.MinionAt(SecondAdjacent);

            fixture.StartPrimary(SabinaCardContent.PathToVictory);
            Assert.That(
                fixture.CandidateMinions(fixture.Choice),
                Is.EquivalentTo(new[] { moved, other }),
                "Both friendly minions stand inside the printed Radius 3.");

            fixture.SelectAndConfirm(moved);
            var firstStep = fixture.CandidateSpaces(fixture.Choice).First();
            fixture.Select(firstStep);
            fixture.Confirm();

            Assert.That(fixture.Choice, Is.Not.Null, "The repeat asks before it happens.");
            fixture.Confirm();

            Assert.That(
                fixture.CandidateMinions(fixture.Choice),
                Is.EquivalentTo(new[] { moved, other }),
                "THE POINT: the repetition chooses from scratch, so the minion the printed instance " +
                "just moved is offered again beside the one it did not touch. A different-target " +
                "binding would have removed it, and no word on any of these three faces asks for " +
                "that.");

            fixture.SelectAndConfirm(moved);
            var secondStep = fixture.CandidateSpaces(fixture.Choice).First();
            fixture.Select(secondStep);
            fixture.Confirm();

            Assert.That(
                fixture.SpaceOf(moved), Is.EqualTo(secondStep),
                "and naming it again really moves it a second time.");
            Assert.That(
                fixture.SpaceOf(other), Is.EqualTo(SecondAdjacent),
                "The minion that was never named never moved - the repetition is a free choice " +
                "rather than a forced rotation.");
        }

        // -----------------------------------------------------------------------------------------
        // "If there are two or more friendly minions in radius..."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Steady Advance's gate is a count, and a count of one is not two: the whole sentence is
        /// skipped, and the same card on a board with a second friendly minion in the disk runs.
        /// </summary>
        [Test]
        public void SteadyAdvance_RunsNothingWithOneFriendlyMinionInRadius_AndRunsWithTwo()
        {
            var one = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: At("blue-melee-01", FirstAdjacent),
                sabinaDiscards: new[] { SabinaCardContent.BackToBack });
            one.StartPrimary(SabinaCardContent.SteadyAdvance);
            Assert.That(one.Choice, Is.Null,
                "One friendly minion is in the disk and the card asks for two or more, so the " +
                "conditional gates everything under it - including the move, which is inside the " +
                "same sentence rather than beside it.");

            var two = TwoFriendlyMinionsInRadius(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling);
            two.StartPrimary(SabinaCardContent.SteadyAdvance);
            Assert.That(two.Choice, Is.Not.Null,
                "Two friendly minions in radius is the printed threshold.");
        }

        /// <summary>
        /// What the gate opens: an optional retrieve, and then a move of the printed length.
        ///
        /// <para>Steady Advance prints "move up to 1 space" and Unwavering Resolve prints "up to 2",
        /// and the retrieve is a "may" on both - so a player who wants the move and not the card can
        /// decline the first half and still take the second.</para>
        /// </summary>
        [Test]
        public void SteadyAdvance_OffersTheDiscardedCardAndThenTheMove_AndTheRetrieveCanBeDeclined()
        {
            var fixture = TwoFriendlyMinionsInRadius(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling);
            Assert.That(fixture.DiscardPile(fixture.Hero),
                Is.EqualTo(new[] { SabinaCardContent.BackToBack }),
                "The board this case needs is one where something has been discarded, because a " +
                "retrieve with nothing to name would prove the empty pile rather than the card.");

            fixture.StartPrimary(SabinaCardContent.SteadyAdvance);
            Assert.That(fixture.Choice.DeclineInvocation, Is.Not.Null,
                "\"you may retrieve a discarded card\" is optional, so it offers a decline.");
            fixture.Decline();

            var destinations = fixture.CandidateSpaces(fixture.Choice);
            Assert.That(destinations, Is.Not.Empty,
                "\"then move up to 1 space\" runs whether or not the retrieve was taken: the two " +
                "are steps of one sentence rather than two halves of one choice.");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, HeroAt) <= 1),
                Is.True,
                "\"up to 1 space\" is the Tier II printed allowance.");
            Assert.That(fixture.Hand(fixture.Hero),
                Has.No.Member(SabinaCardContent.BackToBack),
                "The declined retrieve left the card in the discard pile.");
        }

        /// <summary>
        /// The retrieve taken rather than declined, and the Tier III card's wider move.
        /// </summary>
        [Test]
        public void UnwaveringResolve_TakesTheDiscardedCardBackAndMovesTheFurtherPrintedDistance()
        {
            var fixture = TwoFriendlyMinionsInRadius(
                tier: 3, branch: SabinaUpgradeBranch.SecondSibling);

            fixture.StartPrimary(SabinaCardContent.UnwaveringResolve);
            fixture.SelectFirst();
            fixture.Confirm();

            Assert.That(fixture.Hand(fixture.Hero), Has.Member(SabinaCardContent.BackToBack),
                "p.10's retrieve puts a discarded card back into its owner's hand.");
            Assert.That(fixture.DiscardPile(fixture.Hero), Is.Empty,
                "and takes it out of the pile it came from.");
            Assert.That(
                fixture.CandidateSpaces(fixture.Choice)
                    .Any(space => HexDistanceQuery.Calculate(space, HeroAt) == 2),
                Is.True,
                "\"then move up to 2 spaces\" is what the Tier III face prints where its Tier II " +
                "sibling prints one.");
        }

        // -----------------------------------------------------------------------------------------
        // "An enemy hero in radius adjacent to your friendly minion discards a card..."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Two bounds measured from two different places: the disk from Sabina, the adjacency from each
        /// of her own minions. A hero inside the disk with no friendly minion beside them is not named.
        /// </summary>
        [Test]
        public void CloseSupport_NamesAnEnemyHeroBesideOneOfHerMinions_AndNotOneMerelyInRadius()
        {
            var supported = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: At("blue-melee-01", AdjacentAgainstTerrain));
            supported.StartPrimary(SabinaCardContent.CloseSupport);
            Assert.That(supported.CandidateHeroes(supported.Choice),
                Is.EqualTo(new[] { supported.Enemy }),
                "The enemy hero is inside her Radius 3 and one of her minions is standing beside " +
                "them, which is both halves of the sentence.");

            var unsupported = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: At("blue-melee-01", SecondThreeAway));
            Assert.That(unsupported.IsPrimaryAvailable(SabinaCardContent.CloseSupport), Is.False,
                "The same hero is still inside her radius. What changed is the second bound: no " +
                "minion of hers is beside them, so the sentence names nobody - and milestone " +
                "7.8a1's rule greys an action with nothing to act on rather than starting it and " +
                "stopping.");
        }

        /// <summary>
        /// The discard is the named hero's own decision, and it costs them a card.
        /// </summary>
        [Test]
        public void CloseSupport_AsksTheNamedHeroToDiscard_AndTheCardLeavesTheirHand()
        {
            var fixture = Create(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling,
                minionPlacements: At("blue-melee-01", AdjacentAgainstTerrain));

            fixture.StartPrimary(SabinaCardContent.CloseSupport);
            fixture.SelectFirst();
            fixture.Confirm();

            var choice = fixture.Choice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.ChooserId, Is.EqualTo(fixture.Enemy),
                "p.10 gives a discard to the hero who pays it, and the sentence named that hero " +
                "rather than the acting one.");
            var before = fixture.Hand(fixture.Enemy).Count;
            fixture.SelectFirst();
            fixture.Confirm();

            Assert.That(fixture.Hand(fixture.Enemy).Count, Is.EqualTo(before - 1));
            Assert.That(fixture.DiscardPile(fixture.Enemy), Is.Not.Empty);
        }

        /// <summary>
        /// The one clause that separates the Tier III face from the Tier II one: a hero who cannot pay
        /// is defeated by Covering Fire and untouched by Close Support.
        ///
        /// <para>Close Support prints "discards a card, <b>if able</b>" and Covering Fire prints
        /// "discards a card, <b>or is defeated</b>". Both are asked on the same board, of a hero
        /// holding nothing, so the difference asserted is the printed one and not the position.</para>
        /// </summary>
        [Test]
        public void CoveringFire_DefeatsAHeroWhoCannotPay_WhereCloseSupportLeavesThemStanding()
        {
            var ifAble = EmptyHandedEnemyBesideHerMinion(
                tier: 2, branch: SabinaUpgradeBranch.SecondSibling);
            ifAble.StartPrimary(SabinaCardContent.CloseSupport);
            ifAble.AnswerEveryStandingChoice();
            Assert.That(ifAble.IsOnBoard(ifAble.Enemy), Is.True,
                "\"if able\" is the whole of Close Support's answer for a hero with nothing to " +
                "discard: the step could not be paid and the card stops there.");

            var orDefeated = EmptyHandedEnemyBesideHerMinion(
                tier: 3, branch: SabinaUpgradeBranch.SecondSibling);
            var livesBefore = orDefeated.Lives(TeamSide.Red);
            orDefeated.StartPrimary(SabinaCardContent.CoveringFire);
            orDefeated.AnswerEveryStandingChoice();

            Assert.That(orDefeated.IsOnBoard(orDefeated.Enemy), Is.False,
                "\"or is defeated\" is the escalation, and an empty hand is what reaches it.");
            // Milestone 8.49: p.13 charges the defeated hero's team its lowest card Tier in Life
            // counters, and this Tier III board's Wasp holds Tier III cards - so three, not the one
            // every hero used to cost while the Tier I cards in its deck were being read.
            Assert.That(orDefeated.Lives(TeamSide.Red), Is.EqualTo(livesBefore - 3),
                "p.13 charges the defeated hero's team Life counters equal to its lowest card Tier.");
        }

        // -----------------------------------------------------------------------------------------
        // "+N Attack if the target played an attack card this turn"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The three Red conditionals, read as the live Attack Total against the same target on the
        /// same board - once where the target committed an Attack card and once where they did not.
        /// </summary>
        [Test]
        public void TheThreeConditionalAttacks_RaiseTheirPrintedAmountOnlyAgainstAnAttackingTarget()
        {
            Assert.That(
                TotalAgainstWasp(1, SabinaCardContent.Quickdraw, WaspCardContent.MagneticDagger),
                Is.EqualTo(2 + 3),
                "Quickdraw prints Attack 2 and \"+3 Attack if the target played an attack card " +
                "this turn\", and p.10 makes the commitment the play.");
            Assert.That(
                TotalAgainstWasp(1, SabinaCardContent.Quickdraw, WaspCardContent.LiftUp),
                Is.EqualTo(2),
                "The same face against the same hero, who committed a Skill: the condition is " +
                "about what the target played rather than about the target.");

            Assert.That(
                TotalAgainstWasp(2, SabinaCardContent.Gunslinger, WaspCardContent.MagneticDagger),
                Is.EqualTo(3 + 3),
                "The Tier II face raises its own printed 3 by the same 3.");
            Assert.That(
                TotalAgainstWasp(3, SabinaCardContent.DeadShot, WaspCardContent.MagneticDagger),
                Is.EqualTo(3 + 4),
                "The Tier III face prints +4 where both its ancestors print +3, which is the " +
                "whole of what it upgrades.");
        }

        // -----------------------------------------------------------------------------------------
        // Boards and steps
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Two friendly minions inside her Radius 2 and one card of hers already in the discard pile,
        /// which is the board both retrieve-and-move faces need.
        /// </summary>
        private static StagedHeroBoard TwoFriendlyMinionsInRadius(
            int tier, SabinaUpgradeBranch branch) =>
            Create(
                tier: tier, branch: branch,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = FirstAdjacent,
                    ["blue-ranged-01"] = TwoAway
                },
                sabinaDiscards: new[] { SabinaCardContent.BackToBack });

        /// <summary>
        /// The enemy hero beside one of her minions, holding nothing but the card they commit - so the
        /// discard both Green Tier II/III faces ask for cannot be paid.
        /// </summary>
        private static StagedHeroBoard EmptyHandedEnemyBesideHerMinion(
            int tier, SabinaUpgradeBranch branch) =>
            Create(
                tier: tier, branch: branch,
                minionPlacements: At("blue-melee-01", AdjacentAgainstTerrain),
                enemyHandSize: 1);

        /// <summary>
        /// How many times the named face moved a minion before it stopped asking, taking every
        /// repetition it offered.
        /// </summary>
        private static int RepeatsTaken(int tier, CardDefinitionId cardId)
        {
            // Her Green faces widen from Radius 2 to Radius 3, which reaches the other Blue minion the
            // configured setup deals - and a repetition that moved a different minion would count as
            // this one standing still. One friendly minion in the disk keeps the count about the
            // repetition rather than about who was named.
            var fixture = Create(
                tier: tier,
                minionPlacements: At("blue-melee-01", FirstAdjacent),
                banishedMinions: new[] { "blue-ranged-01", "blue-heavy-01" });
            var subject = fixture.MinionAt(FirstAdjacent);
            fixture.StartPrimary(cardId);

            var moves = 0;
            var at = fixture.SpaceOf(subject);
            for (var budget = 0; budget < 24 && fixture.Choice != null; budget++)
            {
                if (fixture.Choice.SelectInvocations.Count > 0)
                {
                    fixture.SelectFirst();
                }
                else
                {
                    // A repeat asks with nothing to name: taking it is the confirm rather than a
                    // selection, and its decline is what the case beside this one exercises.
                    fixture.Confirm();
                }

                var now = fixture.SpaceOf(subject);
                if (!now.Equals(at))
                {
                    moves++;
                    at = now;
                }
            }

            return moves;
        }

        /// <summary>
        /// The live Attack Total the named face resolves with against the enemy hero, once that hero
        /// has committed the named card. The Defense question is left standing, because the total is
        /// what the answer is measured against.
        /// </summary>
        private static int TotalAgainstWasp(
            int tier, CardDefinitionId cardId, CardDefinitionId enemyCard)
        {
            var fixture = Create(tier: tier);
            fixture.PlayAttackAt(
                cardId, fixture.Enemy, defendSuccessfully: false, enemyCard: enemyCard);
            return fixture.AttackTotal();
        }
    }
}
