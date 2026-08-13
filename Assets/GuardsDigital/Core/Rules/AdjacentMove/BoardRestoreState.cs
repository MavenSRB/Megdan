using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    // A remaining minion's persisted board facts: its stable identity plus its current
    // coordinate. Identity-bound setup content (team, type, spawn) is reconstruction context
    // supplied to AdjacentMoveAuthority.Restore, not part of the persisted stream.
    public readonly struct RestoredMinionPosition
    {
        public RestoredMinionPosition(MinionId minionId, HexCoordinate coordinate)
        {
            if (!minionId.IsDefined)
                throw new ArgumentException("A restored minion position requires a defined minion ID.", nameof(minionId));
            MinionId = minionId;
            Coordinate = coordinate;
        }

        public MinionId MinionId { get; }
        public HexCoordinate Coordinate { get; }
    }

    // The board's persisted-field contract, matching the other authorities' restore-state
    // discipline: this state carries exactly the fields the snapshot's board section emits, its
    // Deconstruct is what the serializer's write path consumes, and reconstruction context the
    // snapshot stores elsewhere (the map definition and the configured minion content) stays a
    // separate required argument of AdjacentMoveAuthority.Restore. Adding a persisted board field
    // changes this type's arity, so capture, write, read, and restore stop compiling together
    // instead of silently omitting the new field from the stream.
    public sealed class BoardRestoreState
    {
        public BoardRestoreState(
            IEnumerable<HeroOccupancy> heroOccupancies,
            IEnumerable<ObstacleOccupancy> obstacleOccupancies,
            IEnumerable<RestoredMinionPosition> remainingMinions)
        {
            if (heroOccupancies == null) throw new ArgumentNullException(nameof(heroOccupancies));
            if (obstacleOccupancies == null) throw new ArgumentNullException(nameof(obstacleOccupancies));
            if (remainingMinions == null) throw new ArgumentNullException(nameof(remainingMinions));

            HeroOccupancies = heroOccupancies.ToArray();
            ObstacleOccupancies = obstacleOccupancies.ToArray();
            RemainingMinions = remainingMinions.ToArray();

            if (HeroOccupancies.Select(value => value.HeroId).Distinct().Count() != HeroOccupancies.Count)
                throw new ArgumentException("Restored hero occupancies must have unique hero IDs.", nameof(heroOccupancies));
            if (ObstacleOccupancies.Select(value => value.ObstacleId).Distinct().Count() != ObstacleOccupancies.Count)
                throw new ArgumentException("Restored obstacle occupancies must have unique obstacle IDs.", nameof(obstacleOccupancies));
            if (RemainingMinions.Select(value => value.MinionId).Distinct().Count() != RemainingMinions.Count)
                throw new ArgumentException("Restored remaining-minion IDs must be unique.", nameof(remainingMinions));
        }

        public IReadOnlyList<HeroOccupancy> HeroOccupancies { get; }
        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies { get; }
        public IReadOnlyList<RestoredMinionPosition> RemainingMinions { get; }

        public void Deconstruct(
            out IReadOnlyList<HeroOccupancy> heroOccupancies,
            out IReadOnlyList<ObstacleOccupancy> obstacleOccupancies,
            out IReadOnlyList<RestoredMinionPosition> remainingMinions)
        {
            heroOccupancies = HeroOccupancies;
            obstacleOccupancies = ObstacleOccupancies;
            remainingMinions = RemainingMinions;
        }
    }
}
