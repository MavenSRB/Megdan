using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5's shared Place: rulebook p.18's "Take a game piece (such as a miniature, or a
    /// token) from the board or from the supply, and put it into a different space. A game piece can
    /// only be placed into an empty space (this does not count as movement)."
    ///
    /// These cover the operation and its declared policy directly. The capabilities that state a policy
    /// against it are covered by <c>PlacementCapabilityTests</c>, and the after-action correction that
    /// states a third one by <c>DisplacedMinionCorrectionTests</c>.
    /// </summary>
    public sealed class PlaceOperationTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("synthetic-place-map");
        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, 0);

        /// <summary>Where every fixture below stands the other hero, and therefore the space a
        /// PlacedPiece-anchored Place measures its destinations from.</summary>
        private static readonly HexCoordinate OtherAt = new HexCoordinate(1, 0);

        private static PlacePolicy BoardRadius(int radius) => new PlacePolicy(
            PlaceSourceContainer.Board,
            PlaceEligiblePieceKind.GamePiece,
            PlaceDestinationScope.EmptySpacesWithinRadius,
            radius,
            PlaceChooserKind.ActingHero,
            PlaceNoDestinationPolicy.Impossible,
            PlaceDestinationAnchor.PlacedPiece);

        [Test]
        public void APlacePolicy_RefusesToExistWithoutEachOfTheSixThingsAPlaceMustDeclare()
        {
            // CARD_EFFECT_CAPABILITIES.md makes an incomplete placement declaration a content-load
            // rejection, so the incomplete object cannot be constructed at all. Milestone 8.6 added the
            // sixth: the space the destination question is measured from.
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 1,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.None),
                Throws.ArgumentException, "destination anchor");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.None, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 1,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.ArgumentException, "source container");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.None,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 1,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.ArgumentException, "eligible piece kind");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.None, 1,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.ArgumentException, "destination query");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 1,
                    PlaceChooserKind.None, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.ArgumentException, "chooser");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 1,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.None,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.ArgumentException, "no-destination policy");
            Assert.That(() => new PlacePolicy(
                    PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                    PlaceDestinationScope.EmptySpacesWithinRadius, 0,
                    PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.PlacedPiece),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "A radius of zero admits only the origin, which p.18's different-space rule excludes.");
        }

        [Test]
        public void TheRadiusScope_OffersEveryEmptyPlayableSpaceInRangeExceptTheOriginItself()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var destinations = PlaceDestinationQuery.FindDestinations(
                BoardRadius(1), board, ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(destinations, Has.No.Member(ActorAt),
                "p.18 puts the piece into a different space, so its own space is never a candidate.");
            Assert.That(destinations, Has.No.Member(new HexCoordinate(1, 0)),
                "An occupied space is not empty (p.19: one obstacle per space, ever).");
            Assert.That(destinations, Is.EqualTo(new[]
            {
                new HexCoordinate(-1, 0),
                new HexCoordinate(-1, 1),
                new HexCoordinate(0, -1),
                new HexCoordinate(0, 1),
                new HexCoordinate(1, -1)
            }), "The empty playable neighbours, in canonical axial order.");
        }

        [Test]
        public void TheRadiusScope_MeasuresDistanceThroughObstaclesRatherThanAlongARoute()
        {
            // p.18 counts spaces "including spaces with obstacles", and placing is not movement, so a
            // space screened by an occupied one is still in range.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var destinations = PlaceDestinationQuery.FindDestinations(
                BoardRadius(2), board, ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(destinations, Has.Member(new HexCoordinate(2, 0)),
                "The hero at (1,0) screens (2,0) from every route, and a Place reaches it anyway.");
        }

        [Test]
        public void TheNearestInScopeQuery_ReturnsOnlyTheEquallyNearestEmptySpacesOfTheDeclaredSet()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var policy = new PlacePolicy(
                PlaceSourceContainer.Board,
                PlaceEligiblePieceKind.MinionMiniature,
                PlaceDestinationScope.NearestEmptySpacesInScope,
                0,
                PlaceChooserKind.OwningTeam,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);
            var scope = new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                new HexCoordinate(2, -1), new HexCoordinate(3, 0)
            };

            var destinations = PlaceDestinationQuery.FindDestinations(policy, board, ActorAt, scope, RuleModificationSet.Empty);

            Assert.That(destinations, Is.EqualTo(new[] { new HexCoordinate(2, -1), new HexCoordinate(2, 0) }),
                "The occupied space drops out and both remaining nearest spaces are returned: p.18 " +
                "gives the choice between equally near spaces to the piece's own team, so the query " +
                "never breaks the tie.");
        }

        [Test]
        public void ABoardPlace_TakesThePieceOffItsSpaceAndReportsBothCoordinates()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var destination = new HexCoordinate(2, 0);

            var outcome = PlaceOperation.PlaceFromBoard(
                BoardRadius(2), board, board,
                BoardObjectReference.ForHero(Other), OtherAt, destination, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.Accepted, Is.True);
            Assert.That(outcome.SourceContainer, Is.EqualTo(PlaceSourceContainer.Board));
            Assert.That(outcome.HasOrigin, Is.True);
            Assert.That(outcome.Origin, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(outcome.Destination, Is.EqualTo(destination));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(board.IsEmptyPlayableSpace(new HexCoordinate(1, 0)), Is.True,
                "The vacated space is free for every occupancy consumer.");
        }

        [Test]
        public void ABoardPlace_RefusesAPieceItsDeclaredEligibilityDoesNotAdmit()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var minionsOnly = new PlacePolicy(
                PlaceSourceContainer.Board,
                PlaceEligiblePieceKind.MinionMiniature,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                2,
                PlaceChooserKind.OwningTeam,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);

            var outcome = PlaceOperation.PlaceFromBoard(
                minionsOnly, board, board,
                BoardObjectReference.ForHero(Other), OtherAt, new HexCoordinate(2, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.Accepted, Is.False);
            Assert.That(outcome.RejectionReason, Is.EqualTo(PlaceRejectionReason.IneligiblePiece));
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)), "A refused Place moves nothing.");
        }

        [Test]
        public void ABoardPlace_RefusesADestinationItsOwnQueryDoesNotCurrentlyAdmit()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var tooFar = PlaceOperation.PlaceFromBoard(
                BoardRadius(1), board, board,
                BoardObjectReference.ForHero(Other), OtherAt, new HexCoordinate(3, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);
            var ownSpace = PlaceOperation.PlaceFromBoard(
                BoardRadius(2), board, board,
                BoardObjectReference.ForHero(Other), OtherAt, new HexCoordinate(1, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);
            var occupied = PlaceOperation.PlaceFromBoard(
                BoardRadius(2), board, board,
                BoardObjectReference.ForHero(Other), OtherAt, ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(tooFar.RejectionReason, Is.EqualTo(PlaceRejectionReason.DestinationNotAdmitted));
            Assert.That(ownSpace.RejectionReason, Is.EqualTo(PlaceRejectionReason.DestinationNotAdmitted),
                "p.18 requires a different space.");
            Assert.That(occupied.RejectionReason, Is.EqualTo(PlaceRejectionReason.DestinationNotAdmitted),
                "The actor stands there; a game piece can only be placed into an empty space.");
        }

        [Test]
        public void ABoardPlace_RefusesAMeasuredFromSpaceItsDeclaredAnchorDoesNotName()
        {
            // Milestone 8.6. The anchor is content - p.18 says nothing about where a destination is
            // measured from, so the invoking card does - and a caller and a declaration that disagree
            // about it are refused rather than quietly resolved in the caller's favour.
            var board = CreateBoard(otherAt: OtherAt);

            var outcome = PlaceOperation.PlaceFromBoard(
                BoardRadius(2), board, board,
                BoardObjectReference.ForHero(Other), ActorAt, new HexCoordinate(2, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.RejectionReason,
                Is.EqualTo(PlaceRejectionReason.DestinationAnchorMismatch),
                "This policy anchors at the placed piece, and the actor's space is not the piece's.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(OtherAt), "A refused Place moves nothing.");
        }

        [Test]
        public void AnActorAnchoredPlace_MeasuresItsDestinationsFromTheAnchorAndNotFromThePiece()
        {
            // Wasp's Telekinesis prints "into a space adjacent to you", which is a question about where
            // the acting hero stands and which no radius around the piece could ask.
            var board = CreateBoard(otherAt: new HexCoordinate(2, 0));
            var anchored = new PlacePolicy(
                PlaceSourceContainer.Board,
                PlaceEligiblePieceKind.UnitOrToken,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                1,
                PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.ActingHero);

            var accepted = PlaceOperation.PlaceFromBoard(
                anchored, board, board,
                BoardObjectReference.ForHero(Other), ActorAt, new HexCoordinate(0, 1),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);
            var besideThePiece = PlaceOperation.PlaceFromBoard(
                anchored, board, board,
                BoardObjectReference.ForHero(Other), ActorAt, new HexCoordinate(3, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(accepted.Accepted, Is.True,
                "A space adjacent to the actor is admitted however far the piece was standing.");
            Assert.That(accepted.Origin, Is.EqualTo(new HexCoordinate(2, 0)),
                "and the outcome still names where the piece came from, which is not the anchor.");
            Assert.That(besideThePiece.RejectionReason,
                Is.EqualTo(PlaceRejectionReason.DestinationNotAdmitted),
                "while a space adjacent to the piece is not adjacent to the actor and is refused.");
        }

        [Test]
        public void ASupplyPlace_RefusesAnAnchorAPieceOffTheBoardCannotHave()
        {
            var board = CreateBoard(otherAt: OtherAt);
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            var impossible = new PlacePolicy(
                PlaceSourceContainer.Supply,
                PlaceEligiblePieceKind.Token,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                1,
                PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);

            var outcome = PlaceOperation.PlaceFromSupply(
                impossible, board, catalog, Hazard(catalog, new HexCoordinate(0, 1)),
                ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.RejectionReason,
                Is.EqualTo(PlaceRejectionReason.DestinationAnchorMismatch),
                "A piece taken from the supply came off no space, so it cannot be the space its own " +
                "destinations are measured from.");
        }

        [Test]
        public void TheUnitOrTokenKind_AdmitsWhatTheCardSaysAndIsStillNotThePagesGamePiece()
        {
            // Milestone 8.6. `a game piece (such as a miniature, or a token)` is p.18's sentence about
            // the Place keyword; `a unit or a token` is Wasp's sentence about her Place. They admit the
            // same three kinds on this board and are still two declarations, for the reason
            // PushEligiblePieceKind refuses to call its own unrestricted value `GamePiece`: the name is
            // the content, and the two part company the first time a placeable piece is neither.
            var board = CreateBoard(otherAt: OtherAt);
            var unitOrToken = new PlacePolicy(
                PlaceSourceContainer.Board,
                PlaceEligiblePieceKind.UnitOrToken,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                2,
                PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);

            var outcome = PlaceOperation.PlaceFromBoard(
                unitOrToken, board, board,
                BoardObjectReference.ForHero(Other), OtherAt, new HexCoordinate(2, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.Accepted, Is.True, "A hero is a unit.");
            Assert.That(unitOrToken.EligiblePiece, Is.Not.EqualTo(PlaceEligiblePieceKind.GamePiece),
                "and the declaration still says what the card says.");
        }

        [Test]
        public void ABoardPlace_RefusesAPieceThatIsNoLongerStanding()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var outcome = PlaceOperation.PlaceFromBoard(
                BoardRadius(2), board, board,
                BoardObjectReference.ForMinion(new MinionId("absent-minion")),
                OtherAt, new HexCoordinate(2, 0), Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.RejectionReason, Is.EqualTo(PlaceRejectionReason.SubjectNotOnBoard));
        }

        [Test]
        public void EachArmRefusesTheOtherContainersPolicy()
        {
            // p.18's Spawn is defined by ruling out one of Place's two source containers, so the two
            // arms cannot silently serve each other's declaration.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var catalog = SyntheticComponentDefinitions.CreateCatalog();

            var boardArm = PlaceOperation.PlaceFromSupply(
                BoardRadius(1), board, catalog, Hazard(catalog, new HexCoordinate(0, 1)),
                ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);
            var supplyArm = PlaceOperation.PlaceFromBoard(
                SupplyRadius(1), board, board,
                BoardObjectReference.ForHero(Other), OtherAt, new HexCoordinate(0, 1),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(boardArm.RejectionReason, Is.EqualTo(PlaceRejectionReason.SourceContainerMismatch));
            Assert.That(supplyArm.RejectionReason, Is.EqualTo(PlaceRejectionReason.SourceContainerMismatch));
        }

        [Test]
        public void ASupplyPlace_PutsATokenIntoAnEmptySpaceAndHasNoOriginAtAll()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var catalog = SyntheticComponentDefinitions.CreateCatalog();

            var outcome = PlaceOperation.PlaceFromSupply(
                SupplyRadius(1), board, catalog, Hazard(catalog, new HexCoordinate(0, 1)),
                ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(outcome.Accepted, Is.True);
            Assert.That(outcome.SourceContainer, Is.EqualTo(PlaceSourceContainer.Supply));
            Assert.That(outcome.HasOrigin, Is.False,
                "A supply piece came off no space, so there is no origin to report rather than a " +
                "default one that would read as a coordinate.");
            Assert.That(outcome.Destination, Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(new HexCoordinate(0, 1)));
        }

        [Test]
        public void ASupplyPlace_RefusesWhenTheFiniteSupplyIsExhausted()
        {
            // p.19: "tokens are intentionally limited". The supply is the trusted total minus what is
            // on the board, so exhausting it needs nothing but placing them.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            Assert.That(PlaceOperation.PlaceFromSupply(
                SupplyRadius(1), board, catalog, Hazard(catalog, new HexCoordinate(0, 1)),
                ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty).Accepted, Is.True);

            var exhausted = PlaceOperation.PlaceFromSupply(
                SupplyRadius(1), board, catalog, Hazard(catalog, new HexCoordinate(-1, 0)),
                ActorAt, Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);

            Assert.That(exhausted.RejectionReason, Is.EqualTo(PlaceRejectionReason.SupplyEmpty));
            Assert.That(board.PlacedTokens.Count,
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount),
                "Nothing was placed beyond the supply.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static PlacePolicy SupplyRadius(int radius) => new PlacePolicy(
            PlaceSourceContainer.Supply,
            PlaceEligiblePieceKind.Token,
            PlaceDestinationScope.EmptySpacesWithinRadius,
            radius,
            PlaceChooserKind.ActingHero,
            PlaceNoDestinationPolicy.Impossible,
            PlaceDestinationAnchor.ActingHero);

        private static PlacedToken Hazard(ComponentDefinitionCatalog catalog, HexCoordinate coordinate) =>
            new PlacedToken(
                new TokenInstanceId($"place-token-{coordinate.Q}-{coordinate.R}"),
                catalog.LoadToken(SyntheticComponentDefinitions.HazardV4),
                Actor,
                Actor,
                new CardInstanceId("place-token-source"),
                coordinate);

        /// <summary>A hex disk of radius 3 around the origin, with the actor at its centre.</summary>
        private static AdjacentMoveAuthority CreateBoard(HexCoordinate otherAt)
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            {
                for (var r = Math.Max(-3, -q - 3); r <= Math.Min(3, -q + 3); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            var map = BoardMapDefinition.CreateSynthetic(Map, 1, coordinates);
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Actor, ActorAt), new HeroOccupancy(Other, otherAt) },
                Array.Empty<ObstacleOccupancy>());
        }
    }
}
