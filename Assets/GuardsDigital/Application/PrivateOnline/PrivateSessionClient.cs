using System;
using System.Collections.Generic;
using System.Globalization;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>The client half of <see cref="IPrivateSessionOutbox"/>: one connection, no ordinal.</summary>
    public interface IPrivateSessionClientOutbox
    {
        void Send(byte[] frame);
        void Close();
    }

    /// <summary>Where a client's session establishment stands.</summary>
    public enum PrivateSessionClientState
    {
        None = 0,

        /// <summary>The transport reported the connection open; the host speaks first.</summary>
        AwaitingChallenge = 1,

        /// <summary>Hello sent; the host decides admission.</summary>
        AwaitingWelcome = 2,

        /// <summary>Admitted and the claim sent; the host decides the seat.</summary>
        AwaitingSeat = 3,

        /// <summary>The target seat is held. 8.31's terminal success.</summary>
        Seated = 4,

        /// <summary>The host refused; <see cref="PrivateSessionClient.RejectionReason"/> says why.</summary>
        Rejected = 5,

        /// <summary>The connection ended without a refusal.</summary>
        Disconnected = 6,

        /// <summary>The host's own stream violated the protocol; nothing further is trusted.</summary>
        Faulted = 7,

        /// <summary>
        /// Milestone 8.33. The one recoverable failure state: this seat detected a gap in the
        /// host's ordered stream, its view is known stale, and it has asked for one bounded
        /// current-projection resync. Still seated, still connected; it neither submits nor
        /// trusts its projection until the next complete projection returns it to
        /// <see cref="Seated"/>.
        /// </summary>
        Resynchronizing = 8,

        /// <summary>
        /// Milestone 8.34. The authority this seat was playing under is gone: the connection ended
        /// while the seat was held, without the host saying Goodbye and without refusing anything.
        /// </summary>
        /// <remarks>
        /// <para>Terminal, in the sense the decision record's host-loss row means it: nothing is
        /// submitted from here, no continuation is claimed, no authority migrates, and a surface
        /// showing this state carries no control a player could still press. What the seat last
        /// saw is kept, because a player is entitled to see the position they were in; what they
        /// cannot do is act on it.</para>
        /// <para>Leaving this state is not resuming from it. <see cref="PrivateSessionClient
        /// .BeginReconnect"/> starts a complete admission handshake again, and what comes back is
        /// a whole freshly encoded projection rather than any part of what was held here.</para>
        /// </remarks>
        HostLost = 9,

        /// <summary>
        /// 8.35z54. Admitted with no seat named yet: the connection is established, the
        /// build was accepted, and the roster is arriving - but which seat this player will hold is
        /// still theirs to choose.
        /// </summary>
        /// <remarks>
        /// <para><b>This is the state the user's flow spec is made of.</b> "They join the server,
        /// see open seats, they pick a seat" is only expressible if being IN the session and
        /// HOLDING a seat are two different things. Before this, a client was constructed around
        /// one seat and claimed it inside its own handshake, so the seat had to be chosen on the
        /// entry form - by a player who could not yet see which seats were free, which is the
        /// surface the spec removes.</para>
        /// <para>It costs no wire change at all. The host has always sent the full roster on the
        /// Welcome and has always broadcast roster updates to every connection past
        /// <c>AwaitingHello</c> rather than only to seated ones, so an admitted-but-unclaimed
        /// connection can already see every seat and its occupancy. What was missing was a client
        /// willing to WAIT there instead of claiming immediately.</para>
        /// </remarks>
        Admitted = 10
    }

    /// <summary>
    /// Milestone 8.31. One client's session state machine: answer the challenge, present the
    /// tuple, claim the one configured seat, and hold it.
    /// </summary>
    /// <remarks>
    /// Transport-neutral and single-threaded by the same pump contract as the host. The seat it
    /// claims is fixed at construction - a client is one player sitting down at one place - and a
    /// grant naming any other seat is a fault, not a fallback.
    /// </remarks>
    public sealed class PrivateSessionClient
    {
        /// <summary>
        /// A local press receives ordinary waiting feedback first, then distinct delayed feedback
        /// after this bounded interval. This clock affects presentation only, never authority.
        /// </summary>
        public const long DelayedHostResponseThresholdTicks = 2L * TimeSpan.TicksPerSecond;

        private readonly PrivateSessionIdentity identity;
        // 8.35z54. No longer readonly: a client may arrive seatless and name its seat once
        // it can see the table. It is written exactly once, by `TryClaimSeat`, and only from
        // `Admitted` - so a seated client's seat is as fixed as it has ever been.
        private SeatId targetSeat;
        private readonly string sessionKey;
        private readonly IPrivateSessionClientOutbox outbox;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly Func<long> timeSource;
        private readonly List<SeatRosterEntry> roster = new List<SeatRosterEntry>();
        private PrivateSessionSecureChannel channel;
        private long gameplaySendSequence;
        private long gameplayReceiveSequence;

        /// <summary>
        /// Milestone 8.35r. The match log as this client holds it. Mutable because the host extends
        /// it a few lines at a time; replaced wholesale only where a rewind or an arrival makes the
        /// held one unextendable.
        /// </summary>
        private readonly List<GuardsDigital.Core.Diagnostics.MatchLogEntry> matchLog =
            new List<GuardsDigital.Core.Diagnostics.MatchLogEntry>();
        private string awaitingCommandResultId;
        private string awaitingAgreementResultId;
        private string awaitingUndoResultId;
        private long awaitingProjectionCorrelation;
        private bool awaitingReconnectProjection;
        private long waitingStartedTicks;

        /// <summary>
        /// Milestone 8.34. The capability the host issued with this seat's grant, held here and
        /// nowhere else. It is deliberately not a property: nothing outside this machine needs to
        /// read it, and a surface that could read it could log it.
        /// </summary>
        private string reconnectCapability = string.Empty;

        /// <summary>
        /// Milestone 8.35k. Where this seat's capability is kept between connections, so a process
        /// that died has a road back. Read by this machine and by nothing else.
        /// </summary>
        private readonly IRetainedSeatCapabilityStore retainedCapabilities;
        private bool hasHeldSeat;

        /// <summary>
        /// Milestone 8.34. Whether the host has named a refusal on this connection. A connection
        /// the host refused and closed ended because it was told no, which is not the same event
        /// as the host disappearing, however similar the socket looks afterwards.
        /// </summary>
        private bool refusalReceived;

        public PrivateSessionClient(
            PrivateSessionIdentity identity,
            SeatId targetSeat,
            string sessionKey,
            IPrivateSessionClientOutbox outbox,
            IPrivateSessionNonceSource nonceSource = null,
            Func<long> timeSource = null,
            IRetainedSeatCapabilityStore retainedCapabilities = null)
        {
            // Milestone 8.35k. The road back, and the only new dependency the corrective adds. A
            // client with no store behaves exactly as it did before: it retains nothing and finds
            // nothing, so a fixture that does not care is unaffected.
            this.retainedCapabilities =
                retainedCapabilities ?? new InMemoryRetainedSeatCapabilityStore();
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));

            // 8.35z54. An UNDEFINED seat is now legitimate and means "admit me, I will
            // pick when I can see the table" - the user's flow spec. Every existing caller passes a
            // defined seat and behaves exactly as before, claiming inside its own handshake; only a
            // client that deliberately arrives seatless waits in
            // `PrivateSessionClientState.Admitted` for `TryClaimSeat`.
            this.targetSeat = targetSeat;
            this.sessionKey = sessionKey ?? string.Empty;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.nonceSource = nonceSource;
            this.timeSource = timeSource ?? UtcNowTicks;
        }

        public PrivateSessionClientState State { get; private set; }

        /// <summary>
        /// Milestone 8.108. The name this player typed, sent with every seat claim and shown on
        /// that seat's roster row. Empty sends no name, and the lobby shows the seat's own label.
        /// </summary>
        public string PlayerName
        {
            get => playerName;
            set => playerName = PrivateLobbyEntryText.CleanPlayerName(value);
        }

        private string playerName = string.Empty;

        public SeatId TargetSeat => targetSeat;

        /// <summary>Why the host refused, when <see cref="State"/> is <c>Rejected</c>.</summary>
        public SessionRejectionReason RejectionReason { get; private set; }

        /// <summary>The host's bounded rejection detail, empty otherwise.</summary>
        public string RejectionDetail { get; private set; } = string.Empty;

        /// <summary>The host's opaque session correlation, known from admission on.</summary>
        public string SessionCorrelation { get; private set; } = string.Empty;

        /// <summary>The granted seat's identity row, known from seating on.</summary>
        public SeatRosterEntry GrantedSeat { get; private set; }

        /// <summary>The latest roster the host published.</summary>
        public IReadOnlyList<SeatRosterEntry> Roster => roster;

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's coin as the host published it, or
        /// <see cref="TeamSide.None"/> in a session running no selection.
        /// </summary>
        /// <remarks>
        /// It is the ORDER, not a rules answer: with it and the roster's own
        /// <see cref="SeatRosterEntry.HasChosenHero"/> flags a surface can say whose pick it is
        /// without asking the host a second time. The host still owns the rule - a pick out of turn
        /// is refused there - so a surface reading this is showing the order, never enforcing it.
        /// </remarks>
        public TeamSide FirstPickingTeam { get; private set; }

        /// <summary>
        /// Milestone 8.35a. Whether this session has left the lobby, learned from the host's
        /// announcement or - for a client that was not there to hear it - from the admission
        /// answer it re-ran on the way back.
        /// </summary>
        public bool HasMatchStarted { get; private set; }

        /// <summary>
        /// Milestone 8.35a. Whether this seat is ready, as the host's own roster states it.
        /// </summary>
        /// <remarks>
        /// Read from the roster rather than remembered from the request, so what a surface shows
        /// is the public answer six people share rather than one process's belief about it. A
        /// client that has asked and not yet been answered reads the previous value, which is the
        /// truth at that moment.
        /// </remarks>
        public bool IsReady
        {
            get
            {
                foreach (var entry in roster)
                {
                    if (entry.Seat.Equals(targetSeat))
                    {
                        return entry.IsReady;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Milestone 8.32. This seat's latest owner-scoped projection as the host pushed it, or
        /// <c>null</c> before the first push. Everything a surface may show or submit is in here;
        /// nothing else about the match ever reaches this process.
        /// </summary>
        public NetworkSeatProjectionRecord LatestProjection { get; private set; }

        /// <summary>
        /// Monotonic count of complete projections accepted from the host. Unlike the authoritative
        /// match version this also moves when public session state, such as a teammate's agreement
        /// statement, is republished at the same gameplay version.
        /// </summary>
        public long ProjectionRevision { get; private set; }

        /// <summary>
        /// Milestone 8.35r. The seat-public match log this client currently holds, oldest first,
        /// bounded to the retained window.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog => matchLog;

        /// <summary>Monotonic count of match-log updates applied.</summary>
        public long MatchLogRevision { get; private set; }

        /// <summary>Milestone 8.32. The host's answer to this client's newest submission.</summary>
        public NetworkSubmissionResultMessage LastSubmissionResult { get; private set; }

        /// <summary>Milestone 8.33. The host's answer to this client's newest agreement statement.</summary>
        public NetworkAgreementResultMessage LastAgreementResult { get; private set; }

        /// <summary>Milestone 8.35f. The host's answer to this client's newest rewind.</summary>
        public NetworkUndoResultMessage LastUndoResult { get; private set; }

        /// <summary>The newest bounded session feedback a match surface must say.</summary>
        public string MatchFeedback { get; private set; } = string.Empty;

        /// <summary>Whether an accepted local press is still awaiting the host's named answer.</summary>
        public bool IsWaitingForHost =>
            !string.IsNullOrEmpty(awaitingCommandResultId) ||
            !string.IsNullOrEmpty(awaitingAgreementResultId) ||
            !string.IsNullOrEmpty(awaitingUndoResultId);

        /// <summary>
        /// Whether one in-flight press has crossed the player-facing delayed threshold. The
        /// injected clock keeps the boundary deterministic in tests; this state never enters a
        /// command, projection, snapshot, or host decision.
        /// </summary>
        public bool IsHostResponseDelayed =>
            IsWaitingForHost && timeSource() - waitingStartedTicks >=
                DelayedHostResponseThresholdTicks;

        /// <summary>
        /// Milestone 8.33. Stale or replayed host frames set aside unapplied - each one a
        /// duplicate the ordered stream already delivered. A discard is convergence, not damage.
        /// </summary>
        public int DiscardedFrameCount { get; private set; }

        /// <summary>
        /// Milestone 8.33. Gaps healed by the very frame that revealed them: a complete projection
        /// that arrived past a missing frame is adopted whole, because a newer complete view is
        /// exactly what a resync would have answered with.
        /// </summary>
        public int RecoveredProjectionGapCount { get; private set; }

        /// <summary>Milestone 8.33. How many bounded resync requests this client has sent.</summary>
        public int ResyncRequestCount { get; private set; }

        /// <summary>
        /// Milestone 8.34. The connection epoch this client speaks under, as the host's grant
        /// named it. Every gameplay envelope it sends carries this, and it refuses to read one
        /// that does not.
        /// </summary>
        public int ConnectionEpoch { get; private set; } =
            PrivateSessionProtocol.InitialConnectionEpoch;

        /// <summary>
        /// Milestone 8.34. Whether this machine is on its way back to a seat it already held, so a
        /// surface can say "reconnecting" rather than "joining" while the handshake runs.
        /// </summary>
        public bool IsReconnecting { get; private set; }

        /// <summary>
        /// Milestone 8.34. Whether a return is even possible: this client held a seat, was issued
        /// a capability for it, and is in a state a fresh admission may be started from.
        /// </summary>
        /// <remarks>
        /// A refusal is not one of those states. A host that said no - to the tuple, to the key,
        /// or to the reclaim itself - has answered; asking again with the same answer to give is
        /// not a recovery, and <see cref="PrivateSessionClientState.Faulted"/> is worse, because
        /// nothing that stream said is trusted any more.
        /// </remarks>
        public bool CanAttemptReconnect =>
            reconnectCapability.Length > 0 &&
            (State == PrivateSessionClientState.HostLost ||
                State == PrivateSessionClientState.Disconnected);

        /// <summary>
        /// Milestone 8.34. Arms a complete second admission for the seat this client already held,
        /// to be driven by a new transport connection: the machine returns to its opening state,
        /// its sealed channel and both sequence counters are dropped because the connection they
        /// belonged to is gone, and the claim it will make is a reclaim carrying the capability.
        /// </summary>
        /// <returns>Whether a return could be armed at all.</returns>
        /// <remarks>
        /// The last projection is kept until the host replaces it. It is not carried into the new
        /// session as state - the host sends a complete projection and this machine adopts that
        /// whole - it is simply the last thing this player was shown, and blanking the board on
        /// the way back would tell them less than leaving it.
        /// </remarks>
        public bool BeginReconnect()
        {
            if (!CanAttemptReconnect)
            {
                return false;
            }

            State = PrivateSessionClientState.None;
            IsReconnecting = true;
            awaitingReconnectProjection = true;
            channel = null;
            gameplaySendSequence = 0;
            gameplayReceiveSequence = 0;
            RejectionReason = SessionRejectionReason.None;
            RejectionDetail = string.Empty;
            refusalReceived = false;
            ClearWaitingForHost();
            MatchFeedback = string.Empty;
            return true;
        }

        /// <summary>
        /// Reports that a transport could not open the connection armed by
        /// <see cref="BeginReconnect"/>. The owner-private capability is retained, while the
        /// half-started handshake is discarded so the player may try another dial.
        /// </summary>
        public void OnReconnectTransportFailed()
        {
            if (!IsReconnecting ||
                (State != PrivateSessionClientState.None &&
                    State != PrivateSessionClientState.AwaitingChallenge))
            {
                return;
            }

            State = PrivateSessionClientState.Disconnected;
            IsReconnecting = false;
            awaitingReconnectProjection = false;
            channel = null;
            gameplaySendSequence = 0;
            gameplayReceiveSequence = 0;
            ClearWaitingForHost();
            MatchFeedback = "Could not reach the host. Try reconnecting.";
        }

        public void OnConnected()
        {
            if (State != PrivateSessionClientState.None)
            {
                return;
            }

            State = PrivateSessionClientState.AwaitingChallenge;
        }

        public void OnFrame(SessionFrame frame)
        {
            if (State == PrivateSessionClientState.Rejected ||
                State == PrivateSessionClientState.Disconnected ||
                State == PrivateSessionClientState.Faulted ||
                State == PrivateSessionClientState.HostLost)
            {
                return;
            }

            // Milestone 8.32. The client's half of the seal discipline: once it has answered the
            // challenge on a keyed session, only sealed frames and plaintext Reject or Goodbye are
            // trusted from the host - anything else is an injection or a broken peer.
            if (frame.Kind == SessionMessageKind.Sealed)
            {
                if (channel == null || !channel.TryUnseal(frame.Payload, out frame))
                {
                    Fault();
                    return;
                }
            }
            else if (channel != null &&
                frame.Kind != SessionMessageKind.Reject &&
                frame.Kind != SessionMessageKind.Goodbye)
            {
                Fault();
                return;
            }

            if (frame.Kind == SessionMessageKind.Reject)
            {
                if (!PrivateSessionFrameCodec.TryDecodeReject(frame.Payload, out var reject))
                {
                    Fault();
                    return;
                }

                RejectionReason = reject.Reason;
                RejectionDetail = reject.Detail;
                refusalReceived = true;
                IsReconnecting = false;
                awaitingReconnectProjection = false;
                ClearWaitingForHost();
                if (State != PrivateSessionClientState.Seated)
                {
                    // A refusal after seating - a refused seat change - is recorded but does not
                    // unseat the client; the host keeps the seat map unchanged and so does this.
                    State = PrivateSessionClientState.Rejected;
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.Goodbye)
            {
                State = PrivateSessionClientState.Disconnected;
                IsReconnecting = false;
                awaitingReconnectProjection = false;
                ClearWaitingForHost();
                return;
            }

            if (frame.Kind == SessionMessageKind.RosterUpdate)
            {
                // 8.35z54. `Admitted` belongs on this list and belongs FIRST: a connection
                // waiting to be shown the open seats is precisely the one that most needs the
                // roster, and the host has always broadcast to every connection past AwaitingHello.
                if (State != PrivateSessionClientState.Admitted &&
                    State != PrivateSessionClientState.AwaitingSeat &&
                    State != PrivateSessionClientState.Seated &&
                    State != PrivateSessionClientState.Resynchronizing)
                {
                    Fault();
                    return;
                }

                if (!PrivateSessionFrameCodec.TryDecodeRosterUpdate(frame.Payload, out var update))
                {
                    Fault();
                    return;
                }

                roster.Clear();
                roster.AddRange(update.Roster);
                FirstPickingTeam = update.FirstPickingTeam;
                return;
            }

            // Milestone 8.35a. The lobby is over. It is a statement about the seats, so only a
            // seated connection is sent one; hearing it in any other state means this stream is
            // not the stream this machine thinks it is.
            if (frame.Kind == SessionMessageKind.MatchStarted)
            {
                if (State != PrivateSessionClientState.Seated &&
                    State != PrivateSessionClientState.Resynchronizing)
                {
                    Fault();
                    return;
                }

                if (!PrivateSessionFrameCodec.TryDecodeMatchStarted(frame.Payload))
                {
                    Fault();
                    return;
                }

                HasMatchStarted = true;
                return;
            }

            switch (State)
            {
                case PrivateSessionClientState.AwaitingChallenge:
                    if (frame.Kind != SessionMessageKind.Challenge ||
                        !PrivateSessionFrameCodec.TryDecodeChallenge(frame.Payload, out var challenge))
                    {
                        Fault();
                        return;
                    }

                    // The Hello itself is plaintext - the host cannot unseal before it has
                    // verified the proof inside it - and everything after it travels sealed on a
                    // keyed session, both ways.
                    outbox.Send(PrivateSessionFrameCodec.HelloFrame(new SessionHelloMessage(
                        identity, PrivateSessionKeyProof.Compute(challenge.Nonce, sessionKey))));
                    if (sessionKey.Length > 0 && challenge.Nonce.Length > 0)
                    {
                        channel = PrivateSessionSecureChannel.ForClient(sessionKey, challenge.Nonce);
                    }

                    State = PrivateSessionClientState.AwaitingWelcome;
                    return;

                case PrivateSessionClientState.AwaitingWelcome:
                    if (frame.Kind != SessionMessageKind.Welcome ||
                        !PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var welcome))
                    {
                        Fault();
                        return;
                    }

                    SessionCorrelation = welcome.SessionCorrelation;
                    roster.Clear();
                    roster.AddRange(welcome.Roster);
                    // Milestone 8.35z37. p.22's coin, so a client joining mid-selection can
                    // draw the pick order from its very first roster rather than after a pick.
                    FirstPickingTeam = welcome.FirstPickingTeam;
                    // Milestone 8.35a. The admission answer says which phase the session is in, so
                    // a returning player knows before it claims anything that the lobby is over.
                    HasMatchStarted = welcome.MatchStarted;
                    // 8.35z54. An ordinary first-time client that arrived without naming a seat
                    // stops here and waits for the player to choose one of the OPEN seats it can
                    // now see. Milestone 8.99 adds the one exception the real relaunch path needs:
                    // if exactly one RESERVED row has a capability retained on this machine, this
                    // is that seat's returning owner. Select it locally and continue through the
                    // existing authenticated reclaim below. No capability means an ordinary
                    // seatless join; more than one means multiple local players are ambiguous and
                    // must not be guessed at.
                    if (!targetSeat.IsDefined && !TrySelectRetainedReservedSeat())
                    {
                        State = PrivateSessionClientState.Admitted;
                        return;
                    }

                    // Milestone 8.34. A client that already held this seat asks for it back by
                    // presenting the capability, because an ordinary claim of a reserved seat is
                    // refused - and must stay refused, or a reservation would be anybody's.
                    // Milestone 8.35k. The road back for a process that died. The store is consulted
                    // here rather than at the lobby's entry form because the session is not
                    // identified until this message arrives. Holding a capability is not authority:
                    // the host still requires the seat Reserved, a non-empty stored capability and a
                    // constant-time match, and answers both wrong-capability cases identically.
                    //
                    // ARMED ONLY AGAINST A SEAT THE HOST SAYS IS HELD, and that condition is
                    // load-bearing rather than defensive. `SessionCorrelation` is deterministic over
                    // the content tuple - its own record says two processes composed from the same
                    // content derive the same value, drawing no entropy - so it identifies a BUILD
                    // and not a session, and a stored entry outlives the session that made it. Arming
                    // on the entry alone therefore made every SECOND session on a machine send a
                    // reclaim the new host had never minted, refused `ReconnectNotAuthorized` and
                    // closed: a player who finished one game could never join another. The roster in
                    // this very message is the evidence that IS about this session, and a seat nobody
                    // is being held for is answered by an ordinary claim.
                    if (!IsReconnecting && reconnectCapability.Length == 0)
                    {
                        var heldForItsPlayer = false;
                        foreach (var entry in roster)
                        {
                            if (entry.Seat.Equals(targetSeat))
                            {
                                heldForItsPlayer = entry.Occupancy == SeatOccupancyState.Reserved;
                                break;
                            }
                        }

                        var retained = heldForItsPlayer
                            ? retainedCapabilities.Find(SessionCorrelation, targetSeat)
                            : string.Empty;
                        if (retained.Length > 0)
                        {
                            reconnectCapability = retained;
                            IsReconnecting = true;
                        }
                        else if (!heldForItsPlayer)
                        {
                            // Nobody is being held for this seat, so anything stored under it belongs
                            // to a session that has ended. Dropping it keeps one stale secret from
                            // sitting on disk for the life of the build.
                            retainedCapabilities.Forget(SessionCorrelation, targetSeat);
                        }
                    }

                    Send(IsReconnecting && reconnectCapability.Length > 0
                        ? PrivateSessionFrameCodec.ReclaimSeatFrame(
                            new SeatReclaimMessage(targetSeat, reconnectCapability))
                        : PrivateSessionFrameCodec.ClaimSeatFrame(new SeatClaimMessage(targetSeat, PlayerName)));
                    State = PrivateSessionClientState.AwaitingSeat;
                    return;

                case PrivateSessionClientState.AwaitingSeat:
                    if (frame.Kind != SessionMessageKind.SeatGranted ||
                        !PrivateSessionFrameCodec.TryDecodeSeatGranted(frame.Payload, out var granted))
                    {
                        Fault();
                        return;
                    }

                    if (!granted.Seat.Seat.Equals(targetSeat))
                    {
                        Fault();
                        return;
                    }

                    GrantedSeat = granted.Seat;
                    // Milestone 8.34. The grant is where this connection learns which epoch it
                    // speaks under and how it would come back; both are the host's to say, and
                    // both arrive here and nowhere else.
                    ConnectionEpoch = granted.ConnectionEpoch;
                    lastReadinessVersion = -1;
                    if (granted.ReconnectCapability.Length > 0)
                    {
                        reconnectCapability = granted.ReconnectCapability;
                        // Milestone 8.35k. Retained here, where the capability already is, so it
                        // never has to become readable from outside this machine to be saved.
                        retainedCapabilities.Retain(
                            SessionCorrelation, targetSeat, granted.ReconnectCapability);
                    }

                    hasHeldSeat = true;
                    if (awaitingReconnectProjection)
                    {
                        // A grant proves who owns the seat, not what match state that seat may now
                        // act on. The retained pre-disconnect projection stays visible but disabled
                        // until the complete projection queued after the grant is adopted.
                        State = PrivateSessionClientState.Resynchronizing;
                    }
                    else
                    {
                        IsReconnecting = false;
                        State = PrivateSessionClientState.Seated;
                    }
                    return;

                case PrivateSessionClientState.Seated:
                case PrivateSessionClientState.Resynchronizing:
                    HandleGameplayFrame(frame);
                    return;

                default:
                    Fault();
                    return;
            }
        }

        /// <summary>
        /// Selects the only reserved seat for which this machine retained the host-issued owner
        /// capability. The capability remains private and the host still authenticates it.
        /// </summary>
        private bool TrySelectRetainedReservedSeat()
        {
            SeatId retainedSeat = default;
            var retainedCapability = string.Empty;
            foreach (var entry in roster)
            {
                if (entry.Occupancy != SeatOccupancyState.Reserved)
                {
                    continue;
                }

                var candidate = retainedCapabilities.Find(SessionCorrelation, entry.Seat);
                if (candidate.Length == 0 ||
                    candidate.Length > PrivateSessionProtocol.MaxReconnectCapabilityChars)
                {
                    continue;
                }

                if (retainedSeat.IsDefined)
                {
                    return false;
                }

                retainedSeat = entry.Seat;
                retainedCapability = candidate;
            }

            if (!retainedSeat.IsDefined)
            {
                return false;
            }

            targetSeat = retainedSeat;
            reconnectCapability = retainedCapability;
            IsReconnecting = true;
            awaitingReconnectProjection = HasMatchStarted;
            return true;
        }

        /// <summary>
        /// Milestone 8.35r. Adopts the match log carried by a projection this client just accepted.
        /// </summary>
        /// <remarks>
        /// <para>An absolute replacement, and that is the whole of its correctness: the frame it
        /// arrived in has already passed the ordered-stream discipline above, so a stale or
        /// duplicate copy never reaches here, and a window that arrives is by definition the log as
        /// the authority holds it right now. A rewind therefore needs no special case - the shorter
        /// window simply replaces the longer one, which is what makes undone lines "cease to
        /// exist" rather than have to be hunted down and removed.</para>
        /// </remarks>
        private void ApplyMatchLog(IReadOnlyList<NetworkMatchLogRow> rows)
        {
            matchLog.Clear();
            foreach (var row in rows)
            {
                matchLog.Add(NetworkMatchLogCodec.ToEntry(row));
            }

            MatchLogRevision++;
        }

        /// <summary>
        /// Milestone 8.33. The one ordered-stream discipline for every gameplay frame: a stale
        /// sequence is a duplicate or replay and is discarded unapplied; the next sequence is
        /// applied; and a gap is an explicit recovery, never a silent continuation - a complete
        /// projection past the gap converges on its own, while a result past the gap means the
        /// projection this seat holds is stale, so it asks for one bounded resync and stops
        /// acting until a complete projection arrives.
        /// </summary>
        private void HandleGameplayFrame(SessionFrame frame)
        {
            if (frame.Kind == SessionMessageKind.SeatProjection)
            {
                if (!NetworkMessageCodec.TryDecodeSeatProjection(frame.Payload, out var projection))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(projection.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    // A stale copy is set aside unapplied - but a stale copy carrying THIS seat's
                    // awaited correlation still proves the host answered the in-flight press, and
                    // it is the only copy that ever will: the host echoes a correlation exactly
                    // once. Under a reordered delivery the broadcast that overtook this frame has
                    // already been adopted, so the view the latch was protecting is at least as
                    // new as the answer it awaited - discarding the release with the frame would
                    // leave the latch closed forever and this one seat silently unable to act.
                    if (IsWaitingForHost && awaitingProjectionCorrelation != 0 &&
                        projection.Envelope.Correlation == awaitingProjectionCorrelation)
                    {
                        ClearWaitingForHost();
                    }

                    DiscardedFrameCount++;
                    return;
                }

                if (!NetworkSeatProjectionCodec.TryDecode(projection.ProjectionPayload, out var record))
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Gap)
                {
                    RecoveredProjectionGapCount++;
                }

                gameplayReceiveSequence = projection.Envelope.Sequence;
                LatestProjection = record;
                ProjectionRevision++;
                ApplyMatchLog(projection.MatchLog);
                if (IsWaitingForHost && awaitingProjectionCorrelation != 0 &&
                    projection.Envelope.Correlation == awaitingProjectionCorrelation)
                {
                    // The host echoes this submission (or its recovery request) on the initiating
                    // seat's complete projection. A zero-correlation broadcast from another seat
                    // can be newer and still says nothing about input queued in this direction.
                    ClearWaitingForHost();
                }
                if ((LastSubmissionResult == null || record.Version > LastSubmissionResult.Version) &&
                    (LastAgreementResult == null || record.Version > LastAgreementResult.Version) &&
                    (LastUndoResult == null || record.Version >= LastUndoResult.Version))
                {
                    MatchFeedback = string.Empty;
                }
                if (awaitingReconnectProjection)
                {
                    awaitingReconnectProjection = false;
                    IsReconnecting = false;
                    State = PrivateSessionClientState.Seated;
                }
                else if (State == PrivateSessionClientState.Resynchronizing)
                {
                    State = PrivateSessionClientState.Seated;
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.SubmissionResult)
            {
                if (!NetworkMessageCodec.TryDecodeSubmissionResult(frame.Payload, out var result))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(result.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                gameplayReceiveSequence = result.Envelope.Sequence;
                LastSubmissionResult = result;
                MatchFeedback = result.Accepted ? string.Empty :
                    NetworkMatchFeedback.PlayerDetail(result.SessionRejectionReason, result.Detail, result.RejectionReason.ToString());
                if (string.Equals(awaitingCommandResultId, result.SubmissionId, StringComparison.Ordinal))
                {
                    if (result.Accepted ||
                        result.RejectionReason == SeatSubmissionRejectionReason.StaleProjection)
                    {
                        // Keep the submission latch. Its correlated complete projection releases it.
                    }
                    else
                    {
                        ClearCommandWait();
                    }
                }
                if (reading == EnvelopeReading.Gap)
                {
                    BeginResync();
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.UndoResult)
            {
                if (!NetworkMessageCodec.TryDecodeUndoResult(frame.Payload, out var undo))
                {
                    Fault();
                    return;
                }

                var undoReading = ReadEnvelope(undo.Envelope);
                if (undoReading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (undoReading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                gameplayReceiveSequence = undo.Envelope.Sequence;
                LastUndoResult = undo;
                MatchFeedback = undo.Accepted ? string.Empty :
                    NetworkMatchFeedback.PlayerDetail(undo.SessionRejectionReason, undo.Detail, undo.RejectionReason.ToString());
                if (string.Equals(awaitingUndoResultId, undo.SubmissionId, StringComparison.Ordinal))
                {
                    if (undo.Accepted ||
                        undo.RejectionReason == SeatSubmissionRejectionReason.StaleProjection)
                    {
                        // Keep the latch. Its correlated complete projection releases it, exactly
                        // as an accepted command's does - a rewind changes the board it is about.
                    }
                    else
                    {
                        ClearUndoWait();
                    }
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.AgreementResult)
            {
                if (!NetworkMessageCodec.TryDecodeAgreementResult(frame.Payload, out var result))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(result.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                gameplayReceiveSequence = result.Envelope.Sequence;
                LastAgreementResult = result;
                MatchFeedback = result.Accepted ? string.Empty :
                    (string.IsNullOrWhiteSpace(result.Detail)
                        ? result.RejectionReason.ToString()
                        : result.Detail);
                if (string.Equals(awaitingAgreementResultId, result.SubmissionId, StringComparison.Ordinal))
                {
                    if (result.Accepted ||
                        result.RejectionReason ==
                            TeamAgreementSubmissionRejectionReason.StaleProjection ||
                        result.RejectionReason ==
                            TeamAgreementSubmissionRejectionReason.StaleProposal)
                    {
                        // Keep the statement latch. Its correlated complete projection releases it.
                    }
                    else
                    {
                        ClearAgreementWait();
                    }
                }
                if (reading == EnvelopeReading.Gap)
                {
                    BeginResync();
                }

                return;
            }

            Fault();
        }

        /// <summary>
        /// Enters the recoverable state and asks for one bounded resync. One request per episode:
        /// a second gap while one is outstanding adds nothing the first answer will not heal.
        /// </summary>
        private void BeginResync()
        {
            if (State == PrivateSessionClientState.Resynchronizing)
            {
                return;
            }

            State = PrivateSessionClientState.Resynchronizing;
            ResyncRequestCount++;
            gameplaySendSequence++;
            if (IsWaitingForHost)
            {
                // If a gap interrupted an in-flight answer, the explicit recovery projection is
                // now the exact frame that may release that answer's input latch.
                awaitingProjectionCorrelation = gameplaySendSequence;
            }
            Send(NetworkMessageCodec.RequestResyncFrame(new NetworkResyncRequestMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence))));
        }

        /// <summary>
        /// Milestone 8.32. Submits one command from this seat's own latest projection: the
        /// submission identity is the wire's own - drawn from the injected nonce source, never
        /// composed by presentation - and the expected version is the projection the answer was
        /// decided against.
        /// </summary>
        public bool TrySubmitCommand(DirectorCommand command, out string submissionId)
        {
            submissionId = null;
            if(command!=null && LatestProjection?.Experiments.TurnTimer==true &&
                (command.Kind==DirectorCommandKind.AnswerSystemTransitionStep && command.SystemTransitionChoice.StepId.Equals(GuardsDigital.Core.Rules.LanePush.LanePushSystemTransition.RespawnStepId) ||
                 command.Kind==DirectorCommandKind.ChooseCorrectionDestination && !LatestProjection.SelectedActor.IsDefined))
            { submissionId="board-vote"; return TrySubmitBoardVote(command); }
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (State != PrivateSessionClientState.Seated || LatestProjection == null ||
                IsWaitingForHost)
            {
                return false;
            }

            submissionId = MintSubmissionId();
            gameplaySendSequence++;
            BeginCommandWait(submissionId, gameplaySendSequence);
            Send(NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                NetworkCommandCodec.Encode(command))));
            return true;
        }

        /// <summary>
        /// Milestone 8.33. Submits one public agreement statement from this seat's own latest
        /// projected agreement surface, under exactly the command-submission discipline: the
        /// wire's own submission identity, the version the statement was decided against, and a
        /// refusal to act at all while the view is known stale.
        /// </summary>
        private long lastReadinessVersion = -1;
        private bool lastReadiness;
        public void ReportTimerReadiness(bool ready)
        {
            if (State!=PrivateSessionClientState.Seated || !HasMatchStarted || LatestProjection?.Experiments.TurnTimer!=true) return;
            var version=LatestProjection.Version;
            if (version==lastReadinessVersion && ready==lastReadiness) return;
            lastReadinessVersion=version; lastReadiness=ready; gameplaySendSequence++;
            Send(NetworkMessageCodec.TimerReadinessFrame(new NetworkEnvelope(SessionCorrelation,ConnectionEpoch,
                gameplaySendSequence,gameplaySendSequence),version,ready));
        }
        public bool TrySubmitBoardVote(DirectorCommand command)
        {
            if(State!=PrivateSessionClientState.Seated || LatestProjection?.Experiments.TurnTimer!=true) return false;
            gameplaySendSequence++;
            Send(NetworkMessageCodec.BoardVoteFrame(new NetworkEnvelope(SessionCorrelation,ConnectionEpoch,gameplaySendSequence,gameplaySendSequence),LatestProjection.Version,command));
            return true;
        }
        public bool TrySubmitCoordination(CoordinationCommand command)
        {
            if (command == null || State != PrivateSessionClientState.Seated || LatestProjection == null ||
                !LatestProjection.Experiments.TeammateCoordination) return false;
            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitCoordinationFrame(new NetworkEnvelope(
                SessionCorrelation, ConnectionEpoch, gameplaySendSequence, gameplaySendSequence), command));
            return true;
        }

        public bool TrySubmitAgreement(TeamAgreementCommand statement, out string submissionId)
        {
            submissionId = null;
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            if (State != PrivateSessionClientState.Seated || LatestProjection == null ||
                IsWaitingForHost)
            {
                return false;
            }

            submissionId = MintSubmissionId();
            gameplaySendSequence++;
            BeginAgreementWait(submissionId, gameplaySendSequence);
            Send(NetworkMessageCodec.SubmitAgreementFrame(new NetworkAgreementSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                statement)));
            return true;
        }

        /// <summary>
        /// Milestone 8.34. Sends one submission again under the identity it was first sent with:
        /// the retry a client makes when a lost connection took the answer with it.
        /// </summary>
        /// <remarks>
        /// <para>Keeping the identity is the whole point. A retry under a new name is a second
        /// question and the host would be right to execute it; a retry under the same name is the
        /// same question, and the host answers it from what it already did - the acceptance, or
        /// the refusal - without running anything twice. That is the failure policy's first row,
        /// and milestone 8.33's measured retry distance is what says the answer is still
        /// retained when a reconnect gets around to asking.</para>
        /// <para>The expected version is this seat's current one, not the one the lost attempt
        /// carried: a returning client has just been given a complete projection, and a retry that
        /// insisted on the older number would be answered as stale by a host that has already
        /// decided the question.</para>
        /// </remarks>
        public bool TryResubmitCommand(DirectorCommand command, string submissionId)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A retry names the identity it is retrying.", nameof(submissionId));
            }

            if (State != PrivateSessionClientState.Seated || LatestProjection == null ||
                IsWaitingForHost)
            {
                return false;
            }

            gameplaySendSequence++;
            BeginCommandWait(submissionId, gameplaySendSequence);
            Send(NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                NetworkCommandCodec.Encode(command))));
            return true;
        }

        /// <summary>Milestone 8.34. The agreement half of <see cref="TryResubmitCommand"/>.</summary>
        public bool TryResubmitAgreement(TeamAgreementCommand statement, string submissionId)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A retry names the identity it is retrying.", nameof(submissionId));
            }

            if (State != PrivateSessionClientState.Seated || LatestProjection == null ||
                IsWaitingForHost)
            {
                return false;
            }

            gameplaySendSequence++;
            BeginAgreementWait(submissionId, gameplaySendSequence);
            Send(NetworkMessageCodec.SubmitAgreementFrame(new NetworkAgreementSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                statement)));
            return true;
        }

        private string MintSubmissionId()
        {
            if (nonceSource == null)
            {
                throw new InvalidOperationException(
                    "Submitting requires a nonce source for the wire's own submission identities.");
            }

            var nonce = nonceSource.NextNonce() ?? Array.Empty<byte>();
            var builder = new System.Text.StringBuilder("wire-", 5 + nonce.Length * 2);
            foreach (var value in nonce)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            // The nonce source is transport-owned and may legitimately use any size admitted by
            // the session protocol. Hash it to a fixed wire identity so stronger entropy cannot
            // exceed the submission DTO's bounded text field.
            return "wire-" + PrivateSessionContentDigest.HashText(builder.ToString())
                .Substring(0, 32);
        }

        private void BeginCommandWait(string submissionId, long projectionCorrelation)
        {
            awaitingCommandResultId = submissionId;
            awaitingProjectionCorrelation = projectionCorrelation;
            waitingStartedTicks = timeSource();
        }

        private void BeginAgreementWait(string submissionId, long projectionCorrelation)
        {
            awaitingAgreementResultId = submissionId;
            awaitingProjectionCorrelation = projectionCorrelation;
            waitingStartedTicks = timeSource();
        }

        private void ClearCommandWait()
        {
            awaitingCommandResultId = null;
            awaitingProjectionCorrelation = 0;
            if (string.IsNullOrEmpty(awaitingAgreementResultId))
            {
                waitingStartedTicks = 0;
            }
        }

        private void ClearAgreementWait()
        {
            awaitingAgreementResultId = null;
            awaitingProjectionCorrelation = 0;
            if (string.IsNullOrEmpty(awaitingCommandResultId))
            {
                waitingStartedTicks = 0;
            }
        }

        private void BeginUndoWait(string submissionId, long projectionCorrelation)
        {
            awaitingUndoResultId = submissionId;
            awaitingProjectionCorrelation = projectionCorrelation;
            waitingStartedTicks = timeSource();
        }

        private void ClearUndoWait()
        {
            awaitingUndoResultId = null;
            awaitingProjectionCorrelation = 0;
            if (string.IsNullOrEmpty(awaitingCommandResultId) &&
                string.IsNullOrEmpty(awaitingAgreementResultId))
            {
                waitingStartedTicks = 0;
            }
        }

        /// <summary>
        /// Milestone 8.35f. Asks the host to step this seat's own turn back by one accepted
        /// command, under exactly the command-submission discipline: the wire's own submission
        /// identity, the version the rewind was decided against, and a refusal to ask at all while
        /// the view is not actionable or the host's own answer says there is nothing to take.
        /// </summary>
        /// <remarks>
        /// The <see cref="NetworkSeatProjectionRecord.IsSeatUndoAvailable"/> gate here is not the
        /// authorization - the host re-decides that - but it is what keeps a double click from
        /// becoming two questions: the second press happens against a projection the first press's
        /// result has not replaced yet, so this refuses to send it and the latch says why.
        /// </remarks>
        public bool TrySubmitUndo(out string submissionId)
        {
            submissionId = null;
            if (State != PrivateSessionClientState.Seated || LatestProjection == null ||
                IsWaitingForHost || !LatestProjection.IsSeatUndoAvailable)
            {
                return false;
            }

            submissionId = MintSubmissionId();
            gameplaySendSequence++;
            BeginUndoWait(submissionId, gameplaySendSequence);
            Send(NetworkMessageCodec.SubmitUndoFrame(new NetworkUndoSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version)));
            return true;
        }

        private void ClearWaitingForHost()
        {
            awaitingCommandResultId = null;
            awaitingAgreementResultId = null;
            awaitingUndoResultId = null;
            awaitingProjectionCorrelation = 0;
            waitingStartedTicks = 0;
        }

        private static long UtcNowTicks() => DateTime.UtcNow.Ticks;

        /// <summary>
        /// Milestone 8.35a. Tells the host this seat is ready, or that it is not.
        /// </summary>
        /// <remarks>
        /// <para>It says nothing about whether the seat then <em>is</em> ready:
        /// <see cref="IsReady"/> answers that, from the roster the host publishes, which is the
        /// only place readiness exists. A surface that lit its own control on this return value
        /// would be showing one process's belief instead of the table's shared state.</para>
        /// <para>Refused before it is sent from any state but <see cref="PrivateSessionClientState
        /// .Seated"/> and after the match has begun: readiness is a lobby statement about a held
        /// seat, and a client is the first thing that knows it is in neither position.</para>
        /// </remarks>
        public bool TrySetReadiness(bool isReady)
        {
            if (State != PrivateSessionClientState.Seated || HasMatchStarted)
            {
                return false;
            }

            Send(PrivateSessionFrameCodec.SetReadinessFrame(new SeatReadinessMessage(isReady)));
            return true;
        }

        /// <summary>
        /// 8.35z54. Claims one of the seats this admitted connection can now see, which is
        /// the user's flow spec: join the server, look at the table, take a chair.
        /// </summary>
        /// <remarks>
        /// <para>Offered only from <see cref="PrivateSessionClientState.Admitted"/> - a connection
        /// that arrived without naming a seat and has not asked for one yet. A seat is fixed for the
        /// session once granted, which is milestone 8.31's rule and is unchanged: this is the FIRST
        /// claim, not a change of seat, and the host refuses a second one by name exactly as before.
        /// </para>
        /// <para>It sends an ordinary <c>ClaimSeat</c> - the same frame the handshake has always
        /// sent, at the same point in the same state machine, merely later - so there is no new
        /// message, no new refusal and no wire movement. Whether the seat is actually granted is the
        /// host's answer arriving as <c>SeatGranted</c> or as a refusal; a surface that marked the
        /// seat taken on this return value would be showing one process's hope, exactly as
        /// <see cref="TrySetReadiness"/> and <see cref="TryChooseHero"/> record.</para>
        /// <para>A seat the roster already shows Claimed or Reserved is refused HERE as well as at
        /// the host, and that is not a second implementation of the rule: the host's answer remains
        /// authoritative and is what a player is told. This only spares the wire a frame whose
        /// answer the client already holds - the roster it is looking at.</para>
        /// </remarks>
        public bool TryClaimSeat(SeatId seat)
        {
            if (State != PrivateSessionClientState.Admitted || !seat.IsDefined || HasMatchStarted)
            {
                return false;
            }

            foreach (var entry in roster)
            {
                if (entry.Seat.Equals(seat) && entry.Occupancy != SeatOccupancyState.Open)
                {
                    return false;
                }
            }

            targetSeat = seat;
            Send(PrivateSessionFrameCodec.ClaimSeatFrame(new SeatClaimMessage(seat, PlayerName)));
            State = PrivateSessionClientState.AwaitingSeat;
            return true;
        }

        /// <summary>
        /// Milestone 8.35z37. Tells the host which hero this seat picks, under rulebook
        /// p.22's All Pick.
        /// </summary>
        /// <remarks>
        /// <para>It says nothing about whether the pick was TAKEN, for the reason
        /// <see cref="TrySetReadiness"/> says nothing about readiness: the answer is the roster the
        /// host publishes, and a surface that lit a hero on this return value would be showing one
        /// process's hope rather than the table's shared state. A refusal arrives on the reject path
        /// as <c>HeroSelectionRefused</c> with the stage's own words.</para>
        /// <para>Refused before it is sent from any state but
        /// <see cref="PrivateSessionClientState.Seated"/> and after the match has begun. The pick
        /// ORDER is not checked here and deliberately: whose turn it is is the host's answer, and a
        /// client that enforced it locally would be a second implementation of p.22 free to disagree
        /// with the first. A surface may grey the control from the roster it already reads; the rule
        /// stays on one side of the wire.</para>
        /// </remarks>
        public bool TryChooseHero(string heroIdValue)
        {
            if (State != PrivateSessionClientState.Seated || HasMatchStarted)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(heroIdValue) ||
                heroIdValue.Length > PrivateSessionProtocol.MaxHeroIdChars)
            {
                return false;
            }

            Send(PrivateSessionFrameCodec.ChooseHeroFrame(new HeroChoiceMessage(heroIdValue)));
            return true;
        }

        /// <summary>
        /// Milestone 8.35z37. The line-up the host settled on, read off the roster this
        /// client already holds - or <c>null</c> while the roster does not yet name a hero for every
        /// seat.
        /// </summary>
        /// <remarks>
        /// <para><b>This is how a client recomposes its own board to match the host's.</b> A client
        /// draws its match from its OWN local composition, updated by the host's projections, so a
        /// host that composed a chosen line-up while the client kept the printed one would leave six
        /// people looking at the wrong heroes. The roster is the fix and it needs no new message:
        /// every row has carried a hero id since milestone 8.31, and the host broadcasts a fresh
        /// roster on every accepted pick.</para>
        /// <para>Derived on demand rather than latched, so a roster arriving after a resync or a
        /// reconnect gives the same answer as the one before it. It resolves each row against the
        /// live registration and answers null if any row names a hero this build cannot deal, which
        /// is the honest reading: a client that cannot compose what the host composed must not
        /// compose something else and call it the same match.</para>
        /// </remarks>
        public HeroLineup ComposedLineup()
        {
            var red = new List<RegisteredHero>();
            var blue = new List<RegisteredHero>();
            foreach (var entry in roster)
            {
                if (!RegisteredHeroRoster.TryFind(entry.Hero.Value, out var hero))
                {
                    return null;
                }

                if (entry.TeamSide == TeamSide.Red)
                {
                    red.Add(hero);
                }
                else if (entry.TeamSide == TeamSide.Blue)
                {
                    blue.Add(hero);
                }
                else
                {
                    return null;
                }
            }

            if (red.Count == 0 || red.Count != blue.Count)
            {
                return null;
            }

            try
            {
                return new HeroLineup(red, blue);
            }
            catch (ArgumentException)
            {
                // The one-copy rule refused it. A roster naming one hero twice is a host that
                // cannot have composed a board either, so answering null leaves this client on the
                // match it has rather than throwing inside a redraw.
                return null;
            }
        }

        /// <summary>An orderly forfeiting departure, authenticated when the connection is keyed.</summary>
        /// <remarks>
        /// Milestone 8.35k. LEAVING FORFEITS THE SEAT, and this is the client's half of that. A
        /// Goodbye is the one departure the player chose, so the road back is dropped here at the
        /// same moment the host drops the reservation on the other end of the same signal - a
        /// player who leaves deliberately gives the seat up, and a player whose process died does
        /// not. Nothing else clears the store mid-session, so a crash keeps its road.
        /// </remarks>
        public void SendGoodbye()
        {
            retainedCapabilities.Forget(SessionCorrelation, targetSeat);
            reconnectCapability = string.Empty;
            Send(PrivateSessionFrameCodec.GoodbyeFrame());
        }

        private enum EnvelopeReading
        {
            /// <summary>Wrong session or epoch: a protocol violation, not a delivery fault.</summary>
            Invalid = 0,

            /// <summary>Exactly the next sequence.</summary>
            Next = 1,

            /// <summary>Past the next sequence: something between was never delivered.</summary>
            Gap = 2,

            /// <summary>At or before the last accepted sequence: a duplicate or a replay.</summary>
            Stale = 3
        }

        private EnvelopeReading ReadEnvelope(NetworkEnvelope envelope)
        {
            if (!string.Equals(envelope.SessionCorrelation, SessionCorrelation, StringComparison.Ordinal) ||
                envelope.Epoch != ConnectionEpoch)
            {
                return EnvelopeReading.Invalid;
            }

            if (envelope.Sequence <= gameplayReceiveSequence)
            {
                return EnvelopeReading.Stale;
            }

            return envelope.Sequence == gameplayReceiveSequence + 1
                ? EnvelopeReading.Next
                : EnvelopeReading.Gap;
        }

        private void Send(byte[] frame)
        {
            if (channel == null)
            {
                outbox.Send(frame);
                return;
            }

            var kind = (SessionMessageKind)PrivateSessionFrameCodec.ReadUInt16(frame, 6);
            var payload = new byte[frame.Length - PrivateSessionProtocol.FrameHeaderBytes];
            Array.Copy(frame, PrivateSessionProtocol.FrameHeaderBytes, payload, 0, payload.Length);
            outbox.Send(channel.Seal(kind, payload));
        }

        /// <summary>The transport found the host's byte stream itself invalid.</summary>
        public void OnStreamRejected(SessionRejectionReason reason)
        {
            if (State == PrivateSessionClientState.Rejected)
            {
                return;
            }

            RejectionReason = reason;
            Fault();
        }

        /// <summary>
        /// The transport says the connection ended.
        /// </summary>
        /// <remarks>
        /// Milestone 8.34 separates the two ways that happens. A connection that ends after an
        /// orderly close, a refusal, or before this client ever held a seat is simply
        /// <see cref="PrivateSessionClientState.Disconnected"/> - nothing was lost, and the
        /// decision record closes a pre-claim connection silently. A connection that ends while
        /// the seat was held, with nothing said, is the host-loss row: the authority this seat was
        /// playing under is gone as far as this process can tell, and
        /// <see cref="PrivateSessionClientState.HostLost"/> is the terminal state the record names
        /// for it.
        /// </remarks>
        public void OnDisconnected()
        {
            if (State == PrivateSessionClientState.Rejected ||
                State == PrivateSessionClientState.Faulted ||
                State == PrivateSessionClientState.Disconnected ||
                State == PrivateSessionClientState.HostLost)
            {
                return;
            }

            State = hasHeldSeat && !refusalReceived
                ? PrivateSessionClientState.HostLost
                : PrivateSessionClientState.Disconnected;
            IsReconnecting = false;
            awaitingReconnectProjection = false;
            ClearWaitingForHost();
        }

        private void Fault()
        {
            State = PrivateSessionClientState.Faulted;
            IsReconnecting = false;
            awaitingReconnectProjection = false;
            ClearWaitingForHost();
            outbox.Close();
        }
    }
}
