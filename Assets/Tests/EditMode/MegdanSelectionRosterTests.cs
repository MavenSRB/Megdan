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
