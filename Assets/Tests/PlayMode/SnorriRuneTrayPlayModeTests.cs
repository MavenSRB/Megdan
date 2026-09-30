using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.93f. Snorri's runes, placed through the real pop-up by pointer, stand in his
    /// tray's numbered turn slots and above the live turn's card slot.
    /// </summary>
    public sealed class SnorriRuneTrayPlayModeTests
    {
        internal static HeroId Snorri(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "snorri").HeroId;

        [UnityTest]
        public IEnumerator InscribedRunes_StandInTheirTraySlotsAndAboveTheLiveTurnsCard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSnorri);
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            var hud = root.Hud.RootElement;
            Assert.That(Named(hud, "gd-turn-slot-marker"), Is.Empty,
                "No rune stands in a slot before Inscribe the Runes resolves.");

            yield return CommitEverySeat(root, snorri,
                NameOf(runtime, SnorriSilverFace.InscribeTheRunes));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Rune N goes to the slot this permutation names: not the identity, so a view that
            // drew runes by slot order rather than by assignment would fail.
            var slotFor = new[] { 0, 3, 1, 4, 2 };
            for (var rune = 1; rune <= 4; rune++)
            {
                var piece = hud.Q<Button>("gd-rune-piece-" + rune);
                Assert.That(piece, Is.Not.Null, UiPlayer.Describe(root.Hud));
                Assert.That(piece.Q("gd-rune-glyph-" + rune), Is.Not.Null,
                    "The pop-up piece draws the printed glyph.");
                UiPlayer.Click(piece);
                yield return null;
                UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slotFor[rune]));
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "Placing rune " + rune + " must not commit before DONE.");
                Assert.That(runtime.PendingEffectChoice.SelectedCandidates.Count, Is.EqualTo(rune),
                    "rune " + rune + " placed; " + UiPlayer.Describe(root.Hud));
            }
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri), Is.Empty,
                "The four physical pieces stay unassigned until DONE.");
            var done = hud.Q<Button>("gd-rune-slot-done");
            Assert.That(done, Is.Not.Null, "DONE after four placements; " +
                UiPlayer.Describe(root.Hud));
            UiPlayer.Click(done);
            yield return null;
            yield return null;

            var held = runtime.ReadModel.MarkersHeldBy(snorri).ToArray();
            Assert.That(held, Has.Length.EqualTo(4));
            for (var rune = 1; rune <= 4; rune++)
                Assert.That(held.Single(marker => marker.Definition.Id.Equals(
                        SnorriComponentContent.Runes[rune - 1].Id)).TurnSlotIndex,
                    Is.EqualTo(slotFor[rune]));

            var slotMarkers = Named(hud, "gd-turn-slot-marker")
                .Where(UiPlayer.IsShown).ToArray();
            Assert.That(slotMarkers, Has.Length.EqualTo(4),
                "Each numbered slot of Snorri's tray shows the rune assigned to it.");
            for (var rune = 1; rune <= 4; rune++)
                Assert.That(slotMarkers.Count(marker => marker.tooltip ==
                        "Rune " + rune + " assigned to turn " + slotFor[rune]),
                    Is.EqualTo(1), "Rune " + rune);
            Assert.That(slotMarkers.All(marker =>
                    marker.resolvedStyle.backgroundImage.texture != null),
                "Every tray rune draws the prepared glyph, not the numeric fallback.");

            var live = Named(hud, "gd-current-turn-marker").Where(UiPlayer.IsShown).ToArray();
            Assert.That(live, Has.Length.EqualTo(1));
            Assert.That(live[0].tooltip, Is.EqualTo("Rune 2 assigned to turn 1"),
                "Turn 1's rune stands above the resolving card's Initiative.");
            yield return UiPlayer.CaptureScreen("tmp/893f-snorri-rune-tray-1920x1080.png");

            // The live-turn rune follows the turn: every seat ends turn 1 through END TURN, and
            // before any turn-2 commitment the empty current-card slot shows turn 2's rune.
            for (var budget = 0; budget < 24 && runtime.ReadModel.TurnIndex < 2; budget++)
            {
                var release = runtime.RoundView.Model.EndTurnInvocation;
                if (release != null)
                    UiPlayer.Click(UiPlayer.Button(root.Hud, release.Label));
                yield return null;
            }
            Assert.That(runtime.ReadModel.TurnIndex, Is.EqualTo(2), UiPlayer.Describe(root.Hud));
            yield return null;
            live = Named(hud, "gd-current-turn-marker").Where(UiPlayer.IsShown).ToArray();
            Assert.That(live, Has.Length.EqualTo(1));
            Assert.That(live[0].tooltip, Is.EqualTo("Rune 4 assigned to turn 2"));
            Assert.That(Named(hud, "gd-turn-slot-marker").Count(UiPlayer.IsShown),
                Is.EqualTo(4), "The tray keeps all four runes as the turn advances.");
            yield return UiPlayer.CaptureScreen("tmp/893f-snorri-rune-turn2-1920x1080.png");

            // With turn 2's card committed and resolving, the rune still stands above it.
            yield return CommitEverySeat(root, snorri, NameOf(runtime, SnorriAttackFaces.RuneSigils));
            yield return WaitForActor(root, snorri);
            live = Named(hud, "gd-current-turn-marker").Where(UiPlayer.IsShown).ToArray();
            Assert.That(live, Has.Length.EqualTo(1));
            Assert.That(live[0].tooltip, Is.EqualTo("Rune 4 assigned to turn 2"));
            Assert.That(live[0].parent.Q("gd-current-card"), Is.Not.Null,
                "The rune shares the slot with the resolving card.");
            yield return UiPlayer.CaptureScreen("tmp/893f-snorri-rune-resolving-1920x1080.png");
            Healthy(runtime);
        }


        /// <summary>
        /// Milestone 9.20, the user's "center them on card slots, and just have like a picture of
        /// hammer sigil anvil hook". Each tray rune is drawn centred on the numbered slot its
        /// <c>TurnSlotIndex</c> names, and what it draws is its ordinal's printed glyph cut off the
        /// card stock - a transparent ground, not the opaque printed tile.
        /// </summary>
        [UnityTest]
        public IEnumerator InscribedRunes_AreTheirPrintedGlyphsCentredOnTheirTraySlots()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSnorri);
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            var hud = root.Hud.RootElement;

            yield return CommitEverySeat(root, snorri,
                NameOf(runtime, SnorriSilverFace.InscribeTheRunes));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var slotFor = new[] { 0, 3, 1, 4, 2 };
            for (var rune = 1; rune <= 4; rune++)
            {
                UiPlayer.Click(hud.Q<Button>("gd-rune-piece-" + rune));
                yield return null;
                UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slotFor[rune]));
                yield return null;
            }
            UiPlayer.Click(hud.Q<Button>("gd-rune-slot-done"));
            yield return null;
            yield return null;
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri).Count(), Is.EqualTo(4),
                UiPlayer.Describe(root.Hud));
            // Let the resolved card's flight into the tray finish before the photograph.
            yield return new UnityEngine.WaitForSecondsRealtime(2f);
            yield return UiPlayer.CaptureScreen("tmp/9-20-runes-tray-1920x1080.png");

            var slotMarkers = Named(hud, "gd-turn-slot-marker").Where(UiPlayer.IsShown).ToArray();
            Assert.That(slotMarkers, Has.Length.EqualTo(4));
            // Every rune is checked before anything is reported, so one reading names every miss.
            var misses = new System.Collections.Generic.List<string>();
            for (var rune = 1; rune <= 4; rune++)
            {
                var marker = slotMarkers.Single(value =>
                    value.tooltip == "Rune " + rune + " assigned to turn " + slotFor[rune]);
                var slot = marker.parent.Q("gd-turn-slot");
                if (slot == null)
                {
                    misses.Add("rune " + rune + " stands outside its slot's frame");
                    continue;
                }

                var centre = marker.worldBound.center;
                var slotCentre = slot.worldBound.center;
                if (UnityEngine.Mathf.Abs(centre.x - slotCentre.x) > 0.75f ||
                    UnityEngine.Mathf.Abs(centre.y - slotCentre.y) > 0.75f)
                    misses.Add("rune " + rune + " is centred at " + centre + ", turn slot " +
                        slotFor[rune] + " at " + slotCentre);

                var resource = GuardsDigital.Presentation.Match.TurnSlotMarkerViewModel
                    .RuneGlyphResourcePath(rune);
                var texture = marker.resolvedStyle.backgroundImage.texture;
                var glyph = System.IO.Path.GetFileName(resource) + " (glyph)";
                if (texture == null || texture.name != glyph)
                {
                    misses.Add("rune " + rune + " draws " +
                        (texture == null ? "no picture" : "'" + texture.name + "'") +
                        ", not '" + glyph + "'");
                    continue;
                }

                if (!texture.isReadable)
                {
                    misses.Add("rune " + rune + " draws an unreadable texture");
                    continue;
                }

                var pixels = texture.GetPixels32();
                var width = texture.width;
                var corners = new[]
                {
                    pixels[0], pixels[width - 1],
                    pixels[pixels.Length - width], pixels[pixels.Length - 1]
                };
                if (!corners.All(pixel => pixel.a == 0))
                    misses.Add("rune " + rune + "'s ground is not transparent");
                if (pixels.Count(pixel => pixel.a == 255) <= pixels.Length / 8)
                    misses.Add("rune " + rune + "'s glyph is not drawn solid");

                // The user's "a tad bit smaller so they fit the card": what the glyph draws - its
                // figure and outline, not its transparent margin - stays inside the card with a
                // visible margin on every side.
                int minX = width, maxX = -1, minY = texture.height, maxY = -1;
                for (var index = 0; index < pixels.Length; index++)
                {
                    if (pixels[index].a == 0) continue;
                    minX = System.Math.Min(minX, index % width);
                    maxX = System.Math.Max(maxX, index % width);
                    minY = System.Math.Min(minY, index / width);
                    maxY = System.Math.Max(maxY, index / width);
                }

                var drawnWidth = marker.worldBound.width * (maxX - minX + 1) / width;
                var drawnHeight = marker.worldBound.height * (maxY - minY + 1) / texture.height;
                if (drawnWidth > slot.worldBound.width - (2f * CardMargin) ||
                    drawnHeight > slot.worldBound.height - (2f * CardMargin))
                    misses.Add("rune " + rune + " draws " + drawnWidth.ToString("0.0") + " x " +
                        drawnHeight.ToString("0.0") + " on a " + slot.worldBound.width.ToString("0.0") +
                        " x " + slot.worldBound.height.ToString("0.0") + " card, leaving under " +
                        CardMargin + " each side");
            }

            // The live turn's rune over the header's card is the same glyph at the same size, to
            // within the one screen pixel (4/3 of a panel unit at 1920x1080) layout snapping moves it.
            var live = Named(hud, "gd-current-turn-marker").Where(UiPlayer.IsShown).ToArray();
            var trayTwin = slotMarkers.Single(value => value.tooltip == "Rune 2 assigned to turn 1");
            if (live.Length != 1 || live[0].tooltip != trayTwin.tooltip)
                misses.Add("the live turn shows " + live.Length + " rune(s), not rune 2");
            else if (live[0].resolvedStyle.backgroundImage.texture !=
                     trayTwin.resolvedStyle.backgroundImage.texture ||
                     UnityEngine.Mathf.Abs(live[0].worldBound.width - trayTwin.worldBound.width) > 1.5f ||
                     UnityEngine.Mathf.Abs(live[0].worldBound.height - trayTwin.worldBound.height) > 1.5f)
                misses.Add("the live turn's rune is " + live[0].worldBound.size + ", the tray's " +
                    trayTwin.worldBound.size);

            Assert.That(misses, Is.Empty, string.Join("; ", misses));
            Healthy(runtime);
        }

        // Milestone 9.20: the least clear card a tray glyph leaves on each side, in panel units.
        private const float CardMargin = 2f;

        private static VisualElement[] Named(VisualElement root, string name) =>
            UiPlayer.Descendants(root).Where(element => element.name == name).ToArray();
    }
}
