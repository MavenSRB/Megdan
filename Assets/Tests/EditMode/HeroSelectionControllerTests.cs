using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
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
        public void SelectHero_HighlightsOnlyAdjacentEmptyTiles()
        {
            var objects = new List<GameObject>();

            try
            {
                var hero = CreateHero(objects, new HexCoordinate(0, 0));
                var origin = hero.Coordinate;
                var tileCoordinates = origin.GetNeighbors().Concat(new[]
                {
                    origin,
                    new HexCoordinate(2, 0)
                });
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
                Assert.That(controller.HighlightedCoordinates, Is.EquivalentTo(origin.GetNeighbors()));
                Assert.That(highlightedCoordinates, Is.EquivalentTo(origin.GetNeighbors()));
                Assert.That(tiles.Single(tile => tile.Coordinate.Equals(origin)).IsHighlighted, Is.False);
                Assert.That(tiles.Single(tile => tile.Coordinate.Equals(new HexCoordinate(2, 0))).IsHighlighted, Is.False);
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

        private static Material CreateMaterial(Color color)
        {
            return new Material(Shader.Find("Standard"))
            {
                color = color
            };
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
