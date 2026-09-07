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

        /// <summary>
        /// Milestone 8.35z48. THE CASE THAT FAILS ON SHIPPED: a click that touches a hero's pick
        /// volume and lands on a space the match is asking about is answered by the SPACE.
        ///
        /// <para><b>The recorded symptom.</b> Milestone 8.35z38 spent eight Play Mode runs on a board
        /// click that was accepted and moved nothing, at a coordinate where the identical click moved
        /// the hero on a different staging. Reach, the offer and the projected command were ruled out
        /// correctly - all three are model-side, and the click never reached the model. It was
        /// swallowed here: the tray-pointing fallback lived inside the hero arm of a first-match-wins
        /// chain and succeeds for any hero that is not already the viewed one, so any ray grazing a
        /// piece was consumed before the board was consulted, and reported success while nothing
        /// moved.</para>
        ///
        /// <para><b>The ray is built to be exactly that click</b> - through the hero's own capsule
        /// centre and on down to a neighbouring space's centre - because that is the geometry the
        /// scene produces: pieces are picked by a capsule wider than half its height (so, a sphere
        /// standing above the board nearly as wide as a hex) while spaces are picked on the flat y=0
        /// plane, and the camera looks down at an angle.</para>
        /// </summary>
        [Test]
        public void ConfiguredPointerInput_AnswersTheSpaceWhenTheRayGrazesAHeroWhoAnswersNothing()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var destination = new HexCoordinate(1, 0);
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tiles = new[]
                {
                    CreateTile(objects, occupancy.Coordinate),
                    CreateTile(objects, destination)
                };
                var view = new BoardInteractionUnityView(new[] { hero }, tiles, material);

                var forwarded = new List<HexCoordinate>();
                var pointedAt = new List<HeroId>();
                view.BindIntentHandlers(
                    // The authority has no use for this hero right now: no target is being chosen,
                    // no unit is being named. It declines, which is the ordinary case.
                    _ => false,
                    _ => false,
                    coordinate =>
                    {
                        forwarded.Add(coordinate);
                        return true;
                    },
                    heroId =>
                    {
                        pointedAt.Add(heroId);
                        return true;
                    });

                var pointerInput = CreatePointerInput(objects, view, hero, tiles);
                var grazing = RayThroughPieceOnto(hero, tiles[1]);

                var handled = pointerInput.TryHandlePointerRay(grazing);

                Assert.That(handled, Is.True);
                Assert.That(forwarded, Is.EqualTo(new[] { destination }),
                    "The space under the pointer is the answer, and it was asked. Before this " +
                    "milestone the hero's capsule consumed the click and this list was empty.");
                Assert.That(pointedAt, Is.Empty,
                    "and the cosmetic tray-pointing never ran, because something on the ray was a " +
                    "real answer. That is the ordering the binding site has claimed since 8.11a.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        /// <summary>
        /// Milestone 8.35z48, the other half of the same rule and the reason the fix could not simply
        /// delete the fallback: when NOTHING on the ray answers anything, clicking a hero still points
        /// the tray at that hero. Looking around the board is a feature (milestone 7.8a), and it is
        /// unchanged - it has only stopped pre-empting the questions the match is actually asking.
        /// </summary>
        [Test]
        public void ConfiguredPointerInput_StillPointsTheTrayWhenNothingOnTheRayIsAnAnswer()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tiles = new[]
                {
                    CreateTile(objects, occupancy.Coordinate),
                    CreateTile(objects, new HexCoordinate(1, 0))
                };
                var view = new BoardInteractionUnityView(new[] { hero }, tiles, material);

                var pointedAt = new List<HeroId>();
                var refusedDestinations = new List<HexCoordinate>();
                view.BindIntentHandlers(
                    _ => false,
                    _ => false,
                    coordinate =>
                    {
                        refusedDestinations.Add(coordinate);
                        return false;
                    },
                    heroId =>
                    {
                        pointedAt.Add(heroId);
                        return true;
                    });

                var pointerInput = CreatePointerInput(objects, view, hero, tiles);

                var handled = pointerInput.TryHandlePointerRay(RayThroughPieceOnto(hero, tiles[1]));

                Assert.That(handled, Is.True);
                Assert.That(refusedDestinations, Is.Not.Empty,
                    "The board was asked FIRST and declined, which is what makes the tray the last " +
                    "resort rather than the first.");
                Assert.That(pointedAt, Is.EqualTo(new[] { occupancy.HeroId }),
                    "and then the click means the only thing left that it can mean.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        /// <summary>
        /// Milestone 8.35z48. When the HERO is a legal answer - the match is choosing an Attack target
        /// and this piece is one of them - the hero wins, and the space under the pointer is never
        /// asked. The arbitration is by what the click can legally DO, and a hero that answers
        /// something outranks a space in the one order the chain has always used.
        /// </summary>
        [Test]
        public void ConfiguredPointerInput_PrefersTheHeroWhenTheHeroItselfIsALegalAnswer()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tiles = new[]
                {
                    CreateTile(objects, occupancy.Coordinate),
                    CreateTile(objects, new HexCoordinate(1, 0))
                };
                var view = new BoardInteractionUnityView(new[] { hero }, tiles, material);

                var submittedHeroes = new List<HeroId>();
                var forwarded = new List<HexCoordinate>();
                var pointedAt = new List<HeroId>();
                view.BindIntentHandlers(
                    heroId =>
                    {
                        submittedHeroes.Add(heroId);
                        return true;
                    },
                    _ => false,
                    coordinate =>
                    {
                        forwarded.Add(coordinate);
                        return true;
                    },
                    heroId =>
                    {
                        pointedAt.Add(heroId);
                        return true;
                    });

                var pointerInput = CreatePointerInput(objects, view, hero, tiles);

                Assert.That(pointerInput.TryHandlePointerRay(RayThroughPieceOnto(hero, tiles[1])),
                    Is.True);
                Assert.That(submittedHeroes, Is.EqualTo(new[] { occupancy.HeroId }));
                Assert.That(forwarded, Is.Empty,
                    "A hero that answers the standing question ends the click there; the space is " +
                    "not also submitted.");
                Assert.That(pointedAt, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        /// <summary>
        /// Milestone 8.35z48. A click that does nothing REPORTS that it did nothing.
        ///
        /// <para>This is the property the absorbing fallback destroyed and the one an automated driver
        /// depends on: <c>UiPlayer.ClickBoard</c> asserts the returned bool, so a chain that answers
        /// true while nothing visible happens makes every journey that clicks the board unable to
        /// fail. Pointing the tray at the hero already being viewed is exactly that case - it changes
        /// nothing a player can see - and it answers false, so the whole chain does.</para>
        /// </summary>
        [Test]
        public void ConfiguredPointerInput_ReportsFailureWhenNothingItCanDoIsVisible()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));

            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var tiles = new[]
                {
                    CreateTile(objects, occupancy.Coordinate),
                    CreateTile(objects, new HexCoordinate(1, 0))
                };
                var view = new BoardInteractionUnityView(new[] { hero }, tiles, material);

                view.BindIntentHandlers(
                    _ => false,
                    _ => false,
                    _ => false,
                    // What the shipped tray-pointing does for the hero it is ALREADY showing.
                    _ => false);

                var pointerInput = CreatePointerInput(objects, view, hero, tiles);

                Assert.That(pointerInput.TryHandlePointerRay(RayThroughPieceOnto(hero, tiles[1])),
                    Is.False,
                    "Nothing was submitted and nothing on screen changed, so the click must not " +
                    "claim to have been handled.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        /// <summary>
        /// The ray the anomaly is made of: it passes through the hero's own pick volume and lands on
        /// another space's centre, which is what a downward-angled camera produces whenever a piece
        /// stands between the viewer and the space being aimed at.
        /// </summary>
        private static Ray RayThroughPieceOnto(PrimitiveHeroView hero, HexTileView tile)
        {
            var pickCentre = hero.transform.position +
                (Vector3.up * (PrimitiveHeroView.DefaultHeight * 0.5f));
            var direction = (tile.transform.position - pickCentre).normalized;
            return new Ray(pickCentre - (direction * 10f), direction);
        }

        private static PrimitiveHeroPointerInput CreatePointerInput(
            ICollection<GameObject> objects,
            BoardInteractionUnityView view,
            PrimitiveHeroView hero,
            IReadOnlyList<HexTileView> tiles)
        {
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
            return pointerInput;
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
