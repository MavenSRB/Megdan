using System.Collections;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class GrassDecorationStandeeTests
    {
        [UnityTest]
        public IEnumerator OriginalFourTilesSwitchTogetherWithoutChangingGroundOrPicking()
        {
            var root = new GameObject("Four grass standee inspection");
            var cameraHost = new GameObject("Grass inspection camera", typeof(Camera));
            var tiles = new HexTileView[4];
            var materials = new Material[4];
            Mesh tileMesh = null;
            try
            {
                Assert.That(MegdanGrassTileCatalog.TryLoadTextures(out var textures), Is.True);
                tileMesh = FlatHexMeshBuilder.Create();
                for (int i = 0; i < 4; i++)
                {
                    var obj = new GameObject("Original grass " + i, typeof(MeshFilter), typeof(MeshRenderer));
                    obj.transform.SetParent(root.transform);
                    obj.GetComponent<MeshFilter>().sharedMesh = tileMesh;
                    materials[i] = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    materials[i].SetTexture("_BaseMap", textures[i]);
                    obj.GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                    tiles[i] = obj.AddComponent<HexTileView>();
                    tiles[i].Configure(new HexCoordinate(i % 2 * 2, i / 2 * 2), 1);
                }
                var camera = cameraHost.GetComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 3.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f,.1f,.13f);
                var focus = Vector3.zero;
                foreach (var tile in tiles) focus += tile.transform.position / 4f;
                camera.transform.rotation = Quaternion.Euler(50,0,0);
                camera.transform.position = focus - camera.transform.forward * 12;
                var decoration = root.AddComponent<GrassDecorationStandees>();
                decoration.Configure(tiles, camera);
                Assert.That(decoration.DecorationCount, Is.EqualTo(14));
                Assert.That(decoration.IsStanding, Is.True);
                var firstPanel = root.transform.Find("Grass decoration standees").GetChild(0);
                var firstPatch = root.transform.Find("Grass decoration standees").GetChild(1);
                var blocker = new Bounds(firstPanel.position, Vector3.one * .1f);
                decoration.ApplyClearance(new[] { blocker });
                Assert.That(firstPanel.gameObject.activeSelf, Is.False, "A piece excludes an intersecting standee.");
                Assert.That(firstPatch.gameObject.activeSelf, Is.False, "Its flat original must return together with it.");
                decoration.Refresh(camera);
                decoration.ApplyClearance(new[] { blocker });
                Assert.That(firstPanel.gameObject.activeSelf, Is.False, "Inactive panels must keep stable clearance bounds.");
                decoration.ApplyClearance(System.Array.Empty<Bounds>());
                Assert.That(firstPanel.gameObject.activeSelf, Is.True, "Moving/removing a piece restores the decoration.");
                Assert.That(firstPatch.gameObject.activeSelf, Is.True);
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                yield return null;
                Capture(camera,"tmp/grass-standees-restart-angled.png");
                foreach (var point in new[] { (79.5f,true),(80.1f,false),(79.5f,false),(78f,true),(90f,false) })
                {
                    camera.transform.rotation = Quaternion.Euler(point.Item1,0,0);
                    decoration.Refresh(camera);
                    Assert.That(decoration.IsStanding, Is.EqualTo(point.Item2));
                }
                camera.transform.position = focus - camera.transform.forward * 12;
                for (int i=0;i<4;i++)
                    Assert.That(tiles[i].GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(materials[i]));
                Capture(camera,"tmp/grass-standees-restart-top-down.png");
                decoration.Configure(tiles,camera);
                Assert.That(decoration.DecorationCount,Is.EqualTo(14),"Rebuild must replace decoration inventory.");
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(cameraHost); Object.Destroy(tileMesh);
                foreach (var material in materials) Object.Destroy(material);
            }
        }
        private static void Capture(Camera camera,string path)
        {
            var target = RenderTexture.GetTemporary(1200,900,24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(1200,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1200,900),0,0); image.Apply();
                Directory.CreateDirectory("tmp"); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget; RenderTexture.active=oldActive;
                Object.Destroy(image); RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
