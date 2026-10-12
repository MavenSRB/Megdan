using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Presentation;
using GuardsDigital.Pve.Play;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.Pve.PlayMode
{
    public sealed class PveSharedLobbyPlayModeTests
    {
        private sealed class RemoteOutbox : IPrivateSessionClientOutbox
        {
            public TcpSessionClientTransport Transport;
            public void Send(byte[] frame) => Transport.Send(frame);
            public void Close() => Transport.Close();
        }
        private sealed class RemotePeer : IDisposable
        {
            public readonly RemoteOutbox Outbox = new RemoteOutbox();
            public readonly TcpSessionClientTransport Transport = new TcpSessionClientTransport();
            public readonly PrivateSessionClient Client;

            public RemotePeer(PrivateSessionIdentity identity, SeatId seat, string key)
            {
                Outbox.Transport = Transport;
                Client = new PrivateSessionClient(identity, seat, key, Outbox, new CryptoNonceSource());
            }

            public void Pump()
            {
                foreach (var e in Transport.Drain())
                {
                    if (e.Kind == TransportEventKind.Connected) Client.OnConnected();
                    else if (e.Kind == TransportEventKind.Frame) Client.OnFrame(e.Frame);
                    else if (e.Kind == TransportEventKind.Disconnected) Client.OnDisconnected();
                    else if (e.Kind == TransportEventKind.StreamRejected) Client.OnStreamRejected(e.Reason);
                }
            }

            public void Dispose()
            {
                Client.SendGoodbye();
                Transport.Dispose();
            }
        }
        private PveSharedLobby lobby;
        private GameObject entryObject;
        private PanelSettings entrySettings;
        private TcpSessionClientTransport remoteTransport;
        private PrivateSessionClient remote;
        private PveSharedLobby servingLobby;
        private TcpSessionListener manuallyPumpedListener;
        private PrivateSessionHost manuallyPumpedServer;
        private readonly List<RemotePeer> peers = new List<RemotePeer>();
        private static T Field<T>(object instance, string name) => (T)instance.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        private static string ReadLive(string path)
        {
            using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            using (var reader = new System.IO.StreamReader(stream)) return reader.ReadToEnd();
        }
        private IEnumerator Until(Func<bool> predicate, string message, Func<string> detail = null)
        {
            var end = Time.realtimeSinceStartup + 30;
            while (!predicate() && Time.realtimeSinceStartup < end)
            {
                if (manuallyPumpedListener != null && manuallyPumpedServer != null)
                {
                    var now = DateTime.UtcNow.Ticks;
                    foreach (var e in manuallyPumpedListener.Drain())
                    {
                        if (e.Kind == TransportEventKind.Connected) manuallyPumpedServer.OnConnected(e.Ordinal, now);
                        else if (e.Kind == TransportEventKind.Frame) manuallyPumpedServer.OnFrame(e.Ordinal, e.Frame);
                        else if (e.Kind == TransportEventKind.Disconnected) manuallyPumpedServer.OnDisconnected(e.Ordinal);
                        else if (e.Kind == TransportEventKind.StreamRejected) manuallyPumpedServer.OnStreamRejected(e.Ordinal, e.Reason);
                    }
                    manuallyPumpedServer.Sweep(now);
                    manuallyPumpedServer.DrainEvents();
                }
                if (remoteTransport != null)
                    foreach (var e in remoteTransport.Drain())
                    {
                        if (e.Kind == TransportEventKind.Connected) remote.OnConnected();
                        else if (e.Kind == TransportEventKind.Frame) remote.OnFrame(e.Frame);
                        else if (e.Kind == TransportEventKind.Disconnected) remote.OnDisconnected();
                    }
                foreach (var peer in peers) peer.Pump();
                yield return null;
            }
            Assert.That(predicate(), Is.True, detail == null ? message : message + ": " + detail());
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreshProcessRejoinReclaimsReservedSeatAndLivePveProjection()
        {
            const string key = "fresh-pve-rejoin-test";
            const int port = 45794;
            const ulong seed = 74;
            var retained = new InMemoryRetainedSeatCapabilityStore();
            typeof(PveSharedLobby).GetField("retainedCapabilityStoreFactory",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, new Func<IRetainedSeatCapabilityStore>(() => retained));

            servingLobby = PveSharedLobby.Open(new PrivateLobbyEntry("", port.ToString(), key, default,
                "Host", modeId: PrivateLobbyMode.PveId), true, seed);
            yield return Until(() => Field<PrivateSessionClient>(servingLobby, "active")?.State ==
                PrivateSessionClientState.Admitted, "Host admitted before seats are claimed");
            var view = Field<PrivateLobbyUnityView>(servingLobby, "view");
            for (var count = 1; count <= 2; count++)
            {
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.SeatPickName).ToList().First());
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(servingLobby, "active").Roster.Count(row =>
                    row.Occupancy == SeatOccupancyState.Claimed) == expected, "Host claims seat " + count);
            }

            var server = Field<PrivateSessionHost>(servingLobby, "server");
            remoteTransport = new TcpSessionClientTransport();
            var remoteOutbox = new RemoteOutbox { Transport = remoteTransport };
            remote = new PrivateSessionClient(server.Identity, new SeatId("blue-player-03"), key,
                remoteOutbox, new CryptoNonceSource(), retainedCapabilities: retained);
            Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", port, out var endpoint, out var problem),
                Is.True, problem);
            Assert.That(remoteTransport.Connect(endpoint, 3, out problem), Is.True, problem);
            yield return Until(() => remote.GrantedSeat != null, "Third player claims the seat that will be reclaimed");
            Assert.That(remote.TrySetReadiness(true), Is.True);
            Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(button =>
                button.text == "READY"));
            for (var count = 1; count <= 2; count++)
            {
                yield return Until(() => Field<PrivateLobbyModel>(servingLobby, "model").Selection.IsOwnPick,
                    "Host receives hero pick " + count);
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList().First(button => button.enabledSelf));
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(button =>
                    button.text == "PICK HERO"));
                var expected = count;
                yield return Until(() => remote.Roster.Count(row => row.HasChosenHero) == expected,
                    "Returning player receives host pick " + count);
            }
            yield return Until(() => PrivateLobbyModel.ForClient(remote).Selection.IsOwnPick &&
                                     server.IsPickPhaseOpen &&
                                     server.Selection.PickingSeat.Equals(remote.TargetSeat),
                "Returning player and authority agree on the final hero pick");
            var returningHero = server.Selection.AvailableHeroes.First().HeroIdValue;
            Assert.That(remote.TryChooseHero(returningHero), Is.True);
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(servingLobby, "runtime") != null &&
                remote.HasMatchStarted, "PvE authority and original player enter the match",
                () => "client=" + remote.State + ", rejection=" + remote.RejectionDetail +
                      ", host-picking-seat=" + server.Selection.PickingSeat.Value +
                      ", host-pick-open=" + server.IsPickPhaseOpen);

            var hostOwner = Field<PrivateSessionClient>(servingLobby, "active");
            yield return Until(() => hostOwner.LatestProjection?.AuthorizedCommands.Any(command =>
                command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep) == true,
                "Host owner receives the first placement");
            var placement = hostOwner.LatestProjection.AuthorizedCommands.First(command =>
                command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep);
            var placedHero = Field<ConfiguredMatchSceneRuntime>(servingLobby, "runtime")
                .Host.Seats.Find(hostOwner.TargetSeat).HeroId;
            Assert.That(hostOwner.TrySubmitCommand(placement, out _), Is.True);
            var authority = Field<PveCoopMatch>(servingLobby, "match");
            yield return Until(() => authority.Session.Projection.Heroes.Count > 0,
                "Authoritative PvE state progresses beyond its initial composition");
            var progressedVersion = authority.Session.Projection.Version;

            remoteTransport.Dispose();
            remote.OnDisconnected();
            remoteTransport = null;
            remote = null;
            yield return Until(() => server.IsPaused, "Host reserves the abruptly disconnected player's seat");

            manuallyPumpedListener = Field<TcpSessionListener>(servingLobby, "listener");
            manuallyPumpedServer = server;
            var dialAddress = Field<PrivateSessionEndpoint>(servingLobby, "endpoint").ToString();
            servingLobby.enabled = false;
            lobby = PveSharedLobby.Open(new PrivateLobbyEntry(dialAddress, port.ToString(), key,
                default, "Returning player", modeId: PrivateLobbyMode.PveId), false, seed);
            Assert.That(Field<string>(lobby, "feedback"), Is.Empty,
                "The fresh PvE adapter must dial the still-running host before it can reclaim.");
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null,
                "Fresh PvE client reclaims its reserved seat and replaces the demo composition",
                () =>
                {
                    var client = Field<PrivateSessionClient>(lobby, "active");
                    return "state=" + client.State + ", seat=" + client.TargetSeat.Value +
                           ", started=" + client.HasMatchStarted + ", projection=" +
                           (client.LatestProjection == null ? "none" : client.LatestProjection.Version.ToString()) +
                           ", loading=" + Field<bool>(lobby, "loading") +
                           ", next-composition=" + (MatchSceneCompositionRoot.NextComposedExternalMatch != null);
                });

            var returned = Field<PrivateSessionClient>(lobby, "active");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            Assert.That(returned.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(returned.TargetSeat.Value, Is.EqualTo("blue-player-03"));
            Assert.That(runtime.Seat.HeroId.Value, Is.EqualTo(returningHero));
            Assert.That(runtime.InitialState.Setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Blue), Is.EqualTo(3),
                "The returned runtime uses the chosen PvE lineup, not the default demo lineup.");
            Assert.That(runtime.ReadModel.Version, Is.GreaterThanOrEqualTo(progressedVersion));
            Assert.That(runtime.ReadModel.Heroes.Any(hero => hero.HeroId.Equals(placedHero)), Is.True,
                "The returned owner projection contains authoritative progress made before exit.");
        }
        private static void Click(VisualElement button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.enabledInHierarchy, Is.True);
            var point = button.worldBound.center;
            var picked = button.panel.Pick(point);
            Assert.That(picked == button || button.Contains(picked), Is.True, "Pointer reachable: " + button.name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = point, button = 0, clickCount = 1 }))
            { down.target = picked; picked.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = point, button = 0, clickCount = 1 }))
            { up.target = picked; picked.SendEvent(up); }
        }

        [UnityTest]
        public IEnumerator UnseatedHostSpectatesRemotePveAndRefreshesAfterPlacement()
        {
            const string key = "host-spectator-test";
            const int port = 45793;
            lobby = PveSharedLobby.Open(new PrivateLobbyEntry("", port.ToString(), key, default,
                "Spectating host", modeId: PrivateLobbyMode.PveId), true, 73);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active")?.State ==
                PrivateSessionClientState.Admitted, "Host remains admitted without claiming a seat");

            var server = Field<PrivateSessionHost>(lobby, "server");
            Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", port, out var endpoint, out var problem),
                Is.True, problem);
            foreach (var seat in new[] { "blue-player-01", "blue-player-02", "blue-player-03" })
            {
                var peer = new RemotePeer(server.Identity, new SeatId(seat), key);
                peers.Add(peer);
                Assert.That(peer.Transport.Connect(endpoint, 3, out problem), Is.True, problem);
            }
            yield return Until(() => peers.All(peer => peer.Client.GrantedSeat != null),
                "Three remote owners claim every Blue seat");
            foreach (var peer in peers) peer.Client.TrySetReadiness(true);

            var heroes = new[] { "hero-arien", "hero-brogan", "hero-wasp" };
            for (var index = 0; index < peers.Count; index++)
            {
                var peer = peers[index];
                yield return Until(() => PrivateLobbyModel.ForClient(peer.Client).Selection.IsOwnPick,
                    "Remote owner receives hero pick " + (index + 1));
                Assert.That(peer.Client.TryChooseHero(heroes[index]), Is.True);
            }

            yield return Until(() => server.HasMatchStarted &&
                Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null,
                "Unseated host enters the started PvE match");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            var authority = Field<PveCoopMatch>(lobby, "match");
            Assert.That(runtime.IsSeatlessHostView, Is.True);
            Assert.That(runtime.Seat, Is.Null);
            Assert.That(Field<PrivateSessionClient>(lobby, "active").State,
                Is.EqualTo(PrivateSessionClientState.Admitted));
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(authority.Session.Projection.Version));

            for (var placed = 0; placed < peers.Count; placed++)
            {
                yield return Until(() => peers.Any(peer => peer.Client.LatestProjection?.AuthorizedCommands.Any(command =>
                    command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep) == true),
                    "Host-paced setup offers remote placement " + (placed + 1));
                var owner = peers.First(peer => peer.Client.LatestProjection.AuthorizedCommands.Any(command =>
                    command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep));
                var placement = owner.Client.LatestProjection.AuthorizedCommands.First(command =>
                    command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep);
                var placedHero = runtime.Host.Seats.Find(owner.Client.TargetSeat).HeroId;
                var before = authority.Session.Projection.Version;
                Assert.That(owner.Client.TrySubmitCommand(placement, out _), Is.True);
                yield return Until(() => authority.Session.Projection.Version > before &&
                    runtime.ReadModel.Version == authority.Session.Projection.Version &&
                    runtime.ReadModel.Heroes.Any(hero => hero.HeroId.Equals(placedHero)) &&
                    peers.All(peer => !peer.Client.IsWaitingForHost &&
                        peer.Client.LatestProjection?.Version == authority.Session.Projection.Version),
                    "Host spectator refreshes after remote placement " + (placed + 1));
            }
            Assert.That(runtime.ReadModel.Heroes.Count(hero =>
                runtime.ReadModel.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue), Is.EqualTo(3));
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot("tmp/w060-pve-host-spectator.png");
            }
        }

        [UnityTest]
        public IEnumerator SharedMapSelectionHidesExperimentsAndOnePlayerClaimsThreeHeroes()
        {
            PveSharedLobby.Register();
            entryObject = new GameObject("Shared map selection test");
            var entryDocument = entryObject.AddComponent<UIDocument>();
            entrySettings = Object.Instantiate(Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath));
            entryDocument.panelSettings = entrySettings;
            var entry = new PrivateLobbyUnityView();
            entryDocument.rootVisualElement.Add(entry.Root);
            entry.Render(PrivateLobbyModel.Entry(Array.Empty<SeatRosterEntry>(), "", "3v3"));
            entry.ShowPage(PrivateLobbyEntryPage.Host);
            yield return null;
            entry.Root.Q<DropdownField>("host-map-selector").value = PrivateLobbyMode.PveLabel;
            Assert.That(entry.CurrentEntry().ModeId, Is.EqualTo(PrivateLobbyMode.PveId));
            Assert.That(entry.CurrentEntry().Experiments.TeammateCoordination, Is.True);
            Assert.That(entry.CurrentEntry().Experiments.TurnTimer, Is.False);
            Assert.That(entry.Root.Q<Button>("experimental-button").style.display.value, Is.EqualTo(DisplayStyle.None));
            Object.Destroy(entryObject);
            yield return null;

            lobby = PveSharedLobby.Open(new PrivateLobbyEntry("", "45791", "shared-test", default,
                "Solo player", modeId: PrivateLobbyMode.PveId), true, 42);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active")?.State == PrivateSessionClientState.Admitted,
                "Host admitted before choosing seats");
            var view = Field<PrivateLobbyUnityView>(lobby, "view");
            for (var count = 1; count <= 3; count++)
            {
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.SeatPickName).ToList().First());
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r =>
                    r.Occupancy == SeatOccupancyState.Claimed) == expected, "Claim " + count);
            }
            Assert.That(Field<PrivateLobbyModel>(lobby, "model").Seats.Count(r => r.IsOwnSeat), Is.EqualTo(3));
            Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "READY"));
            for (var count = 1; count <= 3; count++)
            {
                yield return Until(() => Field<PrivateLobbyModel>(lobby, "model").Selection.IsOwnPick, "Next owned pick");
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList().First(b => b.enabledSelf));
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "PICK HERO"));
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r => r.HasChosenHero) == expected,
                    "Hero choice " + count);
            }
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null, "Shared lobby enters match");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            Assert.That(runtime.IsSeatlessHostView, Is.False);
            Assert.That(runtime.Seat, Is.Not.Null, "An ordinary playing host is never raced into spectator scope.");
            Assert.That(runtime.InitialState.Experiments.TeammateCoordination, Is.True);
            var root = lobby.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Query<Button>().ToList().Any(b => b.name.StartsWith("pve-control-")), Is.False);
            for (var placed = 0; placed < 3; placed++)
            {
                yield return Until(() => runtime.ReadModel.PendingSystemTransitionStep != null &&
                    runtime.ReadModel.DecisionContract?.Commands.Count > 0, "Owned placement is automatically selected");
                var pending = runtime.ReadModel.PendingSystemTransitionStep;
                var item = pending.OpenItems.Single();
                Assert.That(ModeRoundStartPlacementRules.TryReadItem(item.ItemId, out var hero), Is.True);
                Assert.That(runtime.Seat.HeroId, Is.EqualTo(hero));
                Assert.That(ModeRoundStartPlacementRules.TryReadOption(item.Options[0], out var space), Is.True);
                var tile = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Single(t => t.Coordinate.Equals(space));
                var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                Assert.That(pointer.TryHandlePointerRay(Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(tile.transform.position))), Is.True);
                yield return Until(() => runtime.ReadModel.Heroes.Any(h => h.HeroId.Equals(hero)), "Placed hero replicated");
            }
            yield return Until(() => runtime.ReadModel.PendingSystemTransitionStep == null, "Placement completes");
            var heroes = runtime.InitialState.Setup.Heroes.Where(h => h.TeamSide == TeamSide.Blue).Select(h => h.HeroId).ToArray();
            foreach (var hero in heroes.Reverse())
            {
                var select = runtime.LocalHeroSelection;
                var invoked = false;
                var accepted = false;
                runtime.LocalHeroSelection = selected => { invoked = true; return accepted = select(selected); };
                var rail = runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                    .Single(r => r.userData is HeroId id && id.Equals(hero));
                Click(rail.Q("gd-hero-medallion"));
                runtime.LocalHeroSelection = select;
                Assert.That(invoked, Is.True, "Portrait routes through local selection");
                Assert.That(accepted, Is.True, "Owned selection accepted: " + runtime.ReadModel.Phase + "/" + runtime.ReadModel.DecisionContract?.FamilyId);
                Assert.That(runtime.Seat.HeroId, Is.EqualTo(hero));
                yield return null; yield return null;
                Assert.That(runtime.Seat.HeroId, Is.EqualTo(hero), "Manual actionable selection stays stable");
            }
            for (var committed = 0; committed < 2; committed++)
            {
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").LatestProjection.AuthorizedCommands.Any(c =>
                    c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard) == true, "Current owner may commit");
                var owner = Field<PrivateSessionClient>(lobby, "active");
                var previous = runtime.Seat.HeroId;
                var command = owner.LatestProjection.AuthorizedCommands.First(c =>
                    c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard);
                Assert.That(owner.TrySubmitCommand(command, out _), Is.True);
                yield return Until(() => !runtime.Seat.HeroId.Equals(previous), "Commit automatically selects another unfinished owned hero");
                if (committed == 0)
                {
                    var committedRail = runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                        .Single(r => r.userData is HeroId id && id.Equals(previous));
                    Click(committedRail.Q("gd-hero-medallion"));
                    yield return null; yield return null;
                    Assert.That(runtime.Seat.HeroId, Is.EqualTo(previous),
                        "A manually selected committed seat stays selected while its Undo is available.");
                    Assert.That(runtime.RoundView.Model.SeatUndoAvailable, Is.True);
                    var undo = (Button)runtime.RoundView.SeatUndoRoot.Q("gd-seat-undo-control");
                    Assert.That(undo, Is.Not.Null);
                    Assert.That(runtime.RoundView.SeatUndoRoot.style.display.value,
                        Is.EqualTo(DisplayStyle.Flex));
                    Click(undo);
                    yield return Until(() => runtime.ReadModel.GetCommitment(previous).Status ==
                        GuardsDigital.Core.Cards.TurnCardCommitmentStatus.AwaitingResponse,
                        "Visible PvE Undo returns the committed card");
                    committedRail = runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                        .Single(r => r.userData is HeroId id && id.Equals(previous));
                    Click(committedRail.Q("gd-hero-medallion"));
                    yield return Until(() => runtime.Seat.HeroId.Equals(previous),
                        "The withdrawn commitment can be selected again");
                    yield return Until(() => Field<PrivateSessionClient>(lobby, "active").LatestProjection
                        .AuthorizedCommands.Any(c => c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard),
                        "The withdrawn owner may recommit");
                    var recommit = Field<PrivateSessionClient>(lobby, "active");
                    Assert.That(recommit.TrySubmitCommand(recommit.LatestProjection.AuthorizedCommands.First(c =>
                        c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard), out _), Is.True);
                    yield return Until(() => !runtime.Seat.HeroId.Equals(previous),
                        "Recommit resumes automatic unfinished-seat selection");
                }
            }
            // Map tokens select the same owned seat without replaying or submitting a rule command.
            var firstHero = heroes[0];
            var token = runtime.BoardView.HeroViews.Single(h => h.Occupancy.HeroId.Equals(firstHero));
            var tokenPointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(tokenPointer.TryHandlePointerRay(Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(token.transform.position))), Is.True);
            Assert.That(runtime.Seat.HeroId, Is.EqualTo(firstHero));
            yield return null; yield return null;
            Assert.That(runtime.Seat.HeroId, Is.EqualTo(firstHero),
                "An explicitly selected committed hero remains available for Undo.");
            var unfinishedHero = runtime.ReadModel.Commitments.Single(c =>
                c.Status == GuardsDigital.Core.Cards.TurnCardCommitmentStatus.AwaitingResponse).HeroId;
            var unfinishedRail = runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                .Single(r => r.userData is HeroId id && id.Equals(unfinishedHero));
            Click(unfinishedRail.Q("gd-hero-medallion"));
            yield return Until(() => runtime.Seat.HeroId.Equals(unfinishedHero),
                "The remaining commitment owner can still be selected directly");
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active").LatestProjection.AuthorizedCommands.Any(c =>
                c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard), "Remaining commitment owner");
            var lastOwner = Field<PrivateSessionClient>(lobby, "active");
            Assert.That(lastOwner.TrySubmitCommand(lastOwner.LatestProjection.AuthorizedCommands.First(c =>
                c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.CommitCard), out _), Is.True);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active").LatestProjection.AuthorizedCommands.Any(c =>
                c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.StartAction), "An owned action turn is automatically selected");
            Assert.That(runtime.Seat.HeroId, Is.EqualTo(runtime.ReadModel.SelectedActor.ActorId));
            var authority = Field<PveCoopMatch>(lobby, "match");
            var server = Field<PrivateSessionHost>(lobby, "server");
            var enemyActed = false;
            for (var step = 0; step < 200 && !enemyActed; step++)
            {
                var next = authority.Session.Peek();
                enemyActed = next.Command?.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.StartAction;
                var result = next.Kind == PveStepKind.Command ? authority.Session.Step(out _) :
                    authority.Session.Execute(PvePassivePlayer.NextAnswer(authority.Session));
                Assert.That(result.Accepted, Is.True, result.Detail);
                server.RepublishProjections();
                yield return null;
            }
            Assert.That(enemyActed, Is.True, "Saved output includes an automatic enemy action");
            var narrationPath = Field<string>(lobby, "narrationPath");
            Assert.That(System.IO.File.Exists(narrationPath), Is.True);
            var saved = ReadLive(narrationPath);
            Assert.That(saved, Does.Contain(GameplayNarrationLog.FormatVersion));
            foreach (var row in authority.Session.Projection.MatchLog)
                Assert.That(saved, Does.Contain("#" + row.Sequence + " " + row.Kind));
            Assert.That(saved, Does.Not.Contain("shared-test"), "Never save the session key");
            server.RepublishProjections();
            Assert.That(ReadLive(narrationPath), Is.EqualTo(saved), "Duplicate publishes do not duplicate narration");
            var capture = Field<PrivateOnlineEvidenceFile>(lobby, "hostEvidence");
            Assert.That(ReadLive(capture.Path), Does.Contain("role=host"));
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot("tmp/w047-solo-shared-lobby.png");
            }
            Object.Destroy(lobby.gameObject);
            yield return null;
            using (System.IO.File.Open(narrationPath, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) { }
            Assert.That(System.IO.File.ReadAllText(capture.Path), Does.Contain("GD_EVIDENCE_ABORT"), "Unfinished session closes its capture");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (lobby != null) Object.Destroy(lobby.gameObject);
            if (servingLobby != null) Object.Destroy(servingLobby.gameObject);
            if (entryObject != null) Object.Destroy(entryObject);
            if (entrySettings != null) Object.Destroy(entrySettings);
            remote?.SendGoodbye();
            remoteTransport?.Dispose();
            remote = null; remoteTransport = null;
            foreach (var peer in peers) peer.Dispose();
            peers.Clear();
            servingLobby = null;
            manuallyPumpedListener = null;
            manuallyPumpedServer = null;
            typeof(PveSharedLobby).GetField("retainedCapabilityStoreFactory",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);
            foreach (var overlay in Object.FindObjectsByType<PveEncounterView>(FindObjectsSortMode.None)) Object.Destroy(overlay.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoPlayersSplitThreeSeatsAndReconnectWithoutSharingControl()
        {
            lobby = PveSharedLobby.Open(new PrivateLobbyEntry("", "45792", "two-player-test", default, "Host"), true, 71);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active")?.State == PrivateSessionClientState.Admitted, "Host admitted");
            var view = Field<PrivateLobbyUnityView>(lobby, "view");
            for (var count = 1; count <= 2; count++)
            {
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.SeatPickName).ToList().First());
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r => r.Occupancy == SeatOccupancyState.Claimed) == expected,
                    "Host claim " + count);
            }
            var server = Field<PrivateSessionHost>(lobby, "server");
            remoteTransport = new TcpSessionClientTransport();
            var remoteOutbox = new RemoteOutbox { Transport = remoteTransport };
            remote = new PrivateSessionClient(server.Identity, new SeatId("blue-player-03"), "two-player-test", remoteOutbox, new CryptoNonceSource());
            Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", 45792, out var endpoint, out _), Is.True);
            Assert.That(remoteTransport.Connect(endpoint, 3, out var problem), Is.True, problem);
            yield return Until(() => remote.GrantedSeat != null, "Other player claims third seat");
            Assert.That(Field<PrivateLobbyModel>(lobby, "model").Seats.Count(s => s.IsOwnSeat), Is.EqualTo(2));
            remote.TrySetReadiness(true);
            Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "READY"));
            for (var count = 1; count <= 2; count++)
            {
                yield return Until(() => Field<PrivateLobbyModel>(lobby, "model").Selection.IsOwnPick, "Host pick");
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList().First(b => b.enabledSelf));
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "PICK HERO"));
                var expected = count;
                yield return Until(() => remote.Roster.Count(r => r.HasChosenHero) == expected, "Remote receives host pick");
            }
            Assert.That(remote.TryChooseHero("hero-brogan"), Is.True);
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null && remote.HasMatchStarted, "Two-player match starts");
            Assert.That(lobby.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList()
                .Count(b => b.name.StartsWith("pve-control-")), Is.Zero);
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            var foreignHero = runtime.Host.Seats.Find(remote.TargetSeat).HeroId;
            var selectedSeat = runtime.Seat.Id;
            yield return null; yield return null;
            var foreignRail = runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                .Single(r => r.userData is HeroId id && id.Equals(foreignHero));
            Click(foreignRail.Q("gd-hero-medallion"));
            Assert.That(runtime.Seat.Id, Is.EqualTo(selectedSeat), "Inspection never transfers the other player's authority");
            Assert.That(runtime.HeroOverviewsView.CardInspection.Hero, Is.EqualTo(foreignHero));
            runtime.HeroOverviewsView.CardInspection.Close();
            remoteTransport.Dispose(); remote.OnDisconnected();
            yield return Until(() => server.IsPaused, "Disconnect pauses authority");
            Assert.That(remote.BeginReconnect(), Is.True);
            remoteTransport = new TcpSessionClientTransport();
            remoteOutbox.Transport = remoteTransport;
            Assert.That(remoteTransport.Connect(endpoint, 3, out problem), Is.True, problem);
            yield return Until(() => remote.State == PrivateSessionClientState.Seated && !server.IsPaused, "Same third seat reconnects");
            Assert.That(remote.TargetSeat.Value, Is.EqualTo("blue-player-03"));
        }
    }
}
