using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    public readonly struct TeamId : IEquatable<TeamId>
    {
        public TeamId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Team ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(TeamId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TeamId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public PlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Player ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    public enum TeamSide
    {
        None = 0,
        Red = 1,
        Blue = 2
    }

    public sealed class Team
    {
        public Team(TeamId id, TeamSide side)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Team requires a defined ID.", nameof(id));
            }

            if (side != TeamSide.Red && side != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Team side must be Red or Blue.");
            }

            Id = id;
            Side = side;
        }

        public TeamId Id { get; }

        public TeamSide Side { get; }
    }

    public sealed class Player
    {
        public Player(PlayerId id, TeamId teamId)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Player requires a defined ID.", nameof(id));
            }

            if (!teamId.IsDefined)
            {
                throw new ArgumentException("Player requires a defined team ID.", nameof(teamId));
            }

            Id = id;
            TeamId = teamId;
        }

        public PlayerId Id { get; }

        public TeamId TeamId { get; }
    }

    public sealed class Hero
    {
        public Hero(HeroId id, PlayerId playerId)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Hero requires a defined ID.", nameof(id));
            }

            if (!playerId.IsDefined)
            {
                throw new ArgumentException("Hero requires a defined player ID.", nameof(playerId));
            }

            Id = id;
            PlayerId = playerId;
        }

        public HeroId Id { get; }

        public PlayerId PlayerId { get; }
    }

    public sealed class MatchParticipants
    {
        private readonly IReadOnlyList<Team> teams;
        private readonly IReadOnlyList<Player> players;
        private readonly IReadOnlyList<Hero> heroes;
        private readonly IReadOnlyDictionary<TeamId, Team> teamsById;
        private readonly IReadOnlyDictionary<PlayerId, Player> playersById;
        private readonly IReadOnlyDictionary<HeroId, Hero> heroesById;

        public MatchParticipants(
            IEnumerable<Team> teams,
            IEnumerable<Player> players,
            IEnumerable<Hero> heroes)
        {
            if (teams == null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if (heroes == null)
            {
                throw new ArgumentNullException(nameof(heroes));
            }

            var teamArray = teams.ToArray();
            var playerArray = players.ToArray();
            var heroArray = heroes.ToArray();
            if (teamArray.Length == 0 || playerArray.Length == 0 || heroArray.Length == 0)
            {
                throw new ArgumentException("Match participants require at least one team, player, and hero.");
            }

            var mutableTeamsById = BuildUniqueIndex(
                teamArray,
                team => team != null ? team.Id : default,
                id => id.IsDefined,
                "teams");
            var mutablePlayersById = BuildUniqueIndex(
                playerArray,
                player => player != null ? player.Id : default,
                id => id.IsDefined,
                "players");
            var mutableHeroesById = BuildUniqueIndex(
                heroArray,
                hero => hero != null ? hero.Id : default,
                id => id.IsDefined,
                "heroes");

            foreach (var player in playerArray)
            {
                if (!mutableTeamsById.ContainsKey(player.TeamId))
                {
                    throw new ArgumentException("Every player must reference a team in the match collection.", nameof(players));
                }
            }

            var representedPlayers = new HashSet<PlayerId>();
            foreach (var hero in heroArray)
            {
                if (!mutablePlayersById.ContainsKey(hero.PlayerId))
                {
                    throw new ArgumentException("Every hero must reference a player in the match collection.", nameof(heroes));
                }

                if (!representedPlayers.Add(hero.PlayerId))
                {
                    throw new ArgumentException("Each player must be represented by exactly one hero.", nameof(heroes));
                }
            }

            if (representedPlayers.Count != playerArray.Length)
            {
                throw new ArgumentException("Every player in the match collection must be represented by one hero.", nameof(heroes));
            }

            this.teams = Array.AsReadOnly(teamArray);
            this.players = Array.AsReadOnly(playerArray);
            this.heroes = Array.AsReadOnly(heroArray);
            teamsById = mutableTeamsById;
            playersById = mutablePlayersById;
            heroesById = mutableHeroesById;
        }

        public IReadOnlyList<Team> Teams => teams;

        public IReadOnlyList<Player> Players => players;

        public IReadOnlyList<Hero> Heroes => heroes;

        public Team GetTeam(TeamId id)
        {
            if (!id.IsDefined || !teamsById.TryGetValue(id, out var team))
            {
                throw new KeyNotFoundException($"Unknown team ID '{id}'.");
            }

            return team;
        }

        public Player GetPlayer(PlayerId id)
        {
            if (!id.IsDefined || !playersById.TryGetValue(id, out var player))
            {
                throw new KeyNotFoundException($"Unknown player ID '{id}'.");
            }

            return player;
        }

        public Hero GetHero(HeroId id)
        {
            if (!id.IsDefined || !heroesById.TryGetValue(id, out var hero))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{id}'.");
            }

            return hero;
        }

        public IReadOnlyList<Player> GetPlayersForTeam(TeamId teamId)
        {
            GetTeam(teamId);
            return Array.AsReadOnly(players.Where(player => player.TeamId.Equals(teamId)).ToArray());
        }

        public IReadOnlyList<Hero> GetHeroesForTeam(TeamId teamId)
        {
            GetTeam(teamId);
            var teamPlayerIds = new HashSet<PlayerId>(
                players.Where(player => player.TeamId.Equals(teamId)).Select(player => player.Id));
            return Array.AsReadOnly(heroes.Where(hero => teamPlayerIds.Contains(hero.PlayerId)).ToArray());
        }

        public Player GetPlayerForHero(HeroId heroId)
        {
            return GetPlayer(GetHero(heroId).PlayerId);
        }

        public Team GetTeamForHero(HeroId heroId)
        {
            return GetTeam(GetPlayerForHero(heroId).TeamId);
        }

        private static Dictionary<TId, TItem> BuildUniqueIndex<TItem, TId>(
            IEnumerable<TItem> items,
            Func<TItem, TId> getId,
            Func<TId, bool> isDefined,
            string collectionName)
            where TItem : class
        {
            var index = new Dictionary<TId, TItem>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException($"Match {collectionName} must not contain null entries.", collectionName);
                }

                var id = getId(item);
                if (!isDefined(id))
                {
                    throw new ArgumentException($"Match {collectionName} must have defined IDs.", collectionName);
                }

                if (index.ContainsKey(id))
                {
                    throw new ArgumentException($"Match {collectionName} must have unique IDs.", collectionName);
                }

                index.Add(id, item);
            }

            return index;
        }
    }
}
