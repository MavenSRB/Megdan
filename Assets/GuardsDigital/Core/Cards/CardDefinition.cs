using System;

namespace GuardsDigital.Core.Cards
{
    public sealed class CardDefinition
    {
        public CardDefinition(CardDefinitionId id, string contentRevision, string displayName)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Card definition requires a defined ID.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(contentRevision))
            {
                throw new ArgumentException("Card definition requires a content revision.", nameof(contentRevision));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Card definition requires a display name.", nameof(displayName));
            }

            Id = id;
            ContentRevision = contentRevision;
            DisplayName = displayName;
        }

        public CardDefinitionId Id { get; }

        public string ContentRevision { get; }

        public string DisplayName { get; }

        public CardDefinitionReference Reference => new CardDefinitionReference(Id, ContentRevision);
    }
}
