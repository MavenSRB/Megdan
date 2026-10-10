using System.Collections;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using UnityEngine;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// The PvE executable's launch smoke (<c>-guardsPveSmokeTest</c>): compose a fixed encounter
    /// through the launcher's own path, let the existing match scene build it, check what stands on
    /// the board and quit with an exit code. Every line it writes starts with <see cref="Marker"/>.
    /// </summary>
    public sealed class PveSmokeRunner : MonoBehaviour
    {
        public const string Marker = "GUARDS_PVE_SMOKE:";
        public const ulong Seed = 20261010UL;
        public static readonly string[] Heroes = { "hero-arien", "hero-brogan" };
        private const float TimeoutSeconds = 120f;

        private bool finished;

        public static void Run(PveLauncher launcher, string loadProblem)
        {
            Debug.Log(Marker + " BEGIN");
            var runner = new GameObject("PvE Smoke").AddComponent<PveSmokeRunner>();
            DontDestroyOnLoad(runner.gameObject);
            if (loadProblem != null)
            {
                runner.Finish(false, "content " + loadProblem.Replace('\n', ' '));
                return;
            }

            var problems = launcher.StartEncounter(Heroes, Seed, runner.OnComposed);
            if (problems.Count > 0) runner.Finish(false, "setup " + string.Join(" ", problems));
        }

        private void Awake()
        {
            UnityEngine.Application.logMessageReceived += OnLog;
        }

        private void OnDestroy()
        {
            UnityEngine.Application.logMessageReceived -= OnLog;
        }

        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
            if (!finished) Finish(false, "timeout before the encounter composed");
        }

        private void OnComposed(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.ReadModel;
            var composed = PveLauncher.LastComposed;
            var blue = model.Heroes.Count(hero => model.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue);
            var red = model.Heroes.Count(hero => model.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Red);
            var expectedRed = composed.Enemies.Count(enemy => enemy.Spawn.Round == 1);
            Debug.Log($"{Marker} COMPOSED heroes={blue} enemiesStanding={red} enemiesTotal={composed.Enemies.Count} " +
                $"creeps={model.Minions.Count} seed={composed.Setup.Seed}");
            if (blue != Heroes.Length || red != expectedRed || model.Minions.Count != composed.Encounter.Creeps.Count)
            {
                Finish(false, "the composed board does not match the encounter");
                return;
            }

            StartCoroutine(PassAfterFrames());
        }

        private IEnumerator PassAfterFrames()
        {
            for (var frame = 0; frame < 5; frame++) yield return null;
            Finish(true, null);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (finished || (type != LogType.Exception && type != LogType.Error)) return;
            if (condition != null && condition.StartsWith(Marker)) return;
            Finish(false, type + " " + condition);
        }

        private void Finish(bool passed, string reason)
        {
            if (finished) return;
            finished = true;
            Debug.Log(passed ? Marker + " PASS" : Marker + " FAIL " + reason);
            UnityEngine.Application.Quit(passed ? 0 : 1);
        }
    }
}
