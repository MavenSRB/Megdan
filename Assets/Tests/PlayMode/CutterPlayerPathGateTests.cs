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
    /// Cutter the Sky Pirate's half of the permanent hero player-path completion gate. Milestone
    /// 8.39j, the FOURTEENTH hero to take it, and the last block of her series.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the thirteen gates before it. This
    /// milestone widened <c>CapabilityShapes</c> with NO new shape at all and exactly three capability
    /// ids - <c>synthetic-range-obstacle-target</c>, <c>claim-solo-victory</c> and
    /// <c>synthetic-note-actor</c> - and made ONE derivation refinement, which is the only change to
    /// the shared machinery: the push distance is now read off the node for BOTH of this build's
    /// pushes rather than for the batch alone. Every row of the thirteen heroes before her is
    /// byte-identical afterwards, which is the test of whether a refinement is a generalisation or a
    /// re-fit - Sabina's two faces are the only other content naming the named-target push and both
    /// declare one distance, so the predicate is false for them exactly as it was.</para>
    ///
    /// <para><b>Why this table was derived rather than written.</b> Nothing here declares what a face
    /// does; <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="CutterCardContent"/> and <see cref="CutterEffectContent"/> - action kind, origin,
    /// printed range, card-text timing, effect node kind, failure policy, capability identity. Every
    /// row below is written FROM that derivation, offline, the way milestone 8.24's method requires
    /// and every hero since has copied: the probe at <c>tmp/8-39j-shape-probe/</c> prints all nineteen
    /// faces in about two seconds, so the table is written already correct instead of found by
    /// nine-minute Unity runs.</para>
    ///
    /// <para><b>She declares no Active effect at all</b>, which is why no <c>*ActiveEffectContent</c>
    /// is passed below and why no row of hers carries an Active-effect reading: no face of hers prints
    /// "This turn:", "Next turn:" or "End of turn:". The deferred lists are composed anyway so the
    /// empty case is exercised rather than assumed.</para>
    /// </summary>
    [TestFixture]
    public sealed class CutterPlayerPathGateTests
    {
        // -------------------------------------------------------------------------------------------
        // The journeys, named once so a rename is a compile error rather than a silent gap
        // -------------------------------------------------------------------------------------------

        private const string TrayCommitAndWalk =
            "HerTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot";

        private const string DefeatAndRespawn =
            "HerDefenseAnswersAnAttack_AndHerPieceRespawnsWhenItDoesNot";

        private const string GrappleJourney =
            "GrapplingBoltReelsHerToTheFirstObstacleOnAFile_AndStopsBesideIt";

        private const string PlankJourney =
            "WalkThePlankPushesAHeroOutOfTheBattleZone_AndTakesACardForIt";

        private const string LungeJourney =
            "DaringStrikeMovesFirst_AndReachesOnlyTheHeroInTheDirectionOfThatMove";

        private const string BombardJourney =
            "BombardmentReachesTheEnemyBesideAnotherEnemy_AndTheyPickTheirOwnDiscard";

        private const string ChargeJourney =
            "BraceForImpactChargesThroughAHero_AndTakesACardFromWhoItLandsBeside";

        private const string RetreatJourney =
            "EvasiveShotFiresDownAFile_ThenRetreatsInTheOppositeDirection";

        private const string EnemyModalJourney =
            "XMarksTheSpotPutsItsModalToTheEnemySeat_WhoChoosesToBeMoved";

        private const string SwapJourney =
            "OutmaneuverSwapsHerWithAnEnemyMinion_ThenWalksThatMinionAway";

        private const string BroadsideJourney =
            "BroadsideTakesACard_AndOffersItsRepeatOnADifferentTarget";

        private const string UltimateJourney =
            "LegendOfTheSkiesBorrowsThePreviousTurnSlotsPrimary_AndPerformsItAgain";

        private const string VictoryJourney =
            "AFistfulOfCoinsPaysItsThirteenthCoin_AndOneHeroAloneWinsTheMatch";

        private const string SpawnJourney =
            "HerPieceSpawnsAsHerOwnToken_AndAnswersThePointer";

        private const string FootprintConformance =
            "EveryFaceOfHersDrawsAtTheStandardCardFootprint";

        private const string SessionOperations =
            "HerEngagedBoardRestores_Undoes_Replays_AndRestarts";

        private const string PresentationRebuildJourney =
            "HerHandAndRailsRebuildOnTheSameFootprintAfterAPresentationReload";

        // -------------------------------------------------------------------------------------------
        // The table. Every row's shapes are the probe's output for that face, copied, not invented.
        // -------------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Cutter",
            CutterCardContent.AllDefinitions,
            CutterCardContent.ReturnedToBoxAtSetup,
            // She creates no Active effect, so there are no Active-effect handlers of hers to pass.
            // The deferred lists are composed anyway, for the reason
            // `SyntheticPlacementCapabilities.DeferredHandlers` survives as a member at all: the empty
            // case is exercised rather than assumed, and a later hero deferring a handler that CREATES
            // an effect would otherwise have its rows derived from nothing.
            SyntheticConditionCapabilities.DeferredHandlers()
                .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers())
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Concat(SyntheticRosterCapabilities.DeferredHandlers()),
            // Legend of the Skies' own triggered effect, which its ULTIMATE rule carries by reference.
            CutterEffectContent.TriggeredEffects(CutterCardContent.ContentRevision),
            new[]
            {
                // p.6's Gold basic, and the roster's first face whose consequence reads p.19's zones.
                // Its push is the one in the build that prints "up to N" on the NAMED-target push, so
                // it is also the face that corrected this gate's own distance derivation.
                Face(CutterCardContent.WalkThePlank, "Walk the Plank", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, PlankJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, PlankJourney),
                        Prove(HandCardSelection, PlankJourney),
                        Prove(PushDistanceChoice, PlankJourney),
                        Prove(ModalBulletChoice, PlankJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.WalkThePlank_ComposesAModalWithAHeroPushArmAndAMinionDefeatArm",
                        "CutterPrintedSetTests.WalkThePlank_PushesUpToFourSpacesAndNotExactlyFour",
                        "ZoneCrossingPushTests.AFullDistancePushAcrossABoundary_ReportsTheCrossing"
                    }),

                // p.20's Handicap card. Returned to the box at p.6's setup, so no player reaches it
                // and the row says so rather than leaving an empty cell - the standing deferral every
                // hero's Handicap carries since the 2026-08-21 user decision.
                UnreachableFace(CutterCardContent.FewMoreSteps, "Few More Steps",
                    "p.20's Handicap card, returned to the box at p.6's setup and reachable only in " +
                    "a configuration this build does not deal. Its composition carries the standing " +
                    "p.20 deferral."),

                // p.6's Silver basic, and the only face in the corpus that targets an OBSTACLE. It
                // prints ONE secondary icon and the glyph is a shield, so it carries no boot at all -
                // which is why its row names no secondary Movement where seventeen of her faces do.
                Face(CutterCardContent.GrapplingBolt, "Grappling Bolt", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GrappleJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardSpaceSelection, GrappleJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.GrapplingBolt_PrintsADefenseSecondaryAndNoMovementSecondary",
                        "ObstacleLineTargetTests.EachFile_OffersItsFirstObstacleAndNothingBehindIt",
                        "ObstacleLineTargetTests.TheApproachSpace_IsOneStepBackAlongTheSameFile"
                    }),

                // The Red line. Tier I, and the first of the three lunges whose printed `+` belongs to
                // the branch that moves first.
                Face(CutterCardContent.DaringStrike, "Daring Strike", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, LungeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, LungeJourney),
                        Prove(BoardUnitSelection, LungeJourney),
                        Prove(BoardSpaceSelection, LungeJourney),
                        Prove(ModalBulletChoice, LungeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachLunge_NarrowsItsVictimToTheHeadingTheMoveTravelled",
                        "CutterPrintedSetTests.OnlyHerThreeLunges_RaiseTheirAttackOnTheMovedBranch",
                        "MovedBranchAttackBonusTests.TheBonusApplies_WhenTheActorHasLeftTheSpaceTheAttackStartedFrom"
                    }),

                // Tier II on the first branch: the same shape with two printed distances.
                Face(CutterCardContent.BoldThrust, "Bold Thrust", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, LungeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, LungeJourney),
                        Prove(BoardUnitSelection, LungeJourney),
                        Prove(BoardSpaceSelection, LungeJourney),
                        Prove(ModalBulletChoice, LungeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachLunge_WalksItsOwnPrintedDistance"
                    }),

                // Tier II on the second branch, and the first of the two shots whose rider stops the
                // page AFTER the attack and walks her back down the file she fired along.
                Face(CutterCardContent.EvasiveShot, "Evasive Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RetreatJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(AfterAttackTextPause, RetreatJourney),
                        Prove(BoardSpaceSelection, RetreatJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerRedLine_SplitsThreeOnCardTextAndTwoOnTheStraightLine",
                        "CutterPrintedSetTests.EachRangedShot_RetreatsAlongTheReverseOfItsOwnLine",
                        "ContinuedLineNarrowingTests.TheOppositeDirectionMove_TravelsOnlyTheReverseOfTheAttacksOwnLine"
                    }),

                // Tier III on the first branch. Three enumerated distances where Tier I prints one -
                // a VALUE difference, so it introduces no shape of its own and shares the lunge
                // journey.
                Face(CutterCardContent.FearlessLunge, "Fearless Lunge", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, LungeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, LungeJourney),
                        Prove(BoardUnitSelection, LungeJourney),
                        Prove(BoardSpaceSelection, LungeJourney),
                        Prove(ModalBulletChoice, LungeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachLunge_WalksItsOwnPrintedDistance"
                    }),

                // Tier III on the second branch, on Evasive Shot's shape with one more space of
                // retreat.
                Face(CutterCardContent.TumbleShot, "Tumble Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RetreatJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(AfterAttackTextPause, RetreatJourney),
                        Prove(BoardSpaceSelection, RetreatJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachRangedShot_RetreatsAlongTheReverseOfItsOwnLine"
                    }),

                // The Blue line. Tier I, and the first of the three that qualify their enemy by that
                // enemy's OWN neighbours and by p.9's innermost ring being dropped.
                Face(CutterCardContent.Bombardment, "Bombardment", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BombardJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, BombardJourney),
                        Prove(HandCardSelection, BombardJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerThreeBlueDiscards_NarrowBothHalvesOfOneSentence"
                    }),

                // Tier II on the first branch: Bombardment's text with a wider printed Radius, which is
                // an icon rather than a word and therefore two definitions rather than one.
                Face(CutterCardContent.Barrage, "Barrage", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BombardJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, BombardJourney),
                        Prove(HandCardSelection, BombardJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerThreeBlueDiscards_NarrowBothHalvesOfOneSentence"
                    }),

                // Tier II on the second branch, and the first of the two faces that hand a modal to an
                // enemy hero. The Place is HERS to carry out, which is why the row names a board space
                // as well as a board unit.
                Face(CutterCardContent.XMarksTheSpot, "X Marks the Spot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EnemyModalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, EnemyModalJourney),
                        Prove(BoardSpaceSelection, EnemyModalJourney),
                        Prove(ModalBulletChoice, EnemyModalJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerTwoEnemyChosenModals_HandTheChoiceToTheChosenEnemyHero"
                    }),

                // Tier III on the first branch, and the only face of hers that repeats.
                Face(CutterCardContent.Broadside, "Broadside", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BroadsideJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, BroadsideJourney),
                        Prove(RepeatDecision, BroadsideJourney),
                        Prove(BoardUnitSelection, BroadsideJourney),
                        Prove(HandCardSelection, BroadsideJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.OnlyBroadside_RepeatsOnADifferentSelectionSubject",
                        "ExcludedSubjectRepeatTests.TheCapture_ArmsFromTheHeroASelectionPublished_WhenItPublishesNoSpace",
                        "ExcludedSubjectRepeatTests.TheOffer_LosesTheSubjectTheAskingInstanceTook"
                    }),

                // Tier III on the second branch, and the only sentence in the whole corpus that ends
                // the match for a single hero. Its modal is X Marks the Spot's, so its board-space
                // half rests on that face's journey - the same shape, the same capability and the same
                // anchor - while the bullet this one's journey takes is the OTHER one.
                Face(CutterCardContent.AFistfulOfCoins, "A Fistful of Coins", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, VictoryJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, VictoryJourney),
                        Prove(BoardSpaceSelection, EnemyModalJourney),
                        Prove(ModalBulletChoice, VictoryJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.OnlyAFistfulOfCoins_ClaimsTheSoloVictoryAndAtItsOwnPrintedThreshold",
                        "SoloVictoryClaimTests.TheClaimIsTakenAtThePrintedThresholdAndNotBelowIt",
                        "SoloVictoryClaimTests.TheWinnerIsTheControllerAndNotTheActor"
                    }),

                // The Green line. Tier I, and the first of the three charges that cross obstacles and
                // land beside an enemy hero.
                Face(CutterCardContent.BraceForImpact, "Brace for Impact", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ChargeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(HandCardSelection, ChargeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerThreeCharges_DeclareTheirPrintedDistancesAndCrossObstacles",
                        "CutterPrintedSetTests.HerThreeCharges_NameTheirSubjectAfterTheMoveAndNotInsideTheChargeNode",
                        "ChargeLineObstacleCrossingTests.CrossingObstacles_ReachesADestinationTheBlockedRouteCouldNot"
                    }),

                // Tier II on the first branch: the same charge at two enumerated distances.
                Face(CutterCardContent.RammingSpeed, "Ramming Speed", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ChargeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(HandCardSelection, ChargeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerThreeCharges_DeclareTheirPrintedDistancesAndCrossObstacles",
                        "CutterPrintedSetTests.HerThreeCharges_NameTheirSubjectAfterTheMoveAndNotInsideTheChargeNode",
                        "ChargeLineObstacleCrossingTests.CrossingObstacles_ReachesADestinationTheBlockedRouteCouldNot"
                    }),

                // Tier II on the second branch, and the first of the two that swap her with an enemy
                // minion.
                Face(CutterCardContent.Outmaneuver, "Outmaneuver", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(BoardSpaceSelection, SwapJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachSwapFace_ExchangesWithAnEnemyMinionAndMayMoveIt"
                    }),

                // Tier III on the first branch: the charge at three enumerated distances.
                Face(CutterCardContent.Crashland, "Crashland", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ChargeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(HandCardSelection, ChargeJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerThreeCharges_DeclareTheirPrintedDistancesAndCrossObstacles",
                        "CutterPrintedSetTests.HerThreeCharges_NameTheirSubjectAfterTheMoveAndNotInsideTheChargeNode",
                        "ChargeLineObstacleCrossingTests.CrossingObstacles_ReachesADestinationTheBlockedRouteCouldNot"
                    }),

                // Tier III on the second branch: the swap with one more space of follow-up move.
                Face(CutterCardContent.Outsmart, "Outsmart", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(BoardSpaceSelection, SwapJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.EachSwapFace_ExchangesWithAnEnemyMinionAndMayMoveIt"
                    }),

                // p.16's Ultimate. It prints no Initiative, no action and neither Range nor Radius, so
                // it carries no CommitAndReveal and no secondary at all - the whole of its printed
                // content is the permanent ability, which is why its row is the shortest in the table.
                Face(CutterCardContent.LegendOfTheSkies, "Legend of the Skies", new[]
                    {
                        Prove(ConfirmOrDecline, UltimateJourney),
                        Prove(HandCardSelection, UltimateJourney),
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.LegendOfTheSkies_FiresAfterAPrimaryActionAndBorrowsThePreviousSlot",
                        "PreviousTurnSlotBorrowTests.YourOwnCardInThatSlot_IsTheOneThePoolNames"
                    }),

                // -----------------------------------------------------------------------------------
                // The components. These are the gate's own required shapes, whose subject is the
                // configured session and the board piece rather than anything her cards describe.
                // -----------------------------------------------------------------------------------

                Component("cutter-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, SpawnJourney),
                        Prove(HeroPiecePointerSelection, SpawnJourney),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, PresentationRebuildJourney)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerSet_IsNineteenFacesInThePrintedColourShape"
                    }),

                // Restore, Undo, replay and restart, on a board that actually deals her - the arm
                // milestone 8.39h recorded it could not reach, because at that block no configured
                // match dealt her at all. The snapshot half is additionally driven INSIDE a live
                // resolution by the Walk the Plank journey, which captures at a standing pause and
                // restores onto the same question.
                Component("cutter-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, SessionOperations),
                        Prove(SnapshotRestoreContinuation, PlankJourney),
                        Prove(RetainedPrefixUndo, SessionOperations),
                        Prove(ReplayReconstruction, SessionOperations),
                        Prove(RestartReconstruction, SessionOperations)
                    },
                    new[]
                    {
                        "CutterPrintedSetTests.HerHand_ComposesAtPageSixSetup"
                    }),

                Component("cutter-card-footprint",
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
                AbsentComponent("cutter-tokens-and-markers",
                    "tokens and markers",
                    "Cutter's nineteen faces place no token and attach no marker; she owns no supply " +
                    "and no rules component at all, which her inventory recorded at milestone 8.39 " +
                    "and the gate re-derives here from her own content."),

                // p.16's item track, on the terms every hero's item row states - and MEASURED rather
                // than claimed, because milestone 8.37's own item row is the recorded case of a gate
                // asserting an absence that was false. She prints TWELVE item icons: three Attack,
                // three Defense, three Initiative, and one each of Movement, Radius and Range, which
                // covers all six of p.16's kinds. The three Tier I faces print none.
                AbsentComponent("cutter-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; her twelve item icons cover all " +
                    "six of p.16's kinds, pinned at the definition tier")
            });

        // -------------------------------------------------------------------------------------------
        // The guards. These are HeroPlayerPathMatrix's own, shared with the thirteen gates before this.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryCutterFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesCuttersPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(CutterPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(CutterPlayerPathGateTests).Assembly);

        [Test]
        public void EveryCutterJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(CutterUiJourneyPlayModeTests),
                typeof(CutterEngagedUiJourneyPlayModeTests),
                typeof(CutterProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndCutterActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "cutter-items",
                CutterCardContent.AllDefinitions.Any(definition =>
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
                CutterCardContent.AllDefinitions
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
