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

    /// <summary>
    /// Milestone 6.3a: one currently available replaceable Attack, the tokens the general Clear
    /// could take instead, and the exact command for every subset a player may confirm.
    ///
    /// Milestone 6.3 implemented Clear and deliberately shipped no player-facing selection, so the
    /// configured HUD had no way to submit the `PerformClear` command that already existed. This is
    /// what that surface reads. Nothing here is new legality: the offer is
    /// <see cref="GeneralRuleActionOfferQuery.GetAvailableClearOffer"/>'s and the candidates are
    /// <see cref="ClearActionQuery.FindLegalTokenIds"/>'s, which are the same two answers the
    /// command path asks on its way in.
    ///
    /// The invocations are enumerated here rather than composed by presentation, because a payload a
    /// view builds is a payload a view can build wrongly. Adjacency bounds the candidate set to the
    /// six neighbouring spaces, so the enumeration is at most 64 commands and is refused outright
    /// above that - a bound this type asserts rather than assumes, since an unbounded candidate set
    /// would silently make the enumeration exponential.
    /// </summary>
    public sealed class ClearOptionProjection
    {
        /// <summary>The six neighbours of the actor's space, which is every space a Clear can reach.</summary>
        public const int MaximumCandidates = 6;

        public ClearOptionProjection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<PlacedToken> adjacentTokens)
        {
            if (replacementOffer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedAttackActionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "Clear projection requires replacement and replaced-action identities.");
            }

            ReplacementOffer = replacementOffer;
            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId;
            AdjacentTokens = Array.AsReadOnly((adjacentTokens ??
                throw new ArgumentNullException(nameof(adjacentTokens))).ToArray());
            if (AdjacentTokens.Any(token => token == null) ||
                AdjacentTokens.Select(token => token.Id).Distinct().Count() != AdjacentTokens.Count)
            {
                throw new ArgumentException(
                    "Clear projection requires distinct placed tokens.", nameof(adjacentTokens));
            }

            if (AdjacentTokens.Count > MaximumCandidates)
            {
                throw new ArgumentException(
                    "A Clear can only reach the actor's six adjacent spaces.", nameof(adjacentTokens));
            }

            SubsetInvocations = Array.AsReadOnly(EnumerateSubsets(
                replacedAttackActionDefinitionId,
                AdjacentTokens.Select(token => token.Id).ToArray()));
        }

        public ActionOffer ReplacementOffer { get; }
        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }

        /// <summary>
        /// The tokens this Clear may take, in the authority's own order. Each is the board's own
        /// instance, so a caller reads the coordinate and the trusted definition off the piece rather
        /// than looking either up.
        /// </summary>
        public IReadOnlyList<PlacedToken> AdjacentTokens { get; }

        /// <summary>
        /// One canonical <c>PerformClear</c> per confirmable subset, the empty one first. p.13 permits
        /// any number, and milestone 6.3 already accepts none, so confirming nothing is an invocation
        /// like any other rather than a case a surface has to special-case.
        /// </summary>
        public IReadOnlyList<DirectorCommand> SubsetInvocations { get; }

        /// <summary>
        /// The one invocation that clears exactly these tokens, or <c>null</c> when the set is not a
        /// subset this option offers. Order and duplication do not matter: a subset is a set, and the
        /// command orders its own ids.
        /// </summary>
        public DirectorCommand FindInvocation(IEnumerable<TokenInstanceId> selectedTokenIds)
        {
            if (selectedTokenIds == null)
            {
                throw new ArgumentNullException(nameof(selectedTokenIds));
            }

            var selected = new HashSet<TokenInstanceId>(selectedTokenIds);
            return SubsetInvocations.SingleOrDefault(invocation =>
                invocation.TokenInstanceIds.Count == selected.Count &&
                invocation.TokenInstanceIds.All(selected.Contains));
        }

        private static DirectorCommand[] EnumerateSubsets(
            ActionDefinitionId replacedAttackActionDefinitionId,
            IReadOnlyList<TokenInstanceId> candidates)
        {
            var invocations = new DirectorCommand[1 << candidates.Count];
            for (var mask = 0; mask < invocations.Length; mask++)
            {
                var subset = new List<TokenInstanceId>();
                for (var index = 0; index < candidates.Count; index++)
                {
                    if ((mask & (1 << index)) != 0)
                    {
                        subset.Add(candidates[index]);
                    }
                }

                invocations[mask] = DirectorCommand.PerformClear(
                    replacedAttackActionDefinitionId, subset);
            }

            return invocations;
        }
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
                pendingEffectChoicePreview = null,
            IEnumerable<ClearOptionProjection> availableClearOptions = null,
            GuardsDigital.Core.Rules.CardResolution.PendingImmediateLanePushRequest
                pendingImmediateLanePush = null,
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                pendingSystemTransitionStep = null)
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
            PendingImmediateLanePush = pendingImmediateLanePush;
            PendingSystemTransitionStep = pendingSystemTransitionStep;
            PendingDisplacedMinionCorrection = pendingDisplacedMinionCorrection;
            PendingEffectChoicePreview = Array.AsReadOnly((pendingEffectChoicePreview ??
                Enumerable.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>())
                .ToArray());
            AvailableClearOptions = Array.AsReadOnly((availableClearOptions ??
                Enumerable.Empty<ClearOptionProjection>()).ToArray());
            if (AvailableClearOptions.Select(option => option.ReplacedAttackActionDefinitionId)
                    .Distinct().Count() != AvailableClearOptions.Count)
            {
                throw new ArgumentException(
                    "One replaceable Attack carries at most one Clear option.",
                    nameof(availableClearOptions));
            }
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

        /// <summary>
        /// Milestone 6.3a: the replaceable Attacks the current actor could take a Clear instead of,
        /// each carrying the tokens it would offer and the command for every confirmable subset.
        ///
        /// Empty whenever the current card prints no available replaceable Attack, which is what makes
        /// the absence of a Clear control a projected fact rather than a presentation decision. An
        /// option with no adjacent token is still an option: p.13 permits clearing none, so a player
        /// may take the replacement and resolve the card either way.
        /// </summary>
        public IReadOnlyList<ClearOptionProjection> AvailableClearOptions { get; }
        public PendingDefensePreviewProjection PendingDefensePreview { get; }
        public PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        public PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }
        public PendingLanePushRequest PendingLanePushRequest { get; }

        /// <summary>
        /// Milestone 6.10a: where a multi-step system transition is stopped, or <c>null</c>. This is
        /// authoritative state reaching a reader, not a presentation surface - the milestone's non-goals
        /// forbid production presentation, and nothing draws this. It is projected because a paused
        /// transition is a state a caller has to be able to see to answer, exactly as the Minion-Battle
        /// choice beside it is.
        /// </summary>
        public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            PendingSystemTransitionStep { get; }

        /// <summary>
        /// Milestone 6.9: rulebook p.8's mid-turn Lane-Push request, or <c>null</c>. Projected beside its
        /// end-of-round sibling so a reader can tell a turn stopped at the immediate checkpoint from a
        /// turn stopped for any other reason. Nothing draws it - this milestone adds no control, because
        /// there is no decision for a player to make until milestones 6.10 and 6.11 resolve the push.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.PendingImmediateLanePushRequest
            PendingImmediateLanePush { get; }

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
