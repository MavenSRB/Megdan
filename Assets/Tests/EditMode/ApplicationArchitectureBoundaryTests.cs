using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;

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
            var fields = typeof(HeroSelectionController)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.That(fields.Single(field => field.Name == "matchSession").FieldType,
                Is.EqualTo(typeof(IMatchSession)));
            Assert.That(fields.Any(field => field.FieldType == typeof(RoundResolutionAuthority)), Is.False);
            Assert.That(fields.Any(field => field.FieldType == typeof(DirectorCommandHistory)), Is.False);
            Assert.That(fields.Any(field => field.FieldType == typeof(RuleEventLogProjection)), Is.False);
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
