using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExperimentalCoordinationTests
    {
        private static MatchHost Create(bool enabled = true)
        {
            var state = OwnerScopedProjectionTests.CreateOfficialInitialState();
            return MatchHost.Compose(new MatchSessionInitialState(state.Scenario, state.Setup, state.AvailableMaps,
                state.InitialConfiguration, state.InitialTiming, state.StagedProgression,
                new ExperimentalMatchOptions(teammateCoordination: enabled)));
        }
        private static MatchHost Create(ExperimentalMatchOptions options)
        {
            var state = OwnerScopedProjectionTests.CreateOfficialInitialState();
            return MatchHost.Compose(new MatchSessionInitialState(state.Scenario, state.Setup, state.AvailableMaps,
                state.InitialConfiguration, state.InitialTiming, state.StagedProgression, options));
        }
        [TestCase("")] [TestCase("marker-test")]
        public void MarkerOnlyUpdatesPreserveGameplayProjectionAndLogIdentity(string key)
        {
            var host=Create(); var wire=PrivateOnlineWireHarness.Compose(host,key); wire.SeatAll(); wire.ReadyAndPickEverySeat();
            var sender=host.Seats.Seats[0]; var machine=wire.Client(1);
            var adapter=new NetworkMatchClient(machine,host.Projection,MatchContentCatalogs.ForThisBuild());
            var before=machine.LatestProjection; var model=adapter.Projection;
            var revision=machine.ProjectionRevision; var log=machine.MatchLogRevision;
            var hex=host.Projection.SelectedMap.Spaces.First().Coordinate;
            Assert.That(machine.TrySubmitCoordination(new CoordinationCommand(0,CoordinationTargetKind.Hex,false,
                host.Projection.SelectedMap.Reference,hex)),Is.True);
            wire.Pump();
            Assert.That(machine.LatestProjection,Is.SameAs(before),"A highlight must never replace the gameplay projection.");
            Assert.That(adapter.Projection,Is.SameAs(model));
            Assert.That(machine.ProjectionRevision,Is.EqualTo(revision));
            Assert.That(machine.MatchLogRevision,Is.EqualTo(log));
            Assert.That(((IExperimentalReadModel)adapter.Projection).TeammateMarkers.Single().Hex,Is.EqualTo(hex));
            Assert.That(host.Version,Is.EqualTo(before.Version));
        }
        [Test] public void MarkerFramesCannotAcknowledgeCommandsOrReplaceNewerOrDifferentGameState()
        {
            var host=Create();var wire=PrivateOnlineWireHarness.Compose(host);wire.SeatAll();wire.ReadyAndPickEverySeat();
            var machine=wire.Client(1);var original=machine.LatestProjection;var first=machine.LatestCoordination;
            Assert.That(machine.TrySubmitCommand(original.AuthorizedCommands[0],out _),Is.True);
            void Deliver(long sequence,long version,long revision,int generation)
            {
                var bytes=NetworkMessageCodec.CoordinationUpdateFrame(new NetworkEnvelope(machine.SessionCorrelation,
                    machine.ConnectionEpoch,sequence,0),new CoordinationDisplayState(version,revision,generation,first.Markers));
                machine.OnFrame(new SessionFrame(SessionMessageKind.CoordinationUpdate,bytes.Skip(PrivateSessionProtocol.FrameHeaderBytes).ToArray()));
            }
            Deliver(1,original.Version+1,first.Revision+1,first.Generation);
            Assert.That(machine.LatestCoordination,Is.SameAs(first));
            Deliver(2,original.Version,first.Revision+1,first.Generation+1);
            Assert.That(machine.LatestCoordination,Is.SameAs(first));
            Deliver(3,original.Version,first.Revision+2,first.Generation);
            var accepted=machine.LatestCoordination;
            Assert.That(accepted,Is.Not.SameAs(first));
            Deliver(3,original.Version,first.Revision+3,first.Generation);
            Deliver(2,original.Version,first.Revision+3,first.Generation);
            Deliver(4,original.Version,first.Revision+1,first.Generation);
            Assert.That(machine.LatestCoordination,Is.SameAs(accepted));
            Assert.That(machine.LatestProjection,Is.SameAs(original));
            Assert.That(machine.IsWaitingForHost,Is.True);
            wire.Pump();
            Assert.That(machine.IsWaitingForHost,Is.False);
            Assert.That(machine.RecoveredProjectionGapCount,Is.Zero);
            wire.DisconnectClient(1);var reconnected=wire.Client(wire.ReconnectClient(1));
            var current=reconnected.LatestProjection;var revisionBefore=reconnected.ProjectionRevision;
            wire.Session.PublishCoordinationUpdate();wire.Pump();
            Assert.That(reconnected.LatestProjection,Is.SameAs(current));
            Assert.That(reconnected.ProjectionRevision,Is.EqualTo(revisionBefore));
            Assert.That(reconnected.LatestCoordination.Generation,Is.EqualTo(current.CoordinationGeneration));
            Assert.That(reconnected.RecoveredProjectionGapCount,Is.Zero);
        }
        [TestCase("2v2-quick")]
        [TestCase("2v2-long")]
        [TestCase("3v3-quick")]
        [TestCase("3v3-long")]
        public void EverySupportedFormatScopesJoinedHandsAndSuggestions(string formatId)
        {
            var format = MatchFormat.All.Single(f => f.Id == formatId);
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(format, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(QuickGameSetupConfiguration.CreateOfficial(format),
                configuration.Participants, map, configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(configuration.Participants, configuration.CardDefinitions,
                configuration.CardInstances, configuration.TieBreakerState, configuration.SetupSeed, map.Reference,
                setup.Heroes.Select(h => new HeroOccupancy(h.HeroId, h.Coordinate)), Array.Empty<ObstacleOccupancy>());
            var host = MatchHost.Compose(new MatchSessionInitialState(scenario, setup, new[] { map },
                configuration, experiments: new ExperimentalMatchOptions(teammateCoordination: true)));
            foreach (var sender in host.Seats.Seats)
            {
                var own = host.ProjectionForSeat(sender.Id);
                Assert.That(own.AlliedHandCards.Select(c => c.OwnerId).Distinct().Count(), Is.EqualTo(format.HeroesPerTeam - 1));
                var suggested = own.AlliedHandCards.First();
                Assert.That(host.SubmitCoordinationForSeat(sender.Id, new CoordinationCommand(0,
                    CoordinationTargetKind.Card, false, map.Reference, cardOwner: suggested.OwnerId, card: suggested.Id)), Is.True);
            }
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(view), out var joined), Is.True);
                Assert.That(joined.Experiments.TeammateCoordination, Is.True);
                Assert.That(joined.Experiments.TurnTimer, Is.False);
                Assert.That(joined.TeammateMarkers.Count, Is.EqualTo(format.HeroesPerTeam));
                Assert.That(joined.AlliedHandCards.Count, Is.EqualTo(view.AlliedHandCards.Count));
                Assert.That(view.TeammateMarkers.All(m => host.Seats.FindByHero(m.Sender).TeamSide == seat.TeamSide), Is.True);
                Assert.That(view.AuthorizedCommands.All(c => !view.AlliedHandCards.Any(a => a.Id.Equals(c.CardInstanceId))), Is.True);
            }
        }
        [Test] public void HostSnapshotsRetainOptionsAndRejectMismatchedGamesAtomically()
        {
            var host = Create(); var snapshot = host.CaptureSnapshot();
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.True);
            Assert.That(host.Experiments.TeammateCoordination, Is.True);
            var other = Create(false); var before = other.CaptureSnapshot();
            Assert.That(other.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.False);
            Assert.That(other.CaptureSnapshot(), Is.EqualTo(before));
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(before)).Accepted, Is.False);
        }
        [Test] public void MinionBattleCapOptionsRoundTripAndSnapshotsRejectDifferentValuesAtomically()
        {
            var options = new ExperimentalMatchOptions(
                minionBattleRemovalCapEnabled: true,
                minionBattleRemovalCap: 0);
            Assert.That(options.MinionBattleRemovalCapEnabled, Is.True);
            Assert.That(options.MinionBattleRemovalCap, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperimentalMatchOptions(
                minionBattleRemovalCapEnabled: true,
                minionBattleRemovalCap: -1));

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                ExperimentalWireCodec.WriteOptions(writer, options);
                writer.Flush();
                stream.Position = 0;
                using (var reader = new System.IO.BinaryReader(stream))
                {
                    Assert.That(ExperimentalWireCodec.ReadOptions(reader), Is.EqualTo(options));
                    Assert.That(stream.Position, Is.EqualTo(stream.Length));
                }
            }

            var host = Create(options);
            var snapshot = host.CaptureSnapshot();
            Assert.That(snapshot.StartsWith("guards-experimental/4:"), Is.True);
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.True);
            var other = Create(new ExperimentalMatchOptions(
                minionBattleRemovalCapEnabled: true,
                minionBattleRemovalCap: 1));
            var before = other.CaptureSnapshot();
            Assert.That(other.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.False);
            Assert.That(other.CaptureSnapshot(), Is.EqualTo(before));
        }
        [TestCase("")]
        [TestCase("coordination-private-session")]
        public void RealWireDeliversPrivateMarkersWithoutBlockingAnOutstandingGameplaySubmission(string key)
        {
            var host = Create(); var wire = PrivateOnlineWireHarness.Compose(host, key); wire.SeatAll(); wire.ReadyAndPickEverySeat();
            var sender = host.Seats.Seats[0]; var machine = wire.Client(1);
            var before = host.Version;
            var hex = host.Projection.SelectedMap.Spaces.First().Coordinate;
            var command = new CoordinationCommand(0, CoordinationTargetKind.Hex, false, host.Projection.SelectedMap.Reference, hex);
            var gameplay = host.ProjectionForSeat(sender.Id).AuthorizedCommands.First(c => c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard);
            Assert.That(machine.TrySubmitCommand(gameplay, out _), Is.True);
            Assert.That(machine.TrySubmitCoordination(command), Is.True);
            wire.Pump();
            Assert.That(host.Version, Is.EqualTo(before + 1));
            Assert.That(machine.IsWaitingForHost, Is.False);
            for (int i = 0; i < host.Seats.Seats.Count; i++)
            {
                var record = wire.Client(i + 1).LatestCoordination;
                Assert.That(record.Markers.Count, Is.EqualTo(host.Seats.Seats[i].TeamSide == sender.TeamSide ? 1 : 0));
            }
            wire.DisconnectClient(1);
            Assert.That(host.ProjectionForSeat(sender.Id).TeammateMarkers, Is.Empty);
            for (int i = 2; i <= host.Seats.Seats.Count; i++)
                Assert.That(wire.Client(i).LatestProjection.TeammateMarkers, Is.Empty, "Departure must clear remote views even while play is paused.");
        }
        [Test] public void SignalsRemainIndependentAcrossCommitmentActionsAndPendingResponses()
        {
            var host = Create(); var sender = host.Seats.Seats[0];
            var phases = new System.Collections.Generic.HashSet<string>();
            OwnerScopedProjectionTests.DriveThroughSeats(host, 400, source =>
            {
                phases.Add(source.Phase.ToString());
                if (source.DecisionContract != null) phases.Add(source.DecisionContract.FamilyId.Value);
                var view = host.ProjectionForSeat(sender.Id);
                var previous = view.TeammateMarkers.FirstOrDefault(m => m.Sender.Equals(sender.HeroId));
                var version = host.Version; var sequence = host.Sequence;
                var hex = source.SelectedMap.Spaces[(int)(sequence % source.SelectedMap.Spaces.Count)].Coordinate;
                Assert.That(host.SubmitCoordinationForSeat(sender.Id, new CoordinationCommand(previous?.Revision ?? 0,
                    CoordinationTargetKind.Hex, false, source.SelectedMap.Reference, hex)), Is.True);
                Assert.That(host.Version, Is.EqualTo(version)); Assert.That(host.Sequence, Is.EqualTo(sequence));
                return false;
            }, 6);
            Assert.That(phases.Count, Is.GreaterThanOrEqualTo(4), string.Join(",", phases));
            var old = host.ProjectionForSeat(sender.Id);
            Assert.That(host.Submit(MatchSubmission.ForRestart()).Accepted, Is.True);
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, new CoordinationCommand(0, CoordinationTargetKind.Hex,
                false, old.SelectedMap.Reference, old.SelectedMap.Spaces.First().Coordinate,
                generation: old.CoordinationGeneration)), Is.False, "An old match cannot mark a restarted one.");
            Assert.That(host.ProjectionForSeat(sender.Id).TeammateMarkers, Is.Empty);
        }
        [Test] public void TwoTeammatesCanMarkOneHexAndEachOwnsOnlyTheirOwnSignal()
        {
            var host = Create(); var red = host.Seats.ForSide(TeamSide.Red).ToArray();
            var hex = host.Projection.SelectedMap.Spaces.First().Coordinate;
            var command = new CoordinationCommand(0, CoordinationTargetKind.Hex, false, host.Projection.SelectedMap.Reference, hex);
            Assert.That(host.SubmitCoordinationForSeat(red[0].Id, command), Is.True);
            Assert.That(host.SubmitCoordinationForSeat(red[1].Id, command), Is.True);
            Assert.That(host.ProjectionForSeat(red[2].Id).TeammateMarkers.Count(m => m.Hex.HasValue), Is.EqualTo(2));
            Assert.That(host.SubmitCoordinationForSeat(red[0].Id, new CoordinationCommand(1,
                CoordinationTargetKind.Hex, true, host.Projection.SelectedMap.Reference)), Is.True);
            Assert.That(host.ProjectionForSeat(red[2].Id).TeammateMarkers.Single(m => m.Hex.HasValue).Sender, Is.EqualTo(red[1].HeroId));
        }
        [Test] public void EveryHostConstructionReadsTheSameImmutableInitialOptions()
        {
            var state = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var options = new ExperimentalMatchOptions(teammateCoordination: true);
            var initial = new MatchSessionInitialState(state.Scenario, state.Setup, state.AvailableMaps,
                state.InitialConfiguration, experiments: options);
            var host = new MatchHost(new MatchSession(initial));
            Assert.That(host.Experiments, Is.SameAs(options));
            Assert.That(host.ProjectionForSeat(host.Seats.Seats[0].Id).Experiments, Is.SameAs(options));
        }
        [Test] public void DefaultsAreDisabledWithTheRequestedEditableDurations()
        {
            var options = new ExperimentalMatchOptions();
            Assert.That(options.TeammateCoordination || options.TurnTimer, Is.False);
            Assert.That(new[] { options.CardCommitmentSeconds, options.CardResolutionSeconds, options.ResponseSeconds,
                options.CardUpgradeSeconds, options.TeamBankSeconds }, Is.EqualTo(new[] {120,30,30,120,300}));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperimentalMatchOptions(responseSeconds: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperimentalMatchOptions(teamBankSeconds: int.MaxValue));
            Assert.That(new ExperimentalMatchOptions(teamBankSeconds: 0).TeamBankSeconds, Is.Zero);
            Assert.That(options.MinionBattleRemovalCapEnabled, Is.False);
            Assert.That(options.MinionBattleRemovalCap, Is.Zero);
        }
        [Test] public void AlliedHandsTravelSeparatelyAndNeverGrantActionsOrEnemyInformation()
        {
            var host = Create();
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(view.AlliedHandCards, Is.Not.Empty);
                Assert.That(view.AlliedHandCards.All(c => c.State.Location == CardLocation.Hand &&
                    host.Seats.FindByHero(c.OwnerId).TeamSide == seat.TeamSide && !seat.Owns(c.OwnerId)), Is.True);
                Assert.That(view.OwnCards.Any(c => !seat.Owns(c.OwnerId)), Is.False);
                var ids = view.AlliedHandCards.Select(c => c.Id).ToArray();
                Assert.That(view.Cards.Any(c => ids.Contains(c.Id)), Is.False);
                Assert.That(view.AuthorizedCommands, Is.EquivalentTo(Create(false).ProjectionForSeat(seat.Id).AuthorizedCommands));
                var bytes = NetworkSeatProjectionCodec.Encode(view);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(bytes, out var decoded), Is.True);
                Assert.That(decoded.AlliedHandCards.Count, Is.EqualTo(view.AlliedHandCards.Count));
                foreach (var enemy in host.Seats.Seats.Where(s => s.TeamSide != seat.TeamSide))
                    Assert.That(decoded.AlliedHandCards.Any(c => c.OwnerHeroId == enemy.HeroId.Value), Is.False);
            }
        }
        [Test] public void MarkersAreTeamPrivateAndNeverInvalidateGameplayOrEnterSnapshots()
        {
            var host = Create(); var sender = host.Seats.ForSide(TeamSide.Red).First();
            var before = host.CaptureSnapshot(); var version = host.Version; var sequence = host.Sequence;
            var hex = host.Projection.SelectedMap.Spaces.First().Coordinate;
            var command = new CoordinationCommand(0, CoordinationTargetKind.Hex, false, host.Projection.SelectedMap.Reference, hex);
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, command), Is.True);
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, command), Is.False);
            foreach (var seat in host.Seats.Seats)
                Assert.That(host.ProjectionForSeat(seat.Id).TeammateMarkers.Count, Is.EqualTo(seat.TeamSide == sender.TeamSide ? 1 : 0));
            Assert.That(host.Version, Is.EqualTo(version)); Assert.That(host.Sequence, Is.EqualTo(sequence));
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(before));
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, new CoordinationCommand(1,
                CoordinationTargetKind.Hex, true, host.Projection.SelectedMap.Reference)), Is.True);
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, command), Is.False);
            Assert.That(host.ProjectionForSeat(sender.Id).TeammateMarkers.Single().Hex, Is.Null);
        }
        [Test] public void DisabledUnknownAndEnemyCardRequestsCannotCreateSignals()
        {
            var host = Create(); var sender = host.Seats.ForSide(TeamSide.Red).First();
            var enemy = host.Seats.ForSide(TeamSide.Blue).First();
            var card = host.Projection.Cards.First(c => c.OwnerId.Equals(enemy.HeroId) && c.State.Location == CardLocation.Hand);
            var command = new CoordinationCommand(0, CoordinationTargetKind.Card, false, host.Projection.SelectedMap.Reference,
                cardOwner: enemy.HeroId, card: card.Id);
            Assert.That(host.SubmitCoordinationForSeat(sender.Id, command), Is.False);
            Assert.That(host.SubmitCoordinationForSeat(new SeatId("unknown"), command), Is.False);
            var hex = new CoordinationCommand(0, CoordinationTargetKind.Hex, false, host.Projection.SelectedMap.Reference,
                host.Projection.SelectedMap.Spaces.First().Coordinate);
            Assert.That(Create(false).SubmitCoordinationForSeat(sender.Id, hex), Is.False);
        }
        [Test] public void CardSuggestionsUseInstanceIdentityAndStaleTargetsAreRemoved()
        {
            var host = Create(); var seats = host.Seats.ForSide(TeamSide.Red).ToArray();
            var card = host.Projection.Cards.First(c => c.OwnerId.Equals(seats[1].HeroId) && c.State.Location == CardLocation.Hand);
            var command = new CoordinationCommand(0, CoordinationTargetKind.Card, false, host.Projection.SelectedMap.Reference,
                cardOwner: card.OwnerId, card: card.Id);
            Assert.That(host.SubmitCoordinationForSeat(seats[0].Id, command), Is.True);
            Assert.That(host.ProjectionForSeat(seats[1].Id).TeammateMarkers.Single().Card, Is.EqualTo(card.Id));
            var own = host.ProjectionForSeat(seats[1].Id);
            var commit = own.AuthorizedCommands.First(c => c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard && c.CardInstanceId.Equals(card.Id));
            Assert.That(host.SubmitForSeat(seats[1].Id, new SeatSubmission(new SubmissionId("commit-suggested"), own.Version, commit)).Accepted, Is.True);
            Assert.That(host.ProjectionForSeat(seats[0].Id).TeammateMarkers.Single().Card.IsDefined, Is.False);
        }
    }
}
