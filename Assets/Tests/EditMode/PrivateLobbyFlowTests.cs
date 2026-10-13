using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// 8.35z54. The lobby the user's spec describes, driven over the real wire: join the
    /// server, see the open seats, take one, ready, then everybody picks a hero, then the match
    /// that starts is the one that was chosen - and every seat can play it.
    /// </summary>
    /// <remarks>
    /// <para><b>Every case here is over six real client machines and a real host</b>, because the
    /// tier that only drove the host's own view is exactly where the shipped defects lived. The
    /// live playtest that produced this milestone's findings failed on things the whole Edit Mode
    /// suite was structurally blind to: a projection nobody republished, and a redraw condition
    /// that could not see the fact it was drawing.</para>
    /// </remarks>
    [TestFixture]
    public sealed class PrivateLobbyFlowTests
    {
        /// <summary>Composes a match for a chosen line-up, exactly as the scene's own composer does.</summary>
        private sealed class LineupComposer : IDeferredMatchComposition
        {
            private readonly PrivateOnlineWireHarness harness;

            public LineupComposer(PrivateOnlineWireHarness harness)
            {
                this.harness = harness;
            }

            public HeroLineup Last { get; private set; }

            public IMatchHost ComposeForLineup(HeroLineup lineup)
            {
                Last = lineup;
                return OwnerScopedProjectionTests.ComposeForLineup(lineup);
            }
        }

        private static PrivateOnlineWireHarness Seated(out LineupComposer composer)
        {
            var harness = PrivateOnlineWireHarness.Compose();
            composer = new LineupComposer(harness);
            harness.Session.Composition = composer;
            harness.SeatAll();
            return harness;
        }

        private static void ReadyEverySeat(PrivateOnlineWireHarness harness)
        {
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                harness.Client(ordinal).TrySetReadiness(true);
            }

            harness.Pump();
        }

        private static int OrdinalOf(PrivateOnlineWireHarness harness, SeatId seat)
        {
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                if (harness.SeatOf(ordinal) == seat.Value)
                {
                    return ordinal;
                }
            }

            Assert.Fail("No connection holds " + seat.Value + ".");
            return -1;
        }

        /// <summary>Runs p.22's order to completion, each seat taking the first hero its own surface offers.</summary>
        private static void EverySeatPicks(PrivateOnlineWireHarness harness)
        {
            var guard = 0;
            while (!harness.Session.HasMatchStarted && guard++ < 12)
            {
                var picking = harness.Session.Selection.PickingSeat;
                if (!picking.IsDefined)
                {
                    break;
                }

                var client = harness.Client(OrdinalOf(harness, picking));
                var offered = PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .First(option => option.IsAvailable);
                client.TryChooseHero(offered.HeroIdValue);
                harness.Pump();
            }
        }

        // ------------------------------------------------------------------ joining seatless

        [Test]
        public void AJoinerThatNamesNoSeat_IsAdmittedAndCanSeeEverySeatThatIsOpen()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var joiner = harness.ConnectSeatless(1);
            harness.Pump();

            Assert.That(joiner.State, Is.EqualTo(PrivateSessionClientState.Admitted),
                "The spec's player joins the SERVER first; holding a seat is a later step.");
            Assert.That(joiner.TargetSeat.IsDefined, Is.False);
            Assert.That(joiner.Roster.Count, Is.EqualTo(6),
                "It can see the whole table before it takes a chair, which is what makes " +
                "'see available seats and pick one' expressible at all.");
            Assert.That(joiner.Roster.All(row => row.Occupancy == SeatOccupancyState.Open), Is.True);
        }

        [Test]
        public void AnAdmittedJoiner_TakesOneOfTheSeatsItCanSee_AndIsSeatedInIt()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var joiner = harness.ConnectSeatless(1);
            harness.Pump();

            var wanted = joiner.Roster.Single(row => row.Seat.Value == "blue-player-01");
            Assert.That(joiner.TryClaimSeat(wanted.Seat), Is.True);
            harness.Pump();

            Assert.That(joiner.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(joiner.TargetSeat.Value, Is.EqualTo("blue-player-01"));
            Assert.That(
                harness.Session.Roster.Single(row => row.Seat.Value == "blue-player-01").Occupancy,
                Is.EqualTo(SeatOccupancyState.Claimed));
        }

        [Test]
        public void ASeatSomebodyElseHolds_IsNotOfferedToAnAdmittedJoiner()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.Connect(1, "blue-player-01");
            harness.Pump();
            var second = harness.ConnectSeatless(2);
            harness.Pump();

            var taken = second.Roster.Single(row => row.Seat.Value == "blue-player-01");
            Assert.That(taken.Occupancy, Is.EqualTo(SeatOccupancyState.Claimed),
                "The roster the joiner is reading already says who is sitting where.");
            Assert.That(second.TryClaimSeat(taken.Seat), Is.False,
                "A seat the roster shows taken is not sent; the host's refusal remains the " +
                "authority, this only spares the wire a frame whose answer is already held.");
            Assert.That(second.State, Is.EqualTo(PrivateSessionClientState.Admitted));
        }

        [Test]
        public void TheAdmittedState_ReadsAsWordsAndOffersTakingASeatAndNothingElseButLeaving()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var joiner = harness.ConnectSeatless(1);
            harness.Pump();

            var model = PrivateLobbyModel.ForClient(joiner);
            Assert.That(model.StatusLabel, Is.EqualTo("PICK YOUR SEAT"));
            Assert.That(model.Controls,
                Is.EquivalentTo(new[] { PrivateLobbyControl.ClaimSeat, PrivateLobbyControl.Leave }),
                "Readiness needs a seat to be ready WITH and a hero belongs to a seat, so " +
                "neither is offered to somebody who holds neither.");
        }

        // ------------------------------------------------------------------ ready, then pick

        [Test]
        public void AHeroPick_BeforeEverySeatIsTakenAndReady_IsRefusedByNameAndKeepsTheConnection()
        {
            var harness = Seated(out _);

            harness.Client(1).TryChooseHero("hero-wasp");
            harness.Pump();

            Assert.That(harness.Client(1).RejectionReason,
                Is.EqualTo(SessionRejectionReason.HeroSelectionRefused));
            Assert.That(harness.Client(1).RejectionDetail,
                Does.Contain("ready"),
                "A player who picked too early is told what the lobby is waiting for.");
            Assert.That(harness.Client(1).State, Is.EqualTo(PrivateSessionClientState.Seated),
                "A pick that arrived early is a mistake, not an attack.");
            Assert.That(
                harness.Session.Roster.Any(row => row.HasChosenHero), Is.False);
        }

        [Test]
        public void TheSelectionOpens_OnTheREADINESSOfTheLastSeat_AndNotOneSeatBefore()
        {
            var harness = Seated(out _);

            for (var ordinal = 1; ordinal <= 5; ordinal++)
            {
                harness.Client(ordinal).TrySetReadiness(true);
                harness.Pump();
                Assert.That(harness.Session.IsPickPhaseOpen, Is.False,
                    "Five of six ready is not a table ready to pick.");
                Assert.That(PrivateLobbyModel.ForClient(harness.Client(1)).Selection.IsRunning,
                    Is.False, "...and no surface is showing a hero list yet either.");
            }

            harness.Client(6).TrySetReadiness(true);
            harness.Pump();

            Assert.That(harness.Session.IsPickPhaseOpen, Is.True);
            Assert.That(harness.Session.HasMatchStarted, Is.False,
                "Readiness OPENS the picking; it no longer starts the match.");
            Assert.That(PrivateLobbyModel.ForClient(harness.Client(1)).Selection.IsRunning, Is.True);
        }

        [Test]
        public void EveryClientAndTheHost_OpenTheSelectionAtTheSameInstant_FromTheRosterAlone()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);

            Assert.That(PrivateLobbyModel.IsPickPhaseOpen(harness.Session.Roster), Is.True);
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                Assert.That(
                    PrivateLobbyModel.IsPickPhaseOpen(harness.Client(ordinal).Roster), Is.True,
                    "Seat " + harness.SeatOf(ordinal) + " derives the same answer the host does, " +
                    "from the roster it was sent - which is why this needed no message of its own.");
            }
        }

        [Test]
        public void TheMatchStarts_OnTheLastPickRatherThanOnTheLastReadiness()
        {
            var harness = Seated(out var composer);
            ReadyEverySeat(harness);
            Assert.That(harness.Session.HasMatchStarted, Is.False);

            var picked = 0;
            while (!harness.Session.HasMatchStarted && picked < 6)
            {
                var picking = harness.Session.Selection.PickingSeat;
                var client = harness.Client(OrdinalOf(harness, picking));
                var offered = PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .First(option => option.IsAvailable);
                client.TryChooseHero(offered.HeroIdValue);
                harness.Pump();
                picked++;
                Assert.That(harness.Session.HasMatchStarted, Is.EqualTo(picked == 6),
                    "The match begins on the SIXTH pick and not one pick before.");
            }

            Assert.That(composer.Last, Is.Not.Null,
                "The line-up every seat chose is what the match was composed from.");
        }

        [Test]
        public void ThePicksRun_InTheOrderRulebookPage22Prints_AlternatingFromTheCoinsTeam()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);

            var coin = harness.Session.Selection.FirstPickingTeam;
            var seen = new System.Collections.Generic.List<TeamSide>();
            while (!harness.Session.HasMatchStarted)
            {
                var picking = harness.Session.Selection.PickingSeat;
                seen.Add(harness.Session.Roster.Single(row => row.Seat.Equals(picking)).TeamSide);
                var client = harness.Client(OrdinalOf(harness, picking));
                client.TryChooseHero(PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .First(option => option.IsAvailable).HeroIdValue);
                harness.Pump();
            }

            Assert.That(seen.Count, Is.EqualTo(6));
            Assert.That(seen[0], Is.EqualTo(coin), "The coin's team picks first, as printed.");
            for (var index = 1; index < seen.Count; index++)
            {
                Assert.That(seen[index], Is.Not.EqualTo(seen[index - 1]),
                    "...and the teams alternate, which is the printed rule rather than a scan.");
            }
        }

        // ------------------------------------------------------------------ the shipped defects

        /// <summary>
        /// THE DEFECT THE LIVE PLAYTEST FOUND, as a regression. A match recomposed for a chosen
        /// line-up must be PUBLISHED, or every seat plays a projection of a match nobody is playing.
        /// </summary>
        [Test]
        public void AChosenLineUp_ReachesEverySeatsProjection_SoNoSeatHoldsAnotherMatchsHero()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);
            EverySeatPicks(harness);

            Assert.That(harness.Session.HasMatchStarted, Is.True);
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                var client = harness.Client(ordinal);
                var seat = harness.SeatOf(ordinal);
                var chosen = client.Roster.Single(row => row.Seat.Value == seat);
                Assert.That(client.LatestProjection, Is.Not.Null);
                Assert.That(client.LatestProjection.HeroId, Is.EqualTo(chosen.Hero.Value),
                    "Seat " + seat + " was sent a projection of the match it is actually in. " +
                    "Before this milestone every seat held the PROVISIONAL match's projection, " +
                    "so the seat's own hero disagreed with the roster's on all six seats.");
                Assert.That(chosen.HasChosenHero, Is.True);
            }
        }

        /// <summary>
        /// THE DIRECT NEGATIVE OF THE USER'S HEADLINE FINDING - "as a player i had to choose cards
        /// for all heroes" - asserted per client rather than on the host's own view.
        /// </summary>
        [Test]
        public void EachSeat_IsOfferedOnlyItsOwnHerosCommands_AndCanActuallySubmitThem()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);
            EverySeatPicks(harness);

            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                var client = harness.Client(ordinal);
                var record = client.LatestProjection;
                var mine = record.HeroId;
                Assert.That(record.AuthorizedCommands, Is.Not.Empty,
                    "Seat " + harness.SeatOf(ordinal) + " has something to do.");
                foreach (var command in record.AuthorizedCommands)
                {
                    var text = command.ToString();
                    foreach (var hero in RegisteredHeroRoster.ForThisBuild)
                    {
                        if (hero.HeroIdValue == mine || !text.Contains(hero.HeroIdValue))
                        {
                            continue;
                        }

                        Assert.Fail(
                            "Seat " + harness.SeatOf(ordinal) + " (hero " + mine + ") was offered " +
                            "a command naming " + hero.HeroIdValue + ": " + text);
                    }
                }

                Assert.That(client.TrySubmitCommand(record.AuthorizedCommands[0], out _), Is.True);
                harness.Pump();
                Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                    "Seat " + harness.SeatOf(ordinal) + "'s own offered command is one the host " +
                    "accepts. Before this milestone every seat's only offer came back refused - " +
                    "'the payload claims hero X, which seat Y does not own' - and the match could " +
                    "not advance one version.");
            }
        }

        /// <summary>
        /// THE REDRAW DEFECT behind "it let me pick a hero but it didnt do nothing": the string a
        /// lobby surface redraws on must be able to see a pick.
        /// </summary>
        [Test]
        public void TheRosterSignature_MovesWhenAPickLands_AndTheOldOneCouldNotHaveSeenIt()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);

            var picking = harness.Session.Selection.PickingSeat;
            var watcher = harness.Client(1);
            var before = PrivateLobbyModel.RosterSignature(watcher.Roster);
            var beforeWithoutHero = WithoutHero(watcher.Roster);

            var picker = harness.Client(OrdinalOf(harness, picking));
            picker.TryChooseHero(PrivateLobbyModel.ForClient(picker).Selection.Heroes
                .First(option => option.IsAvailable).HeroIdValue);
            harness.Pump();

            Assert.That(PrivateLobbyModel.RosterSignature(watcher.Roster), Is.Not.EqualTo(before),
                "Every OTHER seat's surface can see that a pick happened.");
            Assert.That(WithoutHero(watcher.Roster), Is.EqualTo(beforeWithoutHero),
                "...and the seat, occupancy and readiness are exactly what they were, which is " +
                "why the shipped signature - which named only those three - was blind to the " +
                "whole selection and no lobby ever redrew during it.");
        }

        private static string WithoutHero(
            System.Collections.Generic.IReadOnlyList<SeatRosterEntry> roster)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var entry in roster)
            {
                builder.Append('|').Append(entry.Seat.Value).Append(entry.Occupancy)
                    .Append(entry.IsReady ? '1' : '0');
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------------ the host's undo

        [Test]
        public void TheHostsUndo_TakesBackTheLastPick_AndReturnsTheOrderToThatSeat()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);

            var first = harness.Session.Selection.PickingSeat;
            var picker = harness.Client(OrdinalOf(harness, first));
            var hero = PrivateLobbyModel.ForClient(picker).Selection.Heroes
                .First(option => option.IsAvailable).HeroIdValue;
            picker.TryChooseHero(hero);
            harness.Pump();
            Assert.That(harness.Session.Selection.PickingSeat, Is.Not.EqualTo(first));
            Assert.That(harness.Session.CanUndoLobbyStep, Is.True);

            Assert.That(harness.Session.TryUndoLobbyStep(), Is.True);
            harness.Pump();

            Assert.That(harness.Session.Selection.PickingSeat, Is.EqualTo(first),
                "The order is back on the seat whose pick was taken away.");
            Assert.That(
                harness.Session.Roster.Single(row => row.Seat.Equals(first)).HasChosenHero,
                Is.False);
            Assert.That(
                harness.Client(1).Roster.Single(row => row.Seat.Equals(first)).HasChosenHero,
                Is.False,
                "The rewind is broadcast, so nobody is left reading a pick that no longer stands.");
            Assert.That(PrivateLobbyModel.ForClient(picker).Selection.Heroes
                    .Single(option => option.HeroIdValue == hero).IsAvailable, Is.True,
                "The hero it released is available again.");
        }

        [Test]
        public void TheHostsUndo_IsNeitherOfferedNorHonoured_OnceTheMatchHasBegun()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);
            EverySeatPicks(harness);

            Assert.That(harness.Session.HasMatchStarted, Is.True);
            Assert.That(harness.Session.CanUndoLobbyStep, Is.False,
                "The transition out of the lobby is one-way, so there is no lobby to step back " +
                "into - the bound is the transition itself rather than a check on a button.");
            Assert.That(harness.Session.TryUndoLobbyStep(), Is.False);
            Assert.That(PrivateLobbyModel.ForHost(harness.Session, string.Empty)
                .Offers(PrivateLobbyControl.HostUndo), Is.False);
        }

        [Test]
        public void TheHostsUndo_IsNotOfferedBeforeAnybodyHasPicked()
        {
            var harness = Seated(out _);
            ReadyEverySeat(harness);

            Assert.That(harness.Session.CanUndoLobbyStep, Is.False,
                "There is no lobby step to take back yet; a seat's own readiness is that " +
                "player's switch rather than the host's.");
            Assert.That(PrivateLobbyModel.ForHost(harness.Session, string.Empty)
                .Offers(PrivateLobbyControl.HostUndo), Is.False);
        }

        // ------------------------------------------------------------------ the entry form

        [Test]
        public void TheEntryForm_OffersTeamSizeWithoutPlayerFacingGameLengths()
        {
            var model = PrivateLobbyModel.Entry(
                System.Array.Empty<SeatRosterEntry>(), string.Empty, "3V3-QUICK");

            Assert.That(model.Offers(PrivateLobbyControl.SetMatchFormat), Is.False,
                "The user pressed the cycling SETUP button six times in the failed session's own " +
                "host log and never reached a setup they wanted.");
            Assert.That(model.Offers(PrivateLobbyControl.ChooseTwoVsTwo), Is.True);
            Assert.That(model.Offers(PrivateLobbyControl.ChooseThreeVsThree), Is.True);
            Assert.That(model.Offers(PrivateLobbyControl.ChooseFourVsFour), Is.True);
            Assert.That(model.Offers(PrivateLobbyControl.ChooseFiveVsFive), Is.True);
            Assert.That(model.Offers(PrivateLobbyControl.ChooseShortGame), Is.False);
            Assert.That(model.Offers(PrivateLobbyControl.ChooseLongGame), Is.False);
            Assert.That(model.Offers(PrivateLobbyControl.Host), Is.True);
            Assert.That(model.Offers(PrivateLobbyControl.Join), Is.True);
        }

        [Test]
        public void EveryTeamSize_HasTheFormerQuickValuesAsItsHostDefault()
        {
            Assert.That(MatchFormat.DefaultForTeamSize(2), Is.SameAs(MatchFormat.TwoVersusTwoQuick));
            Assert.That(MatchFormat.DefaultForTeamSize(3), Is.SameAs(MatchFormat.ThreeVersusThreeQuick));
            Assert.That(MatchFormat.DefaultForTeamSize(4).Id, Is.EqualTo("custom-8p-w7-l6"));
            Assert.That(MatchFormat.DefaultForTeamSize(5).Id, Is.EqualTo("custom-10p-w7-l7"));
        }
    }
}
