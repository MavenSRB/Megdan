using System.Collections;
using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 6.1e, finding F613-05. The composition root collects board views with
    /// GetComponentsInChildren immediately after a rebuild. In Play Mode the rebuild retires
    /// superseded views with the deferred UnityEngine.Object.Destroy, which does not take effect
    /// until the end of the frame, so a retired object is still returned by that collection and
    /// can enter authoritative board state.
    /// </summary>
    public sealed class ConfiguredViewCollectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator RebuildThenImmediateCollection_ExcludesViewsAwaitingDeferredDestroy()
        {
            yield return LoadScene();

            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            Assert.That(mapView, Is.Not.Null, "The configured scene must expose a map view.");

            // A serialized minion view with no bound identity is exactly what edit-time scene
            // dirtying (F613-04) leaves behind. The rebuild retires it through deferred Destroy.
            var doomed = new GameObject("Unbound Serialized Minion");
            doomed.transform.SetParent(mapView.transform, false);
            doomed.AddComponent<StandardMinionView>();

            var map = mapView.RenderedDefinition;
            var setup = mapView.RenderedSetup;
            Assert.That(map, Is.Not.Null);
            Assert.That(setup, Is.Not.Null);

            mapView.Rebuild(map, setup);

            // Re-discovery through a child search, which the composition root used before this
            // milestone, and the published set it uses now.
            var rediscovered = mapView.GetComponentsInChildren<StandardMinionView>(true);
            var published = mapView.CurrentMinionViews.ToArray();

            // Let the deferred Destroy actually take effect.
            yield return null;

            // The hazard itself is a Unity behaviour and stays true: a child search performed in the
            // same frame as a deferred Destroy still returns the doomed object. This assertion is the
            // reason the composition root must not re-discover, and it fails if the retire path stops
            // being exercised, which would make the rest of this test vacuous.
            Assert.That(
                rediscovered.Count(view => view == null),
                Is.EqualTo(1),
                "Expected the unbound serialized view to be retired and still returned by a " +
                "same-frame child search; without that, this regression proves nothing.");

            // The fix: the set the composition root consumes contains no doomed object.
            var doomedEntries = published.Count(view => view == null);
            Assert.That(
                doomedEntries,
                Is.Zero,
                "Views the composition root consumes must all survive the frame. " +
                $"{doomedEntries} of {published.Length} published minion views were awaiting " +
                "deferred destruction and would have entered authoritative board state.");
            Assert.That(published, Has.Length.EqualTo(setup.Minions.Count));
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            yield return null;
        }
    }
}
