using System.Linq;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The ELEVENTH amendment's format menu against the printed page.
    ///
    /// <para>Every expectation below is written out as a literal transcribed from rulebook pp.5-6
    /// rather than read back off the type under test, because the whole claim this type makes is
    /// that its numbers are the book's. A case that computed them the way the type does could agree
    /// with a wrong table.</para>
    /// </summary>
    public sealed class MatchFormatTests
    {
        /// <summary>
        /// p.6 setup step 4 ("Quick game: 3 Wave counters. Long game: 5 Wave counters.") and p.6
        /// setup step 5's Life-counter table, whose two rows are 4 Players / 4 / 6 and 6 Players /
        /// 5 / 8. Nothing else on those pages varies by format.
        /// </summary>
        private static readonly (string Id, int Players, MatchGameLength Length, int Waves, int Lives)[]
            PrintedMenu =
            {
                ("2v2-quick", 4, MatchGameLength.Quick, 3, 4),
                ("2v2-long", 4, MatchGameLength.Long, 5, 6),
                ("3v3-quick", 6, MatchGameLength.Quick, 3, 5),
                ("3v3-long", 6, MatchGameLength.Long, 5, 8)
            };

        [Test]
        public void ThePrintedMenu_IsExactlyThePlayerCountsAndLengthsPagesFiveAndSixCover()
        {
            Assert.That(MatchFormat.All.Select(format => format.Id).ToArray(),
                Is.EqualTo(PrintedMenu.Select(row => row.Id).ToArray()),
                "The setup chapter is titled 'GAME SETUP (4 or 6 players)' and offers one choice of " +
                "game length, so the whole printed menu is four rows in the page's own order.");
        }

        [Test]
        public void EveryPrintedFormat_CarriesThatRowsWaveAndLifeCounts()
        {
            foreach (var row in PrintedMenu)
            {
                var format = MatchFormat.FindById(row.Id);
                Assert.That(format, Is.Not.Null, "Format '" + row.Id + "' is printed on pp.5-6.");
                Assert.That(format.PlayerCount, Is.EqualTo(row.Players));
                Assert.That(format.GameLength, Is.EqualTo(row.Length));
                Assert.That(format.WaveCount, Is.EqualTo(row.Waves),
                    "p.6 setup step 4 gives '" + row.Id + "' " + row.Waves + " Wave counters.");
                Assert.That(format.LifeCountPerTeam, Is.EqualTo(row.Lives),
                    "p.6 setup step 5's table gives '" + row.Id + "' " + row.Lives +
                    " Life counters per team.");
            }
        }

        [Test]
        public void TheWaveCount_FollowsTheGameLengthAloneAndNotThePlayerCount()
        {
            Assert.That(MatchFormat.TwoVersusTwoQuick.WaveCount,
                Is.EqualTo(MatchFormat.ThreeVersusThreeQuick.WaveCount),
                "Setup step 4 states the Wave counters without mentioning the player count, so the " +
                "two Quick games start the same.");
            Assert.That(MatchFormat.TwoVersusTwoLong.WaveCount,
                Is.EqualTo(MatchFormat.ThreeVersusThreeLong.WaveCount));
            Assert.That(MatchFormat.TwoVersusTwoQuick.WaveCount,
                Is.Not.EqualTo(MatchFormat.TwoVersusTwoLong.WaveCount));
        }

        [Test]
        public void TheLifeCount_MovesWithBothAxesBecauseTheTableHasBoth()
        {
            Assert.That(MatchFormat.TwoVersusTwoQuick.LifeCountPerTeam,
                Is.Not.EqualTo(MatchFormat.ThreeVersusThreeQuick.LifeCountPerTeam),
                "The table's own point is that the Life count differs at four and six players.");
            Assert.That(MatchFormat.ThreeVersusThreeQuick.LifeCountPerTeam,
                Is.Not.EqualTo(MatchFormat.ThreeVersusThreeLong.LifeCountPerTeam),
                "and that it differs between a Quick and a Long game at the same count.");
        }

        [Test]
        public void EveryFormat_SplitsIntoTwoEqualTeamsAndSaysSoTheWayAPlayerDoes()
        {
            foreach (var format in MatchFormat.All)
            {
                Assert.That(format.HeroesPerTeam * 2, Is.EqualTo(format.PlayerCount),
                    "Setup step 2: 'Players must split into two equal teams.'");
            }

            Assert.That(MatchFormat.TwoVersusTwoQuick.TeamShape, Is.EqualTo("2v2"));
            Assert.That(MatchFormat.ThreeVersusThreeLong.TeamShape, Is.EqualTo("3v3"));
        }

        [Test]
        public void TheDefault_IsStillTheSixPlayerQuickGameMilestoneEightTwentyFiveMadeTheOne()
        {
            Assert.That(MatchFormat.Default, Is.SameAs(MatchFormat.ThreeVersusThreeQuick),
                "Nothing about adding a menu changes what a build composes when nobody chooses.");
            Assert.That(MatchFormat.Default.WaveCount, Is.EqualTo(3));
            Assert.That(MatchFormat.Default.LifeCountPerTeam, Is.EqualTo(5));
            Assert.That(MatchFormat.Default.PlayerCount, Is.EqualTo(6));
        }

        [Test]
        public void FindById_TakesAnyCaseAndReconstructsConfiguredHostChoices()
        {
            Assert.That(MatchFormat.FindById("2V2-QUICK"), Is.SameAs(MatchFormat.TwoVersusTwoQuick),
                "A person types a launch flag, so the id is matched without case.");
            Assert.That(MatchFormat.FindById("  3v3-long  "), Is.SameAs(MatchFormat.ThreeVersusThreeLong));
            var configured = MatchFormat.FindById("  CUSTOM-4P-W7-L9  ");
            Assert.That(configured, Is.Not.Null);
            Assert.That(configured.Id, Is.EqualTo("custom-4p-w7-l9"));
            Assert.That(configured.HeroesPerTeam, Is.EqualTo(2));
            Assert.That(configured.WaveCount, Is.EqualTo(7));
            Assert.That(configured.LifeCountPerTeam, Is.EqualTo(9));
            Assert.That(MatchFormat.FindById("1v1-quick"), Is.Null,
                "1v1 was dropped from the amendment and is not printed as a setup at any length.");
            Assert.That(MatchFormat.FindById("4v4-quick"), Is.Null);
            Assert.That(MatchFormat.FindById(""), Is.Null);
            Assert.That(MatchFormat.FindById(null), Is.Null);
        }

        [Test]
        public void ConfiguredHostCounts_ArePositiveBoundedAndReuseOfficialReferenceRows()
        {
            Assert.That(MatchFormat.CreateConfigured(2, 3, 4),
                Is.SameAs(MatchFormat.TwoVersusTwoQuick));
            Assert.That(MatchFormat.CreateConfigured(3, 5, 8),
                Is.SameAs(MatchFormat.ThreeVersusThreeLong));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                MatchFormat.CreateConfigured(1, 3, 4));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                MatchFormat.CreateConfigured(2, 0, 4));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                MatchFormat.CreateConfigured(2, 3, MatchFormat.MaximumSetupCounterCount + 1));
            Assert.That(MatchFormat.FindById("custom-6p-w0-l5"), Is.Null);
            Assert.That(MatchFormat.FindById("custom-4p-w3-l100"), Is.Null);
        }

        [TestCase(2, 3, 4)]
        [TestCase(3, 3, 5)]
        public void TeamSizeDefaults_AreTheFormerQuickValues(
            int heroesPerTeam, int waves, int lives)
        {
            var format = MatchFormat.DefaultForTeamSize(heroesPerTeam);
            Assert.That(format.WaveCount, Is.EqualTo(waves));
            Assert.That(format.LifeCountPerTeam, Is.EqualTo(lives));
        }

        [Test]
        public void Identify_RecognisesAPrintedRowOnlyWhenAllThreeNumbersAreThatRows()
        {
            Assert.That(MatchFormat.Identify(4, 3, 4), Is.SameAs(MatchFormat.TwoVersusTwoQuick));
            Assert.That(MatchFormat.Identify(6, 5, 8), Is.SameAs(MatchFormat.ThreeVersusThreeLong));
            Assert.That(MatchFormat.Identify(4, 3, 5), Is.Null,
                "Four players at three Waves take four Life counters, not the six-player five; a " +
                "composition holding that mixture is not one of the printed setups.");
            Assert.That(MatchFormat.Identify(2, 3, 4), Is.Null,
                "Two heroes a side is p.20's extended-count territory, which this build claims none of.");
        }

        [Test]
        public void DescribeComposition_NamesAPrintedRowAndStillDescribesEverythingElse()
        {
            Assert.That(MatchFormat.DescribeComposition(6, 3, 5), Is.EqualTo("3v3-quick"));
            Assert.That(MatchFormat.DescribeComposition(4, 5, 6), Is.EqualTo("2v2-long"));

            // The staged development boards are the real second case: one hero a side over the
            // six-player numbers is what every mirror and staged-hero fixture composes, and each is
            // a composition a peer must still have to match rather than one with no name.
            Assert.That(MatchFormat.DescribeComposition(2, 3, 5), Is.EqualTo("custom-2p-w3-l5"));
            Assert.That(MatchFormat.DescribeComposition(3, 3, 5), Is.EqualTo("custom-3p-w3-l5"));
            Assert.That(MatchFormat.DescribeComposition(6, 3, -1), Is.EqualTo("custom-6p-w3-l-1"),
                "Teams holding different Life counts is not a format, and is described rather than " +
                "silently reported as one of the printed four.");
        }

        [Test]
        public void EveryIdIsDistinct_AndTheRefusalMessageListsThemAll()
        {
            Assert.That(MatchFormat.All.Select(format => format.Id).Distinct().Count(),
                Is.EqualTo(MatchFormat.All.Count));
            foreach (var format in MatchFormat.All)
            {
                Assert.That(MatchFormat.DescribeEveryId(), Does.Contain(format.Id),
                    "A refusal that lists the acceptable answers must list every one of them.");
            }
        }

        [Test]
        public void AFormat_EqualsItselfByIdAndNothingElse()
        {
            Assert.That(MatchFormat.FindById("2v2-quick"), Is.EqualTo(MatchFormat.TwoVersusTwoQuick));
            Assert.That(MatchFormat.TwoVersusTwoQuick, Is.Not.EqualTo(MatchFormat.TwoVersusTwoLong));
            Assert.That(MatchFormat.TwoVersusTwoQuick.GetHashCode(),
                Is.EqualTo(MatchFormat.FindById("2v2-quick").GetHashCode()));
            Assert.That(MatchFormat.ThreeVersusThreeQuick.ToString(), Is.EqualTo("3v3-quick"));
        }
    }
}
