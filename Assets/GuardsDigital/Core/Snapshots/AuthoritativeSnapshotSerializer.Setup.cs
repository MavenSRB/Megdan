using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
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

        /// <summary>
        /// Milestone 6.2a. Records exactly which capability contract set this payload was written
        /// against, so a restore can refuse a build whose registered capabilities differ rather than
        /// discovering the mismatch later, mid-effect. Mirrors <see cref="WriteDefinitions"/>.
        /// </summary>
        private static void WriteCapabilityRegistrations(
            BinaryWriter writer,
            EffectCapabilityCatalog capabilities)
        {
            writer.Write(capabilities.Registered.Count);
            foreach (var reference in capabilities.Registered)
            {
                writer.Write(reference.CapabilityId.Value);
                writer.Write(reference.Revision);
            }
        }

        private static void ValidateCapabilityRegistrations(
            BinaryReader reader,
            EffectCapabilityCatalog capabilities)
        {
            var stored = ReadMany(reader, "capability registrations", () =>
            {
                var id = reader.ReadString();
                var revision = reader.ReadInt32();
                return id + "@" + revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
            });
            var current = capabilities.Registered
                .Select(reference => reference.ToString())
                .ToArray();
            if (stored.SequenceEqual(current, StringComparer.Ordinal))
            {
                return;
            }

            var missing = stored.Except(current, StringComparer.Ordinal).ToArray();
            var knownIds = new HashSet<string>(
                capabilities.Registered.Select(reference => reference.CapabilityId.Value),
                StringComparer.Ordinal);
            var unknownCapability = missing.Any(entry =>
                !knownIds.Contains(entry.Substring(0, entry.LastIndexOf('@'))));
            throw new SnapshotLoadException(
                unknownCapability
                    ? SnapshotLoadRejectionReason.EffectCapabilityMissing
                    : SnapshotLoadRejectionReason.EffectCapabilityRevisionMismatch,
                "Snapshot capability registrations do not match this build: stored [" +
                string.Join(", ", stored) + "], current [" + string.Join(", ", current) + "].");
        }

        /// <summary>
        /// Milestone 6.2. The component counterpart of <see cref="WriteCapabilityRegistrations"/>:
        /// records exactly which token and marker content set this payload was written against, so a
        /// restore refuses a build whose components differ rather than discovering the mismatch later,
        /// when a supply count no longer explains the tokens already on the board.
        /// </summary>
        private static void WriteComponentRegistrations(
            BinaryWriter writer,
            ComponentDefinitionCatalog components)
        {
            writer.Write(components.RegisteredTokens.Count);
            foreach (var reference in components.RegisteredTokens)
            {
                writer.Write(reference.DefinitionId.Value);
                writer.Write(reference.Revision);
            }

            writer.Write(components.RegisteredMarkers.Count);
            foreach (var reference in components.RegisteredMarkers)
            {
                writer.Write(reference.DefinitionId.Value);
                writer.Write(reference.Revision);
            }
        }

        private static void ValidateComponentRegistrations(
            BinaryReader reader,
            ComponentDefinitionCatalog components)
        {
            var storedTokens = ReadRegistrationKeys(reader, "token registrations");
            var storedMarkers = ReadRegistrationKeys(reader, "marker registrations");
            var currentTokens = components.RegisteredTokens.Select(reference => reference.ToString()).ToArray();
            var currentMarkers = components.RegisteredMarkers.Select(reference => reference.ToString()).ToArray();

            RequireRegistrationsMatch(storedTokens, currentTokens, "token");
            RequireRegistrationsMatch(storedMarkers, currentMarkers, "marker");
        }

        private static string[] ReadRegistrationKeys(BinaryReader reader, string name) =>
            ReadMany(reader, name, () =>
            {
                var id = reader.ReadString();
                var revision = reader.ReadInt32();
                return id + "@" + revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
            });

        private static void RequireRegistrationsMatch(
            string[] stored,
            string[] current,
            string componentKind)
        {
            if (stored.SequenceEqual(current, StringComparer.Ordinal))
            {
                return;
            }

            var missing = stored.Except(current, StringComparer.Ordinal).ToArray();
            var knownIds = new HashSet<string>(
                current.Select(entry => entry.Substring(0, entry.LastIndexOf('@'))),
                StringComparer.Ordinal);
            var unknownComponent = missing.Any(entry =>
                !knownIds.Contains(entry.Substring(0, entry.LastIndexOf('@'))));
            throw new SnapshotLoadException(
                unknownComponent
                    ? SnapshotLoadRejectionReason.ComponentDefinitionMissing
                    : SnapshotLoadRejectionReason.ComponentDefinitionRevisionMismatch,
                $"Snapshot {componentKind} registrations do not match this build: stored [" +
                string.Join(", ", stored) + "], current [" + string.Join(", ", current) + "].");
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
                WriteDashboard(writer, hero.Dashboard);
            }
        }

        /// <summary>
        /// Milestone 7.2a. One hero's four p.16 item slots, each with the position it occupies and every
        /// icon it accepts.
        ///
        /// The user decided on 2026-08-19 that the slots ride the wire rather than being rebound from
        /// configuration at restore. The layout is therefore written per hero and in full - positions
        /// and icons both - rather than as a named layout id, because a payload that named a layout
        /// would describe a build's vocabulary instead of the dashboards the match was played with, and
        /// a later build could give the same name different slots. What the reader does with it is the
        /// other half of that decision: it refuses a payload whose dashboards are not the ones this
        /// build supplies, exactly as the definition table refuses one whose card content differs.
        /// </summary>
        private static void WriteDashboard(BinaryWriter writer, HeroDashboard dashboard)
        {
            writer.Write(dashboard.Slots.Count);
            foreach (var slot in dashboard.Slots)
            {
                writer.Write(slot.Position.Index);
                writer.Write(slot.AcceptedIcons.Count);
                foreach (var icon in slot.AcceptedIcons)
                {
                    writer.Write((int)icon);
                }
            }
        }

        private static HeroDashboard ReadDashboard(BinaryReader reader)
        {
            var slots = ReadMany(reader, "dashboard item slots", () =>
            {
                var position = reader.ReadInt32();
                if (position < 1 || position > HeroDashboard.SlotCount)
                {
                    throw Malformed($"Dashboard item slot position {position} is outside the legal range.");
                }

                var icons = ReadMany(reader, "dashboard slot icons", () =>
                    SnapshotEnumReader.Read<PrintedValueChannel>(reader, "dashboard slot icon"));
                if (icons.Any(icon => icon == PrintedValueChannel.None))
                {
                    throw Malformed("A dashboard item slot accepts named printed icons.");
                }

                return new HeroDashboardSlot(new HeroDashboardSlotPosition(position), icons);
            });

            var dashboard = new HeroDashboard(slots);
            if (!dashboard.Equals(HeroDashboard.RulebookLayout))
            {
                // Every hero this build supplies declares rulebook p.16's layout, so that layout is what
                // "the current content" means here. When a verified hero declares a different one -
                // milestone 8.1's content - this comparison becomes a per-hero lookup against the
                // configured heroes the reader would then be handed, in the way the definition table is
                // compared against the current definitions rather than against a constant. Until then a
                // payload written by a build whose dashboards differ is refused rather than restored
                // into a match whose items would sit under slots this build does not have.
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.HeroDashboardMismatch,
                    "Snapshot hero dashboard [" + dashboard + "] does not match this build's [" +
                    HeroDashboard.RulebookLayout + "].");
            }

            return dashboard;
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
                new PlayerId(reader.ReadString()),
                ReadDashboard(reader)));
            return new MatchParticipants(teams, players, heroes);
        }

        private static void WriteBoard(BinaryWriter writer, AdjacentMoveAuthority board)
        {
            var (heroOccupancies, obstacleOccupancies, remainingMinions, placedTokens,
                currentBattleZoneId) = board.CaptureRestoreState();
            writer.Write(board.SelectedMap.MapId.Value);
            writer.Write(board.SelectedMap.ContentRevision);

            // Milestone 6.10. p.8's Battle Zone rides beside the map reference because it is scoped to
            // that map and means nothing without it. The empty string is the named absence rather than
            // a sentinel zone: a map that declares no starting Battle Zone has none to record, and a
            // reader that saw one would be reading a lane the map does not have.
            writer.Write(currentBattleZoneId.HasValue ? currentBattleZoneId.Value.Value : string.Empty);
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

            // Milestone 6.2. A placed token writes identity, provenance, and where it is - never its
            // supply size, display name, or any other definition content. Those come from the component
            // catalog on the way back in, for the reason the Active-effect section states at v8: a
            // payload may say which component this is and what has happened to it, not what it means.
            writer.Write(placedTokens.Count);
            foreach (var token in placedTokens.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                writer.Write(token.Id.Value);
                writer.Write(token.Definition.Id.Value);
                writer.Write(token.Definition.Revision);
                writer.Write(token.OwnerHeroId.Value ?? string.Empty);
                writer.Write(token.ControllerHeroId.Value ?? string.Empty);
                writer.Write(token.SourceCardInstanceId.Value ?? string.Empty);
                WriteCoordinate(writer, token.Coordinate);
            }
        }

        private static AdjacentMoveAuthority ReadBoard(
            BinaryReader reader,
            BoardMapCatalog mapCatalog,
            IEnumerable<QuickGameMinionSetupState> minions,
            ComponentDefinitionCatalog components)
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

            // Milestone 6.10. Read, then validated against the map that was just loaded, and refused
            // rather than repaired. A blank zone says the map declares none; anything else has to be a
            // zone p.8 could have pushed the Lane to on this map, which is the one statement of that
            // answer the board's own restore consults. The alternative - falling back to the
            // starting-Battle-Zone flag - is exactly what the acceptance check forbids: it would turn a
            // payload naming another map's zone, or a zone this build no longer declares, into a match
            // silently restored to the zone it began in.
            var currentBattleZoneValue = reader.ReadString();
            ZoneId? currentBattleZoneId = null;
            if (currentBattleZoneValue.Length > 0)
            {
                var candidate = new ZoneId(currentBattleZoneValue);
                if (!GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.CanBeCurrentBattleZone(
                        mapDefinition, candidate))
                {
                    throw Malformed(
                        $"Snapshot Battle Zone '{candidate}' is not a zone map '{reference}' could push the Lane to.");
                }

                currentBattleZoneId = candidate;
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
            var placedTokens = ReadMany(reader, "placed tokens", () =>
            {
                var id = new TokenInstanceId(reader.ReadString());
                var reference = new TokenDefinitionReference(
                    new TokenDefinitionId(reader.ReadString()), reader.ReadInt32());
                var owner = ReadOptionalHeroId(reader);
                var controller = ReadOptionalHeroId(reader);
                var sourceCard = ReadOptionalCardInstanceId(reader);
                return new PlacedToken(
                    id,
                    LoadTrustedTokenDefinition(components, reference),
                    owner,
                    controller,
                    sourceCard,
                    ReadCoordinate(reader));
            });
            return AdjacentMoveAuthority.Restore(
                new BoardRestoreState(
                    heroes, obstacles, remainingMinions, placedTokens, currentBattleZoneId),
                mapDefinition,
                availableMinions);
        }

        /// <summary>
        /// Binds a restored token to the catalog's own definition object, so the reconstructed instance
        /// is reference-equal to the trusted content rather than an equivalent rebuilt from the payload.
        /// The two load rejections stay distinct on the wire boundary as well, matching how the
        /// capability registrations and the map catalog each separate "absent" from "different".
        /// </summary>
        private static TokenDefinition LoadTrustedTokenDefinition(
            ComponentDefinitionCatalog components,
            TokenDefinitionReference reference)
        {
            try
            {
                return components.LoadToken(reference);
            }
            catch (ComponentDefinitionLoadException exception)
            {
                throw new SnapshotLoadException(
                    exception.Reason == ComponentDefinitionLoadRejectionReason.MissingDefinition
                        ? SnapshotLoadRejectionReason.ComponentDefinitionMissing
                        : SnapshotLoadRejectionReason.ComponentDefinitionRevisionMismatch,
                    exception.Message,
                    exception);
            }
        }

        private static HeroId ReadOptionalHeroId(BinaryReader reader)
        {
            var value = reader.ReadString();
            return string.IsNullOrEmpty(value) ? default : new HeroId(value);
        }

        private static CardInstanceId ReadOptionalCardInstanceId(BinaryReader reader)
        {
            var value = reader.ReadString();
            return string.IsNullOrEmpty(value) ? default : new CardInstanceId(value);
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
