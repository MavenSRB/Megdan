using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.4: the configured match deals Wasp's verified cards, and the honesty rule that
    /// makes dealing them safe before their text has been composed.
    ///
    /// <para>Every case here drives a real <see cref="MatchSession"/>, because the subject is what a
    /// player is dealt and offered in a position a match can actually be in - not what a content class
    /// returns when asked directly. The exceptions are the composition inventories, which are about the
    /// configuration itself.</para>
    /// </summary>
    public sealed class ConfiguredVerifiedHandTests
    {
        [Test]
        public void BothConfiguredHands_HoldThePrintedStartingFiveWithTheRemainingThirteenInTheDeck()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            foreach (var hand in new[] { configuration.RedHand, configuration.BlueHand })
            {
                Assert.That(
                    hand.Instances.Select(card => card.DefinitionReference.DefinitionId).ToArray(),
                    Is.EquivalentTo(WaspCardContent.StartingHand),
                    "p.5 deals the three Tier I cards and the two untiered basics.");
                Assert.That(
                    hand.DeckInstances.Select(card => card.DefinitionReference.DefinitionId).ToArray(),
                    Is.EquivalentTo(WaspCardContent.Deck),
                    "and returns the remaining Tier II, Tier III and Tier IV cards to the deck.");
                Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
                Assert.That(
                    hand.AllInstances.All(card =>
                        card.State.IsInitialInHand || card.State.IsInitialInDeck),
                    Is.True);
                Assert.That(
                    hand.Definitions.All(definition =>
                        definition.ContentAuthority == CardContentAuthority.VerifiedOfficial &&
                        definition.ContentRevision == WaspCardContent.ContentRevision),
                    Is.True,
                    "Nothing synthetic stands in for hero content on the path a match takes.");
            }
        }

        [Test]
        public void TheHandicapCard_IsNotInstantiatedAnywhereInTheConfiguredMatch()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            Assert.That(WaspCardContent.ReturnedToBoxAtSetup, Is.Not.Empty,
                "or this holds over nothing.");
            foreach (var boxed in WaspCardContent.ReturnedToBoxAtSetup)
            {
                Assert.That(
                    configuration.CardInstances.Any(card =>
                        card.DefinitionReference.DefinitionId.Equals(boxed)),
                    Is.False,
                    "p.5 returns the Handicap card to the box, so no seat may hold an instance of it.");
                Assert.That(
                    configuration.CardDefinitions.Any(definition => definition.Id.Equals(boxed)),
                    Is.False,
                    "and a definition nothing can be dealt is not content this match resolves against.");
            }
        }

        [Test]
        public void TheMirroredSeats_ShareOnePrintedSetAndHoldThirtySixDistinctInstances()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            Assert.That(configuration.CardDefinitions.Count, Is.EqualTo(18),
                "A mirror is one printed set dealt twice, not two sets.");
            Assert.That(configuration.CardInstances.Count, Is.EqualTo(36),
                "which is the player-build smoke gate's pinned cards=36 marker.");
            Assert.That(
                configuration.CardInstances.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(36),
                "Instance identity is per seat even when the definition is shared.");
            Assert.That(
                configuration.CardInstances.Select(card => card.OwnerId).Distinct().Count(),
                Is.EqualTo(2));
        }

        /// <summary>
        /// The milestone's honesty rule, on the primary kind that cannot be greyed by anything else.
        ///
        /// Static Barrier's primary is a Skill carrying no <c>EffectRoot</c>, and both existing
        /// availability rules answer true for exactly that shape: milestone 6.5af's asks whether the
        /// effect can apply anything and returns true when there is no effect to ask about, and
        /// milestone 7.8a1's returns true for anything that is not an Attack. So this offer is
        /// available and startable by construction until the card-text rule exists, which is what makes
        /// the observation structural rather than a position that happened to refuse.
        ///
        /// Running it would perform part of a card and silently drop the rest - a rules violation the
        /// match cannot see; p.12 lets the hero Hold instead, so nothing they were owed is taken away.
        /// </summary>
        [Test]
        public void ASkillPrimaryWhoseCardTextHasNoComposition_IsOfferedGreyedAndRefusesTheStart()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.StaticBarrier, 13);

            var offer = SingleAwaitedPrimary(session, ActionKind.Skill);
            Assert.That(ActionOf(session, offer).EffectRoot, Is.Null,
                "or the two existing availability rules could have decided this offer themselves.");

            AssertGreyedAndRefusedAtomically(session, offer);
        }

        /// <summary>
        /// The same rule on an Attack, in a position where milestone 7.8a1's rule provably cannot be
        /// the cause: an enemy minion stands beside the attacker, which is the position that fixture
        /// exists to prove an Attack offer stays available in.
        /// </summary>
        [Test]
        public void AnAttackPrimaryWhoseCardTextHasNoComposition_IsGreyedEvenWithATargetBesideIt()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorWithAnAdjacentEnemyMinion(out _);
            CommitAgainstStopProjectiles(session, WaspCardContent.Shock, 8);

            var actor = session.Projection.SelectedActor.ActorId;
            var attackerCoordinate = session.Projection.Heroes
                .Single(occupancy => occupancy.HeroId.Equals(actor)).Coordinate;
            Assert.That(
                session.Projection.Minions.Any(minion =>
                    attackerCoordinate.GetNeighbors().Any(neighbor =>
                        neighbor.Equals(minion.Coordinate))),
                Is.True,
                "An enemy minion stands adjacent, so Shock's non-Ranged Attack has somebody to hit and " +
                "the greying below cannot be milestone 7.8a1's no-target rule.");

            AssertGreyedAndRefusedAtomically(session, SingleAwaitedPrimary(session, ActionKind.Attack));
        }

        [Test]
        public void TheGreyingIsTargeted_LeavingTheSameCardsSecondaryAndGeneralRuleActionsTakeable()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.Shock, 8);

            var secondaries = session.Projection.AvailableTurnActions
                .Where(offer => ActionOf(session, offer).Origin == ActionDefinitionOrigin.PrintedSecondary)
                .ToArray();
            Assert.That(secondaries, Is.Not.Empty,
                "The acting card prints a secondary, or this holds over nothing.");
            Assert.That(secondaries.All(offer => offer.IsAvailable), Is.True,
                "A secondary Movement prints no text, so nothing about it is missing.");

            Assert.That(
                GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                    session.Projection.SelectedActor.CardInstanceId,
                    session.Projection.SelectedActor.ActorId,
                    session.Projection.Definitions,
                    session.Projection.Cards).HasValue,
                Is.True,
                "p.12's Hold keeps the card spendable while its text is owed.");
        }

        /// <summary>
        /// A round of the verified mirror played through the already-proven turn controller: commit,
        /// reveal, Initiative order, Hold, and a secondary Movement. Every value the round reads is one
        /// Wasp prints.
        /// </summary>
        [Test]
        public void TheVerifiedMirror_PlaysCommitRevealInitiativeAndSpendsTurnsOnPrintedValues()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();

            var red = HandCard(session, TeamSide.Red, WaspCardContent.StaticBarrier);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "Static Barrier prints Initiative 13 and Stop Projectiles 3, so the reveal is not tied.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "and the printed Initiative alone decides who acts first.");

            // p.12: the hero may Hold rather than act, which is what keeps a card whose primary has no
            // composition spendable.
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(red.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(blue.Id));

            // Stop Projectiles prints a Movement 2 secondary; the Fast Travel p.13 lets a hero take
            // instead of it is offered against that same printed action.
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                ActionOf(session, offer).Kind == ActionKind.Movement);
            Assert.That(movement.IsAvailable, Is.True);
            Assert.That(
                ActionOf(session, movement).BaseValues
                    .Single(value => value.Channel == PrintedValueChannel.Movement).Amount,
                Is.EqualTo(2),
                "which is the value the printed face carries.");
            Assert.That(
                session.Projection.AvailableFastTravelOptions.Any(option =>
                    option.ReplacedMovementActionDefinitionId.Equals(movement.ActionDefinitionId)),
                Is.True,
                "p.13's Fast Travel replaces that printed Movement.");

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(movement.ActionDefinitionId));
            var destinations = session.Projection.PendingMovementChoice.LegalDestinations;
            Assert.That(destinations.Any(destination => destination.Cost == 2), Is.True,
                "and the printed 2 is the distance the move is allowed to reach.");
            Assert.That(destinations.All(destination => destination.Cost <= 2), Is.True,
                "and no further.");
        }

        /// <summary>
        /// p.14's Defense reaction answered from a verified printed value.
        ///
        /// The attacker holds showcase content on purpose: no Wasp hero can attack until the 8.10 slice
        /// composes an Attack's text, because this milestone's own rule refuses every printed primary
        /// whose composition is owed. Nothing synthetic decides anything about the defence - the offers,
        /// the Defense value and the discard are all Wasp's printed content.
        /// </summary>
        [Test]
        public void AVerifiedSecondaryDefense_AnswersARealAttackFromItsPrintedValue()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedDefenderSession();

            var red = session.Projection.Cards.Single(card =>
                card.State.Location == CardLocation.Hand && card.Id.Value == "red-pursuit-card");
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "Red Pursuit prints Initiative 7 against Stop Projectiles' 3.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionOf(session, offer).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));

            var choice = session.Projection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(
                choice.LegalDefenseOffers
                    .Select(offer => offer.SourceDefinitionReference.DefinitionId).ToArray(),
                Is.EquivalentTo(new[]
                {
                    WaspCardContent.MagneticDagger,
                    WaspCardContent.StaticBarrier,
                    WaspCardContent.Shock,
                    WaspCardContent.LiftUp
                }),
                "Exactly the four cards still in hand whose faces print a secondary Defense icon; " +
                "Stop Projectiles prints none, which is why its Green siblings are Block cards.");

            var shock = choice.LegalDefenseOffers.Single(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(WaspCardContent.Shock));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseDefenseCard(
                choice.ResolutionId, choice.ChoiceId, shock.SourceCardInstanceId));

            Assert.That(
                session.Projection.DiagnosticEvents.Any(entry =>
                    entry.EventType == "Action.AttackSuccessfullyDefended" &&
                    entry.Detail.Contains("defense=6")),
                Is.True,
                "Shock's printed Defense 6 answers Red Pursuit's printed Attack 4, from the face and " +
                "from nowhere else.");
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(shock.SourceCardInstanceId))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and the reaction spends the card the printed face says it spends.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Red commits the named card and Blue commits Stop Projectiles, whose printed Initiative 3 is
        /// the lowest in the starting hand, so the Red card acts first on its printed value alone.
        /// </summary>
        private static void CommitAgainstStopProjectiles(
            MatchSession session, CardDefinitionId redCardId, int expectedRedInitiative)
        {
            var red = HandCard(session, TeamSide.Red, redCardId);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(red.DefinitionReference))
                    .Initiative.Value,
                Is.EqualTo(expectedRedInitiative),
                "The printed Initiative this case reasons from.");

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        /// <summary>The acting card's one printed primary of that kind, which owes its composition.</summary>
        private static ActionOffer SingleAwaitedPrimary(MatchSession session, ActionKind kind)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == kind);
            Assert.That(ActionOf(session, offer).TextComposition.IsAwaited, Is.True,
                "The acting card's primary must still owe its 8.5-8.13 composition, or this case " +
                "holds over nothing.");
            return offer;
        }

        private static void AssertGreyedAndRefusedAtomically(MatchSession session, ActionOffer offer)
        {
            var action = ActionOf(session, offer);
            Assert.That(offer.IsAvailable, Is.False,
                "A primary that owes its composition (" + action.TextComposition +
                ") cannot be offered as takeable.");

            var before = session.Projection;
            var start = session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId));
            Assert.That(start.Accepted, Is.False);
            Assert.That(start.TurnRejectionReason,
                Is.EqualTo(TurnResolutionRejectionReason.PrintedCardTextNotComposed),
                "and the refusal names the missing composition rather than blaming the board.");

            // Atomically: nothing pending was created and the turn is exactly where it was.
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(session.Projection.PendingAttackResolution, Is.Null);
            Assert.That(session.Projection.IsTurnActive, Is.True);
            Assert.That(session.Projection.AcceptedCommands.Count,
                Is.EqualTo(before.AcceptedCommands.Count));
        }

        private static CardInstance HandCard(MatchSession session, TeamSide side, CardDefinitionId id) =>
            session.Projection.Cards.Single(card =>
                Side(session, card) == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(id));

        private static TeamSide Side(MatchSession session, CardInstance card) =>
            session.Projection.Participants.GetTeamForHero(card.OwnerId).Side;

        private static ActionDefinition ActionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));
    }
}
