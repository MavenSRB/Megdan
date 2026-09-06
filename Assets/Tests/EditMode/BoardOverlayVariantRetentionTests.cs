using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31b. The board's blended overlays must carry their surface state from a material
    /// that ships in the build, not only from runtime calls.
    /// </summary>
    /// <remarks>
    /// <para>A URP material's surface keyword is a shader feature: the build keeps its variant only
    /// when a material in build data uses it. Every board overlay used to be created at runtime and
    /// blended by keyword, so no material on disk was transparent, the variant was stripped, and a
    /// built player drew every tile fill over the artwork as opaque white while the Editor - which
    /// holds all variants - looked right.</para>
    /// <para>These cases pin the shape of the fix rather than its appearance: the templates exist
    /// under Resources, they declare the states the runtime materials need, and the generated
    /// materials are copies of them. What the player actually draws is proven by the board visual
    /// smoke gate, which photographs a built player; a fixture inside the Editor cannot see a
    /// stripped variant at all, and pretending otherwise is how this defect survived.</para>
    /// </remarks>
    public sealed class BoardOverlayVariantRetentionTests
    {
        [Test]
        public void EveryOverlayTemplate_ShipsUnderResources()
        {
            foreach (var path in new[]
            {
                BoardOverlayMaterial.OverlayTemplateResourcePath,
                BoardOverlayMaterial.RimTemplateResourcePath,
                BoardOverlayMaterial.UnlitOverlayTemplateResourcePath
            })
            {
                var template = Resources.Load<Material>(path);
                Assert.That(template, Is.Not.Null,
                    "Resources/" + path + " must exist, or the build keeps no variant for the " +
                    "materials copied from it and a player draws them opaque.");
            }
        }

        [Test]
        public void TheBlendedTemplates_DeclareTransparentSurfaceState()
        {
            foreach (var path in new[]
            {
                BoardOverlayMaterial.OverlayTemplateResourcePath,
                BoardOverlayMaterial.UnlitOverlayTemplateResourcePath
            })
            {
                var template = Resources.Load<Material>(path);
                Assert.That(template.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True,
                    "Resources/" + path + " carries the transparent variant into the build only " +
                    "while it is itself transparent.");
                Assert.That(template.GetFloat("_Surface"), Is.EqualTo(1f),
                    "Resources/" + path + " must be authored as a transparent surface.");
                Assert.That(template.renderQueue,
                    Is.GreaterThanOrEqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent));
            }
        }

        [Test]
        public void TheRimTemplate_DeclaresTheEmissiveStateItsCopiesRelyOn()
        {
            var template = Resources.Load<Material>(BoardOverlayMaterial.RimTemplateResourcePath);

            Assert.That(template.IsKeywordEnabled("_EMISSION"), Is.True);
            Assert.That(template.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"), Is.True);
            Assert.That(template.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"), Is.True,
                "The rim's whole job is to be exactly one colour; its keywords must reach the build.");
        }

        [Test]
        public void GeneratedOverlays_AreCopiesOfTheirTemplateRatherThanOfAFoundShader()
        {
            var overlayTemplate = Resources.Load<Material>(
                BoardOverlayMaterial.OverlayTemplateResourcePath);
            var unlitTemplate = Resources.Load<Material>(
                BoardOverlayMaterial.UnlitOverlayTemplateResourcePath);
            var rimTemplate = Resources.Load<Material>(BoardOverlayMaterial.RimTemplateResourcePath);

            var fill = BoardOverlayMaterial.Create("Test Fill", new Color(1f, 1f, 1f, 0f));
            var unlit = BoardOverlayMaterial.CreateUnlitBlended("Test Unlit", new Color(1f, 0f, 0f, 0.5f));
            var rim = BoardOverlayMaterial.CreateRim("Test Rim", Color.cyan);
            try
            {
                Assert.That(fill.shader, Is.SameAs(overlayTemplate.shader));
                Assert.That(fill.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);
                Assert.That(fill.color.a, Is.EqualTo(0f), "The suppressed fill stays invisible.");

                Assert.That(unlit.shader, Is.SameAs(unlitTemplate.shader));
                Assert.That(unlit.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);

                Assert.That(rim.shader, Is.SameAs(rimTemplate.shader));
                Assert.That(rim.IsKeywordEnabled("_EMISSION"), Is.True);
                Assert.That(rim.GetColor("_EmissionColor"),
                    Is.EqualTo(new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 1f)));
            }
            finally
            {
                foreach (var material in new[] { fill, unlit, rim }.Where(value => value != null))
                {
                    Object.DestroyImmediate(material);
                }
            }
        }

        /// <summary>
        /// Milestone 8.35z39. A luminous mark is a COPY OF THE SAME SHIPPED TEMPLATE the
        /// blended marks use, differing only in its destination blend factor.
        ///
        /// <para>That is the whole safety argument for adding a light family to this board, stated as
        /// a test rather than as a claim in a comment: <c>_SrcBlend</c> and <c>_DstBlend</c> are
        /// ordinary shader properties, not keywords, so an additive material needs no shader variant
        /// that the blended one did not already need - and the blended variant is the one the shipped
        /// template carries into build data. If a future edit ever reaches for a different shader or a
        /// fresh material here, the milestone 8.31b defect returns: correct in the Editor, opaque in
        /// the player, invisible to every fixture that is not this one.</para>
        /// </summary>
        [Test]
        public void LuminousMarks_AreTheSameShippedTemplateWithOnlyTheBlendFactorChanged()
        {
            var unlitTemplate = Resources.Load<Material>(
                BoardOverlayMaterial.UnlitOverlayTemplateResourcePath);

            var blended = BoardOverlayMaterial.CreateUnlitBlended("Test Paint", Color.green);
            var luminous = BoardOverlayMaterial.CreateLuminous("Test Light", Color.green);
            try
            {
                Assert.That(luminous.shader, Is.SameAs(unlitTemplate.shader),
                    "A light is the same shader as the paint it replaces, or it needs a variant the " +
                    "build has no reason to keep.");
                Assert.That(luminous.shader, Is.SameAs(blended.shader));
                Assert.That(luminous.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);
                Assert.That(luminous.renderQueue, Is.EqualTo(blended.renderQueue),
                    "Same queue: a light is not drawn at a different time from the paint it replaces.");

                Assert.That(
                    luminous.GetInt("_DstBlend"),
                    Is.EqualTo((int)UnityEngine.Rendering.BlendMode.One),
                    "ADDING to the board is the one thing that separates light from paint.");
                Assert.That(
                    blended.GetInt("_DstBlend"),
                    Is.EqualTo((int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha),
                    "and paint replaces what it covers, which is what the marks used to do.");
                Assert.That(luminous.GetInt("_SrcBlend"), Is.EqualTo(blended.GetInt("_SrcBlend")),
                    "Only the destination factor differs.");

                Assert.That(BoardOverlayMaterial.IsAdditive(luminous), Is.True);
                Assert.That(BoardOverlayMaterial.IsAdditive(blended), Is.False);

                Assert.That(luminous.color.r, Is.EqualTo(Color.green.r).Within(0.0001f));
                Assert.That(luminous.color.g, Is.EqualTo(Color.green.g).Within(0.0001f));
                Assert.That(luminous.color.b, Is.EqualTo(Color.green.b).Within(0.0001f));
                Assert.That(luminous.color.a,
                    Is.EqualTo(BoardOverlayMaterial.LuminousAlpha).Within(0.0001f),
                    "A light's alpha is how strongly it is added, never how much board it hides.");
            }
            finally
            {
                foreach (var material in new[] { blended, luminous }.Where(value => value != null))
                {
                    Object.DestroyImmediate(material);
                }
            }
        }

        /// <summary>
        /// Milestone 8.35z39. The acting hero's ring keeps its exact geometry and its exact
        /// gold, and is drawn as light.
        ///
        /// <para>The FOURTH amendment asked for "a distinct gold ring" around the acting piece so
        /// spectators can see who is doing something. That is unchanged in every respect a player
        /// could name - same radius margin, same ring width, same colour, same one-instance-per-board
        /// ownership - and the ring now ADDS its gold to the board instead of painting over it, so the
        /// space the actor stands on stays readable through the mark.</para>
        /// </summary>
        [Test]
        public void TheActingHeroRing_KeepsItsGeometryAndGoldAndIsDrawnAsLight()
        {
            var marker = ActingHeroMarker.Create();
            try
            {
                Assert.That(ActingHeroMarker.RingWidthRatio, Is.EqualTo(0.22f).Within(0.0001f),
                    "The ring's own width is not this milestone's to move.");
                Assert.That(ActingHeroMarker.RadiusMargin, Is.EqualTo(1.18f).Within(0.0001f),
                    "nor how far outside the piece it sits.");
                Assert.That(ActingHeroMarker.Subdivisions, Is.EqualTo(48),
                    "nor how round it is.");

                marker.SetVisible(Vector3.zero, 1f);
                Assert.That(marker.IsVisible, Is.True);
                Assert.That(marker.VisibleRadius,
                    Is.EqualTo(ActingHeroMarker.RadiusMargin).Within(0.0001f),
                    "A unit piece rings at exactly the recorded margin, as it always did.");

                var material = marker.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material, Is.Not.Null);
                Assert.That(BoardOverlayMaterial.IsAdditive(material), Is.True,
                    "The ring is light now. An opaque ring is what the user asked to stop seeing.");

                var gold = MatchHudTheme.ActingHeroMarkerColor;
                Assert.That(material.color.r, Is.EqualTo(gold.r).Within(0.0001f));
                Assert.That(material.color.g, Is.EqualTo(gold.g).Within(0.0001f));
                Assert.That(material.color.b, Is.EqualTo(gold.b).Within(0.0001f),
                    "Same gold. The rendering changed; what the ring MEANS did not.");
            }
            finally
            {
                if (marker != null)
                {
                    Object.DestroyImmediate(marker.gameObject);
                }
            }
        }

        [Test]
        public void NoBoardOverlaySource_BuildsABlendedMaterialFromAFoundShader()
        {
            var boardSources = System.IO.Directory.GetFiles(
                BoardSourceRoot(), "*.cs", System.IO.SearchOption.AllDirectories);
            var offenders = boardSources
                .Where(path =>
                {
                    var text = System.IO.File.ReadAllText(path);
                    return text.Contains("ApplyBlending") && text.Contains("Shader.Find");
                })
                .Select(System.IO.Path.GetFileName)
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "A board source blends a material it built from a found shader. The found shader " +
                "reaches the build; the blended variant of it does not, unless the material is " +
                "copied from a template that ships.");
        }

        /// <summary>
        /// Milestone 8.35z39, from the light pass's own first complete Edit Mode run. The composite
        /// mark - an Attack target that is also a Defense contributor - draws a SATURATED core inside
        /// a luminous halo, and the two must not be the same material.
        /// </summary>
        /// <remarks>
        /// <para>The core used to borrow the halo material to get the saturated colour. Once halos
        /// became light that borrow silently handed this core the luminous alpha, and the shipped
        /// Defense-preview assertion caught it: expected alpha 1.000, measured 0.620. This case pins
        /// the distinction directly, from the template rather than through a rendered tile, so the
        /// next change to the light family cannot re-borrow it without a red test.</para>
        /// </remarks>
        [Test]
        public void TheCompositeMarksCore_IsSaturatedPaint_WhileItsHaloIsLight()
        {
            using (var template = new HexCornerHighlightTemplate(1f))
            {
                foreach (var kind in new[]
                {
                    HexCornerHighlightKind.DefensePositive,
                    HexCornerHighlightKind.DefenseNegative
                })
                {
                    var declared = kind == HexCornerHighlightKind.DefensePositive
                        ? HexCornerHighlightTemplate.DefensePositiveColor
                        : HexCornerHighlightTemplate.DefenseNegativeColor;

                    var saturated = template.SaturatedCore(kind);
                    Assert.That(saturated, Is.Not.Null,
                        $"The composite core for {kind} must exist; Set asks for it by name.");
                    Assert.That(BoardOverlayMaterial.IsAdditive(saturated), Is.False,
                        $"The {kind} composite core is PAINT. Added light is what makes it vanish " +
                        "inside the blood-red halo it sits in.");
                    Assert.That(saturated.color, Is.EqualTo(declared),
                        $"The {kind} composite core keeps its declared colour at full alpha - which " +
                        "is exactly what borrowing the halo used to provide and no longer does.");

                    var halo = template.Halo(kind);
                    Assert.That(BoardOverlayMaterial.IsAdditive(halo), Is.True,
                        $"The {kind} halo is light, which is the milestone's whole subject.");
                    Assert.That(halo, Is.Not.SameAs(saturated),
                        "The borrow is what broke. They are two materials now.");
                }
            }
        }

        private static string BoardSourceRoot()
        {
            var roots = new[]
            {
                System.IO.Directory.GetCurrentDirectory(),
                TestContext.CurrentContext.TestDirectory
            };

            foreach (var root in roots)
            {
                var directory = new System.IO.DirectoryInfo(root);
                while (directory != null)
                {
                    var candidate = System.IO.Path.Combine(
                        directory.FullName, "Assets", "GuardsDigital", "Presentation", "Board");
                    if (System.IO.Directory.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            Assert.Fail("Could not locate the board presentation sources.");
            return null;
        }
    }
}
