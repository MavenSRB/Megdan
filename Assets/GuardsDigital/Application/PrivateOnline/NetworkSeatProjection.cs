using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;

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
            int remainingWaveCounters,
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
            IReadOnlyList<NetworkScoreboardRow> scoreboard,
            IReadOnlyList<string> activeEffectSourceCardInstanceIds,
            NetworkDecisionOwnerRecord decisionOwner,
            string decisionContractFamilyId,
            IReadOnlyList<DirectorCommand> decisionContractCommands,
            IReadOnlyList<DirectorCommand> authorizedCommands,
            NetworkProjectionDetailRecord detail,
            IReadOnlyList<NetworkTeamVictoryTallyRow> teamVictoryTallies = null)
        {
            Version = version;
            TeamVictoryTallies = teamVictoryTallies ?? Array.Empty<NetworkTeamVictoryTallyRow>();
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
            RemainingWaveCounters = remainingWaveCounters;
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
            Scoreboard = scoreboard;
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

        /// <summary>
        /// Milestone 8.35z60, protocol version 11. Rulebook p.8's ONE SHARED Wave-counter pool as it
        /// currently stands, so a remote seat's top strip reads the same live number the host's does
        /// rather than the frozen starting total its own locally composed match still holds.
        ///
        /// <para><b>Carried, not derived, and not optional.</b> Derivation was available and refused
        /// for the reason milestone 8.35z14 records for the blocker channel: a seat holds no rule
        /// material that can say how many pushes a match has spent, so a client-side count would be a
        /// second reading of the host's rules, free to disagree with it. It is a plain
        /// <c>Int32</c> rather than an optional pair because the host ALWAYS knows this number - it
        /// reads straight off <c>LanePushTransitionAuthority.RemainingWaveCounters</c>, which exists
        /// for the whole life of a composed match - so an absent branch would be one that can never
        /// be taken, and this codebase does not ship a branch it cannot reach.</para>
        /// </summary>
        public int RemainingWaveCounters { get; }

        /// <summary>
        /// Milestone 9.30g, protocol /32. Every team's count toward a printed team victory (Gydion's
        /// Wish) with its printed threshold - the same rows for every seat (GY-Q10: public), carried
        /// rather than derived, because no seat holds the history that would count them.
        /// </summary>
        public IReadOnlyList<NetworkTeamVictoryTallyRow> TeamVictoryTallies { get; }

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

        /// <summary>
        /// Milestone 8.53, protocol version 17. The match scoreboard the victory screen prints, one
        /// row per participating hero.
        ///
        /// <para><b>Carried rather than derived, and derivation is not merely refused here - it is
        /// unavailable.</b> Every fact these six columns count is seat-public and already crosses the
        /// wire as a match-log row, but that log is a BOUNDED WINDOW of recent lines: a seat counting
        /// its own totals out of it would report fewer kills the longer the match ran, and a seat
        /// that reconnected would start its board from the moment it came back. The host keeps the
        /// totals; the frame carries them.</para>
        ///
        /// <para><b>The same rows for every seat.</b> There is no per-seat narrowing here, for the
        /// reason the match log has none: a Defeat, a declared attack, an answered defense, a removed
        /// minion and a paid-out coin are all things the whole table watched happen.</para>
        /// </summary>
        public IReadOnlyList<NetworkScoreboardRow> Scoreboard { get; }

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
        public NetworkOutcomeRecord(
            int cause, string winningTeamId, string losingTeamId, string winningHeroId = "")
        {
            Cause = cause;
            WinningTeamId = winningTeamId;
            LosingTeamId = losingTeamId;
            WinningHeroId = winningHeroId ?? string.Empty;
        }

        public int Cause { get; }
        public string WinningTeamId { get; }
        public string LosingTeamId { get; }

        /// <summary>
        /// The hero who won alone, or the empty string for one of the rulebook's three team endings.
        /// Milestone 8.39g, protocol 14.
        /// </summary>
        public string WinningHeroId { get; }
    }

    public sealed class NetworkDecisionOwnerRecord
    {
        public NetworkDecisionOwnerRecord(string familyId, IReadOnlyList<string> heroIds, int teamSide)
        {
            FamilyId = familyId;
            HeroIds = heroIds ?? Array.Empty<string>();
            TeamSide = teamSide;
        }

        /// <summary>Null while nothing is pending.</summary>
        public string FamilyId { get; }

        /// <summary>
        /// Milestone 8.35u. Every hero the authority is waiting on, empty for a team-owned decision.
        ///
        /// <para>It became a repeated group because rulebook p.15 asks every hero that bought a level
        /// at the same time, and "we are waiting on these players" is table-public in a way "what they
        /// were each offered" is not - each seat still receives only its own question in the detail
        /// record beside this.</para>
        /// </summary>
        public IReadOnlyList<string> HeroIds { get; }

        /// <summary>The first owning hero, or null for a team-owned decision.</summary>
        public string HeroId => HeroIds.Count == 0 ? null : HeroIds[0];

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

    /// <summary>Milestone 9.30g, protocol /32. One team's count toward a printed team victory.</summary>
    public sealed class NetworkTeamVictoryTallyRow
    {
        public NetworkTeamVictoryTallyRow(string teamId, int count, int threshold)
        {
            TeamId = teamId;
            Count = count;
            Threshold = threshold;
        }

        public string TeamId { get; }
        public int Count { get; }
        public int Threshold { get; }
    }

    public sealed class NetworkHeroRow
    {
        public NetworkHeroRow(string heroId, int body, int q, int r, bool acting = false)
        {
            HeroId = heroId;
            Body = body;
            Q = q;
            R = r;
            Acting = acting;
        }

        public string HeroId { get; }

        /// <summary>Milestone 9.21a, protocol /26. Which body of the hero stands here (1 for a single-bodied hero).</summary>
        public int Body { get; }
        public int Q { get; }
        public int R { get; }

        /// <summary>
        /// Milestone 9.21j, protocol /28. Whether this body is its many-bodied hero's settled acting
        /// body (RZ-Q4, <c>IMatchReadModel.ActingBodies</c>) - what the board's acting cue is drawn
        /// from. A settled acting body always stands (9.21b-9.21e), so its own row carries it.
        /// </summary>
        public bool Acting { get; }
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
            string sourceActionDefinitionId,
            int turnSlotIndex,
            bool activeUntilHolderNextAction = false)
        {
            MarkerInstanceId = markerInstanceId;
            MarkerDefinitionId = markerDefinitionId;
            OwnerHeroId = ownerHeroId;
            HolderHeroId = holderHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            TurnSlotIndex = turnSlotIndex;
            ActiveUntilHolderNextAction = activeUntilHolderNextAction;
        }

        public string MarkerInstanceId { get; }
        public string MarkerDefinitionId { get; }
        public string OwnerHeroId { get; }
        public string HolderHeroId { get; }
        public string SourceCardInstanceId { get; }
        public string SourceActionDefinitionId { get; }
        public int TurnSlotIndex { get; }
        public bool ActiveUntilHolderNextAction { get; }
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
        public NetworkTurnAssignmentRow(
            string heroId, string cardInstanceId, bool passedWithEmptyHand, bool isFacedown = false,
            CardColor knownColor = CardColor.None)
        {
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            PassedWithEmptyHand = passedWithEmptyHand;
            IsFacedown = isFacedown;
            KnownColor = knownColor;
        }

        public string HeroId { get; }

        /// <summary>
        /// Null for a pass, or (milestone 9.32a) for a card placed facedown that this seat may not
        /// name. Slots exist only after the reveal, so every other card here is public.
        /// </summary>
        public string CardInstanceId { get; }

        public bool PassedWithEmptyHand { get; }

        /// <summary>
        /// Milestone 9.32a, wire <c>/34</c>. The slot's card was placed facedown (p.10); with no card
        /// id it is withheld from this seat (<see cref="GuardsDigital.Core.Cards.TurnCardAssignment.IsWithheld"/>).
        /// </summary>
        public bool IsFacedown { get; }

        /// <summary>Milestone 9.32g, protocol /35. A withheld facedown card's known colour.</summary>
        public CardColor KnownColor { get; }
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
        public NetworkHandSummaryRow(
            string heroId,
            int handCount,
            int deckCount,
            int spellbookCount,
            IEnumerable<SpellbookBackCount> spellbookBacks = null,
            IEnumerable<CardColor> facedownDiscardColors = null)
        {
            HeroId = heroId;
            HandCount = handCount;
            DeckCount = deckCount;
            SpellbookCount = spellbookCount;
            SpellbookBacks = Array.AsReadOnly(
                (spellbookBacks ?? Enumerable.Empty<SpellbookBackCount>()).ToArray());
            FacedownDiscardColors = Array.AsReadOnly(
                (facedownDiscardColors ?? Enumerable.Empty<CardColor>()).ToArray());
        }

        public string HeroId { get; }
        public int HandCount { get; }
        public int DeckCount { get; }

        /// <summary>Milestone 9.30b, protocol /31: the facedown spellbook's size, naming no spell.</summary>
        public int SpellbookCount { get; }

        /// <summary>
        /// Milestone 9.30h, protocol /32: the same book counted by the back each spell shows, one row
        /// per back, summing to <see cref="SpellbookCount"/> - the tier backs every seat sees.
        /// </summary>
        public IReadOnlyList<SpellbookBackCount> SpellbookBacks { get; }

        /// <summary>Milestone 9.32g, protocol /35. The pile's facedown cards, by known colour.</summary>
        public IReadOnlyList<CardColor> FacedownDiscardColors { get; }
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
            int faceOrientation,
            int rewrittenInitiative = -1,
            string rewrittenInitiativeSource = null)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            ContentRevision = contentRevision;
            OwnerHeroId = ownerHeroId;
            Location = location;
            HasBeenPlayed = hasBeenPlayed;
            ResolutionStatus = resolutionStatus;
            FaceOrientation = faceOrientation;
            RewrittenInitiative = rewrittenInitiative;
            RewrittenInitiativeSource = rewrittenInitiativeSource;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string ContentRevision { get; }
        public string OwnerHeroId { get; }
        public int Location { get; }
        public bool HasBeenPlayed { get; }
        public int ResolutionStatus { get; }
        public int FaceOrientation { get; }

        /// <summary>
        /// Milestone 8.71a, wire <c>guards-private-online/20</c>. The printed Initiative Hanu's Hurry
        /// up! has rewritten on this card, or <c>-1</c> where none has - which is every card in every
        /// match no Hurry up! has touched.
        ///
        /// <para>It crosses the wire rather than being derived on each client because it CANNOT be
        /// derived: nothing a client holds says a sentence was spoken about a card. Rulebook p.7
        /// re-checks the Initiative after every resolution, so a seat that did not know would draw
        /// the printed icon on the rail while the host ordered the round by the rewritten one - the
        /// projection disagreement the wire exists to make impossible.</para>
        /// </summary>
        public int RewrittenInitiative { get; }

        /// <summary>The card instance whose text rewrote it, or <c>null</c> where none has.</summary>
        public string RewrittenInitiativeSource { get; }
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
    /// Milestone 8.53, protocol version 17. One hero's line of the match scoreboard on the wire.
    /// The Level is deliberately not repeated here - it is already on
    /// <see cref="NetworkProgressionRow"/>, and the victory screen reads it there.
    /// </summary>
    public sealed class NetworkScoreboardRow
    {
        public NetworkScoreboardRow(
            string heroId,
            int kills,
            int deaths,
            int attacksAtHeroes,
            int timesDefended,
            int minionsKilled,
            int coinsEarned)
        {
            HeroId = heroId;
            Kills = kills;
            Deaths = deaths;
            AttacksAtHeroes = attacksAtHeroes;
            TimesDefended = timesDefended;
            MinionsKilled = minionsKilled;
            CoinsEarned = coinsEarned;
        }

        public string HeroId { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int AttacksAtHeroes { get; }
        public int TimesDefended { get; }
        public int MinionsKilled { get; }
        public int CoinsEarned { get; }
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

                    // Milestone 8.39g. A solo ending names no teams at all - "you alone win the game"
                    // leaves the winner's own team-mates losing with everybody else - so the two team
                    // fields are written as the named absence and the hero is written beside them.
                    // Both halves are always on the wire, so a reader does not have to know which
                    // shape it is before it can parse the row.
                    writer.Write(projection.Outcome.HasSoloWinner
                        ? string.Empty
                        : projection.Outcome.WinningTeamId.Value);
                    writer.Write(projection.Outcome.HasSoloWinner
                        ? string.Empty
                        : projection.Outcome.LosingTeamId.Value);
                    writer.Write(projection.Outcome.HasSoloWinner
                        ? projection.Outcome.WinningHeroId.Value
                        : string.Empty);
                }

                writer.Write(projection.TeamLifeCounters.Count);
                foreach (var counter in projection.TeamLifeCounters)
                {
                    writer.Write(counter.Key.Value);
                    writer.Write(counter.Value);
                }

                // Milestone 8.35z60, protocol version 11. p.8's shared Wave pool rides here, beside
                // the per-team Life counters it is the table-wide counterpart of, rather than being
                // appended at the end: version 10 is refused from the bounded header before either
                // side interprets a payload, so placement is a readability choice and the two
                // counter facts belong together.
                writer.Write(projection.RemainingWaveCounters);

                // Milestone 9.30g, protocol /32. Every team's count toward a printed team victory,
                // beside the two other counter facts of the table.
                writer.Write(projection.TeamVictoryTallies.Count);
                foreach (var tally in projection.TeamVictoryTallies)
                {
                    writer.Write(tally.TeamId.Value);
                    writer.Write(tally.Count);
                    writer.Write(tally.Threshold);
                }

                writer.Write(projection.Heroes.Count);
                foreach (var hero in projection.Heroes)
                {
                    writer.Write(hero.HeroId.Value);
                    writer.Write(hero.Body);
                    writer.Write(hero.Coordinate.Q);
                    writer.Write(hero.Coordinate.R);
                    // Milestone 9.21j, protocol /28: the settled acting body rides its own row.
                    writer.Write(projection.ActingBodies.Contains(new HeroActingBody(hero.HeroId, hero.Body)));
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
                    writer.Write(marker.TurnSlotIndex ?? 0);
                    writer.Write(marker.ActiveUntilHolderNextAction);
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
                        writer.Write(assignment.IsFacedown);
                        // Milestone 9.32g, protocol /35: a withheld card's known colour.
                        writer.Write((int)assignment.KnownColor);
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
                    // Milestone 9.30b, protocol /31: the facedown spellbook's size, naming no spell.
                    writer.Write(summary.SpellbookCount);
                    // Milestone 9.30h, protocol /32: the same book by the back each spell shows.
                    writer.Write(summary.SpellbookBacks.Count);
                    foreach (var back in summary.SpellbookBacks)
                    {
                        writer.Write((int)back.Back);
                        writer.Write(back.Count);
                    }

                    // Milestone 9.32g, protocol /35: the discard pile's facedown cards, by known colour.
                    writer.Write(summary.FacedownDiscardColors.Count);
                    foreach (var color in summary.FacedownDiscardColors)
                    {
                        writer.Write((int)color);
                    }
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

                // Milestone 8.53, protocol 17: the match scoreboard, written between the progression
                // rows and the Active-effect sources. A version 16 peer reads this count as the
                // source count and mis-decodes everything after it, which is why the version moved.
                writer.Write(projection.Scoreboard.Count);
                foreach (var line in projection.Scoreboard)
                {
                    writer.Write(line.HeroId.Value);
                    writer.Write(line.Kills);
                    writer.Write(line.Deaths);
                    writer.Write(line.AttacksAtHeroes);
                    writer.Write(line.TimesDefended);
                    writer.Write(line.MinionsKilled);
                    writer.Write(line.CoinsEarned);
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

                    // Milestone 8.35u, protocol 7: a repeated hero group where protocol 6 wrote one
                    // optional hero. The count sits exactly where that present/absent boolean sat, so
                    // an older client does not read a shorter record - it reads a different one, which
                    // is why this moved the protocol version rather than riding the old one.
                    writer.Write(projection.DecisionOwner.HeroIds.Count);
                    foreach (var heroId in projection.DecisionOwner.HeroIds)
                    {
                        writer.Write(heroId.Value);
                    }

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
                        var soloWinner = reader.ReadString();

                        // Milestone 8.39g. Exactly one of the two shapes, and a row carrying neither
                        // or both is refused rather than resolved by preference: a solo ending names
                        // one hero and no team, and a team ending names two teams and no hero.
                        var solo = !string.IsNullOrWhiteSpace(soloWinner);
                        var team = !string.IsNullOrWhiteSpace(winner) &&
                            !string.IsNullOrWhiteSpace(loser);
                        if (cause == 0 || solo == team ||
                            (team && !string.IsNullOrWhiteSpace(soloWinner)))
                        {
                            return false;
                        }

                        outcome = solo
                            ? new NetworkOutcomeRecord(cause, string.Empty, string.Empty, soloWinner)
                            : new NetworkOutcomeRecord(cause, winner, loser);
                    }
                    else
                    {
                        outcome = new NetworkOutcomeRecord(0, null, null, null);
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

                    // Milestone 8.35z60. Validated on the same rule as every other counted value
                    // here: a pool cannot be negative, and a frame claiming one is refused whole
                    // rather than clamped, because a clamp would hand the strip a number no host
                    // ever sent.
                    var remainingWaveCounters = reader.ReadInt32();
                    if (remainingWaveCounters < 0)
                    {
                        return false;
                    }

                    // Milestone 9.30g, protocol /32. A tally exists once it has counted, toward a
                    // positive threshold; a row claiming otherwise is refused whole.
                    if (!TryReadCount(reader, out var tallyCount))
                    {
                        return false;
                    }

                    var tallies = new List<NetworkTeamVictoryTallyRow>(tallyCount);
                    for (var index = 0; index < tallyCount; index++)
                    {
                        var tallyTeamId = reader.ReadString();
                        var tallyCountValue = reader.ReadInt32();
                        var tallyThreshold = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(tallyTeamId) || tallyCountValue < 1 || tallyThreshold < 1)
                        {
                            return false;
                        }

                        tallies.Add(new NetworkTeamVictoryTallyRow(tallyTeamId, tallyCountValue, tallyThreshold));
                    }

                    if (!TryReadCount(reader, out var heroCount))
                    {
                        return false;
                    }

                    var heroes = new List<NetworkHeroRow>(heroCount);
                    for (var index = 0; index < heroCount; index++)
                    {
                        var id = reader.ReadString();
                        var body = reader.ReadInt32();
                        var q = reader.ReadInt32();
                        var r = reader.ReadInt32();
                        var acting = reader.ReadBoolean();
                        if (string.IsNullOrWhiteSpace(id) || body < 1)
                        {
                            return false;
                        }

                        // Milestone 9.21j. One acting body per hero at most, as the engine settles it.
                        if (acting && heroes.Any(row => row.Acting && row.HeroId == id))
                        {
                            return false;
                        }

                        heroes.Add(new NetworkHeroRow(id, body, q, r, acting));
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

                        var turnSlotIndex = reader.ReadInt32();
                        if (turnSlotIndex < 0 ||
                            turnSlotIndex > GuardsDigital.Core.Cards.RoundResolutionAuthority.TurnsPerRound)
                        {
                            return false;
                        }
                        markers.Add(new NetworkMarkerRow(id, definitionId, owner, holder,
                            sourceCard, sourceAction, turnSlotIndex, reader.ReadBoolean()));
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

                            var slotPassed = reader.ReadBoolean();
                            var slotFacedown = reader.ReadBoolean();
                            if (!TryReadKnownColor(reader, slotCard, slotFacedown, out var slotColor))
                            {
                                return false;
                            }

                            assignments.Add(new NetworkTurnAssignmentRow(
                                slotHero, slotCard, slotPassed, slotFacedown, slotColor));
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
                        var spellbookCount = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id) || handCount < 0 || deckCount < 0 ||
                            spellbookCount < 0 || !TryReadCount(reader, out var backCount))
                        {
                            return false;
                        }

                        // Milestone 9.30h, protocol /32. Each back at most once, Tier I to IV, in
                        // order, and together exactly the book's size - or the frame is refused.
                        var backs = new List<SpellbookBackCount>(backCount);
                        for (var backIndex = 0; backIndex < backCount; backIndex++)
                        {
                            var back = reader.ReadInt32();
                            var count = reader.ReadInt32();
                            if (back < (int)CardTier.I || back > (int)CardTier.IV || count < 1 ||
                                (backs.Count > 0 && (int)backs[backs.Count - 1].Back >= back))
                            {
                                return false;
                            }

                            backs.Add(new SpellbookBackCount((CardTier)back, count));
                        }

                        if (backs.Count > 0 && backs.Sum(back => back.Count) != spellbookCount)
                        {
                            return false;
                        }

                        // Milestone 9.32g, protocol /35. One known colour per facedown discard.
                        if (!TryReadCount(reader, out var facedownCount))
                        {
                            return false;
                        }

                        var facedownColors = new List<CardColor>(facedownCount);
                        for (var colorIndex = 0; colorIndex < facedownCount; colorIndex++)
                        {
                            var color = reader.ReadInt32();
                            if (!Enum.IsDefined(typeof(CardColor), color) || color == (int)CardColor.Purple)
                            {
                                return false;
                            }

                            facedownColors.Add((CardColor)color);
                        }

                        summaries.Add(new NetworkHandSummaryRow(
                            id, handCount, deckCount, spellbookCount, backs, facedownColors));
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

                    if (!TryReadCount(reader, out var scoreboardCount))
                    {
                        return false;
                    }

                    var scoreboard = new List<NetworkScoreboardRow>(scoreboardCount);
                    for (var index = 0; index < scoreboardCount; index++)
                    {
                        var id = reader.ReadString();
                        var kills = reader.ReadInt32();
                        var deaths = reader.ReadInt32();
                        var attacksAtHeroes = reader.ReadInt32();
                        var timesDefended = reader.ReadInt32();
                        var minionsKilled = reader.ReadInt32();
                        var coinsEarned = reader.ReadInt32();
                        if (string.IsNullOrWhiteSpace(id) || kills < 0 || deaths < 0 ||
                            attacksAtHeroes < 0 || timesDefended < 0 || minionsKilled < 0 ||
                            coinsEarned < 0)
                        {
                            return false;
                        }

                        scoreboard.Add(new NetworkScoreboardRow(
                            id, kills, deaths, attacksAtHeroes, timesDefended, minionsKilled,
                            coinsEarned));
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
                        if (string.IsNullOrWhiteSpace(family))
                        {
                            return false;
                        }

                        var ownerHeroCount = reader.ReadInt32();
                        if (ownerHeroCount < 0 || ownerHeroCount > MaxListEntries)
                        {
                            return false;
                        }

                        var ownerHeroes = new List<string>(ownerHeroCount);
                        for (var index = 0; index < ownerHeroCount; index++)
                        {
                            var ownerHero = reader.ReadString();
                            if (string.IsNullOrWhiteSpace(ownerHero))
                            {
                                return false;
                            }

                            ownerHeroes.Add(ownerHero);
                        }

                        owner_ = new NetworkDecisionOwnerRecord(family, ownerHeroes, reader.ReadInt32());
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
                        remainingWaveCounters,
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
                        Array.AsReadOnly(scoreboard.ToArray()),
                        Array.AsReadOnly(activeSources.ToArray()),
                        owner_,
                        contractFamily,
                        contractCommands,
                        authorizedCommands,
                        detail,
                        Array.AsReadOnly(tallies.ToArray()));
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

            // Milestone 8.71a, wire `guards-private-online/20`. The rewritten printed Initiative
            // values among these cards, SPARSE and trailing, exactly as schema v44 writes them into
            // the snapshot's card section - one shape, stated twice because the two payloads are two
            // payloads, and never two different shapes to keep in agreement. A count of zero is what
            // every frame of every match carries until a Hurry up! resolves.
            var rewritten = cards.Where(card => card.InitiativeOverride.IsDefined).ToArray();
            writer.Write(rewritten.Length);
            foreach (var card in rewritten)
            {
                writer.Write(card.Id.Value);
                writer.Write(card.InitiativeOverride.Value);
                writer.Write(card.InitiativeOverride.SourceCardInstanceId.Value);
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

            // Milestone 8.71a, wire `guards-private-online/20`. The sparse rewritten-Initiative block
            // WriteCards states, applied to the rows it was written beside. A block naming a card
            // these rows do not contain is a frame this seat refuses rather than one it half-reads -
            // the same refusal every other bounded read in this codec makes.
            if (!TryReadCount(reader, out var rewrittenCount))
            {
                return false;
            }

            for (var index = 0; index < rewrittenCount; index++)
            {
                var instanceId = reader.ReadString();
                var value = reader.ReadInt32();
                var source = reader.ReadString();
                var position = rows.FindIndex(row =>
                    string.Equals(row.InstanceId, instanceId, StringComparison.Ordinal));
                if (position < 0 || value < 0 || string.IsNullOrWhiteSpace(source))
                {
                    return false;
                }

                var row = rows[position];
                rows[position] = new NetworkCardRow(
                    row.InstanceId, row.DefinitionId, row.ContentRevision, row.OwnerHeroId,
                    row.Location, row.HasBeenPlayed, row.ResolutionStatus, row.FaceOrientation,
                    value, source);
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

        /// <summary>
        /// Milestone 9.32g, protocol /35. A slot row's known colour: a printed hand colour, and anything
        /// but None only on a withheld facedown row (no card id) - or the frame is refused.
        /// </summary>
        internal static bool TryReadKnownColor(BinaryReader reader, string cardId, bool isFacedown, out CardColor color)
        {
            var value = reader.ReadInt32();
            color = (CardColor)value;
            if (!Enum.IsDefined(typeof(CardColor), value) || value == (int)CardColor.Purple)
            {
                return false;
            }

            return color == CardColor.None || (isFacedown && cardId == null);
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
