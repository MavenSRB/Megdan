using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Silverarrow the Pathfinder's half of the permanent hero player-path completion gate. Milestone
    /// 8.35z46, and the NINTH hero to take it.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the eight gates before it; the table
    /// below is Silverarrow's own. This milestone added NO interaction shape and three capability
    /// classifications - <c>fast-travel</c>, <c>choose-line-destination</c> and
    /// <c>synthetic-attack-took-a-hero</c> - and the offline derivation printed every earlier hero's
    /// row byte-identical afterwards, which is the test milestone 8.20b set for whether an addition is
    /// a generalisation or a re-fit. A hero adding no shape is the healthiest outcome the gate has:
    /// nineteen new faces and the surface already had a name for every input they ask for.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="SilverarrowCardContent"/> - action kind, origin, printed range, card-text timing,
    /// effect node kind, failure policy, capability identity, progression profile - and the guard
    /// compares the derivation against the row exactly. Every row below was written FROM that
    /// derivation rather than checked against it afterwards: it was printed per face, offline, before
    /// a line of this table existed.</para>
    ///
    /// <para><b>Her three Red modals carry no <see cref="ModalBulletChoice"/>, and that is the
    /// milestone's cheapest finding made visible.</b> Snap Shot, Opportunity Shot and Clear Shot each
    /// print "Choose one" with two bullets - and milestone 8.35z30 proved the two bullets name the
    /// SAME SET under every reading of "any other unit", so they compose one candidate set rather than
    /// a Choose node. A player is shown one target question, so the row names one, and a row claiming
    /// a bullet control would name a journey in which a player presses a control that is never
    /// drawn.</para>
    ///
    /// <para><b>Rain of Arrows carries <see cref="RepeatDecision"/> and its repeats open no
    /// control</b>, which is worth stating because the pairing looks wrong until you read the rule:
    /// the shape is derived from the NODE KIND, and RULING 38's repeats are `if able` rather than
    /// `may`. So the journey that proves this row shows the chain ADVANCING - three target questions
    /// in a row with nothing between them - rather than a control being pressed. Xargatha's Cleave is
    /// the same shape with a `may`, and the difference between the two is exactly what her journey
    /// asserts is absent.</para>
    ///
    /// <para><b>Her two "End of turn:" faces are read rather than answered at the moment they are
    /// played.</b> Warning Shot and Treetop Sentinel ask nothing when the Skill resolves - their whole
    /// printed content is that work happens later - so what those rows carry is the effect READING,
    /// and the questions belong to the turn's own close. SL-06 is journeyed there.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class SilverarrowPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names. Declared as constants so a row and the method it names cannot drift apart
        // silently in one direction only.
        // -----------------------------------------------------------------------------------------

        private const string TrayCommitAndWalk =
            "Silverarrow_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnSilverarrowsOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesSilverarrowsCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEverySilverarrowFaceAtTheStandardCardFootprint";

        /// <summary>
        /// RULING 39's journey, and the one that proves her Red modals are one question rather than
        /// two: the union set admits the unit beside her, and the Block that answers it is a ranged
        /// Block because the ACTION is ranged whichever bullet the set came from.
        /// </summary>
        private const string ModalRangedAttack =
            "SnapShot_TakesTheUnitBesideHerAndIsStillAnsweredByARangedBlock";

        /// <summary>
        /// RULING 1 and the card-text Fast Travel in one turn. It is the band journey: the candidates
        /// stand at EXACTLY the printed Range and a unit any nearer is not among them.
        /// </summary>
        private const string BandAndFastTravel =
            "ShootAndScoot_ReachesExactlyItsPrintedRangeAndThenOffersHerTheAdjacentZone";

        private const string ObstacleScope =
            "Trailblazer_LightsObstaclesForHerMovementActionAndNotForACardTextMove";

        private const string ShoveAndStep =
            "Disorient_ShovesTheEnemyBesideHerAndThenOffersHerOwnStepOnItsOwnControl";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndSilverarrowSurvivesTheAttack";

        private const string RealisedDistance =
            "DivertAttention_OffersHerALineNoLongerThanTheSpacesItActuallyMovedTheEnemy";

        private const string RealisedDistanceOfNought =
            "DivertAttention_OffersHerNothing_WhenTheEnemyWasMovedNoSpacesAtAll";

        private const string RetrievalForAnEnemy =
            "FaeHealing_RetrievesACardForTheEnemyHeroItNamed";

        private const string RetrievalForHerself =
            "FaeHealing_RetrievesACardForSilverarrowHerself";

        private const string EndOfTurnDiscard =
            "WarningShot_FiresAtTheCloseOfHerOwnTurnAndTakesACardFromTheEnemyInRadius";

        /// <summary>
        /// RULING 38 as a staged scene can reach it. The three-instance chain is proved at the
        /// authority instead, on a board that fixture builds — every staged scene here is one hero per
        /// team, so once the printed instance has taken the one enemy hero "a different hero" has
        /// nobody left and the chain is structurally bounded at one instance. That is arithmetic about
        /// the staging, measured before the journey was written, and the journey's own summary carries
        /// it.
        /// </summary>
        private const string MandatoryChain =
            "RainOfArrows_TakesItsHeroAndOpensNoRepeatControl_TheChainHavingNowhereToGo";

        /// <summary>
        /// Undo, restore, replay and restart, driven on the end-of-turn discard's TWO questions -
        /// which is the shape milestone 8.35z34 had to make restore-safe, met here for a third time
        /// and with the two questions belonging to two different players.
        /// </summary>
        private const string SessionOperations =
            "TheEndOfTurnDiscardsTwoQuestions_AreAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string UltimateJourney =
            "WildHunt_IsDrawnFaceupOnHerRailAndOffersALineBeforeTheActionSheStarts";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_SILVERARROW.md's inventory table, which is in
        // the same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Silverarrow",
            SilverarrowCardContent.AllDefinitions,
            SilverarrowCardContent.ReturnedToBoxAtSetup,
            SilverarrowActiveEffectContent.Handlers(),
            SilverarrowEffectContent.TriggeredEffects(SilverarrowCardContent.ContentRevision),
            new[]
            {
                // Her Gold basic. RULING 1's band is the whole of what makes it interesting: an
                // Attack 1 that cannot answer an enemy who has closed with her, which is what the
                // card's own name describes. The "After the attack:" Fast Travel is p.14's step 7.
                Face(SilverarrowCardContent.ShootAndScoot, "Shoot and Scoot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, BandAndFastTravel),
                        Prove(AfterAttackTextPause, BandAndFastTravel),
                        Prove(ConfirmOrDecline, BandAndFastTravel),
                        Prove(BoardSpaceSelection, BandAndFastTravel),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowCapabilityTests" +
                            ".Ruling1_AtMaximumRange_AdmitsTheRingAndRefusesEverythingInsideIt",
                        "SilverarrowCapabilityTests" +
                            ".Ruling1_AnEnemyAdjacentToHer_IsNotATargetOfEitherGoldBasic"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player
                // match, so no player of this build can reach it; the guard checks that claim against
                // the content rather than taking the row's word for it.
                UnreachableFace(SilverarrowCardContent.KeepTheDistance, "Keep the Distance",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its \"After the attack:\" sentence carries a named " +
                    "deferral besides, the same one Wasp's Crackling Dagger and Arien's Ceremonial " +
                    "Blade take"),

                // Her Silver basic and only basic Skill, and the only card in her set printing ONE
                // secondary icon - a Defense, resolved by glyph shape against governed reference
                // faces because on a one-icon card the slot position carries no information at all.
                // That is why this row carries no secondary-movement shape where every other dealt
                // face of hers does.
                Face(SilverarrowCardContent.Trailblazer, "Trailblazer", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ObstacleScope),
                        Prove(ConfirmOrDecline, ObstacleScope),
                        Prove(BoardSpaceSelection, ObstacleScope),
                        Prove(ActiveEffectReading, ObstacleScope),
                        Prove(RoundLongActiveEffectReading, ObstacleScope),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "SilverarrowCapabilityTests" +
                            ".Ruling47_TheScopedPermission_ReachesAMovementActionAndNotACardTextMove"
                    }),

                // The three Red modals, which print one sentence between them and differ only in
                // their Attack and Range. One journey proves all three for the reason the gate's own
                // "need not duplicate permutations" sentence permits: the union set is the same
                // question at every printed number.
                Face(SilverarrowCardContent.SnapShot, "Snap Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalRangedAttack),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowCapabilityTests" +
                            ".TheRedModal_IsTheSameSetUnderBothReadingsOfAnyOtherUnit"
                    }),

                Face(SilverarrowCardContent.OpportunityShot, "Opportunity Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalRangedAttack),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // Band only, no effect root: the plainest Attack in her set and the control for the
                // three modals beside it.
                Face(SilverarrowCardContent.LongShot, "Long Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, BandAndFastTravel),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // Range 4 is the longest printed reach in the verified roster; Wasp's maximum is 3.
                Face(SilverarrowCardContent.ClearShot, "Clear Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalRangedAttack),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // RULING 38's chain, and the only face in the roster printing TWO repeat clauses.
                // Its RepeatDecision shape opens no control, so the journey that proves it shows the
                // chain advancing - see this fixture's own summary.
                Face(SilverarrowCardContent.RainOfArrows, "Rain of Arrows", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, MandatoryChain),
                        Prove(AfterAttackTextPause, MandatoryChain),
                        Prove(RepeatDecision, MandatoryChain),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowRepeatChainTests" +
                            ".RainOfArrows_ChainsThreeInstances_EachNarrowedToWhatItsOwnClausePrints",
                        "SilverarrowRepeatChainTests" +
                            ".RainOfArrows_StopsAtOneInstance_WhenNoDifferentEnemyHeroIsInTheBand"
                    }),

                // The three restriction Blues, which print Arien's Deluge word for word and differ
                // only in the disk they are measured over. Nothing here asks a player anything: the
                // Skill creates the effect and what a player does is READ it, on their own rail and
                // on the enemy's.
                Face(SilverarrowCardContent.GraspingRoots, "Grasping Roots", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ObstacleScope),
                        Prove(ActiveEffectReading, ObstacleScope),
                        Prove(OpponentRestrictionReading, ObstacleScope),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowPrintedSetTests" +
                            ".TheBlueTrio_DeclaresExactlyArienSownDelugeModifications"
                    }),

                Face(SilverarrowCardContent.EntanglingVines, "Entangling Vines", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ObstacleScope),
                        Prove(ActiveEffectReading, ObstacleScope),
                        Prove(OpponentRestrictionReading, ObstacleScope),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // The roster's first "End of turn:" face. What it asks is asked at the turn's close
                // and not when it is played, which is why this row's shapes are readings.
                Face(SilverarrowCardContent.WarningShot, "Warning Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EndOfTurnDiscard),
                        Prove(ActiveEffectReading, EndOfTurnDiscard),
                        Prove(RoundLongActiveEffectReading, EndOfTurnDiscard),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowEndOfTurnDriveTests" +
                            ".WarningShot_WithholdsTheTurnsCloseUntilBothOfItsQuestionsAreAnswered",
                        "SilverarrowEndOfTurnDriveTests" +
                            ".SL06_TheRadiusIsMeasuredWhenTheTriggerFires_SoOneSpaceFurtherOutIsNoCandidate"
                    }),

                Face(SilverarrowCardContent.GrapplingBranches, "Grappling Branches", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ObstacleScope),
                        Prove(ActiveEffectReading, ObstacleScope),
                        Prove(OpponentRestrictionReading, ObstacleScope),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // Warning Shot's sentence with ledger question 13's escalation in place of its "if
                // able" - the same relationship Wasp's Shock and Electroblast have.
                Face(SilverarrowCardContent.TreetopSentinel, "Treetop Sentinel", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EndOfTurnDiscard),
                        Prove(ActiveEffectReading, EndOfTurnDiscard),
                        Prove(RoundLongActiveEffectReading, EndOfTurnDiscard),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowPrintedSetTests" +
                            ".EveryFaceThatPrintsAStep_ComposesOne"
                    }),

                // Its two printed differences from its Tier II and III siblings, both of which a
                // player meets: the first Move is exact and mandatory, and the self-move is optional
                // and walks no straight line.
                Face(SilverarrowCardContent.Disorient, "Disorient", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShoveAndStep),
                        Prove(ConfirmOrDecline, ShoveAndStep),
                        Prove(BoardUnitSelection, ShoveAndStep),
                        Prove(BoardSpaceSelection, ShoveAndStep),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.Disorient_IsNotASmallerDivertAttention" }),

                // RULING 37's realised-distance binding, and the two cells that make it observable.
                Face(SilverarrowCardContent.DivertAttention, "Divert Attention", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RealisedDistance),
                        Prove(ConfirmOrDecline, RealisedDistanceOfNought),
                        Prove(BoardUnitSelection, RealisedDistance),
                        Prove(BoardSpaceSelection, RealisedDistance),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowPrintedSetTests" +
                            ".Ruling37_TheSelfMovesBound_IsTheFirstMovesRealisedDistance"
                    }),

                // RULING 8, both arms: a hero in radius is any hero in radius, enemies and herself.
                Face(SilverarrowCardContent.FaeHealing, "Fae Healing", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalForAnEnemy),
                        Prove(ConfirmOrDecline, RetrievalForAnEnemy),
                        Prove(BoardUnitSelection, RetrievalForHerself),
                        Prove(HandCardSelection, RetrievalForAnEnemy),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SilverarrowPrintedSetTests" +
                            ".Ruling8_TheHealingFaces_AdmitAnyHeroIncludingHerself"
                    }),

                Face(SilverarrowCardContent.LeadAstray, "Lead Astray", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RealisedDistance),
                        Prove(ConfirmOrDecline, RealisedDistanceOfNought),
                        Prove(BoardUnitSelection, RealisedDistance),
                        Prove(BoardSpaceSelection, RealisedDistance),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                Face(SilverarrowCardContent.NaturesBlessing, "Nature's Blessing", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalForAnEnemy),
                        Prove(ConfirmOrDecline, RetrievalForAnEnemy),
                        Prove(BoardUnitSelection, RetrievalForHerself),
                        Prove(HandCardSelection, RetrievalForAnEnemy),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "SilverarrowPrintedSetTests.EveryPrintedFace_IsDefinedWithItsTranscribedValues" }),

                // p.16's Ultimate, and the first in this build that fires before EVERY action. It
                // prints neither Range nor Radius, which makes her the first hero for whom ledger
                // question 12's item-reaches-the-Ultimate reading has nothing at all to apply to.
                Face(SilverarrowCardContent.WildHunt, "Wild Hunt", new[]
                    {
                        Prove(ConfirmOrDecline, UltimateJourney),
                        Prove(BoardSpaceSelection, UltimateJourney),
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney)
                    },
                    new[]
                    {
                        "SilverarrowCapabilityTests" +
                            ".WildHunt_FiresBeforeEveryActionKindAndIsNeverConsumed"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("silverarrow-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, NumericDefense),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[] { "SilverarrowPrintedSetTests.TheDeckShape_IsPageFivesWithoutException" }),

                Component("silverarrow-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, SessionOperations),
                        Prove(RetainedPrefixUndo, SessionOperations),
                        Prove(ReplayReconstruction, SessionOperations),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[]
                    {
                        "SilverarrowEndOfTurnDriveTests" +
                            ".WarningShot_AsksSilverarrowWhichEnemyAndThatEnemyWhichCard"
                    }),

                Component("silverarrow-card-footprint",
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
                // same row: a composed scene stages a hand, and no scene stages the coin-earning
                // rounds a real Level Up needs.
                AbsentComponent("silverarrow-items",
                    "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; her twelve item icons " +
                    "are pinned face by face by SilverarrowPrintedSetTests." +
                    "EveryPrintedFace_MatchesTheSourceInventory, and the consequence that her " +
                    "Ultimate prints neither Range nor Radius - so no item can reach it, a first in " +
                    "the roster - is recorded in the shard rather than journeyed"),

                // She owns no token and no marker, and that is content rather than an omission -
                // ledger question 14 rules that a hero's tokens are defined by that hero's card
                // faces, and hers name none. Where Wasp's telekinetic Skills read "a unit, or a
                // token", her three Green displacement Skills read "an enemy unit" only: the token
                // class never appears in her set.
                AbsentComponent("silverarrow-tokens-and-markers", "hero-owned tokens and markers",
                    "Silverarrow's nineteen faces place no token and give no marker, and none of " +
                    "them moves one either - the blank Tabletopia dashboard back is missing ART " +
                    "rather than missing rules, under the standing 2026-08-23 ruling that a hero's " +
                    "tokens are defined by that hero's card faces")
            });

        /// <summary>
        /// The capability identities that put a token or a marker on the board. A hero whose content
        /// names one of these has a component the matrix must have a row for.
        /// </summary>
        private static readonly string[] TokenAndMarkerCapabilities =
        {
            "synthetic-place-token",
            "synthetic-remove-token",
            "tigerclaw-give-poison-marker-1",
            "tigerclaw-give-poison-marker-2"
        };

        [Test]
        public void EverySilverarrowFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesSilverarrowsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(SilverarrowPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(SilverarrowPlayerPathGateTests).Assembly);

        [Test]
        public void EverySilverarrowJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(SilverarrowUiJourneyPlayModeTests),
                typeof(SilverarrowEngagedUiJourneyPlayModeTests),
                typeof(SilverarrowProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if
        /// a hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndSilverarrowActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "silverarrow-items",
                SilverarrowCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void SilverarrowPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Silverarrow's nineteen faces place no token and give no marker");

        /// <summary>
        /// Every row's authoritative scenarios name a fixture the Edit Mode assembly actually has.
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
