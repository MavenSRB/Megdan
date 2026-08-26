using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class FastTravelActionTests
    {
        [Test]
        public void SameZoneFastTravel_ReplacesMovementAndCommitsOnePathlessRelocation()
        {
            var fixture = CreateFixture("red-throne");
            var destination = fixture.EmptyCoordinate("red-throne");

            var result = fixture.Perform(destination);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(fixture.Board.Occupancies.Single(value => value.HeroId.Equals(fixture.ActorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(result.Events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.FastTravelActionStarted,
                CardActionResolutionEventKind.FastTravelCommitted,
                CardActionResolutionEventKind.FastTravelActionCompleted
            }));
            Assert.That(result.Events, Has.None.Matches<CardActionResolutionEvent>(value =>
                value.Kind == CardActionResolutionEventKind.MovementStepCommitted));
            Assert.That(result.Events[1].FastTravelEvent.OriginZoneId, Is.EqualTo(new ZoneId("red-throne")));
            Assert.That(result.Events[1].FastTravelEvent.DestinationZoneId, Is.EqualTo(new ZoneId("red-throne")));
            Assert.That(result.Events[1].ReplacedActionDefinitionId, Is.EqualTo(fixture.MovementActionId));
        }

        [Test]
        public void AdjacentZoneFastTravel_DoesNotRequireAnyEmptySpacePath()
        {
            var blockedFixture = CreateFixture("red-throne", surroundActorWithObstacles: true);
            var destination = blockedFixture.EmptyCoordinate("red-approach");

            var result = blockedFixture.Perform(destination);

            Assert.That(result.Accepted, Is.True);
            Assert.That(blockedFixture.Board.Occupancies.Single(value =>
                value.HeroId.Equals(blockedFixture.ActorId)).Coordinate, Is.EqualTo(destination));
            Assert.That(result.Events[1].FastTravelEvent.DestinationZoneId,
                Is.EqualTo(new ZoneId("red-approach")));
        }

        [Test]
        public void DestinationQuery_ProjectsOnlyCurrentAuthoritativeSameOrAdjacentZoneChoices()
        {
            var fixture = CreateFixture("red-throne");
            var sameZone = fixture.EmptyCoordinate("red-throne");
            var adjacentZone = fixture.EmptyCoordinate("red-approach");
            var nonAdjacentZone = fixture.EmptyCoordinate("starting-battle-zone");

            var destinations = fixture.LegalDestinations();

            Assert.That(destinations, Does.Contain(sameZone));
            Assert.That(destinations, Does.Contain(adjacentZone));
            Assert.That(destinations.Contains(nonAdjacentZone), Is.False);
            Assert.That(destinations.Contains(fixture.ActorCoordinate), Is.False);
        }

        [Test]
        public void NonAdjacentZoneFastTravel_IsRejectedAtomically()
        {
            var fixture = CreateFixture("red-throne");
            var destination = fixture.EmptyCoordinate("starting-battle-zone");
            var before = fixture.ActorCoordinate;

            var result = fixture.Perform(destination);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.FastTravelZoneNotEligible));
            Assert.That(result.Events, Is.Empty);
            Assert.That(fixture.Board.Occupancies.Single(value => value.HeroId.Equals(fixture.ActorId)).Coordinate,
                Is.EqualTo(before));
        }

        [TestCase("red-throne", "red-throne", CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInOriginZone)]
        [TestCase("red-throne", "red-approach", CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInDestinationZone)]
        public void EnemyHeroInOriginOrDestinationZone_BlocksFastTravel(
            string originZone,
            string enemyZone,
            CardActionResolutionRejectionReason expectedReason)
        {
            var fixture = CreateFixture(originZone, enemyZone);
            var destinationZone = expectedReason ==
                CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInOriginZone
                ? "red-approach"
                : enemyZone;
            var destination = fixture.EmptyCoordinate(destinationZone);

            var result = fixture.Perform(destination);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(expectedReason));
            Assert.That(fixture.Board.Occupancies.Single(value => value.HeroId.Equals(fixture.ActorId)).Coordinate,
                Is.EqualTo(fixture.ActorCoordinate));
        }

        [TestCase("starting-battle-zone", "red-approach", CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInOriginZone)]
        [TestCase("red-approach", "starting-battle-zone", CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInDestinationZone)]
        public void EnemyMinionInOriginOrDestinationZone_BlocksFastTravel(
            string originZone,
            string destinationZone,
            CardActionResolutionRejectionReason expectedReason)
        {
            var fixture = CreateFixture(originZone);
            var destination = fixture.EmptyCoordinate(destinationZone);

            var result = fixture.Perform(destination);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(expectedReason));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void PrimaryMovementReplacement_UsesFastTravelSuppressedTextPolicy()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var actorId = configuration.RedHand.Instances[0].OwnerId;
            var primaryMovement = new ActionDefinition(
                new ActionDefinitionId("primary-movement-with-text"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true));
            var definition = new CardDefinition(
                new CardDefinitionId("fast-travel-primary-source"),
                "synthetic-fast-travel-r1",
                "TEST - Primary Movement Source",
                new InitiativeValue(10),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { primaryMovement });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("fast-travel-primary-card"),
                definition.Reference,
                actorId);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);

            var offer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                primaryMovement.Id,
                card.Id,
                actorId,
                new[] { definition },
                new[] { card }, RuleModificationSet.Empty);

            Assert.That(offer.HasValue, Is.True);
            Assert.That(offer.Value.InvocationContext, Is.EqualTo(ActionInvocationContext.Replacement));
            Assert.That(offer.Value.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.FastTravelDefinition.Id));
            Assert.That(primaryMovement.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Applies));
            Assert.That(GeneralRuleActionOfferQuery.FastTravelDefinition.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Suppressed));
            Assert.That(GeneralRuleActionOfferQuery.FastTravelDefinition.Kind,
                Is.EqualTo(ActionKind.FastTravel));
            Assert.That(GeneralRuleActionOfferQuery.FastTravelDefinition.Origin,
                Is.EqualTo(ActionDefinitionOrigin.Replacement));
            Assert.That(GeneralRuleActionOfferQuery.FastTravelDefinition.BaseValues, Is.Empty);
        }

        private static Fixture CreateFixture(
            string actorZoneId,
            string enemyHeroZoneId = null,
            bool surroundActorWithObstacles = false)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var actorId = configuration.RedHand.Instances[0].OwnerId;
            var enemyId = configuration.BlueHand.Instances[0].OwnerId;
            var occupiedByMinions = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var actorCoordinate = FindCoordinate(map, actorZoneId, occupiedByMinions);
            var reserved = new HashSet<HexCoordinate>(occupiedByMinions) { actorCoordinate };
            var enemyCoordinate = FindCoordinate(map, enemyHeroZoneId ?? "blue-throne", reserved);
            reserved.Add(enemyCoordinate);
            var obstacles = surroundActorWithObstacles
                ? actorCoordinate.GetNeighbors()
                    .Where(map.PlayableCoordinates.Contains)
                    .Where(coordinate => !reserved.Contains(coordinate))
                    .Select((coordinate, index) => new ObstacleOccupancy(
                        new ObstacleId($"fast-travel-path-blocker-{index}"),
                        coordinate))
                    .ToArray()
                : Array.Empty<ObstacleOccupancy>();
            foreach (var obstacle in obstacles)
            {
                reserved.Add(obstacle.Coordinate);
            }
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(actorId, actorCoordinate),
                    new HeroOccupancy(enemyId, enemyCoordinate)
                },
                obstacles,
                setup.Minions);
            var card = configuration.RedHand.Instances[0];
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            var movementAction = configuration.CardDefinitions
                .Single(value => value.Reference.Equals(card.DefinitionReference))
                .ActionDefinitions.Single(value => value.Kind == ActionKind.Movement);
            var replacementOffer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                movementAction.Id,
                card.Id,
                actorId,
                configuration.CardDefinitions,
                configuration.CardInstances, RuleModificationSet.Empty);
            Assert.That(replacementOffer.HasValue, Is.True);
            return new Fixture(
                configuration,
                map,
                setup,
                board,
                actorId,
                actorCoordinate,
                movementAction.Id,
                replacementOffer.Value,
                reserved);
        }

        private static HexCoordinate FindCoordinate(
            BoardMapDefinition map,
            string zoneId,
            ISet<HexCoordinate> excluded)
        {
            return map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Where(space => map.GetZoneForSpace(space.Id).Id.Equals(new ZoneId(zoneId)))
                .Select(space => space.Coordinate)
                .First(coordinate => !excluded.Contains(coordinate));
        }

        private sealed class Fixture
        {
            private readonly MatchConfiguration configuration;
            private readonly BoardMapDefinition map;
            private readonly QuickGameSetupState setup;
            private readonly ActionOffer replacementOffer;
            private readonly HashSet<HexCoordinate> reserved;

            public Fixture(
                MatchConfiguration configuration,
                BoardMapDefinition map,
                QuickGameSetupState setup,
                AdjacentMoveAuthority board,
                HeroId actorId,
                HexCoordinate actorCoordinate,
                ActionDefinitionId movementActionId,
                ActionOffer replacementOffer,
                HashSet<HexCoordinate> reserved)
            {
                this.configuration = configuration;
                this.map = map;
                this.setup = setup;
                this.replacementOffer = replacementOffer;
                this.reserved = reserved;
                Board = board;
                ActorId = actorId;
                ActorCoordinate = actorCoordinate;
                MovementActionId = movementActionId;
            }

            public AdjacentMoveAuthority Board { get; }
            public HeroId ActorId { get; }
            public HexCoordinate ActorCoordinate { get; }
            public ActionDefinitionId MovementActionId { get; }

            public HexCoordinate EmptyCoordinate(string zoneId) => FindCoordinate(map, zoneId, reserved);

            public CardActionResolutionResult Perform(HexCoordinate destination)
            {
                return new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), Board)
                    .PerformTurnFastTravel(
                    new ActionResolutionId("fast-travel-test-resolution"),
                    replacementOffer,
                    MovementActionId,
                    destination,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    Board.Minions, RuleModificationSet.Empty);
            }

            public IReadOnlyList<HexCoordinate> LegalDestinations()
            {
                return FastTravelActionQuery.FindLegalDestinations(
                    replacementOffer,
                    MovementActionId,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    Board,
                    configuration.Participants,
                    Board.Minions, RuleModificationSet.Empty);
            }
        }
    }
}
