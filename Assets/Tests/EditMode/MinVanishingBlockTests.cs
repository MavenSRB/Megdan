using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Min the Dragonmonk's vanishing Block. Milestone 8.61b.
    ///
    /// <para>Three faces print one sentence: "Swap with a [Smoke bomb] Smoke bomb token in range; if you
    /// do, block the attack" (Poof! at Range 3, Vanish at 4), and Ruse adds "You may place the Smoke
    /// bomb into a space in range". None of the three prints a Defense value, so rulebook p.14's other
    /// half is in full force - "If the Block condition is fulfilled, you successfully defend, regardless
    /// of the attack value. If not, you are defeated."</para>
    ///
    /// <para><b>What this fixture is really measuring.</b> The swap is mandatory, so "if you do" is
    /// fulfilled exactly when an owned Smoke bomb stands inside the card's printed Range of her - and
    /// the condition is therefore that swap's own precondition rather than a second fact that happens to
    /// agree with it. The cases below prove that equivalence on boards rather than asserting it: the
    /// condition's verdict and the swap sub-graph's own feasibility are compared on every space of a
    /// disc, and both are traced to one reading.</para>
    /// </summary>
    public sealed class MinVanishingBlockTests
    {
        private static readonly MapId MapIdentity = new MapId("min-vanishing-map");
        private static readonly SpawnPointId Spawn = new SpawnPointId("min-vanishing-spawn");

        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Attacker = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        /// <summary>Min's space in every case here; the disc is measured from it.</summary>
        private static readonly HexCoordinate MinsSpace = new HexCoordinate(0, 0);

        private static readonly CardDefinitionId NonRangedAttackCard =
            new CardDefinitionId("min-vanishing-melee-attacker");

        private static readonly CardDefinitionId RangedAttackCard =
            new CardDefinitionId("min-vanishing-ranged-attacker");

        private static readonly ActionDefinitionId AttackActionId =
            new ActionDefinitionId("min-vanishing-attack");

        // -------------------------------------------------------------------------------------
        // (a) The three printed faces
        // -------------------------------------------------------------------------------------

        [TestCase(2, 3, false)]
        [TestCase(2, 4, false)]
        [TestCase(1, 4, true)]
        public void EachFacePrintsItsInitiativeItsRangeAndNoDefenseValueAtAll(
            int initiative, int range, bool isRuse)
        {
            var card = MinCardContent.VanishingDefinition(
                isRuse ? MinCardContent.Ruse : range == 3 ? MinCardContent.Poof : MinCardContent.Vanish);

            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));

            var block = card.ActionDefinitions.Single(
                action => action.DefenseResolution == PrintedDefenseResolution.Block);
            Assert.That(block.Id, Is.EqualTo(MinCardContent.PrimaryBlock));
            Assert.That(block.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary),
                "p.14 prints a Block as the card's own primary action, never as a secondary reaction.");
            Assert.That(
                block.BaseValues.Single(),
                Is.EqualTo(new PrintedValueDefinition(PrintedValueChannel.Range, range)),
                "The one printed value on a Block card is p.9's Range icon - the reach the sentence " +
                "means by \"in range\".");
            Assert.That(
                card.ActionDefinitions.SelectMany(action => action.BaseValues)
                    .Any(value => value.Channel == PrintedValueChannel.Defense),
                Is.False,
                "MI-05: these three print NO Defense value at all. A missing icon is absent, never " +
                "zero, so there is no Defense ribbon action here to carry one either.");
            Assert.That(
                card.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement).BaseValues
                    .Single(),
                Is.EqualTo(new PrintedValueDefinition(PrintedValueChannel.Movement, 3)),
                "and all three print a Movement ribbon of 3.");
        }

        [Test]
        public void EachFaceDeclaresTheSwapPreconditionAsItsBlockConditionAndRunsItAtPage14Step4()
        {
            foreach (var cardId in MinCardContent.VanishingCards())
            {
                var block = BlockAction(cardId);

                Assert.That(
                    block.BlockCondition,
                    Is.EqualTo(PrintedBlockCondition
                        .TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange),
                    $"{cardId.Value} resolves by the precondition of the swap its own text performs.");
                Assert.That(block.CardTextTiming, Is.EqualTo(CardTextTiming.DefenseAction),
                    "p.14 numbers the defender's own text step 4 - \"Defender applies Defense action " +
                    "text, if any\" - which is the only moment a Defense action has.");
                Assert.That(block.CardTextAt(CardTextTiming.DefenseAction), Is.Not.Null);
                Assert.That(block.DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Applies),
                    "p.14's own worked example: a Defense printed as the card's PRIMARY applies its " +
                    "card text.");
                Assert.That(
                    block.SourceCardStateTransition,
                    Is.EqualTo(new ActionCardStateTransition(
                        CardLocation.Hand, CardLocation.DiscardPile)),
                    "and it is spent from hand exactly as every other Defense action is.");
            }
        }

        [Test]
        public void OnlyRuseComposesTheOptionalRePlaceThatItAlonePrints()
        {
            Assert.That(
                MinCardContent.VanishingDefinition(MinCardContent.Ruse).SourceContent.PrintedText,
                Does.Contain("You may place the Smoke bomb into a space in range"));

            foreach (var cardId in MinCardContent.VanishingCards())
            {
                var text = BlockAction(cardId).CardTextAt(CardTextTiming.DefenseAction);
                var optional = text.Nodes
                    .Where(node => node.FailurePolicy == EffectFailurePolicy.May)
                    .ToArray();

                Assert.That(optional.Length, Is.EqualTo(cardId.Equals(MinCardContent.Ruse) ? 1 : 0),
                    $"{cardId.Value}: \"you may\" is printed on Ruse's second sentence and on neither " +
                    "of the other two, whose whole body is a mandatory swap.");
                Assert.That(EffectDefinitionValidator.Validate(text, Capabilities), Is.Empty,
                    $"{cardId.Value}'s composed Defense text is a valid graph against the catalog its " +
                    "capabilities are declared in.");
            }
        }

        [Test]
        public void TheThreeFacesCarryTheirTiersTheirItemsAndTheirTranscribedText()
        {
            var poof = MinCardContent.VanishingDefinition(MinCardContent.Poof);
            var vanish = MinCardContent.VanishingDefinition(MinCardContent.Vanish);
            var ruse = MinCardContent.VanishingDefinition(MinCardContent.Ruse);

            Assert.That(poof.Progression.Tier, Is.EqualTo(CardTier.I));
            Assert.That(poof.Progression.Color, Is.EqualTo(CardColor.Green));
            Assert.That(poof.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None),
                "Poof! prints no Item icon at all, which the shard's table records as a dash.");
            Assert.That(vanish.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Defense));
            Assert.That(ruse.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(ruse.Progression.Tier, Is.EqualTo(CardTier.III));

            Assert.That(poof.SourceContent.PrintedText, Is.EqualTo(MinCardContent.PoofText));
            Assert.That(vanish.SourceContent.PrintedText, Is.EqualTo(MinCardContent.PoofText),
                "Vanish prints Poof!'s sentence word for word; only the printed Range differs.");
            Assert.That(ruse.SourceContent.PrintedText, Is.EqualTo(MinCardContent.RuseText));
        }

        // -------------------------------------------------------------------------------------
        // (b) The condition, asked of the one query the preview and the resolution both call
        // -------------------------------------------------------------------------------------

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ABombInsidePoofsPrintedRange_FulfilsTheConditionAtEveryDistance(int distance)
        {
            var board = Board();
            Place(board, Step(HexDirection.East, distance));

            Assert.That(Verdict(MinCardContent.Poof, board), Is.EqualTo(DefenseBlockResult.Blocked),
                "\"Swap with a Smoke bomb token in range; if you do, block the attack.\"");
        }

        [Test]
        public void ABombOutsideThePrintedRange_LeavesTheConditionUnfulfilled()
        {
            var board = Board();
            Place(board, Step(HexDirection.East, 4));

            Assert.That(
                Verdict(MinCardContent.Poof, board),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "Poof! prints Range 3, so a bomb at 4 is a bomb she cannot swap with - and p.14 then " +
                "defeats her, because the card prints no Defense value to compare instead.");
            Assert.That(Verdict(MinCardContent.Vanish, board), Is.EqualTo(DefenseBlockResult.Blocked),
                "The same board and the same piece: Vanish prints Range 4 and reaches it.");
        }

        [Test]
        public void NoBombOnTheBoardAtAll_LeavesTheConditionUnfulfilled()
        {
            foreach (var cardId in MinCardContent.VanishingCards())
            {
                Assert.That(
                    Verdict(cardId, Board()),
                    Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                    $"{cardId.Value}: a bomb never placed, or reclaimed by an earlier Place, is a " +
                    "board on which the mandatory swap is p.10's impossible step.");
            }
        }

        [Test]
        public void ARangeItemRaisesTheReachTheConditionMeasures()
        {
            var board = Board();
            Place(board, Step(HexDirection.NorthWest, 4));

            Assert.That(
                Verdict(MinCardContent.Poof, board),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "Poof!'s printed 3 does not reach a bomb at 4...");
            Assert.That(
                Verdict(MinCardContent.Poof, board, RangeItem()),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "...and p.16's Range item raises the printed icon, so the same board now blocks. " +
                "Nothing in the condition knows an item exists: it asks p.9's Range Total.");
        }

        /// <summary>
        /// The over-generalisation this condition refuses. She places blast mines, dud mines and a
        /// Grenade as well as the bomb, and all of them are hers and stand on the board while she
        /// defends - so "a token of yours in range" would block an attack her printed sentence does not.
        /// The piece is named by the very node her swap consumes.
        /// </summary>
        [Test]
        public void AnotherTokenOfHersInRange_IsNotThePieceTheSentenceNames()
        {
            var board = Board();
            Place(board, Step(HexDirection.East, 1), definition: OtherTokenOfHers);

            Assert.That(
                Verdict(MinCardContent.Poof, board),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "\"a [Smoke bomb] Smoke bomb token\" is one named piece, and her mines and Grenade " +
                "are not it.");

            Place(board, Step(HexDirection.West, 1));
            Assert.That(Verdict(MinCardContent.Poof, board), Is.EqualTo(DefenseBlockResult.Blocked),
                "and the bomb standing beside them is.");
        }

        [Test]
        public void AnEnemysCopyOfTheSamePiece_IsNotHers()
        {
            var board = Board();
            Place(board, Step(HexDirection.East, 1), controller: Attacker);

            Assert.That(
                Verdict(MinCardContent.Poof, board),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "\"Swap with a Smoke bomb\" is her own piece: the fact is owner-scoped, which is what " +
                "milestone 8.35z32's contract has always meant by an OWNED token.");
        }

        [Test]
        public void TheConditionAsksNothingAboutTheAttackSoBothSubtypesAreBlockedAlike()
        {
            var board = Board();
            Place(board, Step(HexDirection.SouthEast, 2));

            Assert.That(
                Verdict(MinCardContent.Poof, board, attackCard: NonRangedAttackCard),
                Is.EqualTo(DefenseBlockResult.Blocked));
            Assert.That(
                Verdict(MinCardContent.Poof, board, attackCard: RangedAttackCard),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "Her three faces print no ranged or non-ranged clause, so p.13's classification is not " +
                "a question this condition asks - exactly as Bain's marker clause does not ask it.");

            var empty = Board();
            Assert.That(
                Verdict(MinCardContent.Poof, empty, attackCard: NonRangedAttackCard),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled));
            Assert.That(
                Verdict(MinCardContent.Poof, empty, attackCard: RangedAttackCard),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "and it fails against both alike too.");
        }

        [Test]
        public void ABlockIsAnsweredByItsConditionRatherThanByANumber()
        {
            var definitions = Definitions();

            foreach (var cardId in MinCardContent.VanishingCards())
            {
                Assert.That(DefenseBlockQuery.IsBlock(Offer(cardId), definitions), Is.True,
                    $"{cardId.Value} is settled by the condition, so no Defense value is calculated " +
                    "for it and no printed Defense icon is looked for.");
            }
        }

        // -------------------------------------------------------------------------------------
        // (c) One reading, not two that agree
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// THE CLAIM THIS BLOCK RESTS ON, measured rather than asserted. For every space of a radius-5
        /// disc, the verdict the defence preview and the resolution are given is compared against
        /// whether the card's own mandatory swap could actually be performed from that board. They agree
        /// on all of them, for all three faces, with and without a Range item - which is what makes
        /// "if you do" a statement about one fact rather than about two registers.
        /// </summary>
        [Test]
        public void TheConditionAndTheSwapItGates_AgreeOnEverySpaceOfTheDisc()
        {
            foreach (var cardId in MinCardContent.VanishingCards())
            {
                foreach (var withItem in new[] { false, true })
                {
                    var items = withItem ? RangeItem() : null;
                    foreach (var space in Disc(5))
                    {
                        var board = Board();
                        if (!board.IsEmptyPlayableSpace(space))
                        {
                            // p.18: a token is placed into an empty space, so the two heroes' own
                            // spaces are not boards this comparison can be made on.
                            continue;
                        }

                        Place(board, space);

                        var blocked = Verdict(cardId, board, items) == DefenseBlockResult.Blocked;
                        var swapRuns = TheSwapCouldBePerformed(cardId, board, items);

                        Assert.That(blocked, Is.EqualTo(swapRuns),
                            $"{cardId.Value} with the bomb at {space} and " +
                            $"{(withItem ? "a Range item" : "no item")}: the Block condition and the " +
                            "swap it is the precondition of reached different answers, which is the " +
                            "one thing p.14's \"if you do\" cannot mean.");
                    }
                }
            }
        }

        /// <summary>
        /// WHY MILESTONE 8.11's TIMING NEEDED NO CHANGE. The verdict is decided where every Block's has
        /// been since 8.11 - at p.14's step 3, when the card is revealed - and carried to step 6, while
        /// the swap it gates runs between them at step 4. That could have been a disagreement, and it
        /// cannot be: p.19's Swap EXCHANGES two spaces, so the distance between her and the piece is the
        /// same number afterwards, and Ruse's re-place puts the piece back inside the same printed
        /// Range measured from where she now stands. The condition is invariant under the whole of its
        /// own Defense text.
        /// </summary>
        [Test]
        public void TheConditionIsUnchangedByTheSwapAndByTheRePlaceThatFollowIt()
        {
            foreach (var cardId in MinCardContent.VanishingCards())
            {
                var board = Board();
                Place(board, Step(HexDirection.East, 3));
                Assert.That(Verdict(cardId, board), Is.EqualTo(DefenseBlockResult.Blocked));

                Assert.That(TheSwapCouldBePerformed(cardId, board, null), Is.True);
                Assert.That(Verdict(cardId, board), Is.EqualTo(DefenseBlockResult.Blocked),
                    $"{cardId.Value}: after the exchange she stands where the piece stood and the " +
                    "piece where she stood, so the separation the condition measures is the number it " +
                    "was. A verdict taken at p.14 step 3 and read at step 6 cannot be contradicted by " +
                    "the step 4 between them.");
            }
        }

        /// <summary>
        /// The structural half of the same claim, and the half a board cannot prove: the preview a
        /// defender decides from and the resolution that answers them both route through the single
        /// query, and that query and the swap's own capability both route through the single reading.
        /// </summary>
        [Test]
        public void ThePreviewTheResolverAndTheSwap_EachReachTheOneReading()
        {
            var evaluate = ILConformanceScanner.Method(
                typeof(DefenseBlockQuery), nameof(DefenseBlockQuery.Evaluate));
            var find = ILConformanceScanner.Method(
                typeof(OwnedTokenReachQuery), nameof(OwnedTokenReachQuery.TryFindOwnedToken));

            // One literal, not a concatenation: the living-document validator's reflected-member check
            // reads the string a test hands `GetType`.
            var backend = typeof(RuleModificationSet).Assembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                throwOnError: true);
            var ownedTokenFacts = typeof(RuleModificationSet).Assembly.GetType(
                "GuardsDigital.Core.Effects.Synthetic.SyntheticOwnedTokenCapabilities",
                throwOnError: true);

            Assert.That(ILConformanceScanner.References(new[] { backend }, evaluate), Is.True,
                "The resolution answers a Block through the one query...");
            Assert.That(
                ILConformanceScanner.References(new[] { typeof(MatchSession) }, evaluate), Is.True,
                "...and so does the defence preview, which is milestone 8.11a's whole rule.");
            Assert.That(
                ILConformanceScanner.References(new[] { typeof(DefenseBlockQuery) }, find), Is.True,
                "The condition asks where her bomb is through the one reading...");
            Assert.That(
                ILConformanceScanner.References(
                    ownedTokenFacts.GetNestedTypes(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic),
                    find),
                Is.True,
                "...and so does the capability her swap consumes, so the two cannot be made to " +
                "disagree by any board.");
        }

        // -------------------------------------------------------------------------------------
        // (d) Compositions this condition refuses rather than answering
        // -------------------------------------------------------------------------------------

        [Test]
        public void WithNoBoardNoCatalogOrNoDefenseText_TheConditionRefusesLoudlyRatherThanAnswering()
        {
            var board = Board();
            Place(board, Step(HexDirection.East, 1));
            var definitions = Definitions();

            Assert.That(
                () => DefenseBlockQuery.Evaluate(
                    Offer(MinCardContent.Poof), definitions, Reference(NonRangedAttackCard),
                    AttackActionId, new MarkerAttachmentRegister(), Attacker, Set(board),
                    tokens: null, heroItems: null, capabilities: Capabilities),
                Throws.InvalidOperationException,
                "A silent \"not in range\" for a board nobody looked at would grey a defence that in " +
                "fact holds - milestone 8.40c's rule.");
            Assert.That(
                () => DefenseBlockQuery.Evaluate(
                    Offer(MinCardContent.Poof), definitions, Reference(NonRangedAttackCard),
                    AttackActionId, new MarkerAttachmentRegister(), Attacker, Set(board),
                    board, null, capabilities: null),
                Throws.InvalidOperationException,
                "and with no catalog nothing could say which token that card text swaps with.");

            var textless = new[]
            {
                AttackCard(NonRangedAttackCard, AttackRangeKind.NonRanged),
                BlockCardWithoutItsText()
            };
            Assert.That(
                () => DefenseBlockQuery.Evaluate(
                    new ActionOffer(
                        new CardInstanceId("min-vanishing-textless"), textless[1].Reference,
                        MinCardContent.PrimaryBlock, Min,
                        ActionInvocationContext.PrintedPrimary, true),
                    textless, textless[0].Reference, AttackActionId,
                    new MarkerAttachmentRegister(), Attacker, Set(board), board, null, Capabilities),
                Throws.InvalidOperationException,
                "and a card printing this condition with no Defense text at all has no swap for the " +
                "condition to be the precondition of.");
        }

        // -------------------------------------------------------------------------------------
        // (e) The deferral, from both sides
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.61j FLIPPED this case: the swap all three faces perform runs on 8.61a's
        /// bounded owned-token fact, and a configured match can start it now.
        /// </summary>
        [Test]
        public void HerVanishingBlocksContractsAreRegisteredInThisBuild()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            Assert.That(MinComponentContent.SmokeBombDeferredHandlers(), Is.Empty);
            foreach (var handler in MinComponentContent.SmokeBombRegisteredHandlers())
            {
                Assert.That(build.Capabilities.Registered, Has.Member(handler.Reference),
                    $"'{handler.Reference}' is in the build's catalog, so a configured match can " +
                    "start the swap these three faces perform.");
            }

            Assert.That(
                MinCardContent.AllDefinitions.Select(card => card.Id),
                Is.SupersetOf(MinCardContent.VanishingCards()),
                "The three faces join the class aggregate the corpus sweeps read her through, so a " +
                "sweep that checks every Block in the build sees these too.");
        }

        // -------------------------------------------------------------------------------------
        // (f) p.14 driven whole: the swap at step 4, the verdict at step 6, the rider at step 7
        // -------------------------------------------------------------------------------------

        [TestCase("min-poof")]
        [TestCase("min-vanish")]
        [TestCase("min-ruse")]
        public void WithHerBombInRange_TheSwapRunsAtStep4AndTheAttackIsBlockedWhateverItsTotal(
            string cardId)
        {
            var turn = new VanishingTurn(
                new CardDefinitionId(cardId), bombAt: VanishingTurn.BombFarFromTheAttacker,
                // What all three faces share is the swap, so Ruse's own second sentence is declined
                // here and measured on its own below; taken, it would move the piece this case is
                // about after the exchange it is about.
                declineTheRePlace: true);

            turn.Drive();

            Assert.That(turn.MinStandsAt, Is.EqualTo(VanishingTurn.BombFarFromTheAttacker),
                "p.19's Swap: she stands where the bomb stood...");
            Assert.That(turn.BombStandsAt, Is.EqualTo(VanishingTurn.MinsStartingSpace),
                "...and the bomb stands where she stood. One exchange, no movement and no placement.");
            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "p.14: \"If the Block condition is fulfilled, you successfully defend, regardless of " +
                "the attack value\" - and the attack printed 9 against a card printing no Defense at all.");
            Assert.That(turn.RecordedDefenseTotal, Is.Null,
                "A Block prints no Defense value, so the resolution records none to compare.");
            Assert.That(turn.MinIsStanding, Is.True);
            Assert.That(turn.BlockCardLocation, Is.EqualTo(CardLocation.DiscardPile),
                "and it was spent from hand exactly as every other Defense action is.");
            Assert.That(turn.TimesDefended, Is.EqualTo(1),
                "The 8.53 scoreboard counts a blocked attack as a defence, not as a kill.");
        }

        [TestCase("min-poof")]
        [TestCase("min-ruse")]
        public void WithNoBombInRange_TheBlockFailsAndPage14DefeatsHerWithItsRewards(string cardId)
        {
            var turn = new VanishingTurn(new CardDefinitionId(cardId), bombAt: null);

            turn.Drive();

            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful),
                "p.14: \"If not, you are defeated.\" There is no Defense value to fall back on.");
            Assert.That(turn.MinIsStanding, Is.False, "and p.14 step 6 removed her.");
            Assert.That(turn.MinStillStandsWhereSheStarted, Is.True,
                "Nothing swapped: the mandatory pair was p.10's impossible step, so the space she was " +
                "removed from is the space she was attacked in.");
            Assert.That(
                turn.RedLivesSpent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(
                    MinCardContent.VanishingDefinition(new CardDefinitionId(cardId))
                        .Progression.Tier,
                    0)),
                "p.13 prices a hero Defeat in her own team's Life counters, \"equal to that hero's " +
                "lowest card Tier\" - which is Poof!'s I and Ruse's III, so the two cases pay " +
                "different numbers for the same page...");
            Assert.That(turn.AttackerCoins, Is.GreaterThan(0),
                "...and pays the attacker coins for it.");
            Assert.That(turn.TimesDefended, Is.EqualTo(0),
                "An unfulfilled Block is not a defence on the 8.53 scoreboard.");
        }

        [Test]
        public void ABombOutOfTheCardsRange_FailsWhereTheSameBoardWouldHaveBlockedAtOneMore()
        {
            var poof = new VanishingTurn(MinCardContent.Poof, bombAt: VanishingTurn.BombAtFour);
            poof.Drive();

            Assert.That(poof.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful),
                "Poof! prints Range 3 and the bomb stands at 4.");

            var vanish = new VanishingTurn(MinCardContent.Vanish, bombAt: VanishingTurn.BombAtFour);
            vanish.Drive();

            Assert.That(vanish.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "and Vanish prints 4, which is the whole difference between the two faces.");
            Assert.That(vanish.MinStandsAt, Is.EqualTo(VanishingTurn.BombAtFour));
        }

        /// <summary>
        /// p.14 step 7 - "Apply 'After the attack:' text on the Attack card" - runs after step 6, so the
        /// board it reads is the board her Defense text left. The attacker's rider is Crane's shape,
        /// "Push an enemy unit adjacent to you", and the two cases differ only in where her bomb stood.
        /// </summary>
        [Test]
        public void TheAttackersAfterAttackText_ReadsTheAdjacencyTheSwapLeftBehind()
        {
            var away = new VanishingTurn(
                MinCardContent.Poof, bombAt: VanishingTurn.BombFarFromTheAttacker,
                attackerPushesAfterTheAttack: true);
            away.Drive();

            Assert.That(away.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(away.MinStandsAt, Is.EqualTo(VanishingTurn.BombFarFromTheAttacker),
                "She vanished out of contact and the attacker's own push therefore had no adjacent " +
                "enemy to name: p.14's step 7 reads the board step 4 left, not the board the attack " +
                "was declared on.");

            var beside = new VanishingTurn(
                MinCardContent.Poof, bombAt: VanishingTurn.BombBesideTheAttacker,
                attackerPushesAfterTheAttack: true);
            beside.Drive();

            Assert.That(beside.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "The control blocks the same way...");
            Assert.That(beside.MinStandsAt, Is.Not.EqualTo(VanishingTurn.BombBesideTheAttacker),
                "...but the swap left her on a space still beside him, so the rider named her there " +
                "and moved her. One rider, two boards, and the board is the one step 4 left.");
            Assert.That(
                HexDistanceQuery.Calculate(beside.MinStandsAt, VanishingTurn.AttackersSpace),
                Is.GreaterThan(1),
                "and it pushed her away from him, which is what the printed push does.");
        }

        /// <summary>
        /// p.14 computes the minion Defense modifiers at step 2 and the defender's own text at step 4,
        /// so a swap out of a friendly minion's contact cannot unmake a modifier already calculated -
        /// and on a Block it never mattered, because the page's step 5 total is not computed at all.
        /// </summary>
        [Test]
        public void MinionDefenseModifiersAreCalculatedBeforeTheSwapAndNoTotalIsComputedAnyway()
        {
            var turn = new VanishingTurn(
                MinCardContent.Poof, bombAt: VanishingTurn.BombFarFromTheAttacker,
                friendlyMinionBesideMin: true);

            var beforeTheSwap = turn.MinionContributionsForMin();
            Assert.That(beforeTheSwap, Is.Not.Empty,
                "p.14 step 2 reads the minions standing beside the defender, which is before her own " +
                "text runs at step 4.");

            turn.Drive();

            Assert.That(turn.MinionContributionsForMin(), Is.Empty,
                "and after the swap she is out of that minion's contact, which is exactly why the " +
                "page computes the modifier at step 2 rather than at step 5.");
            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(turn.RecordedDefenseTotal, Is.Null,
                "A Block is settled by its condition regardless of the attack value, so no minion " +
                "modifier - taken at step 2 or at any other step - enters this verdict at all.");
        }

        /// <summary>
        /// p.13's other Attack subtype, driven whole - because a condition that asks nothing about the
        /// attack has to be measured against both, and the page's own window is what opens either way.
        /// </summary>
        [Test]
        public void ARangedAttackIsBlockedAndFailedOnExactlyTheSameBoardsAsANonRangedOne()
        {
            var blocked = new VanishingTurn(
                MinCardContent.Poof, bombAt: VanishingTurn.BombFarFromTheAttacker,
                theAttackIsRanged: true);
            blocked.Drive();

            Assert.That(blocked.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(blocked.MinStandsAt, Is.EqualTo(VanishingTurn.BombFarFromTheAttacker),
                "The same swap, answering a Ranged attack: her sentence names no subtype at all.");

            var failed = new VanishingTurn(
                MinCardContent.Poof, bombAt: null, theAttackIsRanged: true);
            failed.Drive();

            Assert.That(failed.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(failed.MinIsStanding, Is.False,
                "and it fails against a Ranged attack on exactly the board it fails against a " +
                "non-Ranged one.");
        }

        /// <summary>
        /// THE AGREEMENT, asked of the two call sites rather than of the query alone: the verdict the
        /// defence preview would have shown - the same query with the same arguments
        /// <c>MatchSession.CreateDefensePreview</c> passes, on the board the attack is standing on - is
        /// the verdict p.14's step 3 then recorded and step 6 then read.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TheVerdictThePreviewWouldShow_IsTheVerdictTheResolutionRecords(bool bombInRange)
        {
            var turn = new VanishingTurn(
                MinCardContent.Poof,
                bombAt: bombInRange ? VanishingTurn.BombFarFromTheAttacker : (HexCoordinate?)null);

            var previewed = turn.PreviewShapedVerdict();
            turn.Drive();

            Assert.That(
                previewed,
                Is.EqualTo(bombInRange
                    ? DefenseBlockResult.Blocked
                    : DefenseBlockResult.ConditionUnfulfilled));
            Assert.That(
                turn.Outcome,
                Is.EqualTo(
                    AttackOutcomeComparison.Create(turn.AttackTotal, null, previewed).Outcome),
                "The outcome the preview sums for that verdict - the same sum " +
                "MatchSession.CreateDefensePreview projects - is the outcome p.14 reached. The " +
                "preview is not a second derivation that happens to agree: both callers reach one " +
                "query, and that query reaches one reading of the board.");
            Assert.That(
                turn.Outcome,
                Is.EqualTo(bombInRange
                    ? AttackResolutionOutcome.SuccessfullyDefended
                    : AttackResolutionOutcome.AttackSuccessful));
        }

        // -------------------------------------------------------------------------------------
        // (g) Ruse's second sentence
        // -------------------------------------------------------------------------------------

        [Test]
        public void RuseMayRePlaceTheBombInRangeOfWhereTheSwapLeftHer()
        {
            var turn = new VanishingTurn(
                MinCardContent.Ruse, bombAt: VanishingTurn.BombFarFromTheAttacker);

            turn.Drive();

            Assert.That(turn.RePlaceWasOptional, Is.True,
                "\"You may place the Smoke bomb into a space in range\" - p.10's `may`, hers to answer.");
            Assert.That(turn.RePlaceDestinations, Is.Not.Empty);
            Assert.That(
                turn.RePlaceDestinations.All(space =>
                    HexDistanceQuery.Calculate(VanishingTurn.BombFarFromTheAttacker, space) <= 4),
                Is.True,
                "Every offered space is inside Ruse's printed Range 4 of where the swap left her - not " +
                "of where she was standing when the attack was declared.");
            Assert.That(
                turn.RePlaceDestinations,
                Has.No.Member(VanishingTurn.BombFarFromTheAttacker),
                "and not the space she now occupies, because p.18 places a token into an empty space.");
            Assert.That(turn.BombStandsAt, Is.EqualTo(turn.RePlaceChose),
                "The piece moved to the space she chose - the same bomb, picked up and put down.");
            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "and the Block it followed still held.");
        }

        [Test]
        public void RusesRePlaceMayBeDeclinedAndTheBlockStillHolds()
        {
            var turn = new VanishingTurn(
                MinCardContent.Ruse, bombAt: VanishingTurn.BombFarFromTheAttacker,
                declineTheRePlace: true);

            turn.Drive();

            Assert.That(turn.RePlaceDestinations, Is.Empty,
                "Declining answers the `may` without ever opening the placement.");
            Assert.That(turn.BombStandsAt, Is.EqualTo(VanishingTurn.MinsStartingSpace),
                "so the bomb stays where the swap put it - in the space she vacated.");
            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "p.10: declining an optional step continues the action, and the Block was settled by " +
                "its own condition before this sentence was read.");
        }

        [Test]
        public void RusesRePlaceNeverRunsOnABoardWhereTheSwapCouldNotHappen()
        {
            var turn = new VanishingTurn(MinCardContent.Ruse, bombAt: VanishingTurn.BombAtFive);

            turn.Drive();

            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(turn.RePlaceWasOptional, Is.False,
                "\"the Smoke bomb\" is the piece she just swapped with, and there was none: p.10's " +
                "impossible step ends the sequence, so the second sentence is never reached.");
            Assert.That(turn.BombStandsAt, Is.EqualTo(VanishingTurn.BombAtFive),
                "and the bomb standing out of range was never picked up.");
        }

        // -------------------------------------------------------------------------------------
        // (h) Through a snapshot, at every step of the page
        // -------------------------------------------------------------------------------------

        [Test]
        public void EveryStateOfTheDefenceIncludingItsPauses_CapturesAndRestoresByteIdentically()
        {
            var turn = new VanishingTurn(
                MinCardContent.Ruse, bombAt: VanishingTurn.BombFarFromTheAttacker,
                captureEveryStep: true);

            turn.Drive();

            Assert.That(turn.CapturedStates, Is.GreaterThan(0));
            Assert.That(turn.StatesPausedInsideTheDefenseText, Is.GreaterThan(0),
                "and at least one of them was taken while her Defense text stood at a pause inside " +
                "p.14's own window, which is the state a retained-prefix Undo replays through.");
            Assert.That(turn.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "The run that was captured at every step still reached the page's own verdict.");
        }

        // =====================================================================================
        // Fixture support
        // =====================================================================================

        private static MatchParticipants Participants => StagedBoardParticipants.Create();

        private static EffectCapabilityCatalog Capabilities
        {
            get
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new EffectCapabilityCatalog(defaults.Capabilities.Registered
                    .Select(defaults.Capabilities.Load)
                    .Concat(MinComponentContent.SmokeBombDeferredHandlers()));
            }
        }

        private static ComponentDefinitionCatalog Components =>
            new ComponentDefinitionCatalog(
                MinComponentContent.SmokeBombTokens().Concat(new[] { OtherTokenOfHers }),
                Array.Empty<MarkerDefinition>());

        /// <summary>
        /// A second piece of hers, standing in for the mines and the Grenade 8.61d and 8.61f own. It
        /// carries the bomb's own properties deliberately: what must not decide the condition is any
        /// resemblance between the pieces, only which one her card text names.
        /// </summary>
        private static readonly TokenDefinition OtherTokenOfHers = new TokenDefinition(
            new TokenDefinitionId("min-vanishing-other-token"), 1, "Mine", 1,
            MinComponentContent.SmokeBombDefinition.Properties,
            TokenRemovalCondition.EndOfRoundCleanup);

        private static ActionDefinition BlockAction(CardDefinitionId cardId) =>
            MinCardContent.VanishingDefinition(cardId).ActionDefinitions
                .Single(action => action.DefenseResolution == PrintedDefenseResolution.Block);

        private static DefenseBlockResult Verdict(
            CardDefinitionId cardId,
            AdjacentMoveAuthority board,
            HeroItemState items = null,
            CardDefinitionId attackCard = default) =>
            DefenseBlockQuery.Evaluate(
                Offer(cardId),
                Definitions(),
                Reference(attackCard.IsDefined ? attackCard : NonRangedAttackCard),
                AttackActionId,
                new MarkerAttachmentRegister(),
                Attacker,
                Set(board),
                board,
                items,
                Capabilities);

        /// <summary>
        /// Whether the swap the card's Defense text opens with could actually be performed on this
        /// board - asked of the composed body itself, through the effect frame, rather than of the
        /// condition. It is the other side of the equivalence the condition claims.
        /// </summary>
        private static bool TheSwapCouldBePerformed(
            CardDefinitionId cardId, AdjacentMoveAuthority board, HeroItemState items)
        {
            var definition = MinCardContent.VanishingDefinition(cardId);
            var text = BlockAction(cardId).CardTextAt(CardTextTiming.DefenseAction);
            var context = new EffectResolutionContext(
                text, Capabilities, Participants, null, 1, 1, Set(board),
                EffectCardState.Of(Definitions(), Array.Empty<CardInstance>()),
                ActionKind.Defense,
                components: new MatchComponents(
                    Components, board, new MarkerAttachmentRegister(), board, board, board, board),
                heroItems: items);
            var started = EffectResolutionSession.Start(
                new EffectResolutionId("min-vanishing-swap-probe"),
                Instance(cardId), definition.Reference, MinCardContent.PrimaryBlock,
                Min, Min, ActionInvocationContext.PrintedPrimary, context);
            Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());

            // The swap moved her exactly when it ran: p.19's Swap exchanges the two spaces, so a body
            // that could not perform it leaves her standing where she started.
            return !board.Occupancies.Single(value => value.HeroId.Equals(Min))
                .Coordinate.Equals(MinsSpace);
        }

        private static ActionOffer Offer(CardDefinitionId cardId) =>
            new ActionOffer(
                Instance(cardId),
                MinCardContent.VanishingDefinition(cardId).Reference,
                MinCardContent.PrimaryBlock,
                Min,
                ActionInvocationContext.PrintedPrimary,
                true);

        private static CardInstanceId Instance(CardDefinitionId cardId) =>
            new CardInstanceId(cardId.Value + "-instance");

        private static CardDefinitionReference Reference(CardDefinitionId cardId) =>
            Definitions().Single(definition => definition.Id.Equals(cardId)).Reference;

        private static IReadOnlyList<CardDefinition> Definitions() =>
            MinCardContent.VanishingDefinitions
                .Concat(new[]
                {
                    AttackCard(NonRangedAttackCard, AttackRangeKind.NonRanged),
                    AttackCard(RangedAttackCard, AttackRangeKind.Ranged)
                })
                .ToArray();

        /// <summary>One attacking face per p.13 classification; neither prints any text.</summary>
        private static CardDefinition AttackCard(CardDefinitionId cardId, AttackRangeKind classification) =>
            new CardDefinition(
                cardId,
                MinCardContent.ContentRevision,
                "TEST - Min vanishing attacker",
                new InitiativeValue(6),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        AttackActionId,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        classification == AttackRangeKind.Ranged
                            ? new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Attack, 9),
                                new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                            }
                            : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 9) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        classification,
                        default)
                });

        /// <summary>
        /// A face printing this condition and no Defense text - a composition nothing can resolve, kept
        /// here so the refusal is measured rather than described.
        /// </summary>
        private static CardDefinition BlockCardWithoutItsText() =>
            new CardDefinition(
                new CardDefinitionId("min-vanishing-textless-block"),
                MinCardContent.ContentRevision,
                "TEST - Min Block without its text",
                new InitiativeValue(2),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        MinCardContent.PrimaryBlock,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 3) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        null,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.Block,
                        CardTextTiming.None,
                        blockCondition: PrintedBlockCondition
                            .TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange)
                });

        /// <summary>
        /// p.16's Range item, placed under the slot her dashboard accepts that icon in - asked of the
        /// layout rather than written as an index, because which slot takes which icon is the
        /// dashboard's own rule.
        /// </summary>
        private static HeroItemState RangeItem()
        {
            Assert.That(
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(
                    PrintedValueChannel.Range, out var slot),
                Is.True);
            return HeroItemState.Restore(
                Participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(
                        Min, new CardInstanceId("min-vanishing-range-item"),
                        slot.Position, PrintedValueChannel.Range, 1)
                }));
        }

        private static RuleModificationSet Set(AdjacentMoveAuthority board) =>
            RuleModificationQuery.Build(
                null, board, Participants, TieBreakerState.Unrevealed, UnlockedUltimateSet.None,
                RuleObject.None, null, null);

        private static HexCoordinate Step(HexDirection direction, int distance) =>
            StraightLineQuery.Step(MinsSpace, direction, distance);

        private static PlacedToken Place(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            HeroId controller = default,
            TokenDefinition definition = null)
        {
            var owner = controller.IsDefined ? controller : Min;
            var token = new PlacedToken(
                new TokenInstanceId(
                    (definition ?? MinComponentContent.SmokeBombDefinition).Id.Value + "-" +
                    owner.Value + "-instance"),
                definition ?? MinComponentContent.SmokeBombDefinition,
                owner,
                owner,
                new CardInstanceId("min-vanishing-token-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static IEnumerable<HexCoordinate> Disc(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(MinsSpace, coordinate) <= radius)
                    {
                        yield return coordinate;
                    }
                }
            }
        }

        private static BoardMapDefinition Map()
        {
            var zone = new BoardMapZone(
                new ZoneId("min-vanishing-zone"), "Min Vanishing Zone", "synthetic", null, false, false);
            var playable = Disc(5)
                .Select(space => new BoardMapSpace(
                    SpaceIdOf(space), space, BoardMapSpaceKind.Playable, zone.Id))
                .ToArray();
            return new BoardMapDefinition(
                MapIdentity, 1, "Min vanishing map", playable, new[] { zone },
                new[]
                {
                    new BoardMapSpawnPoint(
                        Spawn, SpaceIdOf(new HexCoordinate(-5, 0)),
                        StagedBoardParticipants.RedTeamIdValue, "melee")
                },
                Array.Empty<ZoneId>());
        }

        private static SpaceId SpaceIdOf(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        /// <summary>
        /// The radius-5 disc every case here measures on: Min at its centre with the attacking hero
        /// beside her, so a non-Ranged attack has a target and every distance from 1 to 5 is a space her
        /// bomb can stand on.
        /// </summary>
        private static AdjacentMoveAuthority Board(HexCoordinate? attackerAt = null) =>
            new AdjacentMoveAuthority(
                Map(),
                new[]
                {
                    new HeroOccupancy(Min, MinsSpace),
                    new HeroOccupancy(
                        Attacker, attackerAt ?? MinsSpace.GetNeighbor(HexDirection.SouthWest))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

        // =====================================================================================
        // One whole rulebook p.14, driven offline
        // =====================================================================================

        /// <summary>
        /// One attacked turn, run through a composed round rather than a configured session - because
        /// every capability her Defense text reaches is DEFERRED until 8.61j, and a live
        /// <see cref="MatchSession"/> runs the build's registered catalog alone. The round is composed
        /// with a fixture catalog carrying 8.61a's handlers beside everything the build registers, which
        /// is the arrangement <c>MinSmokeBombTests</c> and <c>MinBorrowedRiderTests</c> already take.
        ///
        /// <para>The attacking hero commits a card printing a plain non-Ranged Attack of 9 - a number no
        /// Defense value in her set could answer, so a Block that holds is holding on its condition and
        /// on nothing else. Min commits a filler and keeps her Block in hand, because p.14's step 3
        /// offers the defender their HAND.</para>
        /// </summary>
        private sealed class VanishingTurn
        {
            internal static readonly HexCoordinate MinsStartingSpace = new HexCoordinate(0, 0);

            internal static readonly HexCoordinate AttackersSpace =
                MinsStartingSpace.GetNeighbor(HexDirection.East);

            /// <summary>Three from her and four from him: a swap here leaves his rider nothing to name.</summary>
            internal static readonly HexCoordinate BombFarFromTheAttacker =
                StraightLineQuery.Step(MinsStartingSpace, HexDirection.West, 3);

            /// <summary>One from her and one from him: the control, where the swap keeps her in contact.</summary>
            internal static readonly HexCoordinate BombBesideTheAttacker =
                MinsStartingSpace.GetNeighbor(HexDirection.NorthEast);

            /// <summary>Outside Poof!'s printed 3 and inside Vanish's 4.</summary>
            internal static readonly HexCoordinate BombAtFour =
                StraightLineQuery.Step(MinsStartingSpace, HexDirection.West, 4);

            /// <summary>Outside every one of the three printed Ranges.</summary>
            internal static readonly HexCoordinate BombAtFive =
                StraightLineQuery.Step(MinsStartingSpace, HexDirection.West, 5);

            private static readonly CardDefinitionId TurnFillerCard =
                new CardDefinitionId("min-vanishing-filler");

            private static readonly CardDefinitionId AttackerCard =
                new CardDefinitionId("min-vanishing-attacker-card");

            private static readonly ActionDefinitionId AttackerAttackId =
                new ActionDefinitionId("primary-attack");

            private static readonly CardInstanceId BlockCardInstance =
                new CardInstanceId("min-vanishing-block-card");

            private static readonly CardInstanceId MinsCommit =
                new CardInstanceId("min-vanishing-commit");

            private static readonly CardInstanceId AttackersCommit =
                new CardInstanceId("min-vanishing-attacker-commit");

            private static readonly MinionId RedMinion = new MinionId("min-vanishing-red-minion");

            private readonly List<CardDefinition> definitions = new List<CardDefinition>();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly MatchParticipants participants = Participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MatchContentCatalogs catalogs;
            private readonly RoundResolutionAuthority round;
            private readonly CardActionResolutionAuthority cardActions;
            private readonly List<string> trace = new List<string>();
            private readonly List<HexCoordinate> rePlaceDestinations = new List<HexCoordinate>();
            private readonly bool declineTheRePlace;
            private readonly bool captureEveryStep;

            internal VanishingTurn(
                CardDefinitionId blockCard,
                HexCoordinate? bombAt,
                bool attackerPushesAfterTheAttack = false,
                bool friendlyMinionBesideMin = false,
                bool declineTheRePlace = false,
                bool captureEveryStep = false,
                bool theAttackIsRanged = false)
            {
                this.declineTheRePlace = declineTheRePlace;
                this.captureEveryStep = captureEveryStep;

                var minionAt = MinsStartingSpace.GetNeighbor(HexDirection.NorthWest);
                board = new AdjacentMoveAuthority(
                    Map(),
                    new[]
                    {
                        new HeroOccupancy(Min, MinsStartingSpace),
                        new HeroOccupancy(Attacker, AttackersSpace)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    friendlyMinionBesideMin
                        ? new[]
                        {
                            new QuickGameMinionSetupState(
                                RedMinion,
                                new TeamId(StagedBoardParticipants.RedTeamIdValue),
                                TeamSide.Red,
                                StandardMinionType.Melee,
                                new ConfiguredSpawnPointReference(MapIdentity, Spawn),
                                new ConfiguredSpaceReference(MapIdentity, SpaceIdOf(minionAt)),
                                minionAt)
                        }
                        : Array.Empty<QuickGameMinionSetupState>());

                definitions.AddRange(MinCardContent.VanishingDefinitions);
                definitions.Add(Filler);
                definitions.Add(AttackingCard(attackerPushesAfterTheAttack, theAttackIsRanged));

                instances.Add(CardInstance.CreateInHand(
                    BlockCardInstance,
                    MinCardContent.VanishingDefinition(blockCard).Reference,
                    Min));
                instances.Add(CardInstance.CreateInHand(MinsCommit, Filler.Reference, Min));
                instances.Add(CardInstance.CreateInHand(
                    AttackersCommit,
                    definitions.Single(card => card.Id.Equals(AttackerCard)).Reference,
                    Attacker));

                catalogs = new MatchContentCatalogs(Capabilities, Components);
                cardActions = new CardActionResolutionAuthority(
                    catalogs,
                    board,
                    new MarkerAttachmentRegister(),
                    new HeroProgressionState(participants),
                    new TeamLifeState(
                        participants, RoundResolutionAuthority.BaselineLifeCountersPerTeam));
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForCardActionsOnly(cardActions));

                if (bombAt.HasValue)
                {
                    Place(board, bombAt.Value);
                }

                Accept(round.TryCommitCard(Min, MinsCommit));
                Accept(round.TryCommitCard(Attacker, AttackersCommit));
            }

            internal AttackResolutionOutcome Outcome { get; private set; } =
                AttackResolutionOutcome.None;

            internal int? RecordedDefenseTotal { get; private set; }

            /// <summary>The Attack Total p.14's step 3 showed the defender, for the preview's own sum.</summary>
            internal int AttackTotal { get; private set; }

            /// <summary>
            /// The same query with the same arguments the defence preview passes -
            /// <c>MatchSession.CreateDefensePreview</c>'s own call shape - asked of this board before
            /// the attack runs. It is what the defender would have been shown.
            /// </summary>
            internal DefenseBlockResult PreviewShapedVerdict() =>
                DefenseBlockQuery.Evaluate(
                    new ActionOffer(
                        BlockCardInstance,
                        instances.Single(instance => instance.Id.Equals(BlockCardInstance))
                            .DefinitionReference,
                        MinCardContent.PrimaryBlock,
                        Min,
                        ActionInvocationContext.PrintedPrimary,
                        true),
                    definitions,
                    definitions.Single(card => card.Id.Equals(AttackerCard)).Reference,
                    AttackerAttackId,
                    new MarkerAttachmentRegister(),
                    Attacker,
                    Set(board),
                    board,
                    round.HeroItems,
                    catalogs.Capabilities);

            internal bool RePlaceWasOptional { get; private set; }

            internal HexCoordinate RePlaceChose { get; private set; }

            internal int CapturedStates { get; private set; }

            internal int StatesPausedInsideTheDefenseText { get; private set; }

            internal IReadOnlyList<HexCoordinate> RePlaceDestinations => rePlaceDestinations;

            internal bool MinIsStanding =>
                board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(Min));

            internal HexCoordinate MinStandsAt =>
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Min)).Coordinate;

            internal bool MinStillStandsWhereSheStarted =>
                !MinIsStanding || MinStandsAt.Equals(MinsStartingSpace);

            internal HexCoordinate BombStandsAt => board.PlacedTokens
                .Single(token => token.Definition.Reference.Equals(
                    MinComponentContent.SmokeBombDefinition.Reference))
                .Coordinate;

            internal CardLocation BlockCardLocation => instances
                .Single(instance => instance.Id.Equals(BlockCardInstance)).State.Location;

            internal int TimesDefended => round.Scoreboard.For(Min).TimesDefended;

            internal int RedLivesSpent =>
                RoundResolutionAuthority.BaselineLifeCountersPerTeam -
                round.TeamLives.GetCounters(new TeamId(StagedBoardParticipants.RedTeamIdValue));

            internal int AttackerCoins => round.CoinBalances
                .Single(balance => balance.Key.Equals(Attacker)).Value;

            /// <summary>p.14 step 2's minion Defense modifiers for her, read off the live board.</summary>
            internal IReadOnlyList<ValueContribution> MinionContributionsForMin() =>
                DefenseValueQuery.CalculateMinionContributions(Min, participants, board);

            internal string Trace() => string.Join(",", trace);

            internal void Drive()
            {
                var started = false;
                for (var step = 0; step < 160; step++)
                {
                    var turn = round.CurrentTurnAuthority;
                    if (turn == null)
                    {
                        trace.Add("no-turn");
                        return;
                    }

                    if (turn.PendingChoice != null)
                    {
                        trace.Add("tie");
                        var tie = turn.PendingChoice;
                        Accept(round.TryChooseTiedActor(
                            tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        trace.Add("release");
                        Accept(round.TryReleaseTurn());
                        continue;
                    }

                    if (turn.PendingAttackTargetChoice != null)
                    {
                        var choice = turn.PendingAttackTargetChoice;
                        trace.Add("target");
                        Accept(round.TryAnswerAttackTarget(
                            new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, Min)));
                        continue;
                    }

                    if (turn.PendingDefenseChoice != null)
                    {
                        var choice = turn.PendingDefenseChoice;
                        trace.Add("defense");
                        AttackTotal = choice.AttackTotal;
                        Assert.That(
                            choice.LegalDefenseOffers.Select(offer => offer.SourceCardInstanceId),
                            Has.Member(BlockCardInstance),
                            "p.14 step 3 offers the defender their hand, and her Block is in it.");
                        Accept(round.TryAnswerDefenseResponse(
                            DefenseResponseChoiceCommand.SelectCards(
                                choice.ResolutionId, choice.ChoiceId, BlockCardInstance)));
                        RecordedDefenseTotal = cardActions.PendingAttackResolution?.DefenseTotal;
                        continue;
                    }

                    if (turn.PendingEffectChoice != null)
                    {
                        var choice = turn.PendingEffectChoice;
                        var node = choice.NodeId.Value;
                        trace.Add("choice:" + node);
                        if (node == "vanishing-replace-may")
                        {
                            RePlaceWasOptional = choice.DeclineInvocation != null;
                            if (declineTheRePlace)
                            {
                                Accept(round.TryAnswerEffectChoice(choice.DeclineInvocation));
                                continue;
                            }
                        }

                        if (node == "vanishing-replace-place")
                        {
                            rePlaceDestinations.Clear();
                            rePlaceDestinations.AddRange(choice.Candidates
                                .Where(candidate =>
                                    candidate.Value.Kind == EffectValueKind.Coordinate)
                                .Select(candidate => candidate.Value.Coordinate));
                        }

                        // The attacker's own push is answered with the farthest answer it offers, so
                        // "up to 3 spaces" is actually taken: what this fixture measures about his
                        // rider is whether it found her at all, and a push of nought would hide that.
                        var answer = node == "attacker-push" && choice.SelectInvocations.Count > 0
                            ? choice.SelectInvocations
                                .OrderByDescending(invocation =>
                                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate
                                        ? HexDistanceQuery.Calculate(
                                            AttackersSpace, invocation.Candidate.Value.Coordinate)
                                        : invocation.Candidate.Value.Kind == EffectValueKind.Integer
                                            ? invocation.Candidate.Value.IntegerValue
                                            : 0)
                                .First()
                            : choice.SelectInvocations.Count > 0
                                ? choice.SelectInvocations[0]
                                : choice.ConfirmInvocation;
                        Assert.That(answer, Is.Not.Null,
                            Trace() + " | " + node + " offered no answer at all.");
                        if (node == "vanishing-replace-place")
                        {
                            RePlaceChose = answer.Candidate.Value.Coordinate;
                        }

                        Accept(round.TryAnswerEffectChoice(answer));
                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(AttackersCommit))
                        {
                            trace.Add("hold");
                            Accept(round.TryPerformHold(turn.SelectedActor.CardInstanceId));
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, Attacker, AttackersCommit)
                            .Single(value => value.ActionDefinitionId.Equals(AttackerAttackId));
                        Accept(round.TryStartAttack(
                            new ActionResolutionId("min-vanishing-attack"), offer));
                        started = true;
                        continue;
                    }

                    trace.Add("break");
                    break;
                }
            }

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    Trace() + " | refused: " + result.RejectionReason + "/" +
                    result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                foreach (var produced in result.Events ?? Array.Empty<RoundResolutionEvent>())
                {
                    // The 8.53 scoreboard is fed here for the reason it is fed at all: `MatchAggregate`
                    // is what calls Observe in a configured match, and this drive composes the round
                    // directly. The event and the reader are still production's.
                    round.Scoreboard.Observe(produced);
                    switch (produced.TurnEvent.CardActionEvent.Kind)
                    {
                        case CardActionResolutionEventKind.AttackSuccessfullyDefended:
                            Outcome = AttackResolutionOutcome.SuccessfullyDefended;
                            break;
                        case CardActionResolutionEventKind.AttackSuccessful:
                            Outcome = AttackResolutionOutcome.AttackSuccessful;
                            break;
                    }
                }

                if (!captureEveryStep) return;
                var pausedInsideHerDefenseText =
                    cardActions.PendingCardTextResolution != null &&
                    cardActions.PendingCardTextResolution.Attack != null &&
                    cardActions.PendingCardTextResolution.Frame.SourceCardInstanceId
                        .Equals(BlockCardInstance);
                var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var replay = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, payload, definitions, new[] { board.MapDefinition },
                    Array.Empty<QuickGameMinionSetupState>());
                Assert.That(
                    AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board),
                    Is.EqualTo(payload),
                    Trace() + " | a restore that re-serializes differently has changed the match it " +
                    "claims to be.");
                CapturedStates++;
                if (pausedInsideHerDefenseText)
                {
                    StatesPausedInsideTheDefenseText++;
                }
            }

            /// <summary>A card the seats commit so the round has a turn for each of them.</summary>
            private static readonly CardDefinition Filler = new CardDefinition(
                TurnFillerCard, MinCardContent.ContentRevision, "TEST - filler",
                new InitiativeValue(20), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });

            /// <summary>
            /// The attacking face: a plain non-Ranged Attack of 9, optionally printing Crane's own
            /// rider - "After the attack: Push an enemy unit adjacent to you up to 3 spaces" - which is
            /// what p.14's step 7 applies from the positions her Defense text left behind.
            /// </summary>
            private static CardDefinition AttackingCard(bool pushesAfterTheAttack, bool ranged) =>
                new CardDefinition(
                    AttackerCard, MinCardContent.ContentRevision, "TEST - Min's attacker",
                    new InitiativeValue(6), CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.BasicCard(CardColor.Gold),
                    new[]
                    {
                        new ActionDefinition(
                            AttackerAttackId, ActionKind.Attack,
                            ActionDefinitionOrigin.PrintedPrimary,
                            ranged
                                ? new[]
                                {
                                    new PrintedValueDefinition(PrintedValueChannel.Attack, 9),
                                    new PrintedValueDefinition(PrintedValueChannel.Range, 1)
                                }
                                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 9) },
                            new ActionDefinitionPolicy(
                                CardTextApplicationPolicy.Applies, false, true),
                            ranged ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged,
                            default,
                            pushesAfterTheAttack ? AfterAttackPush() : null,
                            PrintedTextComposition.None,
                            PrintedDefenseResolution.None,
                            pushesAfterTheAttack
                                ? CardTextTiming.AfterTheAttack
                                : CardTextTiming.None)
                    });

            /// <summary>"Push an enemy unit adjacent to you up to 3 spaces", on registered capabilities.</summary>
            private static EffectDefinition AfterAttackPush()
            {
                var root = new EffectNodeId("attacker-push-root");
                var subject = new EffectNodeId("attacker-push-target");
                var push = new EffectNodeId("attacker-push");
                return new EffectDefinition(
                    new EffectDefinitionId("min-vanishing-attacker-after-attack"),
                    MinCardContent.ContentRevision,
                    root,
                    new[]
                    {
                        new EffectNodeDefinition(
                            root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { subject, push }),
                        new EffectNodeDefinition(
                            subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticRosterCapabilities.ChooseTargetV2,
                            inputs: new[]
                            {
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticRosterCapabilities.OriginParameter,
                                    RosterQueryOrigin.Actor),
                                EffectInputBinding.FromLiteral(
                                    SyntheticRosterCapabilities.DistanceParameter,
                                    EffectValue.Integer(1)),
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticRosterCapabilities.UnitKindParameter,
                                    RosterUnitKind.Hero),
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticRosterCapabilities.RelationParameter,
                                    SourceTeamRelation.OpposingSource),
                                EffectInputBinding.FromClosedVocabulary(
                                    "activity", RuleActivity.ObjectPush)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticRosterCapabilities.ChosenHeroOutput,
                                    EffectValueKind.Hero),
                                new EffectOutputBinding(
                                    SyntheticRosterCapabilities.ChosenSpaceOutput,
                                    EffectValueKind.Coordinate)
                            }),
                        new EffectNodeDefinition(
                            push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticDisplacementCapabilities.PushTargetV3,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    "target", subject,
                                    SyntheticRosterCapabilities.ChosenSpaceOutput,
                                    EffectValueKind.Coordinate),
                                EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                    PushEligiblePieceKind.Unit)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding("moved", EffectValueKind.Integer)
                            })
                    });
            }
        }
    }
}
