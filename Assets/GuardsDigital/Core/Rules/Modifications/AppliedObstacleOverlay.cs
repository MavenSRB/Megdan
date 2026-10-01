using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// One <see cref="ConditionalObstacleOverlay"/> bound to the moment it is being asked about: the
    /// space its source hero currently stands in, and where the hero performing the current action
    /// stands relative to the disk drawn around it.
    ///
    /// Both bindings happen once, in <see cref="RuleModificationQuery"/>, and for the same reason the
    /// team and global affected scopes are expanded there: the derivation holds the board and the
    /// participants, and a query that consulted the live set afterwards would have to hold them too.
    /// What is left here is a distance comparison, which is what makes
    /// <see cref="ObstacleQuery"/> able to answer p.18's classification without knowing anything about
    /// Active effects, teams, or who is acting.
    ///
    /// The acting hero's own position is read once, at derivation, rather than re-read as a route is
    /// walked. That is not an approximation: an overlay that declares both arms is a boundary the
    /// acting hero cannot cross, so it stands on the same side at every step of its own action, and an
    /// overlay that declares one arm states its condition about the hero performing the action rather
    /// than about wherever the piece being moved has got to.
    /// </summary>
    public sealed class AppliedObstacleOverlay
    {
        private readonly bool actorQualifies;
        private readonly bool actorIsInside;
        private readonly System.Collections.Generic.IReadOnlyCollection<RuleObject> barredUnits;

        private AppliedObstacleOverlay(
            ConditionalObstacleOverlay declaration,
            System.Collections.Generic.IReadOnlyList<HexCoordinate> anchors,
            int radius,
            string sourceId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            bool actorQualifies,
            bool actorIsInside,
            System.Collections.Generic.IReadOnlyCollection<RuleObject> barredUnits)
        {
            Declaration = declaration;
            Anchors = anchors;
            Radius = radius;
            SourceId = sourceId;
            ControllerHeroId = controllerHeroId;
            ControllerTeamSide = controllerTeamSide;
            this.actorQualifies = actorQualifies;
            this.actorIsInside = actorIsInside;
            this.barredUnits = barredUnits ?? Array.Empty<RuleObject>();
        }

        /// <summary>
        /// Binds one declaration to the current moment.
        /// </summary>
        /// <param name="anchor">The source hero's current space (p.17).</param>
        /// <param name="radius">
        /// Milestone 8.9. The disk the source's declaration draws right now - its printed Radius icon
        /// with p.16's matching items on it, or the number its own text wrote down. Resolved by the
        /// derivation for the reason the anchor is: that is where the card and the hero's items are.
        /// </param>
        /// <param name="actingHeroSide">
        /// The side of the hero performing the current action, or <c>null</c> when no hero is
        /// performing one - a round-boundary correction, a restore with no action in flight. An
        /// overlay conditions on a hero acting, so with none it applies to nothing.
        /// </param>
        /// <param name="actingHeroSpace">
        /// Where that hero stands. Ignored when <paramref name="actingHeroSide"/> is <c>null</c>.
        /// </param>
        public static AppliedObstacleOverlay Bind(
            ConditionalObstacleOverlay declaration,
            HexCoordinate anchor,
            int radius,
            string sourceId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            TeamSide? actingHeroSide,
            HexCoordinate actingHeroSpace) =>
            Bind(declaration, new[] { anchor }, radius, sourceId, controllerHeroId, controllerTeamSide,
                actingHeroSide, actingHeroSpace);

        /// <summary>
        /// Milestone 9.21h (RZ-Q13). The same binding around every body of a many-bodied source hero:
        /// the disk is the union of one disk per body, so a space is inside when it is inside any.
        /// </summary>
        public static AppliedObstacleOverlay Bind(
            ConditionalObstacleOverlay declaration,
            System.Collections.Generic.IReadOnlyList<HexCoordinate> anchors,
            int radius,
            string sourceId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            TeamSide? actingHeroSide,
            HexCoordinate actingHeroSpace) =>
            Bind(declaration, anchors, radius, sourceId, controllerHeroId, controllerTeamSide,
                actingHeroSide, actingHeroSpace, null);

        /// <param name="barredUnits">
        /// Milestone 9.32c. For a <see cref="ObstacleOverlayArm.EmptySpacesInsideForUnits"/> overlay,
        /// the units its <see cref="ConditionalObstacleOverlay.UnitRelation"/> names, resolved by the
        /// derivation that holds the board and the participants, as the anchor and radius are.
        /// </param>
        public static AppliedObstacleOverlay Bind(
            ConditionalObstacleOverlay declaration,
            System.Collections.Generic.IReadOnlyList<HexCoordinate> anchors,
            int radius,
            string sourceId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            TeamSide? actingHeroSide,
            HexCoordinate actingHeroSpace,
            System.Collections.Generic.IReadOnlyCollection<RuleObject> barredUnits)
        {
            if (anchors == null || anchors.Count == 0)
            {
                throw new ArgumentException("A bound overlay is drawn around at least one space.", nameof(anchors));
            }

            if (declaration == null) throw new ArgumentNullException(nameof(declaration));
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "A bound overlay requires its source identity.", nameof(sourceId));
            }

            if (!controllerHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A bound overlay requires the hero controlling its source.", nameof(controllerHeroId));
            }

            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), radius, "An overlay's radius is the inclusive disk p.9 defines.");
            }

            var qualifies = actingHeroSide.HasValue && Qualifies(
                declaration.ActingHeroRelation, actingHeroSide.Value, controllerTeamSide);
            var inside = qualifies &&
                IsInside(anchors, radius, actingHeroSpace);
            return new AppliedObstacleOverlay(
                declaration, System.Linq.Enumerable.ToArray(anchors), radius, sourceId, controllerHeroId, controllerTeamSide,
                qualifies, inside,
                barredUnits == null
                    ? null
                    : new System.Collections.Generic.HashSet<RuleObject>(barredUnits));
        }

        public ConditionalObstacleOverlay Declaration { get; }

        /// <summary>The source hero's space at derivation - the centre of p.17's moving disk.</summary>
        public HexCoordinate Anchor => Anchors[0];

        /// <summary>
        /// Milestone 9.21h (RZ-Q13). Every centre the disk is drawn around: the one space of a hero with
        /// one body, every body of a many-bodied one.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<HexCoordinate> Anchors { get; }

        private static bool IsInside(
            System.Collections.Generic.IReadOnlyList<HexCoordinate> anchors, int radius, HexCoordinate space) =>
            System.Linq.Enumerable.Any(anchors, anchor => HexDistanceQuery.Calculate(anchor, space) <= radius);

        /// <summary>The disk's inclusive radius at derivation, items already composed onto it.</summary>
        public int Radius { get; }

        /// <summary>The source's stable identity, which is also this overlay's provenance key.</summary>
        public string SourceId { get; }

        public HeroId ControllerHeroId { get; }

        public TeamSide ControllerTeamSide { get; }

        /// <summary>
        /// Whether a hero whose action this overlay conditions on is performing one right now. False
        /// makes every space below ordinary, which is the overlay applying to nobody rather than to
        /// everybody.
        /// </summary>
        public bool AppliesToTheCurrentActor => actorQualifies;

        /// <summary>
        /// Whether <paramref name="space"/> counts as an obstacle under this overlay.
        ///
        /// The arm that answers is chosen by where the acting hero stands, which is the whole of the
        /// condition: outside the disk, the disk is closed to it; inside, everything beyond it is.
        /// </summary>
        public bool CountsAsObstacle(HexCoordinate space)
        {
            if (!actorQualifies)
            {
                return false;
            }

            var spaceIsInside = IsInside(Anchors, Radius, space);
            return actorIsInside
                ? !spaceIsInside && Declaration.Declares(ObstacleOverlayArm.SpacesOutsideWhileActorInside)
                : spaceIsInside && Declaration.Declares(ObstacleOverlayArm.SpacesInsideWhileActorOutside);
        }

        /// <summary>
        /// Milestone 9.32c. Whether an EMPTY space counts as an obstacle for <paramref name="piece"/>:
        /// the overlay bars that unit (TK-Q3) and the space lies inside the disk drawn around its
        /// source hero right now (p.17), other than the hero's own space. The caller has already found
        /// the space empty - an occupied space is an obstacle for every piece without asking this, and
        /// a token's "may be moved through" is its own sentence - so nothing here reads the board.
        /// </summary>
        public bool CountsAsObstacleFor(HexCoordinate space, RuleObject piece) =>
            piece.IsDefined &&
            Declaration.Declares(ObstacleOverlayArm.EmptySpacesInsideForUnits) &&
            System.Linq.Enumerable.Contains(barredUnits, piece) &&
            IsInside(Anchors, Radius, space) &&
            !System.Linq.Enumerable.Contains(Anchors, space);

        private static bool Qualifies(
            SourceTeamRelation relation, TeamSide actingSide, TeamSide controllerSide)
        {
            switch (relation)
            {
                case SourceTeamRelation.AnyTeam:
                    return true;
                case SourceTeamRelation.OpposingSource:
                    return actingSide != controllerSide;
                case SourceTeamRelation.AlliedWithSource:
                    return actingSide == controllerSide;
                default:
                    throw new InvalidOperationException(
                        "A bound overlay declares a defined team relation.");
            }
        }

        public override string ToString() =>
            $"{Declaration} at radius {Radius} around {Anchor} from {SourceId}";
    }
}
