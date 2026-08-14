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

        public PlaceholderConfigurationRestoreState CaptureRestoreState() =>
            new PlaceholderConfigurationRestoreState(
                Participants,
                CardDefinitions,
                CardInstances,
                TieBreakerState);

        public static PlaceholderOneVsOneTestConfiguration Restore(
            PlaceholderConfigurationRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var participants = state.Participants;
            var definitionArray = state.Definitions;
            var instanceArray = state.Instances;
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            PlaceholderMovementHand BuildHand(HeroId ownerId)
            {
                var ownedInstances = instanceArray.Where(instance => instance.OwnerId.Equals(ownerId)).ToArray();
                var ownedReferences = new HashSet<CardDefinitionReference>(
                    ownedInstances.Select(instance => instance.DefinitionReference));
                return PlaceholderMovementHand.Restore(new PlaceholderMovementHandRestoreState(
                    definitionArray.Where(definition => ownedReferences.Contains(definition.Reference)),
                    ownedInstances));
            }

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                BuildHand(redHero),
                BuildHand(blueHero),
                state.TieBreakerState,
                false);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 6.2a's Skill-bearing card in each hand.
        ///
        /// Separate from the default deliberately: the default configuration is what every pinned
        /// digest and card-matrix fixture measures, so the Skill fixture must not change it.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForSkillFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                true);
        }

        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState,
            bool withSkillCard = false)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var redHand = withSkillCard
                ? PlaceholderMovementHand.CreateRedWithSkill(redHeroId)
                : PlaceholderMovementHand.CreateRed(redHeroId);
            var blueHand = withSkillCard
                ? PlaceholderMovementHand.CreateBlueWithSkill(blueHeroId)
                : PlaceholderMovementHand.CreateBlue(blueHeroId);

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                redHand,
                blueHand,
                tieBreakerState);
        }
    }
}
