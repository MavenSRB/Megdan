using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;
using T = GuardsDigital.Core.Cards.Verified.EmmittCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 9.50: one content-derived, real-control claim per Emmitt front and component.</summary>
    public sealed class EmmittPlayerPathGateTests
    {
        private const string Attack = nameof(EmmittUiJourneyPlayModeTests.ReverseTimeAndTemporalPunchLineAttackFromTheirOwnFaces);
        private const string RibbonsMove = nameof(EmmittUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesEmmittThroughItsCardControl);
        private const string RibbonsDefend = nameof(EmmittUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersFromItsOwnFace);
        private const string GlitchSkill = nameof(EmmittUiJourneyPlayModeTests.UnstableTimelinePlacesSpacedGlitchesAndTheEnemyChoosesHisSwap);
        private const string GlitchArt = nameof(EmmittUiJourneyPlayModeTests.GlitchOnTheLiveBoardUsesItsPreparedOfficialFace);
        private const string GlitchDefense = nameof(EmmittUiJourneyPlayModeTests.UnstableTimelineDefensePlacesThreeGlitchesFromItsWholeFace);
        private const string GlitchAttack = nameof(EmmittUiJourneyPlayModeTests.FlashbackAndDejaVuPlaceGlitchesThenLetTheEnemyChooseTheirSwap);
        private const string Snare = nameof(EmmittUiJourneyPlayModeTests.TimeSnareAndTrapTargetOnlyAResolvedEnemyAndMakeHerDiscard);
        private const string Loop = nameof(EmmittUiJourneyPlayModeTests.TimeLoopSwapsWithTheAlreadyResolvedEnemy);
        private const string Bomb = nameof(EmmittUiJourneyPlayModeTests.TimeBombMakesTheResolvedEnemyDiscardOrFallWhenEmpty);
        private const string Warp = nameof(EmmittUiJourneyPlayModeTests.TimeWarpChoosesItsResolvedEnemySwapArmAndExchangesBothPieces);
        private const string TwoCards = nameof(EmmittUiJourneyPlayModeTests.AlternativeTimelinesCommitsTwoFacesAndRetrievesEitherAtReveal);
        private const string StraightMove = nameof(EmmittUiJourneyPlayModeTests.TimeWalkAndFastForwardMoveAStationaryEnemyExactlyTwoStraightSpaces);
        private const string Back = nameof(EmmittUiJourneyPlayModeTests.BackToTheFutureOffersItsModalMovementAndMovesTheChosenStationaryEnemy);
        private const string Proof = nameof(EmmittUiJourneyPlayModeTests.FutureProofChoosesTheVisibleThisTurnImmunityArm);
        private const string Capsule = nameof(EmmittUiJourneyPlayModeTests.TimeCapsuleLetsEmmittAndHisAllyRetrieveOnlyThisTurnsDiscards);
        private const string Spawn = nameof(EmmittUiJourneyPlayModeTests.StagedEmmittSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(EmmittUiJourneyPlayModeTests.EveryEmmittFaceKeepsTheStandardCardFootprint);
        private const string ShortViewport = nameof(EmmittUiJourneyPlayModeTests.EmmittsFiveCardHandStaysReachableAtTheShortDesktopViewport);
        private const string Restore = nameof(EmmittUiJourneyPlayModeTests.AGlitchPlacementPauseRestoresAndAnswersOnTheRebuiltBoard);
        private const string Undo = nameof(EmmittUiJourneyPlayModeTests.AGlitchPlacementUndoesToItsChoiceAndReplaysExactly);
        private const string Lifecycle = nameof(EmmittUiJourneyPlayModeTests.EmmittFallsRespawnsByBoardClickAndRestartRebuildsHisPiece);
        private const string Upgrade = nameof(EmmittUiJourneyPlayModeTests.EmmittsLevelUpAddsTheChosenCardAndShowsTheSiblingItem);

        private static readonly ShapeEvidence[] BothRibbons =
        {
            Prove(SecondaryMovementDestination, RibbonsMove),
            Prove(SecondaryDefenseOffer, RibbonsDefend)
        };

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] AttackBefore => new[]
        {
            Prove(CommitAndReveal, Attack), Prove(AdjacentAttackTarget, Attack),
            Prove(BeforeAttackTextPause, Attack), Prove(ActiveEffectReading, Attack)
        };

        private static ShapeEvidence[] ResolvedSkill(string journey, bool hasCardChoice) =>
            (hasCardChoice
                ? new[] { Prove(HandCardSelection, journey) }
                : Array.Empty<ShapeEvidence>())
            .Concat(new[]
            {
                Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
                Prove(BoardUnitSelection, journey)
            }).ToArray();

        private static ShapeEvidence[] GlitchRed => new[]
        {
            Prove(CommitAndReveal, GlitchAttack), Prove(AdjacentAttackTarget, GlitchAttack),
            Prove(AfterAttackTextPause, GlitchAttack), Prove(ConfirmOrDecline, GlitchAttack),
            Prove(BoardUnitSelection, GlitchAttack), Prove(BoardSpaceSelection, GlitchAttack)
        };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Emmitt", T.AllDefinitions, T.ReturnedToBoxAtSetup,
            EmmittActiveEffectContent.Handlers(), Array.Empty<EffectDefinition>(), new[]
            {
                Row(T.ReverseTime, "Reverse Time",
                    "EmmittAttackFacesTests.ReverseTime_AndTemporalPunchLine_PrintTheirVerifiedValuesAndTiming",
                    BothRibbons,
                    Prove(CommitAndReveal, Attack), Prove(AdjacentAttackTarget, Attack),
                    Prove(AfterAttackTextPause, Attack), Prove(ActiveEffectReading, Attack),
                    Prove(RoundLongActiveEffectReading, Attack)),
                Row(T.UnstableTimeline, "Unstable Timeline",
                    "EmmittSilverFaceTests.UnstableTimeline_IsBasicDefenseOrSkill_WithTwoOrThreeGlitchesAndEnemyChoice",
                    Array.Empty<ShapeEvidence>(),
                    Prove(CommitAndReveal, GlitchSkill), Prove(PrimarySkillControl, GlitchSkill),
                    Prove(PrimaryDefenseOffer, GlitchDefense), Prove(DefenseTextApplication, GlitchDefense),
                    Prove(BoardUnitSelection, GlitchSkill), Prove(BoardSpaceSelection, GlitchSkill),
                    Prove(BoardSpaceSelection, GlitchArt)),

                Row(T.TemporalPunch, "Temporal Punch",
                    "EmmittAttackFacesTests.ReverseTime_AndTemporalPunchLine_PrintTheirVerifiedValuesAndTiming",
                    BothRibbons, AttackBefore),
                Row(T.TemporalSlam, "Temporal Slam",
                    "EmmittAttackFacesTests.ReverseTime_AndTemporalPunchLine_PrintTheirVerifiedValuesAndTiming",
                    BothRibbons, AttackBefore),
                Row(T.Flashback, "Flashback",
                    "EmmittAttackFacesTests.FlashbackAndDejaVu_ValidateOptionalPlacementAndEnemyChosenSwap",
                    BothRibbons, GlitchRed),
                Row(T.TemporalJudgement, "Temporal Judgement",
                    "EmmittAttackFacesTests.ReverseTime_AndTemporalPunchLine_PrintTheirVerifiedValuesAndTiming",
                    BothRibbons, AttackBefore),
                Row(T.DejaVu, "Deja Vu",
                    "EmmittAttackFacesTests.FlashbackAndDejaVu_ValidateOptionalPlacementAndEnemyChosenSwap",
                    BothRibbons, GlitchRed),

                Row(T.TimeSnare, "Time Snare",
                    "EmmittBlueFacesTests.SnareAndTrap_PrintTheirExactFronts_AndValidateTheirResolvedHeroSelector",
                    BothRibbons, ResolvedSkill(Snare, true)),
                Row(T.TimeTrap, "Time Trap",
                    "EmmittBlueFacesTests.SnareAndTrap_PrintTheirExactFronts_AndValidateTheirResolvedHeroSelector",
                    BothRibbons, ResolvedSkill(Snare, true)),
                Row(T.TimeLoop, "Time Loop",
                    "EmmittDealingTests.EveryFrontIsComposed_AndEveryEffectUsesTheProductionCatalog",
                    BothRibbons, ResolvedSkill(Loop, false)),
                Row(T.TimeBomb, "Time Bomb",
                    "EmmittDealingTests.EveryFrontIsComposed_AndEveryEffectUsesTheProductionCatalog",
                    BothRibbons, ResolvedSkill(Bomb, true)),
                Row(T.TimeWarp, "Time Warp",
                    "EmmittTimeWarpOutcomeTests.EnemySeatChoosesItsPriorResolvedCard_AndTheExchangeSurvivesRestore",
                    BothRibbons, ResolvedSkill(Warp, true)
                        .Concat(new[] { Prove(ModalBulletChoice, Warp) }).ToArray()),

                Row(T.TimeWalk, "Time Walk",
                    "EmmittGreenFacesTests.WalkAndForward_PrintTheirValuesAndValidateExactTwoSpaceMove",
                    BothRibbons,
                    Prove(CommitAndReveal, StraightMove), Prove(PrimarySkillControl, StraightMove),
                    Prove(BoardUnitSelection, StraightMove), Prove(BoardSpaceSelection, StraightMove)),
                Row(T.FastForward, "Fast Forward",
                    "EmmittGreenFacesTests.WalkAndForward_PrintTheirValuesAndValidateExactTwoSpaceMove",
                    BothRibbons,
                    Prove(CommitAndReveal, StraightMove), Prove(PrimarySkillControl, StraightMove),
                    Prove(BoardUnitSelection, StraightMove), Prove(BoardSpaceSelection, StraightMove)),
                Row(T.TimeCapsule, "Time Capsule",
                    "EmmittGlitchFoundationTests.TimeCapsule_RetrievesOnlyTheOwnersCurrentTurnDiscards",
                    BothRibbons,
                    Prove(CommitAndReveal, Capsule), Prove(PrimarySkillControl, Capsule),
                    Prove(ConfirmOrDecline, Capsule), Prove(BoardUnitSelection, Capsule)),
                Row(T.BackToTheFuture, "Back to the Future",
                    "EmmittGreenFacesTests.BackToTheFuture_OffersHistoricalPlaceOrTheExactTimeWalkClause",
                    BothRibbons,
                    Prove(CommitAndReveal, Back), Prove(PrimarySkillControl, Back),
                    Prove(BoardUnitSelection, Back), Prove(BoardSpaceSelection, Back),
                    Prove(ModalBulletChoice, Back)),
                Row(T.FutureProof, "Future Proof",
                    "EmmittGreenFacesTests.FutureProof_ChoosesBetweenTurnDiscardsAndThisTurnImmunity",
                    BothRibbons,
                    Prove(CommitAndReveal, Proof), Prove(PrimarySkillControl, Proof),
                    Prove(ConfirmOrDecline, Proof), Prove(BoardUnitSelection, Proof),
                    Prove(ModalBulletChoice, Proof), Prove(ActiveEffectReading, Proof)),
                Face(T.AlternativeTimelines, "Alternative Timelines",
                    new[] { Prove(UltimateRailVisibility, TwoCards),
                        Prove(TwoCardCommitAndRevealRetrieval, TwoCards) },
                    new[] { "EmmittAttackFacesTests.AlternativeTimelines_RegistersTheSettledStandingRule" }),
                UnreachableFace(T.TearInTime, "Tear in Time",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),

                Component("emmitt-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn), Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, RibbonsMove),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle), Prove(PresentationRebuild, Lifecycle)
                }, new[] { "EmmittDealingTests.SelectedEmmitt_DealsExactlyEighteenFronts_AndBoxesTheHandicap" }),
                Component("emmitt-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo), Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "EmmittGlitchFoundationTests.GlitchAndDormantReverseTime_RoundTripThroughSnapshot" }),
                Component("emmitt-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint),
                        Prove(StandardCardFootprintConformance, ShortViewport) },
                    new[] { "EmmittDealingTests.EveryFrontIsComposed_AndEveryEffectUsesTheProductionCatalog" }),
                Component("emmitt-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "EmmittDealingTests.SelectedEmmitt_DealsExactlyEighteenFronts_AndBoxesTheHandicap" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(EmmittPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(EmmittPlayerPathGateTests).Assembly);
        [Test] public void EveryEmmittJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(EmmittUiJourneyPlayModeTests));
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
