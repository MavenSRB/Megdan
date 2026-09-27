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
    /// The permanent hero player-path completion gate, for Ignatia the Mad. Milestone 8.75c, and the
    /// TWENTY-SECOND hero to take it.
    ///
    /// <para><b>Nothing below is a checklist.</b> Every row's shape set is DERIVED from her own printed
    /// content by <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> and compared against the row
    /// exactly, so a row that claims less has a shape nothing proves and a row that claims more is a
    /// coverage claim the content does not support. A shape cannot be declared without naming the
    /// [UnityTest] that drives it, and a journey in her fixtures that no row names fails too.</para>
    ///
    /// <para><b>She introduces THREE shapes this table had never carried</b>, which is more than any
    /// dealing since Bain's and the first new vocabulary since Min's. All three are the one mechanism
    /// her whole set is built on:</para>
    /// <list type="number">
    /// <item><description><see cref="CoinSelectedTextReading"/> - the same card read under both faces of
    /// rulebook p.7's Tie Breaker coin. It is on SIXTEEN of her rows - every dealt face but the Silver
    /// that frees the choice and the Ultimate that prints no action at all, with the seventeenth
    /// coin-branched face being the p.20 Handicap the box takes - and it is derived off
    /// <see cref="PrintedTextSelection"/> rather than off any node: her bodies put the coin predicate in
    /// a <c>Conditional</c>'s PREDICATE slot, which <c>AddNodeShapes</c> never consults, and milestone
    /// 8.75b's sweep found fifteen other predicates in that position and not one of them
    /// classified.</description></item>
    /// <item><description><see cref="PrintedTextFaceChoice"/> - the input half of the same reading, on
    /// the ONE face that frees it. Equilibrium's round-long marker makes the coin predicate stop reading
    /// the coin and ask; the journey that proves it reaches the pause on a primary action AND inside a
    /// repeat of one, which is what "each time you perform or repeat" asks for.</description></item>
    /// <item><description><see cref="TieBreakerCoinFlipReading"/> - the coin turning over from inside a
    /// card action, which is authoritative round state every seat reads and which no content in this
    /// build could change before Chaos Incarnate.</description></item>
    /// </list>
    ///
    /// <para><b>Four derivation readings are worth stating outright, because each keeps a row HONEST
    /// rather than generous.</b></para>
    /// <list type="number">
    /// <item><description><b>Equilibrium declares no secondary Movement.</b> IG-06: it prints a Defense 7
    /// and no Movement ribbon whatsoever, and a missing icon is absent rather than zero - so it is the
    /// one dealt face of hers whose row starts from <see cref="NoBoot"/>. It is also the only face of
    /// hers carrying <see cref="PrintedTextFaceChoice"/> and NOT
    /// <see cref="CoinSelectedTextReading"/>: the card that frees the choice cannot itself be gated by
    /// it, and its composition says so structurally rather than by a list.</description></item>
    /// <item><description><b>The coin predicate classifies to nothing, and the wake asks nobody.</b>
    /// <c>ignatia-coin-sun@1</c> and <c>ignatia-place-magma-wake@1</c> are both declared with an EMPTY
    /// shape set in the capability table - the first because a Conditional's predicate is read rather
    /// than pressed, the second because "place a magma token in each empty space you moved through" is
    /// p.10's mandatory automatic step whose spaces the Move already decided. The clicks that DO decide
    /// the trail are the straight-line offer's, which is <c>choose-line-destination@1</c>'s
    /// <see cref="BoardSpaceSelection"/>.</description></item>
    /// <item><description><b>Her four card-text Attack subjects and her two discard subjects are ONE
    /// control.</b> A player clicks a unit where it stands; that the candidate set is narrowed by p.19's
    /// straight line, by an equality against the printed Range, by p.19's ring, or by who is standing
    /// beside the candidate is a bound on the same control rather than a second shape - milestone
    /// 8.20a's own sentence.</description></item>
    /// <item><description><b>Chaos Incarnate declares no secondary anything and no commitment.</b> It
    /// prints no Initiative, no Range, no Radius and no action, so p.7 never commits it and nothing is
    /// revealed; what it introduces is the rail it is read on, the pause its trigger opens, the two
    /// answers that pause takes, and the coin it turns over.</description></item>
    /// </list>
    /// </summary>
    public sealed class IgnatiaPlayerPathGateTests
    {
        // Her journeys, named once so a rename fails at compile time rather than in the guard.
        private const string Spawn = nameof(
            IgnatiaUiJourneyPlayModeTests.HerPieceSpawnsAsIgnatiaAndHerTrayCommitsAndRevealsACard);
        private const string Secondary = nameof(
            IgnatiaUiJourneyPlayModeTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string ChaosBolt = nameof(IgnatiaUiJourneyPlayModeTests
            .ChaosBoltTakesTheAdjacentMinionUnderTheSunAndTheHeroInRangeUnderTheMoon);
        private const string Equilibrium = nameof(IgnatiaUiJourneyPlayModeTests
            .EquilibriumFreesTheRoundAndTheFaceSheAppliesIsTheOneThatRuns);
        private const string PlayingWithFire = nameof(IgnatiaUiJourneyPlayModeTests
            .PlayingWithFireTakesTheUnitOffEveryFileAndTheOneOnAFile);
        private const string AbruptCombustion = nameof(IgnatiaUiJourneyPlayModeTests
            .AbruptCombustionMakesTheQualifyingHeroDiscardAndRemovesTheQualifyingMinion);
        private const string PathOfAshesWake = nameof(IgnatiaUiJourneyPlayModeTests
            .PathOfAshesLaysItsWakeAndTheMagmaClosesTheFileUntilTheRoundEnds);
        private const string PathOfAshesScatter = nameof(IgnatiaUiJourneyPlayModeTests
            .PathOfAshesScattersItsTokensInRadiusAndEitherStepMayBeRefused);
        private const string Footprint = nameof(
            IgnatiaUiJourneyPlayModeTests.EveryIgnatiaFaceUsesTheStandardFootprint);

        private const string ErraticFireblast = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .ErraticFireblastTakesTheUnitOffEveryFileAtItsWiderRange);
        private const string CrackOfDoom = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .CrackOfDoomTakesTheAdjacentUnitAndTheUnitAtMaximumRange);
        private const string SpontaneousImmolation = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .SpontaneousImmolationCarriesTheDiscardLineAtItsWiderRadius);
        private const string SearingHeat = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .SearingHeatPushesAFriendUnderTheSunAndAnEnemyUnderTheMoon);
        private const string PathOfCinders = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .PathOfCindersLaysThreeTokensInItsWakeAndRefusesItsThirdPlacement);
        private const string UnstablePortal = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .UnstablePortalSwapsWithAFriendUnderTheSunAndAnEnemyUnderTheMoon);
        private const string Wall = nameof(IgnatiaEngagedUiJourneyPlayModeTests
            .HerAttackTargetWallsHerOwnRewindAndAFriendlyPushLeavesItOpen);

        private const string Items = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem);
        private const string LooselyAimedFirebolts = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .LooselyAimedFirebolstsRepeatsOnADifferentHero);
        private const string ImminentEruption = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ImminentEruptionRepeatsOnAMinionOrStopsAfterTheFirstAttack);
        private const string ViolentConflagration = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ViolentConflagrationDiscardsOrDefeatsAndItsMoonDefeatPaysACoin);
        private const string ScorchingBlaze = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ScorchingBlazePushesTwoOrThreeSpacesInAStraightLine);
        private const string PathOfFlames = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .PathOfFlamesSpendsHerWholePileOfFour);
        private const string MagmaShortSupply = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ChaosIncarnateRepeatsPathOfFlamesAndTheEmptyPileTakesMagmaBack);
        private const string ChaosGate = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ChaosGateSwapsAndThenMovesOneSpaceOrNone);
        private const string ChaosIncarnate = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ChaosIncarnateFlipsTheCoinAndRepeatsTheActionOnTheOppositeFace);
        private const string ChaosIncarnateRefused = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .ChaosIncarnatesTwoOffersMayEachBeRefused);
        private const string Lifecycle = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .HerDefeatedPieceRespawnsAndRestartRebuildsTheSession);
        private const string Continuity = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .HerOwnPausesUndoAndTheReplayReproducesTheirOffers);
        private const string Restore = nameof(IgnatiaProgressedUiJourneyPlayModeTests
            .HerOwnCoinPauseSurvivesASnapshotRestore);

        /// <summary>
        /// The pick screen is part of her gate, and it is a different SCENE from her match journeys -
        /// so both halves live in the lobby's own fixture and her piece row names them beside the spawn
        /// journey. She is the first hero the picker's FOURTH star has ever found, and the first to
        /// print five roles; being the twenty-second PICK and standing at the configured spawn AS
        /// Ignatia are two halves of one identity.
        /// </summary>
        private const string PickerFilters = nameof(
            PrivateLobbyPlayModeTests.IgnatiaIsFoundByHerFourStarsAndEachOfHerFiveRoles);

        private const string PickerPick = nameof(
            PrivateLobbyPlayModeTests.IgnatiaIsPickedThroughTheFilteredPickerAndClearRestoresHer);

        /// <summary>
        /// Sixteen of her dealt faces print p.10's secondary Movement boot AND p.14's secondary Defense
        /// shield AND have the coin choose between their two texts, so one journey drives the secondary
        /// pair face by face and this is the trio those rows start from.
        /// </summary>
        private static ShapeEvidence[] Ordinary(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(CoinSelectedTextReading, ownJourney),
                    Prove(SecondaryMovementDestination, Secondary),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        /// <summary>
        /// IG-06: Equilibrium prints a Defense 7 and NO Movement ribbon at all. A missing icon is
        /// absent, never zero, and it is the only face of hers the split touches - and the only one
        /// whose printed text the coin does NOT select, because it is the card that frees the choice.
        /// </summary>
        private static ShapeEvidence[] NoBoot(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        private static readonly string[] AttackScenarios =
            { "IgnatiaAttackTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] CoinScenarios =
            { "IgnatiaCoinTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] DiscardScenarios =
            { "IgnatiaDiscardTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] PushScenarios =
            { "IgnatiaPushTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] MagmaScenarios =
            { "IgnatiaMagmaTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] SwapScenarios =
            { "IgnatiaSwapTests", "IgnatiaPrintedSetTests" };
        private static readonly string[] UltimateScenarios =
            { "IgnatiaChaosIncarnateTests", "IgnatiaDealingTests" };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Ignatia",
            IgnatiaCardContent.AllDefinitions,
            IgnatiaCardContent.ReturnedToBoxAtSetup,

            // Her registered handlers, handed in because the matrix resolves a capability to an Active
            // effect through the handler that creates it. Hers is exactly one - Equilibrium's
            // round-long free-choice marker - and it is the only Active effect her whole set raises.
            IgnatiaComponentContent.RegisteredHandlers(),

            // And the triggered effects the build's catalog holds for her ULTIMATE, which is one:
            // Chaos Incarnate's flip-and-repeat body, whose own nodes are what put the
            // confirm-or-decline and the repeat on its row.
            IgnatiaEffectContent.TriggeredEffects(IgnatiaCardContent.ContentRevision),
            new[]
            {
                // ---------------------------------------------------------------------------------
                // The two basics
                // ---------------------------------------------------------------------------------

                // Her Gold. The coin picks the TARGET: a minion in her ring, or a hero inside the
                // printed Range. p.14 reaches step 1 with no target at all and the card text binds
                // one, which is why the row carries the before-the-attack pause beside the click.
                Face(
                    IgnatiaCardContent.ChaosBolt, "Chaos Bolt",
                    Ordinary(
                        ChaosBolt, RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection),
                    AttackScenarios),

                // Her Silver. The one face of hers the coin does not read, and the one that makes
                // every other face of hers ask.
                Face(
                    IgnatiaCardContent.Equilibrium, "Equilibrium",
                    NoBoot(
                        Equilibrium,
                        PrimarySkillControl, PrintedTextFaceChoice, ActiveEffectReading,
                        RoundLongActiveEffectReading),
                    CoinScenarios),

                // ---------------------------------------------------------------------------------
                // The five Reds: the straight line and its complement, the ring and the far edge
                // ---------------------------------------------------------------------------------

                Face(
                    IgnatiaCardContent.PlayingWithFire, "Playing with Fire",
                    Ordinary(
                        PlayingWithFire,
                        RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection),
                    AttackScenarios),
                Face(
                    IgnatiaCardContent.ErraticFireblast, "Erratic Fireblast",
                    Ordinary(
                        ErraticFireblast,
                        RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection),
                    AttackScenarios),
                Face(
                    IgnatiaCardContent.CrackOfDoom, "Crack of Doom",
                    Ordinary(
                        CrackOfDoom,
                        RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection),
                    AttackScenarios),
                Face(
                    IgnatiaCardContent.LooselyAimedFirebolts, "Loosely-Aimed Firebolts",
                    Ordinary(
                        LooselyAimedFirebolts,
                        RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection,
                        ConfirmOrDecline, RepeatDecision),
                    AttackScenarios),
                Face(
                    IgnatiaCardContent.ImminentEruption, "Imminent Eruption",
                    Ordinary(
                        ImminentEruption,
                        RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection,
                        ConfirmOrDecline, RepeatDecision),
                    AttackScenarios),

                // ---------------------------------------------------------------------------------
                // The five Blues: the discard line, and the two pushes
                // ---------------------------------------------------------------------------------

                Face(
                    IgnatiaCardContent.AbruptCombustion, "Abrupt Combustion",
                    Ordinary(
                        AbruptCombustion,
                        PrimarySkillControl, BoardUnitSelection, HandCardSelection),
                    DiscardScenarios),
                Face(
                    IgnatiaCardContent.SpontaneousImmolation, "Spontaneous Immolation",
                    Ordinary(
                        SpontaneousImmolation,
                        PrimarySkillControl, BoardUnitSelection, HandCardSelection),
                    DiscardScenarios),
                Face(
                    IgnatiaCardContent.ViolentConflagration, "Violent Conflagration",
                    Ordinary(
                        ViolentConflagration,
                        PrimarySkillControl, BoardUnitSelection, HandCardSelection),
                    DiscardScenarios),

                Face(
                    IgnatiaCardContent.SearingHeat, "Searing Heat",
                    Ordinary(
                        SearingHeat,
                        PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection)
                        .Concat(new[]
                        {
                            Prove(BoardUnitSelection, Wall),
                            Prove(BoardSpaceSelection, Wall)
                        })
                        .ToArray(),
                    PushScenarios),
                Face(
                    IgnatiaCardContent.ScorchingBlaze, "Scorching Blaze",
                    Ordinary(
                        ScorchingBlaze,
                        PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection),
                    PushScenarios),

                // ---------------------------------------------------------------------------------
                // The five Greens: the three magma Paths and the two swaps
                // ---------------------------------------------------------------------------------

                Face(
                    IgnatiaCardContent.PathOfAshes, "Path of Ashes",
                    Ordinary(PathOfAshesWake, PrimarySkillControl, BoardSpaceSelection)
                        .Concat(new[]
                        {
                            Prove(CoinSelectedTextReading, PathOfAshesScatter),
                            Prove(PrimarySkillControl, PathOfAshesScatter),
                            Prove(BoardSpaceSelection, PathOfAshesScatter),
                            Prove(ConfirmOrDecline, PathOfAshesScatter)
                        })
                        .ToArray(),
                    MagmaScenarios),
                Face(
                    IgnatiaCardContent.PathOfCinders, "Path of Cinders",
                    Ordinary(
                        PathOfCinders,
                        PrimarySkillControl, BoardSpaceSelection, ConfirmOrDecline),
                    MagmaScenarios),
                Face(
                    IgnatiaCardContent.PathOfFlames, "Path of Flames",
                    Ordinary(
                        PathOfFlames,
                        PrimarySkillControl, BoardSpaceSelection, ConfirmOrDecline),
                    MagmaScenarios),

                Face(
                    IgnatiaCardContent.UnstablePortal, "Unstable Portal",
                    Ordinary(UnstablePortal, PrimarySkillControl, BoardUnitSelection),
                    SwapScenarios),
                Face(
                    IgnatiaCardContent.ChaosGate, "Chaos Gate",
                    Ordinary(
                        ChaosGate,
                        PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection,
                        ConfirmOrDecline),
                    SwapScenarios),

                // ---------------------------------------------------------------------------------
                // p.16's Ultimate, and p.20's Handicap
                // ---------------------------------------------------------------------------------

                // Chaos Incarnate prints no Initiative, no values and no action, so it is committed to
                // no turn slot and reveals nothing; what it introduces is the rail a player reads it
                // on, the pause its own trigger opens after a primary action, the two answers that
                // pause and the one inside it take, and the coin it turns over - which is the only
                // content in this build that changes what every other card of hers says.
                Face(
                    IgnatiaCardContent.ChaosIncarnate, "Chaos Incarnate",
                    new[]
                    {
                        Prove(UltimateRailVisibility, ChaosIncarnate),
                        Prove(UltimateTriggerPause, ChaosIncarnate),
                        Prove(TieBreakerCoinFlipReading, ChaosIncarnate),
                        Prove(ConfirmOrDecline, ChaosIncarnate),
                        Prove(RepeatDecision, ChaosIncarnate),
                        Prove(ConfirmOrDecline, ChaosIncarnateRefused),
                        Prove(RepeatDecision, ChaosIncarnateRefused),
                        Prove(TieBreakerCoinFlipReading, ChaosIncarnateRefused)
                    },
                    UltimateScenarios),

                UnreachableFace(
                    IgnatiaCardContent.UnreliableJolt, "Unreliable Jolt",
                    "Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player " +
                    "format. The 2026-08-21 user decision named that trigger for Wasp's Crackling " +
                    "Dagger and Arien's, Widget's, Swift's, Bain's, Rowenna's, Min's, Brynn's and " +
                    "Hanu's boxed faces carry the same string."),

                // ---------------------------------------------------------------------------------
                // Her own components
                // ---------------------------------------------------------------------------------

                Component(
                    "ignatia-piece", "Hero piece",
                    new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPieceSpawnAndIdentity, PickerFilters),
                        Prove(HeroPieceSpawnAndIdentity, PickerPick),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, Secondary),
                        Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                        Prove(PresentationRebuild, Lifecycle)
                    },
                    new[] { "IgnatiaDealingTests" }),

                Component(
                    "ignatia-session", "Session continuity",
                    new[]
                    {
                        Prove(SnapshotRestoreContinuation, Restore),
                        Prove(RetainedPrefixUndo, Continuity),
                        Prove(RetainedPrefixUndo, Wall),
                        Prove(ReplayReconstruction, Continuity),
                        Prove(RestartReconstruction, Lifecycle)
                    },
                    new[] { "IgnatiaCoinThreadingTests", "IgnatiaMagmaTests" }),

                Component(
                    "ignatia-footprint", "Complete faces",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "IgnatiaPrintedSetTests" }),

                Component(
                    "ignatia-items", "Earned items",
                    new[] { Prove(ItemVisibilityAndUse, Items) },
                    new[] { "IgnatiaPrintedSetTests" }),

                // Her three magma Paths place them, and p.19 states both ends of their lifetime in
                // sentences no card of hers repeats: "All tokens are obstacles", and "Unless the card
                // text says otherwise, tokens are removed at the end of round". Both are things a
                // player READS on the board, and both are proved on the wake's own trail - a token
                // the next Movement cannot cross, and an empty board when the round closes.
                Component(
                    "ignatia-tokens", "Placed magma tokens",
                    new[]
                    {
                        Prove(BoardSpaceSelection, PathOfAshesWake),
                        Prove(BoardSpaceSelection, PathOfAshesScatter),
                        Prove(BoardSpaceSelection, PathOfFlames),
                        Prove(BoardSpaceSelection, MagmaShortSupply)
                    },
                    new[] { "IgnatiaMagmaTests", "IgnatiaDealingTests" }),

                AbsentComponent(
                    "ignatia-markers", "Hero-owned markers",
                    "No Ignatia face gives, transfers or returns a rulebook p.18 marker. What her " +
                    "set carries instead is a TOKEN supply of three and a coin nobody owns: the Tie " +
                    "Breaker coin is the match's, and her Ultimate turns it over rather than " +
                    "holding it.")
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
                typeof(IgnatiaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryNamedJourneyExists() =>
            Matrix().AssertEveryNamedJourneyExists(typeof(IgnatiaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryJourneyInHerFixturesIsNamedByARow() =>
            Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(IgnatiaUiJourneyPlayModeTests),
                typeof(IgnatiaEngagedUiJourneyPlayModeTests),
                typeof(IgnatiaProgressedUiJourneyPlayModeTests));

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
                "Ignatia owns no marker; what she places is a token.");

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
        /// of them accounted for. A twentieth face or a seventh component row fails here by name rather
        /// than by silence.
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
        /// SIXTEEN of her eighteen dealt faces have the coin choose which of their two printed texts
        /// applies; one - the Silver - frees the choice instead; and one - the Ultimate, which prints
        /// a single sentence and no action at all - declares neither. Seventeen of her NINETEEN printed
        /// faces are coin-branched, the seventeenth being the p.20 Handicap p.6 returns to the box.
        ///
        /// <para>It is pinned here rather than in a paragraph because it is the sentence her whole
        /// implementation is about: a later slice that quietly composed one of her faces without the
        /// coin Conditional would take that face's <see cref="CoinSelectedTextReading"/> away, and this
        /// is where a reader would look for the number.</para>
        /// </summary>
        [Test]
        public void SixteenOfHerDealtFacesAreCoinSelectedAndTheSilverFreesTheChoice()
        {
            var coinSelected = IgnatiaCardContent.DealtDefinitions
                .Where(definition =>
                    definition.PrintedTextSelection == PrintedTextSelection.TieBreakerCoin)
                .Select(definition => definition.Id.Value)
                .ToArray();
            Assert.That(coinSelected, Has.Length.EqualTo(16),
                "Sixteen DEALT faces print two texts the coin chooses between. Derived: " +
                string.Join(", ", coinSelected));
            Assert.That(
                IgnatiaCardContent.AllDefinitions.Count(definition =>
                    definition.PrintedTextSelection == PrintedTextSelection.TieBreakerCoin),
                Is.EqualTo(17),
                "and seventeen of her nineteen PRINTED faces are, the seventeenth being the p.20 " +
                "Handicap the box takes at setup.");
            Assert.That(
                IgnatiaCardContent.DealtDefinitions
                    .Where(definition =>
                        definition.PrintedTextSelection == PrintedTextSelection.FreesTheCoinChoice)
                    .Select(definition => definition.Id.Value)
                    .ToArray(),
                Is.EqualTo(new[] { IgnatiaCardContent.Equilibrium.Value }).AsCollection,
                "Exactly one frees it.");
            Assert.That(
                IgnatiaCardContent.DealtDefinitions
                    .Where(definition =>
                        definition.PrintedTextSelection == PrintedTextSelection.None)
                    .Select(definition => definition.Id.Value)
                    .ToArray(),
                Is.EqualTo(new[] { IgnatiaCardContent.ChaosIncarnate.Value }).AsCollection,
                "and exactly one declares neither, which is the Ultimate: p.16 prints one sentence " +
                "on it and no action, so there is no primary body for the coin to branch and " +
                "nothing for a marker to describe.");
        }

        /// <summary>
        /// The three shapes she is the first hero to name, and the whole set of shapes she raises. It
        /// is pinned where a later reader will look for it, because "the twenty-second hero needed
        /// three new shapes" is a fact about the ENGINE rather than about her - and a later refinement
        /// that quietly gave one of her contracts a control would fail here rather than in a paragraph.
        /// </summary>
        [Test]
        public void SheIntroducesTheThreeShapesTheTableHadNeverCarried()
        {
            var hers = Matrix().AllIntroducedShapes();
            Assert.That(
                hers,
                Is.SupersetOf(new[]
                {
                    CoinSelectedTextReading, PrintedTextFaceChoice, TieBreakerCoinFlipReading
                }),
                "Her three are the one mechanism her whole set is built on. Derived: " +
                string.Join(", ", hers.Select(shape => shape.ToString())));
            Assert.That(
                hers,
                Is.SupersetOf(new[]
                {
                    CommitAndReveal, RangedAttackTarget, PrimarySkillControl,
                    SecondaryMovementDestination, SecondaryDefenseOffer, BeforeAttackTextPause,
                    BoardUnitSelection, BoardSpaceSelection, HandCardSelection, ConfirmOrDecline,
                    RepeatDecision, ActiveEffectReading, RoundLongActiveEffectReading,
                    UltimateRailVisibility, UltimateTriggerPause
                }),
                "and everything else she asks for is a shape this table carried before her.");

            Assert.That(
                hers.Intersect(new[]
                {
                    AdjacentAttackTarget, AfterAttackTextPause, ModalBulletChoice,
                    PushDistanceChoice, ResourceAmountChoice, DirectionChoice, ColorNamingChoice,
                    HeldMarkerReading, PublicCardRevealReading, FacedownTokenReading,
                    OpponentRestrictionReading, ActionTextPlacementChoice, BeforeMovementTextPause,
                    AfterMovementTextPause, DefenseTextApplication, PrimaryDefenseOffer,
                    BlockDefenseOffer, PrimaryMovementDestination, MinionDefenseSuppressionReading,
                    RespawnedMinionTypeChoice, HeroCoinBalanceReading
                }),
                Is.Empty,
                "She holds no marker, reveals no card, places nothing facedown, names no direction " +
                "and no colour, prints no Block and no primary Defense, prints no Movement primary " +
                "and no Movement card text, opens no p.10 \"Choose one\" modal at all - every one " +
                "of her eighteen dealt bodies is a Conditional on the coin rather than a choice a " +
                "player makes - and her Attacks all print a Range, so not one of them is p.13's " +
                "adjacency by rule. Derived: " +
                string.Join(", ", hers.Select(shape => shape.ToString())));
        }
    }
}
