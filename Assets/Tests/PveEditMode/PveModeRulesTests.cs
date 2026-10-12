using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-022. The engine hooks the PvE mode rules switch on, observed through played encounters:
    /// PVE-D03 end-of-round steps, PVE-D11/D14/D22 no coins, PVE-D14 granted levels, PVE-D18
    /// respawn spaces - and all of them still in force after a snapshot restore and a checkpoint Undo.
    /// </summary>
    public sealed class PveModeRulesTests
    {
        [Test]
        public void TheEndOfRoundSkipsLanePushLevelUpAndPityCoins()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.CompletedRounds.Count >= 1);

            AssertEndOfRoundSkipped(driver.Match);
        }

        [Test]
        public void DefeatsPayNoCoins()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.CompletedRounds.Count >= 1);

            Assert.That(driver.Match.Scoreboard.Sum(entry => entry.Deaths), Is.GreaterThan(0),
                "The enemies defeated the passive heroes, which would have paid coins.");
            AssertNoCoins(driver.Match);
        }

        [Test]
        public void ManualLevelGrantIsAbsentAndRefusedBeforeAndAfterCommitment()
        {
            var driver = PveTestDriver.Start();
            driver.PlayUntil(match => match.PendingSystemTransitionStep == null &&
                driver.Composed.Heroes.All(hero => match.Heroes.Any(value => value.HeroId.Equals(hero))));
            driver.Session.RunAutomatic();
            var hero = driver.Composed.Heroes[0];
            var retired = new SystemTransitionDefinitionId("mode-grant-level/" + hero.Value);
            Assert.That(driver.Composed.InitialState.ModeRules.TransitionDefinitions.Any(value => value.Id.Equals(retired)), Is.False);
            var before = driver.Session.CaptureSnapshot();
            Assert.That(driver.Session.Execute(DirectorCommand.BeginSystemTransition(retired)).Accepted, Is.False);
            Assert.That(driver.Session.CaptureSnapshot(), Is.EqualTo(before));
            Assert.That(driver.Match.HeroProgression.Single(value => value.HeroId.Equals(hero)).Level, Is.EqualTo(1));
            var card = driver.Match.Cards.First(value => value.OwnerId.Equals(hero) && value.State.Location == CardLocation.Hand);
            Assert.That(driver.Session.Execute(DirectorCommand.CommitCard(hero, card.Id)).Accepted, Is.True);
            Assert.That(driver.Session.Execute(DirectorCommand.BeginSystemTransition(retired)).Accepted, Is.False);
        }

        [Test]
        public void ADefeatedHeroRespawnsOntoThePlacementSpaces()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.PendingHeroRespawnChoice != null &&
                driver.Composed.Heroes.Contains(match.PendingHeroRespawnChoice.HeroId));

            var placement = driver.Composed.Encounter.HeroPlacement;
            var destinations = driver.Match.PendingHeroRespawnChoice.Destinations.Select(value => value.Coordinate).ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations, Is.SubsetOf(placement), "PVE-D18: the empty placement spaces, not the Throne.");
        }

        [Test]
        public void ARestoredSnapshotKeepsTheModeRules()
        {
            var played = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            played.PlayUntil(match => match.TurnIndex >= 3);
            var snapshot = played.Director.CaptureSnapshot();

            var restored = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            var result = restored.Director.RestoreSnapshot(snapshot);
            Assert.That(result.Accepted, Is.True, result.Detail);
            restored.PlayUntil(match => match.CompletedRounds.Count >= 1);

            AssertEndOfRoundSkipped(restored.Match);
            AssertNoCoins(restored.Match);
        }

        [Test]
        public void ACheckpointUndoKeepsTheModeRules()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.TurnIndex >= 4 && match.Phase == RoundResolutionPhase.ResolvingTurn);
            Assert.That(driver.Director.Projection.Version, Is.GreaterThan(40), "Enough commands for a checkpoint Undo.");

            Assert.That(driver.Session.Undo().Accepted, Is.True);
            driver.PlayUntil(match => match.CompletedRounds.Count >= 1);

            AssertEndOfRoundSkipped(driver.Match);
            AssertNoCoins(driver.Match);
        }

        private static void AssertEndOfRoundSkipped(IMatchReadModel match)
        {
            var outcomes = match.CompletedRounds[0].EndOfRoundOutcomes;
            foreach (var stage in new[]
                     {
                         EndOfRoundStage.ResolveRequestedLanePush,
                         EndOfRoundStage.LevelUpAndUpgrade,
                         EndOfRoundStage.CollectPityCoins
                     })
            {
                Assert.That(outcomes.Single(value => value.Stage == stage).Kind,
                    Is.EqualTo(EndOfRoundStageOutcomeKind.NotApplicable), stage.ToString());
            }

            Assert.That(outcomes.Single(value => value.Stage == EndOfRoundStage.MinionBattle).Kind,
                Is.Not.EqualTo(EndOfRoundStageOutcomeKind.NotApplicable), "PVE-D23: the Minion Battle step still runs, inert.");
        }

        private static void AssertNoCoins(IMatchReadModel match)
        {
            Assert.That(match.HeroProgression.All(value => value.Coins == 0), Is.True, "No balance moved.");
            Assert.That(match.Scoreboard.Sum(entry => entry.CoinsEarned), Is.EqualTo(0), "No coin was earned.");
        }
    }
}
