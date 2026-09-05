using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    /// <summary>
    /// Rulebook p.6 setup step 7's four seats: two equal teams of two, one player per seat, one hero
    /// per player. The "2v2" a player asks for.
    ///
    /// <para><b>Which four heroes is printed, not chosen.</b> Step 7 says, in a game with 4 players,
    /// take the dashboards for "Wasp the Warmaiden" and "Xargatha the Changed" for the Red team and
    /// "Arien the Tidemaster" and "Brogan the Destroyer" for the Blue team. All four are among the
    /// six whose printed sets this build has verified, so a 2v2 match deals nothing this build has
    /// not read off the cards.</para>
    ///
    /// <para><b>This roster is the six-player one's first two a side, and that is the book's own
    /// construction rather than a convenience.</b> Step 7 describes the six-player line-up as the
    /// four-player one plus Tigerclaw and Sabina "for the third players on the Red and Blue teams" -
    /// so the four seats here are literally the six-seat roster with each team's third seat absent.
    /// The four are stated out in full below anyway, because a reader should be able to check them
    /// against the page without reasoning about which entries a truncation would keep; the
    /// relationship itself is asserted by fixture rather than relied on by either type.</para>
    ///
    /// <para><b>The hero-to-seat pairing is stated rather than drawn</b>, for exactly the reason
    /// <see cref="OfficialSixPlayerParticipants"/> gives for the six: p.6 distributes a team's
    /// dashboards randomly among that team's players, and this build's seats hold no name, device or
    /// order of play of their own, so a draw between them changes nothing any rule, projection or
    /// snapshot can observe. The seeded draw stays on the Tie Breaker coin, which decides
    /// something.</para>
    ///
    /// <para>The seat and hero ids are <see cref="OfficialSixPlayerParticipants"/>' own constants
    /// rather than new strings. One seat is one seat whichever format is being played: a player who
    /// held <c>red-player-01</c> in a 3v3 holds the same seat in a 2v2, and nothing downstream - the
    /// roster, a reconnect capability, a log line - has to learn a second vocabulary for the same
    /// chair.</para>
    /// </summary>
    public static class OfficialFourPlayerParticipants
    {
        /// <summary>
        /// The four hero identities in p.6 setup step 7's own order - the Red team's two, then the
        /// Blue team's two - so a list built from this one meets them the way the step prints them.
        /// </summary>
        public static IReadOnlyList<HeroId> HeroIds { get; } = new[]
        {
            new HeroId(OfficialSixPlayerParticipants.WaspHeroIdValue),
            new HeroId(OfficialSixPlayerParticipants.XargathaHeroIdValue),
            new HeroId(OfficialSixPlayerParticipants.ArienHeroIdValue),
            new HeroId(OfficialSixPlayerParticipants.BroganHeroIdValue)
        };

        /// <summary>The seat each hero is dealt to, stated as a pair rather than derived from order.</summary>
        public static IReadOnlyList<(string HeroId, string PlayerIdValue, TeamSide Side)> Seats { get; } =
            new[]
            {
                (OfficialSixPlayerParticipants.WaspHeroIdValue,
                    OfficialSixPlayerParticipants.RedFirstPlayerIdValue, TeamSide.Red),
                (OfficialSixPlayerParticipants.XargathaHeroIdValue,
                    OfficialSixPlayerParticipants.RedSecondPlayerIdValue, TeamSide.Red),
                (OfficialSixPlayerParticipants.ArienHeroIdValue,
                    OfficialSixPlayerParticipants.BlueFirstPlayerIdValue, TeamSide.Blue),
                (OfficialSixPlayerParticipants.BroganHeroIdValue,
                    OfficialSixPlayerParticipants.BlueSecondPlayerIdValue, TeamSide.Blue)
            };

        /// <summary>
        /// The official four-player participant graph: two teams, four players, four heroes, and
        /// every ownership relationship between them.
        ///
        /// <para>Every hero declares rulebook p.16's dashboard layout, as all six have since
        /// milestone 7.2a - the layout is a statement about the roster, and dealing four of the six
        /// does not change what any of the four print.</para>
        /// </summary>
        public static MatchParticipants Create()
        {
            var redTeamId = new TeamId(OfficialSixPlayerParticipants.RedTeamIdValue);
            var blueTeamId = new TeamId(OfficialSixPlayerParticipants.BlueTeamIdValue);

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
