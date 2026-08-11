using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Interaction
{
    public sealed class DirectorTestHand
    {
        public DirectorTestHand(TeamSide teamSide, PlaceholderMovementHand hand, string heroDisplayName = "Hero")
            : this(teamSide, hand?.Definitions, hand?.Instances, heroDisplayName)
        {
        }

        public DirectorTestHand(
            TeamSide teamSide,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            string heroDisplayName = "Hero")
        {
            TeamSide = teamSide;
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            Definitions = Array.AsReadOnly(definitions.ToArray());
            var values = instances.ToArray();
            OwnerId = values.Length > 0 ? values[0].OwnerId : default;
            Instances = Array.AsReadOnly(values
                .Where(instance => instance.State.Location == CardLocation.Hand)
                .ToArray());
            HeroDisplayName = string.IsNullOrWhiteSpace(heroDisplayName) ? "Hero" : heroDisplayName;
        }

        public TeamSide TeamSide { get; }

        public HeroId OwnerId { get; }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public string HeroDisplayName { get; }

        public string Label => TeamSide == GuardsDigital.Core.Participants.TeamSide.Red
            ? $"[R] RED {HeroDisplayName.ToUpperInvariant()}"
            : $"[B] BLUE {HeroDisplayName.ToUpperInvariant()}";

        public string GetCardButtonLabel(CardInstance instance)
        {
            if (instance == null || !Instances.Contains(instance))
            {
                throw new ArgumentException("The card must be visible in this director hand.", nameof(instance));
            }

            var definition = Definitions.Single(candidate =>
                candidate.Reference.Equals(instance.DefinitionReference));
            return $"{definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}";
        }
    }
}
