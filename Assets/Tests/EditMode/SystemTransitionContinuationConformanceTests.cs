using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Transitions.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.10a's fourth acceptance check: no transition holds handler-local continuation state.
    ///
    /// This is the rule the whole contract rests on, and it is worth stating why it needs to be a rule
    /// rather than a review note. Holding "where am I up to" in a field, a captured local, or a callback
    /// is the obvious way to write a multi-step transition, it compiles, and it works perfectly right up
    /// until the match is snapshotted, replayed, or Undone - at which point the transition silently
    /// resumes from wherever the rebuilt object happens to start. Nothing about that failure is visible
    /// at the call site, so the check is structural: a type that both drives a transition and keeps step
    /// state of its own is reported, whoever writes it and whenever.
    ///
    /// Unity-independent.
    /// </summary>
    public sealed class SystemTransitionContinuationConformanceTests
    {
        [Test]
        public void NoTransitionDriver_KeepsStepStateOfItsOwn()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindHandlerLocalState(subjects), Is.Empty,
                "Something that drives a system transition also keeps its own record of where the " +
                "transition is. Two records of one position is the defect this contract exists to " +
                "make unrepresentable: a snapshot carries one of them and a replay rebuilds the other.");
        }

        [Test]
        public void TheAuthority_IsTheOnePermittedHolder()
        {
            // The vacuity guard. A rule saying "nothing keeps step state" is satisfied perfectly by an
            // engine where nothing keeps any, so the one thing that is supposed to keep it is named.
            Assert.That(KeepsStepState(typeof(SystemTransitionAuthority)), Is.True,
                "The authority owns the continuation; that is what authority-owned means.");
            Assert.That(
                typeof(SystemTransitionAuthority).GetProperty(
                    nameof(SystemTransitionAuthority.PendingStep)),
                Is.Not.Null,
                "And it publishes it, so a caller reads where the transition is rather than being told.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindHandlerLocalState(new[] { typeof(HandlerLocalTransitionSample) }), Is.Not.Empty,
                "A driver that remembers its own step must be reported, or the rule is inert.");

            Assert.That(FindHandlerLocalState(new[] { typeof(AuthorityOwnedTransitionSample) }), Is.Empty,
                "The compliant twin drives the same transition and remembers nothing, so it must not " +
                "be reported, or the rule bans the shape it exists to permit.");
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(
                ILConformanceScanner.Method(
                    typeof(SystemTransitionAuthority), nameof(SystemTransitionAuthority.TryAnswer)),
                Is.Not.Null,
                "A rule that names nothing protects nothing.");
            Assert.That(
                ILConformanceScanner.Method(
                    typeof(SystemTransitionAuthority), nameof(SystemTransitionAuthority.TryBegin)),
                Is.Not.Null);
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A type violates when it does both halves at once: it drives a transition, and it keeps state
        /// of its own that names a step. Either alone is fine - the round authority drives one and
        /// remembers nothing, and a test fixture may hold a step identity it never drives with - and it
        /// is the pair that reproduces the position twice.
        /// </summary>
        private static IReadOnlyList<string> FindHandlerLocalState(IEnumerable<Type> types) =>
            types
                .Where(type => type != typeof(SystemTransitionAuthority))
                .Where(DrivesATransition)
                .Where(KeepsStepState)
                .Select(type => $"{type.FullName} drives a transition and keeps its own step state")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static bool DrivesATransition(Type type) =>
            Drivers().Any(member => ILConformanceScanner.References(new[] { type }, member));

        private static IReadOnlyList<MethodInfo> Drivers() => new[]
        {
            ILConformanceScanner.Method(
                typeof(SystemTransitionAuthority), nameof(SystemTransitionAuthority.TryBegin)),
            ILConformanceScanner.Method(
                typeof(SystemTransitionAuthority), nameof(SystemTransitionAuthority.TryAnswer))
        };

        /// <summary>
        /// Whether the type holds instance state that names a step or a continuation - a field or an
        /// auto-property backing field of a step identity, a pending step, or a delegate that could
        /// stand in for one.
        /// </summary>
        private static bool KeepsStepState(Type type) =>
            type.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field => field.FieldType)
                .Any(IsStepState);

        private static bool IsStepState(Type fieldType)
        {
            if (typeof(Delegate).IsAssignableFrom(fieldType)) return true;
            var candidate = fieldType.IsGenericType &&
                typeof(IEnumerable<>).IsAssignableFrom(fieldType.GetGenericTypeDefinition())
                    ? fieldType.GetGenericArguments()[0]
                    : fieldType;
            if (candidate.IsGenericType) candidate = candidate.GetGenericArguments()[0];
            return candidate == typeof(SystemTransitionStepId) ||
                candidate == typeof(PendingSystemTransitionStep);
        }

        /// <summary>
        /// Every Core and Application type, so a transition written after this milestone is audited by
        /// existing rather than by being added to a list. Compiler-generated closures are skipped for
        /// the reason every scan here skips them: they carry the enclosing method's instructions, which
        /// are already scanned. The namespace filter matters in the cross-platform harness, which
        /// compiles Core and these fixtures into one assembly, so without it this rule would report its
        /// own deliberate violation sample.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(SystemTransitionAuthority).Assembly)
                .Concat(ILConformanceScanner.TypesIn(
                    typeof(GuardsDigital.Application.MatchSession).Assembly))
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    (type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal) ||
                        type.Namespace.StartsWith("GuardsDigital.Application", StringComparison.Ordinal)))
                .Distinct();

        // -------------------------------------------------------------------------------------------
        // The samples the rule is measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The obvious wrong shape: drive the transition, and remember separately which step is next.
        /// It works until the match is snapshotted, and then the two disagree.
        /// </summary>
        private sealed class HandlerLocalTransitionSample
        {
            private SystemTransitionStepId currentStep;

            public void Drive(SystemTransitionAuthority authority, SystemTransitionChoiceCommand command)
            {
                var result = authority.TryAnswer(command);
                currentStep = result.PendingStep?.StepId ?? default;
            }

            public SystemTransitionStepId CurrentStep => currentStep;
        }

        /// <summary>The compliant twin: drive it, and ask the authority where it is.</summary>
        private sealed class AuthorityOwnedTransitionSample
        {
            public SystemTransitionStepId Drive(
                SystemTransitionAuthority authority, SystemTransitionChoiceCommand command)
            {
                authority.TryAnswer(command);
                return authority.PendingStep?.StepId ?? default;
            }
        }

        /// <summary>Named so the synthetic definition the rule audits against is not unreferenced.</summary>
        [Test]
        public void TheAuditedBuild_RegistersItsFirstConsumer()
        {
            Assert.That(
                SyntheticSystemTransitions.CreateCatalog().Contains(
                    SyntheticSystemTransitions.BoundedFourStepId),
                Is.True,
                "The contract's declared first consumer is registered, so the rule audits an engine " +
                "that actually has a transition in it.");
        }
    }
}
