using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TokenPlacementSeparationQueryTests
    {
        [Test]
        public void PrintedMinimumDistance_ExcludesNearerStandingCopiesOnly()
        {
            var definition = SyntheticComponentDefinitions.HazardDefinition;
            var owner = new HeroId("owner");
            var token = new PlacedToken(new TokenInstanceId("standing"), definition,
                owner, owner, new CardInstanceId("source"), new HexCoordinate(0, 0));
            var placed = new[] { token };

            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(1, 0), definition.Reference, placed, 3), Is.False);
            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(2, 0), definition.Reference, placed, 3), Is.False);
            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(3, 0), definition.Reference, placed, 3), Is.True);
            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(2, 0), definition.Reference, placed, 2), Is.True);
            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(1, 0), definition.Reference, placed, 0), Is.True);
            Assert.That(TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(1, 0),
                new TokenDefinitionReference(new TokenDefinitionId("other"), 1), placed, 3), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => TokenPlacementSeparationQuery.Admits(
                new HexCoordinate(1, 0), definition.Reference, placed, -1));
        }
    }
}
