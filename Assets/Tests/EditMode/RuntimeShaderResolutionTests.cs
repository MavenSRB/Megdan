using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
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
    ///
    /// Milestone 6.1k replaced the two source regexes. Shader names now come from the <c>ldstr</c>
    /// operands the compiler emitted for each <see cref="Shader.Find"/> argument, and the
    /// fail-loudly guard is expressed as instruction order rather than as the textual layout of the
    /// surrounding lines. Both changes widen what is caught: a fully-qualified or aliased
    /// <c>Shader.Find</c>, and a guard that is reformatted or wrapped, all read the same way now.
    /// A name that is not a literal - built by concatenation, or read from a field - is invisible to
    /// this and was invisible to the regex; the always-included list is asserted non-empty so the
    /// fixture cannot pass by finding nothing.
    /// </summary>
    public sealed class RuntimeShaderResolutionTests
    {
        private const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";

        [Test]
        public void EveryRuntimeShaderName_ResolvesAndIsAlwaysIncluded()
        {
            var runtimeShaderNames = FindRuntimeShaderNames();

            // Without this the fixture would pass vacuously the moment the scan or the production
            // assembly set changed - the same failure mode the living-document validator guards with
            // its no-declared-behaviours check.
            Assert.That(runtimeShaderNames, Is.Not.Empty,
                "No runtime Shader.Find call was discovered in production code. The scan is wrong, or " +
                "production moved; either way this fixture would otherwise pass vacuously.");

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
            var find = ShaderFindMethod();
            var materialFromShader = ILConformanceScanner.Constructor(typeof(Material), typeof(Shader));
            var offenders = new List<string>();
            var inspected = 0;

            // Per method rather than per file. A file-level search gives a false negative on any file
            // that null-checks something else, which is most of them - so it would report one offender
            // while two more went unfixed.
            foreach (var method in ILConformanceScanner.Scan(ProductionTypes()))
            {
                var lookupIndex = method.FirstIndexOf(find);
                if (lookupIndex < 0)
                {
                    continue;
                }

                inspected++;
                var throwIndex = method.FirstIndexOf(System.Reflection.Emit.OpCodes.Throw);
                var materialIndex = method.FirstIndexOf(materialFromShader);

                if (throwIndex < 0)
                {
                    offenders.Add($"{method.Description} resolves a shader without throwing on null");
                    continue;
                }

                if (materialIndex >= 0 && materialIndex < throwIndex)
                {
                    offenders.Add(
                        $"{method.Description} builds a Material from the lookup before the guard throws");
                }
            }

            Assert.That(inspected, Is.GreaterThan(0),
                "No production method resolves a shader by name, so this rule inspected nothing.");
            Assert.That(offenders, Is.Empty,
                "Every runtime shader lookup must fail loudly, using the explicit-failure pattern in " +
                "DestinationHighlightMaterialOwner, rather than assigning a null material: " +
                string.Join("; ", offenders));
        }

        private static IReadOnlyList<string> FindRuntimeShaderNames()
        {
            var find = ShaderFindMethod();
            var names = new SortedSet<string>(
                ILConformanceScanner.Scan(ProductionTypes()).SelectMany(method => method.StringsPassedTo(find)),
                StringComparer.Ordinal);
            return names.ToArray();
        }

        private static System.Reflection.MethodInfo ShaderFindMethod() =>
            ILConformanceScanner.Method(typeof(Shader), nameof(Shader.Find), typeof(string));

        /// <summary>
        /// Production code only. Test scaffolding also resolves shaders to build throwaway materials,
        /// and a fixture is not a runtime path - the same exclusion the living-document validator
        /// applies when it scans for reachable components. The pure assemblies are included even
        /// though they cannot reference Unity, because that is a boundary
        /// <see cref="Phase4ArchitectureConformanceTests"/> enforces rather than something this
        /// fixture should assume.
        /// </summary>
        private static IEnumerable<Type> ProductionTypes() =>
            new[]
            {
                typeof(RoundResolutionAuthority).Assembly,
                typeof(CardInstance).Assembly,
                typeof(MatchSession).Assembly,
                typeof(DestinationHighlightMaterialOwner).Assembly
            }.Distinct().SelectMany(ILConformanceScanner.TypesIn);

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
