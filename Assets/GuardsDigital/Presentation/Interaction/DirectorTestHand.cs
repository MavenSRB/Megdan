using System;
using System.Collections.Generic;
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
            Instances = hand.Instances;
            OwnerId = Instances.Count > 0 ? Instances[0].OwnerId : default;
        }

        public TeamSide TeamSide { get; }

        public HeroId OwnerId { get; }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public string Label => TeamSide == GuardsDigital.Core.Participants.TeamSide.Red
            ? "[R] RED HERO"
            : "[B] BLUE HERO";
    }
}
