using System;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// Identifies one token definition inside the trusted component catalog. Distinct from
    /// <see cref="TokenInstanceId"/>: many placed tokens may share one definition, and the rulebook's
    /// supply rule on p.18 counts "tokens of the same type", which is this identity.
    /// </summary>
    public readonly struct TokenDefinitionId : IEquatable<TokenDefinitionId>
    {
        public TokenDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Token definition ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(TokenDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is TokenDefinitionId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// A token definition plus the exact content revision it was authored at, so a restored match
    /// cannot bind a placed token to a definition whose supply size or properties have changed
    /// underneath it. Modelled on <c>EffectCapabilityReference</c>, which pairs a capability with an
    /// integer revision for the same reason.
    /// </summary>
    public readonly struct TokenDefinitionReference : IEquatable<TokenDefinitionReference>
    {
        public TokenDefinitionReference(TokenDefinitionId definitionId, int revision)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException(
                    "A token definition reference requires a defined definition ID.", nameof(definitionId));
            }

            if (revision < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision), "A token definition revision starts at 1.");
            }

            DefinitionId = definitionId;
            Revision = revision;
        }

        public TokenDefinitionId DefinitionId { get; }

        public int Revision { get; }

        public bool IsDefined => DefinitionId.IsDefined && Revision >= 1;

        public bool Equals(TokenDefinitionReference other) =>
            DefinitionId.Equals(other.DefinitionId) && Revision == other.Revision;

        public override bool Equals(object obj) => obj is TokenDefinitionReference other && Equals(other);

        public override int GetHashCode() => (DefinitionId.GetHashCode() * 397) ^ Revision;

        public override string ToString() => $"{DefinitionId}@{Revision}";
    }

    /// <summary>
    /// The stable identity of one token, from the moment it leaves the supply until it returns.
    ///
    /// Rulebook p.19: a placed token "retains all its properties until that token is removed". That is
    /// a statement about a specific token, not about its artwork, so the instance carries its own
    /// identity and every operation - placement, reclamation, and later Move, Push, Swap, Replace and
    /// Remove - names it rather than a coordinate.
    /// </summary>
    public readonly struct TokenInstanceId : IEquatable<TokenInstanceId>
    {
        public TokenInstanceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Token instance ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(TokenInstanceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is TokenInstanceId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Identifies one marker definition inside the trusted component catalog.</summary>
    public readonly struct MarkerDefinitionId : IEquatable<MarkerDefinitionId>
    {
        public MarkerDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Marker definition ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(MarkerDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is MarkerDefinitionId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>A marker definition plus its content revision. The token reference's counterpart.</summary>
    public readonly struct MarkerDefinitionReference : IEquatable<MarkerDefinitionReference>
    {
        public MarkerDefinitionReference(MarkerDefinitionId definitionId, int revision)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException(
                    "A marker definition reference requires a defined definition ID.", nameof(definitionId));
            }

            if (revision < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision), "A marker definition revision starts at 1.");
            }

            DefinitionId = definitionId;
            Revision = revision;
        }

        public MarkerDefinitionId DefinitionId { get; }

        public int Revision { get; }

        public bool IsDefined => DefinitionId.IsDefined && Revision >= 1;

        public bool Equals(MarkerDefinitionReference other) =>
            DefinitionId.Equals(other.DefinitionId) && Revision == other.Revision;

        public override bool Equals(object obj) => obj is MarkerDefinitionReference other && Equals(other);

        public override int GetHashCode() => (DefinitionId.GetHashCode() * 397) ^ Revision;

        public override string ToString() => $"{DefinitionId}@{Revision}";
    }

    /// <summary>
    /// The stable identity of one marker, from the moment it leaves the supply until it is returned.
    /// A marker is a physical component with a finite supply exactly as a token is; the difference is
    /// where it goes, not whether it has identity.
    /// </summary>
    public readonly struct MarkerInstanceId : IEquatable<MarkerInstanceId>
    {
        public MarkerInstanceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Marker instance ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(MarkerInstanceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is MarkerInstanceId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }
}
