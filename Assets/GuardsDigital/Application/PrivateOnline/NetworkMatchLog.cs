using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35r. One line of the seat-public match log as it crosses the wire.
    /// </summary>
    public sealed class NetworkMatchLogRow
    {
        public NetworkMatchLogRow(
            int sequence,
            int kind,
            int roundIndex,
            int turnIndex,
            string subjectHeroId,
            string targetHeroId,
            string subjectMinionId,
            string subjectCardInstanceId,
            string subjectTeamId,
            bool hasOrigin,
            int originQ,
            int originR,
            bool hasDestination,
            int destinationQ,
            int destinationR,
            int outcome,
            int amount,
            string blockingHeroId = null,
            string blockingCardInstanceId = null,
            IEnumerable<CardMoveFact> cardMoves = null,
            IEnumerable<HexCoordinate> movementRoute = null)
        {
            CardMoves = Array.AsReadOnly((cardMoves ?? Enumerable.Empty<CardMoveFact>()).ToArray());
            MovementRoute = Array.AsReadOnly((movementRoute ?? Enumerable.Empty<HexCoordinate>()).ToArray());
            Sequence = sequence;
            Kind = kind;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            SubjectHeroId = subjectHeroId;
            TargetHeroId = targetHeroId;
            SubjectMinionId = subjectMinionId;
            SubjectCardInstanceId = subjectCardInstanceId;
            SubjectTeamId = subjectTeamId;
            HasOrigin = hasOrigin;
            OriginQ = originQ;
            OriginR = originR;
            HasDestination = hasDestination;
            DestinationQ = destinationQ;
            DestinationR = destinationR;
            Outcome = outcome;
            Amount = amount;
            BlockingHeroId = blockingHeroId;
            BlockingCardInstanceId = blockingCardInstanceId;
        }

        public int Sequence { get; }
        public int Kind { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public string SubjectHeroId { get; }
        public string TargetHeroId { get; }
        public string SubjectMinionId { get; }
        public string SubjectCardInstanceId { get; }
        public string SubjectTeamId { get; }
        public bool HasOrigin { get; }
        public int OriginQ { get; }
        public int OriginR { get; }
        public bool HasDestination { get; }
        public int DestinationQ { get; }
        public int DestinationR { get; }
        public int Outcome { get; }
        public int Amount { get; }

        /// <summary>
        /// Milestone 8.35z14 (the blocker-identity channel; protocol version 8). Present only on a
        /// <c>BlockedAction</c> row whose refusing node could explain itself.
        /// </summary>
        public string BlockingHeroId { get; }

        /// <summary>Milestone 8.35z14. Beside <see cref="BlockingHeroId"/>.</summary>
        public string BlockingCardInstanceId { get; }

        /// <summary>
        /// Milestone 9.32g, protocol /35. A swap's, a kept swap's or an Ultimate's unlock's moves, as
        /// the seat this frame is for may read them - a move it may not name arrives unnamed.
        /// </summary>
        public IReadOnlyList<CardMoveFact> CardMoves { get; }

        /// <summary>Milestone 9.86, protocol /48. The ordered public Movement route.</summary>
        public IReadOnlyList<HexCoordinate> MovementRoute { get; }
    }

    /// <summary>
    /// Milestone 8.35r. The match log's own codec: the typed entry in, bounded bytes out, and a
    /// decode that rebuilds through the entry's own constructor so its guards validate wire input.
    /// </summary>
    public static class NetworkMatchLogCodec
    {
        /// <summary>
        /// The most lines one frame may carry. The retained window is the bound, and it is stated
        /// here as well so a decoder refuses an oversized claim before allocating for it.
        /// </summary>
        public const int MaxRows = MatchLogProjection.RetainedEntries;

        /// <summary>
        /// Milestone 9.32g. The most card moves one row may carry: a swap's two and Ready for War's
        /// three, with room - stated so a decoder refuses an oversized claim before allocating for it.
        /// </summary>
        public const int MaxCardMovesPerRow = 8;

        /// <summary>Maximum board spaces carried for one resolved Movement route.</summary>
        public const int MaxMovementRouteSpaces = 64;

        /// <summary>The typed entry as the row the wire carries.</summary>
        public static NetworkMatchLogRow ToRow(MatchLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            return new NetworkMatchLogRow(
                entry.Sequence,
                (int)entry.Kind,
                entry.RoundIndex,
                entry.TurnIndex,
                entry.SubjectHeroId.Value,
                entry.TargetHeroId.Value,
                entry.SubjectMinionId.Value,
                entry.SubjectCardInstanceId.Value,
                entry.SubjectTeamId.Value,
                entry.HasOrigin,
                entry.Origin.Q,
                entry.Origin.R,
                entry.HasDestination,
                entry.Destination.Q,
                entry.Destination.R,
                (int)entry.Outcome,
                entry.Amount,
                entry.BlockingHeroId.Value,
                entry.BlockingCardInstanceId.Value,
                entry.CardMoves,
                entry.MovementRoute);
        }

        /// <summary>
        /// The row's bytes. The ONE writer for this format - the message codec calls it rather than
        /// keeping a second copy, because two writers for one layout drift apart silently and the
        /// reader below can only match one of them.
        /// </summary>
        /// <remarks>
        /// Internal, like <c>NetworkCommandCodec</c>'s own pair and for the same reason: a public
        /// <see cref="BinaryWriter"/> parameter would put <c>System.IO</c> on the transport-neutral
        /// port surface, which <c>OnlineArchitectureConformanceTests</c> refuses. The public half of
        /// this type is <see cref="ToRow"/> and <see cref="ToEntry"/>, which name only Core types.
        /// </remarks>
        internal static void Write(BinaryWriter writer, NetworkMatchLogRow row)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (row == null) throw new ArgumentNullException(nameof(row));

            writer.Write(row.Sequence);
            writer.Write(row.Kind);
            writer.Write(row.RoundIndex);
            writer.Write(row.TurnIndex);
            WriteOptional(writer, row.SubjectHeroId);
            WriteOptional(writer, row.TargetHeroId);
            WriteOptional(writer, row.SubjectMinionId);
            WriteOptional(writer, row.SubjectCardInstanceId);
            WriteOptional(writer, row.SubjectTeamId);
            writer.Write(row.HasOrigin);
            if (row.HasOrigin)
            {
                writer.Write(row.OriginQ);
                writer.Write(row.OriginR);
            }

            writer.Write(row.HasDestination);
            if (row.HasDestination)
            {
                writer.Write(row.DestinationQ);
                writer.Write(row.DestinationR);
            }

            writer.Write(row.Outcome);
            writer.Write(row.Amount);

            // Milestone 8.35z14, protocol version 8: appended at the end, so a version 7 peer's own
            // reader - which stops after `amount` - is refused from the bounded header rather than
            // reading these two fields as the start of whatever it expects to come next.
            WriteOptional(writer, row.BlockingHeroId);
            WriteOptional(writer, row.BlockingCardInstanceId);

            // Milestone 9.32g, protocol /35: the row's card moves, appended at the end on 8.35z14's
            // terms - a /34 peer is refused from the header before it could read them.
            writer.Write(row.CardMoves.Count);
            foreach (var move in row.CardMoves)
            {
                WriteOptional(writer, move.Definition.IsDefined ? move.Definition.Value : null);
                writer.Write((int)move.From);
                writer.Write((int)move.To);
                writer.Write(move.DepartsFacedown);
                writer.Write(move.ArrivesFacedown);
                writer.Write((int)move.KnownColor);
            }

            writer.Write(row.MovementRoute.Count);
            foreach (var space in row.MovementRoute)
            {
                writer.Write(space.Q);
                writer.Write(space.R);
            }
        }

        internal static bool TryRead(BinaryReader reader, out NetworkMatchLogRow row)
        {
            row = null;
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var sequence = reader.ReadInt32();
            var kind = reader.ReadInt32();
            if (sequence < 1 ||
                !Enum.IsDefined(typeof(MatchLogEntryKind), kind) ||
                kind == (int)MatchLogEntryKind.None)
            {
                return false;
            }

            var roundIndex = reader.ReadInt32();
            var turnIndex = reader.ReadInt32();
            if (!TryReadOptional(reader, out var subjectHero) ||
                !TryReadOptional(reader, out var targetHero) ||
                !TryReadOptional(reader, out var subjectMinion) ||
                !TryReadOptional(reader, out var subjectCard) ||
                !TryReadOptional(reader, out var subjectTeam))
            {
                return false;
            }

            var hasOrigin = reader.ReadBoolean();
            var originQ = 0;
            var originR = 0;
            if (hasOrigin)
            {
                originQ = reader.ReadInt32();
                originR = reader.ReadInt32();
            }

            var hasDestination = reader.ReadBoolean();
            var destinationQ = 0;
            var destinationR = 0;
            if (hasDestination)
            {
                destinationQ = reader.ReadInt32();
                destinationR = reader.ReadInt32();
            }

            var outcome = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(MatchLogOutcome), outcome))
            {
                return false;
            }

            var amount = reader.ReadInt32();
            if (!TryReadOptional(reader, out var blockingHero) ||
                !TryReadOptional(reader, out var blockingCard) ||
                !TryReadCardMoves(reader, out var cardMoves) ||
                !TryReadMovementRoute(reader, out var movementRoute))
            {
                return false;
            }

            row = new NetworkMatchLogRow(
                sequence, kind, roundIndex, turnIndex,
                subjectHero, targetHero, subjectMinion, subjectCard, subjectTeam,
                hasOrigin, originQ, originR,
                hasDestination, destinationQ, destinationR,
                outcome, amount, blockingHero, blockingCard, cardMoves, movementRoute);
            return true;
        }

        /// <summary>
        /// Milestone 9.32g, protocol /35. At most <see cref="MaxCardMovesPerRow"/> moves, each between
        /// two named zones with a card colour or none - or the frame is refused. Like every other
        /// field of a row, the moves are carried whatever the row's kind.
        /// </summary>
        private static bool TryReadCardMoves(BinaryReader reader, out IReadOnlyList<CardMoveFact> moves)
        {
            moves = null;
            var count = reader.ReadInt32();
            if (count < 0 || count > MaxCardMovesPerRow)
            {
                return false;
            }

            var read = new List<CardMoveFact>(count);
            for (var index = 0; index < count; index++)
            {
                if (!TryReadOptional(reader, out var definition))
                {
                    return false;
                }

                var from = reader.ReadInt32();
                var to = reader.ReadInt32();
                var departsFacedown = reader.ReadBoolean();
                var arrivesFacedown = reader.ReadBoolean();
                var color = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(CardMoveZone), from) || from == (int)CardMoveZone.None ||
                    !Enum.IsDefined(typeof(CardMoveZone), to) || to == (int)CardMoveZone.None ||
                    !Enum.IsDefined(typeof(CardColor), color) || color == (int)CardColor.Purple)
                {
                    return false;
                }

                read.Add(new CardMoveFact(
                    definition == null ? default : new CardDefinitionId(definition),
                    (CardMoveZone)from,
                    (CardMoveZone)to,
                    departsFacedown,
                    arrivesFacedown,
                    (CardColor)color));
            }

            moves = read;
            return true;
        }


        private static bool TryReadMovementRoute(BinaryReader reader, out IReadOnlyList<HexCoordinate> route)
        {
            route = null;
            var count = reader.ReadInt32();
            if (count < 0 || count > MaxMovementRouteSpaces)
            {
                return false;
            }

            var read = new List<HexCoordinate>(count);
            for (var index = 0; index < count; index++)
            {
                read.Add(new HexCoordinate(reader.ReadInt32(), reader.ReadInt32()));
            }

            route = read;
            return true;
        }
        /// <summary>
        /// The decoded row as the typed entry a read model publishes. It goes back through
        /// <see cref="MatchLogEntry"/>'s own constructor, so a wire value the type would refuse -
        /// a sequence below one, a kind of None - is refused here too.
        /// </summary>
        public static MatchLogEntry ToEntry(NetworkMatchLogRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            return new MatchLogEntry(
                row.Sequence,
                (MatchLogEntryKind)row.Kind,
                row.RoundIndex,
                row.TurnIndex,
                subjectHeroId: string.IsNullOrEmpty(row.SubjectHeroId)
                    ? default
                    : new HeroId(row.SubjectHeroId),
                targetHeroId: string.IsNullOrEmpty(row.TargetHeroId)
                    ? default
                    : new HeroId(row.TargetHeroId),
                subjectMinionId: string.IsNullOrEmpty(row.SubjectMinionId)
                    ? default
                    : new MinionId(row.SubjectMinionId),
                subjectCardInstanceId: string.IsNullOrEmpty(row.SubjectCardInstanceId)
                    ? default
                    : new CardInstanceId(row.SubjectCardInstanceId),
                subjectTeamId: string.IsNullOrEmpty(row.SubjectTeamId)
                    ? default
                    : new TeamId(row.SubjectTeamId),
                origin: new HexCoordinate(row.OriginQ, row.OriginR),
                destination: new HexCoordinate(row.DestinationQ, row.DestinationR),
                hasOrigin: row.HasOrigin,
                hasDestination: row.HasDestination,
                outcome: (MatchLogOutcome)row.Outcome,
                amount: row.Amount,
                blockingHeroId: string.IsNullOrEmpty(row.BlockingHeroId)
                    ? default
                    : new HeroId(row.BlockingHeroId),
                blockingCardInstanceId: string.IsNullOrEmpty(row.BlockingCardInstanceId)
                    ? default
                    : new CardInstanceId(row.BlockingCardInstanceId),
                cardMoves: row.CardMoves,
                movementRoute: row.MovementRoute);
        }

        private static void WriteOptional(BinaryWriter writer, string value)
        {
            var stated = !string.IsNullOrWhiteSpace(value);
            writer.Write(stated);
            if (stated)
            {
                writer.Write(value);
            }
        }

        private static bool TryReadOptional(BinaryReader reader, out string value)
        {
            value = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            value = reader.ReadString();
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
