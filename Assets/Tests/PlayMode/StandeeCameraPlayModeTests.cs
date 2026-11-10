using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Board;
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
        public IEnumerator CaptureRealStandeeAttackOutcomes()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            var red = new[] { "brogan", "rowenna", "tali" }.Select(id => roster.Single(h => h.ContentId.Value == id)).ToArray();
            var blue = new[] { "misa", "tigerclaw", "gydion" }.Select(id => roster.Single(h => h.ContentId.Value == id)).ToArray();
            var attackerId = new HeroId(red[0].HeroIdValue);
            var defenderId = new HeroId(blue[0].HeroIdValue);
            var attackAt = new HexCoordinate(-3, 1);
            var defendAt = new HexCoordinate(-2, 1);
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(red, blue);
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var initial = BroganUiJourneyPlayModeTests.Root().Runtime;
            var removed = initial.BoardView.Model.Minions.Select(m => m.MinionId).ToArray();
            foreach (var outcome in new[] { "defeated", "bumped" })
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(red, blue);
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                    heroes: new[] { new SandboxHeroStaging(attackerId, attackAt), new SandboxHeroStaging(defenderId, defendAt) },
                    removedMinions: removed);
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                UiPlayer.SetPlayerViewport(1280, 720, "W-189 real resolved attack: " + outcome);
                yield return null;
                var runtime = BroganUiJourneyPlayModeTests.Root().Runtime;
                var attackView = runtime.BoardView.HeroViews.Single(h => h.Occupancy.HeroId.Equals(attackerId));
                Assert.That(attackView.Standee != null && attackView.Standee.HasArtwork, Is.True,
                    "Configured attack artwork: " + attackView.HeroContentId);
                foreach (var driver in Object.FindObjectsByType<KineticCueDriver>(FindObjectsSortMode.None)) driver.enabled = false;
                foreach (var driver in Object.FindObjectsByType<AttackSpotlightDriver>(FindObjectsSortMode.None)) driver.enabled = false;
                foreach (var driver in Object.FindObjectsByType<BoardDefeatFadeDriver>(FindObjectsSortMode.None)) driver.enabled = false;
                BroganEngagedUiJourneyPlayModeTests.CommitSoTheNamedSeatActs(runtime, attackerId,
                    GuardsDigital.Core.Cards.Verified.BroganCardContent.Onslaught);
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(attackerId));
                for (var drain = 0; drain < 600 && (runtime.KineticView.Conductor.PendingSceneCount > 0 ||
                    runtime.KineticView.Conductor.ActiveScene != null); drain++)
                    runtime.KineticView.Tick(1f / 60f);
                Assert.That(runtime.SubmitAction(BroganEngagedUiJourneyPlayModeTests.PrimaryActionOf(runtime)), Is.True);
                Assert.That(runtime.SubmitProjectedCommand(GuardsDigital.Core.Replay.DirectorCommand.ChooseAttackTarget(defenderId)), Is.True);
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
                var defender = runtime.BoardView.HeroViews.Single(h => h.Occupancy.HeroId.Equals(defenderId));
                var startPosition = defender.transform.position;
                var restingScale = defender.transform.localScale;
                if (outcome == "bumped")
                {
                    var card = runtime.Cards.Single(c => c.OwnerId.Equals(defenderId) && c.DefinitionReference.DefinitionId.Equals(
                        GuardsDigital.Core.Cards.Verified.MisaCardContent.ChallengeAccepted));
                    Assert.That(runtime.SubmitDefenseCard(card.Id), Is.True);
                }
                else Assert.That(runtime.DeclineDefense(), Is.True);
                var resolved = runtime.ReadModel.MatchLog.Last(e => e.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.Attack);
                Assert.That(resolved.Outcome, Is.EqualTo(outcome == "defeated" ?
                    GuardsDigital.Core.Diagnostics.MatchLogOutcome.AttackSuccessful : GuardsDigital.Core.Diagnostics.MatchLogOutcome.AttackSuccessfullyDefended));
                Assert.That(runtime.Heroes.Any(h => h.HeroId.Equals(defenderId)), Is.EqualTo(outcome != "defeated"));
                var directory = Path.GetFullPath("tmp/w192-real-" + outcome + "-frames");
                Directory.CreateDirectory(directory);
                bool sawLean = false, sawBump = false, sawFall = false, sawTravel = false;
                for (var frame = 0; frame < 240; frame++)
                {
                    const float delta = 1f / 60f; // half-speed export, same production clocks and curves
                    runtime.SpotlightPresenter.Tick(delta);
                    runtime.KineticView.Tick(delta);
                    runtime.SpotlightView.ApplyFrame(delta);
                    runtime.BoardView.AdvanceDefeatFades(delta);
                    var actor = runtime.BoardView.HeroViews.Single(h => h.Occupancy.HeroId.Equals(attackerId));
                    actor.Standee.Refresh(Camera.main); defender.Standee.Refresh(Camera.main);
                    sawLean |= actor.Standee.IsStanding && Vector3.Dot(actor.Standee.PanelRenderer.transform.up, Vector3.up) < 0.995f;
                    sawBump |= Vector3.Distance(defender.transform.position, startPosition) > 0.01f;
                    var ghost = runtime.BoardView.HeroDefeatGhostFor(defender);
                    if (defender.gameObject.activeInHierarchy && defender.Standee.IsStanding)
                    {
                        var baseProperties = new MaterialPropertyBlock();
                        defender.GetComponent<MeshRenderer>().GetPropertyBlock(baseProperties);
                        Assert.That(baseProperties.isEmpty, Is.True, "Defense keeps the base's resting colour.");
                    }
                    sawFall |= ghost != null && ghost.StandeePanel != null && ghost.Progress01 > 0.2f;
                    if (outcome == "defeated")
                    {
                        var actorMotion = runtime.KineticView.Playback.MotionFor(KineticSubject.ForHero(attackerId));
                        if (runtime.KineticView.Playback.LungeTargetFor(KineticSubject.ForHero(attackerId)).IsDefined && ghost != null)
                        {
                            Assert.That(ghost.Progress01, Is.Zero, "The fall waits until the attack lunge has ended.");
                            if (ghost.StandeePanel != null)
                                Assert.That(Vector3.Dot(ghost.StandeePanel.up, Vector3.up), Is.GreaterThan(0.999f),
                                    "The defeated standee stays upright for the entire attack lunge.");
                        }
                        sawTravel |= actorMotion.HasRouteTravel;
                        if (actorMotion.HasRouteTravel)
                            Assert.That(ghost == null || !ghost.IsFading, Is.True,
                                "The card's resolved relocation is displayed after the fallen figure clears.");
                        if (!sawLean && actorMotion.LungeLean <= 0f)
                            Assert.That(Vector3.Distance(actor.transform.position,
                                HexWorldLayout.ToWorld(attackAt, actor.OuterRadius) + Vector3.up * actor.VerticalOffset),
                                Is.LessThan(0.001f), "The attacker waits at the strike origin before impact.");
                        if (ghost != null && ghost.Progress01 <= 0f && actor.Standee.IsStanding)
                            Assert.That(ghost.StandeePanel, Is.Not.Null,
                                "A held defender follows the camera into standee view before it falls.");
                        if (ghost != null && ghost.StandeePanel != null && ghost.Progress01 > 0.15f)
                        {
                            Assert.That(ghost.transform.localScale, Is.EqualTo(restingScale),
                                "The defeated figure must not shrink during or after its fall.");
                            if (ghost.Progress01 >= 0.70f)
                            {
                                var away = HexWorldLayout.ToWorld(defendAt, actor.OuterRadius) -
                                    HexWorldLayout.ToWorld(attackAt, actor.OuterRadius);
                                Assert.That(Vector3.Dot(ghost.StandeePanel.up, away.normalized), Is.GreaterThan(0.999f),
                                    "The figure lies flat away from the attacker's recorded strike space.");
                                Assert.That(Mathf.Abs(Vector3.Dot(ghost.StandeePanel.forward, Vector3.up)), Is.GreaterThan(0.999f));
                            }
                            Assert.That(Vector3.Distance(actor.transform.position, startPosition), Is.GreaterThan(0.7f),
                                "After-attack movement waits until the falling defender has cleared.");
                        }
                    }
                    yield return null;
                    CaptureRuntimeCameraFrame(Camera.main, directory, frame);
                }
                Assert.That(sawLean, Is.True, "The resolved attack must visibly lean in its production spotlight.");
                if (outcome == "bumped") Assert.That(sawBump, Is.True);
                if (outcome == "planted") Assert.That(sawBump, Is.False);
                if (outcome == "defeated")
                {
                    Assert.That(sawFall, Is.True);
                    Assert.That(sawTravel, Is.True);
                    var actor = runtime.BoardView.HeroViews.Single(h => h.Occupancy.HeroId.Equals(attackerId));
                    Assert.That(Vector3.Distance(actor.transform.position,
                        HexWorldLayout.ToWorld(actor.Coordinate, actor.OuterRadius) + Vector3.up * actor.VerticalOffset),
                        Is.LessThan(0.001f), "Playback finishes exactly at the already-resolved placement.");
                }
                Assert.That(Directory.GetFiles(directory, "frame-*.jpg"), Has.Length.EqualTo(240));
            }
        }

        [UnityTest]
        public IEnumerator CapturePhoneFriendlyMinionStandeeOrbitFrames()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { "brogan", "misa", "rowenna" }.Select(id => roster.Single(h => h.ContentId.Value == id)),
                new[] { "tigerclaw", "gydion", "tali" }.Select(id => roster.Single(h => h.ContentId.Value == id)));
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(1280, 720, "W-188 minion standee orbit video");
            yield return new WaitForSecondsRealtime(1f);

            var root = BroganUiJourneyPlayModeTests.Root();
            var camera = Camera.main;
            var minions = root.MapView.CurrentMinionViews
                .Where(value => value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(minions, Is.Not.Empty);
            var boardCentre = minions.Aggregate(Vector3.zero, (sum, value) => sum + value.transform.position) /
                minions.Length;
            var shots = new List<OrbitShot>
            {
                OrbitShot.Angled(boardCentre, 0f, 20f, 10f, 12f),
            };
            foreach (var team in new[] { TeamSide.Red, TeamSide.Blue })
            foreach (var type in new[] { StandardMinionType.Heavy, StandardMinionType.Melee, StandardMinionType.Ranged })
            {
                var view = minions.First(value =>
                    value.Occupancy.TeamSide == team && value.Occupancy.MinionType == type);
                Assert.That(view.Standee.HasArtwork, Is.True, team + " " + type);
                shots.Add(OrbitShot.Angled(
                    view.transform.position,
                    shots.Count * 42f,
                    6.2f,
                    3.2f,
                    2.6f));
            }
            shots.Add(OrbitShot.Angled(boardCentre, 300f, 17f, 8f, 8.5f));
            shots.Add(OrbitShot.TopDown(boardCentre, 30f, 15f));

            var frameDirectory = Path.GetFullPath("tmp/w188-minion-standee-orbit-frames");
            Directory.CreateDirectory(frameDirectory);
            foreach (var oldFrame in Directory.GetFiles(frameDirectory, "frame-*.jpg"))
            {
                File.Delete(oldFrame);
            }

            const int transitionFrames = 12;
            const int holdFrames = 10;
            var frame = 0;
            Apply(camera, shots[0]);
            yield return null;
            yield return CaptureFrame(frameDirectory, frame++);
            for (var shotIndex = 1; shotIndex < shots.Count; shotIndex++)
            {
                var from = shots[shotIndex - 1];
                var to = shots[shotIndex];
                for (var step = 1; step <= transitionFrames; step++)
                {
                    var amount = Mathf.SmoothStep(0f, 1f, step / (float)transitionFrames);
                    Apply(camera, OrbitShot.Lerp(from, to, amount));
                    yield return CaptureFrame(frameDirectory, frame++);
                }
                for (var hold = 0; hold < holdFrames; hold++)
                {
                    Apply(camera, to);
                    yield return CaptureFrame(frameDirectory, frame++);
                }
            }

            Assert.That(frame, Is.EqualTo(177));
            Assert.That(minions.All(value => !value.Standee.IsStanding), Is.True,
                "The final top-down shot shows the old token icons.");
            Assert.That(Directory.GetFiles(frameDirectory, "frame-*.jpg"), Has.Length.EqualTo(frame));
        }

        private static void CaptureRuntimeCameraFrame(Camera camera, string directory, int frame)
        {
            var target = new RenderTexture(1280, 720, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                capture.Apply();
                File.WriteAllBytes(Path.Combine(directory, $"frame-{frame:0000}.jpg"), capture.EncodeToJPG(88));
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(capture);
            }
        }

        private static IEnumerator CaptureFrame(string directory, int frame)
        {
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                File.WriteAllBytes(
                    Path.Combine(directory, $"frame-{frame:0000}.jpg"),
                    capture.EncodeToJPG(84));
            }
            finally
            {
                Object.Destroy(capture);
            }
        }

        private static void Apply(Camera camera, OrbitShot shot)
        {
            camera.transform.SetPositionAndRotation(shot.Position, shot.Rotation);
            camera.orthographicSize = shot.OrthographicSize;
        }

        private readonly struct OrbitShot
        {
            public OrbitShot(Vector3 position, Quaternion rotation, float orthographicSize)
            {
                Position = position;
                Rotation = rotation;
                OrthographicSize = orthographicSize;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public float OrthographicSize { get; }

            public static OrbitShot Angled(
                Vector3 focus,
                float yaw,
                float distance,
                float height,
                float orthographicSize)
            {
                var offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, height, -distance);
                var position = focus + offset;
                var rotation = Quaternion.LookRotation((focus + Vector3.up * 0.65f) - position, Vector3.up);
                return new OrbitShot(position, rotation, orthographicSize);
            }

            public static OrbitShot TopDown(Vector3 focus, float height, float orthographicSize) =>
                new OrbitShot(focus + Vector3.up * height, Quaternion.Euler(90f, 0f, 0f), orthographicSize);

            public static OrbitShot Lerp(OrbitShot from, OrbitShot to, float amount) =>
                new OrbitShot(
                    Vector3.Lerp(from.Position, to.Position, amount),
                    Quaternion.Slerp(from.Rotation, to.Rotation, amount),
                    Mathf.Lerp(from.OrthographicSize, to.OrthographicSize, amount));
        }

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
                yield return UiPlayer.CaptureScreen("tmp/w196-minion-standees-angled.png");
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
                yield return UiPlayer.CaptureScreen("tmp/w196-minion-standee-detail.png");
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
                        "Top-down play restores the matching token.");
                    Assert.That(Vector3.Dot(minion.transform.up, camera.transform.up), Is.GreaterThan(0.999f),
                        "Flat creep artwork follows camera heading.");
                    Assert.That(Vector3.Dot(minion.transform.forward, Vector3.up), Is.GreaterThan(0.999f),
                        "Creep artwork remains above its extruded base.");
                }
                navigation.ResetView();
                yield return null; yield return null;
                yield return UiPlayer.CaptureScreen("tmp/w165-top-down-live-match.png");
                yield return UiPlayer.CaptureScreen("tmp/w187-minion-tokens-top-down.png");
                yield return UiPlayer.CaptureScreen("tmp/w196-minion-tokens-top-down.png");
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
