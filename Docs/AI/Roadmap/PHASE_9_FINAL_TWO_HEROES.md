# Phase 9 - Final two heroes: Emmitt and NebKher

Status: [session brief](../SESSION_BRIEF.md). Rules: [Emmitt](../Rules/HERO_EMMITT.md) and [NebKher](../Rules/HERO_NEBKHER.md).

Authorized 2026-10-04 by the user: "go for last two heroes"; clarified as the last two **unimplemented** heroes, with a request to read all cards and raise rule questions before the user sleeps. The bounded outcome is to make Emmitt the Traveller and NebKher the Harbinger fully composed, selectable, artworked, and covered by the permanent player-path gate, one hero at a time. The sequence below is approved as one sequence; each ID has its own validation and local commit. A question recorded in the rules shards blocks only the behavior that depends on its answer. No package, publication, balance change, or third hero is included.

The source is the curated second-printing Tabletopia extraction, nineteen fronts per hero. Each has a basic gold, basic silver, five red, five blue, five green, one Ultimate, and one p.20 Handicap that p.6 boxes at setup. The rulebook and the approved wording readings govern shared terms; the exact card front governs its own text.

### [x] 9.45 Inventory the final two heroes and map their contracts

Outcome: read all thirty-eight fronts, the two selection profiles and dashboards, verify source identities and deck shapes, transcribe printed values and effects into the two hero shards, check rulebook intersections, record each honest ambiguity as an answerable question, and map the implementation seams below. No hero content enters the executable catalog. Closing check: nineteen distinct fronts per hero, the Handicap identified and excluded from each eighteen-card deck, every face represented in the rule shards, and the living documentation validator plus `git diff --check` green. T0.

Completed 2026-10-04. Both nineteen-front inventories, the profiles, dashboards, back variants, exact clauses, values, item glyphs, upgrade lines and boxed Handicaps are recorded in the hero shards. The user viewed the relevant card fronts and settled EM-Q1-EM-Q2 and NB-Q1-NB-Q4, and confirmed three tokens in each supply. The contract map names the shared authorities and new seams. No executable content or private art was imported. The documentation gate is green.

Historical evidence: [9.45 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-945-final-two-hero-inventory-evidence).

### [x] 9.46 Emmitt's turn and position history

Outcome: authoritative turn-start position and prior-turn immobility queries, plus current-turn resolved-card predicates, available to card effects and preserved through snapshot, replay and Undo. Tests distinguish moves away and back, placement, swap, removal/respawn, and a card resolved earlier this turn. T2.

Completed 2026-10-04. The board captures both turn-start positions and continuous unit movement revisions; the round records heroes whose cards resolved this turn. Typed effect-facing interfaces, snapshot v55, restore and replay preserve those facts. Focused tests cover move-away-and-back, swap, removal/respawn, absent-at-start placement, and a resolved card before the next reveal.

Historical evidence: [9.46 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-946-emmitt-turn-history-evidence).

### [x] 9.47 Emmitt's Glitch and altered Initiative

Outcome: typed Glitch token placement, short supply, swap and turn-end cleanup; Reverse Time changes next-turn Initiative ordering while retaining the printed ordinary tie method; Temporal Punch's Defense substitution reads the defending card's Initiative and items. Focused tests cover token ownership, swap, tie order, effective Defense, persistence and replay. T2.

Completed 2026-10-04. EM-Q3 fixes Glitch spacing at hex distance 3. The three-copy definition, placement/reclaim handlers, live spacing revalidation and turn-end removal are typed; a board swap preserves token identity and ownership. Reverse Time's dormant/active next-turn source selects lower effective Initiative with p.7 ties unchanged. Temporal Punch substitutes the defending card's Initiative icon and matching items, consistently in resolution, replay verification and preview. Scoped snapshot/restore and deterministic continuation cover the new token and effect state. Emmitt's definitions and handlers remain unregistered until 9.49's full card composition. T2 focused and directly affected offline checks are green; no Unity run is needed for this Core-only milestone.

Historical evidence: [9.47 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-947-emmitt-glitch-and-altered-initiative-evidence).

### [x] 9.48 Emmitt's two-card Ultimate

Outcome: one owning seat may commit two cards on its turn, reveals both, then retrieves one unresolved card at the printed reveal checkpoint; the other card alone takes an ordinary action. Commitment privacy, selection, Initiative, card states, owner authorization, bot choices, restore and wire projection are proved. T2. Requires the Emmitt Ultimate ruling.

User-directed corrective scope (2026-10-04): repair `RISK-PLAYMODE-001` within 9.48 before continuing the approved sequence; no quarantine is approved. Reproduce and close the 56 existing-hero UI journeys, the journey-matrix failure, and the local online-match failure with failing-then-passing checks. Closing gate: those named cases green, unfiltered Play Mode green, directly affected Edit Mode and offline regressions green, documentation validation and `git diff --check` green, then review and locally commit 9.48. Do not start 9.49 before that commit.

Completed 2026-10-04. The two-card response is atomic and owner-scoped; both faces reveal together, the owning hero retrieves either unresolved face at the reveal checkpoint, and the remaining face alone enters Initiative. The typed rule is not registered to Emmitt until 9.49. Bot, UI, network seat, replay and snapshot v56/wire `/36` carry the same command and privacy contract. The corrective Play Mode work closed the 58-case red gate with real-window waits, typed control clicks, cinematic release/Undo handling, the Mortimer matrix row, and a full 1096/1096 unfiltered Play Mode pass; the local TCP match passed in the full gate. The T2 offline and Edit Mode checks remained green at unchanged production source revision. The completion evidence is in the local milestone commit.

Historical evidence: [9.48 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-948-emmitt-two-card-ultimate-and-playmode-repair-evidence).

### [x] 9.49 Compose and deal Emmitt

Outcome: all eighteen playable fronts composed at their exact printed values and clauses; Tear in Time boxed; roster selection, standee/profile and card art, decision labels, bots, wording and offer/outcome checks. The configured match and network seat use the same authority. T3.

Completed 2026-10-05. All eighteen playable fronts are composed, registered and dealt; Tear in Time is boxed. Emmitt's verified profile, three-Glitch supply and typed effects work in configured matches. Private card, selection, standee and shine art is prepared locally, not published. Time Warp's target-owner exchange, Time Capsule's v57 discard history, Back to the Future's body-preserving historical Place and Future Proof's printed choice have direct outcome tests. Snapshot compatibility explicitly rejects v56. The T3 gates passed: focused Emmitt Edit 28/28, offline 6,670/6,670, unfiltered Unity Edit 7,770 passed plus 37 explicit diagnostic skips, unfiltered Unity Play 1,097/1,097, and Windows player-build smoke. A real picker journey verified all five printed roles and the selection face; the permanent per-card journey matrix is 9.50. Observed UI regressions were corrected and green-rerun, not quarantined. The local milestone commit holds exact evidence.

Historical evidence: [9.49 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-949-emmitt-dealing-evidence).

### [x] 9.50 Gate Emmitt on the player path

Outcome: permanent face-to-journey matrix with real controls and asserted outcomes, plus the highest applicable roster validation tier. T3.

Completed 2026-10-05. The permanent matrix covers all nineteen fronts (eighteen playable, one boxed), their printed action shapes and Emmitt's piece, items, footprint, progression, lifecycle, restore, Undo, replay and restart. Real-control journeys assert board, HUD and authoritative outcomes. Unstable Timeline exposed a shared p.14 step-5 Defense-total timing defect after step-4 text; it is corrected and green-rerun. Focused Emmitt 30/30, offline 6,670/6,670, unfiltered Unity Edit 7,770 passed with 37 explicit diagnostic skips, unfiltered Unity Play 1,127/1,127, boundary guard and non-batch visual inspection passed. The local milestone commit holds exact evidence.

Historical evidence: [9.50 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-950-emmitt-player-path-evidence).

### [x] 9.51 NebKher's Illusions and borrowed actions

Outcome: typed Illusion token placement, swap, short supply and cleanup; Mind Grip performs an allowed action on the chosen enemy's previous-slot card with token substitution and no markers; the special friendly-melee-minion count applies at its printed action scope. Focused rules, snapshot, replay, owner-seat and token tests. T2. Requires the Mind Grip and Illusion rulings.

Completed 2026-10-05. The three-copy Illusion definition and typed handlers place, reclaim, swap and clean up tokens; a controller-action-only overlay makes each count as a friendly melee minion for coordinate-backed targets, Swap/Place and Defense contributions, never the round-end Minion Battle. Mind Grip selects an enemy's previous-slot printed action and performs it as NebKher, including secondary Movement, Defense text and Attack, without moving the borrowed card, attaching its markers, or placing its original tokens. Its parent frame supplies the substitution and marker policy after restore; seat, replay and every command-prefix snapshot are tested. The closed borrowed-secondary invocation advances snapshot schema v57 to v58 with prior-version rejection and measured compatibility re-pins; wire /36 remains guarded by the schema admission tuple. The demonstrated shared Swap modification-seam failure was corrected and green-rerun; the schema-only aggregate digest failure was measured, re-pinned and green-rerun. Snapshot migration promoted acceptance from T2 to T3: full offline 6,686/6,686, unfiltered Unity Edit 7,786 passed plus 37 explicit diagnostics, and unfiltered Unity Play 1,127/1,127. The local milestone commit holds exact evidence. NebKher's definitions remain unregistered until 9.53.

Historical evidence: [9.51 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-951-nebkher-illusion-and-borrow-evidence).

### [~] 9.52 NebKher's laughter and divided board

Outcome: Diabolical Laughter's optional repeated choices and the Ultimate's trigger; a selected straight line through NebKher's space enforces the two printed cross-side interaction restrictions for Crack in Reality and Shift Reality, across every target, space, movement, placement and area query. Board geometry, timing, snapshot, replay and wire checks. T2. Requires the laughter and line rulings.

### [ ] 9.53 Compose and deal NebKher

Outcome: all eighteen playable fronts composed at their exact printed values and clauses; Mind Probe boxed; roster selection, standee/profile and card art, decision labels, bots, wording and offer/outcome checks. T3.

### [ ] 9.54 Gate NebKher and the complete roster on the player path

Outcome: permanent face-to-journey matrix with real controls and asserted outcomes, plus the highest applicable full-roster validation tier. The resulting configured roster is thirty-two heroes. T3.
