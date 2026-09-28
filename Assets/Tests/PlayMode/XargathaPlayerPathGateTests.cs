using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Xargatha's half of the permanent hero player-path completion gate. Milestone 8.24, and the
    /// sixth and last hero of the mapped pitch roster to take it.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with <see cref="ArienPlayerPathGateTests"/>,
    /// <see cref="WaspPlayerPathGateTests"/>, <see cref="BroganPlayerPathGateTests"/>,
    /// <see cref="SabinaPlayerPathGateTests"/> and <see cref="TigerclawPlayerPathGateTests"/>; the
    /// table below is Xargatha's own. This milestone added one interaction shape, five capability
    /// classifications and one derivation refinement - and the offline derivation printed every Wasp,
    /// Arien, Brogan, Sabina and Tigerclaw row byte-identical afterwards, which is the test milestone
    /// 8.20b set for whether an addition is a generalisation or a re-fit.</para>
    ///
    /// <para><b>The new shape is <see cref="ActionTextPlacementChoice"/>.</b> Her three Green
    /// Movements are the first verified faces to say otherwise to rulebook p.11's before-movement
    /// default, and what a player meets is a question with two answers and no way to decline. The
    /// refinement is what stopped it being read as p.10's modal: a <c>Choose</c> is classified by what
    /// its branches are, so a Choose between two placements is the placement decision and a Choose
    /// between printed bullets stays the modal. No earlier hero's content names
    /// <c>around-primary-action</c>, so no earlier row could move.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="XargathaCardContent"/> - action kind, origin, printed range, card-text timing,
    /// effect node kind, failure policy, capability identity, progression profile - and the guard
    /// compares the derivation against the row exactly. A row may claim neither less nor more than its
    /// printed face introduces, and a shape cannot be declared without naming the journey that drives
    /// it.</para>
    ///
    /// <para><b>Her five scaled values ask a player for nothing, and that is why those rows are
    /// short.</b> "+1 Attack for each other enemy unit adjacent to you" is a value inside p.14's own
    /// total rather than a step, which the card marks with the <c>+</c> on the number itself - the
    /// same reading Sabina's three conditional Attacks and Tigerclaw's two Backstabs got. What a
    /// player does on Dangerous Slash is choose a target adjacent to her; what the count does is
    /// happen.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class XargathaPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names. Declared as constants so a row and the method it names cannot drift apart
        // silently in one direction only - a rename that misses one end fails
        // MatrixNamesOnlyJourneysThatExist rather than quietly proving nothing.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The one journey that commits a card through the real tray controls and carries it through
        /// p.7's reveal, which is what every face's commitment row rests on. One tray journey serves
        /// all eighteen dealt faces for the reason milestone 8.20b's own fixture header records: it is
        /// the same input whichever face is picked up, and the engaged and progressed journeys commit
        /// headlessly by design so their own subjects are the cards rather than the commitment.
        /// </summary>
        private const string TrayCommitAndWalk =
            "Xargatha_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnXargathasOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesXargathasCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryXargathaFaceAtTheStandardCardFootprint";

        private const string CleaveJourney =
            "Cleave_AttacksAnAdjacentMinionAndIsOfferedItsRepeatOnAHero";

        private const string RepeatSurvivesUndoAndRestore =
            "CleavesRepeat_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string SirensCallJourney =
            "SirensCall_OffersOnlyDistantEnemiesAndDrawsTheNamedOneToHerSide";

        private const string StoneGazeJourney =
            "StoneGaze_IsDormantOnTheTurnItIsPlayedAndStandsOnHerNext";

        // Milestone 8.117: the pre-battle question both end-of-round Defeats' trigger bodies ask. The
        // staging's adjacent enemy minion is Ranged, which Final Embrace admits; Constrict's body is the
        // same composed node table with the class narrowed to Melee.
        private const string PreBattleDefeatJourney = nameof(XargathaProgressedUiJourneyPlayModeTests
            .FinalEmbrace_AsksHerAtThePreBattleMomentWhichAdjacentMinionFalls);

        private const string CharmJourney =
            "Charm_AsksWhichSideOfHerMovementItRunsOnAndWalksTheRangedMinion";

        private const string NumericDefense =
            "SirensCall_IsOfferedForItsPrintedDefenseThreeAndXargathaSurvivesTheAttack";

        private const string DefeatAndRespawn =
            "Xargatha_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        private const string RangedAttackJourney =
            "LongThrust_ReachesTheHeroOnlyBecauseAnEnemyStandsBesideHer";

        private const string RetrievalJourney =
            "FreshConverts_TakesBackTheCardSheSpentDefendingOnItsOwnFace";

        private const string ControlJourney =
            "Control_ReachesTheMeleeMinionsCharmRefuses";

        private const string UltimateJourney =
            "Metamorphosis_IsDrawnOnHerRailAndWalksHerFurtherThanHerCardPrints";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_XARGATHA.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Xargatha",
            XargathaCardContent.AllDefinitions,
            XargathaCardContent.ReturnedToBoxAtSetup,
            XargathaActiveEffectContent.Handlers(),
            new[]
            {
                XargathaActiveEffectContent.ConstrictTriggeredEffect.Definition,
                XargathaActiveEffectContent.FinalEmbraceTriggeredEffect.Definition
            },
            new[]
            {
                // Her Gold basic, and the roster's first Attack whose repeat narrows the CLASS of unit
                // it may take: the printed instance targets "a unit", the repeated one "a different
                // enemy hero". The parenthesis is p.9 reminder text, so the row carries no extra
                // question for it - what it states is why the first target being a minion does not
                // spend the repeat.
                Face(XargathaCardContent.Cleave, "Cleave", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CleaveJourney),
                        Prove(AfterAttackTextPause, CleaveJourney),
                        Prove(ConfirmOrDecline, CleaveJourney),
                        Prove(RepeatDecision, CleaveJourney),
                        Prove(RepeatDecision, RepeatSurvivesUndoAndRestore),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests" +
                            ".CleaveAndRapidThrusts_RepeatOnADifferentEnemyHero"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(XargathaCardContent.DesperateCleave, "Desperate Cleave",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its text composition carries a named deferral besides"),

                // Her Silver basic and only Ranged Skill. Its left-ribbon shield is Defense 3; the
                // lower-right 3 belongs to the primary's Range, and the face prints no Movement. Its
                // text names a unit and then a space, so a player answers it twice on the board.
                Face(XargathaCardContent.SirensCall, "Siren's Call", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SirensCallJourney),
                        Prove(BoardUnitSelection, SirensCallJourney),
                        Prove(BoardSpaceSelection, SirensCallJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".SirensCall_OffersOnlyEnemiesInRangeThatAreNotAlreadyAdjacent",
                        "XargathaPrintedSetTests.SirensCall_MovesTheNamedUnitToASpaceBesideHer"
                    }),

                // The three melee scaled Attacks. Their `+N for each other enemy unit adjacent to you`
                // is a value inside p.14's total rather than a step, so what a player does is choose a
                // target adjacent to her - one shape on three faces, exactly as the gate's own
                // sentence about permutations allows.
                Face(XargathaCardContent.DangerousSlash, "Dangerous Slash", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CleaveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".DangerousSlash_CountsEveryOtherAdjacentEnemy_AndNotItsTarget",
                        "XargathaPrintedSetTests" +
                            ".DangerousSlash_WithNoOtherEnemyBesideHer_IsItsPrintedNumber"
                    }),

                Face(XargathaCardContent.DeadlySwipe, "Deadly Swipe", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CleaveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".DeadlySwipeAndLethalSpin_ScaleTheSameCountByTheirOwnAmounts"
                    }),

                // The first of the two Ranged Attacks, and the first face in this build whose printed
                // RANGE is counted from the board rather than printed alone. A player meets that as a
                // wider set of pieces to click, which is why the row's shape is the ranged target and
                // nothing beside it.
                Face(XargathaCardContent.LongThrust, "Long Thrust", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".LongThrust_CountsEveryAdjacentEnemy_IncludingOneItCouldTarget",
                        "XargathaVerifiedContentTests.TheFiveScaledValues_ReadTheQueryTheirWordingNames"
                    }),

                Face(XargathaCardContent.LethalSpin, "Lethal Spin", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CleaveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".DeadlySwipeAndLethalSpin_ScaleTheSameCountByTheirOwnAmounts"
                    }),

                // The one face carrying both halves at once: a Range counted from the board and a
                // repeat that recalculates it. Both of its shapes are already driven - the ranged
                // target on Long Thrust and the repeat on Cleave - and what is face-specific about it,
                // that the second instance re-reads the Range, is authoritative and linked here.
                Face(XargathaCardContent.RapidThrusts, "Rapid Thrusts", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(AfterAttackTextPause, CleaveJourney),
                        Prove(ConfirmOrDecline, CleaveJourney),
                        Prove(RepeatDecision, CleaveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests" +
                            ".CleaveAndRapidThrusts_RepeatOnADifferentEnemyHero"
                    }),

                // The Blue petrification line. One printed sentence and two declarations, and a player
                // meets both of them as readings rather than inputs: the disk standing on her tray,
                // and the greyed Movement control the enemy inside it is shown. It is the one effect
                // of hers that a player meets on a turn where nothing has just happened, which is what
                // the round-long reading is about.
                Face(XargathaCardContent.StoneGaze, "Stone Gaze", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StoneGazeJourney),
                        Prove(ActiveEffectReading, StoneGazeJourney),
                        Prove(OpponentRestrictionReading, StoneGazeJourney),
                        Prove(RoundLongActiveEffectReading, StoneGazeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".StoneGaze_RaisesItsEffectAndDoesNothingElseWhenItResolves",
                        "XargathaVerifiedContentTests.HerFiveActiveEffects_DeclareWhatTheirFacesPrint"
                    }),

                Face(XargathaCardContent.PetrifyingStare, "Petrifying Stare", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StoneGazeJourney),
                        Prove(ActiveEffectReading, StoneGazeJourney),
                        Prove(OpponentRestrictionReading, StoneGazeJourney),
                        Prove(RoundLongActiveEffectReading, StoneGazeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests.HerFiveActiveEffects_DeclareWhatTheirFacesPrint"
                    }),

                // The two retrievals. Each is one condition the board answers, one printed `may` on
                // its own control, and one card taken back on its whole face under milestone 8.15a's
                // contract - and what separates them is only which enemy pieces answer the condition,
                // which changes no input the player makes.
                Face(XargathaCardContent.FreshConverts, "Fresh Converts", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalJourney),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(HandCardSelection, RetrievalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".FreshConverts_BesideAnEnemyMinion_OffersTheRetrieveAndMayDeclineIt",
                        "XargathaPrintedSetTests" +
                            ".FreshConverts_WithNoEnemyMinionBesideHer_OffersNothing"
                    }),

                Face(XargathaCardContent.TurnIntoStatues, "Turn Into Statues", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StoneGazeJourney),
                        Prove(ActiveEffectReading, StoneGazeJourney),
                        Prove(OpponentRestrictionReading, StoneGazeJourney),
                        Prove(RoundLongActiveEffectReading, StoneGazeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".TurnIntoStatues_RaisesTheSameEffectFromTheTierThreeCard"
                    }),

                Face(XargathaCardContent.DevotedFollowers, "Devoted Followers", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalJourney),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(HandCardSelection, RetrievalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory"
                    }),

                // The three Green Movement primaries, and the milestone's own shape. They print no
                // secondary Movement icon - a Movement card's walk IS its primary - so these rows
                // carry the primary destination where every other face of hers carries the secondary
                // one. The placement question comes first, the printed `may` behind it, then the
                // minion and its space.
                Face(XargathaCardContent.Charm, "Charm", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(ActionTextPlacementChoice, CharmJourney),
                        Prove(ConfirmOrDecline, CharmJourney),
                        Prove(BoardUnitSelection, ControlJourney),
                        Prove(BoardSpaceSelection, ControlJourney),
                        Prove(PrimaryMovementDestination, ControlJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests.Charm_AsksWhichSideOfHerMovementFirst",
                        "XargathaPrintedSetTests.Charm_OffersARangedMinionAndNotAMeleeOne",
                        "XargathaVerifiedContentTests.TheThreeGreenMovements_PlaceTheirTextAroundTheAction"
                    }),

                Face(XargathaCardContent.Control, "Control", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(ActionTextPlacementChoice, ControlJourney),
                        Prove(ConfirmOrDecline, ControlJourney),
                        Prove(BoardUnitSelection, ControlJourney),
                        Prove(BoardSpaceSelection, ControlJourney),
                        Prove(PrimaryMovementDestination, ControlJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests.Control_AdmitsTheMeleeMinionCharmRefuses"
                    }),

                // The two end-of-round Defeats. What a player does is play the card; what it leaves
                // behind is a source standing until p.8, which is a reading rather than an input - and
                // the same two readings the petrification line carries, because a standing effect of
                // hers is met the same way whichever sentence raised it.
                Face(XargathaCardContent.Constrict, "Constrict", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StoneGazeJourney),
                        Prove(ActiveEffectReading, StoneGazeJourney),
                        Prove(RoundLongActiveEffectReading, StoneGazeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(BoardUnitSelection, PreBattleDefeatJourney)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests" +
                            ".Constrict_DefeatsAnAdjacentMeleeMinionAtTheEndOfTheRound",
                        "XargathaPrintedSetTests.Constrict_LeavesAnAdjacentRangedMinionStanding"
                    }),

                Face(XargathaCardContent.Dominate, "Dominate", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(ActionTextPlacementChoice, ControlJourney),
                        Prove(ConfirmOrDecline, ControlJourney),
                        Prove(BoardUnitSelection, ControlJourney),
                        Prove(BoardSpaceSelection, ControlJourney),
                        Prove(PrimaryMovementDestination, ControlJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests.TheThreeGreenMovements_PlaceTheirTextAroundTheAction",
                        "XargathaVerifiedContentTests" +
                            ".EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                Face(XargathaCardContent.FinalEmbrace, "Final Embrace", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StoneGazeJourney),
                        Prove(ActiveEffectReading, StoneGazeJourney),
                        Prove(RoundLongActiveEffectReading, StoneGazeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(BoardUnitSelection, PreBattleDefeatJourney)
                    },
                    new[]
                    {
                        "XargathaPrintedSetTests.FinalEmbrace_DefeatsTheRangedMinionConstrictLeaves"
                    }),

                // p.16's Ultimate, and the first in this build that asks nothing and waits for
                // nothing: a standing declaration rather than a trigger. So this row carries the rail
                // reading alone - there is no trigger pause to answer, and the three rules it states
                // are read off an ordinary turn instead.
                Face(XargathaCardContent.Metamorphosis, "Metamorphosis", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests.Metamorphosis_IsAStandingDeclarationOfThreeRules",
                        "XargathaPrintedSetTests" +
                            ".Metamorphosis_ScalesBothChannelsWithAdjacentEnemies_WithNothingPlayed",
                        "XargathaPrintedSetTests.Metamorphosis_RecalculatesWhenTheBoardChanges"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                // The piece. Every composition that deals Xargatha restages her into the engagement,
                // so the spawn half of this row is proved the two ways that staging leaves open: the
                // piece carries the map's own declared Blue spawn point and stands where the
                // configured setup says, and p.13's respawn puts her back in that spawn area from a
                // board click.
                Component("xargatha-hero-piece", "the official circular Blue hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "XargathaVerifiedContentTests" +
                            ".TwoSeatsDealingXargatha_GetDistinctInstanceIdentities"
                    }),

                // Restore, Undo, replay and restart, proved across her printed repeat - the one
                // decision of hers a player is asked after the thing it is about has already
                // happened, and therefore the pause whose re-derivation has something to be wrong
                // about.
                Component("xargatha-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, RepeatSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, RepeatSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, RepeatSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "XargathaVerifiedContentTests.TheDealtCards_PassTheInitialSetupValidator" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. It is required of every hero, and hers is a real test of it rather than a
                // formality: three faces carry a parenthesised reminder, and Stone Gaze's rules panel
                // is the longest in the roster - a bolded keyword, two clauses and a parenthesis
                // carrying an internal quotation. A hero added after the footprint became a standard
                // has to fit it rather than widen it.
                Component("xargatha-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests.EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests.NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item is gained from the sibling p.15's Level Up did not take. No journey
                // reaches it, for the reason milestones 8.20a, 8.20b, 8.21, 8.22 and 8.23 recorded
                // against the identical rows the other five heroes owe: the item track is derived
                // progression state, `DEV-004` composes card locations only, and no scene content
                // stages a position where a Level Up is offered. This is a debt rather than a gap -
                // the row names it, and the guard below holds it to Xargatha actually printing item
                // icons.
                AbsentComponent("xargatha-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; her twelve item icons " +
                    "are pinned face by face by XargathaVerifiedContentTests." +
                    "EveryPrintedFace_MatchesTheSourceInventory"),

                // She owns no token, marker, alternate form or statue, and that is content rather
                // than an omission: her petrification turns enemy heroes into terrain where they
                // stand, which is a rule about the pieces already on the board and not a component
                // put beside them.
                AbsentComponent("xargatha-tokens-and-markers", "hero-owned tokens and markers",
                    "Xargatha's nineteen faces place no token and give no marker; her petrification " +
                    "changes what an enemy hero already standing on the board counts as, and " +
                    "materialises no statue to stand in their place")
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
        public void EveryXargathaFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesXargathasPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(XargathaPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(XargathaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryXargathaJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(XargathaUiJourneyPlayModeTests),
                typeof(XargathaEngagedUiJourneyPlayModeTests),
                typeof(XargathaProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndXargathaActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "xargatha-items",
                XargathaCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void XargathaPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Xargatha's nineteen faces place no token and give no marker");

        /// <summary>
        /// Every row's authoritative scenarios name a fixture the Edit Mode assembly actually has. The
        /// gate asks each row to "link its source-backed authoritative scenarios", and a link that
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
