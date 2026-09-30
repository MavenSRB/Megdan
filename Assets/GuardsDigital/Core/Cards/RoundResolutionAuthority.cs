using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Cards
{
    public enum RoundResolutionPhase
    {
        AwaitingCommitments = 0,
        ResolvingTurn = 1,
        EndOfRound = 2,
        GameEnded = 3
    }

    public sealed class RoundResolutionAuthority
        : GuardsDigital.Core.Rules.Victory.ISoloVictoryClaim,
            GuardsDigital.Core.Setup.IMatchTieBreakerCoin,
            IResolvedTurnSlotSwap, IItemCardSwap
    {
        public const int TurnsPerRound = 4;

        /// <summary>
        /// The Life counters a team begins with when no composed setup states otherwise. It is the
        /// prototype baseline in <c>RULES_LEDGER.md</c> - "Five Life counters per team" - rather than a
        /// number invented here, and it applies only to a composition that supplied no setup at all.
        /// </summary>
        public const int BaselineLifeCountersPerTeam = 5;

        /// <summary>
        /// Rulebook p.8: an eligible hero collects "1 coin". The number is stated once here rather than
        /// written at the grant, because it is the page's and not an implementation detail.
        /// </summary>
        public const int PityCoinsPerRound = 1;

        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly MatchParticipants participants;
        private readonly CardActionResolutionAuthority cardActions;
        private readonly List<RoundTurnSlot> turnSlots = new List<RoundTurnSlot>();
        private readonly List<CompletedRoundRecord> completedRounds = new List<CompletedRoundRecord>();
        private readonly List<EndOfRoundStageOutcome> endOfRoundOutcomes =
            new List<EndOfRoundStageOutcome>();
        private readonly EndOfRoundStageAvailability endOfRoundStageAvailability;
        private readonly IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers;
        private readonly AdjacentMoveAuthority board;
        private TieBreakerState tieBreakerState;

        /// <summary>
        /// Milestone 8.19c1. Whether this round has already run the moment before its Minion Battle is
        /// planned. Reset when the next round begins, because the moment belongs to a round.
        /// </summary>
        private bool preMinionBattleTriggersVisited;
        private readonly ActiveEffectLifecycle timing;
        private readonly MarkerAttachmentRegister markers;
        private readonly GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority systemTransitions;
        private readonly GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush;
        private readonly GuardsDigital.Core.Rules.Progression.HeroLevelUpStepRules levelUp;

        public RoundResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            int roundIndex,
            RoundResolutionComposition composition)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var cardActions = composition.CardActions;
            var board = composition.Board;
            var timing = composition.Timing;
            var markers = composition.Markers;
            var compositionTransitions = composition.SystemTransitions;
            var compositionLanePush = composition.LanePush;
            var compositionLevelUp = composition.LevelUp;
            var compositionTeamLives = composition.TeamLives;
            var compositionStartingProgression = composition.StartingProgression;
            var compositionStartingCoins = composition.StartingCoins;
            var endOfRoundStageAvailability = composition.EndOfRoundStageAvailability;
            var endOfRoundHandlers = composition.EndOfRoundHandlers;

            this.definitions = Array.AsReadOnly(
                ActionOfferQuery.BuildDefinitionLookup(definitions).Values.ToArray());
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) ||
                cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Round card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            this.instances = Array.AsReadOnly(cards);
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            foreach (var card in cards)
            {
                if (!this.definitions.Any(definition => definition.Reference.Equals(card.DefinitionReference)))
                {
                    throw new ArgumentException(
                        "Every round card must reference a current definition.",
                        nameof(instances));
                }

                participants.GetHero(card.OwnerId);
            }

            if (!initialTieBreakerState.IsDefined)
            {
                throw new ArgumentException("Round authority requires Tie Breaker state.", nameof(initialTieBreakerState));
            }

            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            tieBreakerState = initialTieBreakerState;
            this.cardActions = cardActions;
            this.board = board;
            this.timing = timing ?? new ActiveEffectLifecycle();
            this.markers = markers ?? new MarkerAttachmentRegister();

            // An absent transition authority is substituted with one over the empty catalog rather than
            // left null, so every caller can ask where a transition is stopped and get the honest "no
            // transition exists in this build" instead of a reference check.
            this.systemTransitions = compositionTransitions ??
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority(
                    GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog.Empty);

            // Milestone 6.11. Absent outside a configured match, where there is no Lane to push and no
            // Wave counters to spend; the two consumption checkpoints say so rather than substituting a
            // push nothing could win.
            lanePush = compositionLanePush;

            // Milestone 7.1. p.15's "Your hero starts the game at level 1" applied to every hero the
            // match has, at the moment the round authority knows who they are. Restore replaces it
            // below rather than mutating it, so a restored match never passes through Level 1 on its
            // way to the Levels it recorded.
            // Milestone 8.35z17. A composed starting position replaces that default rather than
            // mutating it, exactly as the restore below replaces it and for the same stated reason: a
            // staged match must never pass through Level 1 on its way to the Levels it was staged at.
            // Absent - which is every caller but the sandbox composition - this is unchanged.
            HeroProgression = compositionStartingProgression != null
                ? GuardsDigital.Core.Rules.Progression.HeroProgressionState.Restore(
                    participants, compositionStartingProgression)
                : new GuardsDigital.Core.Rules.Progression.HeroProgressionState(participants);

            // Milestone 7.2. p.16 gains an item during the Upgrade a Card step of a level purchase, and
            // p.15 makes that purchase an end-of-round step, which is this authority's boundary - the
            // same reason the Level above it lives here. Every hero starts with none, and nothing in this
            // build gains one, because the purchase that would is milestone 7.5's.
            HeroItems = new GuardsDigital.Core.Rules.Progression.HeroItemState(participants);

            // Milestone 7.4. p.13's Life counters, which a hero Defeat spends and whose exhaustion ends
            // the game. An absent composition is substituted with the prototype baseline's five per
            // team, the count `RULES_LEDGER.md` records for the official setup, in the shape the marker
            // register and the timing lifecycle are substituted: a fixture without a composed setup
            // still has counters, rather than a null every consumer would have to check.
            TeamLives = compositionTeamLives ?? new GuardsDigital.Core.Rules.Consequences.TeamLifeState(
                participants, BaselineLifeCountersPerTeam);

            // Milestone 7.6. p.8's Pity Coin asks who did not level up this round, so the round records
            // who did. It is created here rather than composed because there is nothing to compose: a
            // round begins with nobody having levelled, and unlike the Life counters beside it no setup
            // states otherwise.
            RoundLeveling = new GuardsDigital.Core.Rules.Progression.RoundLevelingRecord(participants);

            // Milestone 8.19a1. Brogan's coin-granting cards ask whether any hero was defeated this
            // round. Coins and Life are cumulative and the event stream is diagnostic, so the typed
            // p.13 outcomes have one round-owned record of their own.
            HeroDefeatHistory = new RoundHeroDefeatHistory(participants);

            // Milestone 8.53. The victory screen's per-hero board. Held here for the reason the
            // Levels, the items and the Life counters are: it is MATCH-scoped authoritative state
            // whose only writers are rules this round's own graph reaches, and the round's snapshot
            // section is where it is persisted. It is emphatically not the round-scoped Defeat
            // history above it - that record is reset at every round boundary because p.8's question
            // is about this round, and a scoreboard that reset would print zeroes at the moment the
            // match ended.
            Scoreboard = new GuardsDigital.Core.Rules.Victory.MatchScoreboard(participants);

            // One state, shared with the dispatcher whose Defense total reads it - the same arrangement
            // the marker register has, and for the same reason.
            this.cardActions?.BindHeroItems(HeroItems);

            // Milestone 8.38c1: the same arrangement a third time. p.13's Defeat is what spends a
            // standing minion bounty, and the Defeat is resolved inside the dispatcher.
            this.cardActions?.BindActiveEffects(timing);

            // Milestone 7.4: the same arrangement again, for the two states p.13's Defeat consequences
            // read and write inside the Attack that caused them.
            this.cardActions?.BindDefeatConsequenceState(HeroProgression, TeamLives);
            this.cardActions?.BindHeroDefeatHistory(HeroDefeatHistory);

            // Milestone 7.5. p.15's level purchase spends from the same balances p.13's Defeat fills, so
            // the ledger is asked for here rather than left to be created by whichever rule reaches it
            // first: two ledgers would be two answers to what a hero can afford, and only one of them
            // would be on the wire. The leveling rules are then handed the three states a purchase
            // moves - the Level, the items and those balances - in the shape the two bindings above use
            // and for the same reason: this authority owns all three and the transition authority the
            // rules are composed into is built before it.
            levelUp = compositionLevelUp;
            // Milestone 8.35z17. The staged balances seed the one ledger rather than creating a
            // second: the dispatcher is composed before this authority and has not been asked for a
            // ledger yet, so the seed is what the lazy creation below produces. Two ledgers would be
            // two answers to what a hero can afford, which is the one thing that must not happen -
            // so the seed goes through the same single creation point every other caller uses.
            var roundCoinLedger =
                this.cardActions?.EnsureCoinLedger(participants, compositionStartingCoins) ??
                (compositionStartingCoins != null
                    ? GuardsDigital.Core.Rules.Consequences.HeroCoinLedger.Restore(
                        participants, compositionStartingCoins)
                    : new GuardsDigital.Core.Rules.Consequences.HeroCoinLedger(participants));

            // Milestone 8.53. The one ledger is handed the one scoreboard, so that every coin it
            // credits - p.13's Defeat and assist coins, a minion's bounty, p.8's Pity Coin and card
            // text's own grants - is counted once, at the single place a balance goes up.
            roundCoinLedger.BindScoreboard(Scoreboard);
            levelUp?.Bind(
                HeroProgression,
                HeroItems,
                roundCoinLedger);
            // Milestone 8.39g1 hands the round's own solo-victory claim to the effect frame beside the
            // three authorities it already carries, which is how a printed card reaches an ending it
            // otherwise could not name. It is `this` rather than a delegate because the claim is this
            // authority's own state and the interface is already the narrowing.
            this.cardActions?.BindRosterState(EffectRosterState.Of(
                turnSlots.AsReadOnly(), HeroDefeatHistory, roundCoinLedger, this, this, this));

            // Milestone 8.75a. The same arrangement for the match's one Tie Breaker coin: this
            // authority is what answers which face is showing at every moment a turn is not running,
            // and the turn re-binds itself when one starts. Ignatia the Mad's every action reads it.
            this.cardActions?.BindTieBreakerCoin(this);
            this.endOfRoundStageAvailability = endOfRoundStageAvailability ??
                (board != null && GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .TryGetCurrentBattleZone(board, out _)
                    ? EndOfRoundStageAvailability.ConfiguredMatch()
                    : EndOfRoundStageAvailability.SyntheticRepeatedRound());
            var handlers = endOfRoundHandlers != null
                ? new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>(endOfRoundHandlers)
                : new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    {
                        EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                        new CardRetrievalHandler()
                    }
                };
            if (handlers.Any(pair =>
                    pair.Key == EndOfRoundStage.Complete ||
                    pair.Value == null))
            {
                throw new ArgumentException(
                    "End-of-round handlers require executable stages and non-null handlers.",
                    nameof(endOfRoundHandlers));
            }

            this.endOfRoundHandlers = new System.Collections.ObjectModel.ReadOnlyDictionary<
                EndOfRoundStage,
                IEndOfRoundStageHandler>(handlers);
            RoundIndex = roundIndex;
            TurnIndex = 1;
            Phase = RoundResolutionPhase.AwaitingCommitments;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(this.instances, this.participants);
        }

        public int RoundIndex { get; private set; }

        public int TurnIndex { get; private set; }

        public RoundResolutionPhase Phase { get; private set; }

        public TieBreakerState TieBreakerState =>
            CurrentTurnAuthority?.TieBreakerState ?? tieBreakerState;

        /// <summary>
        /// Milestone 8.75a. Turns the match's coin over for a card that prints it, from whichever of
        /// the two authorities is holding it: the turn's, while one is running, and the round's own
        /// between turns. It is the same read the property above makes, written once more as a write.
        /// </summary>
        TeamSide GuardsDigital.Core.Setup.IMatchTieBreakerCoin.FlipTieBreaker()
        {
            if (CurrentTurnAuthority != null)
            {
                return ((GuardsDigital.Core.Setup.IMatchTieBreakerCoin)CurrentTurnAuthority)
                    .FlipTieBreaker();
            }

            tieBreakerState = new TieBreakerState(
                tieBreakerState.RevealedSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red);
            return tieBreakerState.RevealedSide;
        }

        public IReadOnlyList<RoundTurnSlot> TurnSlots => turnSlots.AsReadOnly();

        /// <summary>
        /// A printed card exchange changes both card lifecycle and the numbered slot's identity.
        /// Revalidate the recorded assignment and both resting states when a paused answer arrives.
        /// </summary>
        public bool TrySwapResolvedWithHand(
            HeroId owner, CardInstanceId resolvedCard, CardInstanceId handCard)
        {
            if (!owner.IsDefined || !resolvedCard.IsDefined || !handCard.IsDefined ||
                resolvedCard.Equals(handCard) ||
                Phase != RoundResolutionPhase.ResolvingTurn)
                return false;

            var oldCard = instances.SingleOrDefault(card => card.Id.Equals(resolvedCard));
            var newCard = instances.SingleOrDefault(card => card.Id.Equals(handCard));
            if (oldCard == null || newCard == null ||
                !oldCard.OwnerId.Equals(owner) || !newCard.OwnerId.Equals(owner) ||
                !CardLifecycleAuthority.IsResolvedCardInTurnArea(oldCard) ||
                newCard.State.Location != CardLocation.Hand ||
                newCard.State.HasBeenPlayed ||
                newCard.State.ResolutionStatus != CardResolutionStatus.None)
                return false;

            var matching = turnSlots
                .Select((slot, index) => new { slot, index })
                .Where(entry => entry.slot.RoundIndex == RoundIndex &&
                    entry.slot.Assignments.Any(assignment =>
                        assignment.HeroId.Equals(owner) &&
                        assignment.CardInstanceId.HasValue &&
                        assignment.CardInstanceId.Value.Equals(resolvedCard)))
                .ToArray();
            if (matching.Length != 1)
                return false;

            var oldSlot = matching[0].slot;
            var newSlot = new RoundTurnSlot(
                oldSlot.RoundIndex, oldSlot.TurnIndex,
                oldSlot.Assignments.Select(assignment =>
                    assignment.HeroId.Equals(owner)
                        ? new TurnCardAssignment(owner, handCard, false)
                        : assignment),
                instances, participants);
            if (!CardLifecycleAuthority.TrySwapResolvedWithHand(oldCard, newCard))
                return false;
            turnSlots[matching[0].index] = newSlot;
            return true;
        }

        public bool TrySwapItemWithCard(
            HeroId owner, CardInstanceId itemCard, CardInstanceId replacementCard)
        {
            if (!owner.IsDefined || !itemCard.IsDefined || !replacementCard.IsDefined ||
                itemCard.Equals(replacementCard) ||
                Phase != RoundResolutionPhase.ResolvingTurn || HeroItems == null)
                return false;
            var oldItemCard = instances.SingleOrDefault(card => card.Id.Equals(itemCard));
            var newItemCard = instances.SingleOrDefault(card => card.Id.Equals(replacementCard));
            if (oldItemCard == null || newItemCard == null ||
                !oldItemCard.OwnerId.Equals(owner) ||
                !newItemCard.OwnerId.Equals(owner) ||
                !HeroItems.CanSwapItemWithCard(
                    oldItemCard, newItemCard, definitions))
                return false;

            var resolved = CardLifecycleAuthority.IsResolvedCardInTurnArea(newItemCard);
            RoundTurnSlot newSlot = null;
            var slotIndex = -1;
            if (resolved)
            {
                var matches = turnSlots
                    .Select((slot, index) => new { slot, index })
                    .Where(entry => entry.slot.RoundIndex == RoundIndex &&
                        entry.slot.Assignments.Any(assignment =>
                            assignment.HeroId.Equals(owner) &&
                            assignment.CardInstanceId.HasValue &&
                            assignment.CardInstanceId.Value.Equals(replacementCard)))
                    .ToArray();
                if (matches.Length != 1)
                    return false;
                slotIndex = matches[0].index;
                var oldSlot = matches[0].slot;
                newSlot = new RoundTurnSlot(
                    oldSlot.RoundIndex, oldSlot.TurnIndex,
                    oldSlot.Assignments.Select(assignment =>
                        assignment.HeroId.Equals(owner)
                            ? new TurnCardAssignment(owner, itemCard, false)
                            : assignment),
                    instances, participants);
            }
            if (!HeroItems.TrySwapItemWithCard(
                oldItemCard, newItemCard, definitions))
                return false;
            if (resolved)
                turnSlots[slotIndex] = newSlot;
            return true;
        }

        /// <summary>
        /// EVERY completed round of this match, in order. Milestone 8.35w bounded what
        /// the READ MODEL exposes to a retained window, because that is what one session frame has
        /// to carry; the authority is deliberately not bounded, because the rules, the restore state
        /// and the authoritative snapshot are entitled to the whole history. Read this, not the
        /// read model's <c>CompletedRounds</c>, wherever the question is about the match rather than
        /// about what a seat is shown.
        /// </summary>
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => completedRounds.AsReadOnly();

        /// <summary>
        /// Typed p.13 hero-Defeat outcomes in the current round. This is rules authority; the event log
        /// remains diagnostic and is never queried to resolve card text.
        /// </summary>
        public RoundHeroDefeatHistory HeroDefeatHistory { get; private set; }

        public EndOfRoundStage? CurrentEndOfRoundStage { get; private set; }

        public IReadOnlyList<EndOfRoundStageOutcome> EndOfRoundOutcomes =>
            endOfRoundOutcomes.AsReadOnly();

        public PendingMinionBattleChoice PendingMinionBattleChoice { get; private set; }

        public PendingLanePushRequest PendingLanePushRequest { get; private set; }

        /// <summary>
        /// Milestone 6.4a's paused after-action correction, or <c>null</c>. Milestone 7.7: and
        /// <c>null</c> once the match has ended, beside the turn's own stopped decisions - p.18's
        /// correction belongs to an action that will never finish.
        /// </summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => Phase == RoundResolutionPhase.GameEnded
                ? null
                : CardActions?.PendingDisplacedMinionCorrection;

        public TurnCardCommitmentAuthority CurrentCommitmentAuthority { get; private set; }

        public TurnResolutionAuthority CurrentTurnAuthority { get; private set; }

        internal IReadOnlyList<CardDefinition> Definitions => definitions;

        internal IReadOnlyList<CardInstance> Instances => instances;

        internal MatchParticipants Participants => participants;

        internal CardActionResolutionAuthority CardActions => cardActions;

        /// <summary>
        /// Milestone 8.19c1's Defeat standing at its removal stage, or <c>null</c>.
        ///
        /// The round reads it rather than owning it, the way it reads the mid-turn Lane-Push request:
        /// the dispatcher records it, and what the round adds is that a decision standing here belongs
        /// to whichever hero's source is offering rather than to whoever is acting.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.PendingRemovalPreventionDecision
            PendingRemovalPrevention => cardActions?.PendingRemovalPrevention;

        public ActiveEffectLifecycle Timing => timing;

        /// <summary>
        /// Milestone 7.1's per-hero Level, held here because p.15 changes a Level at the end of a
        /// round, which is this authority's boundary, and because it is round-scoped state the round's
        /// restore state already carries the rest of.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroProgressionState HeroProgression { get; private set; }

        /// <summary>
        /// Milestone 7.2's per-hero items, held beside <see cref="HeroProgression"/> because p.16 gains
        /// one as part of the level purchase that Level records.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems { get; private set; }

        /// <summary>
        /// Milestone 7.4's per-team Life counters. Held here for the reason Level and items are: it is
        /// match-scoped authoritative state whose only writer is a rule the round's own graph reaches,
        /// and the round's snapshot section is where it is persisted.
        ///
        /// p.13's losing team is not a field beside it. A team at zero counters <em>is</em> that fact,
        /// and <see cref="Phase"/> records that the game ended, so a third record could only agree with
        /// the counters or be wrong about them - milestone 7.3's argument for deriving the unlocked
        /// Ultimate from the card's own position.
        /// </summary>
        public TeamLifeState TeamLives { get; private set; }

        /// <summary>
        /// Milestone 8.53's per-hero match scoreboard - the numbers the victory screen prints.
        /// Match-scoped, held here for the reason the Levels, the items and the Life counters above
        /// it are, and persisted in this authority's own snapshot section.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.MatchScoreboard Scoreboard { get; private set; }

        /// <summary>
        /// Milestone 7.6's record of which heroes bought a level this round, held beside the Levels it
        /// is about. p.8's Pity Coin is the only rule that reads it, and it reads the negation: "All
        /// players who have NOT leveled up this round collect 1 coin."
        ///
        /// It is round-scoped rather than match-scoped, which is the one thing that distinguishes it
        /// from every other progression state on this authority: it is emptied when the round closes,
        /// beside the turn slots and stage outcomes that are cleared there for the same reason.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.RoundLevelingRecord RoundLeveling { get; private set; }

        /// <summary>
        /// Milestone 7.3's unlocked Ultimates, derived from this match's own card instances whenever a
        /// modification set is built.
        ///
        /// Unlike Level and items, this is a query rather than a held state, because rulebook p.16
        /// records the unlock by moving the card - "Place it Faceup to the left side of your hero
        /// dashboard" - and the card's position is already authoritative and already persisted. A second
        /// per-hero record beside it could only agree with the card or be wrong about it.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet CurrentUltimates() =>
            GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(definitions, instances);

        /// <summary>
        /// Milestone 8.19c. The declared Minion-Battle rules applicable right now, derived from those
        /// same unlocked Ultimates and the board - empty on a board-free round, which has no Battle
        /// Zone for a rule to be in.
        ///
        /// Derived at every ask, like the Ultimates it reads, so a hero who left the board between the
        /// battle being planned and being completed supplies nothing to either.
        /// </summary>
        internal IReadOnlyList<GuardsDigital.Core.Rules.Consequences.MinionBattleParticipant>
            CurrentMinionBattleRules() =>
            board == null
                ? Array.Empty<GuardsDigital.Core.Rules.Consequences.MinionBattleParticipant>()
                : GuardsDigital.Core.Rules.Progression.MinionBattleRuleSourceQuery.Derive(
                    CurrentUltimates(), board, participants);

        /// <summary>
        /// Milestone 7.7. Who won this match, and which of the rulebook's three victory sentences
        /// decided it - or <see cref="MatchOutcome.Unfinished"/> while it is still being played.
        ///
        /// Derived, like <see cref="CurrentUltimates"/> and for the same reason: p.8's ending is
        /// already recorded on the Lane-Push transition and p.13's is already recorded in the Life
        /// counters, both of them persisted, so a fourth record could only agree with them or be wrong.
        /// <see cref="Phase"/> supplies the one thing neither of them says, which is whether the match
        /// has stopped.
        ///
        /// It throws rather than reporting a fault, because every fault the reading can produce is a
        /// claim about state this authority owns: a live match ends once, at one of three seams that
        /// all run through <see cref="EndGame"/>, so a second ending or an unexplained one would be an
        /// engine defect. A payload can still claim one, and the snapshot reader rejects it there.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome => ReadOutcome().Outcome;

        /// <summary>
        /// The hero who won the match alone, or the named absence. Milestone 8.39g: the one victory
        /// fact this build records rather than derives - see
        /// <see cref="RoundCursorRestoreState.SoloVictoryHeroId"/> for why it has to be.
        /// </summary>
        public GuardsDigital.Core.Units.Heroes.HeroId SoloVictoryHeroId { get; private set; }

        /// <summary>
        /// Records that one hero has won the match alone, and ends it. Milestone 8.39g, and Cutter's
        /// A Fistful of Coins is the only sentence that calls it.
        ///
        /// <para><b>It goes through <see cref="EndGame"/> like every other ending</b>, passing no
        /// end-of-round stage - which is exactly the shape milestone 7.7 gave that parameter for p.8's
        /// mid-turn push, so that "where did the match end" is answered with where it ended rather
        /// than with a stage it never reached. The turn's in-flight resolution is stopped by the same
        /// chokepoint, and nothing is cleared, because the position the match ended in is what the
        /// payload keeps.</para>
        ///
        /// <para>A second claim is refused rather than layered: the phase already records an ending,
        /// and two endings cannot both be the ending.</para>
        /// </summary>
        public bool TryRecordSoloVictory(
            GuardsDigital.Core.Units.Heroes.HeroId winningHeroId,
            out RoundResolutionEvent gameEnded)
        {
            gameEnded = default;
            if (!TryClaim(winningHeroId))
            {
                return false;
            }

            gameEnded = EndGame(null);
            return true;
        }

        /// <summary>Milestone 8.39g1. Whether this match has already been claimed by somebody.</summary>
        public bool HasBeenClaimed => SoloVictoryHeroId.IsDefined;

        /// <summary>
        /// Milestone 8.39g1. Records the hero who won alone, and does NOT end the match - the half of
        /// <see cref="TryRecordSoloVictory"/> that a printed card can reach.
        ///
        /// <para><b>The split is what lets card text claim the victory at all.</b> An effect capability
        /// is handed no route to this authority except
        /// <see cref="GuardsDigital.Core.Effects.EffectRosterState"/>, which carries the round-owned
        /// authorities a roster capability may read or mutate; a claim is one more of those. What it
        /// must not do from in there is call <see cref="EndGame"/>, which stops the turn's in-flight
        /// resolution - and the resolution still running is the one making the claim. So this writes
        /// the fact and returns, and <see cref="AnUnrecordedEndingExists"/> stops the match at the end
        /// of the same turn command, which is the seam milestone 8.16 widened to notice exactly this.
        /// </para>
        ///
        /// <para>The refusals are <see cref="TryRecordSoloVictory"/>'s own, unchanged and now stated
        /// once: an ended match, an already-claimed one, an undefined hero, and a hero this match is
        /// not playing.</para>
        /// </summary>
        public bool TryClaim(GuardsDigital.Core.Units.Heroes.HeroId winningHeroId)
        {
            if (!winningHeroId.IsDefined ||
                Phase == RoundResolutionPhase.GameEnded ||
                SoloVictoryHeroId.IsDefined ||
                !participants.Heroes.Any(hero => hero.Id.Equals(winningHeroId)))
            {
                return false;
            }

            SoloVictoryHeroId = winningHeroId;
            return true;
        }

        private GuardsDigital.Core.Rules.Victory.MatchOutcomeReading ReadOutcome(
            bool gameIsRecordedAsEnded)
        {
            var reading = GuardsDigital.Core.Rules.Victory.MatchOutcomeQuery.Read(
                participants, lanePush?.LanePush, TeamLives, gameIsRecordedAsEnded,
                SoloVictoryHeroId);
            if (!reading.IsCoherent)
            {
                throw new InvalidOperationException(
                    $"Authoritative state describes no single ending for this match ({reading.Fault}).");
            }

            return reading;
        }

        private GuardsDigital.Core.Rules.Victory.MatchOutcomeReading ReadOutcome() =>
            ReadOutcome(Phase == RoundResolutionPhase.GameEnded);

        /// <summary>
        /// Milestone 8.16. Whether the authoritative state already describes one of the rulebook's
        /// three endings that <see cref="Phase"/> has not recorded yet.
        ///
        /// It asks the same reading <see cref="Outcome"/> asks, with the phase answered as "not
        /// recorded", and looks for exactly the one fault that means "an ending happened here":
        /// <c>UnrecordedGameEnd</c>. The other faults stay faults - two endings at once is an engine
        /// defect whether or not a turn command produced it, and ending the game on one of them would
        /// pick a winner by accident - so they are left for <see cref="Outcome"/> to throw on.
        ///
        /// This is deliberately a question about state rather than about which rule ran. A turn
        /// command can end the match by p.13's counters or by either of p.8's push sentences, and the
        /// list of rules that can do it grows with the content; the list of endings does not.
        /// </summary>
        private bool AnUnrecordedEndingExists() =>
            GuardsDigital.Core.Rules.Victory.MatchOutcomeQuery
                .Read(
                    participants,
                    lanePush?.LanePush,
                    TeamLives,
                    gameIsRecordedAsEnded: false,
                    // Milestone 8.39g1. This overload was called WITHOUT the solo winner, while
                    // `ReadOutcome` two methods up passed it - so the one seam that notices every
                    // mid-turn ending was the one place blind to the fact milestone 8.39g added, and a
                    // match won outright went on asking its winner for their next command. The check's
                    // own remarks already said what it was for: "the list of rules that can end the
                    // match grows with the content; the list of endings does not."
                    SoloVictoryHeroId)
                .Fault == GuardsDigital.Core.Rules.Victory.MatchOutcomeFault.UnrecordedGameEnd;

        /// <summary>Milestone 6.2's marker attachments, held beside <see cref="Timing"/>.</summary>
        public MarkerAttachmentRegister Markers => markers;

        /// <summary>Milestone 6.10a's system-transition continuation, held beside <see cref="Markers"/>.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority SystemTransitions =>
            systemTransitions;

        /// <summary>
        /// Milestone 6.11's Lane-Push transition, or <c>null</c> in a composition with no Lane to push.
        /// </summary>
        public GuardsDigital.Core.Rules.LanePush.LanePushConsumption LanePush => lanePush;

        /// <summary>
        /// Milestone 7.8a. Every hero's current coin balance, ordered by hero id, as a read.
        ///
        /// The balances alone, not the ledger. p.13's award, p.15's spend and p.8's grant are the only
        /// three things that may move a coin, and all three already live behind this authority; a reader
        /// that could reach the ledger itself could become a fourth. Empty in a composition with no card
        /// actions behind it, which is the honest answer for a fixture that has no ledger at all.
        /// </summary>
        public IReadOnlyList<KeyValuePair<HeroId, int>> CoinBalances =>
            cardActions?.CoinLedger?.CoinBalances ??
            (IReadOnlyList<KeyValuePair<HeroId, int>>)Array.Empty<KeyValuePair<HeroId, int>>();

        /// <summary>
        /// Begins a declared multi-step system transition (milestone 6.10a).
        ///
        /// The instance identity is composed from the round cursor and the definition rather than minted
        /// from a counter, so a replay of the same command prefix produces the same identity: a counter
        /// would rebuild an equal match under different names and break the byte-identity the aggregate
        /// invariance fixture pins.
        ///
        /// This exists because a bounded synthetic transition has no rules of its own to be begun by.
        /// Milestone 6.11's Lane-Push respawn and milestone 7.5's progression begin theirs from their
        /// own rules and do not reach this path.
        /// </summary>
        public RoundResolutionResult TryBeginSystemTransition(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId definitionId)
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            // Milestone 7.9c, finding F-PROG-01. The paragraph above says these two do not reach this
            // path; until now nothing made that true, and a command naming either ran a rulebook step
            // outside the sequence that owns its bookkeeping - p.15's purchase without
            // `DrainLevelUpEvents`, so `RoundLeveling` never learned of it and p.8's Pity Coin was then
            // paid to the hero that had just levelled and withheld from it a round later. The Lane-Push
            // arm faulted instead, which the aggregate rolled back; a rule that holds is better than a
            // throw that is caught.
            if (definitionId.Equals(GuardsDigital.Core.Rules.LanePush.LanePushSystemTransition.DefinitionId) ||
                definitionId.Equals(
                    GuardsDigital.Core.Rules.Progression.HeroLevelUpSystemTransition.DefinitionId))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.SystemTransitionNotBegunByCommand);
            }

            var instanceId = new GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId(
                $"round-{RoundIndex}-turn-{TurnIndex}-{definitionId.Value}");
            var result = systemTransitions.TryBegin(definitionId, instanceId);
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(TranslateTransitionRejection(result.RejectionReason));
            }

            return RoundResolutionResult.Accept(new[]
            {
                RoundResolutionEvent.SystemTransitionProgressed(RoundIndex, TurnIndex, result)
            });
        }

        /// <summary>
        /// Answers the paused step of the running system transition (milestone 6.10a), and closes the
        /// Lane Push behind it when that answer was the last one it was waiting for (milestone 6.11).
        /// </summary>
        public RoundResolutionResult TryAnswerSystemTransitionStep(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand command)
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            var wasLanePush = lanePush != null && lanePush.IsPaused;
            var wasLevelUp = IsPausedLevelUp();
            var result = systemTransitions.TryAnswer(command);
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(TranslateTransitionRejection(result.RejectionReason));
            }

            // Milestone 7.5. What the answered upgrade did is reported before where the transition now
            // is, so the stream reads in the order the round happened.
            var events = wasLevelUp ? DrainLevelUpEvents() : new List<RoundResolutionEvent>();
            events.Add(RoundResolutionEvent.SystemTransitionProgressed(RoundIndex, TurnIndex, result));
            if (wasLevelUp)
            {
                return result.TransitionCompleted &&
                    Phase == RoundResolutionPhase.EndOfRound &&
                    CurrentEndOfRoundStage == EndOfRoundStage.LevelUpAndUpgrade
                    ? CompleteEndOfRoundStage(
                        EndOfRoundStage.LevelUpAndUpgrade,
                        EndOfRoundStageOutcomeKind.Completed,
                        events)
                    : RoundResolutionResult.Accept(events);
            }

            if (!wasLanePush || !result.TransitionCompleted)
            {
                return RoundResolutionResult.Accept(events);
            }

            return CloseFinishedLanePush(lanePush.ReportAfterAnswer(), events);
        }

        /// <summary>
        /// p.8's end-of-round push, at the stage milestone 6.1 built and left refusing.
        ///
        /// A push that stops on the sidebar's Place leaves the stage where it is, so the round waits for
        /// the answer rather than advancing past a transition that has not finished. A second advance
        /// while one is stopped is refused for the same reason.
        /// </summary>
        private RoundResolutionResult ResolveRequestedLanePush(EndOfRoundStage stage)
        {
            if (lanePush == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.UnavailableLanePushTransition);
            }

            if (lanePush.IsRunning)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteLanePushTransition);
            }

            var request = PendingLanePushRequest;
            var consumption = lanePush.TryConsume(
                request.RequestId, request.WinningSide, request.LosingSide, TieBreakerState);
            if (!consumption.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.RefusedLanePushTransition);
            }

            var events = new List<RoundResolutionEvent>();
            if (consumption.IsPaused)
            {
                events.Add(RoundResolutionEvent.SystemTransitionPaused(
                    RoundIndex, TurnIndex, consumption.PendingStep));
                return CompleteEndOfRoundStage(stage, EndOfRoundStageOutcomeKind.Paused, events);
            }

            return CloseFinishedLanePush(consumption, events);
        }

        /// <summary>
        /// Everything both checkpoints do once a push has finished: forget the request it consumed,
        /// report p.8's stages, and end the game if one of the page's two victory sentences did.
        /// </summary>
        private RoundResolutionResult CloseFinishedLanePush(
            GuardsDigital.Core.Rules.LanePush.LanePushConsumptionResult consumption,
            List<RoundResolutionEvent> events)
        {
            if (consumption.IsPaused)
            {
                events.Add(RoundResolutionEvent.SystemTransitionPaused(
                    RoundIndex, TurnIndex, consumption.PendingStep));
                return RoundResolutionResult.Accept(events);
            }

            var finished = consumption.Finished;
            events.AddRange(finished.Outcomes.Select(outcome =>
                RoundResolutionEvent.LanePushStageResolved(RoundIndex, TurnIndex, outcome)));

            var endOfRoundRequest = PendingLanePushRequest;
            PendingLanePushRequest = null;
            cardActions?.ConsumeImmediateLanePush();

            if (finished.EndedTheGame)
            {
                // Milestone 7.7. The stage is reported only when the push was the end-of-round one:
                // p.8's mid-turn push ends the game in the middle of a turn, and naming a stage it
                // never reached was the last thing left in this build that answered "where did the
                // match end" with something other than where it ended.
                //
                // Milestone 7.8. An end-of-round ending now leaves through the same tail every other
                // stage outcome leaves through, rather than past it. It ended the step it was in, and
                // `GameEnded` is the outcome the stage vocabulary has always had for exactly that -
                // declared at 3.8c and, until this milestone, never once produced. Going around the
                // tail meant the ordered stage stream skipped the step that ended the match, which is
                // a second way out of a stage and the one thing this milestone's integration is
                // supposed not to leave behind.
                if (endOfRoundRequest != null &&
                    CurrentEndOfRoundStage == EndOfRoundStage.ResolveRequestedLanePush)
                {
                    return CompleteEndOfRoundStage(
                        EndOfRoundStage.ResolveRequestedLanePush,
                        EndOfRoundStageOutcomeKind.GameEnded,
                        events);
                }

                events.Add(EndGame(null));
                return RoundResolutionResult.Accept(events);
            }

            // An end-of-round push completes the stage it was consumed at; a mid-turn one owes the
            // Initiative advance p.8 withheld while it ran.
            if (endOfRoundRequest != null &&
                CurrentEndOfRoundStage == EndOfRoundStage.ResolveRequestedLanePush)
            {
                return CompleteEndOfRoundStage(
                    EndOfRoundStage.ResolveRequestedLanePush,
                    EndOfRoundStageOutcomeKind.Completed,
                    events);
            }

            if (CurrentTurnAuthority == null)
            {
                return RoundResolutionResult.Accept(events);
            }

            var continued = CurrentTurnAuthority.ContinueAfterImmediateLanePush();
            if (!continued.Accepted)
            {
                throw new InvalidOperationException(
                    "A finished mid-turn Lane Push could not hand the turn back to Initiative order.");
            }

            events.AddRange(continued.Events.Select(value =>
                RoundResolutionEvent.FromTurn(RoundIndex, TurnIndex, value)));
            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        /// <summary>
        /// The one seam every ending crosses, milestone 7.7.
        ///
        /// It exists because the three victory sentences are reached by two unrelated rules - p.8's two
        /// by the Lane-Push transition and p.13's by an Attack that spends the last Life counter - and
        /// before this milestone each of them stopped the match its own way, with its own event shape
        /// and its own idea of what "the game ends" leaves behind. Everything that is true of an ended
        /// match is stated here once: the outcome is read, the phase records that the match stopped, the
        /// turn stops offering the decisions it was in the middle of, and one event says who won and
        /// why.
        ///
        /// The turn authority is stopped rather than discarded, and nothing is cleared. "The game ends
        /// immediately" is about what may still happen, not about forgetting what had; the position the
        /// match ended in is the only honest record of where it ended, it is what the payload keeps, and
        /// a retained-prefix Undo truncated before the ending needs it to still be there.
        /// </summary>
        private RoundResolutionEvent EndGame(EndOfRoundStage? stage)
        {
            var outcome = ReadOutcome(gameIsRecordedAsEnded: true).Outcome;
            Phase = RoundResolutionPhase.GameEnded;
            CurrentTurnAuthority?.StopResolutionForGameEnd();
            return RoundResolutionEvent.GameEnded(RoundIndex, TurnIndex, outcome, stage);
        }

        private static RoundResolutionRejectionReason TranslateTransitionRejection(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason reason)
        {
            switch (reason)
            {
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.UnknownDefinition:
                    return RoundResolutionRejectionReason.UnknownSystemTransition;
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.TransitionAlreadyRunning:
                    return RoundResolutionRejectionReason.SystemTransitionAlreadyRunning;
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.NoPendingStep:
                    return RoundResolutionRejectionReason.NoPendingSystemTransitionStep;
                default:
                    // Wrong instance, stale step, wrong chooser and an illegal option are all one thing
                    // to a caller: this answer is not about the step that is paused, and the
                    // continuation is exactly as it was.
                    return RoundResolutionRejectionReason.IllegalSystemTransitionChoice;
            }
        }

        internal RoundResolutionRestoreState CaptureRestoreState()
        {
            var ledger = CardActions?.CoinLedger == null
                ? HeroCoinLedgerRestoreStateHolder.Absent()
                : HeroCoinLedgerRestoreStateHolder.Present(CardActions.CoinLedger.CaptureRestoreState());
            return new RoundResolutionRestoreState(
                new RoundCursorRestoreState(
                    RoundIndex, TurnIndex, Phase, TieBreakerState, SoloVictoryHeroId),
                new RoundHistoryRestoreState(
                    TurnSlots,
                    HeroDefeatHistory.CaptureRestoreState(),
                    CompletedRounds),
                new EndOfRoundRestoreState(
                    CurrentEndOfRoundStage,
                    EndOfRoundOutcomes,
                    PendingMinionBattleChoice,
                    PendingLanePushRequest,
                    preMinionBattleTriggersVisited),
                new RoundCardActionsRestoreState(CardActions != null, ledger),
                systemTransitions.CaptureRestoreState(),
                HeroProgression.CaptureRestoreState(),
                HeroItems.CaptureRestoreState(),
                TeamLives.CaptureRestoreState(),
                RoundLeveling.CaptureRestoreState(),
                Scoreboard.CaptureRestoreState());
        }

        internal static RoundResolutionAuthority Restore(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RoundResolutionRestoreState state,
            TurnCardCommitmentAuthority commitmentAuthority,
            TurnResolutionAuthority turnAuthority,
            CardActionResolutionAuthority cardActions,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            AdjacentMoveAuthority board,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority systemTransitions,
            GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush,
            GuardsDigital.Core.Rules.Progression.HeroLevelUpStepRules levelUp)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var roundIndex = state.Cursor.RoundIndex;
            var turnIndex = state.Cursor.TurnIndex;
            var phase = state.Cursor.Phase;
            var tieBreakerState = state.Cursor.TieBreakerState;
            var currentEndOfRoundStage = state.EndOfRound.CurrentStage;
            var pendingMinionBattleChoice = state.EndOfRound.PendingMinionBattleChoice;
            var pendingLanePushRequest = state.EndOfRound.PendingLanePushRequest;
            var authority = new RoundResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                roundIndex,
                RoundResolutionComposition.ForMatch(
                    cardActions, board, timing, markers, systemTransitions, lanePush, levelUp));
            if (turnIndex < 1 || turnIndex > TurnsPerRound || !Enum.IsDefined(typeof(RoundResolutionPhase), phase))
            {
                throw new ArgumentException("Restored round cursor is invalid.");
            }

            // Milestone 8.39g. The one victory fact this build records rather than derives, put back
            // before anything asks for the outcome - a restored match that had ended alone must read
            // the same ending it ended with.
            authority.SoloVictoryHeroId = state.Cursor.SoloVictoryHeroId;

            authority.HeroProgression = GuardsDigital.Core.Rules.Progression.HeroProgressionState.Restore(
                participants, state.HeroProgression);
            authority.HeroItems = GuardsDigital.Core.Rules.Progression.HeroItemState.Restore(
                participants, state.HeroItems);

            // Milestone 7.4. The counters the payload recorded, replacing the composed starting ones
            // rather than mutating them, so a restored match never passes through a full pile on its way
            // to the count it saved.
            authority.TeamLives = TeamLifeState.Restore(participants, state.TeamLives);

            // Milestone 7.6. p.8's question is about the round the payload was taken in, so the record
            // is replaced by the one it carried rather than left as the empty round the constructor
            // above created - which would have paid a Pity Coin to a hero that had already levelled.
            authority.RoundLeveling = GuardsDigital.Core.Rules.Progression.RoundLevelingRecord.Restore(
                participants, state.RoundLeveling);
            authority.HeroDefeatHistory = RoundHeroDefeatHistory.Restore(
                participants, state.History.HeroDefeats);

            // Milestone 8.53. The totals the payload recorded, replacing the empty board the
            // constructor above created rather than mutating it, on the terms the three restores
            // above state: a restored match must never pass through a zeroed scoreboard on its way
            // to the numbers it saved. The ledger is re-bound to the replacement below, once the
            // one ledger this authority owns has been asked for.
            authority.Scoreboard = GuardsDigital.Core.Rules.Victory.MatchScoreboard.Restore(
                participants, state.Scoreboard);
            if (authority.HeroDefeatHistory.Entries.Count > 0 &&
                (cardActions?.CoinLedger == null ||
                    authority.HeroDefeatHistory.Entries.Any(entry =>
                        !cardActions.CoinLedger.AwardedOutcomeIds.Contains(entry.OutcomeId))))
            {
                throw new ArgumentException(
                    "Every current-round hero-Defeat outcome must exist in the authoritative coin " +
                    "ledger that p.13 awarded for it.");
            }
            cardActions?.BindHeroItems(authority.HeroItems);
            cardActions?.BindActiveEffects(authority.Timing);
            cardActions?.BindDefeatConsequenceState(authority.HeroProgression, authority.TeamLives);
            cardActions?.BindHeroDefeatHistory(authority.HeroDefeatHistory);

            // Milestone 7.5. The constructor above bound the leveling rules the states it created; these
            // are the ones the payload recorded, so they are bound again over the top rather than
            // mutated into place - the same replacement the three restores above make, and for the same
            // reason: a restored match must never pass through a starting value on its way to the one it
            // saved. The coin ledger is not re-bound because it is the same instance either way: this
            // authority asked the dispatcher for it, and the dispatcher was restored before it.
            levelUp?.Bind(
                authority.HeroProgression,
                authority.HeroItems,
                cardActions?.EnsureCoinLedger(participants) ??
                    new GuardsDigital.Core.Rules.Consequences.HeroCoinLedger(participants));

            // Milestone 8.53. The same single ledger the level purchase above spends from is what
            // counts the "Coins earned" column, so it is handed the restored board here - after the
            // ensure above, which is the one creation point a restore can reach.
            cardActions?.EnsureCoinLedger(participants).BindScoreboard(authority.Scoreboard);
            authority.RoundIndex = roundIndex;
            authority.TurnIndex = turnIndex;
            authority.Phase = phase;
            authority.tieBreakerState = tieBreakerState;
            authority.turnSlots.Clear();
            authority.turnSlots.AddRange(state.History.TurnSlots);
            cardActions?.BindRosterState(EffectRosterState.Of(
                authority.turnSlots.AsReadOnly(),
                authority.HeroDefeatHistory,
                cardActions.EnsureCoinLedger(participants),
                // Milestone 8.39g1. A restored round is as claimable as a composed one - the claim is
                // refused on its own terms when the payload already carries a winner.
                authority, authority, authority));

            // Milestone 8.75a. A restored dispatcher is re-bound to the restored round's coin, the
            // way it is re-bound to the restored roster state one line above and for the same reason:
            // the binding is not part of the payload, so a restore that skipped it would walk every
            // one of Ignatia the Mad's cards with no face showing.
            cardActions?.BindTieBreakerCoin(authority);
            authority.completedRounds.Clear();
            authority.completedRounds.AddRange(state.History.CompletedRounds);
            authority.endOfRoundOutcomes.Clear();
            authority.endOfRoundOutcomes.AddRange(state.EndOfRound.Outcomes);
            authority.CurrentEndOfRoundStage = currentEndOfRoundStage;
            authority.preMinionBattleTriggersVisited = state.EndOfRound.PreMinionBattleTriggersVisited;
            authority.CurrentCommitmentAuthority = commitmentAuthority;
            authority.CurrentTurnAuthority = turnAuthority;

            // Milestone 7.2a. The restored turn reads Initiative through the same items the live one
            // did. It is bound after the items are restored rather than passed into the turn's own
            // restore, because the turn is rebuilt by the reader before the round that owns them.
            turnAuthority?.BindHeroItems(authority.HeroItems);
            authority.PendingMinionBattleChoice = pendingMinionBattleChoice;
            authority.PendingLanePushRequest = pendingLanePushRequest;

            // Milestone 7.7. A restored ended match stops offering the decisions it stopped offering
            // when it ended. The flag is re-applied from the phase rather than written to the payload,
            // because the phase already says the match is over and a second record of that could only
            // agree with it - the same argument the outcome itself is derived by.
            if (phase == RoundResolutionPhase.GameEnded)
            {
                turnAuthority?.StopResolutionForGameEnd();
            }

            var awaitingShape = phase == RoundResolutionPhase.AwaitingCommitments &&
                commitmentAuthority != null && turnAuthority == null && !currentEndOfRoundStage.HasValue;
            var resolvingShape = phase == RoundResolutionPhase.ResolvingTurn &&
                commitmentAuthority != null && turnAuthority != null && !turnAuthority.IsEnded &&
                !currentEndOfRoundStage.HasValue;
            var endShape = phase == RoundResolutionPhase.EndOfRound &&
                commitmentAuthority != null && turnAuthority != null && turnAuthority.IsEnded &&
                currentEndOfRoundStage.HasValue;

            // Milestone 7.7. A match that ended keeps whichever shape it ended in, and there are two of
            // them: p.8's two endings stop an end-of-round stage, and p.13's stops inside a turn that
            // never completed. Until this milestone only the first was admitted, so a payload taken
            // after the last Life counter was spent - the one state milestone 7.4's own victory path can
            // leave a match in - was refused as malformed by the reader that had just written it.
            //
            // The turn's own ended flag is deliberately not constrained on the second shape. p.13's
            // ending happens inside an Attack, and whether the turn had any card left to resolve
            // afterwards is a fact about that turn rather than about the ending: a defeated hero's card
            // is auto-resolved, so an ending that defeats the last unresolved hero leaves a turn that
            // is finished and was never completed. What the ending does constrain is the other
            // direction, which the stage cursor states: a round standing at an end-of-round stage is a
            // round whose turns are all over.
            var gameEndedShape = phase == RoundResolutionPhase.GameEnded &&
                commitmentAuthority != null && turnAuthority != null &&
                (!currentEndOfRoundStage.HasValue || turnAuthority.IsEnded);
            if (!awaitingShape && !resolvingShape && !endShape && !gameEndedShape)
            {
                throw new ArgumentException("Restored round phase does not match its active authority or cursor.");
            }

            var pendingMinionBattleShape = pendingMinionBattleChoice == null ||
                (phase == RoundResolutionPhase.EndOfRound &&
                    currentEndOfRoundStage == EndOfRoundStage.MinionBattle &&
                    board != null);
            if (!pendingMinionBattleShape)
            {
                throw new ArgumentException("Restored Minion Battle choice does not match the round cursor.");
            }

            if (pendingMinionBattleChoice != null)
            {
                var expectedPlan = MinionBattleRules.Plan(
                    roundIndex,
                    board,
                    participants,
                    cardActions?.CurrentModifications(
                        timing, participants, tieBreakerState, authority.CurrentUltimates(),
                        GuardsDigital.Core.Rules.Modifications.RuleObject.None) ??
                        GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                    authority.CurrentMinionBattleRules(),
                    GuardsDigital.Core.Rules.Consequences.MinionBattleRuleCatalog.Rulebook);
                if (expectedPlan.PendingChoice == null)
                {
                    throw new ArgumentException("Restored Minion Battle choice is no longer required by authoritative state.");
                }

                PendingMinionBattleChoice expected;
                try
                {
                    expected = expectedPlan.PendingChoice.WithSelected(
                        pendingMinionBattleChoice.SelectedUnits);
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(
                        "Restored Minion Battle selection violates authoritative Heavy-last ordering.",
                        exception);
                }

                if (!MinionBattleChoicesMatch(expected, pendingMinionBattleChoice))
                {
                    throw new ArgumentException("Restored Minion Battle choice does not match authoritative candidates.");
                }
            }

            // Milestone 8.35z23. What a restored end-of-round push request can honestly be checked
            // against is its own identity - the round it belongs to, the two teams and their sides,
            // and the Minion Battle that caused it. The board question below is asked only while the
            // push has not begun, and the board question that used to stand beside it is gone.
            //
            // THE ZONE. p.8's push is a transition with numbered steps and it can stop on the
            // sidebar's Place. By then step 2 has moved the Battle Zone and step 3 has been
            // respawning into the new one, while the request is only spent when the whole transition
            // finishes - so "the current Zone is still the one the request names" is false on a
            // position this engine itself produced, and a snapshot taken there was written by this
            // serializer and then refused by it.
            //
            // THE LOSING SIDE'S MINIONS. That clause asked whether the battle had emptied the Zone
            // out. Milestone 8.19c made the answer irrelevant: a substituted removal "loses the push
            // instead", and that request stands whether or not the side emptied out. Which of the two
            // causes made a given request is not recorded anywhere in the payload, so no reader can
            // ask the question the clause was asking - and the clause read raw minions where the
            // creating rule reads EFFECTIVE units and heroes as well, so it did not even ask the
            // creating rule's question. It is removed rather than narrowed.
            var lanePushHasBegun = lanePush != null && lanePush.IsRunning;
            if (pendingLanePushRequest != null &&
                (phase != RoundResolutionPhase.EndOfRound ||
                    !currentEndOfRoundStage.HasValue ||
                    (int)currentEndOfRoundStage.Value < (int)EndOfRoundStage.RemoveTokensAndReturnMarkers ||
                    pendingLanePushRequest.RoundIndex != roundIndex ||
                    board == null ||
                    participants.GetTeam(pendingLanePushRequest.WinningTeamId).Side !=
                        pendingLanePushRequest.WinningSide ||
                    participants.GetTeam(pendingLanePushRequest.LosingTeamId).Side !=
                        pendingLanePushRequest.LosingSide ||
                    !pendingLanePushRequest.CauseResolutionId.Equals(
                        new EndOfRoundResolutionId($"round-{roundIndex}-minion-battle")) ||
                    (!lanePushHasBegun &&
                        (!GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.TryGetCurrentBattleZone(
                                board, out var currentZone) ||
                            !currentZone.Id.Equals(pendingLanePushRequest.BattleZoneId)))))
            {
                throw new ArgumentException("Restored Lane-Push request does not match authoritative end-of-round state.");
            }

            return authority;
        }

        private static bool MinionBattleChoicesMatch(
            PendingMinionBattleChoice expected,
            PendingMinionBattleChoice actual)
        {
            return expected.ResolutionId.Equals(actual.ResolutionId) &&
                expected.ChoiceId.Equals(actual.ChoiceId) &&
                expected.LosingTeamId.Equals(actual.LosingTeamId) &&
                expected.ChoosingSide == actual.ChoosingSide &&
                expected.BattleZoneId.Equals(actual.BattleZoneId) &&
                expected.RequiredRemovalCount == actual.RequiredRemovalCount &&
                expected.Candidates.SequenceEqual(actual.Candidates) &&
                expected.SelectedUnits.SequenceEqual(actual.SelectedUnits) &&
                expected.LegalSelectUnits.SequenceEqual(actual.LegalSelectUnits) &&
                expected.LegalDeselectUnits.SequenceEqual(actual.LegalDeselectUnits);
        }

        public RoundResolutionResult TryCommitCard(HeroId heroId, CardInstanceId cardInstanceId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryCommitCard(
                heroId,
                cardInstanceId));
        }

        public RoundResolutionResult TryPass(HeroId heroId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryPass(heroId));
        }

        public RoundResolutionResult TryChooseTiedActor(
            TeamSide choosingSide,
            CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryChooseTiedActor(
                choosingSide,
                cardInstanceId));
        }

        public RoundResolutionResult TryPerformHold(CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformHold(cardInstanceId));
        }

        /// <summary>
        /// Milestone 8.35g. Hands a finished turn on at its own player's word. It runs through the
        /// ordinary turn-command path, so a released turn that has no unresolved cards left ends
        /// the round exactly as a completed resolution used to.
        /// </summary>
        public RoundResolutionResult TryReleaseTurn()
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryReleaseTurn());
        }

        public RoundResolutionResult TryStartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartMovement(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerMovementChoice(MovementChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerMovementChoice(command));
        }

        public RoundResolutionResult TryPerformFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            GuardsDigital.Core.Board.HexCoordinate destination,
            IEnumerable<MinionOccupancy> minions)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                minions));
        }

        public RoundResolutionResult TryPerformClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds));
        }

        public RoundResolutionResult TryStartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartAttack(resolutionId, offer));
        }

        public RoundResolutionResult TryStartSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartSkill(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerEffectChoice(EffectChoiceCommand command)
        {
            // Milestone 8.19c1. A question asked at rulebook p.8's moment before the Minion Battle is
            // answered with the same command a Skill's is, because it is the same frame paused in the
            // same way. What differs is who owns it: the round is between turns, so the turn authority
            // that answers every other effect choice has nothing to resume.
            if (Phase == RoundResolutionPhase.EndOfRound &&
                cardActions != null && cardActions.IsPendingTriggeredEffect)
            {
                return TryAnswerEndOfRoundTriggerChoice(command);
            }

            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerEffectChoice(command));
        }

        /// <summary>
        /// Milestone 8.19c1. Closes the standing removal-prevention decision. It goes through the turn
        /// like every other card-action answer, because the Defeat it is standing inside belongs to an
        /// action the turn is resolving.
        /// </summary>
        public RoundResolutionResult TryAnswerRemovalPrevention(
            GuardsDigital.Core.Rules.CardResolution.RemovalPreventionCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerRemovalPrevention(command));
        }

        public RoundResolutionResult TryAnswerDisplacedMinionCorrection(
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand command)
        {
            return ExecuteTurnCommand(() =>
                CurrentTurnAuthority.TryAnswerDisplacedMinionCorrection(command));
        }

        public RoundResolutionResult TryAnswerAttackTarget(AttackTargetChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerAttackTarget(command));
        }

        public RoundResolutionResult TryAnswerHeroRespawn(HeroRespawnCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerHeroRespawn(command));
        }

        public RoundResolutionResult TryAnswerDefenseResponse(DefenseResponseChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerDefenseResponse(command));
        }

        public RoundResolutionResult TryAnswerMinionBattleChoice(MinionBattleChoiceCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (Phase != RoundResolutionPhase.EndOfRound ||
                CurrentEndOfRoundStage != EndOfRoundStage.MinionBattle ||
                PendingMinionBattleChoice == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NoPendingMinionBattleChoice);
            }

            var choice = PendingMinionBattleChoice;
            if (!command.ResolutionId.Equals(choice.ResolutionId) ||
                !command.ChoiceId.Equals(choice.ChoiceId))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.StaleMinionBattleChoice);
            }

            if (command.ChoosingSide != choice.ChoosingSide)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.WrongMinionBattleChooser);
            }

            if (command.Kind == MinionBattleChoiceCommandKind.Select)
            {
                if (!choice.LegalSelectUnits.Contains(command.Unit))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedUnits.Concat(new[] { command.Unit }));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.Unit,
                        command.Kind)
                });
            }

            if (command.Kind == MinionBattleChoiceCommandKind.Deselect)
            {
                if (!choice.LegalDeselectUnits.Contains(command.Unit))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedUnits.Where(value => !value.Equals(command.Unit)));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.Unit,
                        command.Kind)
                });
            }

            if (!choice.CanConfirm)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteMinionBattleSelection);
            }

            return CompleteMinionBattle(
                choice.ResolutionId,
                choice.BattleZoneId,
                participants.Teams.Single(value => value.Side != choice.ChoosingSide),
                participants.GetTeam(choice.LosingTeamId),
                choice.SelectedUnits);
        }

        /// <summary>
        /// Milestone 8.19c1. Runs whatever declared rulebook p.8's pre-Minion-Battle moment, and
        /// returns everything it produced.
        ///
        /// <para>It is the ordinary triggered-effect visit, at a checkpoint the lifecycle already had
        /// and nothing had ever raised. The card-action dispatcher runs it because it holds the
        /// triggered-effect authority, the capability catalog and the components an effect executes
        /// against - the same collaborators a triggered `Action Ended` needs, at a different moment. A
        /// composition with no card actions has nothing that could declare the window, so the moment
        /// passes silently and the battle is planned exactly as it was before this milestone.</para>
        /// </summary>
        private List<RoundResolutionEvent> VisitPreMinionBattleTriggers()
        {
            preMinionBattleTriggersVisited = true;
            var events = new List<RoundResolutionEvent>();
            if (cardActions == null)
            {
                return events;
            }

            var modifications = cardActions.CurrentModifications(
                timing, participants, TieBreakerState, CurrentUltimates(),
                GuardsDigital.Core.Rules.Modifications.RuleObject.None);
            foreach (var timingEvent in cardActions.ApplyTriggeredActionEnded(
                timing,
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.EndOfRoundBeforeMinionBattle, RoundIndex, TurnsPerRound),
                TieBreakerState,
                definitions,
                instances,
                participants,
                modifications))
            {
                events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnsPerRound, timingEvent));
            }

            AppendTriggeredEffectEvents(events);
            return events;
        }

        /// <summary>
        /// Everything a pre-Minion-Battle trigger's own frame produced, in the round's vocabulary. A
        /// fact becomes the nested turn event every other triggered effect publishes; a checkpoint the
        /// effect raised is applied here, in stream order, exactly as the turn applies one.
        /// </summary>
        private void AppendTriggeredEffectEvents(ICollection<RoundResolutionEvent> events)
        {
            foreach (var step in cardActions.ConsumeTriggeredResolutionSteps())
            {
                if (step.Kind == GuardsDigital.Core.Rules.CardResolution.ResolutionStepKind.Checkpoint)
                {
                    foreach (var timingEvent in timing.ApplyCheckpoint(
                        new ActiveEffectCheckpointContext(
                            step.Checkpoint,
                            RoundIndex,
                            TurnsPerRound,
                            step.CheckpointAffectedHeroId,
                            step.CheckpointSourceCardInstanceId),
                        TieBreakerState))
                    {
                        events.Add(RoundResolutionEvent.FromTiming(
                            RoundIndex, TurnsPerRound, timingEvent));
                    }

                    continue;
                }

                if (step.Kind == GuardsDigital.Core.Rules.CardResolution.ResolutionStepKind.Fact)
                {
                    events.Add(RoundResolutionEvent.FromTurn(
                        RoundIndex, TurnsPerRound, TurnResolutionEvent.FromTriggeredEffect(step.Fact)));
                }
            }
        }

        /// <summary>
        /// Milestone 8.19c1. Answers the question a pre-Minion-Battle trigger stopped on. When nothing
        /// is left standing the round is exactly where it was - at its Minion Battle stage, with the
        /// battle still to plan - so the next advance resumes into the same battle.
        /// </summary>
        public RoundResolutionResult TryAnswerEndOfRoundTriggerChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.EndOfRound ||
                CurrentEndOfRoundStage != EndOfRoundStage.MinionBattle ||
                cardActions == null || !cardActions.IsPendingTriggeredEffect)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NotResolvingEndOfRound);
            }

            var answered = cardActions.AnswerTriggeredEffectChoice(
                command,
                definitions,
                instances,
                participants,
                timing,
                cardActions.CurrentModifications(
                    timing, participants, TieBreakerState, CurrentUltimates(),
                    GuardsDigital.Core.Rules.Modifications.RuleObject.None),
                RoundIndex,
                TurnsPerRound,
                TieBreakerState);
            if (!answered.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NotResolvingEndOfRound);
            }

            var events = new List<RoundResolutionEvent>();
            AppendTriggeredEffectEvents(events);
            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ResolveMinionBattle(List<RoundResolutionEvent> precedingEvents = null)
        {
            if (board == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
            }

            var plan = MinionBattleRules.Plan(
                RoundIndex,
                board,
                participants,
                cardActions?.CurrentModifications(
                        timing, participants, TieBreakerState, CurrentUltimates(),
                        GuardsDigital.Core.Rules.Modifications.RuleObject.None) ??
                    GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                CurrentMinionBattleRules(),
                GuardsDigital.Core.Rules.Consequences.MinionBattleRuleCatalog.Rulebook);
            var preceding = precedingEvents ?? new List<RoundResolutionEvent>();
            if (plan.PendingChoice != null)
            {
                PendingMinionBattleChoice = plan.PendingChoice;
                preceding.Add(
                    RoundResolutionEvent.MinionBattleChoiceRequested(RoundIndex, plan.PendingChoice));
                preceding.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                    RoundIndex,
                    EndOfRoundStage.MinionBattle,
                    EndOfRoundStageOutcomeKind.Paused));
                return RoundResolutionResult.Accept(preceding);
            }

            if (!plan.LosingTeamId.IsDefined)
            {
                return CompleteMinionBattle(
                    plan.ResolutionId,
                    plan.BattleZoneId,
                    null,
                    null,
                    Array.Empty<GuardsDigital.Core.Rules.Modifications.RuleObject>(),
                    preceding);
            }

            return CompleteMinionBattle(
                plan.ResolutionId,
                plan.BattleZoneId,
                participants.GetTeam(plan.WinningTeamId),
                participants.GetTeam(plan.LosingTeamId),
                plan.AutomaticRemovalUnits,
                preceding);
        }

        /// <summary>
        /// Removes the effective minion units selected by the battle. A hero removal is not Defeat:
        /// it awards nothing and changes no Life; it only takes that hero off board for respawn.
        /// </summary>
        private RoundResolutionResult CompleteMinionBattle(
            EndOfRoundResolutionId resolutionId,
            GuardsDigital.Core.Board.ZoneId battleZoneId,
            Team winningTeam,
            Team losingTeam,
            IEnumerable<GuardsDigital.Core.Rules.Modifications.RuleObject> removalUnits,
            List<RoundResolutionEvent> precedingEvents = null)
        {
            var units = (removalUnits ?? throw new ArgumentNullException(nameof(removalUnits))).ToArray();
            if (units.Distinct().Count() != units.Length)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var modifications = cardActions?.CurrentModifications(
                timing, participants, TieBreakerState, CurrentUltimates(),
                GuardsDigital.Core.Rules.Modifications.RuleObject.None) ??
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty;
            var ruleParticipants = CurrentMinionBattleRules();
            var candidates = units
                .Select(unit => ResolveBattleCandidate(unit, modifications, ruleParticipants))
                .ToArray();
            if (candidates.Any(value => !value.HasValue) || candidates.Any(value =>
                    losingTeam == null || value.Value.TeamSide != losingTeam.Side))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var ordered = candidates.Select(value => value.Value)
                .OrderBy(value => value.IsHeavy ? 1 : 0)
                .ThenBy(value => GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(value.Unit))
                .ThenBy(value => GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(value.Unit) ==
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero
                    ? value.Unit.HeroId.Value
                    : value.Unit.MinionId.Value, StringComparer.Ordinal)
                .ToArray();

            // Milestone 8.19c1. Whatever the pre-battle moment produced stands ahead of the battle in
            // the stream, because that is where it happened: the effect ran and then the battle counted
            // what it left standing.
            var events = precedingEvents ?? new List<RoundResolutionEvent>();
            var pushLostBySubstitution = false;
            foreach (var candidate in ordered)
            {
                // Milestone 8.19c. p.8's removal is what a declared Minion-Battle rule replaces, so the
                // substitution is decided here, immediately before the miniature would come off the
                // board, and by the registered handler rather than by this authority. A substituted
                // removal is not a Defeat and never was one: no coins are awarded, no reward
                // eligibility opens and no Team Life moves, exactly as the removal it replaces awards
                // none of those.
                if (SubstitutesRemoval(candidate.Unit, ruleParticipants))
                {
                    pushLostBySubstitution = true;
                    events.Add(RoundResolutionEvent.MinionBattlePushLost(
                        RoundIndex,
                        resolutionId,
                        losingTeam.Id,
                        losingTeam.Side,
                        candidate.Unit));
                    continue;
                }

                if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(candidate.Unit) ==
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero)
                {
                    board.CommitValidatedHeroRemoval(candidate.Unit.HeroId);
                }
                else
                {
                    board.CommitValidatedMinionRemoval(candidate.Unit.MinionId);
                }

                events.Add(RoundResolutionEvent.MinionBattleUnitRemoved(
                    RoundIndex,
                    resolutionId,
                    losingTeam.Id,
                    losingTeam.Side,
                    candidate.Unit));
            }

            // `This Round` remains live through the Minion Battle it can modify. The round-ending
            // checkpoint occurs only after that battle has counted and removed effective minions.
            foreach (var timingEvent in timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.RoundEnd,
                RoundIndex,
                TurnsPerRound),
                TieBreakerState))
            {
                events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnsPerRound, timingEvent));
            }

            PendingMinionBattleChoice = null;
            var postRoundModifications = cardActions?.CurrentModifications(
                timing, participants, TieBreakerState, CurrentUltimates(),
                GuardsDigital.Core.Rules.Modifications.RuleObject.None) ??
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty;
            // Milestone 8.19c. A substituted removal loses p.8's push by itself - "lose the push
            // instead" is the whole of the sentence - so the request stands whether or not the losing
            // side has emptied out. The two causes reach one request, with one identity and one pair of
            // teams, because p.8 pushes the Battle Zone once per battle however many reasons it had.
            if (losingTeam != null && ordered.Length > 0 &&
                (pushLostBySubstitution ||
                    !HasEffectiveMinionInBattleZone(losingTeam.Side, postRoundModifications)))
            {
                PendingLanePushRequest = new PendingLanePushRequest(
                    new LanePushRequestId($"round-{RoundIndex}-minion-battle-lane-push"),
                    RoundIndex,
                    battleZoneId,
                    winningTeam.Id,
                    winningTeam.Side,
                    losingTeam.Id,
                    losingTeam.Side,
                    resolutionId);
                events.Add(RoundResolutionEvent.LanePushRequested(RoundIndex, PendingLanePushRequest));
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(
                EndOfRoundStage.MinionBattle,
                EndOfRoundStageOutcomeKind.Completed));
            CurrentEndOfRoundStage = EndOfRoundStage.RemoveTokensAndReturnMarkers;
            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                EndOfRoundStage.MinionBattle,
                EndOfRoundStageOutcomeKind.Completed));
            return RoundResolutionResult.Accept(events);
        }

        /// <summary>
        /// Milestone 9.21a (RZ-Q2). Whether a hero that counts as a minion stands in the Battle Zone:
        /// presence is read per body, so a hero any of whose bodies stands there is in it, and it is
        /// one unit of the battle however many of its bodies do.
        /// </summary>
        private bool HeroStandsInCurrentBattleZone(HeroId heroId) =>
            board.GetHeroBodies(heroId).Any(body =>
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                    board,
                    board.MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(body.Coordinate)).Id));

        private BattleRemovalCandidate? ResolveBattleCandidate(
            GuardsDigital.Core.Rules.Modifications.RuleObject unit,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications,
            IReadOnlyList<GuardsDigital.Core.Rules.Consequences.MinionBattleParticipant> ruleParticipants)
        {
            // Milestone 8.19c. Two ways to be one of the minions this battle can remove: p.17's Counts
            // As, which the modification set answers, and a declared Minion-Battle rule supplying
            // strength, which only the rule answers. The rule's arm is asked second and reports the
            // Heavy the card names, so a hero counting as a Heavy minion is ordered last exactly as a
            // Heavy miniature is.
            var suppliedStrength = RuleSuppliedStrengthFor(unit, ruleParticipants);
            if (suppliedStrength == GuardsDigital.Core.Rules.Consequences.MinionBattleStrength.HeavyMinion &&
                GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(unit) ==
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero &&
                board.IsHeroOnBoard(unit.HeroId))
            {
                return HeroStandsInCurrentBattleZone(unit.HeroId)
                    ? new BattleRemovalCandidate(
                        unit, participants.GetTeamForHero(unit.HeroId).Side, true)
                    : (BattleRemovalCandidate?)null;
            }

            if (!GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                    unit,
                    GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                    modifications))
            {
                return null;
            }

            if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.IsMinionMiniature(unit))
            {
                var minion = board.Minions.SingleOrDefault(value => value.MinionId.Equals(unit.MinionId));
                return minion != null && GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId)
                    ? new BattleRemovalCandidate(
                        unit, minion.TeamSide, minion.MinionType == StandardMinionType.Heavy)
                    : (BattleRemovalCandidate?)null;
            }

            if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(unit) !=
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero ||
                !board.IsHeroOnBoard(unit.HeroId))
            {
                return null;
            }

            return HeroStandsInCurrentBattleZone(unit.HeroId)
                ? new BattleRemovalCandidate(
                    unit, participants.GetTeamForHero(unit.HeroId).Side, false)
                : (BattleRemovalCandidate?)null;
        }

        /// <summary>
        /// Milestone 8.19c. What a declared Minion-Battle rule adds for this unit right now, asked of
        /// the registered handler. <see cref="MinionBattleStrength.None"/> for a unit no rule names,
        /// which is every unit in a match whose roster declares none.
        /// </summary>
        private static GuardsDigital.Core.Rules.Consequences.MinionBattleStrength RuleSuppliedStrengthFor(
            GuardsDigital.Core.Rules.Modifications.RuleObject unit,
            IReadOnlyList<GuardsDigital.Core.Rules.Consequences.MinionBattleParticipant> ruleParticipants)
        {
            for (var index = 0; index < ruleParticipants.Count; index++)
            {
                if (!ruleParticipants[index].Unit.Equals(unit)) continue;
                return GuardsDigital.Core.Rules.Consequences.MinionBattleRuleCatalog.Rulebook
                    .Get(ruleParticipants[index].Declaration.Reference)
                    .Contribute(ruleParticipants[index]);
            }

            return GuardsDigital.Core.Rules.Consequences.MinionBattleStrength.None;
        }

        /// <summary>
        /// Milestone 8.19c. Whether a declared Minion-Battle rule replaces this unit's own removal, and
        /// with what. Asked immediately before the miniature would leave the board, which is the moment
        /// the card's "if you would be removed" names.
        /// </summary>
        private static bool SubstitutesRemoval(
            GuardsDigital.Core.Rules.Modifications.RuleObject unit,
            IReadOnlyList<GuardsDigital.Core.Rules.Consequences.MinionBattleParticipant> ruleParticipants)
        {
            for (var index = 0; index < ruleParticipants.Count; index++)
            {
                if (!ruleParticipants[index].Unit.Equals(unit)) continue;
                return GuardsDigital.Core.Rules.Consequences.MinionBattleRuleCatalog.Rulebook
                    .Get(ruleParticipants[index].Declaration.Reference)
                    .SubstituteRemoval(ruleParticipants[index]) ==
                    GuardsDigital.Core.Rules.Consequences.MinionBattleRemovalSubstitution.PushLost;
            }

            return false;
        }

        private bool HasEffectiveMinionInBattleZone(
            TeamSide side,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications)
        {
            return board.Minions.Any(minion =>
                    minion.TeamSide == side &&
                    GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(minion.MinionId),
                        GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                        modifications) &&
                    GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                        board, minion.OccupiedSpace.SpaceId)) ||
                board.Occupancies.Any(hero =>
                {
                    var unit = GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(hero.HeroId);
                    if (participants.GetTeamForHero(hero.HeroId).Side != side ||
                        !GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                            unit,
                            GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                            modifications))
                    {
                        return false;
                    }

                    var space = board.MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(hero.Coordinate));
                    return GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .IsInCurrentBattleZone(board, space.Id);
                });
        }

        private readonly struct BattleRemovalCandidate
        {
            public BattleRemovalCandidate(
                GuardsDigital.Core.Rules.Modifications.RuleObject unit,
                TeamSide teamSide,
                bool isHeavy)
            {
                Unit = unit;
                TeamSide = teamSide;
                IsHeavy = isHeavy;
            }

            public GuardsDigital.Core.Rules.Modifications.RuleObject Unit { get; }
            public TeamSide TeamSide { get; }
            public bool IsHeavy { get; }
        }

        public RoundResolutionResult TryAdvanceEndOfRound()
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.EndOfRound || !CurrentEndOfRoundStage.HasValue)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NotResolvingEndOfRound);
            }

            var stage = CurrentEndOfRoundStage.Value;
            if (PendingMinionBattleChoice != null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteMinionBattleSelection);
            }

            // Milestone 6.11. p.8's push now runs here rather than stopping the round. A push that
            // stops on the sidebar's Place is answered by its own command, so a second advance while
            // one is stopped is refused rather than beginning a second push.
            if (stage == EndOfRoundStage.ResolveRequestedLanePush && PendingLanePushRequest != null)
            {
                return ResolveRequestedLanePush(stage);
            }

            // Milestone 7.8. The other half of that step, and the reason the availability list did not
            // name it until now: a round whose Minion Battle awarded no push still runs p.8's push
            // step, and the step's honest answer is that there was nothing to push. Before this it
            // fell through to the generic arm, was found undeclared, and reported the step
            // *unavailable* - a claim about this match rather than about this round.
            //
            // A composed handler for this stage still wins, and the check is here rather than implied:
            // a configured match composes none, while the milestone 6.13 cursor fixtures deliberately
            // stage a pause and a game-ending at this stage through one, because what they are about
            // is the cursor's promise rather than p.8's transition.
            if (stage == EndOfRoundStage.ResolveRequestedLanePush &&
                endOfRoundStageAvailability.IsAvailable(EndOfRoundStage.ResolveRequestedLanePush) &&
                !endOfRoundHandlers.ContainsKey(EndOfRoundStage.ResolveRequestedLanePush))
            {
                return CompleteEndOfRoundStage(
                    stage,
                    EndOfRoundStageOutcomeKind.Completed,
                    new List<RoundResolutionEvent>());
            }

            if (stage == EndOfRoundStage.MinionBattle &&
                endOfRoundStageAvailability.IsAvailable(EndOfRoundStage.MinionBattle) &&
                board != null)
            {
                // Milestone 8.19c1. Rulebook p.8's end of round, at the moment before the battle is
                // planned - the position Xargatha's Constrict prints in its own reminder text. It runs
                // before anything about the battle is derived, so an effect that defeats a minion here
                // changes what the battle then counts, which is the whole point of the card.
                if (!preMinionBattleTriggersVisited)
                {
                    var preBattle = VisitPreMinionBattleTriggers();
                    if (cardActions != null && cardActions.IsPendingTriggeredEffect)
                    {
                        return RoundResolutionResult.Accept(preBattle);
                    }

                    return ResolveMinionBattle(preBattle);
                }

                // A question the moment asked is still standing. The round waits for it rather than
                // planning a battle around half-run card text.
                if (cardActions != null && cardActions.IsPendingTriggeredEffect)
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.NotResolvingEndOfRound);
                }

                return ResolveMinionBattle();
            }

            // Milestone 6.12. Handled here rather than through an IEndOfRoundStageHandler for the reason
            // Minion Battle is: the stage's collaborators are the board and the marker register, which
            // the authority already holds and the handler context deliberately does not carry.
            if (stage == EndOfRoundStage.RemoveTokensAndReturnMarkers &&
                endOfRoundStageAvailability.IsAvailable(EndOfRoundStage.RemoveTokensAndReturnMarkers))
            {
                return ResolveComponentCleanup(stage);
            }

            // Milestone 7.5. Handled here rather than through an IEndOfRoundStageHandler for the reason
            // the two stages above are: its collaborators are the transition authority and the states a
            // purchase moves, which this authority holds and the handler context deliberately does not
            // carry. A leveling that stops on p.15's Upgrade a Card leaves the stage where it is, so the
            // round waits for the answer rather than advancing past a transition that has not finished.
            if (stage == EndOfRoundStage.LevelUpAndUpgrade &&
                endOfRoundStageAvailability.IsAvailable(EndOfRoundStage.LevelUpAndUpgrade))
            {
                return ResolveLevelUp(stage);
            }

            // Milestone 7.6. Handled here for the reason the three stages above are: its collaborators
            // are the coin ledger and the round's own leveling record, which this authority holds.
            // Unlike the stage before it this one asks nobody anything, so it can never pause.
            if (stage == EndOfRoundStage.CollectPityCoins &&
                endOfRoundStageAvailability.IsAvailable(EndOfRoundStage.CollectPityCoins))
            {
                return ResolvePityCoins(stage);
            }

            EndOfRoundStageResolution resolution;
            if (!endOfRoundStageAvailability.IsAvailable(stage))
            {
                resolution = EndOfRoundStageResolution.NotApplicable();
            }
            else
            {
                if (!endOfRoundHandlers.TryGetValue(stage, out var handler))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
                }

                resolution = handler.TryResolve(new EndOfRoundStageContext(
                    RoundIndex,
                    instances,
                    participants));
            }

            var events = new List<RoundResolutionEvent>();
            foreach (var retrieval in resolution.RetrievedCards ?? Array.Empty<CardRetrievalFact>())
            {
                events.Add(RoundResolutionEvent.RetrievedCard(RoundIndex, stage, retrieval));
            }

            if (resolution.ActiveEffectCancellationBoundaryReached)
            {
                events.AddRange(timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.CardsRetrieved,
                    RoundIndex,
                    TurnIndex),
                    TieBreakerState).Select(value => RoundResolutionEvent.FromTiming(
                        RoundIndex, TurnIndex, value)));
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.ActiveEffectCancellationCheckpoint,
                    RoundIndex,
                    stage));
            }

            return CompleteEndOfRoundStage(stage, resolution.OutcomeKind, events);
        }

        /// <summary>
        /// Rulebook p.8's "Remove Tokens, Return Markers": "Remove all tokens from the board. Return all
        /// markers back into the supply."
        ///
        /// Deterministic and unconditional. It asks no player anything, so unlike Minion Battle and the
        /// Lane Push it can never pause, which is why it carries no continuation and adds no persisted
        /// state - the stage cursor that already crosses the snapshot is the whole of where it is.
        ///
        /// Order inside the stage is tokens then markers, matching the page's two sentences. Nothing
        /// depends on it: a token is board state and a marker is not, so neither can change what the
        /// other's rule sees.
        /// </summary>
        private RoundResolutionResult ResolveComponentCleanup(EndOfRoundStage stage)
        {
            var events = new List<RoundResolutionEvent>();

            if (board != null)
            {
                // Milestone 8.114b1. The Minion Battle is over, so what Dominance excluded from its
                // count is excluded no longer.
                board.ClearMinionBattleExclusions();
                foreach (var token in GuardsDigital.Core.Components.ComponentCleanupRules
                             .TokensRemovedAtEndOfRound(board.PlacedTokens))
                {
                    events.Add(RoundResolutionEvent.TokenRemovedAtEndOfRound(
                        RoundIndex,
                        board.CommitValidatedTokenReclamation(token.Id)));
                }
            }

            foreach (var attachment in GuardsDigital.Core.Components.ComponentCleanupRules
                         .MarkersReturnedAtEndOfRound(markers.Attachments))
            {
                events.Add(RoundResolutionEvent.MarkerReturnedAtEndOfRound(
                    RoundIndex,
                    markers.Return(attachment.Id)));
            }

            return CompleteEndOfRoundStage(stage, EndOfRoundStageOutcomeKind.Completed, events);
        }

        /// <summary>
        /// p.8's "Level Up" step, which p.15 makes mandatory: "If you have enough coins to buy one or
        /// more level ups, you must do so now."
        ///
        /// Every hero's purchases run inside one transition, so the stage either finishes in this one
        /// command - when nobody could afford a level, or nobody's upgrade needed answering - or stops
        /// on p.15's Upgrade a Card and is finished by the answer.
        /// </summary>
        private RoundResolutionResult ResolveLevelUp(EndOfRoundStage stage)
        {
            if (levelUp == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.UnavailableLevelUpTransition);
            }

            // Milestone 7.6. A leveling stopped on p.15's Upgrade a Card is answered by its own command,
            // so a second advance while it is stopped is refused rather than beginning a second
            // leveling - the same guard p.8's push has carried since 6.11. It became reachable with this
            // milestone: until the Pity Coin existed, the staged 1v1 slice could not leave a hero able
            // to afford a level, so no ordinary round ever paused here.
            if (IsPausedLevelUp())
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteLevelUpTransition);
            }

            var result = systemTransitions.TryBegin(
                GuardsDigital.Core.Rules.Progression.HeroLevelUpSystemTransition.DefinitionId,
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId(
                    $"round-{RoundIndex}-" +
                    GuardsDigital.Core.Rules.Progression.HeroLevelUpSystemTransition.DefinitionId.Value));
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(TranslateTransitionRejection(result.RejectionReason));
            }

            var events = DrainLevelUpEvents();
            events.Add(RoundResolutionEvent.SystemTransitionProgressed(RoundIndex, TurnIndex, result));
            return CompleteEndOfRoundStage(
                stage,
                result.TransitionCompleted
                    ? EndOfRoundStageOutcomeKind.Completed
                    : EndOfRoundStageOutcomeKind.Paused,
                events);
        }

        /// <summary>
        /// p.8's "Collect a Pity Coin": "All players who have NOT leveled up this round collect 1 coin."
        ///
        /// Every participant hero is asked, in hero-id order, and the question is the page's own - did
        /// this hero level up this round - rather than p.15's reminder about not having had enough
        /// coins. The two differ: a hero that could afford a level and did not buy one for any other
        /// reason has still not levelled up, so it collects, which is what p.8's words say and what this
        /// implements.
        ///
        /// Deterministic and unconditional, so like the component cleanup it can never pause and carries
        /// no continuation. The coin is granted rather than awarded because nobody was defeated.
        /// </summary>
        private RoundResolutionResult ResolvePityCoins(EndOfRoundStage stage)
        {
            var ledger = cardActions?.CoinLedger;
            if (ledger == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.UnavailablePityCoinLedger);
            }

            var events = new List<RoundResolutionEvent>();
            foreach (var hero in participants.Heroes
                         .OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                if (RoundLeveling.HasLeveled(hero.Id))
                {
                    continue;
                }

                if (!ledger.TryGrant(
                        hero.Id,
                        PityCoinsPerRound,
                        GuardsDigital.Core.Rules.Consequences.CoinGrantKind.PityCoin,
                        out var granted))
                {
                    throw new InvalidOperationException(
                        $"p.8's Pity Coin for '{hero.Id}' could not be granted.");
                }

                events.Add(RoundResolutionEvent.PityCoinCollected(RoundIndex, granted));
            }

            return CompleteEndOfRoundStage(stage, EndOfRoundStageOutcomeKind.Completed, events);
        }

        /// <summary>
        /// What the leveling rules have done since they were last asked, as events.
        ///
        /// The rules report rather than raise for the reason milestone 6.11's Lane-Push stages do: a
        /// step rules implementation answers the transition authority and has no event stream, and this
        /// authority owns the one the round writes to.
        /// </summary>
        private List<RoundResolutionEvent> DrainLevelUpEvents()
        {
            var outcomes = levelUp == null
                ? Array.Empty<GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome>()
                : (IReadOnlyList<GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome>)
                    levelUp.DrainOutcomes();

            // Milestone 7.6. p.8's Pity Coin question is answered from the purchases this round made, so
            // the round records them as it reads them. It is recorded here rather than by the leveling
            // rules because the rules are about p.15 and this is p.8's fact: the same division that
            // keeps the schedule out of the ledger and the ledger out of the purchase.
            foreach (var outcome in outcomes)
            {
                if (outcome.Kind == GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcomeKind.LevelPurchased)
                {
                    RoundLeveling.RecordLevelPurchased(outcome.HeroId);
                }
            }

            return outcomes
                .Select(outcome => RoundResolutionEvent.HeroLevelUpProgressed(
                    RoundIndex, TurnIndex, outcome))
                .ToList();
        }

        private bool IsPausedLevelUp() =>
            levelUp != null &&
            systemTransitions.PendingStep != null &&
            systemTransitions.PendingStep.DefinitionId.Equals(
                GuardsDigital.Core.Rules.Progression.HeroLevelUpSystemTransition.DefinitionId);

        /// <summary>
        /// The tail every end-of-round stage shares: report what the stage did, stop if it paused or
        /// ended the game, otherwise move to the next stage or close the round.
        ///
        /// Extracted at milestone 6.11 because the Lane-Push stage stopped being a refusal and became a
        /// stage that can pause, end the game, or complete like any other, and a second copy of this
        /// tail is a second place for the round's own order to drift.
        /// </summary>
        private RoundResolutionResult CompleteEndOfRoundStage(
            EndOfRoundStage stage,
            EndOfRoundStageOutcomeKind outcomeKind,
            List<RoundResolutionEvent> events)
        {
            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                stage,
                outcomeKind));
            if (outcomeKind == EndOfRoundStageOutcomeKind.Paused)
            {
                return RoundResolutionResult.Accept(events);
            }

            if (outcomeKind == EndOfRoundStageOutcomeKind.GameEnded)
            {
                endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, outcomeKind));
                events.Add(EndGame(stage));
                return RoundResolutionResult.Accept(events);
            }

            // Synthetic/no-board configurations still cross the same round-ending timing boundary
            // when their Minion Battle stage is skipped or handled generically.
            if (stage == EndOfRoundStage.MinionBattle)
            {
                events.AddRange(timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.RoundEnd,
                    RoundIndex,
                    TurnsPerRound),
                    TieBreakerState).Select(value => RoundResolutionEvent.FromTiming(
                        RoundIndex, TurnsPerRound, value)));
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, outcomeKind));
            var nextStage = (EndOfRoundStage)((int)stage + 1);
            if (nextStage != EndOfRoundStage.Complete)
            {
                CurrentEndOfRoundStage = nextStage;
                return RoundResolutionResult.Accept(events);
            }

            var completedRound = new CompletedRoundRecord(
                RoundIndex,
                turnSlots,
                endOfRoundOutcomes);
            completedRounds.Add(completedRound);
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.RoundCompleted,
                completedRound,
                RoundIndex));

            RoundIndex++;
            TurnIndex = 1;
            turnSlots.Clear();
            endOfRoundOutcomes.Clear();

            // Milestone 7.6. p.8 asks its Pity Coin question of "this round", so the record of who
            // levelled is emptied here beside the slots and stage outcomes, which are round-scoped for
            // the same reason. This is also what makes the page's last sentence true without a second
            // rule enforcing it: a coin collected a moment ago cannot buy a level until the next round's
            // Level Up stage, because the stage cursor only moves forward and this round's is behind it.
            RoundLeveling.BeginRound();
            HeroDefeatHistory.BeginRound();

            // Milestone 8.19c1, and round-scoped for exactly the reason those two are: p.8's moment
            // before the Minion Battle belongs to a round, so the next round owes its own.
            preMinionBattleTriggersVisited = false;
            CurrentEndOfRoundStage = null;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.NextRoundStarted,
                completedRound,
                RoundIndex));
            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteCommitment(Func<TurnCardCommitmentResult> command)
        {
            // Milestone 7.7. Before this milestone a command arriving after the match ended was refused
            // here by the phase check below, which reported "not awaiting commitments" - true, and not
            // the reason. p.8 and p.13 both say the game ends, so that is what a refused command is
            // told, at every seam rather than only at the three that already said it.
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotAwaitingCommitments);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.CommitmentCommandRejected,
                    result.RejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromCommitment(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();
            if (CurrentCommitmentAuthority.IsRevealed)
            {
                StartCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteTurnCommand(Func<TurnResolutionResult> command)
        {
            // Milestone 7.7: the same exact reason the commitment seam above now gives.
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.ResolvingTurn || CurrentTurnAuthority == null)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotResolvingTurn);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.TurnCommandRejected,
                    turnReason: result.RejectionReason,
                    cardActionReason: result.CardActionRejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromTurn(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();

            // Milestone 7.4. p.13: "If the last counter is spent, the game ends and the team with no
            // Life counters left, loses the game." The check is here, once, at the boundary every turn
            // command crosses, rather than inside the Attack that spent them: a hero Defeat is the only
            // thing that spends a counter today, but the page ends the game on the counters being gone
            // rather than on which rule took the last one, and asking the state is what keeps that true
            // for the next rule that spends one. "The game ends" is taken literally - nothing later in
            // the turn runs, so the turn is not completed and the round does not advance.
            //
            // Milestone 8.16 widened the question from p.13's counters to every ending the state can
            // already describe, because p.8's mid-turn Lane Push reaches this boundary too and was not
            // being asked about. A push that empties the enemy Battle Zone in the middle of a turn is
            // consumed inside `TurnResolutionAuthority.AdvanceInitiative`, which finishes it and
            // returns; if that push flipped the final Wave or captured the Throne, the match had ended
            // and only the Life counters were consulted here, so the phase never recorded it and the
            // very next projection read faulted with `UnrecordedGameEnd`. Asking the state which
            // ending it holds is the same discipline 7.4 wrote down, applied to all three sentences.
            if (AnUnrecordedEndingExists())
            {
                // Milestone 7.7: through the same seam p.8's two endings cross, so this one names its
                // winner as well as the team the page names, and stops the turn it ended in the middle
                // of rather than leaving its decisions standing where nothing could answer them.
                events.Add(EndGame(null));
                return RoundResolutionResult.Accept(events);
            }

            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private void StartCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            var slot = new RoundTurnSlot(
                RoundIndex,
                TurnIndex,
                CurrentCommitmentAuthority.ExpectedHeroIds.Select(heroId =>
                    new TurnCardAssignment(
                        heroId,
                        CurrentCommitmentAuthority.GetRevealedCardInstanceId(heroId),
                        CurrentCommitmentAuthority.GetStatus(heroId) == TurnCardCommitmentStatus.Passed)),
                instances,
                participants);
            turnSlots.Add(slot);
            events.Add(RoundResolutionEvent.SlotRecorded(slot));

            CurrentTurnAuthority = new TurnResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                cardActions,
                timing,
                RoundIndex,
                TurnIndex,
                lanePush);

            // Milestone 7.2a: the same items the card-action dispatcher holds, so an Initiative item
            // raises the order this turn resolves in.
            CurrentTurnAuthority.BindHeroItems(HeroItems);
            Phase = RoundResolutionPhase.ResolvingTurn;
            foreach (var timingEvent in timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.CardsPlayedAndRevealed,
                RoundIndex,
                TurnIndex),
                tieBreakerState))
                events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnIndex, timingEvent));
            var start = CurrentTurnAuthority.TryStart();
            if (!start.Accepted)
            {
                throw new InvalidOperationException("A validated round turn could not start.");
            }

            foreach (var turnEvent in start.Events)
            {
                events.Add(RoundResolutionEvent.FromTurn(RoundIndex, TurnIndex, turnEvent));
            }

            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }
        }

        private void CompleteCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            tieBreakerState = CurrentTurnAuthority.TieBreakerState;
            if (TurnIndex == TurnsPerRound)
            {
                Phase = RoundResolutionPhase.EndOfRound;
                CurrentEndOfRoundStage = EndOfRoundStage.MinionBattle;
                events.Add(RoundResolutionEvent.Boundary(
                    RoundResolutionEventKind.EndOfRoundEntered,
                    RoundIndex,
                    TurnIndex));
                return;
            }

            TurnIndex++;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.Boundary(
                RoundResolutionEventKind.TurnAdvanced,
                RoundIndex,
                TurnIndex));
        }
    }
}
