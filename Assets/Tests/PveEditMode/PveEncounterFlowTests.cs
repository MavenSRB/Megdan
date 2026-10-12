using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-022, PVE-D05/D07/D11/D12/D13. The encounter's own flow over real rounds: round-1 placement,
    /// enemy commitments from their plans, enemy turns, the end of round and round-2 spawns.
    /// </summary>
    public sealed class PveEncounterFlowTests
    {
        [Test]
        public void RoundOneOpensWithThePlayersPlacementBeforeAnyoneCommits()
        {
            var driver = PveTestDriver.Start();
            driver.Session.RunAutomatic();

            var pending = driver.Match.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null, "The round-1 placement is waiting on the player.");
            Assert.That(MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out var round) && round == 1, Is.True);
            Assert.That(pending.OpenItems.Single().ChoosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(pending.OpenItems.Single().Options.Count, Is.EqualTo(5), "All five placement spaces are offered.");
            Assert.That(driver.Match.Commitments.All(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse), Is.True);

            var hero = driver.Composed.Heroes[0];
            var card = driver.Match.Cards.First(value => value.OwnerId.Equals(hero));
            Assert.That(driver.Session.Execute(DirectorCommand.CommitCard(hero, card.Id)).Accepted, Is.False,
                "No hero commits before every hero is placed.");
        }

        [Test]
        public void AfterPlacementEveryEnemyCommitsItsPlannedCard()
        {
            var driver = PveTestDriver.Start();
            driver.PlayUntil(match => match.PendingSystemTransitionStep == null &&
                driver.Composed.Heroes.All(hero => match.Heroes.Any(value => value.HeroId.Equals(hero))));
            driver.Session.RunAutomatic();

            foreach (var enemy in driver.Composed.Enemies)
            {
                var commitment = driver.Match.Commitments.Single(value => value.HeroId.Equals(enemy.HeroId));
                Assert.That(commitment.Status, Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown), enemy.DisplayName);
            }

            foreach (var hero in driver.Composed.Heroes)
            {
                Assert.That(driver.Match.Commitments.Single(value => value.HeroId.Equals(hero)).Status,
                    Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse), "Heroes are the player's to commit.");
            }
        }

        [Test]
        public void TwoRoundsPlayAndTheRoundTwoEnemiesEnterOnTheirSpawns()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.RoundIndex >= 2 && match.PendingSystemTransitionStep == null &&
                match.Commitments.Any(value => value.Status != TurnCardCommitmentStatus.AwaitingResponse));

            Assert.That(driver.Session.Status, Is.EqualTo(PveEncounterStatus.Ongoing));
            foreach (var enemy in driver.Composed.Enemies.Where(value => value.Spawn.Round == 2))
            {
                var standing = driver.Match.Heroes.Single(value => value.HeroId.Equals(enemy.HeroId));
                Assert.That(standing.Coordinate, Is.EqualTo(enemy.Spawn.Coordinate), enemy.DisplayName);
            }
        }

        [Test]
        public void EveryTurnCommitsThePublicShuffledCard()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            var cards = new PveCardIndex(driver.Composed.Catalog);
            var turns = new System.Collections.Generic.HashSet<int>();
            for (var index = 0; index < 2000 && driver.Match.RoundIndex == 1; index++)
            {
                var step = driver.Session.Peek();
                if (step.Kind == PveStepKind.Command)
                {
                    if (step.Command.Kind == DirectorCommandKind.CommitCard)
                    {
                        var enemy = driver.Composed.FindEnemy(step.Command.HeroId);
                        Assert.That(enemy, Is.Not.Null);
                        var card = driver.Match.Cards.Single(value => value.Id.Equals(step.Command.CardInstanceId));
                        Assert.That(cards.Orange(card.DefinitionReference.DefinitionId).Key,
                            Is.EqualTo(PvePlans.PlanFor(driver.Composed, enemy, 1)[driver.Match.TurnIndex - 1]));
                        turns.Add(driver.Match.TurnIndex);
                    }
                    Assert.That(driver.Session.Step(out _).Accepted, Is.True);
                }
                else
                {
                    var answer = driver.PlayerAnswer();
                    Assert.That(answer, Is.Not.Null);
                    Assert.That(driver.Session.Execute(answer).Accepted, Is.True);
                }
            }
            Assert.That(driver.Match.RoundIndex, Is.EqualTo(2));
            Assert.That(turns, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void RoundTwoEnemiesStayOffTheBoardThroughRoundOne()
        {
            var driver = PveTestDriver.Start(catalog: PveTestContent.HeroHuntingCatalog());
            driver.PlayUntil(match => match.RoundIndex == 1 && match.TurnIndex == 4 &&
                match.Phase == RoundResolutionPhase.ResolvingTurn);

            foreach (var enemy in driver.Composed.Enemies.Where(value => value.Spawn.Round == 2))
            {
                Assert.That(driver.Match.Heroes.Any(value => value.HeroId.Equals(enemy.HeroId)), Is.False, enemy.DisplayName);
            }
        }

        [Test]
        public void TheSameSeedPlaysTheSameEncounter()
        {
            var first = PveTestDriver.Start(seed: 77, catalog: PveTestContent.HeroHuntingCatalog());
            var second = PveTestDriver.Start(seed: 77, catalog: PveTestContent.HeroHuntingCatalog());
            first.PlayUntil(match => match.RoundIndex >= 2 && match.TurnIndex >= 2);
            second.PlayUntil(match => match.RoundIndex >= 2 && match.TurnIndex >= 2);

            Assert.That(second.Session.CaptureSnapshot(), Is.EqualTo(first.Session.CaptureSnapshot()));
        }

        [Test]
        public void UndoWalksBackOverTheEnemiesToTheLastPlayerCommand()
        {
            var driver = PveTestDriver.Start();
            for (var attempt = 0; attempt < 500; attempt++)
            {
                driver.Session.RunAutomatic();
                var before = driver.Session.CaptureSnapshot();
                var command = driver.PlayerAnswer();
                Assert.That(command, Is.Not.Null, "The passive player always has an answer here.");
                Assert.That(driver.Session.Execute(command).Accepted, Is.True);
                if (driver.Session.RunAutomatic() == 0) continue;

                Assert.That(driver.Session.Undo().Accepted, Is.True);
                Assert.That(driver.Session.CaptureSnapshot(), Is.EqualTo(before),
                    "Undo removed the enemies' steps and the player's " + command.Kind + ".");
                return;
            }

            Assert.Fail("No player command was followed by enemy steps.");
        }

        [Test]
        public void RestartReturnsToTheComposedEncounter()
        {
            var driver = PveTestDriver.Start();
            var composed = driver.Session.CaptureSnapshot();
            driver.PlayUntil(match => match.TurnIndex >= 2);

            Assert.That(driver.Session.Restart().Accepted, Is.True);
            Assert.That(driver.Session.CaptureSnapshot(), Is.EqualTo(composed));
        }

        [Test]
        public void EnemyTiesAreAutomaticLegalAndStableAcrossRestoreRatherThanPlayerCommands()
        {
            var driver = PveTestDriver.Start();
            for (var i = 0; i < 2000; i++)
            {
                var next = driver.Session.Peek();
                if (next.Note?.StartsWith("enemy choice:") == true) break;
                if (next.Kind == PveStepKind.Command) Assert.That(driver.Session.Step(out _).Accepted, Is.True);
                else
                {
                    var answer = driver.PlayerAnswer();
                    Assert.That(answer, Is.Not.Null, "Fixture must reach an enemy tie before ending.");
                    Assert.That(driver.Session.Execute(answer).Accepted, Is.True);
                }
            }
            var selected = driver.Session.Peek();
            Assert.That(selected.Note, Does.StartWith("enemy choice:"));
            Assert.That(driver.Match.DecisionContract.Commands, Does.Contain(selected.Command));
            Assert.That(driver.Session.CurrentPrompt, Is.Null);
            var enemy = driver.Composed.Enemies[0].HeroId;
            Assert.That(driver.Session.Execute(DirectorCommand.Pass(enemy)).Accepted, Is.False);
            Assert.That(driver.Session.Execute(selected.Command).Accepted, Is.False);
            Assert.That(driver.Session.Peek().Command, Is.EqualTo(selected.Command));
            var restored = PveTestDriver.Start();
            Assert.That(restored.Director.RestoreSnapshot(driver.Director.CaptureSnapshot()).Accepted, Is.True);
            Assert.That(restored.Session.Peek().Command, Is.EqualTo(selected.Command));
            Assert.That(driver.Session.Step(out _).Accepted, Is.True);
            Assert.That(restored.Session.Step(out _).Accepted, Is.True);
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(driver.Director.CaptureSnapshot()));
        }

        [Test]
        public void LosingTheLastCreepEndsTheEncounter()
        {
            var driver = PveTestDriver.Start();
            driver.PlayUntil(match => false, 5000);

            Assert.That(driver.Session.Status, Is.EqualTo(PveEncounterStatus.Lost),
                "The placeholder enemies defeat the creeps of a passive player.");
            Assert.That(driver.Match.Minions.Any(minion => minion.TeamSide == TeamSide.Blue), Is.False);
            Assert.That(driver.Session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.False,
                "A lost encounter takes no further command.");
            Assert.That(driver.Session.Undo().Accepted, Is.True, "Undo stays available.");
        }
    }
}
