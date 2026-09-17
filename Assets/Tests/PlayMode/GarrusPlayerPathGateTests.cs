using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Explicit coverage claims, independently checked against the executable printed set.</summary>
    public sealed class GarrusPlayerPathGateTests
    {
        private const string Secondary = nameof(GarrusLifecycleJourneyTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string Angry = nameof(GarrusLifecycleJourneyTests.AngryStrikeCountsHisDiscardsWhenTheHeroIsTargeted);
        private const string Blue = nameof(GarrusUiJourneyTests.BlueFacesMoveTheChosenUnitExactlyOneAndOfferTheirPrintedRepeats);
        private const string Pursuit = nameof(GarrusUiJourneyTests.PursuitFacesTakeBothBulletsAndMoveBeforeSelectingTheHero);
        private const string Push = nameof(GarrusUiJourneyTests.BothPushAttacksAcceptZeroMoveAndPushTheSeparatelyChosenHero);
        private const string Pilum = nameof(GarrusUiJourneyTests.PilumsAskTheOpponentForACardThenOfferTheirPrintedMove);
        private const string Retrieve = nameof(GarrusLifecycleJourneyTests.AllThreeRetrievalFacesChooseDiscardsWithTwoEnemyHeroesPresent);
        private const string Fury = nameof(GarrusUiJourneyTests.HowlDiscardsAResolvedCardAndBattleFuryResumesThroughUndoAndRestore);
        private const string Howl = nameof(GarrusLifecycleJourneyTests.HowlRestrictsTheOpponentAndExpiresWhenTheRoundRetrievesIt);
        private const string Lifecycle = nameof(GarrusLifecycleJourneyTests.HisDefeatedPieceRespawnsAndHisSessionRebuildsWithoutDuplicates);
        private const string Upgrade = nameof(GarrusLifecycleJourneyTests.LevelUpChoosesTheWholeUpgradeAndShowsItsSiblingItem);
        private const string Spawn = nameof(GarrusSceneIntegrationTests.EveryStagedBranchSpawnsGarrusWithHisOwnArtworkAndPointer);
        private const string Footprint = nameof(GarrusSceneIntegrationTests.EveryGarrusFaceUsesTheStandardFootprint);

        // These fixed sets were measured by the offline shape probe; they are not computed from the
        // content under test. A later capability, node, or printed-action change must fail this gate.
        private static readonly PlayerInteractionShape[] PursuitShapes =
            { AdjacentAttackTarget, BeforeAttackTextPause, BoardUnitSelection, BoardSpaceSelection, ModalBulletChoice };
        private static readonly PlayerInteractionShape[] PushShapes =
            { AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline, BoardUnitSelection, BoardSpaceSelection };
        private static readonly PlayerInteractionShape[] BlueShapes =
            { PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection };
        private static readonly PlayerInteractionShape[] RepeatShapes =
            { PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection, ConfirmOrDecline, RepeatDecision };
        private static readonly PlayerInteractionShape[] RetrieveShapes =
            { PrimarySkillControl, ConfirmOrDecline, HandCardSelection };
        private static readonly PlayerInteractionShape[] PilumShapes =
            { PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection, BoardSpaceSelection, HandCardSelection };

        private static PlayerPathRow Ordinary(CardDefinitionId id, string name, string journey,
            PlayerInteractionShape[] shapes, string scenario) => Face(id,name,
                new[] { Prove(CommitAndReveal,journey), Prove(SecondaryMovementDestination,Secondary),
                    Prove(SecondaryDefenseOffer,Secondary) }.Concat(shapes.Select(shape => Prove(shape,journey))).ToArray(),
                new[] { scenario });

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix("Garrus",
            GarrusCardContent.AllDefinitions,GarrusCardContent.ReturnedToBoxAtSetup,GarrusActiveEffectContent.Handlers(),
            new[] { GarrusEffectContent.ResolvedDiscardPrimary(GarrusCardContent.BattleFury,GarrusCardContent.ContentRevision) },
            new[] {
                Ordinary(GarrusCardContent.AngryStrike,"Angry Strike",Angry,new[] { AdjacentAttackTarget },
                    "GarrusComposedRoundTests.AngryStrike_ReadsTheLiveDiscardCountFromItsRealDefinition"),
                Face(GarrusCardContent.ChillingHowl,"Chilling Howl",new[] {
                    Prove(CommitAndReveal,Howl), Prove(PrimarySkillControl,Howl), Prove(SecondaryDefenseOffer,Secondary),
                    Prove(ConfirmOrDecline,Fury), Prove(HandCardSelection,Fury), Prove(ActiveEffectReading,Howl),
                    Prove(OpponentRestrictionReading,Howl), Prove(RoundLongActiveEffectReading,Howl) },
                    new[] { "GarrusPrintedSetTests.Howl_OptionalResolvedDiscard_DoesNotGateTheAura",
                        "GarrusComposedRoundTests.Howl_MovementActionIsCappedAtStart_AndDoesNotGrowAfterLeavingTheAura" }),
                Ordinary(GarrusCardContent.Trace,"Trace",Pursuit,PursuitShapes,"GarrusAttackVocabularyTests"),
                Ordinary(GarrusCardContent.Chase,"Chase",Pursuit,PursuitShapes,"GarrusAttackVocabularyTests"),
                Ordinary(GarrusCardContent.HuntDown,"Hunt Down",Pursuit,PursuitShapes,"GarrusAttackVocabularyTests"),
                Ordinary(GarrusCardContent.BluntForce,"Blunt Force",Push,PushShapes,"GarrusComposedRoundTests.PushCards_AttackFirst_ThenZeroMoveCanPushTheSurvivingHero"),
                Ordinary(GarrusCardContent.SendFlying,"Send Flying",Push,PushShapes,"GarrusComposedRoundTests.PushCards_AttackFirst_ThenZeroMoveCanPushTheSurvivingHero"),
                Ordinary(GarrusCardContent.Menace,"Menace",Blue,BlueShapes,"GarrusPrintedSetTests.BlueMoves_ExactlyOneSpace_InThePrintedDirection_AndNeverSelf"),
                Ordinary(GarrusCardContent.Threaten,"Threaten",Blue,RepeatShapes,"GarrusPrintedSetTests.BlueFaces_UseRangeItemsAndTheirPrintedRepeatCount"),
                Ordinary(GarrusCardContent.FormUp,"Form Up",Blue,RepeatShapes,"GarrusPrintedSetTests.BlueFaces_UseRangeItemsAndTheirPrintedRepeatCount"),
                Ordinary(GarrusCardContent.Terrify,"Terrify",Blue,RepeatShapes,"GarrusPrintedSetTests.BlueFaces_UseRangeItemsAndTheirPrintedRepeatCount"),
                Ordinary(GarrusCardContent.Testudo,"Testudo",Blue,RepeatShapes,"GarrusPrintedSetTests.BlueFaces_UseRangeItemsAndTheirPrintedRepeatCount"),
                Ordinary(GarrusCardContent.HoldGround,"Hold Ground",Retrieve,RetrieveShapes,"GarrusPrintedSetTests.Retrieval_RequiresTwoEnemyHeroesInRadius_NotRange"),
                Ordinary(GarrusCardContent.MakeAStand,"Make a Stand",Retrieve,RetrieveShapes,"GarrusPrintedSetTests.Retrieval_RequiresTwoEnemyHeroesInRadius_NotRange"),
                Ordinary(GarrusCardContent.BattleReady,"Battle Ready",Retrieve,RetrieveShapes,"GarrusPrintedSetTests.BattleReady_RetrievesZeroOneOrTwoDistinctDiscards"),
                Ordinary(GarrusCardContent.LightPilum,"Light Pilum",Pilum,PilumShapes,"GarrusPrintedSetTests.Pilum_DiscardBelongsToOpponent_AndTheMoveSurvivesEitherOutcome"),
                Ordinary(GarrusCardContent.HeavyPilum,"Heavy Pilum",Pilum,PilumShapes,"GarrusComposedRoundTests.HeavyPilum_CompletesDiscardOrDefeatBeforeOptionalMovement"),
                Face(GarrusCardContent.BattleFury,"Battle Fury",new[] {
                    Prove(ConfirmOrDecline,Fury),Prove(UltimateRailVisibility,Fury),Prove(UltimateTriggerPause,Fury) },
                    new[] { "GarrusComposedRoundTests.BattleFury_ReplaysTheExactDiscardedThreaten_IncludingItsRepeat_BeforeHowlAura" }),
                UnreachableFace(GarrusCardContent.AngrySwipe,"Angry Swipe","Rulebook p.6 returns the p.20 Handicap face to the box; no configured odd-player format."),
                Component("garrus-piece","Hero piece",new[] { Prove(HeroPieceSpawnAndIdentity,Spawn),
                    Prove(HeroPiecePointerSelection,Spawn),Prove(HeroPieceMovement,Secondary),
                    Prove(HeroPieceDefeatAndRespawn,Lifecycle),Prove(PresentationRebuild,Lifecycle) },new[] { "GarrusDealingTests" }),
                Component("garrus-session","Session continuity",new[] { Prove(SnapshotRestoreContinuation,Fury),
                    Prove(RetainedPrefixUndo,Fury),Prove(ReplayReconstruction,Fury),Prove(RestartReconstruction,Lifecycle) },
                    new[] { "GarrusComposedRoundTests.BattleFury_ReplaysTheExactDiscardedThreaten_IncludingItsRepeat_BeforeHowlAura" }),
                Component("garrus-footprint","Every complete face",new[] { Prove(StandardCardFootprintConformance,Footprint) },
                    new[] { "MatchHudPresentationTests.EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint" }),
                Component("garrus-items","Level-up sibling items",new[] { Prove(ItemVisibilityAndUse,Upgrade) },
                    new[] { "GarrusPrintedSetTests.Upgrades_PreserveEachPrintedColorAndBothNextTierChoices" }),
                AbsentComponent("garrus-tokens","Hero-owned tokens","No Garrus face creates, moves or reclaims a token."),
                AbsentComponent("garrus-markers","Hero-owned markers","No Garrus face gives, transfers or returns a marker.")
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(GarrusPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(GarrusPlayerPathGateTests).Assembly);
        [Test] public void EveryGarrusJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(GarrusUiJourneyTests),typeof(GarrusLifecycleJourneyTests),typeof(GarrusSceneIntegrationTests));
        [Test] public void EveryRequiredComponentShapeIsDeclared() => Matrix().AssertEveryRequiredComponentShapeIsDeclared();
        [Test] public void TokenAndMarkerAbsencesMatchTheContent() => Matrix().AssertNoContentNamesAnyCapability(
            new[] { "synthetic-place-token","synthetic-reclaim-token","tali-place-ice-token","tali-place-ancestral-totem",
                "tali-place-venerated-totem","bain-give-bounty-marker","bain-transfer-bounty-marker","bain-return-bounty-marker" },
            "Garrus owns no token or marker.");
        [Test] public void EveryAuthoritativeScenarioExists()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(value => value.GetName().Name == "GuardsDigital.EditModeTests");
            foreach (var scenario in Matrix().Rows.SelectMany(row => row.AuthoritativeScenarios))
            {
                var parts = scenario.Split('.');
                var fixture = assembly.GetTypes().SingleOrDefault(type => type.Name == parts[0]);
                Assert.That(fixture, Is.Not.Null,scenario);
                if (parts.Length > 1) Assert.That(fixture.GetMethod(parts[1]), Is.Not.Null,scenario);
            }
        }
    }
}
