using System;
using System.Diagnostics;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 8.36g's failing-control/passing-candidate tactical evidence.</summary>
    public sealed class BotPrivateTacticsTests
    {
        [Test]
        public void CandidateCrossesTheOneHexGapThatTheStaticControlAvoids()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var hero = session.Projection.Participants.Heroes[0].Id;
            var own = session.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;
            var ownTeam = session.Projection.Participants.GetTeamForHero(hero).Id;
            var enemy = session.Projection.Heroes.First(value =>
                !session.Projection.Participants.GetTeamForHero(value.HeroId).Id.Equals(ownTeam));
            var adjacent = enemy.Coordinate.GetNeighbors()
                .OrderBy(value => HexDistanceQuery.Calculate(own, value)).First();
            var twoAway = adjacent.GetNeighbors()
                .Where(value => HexDistanceQuery.Calculate(value, enemy.Coordinate) == 2)
                .OrderBy(value => HexDistanceQuery.Calculate(own, value)).First();
            var stayShort = DirectorCommand.ChooseMovementDestination(twoAway);
            var enterBattle = DirectorCommand.ChooseMovementDestination(adjacent);
            var choices = new[] { stayShort, enterBattle };

            var control = BotPolicy.SelectTierTwo(
                choices, session.Projection, hero, new DeterministicBotRandom(1),
                nearEqualBand: BotPolicy.ExactTiesOnly);
            var candidate = BotPolicy.SelectPrivateSessionTactics(
                choices, session.Projection, hero, new DeterministicBotRandom(1),
                BotPolicy.ExactTiesOnly);

            Assert.That(control, Is.SameAs(stayShort),
                "The control must reproduce the reported stop-short decision before the fix is credited.");
            Assert.That(candidate, Is.SameAs(enterBattle),
                "The lobby policy should accept one enemy adjacency to create productive combat.");
        }

        [Test]
        public void CandidateValuesAFeasibleSkillAboveRoutineMovement()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var pair = session.Projection.Cards
                .Select(card => new
                {
                    Card = card,
                    Definition = session.Projection.Definitions.First(definition =>
                        definition.Reference.Equals(card.DefinitionReference))
                })
                .Select(value => new
                {
                    value.Card,
                    Skill = value.Definition.ActionDefinitions.FirstOrDefault(action => action.Kind == ActionKind.Skill),
                    Move = value.Definition.ActionDefinitions.FirstOrDefault(action =>
                        action.Kind == ActionKind.Movement || action.Kind == ActionKind.FastTravel)
                })
                .First(value => value.Skill != null && value.Move != null);
            var commands = new[]
            {
                DirectorCommand.StartAction(pair.Card.Id, pair.Move.Id),
                DirectorCommand.StartAction(pair.Card.Id, pair.Skill.Id)
            };

            Assert.That(BotPolicy.SelectTierTwo(
                    commands, session.Projection, pair.Card.OwnerId, new DeterministicBotRandom(3),
                    nearEqualBand: BotPolicy.ExactTiesOnly),
                Is.SameAs(commands[0]), "The static control treats Skill as a zero-value tie.");
            Assert.That(BotPolicy.SelectPrivateSessionTactics(
                    commands, session.Projection, pair.Card.OwnerId, new DeterministicBotRandom(3),
                    BotPolicy.ExactTiesOnly),
                Is.SameAs(commands[1]), "A feasible Skill should beat routine repositioning.");
        }

        [Test]
        public void CandidateReactsToKnownThreatAndDoesNotInventHiddenCards()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var owner = host.ProjectionForSeat(seat.Id);
            var ownTeam = owner.Participants.GetTeamForHero(seat.HeroId).Id;
            var enemies = owner.Participants.Heroes.Where(hero =>
                !owner.Participants.GetTeamForHero(hero.Id).Id.Equals(ownTeam)).Take(2).ToArray();
            var targets = new[]
            {
                DirectorCommand.ChooseAttackTarget(enemies[0].Id),
                DirectorCommand.ChooseAttackTarget(enemies[1].Id)
            };

            Assert.That(owner.Cards.Any(card => card.OwnerId.Equals(enemies[1].Id)), Is.False,
                "An unseen enemy hand must be absent, not estimated as exact knowledge.");
            var tieSeed = Enumerable.Range(0, 100).First(seed => ReferenceEquals(
                BotPolicy.SelectPrivateSessionTactics(
                    targets, owner, seat.HeroId, new DeterministicBotRandom(seed),
                    BotPolicy.ExactTiesOnly), targets[0]));
            var hiddenChoice = BotPolicy.SelectPrivateSessionTactics(
                targets, owner, seat.HeroId, new DeterministicBotRandom(tieSeed),
                BotPolicy.ExactTiesOnly);

            var threat = host.Projection.Cards
                .Where(card => card.OwnerId.Equals(enemies[1].Id) &&
                    card.State.Location == CardLocation.Hand)
                .First(card => host.Projection.Definitions
                    .First(definition => definition.Reference.Equals(card.DefinitionReference))
                    .ActionDefinitions.Any(action => action.Kind == ActionKind.Attack || action.Kind == ActionKind.Skill));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(threat), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(threat), Is.True);
            owner = host.ProjectionForSeat(seat.Id);
            Assert.That(owner.PublicCards.Select(card => card.Id), Does.Contain(threat.Id));
            var knownChoice = BotPolicy.SelectPrivateSessionTactics(
                targets, owner, seat.HeroId, new DeterministicBotRandom(tieSeed),
                BotPolicy.ExactTiesOnly);

            Assert.That(hiddenChoice, Is.SameAs(targets[0]),
                "With both hands hidden, the seeded tie—not invented knowledge—decides.");
            Assert.That(knownChoice, Is.SameAs(targets[1]),
                "Once a threat is public, the bot should prefer removing its owner.");
        }

        [Test]
        public void RetrievedPublicCardsRemainKnownWhileNeverSeenCardsStayHidden()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var driven = OwnerScopedProjectionTests.DriveThroughSeats(
                host, 5000, model => model.RoundIndex > 1, seed: 2);
            Assert.That(host.Projection.RoundIndex, Is.GreaterThan(1),
                "The fixture did not cross p.8 retrieval; commands=" + driven);

            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                var retrievedKnown = view.PublicCards.Where(card =>
                    card.State.Location == CardLocation.Hand &&
                    !card.OwnerId.Equals(seat.HeroId)).ToArray();
                Assert.That(retrievedKnown, Is.Not.Empty,
                    "A seat should retain bounded knowledge of cards revealed before retrieval.");

                var stillHidden = host.Projection.Cards.Where(card =>
                    !card.OwnerId.Equals(seat.HeroId) &&
                    card.State.Location == CardLocation.Hand &&
                    !view.PublicCards.Any(known => known.Id.Equals(card.Id))).ToArray();
                Assert.That(stillHidden, Is.Not.Empty,
                    "Public retrieval memory must not expose never-seen cards in the same hands.");
            }
        }

        [Test]
        public void FixedSeedCandidateMakesBoundedLegalForwardProgress()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var timer = Stopwatch.StartNew();
            var result = BotSelfPlay.Play(
                session, 307, 900, _ => BotTier.PrivateSessionTactics, useLookahead: false);
            timer.Stop();

            TestContext.WriteLine(
                "commands=" + result.Commands + " rounds=" + result.RoundsReached +
                " attack=" + result.Tactics.Attacks + " skill=" + result.Tactics.Skills +
                " fastTravel=" + result.Tactics.FastTravels + " minionDefeat=" +
                result.Tactics.MinionDefeats + " heroDefeat=" + result.Tactics.HeroDefeats +
                " life=" + result.Tactics.TeamLifeSpent + " elapsedMs=" + timer.Elapsed.TotalMilliseconds);
            Assert.That(result.Stalled, Is.False);
            Assert.That(result.Refusals, Is.Empty);
            Assert.That(result.RoundsReached, Is.GreaterThan(1));
            Assert.That(result.Tactics.Attacks + result.Tactics.Skills, Is.GreaterThan(0));
            Assert.That(result.MillisecondsPerDecision(BotTier.PrivateSessionTactics), Is.LessThan(10));
        }

        [Test]
        [Explicit("Measurement: paired static-control/candidate outcomes and tactical counters.")]
        public void PairedBaselineCandidateSample()
        {
            foreach (var seed in new[] { 301, 302, 303, 304 })
            {
                foreach (var tier in new[] { BotTier.TierTwo, BotTier.PrivateSessionTactics })
                {
                    var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
                    var result = BotSelfPlay.Play(session, seed, BotTierTwoBench.CommandBudget,
                        _ => tier, useLookahead: false);
                    TestContext.WriteLine(
                        "seed=" + seed + " policy=" + tier + " winner=" + result.Outcome.WinningTeamId +
                        " commands=" + result.Commands + " rounds=" + result.RoundsReached +
                        " attacks=" + result.Tactics.Attacks + " skills=" + result.Tactics.Skills +
                        " fastTravel=" + result.Tactics.FastTravels + " minionDefeats=" +
                        result.Tactics.MinionDefeats + " heroDefeats=" + result.Tactics.HeroDefeats +
                        " life=" + result.Tactics.TeamLifeSpent + " holds=" + result.Tactics.PassesOrHolds +
                        " ms=" + result.MillisecondsPerDecision(tier).ToString("F3"));
                    Assert.That(result.Stalled, Is.False);
                    Assert.That(result.Refusals, Is.Empty);
                }
            }
        }
    }
}
