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
        /// Milestone 6.8a keeps schema v9 and re-pins every fixture for one content cause: Red Bulwark's
        /// printed Primary Attack became its showcase Skill, so that card definition advanced from
        /// revision <c>phase-5-1a-rev-3</c> to <c>phase-6-8a-rev-1</c>. The decision is reject rather
        /// than migrate: a v9 payload naming Red Bulwark at the 5.1a revision states a printed action
        /// set the trusted catalog no longer supplies, and reading it as the converted card would
        /// silently give a saved match a Skill where it recorded an Attack.
        /// The delta was read at the byte level either side of the change rather than assumed. Every
        /// payload keeps its exact decoded length, and each differs in six to eight bytes across
        /// exactly two regions: the definition table's Red Bulwark entry and the <c>red-bulwark-card</c>
        /// instance's definition reference. Both are the same sixteen-character revision string, so
        /// nothing shifted. The capability registration list and the Hazard token revision are
        /// byte-identical, and the replaced action set never appears at all, because the payload names
        /// definitions and the catalog supplies what they contain.
        /// Milestone 6.8h keeps schema v9 and re-pins exactly one fixture, <c>attached-marker-v9</c>,
        /// for one cause: the marker capability now composes its attachment's instance ID through
        /// <c>MarkerInstanceIdentity</c>, so the persisted ID gains the action and the round/turn
        /// anchors. The decision here is accept without migration, unlike the revision causes above: a
        /// marker instance ID is an opaque wire string that nothing parses, re-derives, or compares
        /// against content, so a v9 payload written before this milestone still restores and still
        /// names the same attachment. Only newly captured payloads differ, which is why the fixture is
        /// re-pinned rather than the reader changed. The delta was read at the byte level: the decoded
        /// payload grows from 3232 to 3261 bytes in exactly one region - common prefix 2957 bytes,
        /// common suffix 238 bytes - where the ID's single-byte length prefix moves from 51 to 80 and
        /// the string gains <c>:primary-component-skill</c> and <c>:r1t1</c>. Both lengths stay inside
        /// one 7-bit-encoded prefix byte, so nothing else shifted, and the other six payloads including
        /// <c>placed-token-v9</c> are byte-identical, which is the token family's independent proof
        /// that this milestone left it alone.
        /// Milestone 6.4 keeps schema v9 and re-pins all seven fixtures for one cause: the build now
        /// registers two more effect capabilities - <c>synthetic-direct-move</c> and
        /// <c>synthetic-object-push</c> - and every payload records the capability contract set it was
        /// written against. The decision is reject rather than migrate, and it needs no reader change
        /// because <c>ValidateCapabilityRegistrations</c> already refuses any payload whose stored list
        /// is not sequence-equal to this build's: a payload written against the sixteen-capability set
        /// states a contract set that no longer describes this build, and restoring it would leave a
        /// saved match unable to resolve a definition binding either new capability. The delta was read
        /// at the byte level rather than assumed. The registration list grows 16 -> 18 entries, the
        /// decoded payload grows 2893 -> 2945 bytes - exactly the 52 bytes those two entries cost as
        /// 7-bit length-prefixed ids plus int32 revisions - and the change is confined to one
        /// contiguous region, with a 608-byte common prefix and a 1986-byte common suffix that are
        /// byte-identical either side. No effect, board, component, or round field moved.
        /// A second and separable 6.4 cause moves only <c>placed-token-v9</c> and
        /// <c>attached-marker-v9</c>: the milestone's displacement Skill card joins the Skill-fixture
        /// hands, and those are the two fixtures built from them, so their definition table gains
        /// <c>placeholder-red-shove</c> and <c>placeholder-blue-shove</c> at
        /// <c>phase-6-4-rev-1</c> and their card list gains one instance per hand. Measured rather than
        /// assumed: 241 bytes on top of the capability delta, of which 77 are the two definition-table
        /// entries and 164 the two instance records. The decision is reject for the same reason as the
        /// capability cause and by the same existing check - a payload written against the two-card
        /// fixture hand states a card set the trusted catalog no longer supplies. The other five
        /// fixtures are untouched by this cause because only the component snapshots use the Skill
        /// fixture, which is that hand's own proof that adding a card to it leaves the ordinary
        /// five-card matrix alone, exactly as 6.2a's separate-profile decision intended.
        /// Milestone 6.8k keeps schema v9 and re-pins all seven fixtures for one registration cause
        /// with two parts: the capability list gains <c>synthetic-choose-target</c>, and
        /// <c>synthetic-direct-move</c> moves from revision 1 to revision 2 because its subject
        /// parameter widened from a typed hero to a typed coordinate. The decision is reject rather
        /// than migrate, by the same existing check as 6.4's cause: a payload recording the
        /// eighteen-capability set with direct-move at revision 1 states a contract set this build no
        /// longer supplies, and restoring one would ask the build to resolve a Hero input into a
        /// Coordinate parameter. Measured rather than assumed, at the same authoritative-payload layer
        /// as the 6.4 record: the five ordinary fixtures each grow exactly 28 bytes - the one new
        /// registration entry as a 7-bit length-prefixed 23-character id plus an int32 revision - in
        /// one contiguous region with a 452-byte common prefix and every later section byte-identical.
        /// A second and separable cause moves <c>placed-token-v9</c> and <c>attached-marker-v9</c> by
        /// 4 more bytes each: the two Skill-fixture shove cards advance from <c>phase-6-4-rev-1</c> to
        /// <c>phase-6-8k-rev-1</c> - one byte longer, twice per card across the card and effect
        /// revision strings - because their move branch now runs a target-selection node whose
        /// published space is the direct move's subject. The reject decision for that cause is the
        /// content-revision check, exactly as at 6.8a.
        /// Milestone 6.4a moves every fixture from v9 to v10 and adds the eighth,
        /// <c>pending-correction-v10</c>: a completed Object Push has displaced one minion outside the
        /// Battle Zone and the after-action correction is paused on its team's route choice between
        /// two equal-shortest return endpoints. The pending block carries the owning action's
        /// identity, the stage, the deciding team, and candidate identities only - routes and costs
        /// are never on the wire, because restore re-derives the whole correction from the restored
        /// board and the Tie Breaker and refuses any payload the derivation does not reproduce. The
        /// migrate-or-reject decision is reject, as at every bump: a v9 payload could not represent a
        /// paused correction, the engine has one read path per version, and no persisted state exists
        /// outside this corpus. Measured rather than assumed: each carried-over payload keeps its
        /// exact decoded length and differs from its v9 predecessor in the one version integer,
        /// because the correction block is written only under continuation kind 4 and no carried-over
        /// fixture holds one.
        /// Milestone 6.5 keeps schema v10 and re-pins all eight fixtures for two separable causes, and
        /// adds no fixture: it introduces no persisted family, because the unit supply a Spawn draws
        /// from is derived on restore as the configured roster minus the restored positions and never
        /// written. The first cause reaches every payload - the capability list gains
        /// <c>synthetic-actor-space</c>, <c>synthetic-place</c>, and <c>synthetic-spawn</c> - and the
        /// decision is reject rather than migrate, by the same existing
        /// <c>ValidateCapabilityRegistrations</c> sequence check 6.4 and 6.8k relied on: a payload
        /// recording the nineteen-capability set states a contract set this build no longer supplies.
        /// Measured rather than assumed: each of the five ordinary fixtures grows exactly 66 bytes -
        /// the three registration entries as 7-bit length-prefixed ids of 21, 15, and 15 characters
        /// plus an int32 revision each - in one contiguous region with a 452-byte common prefix and
        /// every later section byte-identical.
        /// The second cause moves only the three Skill-fixture payloads, by 4 bytes the other way: the
        /// two shove cards become the fixture hand's board-object Skill and advance from
        /// <c>phase-6-8k-rev-1</c> to <c>phase-6-5-rev-1</c>, one character shorter, and each card
        /// writes its revision twice - once for the card and once for its effect definition - so the
        /// string appears four times per payload and each payload nets 66 - 4 = 62 bytes. Its reject
        /// decision is the content-revision check, exactly as at 6.8a: a payload naming the shove card
        /// at the 6.8k revision states a branch set the trusted catalog no longer supplies, and reading
        /// it as the widened card would silently give a saved match Place and Spawn options it never
        /// recorded.
        /// Milestone 6.8b keeps schema v10 and re-pins every fixture for one content cause, the same
        /// shape 6.8a recorded: Blue Feint's printed Primary Attack became its showcase Skill, so that
        /// card definition advanced from <c>phase-5-1a-rev-3</c> to <c>phase-6-8b-rev-1</c>. The
        /// decision is reject rather than migrate, because a payload naming Blue Feint at the 5.1a
        /// revision states a printed action set the trusted catalog no longer supplies, and reading it
        /// as the converted card would silently give a saved match a Skill where it recorded an Attack.
        /// Measured at the byte level either side of the change rather than assumed: both revision
        /// strings are sixteen characters, so every payload keeps its exact decoded length and differs
        /// in exactly eight bytes across exactly two regions - the definition table's Blue Feint entry
        /// and the <c>blue-feint-card</c> instance's definition reference - which is the four positions
        /// where the two strings differ, twice. <c>pending-choice-v10</c> differs in twelve bytes
        /// across three regions rather than two, because the Blue hero is its acting hero and its
        /// paused Movement continuation records the same source definition reference a third time. The
        /// capability registration list is byte-identical, and the replaced action set never appears at
        /// all, because the payload names definitions and the catalog supplies what they contain.
        /// Milestone 6.5a keeps schema v10 and re-pins every fixture for two separable causes, both
        /// already familiar. The registration list gains <c>synthetic-push-target</c>, p.18's Push with
        /// the target the definition names beside the adjacency-derived declaration 6.4 registered, and
        /// that costs every payload exactly 26 bytes - a 7-bit length-prefixed 21-character id plus an
        /// int32 revision - in one contiguous region. The content cause is Red Needle's printed Primary
        /// Attack becoming its showcase Skill, so its definition advances from
        /// <c>phase-5-1a-rev-3</c> to <c>phase-6-5a-rev-1</c>; both strings are sixteen characters, so
        /// that cause shifts nothing and the string appears twice per payload, in the definition table
        /// and in the <c>red-needle-card</c> instance's definition reference. Measured rather than
        /// assumed: every payload grows by exactly 26 bytes and every occurrence count moves as stated.
        /// Both decisions are reject rather than migrate, by the existing capability-sequence and
        /// content-revision checks.
        /// Milestone 6.5aa moves every fixture from v10 to v11 because a pending Minion Battle
        /// continuation now persists a discriminated unit identity rather than only a minion ID, so
        /// it can faithfully retain a counts-as hero removal choice. The migrate-or-reject decision
        /// is reject: v10 has no hero arm for that union, the engine intentionally keeps one reader,
        /// and no persisted state exists outside this corpus. Red Needle also advances to
        /// <c>phase-6-5aa-rev-1</c> because its target node now declares Object Push as the consuming
        /// activity; restoring the old revision as the corrected definition would silently change
        /// its offered choices, so the existing content-revision check rejects it rather than
        /// migrating it. All fixtures are therefore renamed and re-pinned as v11.
        /// </summary>
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v11", "7d95324d7352b6f039463748a65aea83a6ff4eda87eae2092f255a4a28c9995d"),
            new CompatibilityFixture("active-turn-v11", "2b83aeb52d6b28f6d97d7d17039dd1dc19926eb823db73c1b33f6fb67dac065f"),
            new CompatibilityFixture("pending-choice-v11", "97819d3487fdc249e212b0fdd29528f517d88dbaf434697cdbea200e70638414"),
            new CompatibilityFixture("end-of-round-v11", "b1bbd2cd2c5ca9bc15de260068c92175fcc98ed533707e4883508d179b860747"),
            new CompatibilityFixture("pending-minion-battle-v11", "11b864c6f26fed119ba982d326d901c2e335ad469528fcf3c7ab02f99863ed78"),
            new CompatibilityFixture("placed-token-v11", "ef796fb90fdc4f43019aca64f10752e319ab408b424e6067a6533709a116747e"),
            new CompatibilityFixture("attached-marker-v11", "7d6df9890e2275dfc90934ce56f3cfbf4745c852b7110f9e0d8cbe59aef44057"),
            new CompatibilityFixture("pending-correction-v11", "e4397f2bac031ceb66f8efc6a5b7327f1245d3cfa9c0dd18c86bed340a786f16")
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
            values.Add("setup-v11", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v11", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v11", session.CaptureSnapshot());

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

            values.Add("end-of-round-v11", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v11", session.CaptureSnapshot());

            // Milestone 6.2's two new wire sections. A version-controlled corpus whose fixtures all
            // carry zero components would leave the sections it exists to protect untested, so one
            // payload holds a token on the board and one holds a marker on a hero.
            values.Add("placed-token-v11", CaptureComponentSnapshot("component-place-branch"));
            values.Add("attached-marker-v11", CaptureComponentSnapshot("component-give-marker"));

            // Milestone 6.4a's wire block: every persisted pending family keeps one corpus fixture,
            // so the paused after-action correction gets the eighth.
            values.Add("pending-correction-v11", CapturePendingCorrectionSnapshot());
            return values;
        }

        /// <summary>
        /// Drives the displacement Skill's Push branch on real official-map geography chosen so the
        /// batch displaces exactly one minion outside the Battle Zone: the pusher stands at (0,-1),
        /// and red-melee-01 at (-1,-1) travels its full two spaces to (-3,-1), outside the zone, while
        /// every other adjacent object ends inside it. The correction then pauses on the Red team's
        /// route choice between the two equal-shortest return endpoints, which is the paused state
        /// this fixture exists to pin.
        /// </summary>
        private static string CapturePendingCorrectionSnapshot()
        {
            var session = CreateCorrectionSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("shove") &&
                    candidate.State.Location == CardLocation.Hand);
                Assert.That(session.Execute(
                    DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0])).Accepted, Is.True);
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-displacement-skill")));
            Assert.That(session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);

            var push = session.Projection.PendingEffectChoice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Value == "displacement-object-push");
            AnswerEffect(session, push);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null,
                "The push must leave the after-action correction paused, or this fixture pins nothing.");
            Assert.That(pending.DestinationCandidates.Count, Is.GreaterThanOrEqualTo(2),
                "The paused stage must be a real route choice.");
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// The Skill-fixture configuration with the acting Red hero placed beside the Battle Zone's
        /// north-east minions rather than on its Throne spawn, so a two-space push crosses the zone
        /// boundary. Scenario occupancies override setup placements exactly as the displacement
        /// dispatch fixture does.
        /// </summary>
        private static MatchSession CreateCorrectionSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, new HexCoordinate(0, -1)),
                    new HeroOccupancy(blueHeroId, new HexCoordinate(-1, -5))
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
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
            fixtureName.StartsWith("attached-marker", StringComparison.Ordinal) ||
            fixtureName.StartsWith("pending-correction", StringComparison.Ordinal)
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
