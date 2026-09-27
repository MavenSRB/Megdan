using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112a. Mrak's rock token: a bare p.19 obstacle from a pile of three (MR-Q1), with
    /// p.8's end-of-round lifetime, p.18's short-supply reach and p.13's Clear.
    /// </summary>
    public sealed class MrakRockTokenTests
    {
        private static readonly HeroId Mrak = new HeroId("mrak-rock-test-hero");
        private static readonly CardInstanceId Source = new CardInstanceId("mrak-rock-test-source");

        [Test]
        public void TheRockIsABareImpassableObstacle_FromAPileOfThree()
        {
            var rock = MrakComponentContent.Rock;

            Assert.That(MrakComponentContent.RockSupplyCount, Is.EqualTo(3),
                "USER-SETTLED 2026-09-27 (MR-Q1): \"Mark has 3 tokens in supply\".");
            Assert.That(rock.SupplyCount, Is.EqualTo(3));
            Assert.That(rock.Id.Value, Is.EqualTo("mrak-rock"));
            Assert.That(rock.Properties, Is.Empty,
                "No face of his prints a property for a rock, so it declares none.");
            Assert.That(ObstacleQuery.DeclaresMayBeMovedThrough(rock), Is.False,
                "p.19: \"All tokens are obstacles\", and a rock declares no way through.");
            Assert.That(rock.RemovalCondition, Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup),
                "p.19: \"Unless the card text says otherwise, tokens are removed at the end of round\".");
            Assert.That(MrakComponentContent.Tokens().Select(token => token.Id),
                Is.EqualTo(new[] { MrakComponentContent.RockId }));
        }

        [Test]
        public void ThePileHoldsThree_AndAFourthFromTheSupplyIsRefused()
        {
            var board = Line();
            var catalog = Catalog();
            Assert.That(catalog.CountAvailableTokens(
                MrakComponentContent.RockReference, board.PlacedTokens), Is.EqualTo(3));

            foreach (var q in new[] { 2, 3, 4 })
            {
                board.CommitValidatedTokenPlacement(Rock(q));
            }

            Assert.That(catalog.CountAvailableTokens(
                MrakComponentContent.RockReference, board.PlacedTokens), Is.EqualTo(0));

            var outcome = PlaceOperation.PlaceFromSupply(
                new PlacePolicy(
                    PlaceSourceContainer.Supply,
                    PlaceEligiblePieceKind.Token,
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                    6,
                    PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    PlaceDestinationAnchor.ActingHero),
                board, catalog, Rock(5), new HexCoordinate(0, 0),
                Array.Empty<HexCoordinate>(), RuleModificationSet.Empty);
            Assert.That(outcome.Accepted, Is.False, "A rock that is not in the pile is not placed.");

            Assert.That(catalog.ReclaimableForPlacement(
                    MrakComponentContent.RockReference, board.PlacedTokens).Count(),
                Is.EqualTo(3),
                "p.18's short supply may take back any standing rock - all three are his type.");
        }

        [Test]
        public void ARockClosesTheRouteThroughItsSpace()
        {
            var open = Line();
            var openReach = open.FindDirectMoveDestinations(
                    BoardObjectReference.ForHero(Mrak), 2, RuleModificationSet.Empty, false)
                .Select(destination => destination.Destination).ToArray();
            Assert.That(openReach, Has.Member(new HexCoordinate(2, 0)));

            var blocked = Line();
            blocked.CommitValidatedTokenPlacement(Rock(1));
            var blockedReach = blocked.FindDirectMoveDestinations(
                    BoardObjectReference.ForHero(Mrak), 2, RuleModificationSet.Empty, false)
                .Select(destination => destination.Destination).ToArray();

            Assert.That(blockedReach, Has.No.Member(new HexCoordinate(1, 0)),
                "nobody may stand on a rock");
            Assert.That(blockedReach, Has.No.Member(new HexCoordinate(2, 0)),
                "and on a one-file board the rock closes the only route past it.");
        }

        [Test]
        public void EveryRockLeavesAtTheEndOfTheRound()
        {
            var board = Line();
            board.CommitValidatedTokenPlacement(Rock(1));
            board.CommitValidatedTokenPlacement(Rock(3));

            Assert.That(
                ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens)
                    .Select(token => token.Id),
                Is.EquivalentTo(board.PlacedTokens.Select(token => token.Id)));
        }

        [Test]
        public void ClearReplacingHisSecondaryAttackRemovesAnAdjacentRock()
        {
            var board = Line();
            board.CommitValidatedTokenPlacement(Rock(1));
            board.CommitValidatedTokenPlacement(Rock(3));
            var definition = new CardDefinition(
                new CardDefinitionId("mrak-rock-test-card"), "rev-1", "TEST - Seismic rail",
                new InitiativeValue(7), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                    MrakCardContent.SecondaryAttack(7)
                });
            var card = CardInstance.CreateInHand(Source, definition.Reference, Mrak);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);

            var clear = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                MrakCardContent.SecondaryAttackId, card.Id, Mrak, new[] { definition }, new[] { card },
                RuleModificationSet.Empty);
            Assert.That(clear, Is.Not.Null);
            var legal = ClearActionQuery.FindLegalTokenIds(
                clear.Value, MrakCardContent.SecondaryAttackId, new[] { definition }, new[] { card },
                board, RuleModificationSet.Empty);
            Assert.That(legal, Is.EqualTo(new[] { Rock(1).Id }),
                "p.13: Clear removes tokens ADJACENT to you - the rock two spaces off stays.");

            var result = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board)
                .PerformTurnClear(
                    new ActionResolutionId("mrak-rock-clear"), clear.Value,
                    MrakCardContent.SecondaryAttackId, legal, new[] { definition }, new[] { card },
                    RuleModificationSet.Empty);
            Assert.That(result.Accepted, Is.True);
            Assert.That(board.PlacedTokens.Select(token => token.Coordinate),
                Is.EqualTo(new[] { new HexCoordinate(3, 0) }));
        }

        // -------------------------------------------------------------------------------------------

        /// <summary>One file, q = -1..6, with Mrak at the origin.</summary>
        private static AdjacentMoveAuthority Line() =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("mrak-rock-line"), 1,
                    Enumerable.Range(-1, 8).Select(q => new HexCoordinate(q, 0)).ToArray()),
                new[] { new HeroOccupancy(Mrak, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());

        private static ComponentDefinitionCatalog Catalog() =>
            new ComponentDefinitionCatalog(
                MrakComponentContent.Tokens(), Array.Empty<MarkerDefinition>());

        private static PlacedToken Rock(int q) =>
            new PlacedToken(
                new TokenInstanceId("mrak-rock-" + q),
                MrakComponentContent.Rock,
                Mrak,
                Mrak,
                Source,
                new HexCoordinate(q, 0));
    }
}
