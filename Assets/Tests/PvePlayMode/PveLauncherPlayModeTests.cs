using System.Collections;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Pve.Play;
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
            Assert.That(overlay.Q<Label>("pve-plan-" + encounter.Enemies[0].HeroId.Value)?.resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex), "A standing enemy shows its plan beside its token.");

            var rail = overlay.Q<ScrollView>("pve-enemy-rail");
            Assert.That(rail, Is.Not.Null);
            foreach (var enemy in encounter.Enemies)
            {
                var row = rail.Q("pve-enemy-" + enemy.HeroId.Value);
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
                Assert.That(row.Q<Label>("pve-enemy-reading").text,
                    Does.Contain(enemy.Spawn.Round == 1 ? "On board" : "Spawn round 2"));
                if (enemy.Spawn.Round == 1)
                {
                    var view = composed.BoardView.HeroViews.First(value => value.Occupancy.HeroId.Equals(enemy.HeroId));
                    Assert.That(view.HasTokenVisual, Is.True);
                    Assert.That(view.GetComponent<MeshRenderer>().sharedMaterials[1].mainTexture, Is.SameAs(sprite.texture));
                }
            }
            var firstRow = rail.Q("pve-enemy-" + encounter.Enemies[0].HeroId.Value);
            var firstSlot = firstRow.Q("gd-turn-slot");
            using (var hover = PointerEnterEvent.GetPooled()) { hover.target = firstSlot; firstSlot.SendEvent(hover); }
            yield return null;
            Assert.That(overlay.Q("gd-turn-slot-preview").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return new WaitForEndOfFrame();
            var hoverCapture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes("tmp/w030-enemy-card-hover.png", hoverCapture.EncodeToPNG());
            Object.Destroy(hoverCapture);
            using (var leave = PointerLeaveEvent.GetPooled()) { leave.target = firstSlot; firstSlot.SendEvent(leave); }
            yield return null;
            Assert.That(overlay.Q("gd-turn-slot-preview").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            // Capture the changed rail and official enemy tokens in the real composed scene.
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.Directory.CreateDirectory("tmp");
            System.IO.File.WriteAllBytes("tmp/w030-enemy-rail.png", capture.EncodeToPNG());
            Object.Destroy(capture);

            Assert.That(session.Undo().Accepted, Is.True);
            composed.RefreshAll();
            yield return null;
            Assert.That(session.Restart().Accepted, Is.True);
            composed.RefreshAll();
            yield return null;
            yield return null;
            Assert.That(rail.Q("pve-enemy-" + encounter.Enemies[0].HeroId.Value)
                .Q<Label>("pve-enemy-reading").text, Does.Contain("On board"));

            foreach (var hero in session.Projection.Participants.Heroes)
            {
                Assert.DoesNotThrow(() => PlayerAccent.For(session.Projection.Participants, hero.Id), hero.Id.Value);
            }
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
