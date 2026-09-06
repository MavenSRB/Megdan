using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z37. The session half of hero selection: the `/10` pick message, the
    /// refusals it can meet, what the roster then says, and the recomposition the transition fires.
    /// </summary>
    public sealed class PrivateSessionHeroSelectionTests
    {
        /// <summary>
        /// A composer that records what it was asked for and answers null, which is the seam's own
        /// "compose nothing" contract - so this fixture measures the DECISION without needing a
        /// scene to build a board.
        /// </summary>
        private sealed class RecordingComposer : IDeferredMatchComposition
        {
            public HeroLineup Requested { get; private set; }
            public int Calls { get; private set; }

            public IMatchHost ComposeForLineup(HeroLineup lineup)
            {
                Requested = lineup;
                Calls++;
                return null;
            }
        }

        /// <summary>A composer that hands back a whole other match, to exercise the rebind.</summary>
        private sealed class SwappingComposer : IDeferredMatchComposition
        {
            private readonly IMatchHost replacement;

            public SwappingComposer(IMatchHost replacement) => this.replacement = replacement;

            public IMatchHost ComposeForLineup(HeroLineup lineup) => replacement;
        }

        private static readonly string[] EverySeat =
        {
            "blue-player-01", "blue-player-02", "blue-player-03",
            "red-player-01", "red-player-02", "red-player-03"
        };

        [Test]
        public void AnAcceptedPick_ShowsInTheRosterAsChosen()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");

            Choose(driver, 1, "hero-sabina");

            var row = driver.Session.Roster.Single(entry => entry.Seat.Value == "red-player-01");
            Assert.That(row.Hero.Value, Is.EqualTo("hero-sabina"),
                "An accepted pick rides the roster row that already carried a hero id.");
            Assert.That(row.HasChosenHero, Is.True,
                "...and the row says it was CHOSEN, which is the fact a provisional hero cannot " +
                "be told from otherwise.");
        }

        /// <summary>
        /// The seat that has NOT picked still shows its provisional hero, and says so - which is the
        /// whole reason the flag exists.
        /// </summary>
        [Test]
        public void AnUnpickedSeat_ShowsItsProvisionalHeroAndSaysItWasNotChosen()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");

            var row = driver.Session.Roster.Single(entry => entry.Seat.Value == "red-player-02");
            Assert.That(row.Hero.Value, Is.EqualTo("hero-xargatha"));
            Assert.That(row.HasChosenHero, Is.False);
        }

        [Test]
        public void AnOutOfTurnPick_IsRefusedByNameAndKeepsTheConnection()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");
            driver.Seat(2, "red-player-02");

            // p.22's order opens on the coin's team - Red, per the test driver's composition - and
            // on that team's FIRST seat. red-player-02 is not it.
            Choose(driver, 2, "hero-sabina");

            Assert.That(driver.Outbox.RejectFor(2).Reason,
                Is.EqualTo(SessionRejectionReason.HeroSelectionRefused));
            Assert.That(driver.Outbox.WasClosed(2), Is.False,
                "A pick that arrived out of turn is a mistake, not an attack.");
            Assert.That(
                driver.Session.Roster.Single(row => row.Seat.Value == "red-player-02").HasChosenHero,
                Is.False);
        }

        [Test]
        public void ATakenHero_IsRefusedForEveryOtherSeatIncludingTheOtherTeam()
        {
            var driver = PrivateSessionTestDriver.Compose();
            SeatEveryone(driver);

            Choose(driver, 4, "hero-sabina");                // red-player-01, the coin's first seat
            Choose(driver, 1, "hero-sabina");                // blue-player-01, the other team

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.HeroSelectionRefused));
            Assert.That(
                driver.Session.Roster.Single(row => row.Seat.Value == "blue-player-01").HasChosenHero,
                Is.False,
                "One copy of each hero exists, so a hero taken on Red is not available on Blue.");
        }

        [Test]
        public void AnUnregisteredHero_IsRefusedWithoutClosingTheConnection()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");

            Choose(driver, 1, "hero-nobody");

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.HeroSelectionRefused));
            Assert.That(driver.Outbox.WasClosed(1), Is.False);
        }

        /// <summary>
        /// The published coin, which is what lets a client draw p.22's order before anybody has
        /// picked - the one case it cannot be inferred from the roster.
        /// </summary>
        [Test]
        public void TheWelcomeAndEveryRoster_CarryTheCoin()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);

            var welcome = driver.Outbox.FramesFor(1)
                .Single(frame => frame.Kind == SessionMessageKind.Welcome);
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeWelcome(welcome.Payload, out var decoded), Is.True);
            Assert.That(decoded.FirstPickingTeam, Is.EqualTo(TeamSide.Red),
                "The coin that orders the picking is the coin the composed match plays with.");

            driver.Claim(1, "red-player-01");
            var roster = driver.Outbox.FramesFor(1)
                .Last(frame => frame.Kind == SessionMessageKind.RosterUpdate);
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeRosterUpdate(roster.Payload, out var update),
                Is.True);
            Assert.That(update.FirstPickingTeam, Is.EqualTo(TeamSide.Red));
        }

        /// <summary>The `/10` frame round-trips, and its measured size is the survey's ~12 bytes.</summary>
        [Test]
        public void ThePickFrame_RoundTripsAndCostsWhatTheSurveyPredicted()
        {
            var frame = PrivateSessionFrameCodec.ChooseHeroFrame(
                new HeroChoiceMessage("hero-wasp"));
            var decoded = PrivateSessionTestDriver.Decode(frame);

            Assert.That(decoded.Kind, Is.EqualTo(SessionMessageKind.ChooseHero));
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeChooseHero(decoded.Payload, out var message),
                Is.True);
            Assert.That(message.HeroIdValue, Is.EqualTo("hero-wasp"));
            Assert.That(decoded.Payload.Length, Is.EqualTo(10),
                "One length-prefixed nine-character id: the ~12 bytes the wire survey predicted.");
        }

        /// <summary>
        /// EVERY declared message kind and EVERY declared refusal reason survives a decode.
        /// </summary>
        /// <remarks>
        /// <para>This exists because the decoder bounds its two enums by naming their LAST member,
        /// and nothing else in the build points at those two constants. Adding
        /// <c>ChooseHero</c> compiled, encoded, and was then thrown away as a malformed frame - the
        /// whole selection fixture went red on one stale line.</para>
        /// <para>It also caught a defect that was ALREADY SHIPPED: the refusal bound still read
        /// <c>ReconnectNotAuthorized</c> (21) while the ELEVENTH amendment's
        /// <c>MatchFormatMismatch</c> (22) sat past it, so a peer refused for composing a different
        /// printed setup had its reject frame discarded as unreadable by its own decoder. Enumerating
        /// the enums rather than spot-checking is what makes this non-vacuous for the NEXT member as
        /// well as this one.</para>
        /// </remarks>
        [Test]
        public void EveryDeclaredKindAndRefusal_SurvivesADecode()
        {
            foreach (SessionMessageKind kind in System.Enum.GetValues(typeof(SessionMessageKind)))
            {
                if (kind == SessionMessageKind.None)
                {
                    continue;
                }

                Assert.That(
                    PrivateSessionFrameCodec.TryReadMessageKind((int)kind, out var decoded), Is.True,
                    "Message kind " + kind + " is declared but the frame decoder refuses it.");
                Assert.That(decoded, Is.EqualTo(kind));
            }

            foreach (SessionRejectionReason reason in
                System.Enum.GetValues(typeof(SessionRejectionReason)))
            {
                if (reason == SessionRejectionReason.None)
                {
                    continue;
                }

                var frame = PrivateSessionTestDriver.Decode(
                    PrivateSessionFrameCodec.RejectFrame(new SessionRejectMessage(reason, "why")));
                Assert.That(
                    PrivateSessionFrameCodec.TryDecodeReject(frame.Payload, out var message),
                    Is.True,
                    "Refusal reason " + reason + " is declared but a peer cannot read it back, so " +
                    "a player refused for it is told the host sent something unreadable.");
                Assert.That(message.Reason, Is.EqualTo(reason));
            }
        }

        [Test]
        public void AMalformedPickPayload_IsRefused()
        {
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeChooseHero(new byte[] { 0x00 }, out _),
                Is.False, "An empty id is not a pick.");
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeChooseHero(null, out _), Is.False);
        }

        /// <summary>
        /// THE TRANSITION. Every seat picks, everyone readies, and the composer is handed exactly
        /// the line-up that was chosen - before the MatchStarted frame goes out.
        /// </summary>
        [Test]
        public void TheTransition_HandsTheComposerTheChosenLineup()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var composer = new RecordingComposer();
            driver.Session.Composition = composer;
            SeatEveryone(driver);
            PickInPrintedOrder(driver);
            ReadyEveryone(driver);

            Assert.That(driver.Session.HasMatchStarted, Is.True);
            Assert.That(composer.Calls, Is.EqualTo(1), "Composed once, at the one-way transition.");
            Assert.That(composer.Requested, Is.Not.Null);
            CollectionAssert.AreEqual(
                new[] { "hero-sabina", "hero-brogan", "hero-arien" },
                composer.Requested.Red.Select(hero => hero.HeroIdValue).ToArray(),
                "Red picked the Blue team's printed heroes, and the composer was told so.");
            CollectionAssert.AreEqual(
                new[] { "hero-wasp", "hero-xargatha", "hero-tigerclaw" },
                composer.Requested.Blue.Select(hero => hero.HeroIdValue).ToArray());
        }

        /// <summary>
        /// A session with NO composer transitions exactly as milestone 8.35a wrote it. This is the
        /// backward-compatibility guarantee, and it is why every fixture predating this milestone
        /// needed no edit.
        /// </summary>
        [Test]
        public void ASessionWithNoComposer_TransitionsUnchanged()
        {
            var driver = PrivateSessionTestDriver.Compose();
            SeatEveryone(driver);
            ReadyEveryone(driver);

            Assert.That(driver.Session.HasMatchStarted, Is.True);
            Assert.That(driver.Session.Roster.All(row => !row.HasChosenHero), Is.True);
        }

        /// <summary>
        /// THE GUARD that keeps the shipped default: nobody picked, so the composer is never called
        /// and the provisional - printed - match stands.
        /// </summary>
        /// <remarks>
        /// Filling every seat first-available instead would NOT reproduce the printed line-up: seats
        /// are ordered by player identity, which puts blue-player-01 first, so an empty selection
        /// filled in order would hand Blue the Red team's printed heroes.
        /// </remarks>
        [Test]
        public void WhenNobodyPicked_TheComposerIsNeverCalled()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var composer = new RecordingComposer();
            driver.Session.Composition = composer;
            SeatEveryone(driver);
            ReadyEveryone(driver);

            Assert.That(driver.Session.HasMatchStarted, Is.True);
            Assert.That(composer.Calls, Is.EqualTo(0),
                "A session where no hero was chosen deals setup step 7's printed line-up.");
        }

        /// <summary>
        /// A PARTIAL selection is completed deterministically rather than discarded, so the composed
        /// board cannot contradict the roster the players were reading.
        /// </summary>
        [Test]
        public void APartialSelection_IsFilledDeterministicallyRatherThanDiscarded()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var composer = new RecordingComposer();
            driver.Session.Composition = composer;
            SeatEveryone(driver);
            Choose(driver, 4, "hero-sabina");   // red-player-01 alone picks
            ReadyEveryone(driver);

            Assert.That(composer.Calls, Is.EqualTo(1));
            Assert.That(composer.Requested.Red.First().HeroIdValue, Is.EqualTo("hero-sabina"),
                "The one real pick survives.");
            Assert.That(
                composer.Requested.Red.Concat(composer.Requested.Blue)
                    .Select(hero => hero.HeroIdValue).Distinct().Count(),
                Is.EqualTo(6),
                "...and every filled seat obeys the one-copy rule the stage enforces.");
        }

        /// <summary>
        /// A pick after the transition is refused by name rather than silently changing a match
        /// already on the board.
        /// </summary>
        [Test]
        public void APickAfterTheTransition_IsRefusedByName()
        {
            var driver = PrivateSessionTestDriver.Compose();
            SeatEveryone(driver);
            ReadyEveryone(driver);

            Choose(driver, 4, "hero-sabina");

            Assert.That(driver.Outbox.RejectFor(4).Reason,
                Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
            Assert.That(driver.Outbox.WasClosed(4), Is.False);
        }

        /// <summary>
        /// THE SEAT GUARD: a composition that seats different seats is refused loudly rather than
        /// silently reattaching six live connections to other players' heroes.
        /// </summary>
        [Test]
        public void ARecompositionThatMovesASeat_IsRefusedLoudly()
        {
            var driver = PrivateSessionTestDriver.Compose();
            // A four-seat match is a genuinely different seat table, which is exactly what the
            // guard exists to catch - and cannot arise from a line-up, only from a broken composer.
            var fourSeat = MatchHost.Compose(FourSeatInitialState());
            driver.Session.Composition = new SwappingComposer(fourSeat);
            SeatEveryone(driver);
            PickInPrintedOrder(driver);

            Assert.Throws<System.InvalidOperationException>(() => ReadyEveryone(driver));
        }

        // ---- helpers ----

        /// <summary>
        /// A 2v2's four-seat match, used only to prove the seat guard fires. A line-up can never
        /// produce this - the format is settled before anybody is seated - so the guard is against a
        /// composer that built something other than what it was asked for.
        /// </summary>
        private static MatchSessionInitialState FourSeatInitialState()
        {
            var format = MatchFormat.TwoVersusTwoQuick;
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                format, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficial(format),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                System.Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }

        private static void Choose(PrivateSessionTestDriver driver, int ordinal, string heroId) =>
            driver.Submit(ordinal, PrivateSessionFrameCodec.ChooseHeroFrame(
                new HeroChoiceMessage(heroId)));

        private static void SeatEveryone(PrivateSessionTestDriver driver)
        {
            for (var index = 0; index < EverySeat.Length; index++)
            {
                driver.Seat(index + 1, EverySeat[index]);
            }
        }

        private static void ReadyEveryone(PrivateSessionTestDriver driver)
        {
            for (var ordinal = 1; ordinal <= EverySeat.Length; ordinal++)
            {
                driver.Submit(ordinal, PrivateSessionFrameCodec.SetReadinessFrame(
                    new SeatReadinessMessage(true)));
            }
        }

        /// <summary>
        /// Every seat picks, in p.22's own alternating order from the coin's team, each side taking
        /// the OTHER side's printed heroes - so the resulting line-up cannot be confused with the
        /// printed one at any point in the assertions above.
        /// </summary>
        private static void PickInPrintedOrder(PrivateSessionTestDriver driver)
        {
            // Ordinals follow EverySeat: 1..3 are blue-player-01..03, 4..6 are red-player-01..03.
            // The coin shows Red, so Red's first seat opens and the teams alternate.
            Choose(driver, 4, "hero-sabina");      // red-player-01
            Choose(driver, 1, "hero-wasp");        // blue-player-01
            Choose(driver, 5, "hero-brogan");      // red-player-02
            Choose(driver, 2, "hero-xargatha");    // blue-player-02
            Choose(driver, 6, "hero-arien");       // red-player-03
            Choose(driver, 3, "hero-tigerclaw");   // blue-player-03
        }
    }
}
