using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tigerclaw's half of the permanent hero player-path completion gate. Milestone 8.23.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with <see cref="ArienPlayerPathGateTests"/>,
    /// <see cref="WaspPlayerPathGateTests"/>, <see cref="BroganPlayerPathGateTests"/> and
    /// <see cref="SabinaPlayerPathGateTests"/>; the table below is Tigerclaw's own. This milestone
    /// added one interaction shape and six capability classifications, and no derivation rule at all -
    /// the offline derivation printed every Wasp, Arien, Brogan and Sabina row byte-identical
    /// afterwards, which is the test milestone 8.20b set for whether an addition is a generalisation
    /// or a re-fit.</para>
    ///
    /// <para><b>The new shape is <see cref="ResourceAmountChoice"/>.</b> His three thieving Greens are
    /// the first printed content to move rulebook p.15's coins, and how many is a control the player
    /// presses - one number on Light-Fingered and Pick Pocket, two on Master Thief. It is separate
    /// from <see cref="PushDistanceChoice"/> because the two choose different things; the reasoning is
    /// on the member.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="TigerclawCardContent"/> - action kind, origin, printed range, card-text timing,
    /// effect node kind, failure policy, capability identity, progression profile - and
    /// <see cref="MatrixRowsDeclareExactlyTheShapesTigerclawsPrintedContentIntroduces"/> compares the
    /// two. A row cannot claim less than its face asks for, cannot claim more, and cannot name a shape
    /// without naming the journey where a player performs it.</para>
    ///
    /// <para><b>Two of his faces print card text at both of p.14's attacker moments</b> - Combat
    /// Reflexes and Leaping Strike - and their rows carry one card-text pause each rather than two,
    /// because a pause is a shape and both of theirs is the same one: a printed <c>you may</c> move,
    /// answered on the board or declined. What is face-specific about them - that taking the first
    /// closes the second - is authoritative, and the column links it.</para>
    ///
    /// <para><b>His two Backstabs ask a player for less than their sentences suggest.</b> "If a
    /// friendly unit is adjacent to the target, +2 Attack" is a value rather than a step, which
    /// rulebook p.14 marks with the <c>+</c> on the number itself, so those rows carry no card-text
    /// pause and no selection. Milestone 8.22's three conditional Attacks of Sabina's are the same
    /// reading.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class TigerclawPlayerPathGateTests
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
            "Tigerclaw_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnTigerclawsOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesTigerclawsCommitmentAndRestartsHimOntoHisStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryTigerclawFaceAtTheStandardCardFootprint";

        private const string BlinkStrikeJourney =
            "BlinkStrike_CrossesTheEnemyItsBoardClickNamesAndAttacksThatUnit";

        private const string HitAndRunJourney =
            "HitAndRun_AttacksAndThenTakesItsPrintedStepOnItsOwnControls";

        private const string BlendJourney =
            "BlendIntoShadows_PlacesHimInsideItsRadiusAndDrawsTheActiveEffectItRaises";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndTigerclawSurvivesTheAttack";

        private const string BlockJourney =
            "Dodge_IsOfferedAsAWholeBlockFace_AndItsUnmetConditionDefeatsHim";

        private const string DefeatAndRespawn =
            "Tigerclaw_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick";

        private const string TheftJourney =
            "LightFingered_TakesTheEnemyHerosCoinOnItsOwnAmountControlAndThenSteps";

        private const string TheftSurvivesUndoAndRestore =
            "TheTheftsAmount_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string CombatReflexesJourney =
            "CombatReflexes_DeclinesItsFirstMoveAndTakesTheSecondAfterTheAttack";

        private const string PoisonJourney =
            "PoisonedDagger_GivesThePoisonMarkerToTheHeroItsBoardClickNames";

        /// <summary>Milestone 8.23a: the marker is read on the rail of the hero who is holding it.</summary>
        private const string PoisonReadingJourney =
            "APoisonedHero_ReadsTheMarkerAndItsThreeLostValuesOnTheirOwnRail";

        /// <summary>
        /// and the reading follows the attachment's lifetime through every reconstruction the gate
        /// names, ending when p.8's end of round returns the marker.
        /// </summary>
        private const string PoisonReadingSurvivesJourney =
            "ThePoisonReading_SurvivesRestoreReplayAndUndo_AndEndsWhenTheRoundReturnsTheMarker";

        private const string DefenseRiderJourney =
            "Parry_BlocksTheAttackItIsPrintedAgainstAndItsRiderTakesTheAttackersCard";

        private const string SidestepJourney =
            "Sidestep_StepsOneSpaceOnItsOwnDefenseRiderAfterTheBlockIsAnswered";

        private const string EvadeJourney =
            "Evade_SpendsACardAndThenStepsOnItsOwnDefenseRider";

        private const string RangedAttackJourney =
            "BackstabWithABallista_ShootsTheEnemyMinionAtItsPrintedRange";

        private const string UltimateJourney =
            "CloakAndDaggers_IsDrawnOnTigerclawsRailAndRepeatsTheBasicActionHeJustTook";

        /// <summary>
        /// Milestone 8.25a. The repeat decision has two answers and one journey can only give one, so
        /// the shape that is about what taking it *does* rests on the journey that takes it. The one
        /// above declines, which is what proves the decline is accepted.
        /// </summary>
        private const string TakenRepeatJourney =
            "CloakAndDaggers_TakenRepeatRunsHisBasicActionAgainOnTheSameCard";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_TIGERCLAW.md's inventory table, which is in
        // the same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Tigerclaw",
            TigerclawCardContent.AllDefinitions,
            TigerclawCardContent.ReturnedToBoxAtSetup,
            TigerclawActiveEffectContent.Handlers(),
            new[]
            {
                TigerclawEffectContent.CloakAndDaggersTriggeredEffect(
                    TigerclawCardContent.ContentRevision)
            },
            new[]
            {
                // The Red basic. Its step-1 line is one board click that answers two printed clauses
                // at once - which enemy to cross, and therefore where he lands - so the row carries a
                // unit selection and no space selection beside it: the destination is published by
                // the geometry rather than asked about.
                Face(TigerclawCardContent.BlinkStrike, "Blink Strike", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, BlinkStrikeJourney),
                        Prove(BeforeAttackTextPause, BlinkStrikeJourney),
                        Prove(BoardUnitSelection, BlinkStrikeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".BlinkStrike_OffersOnlyACrossableEnemy_AndLandsBeyondIt",
                        "TigerclawPrintedSetTests" +
                            ".BlinkStrike_WithAnOccupiedEndSpace_IsUnavailable",
                        "TigerclawPrintedSetTests.BlinkStrike_WithNoCrossableEnemy_IsUnavailable"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player
                // match, so no player of this build can reach it; the guard checks that claim against
                // the content rather than taking the row's word for it.
                UnreachableFace(TigerclawCardContent.NimbleStrike, "Nimble Strike",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its text composition carries a named deferral besides"),

                // The Silver basic. Its Place is a space inside the printed radius, and what it raises
                // is an Active effect standing into the next turn - two readings rather than one,
                // because a player meets the second where nothing on screen has just happened. Its one
                // secondary ribbon icon is a Defense value (2), not a Movement icon: milestone 8.35z20
                // corrected the one-icon slot-position trap this face's row previously read wrong (a
                // lone secondary icon sits in the ribbon's lower slot whatever its type - see
                // Docs/AI/Rules/HERO_TIGERCLAW.md's settled-corrections block), so he is the one
                // non-Ultimate face with no secondary Movement - the same shape every other verified
                // hero's Silver basic already has.
                Face(TigerclawCardContent.BlendIntoShadows, "Blend Into Shadows", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BlendJourney),
                        Prove(BoardSpaceSelection, BlendJourney),
                        Prove(ActiveEffectReading, BlendJourney),
                        Prove(RoundLongActiveEffectReading, BlendJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".BlendIntoShadows_AwayFromTerrain_PlacesNobodyAndRaisesNoImmunity",
                        "TigerclawVerifiedContentTests" +
                            ".EveryBlueCardIsABlock_AndEveryNonUltimateFacePrintsMovement"
                    }),

                // The Green basic. "After the attack: You may move 1 space" is p.14's step 7, and the
                // player answers it with the board or with the decline milestone 8.20a made an answer
                // that has to be offered by name.
                Face(TigerclawCardContent.HitAndRun, "Hit and Run", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, HitAndRunJourney),
                        Prove(AfterAttackTextPause, HitAndRunJourney),
                        Prove(ConfirmOrDecline, HitAndRunJourney),
                        Prove(BoardSpaceSelection, HitAndRunJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".TwoFacesPrintCardTextAtBothOfPageFourteensAttackerMoments"
                    }),

                // The first of the two faces printing both attacker moments. One pause shape covers
                // both: the same printed `you may` move, offered at step 1 and again at step 7.
                //
                // Milestone 8.35z25 added BoardUnitSelection. The face prints its move
                // before its targeting, so the target is named by the card's own text - a board
                // click on the unit - rather than by p.14's ordinary question, which is exactly the
                // pair of shapes the Theft cards already declare together.
                Face(TigerclawCardContent.CombatReflexes, "Combat Reflexes", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CombatReflexesJourney),
                        Prove(BeforeAttackTextPause, CombatReflexesJourney),
                        // Milestone 8.35z49. This row has named p.14's step-1 pause since milestone
                        // 8.23 and not its step-7 one, though the face prints both - because
                        // HeroPlayerPathMatrix read an action's FIRST card-text body only and never its
                        // second. The derivation reads both now, and the evidence this row needed was
                        // already written: the journey named here is
                        // `CombatReflexes_DeclinesItsFirstMoveAndTakesTheSecondAfterTheAttack`, which
                        // takes the step-7 move and says so in its own name. Nothing about the face
                        // changed; the table stopped under-claiming it.
                        Prove(AfterAttackTextPause, CombatReflexesJourney),
                        Prove(ConfirmOrDecline, CombatReflexesJourney),
                        Prove(BoardUnitSelection, CombatReflexesJourney),
                        Prove(BoardSpaceSelection, CombatReflexesJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".CombatReflexes_DecliningTheFirstMove_OpensTheSecond",
                        "TigerclawPrintedSetTests" +
                            ".CombatReflexes_TakingTheFirstMove_ClosesTheSecond"
                    }),

                // The Tier II conditional Attack. Its `+2` is a value inside p.14's own total rather
                // than a step, so what a player does is choose a target adjacent to him.
                Face(TigerclawCardContent.Backstab, "Backstab", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, BlinkStrikeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".Backstab_RaisesItsAttackOnlyWhenAFriendlyUnitStandsBesideTheTarget",
                        "TigerclawPrintedSetTests" +
                            ".Backstab_DoesNotCountTigerclawHimselfAsTheFriendlyUnit"
                    }),

                // The Tier III sibling of Combat Reflexes: both moments again, and both unconditional
                // this time. Its printed difference is which of the two `you may` steps is gated,
                // which is not a shape - a player answers both the same way.
                Face(TigerclawCardContent.LeapingStrike, "Leaping Strike", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, CombatReflexesJourney),
                        Prove(BeforeAttackTextPause, CombatReflexesJourney),
                        // Milestone 8.35z49, for Combat Reflexes' reason exactly: the face prints both
                        // of p.14's attacker moments and the derivation now reads both bodies rather
                        // than the first alone.
                        Prove(AfterAttackTextPause, CombatReflexesJourney),
                        Prove(ConfirmOrDecline, CombatReflexesJourney),
                        // Milestone 8.35z25, for Combat Reflexes' reason exactly: it
                        // prints the identical step-1 body and now names its target through it.
                        Prove(BoardUnitSelection, CombatReflexesJourney),
                        Prove(BoardSpaceSelection, CombatReflexesJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".TwoFacesPrintCardTextAtBothOfPageFourteensAttackerMoments"
                    }),

                // His only Ranged Attack and his only face printing a Range icon. Its second clause
                // takes a Defense away from the target rather than asking its own player anything.
                Face(TigerclawCardContent.BackstabWithABallista, "Backstab with a Ballista", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".BackstabWithABallista_ForbidsAPrimaryDefense_AndLeavesTheSecondaryOne",
                        "TigerclawVerifiedContentTests" +
                            ".TwoBackstabsRaiseTheirAttack_AndOnlyTheBallistaForbidsAPrimaryDefense"
                    }),

                // The Blue Tier I Block, and the plainest of his five: a whole face offered in p.14's
                // Defense prompt, succeeding outright against the attack its printed condition names.
                Face(TigerclawCardContent.Dodge, "Dodge", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, BlockJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne"
                    }),

                // The Blocks that print a rider. Sidestep's is a step the defender may take or
                // decline; Parry's and Riposte's spend a card from the defender's own hand; Evade
                // prints both. Each is p.14's step 4 - card text the DEFENDER applies - which is why
                // every one of these rows carries DefenseTextApplication beside its Block.
                Face(TigerclawCardContent.Sidestep, "Sidestep", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, SidestepJourney),
                        Prove(DefenseTextApplication, SidestepJourney),
                        Prove(ConfirmOrDecline, SidestepJourney),
                        Prove(BoardSpaceSelection, SidestepJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne"
                    }),

                Face(TigerclawCardContent.Parry, "Parry", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, DefenseRiderJourney),
                        Prove(DefenseTextApplication, DefenseRiderJourney),
                        Prove(HandCardSelection, DefenseRiderJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne"
                    }),

                Face(TigerclawCardContent.Evade, "Evade", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, EvadeJourney),
                        Prove(DefenseTextApplication, EvadeJourney),
                        Prove(ConfirmOrDecline, EvadeJourney),
                        Prove(BoardSpaceSelection, EvadeJourney),
                        Prove(HandCardSelection, EvadeJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne"
                    }),

                Face(TigerclawCardContent.Riposte, "Riposte", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(BlockDefenseOffer, DefenseRiderJourney),
                        Prove(DefenseTextApplication, DefenseRiderJourney),
                        Prove(HandCardSelection, DefenseRiderJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne"
                    }),

                // The three thieving Greens. Each is one board click naming whose pile to take from,
                // one amount control saying how much, and a step behind the semicolon the player may
                // take or decline. Master Thief prints two amounts where the other two print one,
                // which is the same control with a second number on it.
                Face(TigerclawCardContent.LightFingered, "Light-Fingered", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, TheftJourney),
                        Prove(BoardUnitSelection, TheftJourney),
                        Prove(ResourceAmountChoice, TheftJourney),
                        Prove(ConfirmOrDecline, TheftJourney),
                        Prove(BoardSpaceSelection, TheftJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".LightFingered_WithNobodyToStealFrom_StopsAtThatStep",
                        "TigerclawPrintedSetTests" +
                            ".LightFingered_AgainstAnEmptyPile_TakesNothingAndSkipsWhatFollows"
                    }),

                Face(TigerclawCardContent.PickPocket, "Pick Pocket", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, TheftJourney),
                        Prove(BoardUnitSelection, TheftJourney),
                        Prove(ResourceAmountChoice, TheftJourney),
                        Prove(ConfirmOrDecline, TheftJourney),
                        Prove(BoardSpaceSelection, TheftJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests.MasterThief_DeclaresTheTwoAmountsItsFacePrints"
                    }),

                // The two Poison faces. What a player does is name the hero in range; what the marker
                // then does to that hero is read on their own rail, which milestone 8.23a added and
                // which the component row below owns. Both halves are this face's, so the face
                // declares the reading its capability introduces as well as the click.
                Face(TigerclawCardContent.PoisonedDagger, "Poisoned Dagger", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, PoisonJourney),
                        Prove(BoardUnitSelection, PoisonJourney),
                        Prove(HeldMarkerReading, PoisonReadingJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests" +
                            ".PoisonedDagger_GivesTheMarker_AndTheHolderLosesThreeValues",
                        "TigerclawPrintedSetTests" +
                            ".TheFriendlyHalfOfTheMarkerSelection_IsUnreachableOnAOneVersusOneBoard"
                    }),

                // MILESTONE 8.83 DROPPED ONE SHAPE FROM THIS ROW AND FROM NO OTHER, and the drop is
                // the correction rather than a loss of coverage. Master Thief ends "you may move UP
                // TO 2 spaces" - p.11's optional DISTANCE - and its follow-up step declared `may` at
                // an exact 2, which is p.11's optional STEP and a confirm-or-decline beside the board.
                // Under `up to` the same handler expresses the optionality INSIDE its own offer: the
                // hero's own space is one of the destinations, so walking away is a board click and
                // not a button. Light-Fingered and Pick Pocket print "you may move 1 SPACE" and still
                // declare `may`, which is why their rows still carry the shape.
                Face(TigerclawCardContent.MasterThief, "Master Thief", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, TheftJourney),
                        Prove(BoardUnitSelection, TheftJourney),
                        Prove(ResourceAmountChoice, TheftJourney),
                        Prove(BoardSpaceSelection, TheftJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests.MasterThief_DeclaresTheTwoAmountsItsFacePrints"
                    }),

                Face(TigerclawCardContent.PoisonedDart, "Poisoned Dart", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, PoisonJourney),
                        Prove(BoardUnitSelection, PoisonJourney),
                        Prove(HeldMarkerReading, PoisonReadingJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests.PoisonedDart_IsTheSameSentenceAtTwice_TheStrength",
                        "TigerclawPrintedSetTests" +
                            ".PoisonedDagger_GivesTheMarker_AndTheHolderLosesThreeValues"
                    }),

                // p.16's Ultimate. It prints no action, so it has no commitment row; what it prints is
                // one thing that fires - p.10's Repeat, reached from outside the action it repeats -
                // and the player answers it as a decision carrying both a confirm and a decline.
                Face(TigerclawCardContent.CloakAndDaggers, "Cloak and Daggers", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney),
                        Prove(ConfirmOrDecline, UltimateJourney),
                        Prove(RepeatDecision, TakenRepeatJourney)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                // The piece. Every composition that deals Tigerclaw restages him into the engagement,
                // so the spawn half of this row is proved the two ways the staging leaves open: the
                // piece carries the map's own declared Blue spawn point and stands where the
                // configured setup says, and p.13's respawn puts him back on that spawn from a board
                // click.
                Component("tigerclaw-hero-piece", "the official circular Blue hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "TigerclawVerifiedContentTests" +
                            ".TwoSeatsDealingTigerclaw_GetDistinctInstanceIdentities"
                    }),

                // Restore, Undo, replay and restart, proved across the theft's amount control - the
                // one decision of his that a player answers with a number, and therefore the pause
                // whose re-derivation after a restore has something to be wrong about.
                Component("tigerclaw-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, TheftSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, TheftSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, TheftSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "TigerclawVerifiedContentTests.TheDealtCards_PassTheInitialSetupValidator" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. It is required of every hero: one whose longest sentence does not fit is
                // exactly the case that must fail here rather than widen it. Tigerclaw's Blink Strike
                // carries a parenthesised reminder and Backstab a parenthesised definition.
                Component("tigerclaw-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests.EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests.NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item is gained from the sibling p.15's Level Up did not take. No journey
                // reaches it, for the reason milestones 8.20a, 8.20b, 8.21 and 8.22 recorded against
                // the identical rows the other four heroes owe: the item track is derived progression
                // state, `DEV-004` composes card locations only, and no scene content stages a
                // position where a Level Up is offered.
                AbsentComponent("tigerclaw-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; his twelve item icons and the " +
                    "four dashboard slots they reach are pinned by TigerclawVerifiedContentTests." +
                    "HisTwelveItemIconsReachAllFourDashboardSlots_WithTheWidestMovementReach"),

                // THE POISON MARKER, DRAWN AT LAST. Milestone 8.23a closed the debt this row carried.
                //
                // Milestone 8.23 recorded an absence here, and recorded it honestly: he was the first
                // verified hero to attach one of p.19's markers, this build drew no held marker
                // anywhere, and Poisoned Dagger and Poisoned Dart lowered a hero's Initiative, Attack
                // and Defense with nothing on screen saying so. What closes it is a reading rather
                // than an excuse, so the row is now an ordinary component row with journeys.
                //
                // It is a component rather than a face row for the reason the coin balance and the
                // footprint are: the subject is the hero holding the marker, not the card that gave
                // it. The two faces above still carry their own click - the hero who receives one is
                // named by the selection node in front of them - and this row carries what the click
                // leaves behind.
                //
                // The reading is proved on WASP'S rail, which is the point of the milestone rather
                // than an accident of the staging: p.18 gives markers to other heroes, a marker
                // property affects its holder where a token property affects its controller, and a
                // rule lowering three of a hero's values has to be visible to the player it is
                // applied to.
                Component("tigerclaw-poison-marker", "the Poison marker, read on the hero holding it",
                    new[]
                    {
                        Prove(HeldMarkerReading, PoisonReadingJourney),
                        Prove(HeldMarkerReading, PoisonReadingSurvivesJourney)
                    },
                    new[]
                    {
                        "TigerclawPrintedSetTests." +
                            "PoisonedDagger_GivesTheMarker_AndTheHolderLosesThreeValues",
                        "MatchHudPresentationTests." +
                            "AHeldMarker_IsDrawnOnItsHoldersRailFromTheProjectionAlone",
                        "MatchHudPresentationTests." +
                            "AReturnedMarker_LeavesNoReadingBehindOnAnyRail"
                    }),

                // He places no token. His Blue basic places HIMSELF - p.18's Place with the actor as
                // its own subject - and nothing else of his puts a new object on the board.
                AbsentComponent("tigerclaw-tokens", "hero-owned tokens",
                    "Tigerclaw's nineteen faces place no token; Blend Into Shadows places him, which " +
                    "is p.18's Place with the acting hero as its own subject")
            });

        /// <summary>
        /// The capability identities that put a token on the board. A hero whose content names one of
        /// these has a component the matrix must have a row for.
        /// </summary>
        private static readonly string[] TokenCapabilities =
        {
            "synthetic-place-token",
            "synthetic-reclaim-token"
        };

        /// <summary>The two marker supplies his content names, which the debt row above is about.</summary>
        private static readonly string[] MarkerCapabilities =
        {
            "tigerclaw-give-poison-marker-1",
            "tigerclaw-give-poison-marker-2"
        };

        [Test]
        public void EveryTigerclawFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesTigerclawsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(TigerclawPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(TigerclawPlayerPathGateTests).Assembly);

        [Test]
        public void EveryTigerclawJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(TigerclawUiJourneyPlayModeTests),
                typeof(TigerclawEngagedUiJourneyPlayModeTests),
                typeof(TigerclawProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row should
        /// go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndTigerclawActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "tigerclaw-items",
                TigerclawCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        /// <summary>
        /// Milestone 8.23a: the marker row has a subject, and it is no longer a debt.
        ///
        /// <para>Milestone 8.23 held the row to having something to excuse - a build that stopped
        /// attaching a marker would have left it excusing nothing. The row is now an ordinary
        /// component row, so what it is held to is the same fact from the other side: his content
        /// still names a marker supply, and the row that draws one is therefore about something. A
        /// later hero attaching a marker inherits the reading rather than the excuse.</para>
        /// </summary>
        [Test]
        public void ThePoisonMarkerRow_HasItsSubject_BecauseHisContentActuallyAttachesOne() =>
            Assert.That(
                MarkerCapabilities.Any(ContentNamesCapability),
                Is.True,
                "The Poison row draws a marker no face of his gives, so the row is about nothing.");

        [Test]
        public void TigerclawPrintsNoToken_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenCapabilities,
                "Tigerclaw's nineteen faces place no token; his Blue basic places himself");

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

        /// <summary>
        /// Whether any of his printed effects names this capability, read off the content the same way
        /// the matrix reads it.
        /// </summary>
        private static bool ContentNamesCapability(string capabilityId) =>
            TigerclawCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(EveryBody)
                .SelectMany(effect => effect.Nodes)
                .Any(node =>
                    node.Kind == EffectNodeKind.Capability &&
                    node.Capability.CapabilityId.Value == capabilityId);

        private static System.Collections.Generic.IEnumerable<EffectDefinition> EveryBody(
            GuardsDigital.Core.Cards.ActionDefinition action)
        {
            if (action.EffectRoot != null)
            {
                yield return action.EffectRoot;
            }

            if (action.SecondCardTextRoot != null)
            {
                yield return action.SecondCardTextRoot;
            }
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
