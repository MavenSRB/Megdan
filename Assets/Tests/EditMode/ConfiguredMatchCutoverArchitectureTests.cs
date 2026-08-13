using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ConfiguredMatchCutoverArchitectureTests
    {
        [Test]
        public void SavedScene_UsesCompositionRuntimeAndCarriesNoStaleSelectorBinding()
        {
            var scene = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath,
                "Scenes",
                "Milestone1_2_HexGrid.unity"));

            Assert.That(scene, Does.Contain("guid: e175f8893df543869e41ab5f42efc0ba"));
            Assert.That(scene, Does.Contain("guid: 926572b67b5c43ae88abb1568bd9f54a"));

            // Milestone 6.1c deleted the legacy drill, so the former "this GUID is absent" pin became
            // vacuous - a deleted asset's GUID is trivially absent from every scene. What is still
            // worth pinning is that no serialized selector binding survives the field's removal.
            Assert.That(scene, Does.Not.Contain("selectionController:"));
        }

        [Test]
        public void ConfiguredOwnership_IsSplitAcrossCompositionRuntimePassiveHudAndPointerAdapter()
        {
            var rootFields = Fields(typeof(MatchSceneCompositionRoot));
            var runtimeFields = Fields(typeof(ConfiguredMatchSceneRuntime));
            var hudFields = Fields(typeof(ConfiguredMatchHudRenderer));
            var pointerFields = Fields(typeof(PrimitiveHeroPointerInput));

            Assert.That(runtimeFields.Count(value => value.FieldType == typeof(IMatchSession)), Is.EqualTo(1));
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(typeof(ConfiguredMatchSceneRuntime)), Is.False);
            Assert.That(runtimeFields.Any(value => value.FieldType == typeof(PrimitiveHeroPointerInput)), Is.False);
            Assert.That(hudFields.Any(value => value.FieldType == typeof(IMatchSession)), Is.False);
            Assert.That(pointerFields.Any(value => value.FieldType == typeof(IMatchSession)), Is.False);
            Assert.That(
                typeof(ConfiguredMatchHudRenderer).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(MatchSceneCompositionRoot).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Null);
        }

        [Test]
        public void ConfiguredPointerSource_ForwardsOnlyThroughPassiveBoardCallbacks()
        {
            var input = ReadPresentationSource("Interaction", "PrimitiveHeroPointerInput.cs");
            var selector = ReadPresentationSource("Interaction", "PrimitiveHeroPointerSelector.cs");
            var root = ReadPresentationSource("Interaction", "MatchSceneCompositionRoot.cs");

            Assert.That(input, Does.Contain("boardView.TryForwardDestinationIntent"));
            Assert.That(selector, Does.Contain("boardView.TryForwardHeroIntent"));
            Assert.That(root, Does.Contain("selector.Configure(runtime.BoardView)"));
            Assert.That(root, Does.Contain("runtime.BoardView,"));
            Assert.That(root, Does.Not.Contain("controller.ConfigureSceneBindings"));
            Assert.That(root, Does.Not.Contain("pointerInput?.Configure(eventCamera, pointerSelectors, controller)"));
        }

        [Test]
        public void ConfiguredPointerAdapters_ExposeExactlyOnePassiveConfigurePathEach()
        {
            // Replaces the milestone 6.1c-retired assertions that pinned the legacy drill's absence
            // by scene GUID and by field type. Those could only ever say "the old thing is gone";
            // this says the surviving adapters have no second, non-passive way to be wired at all.
            foreach (var adapter in new[] { typeof(PrimitiveHeroPointerSelector), typeof(PrimitiveHeroPointerInput) })
            {
                var configureMethods = adapter
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(method => method.Name == "Configure")
                    .ToArray();

                Assert.That(configureMethods, Has.Length.EqualTo(1),
                    $"{adapter.Name} must offer one wiring path, not a passive one plus a legacy one.");
                Assert.That(
                    configureMethods[0].GetParameters()
                        .Any(parameter => parameter.ParameterType == typeof(BoardInteractionUnityView)),
                    Is.True,
                    $"{adapter.Name} must be wired through the passive board view.");
                Assert.That(
                    Fields(adapter).Any(field => field.FieldType.Namespace != null &&
                        field.FieldType.Namespace.StartsWith("GuardsDigital.Application", StringComparison.Ordinal)),
                    Is.False,
                    $"{adapter.Name} must not hold a session-side type directly.");
            }

            Assert.That(
                typeof(PrimitiveHeroPointerSelector).GetProperty(
                    "SelectionController",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Null,
                "The legacy controller accessor is gone with the drill it exposed.");
        }

        [Test]
        public void HighlightOwnerSource_RequiresGeneratedFlagReuseAndOwnedOnlyTeardown()
        {
            var source = ReadPresentationSource("Match", "DestinationHighlightMaterialOwner.cs");

            Assert.That(source, Does.Contain("private bool generated;"));
            Assert.That(source, Does.Contain("if (material == null)"));
            Assert.That(source, Does.Contain("if (!generated || material == null)"));
            Assert.That(source, Does.Contain("UnityEngine.Object.Destroy(ownedMaterial)"));
            Assert.That(source, Does.Contain("UnityEngine.Object.DestroyImmediate(ownedMaterial)"));
        }

        private static FieldInfo[] Fields(Type type) =>
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static string ReadPresentationSource(string folder, string file) =>
            File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath,
                "GuardsDigital",
                "Presentation",
                folder,
                file));
    }
}
