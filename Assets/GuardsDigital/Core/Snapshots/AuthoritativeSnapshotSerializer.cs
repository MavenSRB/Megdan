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
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Snapshots
{
    public enum SnapshotLoadRejectionReason
    {
        None = 0,
        MalformedPayload = 1,
        UnsupportedSchemaVersion = 2,
        CardDefinitionContentRevisionMismatch = 3,
        MapDefinitionMissing = 4,
        MapDefinitionContentRevisionMismatch = 5
    }

    public sealed class SnapshotLoadException : Exception
    {
        public SnapshotLoadException(SnapshotLoadRejectionReason reason, string message, Exception inner = null)
            : base(message, inner)
        {
            Reason = reason;
        }

        public SnapshotLoadRejectionReason Reason { get; }
    }

    public sealed class RestoredAuthoritativeMatch
    {
        internal RestoredAuthoritativeMatch(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IReadOnlyList<CardInstance> cards)
        {
            Round = round;
            Board = board;
            Participants = participants;
            Cards = cards;
            CardActions = round.CardActions;
        }

        public RoundResolutionAuthority Round { get; }
        public AdjacentMoveAuthority Board { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public CardActionResolutionAuthority CardActions { get; }
    }

    public static class AuthoritativeSnapshotSerializer
    {
        private const string Magic = "GUARDS-AUTHORITATIVE-SNAPSHOT";
        public const int CurrentSchemaVersion = 2;

        public static string Serialize(RoundResolutionAuthority round, AdjacentMoveAuthority board)
        {
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (board == null) throw new ArgumentNullException(nameof(board));

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
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static RestoredAuthoritativeMatch Deserialize(
            string payload,
            IEnumerable<CardDefinition> currentDefinitions,
            IEnumerable<BoardMapDefinition> currentMapDefinitions)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload must not be blank.");
            }

            try
            {
                var bytes = Convert.FromBase64String(payload);
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
                    var board = ReadBoard(reader, mapCatalog);
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
                (TeamSide)reader.ReadInt32()));
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
            writer.Write(board.SelectedMap.MapId.Value);
            writer.Write(board.SelectedMap.ContentRevision);
            writer.Write(board.Occupancies.Count);
            foreach (var occupancy in board.Occupancies.OrderBy(value => value.HeroId.Value, StringComparer.Ordinal))
            {
                writer.Write(occupancy.HeroId.Value);
                WriteCoordinate(writer, occupancy.Coordinate);
            }

            writer.Write(board.ObstacleOccupancies.Count);
            foreach (var occupancy in board.ObstacleOccupancies.OrderBy(value => value.ObstacleId.Value, StringComparer.Ordinal))
            {
                writer.Write(occupancy.ObstacleId.Value);
                WriteCoordinate(writer, occupancy.Coordinate);
            }
        }

        private static AdjacentMoveAuthority ReadBoard(BinaryReader reader, BoardMapCatalog mapCatalog)
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
            return new AdjacentMoveAuthority(mapDefinition, heroes, obstacles);
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
                        (CardLocation)reader.ReadInt32(),
                        reader.ReadBoolean(),
                        (CardResolutionStatus)reader.ReadInt32(),
                        (CardFaceOrientation)reader.ReadInt32()));
            });
        }

        private static void WriteRound(BinaryWriter writer, RoundResolutionAuthority round)
        {
            writer.Write(round.RoundIndex);
            writer.Write(round.TurnIndex);
            writer.Write((int)round.Phase);
            writer.Write((int)round.TieBreakerState.RevealedSide);
            WriteSlots(writer, round.TurnSlots);
            writer.Write(round.CompletedRounds.Count);
            foreach (var completed in round.CompletedRounds)
            {
                writer.Write(completed.RoundIndex);
                WriteSlots(writer, completed.TurnSlots);
                WriteOutcomes(writer, completed.EndOfRoundOutcomes);
            }

            writer.Write(round.CurrentEndOfRoundStage.HasValue);
            if (round.CurrentEndOfRoundStage.HasValue) writer.Write((int)round.CurrentEndOfRoundStage.Value);
            WriteOutcomes(writer, round.EndOfRoundOutcomes);

            writer.Write(round.CurrentCommitmentAuthority != null);
            if (round.CurrentCommitmentAuthority != null) WriteCommitment(writer, round.CurrentCommitmentAuthority);
            writer.Write(round.CurrentTurnAuthority != null);
            if (round.CurrentTurnAuthority != null) WriteTurn(writer, round.CurrentTurnAuthority);
        }

        private static RoundResolutionAuthority ReadRound(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            AdjacentMoveAuthority board)
        {
            var roundIndex = reader.ReadInt32();
            var turnIndex = reader.ReadInt32();
            var phase = (RoundResolutionPhase)reader.ReadInt32();
            var tieBreaker = new TieBreakerState((TeamSide)reader.ReadInt32());
            var slots = ReadSlots(reader, cards, participants);
            var completed = ReadMany(reader, "completed rounds", () =>
            {
                var completedIndex = reader.ReadInt32();
                return new CompletedRoundRecord(
                    completedIndex,
                    ReadSlots(reader, cards, participants),
                    ReadOutcomes(reader));
            });
            var hasStage = reader.ReadBoolean();
            var stage = hasStage ? (EndOfRoundStage?)(EndOfRoundStage)reader.ReadInt32() : null;
            var outcomes = ReadOutcomes(reader);

            TurnCardCommitmentAuthority commitment = null;
            if (reader.ReadBoolean()) commitment = ReadCommitment(reader, cards, participants);

            CardActionResolutionAuthority cardActions = null;
            TurnResolutionAuthority turn = null;
            if (reader.ReadBoolean())
            {
                turn = ReadTurn(reader, definitions, cards, participants, board, out cardActions);
            }

            return RoundResolutionAuthority.Restore(
                definitions,
                cards,
                participants,
                tieBreaker,
                roundIndex,
                turnIndex,
                phase,
                slots,
                completed,
                stage,
                outcomes,
                commitment,
                turn,
                cardActions);
        }

        private static void WriteCommitment(BinaryWriter writer, TurnCardCommitmentAuthority authority)
        {
            writer.Write(authority.IsRevealed);
            writer.Write(authority.CommittedCards.Count);
            foreach (var pair in authority.CommittedCards.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
            {
                writer.Write(pair.Key.Value);
                writer.Write(pair.Value.Value);
            }
            writer.Write(authority.PassedHeroes.Count);
            foreach (var hero in authority.PassedHeroes.OrderBy(hero => hero.Value, StringComparer.Ordinal))
            {
                writer.Write(hero.Value);
            }
        }

        private static TurnCardCommitmentAuthority ReadCommitment(
            BinaryReader reader,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants)
        {
            var revealed = reader.ReadBoolean();
            var commitments = ReadMany(reader, "commitments", () => new KeyValuePair<HeroId, CardInstanceId>(
                new HeroId(reader.ReadString()),
                new CardInstanceId(reader.ReadString())));
            var passes = ReadMany(reader, "passes", () => new HeroId(reader.ReadString()));
            return TurnCardCommitmentAuthority.Restore(cards, participants, commitments, passes, revealed);
        }

        private static void WriteTurn(BinaryWriter writer, TurnResolutionAuthority turn)
        {
            writer.Write(turn.IsStarted);
            writer.Write(turn.IsEnded);
            writer.Write((int)turn.TieBreakerState.RevealedSide);
            WritePendingInitiativeChoice(writer, turn.PendingChoice);
            WriteSelectedActor(writer, turn.SelectedActor);
            WriteCardAction(writer, turn.CardActions);
        }

        private static TurnResolutionAuthority ReadTurn(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            out CardActionResolutionAuthority cardActions)
        {
            var started = reader.ReadBoolean();
            var ended = reader.ReadBoolean();
            var tieBreaker = new TieBreakerState((TeamSide)reader.ReadInt32());
            var pendingChoice = ReadPendingInitiativeChoice(reader);
            var selectedActor = ReadSelectedActor(reader);
            cardActions = ReadCardAction(reader, board, definitions, cards, participants);
            return TurnResolutionAuthority.Restore(
                definitions,
                cards,
                participants,
                cardActions,
                started,
                ended,
                tieBreaker,
                pendingChoice,
                selectedActor);
        }

        private static void WritePendingInitiativeChoice(BinaryWriter writer, PendingInitiativeChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null) return;
            writer.Write((int)choice.OwningSide);
            writer.Write(choice.InitiativeTotal);
            writer.Write(choice.FlipTieBreakerAfterResolution);
            WriteStrings(writer, choice.CandidateCardInstanceIds.Select(value => value.Value));
        }

        private static PendingInitiativeChoice ReadPendingInitiativeChoice(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var side = (TeamSide)reader.ReadInt32();
            var total = reader.ReadInt32();
            var flip = reader.ReadBoolean();
            var cards = ReadStrings(reader, "Initiative candidates").Select(value => new CardInstanceId(value));
            return new PendingInitiativeChoice(side, total, cards, flip);
        }

        private static void WriteSelectedActor(BinaryWriter writer, SelectedInitiativeActor actor)
        {
            writer.Write(actor.IsDefined);
            if (!actor.IsDefined) return;
            writer.Write(actor.CardInstanceId.Value);
            writer.Write(actor.ActorId.Value);
            writer.Write((int)actor.TeamSide);
            writer.Write(actor.InitiativeTotal);
            writer.Write(actor.FlipTieBreakerAfterResolution);
        }

        private static SelectedInitiativeActor ReadSelectedActor(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return default;
            return new SelectedInitiativeActor(
                new CardInstanceId(reader.ReadString()),
                new HeroId(reader.ReadString()),
                (TeamSide)reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean());
        }

        private static void WriteCardAction(BinaryWriter writer, CardActionResolutionAuthority authority)
        {
            var kind = authority == null ? -1 : authority.PendingResolution != null ? 1 : authority.PendingAttackResolution != null ? 2 : 0;
            writer.Write(kind);
            if (kind == 1) WriteMovementResolution(writer, authority.PendingResolution);
            if (kind == 2) WriteAttackResolution(writer, authority.PendingAttackResolution);
        }

        private static CardActionResolutionAuthority ReadCardAction(
            BinaryReader reader,
            AdjacentMoveAuthority board,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants)
        {
            var kind = reader.ReadInt32();
            if (kind == -1) return null;
            if (kind == 0) return new CardActionResolutionAuthority(board);
            if (kind == 1)
            {
                var pending = ReadMovementResolution(reader);
                return CardActionResolutionAuthority.Restore(board, pending);
            }
            if (kind == 2)
            {
                var pending = ReadAttackResolution(reader);
                return CardActionResolutionAuthority.RestoreAttack(
                    board, pending, definitions, cards, participants);
            }
            throw Malformed("Unknown pending card-action kind.");
        }

        private static void WriteMovementResolution(BinaryWriter writer, PendingCardActionResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            var state = pending.MovementSessionState;
            writer.Write(state.MovementAllowance);
            writer.Write(state.RemainingAllowance);
            writer.Write(state.IsComplete);
            WriteCoordinates(writer, state.Route);
        }

        private static PendingCardActionResolution ReadMovementResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var allowance = reader.ReadInt32();
            var remaining = reader.ReadInt32();
            var complete = reader.ReadBoolean();
            var route = ReadCoordinates(reader, "movement route");
            var events = new List<AdjacentMoveEvent>();
            for (var index = 0; index < route.Length - 1; index++)
                events.Add(new AdjacentMoveEvent(common.ActorId, route[index], route[index + 1]));
            return new PendingCardActionResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId,
                new MovementSessionState(common.ActorId, allowance, remaining, complete, route, events));
        }

        private static void WriteAttackResolution(BinaryWriter writer, PendingAttackResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            writer.Write((int)pending.Phase);
            writer.Write(pending.TargetHeroId.IsDefined);
            if (pending.TargetHeroId.IsDefined) writer.Write(pending.TargetHeroId.Value);
            WritePrintedValue(writer, pending.AttackValue);
            writer.Write(pending.DefenseOffer.HasValue);
            if (pending.DefenseOffer.HasValue) WriteActionOffer(writer, pending.DefenseOffer.Value);
            WritePrintedValue(writer, pending.DefenseValue);
        }

        private static PendingAttackResolution ReadAttackResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var phase = (AttackResolutionPhase)reader.ReadInt32();
            var target = reader.ReadBoolean() ? new HeroId(reader.ReadString()) : default;
            var attackValue = ReadPrintedValue(reader);
            var defenseOffer = reader.ReadBoolean() ? (ActionOffer?)ReadActionOffer(reader) : null;
            var defenseValue = ReadPrintedValue(reader);
            return new PendingAttackResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId, phase, target,
                attackValue, defenseOffer, defenseValue);
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
                new HeroId(reader.ReadString()), (ActionInvocationContext)reader.ReadInt32(),
                new ActionResolutionStepId(reader.ReadString()));
        }

        private static void WritePrintedValue(BinaryWriter writer, PrintedValueResult value)
        {
            writer.Write(value != null);
            if (value == null) return;
            writer.Write((int)value.Channel);
            writer.Write(value.Contributions.Count);
            foreach (var contribution in value.Contributions)
            {
                writer.Write((int)contribution.Kind); writer.Write(contribution.SourceCardInstanceId.Value);
                writer.Write(contribution.HasSourceActionDefinitionId);
                if (contribution.HasSourceActionDefinitionId) writer.Write(contribution.SourceActionDefinitionId.Value);
                writer.Write((int)contribution.Channel); writer.Write(contribution.Amount);
            }
        }

        private static PrintedValueResult ReadPrintedValue(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var channel = (PrintedValueChannel)reader.ReadInt32();
            var contributions = ReadMany(reader, "printed contributions", () =>
            {
                var kind = (PrintedValueContributionKind)reader.ReadInt32();
                var card = new CardInstanceId(reader.ReadString());
                var hasAction = reader.ReadBoolean();
                var action = hasAction ? new ActionDefinitionId(reader.ReadString()) : default;
                var contributionChannel = (PrintedValueChannel)reader.ReadInt32();
                var amount = reader.ReadInt32();
                return hasAction
                    ? new PrintedValueContribution(kind, card, action, contributionChannel, amount)
                    : new PrintedValueContribution(kind, card, contributionChannel, amount);
            });
            return new PrintedValueResult(channel, contributions);
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
            var context = (ActionInvocationContext)reader.ReadInt32(); var available = reader.ReadBoolean();
            var range = (AttackRangeKind)reader.ReadInt32(); var hasTransition = reader.ReadBoolean();
            var transition = hasTransition
                ? new ActionCardStateTransition((CardLocation)reader.ReadInt32(), (CardLocation)reader.ReadInt32())
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
                (EndOfRoundStage)reader.ReadInt32(), (EndOfRoundStageOutcomeKind)reader.ReadInt32()));
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
