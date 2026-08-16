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
