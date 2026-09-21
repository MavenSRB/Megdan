using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.PlayMode.PlayerInteractionShape;
using static GuardsDigital.Tests.PlayMode.PlayerPathRow;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The permanent hero player-path completion gate, for Hanu the Trickster. Milestone 8.71i, and the
    /// TWENTY-FIRST hero to take it.
    ///
    /// <para><b>Nothing below is a checklist.</b> Every row's shape set is DERIVED from his own printed
    /// content by <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> and compared against the row
    /// exactly, so a row that claims less has a shape nothing proves and a row that claims more is a
    /// coverage claim the content does not support. A shape cannot be declared without naming the
    /// [UnityTest] that drives it, and a journey in his fixtures that no row names fails too.</para>
    ///
    /// <para><b>He introduces NO new interaction shape</b>, which milestone 8.71h predicted while
    /// dealing him and this gate is the first to rest a table on - the third dealing in a row to add
    /// none, after Brynn's and Rowenna's. His three own contracts are either a question nobody is asked
    /// (<c>hanu-hurry-up-initiative@1</c> is the rewrite's VERB and <c>hanu-journey-partner@1</c> runs
    /// inside a triggered body where there is no acting player at all) or the board click this table has
    /// carried since milestone 8.15 (<c>hanu-friendly-unit-pair@1</c> takes two units where they stand,
    /// and a bound is not a second question).</para>
    ///
    /// <para><b>Five derivation readings are worth stating outright, because each one keeps a row
    /// HONEST rather than generous.</b></para>
    /// <list type="number">
    /// <item><description><b>Hurry up! declares no secondary Movement.</b> HA-06: it prints a Defense 1
    /// and no Movement ribbon whatsoever, and a missing icon is absent rather than zero - so it is the
    /// one dealt face of his whose row starts from <see cref="NoBoot"/>.</description></item>
    /// <item><description><b>The Ultimate declares only the rail.</b> The Ultimate Trick prints no
    /// Initiative, no Range, no Radius and no action, so p.7 never commits it and nothing is revealed;
    /// its permanent ability runs no effect at all - it is a standing declaration
    /// <c>DelegatedActionQuery</c> reads by rule kind - so it carries no
    /// <see cref="UltimateTriggerPause"/> and introduces <see cref="UltimateRailVisibility"/> and
    /// nothing else. What it DOES is read on the rows of the faces it re-homes, and it is journeyed
    /// there.</description></item>
    /// <item><description><b>The three journeys declare an Active-effect reading and no opponent
    /// restriction.</b> Their <c>EndOfTurn</c> occurrence is not <c>ThisTurn</c>, so the derivation adds
    /// <see cref="RoundLongActiveEffectReading"/> beside <see cref="ActiveEffectReading"/>; and p.18's
    /// immunity it carries is a rule about what may be done TO the swapped hero, which every consumer
    /// enforces by leaving that piece out of its candidates rather than by drawing a greyed control -
    /// the line milestone 8.20b drew for Wasp's Magnetic Dagger. The journey that drives each face
    /// reads the effect on the card that raised it and watches the candidate set close around the
    /// hero it names.</description></item>
    /// <item><description><b>No face of his opens a confirm-or-decline.</b> Every optional step he
    /// prints is either <c>if able</c> (Fight and Flight's straight line, both of its nodes) or a
    /// direct Move at <c>UpTo</c> (the two Greens' and Safe Travels' "Move up to 1 space"), and
    /// milestone 8.39j settled that both express their optionality inside their own offer rather than
    /// in a control beside it. A row claiming <see cref="ConfirmOrDecline"/> would name a button no
    /// player of his is ever shown.</description></item>
    /// <item><description><b>Safe Travels' row is There and Back Again's.</b> Its trailing move lives in
    /// the END-OF-TURN body (HA-Q8), and this derivation walks a face's own card text rather than the
    /// triggered effect an Active effect of its holds - so the move contributes no shape to the row.
    /// It is journeyed all the same, where a player meets it: at the turn's close, after the swap
    /// back.</description></item>
    /// </list>
    /// </summary>
    public sealed class HanuPlayerPathGateTests
    {
        // His journeys, named once so a rename fails at compile time rather than in the guard.
        private const string Spawn = nameof(
            HanuUiJourneyPlayModeTests.HisPieceSpawnsAsHanuAndHisTrayCommitsAndRevealsACard);
        private const string Secondary = nameof(
            HanuUiJourneyPlayModeTests.EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace);
        private const string HurryUpEnemy = nameof(HanuUiJourneyPlayModeTests
            .HurryUpRewritesAnEnemysCardToElevenAndWallsHisOwnRewind);
        private const string Gold = nameof(HanuUiJourneyPlayModeTests
            .FightAndFlightMovesHimThreeSpacesInALineOrDoesNothingAtAll);
        private const string HelpingHand = nameof(HanuUiJourneyPlayModeTests
            .HelpingHandTakesEitherPrintedBulletAndGreysTheOneNamingNobody);
        private const string UnexpectedJourney = nameof(HanuUiJourneyPlayModeTests
            .UnexpectedJourneySwapsWithTheEnemyWhoIsImmuneUntilTheSwapBack);
        private const string MonkeyTrick = nameof(HanuUiJourneyPlayModeTests
            .MonkeyTrickSwapsTwoFriendlyUnitsInsideItsPrintedRadius);
        private const string Footprint = nameof(
            HanuUiJourneyPlayModeTests.EveryHanuFaceUsesTheStandardFootprint);

        private const string EvenTheOdds = nameof(HanuEngagedUiJourneyPlayModeTests
            .EvenTheOddsReachesTheHeroBesideHisFriendlyHeroAtItsPrintedRangeFour);
        private const string Outnumber = nameof(HanuEngagedUiJourneyPlayModeTests
            .OutnumberTakesTheMinionBesideHisFriendlyHeroAndNeverTheHero);
        private const string ThereAndBackAgain = nameof(HanuEngagedUiJourneyPlayModeTests
            .ThereAndBackAgainSwapsAtItsWiderRadiusAndSwapsBackAtTheTurnsEnd);
        private const string ThisWay = nameof(HanuEngagedUiJourneyPlayModeTests
            .ThisWayLetsTheFriendChooseTheDistanceAndHanuTheDirection);
        private const string MonkeyTwist = nameof(HanuEngagedUiJourneyPlayModeTests
            .MonkeyTwistSwapsAFriendlyHeroAndAMinionAtItsWiderRadius);
        private const string HearNothing = nameof(HanuEngagedUiJourneyPlayModeTests
            .HearNothingSwapsWithTheEnemyAndLeavesNothingStanding);
        private const string HurryUpFriend = nameof(HanuEngagedUiJourneyPlayModeTests
            .HurryUpOnAFriendLeavesHisOwnRewindOpenAndItRestoresTheOrder);

        private const string Items = nameof(
            HanuProgressedUiJourneyPlayModeTests.LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem);
        private const string TrustedSidekick = nameof(HanuProgressedUiJourneyPlayModeTests
            .TrustedSidekickTakesBothPrintedBulletsInEitherOrder);
        private const string PileOn = nameof(HanuProgressedUiJourneyPlayModeTests
            .PileOnOffersOneMinionToBothBulletsAndBindsEachAfresh);
        private const string SafeTravels = nameof(HanuProgressedUiJourneyPlayModeTests
            .SafeTravelsMovesHimUpToOneSpaceAfterTheSwapBack);
        private const string ThatWay = nameof(HanuProgressedUiJourneyPlayModeTests
            .ThatWayOffersTheFriendEachOfItsThreePrintedDistances);
        private const string MonkeyBusiness = nameof(HanuProgressedUiJourneyPlayModeTests
            .MonkeyBusinessSwapsThePairAndThenMovesHimOrLeavesHim);
        private const string SeeNothing = nameof(HanuProgressedUiJourneyPlayModeTests
            .SeeNothingSwapsWithTheEnemyAndMovesFromTheExchangedSpace);
        private const string Ultimate = nameof(HanuProgressedUiJourneyPlayModeTests
            .TheUltimateTrickTakesTheNextActionOfAnEnemyAndOfAFriendFromHisOwnSeat);
        private const string Lifecycle = nameof(HanuProgressedUiJourneyPlayModeTests
            .HisDefeatedPieceRespawnsAndRestartRebuildsTheSession);
        private const string Continuity = nameof(HanuProgressedUiJourneyPlayModeTests
            .HisOwnPausesUndoAndTheReplayReproducesTheirOffers);
        private const string Restore = nameof(HanuProgressedUiJourneyPlayModeTests
            .HisOwnCrossSeatDistanceSurvivesASnapshotRestore);

        /// <summary>
        /// THE USER'S ONE NAMED REQUIREMENT FOR THIS HERO - "Make sure pick screen is correctly
        /// imeplented" - proved on the real selection screen at the rendered tier, which is a different
        /// scene from his match journeys and therefore a journey in the lobby's own fixture. His piece
        /// row names it beside the spawn journey: being the twenty-first PICK and standing at the
        /// configured spawn AS Hanu are two halves of one identity.
        /// </summary>
        private const string Picker = nameof(
            PrivateLobbyPlayModeTests.HanuIsFoundByHisThreeStarsAndEachOfHisThreeRoles);

        /// <summary>
        /// Seventeen of his nineteen faces print p.10's secondary Movement boot AND p.14's secondary
        /// Defense shield, so one journey drives both halves face by face and this is the pair those
        /// rows start from.
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

        /// <summary>
        /// HA-06: Hurry up! prints a Defense 1 and NO Movement ribbon at all. A missing icon is absent,
        /// never zero, and it is the only face of his the split touches.
        /// </summary>
        private static ShapeEvidence[] NoBoot(string ownJourney, params PlayerInteractionShape[] own) =>
            new[]
                {
                    Prove(CommitAndReveal, ownJourney),
                    Prove(SecondaryDefenseOffer, Secondary)
                }
                .Concat(own.Select(shape => Prove(shape, ownJourney)))
                .ToArray();

        private static readonly string[] HurryUpScenarios =
            { "HanuHurryUpTests", "HanuPrintedSetTests" };
        private static readonly string[] RedScenarios =
            { "HanuRedsTests", "HanuPrintedSetTests" };
        private static readonly string[] JourneyScenarios =
            { "HanuJourneyTests", "HanuPrintedSetTests" };
        private static readonly string[] CoMoveScenarios =
            { "HanuCoMoveTests", "HanuDealingTests", "HanuPrintedSetTests" };
        private static readonly string[] GreenScenarios =
            { "HanuGreensTests", "HanuPrintedSetTests" };
        private static readonly string[] UltimateScenarios =
            { "HanuUltimateTrickTests", "HanuDealingTests" };

        private static HeroPlayerPathMatrix Matrix() => new HeroPlayerPathMatrix(
            "Hanu",
            HanuCardContent.AllDefinitions,
            HanuCardContent.ReturnedToBoxAtSetup,

            // His registered handlers, handed in because the matrix resolves a capability to an Active
            // effect through the handler that creates it. His three Blue journeys each create one, and
            // they are the first Hero-scoped occurrences in this build.
            HanuComponentContent.Handlers(),

            // And the triggered effects the build's catalog holds for his ULTIMATE, which is none: The
            // Ultimate Trick is a permanent DECLARATION rather than a trigger, so there is no effect
            // for the derivation to walk and no `UltimateTriggerPause` on his Purple's row. The three
            // end-of-turn bodies his journeys arm are held by their Active effects and reached through
            // those instead.
            Array.Empty<GuardsDigital.Core.Effects.EffectDefinition>(),
            new[]
            {
                // ---------------------------------------------------------------------------------
                // The two basics
                // ---------------------------------------------------------------------------------

                // His Gold prints no Range icon at all, so p.13 reads it as adjacency by rule, and its
                // whole text is p.14's step-7 rider: the straight line is a space clicked on the board
                // and the Move that consumes it names its own destination, so the row carries one
                // board-space selection rather than two.
                Face(
                    HanuCardContent.FightAndFlight, "Fight and Flight",
                    Ordinary(
                        Gold, AdjacentAttackTarget, AfterAttackTextPause, BoardSpaceSelection),
                    RedScenarios),

                // His Silver: one board click for the hero whose card is rewritten, and the rewrite
                // itself asks nobody anything. Both halves of milestone 8.71a's Undo pair are named
                // here, because the wall the rewrite raises is a fact about THIS face.
                Face(
                    HanuCardContent.HurryUp, "Hurry up!",
                    NoBoot(HurryUpEnemy, PrimarySkillControl, BoardUnitSelection)
                        .Concat(new[]
                        {
                            Prove(PrimarySkillControl, HurryUpFriend),
                            Prove(BoardUnitSelection, HurryUpFriend)
                        })
                        .ToArray(),
                    HurryUpScenarios),

                // ---------------------------------------------------------------------------------
                // The five Reds: one modal, at one bullet or at two
                // ---------------------------------------------------------------------------------

                Face(
                    HanuCardContent.HelpingHand, "Helping Hand",
                    Ordinary(
                        HelpingHand,
                        RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice),
                    RedScenarios),
                Face(
                    HanuCardContent.EvenTheOdds, "Even the Odds",
                    Ordinary(
                        EvenTheOdds,
                        RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice),
                    RedScenarios),
                Face(
                    HanuCardContent.Outnumber, "Outnumber",
                    Ordinary(
                        Outnumber,
                        RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice),
                    RedScenarios),
                Face(
                    HanuCardContent.TrustedSidekick, "Trusted Sidekick",
                    Ordinary(
                        TrustedSidekick,
                        RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice),
                    RedScenarios),
                Face(
                    HanuCardContent.PileOn, "Pile On",
                    Ordinary(
                        PileOn,
                        RangedAttackTarget, BeforeAttackTextPause, ModalBulletChoice),
                    RedScenarios),

                // ---------------------------------------------------------------------------------
                // The five Blues: three journeys and two co-moves
                // ---------------------------------------------------------------------------------

                Face(
                    HanuCardContent.UnexpectedJourney, "Unexpected Journey",
                    Ordinary(
                        UnexpectedJourney,
                        PrimarySkillControl, BoardUnitSelection, ActiveEffectReading,
                        RoundLongActiveEffectReading),
                    JourneyScenarios),
                Face(
                    HanuCardContent.ThereAndBackAgain, "There and Back Again",
                    Ordinary(
                        ThereAndBackAgain,
                        PrimarySkillControl, BoardUnitSelection, ActiveEffectReading,
                        RoundLongActiveEffectReading),
                    JourneyScenarios),
                Face(
                    HanuCardContent.SafeTravels, "Safe Travels",
                    Ordinary(
                        SafeTravels,
                        PrimarySkillControl, BoardUnitSelection, ActiveEffectReading,
                        RoundLongActiveEffectReading),
                    JourneyScenarios),

                Face(
                    HanuCardContent.ThisWay, "This Way!",
                    Ordinary(
                        ThisWay,
                        PrimarySkillControl, BoardUnitSelection, ModalBulletChoice,
                        BoardSpaceSelection),
                    CoMoveScenarios),
                Face(
                    HanuCardContent.ThatWay, "That Way!",
                    Ordinary(
                        ThatWay,
                        PrimarySkillControl, BoardUnitSelection, ModalBulletChoice,
                        BoardSpaceSelection),
                    CoMoveScenarios),

                // ---------------------------------------------------------------------------------
                // The five Greens: three friendly pairs and two enemy swaps
                // ---------------------------------------------------------------------------------

                Face(
                    HanuCardContent.MonkeyTrick, "Monkey Trick",
                    Ordinary(MonkeyTrick, PrimarySkillControl, BoardUnitSelection),
                    GreenScenarios),
                Face(
                    HanuCardContent.MonkeyTwist, "Monkey Twist",
                    Ordinary(MonkeyTwist, PrimarySkillControl, BoardUnitSelection),
                    GreenScenarios),
                Face(
                    HanuCardContent.MonkeyBusiness, "Monkey Business",
                    Ordinary(
                        MonkeyBusiness,
                        PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection),
                    GreenScenarios),

                Face(
                    HanuCardContent.HearNothing, "Hear Nothing",
                    Ordinary(HearNothing, PrimarySkillControl, BoardUnitSelection),
                    GreenScenarios),
                Face(
                    HanuCardContent.SeeNothing, "See Nothing",
                    Ordinary(
                        SeeNothing,
                        PrimarySkillControl, BoardUnitSelection, BoardSpaceSelection),
                    GreenScenarios),

                // ---------------------------------------------------------------------------------
                // p.16's Ultimate, and p.20's Handicap
                // ---------------------------------------------------------------------------------

                // The Ultimate Trick prints no action at all, so it is committed to no turn slot and
                // reveals nothing, and its permanent ability runs no effect to pause on. What it
                // introduces is the rail a player reads it on; what it DOES is read on the seat that
                // answers the next action of the hero his Silver rewrote, and the Ultimate journey
                // drives that on both sides of the table.
                Face(
                    HanuCardContent.TheUltimateTrick, "The Ultimate Trick",
                    new[] { Prove(UltimateRailVisibility, Ultimate) },
                    UltimateScenarios),

                UnreachableFace(
                    HanuCardContent.FightOrFlight, "Fight or Flight",
                    "Rulebook p.6 returns the p.20 Handicap to the box; no configured odd-player " +
                    "format. The 2026-08-21 user decision named that trigger for Wasp's Crackling " +
                    "Dagger and Arien's, Widget's, Swift's, Bain's, Rowenna's, Min's and Brynn's " +
                    "boxed faces carry the same string."),

                // ---------------------------------------------------------------------------------
                // His own components
                // ---------------------------------------------------------------------------------

                Component(
                    "hanu-piece", "Hero piece",
                    new[]
                    {
                        Prove(HeroPieceSpawnAndIdentity, Spawn),
                        Prove(HeroPieceSpawnAndIdentity, Picker),
                        Prove(HeroPiecePointerSelection, Spawn),
                        Prove(HeroPieceMovement, Secondary),
                        Prove(HeroPieceDefeatAndRespawn, Lifecycle),
                        Prove(PresentationRebuild, Lifecycle)
                    },
                    new[] { "HanuDealingTests" }),

                Component(
                    "hanu-session", "Session continuity",
                    new[]
                    {
                        Prove(SnapshotRestoreContinuation, Restore),
                        Prove(RetainedPrefixUndo, Continuity),
                        Prove(ReplayReconstruction, Continuity),
                        Prove(RestartReconstruction, Lifecycle)
                    },
                    new[] { "HanuCoMoveTests", "HanuRedsTests" }),

                Component(
                    "hanu-footprint", "Complete faces",
                    new[] { Prove(StandardCardFootprintConformance, Footprint) },
                    new[] { "HanuPrintedSetTests" }),

                Component(
                    "hanu-items", "Earned items",
                    new[] { Prove(ItemVisibilityAndUse, Items) },
                    new[] { "HanuPrintedSetTests" }),

                AbsentComponent(
                    "hanu-markers", "Hero-owned markers",
                    "No Hanu face gives, transfers or returns a rulebook p.18 marker. What his set " +
                    "changes is where the pieces already on the board are standing, which card is " +
                    "next to act, and whose seat answers it."),

                AbsentComponent(
                    "hanu-tokens", "Placed tokens",
                    "He places nothing. Milestone 8.71's inventory found no token and no marker " +
                    "named anywhere in his nineteen faces, his profile or the Tabletopia manifest - " +
                    "his standee is the only piece of his in the box.")
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
                typeof(HanuPlayerPathGateTests).Assembly);

        [Test]
        public void EveryNamedJourneyExists() =>
            Matrix().AssertEveryNamedJourneyExists(typeof(HanuPlayerPathGateTests).Assembly);

        [Test]
        public void EveryJourneyInHisFixturesIsNamedByARow() =>
            Matrix().AssertEveryJourneyInFixturesIsNamedByARow(
                typeof(HanuUiJourneyPlayModeTests),
                typeof(HanuEngagedUiJourneyPlayModeTests),
                typeof(HanuProgressedUiJourneyPlayModeTests));

        [Test]
        public void EveryRequiredComponentShapeIsDeclared() =>
            Matrix().AssertEveryRequiredComponentShapeIsDeclared();

        [Test]
        public void HisMarkerAndTokenAbsenceMatchesHisContent() =>
            Matrix().AssertNoContentNamesAnyCapability(
                new[]
                {
                    "bain-give-bounty-marker", "bain-transfer-bounty-marker",
                    "bain-return-bounty-marker", "tigerclaw-give-poison-marker-1",
                    "tigerclaw-give-poison-marker-2", "give-marker",
                    "synthetic-place", "synthetic-place-in-range", "synthetic-remove"
                },
                "Hanu owns no marker and places no token.");

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
        /// of them accounted for. A twentieth face or a seventh component row fails here by name rather
        /// than by silence.
        /// </summary>
        [Test]
        public void HisTableCoversNineteenFacesAndSixComponents()
        {
            var matrix = Matrix();
            Assert.That(matrix.Rows.Count(row => !row.IsComponent), Is.EqualTo(19));
            Assert.That(matrix.Rows.Count(row => row.IsComponent), Is.EqualTo(6));
            Assert.That(matrix.AllIntroducedShapes(), Is.Not.Empty);
        }

        /// <summary>
        /// The claim milestone 8.71h made and this gate is the first to rest a table on: he introduces
        /// NO shape this table did not already carry, and the shapes he DOES raise are exactly these.
        /// It is pinned where a later reader will look for it, because "the twenty-first hero needed no
        /// new vocabulary" is a fact about the engine rather than about him - and a later refinement
        /// that quietly gave one of his contracts a control would fail here rather than in a paragraph.
        /// </summary>
        [Test]
        public void HeIntroducesNoShapeTheTableDidNotAlreadyCarry()
        {
            var his = Matrix().AllIntroducedShapes();
            Assert.That(
                his,
                Is.SupersetOf(new[]
                {
                    CommitAndReveal, AdjacentAttackTarget, RangedAttackTarget, PrimarySkillControl,
                    SecondaryMovementDestination, SecondaryDefenseOffer, BeforeAttackTextPause,
                    AfterAttackTextPause, BoardUnitSelection, BoardSpaceSelection,
                    ModalBulletChoice, ActiveEffectReading, RoundLongActiveEffectReading,
                    UltimateRailVisibility
                }),
                "His set asks for every one of these, and each is a shape this table carried before " +
                "him. Derived: " + string.Join(", ", his.Select(shape => shape.ToString())));

            Assert.That(
                his.Intersect(new[]
                {
                    ConfirmOrDecline, RepeatDecision, HandCardSelection, PushDistanceChoice,
                    ResourceAmountChoice, DirectionChoice, ColorNamingChoice, HeldMarkerReading,
                    PublicCardRevealReading, FacedownTokenReading, UltimateTriggerPause,
                    OpponentRestrictionReading, ActionTextPlacementChoice, BeforeMovementTextPause,
                    AfterMovementTextPause, DefenseTextApplication, PrimaryDefenseOffer,
                    BlockDefenseOffer, PrimaryMovementDestination,
                    MinionDefenseSuppressionReading, RespawnedMinionTypeChoice
                }),
                Is.Empty,
                "He holds no marker, places no token, reveals no card, pushes nobody, transfers no " +
                "coin, names no direction and no colour, prints no Block and no primary Defense, " +
                "prints no Movement primary and no Movement card text, repeats nothing, spends no " +
                "card out of a hand, and opens no confirm-or-decline at all - every optional step " +
                "he prints is `if able` or a direct Move whose nought is inside its own offer. And " +
                "his Ultimate is a standing declaration rather than a trigger. Derived: " +
                string.Join(", ", his.Select(shape => shape.ToString())));
        }
    }
}
