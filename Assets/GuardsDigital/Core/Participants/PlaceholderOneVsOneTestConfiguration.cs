using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    public sealed class PlaceholderOneVsOneTestConfiguration
    {
        private PlaceholderOneVsOneTestConfiguration(
            MatchParticipants participants,
            PlaceholderMovementHand redHand,
            PlaceholderMovementHand blueHand,
            TieBreakerState tieBreakerState,
            bool validateInitialCardState = true)
        {
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            RedHand = redHand ?? throw new ArgumentNullException(nameof(redHand));
            BlueHand = blueHand ?? throw new ArgumentNullException(nameof(blueHand));
            if (!tieBreakerState.IsDefined)
            {
                throw new ArgumentException(
                    "The test configuration requires authoritative Tie Breaker setup state.",
                    nameof(tieBreakerState));
            }

            TieBreakerState = tieBreakerState;

            var cardInstances = redHand.Instances.Concat(blueHand.Instances).ToArray();
            foreach (var instance in cardInstances)
            {
                participants.GetHero(instance.OwnerId);
            }

            var cardDefinitions = redHand.Definitions.Concat(blueHand.Definitions).ToArray();
            if (validateInitialCardState)
            {
                CardInitialSetupValidator.Validate(cardDefinitions, cardInstances);
            }
            CardDefinitions = Array.AsReadOnly(cardDefinitions);
            CardInstances = Array.AsReadOnly(cardInstances);
        }

        public MatchParticipants Participants { get; }

        public PlaceholderMovementHand RedHand { get; }

        public PlaceholderMovementHand BlueHand { get; }

        public TieBreakerState TieBreakerState { get; }

        public IReadOnlyList<CardDefinition> CardDefinitions { get; }

        public IReadOnlyList<CardInstance> CardInstances { get; }

        public static PlaceholderOneVsOneTestConfiguration Create()
        {
            return Create(TieBreakerSetupAuthority.CreateNormalSetupState());
        }

        public static PlaceholderOneVsOneTestConfiguration CreateForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide));
        }

        public static PlaceholderOneVsOneTestConfiguration Restore(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            TieBreakerState tieBreakerState)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            var definitionArray = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var instanceArray = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            PlaceholderMovementHand BuildHand(HeroId ownerId)
            {
                var ownedInstances = instanceArray.Where(instance => instance.OwnerId.Equals(ownerId)).ToArray();
                var ownedReferences = new HashSet<CardDefinitionReference>(
                    ownedInstances.Select(instance => instance.DefinitionReference));
                return PlaceholderMovementHand.Restore(
                    definitionArray.Where(definition => ownedReferences.Contains(definition.Reference)),
                    ownedInstances);
            }

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                BuildHand(redHero),
                BuildHand(blueHero),
                tieBreakerState,
                false);
        }

        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHand = PlaceholderMovementHand.CreateRed(
                new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue));
            var blueHand = PlaceholderMovementHand.CreateBlue(
                new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue));

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                redHand,
                blueHand,
                tieBreakerState);
        }
    }
}
