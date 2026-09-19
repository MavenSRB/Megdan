using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Diagnostics
{
    /// <summary>
    /// Milestone 8.35r. The closed set of lines the seat-public match log may carry - one member per
    /// row of the event-selection contract, and nothing the contract did not name.
    /// </summary>
    /// <remarks>
    /// <para>It is a typed vocabulary rather than the raw <see cref="RuleEventLogEntry.EventType"/>
    /// string because a surface must never parse diagnostic prose to decide what a line means. The
    /// translation from one to the other happens once, in <see cref="MatchLogProjection"/>, and every
    /// reader downstream of it sees only these members.</para>
    /// <para><b>What is absent is the point.</b> There is no member for a pending choice, a facedown
    /// commitment, a per-hex movement step, a turn slot's assignment payload, or any engine-internal
    /// progression - those are the selection contract's exclusions, and a kind for them here is the
    /// one thing that could put them on the wire by accident.</para>
    /// </remarks>
    public enum MatchLogEntryKind
    {
        None = 0,

        /// <summary>One move action, however many hexes it crossed.</summary>
        Move = 1,

        /// <summary>One Fast Travel between zones.</summary>
        FastTravel = 2,

        /// <summary>An attack that resolved, with its outcome.</summary>
        Attack = 3,

        /// <summary>A defense that was answered, with its outcome.</summary>
        Defense = 4,

        /// <summary>A Skill action, naming the card it came off.</summary>
        SkillUsed = 5,

        /// <summary>A Clear action, naming the token it removed.</summary>
        Clear = 6,

        /// <summary>
        /// A step a standing modification refused. Milestone 8.35z14 named the blocker where the
        /// refusing node could explain itself - see <see cref="MatchLogEntry.BlockingHeroId"/> and
        /// <see cref="MatchLogEntry.BlockingCardInstanceId"/> - and left it unstated, honestly, where
        /// it could not.
        /// </summary>
        BlockedAction = 7,

        MinionDefeated = 8,
        MinionRemoved = 9,
        MinionRemovalPrevented = 10,
        HeroDefeated = 11,
        HeroRemoved = 12,
        HeroRemovalPrevented = 13,
        HeroRespawned = 14,
        TeamLifeSpent = 15,
        TeamEliminated = 16,

        /// <summary>Rulebook p.18's correction of a minion displaced off a legal space.</summary>
        DisplacedMinionCorrected = 17,

        /// <summary>A Minion Battle removed a minion at the end of a round.</summary>
        MinionBattleMinionRemoved = 18,

        /// <summary>A lane push was requested or resolved.</summary>
        LanePush = 19,

        LevelPurchased = 20,
        CardUpgraded = 21,
        ItemGained = 22,
        PityCoinCollected = 23,

        /// <summary>A round boundary header.</summary>
        RoundStarted = 24,
        RoundCompleted = 25,

        /// <summary>
        /// A turn group header. Carried from the turn slot the round recorded - the header only,
        /// never that slot's assignment payload, which names every hero's committed card.
        /// </summary>
        TurnStarted = 26,
        TurnEnded = 27,

        /// <summary>The match ended, naming the winning side.</summary>
        GameEnded = 28,

        /// <summary>A commitment was revealed. Only ever the revealed form.</summary>
        CardRevealed = 29,

        /// <summary>
        /// Milestone 8.35z61. Rulebook p.13's second line: "If the defeated hero had an
        /// unresolved card, it is resolved, without performing an action."
        ///
        /// <para>THE WHOLE POINT OF THIS ROW IS THAT NOTHING ELSE SAYS IT. The engine has applied
        /// that line since milestone 7.4 and publishes
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.CardActionResolutionEventKind.HeroUnresolvedCardResolved"/>
        /// when it does - but no log kind carried it, so the card silently changed to Resolved,
        /// Initiative never selected its owner, and the player was never asked for anything. From
        /// that player's chair the game passed them over and the log said nothing at all, which is
        /// exactly what the 2026-09-06 six-player playtest reported and could not explain
        /// afterwards.</para>
        ///
        /// <para>It is a hero row rather than a card row: the event names the defeated hero, and
        /// which card was eaten is private commitment content this channel does not carry - the
        /// same line <see cref="CardRevealed"/> draws.</para>
        /// </summary>
        HeroCardResolvedWithoutAction = 30,

        /// <summary>
        /// Milestone 8.40f. A card of a hero's was turned face up by card text, naming that hero and
        /// that card - Bain's three guessing Skills, "then reveal the card".
        ///
        /// <para><b>Why this is not <see cref="CardRevealed"/>, measured rather than argued.</b> Both
        /// rows say a card was turned over, and every CONSUMER of the older one reads it as p.7's
        /// turn-card commitment: the HUD's flip cue borrows the hero's current-card slot to play it
        /// at, the reveal batch is re-staged in printed-Initiative order because the commitment flow
        /// emits its reveals in roster order, and the channel's own one-for-one ledger asserts
        /// exactly as many rows of that kind as <c>Commitment.CardRevealed</c> events occurred. A
        /// card turned over out of a HAND mid-resolution is in neither place and has no second
        /// producer to fold into that ledger, so reusing the member would have attached three rules
        /// the printed sentence does not carry.</para>
        ///
        /// <para><b>It is a hero row AND a card row, and that is the point.</b> The row is what makes
        /// the card nameable: <c>SeatProjection.IsPublic</c> admits a hand card to every seat exactly
        /// when the bounded public log names it, which is the same memory a retrieved card already
        /// uses - so the reveal is permanent, and the card stays readable after it goes back into a
        /// hand everybody has now seen it in.</para>
        /// </summary>
        CardRevealedByCardText = 31,

        /// <summary>
        /// Milestone 8.51. Rulebook p.12's Hold: the acting hero resolved their card WITHOUT
        /// performing its action, by their own seat's command - never the engine's doing.
        ///
        /// <para><b>Why this row exists.</b> At the 2026-09-18 playtest a player reported that
        /// Silverarrow's Warning Shot "got resolved without input". The host's narration file
        /// carried nothing for that turn at all, because a Hold and the turn release that follows
        /// it were the two accepted commands the seat-public log had no word for; the turn could
        /// only be read off the wire log's timing - two accepted submissions from one seat 67 ms
        /// apart, the signature of one END TURN press composed over the network - which is a
        /// reading, not a record. This row is the record: the hero and the card they held, so the
        /// file and the in-game tray both state that the seat ended its turn on an untouched card.</para>
        ///
        /// <para>It is a hero row AND a card row: <c>Turn.HoldActionPerformed</c> names both, and
        /// the card is a resolved turn card every seat already sees. The turn release itself stays
        /// silent - <c>Turn.TurnReleased</c> is a checkpoint naming no hero, and the next actor's
        /// own rows already state that the turn was handed on.</para>
        /// </summary>
        CardHeld = 32,

        /// <summary>
        /// Milestone 8.61e. A hero moved through a token whose own text says it goes off, and the
        /// removal turned it face up for the whole table - Min the Dragonmonk's mines, "After an enemy
        /// hero moves through a mine, remove it; for every blast removed, that hero discards a card, if
        /// able".
        ///
        /// <para><b>It is table-public, and it is the one surface that makes it so.</b> Until the
        /// removal the piece is facedown and every seat but its controller's is handed a face that
        /// names no kind; the moment it goes off, which kind it was is something the whole table saw
        /// happen. The board shows the space emptying and nothing else, so without this row the reveal
        /// would happen and be unrecorded - which is exactly the gap milestone 8.40f's own reveal row
        /// was minted to close.</para>
        ///
        /// <para>The row names the hero who walked over it through <c>SubjectHeroId</c> - never the
        /// acting hero, who on a push is the one doing the pushing - the space through <c>Origin</c>,
        /// and which kind it was through <c>Amount</c>: the number of cards the removal costs that
        /// hero, which is one for a blast and none for a dud. It carries no card, because the hero it
        /// is about played none.</para>
        /// </summary>
        MoveThroughTrapSprung = 33
    }

    /// <summary>
    /// Milestone 8.35r. How a resolved attack or defense came out. Separate from the kind because the
    /// contract's Attack and Defense rows each ask for one line carrying its outcome, not two kinds.
    /// </summary>
    public enum MatchLogOutcome
    {
        None = 0,

        /// <summary>The attack went through.</summary>
        AttackSuccessful = 1,

        /// <summary>The attack was defended.</summary>
        AttackSuccessfullyDefended = 2,

        /// <summary>The defender discarded a card to defend.</summary>
        DefenseCardDiscarded = 3,

        /// <summary>The defender declined to defend.</summary>
        DefenseDeclined = 4
    }

    /// <summary>
    /// Milestone 8.35r. One line of the seat-public match log: what happened, to whom, in which
    /// round and turn - and nothing a viewer's own projection does not already reveal.
    /// </summary>
    /// <remarks>
    /// <para><b>Every field here is table-public by construction.</b> The selection contract admits
    /// only events whose subject the whole table can already see: board positions, resolved attacks
    /// and defenses, revealed commitments, defeats, end-of-round outcomes and round boundaries. No
    /// hand, no unrevealed commitment and no pending choice's option set can reach this type, because
    /// no <see cref="MatchLogEntryKind"/> names one. That is why every seat's log is the SAME log,
    /// and why the seat filter is an identity rather than a redaction.</para>
    /// <para><b>Derived, never stored.</b> These are rebuilt from the retained rule-event log on
    /// every reconstruction, so an undo, a replay, a resync and a snapshot restore each redraw the
    /// log without this type appearing in any persisted payload.</para>
    /// </remarks>
    public sealed class MatchLogEntry : IEquatable<MatchLogEntry>
    {
        public MatchLogEntry(
            int sequence,
            MatchLogEntryKind kind,
            int roundIndex,
            int turnIndex,
            HeroId subjectHeroId = default,
            HeroId targetHeroId = default,
            MinionId subjectMinionId = default,
            CardInstanceId subjectCardInstanceId = default,
            TeamId subjectTeamId = default,
            HexCoordinate origin = default,
            HexCoordinate destination = default,
            bool hasOrigin = false,
            bool hasDestination = false,
            MatchLogOutcome outcome = MatchLogOutcome.None,
            int amount = 0,
            HeroId blockingHeroId = default,
            CardInstanceId blockingCardInstanceId = default)
        {
            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence),
                    "A match-log entry keeps the rule-event sequence it was derived from, which is one-based.");
            }

            if (kind == MatchLogEntryKind.None)
            {
                throw new ArgumentException(
                    "A match-log entry requires a named kind. The absence of a kind is how the " +
                    "selection contract excludes an event, so an entry may never carry None.",
                    nameof(kind));
            }

            Sequence = sequence;
            Kind = kind;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            SubjectHeroId = subjectHeroId;
            TargetHeroId = targetHeroId;
            SubjectMinionId = subjectMinionId;
            SubjectCardInstanceId = subjectCardInstanceId;
            SubjectTeamId = subjectTeamId;
            Origin = origin;
            Destination = destination;
            HasOrigin = hasOrigin;
            HasDestination = hasDestination;
            Outcome = outcome;
            Amount = amount;
            BlockingHeroId = blockingHeroId;
            BlockingCardInstanceId = blockingCardInstanceId;
        }

        /// <summary>
        /// The sequence of the rule-event this line was derived from. Ordering and every
        /// idempotent merge key on it, and it is why a rewind is expressible as a truncation.
        /// </summary>
        public int Sequence { get; }

        public MatchLogEntryKind Kind { get; }

        public int RoundIndex { get; }

        public int TurnIndex { get; }

        /// <summary>The hero the line is about, where the line names one.</summary>
        public HeroId SubjectHeroId { get; }

        /// <summary>The hero on the receiving end of an attack or defense.</summary>
        public HeroId TargetHeroId { get; }

        /// <summary>The minion the line is about - a target, a defeat, a Minion Battle removal.</summary>
        public MinionId SubjectMinionId { get; }

        /// <summary>
        /// The card the line names: the Skill's own card, the revealed commitment, the card upgraded
        /// or the item gained. Every one of these is a card the table can already see.
        /// </summary>
        public CardInstanceId SubjectCardInstanceId { get; }

        /// <summary>The team a life-counter or elimination line is about.</summary>
        public TeamId SubjectTeamId { get; }

        public HexCoordinate Origin { get; }

        public HexCoordinate Destination { get; }

        /// <summary>Whether <see cref="Origin"/> was stated; a default coordinate is a real space.</summary>
        public bool HasOrigin { get; }

        /// <summary>Whether <see cref="Destination"/> was stated.</summary>
        public bool HasDestination { get; }

        public MatchLogOutcome Outcome { get; }

        /// <summary>
        /// The one number the line carries, in that line's own units: the Level bought, the coins
        /// collected, or the Life counters spent. Zero where the kind names no quantity.
        /// </summary>
        public int Amount { get; }

        /// <summary>
        /// Milestone 8.35z14 (the blocker-identity channel). On a <see cref="MatchLogEntryKind.BlockedAction"/>
        /// line, the hero controlling the standing rule modification that refused the step - the
        /// undefined hero where the refusing node could not explain itself, which is a real, honest
        /// state and never papered over with a guess. Privacy is unaffected: the blocking source is
        /// always a standing PUBLIC Active effect (rulebook p.17), so naming it reveals nothing a
        /// viewer's own projection does not already carry as one of <c>ActiveEffectSourceCardInstanceIds</c>.
        /// </summary>
        public HeroId BlockingHeroId { get; }

        /// <summary>Milestone 8.35z14. The card the blocking modification's source came off.</summary>
        public CardInstanceId BlockingCardInstanceId { get; }

        public bool Equals(MatchLogEntry other)
        {
            return other != null &&
                Sequence == other.Sequence &&
                Kind == other.Kind &&
                RoundIndex == other.RoundIndex &&
                TurnIndex == other.TurnIndex &&
                SubjectHeroId.Equals(other.SubjectHeroId) &&
                TargetHeroId.Equals(other.TargetHeroId) &&
                SubjectMinionId.Equals(other.SubjectMinionId) &&
                SubjectCardInstanceId.Equals(other.SubjectCardInstanceId) &&
                SubjectTeamId.Equals(other.SubjectTeamId) &&
                HasOrigin == other.HasOrigin &&
                HasDestination == other.HasDestination &&
                (!HasOrigin || Origin.Equals(other.Origin)) &&
                (!HasDestination || Destination.Equals(other.Destination)) &&
                Outcome == other.Outcome &&
                Amount == other.Amount &&
                BlockingHeroId.Equals(other.BlockingHeroId) &&
                BlockingCardInstanceId.Equals(other.BlockingCardInstanceId);
        }

        public override bool Equals(object obj) => Equals(obj as MatchLogEntry);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Sequence;
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ RoundIndex;
                hashCode = (hashCode * 397) ^ TurnIndex;
                hashCode = (hashCode * 397) ^ (int)Outcome;
                hashCode = (hashCode * 397) ^ Amount;
                return hashCode;
            }
        }

        public override string ToString() =>
            $"{Sequence:0000} | {Kind} | round={RoundIndex};turn={TurnIndex}";
    }
}
