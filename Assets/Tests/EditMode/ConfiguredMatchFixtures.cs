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

            redHeroId = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            return CreateSession(
                configuration, map, WithAMeleeMinionBesideTheRedHero(map, setup, out target));
        }

        /// <summary>
        /// Milestone 8.4. The verified mirror with an enemy minion standing beside the Red Wasp, so an
        /// Attack offer in this position provably has a target under milestone 7.8a1's rule. That is
        /// what lets the card-text rule be observed on its own: an Attack greyed here cannot have been
        /// greyed for having nobody to hit.
        /// </summary>
        public static MatchSession CreateVerifiedMirrorWithAnAdjacentEnemyMinion(out MinionId target)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            return CreateSession(
                configuration, map, WithAMeleeMinionBesideTheRedHero(map, setup, out target));
        }

        /// <summary>One Blue melee minion moved to a free space beside the Red hero.</summary>
        private static QuickGameSetupState WithAMeleeMinionBesideTheRedHero(
            BoardMapDefinition map, QuickGameSetupState setup, out MinionId target)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
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
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(relocatedTarget)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId,
                        minion.TeamId,
                        minion.TeamSide,
                        minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(map.Id, besideSpaceId),
                        beside)
                    : minion)
                .ToArray();

            return new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, setup.Heroes,
                minions);
        }

        /// <summary>
        /// Milestone 7.9. The configured match with the two heroes standing beside each other and the
        /// stated number of Life counters per team, so ordinary play reaches p.13's Defeat, its respawn
        /// and its ending instead of needing thousands of commands to find them.
        /// </summary>
        /// <remarks>
        /// Everything else is the configured setup: both full minion waves, the declared Wave count,
        /// the same ten dealt cards. The two changes are the ones that make a mirrored 1v1 test
        /// configuration behave like a match with something at stake, and neither invents a rule - a
        /// starting position and a counter count are setup content, which is exactly what
        /// <see cref="QuickGameSetupState"/> carries. The adjacency is the same one
        /// <c>MatchVictoryPathTests</c> builds for the same reason.
        /// </remarks>
        public static MatchSession CreateEngagedSession(int lifeCountersPerTeam)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var heroes = WithBlueHeroBesideTheRedOne(map, setup);
            return CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams
                    .Select(team => new QuickGameTeamSetupState(
                        team.TeamId, team.TeamSide, lifeCountersPerTeam, team.ThroneZoneId))
                    .ToArray(),
                heroes,
                setup.Minions));
        }

        /// <summary>
        /// Milestone 7.9. The same adjacency with the configured counter counts untouched, for a
        /// scenario that needs two heroes within reach of each other and nothing else changed.
        /// </summary>
        public static MatchSession CreateAdjacentConfiguredSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            return CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions));
        }

        /// <summary>
        /// Milestone 8.5. The two heroes standing beside each other with the card-text cards dealt, so
        /// a fixture can watch rulebook p.14's numbered order with real text in it.
        /// </summary>
        public static MatchSession CreateAttackTextSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForAttackTextFixture(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            return CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions));
        }

        /// <summary>
        /// The same, with a Blue melee minion moved beside the Red hero as well, so a fixture can ask
        /// what an Attack narrowed to p.13's heroes alone is offered when both a hero and a minion
        /// stand next to it.
        /// </summary>
        public static MatchSession CreateAttackTextSessionWithAnAdjacentMinion(out MinionId minionId)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForAttackTextFixture(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var withMinion = WithAMeleeMinionBesideTheRedHero(map, setup, out minionId);

            return CreateSession(configuration, map, new QuickGameSetupState(
                withMinion.SelectedMap,
                withMinion.WaveCount,
                withMinion.TieBreakerState,
                withMinion.Teams,
                WithBlueHeroBesideTheRedOne(map, withMinion),
                withMinion.Minions));
        }

        /// <summary>
        /// The configured match milestone 8.4 made verified: two mirrored Wasps on the official map,
        /// dealt rulebook p.5's starting five with the remaining thirteen in their decks, with p.5's
        /// coin flip replaced by an explicit side so a fixture can play it deterministically.
        /// </summary>
        public static MatchSession CreateVerifiedMirrorSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            return CreateSession(configuration, map, setup);
        }

        /// <summary>
        /// A showcase attacker beside a verified defender: the same adjacent placement
        /// <see cref="CreateAdjacentConfiguredSession"/> builds, with Wasp's cards in the Blue hand so
        /// a verified secondary Defense can answer a real attack. Milestone 8.4.
        /// </summary>
        public static MatchSession CreateVerifiedDefenderSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForVerifiedDefenseFixture(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            return CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions));
        }

        /// <summary>
        /// Milestone 7.9. The configured match with exactly one Blue minion, standing in the starting
        /// Battle Zone, so p.8's Minion Battle removes it and awards Red the push at the end of every
        /// round - milestone 7.7's Throne-capture scenario, reused so a generated match can reach p.8's
        /// two push endings rather than only p.13's.
        /// </summary>
        public static MatchSession CreateUncontestedPushSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue &&
                minion.MinionType != StandardMinionType.Heavy &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));

            return CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                setup.Heroes,
                setup.Minions
                    .Where(minion => minion.TeamSide != TeamSide.Blue ||
                        minion.MinionId.Equals(survivor.MinionId))
                    .ToArray()));
        }

        private static IReadOnlyList<QuickGameHeroSetupState> WithBlueHeroBesideTheRedOne(
            BoardMapDefinition map, QuickGameSetupState setup)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            return setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();
        }

        private static MatchSession CreateSession(
            PlaceholderOneVsOneTestConfiguration configuration,
            BoardMapDefinition map,
            QuickGameSetupState setup)
        {
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

        /// <summary>
        /// A configured match stopped with one Active effect live, and the card that sourced it named.
        ///
        /// Milestone 8.5b. The producer is the showcase Bulwark card's modification Skill in its
        /// Defense-bonus mode - the same synthetic effect `ConfiguredEffectLifetimeGateTests` drives to
        /// <c>Active</c>, reused rather than reinvented so the two agree about what a live effect is.
        /// It is a synthetic producer by the milestone's own acceptance rule: the complete transition
        /// is proven before Wasp composes a persistent card at 8.9.
        ///
        /// <para>The Blue commitment is the lowest-Initiative card in that hand, which is what puts
        /// Bulwark's hero on the first turn; a card whose effect never started would leave the fixture
        /// asserting nothing, so the caller's first assertion is that something really is live.</para>
        /// </summary>
        public static MatchSession CreateSessionWithALiveActiveEffect(out CardInstanceId sourceCardId)
        {
            var bulwarkCardId = new CardInstanceId("red-bulwark-card");
            var session = CreateConfiguredSession();
            var blue = session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue)
                .OrderBy(card => session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            foreach (var id in new[] { bulwarkCardId, blue.Id })
            {
                var card = session.Projection.Cards.Single(candidate => candidate.Id.Equals(id));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            Accept(session, DirectorCommand.StartAction(
                new ActionDefinitionId("primary-modification-skill")));
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations
                .Single(candidate => candidate.Candidate.OptionNodeId.Equals(
                    new GuardsDigital.Core.Effects.EffectNodeId("bulwark-defense-bonus")))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            sourceCardId = bulwarkCardId;
            return session;
        }

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
