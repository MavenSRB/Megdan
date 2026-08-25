using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    public static class PlaceholderOneVsOneParticipants
    {
        public const string RedTeamIdValue = "red-team";
        public const string BlueTeamIdValue = "blue-team";
        public const string RedPlayerIdValue = "red-test-player";
        public const string BluePlayerIdValue = "blue-test-player";
        public const string RedHeroIdValue = "primitive-hero";
        public const string BlueHeroIdValue = "blue-test-hero";

        /// <summary>
        /// Milestone 8.21. A second Blue seat, and the player who controls it.
        ///
        /// <para>It exists because two of Brogan's faces cannot otherwise be reached at all: War
        /// Drummer and Master Skald print "A friendly hero in range gains N coins", and rulebook p.10
        /// keeps the acting hero out of what an action affects unless the text says so - which this
        /// text does not. In a match with one hero per team the selection has no candidate, so the
        /// Skill stops at its first step and neither card can be proved through the ordinary player
        /// pipeline.</para>
        ///
        /// <para>It is a second <em>player</em> rather than a second hero on one player, because p.5
        /// gives each player one hero and the seat this adds is a seat. That is also the shape
        /// milestone 8.25 needs when it composes the official six.</para>
        ///
        /// <para>`DEV-004` development affordance: only a configuration that asks for it deals this
        /// seat, and the match a player loads is unchanged.</para>
        /// </summary>
        public const string BlueAllyHeroIdValue = "blue-ally-hero";

        public const string BlueAllyPlayerIdValue = "blue-ally-test-player";

        /// <summary>
        /// Milestone 7.2a. Both synthetic heroes declare rulebook p.16's dashboard layout. That is
        /// content this build can state for them - their upgrade trees print four icons - rather than
        /// the verified hero content milestone 8.1 supplies, and it is stated once per hero through
        /// the named layout so that a build in which two dashboards differ has to say which.
        /// </summary>
        public static MatchParticipants Create()
        {
            var redTeamId = new TeamId(RedTeamIdValue);
            var blueTeamId = new TeamId(BlueTeamIdValue);
            var redPlayerId = new PlayerId(RedPlayerIdValue);
            var bluePlayerId = new PlayerId(BluePlayerIdValue);

            return new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(redPlayerId, redTeamId),
                    new Player(bluePlayerId, blueTeamId)
                },
                new[]
                {
                    new Hero(new HeroId(RedHeroIdValue), redPlayerId, HeroDashboard.RulebookLayout),
                    new Hero(new HeroId(BlueHeroIdValue), bluePlayerId, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// Milestone 8.21. The same two teams with a second Blue player and their hero, for the
        /// fixtures whose subject is a card that names a friendly hero.
        ///
        /// <para>Nothing else differs, and nothing here is the official six-player setup: the Red team
        /// still fields one hero, both teams still use the ordinary Tie Breaker, and milestone 8.25
        /// still owns the configuration a player loads. What this adds is the smallest board on which
        /// "a friendly hero in range" can be true of anybody.</para>
        /// </summary>
        public static MatchParticipants CreateWithBlueAlly()
        {
            var redTeamId = new TeamId(RedTeamIdValue);
            var blueTeamId = new TeamId(BlueTeamIdValue);
            var redPlayerId = new PlayerId(RedPlayerIdValue);
            var bluePlayerId = new PlayerId(BluePlayerIdValue);
            var blueAllyPlayerId = new PlayerId(BlueAllyPlayerIdValue);

            return new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(redPlayerId, redTeamId),
                    new Player(bluePlayerId, blueTeamId),
                    new Player(blueAllyPlayerId, blueTeamId)
                },
                new[]
                {
                    new Hero(new HeroId(RedHeroIdValue), redPlayerId, HeroDashboard.RulebookLayout),
                    new Hero(new HeroId(BlueHeroIdValue), bluePlayerId, HeroDashboard.RulebookLayout),
                    new Hero(
                        new HeroId(BlueAllyHeroIdValue),
                        blueAllyPlayerId,
                        HeroDashboard.RulebookLayout)
                });
        }
    }
}
