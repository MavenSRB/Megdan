using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
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
    /// Milestone 9.32b: a teammate swaps their unresolved card, the shape of Takahide's Set an Example
    /// and Hold my Saké - "After the attack: A friendly hero in radius may swap their unresolved card
    /// with a card in their hand[, or in their discard]." p.10 swaps every state, and p.7 names the
    /// next actor by "the highest Initiative on their unresolved card", so the teammate now acts by the
    /// swapped-in card's Initiative. Takahide chooses the hero; the hero decides whether and with which
    /// card (p.10 Making Choices). Proven on <see cref="SyntheticUnresolvedSwapHand"/>; no Takahide face
    /// is registered.
    /// </summary>
    public sealed class TakahideUnresolvedSwapTests
    {
        private static readonly CardStateFacts Unresolved = new CardStateFacts(
            CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp);

        [Test]
        public void ATeammateSwapsTheirUnresolvedCard_AndActsByTheSwappedInInitiative_AheadOfTheEnemy()
        {
            var stage = new Stage(allyPlayed: 2, allyHand: new[] { 9, 4 });
            stage.AttackAndDefend();

            Assert.That(stage.Choice?.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget),
                "Takahide chooses the friendly hero (p.10 Making Choices)");
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Taka));
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.HeroId),
                Is.EqualTo(new[] { Stage.Ally }), "a friendly hero, never Takahide himself");
            stage.ChooseHero(Stage.Ally);

            Assert.That(stage.Choice?.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "\"may swap\" is the teammate's to decline");
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally), "asked of the teammate's seat");
            stage.Confirm();

            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally), "and which card is the teammate's choice");
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.AllyHand(9), stage.AllyHand(4) }), "every card in their hand");
            stage.ChooseCard(stage.AllyHand(9));

            Assert.That(stage.Card(stage.AllyHand(9)).State, Is.EqualTo(Unresolved),
                "the hand card takes the unresolved card's states (p.10)");
            Assert.That(stage.Card(stage.AllyPlayed).State.IsInitialInHand, Is.True,
                "and the old card goes back to the hand, playable again");
            Assert.That(stage.AllySlotCard, Is.EqualTo(stage.AllyHand(9)), "this turn's slot names it");
            Assert.That(stage.Round.CurrentCommitmentAuthority.GetRevealedCardInstanceId(Stage.Ally),
                Is.EqualTo(stage.AllyHand(9)), "and so does the revealed record");

            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Ally),
                "Initiative 9 now acts before the enemy's 5; with the old card's 2 the enemy was next (p.7)");
            Assert.That(stage.Turn.SelectedActor.CardInstanceId, Is.EqualTo(stage.AllyHand(9)));
        }

        [Test]
        public void ATeammateCanSwapToActLater_BehindTheEnemy()
        {
            var stage = new Stage(allyPlayed: 9, allyHand: new[] { 2 });
            stage.AttackAndDefend();
            stage.ChooseHero(Stage.Ally);
            stage.Confirm();
            stage.ChooseCard(stage.AllyHand(2));
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Enemy),
                "Initiative 2 now waits behind the enemy's 5, where 9 had acted first");
        }

        [Test]
        public void TheTeammateMayDecline_AndNothingMoves()
        {
            var stage = new Stage(allyPlayed: 2, allyHand: new[] { 9 });
            var before = stage.Snapshot();
            stage.AttackAndDefend();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Choice, Is.Null);
            Assert.That(stage.Card(stage.AllyPlayed).State, Is.EqualTo(Unresolved));
            Assert.That(stage.Card(stage.AllyHand(9)).State.IsInitialInHand, Is.True);
            Assert.That(stage.AllySlotCard, Is.EqualTo(stage.AllyPlayed));
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Enemy));
            Assert.That(before, Is.Not.Empty);
        }

        [Test]
        public void HoldMySake_ReachesTheDiscard_AndTheOldCardTakesTheDiscardsState()
        {
            var stage = new Stage(allyPlayed: 2, allyHand: Array.Empty<int>(), allyDiscarded: new[] { 9 },
                played: SyntheticUnresolvedSwapHand.RallySakeId);
            stage.AttackAndDefend();
            stage.ChooseHero(Stage.Ally);
            stage.Confirm();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EqualTo(new[] { stage.AllyDiscarded(9) }), "\"or in their discard\"");
            stage.ChooseCard(stage.AllyDiscarded(9));
            Assert.That(stage.Card(stage.AllyDiscarded(9)).State, Is.EqualTo(Unresolved),
                "the discarded card becomes played and unresolved (p.10)");
            Assert.That(stage.Card(stage.AllyPlayed).State, Is.EqualTo(new CardStateFacts(
                    CardLocation.DiscardPile, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)),
                "and the old card takes the discarded card's state");
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Ally));
        }

        [Test]
        public void TheHandOnlyFace_DoesNotReachTheDiscard_SoATeammateWithOnlyDiscardsIsNotOffered()
        {
            var stage = new Stage(allyPlayed: 2, allyHand: Array.Empty<int>(), allyDiscarded: new[] { 9 });
            stage.AttackAndDefend();
            Assert.That(stage.Choice, Is.Null,
                "no friendly hero the swap can act on: the after-attack text does nothing (6.8l, TK-Q4: the Attack " +
                "is what is offered)");
            Assert.That(stage.Card(stage.TakaPlayed).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(stage.AllySlotCard, Is.EqualTo(stage.AllyPlayed));
        }

        [Test]
        public void ATeammateWhoHasAlreadyResolved_HasNoUnresolvedCard_AndIsNotOffered()
        {
            var stage = new Stage(allyPlayed: 12, allyHand: new[] { 9 }, takaInitiativeBelowAlly: true);
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Ally), "the teammate acts first");
            stage.HoldForTheSelectedHero();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Taka));
            stage.AttackAndDefend();
            Assert.That(stage.Choice, Is.Null, "their card is resolved, not unresolved");
            Assert.That(stage.Card(stage.AllyHand(9)).State.IsInitialInHand, Is.True);
        }

        [Test]
        public void TheCardChoice_SurvivesARestore_AndFinishesInTheSameBytes()
        {
            var stage = new Stage(allyPlayed: 2, allyHand: new[] { 9, 4 });
            stage.AttackAndDefend();
            stage.ChooseHero(Stage.Ally);
            stage.Confirm();
            var restored = stage.Restore(stage.Snapshot());
            Assert.That(restored.Choice?.ChooserId, Is.EqualTo(Stage.Ally));
            restored.ChooseCard(stage.AllyHand(9));
            stage.ChooseCard(stage.AllyHand(9));
            Assert.That(restored.Snapshot(), Is.EqualTo(stage.Snapshot()));
            var after = stage.Restore(stage.Snapshot());
            Assert.That(after.AllySlotCard, Is.EqualTo(stage.AllyHand(9)));
            Assert.That(after.Round.CurrentCommitmentAuthority.GetRevealedCardInstanceId(Stage.Ally),
                Is.EqualTo(stage.AllyHand(9)), "the revealed record is restored as swapped");
        }

        [Test]
        public void TheTransition_RefusesAnotherOwnersCard_AFacedownDiscard_AndACardNotWaiting()
        {
            var definition = Stage.Plain(5).Reference;
            CardInstance InHand(string id, HeroId hero) => CardInstance.CreateInHand(new CardInstanceId(id), definition, hero);
            var waiting = InHand("waiting", Stage.Ally);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(waiting) && CardLifecycleAuthority.TryReveal(waiting), Is.True);
            Assert.That(CardLifecycleAuthority.TrySwapUnresolvedWithRestingCard(waiting, InHand("other", Stage.Taka)), Is.False,
                "both cards are the owner's");
            Assert.That(CardLifecycleAuthority.TrySwapUnresolvedWithRestingCard(InHand("fresh", Stage.Ally), InHand("x", Stage.Ally)),
                Is.False, "only an unresolved card is swapped this way");
            var hidden = InHand("hidden", Stage.Ally);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(hidden), Is.True);
            var facedown = CardInstance.CreateInDeck(new CardInstanceId("facedown"), definition, Stage.Ally);
            Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(hidden, facedown, true), Is.True);
            Assert.That(CardLifecycleAuthority.TrySwapUnresolvedWithRestingCard(waiting, facedown), Is.False,
                "a facedown discarded card has no face to play");
            Assert.That(CardLifecycleAuthority.TrySwapUnresolvedWithRestingCard(waiting, InHand("partner", Stage.Ally)), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // A staged round: Takahide (Blue) and his teammate against one enemy (Red)
        // -------------------------------------------------------------------------------------------

        internal sealed class Stage
        {
            internal static readonly HeroId Taka = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            private static readonly CardInstanceId EnemyPlayed = new CardInstanceId("enemy-played");
            private static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");

            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();
            private readonly MatchContentCatalogs catalogs;
            private readonly MatchParticipants participants;

            internal Stage(
                int allyPlayed,
                IEnumerable<int> allyHand,
                IEnumerable<int> allyDiscarded = null,
                CardDefinitionId played = default,
                bool takaInitiativeBelowAlly = false)
            {
                catalogs = CreateCatalogs();
                participants = StagedBoardParticipants.CreateWithBlueAlly();
                var takaCard = SyntheticUnresolvedSwapHand.Definitions.Single(definition =>
                    definition.Id.Equals(played.IsDefined ? played : SyntheticUnresolvedSwapHand.RallyId));
                var plain = Enumerable.Range(1, 13).Select(Plain).ToArray();
                Definitions = SyntheticUnresolvedSwapHand.Definitions.Concat(plain).Concat(new[] { Wall }).ToArray();

                TakaPlayed = new CardInstanceId("taka-played");
                AllyPlayed = new CardInstanceId("ally-played");
                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(TakaPlayed,
                        takaInitiativeBelowAlly ? Lower(takaCard).Reference : takaCard.Reference, Taka),
                    CardInstance.CreateInHand(AllyPlayed, plain[allyPlayed - 1].Reference, Ally),
                    CardInstance.CreateInHand(EnemyPlayed, plain[4].Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, Wall.Reference, Enemy)
                };
                foreach (var initiative in allyHand)
                    cards.Add(CardInstance.CreateInHand(AllyHand(initiative), plain[initiative - 1].Reference, Ally));
                foreach (var initiative in allyDiscarded ?? Enumerable.Empty<int>())
                {
                    var card = CardInstance.CreateInHand(AllyDiscarded(initiative), plain[initiative - 1].Reference, Ally);
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                    cards.Add(card);
                }

                if (takaInitiativeBelowAlly)
                    Definitions = Definitions.Concat(new[] { Lower(takaCard) }).ToArray();
                Cards = cards.ToArray();
                var spaces = (from q in Enumerable.Range(-4, 9)
                              from r in Enumerable.Range(-4, 9)
                              where Math.Abs(q + r) <= 4
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("unresolved-swap"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Taka, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(1, 0)),
                        new HeroOccupancy(Ally, new HexCoordinate(-2, 0))
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Blue), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                Accept(Round.TryCommitCard(Taka, TakaPlayed));
                Accept(Round.TryCommitCard(Ally, AllyPlayed));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
            }

            private Stage(Stage template, string snapshot)
            {
                catalogs = template.catalogs;
                participants = template.participants;
                Definitions = template.Definitions;
                TakaPlayed = template.TakaPlayed;
                AllyPlayed = template.AllyPlayed;
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
                        .Concat(SyntheticUnresolvedSwapHand.Handlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;
            internal CardInstanceId TakaPlayed { get; }
            internal CardInstanceId AllyPlayed { get; }

            internal CardInstanceId AllyHand(int initiative) => new CardInstanceId("ally-hand-" + initiative);
            internal CardInstanceId AllyDiscarded(int initiative) => new CardInstanceId("ally-discarded-" + initiative);
            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstanceId AllySlotCard =>
                Round.TurnSlots.Single(slot => slot.RoundIndex == 1 && slot.TurnIndex == 1)
                    .Assignments.Single(assignment => assignment.HeroId.Equals(Ally)).CardInstanceId.Value;

            internal Stage Restore(string snapshot) => new Stage(this, snapshot);
            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal void AttackAndDefend()
            {
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Taka));
                var offer = Turn.GetTurnActionOffers().Single(value =>
                    value.SourceCardInstanceId.Equals(TakaPlayed) &&
                    value.ActionDefinitionId.Equals(SyntheticUnresolvedSwapHand.PrimaryId));
                Accept(Round.TryStartAttack(new ActionResolutionId("rally"), offer));
                var target = Turn.PendingAttackTargetChoice;
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
                var defense = Turn.PendingDefenseChoice;
                Accept(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, EnemyWall)));
            }

            internal void HoldForTheSelectedHero()
            {
                Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                ReleaseIfAsked();
            }

            internal void ChooseHero(HeroId hero) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.HeroId.Equals(hero))));

            internal void ChooseCard(CardInstanceId card) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.CardInstanceId.Equals(card))));

            internal void Confirm() => Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));

            internal void Decline() => Accept(Round.TryAnswerEffectChoice(Choice.DeclineInvocation));

            internal void ReleaseIfAsked()
            {
                if (Turn != null && Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            /// <summary>A plain card printing only an Initiative and a one-space secondary Movement.</summary>
            internal static CardDefinition Plain(int initiative) => new CardDefinition(
                new CardDefinitionId("unresolved-swap-plain-" + initiative), "rev-1", "TEST - Initiative " + initiative,
                new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            /// <summary>Rally with Initiative 10, below a teammate's 12.</summary>
            private static CardDefinition Lower(CardDefinition rally) => new CardDefinition(
                new CardDefinitionId(rally.Id.Value + "-ten"), rally.Reference.ContentRevision, rally.DisplayName,
                new InitiativeValue(10), rally.ContentAuthority, rally.Progression, rally.ActionDefinitions);

            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("unresolved-swap-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
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
