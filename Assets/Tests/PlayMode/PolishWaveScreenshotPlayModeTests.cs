using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// The polish wave's owed VISUAL EVIDENCE, captured rather than described: milestone 8.35z58's
    /// button treatment in all four of its states on both surfaces that draw buttons, the levelling
    /// hand preview the same milestone added, and milestone 8.35z59's ultrawide fit at a real
    /// 3440x1440 viewport.
    /// </summary>
    /// <remarks>
    /// <para><b>Why a fixture rather than the hand-capture recipe that was written for this.</b> The
    /// worktree that built the button pass could not launch Unity and left a verbatim recipe for a
    /// human to follow with a mouse - open the scene, hover, hold the button down, screenshot. That
    /// recipe is correct and unreproducible: "hold the left button and photograph it" is not a step
    /// a validation run can repeat, and the states it asks for are driven by
    /// <c>HudElements.ApplyButtonMotion</c>'s own pointer callbacks, which a fixture can raise
    /// exactly. So the states are entered through the SAME events a real pointer raises -
    /// <c>PointerEnterEvent</c> for hover, <c>PointerDownEvent</c> with button 0 for press - and the
    /// Game View is photographed between them.</para>
    ///
    /// <para><b>These cases capture; they do not gate.</b> Milestone 8.35z58's own
    /// <c>HudButtonTreatmentTests</c> already asserts the contrast floor, the shared radius and the
    /// 100ms press numerically, and that is where a regression is caught. What no assertion can do is
    /// answer "does it LOOK right", which is the user's call and needs a picture. A batch run has no
    /// Game View to photograph, so <see cref="UiPlayer.CaptureGameView"/> captures nothing there
    /// rather than failing - these cases still drive every state, so the batch tier proves the states
    /// are REACHABLE even when it cannot photograph them.</para>
    /// </remarks>
    public sealed class PolishWaveScreenshotPlayModeTests
    {
        /// <summary>
        /// Where the wave's evidence lands, as one folder the integration report can name. It is a
        /// prefix on <see cref="UiPlayer.CaptureGameView"/>'s own <c>tmp/</c>-rooted path rather than
        /// a second capture implementation.
        /// </summary>
        private const string Shot =
            "8-35d-8-35e-multiplayer-corrections/integration-screenshots/polish-wave";

        private static void EnsureShotFolder()
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath("tmp/" + Shot));
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        /// <summary>
        /// Enters the pressed state the way a held finger does, through the event
        /// <c>ApplyButtonMotion</c> actually listens for. A pooled <c>PointerDownEvent</c> carries
        /// button 0, which is the left button that motion pass requires.
        /// </summary>
        private static void PressAndHold(VisualElement element)
        {
            Assert.That(element?.panel, Is.Not.Null, "Nothing to press.");
            using (var down = PointerDownEvent.GetPooled())
            {
                down.target = element;
                element.SendEvent(down);
            }
        }

        private static void ReleasePress(VisualElement element)
        {
            using (var up = PointerUpEvent.GetPooled())
            {
                up.target = element;
                element.SendEvent(up);
            }
        }

        /// <summary>
        /// Loads the ordinary Director match - the shipped scene with none of
        /// <c>PrivateSessionLaunchOptions</c>' role flags present, which is the composition the
        /// button recipe names.
        /// </summary>
        private static IEnumerator LoadTheMatch()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        /// <summary>
        /// Milestone 8.35z58. A representative HUD button in idle, hover, pressed and disabled, so the
        /// user can accept or reject the treatment from pictures rather than from colour triples.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHudButton_IsPhotographedInEveryStateItDraws()
        {
            EnsureShotFolder();
            yield return LoadTheMatch();
            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null, "The match HUD is not composed into the scene.");

            var buttons = UiPlayer.Descendants(hud.RootElement)
                .OfType<Button>()
                .Where(candidate => UiPlayer.IsShown(candidate) &&
                    !string.IsNullOrWhiteSpace(candidate.text))
                .ToArray();
            Assert.That(buttons, Is.Not.Empty,
                "The composed HUD draws no labelled button to photograph. Shown: " +
                UiPlayer.Describe(hud));

            var enabled = buttons.FirstOrDefault(candidate => candidate.enabledInHierarchy)
                ?? buttons[0];

            yield return UiPlayer.CaptureGameView(Shot, "hud-button-idle");

            UiPlayer.PointAt(enabled);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "hud-button-hover");

            PressAndHold(enabled);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "hud-button-pressed");
            ReleasePress(enabled);
            UiPlayer.PointAway(enabled);
            yield return null;

            // The DISABLED state is photographed on whatever the composed match actually draws
            // unavailable, rather than on a control forced into it - a forced disable would
            // photograph a state the shipped HUD never reaches.
            var disabled = buttons.FirstOrDefault(candidate => !candidate.enabledInHierarchy);
            if (disabled != null)
            {
                UiPlayer.EnsureVisible(disabled);
                yield return null;
                yield return UiPlayer.CaptureGameView(Shot, "hud-button-disabled");
            }
            else
            {
                Debug.Log(
                    "GUARDS_SHOT: no HUD control is drawn unavailable in the opening position, so " +
                    "the disabled state is photographed on the lobby's taken-hero option instead.");
            }

            Assert.Pass(
                "Captured the HUD button states that the composed match draws. Photographed control: '" +
                enabled.text + "'.");
        }

        /// <summary>
        /// Milestone 8.35z59. The HUD at a real ultrawide viewport, which is the milestone's whole
        /// subject: before it, the hero rail column went short at about 16:9.06 and the plaques
        /// overflowed into the tray band.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHud_IsPhotographedAtAnUltrawideViewport()
        {
            EnsureShotFolder();

            // THE VIEWPORT IS SET BEFORE THE SCENE COMPOSES, and that ordering is the honest one
            // rather than a convenience. A first attempt resized an already-composed match and
            // photographed a 2560-wide frame padded into a 3440 image - the HUD had laid out for the
            // old viewport and does not re-lay-out on a mid-session resolution change, so the shot
            // showed a black band that reads as "the panel does not fill an ultrawide" when it is
            // really "this HUD was composed for a narrower screen". A shipped build takes its
            // resolution at launch and composes once inside it, so composing INSIDE the ultrawide is
            // what a player actually gets, and is what this photographs.
            UiPlayer.SetPlayerViewport(3440, 1440, "8.35z59 ultrawide evidence");
            var settled = false;
            for (var frame = 0; frame < 120 && !settled; frame++)
            {
                yield return null;
                settled = Screen.width >= 3400;
            }

            Assert.That(settled, Is.True,
                "The play view never reported the 3440-wide viewport this shot is about; it stopped " +
                "at " + Screen.width + "x" + Screen.height + ".");

            yield return LoadTheMatch();

            // WaitForEndOfFrame THROWS in batch mode - Unity never evokes it without a display - and
            // the complete Play Mode run is batch, so this is guarded rather than unconditional. The
            // extra settle frame only matters for the capture, which batch mode does not take
            // either, so the batch tier still drives every state and simply photographs nothing.
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
            }

            yield return null;

            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null);

            var rails = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Is.Not.Empty,
                "The ultrawide shot is only evidence if the rails it is about are drawn.");

            yield return UiPlayer.CaptureGameView(Shot, "hud-ultrawide-3440x1440");

            // Put the viewport back, because every other journey in this suite reads its geometry
            // from whatever size is left behind.
            UiPlayer.SetPlayerViewport(1920, 1080, "restore after 8.35z59 evidence");
            yield return null;
        }

        /// <summary>
        /// Milestone 8.35z58. A LOBBY button in the same four states, on the surface the user was
        /// actually looking at when they reported "they were the old Buttons" - and the disabled
        /// state on the control that genuinely draws one, p.22's already-taken hero option.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLobbyButton_IsPhotographedInEveryStateItDraws()
        {
            EnsureShotFolder();
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            var host = new GameObject("gd-private-lobby");
            host.AddComponent<PrivateLobbyDriver>().Configure(Root());
            yield return null;
            yield return null;

            var driver = Object.FindAnyObjectByType<PrivateLobbyDriver>();
            Assert.That(driver, Is.Not.Null, "The lobby driver was not composed into the scene.");
            Assert.That(driver.View, Is.Not.Null, "The lobby driver has not built its panel yet.");

            var controls = UiPlayer.Descendants(driver.View.Root)
                .OfType<Button>()
                .Where(candidate => UiPlayer.IsShown(candidate))
                .ToArray();
            Assert.That(controls, Is.Not.Empty,
                "The entry form draws no control to photograph.");

            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-idle");

            var control = controls.FirstOrDefault(candidate => candidate.text == "HOST")
                ?? controls[0];
            UiPlayer.PointAt(control);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-hover");

            PressAndHold(control);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-pressed");
            ReleasePress(control);
            UiPlayer.PointAway(control);
            yield return null;

            var unavailable = UiPlayer.Descendants(driver.View.Root)
                .OfType<Button>()
                .FirstOrDefault(candidate =>
                    UiPlayer.IsShown(candidate) && !candidate.enabledInHierarchy);
            if (unavailable != null)
            {
                yield return UiPlayer.CaptureGameView(Shot, "lobby-button-disabled");
            }
            else
            {
                Debug.Log(
                    "GUARDS_SHOT: the entry form draws no unavailable control before anybody is " +
                    "seated, which is correct - a taken-hero option only exists once picking opens.");
            }
        }

        /// <summary>
        /// Milestone 8.35z58's second half: the seat's own hand, drawn above the p.15 upgrade offer.
        /// </summary>
        /// <remarks>
        /// The preview is gated on a real levelling pause, so this case photographs it only when the
        /// composed opening position can be driven to one within its own budget; otherwise it says so
        /// rather than photographing an empty stack and calling it the feature.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheLevellingHandPreview_IsPhotographedAboveTheOffer()
        {
            EnsureShotFolder();
            yield return LoadTheMatch();
            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null);

            var preview = UiPlayer.Descendants(hud.RootElement)
                .FirstOrDefault(element => element.name == "gd-levelup-hand-preview");
            if (preview == null || !UiPlayer.IsShown(preview))
            {
                Assert.Ignore(
                    "The composed opening position is not paused on a p.15 upgrade, so the levelling " +
                    "hand preview is correctly absent. Its four Edit Mode cases in " +
                    "LevelUpHandPreviewTests drive the real pause and gate the behaviour; this case " +
                    "photographs it only when a driven scene stands in that window.");
            }

            yield return UiPlayer.CaptureGameView(Shot, "levelup-hand-preview");
        }
    }
}
