using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittGlitchFoundationTests
    {
        [Test]
        public void GlitchAndDormantReverseTime_RoundTripThroughSnapshot()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var red = configuration.RedHand.Instances[0];
            var blue = configuration.BlueHand.Instances[0];
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(-1, 0), new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0), new HexCoordinate(2, 0)
                },
                new[]
                {
                    new HeroOccupancy(red.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue.OwnerId, new HexCoordinate(-1, 0))
                });
            var token = new PlacedToken(new TokenInstanceId("snapshot-glitch"),
                EmmittComponentContent.Glitch, red.OwnerId, red.OwnerId, red.Id,
                new HexCoordinate(2, 0));
            board.CommitValidatedTokenPlacement(token);
            var timing = new ActiveEffectLifecycle();
            var sourceAction = configuration.CardDefinitions.Single(value =>
                    value.Reference.Equals(red.DefinitionReference))
                .ActionDefinitions[0].Id;
            timing.Create(new ActiveEffectId("snapshot-reverse-time"),
                EmmittActiveEffectContent.ReverseTimeDefinition,
                red.OwnerId, TeamSide.Red, default, red.Id, sourceAction, 1, 1);
            var content = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    EmmittActiveEffectContent.Handlers().Concat(
                        EmmittComponentContent.Handlers())),
                new ComponentDefinitionCatalog(new[] { EmmittComponentContent.Glitch },
                    Array.Empty<MarkerDefinition>()));
            var actions = new CardActionResolutionAuthority(content, board);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions, configuration.CardInstances,
                configuration.Participants, configuration.TieBreakerState, 1,
                new RoundResolutionComposition(actions, board, timing,
                    new MarkerAttachmentRegister(), null, null, null, null, null, null));

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(content, payload,
                configuration.CardDefinitions, new[] { board.MapDefinition });

            Assert.That(restored.Board.PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(EmmittComponentContent.Glitch.Reference));
            Assert.That(restored.Board.PlacedTokens.Single().OwnerHeroId, Is.EqualTo(red.OwnerId));
            Assert.That(restored.Round.Timing.Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload));

            void ResolveOneTurn(RoundResolutionAuthority authority)
            {
                Assert.That(authority.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
                Assert.That(authority.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
                while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    var result = authority.CurrentTurnAuthority.PendingTurnRelease != null
                        ? authority.TryReleaseTurn()
                        : authority.TryPerformHold(
                            authority.CurrentTurnAuthority.SelectedActor.CardInstanceId);
                    Assert.That(result.Accepted, Is.True);
                }
            }

            ResolveOneTurn(round);
            ResolveOneTurn(restored.Round);
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(restored.Board.PlacedTokens, Is.Empty);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)),
                "The restored command continuation must replay token cleanup and next-turn effect state exactly.");
        }

        [Test]
        public void ThreeCopyGlitchSupply_OwnershipAndSwapRemainAuthoritative()
        {
            var emmitt = new HeroId("emmitt");
            var enemy = new HeroId("enemy");
            var source = new CardInstanceId("emmitt-source");
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 10).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(emmitt, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var catalog = new ComponentDefinitionCatalog(
                new[] { EmmittComponentContent.Glitch }, Array.Empty<MarkerDefinition>());
            Assert.That(EmmittComponentContent.Glitch.SupplyCount, Is.EqualTo(3));
            Assert.That(EmmittComponentContent.MinimumGlitchHexDistance, Is.EqualTo(3));
            Assert.That(EmmittComponentContent.Glitch.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.EndOfTurnCleanup));

            foreach (var q in new[] { 2, 5, 8 })
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId($"glitch-{q}"), EmmittComponentContent.Glitch,
                    emmitt, emmitt, source, new HexCoordinate(q, 0)));
            }
            Assert.That(catalog.CountAvailableTokens(
                EmmittComponentContent.Glitch.Reference, board.PlacedTokens), Is.Zero);

            var swapped = SwapOperation.Swap(SwapEligiblePieceKind.GamePiece,
                board, board, BoardObjectReference.ForHero(emmitt),
                BoardObjectReference.ForToken(new TokenInstanceId("glitch-5")));
            Assert.That(swapped.Accepted, Is.True);
            Assert.That(board.Occupancies.Single(value => value.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(new HexCoordinate(5, 0)));
            var moved = board.PlacedTokens.Single(value => value.Id.Value == "glitch-5");
            Assert.That(moved.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(moved.OwnerHeroId, Is.EqualTo(emmitt));
            Assert.That(moved.ControllerHeroId, Is.EqualTo(emmitt));
            Assert.That(moved.SourceCardInstanceId, Is.EqualTo(source));

            board.CommitValidatedTokenReclamation(moved.Id);
            Assert.That(catalog.CountAvailableTokens(
                EmmittComponentContent.Glitch.Reference, board.PlacedTokens), Is.EqualTo(1));
        }
    }
}
