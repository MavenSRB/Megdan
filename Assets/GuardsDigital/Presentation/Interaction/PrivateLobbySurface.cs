using System;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.35a. The invitation as a player types it: where the host is, and the optional
    /// pre-shared session key that admits them.
    /// </summary>
    /// <remarks>
    /// The 2026-09-01 transport amendment makes this the whole invitation mechanism - an address,
    /// a port, and a key agreed out of band. There is no code service, no account and no directory
    /// to look anything up in, so what is dialed is exactly what was typed.
    /// </remarks>
    public sealed class PrivateLobbyEntry
    {
        public PrivateLobbyEntry(string address, string port, string sessionKey, SeatId seat)
        {
            Address = address ?? string.Empty;
            Port = port ?? string.Empty;
            SessionKey = sessionKey ?? string.Empty;
            Seat = seat;
        }

        public string Address { get; }
        public string Port { get; }
        public string SessionKey { get; }

        /// <summary>The seat a join will claim; undefined until one is picked.</summary>
        public SeatId Seat { get; }
    }

    /// <summary>
    /// Milestone 8.35a. The lobby's view port: it is handed a model and draws it. It is told
    /// nothing else and asked nothing else, so a second surface - a different engine, a console
    /// listing - is a second implementation of this and no other change.
    /// </summary>
    public interface IPrivateLobbyView
    {
        void Render(PrivateLobbyModel model);
    }

    /// <summary>
    /// Milestone 8.35a. The primitive private lobby's presenter: it reads the session contracts,
    /// hands the view a model of them, and turns a pressed control back into a session call.
    /// </summary>
    /// <remarks>
    /// <para>Engine-free by construction, so what a Play Mode journey clicks and what an Edit Mode
    /// fixture reasons about are the same object. It holds a <see cref="PrivateSessionClient"/> or
    /// a <see cref="PrivateSessionHost"/> and nothing else: no match session, no authority, no
    /// command. Every question about who holds which seat is answered by the host, in the roster,
    /// and this only reads the answer - which is the block's fourth acceptance check.</para>
    /// <para>Opening and closing sockets is deliberately not its job. Pressing HOST, JOIN or
    /// RECONNECT raises a request the driver answers, because the driver is the one piece that is
    /// allowed to know a transport exists.</para>
    /// </remarks>
    public sealed class PrivateLobbyPresenter
    {
        private readonly IPrivateLobbyView view;
        private System.Collections.Generic.IReadOnlyList<SeatRosterEntry> localSeats =
            Array.Empty<SeatRosterEntry>();
        private PrivateSessionClient client;
        private PrivateSessionHost host;
        private string feedback = string.Empty;

        /// <summary>
        /// Milestone 8.35z37. The entry form's printed setup, as words. Held rather than
        /// passed to <see cref="Refresh"/> so a redraw triggered by anything else keeps showing it.
        /// </summary>
        private string entryMatchFormatLabel;
        private System.Collections.Generic.ISet<SeatId> testDriverSeats =
            new System.Collections.Generic.HashSet<SeatId>();

        public PrivateLobbyPresenter(IPrivateLobbyView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>Raised when a player asked to become the seatless host of a new session.</summary>
        public event Action<PrivateLobbyEntry> HostRequested;

        /// <summary>Raised when a player asked to join the typed session as the picked seat.</summary>
        public event Action<PrivateLobbyEntry> JoinRequested;

        /// <summary>Raised when a player asked to leave whatever this process is in.</summary>
        public event Action LeaveRequested;

        /// <summary>
        /// Raised when a return has been armed on the session client and needs a new connection
        /// dialed for it.
        /// </summary>
        public event Action ReconnectRequested;

        /// <summary>
        /// THIRTEENTH amendment. Raised when the host asked to fill every still-Open seat with a
        /// spawned test driver. The driver is the one piece allowed to know a process exists.
        /// </summary>
        public event Action FillWithTestDriversRequested;

        /// <summary>
        /// Milestone 8.35z37. Raised on the entry form when the host asked for a different
        /// printed setup. The driver answers by RECOMPOSING the scene, which is the one thing a
        /// presenter must not do: composing a match is scene work, and the format has to be settled
        /// before a listener exists so the admission tuple a client checks describes the match that
        /// will actually be played.
        /// </summary>
        public event Action MatchFormatChangeRequested;

        /// <summary>
        /// 8.35z54. Raised on the entry form when the host named a printed setup DIRECTLY -
        /// the team size or the game length - rather than stepping to the next one.
        /// </summary>
        /// <remarks>
        /// The control that was pressed rides the event, because the driver is what owns the four
        /// printed setups and their composition; the presenter's job is to say which named choice a
        /// player made. Composing a match is scene work, which is why this is an event at all - the
        /// same reason <see cref="MatchFormatChangeRequested"/> is one.
        /// </remarks>
        public event Action<PrivateLobbyControl> MatchSetupChoiceRequested;

        /// <summary>
        /// 8.35z54. Raised when this admitted connection asked for one of the seats it can
        /// see. The seat rides the entry the view built, exactly as an invitation's address does.
        /// </summary>
        public event Action<SeatId> SeatClaimRequested;

        /// <summary>
        /// Milestone 8.35z37. The hero the surface offers on the next
        /// <see cref="PrivateLobbyControl.ChooseHero"/> press. A view sets it from whichever option
        /// the player clicked; the presenter never chooses one.
        /// </summary>
        public string SelectedHeroIdValue { get; set; } = string.Empty;

        /// <summary>The model the view was last handed, for a fixture that reads what was drawn.</summary>
        public PrivateLobbyModel Model { get; private set; }

        /// <summary>
        /// Returns to the entry form, saying why where there is something to say. Any session this
        /// presenter was reading is dropped - the driver owns closing it.
        /// </summary>
        /// <param name="matchFormatLabel">
        /// Milestone 8.35z37. The printed setup this process would host, as words. Given, it
        /// puts the format control on the entry form; omitted, the form is exactly what it was.
        /// </param>
        public void ShowEntry(
            System.Collections.Generic.IReadOnlyList<SeatRosterEntry> seats, string reason,
            string matchFormatLabel = null)
        {
            localSeats = seats ?? Array.Empty<SeatRosterEntry>();
            client = null;
            host = null;
            feedback = reason ?? string.Empty;
            // A retry/leave omits the format because it did not change it. Explicit empty still
            // suppresses setup when composition is unavailable; first-use null stays unspecified.
            entryMatchFormatLabel = matchFormatLabel ?? entryMatchFormatLabel;
            SelectedHeroIdValue = string.Empty;
            Refresh();
        }

        /// <summary>Shows one invited player's own session client.</summary>
        public void AttachClient(PrivateSessionClient session)
        {
            client = session ?? throw new ArgumentNullException(nameof(session));
            host = null;
            feedback = string.Empty;
            Refresh();
        }

        /// <summary>Shows the dedicated seatless host's own view of the seats it serves.</summary>
        public void AttachHost(PrivateSessionHost session, string status)
        {
            host = session ?? throw new ArgumentNullException(nameof(session));
            client = null;
            feedback = status ?? string.Empty;
            testDriverSeats.Clear();
            Refresh();
        }

        /// <summary>
        /// THIRTEENTH amendment. Records which seats this host's own spawned test drivers claimed,
        /// for <see cref="PrivateLobbySeatRow.IsTestDriverSeat"/>, and redraws. The driver calls this
        /// once it knows which seats it spawned into; the presenter itself spawns nothing.
        /// </summary>
        /// <param name="status">
        /// An optional feedback line to show alongside the update - e.g. a partial-spawn count -
        /// without going through <see cref="AttachHost"/>, which would drop this same tracking.
        /// </param>
        public void SetTestDriverSeats(
            System.Collections.Generic.IEnumerable<SeatId> seats, string status = null)
        {
            testDriverSeats = new System.Collections.Generic.HashSet<SeatId>(
                seats ?? Array.Empty<SeatId>());
            if (status != null)
            {
                feedback = status;
            }

            Refresh();
        }

        /// <summary>Rebuilds the model from the session as it stands and redraws.</summary>
        public void Refresh()
        {
            Model = client != null
                ? PrivateLobbyModel.ForClient(client)
                : host != null
                    ? PrivateLobbyModel.ForHost(host, feedback, testDriverSeats)
                    : PrivateLobbyModel.Entry(localSeats, feedback, entryMatchFormatLabel);
            view.Render(Model);
        }

        /// <summary>
        /// Answers a pressed control.
        /// </summary>
        /// <remarks>
        /// A control the current model does not offer is ignored rather than obeyed: a surface that
        /// kept a stale button - the case milestone 8.34 makes terminal - must not be able to reach
        /// the session through it.
        /// </remarks>
        /// <returns>Whether the press was one the lobby was offering.</returns>
        public bool Press(PrivateLobbyControl control, PrivateLobbyEntry entry)
        {
            if (Model == null || !Model.Offers(control))
            {
                return false;
            }

            switch (control)
            {
                case PrivateLobbyControl.Host:
                    HostRequested?.Invoke(entry);
                    return true;

                // 8.35z54. JOINING NAMES NO SEAT. The spec is "they join the server, see open
                // seats, they pick a seat", so the seat is chosen from the roster once this player
                // is in the session and can see which chairs are free - the refusal that used to
                // stand here asked a player to choose blind from a table they had not been shown.
                case PrivateLobbyControl.Join:
                    JoinRequested?.Invoke(entry);
                    return true;

                case PrivateLobbyControl.ClaimSeat:
                    if (entry == null || !entry.Seat.IsDefined)
                    {
                        feedback = "Press the seat you want to take.";
                        Refresh();
                        return true;
                    }

                    if (client == null || !client.TryClaimSeat(entry.Seat))
                    {
                        feedback = "That seat is not free.";
                        Refresh();
                        return true;
                    }

                    SeatClaimRequested?.Invoke(entry.Seat);
                    Refresh();
                    return true;

                case PrivateLobbyControl.HostUndo:
                    if (host == null || !host.TryUndoLobbyStep())
                    {
                        feedback = "There is no lobby step left to take back.";
                    }

                    Refresh();
                    return true;

                case PrivateLobbyControl.ChooseTwoVsTwo:
                case PrivateLobbyControl.ChooseThreeVsThree:
                case PrivateLobbyControl.ChooseShortGame:
                case PrivateLobbyControl.ChooseLongGame:
                    MatchSetupChoiceRequested?.Invoke(control);
                    return true;

                case PrivateLobbyControl.Ready:
                case PrivateLobbyControl.Unready:
                    client?.TrySetReadiness(control == PrivateLobbyControl.Ready);
                    Refresh();
                    return true;

                case PrivateLobbyControl.Leave:
                    if (client != null && client.State == PrivateSessionClientState.Seated)
                    {
                        client.SendGoodbye();
                    }

                    LeaveRequested?.Invoke();
                    return true;

                case PrivateLobbyControl.Reconnect:
                    if (client == null || !client.BeginReconnect())
                    {
                        feedback = "This seat's return is no longer available.";
                        Refresh();
                        return true;
                    }

                    ReconnectRequested?.Invoke();
                    Refresh();
                    return true;

                case PrivateLobbyControl.FillWithTestDrivers:
                    FillWithTestDriversRequested?.Invoke();
                    return true;

                // Milestone 8.35z37. The pick is SENT and nothing local changes: whether
                // the seat then holds that hero is the roster's answer, exactly as readiness is.
                // A surface that lit the hero on this press would be showing one process's hope.
                case PrivateLobbyControl.ChooseHero:
                    if (client == null || string.IsNullOrWhiteSpace(SelectedHeroIdValue))
                    {
                        feedback = "Pick a hero from the list first.";
                        Refresh();
                        return true;
                    }

                    client.TryChooseHero(SelectedHeroIdValue);
                    Refresh();
                    return true;

                case PrivateLobbyControl.SetMatchFormat:
                    MatchFormatChangeRequested?.Invoke();
                    return true;

                default:
                    return false;
            }
        }
    }
}
