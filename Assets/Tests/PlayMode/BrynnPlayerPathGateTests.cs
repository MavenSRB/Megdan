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
    /// The permanent hero player-path completion gate, for Brynn the Seeker. Milestone 8.69h, and the
    /// TWENTIETH hero to take it.
    ///
    /// <para><b>Nothing below is a checklist.</b> Every row's shape set is DERIVED from his own printed
    /// content by <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> and compared against the row
    /// exactly, so a row that claims less has a shape nothing proves and a row that claims more is a
    /// coverage claim the content does not support. A shape cannot be declared without naming the
    /// [UnityTest] that drives it, and a journey in his fixtures that no row names fails too.</para>
    ///
    /// <para><b>He introduces NO new interaction shape</b>, which milestone 8.69g predicted while
    /// dealing him and this gate is the first to rest a table on. His three own contracts are either a
    /// question nobody is asked - <c>brynn-enemy-hero-in-radius-adjacent-to-obstacles@1</c> and
    /// <c>brynn-attack-target-was-adjacent-to-obstacles@1</c> are p.19 PRESENCE checks whose whole
    /// output is one Boolean a <c>Conditional</c> branches on - or the board click this table has
    /// carried since milestone 8.15: <c>brynn-tracking-swap-subject@1</c> joins two reaches into one
    /// selection, and a union narrows the candidates rather than the input. He is the second hero in a
    /// row to add none, after Rowenna's.</para>
    ///
    /// <para><b>Four derivation readings are worth stating outright, because each one keeps a row
    /// HONEST rather than generous.</b></para>
    /// <list type="number">
    /// <item><description><b>Over the Top declares only the rail.</b> His Purple prints no action, so
    /// p.7 never commits it and nothing is revealed; its permanent ability is a
    /// <c>RuleModificationKind.CountsAsAdjacentToThreeOrMoreObstacles</c> overlay that changes what
    /// OTHER faces of his may name, and it carries no triggered effect at all - so it introduces
    /// <see cref="UltimateRailVisibility"/> and nothing else. The widening itself is a fact about the
    /// rows that read the predicate, and it is journeyed there.</description></item>
    /// <item><description><b>The three Attack faces that print the plus declare no card-text pause.</b>
    /// High Ground, Elevated Ambush and Peak Precision's "+2" is a
    /// <c>RosterValueContributionDefinition</c> computed when p.14 totals the Attack, not a body that
    /// stops the page - so only Peak Precision, whose printed retrieval really is an after-the-attack
    /// step, declares <see cref="AfterAttackTextPause"/>. A row claiming a pause for High Ground would
    /// name a prompt no player is shown.</description></item>
    /// <item><description><b>Split Throw declares its repeat and no confirm.</b> BR-Q15b composes it
    /// <c>IfAble</c> rather than <c>May</c>, so the second instance simply runs; Split Attack's
    /// <c>May</c> is what draws the confirm-or-decline beside its
    /// <see cref="RepeatDecision"/>.</description></item>
    /// <item><description><b>Decoy declares no secondary Movement.</b> BR-06: it prints a Defense 2 and
    /// no Movement ribbon whatsoever, and a missing icon is absent rather than zero - so it is the one
    /// dealt face of his whose row starts from <see cref="NoBoot"/>.</description></item>
    /// </list>
    /// </summary>
    public sealed class BrynnPlayerPathGateTests
    {
        // His journeys, named once so a rename fails at compile time rather than in the guard.
        private const string Spawn = nameof(
            BrynnUiJourneyPlayModeTests.HisPieceSpawnsAsBrynnAndHisTrayCommitsAndRevealsACard);
        private const string Secondary = nameof(
            BrynnUiJourneyPlayModeTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string Gold = nameof(BrynnUiJourneyPlayModeTests
            .FamiliarGroundTakesEitherPrintedBulletAndIsGreyedWhenNeitherNamesAnybody);
        private const string WaterRing = nameof(
            BrynnUiJourneyPlayModeTests.TheWaterRingAloneBoxesInAHeroStandingOnTheMapsEdge);
        private const string HighGround = nameof(BrynnUiJourneyPlayModeTests
            .HighGroundAddsItsPrintedTwoBesideThreeObstaclesAndNothingInTheOpen);
        private const string Decoy = nameof(
            BrynnUiJourneyPlayModeTests.DecoyMovesTwoEnemyUnitsOneSpaceEachOrMovesNobodyAtAll);
        private const string TreadLightly = nameof(BrynnUiJourneyPlayModeTests
            .TreadLightlySwapsWithAnAdjacentUnitOrWithABoxedInHeroInRadius);
        private const string BearTrap = nameof(BrynnUiJourneyPlayModeTests
            .BearTrapTakesACardFromTheHeroBesideHimOrFromTheOneBoxedInAtRange);
        private const string Footprint = nameof(
            BrynnUiJourneyPlayModeTests.EveryBrynnFaceUsesTheStandardFootprint);

        private const string ElevatedAmbush = nameof(BrynnEngagedUiJourneyPlayModeTests
            .ElevatedAmbushAddsItsPrintedTwoBesideThreeObstacles);
        private const string SplitAttack = nameof(BrynnEngagedUiJourneyPlayModeTests
            .SplitAttackRepeatsOnADifferentUnitBesideHimAndIsNotOfferedInTheOpen);
        private const string CoverTracks = nameof(BrynnEngagedUiJourneyPlayModeTests
            .CoverTracksSwapsAndThenMovesHimOneSpaceOrLeavesHimWhereItPutHim);
        private const string MountainGuide = nameof(BrynnEngagedUiJourneyPlayModeTests
            .MountainGuideMovesASecondFriendlyUnitOnlyWhenAHeroInRadiusIsBoxedIn);
        private const string LogTrap = nameof(BrynnEngagedUiJourneyPlayModeTests
            .LogTrapTakesACardFromTheChosenHeroAndAsksNothingOfAnEmptyHand);
        private const string TrueGrit = nameof(BrynnEngagedUiJourneyPlayModeTests
            .TrueGritRetrievesOnlyAnAttackCardAndMovesHimOnlyWhenAHeroInRadiusIsBoxedIn);

        private const string Items = nameof(
            BrynnProgressedUiJourneyPlayModeTests.LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem);
        private const string PeakPrecision = nameof(BrynnProgressedUiJourneyPlayModeTests
            .PeakPrecisionAddsItsTwoAndTakesItselfBackIntoHisHand);
        private const string SplitThrow = nameof(BrynnProgressedUiJourneyPlayModeTests
            .SplitThrowRepeatsOnADifferentTargetUnitWithoutAskingHim);
        private const string HideTraces = nameof(BrynnProgressedUiJourneyPlayModeTests
            .HideTracesSwapsWithAHeroInRadiusAndThenMovesHimUpToTwo);
        private const string ExpeditionLeader = nameof(BrynnProgressedUiJourneyPlayModeTests
            .ExpeditionLeaderMovesTheUnitBesideHimAndADifferentOneInRadius);
        private const string DeadfallTrap = nameof(BrynnProgressedUiJourneyPlayModeTests
            .DeadfallTrapDefeatsTheHeroWhoCannotDiscardAndPaysHimThePageThirteenCoins);
        private const string DieHard = nameof(BrynnProgressedUiJourneyPlayModeTests
            .DieHardDeclinesItsRetrievalAndMovesHimUpToFour);
        private const string Ultimate = nameof(BrynnProgressedUiJourneyPlayModeTests
            .OverTheTopWidensHisOwnClauseOnABoardThatRefusedItOneTierEarlier);
        private const string Lifecycle = nameof(BrynnProgressedUiJourneyPlayModeTests
            .HisDefeatedPieceRespawnsAndRestartRebuildsTheSession);
        private const string Continuity = nameof(BrynnProgressedUiJourneyPlayModeTests
            .HisOwnDecoyPauseUndoesAndTheReplayReproducesTheExclusion);
        private const string Restore = nameof(
            BrynnProgressedUiJourneyPlayModeTests.HisOwnTrapPauseSurvivesASnapshotRestore);

        /// <summary>
        /// THE USER'S ONE NAMED REQUIREMENT FOR THIS HERO - "he needs to be correctly implemented in
        /// new hero pick screen" - proved on the real selection screen at the rendered tier, which is a
        /// different scene from his match journeys and therefore a journey in the lobby's own fixture.
        /// His piece row names it beside the spawn journey: being the twentieth PICK and standing at the
        /// configured spawn AS Brynn are two halves of one identity.
        /// </summary>
        private const string Picker = nameof(
            PrivateLobbyPlayModeTests.BrynnIsFoundByHisThreeStarsAndEachOfHisThreeRoles);

        /// <summary>
        /// Seventeen of his nineteen faces print p.10's secondary Movement boot AND p.14's secondary
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
        /// BR-06: Decoy prints a Defense 2 and NO Movement ribbon at all. A missing icon is absent,
        /// never zero, and it is the only face of his the split touches.
        /// </summary>
        private static ShapeEvidence[] NoBoot(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        private static readonly string[] Obstacles =
            { "BrynnObstacleAdjacencyTests", "BrynnPrintedSetTests" };
        private static readonly string[] GoldFace =
            { "BrynnObstacleAdjacencyTests", "BrynnDealingTests", "BrynnPrintedSetTests" };
        private static readonly string[] Splits =
            { "BrynnSplitAttackTests", "BrynnPrintedSetTests" };
        private static readonly string[] Swaps =
            { "BrynnTrackingSwapTests", "BrynnPrintedSetTests" };
        private static readonly string[] DecoyAndGuides =
            { "BrynnDecoyAndGuidesTests", "BrynnPrintedSetTests" };
        private static readonly string[] TrapsAndGrit =
            { "BrynnTrapAndGritTests", "BrynnPrintedSetTests" };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Brynn",
            BrynnCardContent.AllDefinitions,
            BrynnCardContent.ReturnedToBoxAtSetup,

            // His registered handlers, handed in because the matrix resolves a capability to an Active
            // effect through the handler that creates it. He raises none: nothing of his is a p.17
            // `This turn:` or `This round:` reading, which is why no row of his declares one.
            BrynnComponentContent.Handlers(),

            // And the triggered effects the build's catalog holds for him, which is none: Over the Top
            // is a PERMANENT ability rather than a trigger, so there is no effect for the derivation to
            // walk and no `UltimateTriggerPause` on his Purple's row.
            Array.Empty<GuardsDigital.Core.Effects.EffectDefinition>(),
            new[]
            {
                // ---------------------------------------------------------------------------------
                // The two basics
                // ---------------------------------------------------------------------------------

                // His Gold is p.10's modal Attack at the printed Range 3, and the only face of his
                // whose card text runs before p.14 checks the attack.
                Face(
                    BrynnCardContent.FamiliarGround, "Familiar Ground",
                    Ordinary(Gold, RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice)
                        .Concat(new[] { Prove(RangedAttackTarget, WaterRing) })
                        .ToArray(),
                    GoldFace),

                Face(
                    BrynnCardContent.Decoy, "Decoy",
                    NoBoot(
                        Decoy,
                        PrimarySkillControl, ConfirmOrDecline, ModalBulletChoice,
                        BoardUnitSelection, BoardSpaceSelection),
                    DecoyAndGuides),

                // ---------------------------------------------------------------------------------
                // The five Reds
                // ---------------------------------------------------------------------------------

                Face(
                    BrynnCardContent.HighGround, "High Ground",
                    Ordinary(HighGround, AdjacentAttackTarget),
                    Obstacles),
                Face(
                    BrynnCardContent.ElevatedAmbush, "Elevated Ambush",
                    Ordinary(ElevatedAmbush, AdjacentAttackTarget),
                    Obstacles),

                Face(
                    BrynnCardContent.SplitAttack, "Split Attack",
                    Ordinary(
                        SplitAttack,
                        RangedAttackTarget, BeforeAttackTextPause, ConfirmOrDecline, RepeatDecision),
                    Splits),
                Face(
                    BrynnCardContent.SplitThrow, "Split Throw",
                    Ordinary(
                        SplitThrow, RangedAttackTarget, BeforeAttackTextPause, RepeatDecision),
                    Splits),

                Face(
                    BrynnCardContent.PeakPrecision, "Peak Precision",
                    Ordinary(
                        PeakPrecision,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline,
                        HandCardSelection),
                    Splits),

                // ---------------------------------------------------------------------------------
                // The five Blues: three tracking swaps and two guides
                // ---------------------------------------------------------------------------------

                Face(
                    BrynnCardContent.TreadLightly, "Tread Lightly",
                    Ordinary(TreadLightly, PrimarySkillControl, BoardUnitSelection),
                    Swaps),
                Face(
                    BrynnCardContent.CoverTracks, "Cover Tracks",
                    Ordinary(
                        CoverTracks,
                        PrimarySkillControl, BoardUnitSelection, ConfirmOrDecline,
                        BoardSpaceSelection),
                    Swaps),
                Face(
                    BrynnCardContent.HideTraces, "Hide Traces",
                    Ordinary(
                        HideTraces, PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection),
                    Swaps),

                Face(
                    BrynnCardContent.MountainGuide, "Mountain Guide",
                    Ordinary(
                        MountainGuide,
                        PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection,
                        BoardSpaceSelection),
                    DecoyAndGuides),
                Face(
                    BrynnCardContent.ExpeditionLeader, "Expedition Leader",
                    Ordinary(
                        ExpeditionLeader,
                        PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection,
                        BoardSpaceSelection),
                    DecoyAndGuides),

                // ---------------------------------------------------------------------------------
                // The five Greens: three traps and two grits
                // ---------------------------------------------------------------------------------

                Face(
                    BrynnCardContent.BearTrap, "Bear Trap",
                    Ordinary(
                        BearTrap,
                        PrimarySkillControl, ModalBulletChoice, BoardUnitSelection,
                        HandCardSelection),
                    TrapsAndGrit),
                Face(
                    BrynnCardContent.LogTrap, "Log Trap",
                    Ordinary(
                        LogTrap,
                        PrimarySkillControl, ModalBulletChoice, BoardUnitSelection,
                        HandCardSelection),
                    TrapsAndGrit),
                Face(
                    BrynnCardContent.DeadfallTrap, "Deadfall Trap",
                    Ordinary(
                        DeadfallTrap,
                        PrimarySkillControl, ModalBulletChoice, BoardUnitSelection,
                        HandCardSelection),
                    TrapsAndGrit),

                Face(
                    BrynnCardContent.TrueGrit, "True Grit",
                    Ordinary(
                        TrueGrit,
                        PrimarySkillControl, ConfirmOrDecline, HandCardSelection,
                        BoardSpaceSelection),
                    TrapsAndGrit),
                Face(
                    BrynnCardContent.DieHard, "Die Hard",
                    Ordinary(
                        DieHard,
                        PrimarySkillControl, ConfirmOrDecline, HandCardSelection,
                        BoardSpaceSelection),
                    TrapsAndGrit),

                // ---------------------------------------------------------------------------------
                // p.16's Ultimate, and p.20's Handicap
                // ---------------------------------------------------------------------------------

                // Over the Top prints no action at all, so it is committed to no turn slot and
                // reveals nothing, and its permanent ability carries no trigger to pause on. What it
                // introduces is the rail a player reads it on; what it DOES is read on the rows of
                // every face that asks the predicate, and the Ultimate journey drives one of those on
                // a board that refused it one tier earlier.
                Face(
                    BrynnCardContent.OverTheTop, "Over the Top",
                    new[] { Prove(UltimateRailVisibility, Ultimate) },
                    new[] { "BrynnObstacleAdjacencyTests", "BrynnDealingTests" }),

                UnreachableFace(
                    BrynnCardContent.HomeTurf, "Home Turf",
                    "Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player " +
                    "format. The 2026-08-21 user decision named that trigger for Wasp's Crackling " +
                    "Dagger and Arien's, Widget's, Swift's, Bain's, Rowenna's and Min's boxed faces " +
                    "carry the same string."),

                // ---------------------------------------------------------------------------------
                // His own components
                // ---------------------------------------------------------------------------------

                Component(
                    "brynn-piece", "Hero piece",
                    new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPieceSpawnAndIdentity, Picker),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, Secondary),
                        Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                        Prove(PresentationRebuild, Lifecycle)
                    },
                    new[] { "BrynnDealingTests" }),

                Component(
                    "brynn-session", "Session continuity",
                    new[]
                    {
                        Prove(SnapshotRestoreContinuation, Restore),
                        Prove(RetainedPrefixUndo, Continuity),
                        Prove(ReplayReconstruction, Continuity),
                        Prove(RestartReconstruction, Lifecycle)
                    },
                    new[] { "BrynnSplitAttackTests", "BrynnTrapAndGritTests" }),

                Component(
                    "brynn-footprint", "Complete faces",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "BrynnPrintedSetTests" }),

                Component(
                    "brynn-items", "Earned items",
                    new[] { Prove(ItemVisibilityAndUse, Items) },
                    new[] { "BrynnPrintedSetTests" }),

                AbsentComponent(
                    "brynn-markers", "Hero-owned markers",
                    "No Brynn face gives, transfers or returns a rulebook p.18 marker. His whole " +
                    "set is positional: what his cards read is where the pieces already on the " +
                    "board are standing, and what they change is where they stand."),

                AbsentComponent(
                    "brynn-tokens", "Placed tokens",
                    "He places nothing. Milestone 8.69's inventory found no token and no marker " +
                    "named anywhere in his nineteen faces, his profile or the Tabletopia manifest - " +
                    "his standee is the only piece of his in the box, and his own body is one of " +
                    "the obstacles his cards count.")
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
                typeof(BrynnPlayerPathGateTests).Assembly);

        [Test]
        public void EveryNamedJourneyExists() =>
            Matrix().AssertEveryNamedJourneyExists(typeof(BrynnPlayerPathGateTests).Assembly);

        [Test]
        public void EveryJourneyInHisFixturesIsNamedByARow() =>
            Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(BrynnUiJourneyPlayModeTests),
                typeof(BrynnEngagedUiJourneyPlayModeTests),
                typeof(BrynnProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryRequiredComponentShapeIsDeclared() =>
            Matrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void HisMarkerAndTokenAbsenceMatchesHisContent() =>
            Matrix().AssertNoContentNamesAnyCapability(
                new[]
                {
                    "bain-give-bounty-marker", "bain-transfer-bounty-marker",
                    "bain-return-bounty-marker", "tigerclaw-give-poison-marker-1",
                    "tigerclaw-give-poison-marker-2", "give-marker",
                    "synthetic-place", "synthetic-place-in-range", "synthetic-remove"
                },
                "Brynn owns no marker and places no token.");

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
        public void HisTableCoversNineteenFacesAndSixComponents()
        {
            var matrix = Matrix();
            Assert.That(matrix.Rows.Count(row => !row.IsComponent), Is.EqualTo(19));
            Assert.That(matrix.Rows.Count(row => row.IsComponent), Is.EqualTo(6));
            Assert.That(matrix.AllIntroducedShapes(), Is.Not.Empty);
        }

        /// <summary>
        /// The claim milestone 8.69g made and this gate is the first to rest a table on: he introduces
        /// NO shape this table did not already carry. It is pinned where a later reader will look for
        /// it, because "the twentieth hero needed no new vocabulary" is a fact about the engine rather
        /// than about him - and a later refinement that quietly gave one of his contracts a control
        /// would fail here rather than in a paragraph.
        /// </summary>
        [Test]
        public void HeIntroducesNoShapeTheTableDidNotAlreadyCarry()
        {
            var his = Matrix().AllIntroducedShapes();
            Assert.That(his, Is.Not.Empty);
            Assert.That(
                his.Contains(FacedownTokenReading) || his.Contains(PublicCardRevealReading) ||
                    his.Contains(HeldMarkerReading) || his.Contains(ActiveEffectReading) ||
                    his.Contains(RoundLongActiveEffectReading) ||
                    his.Contains(OpponentRestrictionReading) ||
                    his.Contains(UltimateTriggerPause) || his.Contains(PushDistanceChoice) ||
                    his.Contains(ResourceAmountChoice) || his.Contains(DirectionChoice) ||
                    his.Contains(ColorNamingChoice) ||
                    his.Contains(RespawnedMinionTypeChoice) ||
                    his.Contains(ActionTextPlacementChoice),
                Is.False,
                "He raises no Active effect, holds no marker, places no token, reveals no card, " +
                "pushes nobody, transfers no coin, names no direction and no colour, and his " +
                "Ultimate is a permanent overlay rather than a trigger - so none of those shapes " +
                "belongs on a row of his. Derived: " +
                string.Join(", ", his.Select(shape => shape.ToString())));
        }
    }
}
