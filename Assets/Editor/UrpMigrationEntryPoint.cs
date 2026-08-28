using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuardsDigital.EditorTools
{
    /// <summary>
    /// Creates the checked-in URP baseline through Unity's serialization APIs. The method is
    /// intentionally idempotent so a clean checkout can reproduce the asset relationship without
    /// hand-authoring Unity YAML.
    /// </summary>
    public static class UrpMigrationEntryPoint
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string RendererPath = SettingsFolder + "/GuardsUniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/GuardsUniversalRenderPipeline.asset";

        public static void CreateBaseline()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(SettingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }

                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    renderer.name = "Guards Universal Renderer";
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }

                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    pipeline.name = "Guards Universal Render Pipeline";
                    pipeline.supportsHDR = true;
                    pipeline.msaaSampleCount = 4;
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;
                for (var index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    QualitySettings.renderPipeline = pipeline;
                }

                ConvertAuthoredMaterial(
                    "Assets/GuardsDigital/Presentation/Board/Placeholder/HexFillPlaceholder.mat",
                    "Universal Render Pipeline/Lit");
                ConvertAuthoredMaterial(
                    "Assets/GuardsDigital/Presentation/Board/Placeholder/HexEdgePlaceholder.mat",
                    "Universal Render Pipeline/Unlit");
                ConfigureAlwaysIncludedShaders();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("GUARDS_URP_MIGRATION: PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ConvertAuthoredMaterial(string path, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path) ??
                throw new InvalidOperationException($"Required authored material is missing: {path}");
            var shader = Shader.Find(shaderName) ??
                throw new InvalidOperationException($"Required URP shader does not resolve: {shaderName}");
            var color = material.color;
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureAlwaysIncludedShaders()
        {
            var settingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settingsObjects.Length == 0)
            {
                throw new InvalidOperationException("Graphics settings could not be loaded.");
            }

            var shaders = new[]
            {
                Shader.Find("Universal Render Pipeline/Lit"),
                Shader.Find("Universal Render Pipeline/Unlit")
            };
            if (Array.Exists(shaders, shader => shader == null))
            {
                throw new InvalidOperationException("A required runtime URP shader does not resolve.");
            }

            var serialized = new SerializedObject(settingsObjects[0]);
            var alwaysIncluded = serialized.FindProperty("m_AlwaysIncludedShaders") ??
                throw new InvalidOperationException("Graphics settings have no always-included shader list.");
            alwaysIncluded.arraySize = shaders.Length;
            for (var index = 0; index < shaders.Length; index++)
            {
                alwaysIncluded.GetArrayElementAtIndex(index).objectReferenceValue = shaders[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
