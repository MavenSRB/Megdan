using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class PrivateSessionBotFleetTests
    {
        private sealed class Composer : IDeferredMatchComposition
        {
            internal readonly MatchFormat Format;
            internal MatchConfiguration Configuration;
            internal IMatchHost Match;
            internal PrivateSessionIdentity Identity;
            internal Composer(MatchFormat format)
            {
                Format = format;
                ComposeForLineup(RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam));
            }
            public IMatchHost ComposeForLineup(HeroLineup lineup)
            {
                Configuration = MatchConfiguration.CreateForLineupForInternalTest(Format, lineup, TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(Format, lineup),
                    Configuration.Participants, map, Configuration.TieBreakerState);
                var scenario = new InitialScenarioDescription(Configuration.Participants,
                    Configuration.CardDefinitions, Configuration.CardInstances, Configuration.TieBreakerState,
                    Configuration.SetupSeed, map.Reference,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    Array.Empty<ObstacleOccupancy>());
                Match = MatchHost.Compose(new MatchSessionInitialState(scenario, setup, new[] { map }));
                Identity = PrivateSessionIdentity.ForComposition(map, Configuration, setup, "bot-fleet-test");
                return Match;
            }
        }

        // The human side remains a separate byte endpoint with positive transport identity.
        private sealed class HumanWire : IPrivateSessionOutbox, IPrivateSessionClientOutbox
        {
            private readonly Queue<(bool ToHost, byte[] Bytes)> queue = new Queue<(bool, byte[])>();
            private readonly PrivateSessionFrameAssembler toHost = new PrivateSessionFrameAssembler();
            private readonly PrivateSessionFrameAssembler fromHost = new PrivateSessionFrameAssembler();
            internal PrivateSessionHost Host;
            internal PrivateSessionClient Machine;
            internal int HostFrames;
            internal bool Closed;
            public void Send(int ordinal, byte[] frame)
            {
                Assert.That(ordinal, Is.EqualTo(1), "Bot bytes must never reach the human transport.");
                HostFrames++;
                queue.Enqueue((false, frame));
            }
            public void Close(int ordinal) { Assert.That(ordinal, Is.EqualTo(1)); Closed = true; }
            public void Send(byte[] frame) => queue.Enqueue((true, frame));
            public void Close() { Closed = true; Host.OnDisconnected(1); }
            internal void Pump()
            {
                for (var count = 0; queue.Count > 0 && count < 1024; count++)
                {
                    var next = queue.Dequeue();
                    var assembler = next.ToHost ? toHost : fromHost;
                    Assert.That(assembler.Add(next.Bytes, 0, next.Bytes.Length), Is.True);
                    while (assembler.TryDequeue(out var frame))
                    {
                        if (next.ToHost) Host.OnFrame(1, frame);
                        else Machine.OnFrame(frame);
                    }
                }
                Assert.That(queue, Is.Empty);
            }
        }

        [TestCase("2v2-quick", "")]
        [TestCase("2v2-long", "private-test-key")]
        [TestCase("3v3-quick", "private-test-key")]
        [TestCase("3v3-long", "")]
        public void FillUsesNormalAdmissionDraftAndBoundedPlayBesideAHuman(string formatId, string key)
        {
            var composer = new Composer(MatchFormat.FindById(formatId));
            var wire = new HumanWire();
            var nonce = new CountingNonceSource(69);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, key, wire, nonce,
                () => composer.Match.Projection, composer.Configuration.ContentCatalogs, 17);
            wire.Host = fleet.Host;
            fleet.Host.Composition = composer;
            var humanSeat = composer.Match.Seats.Seats[0].Id;
            wire.Machine = new PrivateSessionClient(fleet.Host.Identity, humanSeat, key, wire, nonce);
            wire.Machine.OnConnected();
            fleet.Host.OnConnected(1, 0);
            wire.Pump();
            Assert.That(wire.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(fleet.FillOpenSeats(0, 0), Is.EqualTo(fleet.Host.TotalSeats - 1));
            Assert.That(fleet.FillOpenSeats(0, 0), Is.Zero, "Repeated FILL cannot create duplicate clients.");
            for (var tick = 0; tick < 20; tick++) { fleet.Tick(tick); wire.Pump(); }
            Assert.That(fleet.Host.Roster.All(row => row.Occupancy == SeatOccupancyState.Claimed), Is.True);
            Assert.That(fleet.Host.IsPickPhaseOpen, Is.False, "The human has not readied yet.");
            Assert.That(fleet.Seats.Contains(humanSeat), Is.False);
            Assert.That(wire.Machine.TrySetReadiness(true), Is.True);
            wire.Pump();
            string humanHero = null;
            for (var tick = 20; tick < 100 && !fleet.Host.HasMatchStarted; tick++)
            {
                fleet.Tick(tick);
                wire.Pump();
                var lobby = PrivateLobbyModel.ForClient(wire.Machine);
                if (!lobby.Selection.IsOwnPick) continue;
                var hero = lobby.Selection.Heroes.First(value => value.IsAvailable &&
                    value.HeroIdValue.IndexOf("whisper", StringComparison.OrdinalIgnoreCase) >= 0);
                humanHero = hero.HeroIdValue;
                Assert.That(wire.Machine.TryChooseHero(humanHero), Is.True);
                wire.Pump();
            }
            Assert.That(fleet.Host.HasMatchStarted, Is.True);
            Assert.That(humanHero, Is.Not.Null);
            Assert.That(fleet.Host.Roster.Single(row => row.Seat.Equals(humanSeat)).Hero.Value, Is.EqualTo(humanHero));
            Assert.That(fleet.Host.Identity.MatchFormatId, Is.EqualTo(formatId));
            Assert.That(fleet.LastFailure, Is.Empty);
            var human = new NetworkMatchClient(wire.Machine, composer.Match.Projection, composer.Configuration.ContentCatalogs);
            var random = new DeterministicBotRandom(170);
            for (var tick = 100; tick < 1300 && fleet.AcceptedCommands < 40; tick++)
            {
                var before = fleet.AcceptedCommands;
                fleet.Tick(tick);
                wire.Pump();
                Assert.That(fleet.AcceptedCommands - before, Is.InRange(0, 1));
                Assert.That(fleet.PendingDeliveries, Is.Zero);
                PlayHuman(wire, human, random);
                wire.Pump();
            }
            Assert.That(fleet.AcceptedCommands, Is.GreaterThanOrEqualTo(40),
                "Bots must play beyond the first commitments. " + fleet.LastFailure);
            Assert.That(wire.Machine.LatestProjection, Is.Not.Null);
            Assert.That(wire.Machine.LatestProjection.Version, Is.EqualTo(composer.Match.Version));
            Assert.That(wire.Closed, Is.False);
            // Milestone 8.38e made this message say WHAT was refused. A bare `Is.False` reports that
            // some bot offered something the host would not authorize and nothing about which, and
            // this tier exists precisely because it is the one that finds a hero's offers disagreeing
            // with the host's authorization.
            var refusals = fleet.Host.DrainEvents()
                .Where(record => record.Kind == PrivateSessionEventKind.SubmissionRefused)
                .Select(record => record.Seat + "@" + record.Version + ": " + record.SubmissionRejection)
                .ToArray();
            Assert.That(refusals, Is.Empty,
                "A bot offered something the host refused: " + string.Join(" | ", refusals) + " ;; lastRefusal=" + fleet.LastRefusal);

            fleet.Host.OnDisconnected(1);
            var paused = composer.Match.Version;
            for (var tick = 200; tick < 220; tick++) fleet.Tick(tick);
            Assert.That(fleet.Host.IsPaused, Is.True);
            Assert.That(composer.Match.Version, Is.EqualTo(paused), "No bot bypasses a reserved human's pause.");
            Assert.That(fleet.FillOpenSeats(220, 0), Is.Zero, "FILL never takes a reserved human seat or changes a started match.");
            fleet.Dispose();
            Assert.That(fleet.Seats, Is.Empty);
            Assert.That(fleet.PendingDeliveries, Is.Zero);
            Assert.That(fleet.Tick(300), Is.False);
        }

        private static void PlayHuman(HumanWire wire, NetworkMatchClient client, IBotRandom random)
        {
            var machine = wire.Machine;
            if (machine.IsWaitingForHost) return;
            var projection = client.Projection;
            var hero = new HeroId(machine.LatestProjection.HeroId);
            var agreement = projection.TeamAgreement;
            var own = agreement?.Participants.FirstOrDefault(value => value.HeroId.Equals(hero));
            if (own != null)
            {
                var statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                    agreement.Kind == TeamAgreementKind.Initiative && !own.HasStatement &&
                    value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                    value.PreferredCardInstanceId.Equals(agreement.Candidates[0].CardInstanceId) ||
                    agreement.Kind == TeamAgreementKind.MinionBattle && !own.Agreed &&
                    value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                if (statement != null) { Assert.That(machine.TrySubmitAgreement(statement, out _), Is.True); return; }
            }
            var commands = machine.LatestProjection.AuthorizedCommands;
            if (commands.Count == 0) return;
            var selected = commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (projection.PendingMinionBattleChoice != null)
                selected = commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.SelectMinionBattleRemoval);
            selected = selected ?? BotPolicy.SelectGreedy(commands, projection, hero, random) ?? commands[0];
            Assert.That(machine.TrySubmitCommand(selected, out _), Is.True);
        }

        [Test]
        public void FirstLobbyBotCommitmentUsesTheMeasuredTacticalOwnerPolicy()
        {
            const int seed = 17;
            var composer = new Composer(MatchFormat.TwoVersusTwoQuick);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, "", new HumanWire(),
                new CountingNonceSource(85), () => composer.Match.Projection,
                composer.Configuration.ContentCatalogs, seed);
            fleet.Host.Composition = composer;
            fleet.FillOpenSeats(0, 0);
            long tick = 0;
            while (!fleet.Host.HasMatchStarted && tick < 100) fleet.Tick(++tick);
            Assert.That(fleet.Host.HasMatchStarted, Is.True);
            Assert.That(fleet.AcceptedCommands, Is.Zero);
            var unchanged = composer.Match.CaptureSnapshot();
            var expected = composer.Match.Seats.Seats.Select(seat =>
            {
                var owner = composer.Match.ProjectionForSeat(seat.Id);
                return BotPolicy.SelectPrivateSessionTactics(owner.AuthorizedCommands, owner, seat.HeroId,
                    new DeterministicBotRandom(seed));
            }).ToArray();
            Assert.That(expected, Has.All.Not.Null);
            Assert.That(expected.All(command => command.Kind == DirectorCommandKind.CommitCard), Is.True);
            Assert.That(composer.Match.CaptureSnapshot(), Is.EqualTo(unchanged),
                "Owner-only policy evaluation must not mutate the host.");
            var cursor = composer.Match.ReadResults(0).NextSequence;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Assert.That(fleet.Tick(++tick), Is.True);
            timer.Stop();
            var accepted = composer.Match.ReadResults(cursor).Records.Single().Command;
            TestContext.WriteLine("First encoded lobby decision including wire/host refresh: " +
                timer.Elapsed.TotalMilliseconds.ToString("F2") + " ms");
            Assert.That(fleet.AcceptedCommands, Is.EqualTo(1));
            Assert.That(expected, Does.Contain(accepted),
                "The real fleet must use the same tactical choice as the owner-scoped policy.");
        }

        [Test]
        public void BotDraftVariesBySeedAndReproducesLegalPicks()
        {
            var first = Draft(7);
            Assert.That(Draft(7), Is.EqualTo(first), "A recorded input must reproduce the draft.");
            Assert.That(Draft(29), Is.Not.EqualTo(first), "Bots must not always take the first available hero.");
        }

        [Test]
        public void ActualLobbyBotsUseFastTravelAndReachProductiveCombat()
        {
            var composer = new Composer(MatchFormat.TwoVersusTwoQuick);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, "", new HumanWire(),
                new CountingNonceSource(85), () => composer.Match.Projection,
                composer.Configuration.ContentCatalogs, seed: 307);
            fleet.Host.Composition = composer;
            fleet.FillOpenSeats(0, 0);
            long tick = 0;
            while (!fleet.Host.HasMatchStarted && tick < 200) fleet.Tick(++tick);
            Assert.That(fleet.Host.HasMatchStarted, Is.True, fleet.LastFailure);

            var cursor = composer.Match.ReadResults(0).NextSequence;
            var fastTravels = 0;
            var accepted = 0;
            while (!fleet.Host.HasEnded && accepted < 500 && tick < 5000)
            {
                fleet.Tick(++tick);
                var batch = composer.Match.ReadResults(cursor);
                cursor = batch.NextSequence;
                foreach (var record in batch.Records)
                {
                    accepted++;
                    if (record.Command.Kind == DirectorCommandKind.PerformFastTravel) fastTravels++;
                }
            }

            var combat = composer.Match.Projection.MatchLog.Count(entry =>
                entry.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.Attack ||
                entry.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.SkillUsed ||
                entry.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.MinionDefeated ||
                entry.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.HeroDefeated);
            TestContext.WriteLine(
                "accepted=" + accepted + " fastTravel=" + fastTravels + " combat=" + combat +
                " ended=" + fleet.Host.HasEnded);
            Assert.That(fleet.LastFailure, Is.Empty);
            Assert.That(accepted, Is.GreaterThan(100));
            Assert.That(fastTravels, Is.GreaterThan(0),
                "The actual encoded lobby fleet never used Fast Travel.");
            Assert.That(combat, Is.GreaterThan(0),
                "The actual lobby fleet moved but never reached an Attack, Skill or Defeat.");
        }

        private static string[] Draft(int seed)
        {
            var composer = new Composer(MatchFormat.TwoVersusTwoQuick);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, "", new HumanWire(),
                new CountingNonceSource(85), () => composer.Match.Projection, composer.Configuration.ContentCatalogs, seed);
            fleet.Host.Composition = composer;
            fleet.FillOpenSeats(0, 0);
            for (var tick = 1; tick <= 100 && !fleet.Host.HasMatchStarted; tick++) fleet.Tick(tick);
            Assert.That(fleet.Host.HasMatchStarted, Is.True, fleet.LastFailure);
            var choices = fleet.Host.Roster.Select(seat => seat.Hero.Value).ToArray();
            Assert.That(choices.Distinct().Count(), Is.EqualTo(4));
            Assert.That(choices.All(hero => RegisteredHeroRoster.ForThisBuild.Any(option => option.HeroIdValue == hero)), Is.True);
            return choices;
        }

        /// <summary>
        /// Milestone 8.57g, the milestone that deals Rowenna the Vanguard. A LOBBY BOT actually
        /// holding her printed set, playing it against three other bots until the host has accepted a
        /// hundred commands, with nothing refused.
        ///
        /// <para><b>Why this tier and not another.</b> Milestone 8.38e's dealing found an
        /// <c>AtomicityViolation</c> here that no other tier can see: a bot answered an effect choice,
        /// that answer started a borrowed Attack with nobody in reach, and the command was refused as
        /// <c>AttackActionRejected: TargetUnavailable</c> AFTER mutating state. A bot answers every
        /// prompt a hero's content can raise, in orders a written journey never chooses, so a hero
        /// whose offers disagree with the host's authorization fails here first. Rowenna raises more
        /// prompts per turn than most - her Reds ask a second hero to retrieve or take a coin, Throw
        /// the Gauntlet hands the DECISION to an opponent, and Grand Melee offers two performances in
        /// any combination - so the question is worth asking of her by name.</para>
        ///
        /// <para><b>The seed is searched rather than written down, and the search is the assertion.</b>
        /// A lobby bot drafts its own hero, so no caller can put her in a seat: what a fixture can do
        /// is walk seeds until a draft seats her and then play THAT one. The walk is deterministic and
        /// bounded, and a build in which no seed in the range seats her fails here - which would mean
        /// she had fallen out of the offerable roster, the very thing milestone 8.35z41 exists to
        /// catch.</para>
        /// </summary>
        [Test]
        public void ALobbyBotDealtRowenna_PlaysHerWithoutTheHostRefusingAnything()
        {
            const string rowenna = QuickGameSetupConfiguration.RowennaHeroIdValue;

            var seed = 0;
            for (var candidate = 1; candidate <= 64 && seed == 0; candidate++)
            {
                if (Draft(candidate).Contains(rowenna))
                {
                    seed = candidate;
                }
            }

            Assert.That(seed, Is.Not.Zero,
                "No seed in 1..64 drafted Rowenna, which means she is not offerable in a lobby at " +
                "all - the exact defect milestone 8.35z41 was written to catch.");
            TestContext.WriteLine("Rowenna is drafted by lobby seed " + seed + ".");

            var composer = new Composer(MatchFormat.TwoVersusTwoQuick);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, "", new HumanWire(),
                new CountingNonceSource(85), () => composer.Match.Projection,
                composer.Configuration.ContentCatalogs, seed);
            fleet.Host.Composition = composer;
            fleet.FillOpenSeats(0, 0);
            long tick = 0;
            while (!fleet.Host.HasMatchStarted && tick < 200) fleet.Tick(++tick);
            Assert.That(fleet.Host.HasMatchStarted, Is.True, fleet.LastFailure);

            var hers = fleet.Host.Roster.Where(row => row.Hero.Value == rowenna).ToArray();
            Assert.That(hers, Is.Not.Empty, "The measured seed must still seat her.");

            var accepted = 0;
            var cursor = composer.Match.ReadResults(0).NextSequence;
            while (!fleet.Host.HasEnded && accepted < 100 && tick < 3000)
            {
                fleet.Tick(++tick);
                var batch = composer.Match.ReadResults(cursor);
                cursor = batch.NextSequence;
                accepted += batch.Records.Count;
            }

            var refusals = fleet.Host.DrainEvents()
                .Where(record => record.Kind == PrivateSessionEventKind.SubmissionRefused)
                .Select(record => record.Seat + "@" + record.Version + ": " + record.SubmissionRejection)
                .ToArray();
            Assert.That(refusals, Is.Empty,
                "A bot offered something the host refused: " + string.Join(" | ", refusals) +
                " ;; lastRefusal=" + fleet.LastRefusal);
            Assert.That(fleet.LastFailure, Is.Empty);
            Assert.That(accepted, Is.GreaterThanOrEqualTo(100),
                "The fleet must play well past the first commitments with her in a seat. " +
                fleet.LastFailure);
        }

        [Test]
        public void InvalidPacingIsRefusedBeforeAnySeatIsClaimed()
        {
            var composer = new Composer(MatchFormat.TwoVersusTwoQuick);
            using var fleet = new PrivateSessionBotFleet(composer.Match, composer.Identity, "", new HumanWire(),
                new CountingNonceSource(69), () => composer.Match.Projection, composer.Configuration.ContentCatalogs);
            foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity, 61d })
                Assert.Throws<ArgumentOutOfRangeException>(() => fleet.FillOpenSeats(0, invalid));
            Assert.That(fleet.Host.ClaimedSeatCount, Is.Zero);
        }
    }
}
