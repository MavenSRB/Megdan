using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;
using T = GuardsDigital.Core.Cards.Verified.NebKherCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 9.54: source-backed real-control coverage of all NebKher fronts.</summary>
    public sealed class NebKherPlayerPathGateTests
    {
        private const string Move = nameof(NebKherUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesNebKherThroughItsCardControl);
        private const string Defend = nameof(NebKherUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersFromItsOwnFace);
        private const string Blue = nameof(NebKherUiJourneyPlayModeTests.EveryDelayedBlueFaceNamesAColorOnItsRealWindow);
        private const string BlueReveal = nameof(NebKherUiJourneyPlayModeTests.DelayedBlueNextRevealAsksForEnemyAndWholeCardThroughRealControls);
        private const string Split = nameof(NebKherUiJourneyPlayModeTests.BothRealitySplitsChooseTheirAxisOnTheRealWindow);
        private const string Laugh = nameof(NebKherUiJourneyPlayModeTests.DiabolicalLaughterLaughsThenPlacesAnIllusionThroughItsPrintedModes);
        private const string LaughSwap = nameof(NebKherUiJourneyPlayModeTests.DiabolicalLaughterSwapsTwoResolvedEnemyCardsThroughTheirFaces);
        private const string Ultimate = nameof(NebKherUiJourneyPlayModeTests.UnlockedUltimateInterruptsOneChosenLaughAndEnemyDiscardsAWholeFace);
        private const string Sentry = nameof(NebKherUiJourneyPlayModeTests.PhantasmalSentryChoosesItsAdjacentAttackArmThenTargetsOnTheBoard);
        private const string Warrior = nameof(NebKherUiJourneyPlayModeTests.PhantasmalWarriorAndChampionChooseTheirAdjacentAttackArmOnTheBoard);
        private const string WarriorImage = nameof(NebKherUiJourneyPlayModeTests.WarriorAndChampionMoveAnIllusionThroughTheirPrintedAttackArm);
        private const string Fate = nameof(NebKherUiJourneyPlayModeTests.TwistFateAndDeviousSchemeTargetADistantUnitThroughTheirOwnAttack);
        private const string FateSwap = nameof(NebKherUiJourneyPlayModeTests.TwistFateAndDeviousSchemeTakeTheirOptionalIllusionSwapAfterAttack);
        private const string GripMinion = nameof(NebKherUiJourneyPlayModeTests.MindGripChoosesThePrintedAdjacentMinionDefeatOnTheBoard);
        private const string GripBorrow = nameof(NebKherUiJourneyPlayModeTests.MindGripBorrowsTheEnemysPreviousSecondaryMovementThroughItsRealControls);
        private const string Images = nameof(NebKherUiJourneyPlayModeTests.EveryUpToImageAndArmyFacePlacesItsFirstIllusionThroughRealControls);
        private const string Overlay = nameof(NebKherUiJourneyPlayModeTests.ForceAndArmyFinishTheirUpToPlacementAndExposeTheActionOnlyOverlay);
        private const string Fleeting = nameof(NebKherUiJourneyPlayModeTests.FleetingImagePlacesAnOfficialIllusionThroughTheBoardChoice);
        private const string Spawn = nameof(NebKherUiJourneyPlayModeTests.StagedNebKherSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(NebKherUiJourneyPlayModeTests.EveryNebKherFaceKeepsTheStandardCardFootprint);
        private const string ShortViewport = nameof(NebKherUiJourneyPlayModeTests.NebKhersFiveCardHandStaysReachableAtTheShortDesktopViewport);
        private const string Restore = nameof(NebKherUiJourneyPlayModeTests.IllusionPlacementPauseRestoresAndAnswersOnTheRebuiltBoard);
        private const string Undo = nameof(NebKherUiJourneyPlayModeTests.IllusionPlacementUndoesToItsChoiceAndReplaysExactly);
        private const string Lifecycle = nameof(NebKherUiJourneyPlayModeTests.NebKherFallsRespawnsByBoardClickAndRestartRebuildsHisPiece);
        private const string Upgrade = nameof(NebKherUiJourneyPlayModeTests.NebKhersLevelUpAddsTheChosenCardAndShowsTheSiblingItem);

        private static readonly ShapeEvidence[] BothSecondaries =
        {
            Prove(SecondaryMovementDestination, Move),
            Prove(SecondaryDefenseOffer, Defend)
        };

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            string journey, ShapeEvidence[] secondaries, params ShapeEvidence[] own) =>
            Face(id, title, new[] { Prove(CommitAndReveal, journey) }
                .Concat(own).Concat(secondaries).ToArray(), new[] { scenario });

        private static ShapeEvidence[] Skill(string journey, params ShapeEvidence[] extra) =>
            new[] { Prove(PrimarySkillControl, journey) }.Concat(extra).ToArray();

        private static ShapeEvidence[] Attack(string journey, params ShapeEvidence[] extra) =>
            new[] { Prove(RangedAttackTarget, journey) }.Concat(extra).ToArray();

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "NebKher", T.AllDefinitions, T.ReturnedToBoxAtSetup,
            NebKherComponentContent.Handlers()
                .Concat(NebKherDelayedDoubtContent.Handlers())
                .Concat(NebKherUltimateContent.Handlers()),
            new[] { NebKherUltimateContent.TriggeredEffect }, new[]
            {
                Row(T.MindGrip, "Mind Grip",
                    "NebKherComposedBasicAndSplitTests.MindGrip_OffersExactlyTheBorrowAndAdjacentDefeatArms",
                    GripBorrow, BothSecondaries,
                    Skill(GripBorrow, Prove(ModalBulletChoice, GripMinion),
                        Prove(BoardUnitSelection, GripMinion),
                        Prove(PreviousSlotPrintedActionChoice, GripBorrow))),
                Row(T.DiabolicalLaughter, "Diabolical Laughter",
                    "NebKherComposedBasicAndSplitTests.DiabolicalLaughter_OffersRealLaughThenThreeOptionalThreeWayChoices",
                    Laugh, new[] { Prove(SecondaryDefenseOffer, Defend) },
                    Skill(Laugh, Prove(ConfirmOrDecline, Laugh),
                        Prove(ModalBulletChoice, Laugh), Prove(BoardSpaceSelection, Laugh),
                        Prove(BoardUnitSelection, LaughSwap), Prove(HandCardSelection, LaughSwap))),

                Row(T.PhantasmalSentry, "Phantasmal Sentry",
                    "NebKherComposedBasicAndSplitTests.Sentry_BindsEitherARangedHeroBesideAnInRangeIllusion_OrAnAdjacentUnit",
                    Sentry, BothSecondaries,
                    Attack(Sentry, Prove(BeforeAttackTextPause, Sentry),
                        Prove(ModalBulletChoice, Sentry), Prove(BoardUnitSelection, Sentry))),
                Row(T.PhantasmalWarrior, "Phantasmal Warrior",
                    "NebKherComposedBasicAndSplitTests.WarriorAndChampion_MoveAnInRangeIllusionAtMostTheirPrintedDistance",
                    Warrior, BothSecondaries,
                    Attack(Warrior, Prove(BeforeAttackTextPause, Warrior),
                        Prove(ModalBulletChoice, WarriorImage), Prove(BoardUnitSelection, WarriorImage),
                        Prove(BoardSpaceSelection, WarriorImage))),
                Row(T.TwistFate, "Twist Fate",
                    "NebKherComposedBasicAndSplitTests.FateAndScheme_DistinguishAdjacentFromInRangeIllusionSwapsAfterAttack",
                    Fate, BothSecondaries,
                    Attack(Fate, Prove(AfterAttackTextPause, FateSwap),
                        Prove(ConfirmOrDecline, FateSwap), Prove(BoardUnitSelection, FateSwap))),
                Row(T.PhantasmalChampion, "Phantasmal Champion",
                    "NebKherComposedBasicAndSplitTests.WarriorAndChampion_MoveAnInRangeIllusionAtMostTheirPrintedDistance",
                    Warrior, BothSecondaries,
                    Attack(Warrior, Prove(BeforeAttackTextPause, Warrior),
                        Prove(ModalBulletChoice, WarriorImage), Prove(BoardUnitSelection, WarriorImage),
                        Prove(BoardSpaceSelection, WarriorImage))),
                Row(T.DeviousScheme, "Devious Scheme",
                    "NebKherComposedBasicAndSplitTests.FateAndScheme_DistinguishAdjacentFromInRangeIllusionSwapsAfterAttack",
                    Fate, BothSecondaries,
                    Attack(Fate, Prove(AfterAttackTextPause, FateSwap),
                        Prove(ConfirmOrDecline, FateSwap), Prove(BoardUnitSelection, FateSwap))),

                Row(T.ImbueDoubt, "Imbue Doubt",
                    "NebKherComposedBasicAndSplitTests.DelayedBlueLine_NamesSixColorsAndCarriesChoiceToNextReveal",
                    Blue, BothSecondaries,
                    Skill(Blue, Prove(ModalBulletChoice, Blue),
                        Prove(BoardUnitSelection, BlueReveal), Prove(HandCardSelection, BlueReveal),
                        Prove(ActiveEffectReading, BlueReveal), Prove(RoundLongActiveEffectReading, BlueReveal))),
                Row(T.TimeToReconsider, "Time to Reconsider",
                    "NebKherComposedBasicAndSplitTests.DelayedBlueLine_NamesSixColorsAndCarriesChoiceToNextReveal",
                    Blue, BothSecondaries,
                    Skill(Blue, Prove(ModalBulletChoice, Blue),
                        Prove(BoardUnitSelection, BlueReveal), Prove(HandCardSelection, BlueReveal),
                        Prove(ActiveEffectReading, BlueReveal), Prove(RoundLongActiveEffectReading, BlueReveal))),
                Row(T.CrackInReality, "Crack in Reality",
                    "NebKherComposedBasicAndSplitTests.CrackAndShift_PrintDistinctTypedBoardPartitions",
                    Split, BothSecondaries,
                    Skill(Split, Prove(DirectionChoice, Split), Prove(ActiveEffectReading, Split))),
                Row(T.AnIllusionOfChoice, "An Illusion of Choice",
                    "NebKherDealingTests.AnIllusionOfChoice_AffectsUpToTwoPresentEnemiesWithoutADecline",
                    Blue, BothSecondaries,
                    Skill(Blue, Prove(ModalBulletChoice, Blue),
                        Prove(BoardUnitSelection, BlueReveal), Prove(HandCardSelection, BlueReveal),
                        Prove(ActiveEffectReading, BlueReveal), Prove(RoundLongActiveEffectReading, BlueReveal))),
                Row(T.ShiftReality, "Shift Reality",
                    "NebKherComposedBasicAndSplitTests.CrackAndShift_PrintDistinctTypedBoardPartitions",
                    Split, BothSecondaries,
                    Skill(Split, Prove(DirectionChoice, Split), Prove(ActiveEffectReading, Split))),

                Row(T.FleetingImage, "Fleeting Image",
                    "NebKherComposedBasicAndSplitTests.ImageLine_PlacesItsPrintedCount_ThenOffersARealOptionalIllusionSwap",
                    Fleeting, BothSecondaries,
                    Skill(Fleeting, Prove(BoardSpaceSelection, Fleeting),
                        Prove(BoardUnitSelection, Fleeting),
                        Prove(ConfirmOrDecline, Fleeting))),
                Row(T.MultipleProjections, "Multiple Projections",
                    "NebKherComposedBasicAndSplitTests.ImageLine_PlacesItsPrintedCount_ThenOffersARealOptionalIllusionSwap",
                    Images, BothSecondaries,
                    Skill(Images, Prove(BoardSpaceSelection, Images),
                        Prove(BoardUnitSelection, Fleeting),
                        Prove(ConfirmOrDecline, Images))),
                Row(T.IllusionaryForce, "Illusionary Force",
                    "NebKherComposedBasicAndSplitTests.ForceAndArmy_PlaceUpToThePrintedSupply_ThenGrantTheActionOnlyOverlay",
                    Images, BothSecondaries,
                    Skill(Images, Prove(BoardSpaceSelection, Images),
                        Prove(ConfirmOrDecline, Overlay), Prove(ActiveEffectReading, Overlay),
                        Prove(RoundLongActiveEffectReading, Overlay))),
                Row(T.MasterOfIllusions, "Master of Illusions",
                    "NebKherComposedBasicAndSplitTests.ImageLine_PlacesItsPrintedCount_ThenOffersARealOptionalIllusionSwap",
                    Images, BothSecondaries,
                    Skill(Images, Prove(BoardSpaceSelection, Images),
                        Prove(BoardUnitSelection, Fleeting),
                        Prove(ConfirmOrDecline, Images))),
                Row(T.IllusionaryArmy, "Illusionary Army",
                    "NebKherComposedBasicAndSplitTests.ForceAndArmy_PlaceUpToThePrintedSupply_ThenGrantTheActionOnlyOverlay",
                    Images, BothSecondaries,
                    Skill(Images, Prove(BoardSpaceSelection, Images),
                        Prove(ConfirmOrDecline, Overlay), Prove(ActiveEffectReading, Overlay),
                        Prove(RoundLongActiveEffectReading, Overlay))),

                Face(T.WhatTheHellAreYou, "What the Hell Are You?",
                    new[] { Prove(UltimateRailVisibility, Ultimate),
                        Prove(UltimateTriggerPause, Ultimate),
                        Prove(HandCardSelection, Ultimate) },
                    new[] { "NebKherRevealJourneyTests.PrintedLaugh_InterruptsImmediatelyWithUnlockedUltimateDiscard" }),
                UnreachableFace(T.MindProbe, "Mind Probe",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),

                Component("nebkher-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn), Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Move), Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "NebKherDealingTests.SelectedNebKher_DealsExactlyEighteenFronts_AndBoxesMindProbe" }),
                Component("nebkher-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore), Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo), Prove(RestartReconstruction, Lifecycle)
                }, new[] { "NebKherIllusionFoundationTests.ThreeIllusions_ShareOneFiniteSupplyAndOrdinaryRoundCleanup" }),
                Component("nebkher-footprint", "Every complete face", new[]
                {
                    Prove(StandardCardFootprintConformance, Footprint),
                    Prove(StandardCardFootprintConformance, ShortViewport)
                }, new[] { "NebKherDealingTests.EveryFrontAndTriggeredBodyIsLoadedFromTheProductionCatalog" }),
                Component("nebkher-illusion", "Three official Illusion pieces", new[]
                {
                    Prove(BoardSpaceSelection, Fleeting), Prove(ConfirmOrDecline, Fleeting)
                }, new[] { "NebKherIllusionFoundationTests.ThreeIllusions_ShareOneFiniteSupplyAndOrdinaryRoundCleanup" }),
                Component("nebkher-items", "Level-up sibling items", new[]
                {
                    Prove(ItemVisibilityAndUse, Upgrade)
                }, new[] { "NebKherDealingTests.SelectedNebKher_DealsExactlyEighteenFronts_AndBoxesMindProbe" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(NebKherPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(NebKherPlayerPathGateTests).Assembly);
        [Test] public void EveryNebKherJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(NebKherUiJourneyPlayModeTests));
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
