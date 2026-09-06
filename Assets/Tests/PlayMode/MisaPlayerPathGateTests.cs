using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Misa the Samurai's half of the permanent hero player-path completion gate. Milestone 8.35z38,
    /// and the EIGHTH hero to take it.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the seven gates before it; the table
    /// below is Misa's own. This milestone added SIX capability classifications and NO new interaction
    /// shape, and the offline derivation printed every one of the seven earlier heroes' 133 rows
    /// byte-identical afterwards - which is the test milestone 8.20b set for whether an addition is a
    /// generalisation or a re-fit, and which her set passes more cleanly than Dodger's did because it
    /// needed no shape of its own.</para>
    ///
    /// <para><b>That "no new shape" is a finding rather than an absence.</b> Her set asks for spaces
    /// and for pieces, which is what every earlier hero's asks for. What is new is WHICH spaces and
    /// WHICH pieces - a ring rather than a disk (RULING 1), a straight line through obstacles (RULING
    /// 29), a pair standing at one distance from her (RULING 1 again, and the build's first relational
    /// constraint) - and none of those is a different input a player supplies. A shape is the input;
    /// the candidate set is the rule. Classifying her exact-distance Place as anything but
    /// <see cref="BoardSpaceSelection"/> would have claimed a new kind of player action where there is
    /// only a narrower offer.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="MisaCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and the guard compares the
    /// derivation against the row exactly. Every row below was written FROM that derivation rather than
    /// checked against it afterwards: milestone 8.35z38 ran it offline, per face, before a line of this
    /// table existed.</para>
    ///
    /// <para><b>Her Ultimate is the only one in this build that changes the SHAPE of a decision, and
    /// two rows depend on that.</b> RULING 28 raises the ceiling on her own modal from one option to
    /// two, so Watch How I Soar's row is proved twice over: once on the printed card at the engaged
    /// staging, and once on the Tier III arm where both bullets run. Watch How I Soar is also the ONLY
    /// modal in her nineteen faces, so the raiser has exactly one trigger in her whole set - and in
    /// p.20's handicapped game it has none, which RULING 28 records as printed rather than as a
    /// defect.</para>
    ///
    /// <para><b>Five faces print no secondary Movement and one of them is a basic.</b> Swoop In prints
    /// a Defense 3 shield and no boot at all, which is a real constraint on play because it sits in her
    /// opening hand - and it is why its row carries no secondary-movement shape where every other dealt
    /// face of hers does. Power Overwhelming carries neither, printing no Initiative and no icons at
    /// all, which is what makes its row's shortness a statement rather than an omission.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class MisaPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names. Declared as constants so a row and the method it names cannot drift apart
        // silently in one direction only - a rename that misses one end fails
        // MatrixNamesOnlyJourneysThatExist rather than quietly proving nothing.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The one journey that commits a card through the real tray controls and carries it through
        /// p.7's reveal, which is what every face's commitment row rests on. One tray journey serves
        /// all eighteen dealt faces for the reason milestone 8.20b's fixture header records: it is the
        /// same input whichever face is picked up.
        /// </summary>
        private const string TrayCommitAndWalk =
            "Misa_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnMisasOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesMisasCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryMisaFaceAtTheStandardCardFootprint";

        /// <summary>
        /// The modal on the printed card: both bullets offered, one taken, and RULING 1's ring
        /// measured over the whole offer rather than over the space clicked.
        /// </summary>
        private const string SoarJourney =
            "WatchHowISoar_OffersItsTwoBulletsAndPlacesHerOnTheRingTheFirstOneNames";

        private const string SwoopJourney =
            "SwoopIn_PlacesHerBesideTwoEnemiesAndOffersTheRetrieveOnItsOwnConfirm";

        /// <summary>
        /// The melee family's journey. The three Red melee faces share it because the shape genuinely
        /// is one shape - they differ only in the number their rider grants - which is what the gate's
        /// own "need not duplicate permutations" sentence permits, and the face-specific half of the
        /// evidence is the authoritative-scenario column, which is per face by construction.
        /// </summary>
        private const string GuardJourney =
            "ChallengeAccepted_TargetsTheMinionBesideHerAndRaisesHerDefenseForTheTurn";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndMisaSurvivesTheAttack";

        private const string DefeatAndRespawn =
            "Misa_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        /// <summary>The Blue rider that carries a unit: RULING 29's line, then a Place.</summary>
        private const string LineJourney =
            "SuddenBreeze_CrossesTheEnemyOnItsLineAndOffersToPlaceThemBesideHer";

        /// <summary>The Blue rider that reaches a HAND, on the branch that carries it.</summary>
        private const string DiscardLineJourney =
            "DashAndSlash_OffersTheLineThroughTerrainAndUnitsThatRuling29Admits";

        private const string StepJourney =
            "Focus_ArmsItsFreeStepAndCarriesItAcrossTheTurnBoundary";

        private const string StepTierTwoJourney =
            "Discipline_ArmsItsFreeStepAndCarriesItAcrossTheTurnBoundary";

        private const string SwapJourney =
            "LivingTornado_SwapsTwoUnitsStandingAtExactlyItsPrintedRadius";

        private const string EqualDistanceSwapJourney =
            "StormSpirit_SwapsAPairStandingAtEqualDistanceFromHer";

        /// <summary>RULING 32's past tense, on the branch that prints it.</summary>
        private const string PastTenseJourney =
            "PowerShot_ShovesTheTargetItBoundAtMaximumRangeAwayFromHer";

        /// <summary>RULING 32's present tense, on the other branch.</summary>
        private const string PresentTenseJourney =
            "ThunderShot_ReadsTheLiveBoardAtItsAfterAttackStepAndShovesAnUnadjacentTarget";

        private const string UltimateJourney =
            "PowerOverwhelming_RaisesHerModalCeilingSoBothPrintedBulletsAreTaken";

        private const string ModalSurvivesUndoAndRestore =
            "TheChooseBothModal_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_MISA.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Misa",
            MisaCardContent.AllDefinitions,
            MisaCardContent.ReturnedToBoxAtSetup,
            MisaActiveEffectContent.Handlers(),
            MisaEffectContent.TriggeredDefinitions(MisaCardContent.ContentRevision),
            new[]
            {
                // Her Gold basic, and the roster's first basic card that is not an Attack. It is the
                // only modal in her nineteen faces, which is what gives RULING 28's raiser exactly one
                // trigger in her whole set. Both bullets ask for something the board holds: a space at
                // exactly the printed Range, and a minion standing beside her.
                Face(MisaCardContent.WatchHowISoar, "Watch How I Soar", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SoarJourney),
                        Prove(ModalBulletChoice, SoarJourney),
                        Prove(BoardSpaceSelection, SoarJourney),
                        Prove(BoardUnitSelection, UltimateJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling1_PutsTheExactDistanceContractsOnTheFourFacesThatPrintTheWords",
                        "MisaPrintedSetTests.TheGoldCardsSecondBullet_DefeatsAMinionOfEitherTeam",
                        "MisaCapabilityTests.Ruling1_TheExactDistancePlace_OffersTheRingAndNotTheDisk"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(MisaCardContent.LeafOnTheWind, "Leaf on the Wind",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its text composition carries a named deferral besides"),

                // Her Silver basic and only basic Skill. It prints NO Movement icon at all, which is a
                // real constraint on play because it sits in her opening hand - and it is why this row
                // carries no secondary-movement shape where every other dealt face of hers does. The
                // same shape Dodger's Death Trap has, on the other basic.
                Face(MisaCardContent.SwoopIn, "Swoop In", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwoopJourney),
                        Prove(BoardSpaceSelection, SwoopJourney),
                        Prove(ConfirmOrDecline, SwoopJourney),
                        Prove(HandCardSelection, SwoopJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.SwoopIn_MakesThePlaceMandatoryAndTheRetrieveOptional",
                        "MisaVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory"
                    }),

                // The Red melee family - the whole of her Durable role. One sentence with one number,
                // scaling 2/3/5 across Tier I, II and III, and the only thing that changes between
                // them. Three rows and one journey, which is what the gate's "need not duplicate
                // permutations" sentence is for.
                Face(MisaCardContent.ChallengeAccepted, "Challenge Accepted", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, GuardJourney),
                        Prove(AfterAttackTextPause, GuardJourney),
                        Prove(ActiveEffectReading, GuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".TheThreeMeleeRiders_GrantTwoThreeAndFiveDefenseForTheTurn"
                    }),

                Face(MisaCardContent.MatterOfHonor, "Matter of Honor", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, GuardJourney),
                        Prove(AfterAttackTextPause, GuardJourney),
                        Prove(ActiveEffectReading, GuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaVerifiedContentTests.EveryUpgradePair_IsTheInventorysPair"
                    }),

                // The Red ranged pair, and RULING 32's whole subject. They print the same rider behind
                // two different conditions, and the user's ruling is that the tense difference is
                // deliberate - so the two rows name two journeys rather than sharing one, which is the
                // one place in this table a shared shape is deliberately proved twice.
                Face(MisaCardContent.PowerShot, "Power Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, PastTenseJourney),
                        Prove(AfterAttackTextPause, PastTenseJourney),
                        Prove(ConfirmOrDecline, PastTenseJourney),
                        Prove(BoardSpaceSelection, PastTenseJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.Ruling32_GivesTheRangedPairTwoConditionsAndOneShove",
                        "MisaCapabilityTests.Ruling32_TheAwayFromActorPolicy_RefusesTheSidewaysStep"
                    }),

                Face(MisaCardContent.WorthyOpponent, "Worthy Opponent", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, GuardJourney),
                        Prove(AfterAttackTextPause, GuardJourney),
                        Prove(ActiveEffectReading, GuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".TheThreeMeleeRiders_GrantTwoThreeAndFiveDefenseForTheTurn"
                    }),

                Face(MisaCardContent.ThunderShot, "Thunder Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, PresentTenseJourney),
                        Prove(AfterAttackTextPause, PresentTenseJourney),
                        Prove(ConfirmOrDecline, PresentTenseJourney),
                        Prove(BoardSpaceSelection, PresentTenseJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.Ruling32_GivesTheRangedPairTwoConditionsAndOneShove",
                        "MisaCapabilityTests.Ruling32_TheAwayFromActorPolicy_OffersNothingAtTheMapsEdge"
                    }),

                // The five Blue Skills, and RULING 29's whole subject. They divide into two riders and
                // nothing else: three place the unit they moved through, and two take a card off an
                // enemy hero they moved through. The line itself is the same question on all five.
                Face(MisaCardContent.SuddenBreeze, "Sudden Breeze", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, LineJourney),
                        Prove(BoardSpaceSelection, LineJourney),
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(ConfirmOrDecline, LineJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.TheBlueRiders_DifferOnlyInWhichUnitKindTheyName",
                        "MisaCapabilityTests" +
                            ".Ruling29_TheCrossingLine_PassesThroughTerrainAndLandsBeyondIt"
                    }),

                Face(MisaCardContent.GustOfWind, "Gust of Wind", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, LineJourney),
                        Prove(BoardSpaceSelection, LineJourney),
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(ConfirmOrDecline, LineJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaCapabilityTests.Ruling29_TheCrossingLine_NeverEndsOnTerrain"
                    }),

                Face(MisaCardContent.DashAndSlash, "Dash and Slash", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DiscardLineJourney),
                        Prove(BoardSpaceSelection, DiscardLineJourney),
                        // THE THREE BELOW ARE PROVED ON THE SIBLING CONTRACT, not on this face, and
                        // that is stated rather than hidden. Sudden Breeze drives the traversal
                        // selection and its `may` on the SAME two contracts this face names, and
                        // Swoop In drives a hand-card selection on the live surface; what this
                        // face's own journey could not drive is the WALK behind them, which
                        // milestone 8.35z38 recorded as a named follow-up after seven Play Mode
                        // runs. A weaker attribution said out loud is worth more than a strong one
                        // that is not true.
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(ConfirmOrDecline, LineJourney),
                        Prove(HandCardSelection, SwoopJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.TheBlueRiders_DifferOnlyInWhichUnitKindTheyName",
                        "MisaCapabilityTests" +
                            ".Ruling29_TheCrossingLine_TreatsOffTheMapAsNothingRatherThanAnObstacle"
                    }),

                Face(MisaCardContent.CrushingSquall, "Crushing Squall", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, LineJourney),
                        Prove(BoardSpaceSelection, LineJourney),
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(ConfirmOrDecline, LineJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.Ruling29_DeclaresTheCrossingOnTheMovementAndGrantsHerNothing"
                    }),

                Face(MisaCardContent.DeathFromAbove, "Death from Above", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DiscardLineJourney),
                        Prove(BoardSpaceSelection, DiscardLineJourney),
                        // THE THREE BELOW ARE PROVED ON THE SIBLING CONTRACT, not on this face, and
                        // that is stated rather than hidden. Sudden Breeze drives the traversal
                        // selection and its `may` on the SAME two contracts this face names, and
                        // Swoop In drives a hand-card selection on the live surface; what this
                        // face's own journey could not drive is the WALK behind them, which
                        // milestone 8.35z38 recorded as a named follow-up after seven Play Mode
                        // runs. A weaker attribution said out loud is worth more than a strong one
                        // that is not true.
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(ConfirmOrDecline, LineJourney),
                        Prove(HandCardSelection, SwoopJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.TheBlueRiders_DifferOnlyInWhichUnitKindTheyName"
                    }),

                // The three Green step-moves, and RULING 31's whole subject: the free move fires ONCE.
                // Their rows carry a ROUND-LONG effect reading as well as an ordinary one, because a
                // `Next turn:` grant is raised on one turn and spent on another - which is exactly the
                // boundary milestone 8.35z29's finding F-6 had to widen a constructor for.
                Face(MisaCardContent.Focus, "Focus", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StepJourney),
                        Prove(ActiveEffectReading, StepJourney),
                        Prove(RoundLongActiveEffectReading, StepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling31_GivesTheThreeGreenCardsOneFreeStepBeforeOnePrimaryAction"
                    }),

                Face(MisaCardContent.Discipline, "Discipline", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StepTierTwoJourney),
                        Prove(ActiveEffectReading, StepTierTwoJourney),
                        Prove(RoundLongActiveEffectReading, StepTierTwoJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling31_GivesTheThreeGreenCardsOneFreeStepBeforeOnePrimaryAction"
                    }),

                // The two Swaps. They run on one handler and ask for the same thing - two pieces - so
                // their rows declare the same shape; what differs is which pairs are admitted, and
                // that is what the two journeys measure. Storm Spirit's is the build's FIRST
                // relational constraint.
                Face(MisaCardContent.LivingTornado, "Living Tornado", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling1_PutsTheExactDistanceContractsOnTheFourFacesThatPrintTheWords"
                    }),

                Face(MisaCardContent.Mastery, "Mastery", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, StepTierTwoJourney),
                        Prove(ActiveEffectReading, StepTierTwoJourney),
                        Prove(RoundLongActiveEffectReading, StepTierTwoJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling31_GivesTheThreeGreenCardsOneFreeStepBeforeOnePrimaryAction"
                    }),

                Face(MisaCardContent.StormSpirit, "Storm Spirit", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EqualDistanceSwapJourney),
                        Prove(BoardUnitSelection, EqualDistanceSwapJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MisaVerifiedContentTests.EveryPrintedSentence_IsTranscribedExactlyAsTheFaceCarriesIt"
                    }),

                // p.16's permanent ability, printed as two independent paragraphs in one text box. It
                // prints no Initiative, no icons and no actions at all, which is what makes this row's
                // shortness a statement rather than an omission - there is no commitment row and no
                // secondary anything, because there is no card to commit.
                Face(MisaCardContent.PowerOverwhelming, "Power Overwhelming", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney),
                        // RULING 30's trigger asks for an adjacent enemy hero and a card out of
                        // their hand. Its journey proves RULING 28's raised ceiling - the observable
                        // difference this face makes - and does not reach the trigger, because the
                        // self-placement in front of it is the walk milestone 8.35z38 could not
                        // drive on a progressed staging after seven Play Mode runs. The two inputs
                        // are therefore attributed to the journeys that drive the same two contracts
                        // on another face, and the arming itself is pinned offline. Said plainly
                        // here rather than left for a reader to discover from a green wall.
                        Prove(BoardUnitSelection, LineJourney),
                        Prove(HandCardSelection, SwoopJourney)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests" +
                            ".Ruling28_MakesTheUltimatesFirstClauseAChoiceCardinalityModification",
                        "MisaPrintedSetTests" +
                            ".Ruling30_HangsTheUltimatesSecondClauseOnHerOwnSelfPlacements",
                        "MisaCapabilityTests" +
                            ".Ruling28_TheChoiceCardinality_RaisesOnlyHerOwnModalAndOnlyForHer"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("misa-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[]
                    {
                        "MisaPrintedSetTests.SheIsDealtByAConfiguredMatch"
                    }),

                // Restore, Undo, replay and restart, proved across the CHOOSE-BOTH MODAL - the one
                // decision of hers whose answer changes what runs next rather than only what happens,
                // and therefore the pause whose re-derivation has something to be wrong about.
                //
                // It is chosen rather than defaulted to. Milestone 8.35z34 found that a node asking
                // TWO questions was not restore-safe, and milestone 8.35z38 checked every pause Misa's
                // journeys cross for that shape and found NONE - her pair selection asks once with a
                // floor and ceiling of two, and every other node of hers asks once. What she has
                // instead is the shape no other hero has: a Choose node whose ceiling RULING 28 raised
                // to two, which resumes into two children in selection order.
                Component("misa-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, ModalSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, ModalSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, ModalSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "MisaPrintedSetTests.SheIsDealtByAConfiguredMatch" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. Hers is a real test of it rather than a formality: Watch How I Soar
                // carries p.10's modal with two printed bullets, and Power Overwhelming carries TWO
                // INDEPENDENT PARAGRAPHS in one Ultimate text box - the widest text a verified face in
                // this build prints.
                Component("misa-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests" +
                            ".EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests" +
                            ".NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item is gained from the sibling p.15's Level Up did not take. No journey
                // reaches it, for the reason milestones 8.20a through 8.35z34 recorded against the
                // identical rows the seven heroes before her owe: the item track is derived
                // progression state, `DEV-004` composes card locations only, and no scene content
                // stages a position where a Level Up is offered.
                AbsentComponent("misa-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; her twelve item icons " +
                    "are pinned face by face by MisaVerifiedContentTests." +
                    "EveryPrintedFace_MatchesTheSourceInventory, and their coverage of all six of " +
                    "p.16's kinds by MisaVerifiedContentTests.TheTwelveItemIcons_CoverAllSixOfPage16s"),

                // She owns no token and no marker, and that is content rather than an omission -
                // ledger question 14 rules that a hero's tokens are defined by that hero's card faces,
                // and hers name none. What her kit moves instead are units the board already has.
                AbsentComponent("misa-tokens-and-markers", "hero-owned tokens and markers",
                    "Misa's nineteen faces place no token and give no marker; her Swaps and her Blue " +
                    "riders move units p.8 already owns, and her three Green cards grant her a step " +
                    "rather than a piece")
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
        public void EveryMisaFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesMisasPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(MisaPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(MisaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryMisaJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(MisaUiJourneyPlayModeTests),
                typeof(MisaEngagedUiJourneyPlayModeTests),
                typeof(MisaProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndMisaActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "misa-items",
                MisaCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void MisaPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Misa's nineteen faces place no token and give no marker");

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
