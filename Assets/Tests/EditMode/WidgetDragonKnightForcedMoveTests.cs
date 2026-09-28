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
    /// <summary>
    /// Milestone 8.116, Whisper Rulings 16b-16c (user, 2026-09-28): Swift Justice's forced move is the
    /// defender's Movement action, so Widget's Dragon Knight - "Each time after you perform a movement
    /// action, you may perform the primary action on your face-up Skill card" - fires off it, right
    /// after the move and before the rest of Whisper's Attack. Widget answers on his own seat, performs
    /// the borrowed Skill inside Whisper's turn, and Whisper's page then finishes.
    /// </summary>
    public sealed class WidgetDragonKnightForcedMoveTests
    {
        private static readonly HeroId Whisper = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Widget = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("widget-dragon-knight-forced-move");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate WidgetAt = H(0, 2);

        private static CardDefinition WidgetFace(CardDefinitionId id) =>
            WidgetCardContent.DealtDefinitions.Single(definition => definition.Id.Equals(id));

        [Test]
        public void TheForcedMove_FiresDragonKnightRightAfterTheMove_BeforeWhispersAttackEnds()
        {
            var round = new Round(withDragonKnight: true);
            round.AttackDefendAndMove();

            Assert.That(round.Chooser, Is.EqualTo(Widget),
                "Ruling 16c: Widget is asked right after his forced Movement action.");
            Assert.That(round.PendingNode, Is.Not.Null);
            Assert.That(round.WhispersCardResolved, Is.False, "Whisper's Attack has not ended yet.");
            Assert.That(round.PyroOnBoard, Is.False);

            round.AcceptAndPlacePyro();

            Assert.That(round.PyroOnBoard, Is.True, "Widget performed Dragon Bond's primary inside Whisper's turn.");
            Assert.That(round.WhispersAttackIsOver, Is.True,
                "and Whisper's page was handed back and finished afterwards.");
        }

        [Test]
        public void DecliningDragonKnight_LetsWhispersAttackFinishAtOnce()
        {
            var round = new Round(withDragonKnight: true);
            round.AttackDefendAndMove();
            Assert.That(round.Chooser, Is.EqualTo(Widget));

            round.DeclineEverythingOfWidgets();

            Assert.That(round.PyroOnBoard, Is.False);
            Assert.That(round.WhispersAttackIsOver, Is.True);
        }

        [Test]
        public void WithoutDragonKnight_NothingIsAskedAfterTheMove()
        {
            var round = new Round(withDragonKnight: false);
            round.AttackDefendAndMove();

            Assert.That(round.PendingNode, Is.Null, "no Ultimate watches Widget's Movement actions.");
            Assert.That(round.WhispersAttackIsOver, Is.True);
        }

        [Test]
        public void ThePausedTrigger_RestoresOnWidgetsQuestion_AndConverges()
        {
            var round = new Round(withDragonKnight: true);
            round.AttackDefendAndMove();
            round.AssertRestoreConvergesAfterAcceptingAndPlacing();
        }

        // --- harness ------------------------------------------------------------------------------

        private static readonly CardDefinition WidgetGuard = new CardDefinition(
            new CardDefinitionId("dragon-knight-guard"), "rev-1", "TEST - Guard",
            new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)),
                new ActionDefinition(new ActionDefinitionId("secondary-movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private sealed class Round
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly MatchContentCatalogs catalogs;

            internal Round(bool withDragonKnight)
            {
                var participants = StagedBoardParticipants.Create();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces),
                    new[] { new HeroOccupancy(Whisper, H(0, 0)), new HeroOccupancy(Widget, WidgetAt) },
                    Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());

                var face = WhisperCardContent.Definition(WhisperCardContent.SwiftJustice);
                var bond = WidgetFace(WidgetCardContent.DragonBond);
                var knight = WidgetFace(WidgetCardContent.DragonKnight);
                source = CardInstance.CreateInHand(new CardInstanceId("whisper-source"), face.Reference, Whisper);
                var skill = CardInstance.CreateInHand(new CardInstanceId("widget-bond"), bond.Reference, Widget);
                var guard = CardInstance.CreateInHand(new CardInstanceId("widget-guard"), WidgetGuard.Reference, Widget);
                var all = new[] { source, skill, guard }.ToList();
                if (withDragonKnight)
                {
                    var ultimate = CardInstance.CreateInDeck(new CardInstanceId("widget-knight"), knight.Reference, Widget);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                    all.Add(ultimate);
                }

                definitions = new[] { face, bond, knight, WidgetGuard };
                instances = all.ToArray();
                catalogs = MatchContentCatalogs.ForThisBuild();
                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(
                        new CardActionResolutionAuthority(catalogs, board, markers),
                        board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Whisper, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Widget, skill.Id).Accepted, Is.True,
                    "Widget's face-up Skill for the turn is Dragon Bond.");
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Whisper));
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal HeroId Chooser => Turn.PendingEffectChoice?.ChooserId ?? default;

            internal bool PyroOnBoard => board.PlacedTokens.Any(token =>
                token.Definition.Id.Equals(WidgetComponentContent.PyroDefinition.Id));

            internal bool WhispersCardResolved =>
                instances.Single(card => card.Id.Equals(source.Id)).State.ResolutionStatus ==
                CardResolutionStatus.Resolved;

            internal bool WhispersAttackIsOver =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null && Turn.PendingMovementChoice == null;

            /// <summary>Swift Justice's ranged-hero bullet at Widget, answered with the Guard, and the forced move.</summary>
            internal void AttackDefendAndMove()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, Whisper, source.Id)
                    .Single(value => value.ActionDefinitionId.Value == "primary-ranged-attack");
                Assert.That(round.TryStartAttack(new ActionResolutionId("whisper-attack"), offer).Accepted, Is.True);

                var modal = Turn.PendingEffectChoice;
                Assert.That(round.TryAnswerEffectChoice(modal.SelectInvocations.Single(command =>
                    command.Candidate.OptionNodeId.Value == "modal-ranged-hero")).Accepted, Is.True);
                ConfirmIfStillOpen(modal);

                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "p.14 asks whom");
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    target.ResolutionId, target.ChoiceId, Widget)).Accepted, Is.True);

                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null);
                var guard = defense.LegalDefenseOffers.First(value =>
                    value.SourceDefinitionReference.Equals(WidgetGuard.Reference));
                Assert.That(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, guard.SourceCardInstanceId)).Accepted, Is.True);

                var move = Turn.PendingEffectChoice;
                Assert.That(move?.ChooserId, Is.EqualTo(Widget), "the forced move is Widget's (Ruling 16)");
                var before = SpaceOf(Widget);
                Answer(move.SelectInvocations.First());
                ConfirmIfStillOpen(move);
                Assert.That(SpaceOf(Widget), Is.Not.EqualTo(before), "Widget moved the full distance.");
            }

            /// <summary>Takes Dragon Knight, borrows Dragon Bond, takes its Pyro bullet and places Pyro.</summary>
            internal void AcceptAndPlacePyro() => AcceptAndPlacePyro(round, Turn);

            private void AcceptAndPlacePyro(RoundResolutionAuthority target, TurnResolutionAuthority turn)
            {
                for (var step = 0; step < 8 && turn.PendingEffectChoice != null; step++)
                {
                    var choice = turn.PendingEffectChoice;
                    Assert.That(choice.ChooserId, Is.EqualTo(Widget), "every question here is Widget's: " +
                        choice.NodeId.Value);
                    var select = choice.SelectInvocations.FirstOrDefault();
                    var command = select ?? choice.ConfirmInvocation;
                    Assert.That(command, Is.Not.Null, choice.NodeId.Value);
                    Assert.That(target.TryAnswerEffectChoice(command).Accepted, Is.True, choice.NodeId.Value);
                    var still = turn.PendingEffectChoice;
                    if (still != null && still.ChoiceId.Equals(choice.ChoiceId) && still.ConfirmInvocation != null &&
                        still.SelectedCandidates.Count > 0)
                    {
                        Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                    }
                }
            }

            internal void DeclineEverythingOfWidgets()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null, "Dragon Knight is a may.");
                Answer(choice.DeclineInvocation);
            }

            internal void AssertRestoreConvergesAfterAcceptingAndPlacing()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition },
                    Array.Empty<QuickGameMinionSetupState>());
                Assert.That(restored.Round.CurrentTurnAuthority.PendingEffectChoice?.NodeId.Value,
                    Is.EqualTo(PendingNode), "the restored match stands on Widget's question.");

                AcceptAndPlacePyro();
                AcceptAndPlacePyro(restored.Round, restored.Round.CurrentTurnAuthority);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)),
                    "the restored trigger and borrowed Skill replay to the same authoritative bytes.");
                Assert.That(PyroOnBoard, Is.True);
                Assert.That(WhispersAttackIsOver, Is.True);
            }

            private HexCoordinate SpaceOf(HeroId hero)
            {
                Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out var at), Is.True);
                return at;
            }

            private void Answer(EffectChoiceCommand command) =>
                Assert.That(round.TryAnswerEffectChoice(command).Accepted, Is.True);

            private void ConfirmIfStillOpen(PendingEffectChoice asked)
            {
                var still = Turn.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
                {
                    Answer(still.ConfirmInvocation);
                }
            }
        }
    }
}
