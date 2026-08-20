using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
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
        // v20: milestone 7.4 puts every team's remaining Life counters in the round section, immediately
        // after the items - one count-prefixed team id and counter total per participating team. It is
        // the first milestone in which a Life counter can change: p.13's "Defeated hero's team spends
        // the number of Life counters, equal to that hero's lowest card Tier" is the only rule that
        // moves one, and until this milestone the count existed solely as the setup constant
        // `QuickGameTeamSetupState.LifeCount` that the HUD renderer printed.
        // v19 is rejected rather than migrated, and here the reason is the sharpest of the series: a
        // v19 payload records no counters, and the only reading available for their absence is "nobody
        // has spent any", which is a statement about a resource whose exhaustion loses the game. It is
        // right for every payload a v19 build could produce, because no v19 rule spends one - and that
        // is precisely why it must not be written into the reader, since the first payload it would be
        // wrong about is the first one this milestone writes, and a restored match would come back with
        // a full pile and a team that had already lost. No persisted state exists outside the
        // version-controlled corpus to strand.
        //
        // v19: milestone 7.2a puts every hero's four rulebook p.16 item slots in the setup section,
        // beside the hero id and player id already there, and splits an item's single slot field into
        // the slot it occupies and the icon it supplies. The user chose the wire over rebinding the
        // slots from configuration at restore, so a restore refuses a build whose dashboards differ the
        // way it already refuses one whose card content differs; the refusal is its own reason,
        // HeroDashboardMismatch, rather than a malformed payload, because the bytes parse and describe
        // content this build does not have.
        // v18 is rejected rather than migrated, and here the reason is not that the absence is
        // unreadable - a v18 payload has no dashboards at all, and every hero this build supplies has
        // the same four slots, so reading one as "p.16's layout" would be right for every payload that
        // exists. It is refused for the reason v17 and v16 were: the guess is a statement about heroes
        // the payload never made, and the first payload it would be wrong about is the first one a
        // verified hero with a different dashboard writes, by which time the migration would look
        // established. The item split is the separable second cause and is unmigratable in the
        // ordinary way: a v18 item names a slot by its icon, which cannot say which of the fourth
        // slot's three icons it supplies. No persisted state exists outside the version-controlled
        // corpus to strand.
        //
        // v18: milestone 7.2 adds every hero's items to the round section, immediately after the Levels
        // - one count-prefixed list of owner, source card instance, slot icon and order. v17 is rejected
        // rather than migrated, and the reason is not the shape of the absence but what the absence
        // would have to be read as. A v17 payload records no items at all, and reading one as "nobody
        // has any" is a guess about state the page makes authoritative the moment a hero levels: p.16
        // gives an item on every ordinary level purchase, so a match restored from a v17 payload written
        // after milestone 7.5 exists would come back with heroes whose dashboards had been emptied.
        // That the guess is right for every payload this build can produce - nothing here gains an item,
        // since the purchase is 7.5's - is exactly why it must not be written into the reader, which is
        // the same argument v17 made for Level one milestone earlier. No persisted state exists outside
        // the version-controlled corpus to strand.
        //
        // v17: milestone 7.1 adds every hero's current Level to the round section, beside the paused
        // system transition - one count-prefixed hero id and Level per participant. v16 is rejected
        // rather than migrated, and the reason is the same one v14 gave for the Battle Zone rather
        // than the weaker "we have one read path per version": a v16 payload records no Level at all,
        // and reading one as "everybody is at Level 1" would be a guess about a value the page makes
        // authoritative from its first sentence. It is a guess that happens to be right for every
        // payload this build can produce, because milestone 7.1 adds no way to change a Level, and
        // that is exactly why it must not be written into the reader: the first payload it would be
        // wrong about is the first one milestone 7.5 writes, and by then the migration would look
        // established. No persisted state exists outside the version-controlled corpus to strand.
        //
        // v15: milestone 6.10a adds the system-transition continuation to the round section - one
        // boolean saying whether a multi-step transition is stopped, and when it is, which transition,
        // which step, who chooses, what they may choose, and which steps have already happened. v14 is
        // rejected rather than migrated: a v14 payload has no field for a paused transition, and
        // reading one as "nothing is running" would restore a match that had silently skipped a step a
        // player still owes. What the steps mean stays in the trusted catalog, so a payload naming a
        // transition this build no longer supplies is refused rather than reinterpreted.
        //
        // v14: milestone 6.10 puts rulebook p.8's Battle Zone on the wire, beside the map reference it
        // is scoped to, because it is now authoritative state the Lane-Push Zone stage advances rather
        // than a derivation from the map's starting-Battle-Zone flag. v13 is rejected rather than
        // migrated, and here the reason is unusually concrete: a v13 payload records where every piece
        // stands and nothing about which Zone the Lane has reached, and no derivation can recover it -
        // an emptied Battle Zone and a Zone the Lane never reached look identical in occupancy, and the
        // flag names only where the match began. Reading a v13 payload as "still the starting Zone"
        // would answer the Minion Battle, the Heavy-immunity accompaniment, the displaced-minion
        // correction and the Lane-Push cause about the wrong Zone for the rest of the match.
        //
        // v12: milestone 6.7's bounded Repeat adds an optional pending repeated-action record to the
        // card-action section's Correction continuation, naming the second instance an action asked for
        // while the correction between the two instances is paused. v11 is rejected rather than
        // migrated: a v11 correction payload cannot state whether the action it belongs to asked to
        // repeat, and reading one as "did not" would silently drop half of a card's printed text.
        //
        // v11: milestone 6.5aa widens Minion Battle candidates and selections from intrinsic minion
        // IDs to stable effective-unit identities, so a counts-as-minion hero can be removed and the
        // resulting off-board hero occupancy can survive restore. v10 is rejected rather than
        // migrated because it cannot encode the widened pending continuation.
        //
        // v10: milestone 6.4a's paused after-action displaced-minion correction joins the card-action
        // section as continuation kind 4, carrying the owning action's identity, the stage, the
        // deciding team, and candidate identities only - routes, costs, and coordinates of subjects are
        // never on the wire, because the correction is a positional predicate the restore re-derives
        // from the board and the Tie Breaker, refusing any payload the derivation does not reproduce.
        // v9 is rejected rather than migrated, per the standing decision: a v9 payload could not
        // represent a paused correction, the engine has one read path per version, and no persisted
        // state exists outside the version-controlled corpus to strand.
        //
        // v9: placed tokens join the board section and attached markers join the round section, each
        // carrying identity, provenance and position only; supply size, display name and marker return
        // condition come from the registered component catalog, which the payload also records so a
        // restore can refuse a build whose component content differs. v8 is explicitly rejected rather
        // than migrated (milestone 6.2 migrate-or-reject decision); v8 rejected v7 because the
        // Active-effect section left semantics on the wire, and v7 rejected v6 for the analogous
        // reason at milestone 6.1s.
        // v21: which heroes bought a level in the current round joins the round section, after the Life
        // counters. Milestone 7.6's Pity Coin step reads the negation of it - rulebook p.8 pays "All
        // players who have NOT leveled up this round" - and that step is a stage later than the Level Up
        // that produces the answer, so a match saved between the two has to carry it. v20 is rejected
        // rather than migrated: the only reading available for the section's absence is "nobody levelled
        // this round", which is right for every payload a v20 build could produce, because no v20 rule
        // records one, and wrong for the first payload this milestone writes - a hero that had already
        // levelled would come back eligible and collect a coin the page denies it.
        public const int CurrentSchemaVersion = 21;

        /// <summary>
        /// Instrumentation, not production state. Milestone 6.1i asserts the exact number of full
        /// state serializations an accepted and a rejected command perform, which finding `F613-08`
        /// requires to be measured rather than inspected. The project is single-threaded, so a plain
        /// counter is sufficient; a fixture resets it and reads it around a known command.
        /// </summary>
        public static long SerializeCallCount { get; private set; }

        /// <summary>Total encoded characters produced. See the envelope's counterpart.</summary>
        /// <remarks>
        /// Since milestone 7.9b this counts payload materializations rather than captures: a
        /// capture writes bytes and encodes nothing, and the accepted-command path takes exactly
        /// one capture and materializes no payload at all. <see cref="CapturedByteCount"/> is the
        /// counter that sees what a capture writes.
        /// </remarks>
        public static long SerializedCharacterCount { get; private set; }

        /// <summary>
        /// Total bytes a capture actually wrote: the two current-state spans, plus each completed
        /// round's fragment once, when it first freezes. This is the per-command serialization cost
        /// under milestone 7.9b's capture, so it is what the diagnostic growth fixture measures.
        /// </summary>
        public static long CapturedByteCount { get; private set; }

        public static void ResetSerializeCallCount()
        {
            SerializeCallCount = 0;
            SerializedCharacterCount = 0;
            CapturedByteCount = 0;
        }

        internal static void CountMaterializedPayload(int encodedCharacters)
        {
            SerializedCharacterCount += encodedCharacters;
        }

        internal static void CountCapturedBytes(int bytes)
        {
            CapturedByteCount += bytes;
        }

        public static string Serialize(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board,
            EffectCapabilityCatalog capabilities = null,
            ComponentDefinitionCatalog components = null)
        {
            return Capture(round, board, null, capabilities, components).Payload;
        }

        /// <summary>
        /// One authoritative serialization as its parts: the current state written fresh, and the
        /// completed-round history shared from <paramref name="completedRoundsCache"/> rather than
        /// re-encoded. The capture's <see cref="AuthoritativeSnapshotCapture.Payload"/> is
        /// byte-identical to <see cref="Serialize"/>, which runs through this method itself, so
        /// there is exactly one write path for the format.
        /// </summary>
        /// <remarks>
        /// Milestone 7.9b. This is what makes the per-command rollback pre-image cost independent
        /// of match length: current state is bounded, so writing it is flat, while the history the
        /// payload must still carry grows every round and is written once per round instead of
        /// once per command. A null cache serves one capture at full cost.
        /// </remarks>
        public static AuthoritativeSnapshotCapture Capture(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board,
            CompletedRoundsSerializationCache completedRoundsCache,
            EffectCapabilityCatalog capabilities = null,
            ComponentDefinitionCatalog components = null)
        {
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (board == null) throw new ArgumentNullException(nameof(board));

            SerializeCallCount++;
            var cache = completedRoundsCache ?? new CompletedRoundsSerializationCache();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(CurrentSchemaVersion);
                WriteDefinitions(writer, round.Definitions);
                WriteCapabilityRegistrations(
                    writer, capabilities ?? SyntheticEffectCapabilities.CreateCatalog());
                WriteComponentRegistrations(
                    writer, components ?? SyntheticComponentDefinitions.CreateCatalog());
                WriteParticipants(writer, round.Participants);
                WriteBoard(writer, board);
                WriteCards(writer, round.Instances);

                var state = round.CaptureRestoreState();
                WriteRoundOpening(writer, state);
                writer.Flush();
                var prefixLength = checked((int)stream.Position);
                WriteRoundRemainder(writer, round, state);
                writer.Flush();

                var buffer = stream.GetBuffer();
                var total = checked((int)stream.Length);
                var prefix = new byte[prefixLength];
                Array.Copy(buffer, prefix, prefixLength);
                var suffix = new byte[total - prefixLength];
                Array.Copy(buffer, prefixLength, suffix, 0, suffix.Length);
                CountCapturedBytes(total);

                return new AuthoritativeSnapshotCapture(
                    prefix,
                    cache.GetFragment(state.History.CompletedRounds),
                    suffix);
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
            IEnumerable<QuickGameMinionSetupState> minions,
            EffectCapabilityCatalog capabilities = null,
            ComponentDefinitionCatalog components = null,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions = null)
        {
            // The trusted capability and component sets a restore rebinds against. Defaulted rather
            // than required so existing callers keep compiling; the configured session always supplies
            // the real ones.
            capabilities = capabilities ?? SyntheticEffectCapabilities.CreateCatalog();
            components = components ?? SyntheticComponentDefinitions.CreateCatalog();
            // Milestone 6.11 widened this default from the synthetic transition alone to the set this
            // build actually supplies, because a real one joined it: a restore that rebound against the
            // synthetic catalog refused a paused Lane Push as a transition this build no longer has,
            // which is the right refusal for the wrong reason.
            transitions = transitions ??
                GuardsDigital.Core.Rules.Transitions.MatchSystemTransitions.CreateCatalog();
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
                    ValidateCapabilityRegistrations(reader, capabilities);
                    ValidateComponentRegistrations(reader, components);
                    var participants = ReadParticipants(reader);
                    var board = ReadBoard(reader, mapCatalog, minions, components);
                    var cards = ReadCards(reader, definitions, participants);
                    var round = ReadRound(
                        reader, definitions, cards, participants, board, capabilities, components,
                        transitions);
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
                        : contribution.HasSourceContinuousSourceId
                            ? contribution.SourceContinuousSourceId
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
                if (kind == ValueContributionKind.ContinuousSourceModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("A continuous-source contribution cannot reference a card action.");
                    }

                    return ValueContribution.FromContinuousSource(
                        card.Value,
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
