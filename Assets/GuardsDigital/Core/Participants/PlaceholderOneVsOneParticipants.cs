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
    }
}
