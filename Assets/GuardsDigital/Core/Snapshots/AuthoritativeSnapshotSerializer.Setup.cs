using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        private static void WriteDefinitions(BinaryWriter writer, IReadOnlyList<CardDefinition> definitions)
        {
            var ordered = definitions.OrderBy(definition => definition.Id.Value, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            foreach (var definition in ordered)
            {
                writer.Write(definition.Id.Value);
                writer.Write(definition.ContentRevision);
            }
        }

        private static CardDefinition[] ValidateDefinitions(
            BinaryReader reader,
            IEnumerable<CardDefinition> currentDefinitions)
        {
            var definitions = ActionOfferQuery.BuildDefinitionLookup(
                    currentDefinitions ?? throw new ArgumentNullException(nameof(currentDefinitions)))
                .Values
                .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal)
                .ToArray();
            var count = ReadCount(reader, "definition references");
            var stored = new List<CardDefinitionReference>();
            for (var index = 0; index < count; index++)
            {
                stored.Add(new CardDefinitionReference(
                    new CardDefinitionId(reader.ReadString()),
                    reader.ReadString()));
            }

            var current = definitions.Select(definition => definition.Reference).ToArray();
            if (!stored.SequenceEqual(current))
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch,
                    "Snapshot card-definition IDs/content revisions do not match the current rules content.");
            }

            return definitions;
        }

        private static void WriteParticipants(BinaryWriter writer, MatchParticipants participants)
        {
            writer.Write(participants.Teams.Count);
            foreach (var team in participants.Teams)
            {
                writer.Write(team.Id.Value);
                writer.Write((int)team.Side);
            }

            writer.Write(participants.Players.Count);
            foreach (var player in participants.Players)
            {
                writer.Write(player.Id.Value);
                writer.Write(player.TeamId.Value);
            }

            writer.Write(participants.Heroes.Count);
            foreach (var hero in participants.Heroes)
            {
                writer.Write(hero.Id.Value);
                writer.Write(hero.PlayerId.Value);
            }
        }

        private static MatchParticipants ReadParticipants(BinaryReader reader)
        {
            var teams = ReadMany(reader, "teams", () => new Team(
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "team side")));
            var players = ReadMany(reader, "players", () => new Player(
                new PlayerId(reader.ReadString()),
                new TeamId(reader.ReadString())));
            var heroes = ReadMany(reader, "heroes", () => new Hero(
                new HeroId(reader.ReadString()),
                new PlayerId(reader.ReadString())));
            return new MatchParticipants(teams, players, heroes);
        }

        private static void WriteBoard(BinaryWriter writer, AdjacentMoveAuthority board)
        {
            var (heroOccupancies, obstacleOccupancies, remainingMinions) = board.CaptureRestoreState();
            writer.Write(board.SelectedMap.MapId.Value);
            writer.Write(board.SelectedMap.ContentRevision);
            writer.Write(heroOccupancies.Count);
            foreach (var occupancy in heroOccupancies.OrderBy(value => value.HeroId.Value, StringComparer.Ordinal))
            {
                writer.Write(occupancy.HeroId.Value);
                WriteCoordinate(writer, occupancy.Coordinate);
            }

            writer.Write(obstacleOccupancies.Count);
            foreach (var occupancy in obstacleOccupancies.OrderBy(value => value.ObstacleId.Value, StringComparer.Ordinal))
            {
                writer.Write(occupancy.ObstacleId.Value);
                WriteCoordinate(writer, occupancy.Coordinate);
            }

            writer.Write(remainingMinions.Count);
            foreach (var minion in remainingMinions.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal))
            {
                writer.Write(minion.MinionId.Value);
                WriteCoordinate(writer, minion.Coordinate);
            }
        }

        private static AdjacentMoveAuthority ReadBoard(
            BinaryReader reader,
            BoardMapCatalog mapCatalog,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            var reference = new MapDefinitionReference(new MapId(reader.ReadString()), reader.ReadInt32());
            BoardMapDefinition mapDefinition;
            try
            {
                mapDefinition = mapCatalog.Load(reference);
            }
            catch (BoardMapLoadException exception)
            {
                var reason = exception.Reason == BoardMapLoadRejectionReason.MissingMap
                    ? SnapshotLoadRejectionReason.MapDefinitionMissing
                    : SnapshotLoadRejectionReason.MapDefinitionContentRevisionMismatch;
                throw new SnapshotLoadException(reason, exception.Message, exception);
            }
            var heroes = ReadMany(reader, "hero occupancies", () => new HeroOccupancy(
                new HeroId(reader.ReadString()),
                ReadCoordinate(reader)));
            var obstacles = ReadMany(reader, "obstacle occupancies", () => new ObstacleOccupancy(
                new ObstacleId(reader.ReadString()),
                ReadCoordinate(reader)));
            var availableMinions = (minions ?? throw new ArgumentNullException(nameof(minions))).ToArray();
            var remainingMinions = ReadMany(reader, "remaining minions", () => new RestoredMinionPosition(
                new MinionId(reader.ReadString()),
                ReadCoordinate(reader)));
            if (remainingMinions.Select(value => value.MinionId).Distinct().Count() != remainingMinions.Length ||
                remainingMinions.Any(value => availableMinions.Count(minion => minion.MinionId.Equals(value.MinionId)) != 1))
            {
                throw Malformed("Snapshot remaining-minion identities do not match configured minion content.");
            }
            return AdjacentMoveAuthority.Restore(
                new BoardRestoreState(heroes, obstacles, remainingMinions),
                mapDefinition,
                availableMinions);
        }

        private static void WriteCards(BinaryWriter writer, IReadOnlyList<CardInstance> cards)
        {
            writer.Write(cards.Count);
            foreach (var card in cards)
            {
                writer.Write(card.Id.Value);
                WriteDefinitionReference(writer, card.DefinitionReference);
                writer.Write(card.OwnerId.Value);
                writer.Write((int)card.State.Location);
                writer.Write(card.State.HasBeenPlayed);
                writer.Write((int)card.State.ResolutionStatus);
                writer.Write((int)card.State.FaceOrientation);
            }
        }

        private static CardInstance[] ReadCards(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants)
        {
            var knownReferences = new HashSet<CardDefinitionReference>(definitions.Select(value => value.Reference));
            return ReadMany(reader, "card instances", () =>
            {
                var id = new CardInstanceId(reader.ReadString());
                var reference = ReadDefinitionReference(reader);
                if (!knownReferences.Contains(reference))
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch,
                        $"Card '{id}' references unavailable rules content '{reference}'.");
                }

                var owner = new HeroId(reader.ReadString());
                participants.GetHero(owner);
                return new CardInstance(
                    id,
                    reference,
                    owner,
                    new CardStateFacts(
                        SnapshotEnumReader.Read<CardLocation>(reader, "card location"),
                        reader.ReadBoolean(),
                        SnapshotEnumReader.Read<CardResolutionStatus>(reader, "card resolution status"),
                        SnapshotEnumReader.Read<CardFaceOrientation>(reader, "card face orientation")));
            });
        }

        // Every Write* below destructures a restore state rather than reading authority
        // properties. Adding a persisted field to one of those states changes its
        // Deconstruct arity, so this method stops compiling instead of silently omitting
        // the new field from the stream. That is the write half of milestone 6.1g.
    }
}
