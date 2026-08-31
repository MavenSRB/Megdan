using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HexCornerHighlightTests
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private HexCornerHighlightTemplate template;

        [TearDown]
        public void TearDown()
        {
            template?.Dispose();
            template = null;
            foreach (var value in owned)
            {
                if (value != null) UnityEngine.Object.DestroyImmediate(value);
            }
            owned.Clear();
        }

        [Test]
        public void EveryLegalBoardChoice_LightsSixFixedHexCornerMarksFromProjectedGeometry()
        {
            var movement = Tile(new HexCoordinate(0, 0));
            var attack = Tile(new HexCoordinate(1, 0));
            var unavailable = Tile(new HexCoordinate(2, 0));
            var target = new HeroId("target");
            var view = View(new[] { movement, attack, unavailable });

            view.Render(Model(
                new[] { movement.Coordinate },
                new[] { new HeroOccupancy(target, attack.Coordinate) },
                new[] { target }));

            Assert.That(movement.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(attack.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(movement.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(attack.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(unavailable.CornerHighlight.VisibleMarkCount, Is.Zero);
            Assert.That(movement.IsHighlighted, Is.False,
                "Movement uses the six printed marks, not the retired whole-hex wash.");
            Assert.That(movement.CornerHighlight.LocalBounds.size,
                Is.EqualTo(attack.CornerHighlight.LocalBounds.size));
            Assert.That(template.MarkCount, Is.EqualTo(6));
            Assert.That(template.CoreMesh.normals,
                Has.All.Matches<Vector3>(normal => Vector3.Dot(normal, Vector3.up) > 0.99f),
                "Every mark faces the board camera rather than being back-face culled.");
        }

        [Test]
        public void RespawnDestinations_UseGreenSixCornerShineWithoutTheLegacyFill()
        {
            var destination = Tile(new HexCoordinate(0, 0));
            var view = View(new[] { destination });
            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                heroRespawnDestinations: new[] { destination.Coordinate }));

            Assert.That(destination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Respawn));
            Assert.That(destination.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(destination.IsHighlighted, Is.False);
            Assert.That(HexCornerHighlightTemplate.RespawnColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.MovementColor));
        }

        [Test]
        public void MovementAndAttackHighlights_StrengthenInPlaceWithDistinctSignedOffColours()
        {
            var movement = Tile(new HexCoordinate(0, 0));
            var attack = Tile(new HexCoordinate(1, 0));
            var target = new HeroId("target");
            var view = View(new[] { movement, attack });
            view.Render(Model(
                new[] { movement.Coordinate },
                new[] { new HeroOccupancy(target, attack.Coordinate) },
                new[] { target }));
            var movementBounds = movement.CornerHighlight.LocalBounds;
            var attackBounds = attack.CornerHighlight.LocalBounds;

            view.SetHoveredCoordinate(movement.Coordinate);

            Assert.That(movement.CornerHighlight.IsHovered, Is.True);
            Assert.That(attack.CornerHighlight.IsHovered, Is.False);
            Assert.That(movement.CornerHighlight.LocalBounds, Is.EqualTo(movementBounds));
            Assert.That(attack.CornerHighlight.LocalBounds, Is.EqualTo(attackBounds));
            Assert.That(HexCornerHighlightTemplate.MovementColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.AttackColor));
            Assert.That(Vector4.Distance(
                HexCornerHighlightTemplate.MovementColor,
                HexCornerHighlightTemplate.AttackColor), Is.GreaterThan(0.5f));
        }

        [Test]
        public void FastTravelDestinations_UseTheSameMovementCornerTreatmentWithoutAWholeHexWash()
        {
            var destination = Tile(new HexCoordinate(0, 0));
            var view = View(new[] { destination });
            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                fastTravelDestinations: new[] { destination.Coordinate }));

            Assert.That(destination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(destination.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(destination.IsHighlighted, Is.False);
        }

        [Test]
        public void EffectsAndRemoval_UseNeutralAndBloodRedCornersWithoutLegacyMarks()
        {
            var effectDestination = Tile(new HexCoordinate(0, 0));
            var affectedHeroTile = Tile(new HexCoordinate(1, 0));
            var removedHeroTile = Tile(new HexCoordinate(2, 0));
            var resultingSpace = Tile(new HexCoordinate(3, 0));
            var affectedHero = new HeroId("affected");
            var removedHero = new HeroId("removed");
            var view = View(new[]
            {
                effectDestination, affectedHeroTile, removedHeroTile, resultingSpace
            });

            view.Render(new BoardInteractionViewModel(
                1,
                new[]
                {
                    new HeroOccupancy(affectedHero, affectedHeroTile.Coordinate),
                    new HeroOccupancy(removedHero, removedHeroTile.Coordinate)
                },
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(affectedHero),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(removedHero),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal)
                },
                effectChoiceDestinations: new[] { effectDestination.Coordinate },
                effectChoiceTargetSpace: affectedHeroTile.Coordinate,
                effectChoiceResultingSpace: resultingSpace.Coordinate));

            Assert.That(effectDestination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(affectedHeroTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(removedHeroTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "Minion Battle and other removal choices share the blood-red destructive language.");
            Assert.That(resultingSpace.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.EffectResult));
            Assert.That(new[]
                {
                    effectDestination, affectedHeroTile, removedHeroTile, resultingSpace
                }, Has.All.Matches<HexTileView>(tile =>
                    tile.CornerHighlight.VisibleMarkCount == 6 && !tile.IsHighlighted));
            Assert.That(HexCornerHighlightTemplate.EffectColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.AttackColor));
        }

        [Test]
        public void TeammateActions_KeepTheWarmIvoryCornersWhileOtherEffectsStayNeutral()
        {
            var allyTile = Tile(new HexCoordinate(0, 0));
            var neutralTile = Tile(new HexCoordinate(1, 0));
            var ally = new HeroId("ally");
            var affected = new HeroId("affected");
            var view = View(new[] { allyTile, neutralTile });

            view.Render(new BoardInteractionViewModel(
                1,
                new[]
                {
                    new HeroOccupancy(ally, allyTile.Coordinate),
                    new HeroOccupancy(affected, neutralTile.Coordinate)
                },
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(ally),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Ally),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(affected),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect)
                }));

            Assert.That(allyTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Ally));
            Assert.That(neutralTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(HexCornerHighlightTemplate.AllyColor,
                Is.EqualTo(new Color(0.92f, 0.90f, 0.80f, 1f)));
            Assert.That(HexCornerHighlightTemplate.AllyColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.EffectColor));
        }

        private HexTileView Tile(HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, 1f);
            return tile;
        }

        private BoardInteractionUnityView View(IEnumerable<HexTileView> tiles)
        {
            template = new HexCornerHighlightTemplate(1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            return new BoardInteractionUnityView(
                Array.Empty<GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView>(),
                tiles,
                material,
                sharedCornerHighlightTemplate: template);
        }

        private static BoardInteractionViewModel Model(
            IEnumerable<HexCoordinate> movement,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HeroId> attackTargets) =>
            new BoardInteractionViewModel(
                1,
                heroes,
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                movement,
                attackTargets,
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default);
    }
}
