using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherBorrowJourneyTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly EffectCapabilityReference PlaceOriginal =
            new EffectCapabilityReference(new EffectCapabilityId("mind-grip-test-original-token"), 1);
        private static readonly TokenDefinition Original = new TokenDefinition(
            new TokenDefinitionId("mind-grip-test-original"), 1, "Original", 2,
            Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfRoundCleanup);

        [Test]
        public void PreviousSlotBorrow_UsesActorSeatAndIllusionKind_AcrossRestore()
        {
            var f = new Fixture();
            Assert.That(f.Round.TurnIndex, Is.EqualTo(2));
            Assert.That(f.Round.TurnSlots.Single(slot =>
                slot.TurnIndex == 1).Assignments.Single(a =>
                a.HeroId.Equals(Enemy)).CardInstanceId, Is.EqualTo(f.Previous.Id));

            f.StartMindGrip();
            Assert.That(f.Choice.ChooserId, Is.EqualTo(Actor));
            f.Answer(EffectValue.Hero(Enemy));
            Assert.That(f.Choice.Candidates.Select(c => c.Value),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Integer(0), EffectValue.Integer(1),
                    EffectValue.Integer(2), EffectValue.Integer(3),
                    EffectValue.Integer(4)
                }));
            f.RoundTrip();
            f.Answer(EffectValue.Integer(0));
            Assert.That(f.Choice.ChooserId, Is.EqualTo(Actor),
                "Borrowing changes the source card, not the actor or decision owner.");
            Assert.That(f.Choice.Candidates.Any(c =>
                c.Value.Equals(EffectValue.Space(new HexCoordinate(1, 0)))), Is.True);
            f.RoundTrip();
            f.Answer(EffectValue.Space(new HexCoordinate(1, 0)));
            Assert.That(f.Board.PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(NebKherComponentContent.Illusion.Reference));
            Assert.That(f.Board.PlacedTokens.Single().ControllerHeroId, Is.EqualTo(Actor));
            Assert.That(f.Previous.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Previous.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Choice, Is.Null);
        }

        [Test]
        public void PreviousSlotPrintedSecondaryMovement_IsPerformedByBorrowerWithoutMovingCard()
        {
            var f = new Fixture();
            f.StartMindGrip();
            f.Answer(EffectValue.Hero(Enemy));
            f.Answer(EffectValue.Integer(1));
            var movement = f.Round.CurrentTurnAuthority.PendingMovementChoice;
            Assert.That(movement, Is.Not.Null);
            f.RoundTrip();
            movement = f.Round.CurrentTurnAuthority.PendingMovementChoice;
            Assert.That(f.Round.TryAnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    movement.ResolutionId, movement.ChoiceId,
                    new HexCoordinate(1, 0))).Accepted, Is.True);
            movement = f.Round.CurrentTurnAuthority.PendingMovementChoice;
            if (movement != null)
                Assert.That(f.Round.TryAnswerMovementChoice(
                    MovementChoiceCommand.Stop(movement.ResolutionId, movement.ChoiceId)).Accepted,
                    Is.True);
            Assert.That(f.Board.TryGetHeroCoordinate(Actor, out var actorAt), Is.True);
            Assert.That(actorAt, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(f.Board.TryGetHeroCoordinate(Enemy, out var enemyAt), Is.True);
            Assert.That(enemyAt, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(f.Previous.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Previous.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PreviousSlotPrimaryDefenseExecutesItsPrintedTextWithoutDiscardingCard()
        {
            var f = new Fixture();
            f.StartMindGrip();
            f.Answer(EffectValue.Hero(Enemy));
            f.Answer(EffectValue.Integer(2));
            Assert.That(f.Choice.ChooserId, Is.EqualTo(Actor));
            f.RoundTrip();
            f.Answer(EffectValue.Space(new HexCoordinate(1, 0)));
            Assert.That(f.Board.PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(NebKherComponentContent.Illusion.Reference));
            Assert.That(f.Previous.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Previous.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PreviousSlotMarkerTextIsSuppressedWithoutOpeningRecipientChoice()
        {
            var f = new Fixture();
            f.StartMindGrip();
            f.Answer(EffectValue.Hero(Enemy));
            f.RoundTrip();
            f.Answer(EffectValue.Integer(3));
            Assert.That(f.Choice, Is.Null);
            Assert.That(f.Round.Markers.Attachments, Is.Empty);
            Assert.That(f.Previous.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PreviousSlotAttackStartsForBorrowerWithoutReplayingEnemyCard()
        {
            var f = new Fixture();
            f.StartMindGrip();
            f.Answer(EffectValue.Hero(Enemy));
            f.Answer(EffectValue.Integer(4));
            var target = f.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null);
            f.RoundTrip();
            target = f.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(target.LegalTargetHeroIds, Has.Member(Enemy));
            Assert.That(f.Round.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy))
                .Accepted, Is.True);
            Assert.That(f.Previous.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Previous.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void MindGripChoiceReplayMatchesEveryRestoredCommandPrefix()
        {
            var answers = new[]
            {
                EffectValue.Hero(Enemy),
                EffectValue.Integer(0),
                EffectValue.Space(new HexCoordinate(1, 0))
            };
            var live = new Fixture();
            live.StartMindGrip();
            var expected = new List<string>
            {
                AuthoritativeSnapshotSerializer.Serialize(live.Round, live.Board)
            };
            foreach (var answer in answers)
            {
                live.Answer(answer);
                expected.Add(AuthoritativeSnapshotSerializer.Serialize(live.Round, live.Board));
            }

            for (var prefix = 0; prefix <= answers.Length; prefix++)
            {
                var replay = new Fixture();
                replay.StartMindGrip();
                for (var index = 0; index <= answers.Length; index++)
                {
                    if (index == prefix)
                        replay.RoundTrip();
                    Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                        replay.Round, replay.Board), Is.EqualTo(expected[index]),
                        "restored command prefix " + prefix + ", step " + index);
                    if (index < answers.Length)
                        replay.Answer(answers[index]);
                }
            }
        }

        private sealed class Fixture
        {
            internal readonly CardDefinition[] Definitions;
            internal CardInstance[] Cards;
            internal readonly MatchContentCatalogs Catalogs;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal CardInstance Previous => Cards.Single(card => card.Id.Value == "previous");
            internal CardInstance Source => Cards.Single(card => card.Id.Value == "source");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;

            internal Fixture()
            {
                var placeRoot = new EffectNodeId("place");
                var placeText = new EffectDefinition(
                    new EffectDefinitionId("mind-grip-test-place-text"), "rev-1", placeRoot,
                    new[]
                    {
                        new EffectNodeDefinition(placeRoot, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory, PlaceOriginal)
                    });
                var markerRoot = new EffectNodeId("marker");
                var markerText = new EffectDefinition(
                    new EffectDefinitionId("mind-grip-test-marker-text"), "rev-1", markerRoot,
                    new[]
                    {
                        new EffectNodeDefinition(markerRoot, EffectNodeKind.Capability,
                            EffectFailurePolicy.IfAble,
                            SyntheticComponentCapabilities.GiveMarkerV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding("holder", EffectValueKind.Hero)
                            })
                    });
                var target = new EffectNodeId("target");
                var borrow = new EffectNodeId("borrow");
                var sourceText = new EffectDefinition(
                    new EffectDefinitionId("mind-grip-test-source-text"), "rev-1",
                    new EffectNodeId("root"), new[]
                    {
                        new EffectNodeDefinition(new EffectNodeId("root"),
                            EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { target, borrow }),
                        new EffectNodeDefinition(target, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory,
                            SyntheticTargetingCapabilities.RangeHeroTargetV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromClosedVocabulary(
                                    "relation", SourceTeamRelation.OpposingSource),
                                EffectInputBinding.FromClosedVocabulary(
                                    "activity", RuleActivity.Skill),
                                EffectInputBinding.FromLiteral(
                                    SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                    EffectValue.Boolean(false)),
                                EffectInputBinding.FromLiteral(
                                    EffectSelectionDeclaration.ConsumerParameterName,
                                    EffectValue.Node(borrow))
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding("chosenHero", EffectValueKind.Hero),
                                new EffectOutputBinding("chosenHeroSpace", EffectValueKind.Coordinate)
                            }),
                        new EffectNodeDefinition(borrow, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory,
                            SyntheticPreviousSlotBorrowCapabilities.BorrowPrintedActionV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticPreviousSlotBorrowCapabilities.EnemyParameter,
                                    target, "chosenHero", EffectValueKind.Hero),
                                EffectInputBinding.FromLiteral(
                                    SyntheticPreviousSlotBorrowCapabilities.SubstituteTokenKindParameter,
                                    EffectValue.TokenKind(NebKherComponentContent.Illusion.Id))
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                                    EffectValueKind.CardInstance),
                                new EffectOutputBinding(
                                    SyntheticPreviousSlotBorrowCapabilities.BorrowedActionIndexOutput,
                                    EffectValueKind.Integer)
                            })
                    });
                var plain = new ActionDefinition(new ActionDefinitionId("plain-move"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 0) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default);
                CardDefinition Card(string id, int initiative, params ActionDefinition[] actions) =>
                    new CardDefinition(new CardDefinitionId(id), "rev-1", id,
                        new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                        CardProgressionProfile.BasicCard(CardColor.Gold), actions);
                Definitions = new[]
                {
                    Card("red-first", 1, plain),
                    Card("previous-blue", 12,
                        new ActionDefinition(new ActionDefinitionId("previous-place"),
                            ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 2) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default, placeText),
                        new ActionDefinition(new ActionDefinitionId("previous-move"),
                            ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, true, true),
                            AttackRangeKind.None, default),
                        new ActionDefinition(new ActionDefinitionId("previous-defense"),
                            ActionKind.Defense, ActionDefinitionOrigin.PrintedPrimary,
                            new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Defense, 3),
                                new PrintedValueDefinition(PrintedValueChannel.Radius, 2)
                            },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                            AttackRangeKind.None,
                            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                            placeText, PrintedTextComposition.None,
                            PrintedDefenseResolution.Value, CardTextTiming.DefenseAction),
                        new ActionDefinition(new ActionDefinitionId("previous-marker"),
                            ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            Array.Empty<PrintedValueDefinition>(),
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default, markerText),
                        new ActionDefinition(new ActionDefinitionId("previous-attack"),
                            ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                            new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                                new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                            },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.Ranged, default)),
                    Card("mind-grip-source", 12,
                        new ActionDefinition(new ActionDefinitionId("mind-grip-skill"),
                            ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 5) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default, sourceText)),
                    Card("blue-wait", 1, plain)
                };
                Cards = new[]
                {
                    InHand("red-first", 0, Actor),
                    InHand("previous", 1, Enemy),
                    InHand("source", 2, Actor),
                    InHand("blue-wait", 3, Enemy)
                };
                var defaults = MatchContentCatalogs.ForThisBuild();
                Catalogs = new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(new[]
                        {
                            SyntheticTokenCapabilities.PlaceHandler(
                                PlaceOriginal, Original, PrintedValueChannel.Radius)
                        })),
                    new ComponentDefinitionCatalog(
                        new[] { Original, NebKherComponentContent.Illusion },
                        new[] { SyntheticComponentDefinitions.FocusDefinition }));
                Assert.That(EffectDefinitionValidator.Validate(sourceText, Catalogs.Capabilities),
                    Is.Empty);
                Assert.That(EffectDefinitionValidator.Validate(placeText, Catalogs.Capabilities),
                    Is.Empty);
                Assert.That(EffectDefinitionValidator.Validate(markerText, Catalogs.Capabilities),
                    Is.Empty);
                Board = new AdjacentMoveAuthority(
                    Enumerable.Range(-2, 8).Select(q => new HexCoordinate(q, 0)).ToArray(),
                    new[]
                    {
                        new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(2, 0))
                    }, Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                Round = NewRound(Board);
                Assert.That(Round.TryCommitCard(Actor, Cards[0].Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Enemy, Cards[1].Id).Accepted, Is.True);
                for (var guard = 0;
                     Round.Phase == RoundResolutionPhase.ResolvingTurn && guard < 8;
                     guard++)
                {
                    var turn = Round.CurrentTurnAuthority;
                    var result = turn.PendingTurnRelease != null
                        ? Round.TryReleaseTurn()
                        : Round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                    Assert.That(result.Accepted, Is.True);
                }
                Assert.That(Round.TurnIndex, Is.EqualTo(2));
                Assert.That(Round.TryCommitCard(Actor, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Enemy, Cards[3].Id).Accepted, Is.True);
            }

            private CardInstance InHand(string id, int definition, HeroId owner) =>
                CardInstance.CreateInHand(new CardInstanceId(id),
                    Definitions[definition].Reference, owner);

            private RoundResolutionAuthority NewRound(AdjacentMoveAuthority board) =>
                new RoundResolutionAuthority(Definitions, Cards,
                    StagedBoardParticipants.Create(), new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(
                        new CardActionResolutionAuthority(Catalogs, board), board,
                        new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                        null, null, null, null, null, null));

            internal void StartMindGrip()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    Definitions, Cards, Actor, Source.Id).Single();
                var result = Round.TryStartSkill(new ActionResolutionId("mind-grip-live"), offer);
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason +
                    "/" + result.CardActionRejectionReason);
            }

            internal void Answer(EffectValue answer)
            {
                var command = Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Equals(answer));
                var choiceId = Choice.ChoiceId;
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason);
                if (Choice != null && Choice.ChoiceId.Equals(choiceId) &&
                    Choice.ConfirmInvocation != null)
                    Assert.That(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation).Accepted,
                        Is.True);
            }

            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    Catalogs, bytes, Definitions, new[] { Board.MapDefinition },
                    Array.Empty<QuickGameMinionSetupState>());
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                    restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }
        }
    }
}
