using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.MisaTerrainRingBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z29. The five capabilities Misa's set needed, measured on a board rather than
    /// read back off the declarations that name them.
    ///
    /// <para><see cref="MisaPrintedSetTests"/> asserts that each ruling reached the composition. This
    /// file asserts that each capability then does what the ruling says - which is the difference the
    /// 2026-09-06 corrective work kept finding: a rule recorded in a document, declared in a
    /// definition, and never actually wired to a query that answers it.</para>
    /// </summary>
    public sealed class MisaCapabilityTests
    {
        private static readonly HeroId Misa = new HeroId("misa-hero");
        private static readonly HeroId Enemy = new HeroId("misa-enemy");

        /// <summary>
        /// RULING 1, measured: "at maximum range" admits the ring at exactly the printed number and
        /// nothing inside it.
        /// </summary>
        [Test]
        public void Ruling1_TheExactDistancePlace_OffersTheRingAndNotTheDisk()
        {
            var board = CreateBoard((Misa, Origin));
            var ring = Destinations(board, PlaceDestinationScope.EmptySpacesAtExactlyRadius, 4);

            Assert.That(ring, Is.Not.Empty);
            Assert.That(
                ring.All(space => Distance(Origin, space) == 4),
                Is.True,
                "Every destination stands at exactly the printed Range.");
            Assert.That(
                ring.Count,
                Is.EqualTo(PlayableCoordinates.Count(space => Distance(Origin, space) == 4)),
                "And every playable space at that distance is offered.");

            var disk = Destinations(board, PlaceDestinationScope.EmptySpacesWithinRadius, 4);
            Assert.That(disk.Count, Is.GreaterThan(ring.Count),
                "The disk is what RULING 1 rejected: it admits everything nearer as well.");
            Assert.That(
                disk.Any(space => Distance(Origin, space) == 1), Is.True);
            Assert.That(
                ring.Any(space => Distance(Origin, space) == 1), Is.False);
        }

        /// <summary>
        /// The consequence RULING 1 brings that no earlier reach in this build had: <b>the candidate
        /// set can be legally empty</b>. A ring of radius 3 on this board is entirely terrain, so a
        /// Place measured there has nowhere to go - which the offer must express as a greyed control
        /// rather than as a crash or as a choice that cannot be completed.
        ///
        /// <para>It is the ordinary negative answer of an ordinary question, which is exactly why it
        /// needed a test: a disk of radius 3 around the same hero is not empty at all.</para>
        /// </summary>
        [Test]
        public void Ruling1_TheExactDistancePlace_CanLegitimatelyOfferNothing()
        {
            var board = CreateBoard((Misa, Origin));

            Assert.That(
                Destinations(board, PlaceDestinationScope.EmptySpacesAtExactlyRadius, 3),
                Is.Empty,
                "Every space at exactly 3 is terrain, and p.18 lets nobody be placed on an obstacle.");
            Assert.That(
                Destinations(board, PlaceDestinationScope.EmptySpacesWithinRadius, 3),
                Is.Not.Empty,
                "The same hero measured as a disk has plenty of destinations, which is what makes the " +
                "empty ring a real distinction rather than a broken board.");
        }

        /// <summary>
        /// RULING 29's first arm, measured: the line crosses the terrain ring and lands beyond it.
        /// </summary>
        [Test]
        public void Ruling29_TheCrossingLine_PassesThroughTerrainAndLandsBeyondIt()
        {
            var board = CreateBoard((Misa, Origin));
            var crossing = Reachable(board, 4, ignoresObstacles: true);
            var ordinary = Reachable(board, 4, ignoresObstacles: false);

            Assert.That(
                crossing.Any(space => Distance(Origin, space) == 4),
                Is.True,
                "A crossing move of 4 reaches the outer band, which is only reachable through the ring.");
            Assert.That(
                ordinary.Any(space => Distance(Origin, space) >= 3),
                Is.False,
                "The same move without the crossing declaration is stopped by the ring, which is what " +
                "makes the declaration the thing being measured.");
        }

        /// <summary>
        /// RULING 29's second arm, measured: "as long as hero ends up on a playable space". The
        /// crossing permission reaches transit and never the endpoint, so no distance and no direction
        /// produces a destination on the ring.
        /// </summary>
        [Test]
        public void Ruling29_TheCrossingLine_NeverEndsOnTerrain()
        {
            var board = CreateBoard((Misa, Origin));

            for (var allowance = 1; allowance <= MapRadius; allowance++)
            {
                Assert.That(
                    Reachable(board, allowance, ignoresObstacles: true)
                        .Any(space => TerrainCoordinates.Contains(space)),
                    Is.False,
                    "A crossing move of " + allowance + " must not be able to stop on the ring.");
            }

            Assert.That(
                Reachable(board, 3, ignoresObstacles: true).Any(space => Distance(Origin, space) == 3),
                Is.False,
                "A line of exactly 3 has nowhere to land at all - it can only reach the ring - which " +
                "is the case that separates crossing from landing.");
        }

        /// <summary>
        /// RULING 29's third arm, measured: the spaces beyond the map are not obstacles to cross but
        /// the absence of a space, so a line aimed past the edge produces nothing there rather than
        /// carrying her off the board.
        /// </summary>
        [Test]
        public void Ruling29_TheCrossingLine_TreatsOffTheMapAsNothingRatherThanAnObstacle()
        {
            var board = CreateBoard((Misa, Origin));

            Assert.That(
                Reachable(board, MapRadius + 1, ignoresObstacles: true)
                    .Any(space => Distance(Origin, space) > MapRadius),
                Is.False,
                "Nothing beyond the map's outermost ring is a destination, however far the line runs.");
            Assert.That(
                Reachable(board, MapRadius, ignoresObstacles: true)
                    .Any(space => Distance(Origin, space) == MapRadius),
                Is.True,
                "The outermost band itself is still reachable, so the bound above is the map's edge " +
                "rather than a movement that stopped early.");
        }

        /// <summary>
        /// RULING 28, measured: the modal's ceiling is raised for the hero whose Ultimate it is, on a
        /// modal she is both resolving and choosing, and for nobody and nothing else.
        ///
        /// <para>The three negative cases are the ruling's two narrowings and its arithmetic. A
        /// different chooser on her card is not "whenever <em>you</em> choose one"; her choosing on
        /// somebody else's card is not "her own cards"; and a hero with no such modification standing
        /// gets the number the card prints.</para>
        /// </summary>
        [Test]
        public void Ruling28_TheChoiceCardinality_RaisesOnlyHerOwnModalAndOnlyForHer()
        {
            var widened = SetFor(Misa, RuleModification.ForChoiceCardinality(2));

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, Misa, Misa, widened),
                Is.EqualTo(2),
                "Her own card, her own decision, two bullets: \"two different options\" is both.");

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, Misa, Enemy, widened),
                Is.EqualTo(1),
                "A decision her card puts to an opponent is not \"whenever you choose one\".");
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, Enemy, Misa, widened),
                Is.EqualTo(1),
                "A modal on an enemy's card is not one of her own cards' choose-one nodes.");
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, Misa, Misa, RuleModificationSet.Empty),
                Is.EqualTo(1),
                "With no Ultimate standing, the printed number is the answer.");
        }

        /// <summary>
        /// The arithmetic bounds of the same query, which the ruling's own words imply: a declaration
        /// cannot invent a bullet the card does not print, and it never lowers a ceiling.
        /// </summary>
        [Test]
        public void Ruling28_TheChoiceCardinality_NeverExceedsThePrintedOptionsOrLowersACeiling()
        {
            var widened = SetFor(Misa, RuleModification.ForChoiceCardinality(2));

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 1, Misa, Misa, widened),
                Is.EqualTo(1),
                "\"Two different options\" cannot be satisfied from a one-bullet list.");
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(3, 3, Misa, Misa, widened),
                Is.EqualTo(3),
                "A node already admitting three is not narrowed to two by a permission.");
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 5, Misa, Misa, widened),
                Is.EqualTo(2),
                "And the declaration's own number bounds it from above.");
        }

        /// <summary>
        /// RULING 32's displacement, measured: "to a space farther away from you" admits strictly
        /// farther spaces, and equal separation is neither closer nor farther - so a sideways step
        /// around Misa is refused, exactly as the mirror policy already refuses one.
        /// </summary>
        [Test]
        public void Ruling32_TheAwayFromActorPolicy_RefusesTheSidewaysStep()
        {
            var actor = Origin;
            var target = new HexCoordinate(2, 0);
            var candidates = Disk(target, 1).Where(space => !space.Equals(target)).ToArray();

            var farther = CloserQuery.FilterFartherFrom(actor, target, candidates);
            Assert.That(farther, Is.Not.Empty);
            Assert.That(
                farther.All(space => Distance(actor, space) > Distance(actor, target)),
                Is.True);
            Assert.That(
                farther.Any(space => Distance(actor, space) == Distance(actor, target)),
                Is.False,
                "Equal separation is not farther.");

            var closer = CloserQuery.FilterCloserTo(actor, target, candidates);
            Assert.That(
                farther.Intersect(closer), Is.Empty,
                "The two policies partition the strictly-moved candidates and share none.");
        }

        /// <summary>
        /// The rider's own reach, measured on a board where it matters: a target with the map's edge
        /// behind it has nowhere farther to go, so the optional step is simply not taken. Both faces
        /// print "you may", which is what makes that an ordinary answer.
        /// </summary>
        [Test]
        public void Ruling32_TheAwayFromActorPolicy_OffersNothingAtTheMapsEdge()
        {
            var actor = Origin;
            var target = PlayableCoordinates
                .Where(space => Distance(actor, space) == MapRadius)
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R)
                .First();
            var reachable = Disk(target, 1)
                .Where(space => !space.Equals(target) && AllCoordinates.Contains(space))
                .ToArray();

            Assert.That(
                CloserQuery.FilterFartherFrom(actor, target, reachable),
                Is.Empty,
                "Nothing on this map stands farther from the origin than the outermost ring.");
        }

        private static IReadOnlyList<HexCoordinate> Destinations(
            AdjacentMoveAuthority board, PlaceDestinationScope scope, int radius) =>
            PlaceDestinationQuery.FindDestinations(
                new PlacePolicy(
                    PlaceSourceContainer.Board,
                    PlaceEligiblePieceKind.GamePiece,
                    scope,
                    radius,
                    PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.ActingHero),
                board,
                Origin,
                Array.Empty<HexCoordinate>(),
                RuleModificationSet.Empty);

        private static IReadOnlyList<HexCoordinate> Reachable(
            AdjacentMoveAuthority board, int allowance, bool ignoresObstacles) =>
            MovementReachabilityQuery.FindReachableDestinations(
                    Origin,
                    RuleObject.ForHero(Misa),
                    board,
                    allowance,
                    RuleModificationSet.Empty,
                    ignoresObstacles)
                .Select(value => value.Destination)
                .ToArray();

        private static RuleModificationSet SetFor(HeroId hero, RuleModification modification) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        modification,
                        RuleObject.ForHero(hero),
                        new GuardsDigital.Core.Timing.ActiveEffectId("misa-test-effect"),
                        hero,
                        TeamSide.Red,
                        1)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                default);
    }
}
