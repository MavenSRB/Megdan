using System.Collections.Generic;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.33. The presentation-fidelity half of one seat's wire projection: the
    /// pending-choice candidate detail and previews, the three resolution envelopes, the offer
    /// rows, the team-agreement surface, and the completed-round history - every field milestone
    /// 8.32 deferred here by name.
    /// </summary>
    /// <remarks>
    /// <para>Like the acting surface it completes, each row restates what the owner-scoped
    /// <see cref="SeatProjection"/> already holds and nothing else, so the scoping rule needs no
    /// second statement: a decision this seat does not own arrives as the same <c>null</c> its
    /// projection reads, and what is absent from the seat's view still has no field to travel in.</para>
    /// <para>Invocation lists are deliberately not rows. Every command a pending choice mints
    /// already crosses the wire typed, inside <see cref="NetworkSeatProjectionRecord.DecisionContractCommands"/>
    /// and <see cref="NetworkSeatProjectionRecord.AuthorizedCommands"/>, so a row repeating them
    /// would be a second copy that could disagree. The one exception is the team agreement's own
    /// statements, which are not <see cref="GuardsDigital.Core.Replay.DirectorCommand"/>s and
    /// travel typed here so a client can submit them back verbatim.</para>
    /// <para>What is absent is decided, not forgotten: the effect resolution's continuation
    /// frames, completed outcomes and accepted choices are rules-execution state - the roadmap's
    /// own boundary is "without client-owned rules authority" - and the movement session's
    /// per-step events restate its route. Setup, map, participants and definitions stay off the
    /// wire because the admission tuple already pins the same content on both sides.</para>
    /// </remarks>
    public sealed class NetworkProjectionDetailRecord
    {
        public NetworkProjectionDetailRecord(
            NetworkTeamAgreementRow teamAgreement,
            IReadOnlyList<NetworkCompletedRoundRow> completedRounds,
            NetworkInitiativeChoiceRow pendingInitiativeChoice,
            NetworkMovementResolutionRow pendingMovementResolution,
            NetworkMovementChoiceRow pendingMovementChoice,
            NetworkAttackResolutionRow pendingAttackResolution,
            NetworkAttackTargetChoiceRow pendingAttackTargetChoice,
            IReadOnlyList<NetworkAttackTargetDefensePreviewRow> pendingAttackTargetDefensePreviews,
            NetworkDefenseChoiceRow pendingDefenseChoice,
            NetworkDefensePreviewRow pendingDefensePreview,
            NetworkSkillResolutionRow pendingSkillResolution,
            NetworkEffectChoiceRow pendingEffectChoice,
            IReadOnlyList<NetworkEffectChoicePreviewRow> pendingEffectChoicePreview,
            NetworkCorrectionRow pendingDisplacedMinionCorrection,
            NetworkMinionBattleChoiceRow pendingMinionBattleChoice,
            NetworkHeroRespawnChoiceRow pendingHeroRespawnChoice,
            NetworkSystemTransitionStepRow pendingSystemTransitionStep,
            NetworkSystemTransitionDescriptionRow pendingSystemTransitionDescription,
            NetworkRemovalPreventionRow pendingRemovalPrevention,
            NetworkTurnReleaseRow pendingTurnRelease,
            IReadOnlyList<NetworkActionOfferRow> availableTurnActions,
            IReadOnlyList<NetworkFastTravelOptionRow> availableFastTravelOptions,
            IReadOnlyList<NetworkClearOptionRow> availableClearOptions,
            NetworkActionOfferRow availableHoldOffer,
            NetworkUndoMarkerRow undoMarker = null)
        {
            UndoMarker = undoMarker;
            TeamAgreement = teamAgreement;
            CompletedRounds = completedRounds;
            PendingInitiativeChoice = pendingInitiativeChoice;
            PendingMovementResolution = pendingMovementResolution;
            PendingMovementChoice = pendingMovementChoice;
            PendingAttackResolution = pendingAttackResolution;
            PendingAttackTargetChoice = pendingAttackTargetChoice;
            PendingAttackTargetDefensePreviews = pendingAttackTargetDefensePreviews;
            PendingDefenseChoice = pendingDefenseChoice;
            PendingDefensePreview = pendingDefensePreview;
            PendingSkillResolution = pendingSkillResolution;
            PendingEffectChoice = pendingEffectChoice;
            PendingEffectChoicePreview = pendingEffectChoicePreview;
            PendingDisplacedMinionCorrection = pendingDisplacedMinionCorrection;
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingHeroRespawnChoice = pendingHeroRespawnChoice;
            PendingSystemTransitionStep = pendingSystemTransitionStep;
            PendingSystemTransitionDescription = pendingSystemTransitionDescription;
            PendingRemovalPrevention = pendingRemovalPrevention;
            PendingTurnRelease = pendingTurnRelease;
            AvailableTurnActions = availableTurnActions;
            AvailableFastTravelOptions = availableFastTravelOptions;
            AvailableClearOptions = availableClearOptions;
            AvailableHoldOffer = availableHoldOffer;
        }

        /// <summary>The public agreement surface, or null while none is standing for this seat.</summary>
        public NetworkTeamAgreementRow TeamAgreement { get; }

        public IReadOnlyList<NetworkCompletedRoundRow> CompletedRounds { get; }
        public NetworkInitiativeChoiceRow PendingInitiativeChoice { get; }
        public NetworkMovementResolutionRow PendingMovementResolution { get; }
        public NetworkMovementChoiceRow PendingMovementChoice { get; }
        public NetworkAttackResolutionRow PendingAttackResolution { get; }
        public NetworkAttackTargetChoiceRow PendingAttackTargetChoice { get; }
        public IReadOnlyList<NetworkAttackTargetDefensePreviewRow> PendingAttackTargetDefensePreviews { get; }
        public NetworkDefenseChoiceRow PendingDefenseChoice { get; }
        public NetworkDefensePreviewRow PendingDefensePreview { get; }
        public NetworkSkillResolutionRow PendingSkillResolution { get; }
        public NetworkEffectChoiceRow PendingEffectChoice { get; }
        public IReadOnlyList<NetworkEffectChoicePreviewRow> PendingEffectChoicePreview { get; }
        public NetworkCorrectionRow PendingDisplacedMinionCorrection { get; }
        public NetworkMinionBattleChoiceRow PendingMinionBattleChoice { get; }
        public NetworkHeroRespawnChoiceRow PendingHeroRespawnChoice { get; }
        public NetworkSystemTransitionStepRow PendingSystemTransitionStep { get; }
        public NetworkSystemTransitionDescriptionRow PendingSystemTransitionDescription { get; }
        public NetworkRemovalPreventionRow PendingRemovalPrevention { get; }

        /// <summary>
        /// Milestone 8.35g. The finished turn awaiting its own player, or null while none stands.
        /// Public to every seat: the whole table is waiting on it, and it names nothing private -
        /// the hero and side are already public, and the card it names has finished resolving into
        /// the public turn area.
        /// </summary>
        public NetworkTurnReleaseRow PendingTurnRelease { get; }

        /// <summary>
        /// Milestone 8.35r. The rewind currently standing, or null while none is. Public to every
        /// seat for the same reason the standing decision's owner is: that a player is taking their
        /// own turn back is something the whole table can see happening, and the marker is what lets
        /// the other five read the log's disappearing lines as a rewind rather than as a fault.
        /// </summary>
        public NetworkUndoMarkerRow UndoMarker { get; }
        public IReadOnlyList<NetworkActionOfferRow> AvailableTurnActions { get; }
        public IReadOnlyList<NetworkFastTravelOptionRow> AvailableFastTravelOptions { get; }
        public IReadOnlyList<NetworkClearOptionRow> AvailableClearOptions { get; }

        /// <summary>The Hold offer, or null while none stands.</summary>
        public NetworkActionOfferRow AvailableHoldOffer { get; }
    }

    /// <summary>One hex space, in the flat DTO shape every other row uses.</summary>
    public readonly struct NetworkCoordinateRow
    {
        public NetworkCoordinateRow(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }
    }

    /// <summary>One rules object: a hero or a minion, never both.</summary>
    public sealed class NetworkUnitRow
    {
        public NetworkUnitRow(int kind, string heroId, string minionId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
        }

        public int Kind { get; }
        public string HeroId { get; }
        public string MinionId { get; }
    }

    /// <summary>One Attack target: a hero or a minion, in the target choice's own kind space.</summary>
    public sealed class NetworkAttackTargetRow
    {
        public NetworkAttackTargetRow(int kind, string heroId, string minionId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
        }

        public int Kind { get; }
        public string HeroId { get; }
        public string MinionId { get; }
    }

    /// <summary>One typed value contribution, with the readable label its projection composed.</summary>
    public sealed class NetworkValueContributionRow
    {
        public NetworkValueContributionRow(
            int kind,
            int channel,
            int amount,
            string sourceLabel,
            string sourceCardInstanceId,
            string sourceActionDefinitionId,
            string sourceMinionId,
            string sourceActiveEffectId,
            string sourceContinuousSourceId)
        {
            Kind = kind;
            Channel = channel;
            Amount = amount;
            SourceLabel = sourceLabel;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            SourceMinionId = sourceMinionId;
            SourceActiveEffectId = sourceActiveEffectId;
            SourceContinuousSourceId = sourceContinuousSourceId;
        }

        public int Kind { get; }
        public int Channel { get; }
        public int Amount { get; }
        public string SourceLabel { get; }
        public string SourceCardInstanceId { get; }
        public string SourceActionDefinitionId { get; }
        public string SourceMinionId { get; }
        public string SourceActiveEffectId { get; }
        public string SourceContinuousSourceId { get; }
    }

    /// <summary>One action offer, including the card-state transition taking it would perform.</summary>
    public sealed class NetworkActionOfferRow
    {
        public NetworkActionOfferRow(
            string sourceCardInstanceId,
            string sourceDefinitionId,
            string sourceContentRevision,
            string actionDefinitionId,
            string actorId,
            int invocationContext,
            bool isAvailable,
            int attackRange,
            bool hasCardStateTransition,
            int requiredLocation,
            int destinationLocation,
            int unavailabilityKind = 0,
            int blockedActivity = 0,
            string blockingHeroId = null,
            string blockingCardInstanceId = null,
            string blockingActiveEffectId = null)
        {
            UnavailabilityKind = unavailabilityKind;
            BlockedActivity = blockedActivity;
            BlockingHeroId = blockingHeroId ?? string.Empty;
            BlockingCardInstanceId = blockingCardInstanceId ?? string.Empty;
            BlockingActiveEffectId = blockingActiveEffectId ?? string.Empty;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionId = sourceDefinitionId;
            SourceContentRevision = sourceContentRevision;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            IsAvailable = isAvailable;
            AttackRange = attackRange;
            HasCardStateTransition = hasCardStateTransition;
            RequiredLocation = requiredLocation;
            DestinationLocation = destinationLocation;
        }

        public string SourceCardInstanceId { get; }
        public string SourceDefinitionId { get; }
        public string SourceContentRevision { get; }
        public string ActionDefinitionId { get; }
        public string ActorId { get; }
        public int InvocationContext { get; }
        public bool IsAvailable { get; }
        public int AttackRange { get; }
        public bool HasCardStateTransition { get; }
        public int RequiredLocation { get; }
        public int DestinationLocation { get; }

        /// <summary>
        /// Milestone 8.35l. Why a greyed offer is greyed, where the authority could say.
        ///
        /// <para>It crosses the wire rather than being derived on the client because deriving it would
        /// mean a seat re-reading the rule modifications for itself - a second reading of the rules,
        /// free to disagree with the host's - and because the only client-side material available is a
        /// flat list of the cards standing effects came off, which cannot say which of them refused
        /// this action. The five fields are the applied modification's own provenance and nothing
        /// more; the words a player reads are composed by presentation from the hero and card named
        /// here. This is the field group that advanced the protocol to
        /// <c>guards-private-online/5</c>.</para>
        /// </summary>
        public int UnavailabilityKind { get; }

        public int BlockedActivity { get; }

        public string BlockingHeroId { get; }

        public string BlockingCardInstanceId { get; }

        public string BlockingActiveEffectId { get; }
    }

    /// <summary>One typed effect value, one arm defined per its kind.</summary>
    public sealed class NetworkEffectValueRow
    {
        public NetworkEffectValueRow(
            int kind,
            bool booleanValue,
            int integerValue,
            string heroId,
            string minionId,
            string cardInstanceId,
            int q,
            int r,
            string nodeId)
        {
            Kind = kind;
            BooleanValue = booleanValue;
            IntegerValue = integerValue;
            HeroId = heroId;
            MinionId = minionId;
            CardInstanceId = cardInstanceId;
            Q = q;
            R = r;
            NodeId = nodeId;
        }

        public int Kind { get; }
        public bool BooleanValue { get; }
        public int IntegerValue { get; }
        public string HeroId { get; }
        public string MinionId { get; }
        public string CardInstanceId { get; }
        public int Q { get; }
        public int R { get; }
        public string NodeId { get; }
    }

    public sealed class NetworkTeamAgreementRow
    {
        public NetworkTeamAgreementRow(
            int kind,
            string proposalId,
            int choosingSide,
            IReadOnlyList<NetworkAgreementCandidateRow> candidates,
            IReadOnlyList<NetworkAgreementParticipantRow> participants,
            IReadOnlyList<NetworkUnitRow> selectedUnits,
            int requiredSelectionCount,
            bool selectionIsConfirmable,
            IReadOnlyList<TeamAgreementCommand> authorizedStatements)
        {
            Kind = kind;
            ProposalId = proposalId;
            ChoosingSide = choosingSide;
            Candidates = candidates;
            Participants = participants;
            SelectedUnits = selectedUnits;
            RequiredSelectionCount = requiredSelectionCount;
            SelectionIsConfirmable = selectionIsConfirmable;
            AuthorizedStatements = authorizedStatements;
        }

        public int Kind { get; }
        public string ProposalId { get; }
        public int ChoosingSide { get; }
        public IReadOnlyList<NetworkAgreementCandidateRow> Candidates { get; }
        public IReadOnlyList<NetworkAgreementParticipantRow> Participants { get; }
        public IReadOnlyList<NetworkUnitRow> SelectedUnits { get; }
        public int RequiredSelectionCount { get; }
        public bool SelectionIsConfirmable { get; }

        /// <summary>
        /// The statements this seat may submit, typed, so the client sends back the host's own
        /// instance rather than composing one.
        /// </summary>
        public IReadOnlyList<TeamAgreementCommand> AuthorizedStatements { get; }
    }

    public sealed class NetworkAgreementCandidateRow
    {
        public NetworkAgreementCandidateRow(string cardInstanceId, string heroId)
        {
            CardInstanceId = cardInstanceId;
            HeroId = heroId;
        }

        public string CardInstanceId { get; }
        public string HeroId { get; }
    }

    public sealed class NetworkAgreementParticipantRow
    {
        public NetworkAgreementParticipantRow(
            string heroId, bool hasStatement, string preferredCardInstanceId, bool agreed)
        {
            HeroId = heroId;
            HasStatement = hasStatement;
            PreferredCardInstanceId = preferredCardInstanceId;
            Agreed = agreed;
        }

        public string HeroId { get; }
        public bool HasStatement { get; }

        /// <summary>Null until the participant has stated an Initiative preference.</summary>
        public string PreferredCardInstanceId { get; }

        public bool Agreed { get; }
    }

    public sealed class NetworkCompletedRoundRow
    {
        public NetworkCompletedRoundRow(
            int roundIndex,
            IReadOnlyList<NetworkTurnSlotRow> turnSlots,
            IReadOnlyList<NetworkEndOfRoundOutcomeRow> endOfRoundOutcomes)
        {
            RoundIndex = roundIndex;
            TurnSlots = turnSlots;
            EndOfRoundOutcomes = endOfRoundOutcomes;
        }

        public int RoundIndex { get; }
        public IReadOnlyList<NetworkTurnSlotRow> TurnSlots { get; }
        public IReadOnlyList<NetworkEndOfRoundOutcomeRow> EndOfRoundOutcomes { get; }
    }

    public sealed class NetworkEndOfRoundOutcomeRow
    {
        public NetworkEndOfRoundOutcomeRow(int stage, int kind)
        {
            Stage = stage;
            Kind = kind;
        }

        public int Stage { get; }
        public int Kind { get; }
    }

    public sealed class NetworkInitiativeChoiceRow
    {
        public NetworkInitiativeChoiceRow(
            int owningSide,
            int initiativeTotal,
            IReadOnlyList<string> candidateCardInstanceIds,
            bool flipTieBreakerAfterResolution)
        {
            OwningSide = owningSide;
            InitiativeTotal = initiativeTotal;
            CandidateCardInstanceIds = candidateCardInstanceIds;
            FlipTieBreakerAfterResolution = flipTieBreakerAfterResolution;
        }

        public int OwningSide { get; }
        public int InitiativeTotal { get; }
        public IReadOnlyList<string> CandidateCardInstanceIds { get; }
        public bool FlipTieBreakerAfterResolution { get; }
    }

    /// <summary>
    /// The movement resolution envelope: its identity surface plus the committed route. The
    /// per-step events restate the route pair by pair and stay host-side.
    /// </summary>
    public sealed class NetworkMovementResolutionRow
    {
        public NetworkMovementResolutionRow(
            string resolutionId,
            string sourceCardInstanceId,
            string sourceDefinitionId,
            string sourceContentRevision,
            string actionDefinitionId,
            string actorId,
            int invocationContext,
            string currentStepId,
            int movementAllowance,
            int remainingAllowance,
            bool movementIsComplete,
            IReadOnlyList<NetworkCoordinateRow> route)
        {
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionId = sourceDefinitionId;
            SourceContentRevision = sourceContentRevision;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            CurrentStepId = currentStepId;
            MovementAllowance = movementAllowance;
            RemainingAllowance = remainingAllowance;
            MovementIsComplete = movementIsComplete;
            Route = route;
        }

        public string ResolutionId { get; }
        public string SourceCardInstanceId { get; }
        public string SourceDefinitionId { get; }
        public string SourceContentRevision { get; }
        public string ActionDefinitionId { get; }
        public string ActorId { get; }
        public int InvocationContext { get; }
        public string CurrentStepId { get; }
        public int MovementAllowance { get; }
        public int RemainingAllowance { get; }
        public bool MovementIsComplete { get; }
        public IReadOnlyList<NetworkCoordinateRow> Route { get; }
    }

    public sealed class NetworkMovementDestinationRow
    {
        public NetworkMovementDestinationRow(
            int q, int r, int cost, IReadOnlyList<NetworkCoordinateRow> route)
        {
            Q = q;
            R = r;
            Cost = cost;
            Route = route;
        }

        public int Q { get; }
        public int R { get; }
        public int Cost { get; }
        public IReadOnlyList<NetworkCoordinateRow> Route { get; }
    }

    public sealed class NetworkMovementChoiceRow
    {
        public NetworkMovementChoiceRow(
            string resolutionId,
            string choiceId,
            string chooserId,
            string movingHeroId,
            int originQ,
            int originR,
            int remainingAllowance,
            bool canStop,
            IReadOnlyList<NetworkMovementDestinationRow> legalDestinations)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            MovingHeroId = movingHeroId;
            OriginQ = originQ;
            OriginR = originR;
            RemainingAllowance = remainingAllowance;
            CanStop = canStop;
            LegalDestinations = legalDestinations;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public string ChooserId { get; }
        public string MovingHeroId { get; }
        public int OriginQ { get; }
        public int OriginR { get; }
        public int RemainingAllowance { get; }
        public bool CanStop { get; }
        public IReadOnlyList<NetworkMovementDestinationRow> LegalDestinations { get; }
    }

    /// <summary>
    /// The Attack resolution envelope: identity, phase, target and the totals that stand. The
    /// contribution detail a player reads belongs to the Defense preview rows, which carry it.
    /// </summary>
    public sealed class NetworkAttackResolutionRow
    {
        public NetworkAttackResolutionRow(
            string resolutionId,
            string sourceCardInstanceId,
            string sourceDefinitionId,
            string sourceContentRevision,
            string actionDefinitionId,
            string actorId,
            int invocationContext,
            string currentStepId,
            int phase,
            NetworkAttackTargetRow target,
            bool hasAttackTotal,
            int attackTotal,
            NetworkActionOfferRow defenseOffer,
            bool hasDefenseTotal,
            int defenseTotal,
            NetworkAttackTargetRow excludedTarget,
            int blockResult,
            NetworkCoordinateRow? targetSpaceBeforeConsequences,
            NetworkCoordinateRow? actorSpaceAtActionStart)
        {
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionId = sourceDefinitionId;
            SourceContentRevision = sourceContentRevision;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            CurrentStepId = currentStepId;
            Phase = phase;
            Target = target;
            HasAttackTotal = hasAttackTotal;
            AttackTotal = attackTotal;
            DefenseOffer = defenseOffer;
            HasDefenseTotal = hasDefenseTotal;
            DefenseTotal = defenseTotal;
            ExcludedTarget = excludedTarget;
            BlockResult = blockResult;
            TargetSpaceBeforeConsequences = targetSpaceBeforeConsequences;
            ActorSpaceAtActionStart = actorSpaceAtActionStart;
        }

        public string ResolutionId { get; }
        public string SourceCardInstanceId { get; }
        public string SourceDefinitionId { get; }
        public string SourceContentRevision { get; }
        public string ActionDefinitionId { get; }
        public string ActorId { get; }
        public int InvocationContext { get; }
        public string CurrentStepId { get; }
        public int Phase { get; }

        /// <summary>Null while the Attack has no target yet.</summary>
        public NetworkAttackTargetRow Target { get; }

        public bool HasAttackTotal { get; }
        public int AttackTotal { get; }

        /// <summary>Null while no Defense response stands.</summary>
        public NetworkActionOfferRow DefenseOffer { get; }

        public bool HasDefenseTotal { get; }
        public int DefenseTotal { get; }

        /// <summary>Null on an instance that may target anything.</summary>
        public NetworkAttackTargetRow ExcludedTarget { get; }

        public int BlockResult { get; }
        public NetworkCoordinateRow? TargetSpaceBeforeConsequences { get; }
        public NetworkCoordinateRow? ActorSpaceAtActionStart { get; }
    }

    public sealed class NetworkAttackTargetChoiceRow
    {
        public NetworkAttackTargetChoiceRow(
            string resolutionId,
            string choiceId,
            string chooserId,
            IReadOnlyList<NetworkAttackTargetRow> legalTargets)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            LegalTargets = legalTargets;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public string ChooserId { get; }
        public IReadOnlyList<NetworkAttackTargetRow> LegalTargets { get; }
    }

    public sealed class NetworkAttackTargetDefensePreviewRow
    {
        public NetworkAttackTargetDefensePreviewRow(
            string heroId, IReadOnlyList<NetworkValueContributionRow> minionContributions)
        {
            HeroId = heroId;
            MinionContributions = minionContributions;
        }

        public string HeroId { get; }
        public IReadOnlyList<NetworkValueContributionRow> MinionContributions { get; }
    }

    public sealed class NetworkDefenseChoiceRow
    {
        public NetworkDefenseChoiceRow(
            string resolutionId,
            string choiceId,
            string chooserId,
            string attackerId,
            int attackTotal,
            bool canDecline,
            IReadOnlyList<NetworkActionOfferRow> legalDefenseOffers)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            AttackerId = attackerId;
            AttackTotal = attackTotal;
            CanDecline = canDecline;
            LegalDefenseOffers = legalDefenseOffers;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public string ChooserId { get; }
        public string AttackerId { get; }
        public int AttackTotal { get; }
        public bool CanDecline { get; }
        public IReadOnlyList<NetworkActionOfferRow> LegalDefenseOffers { get; }
    }

    public sealed class NetworkDefenseChoicePreviewRow
    {
        public NetworkDefenseChoicePreviewRow(
            string cardInstanceId,
            string choiceLabel,
            IReadOnlyList<NetworkValueContributionRow> defenseContributions,
            bool hasDefenseTotal,
            int defenseTotal,
            int blockResult,
            int outcome,
            bool suppressesMinionContributions)
        {
            CardInstanceId = cardInstanceId;
            ChoiceLabel = choiceLabel;
            DefenseContributions = defenseContributions;
            HasDefenseTotal = hasDefenseTotal;
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            Outcome = outcome;
            SuppressesMinionContributions = suppressesMinionContributions;
        }

        /// <summary>Null for the decline choice.</summary>
        public string CardInstanceId { get; }

        public string ChoiceLabel { get; }
        public IReadOnlyList<NetworkValueContributionRow> DefenseContributions { get; }
        public bool HasDefenseTotal { get; }
        public int DefenseTotal { get; }
        public int BlockResult { get; }
        public int Outcome { get; }
        public bool SuppressesMinionContributions { get; }
    }

    public sealed class NetworkDefensePreviewRow
    {
        public NetworkDefensePreviewRow(
            string resolutionId,
            string choiceId,
            int attackTotal,
            IReadOnlyList<NetworkValueContributionRow> attackContributions,
            IReadOnlyList<NetworkDefenseChoicePreviewRow> choices,
            IReadOnlyList<NetworkValueContributionRow> minionContributions)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            AttackTotal = attackTotal;
            AttackContributions = attackContributions;
            Choices = choices;
            MinionContributions = minionContributions;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<NetworkValueContributionRow> AttackContributions { get; }
        public IReadOnlyList<NetworkDefenseChoicePreviewRow> Choices { get; }
        public IReadOnlyList<NetworkValueContributionRow> MinionContributions { get; }
    }

    /// <summary>
    /// The effect resolution envelope's identity surface. Its continuation, outcomes and accepted
    /// choices are rules-execution state and never client fields; the pending choice itself
    /// travels as <see cref="NetworkProjectionDetailRecord.PendingEffectChoice"/>.
    /// </summary>
    public sealed class NetworkSkillResolutionRow
    {
        public NetworkSkillResolutionRow(
            string resolutionId,
            string sourceCardInstanceId,
            string sourceDefinitionId,
            string sourceContentRevision,
            string actionDefinitionId,
            string effectDefinitionId,
            string effectContentRevision,
            string actorId,
            string controllerId,
            int invocationContext,
            bool hasPendingChoice)
        {
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionId = sourceDefinitionId;
            SourceContentRevision = sourceContentRevision;
            ActionDefinitionId = actionDefinitionId;
            EffectDefinitionId = effectDefinitionId;
            EffectContentRevision = effectContentRevision;
            ActorId = actorId;
            ControllerId = controllerId;
            InvocationContext = invocationContext;
            HasPendingChoice = hasPendingChoice;
        }

        public string ResolutionId { get; }
        public string SourceCardInstanceId { get; }
        public string SourceDefinitionId { get; }
        public string SourceContentRevision { get; }
        public string ActionDefinitionId { get; }
        public string EffectDefinitionId { get; }
        public string EffectContentRevision { get; }
        public string ActorId { get; }
        public string ControllerId { get; }
        public int InvocationContext { get; }
        public bool HasPendingChoice { get; }
    }

    public sealed class NetworkEffectCandidateRow
    {
        public NetworkEffectCandidateRow(int kind, string optionNodeId, NetworkEffectValueRow value)
        {
            Kind = kind;
            OptionNodeId = optionNodeId;
            Value = value;
        }

        public int Kind { get; }

        /// <summary>Null for a value candidate.</summary>
        public string OptionNodeId { get; }

        /// <summary>Null for an option candidate.</summary>
        public NetworkEffectValueRow Value { get; }
    }

    public sealed class NetworkEffectChoiceRow
    {
        public NetworkEffectChoiceRow(
            string resolutionId,
            string choiceId,
            string nodeId,
            string chooserId,
            int kind,
            int minimumSelections,
            int maximumSelections,
            IReadOnlyList<NetworkEffectCandidateRow> candidates,
            IReadOnlyList<NetworkEffectCandidateRow> selectedCandidates,
            IReadOnlyList<NetworkEffectCandidateRow> unavailableCandidates,
            bool hasConfirmInvocation,
            bool hasDeclineInvocation,
            bool hasStopInvocation,
            int valueVocabulary)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            NodeId = nodeId;
            ChooserId = chooserId;
            Kind = kind;
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
            Candidates = candidates;
            SelectedCandidates = selectedCandidates;
            UnavailableCandidates = unavailableCandidates;
            HasConfirmInvocation = hasConfirmInvocation;
            HasDeclineInvocation = hasDeclineInvocation;
            HasStopInvocation = hasStopInvocation;
            ValueVocabulary = valueVocabulary;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public string NodeId { get; }
        public string ChooserId { get; }
        public int Kind { get; }
        public int MinimumSelections { get; }
        public int MaximumSelections { get; }
        public IReadOnlyList<NetworkEffectCandidateRow> Candidates { get; }
        public IReadOnlyList<NetworkEffectCandidateRow> SelectedCandidates { get; }
        public IReadOnlyList<NetworkEffectCandidateRow> UnavailableCandidates { get; }
        public bool HasConfirmInvocation { get; }
        public bool HasDeclineInvocation { get; }
        public bool HasStopInvocation { get; }

        /// <summary>
        /// Milestone 8.37. What this choice's integer candidates mean, as
        /// <see cref="Core.Rules.EffectResolution.EffectChoiceValueVocabulary"/>. It crosses the wire
        /// because a JOINED seat draws the controls, and a colour named as "3" is not a control a
        /// player can answer. The snapshot does not carry it - restore re-asks the handler - so this
        /// moves the wire version and leaves the schema alone.
        /// </summary>
        public int ValueVocabulary { get; }
    }

    public sealed class NetworkEffectChoicePreviewRow
    {
        public NetworkEffectChoicePreviewRow(
            NetworkEffectValueRow candidateValue,
            int q,
            int r,
            int occupantKind,
            string occupantHeroId,
            string occupantMinionId,
            string occupantTokenInstanceId,
            NetworkCoordinateRow? resultingCoordinate,
            int consequenceIntent)
        {
            CandidateValue = candidateValue;
            Q = q;
            R = r;
            OccupantKind = occupantKind;
            OccupantHeroId = occupantHeroId;
            OccupantMinionId = occupantMinionId;
            OccupantTokenInstanceId = occupantTokenInstanceId;
            ResultingCoordinate = resultingCoordinate;
            ConsequenceIntent = consequenceIntent;
        }

        public NetworkEffectValueRow CandidateValue { get; }
        public int Q { get; }
        public int R { get; }
        public int OccupantKind { get; }
        public string OccupantHeroId { get; }
        public string OccupantMinionId { get; }
        public string OccupantTokenInstanceId { get; }
        public NetworkCoordinateRow? ResultingCoordinate { get; }
        public int ConsequenceIntent { get; }
    }

    public sealed class NetworkCorrectionSubjectRow
    {
        public NetworkCorrectionSubjectRow(string minionId, int q, int r)
        {
            MinionId = minionId;
            Q = q;
            R = r;
        }

        public string MinionId { get; }
        public int Q { get; }
        public int R { get; }
    }

    public sealed class NetworkCorrectionRow
    {
        public NetworkCorrectionRow(
            string resolutionId,
            string sourceCardInstanceId,
            string actionDefinitionId,
            string actorId,
            int stage,
            string choosingTeamId,
            int choosingSide,
            string battleZoneId,
            string choiceId,
            string subjectMinionId,
            int kind,
            IReadOnlyList<NetworkCorrectionSubjectRow> subjectCandidates,
            IReadOnlyList<NetworkMovementDestinationRow> destinationCandidates)
        {
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            Stage = stage;
            ChoosingTeamId = choosingTeamId;
            ChoosingSide = choosingSide;
            BattleZoneId = battleZoneId;
            ChoiceId = choiceId;
            SubjectMinionId = subjectMinionId;
            Kind = kind;
            SubjectCandidates = subjectCandidates;
            DestinationCandidates = destinationCandidates;
        }

        public string ResolutionId { get; }
        public string SourceCardInstanceId { get; }
        public string ActionDefinitionId { get; }
        public string ActorId { get; }
        public int Stage { get; }
        public string ChoosingTeamId { get; }
        public int ChoosingSide { get; }

        /// <summary>Null where the correction stands outside a Battle Zone.</summary>
        public string BattleZoneId { get; }

        public string ChoiceId { get; }

        /// <summary>Null during the subject stage.</summary>
        public string SubjectMinionId { get; }

        public int Kind { get; }
        public IReadOnlyList<NetworkCorrectionSubjectRow> SubjectCandidates { get; }
        public IReadOnlyList<NetworkMovementDestinationRow> DestinationCandidates { get; }
    }

    public sealed class NetworkMinionBattleCandidateRow
    {
        public NetworkMinionBattleCandidateRow(NetworkUnitRow unit, int minionType)
        {
            Unit = unit;
            MinionType = minionType;
        }

        public NetworkUnitRow Unit { get; }
        public int MinionType { get; }
    }

    public sealed class NetworkMinionBattleChoiceRow
    {
        public NetworkMinionBattleChoiceRow(
            string resolutionId,
            string choiceId,
            string losingTeamId,
            int choosingSide,
            string battleZoneId,
            int requiredRemovalCount,
            IReadOnlyList<NetworkMinionBattleCandidateRow> candidates,
            IReadOnlyList<NetworkUnitRow> selectedUnits,
            bool canConfirm)
        {
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            LosingTeamId = losingTeamId;
            ChoosingSide = choosingSide;
            BattleZoneId = battleZoneId;
            RequiredRemovalCount = requiredRemovalCount;
            Candidates = candidates;
            SelectedUnits = selectedUnits;
            CanConfirm = canConfirm;
        }

        public string ResolutionId { get; }
        public string ChoiceId { get; }
        public string LosingTeamId { get; }
        public int ChoosingSide { get; }
        public string BattleZoneId { get; }
        public int RequiredRemovalCount { get; }
        public IReadOnlyList<NetworkMinionBattleCandidateRow> Candidates { get; }
        public IReadOnlyList<NetworkUnitRow> SelectedUnits { get; }
        public bool CanConfirm { get; }
    }

    public sealed class NetworkRespawnDestinationRow
    {
        public NetworkRespawnDestinationRow(int q, int r, string adjacentSpawnPointId, bool isFallback)
        {
            Q = q;
            R = r;
            AdjacentSpawnPointId = adjacentSpawnPointId;
            IsFallback = isFallback;
        }

        public int Q { get; }
        public int R { get; }
        public string AdjacentSpawnPointId { get; }
        public bool IsFallback { get; }
    }

    public sealed class NetworkHeroRespawnChoiceRow
    {
        public NetworkHeroRespawnChoiceRow(
            string heroId,
            string cardInstanceId,
            IReadOnlyList<NetworkRespawnDestinationRow> destinations)
        {
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            Destinations = destinations;
        }

        public string HeroId { get; }
        public string CardInstanceId { get; }
        public IReadOnlyList<NetworkRespawnDestinationRow> Destinations { get; }
    }

    public sealed class NetworkSystemTransitionStepRow
    {
        public NetworkSystemTransitionStepRow(
            string instanceId,
            string definitionId,
            string stepId,
            string itemId,
            int choosingSide,
            IReadOnlyList<string> options,
            IReadOnlyList<string> completedStepIds,
            IReadOnlyList<string> completedItemIds)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            StepId = stepId;
            ItemId = itemId;
            ChoosingSide = choosingSide;
            Options = options;
            CompletedStepIds = completedStepIds;
            CompletedItemIds = completedItemIds;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string StepId { get; }

        /// <summary>Null when the step pauses once for itself.</summary>
        public string ItemId { get; }

        public int ChoosingSide { get; }
        public IReadOnlyList<string> Options { get; }
        public IReadOnlyList<string> CompletedStepIds { get; }
        public IReadOnlyList<string> CompletedItemIds { get; }
    }

    public sealed class NetworkSystemTransitionOptionRow
    {
        public NetworkSystemTransitionOptionRow(
            string optionId,
            int kind,
            string upgradedCardInstanceId,
            string upgradedDefinitionId,
            string upgradedContentRevision,
            string takenCardInstanceId,
            string takenDefinitionId,
            string takenContentRevision,
            string setAsideCardInstanceId,
            string setAsideDefinitionId,
            string setAsideContentRevision,
            int itemIcon,
            int itemSlotIndex,
            int destinationQ,
            int destinationR,
            string destinationSpaceId)
        {
            OptionId = optionId;
            Kind = kind;
            UpgradedCardInstanceId = upgradedCardInstanceId;
            UpgradedDefinitionId = upgradedDefinitionId;
            UpgradedContentRevision = upgradedContentRevision;
            TakenCardInstanceId = takenCardInstanceId;
            TakenDefinitionId = takenDefinitionId;
            TakenContentRevision = takenContentRevision;
            SetAsideCardInstanceId = setAsideCardInstanceId;
            SetAsideDefinitionId = setAsideDefinitionId;
            SetAsideContentRevision = setAsideContentRevision;
            ItemIcon = itemIcon;
            ItemSlotIndex = itemSlotIndex;
            DestinationQ = destinationQ;
            DestinationR = destinationR;
            DestinationSpaceId = destinationSpaceId;
        }

        public string OptionId { get; }
        public int Kind { get; }
        public string UpgradedCardInstanceId { get; }
        public string UpgradedDefinitionId { get; }
        public string UpgradedContentRevision { get; }
        public string TakenCardInstanceId { get; }
        public string TakenDefinitionId { get; }
        public string TakenContentRevision { get; }
        public string SetAsideCardInstanceId { get; }
        public string SetAsideDefinitionId { get; }
        public string SetAsideContentRevision { get; }
        public int ItemIcon { get; }

        /// <summary>The dashboard slot index from 1, and 0 where no slot accepts the icon.</summary>
        public int ItemSlotIndex { get; }

        public int DestinationQ { get; }
        public int DestinationR { get; }
        public string DestinationSpaceId { get; }
    }

    public sealed class NetworkSystemTransitionDescriptionRow
    {
        public NetworkSystemTransitionDescriptionRow(
            int kind,
            string heroId,
            int level,
            int paidCoins,
            int remainingCoins,
            IReadOnlyList<NetworkSystemTransitionOptionRow> options)
        {
            Kind = kind;
            HeroId = heroId;
            Level = level;
            PaidCoins = paidCoins;
            RemainingCoins = remainingCoins;
            Options = options;
        }

        public int Kind { get; }

        /// <summary>Null for a pause that belongs to a team rather than a hero.</summary>
        public string HeroId { get; }

        public int Level { get; }
        public int PaidCoins { get; }
        public int RemainingCoins { get; }
        public IReadOnlyList<NetworkSystemTransitionOptionRow> Options { get; }
    }

    public sealed class NetworkRemovalPaymentRow
    {
        public NetworkRemovalPaymentRow(
            string sourceId,
            int costKind,
            int costFromLocation,
            IReadOnlyList<int> costAdmittedColors,
            int costAdmittedClassification,
            string paymentCardInstanceId)
        {
            SourceId = sourceId;
            CostKind = costKind;
            CostFromLocation = costFromLocation;
            CostAdmittedColors = costAdmittedColors;
            CostAdmittedClassification = costAdmittedClassification;
            PaymentCardInstanceId = paymentCardInstanceId;
        }

        public string SourceId { get; }
        public int CostKind { get; }
        public int CostFromLocation { get; }
        public IReadOnlyList<int> CostAdmittedColors { get; }
        public int CostAdmittedClassification { get; }
        public string PaymentCardInstanceId { get; }
    }

    public sealed class NetworkRemovalPreventionRow
    {
        public NetworkRemovalPreventionRow(
            string requestId,
            string decidingHeroId,
            string subjectMinionId,
            int origin,
            IReadOnlyList<NetworkRemovalPaymentRow> payments)
        {
            RequestId = requestId;
            DecidingHeroId = decidingHeroId;
            SubjectMinionId = subjectMinionId;
            Origin = origin;
            Payments = payments;
        }

        public string RequestId { get; }
        public string DecidingHeroId { get; }
        public string SubjectMinionId { get; }
        public int Origin { get; }
        public IReadOnlyList<NetworkRemovalPaymentRow> Payments { get; }
    }

    /// <summary>
    /// Milestone 8.35g. A turn whose card resolution has finished and which is waiting for its own
    /// player to hand it on.
    /// </summary>
    /// <remarks>
    /// It is carried rather than derived from the standing decision owner, because the read model
    /// exposes the pause itself and the wire-growth rule's floor is deliberate: an excused member is
    /// a fact a client cannot see, and this one is read by the shared status derivation that every
    /// seat's top bar is drawn from.
    /// </remarks>
    public sealed class NetworkTurnReleaseRow
    {
        public NetworkTurnReleaseRow(string actorId, string cardInstanceId, int teamSide)
        {
            ActorId = actorId;
            CardInstanceId = cardInstanceId;
            TeamSide = teamSide;
        }

        public string ActorId { get; }
        public string CardInstanceId { get; }
        public int TeamSide { get; }
    }

    /// <summary>
    /// Milestone 8.35r. The standing rewind on the wire: whose it is, and how many accepted commands
    /// it has taken back so far.
    /// </summary>
    public sealed class NetworkUndoMarkerRow
    {
        public NetworkUndoMarkerRow(string heroId, int count)
        {
            HeroId = heroId;
            Count = count;
        }

        public string HeroId { get; }
        public int Count { get; }
    }

    public sealed class NetworkFastTravelOptionRow
    {
        public NetworkFastTravelOptionRow(
            NetworkActionOfferRow replacementOffer,
            string replacedMovementActionDefinitionId,
            IReadOnlyList<NetworkCoordinateRow> legalDestinations)
        {
            ReplacementOffer = replacementOffer;
            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinations = legalDestinations;
        }

        public NetworkActionOfferRow ReplacementOffer { get; }
        public string ReplacedMovementActionDefinitionId { get; }
        public IReadOnlyList<NetworkCoordinateRow> LegalDestinations { get; }
    }

    public sealed class NetworkClearOptionRow
    {
        public NetworkClearOptionRow(
            NetworkActionOfferRow replacementOffer,
            string replacedAttackActionDefinitionId,
            IReadOnlyList<string> adjacentTokenInstanceIds,
            IReadOnlyList<GuardsDigital.Core.Replay.DirectorCommand> subsetInvocations)
        {
            ReplacementOffer = replacementOffer;
            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId;
            AdjacentTokenInstanceIds = adjacentTokenInstanceIds;
            SubsetInvocations = subsetInvocations;
        }

        public NetworkActionOfferRow ReplacementOffer { get; }
        public string ReplacedAttackActionDefinitionId { get; }

        /// <summary>The tokens by instance identity; their piece rows already travel in the record.</summary>
        public IReadOnlyList<string> AdjacentTokenInstanceIds { get; }

        /// <summary>
        /// One typed <c>PerformClear</c> per confirmable subset, exactly as the projection
        /// enumerates them - a payload a client composes is a payload it can compose wrongly.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Replay.DirectorCommand> SubsetInvocations { get; }
    }
}
