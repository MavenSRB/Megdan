using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.21. Brogan's two faces that name a friendly hero, on the one staged board where a
    /// friendly hero exists.
    ///
    /// <para><b>Why this fixture has its own configuration.</b> War Drummer and Master Skald print "A
    /// friendly hero in range gains N coins", and rulebook p.10 keeps the acting hero out of what an
    /// action affects unless the text says so - which neither text does. Every other configuration
    /// this build composes deals one hero per team, so the selection has no candidate at all and the
    /// Skill stops at its first step under p.10's mandatory-step rule. That is the correct behaviour
    /// and it proves nothing about the cards, which is why the staged match below deals Arien and
    /// Brogan to the Blue team and leaves Wasp alone on Red.</para>
    ///
    /// <para>It is not the six-player setup and does not pretend to be one: milestone 8.25 still owns
    /// the official configuration and all six spawn assignments. This is one extra seat on the second
    /// Blue hero spawn the reviewed map has always declared, under the same `DEV-004`/`DEV-005` terms
    /// every staged configuration here is on.</para>
    /// </summary>
    public sealed class BroganAlliedHeroCardTests
    {
        [Test]
        public void TheStagedAllyMatch_DealsThreeSeatsWithBroganBesideAnAllyInRange()
        {
            var session = CreateAlliedSession(out var brogan, out var ally);

            Assert.That(session.Projection.Participants.Heroes, Has.Count.EqualTo(3),
                "The whole point of this configuration is a third seat; two would put Brogan alone " +
                "on his side again.");
            Assert.That(
                session.Projection.Participants.GetTeamForHero(brogan).Side,
                Is.EqualTo(session.Projection.Participants.GetTeamForHero(ally).Side),
                "A friendly hero is one on the acting hero's own team, which is what the cards name.");

            var broganAt = SpaceOf(session, brogan);
            var allyAt = SpaceOf(session, ally);
            Assert.That(HexDistanceQuery.Calculate(broganAt, allyAt), Is.LessThanOrEqualTo(4),
                "Both cards print Range 4, so an ally further than that would make the selection " +
                "empty for a reason that has nothing to do with the rule under test.");
            Assert.That(broganAt, Is.Not.EqualTo(allyAt),
                "Two heroes on one space would mean the staging placed them both at the same seat.");
        }

        [Test]
        public void WarDrummer_GrantsItsPrintedCoinsToAFriendlyHeroAndNeverToBrogan()
        {
            var session = CreateAlliedSession(out var brogan, out var ally, BroganAllyStage.TierTwo);
            var before = CoinsOf(session, ally);

            PlayBroganSkill(session, brogan, BroganCardContent.WarDrummer);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null,
                "The card names a friendly hero rather than a fixed one, so who receives is a " +
                "decision the actor makes.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { ally }),
                "Rulebook p.10 keeps the acting hero out of what the action affects unless the text " +
                "says so, and this text does not - so Brogan is not among his own candidates.");

            AnswerAndConfirm(session, choice);
            Assert.That(CoinsOf(session, ally), Is.EqualTo(before + 1),
                "No hero has been defeated this round, so the sentence's first amount is the one " +
                "that applies: 'gains 1 coin'.");
            Assert.That(CoinsOf(session, brogan), Is.EqualTo(0),
                "The coins go to the hero the card names, and Brogan named somebody else.");
        }

        [Test]
        public void MasterSkald_GrantsTheHigherPrintedAmountForTheSameSentence()
        {
            var session = CreateAlliedSession(out var brogan, out var ally, BroganAllyStage.TierThree);
            var before = CoinsOf(session, ally);

            PlayBroganSkill(session, brogan, BroganCardContent.MasterSkald);

            AnswerAndConfirm(session, session.Projection.PendingEffectChoice);
            Assert.That(CoinsOf(session, ally), Is.EqualTo(before + 2),
                "The Tier III card prints 2 where its Tier II sibling prints 1, and the two " +
                "compositions differ in nothing else.");
        }

        /// <summary>
        /// The staged Wasp-versus-Arien-and-Brogan match, composed from the same authority every other
        /// configured fixture composes from.
        /// </summary>
        private static MatchSession CreateAlliedSession(
            out HeroId brogan, out HeroId ally, BroganAllyStage stage = BroganAllyStage.StartingHand)
        {
            var configuration =
                PlaceholderOneVsOneTestConfiguration.CreateStagedBroganWithAllyForInternalTest(
                    TeamSide.Red, stage);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateStagedWaspVersusArienAndBroganOneVsTwo(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            brogan = new HeroId(PlaceholderOneVsOneParticipants.BlueAllyHeroIdValue);
            ally = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Commits every seat's card so that Brogan's is the one that acts, then starts its printed
        /// Skill. Brogan's Green cards print the lowest Initiative in his hand, which is what makes
        /// this deterministic rather than a race with the other two seats.
        /// </summary>
        private static void PlayBroganSkill(
            MatchSession session, HeroId brogan, CardDefinitionId cardId)
        {
            foreach (var hero in session.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var card = hero.Equals(brogan)
                    ? HandCard(session, hero, cardId)
                    : LowestInitiativeHandCard(session, hero);
                Assert.That(
                    session.Execute(DirectorCommand.CommitCard(hero, card.Id)).Accepted, Is.True,
                    $"Seat '{hero}' could not commit its card, so the turn under test never starts.");
            }

            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(brogan),
                "Brogan's War Drummer and Master Skald print Initiative 5, above the lowest card " +
                "either other seat holds, and p.7 resolves the higher Initiative first.");

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition =>
                        definition.Reference.Equals(candidate.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(candidate.ActionDefinitionId))
                    .Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(
                session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                Is.True);
        }

        /// <summary>
        /// Names the one candidate and then closes the decision.
        ///
        /// <para>Both commands are needed and neither stands for the other: a Select records who was
        /// named and a Confirm is the answer, which is why a player clicking a candidate on the board
        /// produces two accepted commands rather than one. A fixture that submitted only the first
        /// would leave the choice standing and read the state before the card had done anything.</para>
        /// </summary>
        private static void AnswerAndConfirm(MatchSession session, PendingEffectChoice choice)
        {
            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                choice.SelectInvocations.Single())).Accepted, Is.True);

            var open = session.Projection.PendingEffectChoice;
            if (open?.ConfirmInvocation != null)
            {
                Assert.That(session.Execute(
                    DirectorCommand.AnswerEffectChoice(open.ConfirmInvocation)).Accepted, Is.True);
            }
        }

        private static CardInstance HandCard(
            MatchSession session, HeroId ownerId, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(ownerId) &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        /// <summary>
        /// The card in this seat's hand that acts last, so it cannot take the turn from the seat under
        /// test. Rulebook p.7 resolves the higher printed Initiative first, so the card that yields is
        /// the lowest one this hand holds - chosen by the value the projection carries rather than by
        /// name, because the three seats deal three different printed sets.
        /// </summary>
        private static CardInstance LowestInitiativeHandCard(MatchSession session, HeroId ownerId) =>
            session.Projection.Cards
                .Where(card =>
                    card.OwnerId.Equals(ownerId) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();

        private static HexCoordinate SpaceOf(MatchSession session, HeroId heroId) =>
            session.Projection.Heroes.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static int CoinsOf(MatchSession session, HeroId heroId) =>
            session.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId))
                .Coins;
    }
}
