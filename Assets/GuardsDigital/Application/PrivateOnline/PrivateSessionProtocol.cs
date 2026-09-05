namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.31. The fixed constants of `guards-private-online` protocol version 1's session
    /// slice: what a frame looks like, how large anything may be, and how long an unadmitted
    /// connection may stand.
    /// </summary>
    /// <remarks>
    /// <para>These are protocol facts, not tuning knobs: a change to any of them changes what a
    /// peer accepts, so the amended architecture record's upgrade rule applies - if wire meaning
    /// moves, <see cref="Version"/> moves with it and the prior version is rejected.</para>
    /// <para>The bounds exist so that nothing about an incoming byte sequence is trusted before it
    /// is checked: a declared payload length is validated against <see cref="MaxPayloadBytes"/>
    /// before any buffer for it exists, and a handshake that has not completed by its deadline is
    /// closed rather than held open indefinitely.</para>
    /// </remarks>
    public static class PrivateSessionProtocol
    {
        /// <summary>The protocol family the amended 8.29 decision record pins.</summary>
        public const string FamilyName = "guards-private-online";

        /// <summary>
        /// Protocol version 6: milestone 8.35r opened the seat-public event channel and published
        /// who is being waited on. Three things move, and they move together deliberately - this is
        /// Protocol version 7: milestone 8.35u made rulebook p.15's leveling simultaneous, so the
        /// authority can be waiting on several heroes at once and <c>DecisionOwner</c> carries a
        /// repeated hero group where version 6 wrote one optional hero. A version 6 peer reads the
        /// count where it expects a present/absent boolean, so it does not read a truncated record -
        /// it reads a different one - and version 6 is refused from the bounded header.
        ///
        /// <para><b>What did NOT move, and it is the more interesting half.</b> The paused
        /// transition's own detail row - item, chooser, options - is byte-identical to version 6,
        /// because milestone 8.32's per-seat encoding discipline had already made each seat's payload
        /// its own: the seat projection narrows a pause standing on six heroes to the one item that
        /// seat may answer, so the row has never had more than one item to carry and still does not.
        /// The public half of a simultaneous pause is who is being waited on, and that is exactly the
        /// field that grew. A shape change absorbed by an existing privacy boundary is worth
        /// recording as a property of that boundary rather than as luck.</para>
        ///
        /// <para>The bump is honesty rather than a hole being closed: milestone 8.35u also moves the
        /// snapshot schema to v36, and <c>PrivateSessionIdentity</c> already refuses admission on a
        /// <c>SnapshotSchemaMismatch</c>, so a mixed-version session was impossible before this line
        /// was written. The ratchet is kept anyway - a protocol version that only moves when it has
        /// to is a protocol version nobody trusts.</para>
        ///
        /// <para>Version 6 was the one bump the remaining wire growth was gathered into, rather than
        /// three.</para>
        ///
        /// <para>(1) The seat projection message now carries the match log beside that seat's own
        /// record, as a bounded window of typed lines. A version 5 peer reaches the end of the
        /// record and then meets a line count it has no field for. (2) The projection detail
        /// record gained the standing rewind marker at its end; a version 5 peer reading a version 6
        /// record would run off the end of the payload. (3) <c>DecisionOwner</c> joined
        /// <c>IMatchReadModel</c> - and it is the one part that moves NO bytes, because the seat
        /// projection record has carried it since milestone 8.32 and only the read-model contract
        /// was missing it. It is named here so the record does not read as if it grew for it.</para>
        ///
        /// <para>The event channel is a separate kind rather than a field on the seat projection
        /// because the size was measured, not estimated: the largest complete projection across the
        /// seeded six-seat walk is 30,574 bytes, and the synchronization sweep's own guard is half
        /// the sealed frame bound - 32,747 - leaving 2,173 bytes. A window of recent history does
        /// not fit there, and that guard is a ratchet rather than a budget to spend.</para>
        ///
        /// <para>Protocol version 5: milestone 8.35l gave a greyed action offer a reason. Each offer row
        /// now carries an optional block naming the standing rule modification that refused it - its
        /// kind, the activity barred, and the controlling hero, source card and Active effect. A
        /// version 4 peer would read that block as the next offer's card id and mis-decode the whole
        /// remainder of the detail record, so version 4 is rejected before either side interprets a
        /// payload.
        ///
        /// <para>It is carried rather than derived, and the wire-growth rule's exclusion floor is why:
        /// the only material a seat holds is a flat list of the cards standing effects came off, which
        /// cannot say which of them refused this action, and a seat that worked the answer out for
        /// itself would be re-deriving the rules the host already applied - the exact split the
        /// blocked-reason design forbids. The cost is five fields on offers that are unavailable, and
        /// one bool on every other offer.</para>
        ///
        /// <para>Protocol version 4: milestone 8.35g added the turn release - one new command kind, and
        /// the eleventh pending family carried in the projection detail as
        /// <c>PendingTurnRelease</c>. It is carried rather than derived from the standing decision
        /// owner because the shared status derivation reads the pause itself, and the wire-growth
        /// rule's exclusion floor exists to stop exactly that kind of excuse. A version 3 peer would
        /// read the release row out of the middle of a detail record and mis-decode everything after
        /// it, so it is rejected before either side interprets the payload.
        ///
        /// <para>Version 3 was milestone 8.35f's seat undo - two message kinds and one carried
        /// projection fact, <c>IsSeatUndoAvailable</c>, which a client cannot derive because the
        /// bounds are facts about the host's recorded history. Version 2 applied the same rule to
        /// version 1 when milestone 8.35b gave SelectedActor its Tie Breaker meaning. Every
        /// superseded version is refused from the bounded header rather than reinterpreted.</para>
        /// </summary>
        public const int Version = 7;

        /// <summary>The four ASCII bytes that open every frame: G, D, P, O.</summary>
        public const byte FrameMagic0 = 0x47;
        public const byte FrameMagic1 = 0x44;
        public const byte FrameMagic2 = 0x50;
        public const byte FrameMagic3 = 0x4F;

        /// <summary>Magic (4) + version (2) + kind (2) + payload length (4).</summary>
        public const int FrameHeaderBytes = 12;

        /// <summary>
        /// The largest payload any session message may declare. Validated against the declared
        /// length before a payload buffer is allocated, so an attacker's length field cannot buy an
        /// allocation.
        /// </summary>
        public const int MaxPayloadBytes = 64 * 1024;

        /// <summary>The largest challenge nonce a client will accept.</summary>
        public const int MaxNonceBytes = 64;

        /// <summary>The most characters a rejection detail may carry to a peer.</summary>
        public const int MaxRejectDetailChars = 200;

        /// <summary>The official configuration's six seats.</summary>
        public const int MaxSeats = 6;

        /// <summary>
        /// The most simultaneous connections the host keeps: the six seats plus headroom for a
        /// rejected straggler or two. Anything past this is refused at accept.
        /// </summary>
        public const int MaxConcurrentConnections = 12;

        /// <summary>How long an accepted connection may stand without completing its handshake.</summary>
        public const int HandshakeTimeoutSeconds = 5;

        /// <summary>
        /// Milestone 8.32. Every connection's first epoch. The gameplay envelope carries the
        /// sender's epoch so a stale connection's frames can be told from its replacement's.
        ///
        /// <para>Milestone 8.34 rotates it, exactly as the decision record scheduled: a seat that
        /// returns under its reconnect capability is granted the next epoch, and the envelope of
        /// any frame still stamped with the epoch the lost connection used is refused before it
        /// can reach a seat-level answer.</para>
        /// </summary>
        public const int InitialConnectionEpoch = 1;

        /// <summary>
        /// Milestone 8.34. The most characters a reconnect capability may carry.
        /// </summary>
        /// <remarks>
        /// The capability is the owner-private token the host issues with a seat grant and
        /// requires back before it will return that seat to a new connection. It is bounded like
        /// every other wire string, and it is never the session key: the key is the invitation,
        /// which every invited player holds, while the capability is one seat's own and is what
        /// keeps a reconnect from transferring seat control between invited players.
        /// </remarks>
        public const int MaxReconnectCapabilityChars = 64;

        /// <summary>
        /// Milestone 8.33. How many consumed submission identities the host remembers, so a
        /// retried submission inside the window is recognized as the duplicate it is and one
        /// older than the window is refused rather than executed twice.
        ///
        /// <para>Sized from measured traffic, as the decision record requires: across the
        /// deterministic seed-2 six-seat walk the longest any one seat goes between two of its own
        /// consumed submission identities is measured at 27 by
        /// <c>PrivateOnlineSynchronizationTests.TheIdempotencyWindow_CoversMeasuredRetryTraffic</c>,
        /// so 256 holds every seat's most recent history several times over - enough for a
        /// stale-view retry today and for 8.34's reconnect-and-retry to land inside it. That same
        /// fixture is the growth check: traffic outgrowing the margin fails it, and the constant
        /// is then re-sized deliberately rather than overtaken silently.</para>
        /// </summary>
        public const int SubmissionIdempotencyWindowEntries = 256;

        /// <summary>The default host port; an operator may name another.</summary>
        public const int DefaultPort = 45677;

        /// <summary>
        /// Milestone 8.35a1. Whether a session offered this bind and this key must be refused,
        /// because an unkeyed session may bind only the loopback interface.
        /// </summary>
        /// <remarks>
        /// <para>An unkeyed session has no key to derive channel keys from, so its link stays
        /// plaintext, and the architecture record permits an owner-private byte to cross plaintext
        /// only where its on-path observer does not exist - the loopback interface. A keyed session
        /// is sealed from the key proof on and may bind anything.</para>
        /// <para>The rule is stated here, once, because it has two callers that must not drift
        /// apart: the command line's own host role and the lobby's <c>HOST</c> control. It was
        /// written into the first alone, and milestone 8.36's readiness review found that the
        /// second - the path an operator actually uses to host an invited playtest - would open an
        /// unkeyed public listener. Each surface still says so in its own words; only the decision
        /// is shared.</para>
        /// </remarks>
        public static bool BindRequiresSessionKey(bool bindsLoopback, string sessionKey) =>
            !bindsLoopback && string.IsNullOrEmpty(sessionKey);

        /// <summary>The listener's pending-accept backlog.</summary>
        public const int ListenBacklog = 8;
    }
}
