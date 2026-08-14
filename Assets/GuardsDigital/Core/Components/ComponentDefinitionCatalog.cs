using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// Why a component definition could not be bound. Missing and revision-mismatched stay distinct for
    /// the same reason <c>EffectCapabilityLoadRejectionReason</c> and <c>BoardMapLoadRejectionReason</c>
    /// separate them: "this build never had it" and "this build has a different version of it" call for
    /// different responses.
    /// </summary>
    public enum ComponentDefinitionLoadRejectionReason
    {
        None = 0,
        MissingDefinition = 1,
        RevisionMismatch = 2
    }

    public sealed class ComponentDefinitionLoadException : Exception
    {
        public ComponentDefinitionLoadException(ComponentDefinitionLoadRejectionReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }

        public ComponentDefinitionLoadRejectionReason Reason { get; }
    }

    /// <summary>
    /// The trusted, immutable set of token and marker definitions this build registers.
    ///
    /// This is the component counterpart of <c>EffectCapabilityCatalog</c>, and exists for the same
    /// reason: it is what makes milestone 6.2's fourth acceptance check structural rather than
    /// aspirational. A placed token or attached marker records only which definition it is and what has
    /// happened to it; how many exist, what they are called, and when a marker returns are read from
    /// here. A snapshot payload, a prefab, a sprite, or a scene object therefore has no way to state a
    /// component's lifetime - it can only name one this catalog already owns.
    ///
    /// Supply totals live here rather than in match state on purpose. What remains in the supply is
    /// derived by subtracting what is placed or held, so there is no second count that could drift out
    /// of step with the board.
    /// </summary>
    public sealed class ComponentDefinitionCatalog
    {
        private readonly Dictionary<TokenDefinitionReference, TokenDefinition> tokensByReference;
        private readonly HashSet<TokenDefinitionId> knownTokenIds;
        private readonly Dictionary<MarkerDefinitionReference, MarkerDefinition> markersByReference;
        private readonly HashSet<MarkerDefinitionId> knownMarkerIds;

        public ComponentDefinitionCatalog(
            IEnumerable<TokenDefinition> tokens,
            IEnumerable<MarkerDefinition> markers)
        {
            var registeredTokens = (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToArray();
            var registeredMarkers = (markers ?? throw new ArgumentNullException(nameof(markers))).ToArray();

            if (registeredTokens.Any(definition => definition == null))
            {
                throw new ArgumentException("Registered token definitions must be non-null.", nameof(tokens));
            }

            if (registeredMarkers.Any(definition => definition == null))
            {
                throw new ArgumentException("Registered marker definitions must be non-null.", nameof(markers));
            }

            // One revision of each definition, not one entry per reference. Two revisions of the same
            // token type would leave "tokens of the same type" - the rulebook's own p.18 supply
            // wording - ambiguous, and the reclamation choice depends on that phrase meaning one thing.
            if (registeredTokens.Select(definition => definition.Id).Distinct().Count() != registeredTokens.Length)
            {
                throw new ArgumentException(
                    "Two token definitions share an ID; the same-type supply rule could not tell them apart.",
                    nameof(tokens));
            }

            if (registeredMarkers.Select(definition => definition.Id).Distinct().Count() != registeredMarkers.Length)
            {
                throw new ArgumentException(
                    "Two marker definitions share an ID; a return could not tell which supply to use.",
                    nameof(markers));
            }

            tokensByReference = registeredTokens.ToDictionary(definition => definition.Reference);
            knownTokenIds = new HashSet<TokenDefinitionId>(registeredTokens.Select(definition => definition.Id));
            markersByReference = registeredMarkers.ToDictionary(definition => definition.Reference);
            knownMarkerIds = new HashSet<MarkerDefinitionId>(registeredMarkers.Select(definition => definition.Id));

            RegisteredTokens = new ReadOnlyCollection<TokenDefinitionReference>(registeredTokens
                .Select(definition => definition.Reference)
                .OrderBy(reference => reference.DefinitionId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray());
            RegisteredMarkers = new ReadOnlyCollection<MarkerDefinitionReference>(registeredMarkers
                .Select(definition => definition.Reference)
                .OrderBy(reference => reference.DefinitionId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray());
        }

        /// <summary>
        /// Every registered token reference in a deterministic order, so a snapshot can record which
        /// component content set it was written against and a restore can compare them.
        /// </summary>
        public IReadOnlyList<TokenDefinitionReference> RegisteredTokens { get; }

        /// <summary>Every registered marker reference, in the same deterministic order.</summary>
        public IReadOnlyList<MarkerDefinitionReference> RegisteredMarkers { get; }

        public TokenDefinition LoadToken(TokenDefinitionReference reference)
        {
            if (tokensByReference.TryGetValue(reference, out var definition))
            {
                return definition;
            }

            throw knownTokenIds.Contains(reference.DefinitionId)
                ? new ComponentDefinitionLoadException(
                    ComponentDefinitionLoadRejectionReason.RevisionMismatch,
                    $"Token '{reference.DefinitionId}' is registered, but not at revision {reference.Revision}.")
                : new ComponentDefinitionLoadException(
                    ComponentDefinitionLoadRejectionReason.MissingDefinition,
                    $"Token '{reference.DefinitionId}' is not registered in this build.");
        }

        public MarkerDefinition LoadMarker(MarkerDefinitionReference reference)
        {
            if (markersByReference.TryGetValue(reference, out var definition))
            {
                return definition;
            }

            throw knownMarkerIds.Contains(reference.DefinitionId)
                ? new ComponentDefinitionLoadException(
                    ComponentDefinitionLoadRejectionReason.RevisionMismatch,
                    $"Marker '{reference.DefinitionId}' is registered, but not at revision {reference.Revision}.")
                : new ComponentDefinitionLoadException(
                    ComponentDefinitionLoadRejectionReason.MissingDefinition,
                    $"Marker '{reference.DefinitionId}' is not registered in this build.");
        }

        public bool TryLoadToken(TokenDefinitionReference reference, out TokenDefinition definition) =>
            tokensByReference.TryGetValue(reference, out definition);

        public bool TryLoadMarker(MarkerDefinitionReference reference, out MarkerDefinition definition) =>
            markersByReference.TryGetValue(reference, out definition);

        public bool IsRegistered(TokenDefinitionReference reference) =>
            tokensByReference.ContainsKey(reference);

        public bool IsRegistered(MarkerDefinitionReference reference) =>
            markersByReference.ContainsKey(reference);

        /// <summary>
        /// The rulebook's p.18 supply question - "more tokens than are available in the supply" - answered
        /// against the trusted total and the tokens currently on the board. Nothing stores this number.
        /// </summary>
        public int CountAvailableTokens(
            TokenDefinitionReference reference,
            IEnumerable<PlacedToken> placedTokens)
        {
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));

            var placed = placedTokens.Count(token => token.Definition.Reference.Equals(reference));
            return Math.Max(0, LoadToken(reference).SupplyCount - placed);
        }

        /// <summary>The marker counterpart: the total minus every marker currently held by a hero.</summary>
        public int CountAvailableMarkers(
            MarkerDefinitionReference reference,
            IEnumerable<MarkerAttachment> attachments)
        {
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));

            var held = attachments.Count(attachment => attachment.Definition.Reference.Equals(reference));
            return Math.Max(0, LoadMarker(reference).SupplyCount - held);
        }
    }
}
