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
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression)
            : this(
                id,
                contentRevision,
                displayName,
                initiative,
                contentAuthority,
                progression,
                Enumerable.Empty<ActionDefinition>())
        {
        }

        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression,
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

            if (progression == null)
            {
                // Deliberately not defaulted. Milestone 7.1's acceptance check requires every
                // definition to represent its applicable card colour or a verified absence, and a
                // required argument is what makes the absence a statement - CardProgressionProfile.
                // NotStated - instead of the value a caller happened not to pass.
                throw new ArgumentNullException(
                    nameof(progression),
                    "Card definition requires an explicit progression profile; state " +
                    "CardProgressionProfile.NotStated when the definition carries no colour or Tier.");
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
            Progression = progression;
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

        /// <summary>
        /// Milestone 7.1's printed colour, Tier, Basic classification, and same-colour next-Tier
        /// candidates. Never null: the constructor refuses a definition that states nothing rather than
        /// substituting an absence of its own.
        /// </summary>
        public CardProgressionProfile Progression { get; }

        public IReadOnlyList<ActionDefinition> ActionDefinitions { get; }

        public CardDefinitionReference Reference => new CardDefinitionReference(Id, ContentRevision);
    }
}
