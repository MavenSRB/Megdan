using System.Collections;
using System.Linq;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class StandeeCameraPlayModeTests
    {
        [UnityTest]
        public IEnumerator CaptureThreeHeroMirrorDemo()
        {
            var roster = GuardsDigital.Application.MegdanSelectionRoster.DemoHeroes;
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(roster.Take(3), roster.Skip(3));
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(1920, 1080, "Three-hero mirrored demo");
            yield return new WaitForSecondsRealtime(2f);
            var root = BroganUiJourneyPlayModeTests.Root();
            var heroes = root.Runtime.BoardView.HeroViews.Where(h => h.gameObject.activeInHierarchy).ToArray();
            Assert.That(heroes.Length, Is.EqualTo(6));
            foreach (var id in new[] { "misa", "rowenna", "tali" })
                Assert.That(heroes.Count(h => h.HeroContentId.Value.Value == id), Is.EqualTo(2));
            var camera = Camera.main;
            var rotation = Quaternion.Euler(40f, 0f, 0f);
            camera.transform.SetPositionAndRotation(-(rotation * Vector3.forward * 18f), rotation);
            camera.orthographicSize = 15f;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return UiPlayer.CaptureScreen("tmp/w186-demo-lineup.png");
            FocusStandee(camera, heroes.First(h => h.HeroContentId.Value.Value == "tali").transform.position);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return UiPlayer.CaptureScreen("tmp/w186-tali-standee.png");
        }

        [UnityTest]
        public IEnumerator CaptureLowAngleGrassInConfiguredMatch()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { "brogan", "misa", "rowenna" }.Select(id => roster.Single(h => h.ContentId.Value == id)),
                new[] { "tigerclaw", "gydion", "tali" }.Select(id => roster.Single(h => h.ContentId.Value == id)));
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(1920, 1080, "W-170 low angle grass in the actual match");
            yield return new WaitForSecondsRealtime(2f);
            var root = BroganUiJourneyPlayModeTests.Root();
            var camera = Camera.main;
            var grass = root.MapView.CurrentTiles.Where(tile =>
                tile.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != null &&
                tile.GetComponent<MeshRenderer>().sharedMaterial.mainTexture.name == "grass-2")
                .OrderBy(tile => tile.transform.position.sqrMagnitude).First();
            var rotation = Quaternion.Euler(25f, 15f, 0f);
            var focus = grass.transform.position + Vector3.up * .3f;
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 18f, rotation);
            camera.orthographicSize = 3.6f;
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(root.MapView.GetComponent<GuardsDigital.Presentation.Board.GrassDecorationStandees>().IsStanding, Is.True);
            yield return UiPlayer.CaptureScreen("tmp/w172-grass-low-angle-live-match.png");
            camera.orthographicSize = 2.1f;
            yield return null;
            yield return UiPlayer.CaptureScreen("tmp/w172-grass-low-angle-detail.png");
            var heroes = root.Runtime.BoardView.HeroViews.Where(h => h.gameObject.activeInHierarchy).ToArray();
            focus = Vector3.zero;
            foreach (var hero in heroes) focus += hero.transform.position / heroes.Length;
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 18f, rotation);
            camera.orthographicSize = 7f;
            yield return new WaitForSecondsRealtime(1f);
            yield return UiPlayer.CaptureScreen("tmp/w178-middle-board-clearance.png");
            foreach (var id in new[] { "brogan", "misa", "rowenna" })
            {
                var hero = heroes.Single(h => h.HeroContentId.Value.Value == id);
                FocusStandee(camera, hero.transform.position);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(hero.Standee.IsStanding, Is.True, id + " must use the illustrated standee");
                yield return UiPlayer.CaptureScreen("tmp/w185-standee-" + id + ".png");
            }
        }

        private static void FocusStandee(Camera camera, Vector3 position)
        {
            camera.transform.position = position + new Vector3(0f, 4f, -7f);
            camera.transform.LookAt(position + Vector3.up * 0.7f);
            camera.orthographicSize = 1.65f;
        }

        [UnityTest]
        public IEnumerator PlayerCanPanOrbitZoomResetAndSwitchRepresentationsInTheLiveMatch()
        {
            var addedKeyboard = Keyboard.current == null;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            var addedMouse = Mouse.current == null;
            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            try
            {
                var roster = RegisteredHeroRoster.ForThisBuild;
                MatchSceneCompositionRoot.NextComposedSeat = null;
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                    new[] { "brogan", "misa", "rowenna" }.Select(id => roster.Single(h => h.ContentId.Value == id)),
                    new[] { "tigerclaw", "gydion", "tali" }.Select(id => roster.Single(h => h.ContentId.Value == id)));
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                UiPlayer.SetPlayerViewport(1920, 1080, "W-165 standees and board camera");
                var root = BroganUiJourneyPlayModeTests.Root();
                var camera = Camera.main;
                var navigation = camera.GetComponent<BoardCameraController>();
                Assert.That(navigation, Is.Not.Null);
                yield return null; yield return null;
                var heroes = root.Runtime.BoardView.HeroViews.Where(v => v.gameObject.activeInHierarchy).ToArray();
                Assert.That(heroes, Has.Length.EqualTo(6));
                var identities = heroes.Select(h => h.Occupancy).ToArray();
                var home = camera.transform.position;
                var boardPoint = new Vector2(Screen.width * 0.55f, Screen.height * 0.68f);
                Assert.That(root.Hud.BlocksPointerAt(boardPoint), Is.False);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint });
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return new WaitForSecondsRealtime(0.15f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(Vector3.Distance(home, camera.transform.position), Is.GreaterThan(0.01f), "Actual WASD input pans.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint, delta = new Vector2(60, 175) }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Middle));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint });
                yield return null;
                Assert.That(navigation.Elevation, Is.LessThan(69f), "Middle drag orbits into the standee range.");
                Assert.That(heroes.All(h => h.Standee.IsStanding), Is.True);
                foreach (var hero in heroes)
                {
                    hero.SetDefenseModifierShield(2);
                    var shield = hero.DefenseModifierShield;
                    shield.RefreshFacing(camera);
                    var offset = shield.transform.position - hero.transform.position;
                    Assert.That(Vector3.Dot(offset, camera.transform.right),
                        Is.EqualTo(-hero.SelectionRadius * HeroDefenseModifierShield.EdgeDistanceFactor).Within(0.001f));
                    Assert.That(Vector3.Dot(shield.ValueText.transform.forward, Vector3.down),
                        Is.GreaterThan(0.999f), "Shield value lies flat on the base as the camera rotates.");
                    Assert.That(Vector3.Dot(shield.ValueText.transform.up,
                        Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized),
                        Is.GreaterThan(0.999f), "Flat shield text follows the camera heading.");
                    var panel = hero.transform.Find("Illustrated standee panel");
                    Assert.That(Vector3.Dot(offset, panel.right),
                        Is.EqualTo(-hero.SelectionRadius * HeroDefenseModifierShield.EdgeDistanceFactor).Within(0.001f),
                        "Shield retains its left-side position relative to the standee.");
                    Assert.That(Vector3.Dot(shield.ValueText.transform.right, panel.right), Is.GreaterThan(0.999f));
                }
                float size = camera.orthographicSize;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint, scroll = new Vector2(0, 120) });
                yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint });
                yield return null;
                Assert.That(camera.orthographicSize, Is.EqualTo(size * 0.88f * 0.88f).Within(0.001f),
                    "A wheel notch now applies twice the previous zoom response.");
                var hudPoint = new Vector2(Screen.width * 0.5f, Screen.height - 8f);
                Assert.That(root.Hud.BlocksPointerAt(hudPoint), Is.True);
                size = camera.orthographicSize;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = hudPoint, scroll = new Vector2(0, 120) });
                yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = boardPoint });
                yield return null;
                Assert.That(camera.orthographicSize, Is.EqualTo(size), "Scrolling HUD does not zoom the board.");
                var manualPosition = camera.transform.position;
                var manualRotation = camera.transform.rotation;
                root.MapView.ReapplyCameraFrame();
                Assert.That(camera.transform.position, Is.EqualTo(manualPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(manualRotation));

                var field = new TextField("Camera input guard");
                root.Hud.RootElement.Add(field);
                field.Focus();
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return new WaitForSecondsRealtime(0.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(camera.transform.position, Is.EqualTo(manualPosition), "Typing must not pan the camera.");
                field.RemoveFromHierarchy();

                var minions = root.MapView.CurrentMinionViews.Where(m => m.gameObject.activeInHierarchy).ToArray();
                Assert.That(minions.Length, Is.GreaterThan(0));
                foreach (var minion in minions)
                {
                    Assert.That(minion.TokenBodyRenderer, Is.Not.Null);
                    Assert.That(minion.TokenBodyRenderer.bounds.size.y, Is.GreaterThan(0.15f));
                    Assert.That(minion.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                    Assert.That(minion.Standee.HasArtwork, Is.True);
                    Assert.That(minion.Standee.IsStanding, Is.True);
                    Assert.That(minion.GetComponent<SpriteRenderer>().enabled, Is.False,
                        "Angled play shows Veljko's panel instead of the flat icon.");
                    var expectedArt = minion.Occupancy.TeamSide.ToString().ToLowerInvariant() + "-" +
                        minion.Occupancy.MinionType.ToString().ToLowerInvariant();
                    Assert.That(minion.Standee.PanelRenderer.sharedMaterials[0].mainTexture.name,
                        Is.EqualTo(expectedArt));
                }
                Assert.That(heroes.Select(h => h.Occupancy), Is.EqualTo(identities));
                navigation.ResetView();
                navigation.Navigate(Vector2.zero, new Vector2(0, 35f), 0, 0);
                yield return new WaitForSecondsRealtime(2f);
                yield return UiPlayer.CaptureScreen("tmp/w165-angled-live-match.png");
                yield return UiPlayer.CaptureScreen("tmp/w187-minion-standees-angled.png");
                // A close view through the same live camera, so panel edges/base depth can be judged.
                var focal = heroes.First(h => h.HeroContentId.Value.Value == "misa").transform.position;
                camera.transform.position = focal + new Vector3(0, 5.5f, -7.5f);
                camera.transform.LookAt(focal + Vector3.up * 0.5f);
                camera.orthographicSize = 4f;
                yield return new WaitForSecondsRealtime(1f);
                yield return UiPlayer.CaptureScreen("tmp/w177-flat-shield-detail.png");
                focal = minions[0].transform.position;
                camera.transform.position = focal + new Vector3(0, 3f, -6f);
                camera.transform.LookAt(focal);
                camera.orthographicSize = 3f;
                yield return new WaitForSecondsRealtime(0.5f);
                yield return UiPlayer.CaptureScreen("tmp/w165-creep-detail.png");
                navigation.ResetView();
                yield return null; yield return null;
                Assert.That(heroes.All(h => !h.Standee.IsStanding), Is.True);
                navigation.Navigate(Vector2.zero, new Vector2(75f, 0), 0, 0);
                yield return null; yield return null;
                foreach (var hero in heroes)
                    Assert.That(Vector3.Dot(hero.transform.forward, camera.transform.up), Is.GreaterThan(0.999f),
                        "Flat hero artwork follows camera heading.");
                foreach (var minion in minions)
                {
                    Assert.That(minion.Standee.IsStanding, Is.False);
                    Assert.That(minion.GetComponent<SpriteRenderer>().enabled, Is.True,
                        "Top-down play restores the old icon.");
                    Assert.That(Vector3.Dot(minion.transform.up, camera.transform.up), Is.GreaterThan(0.999f),
                        "Flat creep artwork follows camera heading.");
                    Assert.That(Vector3.Dot(minion.transform.forward, Vector3.up), Is.GreaterThan(0.999f),
                        "Creep artwork remains above its extruded base.");
                }
                navigation.ResetView();
                yield return null; yield return null;
                yield return UiPlayer.CaptureScreen("tmp/w165-top-down-live-match.png");
                yield return UiPlayer.CaptureScreen("tmp/w187-minion-tokens-top-down.png");
                navigation.Navigate(Vector2.right, Vector2.zero, 0, 0.2f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Home));
                yield return null;
                Assert.That(camera.transform.position, Is.EqualTo(home));
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState());
                if (addedKeyboard) InputSystem.RemoveDevice(keyboard);
                if (addedMouse) InputSystem.RemoveDevice(mouse);
            }
        }
    }
}
