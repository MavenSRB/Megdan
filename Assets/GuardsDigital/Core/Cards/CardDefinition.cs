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
            : this(id, contentRevision, displayName, initiative, contentAuthority, progression,
                actionDefinitions, null)
        {
        }

        /// <summary>
        /// Milestone 7.3's overload, and the only one that can build rulebook p.16's Ultimate.
        ///
        /// <paramref name="ultimateRule"/> is the permanent ability the Ultimate prints, and it is
        /// required for an Ultimate and refused for everything else. Both directions matter: an
        /// Ultimate with no ability is a card whose entire printed content is missing, and an ordinary
        /// card with one would supply a rule from a card the page gives no permanent ability to.
        /// </summary>
        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression,
            IEnumerable<ActionDefinition> actionDefinitions,
            UltimateRuleDefinition ultimateRule)
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

            // Milestone 7.3. Initiative is required of every card except p.16's Ultimate, which prints
            // none: the printed card carries no value column at all, and it never enters the Initiative
            // order because it is never played - the page places it "to the left side of your hero
            // dashboard" and its ability "is always in effect". Giving one an Initiative would print a
            // value the card does not have, which is the mistake milestone 7.2c corrected elsewhere.
            if (progression.IsUltimate)
            {
                if (initiative.IsDefined)
                {
                    throw new ArgumentException(
                        "An Ultimate prints no Initiative; it is never played (rulebook p.16).",
                        nameof(initiative));
                }
            }
            else if (!initiative.IsDefined)
            {
                throw new ArgumentException(
                    "Card definition requires a positive Initiative value.", nameof(initiative));
            }

            if (progression.IsUltimate == (ultimateRule == null))
            {
                throw new ArgumentException(
                    progression.IsUltimate
                        ? "An Ultimate states the permanent ability it supplies (rulebook p.16)."
                        : "Only an Ultimate carries a permanent ability; this card is " +
                          $"{progression.Classification}.",
                    nameof(ultimateRule));
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
            UltimateRule = ultimateRule;
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

        /// <summary>
        /// Milestone 7.3's permanent ability, on an Ultimate and on nothing else. The constructor
        /// refuses both mismatches, so a non-null value here is a card p.16 gives an always-on rule to
        /// and a null one is a card it does not.
        /// </summary>
        public UltimateRuleDefinition UltimateRule { get; }

        public IReadOnlyList<ActionDefinition> ActionDefinitions { get; }

        public CardDefinitionReference Reference => new CardDefinitionReference(Id, ContentRevision);
    }
}
