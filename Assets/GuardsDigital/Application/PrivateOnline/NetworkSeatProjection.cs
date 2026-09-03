using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.32. One seat's projection as it crosses the wire: a bounded data-transfer tree,
    /// decoded field by field, never a serialized object graph.
    /// </summary>
    /// <remarks>
    /// <para>The encoder's input is <see cref="SeatProjection"/> - the owner-scoped view milestones
    /// 8.30a-8.30c proved - and it writes only the fields this record declares, so what is absent
    /// from the seat's view is structurally absent from the bytes: another hero's hand and deck
    /// never had a field to travel in, and no client-side hiding is involved. That is acceptance
    /// check four of the milestone, and the authorization fixtures prove it over captured
    /// streams.</para>
    /// <para>The 8.32 slice carries the seat's complete <b>acting</b> surface - identity, version,
    /// the public board and round state, the seat's own and the public cards, the countable hand
    /// summaries, commitments, progression, the standing decision's owner and this seat's exact
    /// answer commands - because commands are complete answers: a client holding this record can
    /// answer everything the host would accept from it. Milestone 8.33's <see cref="Detail"/> pays
    /// the deferral that slice named: the pending-choice candidate detail and previews, the
    /// resolution envelopes, the offer presentation rows, the team-agreement surface, and the
    /// completed-round history. Still absent, by decision rather than omission: the
    /// setup/map/participant/definition content the admission tuple already pins on both sides,
    /// the rules-execution continuation no client may hold, and - never - any Director-only
    /// surface.</para>
    /// </remarks>
    public sealed class NetworkSeatProjectionRecord
    {
        public NetworkSeatProjectionRecord(
            long version,
            string seatId,
            string playerId,
            string heroId,
            int teamSide,
            int roundIndex,
            int turnIndex,
            int phase,
            bool isTurnActive,
            bool isSeatUndoAvailable,
            int? endOfRoundStage,
            int tieBreakerRevealedSide,
            NetworkSelectedActorRecord selectedActor,
            NetworkOutcomeRecord outcome,
            IReadOnlyList<NetworkLifeCounterRow> teamLifeCounters,
            IReadOnlyList<NetworkHeroRow> heroes,
            IReadOnlyList<NetworkMinionRow> minions,
            IReadOnlyList<NetworkObstacleRow> obstacles,
            IReadOnlyList<NetworkTokenRow> placedTokens,
            IReadOnlyList<NetworkMarkerRow> heldMarkers,
            IReadOnlyList<NetworkTurnSlotRow> turnSlots,
            IReadOnlyList<NetworkCommitmentRow> commitments,
            IReadOnlyList<NetworkHandSummaryRow> handSummaries,
            IReadOnlyList<NetworkCardRow> ownCards,
            IReadOnlyList<NetworkCardRow> publicCards,
            IReadOnlyList<NetworkProgressionRow> heroProgression,
            IReadOnlyList<string> activeEffectSourceCardInstanceIds,
            NetworkDecisionOwnerRecord decisionOwner,
            string decisionContractFamilyId,
            IReadOnlyList<DirectorCommand> decisionContractCommands,
            IReadOnlyList<DirectorCommand> authorizedCommands,
            NetworkProjectionDetailRecord detail)
        {
            Version = version;
            SeatId = seatId;
            PlayerId = playerId;
            HeroId = heroId;
            TeamSide = teamSide;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            IsTurnActive = isTurnActive;
            IsSeatUndoAvailable = isSeatUndoAvailable;
            EndOfRoundStage = endOfRoundStage;
            TieBreakerRevealedSide = tieBreakerRevealedSide;
            SelectedActor = selectedActor;
            Outcome = outcome;
            TeamLifeCounters = teamLifeCounters;
            Heroes = heroes;
            Minions = minions;
            Obstacles = obstacles;
            PlacedTokens = placedTokens;
            HeldMarkers = heldMarkers;
            TurnSlots = turnSlots;
            Commitments = commitments;
            HandSummaries = handSummaries;
            OwnCards = ownCards;
            PublicCards = publicCards;
            HeroProgression = heroProgression;
            ActiveEffectSourceCardInstanceIds = activeEffectSourceCardInstanceIds;
            DecisionOwner = decisionOwner;
            DecisionContractFamilyId = decisionContractFamilyId;
            DecisionContractCommands = decisionContractCommands;
            AuthorizedCommands = authorizedCommands;
            Detail = detail;
        }

        public long Version { get; }
        public string SeatId { get; }
        public string PlayerId { get; }
        public string HeroId { get; }
        public int TeamSide { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public int Phase { get; }
        public bool IsTurnActive { get; }

        /// <summary>Milestone 8.35f. Whether this seat may step its own turn back one command.</summary>
        public bool IsSeatUndoAvailable { get; }
        public int? EndOfRoundStage { get; }
        public int TieBreakerRevealedSide { get; }
        public NetworkSelectedActorRecord SelectedActor { get; }
        public NetworkOutcomeRecord Outcome { get; }
        public IReadOnlyList<NetworkLifeCounterRow> TeamLifeCounters { get; }
        public IReadOnlyList<NetworkHeroRow> Heroes { get; }
        public IReadOnlyList<NetworkMinionRow> Minions { get; }
        public IReadOnlyList<NetworkObstacleRow> Obstacles { get; }
        public IReadOnlyList<NetworkTokenRow> PlacedTokens { get; }
        public IReadOnlyList<NetworkMarkerRow> HeldMarkers { get; }
        public IReadOnlyList<NetworkTurnSlotRow> TurnSlots { get; }
        public IReadOnlyList<NetworkCommitmentRow> Commitments { get; }
        public IReadOnlyList<NetworkHandSummaryRow> HandSummaries { get; }

        /// <summary>Every card this seat's own hero owns, wherever it is.</summary>
        public IReadOnlyList<NetworkCardRow> OwnCards { get; }

        /// <summary>
        /// Other heroes' cards whose identity rulebook p.4 makes open. Their hands and decks have
        /// no field here at all.
        /// </summary>
        public IReadOnlyList<NetworkCardRow> PublicCards { get; }

        public IReadOnlyList<NetworkProgressionRow> HeroProgression { get; }
        public IReadOnlyList<string> ActiveEffectSourceCardInstanceIds { get; }
        public NetworkDecisionOwnerRecord DecisionOwner { get; }

        /// <summary>The standing decision's family, and <c>null</c> unless this seat owns it.</summary>
        public string DecisionContractFamilyId { get; }

        public IReadOnlyList<DirectorCommand> DecisionContractCommands { get; }

        /// <summary>Every command this seat may submit against exactly this version.</summary>
        public IReadOnlyList<DirectorCommand> AuthorizedCommands { get; }

        /// <summary>
        /// Milestone 8.33. The presentation-fidelity half milestone 8.32 deferred by name: the
        /// pending-choice candidate detail and previews, the resolution envelopes, the offer rows,
        /// the team-agreement surface, and the completed-round history.
        /// </summary>
        public NetworkProjectionDetailRecord Detail { get; }
    }

    /// <summary>
    /// Protocol-2 selected-actor state. The final flag is rules-bearing: it preserves the
    /// authority's deferred Tie Breaker flip after the selected actor resolves.
    /// </summary>
    public sealed class NetworkSelectedActorRecord
    {
        public NetworkSelectedActorRecord(
            bool isDefined, string cardInstanceId, string actorId, int teamSide, int initiativeTotal,
            bool flipTieBreakerAfterResolution)
        {
            IsDefined = isDefined;
            CardInstanceId = cardInstanceId;
            ActorId = actorId;
            TeamSide = teamSide;
            InitiativeTotal = initiativeTotal;
            FlipTieBreakerAfterResolution = flipTieBreakerAfterResolution;
        }

        public bool IsDefined { get; }
        public string CardInstanceId { get; }
        public string ActorId { get; }
        public int TeamSide { get; }
        public int InitiativeTotal { get; }
        /// <summary>
        /// Whether completing this actor flips the Tie Breaker; added with private protocol 2.
        /// </summary>
        public bool FlipTieBreakerAfterResolution { get; }
    }

    public sealed class NetworkOutcomeRecord
    {
        public NetworkOutcomeRecord(int cause, string winningTeamId, string losingTeamId)
        {
            Cause = cause;
            WinningTeamId = winningTeamId;
            LosingTeamId = losingTeamId;
        }

        public int Cause { get; }
        public string WinningTeamId { get; }
        public string LosingTeamId { get; }
    }

    public sealed class NetworkDecisionOwnerRecord
    {
        public NetworkDecisionOwnerRecord(string familyId, string heroId, int teamSide)
        {
            FamilyId = familyId;
            HeroId = heroId;
            TeamSide = teamSide;
        }

        /// <summary>Null while nothing is pending.</summary>
        public string FamilyId { get; }

        /// <summary>The owning hero, or null for a team-owned decision.</summary>
        public string HeroId { get; }

        public int TeamSide { get; }
    }

    public sealed class NetworkLifeCounterRow
    {
        public NetworkLifeCounterRow(string teamId, int count)
        {
            TeamId = teamId;
            Count = count;
        }

        public string TeamId { get; }
        public int Count { get; }
    }

    public sealed class NetworkHeroRow
    {
        public NetworkHeroRow(string heroId, int q, int r)
        {
            HeroId = heroId;
            Q = q;
            R = r;
        }

        public string HeroId { get; }
        public int Q { get; }
        public int R { get; }
    }

    public sealed class NetworkMinionRow
    {
        public NetworkMinionRow(string minionId, string teamId, int teamSide, int minionType, int q, int r)
        {
            MinionId = minionId;
            TeamId = teamId;
            TeamSide = teamSide;
            MinionType = minionType;
            Q = q;
            R = r;
        }

        public string MinionId { get; }
        public string TeamId { get; }
        public int TeamSide { get; }
        public int MinionType { get; }
        public int Q { get; }
        public int R { get; }
    }

    public sealed class NetworkObstacleRow
    {
        public NetworkObstacleRow(string obstacleId, int q, int r)
        {
            ObstacleId = obstacleId;
            Q = q;
            R = r;
        }

        public string ObstacleId { get; }
        public int Q { get; }
        public int R { get; }
    }

    public sealed class NetworkTokenRow
    {
        public NetworkTokenRow(
            string tokenInstanceId,
            string tokenDefinitionId,
            string ownerHeroId,
            string controllerHeroId,
            string sourceCardInstanceId,
            int q,
            int r)
        {
            TokenInstanceId = tokenInstanceId;
            TokenDefinitionId = tokenDefinitionId;
            OwnerHeroId = ownerHeroId;
            ControllerHeroId = controllerHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            Q = q;
            R = r;
        }

        public string TokenInstanceId { get; }
        public string TokenDefinitionId { get; }
        public string OwnerHeroId { get; }
        public string ControllerHeroId { get; }
        public string SourceCardInstanceId { get; }
        public int Q { get; }
        public int R { get; }
    }

    public sealed class NetworkMarkerRow
    {
        public NetworkMarkerRow(
            string markerInstanceId,
            string markerDefinitionId,
            string ownerHeroId,
            string holderHeroId,
            string sourceCardInstanceId,
            string sourceActionDefinitionId)
        {
            MarkerInstanceId = markerInstanceId;
            MarkerDefinitionId = markerDefinitionId;
            OwnerHeroId = ownerHeroId;
            HolderHeroId = holderHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
        }

        public string MarkerInstanceId { get; }
        public string MarkerDefinitionId { get; }
        public string OwnerHeroId { get; }
        public string HolderHeroId { get; }
        public string SourceCardInstanceId { get; }
        public string SourceActionDefinitionId { get; }
    }

    public sealed class NetworkTurnSlotRow
    {
        public NetworkTurnSlotRow(
            int roundIndex, int turnIndex, IReadOnlyList<NetworkTurnAssignmentRow> assignments)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Assignments = assignments;
        }

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public IReadOnlyList<NetworkTurnAssignmentRow> Assignments { get; }
    }

    public sealed class NetworkTurnAssignmentRow
    {
        public NetworkTurnAssignmentRow(string heroId, string cardInstanceId, bool passedWithEmptyHand)
        {
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            PassedWithEmptyHand = passedWithEmptyHand;
        }

        public string HeroId { get; }

        /// <summary>Null for a pass; slots exist only after the reveal, so nothing here is hidden.</summary>
        public string CardInstanceId { get; }

        public bool PassedWithEmptyHand { get; }
    }

    public sealed class NetworkCommitmentRow
    {
        public NetworkCommitmentRow(string heroId, int status, bool canPass, string revealedCardInstanceId)
        {
            HeroId = heroId;
            Status = status;
            CanPass = canPass;
            RevealedCardInstanceId = revealedCardInstanceId;
        }

        public string HeroId { get; }
        public int Status { get; }
        public bool CanPass { get; }

        /// <summary>
        /// Null until the authority reveals it. A facedown commitment's identity has no field to
        /// leak through - the authority publishes it this way and the wire restates the authority.
        /// </summary>
        public string RevealedCardInstanceId { get; }
    }

    public sealed class NetworkHandSummaryRow
    {
        public NetworkHandSummaryRow(string heroId, int handCount, int deckCount)
        {
            HeroId = heroId;
            HandCount = handCount;
            DeckCount = deckCount;
        }

        public string HeroId { get; }
        public int HandCount { get; }
        public int DeckCount { get; }
    }

    public sealed class NetworkCardRow
    {
        public NetworkCardRow(
            string instanceId,
            string definitionId,
            string contentRevision,
            string ownerHeroId,
            int location,
            bool hasBeenPlayed,
            int resolutionStatus,
            int faceOrientation)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            ContentRevision = contentRevision;
            OwnerHeroId = ownerHeroId;
            Location = location;
            HasBeenPlayed = hasBeenPlayed;
            ResolutionStatus = resolutionStatus;
            FaceOrientation = faceOrientation;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string ContentRevision { get; }
        public string OwnerHeroId { get; }
        public int Location { get; }
        public bool HasBeenPlayed { get; }
        public int ResolutionStatus { get; }
        public int FaceOrientation { get; }
    }

    public sealed class NetworkProgressionRow
    {
        public NetworkProgressionRow(
            string heroId,
            int level,
            int coins,
            bool leveledThisRound,
            IReadOnlyList<KeyValuePair<int, int>> itemBonuses)
        {
            HeroId = heroId;
            Level = level;
            Coins = coins;
            LeveledThisRound = leveledThisRound;
            ItemBonuses = itemBonuses;
        }

        public string HeroId { get; }
        public int Level { get; }
        public int Coins { get; }
        public bool LeveledThisRound { get; }
        public IReadOnlyList<KeyValuePair<int, int>> ItemBonuses { get; }
    }

    /// <summary>
    /// Milestone 8.32. Writes one <see cref="SeatProjection"/> as its bounded wire record, and
    /// reads one back field by field, refusing anything out of bounds.
    /// </summary>
    public static class NetworkSeatProjectionCodec
    {
        /// <summary>The most rows any one list may declare; far above any real board, far below harm.</summary>
        public const int MaxListEntries = 1024;

        public static byte[] Encode(SeatProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(projection.Version);
                writer.Write(projection.Seat.Id.Value);
                writer.Write(projection.Seat.PlayerId.Value);
                writer.Write(projection.Seat.HeroId.Value);
                writer.Write((int)projection.Seat.TeamSide);
                writer.Write(projection.RoundIndex);
                writer.Write(projection.TurnIndex);
                writer.Write((int)projection.Phase);
                writer.Write(projection.IsTurnActive);
                writer.Write(projection.IsSeatUndoAvailable);
                writer.Write(projection.CurrentEndOfRoundStage.HasValue);
                if (projection.CurrentEndOfRoundStage.HasValue)
                {
                    writer.Write((int)projection.CurrentEndOfRoundStage.Value);
                }

                writer.Write((int)projection.TieBreakerState.RevealedSide);

                writer.Write(projection.SelectedActor.IsDefined);
                if (projection.SelectedActor.IsDefined)
                {
                    writer.Write(projection.SelectedActor.CardInstanceId.Value);
                    writer.Write(projection.SelectedActor.ActorId.Value);
                    writer.Write((int)projection.SelectedActor.TeamSide);
                    writer.Write(projection.SelectedActor.InitiativeTotal);
                    writer.Write(projection.SelectedActor.FlipTieBreakerAfterResolution);
                }

                writer.Write(projection.Outcome.HasEnded);
                if (projection.Outcome.HasEnded)
                {
                    writer.Write((int)projection.Outcome.Cause);
                    writer.Write(projection.Outcome.WinningTeamId.Value);
                    writer.Write(projection.Outcome.LosingTeamId.Value);
                }

                writer.Write(projection.TeamLifeCounters.Count);
                foreach (var counter in projection.TeamLifeCounters)
                {
                    writer.Write(counter.Key.Value);
                    writer.Write(counter.Value);
                }

                writer.Write(projection.Heroes.Count);
                foreach (var hero in projection.Heroes)
                {
                    writer.Write(hero.HeroId.Value);
                    writer.Write(hero.Coordinate.Q);
                    writer.Write(hero.Coordinate.R);
                }

                writer.Write(projection.Minions.Count);
                foreach (var minion in projection.Minions)
                {
                    writer.Write(minion.MinionId.Value);
                    writer.Write(minion.TeamId.Value);
                    writer.Write((int)minion.TeamSide);
                    writer.Write((int)minion.MinionType);
                    writer.Write(minion.Coordinate.Q);
                    writer.Write(minion.Coordinate.R);
                }

                writer.Write(projection.Obstacles.Count);
                foreach (var obstacle in projection.Obstacles)
                {
                    writer.Write(obstacle.ObstacleId.Value);
                    writer.Write(obstacle.Coordinate.Q);
                    writer.Write(obstacle.Coordinate.R);
                }

                writer.Write(projection.PlacedTokens.Count);
                foreach (var token in projection.PlacedTokens)
                {
                    writer.Write(token.Id.Value);
                    writer.Write(token.Definition.Id.Value);
                    WriteOptional(writer, token.OwnerHeroId.IsDefined, token.OwnerHeroId.Value);
                    WriteOptional(writer, token.ControllerHeroId.IsDefined, token.ControllerHeroId.Value);
                    WriteOptional(writer, token.SourceCardInstanceId.IsDefined, token.SourceCardInstanceId.Value);
                    writer.Write(token.Coordinate.Q);
                    writer.Write(token.Coordinate.R);
                }

                writer.Write(projection.HeldMarkers.Count);
                foreach (var marker in projection.HeldMarkers)
                {
                    writer.Write(marker.Id.Value);
                    writer.Write(marker.Definition.Id.Value);
                    WriteOptional(writer, marker.OwnerHeroId.IsDefined, marker.OwnerHeroId.Value);
                    WriteOptional(writer, marker.HolderHeroId.IsDefined, marker.HolderHeroId.Value);
                    WriteOptional(writer, marker.SourceCardInstanceId.IsDefined, marker.SourceCardInstanceId.Value);
                    WriteOptional(writer, marker.SourceActionDefinitionId.IsDefined, marker.SourceActionDefinitionId.Value);
                }

                writer.Write(projection.TurnSlots.Count);
                foreach (var slot in projection.TurnSlots)
                {
                    writer.Write(slot.RoundIndex);
                    writer.Write(slot.TurnIndex);
                    writer.Write(slot.Assignments.Count);
                    foreach (var assignment in slot.Assignments)
                    {
                        writer.Write(assignment.HeroId.Value);
                        WriteOptional(writer, assignment.CardInstanceId.HasValue,
                            assignment.CardInstanceId.HasValue ? assignment.CardInstanceId.Value.Value : null);
                        writer.Write(assignment.PassedWithEmptyHand);
                    }
                }

                writer.Write(projection.Commitments.Count);
                foreach (var commitment in projection.Commitments)
                {
                    writer.Write(commitment.HeroId.Value);
                    writer.Write((int)commitment.Status);
                    writer.Write(commitment.CanPass);
                    WriteOptional(writer, commitment.RevealedCardInstanceId.HasValue,
                        commitment.RevealedCardInstanceId.HasValue
                            ? commitment.RevealedCardInstanceId.Value.Value
                            : null);
                }

                writer.Write(projection.HandSummaries.Count);
                foreach (var summary in projection.HandSummaries)
                {
                    writer.Write(summary.HeroId.Value);
                    writer.Write(summary.HandCount);
                    writer.Write(summary.DeckCount);
                }

                WriteCards(writer, projection.OwnCards);
                WriteCards(writer, projection.PublicCards);

                writer.Write(projection.HeroProgression.Count);
                foreach (var progression in projection.HeroProgression)
                {
                    writer.Write(progression.HeroId.Value);
                    writer.Write(progression.Level);
                    writer.Write(progression.Coins);
                    writer.Write(progression.LeveledThisRound);
                    writer.Write(progression.ItemBonuses.Count);
                    foreach (var bonus in progression.ItemBonuses)
                    {
                        writer.Write((int)bonus.Key);
                        writer.Write(bonus.Value);
                    }
                }

                writer.Write(projection.ActiveEffectSourceCardInstanceIds.Count);
                foreach (var cardId in projection.ActiveEffectSourceCardInstanceIds)
                {
                    writer.Write(cardId.Value);
                }

                writer.Write(projection.DecisionOwner.IsDefined);
                if (projection.DecisionOwner.IsDefined)
                {
                    writer.Write(projection.DecisionOwner.FamilyId.Value);
                    WriteOptional(writer, projection.DecisionOwner.HeroId.IsDefined,
                        projection.DecisionOwner.HeroId.Value);
                    writer.Write((int)projection.DecisionOwner.TeamSide);
                }

                writer.Write(projection.DecisionContract != null);
                if (projection.DecisionContract != null)
                {
                    writer.Write(projection.DecisionContract.FamilyId.Value);
                    WriteCommands(writer, projection.DecisionContract.Commands);
                }

                WriteCommands(writer, projection.AuthorizedCommands);
                NetworkProjectionDetailCodec.Write(writer, projection);

                writer.Flush();
                return stream.ToArray();
            }
        }

        public static bool TryDecode(byte[] payload, out NetworkSeatProjectionRecord record)
        {
            record = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var version = reader.ReadInt64();
                    var seatId = reader.ReadString();
                    var playerId = reader.ReadString();
                    var heroId = reader.ReadString();
                    var teamSide = reader.ReadInt32();
                    var roundIndex = reader.ReadInt32();
                    var turnIndex = reader.ReadInt32();
                    var phase = reader.ReadInt32();
                    var isTurnActive = reader.ReadBoolean();
                    var isSeatUndoAvailable = reader.ReadBoolean();
                    int? endOfRoundStage = null;
                    if (reader.ReadBoolean())
                    {
                        endOfRoundStage = reader.ReadInt32();
                    }

                    var tieBreakerSide = reader.ReadInt32();
                    if (version < 0 || string.IsNullOrWhiteSpace(seatId) ||
                        string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId))
                    {
                        return false;
                    }

                    NetworkSelectedActorRecord selectedActor;
                    if (reader.ReadBoolean())
                    {
                        var actorCard = reader.ReadString();
                        var actorId = reader.ReadString();
                        var actorSide = reader.ReadInt32();
                        var initiativeTotal = reader.ReadInt32();
                        var flipTieBreakerAfterResolution = reader.ReadBoolean();
                        if (string.IsNullOrWhiteSpace(actorCard) || string.IsNullOrWhiteSpace(actorId))
                        {
                            return false;
                        }

                        selectedActor = new NetworkSelectedActorRecord(
                            true, actorCard, actorId, actorSide, initiativeTotal,
                            flipTieBreakerAfterResolution);
                    }
                    else
                    {
                        selectedActor = new NetworkSelectedActorRecord(false, null, null, 0, 0, false);
                    }

                    NetworkOutcomeRecord outcome;
                    if (reader.ReadBoolean())
                    {
                        var cause = reader.ReadInt32();
                        var winner = reader.ReadString();
                        var loser = reader.ReadString();
                        if (cause == 0 || string.IsNullOrWhiteSpace(winner) || string.IsNullOrWhiteSpace(loser))
                        {
                            return false;
                        }

                        outcome = new NetworkOutcomeRecord(cause, winner, loser);
                    }
                    else
                    {
                        outcome = new NetworkOutcomeRecord(0, null, null);
                    }

                    if (!TryReadCount(reader, out var counterCount))
                    {
                        return false;
                    }

                    var counters = new List<NetworkLifeCounterRow>(counterCount);
                    for (var index = 0; index < counterCount; index++)
                    {
                        var teamId = reader.ReadString();
                        var count = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(teamId) || count < 0)
                        {
                            return false;
                        }

                        counters.Add(new NetworkLifeCounterRow(teamId, count));
                    }

                    if (!TryReadCount(reader, out var heroCount))
                    {
                        return false;
                    }

                    var heroes = new List<NetworkHeroRow>(heroCount);
                    for (var index = 0; index < heroCount; index++)
                    {
                        var id = reader.ReadString();
                        var q = reader.ReadInt32();
                        var r = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            return false;
                        }

                        heroes.Add(new NetworkHeroRow(id, q, r));
                    }

                    if (!TryReadCount(reader, out var minionCount))
                    {
                        return false;
                    }

                    var minions = new List<NetworkMinionRow>(minionCount);
                    for (var index = 0; index < minionCount; index++)
                    {
                        var id = reader.ReadString();
                        var teamId = reader.ReadString();
                        var side = reader.ReadInt32();
                        var minionType = reader.ReadInt32();
                        var q = reader.ReadInt32();
                        var r = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(teamId))
                        {
                            return false;
                        }

                        minions.Add(new NetworkMinionRow(id, teamId, side, minionType, q, r));
                    }

                    if (!TryReadCount(reader, out var obstacleCount))
                    {
                        return false;
                    }

                    var obstacles = new List<NetworkObstacleRow>(obstacleCount);
                    for (var index = 0; index < obstacleCount; index++)
                    {
                        var id = reader.ReadString();
                        var q = reader.ReadInt32();
                        var r = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            return false;
                        }

                        obstacles.Add(new NetworkObstacleRow(id, q, r));
                    }

                    if (!TryReadCount(reader, out var tokenCount))
                    {
                        return false;
                    }

                    var tokens = new List<NetworkTokenRow>(tokenCount);
                    for (var index = 0; index < tokenCount; index++)
                    {
                        var id = reader.ReadString();
                        var definitionId = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(definitionId) ||
                            !TryReadOptional(reader, out var owner) ||
                            !TryReadOptional(reader, out var controller) ||
                            !TryReadOptional(reader, out var sourceCard))
                        {
                            return false;
                        }

                        var q = reader.ReadInt32();
                        var r = reader.ReadInt32();
                        tokens.Add(new NetworkTokenRow(id, definitionId, owner, controller, sourceCard, q, r));
                    }

                    if (!TryReadCount(reader, out var markerCount))
                    {
                        return false;
                    }

                    var markers = new List<NetworkMarkerRow>(markerCount);
                    for (var index = 0; index < markerCount; index++)
                    {
                        var id = reader.ReadString();
                        var definitionId = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(definitionId) ||
                            !TryReadOptional(reader, out var owner) ||
                            !TryReadOptional(reader, out var holder) ||
                            !TryReadOptional(reader, out var sourceCard) ||
                            !TryReadOptional(reader, out var sourceAction))
                        {
                            return false;
                        }

                        markers.Add(new NetworkMarkerRow(id, definitionId, owner, holder, sourceCard, sourceAction));
                    }

                    if (!TryReadCount(reader, out var slotCount))
                    {
                        return false;
                    }

                    var slots = new List<NetworkTurnSlotRow>(slotCount);
                    for (var index = 0; index < slotCount; index++)
                    {
                        var slotRound = reader.ReadInt32();
                        var slotTurn = reader.ReadInt32();
                        if (!TryReadCount(reader, out var assignmentCount))
                        {
                            return false;
                        }

                        var assignments = new List<NetworkTurnAssignmentRow>(assignmentCount);
                        for (var assignmentIndex = 0; assignmentIndex < assignmentCount; assignmentIndex++)
                        {
                            var slotHero = reader.ReadString();
                            if (string.IsNullOrWhiteSpace(slotHero) ||
                                !TryReadOptional(reader, out var slotCard))
                            {
                                return false;
                            }

                            assignments.Add(new NetworkTurnAssignmentRow(
                                slotHero, slotCard, reader.ReadBoolean()));
                        }

                        slots.Add(new NetworkTurnSlotRow(
                            slotRound, slotTurn, Array.AsReadOnly(assignments.ToArray())));
                    }

                    if (!TryReadCount(reader, out var commitmentCount))
                    {
                        return false;
                    }

                    var commitments = new List<NetworkCommitmentRow>(commitmentCount);
                    for (var index = 0; index < commitmentCount; index++)
                    {
                        var id = reader.ReadString();
                        var status = reader.ReadInt32();
                        var canPass = reader.ReadBoolean();
                        if (string.IsNullOrWhiteSpace(id) || !TryReadOptional(reader, out var revealed))
                        {
                            return false;
                        }

                        commitments.Add(new NetworkCommitmentRow(id, status, canPass, revealed));
                    }

                    if (!TryReadCount(reader, out var summaryCount))
                    {
                        return false;
                    }

                    var summaries = new List<NetworkHandSummaryRow>(summaryCount);
                    for (var index = 0; index < summaryCount; index++)
                    {
                        var id = reader.ReadString();
                        var handCount = reader.ReadInt32();
                        var deckCount = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id) || handCount < 0 || deckCount < 0)
                        {
                            return false;
                        }

                        summaries.Add(new NetworkHandSummaryRow(id, handCount, deckCount));
                    }

                    if (!TryReadCards(reader, out var ownCards) || !TryReadCards(reader, out var publicCards))
                    {
                        return false;
                    }

                    if (!TryReadCount(reader, out var progressionCount))
                    {
                        return false;
                    }

                    var progression = new List<NetworkProgressionRow>(progressionCount);
                    for (var index = 0; index < progressionCount; index++)
                    {
                        var id = reader.ReadString();
                        var level = reader.ReadInt32();
                        var coins = reader.ReadInt32();
                        var leveled = reader.ReadBoolean();
                        if (string.IsNullOrWhiteSpace(id) || level < 1 || coins < 0 ||
                            !TryReadCount(reader, out var bonusCount))
                        {
                            return false;
                        }

                        var bonuses = new List<KeyValuePair<int, int>>(bonusCount);
                        for (var bonusIndex = 0; bonusIndex < bonusCount; bonusIndex++)
                        {
                            var channel = reader.ReadInt32();
                            var amount = reader.ReadInt32();
                            if (amount < 1)
                            {
                                return false;
                            }

                            bonuses.Add(new KeyValuePair<int, int>(channel, amount));
                        }

                        progression.Add(new NetworkProgressionRow(
                            id, level, coins, leveled, Array.AsReadOnly(bonuses.ToArray())));
                    }

                    if (!TryReadCount(reader, out var activeCount))
                    {
                        return false;
                    }

                    var activeSources = new List<string>(activeCount);
                    for (var index = 0; index < activeCount; index++)
                    {
                        var id = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            return false;
                        }

                        activeSources.Add(id);
                    }

                    NetworkDecisionOwnerRecord owner_;
                    if (reader.ReadBoolean())
                    {
                        var family = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(family) || !TryReadOptional(reader, out var ownerHero))
                        {
                            return false;
                        }

                        owner_ = new NetworkDecisionOwnerRecord(family, ownerHero, reader.ReadInt32());
                    }
                    else
                    {
                        owner_ = new NetworkDecisionOwnerRecord(null, null, 0);
                    }

                    string contractFamily = null;
                    IReadOnlyList<DirectorCommand> contractCommands = Array.Empty<DirectorCommand>();
                    if (reader.ReadBoolean())
                    {
                        contractFamily = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(contractFamily) ||
                            !TryReadCommands(reader, out contractCommands))
                        {
                            return false;
                        }
                    }

                    if (!TryReadCommands(reader, out var authorizedCommands))
                    {
                        return false;
                    }

                    if (!NetworkProjectionDetailCodec.TryRead(reader, out var detail))
                    {
                        return false;
                    }

                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    record = new NetworkSeatProjectionRecord(
                        version, seatId, playerId, heroId, teamSide, roundIndex, turnIndex, phase,
                        isTurnActive, isSeatUndoAvailable, endOfRoundStage, tieBreakerSide,
                        selectedActor, outcome,
                        Array.AsReadOnly(counters.ToArray()),
                        Array.AsReadOnly(heroes.ToArray()),
                        Array.AsReadOnly(minions.ToArray()),
                        Array.AsReadOnly(obstacles.ToArray()),
                        Array.AsReadOnly(tokens.ToArray()),
                        Array.AsReadOnly(markers.ToArray()),
                        Array.AsReadOnly(slots.ToArray()),
                        Array.AsReadOnly(commitments.ToArray()),
                        Array.AsReadOnly(summaries.ToArray()),
                        ownCards,
                        publicCards,
                        Array.AsReadOnly(progression.ToArray()),
                        Array.AsReadOnly(activeSources.ToArray()),
                        owner_,
                        contractFamily,
                        contractCommands,
                        authorizedCommands,
                        detail);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        private static void WriteCards(BinaryWriter writer, IReadOnlyList<Core.Cards.CardInstance> cards)
        {
            writer.Write(cards.Count);
            foreach (var card in cards)
            {
                writer.Write(card.Id.Value);
                writer.Write(card.DefinitionReference.DefinitionId.Value);
                writer.Write(card.DefinitionReference.ContentRevision);
                writer.Write(card.OwnerId.Value);
                writer.Write((int)card.State.Location);
                writer.Write(card.State.HasBeenPlayed);
                writer.Write((int)card.State.ResolutionStatus);
                writer.Write((int)card.State.FaceOrientation);
            }
        }

        private static bool TryReadCards(BinaryReader reader, out IReadOnlyList<NetworkCardRow> cards)
        {
            cards = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var rows = new List<NetworkCardRow>(count);
            for (var index = 0; index < count; index++)
            {
                var instanceId = reader.ReadString();
                var definitionId = reader.ReadString();
                var contentRevision = reader.ReadString();
                var ownerId = reader.ReadString();
                var location = reader.ReadInt32();
                var hasBeenPlayed = reader.ReadBoolean();
                var resolutionStatus = reader.ReadInt32();
                var faceOrientation = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(definitionId) ||
                    string.IsNullOrWhiteSpace(ownerId))
                {
                    return false;
                }

                rows.Add(new NetworkCardRow(
                    instanceId, definitionId, contentRevision, ownerId, location,
                    hasBeenPlayed, resolutionStatus, faceOrientation));
            }

            cards = Array.AsReadOnly(rows.ToArray());
            return true;
        }

        private static void WriteCommands(BinaryWriter writer, IReadOnlyList<DirectorCommand> commands)
        {
            writer.Write(commands.Count);
            foreach (var command in commands)
            {
                NetworkCommandCodec.Write(writer, command);
            }
        }

        private static bool TryReadCommands(BinaryReader reader, out IReadOnlyList<DirectorCommand> commands)
        {
            commands = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var decoded = new List<DirectorCommand>(count);
            for (var index = 0; index < count; index++)
            {
                if (!NetworkCommandCodec.TryRead(reader, out var command))
                {
                    return false;
                }

                decoded.Add(command);
            }

            commands = Array.AsReadOnly(decoded.ToArray());
            return true;
        }

        private static bool TryReadCount(BinaryReader reader, out int count)
        {
            count = reader.ReadInt32();
            return count >= 0 && count <= MaxListEntries;
        }

        private static void WriteOptional(BinaryWriter writer, bool isDefined, string value)
        {
            writer.Write(isDefined);
            if (isDefined)
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
