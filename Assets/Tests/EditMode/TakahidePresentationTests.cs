using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;
using Hatch = GuardsDigital.Presentation.Match.HeroOverviewsUnityView.FacedownHatch;
using GoldStage = GuardsDigital.Tests.EditMode.TakahideGoldSwapTests.Stage;
using RedStage = GuardsDigital.Tests.EditMode.TakahideBasicAndRedFacesTests.Stage;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32g. Takahide's presentation to the user's approved mockups (2026-10-02): A the
    /// six-card hand, B the facedown gold every seat sees, C and D the two swap windows, F the log's
    /// words. The facts beneath them are <see cref="TakahideTableFactsTests"/>; the composed screens
    /// are the Play Mode captures.
    /// </summary>
    public sealed class TakahidePresentationTests
    {
        // -------------------------------------------------------------------------------------------
        // F - the log's words, on his seat and on every other
        // -------------------------------------------------------------------------------------------

        private static readonly IReadOnlyList<CardDefinition> Cards = TakahideCardContent.BasicAndRedDefinitions
            .Concat(TakahideCardContent.UltimateDefinitions)
            .Concat(WaspCardContent.AllDefinitions)
            .ToArray();

        private static readonly CardDefinitionId Sting = TakahideCardContent.StingLikeABee;
        private static readonly CardDefinitionId Strike = TakahideCardContent.StrikeLikeATiger;
        private static readonly CardDefinitionId Float = TakahideCardContent.FloatLikeAButterfly;

        private static CardMoveFact Move(
            CardDefinitionId card, CardMoveZone from, CardMoveZone to, bool facedown = false,
            CardColor color = CardColor.Gold) =>
            new CardMoveFact(card, from, to, arrivesFacedown: facedown, knownColor: color);

        private static string Both(CardMoveFact outgoing, CardMoveFact incoming, string suffix = "") =>
            CardMoveWords.Swapped(Cards, "TAKAHIDE", new[] { outgoing, incoming }, suffix);

        private static string Others(CardMoveFact outgoing, CardMoveFact incoming) =>
            CardMoveWords.Swapped(Cards, "TAKAHIDE", new[]
            {
                outgoing.IsSeenByTheTable() ? outgoing : outgoing.Unnamed(),
                incoming.IsSeenByTheTable() ? incoming : incoming.Unnamed()
            }, string.Empty);

        [Test]
        public void TheSwapRows_ReadAsTheApprovedMockup_OnHisSeatAndOnEveryOther()
        {
            var selfSwapOut = Move(Sting, CardMoveZone.TurnArea, CardMoveZone.Deck);
            var selfSwapIn = Move(Strike, CardMoveZone.Deck, CardMoveZone.TurnArea);
            Assert.That(Both(selfSwapOut, selfSwapIn),
                Is.EqualTo("TAKAHIDE SWAPPED STING LIKE A BEE FOR STRIKE LIKE A TIGER FROM THE DECK"));
            Assert.That(Others(selfSwapOut, selfSwapIn), Is.EqualTo(Both(selfSwapOut, selfSwapIn)));

            var handOut = Move(Float, CardMoveZone.Hand, CardMoveZone.Deck);
            var handIn = Move(Sting, CardMoveZone.Deck, CardMoveZone.Hand);
            Assert.That(Both(handOut, handIn),
                Is.EqualTo("TAKAHIDE SWAPPED FLOAT LIKE A BUTTERFLY IN HAND FOR STING LIKE A BEE FROM THE DECK"));
            Assert.That(Others(handOut, handIn),
                Is.EqualTo("TAKAHIDE SWAPPED THE GOLD CARD IN HAND WITH ONE FROM THE DECK"));

            var resolvedOut = Move(Sting, CardMoveZone.TurnArea, CardMoveZone.Deck);
            var resolvedIn = Move(Strike, CardMoveZone.Deck, CardMoveZone.TurnArea, facedown: true);
            Assert.That(Both(resolvedOut, resolvedIn),
                Is.EqualTo("TAKAHIDE SWAPPED STING LIKE A BEE FOR STRIKE LIKE A TIGER, FACEDOWN"));
            Assert.That(Others(resolvedOut, resolvedIn),
                Is.EqualTo("TAKAHIDE SWAPPED STING LIKE A BEE FOR A FACEDOWN CARD"));

            var discardOut = Move(Float, CardMoveZone.DiscardPile, CardMoveZone.Deck);
            var discardIn = Move(Strike, CardMoveZone.Deck, CardMoveZone.DiscardPile, facedown: true);
            Assert.That(Both(discardOut, discardIn),
                Is.EqualTo("TAKAHIDE SWAPPED THE DISCARDED FLOAT LIKE A BUTTERFLY FOR STRIKE LIKE A TIGER, FACEDOWN"));
            Assert.That(Others(discardOut, discardIn),
                Is.EqualTo("TAKAHIDE SWAPPED THE DISCARDED FLOAT LIKE A BUTTERFLY FOR A FACEDOWN CARD"));

            Assert.That(CardMoveWords.Swapped(Cards, "WASP", new[]
                {
                    Move(WaspCardContent.Electrocute, CardMoveZone.TurnArea, CardMoveZone.Hand, color: CardColor.None),
                    Move(WaspCardContent.StaticBarrier, CardMoveZone.Hand, CardMoveZone.TurnArea, color: CardColor.None)
                }, " (SET AN EXAMPLE)"),
                Is.EqualTo("WASP SWAPPED ELECTROCUTE FOR STATIC BARRIER (SET AN EXAMPLE)"));
            Assert.That(CardMoveWords.Swapped(Cards, "WASP", new[]
                {
                    Move(WaspCardContent.Electrocute, CardMoveZone.TurnArea, CardMoveZone.DiscardPile, color: CardColor.None),
                    Move(WaspCardContent.Shock, CardMoveZone.DiscardPile, CardMoveZone.TurnArea, color: CardColor.None)
                }, " (HOLD MY SAKE)"),
                Is.EqualTo("WASP SWAPPED ELECTROCUTE FOR SHOCK FROM THE DISCARD (HOLD MY SAKE)"));
        }

        [Test]
        public void ReadyForWarsRow_NamesItsCardsOnHisSeat_AndCountsThemByColourOnEveryOther()
        {
            var moves = new[]
            {
                Move(TakahideCardContent.Bushido, CardMoveZone.Hand, CardMoveZone.Deck, color: CardColor.Silver),
                Move(Sting, CardMoveZone.Deck, CardMoveZone.Hand),
                Move(Strike, CardMoveZone.Deck, CardMoveZone.Hand)
            };
            Assert.That(CardMoveWords.Grouped(Cards, moves),
                Is.EqualTo("BUSHIDO TO THE DECK, STING LIKE A BEE AND STRIKE LIKE A TIGER TO HAND"));
            Assert.That(CardMoveWords.Grouped(Cards, moves.Select(move => move.Unnamed()).ToArray()),
                Is.EqualTo("THE SILVER CARD TO THE DECK, TWO GOLD CARDS TO HAND"),
                "a hand card is never named to another seat (mockup F's rule)");
        }

        [Test]
        public void TheFormatter_WritesTheKeptRow_TheSourceOnlyForATeammate_AndTheObstacleLine()
        {
            var stage = new RedStage(TakahideCardContent.SpinningBlade, allyAt: new HexCoordinate(-2, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ReleaseIfAsked();
            var source = stage.Id(TakahideCardContent.SpinningBlade);
            var log = new[]
            {
                new MatchLogEntry(1, MatchLogEntryKind.Attack, 1, 1, subjectHeroId: RedStage.Taka,
                    targetHeroId: RedStage.Enemy, subjectCardInstanceId: source),
                new MatchLogEntry(2, MatchLogEntryKind.CardKeptByCardText, 1, 1, subjectHeroId: RedStage.Ally,
                    subjectCardInstanceId: source,
                    cardMoves: new[] { Move(WaspCardContent.Electrocute, CardMoveZone.TurnArea, CardMoveZone.TurnArea, color: CardColor.None) }),
                new MatchLogEntry(3, MatchLogEntryKind.CardSwappedByCardText, 1, 1, subjectHeroId: RedStage.Taka,
                    subjectCardInstanceId: source,
                    cardMoves: new[] { Move(Sting, CardMoveZone.TurnArea, CardMoveZone.Deck), Move(Strike, CardMoveZone.Deck, CardMoveZone.TurnArea) })
            };
            var model = new StagedReadModel(stage.Cards, Cards.Concat(stage.Definitions).Distinct().ToArray(), log);

            var lines = MatchLogFormatter.ComposeOldestFirst(model, Catalog()).Select(line => line.Text).ToArray();
            Assert.That(lines.Length, Is.EqualTo(4), string.Join(" | ", lines));
            Assert.That(lines[1], Does.EndWith(": EMPTY SPACES ADJACENT ARE OBSTACLES FOR THE ENEMY THIS TURN"));
            Assert.That(lines[2], Does.EndWith(" KEPT ELECTROCUTE (SPINNING BLADE)"),
                "a teammate's row names the card whose text asked");
            Assert.That(lines[3], Does.EndWith(" SWAPPED STING LIKE A BEE FOR STRIKE LIKE A TIGER FROM THE DECK"),
                "his own card's row needs no source: it already reads as his");
            Assert.That(MatchLogFormatter.ComposeOldestFirst(model).Count, Is.EqualTo(3),
                "without a catalog the obstacle line is simply absent");
        }

        // -------------------------------------------------------------------------------------------
        // B - the facedown gold
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AFacedownGold_IsTheSameHatchedGoldOnEverySeat_NamedOnlyOnHis()
        {
            var stage = new GoldStage(played: SyntheticGoldSwapHand.ButterflyId,
                deck: new[] { SyntheticGoldSwapHand.BeeId, SyntheticGoldSwapHand.TigerId },
                next: SyntheticGoldSwapHand.BushidoId);
            stage.StartMoving();
            stage.StopMovingIfAsked();
            stage.ChooseCard(SyntheticGoldSwapHand.BeeId);
            stage.NextTurn();
            stage.StartSkill();
            stage.ChooseCard(SyntheticGoldSwapHand.TigerId);

            var participants = StagedBoardParticipants.Create();
            var enemySlots = FacedownSlotMask.Mask(stage.Round.TurnSlots,
                TakahideTableFactsTests.Seat(GoldStage.Enemy, TeamSide.Blue), stage.Cards, participants, stage.Definitions);
            var enemyView = new StagedReadModel(
                stage.Cards.Where(card => !card.Id.Equals(stage.Id(SyntheticGoldSwapHand.TigerId))).ToArray(),
                stage.Definitions, Array.Empty<MatchLogEntry>(), enemySlots);
            var enemySlot = CurrentRoundCards.TurnSlots(enemyView, GoldStage.Taka)[0];
            Assert.That(enemySlot.IsWithheld, Is.True);
            Assert.That(enemySlot.IsOccupied, Is.True);
            Assert.That(enemySlot.Color, Is.EqualTo(CardColor.Gold), "every seat sees the hatched gold");
            Assert.That(enemySlot.PlayedCard, Is.Null, "and no face - the hover shows the card back");
            Assert.That(enemySlot.AssistiveLabel, Does.Not.Contain("TEST - "), "no card is named");

            var ownView = new StagedReadModel(stage.Cards, stage.Definitions, Array.Empty<MatchLogEntry>(),
                stage.Round.TurnSlots);
            var ownSlot = CurrentRoundCards.TurnSlots(ownView, GoldStage.Taka)[0];
            Assert.That(ownSlot.IsWithheld, Is.False);
            Assert.That(ownSlot.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(ownSlot.PlayedCard.IsFacedown, Is.True, "his seat's hover shows the face, dimmed and hatched");
        }

        [Test]
        public void AFacedownDiscard_IsAWithheldGoldOnEveryOtherSeat_AndHisOwnCardOnHis()
        {
            var stage = new GoldStage(played: SyntheticGoldSwapHand.BushidoId,
                discarded: new[] { SyntheticGoldSwapHand.ButterflyId },
                deck: new[] { SyntheticGoldSwapHand.BeeId, SyntheticGoldSwapHand.TigerId });
            stage.StartSkill();
            stage.ChooseCard(SyntheticGoldSwapHand.BeeId);
            var summaries = SeatHandSummary.Derive(StagedBoardParticipants.Create(), stage.Cards, stage.Definitions);

            var enemyView = new StagedReadModel(
                stage.Cards.Where(card => !card.Id.Equals(stage.Id(SyntheticGoldSwapHand.BeeId))).ToArray(),
                stage.Definitions, Array.Empty<MatchLogEntry>(), handSummaries: summaries);
            Assert.That(CurrentRoundCards.WithheldDiscardColors(enemyView, GoldStage.Taka),
                Is.EqualTo(new[] { CardColor.Gold }));

            var ownView = new StagedReadModel(stage.Cards, stage.Definitions, Array.Empty<MatchLogEntry>(),
                handSummaries: summaries);
            Assert.That(CurrentRoundCards.WithheldDiscardColors(ownView, GoldStage.Taka), Is.Empty,
                "his own seat holds the card itself, so it is never drawn twice");
        }

        /// <summary>
        /// Milestone 9.36. The fine 9.32g hatch read as plain gold on a mini; the user settled "a bolder
        /// diagonal hatch with fewer, thicker strokes". These are the strokes the hatch's content
        /// generator paints, at the rail mini's footprint and at a full face.
        /// </summary>
        [Test]
        public void TheMiniHatch_DrawsFewerThickerDarkerStrokes_AndTheFullFaceKeepsTheFineHatch()
        {
            var mini = new UnityEngine.Rect(
                0f, 0f, HeroOverviewsUnityView.MiniCardWidth, HeroOverviewsUnityView.MiniCardHeight);
            var bold = Hatch.Strokes(mini, Hatch.Weight.Mini);
            var fine = Hatch.Strokes(mini, Hatch.Weight.Full);

            Assert.That(Hatch.LineWidthOf(Hatch.Weight.Mini), Is.GreaterThanOrEqualTo(2.5f),
                "thicker strokes on a mini");
            Assert.That(bold.Count, Is.InRange(3, 6), "fewer strokes, and still a hatch rather than one strike");
            Assert.That(bold.Count * 2, Is.LessThanOrEqualTo(fine.Count),
                "at most half the fine hatch's strokes on the same mini");
            Assert.That(Hatch.StrokeColorOf(Hatch.Weight.Mini).a, Is.GreaterThanOrEqualTo(0.8f), "and darker");
            foreach (var stroke in bold)
            {
                Assert.That(mini.Contains(stroke.Key) || OnTheEdge(mini, stroke.Key), Is.True, "clipped to the card");
                Assert.That(mini.Contains(stroke.Value) || OnTheEdge(mini, stroke.Value), Is.True, "clipped to the card");
                Assert.That(stroke.Value.x - stroke.Key.x, Is.EqualTo(stroke.Key.y - stroke.Value.y).Within(0.001f),
                    "diagonal, rising left to right at 45 degrees");
            }

            var full = new UnityEngine.Rect(0f, 0f, CardFootprint.StandardWidth, CardFootprint.StandardHeight);
            Assert.That(Hatch.LineWidthOf(Hatch.Weight.Full), Is.EqualTo(1f), "the full face keeps the fine hatch");
            Assert.That(Hatch.SpacingOf(Hatch.Weight.Full), Is.EqualTo(4f));
            Assert.That(Hatch.StrokeColorOf(Hatch.Weight.Full).a, Is.EqualTo(0.62f).Within(0.001f));
            Assert.That(Hatch.Strokes(full, Hatch.Weight.Full).Count, Is.GreaterThan(150));
        }

        private static bool OnTheEdge(UnityEngine.Rect rect, UnityEngine.Vector2 point) =>
            point.x >= rect.xMin - 0.001f && point.x <= rect.xMax + 0.001f &&
            point.y >= rect.yMin - 0.001f && point.y <= rect.yMax + 0.001f;

        /// <summary>
        /// Milestone 9.36, through the rail itself: on every seat of a configured match holding his
        /// facedown gold, each hatched mini is the bold weight; on his seat the face a hover reveals is
        /// the only fine one.
        /// </summary>
        [Test]
        public void EveryMiniWearsTheBoldHatch_AndOnlyTheRevealedFaceTheFine()
        {
            var host = new MatchHost(TakahideDealingTests.CreateFacedownGoldSession());
            var reachedHisSeat = false;
            foreach (var seat in host.Seats.Seats)
            {
                var view = new HeroOverviewsUnityView();
                new HeroOverviewsPresenter(new SeatMatchClient(host.ClientForSeat(seat.Id)), view).Refresh();
                var minis = view.Root.Query<Hatch>().ToList();
                Assert.That(minis, Is.Not.Empty, "every seat draws the facedown gold hatched");
                Assert.That(minis.Select(hatch => hatch.HatchWeight), Has.All.EqualTo(Hatch.Weight.Mini),
                    "and every mini wears the bold hatch");

                var takahide = view.Model.Heroes.Single(hero => hero.ContentId.Value == "takahide");
                if (!seat.Owns(takahide.HeroId))
                {
                    continue;
                }

                reachedHisSeat = true;
                var slot = takahide.TurnSlots.Single(value => value.PlayedCard != null && value.PlayedCard.IsFacedown);
                view.ForwardTurnSlotHighlight(slot);
                var revealed = view.HoverPreviewRoot.Query<Hatch>().ToList();
                Assert.That(revealed.Select(hatch => hatch.HatchWeight), Is.EqualTo(new[] { Hatch.Weight.Full }),
                    "his hover reveals the face, dimmed and finely hatched");
            }

            Assert.That(reachedHisSeat, Is.True, "Fixture: his own seat is among the seats");
        }

        // -------------------------------------------------------------------------------------------
        // A - the six-card hand
        // -------------------------------------------------------------------------------------------

        [Test]
        public void SixCards_FitTheFiveCardFootprint_AndFiveStepAsTheyAlwaysHave()
        {
            Assert.That(DirectorHandsUnityView.PeekSlotFootprintFor(5),
                Is.EqualTo(DirectorHandsUnityView.PeekCardSlotFootprint));
            Assert.That(DirectorHandsUnityView.PeekCardOuterWidth + (5 * DirectorHandsUnityView.PeekSlotFootprintFor(6)),
                Is.EqualTo(ConfiguredMatchHudRenderer.CompactHandRightEdge).Within(0.01f),
                "the sixth card ends where the hand always has");
            Assert.That(DirectorHandsUnityView.PeekSlotFootprintFor(6),
                Is.LessThan(DirectorHandsUnityView.PeekCardSlotFootprint), "each covered card shows a narrower column");
        }

        // -------------------------------------------------------------------------------------------
        // C and D - the two swap windows
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDeckGoldChoice_TagsEachCandidateFacedown_OnlyWhereTheNewCardArrivesFacedown()
        {
            var discarded = new GoldStage(played: SyntheticGoldSwapHand.BushidoId,
                discarded: new[] { SyntheticGoldSwapHand.ButterflyId },
                deck: new[] { SyntheticGoldSwapHand.BeeId, SyntheticGoldSwapHand.TigerId });
            discarded.StartSkill();
            var node = SwapNode(discarded.Definitions, SyntheticDeckSwapCapabilities.SwapOwnedColorCardWithDeckCardV1);
            var view = new StagedReadModel(discarded.Cards, discarded.Definitions, Array.Empty<MatchLogEntry>());
            Assert.That(SwapChoicePresentation.ArrivesFacedown(view, discarded.Choice, node), Is.True,
                "Bushido on a discarded gold: the new card is facedown");
            Assert.That(SwapChoicePresentation.Relabel(view, discarded.Choice.Candidates[0], false, true, "TEST - BEE"),
                Is.EqualTo("TEST - BEE - FACEDOWN"));

            var inHand = new GoldStage(played: SyntheticGoldSwapHand.BushidoId,
                hand: new[] { SyntheticGoldSwapHand.ButterflyId },
                deck: new[] { SyntheticGoldSwapHand.BeeId, SyntheticGoldSwapHand.TigerId });
            inHand.StartSkill();
            Assert.That(SwapChoicePresentation.ArrivesFacedown(
                    new StagedReadModel(inHand.Cards, inHand.Definitions, Array.Empty<MatchLogEntry>()), inHand.Choice, node),
                Is.False, "a gold in hand swaps faceup into the hand");
        }

        [Test]
        public void TheTeammatesSwap_IsOneWindowOnTheirSeat_WithKeepMyCard()
        {
            var stage = new RedStage(TakahideCardContent.HoldMySake, allyAt: new HexCoordinate(-3, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(RedStage.Ally);
            var node = SwapNode(
                stage.Definitions.Where(definition => definition.Id.Equals(TakahideCardContent.HoldMySake)),
                SyntheticUnresolvedSwapCapabilities.SwapUnresolvedCardV1);
            Assert.That(SwapChoicePresentation.IsUnresolvedSwap(node), Is.True);
            Assert.That(SwapChoicePresentation.UnresolvedSwapQuestion(node), Does.Contain("hand or discard"));

            var model = DecisionWindowPresenter.CreateViewModel(
                new StagedReadModel(stage.Cards, stage.Definitions, Array.Empty<MatchLogEntry>(), stage.Round.TurnSlots,
                    choice: stage.Choice, roundIndex: 1, turnIndex: stage.Round.TurnIndex,
                    resolvingSource: stage.Id(TakahideCardContent.HoldMySake)), RedStage.Ally);
            Assert.That(model, Is.Not.Null, "the teammate answers the swap on their own seat");
            Assert.That(model.Title, Does.Contain(SwapChoicePresentation.UnresolvedSwapTitle));
            var labels = model.Controls.Select(control => control.Label)
                .Concat(new[] { model.CornerControl?.Label }).ToArray();
            Assert.That(labels, Has.Member(SwapChoicePresentation.KeepMyCardLabel), string.Join(" | ", labels));
            Assert.That(model.ContextCards.First().Caption, Does.StartWith("YOUR UNRESOLVED CARD - INITIATIVE"));
        }

        // -------------------------------------------------------------------------------------------

        private static EffectNodeDefinition SwapNode(IEnumerable<CardDefinition> definitions, EffectCapabilityReference capability) =>
            definitions.SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(root => root != null)
                .SelectMany(root => root.Nodes)
                .First(node => node.Capability.Equals(capability));

        private static EffectCapabilityCatalog Catalog()
        {
            // Milestone 9.32h: his handlers are the build's own.
            return MatchContentCatalogs.ForThisBuild().Capabilities;
        }

        /// <summary>A session's read model with a staged hero's cards, slots, log and choice in place.</summary>
        private sealed class StagedReadModel : MatchFeaturePresenterTests.DelegatingReadModel
        {
            private static readonly IMatchReadModel Base =
                MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState()).Projection;

            private readonly IReadOnlyList<CardInstance> cards;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<MatchLogEntry> log;
            private readonly IReadOnlyList<GuardsDigital.Core.Cards.RoundTurnSlot> slots;
            private readonly IReadOnlyList<SeatHandSummary> handSummaries;
            private readonly PendingEffectChoice choice;
            private readonly int roundIndex;
            private readonly int turnIndex;
            private readonly CardInstanceId resolvingSource;

            public StagedReadModel(
                IReadOnlyList<CardInstance> cards,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<MatchLogEntry> log,
                IReadOnlyList<GuardsDigital.Core.Cards.RoundTurnSlot> slots = null,
                IReadOnlyList<SeatHandSummary> handSummaries = null,
                PendingEffectChoice choice = null,
                int roundIndex = 1,
                int turnIndex = 1,
                CardInstanceId resolvingSource = default)
                : base(Base)
            {
                this.resolvingSource = resolvingSource;
                this.cards = cards;
                this.definitions = definitions;
                this.log = log;
                this.slots = slots ?? Array.Empty<GuardsDigital.Core.Cards.RoundTurnSlot>();
                this.handSummaries = handSummaries ?? Array.Empty<SeatHandSummary>();
                this.choice = choice;
                this.roundIndex = roundIndex;
                this.turnIndex = turnIndex;
            }

            public override IReadOnlyList<CardInstance> Cards => cards;
            public override IReadOnlyList<CardDefinition> Definitions => definitions;
            public override IReadOnlyList<MatchLogEntry> MatchLog => log;
            public override IReadOnlyList<GuardsDigital.Core.Cards.RoundTurnSlot> TurnSlots => slots;
            public override IReadOnlyList<SeatHandSummary> HandSummaries => handSummaries;
            public override PendingEffectChoice PendingEffectChoice => choice;
            public override int RoundIndex => roundIndex;
            public override int TurnIndex => turnIndex;
            public override CardInstanceId ResolvingSourceCardInstanceId => resolvingSource;
            public override bool IsInputEnabled => true;
            public override GuardsDigital.Core.Cards.RoundResolutionPhase Phase =>
                GuardsDigital.Core.Cards.RoundResolutionPhase.ResolvingTurn;
            public override PendingDecisionOwner DecisionOwner => choice == null
                ? base.DecisionOwner
                : PendingDecisionOwner.ForHero(PendingDecisionFamilies.Effect, choice.ChooserId);
            public override PendingDecisionContract DecisionContract => choice == null
                ? base.DecisionContract
                : PendingDecisionContract.Create(null, null, null, null, choice, null, null, null, null, null, null);
        }
    }
}
