using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueRoundSwapTests
    {
        private static readonly HeroId Red =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Friend =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Actor =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        [Test]
        public void RoundSwapReplacesRecordedSlotAndSurvivesSnapshotRestore()
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Blue);
            var friendResolved = CardInstance.CreateInHand(
                new CardInstanceId("friend-fast-silver"),
                SnorriSilverFace.Silver.Reference, Friend);
            var friendHand = CardInstance.CreateInHand(
                new CardInstanceId("friend-hidden-hand"),
                SnorriBlueMovementFaces.Definitions[1].Reference, Friend);
            var friendItem = CardInstance.CreateInDeck(
                new CardInstanceId("friend-boon-item"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Friend);
            var actorCard = CardInstance.CreateInHand(
                new CardInstanceId("actor-boon"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Actor);
            var definitions = configuration.CardDefinitions
                .Concat(new[] { SnorriSilverFace.Silver })
                .Concat(SnorriBlueMovementFaces.Definitions)
                .Concat(SnorriBlueSupportFaces.Definitions)
                .ToArray();
            var instances = configuration.CardInstances
                .Concat(new[] { friendResolved, friendHand, friendItem, actorCard }).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("snorri-support-swap"), 1,
                new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                    new HexCoordinate(-1, 0)
                });
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Friend, new HexCoordinate(1, 0)),
                    new HeroOccupancy(Red, new HexCoordinate(-1, 0))
                }, Array.Empty<ObstacleOccupancy>());
            var markers = new MarkerAttachmentRegister();
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers()
                        .Concat(SnorriBlueSupportCapabilities.Handlers())),
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers()),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var actions = new CardActionResolutionAuthority(catalogs, board, markers);
            var round = new RoundResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Blue), 1,
                RoundResolutionComposition.ForMatch(
                    actions, board, new ActiveEffectLifecycle(), markers, null, null));
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(friendItem), Is.True);
            Assert.That(round.HeroItems.TryPlaceSetAsideCard(
                friendItem, definitions).IsPlaced, Is.True);
            var redCard = configuration.CardInstances.First(card =>
                card.OwnerId.Equals(Red) && card.State.Location == CardLocation.Hand);
            foreach (var card in new[] { friendResolved, actorCard, redCard })
                Assert.That(round.TryCommitCard(card.OwnerId, card.Id).Accepted, Is.True);

            for (var guard = 0; guard < 20 &&
                !CardLifecycleAuthority.IsResolvedCardInTurnArea(friendResolved); guard++)
            {
                var turn = round.CurrentTurnAuthority;
                if (turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(round.TryChooseTiedActor(
                        tie.OwningSide,
                        tie.CandidateCardInstanceIds.Contains(friendResolved.Id)
                            ? friendResolved.Id : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                }
                else if (turn.PendingTurnRelease != null)
                    Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                else
                    Assert.That(round.TryPerformHold(
                        turn.SelectedActor.CardInstanceId).Accepted, Is.True);
            }
            Assert.That(CardLifecycleAuthority.IsResolvedCardInTurnArea(friendResolved),
                Is.True);
            Assert.That(round.TrySwapResolvedWithHand(
                Friend, friendResolved.Id, friendHand.Id), Is.True);
            Assert.That(friendResolved.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(CardLifecycleAuthority.IsResolvedCardInTurnArea(friendHand), Is.True);
            Assert.That(round.TurnSlots.Single().Assignments.Single(assignment =>
                assignment.HeroId.Equals(Friend)).CardInstanceId,
                Is.EqualTo(friendHand.Id));
            Assert.That(round.TrySwapResolvedWithHand(
                Friend, friendResolved.Id, friendHand.Id), Is.False,
                "A stale answer cannot swap the original slot twice.");

            var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, snapshot, definitions, new[] { map },
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(restored.Round.TurnSlots.Single().Assignments.Single(assignment =>
                assignment.HeroId.Equals(Friend)).CardInstanceId,
                Is.EqualTo(friendHand.Id));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                restored.Round, restored.Board), Is.EqualTo(snapshot));

            Assert.That(restored.Round.TrySwapItemWithCard(
                Friend, friendItem.Id, friendHand.Id), Is.True,
                "An item may exchange with an owned resolved card of the same tier and color.");
            Assert.That(restored.Round.TurnSlots.Single().Assignments.Single(assignment =>
                assignment.HeroId.Equals(Friend)).CardInstanceId,
                Is.EqualTo(friendItem.Id));
            Assert.That(restored.Round.HeroItems.GetItems(Friend).Single().SourceCardInstanceId,
                Is.EqualTo(friendHand.Id));
            var afterItemSwap = AuthoritativeSnapshotSerializer.Serialize(
                restored.Round, restored.Board);
            var restoredAgain = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, afterItemSwap, definitions, new[] { map },
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                restoredAgain.Round, restoredAgain.Board), Is.EqualTo(afterItemSwap));
        }
    }
}
