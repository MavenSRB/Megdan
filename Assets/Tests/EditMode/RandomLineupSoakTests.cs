using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.10. Random line-ups replay, restore and rewind into the same match.
    ///
    /// <para>The 7.9 soak and its four replay oracles play the one official six-hero fixture, so every
    /// hero dealt since has been proved to replay, restore and rewind only in the single-seed journeys
    /// of its own dealing. This plays the line-ups milestone 9.6 draws from every dealt hero
    /// (<see cref="OfferOutcomeTests.RotatedLineups"/>) through <see cref="DeterministicMatchLoop"/>'s
    /// walk, and holds the match to three things along the way:</para>
    /// <list type="number">
    /// <item><b>Restore and continue.</b> At every command the live state is captured and restored,
    /// and the restored session must publish the same authoritative payload, offer exactly the commands
    /// the live one offers, project the same wire record to every seat, and take the command the seed
    /// draws to the same payload. Every <see cref="Stride"/> commands a fresh session restored there
    /// is also walked beside the live one for <see cref="ShadowHorizon"/> commands under the same
    /// checks.</item>
    /// <item><b>Replay and rewind.</b> Every <see cref="Stride"/> commands the Director Undo rebuilds the
    /// retained prefix and the undone command is submitted again; the bundled snapshot - authoritative
    /// state and rule-event log - must come back byte for byte. At the end of each run a fresh session
    /// replays the whole accepted prefix from setup to the same bundled bytes.</item>
    /// <item><b>Invariants live play has broken before</b>, at every accepted command: Life counters
    /// and coins inside their ledgers, one Tie Breaker, no standing decision without an owner who can
    /// answer it, no turn release standing over an unfinished resolution, no card or unit in two
    /// places, and - every <see cref="Stride"/> commands - every command the projection offers
    /// accepted when submitted.</item>
    /// </list>
    /// </summary>
    public sealed class RandomLineupSoakTests
    {
        /// <summary>
        /// Commands walked per line-up. The same length milestone 9.6's survey plays, long enough to
        /// cross several round boundaries in every line-up.
        /// </summary>
        private const int CommandsPerMatch = 300;

        /// <summary>
        /// How often the three expensive checks run: a fresh restored session walked beside the live
        /// one, the Director Undo rewind, and the submission of every offered command. Each costs a
        /// snapshot restore (about 7 ms) or a checkpoint replay per command it covers, so running them
        /// at every command put the ten line-ups at five minutes. At 15 the soak stays near the
        /// two-minute budget while every command still gets the cheap restore-and-continue step.
        /// </summary>
        private const int Stride = 15;

        /// <summary>
        /// How many commands each fresh restored session is walked beside the live match: two strides,
        /// so every command is continued by two sessions restored at different points.
        /// </summary>
        private const int ShadowHorizon = 30;

        /// <summary>Seed 1 for every line-up, the first seed milestone 9.6's survey plays them on.</summary>
        private static readonly int[] Seeds = { 1 };

        /// <summary>
        /// Milestone 9.9/9.10 landing: one soak per line-up, built by the first case that asks for that
        /// line-up and shared by its sibling case, so no single case pays for all ten and every case
        /// fits Unity's 180-second per-case budget. Each line-up plays exactly what it played inside
        /// the single soak: the same seeds, commands, stride and shadow horizon.
        /// </summary>
        private static readonly Lazy<Lazy<Soak>[]> Soaks = new Lazy<Lazy<Soak>[]>(() =>
            OfferOutcomeTests.RotatedLineups()
                .Select(picked => new Lazy<Soak>(() => Run(picked)))
                .ToArray());

        /// <summary>The line-ups, by index, as the per-line-up cases' source.</summary>
        public static IEnumerable<int> Lineups() => OfferOutcomeTests.Lineups();

        private static Soak Soaked(int lineup) => Soaks.Value[lineup].Value;

        [TestCaseSource(nameof(Lineups))]
        public void EveryRandomLineup_RestoresReplaysAndRewindsIntoTheSameMatch(int lineup)
        {
            foreach (var kind in Soaked(lineup).Findings
                .GroupBy(finding => finding.Substring(finding.IndexOf(']') + 1))
                .OrderByDescending(group => group.Count()))
            {
                TestContext.WriteLine($"{kind.Count()} x {kind.First()}");
            }

            Assert.That(Soaked(lineup).Findings, Is.Empty,
                "The soak found these disagreements between the live match and its restored, " +
                "replayed or rewound copies, or these broken invariants:\n" +
                string.Join("\n", Soaked(lineup).Findings.Take(40)));
        }

        /// <summary>
        /// Restated per line-up at the landing. Each clause is the single soak's, asked of one
        /// line-up's share: every hero of THIS line-up commits (stronger than "every dealt hero commits
        /// in some line-up", whose global half is <see cref="TheSoaksLineups_SeatEveryDealtHero"/>),
        /// this line-up walks more than half its own commands (the old floor was the sum of these), and
        /// its restores and rewinds keep the same proportion to its commands.
        /// </summary>
        [TestCaseSource(nameof(Lineups))]
        public void TheSoak_PlaysEveryDealtHeroAndWalksItsBudget(int lineup)
        {
            var missing = OfferOutcomeTests.RotatedLineups()[lineup]
                .Select(hero => hero.ContentId.Value)
                .Except(Soaked(lineup).HeroesCommitted)
                .ToArray();
            Assert.That(missing, Is.Empty,
                "These dealt heroes never committed a card in the soak's line-ups.");
            Assert.That(Soaked(lineup).CommandsWalked, Is.GreaterThan(
                    Seeds.Length * CommandsPerMatch / 2),
                "The soak is stopping early rather than walking its line-ups.");
            Assert.That(Soaked(lineup).Restores, Is.GreaterThan(Soaked(lineup).CommandsWalked / Stride / 2));
            Assert.That(Soaked(lineup).Rewinds, Is.GreaterThan(Soaked(lineup).CommandsWalked / Stride / 2));
            TestContext.WriteLine(
                $"9.10 soak: {Soaked(lineup).Matches} matches, {Soaked(lineup).CommandsWalked} commands, " +
                $"{Soaked(lineup).Restores} restores, {Soaked(lineup).Rewinds} rewinds, " +
                $"{Soaked(lineup).OffersSubmitted} offers submitted, {Soaked(lineup).ShadowSteps} shadow " +
                $"steps, {Soaked(lineup).Elapsed.TotalSeconds:F1}s at stride {Stride}.");
        }

        [Test]
        public void TheSoaksLineups_SeatEveryDealtHero()
        {
            var seated = OfferOutcomeTests.RotatedLineups().SelectMany(picked => picked)
                .Select(hero => hero.ContentId.Value);
            Assert.That(QuickGameSetupConfiguration.DealtHeroes.Select(hero => hero.ContentId.Value)
                    .Except(seated),
                Is.Empty, "Every dealt hero plays in one of the soak's line-ups.");
        }

        /// <summary>
        /// Found by this soak: the snapshot writes the board's heroes, obstacles, minions and tokens in
        /// id order, the live board keeps them in setup and arrival order, and the projection read the
        /// board's own order - so a restored session, and a live one after an Undo rebuilt from a
        /// retained point, projected the same match with its board lists in another order, on the wire
        /// to every seat as well. Walked on the official fixture so tokens and respawned units are
        /// among them.
        /// </summary>
        [Test]
        public void ARestoredSession_ProjectsItsBoardInTheLiveSessionsOrder()
        {
            var live = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var random = new MatchLoopRandom(7);
            for (var step = 0; step <= 200; step++)
            {
                if (step % 50 == 0)
                {
                    var restored = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
                    Assert.That(restored.RestoreSnapshot(live.CaptureSnapshot()).Accepted, Is.True);
                    var projected = restored.Projection;
                    Assert.That(projected.Heroes, Is.EqualTo(live.Projection.Heroes), $"heroes at step {step}");
                    Assert.That(projected.Obstacles, Is.EqualTo(live.Projection.Obstacles), $"obstacles at step {step}");
                    Assert.That(projected.Minions.Select(minion => $"{minion.MinionId} {minion.Coordinate}"),
                        Is.EqualTo(live.Projection.Minions.Select(minion => $"{minion.MinionId} {minion.Coordinate}")),
                        $"minions at step {step}");
                    Assert.That(projected.PlacedTokens.Select(token => token.Id),
                        Is.EqualTo(live.Projection.PlacedTokens.Select(token => token.Id)), $"tokens at step {step}");
                    Assert.That(SeatDifference(projected, live.Projection), Is.Null, $"step {step}");
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(live.Projection);
                Assert.That(live.Execute(legal[random.Next(legal.Count)]).Accepted, Is.True);
            }
        }

        /// <summary>
        /// Found by this soak on its longer walks: the same defect in the held markers. Snorri's
        /// Inscribe the Runes re-seats his four runes in turn-slot order, the snapshot writes them by
        /// id, and the projection read the register's own order - so after a restore his runes reached
        /// every seat in another order. Seed 5 of this line-up re-seats them by command 433.
        /// </summary>
        [Test]
        public void ARestoredSession_ProjectsHeldMarkersInTheLiveSessionsOrder()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var picked = new[] { "min", "brynn", "hanu", "ignatia", "trinkets", "snorri" }
                .Select(name => QuickGameSetupConfiguration.DealtHeroes.Single(hero => hero.ContentId.Value == name))
                .ToArray();
            var initial = OfferOutcomeTests.Compose(map, picked);
            var live = new MatchSession(initial);
            var random = new MatchLoopRandom(5);
            for (var step = 0; step < 433; step++)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(live.Projection);
                Assert.That(live.Execute(legal[random.Next(legal.Count)]).Accepted, Is.True);
            }

            Assert.That(live.Projection.HeldMarkers.Count(marker => marker.Definition.Id.Value.StartsWith(
                    "snorri-rune", StringComparison.Ordinal)), Is.GreaterThan(1),
                "The walk must reach Snorri holding his runes; if a later change reroutes it, find the " +
                "re-seated runes again rather than weakening this case.");
            var restored = new MatchSession(initial);
            Assert.That(restored.RestoreSnapshot(live.CaptureSnapshot()).Accepted, Is.True);
            Assert.That(restored.Projection.HeldMarkers.Select(marker => marker.Id.Value),
                Is.EqualTo(live.Projection.HeldMarkers.Select(marker => marker.Id.Value)));
            Assert.That(SeatDifference(restored.Projection, live.Projection), Is.Null);
        }

        private sealed class Soak
        {
            public readonly List<string> Findings = new List<string>();
            public readonly HashSet<string> HeroesCommitted = new HashSet<string>(StringComparer.Ordinal);
            public int Matches;
            public int CommandsWalked;
            public int Restores;
            public int Rewinds;
            public int OffersSubmitted;
            public int ShadowSteps;
            public TimeSpan Elapsed;
        }

        private static Soak Run(RegisteredHero[] picked)
        {
            var soak = new Soak();
            var clock = Stopwatch.StartNew();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            foreach (var seed in Seeds)
            {
                Play(soak, OfferOutcomeTests.Compose(map, picked), picked, seed);
            }

            soak.Elapsed = clock.Elapsed;
            return soak;
        }

        private static void Play(Soak soak, MatchSessionInitialState initial, RegisteredHero[] picked, int seed)
        {
            soak.Matches++;
            var names = string.Join(",", picked.Select(hero => hero.ContentId.Value));
            var live = new MatchSession(initial);
            var scratch = new MatchSession(initial);
            var random = new MatchLoopRandom(seed);
            var ledger = new Ledger(live.Projection);
            var shadows = new List<Shadow>();

            for (var step = 0; step < CommandsPerMatch &&
                 live.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
            {
                var where = $"[{names} seed {seed} step {step}]";
                var projection = live.Projection;
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                if (legal.Count == 0)
                {
                    soak.Findings.Add($"{where} a live match offered no legal command.");
                    return;
                }

                var snapshot = live.CaptureSnapshot();
                if (step % Stride == 0)
                {
                    SubmitEveryOffer(soak, scratch, snapshot, projection, legal, where);

                    var restored = new MatchSession(initial);
                    if (RestoresTo(soak, restored, snapshot, projection, legal, where))
                    {
                        shadows.Add(new Shadow(restored, step));
                    }
                }

                // Every command: the captured state restored into the scratch session publishes the
                // same payload and offers the same commands, and takes the drawn one to the same place.
                var scratchRestored = RestoresTo(soak, scratch, snapshot, projection, legal, where);

                for (var index = shadows.Count - 1; index >= 0; index--)
                {
                    var offered = DeterministicMatchLoop.EnumerateLegalCommands(shadows[index].Session.Projection);
                    if (!offered.SequenceEqual(legal))
                    {
                        soak.Findings.Add(
                            $"{where} a session restored at step {shadows[index].RestoredAt} offers " +
                            $"[{Describe(offered)}] where the live match offers [{Describe(legal)}].");
                        shadows.RemoveAt(index);
                    }
                }

                var chosen = legal[random.Next(legal.Count)];
                var result = live.Execute(chosen);
                if (!result.Accepted)
                {
                    soak.Findings.Add($"{where} offered {chosen.Kind} was refused: {result.Detail}");
                    return;
                }

                soak.CommandsWalked++;
                if (chosen.Kind == DirectorCommandKind.CommitCard)
                {
                    soak.HeroesCommitted.Add(HeroContentOf(projection, chosen.CardInstanceId));
                }

                if (scratchRestored)
                {
                    Continues(soak, scratch, live, chosen, $"{where} the session restored here");
                }

                for (var index = shadows.Count - 1; index >= 0; index--)
                {
                    soak.ShadowSteps++;
                    var shadow = shadows[index];
                    if (!Continues(soak, shadow.Session, live, chosen,
                            $"{where} the session restored at step {shadow.RestoredAt}") ||
                        step - shadow.RestoredAt + 1 >= ShadowHorizon)
                    {
                        shadows.RemoveAt(index);
                    }
                }

                if ((step + 1) % Stride == 0)
                {
                    Rewind(soak, live, chosen, where);
                }

                foreach (var finding in ledger.Check(projection, chosen, live.Projection))
                {
                    soak.Findings.Add($"{where} after {chosen.Kind}: {finding}");
                }
            }

            ReplayFromSetup(soak, initial, live, names, seed);
        }

        private sealed class Shadow
        {
            public Shadow(MatchSession session, int restoredAt)
            {
                Session = session;
                RestoredAt = restoredAt;
            }

            public MatchSession Session { get; }
            public int RestoredAt { get; }
        }

        /// <summary>
        /// Restores the captured state and asks that the restored session publish the same payload
        /// and offer the same commands as the live one it was captured from.
        /// </summary>
        private static bool RestoresTo(
            Soak soak, MatchSession session, string snapshot, MatchSessionProjection live,
            IReadOnlyList<DirectorCommand> legal, string where)
        {
            soak.Restores++;
            var restoration = session.RestoreSnapshot(snapshot);
            if (!restoration.Accepted)
            {
                soak.Findings.Add($"{where} the live state did not restore: {restoration.Detail}");
                return false;
            }

            if (session.Projection.AuthoritativeSnapshot != live.AuthoritativeSnapshot)
            {
                soak.Findings.Add(
                    $"{where} the restored payload differs from the captured one: " +
                    FirstDifference(session.Projection.AuthoritativeSnapshot, live.AuthoritativeSnapshot));
                return false;
            }

            var offered = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
            if (!offered.SequenceEqual(legal))
            {
                soak.Findings.Add(
                    $"{where} the restored session offers [{Describe(offered)}] where the live match " +
                    $"offers [{Describe(legal)}].");
                return false;
            }

            var seatDifference = SeatDifference(session.Projection, live);
            if (seatDifference != null)
            {
                soak.Findings.Add($"{where} the restored session projects {seatDifference}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Where two projections of what should be one match differ as a seat receives it on the wire,
        /// or <c>null</c>. The leading projection version is left out: it counts this session's own
        /// publications, and a restored session starts counting again.
        /// </summary>
        private static string SeatDifference(MatchSessionProjection left, MatchSessionProjection right)
        {
            foreach (var seat in MatchSeatBinding.ForParticipants(right.Participants).Seats)
            {
                var leftBytes = NetworkSeatProjectionCodec.Encode(SeatProjectionFactory.ForSeat(left, seat));
                var rightBytes = NetworkSeatProjectionCodec.Encode(SeatProjectionFactory.ForSeat(right, seat));
                var at = sizeof(long);
                while (at < leftBytes.Length && at < rightBytes.Length && leftBytes[at] == rightBytes[at])
                {
                    at++;
                }

                if (at != leftBytes.Length || at != rightBytes.Length)
                {
                    return $"a different wire projection for seat {seat.Id} from byte {at} " +
                        $"({leftBytes.Length}/{rightBytes.Length} bytes).";
                }
            }

            return null;
        }

        /// <summary>The same command, taken by a restored session, reaches the live match's payload.</summary>
        private static bool Continues(
            Soak soak, MatchSession session, MatchSession live, DirectorCommand chosen, string who)
        {
            var continued = session.Execute(chosen);
            if (!continued.Accepted)
            {
                soak.Findings.Add($"{who} refused {chosen.Kind} the live match accepted: {continued.Detail}");
                return false;
            }

            if (session.Projection.AuthoritativeSnapshot != live.Projection.AuthoritativeSnapshot)
            {
                soak.Findings.Add(
                    $"{who} continued with {chosen.Kind} to a different authoritative payload than the " +
                    "live match: " + FirstDifference(
                        session.Projection.AuthoritativeSnapshot, live.Projection.AuthoritativeSnapshot));
                return false;
            }

            return true;
        }

        private static void ReplayFromSetup(
            Soak soak, MatchSessionInitialState initial, MatchSession live, string names, int seed)
        {
            var replayed = new MatchSession(initial);
            var commands = live.Projection.AcceptedCommands;
            for (var index = 0; index < commands.Count; index++)
            {
                var again = replayed.Execute(commands[index]);
                if (!again.Accepted)
                {
                    soak.Findings.Add(
                        $"[{names} seed {seed}] replaying command {index + 1} ({commands[index].Kind}) " +
                        $"from setup was refused: {again.Detail}");
                    return;
                }
            }

            if (replayed.CaptureSnapshot() != live.CaptureSnapshot())
            {
                soak.Findings.Add(
                    $"[{names} seed {seed}] the whole accepted prefix replayed from setup to a different " +
                    "bundled state or event log than the live match.");
            }
        }

        /// <summary>
        /// The Director Undo rebuilds the retained prefix without the latest command; submitting that
        /// command again must reproduce the live state and its event log byte for byte.
        /// </summary>
        private static void Rewind(Soak soak, MatchSession live, DirectorCommand latest, string where)
        {
            soak.Rewinds++;
            var before = live.CaptureSnapshot();
            var beforeProjection = live.Projection;
            var undone = live.Undo();
            if (!undone.Accepted)
            {
                soak.Findings.Add($"{where} Undo after {latest.Kind} was refused: {undone.Detail}");
                return;
            }

            var redone = live.Execute(latest);
            if (!redone.Accepted)
            {
                soak.Findings.Add(
                    $"{where} {latest.Kind} was refused after an Undo of itself: {redone.Detail}");
                return;
            }

            var after = live.CaptureSnapshot();
            if (after != before)
            {
                soak.Findings.Add(
                    $"{where} Undo then {latest.Kind} again did not reproduce the bundled state and " +
                    "event log: " + FirstDifference(after, before));
                return;
            }

            var seatDifference = SeatDifference(live.Projection, beforeProjection);
            if (seatDifference != null)
            {
                soak.Findings.Add($"{where} Undo then {latest.Kind} again projects {seatDifference}");
            }
        }

        /// <summary>Every command the projection offers, each submitted to its own restored copy.</summary>
        private static void SubmitEveryOffer(
            Soak soak, MatchSession copy, string snapshot, MatchSessionProjection projection,
            IReadOnlyList<DirectorCommand> legal, string where)
        {
            var offered = legal.Concat(projection.DecisionContract?.Commands ?? Array.Empty<DirectorCommand>())
                .Distinct()
                .ToArray();
            foreach (var command in offered)
            {
                soak.OffersSubmitted++;
                copy.RestoreSnapshot(snapshot);
                var submitted = copy.Execute(command);
                if (!submitted.Accepted)
                {
                    soak.Findings.Add($"{where} offered {command.Kind} was refused: {submitted.Detail}");
                }
            }
        }

        private static string HeroContentOf(MatchSessionProjection projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(candidate => candidate.Id.Equals(cardId));
            var definition = projection.Definitions.Single(
                candidate => candidate.Reference.Equals(card.DefinitionReference));
            return definition.Id.Value.Split('-')[0];
        }

        private static string Describe(IReadOnlyList<DirectorCommand> commands) =>
            string.Join("; ", commands.Take(6).Select(command => command.ToString())) +
            (commands.Count > 6 ? $"; ... {commands.Count} in all" : string.Empty);

        private static string FirstDifference(string left, string right)
        {
            var length = Math.Min(left.Length, right.Length);
            var at = 0;
            while (at < length && left[at] == right[at])
            {
                at++;
            }

            var from = Math.Max(0, at - 60);
            return $"first difference at {at} of {left.Length}/{right.Length}: " +
                $"'{Excerpt(left, from)}' vs '{Excerpt(right, from)}'";
        }

        private static string Excerpt(string value, int from) =>
            from >= value.Length ? string.Empty : value.Substring(from, Math.Min(140, value.Length - from));

        /// <summary>
        /// The invariants read off consecutive projections: what a state may be, and how it may have
        /// changed since the one before it.
        /// </summary>
        private sealed class Ledger
        {
            private readonly MatchParticipants participants;
            private readonly MatchSeatBinding seats;
            private readonly Dictionary<TeamId, int> startingLives;

            public Ledger(MatchSessionProjection initial)
            {
                participants = initial.Participants;
                seats = MatchSeatBinding.ForParticipants(participants);
                startingLives = initial.TeamLifeCounters.ToDictionary(entry => entry.Key, entry => entry.Value);
            }

            public IEnumerable<string> Check(
                MatchSessionProjection before, DirectorCommand command, MatchSessionProjection after)
            {
                return Lives(before, after)
                    .Concat(Coins(before, after))
                    .Concat(TieBreaker(after))
                    .Concat(DecisionOwner(after))
                    .Concat(TurnRelease(before, after))
                    .Concat(Places(after));
            }

            private IEnumerable<string> Lives(MatchSessionProjection before, MatchSessionProjection after)
            {
                if (!after.TeamLifeCounters.Select(entry => entry.Key).OrderBy(id => id.Value)
                        .SequenceEqual(startingLives.Keys.OrderBy(id => id.Value)))
                {
                    yield return "the Life counters no longer name exactly the match's teams.";
                    yield break;
                }

                var deathsBefore = DeathsByTeam(before);
                var deathsAfter = DeathsByTeam(after);
                foreach (var entry in after.TeamLifeCounters)
                {
                    var previous = before.TeamLifeCounters.Single(value => value.Key.Equals(entry.Key)).Value;
                    if (entry.Value < 0 || entry.Value > startingLives[entry.Key])
                    {
                        yield return $"team {entry.Key} holds {entry.Value} Life counters of " +
                            $"{startingLives[entry.Key]}.";
                    }

                    if (entry.Value > previous)
                    {
                        yield return $"team {entry.Key} regained Life counters ({previous} -> {entry.Value}).";
                    }

                    if (entry.Value < previous && deathsAfter[entry.Key] <= deathsBefore[entry.Key])
                    {
                        yield return $"team {entry.Key} spent Life counters ({previous} -> {entry.Value}) " +
                            "with no Defeat of its heroes recorded.";
                    }

                    if (entry.Value == 0 && !after.Outcome.HasEnded)
                    {
                        yield return $"team {entry.Key} has no Life counters left and the match goes on.";
                    }
                }
            }

            private Dictionary<TeamId, int> DeathsByTeam(MatchSessionProjection projection)
            {
                var deaths = startingLives.Keys.ToDictionary(team => team, team => 0);
                foreach (var entry in projection.Scoreboard)
                {
                    deaths[participants.GetTeamForHero(entry.HeroId).Id] += entry.Deaths;
                }

                return deaths;
            }

            private IEnumerable<string> Coins(MatchSessionProjection before, MatchSessionProjection after)
            {
                foreach (var hero in after.HeroProgression.Where(hero => hero.Coins < 0))
                {
                    yield return $"{hero.HeroId} holds {hero.Coins} coins.";
                }

                foreach (var entry in after.Scoreboard)
                {
                    var previous = before.Scoreboard.SingleOrDefault(value => value.HeroId.Equals(entry.HeroId));
                    if (previous != null && (entry.CoinsEarned < previous.CoinsEarned ||
                                             entry.Deaths < previous.Deaths || entry.Kills < previous.Kills))
                    {
                        yield return $"a scoreboard total went down: {previous} -> {entry}.";
                    }
                }

                var held = after.HeroProgression.Sum(hero => hero.Coins) -
                    before.HeroProgression.Sum(hero => hero.Coins);
                var earned = after.Scoreboard.Sum(entry => entry.CoinsEarned) -
                    before.Scoreboard.Sum(entry => entry.CoinsEarned);
                if (held > earned)
                {
                    yield return $"the heroes' coins grew by {held} while the coins earned grew by {earned}.";
                }
            }

            private static IEnumerable<string> TieBreaker(MatchSessionProjection after)
            {
                if (!after.TieBreakerState.IsDefined)
                {
                    yield return $"the Tie Breaker is held by {after.TieBreakerState.RevealedSide}, not by one team.";
                }
            }

            private IEnumerable<string> DecisionOwner(MatchSessionProjection after)
            {
                if (after.DecisionContract == null)
                {
                    yield break;
                }

                if (after.Phase == RoundResolutionPhase.GameEnded)
                {
                    yield return $"a {after.DecisionContract.FamilyId.Value} decision stands in an ended match.";
                    yield break;
                }

                PendingDecisionOwner owner;
                string fault = null;
                try
                {
                    owner = SeatProjectionFactory.OwnerOfStandingDecision(after);
                }
                catch (Exception exception)
                {
                    owner = PendingDecisionOwner.None;
                    fault = exception.GetType().Name + ": " + exception.Message;
                }

                if (fault != null || !owner.IsDefined)
                {
                    yield return $"the standing {after.DecisionContract.FamilyId.Value} decision has no owner" +
                        (fault == null ? "." : $" ({fault}).");
                    yield break;
                }

                var owning = seats.Seats.Where(owner.IsOwnedBy).ToArray();
                if (owning.Length == 0)
                {
                    yield return $"the standing decision's owner {owner} is no seat in this match.";
                    yield break;
                }

                // A tied Initiative and a Minion Battle's confirmation are answered through the host's
                // team agreement rather than by one seat's command, so a seat is not handed them.
                if (owner.FamilyId.Equals(PendingDecisionFamilies.Initiative) ||
                    owner.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))
                {
                    yield break;
                }

                if (owning.All(seat => SeatProjectionFactory.AuthorizedCommands(after, seat, owner).Count == 0))
                {
                    yield return $"the standing decision {owner} authorizes none of its owners' seats to answer.";
                }
            }

            private static IEnumerable<string> TurnRelease(MatchSessionProjection before, MatchSessionProjection after)
            {
                // A turn that has moved on leaves nothing of its card still resolving.
                if (before.SelectedActor.IsDefined && after.ResolvingSourceCardInstanceId.IsDefined &&
                    after.ResolvingSourceCardInstanceId.Equals(before.SelectedActor.CardInstanceId) &&
                    (!after.SelectedActor.IsDefined || after.TurnIndex != before.TurnIndex ||
                     after.RoundIndex != before.RoundIndex))
                {
                    yield return $"the turn moved on while {before.SelectedActor.CardInstanceId} is still resolving.";
                }

                if (after.PendingTurnRelease == null)
                {
                    yield break;
                }

                if (after.DecisionContract == null ||
                    !after.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
                {
                    yield return "a turn release is held while the standing decision is " +
                        (after.DecisionContract?.FamilyId.Value ?? "none") + ".";
                }

                var standing = new (string Name, object Value)[]
                {
                    ("Movement resolution", after.PendingMovementResolution),
                    ("Movement choice", after.PendingMovementChoice),
                    ("Attack resolution", after.PendingAttackResolution),
                    ("Attack target", after.PendingAttackTargetChoice),
                    ("Defense", after.PendingDefenseChoice),
                    ("Skill resolution", after.PendingSkillResolution),
                    ("effect choice", after.PendingEffectChoice),
                    ("removal prevention", after.PendingRemovalPrevention),
                    ("immediate Lane Push", after.PendingImmediateLanePush)
                }.Where(entry => entry.Value != null).Select(entry => entry.Name).ToArray();
                if (standing.Length > 0)
                {
                    yield return $"the turn release stands over an unfinished {string.Join(", ", standing)}.";
                }

                if (!after.Participants.Heroes.Any(hero => hero.Id.Equals(after.PendingTurnRelease.ActorId)))
                {
                    yield return $"the turn release names {after.PendingTurnRelease.ActorId}, who is no hero here.";
                }
            }

            private static IEnumerable<string> Places(MatchSessionProjection after)
            {
                foreach (var duplicate in after.Cards.GroupBy(card => card.Id).Where(group => group.Count() > 1))
                {
                    yield return $"card {duplicate.Key} is carried {duplicate.Count()} times.";
                }

                foreach (var card in after.Cards.Where(card => card.State.Location == CardLocation.None))
                {
                    yield return $"card {card.Id} is nowhere.";
                }

                foreach (var owner in after.Cards
                    .Where(card => card.State.Location == CardLocation.TurnArea &&
                                   card.State.ResolutionStatus == CardResolutionStatus.Unresolved)
                    .GroupBy(card => card.OwnerId)
                    .Where(group => group.Count() > 1))
                {
                    yield return $"{owner.Key} has {owner.Count()} unresolved cards in play: " +
                        string.Join(", ", owner.Select(card => card.Id)) + ".";
                }

                if (after.IsTurnActive && after.SelectedActor.IsDefined)
                {
                    var selected = after.Cards.SingleOrDefault(card => card.Id.Equals(after.SelectedActor.CardInstanceId));
                    if (selected == null)
                    {
                        yield return $"the selected card {after.SelectedActor.CardInstanceId} does not exist.";
                    }
                    else if (selected.State.Location != CardLocation.TurnArea)
                    {
                        yield return $"the selected card {selected.Id} is resolving from {selected.State.Location}.";
                    }
                }

                foreach (var hero in after.Heroes.GroupBy(value => value.HeroId).Where(group => group.Count() > 1))
                {
                    yield return $"hero {hero.Key} stands in {hero.Count()} places.";
                }

                var occupied = after.Heroes.Select(hero => (Unit: "hero " + hero.HeroId, hero.Coordinate))
                    .Concat(after.Minions.Select(minion => (Unit: "minion " + minion.MinionId, minion.Coordinate)))
                    .Concat(after.Obstacles.Select(obstacle => (Unit: "obstacle " + obstacle.ObstacleId, obstacle.Coordinate)));
                foreach (var space in occupied.GroupBy(entry => entry.Coordinate).Where(group => group.Count() > 1))
                {
                    yield return $"{string.Join(" and ", space.Select(entry => entry.Unit))} share {space.Key}.";
                }
            }
        }
    }
}
