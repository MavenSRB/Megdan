using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.5: rulebook p.15's mandatory Leveling Up and Upgrade a Card, and p.16's Gain an Item
    /// and Ultimate, driven through milestone 6.10a's intra-stage continuation contract.
    ///
    /// The cases here are about the rules and the continuation. The end-of-round stage that begins them
    /// and the durability of a paused one across a snapshot, a replay and an Undo are proved through the
    /// public command path in <c>MatchSessionTests</c> and <c>EndOfRoundLevelUpStageTests</c>.
    /// </summary>
    public sealed class HeroLevelUpTests
    {
        // ----- p.15's Hero Level table, as declared content -----

        [Test]
        public void TheLevelTable_PricesEveryStepAsRulebookPageFifteenPrintsIt()
        {
            // "Hero Level 1..8 / Level Up cost - 1 2 3 4 5 6 7", and the caption that says the same
            // thing as a rule: "The first level up costs 1 coin. Each following level up costs 1 more
            // coin than the previous one."
            Assert.That(
                Enumerable.Range(HeroLevel.Minimum, HeroLevel.Maximum - HeroLevel.Minimum)
                    .Select(level => HeroLevelSchedule.LevelUpCost(new HeroLevel(level))),
                Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));

            // "You cannot skip a level. A level 1 hero needs 3 coins to reach level 3 (1 coin for the
            // first level up, and 2 coins for the second)."
            Assert.That(
                HeroLevelSchedule.LevelUpCost(new HeroLevel(1)) +
                HeroLevelSchedule.LevelUpCost(new HeroLevel(2)),
                Is.EqualTo(3));

            Assert.That(HeroLevelSchedule.HasNextLevel(new HeroLevel(7)), Is.True);
            Assert.That(HeroLevelSchedule.HasNextLevel(new HeroLevel(HeroLevel.Maximum)), Is.False,
                "p.15's table ends at Level 8 and p.16 says nothing is gained above it.");
            Assert.That(
                () => HeroLevelSchedule.LevelUpCost(new HeroLevel(HeroLevel.Maximum)),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "A Level with no row above it has no price, rather than a price of zero.");
            Assert.That(
                () => HeroLevelSchedule.NextLevel(new HeroLevel(HeroLevel.Maximum)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => HeroLevelSchedule.LevelUpCost(default), Throws.ArgumentException);

            // The table's third row, printed on the left of the hero dashboard.
            Assert.That(
                Enumerable.Range(HeroLevel.Minimum, HeroLevel.Maximum)
                    .Select(level => HeroLevelSchedule.MaxCardTier(new HeroLevel(level))),
                Is.EqualTo(new[]
                {
                    CardTier.None, CardTier.II, CardTier.II, CardTier.II,
                    CardTier.III, CardTier.III, CardTier.III, CardTier.IV
                }));
        }

        [Test]
        public void TheMaxCardTierRow_IsWhatTheLowestTierChoiceProducesRatherThanASecondConstraint()
        {
            // p.15's row is read here rather than enforced anywhere, and this is why: over the page's
            // own hand - the Gold and Silver Basic pair plus three non-Basic cards - "choose the lowest
            // card Tier or tied for the lowest" produces exactly that row. The footnote states the same
            // consequence from the other side: "You must upgrade all your non-basic cards to Tier II
            // before upgrading them to Tier III."
            // 1+2+3+4+5+6+7 is the whole table, so this hero buys every level there is.
            var fixture = new LevelUpFixture(redCoins: 28);
            fixture.Begin();

            for (var level = 2; level < HeroLevel.Maximum; level++)
            {
                Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(level),
                    "p.15's purchases are consecutive and cannot skip a level.");
                fixture.AnswerRed(0);
                Assert.That(
                    fixture.HighestTierInHand(fixture.RedHeroId),
                    Is.EqualTo(HeroLevelSchedule.MaxCardTier(new HeroLevel(level))),
                    $"The highest card Tier in hand after Level {level}'s upgrade is the table's Max " +
                    "card Tier row.");
            }

            // The last column is Tier IV, and the card that carries it is the Ultimate the seventh
            // purchase unlocked - which p.16 places beside the dashboard rather than in the hand.
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(fixture.HighestTierInHand(fixture.RedHeroId), Is.EqualTo(CardTier.III));
            Assert.That(
                fixture.DefinitionOf(fixture.CardOf("red-ultimate-card")).Progression.Tier,
                Is.EqualTo(HeroLevelSchedule.MaxCardTier(new HeroLevel(HeroLevel.Maximum))));
            Assert.That(fixture.LocationOf("red-ultimate-card"), Is.EqualTo(CardLocation.UltimateArea));
        }

        // ----- p.15's mandatory purchase -----

        [Test]
        public void LevelingIsMandatory_AndBuysAsManyConsecutiveLevelsAsTheHeroCanAfford()
        {
            // p.15: "Leveling up is not optional! At the end of each round, you must purchase a level up
            // if you have enough coins", and "If you have enough coins for multiple level ups, you must
            // purchase as many levels as you can afford."
            var fixture = new LevelUpFixture(redCoins: 4);
            var begun = fixture.Begin();

            Assert.That(begun.Accepted, Is.True);
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(2),
                "The first level up costs 1 of the hero's 4 coins, and nothing asked whether they wanted it.");
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(3));
            Assert.That(fixture.LevelOf(fixture.BlueHeroId).Value, Is.EqualTo(HeroLevel.Minimum),
                "A hero with no coins buys nothing.");

            fixture.AnswerRed(0);
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(3),
                "3 coins left buys the second level, which p.15 prices at 2.");
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(1));

            fixture.AnswerRed(0);
            Assert.That(fixture.Transitions.IsRunning, Is.False,
                "1 coin cannot buy the third level, which costs 3, so the leveling is over.");
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(3));
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(1),
                "An unaffordable level takes nothing.");

            var purchases = fixture.Outcomes
                .Where(outcome => outcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased)
                .ToArray();
            Assert.That(purchases.Select(outcome => outcome.Level.Value), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(purchases.Select(outcome => outcome.Spend.Amount), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(
                purchases.All(outcome => outcome.Spend.SpendKind == CoinSpendKind.LevelUp), Is.True);
        }

        [Test]
        public void NoCoins_LeavesEveryLevelWhereItWasAndFinishesTheTransitionInOneCommand()
        {
            var fixture = new LevelUpFixture(redCoins: 0);
            var begun = fixture.Begin();

            Assert.That(begun.Accepted, Is.True);
            Assert.That(begun.TransitionCompleted, Is.True,
                "Nothing was asked, so the stage's transition is over the moment it began.");
            Assert.That(fixture.Transitions.PendingStep, Is.Null);
            Assert.That(fixture.Outcomes, Is.Empty);
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(HeroLevel.Minimum));
            Assert.That(fixture.Items.Items, Is.Empty);
        }

        // ----- p.15's Upgrade a Card, and p.16's item -----

        [Test]
        public void AnUpgrade_OffersEveryTiedLowestTierCardWithBothOfItsNextTierCandidates()
        {
            var fixture = new LevelUpFixture(redCoins: 1);
            fixture.Begin();

            var pending = fixture.Transitions.PendingStep;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.DefinitionId, Is.EqualTo(HeroLevelUpSystemTransition.DefinitionId));
            Assert.That(pending.StepId, Is.EqualTo(HeroLevelUpSystemTransition.PurchaseAndUpgradeStepId));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red));

            var item = HeroLevelUpItem.Read(pending.ItemId);
            Assert.That(item.HeroId, Is.EqualTo(fixture.RedHeroId));
            Assert.That(item.Level.Value, Is.EqualTo(2), "The item names the Level it belongs to.");
            Assert.That(item.Kind, Is.EqualTo(HeroLevelUpItemKind.Upgrade));

            // p.15: "Choose a non-basic Action card in your hand with the lowest card Tier or tied for
            // the lowest" - the Gold and Silver Basic pair is "not considered" - and then "Find two
            // Action cards in your deck that have the same color as the chosen card but are one Tier
            // higher". Three tied Tier I cards with two candidates each is the whole option space.
            Assert.That(pending.Options.Select(option => option.Value), Is.EqualTo(new[]
            {
                "red-bombard-card->red-bombard-t2a-card",
                "red-bombard-card->red-bombard-t2b-card",
                "red-bulwark-card->red-bulwark-t2a-card",
                "red-bulwark-card->red-bulwark-t2b-card",
                "red-pursuit-card->red-pursuit-t2a-card",
                "red-pursuit-card->red-pursuit-t2b-card"
            }));
            Assert.That(
                pending.Options.Any(option => option.Value.Contains("needle") ||
                    option.Value.Contains("lancer")),
                Is.False,
                "p.15's Basic pair is never upgraded and never considered.");
        }

        [Test]
        public void AnAnsweredUpgrade_ReturnsTheChosenCardTakesOneCandidateAndSetsTheOtherAside()
        {
            var fixture = new LevelUpFixture(redCoins: 1);
            fixture.Begin();

            // Take the Tier II-b card of Red Bulwark, so the II-a card is the one set aside.
            fixture.AnswerRed("red-bulwark-card->red-bulwark-t2b-card");

            Assert.That(fixture.LocationOf("red-bulwark-card"), Is.EqualTo(CardLocation.Deck),
                "p.15: \"Return this card to your deck.\"");
            Assert.That(fixture.LocationOf("red-bulwark-t2b-card"), Is.EqualTo(CardLocation.Hand),
                "p.15: \"Take one of those cards into your hand.\"");
            Assert.That(fixture.CardOf("red-bulwark-t2b-card").State.FaceOrientation,
                Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(fixture.LocationOf("red-bulwark-t2a-card"), Is.EqualTo(CardLocation.ItemSlot),
                "p.15 sets the other one aside and p.16's step then tucks it under the dashboard.");
            Assert.That(fixture.LocationOf("red-bulwark-t3a-card"), Is.EqualTo(CardLocation.Deck),
                "The Tier III cards stay in the deck; only the next Tier is found.");

            var upgrade = fixture.Outcomes.Single(
                outcome => outcome.Kind == HeroLevelUpOutcomeKind.CardUpgraded);
            Assert.That(upgrade.UpgradedCardInstanceId.Value, Is.EqualTo("red-bulwark-card"));
            Assert.That(upgrade.TakenCardInstanceId.Value, Is.EqualTo("red-bulwark-t2b-card"));
            Assert.That(upgrade.SetAsideCardInstanceId.Value, Is.EqualTo("red-bulwark-t2a-card"));

            // p.15: "You should now have exactly 5 cards, 1 of each color, in your hand."
            Assert.That(fixture.HandOf(fixture.RedHeroId), Has.Count.EqualTo(5));
            Assert.That(
                fixture.HandOf(fixture.RedHeroId)
                    .Select(card => fixture.DefinitionOf(card).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5));
        }

        [Test]
        public void TheItemStep_TucksEverySetAsideCardUnderTheSlotItsOwnIconMatches()
        {
            // Red buys two levels, so two cards are set aside and two items are gained; Blue buys none.
            var fixture = new LevelUpFixture(redCoins: 3);
            fixture.Begin();
            fixture.AnswerRed("red-bulwark-card->red-bulwark-t2a-card");
            fixture.AnswerRed("red-pursuit-card->red-pursuit-t2b-card");

            Assert.That(fixture.Transitions.IsRunning, Is.False);
            var items = fixture.Items.GetItems(fixture.RedHeroId);
            Assert.That(items.Select(item => item.SourceCardInstanceId.Value),
                Is.EqualTo(new[] { "red-bulwark-t2b-card", "red-pursuit-t2a-card" }),
                "The unchosen candidate of each purchased level becomes that hero's item.");
            Assert.That(items.Select(item => item.Order), Is.EqualTo(new[] { 1, 2 }));

            // p.16: "It matches the icon on one of four slots on the top of your hero dashboard."
            Assert.That(items[0].Icon, Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(items[1].Icon, Is.EqualTo(PrintedValueChannel.Attack));
            foreach (var item in items)
            {
                Assert.That(
                    HeroDashboard.RulebookLayout.GetSlot(item.Slot).Accepts(item.Icon), Is.True,
                    "p.16 penalizes an item that is not under the appropriate slot, so nothing places one there.");
                Assert.That(fixture.LocationOf(item.SourceCardInstanceId.Value),
                    Is.EqualTo(CardLocation.ItemSlot));
                Assert.That(fixture.CardOf(item.SourceCardInstanceId.Value).State.FaceOrientation,
                    Is.EqualTo(CardFaceOrientation.FaceUp),
                    "p.16: \"tuck it Faceup ... Your items must be clearly visible to the opposing team.\"");
            }

            Assert.That(fixture.Items.GetItems(fixture.BlueHeroId), Is.Empty);
            Assert.That(
                fixture.Outcomes.Count(outcome => outcome.Kind == HeroLevelUpOutcomeKind.ItemGained),
                Is.EqualTo(2));
        }


        [Test]
        public void TheSetAsideLocation_HasExactlyOneWriterAndItIsPageFifteensUpgradeStep()
        {
            // p.16's Gain an Item takes every card in the set-aside location, which is correct only
            // while that location has one writer. Milestone 8.14 assessed scoping the sweep instead
            // (F-PROG-08) and withdrew it, because the scope a completed item can carry is the hero
            // and not the card: a hero who both levelled and had a card set aside by some other rule
            // would defeat that scope anyway, so it would buy confidence rather than correctness.
            // This rule fails the day a second writer is added, which is the moment the decision has
            // to be made again - and then against the rule that actually needs it.
            var writer = ILConformanceScanner.Method(
                typeof(CardLifecycleAuthority),
                nameof(CardLifecycleAuthority.TrySetAsideFromDeck),
                typeof(CardInstance));

            var production = new[]
                {
                    typeof(CardInstance).Assembly,
                    typeof(HeroLevelUpStepRules).Assembly,
                    typeof(GuardsDigital.Application.MatchSession).Assembly
                }
                .Distinct()
                .SelectMany(ILConformanceScanner.TypesIn)
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith("GuardsDigital.", StringComparison.Ordinal) &&
                    !type.Namespace.StartsWith("GuardsDigital.Tests", StringComparison.Ordinal))
                .ToArray();

            var callers = ILConformanceScanner.Scan(production)
                .Where(scanned => scanned.Calls(writer))
                .Select(scanned => scanned.Method.DeclaringType?.FullName + "." + scanned.Method.Name)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(callers, Is.EqualTo(new[]
                {
                    typeof(HeroLevelUpStepRules).FullName + "." + nameof(HeroLevelUpStepRules.ApplyAnswer)
                }),
                "p.15's \"set the other one aside\" is the only rule that may put a card there. A new " +
                "one means p.16's sweep can no longer read the location alone, and needs the card's " +
                "own provenance rather than its owner's.");
        }

        // ----- p.16's Ultimate -----

        [Test]
        public void AfterEveryNonBasicCardReachesTierIII_TheNextLevelUnlocksTheUltimateAndEndsLeveling()
        {
            // 1+2+3+4+5+6 = 21 coins buys the six upgrades that take three Tier I cards to Tier III, and
            // 7 more buys the level p.16 spends on the Ultimate instead. One extra coin proves the page's
            // closing sentence: "Once you unlock your Ultimate, you cannot gain any additional levels."
            var fixture = new LevelUpFixture(redCoins: 29);
            fixture.Begin();
            for (var upgrade = 0; upgrade < 6; upgrade++)
            {
                Assert.That(fixture.Transitions.PendingStep, Is.Not.Null,
                    "Every ordinary level stops on p.15's Upgrade a Card step.");
                fixture.AnswerRed(0);
            }

            Assert.That(fixture.Transitions.IsRunning, Is.False,
                "The seventh purchase unlocks the Ultimate, which asks nobody anything.");
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(1),
                "p.16: an unlocked Ultimate ends leveling, but the hero \"can still gain coins\".");
            Assert.That(fixture.LocationOf("red-ultimate-card"), Is.EqualTo(CardLocation.UltimateArea),
                "p.16: \"Place it Faceup to the left side of your hero dashboard.\"");

            var unlock = fixture.Outcomes.Single(
                outcome => outcome.Kind == HeroLevelUpOutcomeKind.UltimateUnlocked);
            Assert.That(unlock.Level.Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(unlock.UltimateCardInstanceId.Value, Is.EqualTo("red-ultimate-card"));
            Assert.That(
                fixture.Outcomes.Count(outcome => outcome.Kind == HeroLevelUpOutcomeKind.CardUpgraded),
                Is.EqualTo(6),
                "p.16 unlocks the Ultimate \"instead of upgrading a card and gaining an item\".");
            Assert.That(fixture.Items.GetItems(fixture.RedHeroId), Has.Count.EqualTo(6));

            // A second end of round with coins still in hand changes nothing.
            fixture.AwardCoins(fixture.RedHeroId, 20);
            var again = fixture.Begin("round-2");
            Assert.That(again.Accepted, Is.True);
            Assert.That(again.TransitionCompleted, Is.True);
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(21));
            Assert.That(
                fixture.Outcomes.Any(outcome =>
                    outcome.Kind == HeroLevelUpOutcomeKind.UltimateUnlocked),
                Is.False,
                "One Ultimate is unlocked once.");
        }

        // ----- the continuation itself -----

        [Test]
        public void AnAnswer_NamingAnotherHeroLevelStepOrOptionLeavesTheContinuationExactlyAsItWas()
        {
            var fixture = new LevelUpFixture(redCoins: 1);
            fixture.Begin();
            var pending = fixture.Transitions.PendingStep;
            var legal = pending.Invocations[0];

            void AssertRefused(SystemTransitionChoiceCommand command, SystemTransitionRejectionReason reason)
            {
                var result = fixture.Transitions.TryAnswer(command);
                Assert.That(result.Accepted, Is.False);
                Assert.That(result.RejectionReason, Is.EqualTo(reason));
                Assert.That(fixture.Transitions.PendingStep, Is.SameAs(pending));
                Assert.That(fixture.LocationOf("red-bombard-card"), Is.EqualTo(CardLocation.Hand));
                Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(0));
            }

            AssertRefused(
                new SystemTransitionChoiceCommand(
                    new SystemTransitionInstanceId("some-other-run"),
                    legal.StepId, legal.ItemId, legal.ChoosingSide, legal.OptionId),
                SystemTransitionRejectionReason.WrongTransitionInstance);
            AssertRefused(
                new SystemTransitionChoiceCommand(
                    legal.InstanceId, HeroLevelUpSystemTransition.GainItemsStepId, legal.ItemId,
                    legal.ChoosingSide, legal.OptionId),
                SystemTransitionRejectionReason.StaleStep);
            AssertRefused(
                new SystemTransitionChoiceCommand(
                    legal.InstanceId,
                    legal.StepId,
                    new HeroLevelUpItem(
                        fixture.RedHeroId, new HeroLevel(3), HeroLevelUpItemKind.Upgrade).ToItemId(),
                    legal.ChoosingSide,
                    legal.OptionId),
                SystemTransitionRejectionReason.StaleItem);
            AssertRefused(
                new SystemTransitionChoiceCommand(
                    legal.InstanceId,
                    legal.StepId,
                    new HeroLevelUpItem(
                        fixture.BlueHeroId, new HeroLevel(2), HeroLevelUpItemKind.Upgrade).ToItemId(),
                    legal.ChoosingSide,
                    legal.OptionId),
                SystemTransitionRejectionReason.StaleItem);
            AssertRefused(
                new SystemTransitionChoiceCommand(
                    legal.InstanceId, legal.StepId, legal.ItemId, TeamSide.Blue, legal.OptionId),
                SystemTransitionRejectionReason.WrongChooser);
            AssertRefused(
                new SystemTransitionChoiceCommand(
                    legal.InstanceId, legal.StepId, legal.ItemId, legal.ChoosingSide,
                    new HeroLevelUpOption(
                        new CardInstanceId("red-bombard-card"),
                        new CardInstanceId("red-bulwark-t2a-card")).ToOptionId()),
                SystemTransitionRejectionReason.IllegalOption);

            // The legal answer still works afterwards, which is what "exactly as it was" has to mean.
            Assert.That(fixture.Transitions.TryAnswer(legal).Accepted, Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 7.8a: the pause describes itself, so a reader never decodes an option identity
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APausedUpgrade_DescribesItsHeroLevelPriceCoinsAndEveryOptionsThreeCards()
        {
            var fixture = new LevelUpFixture(redCoins: 2);
            fixture.Begin();

            var pending = fixture.Transitions.PendingStep;
            var description = fixture.Transitions.DescribePendingStep();
            Assert.That(description, Is.Not.Null);
            Assert.That(description.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            Assert.That(description.PendingStep, Is.SameAs(pending),
                "A description belongs to the pause it describes, so a reader cannot hold one beside " +
                "another.");
            Assert.That(description.HeroId, Is.EqualTo(fixture.RedHeroId));
            Assert.That(description.Level, Is.EqualTo(2));
            Assert.That(description.PaidCoins, Is.EqualTo(1),
                "p.15's table prices the first level at 1, read one row back from the level reached.");
            Assert.That(description.RemainingCoins, Is.EqualTo(fixture.Coins(fixture.RedHeroId)));

            Assert.That(
                description.Options.Select(option => option.OptionId.Value),
                Is.EqualTo(pending.Options.Select(option => option.Value)),
                "Exactly the options the step offered, in the step's own order.");
            foreach (var option in description.Options)
            {
                var read = HeroLevelUpOption.Read(option.OptionId);
                Assert.That(option.UpgradedCardInstanceId, Is.EqualTo(read.UpgradedCardInstanceId));
                Assert.That(option.TakenCardInstanceId, Is.EqualTo(read.TakenCardInstanceId));
                Assert.That(option.UpgradedCard,
                    Is.EqualTo(fixture.CardOf(read.UpgradedCardInstanceId.Value).DefinitionReference));
                Assert.That(option.TakenCard,
                    Is.EqualTo(fixture.CardOf(read.TakenCardInstanceId.Value).DefinitionReference));
                Assert.That(option.SetAsideCardInstanceId.IsDefined, Is.True,
                    "This content always leaves a second candidate in the deck for p.15 to set aside.");
                Assert.That(option.SetAsideCardInstanceId,
                    Is.Not.EqualTo(option.TakenCardInstanceId));
                Assert.That(option.ItemIcon, Is.Not.EqualTo(PrintedValueChannel.None),
                    "and p.16 files it under the slot its own printed icon matches.");
                Assert.That(option.ItemSlot.IsDefined, Is.True);
            }
        }

        [Test]
        public void AnUnpausedLeveling_DescribesNothing()
        {
            var fixture = new LevelUpFixture(redCoins: 0);
            fixture.Begin();

            Assert.That(fixture.Transitions.PendingStep, Is.Null,
                "Nobody could afford a level, so the step completed without asking.");
            Assert.That(fixture.Transitions.DescribePendingStep(), Is.Null,
                "There is no pause to describe, which is an answer rather than a defect.");
        }

        [Test]
        public void APausedLeveling_ReproducesItselfFromTheCardsAndRefusesAPayloadItCouldNotHaveProduced()
        {
            var fixture = new LevelUpFixture(redCoins: 3);
            fixture.Begin();
            fixture.AnswerRed(0);

            var captured = fixture.Transitions.CaptureRestoreState();
            Assert.That(captured.PendingStep, Is.Not.Null);
            fixture.Rules.DrainOutcomes();
            var restored = SystemTransitionAuthority.Restore(
                captured, MatchSystemTransitions.CreateCatalog(), fixture.Rules);

            Assert.That(restored.PendingStep.ItemId, Is.EqualTo(captured.PendingStep.ItemId));
            Assert.That(
                restored.PendingStep.Options.Select(option => option.Value),
                Is.EqualTo(captured.PendingStep.Options.Select(option => option.Value)));
            Assert.That(
                restored.CompletedItemIds.Select(item => item.Value),
                Is.EqualTo(captured.PendingStep.CompletedItemIds.Select(item => item.Value)),
                "The exact resume location is the items already answered, stated positively.");
            Assert.That(fixture.Rules.DrainOutcomes(), Is.Empty,
                "Re-deriving a paused step buys nothing, upgrades nothing and spends no coin.");
            Assert.That(fixture.Coins(fixture.RedHeroId), Is.EqualTo(0));
            Assert.That(fixture.LevelOf(fixture.RedHeroId).Value, Is.EqualTo(3));
            Assert.That(fixture.LocationOf("red-bombard-card"), Is.EqualTo(CardLocation.Deck),
                "The upgrade this fixture already answered stays answered.");

            var pending = captured.PendingStep;
            var tampered = new PendingSystemTransitionStep(
                pending.InstanceId,
                pending.DefinitionId,
                pending.StepId,
                pending.ItemId,
                pending.ChoosingSide,
                new[] { new SystemTransitionOptionId("red-lancer-card->red-bulwark-t2a-card") },
                pending.CompletedStepIds,
                pending.CompletedItemIds);
            Assert.That(
                () => SystemTransitionAuthority.Restore(
                    new SystemTransitionRestoreState(tampered),
                    MatchSystemTransitions.CreateCatalog(),
                    fixture.Rules),
                Throws.ArgumentException,
                "A derived step has no catalog options to check against, so it is re-derived over the " +
                "restored cards - and a payload they do not reproduce is refused rather than repaired.");
        }

        [Test]
        public void AnItemAndAnOption_RoundTripThroughTheirWireIdentities()
        {
            var item = new HeroLevelUpItem(
                new HeroId("primitive-hero"), new HeroLevel(4), HeroLevelUpItemKind.Purchase);
            Assert.That(item.ToItemId().Value, Is.EqualTo("primitive-hero:4:purchase"));
            Assert.That(HeroLevelUpItem.Read(item.ToItemId()), Is.EqualTo(item));

            foreach (var kind in new[]
            {
                HeroLevelUpItemKind.Purchase, HeroLevelUpItemKind.Upgrade, HeroLevelUpItemKind.Ultimate
            })
            {
                var round = new HeroLevelUpItem(new HeroId("a:b"), new HeroLevel(8), kind);
                Assert.That(HeroLevelUpItem.Read(round.ToItemId()), Is.EqualTo(round),
                    "The hero id is everything before the last two fields, so it is not constrained here.");
            }

            var option = new HeroLevelUpOption(
                new CardInstanceId("red-bulwark-card"), new CardInstanceId("red-bulwark-t2a-card"));
            Assert.That(HeroLevelUpOption.Read(option.ToOptionId()), Is.EqualTo(option));

            Assert.That(
                () => HeroLevelUpItem.Read(new SystemTransitionItemId("not-an-item")),
                Throws.InstanceOf<InvalidOperationException>());
            Assert.That(
                () => HeroLevelUpItem.Read(new SystemTransitionItemId("hero:9:purchase")),
                Throws.InstanceOf<InvalidOperationException>(),
                "p.15's table has no Level 9.");
            Assert.That(
                () => HeroLevelUpOption.Read(new SystemTransitionOptionId("no-separator")),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void ABuildWithoutLevelUpRules_RefusesTheDerivedStepRatherThanGuessingAtIt()
        {
            var transitions = new SystemTransitionAuthority(
                MatchSystemTransitions.CreateCatalog(),
                MatchSystemTransitions.CreateRules(null, null));
            Assert.That(
                () => transitions.TryBegin(
                    HeroLevelUpSystemTransition.DefinitionId,
                    new SystemTransitionInstanceId("unruled")),
                Throws.InstanceOf<InvalidOperationException>(),
                "A registered definition whose derived step has no rules is an engine defect, not a rule.");
        }

        // ----- fixture -----

        /// <summary>
        /// The configured 1v1 content with a coin ledger this fixture fills, and the leveling rules
        /// composed over both.
        ///
        /// Coins arrive through the one source this build has - p.13's Defeat reward - so nothing here
        /// invents a way to gain them. A 1v1 match has no friendly hero to assist, so an award reaches
        /// exactly the hero it names.
        /// </summary>
        private sealed class LevelUpFixture
        {
            private readonly MatchConfiguration configuration;
            private readonly List<HeroLevelUpOutcome> outcomes = new List<HeroLevelUpOutcome>();
            private int awardCount;

            public LevelUpFixture(int redCoins)
            {
                configuration = MatchConfiguration.CreateForShowcaseFixture();
                RedHeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                BlueHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Levels = new HeroProgressionState(configuration.Participants);
                Items = new HeroItemState(configuration.Participants);
                CoinLedger = new HeroCoinLedger(configuration.Participants);
                Rules = new HeroLevelUpStepRules(
                    configuration.Participants,
                    configuration.CardDefinitions,
                    configuration.CardInstances);
                Rules.Bind(Levels, Items, CoinLedger);
                Transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(null, Rules));
                if (redCoins > 0)
                {
                    AwardCoins(RedHeroId, redCoins);
                }
            }

            public HeroId RedHeroId { get; }
            public HeroId BlueHeroId { get; }
            public HeroProgressionState Levels { get; }
            public HeroItemState Items { get; }
            public HeroCoinLedger CoinLedger { get; }
            public HeroLevelUpStepRules Rules { get; }
            public SystemTransitionAuthority Transitions { get; }

            public IReadOnlyList<HeroLevelUpOutcome> Outcomes => outcomes;

            public void AwardCoins(HeroId heroId, int amount)
            {
                var enemyTeam = configuration.Participants.Teams
                    .Single(team => !team.Id.Equals(
                        configuration.Participants.GetTeamForHero(heroId).Id));
                var awarded = CoinLedger.Award(new DefeatOutcome(
                    new DefeatOutcomeId($"fixture-award-{++awardCount}"),
                    new DefeatTargetId($"fixture-target-{awardCount}"),
                    enemyTeam.Id,
                    heroId,
                    amount,
                    0));
                Assert.That(awarded.Accepted, Is.True, awarded.RejectionReason.ToString());
            }

            public SystemTransitionResult Begin(string occasion = "round-1")
            {
                outcomes.Clear();
                var result = Transitions.TryBegin(
                    HeroLevelUpSystemTransition.DefinitionId,
                    new SystemTransitionInstanceId(
                        $"{occasion}-{HeroLevelUpSystemTransition.DefinitionId.Value}"));
                outcomes.AddRange(Rules.DrainOutcomes());
                return result;
            }

            public void AnswerRed(int optionIndex) =>
                Answer(Transitions.PendingStep.Invocations[optionIndex]);

            public void AnswerRed(string optionId) =>
                Answer(Transitions.PendingStep.Invocations.Single(
                    invocation => invocation.OptionId.Value == optionId));

            private void Answer(SystemTransitionChoiceCommand command)
            {
                var result = Transitions.TryAnswer(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                outcomes.AddRange(Rules.DrainOutcomes());
            }

            public HeroLevel LevelOf(HeroId heroId) => Levels.GetLevel(heroId);

            public int Coins(HeroId heroId) => CoinLedger.GetCoins(heroId);

            public CardInstance CardOf(string instanceId) => configuration.CardInstances
                .Single(card => card.Id.Value == instanceId);

            public CardLocation LocationOf(string instanceId) => CardOf(instanceId).State.Location;

            public CardDefinition DefinitionOf(CardInstance card) => configuration.CardDefinitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference));

            public IReadOnlyList<CardInstance> HandOf(HeroId heroId) => Array.AsReadOnly(
                configuration.CardInstances
                    .Where(card => card.OwnerId.Equals(heroId) &&
                        card.State.Location == CardLocation.Hand)
                    .ToArray());

            /// <summary>The highest printed Tier in this hero's hand, ignoring p.15's untiered Basic pair.</summary>
            public CardTier HighestTierInHand(HeroId heroId) => HandOf(heroId)
                .Select(card => DefinitionOf(card).Progression.Tier)
                .Where(tier => tier != CardTier.None)
                .Max();
        }
    }
}
