# Phase 5 - Core full-map actions and combat

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_5 handoffs](../Archive/Phase-Handoffs/PHASE_5.md).

## Phase 5 - Core full-map actions and combat

Phase 5 uses verified general rules and synthetic test definitions. It does not depend on the complete Wasp card set, inventory that set, or implement it.

### [ ] 5.1 Integrate and verify exact card-based Movement on the full map

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: A synthetic Movement action uses its typed allowance to traverse a chosen legal route on the official placeholder map.

Acceptance checks:

- The action reuses Phase 1 route choice, obstacle blocking, command/event authority, and pending resolution.
- The Phase 1 segmented waypoint interaction is integrated on the official placeholder map so the player can construct any legal cumulative route within the allowance, including a deliberately longer/non-shortest route to a destination.
- Focused source-linked tests prove shortest and longer legal cumulative routes to the same destination commit their selected ordered steps deterministically without enumerating every complete route up front.
- Action-start restrictions are evaluated at the correct checkpoint.
- Up-to movement permits zero movement and early stopping.
- Ordinary Movement remains distinct from direct movement written in card text.

### [ ] 5.2 Implement Fast Travel replacement

Implements only after milestone 5.1 and the Phase 4 zone model are complete.

Acceptance checks:

- Fast Travel works within the same zone and into an adjacent zone.
- A non-adjacent destination zone is rejected.
- Enemy presence in the origin or destination zone blocks eligibility.
- No empty-space path between origin and destination is required.
- Fast Travel replaces the entire Movement action and suppresses Primary card text.

### [ ] 5.3 Add minion obstacle behavior and Defense contributions

Observable outcome: Standard minions are authoritative units/obstacles and contribute their general Defense modifiers through the typed value service.

Acceptance checks:

- The rules reuse the authoritative minion identities, teams, types, and spaces created by milestone 4.4 rather than creating a second representation.
- Hero/minion occupancy follows the one-obstacle-per-space rule.
- Defense contributions retain minion-source provenance.
- Heavy, Ranged, and Melee identity is not inferred from presentation.

### [ ] 5.4 Add generic timing, Active-effect, and continuous-source lifecycles

Observable outcome: Synthetic effects can occupy named timing windows and serializable lifetimes without Wasp-specific logic.

Acceptance checks:

- Before/After Attack, action start/end, turn end, round end, and defeat/card-state cancellation hooks are explicit.
- Every timed effect definition explicitly declares its affected scope/controller, duration kind (`This Turn`, `This Round`, `Next Turn`, or `Next Turn after Playing Cards`), creation/activation anchor, exact expiration checkpoint, early-cancellation conditions, and whether it carries across an end-of-round boundary; the engine never infers those facts from display text or collection order.
- A `This Turn` effect created at action start or partway through a turn applies only for its declared remaining window and expires exactly once at the authoritative turn-only cancellation checkpoint before `TurnEnded`, with no off-by-one carry into the next turn.
- A `This Round` effect survives intervening turns and expires exactly once at its declared authoritative round-end checkpoint.
- A `Next Turn` effect remains dormant until the exact affected hero/controller's next applicable turn, is not retargeted by Initiative reordering, and expires exactly once after its declared window. Focused same-round and cross-round cases assert the definition's explicit carry-or-cancel policy rather than assuming all Next Turn effects cross the round boundary.
- `Next Turn after Playing Cards` remains distinct from ordinary `Next Turn` and cannot activate before the exact authoritative play/reveal checkpoint declared by its definition.
- Creation partway through a turn, source-card state change, applicable hero defeat, early cancellation, normal expiry, and simultaneous expiry/trigger ordering cannot double-apply or double-cancel an effect.
- Active-effect state, dormant/active phase, anchors, and remaining lifetime round-trip through the versioned snapshot boundary and produce equivalent state/events through live execution, restore plus continuation, replay, and retained-prefix Undo.
- Active effects and passive/continuous sources remain distinct.
- Simultaneous ordering uses the official tie procedure without flipping the Tie Breaker.
- Presentation timing cannot delay or reorder rules checkpoints.

Non-goals: No Wasp-specific text or duration interpretation, runtime printed-text parsing, full Phase 6 card-retrieval/cancellation integration, networking, or presentation-owned timing.

### [ ] 5.5 Add coin awards and assist-ready accounting

Observable outcome: Defeat outcomes can award coins to the correct hero/team collection through typed resource operations.

Acceptance checks:

- The 1v1 setup awards the acting hero correctly.
- The model supports friendly assist recipients without implementing 3v3 UI.
- Rewards remain distinct from removal so prevented removal does not erase a verified Defeat reward.

Non-goals: Exact hero-Level and lowest-Tier reward amounts remain part of complete hero-defeat integration at milestone 7.4.

### [ ] 5.6 Attack and defeat minions with test cards

Observable outcome: A synthetic Attack can legally target and defeat an enemy minion using the general minion rules.

Acceptance checks:

- Minions do not open a Defense reaction.
- Heavy immunity applies while a friendly non-Heavy minion remains in the Battle Zone.
- Defeat, reward, removal attempt, and removal result remain distinct typed facts.
- Melee/Ranged and Heavy rewards use verified general values.
- Last-minion Lane-Push handling remains a separate Phase 6 integration.

### [ ] 5.7 Return displaced minions to the Battle Zone

Acceptance checks:

- Correction applies only to intrinsic minion miniatures outside the Battle Zone after the whole action.
- A minion follows a shortest empty path; its team chooses among equally short valid paths.
- With no path or legal move, the minion is Placed into a nearest empty Battle-Zone space.
- The fallback implements the minimum general Place capability through typed policy/outcome data and emits Place rather than Movement facts.
- When cross-team order matters, the Tie-Breaker-side team resolves first without flipping the coin.
- End-to-end ordering between separate Repeat instances is deferred until the first verified Repeat implementation.

### [ ] 5.8 Complete the general Hero Attack and Defense order with test cards

Observable outcome: Synthetic hero combat follows the complete general Attack/Defense order while still deferring Wasp-specific text.

Acceptance checks:

- Target validation, Before Attack, Attack Total, Defense response, Block/result comparison, After Attack, and action-end checkpoints occur in verified order.
- Focused synthetic scenarios integrate and regress both milestone 2.3's non-Ranged adjacent targeting and milestone 2.3a's Ranged targeting through typed Range/distance queries within the complete Attack/Defense order.
- Printed, minion, item-ready, card-text-ready, and Active-effect-ready contributions remain distinct even when only synthetic contributions are populated.
- A successful Attack records a staged Defeat outcome for Phase 7 consequences.
- Wasp card text, values, items, and special effects are not used.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused combat scenarios.

### [!] 5.8a Independently review the full-map action and combat foundation

Blocked by: Milestone 5.8 must pass its Phase 5 gate.

Observable outcome: A fresh analysis-only review determines whether full-map Movement, Fast Travel, minion interactions, Attack/Defense, timing readiness, and their shared action-resolution seams can safely support the end-of-round systems in Phase 6.

Acceptance checks:

- The review inspects actual action dispatch, typed handlers, queries, pending continuations, atomic rejection, event ordering, and full-map scaling for representative accepted, rejected, resumed, restored, and replayed paths.
- It looks for duplicated resolution routes, cross-handler leakage, growing conditional complexity, hidden state ownership, repeated full-map queries or allocations, and tests that prove isolated examples without protecting shared invariants.
- It distinguishes verified defects and measured costs from speculative risks or stylistic preferences, following the common independent-review evidence contract.
- Any justified fix is proposed as a separate corrective micro-milestone; the review itself changes no runtime, tests, content, or rules.
- The Phase 6 sequence is re-evaluated against the evidence, including whether any prerequisite must move ahead of Minion Battle, token/marker, Lane Push, or retrieval work.

Non-goals: No Wasp inventory or behavior, end-of-round implementation, combat redesign, speculative optimization, runtime fix, or relaxation of verified rules and validation gates.

Hard gate: Phase 6 does not begin until milestone 5.8a is reviewed and every gate-blocking finding has an approved disposition.
