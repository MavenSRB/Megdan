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
        public void SavedScene_UsesCompositionRuntimeAndContainsNoLegacyControllerOrDirectSelectorBinding()
        {
            var scene = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath,
                "Scenes",
                "Milestone1_2_HexGrid.unity"));

            Assert.That(scene, Does.Contain("guid: e175f8893df543869e41ab5f42efc0ba"));
            Assert.That(scene, Does.Contain("guid: 926572b67b5c43ae88abb1568bd9f54a"));
            Assert.That(scene, Does.Not.Contain("guid: dfc32a097684982449279851bed7aab5"));
            Assert.That(scene, Does.Not.Match(@"selectionController: \{fileID: (?!0\})"));
        }

        [Test]
        public void ConfiguredOwnership_IsSplitAcrossCompositionRuntimePassiveHudAndPointerAdapter()
        {
            var rootFields = Fields(typeof(MatchSceneCompositionRoot));
            var runtimeFields = Fields(typeof(ConfiguredMatchSceneRuntime));
            var hudFields = Fields(typeof(ConfiguredMatchHudRenderer));
            var pointerFields = Fields(typeof(PrimitiveHeroPointerInput));

            Assert.That(rootFields.Any(value => value.FieldType == typeof(HeroSelectionController)), Is.False);
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
