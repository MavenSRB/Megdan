using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// W-021. A side fielding more than three seats - the separate PvE executable's five heroes and
    /// eleven enemies - gets colours from its own side's further palette instead of reading past the
    /// six-entry table, while each side's first three seats keep exactly the colours they always had.
    /// </summary>
    public sealed class PlayerAccentSideTests
    {
        [Test]
        public void TheFirstThreeSeatsOfEachSideKeepTheirColours()
        {
            var participants = Compose(11, 5);
            var expectedRed = new Color32[] { new Color32(255, 59, 48, 255), new Color32(255, 149, 0, 255), new Color32(255, 214, 10, 255) };
            var expectedBlue = new Color32[] { new Color32(47, 128, 255, 255), new Color32(32, 215, 255, 255), new Color32(32, 201, 151, 255) };

            for (var index = 0; index < 3; index++)
            {
                Assert.That((Color32)PlayerAccent.For(participants, Red(index)), Is.EqualTo(expectedRed[index]));
                Assert.That((Color32)PlayerAccent.For(participants, Blue(index)), Is.EqualTo(expectedBlue[index]));
            }
        }

        [Test]
        public void FurtherSeatsResolveWithinTheirOwnSidePalette()
        {
            var participants = Compose(11, 5);
            var blueFirstThree = Enumerable.Range(0, 3).Select(index => (Color32)PlayerAccent.For(participants, Blue(index))).ToArray();
            var redFirstThree = Enumerable.Range(0, 3).Select(index => (Color32)PlayerAccent.For(participants, Red(index))).ToArray();

            for (var index = 3; index < 11; index++)
            {
                var colour = (Color32)PlayerAccent.For(participants, Red(index));
                Assert.That(blueFirstThree, Has.No.Member(colour), "Red seat " + index);
            }

            for (var index = 3; index < 5; index++)
            {
                var colour = (Color32)PlayerAccent.For(participants, Blue(index));
                Assert.That(redFirstThree, Has.No.Member(colour), "Blue seat " + index);
                Assert.That(blueFirstThree, Has.No.Member(colour), "Blue seat " + index);
            }
        }

        private static HeroId Red(int index) => new HeroId("red-hero-" + (index + 1).ToString("00"));
        private static HeroId Blue(int index) => new HeroId("blue-hero-" + (index + 1).ToString("00"));

        private static MatchParticipants Compose(int red, int blue)
        {
            var redTeam = new TeamId("red-team");
            var blueTeam = new TeamId("blue-team");
            var players = new List<Player>();
            var heroes = new List<Hero>();
            for (var index = 0; index < red; index++)
            {
                var player = new PlayerId("red-player-" + (index + 1).ToString("00"));
                players.Add(new Player(player, redTeam));
                heroes.Add(new Hero(Red(index), player, HeroDashboard.RulebookLayout));
            }

            for (var index = 0; index < blue; index++)
            {
                var player = new PlayerId("blue-player-" + (index + 1).ToString("00"));
                players.Add(new Player(player, blueTeam));
                heroes.Add(new Hero(Blue(index), player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(redTeam, TeamSide.Red), new Team(blueTeam, TeamSide.Blue) }, players, heroes);
        }
    }
}
