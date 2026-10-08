# Phase 9 - Completed presentation milestone definitions

Canonical active roadmap shard holding completed Phase 9 presentation definitions. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_9.md](PHASE_9.md). Historical completion evidence: [PHASE_9 handoffs](../Archive/Phase-Handoffs/PHASE_9.md).

Created 2026-10-08 at milestone 9.91 when the active Phase 9 shard exceeded its governance limit; the contiguous presentation run 9.41 through 9.44 and its shield follow-ups 9.84 through 9.84a moved here losslessly.

### [x] 9.41 Readable faded-card hover

Authorized 2026-10-03 after 9.13f: faded cards display at full brightness on hover; remove the redundant separate card-text tooltip, preserving printed face text and action eligibility. T2: faded/readable/restored states, unchanged unavailable-card input, affected card-hover regressions and focused Unity player-path/visual checks. No level-up or network correction is implied.

Result: the ordinary hand's shared full-face hover replacement explicitly renders at full opacity and omits the resting unavailable veil, while the hidden anchor retains its projected unavailable or resolution-dimmed state and returns unchanged on exit. Tray controls keep only `AssistiveLabel` as their tooltip; the complete face remains the sole rendering of printed rules text. The unavailable group stays enabled and focusable for inspection but carries no selection or drag command. Focused Unity evidence: Edit Mode 2/2 (`UnavailableCards_RetainDimmingAndRebuildWithoutDuplicatesCommandsOrHiddenIdentityLeaks`, `TheResolvingCard_ShinesWhileTheRestOfTheHandDims_UntilItResolvesAndReleases`) and Play Mode 1/1 (`Scene_OverlappedHandHoverRevealDoesNotStealTheCardClick`), with zero skips or inconclusive cases.

Completed 2026-10-03.

Historical evidence: [9.41 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-941-readable-faded-card-hover-evidence).

### [x] 9.42 Preserve the standing joined-seat level-up window

Authorized 2026-10-03: "Yes, reproduce and fix it." Joining players reported that the level-up window briefly appeared and then disappeared without input on current and older builds; reconnect restored it. The initial spotlight/display hypothesis was rejected after the user's timing objection: a strengthened real minion-defeat journey confirmed that the bounded local attack shot and the later round-end Level Up do not naturally overlap. The reproduced cause is joined-client state instead. A same-version temporary network/input pause deliberately strips private answer detail and commands; `DecisionWindowPresenter` retained Tied Initiative across that state but rebuilt an owning seat's already-drawn Level Up as `null`. It now retains that Level Up only while the public owner and authority version still name the same decision and the joined read model is non-actionable; the existing interaction wall keeps it unanswerable until private detail returns. Other seats still receive no window, authority advancement still clears it, and resynchronization restores controls without requiring reconnect. No rules, wire or network-authority change.

Closing evidence: `LevelUpWindowSurvivesJoinedSeatConnectionPause` failed pre-fix with the real owner-scoped Level Up replaced by `null`, then passed; the complete `DecisionWindowPresenterTests` fixture passed 24/24. The real Unity Level Up journey `InitiativeBadgeShowsEarnedBonusAndKeepsItsFootprint` passed 1/1 and produced its three expanded-window captures at 1280x720, 1920x1080 and 2560x1440. T2.

Completed 2026-10-03.

Historical evidence: [9.42 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-942-standing-joined-seat-level-up-window-evidence).

### [x] 9.43 Show the attack before its after-attack presentation

Authorized 2026-10-03: attack animations should happen before "after the attack" text happens. Inspect the presenter/conductor ordering; show the attack before its consequent prompts and visual effects, without delaying authoritative rules/network progression or changing printed timing. Cover a choice-bearing after-Attack branch, an automatic consequence, interruption/reconnect and queued attacks. T2; ordered presentation regression and focused rendered player-path/visual evidence. Keep distinct from 9.42's stale visibility correction.

Result: the public Attack row now reaches Presentation at the first authoritative resolved-attack boundary instead of waiting for the action's later lifecycle completion: hero attacks publish at their final success/defended outcome, while minion attacks publish at `AttackStarted` because p.13 fixes that non-defending arm as successful and emits no later outcome fact. The spotlight refreshes before the decision window. A newly authoritative after-Attack model is cached but drawn as absent through camera framing, the post-zoom beat and any earlier queued kinetic scenes; a kinetic scene-start notification releases it only after the framed attack's lunge has genuinely entered playback. Interruption releases the standing question, first-sight/reconnect still primes old attacks without replay, and authority, commands, printed timing, snapshot and wire format are unchanged.

Closing evidence: the new decision regression first failed with the after-Attack model already drawn, and the real Wasp/Shock minion path then demonstrated the missing Attack row while its effect choice stood; both passed after the two-arm timing correction. Focused Edit Mode passed 3/3 and the focused rendered Play Mode journey passed 1/1. Directly affected `MatchLogChannelTests`, `AttackSpotlightTests`, `KineticCueTests`, `KineticConductorTests`, `MatchFeaturePresenterTests` and `DecisionWindowPresenterTests` passed 230/230, including automatic smack pacing, queued/repeated attacks, first-sight/reconnect, projection loss, rewind and interruption; the complete real-scene `WavePresentationPlayModeTests` passed 5/5. The batch-capable visual capture `tmp/943-captures/attack-before-after-attack-question.png` was inspected at the lunge boundary. T2.

Completed 2026-10-03.

Historical evidence: [9.43 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-943-attack-before-after-attack-presentation-evidence).

### [x] 9.44 Implement hero-hover Defense contributors

Mockup approved for implementation 2026-10-03: "Implement this, and all the other stuff previous agent was working on as well." Preserve the hovered hero highlight and highlight only minions contributing to that hero's Defense: green positive and blood-red negative, without numeric labels on the minions. The user's latest correction requires a very small shield near the hero token's bottom edge containing only the centered signed modifier (illustratively "-1"), with no "DEF" text. Mockup uses an illustrative net minion modifier, not an invented complete Defense total before a card is chosen. Implementation consumes the immutable attack-target contribution previews: board and linked rail hover tint only their stable minion identities, each hovered hero body carries the summed projected minion modifier in the number-only shield, and hover exit clears every added treatment without a command. No Defense rule is recalculated in presentation.

Completed 2026-10-03.

Historical evidence: [9.44 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-944-hero-hover-defense-contributors-evidence).

### [x] 9.84 Persistent left-edge hero Defense modifier

Authorized 2026-10-08 after the discussion mockup. Keep the existing tiny number-only shield visible on the left edge of every standing hero token, showing that body's net standard-minion Defense modifier, including zero. Preserve actual card-state pips, token artwork, player colours and shield styling. Ordinary board/linked-rail hover highlights only contributing creeps using the existing signed colours; the same interaction works during Attack target selection. Hover exit removes contributor highlights while retaining each hero's shield. This remains a minion modifier, not a complete Defense Total or a prediction of a private Defense card.

Closing checks: rules-query reuse over public board state; per-body readings, zero and signed totals, movement/removal/Undo refresh and local/seat/network agreement; focused presentation regressions for normal/Attack hover and exit, camera-relative left placement, unchanged card pips and resource reuse; real-scene Play Mode capture and visual inspection of the actual result. T2. No card/rule/lifecycle/command, snapshot, wire, build or publication change. User requested isolated branch codex/hero-defense-shield; merging is deferred. Preserve unrelated 9.82 work and serialize Unity validation.

Historical evidence: [9.84 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-984-persistent-hero-modifier-completion).

### [x] 9.84a Match the hero shield to the discussion mockup

Authorized 2026-10-08: replace the flat squat badge with the mockup's taller tapered shield, black outline and light-grey face. Amended the same day: center the shield on the token's left rim, matching the card pips' rim centerline, with no downward offset. Final approved token preview: enlarge shield width, height, outline and value by 30%; separate the unchanged-size hand pips with 0.28-unit center spacing (0.04-unit clear gap). Change only icon geometry, styling, pip spacing and that placement on `codex/hero-defense-shield`; preserve fitted signed values, card symbols and normal/Attack hover behavior. Closing checks: focused geometry/material/resource, shared rim-centerline, enlarged dimensions, clear pip gaps and existing hover regressions; real-scene normal/hover/Attack captures inspected against the recovered reference. T2; no rules, input, wire, build, merge or publication changes.

Historical evidence: [9.84a completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-984a-approved-token-shield-and-pip-spacing-completion).

### [x] 9.66 Disarm an END TURN press when its actor's turn has passed

Authorized 2026-10-07 by the user's playtest report: "End turn gets pressed automatically now.
After movment, after using skill." Outcome: a joined seat's asynchronous one-press END TURN
continuation is discarded as soon as a projection no longer concerns the hero whose press armed it,
even while that seat's input is disabled because another hero is acting. The stale continuation must
not survive into that hero's later turn and must not submit Hold, Stop Movement, an effect stop, or
Release Turn after a later Movement or Skill update. Preserve the existing one-press composition
across the original press's own network round trips, including a temporary input-disabled projection
that still concerns the same actor. No rules, command, wire, snapshot, bot or ordinary action-flow
change. Closing check: a deterministic joined-client regression reproduces the press crossing to an
input-disabled other-actor projection and then returning to the original actor; it fails before the
fix by submitting a command without a new press and passes after. Run the focused presenter fixture,
the directly affected END TURN input/provenance fixtures, and focused Play Mode for the live control.
T2.

Result: the joined-seat continuation now checks its actor boundary before the temporary network input
gate. Once the authoritative projection concerns another hero, the old press is discarded even though
that seat cannot act; a later turn for the same hero therefore has no press left to resume after a
Movement or Skill update. The original one-press END TURN path still resumes while an input-disabled
projection concerns the same hero. The regression failed before the fix by submitting three commands
where the one old press owned only two (`Expected: 2; But was: 3`) and passed after. Focused Unity
Edit Mode passed 122/122 across `MatchFeaturePresenterTests` and
`EndTurnPressProvenanceTests`; focused live-control Play Mode passed 1/1. No rule, command, wire,
snapshot, bot or package changed.

Completed 2026-10-07.

Historical evidence: [9.66 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-966-end-turn-continuation-boundary-evidence).

### [x] 9.68 Remove redundant tooltips from available printed actions

Authorized 2026-10-07 by the user's direction to merge the remaining completed agent work. Outcome:
an available printed-action hotspot carries no tooltip because the complete card face already prints
its rules, and it stops runtime-tooltip ancestor lookup before the resolving card's fallback label.
An unavailable action retains its concise reason tooltip. Preserve the whole-card hover face, action
highlighting, pointer reachability and exact typed invocation. No rules, command, content, wire,
snapshot or artwork change. Closing checks: focused lookup and hand-presentation regressions, plus a
live-panel hover regression proving the runtime popup remains hidden over an available action while
the hotspot remains clickable. T2.

Result: available printed-action regions now carry an explicit runtime-tooltip barrier and no local
tooltip, so neither the action's assistive label nor the resolving card's ancestor label opens over
rules already visible on the card face. Unavailable regions retain their reason. The typed invocation,
shine treatment, whole-card hover behavior and pointer click path are unchanged. Focused Unity Edit
Mode passed 146/146 across `MatchHudPresentationTests` and `RuntimeTooltipLayerTests`; the
focused live resolving-card Play Mode journey passed 1/1, including the delayed popup absence and
the accepted action click. No rule, command, content, wire, snapshot, artwork or package changed.

Completed 2026-10-07.

Historical evidence: [9.68 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-968-printed-action-tooltip-completion).

### [x] 9.93 Keep the Defense breakdown Total inside its panel and refresh the build

Authorized 2026-10-08 from the user's packaged-player screenshots. The divider's one-pixel line and two four-pixel margins were omitted from the explicit panel-height calculation, so every `Total` row overflowed nine pixels below the dark background. Add exactly that missing height; enlarge the current-card pip above the token and every hand/concealed pip below it to 150% in both dimensions while retaining their existing radial centerline and tightening the lower arc enough to keep visible gaps without reaching the unchanged Defense shield; and raise the ping sender portrait from 0.42 to 0.48 scale without returning to the old oversized 0.62 treatment. Prove empty and one-creep layouts contain the Total, pin pip-to-pip and pip-to-shield separation, visually confirm the corrected real UI and larger ping owner, and cut a replacement private Windows package. Reuse prior behavior evidence and run only the focused panel/pip/ping regressions plus artifact-facing build/package checks; no offline harness or broad Unity suites. Preserve 9.92 and publish nothing.

Result: the missing nine-pixel divider footprint now keeps `Total` fully inside the dark panel. Every current, hand and concealed token card pip is 150% of its former width/depth on the same rim centerline; the lower arc uses 0.22 spacing, preserving visible gaps and conservative clearance from the unchanged shield at three camera headings. Ping sender portraits move from 0.42 to 0.48 scale and remain below the old 0.62 size; shared-hex sender order and placement are unchanged. Focused Edit failed first on the over-wide lower arc and a misaligned geometry fixture, then passed 32/32 after narrowing the arc and aligning the fixture to the real camera. The exact Defense-panel and multi-player-ping Play cases passed 2/2; fresh full-frame captures show the contained Total, separated larger pips, and larger three-sender ping row. `Builds/Playtest-9.93-ui-legibility` and its ZIP carry stamp `1.0-9.93-ui-legibility+20261008123844-23d94a02`, wire `/49` and schema 59. Exact-player and packaged-player interaction smokes passed. All 184 ZIP files match the loose 584325691-byte package byte-for-byte with no log/debug/DoNotShip entry; the 102366856-byte ZIP SHA-256 is `6b3b874d3c7ef6fdb93da2e522d0c838289718d32577a14f9dc6b63a69df03d7`. The offline harness and unfiltered Unity suites were omitted by user direction. Package 9.92 remains preserved; nothing was published.

Completed 2026-10-08.

Historical evidence: [9.93 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-993-defense-and-token-legibility-completion).
