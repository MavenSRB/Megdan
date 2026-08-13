using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct CardDefinitionReference : IEquatable<CardDefinitionReference>
    {
        public CardDefinitionReference(CardDefinitionId definitionId, string contentRevision)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException("Card definition reference requires a defined definition ID.", nameof(definitionId));
            }

            if (string.IsNullOrWhiteSpace(contentRevision))
            {
                throw new ArgumentException("Card definition reference requires a content revision.", nameof(contentRevision));
            }

            DefinitionId = definitionId;
            ContentRevision = contentRevision;
        }

        public CardDefinitionId DefinitionId { get; }

        public string ContentRevision { get; }

        public bool IsDefined => DefinitionId.IsDefined && !string.IsNullOrWhiteSpace(ContentRevision);

        public bool Equals(CardDefinitionReference other)
        {
            return DefinitionId.Equals(other.DefinitionId) &&
                string.Equals(ContentRevision, other.ContentRevision, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CardDefinitionReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (DefinitionId.GetHashCode() * 397) ^
                    StringComparer.Ordinal.GetHashCode(ContentRevision ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return $"{DefinitionId}@{ContentRevision}";
        }
    }
}
