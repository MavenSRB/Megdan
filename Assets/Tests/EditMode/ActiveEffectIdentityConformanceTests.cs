using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8d acceptance check 4: no capability composes an Active-effect instance ID outside
    /// <see cref="ActiveEffectInstanceIdentity"/>.
    ///
    /// The forbidden member is the <see cref="ActiveEffectId"/> string constructor, because composing
    /// an ID is exactly one call to it and that call is an IL token a scan can see. The subjects are
    /// every capability handler's enclosing scope, so a composition hoisted out of the handler into
    /// its surrounding static class cannot hide. The snapshot serializer legitimately reconstructs
    /// IDs from persisted wire data; it hosts no capability handler, so it sits outside the subject
    /// set by construction rather than by allowlist.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class ActiveEffectIdentityConformanceTests
    {
        [Test]
        public void NoCapabilityHandler_ComposesAnActiveEffectIdOutsideTheSharedContract()
        {
            var subjects = CapabilityHandlerScopes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindIdentityViolations(subjects), Is.Empty,
                "A capability is composing an Active-effect instance ID itself. Per-capability " +
                "composition repeats on a replay, and the lifecycle refuses the duplicate including " +
                "a spent one, which is exactly how a card played a second time silently created " +
                "nothing.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindIdentityViolations(new[] { typeof(IdComposingSample) }), Is.Not.Empty,
                "A handler that constructs an ActiveEffectId must be reported, or the rule is inert.");

            Assert.That(FindIdentityViolations(new[] { typeof(ContractMintingSample) }), Is.Empty,
                "A handler that mints through the shared contract must not be reported, or the rule " +
                "bans the ordinary shape it exists to permit.");
        }

        /// <summary>
        /// The negative rule is satisfied by an engine that never mints identity at all, so the
        /// contract must be shown to construct the ID and both real creators to call the contract.
        /// </summary>
        [Test]
        public void TheSharedContract_IsWhereInstanceIdsAreActuallyComposed()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(ActiveEffectInstanceIdentity)),
                    ActiveEffectIdConstructor()),
                Is.True,
                "If the contract stopped constructing the ID, the negative rule would pass while " +
                "nothing minted identity at all.");

            foreach (var creator in KnownCreatorScopes())
            {
                Assert.That(
                    ILConformanceScanner.References(creator.Value, MintMethod()),
                    Is.True,
                    creator.Key + " must mint through the shared contract; a creator that stopped " +
                    "minting has lost per-activation identity.");
            }
        }

        [Test]
        public void SubjectDiscovery_SeesTheKnownCreators_AndTheForbiddenMemberResolves()
        {
            var subjects = CapabilityHandlerScopes();

            Assert.That(subjects.Any(type => type.Name == nameof(ActiveEffectCreationHandler)), Is.True,
                "Subject discovery no longer reaches the shared Active-effect creation handler - the " +
                "one milestone 8.9 lifted out of SyntheticModificationCapabilities and the one every " +
                "registered Active effect now runs through - so the rule could pass vacuously over an " +
                "empty scope.");
            Assert.That(subjects.Any(type => type.Name == "MarkActorHandler"), Is.True,
                "Subject discovery no longer reaches the MarkActor handler, so the rule could pass " +
                "vacuously over an empty scope.");

            Assert.That(ActiveEffectIdConstructor(), Is.Not.Null,
                "The forbidden member no longer resolves. Correct the rule rather than deleting it; " +
                "a rule that names nothing protects nothing.");
        }

        private static IReadOnlyList<string> FindIdentityViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                new MemberInfo[] { ActiveEffectIdConstructor() },
                "Active-effect instance-ID composition outside ActiveEffectInstanceIdentity");

        /// <summary>
        /// Every capability handler's enclosing scope in the Core namespace. The namespace filter
        /// keeps the harness's merged test assembly from sweeping this fixture's own samples into the
        /// real subject set.
        /// </summary>
        private static Type[] CapabilityHandlerScopes() =>
            ILConformanceScanner.TypesIn(typeof(IEffectCapabilityHandler).Assembly)
                .Where(type => !type.IsInterface &&
                    typeof(IEffectCapabilityHandler).IsAssignableFrom(type) &&
                    (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Core", StringComparison.Ordinal))
                .Select(type => type.DeclaringType ?? type)
                .Distinct()
                .SelectMany(ILConformanceScanner.WithNestedTypes)
                .Distinct()
                .ToArray();

        private static IReadOnlyDictionary<string, IEnumerable<Type>> KnownCreatorScopes() =>
            new Dictionary<string, IEnumerable<Type>>
            {
                // Milestone 8.9. SyntheticModificationCapabilities' five families and Wasp's two basic
                // cards create their effects through this one handler now, so it is the creator that
                // has to be shown minting; the file that used to hold a private copy of it declares
                // definitions and nothing else.
                [nameof(ActiveEffectCreationHandler)] =
                    ILConformanceScanner.WithNestedTypes(typeof(ActiveEffectCreationHandler)),
                ["SyntheticEffectCapabilities"] =
                    ILConformanceScanner.WithNestedTypes(typeof(SyntheticEffectCapabilities))
            };

        private static ConstructorInfo ActiveEffectIdConstructor() =>
            ILConformanceScanner.Constructor(typeof(ActiveEffectId), typeof(string));

        private static MethodInfo MintMethod() =>
            ILConformanceScanner.Method(
                typeof(ActiveEffectInstanceIdentity), nameof(ActiveEffectInstanceIdentity.Mint));

        /// <summary>Deliberate violation: composes an instance ID inside the capability.</summary>
        private sealed class IdComposingSample
        {
            public ActiveEffectId Execute(EffectCapabilityContext context) =>
                new ActiveEffectId(
                    context.SourceCardInstanceId.Value + ":" + context.NodeId.Value);
        }

        /// <summary>Compliant twin: asks the shared contract for this activation's identity.</summary>
        private sealed class ContractMintingSample
        {
            public ActiveEffectId Execute(
                EffectCapabilityContext context, ActiveEffectDefinition definition) =>
                ActiveEffectInstanceIdentity.Mint(context, definition);
        }
    }
}
