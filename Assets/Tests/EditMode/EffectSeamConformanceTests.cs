using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a acceptance check 4: architecture tests mechanically prevent effect-node switches
    /// from accumulating in the outer session, round, and turn dispatcher seams.
    ///
    /// The rule is expressed as members rather than types on purpose. An enum comparison compiles to a
    /// bare integer load and leaves no metadata token, so forbidding <c>EffectNodeKind</c> as a type
    /// would scan clean while a switch on it sat in plain sight. Reading a node's kind requires calling
    /// the property getter, and that call is a token - the same reasoning milestone 6.1ap's checkpoint
    /// rule rests on.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class EffectSeamConformanceTests
    {
        [Test]
        public void OuterDispatcherSeams_NeitherInspectEffectNodesNorInvokeCapabilityHandlers()
        {
            var subjects = SeamTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(
                FindEffectSeamViolations(subjects),
                Is.Empty,
                "An outer dispatcher seam is reading an effect node or invoking a capability handler. " +
                "Only the effect resolver may do either; the seams route and materialise.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindEffectSeamViolations(new[] { typeof(NodeInspectingSample) }),
                Is.Not.Empty,
                "A type that branches on an effect node's kind must be reported, or the rule is inert.");

            Assert.That(
                FindEffectSeamViolations(new[] { typeof(FrameForwardingSample) }),
                Is.Empty,
                "A type that forwards an effect frame without inspecting it must not be reported, or " +
                "the rule bans the ordinary shape it exists to permit.");
        }

        /// <summary>
        /// The negative rule is satisfied by an engine with no effect execution at all, so the resolver
        /// must be shown to still do the work the seams are forbidden from doing.
        /// </summary>
        [Test]
        public void TheResolver_IsWhereEffectNodesAreActuallyInspected()
        {
            var resolver = typeof(GuardsDigital.Core.Rules.EffectResolution.EffectResolutionSession);

            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(resolver), NodeKindGetter()),
                Is.True,
                "If the resolver stopped reading node kinds, the seams would pass this rule while the " +
                "effect frame did nothing.");
        }

        /// <summary>
        /// Milestone 8.3, closing the 7.9a review's F-CARD-05: no production type may conjure the
        /// content catalogs a match runs on. It takes what it is given.
        ///
        /// Eleven production sites used to mint a synthetic catalog whenever a caller passed none -
        /// five inside the card-action dispatcher, two inside the snapshot write path, two inside the
        /// read path, and the component-catalog twins beside them - and the serializer's comment
        /// claimed "the configured session always supplies the real ones" while <c>MatchAggregate</c>
        /// supplied neither. Each was a place a verified capability would have to be wired in by hand,
        /// and none of them would fail if it were missed.
        ///
        /// The rule names members rather than types, for the reason the effect-seam rule above does: a
        /// factory call leaves a metadata token, and a token is what a scan can see. Two types are
        /// excluded by name with their reason, which is the FrameShapeStubs precedent - an exclusion
        /// list, not an escape hatch:
        ///
        /// <list type="bullet">
        /// <item><c>MatchContentCatalogs</c> - the one declared origin. Its own factory is what every
        /// other site is required to call instead of naming synthetic content.</item>
        /// <item><c>SyntheticEffectCapabilities</c> and <c>SyntheticComponentDefinitions</c> - the
        /// registered content itself. A catalog's own factory building a catalog is what a catalog is;
        /// what the rule is about is every other type naming it.</item>
        /// <item><c>MatchConfiguration</c> - the configured match's content declaration, which states
        /// once what its cards need. Milestone 8.3 made it the only production type besides the
        /// catalog itself that may ask the build what it registers, so the exclusion is that
        /// declaration rather than a test-only allowance; milestone 8.25b's rename left it
        /// unchanged.</item>
        /// </list>
        /// </summary>
        [Test]
        public void NoProductionCompositionSite_NamesACapabilityCatalogItWasNotGiven()
        {
            var audited = CatalogAuditedTypes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(audited), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(
                FindCatalogMintingViolations(audited),
                Is.Empty,
                "A production type is building or naming a content catalog instead of taking the one " +
                "it was composed with. Only MatchContentCatalogs declares what this build registers.");
        }

        [Test]
        public void TheCatalogRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindCatalogMintingViolations(new[] { typeof(CatalogMintingSample) }),
                Is.Not.Empty,
                "A type that asks the build for a catalog rather than taking one must be reported, or " +
                "the rule is inert.");

            Assert.That(
                FindCatalogMintingViolations(new[] { typeof(CatalogTakingSample) }),
                Is.Empty,
                "A type that uses the catalogs it was handed must not be reported, or the rule bans " +
                "the ordinary shape it exists to permit.");
        }

        /// <summary>
        /// The negative rule is satisfied by a build that registers nothing, so the declared origin must
        /// be shown to still be the thing that names the content.
        /// </summary>
        [Test]
        public void TheDeclaredOrigin_IsWhereThisBuildsCatalogsAreActuallyNamed()
        {
            Assert.That(
                ILConformanceScanner.References(
                    new[] { typeof(MatchContentCatalogs) }, SyntheticCapabilityFactory()),
                Is.True,
                "If MatchContentCatalogs stopped naming the registered content, every other type would " +
                "pass this rule while the build registered nothing.");
        }

        private static IReadOnlyList<string> FindCatalogMintingViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                ForbiddenCatalogMembers(),
                "a content catalog conjured rather than taken as a required argument");

        private static IEnumerable<MemberInfo> ForbiddenCatalogMembers()
        {
            var members = new MemberInfo[]
                {
                    SyntheticCapabilityFactory(),
                    ILConformanceScanner.Method(
                        typeof(GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions),
                        nameof(GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions.CreateCatalog)),
                    // Asking the build what it registers is the declared origin's job. Without this
                    // member the rule would be evaded by calling ForThisBuild() from a dispatcher -
                    // the same defect wearing the new type's name.
                    ILConformanceScanner.Method(
                        typeof(MatchContentCatalogs), nameof(MatchContentCatalogs.ForThisBuild)),
                    ILConformanceScanner.Constructor(typeof(EffectCapabilityCatalog))
                }
                .Where(member => member != null)
                .ToArray();

            Assert.That(members, Has.Length.EqualTo(4),
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
            return members;
        }

        private static MethodInfo SyntheticCapabilityFactory() =>
            ILConformanceScanner.Method(
                typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticEffectCapabilities),
                nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticEffectCapabilities.CreateCatalog));

        /// <summary>
        /// Every production Core and Application type except the two declared by name above, so a
        /// composition site written after this milestone is audited by existing rather than by being
        /// added to a list. The namespace filter matters in the cross-platform harness, which compiles
        /// Core, Application and these fixtures into one assembly and would otherwise hand this rule
        /// its own deliberate violation sample.
        /// </summary>
        private static IReadOnlyList<Type> CatalogAuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(EffectCapabilityCatalog).Assembly)
                .Concat(ILConformanceScanner.TypesIn(typeof(MatchSession).Assembly))
                .Distinct()
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital", StringComparison.Ordinal) &&
                    !(type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Tests", StringComparison.Ordinal))
                .Where(type =>
                    type != typeof(MatchContentCatalogs) &&
                    type != typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticEffectCapabilities) &&
                    type != typeof(GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions) &&
                    type != typeof(GuardsDigital.Core.Participants.MatchConfiguration))
                .ToArray();

        /// <summary>Deliberate violation: asks the build for content instead of being handed it.</summary>
        private sealed class CatalogMintingSample
        {
            public EffectCapabilityCatalog Capabilities() =>
                MatchContentCatalogs.ForThisBuild().Capabilities;
        }

        /// <summary>Compliant twin: uses the catalogs its composition supplied.</summary>
        private sealed class CatalogTakingSample
        {
            public CatalogTakingSample(MatchContentCatalogs catalogs)
            {
                Catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
            }

            public MatchContentCatalogs Catalogs { get; }

            public EffectCapabilityCatalog Capabilities() => Catalogs.Capabilities;
        }

        private static IReadOnlyList<string> FindEffectSeamViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                ForbiddenEffectMembers(),
                "effect-node inspection or capability invocation inside an outer dispatcher seam");

        /// <summary>
        /// The four seams milestone 6.2a's acceptance check names. <c>MatchSession</c> is included here
        /// rather than in the dispatcher-algorithm rule, which it cannot join: it legitimately calls
        /// <c>DefenseValueQuery.Calculate</c> to build the projected Defense preview that milestone 5.8b
        /// deliberately moved off presentation.
        /// </summary>
        private static IEnumerable<Type> SeamTypes() =>
            ILConformanceScanner.WithNestedTypes(typeof(MatchSession))
                .Concat(ILConformanceScanner.WithNestedTypes(typeof(RoundResolutionAuthority)))
                .Concat(ILConformanceScanner.WithNestedTypes(typeof(TurnResolutionAuthority)))
                .Concat(ILConformanceScanner.WithNestedTypes(typeof(CardActionResolutionAuthority)));

        private static IEnumerable<MemberInfo> ForbiddenEffectMembers()
        {
            var members = new MemberInfo[]
                {
                    NodeKindGetter(),
                    Getter(typeof(EffectNodeDefinition), nameof(EffectNodeDefinition.FailurePolicy)),
                    Getter(typeof(EffectNodeDefinition), nameof(EffectNodeDefinition.ChildNodeIds)),
                    ILConformanceScanner.Method(
                        typeof(EffectCapabilityCatalog), nameof(EffectCapabilityCatalog.Load)),
                    ILConformanceScanner.Method(
                        typeof(IEffectCapabilityHandler), nameof(IEffectCapabilityHandler.Execute)),
                    ILConformanceScanner.Method(
                        typeof(IEffectCapabilityHandler), nameof(IEffectCapabilityHandler.Resume)),
                    // Milestone 6.5ac's probe joins the same rule as the two it sits beside. Asking a
                    // capability what it could do is still asking a capability, and the seams route
                    // and materialise rather than judge.
                    ILConformanceScanner.Method(
                        typeof(IEffectCapabilityHandler), nameof(IEffectCapabilityHandler.Anticipate))
                }
                .Where(member => member != null)
                .ToArray();

            Assert.That(members, Has.Length.EqualTo(7),
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
            return members;
        }

        private static MethodInfo NodeKindGetter() =>
            Getter(typeof(EffectNodeDefinition), nameof(EffectNodeDefinition.Kind));

        private static MethodInfo Getter(Type type, string propertyName) =>
            type.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?.GetGetMethod(true);

        /// <summary>Deliberate violation: decides something by reading an effect node.</summary>
        private sealed class NodeInspectingSample
        {
            public bool IsSequence(EffectNodeDefinition node) =>
                node.Kind == EffectNodeKind.Sequence;
        }

        /// <summary>Compliant twin: carries the frame through without asking what is in it.</summary>
        private sealed class FrameForwardingSample
        {
            public IReadOnlyList<EffectNodeDefinition> Forward(
                IEnumerable<EffectNodeDefinition> nodes)
            {
                var forwarded = new List<EffectNodeDefinition>();
                foreach (var node in nodes)
                {
                    forwarded.Add(node);
                }

                return forwarded;
            }
        }
    }
}
