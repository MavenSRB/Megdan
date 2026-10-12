using System.Collections;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Pve.Play;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.Pve.PlayMode
{
    /// <summary>
    /// W-021. The PvE launcher shows its setup screen, composes the static encounter and hands it to
    /// the existing match scene, which draws the chosen heroes, the round-1 enemies and the creeps
    /// and leaves round-2 enemies undrawn.
    /// </summary>
    public sealed class PveLauncherPlayModeTests
    {
        public const string LauncherScenePath = "Assets/GuardsPve/Scenes/PveLauncher.unity";

        private static readonly string[] ThreeHeroes =
            { "hero-arien", "hero-brogan", "hero-wasp" };

        [UnityTest]
        public IEnumerator TheLauncherComposesTheStaticEncounterIntoTheExistingMatchScene()
        {
            yield return LoadLauncher();
            var launcher = Object.FindAnyObjectByType<PveLauncher>();
            Assert.That(launcher, Is.Not.Null);
            var document = launcher.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null, "The setup screen is drawn.");
            Assert.That(document.rootVisualElement.Q<Button>("pve-start"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("pve-hero-hero-arien"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("pve-start").enabledSelf, Is.False);
            Assert.That(launcher.StartEncounter(ThreeHeroes.Take(2), 42), Has.Some.Contains("Choose exactly 3"));
            Assert.That(launcher.StartEncounter(ThreeHeroes.Concat(new[] { "hero-tigerclaw" }), 42), Has.Some.Contains("Choose exactly 3"));
            var toggle = typeof(PveLauncher).GetMethod("Toggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var hero in ThreeHeroes) toggle.Invoke(launcher, new object[] { hero });
            Assert.That(document.rootVisualElement.Q<Button>("pve-start").enabledSelf, Is.True);
            toggle.Invoke(launcher, new object[] { "hero-tigerclaw" });
            var chosen = (System.Collections.Generic.List<string>)typeof(PveLauncher)
                .GetField("chosen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(launcher);
            Assert.That(chosen, Is.EqualTo(ThreeHeroes));
            toggle.Invoke(launcher, new object[] { ThreeHeroes[0] });
            Assert.That(document.rootVisualElement.Q<Button>("pve-start").enabledSelf, Is.False);
            toggle.Invoke(launcher, new object[] { ThreeHeroes[0] });

            yield return null;
            yield return new WaitForEndOfFrame();
            var setupCapture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w030-three-hero-setup.png", setupCapture.EncodeToPNG());
            Object.Destroy(setupCapture);

            ConfiguredMatchSceneRuntime composed = null;
            var problems = launcher.StartEncounter(ThreeHeroes, 42, runtime => composed = runtime);
            Assert.That(problems, Is.Empty);
            var deadline = Time.realtimeSinceStartup + 60f;
            while (composed == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(composed, Is.Not.Null, "The match scene composed the encounter.");
            yield return null;
            yield return null;

            var model = composed.ReadModel;
            var encounter = PveLauncher.LastComposed;
            var session = composed.Session as PveEncounterSession;
            Assert.That(session, Is.Not.Null, "The HUD talks to the encounter session.");
            Assert.That(PveLauncher.LastView, Is.Not.Null, "The PvE overlay is attached.");
            // W-026. The HUD plays Blue: it follows, offers and outlines from Blue's side only.
            Assert.That(composed.DirectorPlayerSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(composed.BoardView.PerspectiveSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(model.Heroes.Count(hero => Side(model, hero.HeroId) == TeamSide.Blue), Is.EqualTo(0),
                "Heroes wait for their round-1 placement.");
            Assert.That(model.Heroes.Count(hero => Side(model, hero.HeroId) == TeamSide.Red), Is.EqualTo(7));
            Assert.That(model.Minions.Count, Is.EqualTo(3));

            // The overlay's driver begins the placement on its own; the player answers it.
            deadline = Time.realtimeSinceStartup + 20f;
            while (session.Projection.PendingSystemTransitionStep == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null, "Round-1 placement began.");
            while (session.Projection.PendingSystemTransitionStep != null)
            {
                var pending = session.Projection.PendingSystemTransitionStep;
                var item = pending.OpenItems.First(value => value.ChoosingSide == TeamSide.Blue);
                var placed = session.Execute(GuardsDigital.Core.Replay.DirectorCommand.AnswerSystemTransitionStep(
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand(
                        pending.InstanceId, pending.StepId, item.ItemId, TeamSide.Blue, item.Options[0])));
                Assert.That(placed.Accepted, Is.True, placed.Detail);
                composed.RefreshAll();
                yield return null;
            }

            Assert.That(session.Projection.Heroes.Count(hero => Side(session.Projection, hero.HeroId) == TeamSide.Blue), Is.EqualTo(3));

            // The paced driver then commits every enemy's planned card, and the tray never shows an
            // enemy's hand as the player's while it does (W-026).
            deadline = Time.realtimeSinceStartup + 30f;
            while (session.Peek().Kind == PveStepKind.Command && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(session.Controller.IsEnemy(composed.ViewedHeroId), Is.False,
                    "The tray shows " + composed.ViewedHeroId.Value + " while the enemies commit.");
                yield return null;
            }

            Assert.That(encounter.Heroes.Contains(composed.ViewedHeroId), Is.True, "The tray shows one of the player's heroes.");
            Assert.That(composed.BoardView.HeroViews.Where(view => view != null && encounter.Heroes.Contains(view.Occupancy.HeroId))
                .All(view => view.gameObject.activeInHierarchy), Is.True, "Every placed hero is drawn on the board.");
            foreach (var enemy in encounter.Enemies)
            {
                Assert.That(session.Projection.Commitments.Single(value => value.HeroId.Equals(enemy.HeroId)).Status,
                    Is.Not.EqualTo(GuardsDigital.Core.Cards.TurnCardCommitmentStatus.AwaitingResponse), enemy.DisplayName);
            }

            yield return null;
            var overlay = PveLauncher.LastView.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(overlay.Q<Label>("pve-status").text, Does.Contain("Round 1"));
            Assert.That(overlay.Q<Label>("pve-prompt-banner"), Is.Not.Null, "An enemy decision has its own banner.");
            Assert.That(overlay.Query<Label>().ToList().Any(label => label.name != null &&
                label.name.StartsWith("pve-plan-pve-enemy-")), Is.False, "No floating text above map tokens.");

            var rail = overlay.Q<ScrollView>("pve-enemy-rail");
            Assert.That(rail, Is.Not.Null);
            foreach (var enemy in encounter.Enemies)
            {
                var row = rail.Q("pve-enemy-" + enemy.HeroId.Value);
                if (enemy.Spawn.Round > 1)
                {
                    Assert.That(row, Is.Null, "Future-wave enemies have no tray before spawning.");
                    continue;
                }
                Assert.That(row, Is.Not.Null);
                var sprite = GuardsDigital.Presentation.Units.Minions.StandardMinionArtCatalog.Load(
                    TeamSide.Red, enemy.Type.TokenType.Value);
                Assert.That(row.Q("gd-hero-icon").style.backgroundImage.value.sprite, Is.SameAs(sprite));
                var slots = row.Query<VisualElement>("gd-turn-slot").ToList();
                Assert.That(slots.Count, Is.EqualTo(4), "Every enemy uses the hero rail's four card slots.");
                var plan = PvePlans.PlanFor(encounter, enemy, System.Math.Max(1, enemy.Spawn.Round));
                for (var turn = 0; turn < 4; turn++)
                {
                    Assert.That(slots[turn].tooltip, Does.Contain(enemy.Type.FindOrange(plan[turn]).Name),
                        "All four planned cards are public before Blue commits.");
                    Assert.That(slots[turn].Q<Label>("pve-plan-initiative").text,
                        Is.EqualTo(enemy.Type.FindOrange(plan[turn]).Initiative.ToString()));
                }
                Assert.That(row.resolvedStyle.height, Is.LessThanOrEqualTo(64.1f));
                Assert.That(row.Q("gd-hero-name"), Is.Null);
                Assert.That(row.Q("gd-hero-level"), Is.Null);
                Assert.That(row.Q("gd-discard"), Is.Null);
                Assert.That(row.Q("gd-bonus-strip"), Is.Null);
                Assert.That(row.Query<VisualElement>("pve-defense-card").ToList().Count,
                    Is.EqualTo(enemy.Type.PurpleCards.Count));
                Assert.That(row.tooltip,
                    Does.Contain(enemy.Spawn.Round == 1 ? "On board" : "Spawn round 2"));
                if (enemy.Spawn.Round == 1)
                {
                    var view = composed.BoardView.HeroViews.First(value => value.Occupancy.HeroId.Equals(enemy.HeroId));
                    Assert.That(view.HasTokenVisual, Is.True);
                    Assert.That(view.GetComponent<MeshRenderer>().sharedMaterials[1].mainTexture, Is.SameAs(sprite.texture));
                }
            }
            var firstRow = rail.Q("pve-enemy-" + encounter.Enemies[0].HeroId.Value);
            yield return AssertLinkedHover(composed, overlay, encounter.Enemies[0].HeroId);
            var firstSlot = firstRow.Q("gd-turn-slot");
            using (var hover = PointerEnterEvent.GetPooled()) { hover.target = firstSlot; firstSlot.SendEvent(hover); }
            yield return null;
            Assert.That(overlay.Q("gd-turn-slot-preview").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return new WaitForEndOfFrame();
            var hoverCapture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w032-enemy-card-hover.png", hoverCapture.EncodeToPNG());
            Object.Destroy(hoverCapture);
            using (var leave = PointerLeaveEvent.GetPooled()) { leave.target = firstSlot; firstSlot.SendEvent(leave); }
            yield return null;
            Assert.That(overlay.Q("gd-turn-slot-preview").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            Assert.That(session.Undo().Accepted, Is.True);
            composed.RefreshAll();
            yield return null;
            var restarted = session.Restart();
            Assert.That(restarted.Accepted, Is.True, restarted.Detail);
            composed.RefreshAll();
            yield return null; yield return null;
            Assert.That(rail.Q("pve-enemy-" + encounter.Enemies[0].HeroId.Value).tooltip, Does.Contain("On board"));
            Assert.That(rail.resolvedStyle.height, Is.EqualTo(24 + 7 * 69).Within(1),
                "The background ends after the live trays, not at the bottom of the screen.");
            // Stage a snapshot with one enemy removed and a Heavy entering the board. This is a
            // projection/lifecycle test, not a second simulation of the separately tested combat rules.
            var heavy = encounter.Enemies.First(enemy => enemy.Type.PurpleCards.Count == 2);
            StageEnemyBoard(composed, encounter.Enemies[0].HeroId, heavy);
            composed.RefreshAll();
            yield return null; yield return null;
            Assert.That(rail.Q("pve-enemy-" + encounter.Enemies[0].HeroId.Value), Is.Null,
                "An enemy removed from the board loses its tray.");
            Assert.That(rail.Q("pve-enemy-" + heavy.HeroId.Value), Is.Not.Null,
                "A newly spawned enemy gains its tray.");
            var defenseRow = rail.Query<VisualElement>().ToList().First(row => row.Q("pve-defense-card") != null &&
                row.name != null && row.name.StartsWith("pve-enemy-pve-enemy-"));
            Assert.That(defenseRow.Query<VisualElement>("pve-defense-card").ToList()
                .Count(card => card.style.opacity.value < .5f), Is.EqualTo(1), "A spent Defense is dimmed.");
            rail.ScrollTo(defenseRow);
            yield return null;
            var defense = defenseRow.Q("pve-defense-card");
            using (var hover = PointerEnterEvent.GetPooled()) { hover.target = defense; defense.SendEvent(hover); }
            yield return null;
            Assert.That(overlay.Q("gd-turn-slot-preview").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "Heavy defenses use the same complete-card hover preview.");
            using (var leave = PointerLeaveEvent.GetPooled()) { leave.target = defense; defense.SendEvent(leave); }
            rail.scrollOffset = Vector2.zero;
            yield return null;

            // Capture the changed rail and official enemy tokens in the real composed scene.
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.Directory.CreateDirectory("tmp");
            System.IO.File.WriteAllBytes("tmp/w032-enemy-rail.png", capture.EncodeToPNG());
            Object.Destroy(capture);

            // Removal shrinks the box; zero live enemies hide it; restoring occupancy grows it.
            var populatedSnapshot = composed.CaptureSnapshot();
            StageEnemyBoard(composed, heavy.HeroId, null);
            composed.RefreshAll();
            yield return null; yield return null;
            Assert.That(rail.resolvedStyle.height, Is.EqualTo(24 + 6 * 69).Within(1));
            foreach (var enemy in encounter.Enemies)
                StageEnemyBoard(composed, enemy.HeroId, null);
            composed.RefreshAll();
            yield return null; yield return null;
            Assert.That(rail.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            RestoreTestSnapshot(composed, populatedSnapshot);
            composed.RefreshAll();
            yield return null; yield return null;
            Assert.That(rail.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(rail.resolvedStyle.height, Is.EqualTo(507).Within(1));

            // Viewport resize caps the box and keeps every tray reachable by scrolling.
            overlay.style.bottom = StyleKeyword.Auto;
            overlay.style.height = 700;
            yield return null; yield return null; yield return null;
            Assert.That(rail.resolvedStyle.height, Is.EqualTo(260).Within(1));
            var lastRow = rail.Q("gd-hero-rail-red").Children().Last();
            rail.ScrollTo(lastRow);
            yield return null; yield return null;
            Assert.That(rail.scrollOffset.y, Is.GreaterThan(0));
            Assert.That(lastRow.worldBound.yMax, Is.LessThanOrEqualTo(rail.worldBound.yMax + 1));
            overlay.style.height = StyleKeyword.Null;
            overlay.style.bottom = 0;
            yield return null; yield return null; yield return null;
            Assert.That(rail.resolvedStyle.height, Is.EqualTo(507).Within(1));

            foreach (var hero in session.Projection.Participants.Heroes)
            {
                Assert.DoesNotThrow(() => PlayerAccent.For(session.Projection.Participants, hero.Id), hero.Id.Value);
            }
        }

        private static void StageEnemyBoard(ConfiguredMatchSceneRuntime runtime, HeroId removed, PveEnemyRecord spawned)
        {
            var payload = runtime.CaptureSnapshot();
            var authoritative = RuleEventLogSnapshotEnvelope.TryDeserialize(payload, out var envelope)
                ? envelope.AuthoritativePayload : payload;
            var restored = AuthoritativeSnapshotSerializer.Deserialize(runtime.ContentCatalogs, authoritative,
                runtime.InitialState.Scenario.CardDefinitions, runtime.InitialState.AvailableMaps,
                runtime.InitialState.Setup.Minions, modeRules: runtime.InitialState.ModeRules);
            var state = restored.Board.CaptureRestoreState();
            var positions = state.HeroOccupancies.Where(hero => !hero.HeroId.Equals(removed));
            if (spawned != null) positions = positions.Concat(new[] { new HeroOccupancy(spawned.HeroId, spawned.Spawn.Coordinate) });
            var staged = new BoardRestoreState(positions, state.ObstacleOccupancies, state.RemainingMinions,
                state.PlacedTokens, state.CurrentBattleZoneId, state.MinionBattleExclusions,
                state.LastCompletedMovementWalk, state.HeroBodySupplies, state.ActingBodies,
                state.CurrentTurnStart, state.PreviousTurnStart, state.UnitMutations);
            var board = AdjacentMoveAuthority.Restore(staged, restored.Board.MapDefinition, runtime.InitialState.Setup.Minions);
            if (spawned != null)
            {
                var defenseId = PveCardFactory.PurpleId(spawned.Type, spawned.Type.PurpleCards[0]);
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(restored.Cards.First(card => card.OwnerId.Equals(spawned.HeroId) &&
                    card.DefinitionReference.DefinitionId.Equals(defenseId))), Is.True);
            }
            RestoreTestSnapshot(runtime, AuthoritativeSnapshotSerializer.Serialize(restored.Round, board));
        }

        private static void RestoreTestSnapshot(ConfiguredMatchSceneRuntime runtime, string payload)
        {
            // PvE intentionally exposes no player restore control. Stage through the existing
            // underlying test authority, retaining the configured overlay and its live read port.
            var inner = (GuardsDigital.Application.IMatchSession)typeof(PveEncounterSession)
                .GetField("inner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(runtime.Session);
            var result = inner.RestoreSnapshot(payload);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        internal static IEnumerator AssertLinkedHover(ConfiguredMatchSceneRuntime runtime, VisualElement overlay, HeroId enemy)
        {
            var row = overlay.Q("pve-enemy-" + enemy.Value);
            var rail = overlay.Q<ScrollView>("pve-enemy-rail");
            rail.ScrollTo(row);
            yield return null;
            var picked = row.panel.Pick(row.worldBound.center);
            Assert.That(picked == row || row.Contains(picked), Is.True, "Compact row is pointer-reachable.");
            var occupancy = runtime.ReadModel.Heroes.First(hero => hero.HeroId.Equals(enemy));
            var tile = runtime.BoardView.TileViews.First(value => value.Coordinate.Equals(occupancy.Coordinate));
            using (var enter = PointerEnterEvent.GetPooled()) { enter.target = row; row.SendEvent(enter); }
            yield return null;
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Ally), "Rail hover lights the enemy's map space.");
            Assert.That(row.resolvedStyle.borderTopColor, Is.EqualTo(HexCornerHighlightTemplate.AllyColor));
            using (var leave = PointerLeaveEvent.GetPooled()) { leave.target = row; row.SendEvent(leave); }
            yield return null;
            Assert.That(tile.CornerHighlight.Kind, Is.Not.EqualTo(HexCornerHighlightKind.Ally));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var token = runtime.BoardView.HeroViews.First(view => view.Occupancy.HeroId.Equals(enemy));
            Physics.SyncTransforms();
            var screen = Camera.main.WorldToScreenPoint(token.transform.position);
            Assert.That(pointer.RefreshHoverFromPointerRay(Camera.main.ScreenPointToRay(screen)), Is.True);
            // The live pointer runs next frame; inspect this actual camera-ray result synchronously.
            Assert.That(row.style.borderTopColor.value, Is.EqualTo(HexCornerHighlightTemplate.AllyColor), "Map-token hover lights the same rail row.");
            runtime.BoardView.SetPointerHoveredHero(default);
            yield return null;
            Assert.That(row.resolvedStyle.borderTopColor, Is.Not.EqualTo(HexCornerHighlightTemplate.AllyColor));
        }

        [UnityTest]
        public IEnumerator TheCardEditorChangesEnemyNumbersBeforeSaving()
        {
            yield return LoadLauncher();
            var launcher = Object.FindAnyObjectByType<PveLauncher>();
            var root = launcher.GetComponent<UIDocument>().rootVisualElement;
            using (var click = NavigationSubmitEvent.GetPooled())
            {
                click.target = root.Q<Button>("pve-edit-cards");
                root.Q<Button>("pve-edit-cards").SendEvent(click);
            }

            yield return null;
            var initiative = root.Q<IntegerField>("pve-edit-melee-A-init");
            Assert.That(initiative, Is.Not.Null, "The editor lists every card's numbers.");
            Assert.That(root.Q<IntegerField>("pve-edit-melee-B-attack"), Is.Not.Null);
            Assert.That(root.Q<IntegerField>("pve-edit-heavy-P-defense"), Is.Not.Null);
            Assert.That(root.Q<IntegerField>("pve-edit-ranged-B-range"), Is.Not.Null);
            Assert.That(root.Q<Button>("pve-card-editor-save"), Is.Not.Null);
        }

        private static TeamSide Side(GuardsDigital.Application.IMatchReadModel model, GuardsDigital.Core.Units.Heroes.HeroId hero) =>
            model.Participants.GetTeamForHero(hero).Side;

        private static IEnumerator LoadLauncher()
        {
#if UNITY_EDITOR
            var operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                LauncherScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
#endif
            yield return null;
            yield return null;
        }
    }
}
