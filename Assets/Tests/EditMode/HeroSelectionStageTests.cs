using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z36. The hero-selection stage: what it offers, what it refuses, and
    /// the line-up it hands the match.
    /// </summary>
    /// <remarks>
    /// Unity-free on purpose, so the whole of it runs in the cross-platform harness in seconds.
    /// Nothing here composes a scene: the stage names no engine type, which is milestone 8.35a's
    /// law and is also what makes these assertions cheap enough to be exhaustive.
    /// </remarks>
    public sealed class HeroSelectionStageTests
    {
        private static readonly string[] ShippedHeroIds =
        {
            "hero-wasp", "hero-xargatha", "hero-tigerclaw",
            "hero-arien", "hero-brogan", "hero-sabina"
        };

        private static HeroSelectionSeat Seat(string id, TeamSide side) =>
            new HeroSelectionSeat(new SeatId(id), side);

        /// <summary>Three seats a side, in lobby order, Red first - the shipped six-seat topology.</summary>
        private static IReadOnlyList<HeroSelectionSeat> SixSeats() => new[]
        {
            Seat("seat-red-1", TeamSide.Red),
            Seat("seat-red-2", TeamSide.Red),
            Seat("seat-red-3", TeamSide.Red),
            Seat("seat-blue-1", TeamSide.Blue),
            Seat("seat-blue-2", TeamSide.Blue),
            Seat("seat-blue-3", TeamSide.Blue)
        };

        // ---------------------------------------------------------------------------------------
        // The registered roster is read, never counted.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The roster a selector offers IS the build's own dealing table, so a hero dealt in a later
        /// milestone appears in every selector without an edit here or in the stage.
        /// </summary>
        /// <remarks>
        /// This deliberately asserts no COUNT. A fixture that pinned "six" would have to be edited
        /// by every dealing milestone, and an assertion that has to be edited to stay true is not
        /// protecting anything. What is pinned is the relationship: the roster is the table.
        /// </remarks>
        [Test]
        public void TheRegisteredRosterIsTheBuildsOwnDealingTable()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;

            Assert.That(
                roster, Is.SameAs(QuickGameSetupConfiguration.RegisteredHeroes),
                "The selector must offer the dealing table itself, not a copy that can drift.");

            foreach (var heroId in ShippedHeroIds)
            {
                Assert.That(
                    roster.Any(hero => hero.HeroIdValue == heroId), Is.True,
                    $"'{heroId}' is dealt by this build, so a selector must offer it.");
            }

            Assert.That(
                roster.Select(hero => hero.HeroIdValue).Distinct().Count(), Is.EqualTo(roster.Count),
                "One copy of each hero exists, so the registration names each one once.");

            foreach (var hero in roster)
            {
                Assert.That(hero.ContentId.IsDefined, Is.True);
                Assert.That(hero.DisplayName, Is.Not.Empty);
            }
        }

        /// <summary>
        /// A hero the registration gains is offered by the selector with no edit to the selector -
        /// which is the actual requirement, stated as a test rather than as a comment.
        /// </summary>
        /// <remarks>
        /// <para>Driven by handing the stage a registration that contains a hero this build does
        /// not deal today. That stands in for the roster expansion: when Dodger's dealing milestone
        /// adds his row, the live registration grows exactly this way and every assertion below is
        /// what will then be true of him - offered, pickable, refused as a duplicate afterwards.</para>
        /// <para>This is the case that would fail if anything in the stage counted the roster,
        /// cached it at construction, or hard-coded six or twelve.</para>
        /// </remarks>
        [Test]
        public void AHeroTheRegistrationGainsIsOfferedWithNoEditToTheSelector()
        {
            var tomorrow = RegisteredHeroRoster.ForThisBuild
                .Concat(new[]
                {
                    new RegisteredHero(
                        "hero-dodger", new HeroContentId("dodger"), "Dodger the Warlock")
                })
                .ToArray();

            var stage = new HeroSelectionStage(
                SixSeats(), TeamSide.Red, HeroSelectionMethod.AllPick, tomorrow);

            Assert.That(
                stage.AvailableHeroes.Any(hero => hero.HeroIdValue == "hero-dodger"), Is.True,
                "A newly dealt hero is offered the moment the registration names it.");

            Assert.That(
                stage.TryChoose(new SeatId("seat-red-1"), "hero-dodger", out var refusal), Is.True,
                $"... and is pickable: {refusal}.");

            Assert.That(
                stage.TryChoose(new SeatId("seat-blue-1"), "hero-dodger", out refusal), Is.False,
                "... and the one-copy rule applies to him exactly as to the shipped six.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.HeroAlreadyTaken));

            Assert.That(
                stage.AvailableHeroes.Count, Is.EqualTo(tomorrow.Length - 1),
                "The available list is the registration minus what is taken - derived, not counted.");
        }

        /// <summary>
        /// A hero the build cannot deal is refused, so a selection screen can never seat one.
        /// </summary>
        [Test]
        public void AnUnregisteredHeroIsRefused()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);

            Assert.That(
                stage.TryChoose(new SeatId("seat-red-1"), "hero-dodger", out var refusal),
                Is.False);
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.UnregisteredHero));
        }

        // ---------------------------------------------------------------------------------------
        // ONE COPY OF EACH HERO EXISTS. The named regression.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The duplicate refusal the follow-up queue names: the same hero can never end up on both
        /// teams, and never on two seats of one team.
        /// </summary>
        /// <remarks>
        /// <para>Asserted at BOTH doors on purpose. The stage refuses it, so a player is told at
        /// pick time; and <see cref="HeroLineup"/> refuses it again, so no other path into a
        /// composed board - the bot filler, a launch flag, a fixture - can produce what the stage
        /// refuses. A rule enforced in one place only is a rule the next caller does not have.</para>
        /// </remarks>
        [Test]
        public void TheSameHeroCanNeverEndUpOnBothTeamsOrOnTwoSeats()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);

            Assert.That(
                stage.TryChoose(new SeatId("seat-red-1"), "hero-wasp", out _), Is.True,
                "The first pick of the Tie Breaker team is legal.");

            // The other team's first pick: Wasp is taken, so she is not available to anyone.
            Assert.That(
                stage.TryChoose(new SeatId("seat-blue-1"), "hero-wasp", out var acrossTeams),
                Is.False,
                "A hero another player holds is not available - not even to the other team.");
            Assert.That(acrossTeams, Is.EqualTo(HeroSelectionRefusal.HeroAlreadyTaken));

            Assert.That(
                stage.AvailableHeroes.Any(hero => hero.HeroIdValue == "hero-wasp"), Is.False,
                "A taken hero leaves the available list for every seat.");

            // ... and the same refusal inside one team.
            Assert.That(stage.TryChoose(new SeatId("seat-blue-1"), "hero-arien", out _), Is.True);
            Assert.That(stage.TryChoose(new SeatId("seat-red-2"), "hero-xargatha", out _), Is.True);
            Assert.That(
                stage.TryChoose(new SeatId("seat-blue-2"), "hero-arien", out var sameTeam),
                Is.False);
            Assert.That(sameTeam, Is.EqualTo(HeroSelectionRefusal.HeroAlreadyTaken));

            // The second door: a line-up naming one hero twice is refused outright, whichever
            // sides it puts the two copies on.
            var wasp = RegisteredHeroRoster.ForThisBuild.Single(h => h.HeroIdValue == "hero-wasp");
            var arien = RegisteredHeroRoster.ForThisBuild.Single(h => h.HeroIdValue == "hero-arien");

            Assert.That(
                () => new HeroLineup(new[] { wasp }, new[] { wasp }),
                Throws.ArgumentException,
                "One hero on both teams is refused at the composition door too.");
            Assert.That(
                () => new HeroLineup(new[] { wasp, wasp }, new[] { arien, arien }),
                Throws.ArgumentException,
                "One hero on two seats of one team is refused the same way.");
        }

        // ---------------------------------------------------------------------------------------
        // Rulebook p.22's printed All Pick order.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// p.22: All Pick starts "with the team showing on the Tie Breaker coin and alternating
        /// between teams". Asserted from both coin faces, because the coin is what decides it.
        /// </summary>
        /// <remarks>
        /// The printed rule constrains the TEAM alternation and says nothing about which player of
        /// a team goes first. That unprinted half is stated once as lobby seat order, on the same
        /// ruling milestone 8.25 made about which dashboard goes to which player and 8.35z16 made
        /// about which marked point a short team uses.
        /// </remarks>
        [Test]
        public void TheAllPickOrderStartsWithTheTieBreakerTeamAndAlternates()
        {
            foreach (var coin in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var stage = new HeroSelectionStage(SixSeats(), coin);
                var expected = coin == TeamSide.Red
                    ? new[]
                    {
                        "seat-red-1", "seat-blue-1", "seat-red-2",
                        "seat-blue-2", "seat-red-3", "seat-blue-3"
                    }
                    : new[]
                    {
                        "seat-blue-1", "seat-red-1", "seat-blue-2",
                        "seat-red-2", "seat-blue-3", "seat-red-3"
                    };

                var available = RegisteredHeroRoster.ForThisBuild.ToList();
                for (var pick = 0; pick < expected.Length; pick++)
                {
                    Assert.That(
                        stage.PickingSeat.Value, Is.EqualTo(expected[pick]),
                        $"Coin {coin}, pick {pick + 1}: p.22 alternates from the coin's team.");

                    Assert.That(
                        stage.TryChoose(
                            stage.PickingSeat, available[pick].HeroIdValue, out var refusal),
                        Is.True,
                        $"Coin {coin}, pick {pick + 1} refused: {refusal}.");
                }

                Assert.That(
                    stage.PickingSeat.IsDefined, Is.False,
                    "Once every seat holds a hero the order is on nobody.");
                Assert.That(stage.IsComplete, Is.True);
            }
        }

        /// <summary>
        /// A seat that picks out of turn is refused by name, so the printed order is the order and
        /// not a suggestion the fastest clicker can skip.
        /// </summary>
        [Test]
        public void APickOutOfTurnIsRefused()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);

            Assert.That(
                stage.TryChoose(new SeatId("seat-blue-1"), "hero-arien", out var refusal),
                Is.False,
                "The coin showed Red, so Blue does not pick first.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.NotThisSeatsTurn));

            Assert.That(
                stage.TryChoose(new SeatId("seat-red-2"), "hero-xargatha", out refusal), Is.False,
                "Inside a team the order is lobby order, so the second seat waits for the first.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.NotThisSeatsTurn));
        }

        /// <summary>A seat that is not in this session is refused rather than silently ignored.</summary>
        [Test]
        public void AnUnknownSeatIsRefused()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);
            Assert.That(
                stage.TryChoose(new SeatId("seat-green-1"), "hero-wasp", out var refusal),
                Is.False);
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.UnknownSeat));
        }

        // ---------------------------------------------------------------------------------------
        // The host's confirm, and the one-way transition it carries.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The confirm is the host's, needs a complete selection, and is one-way - exactly as the
        /// lobby's own transition into the match is.
        /// </summary>
        [Test]
        public void ConfirmingIsHostOnlyCompleteAndOneWay()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);

            Assert.That(stage.TryConfirm(true, out _, out var refusal), Is.False);
            Assert.That(
                refusal, Is.EqualTo(HeroSelectionRefusal.SelectionIncomplete),
                "Nothing is confirmed while a seat still owes a pick.");

            FillEverySeat(stage);

            Assert.That(stage.TryConfirm(false, out _, out refusal), Is.False);
            Assert.That(
                refusal, Is.EqualTo(HeroSelectionRefusal.NotTheHost),
                "A seat cannot confirm the table's line-up.");

            Assert.That(stage.TryConfirm(true, out var lineup, out refusal), Is.True);
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.None));
            Assert.That(lineup, Is.Not.Null);
            Assert.That(lineup.HeroesPerTeam, Is.EqualTo(3));

            Assert.That(stage.IsConfirmed, Is.True);
            Assert.That(
                stage.TryConfirm(true, out _, out refusal), Is.False,
                "The transition is one-way: a second confirm is refused by name.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.SelectionConfirmed));

            Assert.That(
                stage.TryChoose(new SeatId("seat-red-1"), "hero-sabina", out refusal), Is.False,
                "A pick arriving after the confirm cannot move a line-up the board is built from.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.SelectionConfirmed));
        }

        /// <summary>
        /// Every seat's row is visible to every seat, and says who the order is on - the selection
        /// is public at a real table exactly as the lobby roster is.
        /// </summary>
        [Test]
        public void EverySeatSeesEverySeatsChoice()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);
            Assert.That(stage.TryChoose(new SeatId("seat-red-1"), "hero-wasp", out _), Is.True);

            var seenByAnotherSeat = stage.RowsFor(new SeatId("seat-blue-3"));

            Assert.That(seenByAnotherSeat, Has.Count.EqualTo(6));
            var waspRow = seenByAnotherSeat.Single(row => row.Seat.Value == "seat-red-1");
            Assert.That(waspRow.HasChosen, Is.True);
            Assert.That(waspRow.HeroLabel, Is.EqualTo("Wasp the Warmaiden"));
            Assert.That(waspRow.IsOwnSeat, Is.False);
            Assert.That(waspRow.TeamLabel, Is.EqualTo("RED TEAM"));

            var picking = seenByAnotherSeat.Single(row => row.IsPicking);
            Assert.That(
                picking.Seat.Value, Is.EqualTo("seat-blue-1"),
                "Every seat can see whose turn it is, which is what stops six people waiting.");
            Assert.That(picking.HeroLabel, Is.EqualTo(HeroSelectionLabels.Waiting));

            var ownRow = stage.RowsFor(new SeatId("seat-blue-3"))
                .Single(row => row.Seat.Value == "seat-blue-3");
            Assert.That(ownRow.IsOwnSeat, Is.True);
            Assert.That(ownRow.HeroLabel, Is.EqualTo(HeroSelectionLabels.NotYetPicking));
        }

        // ---------------------------------------------------------------------------------------
        // The practice/solo path.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// A seat nobody is sitting in is given a hero by the host, out of what is left - and the
        /// one-copy rule still applies to it.
        /// </summary>
        /// <remarks>
        /// A spawned test driver cannot take its turn in p.22's order, so assignment bypasses the
        /// ORDER and nothing else. The assertion that matters is the second one: an assignment
        /// cannot hand a bot a hero a player has already picked.
        /// </remarks>
        [Test]
        public void BotSeatsAreAssignedFromWhatIsLeftAndNeverDuplicate()
        {
            var stage = new HeroSelectionStage(SixSeats(), TeamSide.Red);

            Assert.That(stage.TryChoose(new SeatId("seat-red-1"), "hero-wasp", out _), Is.True);

            Assert.That(
                stage.TryAssign(new SeatId("seat-blue-2"), "hero-wasp", out var refusal), Is.False,
                "A bot cannot be handed the hero a player already picked.");
            Assert.That(refusal, Is.EqualTo(HeroSelectionRefusal.HeroAlreadyTaken));

            Assert.That(
                stage.TryAssign(new SeatId("seat-blue-2"), "hero-sabina", out refusal), Is.True,
                "A named free hero is assignable out of the picking order.");
            Assert.That(stage.TryGetChoice(new SeatId("seat-blue-2"), out var assigned), Is.True);
            Assert.That(assigned.HeroIdValue, Is.EqualTo("hero-sabina"));

            Assert.That(
                stage.TryAssign(new SeatId("seat-red-3"), null, out refusal), Is.True,
                "With no preference the host takes the first hero still available.");
            Assert.That(stage.TryGetChoice(new SeatId("seat-red-3"), out var picked), Is.True);
            Assert.That(
                picked.HeroIdValue, Is.Not.EqualTo("hero-wasp").And.Not.EqualTo("hero-sabina"),
                "It cannot be one already held.");

            // Fill the rest and prove the whole thing still composes.
            foreach (var seat in new[] { "seat-red-2", "seat-blue-1", "seat-blue-3" })
            {
                Assert.That(stage.TryAssign(new SeatId(seat), null, out refusal), Is.True);
            }

            Assert.That(stage.IsComplete, Is.True);
            Assert.That(stage.TryConfirm(true, out var lineup, out _), Is.True);
            Assert.That(
                HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, lineup),
                Is.Not.Null,
                "A bot-filled table composes an ordinary board.");
        }

        // ---------------------------------------------------------------------------------------
        // Rulebook p.22's other five methods: declared, not built.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// All Pick is the one shipped method, and asking for another is refused loudly rather than
        /// silently behaving like All Pick.
        /// </summary>
        [Test]
        public void OnlyAllPickIsImplementedAndTheOtherFiveRefuse()
        {
            var unimplemented = ((HeroSelectionMethod[])Enum.GetValues(typeof(HeroSelectionMethod)))
                .Where(method => method != HeroSelectionMethod.AllPick)
                .ToArray();

            Assert.That(
                unimplemented, Has.Length.EqualTo(5),
                "Rulebook p.22 prints six selection methods.");

            foreach (var method in unimplemented)
            {
                Assert.That(
                    () => new HeroSelectionStage(SixSeats(), TeamSide.Red, method),
                    Throws.TypeOf<ArgumentOutOfRangeException>(),
                    $"{method} is printed and not built; composing one must say so.");
            }
        }

        /// <summary>
        /// Every method and every refusal has words. Exhaustive over both enums, on the same law
        /// <c>PrivateLobbyLabels</c> follows: a state a player can reach without words reaches them
        /// as a blank line.
        /// </summary>
        [Test]
        public void EveryMethodAndEveryRefusalHasWords()
        {
            foreach (HeroSelectionMethod method in Enum.GetValues(typeof(HeroSelectionMethod)))
            {
                var words = HeroSelectionLabels.Method(method);
                Assert.That(words, Is.Not.Empty, $"{method} has no words.");
                Assert.That(words.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
            }

            foreach (HeroSelectionRefusal refusal in Enum.GetValues(typeof(HeroSelectionRefusal)))
            {
                var words = HeroSelectionLabels.Refusal(refusal);
                if (refusal == HeroSelectionRefusal.None)
                {
                    Assert.That(words, Is.Empty);
                    continue;
                }

                Assert.That(words, Is.Not.Empty, $"{refusal} has no words.");
                Assert.That(words.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
            }
        }

        // ---------------------------------------------------------------------------------------
        // The selector generalises the shipped board rather than changing it.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Composing the PRINTED line-up reproduces the shipped board element for element, on every
        /// one of pp.5-6's four printed setups.
        /// </summary>
        /// <remarks>
        /// This is the zero-regression proof for the board, and it is why the spawn point had to
        /// become a property of the seat rather than of the hero: with the printed heroes selected
        /// in printed order, positional slots land every hero exactly where it has always stood. If
        /// this ever fails, the selector has moved the shipped board and this says which piece.
        /// </remarks>
        [Test]
        public void ComposingThePrintedLineupReproducesTheShippedBoardExactly()
        {
            foreach (var format in MatchFormat.All)
            {
                var shipped = QuickGameSetupConfiguration.CreateOfficial(format);
                var selected = HeroLineupComposition.Compose(
                    format, RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam));

                Assert.That(selected.WaveCount, Is.EqualTo(shipped.WaveCount), format.Id);
                Assert.That(
                    selected.LifeCountPerTeam, Is.EqualTo(shipped.LifeCountPerTeam), format.Id);
                Assert.That(
                    selected.RequiredMap.MapId.Value,
                    Is.EqualTo(shipped.RequiredMap.MapId.Value), format.Id);
                Assert.That(
                    selected.Heroes.Count, Is.EqualTo(shipped.Heroes.Count), format.Id);
                Assert.That(
                    selected.Minions.Count, Is.EqualTo(shipped.Minions.Count), format.Id);

                for (var index = 0; index < shipped.Heroes.Count; index++)
                {
                    var expected = shipped.Heroes[index];
                    var actual = selected.Heroes[index];
                    var where = $"{format.Id} hero {index}";

                    Assert.That(actual.HeroId.Value, Is.EqualTo(expected.HeroId.Value), where);
                    Assert.That(
                        actual.ContentId.Value, Is.EqualTo(expected.ContentId.Value), where);
                    Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName), where);
                    Assert.That(
                        actual.SpawnPoint.SpawnPointId.Value,
                        Is.EqualTo(expected.SpawnPoint.SpawnPointId.Value), where);
                    Assert.That(
                        actual.OccupiedSpace.SpaceId.Value,
                        Is.EqualTo(expected.OccupiedSpace.SpaceId.Value), where);
                }
            }
        }

        /// <summary>
        /// The participant graph a printed line-up composes is the one the shipped six-player
        /// configuration composes: same teams, same players, same hero-to-player ownership.
        /// </summary>
        [Test]
        public void ComposingThePrintedLineupReproducesTheShippedParticipants()
        {
            var shipped = OfficialSixPlayerParticipants.Create();
            var selected = HeroLineupComposition.ComposeParticipants(
                RegisteredHeroRoster.PrintedLineup(3));

            Assert.That(
                selected.Teams.Select(team => team.Side),
                Is.EquivalentTo(shipped.Teams.Select(team => team.Side)));
            Assert.That(
                selected.Players.Select(player => player.Id.Value).OrderBy(value => value),
                Is.EqualTo(shipped.Players.Select(player => player.Id.Value).OrderBy(v => v)));

            foreach (var hero in shipped.Heroes)
            {
                var match = selected.Heroes.SingleOrDefault(
                    candidate => candidate.Id.Value == hero.Id.Value);
                Assert.That(match, Is.Not.Null, $"{hero.Id.Value} is missing from the line-up.");
                Assert.That(
                    match.PlayerId.Value, Is.EqualTo(hero.PlayerId.Value),
                    $"{hero.Id.Value} changed hands.");
            }
        }

        /// <summary>
        /// A line-up that is NOT the printed one composes a real board: the heroes stand on their
        /// new team's marked points, hold their own printed sets, and belong to that team's players.
        /// </summary>
        /// <remarks>
        /// The arrangement chosen swaps the two teams outright, which is the strongest available
        /// statement that no hero owns a side: every one of the six is composed onto the team it
        /// has never been on, and everything still lands.
        /// </remarks>
        [Test]
        public void ASelectedLineupSeatsHeroesOnWhicheverTeamChoseThem()
        {
            var printed = RegisteredHeroRoster.PrintedLineup(3);
            var swapped = new HeroLineup(printed.Blue, printed.Red);

            var board = HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, swapped);
            var participants = HeroLineupComposition.ComposeParticipants(swapped);

            var arien = board.Heroes.Single(hero => hero.HeroId.Value == "hero-arien");
            Assert.That(
                arien.SpawnPoint.SpawnPointId.Value, Is.EqualTo("red-hero-01"),
                "Selected onto Red, Arien stands on Red's first marked point.");
            Assert.That(
                arien.ContentId.Value, Is.EqualTo("arien"),
                "The printed set travels with the hero, never with the seat.");

            var wasp = board.Heroes.Single(hero => hero.HeroId.Value == "hero-wasp");
            Assert.That(
                wasp.SpawnPoint.SpawnPointId.Value, Is.EqualTo("blue-hero-01"),
                "Selected onto Blue, Wasp stands in the Blue Throne zone.");

            Assert.That(swapped.SideOf("hero-wasp"), Is.EqualTo(TeamSide.Blue));
            Assert.That(swapped.SideOf("hero-arien"), Is.EqualTo(TeamSide.Red));

            var waspHero = participants.Heroes.Single(hero => hero.Id.Value == "hero-wasp");
            var waspPlayer = participants.Players.Single(
                player => player.Id.Value == waspHero.PlayerId.Value);
            var blueTeam = participants.Teams.Single(team => team.Side == TeamSide.Blue);
            Assert.That(
                waspPlayer.TeamId.Value, Is.EqualTo(blueTeam.Id.Value),
                "Wasp's player is on the Blue team, because that is the team that chose her.");

            Assert.That(
                board.Heroes.Select(hero => hero.SpawnPoint.SpawnPointId.Value).Distinct().Count(),
                Is.EqualTo(6),
                "No two heroes share a marked point whatever the line-up.");
        }

        /// <summary>
        /// A line-up with uneven teams, or with more heroes than a team's Throne zone has marked
        /// points, is refused rather than composed into a board that cannot be set up.
        /// </summary>
        [Test]
        public void ALineupMustFitThePrintedSetup()
        {
            var printed = RegisteredHeroRoster.PrintedLineup(3);

            Assert.That(
                () => new HeroLineup(printed.Red, printed.Blue.Take(2)),
                Throws.ArgumentException,
                "Setup step 2 splits the players into two EQUAL teams.");

            Assert.That(
                () => HeroLineupComposition.Compose(MatchFormat.TwoVersusTwoQuick, printed),
                Throws.ArgumentException,
                "A 2v2 fields two a side; a three-a-side line-up does not belong to it.");
        }

        private static void FillEverySeat(HeroSelectionStage stage)
        {
            var available = RegisteredHeroRoster.ForThisBuild.ToList();
            var index = 0;
            while (!stage.IsComplete && index < available.Count)
            {
                var seat = stage.PickingSeat;
                if (!seat.IsDefined) break;
                if (stage.TryChoose(seat, available[index].HeroIdValue, out _))
                {
                    index++;
                }
                else
                {
                    index++;
                }
            }

            Assert.That(stage.IsComplete, Is.True, "The fixture failed to seat every seat.");
        }
    }
}
