using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class PrimitiveHeroViewTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Configure_PlacesViewFromAuthoritativeOccupancy()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                var occupancy = new HeroOccupancy(new HeroId("test-hero"), new HexCoordinate(2, -1));

                view.Configure(occupancy, 1.25f);

                var expectedPosition = HexWorldLayout.ToWorld(occupancy.Coordinate, 1.25f) +
                    (Vector3.up * view.VerticalOffset);
                Assert.That(view.Occupancy, Is.EqualTo(occupancy));
                Assert.That(Vector3.Distance(view.transform.localPosition, expectedPosition), Is.LessThan(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnsurePrimitiveVisual_CreatesVisiblePlaceholderMesh()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();

                view.EnsurePrimitiveVisual();

                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CardStatePipsRemainColliderFreeAndRenderAboveEveryHeroHighlight()
        {
            var gameObject = new GameObject("Primitive Hero Card State Test");
            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                view.EnsurePrimitiveVisual();
                view.SetTargetCue(BoardTargetCueState.Selected, BoardTargetConsequenceIntent.Affect);
                view.SetActiveEffectMark(true, Color.magenta);
                view.SetCardStateIndicator(
                    true,
                    CardColor.Red,
                    new[] { CardColor.Blue, CardColor.Green, CardColor.Gold, CardColor.Silver },
                    0);

                var indicator = view.CardStateIndicator;
                Assert.That(indicator, Is.Not.Null);
                Assert.That(indicator.PipCount, Is.EqualTo(5), "One top current pip plus four hand pips.");
                Assert.That(indicator.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(indicator.Renderers, Is.Not.Empty);
                Assert.That(indicator.Renderers.All(renderer =>
                    renderer.sharedMaterial.renderQueue == HeroCardStateIndicator.OverlayRenderQueue &&
                    renderer.sortingOrder == HeroCardStateIndicator.OverlaySortingOrder &&
                    renderer.sharedMaterial.GetInt("_Cull") == (int)UnityEngine.Rendering.CullMode.Off), Is.True);
                Assert.That(indicator.Renderers.All(renderer =>
                    renderer.GetComponent<MeshFilter>().sharedMesh.normals.All(normal => normal.y > 0.99f)), Is.True,
                    "The board camera looks down; upward winding must not be back-face culled.");
                Assert.That(indicator.Renderers.Min(renderer => renderer.bounds.min.y),
                    Is.GreaterThan(gameObject.GetComponent<MeshRenderer>().bounds.max.y),
                    "Every pip is physically above the hero as well as in the top render queue.");
                Assert.That(view.TargetCueState, Is.EqualTo(BoardTargetCueState.Selected));
                Assert.That(view.HasActiveEffectMark, Is.True,
                    "Existing highlight layers continue underneath rather than being replaced.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Milestone 8.11b. A hero this project has no prepared token for keeps the generated prism,
        /// which is the tracked checkout's ordinary state rather than a fault.
        /// </summary>
        [Test]
        public void WithNoPreparedToken_TheHeroKeepsTheGeneratedPrismAndItsOwnPointerRadius()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();

                view.EnsureVisual();

                Assert.That(view.HasTokenVisual, Is.False,
                    "A view with no content identity resolves no face, and never faults over one.");
                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1));
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterials, Has.Length.EqualTo(1));
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Null);
                Assert.That(view.SelectionRadius,
                    Is.EqualTo(PrimitiveHeroView.DefaultBaseRadius).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Milestone 8.11b. Which face a hero would use is decided by projected content identity and
        /// authoritative team, and by nothing else - the resource is addressed the same way
        /// <see cref="StandardMinionArtCatalog"/> addresses a minion's.
        /// </summary>
        [Test]
        public void AHeroTokenIsAddressedByContentIdentityAndTeam_AndAbsenceIsAnOrdinaryAnswer()
        {
            var wasp = new GuardsDigital.Core.Setup.HeroContentId("wasp");
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Red),
                Is.EqualTo("Private/Heroes/wasp-red"));
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Blue),
                Is.EqualTo("Private/Heroes/wasp-blue"));
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Red),
                Is.Not.EqualTo(
                    HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Blue)),
                "so the two teams can never resolve one face.");
            Assert.That(
                HeroTokenArtwork.ResourcePath(
                    new GuardsDigital.Core.Setup.HeroContentId("tigerclaw"),
                    GuardsDigital.Core.Participants.TeamSide.Red),
                Is.EqualTo("Private/Heroes/tigerclaw-red"),
                "and a hero this project has not reached yet needs no new rule to be addressed.");

            Assert.That(
                HeroTokenArtwork.TryLoadTexture(
                    new GuardsDigital.Core.Setup.HeroContentId("no-such-hero"),
                    GuardsDigital.Core.Participants.TeamSide.Red,
                    out var absent),
                Is.False,
                "An absent face answers false rather than throwing: it is the tracked state.");
            Assert.That(absent, Is.Null);

            // The team is stated in this project's own colour, from the side rather than the picture,
            // and it is the same colour the generated prism has always carried.
            Assert.That(
                HeroTokenArtwork.TeamRim(GuardsDigital.Core.Participants.TeamSide.Red),
                Is.Not.EqualTo(HeroTokenArtwork.TeamRim(GuardsDigital.Core.Participants.TeamSide.Blue)));
            Assert.That(HeroTokenArtwork.FaceRadius,
                Is.LessThan(HeroTokenArtwork.TokenOuterRadius),
                "The rim stands outside the face rather than over it.");
        }

        [Test]
        public void TargetRing_FitsHeroBoundsAndRetainsOneInstanceAcrossStateChanges()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                view.Configure(
                    new HeroOccupancy(new HeroId("ring-hero"), new HexCoordinate(2, -1)),
                    HexWorldLayout.DefaultOuterRadius);
                view.EnsurePrimitiveVisual();

                var ring = view.TargetRing;
                Assert.That(ring, Is.Not.Null);
                var sourceRenderer = gameObject.GetComponent<MeshRenderer>();
                var sourceMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                var sourceMaterials = sourceRenderer.sharedMaterials.ToArray();
                var retainedMesh = ring.GeneratedMesh;
                var retainedMaterials = ring.GetComponent<MeshRenderer>().sharedMaterials.ToArray();
                Assert.That(gameObject.GetComponentsInChildren<BoardTargetRing>(true),
                    Has.Length.EqualTo(1));
                Assert.That(ring.OwnedNativeResourceCount, Is.EqualTo(4));
                Assert.That(ring.IsVisible, Is.False);
                Assert.That(gameObject.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                    "Piece targeting no longer creates the old triangle TextMesh or its backing.");

                view.SetTargetCue(
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect);

                var footprint = gameObject.GetComponent<MeshRenderer>().bounds;
                Assert.That(view.TargetRing, Is.SameAs(ring));
                Assert.That(ring.State, Is.EqualTo(BoardTargetCueState.Eligible));
                Assert.That(ring.ConsequenceIntent, Is.EqualTo(BoardTargetConsequenceIntent.Affect));
                Assert.That(ring.InnerRadius,
                    Is.EqualTo(Mathf.Max(footprint.extents.x, footprint.extents.z)).Within(Tolerance),
                    "The reported radius is the current footprint's maximum board extent; the " +
                    "focused silhouette regression verifies every non-circular inner edge.");
                Assert.That(ring.transform.position.x,
                    Is.EqualTo(footprint.center.x).Within(Tolerance));
                Assert.That(ring.transform.position.z,
                    Is.EqualTo(footprint.center.z).Within(Tolerance));
                Assert.That(ring.transform.position.y,
                    Is.EqualTo(BoardTargetRing.BoardSurfaceOffset).Within(Tolerance),
                    "The ring is on the authoritative board plane, not hung above the piece.");
                Assert.That(Vector3.Angle(ring.transform.up, Vector3.up), Is.LessThan(0.001f),
                    "The generated annulus lies flat on the world XZ board plane.");
                Assert.That(
                    Vector3.Angle(ring.transform.forward, BoardChoiceCue.ScreenUpInWorld),
                    Is.LessThan(0.001f));
                Assert.That(ring.GetComponentsInChildren<Collider>(true), Is.Empty,
                    "The passive ring must not enlarge or intercept the hero input surface.");
                Assert.That(ring.GetComponent<MeshRenderer>().sortingLayerID,
                    Is.EqualTo(sourceRenderer.sortingLayerID));
                Assert.That(ring.GetComponent<MeshRenderer>().sortingOrder,
                    Is.EqualTo(sourceRenderer.sortingOrder + 1));
                // Milestone 8.62, at the user's direction: "Don't color on the inside." This used to
                // measure that the translucent wash lay just over the hero's art; there is no wash
                // to measure, so it measures that the piece's own artwork is left alone.
                Assert.That(ring.GeneratedMesh.GetTriangles(2), Is.Empty,
                    "The cue draws its ring on the board plane and nothing over the piece itself.");

                view.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Removal);
                view.SetTargetCue(
                    BoardTargetCueState.Hovered,
                    BoardTargetConsequenceIntent.Affect);
                view.SetTargetCue(
                    BoardTargetCueState.None,
                    BoardTargetConsequenceIntent.None);

                Assert.That(view.TargetRing, Is.SameAs(ring));
                Assert.That(gameObject.GetComponentsInChildren<BoardTargetRing>(true),
                    Has.Length.EqualTo(1));
                Assert.That(ring.GeneratedMesh, Is.SameAs(retainedMesh));
                Assert.That(ring.GetComponent<MeshRenderer>().sharedMaterials[0],
                    Is.SameAs(retainedMaterials[0]));
                Assert.That(ring.GetComponent<MeshRenderer>().sharedMaterials[1],
                    Is.SameAs(retainedMaterials[1]));
                Assert.That(ring.GetComponent<MeshRenderer>().sharedMaterials[2],
                    Is.SameAs(retainedMaterials[2]));
                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(sourceMesh));
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterials,
                    Is.EqualTo(sourceMaterials),
                    "The wash overlays the hero; it never replaces or tints the source art resources.");
                Assert.That(ring.OwnedNativeResourceCount, Is.EqualTo(4),
                    "State changes retain one mesh and three materials rather than growing resources.");
                Assert.That(ring.State, Is.EqualTo(BoardTargetCueState.None));
                Assert.That(ring.ConsequenceIntent, Is.EqualTo(BoardTargetConsequenceIntent.None));
                Assert.That(ring.IsVisible, Is.False,
                    "Clearing disables the retained renderer rather than destroying its owner.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TargetRing_InnerBoundaryMeetsEntireRenderedFootprint_ForPlacedHexTokenAndBluePrimitiveHero()
        {
            var heroObject = new GameObject("Blue Primitive Hero Ring Fit Test");
            var tokenObject = new GameObject("Placed Hex Token Ring Fit Test");

            try
            {
                var hero = heroObject.AddComponent<PrimitiveHeroView>();
                var teamSide = typeof(PrimitiveHeroView).GetField(
                    "teamSide", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(teamSide, Is.Not.Null,
                    "The fixture must select the tracked Blue four-sided fallback explicitly.");
                teamSide.SetValue(hero, TeamSide.Blue);
                heroObject.GetComponent<MeshFilter>().sharedMesh = null;
                hero.EnsurePrimitiveVisual();
                hero.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);

                var owner = new HeroId("ring-fit-token-owner");
                var token = new PlacedToken(
                    new TokenInstanceId("ring-fit-token"),
                    SyntheticComponentDefinitions.HazardDefinition,
                    owner,
                    owner,
                    new CardInstanceId("ring-fit-token-source"),
                    new HexCoordinate(0, 0));
                var tokenView = tokenObject.AddComponent<PlacedTokenView>();
                tokenView.Configure(token, HexWorldLayout.DefaultOuterRadius);
                tokenView.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);

                var mismatches = new List<string>();
                CollectInnerBoundaryMismatches(
                    "Blue four-sided primitive hero",
                    heroObject.GetComponent<MeshFilter>(),
                    hero.TargetRing,
                    expectedPerimeterVertices: 4,
                    mismatches);
                CollectInnerBoundaryMismatches(
                    "placed hex token",
                    tokenObject.GetComponent<MeshFilter>(),
                    tokenView.TargetRing,
                    expectedPerimeterVertices: 6,
                    mismatches);

                Assert.That(mismatches, Is.Empty,
                    "The neutral keyline's inner boundary must meet every source-mesh edge, not " +
                    "only the footprint's furthest vertices, and nothing at all is " +
                    "drawn inside it.\n" + string.Join("\n", mismatches));
            }
            finally
            {
                Object.DestroyImmediate(tokenObject);
                Object.DestroyImmediate(heroObject);
            }
        }

        [Test]
        public void TargetRing_DomainRestorationReclaimsItsRetainedNativeResources()
        {
            var gameObject = new GameObject("Target Ring Restoration Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                view.EnsurePrimitiveVisual();
                view.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);
                var ring = view.TargetRing;
                var retainedMesh = ring.GeneratedMesh;
                var retainedMaterials = ring.GetComponent<MeshRenderer>().sharedMaterials.ToArray();

                foreach (var fieldName in new[]
                         {
                             "ownedMesh",
                             "ownedKeylineMaterial",
                             "ownedConsequenceMaterial",
                             "ownedWashMaterial",
                             "meshFilter",
                             "meshRenderer"
                         })
                {
                    var field = typeof(BoardTargetRing).GetField(
                        fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.That(field, Is.Not.Null);
                    field.SetValue(ring, null);
                }

                var restored = BoardTargetRing.GetOrCreate(gameObject.transform);

                Assert.That(restored, Is.SameAs(ring));
                Assert.That(restored.GeneratedMesh, Is.SameAs(retainedMesh));
                Assert.That(restored.GetComponent<MeshRenderer>().sharedMaterials[0],
                    Is.SameAs(retainedMaterials[0]));
                Assert.That(restored.GetComponent<MeshRenderer>().sharedMaterials[1],
                    Is.SameAs(retainedMaterials[1]));
                Assert.That(restored.GetComponent<MeshRenderer>().sharedMaterials[2],
                    Is.SameAs(retainedMaterials[2]));
                Assert.That(restored.OwnedNativeResourceCount, Is.EqualTo(4));
                Assert.That(gameObject.GetComponentsInChildren<BoardTargetRing>(true),
                    Has.Length.EqualTo(1),
                    "Restoration adopts the exact retained resources instead of orphaning them " +
                    "behind a second cue or replacement native objects.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TargetRing_ExplicitReleaseDetachesAndDestroysAllRetainedNativeResources()
        {
            var gameObject = new GameObject("Target Ring Disposal Test");
            var view = gameObject.AddComponent<PrimitiveHeroView>();
            view.EnsurePrimitiveVisual();
            view.SetTargetCue(
                BoardTargetCueState.Selected,
                BoardTargetConsequenceIntent.Attack);
            var ring = view.TargetRing;
            var mesh = ring.GeneratedMesh;
            var materials = ring.GetComponent<MeshRenderer>().sharedMaterials.ToArray();

            typeof(BoardTargetRing).GetMethod(
                    "ReleaseOwnedResources",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ring, null);
            Assert.That(mesh == null, Is.True,
                "Direct cue disposal must destroy its detached generated mesh.");
            Assert.That(materials, Has.All.Matches<Material>(material => material == null),
                "Direct cue disposal must destroy all three detached generated materials.");
            Assert.That(ring.OwnedNativeResourceCount, Is.EqualTo(0));
            Assert.That(ring.GetComponent<MeshFilter>().sharedMesh, Is.Null);
            Assert.That(ring.GetComponent<MeshRenderer>().sharedMaterials, Is.Empty);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TargetRing_UsesTypedAffectRemovalPalettesAndOneNeutralKeyline()
        {
            var gameObject = new GameObject("Primitive Hero Palette Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                view.EnsurePrimitiveVisual();

                AssertColor(BoardTargetRing.AffectColor, new Color32(27, 129, 71, 255));
                AssertColor(BoardTargetRing.BloodRedColor, new Color32(194, 10, 24, 255));
                AssertColor(BoardTargetRing.KeylineColor, new Color32(248, 247, 242, 255));
                // Milestone 8.62 re-pins these six numbers at the user's direction, after they looked
                // at the cue twice. First, on the shipped board: "can you make targeting highlights
                // more visible, they are almost impossible to see like this.. Blood red light instead
                // of this pink i think." Then, on the photographs of the answer - a wider ring and a
                // 40% blood-red wash of the piece's footprint: "I like the new outline, but coloring
                // on the inside is a bit too much", and finally "actually, just keep the previous
                // shape, and use the new color for the outline. Don't color on the inside."
                //
                // So the SHAPES are the shapes this cue always had, and THE WASH IS GONE. What makes
                // a target visible now is that the ring is drawn at all - 8.30h had left it built and
                // never activated - in the blood red it has declared since 8.11d.
                Assert.That(BoardTargetRing.EligibleWidth, Is.EqualTo(0.11f));
                Assert.That(BoardTargetRing.SelectedWidth, Is.EqualTo(0.055f));
                Assert.That(BoardTargetRing.HoverWidth, Is.EqualTo(0.11f));
                Assert.That(BoardTargetRing.EligibleWashAlpha, Is.Zero);
                Assert.That(BoardTargetRing.SelectedWashAlpha, Is.Zero);
                Assert.That(BoardTargetRing.HoverWashAlpha, Is.Zero);
                // THE RETIRED PIN. These three used to be ordered 0.18 < 0.23 < 0.28 and the
                // strongest was DECLARED to be `BoardOverlayMaterial.OverlayAlpha`, the reviewed cap
                // for a transparent board overlay ("The strongest reserved wash never exceeds the
                // reviewed transparent-overlay cap"). There is no wash to order or to cap: the ring
                // carries the cue, and the three states are told apart by its own treatment -
                // segmented, doubled, solid with ticks - which
                // `TargetRing_ConstructsSegmentedDoubleAndSolidFourTickTreatments` states below.
                Assert.That(BoardOverlayMaterial.OverlayAlpha, Is.GreaterThan(0f),
                    "The overlay cap still governs every overlay that IS a tint; this cue is not " +
                    "one of them any more.");

                view.SetTargetCue(
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect);
                var ring = view.TargetRing;
                AssertColor(ring.VisibleConsequenceColor, BoardTargetRing.AffectColor);
                AssertColor(ring.VisibleKeylineColor, BoardTargetRing.KeylineColor);
                AssertColor(ring.VisibleWashColor, new Color(
                    BoardTargetRing.AffectColor.r,
                    BoardTargetRing.AffectColor.g,
                    BoardTargetRing.AffectColor.b,
                    BoardTargetRing.EligibleWashAlpha));
                var washMaterial = ring.GetComponent<MeshRenderer>().sharedMaterials[2];
                Assert.That(washMaterial.renderQueue,
                    Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent));
                Assert.That(washMaterial.GetFloat("_Surface"), Is.EqualTo(1f));
                Assert.That(washMaterial.GetInt("_SrcBlend"),
                    Is.EqualTo((int)UnityEngine.Rendering.BlendMode.SrcAlpha));
                Assert.That(washMaterial.GetInt("_DstBlend"),
                    Is.EqualTo((int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                Assert.That(washMaterial.GetInt("_ZWrite"), Is.Zero);
                Assert.That(washMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True,
                    "The wash material is RETAINED - one cue owns four native resources - and is " +
                    "still genuinely blended; what changed at 8.62 is that it paints nothing.");
                Assert.That(ring.GeneratedMesh.GetTriangles(2), Is.Empty,
                    "and the wash is not built at all - the user's own words: don't color on the " +
                    "inside.");
                StringAssert.Contains("DARK GREEN", ring.AssistiveReading);

                view.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);
                AssertColor(ring.VisibleConsequenceColor, BoardTargetRing.AffectColor,
                    "Selected changes ring count, not the operation's dark-green palette.");
                Assert.That(ring.VisibleWashColor.a,
                    Is.EqualTo(BoardTargetRing.SelectedWashAlpha).Within(Tolerance),
                    "Nothing is washed in any state: the DOUBLE RING is the whole of Selected.");
                Assert.That(ring.GeneratedMesh.GetTriangles(2), Is.Empty);

                view.SetTargetCue(
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Removal);
                AssertColor(ring.VisibleConsequenceColor, BoardTargetRing.BloodRedColor);
                AssertColor(ring.VisibleWashColor, new Color(
                    BoardTargetRing.BloodRedColor.r,
                    BoardTargetRing.BloodRedColor.g,
                    BoardTargetRing.BloodRedColor.b,
                    BoardTargetRing.EligibleWashAlpha));
                AssertColor(ring.VisibleKeylineColor, BoardTargetRing.KeylineColor,
                    "Removal and non-removal operations use the same light-neutral keyline.");
                StringAssert.Contains("BLOOD RED", ring.AssistiveReading);

                view.SetTargetCue(
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Attack);
                AssertColor(ring.VisibleConsequenceColor, BoardTargetRing.BloodRedColor);
                AssertColor(ring.VisibleWashColor, new Color(
                    BoardTargetRing.BloodRedColor.r,
                    BoardTargetRing.BloodRedColor.g,
                    BoardTargetRing.BloodRedColor.b,
                    BoardTargetRing.EligibleWashAlpha));
                StringAssert.Contains("ATTACK - BLOOD RED", ring.AssistiveReading);

                view.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Removal);
                AssertColor(ring.VisibleConsequenceColor, BoardTargetRing.BloodRedColor,
                    "Selected changes ring count, not the operation's blood-red palette.");
                Assert.That(ring.VisibleWashColor.a,
                    Is.EqualTo(BoardTargetRing.SelectedWashAlpha).Within(Tolerance));

                view.SetTargetCue(
                    BoardTargetCueState.Hovered,
                    BoardTargetConsequenceIntent.Affect);
                AssertColor(
                    ring.VisibleConsequenceColor,
                    Color.Lerp(BoardTargetRing.AffectColor, Color.white, 0.28f),
                    "Only the reserved Hover treatment brightens the base operation colour.");
                AssertColor(ring.VisibleKeylineColor, BoardTargetRing.KeylineColor);
                Assert.That(ring.VisibleWashColor.a,
                    Is.EqualTo(BoardTargetRing.HoverWashAlpha).Within(Tolerance));

                Assert.That(
                    () => view.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.Affect),
                    Throws.ArgumentException,
                    "A hidden ring cannot retain stale consequence intent.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TargetRing_ConstructsSegmentedDoubleAndSolidFourTickTreatments()
        {
            var gameObject = new GameObject("Primitive Hero Geometry Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                view.EnsurePrimitiveVisual();

                view.SetTargetCue(
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect);
                var ring = view.TargetRing;
                Assert.That(
                    CountTriangleComponents(ring.GeneratedMesh, 1),
                    Is.EqualTo(BoardTargetRing.EligibleSegmentCount),
                    "Eligible is one thick segmented ring.");

                view.SetTargetCue(
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);
                Assert.That(CountTriangleComponents(ring.GeneratedMesh, 1), Is.EqualTo(2),
                    "Selected is exactly two complete annuli.");

                view.SetTargetCue(
                    BoardTargetCueState.Hovered,
                    BoardTargetConsequenceIntent.Affect);
                var mesh = ring.GeneratedMesh;
                Assert.That(CountTriangleComponents(mesh, 1),
                    Is.EqualTo(1 + BoardTargetRing.HoverTickCount),
                    "Hover has one solid annulus and four tick quads.");

                var components = TriangleComponents(mesh, 1);
                var ringComponent = components.OrderByDescending(value => value.Count).First();
                var ringVertices = ringComponent.Select(index => mesh.vertices[index]).ToArray();
                var tickComponents = components.Where(value => !ReferenceEquals(value, ringComponent))
                    .ToArray();
                Assert.That(tickComponents, Has.Length.EqualTo(BoardTargetRing.HoverTickCount));
                foreach (var tickComponent in tickComponents)
                {
                    var tickVertices = tickComponent.Select(index => mesh.vertices[index])
                        .OrderBy(PlanarRadius)
                        .ToArray();
                    Assert.That(tickVertices, Has.Length.EqualTo(4));
                    var innerMidpoint = (tickVertices[0] + tickVertices[1]) * 0.5f;
                    var outerMidpoint = (tickVertices[2] + tickVertices[3]) * 0.5f;
                    var direction = new Vector2(innerMidpoint.x, innerMidpoint.z).normalized;
                    var alignedRingRadii = ringVertices.Where(vertex =>
                            Vector2.Dot(
                                new Vector2(vertex.x, vertex.z).normalized,
                                direction) > 0.9999f)
                        .Select(PlanarRadius)
                        .ToArray();
                    Assert.That(alignedRingRadii, Is.Not.Empty);
                    Assert.That(PlanarRadius(innerMidpoint),
                        Is.InRange(alignedRingRadii.Min(), alignedRingRadii.Max()),
                        "Each tick starts inside the solid ring's actual contour, so there is no " +
                        "visual gap on a circular or polygonal footprint.");
                    Assert.That(
                        PlanarRadius(outerMidpoint) - PlanarRadius(innerMidpoint),
                        Is.GreaterThanOrEqualTo(BoardTargetRing.HoverTickLength),
                        "Each of four ticks extends the required short distance outward.");
                }
                Assert.That(mesh.normals, Has.All.Matches<Vector3>(normal => normal.y > 0.99f),
                    "The annulus and all four corrected tick quads face the overhead camera.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void AssertColor(Color actual, Color expected)
        {
            AssertColor(actual, expected, null);
        }

        private static void AssertColor(Color actual, Color expected, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1f / 255f), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1f / 255f), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1f / 255f), message);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1f / 255f), message);
        }

        private static float PlanarRadius(Vector3 value) =>
            Mathf.Sqrt((value.x * value.x) + (value.z * value.z));

        private static void CollectInnerBoundaryMismatches(
            string label,
            MeshFilter footprint,
            BoardTargetRing ring,
            int expectedPerimeterVertices,
            ICollection<string> mismatches)
        {
            var centre = new Vector2(ring.transform.position.x, ring.transform.position.z);
            if (ring.GeneratedMesh.GetTriangles(2).Length != 0)
            {
                mismatches.Add(
                    $"{label}: the wash submesh carries geometry, but the user settled on " +
                    "\"just keep the previous shape, and use the new color for " +
                    "the outline. Don't color on the inside.\"");
            }

            var perimeter = ProjectedPerimeter(footprint, centre);
            if (perimeter.Count != expectedPerimeterVertices)
            {
                mismatches.Add(
                    $"{label}: expected {expectedPerimeterVertices} source-mesh perimeter vertices " +
                    $"but found {perimeter.Count}.");
                return;
            }

            for (var index = 0; index < perimeter.Count; index++)
            {
                var next = perimeter[(index + 1) % perimeter.Count];
                var direction = (perimeter[index] + next).normalized;
                var footprintDistance = RayBoundaryDistance(perimeter, direction, nearest: false);
                var ringDistance = RayMeshDistance(
                    ring.GeneratedMesh,
                    ring.transform,
                    centre,
                    direction,
                    subMesh: 0,
                    nearest: true);
                if (float.IsNaN(footprintDistance) || float.IsNaN(ringDistance) ||
                    Mathf.Abs(ringDistance - footprintDistance) > Tolerance)
                {
                    mismatches.Add(
                        $"{label} edge {index + 1}: source perimeter={footprintDistance:F4}, " +
                        $"keyline inner boundary={ringDistance:F4}.");
                }
            }
        }

        private static IReadOnlyList<Vector2> ProjectedPerimeter(
            MeshFilter footprint, Vector2 centre)
        {
            var projected = footprint.sharedMesh.vertices
                .Select(vertex => footprint.transform.TransformPoint(vertex))
                .Select(vertex => new Vector2(vertex.x - centre.x, vertex.z - centre.y))
                .ToArray();
            var outerRadius = projected.Max(value => value.magnitude);
            var perimeter = new List<Vector2>();
            foreach (var vertex in projected.Where(value =>
                         Mathf.Abs(value.magnitude - outerRadius) <= Tolerance))
            {
                if (!perimeter.Any(existing => Vector2.Distance(existing, vertex) <= Tolerance))
                {
                    perimeter.Add(vertex);
                }
            }

            return perimeter
                .OrderBy(value => Mathf.Atan2(value.y, value.x))
                .ToArray();
        }

        private static float RayBoundaryDistance(
            IReadOnlyList<Vector2> perimeter, Vector2 direction, bool nearest)
        {
            var distance = nearest ? float.PositiveInfinity : float.NegativeInfinity;
            for (var index = 0; index < perimeter.Count; index++)
            {
                if (TryRaySegmentIntersection(
                        direction,
                        perimeter[index],
                        perimeter[(index + 1) % perimeter.Count],
                        out var intersection))
                {
                    distance = nearest
                        ? Mathf.Min(distance, intersection)
                        : Mathf.Max(distance, intersection);
                }
            }

            return float.IsInfinity(distance) ? float.NaN : distance;
        }

        private static float RayMeshDistance(
            Mesh mesh,
            Transform meshTransform,
            Vector2 centre,
            Vector2 direction,
            int subMesh,
            bool nearest)
        {
            var vertices = mesh.vertices
                .Select(vertex => meshTransform.TransformPoint(vertex))
                .Select(vertex => new Vector2(vertex.x - centre.x, vertex.z - centre.y))
                .ToArray();
            var triangles = mesh.GetTriangles(subMesh);
            var distance = nearest ? float.PositiveInfinity : float.NegativeInfinity;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                for (var edge = 0; edge < 3; edge++)
                {
                    var from = vertices[triangles[index + edge]];
                    var to = vertices[triangles[index + ((edge + 1) % 3)]];
                    if (!TryRaySegmentIntersection(direction, from, to, out var intersection))
                    {
                        continue;
                    }

                    distance = nearest
                        ? Mathf.Min(distance, intersection)
                        : Mathf.Max(distance, intersection);
                }
            }

            return float.IsInfinity(distance) ? float.NaN : distance;
        }

        private static bool TryRaySegmentIntersection(
            Vector2 direction, Vector2 from, Vector2 to, out float distance)
        {
            var edge = to - from;
            var denominator = Cross(direction, edge);
            if (Mathf.Abs(denominator) <= Tolerance)
            {
                distance = 0f;
                return false;
            }

            var rayDistance = Cross(from, edge) / denominator;
            var edgeDistance = Cross(from, direction) / denominator;
            if (rayDistance < 0f || edgeDistance < -Tolerance || edgeDistance > 1f + Tolerance)
            {
                distance = 0f;
                return false;
            }

            distance = rayDistance;
            return true;
        }

        private static float Cross(Vector2 left, Vector2 right) =>
            (left.x * right.y) - (left.y * right.x);

        private static int CountTriangleComponents(Mesh mesh, int subMesh) =>
            TriangleComponents(mesh, subMesh).Count;

        private static IReadOnlyList<HashSet<int>> TriangleComponents(Mesh mesh, int subMesh)
        {
            var triangles = mesh.GetTriangles(subMesh);
            var adjacency = new Dictionary<int, HashSet<int>>();
            for (var index = 0; index < triangles.Length; index += 3)
            {
                Connect(adjacency, triangles[index], triangles[index + 1]);
                Connect(adjacency, triangles[index + 1], triangles[index + 2]);
                Connect(adjacency, triangles[index + 2], triangles[index]);
            }

            var visited = new HashSet<int>();
            var pending = new Stack<int>();
            var components = new List<HashSet<int>>();
            foreach (var vertex in adjacency.Keys)
            {
                if (!visited.Add(vertex))
                {
                    continue;
                }

                var component = new HashSet<int> { vertex };
                components.Add(component);
                pending.Push(vertex);
                while (pending.Count > 0)
                {
                    foreach (var neighbour in adjacency[pending.Pop()])
                    {
                        if (visited.Add(neighbour))
                        {
                            component.Add(neighbour);
                            pending.Push(neighbour);
                        }
                    }
                }
            }

            return components;
        }

        private static void Connect(
            IDictionary<int, HashSet<int>> adjacency,
            int left,
            int right)
        {
            if (!adjacency.TryGetValue(left, out var leftNeighbours))
            {
                leftNeighbours = new HashSet<int>();
                adjacency.Add(left, leftNeighbours);
            }

            if (!adjacency.TryGetValue(right, out var rightNeighbours))
            {
                rightNeighbours = new HashSet<int>();
                adjacency.Add(right, rightNeighbours);
            }

            leftNeighbours.Add(right);
            rightNeighbours.Add(left);
        }
    }
}
