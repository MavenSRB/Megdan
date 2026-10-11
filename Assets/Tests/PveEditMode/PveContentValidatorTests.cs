using System.Linq;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>W-021, PVE-D07/D09/D15/D17. Invalid enemy or encounter content is refused readably.</summary>
    public sealed class PveContentValidatorTests
    {
        [Test]
        public void ThePlaceholderContentAndApprovedLayoutAreValid()
        {
            Assert.That(PveContentValidator.Validate(
                    PveTestContent.Catalog(), PveTestContent.Encounter(), PveTestContent.OfficialMap()),
                Is.Empty);
        }

        [Test]
        public void APlanMustUseEachOrangeCardExactlyOnce()
        {
            var raider = PveTestContent.Catalog().Find("raider");
            var broken = new PveEnemyCatalog("placeholder-test", new[]
            {
                new PveEnemyType("raider", "Raider", 1, raider.OrangeCards, raider.PurpleCards,
                    new[] { new[] { "A", "B", "C", "C" } })
            });

            var errors = PveContentValidator.Validate(broken, PveTestContent.Encounter(), PveTestContent.OfficialMap());

            Assert.That(errors, Has.Some.Contains("plan 1 (A B C C) must use each of its orange cards exactly once"));
        }

        [Test]
        public void ATypeHoldsFourOrangeCardsAtLeastThreeActingAndAtMostTwoPurple()
        {
            var brute = PveTestContent.Catalog().Find("brute");
            var broken = new PveEnemyCatalog("placeholder-test", new[]
            {
                new PveEnemyType("brute", "Brute", 1,
                    brute.OrangeCards.Where(card => card.Key != "A")
                        .Concat(new[] { new PveOrangeCard("Y", "Nap", 2, 1, null, 0, null, null, true) }),
                    brute.PurpleCards.Concat(new[] { new PvePurpleCard("R", "Shell", 1, 2) }),
                    new[] { new[] { "B", "X", "Y", "C" } }),
                PveTestContent.Catalog().Find("raider")
            });

            var errors = PveContentValidator.Validate(broken, PveTestContent.Encounter(), PveTestContent.OfficialMap());

            Assert.That(errors, Has.Some.Contains("has 2 acting orange cards"));
            Assert.That(errors, Has.Some.Contains("has 3 purple cards; at most 2"));
        }

        [Test]
        public void AnAttackOptionNeedsAnAttackValueAndAnIdleCardStatesNothing()
        {
            var broken = new PveEnemyCatalog("placeholder-test", new[]
            {
                new PveEnemyType("raider", "Raider", 1,
                    new[]
                    {
                        new PveOrangeCard("A", "Advance", 9, 2, null, 0, 3, new[]
                        {
                            new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero)
                        }),
                        new PveOrangeCard("B", "Slash", 7, 1, 3, 0, null, new[]
                        {
                            new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero)
                        }),
                        new PveOrangeCard("X", "Nap", 1, 1, 2, 0, null, null, true)
                    },
                    null,
                    new[] { new[] { "A", "B", "X", "A" } })
            });

            var errors = PveContentValidator.Validate(broken, PveTestContent.Encounter(), PveTestContent.OfficialMap());

            Assert.That(errors, Has.Some.Contains("card 'A' attacks and needs an Attack value"));
            Assert.That(errors, Has.Some.Contains("card 'A' states a Movement value but lists no move option"));
            Assert.That(errors, Has.Some.Contains("card 'X' is idle and must state no option"));
        }

        [Test]
        public void EncounterSpacesMustBePlayableDistinctAndNameKnownTypes()
        {
            var approved = PveTestContent.Encounter();
            var broken = new PveEncounterDefinition(
                approved.MapId,
                approved.MapRevision,
                approved.Creeps,
                approved.HeroPlacement.Concat(new[] { approved.Creeps[0].Coordinate, PveTestContent.Hex("-4,3") }),
                approved.Spawns.Concat(new[] { new PveSpawn(2, PveTestContent.Hex("1,1"), "ogre") }));

            var errors = PveContentValidator.Validate(PveTestContent.Catalog(), broken, PveTestContent.OfficialMap());

            Assert.That(errors, Has.Some.Contains("A hero placement space at -3,2 shares its space with A creep"));
            Assert.That(errors, Has.Some.Contains("at -4,3 is not a playable space"));
            Assert.That(errors, Has.Some.Contains("names unknown enemy type 'ogre'"));
        }

        [Test]
        public void SetupRefusesDuplicateUnknownAndTooManyHeroes()
        {
            var setup = new PveEncounterSetup(
                new[] { "hero-arien", "hero-arien", "hero-nobody", "hero-wasp", "hero-brogan", "hero-tigerclaw", "hero-xargatha" }, 7);

            var problems = PveEncounterComposer.ValidateSetup(setup, PveTestContent.Encounter());

            Assert.That(problems, Has.Some.Contains("Choose at most 5 heroes"));
            Assert.That(problems, Has.Some.Contains("Hero 'hero-arien' is chosen more than once"));
            Assert.That(problems, Has.Some.Contains("Hero 'hero-nobody' is not a configured hero"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void PlayableBlueTeamRequiresExactlyThreeHeroes(int count)
        {
            var heroes = new[] { "hero-arien", "hero-brogan", "hero-wasp", "hero-tigerclaw", "hero-xargatha" };
            var problems = PveEncounterComposer.ValidatePlayerSetup(
                new PveEncounterSetup(System.Linq.Enumerable.Take(heroes, count), 7), PveTestContent.Encounter());
            Assert.That(problems.Count == 0, Is.EqualTo(count == 3));
            if (count != 3) Assert.That(problems, Has.Some.Contains("Choose exactly 3 heroes for Blue"));
        }
    }
}
