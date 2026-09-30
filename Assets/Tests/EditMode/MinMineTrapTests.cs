using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The trap Min the Dragonmonk's three Blue faces end with. Milestone 8.61e.
    ///
    /// <para>One printed sentence is measured here, clause by clause: <b>"After an enemy hero moves
    /// through a mine, remove it; for every blast removed, that hero discards a card, if able."</b></para>
    ///
    /// <para><b>"moves through"</b> is a route that entered and left a space - a Movement action's walk,
    /// a card-text Move, and p.18's Push or coupled move carrying somebody over one. It is NOT a Swap, a
    /// Place, a Fast Travel or a respawn, and those are excluded by construction rather than by a check:
    /// none of them walks a space, so none of them can hand the board a crossing. <b>"an enemy hero"</b>
    /// is p.19's own sides read from the token's controller - her own team walks over her mines, and so
    /// does every minion of either side. <b>"remove it"</b> happens for a dud exactly as for a blast,
    /// and <b>"for every blast removed"</b> is the only difference between the two kinds, which is
    /// exactly the difference a facedown mine hides.</para>
    ///
    /// <para>MI-Q3's second half - which of several equal routes a one-click destination takes - is
    /// measured in <see cref="AOneClickDestination_TakesTheEqualRouteThatAvoidsAMine"/> and
    /// <see cref="AStepByStepWalk_CanNeverStopOnAMine_SoItCanNeverStepOntoOne"/>, and the second of
    /// those is the finding rather than the assumption.</para>
    /// </summary>
    public sealed class MinMineTrapTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);

        // -------------------------------------------------------------------------------------
        // The declaration: which pieces carry the rule, and which of them charges for it.
        // -------------------------------------------------------------------------------------

        [Test]
        public void EveryMineFaceDeclaresTheRemoval_AndOnlyTheBlastDeclaresWhatItCosts()
        {
            foreach (var definition in MinComponentContent.MineTokens())
            {
                Assert.That(MoveThroughTrapQuery.Declares(definition), Is.True,
                    $"'{definition.Id}' prints \"After an enemy hero moves through a mine, remove it\".");
            }

            Assert.That(MinComponentContent.MineBlastDefinition.MoveThroughTrapConsequences,
                Is.Not.Empty, "\"for every blast removed, that hero discards a card\".");
            Assert.That(MinComponentContent.MineDudDefinition.MoveThroughTrapConsequences,
                Is.Empty, "A dud is removed and costs nothing; that is the whole of what a dud is.");
            Assert.That(MinComponentContent.MineFacedownDefinition.MoveThroughTrapConsequences,
                Is.Empty,
                "The price IS the kind, so the face that names no kind names no price - and nothing " +
                "reads it from that face, because the trap is applied off the trusted definition.");
        }

        [Test]
        public void TheHidingIsStillCostless_BecauseThePropertyListsAreStillIdentical()
        {
            // Milestone 8.61d's invariant, re-measured after a second property was added to all three:
            // a masked board and the authority's own answer every standing question identically.
            var describe = new Func<TokenDefinition, string>(definition => string.Join(
                "|",
                definition.Properties.Select(property => string.Join(
                    ":",
                    property.PropertyId,
                    property.AffectedObject.ToString(),
                    property.Modification.Kind.ToString()))));

            Assert.That(describe(MinComponentContent.MineDudDefinition),
                Is.EqualTo(describe(MinComponentContent.MineBlastDefinition)));
            Assert.That(describe(MinComponentContent.MineFacedownDefinition),
                Is.EqualTo(describe(MinComponentContent.MineBlastDefinition)));
        }

        [Test]
        public void ATokenCannotDeclareAPriceWithoutTheRemovalThatCollectsIt()
        {
            Assert.That(
                () => new TokenDefinition(
                    new TokenDefinitionId("fixture-priced-but-never-removed"),
                    1,
                    "Priced",
                    1,
                    Array.Empty<TokenPropertyDefinition>(),
                    TokenRemovalCondition.EndOfRoundCleanup,
                    null,
                    null,
                    MinComponentContent.MineTrapConsequences().Take(1)),
                Throws.ArgumentException,
                "An effect that nothing can ever run is not a declaration.");
        }

        // -------------------------------------------------------------------------------------
        // "moves through": what a route, a line and a Place each hand the board.
        // -------------------------------------------------------------------------------------

        [Test]
        public void SpacesMovedThrough_DropsBothEndsOfTheWalk_BecauseNeitherWasLeftOrEntered()
        {
            var route = new[]
            {
                new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                new HexCoordinate(2, 0), new HexCoordinate(3, 0)
            };

            Assert.That(MoveThroughTrapQuery.SpacesMovedThrough(route),
                Is.EqualTo(new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) }));
            Assert.That(
                MoveThroughTrapQuery.SpacesMovedThrough(
                    new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }),
                Is.Empty, "One step enters a space and leaves none.");
        }

        [Test]
        public void TheRealisedLine_IsWhereAPushActuallyWent_AndNotWhereItWasAimed()
        {
            var origin = new HexCoordinate(0, 0);
            Assert.That(
                MoveThroughTrapQuery.RealisedLine(origin, new HexCoordinate(3, 0)),
                Is.EqualTo(new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) }));
            Assert.That(MoveThroughTrapQuery.RealisedLine(origin, new HexCoordinate(1, 0)), Is.Empty);
            Assert.That(MoveThroughTrapQuery.RealisedLine(origin, origin), Is.Empty,
                "p.18's zero-space push relocates nothing, so it crosses nothing.");
        }

        [Test]
        public void AMovementRouteRecordsItsCrossing_AndASingleStepRecordsNothing()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            // One step east from (0,0), which enters a space and leaves none.
            Assert.That(
                board.TryCommitRoute(
                    Enemy, new[] { new HexCoordinate(1, 0) }, RuleModificationSet.Empty).Accepted,
                Is.True);
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False,
                "The one space it entered is where it is standing, so nothing was walked over.");

            Assert.That(
                board.TryCommitRoute(
                    Enemy,
                    new[] { new HexCoordinate(2, 0), new HexCoordinate(3, 0) },
                    RuleModificationSet.Empty).Accepted,
                Is.True);
            var crossings = board.TakeRecordedMoveThroughCrossings();
            Assert.That(crossings.Count, Is.EqualTo(1));
            Assert.That(crossings[0].Mover, Is.EqualTo(Enemy));
            Assert.That(crossings[0].SpacesMovedThrough,
                Is.EqualTo(new[] { new HexCoordinate(2, 0) }));
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False,
                "Taking them clears them, so one displacement is priced once however many look.");
        }

        [Test]
        public void APlaceCrossesNothing_SoItSpringsNothing()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            // The same commit a Place reaches, with no spaces walked - which is what a Place is:
            // p.18 puts a piece INTO a space rather than moving it along any.
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(Enemy), new HexCoordinate(3, 0)).Accepted,
                Is.True);
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False);
        }

        [Test]
        public void ASwapCrossesNothing_EvenWhenItSwapsWithTheMineItself()
        {
            var board = LineBoard();
            var mine = PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            // p.19 states it outright: a Swap "does not count as movement or placement". The enemy
            // ends up standing where the mine was, and walked over nothing to get there.
            Assert.That(
                board.CommitValidatedSwap(
                    BoardObjectReference.ForHero(Enemy), BoardObjectReference.ForToken(mine.Id)).Accepted,
                Is.True);
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False);
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(new HexCoordinate(0, 0)),
                "The mine moved and is still standing, which is the other half of a Swap.");
        }

        [Test]
        public void ATeleportCrossesNothing_BecauseITHasNoRouteToHandOver()
        {
            // p.11's Fast Travel and p.13's respawn both put a hero somewhere without walking: at the
            // board they are relocations that name no spaces, which is the same shape a Place has and
            // the reason neither can spring one. This is that commit, over the exact geometry a walk
            // would have crossed.
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(Enemy), new HexCoordinate(5, 0)).Accepted,
                Is.True);
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False);
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1), "And the mine is still standing.");
        }

        [Test]
        public void ACoupledMoveRecordsTheLineBothObjectsWalked()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            // Min at (-1,0) and the enemy at (0,0) move three spaces east together over the mine at
            // (1,0). Both lines cross it, and both are recorded: the board records the routes and says
            // nothing about whose mine it is - which is the split this seam exists for.
            Assert.That(
                board.TryMoveCoupled(
                    BoardObjectReference.ForHero(Min),
                    BoardObjectReference.ForHero(Enemy),
                    HexDirection.East,
                    3,
                    RuleModificationSet.Empty,
                    ignoresObstacles: false),
                Is.True);

            var crossings = board.TakeRecordedMoveThroughCrossings();
            Assert.That(crossings.Select(crossing => crossing.Mover),
                Is.EquivalentTo(new[] { Min, Enemy }));
            Assert.That(
                crossings.All(crossing =>
                    crossing.SpacesMovedThrough.Contains(new HexCoordinate(1, 0))),
                Is.True);

            // And the sides are answered where they belong: her own coupled move over her own mine
            // springs nothing, and the enemy carried along beside her springs it.
            Assert.That(
                MoveThroughTrapQuery.FindSprung(
                    board, Participants(), Min, new[] { new HexCoordinate(1, 0) }),
                Is.Empty);
            Assert.That(
                MoveThroughTrapQuery.FindSprung(
                    board, Participants(), Enemy, new[] { new HexCoordinate(1, 0) }).Count,
                Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------------------
        // "an enemy hero": p.19's sides, read off the token's controller.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AnEnemyHeroSpringsIt_AndTheControllersOwnTeamAndTheirAllyDoNot()
        {
            var board = LineBoard();
            var mine = PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            var crossed = new[] { new HexCoordinate(2, 0) };

            Assert.That(
                MoveThroughTrapQuery.FindSprung(board, Participants(), Enemy, crossed)
                    .Select(token => token.Id),
                Is.EqualTo(new[] { mine.Id }));
            Assert.That(
                MoveThroughTrapQuery.FindSprung(board, Participants(), Min, crossed), Is.Empty,
                "Min walks over her own mines.");

            var allied = LineBoard();
            PlaceMine(allied, MinComponentContent.MineBlastDefinition, 2, "blast-1", controller: Enemy);
            Assert.That(
                MoveThroughTrapQuery.FindSprung(allied, Participants(), EnemyAlly, crossed), Is.Empty,
                "A team-mate of the controller is not an enemy hero.");
        }

        [Test]
        public void WithoutParticipantsNothingIsSprung_BecauseNothingCanAnswerWhoIsAnEnemy()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            Assert.That(
                MoveThroughTrapQuery.FindSprung(
                    board, null, Enemy, new[] { new HexCoordinate(2, 0) }),
                Is.Empty);
        }

        [Test]
        public void APushedTokenWalkingOverOneSpringsNothing_BecauseOnlyAHeroCan()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            var pushed = PlaceMine(board, MinComponentContent.MineDudDefinition, 1, "dud-1");

            // p.18 names "a unit or a token" as push targets, so a non-hero really does reach this
            // commit with a realised line. It records nothing: p.19 separates heroes from minions and
            // from tokens, and the printed sentence names an enemy HERO.
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForToken(pushed.Id),
                    new HexCoordinate(3, 0),
                    new[] { new HexCoordinate(2, 0) }).Accepted,
                Is.True);
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False);
        }

        // -------------------------------------------------------------------------------------
        // "remove it; for every blast removed ...": the operation.
        // -------------------------------------------------------------------------------------

        [Test]
        public void ADudIsRemovedAndCostsNothing()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineDudDefinition, 2, "dud-1");

            var outcome = MoveThroughTrapOperation.Spring(
                board, Participants(), Enemy, new[] { new HexCoordinate(2, 0) });

            Assert.That(outcome.Removed.Count, Is.EqualTo(1));
            Assert.That(outcome.Removed[0].CostsTheMover, Is.False);
            Assert.That(outcome.HasConsequence, Is.False);
            Assert.That(board.PlacedTokens, Is.Empty, "Removed all the same - a dud is still a mine.");
        }

        [Test]
        public void ABlastIsRemovedAndChargesOneCard()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            var outcome = MoveThroughTrapOperation.Spring(
                board, Participants(), Enemy, new[] { new HexCoordinate(2, 0) });

            Assert.That(outcome.Removed.Count, Is.EqualTo(1));
            Assert.That(outcome.Removed[0].Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(outcome.Removed[0].Definition.Id,
                Is.EqualTo(MinComponentContent.MineBlast),
                "The removal reveals the REAL kind, never a mask.");
            Assert.That(outcome.Consequence,
                Is.EqualTo(MinComponentContent.MineTrapConsequences().First()));
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [Test]
        public void TwoBlastsInOneMoveAreBothRemovedAndChargeTwice()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            PlaceMine(board, MinComponentContent.MineDudDefinition, 3, "dud-1");
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 4, "blast-2");

            var outcome = MoveThroughTrapOperation.Spring(
                board,
                Participants(),
                Enemy,
                new[] { new HexCoordinate(2, 0), new HexCoordinate(3, 0), new HexCoordinate(4, 0) });

            Assert.That(outcome.Removed.Count, Is.EqualTo(3), "All three go off; only two charge.");
            Assert.That(
                outcome.Removed.Select(removed => removed.Coordinate),
                Is.EqualTo(new[]
                {
                    new HexCoordinate(2, 0), new HexCoordinate(3, 0), new HexCoordinate(4, 0)
                }),
                "In the order the hero walked over them, so the reveals read as the walk did.");
            Assert.That(outcome.Consequence,
                Is.EqualTo(MinComponentContent.MineTrapConsequences().ElementAt(1)),
                "Two blasts choose the effect that asks twice.");
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [Test]
        public void ARepeatedSpaceInOneRouteSpringsItsPieceOnce()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            var outcome = MoveThroughTrapOperation.Spring(
                board,
                Participants(),
                Enemy,
                new[] { new HexCoordinate(2, 0), new HexCoordinate(3, 0), new HexCoordinate(2, 0) });

            Assert.That(outcome.Removed.Count, Is.EqualTo(1));
            Assert.That(outcome.Consequence,
                Is.EqualTo(MinComponentContent.MineTrapConsequences().First()));
        }

        // -------------------------------------------------------------------------------------
        // "discards a card, if able": what the consequence actually is.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheConsequenceAsksTheCrossingHeroForOneCardPerBlast_AndSkipsAnEmptyHand()
        {
            var effects = MinEffectContent.MineTrapEffects().ToArray();
            Assert.That(effects.Length, Is.EqualTo(MinComponentContent.MineSupplyCount),
                "One per number of blasts the pile lets a single crossing remove.");

            for (var blasts = 1; blasts <= effects.Length; blasts++)
            {
                var effect = effects[blasts - 1];
                var discards = effect.Nodes
                    .Where(node => node.Capability.Equals(
                        SyntheticCardStateCapabilities.DiscardChosenCardV1))
                    .ToArray();
                Assert.That(discards.Length, Is.EqualTo(blasts),
                    "\"for every blast removed, that hero discards a card\".");
                foreach (var discard in discards)
                {
                    Assert.That(discard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                        "\"if able\": an empty hand gives nothing up and is NOT defeated - the " +
                        "contrast the two Grenades print and these three do not.");
                    Assert.That(
                        discard.Inputs.Single().Role, Is.EqualTo(EffectRoleKind.OpposingHero),
                        "The crossing already named the hero, so a prompt here would invent a " +
                        "decision the card does not print.");
                }

                Assert.That(
                    effect.Nodes.Any(node => node.Capability.Equals(
                        SyntheticConsequenceCapabilities.DefeatHeroV1)),
                    Is.False,
                    "No mine face prints \"or is defeated\".");
            }
        }

        [Test]
        public void TheBlastNamesEveryCountTheSupplyAdmits_AndNothingBeyondIt()
        {
            Assert.That(
                MinComponentContent.MineBlastDefinition.MoveThroughTrapConsequences,
                Is.EqualTo(MinComponentContent.MineTrapConsequences().ToArray()));
            Assert.That(
                MinComponentContent.MineBlastDefinition.MoveThroughTrapConsequences
                    .Select(reference => reference.DefinitionId)
                    .SequenceEqual(MinEffectContent.MineTrapEffects().Select(effect => effect.Id)),
                Is.True,
                "The declaration and the definitions are one list read from two sides.");
            Assert.That(() => MinEffectContent.MineTrapEffect(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => MinEffectContent.MineTrapEffect(MinComponentContent.MineSupplyCount + 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Milestone 8.61j FLIPPED this case. The blast's four consequence definitions are named BY
        /// REFERENCE by a TOKEN DEFINITION rather than by a card node, and the trap operation
        /// resolves that reference against the build's own triggered-effect catalog - so an
        /// unregistered one would be a price the rule can name and the build cannot load.
        /// </summary>
        [Test]
        public void EveryTrapConsequenceIsRegisteredByTheDealing()
        {
            var registered = MatchContentCatalogs.ForThisBuild().TriggeredEffects.Definitions
                .Select(effect => effect.Id.Value)
                .ToArray();
            foreach (var effect in MinEffectContent.MineTrapEffects())
            {
                Assert.That(registered, Has.Member(effect.Id.Value),
                    "Milestone 8.61j deals her and moved every definition across in one change.");
            }
        }

        // -------------------------------------------------------------------------------------
        // MI-Q3's second half: which of several equal routes a one-click destination takes.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AOneClickDestination_TakesTheEqualRouteThatAvoidsAMine()
        {
            // A ring around (1,0): the space (2,-1) is two steps away either through (1,0) or through
            // (2,-1)'s other neighbour (1,-1). One of those two routes is the same length as the other.
            var board = DiscBoard();
            var mine = PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            var reachable = MovementReachabilityQuery.FindReachableDestinations(
                new HeroOccupancy(Min, MinAt), board, 2, RuleModificationSet.Empty);
            var through = reachable.Single(destination =>
                destination.Destination.Equals(new HexCoordinate(2, -1)));

            Assert.That(through.Cost, Is.EqualTo(2), "Both routes are two steps, which is the point.");
            Assert.That(through.Route, Has.No.Member(mine.Coordinate),
                "Where an equal-length route avoids the piece, the offered route takes it. p.10 " +
                "leaves the choice to the mover; this is what a mover who said nothing is taken to " +
                "have meant.");
        }

        [Test]
        public void WhereEveryEqualRouteCrossesIt_TheOfferedOneStillDoes()
        {
            // The straight line has one way through: past (1,0).
            var board = MinLineBoard();
            var mine = PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            var reachable = MovementReachabilityQuery.FindReachableDestinations(
                new HeroOccupancy(Min, MinAt), board, 2, RuleModificationSet.Empty);
            var beyond = reachable.Single(destination =>
                destination.Destination.Equals(new HexCoordinate(2, 0)));

            Assert.That(beyond.Route, Contains.Item(mine.Coordinate),
                "Avoidance is a preference between equals, never a refusal.");
        }

        [Test]
        public void AStepByStepWalk_CanNeverStopOnAMine_SoItCanNeverStepOntoOne()
        {
            // MEASURED, and it is the finding rather than the assumption: a Movement action that
            // commits one space at a time commits each of them as an ENDPOINT, and p.18 lets no unit
            // end its movement on an obstacle. So a deliberate step-by-step walk cannot cross a mine
            // at all - crossing one is only ever a multi-space route commit - and the route preference
            // above is the whole of MI-Q3's route question.
            var board = MinLineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            var session = new MovementSession(board, Min, 3);
            var step = session.TryCommitStep(new HexCoordinate(1, 0), RuleModificationSet.Empty);

            Assert.That(step.Accepted, Is.False);
            Assert.That(step.AdjacentMoveRejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationOccupied));
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.False,
                "A refused step walked nothing.");

            Assert.That(
                session.TryCommitRoute(
                    new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) },
                    RuleModificationSet.Empty).Accepted,
                Is.True,
                "The same two spaces as one route are legal, because only the last one is an endpoint.");
            Assert.That(board.HasRecordedMoveThroughCrossings, Is.True);
        }

        // -------------------------------------------------------------------------------------
        // The reveal: the one surface that records which kind it turned out to be.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheRevealRowNamesTheCrossingHero_TheSpaceAndTheKind()
        {
            var blast = TrapRow(cardsOwed: 1);
            Assert.That(blast.Kind, Is.EqualTo(MatchLogEntryKind.MoveThroughTrapSprung));
            Assert.That(blast.SubjectHeroId, Is.EqualTo(Enemy),
                "The hero who walked over it, never the hero whose mine it was.");
            Assert.That(blast.SubjectCardInstanceId.IsDefined, Is.False,
                "That hero played no card; the mine's card is not theirs to be named beside.");
            Assert.That(blast.HasOrigin, Is.True);
            Assert.That(blast.Origin, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(blast.Amount, Is.EqualTo(1), "A blast.");

            Assert.That(TrapRow(cardsOwed: 0).Amount, Is.EqualTo(0), "A dud.");
        }

        [Test]
        public void TheRevealRowIsNarratedAndCrossesTheWireUnchanged()
        {
            foreach (var cardsOwed in new[] { 0, 1 })
            {
                var row = TrapRow(cardsOwed);
                var written = new MemoryStream();
                using (var writer = new BinaryWriter(written, System.Text.Encoding.UTF8, true))
                {
                    NetworkMatchLogCodec.Write(writer, NetworkMatchLogCodec.ToRow(row));
                }

                written.Position = 0;
                using (var reader = new BinaryReader(written, System.Text.Encoding.UTF8, true))
                {
                    Assert.That(NetworkMatchLogCodec.TryRead(reader, out var decoded), Is.True);
                    var round = NetworkMatchLogCodec.ToEntry(decoded);
                    Assert.That(round.Kind, Is.EqualTo(row.Kind));
                    Assert.That(round.SubjectHeroId, Is.EqualTo(row.SubjectHeroId));
                    Assert.That(round.Origin, Is.EqualTo(row.Origin));
                    Assert.That(round.HasOrigin, Is.True);
                    Assert.That(round.Amount, Is.EqualTo(cardsOwed));
                }
            }

            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(28),
                "8.80 carries the ATTACKING CARD ON THE DEFENSE QUESTION: the pending Defense "
                + "row gained the attacking card's instance id, its definition id and that "
                + "definition's content revision, and the action definition id, all written "
                + "between the attacker id and the attack total - so a /20 peer reads the first "
                + "of those strings where it expects the total's four bytes and mis-decodes the "
                + "rest of the detail record. It rides the CHOICE rather than the Attack FRAME "
                + "because SeatProjection hands that frame to the ACTING seat alone, and the seat "
                + "being asked to defend is by construction not that seat - which is why the "
                + "attacker's card had been missing from the Defense window on every network "
                + "seat, the host's own loopback seat included, since 8.58 drew it. The three "
                + "facts are table-public: the card stands face up in p.4's turn area, already in "
                + "every seat's own card list. SCHEMA IS UNMOVED at v44 - the choice is derived "
                + "from the authoritative Attack state at projection time and never persisted. " +
                "8.35z73 added CoinAwarded after this fixture added MoveThroughTrapSprung. " +
                "A new member of a closed wire vocabulary is a breaking change even when no byte " +
                "of the frame moves: an older peer's Enum.IsDefined refuses the whole frame." +
                "Milestone 8.71a carries the REWRITTEN PRINTED INITIATIVE at /20: each "
                + "seat projection's card sections end in a sparse block naming the cards "
                + "Hanu's Hurry up! has set to 11, which a /19 peer would read as whatever "
                + "field follows the last card row. SCHEMA MOVES WITH IT to v44, because "
                + "the rewrite is authoritative state no live state remembers.");
        }

        [Test]
        public void ThereIsNoRevealBeforeTheRemoval()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            var log = new RuleEventLogProjection();
            log.ResetToSetup(Participants(), new TieBreakerState(TeamSide.Red));
            Assert.That(
                MatchLogProjection.Derive(log.Entries)
                    .Any(entry => entry.Kind == MatchLogEntryKind.MoveThroughTrapSprung),
                Is.False,
                "A standing mine is facedown, and the log says nothing about it at all.");
        }

        // -------------------------------------------------------------------------------------
        // The pause the charge opens.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheNewInterruptKind_CrossesTheWireAndTheSnapshotWithoutASchemaMove()
        {
            var written = new MemoryStream();
            using (var writer = new BinaryWriter(written, System.Text.Encoding.UTF8, true))
            {
                writer.Write((int)EffectInterruptKind.AfterMoveThroughTrapTrigger);
            }

            written.Position = 0;
            using (var reader = new BinaryReader(written, System.Text.Encoding.UTF8, true))
            {
                Assert.That(
                    SnapshotEnumReader.Read<EffectInterruptKind>(reader, "effect interrupt kind"),
                    Is.EqualTo(EffectInterruptKind.AfterMoveThroughTrapTrigger));
            }

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(50),
                "A fourth reason an effect frame can be standing costs no schema version: the " +
                "card-action payload has written the kind as an int since milestone 8.19c1." +
                "Milestone 8.71a moved it again, to v44, for Hanu the Trickster's " +
                "rewritten printed Initiative - a sparse card-keyed list in the card " +
                "section, which is authoritative state no live state remembers.");
        }

        [Test]
        public void TheInterruptNamesTheCrossingHeroAndQueuesNobodyElse()
        {
            // The queue this shape carries is a queue of SUBJECTS, and several blasts under one hero
            // are several questions for the SAME hero - which the type refuses to hold, and which is
            // why the count picks a whole effect instead.
            Assert.That(
                () => new PendingEffectInterrupt(
                    null,
                    EffectInterruptKind.AfterMoveThroughTrapTrigger,
                    RuleObject.ForHero(Enemy),
                    new EffectResolutionId("fixture-host")),
                Throws.ArgumentNullException,
                "An effect interrupt is the trigger frame that is standing.");
        }

        // -------------------------------------------------------------------------------------
        // Driven: a whole Movement action over one of her mines, end to end.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AnEnemyMovementThroughABlast_RemovesIt_RevealsIt_AndAsksThatHeroForACard()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            var driven = StartEnemyMovement(board, out var authority, out var boundary);

            var answered = authority.AnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    driven.PendingChoice.ResolutionId,
                    driven.PendingChoice.ChoiceId,
                    new HexCoordinate(3, 0)),
                RuleModificationSet.Empty,
                boundary);

            Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
            Assert.That(board.PlacedTokens, Is.Empty, "\"remove it\", after the whole move.");

            var reveal = answered.Steps
                .Where(step => step.Kind == ResolutionStepKind.Fact)
                .Select(step => step.Fact)
                .Single(fact =>
                    fact.Kind == CardActionResolutionEventKind.MoveThroughTrapSprung);
            Assert.That(reveal.TargetHeroId, Is.EqualTo(Enemy));
            Assert.That(reveal.TargetTokenCoordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(reveal.TargetTokenDefinitionReference,
                Is.EqualTo(MinComponentContent.MineBlastDefinition.Reference),
                "The reveal names the real kind, which is the moment it stops being facedown.");
            Assert.That(reveal.MoveThroughTrapCardsOwed, Is.EqualTo(1));

            Assert.That(answered.IsComplete, Is.False,
                "The action is not over while the charge it set off is standing.");
            Assert.That(authority.PendingEffectInterrupt, Is.Not.Null);
            Assert.That(authority.PendingEffectInterrupt.Kind,
                Is.EqualTo(EffectInterruptKind.AfterMoveThroughTrapTrigger));
            Assert.That(authority.PendingEffectInterrupt.Subject,
                Is.EqualTo(RuleObject.ForHero(Enemy)));
            Assert.That(authority.PendingEffectInterrupt.RemainingSubjects, Is.Empty);

            var choice = authority.PendingEffectChoice;
            Assert.That(choice.ChooserId, Is.EqualTo(Enemy),
                "That hero discards a card OF THEIR OWN CHOICE, on their own seat.");
            Assert.That(choice.Candidates, Is.Not.Empty);
        }

        [Test]
        public void TheActingSeatIsRefusedTheDiscard_AndTheCrossingHerosAnswerIsTaken()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            var driven = StartEnemyMovement(board, out var authority, out var boundary);
            authority.AnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    driven.PendingChoice.ResolutionId,
                    driven.PendingChoice.ChoiceId,
                    new HexCoordinate(3, 0)),
                RuleModificationSet.Empty,
                boundary);

            var choice = authority.PendingEffectChoice;
            var refused = authority.AnswerEffectInterruptChoice(
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                    choice.Candidates[0], Min),
                boundary);
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.WrongChooser),
                "Min set the mine; she does not choose which card it costs.");

            var taken = authority.AnswerEffectInterruptChoice(
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                    choice.Candidates[0], Enemy),
                boundary);
            Assert.That(taken.Accepted, Is.True, taken.RejectionReason.ToString());
            Assert.That(authority.PendingEffectInterrupt, Is.Null,
                "One blast, one card, and the moment is over.");
            Assert.That(
                boundary.Instances.Count(card =>
                    card.OwnerId.Equals(Enemy) && card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(1),
                "\"discards a card\" - out of that hero's own hand.");
        }

        [Test]
        public void AnEmptyHandDiscardsNothingAndIsNotDefeated()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");
            var driven = StartEnemyMovement(board, out var authority, out var boundary, spareCards: 0);

            var answered = authority.AnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    driven.PendingChoice.ResolutionId,
                    driven.PendingChoice.ChoiceId,
                    new HexCoordinate(3, 0)),
                RuleModificationSet.Empty,
                boundary);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(board.PlacedTokens, Is.Empty, "The mine still goes off.");
            Assert.That(answered.IsComplete, Is.True, "And nothing is standing to be answered.");
            Assert.That(authority.PendingEffectInterrupt, Is.Null);
            Assert.That(authority.PendingEffectChoice, Is.Null,
                "\"if able\" is the skipping kind: an empty hand gives nothing up, and these three " +
                "faces print no \"or is defeated\" for it to escalate to.");
            Assert.That(
                board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(Enemy)), Is.True,
                "Still on the board.");
        }

        [Test]
        public void AFriendlyMovementOverTheSameMine_LeavesItStandingAndAsksNobodyAnything()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1", controller: Enemy);
            var driven = StartEnemyMovement(board, out var authority, out var boundary);

            var answered = authority.AnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    driven.PendingChoice.ResolutionId,
                    driven.PendingChoice.ChoiceId,
                    new HexCoordinate(3, 0)),
                RuleModificationSet.Empty,
                boundary);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(answered.IsComplete, Is.True);
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1),
                "A hero walks over their own side's mines and nothing happens at all.");
            Assert.That(authority.PendingEffectChoice, Is.Null);
            Assert.That(
                answered.Steps.Any(step =>
                    step.Kind == ResolutionStepKind.Fact &&
                    step.Fact.Kind == CardActionResolutionEventKind.MoveThroughTrapSprung),
                Is.False,
                "And nothing is revealed, because nothing was removed.");
        }

        // -------------------------------------------------------------------------------------
        // Fixture plumbing.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The build's own catalogs plus the trap's effects, deduplicated by id.
        ///
        /// <para><b>The concat is kept and the dedupe is what milestone 8.61j added</b>, which is
        /// milestone 8.40j's BA-18 lesson stated for a triggered-effect catalog rather than for a
        /// capability one: before the dealing this fixture SUPPLIED the four consequence definitions
        /// the build withheld, and after it the build holds them - so the same line handed the
        /// catalog each definition twice and it refused the duplicate. Reading the build's list alone
        /// would have worked today and quietly stopped exercising the composition the moment a fifth
        /// consequence existed; taking the union by id keeps the fixture saying what it meant.</para>
        /// </summary>
        private static MatchContentCatalogs TrapCatalogs()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            return new MatchContentCatalogs(
                build.Capabilities,
                build.Components,
                new TriggeredEffectCatalog(
                    build.TriggeredEffects.Definitions
                        .Concat(MinEffectContent.MineTrapEffects())
                        .GroupBy(effect => effect.Id.Value, StringComparer.Ordinal)
                        .Select(group => group.First())));
        }

        /// <summary>
        /// Starts the enemy hero's own Movement action from (0,0) with three spaces to spend, and hands
        /// back the authority and the boundary its consequences will need.
        /// </summary>
        private static CardActionResolutionResult StartEnemyMovement(
            AdjacentMoveAuthority board,
            out CardActionResolutionAuthority authority,
            out ActionBoundaryContext boundary,
            int spareCards = 1)
        {
            var card = MovementCard();
            var instances = new List<CardInstance>
            {
                CardInstance.CreateInHand(new CardInstanceId("enemy-card-1"), card.Reference, Enemy)
            };
            for (var index = 0; index < spareCards; index++)
            {
                instances.Add(CardInstance.CreateInHand(
                    new CardInstanceId($"enemy-spare-{index}"), card.Reference, Enemy));
            }

            var definitions = new[] { card };

            // p.7 commits a card face down and reveals it, and the turn's Movement is performed off the
            // card standing in the turn area. Doing it that way here is what makes the hand the
            // discard reads a real hand: the card being played is no longer in it.
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(instances[0]), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(instances[0]), Is.True);
            var offer = ActionOfferQuery
                .GetAvailableTurnActionOffers(definitions, instances, Enemy, instances[0].Id)
                .Single(candidate => candidate.ActionDefinitionId.Value == "fixture-primary-movement");

            authority = new CardActionResolutionAuthority(TrapCatalogs(), board);
            boundary = new ActionBoundaryContext(
                definitions, instances, Participants(), new ActiveEffectLifecycle(),
                TieBreakerState.Unrevealed, 1, 1);
            var started = authority.StartTurnMovementResolution(
                new ActionResolutionId("trap-move"), offer, definitions, instances,
                RuleModificationSet.Empty);
            Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());
            return started;
        }

        private static CardDefinition MovementCard() => new CardDefinition(
            new CardDefinitionId("fixture-movement-card"),
            "fixture-revision",
            "Fixture Movement Card",
            new InitiativeValue(5),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("fixture-primary-movement"),
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
            });

        private static MatchParticipants Participants() =>
            StagedBoardParticipants.CreateWithBlueAlly();

        private static MatchLogEntry TrapRow(int cardsOwed)
        {
            var log = new RuleEventLogProjection();
            log.ResetToSetup(Participants(), new TieBreakerState(TeamSide.Red));
            log.Append(new[]
            {
                RoundResolutionEvent.FromTurn(
                    0,
                    0,
                    TurnResolutionEvent.FromCardAction(
                        CardActionResolutionEvent.MoveThroughTrapSprung(
                            new ActionResolutionId("fixture-trap"),
                            new CardInstanceId("fixture-card"),
                            MinCardContent.PrimarySkill,
                            Min,
                            Enemy,
                            new TokenInstanceId("blast-1"),
                            MinComponentContent.MineBlastDefinition.Reference,
                            new HexCoordinate(2, 0),
                            cardsOwed)))
            });

            return MatchLogProjection.Derive(log.Entries)
                .Single(entry => entry.Kind == MatchLogEntryKind.MoveThroughTrapSprung);
        }

        private static PlacedToken PlaceMine(
            AdjacentMoveAuthority board,
            TokenDefinition definition,
            int q,
            string instanceId,
            HeroId? controller = null)
        {
            var owner = controller ?? Min;
            var token = new PlacedToken(
                new TokenInstanceId(instanceId), definition, owner, owner,
                new CardInstanceId("fixture-card"), new HexCoordinate(q, 0));
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        /// <summary>A straight file with Min at (-1,0) and the enemy she mined against at (0,0).</summary>
        private static AdjacentMoveAuthority LineBoard() => new AdjacentMoveAuthority(
            Enumerable.Range(-3, 13).Select(q => new HexCoordinate(q, 0)).ToArray(),
            new[]
            {
                new HeroOccupancy(Min, new HexCoordinate(-1, 0)),
                new HeroOccupancy(Enemy, new HexCoordinate(0, 0))
            });

        /// <summary>The same file with Min alone on it at (0,0), for the walk she takes herself.</summary>
        private static AdjacentMoveAuthority MinLineBoard() => new AdjacentMoveAuthority(
            Enumerable.Range(-3, 13).Select(q => new HexCoordinate(q, 0)).ToArray(),
            new[] { new HeroOccupancy(Min, MinAt) });

        /// <summary>A radius-3 disc, so two equal-length routes to one space exist.</summary>
        private static AdjacentMoveAuthority DiscBoard() => new AdjacentMoveAuthority(
            Disc(3), new[] { new HeroOccupancy(Min, MinAt) });

        private static AdjacentMoveAuthority MappedBoard()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return new AdjacentMoveAuthority(
                map,
                Enumerable.Empty<HeroOccupancy>(),
                Enumerable.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Enumerable.Empty<QuickGameMinionSetupState>());
        }

        private static HexCoordinate[] Disc(int radius)
        {
            var spaces = new List<HexCoordinate>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(MinAt, coordinate) <= radius)
                    {
                        spaces.Add(coordinate);
                    }
                }
            }

            return spaces.ToArray();
        }
    }
}
