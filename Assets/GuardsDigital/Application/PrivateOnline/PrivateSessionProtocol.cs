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

        /// <summary>Protocol version 1: session establishment, admission, and seat claims.</summary>
        public const int Version = 1;

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
        /// sender's epoch so a stale connection's frames can be told from its replacement's;
        /// rotation on reconnect is milestone 8.34's, so in this slice any other value is refused.
        /// </summary>
        public const int InitialConnectionEpoch = 1;

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

        /// <summary>The listener's pending-accept backlog.</summary>
        public const int ListenBacklog = 8;
    }
}
