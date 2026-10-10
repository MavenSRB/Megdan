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

        private static readonly string[] FiveHeroes =
            { "hero-arien", "hero-brogan", "hero-wasp", "hero-tigerclaw", "hero-xargatha" };

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

            ConfiguredMatchSceneRuntime composed = null;
            var problems = launcher.StartEncounter(FiveHeroes, 42, runtime => composed = runtime);
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

            Assert.That(session.Projection.Heroes.Count(hero => Side(session.Projection, hero.HeroId) == TeamSide.Blue), Is.EqualTo(5));

            // The paced driver then commits every enemy's planned card.
            deadline = Time.realtimeSinceStartup + 30f;
            while (session.Peek().Kind == PveStepKind.Command && Time.realtimeSinceStartup < deadline) yield return null;
            foreach (var enemy in encounter.Enemies)
            {
                Assert.That(session.Projection.Commitments.Single(value => value.HeroId.Equals(enemy.HeroId)).Status,
                    Is.Not.EqualTo(GuardsDigital.Core.Cards.TurnCardCommitmentStatus.AwaitingResponse), enemy.DisplayName);
            }

            yield return null;
            var overlay = PveLauncher.LastView.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(overlay.Q<Label>("pve-status").text, Does.Contain("Round 1"));
            Assert.That(overlay.Q<Label>("pve-plan-" + encounter.Enemies[0].HeroId.Value)?.resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex), "A standing enemy shows its plan beside its token.");

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
            var initiative = root.Q<IntegerField>("pve-edit-raider-A-init");
            Assert.That(initiative, Is.Not.Null, "The editor lists every card's numbers.");
            Assert.That(root.Q<IntegerField>("pve-edit-raider-B-attack"), Is.Not.Null);
            Assert.That(root.Q<IntegerField>("pve-edit-brute-P-defense"), Is.Not.Null);
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
