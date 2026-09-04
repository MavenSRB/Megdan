using System;
using System.Collections.Generic;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35k. Where a seat's reconnect capability lives between one connection and the
    /// next, so a player whose process died has a road back to their own seat.
    /// </summary>
    /// <remarks>
    /// <para>Milestone 8.34 built the whole return mechanism and it works: a reserved seat, a
    /// host-minted bearer capability, a reclaim message, a rotated epoch. What it never had was a
    /// road to it. The capability lived in one private field of one live client object, and three
    /// ordinary player actions end that object - the process exiting, the process crashing, and
    /// pressing LEAVE - after which the lobby could only offer a FIRST join, which the host refuses
    /// by name because the seat is reserved. This is that road.</para>
    /// <para><b>It is a secret at rest and it is treated as one.</b> An entry is scoped to one
    /// session correlation and one seat, so it cannot be replayed into a different session; it is
    /// dropped the moment the session it belongs to ends, whichever way it ends; and no surface can
    /// read it. The client consults this store itself and hands what it finds straight to the wire -
    /// the capability never becomes a property, never reaches a roster, a projection, a log line or
    /// a diagnostic marker, which is the classification `PRIVATE_ONLINE_ARCHITECTURE.md` gives it
    /// alongside the hand.</para>
    /// <para><b>Holding it is not authority.</b> The host's own checks are untouched: a reclaim
    /// still requires the seat to be Reserved, a non-empty stored capability, and a constant-time
    /// match, and both wrong-capability cases still answer identically. The session key still
    /// proves nothing about a seat - every invited player holds it, and if it sufficed any of them
    /// could seize a dropped player's seat and act with their hand.</para>
    /// <para><b>Where the file-backed implementation lives.</b> This layer states the storage
    /// contract only. The concrete type that knows a filesystem detail - one small file per session
    /// and seat, under a caller-supplied directory - lives beneath the transport-neutral port in
    /// <c>GuardsDigital.Network</c> (<c>FileRetainedSeatCapabilityStore</c>), exactly where
    /// <c>CryptoNonceSource</c> already puts platform entropy for the identical reason: the
    /// conformance scan that keeps a socket or engine type out of this layer does not govern the
    /// adapter assembly beneath it.</para>
    /// </remarks>
    public interface IRetainedSeatCapabilityStore
    {
        /// <summary>The capability retained for this seat of this session, or empty when none is.</summary>
        string Find(string sessionCorrelation, SeatId seat);

        /// <summary>Retains the capability the host issued with a grant.</summary>
        void Retain(string sessionCorrelation, SeatId seat, string capability);

        /// <summary>
        /// Drops this seat's retained capability. Called when the session ends and when the player
        /// deliberately leaves, which forfeits the seat.
        /// </summary>
        void Forget(string sessionCorrelation, SeatId seat);
    }

    /// <summary>
    /// Milestone 8.35k. An in-memory store, for fixtures and for a client that should retain
    /// nothing at all.
    /// </summary>
    public sealed class InMemoryRetainedSeatCapabilityStore : IRetainedSeatCapabilityStore
    {
        private readonly Dictionary<string, string> entries =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public int Count => entries.Count;

        public string Find(string sessionCorrelation, SeatId seat) =>
            entries.TryGetValue(KeyFor(sessionCorrelation, seat), out var capability)
                ? capability
                : string.Empty;

        public void Retain(string sessionCorrelation, SeatId seat, string capability)
        {
            if (string.IsNullOrEmpty(capability) ||
                string.IsNullOrWhiteSpace(sessionCorrelation) ||
                !seat.IsDefined)
            {
                return;
            }

            entries[KeyFor(sessionCorrelation, seat)] = capability;
        }

        public void Forget(string sessionCorrelation, SeatId seat) =>
            entries.Remove(KeyFor(sessionCorrelation, seat));

        /// <summary>
        /// Milestone 8.35k. The separator is load-bearing and was an empty string until this
        /// milestone, which made <c>("AB", "C")</c> and <c>("A", "BC")</c> one entry.
        /// </summary>
        /// <remarks>
        /// Unreachable with today's fixed seat vocabulary, and it fails safe when reached - a
        /// capability found under the wrong key is one the host then refuses. It is corrected
        /// anyway, because "no caller can currently collide" is the wrong thing for a type that
        /// decides which secret is handed to the wire for which seat to rest on, and because the
        /// file-backed implementation of this same contract already separates its two parts. A pipe
        /// occurs in neither a host-minted correlation nor a configured seat id.
        /// </remarks>
        private static string KeyFor(string sessionCorrelation, SeatId seat) =>
            (sessionCorrelation ?? string.Empty) + "|" +
            (seat.IsDefined ? seat.Value : string.Empty);
    }
}
