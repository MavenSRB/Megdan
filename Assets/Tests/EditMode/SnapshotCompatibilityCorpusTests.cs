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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnapshotCompatibilityCorpusTests
    {
        /// <summary>
        /// Milestone 6.2 moved every fixture from v8 to v9 and added the two component cases. The
        /// migrate-or-reject decision this rename records is reject: a v8 payload could not represent a
        /// token or a marker, so reading one as a component-free match would have been faithful, and it
        /// is refused anyway - the engine has one read path per version, and no persisted state outside
        /// this corpus exists to strand. See <c>AuthoritativeSnapshotSerializer.CurrentSchemaVersion</c>.
        /// Milestone 6.3 keeps schema v9 and deliberately re-pins every fixture because the registered
        /// synthetic token definition moved from revision 1 to revision 2 to acquire its immutable
        /// property content. The content decision is reject: a v9 payload bound to Hazard v1 names a
        /// definition revision the trusted catalog no longer supplies, rather than silently gaining v2
        /// semantics. No persisted state outside this test corpus exists to migrate.
        /// Milestone 6.8 keeps schema v9 for the same reason and re-pins every fixture again, for two
        /// registration causes: the capability list gained the five synthetic modification capabilities,
        /// and Hazard moved from revision 2 to revision 3 to carry the typed rule its property now
        /// supplies. Both decisions are reject rather than migrate: a payload naming a capability set
        /// this build does not register, or a token revision the catalog no longer supplies, states
        /// semantics that no longer exist, and there is still no persisted state outside this corpus.
        /// The general modification contracts themselves added no wire field - they are definition
        /// content that a restored effect or token rebinds to from the trusted catalog.
        /// </summary>
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v9", "290921b5481a74a6aea04595516ece0e38b723e412fc287cee9661090efd700f"),
            new CompatibilityFixture("active-turn-v9", "34a960c57998b785f7781f875502cb88acb7d3a2c86712ebec64460306498803"),
            new CompatibilityFixture("pending-choice-v9", "936850def5493c1200db86d897327fb56edaacd8d01f3073230db8412765413e"),
            new CompatibilityFixture("end-of-round-v9", "e011950033df0ce01df0539c06993e839ed1703cd96ad81fbffb7d71871f89ea"),
            new CompatibilityFixture("pending-minion-battle-v9", "03051d98d1b39b21577e60102d40b14751e934054ee2b86f754cd16edc4e8b05"),
            new CompatibilityFixture("placed-token-v9", "3f2baa94da773abfd2f9eef39dbe7ac74ef6d602a092e662ea531ab0b0233248"),
            new CompatibilityFixture("attached-marker-v9", "d57586189998ef95ed02e902dc0b955f90c587456bf4a6f5c3003bb4a4cfa1ec")
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
                var restored = CreateRestoreTarget(fixture.Name);
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
                "The immediately prior schema is rejected rather than migrated, which every bump so far " +
                "has decided the same way: v7 could not express an Active effect without stating its " +
                "semantics inline (6.2a), and v8 has no section a placed token or attached marker could " +
                "occupy (6.2).");

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
            values.Add("setup-v9", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v9", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v9", session.CaptureSnapshot());

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

            values.Add("end-of-round-v9", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v9", session.CaptureSnapshot());

            // Milestone 6.2's two new wire sections. A version-controlled corpus whose fixtures all
            // carry zero components would leave the sections it exists to protect untested, so one
            // payload holds a token on the board and one holds a marker on a hero.
            values.Add("placed-token-v9", CaptureComponentSnapshot("component-place-branch"));
            values.Add("attached-marker-v9", CaptureComponentSnapshot("component-give-marker"));
            return values;
        }

        /// <summary>
        /// Runs the synthetic component Skill once, taking the named branch, and captures the result.
        /// Both branches pause on exactly one capability choice, so the driving is the same either way.
        /// </summary>
        private static string CaptureComponentSnapshot(string optionNodeId)
        {
            var session = CreateComponentSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("cache") &&
                    candidate.State.Location == CardLocation.Hand);
                Assert.That(session.Execute(
                    DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-component-skill")));
            Assert.That(session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);

            var option = session.Projection.PendingEffectChoice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Value == optionNodeId);
            AnswerEffect(session, option);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            AnswerEffect(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                $"The {optionNodeId} branch must complete, so the captured payload holds committed " +
                "component state rather than a paused frame.");
            return session.CaptureSnapshot();
        }

        private static void AnswerEffect(MatchSession session, EffectChoiceCommand invocation)
        {
            var result = session.Execute(DirectorCommand.AnswerEffectChoice(invocation));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// A session whose configured content matches the fixture's. A restore validates the payload's
        /// card definitions against the ones the session was configured with, so a component payload -
        /// which was produced by the Skill-bearing configuration - has to come back into that same one.
        /// </summary>
        private static MatchSession CreateRestoreTarget(string fixtureName) =>
            fixtureName.StartsWith("placed-token", StringComparison.Ordinal) ||
            fixtureName.StartsWith("attached-marker", StringComparison.Ordinal)
                ? CreateComponentSession()
                : CreateSession();

        private static MatchSession CreateComponentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
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
