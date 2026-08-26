using System;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroCoinLedgerTests
    {
        [Test]
        public void RewardDeferredHeroDefeatCannotAwardInventedAmounts()
        {
            var participants = StagedBoardParticipants.Create();
            var accounting = new HeroCoinLedger(participants);
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId("hero-defeat-awaiting-progression"),
                new DefeatTargetId(StagedBoardParticipants.BlueHeroIdValue),
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                new HeroId(StagedBoardParticipants.RedHeroIdValue));

            var result = accounting.Award(outcome);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(DefeatRewardRejectionReason.RewardAmountsUnavailable));
            Assert.That(result.Operations, Is.Empty);
            Assert.That(accounting.CoinBalances, Has.All.Matches<System.Collections.Generic.KeyValuePair<HeroId, int>>(
                value => value.Value == 0));
        }

        [Test]
        public void OneVsOneDefeatAwardsTheActingHeroAndNotTheEnemy()
        {
            var participants = StagedBoardParticipants.Create();
            var accounting = new HeroCoinLedger(participants);
            var result = accounting.Award(Outcome(
                "defeat-1",
                StagedBoardParticipants.RedHeroIdValue,
                StagedBoardParticipants.BlueTeamIdValue,
                3,
                1));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            Assert.That(result.Operations[0].RecipientHeroId,
                Is.EqualTo(new HeroId(StagedBoardParticipants.RedHeroIdValue)));
            Assert.That(result.Operations[0].RecipientTeamId,
                Is.EqualTo(new TeamId(StagedBoardParticipants.RedTeamIdValue)));
            Assert.That(result.Operations[0].AwardKind, Is.EqualTo(CoinAwardKind.Defeat));
            Assert.That(result.Operations[0].Amount, Is.EqualTo(3));
            Assert.That(result.Operations[0].BalanceBefore, Is.Zero);
            Assert.That(result.Operations[0].BalanceAfter, Is.EqualTo(3));
            Assert.That(result.Operations[0].FailureBehavior,
                Is.EqualTo(ResourceOperationFailureBehavior.RejectWithoutMutation));
            Assert.That(accounting.GetCoins(new HeroId(StagedBoardParticipants.RedHeroIdValue)),
                Is.EqualTo(3));
            Assert.That(accounting.GetCoins(new HeroId(StagedBoardParticipants.BlueHeroIdValue)),
                Is.Zero);
        }

        [Test]
        public void FriendlyHeroesReceiveAssistCoinsInStableParticipantOrder()
        {
            var participants = CreateThreeVsThreeParticipants();
            var accounting = new HeroCoinLedger(participants);
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId("defeat-team"),
                new DefeatTargetId("blue-target"),
                new TeamId("blue"),
                new HeroId("red-2"),
                6,
                2);

            var result = accounting.Award(outcome);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Operations.Select(operation => operation.RecipientHeroId.Value),
                Is.EqualTo(new[] { "red-2", "red-1", "red-3" }));
            Assert.That(result.Operations.Select(operation => operation.AwardKind),
                Is.EqualTo(new[] { CoinAwardKind.Defeat, CoinAwardKind.Assist, CoinAwardKind.Assist }));
            Assert.That(result.Operations.Select(operation => operation.Amount),
                Is.EqualTo(new[] { 6, 2, 2 }));
            Assert.That(accounting.GetCoins(new HeroId("red-1")), Is.EqualTo(2));
            Assert.That(accounting.GetCoins(new HeroId("red-2")), Is.EqualTo(6));
            Assert.That(accounting.GetCoins(new HeroId("red-3")), Is.EqualTo(2));
            Assert.That(accounting.GetCoins(new HeroId("blue-1")), Is.Zero);
        }

        [Test]
        public void ZeroAssistAmountProducesOnlyTheDefeatOperation()
        {
            var accounting = new HeroCoinLedger(CreateThreeVsThreeParticipants());

            var result = accounting.Award(new DefeatOutcome(
                new DefeatOutcomeId("no-assist"),
                new DefeatTargetId("blue-minion"),
                new TeamId("blue"),
                new HeroId("red-1"),
                2,
                0));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            Assert.That(result.Operations[0].AwardKind, Is.EqualTo(CoinAwardKind.Defeat));
            Assert.That(accounting.GetCoins(new HeroId("red-2")), Is.Zero);
            Assert.That(accounting.GetCoins(new HeroId("red-3")), Is.Zero);
        }

        [Test]
        public void PreventedRemovalDoesNotEraseOrDuplicateAnAwardedDefeatReward()
        {
            var participants = StagedBoardParticipants.Create();
            var accounting = new HeroCoinLedger(participants);
            var outcome = Outcome(
                "prevented-removal",
                StagedBoardParticipants.RedHeroIdValue,
                StagedBoardParticipants.BlueTeamIdValue,
                4,
                0);

            var award = accounting.Award(outcome);
            var removal = new DefeatRemovalResult(
                outcome.Id,
                outcome.TargetId,
                DefeatRemovalResultKind.RemovalPrevented);
            var duplicate = accounting.Award(outcome);

            Assert.That(award.Accepted, Is.True);
            Assert.That(removal.Kind, Is.EqualTo(DefeatRemovalResultKind.RemovalPrevented));
            Assert.That(removal.DefeatOutcomeId, Is.EqualTo(outcome.Id));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.RejectionReason, Is.EqualTo(DefeatRewardRejectionReason.OutcomeAlreadyAwarded));
            Assert.That(duplicate.Operations, Is.Empty);
            Assert.That(accounting.GetCoins(outcome.DefeatingHeroId), Is.EqualTo(4));
        }

        [Test]
        public void SeparateDefeatOutcomesAccumulateDeterministically()
        {
            var accounting = new HeroCoinLedger(StagedBoardParticipants.Create());
            var actorId = new HeroId(StagedBoardParticipants.RedHeroIdValue);

            var first = accounting.Award(Outcome(
                "first",
                actorId.Value,
                StagedBoardParticipants.BlueTeamIdValue,
                2,
                0));
            var second = accounting.Award(Outcome(
                "second",
                actorId.Value,
                StagedBoardParticipants.BlueTeamIdValue,
                4,
                0));

            Assert.That(first.Operations.Single().BalanceAfter, Is.EqualTo(2));
            Assert.That(second.Operations.Single().BalanceBefore, Is.EqualTo(2));
            Assert.That(second.Operations.Single().BalanceAfter, Is.EqualTo(6));
            Assert.That(accounting.GetCoins(actorId), Is.EqualTo(6));
        }

        [Test]
        public void OverflowIsRejectedAtomicallyAndDoesNotConsumeTheOutcomeId()
        {
            var accounting = new HeroCoinLedger(StagedBoardParticipants.Create());
            var actorId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var first = Outcome(
                "maximum",
                actorId.Value,
                StagedBoardParticipants.BlueTeamIdValue,
                int.MaxValue,
                0);
            var overflow = Outcome(
                "overflow",
                actorId.Value,
                StagedBoardParticipants.BlueTeamIdValue,
                1,
                0);

            Assert.That(accounting.Award(first).Accepted, Is.True);
            var rejected = accounting.Award(overflow);
            var repeated = accounting.Award(overflow);

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason, Is.EqualTo(DefeatRewardRejectionReason.CoinBalanceOverflow));
            Assert.That(rejected.Operations, Is.Empty);
            Assert.That(repeated.RejectionReason, Is.EqualTo(DefeatRewardRejectionReason.CoinBalanceOverflow));
            Assert.That(accounting.GetCoins(actorId), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void FriendlyDefeatIsRejectedWithoutMutation()
        {
            var accounting = new HeroCoinLedger(StagedBoardParticipants.Create());
            var actorId = new HeroId(StagedBoardParticipants.RedHeroIdValue);

            var result = accounting.Award(Outcome(
                "friendly",
                actorId.Value,
                StagedBoardParticipants.RedTeamIdValue,
                2,
                1));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(DefeatRewardRejectionReason.FriendlyTarget));
            Assert.That(result.Operations, Is.Empty);
            Assert.That(accounting.GetCoins(actorId), Is.Zero);
        }

        [Test]
        public void UnknownActorOrTeamIsRejectedWithoutMutation()
        {
            var accounting = new HeroCoinLedger(StagedBoardParticipants.Create());
            var knownActor = new HeroId(StagedBoardParticipants.RedHeroIdValue);

            var unknownActor = accounting.Award(Outcome(
                "unknown-actor",
                "missing-hero",
                StagedBoardParticipants.BlueTeamIdValue,
                2,
                0));
            var unknownTeam = accounting.Award(Outcome(
                "unknown-team",
                knownActor.Value,
                "missing-team",
                2,
                0));

            Assert.That(unknownActor.RejectionReason,
                Is.EqualTo(DefeatRewardRejectionReason.DefeatingHeroNotInMatch));
            Assert.That(unknownTeam.RejectionReason,
                Is.EqualTo(DefeatRewardRejectionReason.DefeatedTeamNotInMatch));
            Assert.That(unknownActor.Operations, Is.Empty);
            Assert.That(unknownTeam.Operations, Is.Empty);
            Assert.That(accounting.GetCoins(knownActor), Is.Zero);
        }

        [Test]
        public void OutcomeRejectsNonPositiveDefeatOrNegativeAssistAmounts()
        {
            Assert.That(
                () => Outcome("zero", "actor", "enemy", 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => Outcome("negative-assist", "actor", "enemy", 1, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static DefeatOutcome Outcome(
            string outcomeId,
            string actorId,
            string defeatedTeamId,
            int defeatCoins,
            int assistCoins)
        {
            return new DefeatOutcome(
                new DefeatOutcomeId(outcomeId),
                new DefeatTargetId($"{outcomeId}-target"),
                new TeamId(defeatedTeamId),
                new HeroId(actorId),
                defeatCoins,
                assistCoins);
        }

        private static MatchParticipants CreateThreeVsThreeParticipants()
        {
            var red = new TeamId("red");
            var blue = new TeamId("blue");
            var players = Enumerable.Range(1, 3)
                .Select(index => new Player(new PlayerId($"red-player-{index}"), red))
                .Concat(Enumerable.Range(1, 3)
                    .Select(index => new Player(new PlayerId($"blue-player-{index}"), blue)))
                .ToArray();
            var heroes = Enumerable.Range(1, 3)
                .Select(index => new Hero(new HeroId($"red-{index}"), new PlayerId($"red-player-{index}"), HeroDashboard.RulebookLayout))
                .Concat(Enumerable.Range(1, 3)
                    .Select(index => new Hero(new HeroId($"blue-{index}"), new PlayerId($"blue-player-{index}"), HeroDashboard.RulebookLayout)))
                .ToArray();

            return new MatchParticipants(
                new[] { new Team(red, TeamSide.Red), new Team(blue, TeamSide.Blue) },
                players,
                heroes);
        }
    }
}
