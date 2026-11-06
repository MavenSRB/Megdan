using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MegdanSelectionRosterTests
    {
        [Test]
        public void DemoHasOneIndependentInstanceOfEachTrioMemberPerTeam()
        {
            var roster = MegdanSelectionRoster.DemoHeroes;
            var lineup = new HeroLineup(roster.Take(3), roster.Skip(3));
            Assert.That(roster.Select(h => h.HeroIdValue).Distinct().Count(), Is.EqualTo(6));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                Assert.That(lineup.ForTeam(side).Select(h => h.ContentId.Value),
                    Is.EqualTo(new[] { "misa", "rowenna", "tali" }));
            Assert.That(HeroLineupComposition.ComposeParticipants(lineup).Heroes.Count, Is.EqualTo(6));
            var configuration = MatchConfiguration.CreateForLineup(MatchFormat.ThreeVersusThreeQuick, lineup);
            Assert.That(configuration.CardInstances.Count, Is.EqualTo(108));
            Assert.That(configuration.CardInstances.Select(c => c.Id).Distinct().Count(), Is.EqualTo(108));
            foreach (var hero in roster)
            {
                Assert.That(configuration.CardInstances.Count(c => c.OwnerId.Equals(hero.HeroId)), Is.EqualTo(18));
                Assert.That(MegdanSelectionRoster.TryResolve(hero.HeroIdValue, out var resolved, allowDemo: true), Is.True);
                Assert.That(resolved, Is.EqualTo(hero));
            }
            var seats = new[] { TeamSide.Red, TeamSide.Blue }.SelectMany(side =>
                Enumerable.Range(1, 3).Select(i => new HeroSelectionSeat(
                    new SeatId(side.ToString().ToLowerInvariant() + "-player-0" + i), side))).ToArray();
            var stage = new HeroSelectionStage(seats, TeamSide.Red, registeredHeroes: roster);
            Assert.That(stage.TryChoose(stage.PickingSeat, roster[3].HeroIdValue, out _), Is.False);
            foreach (var seat in stage.PickOrder)
            {
                var hero = roster.First(h => MegdanSelectionRoster.AllowsDemoTeam(h, seat.TeamSide) &&
                    stage.AvailableHeroes.Contains(h));
                Assert.That(stage.TryChoose(seat.Seat, hero.HeroIdValue, out var refused), Is.True, refused.ToString());
            }
            Assert.That(stage.TryConfirm(true, out var picked, out _), Is.True);
            Assert.That(picked.Red.Select(h => h.ContentId), Is.EqualTo(picked.Blue.Select(h => h.ContentId)));
        }

        [Test]
        public void PackageRosterOffersExactlyTheSixRevampedHeroesAndRejectsOtherRegisteredHeroes()
        {
            Assert.That(MegdanSelectionRoster.SixHeroes.Select(hero => hero.HeroIdValue),
                Is.EquivalentTo(new[] { "hero-brogan", "hero-misa", "hero-rowenna",
                    "hero-tigerclaw", "hero-gydion", "hero-tali" }));
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(32));
            var seat = new SeatId("blue-player-01");
            var stage = new HeroSelectionStage(new[] { new HeroSelectionSeat(seat, TeamSide.Blue) },
                TeamSide.Blue, registeredHeroes: MegdanSelectionRoster.SixHeroes);
            Assert.That(stage.TryChoose(seat, "hero-wasp", out var refusal), Is.False);
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.UnregisteredHero));
            Assert.That(stage.AvailableHeroes.Count, Is.EqualTo(6));
            Assert.That(stage.TryChoose(seat, "hero-misa", out refusal), Is.True);
        }
    }
}
