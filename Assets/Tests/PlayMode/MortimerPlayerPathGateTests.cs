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
    /// Milestone 9.8g. Fixed, content-checked claims for each dealt Mortimer face and component. Every
    /// face composes on the one repeated-choice shape, so the Zombie move bullet the Red faces share
    /// with the Blue ones is proved where a journey takes it (Robbing Zombies' move), and each face's
    /// own bullet is proved by its group's journey.
    /// </summary>
    public sealed class MortimerPlayerPathGateTests
    {
        private const string Awaken = nameof(MortimerUiJourneyPlayModeTests.AwakenPlacesTwoZombiesBesideMortimerAndDeclinesTheRest);
        private const string AwakenDefends = nameof(MortimerUiJourneyPlayModeTests.AwakenDiscardedToDefendStillPlacesItsZombies);
        private const string Knife = nameof(MortimerUiJourneyPlayModeTests.KnifeRemovesAZombieBesideWaspForAPlusOneAttackAndDefeatsHer);
        private const string Dead = nameof(MortimerUiJourneyPlayModeTests.DeadFacesHitTheMinionThenWaspBesideAZombieDiscards);
        private const string Brains = nameof(MortimerUiJourneyPlayModeTests.BrainsFacesRetrieveACardBeforeTheAttack);
        private const string Slam = nameof(MortimerUiJourneyPlayModeTests.SlamFacesPushWaspAwayFromAZombie);
        private const string Rob = nameof(MortimerUiJourneyPlayModeTests.RobbingFacesMoveAZombieAndGainACoin);
        private const string Dive = nameof(MortimerUiJourneyPlayModeTests.DiveFacesSwapMortimerWithAZombie);
        private const string Horde = nameof(MortimerUiJourneyPlayModeTests.HordeFacesReplaceAnEnemyMinionBesideTwoZombiesWithAZombie);
        private const string Puppets = nameof(MortimerUiJourneyPlayModeTests.MasterOfPuppetsOpensAFourthAndFifthPass);
        private const string Movement = nameof(MortimerUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesMortimerThroughItsCardControl);
        private const string Defense = nameof(MortimerUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(MortimerUiJourneyPlayModeTests.APausedCrawlingDiscardRestoresAtWaspsQuestion);
        private const string Undo = nameof(MortimerUiJourneyPlayModeTests.AnAwakenPlacementPauseUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(MortimerUiJourneyPlayModeTests.ZombiesOutlastMortimersDefeatAndTheRoundEndBeforeARestart);
        private const string Spawn = nameof(MortimerUiJourneyPlayModeTests.StagedMortimerSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(MortimerUiJourneyPlayModeTests.EveryMortimerFaceKeepsTheStandardCardFootprint);
        private const string Upgrade = nameof(MortimerUiJourneyPlayModeTests.MortimerLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private static readonly ShapeEvidence[] Ribbon =
        {
            Prove(SecondaryMovementDestination, Movement), Prove(SecondaryDefenseOffer, Defense)
        };

        private static PlayerPathRow Row(CardDefinitionId id, string title, string scenario,
            params ShapeEvidence[] shapes) =>
            Face(id, title, shapes.Concat(Ribbon).ToArray(), new[] { scenario });

        private static ShapeEvidence[] Attack(string journey, bool before, bool upTo) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(AdjacentAttackTarget, journey),
            Prove(before ? BeforeAttackTextPause : AfterAttackTextPause, journey),
            Prove(ModalBulletChoice, journey), Prove(BoardUnitSelection, journey),
            Prove(HandCardSelection, journey), Prove(BoardSpaceSelection, Rob)
        }.Concat(upTo ? new[] { Prove(ConfirmOrDecline, journey) } : Array.Empty<ShapeEvidence>()).ToArray();

        private static ShapeEvidence[] Skill(string journey, bool upTo, bool push = false) => new[]
        {
            Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
            Prove(ModalBulletChoice, journey), Prove(BoardUnitSelection, journey),
            Prove(BoardSpaceSelection, journey)
        }.Concat(upTo ? new[] { Prove(ConfirmOrDecline, journey) } : Array.Empty<ShapeEvidence>())
            .Concat(push ? new[] { Prove(PushDistanceChoice, Slam) } : Array.Empty<ShapeEvidence>()).ToArray();

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Mortimer", MortimerCardContent.AllDefinitions,
            MortimerCardContent.ReturnedToBoxAtSetup,
            MortimerComponentContent.Handlers(),
            Array.Empty<EffectDefinition>(), new[]
            {
                Row(MortimerAttackFaces.KnifeOfTheLivingDead, "Knife of the Living Dead",
                    "MortimerAttackFacesTests.Knife_EachZombieRemovedBesideTheTarget_AddsOneAttack",
                    Prove(CommitAndReveal, Knife), Prove(AdjacentAttackTarget, Knife),
                    Prove(BeforeAttackTextPause, Knife), Prove(ConfirmOrDecline, Knife),
                    Prove(ModalBulletChoice, Knife), Prove(BoardUnitSelection, Knife),
                    Prove(BoardSpaceSelection, Knife), Prove(ActiveEffectReading, Knife)),
                Face(MortimerCardContent.Awaken, "Awaken!", new[]
                {
                    Prove(CommitAndReveal, Awaken), Prove(PrimarySkillControl, Awaken),
                    Prove(ConfirmOrDecline, Awaken), Prove(BoardSpaceSelection, Awaken),
                    Prove(PrimaryDefenseOffer, AwakenDefends), Prove(DefenseTextApplication, AwakenDefends)
                }, new[] { "MortimerAwakenDefenseTests.DiscardedToDefend_AwakensTextPlacesZombies" }),
                Row(MortimerAttackFaces.CrawlingDead, "Crawling Dead",
                    "MortimerAttackFacesTests.Crawling_AnotherEnemyHeroBesideAZombie_DiscardsAfterTheAttack",
                    Attack(Dead, before: false, upTo: false)),
                Row(MortimerAttackFaces.WalkingDead, "Walking Dead",
                    "MortimerAttackFacesTests.Walking_EachEnemyHeroCanOnlyBeTargetedOnce",
                    Attack(Dead, before: false, upTo: true)),
                Row(MortimerAttackFaces.Braains, "Braains...!",
                    "MortimerAttackFacesTests.Braains_WithAnEnemyHeroInRadiusBesideAZombie_RetrievesADiscardedCard",
                    Attack(Brains, before: true, upTo: true)),
                Row(MortimerAttackFaces.RacingDead, "Racing Dead",
                    "MortimerAttackFacesTests.Walking_EachEnemyHeroCanOnlyBeTargetedOnce",
                    Attack(Dead, before: false, upTo: true)),
                Row(MortimerAttackFaces.Braaaaaaains, "Braaaaaaaains...!",
                    "MortimerAttackFacesTests.Braains_WithAnEnemyHeroInRadiusBesideAZombie_RetrievesADiscardedCard",
                    Attack(Brains, before: true, upTo: true)),
                Row(MortimerBlueFaces.CorpseSlam, "Corpse Slam",
                    "MortimerBlueFacesTests.CorpseSlam_AZombieThatStaysPut_PushesAUnitBesideItDirectlyAway",
                    Skill(Slam, upTo: true, push: true)),
                Row(MortimerBlueFaces.MorbidMosh, "Morbid Mosh",
                    "MortimerBlueFacesTests.MorbidMosh_APausedPushRestoresAndReplays",
                    Skill(Slam, upTo: true, push: true)),
                Row(MortimerBlueFaces.RobbingZombies, "Robbing Zombies",
                    "MortimerBlueFacesTests.RobbingZombies_EachZombieMoveGainsOneCoin_EvenStandingStill",
                    Skill(Rob, upTo: true)),
                Row(MortimerBlueFaces.MacabreMayhem, "Macabre Mayhem",
                    "MortimerBlueFacesTests.CorpseSlam_AMovedZombie_PushesFromWhereItNowStands",
                    Skill(Slam, upTo: true, push: true)),
                Row(MortimerBlueFaces.StalkingScalpers, "Stalking Scalpers",
                    "MortimerBlueFacesTests.RobbingZombies_EachZombieMoveGainsOneCoin_EvenStandingStill",
                    Skill(Rob, upTo: true)),
                Row(MortimerGreenFaces.StageDive, "Stage Dive",
                    "MortimerGreenFacesTests.StageDive_SwapsMortimerWithAZombieInRange",
                    Skill(Dive, upTo: false)),
                Row(MortimerGreenFaces.CrowdDrift, "Crowd Drift",
                    "MortimerGreenFacesTests.StageDive_MovesAZombieExactlyOneSpace",
                    Skill(Dive, upTo: true)),
                Row(MortimerGreenFaces.GatheringHorde, "Gathering Horde",
                    "MortimerGreenFacesTests.GatheringHorde_ReplacesAnEnemyMinionBesideTwoZombies_WithoutAReward",
                    Skill(Horde, upTo: true)),
                Row(MortimerGreenFaces.CrowdSurf, "Crowd Surf",
                    "MortimerGreenFacesTests.StageDive_SwapsMortimerWithAZombieInRange",
                    Skill(Dive, upTo: true)),
                Row(MortimerGreenFaces.ArmyOfDarkness, "Army of Darkness",
                    "MortimerGreenFacesTests.ArmyOfDarkness_ReplacesOncePerTurn_ReclaimingWhenAllFourZombiesStand",
                    Skill(Horde, upTo: true)),
                Face(MortimerCardContent.MasterOfPuppets, "Master of Puppets",
                    new[] { Prove(UltimateRailVisibility, Puppets) },
                    new[] { "MortimerRepeatedChoiceTests.UpToThree_WithMasterOfPuppetsUnlocked_OffersAFourthAndFifth" }),
                UnreachableFace(MortimerCardContent.ReturnedToBoxAtSetup.Single(), "Shallow Graves",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),
                Component("mortimer-piece", "Hero piece", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Spawn),
                    Prove(HeroPiecePointerSelection, Spawn),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "MortimerDealingTests" }),
                Component("mortimer-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "MortimerAttackFacesTests.Knife_APauseInsideTheChain_RestoresAndReplays" }),
                Component("mortimer-footprint", "Every complete face",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "MortimerDealingTests.TheTrustedCatalog_RegistersTheZombieAndHisHandlers" }),
                Component("mortimer-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "MortimerDealingTests" }),
                Component("mortimer-zombies", "Zombie tokens: placement, retention through round end", new[]
                {
                    Prove(BoardSpaceSelection, Awaken),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "MortimerAwakenTests.Zombie_IsAFourTokenSupplyThatNoRoundEndRemoves" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(MortimerPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(MortimerPlayerPathGateTests).Assembly);
        [Test] public void EveryMortimerJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(typeof(MortimerUiJourneyPlayModeTests));
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
