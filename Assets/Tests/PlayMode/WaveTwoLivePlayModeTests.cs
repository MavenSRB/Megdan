using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The three wave-two claims that only a live panel can settle. Milestones 8.35z16, 8.35z18 and
    /// 8.35z19.
    ///
    /// <para>Each of these was reasoned through statically by the branch that built it, and each is
    /// exactly the kind of claim that survives every headless test and then fails on screen: a 2v2
    /// board drawing four heroes rather than six, a tooltip popup that has never once rendered in a
    /// build, and a banner whose whole job is to be visible. The Edit Mode fixtures prove the
    /// timing, the geometry and the summary; these prove the pixels.</para>
    /// </summary>
    public sealed class WaveTwoLivePlayModeTests
    {
        /// <summary>
        /// Milestone 8.35z16. The printed four-player board, drawn. The measured blast radius said
        /// milestone 8.21's `PlaceConfiguredHeroes` already deactivates the hero view of any seat a
        /// configuration did not deal, so nothing predicted a failure here - and nothing had
        /// measured it either, which is the whole reason this journey exists.
        /// </summary>
        [UnityTest]
        public IEnumerator A2v2Board_DrawsFourHeroesTwoASideWithItsRailsBannerAndTray()
        {
            yield return LoadConfiguredScene(MatchFormat.TwoVersusTwoQuick);
            var root = Root();
            var runtime = root.Runtime;

            Assert.That(runtime.Heroes.Count, Is.EqualTo(4),
                "Rulebook p.6's four-player setup deals two heroes a side.");

            // The board itself: four pieces standing, and no stale fifth or sixth view left active
            // where the third seats would be in a 3v3. Team side is read off the drawn view rather
            // than off HeroOccupancy, which carries only identity and a coordinate - and what is
            // drawn is this journey's subject in any case.
            var activeHeroViews = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(
                activeHeroViews.Count(view => view.TeamSide == TeamSide.Red), Is.EqualTo(2),
                "Two Red pieces stand on a printed four-player board.");
            Assert.That(
                activeHeroViews.Count(view => view.TeamSide == TeamSide.Blue), Is.EqualTo(2),
                "Two Blue pieces stand on a printed four-player board.");
            Assert.That(activeHeroViews, Has.Length.EqualTo(4),
                "A 2v2 draws four pieces; a fifth active view is a seat the configuration never " +
                "dealt still standing on the board. Active: " +
                string.Join(", ", activeHeroViews.Select(v => v.Occupancy.HeroId.Value)));

            foreach (var hero in runtime.Heroes)
            {
                Assert.That(
                    activeHeroViews.Any(view => view.Occupancy.HeroId.Equals(hero.HeroId)), Is.True,
                    "Every dealt hero has a drawn piece: " + hero.HeroId.Value);
            }

            // The HUD chrome the amendment's watch list named: rails built for three a side must
            // still lay out two, the tie-breaker rail banner must be present, and the tray must have
            // real width rather than collapsing on a shorter roster.
            var hud = root.Hud;
            Assert.That(hud, Is.Not.Null);
            yield return null;
            yield return null;

            var redRail = hud.RedHeroRailRect;
            var blueRail = hud.BlueHeroRailRect;
            Assert.That(redRail.height, Is.GreaterThan(0f), "The Red rail must lay out at 2v2.");
            Assert.That(blueRail.height, Is.GreaterThan(0f), "The Blue rail must lay out at 2v2.");
            Assert.That(redRail.width, Is.GreaterThan(0f));
            Assert.That(blueRail.width, Is.GreaterThan(0f));
            Assert.That(redRail.Overlaps(blueRail), Is.False,
                "Two a side is strictly slacker than the measured three-rail fit, so the two rails " +
                "must not have collapsed into one another.");

            var tray = hud.CardPanelRect;
            Assert.That(tray.width, Is.GreaterThan(0f), "The tray must still reserve its footprint.");
            Assert.That(tray.height, Is.GreaterThan(0f));

            var banner = hud.RootElement.Q<VisualElement>("gd-tie-breaker-rail-banner");
            Assert.That(banner, Is.Not.Null,
                "Milestone 8.35z10's tie-breaker rail banner is part of every composed HUD, and a " +
                "shorter roster must not have dropped it.");
        }

        /// <summary>Small text popups stay retired, including disabled and nested controls.</summary>
        [UnityTest]
        public IEnumerator SmallTextTooltips_StayHiddenOverEnabledDisabledAndNestedControls()
        {
            yield return LoadConfiguredScene(MatchFormat.Default);
            var hudRoot = Root().Hud.RootElement;
            Assert.That(hudRoot.panel, Is.Not.Null);
            var popup = hudRoot.Q<VisualElement>("gd-runtime-tooltip");
            Assert.That(popup, Is.Not.Null);
            var probe = new Button { text = "PROBE", tooltip = "Unavailable action explanation." };
            var label = new Label("Value") { tooltip = "Current card details." };
            probe.Add(label);
            probe.style.position = Position.Absolute;
            probe.style.left = 40f;
            probe.style.top = 40f;
            probe.style.width = 160f;
            probe.style.height = 60f;
            hudRoot.Add(probe);
            yield return null;
            yield return null;

            foreach (var disabled in new[] { false, true })
            {
                probe.SetEnabled(!disabled);
                foreach (var target in new VisualElement[] { probe, label })
                {
                    SendPointerMove(target, target.worldBound.center);
                    yield return new WaitForSecondsRealtime(0.65f);
                    Assert.That(popup.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    Assert.That(popup.resolvedStyle.opacity, Is.EqualTo(0f));
                }
            }

            Assert.That(label.tooltip, Is.EqualTo("Current card details."),
                "Retiring the popup does not erase underlying metadata.");
            probe.RemoveFromHierarchy();
        }

        /// <summary>
        /// Milestone 8.35z18. The Director-only sandbox banner: it draws for a staged board, names
        /// what was staged, and goes away with the rest of the Director tools when the lobby hands
        /// this HUD an online seat.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSandboxBanner_DrawsForAStagedBoardAndHidesForAnOnlineSeat()
        {
            var staged = new SandboxStagingDescription(waveCount: 5);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = staged;
            yield return LoadConfiguredScene(MatchFormat.Default);

            var root = Root();
            Assert.That(root.ComposedSandboxStaging, Is.Not.Null,
                "The composition root must have consumed the staging it was handed.");
            Assert.That(root.Runtime.IsSandbox, Is.True,
                "The runtime must report that it began from a staged board.");

            var hud = root.Hud;
            yield return null;
            yield return null;

            var banner = hud.RootElement.Q<VisualElement>("gd-sandbox-banner");
            Assert.That(banner, Is.Not.Null);
            Assert.That(hud.IsSandboxBannerVisible, Is.True,
                "A tester on a staged board must be able to see that they are on one without " +
                "opening anything.");
            Assert.That(banner.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            var lines = banner.Query<Label>().ToList().Select(label => label.text).ToArray();
            Assert.That(lines.Any(line => line.Contains(SandboxStagingSummary.Title)), Is.True);
            Assert.That(
                lines.Any(line => line.Contains("Not available in online seats")), Is.True,
                "The banner states DEV-004's constraint where the person who needs it will read it.");
            Assert.That(lines.Any(line => line.Contains("5 Wave counters")), Is.True,
                "The banner names what this staging actually changed.");

            // The lobby handing this HUD an online seat takes the Director's tools away, and the
            // banner is one of them - the second of two independent reasons a seat never sees one.
            hud.SetDirectorToolsVisible(false);
            yield return null;
            Assert.That(hud.IsSandboxBannerVisible, Is.False,
                "The sandbox banner is a Director tool and goes with the rest of them.");
        }

        [TearDown]
        public void ClearOneShots()
        {
            MatchSceneCompositionRoot.NextComposedSandboxStaging = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = null;
        }

        /// <summary>
        /// One pointer move at a panel position, targeted at the element under it. Pooled from a
        /// synthesised `Event` rather than by assigning `position`, whose setter is not public -
        /// the same shape `WaspUiPlayerDriver` already uses to make a click look like hardware.
        /// </summary>
        private static void SendPointerMove(VisualElement target, Vector2 panelPosition)
        {
            using (var move = PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = panelPosition
            }))
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        private static IEnumerator LoadConfiguredScene(MatchFormat format)
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedMatchFormat = format;
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
    }
}
