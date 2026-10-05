using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
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
        public void CurrentTurnDiscardHistory_RecordsEntriesAndSurvivesSnapshotRestore()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redPlayed = configuration.RedHand.Instances[0];
            var bluePlayed = configuration.BlueHand.Instances[0];
            var oldDiscard = configuration.RedHand.Instances[1];
            var newDiscard = configuration.RedHand.Instances[2];
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(oldDiscard), Is.True);
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0) },
                new[]
                {
                    new HeroOccupancy(redPlayed.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(bluePlayed.OwnerId, new HexCoordinate(-1, 0))
                });
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions, configuration.CardInstances,
                configuration.Participants, configuration.TieBreakerState, 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalogs, board), board,
                    new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                    null, null, null, null, null, null));
            Assert.That(round.TryCommitCard(redPlayed.OwnerId, redPlayed.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(bluePlayed.OwnerId, bluePlayed.Id).Accepted, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn));
            Assert.That(round.WasDiscardedThisTurn(oldDiscard.Id), Is.False,
                "a discard that predates the turn is not eligible");
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(newDiscard), Is.True);
            Assert.That(round.WasDiscardedThisTurn(newDiscard.Id), Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, payload,
                configuration.CardDefinitions, new[] { board.MapDefinition });
            Assert.That(restored.Round.WasDiscardedThisTurn(oldDiscard.Id), Is.False);
            Assert.That(restored.Round.WasDiscardedThisTurn(newDiscard.Id), Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload));
        }

        [Test]
        public void TimeCapsule_RetrievesOnlyTheOwnersCurrentTurnDiscards()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redPlayed = configuration.RedHand.Instances[0];
            var bluePlayed = configuration.BlueHand.Instances[0];
            var oldDiscard = configuration.RedHand.Instances[1];
            var newDiscard = configuration.RedHand.Instances[2];
            var enemyDiscard = configuration.BlueHand.Instances[1];
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(oldDiscard), Is.True);
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, 0), new HexCoordinate(1, 0) },
                new[]
                {
                    new HeroOccupancy(redPlayed.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(bluePlayed.OwnerId, new HexCoordinate(-1, 0))
                });
            var catalog = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions, configuration.CardInstances,
                configuration.Participants, configuration.TieBreakerState, 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalog, board), board,
                    new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                    null, null, null, null, null, null));
            Assert.That(round.TryCommitCard(redPlayed.OwnerId, redPlayed.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(bluePlayed.OwnerId, bluePlayed.Id).Accepted, Is.True);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(newDiscard), Is.True);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(enemyDiscard), Is.True);

            var handler = catalog.Capabilities.Load(EmmittTimeCapsuleContent.RetrieveCurrentTurnDiscardsV1);
            var context = new EffectCapabilityContext(
                new EffectNodeId("time-capsule-outcome"), EffectFailurePolicy.Mandatory,
                redPlayed.OwnerId, redPlayed.OwnerId, redPlayed.Id,
                redPlayed.DefinitionReference, EmmittCardContent.PrimarySkill,
                new Dictionary<string, EffectValue>
                {
                    [EmmittTimeCapsuleContent.OwnerParameter] = EffectValue.Hero(redPlayed.OwnerId)
                },
                1, 1, configuration.Participants, round.Timing, RuleModificationSet.Empty,
                EffectCardState.Of(configuration.CardDefinitions, configuration.CardInstances),
                ActionKind.Skill,
                rosterState: EffectRosterState.Of(round.TurnSlots,
                    new RoundHeroDefeatHistory(configuration.Participants),
                    new HeroCoinLedger(configuration.Participants),
                    currentTurnCardHistory: round));
            Assert.That(handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(handler.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(newDiscard.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(oldDiscard.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(enemyDiscard.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(handler.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

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
                BoardObjectReference.ForToken(new TokenInstanceId("glitch-5")),
                RuleModificationSet.Empty);
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

        [Test]
        public void EnemyHeroChoosesOnlyTheActorsStandingGlitches_AndAReclaimedChoiceIsStale()
        {
            var match = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var actorCard = match.RedHand.Instances[0];
            var actor = actorCard.OwnerId;
            var enemy = match.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 9).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                });
            board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("owned-glitch"),
                EmmittComponentContent.Glitch, actor, actor, actorCard.Id, new HexCoordinate(2, 0)));
            board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("other-glitch"),
                EmmittComponentContent.Glitch, enemy, enemy, actorCard.Id, new HexCoordinate(5, 0)));

            var definition = match.CardDefinitions.Single(card =>
                card.Reference.Equals(actorCard.DefinitionReference));
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(new[] { EmmittComponentContent.Glitch },
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board);
            EffectCapabilityContext Context(IReadOnlyDictionary<string, EffectValue> inputs) =>
                new EffectCapabilityContext(new EffectNodeId("emmitt-glitch-probe"),
                    EffectFailurePolicy.Mandatory, actor, actor, actorCard.Id,
                    actorCard.DefinitionReference, definition.ActionDefinitions[0].Id, inputs,
                    1, 1, match.Participants, null,
                    GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                    EffectCardState.Of(match.CardDefinitions, match.CardInstances),
                    ActionKind.Skill, components);

            var enemyHandler = EmmittGlitchChoiceContent.Handlers().Single(handler =>
                handler.Reference.Equals(EmmittGlitchChoiceContent.EnemyHeroInPlayV1));
            var enemyChoice = enemyHandler.Execute(Context(new Dictionary<string, EffectValue>()));
            Assert.That(enemyChoice.Choice.Candidates, Does.Contain(EffectValue.Hero(enemy)));

            var tokenHandler = EmmittGlitchChoiceContent.Handlers().Single(handler =>
                handler.Reference.Equals(EmmittGlitchChoiceContent.ChooseGlitchByHeroV1));
            var tokenContext = Context(new Dictionary<string, EffectValue>
            {
                [EmmittGlitchChoiceContent.ChooserParameter] = EffectValue.Hero(enemy)
            });
            var tokenChoice = tokenHandler.Execute(tokenContext);
            Assert.That(tokenChoice.Choice.ChooserHeroId, Is.EqualTo(enemy));
            Assert.That(tokenChoice.Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Space(new HexCoordinate(2, 0)) }));
            board.CommitValidatedTokenReclamation(new TokenInstanceId("owned-glitch"));
            Assert.That(tokenHandler.Resume(tokenContext,
                new[] { EffectValue.Space(new HexCoordinate(2, 0)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }
    }
}
