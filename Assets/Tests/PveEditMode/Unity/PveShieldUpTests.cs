using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Play;
using GuardsDigital.Pve.Presentation;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveShieldUpTests
    {
        [Test]
        public void ShieldUpHasApprovedFaceAndTypedSkillWithoutChangingOtherHands()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var melee = catalog.Find("melee");
            var card = melee.FindOrange("X");
            Assert.That(card.Name, Is.EqualTo("Shield Up"));
            Assert.That(card.Initiative, Is.EqualTo(9));
            Assert.That(card.Skill, Is.EqualTo(PveSkillKind.ImmuneThisTurn));
            Assert.That(card.IsIdle, Is.False);
            Assert.That(card.Options, Is.Empty);
            Assert.That(melee.OrangeHand.Count, Is.EqualTo(4));
            Assert.That(melee.OrangeCards.Count(value => value.AttackValue.HasValue), Is.EqualTo(2));
            Assert.That(melee.PurpleCards, Is.Empty);
            Assert.That(catalog.Find("heavy").FindOrange("X").Skill, Is.EqualTo(PveSkillKind.HookClosestBlueUnit));
            var definition = PveCardFactory.CreateDefinitions(melee).Single(value => value.Id.Equals(PveCardFactory.OrangeId(melee, card)));
            Assert.That(definition.ActionDefinitions.Single().Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.OriginalMode));
            Assert.That(definition.SourceContent.StatesASourceFace, Is.False, "Original wording is not official tabletop provenance.");
            var face = CurrentRoundCards.ReadFace(definition);
            Assert.That(face.PrintedText, Is.EqualTo("This unit is immune this turn."));
            Assert.That(face.PrintedTextKind, Is.EqualTo(CardSourceContentKind.Authored));
            Assert.That(MatchContentCatalogs.ForThisBuild().Capabilities.IsRegistered(PveSkillContent.ImmuneCapability), Is.False);
        }

        [Test]
        public void EnemyChoosesShieldUp_ImmuneSurvivesUndoRestoreAndExpiresThisTurn()
        {
            var driver = Create();
            var enemy = driver.Composed.Enemies.Single().HeroId;
            for (var i = 0; i < 100; i++)
            {
                var step = driver.Session.Peek();
                if (step.Kind == PveStepKind.Command && step.Command.ActionDefinitionId.Equals(PveCardFactory.SkillActionId)) break;
                Step(driver);
            }
            Assert.That(driver.Session.Peek().Command?.ActionDefinitionId, Is.EqualTo(PveCardFactory.SkillActionId));
            Assert.That(driver.Match.TurnIndex, Is.EqualTo(1));
            var before = driver.Director.CaptureSnapshot();
            Assert.That(Read(driver).Round.Timing.Effects, Is.Empty);
            Step(driver);
            var after = driver.Director.CaptureSnapshot();
            CheckImmune(driver, true);
            var undo = driver.Director.Undo();
            Assert.That(undo.Accepted, Is.True, undo.Detail);
            Assert.That(driver.Director.CaptureSnapshot(), Is.EqualTo(before));
            Step(driver);
            Assert.That(driver.Director.CaptureSnapshot(), Is.EqualTo(after));
            var restored = Create();
            var restore = restored.Director.RestoreSnapshot(after);
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(after));
            CheckImmune(restored, true);
            foreach (var run in new[] { driver, restored })
            {
                for (var i = 0; i < 100 && run.Match.TurnIndex == 1; i++) Step(run);
                Assert.That(run.Match.TurnIndex, Is.EqualTo(2));
                CheckImmune(run, false);
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(driver.Director.CaptureSnapshot()));
            var restart = driver.Director.Restart();
            Assert.That(restart.Accepted, Is.True, restart.Detail);
            Assert.That(Read(driver).Round.Timing.Effects, Is.Empty);
        }

        private static void CheckImmune(PveTestDriver driver, bool expected)
        {
            var state = Read(driver);
            var enemy = driver.Composed.Enemies.Single().HeroId;
            var blue = driver.Composed.Heroes.Single();
            var rules = RuleModificationQuery.Build(state.Round.Timing, state.Board, state.Participants,
                state.Round.TieBreakerState, UnlockedUltimateSet.None, RuleObject.None, state.Round.HeroItems, null,
                driver.Composed.InitialState.ModeRules);
            Assert.That(state.Round.Timing.Effects.Any(effect => effect.Definition.DefinitionId == PveSkillContent.ImmuneDefinition.DefinitionId &&
                effect.Phase == ActiveEffectPhase.Active), Is.EqualTo(expected));
            foreach (var activity in new[] { RuleActivity.Attack, RuleActivity.Skill, RuleActivity.ObjectPush })
            {
                Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(enemy), activity, RuleObject.ForHero(blue),
                    state.Board, state.Participants, rules), Is.EqualTo(expected));
                Assert.That(ImmunityQuery.IsImmuneTo(RuleObject.ForHero(enemy), activity, RuleObject.ForHero(enemy),
                    state.Board, state.Participants, rules), Is.False);
            }
        }

        private static RestoredAuthoritativeMatch Read(PveTestDriver driver)
        {
            var initial = driver.Composed.InitialState;
            return AuthoritativeSnapshotSerializer.Deserialize(initial.InitialConfiguration.ContentCatalogs,
                driver.Director.Projection.AuthoritativeSnapshot, initial.Scenario.CardDefinitions, initial.AvailableMaps,
                initial.Setup.Minions, modeRules: initial.ModeRules);
        }

        private static void Step(PveTestDriver driver)
        {
            var step = driver.Session.Peek();
            var result = step.Kind == PveStepKind.Command ? driver.Session.Step(out _) : driver.Session.Execute(driver.PlayerAnswer());
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static PveTestDriver Create()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-9,7"), GuardsDigital.Core.Setup.StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-5,3") },
                new[] { new PveSpawn(1, PveTestContent.Hex("-4,2"), "melee") });
            return PveTestDriver.StartWithOpening(new[] { "X" }, new[] { "hero-brogan" }, encounter, catalog);
        }
    }
}
