using System;
using System.Linq;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Play;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveHeavyTests
    {
        [Test]
        public void HookTiesOfferTheClosestUnitsAndRestoreTheChoice()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-1,3"), StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-3,1") }, new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy") });
            PveTestDriver NewRun() => PveTestDriver.StartWithOpening(new[] { "X" }, new[] { "hero-brogan" }, encounter, catalog);
            var run = NewRun();
            Reach(run, () => run.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.SkillActionId) == true);
            Step(run);
            Assert.That(run.Session.Peek().Prompt.Kind, Is.EqualTo(PvePromptKind.EnemyClosestUnit));
            Assert.That(run.Match.PendingEffectChoice.SelectInvocations.Count, Is.EqualTo(2));
            var snapshot = run.Director.CaptureSnapshot();
            var restored = NewRun();
            var restore = restored.Director.RestoreSnapshot(snapshot);
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            Step(run);
            Step(restored);
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(run.Director.CaptureSnapshot()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HookPullsTheNearestCreepAndStopsAtAnEnemyObstacle(bool obstacle)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-4,1"), StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-5,3") },
                obstacle ? new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy"), new PveSpawn(1, PveTestContent.Hex("-3,1"), "heavy") }
                    : new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy") });
            var run = PveTestDriver.StartWithOpening(obstacle ? new[] { "X", "X" } : new[] { "X" },
                new[] { "hero-brogan" }, encounter, catalog);
            var first = run.Composed.Enemies.First().HeroId;
            Reach(run, () => run.Match.SelectedActor.ActorId.Equals(first) &&
                run.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.SkillActionId) == true);
            Step(run);
            Assert.That(run.Match.Minions.Single().Coordinate, Is.EqualTo(PveTestContent.Hex(obstacle ? "-4,1" : "-2,1")));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThickHideCountersOnlyOriginalAdjacentAttackerAndRestoresDefenseChoice(bool defend)
        {
            var run = CreateCounter();
            var hero = run.Composed.Heroes.First();
            var enemy = run.Composed.Enemies.Single().HeroId;
            Reach(run, () => run.Match.Heroes.Any(value => value.HeroId.Equals(hero)));
            run.Session.RunAutomatic();
            var attack = run.Match.Cards.Single(card => card.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger));
            Accept(run, DirectorCommand.CommitCard(hero, attack.Id));
            Reach(run, () => run.Match.SelectedActor.ActorId.Equals(hero));
            Accept(run, DirectorCommand.StartAction(WaspCardContent.PrimaryAttack));
            Reach(run, () => run.Match.PendingAttackTargetChoice != null);
            Accept(run, DirectorCommand.ChooseAttackTarget(enemy));
            Reach(run, () => run.Match.PendingAttackTargetChoice?.LegalTargetHeroIds.Contains(hero) == true);
            Assert.That(run.Match.StandingActiveEffectSourceCardInstanceIds, Does.Contain(attack.Id),
                "Magnetic Dagger's after-attack effect must finish before the counterattack starts.");
            Assert.That(run.Match.PendingAttackTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { hero }));
            var targetSnapshot = run.Director.CaptureSnapshot();
            var atTarget = CreateCounter();
            var targetRestore = atTarget.Director.RestoreSnapshot(targetSnapshot);
            Assert.That(targetRestore.Accepted, Is.True, targetRestore.Detail);
            Assert.That(atTarget.Match.PendingAttackTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { hero }));
            Accept(run, DirectorCommand.ChooseAttackTarget(hero));
            Assert.That(run.Match.PendingDefenseChoice, Is.Not.Null);
            var saved = run.Director.CaptureSnapshot();
            Assert.That(run.Director.Undo().Accepted, Is.True);
            Assert.That(run.Director.CaptureSnapshot(), Is.EqualTo(targetSnapshot));
            Accept(run, DirectorCommand.ChooseAttackTarget(hero));
            Assert.That(run.Director.CaptureSnapshot(), Is.EqualTo(saved));
            var restored = CreateCounter();
            var result = restored.Director.RestoreSnapshot(saved);
            Assert.That(result.Accepted, Is.True, result.Detail);
            foreach (var driver in new[] { run, restored })
            {
                Accept(driver, defend ? DirectorCommand.ChooseDefenseCard(driver.Match.Cards.First(card =>
                    card.OwnerId.Equals(hero) && card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Shock)).Id)
                    : DirectorCommand.DeclineDefense());
                Assert.That(driver.Match.Heroes.Any(value => value.HeroId.Equals(hero)), Is.EqualTo(defend));
                Assert.That(driver.Match.Heroes.Any(value => value.HeroId.Equals(enemy)), Is.True);
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(run.Director.CaptureSnapshot()));
        }

        private static void Accept(PveTestDriver run, DirectorCommand command)
        {
            var result = run.Director.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        [Test]
        public void ThickHideDoesNotCounterARangedAttackerWhoIsNotAdjacent()
        {
            var run = CreateCounter(true);
            var hero = run.Composed.Heroes.First();
            var enemy = run.Composed.Enemies.Single().HeroId;
            Reach(run, () => run.Match.Heroes.Any(value => value.HeroId.Equals(hero)));
            run.Session.RunAutomatic();
            var attack = run.Match.Cards.Single(card => card.DefinitionReference.DefinitionId.Equals(WaspCardContent.ChargedBoomerang));
            Accept(run, DirectorCommand.CommitCard(hero, attack.Id));
            Reach(run, () => run.Match.SelectedActor.ActorId.Equals(hero));
            Accept(run, DirectorCommand.StartAction(WaspCardContent.PrimaryRangedAttack));
            Accept(run, DirectorCommand.ChooseAttackTarget(enemy));
            Assert.That(run.Match.PendingDefenseChoice, Is.Not.Null);
            Step(run);
            Assert.That(run.Match.PendingAttackTargetChoice, Is.Null);
            Assert.That(run.Match.PendingDefenseChoice, Is.Null);
            Assert.That(run.Match.Heroes.Count, Is.EqualTo(3));
            Assert.That(run.Match.Cards.Single(card => card.DefinitionReference.DefinitionId.Value == "pve-heavy-p").State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
        }

        private static PveTestDriver CreateCounter(bool ranged = false)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-9,7"), StandardMinionType.Melee) },
                new[] { PveTestContent.Hex(ranged ? "-3,2" : "-2,1"), PveTestContent.Hex("-1,2") }, new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy") });
            var run = PveTestDriver.StartWithOpening(new[] { "X" }, new[] { "hero-wasp", "hero-brogan" }, encounter, catalog);
            if (!ranged) return run;
            var composed = run.Composed;
            var initial = composed.InitialState;
            var card = initial.InitialConfiguration.CardInstances.Single(value => value.DefinitionReference.DefinitionId.Equals(WaspCardContent.ChargedBoomerang));
            Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(card), Is.True);
            var seed = initial.Scenario;
            var scenario = new InitialScenarioDescription(seed.Participants, seed.CardDefinitions,
                initial.InitialConfiguration.CardInstances, seed.InitialTieBreakerState, seed.SetupSeed,
                seed.SelectedMap, seed.InitialHeroes, seed.InitialObstacles, seed.HeroBodySupplies);
            var staged = new MatchSessionInitialState(scenario, initial.Setup, initial.AvailableMaps,
                initial.InitialConfiguration, initial.InitialTiming, initial.StagedProgression, initial.Experiments, initial.ModeRules);
            return new PveTestDriver(new PveComposedEncounter(composed.Setup, composed.Encounter,
                composed.Catalog, staged, composed.Heroes, composed.Enemies));
        }

        [TestCase("-4,1", "-2,1")]
        [TestCase("-2,1", "-2,1")]
        [TestCase("-5,3", "-5,3")]
        public void HookPullsOnlyOnAStraightLineAndStopsAdjacent(string start, string expected)
        {
            var run = Create("X", start);
            Reach(run, () => run.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.SkillActionId) == true);
            var before = run.Director.CaptureSnapshot();
            Step(run);
            Assert.That(run.Match.Heroes.Single(hero => hero.HeroId.Equals(run.Composed.Heroes.Single())).Coordinate,
                Is.EqualTo(PveTestContent.Hex(expected)));
            var after = run.Director.CaptureSnapshot();
            Assert.That(run.Director.Undo().Accepted, Is.True);
            Assert.That(run.Director.CaptureSnapshot(), Is.EqualTo(before));
            Step(run);
            Assert.That(run.Director.CaptureSnapshot(), Is.EqualTo(after));
            var restored = Create("X", start);
            Assert.That(restored.Director.RestoreSnapshot(after).Accepted, Is.True);
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(after));
        }

        [Test]
        public void HurlAttacksTwoDifferentHeroesExactlyOnceEach()
        {
            var run = Create("C", "-2,1", "-1,2");
            Reach(run, () => run.Match.PendingAttackTargetChoice != null);
            var first = run.Match.PendingAttackTargetChoice.LegalTargetHeroIds.First();
            var command = run.Session.CurrentPrompt?.Answers.First() ?? run.Session.Peek().Command;
            Assert.That(command, Is.Not.Null);
            Step(run);
            Reach(run, () => run.Match.PendingAttackTargetChoice != null);
            Assert.That(run.Match.PendingAttackTargetChoice.LegalTargetHeroIds.Contains(first), Is.False);
            Assert.That(run.Match.PendingAttackTargetChoice.LegalTargetHeroIds.Count, Is.EqualTo(1));
            var saved = run.Director.CaptureSnapshot();
            var restored = Create("C", "-2,1", "-1,2");
            Assert.That(restored.Director.RestoreSnapshot(saved).Accepted, Is.True);
            foreach (var driver in new[] { run, restored })
            {
                Step(driver);
                Reach(driver, () => driver.Match.TurnIndex == 2);
                Assert.That(driver.Match.Heroes.Any(hero => driver.Composed.Heroes.Contains(hero.HeroId)), Is.False);
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(run.Director.CaptureSnapshot()));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HurlMovesTowardClosestBlueUnitAndRestoresChoice(bool creepIsCloser)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var closer = PveTestContent.Hex("-4,1");
            var farther = PveTestContent.Hex("-1,-3");
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(creepIsCloser ? closer : farther, StandardMinionType.Melee) },
                new[] { creepIsCloser ? farther : closer },
                new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy") });
            var run = PveTestDriver.StartWithOpening(new[] { "C" }, new[] { "hero-brogan" }, encounter, catalog);
            var enemy = run.Composed.Enemies.Single().HeroId;
            Reach(run, () => run.Match.PendingMovementChoice?.MovingHeroId.Equals(enemy) == true);
            Assert.That(run.Match.TurnIndex, Is.EqualTo(1));
            var saved = run.Director.CaptureSnapshot();
            var restored = PveTestDriver.Start(run.Composed.Setup.Seed, new[] { "hero-brogan" }, encounter, catalog);
            Assert.That(restored.Director.RestoreSnapshot(saved).Accepted, Is.True);
            foreach (var driver in new[] { run, restored })
            {
                var step = driver.Session.Peek();
                var choices = step.Kind == PveStepKind.Command ? new[] { step.Command } : step.Prompt.Answers.ToArray();
                Assert.That(choices, Is.Not.Empty);
                foreach (var choice in choices)
                {
                    Assert.That(choice.Kind, Is.EqualTo(DirectorCommandKind.ChooseMovementDestination));
                    Assert.That(GuardsDigital.Core.Board.HexDistanceQuery.Calculate(choice.Coordinate, closer), Is.EqualTo(2));
                }
                Step(driver);
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(run.Director.CaptureSnapshot()));
        }

        private static void Reach(PveTestDriver run, Func<bool> condition)
        {
            for (var i = 0; i < 300 && !condition(); i++) Step(run);
            Assert.That(condition(), Is.True);
        }

        private static void Step(PveTestDriver run)
        {
            var answer = run.Session.Peek().Kind == PveStepKind.Command ? run.Session.Step(out _) : run.Session.Execute(run.PlayerAnswer());
            Assert.That(answer.Accepted, Is.True, answer.Detail);
        }

        private static PveTestDriver Create(string opening, params string[] placements)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-9,7"), StandardMinionType.Melee) },
                placements.Select(PveTestContent.Hex), new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "heavy") });
            for (ulong seed = 1; seed < 1000; seed++)
            {
                var run = PveTestDriver.Start(seed, new[] { "hero-brogan", "hero-arien" }.Take(placements.Length).ToArray(), encounter, catalog);
                if (PvePlans.PlanFor(run.Composed, run.Composed.Enemies.Single(), 1)[0] == opening) return run;
            }
            throw new InvalidOperationException("No seed for opening.");
        }
    }
}
