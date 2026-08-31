# Phase 8 - Late HUD and card-interaction corrections

Canonical active roadmap shard for the player-requested HUD and card-interaction corrections found after milestone 8.30d and before the complete six-seat local validation gate. Return to [ROADMAP.md](../ROADMAP.md) or the surrounding [Phase 8 route](PHASE_8.md).

The user approved defining and scheduling these milestones on 2026-08-30. That roadmap amendment does not authorize their implementation: each milestone still requires its own explicit approval under the roadmap execution rule. Milestone 8.30c was already being implemented when these corrections were collected, so 8.30f follows it, 8.30g follows 8.30f, and the separately iterative board treatment at 8.30h follows the completed card interaction. A further user-approved amendment on 2026-08-31 defines milestone 8.30i, which follows 8.30h because it reuses that milestone's shared six-corner construction, and which carries the final pre-network evidence refresh. Each correction reruns the six-seat checks it affects before private-online work begins.

### [x] 8.30f Recompose the turn controls and correct small HUD readings

Blocked by: Milestone 8.30c must complete the already-started six-seat local player-experience gate; milestone 8.30d already provides the current action-shine, corner-banner, narration, and card-tray treatment.

Observable outcome: End Turn keeps its current panel-bottom-right position while the facedown commitment confirmation moves alone into the open lane immediately below the `DIRECTOR` toggle; action highlights are clearly visible at rest and become stronger without moving under pointer or focus; compact hero-rail numbers are centred and readable; and a card discarded for Defense joins the unavailable side of its owner's hand rather than remaining beside playable cards.

Acceptance checks:

- The `DIRECTOR` toggle and End Turn keep their current positions, including End Turn's panel-bottom-right corner. `CONFIRM FACEDOWN` alone moves immediately below `DIRECTOR`; it does not appear in End Turn's corner, move a hero rail, cover the ordinary hand or map, or create a duplicate invocation. Pass retains its existing meaning and placement.
- Clicking a hand card retains the existing select-then-confirm flow. Moving the confirmation surface changes no commitment eligibility, command, hidden-information boundary, focus order, or facedown result.
- Every prepared-front printed-action highlight has identical resolved geometry at rest, pointer hover, and keyboard focus. Hover and focus may change only visual strength such as opacity, colour, radiance, or contrast; they do not translate, scale, resize, or swap to differently aligned artwork.
- A legal resting highlight is unmissable at real card size over both a pale Primary Skill text panel and dark or busy artwork. Its hover/focus state is visibly stronger still. Movement's boot/value badge, Primary Skill and Attack panels, and the generated fallback retain their typed placement and hit regions; unavailable actions remain visually distinct and inert.
- Prepared Movement badges use one user-approved canonical full-boot silhouette at the shared rail anchor, so per-card colour classification cannot collapse a same-hue boot around its numeral. Primary printed-line rings and Defense badges retain their typed per-card geometry; source artwork is unchanged.
- The initiative number on a revealed current/played-card marker is visually centred both horizontally and vertically inside its fixed badge at one- and two-digit values. The badge footprint and card lifecycle meaning do not change.
- The coin count grows from the current 9-point numeral to a clearly readable size, with the coin disc expanding only as much as needed to contain it. The coin remains in the existing item/coin strip, stays circular, preserves its tooltip and assistive reading, and does not collide with item chips or change rail width.
- When a Defense card is discarded, the next projection places that card on the left/unavailable side of its owner's hand/tray rather than leaving it on the right beside cards that can still be played. This is projection-driven presentation ordering only: Defense choice, discard state, card identity, restore, replay, Undo, restart, and another seat's hidden-hand boundary remain unchanged.
- Focused Edit Mode view/geometry regressions and focused Play Mode pointer journeys pass at 2560x1440 and 2048x1152, followed by targeted non-batch Game View inspection of the resting and hovered Movement/Skill highlights, Confirm below Director with End Turn still bottom-right, both number treatments, and the post-Defense hand ordering.

Closing checks: `ConfirmStandsBelowDirectorWhileEndTurnKeepsItsCornerWithoutDuplicates`, `PrintedActionHighlights_StrengthenInPlaceAndRemainReadableAtRest`, and `HeroRailNumbersAndDefenseDiscardOrdering_MatchTheCorrectedHud` reproduce the reported pre-fix layouts and pass after the correction.

Non-goals: No rule, action availability, Defense resolution, discard lifecycle, commitment authority, card definition, source-artwork modification, per-card manual Movement-mask tuning, hidden-information contract, board layout, narration, networking, general HUD redesign, animation loop, or new settings/accessibility suite.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830f-recompose-the-turn-controls-and-correct-small-hud-readings-completion-evidence).

### [x] 8.30f1 Migrate the editor patch and reconcile the stale Confirm geometry gate

Blocked by: Milestone 8.30f established the accepted commitment-control geometry; Unity 6000.5.9f1 must be installed and its new analyzer hash admitted by Smart App Control without weakening Windows security.

Observable outcome: The project, launchers, live recipes, and current platform authorities use Unity 6000.5.9f1, whose editor and compiler path run with Smart App Control and Microsoft Defender still enabled; the one Play Mode geometry assertion left behind by 8.30f now verifies `CONFIRM FACEDOWN` below `DIRECTOR` rather than expecting the retired End Turn-corner placement.

Acceptance checks:

- `ProjectSettings/ProjectVersion.txt`, the local launchers, README, live Unity recipes, and current editor/platform authority agree on Unity 6000.5.9f1 and revision `b57deb96f08d`; historical records remain unchanged.
- `Scene_StacksCurrentActionControlsAboveHandStatusWithoutOverlap` records the observed pre-fix failure against the End Turn rectangle and passes after comparing the live confirmation banner with `ConfirmFacedownControlRect`; it still proves the dock has no duplicate confirmation and End Turn retains its own bottom-right rectangle.
- The launcher contract checks, focused HUD Edit Mode fixture, focused closing Play Mode check, and both complete Unity suites pass with fresh XML/log evidence. Windows records no 6000.5.9f1 UI Toolkit source-generator block, Smart App Control stays in enforcement mode, and Defender real-time protection stays enabled.
- The editor-resolved `com.unity.searcher` patch and Project Auditor serialization reorder are recorded as toolchain-only migration output; no selected networking package, game package, rule, schema, content identity, or presentation behavior changes.

Closing check: `Scene_StacksCurrentActionControlsAboveHandStatusWithoutOverlap` failed at the pre-fix migration revision by comparing the correct below-`DIRECTOR` banner with End Turn's corner and passes once the assertion names the accepted 8.30f geometry.

Non-goals: No HUD production change, new layout decision, rule or command change, package selection beyond the editor-resolved patch, Smart App Control exception or disablement, networking implementation, schema migration, source-artwork change, or work from milestone 8.30g.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830f1-migrate-the-editor-patch-and-reconcile-the-stale-confirm-geometry-gate-completion-evidence).

### [x] 8.30f2 Attach primary replacement flags to their printed title bands

Blocked by: Milestone 8.30f establishes the replacement-ribbon surface and milestone 8.30f1 establishes the validated editor baseline.

Observable outcome: Fast Travel replacing a prepared primary Movement and Clear replacing a prepared primary Attack appear as compact, symmetric card-colour trapezoid flags attached immediately above the centred printed action title, while a Fast Travel replacing a Secondary Movement retains its accepted side-mounted swallowtail; `FAST TRAVEL` uses black lettering in both placements.

Acceptance checks:

- A primary Fast Travel flag is centred over and touches the printed `MOVEMENT` title band rather than following the primary Movement boot's separate hit/highlight bounds. Clear uses the same primary-title geometry over `ATTACK`.
- Both primary flags have equal left and right slopes. The Secondary Fast Travel control retains its established right-notch swallowtail, side placement, size, invocation, hit region and hover behavior.
- `FAST TRAVEL` is black on both primary and Secondary cloth. Clear retains its card-aware ink. Availability, command routing, source artwork and every action/rule contract remain unchanged.
- Focused Edit Mode geometry/style checks, the existing real-input Fast Travel routing journey, and targeted live Game View inspection pass at the approved revision.

Closing checks: `PreparedFastTravelFlag_TouchesAndCentersAboveThePrintedMovementTitle` and `PreparedClearFlag_TouchesAndCentersAboveThePrintedAttackTitle` distinguish primary title tabs from the unchanged Secondary ribbon and pass after the correction.

Non-goals: No Secondary redesign, action eligibility or command change, card/action definition change, source-artwork edit, highlight treatment change, generated-face redesign, board change, networking work, or milestone 8.30g scope.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830f2-attach-primary-replacement-flags-to-their-printed-title-bands-completion-evidence).

### [x] 8.30g Drag an eligible hand card onto the map to commit it facedown

Blocked by: Milestones 8.30f-8.30f2 must establish the final hand, commitment-control, replacement-flag layout and validated editor baseline against which the additional pointer path is tested.

Observable outcome: In addition to the unchanged click-then-confirm path, a player may drag an eligible card from their hand onto the map and release it to commit that exact card facedown immediately, without a separate confirmation press.

Acceptance checks:

- Pressing and moving an eligible hand card beyond a deliberate drag threshold begins one pointer-captured drag whose pointer proxy is the same complete full-size printed face the hand draws. That exact card simultaneously becomes invisible in its hand slot without reflowing the remaining offer; cancellation restores it, while a successful commitment lets the refreshed projection remove it normally. Until the threshold is crossed, the gesture remains the existing click-selection path and cannot commit early. A name-only abstraction, miniature proxy, duplicate face left in the hand, card back, or independently redrawn substitute is not accepted.
- Releasing the dragged card over the live map surface submits the exact standing projected commitment invocation once and immediately reaches the same authoritative facedown state as clicking the card and pressing `CONFIRM FACEDOWN`; the confirmation control is never required or flashed for the successful drag path.
- Releasing outside the map, over HUD chrome, or after the card becomes stale or ineligible cancels cleanly, submits nothing, and clears the proxy and pointer capture. An unavailable card, another seat's card, a card outside the hand, or a hand hidden by an exclusive decision cannot begin a committable drag.
- The drag gesture selects no board space and grants no card-placement or targeting meaning to the drop point. The map is only the bounded drop surface for the existing commitment invocation; rules authority, commitment order, joint reveal, and facedown identity remain unchanged.
- A successful, cancelled, stale, interrupted, or repeated pointer sequence cannot double-submit, leave a selected card or drag proxy behind, retain a callback after session replacement, leak the dragged card to another seat, or block later map input.
- Ordinary clicking, keyboard selection and `CONFIRM FACEDOWN` remain fully usable and retain their current focus order and command path. The drag addition does not remove or silently auto-confirm the accessible non-drag route.
- Focused Edit Mode gesture-state and invocation tests pass, followed by focused Play Mode tests using real UI Toolkit pointer events for successful drop, below-threshold click, off-map cancellation, stale/session-replaced cancellation, and exactly-once submission at 2560x1440 and 2048x1152. One targeted non-batch Game View inspection confirms the drag proxy and map drop boundary are understandable without obscuring the hand or board.
- After 8.30f and 8.30g pass their focused checks, the directly affected 8.30c six-seat commitment, privacy, stale-control, session-replacement, input-handler, and supported-resolution checks rerun green at the corrected revision. This refreshed evidence remains part of the final pre-network gate after 8.30h adds its separately validated board treatment.

Closing checks: `DraggingAnEligibleHandCardOntoTheMap_CommitsItOnceWithoutConfirmation` and `ClickingOrCancellingACardDrag_PreservesTheExistingConfirmationFlow` fail before the additional gesture exists and pass with the two input paths kept distinct. The first also closes the user's post-completion visual finding by requiring the shared complete face, exact prepared artwork when present, and a hidden source slot; the second requires cancellation to restore that slot.

Non-goals: No change to card commitment or reveal rules, no drag-to-reorder hand, no card-to-space targeting, no confirmation removal from click or keyboard input, no touch/mobile gesture scope, no freeform card physics, no networking implementation, and no general animation or input-system rewrite.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830g-drag-an-eligible-hand-card-onto-the-map-to-commit-it-facedown-completion-evidence).

### [ ] 8.30h Illuminate board choices through all six printed hex-corner marks

Blocked by: Milestone 8.30g must complete the new commitment interaction and its affected six-seat revalidation before the separately iterative board-highlight treatment begins.

Observable outcome: Every legal Movement destination and every legal Attack target is made unmistakable by illuminating the six pale printed corner marks of its hex with the same steady layered-shine language as the card-action highlights, using distinct Movement and Attack colours and becoming stronger without moving when pointed at.

Acceptance checks:

- Presentation treats every playable hex as having exactly six pale corner marks, one at each corner, including marks hidden by a unit, token, effect, map art, crop, or current camera view. Their positions come from the existing hex geometry and one shared six-corner template; runtime image reading, OCR, per-space tracing, and a claim that only currently visible marks exist are forbidden.
- Every currently legal Movement destination illuminates all six marks in one Movement-specific colour. The resting state is clearly visible at normal board scale, and pointer hover increases its brightness, contrast, or radiance without translating, rotating, scaling, resizing, pulsing, or changing the six-mark geometry.
- Every currently legal Attack target illuminates all six marks of the target's occupied hex in a clearly different Attack-specific colour. Heroes and minions retain their fitted pieces and pointer surfaces; the shine reads around rather than over the target and invents no target eligibility, range, line of sight, or Attack result.
- Movement and Attack use the same layered construction and intensity hierarchy as each other and the accepted card-action treatment: steady and readable at rest, stronger under the pointer, fixed in place throughout. They do not use a generic whole-hex wash, animated loop, or highlight that jumps when hover begins.
- The distinct hues are not the only statement of meaning: the standing prompt names Movement or Attack, only that decision's projected candidates are lit, and each existing candidate's tooltip/assistive reading retains its action and subject. Colour selection preserves useful contrast over the light, dark, green, stone, and busy portions of the prepared map.
- The milestone deliberately includes bounded hands-on iteration over the Movement hue, Attack hue, resting strength, and hover strength at real gameplay size. Final values are accepted only after targeted non-batch Game View comparison at 2560x1440 and 2048x1152; intermediate palette attempts are implementation work, not new rule or scope decisions.
- Activating a lit destination or target still forwards exactly its existing projected typed invocation once. Unavailable hexes and units remain unlit and inert; stale, cancelled, session-replaced, Undo, restart, and restored states clear or rebuild the six-mark shine solely from the current projection.
- Focused Edit Mode geometry/material tests prove six marks per highlighted hex, stable rest/hover bounds, separate Movement/Attack treatments, and no presentation-owned eligibility. Focused Play Mode pointer journeys cover a Movement destination, hero Attack target, minion Attack target, stale clearing, and repeated rebuild without duplicate handlers or monotonically growing owned materials. The directly affected 8.30c board-choice, six-seat input, resource-lifetime, and supported-resolution checks rerun green at the final signed-off treatment before 8.31.

Closing checks: `EveryLegalBoardChoice_LightsSixFixedHexCornerMarksFromProjectedGeometry` and `MovementAndAttackHighlights_StrengthenInPlaceWithDistinctSignedOffColours` fail against the pre-milestone board treatment and pass after the final visual iteration.

Non-goals: No movement, range, line-of-sight, Attack, targeting, occupancy, or map-data rule change; no runtime pixel analysis, source-map repaint, whole-hex wash, pulsing or looping animation, card-highlight redesign, unit-art change, networking implementation, or general board/presentation overhaul.

### [ ] 8.30i Read Defense participation and its breakdown through the corrected shine language

Blocked by: Milestone 8.30h must deliver the shared six-corner template, the layered steady/stronger shine construction, and its signed-off Movement and Attack hues, which the Defense treatment reuses and must remain clearly distinct from.

Observable outcome: A minion contributing to a projected Defense is read from the board itself - all six printed corner marks of its occupied hex lit in one of two contribution colours, green for +1 and red for -1, replacing the floating text badge - and hovering a Defense offer opens a structured, themed breakdown panel whose contribution rows, totals, and result are readable at a glance instead of one undifferentiated text block.

Acceptance checks:

- Every minion the projection marks with a +1 or -1 Defense contribution lights all six corner marks of its occupied hex through milestone 8.30h's shared six-corner template and layered construction, in two contribution-specific colours - a green family for +1 and a red family for -1 - clearly distinct from the signed-off Movement and Attack hues at real board scale. The `StandardMinionView` floating "+1"/"-1" text badge and its oval text-glyph backing are retired by milestone 8.30i, and tests assert the retirement.
- The corner shine states participation only: contribution amounts, Defense eligibility, adjacency, and resolution rules are unchanged, and each contribution's existing assistive reading keeps its amount and source. A minion simultaneously carrying a fitted target ring keeps it, and one targeted inspection confirms the two readings stay distinguishable on the same hex.
- The Defense breakdown view model carries the typed attack and Defense contribution rows, both totals, the block result, and the outcome the presenter already receives as typed projections, rather than one pre-joined string; the flattened text reading remains available wherever a check or assistive surface consumes it today.
- The hover preview draws the breakdown as themed sections: one row per contribution with its signed amount coloured by sign, emphasized attack and Defense totals, and a result statement visually distinct between defended, attack-succeeds, and defeated. Its anchor-clear inside-viewport placement, hover trigger, dismissal timing, and reference-unit geometry contract are unchanged.
- The milestone deliberately includes bounded hands-on iteration over the two contribution hues, resting and hover strengths, and the breakdown panel's treatment at real gameplay size; final values are accepted only after targeted non-batch Game View comparison at 2560x1440 and 2048x1152, with intermediate attempts remaining implementation work rather than new scope decisions.
- Focused Edit Mode geometry/material and view-model tests plus focused Play Mode Defense journeys pass at both supported resolutions, and the directly affected 8.30c six-seat Defense, board-cue, resource-lifetime, and supported-resolution checks rerun green at the final signed-off treatment before 8.31.

Closing checks: `MinionDefenseContributions_LightSixHexCornerMarksInSignedContributionColours` and `TheDefenseBreakdownPreview_DrawsTypedContributionRowsTotalsAndOutcome` fail against the pre-milestone badge and flattened-text treatments and pass after the final visual iteration.

Non-goals: No Defense rule, contribution-amount, timing, or resolution change; no change to which offers show a preview or when it appears and dismisses; no whole-hex wash, pulsing or looping animation, Movement/Attack hue redesign, networking implementation, or general board or HUD overhaul.
