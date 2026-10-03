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
    /// Tali the Spiritcaller's half of the permanent hero player-path completion gate. Milestone
    /// 8.37, and the twelfth hero to take it.
    ///
    /// <para><b>What is new here and what is reused.</b> The rules are
    /// <see cref="HeroPlayerPathMatrix"/>'s and are shared with the nine gates before it. This
    /// milestone widened <c>CapabilityShapes</c> with two shapes no dealt hero's content had reached
    /// before - <see cref="DirectionChoice"/> (Cold Snap and Snowstorm's free-direction batch Move) and
    /// <see cref="ColorNamingChoice"/> (Commune with Spirits' and Reign of Winter's naming step) - and
    /// six capability ids: <c>tali-place-ice-token</c>, <c>tali-reclaim-for-ice-token</c> and their four
    /// totem siblings, <c>friendly-hero-in-play</c>, <c>synthetic-choose-direction</c>, <c>name-color</c>,
    /// <c>synthetic-batch-direct-move</c>, <c>defended-card-color</c>,
    /// <c>synthetic-source-card-in-discard</c>, <c>borrow-primary-action</c> and
    /// <c>synthetic-card-radius</c> (the last four already reached by Whisper's and Ursafar's own
    /// dealing preps, landed on this base ahead of this one).</para>
    ///
    /// <para><b>Why this table was derived rather than written.</b> Nothing here declares what a face
    /// does; <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> reads that off
    /// <see cref="TaliCardContent"/> and <see cref="TaliEffectContent"/> - action kind, origin, printed
    /// range, card-text timing, effect node kind, failure policy, capability identity. Every row below
    /// is written FROM that derivation, offline, the way milestone 8.35z34's method requires - and it is
    /// checked against the source rather than assumed, at
    /// <c>tmp/8-35d-8-35e-multiplayer-corrections/worker-talideal-prep/probes/gate/</c>, whose printed
    /// output for all nineteen faces is reproduced in this milestone's own notes. Milestone 8.37a
    /// re-derived all nineteen against the composed Blizzard at
    /// <c>tmp/8-37a-blizzard-repeat/probes/gate/</c>: eighteen came back byte-identical and Blizzard's
    /// row below is written from the nineteenth.</para>
    ///
    /// <para><b>THE BORROW DISPATCH IS PARTIAL, and the one row that touches it says so.</b> Commune
    /// with Spirits' own row proves the naming and the routing and stops there:
    /// <see cref="TaliProgressedUiJourneyPlayModeTests.CommuneWithSpirits_LetsTheNamedAllySteerTheColorRatherThanTaliHerself"/>
    /// never lets the optional discard actually stand a card in Tali's own discard, because doing so
    /// would hand the following <c>borrow-primary-action</c> node a real candidate and drive straight
    /// into the Ursafar executor's still-unbuilt "start it" authority. Her own
    /// <see cref="PlayerInteractionShape.HandCardSelection"/> cell is proved instead by
    /// <see cref="TaliProgressedUiJourneyPlayModeTests.GuardianSpirit_TargetsHerselfAndRetrievesTheCardSheSpentAsADefence"/>
    /// - a different capability, the identical card-instance-picking shape, and one that never touches
    /// the borrow authority at all. See the checklist for exactly what completing the dispatch still
    /// owes.</para>
    ///
    /// <para><b>MILESTONE 8.37a CLOSED THE TABLE.</b> The dealing shipped two rows this gate could
    /// not fill: Blizzard as an <c>UnreachableFace</c>, because its second sentence had no composition,
    /// and Guardian Spirit's journey <c>[Explicit]</c>. Both are ordinary rows now, and only Ice Shard
    /// - p.20's Handicap card, returned to the box at setup, exactly as every hero's is - remains
    /// unreachable. Every journey this table names has been RUN, which was not true when the dealing
    /// shipped: see <see cref="TaliUiJourneyPlayModeTests"/>'s own remarks for what the first pass
    /// corrected.</para>
    /// </summary>
    public sealed class TaliPlayerPathGateTests
    {
        private const string Spawn =
            "HeroPiece_StandsOnTalisOwnBlueSeatAndIsPointedAtByABoardClick";
        private const string TrayCommitAndWalk =
            "Tali_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace";
        private const string DrawerAndRestart =
            "TheDirectorDrawer_UndoesTalisCommitmentAndRestartsHerOntoHerStagedSpace";
        private const string FootprintConformance =
            "Scene_DrawsEveryTaliFaceAtTheStandardCardFootprint";
        private const string DefeatAndRespawn =
            "Tali_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick";

        /// <summary>Glacial Barrier's own mandatory Ice placement - Wall of Frost and Pack Ice share it.</summary>
        private const string GlacialBarrierJourney =
            "GlacialBarrier_PlacesAnIceTokenAtASpaceClickedInsideItsPrintedRadius";

        /// <summary>Cold Snap's free-direction batch Move - Snowstorm shares it.</summary>
        private const string ColdSnapJourney =
            "ColdSnap_ChoosesAFreeDirectionThenMovesTheEnemyOneSpaceWithNoSecondQuestion";

        /// <summary>The direction choice answered again after an Undo and a restore.</summary>
        private const string ColdSnapUndoRestore =
            "ColdSnap_TheDirectionChoiceSurvivesAnUndoAndASnapshotRestoreBeforeTheBatchApplies";

        /// <summary>Winter Dagger's own dynamic +3 - Winter Spear and Winter Scepter share the reading.</summary>
        private const string WinterDaggerJourney =
            "WinterDagger_AppliesItsPlusThreeWhenPlayedFreshBecauseItIsNotYetInTheDiscard";

        /// <summary>Ice Blast's own rider gate, driven to its honest "if able" fizzle.</summary>
        private const string IceBlastJourney =
            "IceBlast_TheRidersGateOpensOnADefendedAttack_AndTheDiscardFizzlesForWantOfAnotherEnemy";

        /// <summary>Commune's naming, routed to the ally rather than to Tali.</summary>
        private const string CommuneJourney =
            "CommuneWithSpirits_LetsTheNamedAllySteerTheColorRatherThanTaliHerself";

        /// <summary>The ally's own naming pause, answered again after an Undo and a restore.</summary>
        private const string CommuneUndoRestore =
            "CommuneWithSpirits_TheAllysNamingPauseSurvivesAnUndoAndASnapshotRestore";

        /// <summary>Venerated Totem's own placement - Ancestral Totem shares the mechanism.</summary>
        private const string VeneratedTotemJourney =
            "VeneratedTotem_SavesAnAdjacentMinionMandatorily_AndRefusesTheEnemysClear";

        /// <summary>Spirit Wolf's own two-bullet Choose - Spirit Bear shares the identical composition.</summary>
        private const string SpiritModalJourney =
            "SpiritWolf_ChoosesTheInRangeBulletBecauseTheCardIsFreshAndNotInTheDiscard";

        /// <summary>Guardian Spirit's own select-then-retrieve - Warrior Spirit shares the composition.</summary>
        private const string RetrievalJourney =
            "GuardianSpirit_TargetsHerselfAndRetrievesTheCardSheSpentAsADefence";

        /// <summary>The Gold trigger, fired by a basic Attack that defeats a minion.</summary>
        private const string ReignOfWinterJourney =
            "ReignOfWinter_OpensTheColorNamingChoiceWhenABasicAttackDefeatsAMinion";

        /// <summary>
        /// Milestone 8.37a. Both of Blizzard's sentences on one turn: the printed batch, then the same
        /// two nodes offered again at the turn's own close under the clause's "May".
        /// </summary>
        private const string BlizzardJourney =
            "Blizzard_RepeatsItsOwnBatchMoveAtTheCloseOfHerTurn_AndTheArmIsThenSpent";

        /// <summary>The same clause refused, which a one-shot offer cannot also be in the journey above.</summary>
        private const string BlizzardDeclineJourney =
            "Blizzard_TheEndOfTurnRepeatCanBeRefused_AndTheTurnClosesWithTheArmSpent";

        // -----------------------------------------------------------------------------------------
        // The matrix. One row per printed face in the shard's table order, then the hero-owned
        // components. Read it beside Docs/AI/Rules/HERO_TALI.md's inventory table.
        // -----------------------------------------------------------------------------------------

        private static HeroPlayerPathMatrix CreateMatrix() => new HeroPlayerPathMatrix(
            "Tali",
            TaliCardContent.AllDefinitions,
            TaliCardContent.ReturnedToBoxAtSetup,
            TaliEffectContent.RegisteredHandlers()
                // Milestone 8.37a. Blizzard's arm, which is what makes her Active-effect readings
                // derivable at all - the gate classifies a capability that CREATES an effect from the
                // definition it creates, so the handler has to be here or the row would be guessed.
                .Concat(TaliActiveEffectContent.Handlers())
                .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers())
                .Concat(SyntheticDisplacementCapabilities.DeferredHandlers())
                .Concat(SyntheticRosterCapabilities.DeferredHandlers()),
            // Milestone 8.37a deliberately does NOT add Blizzard's trigger here. This list resolves an
            // ULTIMATE rule's own triggered effect, which the card carries only by reference; an Active
            // effect's trigger is reached through the definition its capability creates, and the two
            // reading shapes are what the gate says about it - exactly as Silverarrow's own two
            // End-of-turn rows do, neither of which declares the questions its trigger asks.
            new[] { TaliEffectContent.ReignOfWinterTriggeredEffect(TaliCardContent.ContentRevision) },
            new[]
            {
                // The Gold basic. "Target a unit adjacent to you." plus a second body - the roster's
                // first face to gate its own After-the-attack rider on the DEFENDER'S discarded card's
                // colour rather than on anything the attacker chose.
                Face(TaliCardContent.IceBlast, "Ice Blast", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, IceBlastJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, IceBlastJourney),
                        // The step-7 rider, which the offline probe's own recorded output missed:
                        // it lists five shapes for this face and the content introduces eight. The
                        // three below are the rider - the After-the-attack moment it runs at, the
                        // enemy hero its `choose-target` names, and the colour-restricted discard
                        // taken off them. Her own journey drives the moment and watches the discard
                        // fizzle for want of a second enemy on a one-versus-one board; the two
                        // shapes that fizzle is short of are proved where they actually open, which
                        // is the ordinary reading every other row here uses for a shape a face
                        // introduces but its own journey cannot reach.
                        Prove(AfterAttackTextPause, IceBlastJourney),
                        Prove(BoardUnitSelection, CommuneJourney),
                        Prove(HandCardSelection, RetrievalJourney)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests.HerOnlyBasicAttackIsIceBlast_AndHerOnlyBasicSkillIsCommune"
                    }),

                // p.20's Handicap card. p.6 returns it to the box and the six-player pitch never
                // brings it back.
                UnreachableFace(TaliCardContent.IceShard, "Ice Shard",
                    "p.20's Handicap card, parked by the standing 2026-08-21 decision rather than by " +
                    "any missing vocabulary - p.6 returns it to the box and no configuration of this " +
                    "build composes a first odd-player-count pitch"),

                // The Silver basic and only basic Skill - the roster's first consumer of a naming step
                // directed to a hero other than the actor.
                Face(TaliCardContent.CommuneWithSpirits, "Commune with Spirits", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, CommuneJourney),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, CommuneJourney),
                        Prove(BoardUnitSelection, CommuneJourney),
                        Prove(HandCardSelection, RetrievalJourney),
                        Prove(ColorNamingChoice, CommuneJourney)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests" +
                            ".EightFacesReadTheirOwnDiscardState_WhichIsWhatMakesCommuneTheHero",
                        "TaliVerifiedContentTests" +
                            ".CardColourNowHasSixPrintedColours_AndAllSixNameCommuneAPool"
                    }),

                // The Red line's Tier I. "+3 Attack if this card is not in the discard" - a value
                // contribution rather than a node, on Sabina's Quickdraw template.
                Face(TaliCardContent.WinterDagger, "Winter Dagger", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, WinterDaggerJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests" +
                            ".EachAttackPrintsItsBaseValue_WithTheRulebooksPlusCueLeftToTheCardText"
                    }),

                Face(TaliCardContent.WinterSpear, "Winter Spear", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, WinterDaggerJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    Array.Empty<string>()),

                // The Red line's Tier II/III modal - the roster's first Choose gated by a condition on
                // the SOURCE CARD rather than by a candidate-level narrowing.
                Face(TaliCardContent.SpiritWolf, "Spirit Wolf", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SpiritModalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, SpiritModalJourney),
                        Prove(ModalBulletChoice, SpiritModalJourney)
                    },
                    Array.Empty<string>()),

                Face(TaliCardContent.WinterScepter, "Winter Scepter", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(AdjacentAttackTarget, WinterDaggerJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn)
                    },
                    Array.Empty<string>()),

                Face(TaliCardContent.SpiritBear, "Spirit Bear", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(RangedAttackTarget, SpiritModalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BeforeAttackTextPause, SpiritModalJourney),
                        Prove(ModalBulletChoice, SpiritModalJourney)
                    },
                    Array.Empty<string>()),

                // The Blue line's Tier I. The roster's first free-direction batch Move - one control,
                // then a mandatory batch with no second question, because "each" names a set.
                Face(TaliCardContent.ColdSnap, "Cold Snap", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ColdSnapJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(DirectionChoice, ColdSnapJourney)
                    },
                    Array.Empty<string>()),

                Face(TaliCardContent.Snowstorm, "Snowstorm", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, ColdSnapJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(DirectionChoice, ColdSnapJourney)
                    },
                    Array.Empty<string>()),

                // The Green line's Tier II/III retrieval - always finds at least herself, then a real
                // May-policy card-instance choice once her own discard holds a candidate.
                Face(TaliCardContent.GuardianSpirit, "Guardian Spirit", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(BoardUnitSelection, RetrievalJourney),
                        Prove(HandCardSelection, RetrievalJourney)
                    },
                    Array.Empty<string>()),

                // Milestone 8.37a. Blizzard was this table's second UnreachableFace and is now a row
                // like any other - the last face in this build whose printed text was recorded absent.
                // Its first sentence is Cold Snap's and Snowstorm's, so it declares their shapes; what
                // it adds is the pair of readings its own second sentence arms, and one journey drives
                // both sentences on one turn because that is where the clause happens.
                Face(TaliCardContent.Blizzard, "Blizzard", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, BlizzardJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(DirectionChoice, BlizzardJourney),
                        Prove(ActiveEffectReading, BlizzardJourney),
                        // Silverarrow's own End-of-turn rows carry this pair for the same reason and
                        // the same duration: `RoundLongActiveEffectReading` is added by every duration
                        // except `This turn`, and `EndOfTurn` is not that one. What there is to prove
                        // of the pair is that the arm is readable while it stands and gone the moment
                        // its checkpoint has fired, and BOTH journeys prove exactly that - so the two
                        // cells are split between them rather than doubled onto one. They differ in
                        // the only way a one-shot offer can: one takes the repeat and one refuses it,
                        // which no single journey can do.
                        Prove(RoundLongActiveEffectReading, BlizzardDeclineJourney),
                        Prove(ConfirmOrDecline, BlizzardJourney)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests" +
                            ".Blizzard_ArmsAnEndOfTurnEffectWhoseTriggerRepeatsItsOwnTwoNodes"
                    }),

                Face(TaliCardContent.WarriorSpirit, "Warrior Spirit", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, RetrievalJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(BoardUnitSelection, RetrievalJourney),
                        Prove(HandCardSelection, RetrievalJourney)
                    },
                    Array.Empty<string>()),

                // The Green line's Tier I Ice placement - the roster's first mandatory single placement
                // with no softener, and the aura RULING 25 gives no floor.
                Face(TaliCardContent.GlacialBarrier, "Glacial Barrier", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GlacialBarrierJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardSpaceSelection, GlacialBarrierJourney)
                    },
                    new[]
                    {
                        "TaliTokenSystemTests.AnIceTokenGivesTheEnemyHeroBesideItMinusOneInitiative",
                        "TaliTokenSystemTests" +
                            ".AHeroAdjacentToThreeIceTokensIsAtMinusThree_WithNoFloorAndNothingCounting"
                    }),

                // Wall of Frost's own softener (the placement may be declined) is the same broad
                // decision shape Guardian Spirit's May-policy retrieve already proves elsewhere in this
                // set, reused here rather than driven a second time on an identical mechanism.
                Face(TaliCardContent.WallOfFrost, "Wall of Frost", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GlacialBarrierJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(BoardSpaceSelection, GlacialBarrierJourney)
                    },
                    Array.Empty<string>()),

                // The Green line's Tier II/III totem placement, on the mandatory-prevention wiring this
                // milestone closed: the save fires with nobody asked, and RULING 26's immunity refuses
                // the enemy's own Clear.
                Face(TaliCardContent.AncestralTotem, "Ancestral Totem", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, VeneratedTotemJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardSpaceSelection, VeneratedTotemJourney)
                    },
                    new[]
                    {
                        "TaliTokenSystemTests" +
                            ".AncestralTotemSavesOnlyMeleeMinions_AndVeneratedSavesAnyFriendlyMinion"
                    }),

                Face(TaliCardContent.PackIce, "Pack Ice", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, GlacialBarrierJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(ConfirmOrDecline, RetrievalJourney),
                        Prove(BoardSpaceSelection, GlacialBarrierJourney)
                    },
                    new[]
                    {
                        "TaliTokenSystemTests.TheShortSupplyStepReachesAcrossRoles"
                    }),

                Face(TaliCardContent.VeneratedTotem, "Venerated Totem", new[]
                    {
                        Prove(CommitAndReveal, TrayCommitAndWalk),
                        Prove(PrimarySkillControl, VeneratedTotemJourney),
                        Prove(SecondaryMovementDestination, TrayCommitAndWalk),
                        Prove(SecondaryDefenseOffer, DefeatAndRespawn),
                        Prove(BoardSpaceSelection, VeneratedTotemJourney)
                    },
                    new[]
                    {
                        "TaliTokenSystemTests" +
                            ".TheMandatoryTotemSaveReplacesDefeatBeforeRewardsWithoutARemovalStageChoice",
                        "TaliTokenSystemTests.ANonAdjacentMinionIsNotAutomaticallyPrevented",
                        "TaliTokenSystemTests.AnEnemysClearIsRefusedAgainstAVeneratedTotemAndAllowedAgainstAnAncestralOne"
                    }),

                // The Gold Ultimate. A TRIGGERED ability rather than a standing declaration - the
                // roster's first Ultimate to open a real pause of its own once its condition reads true.
                Face(TaliCardContent.ReignOfWinter, "Reign of Winter", new[]
                    {
                        Prove(BoardUnitSelection, ReignOfWinterJourney),
                        Prove(HandCardSelection, RetrievalJourney),
                        Prove(UltimateRailVisibility, ReignOfWinterJourney),
                        Prove(UltimateTriggerPause, ReignOfWinterJourney),
                        Prove(ColorNamingChoice, ReignOfWinterJourney)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests.TheUltimateIsTierFour_PrintsNoAction_AndCarriesItsRadius"
                    }),

                // -------------------------------------------------------------------------------
                // The hero-owned components.
                // -------------------------------------------------------------------------------

                Component("tali-hero-piece", "her token, its identity and its movement", new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, TrayCommitAndWalk),
                        Prove(HeroPieceDefeatAndRespawn, DefeatAndRespawn),
                        Prove(PresentationRebuild, DrawerAndRestart)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests.TheSetIsNineteenFaces_EighteenOfWhichAMatchDeals"
                    }),

                Component("tali-session-operations", "restore, Undo, replay and restart", new[]
                    {
                        Prove(SnapshotRestoreContinuation, ColdSnapUndoRestore),
                        Prove(RetainedPrefixUndo, ColdSnapUndoRestore),
                        Prove(ReplayReconstruction, CommuneUndoRestore),
                        Prove(RestartReconstruction, DrawerAndRestart)
                    },
                    new[]
                    {
                        "TaliVerifiedContentTests.TheSetIsNineteenFaces_EighteenOfWhichAMatchDeals"
                    }),

                Component("tali-card-footprint",
                    "every verified face at the standard card footprint",
                    new[] { Prove(StandardCardFootprintConformance, FootprintConformance) },
                    new[]
                    {
                        "MatchHudPresentationTests" +
                            ".EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint",
                        "MatchHudPresentationTests" +
                            ".NoCardStateOrContent_ChangesTheStandardFootprint"
                    }),

                // She is the first verified hero whose profile card's Tokens role icon is printed at
                // all - the placements her five Green faces already prove are what stands here rather
                // than an absence.
                Component("tali-tokens-and-markers",
                    "the Ice and Totem tokens her Green faces place, from one shared supply of three",
                    new[] { Prove(BoardSpaceSelection, GlacialBarrierJourney) },
                    new[]
                    {
                        "TaliTokenSystemTests.AllThreeRolesDeclareOnePileOfThree",
                        "TaliTokenSystemTests.TheShortSupplyStepReachesAcrossRoles"
                    }),

                // p.16's item track, on Ursafar's own terms rather than as an absence.
                //
                // CORRECTED AT THE FIRST PLAY MODE RUN. This row was written as an
                // `AbsentComponent` claiming "Tali's nineteen faces print no item icon anywhere in
                // the set", with its own bespoke assertion below saying the same. She prints TWELVE:
                // six on her Tier II cards (two each of Attack, Defense and Initiative) and six on
                // her Tier III cards, one of every kind p.16 has. The claim was never true, and no
                // offline tier could contradict it - the gate derives SHAPES from content, and an
                // item icon introduces no shape, so only the assertion that actually reads
                // `Progression.HasItemIcon` could catch it, and that assertion was the one asserting
                // the falsehood. It is now the ordinary declared debt every other hero's item row is.
                AbsentComponent("tali-items", "p.16's items, from the upgrade sibling not taken",
                    "p.16 gains an item only through p.15's Level Up, which no composed scene content " +
                    "stages and which no journey can earn its way to; her twelve item icons cover all " +
                    "six of p.16's kinds, pinned at the definition tier")
            });

        [Test]
        public void EveryTaliFace_HasExactlyOneMatrixRow() =>
            CreateMatrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void MatrixRowsDeclareExactlyTheShapesTalisPrintedContentIntroduces() =>
            CreateMatrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void EveryReachableFace_NamesAJourneyAndEveryUnreachableOneNamesItsReason() =>
            CreateMatrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryDeclaredShape_IsProvedByAJourneyThatExists() =>
            CreateMatrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(TaliPlayerPathGateTests).Assembly);

        [Test]
        public void MatrixNamesOnlyJourneysThatExist() =>
            CreateMatrix().AssertEveryNamedJourneyExists(
                typeof(TaliPlayerPathGateTests).Assembly);

        [Test]
        public void EveryTaliJourney_IsAccountedForByARow() =>
            CreateMatrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(TaliUiJourneyPlayModeTests),
                typeof(TaliEngagedUiJourneyPlayModeTests),
                typeof(TaliProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryComponentShapeTheGateRequires_HasAComponentRow() =>
            CreateMatrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void TheItemRowRecordsWhyNoJourneyReachesIt_AndTaliActuallyPrintsItemIcons() =>
            CreateMatrix().AssertDeclaredDebtHasItsSubject(
                "tali-items",
                TaliCardContent.AllDefinitions.Any(definition =>
                    definition.Progression.HasItemIcon),
                "p.16's item track");

        /// <summary>
        /// The count the row above claims, measured rather than restated. Twelve is not an arbitrary
        /// number: p.15 gives every Tier II and Tier III card an icon, and she has six of each.
        /// </summary>
        [Test]
        public void HerTwelveItemIcons_CoverEveryKindPageSixteenHas() =>
            Assert.That(
                TaliCardContent.AllDefinitions
                    .Where(definition => definition.Progression.HasItemIcon)
                    .Select(definition => definition.Progression.ItemIcon)
                    .Distinct()
                    .OrderBy(channel => channel)
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.Attack,
                    PrintedValueChannel.Defense,
                    PrintedValueChannel.Movement,
                    PrintedValueChannel.Range,
                    PrintedValueChannel.Radius,
                    PrintedValueChannel.Initiative
                }.OrderBy(channel => channel).ToArray()),
                "Her twelve item icons cover all six of p.16's kinds.");

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
