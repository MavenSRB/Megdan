using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30b. Everything a match surface reads, and nothing a Director alone may.
    /// </summary>
    /// <remarks>
    /// <para>The one contract the proven HUD is written against, so the same presenters render the
    /// Director's full-trust view and one seat's authorized view without knowing which they were
    /// given. <see cref="MatchSessionProjection"/> answers every member from the whole match;
    /// <see cref="SeatProjection"/> answers the shared ones the same way and scopes the rest - a
    /// pending decision it does not own and an offer it is not the actor for are simply absent.</para>
    /// <para><b>What is deliberately not here is the point of the type.</b> Accepted-command history,
    /// snapshots, the diagnostic log and history availability are Director-only, so a surface written
    /// against this interface cannot read them by accident. The Director's own controls keep taking
    /// <see cref="MatchSessionProjection"/> directly and are simply not composed into a seat's HUD.</para>
    /// </remarks>
    public interface IMatchReadModel
    {
        long Version { get; }

        // Public match state.
        QuickGameSetupState Setup { get; }
        BoardMapDefinition SelectedMap { get; }
        MatchParticipants Participants { get; }
        IReadOnlyList<CardDefinition> Definitions { get; }
        IReadOnlyList<HeroOccupancy> Heroes { get; }
        IReadOnlyList<MinionOccupancy> Minions { get; }
        IReadOnlyList<ObstacleOccupancy> Obstacles { get; }
        IReadOnlyList<PlacedToken> PlacedTokens { get; }
        IReadOnlyList<MarkerAttachment> HeldMarkers { get; }
        IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId);
        int RoundIndex { get; }
        int TurnIndex { get; }
        RoundResolutionPhase Phase { get; }
        TieBreakerState TieBreakerState { get; }
        IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        EndOfRoundStage? CurrentEndOfRoundStage { get; }
        IReadOnlyList<MatchCommitmentProjection> Commitments { get; }
        MatchCommitmentProjection GetCommitment(HeroId heroId);
        SelectedInitiativeActor SelectedActor { get; }
        bool IsTurnActive { get; }
        IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; }
        IReadOnlyList<HeroProgressionProjection> HeroProgression { get; }
        GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome { get; }
        IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds { get; }
        bool IsActiveEffectSource(CardInstanceId cardInstanceId);

        /// <summary>
        /// The cards this reader may name. Whole for the Director; for a seat, its own cards plus the
        /// ones rulebook p.4 calls open information, with another hero's hand and deck absent.
        /// </summary>
        IReadOnlyList<CardInstance> Cards { get; }

        /// <summary>
        /// The standing decision's exact answers, or <c>null</c>. A seat that does not own the
        /// standing decision reads <c>null</c> here and finds every pending member below empty too.
        /// </summary>
        PendingDecisionContract DecisionContract { get; }

        // The pending surface. Every member is scoped to the reader that may answer it.
        PendingInitiativeChoice PendingInitiativeChoice { get; }
        PendingCardActionResolution PendingMovementResolution { get; }
        PendingMovementChoice PendingMovementChoice { get; }
        PendingAttackResolution PendingAttackResolution { get; }
        PendingAttackTargetChoice PendingAttackTargetChoice { get; }
        IReadOnlyList<AttackTargetDefensePreviewProjection> PendingAttackTargetDefensePreviews { get; }
        PendingDefenseResponseChoice PendingDefenseChoice { get; }
        PendingDefensePreviewProjection PendingDefensePreview { get; }
        PendingResolution PendingSkillResolution { get; }
        PendingEffectChoice PendingEffectChoice { get; }
        IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview { get; }
        GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection { get; }
        PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }
        GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            PendingSystemTransitionStep { get; }
        RemovalPreventionPrompt PendingRemovalPrevention { get; }

        GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            PendingSystemTransitionDescription { get; }

        // The offers. Scoped to the reader that may take them.
        IReadOnlyList<ActionOffer> AvailableTurnActions { get; }
        IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions { get; }
        IReadOnlyList<ClearOptionProjection> AvailableClearOptions { get; }
        ActionOffer? AvailableHoldOffer { get; }
    }
}
