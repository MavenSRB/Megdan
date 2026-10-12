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
    /// Milestone 9.30j. Fixed, content-checked claims for each of Gydion's forty-one printed faces - his
    /// eighteen dealt fronts, the boxed Cheap Trick and his twenty-two spells - and his components,
    /// derived offline before any Unity run and proved by the real-input journeys in
    /// <see cref="GydionUiJourneyPlayModeTests"/> and <see cref="GydionSpellUiJourneyPlayModeTests"/>.
    ///
    /// <para>His content introduces two shapes no earlier hero prints, each read off the content:
    /// <see cref="PlayerInteractionShape.SpellCastFromTheBook"/> stands in a spell's row where a front's
    /// commitment and Skill control stand (a spell is cast from the book, never committed), and
    /// <see cref="PlayerInteractionShape.SpellFaceChoice"/> replaces p.10's modal on a choice whose
    /// every option names a spell, drawn as spell faces. The cast and Find Familiar's removal turn a
    /// spell face up for every seat, milestone 8.40k's reading. Suggestion's direction, chosen by the
    /// moved hero's own seat, is the board-space click Whisper's forced move already established, so
    /// it is not a shape of its own; its journey answers it on that seat's HUD all the same.</para>
    /// </summary>
    public sealed class GydionPlayerPathGateTests
    {
        private const string Cast = nameof(GydionUiJourneyPlayModeTests.CantripCastsMagicMissileFromItsFaceAndTheDefenderSeesTheSpell);
        private const string Refill = nameof(GydionUiJourneyPlayModeTests.PrepareSpellsRefillsTheBookAndEndsARunningShield);
        private const string Sunburst = nameof(GydionUiJourneyPlayModeTests.GreaterEvocationCaptionsSunburstAndHitsAUnitAtExactlyThatRange);
        private const string Familiar = nameof(GydionUiJourneyPlayModeTests.FindFamiliarPlacesTheOwlAndRemovesUpToThreeSpellsFaceup);
        private const string DimensionDoor = nameof(GydionUiJourneyPlayModeTests.DimensionDoorPlacesHimAtTheCountedRadius);
        private const string Suggestion = nameof(GydionUiJourneyPlayModeTests.SuggestionsDirectionIsPickedOnTheMovedEnemysOwnSeat);
        private const string EnergyDrain = nameof(GydionUiJourneyPlayModeTests.EnergyDrainTakesANonBasicCardAndRegainsASpentLife);
        private const string Wish = nameof(GydionUiJourneyPlayModeTests.TheArchwizardOffersWishInsteadAndWishCastsASpellNoHeldCardLists);
        private const string ThirdWish = nameof(GydionUiJourneyPlayModeTests.AThirdWishWinsTheGameForHisTeam);
        private const string ShieldImmunity = nameof(GydionUiJourneyPlayModeTests.ShieldRefusesABasicAttackButNotANonBasicOne);
        private const string Invulnerability = nameof(GydionUiJourneyPlayModeTests.InvulnerabilityRefusesANonBasicAttackButNotABasicOne);
        private const string Movement = nameof(GydionUiJourneyPlayModeTests.EveryPrintedSecondaryMovementMovesHimThroughItsCardControl);
        private const string Defense = nameof(GydionUiJourneyPlayModeTests.EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace);
        private const string Restore = nameof(GydionUiJourneyPlayModeTests.APausedSpellChoiceRestoresAtItsQuestion);
        private const string Undo = nameof(GydionUiJourneyPlayModeTests.ACastUndoesToItsRetainedPrefixAndReplaysExactly);
        private const string Lifecycle = nameof(GydionUiJourneyPlayModeTests.HeLeavesOnHisDefeatRespawnsByAClickAndARestartRebuildsTheBoard);
        private const string Piece = nameof(GydionUiJourneyPlayModeTests.StagedGydionSpawnsWithHisOwnArtworkAndBoardPointer);
        private const string Footprint = nameof(GydionUiJourneyPlayModeTests.EveryGydionFaceKeepsItsFootprint);
        private const string Upgrade = nameof(GydionUiJourneyPlayModeTests.GydionLevelUpAddsTheChosenCardAndShowsItsSiblingItem);

        private const string ShockingGrasp = nameof(GydionSpellUiJourneyPlayModeTests.ShockingGraspHitsAnAdjacentHeroAndMovesHerOneSpace);
        private const string ExpeditiousRetreat = nameof(GydionSpellUiJourneyPlayModeTests.ExpeditiousRetreatWalksOnlyInAStraightLine);
        private const string BurningHands = nameof(GydionSpellUiJourneyPlayModeTests.BurningHandsMakesAHeroBesideItsTargetDiscard);
        private const string Fireball = nameof(GydionSpellUiJourneyPlayModeTests.FireballPassesOverAUnitBesideHisFriendlyMinion);
        private const string VampiricTouch = nameof(GydionSpellUiJourneyPlayModeTests.VampiricTouchRetrievesADiscardedCardAfterTheAttack);
        private const string CreateUndead = nameof(GydionSpellUiJourneyPlayModeTests.CreateUndeadRespawnsHisDefeatedMinion);
        private const string DominatePerson = nameof(GydionSpellUiJourneyPlayModeTests.DominatePersonDefeatsAMinionBesideTheEnemyHero);
        private const string PowerWordKill = nameof(GydionSpellUiJourneyPlayModeTests.PowerWordKillDefeatsAnEnemyHeroWithAnEmptyHand);
        private const string Transmutation = nameof(GydionSpellUiJourneyPlayModeTests.LesserTransmutationGainsCoinsAndDisintegratesAMinion);
        private const string Polymorph = nameof(GydionSpellUiJourneyPlayModeTests.PolymorphSwapsTheEnemyHeroWithAMinion);
        private const string Banishment = nameof(GydionSpellUiJourneyPlayModeTests.BanishmentPlacesAnAdjacentMinionIntoRadius);
        private const string CloudKill = nameof(GydionSpellUiJourneyPlayModeTests.CloudKillMakesTheEnemyDiscardABasicCard);

        // -------------------------------------------------------------------------------------------
        // Row builders
        // -------------------------------------------------------------------------------------------

        /// <summary>A casting front: committed, its Skill started, the cast spell turned face up.</summary>
        private static ShapeEvidence[] Front(string journey, bool choosesBetweenSpells) =>
            new[]
                {
                    Prove(CommitAndReveal, journey), Prove(PrimarySkillControl, journey),
                    Prove(PublicCardRevealReading, journey)
                }
                .Concat(choosesBetweenSpells ? new[] { Prove(SpellFaceChoice, journey) } : new ShapeEvidence[0])
                .ToArray();

        /// <summary>Every coloured front prints the ribbon's Movement and Defense.</summary>
        private static readonly ShapeEvidence[] BothRibbons =
        {
            Prove(SecondaryMovementDestination, Movement), Prove(SecondaryDefenseOffer, Defense)
        };

        /// <summary>The Gold and Silver basics print a Defense and no Movement.</summary>
        private static readonly ShapeEvidence[] DefenseRibbon = { Prove(SecondaryDefenseOffer, Defense) };

        private static PlayerPathRow Coloured(CardDefinitionId id, string title, string scenario,
            string journey, bool choosesBetweenSpells) =>
            Face(id, title, Front(journey, choosesBetweenSpells).Concat(BothRibbons).ToArray(), new[] { scenario });

        private static PlayerPathRow Spell(CardDefinitionId id, string title, string scenario,
            string journey, params PlayerInteractionShape[] shapes) =>
            Face(id, title,
                new[] { Prove(SpellCastFromTheBook, journey) }
                    .Concat(shapes.Select(shape => Prove(shape, journey)))
                    .ToArray(),
                new[] { scenario });

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Gydion", GydionCardContent.AllDefinitions,
            GydionCardContent.ReturnedToBoxAtSetup,
            GydionCardContent.ReuseSpellHandlers().Concat(GydionCardContent.ImmunityDiscardLifeMoveHandlers()),
            Array.Empty<EffectDefinition>(), new[]
            {
                // ---- The two basics and the Ultimate ----
                Face(GydionCardContent.Cantrip, "Cantrip",
                    Front(Cast, true).Concat(DefenseRibbon).ToArray(),
                    new[] { "GydionDealingTests.EveryCastingFront_CastsTheSpellsItLists_InPrintedOrder" }),
                Face(GydionCardContent.PrepareSpells, "Prepare Spells", new[]
                {
                    Prove(CommitAndReveal, Refill), Prove(PrimarySkillControl, Refill),
                    Prove(ModalBulletChoice, Wish), Prove(PublicCardRevealReading, Wish),
                    Prove(SecondaryDefenseOffer, Defense)
                }, new[] { "GydionDealingTests.ADealtFront_CastsFromHisBook_AndPrepareSpellsReturnsTheSpell" }),
                Face(GydionCardContent.TheArchwizard, "The Archwizard",
                    new[] { Prove(UltimateRailVisibility, Wish) },
                    new[] { "GydionWishTests.TheArchwizard_OffersNothingWithWishRemoved_OrWhileItIsLocked" }),
                UnreachableFace(GydionCardContent.CheapTrick, "Cheap Trick",
                    "Rulebook p.6 returns the p.20 Handicap face to the box at setup."),

                // ---- Red: Evocation and Necromancy ----
                Coloured(GydionCardContent.ElementaryEvocation, "Elementary Evocation",
                    "GydionReuseSpellTests.BurningHands_WithTwoHeroesBesideTheTarget_GydionPicksTheOneWhoDiscards",
                    BurningHands, false),
                Coloured(GydionCardContent.LesserEvocation, "Lesser Evocation",
                    "GydionReuseSpellTests.Fireball_TargetsInRange_NotAdjacentToHimOrToHisFriendlyUnit",
                    Fireball, true),
                Coloured(GydionCardContent.LesserNecromancy, "Lesser Necromancy",
                    "GydionReuseSpellTests.VampiricTouch_AfterTheAttack_HeMayRetrieveADiscardedCard",
                    VampiricTouch, true),
                Coloured(GydionCardContent.GreaterEvocation, "Greater Evocation",
                    "GydionCountingSpellTests.Sunburst_WithNOthersRemoved_TargetsAtExactlyRangeN_WithAttackN",
                    Sunburst, true),
                Coloured(GydionCardContent.GreaterNecromancy, "Greater Necromancy",
                    "GydionImmunityDiscardLifeMoveSpellTests.EnergyDrain_AnEnemyInRangeDiscardsANonBasicCard_AndHisTeamRegainsASpentLife",
                    EnergyDrain, true),

                // ---- Blue: Enchantment and Transmutation ----
                Coloured(GydionCardContent.ElementaryEnchantment, "Elementary Enchantment",
                    "GydionDealingTests.TheBots_AnswerSuggestion_TheMovedHerosOwnSeatPickingTheDirection",
                    Suggestion, false),
                Coloured(GydionCardContent.LesserEnchantment, "Lesser Enchantment",
                    "GydionReuseSpellTests.DominatePerson_AMinionBesideTheHeroButOutOfRadius_DoesNotQualifyHim",
                    DominatePerson, true),
                Coloured(GydionCardContent.LesserTransmutation, "Lesser Transmutation",
                    "GydionCountingSpellTests.MidasTouch_GainsOneCoinForEveryTwoOthersRemoved_RoundedDown",
                    Transmutation, true),
                Coloured(GydionCardContent.GreaterEnchantment, "Greater Enchantment",
                    "GydionReuseSpellTests.PowerWordKill_DefeatsAnEnemyHeroInRadiusWithNoCardsInHand",
                    PowerWordKill, true),
                Coloured(GydionCardContent.GreaterTransmutation, "Greater Transmutation",
                    "GydionReuseSpellTests.Polymorph_SwapsAnEnemyHeroWithATokenOrAnEnemyMinionInRadius",
                    Polymorph, true),

                // ---- Green: Abjuration and Conjuration ----
                Coloured(GydionCardContent.ElementaryAbjuration, "Elementary Abjuration",
                    "GydionImmunityDiscardLifeMoveSpellTests.Shield_MakesHimImmuneToABasicAttack_ButNotToANonBasicOne",
                    Refill, false),
                Coloured(GydionCardContent.LesserAbjuration, "Lesser Abjuration",
                    "GydionReuseSpellTests.Banishment_PlacesAUnitOrATokenBesideHimIntoASpaceInRadius",
                    Banishment, true),
                Coloured(GydionCardContent.LesserConjuration, "Lesser Conjuration",
                    "GydionCountingSpellTests.FindFamiliar_RemovesUpToThreeOtherSpellsFaceup_WithoutCastingThem",
                    Familiar, true),
                Coloured(GydionCardContent.GreaterAbjuration, "Greater Abjuration",
                    "GydionImmunityDiscardLifeMoveSpellTests.Invulnerability_MakesHimImmuneToANonBasicAttack_ButNotToABasicOne",
                    Invulnerability, true),
                Coloured(GydionCardContent.GreaterConjuration, "Greater Conjuration",
                    "GydionImmunityDiscardLifeMoveSpellTests.CloudKill_AnEnemyHeroInRadiusDiscardsABasicCardOfHisOwnChoosing",
                    CloudKill, true),

                // ---- The twenty-two spells ----
                Spell(GydionCardContent.MagicMissile, "Magic Missile",
                    "GydionReuseSpellTests.MagicMissile_TargetsAUnitInRangeAndNotAdjacent_WithAttackOne",
                    Cast, RangedAttackTarget),
                Spell(GydionCardContent.ShockingGrasp, "Shocking Grasp",
                    "GydionReuseSpellTests.ShockingGrasp_TargetsAnAdjacentUnit_ThenGydionMovesTheTargetOneSpace",
                    ShockingGrasp, AdjacentAttackTarget, AfterAttackTextPause, BoardSpaceSelection),
                Spell(GydionCardContent.ExpeditiousRetreat, "Expeditious Retreat",
                    "GydionReuseSpellTests.ExpeditiousRetreat_IsAMovementOfFive_OnlyInAStraightLine",
                    ExpeditiousRetreat, PrimaryMovementDestination, BeforeMovementTextPause, ActiveEffectReading),
                Spell(GydionCardContent.BurningHands, "Burning Hands",
                    "GydionReuseSpellTests.BurningHands_WithTwoHeroesBesideTheTarget_GydionPicksTheOneWhoDiscards",
                    BurningHands, AdjacentAttackTarget, BeforeAttackTextPause, BoardUnitSelection, HandCardSelection),
                Spell(GydionCardContent.Fireball, "Fireball",
                    "GydionReuseSpellTests.Fireball_TargetsInRange_NotAdjacentToHimOrToHisFriendlyUnit",
                    Fireball, RangedAttackTarget),
                Spell(GydionCardContent.Sunburst, "Sunburst",
                    "GydionCountingSpellTests.Sunburst_WithNOthersRemoved_TargetsAtExactlyRangeN_WithAttackN",
                    Sunburst, RangedAttackTarget),
                Spell(GydionCardContent.VampiricTouch, "Vampiric Touch",
                    "GydionReuseSpellTests.VampiricTouch_AfterTheAttack_HeMayRetrieveADiscardedCard",
                    VampiricTouch, AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline, HandCardSelection),
                Spell(GydionCardContent.CreateUndead, "Create Undead",
                    "GydionReuseSpellTests.CreateUndead_RespawnsAFriendlyMinion_InAnEmptyFriendlySpawnPointInRange",
                    CreateUndead, BoardSpaceSelection),
                Spell(GydionCardContent.EnergyDrain, "Energy Drain",
                    "GydionImmunityDiscardLifeMoveSpellTests.EnergyDrain_AnEnemyInRangeDiscardsANonBasicCard_AndHisTeamRegainsASpentLife",
                    EnergyDrain, BoardUnitSelection, HandCardSelection),
                Spell(GydionCardContent.Suggestion, "Suggestion",
                    "GydionImmunityDiscardLifeMoveSpellTests.Suggestion_ADirectionTheBoardsEdgeStopsShort_IsNotOffered",
                    Suggestion, BoardUnitSelection, BoardSpaceSelection),
                Spell(GydionCardContent.DominatePerson, "Dominate Person",
                    "GydionReuseSpellTests.DominatePerson_AMinionBesideTheHeroButOutOfRadius_DoesNotQualifyHim",
                    DominatePerson, BoardUnitSelection),
                Spell(GydionCardContent.PowerWordKill, "Power Word Kill",
                    "GydionReuseSpellTests.PowerWordKill_DefeatsAnEnemyHeroInRadiusWithNoCardsInHand",
                    PowerWordKill, BoardUnitSelection),
                Spell(GydionCardContent.MidasTouch, "Midas' Touch",
                    "GydionCountingSpellTests.MidasTouch_GainsOneCoinForEveryTwoOthersRemoved_RoundedDown",
                    Transmutation),
                Spell(GydionCardContent.Disintegrate, "Disintegrate",
                    "GydionReuseSpellTests.Disintegrate_RemovesATokenOrAnEnemyNonHeavyMinionBesideHim",
                    Transmutation, BoardUnitSelection),
                Spell(GydionCardContent.Polymorph, "Polymorph",
                    "GydionReuseSpellTests.Polymorph_SwapsAnEnemyHeroWithATokenOrAnEnemyMinionInRadius",
                    Polymorph, BoardUnitSelection),
                Face(GydionCardContent.Shield, "Shield", new[]
                {
                    Prove(SpellCastFromTheBook, Refill), Prove(ActiveEffectReading, Refill),
                    Prove(RoundLongActiveEffectReading, ShieldImmunity)
                }, new[] { "GydionImmunityDiscardLifeMoveSpellTests.ARefillReturningShield_EndsItsImmunity_WhichOtherwiseRunsOnThroughTheRound" }),
                Spell(GydionCardContent.Banishment, "Banishment",
                    "GydionReuseSpellTests.Banishment_PlacesAUnitOrATokenBesideHimIntoASpaceInRadius",
                    Banishment, BoardUnitSelection, BoardSpaceSelection),
                Spell(GydionCardContent.Invulnerability, "Invulnerability",
                    "GydionImmunityDiscardLifeMoveSpellTests.Invulnerability_MakesHimImmuneToANonBasicAttack_ButNotToABasicOne",
                    Invulnerability, ActiveEffectReading, RoundLongActiveEffectReading),
                Spell(GydionCardContent.FindFamiliar, "Find Familiar",
                    "GydionCountingSpellTests.FindFamiliar_RemovesUpToThreeOtherSpellsFaceup_WithoutCastingThem",
                    Familiar, BoardSpaceSelection, SpellFaceChoice, ConfirmOrDecline, PublicCardRevealReading),
                Spell(GydionCardContent.DimensionDoor, "Dimension Door",
                    "GydionCountingSpellTests.DimensionDoor_WithNOthersRemoved_PlacesHimAtExactlyRadiusN",
                    DimensionDoor, BoardSpaceSelection),
                Spell(GydionCardContent.CloudKill, "Cloud Kill",
                    "GydionImmunityDiscardLifeMoveSpellTests.CloudKill_AnEnemyHeroInRadiusDiscardsABasicCardOfHisOwnChoosing",
                    CloudKill, BoardUnitSelection, HandCardSelection),
                Face(GydionCardContent.Wish, "Wish", new[]
                {
                    Prove(SpellCastFromTheBook, Wish), Prove(SpellFaceChoice, Wish),
                    Prove(PublicCardRevealReading, Wish), Prove(SpellCastFromTheBook, ThirdWish)
                }, new[] { "GydionWishTests.EachWishCast_AddsOneToTheCount" }),

                // ---- Components ----
                Component("gydion-piece", "Hero piece: his standee", new[]
                {
                    Prove(HeroPieceSpawnAndIdentity, Piece),
                    Prove(HeroPiecePointerSelection, Piece),
                    Prove(HeroPieceMovement, Movement),
                    Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                    Prove(PresentationRebuild, Lifecycle)
                }, new[] { "GydionDealingTests.SelectedGydion_DealsEighteenFrontsAndStartsAllTwentyTwoSpellsRemovedFaceup" }),
                Component("gydion-session", "Session continuity", new[]
                {
                    Prove(SnapshotRestoreContinuation, Restore),
                    Prove(RetainedPrefixUndo, Undo),
                    Prove(ReplayReconstruction, Undo),
                    Prove(RestartReconstruction, Lifecycle)
                }, new[] { "SpellCastTests.ACastChoice_RestoresMidChoice_AndItsAnswerCrossesTheWire" }),
                Component("gydion-footprint", "Every complete face: the portrait fronts and the landscape spells",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "GydionDealingTests.TheSetIsTheShardsNineteenFronts_AllComposed_UnderHisDealtRevision" }),
                Component("gydion-items", "Level-up sibling items",
                    new[] { Prove(ItemVisibilityAndUse, Upgrade) },
                    new[] { "GydionDealingTests.TheUpgradeLines_FollowEachSchoolsNameFamily" }),
                Component("gydion-spellbook", "The spellbook: the SPELLBOOK chip, the drawer and the faceup pile", new[]
                {
                    Prove(PublicCardRevealReading, Cast),
                    Prove(PublicCardRevealReading, Familiar),
                    Prove(PresentationRebuild, Restore)
                }, new[] { "SpellbookPresentationTests.TheItemsStrip_DrawsTheSpellbookChip_InBookAndRemoved_OnlyForAHeroWithABook_AndItOpensTheDrawer" }),
                Component("gydion-familiar", "The Familiar: one owl token, placed in radius", new[]
                {
                    Prove(BoardSpaceSelection, Familiar)
                }, new[] { "GydionCountingSpellTests.FindFamiliar_PlacesTheOneFamiliarInRadius_AnObstacleHeOwns" })
            });

        [Test] public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();
        [Test] public void EveryRowMatchesTheShapesDerivedFromContent() => Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();
        [Test] public void ReachableFacesHaveJourneysAndBoxedFacesHaveReasons() => Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();
        [Test] public void EveryShapeNamesItsOwnExistingJourney() => Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(typeof(GydionPlayerPathGateTests).Assembly);
        [Test] public void EveryNamedJourneyExists() => Matrix().AssertEveryNamedJourneyExists(typeof(GydionPlayerPathGateTests).Assembly);
        [Test] public void EveryGydionJourneyHasARow() => Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
            typeof(GydionUiJourneyPlayModeTests), typeof(GydionSpellUiJourneyPlayModeTests));
        [Test] public void EveryRequiredComponentShapeIsDeclared() => Matrix().AssertEveryRequiredComponentShapeIsDeclared();

        /// <summary>
        /// Every spell is a face with its own row, and every row of a spell owes the cast: a spell
        /// that derived no SpellCastFromTheBook would be a spell nobody had to cast in a journey.
        /// </summary>
        [Test]
        public void EverySpellDerivesTheCast_AndNoFrontDoes()
        {
            var matrix = Matrix();
            foreach (var definition in GydionCardContent.AllDefinitions)
            {
                var derived = matrix.ShapesIntroducedBy(definition);
                if (definition.Progression.IsSpell)
                {
                    Assert.That(derived, Has.Member(SpellCastFromTheBook), definition.DisplayName);
                    Assert.That(derived, Has.No.Member(CommitAndReveal), definition.DisplayName);
                    Assert.That(derived, Has.No.Member(PrimarySkillControl), definition.DisplayName);
                }
                else
                {
                    Assert.That(derived, Has.No.Member(SpellCastFromTheBook), definition.DisplayName);
                }
            }

            Assert.That(GydionCardContent.SpellDefinitions.Count, Is.EqualTo(22));
        }

        [Test]
        public void EveryAuthoritativeScenarioExists()
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
