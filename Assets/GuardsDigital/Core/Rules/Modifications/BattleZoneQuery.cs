using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one authority-owned Battle-Zone membership question (milestone 6.4a). Minion Battle, the
    /// p.13 Heavy-immunity accompaniment, Lane-Push eligibility, and the displaced-minion correction
    /// all consult this query rather than each deriving the zone and testing membership inline, which
    /// is what let the same three lines drift into five places.
    ///
    /// Since milestone 6.10 the zone those four questions are asked about is the board's authoritative
    /// <see cref="Components.IBoardDisplacementAuthority.CurrentBattleZoneId"/> rather than the map's
    /// starting-Battle-Zone flag. p.8 moves the Battle Zone one Zone along the Lane at every push, so a
    /// query that kept reading the flag would answer every one of those four questions about the zone
    /// the match started in. The flag survives here in exactly one form, and it is asking a different
    /// question: Quick Game setup validates that minions are *placed* in the starting Battle Zone,
    /// which is a statement about setup rather than about the match, and the board derives its opening
    /// current zone from the same flag when it is built.
    ///
    /// Membership never throws for a terrain or off-board coordinate: a terrain space belongs to no
    /// zone by the map invariant and an unknown space is simply not in the zone, and the correction has
    /// to ask exactly those questions about wherever a displaced minion ended up.
    /// </summary>
    public static class BattleZoneQuery
    {
        /// <summary>
        /// Whether the map declares exactly one starting Battle Zone, and which.
        ///
        /// Setup-time only, and inside the engine there are exactly two callers by design: the board's
        /// construction, which derives the opening value of the current Battle Zone from it, and Quick
        /// Game setup validation, which requires configured minions to be placed there. Every rules
        /// question asks <see cref="TryGetCurrentBattleZone"/> instead, and a conformance rule holds the
        /// three flag-reading forms to those two callers, because a rules question that reached one of
        /// them would answer about the Zone the match began in for the rest of the match. It must not
        /// throw on either a configured or a synthetic map, because the round authority's end-of-round
        /// composition asks it to tell the two apart.
        /// </summary>
        public static bool TryGetStartingBattleZone(BoardMapDefinition map, out BoardMapZone zone)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var flagged = map.Zones.Where(value => value.IsStartingBattleZone).ToArray();
            zone = flagged.Length == 1 ? flagged[0] : null;
            return flagged.Length == 1;
        }

        /// <summary>
        /// Whether the named space lies in the map's starting Battle Zone. Setup-time only, for the
        /// reason <see cref="TryGetStartingBattleZone"/> states; a rules question asks
        /// <see cref="IsInCurrentBattleZone(Components.IBoardDisplacementAuthority, SpaceId)"/>.
        /// </summary>
        public static bool IsInStartingBattleZone(BoardMapDefinition map, SpaceId spaceId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            return TryGetStartingBattleZone(map, out var zone) && IsInZone(map, zone.Id, spaceId);
        }

        /// <summary>
        /// Whether the coordinate lies in the map's starting Battle Zone. Setup-time only, and it
        /// exists beside the space form because scenario construction picks places by coordinate before
        /// any board exists to ask.
        /// </summary>
        public static bool IsInStartingBattleZone(BoardMapDefinition map, HexCoordinate coordinate)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (!TryGetStartingBattleZone(map, out var zone))
            {
                return false;
            }

            var space = map.Spaces.FirstOrDefault(value => value.Coordinate.Equals(coordinate));
            return space != null && space.ZoneId != null && space.ZoneId.Value.Equals(zone.Id);
        }

        /// <summary>
        /// The playable coordinates of the map's starting Battle Zone, in canonical axial order.
        /// Setup-time only, for the same reason as the two membership forms above.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindStartingBattleZoneCoordinates(BoardMapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (!TryGetStartingBattleZone(map, out var zone))
            {
                return Array.Empty<HexCoordinate>();
            }

            return map.Spaces
                .Where(space => space.ZoneId != null && space.ZoneId.Value.Equals(zone.Id))
                .Select(space => space.Coordinate)
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();
        }

        /// <summary>
        /// Whether this map could ever have its Battle Zone at <paramref name="zoneId"/> (milestone
        /// 6.10). One statement of the answer, consulted by the board's restore and by the snapshot
        /// reader alike, so a payload and a direct restore cannot disagree about what a legal Battle
        /// Zone is.
        ///
        /// p.8 moves the Battle Zone along the Lane and nowhere else, so on a map that declares an
        /// ordered Lane the answer is exactly that Lane's zones - a Battle Zone in a jungle is not a
        /// position the rules can reach. On a map that declares no Lane, nothing can ever move the zone
        /// at all, so the only legal answer is the one the map flags.
        /// </summary>
        public static bool CanBeCurrentBattleZone(BoardMapDefinition map, ZoneId zoneId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (!map.Zones.Any(zone => zone.Id.Equals(zoneId)))
            {
                return false;
            }

            return map.OrderedLaneZoneIds.Count > 0
                ? map.OrderedLaneZoneIds.Contains(zoneId)
                : TryGetStartingBattleZone(map, out var opening) && opening.Id.Equals(zoneId);
        }

        /// <summary>
        /// The zone the match is currently fighting over, and whether there is one at all. Absent on
        /// every synthetic board, whose map declares no starting Battle Zone for the board to open at.
        /// </summary>
        public static bool TryGetCurrentBattleZone(IBoardDisplacementAuthority board, out BoardMapZone zone)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            zone = board.CurrentBattleZoneId.HasValue
                ? board.MapDefinition.GetZone(board.CurrentBattleZoneId.Value)
                : null;
            return zone != null;
        }

        /// <summary>The current Battle Zone; throws when the board has none.</summary>
        public static BoardMapZone GetCurrentBattleZone(IBoardDisplacementAuthority board)
        {
            if (!TryGetCurrentBattleZone(board, out var zone))
            {
                throw new InvalidOperationException("This board declares no current Battle Zone.");
            }

            return zone;
        }

        /// <summary>Whether the named space lies in the current Battle Zone. False for terrain and unknown spaces.</summary>
        public static bool IsInCurrentBattleZone(IBoardDisplacementAuthority board, SpaceId spaceId)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return board.CurrentBattleZoneId.HasValue &&
                IsInZone(board.MapDefinition, board.CurrentBattleZoneId.Value, spaceId);
        }

        /// <summary>
        /// Whether the coordinate lies in the current Battle Zone. False for terrain, which belongs to
        /// no zone, and for off-board coordinates, which have no space at all.
        /// </summary>
        public static bool IsInCurrentBattleZone(IBoardDisplacementAuthority board, HexCoordinate coordinate)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.CurrentBattleZoneId.HasValue)
            {
                return false;
            }

            var space = board.MapDefinition.Spaces.FirstOrDefault(value => value.Coordinate.Equals(coordinate));
            return space != null && space.ZoneId != null &&
                space.ZoneId.Value.Equals(board.CurrentBattleZoneId.Value);
        }

        /// <summary>
        /// The playable coordinates of the current Battle Zone, in canonical axial order. The
        /// correction derives its route endpoints and Place destinations from this one set.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindCurrentBattleZoneCoordinates(
            IBoardDisplacementAuthority board)
        {
            var zone = GetCurrentBattleZone(board);
            return board.MapDefinition.Spaces
                .Where(space => space.ZoneId != null && space.ZoneId.Value.Equals(zone.Id))
                .Select(space => space.Coordinate)
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();
        }

        /// <summary>
        /// Every minion standing in the current Battle Zone, in canonical identity order. This is the
        /// intrinsic miniature set - the board holds miniatures only - so no effective-type question is
        /// asked or answered here.
        /// </summary>
        public static IReadOnlyList<MinionOccupancy> FindMinionsInside(IBoardDisplacementAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return board.Minions
                .Where(minion => IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every minion miniature standing outside the current Battle Zone, in canonical identity
        /// order: p.18's correction subjects, read from board position at the boundary rather than
        /// from any record of what moved.
        /// </summary>
        public static IReadOnlyList<MinionOccupancy> FindMinionsOutside(IBoardDisplacementAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return board.Minions
                .Where(minion => !IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every unit that counts as a minion in the current Battle Zone, with the side it belongs to
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

            var miniatures = FindMinionsInside(board)
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
                    IsInCurrentBattleZone(board, coordinate))
                .Select(unit => new EffectiveBattleZoneMinion(
                    unit, participants.GetTeamForHero(unit.HeroId).Side, false));
            return Array.AsReadOnly(miniatures.Concat(counting).ToArray());
        }

        private static bool IsInZone(BoardMapDefinition map, ZoneId zoneId, SpaceId spaceId)
        {
            var space = map.Spaces.FirstOrDefault(value => value.Id.Equals(spaceId));
            return space != null && space.ZoneId != null && space.ZoneId.Value.Equals(zoneId);
        }
    }

    /// <summary>One unit counting as a minion inside the current Battle Zone, with its side.</summary>
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
