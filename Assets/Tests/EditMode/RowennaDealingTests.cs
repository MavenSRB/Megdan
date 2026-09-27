using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57g, the milestone that DEALS Rowenna the Vanguard as the eighteenth hero: the
    /// half of a dealing that is about the CONFIGURATION rather than about the content - the selector
    /// row, the hand dispatch, the three staged stagings, the ally board her Red riders need, and the
    /// engaged board 8.57h's journeys will be written on.
    ///
    /// <para>Milestone 8.35z41's rule is what the first case asserts from the roster's own side: the
    /// offerable roster and <c>MatchConfiguration</c>'s hand switch are two halves of one claim, and a
    /// hero present in the first but absent from the second is a pick that throws at the moment the
    /// match composes.</para>
    /// </summary>
    public sealed class RowennaDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedRowennaCarriesHerEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var rowenna = RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == RowennaCardContent.ContentSetId);
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(24),
                "She was the eighteenth hero this build could put on a board; milestone 8.61j " +
                "deals Min as the nineteenth, 8.69g Brynn as the twentieth, 8.71h Hanu as the " +
                "twenty-first and 8.75b Ignatia as the twenty-second.");
            Assert.That(rowenna.HeroIdValue,
                Is.EqualTo(QuickGameSetupConfiguration.RowennaHeroIdValue));
            Assert.That(rowenna.DisplayName, Is.EqualTo("Rowenna the Vanguard"),
                "Setup step 7's own name, which is a label on the seat rather than an identity.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { rowenna }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick
                : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);

            var cards = configuration.CardInstances
                .Where(card => card.OwnerId.Value == rowenna.HeroIdValue)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(18),
                "Eighteen dealt cards; Earn the Spurs is p.20's boxed Handicap and is not one.");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RowennaCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RowennaCardContent.Deck));

            Assert.That(
                HeroLineupComposition.Compose(format, lineup).Heroes
                    .Single(hero => hero.ContentId.Value == RowennaCardContent.ContentSetId)
                    .SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"),
                "A selected hero takes the seat and the marked point of the position it was chosen " +
                "into; the hero column moves and no seat does.");
        }

        [TestCase(1, RowennaUpgradeBranch.FirstSibling)]
        [TestCase(2, RowennaUpgradeBranch.FirstSibling)]
        [TestCase(2, RowennaUpgradeBranch.SecondSibling)]
        [TestCase(3, RowennaUpgradeBranch.FirstSibling)]
        [TestCase(3, RowennaUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksTheUltimate(
            int tier, RowennaUpgradeBranch branch)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedRowennaForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedRowennaTierTwoForInternalTest(
                    TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedRowennaTierThreeForInternalTest(TeamSide.Blue, branch);

            var ids = RowennaCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
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
                        RowennaCardContent.TokenOfGratitude,
                        RowennaCardContent.StandGuard,
                        RowennaCardContent.CloseQuarters
                    }
                    : tier == 2 && branch == RowennaUpgradeBranch.FirstSibling
                        ? new[]
                        {
                            RowennaCardContent.FairShare,
                            RowennaCardContent.ProtectTheWeak,
                            RowennaCardContent.Melee
                        }
                        : tier == 2
                            ? new[]
                            {
                                RowennaCardContent.FeatOfBravery,
                                RowennaCardContent.AcceptSurrender,
                                RowennaCardContent.OpeningShots
                            }
                            : branch == RowennaUpgradeBranch.FirstSibling
                                ? new[]
                                {
                                    RowennaCardContent.ParagonOfGrace,
                                    RowennaCardContent.DefendTheInnocent,
                                    RowennaCardContent.GrandMelee
                                }
                                : new[]
                                {
                                    RowennaCardContent.ParagonOfValor,
                                    RowennaCardContent.GloriousTriumph,
                                    RowennaCardContent.OpeningVolley
                                };

            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[]
                {
                    RowennaCardContent.CodeOfChivalry,
                    RowennaCardContent.ThrowTheGauntlet
                })),
                "Both basics stay in hand at every stage, exactly as p.6 leaves them.");

            var ultimate = cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(RowennaCardContent.FabledLance));
            Assert.That(
                ultimate.State.Location,
                Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck),
                "p.16 puts the Ultimate beside the dashboard only once it is unlocked, and her " +
                "permanent ability is in effect exactly while it is there.");
        }

        /// <summary>
        /// The ally staging seats a FRIENDLY HERO, which is the one thing a one-Blue-hero board cannot
        /// offer her: milestone 8.57a composed "a friendly hero in radius" with <c>includeSelf</c>
        /// false, so her three Red riders and the two Paragons' gated repeat have no candidate without
        /// this configuration.
        /// </summary>
        [TestCase(RowennaAllyStage.StartingHand, 5)]
        [TestCase(RowennaAllyStage.TierTwo, 5)]
        [TestCase(RowennaAllyStage.TierThree, 5)]
        public void TheAllyStagingSeatsAFriendlyHeroBesideHer(RowennaAllyStage stage, int inHand)
        {
            var configuration =
                MatchConfiguration.CreateStagedRowennaWithAllyForInternalTest(TeamSide.Red, stage);

            var rowennaSeat = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var allySeat = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

            Assert.That(
                configuration.Participants.Heroes.Select(hero => hero.Id),
                Has.Member(rowennaSeat).And.Member(allySeat),
                "Three seats: Wasp on Red, Rowenna and her ally on Blue.");
            Assert.That(
                configuration.Participants.GetTeamForHero(rowennaSeat).Side,
                Is.EqualTo(configuration.Participants.GetTeamForHero(allySeat).Side),
                "and the ally is FRIENDLY, which is the whole point of the board.");

            var hers = configuration.CardInstances
                .Where(card => card.OwnerId.Equals(rowennaSeat))
                .ToArray();
            Assert.That(hers, Has.Length.EqualTo(18));
            Assert.That(hers.Count(card => card.State.Location == CardLocation.Hand),
                Is.EqualTo(inHand));
            Assert.That(
                hers.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RowennaCardContent.DealtDefinitions.Select(d => d.Id)),
                "Her own seat holds her printed set at every stage of this board.");

            var riderBearing = new[]
            {
                RowennaCardContent.TokenOfGratitude,
                RowennaCardContent.FairShare,
                RowennaCardContent.FeatOfBravery,
                RowennaCardContent.ParagonOfGrace,
                RowennaCardContent.ParagonOfValor
            };
            var inHandIds = hers.Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();
            Assert.That(inHandIds.Intersect(riderBearing), Is.Not.Empty,
                "and every stage of this board holds a Red that actually prints the rider, so no " +
                "arm of it exists for nothing.");
        }

        /// <summary>
        /// THE STAGED BOARD, MEASURED AND RECORDED. Milestone 8.40j's own dealing is why: it found
        /// every Attack face of a hero offering minions only, because the two heroes stood two spaces
        /// apart around a minion and the gap was bent rather than straight - a fact about the board
        /// that decided which journeys were possible and which were not.
        ///
        /// <para>What 8.57h needs from this board is four things her set asks for, and this case
        /// measures all four rather than restating a coordinate: an ADJACENT UNIT for her Reds to
        /// attack, an ENEMY HERO inside Throw the Gauntlet's printed Range 4, an ENEMY HERO reachable
        /// by her Greens' two-space Movement into adjacency, and an ENEMY HERO inside the Radius 3 her
        /// Opening Shots print. The coordinates are written to the test output so the next milestone
        /// starts from a known board rather than from a search.</para>
        ///
        /// <para><b>MEASURED, 2026-09-19:</b> <c>Rowenna (-3, 1), enemy (-2, 2), separation 2. Units
        /// adjacent to her: blue-melee-01@(-3, 2) Blue, red-ranged-01@(-4, 2) Red.</c> So her Reds
        /// have a friendly minion and an ENEMY minion beside her on turn one; the enemy HERO is two
        /// away, which is inside Throw the Gauntlet's Range 4 and Opening Shots' Radius 3 and outside
        /// every "adjacent" her set prints - and a two-space gap that is BENT rather than straight,
        /// which is the fact milestone 8.40j's dealing had to discover the hard way about the same
        /// engagement.</para>
        /// </summary>
        [Test]
        public void TheEngagedStagedBoard_CarriesEveryThingHerFacesAskFor()
        {
            var configuration = MatchConfiguration.CreateStagedRowennaForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusRowennaOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var rowenna = setup.Heroes
                .Single(hero => hero.ContentId.Value == RowennaCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(rowenna.HeroId));

            var separation = HexDistanceQuery.Calculate(rowenna.Coordinate, enemy.Coordinate);
            var adjacentUnits = setup.Minions
                .Where(minion =>
                    HexDistanceQuery.Calculate(rowenna.Coordinate, minion.Coordinate) == 1)
                .Select(minion => minion.MinionId.Value + "@" + Describe(minion.Coordinate) +
                    " " + minion.TeamSide)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            TestContext.WriteLine(
                "ROWENNA STAGED BOARD (engaged, one seat per team): Rowenna " +
                Describe(rowenna.Coordinate) + ", enemy " + Describe(enemy.Coordinate) +
                ", separation " + separation + ". Units adjacent to her: " +
                (adjacentUnits.Length == 0 ? "(none)" : string.Join(", ", adjacentUnits)) + ".");

            Assert.That(rowenna.Coordinate, Is.EqualTo(new HexCoordinate(-3, 1)),
                "The staged hero's space is a fact about EngagedMirrorStaging rather than about her, " +
                "and StagedHeroBoard.HeroAt says the same thing.");
            Assert.That(separation, Is.EqualTo(2),
                "Two spaces apart, which is the engagement every staged dealing since Bain's has " +
                "been measured on: inside every Range and Radius her set prints and outside " +
                "adjacency, so her Attacks need a step first and her Gauntlet does not.");

            // (1) Her Reds want an adjacent unit to attack. p.8's waves supply it here.
            Assert.That(adjacentUnits, Is.Not.Empty,
                "Her five Reds are Attacks printed 'adjacent to you'; a board with nothing beside " +
                "her makes all five unreachable without a Movement first.");

            // (2) Throw the Gauntlet reads a hero in its printed Range 4.
            Assert.That(separation, Is.LessThanOrEqualTo(4),
                "Throw the Gauntlet names an enemy hero in Range 4 and then a space adjacent to him.");

            // (3) Her Greens move 2 and then read "adjacent to an enemy hero".
            var freeBesideTheEnemy = enemy.Coordinate.GetNeighbors()
                .Where(coordinate =>
                    map.Spaces.Any(space =>
                        space.Coordinate.Equals(coordinate) &&
                        space.Kind == BoardMapSpaceKind.Playable) &&
                    !setup.Minions.Any(minion => minion.Coordinate.Equals(coordinate)) &&
                    !setup.Heroes.Any(hero => hero.Coordinate.Equals(coordinate)))
                .ToArray();
            Assert.That(freeBesideTheEnemy, Is.Not.Empty,
                "and at least one of those neighbours is free, or no Movement of hers can end " +
                "adjacent to him at all.");
            Assert.That(
                freeBesideTheEnemy.Min(coordinate =>
                    HexDistanceQuery.Calculate(rowenna.Coordinate, coordinate)),
                Is.LessThanOrEqualTo(2),
                "Close Quarters, Melee and Grand Melee print Movement 2, and their rider reads " +
                "'adjacent to an enemy hero' AFTER the Movement action.");

            // (4) Opening Shots reads an enemy hero in its printed Radius 3, with both discards empty.
            Assert.That(separation, Is.LessThanOrEqualTo(3),
                "Opening Shots and Opening Volley name an enemy hero in Radius 3; p.6 deals every " +
                "card to a hand or a deck, so at the first turn both discard piles are empty and the " +
                "both-parties predicate is true by setup.");

            // The minions standing inside her own disks, recorded for the same reason
            // StagedHeroBoard records them: a fixture whose subject is WHICH pieces a card offers has
            // to say where these are, or the answer includes them for a reason unrelated to the card.
            Assert.That(
                adjacentUnits.Select(value => value.Split('@')[0]),
                Is.EquivalentTo(StagedHeroBoard.StagedBesideTheHero),
                "The two pieces the engaged staging leaves beside the staged hero are the same two " +
                "every other hero's fixtures measure.");
        }

        private static string Describe(HexCoordinate coordinate) =>
            "(" + coordinate.Q + ", " + coordinate.R + ")";

        // Her ARTWORK half is asserted in `ConfiguredCardArtworkCatalogTests` rather than here, and
        // the reason is structural: `ConfiguredCardArtworkCatalog` lives in the Presentation slice the
        // offline harness compiles only one file of, so a case naming it would make this whole fixture
        // Unity-only - and the claims above are exactly the ones worth being able to run in eleven
        // minutes. See that file's `EveryDealtFaceOfRowennas_HasAnArtworkRowAtHerOwnRevision`.

        /// <summary>
        /// Her fifteen decision labels reach the one catalog presentation asks, which is where a
        /// dealing puts a hero's table - milestone 8.46a aggregates per hero exactly as
        /// <c>MatchContentCatalogs</c> aggregates capability handlers.
        /// </summary>
        [Test]
        public void HerDecisionLabels_AreInTheCatalogPresentationAsks()
        {
            var declared = RowennaEffectContent.DecisionLabels().ToArray();
            Assert.That(declared, Is.Not.Empty);

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
                        RowennaCardContent.ContentSetId + "-", System.StringComparison.Ordinal)),
                Is.EqualTo(declared.Select(declaration => declaration.CardId).Distinct().Count()));
        }

        /// <summary>
        /// Milestone 8.57e's Presentation debt, discharged offline: the four drawing sites read p.13's
        /// classification through <c>RangedAttackOverlayQuery</c>, and this is the projection-shaped
        /// reading they take. Her Attack faces print NonRanged and become Ranged the moment Fabled
        /// Lance is beside her dashboard - and no other hero's do.
        /// </summary>
        [Test]
        public void TheProjectionShapedOverlay_TurnsHerPrintedAttacksRangedOnlyOnceUnlocked()
        {
            var before = MatchConfiguration.CreateStagedRowennaForInternalTest(TeamSide.Red);
            var after = MatchConfiguration.CreateStagedRowennaTierThreeForInternalTest(
                TeamSide.Red, RowennaUpgradeBranch.FirstSibling);

            var rowennaSeat = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var enemySeat = new HeroId(StagedBoardParticipants.RedHeroIdValue);

            var locked = GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                before.CardDefinitions, before.CardInstances);
            var unlocked = GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                after.CardDefinitions, after.CardInstances);

            Assert.That(
                GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery
                    .DeclaredPrintedRangeFromUnlockedUltimates(rowennaSeat, locked),
                Is.Zero,
                "Nothing changes before p.16's card is beside her dashboard.");
            Assert.That(
                GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery
                    .DeclaredPrintedRangeFromUnlockedUltimates(rowennaSeat, unlocked),
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange),
                "and once it is, her attacks count as having the printed Range her card names.");
            Assert.That(
                GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery
                    .DeclaredPrintedRangeFromUnlockedUltimates(enemySeat, unlocked),
                Is.Zero,
                "p.16 hands the ability to 'you', so no other seat reads it.");

            var printedAttacks = RowennaCardContent.DealtDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack)
                .ToArray();
            Assert.That(printedAttacks, Is.Not.Empty);
            foreach (var action in printedAttacks)
            {
                Assert.That(action.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged),
                    "Her Attack faces print p.13's adjacency, and the printed classification stays " +
                    "printed: ActionOffer equality guards every authorization path.");
                Assert.That(
                    GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery.ClassifyFromUnlockedUltimates(
                        action.AttackRange, rowennaSeat, unlocked),
                    Is.EqualTo(AttackRangeKind.Ranged),
                    "and the overlay is what the four drawing sites apply instead.");
                Assert.That(
                    GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery.ClassifyFromUnlockedUltimates(
                        action.AttackRange, rowennaSeat, locked),
                    Is.EqualTo(AttackRangeKind.NonRanged));
            }
        }

        /// <summary>
        /// The offerable roster and the hand switch are two halves of one claim, read from both sides:
        /// every hero a selector offers actually deals, and her arm is what keeps that true for the
        /// eighteenth. Milestone 8.35z41's own rule, asserted for her by name because 8.35z46 broke it
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

            Assert.That(dealt, Has.Member(RowennaCardContent.ContentSetId));
            Assert.That(dealt, Has.Count.EqualTo(24));
            Assert.That(dealt.Distinct().Count(), Is.EqualTo(24),
                "and no two rows name one printed set.");
        }
    }
}
