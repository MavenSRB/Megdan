using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8h acceptance check 4: no capability composes a marker instance ID outside
    /// <see cref="MarkerInstanceIdentity"/>.
    ///
    /// The marker counterpart of <c>ActiveEffectIdentityConformanceTests</c>, and deliberately its own
    /// fixture rather than a second case there: the two rules name different forbidden members and
    /// different minting contracts, and a fixture that failed would otherwise not say which family
    /// lost its identity. The forbidden member is the <see cref="MarkerInstanceId"/> string
    /// constructor, because composing an ID is exactly one call to it. The subjects are every
    /// capability handler's enclosing scope, so a composition hoisted out of the handler into its
    /// surrounding static class cannot hide. The snapshot serializer legitimately reconstructs IDs
    /// from persisted wire data; it hosts no capability handler, so it sits outside the subject set by
    /// construction rather than by allowlist.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class MarkerIdentityConformanceTests
    {
        [Test]
        public void NoCapabilityHandler_ComposesAMarkerInstanceIdOutsideTheSharedContract()
        {
            var subjects = CapabilityHandlerScopes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindIdentityViolations(subjects), Is.Empty,
                "A capability is composing a marker instance ID itself. Per-capability composition " +
                "carries no activation anchor, so a marker given, returned, and given again by a " +
                "replayed card takes the identity the first one already used.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindIdentityViolations(new[] { typeof(IdComposingSample) }), Is.Not.Empty,
                "A handler that constructs a MarkerInstanceId must be reported, or the rule is inert.");

            Assert.That(FindIdentityViolations(new[] { typeof(ContractMintingSample) }), Is.Empty,
                "A handler that mints through the shared contract must not be reported, or the rule " +
                "bans the ordinary shape it exists to permit.");
        }

        /// <summary>
        /// The negative rule is satisfied by an engine that never mints identity at all, so the
        /// contract must be shown to construct the ID and the real creator to call the contract.
        /// </summary>
        [Test]
        public void TheSharedContract_IsWhereInstanceIdsAreActuallyComposed()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(MarkerInstanceIdentity)),
                    MarkerInstanceIdConstructor()),
                Is.True,
                "If the contract stopped constructing the ID, the negative rule would pass while " +
                "nothing minted identity at all.");

            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(SyntheticComponentCapabilities)),
                    MintMethod()),
                Is.True,
                "The marker capability must mint through the shared contract; a creator that stopped " +
                "minting has lost per-activation identity.");
        }

        [Test]
        public void SubjectDiscovery_SeesTheKnownCreator_AndTheForbiddenMemberResolves()
        {
            var subjects = CapabilityHandlerScopes();

            // Milestone 8.23 renamed it. The handler became shared and parameterised by the marker it
            // gives - Tigerclaw's two Poison strengths construct one each, exactly as every hero's
            // Active effects construct an ActiveEffectCreationHandler - so what discovery must still
            // reach is that one type rather than the synthetic-only class it replaced.
            Assert.That(subjects.Any(type => type.Name == "MarkerGivingHandler"), Is.True,
                "Subject discovery no longer reaches the give-marker handler, so the rule could pass " +
                "vacuously over an empty scope.");

            Assert.That(MarkerInstanceIdConstructor(), Is.Not.Null,
                "The forbidden member no longer resolves. Correct the rule rather than deleting it; " +
                "a rule that names nothing protects nothing.");
        }

        private static IReadOnlyList<string> FindIdentityViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                new MemberInfo[] { MarkerInstanceIdConstructor() },
                "Marker instance-ID composition outside MarkerInstanceIdentity");

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

        private static ConstructorInfo MarkerInstanceIdConstructor() =>
            ILConformanceScanner.Constructor(typeof(MarkerInstanceId), typeof(string));

        private static MethodInfo MintMethod() =>
            ILConformanceScanner.Method(
                typeof(MarkerInstanceIdentity), nameof(MarkerInstanceIdentity.Mint),
                typeof(EffectCapabilityContext), typeof(HeroId));

        /// <summary>Deliberate violation: composes an instance ID inside the capability.</summary>
        private sealed class IdComposingSample
        {
            public MarkerInstanceId Execute(EffectCapabilityContext context, HeroId holder) =>
                new MarkerInstanceId(
                    context.SourceCardInstanceId.Value + ":" + context.NodeId.Value + ":" + holder.Value);
        }

        /// <summary>Compliant twin: asks the shared contract for this activation's identity.</summary>
        private sealed class ContractMintingSample
        {
            public MarkerInstanceId Execute(EffectCapabilityContext context, HeroId holder) =>
                MarkerInstanceIdentity.Mint(context, holder);
        }
    }
}
