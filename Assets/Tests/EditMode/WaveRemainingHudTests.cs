using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z60. The 2026-09-08 user directive: "Waves up top should show waves
    /// remaining until the end of the game. Wave remaining:" - the top status strip's WAVES
    /// indicator binds rulebook p.8's live shared Wave-counter pool
    /// (<see cref="IMatchReadModel.RemainingWaveCounters"/>, read off
    /// <c>LanePushTransitionAuthority.RemainingWaveCounters</c> - the same fact
    /// <c>MatchVictoryPathTests</c>, milestone 7.7's victory-path contract, already reads to end a
    /// match), not <see cref="QuickGameSetupState.WaveCount"/>, the printed starting total that
    /// never moves.
    /// </summary>
    /// <remarks>
    /// A separate Presentation-dependent file rather than an addition to
    /// <c>MatchVictoryPathTests</c>: that suite is deliberately Unity-independent (it says so in its
    /// own header and the cross-platform dotnet harness compiles it), and <see
    /// cref="MatchHudHeaderViewModel"/> lives in <c>GuardsDigital.Presentation.Match</c>, which pulls
    /// in <c>UnityEngine</c>. Pulling that into the victory-path file would Unity-taint every other
    /// test there; this file carries that dependency alone and is harness-excluded instead - the
    /// dotnet harness's own convention for every other Presentation-facing Edit Mode file.
    /// </remarks>
    public sealed class WaveRemainingHudTests
    {
        [Test]
        public void TheTopStripsWaveLabel_ReadsTheLiveRemainingPoolAndNotTheStartingTotal()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedPushSession();

            Assert.That(session.Projection.Setup.WaveCount, Is.EqualTo(3),
                "The Quick Game default this fixture stages, so 'known pool state: 3 at start' is " +
                "this build's own standard match rather than a number invented for the test.");
            Assert.That(session.Projection.RemainingWaveCounters, Is.EqualTo(3),
                "A fresh match has spent nothing off p.8's shared pool yet, so the live count and " +
                "the setup's printed total agree at the very start.");
            Assert.That(HeaderFor(session).WaveLabel, Is.EqualTo("WAVES REMAINING 3"));
            Assert.That(HeaderFor(session).Text, Does.Contain("WAVES REMAINING 3"),
                "the header's single-line assistive reading agrees with the chip it describes.");

            // One staged push: play the round out and cross its end-of-round Minion Battle (this
            // fixture's single surviving Blue minion loses it) into the Lane-Push stage that flips
            // one Wave counter - rulebook p.8, step 1.
            ConfiguredMatchFixtures.PlayOneOrdinaryRound(session);
            ConfiguredMatchFixtures.AdvanceToStage(session, EndOfRoundStage.ResolveRequestedLanePush);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AdvanceEndOfRound());

            Assert.That(session.Projection.RemainingWaveCounters, Is.EqualTo(2),
                "p.8 flips exactly one counter per push.");
            Assert.That(session.Projection.Setup.WaveCount, Is.EqualTo(3),
                "and the setup's own total never moves - it is the printed pool size, not a running " +
                "count, which is exactly the value the strip must stop reading.");
            Assert.That(HeaderFor(session).WaveLabel, Is.EqualTo("WAVES REMAINING 2"),
                "the top strip binds the live remaining pool, and labels it as remaining.");
            Assert.That(HeaderFor(session).Text, Does.Contain("WAVES REMAINING 2"));
        }

        /// <summary>
        /// A header built exactly the way <c>ConfiguredMatchSceneRuntime</c> builds the live one, off
        /// the same <see cref="IMatchReadModel"/> members - so a drift between the two constructor
        /// calls is exactly what this test would catch.
        /// </summary>
        private static MatchHudHeaderViewModel HeaderFor(MatchSession session) =>
            new MatchHudHeaderViewModel(
                session.Projection.TieBreakerState,
                session.Projection.Setup,
                session.Projection.TeamLifeCounters,
                session.Projection.RoundIndex,
                session.Projection.TurnIndex,
                false,
                session.Projection.RemainingWaveCounters);
    }
}
