using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 8.114g. Fixed, content-checked claims for each dealt Wuk face and component.</summary>
    public sealed class WukPlayerPathGateTests
    {
        private const string Saplings = nameof(WukUiJourneyPlayModeTests.MysticSaplingsPlantsTwoTreesBesideWukAndDeclinesTheThird);
        private const string TreeSlam = nameof(WukUiJourneyPlayModeTests.TreeSlamRemovesTheTreeBesideWukThenHitsWaspInRange);
        private const string Natures = nameof(WukUiJourneyPlayModeTests.NaturesAttacksHitTheHeroBesideWuk);
        private const string Tramples = nameof(WukUiJourneyPlayModeTests.TramplesWalkThroughWaspInALineWhoDiscardsAndDefeatTheMinionCrossed);
        private const string Blue = nameof(WukUiJourneyPlayModeTests.BlueFacesTakeATreeAndRetrieveACardForWukOrAFriend);
        private const string Dominance = nameof(WukUiJourneyPlayModeTests.DominanceTurnsATiedBattleIntoARedLossBeforeTheBattle);
        private const string Throws = nameof(WukUiJourneyPlayModeTests.ThrowsPlaceWaspIntoASpaceInRangeAndMonstrousThrowRepeats);
        private const string Swaps = nameof(WukUiJourneyPlayModeTests.SwapsTradeWukOrAFriendWithATree);
        private const string March = nameof(WukUiJourneyPlayModeTests.MarchOfNaturePlantsATreeAfterWukResolvesACard);
        private const string Movement = nameof(WukUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesWukThroughItsCardControl);
        private const string Defense = nameof(WukUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(WukUiJourneyPlayModeTests.APausedTrampleDiscardRestoresAtWaspsQuestion);
        private const string Undo = nameof(WukUiJourneyPlayModeTests.ASaplingsPlacementPauseUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(WukUiJourneyPlayModeTests.TreesOutlastWuksDefeatAndTheRoundEndBeforeARestart);
        private const string Spawn = nameof(WukUiJourneyPlayModeTests.StagedWukSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(WukUiJourneyPlayModeTests.EveryWukFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(WukUiJourneyPlayModeTests.WukLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private static ShapeEvidence[] Ribbon(bool movement = true) =>
            (movement ? new[] { Prove(SecondaryMovementDestination, Movement) } : Array.Empty<ShapeEvidence>())
            .Concat(new[] { Prove(SecondaryDefenseOffer, Defense) }).ToArray();

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] Nature() => new[]
        {
            Prove(CommitAndReveal, Natures), Prove(RangedAttackTarget, Natures),
            Prove(BeforeAttackTextPause, Natures), Prove(ModalBulletChoice, Natures)
        };

        private static ShapeEvidence[] Trampling() => new[]
        {
            Prove(CommitAndReveal, Tramples), Prove(PrimaryMovementDestination, Tramples),
            Prove(BeforeMovementTextPause, Tramples), Prove(AfterMovementTextPause, Tramples),
            Prove(ConfirmOrDecline, Tramples), Prove(BoardUnitSelection, Tramples),
            Prove(HandCardSelection, Tramples), Prove(ActiveEffectReading, Tramples)
        };

        private static ShapeEvidence[] Retrieval(bool modal) => new[]
        {
            Prove(CommitAndReveal, Blue), Prove(PrimarySkillControl, Blue),
            Prove(ConfirmOrDecline, Blue), Prove(BoardUnitSelection, Blue),
            Prove(HandCardSelection, Blue)
        }.Concat(modal ? new[] { Prove(ModalBulletChoice, Blue) } : Array.Empty<ShapeEvidence>()).ToArray();

        private static ShapeEvidence[] Dominate() => new[]
        {
            Prove(CommitAndReveal, Dominance), Prove(PrimarySkillControl, Dominance),
            Prove(BoardUnitSelection, Dominance), Prove(ActiveEffectReading, Dominance),
            Prove(RoundLongActiveEffectReading, Dominance)
        };

        private static ShapeEvidence[] Throw(bool repeat) => new[]
        {
            Prove(CommitAndReveal, Throws), Prove(PrimarySkillControl, Throws),
            Prove(BoardUnitSelection, Throws), Prove(BoardSpaceSelection, Throws)
        }.Concat(repeat
            ? new[] { Prove(ConfirmOrDecline, Throws), Prove(RepeatDecision, Throws) }
            : Array.Empty<ShapeEvidence>()).ToArray();

        private static ShapeEvidence[] Swap(bool twice) => new[]
        {
            Prove(CommitAndReveal, Swaps), Prove(PrimarySkillControl, Swaps),
            Prove(BoardUnitSelection, Swaps), Prove(BoardSpaceSelection, Swaps),
            Prove(ModalBulletChoice, Swaps)
        }.Concat(twice ? new[] { Prove(ConfirmOrDecline, Swaps) } : Array.Empty<ShapeEvidence>()).ToArray();

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Wuk", WukCardContent.AllDefinitions,
            WukCardContent.ReturnedToBoxAtSetup,
            WukComponentContent.RegisteredHandlers(),
            WukCardContent.TriggeredEffects(), new[]
            {
                Row(WukCardContent.TreeSlam, "Tree Slam",
                    "WukBasicFacesTests.TreeSlam_TheTreeBulletRemovesATreeBesideWukThenAttacksAUnitInRange",
                    Ribbon(),
                    Prove(CommitAndReveal, TreeSlam), Prove(RangedAttackTarget, TreeSlam),
                    Prove(BeforeAttackTextPause, TreeSlam), Prove(BoardUnitSelection, TreeSlam),
                    Prove(ModalBulletChoice, TreeSlam)),
                Row(WukCardContent.MysticSaplings, "Mystic Saplings",
                    "WukBasicFacesTests.MysticSaplings_UpToThree_StopsWhereWukDeclines",
                    Ribbon(movement: false),
                    Prove(CommitAndReveal, Saplings), Prove(PrimarySkillControl, Saplings),
                    Prove(ConfirmOrDecline, Saplings), Prove(BoardSpaceSelection, Saplings)),
                Row(WukRedFaces.NaturesProtector, "Nature's Protector",
                    "WukRedFacesTests.Protector_TheAdjacentBulletNamesHeroesAndNeverAMinion",
                    Ribbon(), Nature()),
                Row(WukRedFaces.NaturesGuardian, "Nature's Guardian",
                    "WukRedFacesTests.ProtectorReachesTwo_GuardianReachesItsPrintedThree",
                    Ribbon(), Nature()),
                Row(WukRedFaces.Trample, "Trample",
                    "WukTrampleTests.AStraightWalk_PassesThroughAnEnemyHero_WhoThenDiscards",
                    Ribbon(movement: false), Trampling()),
                Row(WukRedFaces.NaturesChampion, "Nature's Champion",
                    "WukRedFacesTests.Champion_TakesBothBulletsOnDifferentTargets",
                    Ribbon(), Nature()),
                Row(WukRedFaces.AngryStampede, "Angry Stampede",
                    "WukTrampleTests.AngryStampede_DefeatsUpToTwo",
                    Ribbon(movement: false), Trampling()),
                Row(WukBlueFaces.GiftsOfNature, "Gifts of Nature",
                    "WukBlueFacesTests.Gifts_RemovesATreeInItsPrintedRadiusThenWukMayRetrieve",
                    Ribbon(), Retrieval(modal: false)),
                Row(WukBlueFaces.TreeOfPlenty, "Tree of Plenty",
                    "WukBlueFacesTests.Plenty_AFriendlyHeroInRadiusRetrievesOnTheirOwnChoice",
                    Ribbon(), Retrieval(modal: true)),
                Row(WukBlueFaces.ClaimDominance, "Claim Dominance",
                    "WukDominanceTests.Claim_TheChosenMinionDoesNotCount_SoALostBattleIsATie",
                    Ribbon(), Dominate()),
                Row(WukBlueFaces.Abundance, "Abundance",
                    "WukBlueFacesTests.Abundance_TakesBothRetrievals",
                    Ribbon(), Retrieval(modal: true)),
                Row(WukBlueFaces.AssertDominance, "Assert Dominance",
                    "WukDominanceTests.Assert_ExcludesUpToTwo",
                    Ribbon(), Dominate()),
                Row(WukGreenFaces.TossAway, "Toss Away",
                    "WukGreenFacesTests.Toss_ThrowsAnAdjacentEnemyMinionIntoAnEmptySpaceInItsPrintedRange",
                    Ribbon(), Throw(repeat: false)),
                Row(WukGreenFaces.MightyThrow, "Mighty Throw",
                    "WukGreenFacesTests.Toss_ReachesThree_MightyThrowReachesItsPrintedFour",
                    Ribbon(), Throw(repeat: false)),
                Row(WukGreenFaces.IntoTheCanopy, "Into the Canopy",
                    "WukGreenFacesTests.Canopy_WukSwapsWithATreeInItsPrintedRadius",
                    Ribbon(), Swap(twice: false)),
                Row(WukGreenFaces.MonstrousThrow, "Monstrous Throw",
                    "WukGreenFacesTests.Monstrous_MayRepeatOnTheSameSubject",
                    Ribbon(), Throw(repeat: true)),
                Row(WukGreenFaces.TreetopRide, "Treetop Ride",
                    "WukGreenFacesTests.Treetop_TakesTheSameBulletTwice",
                    Ribbon(), Swap(twice: true)),
                Face(WukCardContent.MarchOfNature, "March of Nature", new[]
                {
                    Prove(UltimateRailVisibility, March),
                    Prove(UltimateTriggerPause, March),
                    Prove(ConfirmOrDecline, March),
                    Prove(BoardSpaceSelection, March)
                }, new[] { "WukUltimateAndSetTests.MarchOfNature_AfterWukResolvesAColouredCard_MayPlaceATreeInItsPrintedRadius" }),
                UnreachableFace(WukCardContent.ReturnedToBoxAtSetup.Single(), "Makeshift Weapon",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("wuk-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "WukDealingTests" }),
                Component("wuk-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "WukNetworkSeatTests.ATrampleDiscard_RestoresAndIsAnsweredOnlyByTheTrampledSeat" }),
                Component("wuk-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "WukUltimateAndSetTests.TheSetIsTheShardsNineteenRows_AllComposed" }),
                Component("wuk-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "WukDealingTests" }),
                Component("wuk-trees", "Tree tokens: placement, retention through round end", new[]
                {
                    Prove(BoardSpaceSelection, Saplings),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "WukBasicFacesTests.Tree_IsAThreeTokenSupplyThatNoRoundEndRemoves" })
            },
            // Dominance asks its question from the Active effect's own pre-battle trigger (8.114g).
            walksActiveEffectTriggers: true);

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(WukPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(WukPlayerPathGateTests).Assembly);
        [Test] public void EveryWukJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(WukUiJourneyPlayModeTests));
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
