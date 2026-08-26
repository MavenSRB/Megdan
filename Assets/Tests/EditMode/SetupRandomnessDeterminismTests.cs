using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.28b's closing fixture for `F-ER2-04`, and acceptance checks 1 through 3.
    ///
    /// The finding was not that replay produced the wrong side - it produced the right one, because the
    /// side was carried everywhere it was needed. The finding was that normal setup could only be
    /// *remembered*, never *performed again*: the draw happened inside
    /// `TieBreakerSetupAuthority.CreateNormalSetupState`, was consumed on the spot, and was written down
    /// nowhere. These cases hold the corrected property - that the recorded state contains enough to
    /// carry the flip out again and land on the same side.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class SetupRandomnessDeterminismTests
    {
        /// <summary>
        /// The derivation is arithmetic, so this value is fixed for all time by the algorithm. It is
        /// pinned for the same reason the snapshot corpus pins digests: the number itself carries no
        /// meaning, and its *stability* is the entire contract. A recorded seed that derived one side
        /// on the host and the other on a client would be worse than no seed at all.
        /// </summary>
        private const ulong DerivationOfSeedOneForTheCoinFlip = 0x910A2DEC89025CC1UL;

        [Test]
        public void NormalSetup_ReproducesItsTieBreakerSideFromTheRecordedSeedAlone()
        {
            var configuration = MatchConfiguration.Create();

            Assert.That(configuration.SetupSeed.IsRecorded, Is.True,
                "Normal setup must record the draw it made. Without it the side can only be copied " +
                "forward, which is what F-ER2-04 recorded: replay reproduced a remembered answer " +
                "rather than reaching the same one again.");

            var reproduced = TieBreakerSetupAuthority.CreateNormalSetupState(configuration.SetupSeed);

            Assert.That(reproduced, Is.EqualTo(configuration.TieBreakerState),
                "Performing normal setup again from the recorded seed must land on the side the match " +
                "actually started with.");
        }

        [Test]
        public void ReplayingANormalSetupScenario_ReachesTheSameSideFromRecordedStateAlone()
        {
            var configuration = MatchConfiguration.Create();
            var scenario = CreateScenario(configuration);

            Assert.That(scenario.SetupSeed, Is.EqualTo(configuration.SetupSeed),
                "The recorded scenario is what a replay rebuilds from, so it carries the draw.");

            var replayed = scenario.CreateConfiguration();

            Assert.That(replayed.SetupSeed, Is.EqualTo(configuration.SetupSeed),
                "A replayed configuration keeps the recorded draw; otherwise the next replay of the " +
                "replay has nothing to derive from.");
            Assert.That(
                TieBreakerSetupAuthority.CreateNormalSetupState(replayed.SetupSeed),
                Is.EqualTo(configuration.TieBreakerState),
                "Replay reproduces the identical Tie Breaker side by deriving it from recorded state, " +
                "not by carrying the answer.");
        }

        [Test]
        public void CapturingAndRestoringAConfiguration_KeepsTheRecordedDraw()
        {
            var configuration = MatchConfiguration.Create();

            var restored = MatchConfiguration.Restore(
                configuration.CaptureRestoreState());

            Assert.That(restored.SetupSeed, Is.EqualTo(configuration.SetupSeed),
                "This carrier is what MatchAggregate.RestoreGraphs writes the recorded scenario's seed " +
                "into, so a restored match keeps being able to say how its setup was decided. The side " +
                "comes from the payload because it flips during play; the draw cannot, because it " +
                "happened once and no later state restates it.");
            Assert.That(
                TieBreakerSetupAuthority.CreateNormalSetupState(restored.SetupSeed),
                Is.EqualTo(configuration.TieBreakerState),
                "And the kept draw still derives the side the match started on.");
        }

        [Test]
        public void TheDerivation_IsStableAcrossCallsAndPinnedAgainstSilentChange()
        {
            var seed = new SetupRandomnessSeed(1UL);

            Assert.That(
                SetupRandomnessAuthority.Derive(seed, SetupRandomnessPurpose.TieBreakerCoinFlip),
                Is.EqualTo(SetupRandomnessAuthority.Derive(seed, SetupRandomnessPurpose.TieBreakerCoinFlip)),
                "The same recorded seed and purpose must answer the same way every time it is asked.");

            Assert.That(
                SetupRandomnessAuthority.Derive(seed, SetupRandomnessPurpose.TieBreakerCoinFlip),
                Is.EqualTo(DerivationOfSeedOneForTheCoinFlip),
                "The derivation moved. It is persisted meaning: a recorded seed must mean the same " +
                "thing on every runtime and in every build, so this is a deliberate change requiring " +
                "its own migration decision, not an implementation detail.");
        }

        [Test]
        public void TheDerivation_ReachesBothSidesAndDoesNotCorrelateAcrossPurposes()
        {
            var sides = Enumerable.Range(1, 512)
                .Select(value => TieBreakerSetupAuthority
                    .CreateNormalSetupState(new SetupRandomnessSeed((ulong)value)).RevealedSide)
                .ToArray();

            Assert.That(sides.Any(side => side == TeamSide.Red), Is.True,
                "A coin that never lands on Red is not a coin.");
            Assert.That(sides.Any(side => side == TeamSide.Blue), Is.True,
                "A coin that never lands on Blue is not a coin.");

            var seed = new SetupRandomnessSeed(0x0123456789ABCDEFUL);
            Assert.That(
                SetupRandomnessAuthority.Derive(seed, SetupRandomnessPurpose.TieBreakerCoinFlip),
                Is.Not.EqualTo(seed.Value),
                "A derivation that returns its own seed would correlate every purpose with every " +
                "other, which is the defect the purpose parameter exists to prevent.");
        }

        [Test]
        public void AnExplicitlySelectedSide_RecordsNoDrawAndIsUnchanged()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);

            Assert.That(configuration.TieBreakerState,
                Is.EqualTo(new TieBreakerState(TeamSide.Red)),
                "DEV-005's internal override selects the side directly and is unchanged by this seam.");
            Assert.That(configuration.SetupSeed.IsRecorded, Is.False,
                "An explicitly selected setup made no draw, and saying it made one would be a lie the " +
                "next reader would derive from.");
        }

        [Test]
        public void DerivingWithoutARecordedSeed_IsRefused()
        {
            Assert.That(
                () => SetupRandomnessAuthority.Derive(
                    SetupRandomnessSeed.Unrecorded, SetupRandomnessPurpose.TieBreakerCoinFlip),
                Throws.ArgumentException,
                "Deriving from the named absence would invent a draw that never happened.");

            Assert.That(
                () => SetupRandomnessAuthority.Derive(
                    new SetupRandomnessSeed(1UL), SetupRandomnessPurpose.None),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "A draw states which named purpose it is for, so a second consumer cannot silently " +
                "share the first one's stream.");
        }

        [Test]
        public void DrawnSeeds_AreAlwaysRecordable()
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                Assert.That(SetupRandomnessAuthority.DrawSeed().IsRecorded, Is.True,
                    "Zero is the named absence, so a drawn zero would describe itself as never having " +
                    "happened.");
            }
        }

        private static InitialScenarioDescription CreateScenario(
            MatchConfiguration configuration)
        {
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("setup-randomness-map"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            var blueHero = configuration.BlueHand.Instances[0].OwnerId;

            return new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueHero, new HexCoordinate(2, 0))
                },
                Array.Empty<ObstacleOccupancy>());
        }
    }
}
