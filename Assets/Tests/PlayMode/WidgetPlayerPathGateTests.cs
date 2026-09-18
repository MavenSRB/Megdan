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
    public sealed class WidgetPlayerPathGateTests
    {
        private const string Secondary = nameof(WidgetLifecycleJourneyTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string Bond = nameof(WidgetUiJourneyTests.DragonBondMovesBothPiecesInEitherOrderAndRelocatesTheSamePyro);
        private const string Red = nameof(WidgetUiJourneyTests.RedFacesResolveTheirPreAndPostAttackPyroOperations);
        private const string Blue = nameof(WidgetUiJourneyTests.BlueSwapsAndPairedMovesUseBoardDestinations);
        private const string Friendly = nameof(WidgetUiJourneyTests.EveryFriendlySwapChoosesTheAllyOnTheBoard);
        private const string Breath = nameof(WidgetUiJourneyTests.BreathFacesAskTheOtherHeroToDiscardAWholeCard);
        private const string Empty = nameof(WidgetUiJourneyTests.ScorchingBreathDefeatsAnEmptyHandWithoutRemovingPyro);
        private const string Minion = nameof(WidgetUiJourneyTests.NibbleAndGnawRemoveTheirChosenMinionThenPyro);
        private const string Gold = nameof(WidgetReuseJourneyTests.FightAsOneReusesAResolvedTokenOnlySkillWithoutMovingItsCard);
        private const string Exclusion = nameof(WidgetReuseJourneyTests.FightAsOneExcludesTheAttackedHeroFromItsBorrowedSkill);
        private const string Knight = nameof(WidgetReuseJourneyTests.DragonKnightReusesTheCurrentSkillAfterZeroMovementThroughUndoAndRestore);
        private const string Life = nameof(WidgetLifecycleJourneyTests.PyroSurvivesWidgetDefeatAndRespawnThenRestartRebuildsTheSession);
        private const string Clear = nameof(WidgetLifecycleJourneyTests.ClearRemovesPyroThroughItsBoardPieceAndUndoRestoresIt);
        private const string Item = nameof(WidgetLifecycleJourneyTests.LevelUpUsesTheChosenWholeFaceAndItsEarnedAttackItem);
        private const string Spawn = nameof(WidgetSceneIntegrationTests.EveryStagedBranchSpawnsWidgetWithHisOwnArtworkAndPointer);
        private const string Art = nameof(WidgetSceneIntegrationTests.DragonBondPlacesPyrosOwnBoardArtworkThroughPlayerInput);
        private const string Footprint = nameof(WidgetSceneIntegrationTests.EveryWidgetFaceUsesTheStandardFootprint);

        private static PlayerPathRow Ordinary(CardDefinitionId id, string name, string journey,
            params PlayerInteractionShape[] shapes) => Face(id,name,
                new[] { Prove(CommitAndReveal,journey),Prove(SecondaryMovementDestination,Secondary),Prove(SecondaryDefenseOffer,Secondary) }
                    .Concat(shapes.Select(shape => Prove(shape,journey))).ToArray(),new[] { "WidgetPrintedSetTests" });
        private static readonly PlayerInteractionShape[] Diversion = { AdjacentAttackTarget,AfterAttackTextPause,BoardSpaceSelection };
        private static readonly PlayerInteractionShape[] Airborne = { AdjacentAttackTarget,BeforeAttackTextPause,ConfirmOrDecline,BoardUnitSelection };
        private static readonly PlayerInteractionShape[] Swap = { PrimarySkillControl,ModalBulletChoice,BoardUnitSelection };
        private static readonly PlayerInteractionShape[] Pair = { PrimarySkillControl,BoardUnitSelection,BoardSpaceSelection };
        private static readonly PlayerInteractionShape[] Discard = { PrimarySkillControl,BoardUnitSelection,HandCardSelection };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix("Widget",
            WidgetCardContent.AllDefinitions,WidgetCardContent.ReturnedToBoxAtSetup,Array.Empty<IEffectCapabilityHandler>(),
            new[] { WidgetEffectContent.DragonKnight(WidgetCardContent.DragonKnight,WidgetCardContent.ContentRevision) },new[] {
                Ordinary(WidgetCardContent.FightAsOne,"Fight as One",Gold,AdjacentAttackTarget,AfterAttackTextPause,ConfirmOrDecline,HandCardSelection),
                Face(WidgetCardContent.DragonBond,"Dragon Bond",new[] { Prove(CommitAndReveal,Bond),Prove(PrimarySkillControl,Bond),
                    Prove(SecondaryDefenseOffer,Secondary),Prove(ModalBulletChoice,Bond),Prove(BoardSpaceSelection,Bond) },new[] { "WidgetTokenOperationTests" }),
                Ordinary(WidgetCardContent.DiversionaryStrike,"Diversionary Strike",Red,Diversion),
                Ordinary(WidgetCardContent.DiversionaryAttack,"Diversionary Attack",Red,Diversion),
                Ordinary(WidgetCardContent.DiversionaryAssault,"Diversionary Assault",Red,Diversion),
                Ordinary(WidgetCardContent.AirborneAttack,"Airborne Attack",Red,Airborne),
                Ordinary(WidgetCardContent.AirborneAssault,"Airborne Assault",Red,Airborne.Concat(new[] { AfterAttackTextPause }).ToArray()),
                Ordinary(WidgetCardContent.AllAboard,"All Aboard",Blue,Swap),
                Ordinary(WidgetCardContent.TakeOff,"Take-Off",Blue,Swap),
                Ordinary(WidgetCardContent.SafeLanding,"Safe Landing",Blue,Swap.Concat(new[] { ConfirmOrDecline,BoardSpaceSelection }).ToArray()),
                Ordinary(WidgetCardContent.DragOff,"Drag Off",Blue,Pair),
                Ordinary(WidgetCardContent.CarryAway,"Carry Away",Blue,Pair),
                Ordinary(WidgetCardContent.FieryBreath,"Fiery Breath",Breath,Discard),
                Ordinary(WidgetCardContent.FlamingBreath,"Flaming Breath",Breath,Discard),
                Ordinary(WidgetCardContent.ScorchingBreath,"Scorching Breath",Breath,Discard),
                Ordinary(WidgetCardContent.Nibble,"Nibble",Minion,PrimarySkillControl,BoardUnitSelection),
                Ordinary(WidgetCardContent.Gnaw,"Gnaw",Minion,PrimarySkillControl,BoardUnitSelection),
                Face(WidgetCardContent.DragonKnight,"Dragon Knight",new[] { Prove(UltimateRailVisibility,Knight),
                    Prove(UltimateTriggerPause,Knight),Prove(ConfirmOrDecline,Knight),Prove(HandCardSelection,Knight) },new[] { "WidgetSkillReuseTests" }),
                UnreachableFace(WidgetCardContent.FollowAlong,"Follow Along","Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player format."),
                Component("widget-piece","Hero piece",new[] { Prove(HeroPieceSpawnAndIdentity,Spawn),Prove(HeroPiecePointerSelection,Spawn),
                    Prove(HeroPieceMovement,Secondary),Prove(HeroPieceDefeatAndRespawn,Life),Prove(PresentationRebuild,Life) },new[] { "WidgetDealingTests" }),
                Component("widget-session","Session continuity",new[] { Prove(ConfirmOrDecline,Exclusion),Prove(SnapshotRestoreContinuation,Knight),Prove(RetainedPrefixUndo,Knight),
                    Prove(ReplayReconstruction,Knight),Prove(RestartReconstruction,Life) },new[] { "WidgetSkillReuseTests" }),
                Component("widget-footprint","Complete faces",new[] { Prove(StandardCardFootprintConformance,Footprint) },new[] { "ConfiguredCardArtworkCatalogTests" }),
                Component("widget-items","Earned items",new[] { Prove(ItemVisibilityAndUse,Item) },new[] { "WidgetPrintedSetTests" }),
                Component("widget-pyro","Persistent Pyro",new[] { Prove(BoardSpaceSelection,Art),Prove(BoardSpaceSelection,Bond),
                    Prove(BoardSpaceSelection,Clear),Prove(BoardUnitSelection,Friendly),Prove(BoardUnitSelection,Empty),
                    Prove(BoardUnitSelection,Minion),Prove(PresentationRebuild,Life) },new[] { "WidgetTokenOperationTests","WidgetCoupledMoveTests","WidgetAnchoredSkillTests" }),
                AbsentComponent("widget-markers","Hero-owned markers","No Widget face gives, transfers or returns a marker.")
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesContentShapes() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(WidgetPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(WidgetPlayerPathGateTests).Assembly);
        [Test] public void EveryJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(WidgetUiJourneyTests),
            typeof(WidgetReuseJourneyTests),typeof(WidgetLifecycleJourneyTests),typeof(WidgetSceneIntegrationTests));
        [Test] public void EveryRequiredComponentShapeIsDeclared() => Matrix().AssertEveryRequiredComponentShapeIsDeclared();
        [Test] public void MarkerAbsenceMatchesContent() => Matrix().AssertNoContentNamesAnyCapability(new[] {
            "bain-give-bounty-marker","bain-transfer-bounty-marker","bain-return-bounty-marker","give-marker" },"Widget owns no marker.");
        [Test] public void EveryAuthoritativeScenarioExists()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(value => value.GetName().Name == "GuardsDigital.EditModeTests");
            foreach (var scenario in Matrix().Rows.SelectMany(row => row.AuthoritativeScenarios))
                Assert.That(assembly.GetTypes().Any(type => type.Name == scenario), Is.True,scenario);
        }
    }
}
