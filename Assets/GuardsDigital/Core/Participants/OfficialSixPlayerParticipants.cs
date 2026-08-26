using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    /// <summary>
    /// Milestone 8.25. Rulebook p.5's six seats: two equal teams of three, one player per seat, one
    /// hero per player.
    ///
    /// <para>This is what retires `DEV-001`. Every configuration before it fielded one hero per team
    /// because that was all the build could deal - one verified printed set at 8.4, then two at 8.20 -
    /// and each staged arm recorded that milestone 8.25 owned the official configuration. It does.
    /// The six identities here are the ones p.6's setup step 7 names for a six-player game, and they
    /// are all different, which is what retires `DEV-002` as well.</para>
    ///
    /// <para><b>Which hero sits at which seat is fixed rather than drawn.</b> p.6 distributes the three
    /// dashboards of a team randomly among that team's players, and this build's seats are
    /// indistinguishable from one another - they hold no name, no device and no order of play of their
    /// own - so a draw between them would change nothing any rule, projection or snapshot can observe.
    /// The pairing is therefore stated once, here, and a match is reproducible from its recorded setup
    /// without a second seed. Milestone 8.28b's seeded draw stays where it belongs: on p.6's Tie
    /// Breaker coin, which decides something.</para>
    ///
    /// <para>A hero ID names its hero and a player ID names its seat, because those are two different
    /// facts: the hero identity is stable content the projection, the event log and every snapshot
    /// address, and the seat is who is holding it. The two are related through the participant graph
    /// rather than by sharing a string.</para>
    /// </summary>
    public static class OfficialSixPlayerParticipants
    {
        public const string RedTeamIdValue = "red-team";
        public const string BlueTeamIdValue = "blue-team";

        public const string WaspHeroIdValue = "hero-wasp";
        public const string XargathaHeroIdValue = "hero-xargatha";
        public const string TigerclawHeroIdValue = "hero-tigerclaw";
        public const string ArienHeroIdValue = "hero-arien";
        public const string BroganHeroIdValue = "hero-brogan";
        public const string SabinaHeroIdValue = "hero-sabina";

        public const string RedFirstPlayerIdValue = "red-player-01";
        public const string RedSecondPlayerIdValue = "red-player-02";
        public const string RedThirdPlayerIdValue = "red-player-03";
        public const string BlueFirstPlayerIdValue = "blue-player-01";
        public const string BlueSecondPlayerIdValue = "blue-player-02";
        public const string BlueThirdPlayerIdValue = "blue-player-03";

        /// <summary>
        /// The six hero identities in rulebook p.6's own order - the Red team's three, then the Blue
        /// team's three - so a reader of a list built from this one meets them the way setup step 7
        /// prints them.
        /// </summary>
        public static IReadOnlyList<HeroId> HeroIds { get; } = new[]
        {
            new HeroId(WaspHeroIdValue),
            new HeroId(XargathaHeroIdValue),
            new HeroId(TigerclawHeroIdValue),
            new HeroId(ArienHeroIdValue),
            new HeroId(BroganHeroIdValue),
            new HeroId(SabinaHeroIdValue)
        };

        /// <summary>
        /// The seat each hero is dealt to. Stated as a pair rather than derived from order, so a
        /// reader does not have to count to know who controls whom.
        /// </summary>
        public static IReadOnlyList<(string HeroId, string PlayerIdValue, TeamSide Side)> Seats { get; } =
            new[]
            {
                (WaspHeroIdValue, RedFirstPlayerIdValue, TeamSide.Red),
                (XargathaHeroIdValue, RedSecondPlayerIdValue, TeamSide.Red),
                (TigerclawHeroIdValue, RedThirdPlayerIdValue, TeamSide.Red),
                (ArienHeroIdValue, BlueFirstPlayerIdValue, TeamSide.Blue),
                (BroganHeroIdValue, BlueSecondPlayerIdValue, TeamSide.Blue),
                (SabinaHeroIdValue, BlueThirdPlayerIdValue, TeamSide.Blue)
            };

        /// <summary>
        /// The official six-player participant graph: two teams, six players, six heroes, and every
        /// ownership relationship between them.
        ///
        /// <para>Every hero declares rulebook p.16's dashboard layout, as both staged heroes have since
        /// milestone 7.2a. All six printed upgrade trees carry four item icons, so the layout is one
        /// statement about the roster rather than six copies of a claim.</para>
        /// </summary>
        public static MatchParticipants Create()
        {
            var redTeamId = new TeamId(RedTeamIdValue);
            var blueTeamId = new TeamId(BlueTeamIdValue);

            var teams = new[]
            {
                new Team(redTeamId, TeamSide.Red),
                new Team(blueTeamId, TeamSide.Blue)
            };

            var players = Seats
                .Select(seat => new Player(
                    new PlayerId(seat.PlayerIdValue),
                    seat.Side == TeamSide.Red ? redTeamId : blueTeamId))
                .ToArray();

            var heroes = Seats
                .Select(seat => new Hero(
                    new HeroId(seat.HeroId),
                    new PlayerId(seat.PlayerIdValue),
                    HeroDashboard.RulebookLayout))
                .ToArray();

            return new MatchParticipants(teams, players, heroes);
        }
    }
}
