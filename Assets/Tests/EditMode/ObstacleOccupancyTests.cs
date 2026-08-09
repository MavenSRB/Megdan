using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ObstacleOccupancyTests
    {
        [Test]
        public void Constructor_WithDefinedId_StoresCoordinate()
        {
            var obstacleId = new ObstacleId("blocker");
            var coordinate = new HexCoordinate(1, 0);

            var occupancy = new ObstacleOccupancy(obstacleId, coordinate);

            Assert.That(occupancy.ObstacleId, Is.EqualTo(obstacleId));
            Assert.That(occupancy.Coordinate, Is.EqualTo(coordinate));
        }

        [Test]
        public void Constructor_WithBlankId_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ObstacleId(" "));
        }
    }
}
