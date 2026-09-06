using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Milestone 8.35z36. Builds a match's board and its participant graph from a chosen
    /// <see cref="HeroLineup"/> instead of from the printed pairing.
    /// </summary>
    /// <remarks>
    /// <para><b>This is a generalisation, not a change.</b> Every configuration this build has ever
    /// composed put a fixed hero at a fixed spawn point on a fixed side, because the line-up was a
    /// constant. Selection makes the line-up a variable and nothing else: composing from
    /// <see cref="RegisteredHeroRoster.PrintedLineup"/> must produce, element for element, exactly
    /// what <see cref="QuickGameSetupConfiguration.CreateOfficial"/> produces, and that equality is asserted for
    /// both printed formats rather than argued for here. If it ever stops holding, the selector has
    /// changed the shipped board and the fixture says so.</para>
    /// <para><b>What travels with the hero and what travels with the seat.</b> The hero identity,
    /// the printed set and the epithet travel with the HERO - they are what a rule, a token and a
    /// snapshot address, and they are the same wherever the hero is seated. The spawn point, the
    /// occupied space, the player identity and the TEAM travel with the SEAT. Team side is on the
    /// seat's list and that is the entire runtime-team-colour decision expressed in one place: a
    /// hero has no side of its own to bake into anything.</para>
    /// </remarks>
    public static class HeroLineupComposition
    {
        /// <summary>
        /// The player identities each team's seats are filled with, in the order they are filled.
        /// </summary>
        /// <remarks>
        /// Positional exactly as the spawn slots are, and for the same recorded reason: this
        /// build's seats hold no name, device or order of play of their own, so which of a team's
        /// player identities goes with which of its marked points changes nothing any rule,
        /// projection or snapshot can observe. Stating it once keeps a composed match reproducible
        /// from its recorded setup without a second seed - milestone 8.25's ruling, applied to a
        /// line-up that is now chosen rather than printed.
        /// </remarks>
        private static readonly string[] RedPlayerIds =
        {
            OfficialSixPlayerParticipants.RedFirstPlayerIdValue,
            OfficialSixPlayerParticipants.RedSecondPlayerIdValue,
            OfficialSixPlayerParticipants.RedThirdPlayerIdValue
        };

        private static readonly string[] BluePlayerIds =
        {
            OfficialSixPlayerParticipants.BlueFirstPlayerIdValue,
            OfficialSixPlayerParticipants.BlueSecondPlayerIdValue,
            OfficialSixPlayerParticipants.BlueThirdPlayerIdValue
        };

        /// <summary>
        /// The board for one of p.5-6's printed setups with this line-up standing on it: each
        /// team's heroes on that team's marked points in slot order, the format's Wave and Life
        /// counts, and setup step 10's twelve minions.
        /// </summary>
        public static QuickGameSetupConfiguration Compose(MatchFormat format, HeroLineup lineup)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (lineup == null) throw new ArgumentNullException(nameof(lineup));
            if (lineup.HeroesPerTeam != format.HeroesPerTeam)
            {
                throw new ArgumentException(
                    $"A {format.Id} match fields {format.HeroesPerTeam} heroes a side and this " +
                    $"line-up names {lineup.HeroesPerTeam}.",
                    nameof(lineup));
            }

            var mapId = new MapId("official-single-lane");

            var heroes = PlacementsFor(TeamSide.Red, lineup, mapId)
                .Concat(PlacementsFor(TeamSide.Blue, lineup, mapId))
                .ToArray();

            return new QuickGameSetupConfiguration(
                new MapDefinitionReference(mapId, 1),
                format.WaveCount,
                format.LifeCountPerTeam,
                heroes,
                QuickGameSetupConfiguration.OfficialMinionPlacements(mapId));
        }

        /// <summary>
        /// The participant graph for this line-up: two teams, one player per seat, one hero per
        /// player, every hero declaring p.16's dashboard layout as the whole roster does.
        /// </summary>
        public static MatchParticipants ComposeParticipants(HeroLineup lineup)
        {
            if (lineup == null) throw new ArgumentNullException(nameof(lineup));

            var redTeamId = new TeamId(OfficialSixPlayerParticipants.RedTeamIdValue);
            var blueTeamId = new TeamId(OfficialSixPlayerParticipants.BlueTeamIdValue);

            var teams = new[]
            {
                new Team(redTeamId, TeamSide.Red),
                new Team(blueTeamId, TeamSide.Blue)
            };

            var players = new List<Player>();
            var heroes = new List<Hero>();

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var teamId = side == TeamSide.Red ? redTeamId : blueTeamId;
                var playerIds = PlayerIdsFor(side);
                var selected = lineup.ForTeam(side);
                for (var index = 0; index < selected.Count; index++)
                {
                    var playerId = new PlayerId(playerIds[index]);
                    players.Add(new Player(playerId, teamId));
                    heroes.Add(new Hero(
                        selected[index].HeroId, playerId, HeroDashboard.RulebookLayout));
                }
            }

            return new MatchParticipants(teams, players, heroes);
        }

        /// <summary>
        /// Which player identity holds the hero of that identity in this line-up, or false when the
        /// line-up does not contain it.
        /// </summary>
        public static bool TryFindPlayerFor(
            HeroLineup lineup, string heroIdValue, out PlayerId playerId, out TeamSide side)
        {
            if (lineup == null) throw new ArgumentNullException(nameof(lineup));

            foreach (var candidate in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var selected = lineup.ForTeam(candidate);
                var playerIds = PlayerIdsFor(candidate);
                for (var index = 0; index < selected.Count; index++)
                {
                    if (string.Equals(
                            selected[index].HeroIdValue, heroIdValue, StringComparison.Ordinal))
                    {
                        playerId = new PlayerId(playerIds[index]);
                        side = candidate;
                        return true;
                    }
                }
            }

            playerId = default;
            side = TeamSide.None;
            return false;
        }

        private static IEnumerable<QuickGameHeroPlacementDefinition> PlacementsFor(
            TeamSide side, HeroLineup lineup, MapId mapId)
        {
            var slots = RegisteredHeroRoster.SlotsFor(side);
            var selected = lineup.ForTeam(side);
            if (selected.Count > slots.Count)
            {
                throw new ArgumentException(
                    $"The {side} Throne zone has {slots.Count} marked hero spawn points and this " +
                    $"line-up seats {selected.Count} heroes there.");
            }

            for (var index = 0; index < selected.Count; index++)
            {
                var slot = slots[index];
                yield return new QuickGameHeroPlacementDefinition(
                    selected[index].HeroId,
                    selected[index].ContentId,
                    selected[index].DisplayName,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId(slot.SpawnPointId)),
                    new ConfiguredSpaceReference(mapId, new SpaceId(slot.SpaceId)));
            }
        }

        private static string[] PlayerIdsFor(TeamSide side)
        {
            switch (side)
            {
                case TeamSide.Red: return RedPlayerIds;
                case TeamSide.Blue: return BluePlayerIds;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side), side, "A composed team is Red or Blue.");
            }
        }
    }
}
