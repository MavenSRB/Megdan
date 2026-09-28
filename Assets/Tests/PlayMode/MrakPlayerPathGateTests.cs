using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 8.112h. Fixed, content-checked claims for each dealt Mrak face and component.</summary>
    public sealed class MrakPlayerPathGateTests
    {
        private const string Seismic = nameof(MrakUiJourneyPlayModeTests.SeismicSkillsMakeTheEnemyBesideTerrainDiscard);
        private const string Epicentre = nameof(MrakUiJourneyPlayModeTests.EpicentreRepeatsOnTheOtherEnemyHeroBesideTerrain);
        private const string SecondaryAttack = nameof(MrakUiJourneyPlayModeTests.EveryPrintedSecondaryAttackDefeatsTheAdjacentHeroFromItsOwnControl);
        private const string CarapaceDiscard = nameof(MrakUiJourneyPlayModeTests.CarapacesLetTheResolvedCardAnswerTheDiscardShockDemands);
        private const string CarapaceDefense = nameof(MrakUiJourneyPlayModeTests.CarapacesDefendFromWhereTheyRestAsIfInHand);
        private const string Retrieve = nameof(MrakUiJourneyPlayModeTests.RockSolidRetrievesTheCardMrakSpentDefending);
        private const string Grip = nameof(MrakUiJourneyPlayModeTests.StoneGripAsksAboutItsTieAndPlacesThreeRocksBesideTheTarget);
        private const string Fissure = nameof(MrakUiJourneyPlayModeTests.FissureTakesBackTwoOfThreeRocksAndLinesThemToTheBoardEdge);
        private const string Boulders = nameof(MrakUiJourneyPlayModeTests.BouldersPushTheAdjacentEnemyTwoSpacesAndFollowAlongThePush);
        private const string Stomps = nameof(MrakUiJourneyPlayModeTests.StompsMoveAUnitBesideTerrainAndLeaveARockWhereItStood);
        private const string Shifts = nameof(MrakUiJourneyPlayModeTests.ShiftsMayMoveAUnitInRangeOneSpaceToBesideTerrain);
        private const string Rolls = nameof(MrakUiJourneyPlayModeTests.RollsCrossObstaclesInAStraightLineToAChosenSpace);
        private const string HardPlace = nameof(MrakUiJourneyPlayModeTests.RockAndAHardPlaceMakesTheHeroBesideTheNewRockDiscard);
        private const string Movement = nameof(MrakUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesMrakThroughItsCardControl);
        private const string Defense = nameof(MrakUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(MrakUiJourneyPlayModeTests.APausedSecondaryAttackRestoresAtWaspsDefense);
        private const string Undo = nameof(MrakUiJourneyPlayModeTests.AShiftsDestinationPauseUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(MrakUiJourneyPlayModeTests.RocksOutlastMraksDefeatAndLeaveAtTheRoundEndBeforeARestart);
        private const string Spawn = nameof(MrakUiJourneyPlayModeTests.StagedMrakSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(MrakUiJourneyPlayModeTests.EveryMrakFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(MrakUiJourneyPlayModeTests.MrakLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private static ShapeEvidence[] Ribbon(bool movement = true, bool attack = false) =>
            (movement ? new[] { Prove(SecondaryMovementDestination, Movement) } : Array.Empty<ShapeEvidence>())
            .Concat(attack ? new[] { Prove(SecondaryAttackTarget, SecondaryAttack) } : Array.Empty<ShapeEvidence>())
            .Concat(new[] { Prove(SecondaryDefenseOffer, Defense) }).ToArray();

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] Seismic_(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
            Prove(BoardUnitSelection, journey), Prove(HandCardSelection, journey)
        };

        private static ShapeEvidence[] Carapace(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimaryMovementDestination, journey),
            Prove(BeforeMovementTextPause, journey), Prove(ActiveEffectReading, journey),
            Prove(RoundLongActiveEffectReading, CarapaceDefense),
            Prove(SourceCardDiscardSubstitution, CarapaceDiscard),
            Prove(SourceCardDefenseSubstitution, CarapaceDefense)
        };

        private static ShapeEvidence[] Boulder() => new[]
        {
            Prove(CommitAndReveal, Boulders), Prove(PrimarySkillControl, Boulders),
            Prove(ConfirmOrDecline, Boulders), Prove(BoardUnitSelection, Boulders),
            Prove(BoardSpaceSelection, Boulders), Prove(PushDistanceChoice, Boulders)
        };

        private static ShapeEvidence[] Shift() => new[]
        {
            Prove(CommitAndReveal, Shifts), Prove(PrimarySkillControl, Shifts),
            Prove(ConfirmOrDecline, Shifts), Prove(BoardUnitSelection, Shifts),
            Prove(BoardSpaceSelection, Shifts)
        };

        private static ShapeEvidence[] Roll() => new[]
        {
            Prove(CommitAndReveal, Rolls), Prove(PrimarySkillControl, Rolls),
            Prove(ConfirmOrDecline, Rolls), Prove(BoardSpaceSelection, Rolls)
        };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Mrak", MrakCardContent.AllDefinitions,
            MrakCardContent.ReturnedToBoxAtSetup,
            MrakComponentContent.RegisteredHandlers(),
            new[] { MrakCardContent.RockAndAHardPlaceTriggeredEffect }, new[]
            {
                Row(MrakCardContent.Fissure, "Fissure",
                    "MrakBasicFacesTests.Fissure_AShortPileAsksWhichRocksReturnWhenMoreStandThanItNeeds",
                    Ribbon(),
                    Prove(CommitAndReveal, Fissure), Prove(AdjacentAttackTarget, Fissure),
                    Prove(AfterAttackTextPause, Fissure), Prove(BoardSpaceSelection, Fissure)),
                Row(MrakCardContent.StoneGrip, "Stone Grip",
                    "MrakBasicFacesTests.StoneGrip_AnOffLineTargetFillsFromTheNextFarthestAndAsksAboutTheTie",
                    Ribbon(movement: false),
                    Prove(CommitAndReveal, Grip), Prove(PrimarySkillControl, Grip),
                    Prove(BoardUnitSelection, Grip), Prove(BoardSpaceSelection, Grip)),
                Row(MrakRedFaces.SeismicSlam, "Seismic Slam",
                    "MrakRedFacesTests.SeismicSlam_TargetsAnEnemyBesideARock_WhoDiscards",
                    Ribbon(attack: true), Seismic_(Seismic)),
                Row(MrakRedFaces.SeismicAssault, "Seismic Assault",
                    "MrakRedFacesTests.SeismicSlam_TargetsAnEnemyBesideTerrain",
                    Ribbon(attack: true), Seismic_(Seismic)),
                Row(MrakRedFaces.StoneCarapace, "Stone Carapace",
                    "MrakRedFacesTests.Carapace_MayBeDiscardedInsteadOfAHandCard",
                    Ribbon(movement: false, attack: true), Carapace(CarapaceDiscard)),
                Row(MrakRedFaces.Epicentre, "Epicentre",
                    "MrakRedFacesTests.Epicentre_RepeatsOnADifferentTarget",
                    Ribbon(attack: true),
                    // Milestone 9.1, ruling W1: the repeat is automatic, so no confirm-or-decline.
                    Seismic_(Epicentre).Concat(new[]
                    {
                        Prove(RepeatDecision, Epicentre)
                    }).ToArray()),
                Row(MrakRedFaces.RockSolid, "Rock Solid",
                    "MrakRedFacesTests.Carapace_DefendsWithItsOwnDefense_AndIsSpent",
                    Ribbon(movement: false, attack: true),
                    Carapace(Retrieve).Concat(new[]
                    {
                        Prove(ConfirmOrDecline, Retrieve), Prove(HandCardSelection, Retrieve)
                    }).ToArray()),
                Row(MrakBlueFaces.BoulderRush, "Boulder Rush",
                    "MrakBlueFacesTests.BoulderRush_PushesThroughARockToTheFirstReachableEmptySpace_ThenFollows",
                    Ribbon(), Boulder()),
                Row(MrakBlueFaces.BoulderBlitz, "Boulder Blitz",
                    "MrakBlueFacesTests.BoulderBlitz_AZeroSpacePushStillOffersTheFollowAlongItsHeading",
                    Ribbon(), Boulder()),
                Row(MrakBlueFaces.StompingStep, "Stomping Step",
                    "MrakBlueFacesTests.StompingStep_MovesAUnitBesideARockAndLeavesARockWhereItStood",
                    Ribbon(),
                    Prove(CommitAndReveal, Stomps), Prove(PrimarySkillControl, Stomps),
                    Prove(BoardUnitSelection, Stomps), Prove(BoardSpaceSelection, Stomps)),
                Row(MrakBlueFaces.Boulderdozer, "Boulderdozer",
                    "MrakBlueFacesTests.EachBoulderOffersItsPrintedPushDistances",
                    Ribbon(), Boulder()),
                Row(MrakBlueFaces.GroundShaker, "Ground Shaker",
                    "MrakBlueFacesTests.GroundShaker_RepeatsOnADifferentTarget",
                    Ribbon(),
                    Prove(CommitAndReveal, Stomps), Prove(PrimarySkillControl, Stomps),
                    Prove(ConfirmOrDecline, Stomps), Prove(RepeatDecision, Stomps),
                    Prove(BoardUnitSelection, Stomps), Prove(BoardSpaceSelection, Stomps)),
                Row(MrakGreenFaces.TreacherousGround, "Treacherous Ground",
                    "MrakGreenFacesTests.TreacherousGround_OffersOnlyDestinationsBesideARock",
                    Ribbon(), Shift()),
                Row(MrakGreenFaces.Rockslide, "Rockslide",
                    "MrakGreenFacesTests.TreacherousGround_ReachesItsPrintedRangeAndNoFurther",
                    Ribbon(), Shift()),
                Row(MrakGreenFaces.RollingStone, "Rolling Stone",
                    "MrakGreenFacesTests.RollingStone_CrossesObstaclesFreely_AndOneEmptySpaceAtMost",
                    Ribbon(), Roll()),
                Row(MrakGreenFaces.Avalanche, "Avalanche",
                    "MrakGreenFacesTests.Avalanche_MayRepeatOnTheSameUnit",
                    Ribbon(), Shift().Concat(new[] { Prove(RepeatDecision, Shifts) }).ToArray()),
                Row(MrakGreenFaces.StrollingStone, "Strolling Stone",
                    "MrakGreenFacesTests.StrollingStone_AllowsTwoEmptySpaces",
                    Ribbon(), Roll()),
                Face(MrakCardContent.RockAndAHardPlace, "Rock and a Hard Place", new[]
                {
                    Prove(UltimateRailVisibility, HardPlace),
                    Prove(UltimateTriggerPause, HardPlace),
                    Prove(HandCardSelection, HardPlace)
                }, new[] { "MrakUltimateTests.AStompPlacingARockBesideAnEnemyHero_MakesThatHeroDiscard" }),
                UnreachableFace(MrakCardContent.ReturnedToBoxAtSetup.Single(), "Lesser Fissure",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("mrak-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "MrakDealingTests" }),
                Component("mrak-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "MrakNetworkSeatTests.APausedSecondaryAttack_RestoresAndIsAnsweredOnlyByTheDefendersSeat" }),
                Component("mrak-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "MrakUltimateTests.TheSetIsTheNineteenRowTable_AndTheDealtEighteenSplitAsP6Deals" }),
                Component("mrak-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "MrakDealingTests" }),
                Component("mrak-rocks", "Rock tokens: placement, reclaim and round cleanup", new[]
                {
                    Prove(BoardSpaceSelection, Fissure),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "MrakRockTokenTests.EveryRockLeavesAtTheEndOfTheRound" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(MrakPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(MrakPlayerPathGateTests).Assembly);
        [Test] public void EveryMrakJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(MrakUiJourneyPlayModeTests));
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
