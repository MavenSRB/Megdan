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
                ApplyButtonChrome(pick);
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
            ApplyButtonChrome(button);
            return button;
        }

        /// <summary>
        /// Milestone 8.35x (the EIGHTEENTH amendment). The lobby's own controls drew Unity's stock
        /// button skin - the one place in this HUD family that had never adopted
        /// <see cref="MatchHudTheme"/>'s dark chip language at all, which is exactly what read as
        /// "pasted over" a hand-styled dark panel. This gives HOST/JOIN/FILL/LEAVE and a seat's PICK
        /// the same background, border, corner radius and press/hover motion every match-HUD
        /// control now shares, through the same <see cref="HudElements.ApplyButtonMotion"/> this
        /// file borrows rather than re-implements.
        /// </summary>
        private static void ApplyButtonChrome(VisualElement button)
        {
            button.style.color = PanelText;
            button.style.backgroundColor = MatchHudTheme.ButtonIdleBackground;
            HudElements.Border(button, PanelEdge, 1f, MatchHudTheme.ButtonCornerRadius);
            HudElements.ApplyButtonMotion(button, tintBackground: true);
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
    /// THIRTEENTH amendment. What the host asks for when it spawns one test-driver child.
    /// </summary>
    internal sealed class TestDriverSpawnRequest
    {
        public TestDriverSpawnRequest(
            SeatId seat, string address, int port, string sessionKey,
            double? thinkSecondsOverride = null, int parentProcessId = 0)
        {
            Seat = seat;
            Address = address;
            Port = port;
            SessionKey = sessionKey;
            ThinkSecondsOverride = thinkSecondsOverride;
            ParentProcessId = parentProcessId;
        }

        public SeatId Seat { get; }
        public string Address { get; }
        public int Port { get; }
        public string SessionKey { get; }

        /// <summary>
        /// Unset by default, so a real solo session's spawned drivers keep
        /// <see cref="PrivateSeatDriverOptions.DefaultThinkSeconds"/>'s human-followable pace. Set
        /// only by <see cref="ReadThinkSecondsOverride"/> reading the one environment variable the
        /// automated Solo cohort uses to make the same real path finish in a bounded regression
        /// window instead of the many minutes six-players'-worth of human-paced bots would take.
        /// </summary>
        public double? ThinkSecondsOverride { get; }

        /// <summary>
        /// 8.35t corrective. The OS process id of the host that is about to spawn this child - the
        /// one fact a watchdog on the child's own side needs to notice its parent is gone even when
        /// the parent never got the chance to run its own <c>Leave()</c>/<c>OnDestroy()</c> teardown
        /// (a crash, a closed window, a task-killed process). Zero means "no parent to watch",
        /// kept non-fatal so a manually-launched driver (outside any host's own spawn) still runs.
        /// </summary>
        public int ParentProcessId { get; }
    }

    /// <summary>
    /// THIRTEENTH amendment. The one seam allowed to know an OS process exists, so the spawning
    /// decision - which seats, what each gets told - is testable without ever starting a real
    /// process; an Edit Mode fixture injects a fake here instead (Play Mode fixtures cannot: this
    /// assembly's own <c>InternalsVisibleTo</c> stops at the Edit Mode test assembly by design).
    /// </summary>
    internal interface ITestDriverProcessLauncher
    {
        System.Diagnostics.Process Launch(TestDriverSpawnRequest request);
    }

    /// <summary>
    /// Spawns another copy of THIS running executable as a real OS child process, exactly the shape
    /// <c>Invoke-PrivateOnlineRehearsal.ps1</c> already proves for the CI cohorts - reused as a
    /// pattern rather than by literally calling out to that script, which does not exist inside a
    /// shipped player. The endpoint and key travel only through the child's environment block, the
    /// same privacy rule the rehearsal runner and the rehearsal driver both already follow, and a
    /// distinct <c>-logFile</c> per child is load-bearing: several copies of the same build running
    /// at once would otherwise all fight over one default log path.
    /// </summary>
    internal sealed class RealTestDriverProcessLauncher : ITestDriverProcessLauncher
    {
        public System.Diagnostics.Process Launch(TestDriverSpawnRequest request)
        {
            var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                throw new InvalidOperationException("could not resolve this process's own executable path");
            }

            var logDirectory = System.IO.Path.Combine(
                UnityEngine.Application.persistentDataPath, "TestDrivers");
            System.IO.Directory.CreateDirectory(logDirectory);
            var logPath = System.IO.Path.Combine(
                logDirectory, "test-driver-" + request.Seat.Value + ".log");

            var arguments = new List<string>
            {
                GuardsDigital.Network.PrivateSessionLaunchOptions.LobbyFlag,
                PrivateSeatDriverOptions.RoleFlag,
                PrivateSeatDriverOptions.SeatFlag, Quote(request.Seat.Value),
                "-batchmode", "-nographics",
                "-logFile", Quote(logPath)
            };
            if (request.ThinkSecondsOverride.HasValue)
            {
                arguments.Add(PrivateSeatDriverOptions.ThinkSecondsFlag);
                arguments.Add(request.ThinkSecondsOverride.Value.ToString(CultureInfo.InvariantCulture));
            }

            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = string.Join(" ", arguments)
            };
            info.EnvironmentVariables[PrivateSeatDriverOptions.AddressEnvironmentVariable] = request.Address;
            info.EnvironmentVariables[PrivateSeatDriverOptions.PortEnvironmentVariable] =
                request.Port.ToString(CultureInfo.InvariantCulture);
            info.EnvironmentVariables[PrivateSeatDriverOptions.KeyEnvironmentVariable] = request.SessionKey;
            if (request.ParentProcessId > 0)
            {
                info.EnvironmentVariables[PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable] =
                    request.ParentProcessId.ToString(CultureInfo.InvariantCulture);
            }

            return System.Diagnostics.Process.Start(info);
        }

        private static string Quote(string value) => "\"" + (value ?? string.Empty) + "\"";
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
        private HostConsoleReader console;
        private PrivateSessionClient machine;
        private SeatId joinedSeat;
        private string sessionKey = string.Empty;
        private PrivateSessionEndpoint dialled;
        private string signature = string.Empty;

        /// <summary>
        /// 8.35t corrective. Why a spawned child never reached this host at all: the address a
        /// human types to HOST is the bind address, not necessarily a dialable one from a second
        /// process on this same machine. A listener bound to this machine's own real network
        /// address (typed so a remote friend's port-forward lands on it - the shape the tester
        /// instructions' own solo-practice paragraph invites, not only the multi-human case)
        /// actively refuses a loopback dial; confirmed directly with a bare
        /// <c>TcpListener</c>/<c>TcpClient</c> pair before touching this file. Resolved once, right
        /// after a successful bind, so every later spawn dials the address that will actually
        /// answer rather than the form's own unrelated placeholder text.
        /// </summary>
        private string hostDialAddress = string.Empty;

        /// <summary>
        /// THIRTEENTH amendment. This host's own spawned test-driver children - its own OS process
        /// tree, killed when this session ends, per the amendment's own "who owns them, how they
        /// shut down" design point. 8.35t adds a parent-PID watchdog on the child's own side as a
        /// second, independent line of defense: a host that dies without a clean Leave (crash,
        /// closed window, task-killed) still orphaned every spawned child before that corrective,
        /// because nothing but this list's own teardown ever reaped them.
        /// </summary>
        private readonly System.Collections.Generic.List<System.Diagnostics.Process>
            testDriverProcesses = new System.Collections.Generic.List<System.Diagnostics.Process>();
        private readonly System.Collections.Generic.HashSet<SeatId> testDriverSeats =
            new System.Collections.Generic.HashSet<SeatId>();

        /// <summary>
        /// 8.35t corrective. One seat's own join grace period, keyed only while that seat's own
        /// spawn is still unconfirmed: a seat that claims itself before its deadline is simply
        /// removed, never reported as failed. Its presence is also what "must not stack" checks -
        /// a seat already carrying a live deadline is a seat FILL has already tried, so a repeated
        /// press skips it rather than spawning a second, redundant child onto the same seat.
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<SeatId, DateTime> testDriverDeadlines =
            new System.Collections.Generic.Dictionary<SeatId, DateTime>();

        private const double TestDriverJoinGraceSeconds = 12.0;

        private ITestDriverProcessLauncher testDriverLauncher = new RealTestDriverProcessLauncher();

        /// <summary>
        /// Test-only seam: replaces the real process launcher with a fake before a fixture presses
        /// FILL WITH TEST DRIVERS, so the spawning decision is provable without ever starting a real
        /// OS process - spawning six copies of an Editor Play Mode host would otherwise be exactly
        /// what a fixture must never do by accident.
        /// </summary>
        internal void SetTestDriverProcessLauncherForTests(ITestDriverProcessLauncher launcher) =>
            testDriverLauncher = launcher ?? new RealTestDriverProcessLauncher();

        internal IReadOnlyCollection<SeatId> TestDriverSeatsForTests => testDriverSeats;

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

        /// <summary>
        /// 8.35t corrective. Set the instant <see cref="BeginJoining"/> takes either of its own
        /// silent early returns, cleared the instant a join is attempted again: the one thing a
        /// spawned <see cref="PrivateSeatDriver"/> could not previously observe about its own
        /// "successful" Join press - <see cref="PrivateLobbyPresenter.Press"/> returns true the
        /// moment the control was OFFERED, which says nothing about whether the join it triggered
        /// actually reached anyone.
        /// </summary>
        internal string LastJoinFailure { get; private set; }

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
            presenter.FillWithTestDriversRequested += FillEmptySeatsWithTestDrivers;
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
                // Milestone 8.35o. The dedicated host's one typed command, "release <seat>" - the
                // escape hatch milestone 8.35k built but left with no way for a human to reach it.
                // The reader owns its own background thread, so draining it every frame here never
                // blocks this serve loop.
                while (console != null && console.TryDequeue(out var consoleLine))
                {
                    HostConsoleCommands.Dispatch(consoleLine, session);
                }

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

                // 8.35t corrective. A spawned test driver that never claims its seat must not look
                // identical to nothing having happened - checked every frame, but cheap: the
                // dictionary is empty whenever FILL has never been pressed or every prior spawn has
                // already resolved one way or the other.
                CheckTestDriverJoinTimeouts();
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
            console = new HostConsoleReader();
            console.Start();
            signature = string.Empty;
            // 8.35t corrective. The endpoint itself resolves this - a socket-typed decision the
            // architecture record confines to the adapter, per
            // PrivateSessionArchitectureConformanceTests.SocketTypes_LiveOnlyInTheNetworkAdapter -
            // rather than this file naming the socket type its own answer would otherwise require.
            hostDialAddress = endpoint.ResolveLocalDialAddress();
            testDriverSeats.Clear();
            testDriverDeadlines.Clear();
            presenter.AttachHost(session, string.Empty);
            PrivateOnlineMarkers.Log(
                "LOBBY_HOSTING port=" + listener.BoundPort.ToString(CultureInfo.InvariantCulture) +
                " keyed=" + (sessionKey.Length > 0 ? "true" : "false"));
        }

        /// <summary>
        /// THIRTEENTH amendment. An automated regression's only way to make a Solo cohort finish in
        /// a bounded window: the CI runner sets this on the HOST process before it presses FILL, and
        /// the host reads it once and hands each spawned child the equivalent
        /// <see cref="PrivateSeatDriverOptions.ThinkSecondsFlag"/>. Unset for a real solo session, so
        /// nothing about ordinary play depends on this variable existing.
        /// </summary>
        internal const string ThinkSecondsOverrideEnvironmentVariable = "GUARDS_TESTDRIVER_THINK_SECONDS";

        /// <summary>
        /// THIRTEENTH amendment. Spawns one real keyed test-driver client onto every seat still
        /// Open and not already carrying an outstanding attempt of its own, exactly as an operator
        /// running "PLAY - Join Match.bat" would claim one by hand - each child dials
        /// <see cref="hostDialAddress"/> (this host's own actually-bound address, resolved once at
        /// hosting time - 8.35t's corrective, replacing a hardcoded loopback constant that a
        /// non-loopback bind silently defeated) and presents the same session key this host was
        /// hosted with, an ordinary keyed join with no shortcut around it.
        /// </summary>
        /// <remarks>
        /// 8.35t corrective, "must not stack": a seat already carrying a live entry in
        /// <see cref="testDriverDeadlines"/> is a seat this method has already sent a driver to and
        /// not yet heard back from - skipped rather than sent a second, redundant child, so a
        /// player mashing the button out of exactly the frustration this defect caused cannot
        /// accumulate orphaned processes the way the interim-4 report's 34 did.
        /// </remarks>
        private void FillEmptySeatsWithTestDrivers()
        {
            if (session == null || listener == null)
            {
                return;
            }

            var openSeats = new List<SeatId>();
            var freshSeats = new List<SeatId>();
            foreach (var entry in session.Roster)
            {
                if (entry.Occupancy != SeatOccupancyState.Open)
                {
                    continue;
                }

                openSeats.Add(entry.Seat);
                if (!testDriverDeadlines.ContainsKey(entry.Seat))
                {
                    freshSeats.Add(entry.Seat);
                }
            }

            if (openSeats.Count == 0)
            {
                presenter.SetTestDriverSeats(testDriverSeats, "Every seat is already taken.");
                return;
            }

            if (freshSeats.Count == 0)
            {
                // Every still-open seat already has an outstanding attempt - a repeated press is
                // refused with a visible reason instead of silently spawning a second child onto
                // the same seat.
                presenter.SetTestDriverSeats(
                    testDriverSeats, "Test drivers are already on their way for every open seat.");
                return;
            }

            double? thinkSecondsOverride = null;
            var overrideText = Environment.GetEnvironmentVariable(ThinkSecondsOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideText) &&
                double.TryParse(overrideText, System.Globalization.NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var parsedOverride) && parsedOverride >= 0)
            {
                thinkSecondsOverride = parsedOverride;
            }

            var spawned = 0;
            var deadline = DateTime.UtcNow.AddSeconds(TestDriverJoinGraceSeconds);
            foreach (var seat in freshSeats)
            {
                var request = new TestDriverSpawnRequest(
                    seat, hostDialAddress, listener.BoundPort, sessionKey, thinkSecondsOverride,
                    System.Diagnostics.Process.GetCurrentProcess().Id);
                try
                {
                    var process = testDriverLauncher.Launch(request);
                    if (process != null)
                    {
                        testDriverProcesses.Add(process);
                    }

                    testDriverSeats.Add(seat);
                    testDriverDeadlines[seat] = deadline;
                    spawned++;
                    PrivateOnlineMarkers.Log(
                        "LOBBY_TEST_DRIVER_SPAWNED seat=" + seat.Value + " address=" + hostDialAddress);
                }
                catch (Exception exception)
                {
                    PrivateOnlineMarkers.Fail(
                        "test-driver-spawn seat=" + seat.Value + " " + exception.GetType().Name);
                }
            }

            var status = spawned < freshSeats.Count
                ? spawned + "/" + freshSeats.Count + " test drivers spawned; see the log for the rest."
                : "Sent " + spawned + " test driver(s); watch the seats below for who joins.";
            presenter.SetTestDriverSeats(testDriverSeats, status);
        }

        /// <summary>
        /// 8.35t corrective, "must produce visible feedback even when spawning fails". A spawned
        /// child that never claims its seat within its own grace period is reported rather than
        /// left to look identical to nothing having happened: logged for anyone reading the host's
        /// log, and surfaced on the host's own screen through the same feedback line FILL itself
        /// writes to. The seat is then dropped from <see cref="testDriverDeadlines"/> - removed,
        /// not merely expired - so a later FILL press is free to try that seat again rather than
        /// treat a proven failure as a permanently "already attempted" seat.
        /// </summary>
        private void CheckTestDriverJoinTimeouts()
        {
            if (testDriverDeadlines.Count == 0 || session == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            List<SeatId> timedOut = null;
            foreach (var pair in testDriverDeadlines)
            {
                if (now < pair.Value)
                {
                    continue;
                }

                (timedOut ?? (timedOut = new List<SeatId>())).Add(pair.Key);
            }

            if (timedOut == null)
            {
                return;
            }

            var stillOpen = new List<SeatId>();
            foreach (var seat in timedOut)
            {
                testDriverDeadlines.Remove(seat);
                var occupancy = SeatOccupancyState.Open;
                foreach (var entry in session.Roster)
                {
                    if (entry.Seat.Equals(seat))
                    {
                        occupancy = entry.Occupancy;
                        break;
                    }
                }

                if (occupancy == SeatOccupancyState.Open)
                {
                    stillOpen.Add(seat);
                    testDriverSeats.Remove(seat);
                }
            }

            if (stillOpen.Count == 0)
            {
                return;
            }

            foreach (var seat in stillOpen)
            {
                PrivateOnlineMarkers.Fail("test-driver-join-timeout seat=" + seat.Value);
            }

            presenter.SetTestDriverSeats(
                testDriverSeats,
                stillOpen.Count + " test driver(s) failed to join in time (seat"
                + (stillOpen.Count == 1 ? " " : "s ")
                + string.Join(", ", stillOpen.ConvertAll(seat => seat.Value))
                + ") - see the log, or press FILL WITH TEST DRIVERS again to retry.");
        }

        /// <summary>
        /// THIRTEENTH amendment. This host's own spawned children, killed when its session ends -
        /// they hold no reservation and no capability of their own to return with, so there is
        /// nothing for a lingering process to protect and every reason not to leave one running.
        /// </summary>
        private void StopTestDrivers()
        {
            foreach (var process in testDriverProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception)
                {
                    // Already gone, or gone the instant we asked - either way there is nothing left
                    // to clean up and no reason to fail the host's own teardown over it.
                }
                finally
                {
                    process.Dispose();
                }
            }

            testDriverProcesses.Clear();
            testDriverSeats.Clear();
            testDriverDeadlines.Clear();
        }

        private void BeginJoining(PrivateLobbyEntry entry)
        {
            // 8.35t corrective. Cleared on every attempt, not only a failing one: a retry that
            // succeeds must not leave a stale failure standing for a caller - a spawned
            // PrivateSeatDriver among them - that only checks this once, right after pressing.
            LastJoinFailure = null;

            if (!TryReadEndpoint(entry, out var endpoint, out var error))
            {
                LastJoinFailure = error;
                PrivateOnlineMarkers.Fail("lobby-join " + error);
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
                // 8.35t corrective. Dial() already wrote its own reason onto the presenter's
                // feedback line; before this fix that was the ONLY place it went - invisible to
                // any log reader and to a spawned driver, which has no feedback line of its own
                // anyone is watching. A named seat makes this readable out of context.
                LastJoinFailure = "seat=" + joinedSeat.Value + " could not reach the host at " + dialled;
                PrivateOnlineMarkers.Fail("lobby-join " + LastJoinFailure);
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
            StopTestDrivers();
            if (session != null)
            {
                session.Shutdown();
                session = null;
            }

            console = null;
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
            StopTestDrivers();
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
