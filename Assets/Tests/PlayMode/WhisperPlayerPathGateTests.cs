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
    /// Whisper the Outcast's half of the permanent hero player-path completion gate. Milestone
    /// 8.35z53, the NINTH hero to take it, and the first from the Devoted Hero
    /// pack.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the eight gates before it; the table
    /// below is Whisper's own. This milestone added FIVE capability classifications, corrected ONE
    /// structural under-report in the derivation itself, and added NO new interaction shape.</para>
    ///
    /// <para><b>The structural correction is the thing to read first, because it changes what this
    /// table can see.</b> Milestone 8.5 gave one action two card-text bodies - a step-1 body and a
    /// step-7 body - and <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> read the first alone
    /// until this milestone. Swift Justice is where that stopped being survivable: its second body is
    /// <c>defended-card-forced-move@1</c>, whose board-space question is asked of the DEFENDER and
    /// appears nowhere else on the card, and it sits in her opening hand. The derivation reads both
    /// bodies now. Two rows in one shipped hero moved as a result - Tigerclaw's Combat Reflexes and
    /// Leaping Strike each gained <see cref="AfterAttackTextPause"/> - and both were already proved by
    /// a journey that drives p.14's step 7 and says so in its own name, so the correction asked for no
    /// evidence that did not exist.</para>
    ///
    /// <para><b>The most unusual row in this table is Swift Justice's, and the reason is whose hands
    /// the questions are in.</b> One printed face asks Whisper which bullet, asks p.14 whom to attack,
    /// asks the DEFENDER's own seat which way to be shoved, and does all three inside one action. Under
    /// RULING 16 the forced move is steered by the hero performing it rather than by the hero who
    /// caused it - the designer's own 2026-08-19 answer that there is no functional distinction between
    /// "Player" and "Hero" - so its board-space shape is one a player supplies while it is not their
    /// turn. That is the second such shape in this build, and the first was Lesser Evil's, three rows
    /// below.</para>
    ///
    /// <para><b>Two faces of hers hand a whole decision to an opponent.</b> Lesser Evil and Greater
    /// Good put p.10's modal to an enemy hero Whisper selected
    /// (<see cref="EffectChooserRole.BoundHero"/>), which makes their
    /// <see cref="ModalBulletChoice"/> a control the ENEMY seat is shown. RULING 17 then decides which
    /// bullets that seat may take, and milestone 8.35z35's two-tier split is what makes the answer
    /// interesting: an option that CANNOT be performed is greyed with its reason, and an option that
    /// can be taken and would apply nothing is OFFERED, because on an opponent-chosen modal the bullet
    /// that does nothing to them is the escape hatch the decision exists to offer. Both halves are
    /// journeyed - Greater Good proves the empty bullet is offered, Lesser Evil proves taking it
    /// resolves.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="WhisperCardContent"/> - action kind, origin, printed range, both card-text timings,
    /// effect node kind, failure policy, capability identity, progression profile - and the guard
    /// compares the derivation against the row exactly. Every row below was derived by hand from that
    /// code before a line of this table existed, and the offline probe in this milestone's own prep
    /// folder re-derives all nineteen from the real content.</para>
    ///
    /// <para><b>Her Ultimate is the shortest row in this table and that is a statement.</b> Grim Reaper
    /// prints no Initiative, no icons and no action at all, so it composes no action definition, so
    /// there is no commitment row and no secondary anything - the same shape Xargatha's Metamorphosis
    /// has. What it does is raise one number on two of her own basic cards, which is not a shape at
    /// all: a player answers a two-bullet modal the same way they answer a one-bullet one.</para>
    ///
    /// <para><b>One face prints no boot.</b> Death Seeker prints a Defense 2 shield and no Movement
    /// icon, which is a real constraint on play because it sits in her opening hand - and it is why its
    /// row carries no secondary-movement shape where every other dealt face of hers does. The same
    /// shape Misa's Swoop In and Dodger's Death Trap have, on the other basic.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class WhisperPlayerPathGateTests
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
            "Whisper_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnWhispersOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesWhispersCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryWhisperFaceAtTheStandardCardFootprint";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndWhisperSurvivesTheAttack";

        private const string DefeatAndRespawn =
            "Whisper_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        /// <summary>
        /// Her Gold basic's own modal: both printed bullets offered, the adjacent one taken, and p.14
        /// run once for it.
        /// </summary>
        private const string SwiftJusticeJourney =
            "SwiftJustice_OffersItsTwoBulletsAndTakesTheAdjacentUnitTheSecondOneNames";

        /// <summary>
        /// RULING 16, and the only journey in this build where a board space is clicked by a player
        /// whose turn it is not.
        /// </summary>
        private const string ForcedMoveJourney =
            "SwiftJustice_LetsTheDefendersOwnSeatSteerTheForcedMoveItsFirstBulletPrints";

        /// <summary>
        /// RULING 53, on the turn where it is unavoidable. It superseded RULING 42 on 2026-09-08:
        /// a play whose only "If" gate is false is OFFERED as a legal do-nothing play - "it's a
        /// Hold" - so the shipped behaviour milestone 8.35z53 parked this journey over is the RULED
        /// behaviour, and the journey now asserts the offer AND the no-op resolution instead.
        /// </summary>
        private const string DeathSeekerTurnOne =
            "DeathSeeker_IsOfferedOnTheFirstTurnOfRoundOne_AndResolvesAsALegalDoNothingPlay";

        /// <summary>RULING 33's actor-scoped immunity, driven from both sides.</summary>
        private const string ImmunityJourney =
            "DeathSeeker_RaisesHerTurnLongImmunityAndAnEnemyHeroCanNeitherTargetNorAffectHer";

        /// <summary>Grim Reaper's raised ceiling on the OTHER basic card, where "both" is two effects.</summary>
        private const string DeathSeekerBothJourney =
            "DeathSeeker_AppliesBothOfItsBulletsOnceGrimReaperHasRaisedItsCeiling";

        /// <summary>
        /// The Red step-then-target seam, driven on Sanguine Path. Crimson Trail prints the same seam
        /// behind a boolean condition; its distinct branch rule is covered by the source-linked gate.
        /// </summary>
        private const string RedStepJourney =
            "SanguinePath_BuysItsStepFromTheSpawnPointCountBeforeItNamesTheUnitBesideHer";

        private const string RedStepUnavailableJourney =
            "SanguinePath_NoReachableTargetRemainsUnavailable";

        private const string RedStepZeroBudgetJourney =
            "SanguinePath_ZeroBudgetStaysThenTargets";

        /// <summary>RULING 34's chain, and the single-slot exclusion it actually gets.</summary>
        private const string ChainJourney =
            "BloodFrenzy_PerformsFiveDistinctRepeatsAcrossUndoAndRestore";

        private const string SeedsJourney =
            "SeedsOfFear_MovesTheEnemyOffItsSpawnPointExactlyOneSpaceAndNotZero";

        private const string ShadowStepJourney =
            "ShadowStep_PlacesHerIntoAnEmptyMinionSpawnPointAtItsPrintedRange";

        /// <summary>RULING 17's second half: the enemy seat TAKES the bullet that does nothing.</summary>
        private const string OpponentModalJourney =
            "LesserEvil_PutsItsModalToTheEnemySeatWhoTakesTheBulletThatWouldDoNothing";

        /// <summary>RULING 17's first half: the empty bullet is OFFERED rather than greyed.</summary>
        private const string GreaterGoodJourney =
            "GreaterGood_OffersItsEmptyDefeatBulletUngreyedAndTakesThePriceInstead";

        private const string SwapJourney =
            "CruelTwist_SwapsWithAUnitDrawnFromTheUnionItsOneSelectionOffers";

        /// <summary>RULING 19: two complete p.14 orders inside one action, on one printed card.</summary>
        private const string UltimateJourney =
            "GrimReaper_RaisesSwiftJusticesCeilingSoBothBulletsRunTheirOwnFullSequence";

        private const string ModalSurvivesUndoAndRestore =
            "TheOpponentChosenModal_IsAnsweredAgainByTheEnemySeatAfterAnUndoAndAfterARestore";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_WHISPER.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Whisper",
            WhisperCardContent.AllDefinitions,
            WhisperCardContent.ReturnedToBoxAtSetup,
            WhisperActiveEffectContent.Handlers(),

            // Grim Reaper is p.16's PERMANENT ABILITY and triggers no effect at all - it declares one
            // rule modification and nothing else - so there is no triggered definition to hand over
            // and an empty list is the honest argument rather than a placeholder. Xargatha's
            // Metamorphosis is the same shape; Misa's Power Overwhelming is not, which is why hers
            // passes a real list here.
            Array.Empty<EffectDefinition>(),
            new[]
            {
                // Her Gold basic, and the busiest row in this table. It is a modal ATTACK - the first
                // printed face in the build to consume milestone 8.35z27's multi-sequence contract -
                // so its two bullets are two TARGETING SENTENCES rather than two effects, and the
                // enclosing Choose is what contributes ModalBulletChoice while each `attack-sequence@1`
                // leaf contributes nothing at all.
                //
                // THE BOARD-SPACE SHAPE ON THIS ROW IS NOT WHISPER'S TO SUPPLY, and that is the whole
                // point of RULING 16. Its step-7 rider makes the hero who DEFENDED perform a move, and
                // `defended-card-forced-move@1` offers the furthest space each of p.19's six directions
                // reaches to that hero's own seat. "Moving full distance" removes p.11's "up to", so
                // there are at most six candidates and no intermediate space among them.
                Face(WhisperCardContent.SwiftJustice, "Swift Justice", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SwiftJusticeJourney),
                        Prove(BeforeAttackTextPause, SwiftJusticeJourney),
                        Prove(ModalBulletChoice, SwiftJusticeJourney),
                        Prove(AfterAttackTextPause, ForcedMoveJourney),
                        Prove(BoardSpaceSelection, ForcedMoveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".SwiftJustice_BindsItsTargetInCardText_AndDeclaresTwoSequenceBullets",
                        "WhisperVerifiedContentTests" +
                            ".SwiftJustice_ComposesItsRiderAsAnIfAbleForcedMove",
                        "WhisperVerifiedContentTests" +
                            ".SwiftJustice_IsTheOnlyFaceCarryingTwoCardTextMoments",
                        "WhisperVerifiedContentTests" +
                            ".SwiftJusticesBullets_PublishTheirOwnPrintedSentences"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it. Its composition is deferred
                // besides, under the standing 2026-08-21 decision that parks one face of every
                // verified hero.
                UnreachableFace(WhisperCardContent.VileAccusation, "Vile Accusation",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its text composition carries a named deferral besides"),

                // Her Silver basic and only basic Skill. It prints NO Movement icon at all, which is a
                // real constraint on play because it sits in her opening hand - and it is why this row
                // carries no secondary-movement shape where every other dealt face of hers does.
                //
                // ITS TWO BULLETS ARE PROVED BY TWO JOURNEYS AND THE REASON IS THE PRINTED CEILING.
                // The modal is "choose one" until Grim Reaper is unlocked, so one play takes one
                // bullet: the immunity journey takes the first and the Tier III journey takes BOTH,
                // which is what the Ultimate turns "or both" into on this card. The retrieve's own
                // `may` and its hand-card question therefore rest on the arm where they are reachable
                // in the same play as the other bullet, rather than on a second Tier I journey that
                // would prove the same two controls a turn later.
                Face(WhisperCardContent.DeathSeeker, "Death Seeker", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        // Milestone 8.36f3. The turn-one journey drives the SAME control and is named
                        // here rather than left unaccounted. RULING 53 resolved the earlier fork: the
                        // legal do-nothing Hold path is active coverage, not a parked exception.
                        Prove(PrimarySkillControl, DeathSeekerTurnOne),
                        Prove(PrimarySkillControl, ImmunityJourney),
                        Prove(ModalBulletChoice, ImmunityJourney),
                        Prove(ActiveEffectReading, ImmunityJourney),
                        Prove(ConfirmOrDecline, DeathSeekerBothJourney),
                        Prove(HandCardSelection, DeathSeekerBothJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests.DeathSeeker_GatesItsModalOnAnEnemyDiscard",
                        "WhisperVerifiedContentTests" +
                            ".DeathSeeker_IsImmuneToEveryActivity_OfEnemyHeroesOnly",
                        "WhisperVerifiedContentTests" +
                            ".DeathSeekersImmunity_ReachesEveryNamedActivity"
                    }),

                // The Red line's step-1 family: move, then name the unit beside the space the move
                // left her in. All three compose the identical seam and differ only in what pays for
                // the step - a boolean here, a counted budget with a printed ceiling on the other two.
                //
                // CRIMSON TRAIL'S CONDITIONAL AND ITS INPUT SHAPE HAVE SEPARATE WITNESSES. Milestone
                // 8.35z49 corrected the source-level binding walk so the boolean gate can fall through
                // to the mandatory target step. Sanguine Path drives the identical move-then-target
                // player interaction with a counted budget; the authoritative scenarios below pin the
                // Conditional-specific branch rather than asking one UI journey to re-prove it.
                Face(WhisperCardContent.CrimsonTrail, "Crimson Trail", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RedStepJourney),
                        Prove(BeforeAttackTextPause, RedStepJourney),
                        Prove(BoardSpaceSelection, RedStepJourney),
                        Prove(BoardUnitSelection, RedStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".CrimsonTrail_GatesOnlyItsStep_OnThePresenceOfASpawnPoint",
                        "WhisperVerifiedContentTests" +
                            ".TheThreeRedMovers_ComposeMoveThenTarget_AndBindTheirTargetInCardText"
                    }),

                Face(WhisperCardContent.SanguinePath, "Sanguine Path", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RedStepJourney),
                        Prove(AdjacentAttackTarget, RedStepUnavailableJourney),
                        Prove(AdjacentAttackTarget, RedStepZeroBudgetJourney),
                        Prove(BeforeAttackTextPause, RedStepJourney),
                        Prove(BoardSpaceSelection, RedStepJourney),
                        Prove(BoardUnitSelection, RedStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".SanguinePathAndBloodPilgrimage_DeclareTheirPrintedMaximum"
                    }),

                // The Red line's step-7 family. Their rider carries no board question of its own: the
                // repeat is `if able` and mandatory where a target exists, so what a player observes is
                // p.14 starting again rather than a control being pressed - the same pairing
                // Silverarrow's Rain of Arrows row records, and the reason RepeatDecision sits here
                // beside no ConfirmOrDecline.
                Face(WhisperCardContent.BloodFury, "Blood Fury", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ChainJourney),
                        Prove(AfterAttackTextPause, ChainJourney),
                        Prove(RepeatDecision, ChainJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".BloodFuryAndBloodFrenzy_GateMandatoryRepeatsOnTheEnemyDiscard",
                        "WhisperVerifiedContentTests" +
                            ".TheTwoRedRepeaters_TakeThePagesOwnTargetQuestion"
                    }),

                Face(WhisperCardContent.BloodPilgrimage, "Blood Pilgrimage", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RedStepJourney),
                        Prove(BeforeAttackTextPause, RedStepJourney),
                        Prove(BoardSpaceSelection, RedStepJourney),
                        Prove(BoardUnitSelection, RedStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".SanguinePathAndBloodPilgrimage_DeclareTheirPrintedMaximum"
                    }),

                // The face RULING 34 is about, and the only one in the roster where "on different
                // targets" can diverge from what the engine enforces. Its row is Blood Fury's exactly,
                // because the divergence is a bound on the chain rather than a shape a player supplies.
                Face(WhisperCardContent.BloodFrenzy, "Blood Frenzy", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ChainJourney),
                        Prove(AfterAttackTextPause, ChainJourney),
                        Prove(RepeatDecision, ChainJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".BloodFrenzy_IsTheFirstAttackRepeat_WhoseDistinctTargetMaximumIsAboveOne",
                        "WhisperVerifiedContentTests" +
                            ".NoSkillRepeat_DeclaresADistinctTargetBindingNothingCanEnforce"
                    }),

                // The Blue displacement family. Two printed clauses and each is one click: the unit
                // standing on the Spawn point, then where it goes. Three rows and one journey, which is
                // what the gate's "need not duplicate permutations" sentence is for - they differ only
                // in how far, and Seeds of Fear's exact one space against its upgrades' "up to" is a
                // difference in the OFFER rather than in what a player supplies.
                Face(WhisperCardContent.SeedsOfFear, "Seeds of Fear", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SeedsJourney),
                        Prove(BoardUnitSelection, SeedsJourney),
                        Prove(BoardSpaceSelection, SeedsJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheThreeBlueDisplacements_NarrowToSpawnPointOccupants"
                    }),

                Face(WhisperCardContent.SproutsOfPanic, "Sprouts of Panic", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SeedsJourney),
                        Prove(BoardUnitSelection, SeedsJourney),
                        Prove(BoardSpaceSelection, SeedsJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheThreeBlueDisplacements_NarrowToSpawnPointOccupants"
                    }),

                // The two opponent-chosen faces, and the pair of rows this table exists to make
                // readable. Whisper picks WHICH enemy hero (her own board click); that hero picks the
                // BULLET (their own control, routed to their seat by 8.35r's decision-owner projection
                // reading the pending choice's chooser); and bullet 2's effect then happens on
                // Whisper's side of the board, performed by her.
                //
                // LESSER EVIL CARRIES ConfirmOrDecline AND GREATER GOOD DOES NOT, which is the printed
                // difference showing up in the derivation with nothing added by hand: the Tier II
                // prints "You MAY defeat a minion" and the Tier III prints "You defeat up to 3
                // minions". The election is gone at Tier III and the count is Whisper's and may be
                // zero, which is a bound on the selection rather than a control.
                Face(WhisperCardContent.LesserEvil, "Lesser Evil", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, OpponentModalJourney),
                        Prove(ModalBulletChoice, OpponentModalJourney),
                        Prove(BoardUnitSelection, OpponentModalJourney),
                        Prove(ConfirmOrDecline, OpponentModalJourney),
                        // THE HAND-CARD QUESTION IS PROVED ON THE SIBLING FACE, and that is stated
                        // rather than hidden. This journey's subject is RULING 17's escape hatch - the
                        // enemy TAKING the bullet that does nothing to them - and a modal takes one
                        // bullet per play, so the price bullet's `synthetic-discard-chosen-card@2`
                        // question is driven by Greater Good's journey, which prints the identical
                        // clause with "or is defeated" appended and takes it. A weaker attribution
                        // said out loud is worth more than a strong one that is not true.
                        Prove(HandCardSelection, GreaterGoodJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".LesserEvilAndGreaterGood_PutTheirModalToTheHeroWhisperNamed",
                        "WhisperVerifiedContentTests" +
                            ".LesserEvilsFirstBullet_IsEmptyRatherThanBlocked_SoRuling17OffersIt",
                        "WhisperVerifiedContentTests.LesserEvilKeepsItsElection_AndGreaterGoodDoesNot"
                    }),

                Face(WhisperCardContent.BloomingNightmare, "Blooming Nightmare", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SeedsJourney),
                        Prove(BoardUnitSelection, SeedsJourney),
                        Prove(BoardSpaceSelection, SeedsJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheThreeBlueDisplacements_NarrowToSpawnPointOccupants"
                    }),

                Face(WhisperCardContent.GreaterGood, "Greater Good", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GreaterGoodJourney),
                        Prove(ModalBulletChoice, GreaterGoodJourney),
                        Prove(BoardUnitSelection, GreaterGoodJourney),
                        Prove(HandCardSelection, GreaterGoodJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".GreaterGood_EscalatesToADefeat_WhereLesserEvilSkipsSilently",
                        "WhisperVerifiedContentTests" +
                            ".GreaterGood_ChoosesUpToThreeMinions_AndZeroIsAnAnswer",
                        "WhisperVerifiedContentTests.BothOpponentChosenFaces_AdmitMinionsOfEitherTeam"
                    }),

                // The Green Place family. One click on a space, and what differs between the three is
                // which spaces the filter admits - the two lower Tiers admit empty minion Spawn points
                // in the Battle Zone and Creeping Shadow additionally admits the ring beside one. That
                // is a difference in the candidate set and not in the input, which is the same line
                // milestone 8.35z38 drew for Misa's ring against her disk.
                Face(WhisperCardContent.ShadowStep, "Shadow Step", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShadowStepJourney),
                        Prove(BoardSpaceSelection, ShadowStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheThreeGreenPlaces_AdmitOnlyBattleZoneMinionSpawnPoints",
                        "WhisperVerifiedContentTests.ShadowStepAndShadowWalk_ComposeTheIdenticalBody"
                    }),

                Face(WhisperCardContent.ShadowWalk, "Shadow Walk", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShadowStepJourney),
                        Prove(BoardSpaceSelection, ShadowStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests.ShadowStepAndShadowWalk_ComposeTheIdenticalBody"
                    }),

                // The two Swaps. Their one selection spans a UNION of two differently reached sets -
                // "a unit adjacent to you, or an enemy hero in range with a card in the discard" - and
                // it is one question rather than two, because the face offers one choice. Composing it
                // as a modal would have invented a "which kind of swap?" control the card never prints,
                // and this row would then have carried a ModalBulletChoice no player is ever shown.
                Face(WhisperCardContent.CruelTwist, "Cruel Twist", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(BoardSpaceSelection, SwapJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".CruelTwistAndSealedFate_ComposeOneSelectionOverTheUnion_ThenAnUpToMove"
                    }),

                Face(WhisperCardContent.CreepingShadow, "Creeping Shadow", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShadowStepJourney),
                        Prove(BoardSpaceSelection, ShadowStepJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheThreeGreenPlaces_AdmitOnlyBattleZoneMinionSpawnPoints"
                    }),

                Face(WhisperCardContent.SealedFate, "Sealed Fate", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapJourney),
                        Prove(BoardUnitSelection, SwapJourney),
                        Prove(BoardSpaceSelection, SwapJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".CruelTwistAndSealedFate_ComposeOneSelectionOverTheUnion_ThenAnUpToMove"
                    }),

                // p.16's permanent ability, and the shortest row in this table. It prints no
                // Initiative, no icons and no action at all, so it composes no action definition -
                // which is why there is no commitment row and no secondary anything here. What it does
                // is raise a NUMBER on two of her own basic cards, and a raised ceiling is not a shape:
                // a player answers a two-bullet modal exactly the way they answer a one-bullet one.
                // What changes is what the answer produces, and that is what its journeys measure.
                Face(WhisperCardContent.GrimReaper, "Grim Reaper", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".GrimReaper_RaisesChoiceCardinalityToTwo_OnBasicActionsOnly",
                        "WhisperVerifiedContentTests" +
                            ".GrimReaper_WidensOnlyAModalItsOwnHeroIsChoosingOnABasicCard",
                        "WhisperVerifiedContentTests" +
                            ".ChoiceCardinality_IsBoundedByThePrintedOptions_AndNeverLowers"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("whisper-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests" +
                            ".TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard"
                    }),

                // Restore, Undo, replay and restart, proved across THE OPPONENT-CHOSEN MODAL - and this
                // is the one place in this table where the pause was chosen for a reason no earlier
                // hero could give.
                //
                // Milestone 8.35z34 found that a node asking TWO questions was not restore-safe,
                // because a restored pause re-enters through Execute rather than through Resume.
                // Whisper's opponent-chosen modal asks ONE question, so it is not that shape - but it
                // is a pause whose OWNER is not the acting seat, and 8.35r's decision-owner routing has
                // never been exercised live against an `EffectChooserRole.BoundHero` chooser in any
                // milestone. Her phase-one worker traced it through the code and said so in as many
                // words. What a restore has to bring back here is therefore not only the pause but
                // WHOSE it is, and a restore that re-opened it owned by Whisper would look exactly like
                // a working restore from her side of the table.
                Component("whisper-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, ModalSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, ModalSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, ModalSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[]
                    {
                        "WhisperVerifiedContentTests.ABoundChooserModal_MustBindTheHeroItNames"
                    }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. Hers is a real test of it rather than a formality: Swift Justice carries
                // p.10's modal form AND a p.14 timing keyword INSIDE one of its bullets - the only face
                // in the build that does - and Lesser Evil carries a parenthetical reminder clause on
                // top of two bullets, which is the longest Blue text a verified face prints.
                Component("whisper-card-footprint",
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
                // reaches it, for the reason milestones 8.20a through 8.35z38 recorded against the
                // identical rows the eight heroes before her owe: the item track is derived progression
                // state, `DEV-004` composes card locations only, and no scene content stages a position
                // where a Level Up is offered.
                AbsentComponent("whisper-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; her twelve item icons " +
                    "are pinned face by face by WhisperVerifiedContentTests." +
                    "EveryPrintedFace_MatchesTheSourceInventory"),

                // She owns no token and no marker, and that is content rather than an omission -
                // ledger question 14 rules that a hero's tokens are defined by that hero's card faces,
                // and hers name none. Her own content file states it outright: "no token or marker on
                // any face - so this milestone adds no component content at all". What her kit moves
                // instead are units and Spawn points the board already has.
                AbsentComponent("whisper-tokens-and-markers", "hero-owned tokens and markers",
                    "Whisper's nineteen faces place no token and give no marker; her Blue line moves " +
                    "units p.8 already owns, her Green line moves her onto Spawn points the map " +
                    "already declares, and her one persistent text raises an Active effect rather " +
                    "than a piece")
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
        public void EveryWhisperFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesWhispersPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(WhisperPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(WhisperPlayerPathGateTests).Assembly);

        [Test]
        public void EveryWhisperJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(WhisperUiJourneyPlayModeTests),
                typeof(WhisperEngagedUiJourneyPlayModeTests),
                typeof(WhisperProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndWhisperActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "whisper-items",
                WhisperCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void WhisperPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Whisper's nineteen faces place no token and give no marker");

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
