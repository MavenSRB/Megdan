# Phase 8 - Playtest refinements

On 2026-09-17 the user reported Super-Shotgun making the attacked hero discard, requested the revealed-card Initiative badge include the bonus, and approved fixing both Shotgun and Super-Shotgun. The approved sequence is **8.42 -> 8.42a**, with separate validation and local commits. Return to [ROADMAP.md](../ROADMAP.md); [SESSION_BRIEF.md](../SESSION_BRIEF.md) owns the live route.

### [x] 8.42 Exclude the attack target from shotgun scatter

Outcome: Shotgun and Super-Shotgun affect a different enemy hero adjacent to the attack target, never that target itself.

Acceptance: use the existing typed enclosing-target exclusion; reproduce the reported self-discard failure before correction; exercise both actual card definitions against hero and minion targets, with and without an eligible bystander; preserve discard ownership, Super-Shotgun's inability-to-discard Defeat, and continuation of the original attack. Focused Core and directly affected regressions, plus an actual player-input journey for the reported path (T2). Preserve deterministic replay/restore and content identity contracts.

Non-goals: shared distance-query changes, rule reinterpretation, other cards, new packaging.

Historical evidence: [8.42 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842-shotgun-target-exclusion-completion-evidence).

### [ ] 8.42a Show total revealed Initiative in the hero rail

Outcome: the revealed-card badge in each hero plaque shows the card's Initiative plus its applicable Initiative bonus, using authoritative projected values.

Acceptance: base-only and bonus cases, correct per-hero totals, no facedown information leak, updates across reveal/resolution/round refresh, existing hover and badge geometry preserved, and owner-private/Director projection agreement. Focused projection and HUD checks, affected Play Mode and targeted visual inspection (T2); no Initiative resolution rule or protocol/schema change.

Non-goals: other statistics, tray redesign, unrelated presentation changes, packaging.
