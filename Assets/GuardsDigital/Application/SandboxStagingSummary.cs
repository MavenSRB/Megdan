using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>
    /// TWELFTH amendment. What a staged board actually staged, in the words a tester reads off the
    /// Director-only sandbox banner. Milestone 8.35z18.
    ///
    /// <para><b>It is derived from the description rather than recorded beside it</b>, so the banner
    /// cannot drift from the board: there is one staging, and both the composed match and these
    /// lines are read off it. A summary that were assembled by whoever wrote the staging file could
    /// say anything, and the one thing this surface exists to guarantee is that a tester always
    /// knows which board is in front of them.</para>
    ///
    /// <para><b>Engine-free on purpose.</b> It lives in Application beside
    /// <see cref="SandboxMatchComposition"/> rather than in Presentation so the Unity-free harness
    /// covers it; the HUD view holds nothing but the strings this produces. That follows
    /// <c>PrivateLobbyModel</c>, which is a display model in this assembly for the same reason.</para>
    ///
    /// <para>"Sandbox" here is the staged-board test environment of the TWELFTH amendment. It is
    /// unrelated to the agent process-isolation sandbox <c>Docs/AI/Testing/POLICY.md</c> calls by
    /// the same word.</para>
    /// </summary>
    public static class SandboxStagingSummary
    {
        /// <summary>
        /// The banner's title. Constant rather than derived, because its job is to be recognised at
        /// a glance in a screenshot a tester pastes into a report.
        /// </summary>
        public const string Title = "SANDBOX";

        /// <summary>
        /// The one sentence that states the constraint DEV-004 puts on this surface, shown whenever
        /// the banner is. It is here rather than only in a doc because the person who most needs to
        /// know a staged board is not a real match is the one looking at one.
        /// </summary>
        public const string DirectorOnlyNote =
            "Director-only staged board. Not available in online seats.";

        /// <summary>
        /// One line per thing the staging changed, in a fixed order, and exactly one line reading
        /// "Empty staging (the dealt board)" when it changed nothing.
        /// </summary>
        /// <remarks>
        /// The empty case is a real case rather than a defensive one: composing an empty staging is
        /// how the core milestone proves the seam is inert, and a tester who loads a staging file
        /// that turned out to describe nothing needs to be told that rather than shown a blank
        /// panel they will read as a bug.
        /// </remarks>
        public static IReadOnlyList<string> Describe(SandboxStagingDescription description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));

            var lines = new List<string>();
            var placed = description.Heroes.Where(hero => hero.Coordinate.HasValue).ToArray();
            if (placed.Length > 0)
            {
                lines.Add(Count(placed.Length, "hero", "heroes") + " placed");
            }

            var levelled = description.Heroes
                .Where(hero => hero.Level.HasValue || hero.Coins.HasValue)
                .ToArray();
            if (levelled.Length > 0)
            {
                lines.Add(Count(levelled.Length, "hero", "heroes") + " given a Level or coins");
            }

            // Hand is null for a hero whose hand was not staged at all, and an EMPTY list for one
            // deliberately staged with no cards - two different facts, and only the second is a
            // staged hand. Reading Count off the first would throw.
            var handed = description.Heroes.Where(hero => hero.Hand != null).ToArray();
            if (handed.Length > 0)
            {
                lines.Add(Count(handed.Length, "hero", "heroes") + " given a chosen hand");
            }

            if (description.SpawnedMinions.Count > 0)
            {
                lines.Add(Count(description.SpawnedMinions.Count, "minion", "minions") + " spawned");
            }

            if (description.RemovedMinions.Count > 0)
            {
                lines.Add(Count(description.RemovedMinions.Count, "minion", "minions") + " removed");
            }

            if (description.MovedMinions.Count > 0)
            {
                lines.Add(Count(description.MovedMinions.Count, "minion", "minions") + " moved");
            }

            if (description.WaveCount.HasValue)
            {
                lines.Add(Count(description.WaveCount.Value, "Wave counter", "Wave counters"));
            }

            foreach (var team in description.TeamLifeCounts)
            {
                lines.Add(
                    team.Key + ": " + Count(team.Value, "Life counter", "Life counters"));
            }

            if (lines.Count == 0)
            {
                lines.Add("Empty staging (the dealt board)");
            }

            return lines;
        }

        private static string Count(int value, string singular, string plural) =>
            value.ToString(CultureInfo.InvariantCulture) + " " + (value == 1 ? singular : plural);
    }
}
