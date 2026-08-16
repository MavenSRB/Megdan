using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a. The catalog is the trusted binding a restored snapshot rebinds against, so its
    /// refusals have to distinguish "this build never had that capability" from "this build has a
    /// different revision of it" - the same split <c>BoardMapCatalog</c> makes for maps, and for the
    /// same reason: they call for different responses.
    /// </summary>
    public sealed class EffectCapabilityCatalogTests
    {
        [Test]
        public void Load_ResolvesARegisteredCapabilityAtItsExactRevision()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();

            var handler = catalog.Load(SyntheticEffectCapabilities.NoteActorV1);

            Assert.That(handler.Reference, Is.EqualTo(SyntheticEffectCapabilities.NoteActorV1));
            Assert.That(handler.Schema, Is.Not.Null);
        }

        [Test]
        public void Load_SeparatesAnUnknownCapabilityFromAKnownOneAtAnotherRevision()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();

            var missing = Assert.Throws<EffectCapabilityLoadException>(() => catalog.Load(
                new EffectCapabilityReference(new EffectCapabilityId("never-registered"), 1)));
            var mismatch = Assert.Throws<EffectCapabilityLoadException>(() => catalog.Load(
                new EffectCapabilityReference(SyntheticEffectCapabilities.NoteActor, 2)));

            Assert.That(missing.Reason,
                Is.EqualTo(EffectCapabilityLoadRejectionReason.MissingCapability));
            Assert.That(mismatch.Reason,
                Is.EqualTo(EffectCapabilityLoadRejectionReason.RevisionMismatch),
                "A registered capability at an unregistered revision is a version problem, not a " +
                "missing one, and a restore must be able to tell a player which.");
        }

        [Test]
        public void Registered_IsOrderedDeterministicallySoASnapshotCanCompareIt()
        {
            var first = SyntheticEffectCapabilities.CreateCatalog().Registered;
            var second = SyntheticEffectCapabilities.CreateCatalog().Registered;

            Assert.That(first, Is.EqualTo(second));
            var names = first.Select(reference => reference.CapabilityId.Value).ToArray();
            Assert.That(
                names,
                Is.EqualTo(names.OrderBy(name => name, StringComparer.Ordinal).ToArray()),
                "A snapshot records this list to prove which contract set it was written against, so " +
                "its order cannot depend on registration order.");
        }

        [Test]
        public void Construction_RejectsTwoHandlersClaimingTheSameCapabilityRevision()
        {
            Assert.Throws<ArgumentException>(() => new EffectCapabilityCatalog(
                new IEffectCapabilityHandler[] { new StubHandler(), new StubHandler() }));
        }

        [Test]
        public void Construction_RejectsAHandlerWithNoDeclaredSchema()
        {
            Assert.Throws<ArgumentException>(() => new EffectCapabilityCatalog(
                new IEffectCapabilityHandler[] { new SchemalessHandler() }));
        }

        [Test]
        public void Construction_RejectsASelectionThatAcceptsAConsumerItCannotAnswerFor()
        {
            // Milestone 6.8l. Declaring the consumer parameter is what makes a definition able to say
            // "this choice is for that step", and the answer to it can only come from the candidate
            // set the handler would offer. A schema that accepts the question with nothing able to
            // answer it would leave the narrowing silently undone, which is the defect this milestone
            // removes rather than relocates.
            Assert.Throws<ArgumentException>(() => new EffectCapabilityCatalog(
                new IEffectCapabilityHandler[] { new UnanswerableSelectionHandler() }));
        }

        [Test]
        public void TheRegisteredSelection_DeclaresBothHalvesOfThatContract()
        {
            // The vacuity guard: the rule above is only worth having if the shipped selection passes
            // it, and passing it is what the walk and the live pause depend on.
            var catalog = SyntheticEffectCapabilities.CreateCatalog();

            foreach (var reference in new[]
                {
                    SyntheticTargetingCapabilities.ChooseTargetV3,
                    SyntheticTargetingCapabilities.ChooseTargetV4
                })
            {
                var handler = catalog.Load(reference);
                Assert.That(
                    EffectSelectionDeclaration.DeclaresConsumerParameter(handler.Schema), Is.True,
                    $"{reference} accepts a consumer.");
                Assert.That(handler, Is.InstanceOf<IEffectSelectionCapabilityHandler>(),
                    $"{reference} can be asked what it would offer.");
                Assert.That(
                    handler.Schema.Parameters
                        .Single(parameter => parameter.Name ==
                            EffectSelectionDeclaration.ConsumerParameterName)
                        .IsRequired,
                    Is.False,
                    "And declaring one stays optional, so a selection that names no consumer keeps " +
                    "exactly the reading milestone 6.5ac chose.");
            }
        }

        private sealed class UnanswerableSelectionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("unanswerable-selection"), 1);

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(new[]
                {
                    new EffectCapabilityParameter(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValueKind.Node,
                        isRequired: false)
                });

            public IReadOnlyList<GuardsDigital.Core.Timing.ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<GuardsDigital.Core.Timing.ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        private sealed class StubHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("duplicate"), 1);

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<GuardsDigital.Core.Timing.ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<GuardsDigital.Core.Timing.ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        private sealed class SchemalessHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("schemaless"), 1);

            public EffectCapabilityParameterSchema Schema => null;

            public IReadOnlyList<GuardsDigital.Core.Timing.ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<GuardsDigital.Core.Timing.ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
