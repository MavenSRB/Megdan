using System.Linq;
using GuardsDigital.Presentation.Board;
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
