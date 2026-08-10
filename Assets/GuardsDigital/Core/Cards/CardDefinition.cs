using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public sealed class CardDefinition
    {
        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority)
            : this(
                id,
                contentRevision,
                displayName,
                initiative,
                contentAuthority,
                Enumerable.Empty<ActionDefinition>())
        {
        }

        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            IEnumerable<ActionDefinition> actionDefinitions)
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

            if (!initiative.IsDefined)
            {
                throw new ArgumentException("Card definition requires a positive Initiative value.", nameof(initiative));
            }

            if (!Enum.IsDefined(typeof(CardContentAuthority), contentAuthority) ||
                contentAuthority == CardContentAuthority.None)
            {
                throw new ArgumentException("Card definition requires explicit content authority.", nameof(contentAuthority));
            }

            if (actionDefinitions == null)
            {
                throw new ArgumentNullException(nameof(actionDefinitions));
            }

            Id = id;
            ContentRevision = contentRevision;
            DisplayName = displayName;
            Initiative = initiative;
            ContentAuthority = contentAuthority;
            var materializedActionDefinitions = actionDefinitions.ToArray();
            ActionDefinitions = Array.AsReadOnly(materializedActionDefinitions);

            if (ActionDefinitions.Any(actionDefinition => actionDefinition == null))
            {
                throw new ArgumentException("Action definitions must not contain null entries.", nameof(actionDefinitions));
            }

            if (ActionDefinitions.Select(actionDefinition => actionDefinition.Id).Distinct().Count() != ActionDefinitions.Count)
            {
                throw new ArgumentException("Action definitions must have unique local IDs.", nameof(actionDefinitions));
            }
        }

        public CardDefinitionId Id { get; }

        public string ContentRevision { get; }

        public string DisplayName { get; }

        public InitiativeValue Initiative { get; }

        public CardContentAuthority ContentAuthority { get; }

        public IReadOnlyList<ActionDefinition> ActionDefinitions { get; }

        public CardDefinitionReference Reference => new CardDefinitionReference(Id, ContentRevision);
    }
}
