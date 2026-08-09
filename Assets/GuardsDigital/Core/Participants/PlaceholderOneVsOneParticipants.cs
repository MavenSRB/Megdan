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
                    new Hero(new HeroId(RedHeroIdValue), redPlayerId),
                    new Hero(new HeroId(BlueHeroIdValue), bluePlayerId)
                });
        }
    }
}
