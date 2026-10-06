using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroTokenBindingTests
    {
        [TestCase("emmitt")]
        [TestCase("nebkher")]
        public void FinalTwoHeroes_HavePreparedStandeeFaces(string hero)
        {
            var content = new HeroContentId(hero);
            var neutral = Resources.Load<Texture2D>(HeroTokenArtwork.NeutralResourcePath(content));
            foreach (var team in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var baked = Resources.Load<Texture2D>(HeroTokenArtwork.ResourcePath(content, team));
                var expected = baked != null ? baked : neutral;
                Assert.That(expected, Is.Not.Null,
                    "run Tools/Prepare-HeroTokens.ps1 -Hero " + hero +
                    (hero == "emmitt" ? "" : " -Neutral") + " in this checkout");
                var presentation = HeroTokenArtwork.ResolveToken(content, team, out var resolved);
                Assert.That(presentation.FaceKind, Is.Not.EqualTo(TokenFaceKind.GeneratedPrism));
                Assert.That(resolved, Is.SameAs(expected),
                    hero + " should show its available prepared face on either team.");
                Assert.That(resolved.width, Is.GreaterThan(0));
                Assert.That(resolved.height, Is.GreaterThan(0));
            }
        }

        [Test]
        public void BoardAndRail_UseTheAvailableFaceAndItsTeamRing(
            [Values("wasp", "arien", "brogan", "sabina", "tigerclaw", "xargatha",
                "dodger", "misa", "silverarrow", "ursafar", "whisper",
                // Milestone 8.75b. The twenty-second hero's standee, on the same terms every value
                // above it is on: her two team faces are prepared as ignored private resources, and
                // a source-only checkout proves the explicit fallback instead of skipping.
                "ignatia", "emmitt", "nebkher",
                "no-such-hero")] string hero,
            [Values(TeamSide.Red, TeamSide.Blue)] TeamSide team)
        {
            var content = new HeroContentId(hero);
            // Independently resolve expected private resources; a source-only checkout also proves
            // the explicit primitive/empty-portrait fallback rather than skipping the test.
            var baked = Resources.Load<Texture2D>(HeroTokenArtwork.ResourcePath(content, team));
            var neutral = Resources.Load<Texture2D>(HeroTokenArtwork.NeutralResourcePath(content));
            var expected = baked != null ? baked : neutral;
            var faceRadius = baked != null ? RuntimeTeamIdentity.TeamBakedFaceRadius :
                RuntimeTeamIdentity.NeutralFaceRadius;
            var piece = new GameObject("Token binding regression");
            try
            {
                var board = piece.AddComponent<PrimitiveHeroView>();
                var serialized = new SerializedObject(board);
                serialized.FindProperty("heroContentId").stringValue = hero;
                serialized.FindProperty("teamSide").intValue = (int)team;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                board.EnsureVisual();
                var rail = new HeroOverviewsUnityView();
                rail.Render(new HeroOverviewsViewModel(0, new[] {
                    new HeroOverviewViewModel(new HeroId("test-hero"), team, content, hero, 1, 0,
                        Array.Empty<HeroItemBonusViewModel>(), Array.Empty<HeroTurnSlotViewModel>(),
                        Array.Empty<TrayCardViewModel>(), false)
                }));
                var icon = rail.Root.Q<VisualElement>("gd-hero-icon");
                Assert.That(new[] {
                    board.HasTokenVisual == (expected != null),
                    icon.style.backgroundImage.value.texture == expected
                }, Is.All.True, hero + " board and rail must both bind the available face.");
                Assert.That(icon.tooltip, Does.Contain(team.ToString().ToLowerInvariant()));
                if (expected == null) return;
                Assert.That(icon.style.width.value.value,
                    Is.EqualTo(50f * faceRadius /
                        RuntimeTeamIdentity.TeamBakedFaceRadius).Within(0.0001f));
                var mesh = piece.GetComponent<MeshFilter>().sharedMesh;
                var materials = piece.GetComponent<MeshRenderer>().sharedMaterials;
                Assert.That(materials[1].mainTexture, Is.SameAs(expected));
                Assert.That(materials[0].color, Is.EqualTo(HeroTokenArtwork.TeamRim(team)));
                Assert.That(new Vector2(mesh.vertices[1].x, mesh.vertices[1].z).magnitude,
                    Is.EqualTo(faceRadius).Within(0.0001f));
                Assert.That(board.SelectionRadius, Is.EqualTo(HeroTokenArtwork.TokenOuterRadius));
                board.EnsureVisual();
                Assert.That(mesh == null, Is.True, "Rebinding retires its owned mesh.");
                Assert.That(materials.All(material => material == null), Is.True,
                    "Rebinding retires owned materials, not shared private textures.");
                Assert.That(expected != null, Is.True);
            }
            finally { Object.DestroyImmediate(piece); }
        }
    }
}
