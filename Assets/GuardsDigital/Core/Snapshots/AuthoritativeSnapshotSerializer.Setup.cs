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
            var state = board.CaptureRestoreState();
            var (heroOccupancies, obstacleOccupancies, remainingMinions, placedTokens,
                _, minionBattleExclusions, lastCompletedMovementWalk,
                heroBodySupplies, actingBodies, currentTurnStart, previousTurnStart,
                unitMutations) = state;
            writer.Write(board.SelectedMap.MapId.Value);
            writer.Write(board.SelectedMap.ContentRevision);

            // Milestone 6.10. p.8's Battle Zone rides beside the map reference because it is scoped to
            // that map and means nothing without it. The empty string is the named absence rather than
            // a sentinel zone: a map that declares no starting Battle Zone has none to record, and a
            // reader that saw one would be reading a lane the map does not have.
            writer.Write(state.CurrentBattleZoneIds.Count);
            foreach (var currentBattleZoneId in state.CurrentBattleZoneIds)
                writer.Write(currentBattleZoneId.Value);
            writer.Write(heroOccupancies.Count);
            // Milestone 9.21a, schema v49. Each row is one BODY of a hero, written in (hero, body) order;
            // every single-bodied hero writes body 1.
            foreach (var occupancy in heroOccupancies
                .OrderBy(value => value.HeroId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Body))
            {
                writer.Write(occupancy.HeroId.Value);
                writer.Write(occupancy.Body);
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

            // Milestone 8.114b1, schema v47. This round's Minion-Battle exclusions (Wuk's Dominance)
            // and the last completed Movement action walk (Trample's "moved through"). Both are board
            // facts a later command reads, so both ride the board rather than a card's own state.
            writer.Write(minionBattleExclusions.Count);
            foreach (var minion in minionBattleExclusions.OrderBy(value => value.Value, StringComparer.Ordinal))
            {
                writer.Write(minion.Value);
            }

            writer.Write(lastCompletedMovementWalk != null);
            if (lastCompletedMovementWalk != null)
            {
                writer.Write(lastCompletedMovementWalk.Mover.Value);
                writer.Write(lastCompletedMovementWalk.Route.Count);
                foreach (var space in lastCompletedMovementWalk.Route)
                {
                    WriteCoordinate(writer, space);
                }
            }

            // Milestone 9.21a, schema v49. The heroes that declare more than one body (RZ-Q1), by hero
            // id; empty for every match the build deals today. Milestone 9.21b, schema v50: each row
            // ends with the acting body settled for that hero's current action (RZ-Q4), 0 for none.
            writer.Write(heroBodySupplies.Count);
            foreach (var supply in heroBodySupplies.OrderBy(value => value.HeroId.Value, StringComparer.Ordinal))
            {
                writer.Write(supply.HeroId.Value);
                writer.Write(supply.Bodies);
                var acting = actingBodies.Where(value => value.HeroId.Equals(supply.HeroId)).ToArray();
                writer.Write(acting.Length == 0 ? 0 : acting[0].Body);
            }

            // v55: movement revisions survive restore, including removal and a move away and back.
            writer.Write(unitMutations.Count);
            foreach (var entry in unitMutations.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
            WriteTurnPositions(writer, currentTurnStart);
            WriteTurnPositions(writer, previousTurnStart);
        }

        private static void WriteTurnPositions(BinaryWriter writer, IReadOnlyList<UnitTurnPosition> positions)
        {
            writer.Write(positions.Count);
            foreach (var position in positions.OrderBy(value => (int)value.Unit.Kind)
                .ThenBy(value => value.Unit.Kind == BoardObjectKind.Hero
                    ? value.Unit.HeroId.Value : value.Unit.MinionId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Unit.HeroBody))
            {
                writer.Write((int)position.Unit.Kind);
                writer.Write(position.Unit.Kind == BoardObjectKind.Hero
                    ? position.Unit.HeroId.Value : position.Unit.MinionId.Value);
                writer.Write(position.Unit.HeroBody);
                WriteCoordinate(writer, position.Coordinate);
                writer.Write(position.Mutation);
            }
        }

        private static UnitTurnPosition[] ReadTurnPositions(BinaryReader reader, string label)
        {
            return ReadMany(reader, label, () =>
            {
                var kind = SnapshotEnumReader.Read<BoardObjectKind>(reader, "turn position unit kind");
                var id = reader.ReadString();
                var body = reader.ReadInt32();
                if ((kind != BoardObjectKind.Hero && kind != BoardObjectKind.Minion) ||
                    (kind == BoardObjectKind.Hero && body < 1) ||
                    (kind == BoardObjectKind.Minion && body != 0))
                    throw Malformed("Snapshot turn position does not name a unit body.");
                var unit = kind == BoardObjectKind.Hero
                    ? BoardObjectReference.ForHeroBody(new HeroId(id), body)
                    : BoardObjectReference.ForMinion(new MinionId(id));
                return new UnitTurnPosition(unit, ReadCoordinate(reader), reader.ReadInt32());
            });
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
            var currentBattleZoneIds = ReadMany(reader, "current Battle Zones", () =>
            {
                var candidate = new ZoneId(reader.ReadString());
                if (!GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.CanBeCurrentBattleZone(
                        mapDefinition, candidate))
                {
                    throw Malformed(
                        $"Snapshot Battle Zone '{candidate}' is not a zone map '{reference}' could push the Lane to.");
                }
                return candidate;
            });
            var expectedBattleZoneCount = mapDefinition.OrderedLanes.Count > 0
                ? mapDefinition.OrderedLanes.Count
                : mapDefinition.Zones.Count(zone => zone.IsStartingBattleZone);
            if (currentBattleZoneIds.Length != expectedBattleZoneCount ||
                currentBattleZoneIds.Distinct().Count() != currentBattleZoneIds.Length ||
                (mapDefinition.OrderedLanes.Count > 0 && currentBattleZoneIds.Where((zoneId, laneIndex) =>
                    !mapDefinition.OrderedLanes[laneIndex].Contains(zoneId)).Any()))
                throw Malformed("Snapshot current Battle Zones must name exactly one zone on each ordered Lane.");

            var heroes = ReadMany(reader, "hero occupancies", () =>
            {
                var heroId = new HeroId(reader.ReadString());
                var body = reader.ReadInt32();
                if (body < HeroOccupancy.FirstBody)
                {
                    throw Malformed("Snapshot hero occupancy names a body ordinal below 1.");
                }

                return new HeroOccupancy(heroId, body, ReadCoordinate(reader));
            });
            var obstacles = ReadMany(reader, "obstacle occupancies", () => new ObstacleOccupancy(
                new ObstacleId(reader.ReadString()),
                ReadCoordinate(reader)));
            var configuredMinions = (minions ?? throw new ArgumentNullException(nameof(minions))).ToArray();
            var availableMinions = configuredMinions.Concat(
                QuickGameSetupAuthority.CreateLaneReserve(mapDefinition, configuredMinions)).ToArray();
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
            var exclusions = ReadMany(reader, "minion battle exclusions", () => new MinionId(reader.ReadString()));
            if (exclusions.Distinct().Count() != exclusions.Length ||
                exclusions.Any(value => availableMinions.Count(minion => minion.MinionId.Equals(value)) != 1))
            {
                throw Malformed("Snapshot minion-battle exclusions do not name distinct configured minions.");
            }

            GuardsDigital.Core.Rules.AdjacentMove.CompletedMovementWalk lastWalk = null;
            if (reader.ReadBoolean())
            {
                var mover = new HeroId(reader.ReadString());
                var route = ReadMany(reader, "completed movement walk", () => ReadCoordinate(reader));
                if (!mover.IsDefined || route.Length < 1)
                {
                    throw Malformed("Snapshot completed movement walk must name a hero and at least one space.");
                }

                lastWalk = new GuardsDigital.Core.Rules.AdjacentMove.CompletedMovementWalk(mover, route);
            }

            var actingBodies = new List<HeroActingBody>();
            var bodySupplies = ReadMany(reader, "hero body supplies", () =>
            {
                var heroId = new HeroId(reader.ReadString());
                var bodies = reader.ReadInt32();
                if (!heroId.IsDefined || bodies < 2)
                {
                    throw Malformed("Snapshot hero body supply must name a hero with more than one body.");
                }

                // Milestone 9.21b, schema v50: the acting body of the current action, 0 for none.
                var acting = reader.ReadInt32();
                if (acting < 0 || acting > bodies)
                {
                    throw Malformed("Snapshot acting body must be 0 or a body within the hero's supply.");
                }

                if (acting > 0)
                {
                    actingBodies.Add(new HeroActingBody(heroId, acting));
                }

                return new HeroBodySupply(heroId, bodies);
            });

            var mutations = ReadMany(reader, "unit movement counters", () =>
                new KeyValuePair<string, int>(reader.ReadString(), reader.ReadInt32()));
            var currentStart = ReadTurnPositions(reader, "current turn-start positions");
            var previousStart = ReadTurnPositions(reader, "previous turn-start positions");
            return AdjacentMoveAuthority.Restore(
                BoardRestoreState.ForBattleZones(
                    heroes, obstacles, remainingMinions, placedTokens, currentBattleZoneIds,
                    exclusions, lastWalk, bodySupplies, actingBodies,
                    currentStart, previousStart, mutations),
                mapDefinition,
                configuredMinions);
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

            // Milestone 8.71a, SCHEMA v44. The printed Initiative icons some card text has rewritten,
            // as a SPARSE section rather than a field on every card row above.
            //
            // A flag per card would have cost one byte per card in every payload ever written to say
            // "no" - a hundred and fourteen of them in a six-hero match - for a fact that is true of at
            // most one card at a time. This says only what is true: a count, which is zero in every
            // payload no Hurry up! has touched, and then the rewritten cards. It is written here rather
            // than in its own section of the stream because it is a fact about these cards and this is
            // where they are; ReadCards below applies it to the instances it just built, so the
            // override has exactly one home on the read side too.
            var rewritten = cards.Where(card => card.InitiativeOverride.IsDefined).ToArray();
            writer.Write(rewritten.Length);
            foreach (var card in rewritten)
            {
                writer.Write(card.Id.Value);
                writer.Write(card.InitiativeOverride.Value);
                writer.Write(card.InitiativeOverride.SourceCardInstanceId.Value);
            }
        }

        private static CardInstance[] ReadCards(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants)
        {
            var knownReferences = new HashSet<CardDefinitionReference>(definitions.Select(value => value.Reference));
            var cards = ReadMany(reader, "card instances", () =>
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

            // Milestone 8.71a, schema v44. The sparse rewritten-Initiative section WriteCards above
            // states. It is applied to the instances that method's own rows just built, by identity:
            // a row naming a card this payload does not contain, or naming a card the payload does not
            // leave in the one lifecycle shape the rewrite can stand on, is a payload describing a
            // match this build cannot accept rather than one to silently drop.
            var byId = cards.ToDictionary(card => card.Id);
            var rewritten = ReadMany(reader, "rewritten card Initiative values", () =>
            {
                var id = new CardInstanceId(reader.ReadString());
                var value = reader.ReadInt32();
                var source = new CardInstanceId(reader.ReadString());
                return (id, value, source);
            });
            foreach (var (id, value, source) in rewritten)
            {
                if (!byId.TryGetValue(id, out var card))
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.MalformedPayload,
                        $"Snapshot rewrites the printed Initiative of card '{id.Value}', which it " +
                        "does not contain.");
                }

                if (value < 0 || !source.IsDefined ||
                    !CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, value, source))
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.MalformedPayload,
                        $"Snapshot rewrites the printed Initiative of card '{id.Value}', which it " +
                        "does not leave as an unresolved played card (rulebook p.10).");
                }
            }

            ValidateUnlockedUltimates(cards, definitions);
            return cards;
        }

        /// <summary>
        /// Milestone 7.3. Rulebook p.16 places one Ultimate, and only an Ultimate, to the left of a hero
        /// dashboard, so a payload that describes anything else there describes a match this build
        /// cannot accept.
        ///
        /// The unlock is recorded by the card's position alone - there is one place the card goes, so a
        /// second per-hero record beside it could only agree or be wrong - and this is the check that
        /// arrangement owes. <c>UltimateUnlockQuery</c> answers what is unlocked and deliberately skips
        /// content it cannot resolve rather than throwing, because a query is not the place to fail a
        /// match; the reader is, and it fails here so that no query downstream has to.
        /// </summary>
        private static void ValidateUnlockedUltimates(
            IReadOnlyList<CardInstance> cards,
            IReadOnlyList<CardDefinition> definitions)
        {
            var byReference = new Dictionary<CardDefinitionReference, CardDefinition>();
            foreach (var definition in definitions)
            {
                if (!byReference.ContainsKey(definition.Reference))
                {
                    byReference.Add(definition.Reference, definition);
                }
            }

            var owners = new HashSet<HeroId>();
            foreach (var card in cards)
            {
                if (card.State.Location != CardLocation.UltimateArea)
                {
                    continue;
                }

                if (!byReference.TryGetValue(card.DefinitionReference, out var definition) ||
                    !definition.Progression.IsUltimate)
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.UnlockedUltimateMismatch,
                        $"Card '{card.Id}' sits beside hero '{card.OwnerId}''s dashboard without being " +
                        "an Ultimate (rulebook p.16).");
                }

                if (!owners.Add(card.OwnerId))
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.UnlockedUltimateMismatch,
                        $"Hero '{card.OwnerId}' has more than one unlocked Ultimate; p.16 unlocks one.");
                }
            }
        }

        // Every Write* below destructures a restore state rather than reading authority
        // properties. Adding a persisted field to one of those states changes its
        // Deconstruct arity, so this method stops compiling instead of silently omitting
        // the new field from the stream. That is the write half of milestone 6.1g.
    }
}
