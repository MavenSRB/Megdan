# Phase 8 - Late HUD and card-interaction corrections

Canonical active roadmap shard for the player-requested HUD and card-interaction corrections found after milestone 8.30d and before the complete six-seat local validation gate. Return to [ROADMAP.md](../ROADMAP.md) or the surrounding [Phase 8 route](PHASE_8.md).

The user approved defining and scheduling these milestones on 2026-08-30. That roadmap amendment does not authorize their implementation: each milestone still requires its own explicit approval under the roadmap execution rule. Milestone 8.30c was already being implemented when these corrections were collected, so 8.30f follows it, 8.30g follows 8.30f, and the separately iterative board treatment at 8.30h follows the completed card interaction. Further user-approved amendments on 2026-08-31 define milestone 8.30i, which reuses 8.30h's shared six-corner construction. Corrective 8.30i1 then gives every prepared hero-token face a white interior field while preserving its portrait and team rings on both the board and hero rails. User-directed corrective 8.30i2 removes the generic effect-choice confirmation/debug controls and keeps every spatial select/deselect on the map. Corrective 8.30i3 keeps the ordinary hand visible when a target advances into an optional step and places its `ACCEPT`/`DECLINE` controls above those cards in the facedown-submit banner treatment. Milestone 8.30j strips the Upgrade-a-Card choice down to its cards and centred gains facts, milestone 8.30k replaces the hero-plaque item-channel abbreviations with the established printed symbols, milestone 8.30l moves non-resolving unavailable cards out of the playable hand into a right-side HUD group that matches the current hand presentation and retains unavailable dimming, and milestone 8.30m gives every rendered card a consistent thick black exterior outline. The 2026-09-01 amendment then adds milestone 8.30n so tied teammates publicly agree who acts first before networking carries that decision. Milestone 8.30n carries the final pre-network six-seat evidence refresh. Each correction reruns the six-seat checks it affects before private-online work begins.

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

### [x] 8.30h Illuminate board choices through all six printed hex-corner marks

Blocked by: Milestone 8.30g must complete the new commitment interaction and its affected six-seat revalidation before the separately iterative board-highlight treatment begins.

Observable outcome: With the user's live revisions, every board-valued choice is made unmistakable through the same steady six-corner layered-shine language and the old whole-hex fills and fitted target rings are retired from configured play. Movement, Place and board placement destinations use green; Hero Respawn uses cyan; Attack and removal use blood red; teammate actions use warm ivory; and other effects use neutral grey; each becomes stronger without moving when pointed at.

Acceptance checks:

- Presentation treats every playable hex as having exactly six pale corner marks, one at each corner, including marks hidden by a unit, token, effect, map art, crop, or current camera view. Their positions come from the existing hex geometry and one shared six-corner template; runtime image reading, OCR, per-space tracing, and a claim that only currently visible marks exist are forbidden.
- Every currently legal Movement destination and every Fast Travel destination exposed after explicitly selecting that replacement illuminates all six marks in strong green. Hero Respawn destinations and its pointed-at prompt option use cyan. The resting state is clearly visible at normal board scale, and pointer hover increases strength without translating, rotating, scaling, resizing, pulsing, or changing geometry.
- Every legal Attack target, including a target selected by before-Attack text, Minion Battle removal, Clear target, and other projected removal choice illuminates all six marks of its occupied hex in blood red. Every Move, Place, Spawn or token-Place destination, correction destination and board-valued placement transition uses Movement green. A teammate target uses the accepted warm ivory; every other non-destructive effect target uses neutral grey; and an effect's non-selectable projected resulting space uses a darker neutral. Hero, minion and token identities resolve only through projected occupancy to place the cue; their ordinary pieces and pointer surfaces remain.
- All families use the same layered construction and intensity hierarchy: steady and readable at rest, stronger under the pointer, fixed in place throughout. Configured play activates no generic whole-hex fill/rim or fitted target ring/wash, and composition allocates none of the retired fill/rim materials.
- The hues are not the only statement of meaning: the standing prompt names the decision, only that decision's projected candidates are marked, and each candidate's tooltip or assistive reading retains its action and subject. Colour selection preserves useful contrast over the light, dark, green, stone, and busy portions of the prepared map.
- The milestone deliberately includes bounded hands-on iteration over all four colour families, resting strength, and hover strength at real gameplay size. Final values are accepted only after targeted non-batch Game View comparison at 2560x1440 and 2048x1152; intermediate palette attempts are implementation work, not new rule or scope decisions.
- Activating a lit destination or target still forwards exactly its existing projected typed invocation once. Unavailable hexes and units remain unlit and inert; stale, cancelled, session-replaced, Undo, restart, and restored states clear or rebuild the six-mark shine solely from the current projection.
- Focused Edit Mode geometry/material tests prove six marks per highlighted hex, stable rest/hover bounds, every signed-off colour family, and no presentation-owned eligibility. Focused Play Mode pointer journeys cover every board-choice family, stale clearing, and repeated rebuild without duplicate handlers or growing owned resources. The directly affected 8.30c board-choice, six-seat input, resource-lifetime, and supported-resolution checks rerun green at the final signed-off treatment before 8.31.

Closing checks: `EveryLegalBoardChoice_LightsSixFixedHexCornerMarksFromProjectedGeometry`, `EffectsAndRemoval_UseNeutralAndBloodRedCornersWithoutLegacyMarks`, and `MovementAndAttackHighlights_StrengthenInPlaceWithDistinctSignedOffColours` fail against the pre-milestone board treatment and pass after the final visual iteration.

Non-goals: No movement, range, line-of-sight, Attack, targeting, occupancy, or map-data rule change; no runtime pixel analysis, source-map repaint, whole-hex wash, pulsing or looping animation, card-highlight redesign, unit-art change, networking implementation, or general board/presentation overhaul.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830h-illuminate-board-choices-through-all-six-printed-hex-corner-marks-completion-evidence).


### [x] 8.30i Read Defense participation and its breakdown through the corrected shine language

Blocked by: Milestone 8.30h must deliver the shared six-corner template, layered steady/stronger construction, and signed-off board-choice hues; the Defense treatment reuses that construction while remaining non-looping and clearly distinct.

Observable outcome: A minion contributing to a projected Defense is read from the board itself - all six printed corner marks of its occupied hex lit in one of two contribution colours, green for +1 and red for -1, replacing the floating text badge. During Attack-target selection, including a before-Attack effect choice whose typed consequence is Attack such as Brogan's Charge, hovering a legal hero previews that candidate's already-determined minion contributions through the same marks while retaining the target's blood-red Attack corners. After a target is chosen, the Defense choices stand without their redundant labelled box or per-card option frames, `DECLINE DEFENSE` uses the established commitment-confirmation banner, and either clicking a card or dragging it onto the live board selects that exact projected offer. Hovering an offer opens a structured, themed breakdown panel whose contribution rows, totals, and result are readable at a glance instead of one undifferentiated text block.

Acceptance checks:

- Every minion the projection marks with a +1 or -1 Defense contribution lights all six corner marks of its occupied hex through milestone 8.30h's shared six-corner template and layered construction, in two contribution-specific colours - a green family for +1 and a red family for -1 - clearly distinct from the signed-off Movement and Attack hues at real board scale. The `StandardMinionView` floating "+1"/"-1" text badge and its oval text-glyph backing are retired by milestone 8.30i, and tests assert the retirement.
- While an ordinary Attack target choice or an effect candidate explicitly projected with typed Attack intent stands, hovering a legal hero asks an authority-owned per-target projection for the minion contributions that would apply if that hero were selected and lights those minions through the same green/red six-corner language. This includes Brogan's Charge without adding a hero/card/capability-name branch and excludes unrelated hero-target effects. The candidate hero keeps its blood-red Attack corners; leaving it clears only the speculative contribution marks; hovering a minion target produces no Defense preview; and presentation does not rederive adjacency, team, eligibility, or contribution amounts from board state.
- The corner shine states participation only: contribution amounts, Defense eligibility, adjacency, and resolution rules are unchanged, and each contribution's existing assistive reading keeps its amount and source. A minion simultaneously carrying a board-choice corner cue receives a deliberate corner-only coexistence treatment; milestone 8.30i may not restore the retired fitted ring or whole-hex fill, and one targeted inspection confirms both readings remain distinguishable.
- The Defense breakdown view model carries the typed attack and Defense contribution rows, both totals, the block result, and the outcome the presenter already receives as typed projections, rather than one pre-joined string; the flattened text reading remains available wherever a check or assistive surface consumes it today.
- Defense card choices retain their established projected order and complete shared card faces but draw no `DEFENSE` gutter, surrounding option plate, inset, or selection frame. They use the ordinary playable hand's exact compact width, overlapping slot step, available-card raise, and pointer-transparent whole-face hover replacement, which retains the offer's typed Defense shine while replacing the overlapped source. `DECLINE DEFENSE` is removed from the card fan and hosted in the exact 300-by-44 lane below `DIRECTOR` used by `CONFIRM FACEDOWN`, with that control's strong-shine pewter banner language, while remaining the projected decline invocation rather than a card.
- A legal Defense card remains clickable and may also be dragged by its complete face onto the exact live board surface accepted by facedown commitment. Crossing the same drag threshold hides only the source face and raises the same complete-card reading under the pointer; a valid release forwards that offer's exact typed invocation once, while an off-board release, cancellation, stale rebuild, or second release submits nothing and restores the source surface.
- The hover preview draws the breakdown as themed sections: one row per contribution with its signed amount coloured by sign, emphasized attack and Defense totals, and a result statement visually distinct between defended, attack-succeeds, and defeated. By the user's live revision, it anchors to the complete visible Defense-card fan, aligns with the fan's top edge and opens eight reference units beyond its right edge; a constrained viewport alone moves it to the fan's left. It remains card-clear and inside the viewport, while its hover trigger, dismissal timing, and reference-unit coordinate contract are unchanged.
- The milestone deliberately includes bounded hands-on iteration over the two contribution hues, resting and hover strengths, and the breakdown panel's treatment at real gameplay size; final values are accepted only after targeted non-batch Game View comparison at 2560x1440 and 2048x1152, with intermediate attempts remaining implementation work rather than new scope decisions.
- Focused Edit Mode geometry/material and view-model tests plus focused Play Mode Defense journeys pass at both supported resolutions, and the directly affected 8.30c six-seat Defense, board-cue, resource-lifetime, and supported-resolution checks rerun green at the final signed-off treatment before 8.31.

Closing checks: `MinionDefenseContributions_LightSixHexCornerMarksInSignedContributionColours`, `HoveringALegalHeroAttackTarget_PreviewsItsAuthorityProjectedMinionDefenseContributions`, `TheDefenseBreakdownPreview_DrawsTypedContributionRowsTotalsAndOutcome`, `DraggingADefenseCardOntoTheMap_SubmitsThatExactProjectedOfferOnce`, and `DeclineDefenseUsesTheFacedownConfirmationPosition` fail against the pre-milestone badge, absent pre-target preview, flattened-text treatment, and click-only framed Defense row with an inline decline and pass after the final visual iteration.

Non-goals: No Defense rule, eligibility, contribution-amount, timing, command, or resolution change; no speculative Defense-card total or outcome before the defender chooses a card; no change to which Defense offers show a preview or when that later preview appears and dismisses; no whole-hex wash, pulsing or looping animation, Movement/Attack hue redesign, networking implementation, or general board or HUD overhaul.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830i-read-defense-participation-and-its-breakdown-through-the-corrected-shine-language-completion-evidence).

### [x] 8.30i1 Give prepared hero tokens a white interior background

Blocked by: Milestone 8.30i must complete the preceding Defense presentation correction before this independent token-art preparation change begins.

Observable outcome: Every configured hero's prepared official token face places the portrait over a clean white interior field inside the retained official team-coloured ring; the shared prepared texture makes the same treatment visible on the board piece and in that hero's overview rail.

Acceptance checks:

- `Tools/Prepare-HeroTokens.ps1` deterministically replaces only the source token's flat interior background with opaque white for all six configured heroes and both authoritative team variants. It preserves the portrait, the official orange or light-blue team ring, the circular antialiasing, the declared 302x302 dimensions, and the existing content-ID/team resource paths.
- Background separation is derived off-line from the declared source face and guarded by explicit measurements. The preparation route stops without writing when it cannot distinguish the interior field from the portrait or team ring; it does not rely on hero-specific painted masks, runtime pixel reading, OCR, generated replacement art, or a hero-name branch beyond the existing declared source lookup.
- The board's generated outer Red/Blue rim remains unchanged, and the HUD's existing medallion, geometry, tooltip, assistive reading, and hero name remain unchanged. White is only the official face's interior background; it does not erase either team ring or become a new team signal.
- The same prepared resource is still consumed by `PrimitiveHeroView` and the hero-rail portrait, so each configured hero shows one matching white-background face in both places without duplicate assets, presentation-side recolouring, new runtime materials, or changes to identity selection and fallback behavior.
- The private source corpus and prepared output remain ignored, local-only reference material under the standing rights boundary. A clean checkout still carries no official token art and retains the generated-prism/empty-portrait fallback without an error or bundled substitute.
- Focused preparation-tool checks prove all twelve outputs have the declared size, transparent exterior, opaque white interior background samples, retained team-ring classification, and source-faithful non-background portrait samples. Focused Edit Mode resource-addressing/fallback checks and Play Mode board-plus-rail checks pass; targeted Game View inspection at 2560x1440 and 2048x1152 covers all six heroes across both teams.

Closing check: `PreparedHeroTokens_UseWhiteInteriorFieldsWhilePreservingPortraitsTeamRingsAndSharedRuntimeAddressing` fails against the source-coloured interior fields and passes when every configured board token and rail portrait consumes the correctly prepared white-background face.

Non-goals: No hero, team, setup, rule, command, projection, snapshot, replay, Undo, token size, portrait crop, board rim, HUD medallion, hero-plaque layout, source-corpus, source-art identity, runtime shader, generic image-processing framework, networking, or broader board/HUD redesign; no tracked publication or distribution of the private token artwork.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830i1-give-prepared-hero-tokens-a-white-interior-background-completion-evidence).

### [x] 8.30i2 Remove generic effect-choice confirmation buttons and keep spatial choices on the map

Blocked by: Corrective 8.30i1 must complete the shared prepared-token treatment before this independently requested interaction correction begins.

Observable outcome: A player never sees `CONFIRM CHOICE` or a generic `UNDO OPTION` for an effect decision. Picking the last allowed candidate completes the choice immediately, while every candidate that names a map piece or space is selected and deselected by clicking that map subject.

Acceptance checks:

- A select that fills the authority-projected maximum forwards that exact select and its newly projected confirm as one player action, for modes, cards, numbers, supply pieces, targets, and destinations. A choice whose maximum is not yet filled stays open.
- A variable or partially filled choice retains one production `DONE` action; a confirm-or-decline optional step reads `ACCEPT`/`DECLINE`. No runtime UI source contains `CONFIRM CHOICE`.
- A selected spatial effect candidate remains represented by its selected board cue and exact deselect invocation. Clicking it again submits that deselect; no off-map `UNDO OPTION` control is created. Empty destinations and an occupied zero-distance `up to` destination remain map answers.
- Non-spatial candidates retain their established complete-card, printed-number, supply-piece, or mode surfaces. Rules authority, bounds, commands, selection order, snapshot/replay/Undo state, and stale-command rejection are unchanged.
- Focused Edit Mode presenter/status/map-toggle regressions and focused Play Mode mode, token, Push/Place, Active-effect, and two-map-click pair journeys pass.

Closing checks: `RoundPresenter_ForwardsAPausedEffectFramesModesAndWithholdsEveryOtherControl` fails while a mode still exposes the generic second confirmation; `BulletHell_SelectedMapTargetStaysOnTheMapAndCreatesNoDebugDeselectControl` fails while a selected map piece creates an off-map deselect control; both pass after the correction.

Non-goals: No effect, card, target, displacement, selection-bound, confirmation-command, pending-choice, snapshot, replay, Undo, map-cue colour/geometry, Minion-Battle, Clear, facedown commitment, Defense, system-transition, networking, or broader HUD redesign.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830i2-remove-generic-effect-choice-confirmation-buttons-and-keep-spatial-choices-on-the-map-completion-evidence).

### [x] 8.30i3 Keep the hand visible beneath optional-step banners

Blocked by: Corrective 8.30i2 must establish the final `ACCEPT`/`DECLINE` optional-step commands and map-first selection behavior before this independently requested layout correction begins.

Observable outcome: When choosing a target advances play into a confirm-or-decline optional step, the viewed hero's ordinary card hand remains visible in its established bottom-left position, while `ACCEPT` and `DECLINE` appear immediately above those cards as the same pewter banner family used by `CONFIRM FACEDOWN` instead of replacing the hand with small generic buttons.

Acceptance checks:

- Every confirm-or-decline effect pause renders exactly one `ACCEPT` and one `DECLINE` control in projected order. Both forward only their existing typed invocations once; stale, rebuilt, session-replaced, restored, replayed, Undo, or restarted controls submit nothing.
- The two controls use the shared `PewterBanner` construction, height, typography, cloth silhouette, edge and pointer/focus shine language already used by `CONFIRM FACEDOWN`. Neither answer is reduced to the current generic rectangular action button, and colour or shine alone does not distinguish their labels or meaning.
- The banner row sits immediately above the ordinary bottom-left card row. It covers no card face or exposed selection strip, does not shift or resize a card, does not move the hero rails, `DIRECTOR`, narration, or board-safe surface, and remains contained at 2560x1440 and 2048x1152.
- The currently viewed hero's ordinary tray cards remain rendered in their existing order and complete-card presentation throughout the optional step, including the resolving card and every unavailable/active-effect treatment. Entering or leaving the pause, choosing either answer, rebuild, restore, replay, Undo, restart, and session replacement create no missing, duplicated, or orphaned card surface.
- The retained cards are context only while the optional step stands: they cannot submit commitment, selection, drag, printed-action, or other card commands, and another seat's hidden card identity remains absent. Other exclusive contexts - Defense, removal prevention, system transitions, Level Up, card-valued effect choices, and minion choices - retain their established dedicated surfaces.
- Focused Edit Mode hierarchy, style, geometry, invocation, hidden-information, and rebuild checks pass, followed by focused Play Mode target-to-optional-step journeys through both `ACCEPT` and `DECLINE` at the two validated desktop resolutions. Targeted non-batch Game View inspection confirms the banners sit above a stable visible hand and match `CONFIRM FACEDOWN`; directly affected 8.30c optional-choice, six-seat input, stale-control, supported-resolution, and resource-lifetime checks rerun green.

Closing checks: `OptionalStepBanners_StandAboveTheVisibleOrdinaryHandInTheFacedownSubmitTreatment` fails while the exclusive context hides the cards and draws generic buttons, then passes after the corrected composition; `OptionalStepHand_RemainsNonActionableAndRebuildsWithoutMissingOrDuplicateCards` closes the retained-hand input, privacy, and lifecycle boundary.

Non-goals: No effect, optionality, targeting, selection bound, command, pending-choice, card availability, lifecycle, ordering, hidden-information, snapshot, replay, Undo, or stale-command change; no change to spatial map selection/deselection, `DONE`, `CONFIRM FACEDOWN`, Defense, removal prevention, system transitions, Level Up, card-valued effect choices, minion choices, card faces, card footprint, hero rails, networking, animation, or broader HUD redesign.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830i3-keep-the-hand-visible-beneath-optional-step-banners-completion-evidence).

### [ ] 8.30j Strip the Upgrade-a-Card choice down to cards and centred gains

Blocked by: Corrective 8.30i3 must complete the optional-step hand-and-banner correction before this separately reviewable Upgrade-a-Card layout correction begins.

Observable outcome: p.15's Upgrade-a-Card choice shows the six complete candidate cards directly, with no enclosing comparison box, Blue/Green/Red pair captions, pair outlines, or individual option frames; each card's existing `GAINS` item fact is centred immediately below that card.

Acceptance checks:

- The choice retains exactly six standard-footprint whole-card controls in the established left-to-right order: the two projected candidates for the Blue source card, then Green, then Red, with each source card's two candidates retaining their projected option order. Pairing remains a layout/authority fact but has no visible `BLUE PAIR`, `GREEN PAIR`, or `RED PAIR` caption and no coloured or neutral pair container.
- The opaque full-width comparison band and every per-option background, padding plate, resting border, selection border, and enclosing outline shown in the reported layout are absent. Only the complete card faces and their gains rows are visible in the option area; transparent layout elements may preserve spacing and grouping but may not paint a box. The printed border already present in source card artwork is not altered.
- The whole card remains the exact pointer and keyboard control for its existing projected invocation. Hover, focus, and activation remain perceivable without adding an enclosing option frame or moving, resizing, cropping, recolouring, or replacing the face; exactly one choice is submitted and stale or unavailable controls remain inert.
- Each candidate's `GAINS` label and its existing signed item chip, or `NO ITEM` fallback, form one compact row centred horizontally beneath that candidate's card. The row does not align to the left edge of an obsolete option frame, widen the card, or change which sibling card supplies the p.16 item fact; returned and set-aside card facts remain in hover and assistive text.
- All six cards and gains rows fit without horizontal scrolling at 2560x1440 and 2048x1152, retain the standing bounded vertical treatment on a deliberately short supported view, and move neither the hero rails, `DIRECTOR`, nor the board-safe surface.
- Focused Edit Mode hierarchy, geometry, ordering, and invocation checks plus focused Play Mode pointer and keyboard Upgrade-a-Card journeys pass at both validated desktop resolutions. A targeted non-batch Game View inspection confirms that no comparison, pair, or option outline remains and that every gains row is visually centred. The directly affected 8.30c Upgrade-a-Card, six-seat input, stale-control, and supported-resolution checks rerun green at the corrected revision; milestone 8.30l owns the final affected refresh before 8.31.

Closing check: `UpgradeChoices_ShowOnlySixCardsWithCenteredGainsAndNoComparisonFrames` fails against the reported comparison-band, labelled-pair, outlined-option layout and passes after the correction.

Non-goals: No Level Up, card progression, item, eligibility, option-pairing, colour-order, command, snapshot, replay, Undo, or hidden-information change; no card-face, card-footprint, source-artwork, ordinary tray, Defense-choice, general contextual-region, board, networking, animation, or broader HUD redesign.

### [ ] 8.30k Replace hero-plaque stat abbreviations with printed channel symbols

Blocked by: Milestone 8.30j must complete the preceding Upgrade-a-Card correction before this independent hero-plaque readability change begins.

Observable outcome: Every signed item bonus in a hero plaque is shown as the established printed channel symbol followed by its signed value, replacing text abbreviations such as `INI`, `ATK`, and `DEF` while keeping the compact strip and its exact projected totals.

Acceptance checks:

- Initiative, Attack, Defense, Movement, Range, and Radius item bonuses use the project's existing matching printed symbols wherever those assets already exist. Any missing channel symbol is reproduced from the second-printing rulebook/card icon vocabulary rather than invented, renamed, or substituted with an unrelated generic icon; one shared typed channel-to-symbol catalog owns the mapping and no hero or card name selects an asset.
- Each item entry draws exactly one symbol and one signed projected value, with no visible `INI`, `ATK`, `DEF`, `MOV`, `RNG`, or `RAD` token. The sign, magnitude, ordering, composition of multiple items, and distinction between `NO ITEMS` and one or more bonuses remain unchanged.
- The icon and its value are optically centred as one unit inside the existing plaque item strip at one- and two-digit values. Symbols retain a consistent visual height, do not collide with adjacent item entries or the coin disc, do not enlarge the plaque or rail, and remain legible over both team treatments at 2560x1440 and 2048x1152.
- Colour is not the sole channel distinction. Every entry's tooltip and assistive reading names the full channel and signed value, keyboard/focus behavior is unchanged, and the icon resource has a stable non-text identity suitable for tests.
- Prepared card artwork and rulebook pages remain reference sources only: the milestone neither modifies source artwork nor performs runtime pixel reading, OCR, or printed-text parsing. Generated and configured HUD paths consume the same typed symbol catalog.
- Focused Edit Mode mapping, hierarchy, geometry, and assistive-reading checks plus focused Play Mode hero-plaque checks pass at both validated desktop resolutions. A targeted non-batch Game View inspection covers at least Initiative, Attack, and Defense bonuses, a plaque with multiple channels, and coexistence with the coin disc.

Closing check: `HeroPlaqueItemBonuses_UsePrintedChannelSymbolsWithSignedValuesAndFullAssistiveNames` fails against the abbreviation-based item strip and passes when every supported channel uses the shared symbol mapping without changing its projected amount.

Non-goals: No item, progression, bonus-composition, coin, card-definition, hero, rule, command, snapshot, replay, rail-width, plaque-height, source-artwork, card-face, networking, or general HUD redesign; no replacement of full channel names in tooltips, assistive readings, rules explanations, tests that intentionally describe content, or non-plaque surfaces.

### [ ] 8.30l Move unavailable cards from the playable hand into a right-side HUD group

Blocked by: Milestone 8.30k must complete the preceding hero-plaque symbol correction before this separately reviewable hand-composition change begins.

Observable outcome: Cards that the current projection marks unavailable and that are not the card presently resolving no longer occupy the bottom playable hand; they appear as a separate ordered group on the right side, rendered with the same complete-card layout as the current hand and the same unavailable dimming they already carry there, while playable cards and the resolving card remain in the established bottom hand.

Acceptance checks:

- The current projection alone classifies cards. Every non-resolving tray card in a played, discarded, or otherwise unavailable lifecycle group moves to the right-side unavailable-card group; every selectable card remains in the bottom hand, and the one resolving card remains there at full strength even though it carries no commitment command. Presentation does not infer availability from colour, round number, card text, hero identity, or prior UI state.
- The unavailable-card group is anchored on the right side of the HUD and uses the current hand's complete-card footprint, compact overlap, exposed-strip targeting, and whole-face hover/focus presentation rather than a new miniature, icon, pile, or plaque treatment. At 2560x1440 and 2048x1152 it covers no hero plaque, `DIRECTOR` control or drawer, commitment confirmation, narration, contextual decision, playable card, hover preview, or board-safe surface, and it does not move the hero rails or map.
- Unavailable cards retain the shared complete card face, source artwork when configured, stable card identity, lifecycle tooltip/assistive reading, active-effect-source treatment, existing relative lifecycle/order contract, and the same opaque local unavailable veil used while they are in the current hand. Pointing or keyboard focus exposes the addressed complete face without reordering siblings, changing its standard footprint, removing or weakening that dimming, or making it actionable.
- Cards move between the bottom hand and right-side unavailable-card group only when a refreshed projection changes their resolving/selectable/lifecycle facts. Commitment, reveal, discard, Defense discard, turn advance, round refresh, restore, replay, Undo, restart, and session replacement leave no duplicate, missing card, stale callback, or stranded hover/drag state.
- The right-side unavailable-card group cannot begin a commitment drag or submit any command. Its ordering remains deterministic and its focus/assistive order matches the visible order; another seat's hidden card identity is never exposed.
- When no card qualifies, the right-side group paints no empty frame, label, or placeholder. When the supported maximum qualifies, every card remains reachable without horizontal page scrolling or intrusion into the map; a deliberately short supported viewport uses a bounded local fallback without relocating the playable hand.
- Focused Edit Mode classification, ordering, hierarchy, geometry, hand-presentation parity, unavailable-dimming, hidden-information, and exactly-one-surface checks plus focused Play Mode commitment, reveal, Defense-discard, turn-advance, Undo/restart, stale/session-replacement, pointer, keyboard, and drag-cancellation journeys pass at both validated desktop resolutions. Targeted non-batch Game View inspection confirms the requested right-side placement, parity with the current hand presentation, retained unavailable dimming, and increased clarity of the bottom playable hand. The directly affected 8.30c six-seat commitment, privacy, card-lifecycle, stale-control, input-handler, resource-lifetime, and supported-resolution checks rerun green at the corrected revision; milestone 8.30m owns the final affected refresh before 8.31.

Closing checks: `UnavailableNonResolvingCards_MoveToTheRightSideInTheCurrentHandPresentationWhileThePlayableHandKeepsOnlyLiveCards` and `UnavailableCards_RetainDimmingAndRebuildWithoutDuplicatesCommandsOrHiddenIdentityLeaks` fail against the mixed bottom hand and pass after the two presentation groups are projection-driven, visually consistent, and lifecycle-safe.

Non-goals: No card availability, commitment, reveal, discard, Defense, lifecycle, turn, round, hidden-information, command, snapshot, replay, Undo, restart, or card-order rule change; no card-footprint or source-artwork change, hand reordering by drag, new discard pile authority, map movement, hero-rail redesign, contextual-region redesign, networking implementation, animation system, or general HUD overhaul.

### [ ] 8.30m Give every in-game card a thick black exterior outline

Blocked by: Milestone 8.30l must establish the final playable-hand, resolving-card, and right-side unavailable-card composition before this shared card-edge treatment is applied and validated across every card surface.

Observable outcome: Every complete card shown in the match has one clean, thick black outline following its exterior silhouette and rounded corners, so cards remain clearly separated from the board, HUD, neighbouring cards, and contextual backgrounds in every lifecycle and interaction state.

Acceptance checks:

- One shared card-surface treatment draws a solid black perimeter outside the existing card face. The outline follows the complete standard `CardFootprint`, including its rounded corners, with one consistent visual weight at the standard footprint and a deliberately scaled equivalent for existing compact card renderings; it never crosses, covers, recolours, or replaces printed artwork, text, symbols, source borders, or generated-face content.
- The treatment covers every configured complete-card presentation in the match: prepared and generated fronts in the playable hand, the resolving-card surface, milestone 8.30l's right-side unavailable-card group, ordinary hover previews, the commitment drag proxy, Defense choices and previews, Upgrade-a-Card candidates, current/played complete-card displays, and every visible facedown card back. A finite presenter inventory names each surface and proves that no card-sized duplicate or alternate renderer bypasses the shared outline.
- Prepared source artwork and generated fallback faces receive the same exterior silhouette without modifying either asset. The outline is presentation geometry outside the content rectangle, not a painted source-image mutation, texture-processing step, runtime pixel-reading path, OCR result, per-card mask, hero/card-name branch, or second inner border.
- The outer edge remains fully visible without changing the established card content dimensions, aspect ratio, source crop, hit region, pointer capture, keyboard order, spacing contract, or invocation. Layouts reserve the outline deliberately so neighbouring cards, viewport clipping, the right-side unavailable-card overlap, the six Upgrade candidates, and screen edges do not cut it off at 2560x1440 or 2048x1152.
- Hover, focus, selection, availability, resolving, active-effect-source, dragging, facedown, discarded, and spent states retain their existing meanings and visual treatments. Those states may coexist with the black perimeter but cannot replace it, make it pulse, change its thickness, move it, or produce a second outline; the perimeter itself carries no availability, ownership, team, card-colour, or lifecycle meaning.
- Projection rebuild, reveal, commitment, Defense discard, Level Up, turn advance, restore, replay, Undo, restart, session replacement, drag cancellation, and view disposal leave exactly one perimeter per visible card and no orphan geometry, stale callback, retained view, hidden-identity leak, or growing owned resource.
- Focused Edit Mode inventory, hierarchy, geometry, clipping, prepared/fallback parity, and exactly-one-outline checks pass, followed by focused Play Mode journeys covering hand, resolving, spent, hover, drag, facedown, Defense, and Upgrade surfaces at both validated desktop resolutions. Targeted non-batch Game View inspection confirms the signed-off thickness and clean rounded silhouette over light, dark, busy, overlapping, and board-adjacent backgrounds. The directly affected 8.30c six-seat card-lifecycle, commitment, privacy, stale-control, input-handler, resource-lifetime, and supported-resolution checks rerun green at the final revision before 8.31.

Closing checks: `EveryCompleteCardSurface_UsesOneSharedThickBlackExteriorOutline` and `CardOutline_PreservesContentGeometryInputAndLifecycleAcrossEveryPresenter` fail against the current mixed edge treatments and pass when the finite surface inventory shares one unclipped, invariant perimeter through all tested states.

Non-goals: No card rule, definition, availability, lifecycle, ordering, commitment, Defense, Level Up, command, snapshot, replay, Undo, hidden-information, card content, source-artwork, generated-face anatomy, card-footprint, hover-preview, drag, spent-group, or Upgrade layout change; no runtime image processing, private-asset publication, networking implementation, new accessibility/settings suite, or broader HUD redesign.

### [ ] 8.30n Require public teammate agreement for tied Initiative

Blocked by: Milestone 8.30m must complete the shared card-surface correction and its affected six-seat evidence before this separately reviewable team-decision correction begins.

Context: Rulebook p.7 requires tied players on the same team to decide who acts first and p.4 permits them, after reveal, to state preferences but not discuss their rationale; those statements are table talk and therefore public to the opposing team. The current authority correctly pauses on a team-owned `PendingInitiativeChoice`, preserves the Tie Breaker rules, and validates the final candidate, but every seat on the owning team currently receives the final `ChooseTiedActor` command. The first accepted teammate submission therefore decides the order without demonstrating the required agreement. This milestone corrects that local six-seat flow before the network protocol is built around it.

Observable outcome: Whenever two or more eligible teammates are tied for the next Initiative, each tied player can publicly state or revise which tied hero they prefer to act first, every seat can see the preference-only state, and the authoritative actor is selected only after all tied players currently name the same candidate.

Acceptance checks:

- The existing Initiative authority remains the sole source of the owning side, current Initiative total, candidate card identities, cross-team Tie Breaker condition, final selected actor, and deferred coin flip. A transport-neutral Application/host agreement coordinator consumes that immutable pending choice and submits its existing `ChooseTiedActor` command exactly once only after agreement; presentation neither computes candidates nor selects an actor.
- Eligible preference authors are exactly the distinct owners of the pending candidate cards, not every member of the owning team. A tied player may prefer any current candidate and may replace their own latest preference while the same pending choice stands. A non-tied teammate may observe but cannot state or alter a preference; an opponent, stale seat, duplicate/replayed submission, forged hero/team identity, or candidate outside the current choice is refused without changing agreement state.
- The preference statement is deliberately bounded to `I prefer <candidate> first`. It carries no free-form text, reason, card-in-hand data, private note, chat channel, captain role, priority, veto, or hidden team-only field. Candidate cards are already revealed; every seat sees which eligible tied players have or have not stated a preference and the candidate named by each submitted preference, matching p.4's public table-talk boundary without exposing any unrevealed card.
- Agreement exists only when every currently eligible tied player has a latest preference and all those preferences name the same current candidate. The host then submits the final choice once and clears the agreement. Partial participation or disagreement leaves the rules choice pending and permits revisions; no timeout, host override, first-click rule, Tie Breaker fallback, random selection, automatic lowest/highest identity, or non-rules captain resolves it.
- A cross-team Initiative tie first restricts candidates to the side revealed on the Tie Breaker exactly as today. If more than one candidate remains on that side, those tied candidate owners use the same agreement flow and the coin flips only after the agreed actor's card resolves. A tie containing candidates from only one team never flips the coin. After the chosen card resolves, Initiative is queried again and any remaining tie opens a fresh agreement from the newly derived candidates.
- Agreement identity is bound to the exact standing pending-choice revision and its ordered candidate set. Final choice, changed Initiative/candidates, turn or round advance, restore, retained-prefix Undo, replay, restart, session replacement, game end, and view disposal cannot retain or apply stale preferences, duplicate a final command, select a departed candidate, or leak an old preference into a later tie. Gameplay snapshots and deterministic command replay continue to persist the authoritative pending/final Initiative choice rather than treating pre-agreement preferences as game rules; a local host rebuild may require preferences to be restated, while milestone 8.34 owns preserving the current host-published agreement view across network reconnect.
- Director mode retains its existing one-controller direct choice because that development-only mode already embodies all six seats; the six independently authorized player-seat clients use agreement. All non-owning seats receive a clear public waiting status without actionable controls, while each eligible tied seat receives the same candidate presentation plus its own bounded preference controls.
- Focused Edit Mode tests cover two- and three-player same-team ties, a cross-team tie with multiple candidates on the Tie Breaker side, disagreement and revision to agreement, stale/forged/duplicate refusal, candidate re-derivation, exactly-once final submission, coin behavior, and rebuild clearing. Focused Play Mode journeys exercise all six seat perspectives, pointer and keyboard preference changes, public status, Director-mode preservation, repeated ties, Undo/restart/session replacement, and disposal without duplicate handlers or growing resources. The directly affected 8.30c six-seat prompt, authorization, privacy, stale-control, deterministic outcome, input-handler, resource-lifetime, and supported-resolution checks rerun green at the final pre-network revision.

Closing checks: `TiedInitiative_PlayerSeatsWaitForUnanimousPublicCandidatePreferenceBeforeChoosingTheActor` and `TiedInitiativeAgreement_RefusesIneligibleOrStalePreferencesAndSubmitsOneRulesChoice` fail against the current first-accepted-team-command flow and pass when the transport-neutral agreement coordinator and six-seat prompt are in place.

Non-goals: No change to Initiative values, candidate calculation, Tie Breaker ownership or flip timing, reveal timing, action resolution, card visibility, hidden information, snapshots, deterministic gameplay replay, Director mode, team composition, or any other team-owned choice; no networking package, message serialization, lobby, reconnect implementation, voice/text chat, free-form communication, captain system, timer, automatic choice, AI, Phase 9 styling, or general HUD redesign.
