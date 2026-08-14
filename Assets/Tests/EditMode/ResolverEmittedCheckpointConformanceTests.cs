using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1ap, finding <c>F-ER2-05</c>. Protected: a timing checkpoint is raised by the
    /// resolution step that causes it, not inferred afterwards from what a resolver happened to emit.
    ///
    /// Before this milestone <c>TurnResolutionAuthority.ApplyActionTiming</c> took the event list a
    /// card action had already produced, read each entry's <see cref="CardActionResolutionEventKind"/>,
    /// and spliced checkpoints around it. The turn dispatcher therefore decided timing on behalf of
    /// resolvers that had already finished acting. An ordered-step effect engine cannot work that way:
    /// an effect node's checkpoints come from its capability contract and are not recoverable from an
    /// event kind.
    ///
    /// The rule is expressed as "the turn authority does not read a card-action fact's kind" rather
    /// than "the turn authority does not reference the enum", because an enum comparison compiles to a
    /// bare integer load and leaves no metadata token to scan. Reading the fact requires calling the
    /// property getter, and that call is a token.
    ///
    /// Unity-independent by construction, so it also runs in the cross-platform dotnet harness. The
    /// architecture fixture that owns the Unity-facing rules cannot make that claim.
    /// </summary>
    public sealed class ResolverEmittedCheckpointConformanceTests
    {
        [Test]
        public void TurnAuthority_RaisesNoCheckpointByInspectingAProducedCardActionFact()
        {
            var subjects = ILConformanceScanner.WithNestedTypes(typeof(TurnResolutionAuthority)).ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one. Fix the scan rather than trusting an incomplete pass.");

            Assert.That(
                FindFactKindInspectionViolations(subjects),
                Is.Empty,
                "The turn authority reads a produced card-action fact's kind, which is how the deleted " +
                "ApplyActionTiming rewriter decided which checkpoints to splice in. Emit the checkpoint " +
                "from the resolver step that raises it instead of inferring it here.");
        }

        /// <summary>
        /// The negative rule alone is satisfied by emitting no checkpoints at all, so the subjects must
        /// be shown to reference the resolved member the rule is protecting.
        /// </summary>
        [Test]
        public void ConformanceRule_NamesAMemberThatResolvesSoItCannotPassVacuously()
        {
            var forbidden = FactKindReaders();
            Assert.That(forbidden, Is.Not.Empty,
                "No card-action fact-kind reader resolved, so this rule would be blind. If the property " +
                "was renamed, rename it here too rather than leaving the rule pointed at nothing.");

            Assert.That(
                ILConformanceScanner.References(new[] { typeof(FactKindInspectingSample) }, forbidden[0]),
                Is.True,
                "The deliberate-violation sample no longer reaches the member this rule forbids, so a " +
                "passing run would prove nothing about production code.");
        }

        /// <summary>
        /// The positive half. Deleting every checkpoint would satisfy the negative rule perfectly, so
        /// each resolver backend must be shown to still raise them itself. The backends are internal
        /// and this repository does not open Core internals to its test assemblies, so they are
        /// resolved by name from the assembly that declares the dispatcher.
        /// </summary>
        [Test]
        public void EveryCardActionBackend_RaisesItsOwnTimingCheckpoints()
        {
            var emitter = ILConformanceScanner.Method(
                typeof(ResolutionStep), nameof(ResolutionStep.AtCheckpoint));

            foreach (var backend in ProductionBackendTypes())
            {
                Assert.That(
                    ILConformanceScanner.References(
                        ILConformanceScanner.WithNestedTypes(backend), emitter),
                    Is.True,
                    $"{backend.Name} raises no timing checkpoint of its own. Checkpoints must come from " +
                    "the resolver step that causes them; if this backend genuinely raises none, the " +
                    "turn authority is inferring them again somewhere.");
            }
        }

        private static IReadOnlyList<Type> ProductionBackendTypes()
        {
            var assembly = typeof(CardActionResolutionAuthority).Assembly;
            var backends = new[]
                {
                    "GuardsDigital.Core.Rules.CardResolution.MovementCardActionStrategyBackend",
                    "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                    "GuardsDigital.Core.Rules.CardResolution.FastTravelCardActionStrategyBackend",
                    "GuardsDigital.Core.Rules.CardResolution.ClearCardActionStrategyBackend"
                }
                .Select(name => assembly.GetType(name))
                .ToArray();

            Assert.That(backends, Has.None.Null,
                "A named card-action backend no longer resolves. Correct the rule rather than deleting " +
                "it; a rule that names nothing protects nothing.");
            return backends;
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindFactKindInspectionViolations(new[] { typeof(FactKindInspectingSample) }),
                Is.Not.Empty,
                "A type that branches on a produced fact's kind must be reported, or the rule is inert.");

            Assert.That(
                FindFactKindInspectionViolations(new[] { typeof(FactForwardingSample) }),
                Is.Empty,
                "A type that forwards facts without reading their kind must not be reported, or the " +
                "rule bans the ordinary shape it is meant to permit.");
        }

        private static IReadOnlyList<string> FindFactKindInspectionViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                FactKindReaders(),
                "timing inferred by inspecting a produced card-action fact instead of emitted by the resolver step");

        private static IReadOnlyList<MemberInfo> FactKindReaders()
        {
            var getter = typeof(CardActionResolutionEvent)
                .GetProperty(nameof(CardActionResolutionEvent.Kind),
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?.GetGetMethod(true);

            return getter == null
                ? Array.Empty<MemberInfo>()
                : new MemberInfo[] { getter };
        }

        /// <summary>Deliberate violation: decides something by reading a produced fact's kind.</summary>
        private sealed class FactKindInspectingSample
        {
            public int CountCompletions(IEnumerable<CardActionResolutionEvent> facts)
            {
                var count = 0;
                foreach (var fact in facts)
                {
                    if (fact.Kind == CardActionResolutionEventKind.MovementActionCompleted)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Compliant twin: carries facts through without asking what kind they are.</summary>
        private sealed class FactForwardingSample
        {
            public IReadOnlyList<CardActionResolutionEvent> Forward(
                IEnumerable<CardActionResolutionEvent> facts)
            {
                var forwarded = new List<CardActionResolutionEvent>();
                foreach (var fact in facts)
                {
                    forwarded.Add(fact);
                }

                return forwarded;
            }
        }
    }
}
