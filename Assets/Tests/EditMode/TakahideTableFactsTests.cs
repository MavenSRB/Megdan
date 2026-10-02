using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using GoldStage = GuardsDigital.Tests.EditMode.TakahideGoldSwapTests.Stage;
using RedStage = GuardsDigital.Tests.EditMode.TakahideBasicAndRedFacesTests.Stage;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32g. The table facts the approved mockups E and F read, below the views: the log
    /// rows Takahide's swaps write and whose seat reads which card (F), and the barred spaces his
    /// enemy-obstacle effects mark (E). Typed and harness-compiled; the words and the views are
    /// <c>TakahidePresentationTests</c>.
    /// </summary>
    public sealed class TakahideTableFactsTests
    {
        private static readonly CardDefinitionId Bee = SyntheticGoldSwapHand.BeeId;
        private static readonly CardDefinitionId Butterfly = SyntheticGoldSwapHand.ButterflyId;
        private static readonly CardDefinitionId Tiger = SyntheticGoldSwapHand.TigerId;
        private static readonly CardDefinitionId Bushido = SyntheticGoldSwapHand.BushidoId;

        // -------------------------------------------------------------------------------------------
        // F - the swap rows
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheGoldSelfSwap_WritesOneRow_NamingBothCardsToEverySeat()
        {
            var stage = new GoldStage(played: Butterfly, deck: new[] { Bee, Tiger });
            stage.StartMoving();
            stage.StopMovingIfAsked();
            stage.ChooseCard(Bee);

            var row = Single(Log(stage.Events), MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(row.SubjectHeroId, Is.EqualTo(GoldStage.Taka), "the row is the cards' owner's");
            Assert.That(row.SubjectCardInstanceId, Is.EqualTo(stage.Id(Butterfly)), "the source is the card itself");
            Assert.That(row.CardMoves, Is.EqualTo(new[]
            {
                new CardMoveFact(Butterfly, CardMoveZone.TurnArea, CardMoveZone.Deck, knownColor: CardColor.Gold),
                new CardMoveFact(Bee, CardMoveZone.Deck, CardMoveZone.TurnArea, knownColor: CardColor.Gold)
            }));

            var log = Log(stage.Events);
            Assert.That(CardMoveMask.Mask(log, Seat(GoldStage.Enemy, TeamSide.Blue)), Is.SameAs(log),
                "both cards were faceup in the turn area at one end of the swap: every seat reads the same log");
        }

        [Test]
        public void Bushido_SwappingTheGoldInHand_NamesBothCardsToItsOwnerAlone()
        {
            var stage = new GoldStage(played: Bushido, hand: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            stage.StartSkill();
            stage.ChooseCard(Bee);

            var log = Log(stage.Events);
            var row = Single(log, MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(row.SubjectCardInstanceId, Is.EqualTo(stage.Id(Bushido)));
            Assert.That(row.CardMoves, Is.EqualTo(new[]
            {
                new CardMoveFact(Butterfly, CardMoveZone.Hand, CardMoveZone.Deck, knownColor: CardColor.Gold),
                new CardMoveFact(Bee, CardMoveZone.Deck, CardMoveZone.Hand, knownColor: CardColor.Gold)
            }));

            Assert.That(Single(CardMoveMask.Mask(log, Seat(GoldStage.Taka, TeamSide.Red)),
                    MatchLogEntryKind.CardSwappedByCardText).CardMoves, Is.EqualTo(row.CardMoves),
                "his own seat names both cards");
            var enemyRow = Single(CardMoveMask.Mask(log, Seat(GoldStage.Enemy, TeamSide.Blue)),
                MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(enemyRow.CardMoves, Is.EqualTo(row.CardMoves.Select(move => move.Unnamed())),
                "another seat reads a gold card from the hand and one from the deck, naming neither");
            Assert.That(enemyRow.SubjectCardInstanceId, Is.EqualTo(row.SubjectCardInstanceId),
                "Bushido itself resolved faceup, so its name stays");
        }

        [Test]
        public void Bushido_OnAResolvedGold_NamesTheOutgoingCard_AndTheFacedownOneToItsOwnerAlone()
        {
            var stage = new GoldStage(played: Butterfly, deck: new[] { Bee, Tiger }, next: Bushido);
            stage.StartMoving();
            stage.StopMovingIfAsked();
            stage.ChooseCard(Bee);
            stage.NextTurn();
            stage.StartSkill();
            stage.ChooseCard(Tiger);

            var log = Log(stage.Events);
            var row = Rows(log, MatchLogEntryKind.CardSwappedByCardText).Last();
            Assert.That(row.CardMoves, Is.EqualTo(new[]
            {
                new CardMoveFact(Bee, CardMoveZone.TurnArea, CardMoveZone.Deck, knownColor: CardColor.Gold),
                new CardMoveFact(Tiger, CardMoveZone.Deck, CardMoveZone.TurnArea, arrivesFacedown: true,
                    knownColor: CardColor.Gold)
            }));
            var enemyRow = Rows(CardMoveMask.Mask(log, Seat(GoldStage.Enemy, TeamSide.Blue)),
                MatchLogEntryKind.CardSwappedByCardText).Last();
            Assert.That(enemyRow.CardMoves[0], Is.EqualTo(row.CardMoves[0]), "the resolved gold was faceup");
            Assert.That(enemyRow.CardMoves[1], Is.EqualTo(row.CardMoves[1].Unnamed()), "the new card is facedown");
        }

        [Test]
        public void Bushido_OnADiscardedGold_NamesTheOutgoingCard_AndTheFacedownOneToItsOwnerAlone()
        {
            var stage = new GoldStage(played: Bushido, discarded: new[] { Butterfly }, deck: new[] { Bee, Tiger });
            stage.StartSkill();
            stage.ChooseCard(Bee);

            var log = Log(stage.Events);
            var row = Single(log, MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(row.CardMoves, Is.EqualTo(new[]
            {
                new CardMoveFact(Butterfly, CardMoveZone.DiscardPile, CardMoveZone.Deck, knownColor: CardColor.Gold),
                new CardMoveFact(Bee, CardMoveZone.Deck, CardMoveZone.DiscardPile, arrivesFacedown: true,
                    knownColor: CardColor.Gold)
            }));
            Assert.That(Single(CardMoveMask.Mask(log, Seat(GoldStage.Enemy, TeamSide.Blue)),
                    MatchLogEntryKind.CardSwappedByCardText).CardMoves,
                Is.EqualTo(new[] { row.CardMoves[0], row.CardMoves[1].Unnamed() }));
        }

        [Test]
        public void ATeammateSwap_IsTheTeammatesRow_NamingBothCardsAndTheSourceToEverySeat()
        {
            var stage = new RedStage(TakahideCardContent.SetAnExample, allyAt: new HexCoordinate(-3, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(RedStage.Ally);
            stage.ChooseCard(stage.AllyHandCard);

            var log = Log(stage.Events);
            var row = Single(log, MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(row.SubjectHeroId, Is.EqualTo(RedStage.Ally), "the teammate's cards moved");
            Assert.That(row.SubjectCardInstanceId, Is.EqualTo(stage.Id(TakahideCardContent.SetAnExample)));
            Assert.That(row.CardMoves.Select(move => (move.From, move.To)), Is.EqualTo(new[]
            {
                (CardMoveZone.TurnArea, CardMoveZone.Hand), (CardMoveZone.Hand, CardMoveZone.TurnArea)
            }));
            Assert.That(row.CardMoves.All(move => move.Definition.IsDefined && move.IsSeenByTheTable()), Is.True,
                "the unresolved card was faceup and the new one is faceup in its slot");
            Assert.That(CardMoveMask.Mask(log, Seat(RedStage.Enemy, TeamSide.Red)), Is.SameAs(log));
        }

        [Test]
        public void HoldMySake_WritesTheSwapFromTheDiscard()
        {
            var stage = new RedStage(TakahideCardContent.HoldMySake, allyAt: new HexCoordinate(-3, 0), allyHandEmpty: true);
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(RedStage.Ally);
            stage.ChooseCard(stage.AllyDiscardedCard);

            var row = Single(Log(stage.Events), MatchLogEntryKind.CardSwappedByCardText);
            Assert.That(row.CardMoves.Select(move => (move.From, move.To)), Is.EqualTo(new[]
            {
                (CardMoveZone.TurnArea, CardMoveZone.DiscardPile), (CardMoveZone.DiscardPile, CardMoveZone.TurnArea)
            }));
        }

        [Test]
        public void ATeammateWhoKeepsTheirCard_WritesAKeptRow_AndNoSwap()
        {
            var stage = new RedStage(TakahideCardContent.SetAnExample, allyAt: new HexCoordinate(-3, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(RedStage.Ally);
            stage.Decline();

            var log = Log(stage.Events);
            Assert.That(Rows(log, MatchLogEntryKind.CardSwappedByCardText), Is.Empty);
            var row = Single(log, MatchLogEntryKind.CardKeptByCardText);
            Assert.That(row.SubjectHeroId, Is.EqualTo(RedStage.Ally));
            Assert.That(row.SubjectCardInstanceId, Is.EqualTo(stage.Id(TakahideCardContent.SetAnExample)));
            Assert.That(row.CardMoves.Single().From, Is.EqualTo(CardMoveZone.TurnArea));
            Assert.That(row.CardMoves.Single().To, Is.EqualTo(CardMoveZone.TurnArea), "the card stayed");
            Assert.That(row.CardMoves.Single().Definition.IsDefined, Is.True);
        }

        [Test]
        public void AGoldTheTableSawGoIntoTheDeck_IsNoLongerNamedInTheHand()
        {
            var definition = SyntheticGoldSwapHand.Definitions.Single(value => value.Id.Equals(Bee));
            var card = CardInstance.CreateInHand(new CardInstanceId("taka-bee"), definition.Reference, GoldStage.Taka);
            var revealed = new MatchLogEntry(1, MatchLogEntryKind.CardRevealed, 1, 1,
                subjectHeroId: GoldStage.Taka, subjectCardInstanceId: card.Id);
            var intoDeck = new MatchLogEntry(2, MatchLogEntryKind.CardSwappedByCardText, 1, 1,
                subjectHeroId: GoldStage.Taka, subjectCardInstanceId: card.Id,
                cardMoves: new[]
                {
                    new CardMoveFact(Bee, CardMoveZone.TurnArea, CardMoveZone.Deck, knownColor: CardColor.Gold),
                    new CardMoveFact(Tiger, CardMoveZone.Deck, CardMoveZone.TurnArea, knownColor: CardColor.Gold)
                });
            var playedAgain = new MatchLogEntry(3, MatchLogEntryKind.CardRevealed, 2, 1,
                subjectHeroId: GoldStage.Taka, subjectCardInstanceId: card.Id);

            Assert.That(RetrievedCardMemory.Remembers(card, new[] { revealed }), Is.True,
                "p.8: a revealed card stays known in the hand it returns to");
            Assert.That(RetrievedCardMemory.Remembers(card, new[] { revealed, intoDeck }), Is.False,
                "once the table saw it go into the deck, a later return to the hand is unseen (p.4)");
            Assert.That(RetrievedCardMemory.Remembers(card, new[] { revealed, intoDeck, playedAgain }), Is.True,
                "played again, it is seen again");
        }

        // -------------------------------------------------------------------------------------------
        // F - the wire and the host file
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheWire_CarriesTheMoves_AndRefusesAMoveNoTextMakes()
        {
            var moves = new[]
            {
                new CardMoveFact(Butterfly, CardMoveZone.Hand, CardMoveZone.Deck, knownColor: CardColor.Gold),
                new CardMoveFact(default, CardMoveZone.Deck, CardMoveZone.DiscardPile, arrivesFacedown: true,
                    knownColor: CardColor.Gold)
            };
            var entry = new MatchLogEntry(4, MatchLogEntryKind.CardSwappedByCardText, 1, 2,
                subjectHeroId: GoldStage.Taka, subjectCardInstanceId: new CardInstanceId("taka-bushido"),
                cardMoves: moves);

            Assert.That(RoundTrip(Bytes(NetworkMatchLogCodec.ToRow(entry))), Is.EqualTo(entry));

            var bytes = Bytes(NetworkMatchLogCodec.ToRow(entry));
            var purple = (byte[])bytes.Clone();
            BitConverter.GetBytes((int)CardColor.Purple).CopyTo(purple, purple.Length - 4);
            Assert.That(RoundTrip(purple), Is.Null, "no text fixes a Purple card");
            var noZone = (byte[])bytes.Clone();
            BitConverter.GetBytes((int)CardMoveZone.None).CopyTo(noZone, noZone.Length - 4 - 2 - 4);
            Assert.That(RoundTrip(noZone), Is.Null, "a move goes somewhere");

            var crowded = new NetworkMatchLogRow(5, (int)MatchLogEntryKind.CardSwappedByCardText, 1, 2,
                GoldStage.Taka.Value, null, null, null, null, false, 0, 0, false, 0, 0, 0, 0,
                cardMoves: Enumerable.Repeat(moves[0], NetworkMatchLogCodec.MaxCardMovesPerRow + 1));
            Assert.That(RoundTrip(Bytes(crowded)), Is.Null, "a row claiming more moves than any text makes");
        }

        [Test]
        public void TheHostFile_SaysWhoSwappedWhat()
        {
            var entry = new MatchLogEntry(4, MatchLogEntryKind.CardSwappedByCardText, 1, 2,
                subjectHeroId: GoldStage.Taka, subjectCardInstanceId: new CardInstanceId("taka-bushido"),
                cardMoves: new[]
                {
                    new CardMoveFact(default, CardMoveZone.Hand, CardMoveZone.Deck, knownColor: CardColor.Gold),
                    new CardMoveFact(default, CardMoveZone.Deck, CardMoveZone.Hand, knownColor: CardColor.Gold)
                });
            Assert.That(GameplayNarrationLog.Describe(entry),
                Does.Contain("swapped a gold card (Hand -> Deck) and a gold card (Deck -> Hand)"));
        }

        // -------------------------------------------------------------------------------------------
        // E - the barred spaces
        // -------------------------------------------------------------------------------------------

        [Test]
        public void SpinningBlade_BarsTheEmptySpacesAdjacentToTakahide_AndOtherEffectsBarNone()
        {
            var catalog = Catalog();
            var anchor = new HexCoordinate(0, 0);
            ActiveEffectVisibilityModel Live(CardDefinition definition)
            {
                var card = CardInstance.CreateInHand(new CardInstanceId("live-" + definition.Id.Value),
                    definition.Reference, GoldStage.Taka);
                return ActiveEffectVisibility.Derive(new[] { card.Id }, new[] { card },
                    TakahideCardContent.BasicAndRedDefinitions, new[] { new HeroOccupancy(GoldStage.Taka, anchor) },
                    Array.Empty<HeroProgressionProjection>(), catalog);
            }

            var blade = Live(TakahideCardContent.SpinningBladeDefinition);
            Assert.That(blade.BarredSpaces, Is.EquivalentTo(blade.AuraSpaces));
            Assert.That(blade.BarredSpaces, Has.Member(new HexCoordinate(1, 0)));
            Assert.That(blade.BarredSpaces.Count, Is.EqualTo(7), "his own space and the six adjacent ones");
            Assert.That(blade.Auras.Single().BarsEnemyUnits, Is.True);

            var helix = Live(TakahideCardContent.BladeHelixDefinition);
            Assert.That(helix.BarredSpaces.Count, Is.EqualTo(7), "Blade Helix's printed Radius 1");

            var setAnExample = Live(TakahideCardContent.SetAnExampleDefinition);
            Assert.That(setAnExample.BarredSpaces, Is.Empty, "a card declaring no such overlay bars nothing");
        }

        // -------------------------------------------------------------------------------------------

        internal static IReadOnlyList<MatchLogEntry> Log(IEnumerable<GuardsDigital.Core.Cards.RoundResolutionEvent> events)
        {
            var log = new RuleEventLogProjection();
            log.Append(events);
            return MatchLogProjection.Derive(log.Entries, int.MaxValue);
        }

        private static MatchLogEntry[] Rows(IEnumerable<MatchLogEntry> log, MatchLogEntryKind kind) =>
            log.Where(entry => entry.Kind == kind).ToArray();

        private static MatchLogEntry Single(IEnumerable<MatchLogEntry> log, MatchLogEntryKind kind)
        {
            var rows = Rows(log, kind);
            Assert.That(rows.Length, Is.EqualTo(1),
                kind + ": the log holds " + string.Join(", ", log.Select(entry => entry.Kind)));
            return rows[0];
        }

        internal static MatchSeat Seat(HeroId hero, TeamSide side) =>
            new MatchSeat(new SeatId("seat-" + hero.Value), new PlayerId("player-" + hero.Value), hero,
                new TeamId(side == TeamSide.Red ? StagedBoardParticipants.RedTeamIdValue : StagedBoardParticipants.BlueTeamIdValue),
                side);

        private static EffectCapabilityCatalog Catalog()
        {
            var defaults = MatchContentCatalogs.ForThisBuild();
            return new EffectCapabilityCatalog(defaults.Capabilities.Registered
                .Select(defaults.Capabilities.Load)
                .Concat(TakahideCardContent.BasicAndRedHandlers()));
        }

        private static byte[] Bytes(NetworkMatchLogRow row)
        {
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, Encoding.UTF8, true))
                    NetworkMatchLogCodec.Write(writer, row);
                return buffer.ToArray();
            }
        }

        private static MatchLogEntry RoundTrip(byte[] bytes)
        {
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
                return NetworkMatchLogCodec.TryRead(reader, out var row) ? NetworkMatchLogCodec.ToEntry(row) : null;
        }
    }
}
