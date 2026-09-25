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
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueMovementTurnTests
    {
        private static readonly HeroId Snorri =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId BlueAlly =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        [TestCase(0, 0, 3, 0)]
        [TestCase(0, 1, 3, 1)]
        [TestCase(1, 0, 3, 0)]
        [TestCase(1, 1, 3, 1)]
        [TestCase(1, 3, 3, 1)]
        [TestCase(2, 0, 3, 0)]
        [TestCase(2, 1, 3, 1)]
        [TestCase(2, 3, 3, 1)]
        [TestCase(2, 4, 5, 1)]
        public void ACurrentSlotRuneCreatesOnlyItsPrintedPassageClause(
            int faceIndex, int runeOrdinal, int expectedAllowance,
            int expectedEffectCount)
        {
            var run = Start(faceIndex, runeOrdinal);
            Assert.That(run.Choice.RemainingAllowance, Is.EqualTo(expectedAllowance));
            Assert.That(run.Round.Timing.Effects.Count, Is.EqualTo(expectedEffectCount));
            Assert.That(run.Choice.LegalDestinations.Any(route =>
                route.Destination.Equals(new HexCoordinate(2, 0))),
                Is.EqualTo(runeOrdinal == 1),
                "The only route to q2_r0 crosses terrain; a This Turn obstacle permission " +
                "must apply before the Movement route is offered.");
        }

        [Test]
        public void FirstRunePassesThroughTerrainButCannotEndThere()
        {
            var noRune = Start(0, 0);
            var denied = noRune.Round.TryAnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    noRune.Choice.ResolutionId, noRune.Choice.ChoiceId,
                    new HexCoordinate(2, 0)));
            Assert.That(denied.Accepted, Is.False);
            Assert.That(noRune.Board.TryGetHeroCoordinate(
                Snorri, out var unchanged), Is.True);
            Assert.That(unchanged, Is.EqualTo(new HexCoordinate(0, 0)));

            var rune = Start(0, 1);
            Assert.That(rune.Choice.LegalDestinations.Any(route =>
                route.Destination.Equals(new HexCoordinate(1, 0))), Is.False,
                "Ignore obstacles permits transit, never an obstacle endpoint.");
            Assert.That(rune.Round.TryAnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    rune.Choice.ResolutionId, rune.Choice.ChoiceId,
                    new HexCoordinate(2, 0))).Accepted, Is.True);
            Assert.That(rune.Board.TryGetHeroCoordinate(
                Snorri, out var destination), Is.True);
            Assert.That(destination, Is.EqualTo(new HexCoordinate(2, 0)));
        }

        private sealed class Started
        {
            public RoundResolutionAuthority Round;
            public PendingMovementChoice Choice;
            public AdjacentMoveAuthority Board;
        }

        private static Started Start(int faceIndex, int runeOrdinal)
        {
            var configuration = MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                TeamSide.Red);
            var face = SnorriBlueMovementFaces.Definitions[faceIndex];
            var movement = face.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Movement);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("snorri-passage-played"), face.Reference, Snorri);
            var definitions = configuration.CardDefinitions.Concat(new[] { face }).ToArray();
            var instances = configuration.CardInstances.Concat(new[] { played }).ToArray();
            var map = new MapId("snorri-passage-line");
            var spaces = new[]
            {
                new HexCoordinate(-1, 0), new HexCoordinate(0, -1),
                new HexCoordinate(0, 0),
                new HexCoordinate(2, 0), new HexCoordinate(3, 0)
            };
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    map, 1, spaces, new[] { new HexCoordinate(1, 0) }),
                new[]
                {
                    new HeroOccupancy(Snorri, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Blue, new HexCoordinate(-1, 0)),
                    new HeroOccupancy(BlueAlly, new HexCoordinate(0, -1))
                }, Array.Empty<ObstacleOccupancy>());
            var markers = new MarkerAttachmentRegister();
            if (runeOrdinal > 0)
                markers.Give(new MarkerAttachment(
                    new MarkerInstanceId("snorri-passage-rune"),
                    SnorriComponentContent.Runes[runeOrdinal - 1],
                    Snorri, Snorri, new CardInstanceId("snorri-inscribe"),
                    SnorriSilverFace.PrimarySkill, 1));
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers().Concat(
                        SnorriBlueActiveEffectContent.Handlers())),
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(),
                    SnorriComponentContent.Markers()),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var timing = new ActiveEffectLifecycle();
            var actions = new CardActionResolutionAuthority(catalogs, board, markers);
            var round = new RoundResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForMatch(
                    actions, board, timing, markers, null, null));
            foreach (var committed in new[]
            {
                played,
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(Blue) &&
                    card.State.Location == CardLocation.Hand),
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(BlueAlly) &&
                    card.State.Location == CardLocation.Hand)
            })
                Assert.That(round.TryCommitCard(committed.OwnerId, committed.Id).Accepted,
                    Is.True);
            var turn = round.CurrentTurnAuthority;
            for (var step = 0; step < 20; step++)
            {
                if (turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(round.TryChooseTiedActor(
                        tie.OwningSide,
                        tie.CandidateCardInstanceIds.Contains(played.Id)
                            ? played.Id : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                    continue;
                }
                if (turn.PendingTurnRelease != null)
                {
                    Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                    continue;
                }
                if (turn.SelectedActor.CardInstanceId.Equals(played.Id))
                    break;
                Assert.That(round.TryPerformHold(
                    turn.SelectedActor.CardInstanceId).Accepted, Is.True);
            }
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    definitions, instances, Snorri, played.Id)
                .Single(value => value.ActionDefinitionId.Equals(movement.Id));
            Assert.That(round.TryStartMovement(
                new ActionResolutionId("snorri-passage"), offer).Accepted,
                Is.True, face.DisplayName);
            Assert.That(turn.PendingMovementChoice, Is.Not.Null, face.DisplayName);
            return new Started
            {
                Round = round,
                Choice = turn.PendingMovementChoice,
                Board = board
            };
        }
    }
}
