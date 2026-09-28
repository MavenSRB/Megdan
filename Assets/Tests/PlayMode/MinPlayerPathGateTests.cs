using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The permanent hero player-path completion gate, for Min the Dragonmonk. Milestone 8.61k, and
    /// the NINETEENTH hero to take it.
    ///
    /// <para><b>Nothing below is a checklist.</b> Every row's shape set is DERIVED from her own printed
    /// content by <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> and compared against the row
    /// exactly, so a row that claims less has a shape nothing proves and a row that claims more is a
    /// coverage claim the content does not support. A shape cannot be declared without naming the
    /// [UnityTest] that drives it, and a journey in her fixtures that no row names fails too.</para>
    ///
    /// <para><b>She introduces ONE new interaction shape</b>, which milestone 8.61j minted while
    /// dealing her and this gate is the first to rest a row on:
    /// <see cref="PlayerInteractionShape.FacedownTokenReading"/>. Her three Blue mine faces put a piece
    /// on the board whose KIND every seat but hers is forbidden to know, and the shape exists to force
    /// a journey that reads a LOSING seat's projection - the exact mirror of milestone 8.40k's public
    /// reveal, which proves the other seats LEARN something where this proves they DO NOT. Everything
    /// else of hers is a shape this table already carried; her three token systems compose on Widget's
    /// Pyro vocabulary, her cross-seat prompts fall under milestone 8.40f's same-shape ruling, and both
    /// her borrow and Perfect Self's deck card are the 8.15a whole-face card selection.</para>
    ///
    /// <para><b>Three derivation readings are worth stating outright, because each one keeps a row
    /// HONEST rather than generous.</b></para>
    /// <list type="number">
    /// <item><description><b>Viper Stance declares no board click at all.</b> Its swap consumes
    /// <c>min-smoke-bomb-in-radius@1</c> - a reading that publishes the one bomb's coordinate - and
    /// <c>synthetic-swap-actor@1</c>, which asks nobody anything. Her supply holds ONE smoke bomb
    /// (MI-Q1), so there is never a second candidate to pick between and the only question a player
    /// meets is the printed "you may". A row claiming a selection there would name a control that does
    /// not exist.</description></item>
    /// <item><description><b>Both Grenade bodies contribute their shapes to their own rows, and the
    /// mine trap to none.</b> Milestone 8.117 made the derivation walk every Active effect's own
    /// trigger body, so the End-of-turn "may", the adjacent enemy hero and that hero's discard - on the
    /// enemy's own seat, where milestone 8.40f's ruling puts a different chooser answering the same
    /// kind of control - are claims each Grenade row makes and its journey drives. The mine trap is a
    /// token's triggered effect rather than an Active effect's, so no face raises it.</description></item>
    /// <item><description><b>The Smoke bomb's screen is a TOKEN PROPERTY, not an Active
    /// effect</b>, so the Silver face declares no <c>OpponentRestrictionReading</c>. p.17's restriction
    /// reading is derived from an <c>ActiveEffectDefinition</c> the hero raises, and MI-Q2's screen is
    /// read at <c>TargetQuery.IsPrevented</c> instead. The enemy's greyed target set is asserted inside
    /// the Silver journey rather than claimed by the row.</description></item>
    /// </list>
    /// </summary>
    public sealed class MinPlayerPathGateTests
    {
        // Her journeys, named once so a rename fails at compile time rather than in the guard.
        private const string Spawn = nameof(
            MinUiJourneyPlayModeTests.HerPieceSpawnsAsMinAndHerTrayCommitsAndRevealsACard);
        private const string Secondary = nameof(
            MinUiJourneyPlayModeTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string Bomb = nameof(MinUiJourneyPlayModeTests
            .SmokeBombPlacesHerTokenAndScreensTheEnemyTargetingAcrossItsStraightLine);
        private const string Lightning = nameof(MinUiJourneyPlayModeTests
            .FastAsLightningBorrowsHerRedCardsAfterTheAttackTextAndOffersNothingWithoutOne);
        private const string Crane = nameof(MinUiJourneyPlayModeTests
            .CraneStancePushesAnAdjacentEnemyTheDistanceSheChooses);
        private const string Mines = nameof(MinUiJourneyPlayModeTests
            .TripMinePlacesTwoFacedownMinesNoOtherSeatCanIdentify);
        private const string Trap = nameof(MinUiJourneyPlayModeTests
            .AnEnemyMovementThroughHerBlastRevealsItAndTakesThatHerosOwnCard);
        private const string Poof = nameof(MinUiJourneyPlayModeTests
            .PoofSwapsWithHerSmokeBombToBlockAndIsDefeatedWithNoneInRange);
        private const string Footprint = nameof(
            MinUiJourneyPlayModeTests.EveryMinFaceUsesTheStandardFootprint);

        private const string Tiger = nameof(MinEngagedUiJourneyPlayModeTests
            .TigerStanceStepsBesideItsTargetThenPushesWhileFlurryIsStillLocked);
        private const string Viper = nameof(MinEngagedUiJourneyPlayModeTests
            .ViperStanceSwapsWithTheSmokeBombInRadiusOrStaysWhereSheIs);
        private const string Cluster = nameof(MinEngagedUiJourneyPlayModeTests
            .ClusterMinePlacesOneBlastAndTwoDudsAndOnlyTheBlastCostsACard);
        private const string Grenade = nameof(MinEngagedUiJourneyPlayModeTests
            .DeathGrenadeAsksTheChosenEnemyForACardAtTheEndOfHerTurnOrDefeatsAnEmptyHand);
        private const string Vanish = nameof(MinEngagedUiJourneyPlayModeTests
            .VanishBlocksTheAttackFromItsPrintedRangeFour);
        private const string InnerStrength = nameof(MinEngagedUiJourneyPlayModeTests
            .InnerStrengthDoublesHerItemBonusesForTheWholeRound);

        private const string Items = nameof(MinProgressedUiJourneyPlayModeTests
            .LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem);
        private const string Dragon = nameof(MinProgressedUiJourneyPlayModeTests
            .DragonStanceStepsOneOrTwoSpacesBesideTheTargetOrHoldsHerGround);
        private const string Cobra = nameof(MinProgressedUiJourneyPlayModeTests
            .CobraStanceSwapsWithTheSmokeBombThenThrowsItAgainOrLeavesIt);
        private const string MineField = nameof(MinProgressedUiJourneyPlayModeTests
            .MineFieldPlacesTwoBlastsAndOneDudAndEveryBlastCostsACard);
        private const string Holy = nameof(MinProgressedUiJourneyPlayModeTests
            .HolyDeathGrenadeTakesTwoEnemyHeroesOrStopsAtOne);
        private const string Ruse = nameof(MinProgressedUiJourneyPlayModeTests
            .RuseThrowsTheSmokeBombAgainAfterTheBlockOrLeavesItStanding);
        private const string PerfectSelf = nameof(MinProgressedUiJourneyPlayModeTests
            .PerfectSelfTakesEitherBulletOrBothAndTheDeckCardBecomesAPermanentItem);
        private const string Flurry = nameof(MinProgressedUiJourneyPlayModeTests
            .FlurryOfBlowsRepeatsHerAttackOnceOnADifferentTargetAndNeverAThird);
        private const string Lifecycle = nameof(MinProgressedUiJourneyPlayModeTests
            .HerDefeatedPieceRespawnsWithHerMinesStandingAndRestartRebuildsTheSession);
        private const string Continuity = nameof(MinProgressedUiJourneyPlayModeTests
            .HerOwnPlacementUndoesAndTheOpponentsPauseSurvivesTheReplay);
        private const string Restore = nameof(
            MinProgressedUiJourneyPlayModeTests.HerOwnMinePauseSurvivesASnapshotRestore);
        private const string Cleanup = nameof(MinProgressedUiJourneyPlayModeTests
            .EveryTokenOfHersIsRemovedAtTheEndOfTheRound);

        /// <summary>
        /// Fourteen of her nineteen faces print p.10's secondary Movement boot AND p.14's secondary
        /// Defense shield, so one journey drives both halves face by face and this is the pair those
        /// rows start from.
        /// </summary>
        private static ShapeEvidence[] Ordinary(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryMovementDestination, Secondary),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        /// <summary>
        /// MI-05, first half: Smoke Bomb prints a Defense 1 and NO Movement ribbon at all. A missing
        /// icon is absent, never zero.
        /// </summary>
        private static ShapeEvidence[] NoBoot(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        /// <summary>
        /// MI-05, second half: Poof!, Vanish and Ruse print a Movement 3 and NO Defense value
        /// whatsoever - they are Block cards whose only defense is the Block, which is Tigerclaw's
        /// Dodge precedent. A loop asking them for a secondary Defense would be asking for a control
        /// the card does not have.
        /// </summary>
        private static ShapeEvidence[] NoShield(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryMovementDestination, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        private static readonly string[] SmokeBomb = { "MinSmokeBombTests", "MinPrintedSetTests" };
        private static readonly string[] Stances = { "MinStanceTests", "MinPrintedSetTests" };
        private static readonly string[] MineSet = { "MinMineTests", "MinPrintedSetTests" };
        private static readonly string[] TrappedMineSet =
            { "MinMineTests", "MinMineTrapTests", "MinPrintedSetTests" };
        private static readonly string[] Grenades = { "MinGrenadeTests", "MinPrintedSetTests" };
        private static readonly string[] Vanishing = { "MinVanishingBlockTests", "MinPrintedSetTests" };
        private static readonly string[] Discipline = { "MinItemAndFlurryTests", "MinPrintedSetTests" };
        private static readonly string[] Borrowed = { "MinBorrowedRiderTests", "MinPrintedSetTests" };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Min",
            MinCardContent.AllDefinitions,
            MinCardContent.ReturnedToBoxAtSetup,

            // Her Active effects, which is where two of her rows' readings come from: both Grenades'
            // `End of turn:` arming and both doubling clauses' `This round:`. They are handed in
            // rather than derived, because the matrix resolves a capability to an Active effect
            // through the handler that creates it.
            MinComponentContent.Handlers(),

            // And the triggered effects the build's catalog holds for her - seven of them, of which
            // the derivation reads only the one Flurry of Blows' UltimateRule names.
            MinComponentContent.TriggeredEffects,
            new[]
            {
                // ---------------------------------------------------------------------------------
                // The two basics
                // ---------------------------------------------------------------------------------

                // MI-05: the Ranged band on a printed Range 1, so its reach IS adjacency and the
                // derivation still reads the ranged shape off the printed subtype rather than off
                // the distance it happens to cover.
                Face(
                    MinCardContent.FastAsLightning, "Fast as Lightning",
                    Ordinary(
                        Lightning,
                        RangedAttackTarget, AfterAttackTextPause, HandCardSelection),
                    Borrowed),

                Face(
                    MinCardContent.SmokeBomb, "Smoke Bomb",
                    NoBoot(Bomb, PrimarySkillControl, BoardSpaceSelection),
                    SmokeBomb),

                // ---------------------------------------------------------------------------------
                // The five Stances
                // ---------------------------------------------------------------------------------

                Face(
                    MinCardContent.CraneStance, "Crane Stance",
                    Ordinary(
                        Crane,
                        AdjacentAttackTarget, AfterAttackTextPause, BoardUnitSelection,
                        PushDistanceChoice),
                    Stances),
                Face(
                    MinCardContent.TigerStance, "Tiger Stance",
                    Ordinary(
                        Tiger,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline,
                        BoardUnitSelection, BoardSpaceSelection, PushDistanceChoice),
                    Stances),

                // Viper declares the printed `may` and nothing else - see the fixture's own remarks.
                Face(
                    MinCardContent.ViperStance, "Viper Stance",
                    Ordinary(
                        Viper, AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline),
                    Stances),

                Face(
                    MinCardContent.DragonStance, "Dragon Stance",
                    Ordinary(
                        Dragon,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline,
                        BoardUnitSelection, BoardSpaceSelection, PushDistanceChoice),
                    Stances),
                Face(
                    MinCardContent.CobraStance, "Cobra Stance",
                    Ordinary(
                        Cobra,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline,
                        BoardSpaceSelection),
                    Stances),

                // ---------------------------------------------------------------------------------
                // The five Blues: three mine faces and two Grenades
                // ---------------------------------------------------------------------------------

                // Trip Mine names the trap journey as well as the placing one, and it is the only
                // row that does: the reveal a sprung blast performs is the moment a facedown piece
                // stops being facedown, which is the other half of the shape milestone 8.61j minted.
                Face(
                    MinCardContent.TripMine, "Trip Mine",
                    Ordinary(
                        Mines, PrimarySkillControl, BoardSpaceSelection, FacedownTokenReading)
                        .Concat(new[] { Prove(FacedownTokenReading, Trap) })
                        .ToArray(),
                    TrappedMineSet),
                Face(
                    MinCardContent.ClusterMine, "Cluster Mine",
                    Ordinary(
                        Cluster, PrimarySkillControl, BoardSpaceSelection, FacedownTokenReading),
                    MineSet),
                Face(
                    MinCardContent.MineField, "Mine Field",
                    Ordinary(
                        MineField, PrimarySkillControl, BoardSpaceSelection, FacedownTokenReading),
                    MineSet),

                Face(
                    MinCardContent.DeathGrenade, "Death Grenade",
                    Ordinary(
                        Grenade,
                        PrimarySkillControl, BoardSpaceSelection, ActiveEffectReading,
                        RoundLongActiveEffectReading, BoardUnitSelection, HandCardSelection),
                    Grenades),
                Face(
                    MinCardContent.HolyDeathGrenade, "Holy Death Grenade",
                    Ordinary(
                        Holy,
                        PrimarySkillControl, BoardSpaceSelection, ActiveEffectReading,
                        RoundLongActiveEffectReading, BoardUnitSelection, HandCardSelection),
                    Grenades),

                // ---------------------------------------------------------------------------------
                // The five Greens: three vanishing Blocks and the two doubling Skills
                // ---------------------------------------------------------------------------------

                Face(
                    MinCardContent.Poof, "Poof!",
                    NoShield(Poof, BlockDefenseOffer, DefenseTextApplication),
                    Vanishing),
                Face(
                    MinCardContent.Vanish, "Vanish",
                    NoShield(Vanish, BlockDefenseOffer, DefenseTextApplication),
                    Vanishing),
                Face(
                    MinCardContent.Ruse, "Ruse",
                    NoShield(
                        Ruse,
                        BlockDefenseOffer, DefenseTextApplication, ConfirmOrDecline,
                        BoardSpaceSelection),
                    Vanishing),

                Face(
                    MinCardContent.InnerStrength, "Inner Strength",
                    Ordinary(
                        InnerStrength,
                        PrimarySkillControl, ActiveEffectReading, RoundLongActiveEffectReading),
                    Discipline),
                Face(
                    MinCardContent.PerfectSelf, "Perfect Self",
                    Ordinary(
                        PerfectSelf,
                        PrimarySkillControl, HandCardSelection, ModalBulletChoice,
                        ActiveEffectReading, RoundLongActiveEffectReading),
                    Discipline),

                // ---------------------------------------------------------------------------------
                // p.16's Ultimate, and p.20's Handicap
                // ---------------------------------------------------------------------------------

                // Flurry of Blows prints no action at all, so it is committed to no turn slot and
                // reveals nothing. What it introduces is the rail a player reads it on and the
                // printed repeat its own trigger stops the game to offer.
                Face(
                    MinCardContent.FlurryOfBlows, "Flurry of Blows",
                    new[]
                    {
                        Prove(UltimateRailVisibility, Flurry),
                        Prove(UltimateTriggerPause, Flurry),
                        Prove(ConfirmOrDecline, Flurry),
                        Prove(RepeatDecision, Flurry)
                    },
                    Discipline),

                UnreachableFace(
                    MinCardContent.ChopDown, "Chop Down",
                    "Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player " +
                    "format. The 2026-08-21 user decision named that trigger for Wasp's Crackling " +
                    "Dagger and Arien's, Widget's, Swift's, Bain's and Rowenna's boxed faces carry " +
                    "the same string."),

                // ---------------------------------------------------------------------------------
                // Her own components
                // ---------------------------------------------------------------------------------

                Component(
                    "min-piece", "Hero piece",
                    new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, Secondary),
                        Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                        Prove(PresentationRebuild, Lifecycle)
                    },
                    new[] { "MinDealingTests" }),

                Component(
                    "min-session", "Session continuity",
                    new[]
                    {
                        Prove(SnapshotRestoreContinuation, Restore),
                        Prove(RetainedPrefixUndo, Continuity),
                        Prove(ReplayReconstruction, Continuity),
                        Prove(RestartReconstruction, Lifecycle)
                    },
                    new[] { "MinGrenadeTests", "MinMineTrapTests" }),

                Component(
                    "min-footprint", "Complete faces",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "MinPrintedSetTests" }),

                Component(
                    "min-items", "Earned items",
                    new[] { Prove(ItemVisibilityAndUse, Items) },
                    new[] { "MinItemAndFlurryTests", "MinPrintedSetTests" }),

                // Her three printed token kinds have a lifetime of their own, and rulebook p.19 states
                // both ends of it in sentences no card of hers repeats: "Tokens are not removed when
                // the hero who placed them is defeated", and "Unless the card text says otherwise,
                // tokens are removed at the end of round". Both are things a player READS on the
                // board, so both are proved on the facedown reading the mines already carry - a
                // journey that could not identify the piece still has to be able to see it standing.
                Component(
                    "min-tokens", "Placed tokens",
                    new[]
                    {
                        Prove(FacedownTokenReading, Lifecycle),
                        Prove(FacedownTokenReading, Cleanup)
                    },
                    new[] { "MinMineTests", "MinGrenadeTests" }),

                AbsentComponent(
                    "min-markers", "Hero-owned markers",
                    "No Min face gives, transfers or returns a rulebook p.18 marker. Her hidden " +
                    "information is carried by facedown TOKENS instead, which are pieces on the " +
                    "board rather than attachments on a hero.")
            });

        [Test]
        public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void EveryRowDeclaresTheShapesItsContentIntroduces() =>
            Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void ReachableFacesHaveJourneysAndTheBoxedFaceHasItsReason() =>
            Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryShapeNamesAnExistingJourney() =>
            Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(MinPlayerPathGateTests).Assembly);

        [Test]
        public void EveryNamedJourneyExists() =>
            Matrix().AssertEveryNamedJourneyExists(typeof(MinPlayerPathGateTests).Assembly);

        [Test]
        public void EveryJourneyInHerFixturesIsNamedByARow() =>
            Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(MinUiJourneyPlayModeTests),
                typeof(MinEngagedUiJourneyPlayModeTests),
                typeof(MinProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryRequiredComponentShapeIsDeclared() =>
            Matrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void HerMarkerAbsenceMatchesHerContent() =>
            Matrix().AssertNoContentNamesAnyCapability(
                new[]
                {
                    "bain-give-bounty-marker", "bain-transfer-bounty-marker",
                    "bain-return-bounty-marker", "tigerclaw-give-poison-marker-1",
                    "tigerclaw-give-poison-marker-2", "give-marker"
                },
                "Min owns no marker.");

        /// <summary>
        /// Every authoritative scenario a row rests on is a real fixture in the Edit Mode assembly,
        /// which is what keeps the "source-backed" half of the gate honest: a journey proves the input
        /// route, and the fixture it names proves the rule.
        /// </summary>
        [Test]
        public void EveryAuthoritativeScenarioExists()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .Single(value => value.GetName().Name == "GuardsDigital.EditModeTests");
            foreach (var scenario in Matrix().Rows.SelectMany(row => row.AuthoritativeScenarios))
            {
                Assert.That(assembly.GetTypes().Any(type => type.Name == scenario), Is.True,
                    scenario + " is named by a row and is not a fixture in the Edit Mode assembly.");
            }
        }

        /// <summary>
        /// The count itself, pinned: nineteen printed faces, six hero-owned components, and every one
        /// of them accounted for. A twentieth face or a seventh component row fails here by name
        /// rather than by silence.
        /// </summary>
        [Test]
        public void HerTableCoversNineteenFacesAndSixComponents()
        {
            var matrix = Matrix();
            Assert.That(matrix.Rows.Count(row => !row.IsComponent), Is.EqualTo(19));
            Assert.That(matrix.Rows.Count(row => row.IsComponent), Is.EqualTo(6));
            Assert.That(matrix.AllIntroducedShapes(), Is.Not.Empty);
        }

        /// <summary>
        /// The one shape she is the first hero to introduce, pinned where a later reader will look for
        /// it: her three Blue faces declare it and nothing else in the build does.
        /// </summary>
        [Test]
        public void SheIsTheOnlyHeroWhoseContentIntroducesTheFacedownTokenReading()
        {
            var matrix = Matrix();
            var declaring = matrix.Rows
                .Where(row => !row.IsComponent && row.Shapes.Contains(FacedownTokenReading))
                .Select(row => row.SubjectId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(
                declaring,
                Is.EqualTo(new[] { "min-cluster-mine", "min-mine-field", "min-trip-mine" })
                    .AsCollection,
                "Exactly her three mine faces place a piece whose kind another seat may not read.");
        }
    }
}
