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
    /// The permanent hero player-path completion gate, for Rowenna the Vanguard. Milestone 8.57h, and
    /// the EIGHTEENTH hero to take it.
    ///
    /// <para><b>Nothing below is a checklist.</b> Every row's shape set is DERIVED from her own printed
    /// content by <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> and compared against the row
    /// exactly, so a row that claims less has a shape nothing proves and a row that claims more is a
    /// coverage claim the content does not support. A shape cannot be declared without naming the
    /// [UnityTest] that drives it, and a journey in her fixtures that no row names fails too.</para>
    ///
    /// <para><b>She introduces no new interaction SHAPE</b>, which is the same finding Swift's,
    /// Cutter's and Widget's dealings recorded - her two registered capabilities are both Places and a
    /// Place has asked for one thing since milestone 6.6, and her three cross-seat prompts fall under
    /// milestone 8.40f's same-shape ruling: a different chooser answering the same kind of control is
    /// the same shape, asserted inside the journey rather than by a row.</para>
    ///
    /// <para><b>One CLAIM is widened, and it is hers alone.</b> Milestone 8.57f predicted that Fabled
    /// Lance would move her Attack faces from <c>AdjacentAttackTarget</c> to <c>RangedAttackTarget</c>,
    /// and measuring the derivation shows it cannot: the shape a face introduces is read off that
    /// face's own <c>ActionDefinition.AttackRange</c>, and 8.57g deliberately leaves that PRINTED on
    /// every Attack of hers because <c>ActionOffer</c> equality guards every authorization path. So the
    /// derivation now reads the Ultimate's own
    /// <c>RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange</c> and puts the ranged shape
    /// on <em>Fabled Lance's</em> row, where the sentence that causes it is printed. Every hero before
    /// her derives byte-identically, measured by deriving all eighteen composed corpora before and
    /// after the change: exactly one line moves and it is <c>rowenna-fabled-lance</c>.</para>
    /// </summary>
    public sealed class RowennaPlayerPathGateTests
    {
        // Her journeys, named once so a rename fails at compile time rather than in the guard.
        private const string Spawn = nameof(
            RowennaUiJourneyPlayModeTests.HerPieceSpawnsAsRowennaAndHerTrayCommitsAndRevealsACard);
        private const string Secondary = nameof(
            RowennaUiJourneyPlayModeTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string Chivalry = nameof(RowennaUiJourneyPlayModeTests
            .CodeOfChivalryOffersBothRetrievesOnAHeroTargetAndNeitherOnAMinion);
        private const string Gauntlet = nameof(RowennaUiJourneyPlayModeTests
            .ThrowTheGauntletPlacesHerBesideTheEnemyWhoThenOwnsTheMove);
        private const string Footprint = nameof(
            RowennaUiJourneyPlayModeTests.EveryRowennaFaceUsesTheStandardFootprint);

        private const string Reds = nameof(RowennaEngagedUiJourneyPlayModeTests
            .TheRewardRedsGiveTheChosenFriendlyHeroACoinOrTheirOwnRetrieval);
        private const string Paragons = nameof(RowennaEngagedUiJourneyPlayModeTests
            .TheParagonsRepeatOnADifferentTargetOnlyWhenTheRiderWasPerformed);
        private const string Swaps = nameof(RowennaEngagedUiJourneyPlayModeTests
            .EverySwapTakesEitherPrintedQualifierAndDefendTheInnocentRetrieves);
        private const string Surrender = nameof(RowennaEngagedUiJourneyPlayModeTests
            .AcceptSurrenderDefeatsAnEmptyHandedHeroAndGloriousTriumphTriplesTheAssist);
        private const string Ending = nameof(RowennaEngagedUiJourneyPlayModeTests
            .AcceptSurrenderSpendsTheLastLifeCounterAndEndsTheMatch);
        private const string Shots = nameof(RowennaEngagedUiJourneyPlayModeTests
            .OpeningShotsMakesTheNamedEnemyDiscardAndIsGreyedWhenEitherPileIsNot);

        private const string Greens = nameof(RowennaProgressedUiJourneyPlayModeTests
            .TheGreenModalsPlaceEitherMinionAfterTheMovementOrDecline);
        private const string Grand = nameof(
            RowennaProgressedUiJourneyPlayModeTests.GrandMeleeTakesBothPerformancesInOneTurn);
        private const string Volley = nameof(
            RowennaProgressedUiJourneyPlayModeTests.OpeningVolleyRepeatsAgainstASecondEligibleHero);
        private const string Lance = nameof(RowennaProgressedUiJourneyPlayModeTests
            .FabledLanceMakesHerAttacksRangedAtTwoAndARangedBlockAnswersThem);
        private const string Range = nameof(RowennaProgressedUiJourneyPlayModeTests
            .HerAttackControlNamesTheUltimatesPrintedRangeOnlyWhileItIsUnlocked);
        private const string Items = nameof(RowennaProgressedUiJourneyPlayModeTests
            .LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem);
        private const string Lifecycle = nameof(RowennaProgressedUiJourneyPlayModeTests
            .HerDefeatedPieceRespawnsAndRestartRebuildsTheSession);
        private const string Continuity = nameof(RowennaProgressedUiJourneyPlayModeTests
            .TheOpponentsOwnPausesSurviveUndoReplayAndSnapshotRestore);

        /// <summary>
        /// Every dealt face of hers prints p.14's secondary Defense shield, and fourteen of the
        /// eighteen print p.10's secondary Movement boot as well - the four that do not are RO-05's
        /// rule that a missing icon is absent rather than zero. One journey drives both halves, face by
        /// face, so this is the pair every row starts from.
        /// </summary>
        private static ShapeEvidence[] Ordinary(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryMovementDestination, Secondary),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        /// <summary>The same, for a face printing no Movement ribbon at all.</summary>
        private static ShapeEvidence[] NoBoot(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        private static readonly string[] Reward = { "RowennaRewardTests", "RowennaPrintedSetTests" };
        private static readonly string[] Guard =
            { "RowennaSwapAndGauntletTests", "RowennaPrintedSetTests" };
        private static readonly string[] Defeat =
            { "RowennaSurrenderTests", "RowennaPrintedSetTests" };
        private static readonly string[] Close =
            { "RowennaCloseQuartersTests", "RowennaPrintedSetTests" };
        private static readonly string[] Ultimate =
            { "RowennaFabledLanceTests", "RowennaPrintedSetTests" };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Rowenna",
            RowennaCardContent.AllDefinitions,
            RowennaCardContent.ReturnedToBoxAtSetup,

            // She registers no Active effect at all: not one face of hers prints "This turn:",
            // "Next turn:" or "End of turn:", which is what milestone 8.57g's dealing recorded as
            // the emptiest a dealing can be.
            Array.Empty<IEffectCapabilityHandler>(),

            // And no triggered Ultimate effect: Fabled Lance's UltimateRule.TriggeredEffect is null,
            // because a p.16 permanent ability that changes a RULE names no effect to run.
            Array.Empty<EffectDefinition>(),
            new[]
            {
                Face(
                    RowennaCardContent.CodeOfChivalry, "Code of Chivalry",
                    Ordinary(
                        Chivalry,
                        AdjacentAttackTarget, BeforeAttackTextPause, ConfirmOrDecline,
                        HandCardSelection),
                    Reward),

                Face(
                    RowennaCardContent.TokenOfGratitude, "Token of Gratitude",
                    Ordinary(Reds, AdjacentAttackTarget, AfterAttackTextPause, BoardUnitSelection),
                    Reward),
                Face(
                    RowennaCardContent.FairShare, "Fair Share",
                    Ordinary(Reds, AdjacentAttackTarget, AfterAttackTextPause, BoardUnitSelection),
                    Reward),
                Face(
                    RowennaCardContent.FeatOfBravery, "Feat of Bravery",
                    Ordinary(
                        Reds,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline,
                        BoardUnitSelection, HandCardSelection),
                    Reward),
                Face(
                    RowennaCardContent.ParagonOfGrace, "Paragon of Grace",
                    Ordinary(
                        Paragons,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline, RepeatDecision,
                        BoardUnitSelection),
                    Reward),
                Face(
                    RowennaCardContent.ParagonOfValor, "Paragon of Valor",
                    Ordinary(
                        Paragons,
                        AdjacentAttackTarget, AfterAttackTextPause, ConfirmOrDecline, RepeatDecision,
                        BoardUnitSelection, HandCardSelection),
                    Reward),

                // RO-05: her Silver basic prints no Movement ribbon icon at all.
                Face(
                    RowennaCardContent.ThrowTheGauntlet, "Throw the Gauntlet",
                    NoBoot(
                        Gauntlet,
                        PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection,
                        BoardSpaceSelection),
                    Guard),

                Face(
                    RowennaCardContent.StandGuard, "Stand Guard",
                    Ordinary(Swaps, PrimarySkillControl, BoardUnitSelection),
                    Guard),
                Face(
                    RowennaCardContent.ProtectTheWeak, "Protect the Weak",
                    Ordinary(Swaps, PrimarySkillControl, BoardUnitSelection),
                    Guard),
                Face(
                    RowennaCardContent.DefendTheInnocent, "Defend the Innocent",
                    Ordinary(
                        Swaps,
                        PrimarySkillControl, ConfirmOrDecline, BoardUnitSelection, HandCardSelection),
                    Guard),

                // Accept Surrender names TWO journeys for one shape on purpose: the same board click
                // is driven once for p.13's ordinary consequences and once on a team down to its last
                // Life counter, where it ends the match instead.
                Face(
                    RowennaCardContent.AcceptSurrender, "Accept Surrender",
                    Ordinary(Surrender, PrimarySkillControl, BoardUnitSelection)
                        .Concat(new[] { Prove(BoardUnitSelection, Ending) })
                        .ToArray(),
                    Defeat),
                Face(
                    RowennaCardContent.GloriousTriumph, "Glorious Triumph",
                    Ordinary(Surrender, PrimarySkillControl, BoardUnitSelection),
                    Defeat),

                // RO-05 again: the three Green Movement faces print a Defense shield and no boot.
                Face(
                    RowennaCardContent.CloseQuarters, "Close Quarters",
                    NoBoot(
                        Greens,
                        PrimaryMovementDestination, AfterMovementTextPause, ConfirmOrDecline,
                        ModalBulletChoice, BoardUnitSelection, BoardSpaceSelection),
                    Close),
                Face(
                    RowennaCardContent.Melee, "Melee",
                    NoBoot(
                        Greens,
                        PrimaryMovementDestination, AfterMovementTextPause, ConfirmOrDecline,
                        ModalBulletChoice, BoardUnitSelection, BoardSpaceSelection),
                    Close),
                Face(
                    RowennaCardContent.GrandMelee, "Grand Melee",
                    NoBoot(
                        Grand,
                        PrimaryMovementDestination, AfterMovementTextPause, ConfirmOrDecline,
                        ModalBulletChoice, BoardUnitSelection, BoardSpaceSelection),
                    Close),

                Face(
                    RowennaCardContent.OpeningShots, "Opening Shots",
                    Ordinary(Shots, PrimarySkillControl, BoardUnitSelection, HandCardSelection),
                    Close),
                Face(
                    RowennaCardContent.OpeningVolley, "Opening Volley",
                    Ordinary(
                        Volley,
                        PrimarySkillControl, ConfirmOrDecline, RepeatDecision, BoardUnitSelection,
                        HandCardSelection),
                    Close),

                // p.16's Ultimate prints no action at all, so it is committed to no turn slot and
                // reveals nothing: what it introduces is the rail a player reads it on, and the
                // ranged reach its own sentence gives every Attack she performs.
                Face(
                    RowennaCardContent.FabledLance, "Fabled Lance",
                    new[]
                    {
                        Prove(UltimateRailVisibility, Lance),
                        Prove(RangedAttackTarget, Lance),
                        Prove(RangedAttackTarget, Range)
                    },
                    Ultimate),

                UnreachableFace(
                    RowennaCardContent.EarnTheSpurs, "Earn the Spurs",
                    "Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player " +
                    "format. The 2026-08-21 user decision named that trigger for Wasp's Crackling " +
                    "Dagger and Arien's, Widget's, Swift's and Bain's boxed faces carry the same " +
                    "string."),

                Component(
                    "rowenna-piece", "Hero piece",
                    new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, Secondary),
                        Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                        Prove(PresentationRebuild, Lifecycle)
                    },
                    new[] { "RowennaDealingTests" }),

                Component(
                    "rowenna-session", "Session continuity",
                    new[]
                    {
                        Prove(SnapshotRestoreContinuation, Continuity),
                        Prove(RetainedPrefixUndo, Continuity),
                        Prove(ReplayReconstruction, Continuity),
                        Prove(RestartReconstruction, Lifecycle)
                    },
                    new[] { "RowennaSwapAndGauntletTests" }),

                Component(
                    "rowenna-footprint", "Complete faces",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "RowennaPrintedSetTests" }),

                Component(
                    "rowenna-items", "Earned items",
                    new[] { Prove(ItemVisibilityAndUse, Items) },
                    new[] { "RowennaPrintedSetTests" }),

                // Milestone 8.21a's non-required reading, carried because her content moves coins
                // more than any hero's before her: the gauntlet's conditional 2, the Reds' 1 to
                // somebody else, and p.13's assist coins tripled by one Tier III clause.
                Component(
                    "rowenna-coins", "Coin balance",
                    new[]
                    {
                        Prove(HeroCoinBalanceReading, Gauntlet),
                        Prove(HeroCoinBalanceReading, Reds),
                        Prove(HeroCoinBalanceReading, Surrender)
                    },
                    new[] { "RowennaRewardTests", "RowennaSurrenderTests" }),

                AbsentComponent(
                    "rowenna-markers", "Hero-owned markers",
                    "No Rowenna face gives, transfers or returns a rulebook p.18 marker.")
            });

        [Test]
        public void EveryFaceHasExactlyOneRow() => Matrix().AssertEveryFaceHasExactlyOneRow();

        [Test]
        public void EveryRowDeclaresTheShapesItsContentIntroduces() =>
            Matrix().AssertEveryRowDeclaresTheShapesItsContentIntroduces();

        [Test]
        public void ReachableFacesHaveJourneysAndTheBoxedFaceHasItsReason() =>
            Matrix().AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed();

        [Test]
        public void EveryShapeNamesAnExistingJourney() =>
            Matrix().AssertEveryRowsShapesAreProvedByItsOwnJourneys(
                typeof(RowennaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryNamedJourneyExists() =>
            Matrix().AssertEveryNamedJourneyExists(typeof(RowennaPlayerPathGateTests).Assembly);

        [Test]
        public void EveryJourneyInHerFixturesIsNamedByARow() =>
            Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(RowennaUiJourneyPlayModeTests),
                typeof(RowennaEngagedUiJourneyPlayModeTests),
                typeof(RowennaProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryRequiredComponentShapeIsDeclared() =>
            Matrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void HerMarkerAbsenceMatchesHerContent() =>
            Matrix().AssertNoContentNamesAnyCapability(
                new[]
                {
                    "bain-give-bounty-marker", "bain-transfer-bounty-marker",
                    "bain-return-bounty-marker", "tigerclaw-give-poison-marker-1",
                    "tigerclaw-give-poison-marker-2", "give-marker"
                },
                "Rowenna owns no marker.");

        /// <summary>
        /// Every authoritative scenario a row rests on is a real fixture in the Edit Mode assembly,
        /// which is what keeps the "source-backed" half of the gate honest: a journey proves the input
        /// route, and the fixture it names proves the rule.
        /// </summary>
        [Test]
        public void EveryAuthoritativeScenarioExists()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .Single(value => value.GetName().Name == "GuardsDigital.EditModeTests");
            foreach (var scenario in Matrix().Rows.SelectMany(row => row.AuthoritativeScenarios))
            {
                Assert.That(assembly.GetTypes().Any(type => type.Name == scenario), Is.True,
                    scenario + " is named by a row and is not a fixture in the Edit Mode assembly.");
            }
        }

        /// <summary>
        /// The count itself, pinned: nineteen printed faces, six hero-owned components, and every one
        /// of them accounted for. A nineteenth face or a twenty-sixth row fails here by name rather
        /// than by silence.
        /// </summary>
        [Test]
        public void HerTableCoversNineteenFacesAndSixComponents()
        {
            var matrix = Matrix();
            Assert.That(matrix.Rows.Count(row => !row.IsComponent), Is.EqualTo(19));
            Assert.That(matrix.Rows.Count(row => row.IsComponent), Is.EqualTo(6));
            Assert.That(matrix.AllIntroducedShapes(), Is.Not.Empty);
        }
    }
}
