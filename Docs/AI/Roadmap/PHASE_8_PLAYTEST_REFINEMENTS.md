# Phase 8 - Playtest refinements

On 2026-09-17 the user reported Super-Shotgun making the attacked hero discard, requested the revealed-card Initiative badge include the bonus, and approved fixing both Shotgun and Super-Shotgun. They then requested that clicking another player's token must not show that player's cards. The approved sequence is **8.42 -> 8.42a -> 8.42b**, with separate validation and local commits. Return to [ROADMAP.md](../ROADMAP.md); [SESSION_BRIEF.md](../SESSION_BRIEF.md) owns the live route.

### [x] 8.42 Exclude the attack target from shotgun scatter

Outcome: Shotgun and Super-Shotgun affect a different enemy hero adjacent to the attack target, never that target itself.

Acceptance: use the existing typed enclosing-target exclusion; reproduce the reported self-discard failure before correction; exercise both actual card definitions against hero and minion targets, with and without an eligible bystander; preserve discard ownership, Super-Shotgun's inability-to-discard Defeat, and continuation of the original attack. Focused Core and directly affected regressions, plus an actual player-input journey for the reported path (T2). Preserve deterministic replay/restore and content identity contracts.

Non-goals: shared distance-query changes, rule reinterpretation, other cards, new packaging.

Historical evidence: [8.42 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842-shotgun-target-exclusion-completion-evidence).

### [x] 8.42a Show total revealed Initiative in the hero rail

Outcome: the revealed-card badge in each hero plaque shows printed Initiative plus Initiative items, using authoritative public counts and the shared item query. The printed card front remains unchanged. Validation also repairs Swift's artwork catalog references after 8.42's content-revision bump; that resource metadata requires the build-smoke check.

Acceptance: base-only and bonus cases, correct per-hero totals, no facedown information leak, updates across reveal/resolution/round refresh, existing hover and badge geometry preserved, and owner-private/Director projection agreement. Focused projection and HUD checks, affected Play Mode and targeted visual inspection (T2); no Initiative resolution rule or protocol/schema change.

Non-goals: other statistics, tray redesign, unrelated presentation changes, packaging.

On 2026-09-18 the user approved `Tools/Invoke-PlayerBuildSmoke.ps1` for the updated resource catalog, discharging the checkpoint's prerequisite. No package or upload is authorized.

Historical evidence: [checkpoint](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842a-initiative-badge-build-gate-checkpoint); [completion](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842a-initiative-badge-completion-evidence).

### [ ] 8.42b Keep player tray navigation private and connect hero identity controls

Outcome: clicking another player's board token cannot show that player's card tray in a player seat; the seat's own tray remains selected. Clicking the player's own hero plaque selects that hero. Hovering a plaque's hero portrait shows its existing hero-picker profile card. Director-only inspection is preserved. The user explicitly added the own-plaque selection and portrait-preview requirements on 2026-09-17.

Acceptance: actual board clicks on allied and enemy tokens from a player seat keep the owner's tray, hide all foreign tray cards and preserve own card actions; own-plaque click selects the local hero; profile hover uses the correct existing artwork, stays on-screen and dismisses cleanly; private projection boundaries and reconnect/refresh remain intact; Director pinning still works. Focused presenter and affected real-input/online Play Mode checks plus visual inspection (T2).

Non-goals: removing public hero-rail history or hover information, changing game visibility rules, or disabling Director tools.
