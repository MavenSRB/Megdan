using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32a: Takahide's gold cards - two start in the deck, and each swaps itself with a
    /// different gold card in the deck; Bushido swaps "your gold card" with one, the new card facedown
    /// when the old one was resolved or discarded. Rulebook p.10: "When two cards are swapped, all their
    /// card states are swapped", and a facedown resolved or discarded card "loses its type, color and
    /// actions". TK-Q1 (the user, 2026-10-01): Float like a Butterfly swaps after its Movement. TK-Q2:
    /// after a swap, only the swapped-in card is the played card. Proven on
    /// <see cref="SyntheticGoldSwapHand"/>; no Takahide face is registered.
    /// </summary>
    public sealed class TakahideGoldSwapTests
    {
        private static readonly CardDefinitionId Bee = SyntheticGoldSwapHand.BeeId;
        private static readonly CardDefinitionId Butterfly = SyntheticGoldSwapHand.ButterflyId;
        private static readonly CardDefinitionId Tiger = SyntheticGoldSwapHand.TigerId;
        private static readonly CardDefinitionId Bushido = SyntheticGoldSwapHand.BushidoId;

        private static readonly CardStateFacts Resolved = new CardStateFacts(
            CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp);

        // -------------------------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheGoldCards_StartWhereTheirTextSays_OneInTheHandAndTwoInTheDeck()
        {
            var cards = SyntheticGoldSwapHand.CreateInstances(Stage.Taka, "taka");
            Assert.That(cards.Single(card => IsOf(card, Butterfly)).State.IsInitialInHand, Is.True,
                "\"This card starts the game in your hand.\"");
            Assert.That(cards.Single(card => IsOf(card, Bee)).State.IsInitialInDeck, Is.True,
                "\"This card starts the game in your deck.\"");
            Assert.That(cards.Single(card => IsOf(card, Tiger)).State.IsInitialInDeck, Is.True);
            Assert.That(cards.Single(card => IsOf(card, Bushido)).State.IsInitialInHand, Is.True);
            Assert.DoesNotThrow(() => CardInitialSetupValidator.Validate(SyntheticGoldSwapHand.Definitions, cards),
                "a basic card left in the deck by its own text is a legal starting state");
        }

        // -------------------------------------------------------------------------------------------
        // The resolving card (lesson 28: resolving with deck)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AfterTheAttack_TheCardSwapsWithAChosenDeckGold_AndTheActionResolvesTheSwappedInCard()
        {
            var stage = new Stage(played: Bee, deck: new[] { Butterfly, Tiger });
            stage.AttackTheEnemy();
            stage.EnemyDefendsWithTheWall();

            var choice = stage.Choice;
            Assert.That(choice, Is.Not.Null, "\"After the attack: Swap this card with a different gold card in your deck\"");
            Assert.That(choice.ChooserId, Is.EqualTo(Stage.Taka), "Takahide chooses which deck gold (p.10 Making Choices)");
            Assert.That(choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.Id(Butterfly), stage.Id(Tiger) }), "every gold card in his deck, and only those");

            stage.ChooseCard(Tiger);

            Assert.That(stage.Card(Bee).State.IsInitialInDeck, Is.True, "the card that started the action is in the deck");
            Assert.That(stage.Card(Tiger).State, Is.EqualTo(Resolved),
                "the gold card that took its states is played, and resolves when the action completes (p.7, TK-Q2)");
            Assert.That(stage.Card(Butterfly).State.IsInitialInDeck, Is.True, "the other deck gold is untouched");
            Assert.That(stage.SlotCard(1), Is.EqualTo(stage.Id(Tiger)), "the turn slot names the swapped-in card");
            Assert.That(stage.FinishedCards, Is.EqualTo(new[] { stage.Id(Tiger) }),
                "the Initiative's selected card followed the swap, so the order closed the turn on it");
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Enemy), "and the order moved on");
        }

        /// <summary>
        /// Milestone 9.32b's regression on 9.32a: the turn's revealed record (what the HUD shows as the
        /// card a hero played) was left naming the card that went to the deck.
        /// </summary>
        [Test]
        public void AfterTheResolvingSwap_TheRevealedRecordNamesTheSwappedInCard()
        {
            var stage = new Stage(played: Bee, deck: new[] { Butterfly, Tiger });
            Assert.That(stage.Round.CurrentCommitmentAuthority.GetRevealedCardInstanceId(Stage.Taka),
                Is.EqualTo(stage.Id(Bee)));
            stage.AttackTheEnemy();
            stage.EnemyDefendsWithTheWall();
            stage.ChooseCard(Tiger);
            Assert.That(stage.Round.CurrentCommitmentAuthority.GetRevealedCardInstanceId(Stage.Taka),
                Is.EqualTo(stage.Id(Tiger)), "the card played now, as the turn slot names it (TK-Q2)");
            Assert.That(stage.Restore(stage.Snapshot()).Round.CurrentCommitmentAuthority
                .GetRevealedCardInstanceId(Stage.Taka), Is.EqualTo(stage.Id(Tiger)));
        }

        [Test]
        public void AfterASwap_OnlyTheSwappedInCardIsThePlayedCard_TkQ2()
        {
            var stage = new Stage(played: Bee, deck: new[] { Butterfly, Tiger });
            Assert.That(stage.PlayedAnAttackCard, Is.True, "before the swap, Bee is the played Attack card");
            stage.AttackTheEnemy();
            stage.EnemyDefendsWithTheWall();
            stage.ChooseCard(Butterfly);

            Assert.That(stage.Card(Butterfly).State, Is.EqualTo(Resolved));
            Assert.That(stage.PlayedAnAttackCard, Is.False,
                "TK-Q2 (the user): the played card is now Butterfly, a Movement card, so \"who has played an " +
                "attack card this turn\" (Brogan's Shield Bash) finds none");
        }

        [Test]
        public void TheMovementGold_SwapsAfterItsMovement_TkQ1()
        {
            var stage = new Stage(played: Butterfly, deck: new[] { Bee, Tiger });
            stage.StartMoving();
            Assert.That(stage.Turn.PendingMovementChoice, Is.Not.Null, "the Movement comes first (TK-Q1)");
            Assert.That(stage.Choice, Is.Null, "no swap is asked before the move");
            Assert.That(stage.Turn.PendingMovementChoice.RemainingAllowance, Is.EqualTo(SyntheticGoldSwapHand.ButterflyMovement),
                "the move is the printed Movement of the card that started it");
            stage.MoveTo(new HexCoordinate(0, -1));
            stage.StopMovingIfAsked();

            Assert.That(stage.Choice, Is.Not.Null, "then \"Swap this card with a different gold card in your deck\"");
            stage.ChooseCard(Bee);
            Assert.That(stage.Card(Butterfly).State.IsInitialInDeck, Is.True);
            Assert.That(stage.Card(Bee).State, Is.EqualTo(Resolved));
            Assert.That(stage.SlotCard(1), Is.EqualTo(stage.Id(Bee)));
        }

        [Test]
        public void TheMovementGold_SwapsAfterAMoveOfZero_TkQ1()
        {
            var stage = new Stage(played: Butterfly, deck: new[] { Bee, Tiger });
            stage.StartMoving();
            stage.StopMovingIfAsked();
            Assert.That(stage.Choice, Is.Not.Null, "W5: under \"up to\", moving 0 spaces is moving");
            stage.ChooseCard(Tiger);
            Assert.That(stage.Card(Tiger).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void WithNoGoldInTheDeck_TheSwapDoesNothing_AndTheCardResolvesItself()
        {
            var stage = new Stage(played: Bee, hand: new[] { Butterfly, Tiger });
            stage.AttackTheEnemy();
            stage.EnemyDefendsWithTheWall();
            Assert.That(stage.Choice, Is.Null, "\"a different gold card in your deck\" names none (as after Ready for War)");
            Assert.That(stage.Card(Bee).State, Is.EqualTo(Resolved));
            Assert.That(stage.SlotCard(1), Is.EqualTo(stage.Id(Bee)));
        }

        [Test]
        public void TheSwapChoice_SurvivesARestore_AndFinishesInTheSameBytes()
        {
            var stage = new Stage(played: Bee, deck: new[] { Butterfly, Tiger });
            stage.AttackTheEnemy();
            stage.EnemyDefendsWithTheWall();
            var atChoice = stage.Snapshot();
            var restored = stage.Restore(atChoice);
            Assert.That(restored.Choice?.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.Id(Butterfly), stage.Id(Tiger) }));
            restored.ChooseCard(Tiger);
            stage.ChooseCard(Tiger);
            Assert.That(restored.Snapshot(), Is.EqualTo(stage.Snapshot()),
                "the restored swap finishes in the same authoritative bytes");
            var afterSwap = stage.Restore(stage.Snapshot());
            Assert.That(afterSwap.Card(Tiger).State, Is.EqualTo(Resolved));
            Assert.That(afterSwap.SlotCard(1), Is.EqualTo(stage.Id(Tiger)), "the swapped slot is restored as it was");
        }

        // -------------------------------------------------------------------------------------------
        // Bushido (lesson 28: hand, resolved and discarded with deck)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void Bushido_SwapsAGoldInTheHand_ForAFaceupDeckGoldInTheHand()
        {
            var stage = new Stage(played: Bushido, hand: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            stage.StartSkill();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.Id(Bee), stage.Id(Tiger) }));
            stage.ChooseCard(Bee);
            Assert.That(stage.Card(Bee).State.IsInitialInHand, Is.True,
                "only a resolved or discarded gold card turns the new card facedown");
            Assert.That(stage.Card(Butterfly).State.IsInitialInDeck, Is.True);
            Assert.That(stage.SlotCard(1), Is.EqualTo(stage.Id(Bushido)), "Bushido's own slot is Bushido's");
        }

        [Test]
        public void Bushido_SwapsAResolvedGold_AndTheNewCardIsResolvedFacedownInItsSlot()
        {
            var stage = new Stage(played: Butterfly, deck: new[] { Bee, Tiger }, next: Bushido);
            stage.StartMoving();
            stage.StopMovingIfAsked();
            stage.ChooseCard(Bee);
            Assert.That(stage.Card(Bee).State, Is.EqualTo(Resolved), "turn 1 leaves a resolved gold card in slot 1");

            stage.NextTurn();
            stage.StartSkill();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.Id(Butterfly), stage.Id(Tiger) }));
            stage.ChooseCard(Tiger);

            Assert.That(stage.Card(Bee).State.IsInitialInDeck, Is.True);
            Assert.That(stage.Card(Tiger).State, Is.EqualTo(new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceDown)),
                "\"if you swap a resolved ... gold card this way, the new card is facedown\"");
            Assert.That(CardLifecycleAuthority.IsFacedownRestingCard(stage.Card(Tiger)), Is.True);
            Assert.That(stage.SlotCard(1), Is.EqualTo(stage.Id(Tiger)), "slot 1 names the facedown card");
            Assert.That(stage.SlotCard(2), Is.EqualTo(stage.Id(Bushido)));
            Assert.That(PublicCardZoneQuery.IsPubliclyVisible(stage.Card(Tiger)), Is.False,
                "a facedown card is hidden information (p.4)");
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(Stage.Taka, 1, 1, stage.Round.TurnSlots,
                    stage.Definitions, stage.Cards), Is.False,
                "a facedown card \"loses its type\": slot 1 holds no Attack card");
        }

        [Test]
        public void Bushido_SwapsADiscardedGold_AndTheNewCardIsDiscardedFacedown()
        {
            var stage = new Stage(played: Bushido, discarded: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            stage.StartSkill();
            stage.ChooseCard(Bee);
            Assert.That(stage.Card(Butterfly).State.IsInitialInDeck, Is.True);
            Assert.That(stage.Card(Bee).State, Is.EqualTo(new CardStateFacts(
                    CardLocation.DiscardPile, false, CardResolutionStatus.None, CardFaceOrientation.FaceDown)),
                "\"if you swap a ... discarded gold card this way, the new card is facedown\"");
            Assert.That(HeroDiscardPileQuery.HasAnyCard(stage.Cards, Stage.Taka), Is.True,
                "a facedown discarded card is still a card in the discard");
        }

        // -------------------------------------------------------------------------------------------
        // Privacy (lesson 29): a facedown card is named to its owner's seat alone
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AFacedownSlotCard_IsWithheldFromEveryOtherSeat_AndNamedToItsOwner()
        {
            var stage = StageAFacedownSlot();
            var slots = stage.Round.TurnSlots;
            Assert.That(slots.Single(slot => slot.TurnIndex == 1).Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).IsFacedown, Is.True,
                "the authority's slot records that its card was placed facedown");

            var participants = StagedBoardParticipants.Create();
            var owner = Seat(Stage.Taka);
            var enemy = Seat(Stage.Enemy);
            Assert.That(GuardsDigital.Application.FacedownSlotMask.Mask(slots, owner, stage.Cards, participants),
                Is.SameAs(slots), "the owner's seat reads its own facedown card");

            var masked = GuardsDigital.Application.FacedownSlotMask.Mask(slots, enemy, stage.Cards, participants);
            var withheld = masked.Single(slot => slot.TurnIndex == 1).Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka));
            Assert.That(withheld.IsWithheld, Is.True, "another seat sees that a card is there");
            Assert.That(withheld.CardInstanceId, Is.Null, "and not which card: an instance id names its definition");
            Assert.That(masked.Single(slot => slot.TurnIndex == 2).Assignments
                    .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).CardInstanceId,
                Is.EqualTo(stage.Id(Bushido)), "a faceup slot card is public (p.4)");
            Assert.That(masked.Single(slot => slot.TurnIndex == 1).Assignments
                    .Single(assignment => assignment.HeroId.Equals(Stage.Enemy)).CardInstanceId.HasValue, Is.True);
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(Stage.Taka, 1, 1, masked, stage.Definitions, stage.Cards),
                Is.False, "a reader of the masked view finds no card to read");

            var unseated = GuardsDigital.Application.FacedownSlotMask.Mask(slots, null, stage.Cards, participants);
            Assert.That(unseated.Single(slot => slot.TurnIndex == 1).Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).IsWithheld, Is.True,
                "a view with no seat owns no hero, so it is withheld there too (fail closed)");
        }

        [Test]
        public void TheCompletedRoundHistory_WithholdsTheFacedownSlotCard_AfterTheCardIsBackInHand()
        {
            var stage = StageAFacedownSlot();
            var participants = StagedBoardParticipants.Create();
            var turnOne = stage.Round.TurnSlots.Single(slot => slot.TurnIndex == 1);
            var round = new CompletedRoundRecord(
                1,
                Enumerable.Range(1, RoundResolutionAuthority.TurnsPerRound).Select(turn =>
                    new RoundTurnSlot(1, turn, turnOne.Assignments, stage.Cards, participants)),
                Enum.GetValues(typeof(EndOfRoundStage)).Cast<EndOfRoundStage>()
                    .Where(value => value != EndOfRoundStage.Complete)
                    .Select(value => new EndOfRoundStageOutcome(value, EndOfRoundStageOutcomeKind.Completed)));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(stage.Card(Tiger)), Is.True,
                "p.8: the card is back in the hand, faceup, and its own state no longer says where it was");

            var rounds = new[] { round };
            var masked = GuardsDigital.Application.FacedownSlotMask.MaskRounds(rounds, Seat(Stage.Enemy), stage.Cards, participants);
            Assert.That(masked.Single().TurnSlots.All(slot => slot.Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).IsWithheld), Is.True,
                "the slot's own record keeps it hidden in the history");
            Assert.That(GuardsDigital.Application.FacedownSlotMask.MaskRounds(rounds, Seat(Stage.Taka), stage.Cards, participants),
                Is.SameAs(rounds));
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 9.32g: every seat sees that a facedown card is a gold (the user's 2026-10-02 ruling)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AWithheldSlotCard_CarriesItsKnownColour_AndTheHistoryKeepsIt()
        {
            var stage = StageAFacedownSlot();
            var participants = StagedBoardParticipants.Create();
            var masked = GuardsDigital.Application.FacedownSlotMask.Mask(
                stage.Round.TurnSlots, Seat(Stage.Enemy), stage.Cards, participants, stage.Definitions);
            var withheld = masked.Single(slot => slot.TurnIndex == 1).Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka));
            Assert.That(withheld.IsWithheld, Is.True, "still not which card");
            Assert.That(withheld.KnownColor, Is.EqualTo(CardColor.Gold), "but every seat knows it is a gold");
            Assert.That(masked.Single(slot => slot.TurnIndex == 2).Assignments
                .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).KnownColor, Is.EqualTo(CardColor.None),
                "a faceup card names itself and carries no known colour");

            var turnOne = stage.Round.TurnSlots.Single(slot => slot.TurnIndex == 1);
            var round = new CompletedRoundRecord(
                1,
                Enumerable.Range(1, RoundResolutionAuthority.TurnsPerRound).Select(turn =>
                    new RoundTurnSlot(1, turn, turnOne.Assignments, stage.Cards, participants)),
                Enum.GetValues(typeof(EndOfRoundStage)).Cast<EndOfRoundStage>()
                    .Where(value => value != EndOfRoundStage.Complete)
                    .Select(value => new EndOfRoundStageOutcome(value, EndOfRoundStageOutcomeKind.Completed)));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(stage.Card(Tiger)), Is.True);
            var history = GuardsDigital.Application.FacedownSlotMask.MaskRounds(
                new[] { round }, Seat(Stage.Enemy), stage.Cards, participants, stage.Definitions);
            Assert.That(history.Single().TurnSlots.All(slot => slot.Assignments
                    .Single(assignment => assignment.HeroId.Equals(Stage.Taka)).KnownColor == CardColor.Gold),
                Is.True, "the slot's facedown record keeps its colour after p.8 returns the card faceup");
        }

        [Test]
        public void AFacedownDiscard_IsCountedByItsKnownColour_ForEverySeat()
        {
            var stage = new Stage(played: Bushido, discarded: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            stage.StartSkill();
            stage.ChooseCard(Bee);
            var summary = GuardsDigital.Application.SeatHandSummary.Derive(
                    StagedBoardParticipants.Create(), stage.Cards, stage.Definitions)
                .Single(row => row.HeroId.Equals(Stage.Taka));
            Assert.That(summary.FacedownDiscardColors, Is.EqualTo(new[] { CardColor.Gold }),
                "the table-public summary says the pile holds one facedown gold, naming no card");
            var enemyView = GuardsDigital.Application.SeatHandSummary.Derive(
                StagedBoardParticipants.Create(), stage.Cards, stage.Definitions);
            Assert.That(enemyView.Single(row => row.HeroId.Equals(Stage.Enemy)).FacedownDiscardColors, Is.Empty);
        }

        [Test]
        public void TheWire_RefusesAKnownColourOnANamedRow_AndAcceptsItOnAWithheldOne()
        {
            bool Read(int color, string cardId, bool facedown)
            {
                using (var buffer = new System.IO.MemoryStream())
                {
                    using (var writer = new System.IO.BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
                        writer.Write(color);
                    buffer.Position = 0;
                    using (var reader = new System.IO.BinaryReader(buffer))
                        return GuardsDigital.Application.NetworkSeatProjectionCodec.TryReadKnownColor(
                            reader, cardId, facedown, out _);
                }
            }

            Assert.That(Read((int)CardColor.Gold, null, true), Is.True, "a withheld facedown row");
            Assert.That(Read((int)CardColor.Gold, "taka-tiger", true), Is.False, "a named row names its card");
            Assert.That(Read((int)CardColor.Gold, null, false), Is.False, "only a facedown row is withheld");
            Assert.That(Read((int)CardColor.Purple, null, true), Is.False, "Purple prints on no hand card");
            Assert.That(Read(99, null, true), Is.False);
            Assert.That(Read((int)CardColor.None, "taka-tiger", false), Is.True);
        }

        [Test]
        public void TheFacedownSlotFact_SurvivesARestore()
        {
            var stage = StageAFacedownSlot();
            var restored = stage.Restore(stage.Snapshot());
            var assignment = restored.Round.TurnSlots.Single(slot => slot.TurnIndex == 1).Assignments
                .Single(value => value.HeroId.Equals(Stage.Taka));
            Assert.That(assignment.IsFacedown, Is.True, "schema v54 persists the slot's facedown fact");
            Assert.That(assignment.CardInstanceId, Is.EqualTo(stage.Id(Tiger)));
            Assert.That(restored.Snapshot(), Is.EqualTo(stage.Snapshot()));
        }

        [Test]
        public void ABorrow_NeverPerformsOrReadsAFacedownCard_Lesson29()
        {
            var stage = StageAFacedownSlot();
            var tiger = stage.Id(Tiger);
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            Assert.That(BorrowedPrimaryActionQuery.EligibleCards(
                    BorrowedPrimaryActionDeclaration.FromPreviousTurnSlotCard(), Stage.Taka, stage.Definitions,
                    stage.Cards, Array.Empty<ActiveEffectInstance>(), catalog, resolvedOrder: new[] { tiger }),
                Is.Empty, "the card in the previous turn slot is facedown, so it has no action to perform (p.10)");
            Assert.That(BorrowedPrimaryActionQuery.EligibleCards(
                    BorrowedPrimaryActionDeclaration.FromRightmostResolvedCard(), Stage.Taka, stage.Definitions,
                    stage.Cards, Array.Empty<ActiveEffectInstance>(), catalog, resolvedOrder: new[] { tiger }),
                Is.Empty, "nor as the rightmost resolved card (Swift's Reload!)");
            Assert.That(BorrowedCardTextDeclaration.EligibleCards(
                    BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.None), Stage.Taka,
                    stage.Definitions, stage.Cards),
                Has.No.Member(stage.Card(Tiger)), "and its text cannot be borrowed either");

            var discarded = new Stage(played: Bushido, discarded: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            discarded.StartSkill();
            discarded.ChooseCard(Bee);
            Assert.That(BorrowedPrimaryActionQuery.EligibleCards(
                    BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None), Stage.Taka,
                    discarded.Definitions, discarded.Cards, Array.Empty<ActiveEffectInstance>(), catalog),
                Has.No.Member(discarded.Card(Bee)), "a facedown discarded card has no action to borrow");
        }

        /// <summary>Turn 1: Butterfly moves and swaps for Bee; turn 2: Bushido swaps the resolved Bee for Tiger, facedown.</summary>
        private static Stage StageAFacedownSlot()
        {
            var stage = new Stage(played: Butterfly, deck: new[] { Bee, Tiger }, next: Bushido);
            stage.StartMoving();
            stage.StopMovingIfAsked();
            stage.ChooseCard(Bee);
            stage.NextTurn();
            stage.StartSkill();
            stage.ChooseCard(Tiger);
            Assert.That(CardLifecycleAuthority.IsFacedownRestingCard(stage.Card(Tiger)), Is.True);
            return stage;
        }

        private static GuardsDigital.Application.MatchSeat Seat(HeroId hero) =>
            new GuardsDigital.Application.MatchSeat(
                new GuardsDigital.Application.SeatId("seat-" + hero.Value),
                new PlayerId(hero.Equals(Stage.Taka)
                    ? StagedBoardParticipants.RedPlayerIdValue
                    : StagedBoardParticipants.BluePlayerIdValue),
                hero,
                new TeamId(hero.Equals(Stage.Taka)
                    ? StagedBoardParticipants.RedTeamIdValue
                    : StagedBoardParticipants.BlueTeamIdValue),
                hero.Equals(Stage.Taka) ? TeamSide.Red : TeamSide.Blue);

        // -------------------------------------------------------------------------------------------
        // The lifecycle transition alone
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheTransition_RefusesAFacedownCard_ACardNotInTheDeck_AndAnotherOwnersCard()
        {
            var owner = Stage.Taka;
            var other = Stage.Enemy;
            var definition = SyntheticGoldSwapHand.Definitions[0].Reference;
            CardInstance InHand(string id, HeroId hero) => CardInstance.CreateInHand(new CardInstanceId(id), definition, hero);
            CardInstance InDeck(string id, HeroId hero) => CardInstance.CreateInDeck(new CardInstanceId(id), definition, hero);

            var facedown = InHand("facedown", owner);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(facedown), Is.True);
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(InHand("a", owner), InDeck("b", owner), true), Is.True);
            var turnedFacedown = InHand("c", owner);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(turnedFacedown), Is.True);
            var incoming = InDeck("d", owner);
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(turnedFacedown, incoming, true), Is.True);
            Assert.That(incoming.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(incoming, InDeck("e", owner), true), Is.False,
                "a facedown card has lost its colour (p.10), so no swap can name it");
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(InHand("f", owner), InHand("g", owner), false), Is.False,
                "the incoming card must be in the deck");
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(InHand("h", owner), InDeck("i", other), false), Is.False,
                "both cards are the owner's");
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(incoming), Is.True,
                "p.8's retrieval returns a facedown discarded card to the hand");
            Assert.That(incoming.State.IsInitialInHand, Is.True, "faceup again: \"until it changes state to in hand\"");
        }

        private static bool IsOf(CardInstance card, CardDefinitionId definition) =>
            card.DefinitionReference.DefinitionId.Equals(definition);

        // -------------------------------------------------------------------------------------------
        // A staged two-hero round
        // -------------------------------------------------------------------------------------------

        internal sealed class Stage
        {
            internal static readonly HeroId Taka = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            private static readonly CardInstanceId EnemyPlayed = new CardInstanceId("enemy-played");
            private static readonly CardInstanceId EnemyNext = new CardInstanceId("enemy-next");
            private static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");
            private const string Prefix = "taka";

            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();
            private readonly MatchContentCatalogs catalogs;
            private readonly CardDefinitionId next;

            internal Stage(
                CardDefinitionId played,
                IEnumerable<CardDefinitionId> hand = null,
                IEnumerable<CardDefinitionId> deck = null,
                IEnumerable<CardDefinitionId> discarded = null,
                CardDefinitionId next = default)
            {
                this.next = next;
                catalogs = CreateCatalogs();
                Definitions = SyntheticGoldSwapHand.Definitions
                    .Concat(new[] { EnemyHold, EnemyWallDefinition })
                    .ToArray();
                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(EnemyPlayed, EnemyHold.Reference, Enemy),
                    CardInstance.CreateInHand(EnemyNext, EnemyHold.Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, EnemyWallDefinition.Reference, Enemy)
                };
                CardInstance Create(CardDefinitionId definition, bool inDeck) => inDeck
                    ? CardInstance.CreateInDeck(Id(definition), DefinitionOf(definition).Reference, Taka)
                    : CardInstance.CreateInHand(Id(definition), DefinitionOf(definition).Reference, Taka);
                cards.Add(Create(played, false));
                if (next.IsDefined)
                    cards.Add(Create(next, false));
                foreach (var definition in hand ?? Enumerable.Empty<CardDefinitionId>())
                    cards.Add(Create(definition, false));
                foreach (var definition in deck ?? Enumerable.Empty<CardDefinitionId>())
                    cards.Add(Create(definition, true));
                foreach (var definition in discarded ?? Enumerable.Empty<CardDefinitionId>())
                {
                    var card = Create(definition, false);
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True, "staged by p.10's own discard");
                    cards.Add(card);
                }

                Cards = cards.ToArray();
                var spaces = (from q in Enumerable.Range(-4, 9)
                              from r in Enumerable.Range(-4, 9)
                              where Math.Abs(q + r) <= 4
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("gold-swap"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Taka, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(1, 0))
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, StagedBoardParticipants.Create(), new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                Accept(Round.TryCommitCard(Taka, Id(played)));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Taka));
            }

            private Stage(Stage template, string snapshot)
            {
                catalogs = template.catalogs;
                next = template.next;
                Definitions = template.Definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, Definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }

            private static MatchContentCatalogs CreateCatalogs()
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(SyntheticGoldSwapHand.Handlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;

            internal CardInstanceId Id(CardDefinitionId definition) => SyntheticGoldSwapHand.Instance(Prefix, definition);
            internal CardInstance Card(CardDefinitionId definition) => Cards.Single(card => card.Id.Equals(Id(definition)));

            internal CardInstanceId SlotCard(int turn) =>
                Round.TurnSlots.Single(slot => slot.RoundIndex == 1 && slot.TurnIndex == turn)
                    .Assignments.Single(assignment => assignment.HeroId.Equals(Taka)).CardInstanceId.Value;

            internal bool PlayedAnAttackCard => PlayedAttackCardThisTurnQuery.HasPlayed(
                Taka, 1, Round.TurnIndex, Round.TurnSlots, Definitions, Cards);

            /// <summary>Every card the Initiative order recorded as finishing its action, in order.</summary>
            internal IReadOnlyList<CardInstanceId> FinishedCards =>
                events.Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                        value.TurnEvent.Kind == TurnResolutionEventKind.InitiativeProgressed &&
                        value.TurnEvent.InitiativeEvent.Kind == InitiativeResolutionEventKind.SelectedActorFinished)
                    .Select(value => value.TurnEvent.InitiativeEvent.CardInstanceId)
                    .ToArray();

            internal Stage Restore(string snapshot) => new Stage(this, snapshot);
            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal void AttackTheEnemy()
            {
                Accept(Round.TryStartAttack(new ActionResolutionId("attack"), Offer(ActionKind.Attack)));
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "the Attack asks for its target");
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
            }

            internal void EnemyDefendsWithTheWall()
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null);
                Accept(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, EnemyWall)));
            }

            internal void StartMoving() =>
                Accept(Round.TryStartMovement(new ActionResolutionId("move"), Offer(ActionKind.Movement)));

            internal void StartSkill() =>
                Accept(Round.TryStartSkill(new ActionResolutionId("skill-" + Round.TurnIndex), Offer(ActionKind.Skill)));

            internal void MoveTo(HexCoordinate space)
            {
                var move = Turn.PendingMovementChoice;
                Accept(Round.TryAnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                    move.ResolutionId, move.ChoiceId, space)));
            }

            internal void StopMovingIfAsked()
            {
                var move = Turn.PendingMovementChoice;
                if (move != null)
                {
                    Accept(Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(move.ResolutionId, move.ChoiceId)));
                }
            }

            internal void ChooseCard(CardDefinitionId definition)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.CardInstanceId.Equals(Id(definition)))));
            }

            internal void ReleaseIfAsked()
            {
                if (Turn != null && Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            /// <summary>Takahide's turn ends, the enemy Holds, and turn 2 is committed.</summary>
            internal void NextTurn()
            {
                ReleaseIfAsked();
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
                Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                ReleaseIfAsked();
                Accept(Round.TryCommitCard(Taka, Id(next)));
                Accept(Round.TryCommitCard(Enemy, EnemyNext));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Taka));
            }

            private ActionOffer Offer(ActionKind kind)
            {
                var selected = Turn.SelectedActor.CardInstanceId;
                return Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(selected) &&
                    offer.ActionDefinitionId.Equals(SyntheticGoldSwapHand.PrimaryId) &&
                    Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind == kind);
            }

            private CardDefinition DefinitionOf(CardDefinitionId id) => Definitions.Single(value => value.Id.Equals(id));

            /// <summary>Milestone 9.32g. Every round event this stage's accepted commands produced.</summary>
            internal IReadOnlyList<RoundResolutionEvent> Events => events;

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            private static readonly CardDefinition EnemyHold = new CardDefinition(
                new CardDefinitionId("gold-swap-enemy-hold"), "rev-1", "TEST - Hold",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition EnemyWallDefinition = new CardDefinition(
                new CardDefinitionId("gold-swap-enemy-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 12) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });
        }
    }
}
