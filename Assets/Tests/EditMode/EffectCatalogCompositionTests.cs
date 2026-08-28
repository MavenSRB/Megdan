using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e7. Generic effect handlers may implement a verified declaration, but the
    /// generic catalog layer may not decide which hero declarations this build owns. That dependency
    /// points from build composition to both sides, never from Core.Effects back into verified cards.
    /// </summary>
    public sealed class EffectCatalogCompositionTests
    {
        [Test]
        public void CoreEffectsDoesNotReferenceVerifiedCardContent()
        {
            var subject = ILConformanceScanner.TypesIn(typeof(SyntheticEffectCapabilities).Assembly)
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                    "GuardsDigital.Core.Effects",
                    StringComparison.Ordinal))
                .ToArray();
            var forbidden = VerifiedContentTypes()
                .SelectMany(type => new MemberInfo[] { type }.Concat(type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
                .ToArray();

            Assert.That(forbidden, Is.Not.Empty,
                "The compiled verified-content member set must be nonempty, or this dependency scan " +
                "would prove nothing.");
            Assert.That(ILConformanceScanner.UnresolvedTokens(subject), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a verified-content " +
                "dependency could hide behind one.");

            var violations = ILConformanceScanner.FindForbiddenReferences(
                subject,
                forbidden,
                "generic Core.Effects code references verified hero content");
            Assert.That(violations, Is.Empty,
                "Verified handler declarations are supplied by MatchContentCatalogs; generic effect " +
                "code implements contracts and never imports a hero:\n" +
                string.Join("\n", violations));
        }

        [Test]
        public void BuildCompositionPreservesOneOrderedCatalogAndExactCapabilityIdentity()
        {
            var generic = SyntheticEffectCapabilities.CreateCatalog();
            var verified = VerifiedHandlers().ToArray();
            var expected = generic.Registered
                .Concat(verified.Select(handler => handler.Reference))
                .OrderBy(reference => reference.CapabilityId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray();
            var first = MatchContentCatalogs.ForThisBuild().Capabilities;
            var second = MatchContentCatalogs.ForThisBuild().Capabilities;

            Assert.That(first.Registered, Is.EqualTo(expected),
                "The build catalog is exactly the generic implementation set plus its verified " +
                "content declarations; no second registry or discovery set exists.");
            Assert.That(second.Registered, Is.EqualTo(first.Registered),
                "Registration order is deterministic across fresh build compositions.");
            foreach (var handler in verified)
            {
                Assert.That(first.Load(handler.Reference).GetType(), Is.EqualTo(handler.GetType()),
                    $"Capability identity '{handler.Reference}' must still load its declared handler.");
            }

            var missing = Assert.Throws<EffectCapabilityLoadException>(() => first.Load(
                new EffectCapabilityReference(
                    new EffectCapabilityId("not-registered-by-this-build"), 1)));
            Assert.That(missing.Reason,
                Is.EqualTo(EffectCapabilityLoadRejectionReason.MissingCapability));
        }

        private static IReadOnlyList<Type> VerifiedContentTypes() =>
            typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type => string.Equals(
                    type.Namespace,
                    typeof(WaspCardContent).Namespace,
                    StringComparison.Ordinal))
                .ToArray();

        private static IEnumerable<IEffectCapabilityHandler> VerifiedHandlers() =>
            WaspActiveEffectContent.Handlers()
                .Concat(ArienActiveEffectContent.Handlers())
                .Concat(BroganActiveEffectContent.Handlers())
                .Concat(TigerclawActiveEffectContent.Handlers())
                .Concat(TigerclawComponentContent.Handlers())
                .Concat(XargathaActiveEffectContent.Handlers());
    }
}
