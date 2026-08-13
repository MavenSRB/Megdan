using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GuardsDigital.EditorTools
{
    /// <summary>
    /// Fixed project-local entry point for producing a player build, invoked from
    /// Tools/Invoke-PlayerBuildSmoke.ps1 in the same spirit as the Unity validation launcher: one
    /// parameterless method, no ad-hoc command composition at the call site.
    /// </summary>
    public static class PlayerBuildEntryPoint
    {
        public const string MarkerPrefix = "GUARDS_BUILD:";

        private const string OutputFolder = "Builds/Win64Smoke";
        private const string ExecutableName = "GuardsDigitalSmoke.exe";

        /// <summary>
        /// Builds the enabled scenes for the active reference target, Windows 64-bit standalone.
        /// Exits non-zero on anything other than a succeeded report so a failure cannot be mistaken
        /// for a pass by a caller that only reads stdout.
        /// </summary>
        public static void BuildWindowsSmokePlayer()
        {
            var exitCode = 1;
            try
            {
                exitCode = Build();
            }
            catch (Exception exception)
            {
                Debug.LogError($"{MarkerPrefix} FAIL unhandled build exception: {exception}");
            }
            finally
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static int Build()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError($"{MarkerPrefix} FAIL no enabled scene is configured in Build Settings.");
                return 1;
            }

            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)!.FullName;
            var outputDirectory = Path.Combine(projectRoot, OutputFolder);
            if (Directory.Exists(outputDirectory))
            {
                // A stale executable from an earlier run must never be able to satisfy the gate.
                // Delete only the executable and the managed payload rather than the whole tree:
                // removing and immediately recreating the directory races Windows' handle release
                // and can make the player's file copies fail partway.
                foreach (var stale in new[]
                {
                    Path.Combine(outputDirectory, ExecutableName),
                    Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(ExecutableName) + "_Data")
                })
                {
                    if (File.Exists(stale)) { File.Delete(stale); }
                    else if (Directory.Exists(stale)) { Directory.Delete(stale, true); }
                }
            }

            Directory.CreateDirectory(outputDirectory);
            var executablePath = Path.Combine(outputDirectory, ExecutableName);

            Debug.Log($"{MarkerPrefix} BUILDING scenes={scenes.Length} target=StandaloneWindows64");
            foreach (var scene in scenes)
            {
                Debug.Log($"{MarkerPrefix} SCENE {scene}");
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.StrictMode
            });

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{MarkerPrefix} FAIL build result {summary.result}, " +
                    $"{summary.totalErrors} error(s)");
                return 1;
            }

            if (!File.Exists(executablePath))
            {
                Debug.LogError($"{MarkerPrefix} FAIL build reported success but produced no executable at {executablePath}");
                return 1;
            }

            Debug.Log($"{MarkerPrefix} BUILT {executablePath}");
            Debug.Log($"{MarkerPrefix} PASS");
            return 0;
        }
    }
}
