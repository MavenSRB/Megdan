using System.Collections;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
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
            Assert.That(model.Heroes.Count(hero => Side(model, hero.HeroId) == TeamSide.Blue), Is.EqualTo(5));
            Assert.That(model.Heroes.Count(hero => Side(model, hero.HeroId) == TeamSide.Red), Is.EqualTo(7));
            Assert.That(model.Minions.Count, Is.EqualTo(3));

            var views = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in encounter.Enemies)
            {
                var view = views.SingleOrDefault(value => value.Occupancy.HeroId.Equals(enemy.HeroId));
                Assert.That(view, Is.Not.Null, enemy.DisplayName);
                Assert.That(view.gameObject.activeSelf, Is.EqualTo(enemy.Spawn.Round == 1),
                    enemy.DisplayName + (enemy.Spawn.Round == 1 ? " stands." : " is not drawn before it spawns."));
            }

            foreach (var hero in model.Participants.Heroes)
            {
                Assert.DoesNotThrow(() => PlayerAccent.For(model.Participants, hero.Id), hero.Id.Value);
            }
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
