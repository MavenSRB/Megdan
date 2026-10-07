using System;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
namespace GuardsDigital.Application
{
    /// <summary>Stable seat-owner ordering; hero names and selected content never select a colour.</summary>
    public static class PlayerVisualIdentity
    {
        public static int Index(MatchParticipants participants, HeroId hero)
        {
            var owner = participants.Heroes.First(h => h.Id.Equals(hero)).PlayerId;
            var team = participants.GetTeamForHero(hero);
            var players = participants.Players.Where(p => p.TeamId.Equals(team.Id))
                .OrderBy(p => p.Id.Value, StringComparer.Ordinal).ToArray();
            return (team.Side == TeamSide.Blue ? 3 : 0) + Array.FindIndex(players, p => p.Id.Equals(owner));
        }
    }
}
