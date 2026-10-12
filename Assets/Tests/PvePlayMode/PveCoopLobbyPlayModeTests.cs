using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Pve.Play;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.Pve.PlayMode
{
    public sealed class PveCoopLobbyPlayModeTests
    {
        private readonly List<TcpSessionClientTransport> transports = new List<TcpSessionClientTransport>();
        private readonly List<PrivateSessionClient> clients = new List<PrivateSessionClient>();
        private PveCoopLobby lobby;
        private TcpSessionListener externalListener;
        private PrivateSessionHost externalServer;
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

        private void Pump()
        {
            if (externalListener != null)
            {
                foreach (var item in externalListener.Drain())
                    switch (item.Kind)
                    {
                        case TransportEventKind.Connected: externalServer.OnConnected(item.Ordinal, DateTime.UtcNow.Ticks); break;
                        case TransportEventKind.Frame: externalServer.OnFrame(item.Ordinal, item.Frame); break;
                        case TransportEventKind.Disconnected: externalServer.OnDisconnected(item.Ordinal); break;
                        case TransportEventKind.StreamRejected: externalServer.OnStreamRejected(item.Ordinal, item.Reason); break;
                    }
                externalServer.Sweep(DateTime.UtcNow.Ticks);
            }
            for (var i = 0; i < transports.Count; i++)
                foreach (var item in transports[i].Drain())
                    switch (item.Kind)
                    {
                        case TransportEventKind.Connected: clients[i].OnConnected(); break;
                        case TransportEventKind.Frame: clients[i].OnFrame(item.Frame); break;
                        case TransportEventKind.Disconnected: clients[i].OnDisconnected(); break;
                        case TransportEventKind.StreamRejected: clients[i].OnStreamRejected(item.Reason); break;
                    }
        }
        private IEnumerator Until(Func<bool> done, string reason)
        {
            var end = Time.realtimeSinceStartup + 45;
            while (!done() && Time.realtimeSinceStartup < end) { Pump(); yield return null; }
            var local = lobby == null ? null : Field<PrivateSessionClient>(lobby, "client");
            var authority = lobby == null ? null : Field<PveCoopMatch>(lobby, "match");
            Assert.That(done(), Is.True, reason + "; local=" + local?.State + "; feedback=" + local?.MatchFeedback +
                "; result=" + local?.LastSubmissionResult?.Accepted + "; version=" + authority?.Host.Version +
                "; roster=" + string.Join(",", local?.Roster.Select(row => row.Seat + ":" + row.Occupancy + ":" + row.IsReady) ?? Array.Empty<string>()) +
                "; remote=" + string.Join(",", clients.Select(client => client.State + ":" + client.IsReady)));
            yield return null;
        }
        private static void Click(VisualElement element)
        {
            Assert.That(element, Is.Not.Null);
            Assert.That(element.enabledInHierarchy, Is.True);
            var point = element.worldBound.center;
            var picked = element.panel.Pick(point);
            Assert.That(picked == element || element.Contains(picked), Is.True, "Control is pointer reachable: " + element.name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = point, button = 0, clickCount = 1 }))
            { down.target = picked; picked.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = point, button = 0, clickCount = 1 }))
            { up.target = picked; picked.SendEvent(up); }
        }

        [UnityTest]
        public IEnumerator HostMenuThreePlayersPlacementAndReconnectUseTheConfiguredScene()
        {
#if UNITY_EDITOR
            var load = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                PveLauncherPlayModeTests.LauncherScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
#endif
            yield return null; yield return null;
            var launcher = Object.FindAnyObjectByType<PveLauncher>();
            Click(launcher.GetComponent<UIDocument>().rootVisualElement.Q<Button>("pve-coop"));
            yield return null; yield return null;
            lobby = Object.FindAnyObjectByType<PveCoopLobby>();
            Assert.That(lobby, Is.Not.Null);
            Assert.That(Field<TextField>(lobby, "port").value,
                Is.EqualTo(PrivateSessionProtocol.DefaultPort.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Field<TextField>(lobby, "key").value = "pve-network-test-key";
            Field<TextField>(lobby, "port").value = "19531";
            var root = lobby.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<Label>("pve-coop-title").resolvedStyle.color, Is.EqualTo(Color.white));
            Assert.That(root.Q<Button>("pve-coop-host").resolvedStyle.color, Is.EqualTo(Color.black));
            Assert.That(Field<TextField>(lobby, "address").resolvedStyle.color, Is.EqualTo(Color.black));
            yield return new WaitForEndOfFrame();
            var menu = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w031-coop-menu-styled.png", menu.EncodeToPNG()); Object.Destroy(menu);
            Click(root.Q<Button>("pve-coop-host"));
            yield return Until(() => Field<PrivateSessionClient>(lobby, "client")?.GrantedSeat != null, "Host takes its own seat");
            var server = Field<PrivateSessionHost>(lobby, "server");
            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:19531", out var target, out _), Is.True);
            for (var i = 2; i <= 3; i++)
            {
                var transport = new TcpSessionClientTransport();
                var client = new PrivateSessionClient(server.Identity, new SeatId("blue-player-0" + i), "pve-network-test-key", transport, new CryptoNonceSource());
                transports.Add(transport); clients.Add(client);
                Assert.That(transport.Connect(target, 3, out var error), Is.True, error);
            }
            yield return Until(() => clients.All(client => client.GrantedSeat != null), "Two joiners claim their seats");
            foreach (var client in clients) Assert.That(client.TrySetReadiness(true), Is.True);
            Click(root.Q<Button>("pve-coop-ready"));
            yield return Until(() => root.Q<Button>("pve-coop-pick-hero-wasp").enabledSelf, "All players ready, host can pick");
            Click(root.Q<Button>("pve-coop-pick-hero-wasp"));
            yield return Until(() => clients[0].Roster[0].HasChosenHero, "Host's choice reaches joiners");
            Assert.That(clients[0].TryChooseHero("hero-arien"), Is.True);
            yield return Until(() => clients[1].Roster[1].HasChosenHero, "Second choice reaches third player");
            Assert.That(clients[1].TryChooseHero("hero-brogan"), Is.True);
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null, "Host enters configured match");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            Assert.That(runtime.Seat.Id.Value, Is.EqualTo("blue-player-01"));
            Assert.That(runtime.Seat.HeroId.Value, Is.EqualTo("hero-wasp"));
            var match = Field<PveCoopMatch>(lobby, "match");
            yield return Until(() => match.Session.Projection.PendingSystemTransitionStep != null, "Placement begins");
            for (var guard = 0; guard < 3; guard++)
            {
                var seat = match.Host.Seats.Seats.First(value => match.Host.ProjectionForSeat(value.Id).AuthorizedCommands
                    .Any(command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep));
                var command = match.Host.ProjectionForSeat(seat.Id).AuthorizedCommands.First(value => value.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
                var before = match.Host.Version;
                if (seat.Id.Value == "blue-player-01")
                {
                    yield return null; yield return null;
                    var item = match.Session.Projection.PendingSystemTransitionStep.OpenItems
                        .Single(value => GuardsDigital.Core.Rules.Transitions.ModeRoundStartPlacementRules
                            .TryReadItem(value.ItemId, out var hero) && hero.Equals(seat.HeroId));
                    Assert.That(GuardsDigital.Core.Rules.Transitions.ModeRoundStartPlacementRules
                        .TryReadOption(item.Options[0], out var space), Is.True);
                    var tile = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                        .Single(value => value.Coordinate.Equals(space));
                    var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                    runtime.RefreshAll();
                    yield return null;
                    Assert.That(pointer.TryHandlePointerRay(Camera.main.ScreenPointToRay(
                        Camera.main.WorldToScreenPoint(tile.transform.position))), Is.True,
                        "The playing host places through the configured board input.");
                }
                else
                {
                    var owner = clients.Single(client => client.TargetSeat.Equals(seat.Id));
                    yield return Until(() => owner.LatestProjection.Version == before, "Owner receives placement projection");
                    Assert.That(owner.TrySubmitCommand(command, out _), Is.True);
                }
                yield return Until(() => match.Host.Version > before, "Owner placement accepted for " + seat.Id);
            }
            yield return Until(() => runtime.ReadModel.Heroes.Count(hero => runtime.ReadModel.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue) == 3,
                "All three Blue heroes drawn");
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w031-coop-host.png", capture.EncodeToPNG()); Object.Destroy(capture);
            var hostClient = Field<PrivateSessionClient>(lobby, "client");
            Field<TcpSessionClientTransport>(lobby, "transport").Dispose();
            hostClient.OnDisconnected();
            yield return Until(() => server.IsPaused, "Disconnect pauses authority");
            yield return null;
            Click(root.Q<Button>("pve-coop-reconnect"));
            yield return Until(() => !server.IsPaused && hostClient.State == PrivateSessionClientState.Seated, "Menu reconnect restores the same seat");
            Assert.That(hostClient.TargetSeat.Value, Is.EqualTo("blue-player-01"));
            Assert.That(runtime.ReadModel.Cards.Where(card => runtime.ReadModel.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue)
                .All(card => card.OwnerId.Equals(runtime.Seat.HeroId)), Is.True);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (lobby != null)
            {
                foreach (var pointer in Object.FindObjectsByType<PrimitiveHeroPointerInput>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Object.Destroy(pointer.gameObject);
                }
                yield return null;
                Object.Destroy(lobby.gameObject);
                yield return null;
            }
            foreach (var transport in transports) transport.Dispose();
            transports.Clear(); clients.Clear();
            externalServer?.Shutdown(); externalListener?.Dispose();
            externalServer = null; externalListener = null;
        }

        [UnityTest]
        public IEnumerator SeedInheritancePreservesKeyContentAndFormatRejectionsWithoutLooping()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var map = OfficialBoardMapCatalog.Load().Load(layout.MapReference);
            var encounter = PveEncounterComposer.Compose(new PveEncounterSetup(new[] { "hero-arien", "hero-brogan", "hero-wasp" }, 42), layout, catalog, map);
            var normal = PrivateSessionIdentity.ForComposition(map, encounter.InitialState.InitialConfiguration, encounter.InitialState.Setup, BuildIdentityStamp.Current);
            var valid = PveCoopMatch.Identity(normal, 42, PveContentLoader.SaveCatalog(catalog),
                System.IO.File.ReadAllText(System.IO.Path.Combine(PveDataPaths.Directory, "encounter.json")));
            foreach (var expected in new[] { SessionRejectionReason.SessionKeyMismatch,
                SessionRejectionReason.ContentDigestMismatch, SessionRejectionReason.MatchFormatMismatch })
            {
                var identity = new PrivateSessionIdentity(valid.ProtocolFamily, valid.ProtocolVersion, valid.BuildIdentity,
                    valid.SnapshotSchemaVersion, valid.MapId, valid.MapContentRevision,
                    expected == SessionRejectionReason.MatchFormatMismatch ? "pve-coop-1-42" : valid.MatchFormatId,
                    expected == SessionRejectionReason.ContentDigestMismatch ? new string('a', 64) : valid.ContentDigest);
                Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:19533", out var endpoint, out _), Is.True);
                externalListener = new TcpSessionListener(); externalListener.Start(endpoint);
                externalServer = new PrivateSessionHost(new PveCoopMatch(encounter).Host, identity, "host-key", externalListener, new CryptoNonceSource());
                lobby = PveCoopLobby.Open(9876);
                Field<TextField>(lobby, "port").value = "19533";
                Field<TextField>(lobby, "key").value = expected == SessionRejectionReason.SessionKeyMismatch ? "wrong-key" : "host-key";
                yield return null; yield return null;
                Click(lobby.GetComponent<UIDocument>().rootVisualElement.Q<Button>("pve-coop-join"));
                yield return Until(() => Field<PrivateSessionClient>(lobby, "client")?.RejectionReason == expected,
                    "Seed discovery must retain " + expected);
                var refused = Field<PrivateSessionClient>(lobby, "client");
                Assert.That(refused.GrantedSeat, Is.Null);
                Assert.That(Field<bool>(lobby, "seedInheritanceAttempted"), Is.EqualTo(expected != SessionRejectionReason.MatchFormatMismatch));
                for (var frame = 0; frame < 20; frame++) { Pump(); yield return null; }
                Assert.That(Field<PrivateSessionClient>(lobby, "client"), Is.SameAs(refused), "Refusals must not trigger retry loops.");
                yield return Cleanup();
            }
        }

        [UnityTest]
        public IEnumerator JoinMenuClaimsOwnSeatAndDrawsRemoteEnemyPlansWithoutAuthority()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var map = OfficialBoardMapCatalog.Load().Load(layout.MapReference);
            PveComposedEncounter Compose(string[] ids) => PveEncounterComposer.Compose(new PveEncounterSetup(ids, 42), layout, catalog, map);
            var initial = Compose(new[] { "hero-arien", "hero-brogan", "hero-wasp" });
            var baseIdentity = PrivateSessionIdentity.ForComposition(map, initial.InitialState.InitialConfiguration, initial.InitialState.Setup, BuildIdentityStamp.Current);
            var identity = PveCoopMatch.Identity(baseIdentity, 42, PveContentLoader.SaveCatalog(catalog),
                System.IO.File.ReadAllText(System.IO.Path.Combine(PveDataPaths.Directory, "encounter.json")));
            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:19532", out var endpoint, out _), Is.True);
            externalListener = new TcpSessionListener(); externalListener.Start(endpoint);
            var match = new PveCoopMatch(initial);
            externalServer = new PrivateSessionHost(match.Host, identity, "pve-seed-test-key", externalListener, new CryptoNonceSource());
            externalServer.SeatComposition = selection =>
            {
                var choices = selection.PickOrder.Select(seat => { selection.TryGetChoice(seat.Seat, out var hero); return hero.HeroIdValue; }).ToArray();
                match = new PveCoopMatch(Compose(choices)); return match.Host;
            };
            foreach (var seat in new[] { "blue-player-01", "blue-player-03" })
            {
                var transport = new TcpSessionClientTransport();
                var client = new PrivateSessionClient(identity, new SeatId(seat), "pve-seed-test-key", transport, new CryptoNonceSource());
                transports.Add(transport); clients.Add(client);
                Assert.That(transport.Connect(endpoint, 3, out var problem), Is.True, problem);
            }
            lobby = PveCoopLobby.Open(9876);
            Field<TextField>(lobby, "seedInput").value = "ignored when joining";
            Field<TextField>(lobby, "key").value = "pve-seed-test-key";
            Field<TextField>(lobby, "port").value = "19532";
            var root = lobby.GetComponent<UIDocument>().rootVisualElement;
            yield return null; yield return null;
            Click(root.Q<Button>("pve-coop-join"));
            yield return Until(() => Field<PrivateSessionClient>(lobby, "client")?.State == PrivateSessionClientState.Admitted,
                "Join opens the host's three-seat roster");
            Assert.That(Field<ulong>(lobby, "seed"), Is.EqualTo(42UL), "Host seed is adopted without pre-sharing.");
            Assert.That(Field<bool>(lobby, "seedInheritanceAttempted"), Is.True);
            Click(root.Query<Button>().ToList().Single(button => button.text == "Take blue-player-02"));
            yield return Until(() => Field<PrivateSessionClient>(lobby, "client").GrantedSeat != null, "Joiner claims second seat");
            yield return Until(() => clients.All(client => client.GrantedSeat != null), "Other two seats admitted");
            foreach (var client in clients) Assert.That(client.TrySetReadiness(true), Is.True);
            Click(root.Q<Button>("pve-coop-ready"));
            yield return Until(() => externalServer.ReadySeatCount == 3, "All three players ready");
            Assert.That(clients[0].TryChooseHero("hero-wasp"), Is.True);
            yield return Until(() => root.Q<Button>("pve-coop-pick-hero-arien").enabledSelf, "Joiner's turn to pick");
            yield return new WaitForEndOfFrame();
            var lobbyCapture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w031-coop-join-menu.png", lobbyCapture.EncodeToPNG()); Object.Destroy(lobbyCapture);
            Click(root.Q<Button>("pve-coop-pick-hero-arien"));
            yield return Until(() => clients[1].Roster[1].HasChosenHero, "Choice reaches host and third player");
            Assert.That(clients[1].TryChooseHero("hero-brogan"), Is.True);
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null, "Joiner enters configured scene");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            Assert.That(runtime.Seat.Id.Value, Is.EqualTo("blue-player-02"));
            Assert.That(runtime.Seat.HeroId.Value, Is.EqualTo("hero-arien"));
            Assert.That(Field<PrivateSessionHost>(lobby, "server"), Is.Null, "Joiner never hosts an authority");
            yield return null; yield return null;
            var overlay = Object.FindAnyObjectByType<PveEncounterView>();
            Assert.That(overlay.Session, Is.Null);
            Assert.That(overlay.GetComponent<UIDocument>().rootVisualElement.Q("pve-enemy-rail").resolvedStyle.height,
                Is.EqualTo(414).Within(1), "Joining clients fit the rail to six live trays.");
            Assert.That(overlay.GetComponent<UIDocument>().rootVisualElement.Query<VisualElement>()
                .ToList().Count(element => element.name != null && element.name.StartsWith("pve-enemy-pve-enemy-")), Is.EqualTo(6),
                "Only standing enemies have trays on a joining client.");
            var remotePlanSlots = overlay.GetComponent<UIDocument>().rootVisualElement
                .Query<VisualElement>("gd-turn-slot").ToList();
            Assert.That(remotePlanSlots.Count, Is.EqualTo(24));
            Assert.That(remotePlanSlots.All(slot => slot.resolvedStyle.opacity >= .99f), Is.True,
                "The joining client's unresolved enemy plans remain at full strength.");
            Assert.That(runtime.ReadModel.Cards.Any(card => match.Encounter.FindEnemy(card.OwnerId) != null), Is.True);
            yield return PveLauncherPlayModeTests.AssertLinkedHover(runtime,
                overlay.GetComponent<UIDocument>().rootVisualElement, match.Encounter.Enemies[0].HeroId);
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w032-coop-join.png", capture.EncodeToPNG()); Object.Destroy(capture);
            var joinedClient = Field<PrivateSessionClient>(lobby, "client");
            Field<TcpSessionClientTransport>(lobby, "transport").Dispose();
            joinedClient.OnDisconnected();
            yield return Until(() => externalServer.IsPaused, "Joiner disconnect pauses the match");
            Click(root.Q<Button>("pve-coop-reconnect"));
            yield return Until(() => !externalServer.IsPaused && joinedClient.State == PrivateSessionClientState.Seated,
                "Seed-inheriting joiner reconnects to the same seat");
            Assert.That(Field<ulong>(lobby, "seed"), Is.EqualTo(42UL));
            Assert.That(joinedClient.TargetSeat.Value, Is.EqualTo("blue-player-02"));
            externalServer.Shutdown();
            yield return Until(() => Field<PrivateSessionClient>(lobby, "client").State != PrivateSessionClientState.Seated, "Host departure stops the joiner");
        }
    }
}
