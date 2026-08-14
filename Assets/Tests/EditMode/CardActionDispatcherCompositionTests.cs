using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The surviving structural coverage from `CardActionStrategyRegistryTests`, which milestone
    /// 6.1h deleted along with the registry it existed to test.
    /// </summary>
    /// <remarks>
    /// Coverage enumeration, per the precedent set when 6.1c deleted a fixture. The old fixture had
    /// five tests. Three had no surviving subject: two exercised the registry's dispatch and
    /// composition validation, and one asserted the ordinal semantics of `CardActionStrategyId`,
    /// a type that no longer exists. A fourth proved each pass-through handler forwarded to its
    /// backend; the handlers are gone, and the intent - the dispatcher reaches the right backend -
    /// is covered end to end by `MatchSessionTests.Session_DispatchesEveryConfiguredLiveCommand
    /// FamilyThroughOnePort`, which drives every command family through the real authority. The
    /// fifth is preserved here, minus the assertions that named the deleted interfaces.
    /// </remarks>
    public sealed class CardActionDispatcherCompositionTests
    {
        [Test]
        public void ProductionComposition_UsesDistinctConcreteBackends()
        {
            var authorityType = typeof(CardActionResolutionAuthority);
            var coreAssembly = authorityType.Assembly;
            var movementBackendType = coreAssembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.MovementCardActionStrategyBackend",
                true);
            var attackBackendType = coreAssembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                true);
            var fastTravelBackendType = coreAssembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.FastTravelCardActionStrategyBackend",
                true);
            var clearBackendType = coreAssembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.ClearCardActionStrategyBackend",
                true);
            var authorityFields = authorityType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(new[]
            {
                movementBackendType, attackBackendType, fastTravelBackendType, clearBackendType
            }.Distinct().Count(), Is.EqualTo(4));
            Assert.That(movementBackendType.IsSealed, Is.True);
            Assert.That(attackBackendType.IsSealed, Is.True);
            Assert.That(fastTravelBackendType.IsSealed, Is.True);
            Assert.That(clearBackendType.IsSealed, Is.True);
            Assert.That(Array.Exists(authorityFields, field => field.FieldType == movementBackendType), Is.True);
            Assert.That(Array.Exists(authorityFields, field => field.FieldType == attackBackendType), Is.True);
            Assert.That(Array.Exists(authorityFields, field => field.FieldType == fastTravelBackendType), Is.True);
            Assert.That(Array.Exists(authorityFields, field => field.FieldType == clearBackendType), Is.True);
        }

        /// <summary>
        /// The dispatcher must hold its backends, not be one. Before 6.1h this was expressed as
        /// "the authority does not implement the three backend interfaces"; with those interfaces
        /// deleted, the same property is that no backend type is assignable from the authority.
        /// </summary>
        [Test]
        public void Dispatcher_IsNotItselfAStrategyBackend()
        {
            var authorityType = typeof(CardActionResolutionAuthority);
            var coreAssembly = authorityType.Assembly;

            foreach (var backendName in new[]
                     {
                         "MovementCardActionStrategyBackend",
                         "AttackDefenseCardActionStrategyBackend",
                         "FastTravelCardActionStrategyBackend",
                         "ClearCardActionStrategyBackend"
                     })
            {
                var backendType = coreAssembly.GetType(
                    $"GuardsDigital.Core.Rules.CardResolution.{backendName}",
                    true);
                Assert.That(backendType.IsAssignableFrom(authorityType), Is.False,
                    $"{authorityType.Name} must route to {backendName}, not become one.");
            }
        }
    }
}
