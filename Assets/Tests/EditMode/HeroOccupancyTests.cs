using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroOccupancyTests
    {
        [Test]
        public void Constructor_StoresHeroIdAndOccupiedCoordinate()
        {
            var heroId = new HeroId("primitive-hero");
            var coordinate = new HexCoordinate(1, -1);

            var occupancy = new HeroOccupancy(heroId, coordinate);

            Assert.That(occupancy.HeroId, Is.EqualTo(heroId));
            Assert.That(occupancy.Coordinate, Is.EqualTo(coordinate));
        }

        [Test]
        public void HeroId_RejectsBlankValue()
        {
            Assert.That(
                () => new HeroId(" "),
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void Constructor_RejectsUndefinedHeroId()
        {
            Assert.That(
                () => new HeroOccupancy(default, new HexCoordinate(0, 0)),
                Throws.TypeOf<System.ArgumentException>());
        }
    }
}
