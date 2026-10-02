using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32f: Ready for War, Takahide's Ultimate - "Return your silver card to your deck and
    /// take two gold cards from your deck into your hand" - performed once by the level-up that unlocks
    /// it (TK-Q5), and the six-card hand it leaves: committed from, carried through a snapshot, and
    /// the gold swaps finding no deck gold afterwards ([his shard](Docs/AI/Rules/HERO_TAKAHIDE.md)).
    /// </summary>
    public sealed class TakahideReadyForWarTests
    {
        private static readonly HeroId Taka = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [Test]
        public void ReadyForWar_PrintsAnUltimateWhoseWholeTextIsTheUnlockMovement()
        {
            var card = TakahideCardContent.ReadyForWarDefinition;
            Assert.That(card.Progression.IsUltimate, Is.True);
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.IV));
            Assert.That(card.ActionDefinitions, Is.Empty);
            var movement = card.UltimateRule.UnlockCardMovement;
            Assert.That(movement.ReturnedToDeckColor, Is.EqualTo(CardColor.Silver), "\"your silver card\"");
            Assert.That(movement.TakenFromDeckColor, Is.EqualTo(CardColor.Gold));
            Assert.That(movement.TakenFromDeckCount, Is.EqualTo(2), "\"two gold cards\"");
            Assert.That(card.UltimateRule.Modifications, Is.Empty, "no permanent ability: nothing applies while unlocked");
            Assert.That(card.UltimateRule.TriggeredEffect.HasValue, Is.False);
        }

        [Test]
        public void TheUnlock_ReturnsBushidoToTheDeck_AndTakesBothDeckGolds_TkQ5()
        {
            var fixture = new LevelUpFixture(takaCoins: 1, enemyCoins: 0);
            var result = fixture.Begin();
            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Transitions.IsRunning, Is.False, "the unlock asks nobody anything");
            Assert.That(fixture.Outcomes.Single(outcome => outcome.Kind == HeroLevelUpOutcomeKind.UltimateUnlocked)
                .UltimateCardInstanceId, Is.EqualTo(fixture.Id(TakahideCardContent.ReadyForWar)));
            Assert.That(fixture.Location(TakahideCardContent.ReadyForWar), Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(fixture.Location(TakahideCardContent.Bushido), Is.EqualTo(CardLocation.Deck),
                "\"Return your silver card to your deck\"");
            Assert.That(fixture.HandOf(Taka), Is.EquivalentTo(new[]
            {
                TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.StingLikeABee,
                TakahideCardContent.StrikeLikeATiger, TakahideCardContent.HoldMySake,
                TakahideCardContent.TacticalGambit, TakahideCardContent.LoyalRetainer
            }), "\"(You now have a total hand size of 6 cards.)\"");
            Assert.That(fixture.Cards.Where(card => card.State.Location == CardLocation.Hand)
                .All(card => card.State.FaceOrientation == CardFaceOrientation.FaceUp), Is.True);

            fixture.AwardCoins(Taka, 20);
            Assert.That(fixture.Begin("round-2").TransitionCompleted, Is.True);
            Assert.That(fixture.HandOf(Taka).Count, Is.EqualTo(6), "once, at the unlock: nothing re-applies (TK-Q5)");
            Assert.That(fixture.Location(TakahideCardContent.Bushido), Is.EqualTo(CardLocation.Deck));
        }

        [Test]
        public void TheUnlock_ReportsItsMoves_AndTheLogNamesThemToTakahidesSeatAlone()
        {
            var fixture = new LevelUpFixture(takaCoins: 1, enemyCoins: 0);
            Assert.That(fixture.Begin().Accepted, Is.True);
            var unlocked = fixture.Outcomes.Single(outcome => outcome.Kind == HeroLevelUpOutcomeKind.UltimateUnlocked);
            Assert.That(unlocked.CardMoves, Is.EqualTo(new[]
            {
                new CardMoveFact(TakahideCardContent.Bushido, CardMoveZone.Hand, CardMoveZone.Deck,
                    knownColor: CardColor.Silver),
                new CardMoveFact(TakahideCardContent.StingLikeABee, CardMoveZone.Deck, CardMoveZone.Hand,
                    knownColor: CardColor.Gold),
                new CardMoveFact(TakahideCardContent.StrikeLikeATiger, CardMoveZone.Deck, CardMoveZone.Hand,
                    knownColor: CardColor.Gold)
            }), "9.32g (mockup F): Bushido to the deck, then the two golds to the hand");

            var log = TakahideTableFactsTests.Log(new[] { RoundResolutionEvent.HeroLevelUpProgressed(1, 1, unlocked) });
            var row = log.Single(entry => entry.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.UltimateMovedCards);
            Assert.That(row.SubjectHeroId, Is.EqualTo(Taka));
            Assert.That(row.SubjectCardInstanceId, Is.EqualTo(fixture.Id(TakahideCardContent.ReadyForWar)));
            Assert.That(row.CardMoves, Is.EqualTo(unlocked.CardMoves));
            Assert.That(GuardsDigital.Application.CardMoveMask.Mask(log,
                    TakahideTableFactsTests.Seat(Taka, TeamSide.Blue)).Single().CardMoves,
                Is.EqualTo(unlocked.CardMoves), "his own seat names all three");
            Assert.That(GuardsDigital.Application.CardMoveMask.Mask(log,
                    TakahideTableFactsTests.Seat(Enemy, TeamSide.Red)).Single().CardMoves,
                Is.EqualTo(unlocked.CardMoves.Select(move => move.Unnamed())),
                "another seat reads the silver card to the deck and two gold cards to hand");
        }

        [Test]
        public void TheUnlock_DuringSimultaneousLeveling_MovesTheCardsWhileAnotherHeroStillUpgrades()
        {
            var fixture = new LevelUpFixture(takaCoins: 1, enemyCoins: 1);
            fixture.Begin();
            Assert.That(fixture.Transitions.IsRunning, Is.True, "the enemy's Upgrade a Card step is open");
            Assert.That(fixture.Location(TakahideCardContent.ReadyForWar), Is.EqualTo(CardLocation.UltimateArea),
                "p.15 levels every hero at once (8.35u): Takahide's unlock does not wait for the enemy's answer");
            Assert.That(fixture.HandOf(Taka).Count, Is.EqualTo(6));
            fixture.AnswerFirstOption();
            Assert.That(fixture.Transitions.IsRunning, Is.False);
            Assert.That(fixture.HandOf(Taka).Count, Is.EqualTo(6));
            Assert.That(fixture.HandOf(Enemy).Count, Is.EqualTo(1), "the enemy's upgrade completed beside it");
        }

        [Test]
        public void TheSixCardHand_CommitsAnyOfItsCards_AndSurvivesASnapshotRoundTrip()
        {
            var fixture = new LevelUpFixture(takaCoins: 1, enemyCoins: 0);
            fixture.Begin();
            var round = new SixCardRound(fixture);
            Assert.That(round.Accept(round.Round.TryCommitCard(Taka, fixture.Id(TakahideCardContent.StrikeLikeATiger))),
                Is.True, "the sixth card is as committable as the first");
            var snapshot = round.Snapshot();
            var restored = round.Restore(snapshot);
            Assert.That(restored.Snapshot(), Is.EqualTo(snapshot), "the six-card state restores to identical bytes");
            Assert.That(restored.Cards.Count(card => card.OwnerId.Equals(Taka) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(5), "five still in hand beside the committed sixth");
            Assert.That(restored.Cards.Single(card => card.Id.Equals(fixture.Id(TakahideCardContent.ReadyForWar))).State.Location,
                Is.EqualTo(CardLocation.UltimateArea));
        }

        [Test]
        public void AfterReadyForWar_AGoldSwapFindsNoDeckGold_AndDoesNothing()
        {
            var stage = new TakahideBasicAndRedFacesTests.Stage(TakahideCardContent.StrikeLikeATiger,
                hand: new[] { TakahideCardContent.StingLikeABee, TakahideCardContent.FloatLikeAButterfly },
                deck: new[] { TakahideCardContent.Bushido });
            stage.AttackTheEnemyAndDefend();
            Assert.That(stage.Choice, Is.Null, "\"swap with a different gold card in your deck\": there is none");
            Assert.That(stage.Card(TakahideCardContent.StrikeLikeATiger).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved), "the Attack resolves; the swap is if able");
            Assert.That(stage.Card(TakahideCardContent.StingLikeABee).State.IsInitialInHand, Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Takahide with every non-basic card at Tier III, one gold and Bushido in hand, the other two
        /// golds and Ready for War in the deck - the hand p.8's Retrieve Cards leaves before p.15 levels
        /// him. The enemy holds a Tier I Red with its two Tier II faces in the deck.
        /// </summary>
        private sealed class LevelUpFixture
        {
            private readonly List<HeroLevelUpOutcome> outcomes = new List<HeroLevelUpOutcome>();
            private int awardCount;

            internal LevelUpFixture(int takaCoins, int enemyCoins)
            {
                Participants = StagedBoardParticipants.Create();
                Definitions = TakahideCardContent.BasicAndRedDefinitions
                    .Concat(TakahideCardContent.BlueAndGreenDefinitions)
                    .Concat(TakahideCardContent.UltimateDefinitions)
                    .ToArray();
                var cards = new List<CardInstance>();
                void Add(HeroId owner, CardDefinitionId definition, bool inDeck)
                {
                    var reference = Definitions.Single(value => value.Id.Equals(definition)).Reference;
                    var id = new CardInstanceId((owner.Equals(Taka) ? "taka-" : "enemy-") + definition.Value);
                    cards.Add(inDeck
                        ? CardInstance.CreateInDeck(id, reference, owner)
                        : CardInstance.CreateInHand(id, reference, owner));
                }

                foreach (var held in new[]
                {
                    TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.Bushido, TakahideCardContent.HoldMySake,
                    TakahideCardContent.TacticalGambit, TakahideCardContent.LoyalRetainer
                })
                    Add(Taka, held, inDeck: false);
                foreach (var waiting in new[]
                {
                    TakahideCardContent.StingLikeABee, TakahideCardContent.StrikeLikeATiger, TakahideCardContent.ReadyForWar
                })
                    Add(Taka, waiting, inDeck: true);
                Add(Enemy, TakahideCardContent.SetAnExample, inDeck: false);
                Add(Enemy, TakahideCardContent.LeadFromTheFront, inDeck: true);
                Add(Enemy, TakahideCardContent.SpinningBlade, inDeck: true);
                Cards = cards.ToArray();

                Levels = new HeroProgressionState(Participants);
                Items = new HeroItemState(Participants);
                CoinLedger = new HeroCoinLedger(Participants);
                Rules = new HeroLevelUpStepRules(Participants, Definitions, Cards);
                Rules.Bind(Levels, Items, CoinLedger);
                Transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(null, Rules));
                if (takaCoins > 0) AwardCoins(Taka, takaCoins);
                if (enemyCoins > 0) AwardCoins(Enemy, enemyCoins);
            }

            internal MatchParticipants Participants { get; }
            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal HeroProgressionState Levels { get; }
            internal HeroItemState Items { get; }
            internal HeroCoinLedger CoinLedger { get; }
            internal HeroLevelUpStepRules Rules { get; }
            internal SystemTransitionAuthority Transitions { get; }
            internal IReadOnlyList<HeroLevelUpOutcome> Outcomes => outcomes;

            internal CardInstanceId Id(CardDefinitionId definition) => new CardInstanceId("taka-" + definition.Value);

            internal CardLocation Location(CardDefinitionId definition) =>
                Cards.Single(card => card.Id.Equals(Id(definition))).State.Location;

            internal IReadOnlyList<CardDefinitionId> HandOf(HeroId hero) =>
                Cards.Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId).ToArray();

            internal void AwardCoins(HeroId hero, int amount)
            {
                var enemyTeam = Participants.Teams.Single(team =>
                    !team.Id.Equals(Participants.GetTeamForHero(hero).Id));
                var awarded = CoinLedger.Award(new DefeatOutcome(
                    new DefeatOutcomeId("ready-for-war-award-" + ++awardCount),
                    new DefeatTargetId("ready-for-war-target-" + awardCount),
                    enemyTeam.Id, hero, amount, 0));
                Assert.That(awarded.Accepted, Is.True, awarded.RejectionReason.ToString());
            }

            internal SystemTransitionResult Begin(string occasion = "round-1")
            {
                outcomes.Clear();
                var result = Transitions.TryBegin(
                    HeroLevelUpSystemTransition.DefinitionId,
                    new SystemTransitionInstanceId(occasion + "-" + HeroLevelUpSystemTransition.DefinitionId.Value));
                outcomes.AddRange(Rules.DrainOutcomes());
                return result;
            }

            internal void AnswerFirstOption()
            {
                var answered = Transitions.TryAnswer(Transitions.PendingStep.Invocations[0]);
                Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
                outcomes.AddRange(Rules.DrainOutcomes());
            }
        }

        /// <summary>The next round, dealt from the cards the unlock left: commit, snapshot and restore.</summary>
        private sealed class SixCardRound
        {
            private readonly MatchContentCatalogs catalogs;
            private readonly IReadOnlyList<CardDefinition> definitions;

            internal SixCardRound(LevelUpFixture fixture)
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                catalogs = new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(TakahideCardContent.BasicAndRedHandlers())
                        .Concat(TakahideCardContent.BlueAndGreenHandlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
                definitions = fixture.Definitions;
                Cards = fixture.Cards;
                var spaces = (from q in Enumerable.Range(-3, 7)
                              from r in Enumerable.Range(-3, 7)
                              where Math.Abs(q + r) <= 3
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("ready-for-war"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Taka, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(2, 0))
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    definitions, Cards, fixture.Participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
            }

            private SixCardRound(SixCardRound template, string snapshot)
            {
                catalogs = template.catalogs;
                definitions = template.definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }

            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }

            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal SixCardRound Restore(string snapshot) => new SixCardRound(this, snapshot);

            internal bool Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.CommitmentRejectionReason);
                return result.Accepted;
            }
        }
    }
}
