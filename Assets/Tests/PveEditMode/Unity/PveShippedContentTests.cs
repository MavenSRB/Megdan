using System.Linq;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-021, PVE-D15/D17. The JSON shipped beside the executable loads, validates against the
    /// official map, carries the approved placement layout and is the same placeholder content the
    /// engine-free tests state in code.
    /// </summary>
    public sealed class PveShippedContentTests
    {
        [Test]
        public void TheShippedContentLoadsAndValidates()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var encounter);

            Assert.That(PveContentValidator.Validate(catalog, encounter, PveTestContent.OfficialMap()), Is.Empty);
            Assert.That(catalog.ContentStatus, Is.EqualTo("placeholder-test"));
            Assert.That(catalog.Types.Select(type => type.MinionCategory.ToString()),
                Is.EqualTo(new[] { "Melee", "Ranged", "None" }));
        }

        [Test]
        public void TheShippedLayoutIsTheApprovedOne()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out _, out var shipped);
            var approved = PveTestContent.Encounter();

            Assert.That(shipped.MapReference, Is.EqualTo(approved.MapReference));
            Assert.That(shipped.Creeps.Select(creep => (creep.Coordinate, creep.MinionType)),
                Is.EqualTo(approved.Creeps.Select(creep => (creep.Coordinate, creep.MinionType))));
            Assert.That(shipped.HeroPlacement, Is.EqualTo(approved.HeroPlacement));
            Assert.That(shipped.Spawns.Select(spawn => (spawn.Round, spawn.Coordinate)),
                Is.EqualTo(approved.Spawns.Select(spawn => (spawn.Round, spawn.Coordinate))));
            Assert.That(shipped.Spawns.Select(spawn => spawn.EnemyTypeId), Is.EqualTo(new[]
                { "melee", "ranged", "melee", "ranged", "melee", "ranged", "melee", "heavy", "ranged", "heavy", "ranged" }));
        }

        [Test]
        public void TheShippedEnemiesAreThePlaceholderTypes()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var shipped, out _);
            var stated = PveTestContent.Catalog();

            Assert.That(shipped.Types.Select(type => type.Id), Is.EqualTo(new[] { "melee", "ranged", "heavy" }));
            foreach (var pair in new[] { ("melee", "raider"), ("heavy", "brute") })
            {
                var type = stated.Find(pair.Item2);
                var loaded = shipped.Find(pair.Item1);
                Assert.That(loaded.OrangeCards.Where(card => !card.IsIdle && card.Skill == PveSkillKind.None).Select(card => (card.Key, card.Initiative, card.AttackValue, card.AttackRange, card.MoveValue)),
                    Is.EqualTo(type.OrangeCards.Where(card => !card.IsIdle).Select(card => (card.Key, card.Initiative, card.AttackValue, card.AttackRange, card.MoveValue))));
                if (pair.Item1 == "heavy")
                {
                    Assert.That(loaded.Plans.Select(plan => string.Join(" ", plan)),
                        Is.EqualTo(type.Plans.Select(plan => string.Join(" ", plan))), type.Id);
                    Assert.That(loaded.OrangeCards.Select(Describe), Is.EqualTo(type.OrangeCards.Select(Describe)));
                }
                if (pair.Item1 == "heavy")
                    Assert.That(loaded.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense)),
                        Is.EqualTo(type.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense))), type.Id);
            }
            Assert.That(shipped.Types.Select(type => type.TokenType.ToString()), Is.EqualTo(new[] { "Melee", "Ranged", "Heavy" }));
            foreach (var typeId in new[] { "melee", "ranged" })
            {
                var preferred = typeId == "melee" ? PveTargetKind.Minion : PveTargetKind.Hero;
                foreach (var card in shipped.Find(typeId).OrangeCards)
                {
                    var attacks = card.Options.Where(option => option.Kind == PveOptionKind.Attack).ToArray();
                    if (attacks.Length > 0) Assert.That(attacks[0].Target, Is.EqualTo(preferred), card.Name);
                    foreach (var move in card.Options.Where(option => option.Kind == PveOptionKind.Move))
                        Assert.That(move.Target, Is.EqualTo(preferred), card.Name);
                }
            }
            var ranged = shipped.Find("ranged");
            Assert.That(ranged.OrangeCards.Select(Describe), Is.EqualTo(new[]
            {
                "A/8/1//0/2/False/Move:Hero:2",
                "B/6/1/3/3/1/False/Attack:Hero:1,Attack:Minion:1,Move:Hero:2",
                "C/3/1/5/3//False/Attack:Hero:1,Attack:Minion:1",
                "X/1/1//0//False/"
            }));
            Assert.That(ranged.PurpleCards, Is.Empty);
            foreach (var typeId in new[] { "melee", "ranged" })
                Assert.That(shipped.Find(typeId).Plans.Select(plan => string.Join(" ", plan)),
                    Is.EqualTo(new[] { "A B C X", "A C B X", "A B X C" }));
        }

        [TestCase("melee")]
        [TestCase("ranged")]
        [TestCase("heavy")]
        public void EveryHandAndPlanHasExactlyTwoAttackCards(string typeId)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var type = catalog.Find(typeId);
            Assert.That(type.OrangeCards.Where(card => card.AttackValue.HasValue).Sum(card => card.Copies), Is.EqualTo(2));
            Assert.That(type.OrangeHand.Count, Is.EqualTo(4));
            Assert.That(type.FindOrange("X").Initiative, Is.EqualTo(typeId == "melee" ? 9 : 1));
            Assert.That(type.FindOrange("X").IsIdle, Is.EqualTo(typeId == "heavy"));
            foreach (var plan in type.Plans)
            {
                Assert.That(plan.Count, Is.EqualTo(4));
                Assert.That(plan.Count(key => type.FindOrange(key).AttackValue.HasValue), Is.EqualTo(2));
                Assert.That(plan, Is.EquivalentTo(type.OrangeHand));
            }
            var hand = GuardsDigital.Pve.Composition.PveCardFactory.CreateHand(type,
                new GuardsDigital.Core.Units.Heroes.HeroId("test-enemy"), "enemy");
            Assert.That(hand.DealtDefinitions.Count(definition => definition.ActionDefinitions.Any(action =>
                action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack)), Is.EqualTo(2));
            Assert.That(hand.Instances.Count, Is.EqualTo(4 + type.PurpleCards.Count));
        }

        [TestCase("melee", 0)]
        [TestCase("ranged", 0)]
        [TestCase("heavy", 2)]
        public void OnlyHeavyHasTwoDefenseCards(string typeId, int expectedCount)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var type = catalog.Find(typeId);
            Assert.That(type.PurpleCards.Count, Is.EqualTo(expectedCount));
            if (expectedCount == 2)
                Assert.That(type.PurpleCards.Select(card => card.Defense), Is.EqualTo(new[] { 3, 5 }));
            var hand = GuardsDigital.Pve.Composition.PveCardFactory.CreateHand(type,
                new GuardsDigital.Core.Units.Heroes.HeroId("test-enemy"), "enemy");
            Assert.That(hand.DealtDefinitions.Count(definition => definition.ActionDefinitions.Any(action =>
                action.Kind == GuardsDigital.Core.Cards.ActionKind.Defense)), Is.EqualTo(expectedCount));
            Assert.That(hand.Instances.Count, Is.EqualTo(4 + expectedCount));
        }

        [TestCase("melee", true)]
        [TestCase("melee", false)]
        [TestCase("ranged", true)]
        [TestCase("ranged", false)]
        [TestCase("heavy", true)]
        [TestCase("heavy", false)]
        public void WeakAttackAttacksInReachOtherwiseMoves(string typeId, bool inReach)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var type = catalog.Find(typeId);
            var weak = type.OrangeCards.Where(card => card.AttackValue.HasValue)
                .OrderBy(card => card.AttackValue.Value).First();
            Assert.That(weak.MoveValue, Is.GreaterThan(0));
            Assert.That(weak.Options.Last().Kind, Is.EqualTo(PveOptionKind.Move));
            Assert.That(weak.Options.Take(weak.Options.Count - 1).All(option => option.Kind == PveOptionKind.Attack), Is.True);
            var plan = new[] { weak.Key }.Concat(type.OrangeHand.Where(key => key != weak.Key)).ToArray();
            var immediate = new PveEnemyType(type.Id, type.DisplayName, type.Level,
                type.OrangeCards, type.PurpleCards, new[] { plan }, type.TokenType, type.MinionCategory);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(PveTestContent.Hex("-3,2"), GuardsDigital.Core.Setup.StandardMinionType.Melee) },
                new[] { PveTestContent.Hex("-5,3") },
                new[] { new PveSpawn(1, PveTestContent.Hex(inReach ? "-4,2" : "-1,-2"), typeId) });
            var driver = PveTestDriver.StartWithOpening(new[] { weak.Key }, heroes: new[] { "hero-brogan" }, encounter: encounter,
                catalog: new PveEnemyCatalog("placeholder-test", new[] { immediate }));
            for (var index = 0; index < 200; index++)
            {
                var step = driver.Session.Peek();
                if (driver.Session.Controller.IsEnemy(driver.Match.SelectedActor.ActorId) &&
                    step.Kind == GuardsDigital.Pve.Play.PveStepKind.Command && step.Command.ActionDefinitionId.IsDefined)
                {
                    Assert.That(driver.Match.RoundIndex, Is.EqualTo(1));
                    Assert.That(driver.Match.TurnIndex, Is.EqualTo(1), "The first plan slot is the weaker attack.");
                    Assert.That(step.Command.ActionDefinitionId, Is.EqualTo(inReach
                        ? GuardsDigital.Pve.Composition.PveCardFactory.AttackActionId
                        : GuardsDigital.Pve.Composition.PveCardFactory.MovementActionId));
                    Assert.That(driver.Session.Step(out _).Accepted, Is.True);
                    return;
                }
                Assert.That(driver.Advance(), Is.True, "The fixture must reach the enemy action.");
            }
            Assert.Fail("No enemy action was reached.");
        }

        private static string Describe(PveOrangeCard card) =>
            $"{card.Key}/{card.Initiative}/{card.Copies}/{card.AttackValue}/{card.AttackRange}/{card.MoveValue}/" +
            $"{card.IsIdle}/" + string.Join(",", card.Options.Select(option =>
                option.Kind + ":" + option.Target + ":" + option.StopDistance));

        [TestCase("melee", true)]
        [TestCase("ranged", false)]
        public void AttackPriorityChoosesThePreferredKindWhenBothAreLegal(string typeId, bool creepFirst)
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var layout);
            var type = catalog.Find(typeId);
            var immediateAttack = new PveEnemyType(type.Id, type.DisplayName, type.Level,
                type.OrangeCards, type.PurpleCards, new[] { new[] { "B", "A", "C", "X" } }, type.TokenType, type.MinionCategory);
            var encounter = new PveEncounterDefinition(layout.MapId, layout.MapRevision,
                new[] { new PveCreepPlacement(new GuardsDigital.Core.Board.HexCoordinate(-3, 2), GuardsDigital.Core.Setup.StandardMinionType.Melee) },
                new[] { new GuardsDigital.Core.Board.HexCoordinate(-5, 3) },
                new[] { new PveSpawn(1, new GuardsDigital.Core.Board.HexCoordinate(-4, 2), typeId) });
            var driver = PveTestDriver.StartWithOpening(new[] { "B" }, heroes: new[] { "hero-brogan" }, encounter: encounter,
                catalog: new PveEnemyCatalog("placeholder-test", new[] { immediateAttack }));
            for (var index = 0; index < 200; index++)
            {
                var pending = driver.Match.PendingAttackTargetChoice;
                var step = driver.Session.Peek();
                if (pending != null && driver.Session.Controller.IsEnemy(pending.ChooserId))
                {
                    Assert.That(pending.LegalTargetHeroIds, Is.Not.Empty);
                    Assert.That(pending.LegalTargetMinionIds, Is.Not.Empty);
                    Assert.That(step.Kind, Is.EqualTo(GuardsDigital.Pve.Play.PveStepKind.Command));
                    Assert.That(step.Command.MinionId.IsDefined, Is.EqualTo(creepFirst));
                    Assert.That(step.Command.HeroId.IsDefined, Is.EqualTo(!creepFirst));
                    return;
                }
                if (step.Kind == GuardsDigital.Pve.Play.PveStepKind.Command)
                    Assert.That(driver.Session.Step(out _).Accepted, Is.True);
                else
                {
                    var answer = driver.PlayerAnswer();
                    Assert.That(answer, Is.Not.Null);
                    Assert.That(driver.Session.Execute(answer).Accepted, Is.True);
                }
            }
            Assert.Fail("The enemy never reached its attack target choice.");
        }
    }
}
