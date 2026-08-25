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
    /// Sabina's half of the permanent hero player-path completion gate. Milestone 8.22.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with <see cref="ArienPlayerPathGateTests"/>,
    /// <see cref="WaspPlayerPathGateTests"/> and <see cref="BroganPlayerPathGateTests"/>; the table
    /// below is Sabina's own. This milestone needed no derivation refinement at all - the fourth hero
    /// added nine capability classifications and not one rule, and the offline derivation printed every
    /// Wasp, Arien and Brogan row byte-identical afterwards, which is the test milestone 8.20b set for
    /// whether an addition is a generalisation or a re-fit.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="SabinaCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and
    /// <see cref="MatrixRowsDeclareExactlyTheShapesSabinasPrintedContentIntroduces"/> compares the two.
    /// A row cannot claim less than its face asks for, cannot claim more, and cannot name a shape
    /// without naming the journey where a player performs it.</para>
    ///
    /// <para><b>Three of her nineteen faces ask a player for less than their siblings do, and that is
    /// the content rather than a gap.</b> Quickdraw, Gunslinger and Dead Shot print a conditional
    /// addition to their own Attack value, which rulebook p.14 marks with the <c>+</c> on the number
    /// itself. A value is not a step: those three compose no card text, so what they introduce is one
    /// ranged Attack target and nothing else, and their rows say exactly that. The condition itself is
    /// pinned authoritatively, where a number belongs.</para>
    ///
    /// <para><b>Shared journeys are deliberate, and this table shares them widely.</b> Committing a
    /// card is one input for all eighteen dealt faces; the secondary Movement icon seventeen of them
    /// print and the secondary Defense value every one of them prints are likewise one shape on many
    /// faces. Her printed set is built out of five sentences repeated at rising numbers - swap a pair,
    /// order a minion, retrieve and step, remove after the attack, take an enemy's card - so a sibling
    /// pair usually differs by a radius or a count rather than by anything a player does. The gate's
    /// own sentence permits that: "Play Mode need not duplicate permutations that change no input,
    /// presentation, or transition shape". The face-specific half of the evidence is the authoritative
    /// scenario column, which is one row per face by construction.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class SabinaPlayerPathGateTests
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
            "Sabina_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsOnSabinasOwnBlueSeatAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesSabinasCommitmentAndRestartsHerOntoHerStagedSpace";

        private const string FootprintConformance =
            "Scene_DrawsEverySabinaFaceAtTheStandardCardFootprint";

        private const string RangedAttackJourney =
            "PointBlankShot_ShootsTheAdjacentHeroAndPushesHerOneSpaceAway";

        private const string SwapWithMinionJourney =
            "BackToBack_SwapsSabinaWithTheFriendlyMinionSheClicks";

        private const string SwapPairJourney =
            "ListenUp_SwapsTheTwoMinionsItsTwoBoardClicksName";

        private const string MoveOrderJourney =
            "TroopMovement_MovesAFriendlyMinionByTwoClicksAndDeclinesItsRepeat";

        private const string MoveOrderSurvivesUndoAndRestore =
            "TroopMovementsRepeat_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndSabinaSurvivesTheAttack";

        private const string DefeatAndRespawn =
            "Sabina_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        private const string RemovalJourney =
            "Shootout_RemovesTheAdjacentEnemyMinionItsBoardClickNames";

        private const string RefusedRemovalJourney =
            "BulletHell_RefusesItsPrintedRemovalOnItsOwnControl";

        private const string RetrieveJourney =
            "SteadyAdvance_TakesBackADiscardedCardOnItsFaceAndThenStepsInsideHerRadius";

        private const string EnemyDiscardJourney =
            "CloseSupport_NamesTheEnemyHeroBesideHerMinionAndTakesACardFromTheirHand";

        private const string UltimateJourney =
            "BigSoddingGun_IsDrawnOnSabinasRailAndItsPushMakesThePushedHeroDiscard";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_SABINA.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Sabina",
            SabinaCardContent.AllDefinitions,
            SabinaCardContent.ReturnedToBoxAtSetup,
            // She prints no Active effect anywhere - the first verified hero of whom that is true - so
            // there are no Active-effect handlers to classify and no reading shape any face of hers
            // can introduce. `SabinaVerifiedContentTests.NoFacePrintsAnActiveEffect_WhichIsAFirstFor
            // ThisBuild` is what holds that claim against the content.
            Array.Empty<IEffectCapabilityHandler>(),
            // Big Sodding Gun does carry a triggered effect, unlike Brogan's Ultimate: p.16's ability
            // is two standing rules plus one thing that fires, and the matrix has to be handed the
            // effect or the shapes its trigger asks for would be counted as none.
            new[]
            {
                SabinaEffectContent.BigSoddingGunTriggeredEffect(SabinaCardContent.ContentRevision)
            },
            new[]
            {
                // The Gold basic. "After the attack: If the target is adjacent to you, push it 1
                // space" is milestone 8.22's present-tense reading, and it asks nobody anything: the
                // push is a fixed one-space displacement of a piece the Attack already named. What a
                // player sees is the token standing one space further away.
                Face(SabinaCardContent.PointBlankShot, "Point Blank Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(AfterAttackTextPause, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaAfterAttackRemovalTests" +
                            ".PointBlankShot_PushesASurvivingAdjacentTargetOneSpaceAway",
                        "SabinaAfterAttackRemovalTests" +
                            ".PointBlankShot_PushesNothingWhenItsOwnAttackTookTheTargetOffTheBoard"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(SabinaCardContent.Bang, "Bang",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes"),

                // The Silver basic. "Swap with a friendly minion in radius" is one board click, and
                // the row declares no secondary Movement because the face prints none - the one card
                // of her eighteen dealt faces that does not.
                Face(SabinaCardContent.BackToBack, "Back to Back", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapWithMinionJourney),
                        Prove(BoardUnitSelection, SwapWithMinionJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".BackToBack_SwapsHerWithAFriendlyMinion_AndOffersNoEnemyAndNothingOutOfRadius"
                    }),

                // The three conditional Attacks. "+3 Attack if the target played an attack card this
                // turn" is a value rather than a step, which is why these rows carry no card-text
                // pause and no selection: what a player does is choose a target at printed Range, and
                // the addition happens inside p.14's own total.
                Face(SabinaCardContent.Quickdraw, "Quickdraw", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheThreeConditionalAttacks_RaiseTheirPrintedAmountOnlyAgainstAnAttackingTarget",
                        "SabinaVerifiedContentTests" +
                            ".ThreeRedAttacksDeclareTheRaisedValuePageFourteensPlusSignMarks_AndNoOtherFaceDoes"
                    }),

                Face(SabinaCardContent.Gunslinger, "Gunslinger", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheThreeConditionalAttacks_RaiseTheirPrintedAmountOnlyAgainstAnAttackingTarget"
                    }),

                // The first of the two removal Attacks. "If the target was adjacent to you" is the
                // past-tense reading, which is what makes the clause reachable after its own Attack
                // has taken the target off the board - and the removal it opens is an `up to`
                // selection, so it draws a confirm-or-decline beside its board candidates.
                Face(SabinaCardContent.Shootout, "Shootout", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RemovalJourney),
                        Prove(AfterAttackTextPause, RemovalJourney),
                        Prove(ConfirmOrDecline, RemovalJourney),
                        Prove(BoardUnitSelection, RemovalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaAfterAttackRemovalTests" +
                            ".Shootout_RunsItsRemovalAfterDefeatingTheAdjacentTargetItAskedAbout",
                        "SabinaAfterAttackRemovalTests.Shootout_RemovesNothingWhenTheTargetWasNeverAdjacent"
                    }),

                Face(SabinaCardContent.DeadShot, "Dead Shot", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RangedAttackJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheThreeConditionalAttacks_RaiseTheirPrintedAmountOnlyAgainstAnAttackingTarget"
                    }),

                // The Tier III removal. Its printed difference from Shootout is the count - "up to
                // two" where its sibling says "up to one" - and a count is not a shape: a player names
                // each minion with the same board click and the selection's own maximum decides how
                // many it takes. The count itself, the ordered application, the coinless removal and
                // the mid-turn Lane Push a second removal can cause are all pinned authoritatively.
                //
                // It takes the printed refusal where Shootout takes the removal, which is why the two
                // journeys are separate: milestone 8.20a recorded that a confirm with nobody named and
                // a decline are different recorded outcomes, so "up to" is only fully driven when a
                // player has done each once. The board click that names a piece is the same input on
                // both faces, and this row rests on its sibling's journey for it.
                Face(SabinaCardContent.BulletHell, "Bullet Hell", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, RefusedRemovalJourney),
                        Prove(AfterAttackTextPause, RefusedRemovalJourney),
                        Prove(ConfirmOrDecline, RefusedRemovalJourney),
                        Prove(BoardUnitSelection, RemovalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaAfterAttackRemovalTests" +
                            ".BulletHell_OffersOnlyAdjacentEnemyMinions_AndAtMostTheTwoItPrints",
                        "SabinaAfterAttackRemovalTests" +
                            ".BulletHell_AppliesBothRemovalsInOrder_AndPaysNoCoinForEither",
                        "SabinaAfterAttackRemovalTests" +
                            ".BulletHell_RemovingTheLastEnemyMinionInTheZone_PushesTheLaneMidTurn"
                    }),

                // The swap pair. "Swap two minions in radius" is two board clicks into one selection,
                // and the three faces that print it differ by radius and by whether p.19's Heavy
                // immunity is ignored - neither of which is a thing a player does differently.
                Face(SabinaCardContent.ListenUp, "Listen Up", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapPairJourney),
                        Prove(BoardUnitSelection, SwapPairJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheSwapPair_OffersBothSidesMinions_WithinEachFacesOwnPrintedRadius",
                        "SabinaMinionCommandTests" +
                            ".TheSwapPair_ExchangesTheTwoMinionsNamed_AndLeavesSabinaWhereSheStood"
                    }),

                Face(SabinaCardContent.RogerRoger, "Roger Roger", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapPairJourney),
                        Prove(BoardUnitSelection, SwapPairJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheSwapPair_OffersBothSidesMinions_WithinEachFacesOwnPrintedRadius"
                    }),

                // The retrieve pair. "If there are two or more friendly minions in radius you may
                // retrieve a discarded card, then move up to N spaces" - a printed condition that
                // opens nothing when it is false, a card taken back on its own whole face, and a step
                // clicked on the board.
                Face(SabinaCardContent.SteadyAdvance, "Steady Advance", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrieveJourney),
                        Prove(ConfirmOrDecline, RetrieveJourney),
                        Prove(HandCardSelection, RetrieveJourney),
                        Prove(BoardSpaceSelection, RetrieveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".SteadyAdvance_RunsNothingWithOneFriendlyMinionInRadius_AndRunsWithTwo",
                        "SabinaMinionCommandTests" +
                            ".SteadyAdvance_OffersTheDiscardedCardAndThenTheMove_AndTheRetrieveCanBeDeclined"
                    }),

                // "Ignoring heavy minion immunity" is p.19's override rather than a second question:
                // it widens the candidates this face offers and changes nothing about how a player
                // names one. The override itself is pinned authoritatively, against the sibling that
                // does not have it.
                Face(SabinaCardContent.ReadyAndWaiting, "Ready and Waiting", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, SwapPairJourney),
                        Prove(BoardUnitSelection, SwapPairJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".ReadyAndWaiting_NamesAnImmuneHeavyMinion_WhereItsSiblingCannot"
                    }),

                Face(SabinaCardContent.UnwaveringResolve, "Unwavering Resolve", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrieveJourney),
                        Prove(ConfirmOrDecline, RetrieveJourney),
                        Prove(HandCardSelection, RetrieveJourney),
                        Prove(BoardSpaceSelection, RetrieveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".UnwaveringResolve_TakesTheDiscardedCardBackAndMovesTheFurtherPrintedDistance"
                    }),

                // The three move orders. "Move a friendly minion in radius 1 space, to a space in
                // radius. May repeat once" is the face that asks a player for the most: which minion,
                // where it goes, and whether to do it again - and the two radii are measured from two
                // different places, which is what milestone 8.22's `WithinActorRadius` destination
                // policy exists for.
                Face(SabinaCardContent.TroopMovement, "Troop Movement", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MoveOrderJourney),
                        Prove(ConfirmOrDecline, MoveOrderJourney),
                        Prove(RepeatDecision, MoveOrderJourney),
                        Prove(BoardUnitSelection, MoveOrderJourney),
                        Prove(BoardSpaceSelection, MoveOrderJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TroopMovement_MovesAFriendlyMinionOneSpace_AndOnlyToASpaceInsideHerOwnRadius",
                        "SabinaMinionCommandTests" +
                            ".TheDirectMove_RepeatsAsOftenAsItsFacePrints_AndTheRepeatCanBeDeclined"
                    }),

                Face(SabinaCardContent.MarchingOrders, "Marching Orders", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MoveOrderJourney),
                        Prove(ConfirmOrDecline, MoveOrderJourney),
                        Prove(RepeatDecision, MoveOrderJourney),
                        Prove(BoardUnitSelection, MoveOrderJourney),
                        Prove(BoardSpaceSelection, MoveOrderJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheDirectMove_RepeatsAsOftenAsItsFacePrints_AndTheRepeatCanBeDeclined"
                    }),

                // The enemy-discard pair. Two bounds measured from two different places - the disk
                // from Sabina, the adjacency from each of her own minions - and a player answers with
                // one click on the hero standing in both.
                Face(SabinaCardContent.CloseSupport, "Close Support", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EnemyDiscardJourney),
                        Prove(BoardUnitSelection, EnemyDiscardJourney),
                        Prove(HandCardSelection, EnemyDiscardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".CloseSupport_NamesAnEnemyHeroBesideOneOfHerMinions_AndNotOneMerelyInRadius",
                        "SabinaMinionCommandTests" +
                            ".CloseSupport_AsksTheNamedHeroToDiscard_AndTheCardLeavesTheirHand"
                    }),

                Face(SabinaCardContent.PathToVictory, "Path to Victory", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MoveOrderJourney),
                        Prove(ConfirmOrDecline, MoveOrderJourney),
                        Prove(RepeatDecision, MoveOrderJourney),
                        Prove(BoardUnitSelection, MoveOrderJourney),
                        Prove(BoardSpaceSelection, MoveOrderJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".TheDirectMove_RepeatsAsOftenAsItsFacePrints_AndTheRepeatCanBeDeclined"
                    }),

                // The Tier III enemy-discard. Its printed difference is the consequence - "or is
                // defeated" where its sibling says "if able" - and a consequence is not a shape: a
                // player answers it with the same two inputs. It is also the first printed Skill in
                // this build to request a hero Defeat, which is the engine gap milestone 8.22 found
                // and closed; the branch itself is owned authoritatively.
                Face(SabinaCardContent.CoveringFire, "Covering Fire", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EnemyDiscardJourney),
                        Prove(BoardUnitSelection, EnemyDiscardJourney),
                        Prove(HandCardSelection, EnemyDiscardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "SabinaMinionCommandTests" +
                            ".CoveringFire_DefeatsAHeroWhoCannotPay_WhereCloseSupportLeavesThemStanding"
                    }),

                // p.16's Ultimate. It prints no action, so it has no commitment row; what it prints is
                // two standing rules and one thing that fires. The standing rules ask nobody anything
                // - they raise her basic Attack's own numbers - so what the player path owes this face
                // is that it is drawn where a player can see it, that its trigger stops the game, and
                // that the hero it names answers on their own card face.
                Face(SabinaCardContent.BigSoddingGun, "Big Sodding Gun", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney),
                        Prove(UltimateTriggerPause, UltimateJourney),
                        Prove(HandCardSelection, UltimateJourney)
                    },
                    new[]
                    {
                        "SabinaBigSoddingGunTests" +
                            ".APushOfTheEnemyHero_AsksThatHeroToDiscard_AfterTheyHaveMoved",
                        "SabinaBigSoddingGunTests" +
                            ".TheStandingRules_RaiseTheLiveAttackTotalOfHerBasicAttackByTwo",
                        "SabinaVerifiedContentTests.BigSoddingGunDeclaresTwoStandingRulesAndAnAfterPushTrigger"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                // The piece. Every composition that deals Sabina restages her into the engagement, so
                // the spawn half of this row is proved the two ways the staging leaves open: the piece
                // carries the map's own declared Blue spawn point and stands where the configured
                // setup says, and p.13's respawn puts her back on that spawn from a board click. That
                // is the same claim the unrestaged fixtures make about the other three heroes, made
                // about the two ends of it rather than about the opening position alone.
                Component("sabina-hero-piece", "the official circular Blue hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn)
                    },
                    new[] { "SabinaVerifiedContentTests.TwoSeatsDealingSabina_GetDistinctInstanceIdentities" }),

                // Restore, Undo, replay and restart, proved across the printed repeat - the one
                // decision of hers a player is asked twice in one card, and therefore the pause whose
                // re-derivation after a restore has something to be wrong about.
                Component("sabina-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, MoveOrderSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, MoveOrderSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, MoveOrderSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "SabinaBigSoddingGunTests.ThePausedTrigger_SurvivesASnapshotRoundTrip" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. It is required of every hero, including one added after the footprint
                // became a standard: a hero whose longest sentence does not fit is exactly the case
                // that must fail here rather than widen it. Sabina's Point Blank Shot carries the
                // build's only parenthetical push reminder and Big Sodding Gun prints two sentences.
                Component("sabina-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests.EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests.NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item is gained from the sibling p.15's Level Up did not take. No journey
                // reaches it, for the reason milestones 8.20a, 8.20b and 8.21 recorded against the
                // identical rows the other three heroes owe: the item track is derived progression
                // state, `DEV-004` composes card locations only, and no scene content stages a
                // position where a Level Up is offered - so a staged session begins with an empty item
                // track and a live one cannot earn its way to a Level Up inside a journey's budget.
                // This is a debt rather than a gap: the row names it and the guard below holds it to
                // Sabina actually printing item icons.
                AbsentComponent("sabina-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; the printed pairs and their item " +
                    "icons are pinned by SabinaVerifiedContentTests." +
                    "TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator"),

                // Sabina prints no token and no marker. Her Blue and Green kit moves, swaps and
                // removes pieces already standing on the map, and her Red kit pushes one; nothing of
                // hers puts a new object on the board or hangs a marker on one.
                AbsentComponent("sabina-tokens-and-markers", "hero-owned tokens and markers",
                    "Sabina's nineteen faces place no token and give no marker; every noun her text " +
                    "names is a unit already standing on the board when she acts")
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
        public void EverySabinaFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesSabinasPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(SabinaPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(typeof(SabinaPlayerPathGateTests).Assembly);

        [Test]
        public void EverySabinaJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(SabinaUiJourneyPlayModeTests),
                typeof(SabinaEngagedUiJourneyPlayModeTests),
                typeof(SabinaProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row should
        /// go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndSabinaActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "sabina-items",
                SabinaCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void SabinaPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Sabina's nineteen faces place no token and give no marker");

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
