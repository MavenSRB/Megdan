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
                        typeof(IEffectCapabilityHandler), nameof(IEffectCapabilityHandler.Resume))
                }
                .Where(member => member != null)
                .ToArray();

            Assert.That(members, Has.Length.EqualTo(6),
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
