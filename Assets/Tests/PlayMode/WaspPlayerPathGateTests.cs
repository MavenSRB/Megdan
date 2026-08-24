using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Wasp's half of the permanent hero player-path completion gate. Milestone 8.20b.
    ///
    /// <para><b>Why this exists after the fact.</b> Wasp became configured and playable before the
    /// gate in <c>Testing/POLICY.md</c> had its machinery, so she is the one hero whose journeys were
    /// never audited by it. The page waives it for nobody - "a roster list, phase number, shared
    /// implementation, or passing earlier hero does not waive it" - so this is the backfill, and the
    /// point of doing it is that a matrix nobody ran against the first hero is a matrix that proves
    /// only what somebody remembered to write.</para>
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with
    /// <see cref="ArienPlayerPathGateTests"/>; the table below is Wasp's own. Twenty-two of her
    /// twenty-five journeys are milestone 8.15's and 8.16c's, retained rather than rewritten, and the
    /// three the gate found missing were written for it: her piece's own spawn and rebuild, the numeric
    /// secondary Defense fifteen of her faces print, and a pause of hers that survives Undo and a
    /// restore.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="WaspCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and
    /// <see cref="MatrixRowsDeclareExactlyTheShapesWaspsPrintedContentIntroduces"/> compares the two. A
    /// row cannot claim less than its face asks for, cannot claim more, and cannot name a shape without
    /// naming the journey where a player performs it, because a row's shapes <em>are</em> its
    /// evidence.</para>
    ///
    /// <para><b>Shared journeys are deliberate, and this table shares three of them widely.</b>
    /// Committing a card is one input - pick a face up in the tray, press CONFIRM FACEDOWN, meet p.7's
    /// reveal - and it is the same input for all nineteen faces; the secondary Movement icon and the
    /// numeric secondary Defense icon are likewise one shape printed on many faces. The gate's own
    /// sentence permits it: "Play Mode need not duplicate permutations that change no input,
    /// presentation, or transition shape". The face-specific half of the evidence is the authoritative
    /// scenario column, which is one row per face by construction.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class WaspPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names. Declared as constants so a row and the method it names cannot drift apart
        // silently in one direction only - a rename that misses one end fails
        // MatrixNamesOnlyJourneysThatExist rather than quietly proving nothing.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The one journey that commits a card through the real tray controls and carries it through
        /// p.7's reveal, which is what every face's commitment row rests on. Its own subject is the
        /// cross-team Initiative tie, and the card it picks up out of Wasp's hand is Shock.
        /// </summary>
        private const string TrayCommit =
            "Scene_CommitsBothHandsThroughTheRealTrayControlsAndResolvesTheTiedReveal";

        private const string PointAtWasp =
            "Scene_PointsTheTrayAtTheOtherHeroThroughAMapClickTheHudDoesNotBlock";

        private const string Spawn =
            "HeroPiece_StandsAtTheRedSpawnAsWaspAndSurvivesAPresentationRebuild";

        private const string Walk =
            "Scene_MovesByTheSecondaryMovementThroughRealControlsAndABoardClick";

        private const string DrawerAndRestart =
            "Scene_OpensTheDirectorDrawerUndoesTogglesTheLogAndResets";

        private const string ShockJourney =
            "Shock_AsksItsTwoStepSevenQuestionsThroughTheBoardAndTheDiscardControls";

        private const string PauseSurvives =
            "ShocksAfterAttackPause_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string DaggerJourney =
            "MagneticDagger_RunsItsAttackAndItsThisTurnPinWearsTheActiveOutline";

        private const string BarrierJourney =
            "StaticBarrier_IsPerformedByItsControlAndItsFieldReadsActiveOnTheTray";

        private const string OrbitAndRepeat =
            "LiftUp_OrbitsTheChosenUnitAndRepeatsOnTheSameTargetThroughRealControls";

        private const string BlockAndRespawn =
            "StopProjectiles_ClickedAgainstAMeleeAttackDefeatsItsHeroWhoRespawnsByClick";

        private const string BlockedLanePush =
            "BlockedLanePushRespawnPlace_LightsAndAnswersEverySpaceOnTheBoard";

        private const string ElectrocuteJourney =
            "Electrocute_PlayedAsItsOwnInstance_AsksItsTwoQuestionsThroughTheUi";

        private const string BoomerangAndDeflect =
            "ChargedBoomerang_TargetsOffTheStraightLineAndDeflectBlocksItByItsFace";

        private const string NumericDefense =
            "SecondaryDefense_IsOfferedBesideTheBlockAndSpentOnItsOwnNumericFace";

        private const string PushBatch =
            "KineticRepulse_SelectsItsPushedUnitOnTheBoardAndConfirmsTheBatch";

        private const string OrbitDeclined =
            "ControlGravity_OrbitsAPieceAndItsRepeatIsDeclinedByItsControl";

        private const string PlaceOffLine =
            "Telekinesis_PlacesTheOffLineUnitBesideItsActorThroughBoardClicks";

        private const string EscalatingDiscard =
            "Electroblast_TakesACardFromTheChosenHeroWhoHoldsOneThroughTheUi";

        private const string ReflectAndRepeat =
            "ThunderBoomerang_IsReflectedThenItsRepeatIsDeclinedThroughTheControls";

        private const string PushDistance =
            "KineticBlast_AsksForItsDistanceAsRealControlsAfterItsUnitsAreClicked";

        private const string OrbitTwice =
            "CenterOfMass_RepeatsItsOrbitTwiceThroughItsOwnConfirmControls";

        private const string PlaceAndDecline =
            "MassTelekinesis_PlacesItsPieceAndItsUnboundRepeatIsDeclined";

        private const string UltimateJourney =
            "HighVoltage_IsVisibleOnTheRailAndFiresAfterTheBasicSkillThroughTheUi";

        private const string UltimateDeclined =
            "HighVoltage_DeclinedThroughItsControlDefeatsNobody";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_WASP.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Wasp",
            WaspCardContent.AllDefinitions,
            WaspCardContent.ReturnedToBoxAtSetup,
            WaspActiveEffectContent.Handlers(),
            new[]
            {
                WaspEffectContent.HighVoltageTriggeredEffect(WaspCardContent.ContentRevision)
            },
            new[]
            {
                // The Gold basic. Its "This turn:" pin is the one Active effect in this build whose
                // restriction names its actors rather than barring an activity outright, so it stops
                // an enemy without ever drawing that enemy a control - the derivation records why, and
                // the pin's whole enemy-facing half is owned by the authoritative scenarios below.
                Face(WaspCardContent.MagneticDagger, "Magnetic Dagger", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(AdjacentAttackTarget, DaggerJourney),
                        Prove(AfterAttackTextPause, DaggerJourney),
                        Prove(ActiveEffectReading, DaggerJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[]
                    {
                        "ConfiguredBasicCardTests.MagneticDagger_RunsItsAttackThenPinsTheEnemyUnitsLeftInsideItsRadius",
                        "ConfiguredBasicCardTests.MagneticDaggersEffect_DeclaresBothRepositioningsAgainstBothNamedActors",
                        "WaspVerifiedContentTests.TheSupertypeFollowsPageNine_AndOnlyStaticBarriersPrimaryIsABasicSkill"
                    }),

                // p.20's Handicap card. p.5 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; its composition carries the named deferral
                // Arien's Ceremonial Blade later copied. The guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(WaspCardContent.CracklingDagger, "Crackling Dagger",
                    "p.5 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "its text composition is deferred until the first odd-player-count configuration"),

                Face(WaspCardContent.StaticBarrier, "Static Barrier", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, BarrierJourney),
                        Prove(ActiveEffectReading, BarrierJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "ConfiguredBasicCardTests",
                        "WaspVerifiedContentTests.TheSupertypeFollowsPageNine_AndOnlyStaticBarriersPrimaryIsABasicSkill"
                    }),

                Face(WaspCardContent.Shock, "Shock", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(AdjacentAttackTarget, ShockJourney),
                        Prove(AfterAttackTextPause, ShockJourney),
                        Prove(BoardUnitSelection, ShockJourney),
                        Prove(HandCardSelection, ShockJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[]
                    {
                        "ConfiguredAttackCardTests",
                        "WaspVerifiedContentTests.NoActionCarriesAnEffectRoot_AndEveryPrintedPrimaryNamesWhatOwesItsText"
                    }),

                // The same sentence at a wider printed disk. It introduces no shape Shock did not,
                // which is exactly the case the gate's own "need not duplicate permutations" sentence
                // covers; its own instance is nevertheless driven end to end, because milestone 8.15
                // wanted the icon read off the played card rather than off its sibling.
                Face(WaspCardContent.Electrocute, "Electrocute", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(AdjacentAttackTarget, ElectrocuteJourney),
                        Prove(AfterAttackTextPause, ElectrocuteJourney),
                        Prove(BoardUnitSelection, ElectrocuteJourney),
                        Prove(HandCardSelection, ElectrocuteJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[]
                    {
                        "WaspVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory",
                        "WaspProgressedHandsTests"
                    }),

                Face(WaspCardContent.ChargedBoomerang, "Charged Boomerang", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(RangedAttackTarget, BoomerangAndDeflect),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[]
                    {
                        "ConfiguredAttackCardTests",
                        "WaspVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory"
                    }),

                Face(WaspCardContent.Electroblast, "Electroblast", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(AdjacentAttackTarget, EscalatingDiscard),
                        Prove(AfterAttackTextPause, EscalatingDiscard),
                        Prove(BoardUnitSelection, EscalatingDiscard),
                        Prove(HandCardSelection, EscalatingDiscard),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredAttackCardTests" }),

                Face(WaspCardContent.ThunderBoomerang, "Thunder Boomerang", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(RangedAttackTarget, ReflectAndRepeat),
                        Prove(AfterAttackTextPause, ReflectAndRepeat),
                        Prove(RepeatDecision, ReflectAndRepeat),
                        Prove(ConfirmOrDecline, ReflectAndRepeat),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredAttackCardTests" }),

                Face(WaspCardContent.LiftUp, "Lift Up", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, OrbitAndRepeat),
                        Prove(BoardUnitSelection, OrbitAndRepeat),
                        Prove(BoardSpaceSelection, OrbitAndRepeat),
                        Prove(RepeatDecision, OrbitAndRepeat),
                        Prove(ConfirmOrDecline, OrbitAndRepeat),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredTelekineticSkillTests" }),

                Face(WaspCardContent.ControlGravity, "Control Gravity", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, OrbitDeclined),
                        Prove(BoardUnitSelection, OrbitDeclined),
                        Prove(BoardSpaceSelection, OrbitDeclined),
                        Prove(RepeatDecision, OrbitDeclined),
                        Prove(ConfirmOrDecline, OrbitDeclined),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredTelekineticSkillTests" }),

                // p.18's batch Push at a fixed distance: the units are a decision and the three spaces
                // are not, which is why this row declares no PushDistanceChoice and Kinetic Blast's
                // does. Its discard rider needs a pushed hero stopped by an obstacle, which no engaged
                // staging produces, so the shape it asks for is proved on the chooser Shock opens and
                // the rider itself is owned by ConfiguredKineticSkillTests.
                Face(WaspCardContent.KineticRepulse, "Kinetic Repulse", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, PushBatch),
                        Prove(BoardUnitSelection, PushBatch),
                        Prove(ConfirmOrDecline, PushBatch),
                        Prove(HandCardSelection, ShockJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredKineticSkillTests" }),

                Face(WaspCardContent.CenterOfMass, "Center of Mass", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, OrbitTwice),
                        Prove(BoardUnitSelection, OrbitTwice),
                        Prove(BoardSpaceSelection, OrbitTwice),
                        Prove(RepeatDecision, OrbitTwice),
                        Prove(ConfirmOrDecline, OrbitTwice),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredTelekineticSkillTests" }),

                Face(WaspCardContent.KineticBlast, "Kinetic Blast", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, PushDistance),
                        Prove(BoardUnitSelection, PushDistance),
                        Prove(ConfirmOrDecline, PushDistance),
                        Prove(PushDistanceChoice, PushDistance),
                        Prove(HandCardSelection, ShockJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredKineticSkillTests" }),

                Face(WaspCardContent.StopProjectiles, "Stop Projectiles", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(BlockDefenseOffer, BlockAndRespawn),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredDefenseCardTests" }),

                // Its rider excludes the attacker, and a 1v1 board holds no other enemy hero, so the
                // two shapes the rider asks for are proved on Reflect Projectiles' - the same sentence
                // without the exclusion - and the exclusion itself is pinned authoritatively.
                Face(WaspCardContent.DeflectProjectiles, "Deflect Projectiles", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(BlockDefenseOffer, BoomerangAndDeflect),
                        Prove(DefenseTextApplication, ReflectAndRepeat),
                        Prove(BoardUnitSelection, ReflectAndRepeat),
                        Prove(HandCardSelection, ReflectAndRepeat),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredDefenseCardTests" }),

                Face(WaspCardContent.Telekinesis, "Telekinesis", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, PlaceOffLine),
                        Prove(BoardUnitSelection, PlaceOffLine),
                        Prove(BoardSpaceSelection, PlaceOffLine),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredTelekineticSkillTests" }),

                Face(WaspCardContent.ReflectProjectiles, "Reflect Projectiles", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(BlockDefenseOffer, ReflectAndRepeat),
                        Prove(DefenseTextApplication, ReflectAndRepeat),
                        Prove(BoardUnitSelection, ReflectAndRepeat),
                        Prove(HandCardSelection, ReflectAndRepeat),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredDefenseCardTests" }),

                Face(WaspCardContent.MassTelekinesis, "Mass Telekinesis", new[]
                    {
                        Prove(CommitAndReveal, TrayCommit),
                        Prove(PrimarySkillControl, PlaceAndDecline),
                        Prove(BoardUnitSelection, PlaceAndDecline),
                        Prove(BoardSpaceSelection, PlaceAndDecline),
                        Prove(RepeatDecision, PlaceAndDecline),
                        Prove(ConfirmOrDecline, PlaceAndDecline),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ConfiguredTelekineticSkillTests" }),

                // p.16's Ultimate. It prints no action at all, so it has no commitment row: the page
                // puts it faceup in the hero's area and its ability fires from there.
                Face(WaspCardContent.HighVoltage, "High Voltage", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney),
                        Prove(BoardUnitSelection, UltimateJourney),
                        Prove(HandCardSelection, UltimateJourney),
                        Prove(ConfirmOrDecline, UltimateDeclined)
                    },
                    new[]
                    {
                        "ConfiguredHighVoltageTests",
                        "UltimateContinuousSourceTests",
                        "WaspVerifiedContentTests.HighVoltagePrintsItsRadiusAndNoInitiative_AndDeclaresItsTriggeredAbility"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                Component("wasp-hero-piece", "the official circular Red hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPiecePointerSelection, PointAtWasp),
                        Prove(HeroPieceMovement, Walk),
                        Prove(HeroPieceDefeatAndRespawn, BlockAndRespawn)
                    },
                    new[] { "WaspHeroPieceLifecycleTests" }),

                Component("wasp-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, PauseSurvives),
                        Prove(RetainedPrefixUndo, PauseSurvives),
                        Prove(ReplayReconstruction, PauseSurvives),
                        Prove(RetainedPrefixUndo, DrawerAndRestart),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "WaspHeroPieceLifecycleTests" }),

                // Milestone 8.16c's live regression lives in Wasp's own engaged fixture because her
                // staged mirror is the match that reaches p.8's blocked Spawn-point pause, and the
                // orphan guard below accounts for every journey in those fixtures. Its subject is the
                // board rather than any printed face, so it is named here rather than pretending to
                // belong to a card - and what it proves, a board-valued answer given by camera ray on
                // a lit space, is a shape Wasp's Skills ask for too.
                Component("wasp-staged-board-transitions",
                    "p.8's blocked Spawn-point Place on the staged board, answered on the map",
                    new[] { Prove(BoardSpaceSelection, BlockedLanePush) },
                    new[] { "LanePushTransitionTests" }),

                // p.16's item is gained from the sibling p.15's Level Up did not take, and it is drawn
                // on the hero rail once gained. No journey reaches it, for the reason milestone 8.20a
                // recorded against Arien's identical row: the item track is derived progression state,
                // `DEV-004` composes card locations only, and no scene content stages a position where
                // a Level Up is offered - so a staged session begins with an empty item track and a
                // live one cannot earn its way to a Level Up inside a journey's budget. This is a debt
                // rather than a gap: the row names it, the guard below holds it to Wasp actually
                // printing item icons, and it is the same one row both heroes owe.
                AbsentComponent("wasp-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; the printed pairs and their item " +
                    "icons are pinned by WaspVerifiedContentTests." +
                    "TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator"),

                // Wasp prints no token and no marker - ledger question 14's answer, which made the
                // blank dashboard back missing art rather than missing rules. The row records the
                // absence and WaspPrintsNoTokenOrMarker_AsTheMatrixRecords checks it against the
                // content, so the hero who does print one cannot inherit this sentence.
                AbsentComponent("wasp-tokens-and-markers", "hero-owned tokens and markers",
                    "Wasp's nineteen faces place no token and give no marker; her board-object text " +
                    "moves, pushes and places pieces that are already on the map")
            });

        /// <summary>
        /// The capability identities that put a token or a marker on the board. A hero whose content
        /// names one of these has a component the matrix must have a row for.
        /// </summary>
        private static readonly string[] TokenAndMarkerCapabilities =
        {
            "synthetic-place-token",
            "synthetic-reclaim-token",
            "synthetic-give-marker",
            "synthetic-return-marker"
        };

        [Test]
        public void EveryWaspFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesWaspsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(WaspPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(typeof(WaspPlayerPathGateTests).Assembly);

        [Test]
        public void EveryWaspJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(WaspUiJourneyPlayModeTests),
                typeof(WaspEngagedUiJourneyPlayModeTests),
                typeof(WaspProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row should
        /// go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndWaspActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "wasp-items",
                WaspCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void WaspPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Wasp's nineteen faces place no token and give no marker");

        /// <summary>
        /// Every row's authoritative scenarios name a fixture the Edit Mode assembly actually has.
        /// The gate asks each row to "link its source-backed authoritative scenarios", and a link that
        /// resolves to nothing is the shape of documentation this project does not keep.
        /// </summary>
        [Test]
        public void EveryRowsAuthoritativeScenarios_ResolveInTheEditModeAssembly()
        {
            var editMode = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly =>
                    assembly.GetName().Name == "GuardsDigital.EditModeTests");
            Assert.That(editMode, Is.Not.Null,
                "The Edit Mode test assembly is not loaded, so this gate cannot check that its " +
                "matrix links resolve. These tests run in the Editor, where it always is.");

            var dangling = CreateMatrix().Rows
                .SelectMany(row => row.AuthoritativeScenarios.Select(scenario => (row, scenario)))
                .Where(pair => !Resolves(editMode, pair.scenario))
                .Select(pair => $"{pair.row} links '{pair.scenario}'")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.That(dangling, Is.Empty,
                "The matrix links authoritative scenarios that do not exist:" +
                Environment.NewLine + string.Join(Environment.NewLine, dangling));
        }

        private static bool Resolves(System.Reflection.Assembly editMode, string scenario)
        {
            var separator = scenario.IndexOf('.');
            var fixtureName = separator < 0 ? scenario : scenario.Substring(0, separator);
            var fixture = editMode.GetTypes()
                .FirstOrDefault(type => type.Name == fixtureName);
            if (fixture == null)
            {
                return false;
            }

            return separator < 0 ||
                fixture.GetMethod(scenario.Substring(separator + 1)) != null;
        }
    }
}
