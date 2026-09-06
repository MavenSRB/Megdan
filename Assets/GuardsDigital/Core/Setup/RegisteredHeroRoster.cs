using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Milestone 8.35z36. One hero this build can actually deal: the identity a rule reads,
    /// the printed set it deals, and the epithet setup step 7 names it by.
    /// </summary>
    /// <remarks>
    /// <para>A hero is "registered" here exactly when this build can put it on a board - which is
    /// what a hero-selection screen must offer and what it must never offer more of. The list is
    /// therefore DERIVED from the same printed line-up table the official setup composes from
    /// (<see cref="QuickGameSetupConfiguration.RegisteredHeroes"/>) rather than restated, so a dealing milestone
    /// that adds a hero to that table adds it to every selector in the build at the same moment and
    /// without touching this file. That is the standing requirement: nothing here counts the
    /// roster.</para>
    /// <para>What is deliberately NOT here: the hero profile card's Complexity rating. Rulebook
    /// p.22's selection methods filter by it ("of appropriate complexity", and "remove the higher
    /// difficulty heroes, based on the experience level of the least experienced player"), but the
    /// rating is printed on the Profile cards, which this build has never modelled. Recorded as an
    /// absence per the cards-are-the-definition discipline rather than guessed at.</para>
    /// </remarks>
    public readonly struct RegisteredHero : IEquatable<RegisteredHero>
    {
        public RegisteredHero(string heroIdValue, HeroContentId contentId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(heroIdValue))
            {
                throw new ArgumentException(
                    "A registered hero requires a hero ID.", nameof(heroIdValue));
            }

            if (!contentId.IsDefined)
            {
                throw new ArgumentException(
                    "A registered hero requires a content ID.", nameof(contentId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A registered hero requires a display name.", nameof(displayName));
            }

            HeroIdValue = heroIdValue;
            ContentId = contentId;
            DisplayName = displayName;
        }

        /// <summary>The stable hero identity every rule, projection and snapshot addresses.</summary>
        public string HeroIdValue { get; }

        /// <summary>The printed set this hero deals.</summary>
        public HeroContentId ContentId { get; }

        /// <summary>Setup step 7's own epithet, which is what a player reads on a pick screen.</summary>
        public string DisplayName { get; }

        public HeroId HeroId => new HeroId(HeroIdValue);

        /// <remarks>
        /// <b>Identity is the hero id and the printed set, never the epithet.</b> Comparing
        /// <see cref="DisplayName"/> here violated the Phase 4 boundary that rules and application code
        /// never branch on a display name or printed text, and the Edit Mode conformance scan caught it -
        /// the cross-platform harness structurally cannot, because that scan is Unity-only. It is also
        /// the right semantics rather than merely the legal ones: two rows naming the same hero and the
        /// same dealt set ARE the same registered hero, and an epithet is what a pick screen reads, not
        /// what a rule decides on. Dropping it also makes this agree with <see cref="GetHashCode"/>,
        /// which has only ever hashed the id.
        /// </remarks>
        public bool Equals(RegisteredHero other) =>
            string.Equals(HeroIdValue, other.HeroIdValue, StringComparison.Ordinal) &&
            ContentId.Equals(other.ContentId);

        public override bool Equals(object obj) => obj is RegisteredHero other && Equals(other);

        public override int GetHashCode() =>
            HeroIdValue == null ? 0 : StringComparer.Ordinal.GetHashCode(HeroIdValue);

        public override string ToString() => HeroIdValue;
    }

    /// <summary>
    /// Milestone 8.35z36. One marked hero spawn point of one Throne zone, and the space it
    /// stands on.
    /// </summary>
    /// <remarks>
    /// <para><b>A slot is map geometry, not a hero fact, and that separation is the whole point of
    /// this type.</b> Before a selector existed the two were one table: each hero row carried its
    /// own spawn, because the printed line-up put exactly six heroes on exactly six marked points.
    /// A selectable roster breaks that - twelve registered heroes still play on three marked points
    /// a side - so which point a hero stands on becomes a function of WHICH SEAT OF ITS TEAM the
    /// hero was selected into, and never of which hero it is.</para>
    /// <para>p.6 setup step 9 says only to place the miniatures on the marked spaces of their own
    /// Throne Zone and does not say which hero takes which point. Milestone 8.35z16 already answered
    /// the identical question for a short team by stating it once - each hero keeps the point it
    /// already stands on - and this states the same answer one step more generally: the Nth hero of
    /// a team takes the Nth marked point of that team's zone, in the order the printed line-up
    /// fills them. With the printed line-up selected in printed order that reproduces the shipped
    /// board exactly, which is asserted rather than asserted-about.</para>
    /// </remarks>
    public readonly struct HeroSpawnSlot
    {
        public HeroSpawnSlot(TeamSide teamSide, string spawnPointId, string spaceId)
        {
            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teamSide), teamSide, "A hero spawn slot belongs to Red or Blue.");
            }

            if (string.IsNullOrWhiteSpace(spawnPointId))
            {
                throw new ArgumentException("A slot requires a spawn point.", nameof(spawnPointId));
            }

            if (string.IsNullOrWhiteSpace(spaceId))
            {
                throw new ArgumentException("A slot requires a space.", nameof(spaceId));
            }

            TeamSide = teamSide;
            SpawnPointId = spawnPointId;
            SpaceId = spaceId;
        }

        public TeamSide TeamSide { get; }
        public string SpawnPointId { get; }
        public string SpaceId { get; }

        public override string ToString() => $"{TeamSide}:{SpawnPointId}";
    }

    /// <summary>
    /// Milestone 8.35z36. One team's selected heroes, in the order they fill that team's
    /// marked hero spawn points; and both teams together as the line-up a match composes from.
    /// </summary>
    /// <remarks>
    /// <para>This is the whole output of hero selection as far as the rules are concerned, and it
    /// deliberately names no seat, no connection and no player. Who picked a hero is session
    /// vocabulary and lives in the Application layer with the rest of it (milestone 8.35a's law);
    /// what the board is made of is this, and a fixture, a sandbox or a launch flag can state one
    /// without a lobby ever existing.</para>
    /// <para><b>One copy of each hero exists.</b> The constructor refuses a hero that appears twice
    /// across BOTH teams, not merely twice on one. That is the printed rule and not a house rule:
    /// p.22's All Pick has players pick "out of all available heroes" from one shuffled Profile
    /// deck holding one card per hero, so a hero another player has taken is not available to
    /// anyone. Refusing it here means no path in the build - selector, bot filler, launch flag or
    /// fixture - can compose a board with one hero on both teams.</para>
    /// </remarks>
    public sealed class HeroLineup
    {
        private readonly IReadOnlyList<RegisteredHero> red;
        private readonly IReadOnlyList<RegisteredHero> blue;

        public HeroLineup(IEnumerable<RegisteredHero> redTeam, IEnumerable<RegisteredHero> blueTeam)
        {
            var redArray = redTeam?.ToArray() ??
                throw new ArgumentNullException(nameof(redTeam));
            var blueArray = blueTeam?.ToArray() ??
                throw new ArgumentNullException(nameof(blueTeam));

            if (redArray.Length == 0 || blueArray.Length == 0)
            {
                throw new ArgumentException("A line-up needs heroes on both teams.");
            }

            if (redArray.Length != blueArray.Length)
            {
                throw new ArgumentException(
                    "Setup step 2 splits the players into two EQUAL teams, so a line-up's two " +
                    "sides are the same length.");
            }

            var duplicate = redArray
                .Concat(blueArray)
                .GroupBy(hero => hero.HeroIdValue, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException(
                    $"One copy of each hero exists, and '{duplicate.Key}' is in this line-up " +
                    "more than once. A hero cannot be dealt to two seats, or to both teams.");
            }

            red = Array.AsReadOnly(redArray);
            blue = Array.AsReadOnly(blueArray);
        }

        public IReadOnlyList<RegisteredHero> Red => red;
        public IReadOnlyList<RegisteredHero> Blue => blue;

        /// <summary>Setup step 2's per-team count, which is also the format's HeroesPerTeam.</summary>
        public int HeroesPerTeam => red.Count;

        public IReadOnlyList<RegisteredHero> ForTeam(TeamSide side)
        {
            switch (side)
            {
                case TeamSide.Red: return red;
                case TeamSide.Blue: return blue;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side), side, "A line-up has a Red side and a Blue side.");
            }
        }

        /// <summary>
        /// Which side a hero was selected onto, or <see cref="TeamSide.None"/> when this line-up
        /// does not contain it.
        /// </summary>
        /// <remarks>
        /// This is the fact every team-coloured surface in the build ultimately reads, and it is a
        /// property of the LINE-UP rather than of the hero: ask the same hero on two line-ups and
        /// the answer differs, which is exactly what "team colour is a runtime fact" means.
        /// </remarks>
        public TeamSide SideOf(string heroIdValue)
        {
            if (string.IsNullOrWhiteSpace(heroIdValue)) return TeamSide.None;

            foreach (var hero in red)
            {
                if (string.Equals(hero.HeroIdValue, heroIdValue, StringComparison.Ordinal))
                {
                    return TeamSide.Red;
                }
            }

            foreach (var hero in blue)
            {
                if (string.Equals(hero.HeroIdValue, heroIdValue, StringComparison.Ordinal))
                {
                    return TeamSide.Blue;
                }
            }

            return TeamSide.None;
        }
    }

    /// <summary>
    /// Milestone 8.35z36. The heroes this build can deal, and the marked points they stand
    /// on - the two facts a hero-selection screen and its composition both read.
    /// </summary>
    public static class RegisteredHeroRoster
    {
        /// <summary>
        /// Every hero the PRINTED LINE-UP deals, in setup step 7's own order - which is what a lobby
        /// selection screen may offer.
        /// </summary>
        /// <remarks>
        /// <para>Derived, never counted: it is <see cref="QuickGameSetupConfiguration.RegisteredHeroes"/>, which is
        /// the printed line-up table itself. A dealing milestone that gives its hero a row there grows this
        /// list, the selector built on it, and the bot filler in the same commit.</para>
        /// <para><b>This is a subset of the heroes the build can put on a board, and saying so is the
        /// point.</b> Milestone 8.35z34 dealt Dodger the Warlock through a STAGED one-versus-one
        /// configuration rather than through the printed table, and deliberately changed no official
        /// line-up (8.25 owns that table). So a hero can be fully dealt, registered and gated while
        /// having no row here - and it must not be offered in a lobby, because a lobby composes the
        /// printed formats and those are seated from this table. Read this list for what a picker may
        /// offer; read the dealing configurations for what the build can compose. They are two
        /// questions and this answers only the first.</para>
        /// </remarks>
        public static IReadOnlyList<RegisteredHero> ForThisBuild => QuickGameSetupConfiguration.RegisteredHeroes;

        /// <summary>
        /// One team's marked hero spawn points, in the order the printed line-up fills them.
        /// </summary>
        public static IReadOnlyList<HeroSpawnSlot> SlotsFor(TeamSide side) =>
            QuickGameSetupConfiguration.HeroSpawnSlots
                .Where(slot => slot.TeamSide == side)
                .ToArray();

        /// <summary>
        /// The registered hero of that identity, or false when this build cannot deal it. The one
        /// gate a selection has to pass before anything composes from it.
        /// </summary>
        public static bool TryFind(string heroIdValue, out RegisteredHero hero)
        {
            foreach (var candidate in ForThisBuild)
            {
                if (string.Equals(candidate.HeroIdValue, heroIdValue, StringComparison.Ordinal))
                {
                    hero = candidate;
                    return true;
                }
            }

            hero = default;
            return false;
        }

        /// <summary>
        /// The line-up the shipped demo arrangement is: setup step 7's printed order, the first
        /// <paramref name="heroesPerTeam"/> of each side.
        /// </summary>
        /// <remarks>
        /// It exists so the selector's composition can be proven to REPRODUCE the shipped board
        /// rather than to resemble it - compose from this and the result must equal
        /// <see cref="QuickGameSetupConfiguration.CreateOfficial"/> exactly - and so every caller that does not
        /// select heroes at all (fixtures, the sandbox, a launch flag) has one honest default.
        /// </remarks>
        public static HeroLineup PrintedLineup(int heroesPerTeam)
        {
            if (heroesPerTeam <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heroesPerTeam), heroesPerTeam, "A team fields at least one hero.");
            }

            var rows = QuickGameSetupConfiguration.PrintedLineupSides;
            var red = rows
                .Where(row => row.Side == TeamSide.Red)
                .Take(heroesPerTeam)
                .Select(row => row.Hero)
                .ToArray();
            var blue = rows
                .Where(row => row.Side == TeamSide.Blue)
                .Take(heroesPerTeam)
                .Select(row => row.Hero)
                .ToArray();

            if (red.Length != heroesPerTeam || blue.Length != heroesPerTeam)
            {
                throw new InvalidOperationException(
                    "The printed line-up does not name enough heroes for that team size.");
            }

            return new HeroLineup(red, blue);
        }
    }
}
