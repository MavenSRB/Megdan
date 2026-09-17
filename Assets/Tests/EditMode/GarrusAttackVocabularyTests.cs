using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class GarrusAttackVocabularyTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly ActionDefinitionId Action = new ActionDefinitionId("garrus-clause-probe");
        private static readonly CardDefinitionId Card = new CardDefinitionId("garrus-clause-test-card");

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Pursuit_DiscardPermitsBeforeAttackMove_ThenBindsOnlyAnAdjacentHero(int distance)
        {
            var probe = new Probe(GarrusEffectContent.PursuitAttack(Card, "rev-1", distance),
                ActionKind.Attack, new HexCoordinate(distance + 1, 0), ownDiscard: true);
            probe.Option("advance");
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("move"));
            probe.Pick(EffectValue.Space(new HexCoordinate(distance, 0)));
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("hero-target"));
            Assert.That(probe.Choice.Candidates.Select(value => value.Value.Kind), Has.All.EqualTo(EffectValueKind.Hero));
            probe.Pick(EffectValue.Hero(Blue));
            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.Step.BoundAttackTarget, Is.EqualTo(AttackTarget.Hero(Blue)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pursuit_WithoutOwnDiscard_SkipsOnlyTheMove_NotTheHeroTarget(bool foreignDiscard)
        {
            var probe = new Probe(GarrusEffectContent.PursuitAttack(Card, "rev-1", 3),
                ActionKind.Attack, new HexCoordinate(1, 0), foreignDiscard: foreignDiscard, minion: true);
            probe.Option("advance");
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("hero-target"));
            Assert.That(probe.Choice.Candidates.Select(value => value.Value.Kind), Has.All.EqualTo(EffectValueKind.Hero));
            probe.Pick(EffectValue.Hero(Blue));
            Assert.That(probe.Step.BoundAttackTarget, Is.EqualTo(AttackTarget.Hero(Blue)));
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(Red)).Coordinate,
                Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void Pursuit_OrdinaryArmTargetsUnitsAndDoesNotMove_EvenWithDiscard()
        {
            var probe = new Probe(GarrusEffectContent.PursuitAttack(Card, "rev-1", 3),
                ActionKind.Attack, new HexCoordinate(1, 0), ownDiscard: true, minion: true);
            probe.Option("ordinary");
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("unit-target"));
            Assert.That(probe.Effect.Nodes.Single(value => value.Id.Value == "unit-target").Inputs
                .Single(value => value.ParameterName == "unitKind").Literal.IntegerValue,
                Is.EqualTo((int)RosterUnitKind.Unit));
            var minion = probe.Board.Minions.Single().MinionId;
            probe.Pick(EffectValue.Minion(minion));
            Assert.That(probe.Step.BoundAttackTarget, Is.EqualTo(AttackTarget.Minion(minion)));
        }

        [Test]
        public void PostAttack_DeclineDoesNotMoveOrPush_WhileZeroDoesPush()
        {
            var declined = new Probe(GarrusEffectContent.MovePushAfterAttack(Card, "rev-1", 1),
                ActionKind.Skill, new HexCoordinate(1, 0));
            declined.Answer(declined.Choice.DeclineInvocation);
            Assert.That(declined.Step.IsComplete, Is.True);
            Assert.That(declined.Board.Occupancies.Single(value => value.HeroId.Equals(Blue)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)));
            var accepted = new Probe(GarrusEffectContent.MovePushAfterAttack(Card, "rev-1", 1),
                ActionKind.Skill, new HexCoordinate(1, 0));
            accepted.Answer(accepted.Choice.ConfirmInvocation);
            accepted.Pick(EffectValue.Space(new HexCoordinate(0, 0)));
            Assert.That(accepted.Choice.NodeId.Value, Is.EqualTo("push-target"));
            accepted.Pick(EffectValue.Hero(Blue));
            Assert.That(accepted.Step.IsComplete, Is.True);
            Assert.That(accepted.Board.Occupancies.Single(value => value.HeroId.Equals(Blue)).Coordinate,
                Is.EqualTo(new HexCoordinate(4, 0)));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PostAttack_MovePauseAndNamedPush_RoundTripAndContinueIdentically(int maximum)
        {
            var probe = new Probe(GarrusEffectContent.MovePushAfterAttack(Card, "rev-1", maximum),
                ActionKind.Skill, new HexCoordinate(2, 0), terrain: new HexCoordinate(3, 0));
            probe.Answer(probe.Choice.ConfirmInvocation);
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("move"));
            var ledger = new HeroCoinLedger(probe.Participants);
            var actions = CardActionResolutionAuthority.Restore(probe.Catalogs, probe.Board,
                CardActionResolutionRestoreState.WithPendingSkill(probe.Step.Frame,
                    HeroCoinLedgerRestoreStateHolder.Present(ledger.CaptureRestoreState())),
                null, null, null, probe.Definitions, probe.Cards, probe.Participants,
                new MarkerAttachmentRegister(), RuleModificationSet.Empty);
            var round = new RoundResolutionAuthority(probe.Definitions, probe.Cards, probe.Participants,
                new TieBreakerState(TeamSide.Red), 1, RoundResolutionComposition.ForCardActionsOnly(actions));
            Assert.That(round.TryCommitCard(Red, probe.Cards[0].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Blue, probe.Cards[1].Id).Accepted, Is.True);
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, probe.Board);
            var replay = AuthoritativeSnapshotSerializer.Deserialize(probe.Catalogs, payload, probe.Definitions,
                new[] { probe.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board), Is.EqualTo(payload));
            foreach (var branch in new[] { round, replay.Round })
            {
                PickRound(branch, EffectValue.Space(new HexCoordinate(1, 0)));
                Assert.That(branch.CurrentTurnAuthority.PendingEffectChoice.NodeId.Value, Is.EqualTo("push-target"));
            }
            var targetPayload = AuthoritativeSnapshotSerializer.Serialize(round, probe.Board);
            var targetReplay = AuthoritativeSnapshotSerializer.Deserialize(probe.Catalogs, targetPayload, probe.Definitions,
                new[] { probe.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            foreach (var branch in new[] { round, replay.Round, targetReplay.Round })
            {
                PickRound(branch, EffectValue.Hero(Blue));
            }
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, probe.Board)));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(targetReplay.Round, targetReplay.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, probe.Board)));
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(Blue)).Coordinate,
                Is.EqualTo(new HexCoordinate(5, 0)), "Push 3 ignores the terrain at 3,0.");
        }

        [Test]
        public void PostAttack_NoReachableHero_AppliesNeitherMoveNorPush()
        {
            var probe = new Probe(GarrusEffectContent.MovePushAfterAttack(Card, "rev-1", 1),
                ActionKind.Skill, new HexCoordinate(6, 0));
            if (probe.Choice != null) probe.Answer(probe.Choice.ConfirmInvocation);
            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(Red)).Coordinate,
                Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(Blue)).Coordinate,
                Is.EqualTo(new HexCoordinate(6, 0)));
        }

        [Test]
        public void PostAttack_ExplicitlyChoosesAmongAdjacentHeroes_IndependentlyOfTheAttackTarget()
        {
            var second = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            var probe = new Probe(GarrusEffectContent.MovePushAfterAttack(Card, "rev-1", 2),
                ActionKind.Skill, new HexCoordinate(1, 0), secondEnemy: true);
            probe.Answer(probe.Choice.ConfirmInvocation);
            probe.Pick(EffectValue.Space(new HexCoordinate(0, 0)));
            Assert.That(probe.Choice.Candidates.Select(value => value.Value.HeroId), Is.EquivalentTo(new[] { Blue, second }));
            probe.Pick(EffectValue.Hero(second));
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(Blue)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)), "The earlier Attack target need not be the Push target.");
            Assert.That(probe.Board.Occupancies.Single(value => value.HeroId.Equals(second)).Coordinate,
                Is.EqualTo(new HexCoordinate(0, 4)));
        }

        private static void PickRound(RoundResolutionAuthority round, EffectValue value)
        {
            var choice = round.CurrentTurnAuthority.PendingEffectChoice;
            var select = choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value));
            Assert.That(round.TryAnswerEffectChoice(select).Accepted, Is.True);
            Assert.That(round.TryAnswerEffectChoice(select).Accepted, Is.False, "Stale repeat input is rejected.");
            var next = round.CurrentTurnAuthority.PendingEffectChoice;
            if (next != null && next.ChoiceId.Equals(choice.ChoiceId))
                Assert.That(round.TryAnswerEffectChoice(next.ConfirmInvocation).Accepted, Is.True);
        }

        private sealed class Probe
        {
            internal readonly EffectDefinition Effect;
            internal readonly MatchParticipants Participants;
            internal readonly AdjacentMoveAuthority Board;
            internal readonly MatchContentCatalogs Catalogs;
            internal readonly CardDefinition[] Definitions;
            internal readonly CardInstance[] Cards;
            private readonly EffectResolutionContext context;
            internal EffectResolutionStepResult Step;
            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

            internal Probe(EffectDefinition effect, ActionKind kind, HexCoordinate enemy,
                bool ownDiscard = false, bool foreignDiscard = false, HexCoordinate? terrain = null,
                bool minion = false, bool secondEnemy = false)
            {
                Effect = effect;
                Participants = secondEnemy ? StagedBoardParticipants.CreateWithBlueAlly() : StagedBoardParticipants.Create();
                var defaults = MatchContentCatalogs.ForThisBuild();
                var catalog = new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(SyntheticDisplacementCapabilities.DeferredHandlers()));
                Catalogs = new MatchContentCatalogs(catalog, defaults.Components);
                Assert.That(EffectDefinitionValidator.Validate(effect, catalog), Is.Empty);
                var action = new ActionDefinition(Action, kind, ActionDefinitionOrigin.PrintedPrimary,
                    kind == ActionKind.Attack ? new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) }
                        : Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    kind == ActionKind.Attack ? AttackRangeKind.NonRanged : AttackRangeKind.None, default, effect,
                    cardTextTiming: kind == ActionKind.Attack ? CardTextTiming.BeforeTheAttack : CardTextTiming.None,
                    attackTargetPolicy: kind == ActionKind.Attack
                        ? PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged).BoundByCardText()
                        : (PrintedAttackTargetPolicy?)null);
                var definition = new CardDefinition(Card, "rev-1", "TEST - Garrus clause", new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[] { action });
                Definitions = new[] { definition };
                Cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("red"), definition.Reference, Red),
                    CardInstance.CreateInHand(new CardInstanceId("blue"), definition.Reference, Blue),
                    new CardInstance(new CardInstanceId("spare"), definition.Reference, foreignDiscard ? Blue : Red,
                        ownDiscard || foreignDiscard
                            ? new CardStateFacts(CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceDown)
                            : CardStateFacts.InitialInHand())
                };
                var coordinates = Enumerable.Range(-6, 13).SelectMany(q => Enumerable.Range(-6, 13)
                    .Select(r => new HexCoordinate(q, r))).Where(at => !terrain.HasValue || !at.Equals(terrain.Value));
                var mapId = new MapId("garrus-clause-map");
                var minions = minion ? new[] { new QuickGameMinionSetupState(
                    new MinionId("adjacent-minion"), new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                    TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId("minion-spawn")),
                    new ConfiguredSpaceReference(mapId, new SpaceId("q0_r1")), new HexCoordinate(0, 1)) }
                    : Array.Empty<QuickGameMinionSetupState>();
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(mapId,
                    1, coordinates, terrain.HasValue ? new[] { terrain.Value } : Array.Empty<HexCoordinate>()),
                    new[] { new HeroOccupancy(Red, new HexCoordinate(0, 0)), new HeroOccupancy(Blue, enemy) }
                        .Concat(secondEnemy ? new[] { new HeroOccupancy(new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                            new HexCoordinate(0, 1)) } : Array.Empty<HeroOccupancy>()),
                    Array.Empty<ObstacleOccupancy>(), minions);
                context = new EffectResolutionContext(effect, catalog, Participants, null, 1, 1, RuleModificationSet.Empty,
                    EffectCardState.Of(Definitions, Cards), kind, components: new MatchComponents(defaults.Components,
                        Board, new MarkerAttachmentRegister(), Board, Board, Board, Board),
                    enclosingAttack: secondEnemy ? new EnclosingAttackFacts(Red, Blue, default,
                        AttackRangeKind.NonRanged, null, new HexCoordinate(0, 0)) : default);
                Step = EffectResolutionSession.Start(new EffectResolutionId("garrus-clause-resolution"),
                    Cards[0].Id, definition.Reference, Action, Red, Red, ActionInvocationContext.PrintedPrimary, context);
                Assert.That(Step.Accepted, Is.True);
            }

            internal void Option(string node) => Select(Choice.SelectInvocations.Single(command => command.Candidate.OptionNodeId.Value == node));
            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value)));
            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }
        }
    }
}
