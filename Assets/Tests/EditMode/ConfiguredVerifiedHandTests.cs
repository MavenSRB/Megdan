using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
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
        /// <summary>
        /// The six official seats and the printed set each one holds, in rulebook p.6 setup step 7's
        /// order. Milestone 8.25; before it there were two, and before milestone 8.20 there was one set
        /// dealt twice.
        /// </summary>
        private sealed class SeatContent
        {
            public SeatContent(
                string heroIdValue,
                System.Collections.Generic.IReadOnlyList<CardDefinitionId> startingHand,
                System.Collections.Generic.IReadOnlyList<CardDefinitionId> deck,
                System.Collections.Generic.IReadOnlyList<CardDefinitionId> boxed,
                string revision)
            {
                HeroId = new HeroId(heroIdValue);
                StartingHand = startingHand;
                Deck = deck;
                Boxed = boxed;
                Revision = revision;
            }

            public HeroId HeroId { get; }
            public System.Collections.Generic.IReadOnlyList<CardDefinitionId> StartingHand { get; }
            public System.Collections.Generic.IReadOnlyList<CardDefinitionId> Deck { get; }
            public System.Collections.Generic.IReadOnlyList<CardDefinitionId> Boxed { get; }
            public string Revision { get; }
        }

        private static SeatContent[] OfficialSeats() => new[]
        {
            new SeatContent(
                OfficialSixPlayerParticipants.WaspHeroIdValue,
                WaspCardContent.StartingHand, WaspCardContent.Deck,
                WaspCardContent.ReturnedToBoxAtSetup, WaspCardContent.ContentRevision),
            new SeatContent(
                OfficialSixPlayerParticipants.XargathaHeroIdValue,
                XargathaCardContent.StartingHand, XargathaCardContent.Deck,
                XargathaCardContent.ReturnedToBoxAtSetup, XargathaCardContent.ContentRevision),
            new SeatContent(
                OfficialSixPlayerParticipants.TigerclawHeroIdValue,
                TigerclawCardContent.StartingHand, TigerclawCardContent.Deck,
                TigerclawCardContent.ReturnedToBoxAtSetup, TigerclawCardContent.ContentRevision),
            new SeatContent(
                OfficialSixPlayerParticipants.ArienHeroIdValue,
                ArienCardContent.StartingHand, ArienCardContent.Deck,
                ArienCardContent.ReturnedToBoxAtSetup, ArienCardContent.ContentRevision),
            new SeatContent(
                OfficialSixPlayerParticipants.BroganHeroIdValue,
                BroganCardContent.StartingHand, BroganCardContent.Deck,
                BroganCardContent.ReturnedToBoxAtSetup, BroganCardContent.ContentRevision),
            new SeatContent(
                OfficialSixPlayerParticipants.SabinaHeroIdValue,
                SabinaCardContent.StartingHand, SabinaCardContent.Deck,
                SabinaCardContent.ReturnedToBoxAtSetup, SabinaCardContent.ContentRevision)
        };

        /// <summary>
        /// Milestone 8.25 made this six heroes rather than two, so the case asks each of rulebook p.5's
        /// seats about its own printed set. p.6's rule is the same for all six, which is the point: a
        /// starting hand is three Tier I cards and two untiered basics whoever is holding it.
        /// </summary>
        [Test]
        public void EveryConfiguredHand_HoldsThePrintedStartingFiveWithTheRemainingThirteenInTheDeck()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            Assert.That(configuration.Hands, Has.Count.EqualTo(6),
                "p.5's six-player setup deals one hand per seat.");

            foreach (var seat in OfficialSeats())
            {
                var hand = configuration.Hands.Single(candidate =>
                    candidate.AllInstances.All(card => card.OwnerId.Equals(seat.HeroId)));
                Assert.That(
                    hand.Instances.Select(card => card.DefinitionReference.DefinitionId).ToArray(),
                    Is.EquivalentTo(seat.StartingHand),
                    $"p.6 deals {seat.HeroId} the three Tier I cards and the two untiered basics.");
                Assert.That(
                    hand.DeckInstances
                        .Select(card => card.DefinitionReference.DefinitionId).ToArray(),
                    Is.EquivalentTo(seat.Deck),
                    "and returns the remaining Tier II, Tier III and Tier IV cards to the deck.");
                Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
                Assert.That(
                    hand.AllInstances.All(card =>
                        card.State.IsInitialInHand || card.State.IsInitialInDeck),
                    Is.True);
                Assert.That(
                    hand.Definitions.All(definition =>
                        definition.ContentAuthority == CardContentAuthority.VerifiedOfficial &&
                        definition.ContentRevision == seat.Revision),
                    Is.True,
                    "Nothing synthetic stands in for hero content on the path a match takes.");
            }
        }

        [Test]
        public void NoHandicapCard_IsInstantiatedAnywhereInTheConfiguredMatch()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var boxed = OfficialSeats().SelectMany(seat => seat.Boxed).ToArray();

            Assert.That(boxed, Has.Length.EqualTo(6),
                "All six dealt heroes print a p.20 Handicap card, or this holds over less than it says.");
            foreach (var id in boxed)
            {
                Assert.That(
                    configuration.CardInstances.Any(card =>
                        card.DefinitionReference.DefinitionId.Equals(id)),
                    Is.False,
                    "p.6 returns the Handicap card to the box, so no seat may hold an instance of it.");
                Assert.That(
                    configuration.CardDefinitions.Any(definition => definition.Id.Equals(id)),
                    Is.False,
                    "and a definition nothing can be dealt is not content this match resolves against.");
            }
        }

        /// <summary>
        /// Milestone 8.25. The official match holds six printed sets where the staged one held two and
        /// the mirror held one dealt twice - which is what moves the player-build smoke gate's pinned
        /// marker from `heroes=2 cards=36` to `heroes=6 cards=108`, and what makes this the first
        /// milestone at which `RISK-SCALE-001`'s 108 instances and 108 definitions exist.
        /// </summary>
        [Test]
        public void TheSixSeats_HoldSixPrintedSetsAndOneHundredAndEightDistinctInstances()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            Assert.That(configuration.CardDefinitions.Count, Is.EqualTo(108),
                "Six heroes are six printed sets of eighteen, and no two seats share one.");
            Assert.That(configuration.CardInstances.Count, Is.EqualTo(108),
                "which is the player-build smoke gate's pinned cards=108 marker: each seat holds " +
                "eighteen cards.");
            Assert.That(
                configuration.CardInstances.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(108),
                "Instance identity is per seat.");
            Assert.That(
                configuration.CardInstances.Select(card => card.OwnerId).Distinct().Count(),
                Is.EqualTo(6));
            Assert.That(
                configuration.CardDefinitions
                    .Select(definition => definition.ContentRevision).Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(OfficialSeats()
                    .Select(seat => seat.Revision)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "and each set moves as its own hero's revision.");
        }

        /// <summary>
        /// The other side of the milestone 8.4 honesty rule, and what milestone 8.9 changed about it.
        ///
        /// Static Barrier used to be this fixture's Skill-shaped subject, chosen because a Skill
        /// carrying no <c>EffectRoot</c> passes both other availability rules by construction - 6.5af's
        /// asks whether the effect can apply anything and answers true when there is no effect to ask
        /// about, and 7.8a1's answers true for anything that is not an Attack - so a refusal could only
        /// have come from the composition rule. Milestone 8.9 composed its text, and with it every one
        /// of Wasp's Skill primaries, so that subject no longer exists and the rule's remaining
        /// subjects are the Attacks and Defenses of milestones 8.10 and 8.11. The sibling case below
        /// carries the refusal on one of them, in a position where 7.8a1's rule provably cannot be the
        /// cause.
        ///
        /// What is left to state here is the release: the card whose primary this fixture used to find
        /// greyed is offered as takeable and starts, and it owes nothing.
        /// </summary>
        [Test]
        public void TheSkillPrimaryThisRuleUsedToGrey_IsOfferedAndStartsOnceItsTextIsComposed()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.StaticBarrier, 13);

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == ActionKind.Skill);
            Assert.That(ActionOf(session, offer).TextComposition.IsAwaited, Is.False,
                "Milestone 8.9 composed Static Barrier's printed text.");
            Assert.That(ActionOf(session, offer).EffectRoot, Is.Not.Null,
                "and the composition is an effect root on the action rather than a cleared debt row.");
            Assert.That(offer.IsAvailable, Is.True,
                "A primary that owes nothing is offered as takeable.");
            Assert.That(
                session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                Is.True,
                "and starts, which is the refusal this fixture used to record being released.");
        }

        /// <summary>
        /// What milestone 8.10 changed about the same rule, on the Attack side.
        ///
        /// <para>Shock used to be this fixture's Attack-shaped subject, chosen for a position where
        /// milestone 7.8a1's no-target rule provably could not be the cause: an enemy minion stands
        /// beside the attacker, so the Attack has somebody to hit and only the composition rule could
        /// have greyed it. Milestone 8.10 composed Shock's printed text, and with it every Attack of
        /// Wasp's that a starting hand can hold, so that subject no longer exists either.</para>
        ///
        /// <para><b>The rule now has no turn-action subject in Wasp's set at all</b>, and that is
        /// recorded here rather than left to be discovered. Its remaining verified subjects are the
        /// three Defense primaries milestone 8.11 owes, which are performed in reaction rather than on
        /// a turn and which no start site asks this question of yet; Crackling Dagger's deferral is a
        /// fourth, and p.5 returns that card to the box before a hand exists. Milestone 8.11 therefore
        /// owes this rule its refusal case, on the path it reaches a Defense by. What is left to state
        /// here is the release, on the same board that used to record the refusal.</para>
        /// </summary>
        [Test]
        public void TheAttackPrimaryThisRuleUsedToGrey_IsOfferedAndStartsOnceItsTextIsComposed()
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
                "the availability below is not milestone 7.8a1's no-target rule answering instead.");

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == ActionKind.Attack);
            Assert.That(ActionOf(session, offer).TextComposition.IsAwaited, Is.False,
                "Milestone 8.10 composed Shock's printed text.");
            Assert.That(ActionOf(session, offer).EffectRoot, Is.Not.Null,
                "and its rider is an effect root on the action rather than a cleared debt row.");
            Assert.That(offer.IsAvailable, Is.True,
                "A primary that owes nothing is offered as takeable.");
            Assert.That(
                session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                Is.True,
                "and starts, which is the refusal this fixture used to record being released.");
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
                    session.Projection.Cards, RuleModificationSet.Empty).HasValue,
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
