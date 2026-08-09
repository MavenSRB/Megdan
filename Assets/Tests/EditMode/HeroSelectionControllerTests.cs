using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroSelectionControllerTests
    {
        [Test]
        public void SelectHero_HighlightsReachableEmptyTilesWithinAllowance()
        {
            var objects = new List<GameObject>();

            try
            {
                var hero = CreateHero(objects, new HexCoordinate(0, 0));
                var origin = hero.Coordinate;
                var tileCoordinates = CreateRadiusTwoCoordinates();
                var tiles = tileCoordinates.Select(coordinate => CreateTile(objects, coordinate)).ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                var highlightMaterial = CreateMaterial(Color.yellow);

                controller.Configure(hero, tiles, highlightMaterial);
                controller.SelectHero();

                var highlightedCoordinates = tiles
                    .Where(tile => tile.IsHighlighted)
                    .Select(tile => tile.Coordinate)
                    .ToArray();

                Assert.That(controller.IsHeroSelected, Is.True);
                var expectedDestinations = tiles
                    .Select(tile => tile.Coordinate)
                    .Where(coordinate => !coordinate.Equals(origin))
                    .ToArray();

                Assert.That(controller.HighlightedCoordinates, Is.EquivalentTo(expectedDestinations));
                Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
                Assert.That(tiles.Single(tile => tile.Coordinate.Equals(origin)).IsHighlighted, Is.False);
                Assert.That(tiles.Single(tile => tile.Coordinate.Equals(new HexCoordinate(2, 0))).IsHighlighted, Is.True);
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void ClearSelection_RestoresTileMaterials()
        {
            var objects = new List<GameObject>();

            try
            {
                var hero = CreateHero(objects, new HexCoordinate(0, 0));
                var tile = CreateTile(objects, hero.Coordinate.GetNeighbor(HexDirection.East));
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                var highlightMaterial = CreateMaterial(Color.yellow);
                var originalMaterial = tile.GetComponent<MeshRenderer>().sharedMaterial;

                controller.Configure(hero, new[] { tile, CreateTile(objects, hero.Coordinate) }, highlightMaterial);
                controller.SelectHero();
                controller.ClearSelection();

                Assert.That(controller.IsHeroSelected, Is.False);
                Assert.That(controller.HighlightedCoordinates, Is.Empty);
                Assert.That(tile.IsHighlighted, Is.False);
                Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(originalMaterial));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void PointerSelector_SelectsHeroFromPointerRay()
        {
            var objects = new List<GameObject>();

            try
            {
                var hero = CreateHero(objects, new HexCoordinate(0, 0));
                var tiles = hero.Coordinate.GetNeighbors()
                    .Concat(new[] { hero.Coordinate })
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(hero, tiles, CreateMaterial(Color.yellow));

                var selector = hero.gameObject.AddComponent<PrimitiveHeroPointerSelector>();
                selector.Configure(controller);
                var pointerRay = new Ray(
                    hero.transform.position + (Vector3.up * 1f),
                    Vector3.down);

                var selected = selector.TrySelectFromPointerRay(pointerRay);

                Assert.That(selected, Is.True);
                Assert.That(controller.IsHeroSelected, Is.True);
                Assert.That(controller.HighlightedCoordinates, Has.Count.EqualTo(6));
                Assert.That(selector.SelectionController, Is.SameAs(controller));
                Assert.That(hero.GetComponent<CapsuleCollider>(), Is.Not.Null);
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void TryCommitDestination_ReachableNonAdjacentDestination_CommitsRouteAndClearsSelection()
        {
            var objects = new List<GameObject>();

            try
            {
                var hero = CreateHero(objects, new HexCoordinate(0, 0));
                var destination = new HexCoordinate(2, 0);
                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(hero, tiles, CreateMaterial(Color.yellow));

                controller.SelectHero();
                var moved = controller.TryCommitDestination(destination);

                Assert.That(moved, Is.True);
                Assert.That(controller.IsHeroSelected, Is.False);
                Assert.That(controller.IsMovementSessionActive, Is.False);
                Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(HeroSelectionController.DefaultMovementAllowance - 2));
                Assert.That(controller.MovementRoute, Is.EqualTo(new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    destination
                }));
                Assert.That(controller.HighlightedCoordinates, Is.Empty);
                Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
                Assert.That(controller.CurrentHeroOccupancy.Coordinate, Is.EqualTo(destination));
                Assert.That(hero.Occupancy.Coordinate, Is.EqualTo(destination));
                Assert.That(
                    Vector3.Distance(
                        hero.transform.localPosition,
                        HexWorldLayout.ToWorld(destination) + (Vector3.up * hero.VerticalOffset)),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void StopMovementSession_WithoutSteps_AllowsZeroMovementAndClearsHighlights()
        {
            var objects = new List<GameObject>();

            try
            {
                var origin = new HexCoordinate(0, 0);
                var hero = CreateHero(objects, origin);
                var tiles = origin.GetNeighbors()
                    .Concat(new[] { origin })
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(hero, tiles, CreateMaterial(Color.yellow));

                controller.SelectHero();
                var stopped = controller.StopMovementSession();

                Assert.That(stopped, Is.True);
                Assert.That(controller.IsHeroSelected, Is.False);
                Assert.That(controller.IsMovementSessionActive, Is.False);
                Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(HeroSelectionController.DefaultMovementAllowance));
                Assert.That(controller.MovementRoute, Is.EqualTo(new[] { origin }));
                Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
                Assert.That(hero.Occupancy.Coordinate, Is.EqualTo(origin));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void TryCommitDestination_ExactAllowance_ClearsSelectionAfterFinalStep()
        {
            var objects = new List<GameObject>();

            try
            {
                var origin = new HexCoordinate(0, 0);
                var destination = new HexCoordinate(2, 0);
                var hero = CreateHero(objects, origin);
                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(hero, tiles, CreateMaterial(Color.yellow), 2);

                controller.SelectHero();
                var moved = controller.TryCommitDestination(destination);

                Assert.That(moved, Is.True);
                Assert.That(controller.IsHeroSelected, Is.False);
                Assert.That(controller.IsMovementSessionActive, Is.False);
                Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(0));
                Assert.That(controller.MovementRoute, Is.EqualTo(new[] { origin, new HexCoordinate(1, 0), destination }));
                Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
                Assert.That(hero.Occupancy.Coordinate, Is.EqualTo(destination));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void PointerInput_ClickingUnreachableTileKeepsSelection()
        {
            var objects = new List<GameObject>();

            try
            {
                var origin = new HexCoordinate(0, 0);
                var farCoordinate = new HexCoordinate(2, 0);
                var hero = CreateHero(objects, origin);
                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Selection Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(hero, tiles, CreateMaterial(Color.yellow), 1);
                var selector = hero.gameObject.AddComponent<PrimitiveHeroPointerSelector>();
                selector.Configure(controller);
                var inputObject = new GameObject("Pointer Input");
                objects.Add(inputObject);
                var pointerInput = inputObject.AddComponent<PrimitiveHeroPointerInput>();
                pointerInput.Configure(null, new[] { selector });

                controller.SelectHero();
                var highlightedBefore = controller.HighlightedCoordinates.ToArray();
                var farTile = tiles.Single(tile => tile.Coordinate.Equals(farCoordinate));

                var handled = pointerInput.TryHandlePointerRay(CreateDownRay(farTile.transform.position));

                Assert.That(handled, Is.False);
                Assert.That(controller.LastMoveRejectionReason, Is.EqualTo(AdjacentMoveRejectionReason.None));
                Assert.That(controller.IsHeroSelected, Is.True);
                Assert.That(controller.IsMovementSessionActive, Is.True);
                Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(1));
                Assert.That(controller.HighlightedCoordinates, Is.EquivalentTo(highlightedBefore));
                Assert.That(controller.HighlightedCoordinates, Has.No.Member(farCoordinate));
                Assert.That(hero.Occupancy.Coordinate, Is.EqualTo(origin));
                Assert.That(controller.CurrentHeroOccupancy.Coordinate, Is.EqualTo(origin));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        private static PrimitiveHeroView CreateHero(ICollection<GameObject> objects, HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Hero");
            objects.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(new HeroOccupancy(new HeroId("hero"), coordinate), HexWorldLayout.DefaultOuterRadius);
            return hero;
        }

        private static HexTileView CreateTile(ICollection<GameObject> objects, HexCoordinate coordinate)
        {
            var gameObject = new GameObject($"Tile {coordinate}");
            objects.Add(gameObject);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.gray);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, HexWorldLayout.DefaultOuterRadius);
            return tile;
        }

        private static Ray CreateDownRay(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 2f), Vector3.down);
        }

        private static Material CreateMaterial(Color color)
        {
            return new Material(Shader.Find("Standard"))
            {
                color = color
            };
        }

        private static IEnumerable<HexCoordinate> CreateRadiusTwoCoordinates()
        {
            const int radius = 2;
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = System.Math.Max(-radius, -q - radius);
                var maximumR = System.Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private static void DestroyAll(IEnumerable<GameObject> objects)
        {
            foreach (var gameObject in objects)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
