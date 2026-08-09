using System.Collections;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsSevenVisibleHexTilesUnderFixedCamera()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(tiles, Has.Length.EqualTo(7));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile =>
                tile.GetComponent<MeshRenderer>().enabled && tile.GetComponent<MeshFilter>().sharedMesh != null));
        }
    }
}
