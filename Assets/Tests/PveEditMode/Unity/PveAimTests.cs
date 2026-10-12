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
    public sealed class PveAimTests
    {
        [Test]
        public void ApprovedFacesKeepInitiativesMovementAndPrintedAttackRange()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var ranged = catalog.Find("ranged");
            var move = ranged.FindOrange("A");
            Assert.That(move.Name, Is.EqualTo("Take Position"));
            Assert.That(move.Initiative, Is.EqualTo(8));
            Assert.That(move.MoveValue, Is.EqualTo(2));
            Assert.That(move.Options.Single().StopDistance, Is.EqualTo(2));
            var aim = ranged.FindOrange("X");
            Assert.That(aim.Name, Is.EqualTo("Aim"));
            Assert.That(aim.Initiative, Is.EqualTo(1));
            Assert.That(aim.Skill, Is.EqualTo(PveSkillKind.RangeNextTurn));
            var definition = PveCardFactory.CreateDefinitions(ranged).Single(card => card.Id.Equals(PveCardFactory.OrangeId(ranged, aim)));
            Assert.That(CurrentRoundCards.ReadFace(definition).PrintedText, Is.EqualTo("Next turn, have +1 Range."));
            Assert.That(ranged.OrangeCards.Where(card => card.AttackValue.HasValue).Select(card => card.AttackRange),
                Is.EqualTo(new[] { 3, 3 }));
            Assert.That(MatchContentCatalogs.ForThisBuild().Capabilities.IsRegistered(PveSkillContent.RangeCapability), Is.False);
        }

        [TestCase("B")]
        [TestCase("C")]
        public void AimIsDormantThenAllowsAnActualRangeFourAttackAndExpires(string attack)
        {
            var driver = Create(attack);
            Reach(driver, () => driver.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.SkillActionId) == true);
            var before = driver.Director.CaptureSnapshot();
            Step(driver);
            CheckBonus(driver, 0);
            var aimId = driver.Match.Cards.Single(card => card.OwnerId.Equals(driver.Composed.Enemies.Single().HeroId) &&
                card.DefinitionReference.DefinitionId.Value == "pve-ranged-x").Id;
            Assert.That(driver.Match.StandingActiveEffectSourceCardInstanceIds, Does.Contain(aimId), "Pending Next Turn effects retain their source glow.");
            Assert.That(Read(driver).Round.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Dormant));
            var after = driver.Director.CaptureSnapshot();
            Assert.That(driver.Director.Undo().Accepted, Is.True);
            Assert.That(driver.Director.CaptureSnapshot(), Is.EqualTo(before));
            Step(driver);
            Assert.That(driver.Director.CaptureSnapshot(), Is.EqualTo(after));
            var restored = Create(attack);
            var restore = restored.Director.RestoreSnapshot(after);
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            foreach (var run in new[] { driver, restored })
            {
                Reach(run, () => run.Match.TurnIndex == 2 &&
                    run.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.AttackActionId) == true);
                CheckBonus(run, 1);
                var authority = (GuardsDigital.Application.MatchSessionProjection)run.Match;
                Assert.That(authority.CardValues, Is.Empty, "Display modifiers are disabled in this fixture.");
                Assert.That(authority.ModeCardRuleValues.Any(value => value.Channel == PrintedValueChannel.Range && value.Bonus == 1), Is.True);
                Assert.That(run.Match.IsActiveEffectSource(aimId), Is.True);
                Step(run);
                Assert.That(run.Match.PendingAttackTargetChoice, Is.Not.Null);
                Assert.That(run.Match.PendingAttackTargetChoice.LegalTargetHeroIds,
                    Does.Contain(run.Composed.Heroes.Single()), "The hero is four hexes away, outside printed Range 3.");
                Reach(run, () => run.Match.TurnIndex == 3);
                CheckBonus(run, 0);
                Assert.That(run.Match.IsActiveEffectSource(aimId), Is.False);
                Assert.That(run.Match.StandingActiveEffectSourceCardInstanceIds.Contains(aimId), Is.False);
            }
            Assert.That(restored.Director.CaptureSnapshot(), Is.EqualTo(driver.Director.CaptureSnapshot()));
        }

        [Test]
        public void CardRetrievalCancelsTheDormantNextTurnBonus()
        {
            var driver = Create("B");
            Reach(driver, () => driver.Session.Peek().Command?.ActionDefinitionId.Equals(PveCardFactory.SkillActionId) == true);
            Step(driver);
            var state = Read(driver);
            state.Round.Timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(TimingCheckpoint.CardsRetrieved, 1, 4),
                state.Round.TieBreakerState);
            Assert.That(state.Round.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
        }

        private static void CheckBonus(PveTestDriver driver, int amount)
        {
            var state = Read(driver);
            var rules = RuleModificationQuery.Build(state.Round.Timing, state.Board, state.Participants,
                state.Round.TieBreakerState, UnlockedUltimateSet.None, RuleObject.None, state.Round.HeroItems, null,
                driver.Composed.InitialState.ModeRules);
            Assert.That(rules.ValueContributions(RuleObject.ForHero(driver.Composed.Enemies.Single().HeroId),
                PrintedValueChannel.Range).Sum(value => value.Amount), Is.EqualTo(amount));
        }

        private static void Reach(PveTestDriver driver, Func<bool> reached)
        {
            for (var i = 0; i < 150 && !reached(); i++) Step(driver);
            Assert.That(reached(), Is.True);
        }

        private static void Step(PveTestDriver driver)
        {
            var result = driver.Session.Peek().Kind == PveStepKind.Command
                ? driver.Session.Step(out _) : driver.Session.Execute(driver.PlayerAnswer());
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static RestoredAuthoritativeMatch Read(PveTestDriver driver)
        {
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(driver.Director.CaptureSnapshot(), out var envelope), Is.True);
            var initial = driver.Composed.InitialState;
            return AuthoritativeSnapshotSerializer.Deserialize(initial.InitialConfiguration.ContentCatalogs,
                envelope.AuthoritativePayload, initial.Scenario.CardDefinitions, initial.AvailableMaps,
                initial.Setup.Minions, modeRules: initial.ModeRules);
        }

        private static PveTestDriver Create(string attack)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-9,7"), GuardsDigital.Core.Setup.StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-5,3") },
                new[] { new PveSpawn(1, PveTestContent.Hex("-1,1"), "ranged") });
            for (ulong seed = 1; seed < 1000; seed++)
            {
                var driver = PveTestDriver.Start(seed, new[] { "hero-brogan" }, encounter, catalog);
                if (PvePlans.PlanFor(driver.Composed, driver.Composed.Enemies.Single(), 1).Take(2)
                    .SequenceEqual(new[] { "X", attack })) return driver;
            }
            throw new InvalidOperationException("No opening seed found.");
        }
    }
}
