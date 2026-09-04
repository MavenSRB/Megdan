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

        /// <summary>
        /// Whether controls derived from this snapshot may currently submit. Local projections are
        /// always enabled; a network projection disables input while its command is awaiting the
        /// host, while synchronization is unavailable, or after the session has ended.
        /// </summary>
        bool IsInputEnabled { get; }

        /// <summary>
        /// Milestone 8.35f. Whether the seat this view belongs to may step its own turn back by one
        /// accepted command right now.
        /// </summary>
        /// <remarks>
        /// Published by the host rather than derived by a surface, because the bounds it answers -
        /// the turn floor, the attack wall and the other-seat wall - are facts about the recorded
        /// history that no client holds. It is false on the Director's own full-trust view: the
        /// Director's Undo is its drawer's, unbounded and not seat-scoped, and this flag is only
        /// ever about the seat control.
        /// </remarks>
        bool IsSeatUndoAvailable { get; }

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
        /// The host-published public agreement standing over tied Initiative or Minion Battle, or
        /// <c>null</c> in Director mode and while no such player-seat agreement is active.
        /// </summary>
        TeamAgreementProjection TeamAgreement { get; }

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

        /// <summary>
        /// Milestone 8.35r. Who the authority is waiting on, or
        /// <see cref="PendingDecisionOwner.None"/> - the public half of the standing decision, whose
        /// private half is <see cref="DecisionContract"/>.
        /// </summary>
        /// <remarks>
        /// <para><b>Table-public by design.</b> "We are waiting for Blue" is something every player
        /// at a physical table can see, and every seat has received it on the wire since milestone
        /// 8.32. What it never carries is what the awaited player MAY CHOOSE: the options are the
        /// privacy boundary and stay on <see cref="DecisionContract"/>, which only the owner reads.
        /// Who is awaited is public; what they may do is not.</para>
        /// <para><b>Why it had to join this contract.</b> It was already on
        /// <c>SeatProjection</c> and already on the wire, but not here - so no surface written
        /// against this interface could reach it. That gap is exactly what left four of the SEVENTH
        /// amendment's tray arms underivable: the levelling hero, the defence answerer and the
        /// respawn chooser each live on a pending member a non-owning seat reads as <c>null</c>, and
        /// the acting-hero arm needs to know THAT a decision stands on top of the actor's turn -
        /// <c>SelectedActor</c> alone keeps naming the attacker while the DEFENDER is being asked,
        /// which is the bug one level up. All four become derivable from this member plus the ones
        /// already here.</para>
        /// </remarks>
        PendingDecisionOwner DecisionOwner { get; }

        /// <summary>
        /// Milestone 8.35r. The seat-public history of what has happened, newest last, bounded to
        /// the most recent <see cref="GuardsDigital.Core.Diagnostics.MatchLogProjection.RetainedEntries"/>
        /// lines.
        /// </summary>
        /// <remarks>
        /// <para><b>Derived, never stored.</b> Rebuilt from the retained rule-event log on every
        /// reconstruction, so an undo, a replay, a resync and a snapshot restore each redraw it for
        /// free and none of it appears in a persisted payload.</para>
        /// <para><b>The same log for everyone.</b> Its selection admits only events the whole table
        /// can already see, so a seat's log is byte-identical to every other seat's and to the
        /// Director's. That is the privacy property stated positively: there is no per-seat
        /// redaction here because there is nothing to redact.</para>
        /// </remarks>
        IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog { get; }

        /// <summary>
        /// Milestone 8.35r. The rewind currently standing, or <see cref="MatchUndoMarker.None"/>.
        /// </summary>
        /// <remarks>
        /// It is published rather than derived for the reason <see cref="MatchUndoMarker"/> gives at
        /// length: an undo records no command and no rule event, so nothing in the log can say one
        /// happened. It is <see cref="MatchUndoMarker.None"/> on the Director's own full-trust view,
        /// exactly as <see cref="IsSeatUndoAvailable"/> is false there.
        /// </remarks>
        MatchUndoMarker UndoMarker { get; }

        // The pending surface. Every member is scoped to the reader that may answer it.
        PendingInitiativeChoice PendingInitiativeChoice { get; }
        PendingCardActionResolution PendingMovementResolution { get; }
        bool HasPendingMovementResolution { get; }
        PendingMovementChoice PendingMovementChoice { get; }
        PendingAttackResolution PendingAttackResolution { get; }
        bool HasPendingAttackResolution { get; }
        PendingAttackTargetChoice PendingAttackTargetChoice { get; }
        IReadOnlyList<AttackTargetDefensePreviewProjection> PendingAttackTargetDefensePreviews { get; }
        PendingDefenseResponseChoice PendingDefenseChoice { get; }
        PendingDefensePreviewProjection PendingDefensePreview { get; }
        PendingResolution PendingSkillResolution { get; }
        bool HasPendingSkillResolution { get; }
        PendingEffectChoice PendingEffectChoice { get; }
        IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview { get; }
        GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection { get; }
        PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }
        GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            PendingSystemTransitionStep { get; }
        RemovalPreventionPrompt PendingRemovalPrevention { get; }

        /// <summary>
        /// Milestone 8.35g. The finished turn waiting for its own player to hand it on, when one
        /// stands - the eleventh pending family, and the only one every ordinary turn reaches.
        /// </summary>
        /// <remarks>
        /// It is on the read model because the top status bar must be able to report every declared
        /// family: without it a turn waiting on its own player reached the table as the generic
        /// "waiting for the match to continue" fallback, which is the same defect milestone 8.30c
        /// removed for an ended match and 8.20d for the effect choice.
        /// </remarks>
        GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease { get; }

        GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            PendingSystemTransitionDescription { get; }

        // The offers. Scoped to the reader that may take them.
        IReadOnlyList<ActionOffer> AvailableTurnActions { get; }
        IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions { get; }
        IReadOnlyList<ClearOptionProjection> AvailableClearOptions { get; }
        ActionOffer? AvailableHoldOffer { get; }
    }
}
