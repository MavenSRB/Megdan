using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11a. Where the official board face sits relative to the reviewed map, and what the
    /// board presentation does with it - and without it.
    ///
    /// <para>The face itself is private and ignored, so it is absent from a clean checkout. Every case
    /// here that does not need the picture runs either way; the two that do say so and stop, rather
    /// than passing vacuously or failing on a machine that is behaving correctly.</para>
    /// </summary>
    public sealed class OfficialBoardArtworkTests
    {
        /// <summary>
        /// Registration evidence, transcribed from the private source the way the verified card tables
        /// are: each row is one reviewed space and the artwork position measured for it, in the
        /// manifest's own board units. Ten spaces, spread across all seven zones and all four quadrants,
        /// so a transform that drifted in scale, rotation, offset, or handedness moves at least one of
        /// them past the tolerance.
        /// </summary>
        private static readonly Landmark[] Landmarks =
        {
            new Landmark("q-2_r-5", -2, -5, -10.26f, 14.46f),     // red-throne
            new Landmark("q-4_r-4", -4, -4, -13.23f, 11.49f),     // red-throne
            new Landmark("q-8_r0", -8, 0, -15.75f, 2.13f),        // red-approach
            new Landmark("q1_r1", 1, 1, 7.08f, 6.00f),            // starting-battle-zone
            new Landmark("q-7_r7", -7, 7, -1.35f, -9.21f),        // starting-battle-zone
            new Landmark("q3_r6", 3, 6, 20.40f, -1.26f),          // blue-approach
            new Landmark("q-11_r10", -11, 10, -5.61f, -16.92f),   // bottom-jungle
            new Landmark("q-13_r4", -13, 4, -20.64f, -7.89f)      // terrain, the far western edge
        };

        /// <summary>
        /// The registration is a similarity and nothing looser. A shear would let the artwork stretch
        /// along one axis and still satisfy a handful of landmarks; a mirroring would register the
        /// lattice perfectly while presenting the board back to front. Both are excluded here rather
        /// than hoped for, because neither is visible in a landmark table.
        /// </summary>
        [Test]
        public void TheRegistration_IsASimilarityWithNoShearAndNoMirroring()
        {
            var origin = OfficialBoardArtwork.BoardToWorld(Vector2.zero);
            var right = OfficialBoardArtwork.BoardToWorld(Vector2.right) - origin;
            var up = OfficialBoardArtwork.BoardToWorld(Vector2.up) - origin;

            Assert.That(right.y, Is.EqualTo(0f).Within(1e-6f), "The face lies flat on the board plane.");
            Assert.That(up.y, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(right.magnitude, Is.EqualTo(OfficialBoardArtwork.BoardToWorldScale).Within(1e-5f),
                "One board unit is one scale factor of world, whichever way it points - that is the no-shear claim.");
            Assert.That(up.magnitude, Is.EqualTo(OfficialBoardArtwork.BoardToWorldScale).Within(1e-5f));
            Assert.That(Vector3.Dot(right.normalized, up.normalized), Is.EqualTo(0f).Within(1e-5f),
                "and the two axes stay perpendicular.");

            // Handedness. The board's own (x, y) turns the same way in world (x, z) as it does on the
            // face, so the artwork is presented as painted rather than mirrored.
            Assert.That(Vector3.Cross(right, up).y, Is.LessThan(0f),
                "A positive cross would mean the registration flipped the face over.");

            Assert.That(OfficialBoardArtwork.BoardRightInWorld.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(OfficialBoardArtwork.BoardUpInWorld.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(
                Vector3.Dot(OfficialBoardArtwork.BoardRightInWorld, right.normalized), Is.EqualTo(1f).Within(1e-5f),
                "The camera basis is the same pair of axes the face is drawn on, not a second statement of them.");
            Assert.That(
                Vector3.Dot(OfficialBoardArtwork.BoardUpInWorld, up.normalized), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void BoardAndWorldPositions_RoundTripAtEveryRadius()
        {
            var points = new[]
            {
                Vector2.zero, new Vector2(23.82f, 23.82f), new Vector2(-23.82f, 23.82f),
                new Vector2(11.5f, -4.25f), new Vector2(-0.37f, 19.9f)
            };

            foreach (var radius in new[] { 0.5f, HexWorldLayout.DefaultOuterRadius, 2.75f })
            {
                foreach (var point in points)
                {
                    var round = OfficialBoardArtwork.WorldToBoard(
                        OfficialBoardArtwork.BoardToWorld(point, radius), radius);
                    Assert.That(round.x, Is.EqualTo(point.x).Within(1e-3f), $"radius {radius}, {point}");
                    Assert.That(round.y, Is.EqualTo(point.y).Within(1e-3f), $"radius {radius}, {point}");
                }
            }

            Assert.That(
                () => OfficialBoardArtwork.BoardToWorld(Vector2.zero, 0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => OfficialBoardArtwork.WorldToBoard(Vector3.zero, float.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// The registration claim itself: each reviewed space, put through the map's own layout and then
        /// back into the artwork's frame, lands on the position measured for it on the face.
        /// </summary>
        [Test]
        public void EveryLandmarkSpace_RegistersToItsMeasuredArtworkPositionWithinTheDeclaredTolerance()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var toleranceInBoardUnits =
                OfficialBoardArtwork.RegistrationTolerance / OfficialBoardArtwork.BoardToWorldScale;
            var worst = 0f;
            var worstId = string.Empty;

            foreach (var landmark in Landmarks)
            {
                var space = definition.Spaces.SingleOrDefault(value => value.Id.Value == landmark.SpaceId);
                Assert.That(space, Is.Not.Null, $"'{landmark.SpaceId}' is a space of the reviewed map.");
                Assert.That(space.Coordinate, Is.EqualTo(new HexCoordinate(landmark.Q, landmark.R)),
                    $"and '{landmark.SpaceId}' still carries the coordinate this row was measured against.");

                var measured = OfficialBoardArtwork.WorldToBoard(HexWorldLayout.ToWorld(space.Coordinate));
                var deviation = Vector2.Distance(measured, new Vector2(landmark.BoardX, landmark.BoardY));
                if (deviation > worst)
                {
                    worst = deviation;
                    worstId = landmark.SpaceId;
                }
            }

            Assert.That(worst, Is.LessThanOrEqualTo(toleranceInBoardUnits),
                $"The worst registered space is '{worstId}', {worst:F4} board units from where it was measured.");
            Assert.That(worst * OfficialBoardArtwork.BoardToWorldScale,
                Is.LessThanOrEqualTo(OfficialBoardArtwork.MeasuredWorstRegistrationError + 1e-3f),
                "and it is no worse than the deviation this milestone recorded, so a regression shows as a number.");
        }

        /// <summary>
        /// Registration is not enough on its own: the face also has to be big enough. Every reviewed
        /// space must fall inside the square with room around it, or a space at the edge would sit on
        /// painted water or off the picture entirely.
        /// </summary>
        [Test]
        public void TheArtworkSquare_ContainsEveryReviewedSpaceWithMargin()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var half = OfficialBoardArtwork.ArtworkSideInBoardUnits * 0.5f;
            // Half a space, measured to the space's centre. A whole one is more than the face has at
            // its tightest corner - `q-8_r-4` sits 1.22 board units from the edge - and demanding it
            // would be demanding the artwork be bigger than the board it depicts.
            var marginInBoardUnits = 0.5f / OfficialBoardArtwork.BoardToWorldScale;

            Assert.That(definition.Spaces, Has.Count.EqualTo(292));
            foreach (var space in definition.Spaces)
            {
                var board = OfficialBoardArtwork.WorldToBoard(HexWorldLayout.ToWorld(space.Coordinate));
                Assert.That(Mathf.Abs(board.x), Is.LessThanOrEqualTo(half - marginInBoardUnits),
                    $"'{space.Id.Value}' sits inside the face with a space to spare.");
                Assert.That(Mathf.Abs(board.y), Is.LessThanOrEqualTo(half - marginInBoardUnits),
                    $"'{space.Id.Value}' sits inside the face with a space to spare.");
            }
        }

        /// <summary>
        /// The registration is for one map at one revision. Any other definition - every synthetic grid
        /// a test builds, and any future map - must keep the generated presentation, because drawing
        /// this face under it would be drawing a picture of somewhere else.
        /// </summary>
        [Test]
        public void AMapThatIsNotTheReviewedOfficialOne_KeepsItsGeneratedFillsAndEdges()
        {
            var root = new GameObject("Official Board Underlay Rejection");
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                view.Rebuild(CreateForeignMap());

                Assert.That(view.IsUnderlayActive, Is.False);

                var tile = view.CurrentTiles.Single();
                var renderer = tile.GetComponent<MeshRenderer>();
                Assert.That(renderer.sharedMaterial, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.color.a, Is.EqualTo(1f).Within(1e-3f),
                    "Its fill is the opaque generated one, not the suppressed fill.");
                Assert.That(tile.GetComponentInChildren<LineRenderer>(true).enabled, Is.True,
                    "and its debug edge is still drawn.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// W-160 removes the face and keeps the same tile views and highlight path over the generated
        /// grass board.
        /// </summary>
        [Test]
        public void OfficialMap_UsesGeneratedTilesAndKeepsTheirHighlightPath()
        {
            var root = new GameObject("Generated Grass Board");
            Material highlight = null;
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                view.Rebuild(OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap));

                Assert.That(view.IsUnderlayActive, Is.False);
                Assert.That(view.CurrentTiles, Has.Count.EqualTo(292),
                    "Every map space remains one generated, interactive tile.");

                var tile = view.CurrentTiles.First();
                var renderer = tile.GetComponent<MeshRenderer>();
                var restingMaterial = renderer.sharedMaterial;
                Assert.That(renderer.enabled, Is.True, "The generated hex itself is visible.");
                Assert.That(restingMaterial.color.a, Is.EqualTo(1f).Within(1e-3f));
                Assert.That(tile.GetComponentInChildren<LineRenderer>(true).enabled, Is.False,
                    "Ordinary tiles have no resting outline.");

                highlight = new Material(Shader.Find("Standard")) { color = Color.yellow };
                tile.SetHighlighted(true, highlight);
                Assert.That(renderer.sharedMaterial, Is.SameAs(highlight),
                    "The highlight path is untouched by any of this.");
                Assert.That(renderer.enabled, Is.True,
                    "and a highlight turns the renderer back on, which is what keeps that path whole.");
                Assert.That(tile.IsHighlighted, Is.True);

                tile.SetHighlighted(false, highlight);
                Assert.That(renderer.sharedMaterial, Is.SameAs(restingMaterial),
                    "Clearing the highlight returns to the exact grass/fallback material.");
                Assert.That(renderer.enabled, Is.True);
            }
            finally
            {
                if (highlight != null) UnityEngine.Object.DestroyImmediate(highlight);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GrassAndBiomesAreStable_AndOnlyTheThreePossibleBattleZonesKeepThinOutlines()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var selected = definition.Spaces
                .Select(space => MegdanGrassTileCatalog.VariantIndex(definition.Reference, space.Coordinate))
                .ToArray();
            Assert.That(selected.Distinct(), Is.EquivalentTo(new[] { 0, 1, 2, 3 }),
                "The full board uses every supplied grass variation.");
            Assert.That(definition.Spaces.Select((space, index) =>
                    MegdanGrassTileCatalog.VariantIndex(definition.Reference, space.Coordinate) == selected[index])
                .All(value => value), Is.True,
                "Selection is a stable coordinate function, never runtime randomness.");

            var root = new GameObject("Stable Grass And Zone Borders");
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                view.Rebuild(definition);

                var possibleBattleZones = definition.Zones.Where(zone =>
                        string.Equals(zone.Kind, "lane", StringComparison.OrdinalIgnoreCase))
                    .Select(zone => zone.Id).ToHashSet();
                Assert.That(possibleBattleZones, Has.Count.EqualTo(3));
                foreach (var tile in view.CurrentTiles)
                {
                    var edge = tile.GetComponentInChildren<LineRenderer>(true);
                    var expected = tile.IsPlayable && possibleBattleZones.Contains(tile.ZoneId.Value);
                    Assert.That(edge.enabled, Is.EqualTo(expected), tile.SpaceId.Value);
                    if (expected)
                    {
                        Assert.That(edge.startWidth, Is.EqualTo(0.030f).Within(0.0001f));
                    }
                }

                var tileMaterials = view.CurrentTiles.Select(tile =>
                    tile.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
                if (MegdanGrassTileCatalog.TryLoadTextures(out var textures) &&
                    MegdanGrassTileCatalog.TryLoadBiomeTextures(out var water, out var lava, out var desert))
                {
                    Assert.That(textures, Has.Length.EqualTo(MegdanGrassTileCatalog.VariantCount));
                    Assert.That(tileMaterials.Select(material => material.mainTexture).Distinct().Count(),
                        Is.EqualTo(MegdanGrassTileCatalog.VariantCount + 3));
                    Assert.That(tileMaterials.All(material => material.mainTexture != null), Is.True);

                    var byCoordinate = view.CurrentTiles.ToDictionary(
                        tile => tile.Coordinate,
                        tile => tile.GetComponent<MeshRenderer>().sharedMaterial.mainTexture);
                    var allCoordinates = definition.Spaces.Select(space => space.Coordinate).ToHashSet();
                    var zones = definition.Zones.ToDictionary(zone => zone.Id);
                    foreach (var space in definition.Spaces)
                    {
                        if (space.Kind == BoardMapSpaceKind.Terrain)
                        {
                            var expected = space.Coordinate.GetNeighbors().Any(neighbor =>
                                !allCoordinates.Contains(neighbor)) ? water : lava;
                            Assert.That(byCoordinate[space.Coordinate], Is.SameAs(expected));
                        }
                        else if (string.Equals(zones[space.ZoneId.Value].Kind, "jungle",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            Assert.That(byCoordinate[space.Coordinate], Is.SameAs(desert));
                        }
                        else
                        {
                            Assert.That(textures, Has.Member(byCoordinate[space.Coordinate]));
                        }
                    }
                }
                else if (!MegdanGrassTileCatalog.TryLoadTextures(out _) &&
                    !MegdanGrassTileCatalog.TryLoadBiomeTextures(out _, out _, out _))
                {
                    Assert.That(tileMaterials.All(material => material.mainTexture == null), Is.True,
                        "A clean checkout retains the opaque generated fallback when private grass is absent.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// The no-HUD fallback framing claim for the restored synthetic lattice.
        /// </summary>
        [Test]
        public void TheFallbackFramingCamera_CentresEveryReviewedSpaceInsideTheViewport()
        {
            var root = new GameObject("Official Board Framing");
            var cameraObject = new GameObject("Framing Camera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.aspect = 2048f / 1152f;
                var view = root.AddComponent<PlaceholderTestGridView>();
                view.ConfigureSceneBindings(camera, Array.Empty<GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView>());
                view.Rebuild(OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap));

                Assert.That(view.IsUnderlayActive, Is.False);
                Assert.That(
                    Vector3.Dot(camera.transform.up, Vector3.forward),
                    Is.EqualTo(1f).Within(1e-3f),
                    "Screen up returns to the synthetic lattice's world-forward axis.");
                Assert.That(camera.transform.forward.y, Is.LessThan(-0.999f), "and it still looks straight down.");

                var viewport = view.CurrentTiles
                    .Select(tile => camera.WorldToViewportPoint(tile.transform.position))
                    .ToArray();
                Assert.That(viewport.Min(point => point.x), Is.GreaterThanOrEqualTo(0f));
                Assert.That(viewport.Max(point => point.x), Is.LessThanOrEqualTo(1f));
                Assert.That(viewport.Min(point => point.y), Is.GreaterThanOrEqualTo(0f));
                Assert.That(viewport.Max(point => point.y), Is.LessThanOrEqualTo(1f),
                    "Every space is inside the viewport, which is the framing claim itself.");
                var leftMargin = viewport.Min(point => point.x);
                var rightMargin = 1f - viewport.Max(point => point.x);
                Assert.That(leftMargin, Is.EqualTo(rightMargin).Within(1e-5f),
                    "Without a configured HUD-safe layout, the fallback centers the complete map " +
                    "in the whole viewport rather than reserving a retired rectangular tray band.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }


        /// <summary>
        /// W-160 restores the generated spawn rings because the retired face no longer prints them.
        /// </summary>
        [Test]
        public void WithoutTheFace_SpawnMarkersAreVisibleAndKeepTheirReviewedConfiguration()
        {
            var root = new GameObject("Official Board Spawn Markers");
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                view.Rebuild(definition);

                var markers = root.GetComponentsInChildren<BoardMapSpawnMarkerView>(true);
                Assert.That(markers, Has.Length.EqualTo(definition.SpawnPoints.Count),
                    "Every spawn point keeps its generated view.");
                Assert.That(
                    markers.All(marker => marker.GetComponent<LineRenderer>().enabled), Is.True,
                    "and each is drawn because no board picture supplies it now.");
                Assert.That(
                    markers.All(marker => marker.GetComponent<LineRenderer>().positionCount > 0), Is.True,
                    "Their reviewed shapes remain configured.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Milestone 8.11a, at the user's request. Every board highlight is drawn as a see-through fill
        /// with an opaque rim, and the rim reuses the perimeter renderer each tile already carries.
        ///
        /// <para>The fill alone was the first attempt and it was not enough: most of this board is
        /// painted sand, and a blended warm fill over sand reads as slightly different sand. The rim is
        /// what makes the shape unmistakable while the artwork under it stays legible.</para>
        /// </summary>
        [Test]
        public void BothBoardHighlights_AreSeeThroughWithAnOpaqueRimInTheirOwnColour()
        {
            using (var destination = new GuardsDigital.Presentation.Match.DestinationHighlightMaterialOwner())
            using (var resulting = GuardsDigital.Presentation.Match.DestinationHighlightMaterialOwner.ForResultingSpace())
            {
                foreach (var owner in new[] { destination, resulting })
                {
                    Assert.That(owner.Material.color.a,
                        Is.EqualTo(BoardOverlayMaterial.OverlayAlpha).Within(1f / 255f),
                        "The fill is blended, so the space under it still reads.");
                    Assert.That(owner.Material.renderQueue,
                        Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent),
                        "and it is actually in the transparent queue - a low alpha on an opaque " +
                        "Standard material would draw solid.");
                    Assert.That(owner.RimMaterial.color.a, Is.EqualTo(1f).Within(1f / 255f),
                        "The rim is opaque, because it is what carries the shape.");
                    Assert.That(owner.RimMaterial.IsKeywordEnabled("_EMISSION"), Is.True,
                        "and it is emissive, so the scene's lighting cannot shade a line that has to be one colour.");
                    Assert.That(owner.RimMaterial.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"), Is.True,
                        "and URP environment reflections are disabled with URP's own keyword.");
                    Assert.That(owner.RimMaterial, Is.SameAs(owner.RimMaterial), "and it is owned once.");
                }

                // Milestone 6.5c's rule now lives in the rim, which is where a highlight's identity
                // moved. Both fills are the same warm light because both mean "this space"; what says a
                // resulting space is a prediction rather than a thing to click is the colour around it.
                Assert.That(
                    Vector3.Distance(
                        RimColourOf(destination), RimColourOf(resulting)),
                    Is.GreaterThan(0.4f),
                    "A prediction never wears the colour that means 'you may click here'.");
                Assert.That(destination.OwnedNativeResourceCount, Is.EqualTo(2),
                    "Fill and rim are owned together, so neither can be leaked without the other.");
            }
        }

        /// <summary>
        /// The rim is drawn on the perimeter renderer the tile already has, and clearing the highlight
        /// puts that renderer back exactly as the map view left it - the zone-coloured resting border.
        /// </summary>
        [Test]
        public void AHighlightRim_UsesTheTilesOwnPerimeterAndRestoresItWhenCleared()
        {
            var root = new GameObject("Official Board Highlight Rim");
            Material fill = null;
            Material rim = null;
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                view.Rebuild(OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap));
                var tile = view.CurrentTiles.First();
                var edge = tile.GetComponentInChildren<LineRenderer>(true);
                var restingMaterial = edge.sharedMaterial;
                var restingWidth = edge.startWidth;

                Assert.That(edge.enabled, Is.False, "An ordinary tile has no resting outline.");

                fill = new Material(Shader.Find("Standard"));
                rim = new Material(Shader.Find("Standard"));
                tile.SetHighlighted(true, fill, rim);

                Assert.That(tile.IsRimmed, Is.True);
                Assert.That(edge.enabled, Is.True, "Highlighting lights the perimeter it was already carrying.");
                Assert.That(edge.sharedMaterial, Is.SameAs(rim));
                Assert.That(edge.startWidth, Is.GreaterThan(restingWidth),
                    "and draws it heavier than the debug line, because it is a cue rather than a hairline.");

                tile.SetHighlighted(false, fill, rim);

                Assert.That(tile.IsRimmed, Is.False);
                Assert.That(edge.enabled, Is.False, "Clearing it restores the outline-free resting state.");
                Assert.That(edge.sharedMaterial, Is.SameAs(restingMaterial));
                Assert.That(edge.startWidth, Is.EqualTo(restingWidth).Within(1e-4f));
            }
            finally
            {
                if (fill != null) UnityEngine.Object.DestroyImmediate(fill);
                if (rim != null) UnityEngine.Object.DestroyImmediate(rim);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        /// <summary>Where a rim's colour lives now that the material is emissive rather than lit.</summary>
        private static Vector3 RimColourOf(GuardsDigital.Presentation.Match.DestinationHighlightMaterialOwner owner)
        {
            var colour = owner.RimMaterial.GetColor("_EmissionColor");
            return new Vector3(colour.r, colour.g, colour.b);
        }

        private static BoardMapDefinition CreateForeignMap()
        {
            var zone = new BoardMapZone(new ZoneId("shared-zone"), "Shared", "lane", null, false, true);
            var space = new BoardMapSpace(
                new SpaceId("q0_r0"), new HexCoordinate(0, 0), BoardMapSpaceKind.Playable, zone.Id);
            var spawn = new BoardMapSpawnPoint(new SpawnPointId("shared-spawn"), space.Id, "red", "hero");
            return new BoardMapDefinition(
                new MapId("not-the-official-map"), 1, "Not The Official Map",
                new[] { space }, new[] { zone }, new[] { spawn }, Array.Empty<ZoneId>());
        }

        private sealed class Landmark
        {
            public Landmark(string spaceId, int q, int r, float boardX, float boardY)
            {
                SpaceId = spaceId;
                Q = q;
                R = r;
                BoardX = boardX;
                BoardY = boardY;
            }

            public string SpaceId { get; }
            public int Q { get; }
            public int R { get; }
            public float BoardX { get; }
            public float BoardY { get; }
        }
    }
}
