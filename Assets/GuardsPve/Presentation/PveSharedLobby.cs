using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Play;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>PvE adapter for the shared entry, seating and hero-selection views.</summary>
    public sealed partial class PveSharedLobby : MonoBehaviour
    {
        private sealed class Connection : IPrivateSessionClientOutbox, IDisposable
        {
            public TcpSessionClientTransport Transport;
            public PrivateSessionClient Client;
            public PrivateOnlineEvidenceFile Evidence;
            public bool EvidenceUnavailable;
            public void Send(byte[] frame) => Transport?.Send(frame);
            public void Close() => Transport?.Close();
            public void Dispose() { Client?.SendGoodbye(); Transport?.Dispose(); CloseCapture(Evidence); }
        }

        private readonly List<Connection> connections = new List<Connection>();
        private PrivateLobbyEntry entry;
        private PrivateSessionEndpoint endpoint;
        private PrivateSessionIdentity identity;
        private PrivateSessionHost server;
        private TcpSessionListener listener;
        private PveEnemyCatalog catalog;
        private PveEncounterDefinition layout;
        private GuardsDigital.Core.Board.BoardMapDefinition map;
        private PveCoopMatch match;
        private ulong seed;
        private UIDocument document;
        private PrivateLobbyUnityView view;
        private PrivateLobbyModel model;
        private PrivateSessionClient active;
        private ConfiguredMatchSceneRuntime runtime;
        private string feedback = "", rendered = "";
        private long projectionRevision = -1;
        private SeatId manuallySelectedUndoSeat;
        private long manuallySelectedUndoVersion = -1;
        private bool loading;
        private float nextStep;

        // A real rejoin creates a fresh client after the previous process has gone away. Keep the
        // host-issued capability in the same per-user store as the shared PvP lobby so that fresh
        // client can authenticate the reserved-seat reclaim instead of remaining unseated over the
        // scene's provisional demo composition. The optional factory is a process-boundary test seam;
        // production always uses Unity's persistent data directory.
        private static Func<IRetainedSeatCapabilityStore> retainedCapabilityStoreFactory;

        private static IRetainedSeatCapabilityStore RetainedCapabilities() =>
            retainedCapabilityStoreFactory?.Invoke() ??
            new FileRetainedSeatCapabilityStore(UnityEngine.Application.persistentDataPath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            PrivateLobbyMode.OpenPve = (entry, hosting, seed) => Open(entry, hosting, seed);
            PrivateLobbyMode.ReadPveSeed = rejection =>
                PveCoopMatch.TryReadHostSeed(rejection, 0, out var seed, includeMapMismatch: true) ? seed : (ulong?)null;
        }

        public static PveSharedLobby Open(PrivateLobbyEntry entry, bool hosting, ulong seed)
        {
            var lobby = new GameObject("PvE Shared Lobby").AddComponent<PveSharedLobby>();
            DontDestroyOnLoad(lobby.gameObject);
            lobby.entry = entry;
            lobby.seed = seed == 0 ? SetupRandomnessAuthority.DrawSeed().Value : seed;
            lobby.BuildView();
            try { lobby.Begin(hosting); }
            catch (Exception exception) { lobby.feedback = exception.Message; lobby.ShowFailure(); }
            return lobby;
        }

        private void BuildView()
        {
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = Instantiate(Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath));
            document.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ConfiguredMatchHudRenderer.RuntimeThemeResourcePath);
            document.panelSettings.sortingOrder = 500;
            MatchHudPanelScaling.Apply(document.panelSettings);
            view = new PrivateLobbyUnityView();
            document.rootVisualElement.Add(view.Root);
            view.ControlPressed += Press;
        }

        private PveComposedEncounter Compose(IEnumerable<string> heroes) =>
            PveEncounterComposer.Compose(new PveEncounterSetup(heroes, seed), layout, catalog, map);

        private void Begin(bool hosting)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out catalog, out layout);
            map = OfficialBoardMapCatalog.Load().Load(layout.MapReference);
            var provisional = Compose(new[] { "hero-arien", "hero-brogan", "hero-wasp" });
            identity = PveCoopMatch.Identity(PrivateSessionIdentity.ForComposition(map,
                provisional.InitialState.InitialConfiguration, provisional.InitialState.Setup, BuildIdentityStamp.Current),
                seed, PveContentLoader.SaveCatalog(catalog), File.ReadAllText(Path.Combine(PveDataPaths.Directory, "encounter.json")));
            string problem;
            if (hosting)
            {
                if (!int.TryParse(entry.Port, out var port) ||
                    !PrivateSessionEndpoint.TryCreateAutomaticHostBind(port, entry.SessionKey.Length > 0, out endpoint, out problem))
                    throw new ArgumentException("Enter a valid host port.");
                match = new PveCoopMatch(provisional);
                listener = new TcpSessionListener();
                listener.Start(endpoint);
                server = new PrivateSessionHost(match.Host, identity, entry.SessionKey, listener, new CryptoNonceSource());
                server.SeatComposition = selection =>
                {
                    match = new PveCoopMatch(Compose(selection.PickOrder.Select(seat =>
                        selection.TryGetChoice(seat.Seat, out var hero) ? hero.HeroIdValue : throw new InvalidOperationException("Incomplete hero selection."))));
                    return match.Host;
                };
                if (!PrivateSessionEndpoint.TryCreate(endpoint.ResolveLocalDialAddress(), listener.BoundPort, out endpoint, out problem))
                    throw new ArgumentException(problem);
                view.ShowInvitation(PrivateLobbyInvitation.For(entry.SessionKey, listener.BoundPort,
                    HostAddressDiscovery.NotStarted, null, null));
            }
            else if (!PrivateSessionEndpoint.TryParseAddress(entry.Address, out endpoint, out problem))
                throw new ArgumentException(problem);
            view.SetMapName(PrivateLobbyMode.PveLabel);
            Connect(default);
        }

        private void Connect(SeatId seat)
        {
            var connection = new Connection { Transport = new TcpSessionClientTransport() };
            connection.Client = new PrivateSessionClient(identity, seat, entry.SessionKey, connection,
                new CryptoNonceSource(), retainedCapabilities: RetainedCapabilities())
                { PlayerName = entry.PlayerName };
            connections.Add(connection);
            active = connection.Client;
            if (!connection.Transport.Connect(endpoint, 5, out var problem))
            {
                connections.Remove(connection);
                connection.Dispose();
                active = connections.LastOrDefault()?.Client;
                throw new InvalidOperationException(problem);
            }
        }

        private void Update()
        {
            var now = DateTime.UtcNow.Ticks;
            if (listener != null)
            {
                foreach (var e in listener.Drain())
                {
                    if (e.Kind == TransportEventKind.Connected) server.OnConnected(e.Ordinal, now);
                    else if (e.Kind == TransportEventKind.Frame) server.OnFrame(e.Ordinal, e.Frame);
                    else if (e.Kind == TransportEventKind.Disconnected) server.OnDisconnected(e.Ordinal);
                    else if (e.Kind == TransportEventKind.StreamRejected) server.OnStreamRejected(e.Ordinal, e.Reason);
                }
                server.Sweep(now);
                foreach (var e in server.DrainEvents()) PrivateOnlineMarkers.LogHostEvent(e, server);
            }
            foreach (var connection in connections)
                foreach (var e in connection.Transport.Drain())
                {
                    if (e.Kind == TransportEventKind.Connected) connection.Client.OnConnected();
                    else if (e.Kind == TransportEventKind.Frame) connection.Client.OnFrame(e.Frame);
                    else if (e.Kind == TransportEventKind.Disconnected) connection.Client.OnDisconnected();
                    else if (e.Kind == TransportEventKind.StreamRejected) connection.Client.OnStreamRejected(e.Reason);
                }
            if (active == null) return;
            foreach (var rejected in connections.Where(c => c.Client.GrantedSeat == null &&
                c.Client.State == PrivateSessionClientState.Rejected).ToArray())
            {
                if (connections.Count == 1) break;
                feedback = rejected.Client.RejectionDetail;
                connections.Remove(rejected);
                rejected.Dispose();
                if (active == rejected.Client) active = connections[0].Client;
            }
            if (!loading)
            {
                active = connections.Select(c => c.Client).FirstOrDefault(c =>
                    PrivateLobbyModel.ForClient(c).Selection.IsOwnPick) ?? active;
                model = PrivateLobbyModel.ForLocalPlayers(connections.Select(c => c.Client).ToArray(), active, server, feedback);
                var stamp = model.StatusLabel + model.FeedbackLabel + model.Selection.StatusLabel +
                    string.Join("|", active.Roster.Select(r => r.Seat + ":" + r.Occupancy + ":" + r.IsReady + ":" + r.Hero + ":" + r.HasChosenHero)) +
                    string.Join("|", connections.Select(c => c.Client.State + ":" + c.Client.TargetSeat));
                if (stamp != rendered) { rendered = stamp; view.Render(model); }
                var localOwnerReady = connections.Any(c =>
                    c.Client.HasMatchStarted && c.Client.LatestProjection != null);
                var seatlessHostReady = server?.HasMatchStarted == true &&
                    connections.All(c => c.Client.GrantedSeat == null);
                if (seatlessHostReady || localOwnerReady) LoadMatch();
            }
            if (runtime == null) return;
            if (server != null && !server.IsPaused && Time.unscaledTime >= nextStep)
            {
                nextStep = Time.unscaledTime + .45f;
                if (match.Session.Step(out _)?.Accepted == true) server.RepublishProjections();
            }
            SelectAwaitedOwner();
            var nextRevision = runtime.IsSeatlessHostView
                ? match.Session.Projection.Version
                : active.ProjectionRevision;
            if (projectionRevision != nextRevision)
            {
                projectionRevision = nextRevision;
                runtime.RefreshAll();
            }
            CaptureSavedLogs();
        }

        private void Press(PrivateLobbyControl control, PrivateLobbyEntry pressed)
        {
            if (control == PrivateLobbyControl.Leave) { Leave(); return; }
            if (model == null || !model.Offers(control)) return;
            try
            {
                switch (control)
                {
                    case PrivateLobbyControl.ClaimSeat:
                        if (!active.Roster.Any(r => r.Seat.Equals(pressed.Seat) && r.Occupancy == SeatOccupancyState.Open)) return;
                        var unseated = connections.FirstOrDefault(c => c.Client.State == PrivateSessionClientState.Admitted);
                        if (unseated != null) unseated.Client.TryClaimSeat(pressed.Seat);
                        else Connect(pressed.Seat);
                        break;
                    case PrivateLobbyControl.Ready:
                    case PrivateLobbyControl.Unready:
                        foreach (var c in connections) if (c.Client.GrantedSeat != null)
                            c.Client.TrySetReadiness(control == PrivateLobbyControl.Ready);
                        break;
                    case PrivateLobbyControl.ChooseHero: active.TryChooseHero(view.HighlightedHero); break;
                    case PrivateLobbyControl.HostUndo: server?.TryUndoLobbyStep(); break;
                    case PrivateLobbyControl.Reconnect: Reconnect(); break;
                }
            }
            catch (Exception exception) { feedback = exception.Message; }
        }

        private void LoadMatch()
        {
            loading = true;
            var localOwner = connections.Select(c => c.Client)
                .FirstOrDefault(c => c.HasMatchStarted && c.LatestProjection != null);
            var seatlessHost = localOwner == null && server?.HasMatchStarted == true &&
                connections.All(c => c.Client.GrantedSeat == null);
            if (!seatlessHost) active = localOwner ?? throw new InvalidOperationException("No local match view is available.");
            var roster = server != null ? server.Roster : active.Roster;
            var encounter = Compose(roster.OrderBy(r => r.Seat.Value, StringComparer.Ordinal).Select(r => r.Hero.Value));
            MatchSceneCompositionRoot.NextComposedExternalMatch = new ExternalMatchComposition(encounter.InitialState, composed =>
            {
                runtime = composed;
                if (seatlessHost)
                    runtime.AttachSeatlessHostView(server.RepublishProjections);
                else
                {
                    runtime.AttachOnlineClient(active);
                    runtime.LocalHeroSelection = SelectOwnedHero;
                }
                AttachSavedNarration();
                PveEncounterView.AttachCoop(runtime, encounter, server == null ? null : match.Session,
                    command =>
                    {
                        if (server.IsPaused) return false;
                        var accepted = match.Session.Execute(command).Accepted;
                        server.RepublishProjections();
                        return accepted;
                    });
                view.Root.style.display = DisplayStyle.None;
            }, sessionDecorator: seatlessHost
                ? new Func<IMatchHost, IMatchSession, IMatchSession>((_, _) => match.Session)
                : null,
                directorPlayerSide: TeamSide.Blue);
            SceneManager.LoadScene(PveLauncher.MatchSceneName);
        }

        private void Switch(PrivateSessionClient owner)
        {
            if (ReferenceEquals(active, owner)) return;
            if (owner.State != PrivateSessionClientState.Seated || !owner.HasMatchStarted || owner.LatestProjection == null) return;
            active = owner;
            runtime.AttachOnlineClient(active);
            projectionRevision = -1;
        }

        private bool SelectOwnedHero(HeroId hero)
        {
            var projection = runtime.ReadModel;
            // A tray/token never steals an unfinished board or card choice from its current owner.
            if (projection.PendingSystemTransitionStep != null ||
                (projection.DecisionContract != null && projection.Phase != RoundResolutionPhase.AwaitingCommitments)) return false;
            // The admission grant predates hero pick; resolve the final composed seat identity.
            var owner = connections.Select(c => c.Client).FirstOrDefault(c =>
                runtime.Host.Seats.Find(c.TargetSeat)?.HeroId.Equals(hero) == true);
            if (owner == null || owner.State != PrivateSessionClientState.Seated || owner.LatestProjection == null) return false;
            Switch(owner);
            if (owner.LatestProjection.IsSeatUndoAvailable)
            {
                manuallySelectedUndoSeat = owner.TargetSeat;
                manuallySelectedUndoVersion = owner.LatestProjection.Version;
            }
            return true;
        }

        private void SelectAwaitedOwner()
        {
            if (active?.IsWaitingForHost == true) return;
            if (manuallySelectedUndoSeat.IsDefined && active.TargetSeat.Equals(manuallySelectedUndoSeat) &&
                active.LatestProjection?.Version == manuallySelectedUndoVersion &&
                active.LatestProjection.IsSeatUndoAvailable)
            {
                return;
            }
            manuallySelectedUndoSeat = default;
            manuallySelectedUndoVersion = -1;
            if (NeedsInput(active)) return; // Includes a manually selected unfinished commitment.
            var next = connections.Select(c => c.Client).OrderBy(c => c.TargetSeat.Value, StringComparer.Ordinal)
                .FirstOrDefault(NeedsInput);
            if (next != null) Switch(next);
        }

        private bool NeedsInput(PrivateSessionClient owner)
        {
            var projection = owner?.LatestProjection;
            if (owner?.State != PrivateSessionClientState.Seated || projection == null || owner.IsWaitingForHost ||
                projection.AuthorizedCommands.Count == 0) return false;
            if (projection.Phase == (int)RoundResolutionPhase.AwaitingCommitments &&
                !projection.AuthorizedCommands.Any(c => c.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerSystemTransitionStep))
                return projection.Commitments.Any(c => c.HeroId == runtime.Host.Seats.Find(owner.TargetSeat).HeroId.Value &&
                    c.Status == (int)TurnCardCommitmentStatus.AwaitingResponse);
            return true;
        }

        private void Reconnect()
        {
            foreach (var c in connections)
            {
                if (!c.Client.BeginReconnect()) continue;
                c.Transport.Dispose();
                c.Transport = new TcpSessionClientTransport();
                if (!c.Transport.Connect(endpoint, 5, out var problem))
                { feedback = problem; c.Client.OnReconnectTransportFailed(); }
            }
        }

        private void ShowFailure()
        {
            view.Root.style.display = DisplayStyle.None;
            var failure = new VisualElement { name = "pve-connection-failure" };
            failure.Add(new Label(feedback));
            failure.Add(new Button(Leave) { text = "Back" });
            document.rootVisualElement.Add(failure);
        }

        private void Leave()
        {
            Destroy(gameObject);
            SceneManager.LoadScene(PveLauncher.MatchSceneName);
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.LocalHeroSelection = null;
            foreach (var c in connections) c.Dispose();
            server?.Shutdown();
            CloseSavedLogs();
            listener?.Dispose();
            if (document?.panelSettings != null) Destroy(document.panelSettings);
        }
    }
}
