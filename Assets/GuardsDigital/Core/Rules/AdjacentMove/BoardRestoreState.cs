using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
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
            IEnumerable<RestoredMinionPosition> remainingMinions,
            IEnumerable<PlacedToken> placedTokens,
            ZoneId? currentBattleZoneId,
            IEnumerable<GuardsDigital.Core.Setup.MinionId> minionBattleExclusions,
            CompletedMovementWalk lastCompletedMovementWalk,
            IEnumerable<HeroBodySupply> heroBodySupplies,
            IEnumerable<HeroActingBody> actingBodies)
        {
            if (heroOccupancies == null) throw new ArgumentNullException(nameof(heroOccupancies));
            if (obstacleOccupancies == null) throw new ArgumentNullException(nameof(obstacleOccupancies));
            if (remainingMinions == null) throw new ArgumentNullException(nameof(remainingMinions));
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));

            CurrentBattleZoneId = currentBattleZoneId;
            MinionBattleExclusions = (minionBattleExclusions ??
                throw new ArgumentNullException(nameof(minionBattleExclusions))).Distinct().ToArray();
            LastCompletedMovementWalk = lastCompletedMovementWalk;
            HeroOccupancies = heroOccupancies.ToArray();
            HeroBodySupplies = (heroBodySupplies ??
                throw new ArgumentNullException(nameof(heroBodySupplies))).ToArray();
            ActingBodies = (actingBodies ??
                throw new ArgumentNullException(nameof(actingBodies))).ToArray();
            ObstacleOccupancies = obstacleOccupancies.ToArray();
            RemainingMinions = remainingMinions.ToArray();
            PlacedTokens = placedTokens.ToArray();

            // Milestone 9.21a: unique by hero AND body. Whether a hero may have a second body at all is
            // the board's own rule, asked again by the constructor the restore builds.
            if (HeroOccupancies.Select(value => (value.HeroId, value.Body)).Distinct().Count() != HeroOccupancies.Count)
                throw new ArgumentException("Restored hero occupancies must have unique hero IDs.", nameof(heroOccupancies));
            if (HeroBodySupplies.Select(value => value.HeroId).Distinct().Count() != HeroBodySupplies.Count)
                throw new ArgumentException("Restored hero body supplies must name each hero once.", nameof(heroBodySupplies));
            // Milestone 9.21b: an acting body belongs to a hero that declares bodies, once, within its supply.
            if (ActingBodies.Select(value => value.HeroId).Distinct().Count() != ActingBodies.Count ||
                ActingBodies.Any(acting => !HeroBodySupplies.Any(supply =>
                    supply.HeroId.Equals(acting.HeroId) && acting.Body <= supply.Bodies)))
                throw new ArgumentException(
                    "A restored acting body names a hero that declares bodies, once, within its supply.",
                    nameof(actingBodies));
            if (ObstacleOccupancies.Select(value => value.ObstacleId).Distinct().Count() != ObstacleOccupancies.Count)
                throw new ArgumentException("Restored obstacle occupancies must have unique obstacle IDs.", nameof(obstacleOccupancies));
            if (RemainingMinions.Select(value => value.MinionId).Distinct().Count() != RemainingMinions.Count)
                throw new ArgumentException("Restored remaining-minion IDs must be unique.", nameof(remainingMinions));
            if (PlacedTokens.Any(value => value == null))
                throw new ArgumentException("Restored placed tokens must be non-null.", nameof(placedTokens));
            if (PlacedTokens.Select(value => value.Id).Distinct().Count() != PlacedTokens.Count)
                throw new ArgumentException("Restored placed-token IDs must be unique.", nameof(placedTokens));
        }

        public IReadOnlyList<HeroOccupancy> HeroOccupancies { get; }
        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies { get; }
        public IReadOnlyList<RestoredMinionPosition> RemainingMinions { get; }

        // Milestone 6.2. A placed token carries its own trusted definition object rather than a
        // reference the way a restored minion carries only an ID: a minion's identity-bound content is
        // supplied to Restore separately, whereas a token's definition is rebound from the component
        // catalog before this state is built, so what arrives here is already trusted.
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }

        // Milestone 6.10. p.8's Battle Zone travels as a bare zone identity and nothing else: the
        // zone's spaces, display name and lane position all come from the map the restore has already
        // loaded, so what a payload may say is which zone this is, never what that zone contains. The
        // named absence is a board whose map declares no starting Battle Zone, which is every synthetic
        // one; restoring one as anything else would invent a lane the map does not have.
        public ZoneId? CurrentBattleZoneId { get; }

        /// <summary>Milestone 8.114b1, schema v47. This round's Minion-Battle exclusions (Dominance).</summary>
        public IReadOnlyList<GuardsDigital.Core.Setup.MinionId> MinionBattleExclusions { get; }

        /// <summary>Milestone 8.114b1, schema v47. The last completed Movement action walk, or null.</summary>
        public CompletedMovementWalk LastCompletedMovementWalk { get; }

        /// <summary>Milestone 9.21a, schema v49. The heroes that declare more than one body (RZ-Q1).</summary>
        public IReadOnlyList<HeroBodySupply> HeroBodySupplies { get; }

        /// <summary>
        /// Milestone 9.21b, schema v50. The acting body settled for a many-bodied hero's current
        /// action (RZ-Q4); empty for every hero with one body.
        /// </summary>
        public IReadOnlyList<HeroActingBody> ActingBodies { get; }

        public void Deconstruct(
            out IReadOnlyList<HeroOccupancy> heroOccupancies,
            out IReadOnlyList<ObstacleOccupancy> obstacleOccupancies,
            out IReadOnlyList<RestoredMinionPosition> remainingMinions,
            out IReadOnlyList<PlacedToken> placedTokens,
            out ZoneId? currentBattleZoneId,
            out IReadOnlyList<GuardsDigital.Core.Setup.MinionId> minionBattleExclusions,
            out CompletedMovementWalk lastCompletedMovementWalk,
            out IReadOnlyList<HeroBodySupply> heroBodySupplies,
            out IReadOnlyList<HeroActingBody> actingBodies)
        {
            heroOccupancies = HeroOccupancies;
            obstacleOccupancies = ObstacleOccupancies;
            remainingMinions = RemainingMinions;
            placedTokens = PlacedTokens;
            currentBattleZoneId = CurrentBattleZoneId;
            minionBattleExclusions = MinionBattleExclusions;
            lastCompletedMovementWalk = LastCompletedMovementWalk;
            heroBodySupplies = HeroBodySupplies;
            actingBodies = ActingBodies;
        }
    }
}
