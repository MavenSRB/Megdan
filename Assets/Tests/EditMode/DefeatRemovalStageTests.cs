using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6b. p.13's Defeat and p.19's standalone Remove ask one shared decision about whether
    /// a removal happens, so a minion whose removal is prevented stays on the board with its Defeat
    /// recorded and its reward already collected, whichever operation attempted it.
    ///
    /// The prevented arm is exercised on state built directly rather than through a played Attack, and
    /// that is a property of the engine rather than a shortcut. The only prevention the rules can
    /// currently express is p.18's immunity, and an Attack filters an immune minion out of its legal
    /// targets and revalidates that filter immediately before the removal runs, so no legal Attack
    /// target can be immune by the time its removal stage is reached. What milestone 6.6b guaranteed is
    /// that both removal paths would honour a prevention source on the day one existed.
    ///
    /// <para>Milestone 8.19c1 is that day. The general prevent-removal family p.13 names in its Bolster
    /// example is a declaration now - a source covering a minion, at a typed card price - so the cases
    /// below drive a real p.14 Defeat to the decision, pay it, decline it, refuse a stale or foreign
    /// answer, and round-trip the standing decision through a snapshot. They share the same shared
    /// decision the immunity cases above ask, which is the whole point of 6.6b's sharing: a paid
    /// prevention and p.18's immunity produce the same Defeat.</para>
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class DefeatRemovalStageTests
    {
        [Test]
        public void PreventedRemoval_RecordsTheDefeatTheRewardAndTheAttemptWithoutARemovedOccupancy()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var accounting = new HeroCoinLedger(fixture.Configuration.Participants);
            var outcome = MinionDefeatOutcome(fixture, "blue-target", 2);
            var reward = accounting.Award(outcome);

            var defeat = MinionDefeatResolution.Prevented(
                outcome,
                reward,
                new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                new DefeatRemovalResult(
                    outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                LanePushQualification.None);

            Assert.That(defeat.Reward.Accepted, Is.True,
                "p.13's Bolster example prevents the removal and still collects the coins, so the " +
                "reward stage stays accepted rather than being unwound by the stage after it.");
            Assert.That(defeat.RemovalResult.Kind, Is.EqualTo(DefeatRemovalResultKind.RemovalPrevented));
            Assert.That(defeat.RemovalAttempt.TargetId, Is.EqualTo(outcome.TargetId),
                "The attempt is recorded whether or not it succeeded; that is why 5.5 made it its own fact.");
            Assert.That(defeat.RemovedMinion, Is.Null,
                "Nothing left the board, so naming a removed occupancy would name a piece that never moved.");
            Assert.That(defeat.Facts, Is.EqualTo(new[]
            {
                MinionDefeatFactKind.DefeatRecorded,
                MinionDefeatFactKind.RewardAwarded,
                MinionDefeatFactKind.RemovalAttempted,
                MinionDefeatFactKind.RemovalPrevented
            }), "The outcome is stated distinctly rather than by a fact going missing, which would read " +
                "as a resolution cut short instead of a removal that was stopped.");
        }

        [Test]
        public void PreventedRemoval_ProjectsItsOwnResolutionEventRatherThanTheCompletedOne()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var defeat = PreventedDefeat(fixture, "blue-target");
            var resolution = MinionResolution(fixture, "blue-target");

            var events = defeat.Facts
                .Select(fact => CardActionResolutionEvent.MinionDefeatFact(resolution, defeat, fact))
                .ToArray();

            Assert.That(events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MinionDefeatRecorded,
                CardActionResolutionEventKind.MinionRewardAwarded,
                CardActionResolutionEventKind.MinionRemovalAttempted,
                CardActionResolutionEventKind.MinionRemovalPrevented
            }));
        }

        [Test]
        public void PreventedRemoval_KeepsTheRewardAndNeitherWithdrawsNorReawardsItOnASecondAttempt()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var accounting = new HeroCoinLedger(fixture.Configuration.Participants);
            var outcome = MinionDefeatOutcome(fixture, "blue-target", 2);
            var reward = accounting.Award(outcome);
            var attempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var prevented = new DefeatRemovalResult(
                outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented);

            var first = MinionDefeatResolution.Prevented(
                outcome, reward, attempt, prevented, LanePushQualification.None);
            Assert.That(accounting.GetCoins(fixture.AttackerId), Is.EqualTo(2),
                "p.13 awards the coins at the reward stage, which runs before the removal stage decides " +
                "anything, so a prevented removal cannot withdraw them.");

            var second = accounting.Award(outcome);

            Assert.That(second.Accepted, Is.False);
            Assert.That(second.RejectionReason,
                Is.EqualTo(DefeatRewardRejectionReason.OutcomeAlreadyAwarded));
            Assert.That(accounting.GetCoins(fixture.AttackerId), Is.EqualTo(2),
                "A later attempt on the same outcome re-awards nothing; 5.5's outcome identity is what " +
                "makes the reward idempotent across a removal that has to be tried more than once.");
            Assert.That(FactIndex(first, MinionDefeatFactKind.RewardAwarded),
                Is.LessThan(FactIndex(first, MinionDefeatFactKind.RemovalPrevented)),
                "p.13's ordering: the coins are collected, then the removal is attempted.");
        }

        [Test]
        public void CompletedRemoval_OrdersTheRewardBeforeItsOwnStage()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = AttackMinion(fixture, "blue-target", "ordering-attack").MinionDefeat;

            Assert.That(FactIndex(defeat, MinionDefeatFactKind.RewardAwarded),
                Is.LessThan(FactIndex(defeat, MinionDefeatFactKind.RemovalAttempted)));
            Assert.That(FactIndex(defeat, MinionDefeatFactKind.RemovalAttempted),
                Is.LessThan(FactIndex(defeat, MinionDefeatFactKind.RemovalCompleted)),
                "The same order in both directions: p.13 states the reward and the removal as separate " +
                "stages resolved in that sequence, so neither outcome may reorder them.");
        }

        [Test]
        public void PreventedRemoval_LeavesTheBoardTheReserveAndTheAttacksOwnCompletionAlone()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)),
                CreateMinion("blue-second", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var defeat = PreventedDefeat(fixture, "blue-target");

            Assert.That(
                fixture.Board.Minions.Select(minion => minion.MinionId.Value),
                Is.EquivalentTo(new[] { "blue-target", "blue-second" }),
                "A prevented removal changed nothing about occupancy: the piece is still in its space.");
            Assert.That(fixture.Board.FindUnitSupply(), Is.Empty,
                "The reserve is fed by miniatures that actually left the board, so a prevented removal " +
                "must not put one there and make it spawnable.");
            Assert.That(defeat.Facts, Has.Count.EqualTo(4),
                "The Defeat itself completed; only its removal stage answered that the piece stays.");
        }

        // -------------------------------------------------------------------------------------------
        // The shared decision
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSharedDecision_StopsARemovalTheRulesProtect_WhicheverOperationDeclaresIt()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-heavy", TeamSide.Blue, StandardMinionType.Heavy, new HexCoordinate(1, 0)),
                CreateMinion("blue-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));
            var protectedHeavy = BoardObjectReference.ForMinion(new MinionId("blue-heavy"));

            var defeatStage = RemovalStage(fixture, RuleActivity.Attack, LanePushCauseKind.Defeat);
            var removeStage = RemovalStage(fixture, RuleActivity.Skill, LanePushCauseKind.Removal);

            Assert.That(defeatStage.IsPrevented(protectedHeavy), Is.True);
            Assert.That(removeStage.IsPrevented(protectedHeavy), Is.True,
                "One decision, so a protected minion that survives a Skill's Remove cannot die to an " +
                "Attack's Defeat - the half-true rule this milestone exists to close.");
            Assert.That(
                defeatStage.IsPrevented(BoardObjectReference.ForMinion(new MinionId("blue-melee"))),
                Is.False,
                "A rule that stopped every removal would pass the two assertions above and protect nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.19c1: the general prevent-removal family this file's summary had been recording
        // as unimplemented since 6.6b, now that a source can declare it and pay for it
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ATotemSavesTheAttackedMinionAndIsRemovedWithoutAChoice()
        {
            var fixture = CreateFixture(
                CreateMinion(ShieldedMinionId, TeamSide.Blue,
                    StandardMinionType.Melee, new HexCoordinate(-1, 0)),
                CreateMinion("red-holder", TeamSide.Red,
                    StandardMinionType.Melee, new HexCoordinate(2, 0)));
            var defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            fixture.Board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("tali-save-probe"),
                TaliComponentContent.AncestralTotemDefinition,
                defender, defender, new CardInstanceId("tali-save-source"),
                new HexCoordinate(-2, 0)));
            var modifications = RuleModificationQuery.Build(
                new GuardsDigital.Core.Timing.ActiveEffectLifecycle(),
                fixture.Board,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None,
                RuleObject.ForHero(fixture.AttackerId),
                null,
                null);

            var result = AttackMinion(fixture, ShieldedMinionId,
                "tali-save-attack", modifications);

            Assert.That(result.MinionDefeat.IsReplaced, Is.True,
                "Totem replaces the would-be Defeat; it does not merely prevent removal.");
            Assert.That(result.MinionDefeat.Facts, Is.Empty);
            Assert.That(fixture.Authority.CoinLedger.GetCoins(fixture.AttackerId), Is.Zero);
            Assert.That(fixture.Authority.PendingRemovalPrevention, Is.Null,
                "Tali's printed mandatory save asks no player.");
            Assert.That(fixture.Board.Minions.Any(minion =>
                minion.MinionId.Value == ShieldedMinionId), Is.True);
            Assert.That(fixture.Board.PlacedTokens, Is.Empty,
                "The exact source Totem pays for the saved minion.");
        }

        [Test]
        public void AShieldedMinion_StopsThePageAfterItsRewardAndBeforeItsRemoval()
        {
            var fixture = CreateShieldedFixture();
            var coinsBefore = CoinsOf(fixture);

            var result = AttackMinion(fixture, ShieldedMinionId, "shielded-attack", fixture.Shield);

            Assert.That(result.Accepted, Is.True);
            var standing = fixture.Authority.PendingRemovalPrevention;
            Assert.That(standing, Is.Not.Null,
                "A source offering to stop the removal stops rulebook p.14 where p.13 puts the " +
                "decision: after the Defeat and its reward, and before the miniature leaves.");
            Assert.That(standing.DecidingHeroId.Value,
                Is.EqualTo(StagedBoardParticipants.BlueHeroIdValue),
                "p.10 gives the decision to the hero whose source is offering, which is the " +
                "defender's - not the attacker who caused the Defeat.");
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.RemovalPreventionRequested),
                Is.True,
                "The stream says where the page stopped rather than simply stopping.");
            Assert.That(
                fixture.Board.Minions.Any(minion => minion.MinionId.Value == ShieldedMinionId),
                Is.True,
                "The miniature is still on the board while the decision stands.");
            Assert.That(CoinsOf(fixture), Is.Not.EqualTo(coinsBefore),
                "p.13's Bolster example collects the coins in the same breath as preventing the " +
                "removal, so the reward is already paid when the question is asked.");
        }

        [Test]
        public void APaidPrevention_KeepsTheMiniatureDiscardsTheCardAndPublishesTheOutcome()
        {
            var fixture = CreateShieldedFixture();
            AttackMinion(fixture, ShieldedMinionId, "shielded-attack", fixture.Shield);
            var standing = fixture.Authority.PendingRemovalPrevention;
            var option = fixture.Authority.DescribeRemovalPreventionOptions(
                fixture.Configuration.CardDefinitions, fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, fixture.Shield).Single();
            var payment = option.PayableCards[0];

            var answered = fixture.Authority.AnswerRemovalPrevention(
                RemovalPreventionCommand.Accept(
                    standing.RequestId, standing.DecidingHeroId, option.SourceId, payment),
                fixture.Boundary(),
                fixture.Shield);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingRemovalPrevention, Is.Null,
                "One decision per outcome, closed by one answer.");
            Assert.That(
                fixture.Board.Minions.Any(minion => minion.MinionId.Value == ShieldedMinionId),
                Is.True,
                "The price bought exactly what the card says it buys: the miniature stays.");
            Assert.That(
                fixture.Configuration.CardInstances
                    .Single(card => card.Id.Equals(payment)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The typed cost is paid, not merely named.");
            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.RemovalPreventionPaid),
                Is.True);
            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.MinionRemovalPrevented),
                Is.True,
                "The outcome published is p.13's own, which is what makes a paid prevention " +
                "indistinguishable downstream from p.18's immunity stopping the same removal.");
            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.MinionRewardAwarded),
                Is.True,
                "And the Defeat and its reward are retained rather than unwound.");
            Assert.That(fixture.Authority.PendingImmediateLanePush, Is.Null,
                "p.8's mid-turn push is owed for a side emptied out of the Battle Zone, and the " +
                "shielded minion is still standing in it. Milestone 6.9 records the Defeat's own " +
                "cause either way - the page's first sentence is answered by the Defeat - but the " +
                "request is derived from the board, and the board still has both sides in the zone.");
        }

        [Test]
        public void ADeclinedPrevention_LetsTheRemovalHappenAndFinishesThePage()
        {
            var fixture = CreateShieldedFixture();
            AttackMinion(fixture, ShieldedMinionId, "shielded-attack", fixture.Shield);
            var standing = fixture.Authority.PendingRemovalPrevention;

            var answered = fixture.Authority.AnswerRemovalPrevention(
                RemovalPreventionCommand.Decline(standing.RequestId, standing.DecidingHeroId),
                fixture.Boundary(),
                fixture.Shield);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(
                fixture.Board.Minions.Any(minion => minion.MinionId.Value == ShieldedMinionId),
                Is.False,
                "Declining is a legal answer, and the removal that was going to happen happens.");
            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.MinionRemovalCompleted),
                Is.True);
            Assert.That(
                answered.Events.Any(value =>
                    value.Kind == CardActionResolutionEventKind.RemovalPreventionPaid),
                Is.False,
                "Nothing was paid, so nothing is reported as paid.");
            Assert.That(fixture.Authority.PendingImmediateLanePush, Is.Not.Null,
                "The declined removal emptied Blue's side out of the Battle Zone, so p.8's mid-turn " +
                "push is owed - the immediate consequence a prevention would have stopped.");
        }

        [Test]
        public void AStandingPreventionDecision_RefusesAStaleOrForeignAnswer()
        {
            var fixture = CreateShieldedFixture();
            AttackMinion(fixture, ShieldedMinionId, "shielded-attack", fixture.Shield);
            var standing = fixture.Authority.PendingRemovalPrevention;

            Assert.That(
                fixture.Authority.AnswerRemovalPrevention(
                    RemovalPreventionCommand.Decline("not-this-decision", standing.DecidingHeroId),
                    fixture.Boundary(), fixture.Shield).RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));

            Assert.That(
                fixture.Authority.AnswerRemovalPrevention(
                    RemovalPreventionCommand.Decline(standing.RequestId, fixture.AttackerId),
                    fixture.Boundary(), fixture.Shield).RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.WrongChooser),
                "The attacker does not answer for the source that is trying to keep its own minion.");

            Assert.That(fixture.Authority.PendingRemovalPrevention, Is.Not.Null,
                "A refused answer leaves the decision exactly as it was, so it is still there to be " +
                "made.");
        }

        [Test]
        public void AStandingPreventionDecision_SurvivesASnapshotRoundTripByteIdentically()
        {
            var fixture = CreateShieldedFixture();
            AttackMinion(fixture, ShieldedMinionId, "shielded-attack", fixture.Shield);
            var standing = fixture.Authority.PendingRemovalPrevention;
            Assert.That(standing, Is.Not.Null);

            var round = new RoundResolutionAuthority(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(fixture.Authority));

            // The card-action continuation is written inside the turn that owns the dispatcher, so a
            // round with no turn at all has nowhere to put one - which is true of every continuation
            // this engine persists, not of this decision. Committing both cards gives the round its
            // turn, and the turn hands the same dispatcher to the writer.
            foreach (var hero in fixture.Configuration.Participants.Heroes)
            {
                var card = fixture.Configuration.CardInstances.First(value =>
                    value.OwnerId.Equals(hero.Id) && value.State.Location == CardLocation.Hand);
                Assert.That(round.TryCommitCard(hero.Id, card.Id).Accepted, Is.True);
            }

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Map },
                fixture.Minions);

            var restoredDecision = restored.Round.PendingRemovalPrevention;
            Assert.That(restoredDecision, Is.Not.Null,
                "A stopped decision that vanished on restore would remove a miniature its " +
                "controller had been offered the chance to keep - which is why v26 is rejected " +
                "rather than migrated.");
            Assert.That(restoredDecision.RequestId, Is.EqualTo(standing.RequestId));
            Assert.That(restoredDecision.Origin, Is.EqualTo(standing.Origin));
            Assert.That(restoredDecision.DecidingHeroId, Is.EqualTo(standing.DecidingHeroId));
            Assert.That(
                restoredDecision.RewardOperations.Select(value => value.BalanceAfter),
                Is.EqualTo(standing.RewardOperations.Select(value => value.BalanceAfter)),
                "The coin operations cross the wire because they record balances either side of a " +
                "payment that has already happened, and re-running the award would be refused.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "Byte-identical, like every other continuation this engine persists.");
        }

        [Test]
        public void ARemovalStage_RefusesToExistWithoutDeclaringWhichCauseItWouldRecord()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RemovalStage(fixture, RuleActivity.Attack, LanePushCauseKind.None));
        }

        // -------------------------------------------------------------------------------------------
        // p.8's second cause
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DefeatingTheLastBattleZoneMinionOfASide_RecordsPageEightsDefeatCause()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = AttackMinion(fixture, "blue-target", "lane-attack").MinionDefeat;

            Assert.That(defeat.LanePush.Qualifies, Is.True);
            Assert.That(defeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.Defeat),
                "p.8 names its two causes separately - 'It must be either defeated, or removed' - so a " +
                "later milestone reading the record is entitled to know which sentence it answers.");
            Assert.That(defeat.LanePush.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(defeat.LanePush.BattleZoneId, Is.EqualTo(new ZoneId("battle-zone")));
        }

        [Test]
        public void DefeatingOneOfTwoBattleZoneMinions_RecordsNoCause()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)),
                CreateMinion("blue-second", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var defeat = AttackMinion(fixture, "blue-target", "survivor-attack").MinionDefeat;

            Assert.That(defeat.LanePush.Qualifies, Is.False,
                "p.8 asks about the last remaining enemy minion; one is still standing.");
        }

        [Test]
        public void APreventedDefeatRemoval_RecordsNoCauseEvenWhenTheZoneWouldHaveBeenEmptied()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = PreventedDefeat(fixture, "blue-target");

            Assert.That(defeat.LanePush.Qualifies, Is.False,
                "The question is asked only after a piece has left the board, exactly as it is for a " +
                "prevented Remove; the zone is no emptier than it was.");
            Assert.That(defeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.None));
        }

        [Test]
        public void TheDerivedCause_ReachesNoPayload()
        {
            var minion = CreateMinion(
                "blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0));
            var fixture = CreateFixture(minion);
            var defeat = AttackMinion(fixture, "blue-target", "snapshot-attack").MinionDefeat;
            Assert.That(defeat.LanePush.Qualifies, Is.True, "The case is only evidence if a cause exists.");

            var round = new RoundResolutionAuthority(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(fixture.Authority));
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Map },
                new[] { minion });

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(57),
                "Nothing about the removal decision is stored, so it carried no schema move of its " +
                "own. The version has since moved twenty-seven times, none of them for it: " +
                "milestone 8.53 for the victory screen's accumulated per-hero scoreboard, and before " +
                "that milestone 6.7 for " +
                "a repeated action instance owed across a paused correction, milestone 6.9 for the " +
                "mid-turn Lane-Push request a completed action can leave behind, milestone 6.10 for " +
                "p.8's Battle Zone itself, milestone 6.10a for a paused system transition, " +
                "milestone 6.11 for p.8's Wave counters and the push that spends them, " +
                "milestone 7.1 for every hero's current Level, milestone 7.2 for p.16's items, " +
                "milestone 7.2a for the four dashboard item slots every hero declares, " +
                "milestone 7.4 for every team's remaining Life counters, and milestone 8.6 for how " +
                "far through a bounded repeat an instance is and the target it inherited, " +
                "milestone 8.10 for an Attack paused inside p.14's card text and the unit a repeated " +
                "instance may not take, milestone 8.11 for what p.14's Block did, and milestone " +
                "8.19a for the outer pre-action continuation, the before-action triggers a turn has " +
                "spent, and the space an Attack's target stood on before p.14's consequences, and " +
                "milestone 8.19a1 for current-round typed hero-Defeat history, milestone 8.19c1 for " +
                "three decisions a player has been asked and has not answered, and milestone 8.20 " +
                "for the hero an Active-effect occurrence bound with the word `this`, and milestone 8.21 " +
                "for the eleven capability contracts Brogan's printed faces are the first to reach, " +
                "which every v28 payload names a shorter list than, and milestone 8.22 for the five " +
                "Sabina's printed faces are the first to reach, which every v29 payload names a " +
                "shorter list than in turn, and milestone 8.22 again for the rest of p.14 owed " +
                "while a trigger set off by a Push inside an Attack's card text is standing, which " +
                "no v30 payload can describe because that altitude could not set one off, and " +
                "milestone 8.23 for the completed basic action a permanent after-action ability may " +
                "still be asked to repeat, which no v31 payload can describe because no ability in " +
                "an 8.22 build stands outside the action it repeats, and milestone 8.23 again for where " +
                "the attacker stood when an Attack started, which no v32 payload records because no " +
                "earlier face asks at one of p.14's moments about what happened at another, and " +
                "milestone 8.24 for the five capability contracts Xargatha's printed faces are the " +
                "first to reach - her three petrification disks and her two end-of-round sources - " +
                "which every v33 payload names a shorter list than, so one is refused rather than " +
                "resolved against contracts its own capture never had, and milestone 8.35g for the " +
                "finished turn a match is holding until its own player releases it, which no v34 " +
                "payload records because a v34 build handed the turn on inside the command that " +
                "finished it, so a restored v34 capture would advance by itself, and milestone " +
                "8.35u for p.15's simultaneous leveling, which writes one item, chooser and option " +
                "list per hero still choosing where a v35 payload wrote exactly one of each, and " +
                "milestone 8.35z21 for the two repeat instances that have been decided and not yet " +
                "started, which no v36 payload records because in a v36 build one was always started " +
                "inside the command that decided it - p.8's mid-turn push now drains at the end of " +
                "each action instance, and a push stopped on the sidebar's Place holds a decided " +
                "repeat across a player's answer, and milestone 8.35z27 for the modal " +
                "Attack a \"choose one, or both\" opened and has not finished, which no v37 payload " +
                "records because no v37 build could run more than one complete p.14 order inside one " +
                "action. " +
                "The decision is still re-derived from the state as it then stands." +
                "Milestone 8.71a moved it once more, to v44, for Hanu the "
                + "Trickster's rewritten printed Initiative: a sparse card-keyed list in "
                + "the card section, which is the first authoritative fact this build "
                + "records about a CARD rather than about a hero, a minion or a token.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "A restored match re-derives the decision from the state as it then stands, for the " +
                "reason every other derived judgement is re-derived rather than carried.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static int FactIndex(MinionDefeatResolution defeat, MinionDefeatFactKind fact)
        {
            for (var index = 0; index < defeat.Facts.Count; index++)
            {
                if (defeat.Facts[index] == fact) return index;
            }

            return -1;
        }

        private static MinionDefeatResolution PreventedDefeat(Fixture fixture, string minionId)
        {
            var outcome = MinionDefeatOutcome(fixture, minionId, 2);
            var accounting = new HeroCoinLedger(fixture.Configuration.Participants);
            return MinionDefeatResolution.Prevented(
                outcome,
                accounting.Award(outcome),
                new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                new DefeatRemovalResult(
                    outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                LanePushQualification.None);
        }

        private static DefeatOutcome MinionDefeatOutcome(Fixture fixture, string minionId, int coins) =>
            new DefeatOutcome(
                new DefeatOutcomeId($"prevented-{minionId}:minion-defeat"),
                new DefeatTargetId(minionId),
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                fixture.AttackerId,
                coins,
                0);

        private static PendingAttackResolution MinionResolution(Fixture fixture, string minionId) =>
            new PendingAttackResolution(
                new ActionResolutionId($"prevented-{minionId}"),
                fixture.AttackOffer.SourceCardInstanceId,
                fixture.AttackOffer.SourceDefinitionReference,
                fixture.AttackOffer.ActionDefinitionId,
                fixture.AttackerId,
                fixture.AttackOffer.InvocationContext,
                new ActionResolutionStepId("minion-defeat"),
                AttackResolutionPhase.ResolveMinionDefeat,
                AttackTarget.Minion(new MinionId(minionId)),
                null);

        private static RemovalStageDecision RemovalStage(
            Fixture fixture, RuleActivity activity, LanePushCauseKind cause) =>
            new RemovalStageDecision(
                activity,
                RuleObject.ForHero(fixture.AttackerId),
                fixture.Board,
                fixture.Board,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty,
                cause);

        private static AttackActionResolutionResult AttackMinion(
            Fixture fixture,
            string minionId,
            string resolutionId,
            RuleModificationSet modifications = null)
        {
            var rules = modifications ?? RuleModificationSet.Empty;
            var opened = fixture.Authority.StartAttackResolution(
                new ActionResolutionId(resolutionId),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, rules);
            var result = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId, new MinionId(minionId)),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, rules);
            Assert.That(result.Accepted, Is.True);
            return result;
        }

        /// <summary>
        /// Milestone 8.19c1. The minion the defender's own shield covers: adjacent to the attacker so
        /// the Attack is legal, and inside the disk the shield draws around the defender so the source
        /// reaches it. Both distances are the fixture's whole geometry.
        /// </summary>
        private const string ShieldedMinionId = "blue-shielded";

        private static Fixture CreateShieldedFixture()
        {
            var fixture = CreateFixture(
                CreateMinion(
                    ShieldedMinionId, TeamSide.Blue, StandardMinionType.Melee,
                    new HexCoordinate(-1, 0)),
                // Red holds the Battle Zone too, so p.8's question has a real answer either way: with
                // the shielded minion standing both sides hold and no push is owed, and with it removed
                // Blue's side is empty and one is. A fixture with only Blue's minion would make both
                // sides empty after the removal, which p.8 asks nothing about.
                CreateMinion(
                    "red-holder", TeamSide.Red, StandardMinionType.Melee, new HexCoordinate(2, 0)));
            var defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var lifecycle = new GuardsDigital.Core.Timing.ActiveEffectLifecycle();
            lifecycle.Create(
                new GuardsDigital.Core.Timing.ActiveEffectId("shield-probe"),
                GuardsDigital.Core.Effects.Synthetic.SyntheticPausingSourceCapabilities
                    .ShieldFriendlyMinionsDefinition,
                defender,
                TeamSide.Blue,
                default,
                new CardInstanceId("shield-probe-card"),
                new ActionDefinitionId("shield-probe-action"),
                1,
                1);
            fixture.Shield = RuleModificationQuery.Build(
                lifecycle,
                fixture.Board,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None,
                RuleObject.ForHero(fixture.AttackerId),
                null,
                null);
            return fixture;
        }

        private static IReadOnlyList<string> CoinsOf(Fixture fixture) =>
            (fixture.Authority.CoinLedger?.CoinBalances ??
                Array.Empty<KeyValuePair<HeroId, int>>())
            .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
            .Select(entry => entry.Key.Value + ":" + entry.Value)
            .ToArray();

        private static Fixture CreateFixture(params QuickGameMinionSetupState[] minions)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var attacker = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var map = CreateBattleZoneMap();
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(attacker, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defender, new HexCoordinate(-3, 0))
                },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minions);
            var setupMinions = minions;
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attacker,
                    ActionOfferWindow.NormalPlay)
                .Single(value => value.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new Fixture(configuration, attacker, map, board,
                new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board), offer,
                setupMinions);
        }

        private static BoardMapDefinition CreateBattleZoneMap()
        {
            var mapId = new MapId("defeat-removal-map");
            var zone = new BoardMapZone(new ZoneId("battle-zone"), "Battle Zone", "lane", null, false, true);
            var spaces = CreateRadiusThreeCoordinates().Select(coordinate =>
                new BoardMapSpace(new SpaceId($"q{coordinate.Q}_r{coordinate.R}"), coordinate,
                    BoardMapSpaceKind.Playable, zone.Id));
            return new BoardMapDefinition(mapId, 1, "Defeat Removal Map", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static QuickGameMinionSetupState CreateMinion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            var mapId = new MapId("defeat-removal-map");
            var teamId = new TeamId(side == TeamSide.Red
                ? StagedBoardParticipants.RedTeamIdValue
                : StagedBoardParticipants.BlueTeamIdValue);
            return new QuickGameMinionSetupState(
                new MinionId(id), teamId, side, type,
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(mapId, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);
        }

        private static IEnumerable<HexCoordinate> CreateRadiusThreeCoordinates()
        {
            for (var q = -3; q <= 3; q++)
            {
                var minimumR = Math.Max(-3, -q - 3);
                var maximumR = Math.Min(3, -q + 3);
                for (var r = minimumR; r <= maximumR; r++) yield return new HexCoordinate(q, r);
            }
        }

        private sealed class Fixture
        {
            public Fixture(MatchConfiguration configuration, HeroId attackerId,
                BoardMapDefinition map, AdjacentMoveAuthority board,
                CardActionResolutionAuthority authority, ActionOffer attackOffer,
                IReadOnlyList<QuickGameMinionSetupState> minions)
            {
                Configuration = configuration; AttackerId = attackerId; Map = map;
                Board = board; Authority = authority; AttackOffer = attackOffer; Minions = minions;
            }

            public MatchConfiguration Configuration { get; }
            public HeroId AttackerId { get; }
            public BoardMapDefinition Map { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer AttackOffer { get; }
            public IReadOnlyList<QuickGameMinionSetupState> Minions { get; }

            /// <summary>Milestone 8.19c1's live rules carrying the defender's own shield, or null.</summary>
            public RuleModificationSet Shield { get; set; }

            public ActionBoundaryContext Boundary() =>
                new ActionBoundaryContext(
                    Configuration.CardDefinitions,
                    Configuration.CardInstances,
                    Configuration.Participants,
                    null,
                    new TieBreakerState(TeamSide.Red),
                    1,
                    1);
        }
    }
}
