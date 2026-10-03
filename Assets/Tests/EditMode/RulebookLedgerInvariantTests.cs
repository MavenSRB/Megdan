using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Independent second-printing rulebook readings, transcribed in
    /// Docs/AI/Testing/RULEBOOK_LEDGER_INVARIANTS.md. Expected numbers are literal readings of the
    /// printed tables, never the outputs of another production schedule.
    /// </summary>
    [TestFixture]
    public sealed class RulebookLedgerInvariantTests
    {
        [TestCase("2v2-quick", 4, 2, 3, 4)]
        [TestCase("2v2-long", 4, 2, 5, 6)]
        [TestCase("3v3-quick", 6, 3, 3, 5)]
        [TestCase("3v3-long", 6, 3, 5, 8)]
        public void SetupSteps2_4_5_EqualTeamsWavesAndLivesMatchThePrintedRows(
            string formatId, int players, int perTeam, int waves, int lives)
        {
            var format = MatchFormat.FindById(formatId);
            Assert.That(format, Is.Not.Null);
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(format, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficial(format), configuration.Participants,
                map, configuration.TieBreakerState);

            Assert.That(setup.Heroes, Has.Count.EqualTo(players), "p.5 step 2");
            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Red),
                Is.EqualTo(perTeam), "p.5 step 2");
            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Blue),
                Is.EqualTo(perTeam), "p.5 step 2");
            Assert.That(setup.WaveCount, Is.EqualTo(waves), "p.5 step 4");
            Assert.That(setup.Teams, Has.Count.EqualTo(2));
            Assert.That(setup.Teams.All(team => team.LifeCount == lives), Is.True,
                "p.5 step 5 counts Life per team, not in total");
        }

        [Test]
        public void SetupSteps6_8_11_StartAtLevelOneWithNoCoinGrantAndThePrintedFiveCardHand()
        {
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                MatchFormat.ThreeVersusThreeQuick, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficial(MatchFormat.ThreeVersusThreeQuick),
                configuration.Participants, map, configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants, configuration.CardDefinitions, configuration.CardInstances,
                configuration.TieBreakerState, configuration.SetupSeed, map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));

            foreach (var hero in setup.Heroes)
            {
                var progress = session.Projection.HeroProgression.Single(row => row.HeroId.Equals(hero.HeroId));
                Assert.That(progress.Level, Is.EqualTo(1), "p.5 step 8");
                Assert.That(progress.Coins, Is.EqualTo(0), "p.5 step 6 sets out the supply, not a grant");

                var held = session.Projection.Cards.Where(card =>
                    card.OwnerId.Equals(hero.HeroId) && card.State.Location == CardLocation.Hand).ToArray();
                Assert.That(held, Has.Length.EqualTo(5), "p.6 step 11");
                var definitions = held.Select(card => session.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference))).ToArray();
                Assert.That(definitions.Count(definition => definition.Progression.IsNonBasic &&
                    definition.Progression.Tier == CardTier.I), Is.EqualTo(3));
                Assert.That(definitions.Count(definition => !definition.Progression.IsNonBasic),
                    Is.EqualTo(2), "the basic pair has no Tier and cannot lower the defeat Tier");
            }
        }

        [Test]
        public void PushStep1_OnlyTheLastWaveFlipExhaustsTheCounterPool()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                StagedBoardParticipants.Create(), map, new TieBreakerState(TeamSide.Red));
            var board = new AdjacentMoveAuthority(map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>(), setup.Minions);
            var beforeLast = new LanePushTransitionAuthority(board, 2);
            Assert.That(beforeLast.TryBeginPush(TeamSide.Red, TeamSide.Blue,
                new TieBreakerState(TeamSide.Red)), Is.EqualTo(LanePushPrefixRejectionReason.None));
            beforeLast.ResolveWaveStage();
            Assert.That(beforeLast.RemainingWaveCounters, Is.EqualTo(1));
            Assert.That(beforeLast.HasEnded, Is.False);

            var last = new LanePushTransitionAuthority(board, 1);
            Assert.That(last.TryBeginPush(TeamSide.Blue, TeamSide.Red,
                new TieBreakerState(TeamSide.Red)), Is.EqualTo(LanePushPrefixRejectionReason.None));
            last.ResolveWaveStage();
            Assert.That(last.RemainingWaveCounters, Is.Zero);
            Assert.That(last.GameEndingCause, Is.EqualTo(LanePushGameEndingCause.FinalWaveFlipped));
            Assert.That(last.WinningSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void PushStep2_EnteringTheOpposingThroneWinsWithWavesStillAvailable()
        {
            var board = CreateOneLaneBoard();
            Assert.That(board.CommitValidatedBattleZoneAdvance(new ZoneId("blue-approach")), Is.True);
            var push = new LanePushTransitionAuthority(board, 5);
            Assert.That(push.TryBeginPush(TeamSide.Red, TeamSide.Blue,
                new TieBreakerState(TeamSide.Red)), Is.EqualTo(LanePushPrefixRejectionReason.None));
            push.ResolveWaveStage();
            push.ResolveZoneStage();

            Assert.That(push.RemainingWaveCounters, Is.EqualTo(4));
            Assert.That(push.GameEndingCause, Is.EqualTo(LanePushGameEndingCause.ThroneCaptured));
            Assert.That(push.WinningSide, Is.EqualTo(TeamSide.Red));
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(new ZoneId("blue-throne")));
        }

        [Test]
        public void PushStep3_RemovesOldZoneMinionsAndRespawnsInNewZone()
        {
            var board = CreateOneLaneBoard();
            var oldZone = board.CurrentBattleZoneId;
            Assert.That(oldZone.HasValue, Is.True);
            Assert.That(board.FindMinionsInZone(oldZone.Value).Count(), Is.EqualTo(12));
            var push = new LanePushTransitionAuthority(board, 5);
            var transitions = new SystemTransitionAuthority(
                MatchSystemTransitions.CreateCatalog(), MatchSystemTransitions.CreateRules(push, null));
            var result = new LanePushConsumption(push, transitions).TryConsume(
                new LanePushRequestId("rulebook-step-three"), TeamSide.Red, TeamSide.Blue,
                new TieBreakerState(TeamSide.Red));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsPaused, Is.False);
            Assert.That(result.Finished, Is.Not.Null);
            Assert.That(result.Finished.Outcomes.Select(outcome => outcome.Stage), Is.EqualTo(new[]
            {
                LanePushTransitionStage.FlipWaveCounter,
                LanePushTransitionStage.AdvanceBattleZone,
                LanePushTransitionStage.RemoveAndRespawnMinions
            }), "this board-only transition reports all three printed push steps, with no Defeat/reward stage");
            Assert.That(board.FindMinionsInZone(oldZone.Value), Is.Empty);
            Assert.That(board.CurrentBattleZoneId.HasValue, Is.True);
            Assert.That(board.FindMinionsInZone(board.CurrentBattleZoneId.Value), Is.Not.Empty);
        }

        [Test]
        public void PushStep3_LiveRespawnDoesNotAwardDefeatCoins()
        {
            // Leave a single Blue minion in the starting Battle Zone so the ordinary end-of-round
            // Minion Battle requests a Red push. Snapshot only after that battle has resolved:
            // its removal is a different printed step from the subsequent push/respawn.
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants, map, configuration.TieBreakerState);
            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));
            var pushSetup = new QuickGameSetupState(
                setup.SelectedMap, 5, setup.TieBreakerState, setup.Teams, setup.Heroes,
                setup.Minions.Where(minion => minion.TeamSide != TeamSide.Blue ||
                    minion.MinionId.Equals(survivor.MinionId)).ToArray());
            var scenario = new InitialScenarioDescription(
                configuration.Participants, configuration.CardDefinitions, configuration.CardInstances,
                configuration.TieBreakerState, map.Reference,
                pushSetup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(new MatchSessionInitialState(scenario, pushSetup, new[] { map }));

            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments ||
                session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var card = session.Projection.Cards.First(candidate =>
                        session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                        candidate.State.Location == CardLocation.Hand);
                    AcceptLive(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
                }

                var tie = session.Projection.PendingInitiativeChoice;
                if (tie != null)
                {
                    AcceptLive(session, DirectorCommand.ChooseTiedActor(
                        tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                }

                var turn = session.Projection.TurnIndex;
                while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                    session.Projection.TurnIndex == turn)
                {
                    if (session.Projection.PendingTurnRelease != null)
                    {
                        AcceptLive(session, DirectorCommand.ReleaseTurn());
                    }
                    else
                    {
                        AcceptLive(session, DirectorCommand.PerformHold(
                            session.Projection.SelectedActor.CardInstanceId));
                    }
                }
            }

            while (session.Projection.CurrentEndOfRoundStage != EndOfRoundStage.ResolveRequestedLanePush)
            {
                Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                if (session.Projection.PendingMinionBattleChoice != null)
                {
                    while (session.Projection.PendingMinionBattleChoice.SelectInvocations.Count > 0)
                    {
                        AcceptLive(session, DirectorCommand.SelectMinionBattleRemoval(
                            session.Projection.PendingMinionBattleChoice.SelectInvocations[0]));
                    }

                    AcceptLive(session, DirectorCommand.ConfirmMinionBattleRemovals(
                        session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                }
                else
                {
                    AcceptLive(session, DirectorCommand.AdvanceEndOfRound());
                }
            }

            var coins = session.Projection.HeroProgression
                .Select(row => (row.HeroId, row.Coins)).ToArray();
            var logCount = session.Projection.MatchLog.Count;
            var push = AcceptLive(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(push.Events.Any(row => row.Kind == RoundResolutionEventKind.LanePushStageResolved),
                Is.True, "the command must have performed the requested push");
            Assert.That(session.Projection.HeroProgression
                .Select(row => (row.HeroId, row.Coins)).ToArray(), Is.EqualTo(coins),
                "p.8 step 3 removal/respawn is not p.13 Defeat and awards no coins");
            Assert.That(session.Projection.MatchLog.Skip(logCount).Any(row =>
                row.Kind == MatchLogEntryKind.MinionDefeated ||
                row.Kind == MatchLogEntryKind.HeroDefeated ||
                row.Kind == MatchLogEntryKind.CoinAwarded), Is.False,
                "no Defeat or its reward may be published by the push/respawn command");
        }

        private static MatchCommandResult AcceptLive(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        [TestCase(1, CardTier.I, 1, 1)]
        [TestCase(2, CardTier.I, 1, 1)]
        [TestCase(3, CardTier.I, 1, 1)]
        [TestCase(4, CardTier.II, 2, 2)]
        [TestCase(5, CardTier.II, 2, 2)]
        [TestCase(6, CardTier.II, 2, 2)]
        [TestCase(7, CardTier.III, 3, 3)]
        [TestCase(8, CardTier.III, 3, 3)]
        public void HeroDefeatTable_AttackerAssistAndLifeCostUseSeparatePrintedColumns(
            int level, CardTier tier, int assist, int life)
        {
            Assert.That(HeroDefeatRewardSchedule.DefeatCoins(new HeroLevel(level)),
                Is.EqualTo(level), "p.13 attacker gets the defeated hero's level");
            Assert.That(HeroDefeatRewardSchedule.AssistCoins(tier), Is.EqualTo(assist),
                "p.13 assist row, including the 3/4 and 6/7 boundaries");
            Assert.That(HeroDefeatRewardSchedule.LifeCounters(tier), Is.EqualTo(life),
                "p.13 Life row is independently asserted");
        }

        [Test]
        public void HeroDefeatTable_CoinsReachEachOtherFriendlyHeroAndLifeComesFromVictimsTeam()
        {
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                MatchFormat.ThreeVersusThreeQuick, TeamSide.Red);
            var participants = configuration.Participants;
            var red = participants.Teams.Single(team => team.Side == TeamSide.Red).Id;
            var blue = participants.Teams.Single(team => team.Side == TeamSide.Blue).Id;
            var attacker = participants.Heroes.First(hero => participants.GetTeamForHero(hero.Id).Id.Equals(red)).Id;
            var ledger = new HeroCoinLedger(participants);
            var lives = new TeamLifeState(participants, 5);
            var outcome = new DefeatOutcome(new DefeatOutcomeId("rulebook-level-four"),
                new DefeatTargetId("enemy-hero"), blue, attacker,
                HeroDefeatRewardSchedule.DefeatCoins(new HeroLevel(4)),
                HeroDefeatRewardSchedule.AssistCoins(CardTier.II));

            var reward = ledger.Award(outcome);
            var spend = lives.TrySpend(blue, HeroDefeatRewardSchedule.LifeCounters(CardTier.II));
            Assert.That(reward.Accepted, Is.True);
            Assert.That(reward.Operations.Count, Is.EqualTo(3));
            Assert.That(ledger.GetCoins(attacker), Is.EqualTo(4));
            foreach (var hero in participants.Heroes.Where(hero =>
                participants.GetTeamForHero(hero.Id).Id.Equals(red) && !hero.Id.Equals(attacker)))
            {
                Assert.That(ledger.GetCoins(hero.Id), Is.EqualTo(2));
            }
            Assert.That(participants.Heroes.Where(hero =>
                participants.GetTeamForHero(hero.Id).Id.Equals(blue))
                .All(hero => ledger.GetCoins(hero.Id) == 0), Is.True);
            Assert.That(spend.Accepted, Is.True);
            Assert.That(lives.GetCounters(blue), Is.EqualTo(3));
            Assert.That(lives.GetCounters(red), Is.EqualTo(5));
        }

        [Test]
        public void HeroDefeatAtLastLife_SpendsAvailableCountersAndEndsTheTeam()
        {
            var participants = MatchConfiguration.CreateOfficialForInternalTest(
                MatchFormat.TwoVersusTwoQuick, TeamSide.Red).Participants;
            var blue = participants.Teams.Single(team => team.Side == TeamSide.Blue).Id;
            var lives = new TeamLifeState(participants, 4);
            Assert.That(lives.TrySpend(blue, 3).Accepted, Is.True);
            var final = lives.TrySpend(blue, 2);
            Assert.That(final.Accepted, Is.True);
            Assert.That(final.Spend.Requested, Is.EqualTo(2));
            Assert.That(final.Spend.Spent, Is.EqualTo(1));
            Assert.That(final.Spend.SpentLastCounter, Is.True, "p.13 last counter ends the game");
            Assert.That(lives.GetCounters(blue), Is.Zero);
        }

        [Test]
        public void AttackOrderStep6_SuccessfulDefenseLeavesBothEconomiesUntouched()
        {
            var board = SabinaBoard.Create(
                heroPlacements: new System.Collections.Generic.Dictionary<string, HexCoordinate>
                {
                    ["primitive-hero"] = StagedHeroBoard.FirstAdjacent
                });
            var session = board.Session;
            var beforeLives = session.Projection.TeamLifeCounters.ToArray();
            var beforeAttackerCoins = board.Coins(board.Hero);
            var beforeDefenderCoins = board.Coins(board.Enemy);

            board.PlayAttackAt(SabinaCardContent.PointBlankShot, board.Enemy);

            Assert.That(board.IsOnBoard(board.Enemy), Is.True,
                "p.14: defense equal to or above Attack prevents the Defeat");
            Assert.That(session.Projection.TeamLifeCounters, Is.EqualTo(beforeLives));
            Assert.That(board.Coins(board.Hero), Is.EqualTo(beforeAttackerCoins));
            Assert.That(board.Coins(board.Enemy), Is.EqualTo(beforeDefenderCoins));
        }

        [Test]
        public void AttackOrderStep6_EqualDefenseSucceedsAndOneBelowFails()
        {
            Assert.That(AttackOutcomeComparison.Create(5, 5, DefenseBlockResult.None).Outcome,
                Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(AttackOutcomeComparison.Create(5, 4, DefenseBlockResult.None).Outcome,
                Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
        }

        [Test]
        public void BasicCardsAndReturnedLowerCards_DoNotReplaceTheLowestActiveNonBasicTier()
        {
            var hero = new HeroId("tier-probe");
            var basic = SwiftCardContent.DealtDefinitions.First(definition =>
                !definition.Progression.IsNonBasic);
            var low = SwiftCardContent.DealtDefinitions.First(definition =>
                definition.Progression.IsNonBasic && definition.Progression.Tier == CardTier.I);
            var high = SwiftCardContent.DealtDefinitions.First(definition =>
                definition.Progression.IsNonBasic && definition.Progression.Tier == CardTier.II);
            var cards = new[]
            {
                CardInstance.CreateInHand(new CardInstanceId("held-basic"), basic.Reference, hero),
                CardInstance.CreateInDeck(new CardInstanceId("returned-one"), low.Reference, hero),
                CardInstance.CreateInHand(new CardInstanceId("held-two"), high.Reference, hero)
            };
            Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(
                hero, SwiftCardContent.DealtDefinitions, cards, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(CardTier.II));
            Assert.That(HeroDefeatCostQuery.Worth(hero, SwiftCardContent.DealtDefinitions, cards),
                Is.EqualTo(2), "p.13 table and p.15 returned-card/basic exclusions");
        }

        [TestCase(StandardMinionType.Melee, 2)]
        [TestCase(StandardMinionType.Ranged, 2)]
        [TestCase(StandardMinionType.Heavy, 4)]
        public void MinionDefeat_BountyMatchesPageThirteen(StandardMinionType type, int coins)
        {
            Assert.That(StandardMinionProfileCatalog.Rulebook.Get(type).DefeatCoins,
                Is.EqualTo(coins));
        }

        [TestCase(1, 1, CardTier.None)]
        [TestCase(2, 2, CardTier.II)]
        [TestCase(3, 3, CardTier.II)]
        [TestCase(4, 4, CardTier.II)]
        [TestCase(5, 5, CardTier.III)]
        [TestCase(6, 6, CardTier.III)]
        [TestCase(7, 7, CardTier.III)]
        public void LevelTable_ConsecutivePurchaseCostAndMaxTierArePrintedValues(
            int current, int cost, CardTier maxTierAfterPriorPurchases)
        {
            Assert.That(HeroLevelSchedule.LevelUpCost(new HeroLevel(current)), Is.EqualTo(cost));
            Assert.That(HeroLevelSchedule.NextLevel(new HeroLevel(current)).Value,
                Is.EqualTo(current + 1));
            Assert.That(HeroLevelSchedule.MaxCardTier(new HeroLevel(current)),
                Is.EqualTo(maxTierAfterPriorPurchases));
        }

        [Test]
        public void MandatoryLevelUp_SpendsThreeCoinsOnTwoConsecutivePurchasesAndLowestTierChoices()
        {
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var hero = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var levels = new HeroProgressionState(configuration.Participants);
            var items = new HeroItemState(configuration.Participants);
            var coins = new HeroCoinLedger(configuration.Participants);
            Assert.That(coins.TryGrant(hero, 3, CoinGrantKind.CardEffect, out _), Is.True);
            var rules = new HeroLevelUpStepRules(configuration.Participants,
                configuration.CardDefinitions, configuration.CardInstances);
            rules.Bind(levels, items, coins);
            var transitions = new SystemTransitionAuthority(
                MatchSystemTransitions.CreateCatalog(), MatchSystemTransitions.CreateRules(null, rules));
            var begun = transitions.TryBegin(HeroLevelUpSystemTransition.DefinitionId,
                new SystemTransitionInstanceId("rulebook-multiple-purchases"));
            Assert.That(begun.Accepted, Is.True);
            var purchases = rules.DrainOutcomes().Where(outcome =>
                outcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased).ToList();
            var upgrades = 0;
            var budget = 3;
            while (transitions.PendingStep != null && budget-- > 0)
            {
                var pending = transitions.PendingStep;
                var chosen = pending.Invocations[0];
                var answered = transitions.TryAnswer(chosen);
                Assert.That(answered.Accepted, Is.True);
                var outcomes = rules.DrainOutcomes();
                purchases.AddRange(outcomes.Where(outcome =>
                    outcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased));
                upgrades += outcomes.Count(outcome => outcome.Kind == HeroLevelUpOutcomeKind.CardUpgraded);
            }

            Assert.That(transitions.PendingStep, Is.Null);
            Assert.That(levels.GetLevel(hero).Value, Is.EqualTo(3),
                "p.15: 1 + 2 buys two levels, without a choice to stop at level 2");
            Assert.That(coins.GetCoins(hero), Is.Zero);
            Assert.That(purchases.Select(purchase => purchase.Spend.Amount), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(upgrades, Is.EqualTo(2), "one lowest-Tier upgrade per purchased level");
            Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(hero,
                configuration.CardDefinitions, configuration.CardInstances, out var lowest), Is.True);
            Assert.That(lowest, Is.EqualTo(CardTier.I),
                "one of the three starting Tier I cards remains after two upgrades");
        }

        [Test]
        public void LevelEight_UltimateColumnHasNoNinthPurchase()
        {
            Assert.That(HeroLevelSchedule.MaxCardTier(new HeroLevel(8)), Is.EqualTo(CardTier.IV));
            Assert.That(HeroLevelSchedule.HasNextLevel(new HeroLevel(8)), Is.False);
            Assert.That(() => HeroLevelSchedule.LevelUpCost(new HeroLevel(8)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(Enumerable.Range(1, 7).Sum(level =>
                HeroLevelSchedule.LevelUpCost(new HeroLevel(level))), Is.EqualTo(28));
        }

        private static AdjacentMoveAuthority CreateOneLaneBoard()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                StagedBoardParticipants.Create(), map, new TieBreakerState(TeamSide.Red));
            return new AdjacentMoveAuthority(map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>(), setup.Minions);
        }
    }
}
