using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
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
            IEnumerable<MinionId> remainingMinionIds)
        {
            if (heroOccupancies == null) throw new ArgumentNullException(nameof(heroOccupancies));
            if (obstacleOccupancies == null) throw new ArgumentNullException(nameof(obstacleOccupancies));
            if (remainingMinionIds == null) throw new ArgumentNullException(nameof(remainingMinionIds));

            HeroOccupancies = heroOccupancies.ToArray();
            ObstacleOccupancies = obstacleOccupancies.ToArray();
            RemainingMinionIds = remainingMinionIds.ToArray();

            if (HeroOccupancies.Select(value => value.HeroId).Distinct().Count() != HeroOccupancies.Count)
                throw new ArgumentException("Restored hero occupancies must have unique hero IDs.", nameof(heroOccupancies));
            if (ObstacleOccupancies.Select(value => value.ObstacleId).Distinct().Count() != ObstacleOccupancies.Count)
                throw new ArgumentException("Restored obstacle occupancies must have unique obstacle IDs.", nameof(obstacleOccupancies));
            if (RemainingMinionIds.Any(value => !value.IsDefined) ||
                RemainingMinionIds.Distinct().Count() != RemainingMinionIds.Count)
                throw new ArgumentException("Restored remaining-minion IDs must be defined and unique.", nameof(remainingMinionIds));
        }

        public IReadOnlyList<HeroOccupancy> HeroOccupancies { get; }
        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies { get; }
        public IReadOnlyList<MinionId> RemainingMinionIds { get; }

        public void Deconstruct(
            out IReadOnlyList<HeroOccupancy> heroOccupancies,
            out IReadOnlyList<ObstacleOccupancy> obstacleOccupancies,
            out IReadOnlyList<MinionId> remainingMinionIds)
        {
            heroOccupancies = HeroOccupancies;
            obstacleOccupancies = ObstacleOccupancies;
            remainingMinionIds = RemainingMinionIds;
        }
    }
}
