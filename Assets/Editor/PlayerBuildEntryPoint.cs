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
        /// Milestone 8.77. The macOS player: a Mono, universal (x64 + arm64) `.app` built from this
        /// Windows host with the Mac Build Support module, packaged beside the Windows folder for the
        /// user's Mac players. It is BUILT here and can only be LAUNCHED on a Mac, so
        /// <see cref="BuildMacPlayer"/> proves the build report and the bundle's layout and nothing
        /// more - the project's docs declare macOS rather than validate it.
        /// </summary>
        private const string MacOutputFolder = "Builds/MacBuild";
        private const string MacBundleName = "GuardsDigital.app";

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
                exitCode = Build(BuildTarget.StandaloneWindows64, OutputFolder, ExecutableName);
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

        /// <summary>
        /// Builds the enabled scenes as a macOS universal player (Mono) into
        /// <c>Builds/MacBuild/GuardsDigital.app</c>. Same contract as the Windows entry: non-zero on
        /// anything but a succeeded report with the bundle present.
        /// </summary>
        public static void BuildMacPlayer()
        {
            var exitCode = 1;
            try
            {
                // The string-keyed platform setting avoids referencing the OSX module's assembly from
                // a script that must also compile on a host without it.
                EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "x64ARM64");
                exitCode = Build(BuildTarget.StandaloneOSX, MacOutputFolder, MacBundleName);
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

        private static int Build(BuildTarget target, string outputFolder, string artifactName)
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
            var outputDirectory = Path.Combine(projectRoot, outputFolder);
            if (Directory.Exists(outputDirectory))
            {
                // A stale executable from an earlier run must never be able to satisfy the gate.
                // Delete only the executable and the managed payload rather than the whole tree:
                // removing and immediately recreating the directory races Windows' handle release
                // and can make the player's file copies fail partway.
                foreach (var stale in new[]
                {
                    Path.Combine(outputDirectory, artifactName),
                    Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(artifactName) + "_Data")
                })
                {
                    if (File.Exists(stale)) { File.Delete(stale); }
                    else if (Directory.Exists(stale)) { Directory.Delete(stale, true); }
                }
            }

            Directory.CreateDirectory(outputDirectory);
            var executablePath = Path.Combine(outputDirectory, artifactName);

            Debug.Log($"{MarkerPrefix} BUILDING scenes={scenes.Length} target={target}");
            foreach (var scene in scenes)
            {
                Debug.Log($"{MarkerPrefix} SCENE {scene}");
            }

            // Milestone 8.32. Each build carries its own identity: the private-online admission
            // tuple reads Application.version, and two players built from different revisions of
            // the same coarse version string would otherwise admit each other while speaking
            // different wire vocabularies. The stamp is set for the build alone and restored
            // afterward, so the checked-in project settings never change.
            var baseVersion = PlayerSettings.bundleVersion;
            BuildReport report;
            try
            {
                PlayerSettings.bundleVersion = baseVersion + "+" +
                    DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) +
                    "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Debug.Log($"{MarkerPrefix} STAMP {PlayerSettings.bundleVersion}");

                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = executablePath,
                    target = target,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.StrictMode
                });
            }
            finally
            {
                PlayerSettings.bundleVersion = baseVersion;
                AssetDatabase.SaveAssets();
            }

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{MarkerPrefix} FAIL build result {summary.result}, " +
                    $"{summary.totalErrors} error(s)");
                return 1;
            }

            // A macOS player is a bundle DIRECTORY (`.app`); the Windows player is a file.
            if (!File.Exists(executablePath) && !Directory.Exists(executablePath))
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
