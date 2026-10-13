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
    /// Optional public read-model detail for formats whose Wave counters are allocated per lane.
    /// The scalar on <see cref="IMatchReadModel"/> remains the match-ending summary; this exposes
    /// each authoritative pool so presentation never invents or locally tracks lane state.
    /// </summary>
    public interface ILaneWaveCounterReadModel
    {
        IReadOnlyList<int> RemainingWaveCountersByLane { get; }
    }

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

        /// <summary>
        /// Milestone 8.35w. The most recent completed rounds, bounded to
        /// <see cref="MatchSessionProjection.RetainedCompletedRounds"/> - recent history to review,
        /// not the whole match. Every implementation reports the same window; the full history stays
        /// with the round authority and its snapshot.
        /// </summary>
        IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        EndOfRoundStage? CurrentEndOfRoundStage { get; }
        IReadOnlyList<MatchCommitmentProjection> Commitments { get; }
        MatchCommitmentProjection GetCommitment(HeroId heroId);
        SelectedInitiativeActor SelectedActor { get; }
        bool IsTurnActive { get; }
        IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; }

        /// <summary>
        /// Milestone 8.35z60. Rulebook p.8's ONE SHARED Wave-counter pool, as it now stands -
        /// how many pushes remain before the last one ends the game - rather than
        /// <see cref="Setup"/>'s starting total, which never moves. The top status strip binds this
        /// member directly, by the same 2026-09-08 user directive: "Waves up top should show waves
        /// remaining until the end of the game."
        /// </summary>
        /// <remarks>
        /// <para><b>In process, this costs no wire bytes at all.</b>
        /// <see cref="MatchSessionProjection"/> and the <see cref="SeatProjection"/> it scopes both
        /// read the live pool straight off <c>LanePushTransitionAuthority.RemainingWaveCounters</c>
        /// in the same process - the authority already computed the number, and only the read-model
        /// contract was missing it, exactly as <see cref="DecisionOwner"/> was before it.</para>
        ///
        /// <para><b>A networked seat reads the host's number, not its own.</b> This milestone's
        /// worktree was held to a "zero wire" constraint and left a recorded bound here: a remote
        /// seat would report its locally composed match's frozen starting total once any push
        /// happened at the table. The wave's integration pass paid that debt rather than shipping
        /// it, because every real playtest of this project is networked and a host-only readout
        /// would have made this milestone correct for exactly one seat. Protocol version 11 carries
        /// the pool as a plain <c>Int32</c> on <c>NetworkSeatProjectionRecord</c>, beside the
        /// per-team Life counters it is the table-wide counterpart of, so
        /// <c>NetworkMatchClient</c> now decodes it like any other frame fact and falls back to the
        /// composed total only before the first frame arrives.</para>
        /// </remarks>
        int RemainingWaveCounters { get; }

        /// <summary>
        /// Milestone 9.30g. Every team's count toward a printed team victory - Gydion's Wish, "After
        /// you cast the "Wish" spell three times your team wins the game" - ordered by team id, each
        /// with its printed threshold. Public (GY-Q10), the same list for the Director and every seat,
        /// and carried on the wire from protocol /32; empty until a team first counts.
        /// </summary>
        IReadOnlyList<GuardsDigital.Core.Rules.Victory.TeamVictoryTally> TeamVictoryTallies { get; }

        IReadOnlyList<HeroProgressionProjection> HeroProgression { get; }
        IReadOnlyList<HeroDefenseModifierProjection> HeroDefenseModifiers { get; }

        /// <summary>
        /// Milestone 8.53. The match scoreboard the victory screen prints: one line per participating
        /// hero, ordered by hero id, carrying kills, deaths, attacks at heroes, times defended,
        /// minions killed and coins earned.
        /// </summary>
        /// <remarks>
        /// <para><b>Public information, and not per-seat.</b> Every column counts an event the whole
        /// table watched happen - a Defeat, a declared attack, an answered defense, a minion removed,
        /// a coin paid out - so a seat's board is the same board as the Director's and as every other
        /// seat's, exactly as <see cref="MatchLog"/> is. There is no redaction here because there is
        /// nothing to redact.</para>
        /// <para><b>Level is not on it</b>, deliberately: a hero's Level is already
        /// <c>HeroProgressionProjection.Level</c> and the screen reads it there, so this carries the
        /// six columns and only those.</para>
        /// <para><b>Carried, never re-derived by a reader.</b> The totals are accumulated by the
        /// authority and ride the authoritative snapshot, so a resync, a restore and a rejoining seat
        /// all read the same numbers. A client cannot derive them: the seat-public log it holds is a
        /// bounded window of recent lines, and a total counted from a window shrinks as the match
        /// gets longer.</para>
        /// </remarks>
        IReadOnlyList<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry> Scoreboard { get; }

        GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome { get; }
        IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds { get; }
        bool IsActiveEffectSource(CardInstanceId cardInstanceId);
        IReadOnlyList<CardInstanceId> StandingActiveEffectSourceCardInstanceIds { get; }

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
        /// Milestone 9.30b. Every hero's hand, deck and spellbook counts, naming no card - the same
        /// list for the Director and every seat. It is the one way a seat counts another hero's
        /// facedown spellbook, whose cards <see cref="Cards"/> never holds; the removed spells lie
        /// faceup and arrive in <see cref="Cards"/> like any open-zone card.
        /// </summary>
        IReadOnlyList<SeatHandSummary> HandSummaries { get; }

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

        /// <summary>
        /// Milestone 8.58. The card whose printed text the running resolution is resolving - a Skill's
        /// own body, an Attack's before/after text, a Movement's - or the undefined instance when
        /// nothing is resolving.
        ///
        /// <para><b>Why it is stated here rather than read off
        /// <see cref="PendingSkillResolution"/>.</b> A NETWORK seat's read model reports that frame as
        /// <c>null</c> on purpose - milestone 8.40f's privacy arm, because the frame carries completed
        /// outcomes and accepted choices that can name another seat's cards - so every surface that
        /// asked the frame which card was speaking got nothing on a networked client and everything on
        /// the host. That is exactly what the 2026-09-19 packaged playtest showed: Swift's Reload! drew
        /// <c>MODE #1 RELOAD-BORROW-ARM</c> with no card beside it, for a card that HAS declared labels,
        /// because the label is keyed on the source card's definition and the source card could not be
        /// named.</para>
        /// <para>WHICH card is resolving is public - every seat watches it leave the hand and stand in
        /// the turn area, and the wire has carried <c>SourceCardInstanceId</c> on its own resolution row
        /// since milestone 8.32 - so this is the frame's one public field published on its own, with no
        /// wire revision, no schema move and none of the frame's private parts.</para>
        /// </summary>
        CardInstanceId ResolvingSourceCardInstanceId { get; }
        PendingEffectChoice PendingEffectChoice { get; }
        IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview { get; }
        GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection { get; }
        PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }

        /// <summary>
        /// Milestone 9.21b. "Which of you acts" (RZ-Q4), carried to the seat that owns it like every
        /// other pending family, so the status bar can name the step.
        /// </summary>
        PendingActingBodyChoice PendingActingBodyChoice { get; }

        /// <summary>
        /// Milestone 9.21i. The settled acting body of each many-bodied hero (RZ-Q4), the engine's own
        /// read (<c>AdjacentMoveAuthority.ActingBodies</c>), in hero order; empty while none is settled.
        /// The board draws its acting cue from it, so the cue follows whichever body the engine says
        /// acts - including a later "different you" repeat - without a presentation rule of its own.
        /// A public board fact, carried to every seat. The wire does not carry it yet (declared in the
        /// synchronization map, owed with 9.21j): a network client reads it empty.
        /// </summary>
        IReadOnlyList<HeroActingBody> ActingBodies { get; }
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
