using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 8.93g. Fixed, content-checked claims for each dealt Snorri face and component.</summary>
    public sealed class SnorriPlayerPathGateTests
    {
        private const string Tray = nameof(SnorriRuneTrayPlayModeTests.InscribedRunes_StandInTheirTraySlotsAndAboveTheLiveTurnsCard);
        private const string Sigils = nameof(SnorriUiJourneyPlayModeTests.RuneSigilsRepeatsOnTheOtherEnemyHeroUnderRuneFour);
        private const string Retrieve = nameof(SnorriUiJourneyPlayModeTests.EveryRetrievingRedFaceReturnsTheSpentDefenseCardUnderRuneThree);
        private const string MoveBefore = nameof(SnorriUiJourneyPlayModeTests.HammerAndBattleaxeMoveOneBeforeTheirAttackUnderRuneFour);
        private const string AxeRepeat = nameof(SnorriUiJourneyPlayModeTests.BattleaxeRepeatsItsAttackOnAnEnemyMinionUnderRuneTwo);
        private const string CasterDiscard = nameof(SnorriUiJourneyPlayModeTests.CasterAndBlasterMakeAHeroBesideTheirTargetDiscardUnderRuneTwo);
        private const string CasterMove = nameof(SnorriUiJourneyPlayModeTests.CasterAndBlasterMayMoveUpToTwoAfterTheAttackUnderRuneFour);
        private const string Passage = nameof(SnorriUiJourneyPlayModeTests.ThreePassagesCreateTheirRuneEffectBeforeMoving);
        private const string Ancestral = nameof(SnorriUiJourneyPlayModeTests.AncestralSkillsLetTheFriendSwapAResolvedCardUnderRuneTwo);
        private const string Oath = nameof(SnorriUiJourneyPlayModeTests.OathsBlockABasicAttackUnderRuneFourAndLeaveSnorriImmune);
        private const string Runestone = nameof(SnorriUiJourneyPlayModeTests.RunestoneMakesTheEnemyDiscardTheActiveRunesColour);
        private const string Runebomb = nameof(SnorriUiJourneyPlayModeTests.RuneMasteryAddsAnActiveRuneThatRunebombThenChooses);
        private const string Movement = nameof(SnorriUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesSnorriThroughItsCardControl);
        private const string Defense = nameof(SnorriUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Continuity = nameof(SnorriUiJourneyPlayModeTests.RuneSlotPauseRestoresAndRetainedPrefixUndoReplaysExactly);
        private const string Lifecycle = nameof(SnorriUiJourneyPlayModeTests.RunesStayInTheTrayThroughSnorrisDefeatTheRoundBoundaryAndARestart);
        private const string Spawn = nameof(SnorriUiJourneyPlayModeTests.StagedSnorriSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(SnorriUiJourneyPlayModeTests.EverySnorriFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(SnorriUiJourneyPlayModeTests.SnorriLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private static ShapeEvidence[] Ribbon(bool movement = true, bool defense = true) =>
            (movement ? new[] { Prove(SecondaryMovementDestination, Movement) }
                : Array.Empty<ShapeEvidence>())
            .Concat(defense ? new[] { Prove(SecondaryDefenseOffer, Defense) }
                : Array.Empty<ShapeEvidence>()).ToArray();

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Snorri", SnorriCardContent.AllDefinitions,
            SnorriCardContent.ReturnedToBoxAtSetup,
            SnorriComponentContent.Handlers()
                .Concat(SnorriBlueActiveEffectContent.Handlers())
                .Concat(SnorriGreenBlockContent.Handlers())
                .Concat(SnorriUltimateContent.Handlers()),
            new[] { SnorriUltimateContent.TriggeredEffect }, new[]
            {
                Row(SnorriAttackFaces.RuneSigils, "Rune Sigils",
                    "SnorriAttackTurnTests.RuneSigilsFourthRuneRepeatsOnADifferentHeroInPrintedRange",
                    Ribbon(),
                    Prove(CommitAndReveal, Sigils), Prove(RangedAttackTarget, Sigils),
                    Prove(BeforeAttackTextPause, Sigils), Prove(AfterAttackTextPause, Sigils),
                    Prove(RepeatDecision, Sigils), Prove(BoardUnitSelection, Sigils)),
                Row(SnorriSilverFace.InscribeTheRunes, "Inscribe the Runes",
                    "SnorriRuneAssignmentTests.SilverSkillWaitsForFourDraggedPairsBeforeCommittingMarkers",
                    Ribbon(movement: false),
                    Prove(CommitAndReveal, Tray), Prove(PrimarySkillControl, Tray),
                    Prove(RuneSlotAssignment, Tray)),
                Row(SnorriAttackFaces.RunicDagger, "Runic Dagger",
                    "SnorriAttackTurnTests.ThirdRuneRetrievesAnOwnedDiscardAfterEachApplicableRedAttack",
                    Ribbon(),
                    Prove(CommitAndReveal, Retrieve), Prove(AdjacentAttackTarget, Retrieve),
                    Prove(AfterAttackTextPause, Retrieve), Prove(ConfirmOrDecline, Retrieve),
                    Prove(HandCardSelection, Retrieve)),
                Row(SnorriAttackFaces.RunicHammer, "Runic Hammer",
                    "SnorriAttackTurnTests.FourthRuneMayMoveOneBeforeHammerOrBattleaxeAttack",
                    Ribbon(),
                    Prove(CommitAndReveal, MoveBefore), Prove(AdjacentAttackTarget, MoveBefore),
                    Prove(BeforeAttackTextPause, MoveBefore), Prove(ConfirmOrDecline, MoveBefore),
                    Prove(BoardSpaceSelection, MoveBefore), Prove(BoardUnitSelection, MoveBefore),
                    Prove(AfterAttackTextPause, Retrieve), Prove(HandCardSelection, Retrieve)),
                Row(SnorriAttackFaces.Runecaster, "Runecaster",
                    "SnorriAttackTurnTests.RunecasterSecondRuneUsesTheSameAdjacentSpaceClauseAtMaximumRange",
                    Ribbon(),
                    Prove(CommitAndReveal, CasterDiscard), Prove(RangedAttackTarget, CasterDiscard),
                    Prove(AfterAttackTextPause, CasterDiscard), Prove(BoardUnitSelection, CasterDiscard),
                    Prove(HandCardSelection, CasterDiscard), Prove(BoardSpaceSelection, CasterMove)),
                Row(SnorriAttackFaces.RunicBattleaxe, "Runic Battleaxe",
                    "SnorriAttackTurnTests.BattleaxeSecondRuneRepeatsItsAttackOnAnEnemyMinion",
                    Ribbon(),
                    Prove(CommitAndReveal, MoveBefore), Prove(AdjacentAttackTarget, MoveBefore),
                    Prove(BeforeAttackTextPause, MoveBefore), Prove(ConfirmOrDecline, MoveBefore),
                    Prove(BoardSpaceSelection, MoveBefore), Prove(AfterAttackTextPause, AxeRepeat),
                    Prove(RepeatDecision, AxeRepeat), Prove(BoardUnitSelection, AxeRepeat),
                    Prove(HandCardSelection, Retrieve)),
                Row(SnorriAttackFaces.Runeblaster, "Runeblaster",
                    "SnorriAttackTurnTests.RuneblasterSecondRuneDiscardsFromAHeroAdjacentToTheTargetSpace",
                    Ribbon(),
                    Prove(CommitAndReveal, CasterDiscard), Prove(RangedAttackTarget, CasterDiscard),
                    Prove(BeforeAttackTextPause, CasterDiscard), Prove(AfterAttackTextPause, CasterDiscard),
                    Prove(BoardUnitSelection, CasterDiscard), Prove(HandCardSelection, CasterDiscard),
                    Prove(BoardSpaceSelection, CasterMove)),
                Row(SnorriBlueMovementFaces.SafePassage, "Safe Passage",
                    "SnorriBlueMovementTurnTests", Ribbon(movement: false),
                    Prove(CommitAndReveal, Passage), Prove(PrimaryMovementDestination, Passage),
                    Prove(BeforeMovementTextPause, Passage), Prove(ActiveEffectReading, Passage)),
                Row(SnorriBlueMovementFaces.HiddenPassage, "Hidden Passage",
                    "SnorriBlueMovementTurnTests", Ribbon(movement: false),
                    Prove(CommitAndReveal, Passage), Prove(PrimaryMovementDestination, Passage),
                    Prove(BeforeMovementTextPause, Passage), Prove(ActiveEffectReading, Passage)),
                Row(SnorriBlueMovementFaces.DeepPassage, "Deep Passage",
                    "SnorriBlueMovementTurnTests", Ribbon(movement: false),
                    Prove(CommitAndReveal, Passage), Prove(PrimaryMovementDestination, Passage),
                    Prove(BeforeMovementTextPause, Passage), Prove(ActiveEffectReading, Passage)),
                Row(SnorriBlueSupportFaces.AncestralBoon.Id, "Ancestral Boon",
                    "SnorriBlueResolvedSwapTests", Ribbon(),
                    Prove(CommitAndReveal, Ancestral), Prove(PrimarySkillControl, Ancestral),
                    Prove(ConfirmOrDecline, Ancestral), Prove(BoardUnitSelection, Ancestral),
                    Prove(HandCardSelection, Ancestral)),
                Row(SnorriBlueSupportFaces.AncestralGrace.Id, "Ancestral Grace",
                    "SnorriBlueItemSwapTests", Ribbon(),
                    Prove(CommitAndReveal, Ancestral), Prove(PrimarySkillControl, Ancestral),
                    Prove(ConfirmOrDecline, Ancestral), Prove(BoardUnitSelection, Ancestral),
                    Prove(HandCardSelection, Ancestral)),
                Row(SnorriGreenFaces.OathOfEndurance, "Oath of Endurance",
                    "SnorriGreenBlockTests", Ribbon(defense: false),
                    Prove(CommitAndReveal, Movement), Prove(BlockDefenseOffer, Oath),
                    Prove(DefenseTextApplication, Oath), Prove(ActiveEffectReading, Oath)),
                Row(SnorriGreenFaces.OathOfFortitude, "Oath of Fortitude",
                    "SnorriGreenBlockTests", Ribbon(defense: false),
                    Prove(CommitAndReveal, Movement), Prove(BlockDefenseOffer, Oath),
                    Prove(DefenseTextApplication, Oath), Prove(ActiveEffectReading, Oath)),
                Row(SnorriGreenFaces.ChargedRunestone, "Charged Runestone",
                    "SnorriGreenBlockTests", Ribbon(),
                    Prove(CommitAndReveal, Runestone), Prove(PrimarySkillControl, Runestone),
                    Prove(BoardUnitSelection, Runestone), Prove(HandCardSelection, Runestone)),
                Row(SnorriGreenFaces.OathOfPerseverance, "Oath of Perseverance",
                    "SnorriGreenBlockTests.NonBasicRuneAlsoBlocksAnUltimateAttack", Ribbon(defense: false),
                    Prove(CommitAndReveal, Movement), Prove(BlockDefenseOffer, Oath),
                    Prove(DefenseTextApplication, Oath), Prove(ActiveEffectReading, Oath)),
                Row(SnorriGreenFaces.InscribedRunebomb, "Inscribed Runebomb",
                    "SnorriGreenBlockTests", Ribbon(),
                    Prove(CommitAndReveal, Runebomb), Prove(PrimarySkillControl, Runebomb),
                    Prove(BoardUnitSelection, Runebomb), Prove(HandCardSelection, Runebomb),
                    Prove(RuneGlyphChoice, Runebomb)),
                Face(SnorriUltimateContent.RuneMastery, "Rune Mastery", new[]
                {
                    Prove(UltimateRailVisibility, Runebomb),
                    Prove(UltimateTriggerPause, Runebomb),
                    Prove(RuneGlyphChoice, Runebomb)
                }, new[] { "SnorriRuneAssignmentTests.RuneMasteryOffersOnlyInactiveRunes_AndExpiresThePriorChoiceBeforeNextAction" }),
                UnreachableFace(SnorriCardContent.ReturnedToBoxAtSetup.Single(), "Rune Shards",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("snorri-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "SnorriDealingTests" }),
                Component("snorri-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Continuity),
                    Prove(RetainedPrefixUndo, Continuity),
                    Prove(ReplayReconstruction, Continuity),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "SnorriRuneAssignmentTests.SnapshotRoundTripPreservesFourRuneSlotsAndLiveTurnSelection" }),
                Component("snorri-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "SnorriRuneAssignmentTests.ReconciledInventoryContainsNineteenDistinctFronts_AndBoxesOnlyRuneShards" }),
                Component("snorri-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "SnorriDealingTests" }),
                Component("snorri-runes", "Four turn-slot rune markers", new[]
                {
                    Prove(RuneSlotAssignment, Tray),
                    Prove(PresentationRebuild, Tray)
                }, new[] { "SnorriRuneAssignmentTests.ReinscribeMovesTheSameFourPiecesAcrossRoundAndTurn" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(SnorriPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(SnorriPlayerPathGateTests).Assembly);
        [Test] public void EverySnorriJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(SnorriUiJourneyPlayModeTests));
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
