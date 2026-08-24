using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Rulebook p.9's Radius: the inclusive disk a printed Radius icon scopes an action to, and the
    /// objects inside it.
    ///
    /// The page states the geometry and its one exclusion outright. "If your card has a Range/Radius
    /// value of 1, your primary action can only affect adjacent targets. With a Range/Radius value of
    /// 3, your primary action can affect targets up to 3 spaces away", and "there are no 'line of
    /// sight' rules. You can target units through obstacles, as long as they are in range/radius." So
    /// this is <see cref="HexDistanceQuery"/> geometry counting spaces - never movement reachability, a
    /// route length, an occupancy filter, or an invented obstruction rule - and the disk is inclusive
    /// at its edge.
    ///
    /// It takes the Radius <see cref="ValueResult"/> rather than a bare integer, and refuses one of any
    /// other channel. That is the milestone's own boundary made structural: the number a Radius disk is
    /// derived from must be the composed Radius Total - printed icon plus p.16's matching items - and
    /// not a Range, an Attack value, or an integer an effect's text happened to carry.
    ///
    /// The three questions below are deliberately separate, because the rulebook separates them.
    /// <see cref="Spaces"/> is geometry alone and knows nothing about who is standing where.
    /// <see cref="Targets"/> is targeting, so it filters p.18 immunity through
    /// <see cref="TargetQuery"/>: "if a unit is immune, you cannot target that unit".
    /// <see cref="UnitsPresent"/> is presence, so it filters none through
    /// <see cref="UnitPresenceQuery"/>: p.18's "effects that check for unit presence ... will count
    /// immune units". Asking the same disk both ways can legitimately return different sets, and that
    /// difference is the rule rather than an inconsistency.
    /// </summary>
    public static class RadiusQuery
    {
        /// <summary>
        /// Every space within the Radius Total of <paramref name="origin"/>, inclusive of the origin
        /// itself and of the outermost ring, in canonical axial order.
        ///
        /// The origin is included because a disk contains its centre; whether the acting unit's own
        /// space is a legal subject is a targeting question its caller states, exactly as
        /// <see cref="TargetQuery"/>'s <c>includeSelf</c> does, rather than a shape this geometry
        /// decides. Spaces are returned whether or not the board has them: an off-board coordinate
        /// holds no object, so a caller that wants objects asks one of the two questions below and a
        /// caller that wants spaces filters by the board it has.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> Spaces(HexCoordinate origin, ValueResult radiusTotal)
        {
            var radius = RequireRadiusTotal(radiusTotal);
            var spaces = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    spaces.Add(new HexCoordinate(origin.Q + dq, origin.R + dr));
                }
            }

            return Array.AsReadOnly(spaces.ToArray());
        }

        /// <summary>
        /// Whether one coordinate lies inside the disk. The same inclusive comparison
        /// <see cref="Spaces"/> enumerates, for a caller holding a single coordinate rather than a set.
        /// </summary>
        public static bool Contains(HexCoordinate origin, ValueResult radiusTotal, HexCoordinate space) =>
            HexDistanceQuery.Calculate(origin, space) <= RequireRadiusTotal(radiusTotal);

        /// <summary>
        /// Every board object inside the disk that <paramref name="targetingUnit"/> may target under
        /// <paramref name="activity"/>, in <see cref="TargetQuery"/>'s canonical axial order.
        ///
        /// This is the complete eligible set an effect that affects every unit in Radius needs, and it
        /// is complete by construction rather than by the caller stopping when it has enough. The order
        /// is canonical so that two runs of the same state answer identically; it is **not** a mutation
        /// order and no rule may read it as one. p.10's simultaneous resolution and each card's own
        /// commit policy belong to the effect doing the affecting, which is why this query mutates
        /// nothing and declares no ordering beyond determinism.
        /// </summary>
        public static IReadOnlyList<BoardObjectReference> Targets(
            HexCoordinate origin,
            ValueResult radiusTotal,
            bool includeSelf,
            RuleObject targetingUnit,
            RuleActivity activity,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<ImmunityOverrideDeclaration> immunityOverrides = null,
            RuleActivity overrideActivity = RuleActivity.None) =>
            TargetQuery.FindTargetsWithin(
                origin,
                RequireRadiusTotal(radiusTotal),
                includeSelf,
                targetingUnit,
                activity,
                board,
                participants,
                modifications,
                immunityOverrides,
                overrideActivity);

        /// <summary>
        /// Every unit inside the disk whose effective type is <paramref name="effectiveUnitKind"/>, in
        /// canonical identity order, counting immune units because p.18 says a presence check does.
        /// </summary>
        public static IReadOnlyList<RuleObject> UnitsPresent(
            HexCoordinate origin,
            ValueResult radiusTotal,
            EffectiveUnitKind effectiveUnitKind,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications) =>
            UnitPresenceQuery.FindUnitsWithin(
                origin,
                RequireRadiusTotal(radiusTotal),
                effectiveUnitKind,
                board,
                modifications);

        private static int RequireRadiusTotal(ValueResult radiusTotal)
        {
            if (radiusTotal == null)
            {
                throw new ArgumentNullException(nameof(radiusTotal));
            }

            if (radiusTotal.Channel != PrintedValueChannel.Radius)
            {
                throw new ArgumentException(
                    $"A Radius question is asked of a Radius total; this one is {radiusTotal.Channel}.",
                    nameof(radiusTotal));
            }

            if (radiusTotal.Total < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radiusTotal),
                    radiusTotal.Total,
                    "A Radius total cannot be negative.");
            }

            return radiusTotal.Total;
        }
    }
}
