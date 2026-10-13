using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Rulebook p.6 setup step 4's game length: how many Wave counters the table starts with.
    /// </summary>
    /// <remarks>
    /// The printed words are "Quick game" and "Long game", and they are kept here rather than
    /// renamed to the "short/long" a player says out loud, because the number each one carries is
    /// read off the printed page and a reader checking this against the book should meet the book's
    /// own noun.
    /// </remarks>
    public enum MatchGameLength
    {
        Quick = 0,
        Long = 1
    }

    /// <summary>
    /// Rulebook pp.5-6's setup menu: the player counts the printed setup covers and the game
    /// length chosen beside them, with the Wave and Life counts each combination prints.
    ///
    /// <para><b>Every number here is transcribed, not chosen.</b> The chapter is titled "GAME SETUP
    /// (4 or 6 players)" and p.4 says "The recommended way to play is with either 4 or 6 players";
    /// p.20 confirms it from the other side, describing itself as the ways to play "outside of the
    /// recommended 4 or 6 player count described on pages 5-6". So the four rows below are the whole
    /// printed menu, and nothing in this type is an interpretation:</para>
    /// <list type="bullet">
    /// <item>Setup step 4: "Quick game: 3 Wave counters. Long game: 5 Wave counters." The Wave count
    /// is a function of the game length alone - the step says nothing about the player count.</item>
    /// <item>Setup step 5's table, whose axes are exactly these two: 4 players take 4 Life counters
    /// in a Quick game and 6 in a Long one; 6 players take 5 and 8.</item>
    /// <item>Setup step 2: "Players must split into two equal teams", which is what makes a 4-player
    /// game two a side and a 6-player game three - the "2v2" and "3v3" a player says.</item>
    /// <item>Setup step 10 places one Ranged, four Melee and one Heavy minion per team and says
    /// nothing about the player count, so the minions do not appear here at all. Milestone 8.25
    /// already read it that way and shared one placement list across every configuration.</item>
    /// </list>
    ///
    /// <para><b>What is deliberately absent.</b> The Handicap card is not a format element: setup
    /// step 11 returns it to the box, and p.20 brings it back only for an odd player count. Both
    /// printed counts here are even, so no configuration this type describes ever deals one, and the
    /// two deferred Handicap questions - Wasp's Crackling Dagger and Sabina's `S-02` - stay deferred
    /// on their own named trigger rather than being answered by this milestone.</para>
    ///
    /// <para>p.20's other counts are not here either. Two players controlling two heroes each, three
    /// or five players with Handicap cards, and eight or ten on the double-lane map are all real
    /// printed variants, and every one of them needs something this build does not have - a second
    /// lane, a Handicap card, or one seat driving two heroes. They are named in the rules ledger as
    /// reference only, and that row does not move.</para>
    /// </summary>
    public sealed class MatchFormat : IEquatable<MatchFormat>
    {
        public const int MaximumSetupCounterCount = 99;

        private MatchFormat(
            string id,
            int playerCount,
            MatchGameLength gameLength,
            int waveCount,
            int lifeCountPerTeam)
        {
            Id = id;
            PlayerCount = playerCount;
            GameLength = gameLength;
            WaveCount = waveCount;
            LifeCountPerTeam = lifeCountPerTeam;
        }

        /// <summary>
        /// The stable token this format is named by on a command line, in a log line and in the
        /// admission tuple. Ordinal-compared everywhere, never localized, and never derived from
        /// the enum member names - a rename of a C# member must not be able to move a wire value.
        /// </summary>
        public string Id { get; }

        /// <summary>Setup step 2's total, split into two equal teams.</summary>
        public int PlayerCount { get; }

        public MatchGameLength GameLength { get; }

        /// <summary>The immutable starting Life count per team selected for this match.</summary>
        public int LifeCountPerTeam { get; }

        /// <summary>
        /// The immutable starting Wave count selected for this match. Official reference rows keep
        /// the printed Quick/Long values; configured matches may carry another positive value.
        /// </summary>
        public int WaveCount { get; }

        /// <summary>Setup step 2's "two equal teams" over <see cref="PlayerCount"/>.</summary>
        public int HeroesPerTeam => PlayerCount / 2;

        /// <summary>
        /// The "2v2"/"3v3" a player says, derived from the printed count rather than stored, so the
        /// two can never disagree either.
        /// </summary>
        public string TeamShape =>
            HeroesPerTeam.ToString(CultureInfo.InvariantCulture) + "v" +
            HeroesPerTeam.ToString(CultureInfo.InvariantCulture);

        /// <summary>p.6 setup step 7's four dashboards, Quick game: 3 Waves, 4 Lives a side.</summary>
        public static MatchFormat TwoVersusTwoQuick { get; } =
            new MatchFormat("2v2-quick", 4, MatchGameLength.Quick, 3, 4);

        /// <summary>The same four dashboards over p.6's Long game: 5 Waves, 6 Lives a side.</summary>
        public static MatchFormat TwoVersusTwoLong { get; } =
            new MatchFormat("2v2-long", 4, MatchGameLength.Long, 5, 6);

        /// <summary>
        /// p.6 setup step 7's six dashboards, Quick game: 3 Waves, 5 Lives a side. This is the
        /// configuration milestone 8.25 made the one a player loads, and it stays the default.
        /// </summary>
        public static MatchFormat ThreeVersusThreeQuick { get; } =
            new MatchFormat("3v3-quick", 6, MatchGameLength.Quick, 3, 5);

        /// <summary>The same six dashboards over p.6's Long game: 5 Waves, 8 Lives a side.</summary>
        public static MatchFormat ThreeVersusThreeLong { get; } =
            new MatchFormat("3v3-long", 6, MatchGameLength.Long, 5, 8);

        /// <summary>
        /// The whole printed menu, in the order p.6's own table prints it - the four-player row
        /// first, Quick before Long - so a menu built from this list reads the way the book does.
        /// </summary>
        public static IReadOnlyList<MatchFormat> All { get; } = Array.AsReadOnly(new[]
        {
            TwoVersusTwoQuick,
            TwoVersusTwoLong,
            ThreeVersusThreeQuick,
            ThreeVersusThreeLong
        });

        /// <summary>
        /// The format a build composes when nothing selects one: milestone 8.25's official
        /// six-player Quick Game, unchanged. Every existing caller, fixture and journey that never
        /// mentions a format therefore keeps composing exactly the match it composed before.
        /// </summary>
        public static MatchFormat Default => ThreeVersusThreeQuick;

        /// <summary>
        /// Milestone 9.70/W-072. A host-selected 2v2, 3v3, 4v4 or 5v5 setup. Printed Quick/Long rows retain their
        /// historical ids when the selected counts match them; every other supported pair uses the
        /// admission tuple's existing custom description.
        /// </summary>
        public static MatchFormat CreateConfigured(
            int heroesPerTeam,
            int waveCount,
            int lifeCountPerTeam)
        {
            if (heroesPerTeam < 2 || heroesPerTeam > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heroesPerTeam), heroesPerTeam,
                    "Configured play supports 2v2, 3v3, 4v4 or 5v5.");
            }

            ValidateSetupCount(waveCount, nameof(waveCount));
            ValidateSetupCount(lifeCountPerTeam, nameof(lifeCountPerTeam));
            var playerCount = heroesPerTeam * 2;
            var printed = Identify(playerCount, waveCount, lifeCountPerTeam);
            if (printed != null)
            {
                return printed;
            }

            return new MatchFormat(
                DescribeComposition(playerCount, waveCount, lifeCountPerTeam),
                playerCount,
                MatchGameLength.Quick,
                waveCount,
                lifeCountPerTeam);
        }

        public static MatchFormat DefaultForTeamSize(int heroesPerTeam) =>
            heroesPerTeam == 2 ? TwoVersusTwoQuick
            : heroesPerTeam == 3 ? ThreeVersusThreeQuick
            : heroesPerTeam == 4 ? CreateConfigured(4, 7, 6)
            : heroesPerTeam == 5 ? CreateConfigured(5, 7, 7)
            : throw new ArgumentOutOfRangeException(nameof(heroesPerTeam));

        /// <summary>
        /// The format that token names, or null when neither an official row nor a supported
        /// configured identity carries it. Ordinal and case-insensitive because a launch flag typed
        /// by a person may carry any case.
        /// </summary>
        public static MatchFormat FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var trimmed = id.Trim();
            var printed = All.FirstOrDefault(format =>
                string.Equals(format.Id, trimmed, StringComparison.OrdinalIgnoreCase));
            if (printed != null)
            {
                return printed;
            }

            // Existing admission identities already describe non-printed compositions as
            // custom-<players>p-w<waves>-l<lives>. Milestone 9.70 makes that vocabulary a host
            // choice, so a joined client must be able to reconstruct it rather than only compare it.
            var parts = trimmed.Split('-');
            if (parts.Length != 4 ||
                !string.Equals(parts[0], "custom", StringComparison.OrdinalIgnoreCase) ||
                !parts[1].EndsWith("p", StringComparison.OrdinalIgnoreCase) ||
                !parts[2].StartsWith("w", StringComparison.OrdinalIgnoreCase) ||
                !parts[3].StartsWith("l", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(parts[1].Substring(0, parts[1].Length - 1),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var playerCount) ||
                !int.TryParse(parts[2].Substring(1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var waves) ||
                !int.TryParse(parts[3].Substring(1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var lives) ||
                (playerCount != 4 && playerCount != 6 && playerCount != 8 && playerCount != 10))
            {
                return null;
            }

            try
            {
                var configured = CreateConfigured(playerCount / 2, waves, lives);
                return string.Equals(configured.Id, trimmed, StringComparison.OrdinalIgnoreCase)
                    ? configured
                    : null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static void ValidateSetupCount(int value, string parameterName)
        {
            if (value < 1 || value > MaximumSetupCounterCount)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Starting Wave and Life counters must be whole numbers from 1 to " +
                    MaximumSetupCounterCount.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        /// <summary>
        /// Every id, comma-separated, for a refusal that has to say what the acceptable answers
        /// were. Built from <see cref="All"/> so a fifth row could never be missing from the
        /// message that lists them.
        /// </summary>
        public static string DescribeEveryId() => string.Join(", ", All.Select(format => format.Id));

        /// <summary>
        /// The printed row those three numbers are, or null when no printed row carries them.
        /// </summary>
        /// <remarks>
        /// All three are compared rather than the two that would suffice, because the point of this
        /// lookup is to answer "is the match that actually got composed one of the printed setups",
        /// and a composition whose Life count disagreed with its own length and count would be
        /// exactly the thing worth not recognising.
        /// </remarks>
        public static MatchFormat Identify(int playerCount, int waveCount, int lifeCountPerTeam) =>
            All.FirstOrDefault(format =>
                format.PlayerCount == playerCount &&
                format.WaveCount == waveCount &&
                format.LifeCountPerTeam == lifeCountPerTeam);

        /// <summary>
        /// What a composed match's setup numbers are called: the printed row's id when they are one
        /// of pp.5-6's four setups, and a stable descriptive token when they are not.
        ///
        /// <para>The second case is not a defect and is not rare. Every staged development fixture
        /// in this build composes a board no printed setup describes - one hero a side, a mirrored
        /// roster, an ally seat - and each is a legitimate composition that a peer must still be
        /// refused admission to unless it composed the same one. Naming those <c>custom-…</c> rather
        /// than failing keeps the admission tuple total: there is no composition this build can make
        /// whose format element is undefined, so no path exists on which the check silently does not
        /// happen.</para>
        /// </summary>
        public static string DescribeComposition(int playerCount, int waveCount, int lifeCountPerTeam)
        {
            var printed = Identify(playerCount, waveCount, lifeCountPerTeam);
            if (printed != null)
            {
                return printed.Id;
            }

            return "custom-" +
                playerCount.ToString(CultureInfo.InvariantCulture) + "p-w" +
                waveCount.ToString(CultureInfo.InvariantCulture) + "-l" +
                lifeCountPerTeam.ToString(CultureInfo.InvariantCulture);
        }

        public bool Equals(MatchFormat other) =>
            other != null && string.Equals(Id, other.Id, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as MatchFormat);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);

        public override string ToString() => Id;
    }
}
