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
            TieBreakerState tieBreakerState)
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

            CardInitialSetupValidator.Validate(redHand.Definitions, cardInstances);
            CardDefinitions = redHand.Definitions;
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

        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHand = PlaceholderMovementHand.Create(
                new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                new CardInstanceId("red-route-drill-card"),
                new CardInstanceId("red-obstacle-course-card"));
            var blueHand = PlaceholderMovementHand.Create(
                new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue),
                new CardInstanceId("blue-route-drill-card"),
                new CardInstanceId("blue-obstacle-course-card"));

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                redHand,
                blueHand,
                tieBreakerState);
        }
    }
}
