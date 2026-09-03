using System.Globalization;
using GuardsDigital.Core.Rules.Victory;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.36. The one line a private-online process writes about how its match ended.
    /// </summary>
    /// <remarks>
    /// <para>Milestone 8.35c proved host/client convergence from markers the rehearsal driver
    /// emitted, and that driver is exactly what an invited playtest does not have: six people press
    /// the controls instead. An ordinary client's log therefore said nothing at all about which
    /// ending it reached, so the returned bundle could only be compared against what a player
    /// remembered seeing. This is the smallest replacement - one bounded line per process, written
    /// once - so the same comparison can be made from the logs themselves.</para>
    /// <para>It cannot disclose owner-private state, and the reason is structural rather than
    /// careful: the only things it is given are the seat this process already knows it holds, the
    /// authoritative version it already reports, and the public <see cref="MatchOutcome"/> the
    /// whole table reads when a match ends. A hand, an unrevealed commitment identity, an
    /// owner-only offer or a reconnect capability has no parameter to arrive through.</para>
    /// </remarks>
    public static class PrivateOnlineDiagnostics
    {
        /// <summary>The marker an ended private-online process writes exactly once.</summary>
        public const string OutcomeMarker = "MATCH_OUTCOME";

        /// <summary>
        /// The ended match's public outcome as one line, or an empty string while the match is
        /// still being played - a process with nothing to report writes nothing.
        /// </summary>
        /// <param name="seatId">
        /// The reporting seat, or an empty value for the dedicated seatless host, which holds none.
        /// </param>
        /// <param name="version">The authoritative projection version this process ended on.</param>
        /// <param name="outcome">The public ending, which is the same for every seat at the table.</param>
        public static string OutcomeLine(string seatId, long version, MatchOutcome outcome)
        {
            if (!outcome.HasEnded)
            {
                return string.Empty;
            }

            var seat = string.IsNullOrWhiteSpace(seatId) ? string.Empty : " seat=" + seatId;
            return OutcomeMarker + seat +
                " version=" + version.ToString(CultureInfo.InvariantCulture) +
                " cause=" + outcome.Cause +
                " winner=" + outcome.WinningTeamId.Value;
        }
    }
}
