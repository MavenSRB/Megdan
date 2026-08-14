using System;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The shared handler contract an effect node invokes. Distinct from the node's own ID: many nodes
    /// across many card definitions may name the same capability.
    /// </summary>
    public readonly struct EffectCapabilityId : IEquatable<EffectCapabilityId>
    {
        public EffectCapabilityId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Effect capability ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EffectCapabilityId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectCapabilityId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// A capability plus the exact revision of its contract, so a restored match cannot bind a pending
    /// effect to a handler whose meaning has changed underneath it. Modelled on
    /// <c>MapDefinitionReference</c>, which pairs a map with an integer content revision for the same
    /// reason.
    /// </summary>
    public readonly struct EffectCapabilityReference : IEquatable<EffectCapabilityReference>
    {
        public EffectCapabilityReference(EffectCapabilityId capabilityId, int revision)
        {
            if (!capabilityId.IsDefined)
            {
                throw new ArgumentException(
                    "An effect capability reference requires a defined capability ID.", nameof(capabilityId));
            }

            if (revision < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision), "An effect capability revision starts at 1.");
            }

            CapabilityId = capabilityId;
            Revision = revision;
        }

        public EffectCapabilityId CapabilityId { get; }

        public int Revision { get; }

        public bool IsDefined => CapabilityId.IsDefined && Revision >= 1;

        public bool Equals(EffectCapabilityReference other) =>
            CapabilityId.Equals(other.CapabilityId) && Revision == other.Revision;

        public override bool Equals(object obj) =>
            obj is EffectCapabilityReference other && Equals(other);

        public override int GetHashCode() =>
            (CapabilityId.GetHashCode() * 397) ^ Revision;

        public override string ToString() => $"{CapabilityId}@{Revision}";
    }

    /// <summary>Identifies one effect definition inside the trusted match-definition catalog.</summary>
    public readonly struct EffectDefinitionId : IEquatable<EffectDefinitionId>
    {
        public EffectDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Effect definition ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EffectDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectDefinitionId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// An effect definition plus the content revision it was authored at. The revision is a string, not
    /// an integer, because an effect definition rides the card content revision that carries it -
    /// the same shape as <c>CardDefinitionReference</c>.
    /// </summary>
    public readonly struct EffectDefinitionReference : IEquatable<EffectDefinitionReference>
    {
        public EffectDefinitionReference(EffectDefinitionId definitionId, string contentRevision)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException(
                    "An effect definition reference requires a defined definition ID.", nameof(definitionId));
            }

            if (string.IsNullOrWhiteSpace(contentRevision))
            {
                throw new ArgumentException(
                    "An effect definition reference requires a content revision.", nameof(contentRevision));
            }

            DefinitionId = definitionId;
            ContentRevision = contentRevision;
        }

        public EffectDefinitionId DefinitionId { get; }

        public string ContentRevision { get; }

        public bool IsDefined => DefinitionId.IsDefined && !string.IsNullOrWhiteSpace(ContentRevision);

        public bool Equals(EffectDefinitionReference other) =>
            DefinitionId.Equals(other.DefinitionId) &&
            string.Equals(ContentRevision, other.ContentRevision, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is EffectDefinitionReference other && Equals(other);

        public override int GetHashCode() =>
            (DefinitionId.GetHashCode() * 397) ^
            StringComparer.Ordinal.GetHashCode(ContentRevision ?? string.Empty);

        public override string ToString() => $"{DefinitionId}@{ContentRevision}";
    }

    /// <summary>
    /// The stable identity of one node inside one effect definition. Unique within its definition, not
    /// globally. A paused resolution serializes these, so a pending effect resumes at the exact node it
    /// stopped at rather than at a reconstructed position.
    /// </summary>
    public readonly struct EffectNodeId : IEquatable<EffectNodeId>
    {
        public EffectNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Effect node ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EffectNodeId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectNodeId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }
}
