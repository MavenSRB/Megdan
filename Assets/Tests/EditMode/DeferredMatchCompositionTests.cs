using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z37. The recomposition seam's central claim, measured: a match composed
    /// LATE, from a line-up a lobby chose, is the same match composing it at scene load produces.
    /// </summary>
    /// <remarks>
    /// <para><b>Why the instrument is a snapshot payload and not a field-by-field walk.</b> The
    /// claim being defended is not "the two compositions agree about the things I thought to check";
    /// it is "nothing observable differs". The authoritative snapshot is the build's own complete
    /// statement of a match - every hand, every board occupancy, every registration, every counter -
    /// and comparing two of them character for character is the only comparison that cannot be
    /// weakened by forgetting a field. A walk would have to be extended by every later milestone
    /// that adds state; this cannot.</para>
    ///
    /// <para><b>The randomness is held equal rather than avoided.</b> Milestone 8.28b made every
    /// setup draw reproducible from a recorded seed, so both sides of each comparison are composed
    /// from ONE drawn seed and ONE coin. That is what makes the comparison about composition timing
    /// rather than about the draw - and a fixture that instead compared two freshly drawn matches
    /// would be measuring the coin.</para>
    /// </remarks>
    [TestFixture]
    public sealed class DeferredMatchCompositionTests
    {
        /// <summary>
        /// THE PROOF. For every printed format, composing the printed line-up through the
        /// recomposition seam produces a snapshot byte-identical to the one the shipped
        /// scene-load path produces.
        /// </summary>
        /// <remarks>
        /// This is what lets the scene stop composing in <c>Awake</c> without changing what any
        /// existing journey, fixture or player sees: the deferred path is not "close enough to" the
        /// composed-at-load path, it is indistinguishable from it.
        /// </remarks>
        [Test]
        public void ComposedLate_IsByteIdenticalToComposedAtLoad_OnEveryPrintedFormat()
        {
            foreach (var format in MatchFormat.All)
            {
                var seed = SetupRandomnessAuthority.DrawSeed();
                var coin = TieBreakerSetupAuthority.CreateNormalSetupState(seed);

                var atLoad = SnapshotOfShippedPath(format, coin, seed);
                var late = SnapshotOfPlannedPath(
                    MatchCompositionPlan.Printed(format), coin, seed);

                Assert.That(
                    late, Is.EqualTo(atLoad),
                    "A " + format.Id + " match composed from the printed line-up through the " +
                    "recomposition seam must be the same match the scene composes at load.");
            }
        }

        /// <summary>
        /// The same equality with the coin pinned to each side in turn, so the proof above cannot
        /// have passed on a draw that happened to land the same way twice.
        /// </summary>
        [Test]
        public void ComposedLate_IsByteIdenticalToComposedAtLoad_UnderEitherCoin()
        {
            foreach (var format in MatchFormat.All)
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var coin = new TieBreakerState(side);
                    var seed = SetupRandomnessSeed.Unrecorded;

                    Assert.That(
                        SnapshotOfPlannedPath(MatchCompositionPlan.Printed(format), coin, seed),
                        Is.EqualTo(SnapshotOfShippedPath(format, coin, seed)),
                        "A " + format.Id + " match with the coin on " + side +
                        " must compose the same either way round.");
                }
            }
        }

        /// <summary>
        /// A plan built with no line-up composes setup step 7's printed one - the backward
        /// compatibility guarantee, stated as the default rather than as a branch.
        /// </summary>
        [Test]
        public void APlanWithNoLineup_IsThePrintedLineup()
        {
            foreach (var format in MatchFormat.All)
            {
                var plan = MatchCompositionPlan.Printed(format);

                Assert.That(plan.IsPrintedLineup, Is.True);
                Assert.That(plan.Lineup.HeroesPerTeam, Is.EqualTo(format.HeroesPerTeam));
                CollectionAssert.AreEqual(
                    RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam)
                        .Red.Select(hero => hero.HeroIdValue).ToArray(),
                    plan.Lineup.Red.Select(hero => hero.HeroIdValue).ToArray());
                CollectionAssert.AreEqual(
                    RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam)
                        .Blue.Select(hero => hero.HeroIdValue).ToArray(),
                    plan.Lineup.Blue.Select(hero => hero.HeroIdValue).ToArray());
            }
        }

        /// <summary>
        /// A plan refuses a line-up that does not field the format's team size, rather than
        /// composing a board with a seat nobody stands on.
        /// </summary>
        [Test]
        public void APlanRefusesALineupThatDoesNotFitItsFormat()
        {
            Assert.Throws<ArgumentException>(() => new MatchCompositionPlan(
                MatchFormat.ThreeVersusThreeQuick, RegisteredHeroRoster.PrintedLineup(2)));
            Assert.Throws<ArgumentException>(() => new MatchCompositionPlan(
                MatchFormat.TwoVersusTwoQuick, RegisteredHeroRoster.PrintedLineup(3)));
        }

        /// <summary>
        /// THE SEAT INVARIANT the whole seam rests on: a chosen line-up seats exactly the seats the
        /// printed one does - same ids, same sides, same order - because a seat id is the FORMAT's
        /// positional player identity and a line-up cannot change the format.
        /// </summary>
        /// <remarks>
        /// It is what makes recomposing a running session safe: every connection, epoch and
        /// reconnect capability the host holds is keyed by seat, so a match whose seats did not move
        /// is a match every live connection is still correctly attached to. Asserted against a
        /// deliberately SHUFFLED line-up rather than the printed one, because the printed one would
        /// pass this check even if the seats followed the heroes.
        /// </remarks>
        [Test]
        public void AChosenLineupSeatsExactlyTheSeatsThePrintedOneDoes()
        {
            foreach (var format in MatchFormat.All)
            {
                var printed = SeatRowsOf(MatchCompositionPlan.Printed(format));
                var chosen = SeatRowsOf(
                    new MatchCompositionPlan(format, ReversedLineup(format.HeroesPerTeam)));

                CollectionAssert.AreEqual(printed, chosen,
                    "A " + format.Id + " line-up moved a seat, which would reattach a live " +
                    "connection to another player's hero.");
            }
        }

        /// <summary>
        /// A chosen line-up genuinely composes a DIFFERENT match - so the equality proved above is a
        /// property of composing the printed line-up, and not of the seam ignoring its input.
        /// </summary>
        /// <remarks>
        /// The negative half matters more than it looks. Every equality assertion in this fixture
        /// would also pass if <c>CreateForLineup</c> quietly dealt the printed sets and threw the
        /// line-up away, which is precisely the defect a suite of equalities cannot see.
        /// </remarks>
        [Test]
        public void AChosenLineupComposesADifferentMatch()
        {
            var format = MatchFormat.ThreeVersusThreeQuick;
            var coin = new TieBreakerState(TeamSide.Red);
            var seed = SetupRandomnessSeed.Unrecorded;

            var printed = SnapshotOfPlannedPath(
                MatchCompositionPlan.Printed(format), coin, seed);
            var chosen = SnapshotOfPlannedPath(
                new MatchCompositionPlan(format, ReversedLineup(format.HeroesPerTeam)),
                coin, seed);

            Assert.That(chosen, Is.Not.EqualTo(printed),
                "Reversing the line-up must change the match, or the seam is ignoring it.");
        }

        /// <summary>
        /// Which hero stands on which marked point follows the SEAT, not the hero: the Nth hero of a
        /// team takes the Nth marked point whichever hero it is.
        /// </summary>
        [Test]
        public void ASpawnPointBelongsToTheSeatAndNotToTheHero()
        {
            var format = MatchFormat.ThreeVersusThreeQuick;
            var printed = HeroLineupComposition.Compose(
                format, RegisteredHeroRoster.PrintedLineup(3));
            var reversed = HeroLineupComposition.Compose(format, ReversedLineup(3));

            CollectionAssert.AreEqual(
                printed.Heroes.Select(hero => hero.SpawnPoint.SpawnPointId.Value).ToArray(),
                reversed.Heroes.Select(hero => hero.SpawnPoint.SpawnPointId.Value).ToArray(),
                "The marked points are the board's, so reversing who stands on them must not " +
                "renumber them.");

            Assert.That(
                printed.Heroes.Select(hero => hero.HeroId.Value).ToArray(),
                Is.Not.EqualTo(reversed.Heroes.Select(hero => hero.HeroId.Value).ToArray()),
                "...while WHO stands on each of them is exactly what changed.");
        }

        // ---- the two composition paths, written out so the comparison is between paths ----

        /// <summary>
        /// What the scene has always done: the official setup for the format, and the official
        /// configuration for the format. Neither knows a line-up exists.
        /// </summary>
        private static string SnapshotOfShippedPath(
            MatchFormat format, TieBreakerState coin, SetupRandomnessSeed seed)
        {
            var configuration = MatchConfiguration.CreateOfficialFromRecord(format, coin, seed);
            return SnapshotOf(QuickGameSetupConfiguration.CreateOfficial(format), configuration);
        }

        /// <summary>
        /// What the recomposition seam does: both halves composed from the plan's line-up.
        /// </summary>
        private static string SnapshotOfPlannedPath(
            MatchCompositionPlan plan, TieBreakerState coin, SetupRandomnessSeed seed)
        {
            var configuration = MatchConfiguration.CreateForLineupFromRecord(
                plan.Format, plan.Lineup, coin, seed);
            return SnapshotOf(
                HeroLineupComposition.Compose(plan.Format, plan.Lineup), configuration);
        }

        private static string SnapshotOf(
            QuickGameSetupConfiguration setupConfiguration, MatchConfiguration configuration)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                setupConfiguration,
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            // The RECORDED SEED is passed deliberately. Milestone 8.28b put it in the scenario so a
            // composed match can be dealt again from its record, which means it is part of what a
            // snapshot states - and leaving it out would have compared everything about the two
            // compositions except the one input this fixture holds equal on purpose.
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(
                new MatchSessionInitialState(scenario, setup, new[] { map }));
            return session.CaptureSnapshot();
        }

        private static IReadOnlyList<string> SeatRowsOf(MatchCompositionPlan plan)
        {
            var participants = HeroLineupComposition.ComposeParticipants(plan.Lineup);
            return MatchSeatBinding.ForParticipants(participants).Seats
                .Select(seat => seat.Id.Value + "/" + seat.TeamSide)
                .ToArray();
        }

        /// <summary>
        /// The printed line-up with each team's heroes in the opposite order - a genuinely different
        /// arrangement built only from heroes this build registers, so it needs no invented content.
        /// </summary>
        private static HeroLineup ReversedLineup(int heroesPerTeam)
        {
            var printed = RegisteredHeroRoster.PrintedLineup(heroesPerTeam);
            return new HeroLineup(
                printed.Red.Reverse().ToArray(), printed.Blue.Reverse().ToArray());
        }
    }
}
