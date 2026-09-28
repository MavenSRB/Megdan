using System;
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
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsGreenSnapshotTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EachGreenPendingDecision_RestoresWithSameOffer(int index)
        {
            var trinkets = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var participants = StagedBoardParticipants.Create();
            var spaces = Enumerable.Range(-4, 9).SelectMany(q =>
                Enumerable.Range(-4, 9).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("green-snapshot"), 1, spaces),
                new[]
                {
                    new HeroOccupancy(trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(0, 2))
                }, Array.Empty<ObstacleOccupancy>());
            Assert.That(board.TryPlaceOrRepositionObstacle(
                OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, trinkets),
                new HexCoordinate(0, 1)), Is.True);
            var face = TrinketsCardContent.GreenDefinitions[index];
            var source = CardInstance.CreateInHand(
                new CardInstanceId("green-source"), face.Reference, trinkets);
            var hold = new CardDefinition(
                new CardDefinitionId("green-snapshot-hold"), "rev-1", "Hold",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true))
                });
            var enemySource = CardInstance.CreateInHand(
                new CardInstanceId("enemy-hold"), hold.Reference, enemy);
            var definitions = new[] { face, hold };
            var instances = new[] { source, enemySource };
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    TrinketsComponentContent.ObstacleHandlers()),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var markers = new MarkerAttachmentRegister();
            var actions = new CardActionResolutionAuthority(catalogs, board, markers);
            var round = new RoundResolutionAuthority(
                definitions, instances, participants,
                new TieBreakerState(TeamSide.Blue), 1,
                new RoundResolutionComposition(actions, board,
                    new ActiveEffectLifecycle(), markers,
                    null, null, null, null, null, null));
            Assert.That(round.TryCommitCard(trinkets, source.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(enemy, enemySource.Id).Accepted, Is.True);
            Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(trinkets));
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                definitions, instances, trinkets, source.Id).Single(value =>
                value.ActionDefinitionId.Equals(TrinketsCardContent.PrimarySkill));
            Assert.That(round.TryStartSkill(
                new ActionResolutionId("green-snapshot-action"), offer).Accepted, Is.True);
            var pending = round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, bytes, definitions, new[] { board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));
            // Milestone 9.1, ruling W3: a detonation's first question names the enemy hero it hits.
            var invocation = index == 2 || index == 4
                ? pending.SelectInvocations.Single(command =>
                    command.Candidate.Value.Equals(EffectValue.Hero(enemy)))
                : index == 3
                    ? pending.SelectInvocations.Single(command =>
                        command.Candidate.OptionNodeId.Value == "swap-arm")
                    : pending.SelectInvocations.Single(command =>
                        command.Candidate.Value.Equals(EffectValue.Space(
                            new HexCoordinate(0, 2))));
            Assert.That(invocation, Is.Not.Null);
            Assert.That(round.TryAnswerEffectChoice(invocation).Accepted, Is.True);
            Assert.That(restored.Round.TryAnswerEffectChoice(invocation).Accepted, Is.True);
            var liveNext = round.CurrentTurnAuthority.PendingEffectChoice;
            var restoredNext = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(restoredNext?.NodeId, Is.EqualTo(liveNext?.NodeId));
            if (liveNext != null)
                Assert.That(restoredNext.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(liveNext.Candidates.Select(candidate => candidate.ToString())));
            Assert.That(board.TryGetHeroCoordinate(trinkets, out var liveAt), Is.True);
            Assert.That(restored.Board.TryGetHeroCoordinate(trinkets, out var restoredAt), Is.True);
            Assert.That(restoredAt, Is.EqualTo(liveAt));
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, trinkets);
            var liveTurret = board.TryGetObstacleCoordinate(turret, out var liveTurretAt);
            var restoredTurret = restored.Board.TryGetObstacleCoordinate(
                turret, out var restoredTurretAt);
            Assert.That(restoredTurret, Is.EqualTo(liveTurret));
            if (liveTurret) Assert.That(restoredTurretAt, Is.EqualTo(liveTurretAt));
            var liveResult = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var replayResult = AuthoritativeSnapshotSerializer.Serialize(
                restored.Round, restored.Board);
            Assert.That(replayResult, Is.EqualTo(liveResult),
                "the restored pending prefix must replay to the same authoritative bytes");
            var retainedPrefix = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, bytes, definitions, new[] { board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(retainedPrefix.Round.TryAnswerEffectChoice(invocation).Accepted,
                Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                retainedPrefix.Round, retainedPrefix.Board), Is.EqualTo(liveResult),
                "re-executing from the retained pre-choice prefix matches the forward result");
        }
    }
}
