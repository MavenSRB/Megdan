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
        ReclaimSeat = 16,

        /// <summary>
        /// Milestone 8.35a. Client to host: this seat is ready, or is not. One bool, from a seated
        /// connection about its own seat and no other.
        /// </summary>
        /// <remarks>
        /// Readiness is a lobby state the protocol had no word for, and the lobby cannot invent it:
        /// a surface that decided locally who was ready would be deciding when six people start a
        /// match, which is authority. So it travels, the host owns it, and every seat reads the
        /// same public answer out of the roster.
        /// </remarks>
        SetReadiness = 17,

        /// <summary>
        /// Milestone 8.35a. Host to every seated connection, exactly once: every seat is claimed by
        /// a live connection and every seat is ready, so this session has left the lobby.
        /// </summary>
        /// <remarks>
        /// Host-derived and one-way. No client asks for it and none may refuse it; a later
        /// disconnect reserves a seat without returning the session to the lobby, because a match
        /// that has begun does not un-begin. A connection admitted but not yet seated never
        /// receives it - it is a statement about the seats.
        /// </remarks>
        MatchStarted = 18,

        /// <summary>
        /// Milestone 8.35f. Seated connection to host: step this seat's own turn back by one
        /// accepted command.
        /// </summary>
        /// <remarks>
        /// Its own kind rather than a <see cref="SubmitCommand"/> carrying an undo command, for the
        /// reason <see cref="SeatUndoSubmission"/> gives: an undo removes from the recorded stream
        /// and can never be in it. The payload is the gameplay envelope, the submission id and the
        /// expected version, and nothing else - there is nothing to target.
        /// </remarks>
        SubmitUndo = 19,

        /// <summary>Milestone 8.35f. Host to that connection: what its rewind did, or why not.</summary>
        UndoResult = 20,

        /// <summary>
        /// Milestone 8.35z37. Seated connection to host, before the match starts: the hero
        /// this seat picks, by identity.
        /// </summary>
        /// <remarks>
        /// <para>It carries no seat, for the reason <see cref="SetReadiness"/> carries none: the
        /// connection the frame arrived on is what says whose pick it is, so there is no field
        /// through which one player could pick for another. It carries no team either - which side a
        /// seat plays for is the seat's own fact and the host already holds it.</para>
        /// <para>The ANSWER needs no kind of its own in either direction. An accepted pick appears
        /// in the roster the host already broadcasts, whose rows have carried a hero id since
        /// milestone 8.31; a refused one rides the reject path every other lobby refusal rides. That
        /// is the whole downstream cost of hero selection, and it was measured before this kind was
        /// added rather than discovered afterwards.</para>
        /// </remarks>
        ChooseHero = 21,

        SubmitCoordination = 22,
        TimerReadiness = 23,
        BoardVote = 24,
        ClockUpdate = 25

        // Milestone 8.35r deliberately added NO kind here. The seat-public match log rides inside
        // SeatProjection's own message instead, and the reason is a measurement rather than a
        // preference: a separate host frame consumes a gameplay envelope sequence, and the
        // fault-injection suite showed it absorbing the very gaps a dropped projection has to leave
        // behind for the client's recovery to see. Sharing the projection's frame gives the log that
        // whole ordered-stream discipline for free. See NetworkSeatProjectionMessage.MatchLog.
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
        ReconnectNotAuthorized = 21,

        /// <summary>
        /// ELEVENTH amendment. The two peers composed different rulebook setups - a different
        /// player count, a different game length, or both.
        /// </summary>
        /// <remarks>
        /// Its number is appended after every existing reason because these values are wire codes,
        /// but <see cref="PrivateSessionIdentity.FirstMismatch"/> checks it between the map revision
        /// and the content digest. Nothing may infer a check order from an enum value.
        /// </remarks>
        MatchFormatMismatch = 22,

        /// <summary>
        /// Milestone 8.35z37. The host would not take that hero pick - it was out of turn,
        /// already taken, unregistered, or the line-up is already settled.
        /// </summary>
        /// <remarks>
        /// <para>One reason for all of rulebook p.22's refusals rather than five wire codes, and the
        /// detail carries which. The split is deliberate and follows this enum's own stated law: this
        /// layer answers "may this connection be here and hold that seat", and every one of these is
        /// a selection rule rather than an admission question. Spending five admission codes on them
        /// would put the pick order into the layer that decides who may connect.</para>
        /// <para>The words come from <c>HeroSelectionLabels.Refusal</c>, which is exhaustive over the
        /// refusal enum, so a reason added to the stage later reaches a player as a sentence rather
        /// than as a blank detail - the same guarantee <c>PrivateLobbyLabels</c> gives.</para>
        /// </remarks>
        HeroSelectionRefused = 23
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
        public SeatRosterEntry(
            SeatId seat,
            HeroId hero,
            TeamSide teamSide,
            SeatOccupancyState occupancy,
            bool isReady = false,
            bool hasChosenHero = false,
            string playerName = "")
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A roster entry requires a seat.", nameof(seat));
            }

            Seat = seat;
            Hero = hero;
            TeamSide = teamSide;
            Occupancy = occupancy;
            IsReady = isReady;
            HasChosenHero = hasChosenHero;
            PlayerName = PrivateLobbyEntryText.CleanPlayerName(playerName);
        }

        /// <summary>
        /// Milestone 8.108. The name the seat's player typed, cleaned by
        /// <see cref="PrivateLobbyEntryText.CleanPlayerName"/>. It is empty when they typed none
        /// and while the seat is open. It is public at a real table, like the rest of the row, and
        /// it is shown in the lobby only.
        /// </summary>
        public string PlayerName { get; }

        /// <summary>
        /// Milestone 8.35z37. Whether this row's hero was PICKED by that seat, rather than
        /// being the hero the provisional composition happens to have put there.
        /// </summary>
        /// <remarks>
        /// <para><b>Without this bit a client cannot draw a selection screen at all</b>, and the
        /// reason is worth stating because it is not obvious: the hero column is never empty. A
        /// session composes a provisional match before it can admit anybody - the admission tuple is
        /// derived from a composed configuration - so every seat already names a hero from the
        /// moment the roster exists, and "hero-wasp on red-player-01" is indistinguishable between a
        /// seat that chose Wasp and a seat that has not picked at all.</para>
        /// <para>It is public exactly as the rest of the row is: whether somebody has picked is
        /// visible at a real table the moment they put the Profile card down. It says nothing about
        /// what anybody is going to pick.</para>
        /// </remarks>
        public bool HasChosenHero { get; }

        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide TeamSide { get; }
        public SeatOccupancyState Occupancy { get; }

        /// <summary>
        /// Milestone 8.35a. Whether this seat has said it is ready to begin. Public at a real
        /// table, so it rides the public roster rather than being told to one connection.
        /// </summary>
        /// <remarks>
        /// It is the host's answer, not the client's request: a seat is ready when the host says
        /// the roster says so. A seat that is not <see cref="SeatOccupancyState.Claimed"/> is never
        /// ready - a chair nobody is sitting in cannot have agreed to anything - and losing a
        /// connection before the match begins clears it for that reason.
        /// </remarks>
        public bool IsReady { get; }
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
        public SessionWelcomeMessage(
            string sessionCorrelation,
            IReadOnlyList<SeatRosterEntry> roster,
            bool matchStarted = false,
            TeamSide firstPickingTeam = TeamSide.None)
        {
            if (string.IsNullOrWhiteSpace(sessionCorrelation))
            {
                throw new ArgumentException("A welcome requires a correlation.", nameof(sessionCorrelation));
            }

            SessionCorrelation = sessionCorrelation;
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            MatchStarted = matchStarted;
            FirstPickingTeam = firstPickingTeam;
        }

        /// <summary>
        /// Milestone 8.35z37. p.22's coin, so a client that joins mid-selection can draw the
        /// pick order from its very first roster. See <see cref="SeatRosterMessage.FirstPickingTeam"/>.
        /// </summary>
        public TeamSide FirstPickingTeam { get; }

        /// <summary>An opaque, content-derived id for logs; never an address or a secret.</summary>
        public string SessionCorrelation { get; }

        public IReadOnlyList<SeatRosterEntry> Roster { get; }

        /// <summary>
        /// Milestone 8.35a. Whether this session has already left the lobby.
        /// </summary>
        /// <remarks>
        /// The phase rides the admission answer rather than only the
        /// <see cref="SessionMessageKind.MatchStarted"/> broadcast, because a returning player was
        /// not there to hear the broadcast: a reconnect re-runs the whole handshake, so the Welcome
        /// is where it learns that what it is returning to is a match rather than a lobby. It is
        /// public session state, exactly like the roster beside it.
        /// </remarks>
        public bool MatchStarted { get; }
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

    /// <summary>The client's claim of one seat by id, and since 8.108 the name to show on it.</summary>
    public sealed class SeatClaimMessage
    {
        public SeatClaimMessage(SeatId seat, string playerName = "")
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A claim requires a seat.", nameof(seat));
            }

            Seat = seat;
            PlayerName = PrivateLobbyEntryText.CleanPlayerName(playerName);
        }

        public SeatId Seat { get; }

        /// <summary>
        /// Milestone 8.108. The claiming player's name, already cleaned. It rides the claim rather
        /// than the Hello because a keyed session seals every frame after the key proof, and the
        /// Hello carries that proof in the clear.
        /// </summary>
        public string PlayerName { get; }
    }

    /// <summary>
    /// Milestone 8.35z37. One seated connection's hero pick: an identity, and nothing that
    /// names a seat.
    /// </summary>
    /// <remarks>
    /// Whether the identity is one this build can deal is not checked here. It is checked by the
    /// selection stage against the live registration, which is the one place that knows - and
    /// checking it twice, in two places that could disagree, is how a decode and a rule drift apart.
    /// This type's whole job is that the id is present and bounded.
    /// </remarks>
    public sealed class HeroChoiceMessage
    {
        public HeroChoiceMessage(string heroIdValue)
        {
            if (string.IsNullOrWhiteSpace(heroIdValue))
            {
                throw new ArgumentException("A pick requires a hero.", nameof(heroIdValue));
            }

            if (heroIdValue.Length > PrivateSessionProtocol.MaxHeroIdChars)
            {
                throw new ArgumentException(
                    "A hero id may carry at most " +
                    PrivateSessionProtocol.MaxHeroIdChars + " characters.", nameof(heroIdValue));
            }

            HeroIdValue = heroIdValue;
        }

        public string HeroIdValue { get; }
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

    /// <summary>
    /// Milestone 8.35a. One seated connection's statement about its own readiness.
    /// </summary>
    /// <remarks>
    /// It names no seat, deliberately. The seat is the connection's, bound by the grant, and a
    /// payload that named one would be a claim about identity - exactly the shape milestone 8.30a's
    /// caller-identity rule refuses. Setting the value it already holds is accepted and changes
    /// nothing, so a surface may send what it believes without having to know what the host holds.
    /// </remarks>
    public sealed class SeatReadinessMessage
    {
        public SeatReadinessMessage(bool isReady)
        {
            IsReady = isReady;
        }

        public bool IsReady { get; }
    }

    /// <summary>The whole current seat map, sent to every admitted connection when it changes.</summary>
    public sealed class SeatRosterMessage
    {
        public SeatRosterMessage(
            IReadOnlyList<SeatRosterEntry> roster,
            TeamSide firstPickingTeam = TeamSide.None)
        {
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            FirstPickingTeam = firstPickingTeam;
        }

        public IReadOnlyList<SeatRosterEntry> Roster { get; }

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's "the team showing on the Tie Breaker coin",
        /// which is the team that picks first and therefore the whole pick ORDER.
        /// </summary>
        /// <remarks>
        /// <para>Carried because a client cannot derive it, and only in one case - which is why it
        /// is worth four bytes rather than a rule. Once anybody has picked, the first chosen row's
        /// team IS the first picking team, because picks arrive in the printed order. Before the
        /// first pick there is nothing to infer from, and that is exactly the moment a selection
        /// screen has to say whose turn it is.</para>
        /// <para>Public: the Tie Breaker coin is face up on the table from setup step 6, and this
        /// build already shows it. <see cref="TeamSide.None"/> means a session that is not running a
        /// selection, which is every path that predates this milestone.</para>
        /// </remarks>
        public TeamSide FirstPickingTeam { get; }
    }
}
