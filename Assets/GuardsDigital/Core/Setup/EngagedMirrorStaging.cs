using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Restages the configured mirror so both Wasps stand already engaged: each adjacent to one
    /// mid-board Blue melee minion, two spaces from each other around it. Milestone 8.15.
    ///
    /// <para>The configured setup starts the two heroes on their distant spawn points, which is right
    /// for a match and wrong for a fixture whose subject is what a played card does once battle is
    /// joined - every such fixture would first spend eight scripted approach turns that prove nothing
    /// this fixture is about. The board this produces is the milestone 8.13 board fixture's shape with
    /// the attacker closed to adjacency: the minion is a legal Attack target for the Red seat, the
    /// Blue hero is inside a printed Radius 2 disk without being adjacent to the Red hero, and the
    /// bent two-space gap between the heroes is a legal ranged-Attack line under rulebook p.19's
    /// straight-line complement.</para>
    ///
    /// <para>It is `DEV-004` development affordance on the same terms as the showcase composition:
    /// only a fixture or a one-shot scene request reaches it, and a launched player never does.
    /// Nothing here invents card state - it moves the setup's own heroes to different playable
    /// spaces before the match starts, which is a staging decision the rules never see.</para>
    /// </summary>
    public static class EngagedMirrorStaging
    {
        /// <summary>
        /// The setup with both heroes restaged around one engaged minion, which is reported so a
        /// fixture can name the piece the position was built around.
        /// </summary>
        public static QuickGameSetupState Restage(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            out MinionId engagedMinionId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (setup == null) throw new ArgumentNullException(nameof(setup));

            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));

            // The candidate order is geometric rather than a Battle-Zone read, deliberately: the
            // starting-zone flag is exactly what the Lane-Push conformance rule forbids production
            // code to consult, and this staging does not care about zones - it wants the mid-board
            // minion an advancing Red Wasp would meet first, which is the Blue melee minion nearest
            // Red's own spawn placement.
            var redSpawnAt = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).Coordinate;
            foreach (var minion in setup.Minions
                .Where(minion =>
                    minion.TeamSide == TeamSide.Blue &&
                    minion.MinionType == StandardMinionType.Melee)
                .OrderBy(minion => HexDistanceQuery.Calculate(minion.Coordinate, redSpawnAt))
                .ThenBy(minion => minion.MinionId.Value, StringComparer.Ordinal))
            {
                var free = minion.Coordinate.GetNeighbors()
                    .Where(coordinate =>
                        playable.Contains(coordinate) && !occupied.Contains(coordinate))
                    .ToArray();
                foreach (var redAt in free)
                {
                    foreach (var blueAt in free)
                    {
                        if (HexDistanceQuery.Calculate(blueAt, redAt) != 2)
                        {
                            continue;
                        }

                        engagedMinionId = minion.MinionId;
                        return Restaged(map, setup, redAt, blueAt);
                    }
                }
            }

            throw new InvalidOperationException(
                "No Blue melee Battle-Zone minion on this map has two free playable neighbors two " +
                "spaces apart, so the engaged mirror cannot be staged.");
        }

        private static QuickGameSetupState Restaged(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            HexCoordinate redAt,
            HexCoordinate blueAt)
        {
            var heroes = setup.Heroes.Select(hero =>
            {
                var coordinate = hero.TeamSide == TeamSide.Red ? redAt : blueAt;
                var space = map.Spaces.Single(value => value.Coordinate.Equals(coordinate));
                return new QuickGameHeroSetupState(
                    hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                    hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id), coordinate);
            }).ToArray();
            return new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes,
                setup.Minions);
        }
    }
}
