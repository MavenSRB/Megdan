using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;
using T = GuardsDigital.Core.Cards.Verified.TakahideCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.32i. Fixed, content-checked claims for each of Takahide's twenty-one printed faces -
    /// his twenty dealt fronts and the boxed Shameful Display - and his components, derived offline
    /// before any Unity run and proved by the real-input journeys in
    /// <see cref="TakahideUiJourneyPlayModeTests"/>.
    ///
    /// <para>His content introduces no shape of its own. His four card questions - a gold's swap and
    /// Bushido's picking a gold out of his deck, a teammate's swap picking from their hand or discard,
    /// and the discard choice - are each a card chosen on its whole face, and a question addressed to a
    /// teammate's seat is answered on that seat's window, as Whisper's forced move settled. His blades'
    /// barred spaces are an Active effect read on the board.</para>
    /// </summary>
    public sealed class TakahidePlayerPathGateTests
    {
        private const string GoldAttacks = nameof(TakahideUiJourneyPlayModeTests.EachGoldAttackSwapsItselfForTheDeckGoldHePicksAfterItsAttack);
        private const string Float = nameof(TakahideUiJourneyPlayModeTests.FloatLikeAButterflyMovesThenSwapsForTheDeckGoldHePicks);
        private const string Bushido = nameof(TakahideUiJourneyPlayModeTests.BushidoSwapsHisResolvedGoldAndTheNewCardLiesFacedown);
        private const string RedSwaps = nameof(TakahideUiJourneyPlayModeTests.HisRedSwapsLetArienSwapOrKeepHisUnresolvedCardOnHisOwnSeat);
        private const string Blades = nameof(TakahideUiJourneyPlayModeTests.HisBladesBarTheEmptySpacesAroundHimForTheEnemy);
        private const string BlueChoice = nameof(TakahideUiJourneyPlayModeTests.HisBlueChoiceMakesWaspDiscardTheColourHeChoseFromAriensDiscard);
        private const string Risk = nameof(TakahideUiJourneyPlayModeTests.CalculatedRiskLetsArienDiscardAnAttackAndMove);
        private const string Aid = nameof(TakahideUiJourneyPlayModeTests.HisGreenAidMovesHimOnceArienHasACardInHisDiscard);
        private const string Pledges = nameof(TakahideUiJourneyPlayModeTests.HisPledgesPayThemBothCoinsAndHeMayRetrieveACard);
        private const string ReadyForWar = nameof(TakahideUiJourneyPlayModeTests.ReadyForWarStandsFaceupAndHisSixCardHandPlays);
        private const string Movement = nameof(TakahideUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesHimThroughItsCardControl);
        private const string Defense = nameof(TakahideUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(TakahideUiJourneyPlayModeTests.APausedGoldSwapRestoresAtItsQuestion);
        private const string Undo = nameof(TakahideUiJourneyPlayModeTests.AFloatSwapUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(TakahideUiJourneyPlayModeTests.HeLeavesOnHisDefeatRespawnsByAClickAndARestartRebuildsTheBoard);
        private const string Spawn = nameof(TakahideUiJourneyPlayModeTests.StagedTakahideSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(TakahideUiJourneyPlayModeTests.EveryTakahideFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(TakahideUiJourneyPlayModeTests.TakahideLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        // -------------------------------------------------------------------------------------------
        // Row builders
        // -------------------------------------------------------------------------------------------

        /// <summary>Every coloured front prints the ribbon's Movement and Defense.</summary>
        private static readonly ShapeEvidence[] BothRibbons =
        {
            Prove(SecondaryMovementDestination, Movement), Prove(SecondaryDefenseOffer, Defense)
        };

        /// <summary>Float like a Butterfly and Bushido print a Defense and no Movement.</summary>
        private static readonly ShapeEvidence[] DefenseRibbon = { Prove(SecondaryDefenseOffer, Defense) };

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            ShapeEvidence[] ribbon, params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] GoldAttack(PlayerInteractionShape target) => new[]
        {
            Prove(CommitAndReveal, GoldAttacks), Prove(target, GoldAttacks),
            Prove(AfterAttackTextPause, GoldAttacks), Prove(HandCardSelection, GoldAttacks)
        };

        private static readonly ShapeEvidence[] RedSwap =
        {
            Prove(CommitAndReveal, RedSwaps), Prove(AdjacentAttackTarget, RedSwaps),
            Prove(AfterAttackTextPause, RedSwaps), Prove(BoardUnitSelection, RedSwaps),
            Prove(ConfirmOrDecline, RedSwaps), Prove(HandCardSelection, RedSwaps)
        };

        private static readonly ShapeEvidence[] Blade =
        {
            Prove(CommitAndReveal, Blades), Prove(AdjacentAttackTarget, Blades),
            Prove(AfterAttackTextPause, Blades), Prove(ActiveEffectReading, Blades)
        };

        private static readonly ShapeEvidence[] Blue =
        {
            Prove(CommitAndReveal, BlueChoice), Prove(PrimarySkillControl, BlueChoice),
            Prove(BoardUnitSelection, BlueChoice), Prove(HandCardSelection, BlueChoice)
        };

        /// <summary>A teammate's may-discard, then a move: Calculated Risk's for the teammate, the Greens' for him.</summary>
        private static ShapeEvidence[] DiscardThenMove(string journey) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
            Prove(BoardUnitSelection, journey), Prove(ConfirmOrDecline, journey),
            Prove(HandCardSelection, journey), Prove(BoardSpaceSelection, journey)
        };

        private static readonly ShapeEvidence[] Pledge =
        {
            Prove(CommitAndReveal, Pledges), Prove(PrimarySkillControl, Pledges),
            Prove(BoardUnitSelection, Pledges), Prove(ConfirmOrDecline, Pledges),
            Prove(HandCardSelection, Pledges)
        };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Takahide", T.AllDefinitions, T.ReturnedToBoxAtSetup, T.Handlers(),
            Array.Empty<EffectDefinition>(), new[]
            {
                // ---- Gold and silver ----
                Row(T.StingLikeABee, "Sting like a Bee",
                    "TakahideBasicAndRedFacesTests.StingLikeABee_TargetsOnlyAtMaximumRange_ThenSwapsWithADeckGold",
                    BothRibbons, GoldAttack(RangedAttackTarget)),
                Row(T.FloatLikeAButterfly, "Float like a Butterfly",
                    "TakahideBasicAndRedFacesTests.FloatLikeAButterfly_MovesFiveThenSwaps_TkQ1",
                    DefenseRibbon,
                    Prove(CommitAndReveal, Float), Prove(PrimaryMovementDestination, Float),
                    Prove(AfterMovementTextPause, Float), Prove(HandCardSelection, Float)),
                Row(T.StrikeLikeATiger, "Strike like a Tiger",
                    "TakahideBasicAndRedFacesTests.StrikeLikeATiger_AttacksForSevenAdjacent_ThenSwaps",
                    BothRibbons, GoldAttack(AdjacentAttackTarget)),
                Row(T.Bushido, "Bushido",
                    "TakahideGoldSwapTests.Bushido_SwapsAResolvedGold_AndTheNewCardIsResolvedFacedownInItsSlot",
                    DefenseRibbon,
                    Prove(CommitAndReveal, Bushido), Prove(PrimarySkillControl, Bushido),
                    Prove(HandCardSelection, Bushido)),

                // ---- Red ----
                Row(T.SetAnExample, "Set an Example",
                    "TakahideBasicAndRedFacesTests.SetAnExample_ATeammateInRadiusThreeSwapsTheirUnresolvedCard",
                    BothRibbons, RedSwap),
                Row(T.LeadFromTheFront, "Lead from the Front",
                    "TakahideBasicAndRedFacesTests.LeadFromTheFront_ReachesRadiusFour",
                    BothRibbons, RedSwap),
                Row(T.SpinningBlade, "Spinning Blade",
                    "TakahideBasicAndRedFacesTests.SpinningBlade_BarsTheEnemyFromTheSpacesAdjacentToTakahide_ButNotTheTeammate",
                    BothRibbons, Blade),
                Row(T.HoldMySake, "Hold my Sake",
                    "TakahideBasicAndRedFacesTests.HoldMySake_ReachesTheTeammatesDiscard",
                    BothRibbons, RedSwap),
                Row(T.BladeHelix, "Blade Helix",
                    "TakahideBasicAndRedFacesTests.BladeHelix_BarsItsPrintedRadiusOfOne_AndARadiusItemWidensIt",
                    BothRibbons, Blade),

                // ---- Blue ----
                Row(T.ProvenWarrior, "Proven Warrior",
                    "TakahideBlueAndGreenFacesTests.ProvenWarrior_TakahideChoosesATeammatesDiscardedCard_AndTheEnemyDiscardsItsColour",
                    BothRibbons, Blue),
                Row(T.ChosenChampion, "Chosen Champion",
                    "TakahideBlueAndGreenFacesTests.EveryFace_PrintsWhatTheFrontPrints",
                    BothRibbons, Blue),
                Row(T.CalculatedRisk, "Calculated Risk",
                    "TakahideBlueAndGreenFacesTests.CalculatedRisk_TheTeammateDiscardsAnAttackCard_ThenMovesUpToTwo",
                    BothRibbons, DiscardThenMove(Risk)),
                Row(T.TheRightHand, "The Right Hand",
                    "TakahideBlueAndGreenFacesTests.TheRightHand_HitsBothEnemiesInRadius_W3",
                    BothRibbons, Blue),
                Row(T.TacticalGambit, "Tactical Gambit",
                    "TakahideBlueAndGreenFacesTests.TacticalGambit_TheTeammateMovesThroughObstacles",
                    BothRibbons, DiscardThenMove(Risk)),

                // ---- Green ----
                Row(T.ComeToAid, "Come to Aid",
                    "TakahideBlueAndGreenFacesTests.ComeToAid_AfterTheTeammatesDiscard_TakahideMovesUpToThree",
                    BothRibbons, DiscardThenMove(Aid)),
                Row(T.BringTheRelief, "Bring the Relief",
                    "TakahideBlueAndGreenFacesTests.BringTheRelief_ReachesRangeFour_AndMovesUpToFour",
                    BothRibbons, DiscardThenMove(Aid)),
                Row(T.PledgeOfAllegiance, "Pledge of Allegiance",
                    "TakahideBlueAndGreenFacesTests.PledgeOfAllegiance_BothGainACoin_AndTakahideMayRetrieveHisOwnCard",
                    BothRibbons, Pledge),
                Row(T.CommitReserves, "Commit Reserves",
                    "TakahideBlueAndGreenFacesTests.CommitReserves_TakahideMovesThroughObstacles",
                    BothRibbons, DiscardThenMove(Aid)),
                Row(T.LoyalRetainer, "Loyal Retainer",
                    "TakahideBlueAndGreenFacesTests.LoyalRetainer_PaysTwoCoinsEach_AndAFalseCheckPaysNothing",
                    BothRibbons, Pledge),

                // ---- The Ultimate and the Handicap ----
                Face(T.ReadyForWar, "Ready for War",
                    new[] { Prove(UltimateRailVisibility, ReadyForWar) },
                    new[] { "TakahideReadyForWarTests.TheUnlock_ReturnsBushidoToTheDeck_AndTakesBothDeckGolds_TkQ5" }),
                UnreachableFace(T.ShamefulDisplay, "Shameful Display",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),

                // ---- Components ----
                Component("takahide-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "TakahideDealingTests" }),
                Component("takahide-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "TakahideGoldSwapTests.TheSwapChoice_SurvivesARestore_AndFinishesInTheSameBytes" }),
                Component("takahide-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "TakahideDealingTests.TheSetIsTheShardsTwentyOneFaces_AllComposed_UnderHisDealtRevision" }),
                Component("takahide-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "TakahideDealingTests" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(TakahidePlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(TakahidePlayerPathGateTests).Assembly);
        [Test] public void EveryTakahideJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(TakahideUiJourneyPlayModeTests));
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
