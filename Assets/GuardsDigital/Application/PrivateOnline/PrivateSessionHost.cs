using System;
using System.Collections.Generic;
using System.Globalization;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>How the session layer sends and closes; implemented by the transport adapter.</summary>
    /// <remarks>
    /// The session state machines name only this, so they compile and test with no socket in
    /// sight. <see cref="Close"/> is a request: the adapter flushes what was sent, closes that one
    /// connection, and eventually reports <c>OnDisconnected</c> back.
    /// </remarks>
    public interface IPrivateSessionOutbox
    {
        void Send(int connectionOrdinal, byte[] frame);
        void Close(int connectionOrdinal);
    }

    /// <summary>
    /// Where a challenge nonce comes from. The Application assembly draws no platform entropy -
    /// the setup-randomness conformance rule scans for exactly that - so the adapter supplies it.
    /// </summary>
    public interface IPrivateSessionNonceSource
    {
        byte[] NextNonce();
    }

    /// <summary>What happened inside the session host, for a driver's bounded diagnostics.</summary>
    public enum PrivateSessionEventKind
    {
        None = 0,
        ConnectionOpened = 1,
        Admitted = 2,
        SeatClaimed = 3,
        SeatReserved = 4,
        Rejected = 5,
        Disconnected = 6,
        SessionComplete = 7,

        /// <summary>Milestone 8.32. A seat's wire submission reached the authority and was accepted.</summary>
        SubmissionAccepted = 8,

        /// <summary>
        /// Milestone 8.32. A seat's wire submission was refused; the seat-level reason rides
        /// <see cref="PrivateSessionEventRecord.SubmissionRejection"/>. No command content, hand
        /// or detail text is recorded - the reason and the seat are the whole diagnostic.
        /// </summary>
        SubmissionRefused = 9,

        /// <summary>Milestone 8.33. A seat's public agreement statement was recorded.</summary>
        AgreementAccepted = 10,

        /// <summary>
        /// Milestone 8.33. A seat's agreement statement was refused; the reason rides
        /// <see cref="PrivateSessionEventRecord.AgreementRejection"/>.
        /// </summary>
        AgreementRefused = 11,

        /// <summary>
        /// Milestone 8.33. The host served one bounded current-projection resync - to a seat that
        /// asked, or beside a stale-projection refusal. This is the recovery decision the
        /// diagnostics correlate: which seat, and against which authoritative version.
        /// </summary>
        ResyncServed = 12
    }

    /// <summary>
    /// One session-host event. Carries the opaque connection ordinal, the seat where one is
    /// involved, and the rejection reason where there is one - and deliberately nothing else: no
    /// address, no hand, no other seat's state.
    /// </summary>
    public sealed class PrivateSessionEventRecord
    {
        public PrivateSessionEventRecord(
            PrivateSessionEventKind kind,
            int connectionOrdinal,
            SeatId seat,
            HeroId hero,
            TeamSide teamSide,
            SessionRejectionReason reason,
            int claimedSeats,
            int totalSeats,
            SeatSubmissionRejectionReason submissionRejection = SeatSubmissionRejectionReason.None,
            TeamAgreementSubmissionRejectionReason agreementRejection =
                TeamAgreementSubmissionRejectionReason.None,
            long version = 0,
            string submissionId = null)
        {
            Kind = kind;
            ConnectionOrdinal = connectionOrdinal;
            Seat = seat;
            Hero = hero;
            TeamSide = teamSide;
            Reason = reason;
            ClaimedSeats = claimedSeats;
            TotalSeats = totalSeats;
            SubmissionRejection = submissionRejection;
            AgreementRejection = agreementRejection;
            Version = version;
            SubmissionId = submissionId ?? string.Empty;
        }

        public PrivateSessionEventKind Kind { get; }
        public int ConnectionOrdinal { get; }
        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide TeamSide { get; }
        public SessionRejectionReason Reason { get; }
        public int ClaimedSeats { get; }
        public int TotalSeats { get; }

        /// <summary>Milestone 8.32. Why a wire submission was refused; <c>None</c> otherwise.</summary>
        public SeatSubmissionRejectionReason SubmissionRejection { get; }

        /// <summary>Milestone 8.33. Why an agreement statement was refused; <c>None</c> otherwise.</summary>
        public TeamAgreementSubmissionRejectionReason AgreementRejection { get; }

        /// <summary>
        /// Milestone 8.33. The authoritative projection version when the event was recorded, so a
        /// diagnostic line correlates the wire's submission identity with the version it acted
        /// against - the milestone's third check, without a byte of private state.
        /// </summary>
        public long Version { get; }

        /// <summary>Milestone 8.33. The wire's own submission identity, empty where none applies.</summary>
        public string SubmissionId { get; }

        public override string ToString() =>
            Kind + " ordinal=" + ConnectionOrdinal.ToString(CultureInfo.InvariantCulture) +
            (Seat.IsDefined ? " seat=" + Seat.Value : string.Empty) +
            " version=" + Version.ToString(CultureInfo.InvariantCulture) +
            (SubmissionId.Length > 0 ? " submission-id=" + SubmissionId : string.Empty) +
            (Reason != SessionRejectionReason.None ? " reason=" + Reason : string.Empty) +
            (SubmissionRejection != SeatSubmissionRejectionReason.None
                ? " submission=" + SubmissionRejection
                : string.Empty) +
            (AgreementRejection != TeamAgreementSubmissionRejectionReason.None
                ? " agreement=" + AgreementRejection
                : string.Empty);
    }

    /// <summary>
    /// Milestone 8.31. The dedicated host's session state machine: admission by tuple and key,
    /// then one stable seat per connection, everything else refused by name.
    /// </summary>
    /// <remarks>
    /// <para>Transport-neutral and single-threaded by contract: every call arrives on the
    /// adapter's pump thread, the discipline that keeps the serialized-executor rule true when
    /// milestone 8.32 starts submitting commands. Nothing here touches sockets, threads, clocks or
    /// entropy - time arrives as an argument and nonces from an injected source.</para>
    /// <para>It holds the <see cref="IMatchHost"/> port for exactly one thing: the authoritative
    /// six-seat binding. No submission crosses this type in 8.31; putting commands and projections
    /// on the wire is milestones 8.32-8.33.</para>
    /// <para>Seat ownership is host state bound to one live connection. A disconnected claimed
    /// seat becomes <see cref="SeatOccupancyState.Reserved"/> and stays unclaimable: with no
    /// identity to recognize a returning player, releasing it would transfer one player's seat to
    /// whoever asks next, which the decision record forbids. Milestone 8.34's reconnect work is
    /// what turns a reservation back into a claim.</para>
    /// </remarks>
    public sealed class PrivateSessionHost
    {
        private enum ConnectionPhase
        {
            AwaitingHello = 0,
            Admitted = 1,
            Seated = 2
        }

        private sealed class ConnectionRecord
        {
            public ConnectionRecord(int ordinal, long helloDeadlineTicks, byte[] nonce)
            {
                Ordinal = ordinal;
                HelloDeadlineTicks = helloDeadlineTicks;
                Nonce = nonce;
            }

            public int Ordinal { get; }
            public long HelloDeadlineTicks { get; }
            public byte[] Nonce { get; }
            public ConnectionPhase Phase { get; set; }
            public SeatId Seat { get; set; }

            /// <summary>
            /// Milestone 8.32. Armed when this connection's key proof verified on a keyed session;
            /// null on an unkeyed - loopback-bound - session, which stays plaintext.
            /// </summary>
            public PrivateSessionSecureChannel Channel { get; set; }

            /// <summary>Milestone 8.32. The last gameplay-envelope sequence accepted from this connection.</summary>
            public long GameplayReceiveSequence { get; set; }

            /// <summary>Milestone 8.32. The last gameplay-envelope sequence sent to this connection.</summary>
            public long GameplaySendSequence { get; set; }
        }

        private sealed class SeatHold
        {
            public SeatOccupancyState State { get; set; }
            public int HeldByOrdinal { get; set; }
        }

        private const int RetainedEventCount = 1024;

        private readonly IMatchHost matchHost;
        private readonly PrivateSessionIdentity identity;
        private readonly string sessionKey;
        private readonly IPrivateSessionOutbox outbox;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly Dictionary<int, ConnectionRecord> connections =
            new Dictionary<int, ConnectionRecord>();
        private readonly Dictionary<SeatId, SeatHold> holds = new Dictionary<SeatId, SeatHold>();
        private readonly List<PrivateSessionEventRecord> events =
            new List<PrivateSessionEventRecord>();

        public PrivateSessionHost(
            IMatchHost matchHost,
            PrivateSessionIdentity identity,
            string sessionKey,
            IPrivateSessionOutbox outbox,
            IPrivateSessionNonceSource nonceSource = null)
        {
            this.matchHost = matchHost ?? throw new ArgumentNullException(nameof(matchHost));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.sessionKey = sessionKey ?? string.Empty;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.nonceSource = nonceSource;

            if (this.sessionKey.Length > 0 && nonceSource == null)
            {
                throw new ArgumentException(
                    "A keyed session requires a nonce source, or every challenge would be constant.",
                    nameof(nonceSource));
            }

            foreach (var seat in matchHost.Seats.Seats)
            {
                holds[seat.Id] = new SeatHold { State = SeatOccupancyState.Open };
            }
        }

        public PrivateSessionIdentity Identity => identity;

        public int TotalSeats => matchHost.Seats.Seats.Count;

        public int ClaimedSeatCount
        {
            get
            {
                var claimed = 0;
                foreach (var hold in holds.Values)
                {
                    if (hold.State == SeatOccupancyState.Claimed)
                    {
                        claimed++;
                    }
                }

                return claimed;
            }
        }

        /// <summary>Every seat is claimed by a live connection.</summary>
        public bool IsComplete => ClaimedSeatCount == TotalSeats;

        public int ConnectionCount => connections.Count;

        /// <summary>The current public seat map, in the authoritative seat order.</summary>
        public IReadOnlyList<SeatRosterEntry> Roster
        {
            get
            {
                var entries = new List<SeatRosterEntry>(TotalSeats);
                foreach (var seat in matchHost.Seats.Seats)
                {
                    entries.Add(new SeatRosterEntry(
                        seat.Id, seat.HeroId, seat.TeamSide, holds[seat.Id].State));
                }

                return entries;
            }
        }

        public bool TryGetSeatForConnection(int connectionOrdinal, out MatchSeat seat)
        {
            seat = null;
            if (connections.TryGetValue(connectionOrdinal, out var record) &&
                record.Phase == ConnectionPhase.Seated)
            {
                seat = matchHost.Seats.Find(record.Seat);
            }

            return seat != null;
        }

        /// <summary>The events since the last drain, oldest first; draining clears them.</summary>
        public IReadOnlyList<PrivateSessionEventRecord> DrainEvents()
        {
            var drained = events.ToArray();
            events.Clear();
            return drained;
        }

        public void OnConnected(int connectionOrdinal, long nowTicks)
        {
            if (connections.Count >= PrivateSessionProtocol.MaxConcurrentConnections)
            {
                outbox.Send(connectionOrdinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.TooManyConnections,
                        "The session holds its connection bound.")));
                outbox.Close(connectionOrdinal);
                Record(PrivateSessionEventKind.Rejected, connectionOrdinal,
                    default, default, TeamSide.None, SessionRejectionReason.TooManyConnections);
                return;
            }

            var nonce = nonceSource == null ? Array.Empty<byte>() : nonceSource.NextNonce();
            var record = new ConnectionRecord(
                connectionOrdinal,
                nowTicks + PrivateSessionProtocol.HandshakeTimeoutSeconds * TimeSpan.TicksPerSecond,
                nonce ?? Array.Empty<byte>());
            connections[connectionOrdinal] = record;
            outbox.Send(connectionOrdinal, PrivateSessionFrameCodec.ChallengeFrame(
                new SessionChallengeMessage(record.Nonce)));
            Record(PrivateSessionEventKind.ConnectionOpened, connectionOrdinal,
                default, default, TeamSide.None, SessionRejectionReason.None);
        }

        public void OnFrame(int connectionOrdinal, SessionFrame frame)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            // Milestone 8.32. Once this connection's key proof verified, only sealed frames and a
            // plaintext Goodbye are meaningful: a plaintext frame of any other kind on a sealed
            // connection is an injection or a broken peer, refused whole. Goodbye and Reject stay
            // plaintext in both directions - they carry no secret, a pre-admission refusal has no
            // keys yet, and forging a close signal is the denial of service an on-path party could
            // already commit with a reset.
            if (frame.Kind == SessionMessageKind.Sealed)
            {
                if (record.Channel == null || !record.Channel.TryUnseal(frame.Payload, out frame))
                {
                    RejectAndClose(record, SessionRejectionReason.SealViolation,
                        "The sealed frame did not verify.");
                    return;
                }
            }
            else if (record.Channel != null && frame.Kind != SessionMessageKind.Goodbye)
            {
                RejectAndClose(record, SessionRejectionReason.SealViolation,
                    "A sealed connection accepts sealed frames only.");
                return;
            }

            if (frame.Kind == SessionMessageKind.Goodbye)
            {
                Depart(record);
                outbox.Close(connectionOrdinal);
                return;
            }

            switch (record.Phase)
            {
                case ConnectionPhase.AwaitingHello:
                    if (frame.Kind != SessionMessageKind.Hello)
                    {
                        RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                            "Admission begins with Hello.");
                        return;
                    }

                    HandleHello(record, frame.Payload);
                    return;

                case ConnectionPhase.Admitted:
                    if (frame.Kind != SessionMessageKind.ClaimSeat)
                    {
                        RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                            "An admitted connection claims a seat next.");
                        return;
                    }

                    HandleClaim(record, frame.Payload);
                    return;

                case ConnectionPhase.Seated:
                    if (frame.Kind == SessionMessageKind.ClaimSeat)
                    {
                        // Refused without disconnecting, so the rejection leaves the seat map
                        // untouched: the connection keeps the seat it already holds.
                        outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                            new SessionRejectMessage(
                                SessionRejectionReason.SeatChangeRefused,
                                "A seat is stable for the session.")));
                        Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                            default, TeamSide.None, SessionRejectionReason.SeatChangeRefused);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SubmitCommand)
                    {
                        HandleSubmission(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SubmitAgreement)
                    {
                        HandleAgreementSubmission(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.RequestResync)
                    {
                        HandleResyncRequest(record, frame.Payload);
                        return;
                    }

                    RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                        "No further session message is defined after seating.");
                    return;
            }
        }

        /// <summary>The adapter found this connection's byte stream itself invalid.</summary>
        public void OnStreamRejected(int connectionOrdinal, SessionRejectionReason reason)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            RejectAndClose(record, reason, "The connection's stream was refused.");
        }

        public void OnDisconnected(int connectionOrdinal)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            Depart(record);
        }

        /// <summary>Closes every unadmitted connection whose handshake deadline has passed.</summary>
        public void Sweep(long nowTicks)
        {
            List<ConnectionRecord> expired = null;
            foreach (var record in connections.Values)
            {
                if (record.Phase == ConnectionPhase.AwaitingHello &&
                    nowTicks > record.HelloDeadlineTicks)
                {
                    (expired = expired ?? new List<ConnectionRecord>()).Add(record);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (var record in expired)
            {
                RejectAndClose(record, SessionRejectionReason.HandshakeTimeout,
                    "The handshake missed its deadline.");
            }
        }

        private void HandleHello(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeHello(payload, out var hello))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The Hello payload did not decode.");
                return;
            }

            var mismatch = identity.FirstMismatch(hello.Identity);
            if (mismatch != SessionRejectionReason.None)
            {
                // The refusal names the failing element and the host's own value of it - which the
                // client needs to report usefully - and nothing else about the session.
                RejectAndClose(record, mismatch, DescribeMismatch(mismatch));
                return;
            }

            if (sessionKey.Length > 0)
            {
                var expected = PrivateSessionKeyProof.Compute(record.Nonce, sessionKey);
                if (!PrivateSessionKeyProof.Matches(expected, hello.KeyProof))
                {
                    RejectAndClose(record, SessionRejectionReason.SessionKeyMismatch,
                        "The key proof did not answer the challenge.");
                    return;
                }

                // Milestone 8.32. The proof verified, so both ends hold the key and this
                // connection's nonce: everything after this line travels sealed, both ways.
                record.Channel = PrivateSessionSecureChannel.ForHost(sessionKey, record.Nonce);
            }

            // Full is answered at admission, before the roster is disclosed: a claim of a specific
            // taken seat gets that seat's own reason below, so this is the only place the plain
            // "there is no seat left for you" answer can live without shadowing those.
            if (!AnySeatOpen())
            {
                RejectAndClose(record, SessionRejectionReason.SessionFull,
                    "Every seat is taken or reserved.");
                return;
            }

            record.Phase = ConnectionPhase.Admitted;
            Deliver(record, PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage(identity.Correlation, Roster)));
            Record(PrivateSessionEventKind.Admitted, record.Ordinal,
                default, default, TeamSide.None, SessionRejectionReason.None);
        }

        private void HandleClaim(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeClaimSeat(payload, out var claim))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The claim payload did not decode.");
                return;
            }

            var seat = matchHost.Seats.Find(claim.Seat);
            if (seat == null)
            {
                RejectAndClose(record, SessionRejectionReason.UnknownSeat,
                    "No seat of that id is bound in this match.");
                return;
            }

            var targetHold = holds[seat.Id];
            if (targetHold.State == SeatOccupancyState.Claimed)
            {
                RejectAndClose(record, SessionRejectionReason.SeatAlreadyClaimed,
                    "That seat is held by a live connection.");
                return;
            }

            if (targetHold.State == SeatOccupancyState.Reserved)
            {
                RejectAndClose(record, SessionRejectionReason.SeatReserved,
                    "That seat is reserved for its disconnected player.");
                return;
            }

            targetHold.State = SeatOccupancyState.Claimed;
            targetHold.HeldByOrdinal = record.Ordinal;
            record.Phase = ConnectionPhase.Seated;
            record.Seat = seat.Id;

            Deliver(record, PrivateSessionFrameCodec.SeatGrantedFrame(
                new SeatGrantedMessage(
                    new SeatRosterEntry(seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Claimed),
                    ClaimedSeatCount, TotalSeats)));
            BroadcastRoster();
            // Milestone 8.32. A seated connection immediately receives its own owner-scoped view,
            // encoded per seat - never one shared payload - so what it holds is what 8.30a proved
            // it may hold.
            SendProjectionTo(record);
            Record(PrivateSessionEventKind.SeatClaimed, record.Ordinal, seat.Id, seat.HeroId,
                seat.TeamSide, SessionRejectionReason.None);
            if (IsComplete)
            {
                Record(PrivateSessionEventKind.SessionComplete, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
            }
        }

        /// <summary>
        /// Milestone 8.32. One wire submission from a seated connection: the trusted caller is the
        /// seat this connection claimed, the submission identity and expected version come from the
        /// envelope fields outside the command payload, and the command DTO is mapped to the
        /// seat's own offered instance before the authority is asked.
        /// </summary>
        /// <remarks>
        /// <para>Protocol violations - a wrong session, epoch or sequence, or a payload that does
        /// not decode - close the connection: a real client of this build cannot produce them.
        /// Seat-level refusals answer with a <see cref="SessionMessageKind.SubmissionResult"/> and
        /// leave the connection open, because a stale view or a refused rule answer is an ordinary
        /// gameplay moment, not an attack.</para>
        /// <para>The decoded command reaches <see cref="IMatchHost.SubmitForSeat"/> even when it
        /// matches no offered instance, so 8.30a's own ordered refusals - stale before duplicate
        /// before ownership before unauthorized - answer with their exact reasons; an unmatched
        /// command cannot be accepted there, because acceptance requires membership in the same
        /// offered set the mapping searched.</para>
        /// </remarks>
        private void HandleSubmission(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeSubmitCommand(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The submission payload did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            if (!NetworkCommandCodec.TryDecode(message.CommandPayload, out var decoded))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The command payload did not decode.");
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            var command = decoded;
            foreach (var offered in matchHost.ProjectionForSeat(seat.Id).AuthorizedCommands)
            {
                if (offered.Equals(decoded))
                {
                    command = offered;
                    break;
                }
            }

            var result = matchHost.SubmitForSeat(seat.Id, new SeatSubmission(
                new SubmissionId(message.SubmissionId), message.ExpectedVersion, command));

            Deliver(record, NetworkMessageCodec.SubmissionResultFrame(new NetworkSubmissionResultMessage(
                NextEnvelope(record, message.Envelope.Correlation),
                message.SubmissionId,
                result.Accepted,
                result.RejectionReason,
                result.SessionRejectionReason,
                result.Detail,
                result.Version,
                result.Sequence)));

            Record(
                result.Accepted
                    ? PrivateSessionEventKind.SubmissionAccepted
                    : PrivateSessionEventKind.SubmissionRefused,
                record.Ordinal, record.Seat, seat.HeroId, seat.TeamSide,
                SessionRejectionReason.None, result.RejectionReason,
                submissionId: message.SubmissionId);

            if (result.Accepted)
            {
                PublishProjections();
            }
            else if (result.RejectionReason == SeatSubmissionRejectionReason.StaleProjection)
            {
                // The failure policy's row: a stale expected version is refused with no mutation
                // and offered one bounded current-projection resync. Offering it here, unasked,
                // is what lets a client that answered against an old view converge on the next
                // frame rather than waiting for someone else's acceptance to push one.
                SendProjectionTo(record);
                Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                    seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
            }
        }

        /// <summary>
        /// Milestone 8.33. One public agreement statement from a seated connection, under exactly
        /// the submission discipline: envelope violations close the connection, the decoded
        /// statement is mapped to the seat's own projected offer before the coordinator is asked,
        /// and every seat-level refusal answers by name with nothing mutated.
        /// </summary>
        /// <remarks>
        /// An accepted statement republishes every seat's projection even though most statements
        /// move no authoritative version: the statement changed what every teammate's agreement
        /// surface says, and the statement that completes unanimity has moved the match itself.
        /// The final rules command is only ever composed by the host's coordinator - a seat that
        /// submits it directly is refused by the offered surface, which the wire tests prove.
        /// </remarks>
        private void HandleAgreementSubmission(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeSubmitAgreement(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The agreement payload did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            var statement = message.Statement;
            var agreement = matchHost.ProjectionForSeat(seat.Id).TeamAgreement;
            if (agreement != null)
            {
                foreach (var offered in agreement.AuthorizedCommands)
                {
                    if (offered.Equals(statement))
                    {
                        statement = offered;
                        break;
                    }
                }
            }

            var result = matchHost.SubmitAgreementForSeat(seat.Id, new TeamAgreementSubmission(
                new SubmissionId(message.SubmissionId), message.ExpectedVersion, statement));

            Deliver(record, NetworkMessageCodec.AgreementResultFrame(new NetworkAgreementResultMessage(
                NextEnvelope(record, message.Envelope.Correlation),
                message.SubmissionId,
                result.Accepted,
                result.RejectionReason,
                result.Detail,
                result.Version)));

            Record(
                result.Accepted
                    ? PrivateSessionEventKind.AgreementAccepted
                    : PrivateSessionEventKind.AgreementRefused,
                record.Ordinal, record.Seat, seat.HeroId, seat.TeamSide,
                SessionRejectionReason.None,
                agreementRejection: result.RejectionReason,
                submissionId: message.SubmissionId);

            if (result.Accepted)
            {
                PublishProjections();
            }
            else if (result.RejectionReason == TeamAgreementSubmissionRejectionReason.StaleProjection ||
                result.RejectionReason == TeamAgreementSubmissionRejectionReason.StaleProposal)
            {
                SendProjectionTo(record);
                Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                    seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
            }
        }

        /// <summary>
        /// Milestone 8.33. A seat asked for one bounded current-projection resync. The answer is
        /// its ordinary projection push echoing the request's correlation - one complete bounded
        /// projection per request, never a delta chain.
        /// </summary>
        private void HandleResyncRequest(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeRequestResync(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The resync request did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            SendProjectionTo(record, message.Envelope.Correlation);
            Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
        }

        /// <summary>
        /// The one envelope rule every gameplay message obeys: right session, current epoch, and
        /// exactly the next sequence - anything else is a protocol violation that closes the
        /// connection before any seat-level answer.
        /// </summary>
        private bool AcceptGameplayEnvelope(ConnectionRecord record, NetworkEnvelope envelope)
        {
            if (!string.Equals(envelope.SessionCorrelation, identity.Correlation, StringComparison.Ordinal) ||
                envelope.Epoch != PrivateSessionProtocol.InitialConnectionEpoch ||
                envelope.Sequence != record.GameplayReceiveSequence + 1)
            {
                RejectAndClose(record, SessionRejectionReason.EnvelopeMismatch,
                    "The envelope names the wrong session, epoch, or sequence.");
                return false;
            }

            record.GameplayReceiveSequence = envelope.Sequence;
            return true;
        }

        private void SendProjectionTo(ConnectionRecord record, long correlation = 0)
        {
            var seat = matchHost.Seats.Find(record.Seat);
            if (seat == null)
            {
                return;
            }

            var payload = NetworkSeatProjectionCodec.Encode(matchHost.ProjectionForSeat(seat.Id));
            Deliver(record, NetworkMessageCodec.SeatProjectionFrame(
                new NetworkSeatProjectionMessage(NextEnvelope(record, correlation), payload)));
        }

        /// <summary>Every seated connection receives its own view of the state that just committed.</summary>
        private void PublishProjections()
        {
            foreach (var record in connections.Values)
            {
                if (record.Phase == ConnectionPhase.Seated)
                {
                    SendProjectionTo(record);
                }
            }
        }

        /// <summary>
        /// Milestone 8.33. Republishes every seated connection's view after a host-side state
        /// change the wire never saw - a snapshot restore, restart, or Undo submitted through the
        /// authority's own port rather than by a seat. Synchronization is the host's obligation
        /// wherever state moved, not only where a wire submission moved it.
        /// </summary>
        public void RepublishProjections() => PublishProjections();

        private NetworkEnvelope NextEnvelope(ConnectionRecord record, long correlation)
        {
            record.GameplaySendSequence++;
            return new NetworkEnvelope(
                identity.Correlation,
                PrivateSessionProtocol.InitialConnectionEpoch,
                record.GameplaySendSequence,
                correlation);
        }

        /// <summary>
        /// Sends one composed frame, sealed when this connection's channel is armed. Challenge,
        /// Reject, and Goodbye do not come through here - they stay plaintext by design.
        /// </summary>
        private void Deliver(ConnectionRecord record, byte[] frame)
        {
            if (record.Channel == null)
            {
                outbox.Send(record.Ordinal, frame);
                return;
            }

            var kind = (SessionMessageKind)PrivateSessionFrameCodec.ReadUInt16(frame, 6);
            var payload = new byte[frame.Length - PrivateSessionProtocol.FrameHeaderBytes];
            Array.Copy(frame, PrivateSessionProtocol.FrameHeaderBytes, payload, 0, payload.Length);
            outbox.Send(record.Ordinal, record.Channel.Seal(kind, payload));
        }

        /// <summary>
        /// Ends the session on purpose: every connection is told Goodbye and closed. No seat is
        /// reserved by an orderly end - there is no session left to hold a reservation.
        /// </summary>
        public void Shutdown()
        {
            var open = new List<ConnectionRecord>(connections.Values);
            foreach (var record in open)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.GoodbyeFrame());
                outbox.Close(record.Ordinal);
                connections.Remove(record.Ordinal);
            }
        }

        private bool AnySeatOpen()
        {
            foreach (var hold in holds.Values)
            {
                if (hold.State == SeatOccupancyState.Open)
                {
                    return true;
                }
            }

            return false;
        }

        private void RejectAndClose(ConnectionRecord record, SessionRejectionReason reason, string detail)
        {
            outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                new SessionRejectMessage(reason, detail)));
            outbox.Close(record.Ordinal);
            Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                default, TeamSide.None, reason);
            Depart(record);
        }

        private void Depart(ConnectionRecord record)
        {
            connections.Remove(record.Ordinal);
            if (record.Phase != ConnectionPhase.Seated)
            {
                Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
                return;
            }

            var hold = holds[record.Seat];
            hold.State = SeatOccupancyState.Reserved;
            hold.HeldByOrdinal = 0;
            var seat = matchHost.Seats.Find(record.Seat);
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatReserved, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None);
            Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                record.Seat, default, TeamSide.None, SessionRejectionReason.None);
        }

        private void BroadcastRoster()
        {
            var frame = PrivateSessionFrameCodec.RosterUpdateFrame(new SeatRosterMessage(Roster));
            foreach (var record in connections.Values)
            {
                if (record.Phase != ConnectionPhase.AwaitingHello)
                {
                    Deliver(record, frame);
                }
            }
        }

        private string DescribeMismatch(SessionRejectionReason mismatch)
        {
            switch (mismatch)
            {
                case SessionRejectionReason.ProtocolFamilyMismatch:
                    return "Protocol family differs; host runs " + identity.ProtocolFamily + ".";
                case SessionRejectionReason.ProtocolVersionMismatch:
                    return "Protocol version differs; host runs " +
                        identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.BuildIdentityMismatch:
                    return "Build identity differs; host runs " + identity.BuildIdentity + ".";
                case SessionRejectionReason.SnapshotSchemaMismatch:
                    return "Snapshot schema differs; host runs " +
                        identity.SnapshotSchemaVersion.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.MapRevisionMismatch:
                    return "Map revision differs; host runs " + identity.MapId + "@" +
                        identity.MapContentRevision.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.ContentDigestMismatch:
                    return "Registered content differs; host digest " + identity.ContentDigest + ".";
                default:
                    return "The admission tuple differs.";
            }
        }

        private void Record(
            PrivateSessionEventKind kind,
            int ordinal,
            SeatId seat,
            HeroId hero,
            TeamSide teamSide,
            SessionRejectionReason reason,
            SeatSubmissionRejectionReason submissionRejection = SeatSubmissionRejectionReason.None,
            TeamAgreementSubmissionRejectionReason agreementRejection =
                TeamAgreementSubmissionRejectionReason.None,
            string submissionId = null)
        {
            if (events.Count >= RetainedEventCount)
            {
                events.RemoveAt(0);
            }

            events.Add(new PrivateSessionEventRecord(
                kind, ordinal, seat, hero, teamSide, reason, ClaimedSeatCount, TotalSeats,
                submissionRejection, agreementRejection, matchHost.Version, submissionId));
        }
    }
}
