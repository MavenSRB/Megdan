using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnapshotCompatibilityCorpusTests
    {
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v7", "456efc4c309b62db88d2be67020341187b9504b1af8d2a57bb15d81189e9adbc"),
            new CompatibilityFixture("active-turn-v7", "17b19c06334340d4fde3fda61453f25262014486b3b2422b0fe1e60bf2f33441"),
            new CompatibilityFixture("pending-choice-v7", "5befefe447a2353e6c729722a7395b73eb82eb8c35d6201756a3b34a1a961962"),
            new CompatibilityFixture("end-of-round-v7", "de42a3842753fc592ae8931beeef27d743f40f82bbccdf7d8dbda5df7e33e39d"),
            new CompatibilityFixture("pending-minion-battle-v7", "4ff2a08caffab630dd8de31eec183bbff40ec9e73012c9849c438a4b99030df4")
        };

        [Test]
        public void SupportedVersionedCorpus_RoundTripsByteIdentically()
        {
            var snapshots = CreateSupportedSnapshots();
            Assert.That(snapshots.Keys, Is.EquivalentTo(SupportedFixtures.Select(value => value.Name)));

            var actualHashes = SupportedFixtures
                .Select(fixture => Sha256(snapshots[fixture.Name]))
                .ToArray();
            Assert.That(
                actualHashes,
                Is.EqualTo(SupportedFixtures.Select(fixture => fixture.Sha256).ToArray()),
                $"Compatibility corpus changed. Actual SHA-256 values: {string.Join(", ", actualHashes)}. " +
                "Record an explicit migrate-or-reject decision before updating the corpus.");

            foreach (var fixture in SupportedFixtures)
            {
                var snapshot = snapshots[fixture.Name];
                var restored = CreateSession();
                var result = restored.RestoreSnapshot(snapshot);
                Assert.That(result.Accepted, Is.True, $"{fixture.Name}: {result.Detail}");
                Assert.That(restored.CaptureSnapshot(), Is.EqualTo(snapshot), fixture.Name);
            }
        }

        [Test]
        public void UnsupportedCompletedMatchSchemaAndContentRevision_RejectDistinctly()
        {
            var session = CreateSession();
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out var envelopeRejection), Is.True);
            Assert.That(envelopeRejection, Is.EqualTo(RuleEventLogEnvelopeRejectionReason.None));

            var futureCompletedMatchPayload = RewriteSchemaVersion(
                envelope.AuthoritativePayload,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion + 1);
            var schemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    futureCompletedMatchPayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(schemaException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion));

            var priorSchemaPayload = RewriteSchemaVersion(
                envelope.AuthoritativePayload,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion - 1);
            var priorSchemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    priorSchemaPayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(priorSchemaException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion),
                "Schema v6 reconstructs minion positions from setup placements and cannot express a moved minion; milestone 6.1s serializes per-minion coordinates and explicitly rejects v6.");

            var changedDefinitions = session.Projection.Definitions.ToArray();
            var original = changedDefinitions[0];
            changedDefinitions[0] = new CardDefinition(
                original.Id,
                original.ContentRevision + "-incompatible",
                original.DisplayName,
                original.Initiative,
                original.ContentAuthority,
                original.ActionDefinitions);
            var contentException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    envelope.AuthoritativePayload,
                    changedDefinitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(contentException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch));
        }

        private static Dictionary<string, string> CreateSupportedSnapshots()
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var session = CreateSession();
            values.Add("setup-v7", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v7", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v7", session.CaptureSnapshot());

            session = CreateSession();
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(session.Execute(
                    DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }

            values.Add("end-of-round-v7", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v7", session.CaptureSnapshot());
            return values;
        }

        private static MatchSession CreateSessionWithMinionImbalance()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var removed = fullSetup.Minions.First(value =>
                value.TeamSide == TeamSide.Blue && value.MinionType == StandardMinionType.Ranged);
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                fullSetup.Minions.Where(value => !value.MinionId.Equals(removed.MinionId)));
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Execute(DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static void CommitUnequalCards(MatchSession session)
        {
            var red = EligibleCards(session, TeamSide.Red).OrderBy(value => Initiative(session, value)).ToArray();
            var blue = EligibleCards(session, TeamSide.Blue).OrderBy(value => Initiative(session, value)).ToArray();
            Assert.That(red, Is.Not.Empty);
            Assert.That(blue, Is.Not.Empty);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red[0].OwnerId, red[0].Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue[blue.Length - 1].OwnerId,
                blue[blue.Length - 1].Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(value =>
                value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand);
        }

        private static int Initiative(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).Initiative.Value;
        }

        private static ActionKind FindActionKind(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    reader.ReadString();
                }

                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(schemaVersion);
                }
            }

            return Convert.ToBase64String(bytes);
        }

        private static string Sha256(string payload)
        {
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Convert.FromBase64String(payload))
                    .Select(value => value.ToString("x2")));
            }
        }

        private readonly struct CompatibilityFixture
        {
            public CompatibilityFixture(string name, string sha256)
            {
                Name = name;
                Sha256 = sha256;
            }

            public string Name { get; }
            public string Sha256 { get; }
        }
    }
}
