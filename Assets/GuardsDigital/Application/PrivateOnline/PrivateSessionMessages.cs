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
        Goodbye = 8
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
        ProtocolViolation = 18
    }

    /// <summary>Milestone 8.31. Whether a seat is open, held by a live connection, or reserved.</summary>
    public enum SeatOccupancyState
    {
        Open = 0,
        Claimed = 1,

        /// <summary>
        /// Claimed, then its connection was lost. Reserved stays unclaimable for the rest of the
        /// session: with no identity to recognize a returning player, releasing it would hand one
        /// player's seat to whoever asks next. Milestone 8.34's reconnect work is what turns a
        /// reservation back into a claim.
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
    public sealed class SeatGrantedMessage
    {
        public SeatGrantedMessage(SeatRosterEntry seat, int claimedSeats, int totalSeats)
        {
            Seat = seat ?? throw new ArgumentNullException(nameof(seat));
            ClaimedSeats = claimedSeats;
            TotalSeats = totalSeats;
        }

        public SeatRosterEntry Seat { get; }
        public int ClaimedSeats { get; }
        public int TotalSeats { get; }
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
