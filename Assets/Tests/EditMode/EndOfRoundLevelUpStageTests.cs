using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Rulebook p.8's "Level Up" stage seen through a live configured match: a hero that collected
    /// coins this round buys what p.15 makes it buy, stops on the one question that page asks, and
    /// carries the whole transaction across a snapshot, a replay and an Undo.
    ///
    /// The stage has been ordered in <c>EndOfRoundStage</c> since milestone 6.1 and unavailable until
    /// milestone 7.5. What the rules themselves decide - the table's prices, the option space, the item
    /// and the Ultimate - is <see cref="HeroLevelUpTests"/>'s; this fixture is about the stage, the
    /// command path, and the durability boundary Phase 7's rule requires in the same milestone.
    /// </summary>
    public sealed class EndOfRoundLevelUpStageTests
    {
        [Test]
        public void AHeroThatCollectedCoins_BuysWhatItCanAffordAtTheStageAndStopsOnTheUpgradeChoice()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);

            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            var levelUp = Accept(session, DirectorCommand.AdvanceEndOfRound());

            var purchase = levelUp.Events.Single(value =>
                value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed &&
                value.HeroLevelUpOutcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased)
                .HeroLevelUpOutcome;
            Assert.That(purchase.HeroId, Is.EqualTo(redHeroId),
                "p.15: \"Leveling up is not optional!\" - and nobody was asked.");
            Assert.That(purchase.Level.Value, Is.EqualTo(2));
            Assert.That(purchase.Spend.BalanceBefore, Is.EqualTo(2),
                "p.13 prices a melee minion at 2 coins, which is what this round collected.");
            Assert.That(purchase.Spend.Amount, Is.EqualTo(1), "p.15's table prices the first level at 1.");
            Assert.That(purchase.Spend.BalanceAfter, Is.EqualTo(1),
                "1 coin cannot buy the second level, which p.15 prices at 2.");
            Assert.That(
                levelUp.Events.Count(value =>
                    value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed &&
                    value.HeroLevelUpOutcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased),
                Is.EqualTo(1),
                "The Blue hero collected no coin, so it bought no level.");

            var pending = session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.DefinitionId, Is.EqualTo(HeroLevelUpSystemTransition.DefinitionId));
            Assert.That(HeroLevelUpItem.Read(pending.ItemId).HeroId, Is.EqualTo(redHeroId));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.LevelUpAndUpgrade),
                "A stage that stopped on a chooser stays where it is until the answer arrives.");

            // p.15's step is the only thing leveling asks anybody, so a second advance is refused
            // rather than beginning a second leveling.
            var second = session.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(second.Accepted, Is.False);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.SameAs(pending));
        }

        [Test]
        public void AnsweringTheUpgrade_FinishesTheStageWithTheItemAndLetsTheRoundClose()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());

            var pending = session.Projection.PendingSystemTransitionStep;
            var chosen = HeroLevelUpOption.Read(pending.Options[0]);
            var answered = Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                pending.Invocations[0]));

            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
            Assert.That(LocationOf(session, chosen.UpgradedCardInstanceId),
                Is.EqualTo(CardLocation.Deck));
            Assert.That(LocationOf(session, chosen.TakenCardInstanceId), Is.EqualTo(CardLocation.Hand));

            var item = answered.Events.Single(value =>
                value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed &&
                value.HeroLevelUpOutcome.Kind == HeroLevelUpOutcomeKind.ItemGained)
                .HeroLevelUpOutcome.Item;
            Assert.That(item.OwnerId, Is.EqualTo(redHeroId));
            Assert.That(LocationOf(session, item.SourceCardInstanceId),
                Is.EqualTo(CardLocation.ItemSlot),
                "p.16 tucks the card set aside during the Upgrade card step under a matching slot.");
            Assert.That(
                session.Projection.Cards.Count(card => card.State.Location == CardLocation.ItemSlot),
                Is.EqualTo(1),
                "One purchased level sets one card aside, so it becomes exactly one item.");
            Assert.That(
                session.Projection.Cards.Any(card => card.State.Location == CardLocation.SetAside),
                Is.False,
                "p.16's step takes every card set aside during the upgrades, leaving none waiting.");

            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.EndOfRoundStageResolved &&
                    value.EndOfRoundStage == EndOfRoundStage.LevelUpAndUpgrade &&
                    value.EndOfRoundOutcomeKind == EndOfRoundStageOutcomeKind.Completed),
                Is.True,
                "The answer that finishes the leveling is what completes the stage.");

            FinishEndOfRound(session);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));

            // The upgraded card is real: the next round commits it.
            var committed = session.Execute(DirectorCommand.CommitCard(
                redHeroId, chosen.TakenCardInstanceId));
            Assert.That(committed.Accepted, Is.True, committed.Detail);
        }

        [Test]
        public void ThePausedLeveling_SurvivesSnapshotRestoreReplayAndUndoWithoutRepeatingAPurchase()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            var beforeAdvance = session.Projection.AuthoritativeSnapshot;
            Accept(session, DirectorCommand.AdvanceEndOfRound());

            var paused = session.Projection.AuthoritativeSnapshot;
            var pending = session.Projection.PendingSystemTransitionStep;

            // Restore: the paused step comes back and is re-derived from the restored cards rather than
            // trusted, and the payload is byte-identical either side of the round trip.
            var restored = CreateRestoreTarget();
            Assert.That(restored.RestoreSnapshot(paused).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(paused),
                "The Level and the coin balance are on the wire, so an identical payload is the " +
                "statement that the cost was spent once and the restore did not spend it again.");
            Assert.That(restored.Projection.PendingSystemTransitionStep.ItemId,
                Is.EqualTo(pending.ItemId));
            Assert.That(
                restored.Projection.PendingSystemTransitionStep.Options.Select(value => value.Value),
                Is.EqualTo(pending.Options.Select(value => value.Value)));

            // Restore-and-continue: the restored match answers the same step through the same command.
            var continued = restored.Execute(DirectorCommand.AnswerSystemTransitionStep(
                restored.Projection.PendingSystemTransitionStep.Invocations[0]));
            Assert.That(continued.Accepted, Is.True, continued.Detail);
            Assert.That(
                restored.Projection.Cards.Count(card => card.State.Location == CardLocation.ItemSlot),
                Is.EqualTo(1));

            // Replay: the same accepted command prefix rebuilds the same paused match exactly.
            var replayed = CreateRestoreTarget();
            foreach (var command in session.Projection.AcceptedCommands)
            {
                var result = replayed.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(paused));

            // Undo: truncating the advance takes the purchase, its cost and its continuation with it.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeAdvance),
                "An undone purchase is not a purchase: the payload holds the Level and the coins the " +
                "round had before the stage ran.");
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
            Assert.That(
                session.Projection.Cards.Any(card =>
                    card.State.Location == CardLocation.ItemSlot ||
                    card.State.Location == CardLocation.SetAside),
                Is.False);

            // And re-executing the truncated command produces the same match again rather than a second
            // purchase on top of the first.
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(paused));
        }

        [Test]
        public void AStaleOrIllegalAnswer_IsRefusedAtomicallyAndTheStageStaysWhereItIs()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());

            var before = session.Projection;
            var pending = before.PendingSystemTransitionStep;
            var legal = pending.Invocations[0];

            void AssertRefused(SystemTransitionChoiceCommand invocation)
            {
                var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(invocation));
                Assert.That(result.Accepted, Is.False);
                Assert.That(result.Projection.Version, Is.EqualTo(before.Version));
                Assert.That(result.Projection.AuthoritativeSnapshot,
                    Is.EqualTo(before.AuthoritativeSnapshot));
                Assert.That(result.Projection.AcceptedCommands, Is.EqualTo(before.AcceptedCommands));
            }

            AssertRefused(new SystemTransitionChoiceCommand(
                legal.InstanceId,
                legal.StepId,
                new HeroLevelUpItem(redHeroId, new HeroLevel(3), HeroLevelUpItemKind.Upgrade).ToItemId(),
                legal.ChoosingSide,
                legal.OptionId));
            AssertRefused(new SystemTransitionChoiceCommand(
                legal.InstanceId, legal.StepId, legal.ItemId, TeamSide.Blue, legal.OptionId));
            AssertRefused(new SystemTransitionChoiceCommand(
                legal.InstanceId, legal.StepId, legal.ItemId, legal.ChoosingSide,
                new SystemTransitionOptionId("red-lancer-card->red-bulwark-t2a-card")));

            // The step the refusals left alone is still answerable.
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(legal));

            // And answering it twice is refused for the same reason: nothing is paused any more.
            var again = session.Execute(DirectorCommand.AnswerSystemTransitionStep(legal));
            Assert.That(again.Accepted, Is.False);
        }

        [Test]
        public void ARoundNobodyCanLevelIn_CrossesTheStageWithoutAskingAnythingAndChangesNoLevel()
        {
            var session = CreateConfiguredSession();
            PlayOneOrdinaryRound(session);
            AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);

            var levelUp = Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(
                levelUp.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed), Is.False);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
            Assert.That(
                session.Projection.Cards.Any(card =>
                    card.State.Location == CardLocation.ItemSlot ||
                    card.State.Location == CardLocation.SetAside ||
                    card.State.Location == CardLocation.UltimateArea),
                Is.False,
                "No coins means no purchase, and no purchase moves a card.");
            Assert.That(
                levelUp.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.EndOfRoundStageResolved &&
                    value.EndOfRoundStage == EndOfRoundStage.LevelUpAndUpgrade &&
                    value.EndOfRoundOutcomeKind == EndOfRoundStageOutcomeKind.Completed),
                Is.True,
                "p.8 runs the stage every round; nobody having coins is an outcome rather than a skip.");

            FinishEndOfRound(session);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static CardLocation LocationOf(MatchSession session, CardInstanceId cardInstanceId) =>
            session.Projection.Cards.Single(card => card.Id.Equals(cardInstanceId)).State.Location;

        private static MatchCommandResult Accept(MatchSession session, DirectorCommand command)
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
        private static void AdvanceToStage(MatchSession session, EndOfRoundStage stage)
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

        private static void FinishEndOfRound(MatchSession session)
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
        private static MatchSession CreateSessionWithACoinEarningRound(out HeroId redHeroId)
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
        private static MatchSession CreateSessionWithAnAdjacentEnemyMinion(
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
        private static MatchSession CreateConfiguredSession()
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

        private static MatchSession CreateRestoreTarget() =>
            CreateSessionWithAnAdjacentEnemyMinion(out _, out _);

        private static void PlayOneOrdinaryRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                CommitAndHoldOneTurn(session);
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
        }

        private static void PlayOutTheRestOfTheRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            PlayOneOrdinaryRound(session);
        }

        private static void CommitAndHoldOneTurn(MatchSession session)
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
    }
}
