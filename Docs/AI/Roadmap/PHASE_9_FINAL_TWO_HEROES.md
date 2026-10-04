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

### [ ] 9.47 Emmitt's Glitch and altered Initiative

Outcome: typed Glitch token placement, short supply, swap and turn-end cleanup; Reverse Time changes next-turn Initiative ordering while retaining the printed ordinary tie method; Temporal Punch's Defense substitution reads the defending card's Initiative and items. Focused tests cover token ownership, swap, tie order, effective Defense, persistence and replay. T2.

### [ ] 9.48 Emmitt's two-card Ultimate

Outcome: one owning seat may commit two cards on its turn, reveals both, then retrieves one unresolved card at the printed reveal checkpoint; the other card alone takes an ordinary action. Commitment privacy, selection, Initiative, card states, owner authorization, bot choices, restore and wire projection are proved. T2. Requires the Emmitt Ultimate ruling.

### [ ] 9.49 Compose and deal Emmitt

Outcome: all eighteen playable fronts composed at their exact printed values and clauses; Tear in Time boxed; roster selection, standee/profile and card art, decision labels, bots, wording and offer/outcome checks. The configured match and network seat use the same authority. T3.

### [ ] 9.50 Gate Emmitt on the player path

Outcome: permanent face-to-journey matrix with real controls and asserted outcomes, plus the highest applicable roster validation tier. T3.

### [ ] 9.51 NebKher's Illusions and borrowed actions

Outcome: typed Illusion token placement, swap, short supply and cleanup; Mind Grip performs an allowed action on the chosen enemy's previous-slot card with token substitution and no markers; the special friendly-melee-minion count applies at its printed action scope. Focused rules, snapshot, replay, owner-seat and token tests. T2. Requires the Mind Grip and Illusion rulings.

### [ ] 9.52 NebKher's laughter and divided board

Outcome: Diabolical Laughter's optional repeated choices and the Ultimate's trigger; a selected straight line through NebKher's space enforces the two printed cross-side interaction restrictions for Crack in Reality and Shift Reality, across every target, space, movement, placement and area query. Board geometry, timing, snapshot, replay and wire checks. T2. Requires the laughter and line rulings.

### [ ] 9.53 Compose and deal NebKher

Outcome: all eighteen playable fronts composed at their exact printed values and clauses; Mind Probe boxed; roster selection, standee/profile and card art, decision labels, bots, wording and offer/outcome checks. T3.

### [ ] 9.54 Gate NebKher and the complete roster on the player path

Outcome: permanent face-to-journey matrix with real controls and asserted outcomes, plus the highest applicable full-roster validation tier. The resulting configured roster is thirty-two heroes. T3.
