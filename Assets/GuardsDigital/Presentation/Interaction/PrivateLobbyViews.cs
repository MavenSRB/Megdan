using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.35a. The primitive private lobby drawn in UI Toolkit: a seat table, one line of
    /// status, one line of feedback, and exactly the controls the model offers.
    /// </summary>
    /// <remarks>
    /// <para>Primitive on purpose - the block's non-goals rule out production styling - but not
    /// careless about the two things its third acceptance check names. Team, occupancy and
    /// readiness are drawn as words from <see cref="PrivateLobbyLabels"/>, so nothing here is
    /// legible only in colour; and every size is written in the panel's own reference units under
    /// <see cref="MatchHudPanelScaling"/>, with the frame itself bounded as a share of the
    /// viewport, so the same controls are reachable at every supported desktop resolution rather
    /// than at the one they were authored at.</para>
    /// <para>The seat rows and the controls are rebuilt on every render rather than diffed. That is
    /// the primitive choice, and it is also the one that cannot leave a control standing after the
    /// state that offered it has gone - which is the property milestone 8.34 made terminal and
    /// milestone 8.35b's handoff depends on.</para>
    /// </remarks>
    public sealed class PrivateLobbyUnityView : IPrivateLobbyView
    {
        public const string RootName = "gd-lobby-root";
        public const string PanelName = "gd-lobby-panel";
        public const string StatusName = "gd-lobby-status";
        public const string SessionName = "gd-lobby-session";
        public const string FeedbackName = "gd-lobby-feedback";
        public const string EntryName = "gd-lobby-entry";
        public const string AddressFieldName = "gd-lobby-address";
        public const string PortFieldName = "gd-lobby-port";
        public const string KeyFieldName = "gd-lobby-key";
        public const string PickedSeatName = "gd-lobby-picked-seat";
        public const string SeatsName = "gd-lobby-seats";
        public const string SeatRowName = "gd-lobby-seat-row";
        public const string SeatPickName = "gd-lobby-seat-pick";
        public const string ControlsName = "gd-lobby-controls";
        public const string ControlName = "gd-lobby-control";

        /// <summary>The default invitation: this machine, on the protocol's own port.</summary>
        public const string DefaultAddress = "127.0.0.1";

        private static readonly Color PanelBackground = new Color(0.06f, 0.07f, 0.09f, 0.97f);
        private static readonly Color PanelText = new Color(0.93f, 0.94f, 0.96f, 1f);
        private static readonly Color PanelEdge = new Color(0.45f, 0.48f, 0.55f, 1f);

        private readonly VisualElement seats;
        private readonly VisualElement controls;
        private readonly VisualElement entry;
        private readonly Label status;
        private readonly Label session;
        private readonly Label feedback;
        private readonly Label pickedSeat;
        private readonly TextField address;
        private readonly TextField port;
        private readonly TextField key;
        private SeatId picked;

        public PrivateLobbyUnityView()
        {
            Root = new VisualElement { name = RootName };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.right = 0f;
            Root.style.bottom = 0f;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;

            var frame = new VisualElement { name = PanelName };
            frame.style.width = new StyleLength(new Length(72f, LengthUnit.Percent));
            frame.style.maxWidth = 1800f;
            frame.style.maxHeight = new StyleLength(new Length(92f, LengthUnit.Percent));
            frame.style.paddingLeft = 24f;
            frame.style.paddingRight = 24f;
            frame.style.paddingTop = 20f;
            frame.style.paddingBottom = 20f;
            frame.style.backgroundColor = PanelBackground;
            frame.style.borderTopWidth = 2f;
            frame.style.borderBottomWidth = 2f;
            frame.style.borderLeftWidth = 2f;
            frame.style.borderRightWidth = 2f;
            frame.style.borderTopColor = PanelEdge;
            frame.style.borderBottomColor = PanelEdge;
            frame.style.borderLeftColor = PanelEdge;
            frame.style.borderRightColor = PanelEdge;
            frame.style.flexDirection = FlexDirection.Column;
            Root.Add(frame);

            frame.Add(Caption("PRIVATE LOBBY", 30, "gd-lobby-title"));
            status = Caption(string.Empty, 22, StatusName);
            frame.Add(status);
            session = Caption(string.Empty, 16, SessionName);
            frame.Add(session);
            feedback = Caption(string.Empty, 18, FeedbackName);
            feedback.style.whiteSpace = WhiteSpace.Normal;
            frame.Add(feedback);

            entry = new VisualElement { name = EntryName };
            entry.style.flexDirection = FlexDirection.Column;
            entry.style.marginTop = 8f;
            address = Field("ADDRESS", DefaultAddress, AddressFieldName);
            port = Field(
                "PORT",
                PrivateSessionProtocol.DefaultPort.ToString(CultureInfo.InvariantCulture),
                PortFieldName);
            key = Field("SESSION KEY", string.Empty, KeyFieldName);
            entry.Add(address);
            entry.Add(port);
            entry.Add(key);
            pickedSeat = Caption("SEAT: NONE PICKED", 18, PickedSeatName);
            entry.Add(pickedSeat);
            frame.Add(entry);

            seats = new VisualElement { name = SeatsName };
            seats.style.flexDirection = FlexDirection.Column;
            seats.style.marginTop = 10f;
            frame.Add(seats);

            controls = new VisualElement { name = ControlsName };
            controls.style.flexDirection = FlexDirection.Row;
            controls.style.flexWrap = Wrap.Wrap;
            controls.style.marginTop = 12f;
            frame.Add(controls);
        }

        public VisualElement Root { get; }

        /// <summary>Raised when a player presses one of the controls this view drew.</summary>
        public event Action<PrivateLobbyControl, PrivateLobbyEntry> ControlPressed;

        /// <summary>The invitation currently typed into the entry form, plus the picked seat.</summary>
        public PrivateLobbyEntry CurrentEntry() =>
            new PrivateLobbyEntry(address.value, port.value, key.value, picked);

        /// <summary>Picks the seat a join will claim, exactly as pressing its row does.</summary>
        public void PickSeat(SeatId seat)
        {
            picked = seat;
            pickedSeat.text = seat.IsDefined
                ? "SEAT: " + PrivateLobbyLabels.Identifier(seat.Value)
                : "SEAT: NONE PICKED";
        }

        public void Render(PrivateLobbyModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Root.style.display = model.IsShown ? DisplayStyle.Flex : DisplayStyle.None;
            Root.pickingMode = model.IsShown ? PickingMode.Position : PickingMode.Ignore;
            status.text = model.StatusLabel;
            session.text = model.SessionLabel.Length == 0
                ? string.Empty
                : "SESSION " + model.SessionLabel;
            feedback.text = model.FeedbackLabel;
            feedback.style.display = model.FeedbackLabel.Length == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            var isEntry = model.Role == PrivateLobbyRole.None;
            entry.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;

            seats.Clear();
            foreach (var row in model.Seats)
            {
                seats.Add(BuildSeatRow(row, isEntry));
            }

            controls.Clear();
            foreach (var control in model.Controls)
            {
                controls.Add(BuildControl(control));
            }
        }

        private VisualElement BuildSeatRow(PrivateLobbySeatRow row, bool offerPick)
        {
            var element = new VisualElement { name = SeatRowName };
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.marginBottom = 4f;
            element.Add(Cell(row.SeatLabel, 320f, "gd-lobby-seat-name"));
            element.Add(Cell(row.HeroLabel, 300f, "gd-lobby-seat-hero"));
            element.Add(Cell(row.TeamLabel, 200f, "gd-lobby-seat-team"));
            element.Add(Cell(row.OccupancyLabel, 160f, "gd-lobby-seat-occupancy"));
            element.Add(Cell(row.ReadinessLabel, 200f, "gd-lobby-seat-readiness"));
            element.Add(Cell(row.IsOwnSeat ? "YOU" : string.Empty, 90f, "gd-lobby-seat-own"));
            if (offerPick)
            {
                var pick = new Button(() =>
                {
                    PickSeat(row.Seat);
                })
                {
                    name = SeatPickName,
                    text = "PICK"
                };
                pick.style.minWidth = 120f;
                pick.style.height = 40f;
                element.Add(pick);
            }

            return element;
        }

        private Button BuildControl(PrivateLobbyControl control)
        {
            var button = new Button(() =>
                ControlPressed?.Invoke(control, CurrentEntry()))
            {
                name = ControlName,
                text = PrivateLobbyLabels.Control(control)
            };
            button.style.minWidth = 220f;
            button.style.height = 56f;
            button.style.marginRight = 10f;
            button.style.fontSize = 20f;
            return button;
        }

        private static Label Caption(string text, int fontSize, string name)
        {
            var label = new Label(text) { name = name };
            label.style.color = PanelText;
            label.style.fontSize = fontSize;
            label.style.marginTop = 2f;
            label.style.marginBottom = 2f;
            return label;
        }

        private static Label Cell(string text, float width, string name)
        {
            var label = Caption(text, 18, name);
            label.style.width = width;
            label.style.minWidth = width;
            return label;
        }

        private static TextField Field(string label, string value, string name)
        {
            var field = new TextField(label) { name = name, value = value };
            field.style.marginTop = 2f;
            field.style.marginBottom = 2f;
            field.style.width = 620f;
            field.labelElement.style.color = PanelText;
            field.labelElement.style.minWidth = 200f;
            return field;
        }
    }

    /// <summary>
    /// Milestone 8.35a. The one process-level piece of the lobby that knows a transport exists:
    /// it owns the panel, opens and closes the socket a pressed control asks for, pumps it into
    /// the session state machines on this thread, and hands the presenter a redraw when something
    /// a player can see has changed.
    /// </summary>
    /// <remarks>
    /// <para>Attached only when the player was launched with the lobby flag, exactly as the host
    /// and client drivers are: a normally launched build carries no lobby at all. The two
    /// command-line drivers are untouched - this is an additional interactive path over the same
    /// session layer, and the rehearsal script's marker and exit-code contract belongs to them.</para>
    /// <para>It composes a session but never the match: the authority it hands
    /// <see cref="PrivateSessionHost"/> is the one the scene already composed, and nothing here
    /// submits, executes or restores anything.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateLobbyDriver : MonoBehaviour
    {
        public const string MatchStatusName = "gd-online-match-status";
        /// <summary>The lobby draws above the match HUD, which sorts at 100.</summary>
        private const float LobbySortingOrder = 200f;

        private const int ConnectBudgetSeconds = 5;

        /// <summary>
        /// The seam milestone 8.34's real-socket reconnect fixture recorded as owed here: a
        /// client machine's outbox is fixed at construction, and the socket it was built over is
        /// exactly what a reconnect replaces, so a returning client answers the second challenge
        /// down the first dead socket unless the outbox can be swapped under it.
        /// </summary>
        private sealed class SwappableClientOutbox : IPrivateSessionClientOutbox
        {
            public TcpSessionClientTransport Live { get; set; }

            public void Send(byte[] frame) => Live?.Send(frame);

            public void Close() => Live?.Close();
        }

        private readonly SwappableClientOutbox clientOutbox = new SwappableClientOutbox();
        private MatchSceneCompositionRoot compositionRoot;
        private UIDocument document;
        private PanelSettings panelSettings;
        private PrivateLobbyUnityView view;
        private PrivateLobbyPresenter presenter;
        private TcpSessionListener listener;
        private TcpSessionClientTransport transport;
        private PrivateSessionHost session;
        private PrivateSessionClient machine;
        private SeatId joinedSeat;
        private string sessionKey = string.Empty;
        private PrivateSessionEndpoint dialled;
        private string signature = string.Empty;

        /// <summary>Milestone 8.35k. So one refusal is logged once rather than every redraw.</summary>
        private bool refusalReported;
        private Label matchStatus;
        private bool onlineHudAttached;
        private bool outcomeReported;

        /// <summary>
        /// Host-only event observation for the opt-in build rehearsal. The ordinary lobby does
        /// not subscribe; exposing the already privacy-bounded records keeps the automation from
        /// reaching into match authority or transport internals.
        /// </summary>
        internal event Action<PrivateSessionEventRecord> HostEventObserved;

        public void Configure(MatchSceneCompositionRoot root)
        {
            compositionRoot = root;
            // Milestone 8.31a's reasoning, unchanged: a lobby that pauses on focus loss stops
            // pumping the session it is showing, and six windows on one machine are mostly
            // unfocused. This one process only; a normal launch never reaches this line.
            UnityEngine.Application.runInBackground = true;
        }

        /// <summary>The presenter this driver drives, for a journey that composes it deliberately.</summary>
        public PrivateLobbyPresenter Presenter => presenter;

        /// <summary>The view this driver draws into.</summary>
        public PrivateLobbyUnityView View => view;

        internal PrivateSessionClient RehearsalClient => machine;

        internal PrivateSessionHost RehearsalHost => session;

        internal bool RehearsalHudAttached => onlineHudAttached;

        private void Start()
        {
            if (compositionRoot?.Runtime == null)
            {
                PrivateOnlineMarkers.Fail("the lobby driver was attached without a runtime");
                return;
            }

            BuildPanel();
            view = new PrivateLobbyUnityView();
            document.rootVisualElement?.Add(view.Root);
            matchStatus = new Label { name = MatchStatusName };
            matchStatus.style.position = Position.Absolute;
            matchStatus.style.top = 92f;
            matchStatus.style.left = new StyleLength(new Length(35f, LengthUnit.Percent));
            matchStatus.style.width = new StyleLength(new Length(30f, LengthUnit.Percent));
            matchStatus.style.minHeight = 44f;
            matchStatus.style.unityTextAlign = TextAnchor.MiddleCenter;
            matchStatus.style.fontSize = 18f;
            matchStatus.style.color = new Color(0.93f, 0.94f, 0.96f, 1f);
            matchStatus.style.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 0.94f);
            matchStatus.style.display = DisplayStyle.None;
            matchStatus.pickingMode = PickingMode.Ignore;
            document.rootVisualElement?.Add(matchStatus);
            presenter = new PrivateLobbyPresenter(view);
            view.ControlPressed += (control, entry) => presenter.Press(control, entry);
            presenter.HostRequested += BeginHosting;
            presenter.JoinRequested += BeginJoining;
            presenter.LeaveRequested += Leave;
            presenter.ReconnectRequested += Redial;
            presenter.ShowEntry(LocalSeats(), string.Empty);
            PrivateOnlineMarkers.Log("LOBBY_BEGIN");
        }

        private void Update()
        {
            if (presenter == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (session != null && listener != null)
            {
                while (listener.TryDequeue(out var hostEvent))
                {
                    switch (hostEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            session.OnConnected(hostEvent.Ordinal, now.Ticks);
                            break;
                        case TransportEventKind.Frame:
                            session.OnFrame(hostEvent.Ordinal, hostEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            session.OnStreamRejected(hostEvent.Ordinal, hostEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            session.OnDisconnected(hostEvent.Ordinal);
                            break;
                    }
                }

                session.Sweep(now.Ticks);
                foreach (var record in session.DrainEvents())
                {
                    PrivateOnlineMarkers.LogHostEvent(record, session);
                    HostEventObserved?.Invoke(record);
                }
            }

            if (machine != null && transport != null)
            {
                while (transport.TryDequeue(out var clientEvent))
                {
                    switch (clientEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            machine.OnConnected();
                            break;
                        case TransportEventKind.Frame:
                            machine.OnFrame(clientEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            machine.OnStreamRejected(clientEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            machine.OnDisconnected();
                            break;
                    }
                }
            }

            ReportOutcomeOnce();

            var current = Signature();
            if (!string.Equals(current, signature, StringComparison.Ordinal))
            {
                signature = current;
                // Milestone 8.35k. The diagnostic gap that made this corrective take a day to
                // diagnose from a returned playtest log: a refused client left the reason nowhere.
                // The reason is the host's own named refusal and carries nothing private - the
                // capability is not a parameter it can arrive through.
                if (machine != null &&
                    machine.State == PrivateSessionClientState.Rejected &&
                    !refusalReported)
                {
                    refusalReported = true;
                    PrivateOnlineMarkers.Log("LOBBY_REFUSED reason=" + machine.RejectionReason);
                }
                if (!onlineHudAttached && machine != null && machine.HasMatchStarted &&
                    machine.State == PrivateSessionClientState.Seated &&
                    machine.LatestProjection != null)
                {
                    compositionRoot.Runtime.AttachOnlineClient(machine);
                    onlineHudAttached = true;
                    PrivateOnlineMarkers.Log(
                        "LOBBY_MATCH_HUD seat=" + machine.TargetSeat.Value +
                        " version=" + machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture));
                }

                presenter.Refresh();
                if (onlineHudAttached)
                {
                    compositionRoot.Runtime.RefreshAll();
                }

                RenderMatchStatus();
            }
        }

        /// <summary>
        /// Milestone 8.36. Writes this process's own ending exactly once, so an invited playtest's
        /// returned logs can be compared with the host's instead of with what a player remembers.
        /// </summary>
        /// <remarks>
        /// The rehearsal driver wrote its own outcome marker and will not be attached when six
        /// people are playing, which left the ordinary lobby path saying nothing at all about the
        /// ending it reached. The line is <see cref="PrivateOnlineDiagnostics.OutcomeLine"/>'s, so
        /// what it may contain is decided by that signature rather than here, and the host form
        /// omits the seat because a dedicated host holds none.
        /// </remarks>
        private void ReportOutcomeOnce()
        {
            if (outcomeReported || compositionRoot?.Runtime == null)
            {
                return;
            }

            string line;
            if (session != null)
            {
                var authoritative = compositionRoot.Runtime.Host.Projection;
                line = PrivateOnlineDiagnostics.OutcomeLine(
                    string.Empty, authoritative.Version, authoritative.Outcome);
            }
            else if (onlineHudAttached && machine?.LatestProjection != null)
            {
                line = PrivateOnlineDiagnostics.OutcomeLine(
                    machine.TargetSeat.Value,
                    machine.LatestProjection.Version,
                    compositionRoot.Runtime.ReadModel.Outcome);
            }
            else
            {
                return;
            }

            if (line.Length == 0)
            {
                return;
            }

            outcomeReported = true;
            PrivateOnlineMarkers.Log(line);
        }

        /// <summary>
        /// Milestone 8.35k. Where this machine keeps its road back between processes.
        /// </summary>
        /// <remarks>
        /// `persistentDataPath` is per-user and outside the installation, so a packaged build stays
        /// read-only and a returned playtest log bundle sweeps up nothing from here. The Application
        /// assembly is Unity-free, so it takes the directory rather than discovering it - this is the
        /// one place that knows Unity is underneath.
        /// </remarks>
        private static IRetainedSeatCapabilityStore RetainedCapabilities() =>
            new FileRetainedSeatCapabilityStore(UnityEngine.Application.persistentDataPath);

        /// <summary>
        /// Everything a player could see, as one string. The lobby redraws when this moves and not
        /// otherwise, so a primitive rebuild-everything render costs one frame per real change
        /// rather than one per frame.
        /// </summary>
        private string Signature()
        {
            var builder = new StringBuilder();
            if (machine != null)
            {
                builder.Append("client:").Append(machine.State).Append(':')
                    .Append(machine.RejectionReason).Append(':')
                    .Append(machine.HasMatchStarted ? '1' : '0').Append(':')
                    .Append(machine.IsWaitingForHost ? '1' : '0').Append(':')
                    .Append(machine.IsHostResponseDelayed ? '1' : '0').Append(':')
                    .Append(machine.MatchFeedback).Append(':')
                    .Append(machine.LatestProjection?.Version ?? -1).Append(':')
                    .Append(machine.ProjectionRevision).Append(':')
                    .Append(machine.LastSubmissionResult?.SubmissionId ?? string.Empty).Append(':')
                    .Append(machine.LastAgreementResult?.SubmissionId ?? string.Empty);
                foreach (var entry in machine.Roster)
                {
                    builder.Append('|').Append(entry.Seat.Value).Append(entry.Occupancy)
                        .Append(entry.IsReady ? '1' : '0');
                }
            }
            else if (session != null)
            {
                builder.Append("host:").Append(session.ClaimedSeatCount).Append(':')
                    .Append(session.ReadySeatCount).Append(':')
                    .Append(session.HasMatchStarted ? '1' : '0');
            }
            else
            {
                builder.Append("entry");
            }

            return builder.ToString();
        }

        private void RenderMatchStatus()
        {
            if (matchStatus == null)
            {
                return;
            }

            var show = onlineHudAttached && machine != null && machine.HasMatchStarted;
            matchStatus.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            matchStatus.text = show ? NetworkMatchFeedback.For(machine).ToUpperInvariant() : string.Empty;
        }

        private IReadOnlyList<SeatRosterEntry> LocalSeats()
        {
            var rows = new List<SeatRosterEntry>();
            foreach (var seat in compositionRoot.Runtime.Host.Seats.Seats)
            {
                rows.Add(new SeatRosterEntry(
                    seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Open));
            }

            return rows;
        }

        private void BeginHosting(PrivateLobbyEntry entry)
        {
            if (!TryReadEndpoint(entry, out var endpoint, out var error))
            {
                presenter.ShowEntry(LocalSeats(), error);
                return;
            }

            // Milestone 8.35a1. The same rule the command-line host role has always obeyed, read
            // from the one place that now states it, and read before a listener exists: an unkeyed
            // session may bind only the loopback interface, because its link stays plaintext.
            if (PrivateSessionProtocol.BindRequiresSessionKey(
                    endpoint.IsLoopback, entry == null ? string.Empty : entry.SessionKey))
            {
                presenter.ShowEntry(
                    LocalSeats(), PrivateLobbyLabels.UnkeyedPublicBindRefusal);
                return;
            }

            try
            {
                listener = new TcpSessionListener();
                listener.Start(endpoint);
            }
            catch (Exception exception)
            {
                listener?.Dispose();
                listener = null;
                presenter.ShowEntry(
                    LocalSeats(), "The host could not bind: " + exception.GetType().Name + ".");
                return;
            }

            sessionKey = entry.SessionKey;
            session = new PrivateSessionHost(
                compositionRoot.Runtime.Host, compositionRoot.Runtime.SessionIdentity,
                sessionKey, listener, new CryptoNonceSource());
            signature = string.Empty;
            presenter.AttachHost(session, string.Empty);
            PrivateOnlineMarkers.Log(
                "LOBBY_HOSTING port=" + listener.BoundPort.ToString(CultureInfo.InvariantCulture) +
                " keyed=" + (sessionKey.Length > 0 ? "true" : "false"));
        }

        private void BeginJoining(PrivateLobbyEntry entry)
        {
            if (!TryReadEndpoint(entry, out var endpoint, out var error))
            {
                presenter.ShowEntry(LocalSeats(), error);
                return;
            }

            joinedSeat = entry.Seat;
            sessionKey = entry.SessionKey;
            dialled = endpoint;
            refusalReported = false;
            // Milestone 8.35k. The road back for a relaunched process. The store is consulted by
            // the client itself once the host names the session in its Welcome - the correlation is
            // the host's to assign, so it cannot be keyed here, before the handshake.
            machine = new PrivateSessionClient(
                compositionRoot.Runtime.SessionIdentity, joinedSeat, sessionKey,
                clientOutbox, new CryptoNonceSource(),
                retainedCapabilities: RetainedCapabilities());
            OpenTransport();
            if (!Dial())
            {
                machine = null;
                return;
            }

            signature = string.Empty;
            presenter.AttachClient(machine);
            PrivateOnlineMarkers.Log("LOBBY_JOINING seat=" + joinedSeat.Value);
        }

        /// <summary>
        /// Dials the host that was typed, for a first join or for an armed return. A refused or
        /// unreachable host is words on the entry form, not a silent state - the join-failure half
        /// of the block's second acceptance check.
        /// </summary>
        private bool Dial()
        {
            if (transport.Connect(dialled, ConnectBudgetSeconds, out var error))
            {
                return true;
            }

            DisposeTransport();
            presenter.ShowEntry(LocalSeats(), "The host could not be reached: " + error);
            return false;
        }

        /// <summary>
        /// Dials again for a return the presenter has already armed. The machine is the same one -
        /// it holds the capability the host will ask for, and a fresh client would hold none - so
        /// only the socket under its outbox is replaced.
        /// </summary>
        private void Redial()
        {
            if (machine == null)
            {
                return;
            }

            DisposeTransport();
            OpenTransport();
            if (!Dial())
            {
                machine.OnReconnectTransportFailed();
                signature = string.Empty;
                presenter.AttachClient(machine);
                return;
            }

            signature = string.Empty;
            presenter.AttachClient(machine);
            PrivateOnlineMarkers.Log("LOBBY_RECONNECTING seat=" + joinedSeat.Value);
        }

        /// <summary>
        /// Closes this client's real socket for the opt-in rehearsal, then reports the observed
        /// loss through the same client state machine the transport would notify. Reconnect still
        /// has to be pressed through <see cref="PrivateLobbyPresenter"/> and therefore retains the
        /// same capability-bearing machine while replacing only its transport.
        /// </summary>
        internal bool InterruptConnectionForRehearsal()
        {
            if (machine == null || machine.State != PrivateSessionClientState.Seated)
            {
                return false;
            }

            DisposeTransport();
            machine.OnDisconnected();
            signature = string.Empty;
            presenter?.Refresh();
            return true;
        }

        private void OpenTransport()
        {
            transport = new TcpSessionClientTransport();
            clientOutbox.Live = transport;
        }

        private void Leave()
        {
            var departing = machine;
            DisposeTransport();
            departing?.OnDisconnected();
            machine = null;
            if (session != null)
            {
                session.Shutdown();
                session = null;
            }

            listener?.Dispose();
            listener = null;
            onlineHudAttached = false;
            outcomeReported = false;
            if (matchStatus != null)
            {
                matchStatus.text = string.Empty;
                matchStatus.style.display = DisplayStyle.None;
            }

            compositionRoot?.Runtime?.RefreshAll();
            signature = string.Empty;
            presenter.ShowEntry(LocalSeats(), string.Empty);
            PrivateOnlineMarkers.Log("LOBBY_LEFT");
        }

        private static bool TryReadEndpoint(
            PrivateLobbyEntry entry, out PrivateSessionEndpoint endpoint, out string error)
        {
            endpoint = null;
            if (entry == null)
            {
                error = "An address and port are required.";
                return false;
            }

            if (!int.TryParse(entry.Port, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var port))
            {
                error = "A port is a whole number between 1 and 65535.";
                return false;
            }

            return PrivateSessionEndpoint.TryCreate(entry.Address, port, out endpoint, out error);
        }

        private void BuildPanel()
        {
            document = gameObject.GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            // Milestone 8.25c's coordinate space, reused rather than reinvented: the lobby's sizes
            // are reference units in exactly the space the HUD's are, so the same controls are
            // reachable at every supported desktop viewport.
            var authored = Resources.Load<PanelSettings>(
                ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath);
            panelSettings = authored != null
                ? Instantiate(authored)
                : ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Guards Private Lobby Panel";
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            MatchHudPanelScaling.Apply(panelSettings);
            panelSettings.clearColor = false;
            panelSettings.sortingOrder = LobbySortingOrder;
            var theme = Resources.Load<ThemeStyleSheet>(
                ConfiguredMatchHudRenderer.RuntimeThemeResourcePath);
            if (theme != null)
            {
                panelSettings.themeStyleSheet = theme;
            }

            document.panelSettings = panelSettings;
            if (document.rootVisualElement == null)
            {
                document.enabled = false;
                document.enabled = true;
            }

            var root = document.rootVisualElement;
            if (root != null)
            {
                root.name = "gd-lobby-document";
                root.pickingMode = PickingMode.Ignore;
            }
        }

        private void DisposeTransport()
        {
            transport?.Dispose();
            transport = null;
            clientOutbox.Live = null;
        }

        private void OnDestroy()
        {
            DisposeTransport();
            listener?.Dispose();
            listener = null;
            onlineHudAttached = false;
            if (panelSettings != null)
            {
                Destroy(panelSettings);
                panelSettings = null;
            }
        }
    }
}
