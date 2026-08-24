using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Arien the Tidemaster's half of the permanent hero player-path completion gate. Milestone 8.20a.
    ///
    /// <para><b>What this fixture is.</b> `Testing/POLICY.md` requires that a hero first made
    /// configured and playable maintains "a mechanical completeness matrix with one row for every
    /// verified card face and hero-owned component", where each row "identifies the configured Play
    /// Mode journey or journeys that expose every behaviorally distinct player interaction shape the
    /// row introduces", and that "a guard fails when a face/component or interaction shape is
    /// absent". The table below is that matrix and the cases below are that guard.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="ArienCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and
    /// <see cref="MatrixRowsDeclareExactlyTheShapesArienPrintedContentIntroduces"/> compares the two.
    /// A row cannot claim less than its face asks for, cannot claim more, and cannot name a shape
    /// without naming the journey where a player performs it, because a row's shapes <em>are</em> its
    /// evidence. Adding a face, or giving one a repeat it did not print, fails here on the next run
    /// with nobody having chosen to look.</para>
    ///
    /// <para><b>Shared journeys are deliberate.</b> Five faces print "Before the attack:" and only the
    /// two that differ in what the pause then asks for have their own journey. The gate says so itself -
    /// "Play Mode need not duplicate permutations that change no input, presentation, or transition
    /// shape" - and the face-specific half of the evidence is the authoritative scenario column, which
    /// is one row per face by construction.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class ArienPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names. Declared as constants so a row and the method it names cannot drift apart
        // silently in one direction only - a rename that misses one end fails
        // MatrixNamesOnlyJourneysThatExist rather than quietly proving nothing.
        // -----------------------------------------------------------------------------------------

        private const string Spawn =
            "HeroPiece_StandsAtTheBlueSpawnAsArienAndIsPointedAtByABoardClick";

        private const string SpellBreakJourney =
            "SpellBreak_IsPlayedByItsControlAndStopsTheEnemysSilverSkillOnScreen";

        private const string Walk =
            "Arien_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace";

        private const string DefendByFace =
            "AspiringDuelist_IsOfferedAsAPrimaryDefenseValueAndDiscardedByItsOwnFace";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesAriensCommitmentAndRestartsHimOntoHisSpawn";

        private const string NobleBladeJourney =
            "NobleBlade_MovesAnotherUnitBesideItsTargetBeforeTheAttackThroughBoardClicks";

        private const string PauseSurvives =
            "NobleBladesBeforeAttackPause_IsAnsweredAgainAfterARestoreAndAfterAnUndo";

        private const string BehindTheTarget =
            "DangerousCurrent_MakesTheHeroBehindItsTargetDiscardOnItsOwnCardFace";

        private const string LeapJourney =
            "LiquidLeap_PlacesArienOnAClickedSpaceAndLightsNoSpawnPoint";

        private const string DefeatAndRespawn =
            "Arien_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick";

        private const string PushAfterRangedAttack =
            "RogueWave_PushesAnAdjacentEnemyAfterItsRangedAttackByItsOwnControls";

        private const string RepeatOnADifferentUnit =
            "ViolentTorrent_RepeatsItsBeforeAttackTextOnADifferentUnitByItsRepeatControl";

        private const string MovementPrimary =
            "SlipperyGround_AppliesItsTextBeforeMovingAndStopsTheEnemysFastTravel";

        private const string ThisTurnDuel =
            "ExpertDuelist_DefendsAndItsThisTurnImmunityIsReadOnTheTray";

        private const string ThisRoundDuel =
            "MasterDuelist_ImmunityIsStillReadOnALaterTurnOfTheSameRound";

        private const string SwapJourney =
            "ArcaneWhirlpool_SwapsWithTheClickedEnemyMinionThroughTheBoard";

        private const string SwapAndRepeat =
            "EbbAndFlow_SwapsAndItsAdjacentRepeatIsDeclinedByItsOwnControl";

        private const string UltimateJourney =
            "LivingTsunami_IsDrawnOnTheRailAndOffersItsOneSpaceStepBeforeAnAttack";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_ARIEN.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Arien the Tidemaster",
            ArienCardContent.AllDefinitions,
            ArienCardContent.ReturnedToBoxAtSetup,
            ArienActiveEffectContent.Handlers(),
            new[]
            {
                ArienEffectContent.LivingTsunamiTriggeredEffect(ArienCardContent.ContentRevision)
            },
            new[]
            {
                Face(ArienCardContent.NobleBlade, "Noble Blade", new[]
                    {
                        Prove(CommitAndReveal, Walk),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace),
                        Prove(AdjacentAttackTarget, NobleBladeJourney),
                        Prove(BeforeAttackTextPause, NobleBladeJourney),
                        Prove(ConfirmOrDecline, NobleBladeJourney),
                        Prove(BoardUnitSelection, NobleBladeJourney),
                        Prove(BoardSpaceSelection, NobleBladeJourney)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints",
                        "ArienVerifiedContentTests.EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; its composition carries the same named
                // deferral Wasp's Crackling Dagger does. The guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(ArienCardContent.CeremonialBlade, "Ceremonial Blade",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "its text composition is deferred until the first odd-player-count configuration"),

                Face(ArienCardContent.SpellBreak, "Spell Break", new[]
                    {
                        Prove(CommitAndReveal, SpellBreakJourney),
                        Prove(PrimarySkillControl, SpellBreakJourney),
                        Prove(ActiveEffectReading, SpellBreakJourney),
                        Prove(OpponentRestrictionReading, SpellBreakJourney),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay",
                        "RosterPersistentSourceTests"
                    }),

                Face(ArienCardContent.DangerousCurrent, "Dangerous Current", new[]
                    {
                        Prove(CommitAndReveal, BehindTheTarget),
                        Prove(AdjacentAttackTarget, BehindTheTarget),
                        Prove(BeforeAttackTextPause, BehindTheTarget),
                        Prove(BoardUnitSelection, BehindTheTarget),
                        Prove(HandCardSelection, BehindTheTarget),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints",
                        "RosterTargetGeometryCapabilityTests"
                    }),

                // The same sentence over three spaces instead of two. It introduces no shape Dangerous
                // Current did not, which is exactly the case the gate's own "need not duplicate
                // permutations" sentence covers; the distance is pinned at the authoritative layer.
                Face(ArienCardContent.RagingStream, "Raging Stream", new[]
                    {
                        Prove(CommitAndReveal, BehindTheTarget),
                        Prove(AdjacentAttackTarget, BehindTheTarget),
                        Prove(BeforeAttackTextPause, BehindTheTarget),
                        Prove(BoardUnitSelection, BehindTheTarget),
                        Prove(HandCardSelection, BehindTheTarget),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory",
                        "RosterTargetGeometryCapabilityTests"
                    }),

                Face(ArienCardContent.RogueWave, "Rogue Wave", new[]
                    {
                        Prove(CommitAndReveal, PushAfterRangedAttack),
                        Prove(RangedAttackTarget, PushAfterRangedAttack),
                        Prove(AfterAttackTextPause, PushAfterRangedAttack),
                        Prove(ConfirmOrDecline, PushAfterRangedAttack),
                        Prove(BoardUnitSelection, PushAfterRangedAttack),
                        Prove(PushDistanceChoice, PushAfterRangedAttack),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints" }),

                Face(ArienCardContent.ViolentTorrent, "Violent Torrent", new[]
                    {
                        Prove(CommitAndReveal, RepeatOnADifferentUnit),
                        Prove(AdjacentAttackTarget, RepeatOnADifferentUnit),
                        Prove(BeforeAttackTextPause, RepeatOnADifferentUnit),
                        Prove(BoardUnitSelection, RepeatOnADifferentUnit),
                        Prove(HandCardSelection, RepeatOnADifferentUnit),
                        Prove(RepeatDecision, RepeatOnADifferentUnit),
                        Prove(ConfirmOrDecline, RepeatOnADifferentUnit),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints" }),

                Face(ArienCardContent.TidalBlast, "Tidal Blast", new[]
                    {
                        Prove(CommitAndReveal, PushAfterRangedAttack),
                        Prove(RangedAttackTarget, PushAfterRangedAttack),
                        Prove(AfterAttackTextPause, PushAfterRangedAttack),
                        Prove(ConfirmOrDecline, PushAfterRangedAttack),
                        Prove(BoardUnitSelection, PushAfterRangedAttack),
                        Prove(PushDistanceChoice, PushAfterRangedAttack),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory" }),

                Face(ArienCardContent.AspiringDuelist, "Aspiring Duelist", new[]
                    {
                        Prove(CommitAndReveal, DefendByFace),
                        Prove(PrimaryDefenseOffer, DefendByFace),
                        Prove(MinionDefenseSuppressionReading, DefendByFace),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ArienVerifiedContentTests.TheThreeDuelistCards_ArePrimaryDefenseValuesThatSuppressMinionModifiers" }),

                Face(ArienCardContent.ExpertDuelist, "Expert Duelist", new[]
                    {
                        Prove(CommitAndReveal, ThisTurnDuel),
                        Prove(PrimaryDefenseOffer, ThisTurnDuel),
                        Prove(MinionDefenseSuppressionReading, ThisTurnDuel),
                        Prove(DefenseTextApplication, ThisTurnDuel),
                        Prove(ActiveEffectReading, ThisTurnDuel),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ArienVerifiedContentTests.TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay" }),

                Face(ArienCardContent.SlipperyGround, "Slippery Ground", new[]
                    {
                        Prove(CommitAndReveal, MovementPrimary),
                        Prove(PrimaryMovementDestination, MovementPrimary),
                        Prove(BeforeMovementTextPause, MovementPrimary),
                        Prove(ActiveEffectReading, MovementPrimary),
                        Prove(OpponentRestrictionReading, MovementPrimary),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay",
                        "RosterStateOperationCapabilityTests"
                    }),

                Face(ArienCardContent.MasterDuelist, "Master Duelist", new[]
                    {
                        Prove(CommitAndReveal, ThisRoundDuel),
                        Prove(PrimaryDefenseOffer, ThisRoundDuel),
                        Prove(MinionDefenseSuppressionReading, ThisRoundDuel),
                        Prove(DefenseTextApplication, ThisRoundDuel),
                        Prove(ActiveEffectReading, ThisRoundDuel),
                        Prove(RoundLongActiveEffectReading, ThisRoundDuel),
                        Prove(SecondaryMovementDestination, Walk)
                    },
                    new[] { "ArienVerifiedContentTests.TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay" }),

                Face(ArienCardContent.Deluge, "Deluge", new[]
                    {
                        Prove(CommitAndReveal, MovementPrimary),
                        Prove(PrimaryMovementDestination, MovementPrimary),
                        Prove(BeforeMovementTextPause, MovementPrimary),
                        Prove(ActiveEffectReading, MovementPrimary),
                        Prove(OpponentRestrictionReading, MovementPrimary),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay" }),

                Face(ArienCardContent.LiquidLeap, "Liquid Leap", new[]
                    {
                        Prove(CommitAndReveal, LeapJourney),
                        Prove(PrimarySkillControl, LeapJourney),
                        Prove(BoardSpaceSelection, LeapJourney),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog" }),

                Face(ArienCardContent.MagicalCurrent, "Magical Current", new[]
                    {
                        Prove(CommitAndReveal, LeapJourney),
                        Prove(PrimarySkillControl, LeapJourney),
                        Prove(BoardSpaceSelection, LeapJourney),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory" }),

                Face(ArienCardContent.ArcaneWhirlpool, "Arcane Whirlpool", new[]
                    {
                        Prove(CommitAndReveal, SwapJourney),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog" }),

                Face(ArienCardContent.StrangerTide, "Stranger Tide", new[]
                    {
                        Prove(CommitAndReveal, LeapJourney),
                        Prove(PrimarySkillControl, LeapJourney),
                        Prove(BoardSpaceSelection, LeapJourney),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory" }),

                Face(ArienCardContent.EbbAndFlow, "Ebb and Flow", new[]
                    {
                        Prove(CommitAndReveal, SwapAndRepeat),
                        Prove(PrimarySkillControl, SwapAndRepeat),
                        Prove(BoardUnitSelection, SwapAndRepeat),
                        Prove(RepeatDecision, SwapAndRepeat),
                        Prove(ConfirmOrDecline, SwapAndRepeat),
                        Prove(SecondaryMovementDestination, Walk),
                        Prove(SecondaryDefenseOffer, DefendByFace)
                    },
                    new[] { "ArienVerifiedContentTests.EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog" }),

                Face(ArienCardContent.LivingTsunami, "Living Tsunami", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney),
                        Prove(ConfirmOrDecline, UltimateJourney),
                        Prove(BoardSpaceSelection, UltimateJourney)
                    },
                    new[]
                    {
                        "ArienVerifiedContentTests.LivingTsunamiPrintsNoIconAndNoInitiative_AndDeclaresItsBeforeAttackAbility",
                        "UltimateContinuousSourceTests"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                Component("arien-hero-piece", "the official circular Blue hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPieceMovement, Walk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn)
                    },
                    new[] { "ArienHeroPieceLifecycleTests" }),

                Component("arien-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, PauseSurvives),
                        Prove(RetainedPrefixUndo, PauseSurvives),
                        Prove(ReplayReconstruction, PauseSurvives),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "ArienHeroPieceLifecycleTests" }),

                // p.16's item is gained from the sibling p.15's Level Up did not take, and it is drawn
                // on the hero rail once gained. No journey reaches it: the item track is derived
                // progression state, `DEV-004` composes card locations only, and no scene content
                // stages a position where a Level Up is offered - so a staged session begins with an
                // empty item track and a live one cannot earn its way to a Level Up inside a
                // journey's budget. This is a debt rather than a gap: the row names it, the guard
                // below holds it to Arien actually printing item icons, and milestone 8.20a's handoff
                // records it as the one gate row Arien cannot prove through the player path today.
                AbsentComponent("arien-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; the printed pairs and their item " +
                    "icons are pinned by ArienVerifiedContentTests." +
                    "TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator"),

                // Arien prints no token and no marker. The row records the absence and
                // ArienPrintsNoTokenOrMarker_AsTheMatrixRecords checks it against the content, so the
                // hero who does print one cannot inherit this sentence.
                AbsentComponent("arien-tokens-and-markers", "hero-owned tokens and markers",
                    "Arien's nineteen faces place no token and give no marker; his board-object text " +
                    "is Place, Swap and Push of units that are already on the map")
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
        public void EveryArienFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesArienPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(ArienPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(typeof(ArienPlayerPathGateTests).Assembly);

        [Test]
        public void EveryArienJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(ArienUiJourneyPlayModeTests),
                typeof(ArienEngagedUiJourneyPlayModeTests),
                typeof(ArienProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if
        /// a hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndArienActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "arien-items",
                ArienCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void ArienPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Arien's nineteen faces place no token and give no marker");

        /// <summary>
        /// Every row's authoritative scenarios name a fixture the Edit Mode assembly actually has.
        /// The gate asks each row to "link its source-backed authoritative scenarios", and a link
        /// that resolves to nothing is the shape of documentation this project does not keep.
        ///
        /// <para>The Edit Mode assembly is Editor-only and these tests run in the Editor, so it is
        /// loaded whenever this case runs. Rows name a fixture, optionally with a case after a dot;
        /// both halves are resolved.</para>
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
