using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.10's authoritative Battle Zone.
    ///
    /// Before this milestone every Battle-Zone question was answered from the map's
    /// starting-Battle-Zone flag, which is correct for exactly as long as the Lane has never been
    /// pushed. p.8 pushes it, so the zone is authoritative state the board owns: it opens where the map
    /// says, moves only through the Lane-Push Zone stage, and is what the Minion Battle, the p.13
    /// Heavy-immunity accompaniment, p.18's displaced-minion correction and p.8's own Lane-Push cause
    /// all read.
    ///
    /// Two properties are load-bearing and are checked here rather than assumed. It is independent of
    /// occupancy, so an emptied zone is still the Battle Zone and a crowded zone the Lane never reached
    /// is not one. And it is refused rather than repaired on the way back in, because no derivation from
    /// setup or from where minions stand could recover which Zone a pushed Lane reached.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class CurrentBattleZoneTests
    {
        private static readonly ZoneId StartingBattleZone = new ZoneId("starting-battle-zone");
        private static readonly ZoneId BlueApproach = new ZoneId("blue-approach");

        // -------------------------------------------------------------------------------------------
        // Where it comes from
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ABoard_OpensAtTheZoneTheMapFlags()
        {
            var board = CreateOfficialBoard();

            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone));
            Assert.That(
                BattleZoneQuery.GetCurrentBattleZone(board).IsStartingBattleZone, Is.True,
                "The flag decides the opening value and nothing after it.");
        }

        [Test]
        public void ABoardWhoseMapFlagsNoBattleZone_HasTheNamedAbsence()
        {
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("current-zone-synthetic"), 1, new[] { new HexCoordinate(0, 0) }),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            Assert.That(board.CurrentBattleZoneId, Is.Null);
            Assert.That(BattleZoneQuery.TryGetCurrentBattleZone(board, out var zone), Is.False);
            Assert.That(zone, Is.Null);
            Assert.That(BattleZoneQuery.FindMinionsOutside(board), Is.Empty,
                "With no zone there is nothing to be outside of, which is what keeps the single-zone " +
                "synthetic boards free of a p.18 correction they never had.");
        }

        [Test]
        public void ItIsIndependentOfWhereMinionsStand()
        {
            var board = CreateOfficialBoard();
            foreach (var minion in board.Minions.ToArray())
            {
                Assert.That(
                    board.CommitValidatedRemoval(
                        BoardObjectReference.ForMinion(minion.MinionId)).Accepted,
                    Is.True);
            }

            Assert.That(board.Minions, Is.Empty);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone),
                "An emptied Battle Zone is still the Battle Zone. p.8 empties it on the way to " +
                "pushing the Lane, so a zone inferred from occupancy would vanish at exactly the " +
                "moment the rule needs it.");
        }

        // -------------------------------------------------------------------------------------------
        // The four consumers read it
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AdvancingTheZone_MovesTheQuestionEveryConsumerAsks()
        {
            var board = CreateOfficialBoard();
            var participants = StagedBoardParticipants.Create();
            var modifications = RuleModificationSet.Empty;

            var insideBefore = BattleZoneQuery.FindEffectiveMinionsInside(board, participants, modifications);
            Assert.That(insideBefore, Is.Not.Empty, "The configured setup fills the starting zone.");

            // The Zone is moved through the board's own verified commit rather than through a Lane
            // Push, because what is under test here is which Zone the shared queries read - and since
            // milestone 6.11 a real push also removes and respawns, which would answer a different
            // question with the same call.
            Assert.That(board.CommitValidatedBattleZoneAdvance(BlueApproach), Is.True);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueApproach));

            Assert.That(
                BattleZoneQuery.FindEffectiveMinionsInside(board, participants, modifications), Is.Empty,
                "The Minion Battle now counts the minions in the Zone the Lane reached, and every " +
                "miniature is still standing in the one it left.");
            Assert.That(
                BattleZoneQuery.FindMinionsOutside(board).Count, Is.EqualTo(board.Minions.Count),
                "p.18's correction subjects are read against the same moved Zone.");
            Assert.That(
                BattleZoneQuery.FindCurrentBattleZoneCoordinates(board),
                Is.EqualTo(ZoneCoordinates(board.MapDefinition, BlueApproach)),
                "So are the destinations that correction returns a minion to.");

            var heavy = board.Minions.First(minion => minion.MinionType == StandardMinionType.Heavy);
            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForMinion(heavy.MinionId), RuleActivity.Attack, RuleObject.None,
                    board, participants, modifications),
                Is.False,
                "p.13's Heavy immunity holds only while the Heavy is in the Battle Zone, and this one " +
                "is no longer in it.");
            Assert.That(
                LanePushQualificationQuery.AfterRemoval(
                    LanePushCauseKind.Removal,
                    RuleObject.ForMinion(heavy.MinionId),
                    heavy.TeamSide,
                    heavy.Coordinate,
                    board, participants, modifications).Qualifies,
                Is.False,
                "p.8's own question is asked about the Zone the Lane has reached, so a removal in the " +
                "Zone it left qualifies nothing.");
        }

        [Test]
        public void TheStartingFlag_StillAnswersTheSetupQuestionItWasAlwaysAsked()
        {
            var board = CreateOfficialBoard();
            // The Zone is moved through the board's own verified commit rather than through a Lane
            // Push, because what is under test here is which Zone the shared queries read - and since
            // milestone 6.11 a real push also removes and respawns, which would answer a different
            // question with the same call.
            Assert.That(board.CommitValidatedBattleZoneAdvance(BlueApproach), Is.True);

            var minion = board.Minions.First();

            Assert.That(
                BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, minion.OccupiedSpace.SpaceId),
                Is.True,
                "Setup requires configured minions to be placed in the Zone the match starts in, and " +
                "that remains true of this minion however far the Lane is later pushed.");
            Assert.That(
                BattleZoneQuery.IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId), Is.False,
                "The two forms answer different questions, which is the whole reason both exist.");
        }

        // -------------------------------------------------------------------------------------------
        // Crossing the persistence boundaries
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AMovedZone_SurvivesSnapshotAndRestoreExactly()
        {
            var payload = CreateAdvancedPayload(out var setup, out var map, out var definitions);

            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload, definitions, new[] { map }, setup.Minions);

            Assert.That(restored.Board.CurrentBattleZoneId, Is.EqualTo(BlueApproach),
                "The Zone the Lane reached comes back from the payload, not from the map's flag.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "And it round-trips byte-identically, so the field is genuinely on the wire rather " +
                "than re-derived on the way out.");
        }

        [Test]
        public void ARestoredSessionHoldingAMovedZone_KeepsIt()
        {
            var payload = CreateAdvancedPayload(out _, out _, out _);
            var session = CreateSession();

            var restore = session.RestoreSnapshot(RewrapEnvelope(session, payload));

            Assert.That(restore.Accepted, Is.True, restore.Detail);
            Assert.That(ReadBattleZone(session), Is.EqualTo(BlueApproach));
        }

        [Test]
        public void ReplayUndoAndRestart_RebuildTheZoneRatherThanLosingIt()
        {
            var session = CreateSession();
            CommitBothSides(session);
            Assert.That(ReadBattleZone(session), Is.EqualTo(StartingBattleZone));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(ReadBattleZone(session), Is.EqualTo(StartingBattleZone),
                "A retained-prefix Undo replays the commands it kept and rebuilds the board, so the " +
                "Zone has to be part of that reconstruction rather than a field only a payload has.");

            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(ReadBattleZone(session), Is.EqualTo(StartingBattleZone));
        }

        // -------------------------------------------------------------------------------------------
        // Refusals on the way back in
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AZoneThisMapDoesNotDeclare_IsRefusedRatherThanReplacedWithTheFlag()
        {
            var payload = CreateAdvancedPayload(out var setup, out var map, out var definitions);
            var tampered = RewriteFirstZone(payload, "blue-approach", "blue-approacX");

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    tampered, definitions, new[] { map }, setup.Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload));
        }

        [Test]
        public void AZoneOfThisMapThatTheLaneCannotReach_IsRefused()
        {
            var payload = CreateAdvancedPayload(out var setup, out var map, out var definitions);
            var tampered = RewriteFirstZone(payload, "blue-approach", "top-jungle");

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    tampered, definitions, new[] { map }, setup.Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload),
                "The jungle is a real zone of this map and is not on the Lane, so no push could have " +
                "left the Battle Zone there.");
        }

        [Test]
        public void AMissingZoneOnAMapThatDeclaresOne_IsRefused()
        {
            var payload = CreateAdvancedPayload(out var setup, out var map, out var definitions);
            var tampered = RewriteFirstZone(payload, "blue-approach", string.Empty);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    tampered, definitions, new[] { map }, setup.Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload),
                "An absent Zone means a map that declares none; this map declares one, so the payload " +
                "contradicts the map it names rather than describing a Lane-free board.");
        }

        [Test]
        public void ARefusedRestore_LeavesTheSessionExactlyAsItWas()
        {
            var session = CreateSession();
            CommitBothSides(session);
            var before = session.CaptureSnapshot();
            var payload = CreateAdvancedPayload(out _, out _, out _);
            var tampered = RewrapEnvelope(
                session, RewriteFirstZone(payload, "blue-approach", "blue-approacX"));

            var restore = session.RestoreSnapshot(tampered);

            Assert.That(restore.Accepted, Is.False);
            Assert.That(restore.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.SnapshotRejected));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before),
                "Atomic: the refused payload left no part of itself behind.");
        }

        [Test]
        public void TheDirectRestorePath_RefusesTheSameZonesTheReaderDoes()
        {
            var board = CreateOfficialBoard();
            var captured = board.CaptureRestoreState();

            Assert.That(
                () => AdjacentMoveAuthority.Restore(
                    new BoardRestoreState(
                        captured.HeroOccupancies, captured.ObstacleOccupancies,
                        captured.RemainingMinions, captured.PlacedTokens, new ZoneId("top-jungle"),
                        captured.MinionBattleExclusions, captured.LastCompletedMovementWalk),
                    board.MapDefinition,
                    ConfiguredMinions()),
                Throws.ArgumentException,
                "One statement of what a legal Battle Zone is, consulted by the payload reader and by " +
                "a direct restore alike; two would eventually disagree.");

            Assert.That(
                () => AdjacentMoveAuthority.Restore(
                    new BoardRestoreState(
                        captured.HeroOccupancies, captured.ObstacleOccupancies,
                        captured.RemainingMinions, captured.PlacedTokens, null,
                        captured.MinionBattleExclusions, captured.LastCompletedMovementWalk),
                    board.MapDefinition,
                    ConfiguredMinions()),
                Throws.ArgumentException);
        }

        [Test]
        public void ThePriorSchema_IsRejectedRatherThanMigrated()
        {
            var payload = CreateAdvancedPayload(out var setup, out var map, out var definitions);
            var prior = RewriteSchemaVersion(
                payload, AuthoritativeSnapshotSerializer.CurrentSchemaVersion - 1);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    prior, definitions, new[] { map }, setup.Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion),
                "A v13 payload records where every piece stands and nothing about which Zone the Lane " +
                "reached, and reading one as 'still the starting Zone' would answer four live " +
                "questions about the wrong Zone for the rest of the match.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<HexCoordinate> ZoneCoordinates(BoardMapDefinition map, ZoneId zoneId) =>
            map.Spaces
                .Where(space => space.ZoneId != null && space.ZoneId.Value.Equals(zoneId))
                .Select(space => space.Coordinate)
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();

        /// <summary>A payload of a real match whose Lane has been pushed one Zone towards Blue.</summary>
        private static string CreateAdvancedPayload(
            out QuickGameSetupState setup,
            out BoardMapDefinition map,
            out IReadOnlyList<CardDefinition> definitions)
        {
            var session = CreateSession();
            map = session.Projection.SelectedMap;
            definitions = session.Projection.Definitions;
            setup = CreateSetup(map);

            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out _), Is.True);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload, definitions, new[] { map }, setup.Minions);

            Assert.That(restored.Board.CommitValidatedBattleZoneAdvance(BlueApproach), Is.True);
            Assert.That(restored.Board.CurrentBattleZoneId, Is.EqualTo(BlueApproach));
            return AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board);
        }

        /// <summary>Puts an authoritative payload back into the envelope the session restores from.</summary>
        private static string RewrapEnvelope(MatchSession session, string authoritative)
        {
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out _), Is.True);
            return RuleEventLogSnapshotEnvelope.Serialize(authoritative, envelope.Entries);
        }

        private static ZoneId? ReadBattleZone(MatchSession session)
        {
            var map = session.Projection.SelectedMap;
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out _), Is.True);
            return AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { map },
                CreateSetup(map).Minions).Board.CurrentBattleZoneId;
        }

        /// <summary>Replaces the first length-prefixed occurrence of a zone name in the decoded stream.</summary>
        private static string RewriteFirstZone(string payload, string existing, string replacement)
        {
            var bytes = Convert.FromBase64String(payload);
            var needle = new byte[Encoding.UTF8.GetByteCount(existing) + 1];
            needle[0] = (byte)(needle.Length - 1);
            Encoding.UTF8.GetBytes(existing, 0, existing.Length, needle, 1);

            var at = IndexOf(bytes, needle);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), $"'{existing}' is not in this payload.");

            var written = Encoding.UTF8.GetBytes(replacement);
            Assert.That(written.Length, Is.LessThan(128), "The 7-bit length prefix stays one byte.");
            var rewritten = new List<byte>(bytes.Length);
            rewritten.AddRange(bytes.Take(at));
            rewritten.Add((byte)written.Length);
            rewritten.AddRange(written);
            rewritten.AddRange(bytes.Skip(at + needle.Length));
            return Convert.ToBase64String(rewritten.ToArray());
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (var index = 0; index + needle.Length <= haystack.Length; index++)
            {
                var matched = true;
                for (var offset = 0; offset < needle.Length && matched; offset++)
                {
                    matched = haystack[index + offset] == needle[offset];
                }

                if (matched) return index;
            }

            return -1;
        }

        private static string RewriteSchemaVersion(string payload, int version)
        {
            var bytes = Convert.FromBase64String(payload);
            var versionAt = 1 + Encoding.UTF8.GetByteCount("GUARDS-AUTHORITATIVE-SNAPSHOT");
            BitConverter.GetBytes(version).CopyTo(bytes, versionAt);
            return Convert.ToBase64String(bytes);
        }

        private static QuickGameSetupState CreateSetup(BoardMapDefinition map) =>
            QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                StagedBoardParticipants.Create(),
                map,
                new TieBreakerState(TeamSide.Red));

        private static IEnumerable<QuickGameMinionSetupState> ConfiguredMinions() =>
            CreateSetup(OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap)).Minions;

        private static AdjacentMoveAuthority CreateOfficialBoard()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = CreateSetup(map);
            return new AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>(),
                setup.Minions);
        }

        private static MatchSession CreateSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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

        private static void CommitBothSides(MatchSession session)
        {
            foreach (var hero in session.Projection.Participants.Heroes)
            {
                var card = session.Projection.Cards.First(value =>
                    value.OwnerId.Equals(hero.Id) && value.State.Location == CardLocation.Hand);
                var result = session.Execute(DirectorCommand.CommitCard(hero.Id, card.Id));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }
        }
    }
}
