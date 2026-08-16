using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one authority-owned Battle-Zone membership question (milestone 6.4a). Minion Battle, the
    /// p.13 Heavy-immunity accompaniment, Lane-Push eligibility, setup validation, and the
    /// displaced-minion correction all consult this query rather than each deriving the flagged zone
    /// and testing membership inline, which is what let the same three lines drift into five places.
    ///
    /// Membership never throws for a terrain or off-board coordinate: a terrain space belongs to no
    /// zone by the map invariant and an unknown space is simply not in the zone, and the correction has
    /// to ask exactly those questions about wherever a displaced minion ended up.
    /// </summary>
    public static class BattleZoneQuery
    {
        /// <summary>The single zone flagged as the starting Battle Zone; throws when the map does not declare exactly one.</summary>
        public static BoardMapZone GetStartingBattleZone(BoardMapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            return map.Zones.Single(value => value.IsStartingBattleZone);
        }

        /// <summary>
        /// The probe form: whether the map declares exactly one starting Battle Zone, and which. The
        /// round authority's end-of-round composition asks this to tell a configured map from a
        /// synthetic one, so it must not throw on either.
        /// </summary>
        public static bool TryGetStartingBattleZone(BoardMapDefinition map, out BoardMapZone zone)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var flagged = map.Zones.Where(value => value.IsStartingBattleZone).ToArray();
            zone = flagged.Length == 1 ? flagged[0] : null;
            return flagged.Length == 1;
        }

        /// <summary>Whether the named space lies in the starting Battle Zone. False for terrain and unknown spaces.</summary>
        public static bool IsInStartingBattleZone(BoardMapDefinition map, SpaceId spaceId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var space = map.Spaces.FirstOrDefault(value => value.Id.Equals(spaceId));
            if (space == null || space.ZoneId == null)
            {
                return false;
            }

            return map.GetZone(space.ZoneId.Value).IsStartingBattleZone;
        }

        /// <summary>
        /// Whether the coordinate lies in the starting Battle Zone. False for terrain, which belongs to
        /// no zone, and for off-board coordinates, which have no space at all.
        /// </summary>
        public static bool IsInStartingBattleZone(BoardMapDefinition map, HexCoordinate coordinate)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var space = map.Spaces.FirstOrDefault(value => value.Coordinate.Equals(coordinate));
            if (space == null || space.ZoneId == null)
            {
                return false;
            }

            return map.GetZone(space.ZoneId.Value).IsStartingBattleZone;
        }

        /// <summary>
        /// The playable coordinates of the starting Battle Zone, in canonical axial order. The
        /// correction derives its route endpoints and Place destinations from this one set.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindStartingBattleZoneCoordinates(BoardMapDefinition map)
        {
            var zone = GetStartingBattleZone(map);
            return map.Spaces
                .Where(space => space.ZoneId != null && space.ZoneId.Value.Equals(zone.Id))
                .Select(space => space.Coordinate)
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();
        }

        /// <summary>
        /// Every minion standing in the starting Battle Zone, in canonical identity order. This is the
        /// intrinsic miniature set - <see cref="AdjacentMoveAuthority.Minions"/> holds miniatures only -
        /// so no effective-type question is asked or answered here.
        /// </summary>
        public static IReadOnlyList<MinionOccupancy> FindMinionsInside(AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return board.Minions
                .Where(minion => IsInStartingBattleZone(board.MapDefinition, minion.OccupiedSpace.SpaceId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every minion miniature standing outside the starting Battle Zone, in canonical identity
        /// order: p.18's correction subjects, read from board position at the boundary rather than
        /// from any record of what moved.
        /// </summary>
        public static IReadOnlyList<MinionOccupancy> FindMinionsOutside(AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return board.Minions
                .Where(minion => !IsInStartingBattleZone(board.MapDefinition, minion.OccupiedSpace.SpaceId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every unit that counts as a minion in the starting Battle Zone, with the side it belongs to
        /// and whether it is a Heavy miniature.
        ///
        /// This is the effective reading milestone 6.5aa settled for the Minion Battle: p.17 replaces
        /// an object's rules and qualities with those of the type it counts as, so a hero counting as a
        /// minion is one of the minions in the zone. It lives here rather than inside the end-of-round
        /// resolution because milestone 6.6 needs the same set for p.8's last-minion Lane-Push cause,
        /// and "the minions in the Battle Zone" appearing twice with two derivations is exactly the
        /// two-locally-correct-readings defect milestone 6.5ad was written to correct.
        ///
        /// Intrinsic miniatures come from <see cref="FindMinionsInside"/>, and heroes join only when
        /// the effective-type query says they count as minions; a counting hero is never Heavy, because
        /// Heavy is a property of the minion miniature the rules print, not of the type something
        /// counts as.
        /// </summary>
        public static IReadOnlyList<EffectiveBattleZoneMinion> FindEffectiveMinionsInside(
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            var miniatures = board.Minions
                .Where(minion => IsInStartingBattleZone(board.MapDefinition, minion.OccupiedSpace.SpaceId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .Select(value => new EffectiveBattleZoneMinion(
                    RuleObject.ForMinion(value.MinionId),
                    value.TeamSide,
                    value.MinionType == StandardMinionType.Heavy));
            var counting = board.Occupancies
                .Select(value => RuleObject.ForHero(value.HeroId))
                .Where(unit => EffectiveUnitKindQuery.Is(unit, EffectiveUnitKind.Minion, modifications))
                .Where(unit =>
                    board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(unit.HeroId), out var coordinate) &&
                    IsInStartingBattleZone(board.MapDefinition, coordinate))
                .Select(unit => new EffectiveBattleZoneMinion(
                    unit, participants.GetTeamForHero(unit.HeroId).Side, false));
            return Array.AsReadOnly(miniatures.Concat(counting).ToArray());
        }
    }

    /// <summary>One unit counting as a minion inside the starting Battle Zone, with its side.</summary>
    public readonly struct EffectiveBattleZoneMinion
    {
        public EffectiveBattleZoneMinion(RuleObject unit, TeamSide teamSide, bool isHeavy)
        {
            Unit = unit;
            TeamSide = teamSide;
            IsHeavy = isHeavy;
        }

        public RuleObject Unit { get; }
        public TeamSide TeamSide { get; }

        /// <summary>Whether this is a Heavy minion miniature, which p.8 removes last in a Minion Battle.</summary>
        public bool IsHeavy { get; }
    }
}
