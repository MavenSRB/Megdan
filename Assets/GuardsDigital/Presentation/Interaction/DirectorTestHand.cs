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
        public DirectorTestHand(TeamSide teamSide, PlaceholderMovementHand hand)
        {
            TeamSide = teamSide;
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand));
            }

            Definitions = hand.Definitions;
            OwnerId = hand.Instances.Count > 0 ? hand.Instances[0].OwnerId : default;
            Instances = Array.AsReadOnly(hand.Instances
                .Where(instance => instance.State.Location == CardLocation.Hand)
                .ToArray());
        }

        public TeamSide TeamSide { get; }

        public HeroId OwnerId { get; }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public string Label => TeamSide == GuardsDigital.Core.Participants.TeamSide.Red
            ? "[R] RED HERO"
            : "[B] BLUE HERO";

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
