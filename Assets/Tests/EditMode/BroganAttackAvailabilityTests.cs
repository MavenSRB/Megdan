using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class BroganAttackAvailabilityTests
    {
        [Test]
        public void ConfiguredChargeAttacks_AreAvailableOnlyWithACompleteMoveAndTargetBinding()
        {
            var blocked = BroganBoard.Create(
                heroPlacements: At("primitive-hero", new HexCoordinate(-7, 1)),
                banishedMinions: EveryMinion());
            Assert.That(blocked.IsPrimaryAvailable(BroganCardContent.MadDash), Is.False,
                "Mad Dash is unavailable when no straight move can end beside an enemy unit.");

            var performable = BroganBoard.Create(clearHisNeighbourhood: false);
            Assert.That(performable.IsPrimaryAvailable(BroganCardContent.MadDash), Is.True,
                "The configured engaged board retains at least one complete charge binding.");
        }

        private static IReadOnlyDictionary<string, HexCoordinate> At(
            string contentId, HexCoordinate coordinate) =>
            new Dictionary<string, HexCoordinate> { [contentId] = coordinate };

        private static IReadOnlyCollection<string> EveryMinion() => new[]
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };
    }
}
