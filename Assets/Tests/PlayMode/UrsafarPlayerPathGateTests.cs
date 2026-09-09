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
    /// Ursafar the Savage's half of the permanent hero player-path completion gate. Milestone
    /// 8.35z50, and the TENTH hero to take it.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the eight gates before it. This
    /// milestone widened the derivation itself in three places his set is the first to force: a
    /// Movement PRIMARY's own "after the walk" text moment (<see cref="AfterMovementTextPause"/>, a
    /// new shape - his five Blue cards are this roster's first primary Movement action at all), the
    /// SECOND body of card text an Attack may print at p.14's other attacker moment (his Claws That
    /// Catch and Tear are the first faces to declare one), and three capability ids no dealt hero's
    /// content had reached before (<c>borrow-primary-action</c>, <c>synthetic-card-radius</c>,
    /// <c>attack-target-survived</c> - the last one this milestone's own new engine piece).</para>
    ///
    /// <para><b>Why this table was derived rather than written.</b> Nothing here declares what a face
    /// does; <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="UrsafarCardContent"/> and <see cref="UrsafarEffectContent"/> - action kind, origin,
    /// printed range, card-text timing (both moments), effect node kind, failure policy, capability
    /// identity. Every row below is written FROM that derivation, offline, the way milestone 8.35z34's
    /// method requires - and it is checked against the source rather than assumed, at
    /// <c>tmp/8-35d-8-35e-multiplayer-corrections/worker-ursadeal-prep/probes/gate/</c>.</para>
    ///
    /// <para><b>THE BORROW DISPATCH IS COMPLETE, and this paragraph is corrected rather than deleted
    /// because its earlier reading was written before the dispatch existed.</b> The prep branch this
    /// table came from recorded the dispatch as partial - the four <c>TextGranted</c> guards widened
    /// and the offer's card-instance choice built, but nothing to START the borrowed action once that
    /// choice is answered. This milestone built the rest: <c>BorrowedActionQuery</c> reads the
    /// published card off the completed frame and <c>StartOwedBorrowedAction</c> dispatches it through
    /// this authority's own public entry points, at the same turn boundary an owed repeat is
    /// performed. Angry Roar's, Instinctive Reaction's and Evolutionary Response's rows are therefore
    /// proved through the choice AND through what the choice starts.</para>
    ///
    /// <para><b>Ruling 2's build-up is proved structurally rather than by one journey per card.</b>
    /// Thirteen faces grant `enraged`; the grant is the card's own unconditional sibling on every one
    /// of them (never the conditional's child), so no journey needs to prove the build-up thirteen
    /// times - <see cref="UrsafarVerifiedContentTests"/> pins the shape at the definition tier, and
    /// this gate's journeys exercise it live on the two faces that most need it seen: Claws That
    /// Catch's own fizzle-versus-success pair, and Angry Roar's calm-still-grants case.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class UrsafarPlayerPathGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Journey names.
        // -----------------------------------------------------------------------------------------

        private const string Spawn =
            "HeroPiece_StandsOnUrsafarsOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string TrayCommitAndWalk =
            "Ursafar_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesUrsafarsCommitmentAndRestartsHimOntoHisStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEveryUrsafarFaceAtTheStandardCardFootprint";

        private const string DefeatAndRespawn =
            "Ursafar_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick";

        /// <summary>Claws That Catch: the fizzle arm and the move-then-target success arm.</summary>
        private const string ProwlJourney =
            "ClawsThatCatch_FizzlesCalmWithNoTargetAndMovesThenTargetsOnceEnraged";

        /// <summary>Angry Roar: calm still grants, enraged opens the borrow's own card choice.</summary>
        private const string RoarJourney =
            "AngryRoar_GrantsCalmAndOpensTheBorrowChoiceOnceEnraged";

        /// <summary>The removal family - Prey Drive drives it; Prey Abundance and Feeding Frenzy share it.</summary>
        private const string RemovalJourney =
            "PreyDrive_RemovesAnAdjacentEnemyMinionOnceEnragedAndTheTargetSurvived";

        /// <summary>The Green range-discard family - Sniff Out drives it; Eyes on the Prey shares it.</summary>
        private const string SniffJourney =
            "SniffOut_MakesTheEnemyInRangeDiscardOnceEnraged";

        /// <summary>Tear's Tier + 1 single spend, including the game-ending arm.</summary>
        private const string TearJourney =
            "Tear_SpendsTierPlusOneOnce_AndEndsTheGameOnTheLastCounter";

        /// <summary>The Blue swap/raise family - Unstoppable Force drives it directly.</summary>
        private const string UnboundFuryJourney =
            "UnboundFury_IsAlwaysEnraged_AndWidensAngryRoarsPoolToExpiredCards";

        /// <summary>The borrow's card-instance pause, answered again after an Undo and a restore.</summary>
        private const string ModalUndoRestore =
            "TheBorrowsCardChoice_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        /// <summary>The borrow continuation through a discarded printed Attack.</summary>
        private const string BorrowedAttackJourney =
            "InstinctiveReaction_PerformsADiscardedAttackWithoutMovingItsCard";

        /// <summary>The nested borrow continuation through a discarded printed Skill.</summary>
        private const string BorrowedSkillJourney =
            "InstinctiveReaction_PerformsADiscardedSkillAndReturnsThroughNestedParents";

        /// <summary>Apex Predator's escalation to Defeat when the mandatory discard cannot be paid.</summary>
        private const string ApexJourney =
            "ApexPredator_DefeatsTheEnemyInRange_WhenTheirHandIsAlreadyEmpty";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_URSAFAR.md's inventory table.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Ursafar",
            UrsafarCardContent.AllDefinitions,
            UrsafarCardContent.ReturnedToBoxAtSetup,
            // Milestone 8.35z50's first Play Mode run. These read `DeferredHandlers()` when they were
            // written, which was correct while he was composed and undeliverable - and became silently
            // WRONG at the moment this same milestone moved every one of those handlers across to
            // `RegisteredHandlers()`. The deferred lists are empty now, so the matrix received no
            // Active-effect handler at all and could not classify a single one of his thirteen
            // per-card enrage grants. A dealing that flips the registration owes this line with it.
            UrsafarActiveEffectContent.RegisteredHandlers()
                .Concat(GuardsDigital.Core.Effects.Synthetic.SyntheticConditionCapabilities
                    .RegisteredHandlers())
                .Concat(GuardsDigital.Core.Effects.Synthetic.SyntheticBorrowedActionCapabilities
                    .RegisteredHandlers())
                .Concat(GuardsDigital.Core.Effects.Synthetic.SyntheticAttackTargetSurvivalCapabilities
                    .RegisteredHandlers()),
            Array.Empty<EffectDefinition>(),
            new[]
            {
                // The Gold basic. Both p.14 attacker moments in one card - the first face this
                // roster's derivation has walked at both.
                Face(UrsafarCardContent.ClawsThatCatch, "Claws That Catch", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ProwlJourney),
                        Prove(BeforeAttackTextPause, ProwlJourney),
                        Prove(AfterAttackTextPause, ProwlJourney),
                        Prove(ConfirmOrDecline, ProwlJourney),
                        Prove(BoardSpaceSelection, ProwlJourney),
                        Prove(BoardUnitSelection, ProwlJourney),
                        Prove(ActiveEffectReading, ProwlJourney),
                        Prove(RoundLongActiveEffectReading, ProwlJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".BothGoldFaces_PutTheirGrantAfterTheAttack_SoAFizzledAttackNeverEnrages"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match.
                UnreachableFace(UrsafarCardContent.JawsThatBite, "Jaws That Bite",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes"),

                // The Silver basic and only basic Skill - his signature mechanic's first face, and
                // the roster's first `borrow-primary-action` consumer.
                Face(UrsafarCardContent.AngryRoar, "Angry Roar", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RoarJourney),
                        Prove(HandCardSelection, RoarJourney),
                        Prove(ActiveEffectReading, RoarJourney),
                        Prove(RoundLongActiveEffectReading, RoarJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "BorrowedPrimaryActionTests.ActivePool_BeforeTheUltimate_AdmitsOnlyCardsSourcingALiveActiveEffect",
                        "UrsafarVerifiedContentTests" +
                            ".UnboundFury_IsAStandingDeclaration_ReadByBothOfItsClauses"
                    }),

                // The Red line and its removal family. Prey Drive and Prey Abundance print identical
                // text; Feeding Frenzy's own row shares the mechanism at its printed count of 2.
                Face(UrsafarCardContent.PreyDrive, "Prey Drive", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RemovalJourney),
                        Prove(AfterAttackTextPause, RemovalJourney),
                        Prove(ConfirmOrDecline, RemovalJourney),
                        Prove(BoardUnitSelection, RemovalJourney),
                        Prove(ActiveEffectReading, RemovalJourney),
                        Prove(RoundLongActiveEffectReading, RemovalJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests.TheThreeRemovalReds_ComposeTheCompoundGateAndTheRemoval",
                        "RosterTargetGeometryCapabilityTests" +
                            ".EnclosingAttackOf_HeroTarget_ReadsSurvivalLiveFromBoardOccupancy",
                        "RosterTargetGeometryCapabilityTests" +
                            ".EnclosingAttackOf_MinionTarget_ReadsSurvivalLiveFromBoardOccupancy"
                    }),

                Face(UrsafarCardContent.PreyAbundance, "Prey Abundance", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RemovalJourney),
                        Prove(AfterAttackTextPause, RemovalJourney),
                        Prove(ConfirmOrDecline, RemovalJourney),
                        Prove(BoardUnitSelection, RemovalJourney),
                        Prove(ActiveEffectReading, RemovalJourney),
                        Prove(RoundLongActiveEffectReading, RemovalJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests.TheThreeRemovalReds_ComposeTheCompoundGateAndTheRemoval"
                    }),

                // Rip: the grant-only Red, proved as setup inside the removal journey (it enrages
                // Ursafar for Prey Drive's own turn) - the sibling-contract attribution
                // MisaPlayerPathGateTests' Blue riders record, on the identical shape.
                Face(UrsafarCardContent.Rip, "Rip", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RemovalJourney),
                        Prove(AfterAttackTextPause, RemovalJourney),
                        Prove(ActiveEffectReading, RemovalJourney),
                        Prove(RoundLongActiveEffectReading, RemovalJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.FeedingFrenzy, "Feeding Frenzy", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, RemovalJourney),
                        Prove(AfterAttackTextPause, RemovalJourney),
                        Prove(ConfirmOrDecline, RemovalJourney),
                        Prove(BoardUnitSelection, RemovalJourney),
                        Prove(ActiveEffectReading, RemovalJourney),
                        Prove(RoundLongActiveEffectReading, RemovalJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests.TheThreeRemovalReds_ComposeTheCompoundGateAndTheRemoval"
                    }),

                // Tear: RULING 35's whole subject, both card-text moments, and a victory-condition
                // clause - the second face to declare a second body of card text.
                Face(UrsafarCardContent.Tear, "Tear", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, TearJourney),
                        Prove(BeforeAttackTextPause, TearJourney),
                        Prove(AfterAttackTextPause, TearJourney),
                        Prove(ActiveEffectReading, TearJourney),
                        Prove(RoundLongActiveEffectReading, TearJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                // The Blue line - this roster's first PRIMARY Movement action, and
                // AfterMovementTextPause's first three consumers. Prowling Brute and Rampaging Beast
                // share Unstoppable Force's row; the additional-space variants are content the
                // printed face itself states.
                Face(UrsafarCardContent.ProwlingBrute, "Prowling Brute", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, UnboundFuryJourney),
                        Prove(AfterMovementTextPause, UnboundFuryJourney),
                        Prove(ConfirmOrDecline, UnboundFuryJourney),
                        Prove(BoardUnitSelection, UnboundFuryJourney),
                        Prove(ActiveEffectReading, UnboundFuryJourney),
                        Prove(RoundLongActiveEffectReading, UnboundFuryJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.RampagingBeast, "Rampaging Beast", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, UnboundFuryJourney),
                        Prove(AfterMovementTextPause, UnboundFuryJourney),
                        Prove(ConfirmOrDecline, UnboundFuryJourney),
                        Prove(BoardUnitSelection, UnboundFuryJourney),
                        Prove(BoardSpaceSelection, UnboundFuryJourney),
                        Prove(ActiveEffectReading, UnboundFuryJourney),
                        Prove(RoundLongActiveEffectReading, UnboundFuryJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.ColdIre, "Cold Ire", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, TearJourney),
                        Prove(BeforeMovementTextPause, TearJourney),
                        Prove(ActiveEffectReading, TearJourney),
                        Prove(RoundLongActiveEffectReading, TearJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.UnstoppableForce, "Unstoppable Force", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, UnboundFuryJourney),
                        Prove(AfterMovementTextPause, UnboundFuryJourney),
                        Prove(ConfirmOrDecline, UnboundFuryJourney),
                        Prove(BoardUnitSelection, UnboundFuryJourney),
                        Prove(BoardSpaceSelection, UnboundFuryJourney),
                        Prove(ActiveEffectReading, UnboundFuryJourney),
                        Prove(RoundLongActiveEffectReading, UnboundFuryJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.EyesOfFlame, "Eyes of Flame", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimaryMovementDestination, TearJourney),
                        Prove(BeforeMovementTextPause, TearJourney),
                        Prove(ActiveEffectReading, TearJourney),
                        Prove(RoundLongActiveEffectReading, TearJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                // The Green line. Sniff Out and Eyes on the Prey share the range-discard family;
                // Apex Predator's own row proves its escalation.
                Face(UrsafarCardContent.SniffOut, "Sniff Out", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SniffJourney),
                        Prove(BoardUnitSelection, SniffJourney),
                        // The discard itself, even though the enemy is the one who pays it - the same
                        // reading `synthetic-discard-chosen-card@1` already carries generically.
                        Prove(HandCardSelection, SniffJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                Face(UrsafarCardContent.EyesOnThePrey, "Eyes on the Prey", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SniffJourney),
                        Prove(BoardUnitSelection, SniffJourney),
                        Prove(HandCardSelection, SniffJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                // Instinctive Reaction: p.10's "choose one" over the borrow bullet and the retrieve
                // bullet - the SAME card-instance pause Angry Roar opens, over the discard pile
                // instead of the sideways dashboard.
                Face(UrsafarCardContent.InstinctiveReaction, "Instinctive Reaction", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ModalUndoRestore),
                        Prove(ModalBulletChoice, ModalUndoRestore),
                        Prove(HandCardSelection, ModalUndoRestore),
                        Prove(HandCardSelection, BorrowedAttackJourney),
                        Prove(HandCardSelection, BorrowedSkillJourney),
                        Prove(ConfirmOrDecline, ModalUndoRestore),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".EvolutionaryResponse_MayTakeBothBullets_WhereInstinctiveReactionTakesOne"
                    }),

                Face(UrsafarCardContent.ApexPredator, "Apex Predator", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ApexJourney),
                        Prove(BoardUnitSelection, ApexJourney),
                        Prove(HandCardSelection, ApexJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt"
                    }),

                // Evolutionary Response: the "or both" raiser - this roster's first, and the same
                // borrow/retrieve pair Instinctive Reaction carries, now able to take both.
                Face(UrsafarCardContent.EvolutionaryResponse, "Evolutionary Response", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ModalUndoRestore),
                        Prove(ModalBulletChoice, ModalUndoRestore),
                        Prove(HandCardSelection, ModalUndoRestore),
                        Prove(ConfirmOrDecline, ModalUndoRestore),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".EvolutionaryResponse_MayTakeBothBullets_WhereInstinctiveReactionTakesOne"
                    }),

                // p.16's permanent ability. It prints no action and no icons at all - the always-
                // enraged clause and the borrow-pool widening, both structural rather than performed.
                Face(UrsafarCardContent.UnboundFury, "Unbound Fury", new[]
                    {
                        Prove(UltimateRailVisibility, UnboundFuryJourney)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests" +
                            ".UnboundFury_IsAStandingDeclaration_ReadByBothOfItsClauses"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("ursafar-hero-piece", "his token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[]
                    {
                        "UrsafarVerifiedContentTests.NineteenFaces_EighteenDealt_AndOneReturnedToTheBox"
                    }),

                // Restore, Undo, replay and restart, proved across the borrow's own card-instance
                // pause - the second instance of the z34 two-question class this project has met, and
                // the one this milestone's own new mechanic opens.
                Component("ursafar-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, ModalUndoRestore),
                        Prove(RetainedPrefixUndo, ModalUndoRestore),
                        Prove(ReplayReconstruction, ModalUndoRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "UrsafarVerifiedContentTests.NineteenFaces_EighteenDealt_AndOneReturnedToTheBox" }),

                Component("ursafar-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests" +
                            ".EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests" +
                            ".NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item, from the sibling not taken. No journey reaches it, for the reason
                // every earlier hero's identical row records.
                AbsentComponent("ursafar-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene " +
                    "content stages and which no journey can earn its way to; his twelve item icons " +
                    "cover all six of p.16's kinds, pinned at the definition tier"),

                // He owns no token and no marker - his Blue swap clauses act on a token or unit the
                // board already has, per ledger question 14's ruling.
                AbsentComponent("ursafar-tokens-and-markers", "hero-owned tokens and markers",
                    "Ursafar's nineteen faces place no token and give no marker of his own; his three " +
                    "Blue swap riders act on a unit or a token already on the board")
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
        public void EveryUrsafarFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesUrsafarsPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(UrsafarPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(UrsafarPlayerPathGateTests).Assembly);

        [Test]
        public void EveryUrsafarJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(UrsafarUiJourneyPlayModeTests),
                typeof(UrsafarEngagedUiJourneyPlayModeTests),
                typeof(UrsafarProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndUrsafarActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "ursafar-items",
                UrsafarCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void UrsafarPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Ursafar's nineteen faces place no token and give no marker of his own");

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
