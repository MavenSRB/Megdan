using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Play;
using UnityEngine;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// The PvE executable's launch smoke (<c>-guardsPveSmokeTest</c>): compose a fixed encounter
    /// through the launcher's own path, let the existing match scene build it with the encounter
    /// session and overlay, then play it with the passive player while the overlay paces the enemies,
    /// until round 2's enemies have entered or the encounter is lost. Every line it writes starts with
    /// <see cref="Marker"/>; it quits with its exit code.
    /// </summary>
    public sealed class PveSmokeRunner : MonoBehaviour
    {
        public const string Marker = "GUARDS_PVE_SMOKE:";
        public const ulong Seed = 20261010UL;
        public static readonly string[] Heroes = { "hero-arien", "hero-brogan", "hero-wasp" };
        private const float TimeoutSeconds = 300f;
        private const float SmokeStepSeconds = 0.02f;

        private bool finished;
        private bool enemyActed;
        private ConfiguredMatchSceneRuntime runtime;
        private PveEncounterSession session;

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
            if (!finished)
            {
                var match = session?.Projection;
                Finish(false, "timeout" + (match == null ? " before the encounter composed" :
                    $" at round {match.RoundIndex} turn {match.TurnIndex} phase {match.Phase}"));
            }
        }

        private void OnComposed(ConfiguredMatchSceneRuntime composedRuntime)
        {
            runtime = composedRuntime;
            session = composedRuntime.Session as PveEncounterSession;
            var composed = PveLauncher.LastComposed;
            var model = composedRuntime.ReadModel;
            var blue = model.Heroes.Count(hero => model.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue);
            var red = model.Heroes.Count(hero => model.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Red);
            var expectedRed = composed.Enemies.Count(enemy => enemy.Spawn.Round == 1);
            Debug.Log($"{Marker} COMPOSED heroesStanding={blue} enemiesStanding={red} enemiesTotal={composed.Enemies.Count} " +
                $"creeps={model.Minions.Count} seed={composed.Setup.Seed} session={(session != null)} overlay={(PveLauncher.LastView != null)}");
            if (session == null || PveLauncher.LastView == null || blue != 0 || red != expectedRed ||
                model.Minions.Count != composed.Encounter.Creeps.Count)
            {
                Finish(false, "the composed encounter does not match its setup");
                return;
            }

            PveLauncher.LastView.StepSeconds = SmokeStepSeconds;
            PveLauncher.LastView.WaitsForPresentation = false;
        }

        private void Update()
        {
            if (finished || session == null) return;
            var match = session.Projection;
            if (!enemyActed && session.Encounter.Enemies.Any(enemy =>
                    match.Commitments.Any(value => value.HeroId.Equals(enemy.HeroId) &&
                        value.Status != TurnCardCommitmentStatus.AwaitingResponse)))
            {
                enemyActed = true;
                Debug.Log(Marker + " ENEMY_ACTED");
            }

            if (session.Status == PveEncounterStatus.Lost)
            {
                Debug.Log($"{Marker} LOST round={match.RoundIndex} turn={match.TurnIndex}");
                Finish(enemyActed, enemyActed ? null : "lost before any enemy acted");
                return;
            }

            if (match.RoundIndex >= 2 && match.PendingSystemTransitionStep == null &&
                session.Encounter.Enemies.Where(enemy => enemy.Spawn.Round == 2)
                    .All(enemy => match.Heroes.Any(value => value.HeroId.Equals(enemy.HeroId))))
            {
                Debug.Log(Marker + " ROUND_2_SPAWNED");
                Finish(enemyActed, enemyActed ? null : "round 2 began before any enemy acted");
                return;
            }

            var next = session.Peek();
            if (next.Kind != PveStepKind.AwaitingPlayer && next.Kind != PveStepKind.Prompt) return;
            var answer = PvePassivePlayer.NextAnswer(session);
            if (answer == null) return;
            var result = session.Execute(answer);
            if (!result.Accepted)
            {
                Finish(false, "the passive player's " + answer.Kind + " was refused: " + result.Detail);
                return;
            }

            runtime.RefreshAll();
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
