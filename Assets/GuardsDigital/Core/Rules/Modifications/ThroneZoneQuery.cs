using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one place a team side is turned into map data, and the one place a team's Throne zone is
    /// derived from it (milestone 6.10).
    ///
    /// Map zones and spawn points name their owner as a map-local string, deliberately: the map format
    /// knows nothing about <see cref="TeamSide"/>, so something has to translate. Before this milestone
    /// that translation lived privately inside Quick Game setup and the Throne derivation was written
    /// inline beside it, which was fine while setup was the only asker. p.8's Zone stage is the second
    /// asker - "The next Zone towards the losing team's Throne becomes the new Battle Zone" - and two
    /// derivations of one team's Throne is the shape milestone 6.5ad was written to prevent.
    /// </summary>
    public static class ThroneZoneQuery
    {
        /// <summary>The map-local team identifier a side's zones and spawn points carry.</summary>
        public static string GetMapTeamId(TeamSide side)
        {
            if (side == TeamSide.Red) return "red";
            if (side == TeamSide.Blue) return "blue";
            throw new ArgumentOutOfRangeException(nameof(side), side, "Only Red and Blue own map data.");
        }

        /// <summary>
        /// The Throne zone belonging to <paramref name="side"/>, and whether the map declares one. The
        /// probe form rather than a throwing one, because both askers have something to say when it is
        /// absent: setup rejects the configuration, and the Zone stage refuses the push.
        /// </summary>
        public static bool TryGetThroneZone(BoardMapDefinition map, TeamSide side, out BoardMapZone zone)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var mapTeamId = GetMapTeamId(side);
            zone = map.Zones.SingleOrDefault(value =>
                value.IsThrone && string.Equals(value.TeamId, mapTeamId, StringComparison.Ordinal));
            return zone != null;
        }
    }
}
