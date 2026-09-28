using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    /// <summary>
    /// Milestone 8.114b1. The whole route one Movement ACTION walked - origin first, destination last -
    /// kept on the board after the action completes so card text applied after the movement can ask
    /// what the hero moved through.
    ///
    /// <para><b>Why the board keeps it.</b> A Movement action's route is persisted state only while the
    /// walk is under way (milestone 8.61e); at completion it is handed to p.18's move-through traps and
    /// dropped. Wuk's Trample reads it after that - "each enemy hero you moved through discards a card"
    /// - and a trap's own question can stand between the two on a later command, so the route has to
    /// survive a snapshot. It is overwritten by the next completed walk and carries no rule of its own.
    /// </para>
    /// </summary>
    public sealed class CompletedMovementWalk
    {
        public CompletedMovementWalk(HeroId mover, IEnumerable<HexCoordinate> route)
        {
            if (!mover.IsDefined)
            {
                throw new ArgumentException("A completed walk names the hero who walked it.", nameof(mover));
            }

            Route = Array.AsReadOnly((route ?? throw new ArgumentNullException(nameof(route))).ToArray());
            if (Route.Count < 1)
            {
                throw new ArgumentException("A completed walk names at least the space it started on.", nameof(route));
            }

            Mover = mover;
        }

        public HeroId Mover { get; }

        /// <summary>Every space walked, in order: the origin first and the destination last.</summary>
        public IReadOnlyList<HexCoordinate> Route { get; }

        /// <summary>The spaces entered and left - p.18's "moved through" - which excludes both ends.</summary>
        public IReadOnlyList<HexCoordinate> SpacesMovedThrough =>
            Route.Count <= 2
                ? (IReadOnlyList<HexCoordinate>)Array.Empty<HexCoordinate>()
                : Route.Skip(1).Take(Route.Count - 2).ToArray();
    }

    /// <summary>Milestone 8.114b1. The board's record of the last completed Movement action walk.</summary>
    public interface ICompletedMovementWalkRecord
    {
        /// <summary>The last Movement action walk completed on this board, or <c>null</c>.</summary>
        CompletedMovementWalk LastCompletedMovementWalk { get; }
    }

    /// <summary>
    /// Milestone 8.114b1. p.8's Minion Battle counts every minion in the Battle Zone; Wuk's Claim and
    /// Assert Dominance make up to one (two) of them "not count towards the minion total". The board
    /// holds that round's exclusions from the moment they are chosen (the pre-battle trigger) until the
    /// battle is planned on a later command, because a snapshot can fall between the two.
    /// </summary>
    public interface IMinionBattleExclusionAuthority
    {
        IReadOnlyCollection<MinionId> MinionsExcludedFromMinionBattle { get; }

        void ExcludeFromMinionBattle(IEnumerable<MinionId> minions);
    }
}
