using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61j, the milestone that DEALS Min the Dragonmonk as the nineteenth hero: the half
    /// of a dealing that is about the CONFIGURATION rather than about the content - the selector row,
    /// the hand dispatch, the three staged stagings, the registration flip, and the engaged board
    /// 8.61k's journeys will be written on.
    ///
    /// <para>Milestone 8.35z41's rule is what the first case asserts from the roster's own side: the
    /// offerable roster and <c>MatchConfiguration</c>'s hand switch are two halves of one claim, and a
    /// hero present in the first but absent from the second is a pick that throws at the moment the
    /// match composes.</para>
    /// </summary>
    public sealed class MinDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedMinCarriesHerEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var min = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == MinCardContent.ContentSetId);
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(26),
                "She was the nineteenth hero this build could put on a board; milestone 8.69g " +
                "deals Brynn as the twentieth, 8.71h Hanu as the twenty-first and 8.75b Ignatia " +
                "as the twenty-second.");
            Assert.That(min.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.MinHeroIdValue));
            Assert.That(min.DisplayName, Is.EqualTo("Min the Dragonmonk"),
                "Setup step 7's own name, which is a label on the seat rather than an identity.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { min }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick
                : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);

            var cards = configuration.CardInstances
                .Where(card => card.OwnerId.Value == min.HeroIdValue)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(18),
                "Eighteen dealt cards; Chop Down is p.20's boxed Handicap and is not one.");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MinCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MinCardContent.Deck));

            Assert.That(
                HeroLineupComposition.Compose(format, lineup).Heroes
                    .Single(hero => hero.ContentId.Value == MinCardContent.ContentSetId)
                    .SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"),
                "A selected hero takes the seat and the marked point of the position it was chosen " +
                "into; the hero column moves and no seat does.");
        }

        [TestCase(1, MinUpgradeBranch.FirstSibling)]
        [TestCase(2, MinUpgradeBranch.FirstSibling)]
        [TestCase(2, MinUpgradeBranch.SecondSibling)]
        [TestCase(3, MinUpgradeBranch.FirstSibling)]
        [TestCase(3, MinUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksTheUltimate(
            int tier, MinUpgradeBranch branch)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedMinForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedMinTierTwoForInternalTest(
                    TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedMinTierThreeForInternalTest(TeamSide.Blue, branch);

            var ids = MinCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
            var cards = configuration.CardInstances
                .Where(card => ids.Contains(card.DefinitionReference.DefinitionId))
                .ToArray();

            Assert.That(
                cards.Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(ids),
                "Every dealt face of hers is in the staged match, whatever the stage.");
            Assert.That(cards.Select(card => card.OwnerId).Distinct().Count(), Is.EqualTo(1),
                "and they all belong to one seat.");
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5),
                "p.15 closes a level-up with exactly five cards, one of each colour, in hand.");

            var upgraded =
                tier == 1
                    ? new[]
                    {
                        MinCardContent.CraneStance,
                        MinCardContent.TripMine,
                        MinCardContent.Poof
                    }
                    : tier == 2 && branch == MinUpgradeBranch.FirstSibling
                        ? new[]
                        {
                            MinCardContent.TigerStance,
                            MinCardContent.ClusterMine,
                            MinCardContent.Vanish
                        }
                        : tier == 2
                            ? new[]
                            {
                                MinCardContent.ViperStance,
                                MinCardContent.DeathGrenade,
                                MinCardContent.InnerStrength
                            }
                            : branch == MinUpgradeBranch.FirstSibling
                                ? new[]
                                {
                                    MinCardContent.DragonStance,
                                    MinCardContent.MineField,
                                    MinCardContent.Ruse
                                }
                                : new[]
                                {
                                    MinCardContent.CobraStance,
                                    MinCardContent.HolyDeathGrenade,
                                    MinCardContent.PerfectSelf
                                };

            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[]
                {
                    MinCardContent.FastAsLightning,
                    MinCardContent.SmokeBomb
                })),
                "Both basics stay in hand at every stage, exactly as p.6 leaves them - and for her " +
                "that is load-bearing twice over: Fast as Lightning's whole rider is a reading of " +
                "the RED card she is holding, and the Smoke bomb is the piece four other faces of " +
                "hers name.");

            var ultimate = cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(MinCardContent.FlurryOfBlows));
            Assert.That(
                ultimate.State.Location,
                Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck),
                "p.16 puts the Ultimate beside the dashboard only once it is unlocked, and her " +
                "permanent ability triggers after an attack action exactly while it is there.");
        }

        /// <summary>
        /// THE STAGED BOARD, MEASURED AND RECORDED. Milestone 8.40j's own dealing is why: it found
        /// every Attack face of a hero offering minions only, because the two heroes stood two spaces
        /// apart around a minion and the gap was bent rather than straight - a fact about the board
        /// that decided which journeys were possible and which were not.
        ///
        /// <para>What 8.61k needs from this board is four things her set asks for, and this case
        /// measures all four rather than restating a coordinate: an ADJACENT ENEMY UNIT for her five
        /// Stances (which need one twice over - as the Attack's target and as the push's subject),
        /// EMPTY SPACES IN RADIUS 3 for her three mine faces (Mine Field places three at once), an
        /// EMPTY SPACE IN RADIUS 2 ADJACENT TO THE ENEMY HERO for both Grenades, and an EMPTY SPACE
        /// IN RADIUS 3 for the Smoke bomb. The coordinates are written to the test output so the
        /// next milestone starts from a known board rather than from a search.</para>
        /// </summary>
        [Test]
        public void TheEngagedStagedBoard_CarriesEverythingHerFacesAskFor()
        {
            var configuration = MatchConfiguration.CreateStagedMinForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusMinOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var min = setup.Heroes
                .Single(hero => hero.ContentId.Value == MinCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(min.HeroId));

            var separation = HexDistanceQuery.Calculate(min.Coordinate, enemy.Coordinate);
            var adjacentUnits = setup.Minions
                .Where(minion => HexDistanceQuery.Calculate(min.Coordinate, minion.Coordinate) == 1)
                .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate) +
                    " " + minion.TeamSide)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            bool IsFree(HexCoordinate coordinate) =>
                map.Spaces.Any(space =>
                    space.Coordinate.Equals(coordinate) &&
                    space.Kind == BoardMapSpaceKind.Playable) &&
                !setup.Minions.Any(minion => minion.Coordinate.Equals(coordinate)) &&
                !setup.Heroes.Any(hero => hero.Coordinate.Equals(coordinate));

            var freeWithin = new Dictionary<int, HexCoordinate[]>();
            foreach (var radius in new[] { 2, 3 })
            {
                freeWithin[radius] = map.Spaces
                    .Select(space => space.Coordinate)
                    .Where(coordinate =>
                        HexDistanceQuery.Calculate(min.Coordinate, coordinate) <= radius &&
                        !coordinate.Equals(min.Coordinate) &&
                        IsFree(coordinate))
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            var freeBesideTheEnemy = enemy.Coordinate.GetNeighbors()
                .Where(IsFree)
                .ToArray();
            var grenadeSpaces = freeBesideTheEnemy
                .Where(coordinate =>
                    HexDistanceQuery.Calculate(min.Coordinate, coordinate) <= 2)
                .ToArray();

            TestContext.WriteLine(
                "MIN STAGED BOARD (engaged, one seat per team): Min " + Describe(min.Coordinate) +
                ", enemy " + Describe(enemy.Coordinate) + ", separation " + separation +
                ". Units adjacent to her: " +
                (adjacentUnits.Length == 0 ? "(none)" : string.Join(", ", adjacentUnits)) +
                ". Free spaces in radius 2: " + freeWithin[2].Length +
                "; in radius 3: " + freeWithin[3].Length +
                ". Free neighbours of the enemy hero: " +
                string.Join(", ", freeBesideTheEnemy.Select(Describe)) +
                "; of those, inside her Radius 2: " +
                string.Join(", ", grenadeSpaces.Select(Describe)) + ".");

            Assert.That(min.Coordinate, Is.EqualTo(new HexCoordinate(-3, 1)),
                "The staged hero's space is a fact about EngagedMirrorStaging rather than about her, " +
                "and StagedHeroBoard.HeroAt says the same thing.");
            Assert.That(separation, Is.EqualTo(2),
                "Two spaces apart, which is the engagement every staged dealing since Bain's has " +
                "been measured on: inside every Radius her set prints and outside adjacency, so her " +
                "Stances need a step or a minion first and her mines and Grenades do not.");

            // (1) Her five Stances want an ADJACENT ENEMY unit, and want it twice: "Target a unit
            //     adjacent to you" and then "Push an enemy unit adjacent to you".
            Assert.That(
                adjacentUnits.Select(value => value.Split('@')[0]),
                Is.EquivalentTo(StagedHeroBoard.StagedBesideTheHero),
                "The two pieces the engaged staging leaves beside the staged hero are the same two " +
                "every other hero's fixtures measure.");
            Assert.That(
                setup.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(min.Coordinate, minion.Coordinate) == 1 &&
                    minion.TeamSide != setup.Heroes
                        .Single(hero => hero.HeroId.Equals(min.HeroId)).TeamSide),
                Is.True,
                "and at least one of them is an ENEMY, or all five of her Stances would attack a " +
                "friend and push nobody.");

            // (2) Her three mine faces place up to THREE tokens at once, in the printed Radius 3.
            Assert.That(freeWithin[3], Has.Length.GreaterThanOrEqualTo(3),
                "Mine Field places 2 blasts and 1 dud in radius; fewer than three empty spaces " +
                "would make the board rather than the card decide how many happen.");

            // (3) Her Silver basic places one Smoke bomb in the same printed Radius 3.
            Assert.That(freeWithin[3], Is.Not.Empty);

            // (4) Both Grenades place into Radius 2 and then ask about enemy heroes ADJACENT to the
            //     token, so the board has to hold a space that is both.
            Assert.That(grenadeSpaces, Is.Not.Empty,
                "Death Grenade prints Radius 2 and its end-of-turn sentence names enemy heroes " +
                "adjacent to the token; without a space that is inside her radius AND beside him, " +
                "the face can be played and can never reach anybody.");
        }

        private static string Describe(HexCoordinate coordinate) =>
            "(" + coordinate.Q + ", " + coordinate.R + ")";

        // Her ARTWORK half is asserted in `ConfiguredCardArtworkCatalogTests` rather than here, and
        // the reason is structural: `ConfiguredCardArtworkCatalog` lives in the Presentation slice the
        // offline harness compiles only one file of, so a case naming it would make this whole fixture
        // Unity-only - and the claims above are exactly the ones worth being able to run in eleven
        // minutes. See that file's `EveryDealtFaceOfMins_HasAnArtworkRowAtHerOwnRevision`.

        /// <summary>
        /// Her eleven decision labels reach the one catalog presentation asks, which is where a dealing
        /// puts a hero's table - milestone 8.46a aggregates per hero exactly as
        /// <c>MatchContentCatalogs</c> aggregates capability handlers.
        /// </summary>
        [Test]
        public void HerDecisionLabels_AreInTheCatalogPresentationAsks()
        {
            var declared = MinEffectContent.DecisionLabels().ToArray();
            // Milestone 9.1: eight, the Grenades' three "up to N" passes no longer ask (ruling W3).
            Assert.That(declared, Has.Length.EqualTo(8));

            var registered = DecisionLabelCatalog.Registered;
            foreach (var declaration in declared)
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out var label),
                    Is.True,
                    declaration.CardId.Value + " / " + declaration.NodeId.Value +
                    " is declared beside her content and missing from the catalog.");
                Assert.That(label, Is.EqualTo(declaration.Label));
            }

            Assert.That(
                registered.Keys.Count(cardId =>
                    cardId.Value.StartsWith(
                        MinCardContent.ContentSetId + "-", System.StringComparison.Ordinal)),
                Is.EqualTo(declared.Select(declaration => declaration.CardId).Distinct().Count()));
        }

        /// <summary>
        /// THE REGISTRATION FLIP, asserted as one claim: her seventeen own contracts, the two shared
        /// ones that live beside their handler families, her five token definitions and her seven
        /// triggered effects are all in the build's catalogs, and the two ids no composed sentence of
        /// hers reaches are in none of them.
        /// </summary>
        [Test]
        public void TheBuildRegistersEveryContractHerDealtFacesNeed()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            foreach (var id in new[]
            {
                "min-smoke-bomb-present@1", "min-smoke-bomb-space@1", "min-smoke-bomb-in-range@1",
                "min-smoke-bomb-in-radius@1", "min-place-smoke-bomb-in-radius@1",
                "min-place-mine-blast@1", "min-place-mine-dud@1", "min-reclaim-mine@1",
                "min-grenade-present@1", "min-grenade-space@1", "min-place-grenade@1",
                "enemy-hero-beside-grenade@1", "second-enemy-hero-beside-grenade@1",
                "min-death-grenade-at-end-of-turn@1", "min-holy-death-grenade-at-end-of-turn@1",
                "min-inner-strength-double-item-bonuses@1", "min-perfect-self-double-item-bonuses@1",
                "borrow-after-attack-text@1", "take-deck-card-as-item@1"
            })
            {
                Assert.That(
                    build.Capabilities.Registered.Select(capability => capability.ToString()),
                    Has.Member(id),
                    id + " is named by a face a configured match now deals.");
            }

            Assert.That(
                MinComponentContent.Tokens().Select(token => token.Id.Value)
                    .OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "min-grenade", "min-mine-blast", "min-mine-dud", "min-mine-facedown",
                    "min-smoke-bomb-token"
                }));
            foreach (var token in MinComponentContent.Tokens())
            {
                Assert.That(build.Components.IsRegistered(token.Reference), Is.True, token.Id.Value);
            }

            Assert.That(MinComponentContent.TriggeredEffects, Has.Count.EqualTo(7));
            foreach (var effect in MinComponentContent.TriggeredEffects)
            {
                Assert.That(
                    build.TriggeredEffects.Definitions.Select(definition => definition.Id.Value),
                    Has.Member(effect.Id.Value));
            }

            Assert.That(
                build.TriggeredEffects.Definitions.Select(definition => definition.Id.Value)
                    .Where(value => value.StartsWith("min-", System.StringComparison.Ordinal))
                    .OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "min-death-grenade-triggered",
                    "min-flurry-of-blows-effect",
                    "min-holy-death-grenade-triggered",
                    "min-mine-trap-1",
                    "min-mine-trap-2",
                    "min-mine-trap-3",
                    "min-mine-trap-4"
                }),
                "Four mine-trap consequences, both Grenades' end-of-turn bodies and the Ultimate's " +
                "repeat - and nothing else of hers.");
        }

        /// <summary>
        /// The offerable roster and the hand switch are two halves of one claim, read from both sides:
        /// every hero a selector offers actually deals, and her arm is what keeps that true for the
        /// nineteenth. Milestone 8.35z41's own rule, asserted for her by name because 8.35z46 broke it
        /// for a hero who was otherwise completely dealt.
        /// </summary>
        [Test]
        public void EveryOfferableHeroDeals_AndSheIsOneOfThem()
        {
            var dealt = new List<string>();
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                var hand = MatchConfiguration.CreateVerifiedHandForInternalTest(
                    hero.HeroIdValue, "probe");
                Assert.That(hand, Is.Not.Null, hero.HeroIdValue);
                Assert.That(hand.Definitions, Has.Count.EqualTo(18), hero.HeroIdValue);
                dealt.Add(hero.ContentId.Value);
            }

            Assert.That(dealt, Has.Member(MinCardContent.ContentSetId));
            Assert.That(dealt, Has.Count.EqualTo(26));
            Assert.That(dealt.Distinct().Count(), Is.EqualTo(26),
                "and no two rows name one printed set.");
        }
    }
}
