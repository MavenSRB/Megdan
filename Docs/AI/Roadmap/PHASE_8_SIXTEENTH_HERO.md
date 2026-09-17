# Phase 8 - Sixteenth-hero dealing: Garrus the Gladiator

Return to [roadmap](../ROADMAP.md). Current authorization and next route live in [the brief](../SESSION_BRIEF.md).

## Authorization and scope

On 2026-09-17 the user requested implementation of the next hero and selected Garrus: "Garrus is the one I want." This authorizes the bounded sequence **8.41 -> 8.41a -> 8.41b -> 8.41c -> 8.41d -> 8.41e -> 8.41f**, through a fully selectable, artworked hero with working printed cards and the permanent player-path gate. Execute sequentially with separate validation and local commits. Necessary corrections discovered by those checks belong to this sequence; a substantive unresolved rule interpretation still requires authoritative resolution. No other hero or parked program is authorized. No new distributable package or remote publication is included.

[Garrus's rules shard](../Rules/HERO_GARRUS.md) owns his inventory and capability mapping. Eighteen faces are dealt; Angry Swipe is the boxed p.20 Handicap. Existing declared capabilities must be inspected before creating new ones. New generic vocabulary remains outside production registration until a dealt consumer exists, following the existing registration gate. The test-owned shared-foundation claim catalog must record each new or widened claim with its exact scope and evidence.

### [x] 8.41 Inventory Garrus and map the implementation sequence

Outcome: source-backed identity, nineteen-face inventory, exact text/values/upgrades, capability mapping and outstanding dependencies are recorded.

Acceptance: inspect all fronts and selection/dashboard; reconcile set size and back hashes; check the local rulebook for Garrus-specific authority and relevant general rules; distinguish source facts from implementation predictions. Record the complete bounded sequence. T0 documentation validation; no Unity.

Non-goals: executable content or mechanics.

Historical evidence: [8.41 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-841-inventory-garrus-completion-evidence).

### [x] 8.41a Support discarding resolved cards through the ordinary card lifecycle

Outcome: a typed optional choice can discard one owned resolved card, preserving played identity and canceling its Active effect, while round retrieval and card-text retrieval recover the resulting card correctly.

Acceptance: source-state eligibility; correct chooser; stale/foreign/unresolved rejection; deterministic choice ordering; selected-card identity and discard event; played/discarded representation; turn-slot history; active-source cancellation; round and explicit retrieval; snapshot/restore and replay. Trace all existing discard producers and the future trigger integration point. Focused Core/CardState/Application checks plus enumerated lifecycle/restore regressions, T2 or higher if a durable schema changes.

Non-goals: Battle Fury or dealing Garrus.

Historical evidence: [8.41a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-841a-resolved-card-discard-completion-evidence).

### [x] 8.41b Complete Garrus's attack and movement vocabulary

Outcome: Angry Strike can count the acting hero's discarded cards, and the Red cards can express their exact modal pre-move and optional move-adjacent-then-push clauses using verified generic contracts.

Acceptance: live discard count including played discards and facedown cards; owner isolation; Attack item composition; conditional before-attack move followed by hero-only targeting versus ordinary unit targeting; optional move ending beside a named enemy hero followed by that hero's three-space obstacle-ignoring Push, with the zero-space reading resolved by GA-Q1 before implementation; zero/decline and impossible paths; immunity/Heavy; persisted continuation. Probe existing selection/movement contracts first and extend only demonstrated missing vocabulary. Focused and directly affected T2 checks; enumerate the shared-foundation claims.

Non-goals: new content registration or unrelated movement refactors.

Paused checkpoint: `checkpoint/8.41b-rule-gate` preserves the implemented live owned-discard Attack query and its focused evidence; it is not milestone completion. **Prerequisite:** resolve [GA-Q1](../Rules/HERO_GARRUS.md#open-verification-points) using an official source or an explicit user-approved interpretation. Remaining closing checks are the modal pre-move/target path, named-hero post-move Push, distance/decline/impossible/immunity cases, and persisted movement continuation. Existing direct Move supports up-to zero as an applied no-op; existing charge-line selection requires a positive straight route and publishes only an endpoint, so neither alone establishes the printed `if you do` meaning. The existing query's old enum identities and all production content remain unchanged; no Garrus face is dealt by this checkpoint.

Checkpoint index: [8.41b rule gate](../Archive/Phase-Handoffs/PHASE_8.md#milestone-841b-garrus-attack-vocabulary-rule-gate-checkpoint).

Resumed after the user's 2026-09-17 GA-Q1 answer: choosing zero spaces while adjacent does permit the Push. The checkpoint above records the former prerequisite, now discharged; all remaining acceptance checks still apply.

Historical evidence: [8.41b completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-841b-garrus-attack-and-movement-completion-evidence).

### [ ] 8.41c Execute Battle Fury after a resolved-card discard

Outcome: an unlocked Ultimate belonging to an on-board hero observes an actual resolved-card discard and optionally grants that exact card's primary action, then resumes the interrupted operation.

Acceptance: trigger exactly once per actual transition; no hand-card/Defense discard or state-swap false positive; owner and on-board checks; correct card identity; source stays discarded; full printed primary and ordinary repeats; nested choices and timing order; decline/impossible action; source cancellation; deterministic events, snapshot restore and retained-prefix Undo; exercise all reachable resolved-discard producers. Use the established grant/borrow and timing systems without UI resolution. T2 minimum, T3 if persisted schema/infrastructure scope requires it.

Non-goals: changes to existing Ultimate meanings.

### [ ] 8.41d Compose Garrus's eighteen playable faces

Outcome: all nineteen definitions preserve printed data and all eighteen playable faces carry complete executable compositions, including Chilling Howl and Battle Fury.

Acceptance: one source-linked row per face; upgrades/items; lazy content initialization; composition validation against registered plus declared deferred contracts; every distinct legal behavior and negative at Core tier; corpus sweep membership; count/selection/icon distinctions; mandatory/optional/repeat semantics; no invented Handicap behavior. Focused and directly affected T2 checks.

Non-goals: dealing, artwork registration or claiming the player gate passed.

### [ ] 8.41e Deal, register and artwork Garrus

Outcome: Garrus is the sixteenth legal lobby pick, with his real hover profile, card fronts, board token, hand/deck/upgrades and catalog registrations.

Acceptance: standard setup and staged/engaged/progressed configuration routes; all scene-content adapters; selection/hero/artwork bijections; register only reachable contracts; content identity and snapshot compatibility decision; preserve existing heroes' identities; correct source token visual match; focused scene/picker/artwork/complete-card footprint checks and applicable build-smoke affectedness. Apply the highest testing tier triggered by catalog/configuration changes.

Non-goals: a new package or the final hero gate's completion claim.

### [ ] 8.41f Complete Garrus's permanent player-path gate

Outcome: every playable face and interaction shape is covered by the mechanically complete hero matrix and configured real-input journeys.

Acceptance: selection/spawn/identity, actual pointer/board input, commit/reveal, primaries/secondaries, modal and repeat choices, direct movement/Push, opponent discard/Defeat, retrieval, progression/items/Ultimate, Active aura reading, round cleanup, defeat/respawn, stale input, no EngineFault, projection agreement, restore and retained-prefix Undo on a Garrus-specific pause, restart/rebuild, whole-face footprint/short viewport and targeted non-batch visual inspection. No required journey may be Explicit/skipped. Run the breadth required by testing policy, with focused evidence first; review player experience, rules authority, maintainability and risk before closure.

Non-goals: unrelated cleanup or additional heroes.
