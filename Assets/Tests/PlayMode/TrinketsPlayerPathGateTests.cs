using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Fixed, content-checked claims for each dealt Trinkets face and component.</summary>
    public sealed class TrinketsPlayerPathGateTests
    {
        private const string Rapid = nameof(TrinketsUiJourneyPlayModeTests.RapidRedeploymentPlacesAndRepositionsOnlyOnePersistentTurret);
        private const string RapidMinion = nameof(TrinketsUiJourneyPlayModeTests.RapidRedeploymentDefeatsTheAdjacentMinionOnItsOtherBullet);
        private const string Salvage = nameof(TrinketsUiJourneyPlayModeTests.SalvagePartsCanPlaceOrRemoveTheTurretThroughItsNamedBullets);
        private const string Retrieve = nameof(TrinketsUiJourneyPlayModeTests.SalvagePartsRemovesTurretAndRetrievesTheDefenseCardJustSpent);
        private const string Red = nameof(TrinketsUiJourneyPlayModeTests.FiveRedFacesTakeOnlyTurretQualifiedTargetsThroughTheAttackControl);
        private const string RepeatRed = nameof(TrinketsUiJourneyPlayModeTests.SteamAndFlameAcceptTheirPrintedRepeatAgainstADifferentEnemyUnit);
        private const string Disruptor = nameof(TrinketsUiJourneyPlayModeTests.ThreeDisruptorFacesActivateFromTheirPrintedSkillControl);
        private const string DisruptorTrigger = nameof(TrinketsUiJourneyPlayModeTests.DisruptorTriggerPrecedesEnemyPrimaryAndUsesTheirRemainingHand);
        private const string Barrier = nameof(TrinketsUiJourneyPlayModeTests.BothBarrierFacesPlaceTheirPrintedCountThroughMapChoices);
        private const string Prototype = nameof(TrinketsUiJourneyPlayModeTests.PrototypeFacesSwapWithTheChosenBoardPieceAndObserveTheirTurretClause);
        private const string Perfected = nameof(TrinketsUiJourneyPlayModeTests.PerfectedDesignPlacesTrinketsInTheTurretRadiusWithoutMovingTheTurret);
        private const string PerfectedSwap = nameof(TrinketsUiJourneyPlayModeTests.PerfectedDesignAlsoSwapsWithTheChosenUnitWithoutRemovingTurret);
        private const string Detonation = nameof(TrinketsUiJourneyPlayModeTests.DetonationFacesAskTheEnemyForDiscardAndRemoveTheTurret);
        private const string Movement = nameof(TrinketsUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesTrinketsThroughItsCardControl);
        private const string Defense = nameof(TrinketsUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Continuity = nameof(TrinketsUiJourneyPlayModeTests.TurretPlacementPauseRestoresAndRetainedPrefixUndoReplaysExactly);
        private const string Lifecycle = nameof(TrinketsUiJourneyPlayModeTests.TurretRemainsVisibleAfterTrinketsDefeatAndTheRoundBoundary);
        private const string Spawn = nameof(TrinketsUiJourneyPlayModeTests.StagedTrinketsSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(TrinketsUiJourneyPlayModeTests.EveryTrinketsFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(TrinketsUiJourneyPlayModeTests.TrinketsLevelUpAddsTheChosenCardAndShowsItsSiblingItem);
        private const string Ultimate = nameof(TrinketsUiJourneyPlayModeTests.UnlimitedFirepowerIsVisibleInTheTierThreeConfiguredScene);

        private static PlayerPathRow Ordinary(CardDefinitionId id, string title,
            string journey, PlayerInteractionShape[] shapes, string scenario,
            bool movement = true, string specialJourney = null) => Face(id, title,
                new[] { Prove(CommitAndReveal, journey), Prove(SecondaryDefenseOffer, Defense) }
                    .Concat(movement
                        ? new[] { Prove(SecondaryMovementDestination, Movement) }
                        : Array.Empty<ShapeEvidence>())
                    .Concat(shapes.Select(shape => Prove(shape,
                        specialJourney != null && (shape == AfterAttackTextPause ||
                            shape == ConfirmOrDecline || shape == RepeatDecision ||
                            shape == ActiveEffectReading || shape == HandCardSelection)
                            ? specialJourney : journey))).ToArray(),
                new[] { scenario });

        private static readonly PlayerInteractionShape[] RedShapes =
            { RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection };
        private static readonly PlayerInteractionShape[] RepeatRedShapes =
            { RangedAttackTarget, BeforeAttackTextPause, BoardUnitSelection,
                AfterAttackTextPause, ConfirmOrDecline, RepeatDecision };
        private static readonly PlayerInteractionShape[] DisruptorShapes =
            { PrimarySkillControl, ActiveEffectReading };
        private static readonly PlayerInteractionShape[] BarrierShapes =
            { PrimarySkillControl, ConfirmOrDecline, BoardSpaceSelection };
        private static readonly PlayerInteractionShape[] PrototypeShapes =
            { PrimarySkillControl, BoardUnitSelection };
        private static readonly PlayerInteractionShape[] DetonationShapes =
            { PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection, HandCardSelection };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Trinkets", TrinketsCardContent.AllDefinitions,
            TrinketsCardContent.ReturnedToBoxAtSetup,
            TrinketsActiveEffectContent.Handlers(),
            new[]
            {
                TrinketsEffectContent.DisruptorTrigger(TrinketsCardContent.DisruptorJolt,
                    TrinketsCardContent.ContentRevision, false),
                TrinketsEffectContent.DisruptorTrigger(TrinketsCardContent.DisruptorPulse,
                    TrinketsCardContent.ContentRevision, false),
                TrinketsEffectContent.DisruptorTrigger(TrinketsCardContent.DisruptorGrid,
                    TrinketsCardContent.ContentRevision, true)
            }, new[]
            {
                Face(TrinketsCardContent.RapidRedeployment, "Rapid Redeployment", new[]
                {
                    Prove(CommitAndReveal, Rapid),
                    Prove(PrimarySkillControl, Rapid),
                    Prove(SecondaryMovementDestination, Movement),
                    Prove(SecondaryDefenseOffer, Defense),
                    Prove(BoardUnitSelection, RapidMinion),
                    Prove(BoardSpaceSelection, Rapid),
                    Prove(ModalBulletChoice, RapidMinion)
                }, new[] { "TrinketsBasicSkillTests.RapidRedeployment_MoveThenPlace_RepositionsOnlyOnePersistentTurret" }),
                Ordinary(TrinketsCardContent.SalvageParts, "Salvage Parts", Salvage,
                    new[] { PrimarySkillControl, ConfirmOrDecline, BoardSpaceSelection,
                        HandCardSelection, ModalBulletChoice },
                    "TrinketsBasicSkillTests.SalvageParts_PlaceOrRemoveMove_UsesLiveTurret",
                    false, Retrieve),
                Ordinary(TrinketsCardContent.MakeshiftMinigun, "Makeshift Minigun", Red,
                    RedShapes, "TrinketsRedTurnTests"),
                Ordinary(TrinketsCardContent.GatlingGun, "Gatling Gun", Red,
                    RedShapes, "TrinketsRedTurnTests"),
                Ordinary(TrinketsCardContent.SteamDischarge, "Steam Discharge", Red,
                    RepeatRedShapes, "TrinketsRedTurnTests.SteamAndFlame_RepeatOffersExcludeTheFirstUnit",
                    specialJourney: RepeatRed),
                Ordinary(TrinketsCardContent.SuperchargedCannon, "Supercharged Cannon", Red,
                    RedShapes, "TrinketsRedTurnTests"),
                Ordinary(TrinketsCardContent.FlameBelcher, "Flame Belcher", Red,
                    RepeatRedShapes, "TrinketsRedTurnTests.SteamAndFlame_RepeatOffersExcludeTheFirstUnit",
                    specialJourney: RepeatRed),
                Ordinary(TrinketsCardContent.DisruptorJolt, "Disruptor Jolt", Disruptor,
                    DisruptorShapes, "TrinketsBlueDefenseTimingTests.SolePrimaryDefenseCard_IsDiscardedBeforeDefenseOffer",
                    specialJourney: DisruptorTrigger),
                Ordinary(TrinketsCardContent.DisruptorPulse, "Disruptor Pulse", Disruptor,
                    DisruptorShapes, "TrinketsBarrierSkillTests.DisruptorTrigger_UsesLiveTurretDiskAndEnemyPrimaryOnly",
                    specialJourney: DisruptorTrigger),
                Ordinary(TrinketsCardContent.DisruptorGrid, "Disruptor Grid", Disruptor,
                    DisruptorShapes, "TrinketsBarrierSkillTests.GridWithEmptyEnemyHand_RequestsDefeatWhereJoltDoesNot",
                    specialJourney: DisruptorTrigger),
                Ordinary(TrinketsCardContent.DeployableBarrier, "Deployable Barrier", Barrier,
                    BarrierShapes, "TrinketsBarrierSkillTests.FirstBarrierMustAdjoinTurret_ThenUpToPrintedMaximumMayBePlaced"),
                Ordinary(TrinketsCardContent.DeployableBastion, "Deployable Bastion", Barrier,
                    BarrierShapes, "TrinketsBarrierSkillTests.FirstBarrierMustAdjoinTurret_ThenUpToPrintedMaximumMayBePlaced"),
                Ordinary(TrinketsCardContent.EarlyPrototype, "Early Prototype", Prototype,
                    PrototypeShapes, "TrinketsGreenSkillTests.PrototypeSwap_UsesTurretRadiusAndPrintedRemoval"),
                Ordinary(TrinketsCardContent.UpdatedDesign, "Updated Design", Prototype,
                    PrototypeShapes, "TrinketsGreenSkillTests.PrototypeSwap_UsesTurretRadiusAndPrintedRemoval"),
                Ordinary(TrinketsCardContent.SelfDestruct, "Self-Destruct", Detonation,
                    DetonationShapes, "TrinketsGreenSkillTests.Detonation_EmptyHandDiscardsIfAbleOrRequestsDefeatAndRemovesTurret"),
                Ordinary(TrinketsCardContent.EmergencyProtocol, "Emergency Protocol", Detonation,
                    DetonationShapes, "TrinketsGreenSkillTests.Detonation_EmptyHandDiscardsIfAbleOrRequestsDefeatAndRemovesTurret"),
                Face(TrinketsCardContent.PerfectedDesign, "Perfected Design", new[]
                {
                    Prove(CommitAndReveal, Perfected),
                    Prove(PrimarySkillControl, Perfected),
                    Prove(SecondaryMovementDestination, Movement),
                    Prove(SecondaryDefenseOffer, Defense),
                    Prove(BoardUnitSelection, PerfectedSwap),
                    Prove(BoardSpaceSelection, Perfected),
                    Prove(ModalBulletChoice, PerfectedSwap)
                }, new[] { "TrinketsGreenSkillTests.PerfectedDesign_PlacesSelfInLiveTurretRadiusWithoutRemovingIt" }),
                Face(TrinketsCardContent.UnlimitedFirepower, "Unlimited Firepower",
                    new[] { Prove(UltimateRailVisibility, Ultimate) },
                    new[] { "TrinketsCompleteSetTests.UltimateStandingSource_AddsOnceToRedRangeAndTurretCentredGreenRadius" }),
                UnreachableFace(TrinketsCardContent.RamshackleTurret,
                    "Ramshackle Turret", "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("trinkets-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "TrinketsDealingTests" }),
                Component("trinkets-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Continuity),
                    Prove(RetainedPrefixUndo, Continuity),
                    Prove(ReplayReconstruction, Continuity),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "TrinketsComponentFoundationTests.ComponentPreimageRestoreAndDeterministicReplay_KeepBothKindsDistinct" }),
                Component("trinkets-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "TrinketsCompleteSetTests.NineteenFaces_MatchInventoryAndOnlyHandicapIsBoxed" }),
                Component("trinkets-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "TrinketsCompleteSetTests.UltimateStandingSource_AddsOnceToRedRangeAndTurretCentredGreenRadius" }),
                Component("trinkets-barriers", "Three end-of-round Barrier tokens",
                    new[] { Prove(BoardSpaceSelection, Barrier) },
                    new[] { "TrinketsComponentFoundationTests.Barrier_HasThreePhysicalCopiesAndAnEndOfRoundTokenLifetime" }),
                Component("trinkets-turret", "One persistent obstacle",
                    new[] { Prove(BoardSpaceSelection, Rapid), Prove(PresentationRebuild, Lifecycle) },
                    new[] {
                        "TrinketsComponentFoundationTests.RoundCleanup_RemovesBarrierButNotTheDistinctTurretObstacle",
                        "TrinketsComponentFoundationTests.GenericObstacleAppearsIdenticallyInBothOwnerScopedSeatProjections"
                    }),
                AbsentComponent("trinkets-markers", "Hero-owned markers",
                    "No Trinkets face gives, transfers or returns a marker.")
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(TrinketsPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(TrinketsPlayerPathGateTests).Assembly);
        [Test] public void EveryTrinketsJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(TrinketsUiJourneyPlayModeTests));
        [Test] public void EveryRequiredComponentShapeIsDeclared() => Matrix().AssertEveryRequiredComponentShapeIsDeclared();
        [Test] public void EveryAuthoritativeScenarioExists()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(value =>
                value.GetName().Name == "GuardsDigital.EditModeTests");
            foreach (var scenario in Matrix().Rows.SelectMany(row => row.AuthoritativeScenarios))
            {
                var parts = scenario.Split('.');
                var fixture = assembly.GetTypes().SingleOrDefault(type => type.Name == parts[0]);
                Assert.That(fixture, Is.Not.Null, scenario);
                if (parts.Length > 1)
                    Assert.That(fixture.GetMethod(parts[1]), Is.Not.Null, scenario);
            }
        }
    }
}
