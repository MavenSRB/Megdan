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

        /// <summary>
        /// The ordinary five cards per hero, and what the configured scene deals.
        ///
        /// It had a named sibling of its own between milestones 6.2d and the five-card scene, because
        /// the scene was dealt a sixth card - milestone 6.2's Cache - so a player could put a token on
        /// the board at all. That sibling is gone rather than renamed: Blue Feint absorbed the Cache
        /// card's three modes, so the scene's hand and the hand every pinned digest, corpus fixture and
        /// card-matrix assertion measures are the same five again, and there is nothing left for a
        /// second factory to say.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration Create()
        {
            return Create(
                TieBreakerSetupAuthority.CreateNormalSetupState(),
                PlaceholderMovementHand.CreateRed,
                PlaceholderMovementHand.CreateBlue);
        }

        public static PlaceholderOneVsOneTestConfiguration CreateForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                PlaceholderMovementHand.CreateRed,
                PlaceholderMovementHand.CreateBlue);
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
                PlaceholderMovementHand.CreateRedWithSkill,
                PlaceholderMovementHand.CreateBlueWithSkill);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 6.7's Repeat-bearing card in each hand.
        ///
        /// Separate for the same reason the three before it are, and for one more: a Repeat's subject is
        /// the whole enclosing action, so adding it to an existing fixture Skill would give every
        /// fixture that plays that Skill a decision it never asked about.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForRepeatFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                PlaceholderMovementHand.CreateRedWithRepeatSkill,
                PlaceholderMovementHand.CreateBlueWithRepeatSkill);
        }

        /// <summary>
        /// Each named configuration says which hand it deals rather than setting a flag another one
        /// reads. Milestone 6.2d needed a third combination and a second boolean would have made the
        /// callers here describe themselves by what they are not. The five-card scene removed that
        /// combination again, and the shape stayed, because it is what lets the three fixture hands
        /// differ from the ordinary one without any of them naming a flag.
        /// </summary>
        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState,
            Func<HeroId, PlaceholderMovementHand> redHand,
            Func<HeroId, PlaceholderMovementHand> blueHand)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                redHand(redHeroId),
                blueHand(blueHeroId),
                tieBreakerState);
        }
    }
}
