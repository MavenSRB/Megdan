using System;
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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class GarrusComposedRoundTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        [TestCase(false)]
        [TestCase(true)]
        public void BattleFury_ReplaysTheExactDiscardedThreaten_IncludingItsRepeat_BeforeHowlAura(bool accept)
        {
            var f = new Fixture(GarrusCardContent.ChillingHowl, resolved: GarrusCardContent.Threaten);
            f.Answer(f.Choice.ConfirmInvocation);
            f.Pick(EffectValue.Card(f.Borrowed.Id));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("may-perform-primary"));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Timing.Effects, Is.Empty, "Battle Fury happens before the next Howl clause.");
            f.RoundTrip();
            f.Answer(accept ? f.Choice.ConfirmInvocation : f.Choice.DeclineInvocation);
            if (accept)
            {
                Assert.That(f.Authority.PendingSkillResolution.SourceCardInstanceId, Is.EqualTo(f.Borrowed.Id));
                f.Pick(EffectValue.Hero(Blue));
                f.Pick(EffectValue.Space(new HexCoordinate(2, 0)));
                Assert.That(f.Choice.NodeId.Value, Is.EqualTo("repeat"));
                f.RoundTrip();
                f.Answer(f.Choice.ConfirmInvocation);
                f.Pick(EffectValue.Hero(Blue));
                f.Pick(EffectValue.Space(new HexCoordinate(3, 0)));
            }
            Assert.That(f.Choice, Is.Null);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            f.RoundTrip();
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HeavyPilum_CompletesDiscardOrDefeatBeforeOptionalMovement(int enemyHand)
        {
            var f = new Fixture(GarrusCardContent.HeavyPilum, enemyHand: enemyHand);
            f.Pick(EffectValue.Hero(Blue));
            if (enemyHand > 0) f.Pick(EffectValue.Card(f.EnemyDefense.Id));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("may-move"));
            Assert.That(f.Board.TryGetHeroCoordinate(Blue, out _), Is.EqualTo(enemyHand > 0));
            var countersAfterConsequence = f.Round.TeamLives.GetCounters(new TeamId(StagedBoardParticipants.BlueTeamIdValue));
            f.RoundTrip();
            f.Answer(f.Choice.ConfirmInvocation);
            f.Pick(EffectValue.Space(new HexCoordinate(-2, 0)));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Round.TeamLives.GetCounters(new TeamId(StagedBoardParticipants.BlueTeamIdValue)),
                Is.EqualTo(countersAfterConsequence), "The continued frame must not execute its earlier Defeat again.");
        }

        [Test]
        public void Howl_MovementActionIsCappedAtStart_AndDoesNotGrowAfterLeavingTheAura()
        {
            var f = new Fixture(GarrusCardContent.ChillingHowl, enemySpace: new HexCoordinate(3, 0));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Round.TryReleaseTurn().Accepted, Is.True);
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(GarrusCardContent.AllDefinitions, f.Cards, Blue, f.EnemySource.Id)
                .Single(value => value.ActionDefinitionId.Equals(GarrusCardContent.SecondaryMovement));
            Assert.That(f.Round.TryStartMovement(new ActionResolutionId("capped-move"), offer).Accepted, Is.True);
            var choice = f.Authority.PendingChoice;
            Assert.That(choice.RemainingAllowance, Is.EqualTo(2));
            Assert.That(f.Round.TryAnswerMovementChoice(MovementChoiceCommand.SelectDestination(choice.ResolutionId,
                choice.ChoiceId, new HexCoordinate(4, 0))).Accepted, Is.True);
            Assert.That(f.Authority.PendingChoice.RemainingAllowance, Is.EqualTo(1));
            f.RoundTrip();
            Assert.That(f.Authority.PendingChoice.RemainingAllowance, Is.EqualTo(1));
        }

        [TestCase("trace", 1)]
        [TestCase("chase", 2)]
        [TestCase("hunt-down", 3)]
        public void PursuitCards_MoveBeforeBindingHeroAndKeepThePrintedAttack(string card, int move)
        {
            var f = new Fixture(new CardDefinitionId("garrus-" + card), enemySpace: new HexCoordinate(move + 1, 0), discard: true);
            f.Option("advance");
            f.Pick(EffectValue.Space(new HexCoordinate(move, 0)));
            f.RoundTrip();
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Authority.PendingDefenseChoice.AttackTotal, Is.EqualTo(card == "trace" ? 3 : 4));
            f.Defend();
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [TestCase("trace")]
        [TestCase("chase")]
        [TestCase("hunt-down")]
        public void PursuitWithoutDiscard_CannotStartWhenBothBranchesHaveNoAdjacentTarget(string card)
        {
            new Fixture(new CardDefinitionId("garrus-" + card), enemySpace: new HexCoordinate(2, 0), expectStart: false);
        }

        [TestCase("blunt-force")]
        [TestCase("send-flying")]
        public void PushCards_AttackFirst_ThenZeroMoveCanPushTheSurvivingHero(string card)
        {
            var f = new Fixture(new CardDefinitionId("garrus-" + card));
            Assert.That(f.Choice, Is.Null);
            f.Target();
            Assert.That(f.Authority.PendingDefenseChoice.AttackTotal, Is.EqualTo(7));
            f.Defend();
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("may-advance"));
            f.Answer(f.Choice.ConfirmInvocation);
            f.Pick(EffectValue.Space(new HexCoordinate(0, 0)));
            f.RoundTrip();
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Board.TryGetHeroCoordinate(Blue, out var space), Is.True);
            Assert.That(space, Is.EqualTo(new HexCoordinate(4, 0)));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void AngryStrike_ReadsTheLiveDiscardCountFromItsRealDefinition()
        {
            var f = new Fixture(GarrusCardContent.AngryStrike, discard: true);
            f.Target();
            Assert.That(f.Authority.PendingDefenseChoice.AttackTotal, Is.EqualTo(5));
            f.Defend();
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        private sealed class Fixture
        {
            internal CardInstance[] Cards;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal CardActionResolutionAuthority Authority;
            internal ActiveEffectLifecycle Timing;
            internal readonly MatchContentCatalogs Catalogs = GarrusPrintedSetTests.Catalogs();
            internal CardInstance Source => Cards.Single(card => card.Id.Value == "source");
            internal CardInstance EnemySource => Cards.Single(card => card.Id.Value == "enemy-source");
            internal CardInstance EnemyDefense => Cards.Single(card => card.Id.Value == "enemy-defense");
            internal CardInstance Borrowed => Cards.Single(card => card.Id.Value == "borrowed");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority.PendingEffectChoice;
            internal Fixture(CardDefinitionId id, CardDefinitionId resolved = default, int enemyHand = 1,
                HexCoordinate? enemySpace = null, bool discard = false, bool expectStart = true)
            {
                var card = GarrusCardContent.Definition(id);
                var enemy = GarrusCardContent.Definition(id.Equals(GarrusCardContent.ChillingHowl)
                    ? GarrusCardContent.Trace : GarrusCardContent.HeavyPilum);
                var fury = CardInstance.CreateInDeck(new CardInstanceId("ultimate"), GarrusCardContent.Definition(GarrusCardContent.BattleFury).Reference, Red);
                if (resolved.IsDefined) CardLifecycleAuthority.TryUnlockUltimateFromDeck(fury);
                Cards = new[] { CardInstance.CreateInHand(new CardInstanceId("source"), card.Reference, Red),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-source"), enemy.Reference, Blue), fury }
                    .Concat(enemyHand > 0 ? new[] { CardInstance.CreateInHand(new CardInstanceId("enemy-defense"),
                        GarrusCardContent.Definition(GarrusCardContent.SendFlying).Reference, Blue) } : Array.Empty<CardInstance>())
                    .Concat(resolved.IsDefined || discard ? new[] { new CardInstance(new CardInstanceId("borrowed"),
                        GarrusCardContent.Definition(resolved.IsDefined ? resolved : GarrusCardContent.Menace).Reference,
                        Red, new CardStateFacts(discard ? CardLocation.DiscardPile : CardLocation.TurnArea, true,
                            CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp)) } : Array.Empty<CardInstance>()).ToArray();
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("garrus-round-probe"), 1,
                    Enumerable.Range(-6, 13).SelectMany(q => Enumerable.Range(-6, 13).Select(r => new HexCoordinate(q, r)))),
                    new[] { new HeroOccupancy(Red, new HexCoordinate(0, 0)), new HeroOccupancy(Blue, enemySpace ?? new HexCoordinate(1, 0)) },
                    Array.Empty<ObstacleOccupancy>());
                Timing = new ActiveEffectLifecycle();
                var markers = new MarkerAttachmentRegister();
                Authority = new CardActionResolutionAuthority(Catalogs, Board, markers);
                Round = new RoundResolutionAuthority(GarrusCardContent.AllDefinitions, Cards, StagedBoardParticipants.Create(),
                    new TieBreakerState(TeamSide.Red), 1, RoundResolutionComposition.ForMatch(Authority, Board, Timing, markers, null, null));
                Assert.That(Round.TryCommitCard(Red, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Blue, EnemySource.Id).Accepted, Is.True);
                var primary = card.ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(GarrusCardContent.AllDefinitions, Cards, Red, Source.Id)
                    .Single(value => value.ActionDefinitionId.Equals(primary.Id));
                var result = primary.Kind == ActionKind.Attack
                    ? Round.TryStartAttack(new ActionResolutionId("garrus-action"), offer)
                    : Round.TryStartSkill(new ActionResolutionId("garrus-action"), offer);
                Assert.That(result.Accepted, Is.EqualTo(expectStart), result.RejectionReason + "/" + result.CardActionRejectionReason);
            }
            internal void Target()
            {
                var choice = Authority.PendingAttackTargetChoice;
                Assert.That(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, Blue)).Accepted, Is.True);
            }
            internal void Defend()
            {
                var choice = Authority.PendingDefenseChoice;
                Assert.That(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(choice.ResolutionId,
                    choice.ChoiceId, new[] { EnemyDefense.Id })).Accepted, Is.True);
            }
            internal void Option(string id) => Select(Choice.SelectInvocations.Single(command => command.Candidate.OptionNodeId.Value == id));
            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value)));
            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
            }
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(Catalogs, bytes, GarrusCardContent.AllDefinitions,
                    new[] { Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round; Board = restored.Board; Authority = restored.CardActions;
                Cards = restored.Cards.ToArray(); Timing = restored.Round.Timing;
            }
        }
    }
}
