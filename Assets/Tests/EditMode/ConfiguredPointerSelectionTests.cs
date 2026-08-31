using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Edit Mode behavior of the configured pointer adapters and the passive board view's tile
    /// highlighting. Milestone 6.1c retired the legacy <c>HeroSelectionController</c> drill, whose
    /// fixtures were the only Edit Mode assertions of hero pointer-ray forwarding and of default
    /// tile-material restoration. The configured equivalents live here. The minion half of the same
    /// pointer contract is covered by <see cref="StandardMinionViewTests"/>; the whole path through a
    /// real saved scene is covered by the Play Mode saved-scene fixture.
    /// </summary>
    public sealed class ConfiguredPointerSelectionTests
    {
        [Test]
        public void ConfiguredHeroSelector_ForwardsProjectedHeroIdentityFromPointerRay()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tile = CreateTile(objects, occupancy.Coordinate);
                var view = new BoardInteractionUnityView(new[] { hero }, new[] { tile }, material);
                var submitted = new List<HeroId>();
                view.BindIntentHandlers(
                    heroId =>
                    {
                        submitted.Add(heroId);
                        return true;
                    },
                    _ => false,
                    _ => false);

                var selector = hero.gameObject.AddComponent<PrimitiveHeroPointerSelector>();
                selector.Configure(view);

                var selected = selector.TrySelectFromPointerRay(DownRayAt(hero.transform.position));

                Assert.That(selected, Is.True);
                Assert.That(submitted, Is.EqualTo(new[] { occupancy.HeroId }),
                    "The selector must forward exactly the projected hero identity, once.");
                Assert.That(hero.GetComponent<CapsuleCollider>(), Is.Not.Null,
                    "The selector owns its collider shape so a pointer ray can reach the hero at all.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void ConfiguredHeroSelector_ForwardsNothingWhenTheRayMissesTheHero()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tile = CreateTile(objects, occupancy.Coordinate);
                var view = new BoardInteractionUnityView(new[] { hero }, new[] { tile }, material);
                var submitted = new List<HeroId>();
                view.BindIntentHandlers(
                    heroId =>
                    {
                        submitted.Add(heroId);
                        return true;
                    },
                    _ => false,
                    _ => false);

                var selector = hero.gameObject.AddComponent<PrimitiveHeroPointerSelector>();
                selector.Configure(view);

                var missed = selector.TrySelectFromPointerRay(
                    DownRayAt(hero.transform.position + new Vector3(50f, 0f, 50f)));

                Assert.That(missed, Is.False);
                Assert.That(submitted, Is.Empty,
                    "A ray that hits no hero collider must not submit any command.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void ConfiguredPointerInput_ForwardsRejectedDestinationIntentWithoutDisturbingProjection()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var origin = new HexCoordinate(0, 0);
                var illegalDestination = new HexCoordinate(2, 0);
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), origin);
                var hero = CreateHero(objects, occupancy);
                var originTile = CreateTile(objects, origin);
                var legalTile = CreateTile(objects, new HexCoordinate(1, 0));
                var illegalTile = CreateTile(objects, illegalDestination);
                var tiles = new[] { originTile, legalTile, illegalTile };
                var view = new BoardInteractionUnityView(new[] { hero }, tiles, material);

                var forwarded = new List<HexCoordinate>();
                view.BindIntentHandlers(
                    _ => false,
                    _ => false,
                    destination =>
                    {
                        forwarded.Add(destination);

                        // The authority rejects an illegal destination; the adapter must report that
                        // faithfully rather than swallowing it or mutating presentation itself.
                        return destination.Equals(new HexCoordinate(1, 0));
                    });

                view.Render(new BoardInteractionViewModel(
                    1,
                    new[] { occupancy },
                    Array.Empty<MinionOccupancy>(),
                    new[] { new HexCoordinate(1, 0) },
                    Array.Empty<HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                var selector = hero.gameObject.AddComponent<PrimitiveHeroPointerSelector>();
                selector.Configure(view);
                var cameraObject = new GameObject("Pointer Camera");
                objects.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>();
                var inputObject = new GameObject("Pointer Input");
                objects.Add(inputObject);
                var pointerInput = inputObject.AddComponent<PrimitiveHeroPointerInput>();
                pointerInput.Configure(
                    camera,
                    new[] { selector },
                    Array.Empty<StandardMinionPointerSelector>(),
                    view,
                    tiles,
                    null);

                var highlightedBefore = HighlightedCoordinates(tiles);
                var handled = pointerInput.TryHandlePointerRay(DownRayAt(illegalTile.transform.position));

                Assert.That(handled, Is.False,
                    "An illegal destination must surface as an unhandled pointer, not a silent success.");
                Assert.That(forwarded, Is.EqualTo(new[] { illegalDestination }),
                    "The adapter forwards the exact tile coordinate under the pointer and nothing else.");
                Assert.That(HighlightedCoordinates(tiles), Is.EqualTo(highlightedBefore),
                    "A rejected destination leaves the authoritative highlight projection untouched.");
                Assert.That(hero.Occupancy.Coordinate, Is.EqualTo(origin));
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void PassiveBoardView_HidesAnAbsentHeroAndReactivatesItAtReturnedOccupancy()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var initial = new HeroOccupancy(new HeroId("blue-hero"), new HexCoordinate(0, 0));
                var returned = new HeroOccupancy(initial.HeroId, new HexCoordinate(2, -1));
                var hero = CreateHero(objects, initial);
                var view = new BoardInteractionUnityView(
                    new[] { hero },
                    Array.Empty<HexTileView>(),
                    material);

                view.Render(BoardModel(1, initial, Array.Empty<HexCoordinate>()));
                Assert.That(hero.gameObject.activeSelf, Is.True);

                view.Render(new BoardInteractionViewModel(
                    2,
                    Array.Empty<HeroOccupancy>(),
                    Array.Empty<MinionOccupancy>(),
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                Assert.That(hero.gameObject.activeSelf, Is.False,
                    "A hero removed from authoritative occupancy must not remain visible on the board.");

                view.Render(BoardModel(3, returned, Array.Empty<HexCoordinate>()));

                Assert.That(hero.gameObject.activeSelf, Is.True,
                    "Respawn, Undo, and restore must be able to reactivate the retained hero view.");
                Assert.That(hero.Occupancy, Is.EqualTo(returned));
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void PassiveBoardView_RestoresTheExactDefaultTileMaterialWhenHighlightingClears()
        {
            var objects = new List<GameObject>();
            var highlightMaterial = new Material(Shader.Find("Standard")) { color = Color.yellow };
            var cornerTemplate = new HexCornerHighlightTemplate(1f);

            try
            {
                var destination = new HexCoordinate(1, 0);
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tile = CreateTile(objects, destination);
                var defaultMaterial = tile.GetComponent<MeshRenderer>().sharedMaterial;
                var view = new BoardInteractionUnityView(
                    new[] { hero },
                    new[] { tile },
                    highlightMaterial,
                    sharedCornerHighlightTemplate: cornerTemplate);

                view.Render(new BoardInteractionViewModel(
                    1,
                    new[] { occupancy },
                    Array.Empty<MinionOccupancy>(),
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<HeroId>(),
                    Array.Empty<MinionId>(),
                    default,
                    effectChoiceDestinations: new[] { destination }));

                Assert.That(tile.IsHighlighted, Is.False);
                Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(defaultMaterial));
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));

                view.Render(BoardModel(2, occupancy, Array.Empty<HexCoordinate>()));

                Assert.That(tile.IsHighlighted, Is.False);
                Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(defaultMaterial),
                    "The unified corner language never replaces the tile's own material.");
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
            }
            finally
            {
                cornerTemplate.Dispose();
                Object.DestroyImmediate(highlightMaterial);
                DestroyAll(objects);
            }
        }

        private static BoardInteractionViewModel BoardModel(
            long version,
            HeroOccupancy occupancy,
            IReadOnlyList<HexCoordinate> movementDestinations)
        {
            return new BoardInteractionViewModel(
                version,
                new[] { occupancy },
                Array.Empty<MinionOccupancy>(),
                movementDestinations,
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default);
        }

        private static HexCoordinate[] HighlightedCoordinates(IEnumerable<HexTileView> tiles)
        {
            var highlighted = new List<HexCoordinate>();
            foreach (var tile in tiles)
            {
                if (tile.IsHighlighted)
                {
                    highlighted.Add(tile.Coordinate);
                }
            }

            return highlighted.ToArray();
        }

        private static PrimitiveHeroView CreateHero(ICollection<GameObject> objects, HeroOccupancy occupancy)
        {
            var gameObject = new GameObject($"Hero {occupancy.HeroId}");
            objects.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(occupancy, HexWorldLayout.DefaultOuterRadius);
            return hero;
        }

        private static HexTileView CreateTile(ICollection<GameObject> objects, HexCoordinate coordinate)
        {
            var gameObject = new GameObject($"Tile {coordinate}");
            objects.Add(gameObject);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Standard")) { color = Color.gray };
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, HexWorldLayout.DefaultOuterRadius);
            return tile;
        }

        private static Ray DownRayAt(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 10f), Vector3.down);
        }

        private static void DestroyAll(IEnumerable<GameObject> objects)
        {
            foreach (var gameObject in objects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }
    }
}
