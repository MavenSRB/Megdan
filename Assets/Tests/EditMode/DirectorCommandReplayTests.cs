using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class DirectorCommandReplayTests
    {
        [Test]
        public void CommandHistory_RetainsTypedAcceptedPrefixWithoutSnapshotPayloads()
        {
            var history = new DirectorCommandHistory();
            var hero = new HeroId("replay-red");
            var card = new CardInstanceId("replay-card");
            var commit = DirectorCommand.CommitCard(hero, card);
            var destination = DirectorCommand.ChooseMovementDestination(new HexCoordinate(2, -1));

            history.Append(commit);
            history.Append(destination);

            Assert.That(history.Commands, Is.EqualTo(new[] { commit, destination }));
            Assert.That(history.WithoutLatest(), Is.EqualTo(new[] { commit }));
            Assert.That(history.Commands, Has.Count.EqualTo(2));
            Assert.That(history.Commands.Select(command => command.Kind),
                Is.EqualTo(new[] { DirectorCommandKind.CommitCard, DirectorCommandKind.ChooseMovementDestination }));

            history.ReplaceWith(history.WithoutLatest());
            Assert.That(history.Commands, Is.EqualTo(new[] { commit }));
            history.Clear();
            Assert.That(history.Commands, Is.Empty);
        }

        [Test]
        public void InitialScenario_RecreatesFreshCardsBoardMapAndChosenTieBreaker()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("replay-map"),
                3,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            var blueHero = configuration.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(redHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueHero, new HexCoordinate(2, 0))
                },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                board.SelectedMap,
                board.Occupancies,
                board.ObstacleOccupancies);

            var firstConfiguration = scenario.CreateConfiguration();
            var secondConfiguration = scenario.CreateConfiguration();
            var firstBoard = scenario.CreateBoard(new[] { map });
            var secondBoard = scenario.CreateBoard(new[] { map });

            Assert.That(firstConfiguration, Is.Not.SameAs(secondConfiguration));
            Assert.That(firstConfiguration.CardInstances[0], Is.Not.SameAs(secondConfiguration.CardInstances[0]));
            Assert.That(firstConfiguration.CardInstances.Select(value => value.State),
                Is.EqualTo(secondConfiguration.CardInstances.Select(value => value.State)));
            Assert.That(firstConfiguration.TieBreakerState,
                Is.EqualTo(TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Blue)));
            Assert.That(firstBoard, Is.Not.SameAs(secondBoard));
            Assert.That(firstBoard.SelectedMap, Is.EqualTo(new MapDefinitionReference(new MapId("replay-map"), 3)));
            Assert.That(firstBoard.Occupancies, Is.EqualTo(secondBoard.Occupancies));
        }

        [Test]
        public void ClearCommand_CanonicalizesItsChosenTokenSetForReplayIdentity()
        {
            var attack = new ActionDefinitionId("clear-replay-attack");
            var first = new TokenInstanceId("token-a");
            var second = new TokenInstanceId("token-b");

            var forward = DirectorCommand.PerformClear(attack, new[] { first, second });
            var reverse = DirectorCommand.PerformClear(attack, new[] { second, first });

            Assert.That(reverse, Is.EqualTo(forward));
            Assert.That(reverse.GetHashCode(), Is.EqualTo(forward.GetHashCode()));
            Assert.That(reverse.TokenInstanceIds, Is.EqualTo(new[] { first, second }));
            Assert.That(
                () => DirectorCommand.PerformClear(attack, new[] { first, first }),
                Throws.ArgumentException,
                "Clear carries a chosen subset, so a duplicate is invalid rather than two removals.");
        }
    }
}
