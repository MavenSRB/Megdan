using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Every shader a runtime code path resolves by name must be declared in the always-included
    /// shader list. Before milestone 6.1d that list was empty and resolution succeeded only because
    /// one placeholder material referenced from the enabled scene happened to drag the Standard
    /// shader into the build - so deleting or reassigning that material would have shipped magenta
    /// while the Editor kept working. This fixture makes that dependency explicit: a runtime shader
    /// name that is not declared fails here rather than in a player nobody has built yet.
    /// </summary>
    public sealed class RuntimeShaderResolutionTests
    {
        private const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";

        [Test]
        public void EveryRuntimeShaderName_ResolvesAndIsAlwaysIncluded()
        {
            var runtimeShaderNames = FindRuntimeShaderNames();

            // Without this the fixture would pass vacuously the moment the search pattern or the
            // production folder layout changed - the same failure mode the living-document
            // validator guards with its no-declared-behaviours check.
            Assert.That(runtimeShaderNames, Is.Not.Empty,
                "No runtime Shader.Find call was discovered in production source. The search is " +
                "wrong, or production moved; either way this fixture would otherwise pass vacuously.");

            var alwaysIncluded = AlwaysIncludedShaderNames();
            Assert.That(alwaysIncluded, Is.Not.Empty,
                "The always-included shader list is empty, so every runtime Shader.Find depends on " +
                "an unrelated asset reference happening to pull the shader into the build.");

            foreach (var shaderName in runtimeShaderNames)
            {
                Assert.That(Shader.Find(shaderName), Is.Not.Null,
                    $"Runtime code resolves the shader '{shaderName}', but it does not resolve in the Editor.");
                Assert.That(alwaysIncluded, Does.Contain(shaderName),
                    $"Runtime code resolves the shader '{shaderName}' by name, so it must be listed in " +
                    "Graphics settings' always-included shaders. Otherwise it reaches the player only " +
                    "by accident, through some other asset that references it.");
            }
        }

        [Test]
        public void EveryRuntimeShaderLookup_FailsLoudlyInsteadOfBuildingANullMaterial()
        {
            var offenders = new List<string>();

            foreach (var sourceFile in ProductionSourceFiles())
            {
                var text = File.ReadAllText(sourceFile);
                var fileName = Path.GetFileName(sourceFile);

                // Per call site, not per file. A file-level search for "== null" gives a false
                // negative on any file that happens to null-check something else, which is most of
                // them - so it would report one offender while two more went unfixed.
                foreach (Match match in Regex.Matches(text, @"Shader\.Find\(\s*""[^""]+""\s*\)"))
                {
                    var lineNumber = text.Take(match.Index).Count(character => character == '\n') + 1;

                    // The accepted pattern assigns the lookup, tests the local for null, and throws.
                    // A lookup used directly as a constructor argument was never null-checked at all.
                    var precedingText = text.Substring(0, match.Index);
                    if (Regex.IsMatch(precedingText, @"new\s+Material\(\s*$"))
                    {
                        offenders.Add($"{fileName}:{lineNumber} constructs a Material directly from Shader.Find");
                        continue;
                    }

                    var assignment = Regex.Match(precedingText, @"(?:var|Shader)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*$");
                    if (!assignment.Success)
                    {
                        offenders.Add($"{fileName}:{lineNumber} resolves a shader in an unrecognised shape");
                        continue;
                    }

                    var local = Regex.Escape(assignment.Groups[1].Value);
                    var following = text.Substring(match.Index + match.Length);
                    var guard = new Regex($@"if\s*\(\s*{local}\s*==\s*null\s*\)\s*\{{[^}}]*throw\b");
                    var guardMatch = guard.Match(following);

                    // The guard has to come before the local is used to build anything.
                    var firstUse = new Regex($@"new\s+Material\(\s*{local}\b").Match(following);
                    if (!guardMatch.Success || (firstUse.Success && firstUse.Index < guardMatch.Index))
                    {
                        offenders.Add(
                            $"{fileName}:{lineNumber} assigns '{assignment.Groups[1].Value}' without throwing on null first");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "Every runtime shader lookup must fail loudly, using the explicit-failure pattern in " +
                "DestinationHighlightMaterialOwner, rather than assigning a null material: " +
                string.Join("; ", offenders));
        }

        private static IReadOnlyList<string> FindRuntimeShaderNames()
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var sourceFile in ProductionSourceFiles())
            {
                foreach (Match match in Regex.Matches(
                    File.ReadAllText(sourceFile), @"Shader\.Find\(\s*""([^""]+)""\s*\)"))
                {
                    names.Add(match.Groups[1].Value);
                }
            }

            return names.ToArray();
        }

        /// <summary>
        /// Production source only. Test scaffolding also resolves shaders to build throwaway
        /// materials, and a fixture is not a runtime path - the same exclusion the living-document
        /// validator applies when it scans for reachable components.
        /// </summary>
        private static IEnumerable<string> ProductionSourceFiles()
        {
            var productionRoot = Path.Combine(UnityEngine.Application.dataPath, "GuardsDigital");
            return Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories);
        }

        private static IReadOnlyList<string> AlwaysIncludedShaderNames()
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath).FirstOrDefault();
            Assert.That(graphicsSettings, Is.Not.Null,
                $"Could not load {GraphicsSettingsAssetPath}; the always-included list cannot be read.");

            var serialized = new SerializedObject(graphicsSettings);
            var alwaysIncluded = serialized.FindProperty("m_AlwaysIncludedShaders");
            Assert.That(alwaysIncluded, Is.Not.Null,
                "GraphicsSettings has no m_AlwaysIncludedShaders property; the Unity version changed its shape.");

            var names = new List<string>();
            for (var index = 0; index < alwaysIncluded.arraySize; index++)
            {
                var shader = alwaysIncluded.GetArrayElementAtIndex(index).objectReferenceValue as Shader;
                if (shader != null)
                {
                    names.Add(shader.name);
                }
            }

            return names;
        }
    }
}
