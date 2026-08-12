# Phase 5 - Core full-map actions and combat

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_5 handoffs](../Archive/Phase-Handoffs/PHASE_5.md).

## Phase 5 - Core full-map actions and combat

Phase 5 uses verified general rules and synthetic test definitions. It does not depend on the complete Wasp card set, inventory that set, or implement it.

### [x] 5.1 Integrate and verify exact card-based Movement on the full map

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: A synthetic Movement action uses its typed allowance to traverse a chosen legal route on the official placeholder map.

Acceptance checks:

- The action reuses Phase 1 route choice, obstacle blocking, command/event authority, and pending resolution.
- The Phase 1 segmented waypoint interaction is integrated on the official placeholder map so the player can construct any legal cumulative route within the allowance, including a deliberately longer/non-shortest route to a destination.
- Focused source-linked tests prove shortest and longer legal cumulative routes to the same destination commit their selected ordered steps deterministically without enumerating every complete route up front.
- Action-start restrictions are evaluated at the correct checkpoint.
- Up-to movement permits zero movement and early stopping.
- Ordinary Movement remains distinct from direct movement written in card text.

Historical evidence: [milestone 5.1 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-51-integrate-and-verify-exact-card-based-movement-on-the-full-map-completion-evidence).

### [x] 5.1a Increase synthetic Movement and keep Defense controls accessible

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The ten synthetic test cards provide slightly more board traversal, and every pending Defense response remains reachable in the configured HUD at a 778-by-300 viewport.

Acceptance checks:

- Every Red and Blue synthetic card's printed Movement value increases by exactly one; Initiative, Attack, Range, Defense, stable IDs, ownership, and content authority remain unchanged, while the content revision advances so older snapshots cannot silently acquire the new values.
- The configured card panel stays within the viewport and scrolls its hands/action body when the available height cannot display every row.
- Defense selection and decline remain typed projected invocations rendered by the passive view; presentation does not infer legality or own resolution.
- Focused definition/session and snapshot-compatibility tests lock the revised synthetic matrix and revision boundary, a deterministic layout regression proves the reported 778-by-300 viewport boundary, and configured saved-scene Play Mode covers the runtime HUD path.

Non-goals: No official card values, Fast Travel, direct Move, Defense rules, legacy-controller compatibility UI, production styling, or broader responsive-layout system.

Historical evidence: [milestone 5.1a handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-51a-increase-synthetic-movement-and-keep-defense-controls-accessible-completion-evidence).

### [x] 5.1b Show printed Defense values on legal Defense choices

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: While a Defense response is pending, every selectable Defense-card button shows that card's authoritative printed Defense value before the player chooses whether to discard it.

Acceptance checks:

- Each legal Defense-card invocation label contains the card display name and the typed printed Defense total derived from its authoritative action offer and definition.
- The label distinguishes the value as `PRINTED DEFENSE`; it does not imply that unimplemented items, minions, active effects, Block, or other modifiers are included.
- A card remains legal based only on the existing reaction-window ownership, in-hand, unplayed, typed Defense-action, and Hand-to-Discard-Pile requirements; the presentation change does not broaden eligibility or decide whether the Defense succeeds.
- `DECLINE DEFENSE` remains unchanged, and every button continues to carry its existing typed Defense command without presentation-owned legality or resolution.
- Focused presenter tests cover multiple legal cards with distinct values and the unchanged decline command; configured saved-scene Play Mode covers the passive HUD path.

Non-goals: No Defense-rule, value-calculation, card-content, snapshot, modifier, minion, Block, Attack-outcome, legacy-controller UI, or broader card-detail redesign.

Historical evidence: [milestone 5.1b handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-51b-show-printed-defense-values-on-legal-defense-choices-completion-evidence).

### [x] 5.1c Restore readable content-sized Defense choice buttons

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Every configured Defense-card choice renders its complete card name and `PRINTED DEFENSE` value without clipping while preserving the existing typed invocation and scrollable HUD.

Acceptance checks:

- Defense-card controls no longer force the fixed 150-pixel invocation width that clips longer two-line labels; their rendered width is derived from the complete label and current GUI style, consistent with the existing content-sized normal-action path.
- The longest current synthetic Defense label is fully readable at the configured saved-scene viewport with no left/right clipping, truncation, overlap, or loss of the second line. At the validated 2048-by-1152 desktop Game View, the expanded panel shows the four-card turn-one Defense set plus `DECLINE DEFENSE` simultaneously without scrollbars; short viewports still clamp and keep every choice reachable through the existing scrollable fallback.
- Every button retains its exact projected `ChooseDefenseCard` or `DeclineDefense` command, and presentation still owns no eligibility, value, or resolution authority.
- A deterministic layout regression fails when the allocated Defense-button width is smaller than its measured full content, the configured saved-scene Play Mode fixture covers the passive HUD path, and completion includes a visible non-batch Game View inspection of the longest label. A blank/headless framebuffer or model-only label assertion is not visual evidence.

Non-goals: No Defense rules, label-content change, card content, value calculation, HUD redesign, production styling, legacy-controller UI, or unrelated action/initiative/end-turn sizing change.

Historical evidence: [milestone 5.1c handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-51c-restore-readable-content-sized-defense-choice-buttons-completion-evidence).

### [x] 5.2 Implement Fast Travel replacement

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Acceptance checks:

- Fast Travel works within the same zone and into an adjacent zone.
- A non-adjacent destination zone is rejected.
- Enemy presence in the origin or destination zone blocks eligibility.
- No empty-space path between origin and destination is required.
- Fast Travel replaces the entire Movement action and suppresses Primary card text.
- The configured player UI visibly offers Fast Travel only when the application projection contains an eligible replacement with at least one authoritative legal destination.
- The board highlights only the projected legal destinations; selecting one submits the typed Fast Travel command through the existing board presenter and match session, while presentation owns no zone, enemy-presence, or destination legality.
- Configured saved-scene Play Mode coverage proves the offer, destination highlights, pointer submission, authoritative relocation, source-card resolution, and accepted-command identity.

Historical evidence: [milestone 5.2 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-52-implement-fast-travel-replacement-completion-evidence).

### [x] 5.2a Require explicit Fast Travel selection in the configured player UI

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Fast Travel is an explicit player-selected action mode rather than the configured UI's default assumption whenever an eligible replacement exists.

Acceptance checks:

- The configured player UI presents Fast Travel as a clickable button only when the application projection contains an eligible replacement with at least one authoritative legal destination.
- When no eligible Fast Travel replacement is projected, the button is absent rather than disabled or presented as unavailable.
- Merely receiving an eligible Fast Travel projection does not activate Fast Travel or expose its destination highlights; the normal projected action choices remain the default interaction state.
- Clicking the Fast Travel button activates its selection mode and exposes only the authoritative projected legal destinations.
- Selecting a highlighted destination after explicit activation submits the existing typed Fast Travel command through `BoardInteractionPresenter` and `IMatchSession`; presentation still performs no zone, enemy-presence, or destination-legality calculation.
- Focused presenter/view-model coverage proves visible-versus-hidden button state and explicit activation, while configured saved-scene Play Mode coverage proves button selection, destination highlights, pointer submission, and authoritative relocation.

Non-goals: No Fast Travel rule, eligibility, destination-query, command, replay, Undo, card-resolution, general action-selection, or production-styling change.

Historical evidence: [milestone 5.2a handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-52a-require-explicit-fast-travel-selection-in-the-configured-player-ui-completion-evidence).

### [x] 5.3 Add minion obstacle behavior and Defense contributions

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Standard minions are authoritative units/obstacles and contribute their general Defense modifiers through the typed value service.

Acceptance checks:

- The rules reuse the authoritative minion identities, teams, types, and spaces created by milestone 4.4 rather than creating a second representation.
- Hero/minion occupancy follows the one-obstacle-per-space rule.
- Defense contributions retain minion-source provenance.
- Heavy, Ranged, and Melee identity is not inferred from presentation.

Historical evidence: [milestone 5.3 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-53-add-minion-obstacle-behavior-and-defense-contributions-completion-evidence).

### [x] 5.4 Add generic timing, Active-effect, and continuous-source lifecycles

Required Codex profile: GPT-5.6 Sol with high reasoning.

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

Historical evidence: [milestone 5.4 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-54-add-generic-timing-active-effect-and-continuous-source-lifecycles-completion-evidence).

### [x] 5.5 Add coin awards and assist-ready accounting

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Defeat outcomes can award coins to the correct hero/team collection through typed resource operations.

Acceptance checks:

- The 1v1 setup awards the acting hero correctly.
- The model supports friendly assist recipients without implementing 3v3 UI.
- Rewards remain distinct from removal so prevented removal does not erase a verified Defeat reward.

Non-goals: Exact hero-Level and lowest-Tier reward amounts remain part of complete hero-defeat integration at milestone 7.4.

Historical evidence: [milestone 5.5 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-55-add-coin-awards-and-assist-ready-accounting-completion-evidence).

### [x] 5.6 Attack and defeat minions with test cards

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A synthetic Attack can legally target and defeat an enemy minion using the general minion rules.

Acceptance checks:

- Minions do not open a Defense reaction.
- Heavy immunity applies while a friendly non-Heavy minion remains in the Battle Zone.
- Defeat, reward, removal attempt, and removal result remain distinct typed facts.
- Melee/Ranged and Heavy rewards use verified general values.
- Last-minion Lane-Push handling remains a separate Phase 6 integration.

Historical evidence: [milestone 5.6 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-56-attack-and-defeat-minions-with-test-cards-completion-evidence).

### [x] 5.8 Complete the general Hero Attack and Defense order with test cards

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: Synthetic hero combat follows the complete general Attack/Defense order while still deferring Wasp-specific text.

Acceptance checks:

- Target validation, Before Attack, Attack Total, Defense response, Block/result comparison, After Attack, and action-end checkpoints occur in verified order.
- Focused synthetic scenarios integrate and regress both milestone 2.3's non-Ranged adjacent targeting and milestone 2.3a's Ranged targeting through typed Range/distance queries within the complete Attack/Defense order.
- Printed, minion, item-ready, card-text-ready, and Active-effect-ready contributions remain distinct even when only synthetic contributions are populated.
- A successful Attack records a staged Defeat outcome for Phase 7 consequences.
- Wasp card text, values, items, and special effects are not used.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused combat scenarios.

Historical evidence: [milestone 5.8 handoff](../Archive/Phase-Handoffs/PHASE_5.md#milestone-58-complete-the-general-hero-attack-and-defense-order-completion-evidence).

### [ ] 5.8a Independently review the full-map action and combat foundation

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: A fresh analysis-only review determines whether full-map Movement, Fast Travel, minion interactions, Attack/Defense, timing readiness, and their shared action-resolution seams can safely support the end-of-round systems in Phase 6.

Acceptance checks:

- The review inspects actual action dispatch, typed handlers, queries, pending continuations, atomic rejection, event ordering, and full-map scaling for representative accepted, rejected, resumed, restored, and replayed paths.
- It looks for duplicated resolution routes, cross-handler leakage, growing conditional complexity, hidden state ownership, repeated full-map queries or allocations, and tests that prove isolated examples without protecting shared invariants.
- It distinguishes verified defects and measured costs from speculative risks or stylistic preferences, following the common independent-review evidence contract.
- Any justified fix is proposed as a separate corrective micro-milestone; the review itself changes no runtime, tests, content, or rules.
- The Phase 6 sequence is re-evaluated against the evidence, including whether any prerequisite must move ahead of Minion Battle, token/marker, Lane Push, or retrieval work.

Non-goals: No Wasp inventory or behavior, end-of-round implementation, combat redesign, speculative optimization, runtime fix, or relaxation of verified rules and validation gates.

Hard gate: Phase 6 does not begin until milestone 5.8a is reviewed and every gate-blocking finding has an approved disposition.
