using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The configured-match session fixtures more than one fixture needs.
    ///
    /// Extracted at milestone 7.8a, when the player-facing surfaces needed the same live match paused on
    /// rulebook p.15's Upgrade a Card that <see cref="EndOfRoundLevelUpStageTests"/> already built. The
    /// alternative was a second copy of a hundred lines that has to agree with the first one about what
    /// "a round that earned a coin" means, and two copies that disagreed would leave one of the two
    /// fixtures quietly proving something else.
    ///
    /// Coins are still earned rather than granted, for the reason the original comment gives: p.13's
    /// Defeat is the only source this build has, and a fixture that reached into the ledger would prove
    /// a stage against a balance no match could have.
    /// </summary>
    public static class ConfiguredMatchFixtures
    {
        public static CardLocation LocationOf(MatchSession session, CardInstanceId cardInstanceId) =>
            session.Projection.Cards.Single(card => card.Id.Equals(cardInstanceId)).State.Location;

        public static MatchCommandResult Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        /// <summary>
        /// Crosses p.8's earlier end-of-round steps, answering the Minion Battle when it asks. The
        /// relocated minion this fixture defeats stood outside the Battle Zone, so its team is one short
        /// there and p.8 makes them remove the difference before the round can go on.
        /// </summary>
        public static void AdvanceToStage(MatchSession session, EndOfRoundStage stage)
        {
            while (session.Projection.CurrentEndOfRoundStage != stage)
            {
                Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                    "The round left the end of round before reaching the stage under test.");
                var battle = session.Projection.PendingMinionBattleChoice;
                if (battle != null)
                {
                    while (session.Projection.PendingMinionBattleChoice.SelectInvocations.Count > 0)
                    {
                        Accept(session, DirectorCommand.SelectMinionBattleRemoval(
                            session.Projection.PendingMinionBattleChoice.SelectInvocations[0]));
                    }

                    Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(
                        session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                    continue;
                }

                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }
        }

        public static void FinishEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }
        }

        /// <summary>
        /// A configured match in which the Red hero defeats one enemy melee minion in the first turn and
        /// then plays the round out, so it reaches p.8's Level Up step holding p.13's two coins.
        ///
        /// Coins are earned rather than granted, because a Defeat is the only source this build has and a
        /// fixture that reached into the ledger would prove the stage against a balance no match could
        /// have.
        /// </summary>
        public static MatchSession CreateSessionWithACoinEarningRound(out HeroId redHeroId)
        {
            var session = CreateSessionWithAnAdjacentEnemyMinion(out redHeroId, out var target);
            var redCard = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blueCard = session.Projection.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            Accept(session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));
            Accept(session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Red Pursuit's Initiative 7 acts before Blue Longbow's 4.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetMinionIds, Has.Member(target));
            Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));
            Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)),
                Is.False, "The Attack has to defeat the minion, or the round collects no coin.");

            PlayOutTheRestOfTheRound(session);
            return session;
        }

        /// <summary>
        /// The official-map configured match with one Blue melee minion standing beside the Red hero's
        /// spawn point.
        ///
        /// The minion is relocated rather than added, and it is relocated out of the Battle Zone rather
        /// than to any free space: Blue keeps every other minion, so defeating this one triggers no
        /// immediate Lane Push, and the Battle Zone's counts are the same either way, so p.8's Minion
        /// Battle ends with no effect and this fixture stays about the Level Up stage.
        /// </summary>
        public static MatchSession CreateSessionWithAnAdjacentEnemyMinion(
            out HeroId redHeroId, out MinionId target)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            redHeroId = redHero.HeroId;
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));

            var moved = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee);
            target = moved.MinionId;
            var relocatedTarget = target;
            var relocatedBeside = beside;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(relocatedTarget)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId,
                        minion.TeamId,
                        minion.TeamSide,
                        minion.MinionType,
                        minion.SpawnPoint,
                        map.Spaces.Single(space =>
                            space.Kind == BoardMapSpaceKind.Playable &&
                            space.Coordinate.Equals(relocatedBeside)).Id is var spaceId
                            ? new ConfiguredSpaceReference(map.Id, spaceId)
                            : throw new InvalidOperationException(),
                        relocatedBeside)
                    : minion)
                .ToArray();
            var relocatedSetup = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, setup.Heroes,
                minions);

            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                relocatedSetup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(
                new MatchSessionInitialState(scenario, relocatedSetup, new[] { map }));
        }

        /// <summary>The ordinary configured match on the official map, with nobody earning a coin.</summary>
        public static MatchSession CreateConfiguredSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        public static MatchSession CreateRestoreTarget() =>
            CreateSessionWithAnAdjacentEnemyMinion(out _, out _);

        public static void PlayOneOrdinaryRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                CommitAndHoldOneTurn(session);
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
        }

        public static void PlayOutTheRestOfTheRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            PlayOneOrdinaryRound(session);
        }

        public static void CommitAndHoldOneTurn(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.FirstOrDefault(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand);
                if (card == null)
                {
                    var hero = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
                    Accept(session, DirectorCommand.Pass(hero));
                    continue;
                }

                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var turnIndex = session.Projection.TurnIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                session.Projection.TurnIndex == turnIndex)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        /// <summary>
        /// A configured match stopped exactly where rulebook p.15's Upgrade a Card asks its question,
        /// which is the pause milestone 7.8a's contextual region has to be able to draw.
        /// </summary>
        public static MatchSession CreateSessionPausedOnLevelUpUpgrade(out HeroId redHeroId)
        {
            var session = CreateSessionWithACoinEarningRound(out redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null,
                "p.15 stops the end of round on the upgrade the purchased level forces.");
            return session;
        }

        /// <summary>
        /// Two heroes standing next to each other on a four-space synthetic map, with a Red Attack
        /// resolved as far as the Blue hero's Defense response.
        ///
        /// It is deliberately its own map rather than the official one: the configured setup starts the
        /// two heroes far apart, and every existing Defense fixture in this project builds a small board
        /// for the same reason. No minion stands on it, because the contribution cues are milestone
        /// 5.8b's subject and this one is about the response surface.
        /// </summary>
        public static MatchSession CreateSessionAwaitingADefenseResponse()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("hud-defense-fixture"),
                1,
                new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(1, 1),
                    new HexCoordinate(2, 0)
                });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(redHeroId, new HeroContentId("red-hud"), "Red Hud",
                        redTeam.Id, TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")), redCoordinate),
                    new QuickGameHeroSetupState(blueHeroId, new HeroContentId("blue-hud"), "Blue Hud",
                        blueTeam.Id, TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")), blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));

            var red = HandCards(session, TeamSide.Red)
                .Where(card => DefinitionOf(session, card).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Attack))
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            var blue = HandCards(session, TeamSide.Blue)
                .OrderBy(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, red.Id));
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
            var attack = session.Projection.AvailableTurnActions.First(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null);
            return session;
        }

        /// <summary>
        /// The same match one command later: the defending hero answered with a Defense card, which the
        /// reaction discards, so that hero's tray has a card in the discard pile.
        /// </summary>
        public static MatchSession CreateSessionWithADiscardedCard(out HeroId defendingHeroId)
        {
            var session = CreateSessionAwaitingADefenseResponse();
            var choice = session.Projection.PendingDefenseChoice;
            var chooser = choice.ChooserId;
            defendingHeroId = chooser;
            Accept(session, DirectorCommand.ChooseDefenseCard(
                choice.LegalDefenseOffers[0].SourceCardInstanceId));
            Assert.That(
                session.Projection.Cards.Any(card =>
                    card.OwnerId.Equals(chooser) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.True,
                "Answering with a Defense card discards it.");
            return session;
        }

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));

        private static IReadOnlyList<CardInstance> HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(card =>
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
    }
}
