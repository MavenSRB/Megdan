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
    /// Brogan's half of the permanent hero player-path completion gate. Milestone 8.21.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with <see cref="ArienPlayerPathGateTests"/>
    /// and <see cref="WaspPlayerPathGateTests"/>; the table below is Brogan's own. Milestone 8.20b
    /// proved that a hero brings a table and a fixture rather than new rules, and this milestone kept
    /// to it: the two derivation refinements it needed - p.10's modal bullet, and a direct Move that
    /// names its own destination - left every Wasp and Arien row byte-identical, which is the test of
    /// whether a refinement is a generalisation or a re-fit.</para>
    ///
    /// <para><b>Why it cannot be satisfied by writing it down.</b> Nothing here declares what a face
    /// does. <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="BroganCardContent"/> - action kind, origin, printed range, card-text timing, effect
    /// node kind, failure policy, capability identity, progression profile - and
    /// <see cref="MatrixRowsDeclareExactlyTheShapesBrogansPrintedContentIntroduces"/> compares the two.
    /// A row cannot claim less than its face asks for, cannot claim more, and cannot name a shape
    /// without naming the journey where a player performs it.</para>
    ///
    /// <para><b>What the gate found on Brogan, which is the point of running it.</b> Two defects,
    /// neither findable headlessly and both in the band between a declaration and what a player is
    /// actually shown. His three shove faces print "up to N spaces" and declared a zero distance floor
    /// beside a <em>mandatory</em> node policy, which <c>AdmittedDistances.ForDeclared</c> reads only
    /// under <c>UpTo</c> - so every push travelled its declared distance exactly and the printed choice
    /// was never offered. And the removal-prevention decision, reached at the altitude an effect
    /// invokes rather than the one rulebook p.14's Attack produces, could be opened and could not be
    /// answered: Initiative had already advanced past it, so the answer was refused as an Initiative
    /// command and the match was stuck. Both are recorded on the rows below and in the journeys that
    /// now cover them.</para>
    ///
    /// <para><b>Shared journeys are deliberate, and this table shares four of them widely.</b>
    /// Committing a card is one input for all nineteen faces; the secondary Movement icon seventeen of
    /// them print and the secondary Defense value eighteen of them print are likewise one shape on many
    /// faces. The gate's own sentence permits it: "Play Mode need not duplicate permutations that
    /// change no input, presentation, or transition shape". The face-specific half of the evidence is
    /// the authoritative scenario column, which is one row per face by construction.</para>
    ///
    /// <para>These are plain NUnit cases rather than <c>[UnityTest]</c> journeys: the matrix is a
    /// statement about the suite, not a thing a player does, and it must fail in the same run as the
    /// journeys it audits.</para>
    /// </summary>
    public sealed class BroganPlayerPathGateTests
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
        /// the same input whichever face is picked up, and the engaged and progressed fixtures commit
        /// headlessly by design so their own subjects are the cards rather than the commitment.
        /// </summary>
        private const string TrayCommitAndWalk =
            "Brogan_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace";

        private const string Spawn =
            "HeroPiece_StandsAtTheSecondBlueSpawnAsBroganAndIsPointedAtByABoardClick";

        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesBrogansCommitmentAndRestartsHimOntoHisSpawn";

        private const string FootprintConformance =
            "Scene_DrawsEveryBroganFaceAtTheStandardCardFootprint";

        private const string WarDrummerJourney =
            "WarDrummer_GivesTheAlliedHeroOneCoinChosenByAClickOnHisPiece";

        private const string MasterSkaldJourney =
            "MasterSkald_GivesTheAlliedHeroTwoCoinsThroughTheSameBoardClick";

        private const string OnslaughtJourney =
            "Onslaught_AttacksTheAdjacentMinionAndWalksIntoTheSpaceItLeft";

        private const string ChargeJourney =
            "MadDash_ChargesInAStraightLineAndTargetsTheUnitBesideWhereItStops";

        /// <summary>
        /// Milestone 8.29d added this journey and ran Play Mode filtered, so no row named it and the
        /// gate stayed red until milestone 8.29e's unfiltered run. It proves the other half of the
        /// same step-1 moment the charge journey proves: an Attack whose mandatory card text cannot
        /// bind a target is drawn greyed rather than started and stopped.
        /// </summary>
        private const string ChargeWithoutABinding =
            "MadDashWithoutACompleteChargeBinding_IsGreyedAndCannotBeStarted";

        private const string ShoveJourney =
            "BrutalJab_MovesOneSpaceAndPushesTheAdjacentPieceByItsOwnDistanceControls";

        private const string BulwarkJourney =
            "Bulwark_RetrievesTheCardTheEnemysRiderTookAndPinsHisOwnRadius";

        private const string GuardJourney =
            "Shield_PaysASilverCardOnItsOwnFaceToKeepTheMinionAnAttackWouldRemove";

        private const string GuardSurvivesUndoAndRestore =
            "ShieldsRemovalPreventionPause_IsAnsweredAgainAfterAnUndoAndAfterARestore";

        private const string NumericDefense =
            "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndBroganSurvivesTheAttack";

        private const string DefeatAndRespawn =
            "Brogan_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick";

        private const string ModalJourney =
            "ThrowingAxe_ChoosesItsRangedBulletAndSpendsACardToTargetAtRange";

        private const string CounterJourney =
            "ShieldBash_MakesTheAdjacentAttackerDiscardOnHerOwnPrintedFace";

        private const string UltimateJourney =
            "OneManArmy_IsDrawnOnBrogansRailAsHisUnlockedUltimate";

        private const string EffectInvokedGuardJourney =
            "Fortify_KeepsTheMinionAnEnemyUltimateDefeatedByPayingABasicCard";

        private const string HeroModifierHoverJourney =
            "HeroModifiersStayVisibleWithCardPipsAndHoverContributorsOutsideAttacks";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_BROGAN.md's inventory table, which is in the
        // same order and uses the same names.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Brogan",
            BroganCardContent.AllDefinitions,
            BroganCardContent.ReturnedToBoxAtSetup,
            BroganActiveEffectContent.Handlers(),
            // One Man Army carries no triggered effect at all: p.16's ability is a permanent
            // Minion-Battle reading rather than something that fires, so there is nothing to pass and
            // the matrix's own assertion about an unsupplied effect never applies to him.
            Array.Empty<EffectDefinition>(),
            new[]
            {
                // The Gold basic. Its after-attack sentence is milestone 8.21's `NamedSpace` direct
                // Move: the destination is the space p.14's step 6 emptied, so the capability applies
                // it rather than offering a set - which is why this row declares no board-space
                // selection and the charges below do.
                Face(BroganCardContent.Onslaught, "Onslaught", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, OnslaughtJourney),
                        Prove(AfterAttackTextPause, OnslaughtJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory",
                        "BroganVerifiedContentTests" +
                            ".EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the pitch is a six-player match,
                // so no player of this build can reach it; the guard checks that claim against the
                // content rather than taking the row's word for it.
                UnreachableFace(BroganCardContent.LeapForward, "Leap Forward",
                    "p.6 returns the Handicap card to the box and the pitch is a six-player match; " +
                    "p.20 brings it back only for an odd player count, which no configuration of " +
                    "this build composes"),

                // The Silver basic. Its retrieve needs a discard to offer, which is why its journey
                // spends a turn letting the enemy take a card first - a Bulwark over an empty discard
                // asks nothing, because p.10 does not offer a choice with no candidates.
                Face(BroganCardContent.Bulwark, "Bulwark", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BulwarkJourney),
                        Prove(ConfirmOrDecline, BulwarkJourney),
                        Prove(HandCardSelection, BulwarkJourney),
                        Prove(ActiveEffectReading, BulwarkJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests.TheFourActiveEffectDeclarations_SayWhatTheirFacesSay",
                        "BroganVerifiedContentTests" +
                            ".EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints"
                    }),

                // The three charges are one sentence at three declared distances, and they ask two
                // board clicks: which space to end on, and which unit beside it. Milestone 8.35p put
                // the Move between those two questions, so the space now comes from
                // `choose-charge-line` and the unit from `choose-target` - two capabilities where
                // there was one, the same two shapes, and these rows did not move.
                Face(BroganCardContent.MadDash, "Mad Dash", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ChargeJourney),
                        Prove(BeforeAttackTextPause, ChargeJourney),
                        Prove(BeforeAttackTextPause, ChargeWithoutABinding),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.TheChargesAndModalAttacks_BindTheirTargetByCardText" }),

                // "2 or 3 spaces": the same declaration with a wider budget, which is what milestone
                // 8.21's `NamedSpace` policy made expressible in one contract. It introduces no shape
                // Mad Dash does not, which is exactly the case the gate's "need not duplicate
                // permutations" sentence covers; the printed bounds are pinned authoritatively.
                Face(BroganCardContent.Bullrush, "Bullrush", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ChargeJourney),
                        Prove(BeforeAttackTextPause, ChargeJourney),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.TheChargesAndModalAttacks_BindTheirTargetByCardText" }),

                // p.10's modal form, and the first printed face in this build to use it.
                Face(BroganCardContent.ThrowingAxe, "Throwing Axe", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalJourney),
                        Prove(BeforeAttackTextPause, ModalJourney),
                        Prove(ModalBulletChoice, ModalJourney),
                        Prove(ConfirmOrDecline, ModalJourney),
                        Prove(HandCardSelection, ModalJourney),
                        Prove(BoardUnitSelection, ModalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.TheChargesAndModalAttacks_BindTheirTargetByCardText" }),

                Face(BroganCardContent.FuriousCharge, "Furious Charge", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, ChargeJourney),
                        Prove(BeforeAttackTextPause, ChargeJourney),
                        Prove(BoardSpaceSelection, ChargeJourney),
                        Prove(BoardUnitSelection, ChargeJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.TheChargesAndModalAttacks_BindTheirTargetByCardText" }),

                // The Tier III modal. Its ranged bullet reads the discard <em>after</em> the optional
                // discard rather than before it, which is a condition rather than a shape: what a
                // player is asked is what Throwing Axe asks, and the difference is pinned
                // authoritatively.
                Face(BroganCardContent.ThrowingSpear, "Throwing Spear", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, ModalJourney),
                        Prove(BeforeAttackTextPause, ModalJourney),
                        Prove(ModalBulletChoice, ModalJourney),
                        Prove(ConfirmOrDecline, ModalJourney),
                        Prove(HandCardSelection, ModalJourney),
                        Prove(BoardUnitSelection, ModalJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests.TheChargesAndModalAttacks_BindTheirTargetByCardText",
                        "BroganVerifiedContentTests" +
                            ".EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                // The three shoves. This row is where milestone 8.21's gate earned its keep: the
                // printed "up to N spaces" declared a zero floor beside a mandatory node policy, and
                // `AdmittedDistances.ForDeclared` reads a printed floor only under `UpTo` - so the
                // distance question the derivation says these faces ask was never actually offered,
                // and every push travelled its declared distance exactly.
                Face(BroganCardContent.BrutalJab, "Brutal Jab", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShoveJourney),
                        Prove(ConfirmOrDecline, ShoveJourney),
                        Prove(BoardSpaceSelection, ShoveJourney),
                        Prove(BoardUnitSelection, ShoveJourney),
                        Prove(PushDistanceChoice, ShoveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory",
                        "BroganVerifiedContentTests" +
                            ".EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                Face(BroganCardContent.MightyPunch, "Mighty Punch", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShoveJourney),
                        Prove(ConfirmOrDecline, ShoveJourney),
                        Prove(BoardSpaceSelection, ShoveJourney),
                        Prove(BoardUnitSelection, ShoveJourney),
                        Prove(PushDistanceChoice, ShoveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory" }),

                // "who has played an attack card this turn" narrows the candidates rather than gating
                // the consequence, which is why the journey's own assertion is about what a player is
                // offered rather than about what happens when they pick wrongly.
                Face(BroganCardContent.ShieldBash, "Shield Bash", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, CounterJourney),
                        Prove(BoardUnitSelection, CounterJourney),
                        Prove(HandCardSelection, CounterJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests" +
                            ".EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints"
                    }),

                Face(BroganCardContent.SavageKick, "Savage Kick", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ShoveJourney),
                        Prove(ConfirmOrDecline, ShoveJourney),
                        Prove(BoardSpaceSelection, ShoveJourney),
                        Prove(BoardUnitSelection, ShoveJourney),
                        Prove(PushDistanceChoice, ShoveJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[] { "BroganVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory" }),

                // The Tier III counter. Its printed difference is the consequence - "or is defeated"
                // where its sibling says "if able" - and a consequence is not a shape: a player answers
                // it with the same two inputs. The branch itself is owned authoritatively.
                Face(BroganCardContent.Counterattack, "Counterattack", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, CounterJourney),
                        Prove(BoardUnitSelection, CounterJourney),
                        Prove(HandCardSelection, CounterJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganVerifiedContentTests" +
                            ".EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints",
                        "BroganVerifiedContentTests" +
                            ".EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog"
                    }),

                // The three guards. Shield is the Tier I face whose journey opens milestone 8.19c1's
                // removal-prevention decision at rulebook p.14's own altitude, and Fortify the Tier III
                // face whose journey opens it at the altitude an effect invokes.
                Face(BroganCardContent.Shield, "Shield", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GuardJourney),
                        Prove(ActiveEffectReading, GuardJourney),
                        Prove(RoundLongActiveEffectReading, GuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganRemovalPreventionTests" +
                            ".TheThreeGuards_OfferTheirOwnPrintedPriceToTheirControllerAlone",
                        "BroganRemovalPreventionTests" +
                            ".ShieldReachesOneSpace_WhereBolsterAndFortifyReachTwo"
                    }),

                Face(BroganCardContent.Bolster, "Bolster", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GuardJourney),
                        Prove(ActiveEffectReading, GuardJourney),
                        Prove(RoundLongActiveEffectReading, GuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganRemovalPreventionTests" +
                            ".FortifyAcceptsABasicCard_WhereItsTwoSiblingsAcceptOnlyTheSilverOne",
                        "BroganRemovalPreventionTests" +
                            ".ShieldReachesOneSpace_WhereBolsterAndFortifyReachTwo"
                    }),

                // "A friendly hero in range gains N coins." p.10 keeps the acting hero out of his own
                // action, so this face has no candidate at all in a one-hero-per-team match - which is
                // why this build has a staged composition dealing two heroes to Blue, and why these two
                // rows name journeys from it rather than from the engaged staging.
                Face(BroganCardContent.WarDrummer, "War Drummer", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, WarDrummerJourney),
                        Prove(BoardUnitSelection, WarDrummerJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganAlliedHeroCardTests" +
                            ".WarDrummer_GrantsItsPrintedCoinsToAFriendlyHeroAndNeverToBrogan",
                        "BroganAlliedHeroCardTests" +
                            ".TheStagedAllyMatch_DealsThreeSeatsWithBroganBesideAnAllyInRange"
                    }),

                Face(BroganCardContent.Fortify, "Fortify", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, EffectInvokedGuardJourney),
                        Prove(ActiveEffectReading, EffectInvokedGuardJourney),
                        Prove(RoundLongActiveEffectReading, EffectInvokedGuardJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganRemovalPreventionTests" +
                            ".FortifyAcceptsABasicCard_WhereItsTwoSiblingsAcceptOnlyTheSilverOne"
                    }),

                Face(BroganCardContent.MasterSkald, "Master Skald", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, MasterSkaldJourney),
                        Prove(BoardUnitSelection, MasterSkaldJourney),
                        Prove(SecondaryDefenseOffer, NumericDefense),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk)
                    },
                    new[]
                    {
                        "BroganAlliedHeroCardTests" +
                            ".MasterSkald_GrantsTheHigherPrintedAmountForTheSameSentence"
                    }),

                // p.16's Ultimate. It prints no action at all, so it has no commitment row, and unlike
                // Wasp's and Arien's it carries no triggered effect either: p.16's ability is a
                // permanent Minion-Battle reading that fires nothing and asks nobody anything. What the
                // player path owes it is therefore exactly one shape - that it is drawn where a player
                // can see it - and the ability itself is owned by its own isolated handler's tests.
                Face(BroganCardContent.OneManArmy, "One Man Army", new[]
                    {
                        Prove(UltimateRailVisibility, UltimateJourney)
                    },
                    new[]
                    {
                        "BroganOneManArmyTests",
                        "BroganVerifiedContentTests" +
                            ".OneManArmyPrintsNoIconAndNoInitiative_AndDeclaresItsMinionBattleRule"
                    }),

                // -------------------------------------------------------------------------------
                // Hero-owned components
                // -------------------------------------------------------------------------------

                // The piece is the one the scene authors no view for: the staged three-hero
                // composition deals a seat the scene has no authored hero for, so composition
                // generates its view and its pointer selector. That is the half this row exists to
                // hold, because a generated piece that carries no identity, wears no token, or is lost
                // by a rebuild would be invisible to every other row in this table.
                Component("brogan-hero-piece", "the official circular Blue hero token", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPiecePointerSelection, HeroModifierHoverJourney),
                        Prove(PresentationRebuild, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn)
                    },
                    new[] { "BroganVerifiedContentTests.TwoSeatsDealingBrogan_GetDistinctInstanceIdentities" }),

                // Restore, Undo, replay and restart, proved across the one decision this milestone
                // added: milestone 8.19c1 could not reach it, because no configured content could open
                // the pause there was no answered command to Undo across.
                Component("brogan-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, GuardSurvivesUndoAndRestore),
                        Prove(RetainedPrefixUndo, GuardSurvivesUndoAndRestore),
                        Prove(ReplayReconstruction, GuardSurvivesUndoAndRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[] { "RosterPausingSourceTests" }),

                // Milestone 8.20c. The whole printed set at once, measured against the standard card
                // footprint. It is required of every hero, including one added after the footprint
                // became a standard: a hero whose longest sentence does not fit is exactly the case
                // that must fail here rather than widen it. Brogan's two modal faces print the longest
                // bulleted rules text in the build.
                Component("brogan-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests.EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests.NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // p.16's item is gained from the sibling p.15's Level Up did not take. No journey
                // reaches it, for the reason milestones 8.20a and 8.20b recorded against the identical
                // rows Arien and Wasp owe: the item track is derived progression state, `DEV-004`
                // composes card locations only, and no scene content stages a position where a Level
                // Up is offered - so a staged session begins with an empty item track and a live one
                // cannot earn its way to a Level Up inside a journey's budget. This is a debt rather
                // than a gap: the row names it and the guard below holds it to Brogan actually
                // printing item icons.
                AbsentComponent("brogan-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; the printed pairs and their item " +
                    "icons are pinned by BroganVerifiedContentTests." +
                    "TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator"),

                // Brogan prints no token and no marker. His board-object text moves, pushes and walks
                // pieces that are already on the map - the one noun of his that is not a unit is the
                // token his shoves may push, and p.19 puts that on the board before he touches it.
                AbsentComponent("brogan-tokens-and-markers", "hero-owned tokens and markers",
                    "Brogan's nineteen faces place no token and give no marker; the token his three " +
                    "shove faces admit as a push target is one already standing on the board")
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
        public void EveryBroganFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesBrogansPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(BroganPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(typeof(BroganPlayerPathGateTests).Assembly);

        [Test]
        public void EveryBroganJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(BroganUiJourneyPlayModeTests),
                typeof(BroganEngagedUiJourneyPlayModeTests),
                typeof(BroganProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// The item row is a recorded debt rather than a gap, and it is held to having a subject: if a
        /// hero printed no item icon at all, the row's excuse would be about nothing and the row should
        /// go instead of standing as permanent cover.
        /// </summary>
        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndBroganActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "brogan-items",
                BroganCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        [Test]
        public void BroganPrintsNoTokenOrMarker_AsTheMatrixRecords() =>
            CreateMatrix().AssertNoContentNamesAnyCapability(
                TokenAndMarkerCapabilities,
                "Brogan's nineteen faces place no token and give no marker");

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
