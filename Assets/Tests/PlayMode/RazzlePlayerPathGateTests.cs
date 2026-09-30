using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.21k. Fixed, content-checked claims for each dealt Razzle face and component, derived
    /// offline before any Unity run and proved by the real-input journeys in
    /// <see cref="RazzleUiJourneyPlayModeTests"/>. His two repeats introduce the one shape no earlier
    /// hero prints - a repeat performed by a different body, answered by clicking its standee - and the
    /// pick of the acting body is his component's own shape.
    /// </summary>
    public sealed class RazzlePlayerPathGateTests
    {
        private const string Spawn = nameof(RazzleUiJourneyPlayModeTests.StuntDoublesSpawnsTwoBodiesBesideHimAndDeclinesTheThird);
        private const string Removal = nameof(RazzleUiJourneyPlayModeTests.RemovalFacesTakeBodiesOffTheBoardWithoutADefeat);
        private const string CrowdSkill = nameof(RazzleUiJourneyPlayModeTests.CrowdControlAsASkillRemovesEveryOtherBodyFromThePickedOne);
        private const string CrowdDefense = nameof(RazzleUiJourneyPlayModeTests.CrowdControlDiscardedToDefendAddsTwoForEachOtherBodyInRadius);
        private const string Retrieve = nameof(RazzleUiJourneyPlayModeTests.RummageAndRansackRetrieveACardForEachOtherBodyInRadius);
        private const string Alleyoop = nameof(RazzleUiJourneyPlayModeTests.AlleyoopFacesSwapWithArienAndMoveAnotherBody);
        private const string MagicTrick = nameof(RazzleUiJourneyPlayModeTests.MagicTrickFacesPushWaspAndRetreatThisBodyTheSameDistance);
        private const string Tightrope = nameof(RazzleUiJourneyPlayModeTests.TightropeFacesMoveAnotherBodyAfterTheWalk);
        private const string Theatrics = nameof(RazzleUiJourneyPlayModeTests.TheatricsSwapsThePickedBodyWithAMinionInItsRange);
        private const string Spectacle = nameof(RazzleUiJourneyPlayModeTests.SpectacleRepeatsItsSwapFromTheOtherBodyWithADifferentMinion);
        private const string TwinStrike = nameof(RazzleUiJourneyPlayModeTests.TwinStrikeRepeatsTheBasicAttackFromAnotherBodyAgainstADifferentUnit);
        private const string Movement = nameof(RazzleUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesHimThroughItsCardControl);
        private const string Defense = nameof(RazzleUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(RazzleUiJourneyPlayModeTests.APausedHitAndGoneRemovalRestoresAtItsQuestion);
        private const string Undo = nameof(RazzleUiJourneyPlayModeTests.AStuntDoublesSpawnPauseUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(RazzleUiJourneyPlayModeTests.EveryBodyLeavesOnHisDefeatAndHeRespawnsAsOneBeforeARestart);
        private const string Piece = nameof(RazzleUiJourneyPlayModeTests.StagedRazzleSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(RazzleUiJourneyPlayModeTests.EveryRazzleFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(RazzleUiJourneyPlayModeTests.RazzleLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private static readonly ShapeEvidence[] BothRibbons =
        {
            Prove(SecondaryMovementDestination, Movement), Prove(SecondaryDefenseOffer, Defense)
        };

        /// <summary>The Green Movement cards print no Movement ribbon icon; their 2 is the primary.</summary>
        private static readonly ShapeEvidence[] DefenseRibbon = { Prove(SecondaryDefenseOffer, Defense) };

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] AfterTheAttack(string journey, PlayerInteractionShape choice) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(AdjacentAttackTarget, journey),
            Prove(AfterAttackTextPause, journey), Prove(ConfirmOrDecline, journey), Prove(choice, journey)
        };

        private static ShapeEvidence[] Swap(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
            Prove(BoardUnitSelection, journey), Prove(BoardSpaceSelection, journey)
        };

        private static ShapeEvidence[] Push(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
            Prove(BoardUnitSelection, journey), Prove(PushDistanceChoice, journey)
        };

        private static ShapeEvidence[] Walk(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimaryMovementDestination, journey),
            Prove(AfterMovementTextPause, journey), Prove(ConfirmOrDecline, journey),
            Prove(BoardSpaceSelection, journey)
        };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Razzle", RazzleCardContent.AllDefinitions,
            RazzleCardContent.ReturnedToBoxAtSetup,
            RazzleEffectContent.Handlers(),
            new[] { RazzleEffectContent.TwinStrikeTriggeredEffect(RazzleCardContent.ContentRevision) }, new[]
            {
                Row(RazzleCardContent.StuntDoubles, "Stunt Doubles",
                    "RazzleStuntDoublesTests.ThreeSpawns_PutThreeMoreBodiesOfTheSameHeroOnTheBoard",
                    BothRibbons, AfterTheAttack(Spawn, BoardSpaceSelection)),
                Face(RazzleCardContent.CrowdControl, "Crowd Control", new[]
                {
                    Prove(CommitAndReveal, CrowdSkill), Prove(PrimarySkillControl, CrowdSkill),
                    Prove(PrimaryDefenseOffer, CrowdDefense)
                }, new[] { "RazzleCountingFacesTests.CrowdControlsDefense_IsOnePlusTwoForEachOtherBodyInRadiusOfTheAttackedBody" }),
                Row(RazzleCardContent.PhantomStrike, "Phantom Strike",
                    "RazzleRemovalFacesTests.PhantomStrike_WithTwoBodies_OffersEitherBody_AndRemovesExactlyOne",
                    BothRibbons, AfterTheAttack(Removal, BoardSpaceSelection)),
                Row(RazzleCardContent.HitAndGone, "Hit and Gone",
                    "RazzleRemovalFacesTests.HitAndGone_WithThreeBodies_OffersEveryBody_UpToAllButOne",
                    BothRibbons, AfterTheAttack(Removal, BoardSpaceSelection)),
                Row(RazzleCardContent.Rummage, "Rummage",
                    "RazzleCountingFacesTests.Rummage_WithAnotherBodyInRadius_RetrievesTheChosenCard",
                    BothRibbons, AfterTheAttack(Retrieve, HandCardSelection)),
                Row(RazzleCardContent.IntoThinAir, "Into Thin Air",
                    "RazzleRemovalFacesTests.IntoThinAir_WithTwoBodies_MayRemoveBoth_AndHeIsOffTheBoardButNotDefeated",
                    BothRibbons, AfterTheAttack(Removal, BoardSpaceSelection)),
                Row(RazzleCardContent.Ransack, "Ransack",
                    "RazzleCountingFacesTests.Ransack_RetrievesOncePerOtherBodyInRadius",
                    BothRibbons, AfterTheAttack(Retrieve, HandCardSelection)),
                Row(RazzleCardContent.Alleyoop, "Alleyoop",
                    "RazzleMoveFacesTests.Alleyoop_SwapsTheActingBody_ThenMovesAnyOtherBodyOneSpace_KeepingItsIdentity",
                    BothRibbons, Swap(Alleyoop)),
                Row(RazzleCardContent.GroupPerformance, "Group Performance",
                    "RazzleMoveFacesTests.GroupPerformance_MovesTheOtherBodyUpToTwo_ZeroIncluded",
                    BothRibbons, Swap(Alleyoop)),
                Row(RazzleCardContent.MagicTrick, "Magic Trick",
                    "RazzleMoveFacesTests.MagicTrick_RetreatsExactlyTheSpacesTheTargetMoved",
                    BothRibbons, Push(MagicTrick)),
                Row(RazzleCardContent.TeamSpirit, "Team Spirit",
                    "RazzleMoveFacesTests.TeamSpirit_MovesTheOtherBodyUpToThree",
                    BothRibbons, Swap(Alleyoop)),
                Row(RazzleCardContent.AaaandItsGone, "Aaaand it's gone!",
                    "RazzleMoveFacesTests.AaaandItsGone_PushesUpToThree_AndRetreatsThree",
                    BothRibbons, Push(MagicTrick)),
                Row(RazzleCardContent.Tightrope, "Tightrope",
                    "RazzleMoveFacesTests.Tightrope_AfterTheMovement_MovesAnotherBodyOneSpace",
                    DefenseRibbon, Walk(Tightrope)),
                Row(RazzleCardContent.HighWire, "High Wire",
                    "RazzleMoveFacesTests.HighWireAndWireDancers_MoveAnotherBodyUpToTwoAndThree_FromBodyTwoActing",
                    DefenseRibbon, Walk(Tightrope)),
                Row(RazzleCardContent.Theatrics, "Theatrics",
                    "RazzleMoveFacesTests.Theatrics_FromBodyTwo_MeasuresItsRangeFromThatBody",
                    BothRibbons,
                    Prove(CommitAndReveal, Theatrics), Prove(PrimarySkillControl, Theatrics),
                    Prove(BoardUnitSelection, Theatrics)),
                Row(RazzleCardContent.WireDancers, "Wire Dancers",
                    "RazzleMoveFacesTests.HighWireAndWireDancers_MoveAnotherBodyUpToTwoAndThree_FromBodyTwoActing",
                    DefenseRibbon, Walk(Tightrope)),
                Row(RazzleCardContent.Spectacle, "Spectacle",
                    "RazzleRepeatFacesTests.Spectacle_RepeatsFromTheOtherBody_MeasuringRangeFromIt_OnADifferentMinion",
                    BothRibbons,
                    Prove(CommitAndReveal, Spectacle), Prove(PrimarySkillControl, Spectacle),
                    Prove(BoardUnitSelection, Spectacle), Prove(ConfirmOrDecline, Spectacle),
                    Prove(RepeatDecision, Spectacle), Prove(DifferentBodyRepeatPerformer, Spectacle)),
                Face(RazzleCardContent.TwinStrike, "Twin Strike", new[]
                {
                    Prove(UltimateRailVisibility, TwinStrike), Prove(UltimateTriggerPause, TwinStrike),
                    Prove(ConfirmOrDecline, TwinStrike), Prove(RepeatDecision, TwinStrike),
                    Prove(DifferentBodyRepeatPerformer, TwinStrike)
                }, new[] { "RazzleRepeatFacesTests.AfterStuntDoubles_TheOtherBody_PerformsTheRepeat_AgainstADifferentUnitBesideItself" }),
                UnreachableFace(RazzleCardContent.ReturnedToBoxAtSetup.Single(), "Stunted Doubles",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("razzle-piece", "Hero piece: his standees", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Piece),
                    Prove(HeroPiecePointerSelection, Piece),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "RazzleDealingTests" }),
                Component("razzle-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "RazzleRemovalFacesTests.HitAndGone_PausedWithOneBodyChosen_RestoresAndReplaysToTheSameBytes" }),
                Component("razzle-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "RazzleDealingTests.TheTrustedCatalog_RegistersHisSixHandlersAndTwinStrikesBody" }),
                Component("razzle-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "RazzleDealingTests" }),
                Component("razzle-bodies", "Which of you acts: a click on a standee, and the acting cue", new[]
                {
                    Prove(ActingBodyStandeePick, Theatrics),
                    Prove(ActingBodyStandeePick, CrowdSkill),
                    Prove(PresentationRebuild, Restore)
                }, new[] { "HeroActingBodyTests.TwoBodiesStanding_AskWhichOfHimActs_BeforeAnyActionOfTheCard" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(RazzlePlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(RazzlePlayerPathGateTests).Assembly);
        [Test] public void EveryRazzleJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(RazzleUiJourneyPlayModeTests));
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
