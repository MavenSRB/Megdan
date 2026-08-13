using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1e, finding F613-04. The composition root generates the board, spawn, and minion
    /// presentation at load. Nothing it intends to own may also be serialized into the saved scene:
    /// a serialized copy is regenerated anyway, drifts from the map it came from, and produces the
    /// unbound or duplicate views whose retirement triggers the F613-05 collection hazard.
    ///
    /// The scene asset is inspected as a file rather than opened. Opening it would run the
    /// [ExecuteAlways] composition root and generate the very objects under test.
    /// </summary>
    public sealed class ConfiguredSceneSerializationTests
    {
        private const string ScenePath = "Assets/Scenes/Milestone1_2_HexGrid.unity";

        [Test]
        public void ConfiguredScene_SerializesNoGeneratedPresentationObject()
        {
            Assert.That(File.Exists(ScenePath), Is.True, $"Missing configured scene at {ScenePath}.");

            var generatedTypes = new[]
            {
                typeof(HexTileView),
                typeof(StandardMinionView),
                typeof(BoardMapSpawnMarkerView),
                typeof(PlaceholderObstacleView)
            };

            var sceneText = File.ReadAllText(ScenePath);
            var offenders = new List<string>();
            foreach (var type in generatedTypes)
            {
                var guid = ScriptGuid(type);
                Assert.That(
                    guid,
                    Is.Not.Null.And.Not.Empty,
                    $"Could not resolve a MonoScript GUID for {type.Name}.");

                // Count serialized component references to this script, derived from the asset
                // database rather than hardcoded, so a rename or file move cannot silently pass.
                var occurrences = CountOccurrences(sceneText, $"guid: {guid}");
                if (occurrences > 0)
                {
                    offenders.Add($"{type.Name} x{occurrences}");
                }
            }

            Assert.That(
                offenders,
                Is.Empty,
                "The saved scene serializes presentation objects the composition root generates and " +
                $"owns: {string.Join(", ", offenders)}. Delete them from the scene; composition " +
                "recreates them at load.");
        }

        private static string ScriptGuid(System.Type type)
        {
            foreach (var assetGuid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    return assetGuid;
                }
            }

            return null;
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = text.IndexOf(value, System.StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = text.IndexOf(value, index + value.Length, System.StringComparison.Ordinal);
            }

            return count;
        }
    }
}
