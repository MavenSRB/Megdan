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
    /// Bain the Bounty Hunter's half of the permanent hero player-path completion gate. Milestone
    /// 8.40k, the FIFTEENTH hero to take it, and the last block of his series.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the fourteen gates before it. This
    /// milestone widened <c>CapabilityShapes</c> with SIX capability ids and exactly ONE new shape -
    /// <see cref="PlayerInteractionShape.PublicCardRevealReading"/>, for "then reveal the card" - where
    /// Swift's and Cutter's dealings each introduced none. His four marker operations are readings on
    /// Tigerclaw's own terms, the hidden card choice is the ordinary hand-card question asked of a
    /// different seat, and his five Active-effect creations are resolved through the handler walk the
    /// matrix has done since milestone 8.20a. Every row of the fourteen heroes before him is
    /// byte-identical afterwards, which is the test of whether a widening is a generalisation or a
    /// re-fit.</para>
    ///
    /// <para><b>Why this table was derived rather than written.</b> Nothing here declares what a face
    /// does; <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="BainCardContent"/>, <see cref="BainEffectContent"/> and
    /// <see cref="BainActiveEffectContent"/> - action kind, origin, printed range, card-text timing,
    /// effect node kind, failure policy, capability identity. Every row below is written FROM that
    /// derivation, offline, the way milestone 8.24's method requires and every hero since has copied:
    /// the probe at <c>tmp/8-40k-shape-probe/</c> prints all nineteen faces in about two seconds, so
    /// the table is written already correct instead of found by nine-minute Unity runs.</para>
    ///
    /// <para><b>THREE THINGS HIS TABLE SAYS THAT NO EARLIER HERO'S DOES.</b> A card is chosen from an
    /// ENEMY's seat and held hidden while the controller answers a different question; a REVEAL
    /// changes what every other seat can see mid-resolution; and three Green Blocks succeed or fail on
    /// a board fact - who is wearing his bounty - that the defender can read before choosing. The
    /// first two are driven from at least two seats at once, because a hidden answer verified only
    /// from the host's state is not verified at all.</para>
    /// </summary>
    [TestFixture]
    public sealed class BainPlayerPathGateTests
    {
        // -------------------------------------------------------------------------------------------
        // The journeys, named once so a rename is a compile error rather than a silent gap
        // -------------------------------------------------------------------------------------------

        private const string TrayCommitAndWalk =
            "HisTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot";

        private const string DefeatAndRespawn =
            "HisDefenseAnswersAnAttack_AndHisPieceRespawnsWhenItDoesNot";

        private const string BountyGiveJourney =
            "DeadOrAliveGivesTheBountyAfterItsAttack_AndTheHoldersRailNamesIt";

        private const string PullJourney =
            "GetOverHerePullsTheFirstUnitOnItsFile_UntilItStandsBesideHim";

        private const string ClearLineJourney =
            "LightCrossbowShootsDownAClearFile_AndRefusesWhatStandsBehindAUnit";

        private const string GuessMatchJourney =
            "AGameOfChanceHidesTheEnemysCard_GuessesIt_AndRevealsItToEverySeat";

        private const string CloseCallJourney =
            "CloseCallBlocksTheAttack_AndPullsTheBountyOntoHisOwnRail";

        private const string SpawnJourney =
            "HisPieceSpawnsAsHisOwnToken_AndAnswersThePointer";

        private const string FootprintConformance =
            "EveryFaceOfHisDrawsAtTheStandardCardFootprint";

        private const string SessionOperations =
            "HisEngagedBoardRestores_Undoes_Replays_AndRestarts";

        private const string PresentationRebuildJourney =
            "HisHandAndRailsRebuildOnTheSameFootprintAfterAPresentationReload";

        private const string MarkerBulletJourney =
            "HandCrossbowTakesItsMarkerBullet_AndReachesTheHeroWearingTheBounty";

        private const string OneOrBothJourney =
            "HunterSeekerTakesBothBullets_AsTwoCompleteAttackOrders";

        private const string GuessMissJourney =
            "DeadMansHandMissesItsGuess_AndPaysItsCoinsInstead";

        private const string FailureModalJourney =
            "WereNotDoneYetRepeatsOnceOnAMiss_ThenCanOnlyTakeTheCoins";

        private const string RetrievalJourney =
            "DrinkingBuddiesLetsTheOtherHeroRetrieveFromTheirOwnSeat_ThenHimself";

        private const string EndOfTurnRepeatJourney =
            "AnotherOneArmsItsEndOfTurnRepeat_AndTheTrayReadsItWhileItStands";

        private const string VantagePointJourney =
            "VantagePointIgnoresObstacles_AndRaisesHisMovementWhileABountyIsOut";

        private const string HighGroundJourney =
            "HighGroundRaisesHisMovementByTwo_AndCrossesWhatItIgnores";

        private const string NarrowEscapeJourney =
            "NarrowEscapeBlocksTheAttack_AndReturnsTheBountyToTheSupply";

        private const string PerfectGetawayJourney =
            "PerfectGetawayBlocksTheAttackOutright_WhileABountyIsOnTheBoard";

        private const string UltimateJourney =
            "AComplicatedProfessionFiresOnHisGive_AndTheRecipientDiscardsFromTheirOwnSeat";

        // -------------------------------------------------------------------------------------------
        // The table. Every row's shapes are the probe's output for that face, copied, not invented.
        // -------------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Bain",
            BainCardContent.AllDefinitions,
            BainCardContent.ReturnedToBoxAtSetup,
            // His five Active-effect creations - two obstacle permissions, two Movement raises and
            // Another One!'s End-of-turn repeat - plus the deferred lists, composed for the reason
            // `SyntheticPlacementCapabilities.DeferredHandlers` survives as a member at all: the empty
            // case is exercised rather than assumed.
            BainActiveEffectContent.Handlers()
                .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers())
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Concat(SyntheticRosterCapabilities.DeferredHandlers()),
            // A Complicated Profession's own triggered effect, which its ULTIMATE rule carries by
            // reference, and Another One!'s End-of-turn repeat beside it.
            BainEffectContent.TriggeredEffects(BainCardContent.ContentRevision),
            new[]
            {
                // p.6's Gold basic, and the only face of his that both attacks and gives. Its "After
                // the attack:" clause is p.10's `may`, so the row carries the confirm-or-decline, the
                // radius selection that names the recipient, and the marker reading the give leaves on
                // the holder's own rail.
                Face(BainCardContent.DeadOrAlive, "Dead or Alive", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, BountyGiveJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(AfterAttackTextPause, BountyGiveJourney),
                        Prove(ConfirmOrDecline, BountyGiveJourney),
                        Prove(BoardUnitSelection, BountyGiveJourney),
                        Prove(HeldMarkerReading, BountyGiveJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests.DeadOrAlive_MaySelectAnEnemyHeroInRadiusAndGivesLast",
                        "BainPrintedSetTests.TheGive_PutsTheMarkerOnTheChosenHeroAndReportsTheMoment",
                        "BainMarkerDefeatCostTests" +
                            ".AHeroWearingTheMarker_SpendsTierPlusOne_AndAHeroWearingNoneSpendsTier"
                    }),

                // p.20's Handicap card. Returned to the box at p.6's setup, so no player reaches it
                // and the row says so rather than leaving an empty cell - the standing deferral every
                // hero's Handicap carries since the 2026-08-21 user decision.
                UnreachableFace(BainCardContent.SideQuest, "Side Quest",
                    "p.20's Handicap card, returned to the box at p.6's setup and reachable only in " +
                    "a configuration this build does not deal. Its composition carries the standing " +
                    "p.20 deferral."),

                // p.6's Silver basic, and the only face in his set whose line clause is p.18's WIDE
                // obstacle list (BA-03). It prints one secondary icon and the glyph is a shield, so
                // it carries no boot at all - which is why its row names no secondary Movement where
                // twelve of his faces do.
                Face(BainCardContent.GetOverHere, "Get over here!", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, PullJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardSpaceSelection, PullJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".GetOverHere_TargetsAUnitOrTokenAndPullsItToTheActorsOwnApproach",
                        "PullLineTargetTests.TheNarrowedSelection_OffersExactlyTheFirstUnitOrTokenOnEachFile",
                        "PullLineTargetTests.AnAlreadyAdjacentSubject_IsOfferedAndTheMoveLeavesItWhereItStands"
                    }),

                // The Red line. Tier I, and the first of the three plain crossbows whose whole printed
                // text is a targeting DECLARATION - no card-text body at all, which is why these three
                // rows are the shortest attack rows in the table.
                Face(BainCardContent.LightCrossbow, "Light Crossbow", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ClearLineJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisRedLine_SplitsThreeOnTheClearLineAndTwoOnTheMarkerModal",
                        "BainCombatOutcomeTests" +
                            ".HisThreePlainCrossbows_RunACompletePageFourteenAndResolveAnOutcome"
                    }),

                // Tier II on the first branch: the same clear-line shot one space further.
                Face(BainCardContent.HeavyCrossbow, "Heavy Crossbow", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ClearLineJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisRedLine_SplitsThreeOnTheClearLineAndTwoOnTheMarkerModal",
                        "BainCombatOutcomeTests" +
                            ".HisThreePlainCrossbows_RunACompletePageFourteenAndResolveAnOutcome"
                    }),

                // Tier II on the second branch, and the first of the two marker modals. p.10's
                // "Choose one" over two `attack-sequence@1` bullets, the first narrowed to the hero
                // wearing his bounty.
                Face(BainCardContent.HandCrossbow, "Hand Crossbow", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, MarkerBulletJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, MarkerBulletJourney),
                        Prove(ModalBulletChoice, MarkerBulletJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".TheTwoMarkerModals_DifferOnlyInTheirCardinalityAndTheirDistinctTargetsClause",
                        "BainMarkerPredicateTests.TheFourthMember_NarrowsABulletsSubjectsToHolders"
                    }),

                // Tier III on the first branch: the clear-line shot at its widest printed Range.
                Face(BainCardContent.Arbalest, "Arbalest", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ClearLineJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisRedLine_SplitsThreeOnTheClearLineAndTwoOnTheMarkerModal",
                        "BainCombatOutcomeTests" +
                            ".HisThreePlainCrossbows_RunACompletePageFourteenAndResolveAnOutcome"
                    }),

                // Tier III on the second branch, and the only face in his set that may take TWO
                // complete p.14 orders in one action - BA-D4's reading of Dodger's D-06 ruling.
                Face(BainCardContent.HunterSeeker, "Hunter-Seeker", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, OneOrBothJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, OneOrBothJourney),
                        Prove(ModalBulletChoice, OneOrBothJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".TheTwoMarkerModals_DifferOnlyInTheirCardinalityAndTheirDistinctTargetsClause",
                        "BainCombatOutcomeTests" +
                            ".HunterSeekersAdjacentBullet_RunsACompletePageFourteenAndResolvesAnOutcome"
                    }),

                // The Blue line. Tier I, and the first of the three guessing faces - the first hidden
                // choice and the first public reveal this build has ever needed.
                Face(BainCardContent.AGameOfChance, "A Game of Chance", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GuessMatchJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, GuessMatchJourney),
                        Prove(HandCardSelection, GuessMatchJourney),
                        Prove(ColorNamingChoice, GuessMatchJourney),
                        Prove(PublicCardRevealReading, GuessMatchJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests.HisThreeGuessingFaces_ComposeTheClauseInThePrintedOrder",
                        "BainGuessResolutionTests.ACorrectGuess_DiscardsThatCardAndOnlyThatCard",
                        "BainHiddenAnswerProjectionTests" +
                            ".TheChoosersOwnSeat_HoldsTheWholeDecisionAndCanAnswerIt"
                    }),

                // Tier II on the first branch: the same clause paying two coins on a miss.
                Face(BainCardContent.DeadMansHand, "Dead Man's Hand", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GuessMissJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardUnitSelection, GuessMissJourney),
                        Prove(HandCardSelection, GuessMissJourney),
                        Prove(ColorNamingChoice, GuessMissJourney),
                        Prove(PublicCardRevealReading, GuessMissJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".TheThreeGuessingFaces_DifferInTheRewardAndTheFailureBranchAlone",
                        "BainGuessResolutionTests.AWrongGuess_AwardsThePrintedCoinsAndTakesNothing"
                    }),

                // Tier II on the second branch, and the first of the two shared retrievals. p.21's
                // Healer paragraph in as many words: another hero retrieves from their own discard,
                // choosing from their own seat, and only then may he retrieve himself.
                Face(BainCardContent.DrinkingBuddies, "Drinking Buddies", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(BoardUnitSelection, RetrievalJourney),
                        Prove(HandCardSelection, RetrievalJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests.TheRetrievalSelection_NamesAnySideAndExcludesTheActor",
                        "BainPrintedSetTests" +
                            ".HisOwnRetrieval_IsOfferedOnlyAfterTheOtherHeroActuallyRetrieved"
                    }),

                // Tier III on the first branch, and the only face in the corpus whose FAILURE branch
                // is itself a modal - "otherwise may repeat once or gain 2 coins".
                Face(BainCardContent.WereNotDoneYet, "We're Not Done Yet!", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, FailureModalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, FailureModalJourney),
                        Prove(RepeatDecision, FailureModalJourney),
                        Prove(BoardUnitSelection, FailureModalJourney),
                        Prove(HandCardSelection, FailureModalJourney),
                        Prove(ModalBulletChoice, FailureModalJourney),
                        Prove(ColorNamingChoice, FailureModalJourney),
                        Prove(PublicCardRevealReading, FailureModalJourney)
                    },
                    new[]
                    {
                        "BainGuessResolutionTests.TheModal_IsNeverReachedOnACorrectGuess",
                        "BainGuessResolutionTests.TheCoinBullet_IsTheOtherArmOfTheSameModal",
                        "BainGuessResolutionTests" +
                            ".TheRepeatBullet_IsNotOfferedWhenNoSecondInstanceCouldRun",
                        "BainGuessResolutionTests.ASecondRun_ReAsksTheSelectionAndExcludesNeitherEnemy"
                    }),

                // Tier III on the second branch: Drinking Buddies' sentence plus Tali's four
                // End-of-turn costs, which is what its extra keyword sentence buys.
                Face(BainCardContent.AnotherOne, "Another One!", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EndOfTurnRepeatJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, EndOfTurnRepeatJourney),
                        Prove(BoardUnitSelection, EndOfTurnRepeatJourney),
                        Prove(HandCardSelection, EndOfTurnRepeatJourney),
                        Prove(ActiveEffectReading, EndOfTurnRepeatJourney),
                        Prove(RoundLongActiveEffectReading, EndOfTurnRepeatJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".TheTwoRetrievalFaces_AreNotCollapsedAndDifferOnlyInTheKeywordSentence"
                    }),

                // The Green line. Tier I, and the first of the three Blocks whose condition is a fact
                // about the BOARD rather than about the attack. Its consequence is the transfer: the
                // marker comes off whoever is wearing it and onto him.
                Face(BainCardContent.CloseCall, "Close Call", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, CloseCallJourney),
                        Prove(DefenseTextApplication, CloseCallJourney),
                        Prove(HeldMarkerReading, CloseCallJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisThreeBlocks_DeclareTheBoardConditionAndOnlyTwoCarryAConsequence",
                        "BainMarkerMovementTests" +
                            ".ATransfer_MovesOneAttachmentBetweenHolders_KeepingItsInstanceIdentity",
                        "BainPrintedSetTests.TheBlockCondition_IsTrueOnlyWhileAHeroWearsHisMarker"
                    }),

                // Tier II on the first branch: the same Block returning the marker to the supply.
                Face(BainCardContent.NarrowEscape, "Narrow Escape", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, NarrowEscapeJourney),
                        Prove(DefenseTextApplication, NarrowEscapeJourney),
                        Prove(HeldMarkerReading, NarrowEscapeJourney)
                    },
                    new[]
                    {
                        "BainMarkerMovementTests" +
                            ".AReturnAny_DetachesTheOneAttachmentWhereverItSits_AndPublishesItsFormerHolder",
                        "BainMarkerMovementTests" +
                            ".TheTwoReadingsOfTheReturn_DifferOnAMarkerAnotherCardGave"
                    }),

                // Tier II on the second branch: a Movement primary carrying two sentences, one
                // unconditional permission and one marker-gated raise.
                Face(BainCardContent.VantagePoint, "Vantage Point", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, VantagePointJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeMovementTextPause, VantagePointJourney),
                        Prove(ActiveEffectReading, VantagePointJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisTwoMovementFaces_GrantAnUnconditionalPermissionAndAGatedRaise",
                        "BainPrintedSetTests" +
                            ".TheMovementRaise_IsTheTurnsAndReachesASecondMovementActionInIt"
                    }),

                // Tier III on the first branch, and the ONLY Green of his that carries no consequence
                // at all: its whole printed text is the Block's own condition, which is why its row
                // names neither a Defense-text application nor a marker reading.
                Face(BainCardContent.PerfectGetaway, "Perfect Getaway", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, PerfectGetawayJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisThreeBlocks_DeclareTheBoardConditionAndOnlyTwoCarryAConsequence",
                        "BainPrintedSetTests.TheBlockCondition_IsTrueOnlyWhileAHeroWearsHisMarker"
                    }),

                // Tier III on the second branch: Vantage Point's shape with one more space of raise.
                Face(BainCardContent.HighGround, "High Ground", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, HighGroundJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeMovementTextPause, HighGroundJourney),
                        Prove(ActiveEffectReading, HighGroundJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".HisTwoMovementFaces_GrantAnUnconditionalPermissionAndAGatedRaise"
                    }),

                // p.16's Ultimate. It prints no Initiative, no action and neither Range nor Radius, so
                // it carries no CommitAndReveal and no secondary at all - the whole of its printed
                // content is the permanent ability, which is why its row is the shortest in the table.
                Face(BainCardContent.AComplicatedProfession, "A Complicated Profession", new[]
                    {
                        Prove(HandCardSelection, UltimateJourney),
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests" +
                            ".AComplicatedProfession_WatchesTheMomentAMarkerIsGivenAndDiscardsFromTheRecipient"
                    }),

                // -----------------------------------------------------------------------------------
                // The components. These are the gate's own required shapes, whose subject is the
                // configured session and the board piece rather than anything his cards describe.
                // -----------------------------------------------------------------------------------

                Component("bain-hero-piece", "his token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, SpawnJourney),
                        Prove(HeroPiecePointerSelection, SpawnJourney),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, PresentationRebuildJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests.HisSet_IsNineteenFacesInThePrintedColourShape"
                    }),

                // Restore, Undo, replay and restart. The snapshot half is additionally driven INSIDE a
                // live resolution twice - once at Dead or Alive's standing give, and once at the pause
                // BETWEEN the hidden card choice and the reveal, which is the one place in this build
                // where a restore has to reconstruct an answer only one seat may see.
                Component("bain-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, SessionOperations),
                        Prove(SnapshotRestoreContinuation, GuessMatchJourney),
                        Prove(RetainedPrefixUndo, SessionOperations),
                        Prove(RetainedPrefixUndo, BountyGiveJourney),
                        Prove(ReplayReconstruction, SessionOperations),
                        Prove(RestartReconstruction, SessionOperations)
                    },
                    new[]
                    {
                        "BainGuessResolutionTests" +
                            ".TheSameAnswers_ReplayToTheSameFactsAndTheSameFinalState",
                        "BainMarkerMovementTests.EveryOperation_SurvivesASnapshotRoundTrip"
                    }),

                Component("bain-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests" +
                            ".EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests" +
                            ".NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // His bounty marker, which is the SECOND marker this build renders and the first with
                // no value channel at all - so the row a player reads is the caption alone. The
                // reading is proved on three boards rather than one, because his marker is the only
                // component in the build that MOVES between holders and comes back: the give puts it
                // on the enemy's rail, the transfer pulls it onto his own, and the return empties both.
                Component("bain-bounty-marker",
                    "the bounty marker, read on the hero wearing it",
                    new[]
                    {
                        Prove(HeldMarkerReading, BountyGiveJourney),
                        Prove(HeldMarkerReading, CloseCallJourney),
                        Prove(HeldMarkerReading, NarrowEscapeJourney)
                    },
                    new[]
                    {
                        "BainPrintedSetTests.TheGive_PutsTheMarkerOnTheChosenHeroAndReportsTheMoment",
                        "BainMarkerMovementTests.ATransfer_PublishesBothHeroes",
                        "MatchHudPresentationTests" +
                            ".AHeldMarker_IsDrawnOnItsHoldersRailFromTheProjectionAlone"
                    }),

                // He places NO token. His whole set moves one marker between heroes and never puts an
                // object on a hex, which the gate re-derives from his own content rather than taking
                // on this row's word.
                AbsentComponent("bain-tokens", "hero-owned tokens",
                    "Bain's nineteen faces place no token: his only rules component is the bounty " +
                    "marker above, which p.18 keeps off the board and this build draws as a rail " +
                    "caption on its holder."),

                // p.16's item track, on the terms every hero's item row states - and MEASURED rather
                // than claimed, because milestone 8.37's own item row is the recorded case of a gate
                // asserting an absence that was false. He prints TWELVE item icons covering all six of
                // p.16's kinds, which BA-06 identified against faces whose channel this build already
                // declares. The three Tier I faces print none.
                AbsentComponent("bain-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; his twelve item icons cover all " +
                    "six of p.16's kinds, pinned at the definition tier")
            });

        /// <summary>
        /// The capability identities that put a token on the board. A hero whose content names one of
        /// these has a component the matrix must have a row for, which is what holds the absence row
        /// above to the content rather than to its own say-so.
        /// </summary>
        private static readonly string[] TokenCapabilities =
        {
            "synthetic-place-token",
            "synthetic-reclaim-token",
            "tali-place-ice-token",
            "tali-place-ancestral-totem",
            "tali-place-venerated-totem"
        };

        /// <summary>The marker operations his content names, which the marker row above is about.</summary>
        private static readonly string[] MarkerCapabilities =
        {
            "bain-give-bounty-marker",
            "bain-transfer-bounty-marker",
            "bain-return-bounty-marker"
        };

        // -------------------------------------------------------------------------------------------
        // The guards. These are HeroPlayerPathMatrix's own, shared with the fourteen gates before this.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryBainFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesBainsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(BainPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(BainPlayerPathGateTests).Assembly);

        [Test]
        public void EveryBainJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(BainUiJourneyPlayModeTests),
                typeof(BainEngagedUiJourneyPlayModeTests),
                typeof(BainProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndBainActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "bain-items",
                BainCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void BainPrintsNoToken_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenCapabilities,
                "Bain's nineteen faces place no token");

        /// <summary>
        /// The marker row is an ordinary component row rather than a debt, so what it is held to is
        /// the same fact from the other side: his content really does move a marker, and the row that
        /// draws one is therefore about something. Milestone 8.23a's own sentence, applied to the
        /// second marker this build carries.
        /// </summary>
        [Test]
        public void TheBountyMarkerRow_HasItsSubject_BecauseHisContentActuallyMovesOne() =>
            Assert.That(
                MarkerCapabilities.Count(ContentNamesCapability),
                Is.EqualTo(MarkerCapabilities.Length),
                "The bounty row draws a marker his faces do not move, so the row is about nothing. " +
                "His set names the give, the transfer and the return, each on its own face.");

        /// <summary>
        /// Milestone 8.37's item row is the recorded case of a gate asserting an ABSENCE that was
        /// false - it claimed a hero printed no item icon and she printed twelve, and no offline tier
        /// could contradict it because an item icon introduces no SHAPE. This is the assertion that
        /// actually reads the content, written from BA-06's measured count rather than from the claim.
        /// </summary>
        [Test]
        public void HisTwelveItemIcons_CoverEveryKindPageSixteenHas() =>
            Assert.That(
                BainCardContent.AllDefinitions
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
                "His twelve item icons cover all six of p.16's kinds. Measured, not claimed.");

        /// <summary>
        /// The last guard, and the one the offline probe cannot answer: it needs the Edit Mode
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

        private static bool ContentNamesCapability(string capabilityId) =>
            BainCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(root => root != null)
                .SelectMany(root => root.Nodes)
                .Any(node => node.Capability.IsDefined &&
                    string.Equals(
                        node.Capability.CapabilityId.Value, capabilityId, StringComparison.Ordinal));

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
