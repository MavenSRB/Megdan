using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Consequences;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.49. p.13's table, paid by a real Defeat on a real progressed board: a hero at
    /// p.15's Tier II stage costs its team two Life counters, and one at Tier III three - where, before
    /// this milestone, every hero cost one because the Tier I cards its upgrades had returned to the
    /// deck were still being read as its lowest card Tier.
    ///
    /// <para>The staged Wasp-versus-Sabina boards are the fixtures because they are composed at p.15's
    /// stages the way a levelled hero really stands: the progressed hand holds the upgraded cards and
    /// the deck holds everything else face up, the Tier I originals included.</para>
    /// </summary>
    [TestFixture]
    public sealed class DefeatCostByTierTests
    {
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        public void DefeatingAProgressedHero_SpendsAsManyLifeCountersAsItsLowestCardTier(
            int tier, int expectedCounters)
        {
            // The enemy hero beside her, so her basic Point Blank Shot - adjacent-only until the
            // Tier III board's Ultimate lengthens it - is offered at both stages.
            var board = SabinaBoard.Create(
                tier: tier,
                branch: SabinaUpgradeBranch.FirstSibling,
                heroPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["primitive-hero"] = StagedHeroBoard.FirstAdjacent
                });
            var session = board.Session;
            var enemyTeam = session.Projection.Participants.GetTeamForHero(board.Enemy).Id;

            Assert.That(HeroDefeatCostQuery.Worth(
                    board.Enemy, session.Projection.Definitions, session.Projection.Cards),
                Is.EqualTo(expectedCounters),
                "The worth the rail shows before the Defeat is the number the Defeat will charge.");
            var before = Counters(session, enemyTeam);
            Assert.That(before, Is.GreaterThan(0));

            board.PlayAttackAt(SabinaCardContent.PointBlankShot, board.Enemy, defendSuccessfully: false);
            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "A hero target opens p.14's Defense question.");
            board.Accept(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(board.Enemy)),
                Is.False, "Declining the Defense is the Defeat.");
            Assert.That(Counters(session, enemyTeam),
                Is.EqualTo(Math.Max(0, before - expectedCounters)),
                $"p.13: a hero whose lowest card Tier is {tier} costs its team {expectedCounters} " +
                "Life counters, not one.");
        }

        [TestCase(2)]
        [TestCase(3)]
        public void ExperimentalBaseOne_SpendsOneLifeOnARealProgressedDefeat(int tier)
        {
            var board = SabinaBoard.Create(
                tier: tier,
                branch: SabinaUpgradeBranch.FirstSibling,
                heroPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["primitive-hero"] = StagedHeroBoard.FirstAdjacent
                },
                experiments: new GuardsDigital.Application.ExperimentalMatchOptions(
                    heroDefeatBaseLifeCostIsOne: true));
            var session = board.Session;
            var enemyTeam = session.Projection.Participants.GetTeamForHero(board.Enemy).Id;
            Assert.That(
                HeroDefeatCostQuery.Worth(
                    board.Enemy, session.Projection.Definitions, session.Projection.Cards),
                Is.EqualTo(tier),
                "The tray number remains the hero's Tier-derived assist reward.");
            var before = Counters(session, enemyTeam);
            board.PlayAttackAt(SabinaCardContent.PointBlankShot, board.Enemy, defendSuccessfully: false);
            var defense = session.Projection.PendingDefenseChoice;
            board.Accept(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            Assert.That(Counters(session, enemyTeam), Is.EqualTo(before - 1));
        }

        [Test]
        public void ExperimentalBaseOne_SurvivesTheRetainedCheckpointRollbackUsedByUndo()
        {
            var board = SabinaBoard.Create(
                tier: 2,
                branch: SabinaUpgradeBranch.FirstSibling,
                heroPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["primitive-hero"] = StagedHeroBoard.FirstAdjacent
                },
                experiments: new GuardsDigital.Application.ExperimentalMatchOptions(
                    heroDefeatBaseLifeCostIsOne: true));
            var session = board.Session;
            var enemyTeam = session.Projection.Participants.GetTeamForHero(board.Enemy).Id;

            // Undo replays long command histories from a retained serialized pre-image through this
            // rollback boundary. The experiment is immutable match configuration rather than Core
            // snapshot data, so the restored resolver must be rebound before play continues.
            session.aggregate.Rollback(session.aggregate.CapturePreImage());

            var before = Counters(session, enemyTeam);
            board.PlayAttackAt(SabinaCardContent.PointBlankShot, board.Enemy, defendSuccessfully: false);
            var defense = session.Projection.PendingDefenseChoice;
            board.Accept(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            Assert.That(Counters(session, enemyTeam), Is.EqualTo(before - 1),
                "A retained-checkpoint Undo must not silently restore the official Tier-II base cost.");
        }


        private static int Counters(GuardsDigital.Application.MatchSession session, TeamId team) =>
            session.Projection.TeamLifeCounters.Single(entry => entry.Key.Equals(team)).Value;
    }
}
