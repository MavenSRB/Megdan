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

            // Milestone 8.35z32. A shared supply is one physical pile, so its members have to agree
            // how big it is: two numbers for one pile is exactly the drift the derived-count design
            // exists to make impossible, and it would show up as a supply that answers differently
            // depending on which of its members was asked.
            var disagreeingGroup = registeredTokens
                .Where(definition => definition.SharesASupply)
                .GroupBy(definition => definition.SupplyGroupId, StringComparer.Ordinal)
                .FirstOrDefault(group =>
                    group.Select(definition => definition.SupplyCount).Distinct().Count() > 1);
            if (disagreeingGroup != null)
            {
                throw new ArgumentException(
                    $"Token definitions sharing supply '{disagreeingGroup.Key}' declare different " +
                    "supply counts, so the one pile they share would have two sizes.",
                    nameof(tokens));
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
        ///
        /// <para>Since milestone 8.35z32 the count is taken over the <em>pile</em> rather than the
        /// definition: a definition that names a <c>SupplyGroupId</c> shares one physical supply with
        /// every other definition naming it, so every placed member of the group is subtracted from the
        /// group's one total. A definition that names no group is its own pile and the answer is
        /// unchanged, which is every definition written before that milestone.</para>
        /// </summary>
        public int CountAvailableTokens(
            TokenDefinitionReference reference,
            IEnumerable<PlacedToken> placedTokens)
        {
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));

            var definition = LoadToken(reference);
            var placed = definition.SharesASupply
                ? placedTokens.Count(token => SharesSupplyWith(token, definition))
                : placedTokens.Count(token => token.Definition.Reference.Equals(reference));
            return Math.Max(0, definition.SupplyCount - placed);
        }

        /// <summary>
        /// Every token currently on the board that p.18's short-supply step may take back to make room
        /// for one of <paramref name="reference"/> - "remove up to that number of tokens of the same
        /// type from the board".
        ///
        /// <para>For a definition with its own supply that is the tokens of that definition, which is
        /// what the sentence has always meant here. For a definition sharing a pile it is the whole
        /// group, and RULING 44 says so in as many words: Tali's three tokens "are the same token, it
        /// just depends what you use them for", so placing beyond the supply reaches her own standing
        /// tokens whatever role they currently hold.</para>
        /// </summary>
        public IEnumerable<PlacedToken> ReclaimableForPlacement(
            TokenDefinitionReference reference,
            IEnumerable<PlacedToken> placedTokens)
        {
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));

            var definition = LoadToken(reference);
            return definition.SharesASupply
                ? placedTokens.Where(token => SharesSupplyWith(token, definition))
                : placedTokens.Where(token => token.Definition.Reference.Equals(reference));
        }

        private static bool SharesSupplyWith(PlacedToken token, TokenDefinition definition) =>
            token != null && token.Definition != null &&
            string.Equals(
                token.Definition.SupplyGroupId, definition.SupplyGroupId, StringComparison.Ordinal);

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
