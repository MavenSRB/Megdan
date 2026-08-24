using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
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
        /// Milestone 8.6. The verified mirror with the two Wasps standing beside each other, so a
        /// fixture can watch one of them move the other with a card off the printed starting five.
        ///
        /// The enemy hero is the subject deliberately. A minion beside her would work as well for one
        /// orbit and not for a second: p.18's after-action correction returns a minion displaced out of
        /// the Battle Zone before the repeated instance starts, which is the rule working correctly and
        /// a fixture watching the wrong thing.
        /// </summary>
        public static MatchSession CreateEngagedVerifiedMirrorSession() =>
            CreateEngagedFixtureSession(
                PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(TeamSide.Red));

        /// <summary>
        /// Milestone 8.16. The same engaged verified mirror with the stated number of Life counters per
        /// team - the verified twin of <see cref="CreateEngagedSession"/>, which milestone 7.9's gate
        /// plays with the showcase hands.
        ///
        /// Both changes are setup content rather than rules: <see cref="QuickGameSetupState"/> carries a
        /// starting position and a counter count, and what they buy is a mirror that reaches one of the
        /// rulebook's endings inside a bounded command budget instead of playing past two hundred
        /// rounds without one.
        /// </summary>
        public static MatchSession CreateEngagedVerifiedMirrorSession(int lifeCountersPerTeam)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
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
                setup.Teams
                    .Select(team => new QuickGameTeamSetupState(
                        team.TeamId, team.TeamSide, lifeCountersPerTeam, team.ThroneZoneId))
                    .ToArray(),
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions));
        }

        /// <summary>
        /// Milestone 8.16. The verified twin of <see cref="CreateUncontestedPushSession"/>: Blue holds
        /// exactly one minion in the starting Battle Zone, so p.8's Minion Battle removes it and awards
        /// Red the push at the end of every round until the Lane reaches the Blue Throne.
        /// </summary>
        public static MatchSession CreateUncontestedVerifiedPushSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
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

        /// <summary>
        /// Milestone 8.6. The two heroes standing beside each other with the orbit Skill dealt, so a
        /// fixture can watch a subject move around the actor and a bounded repeat move it again.
        ///
        /// The adjacency is what makes the card resolvable at all: the internal-test setup stands the
        /// two heroes at opposite ends of the map, and an orbit needs something inside the acting
        /// card's printed Radius to orbit.
        /// </summary>
        public static MatchSession CreateOrbitSession() =>
            CreateEngagedFixtureSession(
                PlaceholderOneVsOneTestConfiguration.CreateForOrbitFixture(TeamSide.Red));

        /// <summary>
        /// Milestone 8.19a. The two heroes standing beside each other with the four roster continuation
        /// shapes dealt, so a fixture can watch rulebook p.14's step 1 stop, an Attack's own text name
        /// what it attacks, a defender's step-4 rider stop, and a Movement's text be placed either side
        /// of it.
        ///
        /// <para>The adjacency is what makes any of them resolvable: three of the four are Attacks or
        /// Defenses, and the internal-test setup stands the two heroes at opposite ends of the map.</para>
        /// </summary>
        public static MatchSession CreateContinuationSession() =>
            CreateEngagedFixtureSession(
                PlaceholderOneVsOneTestConfiguration.CreateForContinuationFixture(TeamSide.Red));

        /// <summary>
        /// Milestone 8.19a. The same board with the Red hero's Ultimate already beside the dashboard,
        /// so the `before-action-trigger@1` it declares is in effect.
        ///
        /// <para>The unlock happens on the configuration's own instances before the session exists,
        /// which is the order milestone 8.13's High Voltage fixture established: rulebook p.16's
        /// transition is the one way a card reaches the Ultimate area, and a session built first would
        /// be holding a different copy of the card.</para>
        /// </summary>
        public static MatchSession CreateContinuationSessionWithUnlockedUltimate()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForContinuationFixture(
                TeamSide.Red);
            var redHero = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var ultimate = configuration.CardInstances.Single(card =>
                card.OwnerId.Equals(redHero) &&
                configuration.CardDefinitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Progression.IsUltimate);
            if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
            {
                throw new InvalidOperationException(
                    "The continuation fixture could not unlock the Red hero's Ultimate, so the " +
                    "before-action trigger it declares would not be in effect.");
            }

            return CreateEngagedFixtureSession(configuration);
        }

        /// <summary>
        /// Milestone 8.6. The kinesis Skill dealt with a Blue minion standing inside the acting card's
        /// printed Range but off every file through the actor, so a fixture can watch p.19's
        /// straight-line complement decide what the selection may name.
        ///
        /// The enemy hero stands beside the actor as well, and that is the case the complement is for:
        /// p.19 says adjacent units are in a straight line, so the hero is exactly the piece a
        /// "not in a straight line" card may not touch however close it is.
        /// </summary>
        public static MatchSession CreateKinesisSession(out MinionId outOfLineMinionId)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForKinesisFixture(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var engaged = new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions);

            return CreateSession(
                configuration, map, WithAMeleeMinionOutOfLineFromTheRedHero(
                    map, engaged, out outOfLineMinionId));
        }

        /// <summary>
        /// One Blue melee minion moved to the nearest empty playable space that is inside range 3 of the
        /// Red hero and on no file through them.
        ///
        /// The space is searched for rather than written down, because which coordinates satisfy both
        /// conditions is a fact about the official map's geography and the setup standing on it, and a
        /// literal would go stale the first time either moved.
        /// </summary>
        private static QuickGameSetupState WithAMeleeMinionOutOfLineFromTheRedHero(
            BoardMapDefinition map, QuickGameSetupState setup, out MinionId target)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var destination = map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Where(space => !occupied.Contains(space.Coordinate))
                .Where(space =>
                    HexDistanceQuery.Calculate(redHero.Coordinate, space.Coordinate) <= 3 &&
                    !StraightLineQuery.AreInStraightLine(redHero.Coordinate, space.Coordinate))
                .OrderBy(space => HexDistanceQuery.Calculate(redHero.Coordinate, space.Coordinate))
                .ThenBy(space => space.Coordinate.Q)
                .ThenBy(space => space.Coordinate.R)
                .First();

            var moved = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee);
            target = moved.MinionId;
            var relocatedTarget = target;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(relocatedTarget)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId,
                        minion.TeamId,
                        minion.TeamSide,
                        minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(map.Id, destination.Id),
                        destination.Coordinate)
                    : minion)
                .ToArray();

            return new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, setup.Heroes,
                minions);
        }

        /// <summary>
        /// Milestone 8.8. The verified mirror with one of Wasp's kinetic Skills in the Red hand and the
        /// Blue Wasp standing on a chosen side of her.
        ///
        /// <para><b>How the card gets there.</b> Kinetic Repulse and Kinetic Blast are Tier II and III,
        /// so p.6 leaves them in the deck; what puts one in a hand is p.15's Level Up, which swaps a
        /// card for one of its own upgrade candidates. This does exactly that swap at setup rather than
        /// playing four rounds to reach it: the Blue-colour card of the printed starting five moves to
        /// the deck and the named kinetic card takes its place, so the hand is still p.15's "exactly 5
        /// cards, 1 of each color". Only the Red seat is touched, because only the Red seat acts.</para>
        ///
        /// <para><b>Where the two heroes stand.</b> The direction is the fixture's real variable, and it
        /// is searched for rather than written down. <paramref name="stoppedShort"/> asks for the
        /// neighbouring space whose file runs into something inside Kinetic Repulse's three spaces - on
        /// the official map that is its terrain ring, which is p.19's water - and its opposite asks for
        /// one open through Kinetic Blast's four. A literal coordinate would go stale the first time the
        /// map or the spawn points moved, and the two cases are the same fixture differing only in which
        /// side of the actor the target stands on.</para>
        /// </summary>
        public static MatchSession CreateKineticSession(CardDefinitionId kineticCardId, bool stoppedShort)
        {
            var configuration = WithCardSwappedIntoTheRedHand(kineticCardId, out var redHeroId);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var session = CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroOnAPushLine(map, setup, stoppedShort),
                setup.Minions));

            AssertTheHandStillHoldsFive(session, redHeroId);
            return session;
        }

        /// <summary>
        /// Milestone 8.10. The verified mirror with one of Wasp's Boomerangs in the Red hand, the Blue
        /// Wasp standing beside her, and one Blue melee minion inside Range 3 on no file through her.
        ///
        /// <para>It is the discriminating board for "Target a unit in range and not in a straight
        /// line". The adjacent hero is exactly what p.19 puts on a straight line at separation one -
        /// which is what the face's own reminder says - and the minion is the only unit in reach the
        /// sentence admits, so a build that dropped the complement would offer two targets where the
        /// card prints one. Both stand inside Range 3, so nothing here is decided by distance.</para>
        /// </summary>
        public static MatchSession CreateBoomerangSession(
            CardDefinitionId boomerangCardId, out MinionId outOfLineMinionId)
        {
            var configuration = WithCardSwappedIntoTheRedHand(boomerangCardId, out var redHeroId);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var engaged = new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions);

            var session = CreateSession(
                configuration, map,
                WithAMeleeMinionOutOfLineFromTheRedHero(map, engaged, out outOfLineMinionId));

            AssertTheHandStillHoldsFive(session, redHeroId);
            return session;
        }

        /// <summary>
        /// The verified mirror with one named card swapped into the Red hand at setup, in place of the
        /// printed starting card of its own colour.
        ///
        /// <para>Milestone 8.8 first needed this and milestone 8.10 needs the same move for both
        /// Boomerangs, so it is stated once. p.6 leaves Tier II and III cards in the deck; what puts
        /// one in a hand is p.15's Level Up, which swaps a card for one of its own upgrade candidates,
        /// and this performs exactly that swap at setup rather than playing the rounds needed to reach
        /// it. Only the Red seat is touched, because only the Red seat acts, and matching the colour is
        /// what keeps the hand at p.15's "exactly 5 cards, 1 of each color".</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.10. Electroblast's rider on a board where its second arm is the one that can
        /// happen: the Blue hero holds exactly the card they commit to this turn, so by the time
        /// rulebook p.14's step 7 asks them for one, they have none.
        ///
        /// <para>The board is otherwise the rider's own - a minion beside the Red hero to attack, and
        /// the Blue hero at distance 2, inside Electroblast's Radius 3 and off the ring its sentence
        /// removes. What differs is the hand, and it is emptied by putting four of p.5's starting five
        /// in the deck rather than by playing rounds to spend them, so nothing about the case depends
        /// on which cards those rounds happened to take.</para>
        /// </summary>
        public static MatchSession CreateEscalatingRiderSession(out MinionId adjacentMinionId)
        {
            var configuration = WithCardSwappedIntoTheRedHand(
                WaspCardContent.Electroblast, out var redHeroId, emptyTheOpposingHand: true);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var withMinion = WithAMeleeMinionBesideTheRedHero(map, setup, out adjacentMinionId);
            var session = CreateSession(configuration, map, new QuickGameSetupState(
                withMinion.SelectedMap,
                withMinion.WaveCount,
                withMinion.TieBreakerState,
                withMinion.Teams,
                WithBlueHeroAtDistance(map, withMinion, 2),
                withMinion.Minions));

            AssertTheHandStillHoldsFive(session, redHeroId);
            return session;
        }

        /// <summary>
        /// Milestone 8.10. Thunder Boomerang's board: both the Blue hero and a Blue melee minion stand
        /// inside Range 3 and on no file through the Red hero, so the card admits both of them.
        ///
        /// <para>Two off-line units is what the repeat needs and what nothing before it needed. The
        /// hero is there because "If you targeted a hero" is only true of a hero, and the minion is
        /// there because "on a different target" needs somewhere else to go - a board with one legal
        /// target would end the repeat for lack of anyone rather than for the reason under test.</para>
        /// </summary>
        public static MatchSession CreateThunderBoomerangSession(out MinionId outOfLineMinionId)
        {
            var configuration = WithCardSwappedIntoTheRedHand(
                WaspCardContent.ThunderBoomerang, out var redHeroId);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var withHero = new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroOutOfLineFromTheRedHero(map, setup),
                setup.Minions);

            var session = CreateSession(
                configuration, map,
                WithAMeleeMinionOutOfLineFromTheRedHero(map, withHero, out outOfLineMinionId));

            AssertTheHandStillHoldsFive(session, redHeroId);
            return session;
        }

        /// <summary>
        /// The Blue hero moved to the nearest empty playable space inside Range 3 of the Red hero and
        /// on no file through them - the same search
        /// <see cref="WithAMeleeMinionOutOfLineFromTheRedHero"/> runs, asked about the hero.
        /// </summary>
        private static IReadOnlyList<QuickGameHeroSetupState> WithBlueHeroOutOfLineFromTheRedHero(
            BoardMapDefinition map, QuickGameSetupState setup)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var destination = map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Where(space => !occupied.Contains(space.Coordinate))
                .Where(space =>
                    HexDistanceQuery.Calculate(redHero.Coordinate, space.Coordinate) <= 3 &&
                    !StraightLineQuery.AreInStraightLine(redHero.Coordinate, space.Coordinate))
                .OrderBy(space => HexDistanceQuery.Calculate(redHero.Coordinate, space.Coordinate))
                .ThenBy(space => space.Coordinate.Q)
                .ThenBy(space => space.Coordinate.R)
                .First();

            return setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint,
                        new ConfiguredSpaceReference(map.Id, destination.Id),
                        destination.Coordinate)
                    : hero)
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.11. Thunder Boomerang in the attacker's hand and one of Wasp's Block cards in
        /// the defender's, with the two heroes inside Range 3 and off every file through the attacker.
        ///
        /// <para>It is the only board on which a Block's rider can be watched at all: the Block
        /// condition is "a ranged attack", so the attack has to be the Ranged one, and the Boomerang's
        /// own printed targeting refuses anyone standing on a file through it. Deflect and Reflect are
        /// dealt by the same swap p.15's Level Up performs, because both are Tier II and III and p.6
        /// leaves them in the deck.</para>
        /// </summary>
        public static MatchSession CreateBlockRiderSession(CardDefinitionId blockCardId)
        {
            var configuration = WithCardsSwappedIntoBothHands(
                WaspCardContent.ThunderBoomerang, blockCardId, out var redHeroId);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var session = CreateSession(configuration, map, new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroOutOfLineFromTheRedHero(map, setup),
                setup.Minions));

            AssertTheHandStillHoldsFive(session, redHeroId);
            return session;
        }

        private static PlaceholderOneVsOneTestConfiguration WithCardSwappedIntoTheRedHand(
            CardDefinitionId cardId, out HeroId redHeroId) =>
            WithCardSwappedIntoTheRedHand(cardId, out redHeroId, emptyTheOpposingHand: false);

        /// <summary>
        /// The same setup-time Level Up swap performed once per seat: one named card into the acting
        /// hero's hand and one into the opposing hero's, each in place of that hand's card of its own
        /// colour, so both hands keep p.15's "exactly 5 cards, 1 of each color".
        /// </summary>
        private static PlaceholderOneVsOneTestConfiguration WithCardsSwappedIntoBothHands(
            CardDefinitionId redCardId, CardDefinitionId blueCardId, out HeroId redHeroId)
        {
            var source = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
            var state = source.CaptureRestoreState();
            redHeroId = state.Participants.Heroes
                .Single(hero => state.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var redOwner = redHeroId;

            CardInstance Swap(CardInstance instance, bool isRedSeat, CardDefinitionId wanted)
            {
                var onThisSeat = instance.OwnerId.Equals(redOwner) == isRedSeat;
                if (!onThisSeat)
                {
                    return null;
                }

                if (instance.DefinitionReference.DefinitionId.Equals(wanted))
                {
                    return CardInstance.CreateInHand(
                        instance.Id, instance.DefinitionReference, instance.OwnerId);
                }

                var wantedColour = state.Definitions.Single(d => d.Id.Equals(wanted)).Progression.Color;
                var thisColour = state.Definitions
                    .Single(d => d.Reference.Equals(instance.DefinitionReference)).Progression.Color;
                return instance.State.Location == CardLocation.Hand && thisColour == wantedColour
                    ? CardInstance.CreateInDeck(
                        instance.Id, instance.DefinitionReference, instance.OwnerId)
                    : null;
            }

            return PlaceholderOneVsOneTestConfiguration.Restore(
                new PlaceholderConfigurationRestoreState(
                    state.Participants,
                    state.Definitions,
                    state.Instances.Select(instance =>
                        Swap(instance, true, redCardId) ??
                        Swap(instance, false, blueCardId) ??
                        instance),
                    state.TieBreakerState,
                    state.SetupSeed));
        }

        private static PlaceholderOneVsOneTestConfiguration WithCardSwappedIntoTheRedHand(
            CardDefinitionId cardId, out HeroId redHeroId, bool emptyTheOpposingHand)
        {
            var source = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
            var state = source.CaptureRestoreState();
            redHeroId = state.Participants.Heroes
                .Single(hero => state.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var upgraded = state.Definitions.Single(definition => definition.Id.Equals(cardId));
            var replaced = source.RedHand.Instances.Single(instance =>
                state.Definitions
                    .Single(definition =>
                        definition.Reference.Equals(instance.DefinitionReference))
                    .Progression.Color == upgraded.Progression.Color);

            var owner = redHeroId;

            // Milestone 8.10. Everything the opposing hero holds except the card they will commit goes
            // to the deck, so p.14's step 7 finds a hero with nothing to give. Stop Projectiles is the
            // one kept because it is what every fixture here commits against: its printed Initiative 3
            // is the lowest in the starting hand, so the Red card still acts first.
            CardInstance Opposing(CardInstance instance) =>
                emptyTheOpposingHand &&
                !instance.OwnerId.Equals(owner) &&
                instance.State.Location == CardLocation.Hand &&
                !instance.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles)
                    ? CardInstance.CreateInDeck(
                        instance.Id, instance.DefinitionReference, instance.OwnerId)
                    : instance;

            return PlaceholderOneVsOneTestConfiguration.Restore(
                new PlaceholderConfigurationRestoreState(
                    state.Participants,
                    state.Definitions,
                    state.Instances.Select(instance =>
                        instance.OwnerId.Equals(owner) &&
                        instance.DefinitionReference.DefinitionId.Equals(cardId)
                            ? CardInstance.CreateInHand(
                                instance.Id, instance.DefinitionReference, instance.OwnerId)
                            : instance.Id.Equals(replaced.Id)
                                ? CardInstance.CreateInDeck(
                                    instance.Id, instance.DefinitionReference, instance.OwnerId)
                                : Opposing(instance)),
                    state.TieBreakerState,
                    state.SetupSeed));
        }

        private static void AssertTheHandStillHoldsFive(MatchSession session, HeroId redHeroId) =>
            Assert.That(
                session.Projection.Cards.Count(card =>
                    card.OwnerId.Equals(redHeroId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(5),
                "p.15 leaves the hand at five cards, one of each colour; the swap has to keep it there.");

        /// <summary>
        /// The Blue hero moved to the neighbouring space of the Red one whose push line closes early or
        /// stays open, as the caller asks.
        ///
        /// "Closes" is asked of the map rather than of a live board, because the answer has to hold
        /// before any session exists: a space that is not playable is the terrain ring or the nothing
        /// beyond it, and either one stops a push. Nothing else stands on these files at setup.
        /// </summary>
        private static IReadOnlyList<QuickGameHeroSetupState> WithBlueHeroOnAPushLine(
            BoardMapDefinition map, QuickGameSetupState setup, bool stoppedShort)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));

            bool RunsClear(HexCoordinate beside, int spaces)
            {
                var step = new HexCoordinate(
                    beside.Q - redHero.Coordinate.Q, beside.R - redHero.Coordinate.R);
                var walked = beside;
                for (var space = 1; space <= spaces; space++)
                {
                    walked = new HexCoordinate(walked.Q + step.Q, walked.R + step.R);
                    if (!playable.Contains(walked) || occupied.Contains(walked))
                    {
                        return false;
                    }
                }

                return true;
            }

            // Three spaces is Kinetic Repulse's distance and four is Kinetic Blast's greater one, so
            // the stopped line has to close inside the shorter push and the clear one has to stay open
            // through the longer.
            var beside = redHero.Coordinate.GetNeighbors()
                .Where(coordinate =>
                    playable.Contains(coordinate) && !occupied.Contains(coordinate))
                .FirstOrDefault(coordinate => stoppedShort
                    ? !RunsClear(coordinate, 3)
                    : RunsClear(coordinate, 4));
            Assert.That(playable, Does.Contain(beside),
                "The official map has to offer a space beside the Red hero whose push line " +
                (stoppedShort ? "stops short" : "runs clear") + ", or this fixture proves nothing.");

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

        /// <summary>
        /// Milestone 8.9. The configured mirror with something for Magnetic Dagger to hit and something
        /// left standing for its After-the-attack text to pin.
        ///
        /// <para>Both placements are needed and neither is arbitrary. A Blue melee minion stands beside
        /// the Red hero because p.13 resolves an Attack on a minion without a Defense response - "it is
        /// simply defeated" - so the attack runs to p.14's step 7 inside one command and nothing about
        /// the pin depends on how a defender answered. The Blue hero stands two spaces away because the
        /// card's radius is 3 and its target must be adjacent: at distance 2 the hero is inside the
        /// disk and is not the thing being attacked, which is the only position where "enemy units in
        /// radius" can be observed to contain a unit the Attack did not remove.</para>
        /// </summary>
        public static MatchSession CreateMagneticDaggerSession(out MinionId adjacentMinionId)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var withMinion = WithAMeleeMinionBesideTheRedHero(map, setup, out adjacentMinionId);
            return CreateSession(configuration, map, new QuickGameSetupState(
                withMinion.SelectedMap,
                withMinion.WaveCount,
                withMinion.TieBreakerState,
                withMinion.Teams,
                WithBlueHeroAtDistance(map, withMinion, 2),
                withMinion.Minions));
        }

        /// <summary>
        /// Milestone 8.10. Shock's and Electrocute's After-the-attack rider needs exactly the board
        /// milestone 8.9 built for Magnetic Dagger, for exactly the same two reasons: a Blue melee
        /// minion beside the Red hero, so p.13's minion arm runs the whole attack inside one command
        /// and reaches p.14's step 7, and the Blue hero at distance 2, which is inside Shock's printed
        /// Radius 2 and outside the ring "and not adjacent to you" removes.
        ///
        /// <para>It delegates rather than copying. A second board that happened to differ would make
        /// the two milestones' claims incomparable, and the position is the only one where "an enemy
        /// hero in radius and not adjacent to you" has a subject that the Attack did not itself
        /// remove.</para>
        /// </summary>
        public static MatchSession CreateRadiusDiscardRiderSession(out MinionId adjacentMinionId) =>
            CreateMagneticDaggerSession(out adjacentMinionId);

        /// <summary>
        /// Milestone 8.20a. A Blue melee minion beside the Red hero <em>and</em> the Blue hero
        /// adjacent to him, so one printed non-ranged Attack offers a hero and a minion at once.
        ///
        /// <para>No fixture had that board before: milestone 8.9's Magnetic Dagger session puts the
        /// Blue hero at distance 2, and the fixtures whose subject is a hero Defense reach the hero
        /// with a ranged Attack or across an empty space. The combination is the one milestone
        /// 8.20a's player-path journeys walked into, and
        /// <c>PromptReachabilityConformanceTests.EveryLegalAttackTargetOffer_IsAcceptedWhenItIsChosen</c>
        /// is what it is for.</para>
        /// </summary>
        public static MatchSession CreateAdjacentHeroAndMinionTargetSession(
            out MinionId adjacentMinionId,
            bool stagedArien = false)
        {
            var configuration = stagedArien
                ? PlaceholderOneVsOneTestConfiguration.CreateStagedArienForInternalTest(TeamSide.Red)
                : PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                    TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                stagedArien
                    ? QuickGameSetupConfiguration.CreateStagedWaspVersusArienOneVsOne()
                    : QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var withMinion = WithAMeleeMinionBesideTheRedHero(map, setup, out adjacentMinionId);
            return CreateSession(configuration, map, new QuickGameSetupState(
                withMinion.SelectedMap,
                withMinion.WaveCount,
                withMinion.TieBreakerState,
                withMinion.Teams,
                WithBlueHeroAtDistance(map, withMinion, 1),
                withMinion.Minions));
        }

        /// <summary>
        /// The Blue hero moved to a free playable space exactly <paramref name="spaces"/> from the Red
        /// one, measured the way p.18's Near/Far counts - spaces, obstacles included.
        /// </summary>
        private static IReadOnlyList<QuickGameHeroSetupState> WithBlueHeroAtDistance(
            BoardMapDefinition map, QuickGameSetupState setup, int spaces)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Where(hero => hero.TeamSide != TeamSide.Blue)
                    .Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var space = map.Spaces
                .Where(candidate => candidate.Kind == BoardMapSpaceKind.Playable &&
                    !occupied.Contains(candidate.Coordinate) &&
                    HexDistanceQuery.Calculate(redHero.Coordinate, candidate.Coordinate) == spaces)
                .OrderBy(candidate => candidate.Coordinate.Q)
                .ThenBy(candidate => candidate.Coordinate.R)
                .FirstOrDefault();
            Assert.That(space, Is.Not.Null,
                $"The official map has to offer a free playable space {spaces} from the Red hero, or " +
                "this fixture proves nothing.");

            return setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint,
                        new ConfiguredSpaceReference(map.Id, space.Id),
                        space.Coordinate)
                    : hero)
                .ToArray();
        }

        /// <summary>
        /// One session from one named fixture configuration, with the Blue hero moved beside the Red
        /// one. Extracted at milestone 8.6 from the identical bodies above it.
        /// </summary>
        private static MatchSession CreateEngagedFixtureSession(
            PlaceholderOneVsOneTestConfiguration configuration)
        {
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
        public static MatchSession CreateVerifiedMirrorSession() =>
            CreateVerifiedMirrorSession(TeamSide.Red);

        /// <summary>
        /// Milestone 8.16. The same verified mirror with `DEV-005`'s internal Tie Breaker override set
        /// to the named side, so a fixture can ask what the override actually decides.
        /// </summary>
        public static MatchSession CreateVerifiedMirrorSession(TeamSide initialTieBreakerSide)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                initialTieBreakerSide);
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

        /// <summary>
        /// Both hands' response to the current turn's commitment, and the Initiative tie it may raise:
        /// each hero commits their first held card, or passes when p.7 leaves them nothing to commit.
        ///
        /// Split out of <see cref="CommitAndHoldOneTurn"/> at milestone 8.7a, which needed the position
        /// that turn reaches halfway through - both cards revealed, neither resolved - rather than only
        /// the one it ends at.
        /// </summary>
        public static void CommitBothHands(MatchSession session)
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
        }

        public static void CommitAndHoldOneTurn(MatchSession session)
        {
            CommitBothHands(session);
            var turnIndex = session.Projection.TurnIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                session.Projection.TurnIndex == turnIndex)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        /// <summary>
        /// A configured match with exactly one hero's card committed facedown: the other has not
        /// responded, so rulebook p.7's joint reveal has not happened and the committed card's identity
        /// is not in the projection at all.
        ///
        /// Milestone 8.7a. It is the position the rail's facedown marker exists for, and the one that
        /// proves the marker cannot leak a card it is never told.
        /// </summary>
        public static MatchSession CreateSessionWithAFacedownCommitment(out HeroId committedHeroId)
        {
            var session = CreateConfiguredSession();
            var card = session.Projection.Cards.First(candidate =>
                session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == TeamSide.Red &&
                candidate.State.Location == CardLocation.Hand);
            committedHeroId = card.OwnerId;
            Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            Assert.That(
                session.Projection.GetCommitment(card.OwnerId).Status,
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown),
                "The fixture only proves the marker while the commitment really is still facedown.");
            return session;
        }

        /// <summary>
        /// A configured match at the moment rulebook p.7's joint reveal ends a turn's commitment: both
        /// heroes' cards are face up in the turn area, both unresolved, and nobody has acted.
        ///
        /// Milestone 8.7a. It is the position the round's own assignment history already names both
        /// cards in, which is what made the four numbered slots fill before a turn was played. It deals
        /// the verified mirror rather than the showcase hands, so the face the marker reveals carries
        /// Wasp's own transcribed words rather than a synthetic card that prints none.
        /// </summary>
        public static MatchSession CreateSessionWithARevealedUnresolvedTurn()
        {
            var session = CreateVerifiedMirrorSession();
            CommitBothHands(session);
            Assert.That(
                session.Projection.Commitments.All(commitment =>
                    commitment.Status == TurnCardCommitmentStatus.Revealed),
                Is.True,
                "The fixture only proves the marker while both cards really are revealed.");
            return session;
        }

        /// <summary>
        /// A configured match in which the Blue hero was dealt one card instead of p.6's five, so that
        /// from the round's second turn onwards they have an empty hand and rulebook p.7's Pass is the
        /// only response they can make.
        ///
        /// Milestone 8.7a. A Pass is unreachable in ordinary configured play - four turns are spent out
        /// of a hand of five and the end of round retrieves what was played - so the hand is trimmed at
        /// setup rather than emptied by commands that do not exist. The trim is the one thing changed:
        /// the four dropped instances are removed from the configuration before it is restored, and
        /// every definition, the map, the participants and the Tie Breaker are the configured ones.
        /// <see cref="ConfiguredHand"/> re-derives its dealt/deck split from the upgrade closure when it
        /// restores, so its own split no longer matches where each card actually is; nothing a match
        /// reads uses that split, because the session is composed from the instances and their states.
        /// </summary>
        public static MatchSession CreateSessionWithAPassingHero(out HeroId passingHeroId)
        {
            var source = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var state = source.CaptureRestoreState();
            var dropped = new HashSet<CardInstanceId>(
                source.BlueHand.Instances.Skip(1).Select(instance => instance.Id));
            var configuration = PlaceholderOneVsOneTestConfiguration.Restore(
                new PlaceholderConfigurationRestoreState(
                    state.Participants,
                    state.Definitions,
                    state.Instances.Where(instance => !dropped.Contains(instance.Id)),
                    state.TieBreakerState,
                    state.SetupSeed));
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var session = CreateSession(configuration, map, setup);
            var blueHeroId = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            passingHeroId = blueHeroId;
            Assert.That(
                session.Projection.Cards.Count(card =>
                    card.OwnerId.Equals(blueHeroId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(1),
                "The trim leaves exactly the one card that empties the hand after the first turn.");
            return session;
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
        /// The verified Wasp mirror stopped exactly where rulebook p.15's Upgrade a Card asks its
        /// question, with the hero the pause belongs to reported.
        ///
        /// <para>Milestone 8.8a. <see cref="CreateSessionPausedOnLevelUpUpgrade"/> reaches the same pause
        /// from the mechanics-showcase hand, whose faces are synthetic and print no words; this one is
        /// what a player actually sees, because it is the hand the configured scene deals. It needs the
        /// verified content for one reason the showcase hand cannot supply: the prompt draws each
        /// candidate's whole printed face, and only transcribed content can prove that the words on it
        /// are the words the card prints.</para>
        ///
        /// <para>The coin is earned the way every coin in this project is earned - p.13's Defeat - and
        /// milestone 8.9 is what made that reachable from a Wasp: Magnetic Dagger's printed Attack
        /// resolves against p.13's minion arm inside one command, so the round collects two coins and
        /// p.15's table charges one of them for the first level.</para>
        /// </summary>
        public static MatchSession CreateVerifiedMirrorPausedOnLevelUpUpgrade(out HeroId levellingHeroId)
        {
            var session = CreateMagneticDaggerSession(out var minionId);
            var red = HandCards(session, TeamSide.Red).Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger));
            var blue = HandCards(session, TeamSide.Blue).Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "Magnetic Dagger's printed Initiative 12 acts before Stop Projectiles' 3, so the " +
                "Attack is the first thing the round resolves.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetMinionIds, Has.Member(minionId));
            Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, minionId));
            Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(minionId)),
                Is.False, "The Attack has to defeat the minion, or the round collects no coin.");

            PlayOutTheRestOfTheRound(session);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null,
                "p.15 stops the end of round on the upgrade the purchased level forces.");
            levellingHeroId = session.Projection.PendingSystemTransitionDescription.HeroId;
            return session;
        }

        /// <summary>
        /// The verified Wasp mirror standing where rulebook p.16's Ultimate can reach: a Blue melee
        /// minion in its starting Battle Zone, the Red hero within three spaces of it, and the Blue
        /// hero beside it.
        ///
        /// <para>The caller supplies the configuration rather than this method building one, because
        /// the two fixtures that want this board differ in exactly one thing - how the Ultimate came
        /// to be beside the dashboard. Milestone 8.13 staged it there directly, which its own roadmap
        /// block allowed; milestone 8.14 levels a hero to it through p.15's purchases first. The board
        /// is the same either way, so it is built once.</para>
        /// </summary>
        public static MatchSession CreateVerifiedMirrorInRadiusOfAMinion(
            PlaceholderOneVsOneTestConfiguration configuration,
            out HeroId redHeroId,
            out HeroId blueHeroId,
            out MinionId targetMinionId)
        {
            var participants = configuration.Participants;
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(), participants, map,
                configuration.TieBreakerState);
            var target = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var playable = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable).ToArray();
            var redAt = playable.Select(space => space.Coordinate).First(coordinate =>
                !occupied.Contains(coordinate) &&
                HexDistanceQuery.Calculate(coordinate, target.Coordinate) is > 0 and <= 3);
            var blueAt = target.Coordinate.GetNeighbors().First(coordinate =>
                !occupied.Contains(coordinate) && !coordinate.Equals(redAt) &&
                playable.Any(space => space.Coordinate.Equals(coordinate)));
            var heroes = setup.Heroes.Select(hero =>
            {
                var coordinate = hero.HeroId.Equals(redHero) ? redAt : blueAt;
                var space = playable.Single(value => value.Coordinate.Equals(coordinate));
                return new QuickGameHeroSetupState(
                    hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                    hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id), coordinate);
            }).ToArray();
            var staged = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes,
                setup.Minions);
            var scenario = new InitialScenarioDescription(
                participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            redHeroId = redHero;
            blueHeroId = blueHero;
            targetMinionId = target.MinionId;
            return new MatchSession(new MatchSessionInitialState(scenario, staged, new[] { map }));
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
