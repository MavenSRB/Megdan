using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Reads the saved scene as an asset file rather than opening it. Opening would run the
    /// <c>[ExecuteAlways]</c> composition root and generate the very objects the scene fixtures are
    /// asserting about, so the file is parsed instead - but every identity in it is resolved through
    /// the asset database and the type system, never hardcoded.
    ///
    /// Milestone 6.1k, finding F613-13: the literal <c>guid: e175f889...</c> pins and the
    /// <c>selectionController:</c> string pin lived here before, in two fixtures, one of which had
    /// already derived its GUIDs. This is that derivation, shared.
    /// </summary>
    public static class SceneAssetInspection
    {
        private static readonly Regex PropertyKey = new Regex(
            @"^  ([A-Za-z_][A-Za-z0-9_]*):", RegexOptions.CultureInvariant);

        private static readonly Regex ScriptGuidValue = new Regex(
            @"^  m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: \d+\}", RegexOptions.CultureInvariant);

        /// <summary>The MonoScript GUID Unity assigned to this type's source file.</summary>
        public static string ScriptGuid(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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

        /// <summary>The type a serialized <c>m_Script</c> GUID refers to, or null when the GUID belongs
        /// to a package, an Editor script, or an asset that no longer resolves to a class.</summary>
        public static Type TypeForScriptGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass();
        }

        /// <summary>
        /// Every serialized component block in the scene, with the property keys the asset actually
        /// carries for it. Only top-level keys are collected; deeper indentation is part of a value.
        /// </summary>
        public static IReadOnlyList<SceneComponentBlock> ReadComponentBlocks(string scenePath)
        {
            var lines = File.ReadAllLines(scenePath);
            var blocks = new List<SceneComponentBlock>();
            string guid = null;
            var keys = new List<string>();
            var startLine = 0;
            var inBlock = false;

            void Flush()
            {
                if (inBlock && guid != null)
                {
                    blocks.Add(new SceneComponentBlock(guid, keys.ToArray(), startLine));
                }

                inBlock = false;
                guid = null;
                keys = new List<string>();
            }

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (line.StartsWith("--- !u!", StringComparison.Ordinal))
                {
                    Flush();
                    inBlock = true;
                    startLine = index + 1;
                    continue;
                }

                if (!inBlock)
                {
                    continue;
                }

                var scriptMatch = ScriptGuidValue.Match(line);
                if (scriptMatch.Success)
                {
                    guid = scriptMatch.Groups[1].Value;
                    continue;
                }

                var keyMatch = PropertyKey.Match(line);
                if (keyMatch.Success)
                {
                    keys.Add(keyMatch.Groups[1].Value);
                }
            }

            Flush();
            return blocks;
        }

        /// <summary>
        /// The field names Unity serializes for a component: instance fields that are public or carry
        /// <see cref="SerializeField"/>, excluding <see cref="NonSerializedAttribute"/>, across the
        /// declared hierarchy up to the Unity base type.
        /// </summary>
        public static IReadOnlyCollection<string> SerializedFieldNames(Type type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var current = type;
                current != null && current != typeof(MonoBehaviour) &&
                    current != typeof(ScriptableObject) && current != typeof(object);
                current = current.BaseType)
            {
                var fields = current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (field.IsInitOnly || field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        continue;
                    }

                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    {
                        names.Add(field.Name);
                    }
                }
            }

            return names;
        }

        public sealed class SceneComponentBlock
        {
            public SceneComponentBlock(string scriptGuid, IReadOnlyList<string> propertyKeys, int lineNumber)
            {
                ScriptGuid = scriptGuid;
                PropertyKeys = propertyKeys;
                LineNumber = lineNumber;
            }

            public string ScriptGuid { get; }
            public IReadOnlyList<string> PropertyKeys { get; }
            public int LineNumber { get; }

            /// <summary>Keys Unity itself owns are not the subject; a stale binding is one of ours.</summary>
            public IEnumerable<string> AuthoredPropertyKeys =>
                PropertyKeys.Where(key => !key.StartsWith("m_", StringComparison.Ordinal));
        }
    }
}
