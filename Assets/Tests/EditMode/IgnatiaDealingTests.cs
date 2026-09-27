using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75b. Ignatia the Mad IS DEALT: the twenty-second legal pick, with her printed set
    /// in a configured hand, her eleven contracts and her magma token in the build's catalogs, a
    /// roster row the 8.35z86 picker filters by her FOUR stars and her FIVE printed roles, and the
    /// staged boards her faces are played on.
    ///
    /// <para>This fixture is the OFFLINE half of the dealing. The rendered half - the star and role
    /// buttons actually keeping her button visible on a loaded lobby, in an inspected capture - is
    /// <c>PrivateLobbyPlayModeTests</c>'s, and the played half is 8.75c's journeys.</para>
    ///
    /// <para>Unity-independent apart from the token caption, so it runs in the cross-platform harness
    /// as well as the Windows tier.</para>
    /// </summary>
    public sealed class IgnatiaDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedIgnatiaCarriesHerEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var ignatia = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId);
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(24),
                "She is the twenty-second hero this build can put on a board.");
            Assert.That(
                ignatia.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.IgnatiaHeroIdValue));
            Assert.That(ignatia.DisplayName, Is.EqualTo("Ignatia the Mad"),
                "Setup step 7's own name, which is a label on the seat rather than an identity - the " +
                "DASHBOARD's epithet, where her selection card prints only \"Ignatia\".");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { ignatia }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick
                : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);

            var cards = configuration.CardInstances
                .Where(card => card.OwnerId.Value == ignatia.HeroIdValue)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(18),
                "Eighteen dealt cards; Unreliable Jolt is p.20's boxed Handicap and is not one.");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(IgnatiaCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(IgnatiaCardContent.Deck));

            Assert.That(
                HeroLineupComposition.Compose(format, lineup).Heroes
                    .Single(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId)
                    .SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"),
                "A selected hero takes the seat and the marked point of the position it was chosen " +
                "into; the hero column moves and no seat does.");
        }

        /// <summary>
        /// THE PROFILE FACTS THE PICKER FILTERS BY, asserted at the tier every offline check can see.
        /// She is the FIRST four-star hero this build has ever offered and the first to print five
        /// roles, so this row is the first thing the picker's fourth star can find.
        /// </summary>
        [Test]
        public void HerRosterRow_CarriesTheFourStarsAndTheFivePrintedRoles()
        {
            var ignatia = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId);

            Assert.That(ignatia.Complexity, Is.EqualTo(HeroComplexity.Wizard),
                "FOUR filled stars on her selection card, which p.21 calls Wizard - the top band, " +
                "unused by twenty-one dealings, read off the extracted front rather than inferred.");
            Assert.That(
                ignatia.Roles,
                Is.EqualTo(HeroRole.Sniper | HeroRole.Damager | HeroRole.Tokens |
                    HeroRole.Tactician | HeroRole.Pusher),
                "and the five role icons the same front prints - Sniper, Damager and Tokens on the " +
                "top row, Tactician and Pusher below - in its own printed order.");
            Assert.That(ignatia.Roles & HeroRole.Healer, Is.EqualTo(HeroRole.None),
                "She carries NO Healer role, which is the role the rendered filter case uses to " +
                "prove the filter subtracts as well as adds: selecting it must hide her button.");

            Assert.That(
                RegisteredHeroRoster.ForThisBuild
                    .Count(hero => hero.Complexity == HeroComplexity.Wizard),
                Is.EqualTo(2),
                "and she and Snorri (8.93f) are the two four-star heroes the filter finds.");
        }

        [TestCase(1, IgnatiaUpgradeBranch.FirstSibling)]
        [TestCase(2, IgnatiaUpgradeBranch.FirstSibling)]
        [TestCase(2, IgnatiaUpgradeBranch.SecondSibling)]
        [TestCase(3, IgnatiaUpgradeBranch.FirstSibling)]
        [TestCase(3, IgnatiaUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksTheUltimate(
            int tier, IgnatiaUpgradeBranch branch)
        {
            var configuration = tier == 1
                ? MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Red)
                : tier == 2
                    ? MatchConfiguration.CreateStagedIgnatiaTierTwoForInternalTest(
                        TeamSide.Red, branch)
                    : MatchConfiguration.CreateStagedIgnatiaTierThreeForInternalTest(
                        TeamSide.Red, branch);

            var hers = configuration.CardInstances
                .Where(card => card.DefinitionReference.DefinitionId.Value.StartsWith(
                    IgnatiaCardContent.ContentSetId + "-", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(hers, Has.Length.EqualTo(18));

            var inHand = hers
                .Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

            Assert.That(inHand, Has.Member(IgnatiaCardContent.ChaosBolt),
                "Her Gold basic stays in hand at every stage, exactly as p.6 leaves it.");
            Assert.That(inHand, Has.Member(IgnatiaCardContent.Equilibrium),
                "and so does her Silver - which matters for her beyond the convention, because " +
                "Equilibrium is the one face in the set that frees the coin, so a staged hand " +
                "without it could never reach the pause `ignatia-coin-sun@1` opens.");

            if (tier == 1)
            {
                Assert.That(inHand, Is.EquivalentTo(IgnatiaCardContent.StartingHand));
            }
            else if (tier == 2)
            {
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.ErraticFireblast
                        : IgnatiaCardContent.CrackOfDoom));
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.SpontaneousImmolation
                        : IgnatiaCardContent.SearingHeat));
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.PathOfCinders
                        : IgnatiaCardContent.UnstablePortal));
            }
            else
            {
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.LooselyAimedFirebolts
                        : IgnatiaCardContent.ImminentEruption));
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.ViolentConflagration
                        : IgnatiaCardContent.ScorchingBlaze));
                Assert.That(inHand, Has.Member(
                    branch == IgnatiaUpgradeBranch.FirstSibling
                        ? IgnatiaCardContent.PathOfFlames
                        : IgnatiaCardContent.ChaosGate));
            }

            var ultimate = hers.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(IgnatiaCardContent.ChaosIncarnate));
            Assert.That(
                ultimate.State.Location == CardLocation.UltimateArea, Is.EqualTo(tier == 3),
                "p.16 unlocks the Ultimate from the deck at the last Level Up and not before, so " +
                "Chaos Incarnate is faceup in her Ultimate area on the Tier III arms alone.");
        }

        /// <summary>
        /// THE COIN FACE IS PART OF THE STAGING, and for her it is the card text. Every other hero's
        /// staged configuration passes a <see cref="TeamSide"/> to settle p.7's Initiative ties;
        /// hers settles WHICH OF EACH CARD'S TWO PRINTED LINES RUNS (ID-Q5, user-corrected: sun is the
        /// Blue face, moon the Red one), so a dealing that could stage only one of them would leave
        /// half her printed set unreachable at every tier.
        /// </summary>
        [Test]
        public void BothCoinFacesAreStageable_WhichIsHalfHerPrintedSetEachWay()
        {
            var sun = MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Blue);
            var moon = MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Red);

            Assert.That(sun.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(moon.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
            Assert.That(
                IgnatiaComponentContent.RevealedFaceIsSun(sun.TieBreakerState.RevealedSide),
                Is.True);
            Assert.That(
                IgnatiaComponentContent.RevealedFaceIsSun(moon.TieBreakerState.RevealedSide),
                Is.False);

            Assert.That(
                sun.CardDefinitions.Count, Is.EqualTo(moon.CardDefinitions.Count),
                "The two stagings deal the same cards - what differs is which half of each card's " +
                "printed text the coin selects, which is a fact about the match rather than the " +
                "hand.");
        }

        /// <summary>
        /// THE ENGAGED STAGED BOARD, MEASURED AND RECORDED, so milestone 8.75c's journeys start from
        /// coordinates rather than from a guess - and so a staging that quietly stopped satisfying one
        /// of her printed subjects fails here rather than in a journey.
        ///
        /// <para>Her seven Attacks between them need a MINION adjacent (Chaos Bolt's sun face), a
        /// HERO in range (its moon face), units both ON and OFF one of p.19's six files, and a unit at
        /// EXACTLY the printed Range. Her Blue discard line needs an enemy hero beside a token or a
        /// minion and an enemy minion beside an enemy hero. Her Greens need free files out of her own
        /// space for the wakes and free spaces in radius for the scatter.</para>
        /// </summary>
        [Test]
        public void TheEngagedStagedBoard_CarriesEverythingHerFacesAskFor()
        {
            var configuration = MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusIgnatiaOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var ignatia = setup.Heroes
                .Single(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(ignatia.HeroId));

            var separation = HexDistanceQuery.Calculate(ignatia.Coordinate, enemy.Coordinate);
            var adjacentUnits = setup.Minions
                .Where(minion =>
                    HexDistanceQuery.Calculate(ignatia.Coordinate, minion.Coordinate) == 1)
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
            foreach (var radius in new[] { 1, 2, 3, 4 })
            {
                freeWithin[radius] = map.Spaces
                    .Select(space => space.Coordinate)
                    .Where(coordinate =>
                        HexDistanceQuery.Calculate(ignatia.Coordinate, coordinate) <= radius &&
                        !coordinate.Equals(ignatia.Coordinate) &&
                        IsFree(coordinate))
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            // THE THREE PATHS lay their wake along a straight move of up to 2, 3 or 4 spaces, which
            // p.19 makes a FILE rather than any N steps - so what they need is a direction out of her
            // own space with four free playable spaces along it, and a board offering only bent
            // routes would leave every sun face of her Green line unreachable for a reason belonging
            // to the staging.
            HexCoordinate[] FreeFiles(int length) => ignatia.Coordinate.GetNeighbors()
                .Select(neighbour => new
                {
                    Step = new HexCoordinate(
                        neighbour.Q - ignatia.Coordinate.Q, neighbour.R - ignatia.Coordinate.R),
                    Neighbour = neighbour
                })
                .Where(direction => Enumerable.Range(1, length).All(distance => IsFree(
                    new HexCoordinate(
                        ignatia.Coordinate.Q + (direction.Step.Q * distance),
                        ignatia.Coordinate.R + (direction.Step.R * distance)))))
                .Select(direction => direction.Neighbour)
                .ToArray();

            var longestWake = FreeFiles(4);
            var shortestWake = FreeFiles(2);

            var onAFile = setup.Minions
                .Where(minion => StraightLineQuery.AreInStraightLine(
                    ignatia.Coordinate, minion.Coordinate))
                .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            var offAFile = setup.Minions
                .Where(minion => !StraightLineQuery.AreInStraightLine(
                    ignatia.Coordinate, minion.Coordinate))
                .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            var minionsBesideTheEnemyHero = setup.Minions
                .Where(minion =>
                    minion.TeamSide != ignatia.TeamSide &&
                    HexDistanceQuery.Calculate(minion.Coordinate, enemy.Coordinate) == 1)
                .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            TestContext.WriteLine(
                "IGNATIA STAGED BOARD (engaged, one seat per team): Ignatia " +
                Describe(ignatia.Coordinate) + ", enemy " + Describe(enemy.Coordinate) +
                ", separation " + separation + ". Units adjacent to her: " +
                (adjacentUnits.Length == 0 ? "(none)" : string.Join(", ", adjacentUnits)) +
                ". Minions ON one of her six files: " +
                (onAFile.Length == 0 ? "(none)" : string.Join(", ", onAFile)) +
                ". Minions OFF every file: " +
                (offAFile.Length == 0 ? "(none)" : string.Join(", ", offAFile)) +
                ". Enemy minions adjacent to the enemy hero: " +
                (minionsBesideTheEnemyHero.Length == 0
                    ? "(none)"
                    : string.Join(", ", minionsBesideTheEnemyHero)) +
                ". Free spaces in radius 1/2/3/4: " + freeWithin[1].Length + "/" +
                freeWithin[2].Length + "/" + freeWithin[3].Length + "/" + freeWithin[4].Length +
                ". Free files out of her own space, by first step - length 2: " +
                (shortestWake.Length == 0
                    ? "(none)"
                    : string.Join(", ", shortestWake.Select(Describe))) +
                "; length 4: " +
                (longestWake.Length == 0
                    ? "(none)"
                    : string.Join(", ", longestWake.Select(Describe))) + ".");

            Assert.That(ignatia.Coordinate, Is.EqualTo(new HexCoordinate(-3, 1)),
                "The staged hero's space is a fact about EngagedMirrorStaging rather than about her, " +
                "and StagedHeroBoard.HeroAt says the same thing.");
            Assert.That(separation, Is.EqualTo(2),
                "Two spaces apart, which is the engagement every staged dealing since Bain's has " +
                "been measured on: inside every printed Radius her Blues and Greens carry, inside " +
                "every printed Range her Reds carry, and outside p.19's ring - so Chaos Bolt's MOON " +
                "face, both pushes and the discard line reach him while its SUN face needs a minion.");

            // (1) Chaos Bolt's sun face is "a minion adjacent to you", and Crack of Doom's is "a unit
            //     adjacent to you". Without a minion in her ring the whole sun half of her Gold basic
            //     is unplayable for a reason belonging to the staging.
            Assert.That(
                adjacentUnits.Select(value => value.Split('@')[0]),
                Is.EquivalentTo(StagedHeroBoard.StagedBesideTheHero),
                "The two pieces the engaged staging leaves beside the staged hero are the same two " +
                "every other hero's fixtures measure.");
            Assert.That(
                setup.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(ignatia.Coordinate, minion.Coordinate) == 1 &&
                    minion.TeamSide != ignatia.TeamSide),
                Is.True,
                "and at least one is an ENEMY, or Chaos Bolt's sun face and Crack of Doom's would " +
                "reach only her own side.");

            // (2) The straight-line pair needs BOTH readings to have a candidate, which is the one
            //     thing about this board no earlier dealing had to measure: "in a straight line" and
            //     "not in a straight line" are complements, so a board on which every unit is on a
            //     file would make one arm of three of her Reds unreachable and the other trivial.
            Assert.That(onAFile, Is.Not.Empty,
                "Playing with Fire's MOON face takes a unit in range IN a straight line.");
            Assert.That(offAFile, Is.Not.Empty,
                "and its SUN face takes one NOT in a straight line - the complement, which needs a " +
                "unit off every file or the arm is proven only by its refusal.");

            // (3) THE SECOND NEGATIVE THIS BOARD RECORDS, measured rather than asserted away. The
            //     moon discard line is "an enemy minion in radius adjacent to an ENEMY HERO", and
            //     at SETUP no enemy minion stands in the enemy hero's ring: the waves begin in
            //     their lanes and the hero is engaged with her, not with them. Both halves of the
            //     pairing exist on this board - an enemy minion inside her printed Radius and an
            //     enemy hero - so what the arm waits for is one step of an advancing wave rather
            //     than a piece the staging forgot, which is why 8.75c reaches it by PLAYING and
            //     `IgnatiaDiscardTests` proves the selection itself on a board built by hand.
            Assert.That(minionsBesideTheEnemyHero, Is.Empty,
                "Recorded, not required: at setup the minion waves stand in their lanes, so " +
                "Abrupt Combustion's moon face names nobody on turn one. The day a staging change " +
                "puts a minion beside the enemy hero this case says so rather than passing " +
                "silently, which is what a measured board is for.");
            Assert.That(
                setup.Minions.Any(minion =>
                    minion.TeamSide != ignatia.TeamSide &&
                    HexDistanceQuery.Calculate(ignatia.Coordinate, minion.Coordinate) <= 4),
                Is.True,
                "and the half of the pairing the STAGING owes is here: an enemy minion inside the " +
                "largest Radius her Blue line prints, waiting only on the hero it has to stand " +
                "beside.");

            // (4) The three Paths' wakes and the scatter.
            Assert.That(longestWake, Is.Not.Empty,
                "Path of Flames prints \"Move up to 4 spaces in a straight line\", so a board with " +
                "no free file of four out of her space would prove the decline and never the wake.");
            Assert.That(shortestWake, Is.Not.Empty,
                "and Path of Ashes' two-space version has one too.");
            Assert.That(freeWithin[4].Length, Is.GreaterThanOrEqualTo(3),
                "and the moon faces scatter up to N magma tokens in her printed Radius, which needs " +
                "at least her supply of three empty spaces inside the largest disk she prints.");

            // (5) THE NEGATIVE THIS BOARD IS FOR, recorded rather than asserted away: no FRIENDLY
            //     HERO stands anywhere, so Searing Heat's and Scorching Blaze's sun faces find nobody
            //     here. That is what `CreateStagedIgnatiaWithAllyForInternalTest` exists for.
            Assert.That(
                setup.Heroes.Count(hero => hero.TeamSide == ignatia.TeamSide), Is.EqualTo(1),
                "One seat per team, so p.17's \"a friendly hero\" has no candidate on this board at " +
                "all - the sun pushes' board is the ally one, and this case says so rather than " +
                "leaving the absence to be found by a red.");
        }

        /// <summary>
        /// THE ALLY BOARD, MEASURED AND RECORDED: the one staging on which "Move a friendly hero in
        /// radius N spaces in a straight line" has a candidate, because p.17's Friendly never includes
        /// you.
        /// </summary>
        [Test]
        public void TheAllyStagedBoard_PutsAFriendlyHeroInsideBothPushRadii()
        {
            var configuration = MatchConfiguration.CreateStagedIgnatiaWithAllyForInternalTest(
                TeamSide.Red, IgnatiaAllyStage.TierTwo);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateStagedWaspVersusIgnatiaWithAllyOneVsTwo(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var ignatia = setup.Heroes
                .Single(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId);
            var ally = setup.Heroes.Single(hero =>
                hero.TeamSide == ignatia.TeamSide && !hero.HeroId.Equals(ignatia.HeroId));
            var enemy = setup.Heroes.Single(hero => hero.TeamSide != ignatia.TeamSide);

            var toAlly = HexDistanceQuery.Calculate(ignatia.Coordinate, ally.Coordinate);
            TestContext.WriteLine(
                "IGNATIA ALLY BOARD (not restaged, three seats): Ignatia " +
                Describe(ignatia.Coordinate) + ", ally " + ally.ContentId.Value + " " +
                Describe(ally.Coordinate) + " at distance " + toAlly + ", enemy " +
                Describe(enemy.Coordinate) + " at distance " +
                HexDistanceQuery.Calculate(ignatia.Coordinate, enemy.Coordinate) + ".");

            Assert.That(setup.Heroes.Count, Is.EqualTo(3),
                "Three seats: Wasp on Red, Ignatia and her ally on Blue.");
            Assert.That(ally.ContentId.Value, Is.EqualTo("arien"),
                "Arien holds the ally seat, for the reason Brogan's, Tali's, Rowenna's and Hanu's " +
                "ally boards give it to him: he is a hero this build already deals, so the " +
                "selection is proven against real content rather than an invented printed set.");
            Assert.That(toAlly, Is.InRange(1, 3),
                "Searing Heat prints Radius 3 and Scorching Blaze prints 3. A friendly hero outside " +
                "the smaller of those disks would make both sun pushes unreachable for a reason " +
                "that belongs to the staging.");
            Assert.That(
                configuration.CardInstances
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId)
                    .ToArray(),
                Has.Member(IgnatiaCardContent.SearingHeat),
                "and the Tier II ally stage deals the SecondSibling branch, which is the only one " +
                "either push is on.");
        }

        /// <summary>
        /// Her four decision labels reach the one catalog presentation asks, which is where a dealing
        /// puts a hero's table - and hers arrives in TWO declarations, because Chaos Incarnate's pair
        /// lives on the triggered body a p.16 permanent ability names by reference rather than on any
        /// dealt face.
        /// </summary>
        [Test]
        public void HerDecisionLabels_AreInTheCatalogPresentationAsks()
        {
            var declared = IgnatiaEffectContent.DecisionLabels()
                .Concat(IgnatiaEffectContent.UltimateDecisionLabels())
                .ToArray();
            Assert.That(declared, Has.Length.EqualTo(4),
                "Two printed \"may repeat once\" clauses and the Ultimate's flip and repeat. Her " +
                "eighteen dealt faces raise no modal arm at all, because the COIN picks which of a " +
                "card's two texts runs and the player is never asked which.");

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
                        IgnatiaCardContent.ContentSetId + "-", System.StringComparison.Ordinal)),
                Is.EqualTo(declared.Select(declaration => declaration.CardId).Distinct().Count()));
        }

        /// <summary>
        /// THE REGISTRATION FLIP, asserted as one claim: her eleven own contracts are in the build's
        /// catalog, her magma token is in the component catalog, and Chaos Incarnate's body is in the
        /// triggered-effect catalog.
        /// </summary>
        [Test]
        public void TheBuildRegistersEveryContractHerDealtFacesNeed()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            var registered = build.Capabilities.Registered
                .Select(capability => capability.ToString())
                .Where(value => value.StartsWith("ignatia-", System.StringComparison.Ordinal))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(registered, Is.EqualTo(new[]
            {
                "ignatia-coin-sun@1",
                "ignatia-enemy-hero-adjacent-to-magma-or-minion@1",
                "ignatia-enemy-minion-adjacent-to-enemy-hero@1",
                "ignatia-equilibrium@1",
                "ignatia-flip-tie-breaker@1",
                "ignatia-place-magma-in-radius@1",
                "ignatia-place-magma-wake@1",
                "ignatia-range-hero-not-adjacent@1",
                "ignatia-range-unit-at-maximum-range@1",
                "ignatia-range-unit-in-straight-line@1",
                "ignatia-range-unit-not-in-straight-line@1",
                // Milestone 8.113: p.18's short-supply step before each moon placement.
                "ignatia-reclaim-magma@1"
            }),
                "EXACTLY the eleven 8.75 and 8.75a named as the deferral list plus 8.113's magma " +
                "reclamation, and no more: another arriving is a registration somebody has to decide " +
                "on rather than inherit.");

            Assert.That(
                build.Components.RegisteredTokens.Select(token => token.DefinitionId.Value)
                    .Where(value => value.StartsWith("ignatia-", System.StringComparison.Ordinal)),
                Is.EqualTo(new[] { "ignatia-magma" }),
                "ONE token definition, which is what makes her the first dealing since Min's to " +
                "move the component list at all.");
            Assert.That(
                build.Components.LoadToken(
                    IgnatiaMagmaContent.MagmaTokenDefinition.Reference).SupplyCount,
                Is.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount),
                "and the supply of three the user settled reaches the catalog rather than staying " +
                "a constant in her own file.");

            Assert.That(
                build.TriggeredEffects.Definitions
                    .Select(definition => definition.Id.Value)
                    .Where(value => value.StartsWith("ignatia-", System.StringComparison.Ordinal))
                    .ToArray(),
                Has.Length.EqualTo(1),
                "Chaos Incarnate's own body, which p.16's permanent ability names BY REFERENCE and " +
                "the owner walk resolves against this catalog. Equilibrium's \"This round:\" is an " +
                "ActiveEffectCreationHandler rather than a triggered body, so it adds none.");

            var ultimate = IgnatiaCardContent
                .Definition(IgnatiaCardContent.ChaosIncarnate).UltimateRule;
            Assert.That(ultimate, Is.Not.Null);
            Assert.That(ultimate.TriggeredEffect.HasValue, Is.True);
            Assert.That(
                ultimate.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsPrimaryAction),
                "Cutter's once-per-turn after-primary-action trigger, reused unchanged.");
        }

        /// <summary>
        /// The offerable roster and the hand switch are two halves of one claim, read from both
        /// sides: every hero a selector offers actually deals, and her arm is what keeps that true
        /// for the twenty-second. Milestone 8.35z41's own rule, asserted for her by name because
        /// 8.35z46 broke it for a hero who was otherwise completely dealt.
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

            Assert.That(dealt, Has.Member(IgnatiaCardContent.ContentSetId));
            Assert.That(dealt, Has.Count.EqualTo(24));
            Assert.That(dealt.Distinct().Count(), Is.EqualTo(24),
                "and no two rows name one printed set.");
        }

        // Her magma token's prepared face and its absent caption are `PlacedTokenViewTests`' - that
        // lookup lives in Presentation and loads a Unity texture, so it cannot be asked from the
        // cross-platform harness this fixture deliberately runs in.

        private static string Describe(HexCoordinate coordinate) =>
            "(" + coordinate.Q + "," + coordinate.R + ")";
    }
}
