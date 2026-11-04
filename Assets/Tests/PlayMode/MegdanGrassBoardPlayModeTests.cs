using System.Collections;
using System.IO;
using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MegdanGrassBoardPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConfiguredBoard_DrawsStableGrassAndZoneBordersWithoutTheGuardsFace()
        {
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            yield return null;
            yield return null;

            var grid = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.IsUnderlayActive, Is.False);
            Assert.That(grid.CurrentTiles, Has.Count.EqualTo(292));
            Assert.That(grid.CurrentTiles.All(tile => tile.GetComponent<MeshRenderer>().enabled), Is.True);
            var possibleBattleZones = grid.RenderedDefinition.Zones.Where(zone => zone.Kind == "lane")
                .Select(zone => zone.Id).ToHashSet();
            Assert.That(possibleBattleZones, Has.Count.EqualTo(3));
            Assert.That(grid.CurrentTiles.All(tile =>
            {
                var expected = tile.IsPlayable && possibleBattleZones.Contains(tile.ZoneId.Value);
                return tile.GetComponentInChildren<LineRenderer>(true).enabled == expected;
            }), Is.True);

            var materials = grid.CurrentTiles.Select(tile =>
                tile.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
            if (MegdanGrassTileCatalog.TryLoadTextures(out _) &&
                MegdanGrassTileCatalog.TryLoadBiomeTextures(out _, out _, out _))
            {
                Assert.That(materials.Select(material => material.mainTexture).Distinct().Count(), Is.EqualTo(7));
            }

            yield return null;
            CaptureBoard(Object.FindAnyObjectByType<Camera>(),
                Path.GetFullPath("tmp/w160-megdan-grass-board.png"));
        }

        private static void CaptureBoard(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
            var target = new RenderTexture(1920, 1080, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            Texture2D capture = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                capture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, capture.EncodeToPNG());
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(10000));
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (capture != null) Object.Destroy(capture);
                target.Release();
                Object.Destroy(target);
            }
        }
    }
}
