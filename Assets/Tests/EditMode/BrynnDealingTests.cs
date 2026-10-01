using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.69g, the milestone that DEALS Brynn the Seeker as the twentieth hero: the half of a
    /// dealing that is about the CONFIGURATION rather than about the content - the selector row with
    /// the profile facts the user asked for by name, the hand dispatch, the three staged stagings, the
    /// registration flip, and the engaged board 8.69h's journeys will be written on.
    ///
    /// <para>Milestone 8.35z41's rule is what the first case asserts from the roster's own side: the
    /// offerable roster and <c>MatchConfiguration</c>'s hand switch are two halves of one claim, and a
    /// hero present in the first but absent from the second is a pick that throws at the moment the
    /// match composes.</para>
    ///
    /// <para><b>The last three cases are the debt his five composition blocks recorded.</b> Every one
    /// of them proved its faces against a hand-built board and a hand-built capability catalog,
    /// because a configured match cannot deal a hero the build does not register - so Over the Top
    /// through a real snapshot, and Familiar Ground's second bullet OFFERED or GREYED by the board a
    /// player would be looking at, could not be asked until this milestone. They are asked here.</para>
    /// </summary>
    public sealed class BrynnDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedBrynnCarriesHisEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var brynn = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == BrynnCardContent.ContentSetId);
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(29),
                "He was the twentieth hero this build could put on a board; milestone 8.71h deals " +
                "Hanu as the twenty-first and 8.75b Ignatia as the twenty-second.");
            Assert.That(brynn.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.BrynnHeroIdValue));
            Assert.That(brynn.DisplayName, Is.EqualTo("Brynn the Seeker"),
                "Setup step 7's own name, which is a label on the seat rather than an identity - and " +
                "the DASHBOARD's epithet, not the Tabletopia card group's 'the Tracker'.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { brynn }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick
                : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);

            var cards = configuration.CardInstances
                .Where(card => card.OwnerId.Value == brynn.HeroIdValue)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(18),
                "Eighteen dealt cards; Home Turf is p.20's boxed Handicap and is not one.");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(BrynnCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(BrynnCardContent.Deck));

            Assert.That(
                HeroLineupComposition.Compose(format, lineup).Heroes
                    .Single(hero => hero.ContentId.Value == BrynnCardContent.ContentSetId)
                    .SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"),
                "A selected hero takes the seat and the marked point of the position it was chosen " +
                "into; the hero column moves and no seat does.");
        }

        /// <summary>
        /// THE USER'S ONE NAMED REQUIREMENT FOR THIS HERO, asserted at the tier every offline check
        /// can see: the roster row carries the verified profile facts the 8.35z86 picker filters by.
        /// The RENDERED half - the star and role buttons actually keeping his button visible - is
        /// <c>PrivateLobbyPlayModeTests</c>'s, with an inspected capture.
        /// </summary>
        [Test]
        public void HisRosterRow_CarriesTheThreeStarsAndTheThreePrintedRoles()
        {
            var brynn = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == BrynnCardContent.ContentSetId);

            Assert.That(brynn.Complexity, Is.EqualTo(HeroComplexity.VeryComplex),
                "Three filled stars on `_selection-cards/Brynn__front.png`, which p.21 calls Very " +
                "Complex - read off the extracted front at the dealing rather than inferred.");
            Assert.That(
                brynn.Roles,
                Is.EqualTo(HeroRole.Tactician | HeroRole.Damager | HeroRole.Durable),
                "and the three role icons the same front prints, in its own printed order.");
            Assert.That(brynn.Roles & HeroRole.Healer, Is.EqualTo(HeroRole.None),
                "He carries NO Healer role, which is the role the rendered filter case uses to prove " +
                "the filter subtracts as well as adds: selecting it must hide his button.");
        }

        [TestCase(1, BrynnUpgradeBranch.FirstSibling)]
        [TestCase(2, BrynnUpgradeBranch.FirstSibling)]
        [TestCase(2, BrynnUpgradeBranch.SecondSibling)]
        [TestCase(3, BrynnUpgradeBranch.FirstSibling)]
        [TestCase(3, BrynnUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksTheUltimate(
            int tier, BrynnUpgradeBranch branch)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedBrynnForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedBrynnTierTwoForInternalTest(
                    TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedBrynnTierThreeForInternalTest(TeamSide.Blue, branch);

            var ids = BrynnCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
            var cards = configuration.CardInstances
                .Where(card => ids.Contains(card.DefinitionReference.DefinitionId))
                .ToArray();

            Assert.That(
                cards.Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(ids),
                "Every dealt face of his is in the staged match, whatever the stage.");
            Assert.That(cards.Select(card => card.OwnerId).Distinct().Count(), Is.EqualTo(1),
                "and they all belong to one seat.");
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5),
                "p.15 closes a level-up with exactly five cards, one of each colour, in hand.");

            var upgraded =
                tier == 1
                    ? new[]
                    {
                        BrynnCardContent.HighGround,
                        BrynnCardContent.TreadLightly,
                        BrynnCardContent.BearTrap
                    }
                    : tier == 2 && branch == BrynnUpgradeBranch.FirstSibling
                        ? new[]
                        {
                            BrynnCardContent.ElevatedAmbush,
                            BrynnCardContent.CoverTracks,
                            BrynnCardContent.LogTrap
                        }
                        : tier == 2
                            ? new[]
                            {
                                BrynnCardContent.SplitAttack,
                                BrynnCardContent.MountainGuide,
                                BrynnCardContent.TrueGrit
                            }
                            : branch == BrynnUpgradeBranch.FirstSibling
                                ? new[]
                                {
                                    BrynnCardContent.PeakPrecision,
                                    BrynnCardContent.HideTraces,
                                    BrynnCardContent.DeadfallTrap
                                }
                                : new[]
                                {
                                    BrynnCardContent.SplitThrow,
                                    BrynnCardContent.ExpeditionLeader,
                                    BrynnCardContent.DieHard
                                };

            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[]
                {
                    BrynnCardContent.FamiliarGround,
                    BrynnCardContent.Decoy
                })),
                "Both basics stay in hand at every stage, exactly as p.6 leaves them - and for him " +
                "that is load-bearing twice over: Familiar Ground is the only face of his whose " +
                "modal narrows an ATTACK TARGET by the printed predicate, and Decoy is the only one " +
                "that moves an enemy unit at all.");

            var ultimate = cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(BrynnCardContent.OverTheTop));
            Assert.That(
                ultimate.State.Location,
                Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck),
                "p.16 puts the Ultimate beside the dashboard only once it is unlocked, and his " +
                "permanent counts-as overlay is derived from exactly that card being there.");
        }

        /// <summary>
        /// THE STAGED BOARD, MEASURED AND RECORDED, and for him ONE NUMBER matters more than any
        /// coordinate: HOW MANY OBSTACLES STAND BESIDE EACH HERO. Fifteen of his nineteen faces print
        /// "adjacent to 3 or more obstacles", so a board where the enemy hero happens to satisfy it
        /// would answer every one of those clauses by the staging rather than by the card - and a
        /// board where nobody can ever satisfy it would leave half his set unreachable.
        ///
        /// <para>Milestone 8.40j's own dealing is why this case exists at all: it found every Attack
        /// face of a hero offering minions only, because the two heroes stood two spaces apart around
        /// a minion and the gap was bent rather than straight.</para>
        ///
        /// <para>What 8.69h needs from this board is four things his set asks for, and this case
        /// measures all four rather than restating a coordinate: an ADJACENT UNIT for the "adjacent
        /// to you" arm of every swap and both guides' first sentence, a FRIENDLY unit among them and a
        /// SECOND friendly unit in radius for the guides' "a different friendly unit", the ENEMY HERO
        /// inside the printed Radius his Silver, Blue and Green faces carry, and the OBSTACLE COUNT
        /// beside both heroes. The coordinates are written to the test output so the next milestone
        /// starts from a known board rather than from a search.</para>
        /// </summary>
        [Test]
        public void TheEngagedStagedBoard_CarriesEverythingHisFacesAskFor()
        {
            var configuration = MatchConfiguration.CreateStagedBrynnForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusBrynnOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var brynn = setup.Heroes
                .Single(hero => hero.ContentId.Value == BrynnCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(brynn.HeroId));

            // The real placement authority, so the obstacle count below is p.18's own answer through
            // `ObstacleQuery` rather than a second implementation of it written for this case.
            var board = new AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                System.Array.Empty<ObstacleOccupancy>(),
                setup.Minions);
            var noOverlay = RuleModificationSet.Empty;

            int ObstaclesBeside(HexCoordinate coordinate) => coordinate.GetNeighbors()
                .Count(neighbour => ObstacleQuery.IsObstacle(board, neighbour, noOverlay));

            var separation = HexDistanceQuery.Calculate(brynn.Coordinate, enemy.Coordinate);
            var adjacentUnits = setup.Minions
                .Where(minion => HexDistanceQuery.Calculate(brynn.Coordinate, minion.Coordinate) == 1)
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
            foreach (var radius in new[] { 2, 3, 4 })
            {
                freeWithin[radius] = map.Spaces
                    .Select(space => space.Coordinate)
                    .Where(coordinate =>
                        HexDistanceQuery.Calculate(brynn.Coordinate, coordinate) <= radius &&
                        !coordinate.Equals(brynn.Coordinate) &&
                        IsFree(coordinate))
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            var friendlyWithin = new Dictionary<int, string[]>();
            foreach (var radius in new[] { 1, 3, 4 })
            {
                friendlyWithin[radius] = setup.Minions
                    .Where(minion =>
                        minion.TeamSide == brynn.TeamSide &&
                        HexDistanceQuery.Calculate(brynn.Coordinate, minion.Coordinate) <= radius)
                    .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate))
                    .OrderBy(value => value, System.StringComparer.Ordinal)
                    .ToArray();
            }

            var brynnObstacles = ObstaclesBeside(brynn.Coordinate);
            var enemyObstacles = ObstaclesBeside(enemy.Coordinate);

            TestContext.WriteLine(
                "BRYNN STAGED BOARD (engaged, one seat per team): Brynn " + Describe(brynn.Coordinate) +
                " with " + brynnObstacles + " of 6 neighbours an obstacle, enemy " +
                Describe(enemy.Coordinate) + " with " + enemyObstacles +
                " of 6, separation " + separation +
                ". Units adjacent to him: " +
                (adjacentUnits.Length == 0 ? "(none)" : string.Join(", ", adjacentUnits)) +
                ". Friendly units in radius 1/3/4: " +
                string.Join(" | ", new[] { 1, 3, 4 }.Select(radius =>
                    radius + ": " + (friendlyWithin[radius].Length == 0
                        ? "(none)"
                        : string.Join(", ", friendlyWithin[radius])))) +
                ". Free spaces in radius 2/3/4: " + freeWithin[2].Length + "/" +
                freeWithin[3].Length + "/" + freeWithin[4].Length + ".");

            Assert.That(brynn.Coordinate, Is.EqualTo(new HexCoordinate(-3, 1)),
                "The staged hero's space is a fact about EngagedMirrorStaging rather than about him, " +
                "and StagedHeroBoard.HeroAt says the same thing.");
            Assert.That(separation, Is.EqualTo(2),
                "Two spaces apart, which is the engagement every staged dealing since Bain's has " +
                "been measured on: inside every Radius his set prints and outside adjacency, so his " +
                "'adjacent to you' arms need a step or a minion first and his radius arms do not.");

            // (1) THE OBSTACLE COUNT, which is the measurement his whole set turns on. Recorded from
            //     both sides rather than asserted at a number, because the count is a fact about the
            //     map and the staging and belongs to neither hero: what a journey needs to know is
            //     which arms it can reach without moving a piece, and whether Over the Top is doing
            //     any work when it is unlocked.
            Assert.That(brynnObstacles, Is.InRange(0, 6));
            Assert.That(enemyObstacles, Is.InRange(0, 6));
            TestContext.WriteLine(
                "OBSTACLE READING FOR 8.69h: the enemy hero " +
                (enemyObstacles >= ObstacleAdjacencyQuery.PrintedObstacleThreshold
                    ? "ALREADY satisfies"
                    : "does NOT satisfy") +
                " 'adjacent to 3 or more obstacles' on the untouched staged board, so a journey " +
                "about the narrowed arms " +
                (enemyObstacles >= ObstacleAdjacencyQuery.PrintedObstacleThreshold
                    ? "can take them straight away"
                    : "must first move a piece beside him, or unlock Over the Top") + ".");

            // (2) Every swap's "a unit adjacent to you" arm and both guides' first sentence need a
            //     unit in p.19's innermost ring, and the guides need a FRIENDLY one.
            Assert.That(
                adjacentUnits.Select(value => value.Split('@')[0]),
                Is.EquivalentTo(StagedHeroBoard.StagedBesideTheHero),
                "The two pieces the engaged staging leaves beside the staged hero are the same two " +
                "every other hero's fixtures measure.");
            Assert.That(friendlyWithin[1], Is.Not.Empty,
                "and at least one of them is FRIENDLY, or Mountain Guide's and Expedition Leader's " +
                "first sentence - 'a friendly unit adjacent to you' - would have no subject at all. " +
                "p.17's Friendly covers a minion, which is why he needs no ally seat.");
            Assert.That(
                setup.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(brynn.Coordinate, minion.Coordinate) == 1 &&
                    minion.TeamSide != brynn.TeamSide),
                Is.True,
                "and at least one is an ENEMY, or every 'target a unit adjacent to you' arm of his " +
                "three Attack Reds and Familiar Ground's first bullet would reach only his own side.");

            // (3) The guides' second sentence moves "a different friendly unit in radius", so the
            //     Radius 4 both of them print has to hold a SECOND friendly unit.
            Assert.That(friendlyWithin[4], Has.Length.GreaterThanOrEqualTo(2),
                "Mountain Guide and Expedition Leader both print Radius 4 and both move a DIFFERENT " +
                "friendly unit in it, so a board with one friendly unit would make the second " +
                "sentence unreachable for a reason that belongs to the staging.");

            // (4) Decoy, the three traps and the four presence faces all reach the enemy hero through
            //     a printed Radius rather than through adjacency.
            Assert.That(
                HexDistanceQuery.Calculate(brynn.Coordinate, enemy.Coordinate),
                Is.LessThanOrEqualTo(3),
                "Bear Trap prints the smallest Radius his radius arms use - 3 - so the enemy hero " +
                "has to be inside it or his whole Green line reaches nobody.");
            Assert.That(freeWithin[2], Is.Not.Empty,
                "and Decoy moves a unit one space, so there is somewhere for one to go.");
        }

        private static string Describe(HexCoordinate coordinate) =>
            "(" + coordinate.Q + ", " + coordinate.R + ")";

        // His ARTWORK half is asserted in `ConfiguredCardArtworkCatalogTests` rather than here, and
        // the reason is structural: `ConfiguredCardArtworkCatalog` lives in the Presentation slice the
        // offline harness compiles only one file of, so a case naming it would make this whole fixture
        // Unity-only. See that file's `EveryDealtFaceOfBrynns_HasAnArtworkRowAtHisOwnRevision`.

        /// <summary>
        /// His nineteen decision labels reach the one catalog presentation asks, which is where a
        /// dealing puts a hero's table - milestone 8.46a aggregates per hero exactly as
        /// <c>MatchContentCatalogs</c> aggregates capability handlers.
        /// </summary>
        [Test]
        public void HisDecisionLabels_AreInTheCatalogPresentationAsks()
        {
            var declared = BrynnEffectContent.DecisionLabels().ToArray();
            Assert.That(declared, Has.Length.EqualTo(19));

            var registered = DecisionLabelCatalog.Registered;
            foreach (var declaration in declared)
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out var label),
                    Is.True,
                    declaration.CardId.Value + " / " + declaration.NodeId.Value +
                    " is declared beside his content and missing from the catalog.");
                Assert.That(label, Is.EqualTo(declaration.Label));
            }

            Assert.That(
                registered.Keys.Count(cardId =>
                    cardId.Value.StartsWith(
                        BrynnCardContent.ContentSetId + "-", System.StringComparison.Ordinal)),
                Is.EqualTo(declared.Select(declaration => declaration.CardId).Distinct().Count()));
        }

        /// <summary>
        /// THE REGISTRATION FLIP, asserted as one claim: his three own contracts are in the build's
        /// catalog, he contributes no token and no triggered effect at all, and every contract his
        /// eighteen dealt faces name is one a configured match can start.
        /// </summary>
        [Test]
        public void TheBuildRegistersEveryContractHisDealtFacesNeed()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            foreach (var id in new[]
            {
                "brynn-enemy-hero-in-radius-adjacent-to-obstacles@1",
                "brynn-attack-target-was-adjacent-to-obstacles@1",
                "brynn-tracking-swap-subject@1"
            })
            {
                Assert.That(
                    build.Capabilities.Registered.Select(capability => capability.ToString()),
                    Has.Member(id),
                    id + " is named by a face a configured match now deals.");
            }

            Assert.That(
                build.Capabilities.Registered
                    .Select(capability => capability.ToString())
                    .Where(value => value.StartsWith("brynn-", System.StringComparison.Ordinal))
                    .OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "brynn-attack-target-was-adjacent-to-obstacles@1",
                    "brynn-enemy-hero-in-radius-adjacent-to-obstacles@1",
                    "brynn-tracking-swap-subject@1"
                }),
                "and EXACTLY three: 8.69a's dropped fourth declaration must not come back in, and a " +
                "fourth arriving is a registration somebody has to decide on rather than inherit.");

            Assert.That(
                build.TriggeredEffects.Definitions
                    .Select(definition => definition.Id.Value)
                    .Where(value => value.StartsWith("brynn-", System.StringComparison.Ordinal)),
                Is.Empty,
                "He holds NO triggered effect: his Ultimate is a permanent ability whose whole " +
                "content is a standing rule declaration `UltimateUnlockQuery` derives, which is " +
                "Rowenna's and Whisper's shape rather than Min's or Tali's.");

            var overTheTop = BrynnCardContent.Definition(BrynnCardContent.OverTheTop).UltimateRule;
            Assert.That(overTheTop, Is.Not.Null);
            Assert.That(overTheTop.TriggeredEffect, Is.Null);
            Assert.That(overTheTop.Modifications, Has.Count.EqualTo(1));
            Assert.That(
                overTheTop.Modifications.Single().Kind,
                Is.EqualTo(RuleModificationKind.CountsAsAdjacentToThreeOrMoreObstacles));
            Assert.That(
                overTheTop.Modifications.Single().ValueApplicability,
                Is.EqualTo(RuleValueApplicability.ControllerAction),
                "and the applicability member 8.69a minted is the one a DEALT card now carries, " +
                "which is what makes it reachable from a configured match at all.");
        }

        /// <summary>
        /// The offerable roster and the hand switch are two halves of one claim, read from both sides:
        /// every hero a selector offers actually deals, and his arm is what keeps that true for the
        /// twentieth. Milestone 8.35z41's own rule, asserted for him by name because 8.35z46 broke it
        /// for a hero who was otherwise completely dealt.
        /// </summary>
        [Test]
        public void EveryOfferableHeroDeals_AndHeIsOneOfThem()
        {
            var dealt = new List<string>();
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                var hand = MatchConfiguration.CreateVerifiedHandForInternalTest(
                    hero.HeroIdValue, "probe");
                Assert.That(hand, Is.Not.Null, hero.HeroIdValue);
                Assert.That(hand.Definitions.Count(definition => !definition.Progression.IsSpell), Is.EqualTo(18),
                    hero.HeroIdValue + ": eighteen printed fronts; a spell (9.30i, Gydion) lies in the book, not the deck");
                dealt.Add(hero.ContentId.Value);
            }

            Assert.That(dealt, Has.Member(BrynnCardContent.ContentSetId));
            Assert.That(dealt, Has.Count.EqualTo(29));
            Assert.That(dealt.Distinct().Count(), Is.EqualTo(29),
                "and no two rows name one printed set.");
        }

        // -------------------------------------------------------------------------------------------
        // The debts his composition blocks recorded, which only a dealt hero can discharge
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// 8.69a'S FIRST DEBT, first half: Familiar Ground is GREYED OUT by a CONFIGURED MATCH when
        /// neither of its two bullets has a subject - which on the staged board is the ordinary case,
        /// because nothing stands in his ring once the two staged minions are parked and the enemy
        /// hero is standing in the open.
        ///
        /// <para>Milestone 8.35l's rule is what this measures: a step with no legal subject is greyed
        /// rather than offered, drawn and spent. It could not be asked of his Gold face until this
        /// milestone, because an offer query reads a CONFIGURED match's own cards and no configured
        /// match could deal him.</para>
        /// </summary>
        [Test]
        public void FamiliarGround_IsGreyedWhenNeitherBulletHasASubject()
        {
            var board = BrynnBoard.Create(heroPlacements: EnemyInTheOpen);

            Assert.That(
                ObstaclesBesideTheEnemy(board),
                Is.LessThan(ObstacleAdjacencyQuery.PrintedObstacleThreshold),
                "The enemy hero has to be in the open before the second bullet is asked anything.");
            Assert.That(
                board.MinionIds().Select(board.SpaceOf)
                    .Any(space => HexDistanceQuery.Calculate(
                        space, board.HeroSpace(board.Hero)) == 1),
                Is.False,
                "and nothing may stand in p.19's ring around him, or the FIRST bullet has a subject " +
                "and the face is offered for a reason that is not the one under test.");

            Assert.That(
                board.IsPrimaryAvailable(BrynnCardContent.FamiliarGround), Is.False,
                "Both printed bullets name nobody on this board - no unit adjacent to him, and no " +
                "hero in range adjacent to 3 or more obstacles - so the whole printed primary is " +
                "greyed. A modal whose every arm is empty is an action with nothing to act on.");
        }

        /// <summary>
        /// 8.69a'S FIRST DEBT, second half: box the enemy hero in with three obstacles and the same
        /// face on the same board becomes available, offers its obstacle bullet, and reaches p.14's
        /// own target question with him in the legal set.
        ///
        /// <para>The three obstacles are placed through <c>StagedHeroBoard</c>'s setup-obstacle
        /// affordance and computed off the map rather than written down - see <see cref="BoxSpaces"/>
        /// - so the case is boxing him in with exactly the printed threshold and not with whatever
        /// the water ring happened to supply.</para>
        /// </summary>
        [Test]
        public void FamiliarGroundsObstacleBullet_IsOfferedOnceTheEnemyHeroIsBoxedIn()
        {
            var board = BrynnBoard.Create(
                heroPlacements: EnemyInTheOpen,
                obstacles: BoxSpaces(EnemyAt));

            Assert.That(
                ObstaclesBesideTheEnemy(board),
                Is.EqualTo(ObstacleAdjacencyQuery.PrintedObstacleThreshold),
                "Exactly three of his six neighbours, so a reading that passed on four would still " +
                "be measuring the threshold.");

            var offer = board.PrimaryOffer(BrynnCardContent.FamiliarGround);
            Assert.That(offer.IsAvailable, Is.True,
                "The same face that was greyed on the open board is available now, and the only " +
                "thing that moved is how many obstacles stand beside the enemy hero.");
            board.StartOffer(offer);

            var choice = board.Choice;
            Assert.That(choice, Is.Not.Null,
                "Familiar Ground prints p.10's 'Choose one -', so a configured match opens the modal " +
                "before p.14's target question.");
            var bullets = choice.SelectInvocations
                .Select(command => command.Candidate.OptionNodeId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            TestContext.WriteLine(
                "FAMILIAR GROUND bullets offered with the enemy boxed in: " +
                string.Join(", ", bullets));
            Assert.That(bullets, Has.Member("familiar-ground-obstacle-bullet"),
                "and the bullet that names a boxed-in hero in range is one of them.");
            // MILESTONE 8.70. The OTHER bullet - "Target a unit adjacent to you" - names nobody on
            // this board, because nothing at all stands in p.19's ring around him. From this milestone
            // that is greyed at the choice rather than offered, taken and spent on nothing, which is
            // the user's ruling on BR-Q19h - and measuring it here is what makes the positive
            // assertion above about the obstacle bullet rather than about the modal having two
            // controls.
            Assert.That(
                choice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId.Value)
                    .ToArray(),
                Is.EqualTo(new[] { "familiar-ground-adjacent-bullet" }),
                "and the bullet whose own sentence names nobody is greyed beside it.");

            TakeTheObstacleBullet(board);

            var target = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null,
                "Taking the bullet reaches p.14's own target question.");
            Assert.That(target.LegalTargetHeroIds, Is.EqualTo(new[] { board.Enemy }),
                "whose legal set is the ONE hero the narrowing admits - which is the narrowing 8.69a " +
                "minted, read at target selection and now reachable from a dealt card.");
        }

        /// <summary>
        /// 8.69a'S SECOND DEBT: Over the Top through a LIVE configured match - unlocked beside the
        /// dashboard, widening a clause of his own, and surviving a real snapshot capture and restore.
        ///
        /// <para>The board is the one the case above proved offers NO second bullet: the enemy hero
        /// stands in the open at Range 3 with the staged minions parked. Unlocking Over the Top is the
        /// only difference, and it is enough - which is exactly what "while you are performing actions
        /// all enemy heroes in play count as adjacent to 3 or more obstacles" says.</para>
        ///
        /// <para>The snapshot round trip is the half 8.69a could not ask: the declaration is derived
        /// from the unlocked card and the board and is written into no payload, so a capture, a
        /// restore and a second capture have to agree byte for byte while the overlay keeps
        /// working.</para>
        /// </summary>
        [Test]
        public void OverTheTop_WidensHisOwnClauseInAConfiguredMatch_AndSurvivesASnapshot()
        {
            var board = BrynnBoard.Create(
                unlockUltimate: true,
                heroPlacements: EnemyInTheOpen);

            Assert.That(
                ObstaclesBesideTheEnemy(board),
                Is.LessThan(ObstacleAdjacencyQuery.PrintedObstacleThreshold),
                "This is the board the case above proved offers NO second bullet, so the only thing " +
                "that can change the answer below is the Ultimate.");

            Assert.That(
                board.Hand(board.Hero), Has.Member(BrynnCardContent.FamiliarGround),
                "p.6 deals his Gold basic into the opening hand, and p.16's unlock moves the Purple " +
                "out of the deck without touching it.");

            board.RoundTripThroughASnapshot();

            board.StartPrimary(BrynnCardContent.FamiliarGround);

            var bullets = board.Choice.SelectInvocations
                .Select(command => command.Candidate.OptionNodeId.Value)
                .ToArray();
            Assert.That(bullets, Has.Member("familiar-ground-obstacle-bullet"),
                "With Over the Top beside his dashboard the enemy hero counts as adjacent to 3 or " +
                "more obstacles wherever he is standing, so the bullet that named nobody on this " +
                "very board now has a subject - and it is the DEALT card's overlay doing it, derived " +
                "from the unlocked Purple and the participants rather than from anything a fixture " +
                "built.");
            // MILESTONE 8.70, and this is the case that proves the greying reads the overlay rather
            // than the geometry: the enemy hero stands in the open, so WITHOUT Over the Top this
            // bullet would be greyed at the choice - and with it unlocked the bullet is offered. The
            // adjacent bullet is greyed either way, because nothing stands in his ring.
            Assert.That(
                board.Choice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId.Value)
                    .ToArray(),
                Is.EqualTo(new[] { "familiar-ground-adjacent-bullet" }));

            board.RoundTripThroughASnapshot();

            // AND THE GREYED SET SURVIVES THE SNAPSHOT BOUNDARY WITHOUT BEING WRITTEN TO IT. Nothing
            // about a refusal is persisted - it is re-derived beside the candidate set at every pause,
            // on restore, on replay and on retained-prefix Undo - so a restored modal greys what the
            // restored board greys rather than what the payload remembered.
            Assert.That(
                board.Choice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId.Value)
                    .ToArray(),
                Is.EqualTo(new[] { "familiar-ground-adjacent-bullet" }));

            TakeTheObstacleBullet(board);

            var target = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null,
                "and taking the bullet reaches p.14's own target question.");
            Assert.That(target.LegalTargetHeroIds, Has.Member(board.Enemy),
                "whose legal set the narrowing has widened to the hero standing in the open - which " +
                "is the overlay read at TARGET SELECTION rather than at composition.");
        }

        /// <summary>
        /// Answers Familiar Ground's modal with its second printed bullet - "Target a hero in range
        /// who is adjacent to 3 or more obstacles".
        /// </summary>
        private static void TakeTheObstacleBullet(StagedHeroBoard board) =>
            board.Accept(DirectorCommand.AnswerEffectChoice(board.Choice.SelectInvocations
                .Single(command =>
                    command.Candidate.OptionNodeId.Value == "familiar-ground-obstacle-bullet")));

        /// <summary>
        /// Where the two live cases above put the enemy hero: three spaces from Brynn, which is
        /// Familiar Ground's printed Range 3 exactly, and in the open with the two staged minions
        /// parked five spaces away by <c>clearTheNeighbourhood</c>.
        /// </summary>
        private static readonly HexCoordinate EnemyAt = StagedHeroBoard.ThreeAway;

        private static readonly IReadOnlyDictionary<string, HexCoordinate> EnemyInTheOpen =
            new Dictionary<string, HexCoordinate>
            {
                [StagedBoardParticipants.RedHeroIdValue] = EnemyAt
            };

        /// <summary>
        /// The spaces that box a coordinate in: as many FREE PLAYABLE neighbours of it as
        /// <see cref="ObstacleAdjacencyQuery.PrintedObstacleThreshold"/> asks for and not one more,
        /// so a case that passed on four would still be measuring the threshold.
        ///
        /// <para>They are computed off the map rather than written down, because which of a space's
        /// six neighbours are playable is a fact about the official board: a hard-coded triple would
        /// silently put a setup obstacle on a water space that is already terrain (p.19, BR-Q13) and
        /// the case would then pass while proving two.</para>
        /// </summary>
        private static HexCoordinate[] BoxSpaces(HexCoordinate around)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var taken = new[]
            {
                StagedHeroBoard.HeroAt, around, new HexCoordinate(2, 0), new HexCoordinate(2, 1)
            };

            var free = around.GetNeighbors()
                .Where(neighbour =>
                    map.Spaces.Any(space =>
                        space.Coordinate.Equals(neighbour) &&
                        space.Kind == BoardMapSpaceKind.Playable) &&
                    !taken.Any(occupied => occupied.Equals(neighbour)))
                .Take(ObstacleAdjacencyQuery.PrintedObstacleThreshold)
                .ToArray();

            Assert.That(
                free, Has.Length.EqualTo(ObstacleAdjacencyQuery.PrintedObstacleThreshold),
                "The staged board has to leave three free playable neighbours around " + around +
                " for this case to box it in with exactly the printed threshold.");
            return free;
        }

        /// <summary>
        /// How many of the enemy hero's six neighbours are obstacles in a live match, asked through
        /// p.18's own classification rather than recounted here.
        /// </summary>
        private static int ObstaclesBesideTheEnemy(StagedHeroBoard board)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var projection = board.Session.Projection;
            var occupied = projection.Heroes.Select(hero => hero.Coordinate)
                .Concat(projection.Minions.Select(minion => minion.Coordinate))
                .Concat(projection.Obstacles.Select(obstacle => obstacle.Coordinate))
                .ToArray();

            return board.HeroSpace(board.Enemy).GetNeighbors()
                .Count(neighbour =>
                    occupied.Any(value => value.Equals(neighbour)) ||
                    !map.Spaces.Any(space =>
                        space.Coordinate.Equals(neighbour) &&
                        space.Kind == BoardMapSpaceKind.Playable));
        }
    }
}
