using System;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AuthoritativeSnapshotSerializerTests
    {
        [Test]
        public void RoundTrip_PreservesParticipantsBoardCardsCommitmentAndLogicalContinuation()
        {
            var fixture = CreateFixture();
            var red = fixture.Configuration.RedHand.Instances[0];
            var blue = fixture.Configuration.BlueHand.Instances[0];
            Assert.That(fixture.Round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });

            Assert.That(restored.Participants.Teams.Select(team => (team.Id.Value, team.Side)),
                Is.EqualTo(fixture.Configuration.Participants.Teams.Select(team => (team.Id.Value, team.Side))));
            Assert.That(restored.Participants.Players.Select(player => (player.Id.Value, player.TeamId.Value)),
                Is.EqualTo(fixture.Configuration.Participants.Players.Select(player => (player.Id.Value, player.TeamId.Value))));
            Assert.That(restored.Participants.Heroes.Select(hero => (hero.Id.Value, hero.PlayerId.Value)),
                Is.EqualTo(fixture.Configuration.Participants.Heroes.Select(hero => (hero.Id.Value, hero.PlayerId.Value))));
            Assert.That(restored.Board.BoardCoordinates, Is.EquivalentTo(fixture.Board.BoardCoordinates));
            Assert.That(restored.Board.Occupancies, Is.EquivalentTo(fixture.Board.Occupancies));
            Assert.That(restored.Board.ObstacleOccupancies, Is.EquivalentTo(fixture.Board.ObstacleOccupancies));
            Assert.That(restored.Cards.Select(CardFact), Is.EqualTo(fixture.Configuration.CardInstances.Select(CardFact)));
            Assert.That(restored.Round.RoundIndex, Is.EqualTo(1));
            Assert.That(restored.Round.TurnIndex, Is.EqualTo(1));
            Assert.That(restored.Round.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(restored.Round.CurrentCommitmentAuthority.GetStatus(red.OwnerId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload), "An unchanged restored state must serialize deterministically.");

            var originalEvents = fixture.Round.TryCommitCard(blue.OwnerId, blue.Id).Events;
            var restoredBlue = restored.Cards.Single(card => card.Id.Equals(blue.Id));
            var restoredEvents = restored.Round.TryCommitCard(restoredBlue.OwnerId, restoredBlue.Id).Events;
            Assert.That(EventFacts(restoredEvents), Is.EqualTo(EventFacts(originalEvents)));
            Assert.That(restored.Round.Phase, Is.EqualTo(fixture.Round.Phase));
            Assert.That(restored.Round.TieBreakerState.RevealedSide,
                Is.EqualTo(fixture.Round.TieBreakerState.RevealedSide));
        }

        [Test]
        public void RoundTrip_PendingAttackTargetAndDefenseResumeWithSameOrderedEvents()
        {
            var fixture = CreateFixture();
            CommitFirstCards(fixture);
            var selected = fixture.Round.CurrentTurnAuthority.SelectedActor;
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    selected.ActorId,
                    selected.CardInstanceId)
                .Single(candidate => candidate.AttackRange != AttackRangeKind.None);
            Assert.That(fixture.Round.TryStartAttack(new ActionResolutionId("snapshot-attack"), offer).Accepted, Is.True);

            var targetSnapshot = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var targetRestore = AuthoritativeSnapshotSerializer.Deserialize(
                targetSnapshot,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            var target = fixture.Round.CurrentTurnAuthority.PendingAttackTargetChoice.LegalTargetHeroIds.Single();
            var originalTargetEvents = fixture.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                new ActionResolutionId("snapshot-attack"),
                fixture.Round.CurrentTurnAuthority.PendingAttackTargetChoice.ChoiceId,
                target)).Events;
            var restoredTargetChoice = targetRestore.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            var restoredTargetEvents = targetRestore.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                restoredTargetChoice.ResolutionId,
                restoredTargetChoice.ChoiceId,
                target)).Events;
            Assert.That(EventFacts(restoredTargetEvents), Is.EqualTo(EventFacts(originalTargetEvents)));

            var defenseSnapshot = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var defenseRestore = AuthoritativeSnapshotSerializer.Deserialize(
                defenseSnapshot,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            var originalDefense = fixture.Round.CurrentTurnAuthority.PendingDefenseChoice;
            var restoredDefense = defenseRestore.Round.CurrentTurnAuthority.PendingDefenseChoice;
            var originalDefenseEvents = fixture.Round.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(originalDefense.ResolutionId, originalDefense.ChoiceId)).Events;
            var restoredDefenseEvents = defenseRestore.Round.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(restoredDefense.ResolutionId, restoredDefense.ChoiceId)).Events;
            Assert.That(EventFacts(restoredDefenseEvents), Is.EqualTo(EventFacts(originalDefenseEvents)));
            Assert.That(defenseRestore.Round.Phase, Is.EqualTo(fixture.Round.Phase));
            Assert.That(defenseRestore.Cards.Select(CardFact),
                Is.EqualTo(fixture.Configuration.CardInstances.Select(CardFact)));
        }

        [Test]
        public void RoundTrip_PreservesCompletedRoundHistorySlotsAndEndOfRoundCursorOutcomes()
        {
            var fixture = CreateFixture();
            CompleteRoundWithHolds(fixture.Round, fixture.Configuration);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);

            var cursorRestore = AuthoritativeSnapshotSerializer.Deserialize(
                AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board),
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            Assert.That(cursorRestore.Round.CurrentEndOfRoundStage,
                Is.EqualTo(fixture.Round.CurrentEndOfRoundStage));
            Assert.That(cursorRestore.Round.EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind)),
                Is.EqualTo(fixture.Round.EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind))));
            Assert.That(SlotFacts(cursorRestore.Round.TurnSlots), Is.EqualTo(SlotFacts(fixture.Round.TurnSlots)));

            while (fixture.Round.Phase == RoundResolutionPhase.EndOfRound)
                Assert.That(fixture.Round.TryAdvanceEndOfRound().Accepted, Is.True);
            var completedRestore = AuthoritativeSnapshotSerializer.Deserialize(
                AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board),
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition });
            Assert.That(completedRestore.Round.CompletedRounds.Count, Is.EqualTo(1));
            Assert.That(SlotFacts(completedRestore.Round.CompletedRounds[0].TurnSlots),
                Is.EqualTo(SlotFacts(fixture.Round.CompletedRounds[0].TurnSlots)));
            Assert.That(completedRestore.Round.CompletedRounds[0].EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind)),
                Is.EqualTo(fixture.Round.CompletedRounds[0].EndOfRoundOutcomes.Select(value => (value.Stage, value.Kind))));
        }

        [Test]
        public void Deserialize_RejectsSchemaCardAndMapContentMismatchesIndependently()
        {
            var fixture = CreateFixture();
            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var wrongSchema = RewriteSchemaVersion(payload, AuthoritativeSnapshotSerializer.CurrentSchemaVersion + 1);
            var schemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(wrongSchema, fixture.Configuration.CardDefinitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(schemaException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion));

            var definitions = fixture.Configuration.CardDefinitions.ToArray();
            var original = definitions[0];
            definitions[0] = new CardDefinition(
                original.Id,
                original.ContentRevision + "-changed",
                original.DisplayName,
                original.Initiative,
                original.ContentAuthority,
                original.ActionDefinitions);
            var revisionException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(payload, definitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(revisionException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch));

            var missingMapException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(payload, fixture.Configuration.CardDefinitions,
                    Array.Empty<BoardMapDefinition>()));
            Assert.That(missingMapException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MapDefinitionMissing));

            var currentMap = fixture.Board.MapDefinition;
            var changedMap = new BoardMapDefinition(currentMap.Id, currentMap.ContentRevision + 1,
                currentMap.DisplayName, currentMap.Spaces, currentMap.Zones, currentMap.SpawnPoints,
                currentMap.OrderedLaneZoneIds);
            var mapRevisionException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(payload, fixture.Configuration.CardDefinitions,
                    new[] { changedMap }));
            Assert.That(mapRevisionException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.MapDefinitionContentRevisionMismatch));
        }

        private static SnapshotFixture CreateFixture()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            var blueHero = configuration.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(0, 1), new HexCoordinate(-1, 1) },
                new[] { new HeroOccupancy(redHero, new HexCoordinate(0, 0)), new HeroOccupancy(blueHero, new HexCoordinate(1, 0)) },
                new[] { new GuardsDigital.Core.Obstacles.ObstacleOccupancy(new GuardsDigital.Core.Obstacles.ObstacleId("snapshot-rock"), new HexCoordinate(-1, 1)) });
            var cardActions = new CardActionResolutionAuthority(board);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                cardActions: cardActions);
            return new SnapshotFixture(configuration, board, round);
        }

        private static void CommitFirstCards(SnapshotFixture fixture)
        {
            var red = fixture.Configuration.RedHand.Instances[0];
            var blue = fixture.Configuration.BlueHand.Instances[0];
            Assert.That(fixture.Round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            Assert.That(fixture.Round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn));
        }

        private static void CompleteRoundWithHolds(
            RoundResolutionAuthority round,
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            for (var turn = 0; turn < RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                var red = configuration.RedHand.Instances[turn];
                var blue = configuration.BlueHand.Instances[turn];
                Assert.That(round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
                while (round.Phase == RoundResolutionPhase.ResolvingTurn)
                    Assert.That(round.TryPerformHold(round.CurrentTurnAuthority.SelectedActor.CardInstanceId).Accepted, Is.True);
            }
        }

        private static string CardFact(CardInstance card)
        {
            return $"{card.Id.Value}|{card.DefinitionReference}|{card.OwnerId.Value}|" +
                $"{card.State.Location}|{card.State.HasBeenPlayed}|{card.State.ResolutionStatus}|{card.State.FaceOrientation}";
        }

        private static string[] EventFacts(System.Collections.Generic.IEnumerable<RoundResolutionEvent> events)
        {
            return events.Select(value => $"{value.RoundIndex}:{value.TurnIndex}:{value.Kind}:" +
                $"{value.TurnEvent.Kind}:{value.TurnEvent.CardActionEvent.Kind}").ToArray();
        }

        private static string[] SlotFacts(System.Collections.Generic.IEnumerable<RoundTurnSlot> slots)
        {
            return slots.Select(slot => $"{slot.RoundIndex}:{slot.TurnIndex}:" + string.Join(",",
                slot.Assignments.Select(assignment => $"{assignment.HeroId.Value}=" +
                    (assignment.CardInstanceId?.Value ?? "PASS")))).ToArray();
        }

        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                    reader.ReadString();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    writer.Write(schemaVersion);
            }
            return Convert.ToBase64String(bytes);
        }

        private sealed class SnapshotFixture
        {
            public SnapshotFixture(PlaceholderOneVsOneTestConfiguration configuration,
                AdjacentMoveAuthority board, RoundResolutionAuthority round)
            { Configuration = configuration; Board = board; Round = round; }
            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public RoundResolutionAuthority Round { get; }
        }
    }
}
