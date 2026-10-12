using System;
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
    /// <summary>PVE-D28. Playing host plus two owner-scoped clients over the existing keyed transport.</summary>
    public sealed partial class PveCoopLobby : MonoBehaviour, IPrivateSessionClientOutbox
    {
        private static readonly string[] ProvisionalHeroes = { "hero-arien", "hero-brogan", "hero-wasp" };
        private PveEnemyCatalog catalog;
        private PveEncounterDefinition layout;
        private GuardsDigital.Core.Board.BoardMapDefinition map;
        private ulong seed;
        private PrivateSessionHost server;
        private PrivateSessionClient client;
        private TcpSessionListener listener;
        private TcpSessionClientTransport transport;
        private PveCoopMatch match;
        private ConfiguredMatchSceneRuntime runtime;
        private PveEncounterView overlay;
        private UIDocument document;
        private VisualElement panel, lobby, roster, heroes;
        private TextField address, port, key, seedInput, playerName;
        private Label status;
        private string dialAddress, error = string.Empty, rosterKey;
        private bool loading;
        private float nextStep;
        private long lastProjection = -1;

        public static PveCoopLobby Open(ulong seed)
        {
            var view = new GameObject("PvE Co-op Lobby").AddComponent<PveCoopLobby>();
            DontDestroyOnLoad(view.gameObject);
            view.seed = seed;
            view.Build();
            return view;
        }

        private void Build()
        {
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = Instantiate(Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath));
            document.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ConfiguredMatchHudRenderer.RuntimeThemeResourcePath);
            document.panelSettings.sortingOrder = 500;
            MatchHudPanelScaling.Apply(document.panelSettings);
            var root = document.rootVisualElement;
            root.style.backgroundColor = new Color(.07f,.08f,.11f,1f);
            root.style.flexGrow = 1;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            panel = new VisualElement { name = "pve-coop-menu" };
            panel.style.width = 760;
            panel.style.color = Color.white;
            panel.style.fontSize = 18;
            panel.style.paddingLeft = panel.style.paddingRight = 20;
            panel.style.paddingTop = panel.style.paddingBottom = 20;
            panel.style.backgroundColor = new Color(.07f,.08f,.11f,.98f);
            root.Add(panel);
            var title = new Label("PvE CO-OP — HOST / JOIN") { name = "pve-coop-title" };
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            status = new Label { name = "pve-coop-status" };
            status.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(status);
            lobby = new VisualElement();
            panel.Add(lobby);
            playerName = Field("Player name", "Player");
            address = Field("Host public IP (internet Join)", "127.0.0.1");
            port = Field("TCP port (same as PvP)", PrivateSessionProtocol.DefaultPort.ToString(CultureInfo.InvariantCulture));
            key = Field("Session key (required for LAN/internet)", "");
            key.isPasswordField = true;
            seedInput = Field("Encounter seed (same on all three players)", seed.ToString(CultureInfo.InvariantCulture));
            var instructions = new Label("Internet: set a session key, allow the selected TCP port through the host firewall and forward it on the router to the host PC. Share your public IP, port, key and seed; all players need the same build and PvE data. No key: local-machine testing only. PvP and PvE cannot host on the same port at the same time.")
                { name = "pve-coop-network-help" };
            instructions.style.whiteSpace = WhiteSpace.Normal;
            lobby.Add(instructions);
            lobby.Add(Button("HOST", () => Begin(true), "pve-coop-host"));
            lobby.Add(Button("JOIN", () => Begin(false), "pve-coop-join"));
            roster = new VisualElement { name = "pve-coop-roster" };
            panel.Add(roster);
            heroes = new VisualElement { name = "pve-coop-heroes" };
            heroes.style.flexDirection = FlexDirection.Row;
            heroes.style.flexWrap = Wrap.Wrap;
            panel.Add(heroes);
            panel.Add(Button("Reconnect", Reconnect, "pve-coop-reconnect"));
            panel.Add(Button("Leave / Back", Leave, "pve-coop-leave"));
        }

        private TextField Field(string label, string value)
        {
            var field = new TextField(label) { value = value };
            field.style.color = Color.black;
            field.labelElement.style.color = Color.white;
            lobby.Add(field);
            return field;
        }

        private static Button Button(string text, Action action, string name = null)
        {
            var button = new Button(action) { text = text, name = name ?? text };
            button.style.color = Color.black;
            button.style.minHeight = 32;
            button.style.marginTop = 3;
            button.style.marginBottom = 3;
            return button;
        }

        private PveComposedEncounter Compose(string[] choices) => PveEncounterComposer.Compose(
            new PveEncounterSetup(choices, seed), layout, catalog, map);

        private PrivateSessionIdentity Identity(PveComposedEncounter provisional)
        {
            var original = PrivateSessionIdentity.ForComposition(map, provisional.InitialState.InitialConfiguration,
                provisional.InitialState.Setup, BuildIdentityStamp.Current);
            return PveCoopMatch.Identity(original, seed, PveContentLoader.SaveCatalog(catalog),
                File.ReadAllText(Path.Combine(PveDataPaths.Directory, "encounter.json")));
        }

        private void Begin(bool hosting)
        {
            if (client != null) return;
            try
            {
                if (!ulong.TryParse(seedInput.value, out seed)) throw new InvalidOperationException("Enter a whole-number seed.");
                if (!int.TryParse(port.value, out var number) || number < 1 || number > 65535)
                    throw new InvalidOperationException("Port must be 1–65535.");
                PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out catalog, out layout);
                map = OfficialBoardMapCatalog.Load().Load(layout.MapReference);
                var provisional = Compose(ProvisionalHeroes);
                var identity = Identity(provisional);
                if (hosting)
                {
                    match = new PveCoopMatch(provisional);
                    listener = new TcpSessionListener();
                    var bind = string.IsNullOrEmpty(key.value) ? "127.0.0.1:" : "0.0.0.0:";
                    if (!PrivateSessionEndpoint.TryParseAddress(bind + number, out var endpoint, out var problem))
                        throw new InvalidOperationException(problem);
                    listener.Start(endpoint);
                    server = new PrivateSessionHost(match.Host, identity, key.value, listener, new CryptoNonceSource());
                    server.SeatComposition = selection =>
                    {
                        var selected = selection.PickOrder.Select(seat =>
                        {
                            selection.TryGetChoice(seat.Seat, out var hero);
                            return hero.HeroIdValue;
                        }).ToArray();
                        match = new PveCoopMatch(Compose(selected));
                        return match.Host;
                    };
                }
                dialAddress = (hosting ? "127.0.0.1" : address.value.Trim()) + ":" + number;
                if (!PrivateSessionEndpoint.TryParseAddress(dialAddress, out var target, out var targetError))
                    throw new InvalidOperationException(targetError);
                if (!target.IsLoopback && string.IsNullOrEmpty(key.value))
                    throw new InvalidOperationException("LAN/internet sessions require a shared key.");
                transport = new TcpSessionClientTransport();
                client = new PrivateSessionClient(identity, hosting ? new SeatId("blue-player-01") : default,
                    key.value, this, new CryptoNonceSource());
                client.PlayerName = playerName.value;
                if (!transport.Connect(target, 3, out var connectError)) throw new InvalidOperationException(connectError);
                lobby.style.display = DisplayStyle.None;
                error = string.Empty;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Close();
            }
        }

        private void Update()
        {
            if (listener != null)
            {
                foreach (var item in listener.Drain())
                    switch (item.Kind)
                    {
                        case TransportEventKind.Connected: server.OnConnected(item.Ordinal, DateTime.UtcNow.Ticks); break;
                        case TransportEventKind.Frame: server.OnFrame(item.Ordinal, item.Frame); break;
                        case TransportEventKind.StreamRejected: server.OnStreamRejected(item.Ordinal, item.Reason); break;
                        case TransportEventKind.Disconnected: server.OnDisconnected(item.Ordinal); break;
                    }
                server.Sweep(DateTime.UtcNow.Ticks);
            }
            if (transport != null)
                foreach (var item in transport.Drain())
                    switch (item.Kind)
                    {
                        case TransportEventKind.Connected: client.OnConnected(); break;
                        case TransportEventKind.Frame: client.OnFrame(item.Frame); break;
                        case TransportEventKind.StreamRejected: client.OnStreamRejected(item.Reason); break;
                        case TransportEventKind.Disconnected: client.OnDisconnected(); break;
                    }
            if (server != null && server.HasMatchStarted && !server.IsPaused && runtime != null && Time.unscaledTime >= nextStep)
            {
                var result = match.Session.Step(out var step);
                nextStep = Time.unscaledTime + .45f;
                if (result != null && result.Accepted) server.RepublishProjections();
                else if (result != null) error = result.Detail;
            }
            if (client != null && client.HasMatchStarted && client.LatestProjection != null && !loading) LoadMatch();
            if (runtime != null && client.ProjectionRevision != lastProjection)
            {
                lastProjection = client.ProjectionRevision;
                runtime.RefreshAll();
            }
            Render();
        }

        private void LoadMatch()
        {
            loading = true;
            var choices = client.Roster.OrderBy(row => row.Seat.Value, StringComparer.Ordinal).Select(row => row.Hero.Value).ToArray();
            var encounter = Compose(choices);
            MatchSceneCompositionRoot.NextComposedExternalMatch = new ExternalMatchComposition(encounter.InitialState,
                composed =>
                {
                    runtime = composed;
                    runtime.AttachOnlineClient(client);
                    overlay = PveEncounterView.AttachCoop(runtime, encounter, server == null ? null : match.Session,
                        command =>
                        {
                            if (server.IsPaused) return;
                            var result = match.Session.Execute(command);
                            if (!result.Accepted) error = result.Detail;
                            server.RepublishProjections();
                        }, command => client.TrySubmitCommand(command, out _));
                    panel.style.position = Position.Absolute;
                    document.rootVisualElement.pickingMode = PickingMode.Ignore;
                    document.rootVisualElement.style.backgroundColor = Color.clear;
                    panel.Q<Label>("pve-coop-title").text = "PvE CO-OP";
                    panel.style.right = 8; panel.style.bottom = 8; panel.style.width = 340;
                    roster.style.display = heroes.style.display = DisplayStyle.None;
                }, directorPlayerSide: TeamSide.Blue);
            SceneManager.LoadScene(PveLauncher.MatchSceneName, LoadSceneMode.Single);
        }

        private void Render()
        {
            status.text = error.Length > 0 ? error : client == null ? "Three players. One hero each. Host resolves shared decisions." :
                client.State + (server != null && server.IsPaused ? " — paused, waiting for reconnect" : "") +
                (client.RejectionDetail.Length > 0 ? " — " + client.RejectionDetail : "") +
                (client.HasMatchStarted ? " — host decides shared choices" : " — claim a seat, Ready, then pick a hero");
            panel.Q<Button>("pve-coop-reconnect").SetEnabled(client != null && client.CanAttemptReconnect);
            if (client == null || loading) return;
            var stamp = client.State + string.Join("|", client.Roster.Select(row => row.Seat + ":" + row.Occupancy + ":" + row.IsReady + ":" + row.Hero + ":" + row.HasChosenHero));
            if (rosterKey == stamp) return;
            rosterKey = stamp;
            roster.Clear(); heroes.Clear();
            foreach (var row in client.Roster)
            {
                var seat = row.Seat;
                roster.Add(new Label(seat + "  " + row.PlayerName + "  " + row.Occupancy + "  " + (row.IsReady ? "Ready" : "Not ready") + "  " + (row.HasChosenHero ? row.Hero.Value : "Pick pending")));
                if (client.State == PrivateSessionClientState.Admitted && row.Occupancy == SeatOccupancyState.Open && seat.Value != "blue-player-01")
                    roster.Add(Button("Take " + seat, () => client.TryClaimSeat(seat)));
            }
            if (client.GrantedSeat == null) return;
            roster.Add(Button(client.IsReady ? "Not ready" : "Ready", () => client.TrySetReadiness(!client.IsReady), "pve-coop-ready"));
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                var id = hero.HeroIdValue;
                var button = Button(hero.DisplayName, () => client.TryChooseHero(id), "pve-coop-pick-" + id);
                button.style.width = 225;
                var next = client.Roster.FirstOrDefault(row => !row.HasChosenHero);
                button.SetEnabled(client.Roster.All(row => row.IsReady && row.Occupancy == SeatOccupancyState.Claimed) &&
                    next != null && next.Seat.Equals(client.TargetSeat) && !client.Roster.Any(row => row.HasChosenHero && row.Hero.Value == id));
                heroes.Add(button);
            }
        }

        private void Reconnect()
        {
            if (client == null || !client.BeginReconnect()) return;
            transport.Dispose();
            transport = new TcpSessionClientTransport();
            if (!PrivateSessionEndpoint.TryParseAddress(dialAddress, out var endpoint, out var problem) ||
                !transport.Connect(endpoint, 3, out problem))
            { error = problem; client.OnReconnectTransportFailed(); }
        }

        void IPrivateSessionClientOutbox.Send(byte[] frame) => transport.Send(frame);
        void IPrivateSessionClientOutbox.Close() => transport.Close();

        private void Close()
        {
            server?.Shutdown();
            transport?.Dispose(); listener?.Dispose();
            client = null; server = null; transport = null; listener = null;
        }
        private void Leave()
        {
            client?.SendGoodbye();
            Close();
            Destroy(gameObject);
            SceneManager.LoadScene(PveEncounterView.LauncherSceneName);
        }
        private void OnDestroy()
        {
            Close();
            if (document != null && document.panelSettings != null) Destroy(document.panelSettings);
        }
    }
}
