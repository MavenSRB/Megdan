using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>Why a staging description was refused, named rather than described.</summary>
    public enum SandboxStagingRejectionReason
    {
        None = 0,
        MalformedDescription = 1,
        UnknownHero = 2,
        UnknownMinion = 3,
        DuplicateMinionId = 4,
        DuplicateHeroStaging = 5,
        UnplayableSpace = 6,
        OccupancyConflict = 7,
        UnknownCard = 8,
        CardNotOwnedByHero = 9,
        IllegalLevel = 10,
        IllegalCoinBalance = 11,
        IllegalWaveCount = 12,
        IllegalLifeCount = 13
    }

    public sealed class SandboxStagingException : Exception
    {
        public SandboxStagingException(SandboxStagingRejectionReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }

        public SandboxStagingRejectionReason Reason { get; }
    }

    /// <summary>
    /// One hero's staged facts. Every member is optional and an absent one means "leave what the
    /// match dealt", so a description that names a hero and nothing else is a no-op rather than an
    /// instruction to blank them.
    /// </summary>
    public sealed class SandboxHeroStaging
    {
        public SandboxHeroStaging(
            HeroId heroId,
            HexCoordinate? coordinate = null,
            int? level = null,
            int? coins = null,
            IEnumerable<CardDefinitionId> hand = null)
        {
            if (!heroId.IsDefined)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.UnknownHero,
                    "A staged hero requires a defined hero id.");
            }

            HeroId = heroId;
            Coordinate = coordinate;
            Level = level;
            Coins = coins;
            Hand = hand == null ? null : Array.AsReadOnly(hand.ToArray());
        }

        public HeroId HeroId { get; }

        /// <summary>Where to stand them, or null to leave them on the space setup placed them on.</summary>
        public HexCoordinate? Coordinate { get; }

        /// <summary>Their Level, or null for p.15's Level 1.</summary>
        public int? Level { get; }

        /// <summary>Their coin balance, or null for the empty purse a match deals.</summary>
        public int? Coins { get; }

        /// <summary>
        /// Exactly the cards to leave in their hand, or null to leave the dealt hand alone.
        ///
        /// <para>An empty list is meaningful and different from null: it stages a hero holding
        /// nothing, which is the position rulebook p.7's commitment and p.14's Defense both have to
        /// be asked about and which a dealt match reaches only several rounds in.</para>
        /// </summary>
        public IReadOnlyList<CardDefinitionId> Hand { get; }
    }

    /// <summary>A minion this description puts on the board that the dealt setup did not.</summary>
    public sealed class SandboxMinionStaging
    {
        public SandboxMinionStaging(
            MinionId minionId, TeamSide teamSide, StandardMinionType minionType, HexCoordinate coordinate)
        {
            if (!minionId.IsDefined)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.UnknownMinion,
                    "A spawned minion requires a defined minion id.");
            }

            if (minionType == StandardMinionType.None)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.MalformedDescription,
                    $"Spawned minion '{minionId.Value}' requires one of p.6's minion types.");
            }

            MinionId = minionId;
            TeamSide = teamSide;
            MinionType = minionType;
            Coordinate = coordinate;
        }

        public MinionId MinionId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }
        public HexCoordinate Coordinate { get; }
    }

    /// <summary>
    /// A staged situation, stated as data. Milestone 8.35z17.
    ///
    /// <para><b>What this is.</b> The user-approved amendment asks for a Director-only test
    /// environment: stage an arbitrary situation by hand, then play it with the real rules. This is
    /// the "serializable staging description" half of that - a value describing where the pieces
    /// stand, what each hero holds and what the counters read, with no behaviour and no opinion
    /// about how it is produced. A surface builds one; <c>SandboxStagingAuthority</c> turns one into
    /// the ordinary composed setup a match begins from.</para>
    ///
    /// <para><b>Why it is a description and not a command stream.</b> The amendment requires the
    /// choice to be made deliberately: staging operations are either recorded commands, giving the
    /// staging itself replay and undo, or a load-a-staged-description entry point. The choice is
    /// forced rather than preferred. A recorded staging operation would have to be a
    /// <c>DirectorCommandKind</c>, and this build's own conformance holds that enum to be the
    /// network command vocabulary: <c>NetworkSubmissionCodecTests</c> asserts every kind has a
    /// round-trip specimen ("or a kind could ship with no round-trip evidence at all") and
    /// <c>SeatAuthorizedSubmissionTests</c> classifies every kind for seat authorization. A staging
    /// kind would therefore either break those ratchets or be carried on the wire and offered to
    /// seats - which is exactly what the amendment forbids, staging being a debug/Director-only
    /// surface that is never in the networked protocol. So the description is the honest option, and
    /// it is also the better one: because a staged description becomes the session's own
    /// <c>MatchSessionInitialState</c>, Undo, restart and the event log keep working unmodified and
    /// restart returns to the staged board. The snapshot-restore analogue would have lost them -
    /// <c>MatchSession.RestoreSnapshot</c> clears accepted history and stops recording it.</para>
    ///
    /// <para><b>What it deliberately cannot say, and why.</b> Two items of the amendment's own
    /// capability menu - which it states is amendable at design time - are refused here rather than
    /// worked around, each for a measured reason:</para>
    /// <list type="bullet">
    /// <item><description><b>Placed tokens and markers.</b> A token reaches the board only when an
    /// effect places it - <c>AdjacentMoveAuthority</c>'s token-carrying constructor arm is private
    /// and says so - and the marker register is composed empty. <c>InitialScenarioDescription</c>
    /// carries heroes and obstacles and no components at all. Staging one would be a second origin
    /// for a board object no rule authored, and this build has a governed conformance rule,
    /// <c>ComponentLifetimeOwnershipConformanceTests</c>, pinning component lifetime to the
    /// capability handlers. A staged token would be a rules path, which the amendment forbids and
    /// the durable no-invented-rules boundary forbids again.</description></item>
    /// <item><description><b>Mid-round state.</b> A composed match begins at round 1, turn 1, and
    /// the round cursor is not a setup decision - it is where the rules have got to. Staging a
    /// round index would be asserting a history that did not happen. The Wave counters and Life
    /// counters, which ARE setup decisions the composed setup already records, are staged here; the
    /// cursor is not.</description></item>
    /// </list>
    /// </summary>
    public sealed class SandboxStagingDescription
    {
        /// <summary>
        /// The format tag every serialized description opens with. It is versioned for the ordinary
        /// reason: a description saved by one build and loaded by another must be refused by name
        /// rather than half-read. This is a local development file format and touches no wire.
        /// </summary>
        public const string FormatTag = "guards-sandbox-staging/1";

        public SandboxStagingDescription(
            IEnumerable<SandboxHeroStaging> heroes = null,
            IEnumerable<SandboxMinionStaging> spawnedMinions = null,
            IEnumerable<MinionId> removedMinions = null,
            IEnumerable<KeyValuePair<MinionId, HexCoordinate>> movedMinions = null,
            int? waveCount = null,
            IEnumerable<KeyValuePair<TeamSide, int>> teamLifeCounts = null)
        {
            Heroes = Array.AsReadOnly((heroes ?? Enumerable.Empty<SandboxHeroStaging>()).ToArray());
            SpawnedMinions = Array.AsReadOnly(
                (spawnedMinions ?? Enumerable.Empty<SandboxMinionStaging>()).ToArray());
            RemovedMinions = Array.AsReadOnly(
                (removedMinions ?? Enumerable.Empty<MinionId>()).ToArray());
            MovedMinions = Array.AsReadOnly(
                (movedMinions ?? Enumerable.Empty<KeyValuePair<MinionId, HexCoordinate>>()).ToArray());
            WaveCount = waveCount;
            TeamLifeCounts = Array.AsReadOnly(
                (teamLifeCounts ?? Enumerable.Empty<KeyValuePair<TeamSide, int>>()).ToArray());

            var duplicateHero = Heroes.GroupBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateHero != null)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.DuplicateHeroStaging,
                    $"Hero '{duplicateHero.Key}' is staged twice, so the description states two " +
                    "positions for one hero and neither is the answer.");
            }

            var duplicateSpawn = SpawnedMinions.GroupBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateSpawn != null)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.DuplicateMinionId,
                    $"Minion '{duplicateSpawn.Key}' is spawned twice.");
            }

            if (WaveCount.HasValue && WaveCount.Value < 0)
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.IllegalWaveCount,
                    "A staged Wave count cannot be negative.");
            }

            foreach (var life in TeamLifeCounts.Where(entry => entry.Value < 0))
            {
                throw new SandboxStagingException(
                    SandboxStagingRejectionReason.IllegalLifeCount,
                    $"A staged Life count for {life.Key} cannot be negative.");
            }
        }

        public IReadOnlyList<SandboxHeroStaging> Heroes { get; }
        public IReadOnlyList<SandboxMinionStaging> SpawnedMinions { get; }
        public IReadOnlyList<MinionId> RemovedMinions { get; }
        public IReadOnlyList<KeyValuePair<MinionId, HexCoordinate>> MovedMinions { get; }

        /// <summary>rulebook p.8's shared Wave counters, or null for the setup's own count.</summary>
        public int? WaveCount { get; }

        /// <summary>rulebook p.13's Life counters per side, or absent for the setup's own.</summary>
        public IReadOnlyList<KeyValuePair<TeamSide, int>> TeamLifeCounts { get; }

        /// <summary>Nothing staged: composing this is composing the ordinary dealt match.</summary>
        public static SandboxStagingDescription Empty { get; } = new SandboxStagingDescription();

        public bool IsEmpty =>
            Heroes.Count == 0 && SpawnedMinions.Count == 0 && RemovedMinions.Count == 0 &&
            MovedMinions.Count == 0 && !WaveCount.HasValue && TeamLifeCounts.Count == 0;

        /// <summary>
        /// The description as text, in a fixed order so the same staging always writes the same
        /// bytes. That determinism is the point: a staged situation is meant to be saved, shared and
        /// re-loaded, and a file that reorders itself between saves cannot be diffed.
        /// </summary>
        public string Serialize()
        {
            var builder = new StringBuilder();
            builder.Append(FormatTag).Append('\n');
            if (WaveCount.HasValue)
            {
                builder.Append("wave ").Append(Number(WaveCount.Value)).Append('\n');
            }

            foreach (var life in TeamLifeCounts.OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal))
            {
                builder.Append("lives ").Append(life.Key).Append(' ')
                    .Append(Number(life.Value)).Append('\n');
            }

            foreach (var hero in Heroes.OrderBy(value => value.HeroId.Value, StringComparer.Ordinal))
            {
                builder.Append("hero ").Append(hero.HeroId.Value);
                if (hero.Coordinate.HasValue) builder.Append(" at ").Append(Coordinate(hero.Coordinate.Value));
                if (hero.Level.HasValue) builder.Append(" level ").Append(Number(hero.Level.Value));
                if (hero.Coins.HasValue) builder.Append(" coins ").Append(Number(hero.Coins.Value));
                builder.Append('\n');
                if (hero.Hand == null) continue;
                builder.Append("hand ").Append(hero.HeroId.Value);
                foreach (var card in hero.Hand) builder.Append(' ').Append(card.Value);
                builder.Append('\n');
            }

            foreach (var minion in SpawnedMinions.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal))
            {
                builder.Append("spawn ").Append(minion.MinionId.Value).Append(' ')
                    .Append(minion.TeamSide).Append(' ').Append(minion.MinionType).Append(' ')
                    .Append(Coordinate(minion.Coordinate)).Append('\n');
            }

            foreach (var minion in RemovedMinions.OrderBy(value => value.Value, StringComparer.Ordinal))
            {
                builder.Append("remove ").Append(minion.Value).Append('\n');
            }

            foreach (var move in MovedMinions.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal))
            {
                builder.Append("move ").Append(move.Key.Value).Append(' ')
                    .Append(Coordinate(move.Value)).Append('\n');
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads what <see cref="Serialize"/> wrote, reporting the first line it could not read
        /// rather than throwing - a hand-edited staging file is the expected input here, so a typo
        /// has to be answerable rather than fatal.
        /// </summary>
        public static bool TryParse(string text, out SandboxStagingDescription description, out string error)
        {
            description = null;
            error = null;
            if (text == null)
            {
                error = "A staging description requires text.";
                return false;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || !string.Equals(lines[0].Trim(), FormatTag, StringComparison.Ordinal))
            {
                error = $"A staging description opens with '{FormatTag}'.";
                return false;
            }

            var heroes = new List<SandboxHeroStaging>();
            var hands = new Dictionary<string, List<CardDefinitionId>>(StringComparer.Ordinal);
            var spawned = new List<SandboxMinionStaging>();
            var removed = new List<MinionId>();
            var moved = new List<KeyValuePair<MinionId, HexCoordinate>>();
            var lives = new List<KeyValuePair<TeamSide, int>>();
            int? waves = null;

            for (var index = 1; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var lineNumber = index + 1;
                switch (parts[0])
                {
                    case "wave" when parts.Length == 2 && TryNumber(parts[1], out var wave):
                        waves = wave;
                        break;
                    case "lives" when parts.Length == 3 &&
                                      Enum.TryParse<TeamSide>(parts[1], out var side) &&
                                      TryNumber(parts[2], out var life):
                        lives.Add(new KeyValuePair<TeamSide, int>(side, life));
                        break;
                    case "hero" when parts.Length >= 2:
                        if (!TryHero(parts, lineNumber, out var hero, out error)) return false;
                        heroes.Add(hero);
                        break;
                    case "hand" when parts.Length >= 2:
                        hands[parts[1]] = parts.Skip(2).Select(value => new CardDefinitionId(value)).ToList();
                        break;
                    case "spawn" when parts.Length == 5 &&
                                      Enum.TryParse<TeamSide>(parts[2], out var spawnSide) &&
                                      Enum.TryParse<StandardMinionType>(parts[3], out var type) &&
                                      TryCoordinate(parts[4], out var spawnAt):
                        spawned.Add(new SandboxMinionStaging(
                            new MinionId(parts[1]), spawnSide, type, spawnAt));
                        break;
                    case "remove" when parts.Length == 2:
                        removed.Add(new MinionId(parts[1]));
                        break;
                    case "move" when parts.Length == 3 && TryCoordinate(parts[2], out var moveTo):
                        moved.Add(new KeyValuePair<MinionId, HexCoordinate>(
                            new MinionId(parts[1]), moveTo));
                        break;
                    default:
                        error = $"Line {lineNumber} is not a staging instruction: '{line}'.";
                        return false;
                }
            }

            // The hand lines are folded in after the loop so that a `hand` may precede its `hero`,
            // which a hand-written file will do sooner or later.
            var composed = heroes
                .Select(value => hands.TryGetValue(value.HeroId.Value, out var hand)
                    ? new SandboxHeroStaging(
                        value.HeroId, value.Coordinate, value.Level, value.Coins, hand)
                    : value)
                .ToList();
            foreach (var orphan in hands.Keys.Where(key =>
                composed.All(value => !string.Equals(value.HeroId.Value, key, StringComparison.Ordinal))))
            {
                composed.Add(new SandboxHeroStaging(new HeroId(orphan), hand: hands[orphan]));
            }

            try
            {
                description = new SandboxStagingDescription(
                    composed, spawned, removed, moved, waves, lives);
            }
            catch (SandboxStagingException exception)
            {
                error = exception.Message;
                return false;
            }

            return true;
        }

        private static bool TryHero(
            string[] parts, int lineNumber, out SandboxHeroStaging hero, out string error)
        {
            hero = null;
            error = null;
            HexCoordinate? at = null;
            int? level = null;
            int? coins = null;
            for (var index = 2; index < parts.Length; index += 2)
            {
                if (index + 1 >= parts.Length)
                {
                    error = $"Line {lineNumber}: '{parts[index]}' has no value.";
                    return false;
                }

                var value = parts[index + 1];
                switch (parts[index])
                {
                    case "at" when TryCoordinate(value, out var coordinate):
                        at = coordinate;
                        break;
                    case "level" when TryNumber(value, out var levelValue):
                        level = levelValue;
                        break;
                    case "coins" when TryNumber(value, out var coinValue):
                        coins = coinValue;
                        break;
                    default:
                        error = $"Line {lineNumber}: '{parts[index]} {value}' is not a hero fact.";
                        return false;
                }
            }

            hero = new SandboxHeroStaging(new HeroId(parts[1]), at, level, coins);
            return true;
        }

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static string Coordinate(HexCoordinate value) =>
            Number(value.Q) + "," + Number(value.R);

        private static bool TryNumber(string text, out int value) =>
            int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        private static bool TryCoordinate(string text, out HexCoordinate coordinate)
        {
            coordinate = default;
            var parts = text.Split(',');
            if (parts.Length != 2 || !TryNumber(parts[0], out var q) || !TryNumber(parts[1], out var r))
            {
                return false;
            }

            coordinate = new HexCoordinate(q, r);
            return true;
        }
    }
}
