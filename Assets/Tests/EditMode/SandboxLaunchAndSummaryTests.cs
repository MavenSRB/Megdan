using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z18, the TWELFTH amendment's second half: the two engine-free halves of the
    /// sandbox HUD affordances - the launch flag that reaches a staged board at all, and the summary
    /// the Director-only banner reads off the staging it composed.
    ///
    /// <para>The banner's own drawing is Unity-facing and is proved by the Play Mode live-render
    /// check this milestone owes, not here. What is here is everything that decides WHETHER a
    /// staged board can be reached and WHAT the banner says about it - both of which are pure, and
    /// both of which the Unity-free harness therefore covers.</para>
    ///
    /// <para>"Sandbox" throughout is the TWELFTH amendment's staged-board test environment. It is
    /// unrelated to the agent process-isolation sandbox <c>Docs/AI/Testing/POLICY.md</c> names.</para>
    /// </summary>
    [TestFixture]
    public sealed class SandboxLaunchAndSummaryTests
    {
        private const string StagingPath = "C:/tmp/a-staged-board.txt";

        [Test]
        public void TheSandboxFlag_NamesAStagingFileAndMarksTheProcessASandbox()
        {
            var options = PrivateSessionLaunchOptions.Parse(
                new[] { PrivateSessionLaunchOptions.SandboxFlag, StagingPath });

            Assert.That(options.IsValid, Is.True, options.Error);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.None));
            Assert.That(options.SandboxStagingPath, Is.EqualTo(StagingPath));
            Assert.That(options.IsSandbox, Is.True);
        }

        [Test]
        public void NoSandboxFlag_ReadsAsTheOrdinaryDealtMatch()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[] { "-guardsSmokeTest" });

            Assert.That(options.IsValid, Is.True, options.Error);
            Assert.That(options.IsSandbox, Is.False);
            Assert.That(
                options.SandboxStagingPath, Is.Empty,
                "Never null, so no caller has to decide what nothing means.");
        }

        [Test]
        public void TheSandboxFlag_RequiresAPath()
        {
            var options = PrivateSessionLaunchOptions.Parse(
                new[] { PrivateSessionLaunchOptions.SandboxFlag });

            Assert.That(options.IsValid, Is.False);
            Assert.That(options.Error, Does.Contain("staging file"));
        }

        /// <summary>
        /// THE ENFORCEMENT that makes the sandbox Director-only. DEV-004 requires the affordance
        /// never be exposed in a real online match, and a staged board reaches one by exactly one
        /// route - a host composing it before its seats join. Closing that route in the parser is
        /// what makes "Director-only" a property of what will parse rather than of what somebody
        /// remembered to check.
        /// </summary>
        [TestCase(PrivateSessionLaunchOptions.HostFlag)]
        [TestCase(PrivateSessionLaunchOptions.LobbyFlag)]
        public void TheSandboxFlag_IsRefusedBesideEveryNetworkedRole(string roleFlag)
        {
            var options = PrivateSessionLaunchOptions.Parse(
                new[] { roleFlag, PrivateSessionLaunchOptions.SandboxFlag, StagingPath });

            Assert.That(options.IsValid, Is.False, "A seat must never be dealt a staged board.");
            Assert.That(options.Error, Does.Contain("Director-only"));
        }

        [Test]
        public void TheSandboxFlag_IsRefusedBesideAClientSeat()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.ClientFlag,
                PrivateSessionLaunchOptions.AddressFlag, "127.0.0.1:45677",
                PrivateSessionLaunchOptions.SeatFlag, "red-player-01",
                PrivateSessionLaunchOptions.SandboxFlag, StagingPath
            });

            Assert.That(options.IsValid, Is.False);
            Assert.That(options.Error, Does.Contain("Director-only"));
        }

        /// <summary>
        /// The sandbox flag and the format flag are deliberate opposites - one is refused beside
        /// every networked role and the other accepted beside all of them - so this pins that they
        /// still compose on the one line where both are legal.
        /// </summary>
        [Test]
        public void TheSandboxAndFormatFlags_ComposeOnADirectorLine()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.SandboxFlag, StagingPath,
                PrivateSessionLaunchOptions.MatchFormatFlag, "2v2-quick"
            });

            Assert.That(options.IsValid, Is.True, options.Error);
            Assert.That(options.IsSandbox, Is.True);
            Assert.That(options.MatchFormat, Is.SameAs(MatchFormat.TwoVersusTwoQuick));
        }

        [Test]
        public void AnEmptyStaging_IsNamedAsTheDealtBoardRatherThanDrawnBlank()
        {
            var lines = SandboxStagingSummary.Describe(SandboxStagingDescription.Empty);

            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(
                lines[0], Does.Contain("dealt board"),
                "A tester who loaded a staging that described nothing must be told so, rather " +
                "than shown a blank panel they will read as a bug.");
        }

        [Test]
        public void AStagedBoard_NamesEveryKindOfThingItChanged()
        {
            var description = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(
                        new HeroId(OfficialSixPlayerParticipants.WaspHeroIdValue),
                        coordinate: new HexCoordinate(-2, 1),
                        level: 4,
                        coins: 7,
                        hand: null)
                },
                spawnedMinions: null,
                removedMinions: null,
                movedMinions: null,
                waveCount: 5,
                teamLifeCounts: new[]
                {
                    new System.Collections.Generic.KeyValuePair<TeamSide, int>(TeamSide.Red, 3)
                });

            var lines = SandboxStagingSummary.Describe(description);

            Assert.That(lines.Any(line => line.Contains("1 hero") && line.Contains("placed")), Is.True);
            Assert.That(lines.Any(line => line.Contains("Level or coins")), Is.True);
            Assert.That(lines.Any(line => line.Contains("5 Wave counters")), Is.True);
            Assert.That(lines.Any(line => line.Contains("Red") && line.Contains("3 Life counters")), Is.True);
        }

        /// <summary>
        /// The banner is read at a glance, so "1 heroes placed" is a real defect in it. One case
        /// rather than a spelling opinion: the count and the noun have to agree.
        /// </summary>
        [Test]
        public void OneOfAThing_ReadsSingularAndTwoReadPlural()
        {
            var one = SandboxStagingSummary.Describe(
                new SandboxStagingDescription(
                    heroes: new[]
                    {
                        new SandboxHeroStaging(
                            new HeroId(OfficialSixPlayerParticipants.WaspHeroIdValue),
                            coordinate: new HexCoordinate(-2, 1))
                    }));
            var two = SandboxStagingSummary.Describe(
                new SandboxStagingDescription(
                    heroes: new[]
                    {
                        new SandboxHeroStaging(
                            new HeroId(OfficialSixPlayerParticipants.WaspHeroIdValue),
                            coordinate: new HexCoordinate(-2, 1)),
                        new SandboxHeroStaging(
                            new HeroId(OfficialSixPlayerParticipants.ArienHeroIdValue),
                            coordinate: new HexCoordinate(2, -1))
                    }));

            Assert.That(one.Any(line => line.Contains("1 hero placed")), Is.True);
            Assert.That(two.Any(line => line.Contains("2 heroes placed")), Is.True);
        }

        /// <summary>
        /// The banner is fed by a file, so what the file round-trips is what the banner will say.
        /// This is the join between the launch flag above and the summary beside it: the two halves
        /// of this milestone meet at a serialized staging, and a staging that changed meaning across
        /// the round trip would show a tester a board other than the one composed.
        /// </summary>
        [Test]
        public void AStagingThatRoundTripsThroughItsFileFormat_SummarisesIdentically()
        {
            var description = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(
                        new HeroId(OfficialSixPlayerParticipants.WaspHeroIdValue),
                        coordinate: new HexCoordinate(-2, 1),
                        level: 3,
                        coins: 2)
                },
                waveCount: 5);

            Assert.That(
                SandboxStagingDescription.TryParse(
                    description.Serialize(), out var parsed, out var error),
                Is.True, error);
            Assert.That(
                SandboxStagingSummary.Describe(parsed),
                Is.EqualTo(SandboxStagingSummary.Describe(description)));
        }
    }
}
