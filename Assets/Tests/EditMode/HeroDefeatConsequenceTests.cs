using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.4: rulebook p.13's "When a Hero is Defeated" list, resolved in full.
    ///
    /// Organised by the page rather than by type, because that is what the milestone implements: the
    /// five printed lines in their printed order, the table that prices them, the exception that stops
    /// the whole list from running, and p.7's respawn that follows it a turn later.
    ///
    /// The order the list runs in is not this fixture's invention. p.14 prints the numbered
    /// <em>Attack/Defense Detailed Order</em>, whose step 6 is "Check if the attack was successful; if
    /// it was, remove the defending hero" and whose step 7 is "Apply 'After the attack:' text on the
    /// Attack card" - so the consequences belong inside the Attack action and ahead of the after-attack
    /// checkpoint, and the cases below assert exactly that position rather than merely that the facts
    /// happened.
    /// </summary>
    public sealed class HeroDefeatConsequenceTests
    {
        // -------------------------------------------------------------------------------------------
        // p.13's list, in the page's own order
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ASuccessfulHeroAttack_RunsPage13sListInThePrintedOrderInsideTheAttack()
        {
            var fixture = new DefeatFixture();
            var defeat = fixture.DefeatTheDefender();

            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(
                fixture.Round.HeroDefeatHistory), Is.True);
            Assert.That(fixture.Round.HeroDefeatHistory.Entries.Single().OutcomeId,
                Is.EqualTo(defeat.Defeat.Id),
                "The completed p.13 outcome, not a parsed event or cumulative counter, is retained.");

            Assert.That(defeat.Facts, Is.EqualTo(new[]
            {
                HeroDefeatFactKind.DefeatRecorded,
                HeroDefeatFactKind.RemovalAttempted,
                HeroDefeatFactKind.RemovalCompleted,
                HeroDefeatFactKind.UnresolvedCardResolved,
                HeroDefeatFactKind.RewardAwarded,
                HeroDefeatFactKind.LifeSpent
            }), "p.13's lines, in the order the page prints them.");

            var kinds = fixture.LastCardActionEventKinds;
            Assert.That(kinds, Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackSuccessful,
                CardActionResolutionEventKind.HeroDefeatRecorded,
                CardActionResolutionEventKind.HeroRemovalAttempted,
                CardActionResolutionEventKind.HeroRemovalCompleted,
                CardActionResolutionEventKind.HeroUnresolvedCardResolved,
                CardActionResolutionEventKind.HeroDefeatRewardAwarded,
                CardActionResolutionEventKind.TeamLifeSpent,
                CardActionResolutionEventKind.AttackResolutionCompleted
            }), "The stream reports each line where it happened.");

            // p.14's step 6 before step 7: every consequence is inside the Attack and ahead of the
            // after-attack checkpoint, which is where an Attack card's "After the attack" text applies.
            var afterAttack = fixture.IndexOfCheckpoint(TimingCheckpoint.AfterAttack);
            var actionEnded = fixture.IndexOfCheckpoint(TimingCheckpoint.ActionEnded);
            var lifeSpent = fixture.IndexOfCardActionEvent(CardActionResolutionEventKind.TeamLifeSpent);
            Assert.That(afterAttack, Is.GreaterThan(lifeSpent));
            Assert.That(actionEnded, Is.GreaterThan(afterAttack));

            var rewardEvent = fixture.LastCardActionEvents.Single(value =>
                value.Kind == CardActionResolutionEventKind.HeroDefeatRewardAwarded);
            Assert.That(rewardEvent.CoinAwards, Is.EqualTo(defeat.Reward.Operations),
                "The public event carries each authoritative Defeat and assist operation in the " +
                "ledger's stable recipient order.");
        }

        [Test]
        public void TheDefeat_RemovesTheHeroFromTheBoardAndLeavesTheTokensItPlaced()
        {
            var fixture = new DefeatFixture();
            var token = fixture.PlaceHazardFor(fixture.DefenderId, new HexCoordinate(2, 0));

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.RemovalResult.Kind, Is.EqualTo(DefeatRemovalResultKind.Removed));
            Assert.That(defeat.RemovedBodies.Count, Is.EqualTo(1));
            Assert.That(defeat.RemovedBodies.Single().HeroId, Is.EqualTo(fixture.DefenderId));
            Assert.That(
                fixture.Board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(fixture.DefenderId)),
                Is.False,
                "p.13: \"Remove the defeated hero from the board.\"");
            Assert.That(fixture.Board.PlacedTokens.Single().Id, Is.EqualTo(token.Id),
                "Rulebook p.19: \"Tokens are not removed when the hero who placed them is defeated.\"");
        }

        [Test]
        public void TheDefeat_ResolvesTheDefeatedHerosUnresolvedCardWithoutPerformingAnAction()
        {
            var fixture = new DefeatFixture();
            Assert.That(fixture.DefenderCard.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "The case is only evidence if the defender had an unresolved card.");

            fixture.DefeatTheDefenderThroughTheRound();
            Assert.That(fixture.DefenderCard.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.DefenderCard.State.Location, Is.EqualTo(CardLocation.TurnArea),
                "It is resolved rather than played away: p.13 resolves it \"without performing an " +
                "action\", so nothing about where the card sits changes.");
            // Milestone 8.35g. The attacker's own resolution finished with the Defeat, so the turn is
            // held for the attacking player before it closes. The claim below is unchanged - what
            // moved is that closing is now something a player does rather than something that
            // happens to them, so the pause is asserted first and then answered.
            Assert.That(fixture.Round.CurrentTurnAuthority.PendingTurnRelease, Is.Not.Null,
                "The Defeat completed the attacker's card, and a completed resolution waits.");
            Assert.That(fixture.Round.TryReleaseTurn().Accepted, Is.True);

            Assert.That(fixture.Round.TurnIndex, Is.EqualTo(2),
                "With both cards resolved the turn has nothing left to select and closes, so the " +
                "defeated hero is never offered the action p.13 just took away.");
        }

        [Test]
        public void ADefeatedHeroWithNoUnresolvedCard_ReportsNoSuchLine()
        {
            var fixture = new DefeatFixture();
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(fixture.DefenderCard), Is.True);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.ResolvedUnresolvedCardId.IsDefined, Is.False);
            Assert.That(defeat.Facts, Has.No.Member(HeroDefeatFactKind.UnresolvedCardResolved),
                "p.13's line is conditional - \"If the defeated hero had an unresolved card\" - so an " +
                "absent card is an absent fact rather than a fact about nothing.");
        }

        [Test]
        public void TheDefeat_CancelsTheDefeatedHerosOwnEffectsAndLeavesEveryOtherHerosAlone()
        {
            var fixture = new DefeatFixture();
            var defenderEffect = fixture.CreateGlobalEffect("defender-effect", fixture.DefenderId);
            var attackerEffect = fixture.CreateGlobalEffect("attacker-effect", fixture.AttackerId);

            // Through the round, because the cancellation is raised as a checkpoint in the resolver's
            // stream and it is the turn authority that applies a checkpoint to the lifecycle.
            fixture.DefeatTheDefenderThroughTheRound();

            Assert.That(fixture.PhaseOf(defenderEffect), Is.EqualTo(ActiveEffectPhase.Ended),
                "p.13: \"Defeated hero's actions with Active effects are canceled.\"");
            Assert.That(fixture.PhaseOf(attackerEffect), Is.Not.EqualTo(ActiveEffectPhase.Ended),
                "Both effects are Global, and the attacker's survives: p.13 names the defeated hero's " +
                "own actions, and p.17 says the same from the effect's side - it ends when \"you are " +
                "defeated\". Reading the declared scope instead would have cancelled both.");
        }

        [Test]
        public void ASuccessfullyDefendedAttack_RunsNoneOfPage13sList()
        {
            var fixture = new DefeatFixture();
            var lifeBefore = fixture.Round.TeamLives.GetCounters(fixture.DefenderTeamId);

            fixture.DefendThroughTheRound();

            Assert.That(
                fixture.Board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(fixture.DefenderId)),
                Is.True);
            Assert.That(fixture.DefenderCard.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Round.TeamLives.GetCounters(fixture.DefenderTeamId),
                Is.EqualTo(lifeBefore));
            Assert.That(fixture.Coins(fixture.AttackerId), Is.Zero,
                "p.13's rewards are the Defeat's, and there was no Defeat.");
            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(
                fixture.Round.HeroDefeatHistory), Is.False);
        }

        [Test]
        public void HeroDefeatRoundHistory_RoundTripsByteIdenticallyAndRejectsIncoherentEntries()
        {
            var fixture = new DefeatFixture();
            fixture.DefeatTheDefenderThroughTheRound();
            var entry = fixture.Round.HeroDefeatHistory.Entries.Single();
            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Definitions,
                new[] { fixture.Board.MapDefinition });

            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(
                restored.Round.HeroDefeatHistory), Is.True);
            Assert.That(restored.Round.HeroDefeatHistory.Entries.Select(value =>
                    (value.OutcomeId.Value, value.DefeatedHeroId.Value)),
                Is.EqualTo(new[] { (entry.OutcomeId.Value, entry.DefeatedHeroId.Value) }));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload), "An unchanged restored history must serialize byte-identically.");

            var corrupted = RewriteHistoryHeroAsUnknown(
                payload, entry.OutcomeId.Value, entry.DefeatedHeroId.Value);
            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    corrupted,
                    fixture.Definitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload));

            corrupted = RewriteHistoryOutcomeAsUnawarded(payload, entry.OutcomeId.Value);
            exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    corrupted,
                    fixture.Definitions,
                    new[] { fixture.Board.MapDefinition }));
            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload),
                "A current-round outcome cannot name a Defeat the cumulative p.13 ledger never " +
                "awarded; otherwise payload tampering could invent a round-history fact in isolation.");
        }

        // -------------------------------------------------------------------------------------------
        // p.13's table: coins, assist coins, and Life counters
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheRewardSchedule_IsPage13sPrintedTable()
        {
            // The page's table, read column by column. Level 1-3 have lowest card Tier I, 4-6 have II,
            // 7-8 have III; the Coins row is the Level itself and the Assist Coins row is the Tier.
            var expectedTiers = new[]
            {
                CardTier.I, CardTier.I, CardTier.I,
                CardTier.II, CardTier.II, CardTier.II,
                CardTier.III, CardTier.III
            };
            var expectedAssist = new[] { 1, 1, 1, 2, 2, 2, 3, 3 };
            for (var level = HeroLevel.Minimum; level <= HeroLevel.Maximum; level++)
            {
                var tier = expectedTiers[level - 1];
                Assert.That(HeroDefeatRewardSchedule.DefeatCoins(new HeroLevel(level)), Is.EqualTo(level),
                    "p.13: \"collect coins equal to that hero's level\".");
                Assert.That(HeroDefeatRewardSchedule.AssistCoins(tier),
                    Is.EqualTo(expectedAssist[level - 1]));
                Assert.That(HeroDefeatRewardSchedule.LifeCounters(tier),
                    Is.EqualTo(expectedAssist[level - 1]),
                    "The Life counters spent are the same Tier the assist coins read, which is why " +
                    "the table's two rows agree column by column.");
            }

            Assert.That(() => HeroDefeatRewardSchedule.AssistCoins(CardTier.None),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "p.15's Basic pair has no Tier and is \"not considered when checking your lowest card " +
                "Tier\", so there is no number to return rather than a zero.");
            Assert.That(() => HeroDefeatRewardSchedule.LifeCounters(CardTier.IV),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "p.16's Ultimate is unlocked only once every non-Basic card is Tier III, so it can " +
                "never be a hero's lowest card Tier.");
        }

        [Test]
        public void TheAttacker_CollectsCoinsEqualToTheDefeatedHerosLevel()
        {
            var fixture = new DefeatFixture();
            var level = fixture.Round.HeroProgression.GetLevel(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.Defeat.DefeatCoins, Is.EqualTo(level.Value));
            Assert.That(fixture.Coins(fixture.AttackerId), Is.EqualTo(level.Value));
            Assert.That(defeat.Reward.Operations.Single().AwardKind, Is.EqualTo(CoinAwardKind.Defeat),
                "The configured slice is one hero per team, so the Defeat coin is the only operation " +
                "there is a recipient for; the assist half is asserted at its own derivation below and " +
                "its distribution is owned by HeroCoinLedgerTests.");
        }

        [Test]
        public void AssistCoins_AreDerivedFromTheDefeatedHerosLowestCardTierAndReachEveryOtherFriendlyHero()
        {
            var fixture = new DefeatFixture();
            Assert.That(fixture.LowestNonBasicTier(fixture.DefenderId), Is.EqualTo(CardTier.I));

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.Defeat.AssistCoins,
                Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(CardTier.I)),
                "p.13: assist coins are \"based on the defeated hero's lowest card Tier\".");

            // The distribution, on a team that has someone to assist. The outcome is the one the Defeat
            // derived; only the participants differ, which is what isolates the derivation from the
            // accounting that already owned the sharing rule.
            var participants = TwoAgainstOne();
            var accounting = new HeroCoinLedger(participants);
            var shared = new DefeatOutcome(
                new DefeatOutcomeId("assist-distribution"),
                new DefeatTargetId("blue-hero"),
                new TeamId("blue-team"),
                new HeroId("red-hero-1"),
                defeat.Defeat.DefeatCoins,
                defeat.Defeat.AssistCoins);
            Assert.That(accounting.Award(shared).Accepted, Is.True);
            Assert.That(accounting.GetCoins(new HeroId("red-hero-1")),
                Is.EqualTo(defeat.Defeat.DefeatCoins));
            Assert.That(accounting.GetCoins(new HeroId("red-hero-2")),
                Is.EqualTo(defeat.Defeat.AssistCoins));
            Assert.That(accounting.GetCoins(new HeroId("blue-hero")), Is.Zero,
                "p.13 pays the defeating hero's own team.");
        }

        [Test]
        public void LifeSpending_ReadsTheDefeatedHerosLowestNonBasicTierThroughTheMilestone71Query()
        {
            var fixture = new DefeatFixture();
            var before = fixture.Round.TeamLives.GetCounters(fixture.DefenderTeamId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.HasLifeSpend, Is.True);
            Assert.That(defeat.LifeSpend.TeamId, Is.EqualTo(fixture.DefenderTeamId),
                "p.13 spends the defeated hero's own team's counters.");
            Assert.That(defeat.LifeSpend.Spent, Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)));
            Assert.That(fixture.Round.TeamLives.GetCounters(fixture.DefenderTeamId),
                Is.EqualTo(before - defeat.LifeSpend.Spent));
            Assert.That(fixture.Round.TeamLives.GetCounters(fixture.AttackerTeamId),
                Is.EqualTo(fixture.StartingLifeCounters),
                "The attacking team spends nothing.");
        }

        [Test]
        public void SpendingTheLastCounter_EndsTheGameAndNamesTheTeamThatLost()
        {
            var fixture = new DefeatFixture(lifeCountersPerTeam: 1);

            var result = fixture.DefeatTheDefenderThroughTheRound();

            Assert.That(fixture.Round.TeamLives.GetCounters(fixture.DefenderTeamId), Is.Zero);
            Assert.That(fixture.Round.TeamLives.EliminatedTeamId, Is.EqualTo(fixture.DefenderTeamId));
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "p.13: \"If the last counter is spent, the game ends and the team with no Life " +
                "counters left, loses the game.\"");
            var ended = result.Events.Single(value =>
                value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.TeamId, Is.EqualTo(fixture.DefenderTeamId));
            Assert.That(fixture.Round.TurnIndex, Is.EqualTo(1),
                "\"The game ends\" is taken literally: the turn is not completed and the round does " +
                "not advance past it.");
        }

        [Test]
        public void ADefeatWorthMoreCountersThanRemain_SpendsWhatThereIsAndStillEndsTheGame()
        {
            var fixture = new DefeatFixture(lifeCountersPerTeam: 1);
            var spend = fixture.Round.TeamLives.TrySpend(fixture.DefenderTeamId, 3);

            Assert.That(spend.Accepted, Is.True);
            Assert.That(spend.Spend.Requested, Is.EqualTo(3));
            Assert.That(spend.Spend.Spent, Is.EqualTo(1));
            Assert.That(spend.Spend.SpentLastCounter, Is.True,
                "p.13 says nothing about a Tier larger than the pile, and its next sentence settles " +
                "it: the spend that empties the pile is a spend that happened.");
            Assert.That(fixture.Round.TeamLives.TrySpend(fixture.DefenderTeamId, 1).RejectionReason,
                Is.EqualTo(TeamLifeSpendRejectionReason.TeamAlreadyEliminated));
        }

        // -------------------------------------------------------------------------------------------
        // p.7's respawn, over the full Phase 4 map
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnOffBoardHero_IsOfferedItsEmptyThroneSpawnPointsFirst()
        {
            var board = OfficialBoardWithout(BlueHeroId);

            var choice = HeroRespawnRules.CreateChoice(
                BlueHeroId, new CardInstanceId("respawn-card"), OfficialParticipants(), board,
                RuleModificationSet.Empty);

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Destinations, Is.Not.Empty);
            Assert.That(choice.Destinations.All(destination => !destination.IsFallback), Is.True,
                "Every spawn point is empty, so no fallback is reached.");
            Assert.That(choice.Destinations.All(destination =>
                board.MapDefinition.GetZoneForSpace(
                    board.MapDefinition.Spaces.Single(space =>
                        space.Coordinate.Equals(destination.Coordinate)).Id).IsThrone), Is.True,
                "p.7 respawns a hero at its own Throne Zone spawn points.");
        }

        [Test]
        public void WhenEverySpawnPointIsTaken_TheNearestEmptySpaceOnTheWholeMapIsOffered()
        {
            var board = OfficialBoardWithout(BlueHeroId, blockSpawnRing: true);
            var spawns = BlueHeroSpawnCoordinates(board);
            var blocked = SpawnPointsAndTheirNeighbours(spawns);

            var choice = HeroRespawnRules.CreateChoice(
                BlueHeroId, new CardInstanceId("respawn-card"), OfficialParticipants(), board,
                RuleModificationSet.Empty);

            Assert.That(choice.Destinations, Is.Not.Empty,
                "Before milestone 7.4 the fallback searched the adjacent ring alone, so a hero whose " +
                "spawn points and their neighbours were all occupied was offered nothing at all on a " +
                "map with dozens of empty spaces left.");
            Assert.That(choice.Destinations.All(destination => destination.IsFallback), Is.True);
            Assert.That(choice.Destinations.All(destination =>
                !blocked.Contains(destination.Coordinate)), Is.True);
            var distances = choice.Destinations
                .Select(destination => spawns.Min(spawn =>
                    HexDistanceQuery.Calculate(destination.Coordinate, spawn)))
                .ToArray();
            Assert.That(distances.Distinct().Count(), Is.EqualTo(1),
                "\"Nearest\" is one distance, so every destination offered is tied at it.");
            Assert.That(distances[0], Is.EqualTo(2),
                "The adjacent ring is full, so the nearest empty space is the next one out.");
            Assert.That(choice.Destinations.All(destination =>
                board.IsEmptyPlayableSpace(destination.Coordinate)), Is.True);
        }

        [Test]
        public void ADefeatedHero_MayRespawnBeforeItsNextActionOrResolveThatCardWithoutOne()
        {
            foreach (var respawning in new[] { true, false })
            {
                var session = CreateAdjacentSession();
                CommitTheAttackTurn(session);
                DefeatThroughTheSession(session);
                Assert.That(
                    session.Projection.Heroes.Any(hero => hero.HeroId.Equals(BlueHeroId)), Is.False);

                // Milestone 8.35g. The attacking player releases their finished turn before turn one
                // can close; without it the commitments below are submitted into a turn still running.
                Assert.That(session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);

                // Turn two. The Red hero acts first on Initiative and Holds; the defeated hero is then
                // selected, and p.7's choice is what it is offered before its action rather than
                // instead of one.
                var red = session.Projection.Cards.Single(card => card.Id.Value == "red-lancer-card");
                var blue = session.Projection.Cards.Single(card => card.Id.Value == "blue-longbow-card");
                Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
                Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
                while (!session.Projection.SelectedActor.ActorId.Equals(BlueHeroId))
                {
                    // Milestone 8.35g. ABOVE the read below, not beside the Hold: acknowledging the
                    // resolved card is what empties the selection this loop steers by.
                    if (session.Projection.PendingTurnRelease != null)
                    {
                        Assert.That(session.Execute(
                            DirectorCommand.ReleaseTurn()).Accepted, Is.True);
                        continue;
                    }

                    var acting = session.Projection.SelectedActor;
                    Assert.That(session.Execute(
                        DirectorCommand.PerformHold(acting.CardInstanceId)).Accepted, Is.True);
                }

                var choice = session.Projection.PendingHeroRespawnChoice;
                Assert.That(choice, Is.Not.Null, "p.7 offers the choice at the hero's next action.");
                Assert.That(choice.HeroId, Is.EqualTo(BlueHeroId));
                Assert.That(choice.Destinations, Is.Not.Empty);

                if (respawning)
                {
                    var invocation = choice.RespawnInvocations[0];
                    Assert.That(session.Execute(
                        DirectorCommand.RespawnHero(invocation)).Accepted, Is.True);
                    Assert.That(
                        session.Projection.Heroes.Single(hero => hero.HeroId.Equals(BlueHeroId))
                            .Coordinate,
                        Is.EqualTo(invocation.Destination));
                    Assert.That(session.Projection.PendingHeroRespawnChoice, Is.Null);
                    Assert.That(
                        session.Projection.Cards
                            .Single(card => card.Id.Equals(blue.Id)).State.ResolutionStatus,
                        Is.EqualTo(CardResolutionStatus.Unresolved),
                        "The respawn happens before the action, so the card is still owed one.");
                }
                else
                {
                    Assert.That(session.Execute(
                        DirectorCommand.ResolveCardWithoutRespawn(choice.DeclineInvocation)).Accepted,
                        Is.True);
                    Assert.That(
                        session.Projection.Heroes.Any(hero => hero.HeroId.Equals(BlueHeroId)), Is.False,
                        "Declining leaves the hero off the board.");
                    Assert.That(
                        session.Projection.Cards
                            .Single(card => card.Id.Equals(blue.Id)).State.ResolutionStatus,
                        Is.EqualTo(CardResolutionStatus.Resolved),
                        "p.7: the card is resolved without performing an action.");
                }
            }
        }

        [Test]
        public void AHeroStandingOnTheBoard_IsOfferedNoRespawnAtAll()
        {
            var board = OfficialBoardWithout(default);

            Assert.That(
                HeroRespawnRules.CreateChoice(
                    BlueHeroId, new CardInstanceId("respawn-card"), OfficialParticipants(), board,
                    RuleModificationSet.Empty),
                Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Phase 7's durability rule
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ADefeatsConsequences_SurviveSnapshotRestoreReplayUndoAndARefusedCommand()
        {
            var session = CreateAdjacentSession();
            var beforeAnyCommand = session.CaptureSnapshot();

            // A refused command changes nothing, the counters included.
            Assert.That(session.Execute(DirectorCommand.StopMovement()).Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));
            Assert.That(session.Projection.TeamLifeCounters.All(entry => entry.Value == 4), Is.True,
                "The configured setup states four counters per team, and the projection reads the " +
                "match's own rather than the setup's.");

            var afterCommits = CommitTheAttackTurn(session);
            var beforeTheDefeat = session.CaptureSnapshot();
            var lifeBefore = LifeOf(session, TeamSide.Blue);
            Assert.That(SnapshotHasHeroDefeatThisRound(beforeTheDefeat), Is.False);

            OpenTheDefenseThroughTheSession(session);
            var atTheDefenseChoice = session.CaptureSnapshot();
            DeclineTheDefenseThroughTheSession(session);
            var afterTheDefeat = session.CaptureSnapshot();
            Assert.That(LifeOf(session, TeamSide.Blue), Is.LessThan(lifeBefore),
                "The case is only evidence if a counter was actually spent.");
            Assert.That(afterTheDefeat, Is.Not.EqualTo(atTheDefenseChoice));
            Assert.That(SnapshotHasHeroDefeatThisRound(afterTheDefeat), Is.True);

            // Retained-prefix Undo truncated immediately after the new state transition, and re-executing
            // the same command reconstructs it: the counters, the coins and the removal all come back by
            // replay rather than by being kept.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(atTheDefenseChoice));
            Assert.That(SnapshotHasHeroDefeatThisRound(session.CaptureSnapshot()), Is.False);
            Assert.That(LifeOf(session, TeamSide.Blue), Is.EqualTo(lifeBefore),
                "An undone Defeat gives the counter back, because the counter is state the replay " +
                "rebuilds rather than a number written down once.");
            DeclineTheDefenseThroughTheSession(session);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterTheDefeat));
            Assert.That(SnapshotHasHeroDefeatThisRound(session.CaptureSnapshot()), Is.True);

            // And truncated immediately before it, back to the turn that had not started the Attack.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeTheDefeat));
            Assert.That(SnapshotHasHeroDefeatThisRound(session.CaptureSnapshot()), Is.False);
            Assert.That(LifeOf(session, TeamSide.Blue), Is.EqualTo(lifeBefore));
            DefeatThroughTheSession(session);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterTheDefeat));
            Assert.That(SnapshotHasHeroDefeatThisRound(session.CaptureSnapshot()), Is.True);

            // Truncation on the other side of the transition: back to before the commitments, where no
            // Defeat has happened at all.
            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));
            Assert.That(LifeOf(session, TeamSide.Blue), Is.EqualTo(4));
            Assert.That(SnapshotHasHeroDefeatThisRound(session.CaptureSnapshot()), Is.False);

            // Restore-and-continue through the same public command path, from a payload written before
            // the Defeat, reproduces the payload the live match produced.
            var restored = CreateAdjacentSession();
            Assert.That(restored.RestoreSnapshot(beforeTheDefeat).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(beforeTheDefeat));
            Assert.That(LifeOf(restored, TeamSide.Blue), Is.EqualTo(lifeBefore));
            DefeatThroughTheSession(restored);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(afterTheDefeat));

            // And from a payload written after it: the spent counters, the removal and the resolved
            // card all restore as recorded rather than as defaults.
            var afterwards = CreateAdjacentSession();
            Assert.That(afterwards.RestoreSnapshot(afterTheDefeat).Accepted, Is.True);
            Assert.That(afterwards.CaptureSnapshot(), Is.EqualTo(afterTheDefeat));
            Assert.That(SnapshotHasHeroDefeatThisRound(afterwards.CaptureSnapshot()), Is.True);
            Assert.That(LifeOf(afterwards, TeamSide.Blue), Is.EqualTo(lifeBefore - 1));
            Assert.That(
                afterwards.Projection.Heroes.Any(hero => hero.HeroId.Equals(BlueHeroId)), Is.False,
                "The removed hero is still off the board after a restore.");
            Assert.That(afterCommits, Is.Not.EqualTo(beforeAnyCommand));
        }

        [Test]
        public void ARestoredMatch_RefusesAPayloadWhoseCountersAreNotEveryTeamsExactlyOnce()
        {
            var participants = OfficialParticipants();
            var counters = new TeamLifeState(participants, 4).CaptureRestoreState().Counters;
            // Milestone 9.30f. A restore also says what each team began with (snapshot v52).
            var starting = counters;

            Assert.That(
                () => TeamLifeState.Restore(participants, new TeamLifeRestoreState(counters.Take(1), starting)),
                Throws.InstanceOf<ArgumentException>(),
                "A restore that covered one team would leave the other's counters to a default.");
            Assert.That(
                () => TeamLifeState.Restore(
                    participants,
                    new TeamLifeRestoreState(counters.Concat(new[] { counters[0] }), starting)),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(
                () => TeamLifeState.Restore(
                    participants,
                    new TeamLifeRestoreState(counters
                        .Select(entry => new KeyValuePair<TeamId, int>(entry.Key, -1)), starting)),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(
                TeamLifeState.Restore(
                        participants,
                        new TeamLifeRestoreState(counters
                            .Select(entry => new KeyValuePair<TeamId, int>(entry.Key, 0)), starting))
                    .HasEliminatedTeam,
                Is.True,
                "Zero is a legal recorded value and the one p.13's last sentence produces.");
        }

        // -------------------------------------------------------------------------------------------
        // Finding `F-ER2-09`: the relocated standard-minion content
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheRelocatedMinionContent_AnswersExactlyWhatThePagesPrint()
        {
            var catalog = StandardMinionProfileCatalog.Rulebook;
            Assert.That(catalog.Get(StandardMinionType.Melee).DefeatCoins, Is.EqualTo(2));
            Assert.That(catalog.Get(StandardMinionType.Ranged).DefeatCoins, Is.EqualTo(2));
            Assert.That(catalog.Get(StandardMinionType.Heavy).DefeatCoins, Is.EqualTo(4),
                "p.13: \"Ranged and melee minions are worth 2 coins when defeated, while heavy " +
                "minions are worth 4\".");
            Assert.That(catalog.Profiles.Count, Is.EqualTo(3));
            Assert.That(() => catalog.Get(StandardMinionType.None),
                Throws.InstanceOf<KeyNotFoundException>(),
                "An undeclared subtype is named rather than defaulted to a reward of zero.");

            // p.14's Minion Defense Modifiers box, read at every distance the old branches covered and
            // one past it. The pre-relocation expression is restated here as the oracle so the case
            // measures the relocation rather than the table.
            foreach (var type in new[]
                     { StandardMinionType.Melee, StandardMinionType.Ranged, StandardMinionType.Heavy })
            {
                for (var distance = 1; distance <= 4; distance++)
                {
                    foreach (var friendly in new[] { true, false })
                    {
                        var expected = distance == 1 &&
                            (type == StandardMinionType.Melee || type == StandardMinionType.Heavy)
                                ? friendly ? 1 : -1
                                : !friendly && type == StandardMinionType.Ranged && distance <= 2
                                    ? -1
                                    : 0;
                        var relationship = friendly
                            ? MinionDefenseRelationship.Friendly
                            : MinionDefenseRelationship.Enemy;
                        Assert.That(
                            catalog.Get(type).DefenseModifierAt(relationship, distance),
                            Is.EqualTo(expected),
                            $"{type} at distance {distance}, friendly={friendly}");
                    }
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static HeroId BlueHeroId => new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static HeroId RedHeroId => new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static int LifeOf(MatchSession session, TeamSide side)
        {
            var teamId = session.Projection.Setup.Teams
                .Single(team => team.TeamSide == side).TeamId;
            return session.Projection.TeamLifeCounters.Single(entry => entry.Key.Equals(teamId)).Value;
        }

        private static bool SnapshotHasHeroDefeatThisRound(string payload)
        {
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                payload, out var envelope, out var rejection), Is.True);
            Assert.That(rejection, Is.EqualTo(RuleEventLogEnvelopeRejectionReason.None));
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload,
                configuration.CardDefinitions,
                new[] { map },
                setup.Minions);
            return HeroDefeatedThisRoundQuery.IsSatisfied(restored.Round.HeroDefeatHistory);
        }

        private static string RewriteHistoryHeroAsUnknown(
            string payload,
            string outcomeId,
            string heroId)
        {
            var bytes = Convert.FromBase64String(payload);
            var outcomeBytes = Encoding.UTF8.GetBytes(outcomeId);
            var heroBytes = Encoding.UTF8.GetBytes(heroId);
            var outcomeIndex = FindBytes(bytes, outcomeBytes, 0);
            Assert.That(outcomeIndex, Is.GreaterThanOrEqualTo(0));
            var heroIndex = FindBytes(bytes, heroBytes, outcomeIndex + outcomeBytes.Length);
            Assert.That(heroIndex, Is.GreaterThanOrEqualTo(0));

            // Preserve the BinaryWriter string width and corrupt only the typed identity.
            bytes[heroIndex] = bytes[heroIndex] == (byte)'x' ? (byte)'y' : (byte)'x';
            return Convert.ToBase64String(bytes);
        }

        private static string RewriteHistoryOutcomeAsUnawarded(string payload, string outcomeId)
        {
            var bytes = Convert.FromBase64String(payload);
            var outcomeBytes = Encoding.UTF8.GetBytes(outcomeId);
            var outcomeIndex = FindBytes(bytes, outcomeBytes, 0);
            Assert.That(outcomeIndex, Is.GreaterThanOrEqualTo(0));

            // Round opening precedes the cumulative coin ledger in the wire format, so this is the
            // history occurrence alone. Keep BinaryWriter's string width stable.
            bytes[outcomeIndex] = bytes[outcomeIndex] == (byte)'x' ? (byte)'y' : (byte)'x';
            return Convert.ToBase64String(bytes);
        }

        private static int FindBytes(byte[] source, byte[] value, int start)
        {
            for (var index = start; index <= source.Length - value.Length; index++)
            {
                var matches = true;
                for (var offset = 0; offset < value.Length; offset++)
                {
                    if (source[index + offset] == value[offset]) continue;
                    matches = false;
                    break;
                }

                if (matches) return index;
            }

            return -1;
        }

        /// <summary>
        /// A configured match on the official Phase 4 map with the two heroes standing next to each
        /// other, so the Red hero's printed non-Ranged Attack can reach the Blue one. Only the two
        /// starting coordinates differ from the composed setup; everything else - the map, the minions,
        /// the Wave and Life counts - is the configured slice as it stands.
        /// </summary>
        private static MatchSession CreateAdjacentSession()
        {
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redAt = setup.Heroes.Single(hero => hero.HeroId.Equals(RedHeroId)).Coordinate;
            var taken = new HashSet<HexCoordinate>(setup.Minions.Select(minion => minion.Coordinate))
            {
                redAt
            };
            var playable = new HashSet<HexCoordinate>(map.Spaces.Select(space => space.Coordinate));
            var blueAt = redAt.GetNeighbors()
                .First(coordinate => playable.Contains(coordinate) && !taken.Contains(coordinate));
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(RedHeroId, redAt),
                    new HeroOccupancy(BlueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Commits the pair whose Initiative makes the Red hero act first with its printed non-Ranged
        /// Attack, and returns the payload the commitments produced.
        /// </summary>
        private static string CommitTheAttackTurn(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blue = session.Projection.Cards.Single(card => card.Id.Value == "blue-hammer-card");
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(RedHeroId));
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// The whole Defeat as one command sequence a player could type: start the Attack, name the
        /// enemy hero, and decline the Defense.
        /// </summary>
        private static void DefeatThroughTheSession(MatchSession session)
        {
            OpenTheDefenseThroughTheSession(session);
            DeclineTheDefenseThroughTheSession(session);
        }

        private static void OpenTheDefenseThroughTheSession(MatchSession session)
        {
            Assert.That(session.Execute(DirectorCommand.StartAction(
                new ActionDefinitionId("primary-attack"))).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(BlueHeroId)).Accepted, Is.True);
        }

        private static void DeclineTheDefenseThroughTheSession(MatchSession session)
        {
            Assert.That(session.Execute(DirectorCommand.DeclineDefense()).Accepted, Is.True);
        }

        private static MatchParticipants OfficialParticipants() =>
            MatchConfiguration.CreateForInternalTest(TeamSide.Red).Participants;

        /// <summary>
        /// The official Phase 4 map with every configured hero on it except the named one, which is the
        /// state a defeated hero's respawn choice is derived against.
        /// </summary>
        /// <param name="blockSpawnRing">
        /// Fills every Blue hero spawn point and every space adjacent to one with an obstacle, so the
        /// nearest-empty search has to leave the adjacent ring to find anything. Obstacles are used
        /// rather than miniatures because the only question the rule asks is whether a space is empty
        /// and playable, and they are declared at construction because that is where the board takes
        /// them.
        /// </param>
        private static AdjacentMoveAuthority OfficialBoardWithout(
            HeroId absentHeroId,
            bool blockSpawnRing = false)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var heroes = setup.Heroes
                .Where(hero => !hero.HeroId.Equals(absentHeroId))
                .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))
                .ToArray();
            var obstacles = Array.Empty<ObstacleOccupancy>();
            if (blockSpawnRing)
            {
                var occupied = new HashSet<HexCoordinate>(heroes.Select(hero => hero.Coordinate));
                var playable = new HashSet<HexCoordinate>(map.Spaces.Select(space => space.Coordinate));
                obstacles = SpawnPointsAndTheirNeighbours(BlueHeroSpawnCoordinates(map))
                    .Where(coordinate =>
                        playable.Contains(coordinate) && !occupied.Contains(coordinate))
                    .Select((coordinate, index) => new ObstacleOccupancy(
                        new ObstacleId($"respawn-block-{index}"), coordinate))
                    .ToArray();
            }

            return new AdjacentMoveAuthority(
                map,
                heroes,
                obstacles,
                Array.Empty<QuickGameMinionSetupState>());
        }

        private static HexCoordinate[] BlueHeroSpawnCoordinates(AdjacentMoveAuthority board) =>
            BlueHeroSpawnCoordinates(board.MapDefinition);

        private static HexCoordinate[] BlueHeroSpawnCoordinates(BoardMapDefinition map) =>
            map.SpawnPoints
                .Where(spawn =>
                    string.Equals(spawn.TeamId, "blue", StringComparison.Ordinal) &&
                    string.Equals(spawn.UnitType, "hero", StringComparison.Ordinal) &&
                    map.GetZoneForSpace(spawn.SpaceId).IsThrone)
                .Select(spawn => map.Spaces
                    .Single(space => space.Id.Equals(spawn.SpaceId)).Coordinate)
                .ToArray();

        private static HashSet<HexCoordinate> SpawnPointsAndTheirNeighbours(
            IReadOnlyList<HexCoordinate> spawns)
        {
            var blocked = new HashSet<HexCoordinate>();
            foreach (var spawn in spawns)
            {
                blocked.Add(spawn);
                foreach (var neighbor in spawn.GetNeighbors()) blocked.Add(neighbor);
            }

            return blocked;
        }

        private static MatchParticipants TwoAgainstOne() => new MatchParticipants(
            new[]
            {
                new Team(new TeamId("red-team"), TeamSide.Red),
                new Team(new TeamId("blue-team"), TeamSide.Blue)
            },
            new[]
            {
                new Player(new PlayerId("red-player-1"), new TeamId("red-team")),
                new Player(new PlayerId("red-player-2"), new TeamId("red-team")),
                new Player(new PlayerId("blue-player"), new TeamId("blue-team"))
            },
            new[]
            {
                new Hero(new HeroId("red-hero-1"), new PlayerId("red-player-1"), HeroDashboard.RulebookLayout),
                new Hero(new HeroId("red-hero-2"), new PlayerId("red-player-2"), HeroDashboard.RulebookLayout),
                new Hero(new HeroId("blue-hero"), new PlayerId("blue-player"), HeroDashboard.RulebookLayout)
            });

        /// <summary>
        /// One configured turn on a synthetic board, arranged so the Red hero's printed non-Ranged
        /// Attack can reach the Blue hero and the Blue hero still holds an unresolved card of its own.
        ///
        /// The round authority is real, because p.13's rewards read the Levels it owns and its Life
        /// counters are what the Defeat spends; the board is synthetic because none of p.13's lines is
        /// about geography, and the two cases that are about the map build their own official one.
        /// </summary>
        private sealed class DefeatFixture
        {
            private readonly MatchConfiguration configuration;
            private readonly CardActionResolutionAuthority cardActions;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private IReadOnlyList<ResolutionStep> lastSteps = Array.Empty<ResolutionStep>();

            public DefeatFixture(int lifeCountersPerTeam = 4)
            {
                configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                StartingLifeCounters = lifeCountersPerTeam;
                AttackerId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                DefenderId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

                // Red Pursuit at Initiative 7 against Blue Hammer at 6: the Red hero acts first with the
                // team's remaining printed non-Ranged Attack, and the Blue hero's card stays unresolved
                // until p.13 resolves it.
                AttackerCard = configuration.RedHand.Instances[3];
                DefenderCard = configuration.BlueHand.Instances[3];
                Board = new AdjacentMoveAuthority(
                    Coordinates(3),
                    new[]
                    {
                        new HeroOccupancy(AttackerId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(DefenderId, new HexCoordinate(1, 0))
                    });
                cardActions = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), Board, Markers);
                Round = new RoundResolutionAuthority(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    1,
                    new RoundResolutionComposition(
                        cardActions,
                        null,
                        timing,
                        Markers,
                        null,
                        null,
                        null,
                        new TeamLifeState(configuration.Participants, lifeCountersPerTeam),
                        null,
                        null));
                Assert.That(Round.TryCommitCard(AttackerId, AttackerCard.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(DefenderId, DefenderCard.Id).Accepted, Is.True);
                Assert.That(Round.CurrentTurnAuthority.SelectedActor.CardInstanceId,
                    Is.EqualTo(AttackerCard.Id));
            }

            public HeroId AttackerId { get; }
            public HeroId DefenderId { get; }
            public CardInstance AttackerCard { get; }
            public CardInstance DefenderCard { get; }
            public AdjacentMoveAuthority Board { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();
            public RoundResolutionAuthority Round { get; }
            public int StartingLifeCounters { get; }

            public IReadOnlyList<CardDefinition> Definitions => configuration.CardDefinitions;

            public TeamId AttackerTeamId => configuration.Participants.GetTeamForHero(AttackerId).Id;
            public TeamId DefenderTeamId => configuration.Participants.GetTeamForHero(DefenderId).Id;

            public int Coins(HeroId heroId) => cardActions.CoinLedger == null
                ? 0
                : cardActions.CoinLedger.GetCoins(heroId);

            public CardTier LowestNonBasicTier(HeroId heroId)
            {
                Assert.That(CardCharacteristicQuery.TryGetLowestNonBasicTier(
                        new CardCharacteristicCriteria(ownerId: heroId),
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        out var tier), Is.True);
                return tier;
            }

            public IReadOnlyList<CardActionResolutionEventKind> LastCardActionEventKinds =>
                LastCardActionEvents.Select(step => step.Kind).ToArray();

            public IReadOnlyList<CardActionResolutionEvent> LastCardActionEvents =>
                lastSteps
                    .Where(step => step.Kind == ResolutionStepKind.Fact)
                    .Select(step => step.Fact)
                    .ToArray();

            public int IndexOfCheckpoint(TimingCheckpoint checkpoint)
            {
                for (var index = 0; index < lastSteps.Count; index++)
                {
                    if (lastSteps[index].Kind == ResolutionStepKind.Checkpoint &&
                        lastSteps[index].Checkpoint == checkpoint)
                    {
                        return index;
                    }
                }

                return -1;
            }

            public int IndexOfCardActionEvent(CardActionResolutionEventKind kind)
            {
                for (var index = 0; index < lastSteps.Count; index++)
                {
                    if (lastSteps[index].Kind == ResolutionStepKind.Fact &&
                        lastSteps[index].Fact.Kind == kind)
                    {
                        return index;
                    }
                }

                return -1;
            }

            public ActiveEffectPhase PhaseOf(ActiveEffectId id) =>
                timing.Effects.Single(effect => effect.Id.Equals(id)).Phase;

            /// <summary>
            /// A Global-scope effect controlled by the named hero. Global is the scope that makes the
            /// cancellation reading visible: a controller-based reading cancels only the defeated
            /// hero's, while the scope-based one this milestone replaced cancelled every Global effect
            /// whichever hero was defeated.
            /// </summary>
            public ActiveEffectId CreateGlobalEffect(string id, HeroId controllerId)
            {
                var effectId = new ActiveEffectId(id);
                timing.Create(
                    effectId,
                    new ActiveEffectDefinition(
                        $"{id}-definition",
                        ActiveEffectAffectedScopeKind.Global,
                        ActiveEffectDurationKind.ThisRound,
                        TimingCheckpoint.ActionStarted,
                        TimingCheckpoint.ActionStarted,
                        TimingCheckpoint.RoundEnd,
                        new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                        false,
                        "SyntheticDefeatCancellationProbe",
                        id,
                        Array.Empty<TimingCheckpoint>()),
                    controllerId,
                    configuration.Participants.GetTeamForHero(controllerId).Side,
                    default,
                    new CardInstanceId($"{id}-source"),
                    new ActionDefinitionId($"{id}-action"),
                    1,
                    1);
                return effectId;
            }

            public PlacedToken PlaceHazardFor(HeroId owner, HexCoordinate coordinate)
            {
                var token = new PlacedToken(
                    new TokenInstanceId("defeat-consequence-hazard"),
                    SyntheticComponentDefinitions.CreateCatalog()
                        .LoadToken(SyntheticComponentDefinitions.HazardV4),
                    owner,
                    owner,
                    new CardInstanceId("defeat-consequence-source"),
                    coordinate);
                Board.CommitValidatedTokenPlacement(token);
                return token;
            }

            /// <summary>The Attack driven through the dispatcher, which is where the steps are readable.</summary>
            public HeroDefeatResolution DefeatTheDefender()
            {
                var outcome = Resolve();
                lastSteps = outcome.Steps;
                Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
                return outcome.HeroDefeat;
            }

            /// <summary>
            /// The same Attack driven through the round authority's own commands, which is what a
            /// player's turn actually does - and therefore the only path on which the turn advances,
            /// the timing checkpoints are applied, and p.13's game end is observed.
            ///
            /// Declining the Defense is what makes the Attack successful: p.13 says a hero that "has no
            /// cards in hand, is unable to defend, or chooses not to" is defeated.
            /// </summary>
            public RoundResolutionResult DefeatTheDefenderThroughTheRound()
            {
                OpenTheDefenseThroughTheRound();
                var defense = Round.CurrentTurnAuthority.PendingDefenseChoice;
                var result = Round.TryAnswerDefenseResponse(
                    DefenseResponseChoiceCommand.Decline(defense.ResolutionId, defense.ChoiceId));
                Assert.That(result.Accepted, Is.True);
                return result;
            }

            /// <summary>The same turn answered with the strongest Defense the defender holds.</summary>
            public RoundResolutionResult DefendThroughTheRound()
            {
                OpenTheDefenseThroughTheRound();
                var defense = Round.CurrentTurnAuthority.PendingDefenseChoice;
                var strongest = defense.LegalDefenseOffers
                    .OrderBy(candidate => PrintedValueQuery.CalculatePrintedValue(
                        candidate, configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                    .Last();
                var result = Round.TryAnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(
                        defense.ResolutionId, defense.ChoiceId, strongest.SourceCardInstanceId));
                Assert.That(result.Accepted, Is.True);
                return result;
            }

            private void OpenTheDefenseThroughTheRound()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        AttackerId,
                        AttackerCard.Id)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(
                        new ActionDefinitionId("primary-attack")));
                Assert.That(Round.TryStartAttack(
                    new ActionResolutionId("round-defeat"), offer).Accepted, Is.True);
                var target = Round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Assert.That(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    target.ResolutionId, target.ChoiceId, DefenderId)).Accepted, Is.True);
            }

            private AttackActionResolutionResult Resolve()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        AttackerId,
                        AttackerCard.Id)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(
                        new ActionDefinitionId("primary-attack")));
                var start = cardActions.StartTurnAttackResolution(
                    new ActionResolutionId("defeat-consequences"),
                    offer,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                var opened = cardActions.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        DefenderId),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                var ordered = opened.PendingDefenseChoice.LegalDefenseOffers
                    .OrderBy(candidate => PrintedValueQuery.CalculatePrintedValue(
                        candidate, configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                    .ToArray();
                cardActions.AnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(
                        opened.PendingDefenseChoice.ResolutionId,
                        opened.PendingDefenseChoice.ChoiceId,
                        ordered[0].SourceCardInstanceId),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                return cardActions.ResolveAttackOutcome(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
            }

            private static HexCoordinate[] Coordinates(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius) coordinates.Add(new HexCoordinate(q, r));
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
