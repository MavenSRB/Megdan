using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Test-only Tier III branch: both basics and one Tier III face of each colour.</summary>
    public static class EmmittProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                EmmittCardContent.ReverseTime,
                EmmittCardContent.UnstableTimeline,
                EmmittCardContent.TemporalJudgement,
                EmmittCardContent.TimeBomb,
                EmmittCardContent.BackToTheFuture
            };
            var cards = EmmittCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(EmmittCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
