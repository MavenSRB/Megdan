using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// What a <see cref="SandboxStagingDescription"/> composes: the setup a staged match begins
    /// from, and the starting Levels and balances that go with it. Milestone 8.35z17.
    /// </summary>
    public sealed class SandboxStagingResult
    {
        internal SandboxStagingResult(QuickGameSetupState setup, MatchStagedProgression progression)
        {
            Setup = setup;
            Progression = progression;
        }

        public QuickGameSetupState Setup { get; }
        public MatchStagedProgression Progression { get; }
    }

    /// <summary>
    /// Turns a staged situation into the ordinary composed setup a match begins from.
    /// Milestone 8.35z17.
    ///
    /// <para><b>This is staging, not a rules path, and the distinction is the whole design.</b>
    /// <see cref="EngagedMirrorStaging"/> is the prior art and states the terms this follows
    /// exactly: it "moves the setup's own heroes to different playable spaces before the match
    /// starts, which is a staging decision the rules never see", on `DEV-004` development-affordance
    /// terms. Everything below happens before any authority exists. No rule is consulted, no rule is
    /// changed, and the match that follows is composed by the same
    /// <c>MatchSessionInitialState</c> path every dealt match uses - which is why Undo, restart, the
    /// event log, snapshots and deterministic replay all keep working over a staged board with no
    /// code of their own.</para>
    ///
    /// <para><b>What it validates, and what it deliberately does not.</b> It refuses a description
    /// that names a piece the match does not have, puts two pieces on one space, or stands anything
    /// on a space the map does not call playable - because those produce a board no reading of the
    /// rules could make sense of. It does NOT re-impose rulebook p.6's setup restrictions: a staged
    /// hero may stand outside their Throne zone and a staged minion outside the starting Battle
    /// Zone, which is the entire point of a test environment and is exactly the freedom the existing
    /// staged-board fixtures already take. p.6 describes how a match is dealt, not which positions
    /// the rules can resolve.</para>
    /// </summary>
    public static class SandboxStagingAuthority
    {
        /// <summary>
        /// The staged setup, and the hands staged into <paramref name="configuration"/> in place.
        ///
        /// <para>The hand staging mutates the configuration's own card instances rather than
        /// building a parallel hand, because that is what carries: the scenario clones each instance
        /// with its <c>CardState</c>, so a card's location at composition time is what every
        /// rebuild, restart and retained-prefix Undo reproduces. It moves cards only through
        /// <see cref="CardLifecycleAuthority"/>'s own transitions, so no card reaches a state the
        /// lifecycle would not have allowed.</para>
        /// </summary>
        public static SandboxStagingResult Stage(
            SandboxStagingDescription description,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            MatchConfiguration configuration)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var playable = map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .ToDictionary(space => space.Coordinate, space => space);

            foreach (var staged in description.Heroes)
            {
                if (setup.Heroes.All(hero => !hero.HeroId.Equals(staged.HeroId)))
                {
                    throw Reject(
                        SandboxStagingRejectionReason.UnknownHero,
                        $"This match has no hero '{staged.HeroId.Value}' to stage.");
                }
            }

            var removed = new HashSet<string>(
                description.RemovedMinions.Select(minion => minion.Value), StringComparer.Ordinal);
            foreach (var minion in description.RemovedMinions.Concat(description.MovedMinions.Select(entry => entry.Key)))
            {
                if (setup.Minions.All(value => !value.MinionId.Equals(minion)))
                {
                    throw Reject(
                        SandboxStagingRejectionReason.UnknownMinion,
                        $"This match has no minion '{minion.Value}' to remove or move.");
                }
            }

            foreach (var spawn in description.SpawnedMinions)
            {
                if (setup.Minions.Any(value => value.MinionId.Equals(spawn.MinionId)))
                {
                    throw Reject(
                        SandboxStagingRejectionReason.DuplicateMinionId,
                        $"Minion '{spawn.MinionId.Value}' already stands in this match, so spawning " +
                        "one by that id would put two pieces behind one identity.");
                }
            }

            var heroes = StageHeroes(description, map, setup, playable);
            var minions = StageMinions(description, map, setup, playable, removed);
            AssertNoTwoPiecesShareASpace(heroes, minions);
            StageHands(description, configuration);

            var teams = description.TeamLifeCounts.Count == 0
                ? setup.Teams
                : setup.Teams.Select(team =>
                {
                    var staged = description.TeamLifeCounts
                        .Where(entry => entry.Key == team.TeamSide)
                        .Select(entry => (int?)entry.Value)
                        .LastOrDefault();
                    return staged.HasValue
                        ? new QuickGameTeamSetupState(
                            team.TeamId, team.TeamSide, staged.Value, team.ThroneZoneId)
                        : team;
                }).ToArray();

            return new SandboxStagingResult(
                new QuickGameSetupState(
                    setup.SelectedMap,
                    description.WaveCount ?? setup.WaveCount,
                    setup.TieBreakerState,
                    teams,
                    heroes,
                    minions),
                StageProgression(description, setup));
        }

        private static IReadOnlyList<QuickGameHeroSetupState> StageHeroes(
            SandboxStagingDescription description,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            IReadOnlyDictionary<HexCoordinate, BoardMapSpace> playable)
        {
            return setup.Heroes.Select(hero =>
            {
                var staged = description.Heroes
                    .FirstOrDefault(value => value.HeroId.Equals(hero.HeroId));
                if (staged?.Coordinate == null) return hero;
                var space = Space(playable, staged.Coordinate.Value, hero.HeroId.Value);
                return new QuickGameHeroSetupState(
                    hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                    hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id), space.Coordinate);
            }).ToArray();
        }

        private static IReadOnlyList<QuickGameMinionSetupState> StageMinions(
            SandboxStagingDescription description,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            IReadOnlyDictionary<HexCoordinate, BoardMapSpace> playable,
            IReadOnlyCollection<string> removed)
        {
            var kept = setup.Minions
                .Where(minion => !removed.Contains(minion.MinionId.Value))
                .Select(minion =>
                {
                    var moved = description.MovedMinions
                        .Where(entry => entry.Key.Equals(minion.MinionId))
                        .Select(entry => (HexCoordinate?)entry.Value)
                        .LastOrDefault();
                    if (!moved.HasValue) return minion;
                    var space = Space(playable, moved.Value, minion.MinionId.Value);
                    return new QuickGameMinionSetupState(
                        minion.MinionId, minion.TeamId, minion.TeamSide, minion.MinionType,
                        minion.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id),
                        space.Coordinate);
                })
                .ToList();

            // A spawned minion needs a spawn point, because p.8's Lane Push returns removed minions
            // to one. It is taken from the map's own unused spawn points of the same team and unit
            // type - found by reading the archetype's spawn point rather than by restating the
            // minion-type-to-map-unit-type mapping that `QuickGameSetupAuthority` owns, so the two
            // can never disagree. A side and type this match places none of is refused by name
            // rather than given an invented spawn point.
            var taken = new HashSet<SpawnPointId>(kept.Select(minion => minion.SpawnPoint.SpawnPointId));
            foreach (var spawn in description.SpawnedMinions)
            {
                var archetype = setup.Minions.FirstOrDefault(minion =>
                    minion.TeamSide == spawn.TeamSide && minion.MinionType == spawn.MinionType);
                if (archetype == null)
                {
                    throw Reject(
                        SandboxStagingRejectionReason.MalformedDescription,
                        $"This match places no {spawn.TeamSide} {spawn.MinionType} minion, so there " +
                        $"is no spawn point of that kind to give '{spawn.MinionId.Value}'.");
                }

                var pattern = map.SpawnPoints.Single(point =>
                    point.Id.Equals(archetype.SpawnPoint.SpawnPointId));
                var free = map.SpawnPoints.FirstOrDefault(point =>
                    string.Equals(point.TeamId, pattern.TeamId, StringComparison.Ordinal) &&
                    string.Equals(point.UnitType, pattern.UnitType, StringComparison.Ordinal) &&
                    !taken.Contains(point.Id));
                if (free == null)
                {
                    throw Reject(
                        SandboxStagingRejectionReason.MalformedDescription,
                        $"Every {spawn.TeamSide} {spawn.MinionType} spawn point on this map is " +
                        $"already claimed, so '{spawn.MinionId.Value}' cannot be given one of its own.");
                }

                taken.Add(free.Id);
                var space = Space(playable, spawn.Coordinate, spawn.MinionId.Value);
                kept.Add(new QuickGameMinionSetupState(
                    spawn.MinionId, archetype.TeamId, spawn.TeamSide, spawn.MinionType,
                    new ConfiguredSpawnPointReference(map.Id, free.Id),
                    new ConfiguredSpaceReference(map.Id, space.Id), space.Coordinate));
            }

            return kept;
        }

        /// <summary>
        /// Leaves each staged hero holding exactly the cards their description names, by discarding
        /// what they hold and taking the named ones out of their deck. Both moves are
        /// <see cref="CardLifecycleAuthority"/> transitions, so nothing here invents card state.
        /// </summary>
        private static void StageHands(
            SandboxStagingDescription description, MatchConfiguration configuration)
        {
            foreach (var staged in description.Heroes.Where(hero => hero.Hand != null))
            {
                // Milestone 9.30c. The cards this hero was DEALT: a spell in their spellbook is owned
                // and never dealt, so naming one is the not-dealt refusal below, not an unknown card.
                var owned = configuration.CardInstances
                    .Where(card => card.OwnerId.Equals(staged.HeroId) &&
                        !SpellbookQuery.IsInASpellbook(card.State.Location))
                    .ToArray();
                foreach (var wanted in staged.Hand)
                {
                    if (owned.All(card => !card.DefinitionReference.DefinitionId.Equals(wanted)))
                    {
                        throw Reject(
                            SandboxStagingRejectionReason.CardNotOwnedByHero,
                            $"'{staged.HeroId.Value}' was not dealt '{wanted.Value}', so it cannot " +
                            "be put in their hand. A staged hand chooses among the cards this hero " +
                            "brought, and does not deal them somebody else's.");
                    }
                }

                foreach (var card in owned.Where(card => card.State.Location == CardLocation.Hand))
                {
                    if (staged.Hand.Any(wanted => card.DefinitionReference.DefinitionId.Equals(wanted)))
                    {
                        continue;
                    }

                    CardLifecycleAuthority.TryDiscardFromHand(card);
                }

                foreach (var wanted in staged.Hand)
                {
                    if (owned.Any(card =>
                        card.DefinitionReference.DefinitionId.Equals(wanted) &&
                        card.State.Location == CardLocation.Hand))
                    {
                        continue;
                    }

                    var fromDeck = owned.FirstOrDefault(card =>
                        card.DefinitionReference.DefinitionId.Equals(wanted) &&
                        card.State.Location == CardLocation.Deck);
                    if (fromDeck == null || !CardLifecycleAuthority.TryTakeFromDeckIntoHand(fromDeck))
                    {
                        throw Reject(
                            SandboxStagingRejectionReason.UnknownCard,
                            $"'{wanted.Value}' is neither in '{staged.HeroId.Value}'s hand nor " +
                            "available in their deck, so the staged hand cannot be composed.");
                    }
                }
            }
        }

        private static MatchStagedProgression StageProgression(
            SandboxStagingDescription description, QuickGameSetupState setup)
        {
            if (description.Heroes.All(hero => !hero.Level.HasValue && !hero.Coins.HasValue))
            {
                return MatchStagedProgression.None;
            }

            // Both halves must cover every hero exactly once, so an unstaged hero is stated at the
            // value a dealt match gives them rather than omitted - which is what the two authorities'
            // own Restore entry points require, and they are left to say so.
            var levels = new List<KeyValuePair<HeroId, HeroLevel>>();
            var coins = new List<KeyValuePair<HeroId, int>>();
            foreach (var hero in setup.Heroes)
            {
                var staged = description.Heroes.FirstOrDefault(value => value.HeroId.Equals(hero.HeroId));
                var level = staged?.Level ?? HeroLevel.Minimum;
                if (level < HeroLevel.Minimum || level > HeroLevel.Maximum)
                {
                    throw Reject(
                        SandboxStagingRejectionReason.IllegalLevel,
                        $"'{hero.HeroId.Value}' cannot be staged at Level {level}: rulebook p.15's " +
                        $"table runs {HeroLevel.Minimum} to {HeroLevel.Maximum}.");
                }

                var purse = staged?.Coins ?? 0;
                if (purse < 0)
                {
                    throw Reject(
                        SandboxStagingRejectionReason.IllegalCoinBalance,
                        $"'{hero.HeroId.Value}' cannot be staged holding {purse} coins.");
                }

                levels.Add(new KeyValuePair<HeroId, HeroLevel>(hero.HeroId, new HeroLevel(level)));
                coins.Add(new KeyValuePair<HeroId, int>(hero.HeroId, purse));
            }

            return new MatchStagedProgression(levels, coins);
        }

        private static BoardMapSpace Space(
            IReadOnlyDictionary<HexCoordinate, BoardMapSpace> playable,
            HexCoordinate coordinate,
            string pieceId)
        {
            if (!playable.TryGetValue(coordinate, out var space))
            {
                throw Reject(
                    SandboxStagingRejectionReason.UnplayableSpace,
                    $"'{pieceId}' cannot stand on ({coordinate.Q}, {coordinate.R}): this map has no " +
                    "playable space there.");
            }

            return space;
        }

        private static void AssertNoTwoPiecesShareASpace(
            IEnumerable<QuickGameHeroSetupState> heroes,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            var occupied = heroes
                .Select(hero => new KeyValuePair<HexCoordinate, string>(hero.Coordinate, hero.HeroId.Value))
                .Concat(minions.Select(minion =>
                    new KeyValuePair<HexCoordinate, string>(minion.Coordinate, minion.MinionId.Value)))
                .ToArray();
            var clash = occupied
                .GroupBy(entry => entry.Key)
                .FirstOrDefault(group => group.Count() > 1);
            if (clash == null) return;
            throw Reject(
                SandboxStagingRejectionReason.OccupancyConflict,
                $"({clash.Key.Q}, {clash.Key.R}) would hold " +
                string.Join(" and ", clash.Select(entry => "'" + entry.Value + "'")) +
                ", and one space holds one piece.");
        }

        private static SandboxStagingException Reject(
            SandboxStagingRejectionReason reason, string message) =>
            new SandboxStagingException(reason, message);
    }
}
