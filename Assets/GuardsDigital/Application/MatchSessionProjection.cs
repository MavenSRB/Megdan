using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    public sealed class ValueContributionProjection
    {
        public ValueContributionProjection(ValueContribution contribution, string sourceLabel)
        {
            Contribution = contribution;
            SourceLabel = string.IsNullOrWhiteSpace(sourceLabel)
                ? throw new ArgumentException("A projected contribution requires a readable source label.", nameof(sourceLabel))
                : sourceLabel;
        }

        public ValueContribution Contribution { get; }
        public ValueContributionKind Kind => Contribution.Kind;
        public int Amount => Contribution.Amount;
        public string SourceLabel { get; }
        public MinionId SourceMinionId => Contribution.SourceMinionId;
    }

    public sealed class DefenseChoicePreviewProjection
    {
        public DefenseChoicePreviewProjection(
            CardInstanceId? cardInstanceId,
            string choiceLabel,
            IEnumerable<ValueContributionProjection> defenseContributions,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            AttackResolutionOutcome outcome)
        {
            if (cardInstanceId.HasValue != defenseTotal.HasValue)
            {
                throw new ArgumentException("Only a Defense-card preview has a Defense Total.");
            }

            CardInstanceId = cardInstanceId;
            ChoiceLabel = string.IsNullOrWhiteSpace(choiceLabel)
                ? throw new ArgumentException("A Defense preview requires a readable choice label.", nameof(choiceLabel))
                : choiceLabel;
            var contributions = (defenseContributions ??
                throw new ArgumentNullException(nameof(defenseContributions))).ToArray();
            if (defenseTotal.HasValue && defenseTotal.Value != contributions.Sum(value => value.Amount))
            {
                throw new ArgumentException("A Defense preview total must equal its complete contribution list.");
            }

            DefenseContributions = Array.AsReadOnly(contributions);
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            Outcome = outcome;
        }

        public CardInstanceId? CardInstanceId { get; }
        public bool IsDecline => !CardInstanceId.HasValue;
        public string ChoiceLabel { get; }
        public IReadOnlyList<ValueContributionProjection> DefenseContributions { get; }
        public int? DefenseTotal { get; }
        public DefenseBlockResult BlockResult { get; }
        public AttackResolutionOutcome Outcome { get; }
    }

    public sealed class PendingDefensePreviewProjection
    {
        public PendingDefensePreviewProjection(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            IEnumerable<ValueContributionProjection> attackContributions,
            int attackTotal,
            IEnumerable<DefenseChoicePreviewProjection> choices,
            IEnumerable<ValueContributionProjection> minionContributions)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("A Defense preview requires current resolution and choice identities.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            var projectedAttack = (attackContributions ??
                throw new ArgumentNullException(nameof(attackContributions))).ToArray();
            var projectedChoices = (choices ?? throw new ArgumentNullException(nameof(choices))).ToArray();
            var projectedMinions = (minionContributions ??
                throw new ArgumentNullException(nameof(minionContributions))).ToArray();
            if (projectedAttack.Sum(value => value.Amount) != attackTotal)
            {
                throw new ArgumentException("A Defense preview Attack Total must equal its complete contribution list.");
            }

            if (projectedChoices.Length == 0 || projectedChoices.Count(value => value.IsDecline) > 1 ||
                projectedChoices.Where(value => value.CardInstanceId.HasValue)
                    .Select(value => value.CardInstanceId.Value).Distinct().Count() !=
                projectedChoices.Count(value => value.CardInstanceId.HasValue))
            {
                throw new ArgumentException("A Defense preview requires unique legal choices.");
            }

            if (projectedMinions.Any(value => value.Kind != ValueContributionKind.MinionDefenseModifier ||
                    !value.SourceMinionId.IsDefined) ||
                projectedMinions.Select(value => value.SourceMinionId).Distinct().Count() != projectedMinions.Length)
            {
                throw new ArgumentException("A Defense preview requires unique typed minion contributions.");
            }

            foreach (var choice in projectedChoices)
            {
                var expected = AttackOutcomeComparison.Create(
                    attackTotal,
                    choice.DefenseTotal,
                    choice.BlockResult);
                var choiceMinions = choice.DefenseContributions
                    .Where(value => value.Kind == ValueContributionKind.MinionDefenseModifier)
                    .Select(value => value.Contribution);
                if (choice.Outcome != expected.Outcome ||
                    (!choice.IsDecline && !choiceMinions.SequenceEqual(
                        projectedMinions.Select(value => value.Contribution))))
                {
                    throw new ArgumentException("A Defense choice preview must match shared comparison and minion semantics.");
                }
            }

            AttackContributions = Array.AsReadOnly(projectedAttack);
            AttackTotal = attackTotal;
            Choices = Array.AsReadOnly(projectedChoices);
            MinionContributions = Array.AsReadOnly(projectedMinions);
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public IReadOnlyList<ValueContributionProjection> AttackContributions { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<DefenseChoicePreviewProjection> Choices { get; }
        public IReadOnlyList<ValueContributionProjection> MinionContributions { get; }
    }

    public sealed class FastTravelOptionProjection
    {
        public FastTravelOptionProjection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            IEnumerable<HexCoordinate> legalDestinations)
        {
            if (replacementOffer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedMovementActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Fast Travel projection requires replacement and replaced-action identities.");
            }

            ReplacementOffer = replacementOffer;
            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinations = Array.AsReadOnly((legalDestinations ??
                throw new ArgumentNullException(nameof(legalDestinations))).ToArray());
            if (LegalDestinations.Count == 0 || LegalDestinations.Distinct().Count() != LegalDestinations.Count)
            {
                throw new ArgumentException("Fast Travel projection requires unique legal destinations.", nameof(legalDestinations));
            }
        }

        public ActionOffer ReplacementOffer { get; }
        public ActionDefinitionId ReplacedMovementActionDefinitionId { get; }
        public IReadOnlyList<HexCoordinate> LegalDestinations { get; }
    }

    public readonly struct MatchCommitmentProjection
    {
        public MatchCommitmentProjection(
            HeroId heroId,
            TurnCardCommitmentStatus status,
            bool canPass,
            CardInstanceId? revealedCardInstanceId)
        {
            HeroId = heroId;
            Status = status;
            CanPass = canPass;
            RevealedCardInstanceId = revealedCardInstanceId;
        }

        public HeroId HeroId { get; }
        public TurnCardCommitmentStatus Status { get; }
        public bool CanPass { get; }
        public CardInstanceId? RevealedCardInstanceId { get; }
    }

    public sealed class MatchSessionProjection
    {
        private readonly Lazy<string> diagnosticSnapshot;

        public MatchSessionProjection(
            long version,
            QuickGameSetupState setup,
            BoardMapDefinition selectedMap,
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<ObstacleOccupancy> obstacles,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            TieBreakerState tieBreakerState,
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<CompletedRoundRecord> completedRounds,
            EndOfRoundStage? currentEndOfRoundStage,
            IEnumerable<MatchCommitmentProjection> commitments,
            SelectedInitiativeActor selectedActor,
            PendingInitiativeChoice pendingInitiativeChoice,
            bool isTurnStarted,
            bool isTurnEnded,
            PendingCardActionResolution pendingMovementResolution,
            PendingMovementChoice pendingMovementChoice,
            PendingAttackResolution pendingAttackResolution,
            PendingAttackTargetChoice pendingAttackTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<ActionOffer> availableTurnActions,
            IEnumerable<DirectorCommand> acceptedCommands,
            IEnumerable<RoundResolutionEvent> roundEvents,
            IReadOnlyList<RuleEventLogEntry> diagnosticEvents,
            string authoritativeSnapshot,
            bool isHistoryAvailable,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<PlacedToken> placedTokens,
            IEnumerable<FastTravelOptionProjection> availableFastTravelOptions = null,
            PendingDefensePreviewProjection pendingDefensePreview = null,
            PendingMinionBattleChoice pendingMinionBattleChoice = null,
            PendingHeroRespawnChoice pendingHeroRespawnChoice = null,
            PendingLanePushRequest pendingLanePushRequest = null,
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution pendingSkillResolution = null,
            PendingEffectChoice pendingEffectChoice = null,
            GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                pendingDisplacedMinionCorrection = null,
            IEnumerable<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
                pendingEffectChoicePreview = null)
        {
            Version = version;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            SelectedMap = selectedMap ?? throw new ArgumentNullException(nameof(selectedMap));
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            Definitions = Array.AsReadOnly((definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Cards = Array.AsReadOnly((cards ?? throw new ArgumentNullException(nameof(cards))).Select(CloneCard).ToArray());
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
            Obstacles = Array.AsReadOnly((obstacles ?? throw new ArgumentNullException(nameof(obstacles))).ToArray());
            Minions = Array.AsReadOnly((minions ?? throw new ArgumentNullException(nameof(minions))).ToArray());
            PlacedTokens = Array.AsReadOnly((placedTokens ??
                throw new ArgumentNullException(nameof(placedTokens))).ToArray());
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            TieBreakerState = tieBreakerState;
            TurnSlots = Array.AsReadOnly((turnSlots ?? throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            CompletedRounds = Array.AsReadOnly((completedRounds ?? throw new ArgumentNullException(nameof(completedRounds))).ToArray());
            CurrentEndOfRoundStage = currentEndOfRoundStage;
            Commitments = Array.AsReadOnly((commitments ?? throw new ArgumentNullException(nameof(commitments))).ToArray());
            SelectedActor = selectedActor;
            PendingInitiativeChoice = pendingInitiativeChoice;
            IsTurnStarted = isTurnStarted;
            IsTurnEnded = isTurnEnded;
            PendingMovementResolution = pendingMovementResolution;
            PendingMovementChoice = pendingMovementChoice;
            PendingAttackResolution = pendingAttackResolution;
            PendingAttackTargetChoice = pendingAttackTargetChoice;
            PendingDefenseChoice = pendingDefenseChoice;
            PendingSkillResolution = pendingSkillResolution;
            PendingEffectChoice = pendingEffectChoice;
            AvailableTurnActions = Array.AsReadOnly((availableTurnActions ?? throw new ArgumentNullException(nameof(availableTurnActions))).ToArray());
            AcceptedCommands = Array.AsReadOnly((acceptedCommands ?? throw new ArgumentNullException(nameof(acceptedCommands))).ToArray());
            RoundEvents = Array.AsReadOnly((roundEvents ?? throw new ArgumentNullException(nameof(roundEvents))).ToArray());
            // This is the whole accumulated rule-event log, so copying it on every refresh is a
            // per-command cost that grows with session length - the surviving half of finding
            // `F613-08`. The aggregate hands over a snapshot that cannot change, and copying one of
            // those defends against nothing. The copy is kept for every other input, because this
            // constructor is public and a projection that shares a caller's mutable list is not
            // immutable.
            if (diagnosticEvents == null) throw new ArgumentNullException(nameof(diagnosticEvents));
            DiagnosticEvents = diagnosticEvents is IImmutableEventSnapshot
                ? diagnosticEvents
                : Array.AsReadOnly(diagnosticEvents.ToArray());
            AuthoritativeSnapshot = authoritativeSnapshot ?? throw new ArgumentNullException(nameof(authoritativeSnapshot));
            IsHistoryAvailable = isHistoryAvailable;
            diagnosticSnapshot = new Lazy<string>(
                () => RuleEventLogSnapshotEnvelope.Serialize(AuthoritativeSnapshot, DiagnosticEvents),
                LazyThreadSafetyMode.None);
            AvailableFastTravelOptions = Array.AsReadOnly((availableFastTravelOptions ??
                Enumerable.Empty<FastTravelOptionProjection>()).ToArray());
            PendingDefensePreview = pendingDefensePreview;
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingHeroRespawnChoice = pendingHeroRespawnChoice;
            PendingLanePushRequest = pendingLanePushRequest;
            PendingDisplacedMinionCorrection = pendingDisplacedMinionCorrection;
            PendingEffectChoicePreview = Array.AsReadOnly((pendingEffectChoicePreview ??
                Enumerable.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>())
                .ToArray());
        }

        public long Version { get; }
        public QuickGameSetupState Setup { get; }
        public BoardMapDefinition SelectedMap { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<ObstacleOccupancy> Obstacles { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }

        /// <summary>
        /// Milestone 6.2d: the tokens standing on the board, as the board authority holds them.
        ///
        /// It supersedes milestone 6.2's decision that presentation receives no token projection. That
        /// decision was correct while nothing consumed one; 6.6a printed a Replace and a token-only
        /// Remove onto a configured card, and 6.2b had already assigned this to the first presentation
        /// milestone that needed a token view. What is handed over is the authoritative instance, not a
        /// summary of it: the stable identity, the coordinate, and the trusted <c>TokenDefinition</c>
        /// object the catalog supplied. That last part is what keeps presentation free of any component
        /// catalog reference - a display name is read off the instance it belongs to, and nothing here
        /// is derived, so no rule can come to depend on a reading only this projection performs.
        /// </summary>
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        public EndOfRoundStage? CurrentEndOfRoundStage { get; }
        public IReadOnlyList<MatchCommitmentProjection> Commitments { get; }
        public SelectedInitiativeActor SelectedActor { get; }
        public PendingInitiativeChoice PendingInitiativeChoice { get; }
        public bool IsTurnStarted { get; }
        public bool IsTurnEnded { get; }
        public PendingCardActionResolution PendingMovementResolution { get; }
        public PendingMovementChoice PendingMovementChoice { get; }
        public PendingAttackResolution PendingAttackResolution { get; }
        public PendingAttackTargetChoice PendingAttackTargetChoice { get; }
        public PendingDefenseResponseChoice PendingDefenseChoice { get; }

        /// <summary>
        /// The Skill effect currently resolving, and the decision it is waiting on. Presentation reads
        /// the projected invocations off the choice; it never derives Skill legality itself.
        /// </summary>
        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution { get; }

        public PendingEffectChoice PendingEffectChoice { get; }

        /// <summary>
        /// Milestone 6.5c: what each space-valued candidate of that choice means on the board right
        /// now - the object standing there, and where a consuming Push would leave it. Empty unless
        /// such a choice is pending. It is the effect-frame counterpart of
        /// <see cref="PendingDefensePreview"/>: an authority-computed reading of a decision the player
        /// has not made yet, so presentation can show consequences without deriving any rule.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            PendingEffectChoicePreview { get; }

        /// <summary>
        /// Milestone 6.4a's paused after-action correction, or <c>null</c>. Presentation reads the
        /// projected invocations off the choice; it never derives paths, distances, Battle-Zone
        /// membership, occupancy, or Tie-Breaker ordering itself.
        /// </summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection { get; }

        public IReadOnlyList<ActionOffer> AvailableTurnActions { get; }
        public IReadOnlyList<DirectorCommand> AcceptedCommands { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }
        public IReadOnlyList<RuleEventLogEntry> DiagnosticEvents { get; }
        public string AuthoritativeSnapshot { get; }

        /// <summary>
        /// The authoritative snapshot plus the ordered rule-event log, encoded on first read and
        /// then held.
        /// </summary>
        /// <remarks>
        /// Computed from <see cref="AuthoritativeSnapshot"/> and <see cref="DiagnosticEvents"/>,
        /// both of which this projection already holds as immutable copies. That matters: a lazy
        /// value reading the live authorities would hand a caller holding an older projection the
        /// state as it is now, and fixtures compare exactly that way. Deferring it is what stopped
        /// every command re-encoding the whole accumulated log - finding `F613-08`'s growth with
        /// session length - because nothing on the command path reads this.
        /// </remarks>
        public string DiagnosticSnapshot => diagnosticSnapshot.Value;

        /// <summary>
        /// Whether this session can still record accepted commands. False once a snapshot has been
        /// loaded, which permanently disables history.
        /// </summary>
        /// <remarks>
        /// Distinct from an empty <see cref="AcceptedCommands"/>. A fresh session also has nothing
        /// to undo, but its history is available and the next accepted command will appear there;
        /// after a snapshot load no command ever will. Finding `F613-10` is that the two were
        /// indistinguishable to everything downstream, including the Undo control.
        /// </remarks>
        public bool IsHistoryAvailable { get; }
        public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions { get; }
        public PendingDefensePreviewProjection PendingDefensePreview { get; }
        public PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        public PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }
        public PendingLanePushRequest PendingLanePushRequest { get; }

        public bool IsTurnActive =>
            Phase == RoundResolutionPhase.ResolvingTurn && IsTurnStarted && !IsTurnEnded;

        public MatchCommitmentProjection GetCommitment(HeroId heroId)
        {
            return Commitments.Single(value => value.HeroId.Equals(heroId));
        }

        private static CardInstance CloneCard(CardInstance card)
        {
            return new CardInstance(card.Id, card.DefinitionReference, card.OwnerId, card.State);
        }
    }
}
