using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveEnemyCategoryTests
    {
        [TestCase("melee")]
        [TestCase("ranged")]
        public void UnchangedHighVoltageTargetsModeMinionAndRestoresItsTypedOutcome(string typeId)
        {
            var driver = Create(typeId);
            var hero = driver.Composed.Heroes.Single();
            var target = driver.Composed.Enemies[0].HeroId;
            var heavy = driver.Composed.Enemies[1].HeroId;
            for (var i = 0; i < 50 && !driver.Match.Heroes.Any(value => value.HeroId.Equals(hero)); i++)
                Assert.That(driver.Advance(), Is.True);
            driver.Session.RunAutomatic();
            var skill = driver.Match.Cards.Single(value => value.OwnerId.Equals(hero) &&
                value.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier));
            Accept(driver, DirectorCommand.CommitCard(hero, skill.Id));
            for (var i = 0; i < 50 && !driver.Match.SelectedActor.ActorId.Equals(hero); i++)
            {
                var step = driver.Session.Peek();
                if (step.Kind == GuardsDigital.Pve.Play.PveStepKind.Command)
                    Assert.That(driver.Session.Step(out _).Accepted, Is.True);
                else
                {
                    var answer = driver.PlayerAnswer();
                    Assert.That(answer, Is.Not.Null);
                    Accept(driver, answer);
                }
            }
            Assert.That(driver.Match.SelectedActor.ActorId, Is.EqualTo(hero));
            Accept(driver, DirectorCommand.StartAction(WaspCardContent.PrimarySkill));
            Assert.That(driver.Match.PendingEffectChoice, Is.Not.Null);
            Accept(driver, DirectorCommand.AnswerEffectChoice(driver.Match.PendingEffectChoice.ConfirmInvocation));
            var targets = driver.Match.PendingEffectChoice.SelectInvocations;
            Assert.That(targets.Count(value => value.Candidate.Value.HeroId.Equals(target)), Is.EqualTo(1));
            Assert.That(targets.Any(value => value.Candidate.Value.HeroId.Equals(heavy)), Is.False);
            Select(driver, targets.Single(value => value.Candidate.Value.HeroId.Equals(target)));

            // The next clause reads the hero who WAS adjacent. The target remains staged until
            // the triggered effect finishes, exactly as the ordinary minion variant does.
            Assert.That(driver.Match.PendingEffectChoice.SelectInvocations.Any(value =>
                value.Candidate.Value.HeroId.Equals(heavy)), Is.True);
            var snapshot = driver.Director.CaptureSnapshot();
            var undo = driver.Director.Undo();
            Assert.That(undo.Accepted, Is.True, undo.Detail);
            Select(driver, driver.Match.PendingEffectChoice.SelectInvocations.Single(value =>
                value.Candidate.Value.HeroId.Equals(target)));
            Assert.That(driver.Director.CaptureSnapshot(), Is.EqualTo(snapshot),
                "Retained-prefix replay must preserve category selection and its typed outcome.");
            var wrongMode = Create(typeId, false);
            var beforeRefusal = wrongMode.Director.CaptureSnapshot();
            Assert.That(wrongMode.Director.RestoreSnapshot(snapshot).Accepted, Is.False,
                "A Hero value in a declared Minion slot requires the trusted mode category.");
            Assert.That(wrongMode.Director.CaptureSnapshot(), Is.EqualTo(beforeRefusal));
            var restored = Create(typeId);
            var restore = restored.Director.RestoreSnapshot(snapshot);
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(snapshot));
            foreach (var run in new[] { driver, restored })
            {
                Select(run, run.Match.PendingEffectChoice.SelectInvocations.Single(value =>
                    value.Candidate.Value.HeroId.Equals(heavy)));
                Select(run, run.Match.PendingEffectChoice.SelectInvocations.First());
                Assert.That(run.Match.Heroes.Any(value => value.HeroId.Equals(target)), Is.False);
                Assert.That(run.Match.Heroes.Any(value => value.HeroId.Equals(heavy)), Is.True);
                Assert.That(run.Match.Cards.Count(value => value.OwnerId.Equals(heavy) &&
                    value.State.Location == CardLocation.DiscardPile), Is.EqualTo(1));
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(driver.Director.CaptureSnapshot()));
        }

        private static PveTestDriver Create(string typeId, bool enableCategory = true)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var shipped, out var layout);
            var types = shipped.Types.Where(type => type.Id == typeId || type.Id == "heavy")
                .Select(type => new PveEnemyType(type.Id, type.DisplayName, type.Level,
                    type.OrangeCards.Select(card => card.Key == "X"
                        ? new PveOrangeCard("X", "Fixture Idle", 1, 1, null, 0, null, null, true) : card),
                    type.PurpleCards, new[] { new[] { "X", "A", "B", "C" } }, type.TokenType,
                    enableCategory ? type.MinionCategory : StandardMinionType.None));
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-9,7"), StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-5,3") },
                new[] { new PveSpawn(1, PveTestContent.Hex("-4,2"), typeId),
                    new PveSpawn(1, PveTestContent.Hex("-3,2"), "heavy") });
            var composed = PveTestDriver.StartWithOpening(new[] { "X", "X" }, new[] { "hero-wasp" },
                encounter, new PveEnemyCatalog("placeholder-test", types)).Composed;
            var ultimate = composed.InitialState.InitialConfiguration.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.HighVoltage));
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            // Capture the staged Ultimate in the replay seed as well as the live configuration.
            var initial = composed.InitialState;
            var seed = initial.Scenario;
            var scenario = new InitialScenarioDescription(seed.Participants, seed.CardDefinitions,
                initial.InitialConfiguration.CardInstances, seed.InitialTieBreakerState, seed.SetupSeed,
                seed.SelectedMap, seed.InitialHeroes, seed.InitialObstacles, seed.HeroBodySupplies);
            var staged = new MatchSessionInitialState(scenario, initial.Setup, initial.AvailableMaps,
                initial.InitialConfiguration, initial.InitialTiming, initial.StagedProgression,
                initial.Experiments, initial.ModeRules);
            return new PveTestDriver(new PveComposedEncounter(composed.Setup, composed.Encounter,
                composed.Catalog, staged, composed.Heroes, composed.Enemies));
        }

        private static void Accept(PveTestDriver driver, DirectorCommand command)
        {
            var result = driver.Director.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static void Select(PveTestDriver driver, EffectChoiceCommand choice)
        {
            Accept(driver, DirectorCommand.AnswerEffectChoice(choice));
            var standing = driver.Match.PendingEffectChoice;
            if (standing != null && standing.ChoiceId.Equals(choice.ChoiceId) && standing.ConfirmInvocation != null)
                Accept(driver, DirectorCommand.AnswerEffectChoice(standing.ConfirmInvocation));
        }
    }
}
