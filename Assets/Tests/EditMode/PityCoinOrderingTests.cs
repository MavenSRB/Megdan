using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.6: rulebook p.8's "Collect a Pity Coin" step, and the record its question is asked
    /// of.
    ///
    /// The page states the rule and its consequence in two sentences: "All players who have NOT leveled
    /// up this round collect 1 coin. You do not get to level up this round, even if you now have enough
    /// coins, after getting this extra coin." The second is not enforced by a separate rule here - it
    /// follows from where the step sits, which is what the last case in this fixture proves rather than
    /// assumes.
    /// </summary>
    public sealed class PityCoinOrderingTests
    {
        // ----- the record p.8's question is asked of -----

        [Test]
        public void TheRecord_TracksEachHeroIndependentlyAndForgetsTheRoundThatEnded()
        {
            var participants = StagedBoardParticipants.Create();
            var red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var record = new RoundLevelingRecord(participants);

            Assert.That(record.HasLeveled(red), Is.False);
            Assert.That(record.HasLeveled(blue), Is.False);
            Assert.That(record.LeveledThisRound, Is.Empty);

            record.RecordLevelPurchased(red);
            Assert.That(record.HasLeveled(red), Is.True);
            Assert.That(record.HasLeveled(blue), Is.False,
                "Eligibility is independent per hero: one hero levelling says nothing about another.");

            // p.8 asks whether a hero levelled at all, not how often.
            record.RecordLevelPurchased(red);
            Assert.That(record.LeveledThisRound.Select(value => value.Value),
                Is.EqualTo(new[] { red.Value }));

            record.BeginRound();
            Assert.That(record.HasLeveled(red), Is.False,
                "The question is asked of \"this round\", so the next one starts with nobody having levelled.");
            Assert.That(record.LeveledThisRound, Is.Empty);

            Assert.That(() => record.HasLeveled(new HeroId("nobody")),
                Throws.InstanceOf<KeyNotFoundException>());
            Assert.That(() => record.RecordLevelPurchased(new HeroId("nobody")),
                Throws.InstanceOf<KeyNotFoundException>());
        }

        [Test]
        public void TheRecord_RoundTripsAndRefusesAPayloadThisMatchCouldNotHaveProduced()
        {
            var participants = StagedBoardParticipants.Create();
            var red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var record = new RoundLevelingRecord(participants);
            record.RecordLevelPurchased(red);

            var restored = RoundLevelingRecord.Restore(participants, record.CaptureRestoreState());
            Assert.That(restored.LeveledThisRound.Select(value => value.Value),
                Is.EqualTo(new[] { red.Value }));
            Assert.That(restored.HasLeveled(red), Is.True);

            Assert.That(
                () => RoundLevelingRecord.Restore(
                    participants,
                    new RoundLevelingRestoreState(new[] { new HeroId("not-a-participant") })),
                Throws.ArgumentException,
                "A hero this match does not have cannot have levelled in it.");
            Assert.That(
                () => RoundLevelingRecord.Restore(
                    participants, new RoundLevelingRestoreState(new[] { red, red })),
                Throws.ArgumentException,
                "One hero appears at most once, because the record is a fact rather than a count.");
        }

        [Test]
        public void APityCoin_IsGrantedWithoutADefeatAndKeepsItsBalanceTransition()
        {
            var participants = StagedBoardParticipants.Create();
            var red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var ledger = new HeroCoinLedger(participants);

            Assert.That(ledger.TryGrant(red, 1, CoinGrantKind.PityCoin, out var granted), Is.True);
            Assert.That(granted.RecipientHeroId, Is.EqualTo(red));
            Assert.That(granted.GrantKind, Is.EqualTo(CoinGrantKind.PityCoin));
            Assert.That(granted.BalanceBefore, Is.EqualTo(0));
            Assert.That(granted.Amount, Is.EqualTo(1));
            Assert.That(granted.BalanceAfter, Is.EqualTo(1));
            Assert.That(ledger.GetCoins(red), Is.EqualTo(1));
            Assert.That(ledger.AwardedOutcomeIds, Is.Empty,
                "Nobody was defeated, so nothing joins the record that stops a Defeat paying twice.");

            Assert.That(() => ledger.TryGrant(red, 0, CoinGrantKind.PityCoin, out _),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => ledger.TryGrant(new HeroId("nobody"), 1, CoinGrantKind.PityCoin, out _),
                Throws.InstanceOf<KeyNotFoundException>());
            Assert.That(
                () => new CoinGrantOperation(
                    red, participants.GetTeamForHero(red).Id, CoinGrantKind.None, 1, 0, 1),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "A grant names what put the coin there.");
        }

        // ----- p.8's stage, through the public command path -----

        [Test]
        public void EveryHeroThatDidNotLevel_CollectsExactlyOneCoinAtTheStage()
        {
            var session = CreateConfiguredSession();
            var pity = PlayOneRoundToPityCoins(session);

            var collected = pity.Events
                .Where(value => value.Kind == RoundResolutionEventKind.PityCoinCollected)
                .Select(value => value.PityCoinGrant)
                .ToArray();

            Assert.That(collected, Has.Length.EqualTo(2),
                "Neither hero could afford a level, so p.8 pays both.");
            Assert.That(collected.All(grant => grant.Amount == 1), Is.True, "p.8 pays \"1 coin\".");
            Assert.That(collected.All(grant => grant.BalanceBefore == 0), Is.True);
            Assert.That(collected.All(grant => grant.BalanceAfter == 1), Is.True);
            Assert.That(collected.All(grant => grant.GrantKind == CoinGrantKind.PityCoin), Is.True);
            Assert.That(
                collected.Select(grant => grant.RecipientHeroId.Value),
                Is.EqualTo(session.Projection.Setup.Heroes
                    .Select(hero => hero.HeroId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "Every participating hero is asked, in a stable order.");

            Assert.That(
                pity.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.EndOfRoundStageResolved &&
                    value.EndOfRoundStage == EndOfRoundStage.CollectPityCoins &&
                    value.EndOfRoundOutcomeKind == EndOfRoundStageOutcomeKind.Completed),
                Is.True);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null,
                "The stage asks nobody anything, so it can never pause.");
        }

        [Test]
        public void AHeroThatBoughtALevel_CollectsNoPityCoinWhileTheOtherStillDoes()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());

            // Red bought a level with the coins its Attack collected and is paused on p.15's upgrade.
            var pending = session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null);
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(pending.Invocations[0]));

            var pity = Accept(session, DirectorCommand.AdvanceEndOfRound());
            var collected = pity.Events
                .Where(value => value.Kind == RoundResolutionEventKind.PityCoinCollected)
                .Select(value => value.PityCoinGrant)
                .ToArray();

            Assert.That(collected, Has.Length.EqualTo(1),
                "p.8 pays the heroes that did not level up, and Red did.");
            Assert.That(collected[0].RecipientHeroId, Is.Not.EqualTo(redHeroId));
            Assert.That(collected[0].BalanceAfter, Is.EqualTo(1));
            Assert.That(
                pity.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.PityCoinCollected &&
                    value.PityCoinGrant.RecipientHeroId.Equals(redHeroId)),
                Is.False);
        }

        [Test]
        public void ThePityCoinStage_SurvivesSnapshotRestoreReplayAndUndo()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0]));

            // Saved between Level Up and Collect a Pity Coin: the record of who levelled has to survive,
            // or the restored match pays Red a coin p.8 denies it.
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.CollectPityCoins));
            var betweenStages = session.Projection.AuthoritativeSnapshot;

            var restored = CreateSessionWithAnAdjacentEnemyMinion(out _, out _);
            Assert.That(restored.RestoreSnapshot(betweenStages).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(betweenStages));

            var restoredPity = restored.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(restoredPity.Accepted, Is.True, restoredPity.Detail);
            Assert.That(
                restoredPity.Events.Count(value =>
                    value.Kind == RoundResolutionEventKind.PityCoinCollected),
                Is.EqualTo(1),
                "A restored match remembers that Red levelled, so only Blue collects.");
            Assert.That(
                restoredPity.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.PityCoinCollected &&
                    value.PityCoinGrant.RecipientHeroId.Equals(redHeroId)),
                Is.False);

            // Replay of the same accepted prefix rebuilds the same match exactly.
            var replayed = CreateSessionWithAnAdjacentEnemyMinion(out _, out _);
            foreach (var command in session.Projection.AcceptedCommands)
            {
                var result = replayed.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(betweenStages));

            // Undo takes the coins back with the stage that granted them.
            var afterPity = Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(
                afterPity.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.PityCoinCollected), Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(betweenStages),
                "The coin balances are on the wire, so an identical payload is the statement that the "
                + "undone grant is gone.");
        }

        [Test]
        public void APityCoinThatReachesALevelThreshold_CannotBeSpentUntilALaterRound()
        {
            // p.8: "You do not get to level up this round, even if you now have enough coins, after
            // getting this extra coin." One coin is exactly p.15's price for the first level, so this
            // hero can afford a level the moment the coin lands - and the round is already past its
            // Level Up step.
            var session = CreateConfiguredSession();
            var pity = PlayOneRoundToPityCoins(session);
            var hero = pity.Events
                .First(value => value.Kind == RoundResolutionEventKind.PityCoinCollected)
                .PityCoinGrant.RecipientHeroId;

            Assert.That(HeroLevelSchedule.LevelUpCost(new HeroLevel(1)), Is.EqualTo(1),
                "The coin just granted is exactly the price of the level this hero has not bought.");
            Assert.That(
                pity.Events.Any(value => value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed),
                Is.False,
                "Nothing levels in the stage that pays the coin.");

            FinishEndOfRound(session);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));

            // The level is bought at the next round's Level Up step, not at this one's.
            PlayOneOrdinaryRound(session);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            var levelUp = Accept(session, DirectorCommand.AdvanceEndOfRound());
            var purchase = levelUp.Events
                .Where(value => value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed &&
                    value.HeroLevelUpOutcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased)
                .Select(value => value.HeroLevelUpOutcome)
                .Single(value => value.HeroId.Equals(hero));

            Assert.That(purchase.Level.Value, Is.EqualTo(2));
            Assert.That(purchase.Spend.BalanceBefore, Is.EqualTo(1),
                "The coin waited a whole round to be spent, which is p.8's own consequence.");
            Assert.That(purchase.Spend.BalanceAfter, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static MatchCommandResult Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        /// <summary>Plays an ordinary round and advances to the Pity Coin stage, running it.</summary>
        private static MatchCommandResult PlayOneRoundToPityCoins(MatchSession session)
        {
            PlayOneOrdinaryRound(session);
            AdvanceToStage(session, EndOfRoundStage.CollectPityCoins);
            return Accept(session, DirectorCommand.AdvanceEndOfRound());
        }

        private static void AdvanceToStage(MatchSession session, EndOfRoundStage stage)
        {
            while (session.Projection.CurrentEndOfRoundStage != stage)
            {
                Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                    "The round left the end of round before reaching the stage under test.");
                if (session.Projection.PendingMinionBattleChoice != null)
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

        private static void FinishEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }
        }

        private static void PlayOneOrdinaryRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var card = session.Projection.Cards.FirstOrDefault(candidate =>
                        session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                        candidate.State.Location == CardLocation.Hand);
                    if (card == null)
                    {
                        var hero = session.Projection.Setup.Heroes
                            .Single(value => value.TeamSide == side).HeroId;
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
                    // Milestone 8.35g. A finished resolution waits for its own player to hand the
                    // turn on, so this loop presses the release exactly as that player does.
                    if (session.Projection.PendingTurnRelease != null)
                    {
                        Accept(session, DirectorCommand.ReleaseTurn());
                        continue;
                    }

                    Accept(session, DirectorCommand.PerformHold(
                        session.Projection.SelectedActor.CardInstanceId));
                }
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
        }

        /// <summary>The ordinary configured match, in which nobody collects a coin all round.</summary>
        private static MatchSession CreateConfiguredSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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

        /// <summary>
        /// The milestone 7.5 fixture: the Red hero defeats one adjacent enemy melee minion, so it
        /// reaches the Level Up step holding p.13's two coins and buys a level there.
        /// </summary>
        private static MatchSession CreateSessionWithACoinEarningRound(out HeroId redHeroId)
        {
            var session = CreateSessionWithAnAdjacentEnemyMinion(out redHeroId, out var target);
            var redCard = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blueCard = session.Projection.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            Accept(session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));
            Accept(session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id));

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var choice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));
            Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)),
                Is.False, "The Attack has to defeat the minion, or the round collects no coin.");

            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, so this loop presses the release exactly as that player does.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            PlayOneOrdinaryRound(session);
            return session;
        }

        /// <summary>
        /// The official-map configured match with one Blue melee minion beside the Red hero's spawn,
        /// relocated out of the Battle Zone so defeating it triggers no immediate Lane Push.
        /// </summary>
        private static MatchSession CreateSessionWithAnAdjacentEnemyMinion(
            out HeroId redHeroId, out MinionId target)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
            var movedId = target;
            var destination = beside;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(movedId)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId,
                        minion.TeamId,
                        minion.TeamSide,
                        minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(
                            map.Id,
                            map.Spaces.Single(space =>
                                space.Kind == BoardMapSpaceKind.Playable &&
                                space.Coordinate.Equals(destination)).Id),
                        destination)
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
    }
}
