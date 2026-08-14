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
        private const string Magic = "GUARDS-AUTHORITATIVE-SNAPSHOT";
        private const int MaximumDecodedBytes = 4 * 1024 * 1024;
        private const int MaximumBase64Characters = ((MaximumDecodedBytes + 2) / 3) * 4;
        // v7: the board section serializes each remaining minion's current coordinate alongside
        // its identity; v6 reconstructed positions from setup placements and cannot express a
        // moved minion, so it is explicitly rejected (milestone 6.1s migrate-or-reject decision).
        public const int CurrentSchemaVersion = 7;

        /// <summary>
        /// Instrumentation, not production state. Milestone 6.1i asserts the exact number of full
        /// state serializations an accepted and a rejected command perform, which finding `F613-08`
        /// requires to be measured rather than inspected. The project is single-threaded, so a plain
        /// counter is sufficient; a fixture resets it and reads it around a known command.
        /// </summary>
        public static long SerializeCallCount { get; private set; }

        /// <summary>Total encoded characters produced. See the envelope's counterpart.</summary>
        public static long SerializedCharacterCount { get; private set; }

        public static void ResetSerializeCallCount()
        {
            SerializeCallCount = 0;
            SerializedCharacterCount = 0;
        }

        public static string Serialize(RoundResolutionAuthority round, AdjacentMoveAuthority board)
        {
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (board == null) throw new ArgumentNullException(nameof(board));

            SerializeCallCount++;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(CurrentSchemaVersion);
                WriteDefinitions(writer, round.Definitions);
                WriteParticipants(writer, round.Participants);
                WriteBoard(writer, board);
                WriteCards(writer, round.Instances);
                WriteRound(writer, round);
                writer.Flush();
                var encoded = Convert.ToBase64String(stream.ToArray());
                SerializedCharacterCount += encoded.Length;
                return encoded;
            }
        }

        public static RestoredAuthoritativeMatch Deserialize(
            string payload,
            IEnumerable<CardDefinition> currentDefinitions,
            IEnumerable<BoardMapDefinition> currentMapDefinitions)
        {
            return Deserialize(
                payload,
                currentDefinitions,
                currentMapDefinitions,
                Enumerable.Empty<QuickGameMinionSetupState>());
        }

        public static RestoredAuthoritativeMatch Deserialize(
            string payload,
            IEnumerable<CardDefinition> currentDefinitions,
            IEnumerable<BoardMapDefinition> currentMapDefinitions,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload must not be blank.");
            }

            if (payload.Length > MaximumBase64Characters)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload exceeds the supported size limit.");
            }

            try
            {
                var bytes = Convert.FromBase64String(payload);
                if (bytes.Length > MaximumDecodedBytes)
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.MalformedPayload,
                        "Snapshot payload exceeds the supported size limit.");
                }
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream))
                {
                    if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal))
                    {
                        throw Malformed("Snapshot header is not recognized.");
                    }

                    var schemaVersion = reader.ReadInt32();
                    if (schemaVersion != CurrentSchemaVersion)
                    {
                        throw new SnapshotLoadException(
                            SnapshotLoadRejectionReason.UnsupportedSchemaVersion,
                            $"Snapshot schema version {schemaVersion} is unsupported; expected {CurrentSchemaVersion}.");
                    }

                    var mapCatalog = new BoardMapCatalog(
                        currentMapDefinitions ?? throw new ArgumentNullException(nameof(currentMapDefinitions)));
                    var definitions = ValidateDefinitions(reader, currentDefinitions);
                    var participants = ReadParticipants(reader);
                    var board = ReadBoard(reader, mapCatalog, minions);
                    var cards = ReadCards(reader, definitions, participants);
                    var round = ReadRound(reader, definitions, cards, participants, board);
                    if (stream.Position != stream.Length)
                    {
                        throw Malformed("Snapshot contains unexpected trailing data.");
                    }

                    return new RestoredAuthoritativeMatch(
                        round,
                        board,
                        participants,
                        Array.AsReadOnly(cards));
                }
            }
            catch (SnapshotLoadException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload is malformed or internally inconsistent.",
                    exception);
            }
        }

        private static void WriteCommonResolution(BinaryWriter writer, ActionResolutionId resolutionId,
            CardInstanceId cardId, CardDefinitionReference definitionReference, ActionDefinitionId actionId,
            HeroId actorId, ActionInvocationContext context, ActionResolutionStepId stepId)
        {
            writer.Write(resolutionId.Value); writer.Write(cardId.Value); WriteDefinitionReference(writer, definitionReference);
            writer.Write(actionId.Value); writer.Write(actorId.Value); writer.Write((int)context); writer.Write(stepId.Value);
        }

        private static CommonResolution ReadCommonResolution(BinaryReader reader)
        {
            return new CommonResolution(
                new ActionResolutionId(reader.ReadString()), new CardInstanceId(reader.ReadString()),
                ReadDefinitionReference(reader), new ActionDefinitionId(reader.ReadString()),
                new HeroId(reader.ReadString()),
                SnapshotEnumReader.Read<ActionInvocationContext>(reader, "resolution invocation context"),
                new ActionResolutionStepId(reader.ReadString()));
        }

        private static void WritePrintedValue(BinaryWriter writer, ValueResult value)
        {
            writer.Write(value != null);
            if (value == null) return;
            writer.Write((int)value.Channel);
            writer.Write(value.Contributions.Count);
            foreach (var contribution in value.Contributions)
            {
                writer.Write((int)contribution.Kind);
                writer.Write(contribution.HasSourceMinionId
                    ? contribution.SourceMinionId.Value
                    : contribution.HasSourceActiveEffectId
                        ? contribution.SourceActiveEffectId.Value
                        : contribution.SourceCardInstanceId.Value);
                writer.Write(contribution.HasSourceActionDefinitionId);
                if (contribution.HasSourceActionDefinitionId) writer.Write(contribution.SourceActionDefinitionId.Value);
                writer.Write((int)contribution.Channel); writer.Write(contribution.Amount);
            }
        }

        private static ValueResult ReadPrintedValue(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var channel = SnapshotEnumReader.Read<PrintedValueChannel>(reader, "printed value channel");
            var contributions = ReadMany(reader, "printed contributions", () =>
            {
                var kind = SnapshotEnumReader.Read<ValueContributionKind>(reader, "value contribution kind");
                var card = new CardInstanceId(reader.ReadString());
                var hasAction = reader.ReadBoolean();
                var action = hasAction ? new ActionDefinitionId(reader.ReadString()) : default;
                var contributionChannel = SnapshotEnumReader.Read<PrintedValueChannel>(reader, "value contribution channel");
                var amount = reader.ReadInt32();
                if (kind == ValueContributionKind.MinionDefenseModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("A minion Defense contribution cannot reference a card action.");
                    }

                    return new ValueContribution(
                        kind,
                        new MinionId(card.Value),
                        contributionChannel,
                        amount);
                }
                if (kind == ValueContributionKind.ActiveEffectModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("An Active-effect contribution cannot reference a card action.");
                    }

                    return new ValueContribution(
                        kind,
                        new ActiveEffectId(card.Value),
                        contributionChannel,
                        amount);
                }
                return hasAction
                    ? new ValueContribution(kind, card, action, contributionChannel, amount)
                    : new ValueContribution(kind, card, contributionChannel, amount);
            });
            return new ValueResult(channel, contributions);
        }

        private static void WriteActionOffer(BinaryWriter writer, ActionOffer offer)
        {
            writer.Write(offer.SourceCardInstanceId.Value); WriteDefinitionReference(writer, offer.SourceDefinitionReference);
            writer.Write(offer.ActionDefinitionId.Value); writer.Write(offer.ActorId.Value);
            writer.Write((int)offer.InvocationContext); writer.Write(offer.IsAvailable); writer.Write((int)offer.AttackRange);
            writer.Write(offer.SourceCardStateTransition.IsDefined);
            if (offer.SourceCardStateTransition.IsDefined)
            {
                writer.Write((int)offer.SourceCardStateTransition.RequiredLocation);
                writer.Write((int)offer.SourceCardStateTransition.DestinationLocation);
            }
        }

        private static ActionOffer ReadActionOffer(BinaryReader reader)
        {
            var card = new CardInstanceId(reader.ReadString()); var reference = ReadDefinitionReference(reader);
            var action = new ActionDefinitionId(reader.ReadString()); var actor = new HeroId(reader.ReadString());
            var context = SnapshotEnumReader.Read<ActionInvocationContext>(reader, "action offer invocation context");
            var available = reader.ReadBoolean();
            var range = SnapshotEnumReader.Read<AttackRangeKind>(reader, "action offer attack range");
            var hasTransition = reader.ReadBoolean();
            var transition = hasTransition
                ? new ActionCardStateTransition(
                    SnapshotEnumReader.Read<CardLocation>(reader, "transition required location"),
                    SnapshotEnumReader.Read<CardLocation>(reader, "transition destination location"))
                : default;
            return new ActionOffer(card, reference, action, actor, context, available, range, transition);
        }

        private static void WriteSlots(BinaryWriter writer, IReadOnlyList<RoundTurnSlot> slots)
        {
            writer.Write(slots.Count);
            foreach (var slot in slots)
            {
                writer.Write(slot.RoundIndex); writer.Write(slot.TurnIndex); writer.Write(slot.Assignments.Count);
                foreach (var assignment in slot.Assignments)
                {
                    writer.Write(assignment.HeroId.Value); writer.Write(assignment.CardInstanceId.HasValue);
                    if (assignment.CardInstanceId.HasValue) writer.Write(assignment.CardInstanceId.Value.Value);
                    writer.Write(assignment.PassedWithEmptyHand);
                }
            }
        }

        private static RoundTurnSlot[] ReadSlots(BinaryReader reader, IReadOnlyList<CardInstance> cards, MatchParticipants participants)
        {
            return ReadMany(reader, "round slots", () =>
            {
                var round = reader.ReadInt32(); var turn = reader.ReadInt32();
                var assignments = ReadMany(reader, "turn assignments", () =>
                {
                    var hero = new HeroId(reader.ReadString());
                    var card = reader.ReadBoolean() ? (CardInstanceId?)new CardInstanceId(reader.ReadString()) : null;
                    return new TurnCardAssignment(hero, card, reader.ReadBoolean());
                });
                return new RoundTurnSlot(round, turn, assignments, cards, participants);
            });
        }

        private static void WriteOutcomes(BinaryWriter writer, IReadOnlyList<EndOfRoundStageOutcome> outcomes)
        {
            writer.Write(outcomes.Count);
            foreach (var outcome in outcomes) { writer.Write((int)outcome.Stage); writer.Write((int)outcome.Kind); }
        }

        private static EndOfRoundStageOutcome[] ReadOutcomes(BinaryReader reader)
        {
            return ReadMany(reader, "end-of-round outcomes", () => new EndOfRoundStageOutcome(
                SnapshotEnumReader.Read<EndOfRoundStage>(reader, "end-of-round outcome stage"),
                SnapshotEnumReader.Read<EndOfRoundStageOutcomeKind>(reader, "end-of-round outcome kind")));
        }

        private static void WriteDefinitionReference(BinaryWriter writer, CardDefinitionReference reference)
        { writer.Write(reference.DefinitionId.Value); writer.Write(reference.ContentRevision); }
        private static CardDefinitionReference ReadDefinitionReference(BinaryReader reader)
        { return new CardDefinitionReference(new CardDefinitionId(reader.ReadString()), reader.ReadString()); }
        private static void WriteCoordinate(BinaryWriter writer, HexCoordinate coordinate)
        { writer.Write(coordinate.Q); writer.Write(coordinate.R); }
        private static HexCoordinate ReadCoordinate(BinaryReader reader)
        { return new HexCoordinate(reader.ReadInt32(), reader.ReadInt32()); }
        private static void WriteCoordinates(BinaryWriter writer, IEnumerable<HexCoordinate> values)
        { var array = values.ToArray(); writer.Write(array.Length); foreach (var value in array) WriteCoordinate(writer, value); }
        private static HexCoordinate[] ReadCoordinates(BinaryReader reader, string name)
        { return ReadMany(reader, name, () => ReadCoordinate(reader)); }
        private static void WriteStrings(BinaryWriter writer, IEnumerable<string> values)
        { var array = values.ToArray(); writer.Write(array.Length); foreach (var value in array) writer.Write(value); }
        private static string[] ReadStrings(BinaryReader reader, string name)
        { return ReadMany(reader, name, reader.ReadString); }
        private static T[] ReadMany<T>(BinaryReader reader, string name, Func<T> read)
        { var count = ReadCount(reader, name); var values = new T[count]; for (var i = 0; i < count; i++) values[i] = read(); return values; }
        private static int ReadCount(BinaryReader reader, string name)
        { var count = reader.ReadInt32(); if (count < 0 || count > 100000) throw Malformed($"Invalid {name} count."); return count; }
        private static SnapshotLoadException Malformed(string message)
        { return new SnapshotLoadException(SnapshotLoadRejectionReason.MalformedPayload, message); }

        private readonly struct CommonResolution
        {
            public CommonResolution(ActionResolutionId resolutionId, CardInstanceId cardId,
                CardDefinitionReference definitionReference, ActionDefinitionId actionId, HeroId actorId,
                ActionInvocationContext invocationContext, ActionResolutionStepId stepId)
            { ResolutionId = resolutionId; CardId = cardId; DefinitionReference = definitionReference; ActionId = actionId; ActorId = actorId; InvocationContext = invocationContext; StepId = stepId; }
            public ActionResolutionId ResolutionId { get; }
            public CardInstanceId CardId { get; }
            public CardDefinitionReference DefinitionReference { get; }
            public ActionDefinitionId ActionId { get; }
            public HeroId ActorId { get; }
            public ActionInvocationContext InvocationContext { get; }
            public ActionResolutionStepId StepId { get; }
        }
    }
}
