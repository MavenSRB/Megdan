using System;
using System.Collections.Generic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.31. Every message kind protocol version 1's session slice defines. An integer
    /// that is not one of these is refused where it is read, never cast through.
    /// </summary>
    public enum SessionMessageKind
    {
        None = 0,

        /// <summary>Host to client on accept: the nonce the key proof answers.</summary>
        Challenge = 1,

        /// <summary>Client to host: its admission tuple and key proof.</summary>
        Hello = 2,

        /// <summary>Host to client: admitted; carries the session correlation and roster.</summary>
        Welcome = 3,

        /// <summary>Either direction: refused, with the exact reason and a bounded detail.</summary>
        Reject = 4,

        /// <summary>Client to host: claim one seat by id.</summary>
        ClaimSeat = 5,

        /// <summary>Host to the claiming client: the seat is yours.</summary>
        SeatGranted = 6,

        /// <summary>Host to every admitted client: the seat map changed.</summary>
        RosterUpdate = 7,

        /// <summary>Either direction: an orderly close with nothing wrong.</summary>
        Goodbye = 8,

        /// <summary>
        /// Milestone 8.32. An encrypted, authenticated frame carrying exactly one of the other
        /// kinds; see <see cref="PrivateSessionSecureChannel"/>. On a keyed session every frame
        /// after a verified key proof travels as this.
        /// </summary>
        Sealed = 9,

        /// <summary>
        /// Milestone 8.32. Client to host: one gameplay submission - envelope, submission
        /// identity, expected projection version, and the bounded command DTO.
        /// </summary>
        SubmitCommand = 10,

        /// <summary>Milestone 8.32. Host to the submitting client: what its submission did.</summary>
        SubmissionResult = 11,

        /// <summary>
        /// Milestone 8.32. Host to one seated client: that seat's own owner-scoped projection.
        /// Never broadcast as one shared payload - each seat receives its own encoding.
        /// </summary>
        SeatProjection = 12,

        /// <summary>
        /// Milestone 8.33. Client to host: one bounded public agreement statement - envelope,
        /// submission identity, expected projection version, and the typed statement - for the
        /// tied-Initiative and Minion Battle families whose final rules choice only the host's
        /// unanimity may submit.
        /// </summary>
        SubmitAgreement = 13,

        /// <summary>Milestone 8.33. Host to the stating client: what its statement did.</summary>
        AgreementResult = 14,

        /// <summary>
        /// Milestone 8.33. Client to host: this seat detected a gap in the host's ordered stream
        /// and asks for one bounded current-projection resync. The answer is an ordinary
        /// <see cref="SeatProjection"/> push echoing the request's correlation.
        /// </summary>
        RequestResync = 15,

        /// <summary>
        /// Milestone 8.34. Client to host: return to a seat this session reserved, presenting the
        /// reconnect capability the host issued when it granted that seat. A plain
        /// <see cref="ClaimSeat"/> of a reserved seat stays refused - a reservation is one
        /// player's, and only the capability says who that player is.
        /// </summary>
        ReclaimSeat = 16
    }

    /// <summary>
    /// Milestone 8.31. Why the session layer refused something. Distinct from
    /// <see cref="SeatSubmissionRejectionReason"/> for the reason that one is distinct from the
    /// session's own: this layer answers "may this connection be here and hold that seat", never
    /// "is that command allowed".
    /// </summary>
    public enum SessionRejectionReason
    {
        None = 0,

        // The admission tuple, in the order FirstMismatch checks it.
        ProtocolFamilyMismatch = 1,
        ProtocolVersionMismatch = 2,
        BuildIdentityMismatch = 3,
        SnapshotSchemaMismatch = 4,
        MapRevisionMismatch = 5,
        ContentDigestMismatch = 6,

        /// <summary>The key proof did not answer the challenge.</summary>
        SessionKeyMismatch = 7,

        // Seat claims.
        SeatAlreadyClaimed = 8,
        SeatReserved = 9,
        UnknownSeat = 10,
        SeatChangeRefused = 11,
        SessionFull = 12,

        // Connection and frame discipline.
        TooManyConnections = 13,
        MalformedFrame = 14,
        OversizedFrame = 15,
        UnexpectedMessage = 16,
        HandshakeTimeout = 17,
        ProtocolViolation = 18,

        /// <summary>
        /// Milestone 8.32. A gameplay envelope named the wrong session, epoch, or sequence. A
        /// hostile or broken peer, refused at the protocol layer before any seat-level answer.
        /// </summary>
        EnvelopeMismatch = 19,

        /// <summary>
        /// Milestone 8.32. A keyed session received a plaintext frame where only a sealed one is
        /// acceptable, or a sealed frame that failed to unseal.
        /// </summary>
        SealViolation = 20,

        /// <summary>
        /// Milestone 8.34. A reclaim the host will not honour: no reservation stands for that
        /// seat, or the presented capability is not the one that seat's grant issued.
        /// </summary>
        /// <remarks>
        /// One reason answers both, deliberately. Which seats are reserved is already public in
        /// the roster, so nothing is hidden by naming that case; whether a presented capability
        /// was close to the right one is not something a refusal may say at all.
        /// </remarks>
        ReconnectNotAuthorized = 21
    }

    /// <summary>Milestone 8.31. Whether a seat is open, held by a live connection, or reserved.</summary>
    public enum SeatOccupancyState
    {
        Open = 0,
        Claimed = 1,

        /// <summary>
        /// Claimed, then its connection was lost. Reserved stays unclaimable by an ordinary claim
        /// for the rest of the session: releasing it would hand one player's seat to whoever asks
        /// next.
        ///
        /// <para>Milestone 8.34 gives the reservation its one way back: the connection that held
        /// the seat received a reconnect capability with its grant, and presenting that capability
        /// on a <see cref="SessionMessageKind.ReclaimSeat"/> is what turns a reservation into a
        /// claim again - under a rotated connection epoch, and never for anyone else. The
        /// reservation lives exactly as long as the host session does.</para>
        /// </summary>
        Reserved = 2
    }

    /// <summary>
    /// Milestone 8.31. One seat's public row in the roster: who the seat is, and whether it is
    /// taken. All of it is public at a real table; no owner-private state rides here.
    /// </summary>
    public sealed class SeatRosterEntry
    {
        public SeatRosterEntry(SeatId seat, HeroId hero, TeamSide teamSide, SeatOccupancyState occupancy)
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A roster entry requires a seat.", nameof(seat));
            }

            Seat = seat;
            Hero = hero;
            TeamSide = teamSide;
            Occupancy = occupancy;
        }

        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide TeamSide { get; }
        public SeatOccupancyState Occupancy { get; }
    }

    /// <summary>The host's opening message: the nonce a key proof must answer.</summary>
    public sealed class SessionChallengeMessage
    {
        public SessionChallengeMessage(byte[] nonce)
        {
            Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));
            if (nonce.Length > PrivateSessionProtocol.MaxNonceBytes)
            {
                throw new ArgumentException("A challenge nonce is bounded.", nameof(nonce));
            }
        }

        public byte[] Nonce { get; }
    }

    /// <summary>The client's admission request: its tuple and its key proof.</summary>
    public sealed class SessionHelloMessage
    {
        public SessionHelloMessage(PrivateSessionIdentity identity, string keyProof)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            KeyProof = keyProof ?? throw new ArgumentNullException(nameof(keyProof));
        }

        public PrivateSessionIdentity Identity { get; }

        /// <summary>Empty when the client holds no key; never the key itself.</summary>
        public string KeyProof { get; }
    }

    /// <summary>The host's admission answer: in, with the current roster.</summary>
    public sealed class SessionWelcomeMessage
    {
        public SessionWelcomeMessage(string sessionCorrelation, IReadOnlyList<SeatRosterEntry> roster)
        {
            if (string.IsNullOrWhiteSpace(sessionCorrelation))
            {
                throw new ArgumentException("A welcome requires a correlation.", nameof(sessionCorrelation));
            }

            SessionCorrelation = sessionCorrelation;
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        /// <summary>An opaque, content-derived id for logs; never an address or a secret.</summary>
        public string SessionCorrelation { get; }

        public IReadOnlyList<SeatRosterEntry> Roster { get; }
    }

    /// <summary>A refusal: the exact reason, and a bounded human-readable detail.</summary>
    public sealed class SessionRejectMessage
    {
        public SessionRejectMessage(SessionRejectionReason reason, string detail)
        {
            if (reason == SessionRejectionReason.None)
            {
                throw new ArgumentException("A rejection requires a reason.", nameof(reason));
            }

            Reason = reason;
            Detail = detail ?? string.Empty;
            if (Detail.Length > PrivateSessionProtocol.MaxRejectDetailChars)
            {
                Detail = Detail.Substring(0, PrivateSessionProtocol.MaxRejectDetailChars);
            }
        }

        public SessionRejectionReason Reason { get; }
        public string Detail { get; }
    }

    /// <summary>The client's claim of one seat by id.</summary>
    public sealed class SeatClaimMessage
    {
        public SeatClaimMessage(SeatId seat)
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A claim requires a seat.", nameof(seat));
            }

            Seat = seat;
        }

        public SeatId Seat { get; }
    }

    /// <summary>The host's grant: the claimed seat's identity row plus the running count.</summary>
    /// <remarks>
    /// Milestone 8.34 adds the two things the grant now has to say beside "this seat is yours":
    /// which connection epoch this connection speaks under, and the capability that will bring the
    /// seat back if the connection is lost. Both go to exactly one recipient - the connection that
    /// was granted the seat - and neither appears in the roster the host broadcasts, in a
    /// diagnostic record, or in any projection.
    /// </remarks>
    public sealed class SeatGrantedMessage
    {
        public SeatGrantedMessage(
            SeatRosterEntry seat,
            int claimedSeats,
            int totalSeats,
            int connectionEpoch = PrivateSessionProtocol.InitialConnectionEpoch,
            string reconnectCapability = "")
        {
            Seat = seat ?? throw new ArgumentNullException(nameof(seat));
            if (connectionEpoch < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(connectionEpoch), connectionEpoch, "A connection epoch counts from 1.");
            }

            ClaimedSeats = claimedSeats;
            TotalSeats = totalSeats;
            ConnectionEpoch = connectionEpoch;
            ReconnectCapability = reconnectCapability ?? string.Empty;
            if (ReconnectCapability.Length > PrivateSessionProtocol.MaxReconnectCapabilityChars)
            {
                throw new ArgumentException(
                    "A reconnect capability is bounded.", nameof(reconnectCapability));
            }
        }

        public SeatRosterEntry Seat { get; }
        public int ClaimedSeats { get; }
        public int TotalSeats { get; }

        /// <summary>
        /// Milestone 8.34. The epoch every gameplay envelope this connection sends must carry, and
        /// the one the host stamps on everything it sends back. A returning seat is granted the
        /// next one, so a frame from the connection it replaced can never be read as current.
        /// </summary>
        public int ConnectionEpoch { get; }

        /// <summary>
        /// Milestone 8.34. The owner-private token that brings this seat back after a lost
        /// connection, and empty when the host cannot issue one. It is not a rules capability and
        /// carries no authority of its own: presenting it asks the host to bind a new connection
        /// to the seat this one already held.
        /// </summary>
        public string ReconnectCapability { get; }
    }

    /// <summary>
    /// Milestone 8.34. The returning client's request: the seat it held, and the capability the
    /// host issued when it granted that seat.
    /// </summary>
    /// <remarks>
    /// It names a seat exactly as <see cref="SeatClaimMessage"/> does, and naming one proves
    /// nothing: the host answers a reclaim only where that seat's own reservation stands and the
    /// capability is the one it issued for it.
    /// </remarks>
    public sealed class SeatReclaimMessage
    {
        public SeatReclaimMessage(SeatId seat, string capability)
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A reclaim requires a seat.", nameof(seat));
            }

            Seat = seat;
            Capability = capability ?? string.Empty;
            if (Capability.Length > PrivateSessionProtocol.MaxReconnectCapabilityChars)
            {
                throw new ArgumentException("A reconnect capability is bounded.", nameof(capability));
            }
        }

        public SeatId Seat { get; }
        public string Capability { get; }
    }

    /// <summary>The whole current seat map, sent to every admitted connection when it changes.</summary>
    public sealed class SeatRosterMessage
    {
        public SeatRosterMessage(IReadOnlyList<SeatRosterEntry> roster)
        {
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public IReadOnlyList<SeatRosterEntry> Roster { get; }
    }
}
