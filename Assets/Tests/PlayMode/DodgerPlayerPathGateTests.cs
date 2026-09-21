using System;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Dodger the Warlock's half of the permanent hero player-path completion gate. Milestone
    /// 8.35z34, and the SEVENTH hero to take it - the first from outside the mapped pitch roster the
    /// gate was built against.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the six gates before it; the table
    /// below is Dodger's own. This milestone added one interaction shape and six capability
    /// classifications, and the offline derivation printed every Wasp, Arien, Brogan, Sabina,
    /// Tigerclaw and Xargatha row byte-identical afterwards - which is the test milestone 8.20b set
    /// for whether an addition is a generalisation or a re-fit.</para>
    ///
    /// <para><b>The new shape is <see cref="RespawnedMinionTypeChoice"/>, and RULING 48 is what put
    /// it there.</b> Her two Respawns ask which empty space in reach, and - at a Spawn point her
    /// Ultimate granted rather than the map printed - which minion type comes back. The second
    /// question picks among miniatures in the SUPPLY, so it is not a board click and cannot be
    /// classified as one: a row claiming <see cref="BoardUnitSelection"/> for it would name a journey
    /// in which a player clicks a piece, and there is no piece to click.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="DodgerCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and the guard compares the
    /// derivation against the row exactly. Every row below was written FROM that derivation rather than
    /// checked against it afterwards: milestone 8.35z34 ran it offline, per face, before a line of this
    /// table existed.</para>
    ///
    /// <para><b>Her nine spawn-point faces ask a player for nothing extra, and that is why those rows
    /// are short.</b> "If you are adjacent to an empty spawn point in the battlezone" and "if there are
    /// 2 or more empty spawn points in radius" are readings of the board that gate a step or raise a
    /// Defense value. A player who plays Vampiric Shield is shown no control for the count; what the
    /// count does is happen. That is the same reading Sabina's presence count and Tigerclaw's terrain
    /// question already have.</para>
    ///
    /// <para><b>Three faces print a conditional Defense value and none of them is a step.</b> p.14
    /// prints the <c>+</c> on the number itself and says what it means, so Shield of Decay, Vampiric
    /// Shield and Aegis of Doom introduce a <see cref="PrimaryDefenseOffer"/> and nothing else - the
    /// value is inside p.14's own total. She is the first hero in this build to print one on a
    /// Defense.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class DodgerPlayerPathGateTests
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
        /// same input whichever face is picked up, and the engaged and progressed journeys commit
        /// headlessly by design so their own subjects are the cards rather than the commitment.
        /// </summary>
        private const string TrayCommitAndWalk =
            "Dodger_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnDodgersOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesDodgersCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryDodgerFaceAtTheStandardCardFootprint";

        private const string ModalAttackJourney =
            "DreadRazor_OffersItsTwoBulletsAndTakesTheUnitTheChosenOneAdmits";

        /// <summary>
        /// Milestone 8.35z45. Middlefinger of Death does NOT share the journey above, and
        /// the reason is content rather than coverage appetite.
        ///
        /// <para>Her three "Choose one" modals take exactly one bullet, close on that click, and bind
        /// a single target through card text - so what a player answers is an effect selection and
        /// <c>PendingAttackTargetChoice</c> stays null throughout. "Choose one <em>or both</em>" is a
        /// different set of controls end to end: the modal stays open after the first bullet, it is
        /// ended by a Confirm, and each chosen bullet then opens p.14's OWN target prompt. A row that
        /// pointed at the journey above would be claiming those controls are proved when the journey
        /// never meets them.</para>
        ///
        /// <para><b>Milestone 8.70 renamed it for what it now shows.</b> On this staging the second
        /// bullet's pile clause names nobody when the modal is answered, and from that milestone a
        /// bullet naming nobody is GREYED at the choice on the user's ruling - so the journey drives
        /// one live bullet, a greyed one drawn beside it, the printed ceiling of two unmoved, the
        /// Confirm that ends a modal below its ceiling, and p.14's own target prompt behind it. Every
        /// control this row claims is still met; what changed is which of them the board supports.</para>
        /// </summary>
        private const string ChooseBothJourney =
            "MiddlefingerOfDeath_GreysTheBulletWithNoSubjectAndOpensACompletePageFourteenForTheOther";

        private const string DeathTrapJourney =
            "DeathTrap_NamesAnEnemyBesideAnEmptySpawnPointAndTakesACardFromTheirHand";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndDodgerSurvivesTheAttack";

        /// <summary>
        /// The Defense journey plays AEGIS OF DOOM rather than Shield of Decay, and the three Defense
        /// rows share it because the shape genuinely is one shape - which is what the gate's own
        /// "need not duplicate permutations" sentence permits.
        ///
        /// <para>Why the Tier III face rather than the Tier I one in her opening hand: the printed
        /// <c>+</c> is only observable where the board satisfies it, and milestone 8.35z34 measured
        /// that every minion Spawn point in the Battle Zone is occupied at the engaged staging. On the
        /// untouched board all three of her Defense values are their plain printed numbers, so a
        /// journey there would prove the offer and nothing about the clause. With Tide of Darkness up,
        /// every empty space counts, and the raise is real.</para>
        /// </summary>
        private const string PrimaryDefenseJourney =
            "AegisOfDoom_IsOfferedAsAWholeFaceAndItsPrintedPlusIsRaisedByHerUltimate";

        private const string DefeatAndRespawn =
            "Dodger_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        private const string SkullRiderJourney =
            "BurningSkull_ShovesTheMinionItJustHitAndIsAnsweredOnItsOwnConfirm";

        private const string RitualJourney =
            "DarkRitual_IsStartedFromItsOwnSkillControlAndPaysOnTheBoardItReads";

        private const string DrainJourney =
            "Weakness_StandsWhereAPlayerReadsItAndDrainsTheAttackTotalInRadius";

        private const string RestrictionJourney =
            "Enfeeblement_GreysTheRepeatControlOfAnEnemyWhoStartedInsideHerRadius";

        private const string RespawnJourney =
            "Necromastery_RespawnsIntoAGrantedSpaceAndAsksWhichMinionTypeComesBack";

        private const string RespawnSurvivesUndoAndRestore =
            "TheRespawnsTypeChoice_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string UltimateJourney =
            "TideOfDarkness_IsDrawnFaceupOnHerRailAndTurnsHerCountIntoEmptySpaces";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_DODGER.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Dodger",
            DodgerCardContent.AllDefinitions,
            DodgerCardContent.ReturnedToBoxAtSetup,
            DodgerActiveEffectContent.Handlers(),
            // Her Ultimate is p.16's PERMANENT ability and triggers no effect at all, so there is
            // nothing to pass here. That is content rather than an omission: Tide of Darkness starts
            // no step and asks nobody anything, and its row below says exactly that.
            Array.Empty<GuardsDigital.Core.Effects.EffectDefinition>(),
            new[]
            {
                // Her Gold basic, and the first verified basic Attack whose reach is bought by a board
                // condition rather than printed outright. Both bullets take a unit, so the modal is a
                // choice between two printed sentences and the board click that follows is the same
                // click either way - which is why one journey proves both.
                Face(DodgerCardContent.DreadRazor, "Dread Razor", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalAttackJourney),
                        Prove(BeforeAttackTextPause, ModalAttackJourney),
                        Prove(ModalBulletChoice, ModalAttackJourney),
                        Prove(BoardUnitSelection, ModalAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests" +
                            ".TheComposedModalAttacks_BindTheirTargetAtPageFourteensStepOne",
                        "DodgerVerifiedContentTests.EveryAttackSheOwns_IsRanged"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(DodgerCardContent.RustyRazor, "Rusty Razor",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes - and its text composition carries a named deferral besides"),

                // Her Silver basic and only basic Skill. It prints NO Movement icon at all, which is a
                // real constraint on play because it sits in her opening hand - and it is why this row
                // carries no secondary-movement shape where every other dealt face of hers does.
                Face(DodgerCardContent.DeathTrap, "Death Trap", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DeathTrapJourney),
                        Prove(BoardUnitSelection, DeathTrapJourney),
                        Prove(HandCardSelection, DeathTrapJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests.DeathTrap_IsHerOnlyBasicSkill_AndOffersNoMovement",
                        "EmptyMinionSpawnPointCapabilityTests" +
                            ".TheSpawnPointNarrowing_OffersOnlyHeroesStandingBesideAQualifyingPoint",
                        "EmptyMinionSpawnPointCapabilityTests" +
                            ".TheSpawnPointNarrowing_IsMeasuredFromTheCandidateAndNotTheActor"
                    }),

                // The three composed modal Ranged Attacks. They print different numbers and introduce
                // the same shapes, which is exactly what the gate's own "need not duplicate
                // permutations" sentence permits - the face-specific half of the evidence is the
                // authoritative-scenario column, which is per face by construction.
                Face(DodgerCardContent.LittlefingerOfDeath, "Littlefinger of Death", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalAttackJourney),
                        Prove(BeforeAttackTextPause, ModalAttackJourney),
                        Prove(ModalBulletChoice, ModalAttackJourney),
                        Prove(BoardUnitSelection, ModalAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests" +
                            ".EachSpawnPointClause_NamesTheReachItsOwnSentencePrints"
                    }),

                Face(DodgerCardContent.FingerOfDeath, "Finger of Death", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalAttackJourney),
                        Prove(BeforeAttackTextPause, ModalAttackJourney),
                        Prove(ModalBulletChoice, ModalAttackJourney),
                        Prove(BoardUnitSelection, ModalAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory"
                    }),

                // The two skull riders. Their "after the attack" text shoves a minion out of her ring,
                // and the printed "up to" is what opens the confirm-or-decline beside the space.
                Face(DodgerCardContent.BurningSkull, "Burning Skull", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalAttackJourney),
                        Prove(AfterAttackTextPause, SkullRiderJourney),
                        Prove(ConfirmOrDecline, SkullRiderJourney),
                        Prove(BoardUnitSelection, SkullRiderJourney),
                        Prove(BoardSpaceSelection, SkullRiderJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests.TheTwoSkullRiders_ShoveAnyAdjacentMinionOutOfHerRing"
                    }),

                // Her Red Tier III, and the LAST face of hers to become reachable. Milestone
                // 8.35z45 added the per-candidate predicate its second bullet prints, so
                // the row moved from UnreachableFace to a proved one.
                //
                // It does NOT share the modal journey the other three take, and that is content rather
                // than thoroughness: they print "Choose one" and bind a single target through card
                // text, so a player answers an effect selection and PendingAttackTargetChoice stays
                // null. This one prints "one or both", so the modal stays open across the first click,
                // ends on a Confirm, and opens p.14's OWN target prompt once per sequence. Four of its
                // seven paths therefore need their own journey.
                Face(DodgerCardContent.MiddlefingerOfDeath, "Middlefinger of Death", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ChooseBothJourney),
                        Prove(BeforeAttackTextPause, ChooseBothJourney),
                        Prove(ModalBulletChoice, ChooseBothJourney),
                        // NO BoardUnitSelection. This row declared one until the gate's first run on
                        // the composed face, and the content does not introduce it: each bullet binds
                        // its victim through p.14's OWN target prompt - the printed Attack shape
                        // already on this row - rather than through a `choose-target` selection, which
                        // is what her "Choose one" faces use. Declaring both would claim two controls
                        // where a player is shown one.
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "MiddlefingerOfDeathTests" +
                            ".TheSecondBullet_IsGreyedWithAnEmptyDiscard_WhereTheFirstOffersThatSameHero",
                        "MiddlefingerOfDeathTests.ChoosingBoth_RunsACompletePageFourteenPerBullet",
                        "MiddlefingerOfDeathTests" +
                            ".APauseMidChain_RestoresByteIdenticallyAndStillOwesItsSecondSequence",
                        "MiddlefingerOfDeathTests.Enfeeblement_LeavesAChooseBothAttackWhole"
                    }),

                Face(DodgerCardContent.BlazingSkull, "Blazing Skull", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalAttackJourney),
                        Prove(AfterAttackTextPause, SkullRiderJourney),
                        Prove(ConfirmOrDecline, SkullRiderJourney),
                        Prove(BoardUnitSelection, SkullRiderJourney),
                        Prove(BoardSpaceSelection, SkullRiderJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests.TheTwoSkullRiders_ShoveAnyAdjacentMinionOutOfHerRing"
                    }),

                // The three Blue Defense primaries. p.14 prints the `+` on the number and names what it
                // means, so the whole sentence is a value declaration and there is no step: what a
                // player does is answer an attack with the face, and what the board does is decide the
                // number. She is the first hero in this build to print a conditional Defense value.
                Face(DodgerCardContent.ShieldOfDecay, "Shield of Decay", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryDefenseOffer, PrimaryDefenseJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests" +
                            ".TheThreeConditionalDefenseValues_DeclareTheirOwnAmountAgainstOneQuery",
                        "DodgerPrintedSetTests.TheConditionalDefense_IsAThresholdAndNotAScale"
                    }),

                Face(DodgerCardContent.VampiricShield, "Vampiric Shield", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryDefenseOffer, PrimaryDefenseJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerPrintedSetTests" +
                            ".VampiricShield_AddsItsPrintedPlusTwo_ExactlyAsPageFourteenWorksIt",
                        "DodgerPrintedSetTests.BelowItsPrintedThreshold_TheCardTextAddsNothing"
                    }),

                // The two drains. Their Active effect is read where it stands, and Enfeeblement's is
                // additionally met by the enemy as a control of their own - which is the extra shape on
                // its row and the whole of what RULING 14 scoped.
                Face(DodgerCardContent.Weakness, "Weakness", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DrainJourney),
                        Prove(ActiveEffectReading, DrainJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerRuledFacesTests" +
                            ".WeaknessAndEnfeeblement_DrainTheAttackTotalByTheirPrintedNumbers",
                        "DodgerRuledFacesTests.TheAttackTotal_HasNoFloorAtZero"
                    }),

                Face(DodgerCardContent.AegisOfDoom, "Aegis of Doom", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryDefenseOffer, PrimaryDefenseJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerPrintedSetTests.TheBonus_AppliesWithDodgerHerselfOutsideTheBattleZone",
                        "DodgerPrintedSetTests.TheBonus_IsNotBoughtByAHeroSpawnPoint"
                    }),

                Face(DodgerCardContent.Enfeeblement, "Enfeeblement", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, DrainJourney),
                        Prove(ActiveEffectReading, DrainJourney),
                        Prove(OpponentRestrictionReading, RestrictionJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerRuledFacesTests.Enfeeblement_ForbidsOnlyThePageTenRepeat",
                        "DodgerRuledFacesTests.TheRepeatActivity_MapsFromNoActionKind"
                    }),

                // The three rituals. Each reads the board and pays; none of them asks a player
                // anything, which is why all three rows are the plain Skill shape. Darkest Ritual's
                // item clause is a consequence whose recipient the card names.
                Face(DodgerCardContent.DarkRitual, "Dark Ritual", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RitualJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests" +
                            ".TheTwoComposedRituals_GateTheirCoinsOnTheThresholdTheyPrint"
                    }),

                Face(DodgerCardContent.DarkerRitual, "Darker Ritual", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RitualJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerVerifiedContentTests" +
                            ".TheTwoComposedRituals_GateTheirCoinsOnTheThresholdTheyPrint"
                    }),

                // The two Respawns, and the only faces of hers that ask two questions on one node.
                // RULING 48: the destination is any empty space in reach once her Ultimate is up, and
                // at a granted point the type comes from p.19's own eligibility count - which is a
                // control among supply miniatures rather than a board click.
                Face(DodgerCardContent.Necromancy, "Necromancy", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RespawnJourney),
                        Prove(BoardSpaceSelection, RespawnJourney),
                        Prove(RespawnedMinionTypeChoice, RespawnJourney),
                        Prove(RespawnedMinionTypeChoice, RespawnSurvivesUndoAndRestore),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerRespawnCapabilityTests" +
                            ".AtAGrantedPoint_TheActorIsAskedWhichEligibleTypeComesBack",
                        "DodgerRuledFacesTests" +
                            ".AtAGrantedPoint_TheTypeComesFromPageNineteensEligibilityCount"
                    }),

                Face(DodgerCardContent.DarkestRitual, "Darkest Ritual", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RitualJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerRuledFacesTests.DarkestRitual_GatesItsTwoClausesIndependently",
                        "DodgerRuledFacesTests" +
                            ".DarkestRitualsItem_IsUnboundedAndStacksInTheAttackSlot"
                    }),

                Face(DodgerCardContent.Necromastery, "Necromastery", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RespawnJourney),
                        Prove(BoardSpaceSelection, RespawnJourney),
                        Prove(RespawnedMinionTypeChoice, RespawnJourney),
                        Prove(RespawnedMinionTypeChoice, RespawnSurvivesUndoAndRestore),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "DodgerRespawnCapabilityTests" +
                            ".WithTheUltimate_TheDestinationsAreEveryEmptySpaceInReach",
                        "DodgerRuledFacesTests" +
                            ".PageNineteensCount_NeverFavoursAHeavyWhileItsMiniatureStands"
                    }),

                // p.16's Ultimate. It carries no action and triggers no effect - what it does is be
                // true - so the only thing it introduces is being visible where a player can see it.
                // That is the shortest face row in this build, and it is content: the master switch
                // behind nine of her faces asks nobody anything.
                Face(DodgerCardContent.TideOfDarkness, "Tide of Darkness", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney)
                    },
                    new[]
                    {
                        "DodgerRuledFacesTests.TideOfDarkness_ComposesOnePermanentRuleAndNoAction",
                        "DodgerRuledFacesTests.TheUltimate_TurnsTheCountIntoEmptySpacesInRadius"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("dodger-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[]
                    {
                        "DodgerPrintedSetTests.SheIsDealtByAConfiguredMatch"
                    }),

                // Restore, Undo, replay and restart, proved across her Respawn's type choice - the one
                // decision of hers a player is asked in the middle of a step that has already started
                // committing, and therefore the pause whose re-derivation has something to be wrong
                // about. Milestone 8.35i's corrective is why that is the pause worth choosing.
                Component("dodger-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, RespawnSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, RespawnSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, RespawnSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "DodgerPrintedSetTests.SheIsDealtByAConfiguredMatch" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. Hers is a real test of it rather than a formality: Dread Razor and
                // Middlefinger of Death each carry a modal with two printed bullets, and Darkest
                // Ritual carries two independent clauses and a parenthesised reminder.
                Component("dodger-card-footprint",
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
                // reaches it, for the reason milestones 8.20a through 8.24 recorded against the
                // identical rows the other six heroes owe: the item track is derived progression
                // state, `DEV-004` composes card locations only, and no scene content stages a
                // position where a Level Up is offered.
                //
                // Hers carries one thing theirs do not, and it is recorded rather than journeyed:
                // Darkest Ritual grants an item from CARD TEXT, which is a second route onto the same
                // track that p.15's procedure is not needed for. What that route does is pinned at the
                // authoritative tier by the scenario this row links.
                AbsentComponent("dodger-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; her ten item icons are " +
                    "pinned face by face by DodgerVerifiedContentTests." +
                    "EveryPrintedFace_MatchesTheSourceInventory, and her card-text grant by " +
                    "DodgerRuledFacesTests.DarkestRitualsItem_IsUnboundedAndStacksInTheAttackSlot"),

                // She owns no token and no marker, and that is content rather than an omission -
                // ledger question 14 rules that a hero's tokens are defined by that hero's card faces,
                // and hers name none. What her kit moves instead are the standard minions the board
                // already has.
                AbsentComponent("dodger-tokens-and-markers", "hero-owned tokens and markers",
                    "Dodger's nineteen faces place no token and give no marker; her Respawns put a " +
                    "standard minion miniature back onto the board, which is a piece p.8 already " +
                    "owns rather than a component of hers")
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
        public void EveryDodgerFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesDodgersPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(DodgerPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(DodgerPlayerPathGateTests).Assembly);

        [Test]
        public void EveryDodgerJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(DodgerUiJourneyPlayModeTests),
                typeof(DodgerEngagedUiJourneyPlayModeTests),
                typeof(DodgerProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row
        /// should go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndDodgerActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "dodger-items",
                DodgerCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void DodgerPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Dodger's nineteen faces place no token and give no marker");

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
