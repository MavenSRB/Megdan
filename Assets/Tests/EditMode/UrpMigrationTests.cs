using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class UrpMigrationTests
    {
        private const string PipelinePath = "Assets/Settings/GuardsUniversalRenderPipeline.asset";

        [Test]
        public void ProjectAndEveryQualityLevelUseTheCheckedInUrpAsset()
        {
            var expected = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            Assert.That(expected, Is.Not.Null, "The checked-in URP asset must exist.");
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(expected));

            var originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (var index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    Assert.That(QualitySettings.renderPipeline, Is.SameAs(expected),
                        $"Quality level '{QualitySettings.names[index]}' falls back outside URP.");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        [Test]
        public void RetainedAuthoredAndRuntimeMaterialsUseOnlyUrpShaders()
        {
            var authored = new[]
            {
                "Assets/GuardsDigital/Presentation/Board/Placeholder/HexFillPlaceholder.mat",
                "Assets/GuardsDigital/Presentation/Board/Placeholder/HexEdgePlaceholder.mat"
            }.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();

            Assert.That(authored, Has.All.Not.Null);
            Assert.That(authored.Select(material => material.shader.name),
                Is.All.StartsWith("Universal Render Pipeline/"));
            Assert.That(Shader.Find("Universal Render Pipeline/Lit"), Is.Not.Null);
            Assert.That(Shader.Find("Universal Render Pipeline/Unlit"), Is.Not.Null);
        }
    }
}
