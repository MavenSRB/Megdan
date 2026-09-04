using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Why a capability could not be bound. Missing and revision-mismatched stay distinct for the same
    /// reason <c>BoardMapLoadRejectionReason</c> separates them: "this build never had it" and "this
    /// build has a different version of it" call for different responses.
    /// </summary>
    public enum EffectCapabilityLoadRejectionReason
    {
        None = 0,
        MissingCapability = 1,
        RevisionMismatch = 2
    }

    public sealed class EffectCapabilityLoadException : Exception
    {
        public EffectCapabilityLoadException(EffectCapabilityLoadRejectionReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }

        public EffectCapabilityLoadRejectionReason Reason { get; }
    }

    /// <summary>
    /// The trusted, immutable set of capability handlers this build registers.
    ///
    /// This is a catalog, not a discovery mechanism: handlers are supplied by the composition root, keyed
    /// by their own declared reference, and resolved by exact capability plus revision. Nothing here
    /// reflects over the assembly, reads a display name, or accepts an untyped payload. Milestone 6.1h
    /// deleted the interim action-kind strategy registry for good reasons that do not apply here - that
    /// indexed one implementation per interface behind call sites that already named the concrete type,
    /// whereas a capability reference arrives as data inside the definition the resolver is walking and
    /// cannot be named at compile time.
    /// </summary>
    public sealed class EffectCapabilityCatalog
    {
        private readonly Dictionary<EffectCapabilityReference, IEffectCapabilityHandler> handlersByReference;
        private readonly HashSet<EffectCapabilityId> knownCapabilityIds;
        private readonly Dictionary<string, GuardsDigital.Core.Timing.ActiveEffectDefinition>
            activeEffectDefinitionsById;

        public EffectCapabilityCatalog(IEnumerable<IEffectCapabilityHandler> handlers)
        {
            var registered = (handlers ?? throw new ArgumentNullException(nameof(handlers))).ToArray();
            if (registered.Any(handler => handler == null))
            {
                throw new ArgumentException("Registered capability handlers must be non-null.", nameof(handlers));
            }

            if (registered.Any(handler => !handler.Reference.IsDefined))
            {
                throw new ArgumentException(
                    "Every registered capability handler must declare a defined reference.", nameof(handlers));
            }

            if (registered.Any(handler => handler.Schema == null))
            {
                throw new ArgumentException(
                    "Every registered capability handler must declare a parameter schema.", nameof(handlers));
            }

            // Milestone 6.8l. A capability that accepts a consumer is asked for the candidate set it
            // would offer, and neither the feasibility walk nor the live pause may run Execute to get
            // it. Declaring the parameter without declaring that derivation would leave the narrowing
            // silently undone, which is the failure this milestone exists to remove rather than move.
            var unanswerableSelection = registered.FirstOrDefault(handler =>
                EffectSelectionDeclaration.DeclaresConsumerParameter(handler.Schema) &&
                !(handler is IEffectSelectionCapabilityHandler));
            if (unanswerableSelection != null)
            {
                throw new ArgumentException(
                    $"Capability '{unanswerableSelection.Reference}' declares a " +
                    $"'{EffectSelectionDeclaration.ConsumerParameterName}' parameter but does not " +
                    $"implement {nameof(IEffectSelectionCapabilityHandler)}, so nothing could ask it " +
                    "which of its candidates that consumer accepts.", nameof(handlers));
            }

            // Milestone 8.35l. Asking the consumer is not enough on its own: it has to be asked about
            // the value it actually binds. A selection that publishes a minion and a space, consumed by
            // a Swap that takes the space, answers "no" to every candidate when it is handed the bare
            // minion - so the narrowing looked broken, the declaration was dropped, and the offer went
            // unnarrowed. A capability that accepts a consumer therefore also has to say what each
            // prospective answer would publish.
            var unpublishableSelection = registered.FirstOrDefault(handler =>
                EffectSelectionDeclaration.DeclaresConsumerParameter(handler.Schema) &&
                !(handler is IEffectAnticipatedOutcomeCapabilityHandler));
            if (unpublishableSelection != null)
            {
                throw new ArgumentException(
                    $"Capability '{unpublishableSelection.Reference}' declares a " +
                    $"'{EffectSelectionDeclaration.ConsumerParameterName}' parameter but does not " +
                    $"implement {nameof(IEffectAnticipatedOutcomeCapabilityHandler)}, so a candidate " +
                    "could only be put to that consumer as itself rather than as the output the " +
                    "consumer binds.", nameof(handlers));
            }

            if (registered.Select(handler => handler.Reference).Distinct().Count() != registered.Length)
            {
                throw new ArgumentException(
                    "Capability references must be unique; two handlers claim the same capability revision.",
                    nameof(handlers));
            }

            var declaredEffects = registered
                .SelectMany(handler => handler.ActiveEffectDefinitions ??
                    Array.Empty<GuardsDigital.Core.Timing.ActiveEffectDefinition>())
                .ToArray();
            if (declaredEffects.Select(definition => definition.DefinitionId)
                .Distinct(StringComparer.Ordinal).Count() != declaredEffects.Length)
            {
                throw new ArgumentException(
                    "Two registered capabilities declare the same Active-effect definition ID; a restore " +
                    "could not tell which semantics to rebind to.", nameof(handlers));
            }

            activeEffectDefinitionsById = declaredEffects.ToDictionary(
                definition => definition.DefinitionId, StringComparer.Ordinal);
            handlersByReference = registered.ToDictionary(handler => handler.Reference);
            knownCapabilityIds = new HashSet<EffectCapabilityId>(
                registered.Select(handler => handler.Reference.CapabilityId));
            Registered = new ReadOnlyCollection<EffectCapabilityReference>(registered
                .Select(handler => handler.Reference)
                .OrderBy(reference => reference.CapabilityId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray());
        }

        /// <summary>
        /// Every registered reference in a deterministic order, so a snapshot can record exactly which
        /// capability contract set it was written against and a restore can compare them.
        /// </summary>
        public IReadOnlyList<EffectCapabilityReference> Registered { get; }

        public IEffectCapabilityHandler Load(EffectCapabilityReference reference)
        {
            if (handlersByReference.TryGetValue(reference, out var handler))
            {
                return handler;
            }

            throw knownCapabilityIds.Contains(reference.CapabilityId)
                ? new EffectCapabilityLoadException(
                    EffectCapabilityLoadRejectionReason.RevisionMismatch,
                    $"Capability '{reference.CapabilityId}' is registered, but not at revision {reference.Revision}.")
                : new EffectCapabilityLoadException(
                    EffectCapabilityLoadRejectionReason.MissingCapability,
                    $"Capability '{reference.CapabilityId}' is not registered in this build.");
        }

        /// <summary>
        /// The trusted Active-effect semantics this build registers, keyed by definition ID.
        ///
        /// This is what makes acceptance check 5 structural rather than aspirational: a restored
        /// instance is bound to the object found here, so a payload has no way to state what an effect
        /// means - only which effect it is and what has happened to it.
        /// </summary>
        public bool TryGetActiveEffectDefinition(
            string definitionId,
            out GuardsDigital.Core.Timing.ActiveEffectDefinition definition) =>
            activeEffectDefinitionsById.TryGetValue(
                definitionId ?? string.Empty, out definition);

        public bool IsRegistered(EffectCapabilityReference reference) =>
            handlersByReference.ContainsKey(reference);

        public bool TryLoad(EffectCapabilityReference reference, out IEffectCapabilityHandler handler) =>
            handlersByReference.TryGetValue(reference, out handler);
    }
}
