using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
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
            public void Send(byte[] frame) => Transport?.Send(frame);
            public void Close() => Transport?.Close();
            public void Dispose() { Client?.SendGoodbye(); Transport?.Dispose(); }
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
        private VisualElement seatControls;
        private Label status;
        private string feedback = "", rendered = "";
        private long projectionRevision = -1;
        private bool loading;
        private float nextStep;

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
            seatControls = new VisualElement { name = "pve-owned-seat-controls" };
            seatControls.style.position = Position.Absolute;
            seatControls.style.right = 12;
            seatControls.style.bottom = 12;
            seatControls.style.backgroundColor = new Color(.04f, .06f, .09f, .95f);
            document.rootVisualElement.Add(seatControls);
            status = new Label();
            status.style.color = Color.white;
            status.style.whiteSpace = WhiteSpace.Normal;
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
            connection.Client = new PrivateSessionClient(identity, seat, entry.SessionKey, connection, new CryptoNonceSource())
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
                foreach (var e in server.DrainEvents()) Debug.Log("GUARDS_PVE_LOBBY: " + e.Kind);
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
                if (connections.Any(c => c.Client.HasMatchStarted && c.Client.LatestProjection != null)) LoadMatch();
            }
            if (runtime == null) return;
            if (server != null && !server.IsPaused && Time.unscaledTime >= nextStep)
            {
                nextStep = Time.unscaledTime + .45f;
                if (match.Session.Step(out _)?.Accepted == true) server.RepublishProjections();
            }
            if (projectionRevision != active.ProjectionRevision)
            {
                projectionRevision = active.ProjectionRevision;
                runtime.RefreshAll();
            }
            status.text = server?.IsPaused == true ? "Paused — waiting for reconnect" :
                active.State + " — " + HeroLabel(active);
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
            active = connections.Select(c => c.Client).First(c => c.HasMatchStarted && c.LatestProjection != null);
            var encounter = Compose(active.Roster.OrderBy(r => r.Seat.Value, StringComparer.Ordinal).Select(r => r.Hero.Value));
            MatchSceneCompositionRoot.NextComposedExternalMatch = new ExternalMatchComposition(encounter.InitialState, composed =>
            {
                runtime = composed;
                runtime.AttachOnlineClient(active);
                PveEncounterView.AttachCoop(runtime, encounter, server == null ? null : match.Session,
                    command => { if (server.IsPaused) return; match.Session.Execute(command); server.RepublishProjections(); },
                    command => active.TrySubmitCommand(command, out _));
                view.Root.style.display = DisplayStyle.None;
                seatControls.Add(status);
                foreach (var c in connections)
                {
                    var owner = c.Client;
                    if (owner.GrantedSeat == null) continue;
                    seatControls.Add(new Button(() => Switch(owner))
                        { text = "Control " + HeroLabel(owner), name = "pve-control-" + owner.TargetSeat.Value });
                }
                seatControls.Add(new Button(Reconnect) { text = "Reconnect" });
                seatControls.Add(new Button(Leave) { text = "Leave" });
            }, directorPlayerSide: TeamSide.Blue);
            SceneManager.LoadScene(PveLauncher.MatchSceneName);
        }

        private void Switch(PrivateSessionClient owner)
        {
            if (owner.State != PrivateSessionClientState.Seated || !owner.HasMatchStarted || owner.LatestProjection == null) return;
            active = owner;
            runtime.AttachOnlineClient(active);
            projectionRevision = -1;
        }

        private static string HeroLabel(PrivateSessionClient owner)
        {
            var row = owner.Roster.FirstOrDefault(r => r.Seat.Equals(owner.TargetSeat));
            return RegisteredHeroRoster.ForThisBuild.FirstOrDefault(h => h.HeroIdValue == row?.Hero.Value).DisplayName
                ?? owner.TargetSeat.Value;
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
            seatControls.Add(new Label(feedback));
            seatControls.Add(new Button(Leave) { text = "Back" });
        }

        private void Leave()
        {
            Destroy(gameObject);
            SceneManager.LoadScene(PveLauncher.MatchSceneName);
        }

        private void OnDestroy()
        {
            foreach (var c in connections) c.Dispose();
            server?.Shutdown();
            listener?.Dispose();
            if (document?.panelSettings != null) Destroy(document.panelSettings);
        }
    }
}
