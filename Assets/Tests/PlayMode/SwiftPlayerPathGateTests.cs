using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Swift the Sharpshooter's half of the permanent hero player-path completion gate. Milestone
    /// 8.38f, and the THIRTEENTH hero to take it - the first after the package arc closed.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the twelve gates before it. This
    /// milestone widened <c>CapabilityShapes</c> with NO new shape at all and exactly three capability
    /// ids - <c>synthetic-place-in-radius-in-straight-line</c>,
    /// <c>synthetic-place-in-radius-off-every-straight-line</c> and
    /// <c>subject-adjacent-terrain-presence</c>. That a hero this distinctive introduces no new SHAPE
    /// is the finding rather than an omission: her novelty is in which spaces an offer contains and in
    /// what may be reached through a grant, and neither changes how a player names one.</para>
    ///
    /// <para><b>Why this table was derived rather than written.</b> Nothing here declares what a face
    /// does; <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="SwiftCardContent"/> and <see cref="SwiftEffectContent"/> - action kind, origin,
    /// printed range, card-text timing, effect node kind, failure policy, capability identity. Every
    /// row below is written FROM that derivation, offline, the way milestone 8.24's method requires
    /// and every hero since has copied: the probe at <c>tmp/8-38f-shape-probe/</c> prints all nineteen
    /// faces in about two seconds, so the table is written already correct instead of found by
    /// nine-minute Unity runs.</para>
    /// </summary>
    [TestFixture]
    public sealed class SwiftPlayerPathGateTests
    {
        // -------------------------------------------------------------------------------------------
        // The journeys, named once so a rename is a compile error rather than a silent gap
        // -------------------------------------------------------------------------------------------

        private const string TrayCommitAndWalk =
            "HerTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot";

        private const string DefeatAndRespawn =
            "HerDefenseAnswersAnAttack_AndHerPieceRespawnsWhenItDoesNot";

        private const string SnipeJourney =
            "SnipeOffersOnlyTheMaximumRangeFile_AndNothingBesideIt";

        private const string BounceJourney =
            "BounceWalksAStraightLineThroughObstacles_AndOffersNoRepeatOnItsOwnTurn";

        private const string ReloadJourney =
            "ReloadOffersItsTwoBulletsAndDefeatsAnAdjacentMinion";

        // RENAMED at implementation. The draft name said "OffEveryFile", which is Delayed Jump's
        // clause and the COMPLEMENT of what Steam Jump prints - "Place yourself into a space in a
        // straight line in radius". The journey proves the printed reading, so the name follows it.
        private const string SteamJumpJourney =
            "SteamJumpPlacesHerOnAFileInRadius_ThenPushesWhatSheLandsBeside";

        private const string SuppressJourney =
            "SuppressReachesOnlyAnEnemyAwayFromTerrain_AndTakesACardFromThem";

        private const string SpawnJourney =
            "HerPieceSpawnsAsHerOwnToken_AndAnswersThePointer";

        private const string FootprintConformance =
            "EveryFaceOfHersDrawsAtTheStandardCardFootprint";

        private const string SessionOperations =
            "HerEngagedBoardRestores_Undoes_Replays_AndRestarts";

        private const string PresentationRebuildJourney =
            "HerHandAndRailsRebuildOnTheSameFootprintAfterAPresentationReload";

        private const string ShotgunJourney =
            "ShotgunStopsBeforeTheAttack_AndScattersOntoAHeroBesideTheTarget";

        private const string DelayedJumpJourney =
            "DelayedJumpStandsItsPromise_AndPlacesHerWhenTheTurnEnds";

        private const string MarkForDeathJourney =
            "MarkForDeathMovesAMinion_ThenPaysItsCountedBountyNextTurn";

        private const string BulletTimeJourney =
            "BulletTimeGrantsTheResolvedCardsPrimary_AndNamesItsOwnRefusal";

        // -------------------------------------------------------------------------------------------
        // The table. Every row's shapes are the probe's output for that face, copied, not invented.
        // -------------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Swift",
            SwiftCardContent.AllDefinitions,
            SwiftCardContent.ReturnedToBoxAtSetup,
            // Her Active-effect handlers are here because the gate classifies a capability that
            // CREATES an effect from the definition it creates: without them her two "End of turn:"
            // faces and her two bounty faces would be derived from nothing and their rows would be
            // guesses. The deferred lists are all empty now that she is dealt - they are composed
            // anyway so the empty case is exercised rather than assumed, which is the same reason
            // `SyntheticPlacementCapabilities.DeferredHandlers` survives as an empty member.
            SwiftActiveEffectContent.Handlers()
                .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers())
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Concat(SyntheticRosterCapabilities.DeferredHandlers()),
            // Bullet Time's own triggered effect, which its ULTIMATE rule carries by reference. Her two
            // Active-effect triggers are deliberately NOT here: those are reached through the
            // definitions their capabilities create, and the two reading shapes are what the gate says
            // about them - the same division Tali's and Silverarrow's rows already draw.
            new[] { SwiftEffectContent.BulletTimeTriggeredEffect(SwiftCardContent.ContentRevision) },
            new[]
            {
                // p.6's Gold basic. The only face in her set that prints p.10's "Choose one —", and the
                // only one whose first bullet borrows: `borrow-primary-action@1` over the rightmost
                // resolved card, milestone 8.38c's pool.
                Face(SwiftCardContent.Reload, "Reload!", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ReloadJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, ReloadJourney),
                        Prove(HandCardSelection, ReloadJourney),
                        Prove(ModalBulletChoice, ReloadJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.Reload_ComposesAModalWithABorrowArmAndADefeatArm",
                        "RightmostResolvedCardBorrowTests.ThePool_IsTheLastCardResolvedThisRoundAndNothingElse"
                    }),

                // p.20's Handicap card. Returned to the box at p.6's setup, so no player reaches it and
                // the row says so rather than leaving an empty cell - the standing deferral every
                // hero's Handicap carries since the 2026-08-21 user decision.
                UnreachableFace(SwiftCardContent.Backup, "Backup",
                    "p.20's Handicap card, returned to the box at p.6's setup and reachable only in a " +
                    "configuration this build does not deal. Its composition carries the standing " +
                    "p.20 deferral."),

                // p.6's Silver basic. Its "may repeat once" gate can only hold for a SECOND performance
                // off a card whose own turn is over, which is why the journey proves the gate closed on
                // her own turn and Bullet Time's journey proves it open.
                Face(SwiftCardContent.Bounce, "Bounce", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BounceJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, BounceJourney),
                        Prove(RepeatDecision, BulletTimeJourney),
                        Prove(BoardSpaceSelection, BounceJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.Bounce_PrintsADefenseSecondaryAndNoMovementSecondary",
                        "SwiftPrintedSetTests.HerSet_IsNineteenFacesInThePrintedColourShape"
                    }),

                // The Red line. Tier I, and milestone 8.38a's straight-line band at maximum range.
                Face(SwiftCardContent.Snipe, "Snipe", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SnipeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.Snipe_DeclaresTheExactBandOnTheFileAndComposesNoEffect",
                        "PrintedTargetLineAndProximityTests.TheExactBandOnTheFile_RefusesAUnitAtTheRightDistanceOffEveryFile"
                    }),

                // Tier II on the first branch: the same file, with p.19's separation-of-one taken out
                // from under it.
                Face(SwiftCardContent.PreparedShot, "Prepared Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SnipeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.PreparedShotAndKillshot_DeclareTheFileAndTheAdjacentExclusion",
                        "PrintedTargetLineAndProximityTests.TheBeyondAdjacentBand_IsThePageThirteenCeilingWithoutItsAdjacentUnits"
                    }),

                // Tier II on the second branch, and the first of the two faces whose rider stops the
                // page BEFORE the attack - `choose-target@2` anchored on the target rather than on her.
                Face(SwiftCardContent.Shotgun, "Shotgun", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ShotgunJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, ShotgunJourney),
                        Prove(BoardUnitSelection, ShotgunJourney),
                        Prove(HandCardSelection, ShotgunJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.TheTwoShotguns_NarrowNothingAndApplyTheirRiderBeforeTheAttack"
                    }),

                // Tier III on the first branch. The same declared band as Prepared Shot, one Range
                // further and one Attack higher - a VALUE difference, so it introduces no shape of its
                // own and shares her file journey.
                Face(SwiftCardContent.Killshot, "Killshot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SnipeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.PreparedShotAndKillshot_DeclareTheFileAndTheAdjacentExclusion"
                    }),

                // Tier III on the second branch, and the only face in her set whose refused discard
                // ESCALATES to a Defeat - ledger question 13's reading, reached only where the discard
                // could not complete and asked of nobody.
                Face(SwiftCardContent.SuperShotgun, "Super-Shotgun", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ShotgunJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, ShotgunJourney),
                        Prove(BoardUnitSelection, ShotgunJourney),
                        Prove(HandCardSelection, ShotgunJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.OnlySuperShotgun_EscalatesAnImpossibleDiscardToADefeat"
                    }),

                // The Blue line. Tier I, and the first of the three that place her OFF every file and
                // then push what she lands beside.
                Face(SwiftCardContent.SteamJump, "Steam Jump", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SteamJumpJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, SteamJumpJourney),
                        Prove(BoardUnitSelection, SteamJumpJourney),
                        Prove(BoardSpaceSelection, SteamJumpJourney),
                        Prove(PushDistanceChoice, SteamJumpJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerBlueLine_SplitsThreeOnTheFileAndTwoOffIt",
                        "PlacementLineQualifierTests.TheOffFileSet_ExcludesEveryNeighbourBecausePageNineteenCountsAdjacencyAsALine"
                    }),

                // Tier II on the first branch, on Steam Jump's shape with a wider printed Radius.
                Face(SwiftCardContent.AssaultJump, "Assault Jump", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SteamJumpJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, SteamJumpJourney),
                        Prove(BoardUnitSelection, SteamJumpJourney),
                        Prove(BoardSpaceSelection, SteamJumpJourney),
                        Prove(PushDistanceChoice, SteamJumpJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerBlueLine_SplitsThreeOnTheFileAndTwoOffIt"
                    }),

                // Tier III on the first branch, and the only face in her set that pushes TWO units.
                Face(SwiftCardContent.DropTrooper, "Drop Trooper", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SteamJumpJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, SteamJumpJourney),
                        Prove(BoardUnitSelection, SteamJumpJourney),
                        Prove(BoardSpaceSelection, SteamJumpJourney),
                        Prove(PushDistanceChoice, SteamJumpJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.OnlyDropTrooper_DeclaresATwoUnitPush"
                    }),

                // Tier II on the second branch, and one of the only two faces in her set that print
                // "End of turn:" - a promise that stands and then places her when the turn closes.
                Face(SwiftCardContent.DelayedJump, "Delayed Jump", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DelayedJumpJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ActiveEffectReading, DelayedJumpJourney),
                        Prove(RoundLongActiveEffectReading, DelayedJumpJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerBlueLine_SplitsThreeOnTheFileAndTwoOffIt",
                        "PlacementLineQualifierTests.TheTwoDeclarations_PartitionThePrintedDiskExactly"
                    }),

                // Tier III on the second branch, and the only face in her whole set that fast travels.
                Face(SwiftCardContent.MobileScout, "Mobile Scout", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DelayedJumpJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ActiveEffectReading, DelayedJumpJourney),
                        Prove(RoundLongActiveEffectReading, DelayedJumpJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.OnlyMobileScout_FollowsItsPlacementWithAFastTravel"
                    }),

                // The Green line. Tier I, and the first of the three that qualify their enemy by that
                // enemy's OWN footing - milestone 8.38d's `subject-adjacent-terrain-presence@1`, which
                // the 8.38 audit had mapped onto the actor-scoped predicate that asks a different
                // question.
                Face(SwiftCardContent.Suppress, "Suppress", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SuppressJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, SuppressJourney),
                        Prove(HandCardSelection, SuppressJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerThreeTerrainFaces_AskAboutTheSubjectAndNotTheActor"
                    }),

                // Tier II on the first branch, on Suppress's shape with a wider printed Radius.
                Face(SwiftCardContent.PinDown, "Pin Down", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SuppressJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, SuppressJourney),
                        Prove(HandCardSelection, SuppressJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerThreeTerrainFaces_AskAboutTheSubjectAndNotTheActor"
                    }),

                // Tier III on the first branch, and the only terrain face that escalates.
                Face(SwiftCardContent.KillingGround, "Killing Ground", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SuppressJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, SuppressJourney),
                        Prove(HandCardSelection, SuppressJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.OnlyKillingGround_EscalatesAmongTheTerrainFaces"
                    }),

                // Tier II on the second branch, and the first of the two faces that stand milestone
                // 8.38c1's counted bounty. One promise, spent by the first qualifying Defeat.
                Face(SwiftCardContent.MarkForDeath, "Mark for Death", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MarkForDeathJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, MarkForDeathJourney),
                        Prove(BoardSpaceSelection, MarkForDeathJourney),
                        Prove(ActiveEffectReading, MarkForDeathJourney),
                        Prove(RoundLongActiveEffectReading, MarkForDeathJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.TheTwoBountyFaces_StandOneAndTwoPromises",
                        "MinionDefeatBountyTests.ADefeatInsideTheRadius_PaysOneCoinAndSpendsThePromise"
                    }),

                // Tier III on the second branch. TWO promises rather than one, and the pair carries
                // distinct uniqueness keys - without which the lifecycle would suppress the second and
                // silently turn this face into Mark for Death.
                Face(SwiftCardContent.HuntingSeason, "Hunting Season", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MarkForDeathJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, MarkForDeathJourney),
                        Prove(BoardSpaceSelection, MarkForDeathJourney),
                        Prove(ActiveEffectReading, MarkForDeathJourney),
                        Prove(RoundLongActiveEffectReading, MarkForDeathJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HuntingSeasonsTwoPromises_CarryDifferentUniquenessKeys",
                        "MinionDefeatBountyTests.TwoPromises_PayExactlyTwiceAcrossThreeDefeats"
                    }),

                // p.16's Ultimate. It prints no Initiative, no action and neither Range nor Radius, so
                // it carries no CommitAndReveal and no secondary at all - the whole of its printed
                // content is the permanent ability, which is why its row is the shortest in the table
                // and names shapes no other face of hers does.
                Face(SwiftCardContent.BulletTime, "Bullet Time", new[]
                    {
                        Prove(ConfirmOrDecline, BulletTimeJourney),
                        Prove(HandCardSelection, BulletTimeJourney),
                        Prove(UltimateRailVisibility, BulletTimeJourney),
                        Prove(UltimateTriggerPause, BulletTimeJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.BulletTime_DeclaresTheCardResolutionTriggerAndGrantsOptionally",
                        "GrantedTargetExclusionTests.BulletTime_PerformsThePrimaryOfTheBasicCardItJustResolved",
                        "GrantedTargetExclusionTests.TheGrantedInstance_IsNarrowedByWhatTheGrantHasAlreadyReached"
                    }),

                // -----------------------------------------------------------------------------------
                // The components. These are the gate's own required shapes, whose subject is the
                // configured session and the board piece rather than anything her cards describe.
                // -----------------------------------------------------------------------------------

                Component("swift-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, SpawnJourney),
                        Prove(HeroPiecePointerSelection, SpawnJourney),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, PresentationRebuildJourney)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerSet_IsNineteenFacesInThePrintedColourShape"
                    }),

                Component("swift-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, SessionOperations),
                        Prove(RetainedPrefixUndo, SessionOperations),
                        Prove(ReplayReconstruction, SessionOperations),
                        Prove(RestartReconstruction, SessionOperations)
                    },
                    new[]
                    {
                        "SwiftPrintedSetTests.HerHand_ComposesAtPageSixSetup"
                    }),

                Component("swift-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests" +
                            ".EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests" +
                            ".NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // She owns NO rules component, which is the ordinary case and is derived rather than
                // claimed: the gate reads the same absence off her content and fails this row if a
                // face of hers ever places one. Her profile card prints no Tokens role icon.
                AbsentComponent("swift-tokens-and-markers",
                    "tokens and markers",
                    "Swift's nineteen faces place no token and attach no marker; she owns no supply " +
                    "and no rules component at all, which SW-03 recorded at her inventory and the " +
                    "gate re-derives here from her own content."),

                // p.16's item track, on the terms every hero's item row states - and MEASURED rather
                // than claimed, because milestone 8.37's own item row is the recorded case of a gate
                // asserting an absence that was false. She prints TWELVE item icons: three Attack,
                // three Defense, three Initiative, and one each of Movement, Radius and Range, which
                // covers all six of p.16's kinds. The three Tier I faces print none.
                AbsentComponent("swift-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; her twelve item icons cover all " +
                    "six of p.16's kinds, pinned at the definition tier")
            });

        // -------------------------------------------------------------------------------------------
        // The guards. These are HeroPlayerPathMatrix's own, shared with the twelve gates before this.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EverySwiftFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesSwiftsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(SwiftPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(SwiftPlayerPathGateTests).Assembly);

        [Test]
        public void EverySwiftJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(SwiftUiJourneyPlayModeTests),
                typeof(SwiftEngagedUiJourneyPlayModeTests),
                typeof(SwiftProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndSwiftActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "swift-items",
                SwiftCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        /// <summary>
        /// Milestone 8.37's item row is the recorded case of a gate asserting an ABSENCE that was
        /// false - it claimed a hero printed no item icon and she printed twelve, and no offline tier
        /// could contradict it because an item icon introduces no SHAPE. This is the assertion that
        /// actually reads the content, written from the measured count rather than from the claim.
        /// </summary>
        [Test]
        public void HerTwelveItemIcons_CoverEveryKindPageSixteenHas() =>
            Assert.That(
                SwiftCardContent.AllDefinitions
                    .Where(definition => definition.Progression.HasItemIcon)
                    .Select(definition => definition.Progression.ItemIcon)
                    .Distinct()
                    .OrderBy(channel => channel)
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.Attack,
                    PrintedValueChannel.Defense,
                    PrintedValueChannel.Initiative,
                    PrintedValueChannel.Movement,
                    PrintedValueChannel.Range,
                    PrintedValueChannel.Radius
                }.Distinct().OrderBy(channel => channel).ToArray()),
                "Her twelve item icons cover all six of p.16's kinds. Measured, not claimed.");

        /// <summary>
        /// The tenth guard, and the one the offline probe cannot answer: it needs the Edit Mode
        /// assembly loaded. A grep over the Edit Mode fixtures answers it out of the Editor.
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
