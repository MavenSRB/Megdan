using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5ac acceptance check 2: a registered capability cannot omit the feasibility answer,
    /// and answering it performs nothing.
    ///
    /// The compiler already forces every handler to declare
    /// <see cref="IEffectCapabilityHandler.Anticipate"/>, so what is left to enforce is the part a
    /// signature cannot state. Two rules do it, in the shape milestone 6.5aa used for the shared
    /// immunity check and milestone 6.8g for the unit-type queries: what the probe must read, and what
    /// it must never touch.
    ///
    /// The first rule is derivation. A handler whose execution can refuse - it reaches
    /// <see cref="EffectExecutionResult.ImpossibleMandatory"/> or
    /// <see cref="EffectExecutionResult.DeclinedOptional"/> - and that derives that refusal from
    /// anything at all must reach at least one of the same members from its probe. A second, separate
    /// implementation of the same rule is exactly the failure this milestone cannot afford: it would
    /// grey an option the execution would have performed, or offer one it would refuse. A handler that
    /// only ever applies, and one whose execution is a bare constant, are outside the rule because they
    /// have no derivation to share.
    ///
    /// The second rule is the probe's whole reason for existing. Handlers apply rather than pause -
    /// they commit displacements, spawn from the supply, create Active effects, hand over markers, take
    /// them back, and mint identities for all of it - so a probe that reached one of those members
    /// would perform the effect it was asked about.
    ///
    /// Both walk the call graph rather than one method body, so a probe cannot escape either rule by
    /// putting the work one call away. Unity-independent, so it runs in the cross-platform harness as
    /// well as the Windows tier.
    /// </summary>
    public sealed class CapabilityFeasibilityConformanceTests
    {
        [Test]
        public void EveryRegisteredHandler_DerivesItsProbeFromItsOwnExecutionPath()
        {
            var handlers = RegisteredHandlerTypes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(handlers), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindUnsharedDerivations(handlers), Is.Empty,
                "A registered capability answers the feasibility question from code its own execution " +
                "never reads. Two derivations of one rule can disagree, and when they do a player is " +
                "either shown a mode that does nothing or refused one that would have worked.");
        }

        [Test]
        public void NoProbe_ReachesAMemberThatPerformsSomething()
        {
            Assert.That(FindPerformingProbes(RegisteredHandlerTypes()), Is.Empty,
                "A feasibility probe reaches a member that mutates or mints. Asking whether an option " +
                "can be taken must never take it - which is why the contract asks Anticipate rather " +
                "than running Execute in the first place.");
        }

        [Test]
        public void ConformanceRules_DistinguishAllowedAndForbiddenExamples()
        {
            Assert.That(FindUnsharedDerivations(new[] { typeof(SecondOpinionSample) }), Is.Not.Empty,
                "A handler whose probe re-derives its answer through unrelated code must be reported, " +
                "or the rule is inert.");
            Assert.That(FindUnsharedDerivations(new[] { typeof(SharedDerivationSample) }), Is.Empty,
                "A handler whose probe asks the same derivation its execution asks must not be " +
                "reported, or the rule bans the ordinary shape it exists to permit.");
            Assert.That(FindUnsharedDerivations(new[] { typeof(AlwaysAppliesSample) }), Is.Empty,
                "A handler that cannot refuse has no refusal to derive, so it must not be reported.");

            Assert.That(FindPerformingProbes(new[] { typeof(PerformingProbeSample) }), Is.Not.Empty,
                "A probe that commits a displacement must be reported, or the rule is inert.");
            Assert.That(FindPerformingProbes(new[] { typeof(SharedDerivationSample) }), Is.Empty,
                "A probe that only reads must not be reported.");
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(PerformingMembers(), Has.Length.EqualTo(11),
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
            Assert.That(RegisteredHandlerTypes(), Has.Length.EqualTo(19),
                "The registered contract set changed. A new capability joins these rules rather than " +
                "arriving outside them.");
        }

        // -------------------------------------------------------------------------------------------
        // The rules
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindUnsharedDerivations(IEnumerable<Type> handlerTypes) =>
            handlerTypes
                .Select(handler => new
                {
                    Handler = handler,
                    Probe = Reachable(handler, nameof(IEffectCapabilityHandler.Anticipate)),
                    Execution = Reachable(handler, nameof(IEffectCapabilityHandler.Execute))
                })
                .Where(entry => CanRefuse(entry.Execution) && Derives(entry.Execution))
                .Where(entry => !Leaves(entry.Probe).Intersect(Leaves(entry.Execution)).Any())
                .Select(entry =>
                    $"{entry.Handler.FullName}: its probe shares no derivation with its execution")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static IReadOnlyList<string> FindPerformingProbes(IEnumerable<Type> handlerTypes)
        {
            var forbidden = PerformingMembers().Select(Identity).ToArray();
            return handlerTypes
                .SelectMany(handler => Reachable(handler, nameof(IEffectCapabilityHandler.Anticipate))
                    .SelectMany(scanned => scanned.Instructions
                        .Where(instruction => instruction.Member != null)
                        .Where(instruction => forbidden.Contains(Identity(instruction.Member)))
                        .Select(instruction =>
                            $"{handler.FullName}: its probe reaches " +
                            $"{ILConformanceScanner.Describe(instruction.Member)}")))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Whether this execution path can report a refusal at all. A handler that only ever applies
        /// has nothing for its probe to derive, and requiring a shared read there would only invite a
        /// meaningless one.
        /// </summary>
        private static bool CanRefuse(IReadOnlyList<ILConformanceScanner.ScannedMethod> execution) =>
            execution.Any(scanned =>
                scanned.Calls(ResultFactory(nameof(EffectExecutionResult.ImpossibleMandatory))) ||
                scanned.Calls(ResultFactory(nameof(EffectExecutionResult.DeclinedOptional))));

        /// <summary>
        /// Whether this execution reads anything to decide with, counting the result factories as the
        /// answer rather than as a reading. A handler that refuses unconditionally - the resolver
        /// fixture's `synthetic-impossible`, whose whole purpose is to be impossible - states its
        /// answer as a constant in both places, and two identical constants cannot disagree.
        /// </summary>
        private static bool Derives(IReadOnlyList<ILConformanceScanner.ScannedMethod> execution)
        {
            var answers = new HashSet<string>(
                typeof(EffectExecutionResult)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(Identity));
            return Leaves(execution).Any(leaf => !answers.Contains(leaf));
        }

        /// <summary>
        /// The members a call graph reads, minus the methods the walk itself stepped through. Without
        /// that subtraction a probe and an execution would look unrelated purely because each named its
        /// own entry point, and the shape this rule most wants to permit - a probe and an execution
        /// that both call one shared derivation - is exactly that shape.
        /// </summary>
        private static IReadOnlyList<string> Leaves(
            IReadOnlyList<ILConformanceScanner.ScannedMethod> reachable)
        {
            var walked = new HashSet<string>(reachable.Select(scanned => Identity(scanned.Method)));
            return reachable
                .SelectMany(scanned => scanned.Instructions)
                .Where(instruction => instruction.Member != null)
                .Select(instruction => Identity(instruction.Member))
                .Where(identity => !walked.Contains(identity))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Every method body reachable from one of a handler's members, following calls into types the
        /// handler is declared beside - its own body, its nested types, and the static class that holds
        /// the routines its declarations share. That boundary is what makes the walk finite while still
        /// crossing the one hop the shared push and place routines live behind.
        /// </summary>
        private static IReadOnlyList<ILConformanceScanner.ScannedMethod> Reachable(
            Type handlerType, string entryPointName)
        {
            var reached = ILConformanceScanner.ReachableFrom(
                handlerType,
                entryPointName,
                ILConformanceScanner.WithNestedTypes(handlerType)
                    .Concat(ILConformanceScanner.WithNestedTypes(handlerType.DeclaringType))
                    .Distinct());

            Assert.That(reached, Is.Not.Empty,
                $"{handlerType.Name} declares no {entryPointName}; a rule cannot judge a member that " +
                "does not exist.");
            return reached;
        }

        private static Type[] RegisteredHandlerTypes()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();
            return catalog.Registered
                .Select(reference => catalog.Load(reference).GetType())
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Everything a probe must never reach: the four board and component commits, the two identity
        /// mints, the two arms of the shared Place operation, the marker register's two transitions,
        /// and the lifecycle creation. Each is named as a resolved member rather than a string.
        /// </summary>
        private static MemberInfo[] PerformingMembers() => new MemberInfo[]
        {
            ILConformanceScanner.Method(
                typeof(IBoardDisplacementAuthority),
                nameof(IBoardDisplacementAuthority.CommitValidatedDisplacement)),
            ILConformanceScanner.Method(
                typeof(IUnitSpawnAuthority), nameof(IUnitSpawnAuthority.CommitValidatedUnitSpawn)),
            ILConformanceScanner.Method(
                typeof(ITokenPlacementAuthority),
                nameof(ITokenPlacementAuthority.CommitValidatedTokenPlacement)),
            ILConformanceScanner.Method(
                typeof(ITokenPlacementAuthority),
                nameof(ITokenPlacementAuthority.CommitValidatedTokenReclamation)),
            ILConformanceScanner.Method(
                typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Give)),
            ILConformanceScanner.Method(
                typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Return)),
            ILConformanceScanner.Method(
                typeof(ActiveEffectLifecycle), nameof(ActiveEffectLifecycle.Create)),
            ILConformanceScanner.Method(
                typeof(ActiveEffectInstanceIdentity), nameof(ActiveEffectInstanceIdentity.Mint)),
            ILConformanceScanner.Method(
                typeof(MarkerInstanceIdentity), nameof(MarkerInstanceIdentity.Mint)),
            ILConformanceScanner.Method(
                typeof(PlaceOperation), nameof(PlaceOperation.PlaceFromBoard)),
            ILConformanceScanner.Method(
                typeof(PlaceOperation), nameof(PlaceOperation.PlaceFromSupply))
        };

        private static MethodInfo ResultFactory(string name) =>
            ILConformanceScanner.Method(typeof(EffectExecutionResult), name);

        private static string Identity(MemberInfo member)
        {
            try
            {
                return $"{member.Module.ModuleVersionId:N}:{member.MetadataToken:X8}";
            }
            catch (InvalidOperationException)
            {
                return $"{member.DeclaringType?.FullName}.{member.Name}";
            }
        }

        // -------------------------------------------------------------------------------------------
        // Samples. None is registered; each exists so the rules above are shown to have teeth.
        // -------------------------------------------------------------------------------------------

        /// <summary>Deliberate violation: its probe answers from a rule its execution never asks.</summary>
        private sealed class SecondOpinionSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.RoundIndex > 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>Compliant twin: both ask the one derivation.</summary>
        private sealed class SharedDerivationSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        /// <summary>Outside the derivation rule: it can never refuse, so it derives no refusal.</summary>
        private sealed class AlwaysAppliesSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>Deliberate violation: its probe performs the displacement it was asked about.</summary>
        private sealed class PerformingProbeSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                context.Components.Board.CommitValidatedDisplacement(
                    default, default);
                return EffectExecutionResultKind.Applied;
            }
        }

        private abstract class SampleHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("conformance-sample"), 1);

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public abstract EffectExecutionResult Execute(EffectCapabilityContext context);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A sample never requests a choice.");

            public abstract EffectExecutionResultKind Anticipate(EffectCapabilityContext context);

            /// <summary>The shared derivation the samples differ in their use of.</summary>
            protected static IReadOnlyList<HeroId> Targets(EffectCapabilityContext context) =>
                context.Participants == null
                    ? Array.Empty<HeroId>()
                    : context.Participants.Heroes.Select(hero => hero.Id).ToArray();
        }
    }
}
