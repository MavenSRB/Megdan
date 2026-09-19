using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Rules.Victory
{
    /// <summary>
    /// Milestone 8.53. The complete persisted state of a <see cref="MatchScoreboard"/>: one line per
    /// participant hero, in hero-id order.
    /// </summary>
    /// <remarks>
    /// Required and with no absent form, for the reason
    /// <c>GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState</c> is: every match has heroes
    /// and every hero has a scoreboard line from the first command, so a payload carrying no
    /// scoreboard section would be a round whose totals nobody wrote rather than a round whose
    /// heroes have none. An all-zero line is a legal value and the one a fresh match writes.
    /// </remarks>
    public sealed class MatchScoreboardRestoreState
    {
        public MatchScoreboardRestoreState(IEnumerable<HeroScoreboardEntry> entries)
        {
            Entries = Array.AsReadOnly(
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
            if (Entries.Any(entry => entry == null))
            {
                throw new ArgumentException(
                    "A persisted scoreboard carries no null line.", nameof(entries));
            }
        }

        public IReadOnlyList<HeroScoreboardEntry> Entries { get; }

        public void Deconstruct(out IReadOnlyList<HeroScoreboardEntry> entries)
        {
            entries = Entries;
        }
    }
}
