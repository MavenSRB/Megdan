using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5ad acceptance check 1: a capability that moves something a declared number of
    /// spaces reads that number through one statement of pages 10 and 11, and cannot answer it again
    /// for itself.
    ///
    /// The defect this closes was two readings of one sentence rather than a mistake in either. Object
    /// Push offered distances 0..X because p.18 says outright that a push can move a target zero
    /// spaces; card-text direct Move offered only 1..X because its candidates came from the Movement
    /// action's reachability walk, which never emits the mover's own space. Both looked locally
    /// correct, and nothing held them to the same answer - which is exactly what a conformance rule is
    /// for, and why the fix is not simply adding one coordinate to one list.
    ///
    /// The subject set is derived from the declared contracts rather than named: every registered
    /// capability whose parameter schema declares an integer `distance`. A later capability that
    /// declares one joins this rule by declaring it, instead of being remembered into it.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class OptionalDistanceConformanceTests
    {
        [Test]
        public void EveryCapabilityDeclaringADistance_ReadsItThroughTheOneOptionalDistanceStatement()
        {
            var handlers = RegisteredHandlers();

            Assert.That(ILConformanceScanner.UnresolvedTokens(handlers.Select(handler => handler.GetType())),
                Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindUnsharedReadings(handlers), Is.Empty,
                "A capability declares a distance and decides for itself how much of it is optional. " +
                "Two readings of pages 10 and 11 can disagree, and when they did, one displacement " +
                "let a player choose to move nothing and the other refused the same choice.");
        }

        [Test]
        public void TheRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindUnsharedReadings(new[] { new SecondReadingSample() }), Is.Not.Empty,
                "A handler that reads the failure policy itself to decide how far it must travel must " +
                "be reported, or the rule is inert.");
            Assert.That(FindUnsharedReadings(new[] { new SharedReadingSample() }), Is.Empty,
                "A handler that asks the one statement must not be reported, or the rule bans the " +
                "shape it exists to require.");
            Assert.That(FindUnsharedReadings(new[] { new NoDistanceSample() }), Is.Empty,
                "A capability that moves nothing a stated number of spaces has no distance to read.");
        }

        [Test]
        public void TheSubjectSet_IsTheDeclaredOneAndIsNotEmpty()
        {
            var subjects = RegisteredHandlers()
                .Where(DeclaresADistance)
                .Select(handler => handler.Reference.CapabilityId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(subjects, Is.EqualTo(new[]
            {
                "synthetic-direct-move", "synthetic-object-push", "synthetic-push-target"
            }), "The set of capabilities declaring a distance changed. A new one joins this rule " +
                "rather than arriving outside it; a vanished one means the rule now guards nothing.");
        }

        [Test]
        public void TheNamedStatement_Resolves()
        {
            Assert.That(TheStatement(), Is.Not.Null,
                "A rule cannot name a member that does not exist. Correct the rule rather than " +
                "deleting it; a rule that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindUnsharedReadings(
            IEnumerable<IEffectCapabilityHandler> handlers)
        {
            var statement = TheStatement();
            return handlers
                .Where(DeclaresADistance)
                .Where(handler => !Reachable(handler.GetType())
                    .Any(scanned => scanned.Calls(statement)))
                .Select(handler =>
                    $"{handler.GetType().FullName}: it declares a distance without reading " +
                    $"{ILConformanceScanner.Describe(statement)}")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Whether this capability moves something a stated number of spaces, read from the contract it
        /// publishes rather than from its name.
        /// </summary>
        private static bool DeclaresADistance(IEffectCapabilityHandler handler) =>
            handler.Schema.Parameters.Any(parameter =>
                string.Equals(parameter.Name, "distance", StringComparison.Ordinal) &&
                parameter.ValueKind == EffectValueKind.Integer);

        /// <summary>
        /// Everything the handler's execution reaches, following calls into the types it is declared
        /// beside so the shared routines behind a declaration are walked, and no further. Scanning
        /// those types outright would let one handler pass because a sibling declared in the same
        /// static class makes the call.
        /// </summary>
        private static IReadOnlyList<ILConformanceScanner.ScannedMethod> Reachable(Type handlerType)
        {
            var reached = ILConformanceScanner.ReachableFrom(
                handlerType,
                nameof(IEffectCapabilityHandler.Execute),
                ILConformanceScanner.WithNestedTypes(handlerType)
                    .Concat(ILConformanceScanner.WithNestedTypes(handlerType.DeclaringType))
                    .Distinct());

            Assert.That(reached, Is.Not.Empty,
                $"{handlerType.Name} declares no Execute; a rule cannot judge a member that does not " +
                "exist.");
            return reached;
        }

        private static IEffectCapabilityHandler[] RegisteredHandlers()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();
            return catalog.Registered.Select(reference => catalog.Load(reference)).ToArray();
        }

        private static System.Reflection.MethodInfo TheStatement() =>
            ILConformanceScanner.Method(
                typeof(AdmittedDistances), nameof(AdmittedDistances.ForDeclared));

        // -------------------------------------------------------------------------------------------
        // Samples. None is registered; each exists so the rule above is shown to have teeth.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Deliberate violation, and the exact pre-6.5ad shape: it declares a distance and decides for
        /// itself how much of it is optional, by reading the same policy the one statement reads.
        /// </summary>
        private sealed class SecondReadingSample : SampleHandler
        {
            public override EffectCapabilityParameterSchema Schema { get; } = DistanceSchema();

            public override EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var declared = context.Input("distance").IntegerValue;
                var lowest = context.FailurePolicy == EffectFailurePolicy.UpTo ? 1 : declared;
                return lowest > declared
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied();
            }
        }

        /// <summary>Compliant twin: the same decision, taken from the one statement.</summary>
        private sealed class SharedReadingSample : SampleHandler
        {
            public override EffectCapabilityParameterSchema Schema { get; } = DistanceSchema();

            public override EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var admitted = AdmittedDistances.ForDeclared(
                    context.FailurePolicy, context.Input("distance").IntegerValue);
                return admitted.AdmitsStandingStill
                    ? EffectExecutionResult.Applied()
                    : EffectExecutionResult.ImpossibleMandatory();
            }
        }

        /// <summary>Outside the rule: nothing it does is measured in spaces.</summary>
        private sealed class NoDistanceSample : SampleHandler
        {
            public override EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("subject", EffectValueKind.Coordinate) },
                    Array.Empty<EffectOutputBinding>());

            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();
        }

        private abstract class SampleHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("distance-conformance-sample"), 1);

            public abstract EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public abstract EffectExecutionResult Execute(EffectCapabilityContext context);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A sample never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Execute(context).Kind;

            protected static EffectCapabilityParameterSchema DistanceSchema() =>
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("distance", EffectValueKind.Integer) },
                    Array.Empty<EffectOutputBinding>());
        }
    }
}
