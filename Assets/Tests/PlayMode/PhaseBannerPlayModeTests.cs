using System.Collections;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.SabinaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.47. The phase banner on the composed scene: it lands centred on the HUD panel,
    /// over everything, takes no pointer, and takes itself down again after its own total time.
    /// </summary>
    public sealed class PhaseBannerPlayModeTests
    {
        private const string CaptureRoot = "tmp/928-captures";

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

        /// <summary>
        /// Milestone 9.28, on the composed scene: a turn that has finished resolving and waits on its
        /// own player's END TURN says so in the banner, and its END TURN control flashes on the live
        /// panel - really alternating, which only a panel's own scheduler can show - until the press
        /// lands, when both stop at once. Milestone 9.89 adds the full-face treatment to that same
        /// scheduled pulse and modestly shortens its period. The frames go to
        /// <c>tmp/928-captures</c> for the user.
        /// </summary>
        [UnityTest]
        public IEnumerator AFinishedTurn_SaysEndTurnAndFlashesItsControl_UntilReleased()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var sabinaId = SabinaSeat(runtime).HeroId;
            // The composed scene is the Director's; the banner is bound to Sabina's seat the way a
            // player's own client binds it once the lobby admits the seat.
            Assert.That(runtime.PhaseBannerPresenter.SetHomeHero(sabinaId), Is.True);

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.TroopMovement);
            runtime.RefreshAll();
            yield return null;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId), "Arrange: Sabina acts.");

            // p.12's Hold alone, without the release END TURN's own press would chain: a fresh
            // invocation carrying the same projected command is not the drawn control.
            var hold = runtime.RoundView.Model.EndTurnInvocation;
            Assert.That(hold?.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold));
            Assert.That(runtime.RoundPresenter.Submit(new TypedInvocationViewModel(hold.Label, hold.Command)),
                Is.True);
            runtime.RefreshAll();
            yield return new WaitForSecondsRealtime(PhaseBannerUnityView.FadeInMs / 1000f + 0.1f);

            Assert.That(runtime.PendingTurnRelease?.ActorId, Is.EqualTo(sabinaId),
                "Arrange: the finished turn waits on Sabina's own release.");
            Assert.That(runtime.PhaseBannerView.IsShowing, Is.True, "The banner says END TURN.");
            Assert.That(runtime.PhaseBannerView.LineText, Is.EqualTo(PhaseBannerLabels.EndTurnLine));
            Assert.That(runtime.RoundView.EndTurnFlashes, Is.True);
            var control = runtime.RoundView.EndTurnRoot.Q<Button>("gd-end-turn-control");
            Assert.That(control, Is.Not.Null);
            var flash = control.Q(RoundActionUnityView.EndTurnFlashName);
            Assert.That(flash, Is.Not.Null);
            Assert.That(flash.panel, Is.Not.Null, "The flash is on the live panel.");
            Assert.That(flash.worldBound.width, Is.GreaterThan(control.worldBound.width),
                "and rings the control from outside.");

            yield return WaitForFlash(flash, lit: true);
            yield return UiPlayer.CaptureScreen(CaptureRoot + "/end-turn-banner-and-flash.png");

            var lowest = 1f;
            var highest = 0f;
            var until = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < until)
            {
                var opacity = flash.resolvedStyle.opacity;
                lowest = Mathf.Min(lowest, opacity);
                highest = Mathf.Max(highest, opacity);
                yield return null;
            }

            Assert.That(highest, Is.GreaterThan(0.9f), "The flash lights fully.");
            Assert.That(lowest, Is.LessThan(0.3f), "and goes nearly dark: it flashes, it does not glow.");

            yield return new WaitForSecondsRealtime(PhaseBannerUnityView.TotalMs / 1000f);
            Assert.That(runtime.PhaseBannerView.IsShowing, Is.False, "The banner goes on its own time.");
            Assert.That(runtime.RoundView.EndTurnFlashes, Is.True, "The control keeps flashing until pressed.");
            yield return WaitForFlash(flash, lit: true);
            yield return UiPlayer.CaptureScreen(CaptureRoot + "/end-turn-flash-lit.png");
            yield return WaitForFlash(flash, lit: false);
            yield return UiPlayer.CaptureScreen(CaptureRoot + "/end-turn-flash-dark.png");

            Assert.That(runtime.SubmitEndTurn(), Is.True);
            runtime.RefreshAll();
            yield return null;
            Assert.That(runtime.PendingTurnRelease?.ActorId.Equals(sabinaId) ?? false, Is.False,
                "Arrange: the press released her turn.");
            Assert.That(PhaseBannerLabels.IsEndTurn(runtime.PhaseBannerView.Current), Is.False);
            Assert.That(runtime.RoundView.EndTurnFlashes, Is.False, "Pressed, the control stops flashing.");
            Assert.That(control.Q(RoundActionUnityView.EndTurnFlashName)?.panel, Is.Null);
        }

        private static IEnumerator WaitForFlash(VisualElement flash, bool lit)
        {
            var deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline &&
                (lit ? flash.resolvedStyle.opacity < 0.95f : flash.resolvedStyle.opacity > 0.2f))
            {
                yield return null;
            }

            Assert.That(lit ? flash.resolvedStyle.opacity >= 0.95f : flash.resolvedStyle.opacity <= 0.2f,
                Is.True, "The flash reaches its " + (lit ? "lit" : "dark") + " point within three seconds.");
        }
    }
}
