using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19a1's two source-grounded history readings: played-card identity comes from the
    /// existing current-turn slot, while a completed hero Defeat has one new round-owned record.
    /// </summary>
    public sealed class RoundRuleHistoryTests
    {
        [Test]
        public void PlayedAttackCardThisTurn_ReadsTheCommittedPrimaryCard_NotTheActionPerformed()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var definitions = configuration.CardDefinitions;
            var cards = configuration.CardInstances;
            var redHero = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var redAttack = cards.First(card => card.OwnerId.Equals(redHero) &&
                HasPrintedPrimary(definitions, card, ActionKind.Attack));
            var blueNonAttack = cards.First(card => card.OwnerId.Equals(blueHero) &&
                !HasPrintedPrimary(definitions, card, ActionKind.Attack));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(redAttack), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(blueNonAttack), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redAttack), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(blueNonAttack), Is.True);
            var slot = new RoundTurnSlot(
                2,
                3,
                new[]
                {
                    new TurnCardAssignment(redHero, redAttack.Id, false),
                    new TurnCardAssignment(blueHero, blueNonAttack.Id, false)
                },
                cards,
                configuration.Participants);

            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(
                redHero, 2, 3, new[] { slot }, definitions, cards), Is.True,
                "The slot records the card p.10 says was played even before that hero acts.");
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(
                blueHero, 2, 3, new[] { slot }, definitions, cards), Is.False);
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(
                redHero, 2, 2, new[] { slot }, definitions, cards), Is.False,
                "A card played in another turn is not a this-turn fact.");

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(redAttack), Is.True);
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(
                redHero, 2, 3, new[] { slot }, definitions, cards), Is.True,
                "Hold, a Secondary action, and p.13's Defeat-before-acting path all resolve the " +
                "committed card without changing the slot or its printed-primary classification.");

            var passSlot = new RoundTurnSlot(
                2,
                4,
                new[]
                {
                    new TurnCardAssignment(redHero, null, true),
                    new TurnCardAssignment(blueHero, null, true)
                },
                cards,
                configuration.Participants);
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(
                redHero, 2, 4, new[] { slot, passSlot }, definitions, cards), Is.False);
        }

        [Test]
        public void HeroDefeatHistory_IsTypedOrderedPersistableAndRoundScoped()
        {
            var participants = StagedBoardParticipants.Create();
            var red = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blue = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var history = new RoundHeroDefeatHistory(participants);
            var outcome = Outcome("history-defeat-1", blue, red, participants);
            var secondOutcome = Outcome("history-defeat-2", red, blue, participants);

            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(history), Is.False);
            history.Record(outcome);
            history.Record(secondOutcome);
            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(history), Is.True);
            Assert.That(history.Entries.Select(entry =>
                    (entry.OutcomeId.Value, entry.DefeatedHeroId.Value)),
                Is.EqualTo(new[]
                {
                    (outcome.Id.Value, blue.Value),
                    (secondOutcome.Id.Value, red.Value)
                }), "Resolution order is authoritative and survives capture unchanged.");

            var restored = RoundHeroDefeatHistory.Restore(
                participants, history.CaptureRestoreState());
            Assert.That(restored.Entries.Select(entry =>
                    (entry.OutcomeId.Value, entry.DefeatedHeroId.Value)),
                Is.EqualTo(history.Entries.Select(entry =>
                    (entry.OutcomeId.Value, entry.DefeatedHeroId.Value))));
            Assert.That(() => restored.Record(outcome), Throws.InstanceOf<InvalidOperationException>(),
                "One p.13 outcome cannot become two history facts.");

            restored.BeginRound();
            Assert.That(restored.Entries, Is.Empty);
            Assert.That(HeroDefeatedThisRoundQuery.IsSatisfied(restored), Is.False);
        }

        [Test]
        public void HeroDefeatHistoryRestore_RejectsUnknownHeroesAndDuplicateOutcomes()
        {
            var participants = StagedBoardParticipants.Create();
            var red = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blue = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var outcomeId = new DefeatOutcomeId("history-defeat-restore");

            Assert.That(
                () => RoundHeroDefeatHistory.Restore(
                    participants,
                    new RoundHeroDefeatHistoryRestoreState(new[]
                    {
                        new HeroDefeatHistoryEntry(outcomeId, new HeroId("not-a-participant"))
                    })),
                Throws.InstanceOf<ArgumentException>());

            Assert.That(
                () => RoundHeroDefeatHistory.Restore(
                    participants,
                    new RoundHeroDefeatHistoryRestoreState(new[]
                    {
                        new HeroDefeatHistoryEntry(outcomeId, blue),
                        new HeroDefeatHistoryEntry(outcomeId, red)
                    })),
                Throws.InstanceOf<ArgumentException>());
        }

        private static bool HasPrintedPrimary(
            System.Collections.Generic.IEnumerable<CardDefinition> definitions,
            CardInstance card,
            ActionKind kind) =>
            definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == kind);

        private static DefeatOutcome Outcome(
            string id,
            HeroId defeated,
            HeroId defeating,
            MatchParticipants participants) =>
            new DefeatOutcome(
                new DefeatOutcomeId(id),
                new DefeatTargetId(defeated.Value),
                participants.GetTeamForHero(defeated).Id,
                defeating,
                1,
                1);
    }
}
