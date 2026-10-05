using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    public enum BoardPartitionMode
    {
        None = 0,
        OppositeSides = 1,
        OppositeSidesAndController = 2
    }

    /// <summary>
    /// One active, fixed board partition. Its line is an occurrence binding rather than
    /// definition content: the chooser's space and axis are captured when the effect begins.
    /// </summary>
    public readonly struct BoardPartitionRestriction
    {
        public BoardPartitionRestriction(
            BoardPartitionLine line,
            HeroId controllerHeroId,
            bool separatesController)
        {
            if (!line.IsDefined)
                throw new ArgumentException("A restriction requires a chosen line.", nameof(line));
            if (!controllerHeroId.IsDefined)
                throw new ArgumentException("A restriction requires its controller.", nameof(controllerHeroId));
            Line = line;
            ControllerHeroId = controllerHeroId;
            SeparatesController = separatesController;
        }

        public BoardPartitionLine Line { get; }
        public HeroId ControllerHeroId { get; }
        public bool SeparatesController { get; }

        /// <summary>
        /// The acting unit's side is measured at the start of this interaction. A line
        /// space belongs to neither side. The stronger form additionally makes the
        /// controller absent to every unit standing on either side, even if the
        /// controller later moves onto the line.
        /// </summary>
        public bool BlocksInteraction(
            RuleObject actingUnit,
            HexCoordinate actorAt,
            RuleObject target,
            HexCoordinate targetAt)
        {
            if (actingUnit.Kind != RuleObjectKind.Hero &&
                actingUnit.Kind != RuleObjectKind.Minion)
                return false;
            var side = Line.SideOf(actorAt);
            return Line.Separates(actorAt, targetAt) ||
                (SeparatesController && side != BoardPartitionSide.OnLine &&
                 target.Kind == RuleObjectKind.Hero &&
                 target.HeroId.Equals(ControllerHeroId));
        }

        public bool BlocksSpaceInteraction(RuleObject actingUnit, HexCoordinate actorAt,
            HexCoordinate space) =>
            (actingUnit.Kind == RuleObjectKind.Hero || actingUnit.Kind == RuleObjectKind.Minion) &&
            Line.Separates(actorAt, space);
    }
}
