using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ApplicationArchitectureBoundaryTests
    {
        [Test]
        public void ApplicationAssembly_HasNoUnityReferenceAndPolicyRejectsForbiddenExamples()
        {
            var assembly = typeof(MatchSession).Assembly;
            var actualReferences = assembly.GetReferencedAssemblies().Select(value => value.Name).ToArray();

            Assert.That(ApplicationAssemblyDependencyPolicy.FindForbiddenReferences(actualReferences), Is.Empty);
            Assert.That(actualReferences, Does.Contain("GuardsDigital.Core"));
            Assert.That(ApplicationAssemblyDependencyPolicy.FindForbiddenReferences(new[]
            {
                "System",
                "GuardsDigital.Core"
            }), Is.Empty);
            Assert.That(ApplicationAssemblyDependencyPolicy.FindForbiddenReferences(new[]
            {
                "UnityEngine.CoreModule",
                "UnityEditor.CoreModule"
            }), Is.EqualTo(new[] { "UnityEditor.CoreModule", "UnityEngine.CoreModule" }));
        }

        [Test]
        public void EditorScripts_CompileIntoAnEditorOnlyAssemblyThatCannotReachTheSessionPort()
        {
            // Assets/Editor had no assembly definition before milestone 6.1d, so its scripts landed
            // in the catch-all Assembly-CSharp-Editor, which auto-references every other assembly.
            // A loose Editor script could therefore construct a MatchSession and bypass IMatchSession
            // entirely, and nothing would have reported it.
            var editorAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetTypes()
                    .Any(type => type.FullName == "GuardsDigital.EditorTools.CurrentPrototypeLauncher"));

            Assert.That(editorAssembly, Is.Not.Null,
                "The Editor tools assembly was not found; this check would otherwise pass vacuously.");
            Assert.That(editorAssembly.GetName().Name, Is.EqualTo("GuardsDigital.EditorTools"),
                "Editor scripts must compile into their own Editor-only assembly definition, not into " +
                "the catch-all Assembly-CSharp-Editor that references everything.");

            var references = editorAssembly.GetReferencedAssemblies().Select(value => value.Name).ToArray();
            Assert.That(references, Does.Not.Contain("GuardsDigital.Application"),
                "The Editor tools assembly must not reference the application layer; an Editor script " +
                "that can reach MatchSession directly can bypass the IMatchSession port.");
        }

        [Test]
        public void MatchSessionPortAndProjection_ExposeNoUnityTypesAndRemainImmutable()
        {
            var publicTypes = typeof(IMatchSession).Assembly.GetExportedTypes();
            var publicSignatures = publicTypes
                .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .SelectMany(GetSignatureTypes)
                .Where(type => type != null)
                .ToArray();

            Assert.That(publicSignatures.Any(type =>
                type.Assembly.GetName().Name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
            Assert.That(typeof(MatchSessionProjection).GetProperties(BindingFlags.Public | BindingFlags.Instance),
                Has.All.Matches<PropertyInfo>(property => property.SetMethod == null));
        }

        [Test]
        public void ConfiguredPresentation_OwnsOnlyTheSessionPortForMatchOrchestrationState()
        {
            var fields = typeof(ConfiguredMatchSceneRuntime)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.That(fields.Single(field => field.Name == "session").FieldType,
                Is.EqualTo(typeof(IMatchSession)));
            Assert.That(fields.Any(field => field.FieldType == typeof(RoundResolutionAuthority)), Is.False);
            Assert.That(fields.Any(field => field.FieldType == typeof(DirectorCommandHistory)), Is.False);
            Assert.That(fields.Any(field => field.FieldType == typeof(RuleEventLogProjection)), Is.False);
        }

        [Test]
        public void FeaturePresentersAndPassiveViews_KeepRulesAndUnityOnOppositeSidesOfPorts()
        {
            var presenterTypes = new[]
            {
                typeof(BoardInteractionPresenter),
                typeof(DirectorHandsPresenter),
                typeof(RoundActionPresenter),
                typeof(UndoRestartPresenter),
                typeof(DiagnosticLogPresenter)
            };
            Assert.That(presenterTypes, Has.All.Matches<Type>(type =>
                !typeof(MonoBehaviour).IsAssignableFrom(type)));
            Assert.That(presenterTypes.SelectMany(type =>
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                .Any(field => field.FieldType.Name.EndsWith("Presenter", StringComparison.Ordinal)), Is.False);

            var passiveViewTypes = new[]
            {
                typeof(BoardInteractionUnityView),
                typeof(DirectorHandsUnityView),
                typeof(RoundActionUnityView),
                typeof(UndoRestartUnityView),
                typeof(DiagnosticLogUnityView),
                typeof(ConfiguredMatchHudRenderer)
            };
            var viewFields = passiveViewTypes.SelectMany(type =>
                type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.That(viewFields.Any(field => field.FieldType == typeof(IMatchSession)), Is.False);
            Assert.That(viewFields.Any(field => field.FieldType == typeof(RoundResolutionAuthority)), Is.False);
            Assert.That(viewFields.Any(field => field.FieldType == typeof(DirectorCommandHistory)), Is.False);
        }

        private static Type[] GetSignatureTypes(MemberInfo member)
        {
            if (member is MethodInfo method)
            {
                return method.GetParameters().Select(parameter => parameter.ParameterType)
                    .Concat(new[] { method.ReturnType })
                    .ToArray();
            }

            if (member is PropertyInfo property)
            {
                return new[] { property.PropertyType };
            }

            if (member is FieldInfo field)
            {
                return new[] { field.FieldType };
            }

            if (member is ConstructorInfo constructor)
            {
                return constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            }

            return Array.Empty<Type>();
        }
    }
}
