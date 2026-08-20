using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The configured-match cutover boundaries.
    ///
    /// Milestone 6.1k, finding F613-13, replaced this fixture's literal scene GUIDs, its
    /// <c>Does.Contain</c> assertions over presentation source, and its pin on the text
    /// <c>private bool generated;</c>. Each replacement protects the same thing through the asset
    /// database, reflection, or compiled code, and the mapping is stated at each test.
    /// </summary>
    public sealed class ConfiguredMatchCutoverArchitectureTests
    {
        private const string ScenePath = "Assets/Scenes/Milestone1_2_HexGrid.unity";

        [Test]
        public void SavedScene_UsesCompositionRuntimeAndCarriesNoStaleSerializedBinding()
        {
            // Replaces two literal `guid: <hex>` pins. The GUIDs now come from the asset database, so
            // moving a script - which keeps its GUID - still passes, and deleting one fails loudly
            // instead of leaving a hex string nobody can trace back to a type.
            var blocks = SceneAssetInspection.ReadComponentBlocks(ScenePath);
            var scriptGuids = blocks.Select(block => block.ScriptGuid).ToArray();

            foreach (var required in new[] { typeof(MatchSceneCompositionRoot), typeof(ConfiguredMatchHudRenderer) })
            {
                var guid = SceneAssetInspection.ScriptGuid(required);
                Assert.That(guid, Is.Not.Null.And.Not.Empty,
                    $"Could not resolve a MonoScript GUID for {required.Name}.");
                Assert.That(scriptGuids, Does.Contain(guid),
                    $"The saved scene must carry a {required.Name} component.");
            }

            // Replaces `Does.Not.Contain("selectionController:")`. Milestone 6.1c retired that field
            // with the drill that owned it, and the pin could only ever say "this one retired name is
            // absent". The general statement is the one worth making: every authored key the scene
            // serializes still corresponds to a field the component actually declares, so no stale
            // binding of any name survives a field's removal.
            var stale = blocks
                .Select(block => new { Block = block, Type = SceneAssetInspection.TypeForScriptGuid(block.ScriptGuid) })
                .Where(entry => entry.Type != null)
                .SelectMany(entry => entry.Block.AuthoredPropertyKeys
                    .Where(key => !SceneAssetInspection.SerializedFieldNames(entry.Type).Contains(key))
                    .Select(key => $"{ScenePath}:{entry.Block.LineNumber}: {entry.Type.Name} has no serialized field '{key}'"))
                .ToArray();

            Assert.That(stale, Is.Empty, string.Join(Environment.NewLine, stale));
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
            Assert.That(rootFields, Is.Not.Empty);
            // Milestone 7.8a moved the HUD from immediate mode onto a runtime UI Toolkit panel, so the
            // pin that used to require an `OnGUI` now requires the panel host instead. What it protects
            // is unchanged: the HUD component, and only the HUD component, owns the drawing surface.
            Assert.That(
                typeof(ConfiguredMatchHudRenderer).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Null); // absence-intended: nothing in this scene draws in immediate mode any more.
            Assert.That(
                typeof(ConfiguredMatchHudRenderer).GetProperty(
                    nameof(ConfiguredMatchHudRenderer.RootElement)),
                Is.Not.Null);
            Assert.That(
                typeof(MatchSceneCompositionRoot).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Null); // absence-intended: the composition root must not own a drawing surface.
        }

        [Test]
        public void ConfiguredPointerSource_ForwardsOnlyThroughPassiveBoardCallbacks()
        {
            // Replaces four `Does.Contain` assertions over presentation source text. What they
            // protected - the pointer adapters reach the board only through its passive forwarding
            // callbacks, and the root wires them to the runtime's board view - is now read from
            // compiled code, so it survives a rename of any of those members and cannot be satisfied
            // by a comment that happens to contain the string.
            //
            // The forwarding *behavior* is covered by ConfiguredPointerSelectionTests, which drives a
            // real pointer ray through a real view. This test owns only the structural residue: that
            // there is no second path.
            //
            // Two of the old assertions were dropped rather than translated. Both pinned the absence of
            // `controller.ConfigureSceneBindings` and of a legacy `pointerInput?.Configure(...)` call on
            // the drill milestone 6.1c deleted. A deleted type's members cannot be called, and the
            // compiler enforces that; an assertion that a deleted name is absent is vacuous.
            // Every member below is named through `nameof`, not as a string. A rename of one of them
            // updates this reference with it, and a partial rename that missed it would not compile -
            // which is a louder signal than the runtime assertion failure the old text pins produced,
            // and unlike them it cannot go quietly false.
            var forwardDestination = ILConformanceScanner.Method(
                typeof(BoardInteractionUnityView), nameof(BoardInteractionUnityView.TryForwardDestinationIntent));
            var forwardHero = ILConformanceScanner.Method(
                typeof(BoardInteractionUnityView), nameof(BoardInteractionUnityView.TryForwardHeroIntent));
            var configureSelector = ILConformanceScanner.Method(
                typeof(PrimitiveHeroPointerSelector),
                nameof(PrimitiveHeroPointerSelector.Configure),
                typeof(BoardInteractionUnityView));
            var boardView = ILConformanceScanner
                .Property(typeof(ConfiguredMatchSceneRuntime), nameof(ConfiguredMatchSceneRuntime.BoardView))
                .GetGetMethod(true);

            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(PrimitiveHeroPointerInput)), forwardDestination),
                Is.True,
                "The pointer input must submit destination intent through the passive board view.");
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(PrimitiveHeroPointerSelector)), forwardHero),
                Is.True,
                "The hero selector must submit hero intent through the passive board view.");
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(StandardMinionPointerSelector)), forwardHero)
                || ILConformanceScanner.Scan(ILConformanceScanner.WithNestedTypes(typeof(StandardMinionPointerSelector)))
                    .Any(method => method.Instructions.Any(instruction =>
                        instruction.Member != null &&
                        instruction.Member.DeclaringType == typeof(BoardInteractionUnityView))),
                Is.True,
                "The minion selector must also reach the board only through the passive view.");

            var root = ILConformanceScanner.WithNestedTypes(typeof(MatchSceneCompositionRoot)).ToArray();
            Assert.That(ILConformanceScanner.References(root, configureSelector), Is.True,
                "The composition root must wire the selector's one passive Configure path.");
            Assert.That(ILConformanceScanner.References(root, boardView), Is.True,
                "The composition root must wire the adapters to the runtime's board view.");
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
                    "SelectionController", // absence-intended: this accessor must stay deleted.
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Null,
                "The legacy controller accessor is gone with the drill it exposed.");
        }

        [Test]
        public void HighlightOwner_ReleasesOwnedMaterialThroughBothUnityTeardownPaths()
        {
            // Replaces five source-text pins, including `private bool generated;` - a private field
            // declaration, which is precisely what F613-13 names. Renaming that field broke the old
            // assertion while changing nothing observable.
            //
            // What the pins protected splits in two. Generated-flag reuse and owned-only teardown are
            // behavioral and are owned by DestinationHighlightMaterialOwnershipTests, which constructs
            // the owner and counts real materials; they are not restated here, because the testing
            // policy forbids duplicating one owned invariant across layers. What no Edit Mode test can
            // reach is the play-mode branch: `Object.Destroy` runs only while playing, and an Edit Mode
            // fixture always takes `DestroyImmediate`. That branch is covered two ways - this check
            // proves both teardown paths still exist in the compiled method, and
            // DestinationHighlightMaterialPlayModeTests exercises the `Destroy` path for real.
            var dispose = ILConformanceScanner.Scan(typeof(DestinationHighlightMaterialOwner))
                .Single(method => method.Method.Name == nameof(IDisposable.Dispose));

            Assert.That(dispose.Calls(ILConformanceScanner.Method(
                    typeof(UnityEngine.Object),
                    nameof(UnityEngine.Object.Destroy),
                    typeof(UnityEngine.Object))),
                Is.True,
                "Disposing while playing must destroy the owned material through Object.Destroy.");
            Assert.That(dispose.Calls(ILConformanceScanner.Method(
                    typeof(UnityEngine.Object),
                    nameof(UnityEngine.Object.DestroyImmediate),
                    typeof(UnityEngine.Object))),
                Is.True,
                "Disposing in the Editor must destroy the owned material through Object.DestroyImmediate.");
        }

        private static FieldInfo[] Fields(Type type) =>
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
