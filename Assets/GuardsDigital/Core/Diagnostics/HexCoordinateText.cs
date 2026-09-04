using System;
using System.Globalization;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Diagnostics
{
    /// <summary>
    /// Milestone 8.35r. The exact inverse of <see cref="HexCoordinate.ToString"/>, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>It exists because the recorded rule-event log states a move as
    /// <c>move=(q, r)-&gt;(q, r)</c>, and the seat-public match log has to read the spaces back out
    /// of that text. Writing the inverse here, beside the only caller, keeps
    /// <see cref="HexCoordinate"/> free of a parser it has never needed.</para>
    /// <para><b>The pairing is the point.</b> A hand-rolled parser for another type's
    /// <c>ToString</c> is an assumption about a format nobody promised to keep. Stating it as one
    /// named function makes that assumption checkable, and
    /// <c>MatchLogDerivationTests.EveryCoordinate_RoundTripsThroughItsOwnLogText</c> is the check:
    /// if the coordinate's own text ever changes, that fixture goes red here rather than the log
    /// quietly losing every origin and destination it prints.</para>
    /// </remarks>
    public static class HexCoordinateText
    {
        /// <summary>Reads a coordinate written as <c>(q, r)</c>, tolerating surrounding space.</summary>
        public static bool TryParse(string text, out HexCoordinate coordinate)
        {
            coordinate = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var trimmed = text.Trim();
            if (trimmed.Length < 5 || trimmed[0] != '(' || trimmed[trimmed.Length - 1] != ')')
            {
                return false;
            }

            var inner = trimmed.Substring(1, trimmed.Length - 2);
            var comma = inner.IndexOf(',');
            if (comma <= 0)
            {
                return false;
            }

            if (!int.TryParse(
                    inner.Substring(0, comma).Trim(),
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out var q) ||
                !int.TryParse(
                    inner.Substring(comma + 1).Trim(),
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out var r))
            {
                return false;
            }

            coordinate = new HexCoordinate(q, r);
            return true;
        }
    }
}
