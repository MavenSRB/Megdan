using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittTurnHistoryTests
    {
        private static readonly HeroId First = new HeroId("history-first");
        private static readonly HeroId Second = new HeroId("history-second");
        private static readonly MinionId Minion = new MinionId("history-minion");
        private static readonly MapId Map = new MapId("history-map");
        private static readonly HexCoordinate A = new HexCoordinate(0, 0);
        private static readonly HexCoordinate B = new HexCoordinate(1, 0);
        private static readonly HexCoordinate C = new HexCoordinate(2, 0);
        private static readonly HexCoordinate D = new HexCoordinate(3, 0);

        [Test]
        public void PriorTurnImmobility_RejectsMoveAwayAndBack_AndSurvivesForkAndRestore()
        {
            var (board, map, setup) = CreateBoard();
            var unit = BoardObjectReference.ForHero(First);
            board.RecordTurnStart();
            board.RecordTurnStart();
            Assert.That(board.RemainedSincePreviousTurn(unit), Is.True);
            Assert.That(board.TryGetTurnStartPosition(unit, out var start), Is.True);
            Assert.That(start, Is.EqualTo(A));

            Assert.That(board.TryCommit(new AdjacentMoveCommand(First, A, B)).Accepted, Is.True);
            Assert.That(board.TryCommit(new AdjacentMoveCommand(First, B, A)).Accepted, Is.True);
            Assert.That(board.RemainedSincePreviousTurn(unit), Is.False);
            Assert.That(board.TryGetTurnStartPosition(unit, out start), Is.True);
            Assert.That(start, Is.EqualTo(A));
            var restored = AdjacentMoveAuthority.Restore(board.CaptureRestoreState(), map, setup);
            Assert.That(restored.RemainedSincePreviousTurn(unit), Is.False);
        }

        [Test]
        public void SwappingAwayAndBack_IsStillMovement()
        {
            var (board, _, _) = CreateBoard();
            var first = BoardObjectReference.ForHero(First);
            var second = BoardObjectReference.ForHero(Second);
            board.RecordTurnStart();
            board.RecordTurnStart();

            Assert.That(board.CommitValidatedSwap(first, second).Accepted, Is.True);
            Assert.That(board.CommitValidatedSwap(first, second).Accepted, Is.True);
            Assert.That(board.RemainedSincePreviousTurn(first), Is.False);
            Assert.That(board.RemainedSincePreviousTurn(second), Is.False);
        }

        [Test]
        public void RemovedAndRespawnedMinion_DoesNotCountAsStationary()
        {
            var (board, map, setup) = CreateBoard();
            var unit = BoardObjectReference.ForMinion(Minion);
            board.RecordTurnStart();
            board.RecordTurnStart();
            Assert.That(board.RemainedSincePreviousTurn(unit), Is.True);

            Assert.That(board.CommitValidatedRemoval(unit).Accepted, Is.True);
            Assert.That(board.RemainedSincePreviousTurn(unit), Is.False);
            Assert.That(board.CommitValidatedLanePushPlacement(Minion, C), Is.True);
            Assert.That(board.RemainedSincePreviousTurn(unit), Is.False);
            var restored = AdjacentMoveAuthority.Restore(board.CaptureRestoreState(), map, setup);
            Assert.That(restored.RemainedSincePreviousTurn(unit), Is.False);
        }

        [Test]
        public void UnitAbsentAtTurnStart_HasNoPlaceDestination()
        {
            var (board, _, _) = CreateBoard();
            var unit = BoardObjectReference.ForMinion(Minion);
            Assert.That(board.CommitValidatedRemoval(unit).Accepted, Is.True);
            board.RecordTurnStart();
            Assert.That(board.CommitValidatedLanePushPlacement(Minion, C), Is.True);
            Assert.That(board.TryGetTurnStartPosition(unit, out _), Is.False);
        }

        private static (AdjacentMoveAuthority, BoardMapDefinition, QuickGameMinionSetupState[]) CreateBoard()
        {
            var map = BoardMapDefinition.CreateSynthetic(Map, 1, new[] { A, B, C, D });
            var setup = new[] { new QuickGameMinionSetupState(
                Minion, new TeamId(StagedBoardParticipants.RedTeamIdValue), TeamSide.Red,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("history-spawn")),
                new ConfiguredSpaceReference(Map, new SpaceId("q2_r0")), C) };
            var board = new AdjacentMoveAuthority(map,
                new[] { new HeroOccupancy(First, A), new HeroOccupancy(Second, D) },
                Array.Empty<ObstacleOccupancy>(), setup);
            return (board, map, setup);
        }
    }
}
