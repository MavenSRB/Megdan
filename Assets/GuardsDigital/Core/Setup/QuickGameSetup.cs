using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    public readonly struct HeroContentId : IEquatable<HeroContentId>
    {
        public HeroContentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Hero content ID must not be blank.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(HeroContentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is HeroContentId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct MinionId : IEquatable<MinionId>
    {
        public MinionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Minion ID must not be blank.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MinionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MinionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum StandardMinionType
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Heavy = 3
    }

    public readonly struct ConfiguredSpaceReference : IEquatable<ConfiguredSpaceReference>
    {
        public ConfiguredSpaceReference(MapId mapId, SpaceId spaceId)
        {
            if (string.IsNullOrWhiteSpace(mapId.Value)) throw new ArgumentException("Configured space requires a map ID.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(spaceId.Value)) throw new ArgumentException("Configured space requires a space ID.", nameof(spaceId));
            MapId = mapId;
            SpaceId = spaceId;
        }

        public MapId MapId { get; }
        public SpaceId SpaceId { get; }
        public bool Equals(ConfiguredSpaceReference other) => MapId.Equals(other.MapId) && SpaceId.Equals(other.SpaceId);
        public override bool Equals(object obj) => obj is ConfiguredSpaceReference other && Equals(other);
        public override int GetHashCode() => (MapId.GetHashCode() * 397) ^ SpaceId.GetHashCode();
        public override string ToString() => $"{MapId}/{SpaceId}";
    }

    public readonly struct ConfiguredSpawnPointReference : IEquatable<ConfiguredSpawnPointReference>
    {
        public ConfiguredSpawnPointReference(MapId mapId, SpawnPointId spawnPointId)
        {
            if (string.IsNullOrWhiteSpace(mapId.Value)) throw new ArgumentException("Configured spawn point requires a map ID.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(spawnPointId.Value)) throw new ArgumentException("Configured spawn point requires a spawn-point ID.", nameof(spawnPointId));
            MapId = mapId;
            SpawnPointId = spawnPointId;
        }

        public MapId MapId { get; }
        public SpawnPointId SpawnPointId { get; }
        public bool Equals(ConfiguredSpawnPointReference other) =>
            MapId.Equals(other.MapId) && SpawnPointId.Equals(other.SpawnPointId);
        public override bool Equals(object obj) => obj is ConfiguredSpawnPointReference other && Equals(other);
        public override int GetHashCode() => (MapId.GetHashCode() * 397) ^ SpawnPointId.GetHashCode();
        public override string ToString() => $"{MapId}/{SpawnPointId}";
    }

    public sealed class QuickGameHeroPlacementDefinition
    {
        public QuickGameHeroPlacementDefinition(
            HeroId heroId,
            HeroContentId contentId,
            string displayName,
            ConfiguredSpawnPointReference spawnPoint,
            ConfiguredSpaceReference occupiedSpace)
        {
            if (!heroId.IsDefined) throw new ArgumentException("Hero placement requires a hero ID.", nameof(heroId));
            if (!contentId.IsDefined) throw new ArgumentException("Hero placement requires a content ID.", nameof(contentId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Hero display name must not be blank.", nameof(displayName));
            HeroId = heroId;
            ContentId = contentId;
            DisplayName = displayName;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
        }

        public HeroId HeroId { get; }
        public HeroContentId ContentId { get; }
        public string DisplayName { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
    }

    public sealed class QuickGameMinionPlacementDefinition
    {
        public QuickGameMinionPlacementDefinition(
            MinionId minionId,
            TeamSide teamSide,
            StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint,
            ConfiguredSpaceReference occupiedSpace)
        {
            if (!minionId.IsDefined) throw new ArgumentException("Minion placement requires a minion ID.", nameof(minionId));
            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue) throw new ArgumentOutOfRangeException(nameof(teamSide));
            if (minionType == StandardMinionType.None) throw new ArgumentOutOfRangeException(nameof(minionType));
            MinionId = minionId;
            TeamSide = teamSide;
            MinionType = minionType;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
        }

        public MinionId MinionId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
    }

    public sealed class QuickGameSetupConfiguration
    {
        private readonly IReadOnlyList<QuickGameHeroPlacementDefinition> heroes;
        private readonly IReadOnlyList<QuickGameMinionPlacementDefinition> minions;

        public QuickGameSetupConfiguration(
            MapDefinitionReference requiredMap,
            int waveCount,
            int lifeCountPerTeam,
            IEnumerable<QuickGameHeroPlacementDefinition> heroes,
            IEnumerable<QuickGameMinionPlacementDefinition> minions)
        {
            if (string.IsNullOrWhiteSpace(requiredMap.MapId.Value) || requiredMap.ContentRevision <= 0)
                throw new ArgumentException("Quick Game setup requires a map identity and content revision.", nameof(requiredMap));
            if (waveCount <= 0) throw new ArgumentOutOfRangeException(nameof(waveCount));
            if (lifeCountPerTeam <= 0) throw new ArgumentOutOfRangeException(nameof(lifeCountPerTeam));
            var heroArray = heroes?.ToArray() ?? throw new ArgumentNullException(nameof(heroes));
            var minionArray = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            if (heroArray.Length == 0) throw new ArgumentException("Quick Game setup requires heroes.", nameof(heroes));
            if (minionArray.Length == 0) throw new ArgumentException("Quick Game setup requires minions.", nameof(minions));
            if (heroArray.Any(value => value == null) || minionArray.Any(value => value == null))
                throw new ArgumentException("Quick Game setup placements must not contain null entries.");
            if (heroArray.Select(value => value.HeroId).Distinct().Count() != heroArray.Length)
                throw new ArgumentException("Quick Game hero IDs must be unique.", nameof(heroes));
            if (minionArray.Select(value => value.MinionId).Distinct().Count() != minionArray.Length)
                throw new ArgumentException("Quick Game minion IDs must be unique.", nameof(minions));

            RequiredMap = requiredMap;
            WaveCount = waveCount;
            LifeCountPerTeam = lifeCountPerTeam;
            this.heroes = Array.AsReadOnly(heroArray);
            this.minions = Array.AsReadOnly(minionArray);
        }

        public MapDefinitionReference RequiredMap { get; }
        public int WaveCount { get; }
        public int LifeCountPerTeam { get; }
        public IReadOnlyList<QuickGameHeroPlacementDefinition> Heroes => heroes;
        public IReadOnlyList<QuickGameMinionPlacementDefinition> Minions => minions;

        /// <summary>
        /// Milestone 8.25. Rulebook p.5-6's official six-player Quick Game board: three different Red
        /// heroes and three different Blue heroes, each on a hero spawn point in their own Throne zone,
        /// with three Wave counters, five Life counters per team and setup step 10's minions.
        ///
        /// <para><b>This is the configuration a player loads, and it is what every earlier staged arm
        /// said this milestone owned.</b> `DEV-001`'s one-hero-per-team staging and `DEV-002`'s
        /// mirrored Wasp are both retired by it: six seats, six different printed sets, Wasp dealt
        /// exactly once.</para>
        ///
        /// <para><b>Two numbers move and one does not.</b> The Life count moves from four to five,
        /// because p.6's table gives a Quick game four Lives at four players and five at six, and this
        /// is the first configuration here with six. The Wave count stays three, because p.6 makes it a
        /// choice of game length rather than of player count. The minions stay exactly what they were:
        /// setup step 10 places one Ranged, four Melee and one Heavy per team on the starting Battle
        /// Zone's own spawn points, and it says nothing about the player count - so the twelve
        /// placements below are the same twelve the staged board has always used.</para>
        ///
        /// <para><b>The hero-to-spawn assignment is stated rather than drawn</b>, for the reason
        /// <see cref="Participants.OfficialSixPlayerParticipants"/> states the hero-to-seat one: the
        /// three spawn points of a Throne are interchangeable at setup, and a draw between them would
        /// change nothing a rule can ask. Wasp keeps <c>red-hero-01</c> and Arien <c>blue-hero-01</c>,
        /// which are the two the staged board used, so the two heroes a journey already knows stand
        /// where they always stood.</para>
        ///
        /// <para>The display names are p.6 setup step 7's own, all six of them. The staged arms keep
        /// their shorter labels because their subject is one printed set rather than a table, but a
        /// player looking at the official setup is looking at the six dashboards that step names, and
        /// the epithet is how the rulebook names them. It is a label on the seat either way - the
        /// content identity is what a rule, a token and a snapshot read.</para>
        ///
        /// <para>The ELEVENTH amendment's format menu made this one row of four rather than the only
        /// setup, so the board is built by <see cref="CreateOfficial"/> from the same table and the
        /// same numbers. Everything above still describes exactly what this returns; the name is
        /// kept because a great many callers, fixtures and journeys say it.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateOfficialSixPlayer() =>
            CreateOfficial(MatchFormat.ThreeVersusThreeQuick);

        /// <summary>
        /// One row of p.6 setup step 7's line-up: the hero, the printed set it deals, the epithet
        /// step 7 names it by, its profile-card complexity and roles, and the hero spawn point it
        /// stands on.
        /// </summary>
        private readonly struct OfficialHeroRow
        {
            public OfficialHeroRow(
                string heroIdValue, string contentId, string displayName,
                HeroComplexity complexity, HeroRole roles,
                string spawn, string space, TeamSide side)
            {
                HeroIdValue = heroIdValue;
                ContentId = contentId;
                DisplayName = displayName;
                Complexity = complexity;
                Roles = roles;
                Spawn = spawn;
                Space = space;
                Side = side;
            }

            public string HeroIdValue { get; }
            public string ContentId { get; }
            public string DisplayName { get; }
            public HeroComplexity Complexity { get; }
            public HeroRole Roles { get; }
            public string Spawn { get; }
            public string Space { get; }
            public TeamSide Side { get; }
        }

        /// <summary>
        /// p.6 setup step 7's whole line-up in the step's own order - the Red team's, then the
        /// Blue team's - each hero on the spawn point it has stood on since milestone 8.25.
        ///
        /// <para>It is ONE list for both printed player counts because step 7 is written that way:
        /// the four-player game takes Wasp and Xargatha against Arien and Brogan, and the
        /// six-player game is that same line-up plus Tigerclaw and Sabina "for the third players on
        /// the Red and Blue teams". Taking each team's first <c>HeroesPerTeam</c> rows is therefore
        /// the step's own construction, not a truncation invented here - and it keeps one source
        /// for every hero's set, epithet and spawn, so the two formats cannot drift apart.</para>
        /// </summary>
        private static readonly OfficialHeroRow[] OfficialHeroRows =
        {
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.WaspHeroIdValue,
                "wasp", "Wasp the Warmaiden", HeroComplexity.Challenging,
                HeroRole.Disabler | HeroRole.Tactician | HeroRole.Sniper,
                "red-hero-01", "q-2_r-5", TeamSide.Red),
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.XargathaHeroIdValue,
                "xargatha", "Xargatha the Changed", HeroComplexity.Challenging,
                HeroRole.Tactician | HeroRole.Pusher | HeroRole.Disabler |
                HeroRole.Durable | HeroRole.Sniper,
                "red-hero-02", "q-6_r-4", TeamSide.Red),
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.TigerclawHeroIdValue,
                "tigerclaw", "Tigerclaw the Cutpurse", HeroComplexity.Challenging,
                HeroRole.Melee | HeroRole.Disabler | HeroRole.Farming,
                "red-hero-03", "q-4_r-4", TeamSide.Red),
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.ArienHeroIdValue,
                "arien", "Arien the Tidemaster", HeroComplexity.Challenging,
                HeroRole.Tactician | HeroRole.Disabler | HeroRole.Durable | HeroRole.Pusher,
                "blue-hero-01", "q-3_r10", TeamSide.Blue),
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.BroganHeroIdValue,
                "brogan", "Brogan the Destroyer", HeroComplexity.Challenging,
                HeroRole.Durable | HeroRole.Disabler | HeroRole.Pusher | HeroRole.Farming,
                "blue-hero-02", "q-1_r10", TeamSide.Blue),
            new OfficialHeroRow(
                Participants.OfficialSixPlayerParticipants.SabinaHeroIdValue,
                "sabina", "Sabina the Gunslinger", HeroComplexity.Challenging,
                HeroRole.Tactician | HeroRole.Pusher,
                "blue-hero-03", "q-5_r11", TeamSide.Blue)
        };

        /// <summary>
        /// Milestone 8.35z36. Every hero this build can deal, in setup step 7's own order.
        /// </summary>
        /// <remarks>
        /// <para>The printed line-up table is what a lobby may OFFER: a hero appears here exactly when a
        /// dealing milestone has given it a row, and the printed formats are seated from this table.
        /// Publishing it means a hero-selection screen offers what a printed match can deal without
        /// counting or restating anything, and a hero given a row tomorrow appears in every selector the
        /// same commit it appears here. The profile-card complexity and roles travel with that same
        /// registered identity so a selector can filter without restating the roster.</para>
        /// <para>It is NOT the set of heroes the build can put on a board, and the difference arrived
        /// with milestone 8.35z34: Dodger the Warlock is dealt, registered and gated, but through a
        /// STAGED one-versus-one configuration rather than through this table, because that milestone
        /// deliberately changed no official line-up. A hero outside this table must not be offered in a
        /// lobby.</para>
        /// <para>This is a projection of the existing table and adds no row, no value and no
        /// content of its own, so it moves no content digest - the identity oracle walks the card
        /// catalogs, and a setup table has never been in that walk. Measured rather than assumed;
        /// see the milestone's evidence.</para>
        /// </remarks>
        public static IReadOnlyList<RegisteredHero> RegisteredHeroes { get; } = OfficialHeroRows
            .Select(row => new RegisteredHero(
                row.HeroIdValue, new HeroContentId(row.ContentId), row.DisplayName,
                row.Complexity, row.Roles))
            .ToArray();

        /// <summary>
        /// Milestone 8.35z41. The heroes this build DEALS but the printed line-up does not seat -
        /// dealt through a staged configuration by their own dealing milestones.
        /// </summary>
        /// <remarks>
        /// <para>These rows carry a hero id, a printed set and setup step 7's epithet, and nothing
        /// else: they name no side and no marked point, because they have no printed SEAT. A line-up
        /// that selects one of them takes the seat and the marked point of whichever position it was
        /// chosen into, which is what <see cref="Setup.HeroLineupComposition"/> has done since the
        /// selector arrived - the hero column moves and no seat does.</para>
        /// <para><b>The hero ids are minted here and are new.</b> A staged one-versus-one deals these
        /// heroes under the generic staged blue hero id, because on a staged board the hero's identity
        /// is the CONTENT it holds; a selected line-up needs a stable per-hero id instead, because two
        /// of them can sit on one board and the projection has to tell them apart. Minting them moves
        /// nothing: no existing composition names them, so no snapshot, digest or pinned identity can
        /// see this list until a lobby actually picks one.</para>
        /// </remarks>
        /// <summary>Milestone 8.35z41. Dodger's own hero id, minted where she becomes pickable.</summary>
        public const string DodgerHeroIdValue = "hero-dodger";

        /// <summary>Milestone 8.35z41. Misa's own hero id, on the same terms.</summary>
        public const string MisaHeroIdValue = "hero-misa";

        /// <summary>
        /// Milestone 8.35z50. Silverarrow's own hero id, minted late: milestone 8.35z46 dealt her and
        /// did not add her here, so she has been dealt, registered and gated but unpickable ever since.
        /// </summary>
        public const string SilverarrowHeroIdValue = "hero-silverarrow";

        /// <summary>Milestone 8.35z50. Ursafar's own hero id, minted at his own dealing.</summary>
        public const string UrsafarHeroIdValue = "hero-ursafar";

        /// <summary>
        /// Milestone 8.35z53. Whisper's own hero id - and the first that names a
        /// hero from the Devoted Hero pack rather than from the base box.
        /// </summary>
        public const string WhisperHeroIdValue = "hero-whisper";

        /// <summary>
        /// Milestone 8.37. Tali's own hero id - the twelfth and last hero of the package arc, and the
        /// second from the Devoted Hero pack rather than from the base box.
        /// </summary>
        public const string TaliHeroIdValue = "hero-tali";

        /// <summary>
        /// Milestone 8.38e. Swift's own hero id - the thirteenth hero, and the first dealt after the
        /// package arc closed, on the 2026-09-11 user direction that re-opened the roster.
        /// </summary>
        public const string SwiftHeroIdValue = "hero-swift";

        /// <summary>
        /// Milestone 8.39i. Cutter's own hero id - the fourteenth hero, and the second dealt after the
        /// package arc closed, on the same 2026-09-11 user direction that named her by name.
        /// </summary>
        public const string CutterHeroIdValue = "hero-cutter";

        /// <summary>
        /// Milestone 8.40j. Bain's own hero id - the fifteenth hero, and the third dealt after the
        /// package arc closed, on the 2026-09-13 user direction that named him by name.
        /// </summary>
        public const string BainHeroIdValue = "hero-bain";
        public const string GarrusHeroIdValue = "hero-garrus";
        public const string WidgetHeroIdValue = "hero-widget";

        /// <summary>
        /// Milestone 8.57g. Rowenna's own hero id - the EIGHTEENTH hero, on the 2026-09-19 user
        /// direction that named her by name ("After the build start implementing Rowenna").
        /// </summary>
        public const string RowennaHeroIdValue = "hero-rowenna";

        /// <summary>
        /// Milestone 8.61j. Min's own hero id - the NINETEENTH hero, on the 2026-09-19 user direction
        /// that named her by name ("Time for new hero" -> "Min").
        /// </summary>
        public const string MinHeroIdValue = "hero-min";

        /// <summary>
        /// Milestone 8.69g. Brynn's own hero id - the TWENTIETH hero, on the 2026-09-21 user direction
        /// that named him by name ("ok go for Byrnn, keep in mind he needs to be correctly implemented
        /// in new hero pick screen").
        /// </summary>
        public const string BrynnHeroIdValue = "hero-brynn";

        /// <summary>
        /// Milestone 8.71h. Hanu's own hero id - the TWENTY-FIRST hero, on the 2026-09-21 user
        /// direction that opened him ("Lets go for a new hero", then choosing Hanu from the twelve
        /// undealt heroes surveyed by stars and roles, then "Make sure pick screen is correctly
        /// imeplented").
        /// </summary>
        public const string HanuHeroIdValue = "hero-hanu";

        /// <summary>
        /// Milestone 8.75b. Ignatia's own hero id - the TWENTY-SECOND hero, on the 2026-09-22 user
        /// direction that opened her by name ("implement Ignatia") and ordered the finish without
        /// pauses ("keep going and dont stop until you need something from me, or when the hero is
        /// done up to standard").
        /// </summary>
        public const string IgnatiaHeroIdValue = "hero-ignatia";

        public const string TrinketsHeroIdValue = "hero-trinkets";

        public const string SnorriHeroIdValue = "hero-snorri";

        /// <summary>Milestone 8.112g. Mrak the Rockshaper, the TWENTY-FIFTH hero.</summary>
        public const string MrakHeroIdValue = "hero-mrak";

        /// <summary>Milestone 8.114f. Wuk the Grove Keeper, the TWENTY-SIXTH hero.</summary>
        public const string WukHeroIdValue = "hero-wuk";

        /// <summary>Milestone 9.8f. Mortimer the Awakener, the TWENTY-SEVENTH hero.</summary>
        public const string MortimerHeroIdValue = "hero-mortimer";

        /// <summary>Milestone 9.21j. Razzle the Ringmaster, the TWENTY-EIGHTH hero.</summary>
        public const string RazzleHeroIdValue = "hero-razzle";

        /// <summary>Milestone 9.30i. Gydion the Archwizard, the TWENTY-NINTH hero.</summary>
        public const string GydionHeroIdValue = "hero-gydion";

        /// <summary>Milestone 9.32h. Takahide the Warlord, the THIRTIETH hero.</summary>
        public const string TakahideHeroIdValue = "hero-takahide";

        /// <summary>
        /// Milestone 9.21j. How many bodies of Razzle can stand on the board at once: RZ-Q1 (a), settled
        /// by the user on 2026-09-30 - four in all, the hero plus the three "Spawn up to 3 more of you"
        /// adds. A spawn takes only from this supply (p.19).
        /// </summary>
        public const int RazzleBodySupply = 4;

        /// <summary>
        /// Milestone 9.21j. The heroes that stand on several bodies, declared beside their roster rows by
        /// content identity, so a staged seat dealing that content (whatever its hero id) and a selected
        /// seat declare the same supply. Every other hero is one body.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int> DeclaredHeroBodies =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["razzle"] = RazzleBodySupply
            };

        /// <summary>Milestone 9.21j. The body supply a hero's content declares: one unless listed above.</summary>
        public static int HeroBodiesOf(HeroContentId contentId) =>
            contentId.IsDefined && DeclaredHeroBodies.TryGetValue(contentId.Value, out var bodies) ? bodies : 1;

        /// <summary>
        /// Milestone 9.21j. The body supplies a dealt setup's seats declare (9.21a's
        /// <see cref="HeroBodySupply"/>), in hero order - empty for every setup that seats no
        /// many-bodied hero.
        /// </summary>
        public static IReadOnlyList<HeroBodySupply> DeclaredHeroBodySupplies(
            IEnumerable<QuickGameHeroSetupState> heroes) =>
            Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(hero => HeroBodiesOf(hero.ContentId) > 1)
                .Select(hero => new HeroBodySupply(hero.HeroId, HeroBodiesOf(hero.ContentId)))
                .OrderBy(supply => supply.HeroId.Value, StringComparer.Ordinal)
                .ToArray());

        private static readonly (
            string HeroIdValue,
            string ContentId,
            string DisplayName,
            HeroComplexity Complexity,
            HeroRole Roles)[]
            StagedDealtHeroRows =
            {
                // Milestone 8.35z34 dealt her; 8.35z41 makes her pickable.
                (DodgerHeroIdValue, "dodger", "Dodger the Warlock", HeroComplexity.Challenging,
                    HeroRole.Damager | HeroRole.Sniper | HeroRole.Pusher |
                    HeroRole.Farming | HeroRole.Disabler),
                // Milestone 8.35z38 dealt her; 8.35z41 makes her pickable.
                (MisaHeroIdValue, "misa", "Misa the Samurai", HeroComplexity.Complex,
                    HeroRole.Tactician | HeroRole.Durable | HeroRole.Damager),
                // Milestone 8.35z46 dealt her and MISSED THIS ROW; 8.35z50 corrects it. The omission is
                // recorded rather than quietly fixed because it is the second instance of one class:
                // the rule that a dealing adds its row here lives in this file's own doc comment and in
                // no check, so a dealing that simply does not read it leaves its hero invisible to the
                // selector while every other tier passes. She was fully dealt, registered, gated and
                // journeyed at 8.35z46 - only unpickable.
                (SilverarrowHeroIdValue, "silverarrow", "Silverarrow the Pathfinder", HeroComplexity.Complex,
                    HeroRole.Sniper | HeroRole.Damager | HeroRole.Disabler |
                    HeroRole.Healer | HeroRole.Farming),
                // Milestone 8.35z50 deals him and makes him pickable in the same commit, which is what
                // the rule above asks for.
                (UrsafarHeroIdValue, "ursafar", "Ursafar the Savage", HeroComplexity.Complex,
                    HeroRole.Durable | HeroRole.Pusher | HeroRole.Farming | HeroRole.Melee),
                // Milestone 8.35z53 deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. Her prep deliberately withheld this row until the
                // registration flip landed: it makes her PICKABLE, and composing her match throws
                // until her capabilities are registered.
                (WhisperHeroIdValue, "whisper", "Whisper the Outcast", HeroComplexity.Complex,
                    HeroRole.Damager | HeroRole.Durable | HeroRole.Tactician | HeroRole.Pusher),
                // Milestone 8.37 deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. She is the twelfth and last hero of the package arc, so
                // with this row the selector offers every hero this build can deal.
                (TaliHeroIdValue, "tali", "Tali the Spiritcaller", HeroComplexity.VeryComplex,
                    HeroRole.Damager | HeroRole.Pusher | HeroRole.Healer |
                    HeroRole.Durable | HeroRole.Tokens),
                // Milestone 8.38e deals her and makes her pickable in the same commit, which is what
                // the rule above asks for - and which the 8.35z46 omission recorded there is the whole
                // reason to restate. She is the THIRTEENTH, so the sentence Tali's row above wrote -
                // "every hero this build can deal" - stays true only because this row is here.
                (SwiftHeroIdValue, "swift", "Swift the Sharpshooter", HeroComplexity.VeryComplex,
                    HeroRole.Sniper | HeroRole.Farming | HeroRole.Tactician),
                // Milestone 8.39i deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. She is the FOURTEENTH, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                (CutterHeroIdValue, "cutter", "Cutter the Sky Pirate", HeroComplexity.VeryComplex,
                    HeroRole.Tactician | HeroRole.Damager | HeroRole.Farming),
                // Milestone 8.40j deals him and makes him pickable in the same commit, which is what
                // the rule above asks for. He is the FIFTEENTH, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                (BainHeroIdValue, "bain", "Bain the Bounty Hunter", HeroComplexity.Complex,
                    HeroRole.Tactician | HeroRole.Sniper | HeroRole.Healer |
                    HeroRole.Farming | HeroRole.Durable),
                (GarrusHeroIdValue, "garrus", "Garrus the Gladiator", HeroComplexity.Complex,
                    HeroRole.Disabler | HeroRole.Durable | HeroRole.Tactician),
                (WidgetHeroIdValue, "widget", "Widget and Pyro", HeroComplexity.VeryComplex,
                    HeroRole.Tactician | HeroRole.Pusher | HeroRole.Damager),
                // Milestone 8.57g deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. She is the EIGHTEENTH, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                (RowennaHeroIdValue, "rowenna", "Rowenna the Vanguard", HeroComplexity.Complex,
                    HeroRole.Melee | HeroRole.Durable | HeroRole.Healer |
                    HeroRole.Farming | HeroRole.Pusher | HeroRole.Tactician),
                // Milestone 8.61j deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. She is the NINETEENTH, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                (MinHeroIdValue, "min", "Min the Dragonmonk", HeroComplexity.Complex,
                    HeroRole.Tokens | HeroRole.Disabler | HeroRole.Damager | HeroRole.Melee),
                // Milestone 8.69g deals him and makes him pickable in the same commit, which is what
                // the rule above asks for. He is the TWENTIETH, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                //
                // HIS PROFILE FACTS ARE THE USER'S ONE NAMED REQUIREMENT FOR THIS HERO ("keep in mind
                // he needs to be correctly implemented in new hero pick screen"), so they are read off
                // the extracted selection front rather than guessed: `active-hero-cards/_selection-
                // cards/Brynn__front.png` shows THREE filled stars - p.21's "Very Complex" - and the
                // three role icons TACTICIAN, DAMAGER and DURABLE, in that printed order. The dashboard
                // names him "Brynn the Seeker"; the Tabletopia card group says "the Tracker" and is not
                // the authority. `HeroSelectionStageTests` pins both facts and
                // `PrivateLobbyPlayModeTests` drives the star and role filters over his button.
                (BrynnHeroIdValue, "brynn", "Brynn the Seeker", HeroComplexity.VeryComplex,
                    HeroRole.Tactician | HeroRole.Damager | HeroRole.Durable),
                // Milestone 8.71h deals him and makes him pickable in the same commit, which is what
                // the rule above asks for. He is the TWENTY-FIRST, so the sentence Tali's row wrote -
                // "every hero this build can deal" - stays true only because this row is here too.
                //
                // HIS PROFILE FACTS ARE THE USER'S ONE NAMED REQUIREMENT FOR THIS HERO ("Make sure
                // pick screen is correctly imeplented"), so they are read off the extracted selection
                // front rather than guessed: `active-hero-cards/_selection-cards/Hanu__front.png`
                // shows THREE filled stars - p.21's "Very Complex" - and the three role icons
                // TACTICIAN, SNIPER and PUSHER, in that printed order. The dashboard names him "Hanu
                // the Trickster" and the Tabletopia card group agrees, which the last two dealings'
                // rows could not say. `HeroSelectionStageTests` pins both facts and
                // `PrivateLobbyPlayModeTests` drives the star and role filters over his button.
                (HanuHeroIdValue, "hanu", "Hanu the Trickster", HeroComplexity.VeryComplex,
                    HeroRole.Tactician | HeroRole.Sniper | HeroRole.Pusher),
                // Milestone 8.75b deals her and makes her pickable in the same commit, which is what
                // the rule above asks for. She is the TWENTY-SECOND, and the FIRST FOUR-STAR hero
                // this build has ever offered: p.21's top complexity band, `HeroComplexity.Wizard`,
                // which twenty-one dealings left unused because no dealt hero printed four filled
                // stars. The picker's star filter has always drawn four buttons; this row is the
                // first thing that can be found behind the fourth.
                //
                // Her profile facts are read off the extracted selection front rather than guessed -
                // `active-hero-cards/_selection-cards/` and the dashboard, inspected at the 8.75
                // inventory pass and again by the artwork block: FOUR filled stars and the five role
                // icons SNIPER, DAMAGER and TOKENS on the top row with TACTICIAN and PUSHER below, in
                // that printed order. FIVE roles is also a first; no hero before her printed more
                // than three, which is why the filter case her dealing owes drives five of them.
                // The dashboard names her "Ignatia the Mad" where the selection card says only
                // "Ignatia", and the dashboard is the authority every row here uses.
                // `HeroSelectionStageTests` pins both facts and `PrivateLobbyPlayModeTests` drives
                // the star and role filters over her button.
                (IgnatiaHeroIdValue, "ignatia", "Ignatia the Mad", HeroComplexity.Wizard,
                    HeroRole.Sniper | HeroRole.Damager | HeroRole.Tokens | HeroRole.Tactician |
                        HeroRole.Pusher),
                (TrinketsHeroIdValue, "trinkets", "Trinkets the Scavenger",
                    HeroComplexity.VeryComplex,
                    HeroRole.Sniper | HeroRole.Damager | HeroRole.Pusher | HeroRole.Tokens),
                (SnorriHeroIdValue, "snorri", "Snorri the Runescribe",
                    HeroComplexity.Wizard,
                    HeroRole.Sniper | HeroRole.Farming | HeroRole.Durable |
                    HeroRole.Damager | HeroRole.Pusher | HeroRole.Healer),
                // Milestone 8.112g. His selection front prints THREE stars and the roles Melee,
                // Tokens, Durable, Disabler and Tactician (inventory 8.112); the dashboard names him
                // "Mrak the Rockshaper".
                (MrakHeroIdValue, "mrak", "Mrak the Rockshaper",
                    HeroComplexity.VeryComplex,
                    HeroRole.Melee | HeroRole.Tokens | HeroRole.Durable |
                    HeroRole.Disabler | HeroRole.Tactician),
                // Milestone 8.114f. His selection front prints THREE stars and the roles Tokens,
                // Pusher, Durable, Sniper, Tactician and Healer (inventory 8.114); the dashboard names
                // him "Wuk the Grove Keeper".
                (WukHeroIdValue, "wuk", "Wuk the Grove Keeper",
                    HeroComplexity.VeryComplex,
                    HeroRole.Tokens | HeroRole.Pusher | HeroRole.Durable |
                    HeroRole.Sniper | HeroRole.Tactician | HeroRole.Healer),
                // Milestone 9.8f. His selection front prints THREE stars and the roles Melee, Tokens,
                // Farming, Durable and Pusher (inventory 9.8); the dashboard names him "Mortimer the
                // Awakener".
                (MortimerHeroIdValue, "mortimer", "Mortimer the Awakener",
                    HeroComplexity.VeryComplex,
                    HeroRole.Melee | HeroRole.Tokens | HeroRole.Farming |
                    HeroRole.Durable | HeroRole.Pusher),
                // Milestone 9.21j. His selection front prints FOUR stars and the roles Tactician and
                // Melee (inventory 9.21, RZ-01); the dashboard names him "Razzle the Ringmaster"
                // (RZ-02). His four-body supply is declared by content in `DeclaredHeroBodies` above.
                (RazzleHeroIdValue, "razzle", "Razzle the Ringmaster",
                    HeroComplexity.Wizard,
                    HeroRole.Tactician | HeroRole.Melee),
                // Milestone 9.30i. His selection front prints FOUR stars and the roles Sniper,
                // Tactician, Farming, Damager, Pusher and Tokens (inventory 9.30, GY-01); the dashboard
                // names him "Gydion the Archwizard" (GY-02). His spellbook is his content's own
                // (GydionCardContent.CreateHand), so nothing else is declared here.
                (GydionHeroIdValue, "gydion", "Gydion the Archwizard",
                    HeroComplexity.Wizard,
                    HeroRole.Sniper | HeroRole.Tactician | HeroRole.Farming |
                    HeroRole.Damager | HeroRole.Pusher | HeroRole.Tokens),
                // Milestone 9.32h. His selection front prints FOUR stars and the roles Durable, Sniper,
                // Tactician, Damager and Farming (inventory 9.32); the dashboard names him "Takahide the
                // Warlord". He uses no tokens or markers (TK-02).
                (TakahideHeroIdValue, "takahide", "Takahide the Warlord",
                    HeroComplexity.Wizard,
                    HeroRole.Durable | HeroRole.Sniper | HeroRole.Tactician |
                    HeroRole.Damager | HeroRole.Farming)
            };

        /// <summary>
        /// Milestone 8.35z41. Every hero this build can put on a board: the printed line-up's own
        /// rows first, in setup step 7's order, then the staged-dealt heroes in dealing order.
        /// </summary>
        /// <remarks>
        /// <para>This is what a hero-selection screen may OFFER, and it is a SUPERSET of
        /// <see cref="RegisteredHeroes"/> rather than the same list. Milestone 8.35z36 read the
        /// printed table for the selector's roster where its own brief said the dealt one, so a hero
        /// could be fully dealt, registered, gated and journeyed and still be unpickable - which
        /// capped the selector at the shipped six while the build dealt eight.</para>
        /// <para>Derived, never counted: a dealing milestone adds one row here (or a row to the
        /// printed table, which flows in through the concatenation) and the selector, its refusals
        /// and the lobby's option list all grow in the same commit. The order is deliberate - the
        /// printed six keep their setup step 7 positions at the head of every picker.</para>
        /// </remarks>
        public static IReadOnlyList<RegisteredHero> DealtHeroes { get; } = RegisteredHeroes
            .Concat(StagedDealtHeroRows.Select(row => new RegisteredHero(
                row.HeroIdValue, new HeroContentId(row.ContentId), row.DisplayName,
                row.Complexity, row.Roles)))
            .ToArray();

        /// <summary>
        /// Milestone 8.35z36. The printed line-up as pairs of hero and the side step 7
        /// deals it to - the shipped demo arrangement, which selection generalises.
        /// </summary>
        public static IReadOnlyList<(RegisteredHero Hero, TeamSide Side)> PrintedLineupSides { get; } =
            OfficialHeroRows
                .Select(row => (
                    new RegisteredHero(
                        row.HeroIdValue, new HeroContentId(row.ContentId), row.DisplayName,
                        row.Complexity, row.Roles),
                    row.Side))
                .ToArray();

        /// <summary>
        /// Milestone 8.35z36. The marked hero spawn points of both Throne zones, in the
        /// order the printed line-up fills them.
        /// </summary>
        /// <remarks>
        /// Derived from the same rows, because that is where the points are written down; what
        /// changes with a selector is that the point belongs to the SEAT rather than to the hero
        /// standing on it. See <see cref="HeroSpawnSlot"/> for why that separation is load-bearing.
        /// </remarks>
        public static IReadOnlyList<HeroSpawnSlot> HeroSpawnSlots { get; } = OfficialHeroRows
            .Select(row => new HeroSpawnSlot(row.Side, row.Spawn, row.Space))
            .ToArray();

        /// <summary>
        /// The official board for any of p.5-6's four printed setups: the format's own heroes on
        /// their hero spawn points, the format's Wave and Life counts, and setup step 10's minions.
        ///
        /// <para><b>Three numbers come from the format and one does not.</b> The Wave count is setup
        /// step 4's choice of game length and the Life count is setup step 5's table entry for that
        /// length at that player count, both read off <see cref="MatchFormat"/>, which transcribes
        /// them. The hero count is setup step 2's equal teams. The minions are none of those: setup
        /// step 10 places one Ranged, four Melee and one Heavy per team and says nothing about the
        /// player count, so every format shares the same twelve placements.</para>
        ///
        /// <para><b>Which spawn points a short team uses is the one thing p.6 does not print, and it
        /// is recorded rather than invented.</b> A Throne zone carries three marked hero spawn
        /// points, and a two-hero team occupies two of them; step 9 says only to place the miniatures
        /// on the marked spaces of their own Throne Zone, and no step says which. The build answers
        /// it the way milestone 8.25 answered the identical question about which of a team's three
        /// dashboards goes to which of its players - the points are interchangeable at setup and a
        /// draw between them would change nothing any rule, projection or snapshot can observe - by
        /// stating it once: each hero keeps the spawn point it already stands on in the six-player
        /// board, so a 2v2 Wasp stands where every existing journey has always found her.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateOfficial(MatchFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));

            var mapId = new MapId("official-single-lane");
            ConfiguredSpawnPointReference Spawn(string value) =>
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId(value));
            ConfiguredSpaceReference Space(string value) =>
                new ConfiguredSpaceReference(mapId, new SpaceId(value));

            var heroes = OfficialHeroRows
                .Where(row => row.Side == TeamSide.Red)
                .Take(format.HeroesPerTeam)
                .Concat(OfficialHeroRows
                    .Where(row => row.Side == TeamSide.Blue)
                    .Take(format.HeroesPerTeam))
                .Select(row => new QuickGameHeroPlacementDefinition(
                    new HeroId(row.HeroIdValue),
                    new HeroContentId(row.ContentId),
                    row.DisplayName,
                    Spawn(row.Spawn),
                    Space(row.Space)))
                .ToArray();

            return new QuickGameSetupConfiguration(
                new MapDefinitionReference(mapId, 1),
                format.WaveCount,
                format.LifeCountPerTeam,
                heroes,
                CreateOfficialMinionPlacements(mapId));
        }

        /// <summary>
        /// The staged one-hero-per-team board with both seats dealing Wasp's printed set.
        ///
        /// It stays exactly what it was: every fixture that measures the mirror measures this, and
        /// milestone 8.20's Arien integration is a second configuration beside it rather than a change
        /// to it.
        /// </summary>
        public static QuickGameSetupConfiguration CreateMirroredWaspOneVsOne() =>
            CreateOneVsOne(new HeroContentId("wasp"), "Wasp");

        /// <summary>
        /// Milestone 8.20. The same board with Arien on the Blue seat - the staged one-hero-per-team
        /// match the scene composes and a player plays.
        ///
        /// <para>Only the Blue hero's content identity and display name differ, because only those two
        /// are about which hero is standing there: the map, the spawn points, both hero placements and
        /// all twelve minions are the same board either way, and duplicating them would be two boards
        /// that could drift apart. The content identity is what
        /// <c>Presentation.Units.Heroes.HeroTokenArtwork</c> addresses the prepared token by, so
        /// binding it here is what puts Arien's face on the piece.</para>
        ///
        /// <para>This is not the six-player setup and does not pretend to be one. The
        /// one-hero-per-team development deviation stands, both official teams still field one hero,
        /// and milestone 8.25 owns the official configuration and all six spawn assignments.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusArienOneVsOne() =>
            CreateOneVsOne(new HeroContentId("arien"), "Arien");

        /// <summary>
        /// Milestone 8.21. The same board with Brogan on the Blue seat, for the fixtures whose subject
        /// is his printed set.
        ///
        /// <para>It is a third one-hero-per-team staging rather than a wider match, for the reason the
        /// second one records: milestone 8.25 owns the official six-player configuration and all six
        /// spawn assignments. The scene a player loads is unchanged and still deals Wasp against
        /// Arien; this arm exists so a fixture can put Brogan where a player's input can reach
        /// him.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusBroganOneVsOne() =>
            CreateOneVsOne(new HeroContentId("brogan"), "Brogan");

        /// <summary>
        /// Milestone 8.38f. The same board with Swift the Sharpshooter on the Blue seat, for the
        /// fixtures whose subject is her printed set.
        ///
        /// <para><b>Milestone 8.38e added her five <c>SceneCardContent</c> members and her three
        /// <c>MatchConfiguration</c> stagings but not this arm, so every staged Swift scene fell
        /// through <c>SelectSetupConfiguration</c>.s default to Arien.s setup.</b> The configuration
        /// deals her hand to whichever hero the setup puts on Blue - <c>CreateVerifiedSwiftHand</c>
        /// takes the owner id rather than naming one - so the scene composed her cards onto a seat
        /// whose content id was <c>arien</c>, and any fixture selecting her seat by content identity
        /// found none. No Edit Mode tier can see it: the fall-through is in scene composition.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusSwiftOneVsOne() =>
            CreateOneVsOne(new HeroContentId("swift"), "Swift");

        /// <summary>
        /// Milestone 8.39i. The same board with Cutter the Sky Pirate on the Blue seat, for the
        /// fixtures whose subject is her printed set.
        ///
        /// <para>Written in the SAME change as her five <c>SceneCardContent</c> members and her three
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// above teaches: without this arm every staged scene of hers falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HER cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusCutterOneVsOne() =>
            CreateOneVsOne(new HeroContentId("cutter"), "Cutter");

        /// <summary>
        /// Milestone 8.40j. The same board with Bain the Bounty Hunter on the Blue seat, for the
        /// fixtures whose subject is his printed set.
        ///
        /// <para>Written in the SAME change as his five <c>SceneCardContent</c> members and his three
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of his falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HIS cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusBainOneVsOne() =>
            CreateOneVsOne(new HeroContentId("bain"), "Bain");

        public static QuickGameSetupConfiguration CreateStagedWaspVersusGarrusOneVsOne() =>
            CreateOneVsOne(new HeroContentId("garrus"), "Garrus");

        public static QuickGameSetupConfiguration CreateStagedWaspVersusWidgetOneVsOne() =>
            CreateOneVsOne(new HeroContentId("widget"), "Widget");

        /// <summary>
        /// Milestone 8.61j. The same board with Min the Dragonmonk on the Blue seat, for the fixtures
        /// whose subject is her printed set. The NINETEENTH hero.
        ///
        /// <para>Written in the SAME change as her five <c>SceneCardContent</c> members and her three
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of hers falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HER cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        ///
        /// <para><b>She needs NO ally board, and it is worth saying so rather than leaving the
        /// absence to be read as an oversight.</b> Not one of her nineteen faces names a friendly
        /// hero, a friendly unit or an ally of any kind: her Stances push ENEMY units, her mines wait
        /// for an ENEMY hero, both Grenades name ENEMY heroes adjacent to the token, and everything
        /// else - the Smoke bomb, the three Blocks, the two doubling Skills and the Ultimate - is
        /// about her own seat and her own pieces. Rowenna's, Brogan's and Tali's boards exist for a
        /// clause hers never prints.</para>
        ///
        /// <para><b>What this staging reaches, measured at the dealing rather than assumed.</b> An
        /// enemy MINION stands beside her, which is what her five Stances' "adjacent to you" needs
        /// for both the Attack and the push; the enemy HERO is two spaces away, which is inside the
        /// Radius 3 her Silver and her three mine faces print and the Radius 2 both Grenades print,
        /// and outside every "adjacent" her set prints - so her mines have empty spaces to go into
        /// and her Grenade has a space beside him. <c>MinDealingTests</c> measures all of it and
        /// writes the coordinates out.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusMinOneVsOne() =>
            CreateOneVsOne(new HeroContentId("min"), "Min");

        /// <summary>
        /// Milestone 8.69g. The same board with Brynn the Seeker on the Blue seat, for the fixtures
        /// whose subject is his printed set. The TWENTIETH hero.
        ///
        /// <para>Written in the SAME change as his five <c>SceneCardContent</c> members and his three
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of his falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HIS cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        ///
        /// <para><b>He needs NO ally board, and it is worth saying so rather than leaving the absence
        /// to be read as an oversight.</b> The one clause of his that names his own side is the
        /// guides' "a friendly unit adjacent to you ... a different friendly unit in radius", and p.17
        /// makes a friendly MINION as good a subject as a friendly hero - the engaged staging leaves a
        /// friendly melee minion in his ring and more of his own side inside every Radius his Blues
        /// print. Rowenna's ally board exists because her riders name a friendly HERO in radius and
        /// exclude herself; Brynn prints no such word. <c>BrynnDealingTests</c> measures the friendly
        /// units this board actually leaves him rather than assuming them.</para>
        ///
        /// <para><b>What this staging reaches, measured at the dealing rather than assumed.</b> The
        /// enemy HERO stands two spaces away, which is inside every printed Radius his Silver, Blue and
        /// Green faces carry and outside p.19's ring - so his three traps and Decoy reach him through
        /// their radius arm and his "adjacent to you" arms need a step or a minion first. How many
        /// obstacles stand beside each hero is the one measurement his whole set turns on, and
        /// <c>BrynnDealingTests</c> writes both counts into the run output for 8.69h to start
        /// from.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusBrynnOneVsOne() =>
            CreateOneVsOne(new HeroContentId("brynn"), "Brynn");

        /// <summary>
        /// Milestone 8.71h. The same board with Hanu the Trickster on the Blue seat, for the fixtures
        /// whose subject is his printed set. The TWENTY-FIRST hero.
        ///
        /// <para>Written in the SAME change as his eight <c>SceneCardContent</c> members and his four
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of his falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HIS cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        ///
        /// <para><b>He ALSO needs an ally board, and this is not it.</b> Two of his faces name a
        /// friendly HERO with himself excluded - This Way! and That Way! ask one to choose the
        /// distance on their own seat - so
        /// <see cref="CreateStagedWaspVersusHanuWithAllyOneVsTwo"/> is the smallest board on which
        /// either co-move has a candidate at all. This one-versus-one arm is where every other face of
        /// his lives: both swap families, the Reds' two bullets over the friendly MINION p.17's
        /// Friendly admits, Hurry up!'s rewrite of the enemy hero's unresolved card, and the
        /// Ultimate's delegated action.</para>
        ///
        /// <para><b>What this staging reaches, measured at the dealing rather than assumed.</b> The
        /// enemy HERO stands two spaces away - inside every printed Radius his Blues and Greens carry
        /// and inside every printed Range his Reds and his Silver carry, and outside p.19's ring, so
        /// his journeys and Hear Nothing reach him while his "adjacent to you" bullets need a minion
        /// first. <c>HanuDealingTests</c> writes the coordinates into the run output for 8.71i to
        /// start from.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusHanuOneVsOne() =>
            CreateOneVsOne(new HeroContentId("hanu"), "Hanu");

        /// <summary>
        /// Milestone 8.71h. Wasp on Red against Hanu and Arien on Blue - the shape
        /// <see cref="CreateStagedWaspVersusRowennaWithAllyOneVsTwo"/> already proves, with Hanu in the
        /// seat under test and an ally beside him.
        ///
        /// <para>This Way! and That Way! print "A friendly hero in radius chooses a distance ..." and
        /// milestone 8.71e composed that selection with <c>includeSelf</c> false, so his own seat is
        /// excluded and this is the smallest board on which either co-move has a candidate at all -
        /// and therefore the only one on which the friend's cross-seat distance choice can be raised.
        /// It is also the board on which the five Red bullets that print "adjacent to your friendly
        /// hero" can be satisfied by a HERO rather than only by the friendly melee minion.</para>
        ///
        /// <para>Arien holds the ally seat at <c>blue-hero-02</c>, for the identical reason Brogan's,
        /// Tali's and Rowenna's ally configurations give it to him: he is a hero this build already
        /// deals, so a fixture proves the selection against real content rather than inventing a
        /// printed set. The spawn is the same two spaces from the first that Rowenna's board uses,
        /// which is inside the Radius 3 both co-moves print and inside the Radius 2 This Way!'s own
        /// shorter distance needs.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusHanuWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("hanu"), "Hanu");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>
        /// Milestone 8.75b. The same board with Ignatia the Mad on the Blue seat, for the fixtures
        /// whose subject is her printed set. The TWENTY-SECOND hero.
        ///
        /// <para>Written in the SAME change as her eight <c>SceneCardContent</c> members and her four
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of hers falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HER cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        ///
        /// <para><b>She ALSO needs an ally board, and this is not it.</b> Two of her faces name a
        /// FRIENDLY hero with herself excluded - Searing Heat's and Scorching Blaze's sun lines, "Move
        /// a friendly hero in radius 2 (2 or 3) spaces in a straight line", and Unstable Portal's and
        /// Chaos Gate's sun lines swap with a friendly UNIT, which p.17's Friendly lets the melee
        /// minion satisfy - so <see cref="CreateStagedWaspVersusIgnatiaWithAllyOneVsTwo"/> is the
        /// smallest board on which either push's sun arm has a candidate at all. Every other face of
        /// hers lives here: all seven attacks under both coin faces, the whole discard line, both
        /// swaps' moon arms and all three magma Paths.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusIgnatiaOneVsOne() =>
            CreateOneVsOne(new HeroContentId("ignatia"), "Ignatia");

        public static QuickGameSetupConfiguration CreateStagedWaspVersusTrinketsOneVsOne() =>
            CreateOneVsOne(new HeroContentId("trinkets"), "Trinkets");

        public static QuickGameSetupConfiguration CreateStagedWaspVersusSnorriOneVsOne() =>
            CreateOneVsOne(new HeroContentId("snorri"), "Snorri");

        /// <summary>Milestone 8.112g. Wasp on Red against Mrak on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusMrakOneVsOne() =>
            CreateOneVsOne(new HeroContentId("mrak"), "Mrak");

        /// <summary>
        /// Milestone 8.112g. Mrak on Red against Wasp and Arien on Blue - the board on which Stone
        /// Grip and Rock and a Hard Place can reach two enemy heroes at once.
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedMrakVersusWaspAndArien()
        {
            var snorri = CreateStagedSnorriVersusWaspAndArien();
            return new QuickGameSetupConfiguration(
                snorri.RequiredMap,
                snorri.WaveCount,
                snorri.LifeCountPerTeam,
                snorri.Heroes.Select(hero => hero.ContentId.Value == "snorri"
                    ? new QuickGameHeroPlacementDefinition(
                        hero.HeroId, new HeroContentId("mrak"), "Mrak", hero.SpawnPoint, hero.OccupiedSpace)
                    : hero),
                snorri.Minions);
        }

        /// <summary>Milestone 9.30i. Wasp on Red against Gydion on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusGydionOneVsOne() =>
            CreateOneVsOne(new HeroContentId("gydion"), "Gydion");

        /// <summary>Milestone 9.32h. Wasp on Red against Takahide on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusTakahideOneVsOne() =>
            CreateOneVsOne(new HeroContentId("takahide"), "Takahide");

        /// <summary>Milestone 9.21j. Wasp on Red against Razzle on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusRazzleOneVsOne() =>
            CreateOneVsOne(new HeroContentId("razzle"), "Razzle");

        /// <summary>Milestone 9.8f. Wasp on Red against Mortimer on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusMortimerOneVsOne() =>
            CreateOneVsOne(new HeroContentId("mortimer"), "Mortimer");

        /// <summary>Milestone 8.114f. Wasp on Red against Wuk on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusWukOneVsOne() =>
            CreateOneVsOne(new HeroContentId("wuk"), "Wuk");

        /// <summary>
        /// Milestone 8.114f. Wuk with Arien as his Blue ally, on Hanu's ally board: Tree of Plenty's
        /// and Abundance's "a friendly hero in radius may retrieve" and the swaps' "a friendly unit"
        /// have a hero candidate only here (p.17: Friendly never includes Wuk).
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusWukWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("wuk"), "Wuk");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>Milestone 8.93g. Snorri on Red against Wasp and Arien on Blue.</summary>
        public static QuickGameSetupConfiguration CreateStagedSnorriVersusWaspAndArien()
        {
            var mapId = new MapId("official-single-lane");
            ConfiguredSpawnPointReference Spawn(string value) =>
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId(value));
            ConfiguredSpaceReference Space(string value) =>
                new ConfiguredSpaceReference(mapId, new SpaceId(value));
            return new QuickGameSetupConfiguration(
                new MapDefinitionReference(mapId, 1),
                3,
                4,
                new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.RedHeroIdValue),
                        new HeroContentId("snorri"), "Snorri",
                        Spawn("red-hero-01"), Space("q-2_r-5")),
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueHeroIdValue),
                        new HeroContentId("wasp"), "Wasp",
                        Spawn("blue-hero-01"), Space("q-3_r10")),
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"), "Arien",
                        Spawn("blue-hero-02"), Space("q-1_r10"))
                },
                CreateOfficialMinionPlacements(mapId));
        }

        /// <summary>Milestone 8.93g. Snorri with Arien as his Blue ally, on Hanu's ally board.</summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusSnorriWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("snorri"), "Snorri");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>
        /// Milestone 8.75b. Wasp on Red against Ignatia and Arien on Blue - the shape
        /// <see cref="CreateStagedWaspVersusHanuWithAllyOneVsTwo"/> and
        /// <see cref="CreateStagedWaspVersusRowennaWithAllyOneVsTwo"/> already prove, with Ignatia in
        /// the seat under test and an ally beside her.
        ///
        /// <para>Searing Heat and Scorching Blaze print "Move a friendly hero in radius ..." on their
        /// SUN faces and the composed selection excludes her own seat (p.17: "Friendly" never includes
        /// you), so this is the smallest board on which either sun arm has a candidate - and therefore
        /// the only one on which a straight-line push under somebody else's feet can be raised at
        /// all.</para>
        ///
        /// <para>Arien holds the ally seat at <c>blue-hero-02</c>, for the identical reason Brogan's,
        /// Tali's, Rowenna's and Hanu's ally configurations give it to him: he is a hero this build
        /// already deals, so a fixture proves the selection against real content rather than inventing
        /// a printed set. The spawn is two spaces from the first, which is inside the Radius 3 Searing
        /// Heat prints and inside the Radius 4 Unstable Portal and Chaos Gate print.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusIgnatiaWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("ignatia"), "Ignatia");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>
        /// Milestone 8.57g. The same board with Rowenna the Vanguard on the Blue seat, for the
        /// fixtures whose subject is her printed set.
        ///
        /// <para>Written in the SAME change as her five <c>SceneCardContent</c> members and her three
        /// <c>MatchConfiguration</c> stagings, which is the whole of what milestone 8.38f's repair
        /// teaches: without this arm every staged scene of hers falls through
        /// <c>SelectSetupConfiguration</c>'s default to Arien's setup, and the hand factory - which
        /// takes an owner id rather than naming a hero - then deals HER cards to a seat whose content
        /// identity is <c>arien</c>. No offline tier can see that; only a loaded scene composes.</para>
        ///
        /// <para><b>What this staging reaches and what it cannot, measured rather than assumed.</b>
        /// Her Blue swap names a friendly UNIT in range, which p.8's waves answer with minions, and
        /// her Greens, her surrenders and Throw the Gauntlet all name an ENEMY hero, which the Red
        /// seat is. The one thing a one-Blue-hero board cannot offer her is a FRIENDLY HERO: her
        /// three Red riders print "a friendly hero in radius" and milestone 8.57a composed that with
        /// <c>includeSelf</c> false, so on this board the rider finds no candidate and stops at
        /// p.10's impossible step - the Attack still performs and the Paragons' repeat is never
        /// offered. <see cref="CreateStagedWaspVersusRowennaWithAllyOneVsTwo"/> is the configuration
        /// that arm needs, exactly as Brogan's War Drummer needed one.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusRowennaOneVsOne() =>
            CreateOneVsOne(new HeroContentId("rowenna"), "Rowenna");

        /// <summary>
        /// Milestone 8.57g. Wasp on Red against Rowenna and Arien on Blue - the shape
        /// <see cref="CreateStagedWaspVersusTaliWithAllyOneVsTwo"/> already proves, with Rowenna in
        /// the seat under test and an ally beside her.
        ///
        /// <para>Three of her five Reds pay "a friendly hero in radius" a coin or a retrieve, and the
        /// two Paragons gate their p.10 repeat on that rider having been performed. Her own seat is
        /// excluded from the selection, so this is the smallest board on which any of those five
        /// riders has a candidate at all - and therefore the only one on which the repeat can be
        /// offered.</para>
        ///
        /// <para>Arien holds the ally seat at <c>blue-hero-02</c>, for the identical reason Brogan's
        /// and Tali's ally configurations give it to him: he is a hero this build already deals, so a
        /// fixture proves the selection against real content rather than inventing a printed set. The
        /// spawn is two spaces from the first, which is inside every Radius her Reds print.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusRowennaWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("rowenna"), "Rowenna");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>
        /// Milestone 8.22. The same board with Sabina on the Blue seat, for the fixtures whose subject
        /// is her printed set.
        ///
        /// <para>It is a fourth one-hero-per-team staging rather than a wider match, for the reason the
        /// second and third record: milestone 8.25 owns the official six-player configuration and all
        /// six spawn assignments. The scene a player loads is unchanged and still deals Wasp against
        /// Arien; this arm exists so a fixture can put Sabina where a player's input can reach her.</para>
        ///
        /// <para><b>She needs no second Blue seat</b>, unlike Brogan. His two coin-granting Skills name
        /// a friendly <em>hero</em> and a one-hero team gave that selection no candidate; hers command
        /// minions, and p.8's waves put twelve of those on this board before anybody acts.</para>
        ///
        /// <para>The display name is the epithet question S-01 answered on 2026-08-25 - the rulebook's
        /// setup step 7 name - and it is a label on the seat rather than an identity: the content id
        /// stays <c>sabina</c>, which is what the prepared token is addressed by and what every rule
        /// that asks reads.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusSabinaOneVsOne() =>
            CreateOneVsOne(new HeroContentId("sabina"), "Sabina the Gunslinger");

        /// <summary>
        /// Milestone 8.23. The same board with Tigerclaw on the Blue seat, for the fixtures whose
        /// subject is his printed set.
        ///
        /// <para><b>He is a Red hero on the pitch roster and takes the Blue seat here</b>, exactly as
        /// Brogan and Sabina do and for the reason both record: milestone 8.25 owns the official
        /// six-player configuration and all six spawn assignments, and this staging is one seat per
        /// team. Which side of a one-versus-one board a printed set is dealt to is a fixture fact; the
        /// roster slot p.5 prints for him is not changed by it.</para>
        ///
        /// <para><b>He needs no second Blue seat.</b> His two marker cards print "a hero in range"
        /// rather than "an enemy hero", so a friendly hero would widen their candidate set - but Wasp
        /// across the board already satisfies them, and his three thieving Greens name an enemy hero
        /// explicitly. The friendly arm is therefore content this staging cannot reach rather than
        /// content it gets wrong, which the fixtures record where it matters.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusTigerclawOneVsOne() =>
            CreateOneVsOne(new HeroContentId("tigerclaw"), "Tigerclaw the Cutpurse");

        /// <summary>
        /// Milestone 8.24. Wasp on Red against Xargatha on Blue - the sixth verified hero and the last
        /// of the 8.17 roster.
        ///
        /// <para><b>She is a Red hero on the pitch roster and takes the Blue seat here</b>, exactly as
        /// Brogan, Sabina and Tigerclaw do and for the reason all three record: milestone 8.25 owns the
        /// official six-player configuration and all six spawn assignments, and this staging is one
        /// seat per team.</para>
        ///
        /// <para><b>She needs no second Blue seat, and the reason is different from Tigerclaw's.</b>
        /// Every clause of hers that names another unit names an enemy one - "an enemy unit not
        /// adjacent to you", "enemy heroes in radius", "an enemy ranged minion", "an enemy melee minion
        /// adjacent to you" - so a friendly hero would widen no candidate set she has. Her set is the
        /// first in the roster with no friendly-facing arm at all, which is why one seat per team
        /// reaches all nineteen faces.</para>
        ///
        /// <para><b>What this staging does need is minions, and more of them than any earlier hero.</b>
        /// Five of her faces are answered only by minions - the three Green Movements move one, and
        /// Constrict and Final Embrace defeat one - and three of those five narrow it to a printed
        /// class. The ordinary Quick Game minion setup supplies all three of p.22's classes, so nothing
        /// here is staged specially; it is recorded because a fixture that cleared her neighbourhood of
        /// minions would make five faces unreachable without failing.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusXargathaOneVsOne() =>
            CreateOneVsOne(new HeroContentId("xargatha"), "Xargatha the Changed");

        /// <summary>
        /// Milestone 8.35z34. Wasp on Red against Dodger the Warlock on Blue, the seventh hero this
        /// build deals and the first from outside the mapped pitch roster.
        ///
        /// <para>She needs no ally seat: every clause of hers that names another hero names an
        /// <em>enemy</em> one - "an enemy hero in radius", "enemy heroes in radius", "a hero in range
        /// who has one or more cards in the discard" - so a friendly hero would widen no candidate set
        /// she has. The one thing her kit names on her own side is a friendly MINION, which p.8's waves
        /// supply in every configuration.</para>
        ///
        /// <para><b>What this staging needs is minion SPAWN POINTS, which is a demand no earlier hero
        /// made of it.</b> Nine of her nineteen faces read empty minion Spawn points in the Battle
        /// Zone, and two respawn into one. The official map declares those points and the ordinary
        /// Quick Game setup stands a minion on each, so nothing here is staged specially - but the
        /// consequence is worth recording, because it is content rather than a fixture problem: at
        /// setup her count reads ZERO and p.19 admits nothing back. Her kit switches on as the wave
        /// dies, and a journey that wants a Respawn has to remove a miniature first, exactly as p.19
        /// says a player must.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusDodgerOneVsOne() =>
            CreateOneVsOne(new HeroContentId("dodger"), "Dodger the Warlock");

        /// <summary>
        /// Milestone 8.35z38. Wasp on Red against Misa the Samurai on Blue, the eighth hero this build
        /// deals and the second from outside the mapped pitch roster.
        ///
        /// <para>She needs no ally seat, and the reason is stronger than Dodger's: not one of her
        /// nineteen faces names a friendly hero at all. What her set asks for is enemy UNITS in
        /// particular arrangements - a minion adjacent to her, two or more enemy units flanking one
        /// space, an enemy standing on a straight line out of her own - and the one-vs-one board with
        /// p.8's waves supplies every one of them.</para>
        ///
        /// <para><b>What this staging cannot guarantee, stated rather than assumed.</b> RULING 1 reads
        /// "at maximum range" as EXACTLY the printed number, so Watch How I Soar's first bullet wants an
        /// empty space exactly four away and Living Tornado a unit pair exactly three apart. Both are
        /// ordinary questions with ordinary answers here, and both can legitimately have none - which
        /// milestone 8.35z38 journeys as an outcome rather than staging around.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusMisaOneVsOne() =>
            CreateOneVsOne(new HeroContentId("misa"), "Misa the Samurai");

        /// <summary>
        /// Milestone 8.35z46. Wasp on Red against Silverarrow the Pathfinder on Blue, the
        /// ninth hero this build deals and the third from outside the mapped pitch roster.
        ///
        /// <para>She needs no ally seat. Seventeen of her nineteen faces name an <em>enemy</em> or
        /// nobody at all - every Attack, the three movement-restriction Blues, the two "End of turn:"
        /// discards and the three Green displacement Skills - and the two that do not, Fae Healing and
        /// Nature's Blessing, print "a hero in radius", which RULING 8 reads as any hero including
        /// herself. So on this board both retrieval faces already offer two candidate classes, the
        /// enemy across the map and Silverarrow herself, and the only arm a second Blue seat would add
        /// is a friendly hero who is not her. That arm is recorded as unreached by this staging rather
        /// than composed away, exactly as Tigerclaw's friendly arm is.</para>
        ///
        /// <para><b>What this staging cannot supply is the thing RULING 1 makes her whole set turn
        /// on.</b> Six of her seven Attacks target at exactly the printed Range, and the two heroes
        /// stand fifteen spaces apart here, which is outside Clear Shot's Range 4 by eleven. No board
        /// is staged specially for her - this is the same one-versus-one board six earlier heroes
        /// use - but the consequence decides where her journeys live: every face whose subject is a
        /// target is driven from an engaged staging, and this one carries the faces that ask nothing of
        /// distance.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusSilverarrowOneVsOne() =>
            CreateOneVsOne(new HeroContentId("silverarrow"), "Silverarrow the Pathfinder");


        /// <summary>
        /// Milestone 8.35z50. Wasp on Red against Ursafar the Savage on Blue, the TENTH
        /// hero this build deals.
        ///
        /// <para>He needs no ally seat: none of his nineteen faces names a friendly hero at all. What
        /// his set asks for is his own state - whether he is enraged - and enemy minions in radius for
        /// his three removal Reds, both of which the one-vs-one board with p.8's waves supplies.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusUrsafarOneVsOne() =>
            CreateOneVsOne(new HeroContentId("ursafar"), "Ursafar the Savage");

        /// <summary>
        /// Milestone 8.35z53. Wasp on Red against Whisper the Outcast on Blue, on
        /// the same `DEV-005` terms the staged pairings before it are on.
        ///
        /// <para><b>What this staging supplies and what it structurally cannot, stated rather than
        /// assumed.</b> Whisper's kit reads the board in two places and the enemy's DISCARD PILE in
        /// five. The board half is ordinary here - p.8's waves leave empty minion Spawn points for her
        /// whole Red line to count and her three Green Places to reach, and put enemy units on Spawn
        /// points for her three Blue displacements. The discard half cannot be staged at all: p.6 deals
        /// every card to a hand or a deck, so at the first turn of round 1 no pile in the match holds
        /// anything, and Death Seeker, Blood Fury, Blood Frenzy and the printed-Range arm of both Swaps
        /// all read false. RULING 42 accepts that as printed rather than as a defect - the card is
        /// greyed with rule 14's ordinary refusal and becomes playable the moment anybody has spent a
        /// card - so a journey that needs a discard SPENDS one rather than staging around it.</para>
        ///
        /// <para>The inverse is what makes her Gold basic reachable on turn one: Swift Justice's first
        /// bullet wants a hero with an EMPTY discard, and on the opening turn every hero has one.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusWhisperOneVsOne() =>
            CreateOneVsOne(new HeroContentId("whisper"), "Whisper the Outcast");
        /// <summary>
        /// Milestone 8.37. Wasp on Red against Tali the Spiritcaller on Blue, the twelfth
        /// hero this build deals.
        ///
        /// <para>She needs no ally seat for reachability - Commune with Spirits' mandatory naming
        /// already admits herself as a candidate friendly hero, exactly as Silverarrow's Blessing does
        /// under RULING 8 - but the ALLY-naming half of the full borrow flow needs the three-seat
        /// staging <see cref="Participants.MatchConfiguration.CreateStagedTaliWithAllyForInternalTest"/>
        /// builds instead, because this one-versus-one board has nobody else on her side to name
        /// it.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusTaliOneVsOne() =>
            CreateOneVsOne(new HeroContentId("tali"), "Tali the Spiritcaller");

        /// <summary>
        /// Milestone 8.37. Wasp on Red against Tali and Arien on Blue - the shape
        /// <see cref="CreateStagedWaspVersusArienAndBroganOneVsTwo"/> already proves, with the roles
        /// swapped: there the second Blue hero is what BROGAN's faces needed a candidate for, here it is
        /// what COMMUNE WITH SPIRITS' mandatory naming step selects, so a journey can drive an ALLY
        /// naming the colour rather than Tali naming her own.
        ///
        /// <para>Distance is irrelevant to the naming step - "a friendly hero in play" reads p.19's
        /// board membership, not p.9's Radius or Range - so the ally's spawn point is the same
        /// <c>blue-hero-02</c> Brogan's own ally arm uses, for no reason beyond it already being a
        /// declared Blue spawn on the official map.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusTaliWithAllyOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("tali"), "Tali the Spiritcaller");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("arien"),
                        "Arien",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        /// <summary>
        /// Milestone 8.21. Wasp on Red against Arien and Brogan on Blue, which is the smallest board
        /// on which "a friendly hero in range" is true of anybody - the clause War Drummer and Master
        /// Skald print and the reason two of Brogan's nineteen faces would otherwise be unreachable.
        ///
        /// <para>Brogan takes <c>blue-hero-02</c>, a Blue hero spawn the official map has declared
        /// since it was reviewed, at a space two away from the first - inside both cards' printed
        /// Range 4. Nothing about the map, the minions, the Wave counters or the Life counters differs,
        /// and the Red team still fields one hero: this is one extra seat, not the six-player
        /// configuration milestone 8.25 owns.</para>
        /// </summary>
        public static QuickGameSetupConfiguration CreateStagedWaspVersusArienAndBroganOneVsTwo()
        {
            var mapId = new MapId("official-single-lane");
            var ordinary = CreateOneVsOne(new HeroContentId("arien"), "Arien");
            return new QuickGameSetupConfiguration(
                ordinary.RequiredMap,
                ordinary.WaveCount,
                ordinary.LifeCountPerTeam,
                ordinary.Heroes.Concat(new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue),
                        new HeroContentId("brogan"),
                        "Brogan",
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-hero-02")),
                        new ConfiguredSpaceReference(mapId, new SpaceId("q-1_r10")))
                }),
                ordinary.Minions);
        }

        private static QuickGameSetupConfiguration CreateOneVsOne(
            HeroContentId blueContentId, string blueDisplayName)
        {
            var mapId = new MapId("official-single-lane");
            ConfiguredSpawnPointReference Spawn(string value) =>
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId(value));
            ConfiguredSpaceReference Space(string value) =>
                new ConfiguredSpaceReference(mapId, new SpaceId(value));
            return new QuickGameSetupConfiguration(
                new MapDefinitionReference(mapId, 1),
                3,
                4,
                new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.RedHeroIdValue),
                        new HeroContentId("wasp"),
                        "Wasp",
                        Spawn("red-hero-01"),
                        Space("q-2_r-5")),
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(StagedBoardParticipants.BlueHeroIdValue),
                        blueContentId,
                        blueDisplayName,
                        Spawn("blue-hero-01"),
                        Space("q-3_r10"))
                },
                CreateOfficialMinionPlacements(mapId));
        }

        /// <summary>
        /// Rulebook p.6 setup step 10's minions: one Ranged, four Melee and one Heavy per team, each on
        /// the starting Battle Zone's spawn point of its own team colour and type.
        ///
        /// <para>Milestone 8.25 lifted these twelve out of the staged factory rather than copying them
        /// into the six-player one. Setup step 10 says nothing about the player count, so the two
        /// configurations place the same minions, and two lists that must agree forever are one list.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z36 published it as <see cref="OfficialMinionPlacements"/> so a
        /// board composed from a SELECTED line-up places the same twelve minions as the printed one
        /// - the two lists that must agree forever staying one list, one caller further out.
        /// </remarks>
        public static IEnumerable<QuickGameMinionPlacementDefinition> OfficialMinionPlacements(
            MapId mapId) => CreateOfficialMinionPlacements(mapId);

        private static IEnumerable<QuickGameMinionPlacementDefinition> CreateOfficialMinionPlacements(
            MapId mapId)
        {
            QuickGameMinionPlacementDefinition Minion(
                string id, TeamSide side, StandardMinionType type, string spawn, string space) =>
                new QuickGameMinionPlacementDefinition(
                    new MinionId(id),
                    side,
                    type,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId(spawn)),
                    new ConfiguredSpaceReference(mapId, new SpaceId(space)));

            return new[]
            {
                Minion("red-melee-01", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-04", "q-1_r-1"),
                Minion("red-melee-02", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-05", "q-1_r0"),
                Minion("red-melee-03", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-08", "q-8_r4"),
                Minion("red-melee-04", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-09", "q-4_r4"),
                Minion("red-ranged-01", TeamSide.Red, StandardMinionType.Ranged, "red-ranged-minion-02", "q-4_r2"),
                Minion("red-heavy-01", TeamSide.Red, StandardMinionType.Heavy, "red-heavy-minion-03", "q-7_r6"),
                Minion("blue-melee-01", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-03", "q-3_r2"),
                Minion("blue-melee-02", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-04", "q1_r2"),
                Minion("blue-melee-03", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-07", "q-6_r6"),
                Minion("blue-melee-04", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-08", "q-6_r7"),
                Minion("blue-ranged-01", TeamSide.Blue, StandardMinionType.Ranged, "blue-ranged-minion-02", "q-3_r4"),
                Minion("blue-heavy-01", TeamSide.Blue, StandardMinionType.Heavy, "blue-heavy-minion-01", "q0_r0")
            };
        }
    }

    public sealed class QuickGameTeamSetupState
    {
        public QuickGameTeamSetupState(TeamId teamId, TeamSide teamSide, int lifeCount, ZoneId throneZoneId)
        {
            TeamId = teamId;
            TeamSide = teamSide;
            LifeCount = lifeCount;
            ThroneZoneId = throneZoneId;
        }

        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public int LifeCount { get; }
        public ZoneId ThroneZoneId { get; }
    }

    public sealed class QuickGameHeroSetupState
    {
        public QuickGameHeroSetupState(
            HeroId heroId, HeroContentId contentId, string displayName, TeamId teamId, TeamSide teamSide,
            ConfiguredSpawnPointReference spawnPoint, ConfiguredSpaceReference occupiedSpace, HexCoordinate coordinate)
        {
            HeroId = heroId;
            ContentId = contentId;
            DisplayName = displayName;
            TeamId = teamId;
            TeamSide = teamSide;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
            Coordinate = coordinate;
        }

        public HeroId HeroId { get; }
        public HeroContentId ContentId { get; }
        public string DisplayName { get; }
        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
        public HexCoordinate Coordinate { get; }
    }

    public sealed class QuickGameMinionSetupState
    {
        public QuickGameMinionSetupState(
            MinionId minionId, TeamId teamId, TeamSide teamSide, StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint, ConfiguredSpaceReference occupiedSpace, HexCoordinate coordinate)
        {
            MinionId = minionId;
            TeamId = teamId;
            TeamSide = teamSide;
            MinionType = minionType;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
            Coordinate = coordinate;
        }

        public MinionId MinionId { get; }
        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
        public HexCoordinate Coordinate { get; }
    }

    public sealed class QuickGameSetupState
    {
        public QuickGameSetupState(
            MapDefinitionReference selectedMap,
            int waveCount,
            TieBreakerState tieBreakerState,
            IEnumerable<QuickGameTeamSetupState> teams,
            IEnumerable<QuickGameHeroSetupState> heroes,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            SelectedMap = selectedMap;
            WaveCount = waveCount;
            TieBreakerState = tieBreakerState;
            Teams = Array.AsReadOnly(teams.ToArray());
            Heroes = Array.AsReadOnly(heroes.ToArray());
            Minions = Array.AsReadOnly(minions.ToArray());
        }

        public MapDefinitionReference SelectedMap { get; }
        public int WaveCount { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<QuickGameTeamSetupState> Teams { get; }
        public IReadOnlyList<QuickGameHeroSetupState> Heroes { get; }
        public IReadOnlyList<QuickGameMinionSetupState> Minions { get; }
    }

    public enum QuickGameSetupRejectionReason
    {
        None = 0,
        MissingSelectedMap = 1,
        MapIdentityMismatch = 2,
        ContentRevisionMismatch = 3,
        CrossMapReference = 4,
        MissingSpace = 5,
        MissingSpawnPoint = 6,
        SpawnSpaceMismatch = 7,
        SpawnTeamMismatch = 8,
        SpawnUnitTypeMismatch = 9,
        InvalidHeroThrone = 10,
        InvalidMinionZone = 11,
        OccupancyConflict = 12,
        ParticipantMismatch = 13
    }

    public sealed class QuickGameSetupException : Exception
    {
        public QuickGameSetupException(QuickGameSetupRejectionReason reason, string message) : base(message)
        {
            Reason = reason;
        }

        public QuickGameSetupRejectionReason Reason { get; }
    }

    public static class QuickGameSetupAuthority
    {
        public static QuickGameSetupState Create(
            QuickGameSetupConfiguration configuration,
            MatchParticipants participants,
            BoardMapDefinition selectedMap,
            TieBreakerState tieBreakerState)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (selectedMap == null)
                throw Reject(QuickGameSetupRejectionReason.MissingSelectedMap, "Quick Game setup requires the selected map definition.");
            if (!tieBreakerState.IsDefined) throw new ArgumentException("Quick Game setup requires the initial Tie Breaker result.", nameof(tieBreakerState));
            if (participants.Teams.Count != 2 ||
                participants.Teams.Count(value => value.Side == TeamSide.Red) != 1 ||
                participants.Teams.Count(value => value.Side == TeamSide.Blue) != 1)
                throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch,
                    "Quick Game setup requires exactly one Red team and one Blue team.");
            if (!configuration.RequiredMap.MapId.Equals(selectedMap.Id))
                throw Reject(QuickGameSetupRejectionReason.MapIdentityMismatch,
                    $"Setup requires map '{configuration.RequiredMap.MapId}', not '{selectedMap.Id}'.");
            if (configuration.RequiredMap.ContentRevision != selectedMap.ContentRevision)
                throw Reject(QuickGameSetupRejectionReason.ContentRevisionMismatch,
                    $"Setup requires map revision {configuration.RequiredMap.ContentRevision}, not {selectedMap.ContentRevision}.");

            var spaces = selectedMap.Spaces.ToDictionary(value => value.Id);
            var spawns = selectedMap.SpawnPoints.ToDictionary(value => value.Id);
            var occupiedSpaceIds = new HashSet<SpaceId>();
            var occupiedSpawnIds = new HashSet<SpawnPointId>();
            var heroStates = new List<QuickGameHeroSetupState>();
            var minionStates = new List<QuickGameMinionSetupState>();

            foreach (var definition in configuration.Heroes)
            {
                Hero participantHero;
                Team team;
                try
                {
                    participantHero = participants.GetHero(definition.HeroId);
                    team = participants.GetTeamForHero(participantHero.Id);
                }
                catch (KeyNotFoundException exception)
                {
                    throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch, exception.Message);
                }

                var resolved = ResolvePlacement(
                    selectedMap, definition.SpawnPoint, definition.OccupiedSpace, spaces, spawns, occupiedSpaceIds, occupiedSpawnIds);
                var expectedTeam = GuardsDigital.Core.Rules.Modifications.ThroneZoneQuery.GetMapTeamId(team.Side);
                if (!string.Equals(resolved.Spawn.TeamId, expectedTeam, StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnTeamMismatch, $"Hero '{definition.HeroId}' uses the wrong team spawn point.");
                if (!string.Equals(resolved.Spawn.UnitType, "hero", StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnUnitTypeMismatch, $"Hero '{definition.HeroId}' requires a hero spawn point.");
                var zone = selectedMap.GetZoneForSpace(resolved.Space.Id);
                if (!zone.IsThrone || !string.Equals(zone.TeamId, expectedTeam, StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.InvalidHeroThrone, $"Hero '{definition.HeroId}' must start in its own Throne zone.");

                heroStates.Add(new QuickGameHeroSetupState(
                    definition.HeroId, definition.ContentId, definition.DisplayName, team.Id, team.Side,
                    definition.SpawnPoint, definition.OccupiedSpace, resolved.Space.Coordinate));
            }

            if (heroStates.Count != participants.Heroes.Count ||
                participants.Heroes.Any(hero => heroStates.All(state => !state.HeroId.Equals(hero.Id))))
                throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch, "Every configured participant hero requires exactly one setup placement.");

            foreach (var definition in configuration.Minions)
            {
                var team = participants.Teams.Single(value => value.Side == definition.TeamSide);
                var resolved = ResolvePlacement(
                    selectedMap, definition.SpawnPoint, definition.OccupiedSpace, spaces, spawns, occupiedSpaceIds, occupiedSpawnIds);
                if (!string.Equals(resolved.Spawn.TeamId, GuardsDigital.Core.Rules.Modifications.ThroneZoneQuery.GetMapTeamId(definition.TeamSide), StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnTeamMismatch, $"Minion '{definition.MinionId}' uses the wrong team spawn point.");
                if (!string.Equals(resolved.Spawn.UnitType, GetMapUnitType(definition.MinionType), StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnUnitTypeMismatch, $"Minion '{definition.MinionId}' uses the wrong minion-type spawn point.");
                if (!GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                        selectedMap, resolved.Space.Id))
                    throw Reject(QuickGameSetupRejectionReason.InvalidMinionZone, $"Minion '{definition.MinionId}' must start in the starting Battle Zone.");

                minionStates.Add(new QuickGameMinionSetupState(
                    definition.MinionId, team.Id, team.Side, definition.MinionType,
                    definition.SpawnPoint, definition.OccupiedSpace, resolved.Space.Coordinate));
            }

            var teamStates = participants.Teams
                .OrderBy(value => value.Side)
                .Select(team =>
                {
                    if (!GuardsDigital.Core.Rules.Modifications.ThroneZoneQuery.TryGetThroneZone(
                            selectedMap, team.Side, out var throne))
                        throw Reject(QuickGameSetupRejectionReason.InvalidHeroThrone,
                            $"Map '{selectedMap.Reference}' has no {team.Side} Throne zone.");
                    return new QuickGameTeamSetupState(team.Id, team.Side, configuration.LifeCountPerTeam, throne.Id);
                })
                .ToArray();

            return new QuickGameSetupState(
                selectedMap.Reference,
                configuration.WaveCount,
                tieBreakerState,
                teamStates,
                heroStates,
                minionStates);
        }

        private static (BoardMapSpawnPoint Spawn, BoardMapSpace Space) ResolvePlacement(
            BoardMapDefinition selectedMap,
            ConfiguredSpawnPointReference spawnReference,
            ConfiguredSpaceReference spaceReference,
            IReadOnlyDictionary<SpaceId, BoardMapSpace> spaces,
            IReadOnlyDictionary<SpawnPointId, BoardMapSpawnPoint> spawns,
            ISet<SpaceId> occupiedSpaceIds,
            ISet<SpawnPointId> occupiedSpawnIds)
        {
            if (!spawnReference.MapId.Equals(selectedMap.Id) || !spaceReference.MapId.Equals(selectedMap.Id))
                throw Reject(QuickGameSetupRejectionReason.CrossMapReference, "Setup child references must belong to the selected map.");
            if (!spawns.TryGetValue(spawnReference.SpawnPointId, out var spawn))
                throw Reject(QuickGameSetupRejectionReason.MissingSpawnPoint, $"Spawn point '{spawnReference}' is unavailable.");
            if (!spaces.TryGetValue(spaceReference.SpaceId, out var space))
                throw Reject(QuickGameSetupRejectionReason.MissingSpace, $"Space '{spaceReference}' is unavailable.");
            if (!spawn.SpaceId.Equals(space.Id))
                throw Reject(QuickGameSetupRejectionReason.SpawnSpaceMismatch, $"Spawn point '{spawnReference}' does not occupy configured space '{spaceReference}'.");
            if (!occupiedSpawnIds.Add(spawn.Id) || !occupiedSpaceIds.Add(space.Id))
                throw Reject(QuickGameSetupRejectionReason.OccupancyConflict, $"Setup placement conflicts at '{space.Id}'.");
            return (spawn, space);
        }

        private static string GetMapUnitType(StandardMinionType type)
        {
            switch (type)
            {
                case StandardMinionType.Melee: return "melee-minion";
                case StandardMinionType.Ranged: return "ranged-minion";
                case StandardMinionType.Heavy: return "heavy-minion";
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static QuickGameSetupException Reject(QuickGameSetupRejectionReason reason, string message) =>
            new QuickGameSetupException(reason, message);
    }
}
