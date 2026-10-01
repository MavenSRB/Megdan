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
    /// Milestone 8.71h, the milestone that DEALS Hanu the Trickster as the twenty-first hero: the half
    /// of a dealing that is about the CONFIGURATION rather than about the content - the selector row
    /// with the profile facts the user asked for by name, the hand dispatch, the four staged stagings,
    /// the registration flip, and the two boards 8.71i's journeys will be written on.
    ///
    /// <para>Milestone 8.35z41's rule is what the first case asserts from the roster's own side: the
    /// offerable roster and <c>MatchConfiguration</c>'s hand switch are two halves of one claim, and a
    /// hero present in the first but absent from the second is a pick that throws at the moment the
    /// match composes.</para>
    ///
    /// <para><b>The last cases are the debt his six composition blocks recorded.</b> Every one of them
    /// proved its faces against a hand-built board and a hand-built capability catalog, because a
    /// configured match cannot deal a hero the build does not register - so Hurry up! rewriting a
    /// printed icon a LIVE turn order then reads, The Ultimate Trick's delegation derived from a real
    /// projection with two real seats, and a friendly PAIR offered by the board a player would be
    /// looking at, could not be asked until this milestone. They are asked here.</para>
    /// </summary>
    public sealed class HanuDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedHanuCarriesHisEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var hanu = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == HanuCardContent.ContentSetId);
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(29),
                "He is the twenty-first hero this build can put on a board.");
            Assert.That(hanu.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.HanuHeroIdValue));
            Assert.That(hanu.DisplayName, Is.EqualTo("Hanu the Trickster"),
                "Setup step 7's own name, which is a label on the seat rather than an identity - the " +
                "DASHBOARD's epithet, and for the first time in three dealings the Tabletopia card " +
                "group prints the same words.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { hanu }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick
                : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);

            var cards = configuration.CardInstances
                .Where(card => card.OwnerId.Value == hanu.HeroIdValue)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(18),
                "Eighteen dealt cards; Fight or Flight is p.20's boxed Handicap and is not one.");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(HanuCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(HanuCardContent.Deck));

            Assert.That(
                HeroLineupComposition.Compose(format, lineup).Heroes
                    .Single(hero => hero.ContentId.Value == HanuCardContent.ContentSetId)
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
            var hanu = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == HanuCardContent.ContentSetId);

            Assert.That(hanu.Complexity, Is.EqualTo(HeroComplexity.VeryComplex),
                "Three filled stars on `_selection-cards/Hanu__front.png`, which p.21 calls Very " +
                "Complex - read off the extracted front at the dealing rather than inferred.");
            Assert.That(
                hanu.Roles,
                Is.EqualTo(HeroRole.Tactician | HeroRole.Sniper | HeroRole.Pusher),
                "and the three role icons the same front prints, in its own printed order.");
            Assert.That(hanu.Roles & HeroRole.Healer, Is.EqualTo(HeroRole.None),
                "He carries NO Healer role, which is the role the rendered filter case uses to prove " +
                "the filter subtracts as well as adds: selecting it must hide his button.");
        }

        [TestCase(1, HanuUpgradeBranch.FirstSibling)]
        [TestCase(2, HanuUpgradeBranch.FirstSibling)]
        [TestCase(2, HanuUpgradeBranch.SecondSibling)]
        [TestCase(3, HanuUpgradeBranch.FirstSibling)]
        [TestCase(3, HanuUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksTheUltimate(
            int tier, HanuUpgradeBranch branch)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedHanuForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedHanuTierTwoForInternalTest(
                    TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedHanuTierThreeForInternalTest(TeamSide.Blue, branch);

            var ids = HanuCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
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
                        HanuCardContent.HelpingHand,
                        HanuCardContent.UnexpectedJourney,
                        HanuCardContent.MonkeyTrick
                    }
                    : tier == 2 && branch == HanuUpgradeBranch.FirstSibling
                        ? new[]
                        {
                            HanuCardContent.EvenTheOdds,
                            HanuCardContent.ThereAndBackAgain,
                            HanuCardContent.MonkeyTwist
                        }
                        : tier == 2
                            ? new[]
                            {
                                HanuCardContent.Outnumber,
                                HanuCardContent.ThisWay,
                                HanuCardContent.HearNothing
                            }
                            : branch == HanuUpgradeBranch.FirstSibling
                                ? new[]
                                {
                                    HanuCardContent.TrustedSidekick,
                                    HanuCardContent.SafeTravels,
                                    HanuCardContent.MonkeyBusiness
                                }
                                : new[]
                                {
                                    HanuCardContent.PileOn,
                                    HanuCardContent.ThatWay,
                                    HanuCardContent.SeeNothing
                                };

            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[]
                {
                    HanuCardContent.FightAndFlight,
                    HanuCardContent.HurryUp
                })),
                "Both basics stay in hand at every stage, exactly as p.6 leaves them - and for him " +
                "that is load-bearing beyond the convention: Hurry up! is the face The Ultimate " +
                "Trick RIDES, so a branch that upgraded it away would put his Purple on a board " +
                "where it can never name a hero.");

            var ultimate = cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(HanuCardContent.TheUltimateTrick));
            Assert.That(
                ultimate.State.Location,
                Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck),
                "p.16 puts the Ultimate beside the dashboard only once it is unlocked, and his " +
                "delegation is derived from exactly that card being there.");
        }

        /// <summary>
        /// THE STAGED BOARD, MEASURED AND RECORDED. What 8.71i needs from this board is four things
        /// his set asks for, and this case measures all four rather than restating a coordinate: an
        /// ENEMY HERO inside every printed Range and Radius his set carries and outside p.19's ring,
        /// an ADJACENT UNIT for the "a unit adjacent to you" bullet of all five Reds and for Fight and
        /// Flight, a FRIENDLY unit among them for the friendly PAIR his Greens swap and for the Reds'
        /// second bullet, and A STRAIGHT LINE of three free spaces for Fight and Flight's rider.
        ///
        /// <para>Milestone 8.40j's own dealing is why this case exists at all: it found every Attack
        /// face of a hero offering minions only, because the two heroes stood two spaces apart around
        /// a minion and the gap was bent rather than straight.</para>
        /// </summary>
        [Test]
        public void TheEngagedStagedBoard_CarriesEverythingHisFacesAskFor()
        {
            var configuration = MatchConfiguration.CreateStagedHanuForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusHanuOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var hanu = setup.Heroes
                .Single(hero => hero.ContentId.Value == HanuCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(hanu.HeroId));

            var separation = HexDistanceQuery.Calculate(hanu.Coordinate, enemy.Coordinate);
            var adjacentUnits = setup.Minions
                .Where(minion => HexDistanceQuery.Calculate(hanu.Coordinate, minion.Coordinate) == 1)
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
                        HexDistanceQuery.Calculate(hanu.Coordinate, coordinate) <= radius &&
                        !coordinate.Equals(hanu.Coordinate) &&
                        IsFree(coordinate))
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            var friendlyWithin = new Dictionary<int, string[]>();
            foreach (var radius in new[] { 1, 2, 3 })
            {
                friendlyWithin[radius] = setup.Minions
                    .Where(minion =>
                        minion.TeamSide == hanu.TeamSide &&
                        HexDistanceQuery.Calculate(hanu.Coordinate, minion.Coordinate) <= radius)
                    .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate))
                    .OrderBy(value => value, System.StringComparer.Ordinal)
                    .ToArray();
            }

            // FIGHT AND FLIGHT's rider is "move 3 spaces in a straight line", which p.19 makes a file
            // rather than any three steps - so what it needs is a DIRECTION out of his own space with
            // three free playable spaces along it, and a board that only offered bent routes would
            // leave the rider unreachable for a reason belonging to the staging.
            var straightLines = hanu.Coordinate.GetNeighbors()
                .Select(neighbour => new
                {
                    Step = new HexCoordinate(
                        neighbour.Q - hanu.Coordinate.Q, neighbour.R - hanu.Coordinate.R),
                    Neighbour = neighbour
                })
                .Where(direction => Enumerable.Range(1, 3).All(distance => IsFree(new HexCoordinate(
                    hanu.Coordinate.Q + (direction.Step.Q * distance),
                    hanu.Coordinate.R + (direction.Step.R * distance)))))
                .Select(direction => Describe(direction.Neighbour))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            TestContext.WriteLine(
                "HANU STAGED BOARD (engaged, one seat per team): Hanu " + Describe(hanu.Coordinate) +
                ", enemy " + Describe(enemy.Coordinate) + ", separation " + separation +
                ". Units adjacent to him: " +
                (adjacentUnits.Length == 0 ? "(none)" : string.Join(", ", adjacentUnits)) +
                ". Friendly units in radius 1/2/3: " +
                string.Join(" | ", new[] { 1, 2, 3 }.Select(radius =>
                    radius + ": " + (friendlyWithin[radius].Length == 0
                        ? "(none)"
                        : string.Join(", ", friendlyWithin[radius])))) +
                ". Free spaces in radius 1/2/3/4: " + freeWithin[1].Length + "/" +
                freeWithin[2].Length + "/" + freeWithin[3].Length + "/" + freeWithin[4].Length +
                ". Straight lines of 3 free spaces out of his own space, by first step: " +
                (straightLines.Length == 0 ? "(none)" : string.Join(", ", straightLines)) + ".");

            Assert.That(hanu.Coordinate, Is.EqualTo(new HexCoordinate(-3, 1)),
                "The staged hero's space is a fact about EngagedMirrorStaging rather than about him, " +
                "and StagedHeroBoard.HeroAt says the same thing.");
            Assert.That(separation, Is.EqualTo(2),
                "Two spaces apart, which is the engagement every staged dealing since Bain's has " +
                "been measured on: inside the Radius 2 Unexpected Journey and Monkey Twist print, " +
                "inside every Range his Reds and his Silver print, and outside adjacency - so his " +
                "journeys and Hurry up! reach him and his 'adjacent to you' bullets need a unit.");

            // (1) Every Red's first bullet and Fight and Flight name "a unit adjacent to you".
            Assert.That(
                adjacentUnits.Select(value => value.Split('@')[0]),
                Is.EquivalentTo(StagedHeroBoard.StagedBesideTheHero),
                "The two pieces the engaged staging leaves beside the staged hero are the same two " +
                "every other hero's fixtures measure.");
            Assert.That(friendlyWithin[1], Is.Not.Empty,
                "and at least one of them is FRIENDLY, which is what the Reds' SECOND bullet - " +
                "'adjacent to your friendly hero' read over p.17's Friendly - and the friendly PAIR " +
                "his Greens swap both need a candidate for.");
            Assert.That(
                setup.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(hanu.Coordinate, minion.Coordinate) == 1 &&
                    minion.TeamSide != hanu.TeamSide),
                Is.True,
                "and at least one is an ENEMY, or every 'target a unit adjacent to you' bullet of " +
                "his five Reds and Fight and Flight would reach only his own side.");

            // (2) Every journey and both wordless swaps take an ENEMY HERO in the printed Radius, and
            //     Unexpected Journey prints the smallest of them.
            Assert.That(separation, Is.LessThanOrEqualTo(2),
                "Unexpected Journey prints Radius 2, the smallest radius any swap of his uses, so " +
                "the enemy hero has to be inside it or his whole Blue line reaches nobody.");

            // (3) Fight and Flight's rider needs a file of three, and the swaps need somewhere for an
            //     exchanged piece to stand.
            Assert.That(straightLines, Is.Not.Empty,
                "Fight and Flight's 'If able, move 3 spaces in a straight line' is exactly three " +
                "under p.11, so a board with no free file of three out of his space would prove the " +
                "IfAble refusal and never the move.");
            Assert.That(freeWithin[1], Is.Not.Empty,
                "and the trailing 'Move up to 1 space' on Safe Travels, Monkey Business and See " +
                "Nothing has somewhere to go.");

            // (4) THE NEGATIVE THIS BOARD IS FOR, recorded rather than asserted away: no FRIENDLY
            //     HERO stands anywhere, so This Way! and That Way! find nobody here. That is what
            //     `CreateStagedHanuWithAllyForInternalTest` exists for.
            Assert.That(
                setup.Heroes.Count(hero => hero.TeamSide == hanu.TeamSide), Is.EqualTo(1),
                "One seat per team, so p.17's 'a friendly hero' has no candidate on this board at " +
                "all - the co-moves' board is the ally one and this case says so rather than " +
                "leaving the absence to be found by a red.");
        }

        /// <summary>
        /// THE ALLY BOARD, MEASURED AND RECORDED: the one staging on which "A friendly hero in radius
        /// chooses a distance" has a candidate, because milestone 8.71e composed that selection with
        /// <c>includeSelf</c> false.
        /// </summary>
        [Test]
        public void TheAllyStagedBoard_PutsAFriendlyHeroInsideBothCoMoveRadii()
        {
            var configuration = MatchConfiguration.CreateStagedHanuWithAllyForInternalTest(
                TeamSide.Red, HanuAllyStage.TierTwo);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateStagedWaspVersusHanuWithAllyOneVsTwo(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var hanu = setup.Heroes
                .Single(hero => hero.ContentId.Value == HanuCardContent.ContentSetId);
            var ally = setup.Heroes.Single(hero =>
                hero.TeamSide == hanu.TeamSide && !hero.HeroId.Equals(hanu.HeroId));
            var enemy = setup.Heroes.Single(hero => hero.TeamSide != hanu.TeamSide);

            var toAlly = HexDistanceQuery.Calculate(hanu.Coordinate, ally.Coordinate);
            TestContext.WriteLine(
                "HANU ALLY BOARD (not restaged, three seats): Hanu " + Describe(hanu.Coordinate) +
                ", ally " + ally.ContentId.Value + " " + Describe(ally.Coordinate) + " at distance " +
                toAlly + ", enemy " + Describe(enemy.Coordinate) + " at distance " +
                HexDistanceQuery.Calculate(hanu.Coordinate, enemy.Coordinate) + ".");

            Assert.That(setup.Heroes.Count, Is.EqualTo(3),
                "Three seats: Wasp on Red, Hanu and his ally on Blue.");
            Assert.That(ally.ContentId.Value, Is.EqualTo("arien"),
                "Arien holds the ally seat, for the reason Brogan's, Tali's and Rowenna's ally " +
                "boards give it to him: he is a hero this build already deals, so the selection is " +
                "proven against real content rather than against an invented printed set.");
            Assert.That(toAlly, Is.InRange(1, 2),
                "This Way! prints Radius 3 and lets the friend choose 1 or 2; That Way! prints " +
                "Radius 3 and 1, 2 or 3. A friendly hero outside the smaller of those disks would " +
                "make both co-moves unreachable for a reason that belongs to the staging.");
            Assert.That(
                configuration.CardInstances
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId)
                    .ToArray(),
                Has.Member(HanuCardContent.ThisWay),
                "and the Tier II ally stage deals the SecondSibling branch, which is the only one " +
                "either co-move is on.");
        }

        /// <summary>
        /// His fifteen decision labels reach the one catalog presentation asks, which is where a
        /// dealing puts a hero's table - milestone 8.46a aggregates per hero exactly as
        /// <c>MatchContentCatalogs</c> aggregates capability handlers.
        /// </summary>
        [Test]
        public void HisDecisionLabels_AreInTheCatalogPresentationAsks()
        {
            var declared = HanuEffectContent.DecisionLabels().ToArray();
            Assert.That(declared, Has.Length.EqualTo(15));

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
                        HanuCardContent.ContentSetId + "-", System.StringComparison.Ordinal)),
                Is.EqualTo(declared.Select(declaration => declaration.CardId).Distinct().Count()));
        }

        /// <summary>
        /// THE REGISTRATION FLIP, asserted as one claim: his six own contracts are in the build's
        /// catalog, his three end-of-turn bodies are in the triggered-effect catalog, he contributes
        /// no token and no marker at all, and his Ultimate adds neither.
        /// </summary>
        [Test]
        public void TheBuildRegistersEveryContractHisDealtFacesNeed()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            var registered = build.Capabilities.Registered
                .Select(capability => capability.ToString())
                .Where(value => value.StartsWith("hanu-", System.StringComparison.Ordinal))
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(registered, Is.EqualTo(new[]
            {
                "hanu-friendly-unit-pair@1",
                "hanu-hurry-up-initiative@1",
                "hanu-journey-partner@1",
                "hanu-safe-travels-effect@1",
                "hanu-there-and-back-again-effect@1",
                "hanu-unexpected-journey-effect@1"
            }),
                "EXACTLY the six 8.71g named as the deferral list, and no more: a seventh arriving " +
                "is a registration somebody has to decide on rather than inherit.");

            Assert.That(
                HanuComponentContent.DeferredHandlers(), Is.Empty,
                "and nothing of his is withheld any more, which is the other side of the same flip.");

            Assert.That(
                build.TriggeredEffects.Definitions
                    .Select(definition => definition.Id.Value)
                    .Where(value => value.StartsWith("hanu-", System.StringComparison.Ordinal))
                    .OrderBy(value => value, System.StringComparer.Ordinal)
                    .ToArray(),
                Has.Length.EqualTo(3),
                "His three Blue journeys' end-of-turn bodies, one per face: 'End of turn: Swap with " +
                "that hero regardless of radius and immunity' runs at the turn's close rather than " +
                "inside the action, so the body is held by the Active effect and named by reference.");

            var ultimate = HanuCardContent
                .Definition(HanuCardContent.TheUltimateTrick).UltimateRule;
            Assert.That(ultimate, Is.Not.Null);
            Assert.That(ultimate.TriggeredEffect.HasValue, Is.False,
                "The Ultimate Trick runs NO effect - it changes who chooses, which no " +
                "RuleModification family can say - so it adds nothing to either catalog.");
            Assert.That(ultimate.Modifications, Is.Empty);
            Assert.That(
                ultimate.RuleKind, Is.EqualTo(DelegatedActionQuery.DelegatingRuleKind),
                "and the rule kind is the one the delegation query reads by name, which is Ursafar's " +
                "Unbound Fury shape and the only way a permanent ability of this build is recognised.");
        }

        /// <summary>
        /// The offerable roster and the hand switch are two halves of one claim, read from both sides:
        /// every hero a selector offers actually deals, and his arm is what keeps that true for the
        /// twenty-first. Milestone 8.35z41's own rule, asserted for him by name because 8.35z46 broke
        /// it for a hero who was otherwise completely dealt.
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

            Assert.That(dealt, Has.Member(HanuCardContent.ContentSetId));
            Assert.That(dealt, Has.Count.EqualTo(29));
            Assert.That(dealt.Distinct().Count(), Is.EqualTo(29),
                "and no two rows name one printed set.");
        }

        // -------------------------------------------------------------------------------------------
        // The debts his composition blocks recorded, which only a dealt hero can discharge
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// 8.71a'S DEBT: Hurry up! rewrites an enemy's printed Initiative icon inside a CONFIGURED
        /// MATCH, and the number the turn order reads afterwards is the rewritten one.
        ///
        /// <para>Every case 8.71a could write drove a bounded effect frame over a hand-built catalog,
        /// because a configured match cannot deal a hero the build does not register. What this adds
        /// is the whole path: two real seats commit, the Skill is OFFERED by the board a player would
        /// be looking at, the selection names the enemy, and the override is written on the enemy's
        /// own committed CARD INSTANCE - the card, not the hero, which is HA-Q1's reading and the
        /// reason it lapses with that card rather than with its owner.</para>
        /// </summary>
        [Test]
        public void HurryUp_RewritesTheEnemysCommittedCardInAConfiguredMatch()
        {
            var board = HanuBoard.Create();

            Assert.That(board.Hand(board.Hero), Has.Member(HanuCardContent.HurryUp),
                "p.6 deals his Silver basic into the opening hand.");

            // Read the offer and then start THAT offer: `PrimaryOffer` has already committed every
            // seat and drained the earlier turns, so a case that read it and then called
            // `StartPrimary` would commit a second time and be refused NotAwaitingCommitments.
            var offer = board.PrimaryOffer(HanuCardContent.HurryUp);
            Assert.That(offer.IsAvailable, Is.True,
                "The enemy hero is inside its printed Range 4 holding an unresolved card, so the " +
                "face is offered rather than greyed.");
            board.StartOffer(offer);

            var choice = board.Choice;
            Assert.That(choice, Is.Not.Null,
                "'a hero in range' is a selection, so a configured match pauses for it.");
            Assert.That(board.CandidateHeroes(choice), Is.EqualTo(new[] { board.Enemy }),
                "The enemy is the only hero holding an unresolved card: p.10 keeps Hanu himself out " +
                "because the text does not say he is affected (HA-Q1), and the arm's own " +
                "`holdsAnUnresolvedCard` narrowing is what leaves anybody else out.");

            board.Select(board.Enemy);
            board.ConfirmIfStillOwed();

            var rewritten = board.Session.Projection.Cards
                .Where(card => card.OwnerId.Equals(board.Enemy))
                .Where(card => card.InitiativeOverride.IsDefined)
                .ToArray();
            Assert.That(rewritten, Has.Length.EqualTo(1),
                "ONE card instance carries the rewrite - the enemy's own unresolved card - and the " +
                "projection carries it, which is the defect 8.71b found and fixed in CloneCard.");
            Assert.That(rewritten[0].InitiativeOverride.Value, Is.EqualTo(11),
                "the printed eleven the card declares at its own call site.");
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(
                        rewritten[0], board.Session.Projection.Definitions)
                    .Total,
                Is.EqualTo(11),
                "and the value every reader of a printed icon sees is the rewritten one rather than " +
                "the printed one plus something - it SETS, which is why it could not be composed on " +
                "any shipped additive contribution.");
        }

        /// <summary>
        /// 8.71b'S DEBT: The Ultimate Trick's delegation derived from a LIVE configured match with two
        /// real seats, which is what the brief asked for by name - the rewrite is the delegation, so
        /// unlocking the Purple and playing the Silver is the whole of it.
        ///
        /// <para>8.71b could only ask this of a projection it built itself. Here the Purple is
        /// UNLOCKED by <c>CreateStagedHanuTierThreeForInternalTest</c>, the Silver is played from a
        /// real hand, and <c>DelegatedActionQuery</c> is asked about the enemy's next action off the
        /// same projection every seat receives.</para>
        /// </summary>
        [Test]
        public void TheUltimateTrick_DelegatesTheHurriedHerosNextActionToHisSeat()
        {
            var board = HanuBoard.Create(tier: 3);

            var projection = board.Session.Projection;
            Assert.That(
                projection.Cards.Single(card =>
                    card.DefinitionReference.DefinitionId
                        .Equals(HanuCardContent.TheUltimateTrick)).State.Location,
                Is.EqualTo(CardLocation.UltimateArea),
                "p.16's unlock is what makes the permanent ability live at all.");
            Assert.That(
                DelegatedActionQuery.ForActor(
                    projection.Definitions, projection.Cards, projection.Heroes, board.Enemy)
                    .IsDefined,
                Is.False,
                "and nothing is delegated before a rewrite: the ability names 'a hero you target " +
                "with the Hurry Up!', so an unlocked Purple alone delegates nobody.");

            board.StartPrimary(HanuCardContent.HurryUp);
            board.Select(board.Enemy);
            board.ConfirmIfStillOwed();

            var after = board.Session.Projection;
            var delegation = DelegatedActionQuery.ForActor(
                after.Definitions, after.Cards, after.Heroes, board.Enemy);

            Assert.That(delegation.IsDefined, Is.True,
                "The rewrite IS the delegation - nothing is stored, sent or persisted for it - so " +
                "the moment his Silver names the enemy, that hero's next action is Hanu's to take.");
            Assert.That(delegation.DelegatedHeroId, Is.EqualTo(board.Enemy));
            Assert.That(delegation.DecidingHeroId, Is.EqualTo(board.Hero),
                "and the seat that chooses it is HANU's, which is HA-Q3 as the user settled it.");
            Assert.That(
                DelegatedActionQuery.DecidingHeroFor(
                    after.Definitions, after.Cards, after.Heroes, board.Enemy),
                Is.EqualTo(board.Hero),
                "read through the same helper the two Presentation comparisons this milestone " +
                "repointed now call, so the tray that shines is the deciding seat's.");
            Assert.That(
                DelegatedActionQuery.DecidingHeroFor(
                    after.Definitions, after.Cards, after.Heroes, board.Hero),
                Is.EqualTo(board.Hero),
                "while every other actor answers themselves, which is what keeps twenty heroes' " +
                "worth of shipped Presentation reading exactly as it did.");
        }

        /// <summary>
        /// 8.71f'S DEBT: the friendly PAIR his Greens swap, offered by a CONFIGURED MATCH. Monkey
        /// Trick prints Radius 1, so this case puts two friendly minions in p.19's own ring and reads
        /// the pair the board offers rather than the pair a fixture built.
        /// </summary>
        [Test]
        public void MonkeyTrick_OffersTheFriendlyPairAConfiguredBoardHolds()
        {
            var first = StagedHeroBoard.FirstAdjacent;
            var second = StagedHeroBoard.SecondAdjacent;
            var board = HanuBoard.Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = first,
                    ["blue-melee-02"] = second
                });

            var offer = board.PrimaryOffer(HanuCardContent.MonkeyTrick);
            Assert.That(offer.IsAvailable, Is.True,
                "Two friendly units stand in his Radius 1, so the one selection his Green prints " +
                "has a pair - and 8.35l greys the face when it does not.");
            board.StartOffer(offer);

            var choice = board.Choice;
            Assert.That(choice, Is.Not.Null);

            // The pair names UNITS rather than spaces at the question - one piece per space makes the
            // two readings the same board fact, and the contract publishes the space to the exchange
            // behind it while offering the piece to the player.
            var minions = board.CandidateMinions(choice);
            Assert.That(minions, Has.Member(board.MinionAt(first)));
            Assert.That(minions, Has.Member(board.MinionAt(second)));
            Assert.That(board.CandidateHeroes(choice), Has.No.Member(board.Hero),
                "and HANU HIMSELF is not among them: p.17's Friendly is 'another unit on your team. " +
                "It never includes you', which the contract carries rather than the card.");
            Assert.That(board.CandidateHeroes(choice), Has.No.Member(board.Enemy),
                "nor the enemy hero, because the pair is his own side's.");
        }

        private static string Describe(HexCoordinate coordinate) =>
            "(" + coordinate.Q + ", " + coordinate.R + ")";

        // His ARTWORK half is asserted in `ConfiguredCardArtworkCatalogTests` rather than here, and
        // the reason is structural: `ConfiguredCardArtworkCatalog` lives in the Presentation slice the
        // offline harness compiles only one file of, so a case naming it would make this whole fixture
        // Unity-only. See that file's `EveryDealtFaceOfHanus_HasAnArtworkRowAtHisOwnRevision`.
    }
}
