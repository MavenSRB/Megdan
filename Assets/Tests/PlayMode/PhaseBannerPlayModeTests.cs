using System.Collections;
using System.Linq;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.47. The phase banner on the composed scene: it lands centred on the HUD panel,
    /// over everything, takes no pointer, and takes itself down again after its own total time.
    /// </summary>
    public sealed class PhaseBannerPlayModeTests
    {
        [UnityTest]
        public IEnumerator TheBanner_LandsCentredOverTheHud_AndTakesItselfDown()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            var runtime = root.Runtime;
            Assert.That(runtime, Is.Not.Null);
            runtime.RefreshAll();
            yield return null;
            Assert.That(runtime.PhaseBannerView.IsShowing, Is.False,
                "The first projection primes the detector; nothing has changed yet for this viewer.");

            runtime.PhaseBannerView.Show(PhaseBannerLabels.Commit(1, 1));
            yield return null;
            yield return null;

            var hudRoot = root.Hud.RootElement;
            var host = runtime.PhaseBannerView.Root;
            Assert.That(host.parent, Is.SameAs(hudRoot), "The banner attaches to the HUD's own root.");
            Assert.That(hudRoot.IndexOf(host), Is.EqualTo(hudRoot.childCount - 1), "and sits over everything.");
            Assert.That(runtime.PhaseBannerView.IsShowing, Is.True);
            Assert.That(host.pickingMode, Is.EqualTo(PickingMode.Ignore));

            var plate = host.Q<VisualElement>(PhaseBannerUnityView.PlateName);
            var panel = hudRoot.worldBound;
            var centre = plate.worldBound.center;
            Assert.That(centre.x, Is.EqualTo(panel.center.x).Within(2f), "Centred horizontally on the panel.");
            Assert.That(centre.y, Is.EqualTo(panel.center.y).Within(2f), "Centred vertically on the panel.");
            Assert.That(plate.worldBound.width, Is.LessThan(panel.width), "and inside it.");
            Assert.That(plate.worldBound.height, Is.LessThan(panel.height));

            var line = host.Q<Label>(PhaseBannerUnityView.LineName);
            Assert.That(line.text, Is.EqualTo(PhaseBannerLabels.CommitLine));
            Assert.That(line.worldBound.height, Is.GreaterThan(panel.height * 0.06f),
                "Big enough that nobody misses it: the line alone is over six percent of the panel's height.");

            yield return new WaitForSecondsRealtime(PhaseBannerUnityView.TotalMs / 1000f + 0.5f);
            Assert.That(runtime.PhaseBannerView.IsShowing, Is.False, "Gone on its own.");
            Assert.That(host.parent, Is.Null, "and detached, so no sweep of the HUD finds it.");
        }
    }
}
