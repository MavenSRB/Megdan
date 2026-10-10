using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GuardsDigital.Pve.EditorTools
{
    /// <summary>
    /// The separate PvE executable's build (PVE-D01), invoked by Tools/Invoke-PvePlayerBuildSmoke.ps1
    /// as one parameterless method. It names its scenes explicitly - the PvE launcher, then the
    /// existing match scene it composes into - and never reads or edits Build Settings, so the
    /// existing player's scene list, output and project settings are untouched.
    /// </summary>
    /// <remarks>
    /// The PvE assemblies compile into a player only under <see cref="Define"/>, which this build
    /// alone supplies; the existing build therefore contains no PvE assembly. The product name and
    /// version stamp are set for this build and restored afterwards, before any asset is saved.
    /// </remarks>
    public static class PveBuildEntryPoint
    {
        public const string MarkerPrefix = "GUARDS_PVE_BUILD:";
        public const string Define = "GUARDS_PVE";
        public const string OutputFolder = "Builds/PveWin64";
        public const string ExecutableName = "GuardsDigitalPvE.exe";
        public const string ProductName = "Guards PvE";
        public const string DataSourceFolder = "Assets/GuardsPve/Data~";
        public const string DataOutputFolder = "PveData";

        public static readonly string[] Scenes =
        {
            "Assets/GuardsPve/Scenes/PveLauncher.unity",
            "Assets/Scenes/Milestone1_2_HexGrid.unity"
        };

        public static void BuildWindowsPvePlayer()
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
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)!.FullName;
            foreach (var scene in Scenes)
            {
                if (!File.Exists(Path.Combine(projectRoot, scene)))
                {
                    Debug.LogError($"{MarkerPrefix} FAIL missing scene {scene}");
                    return 1;
                }
            }

            var outputDirectory = Path.Combine(projectRoot, OutputFolder);
            foreach (var stale in new[]
            {
                Path.Combine(outputDirectory, ExecutableName),
                Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(ExecutableName) + "_Data"),
                Path.Combine(outputDirectory, DataOutputFolder)
            })
            {
                if (File.Exists(stale)) File.Delete(stale);
                else if (Directory.Exists(stale)) Directory.Delete(stale, true);
            }

            Directory.CreateDirectory(outputDirectory);
            var executablePath = Path.Combine(outputDirectory, ExecutableName);
            Debug.Log($"{MarkerPrefix} BUILDING scenes={Scenes.Length} define={Define}");

            var baseVersion = PlayerSettings.bundleVersion;
            var baseProductName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                var explicitStamp = Environment.GetEnvironmentVariable("GUARDS_BUILD_STAMP");
                PlayerSettings.bundleVersion = !string.IsNullOrWhiteSpace(explicitStamp)
                    ? explicitStamp.Trim()
                    : baseVersion + "-pve+" +
                        DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) +
                        "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                PlayerSettings.productName = ProductName;
                Debug.Log($"{MarkerPrefix} STAMP {PlayerSettings.bundleVersion}");

                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes,
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.StrictMode,
                    extraScriptingDefines = new[] { Define }
                });
            }
            finally
            {
                PlayerSettings.bundleVersion = baseVersion;
                PlayerSettings.productName = baseProductName;
                AssetDatabase.SaveAssets();
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{MarkerPrefix} FAIL build result {report.summary.result}, " +
                    $"{report.summary.totalErrors} error(s)");
                return 1;
            }

            if (!File.Exists(executablePath))
            {
                Debug.LogError($"{MarkerPrefix} FAIL build reported success but produced no executable at {executablePath}");
                return 1;
            }

            var dataTarget = Path.Combine(outputDirectory, DataOutputFolder);
            Directory.CreateDirectory(dataTarget);
            foreach (var file in Directory.GetFiles(Path.Combine(projectRoot, DataSourceFolder), "*.json"))
            {
                File.Copy(file, Path.Combine(dataTarget, Path.GetFileName(file)), true);
                Debug.Log($"{MarkerPrefix} DATA {Path.GetFileName(file)}");
            }

            Debug.Log($"{MarkerPrefix} BUILT {executablePath}");
            Debug.Log($"{MarkerPrefix} PASS");
            return 0;
        }
    }
}
