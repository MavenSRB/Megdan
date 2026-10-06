# Phase 9 - Experimental features and alternate maps

Canonical milestone definitions. Return to [ROADMAP.md](../ROADMAP.md). The [session brief](../SESSION_BRIEF.md) owns active authorization.

## Experimental timer design, documentation only

### [x] 9.55 Record the experimental timer and header design

Authorized 2026-10-05 by the user's instruction to write the agreed discussion into docs, include the in-progress hero's choices, and ensure timeout cannot strand a match; amended to restore clocks on Director Undo. Scope: record the per-match Experimental menu, timer/settings/timeout and header decisions, NebKher coverage prerequisite, and progress acceptance checks. No runtime implementation, Unity launch, publication, or change to the existing hero sequence. T0 closing checks: review against the conversation and current choice inventory, living-documentation validator, and `git diff --check`; local documentation commit and archive index.

Completed 2026-10-05: the [canonical design](../Project/EXPERIMENTAL_FEATURES.md) records the user decisions and unexecuted acceptance obligations.

Historical evidence: [9.55 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-955-experimental-timer-design-evidence).

### [x] 9.56 Implement and gate the experimental timer and header

Authorization amended 2026-10-05: follows 9.58 in the isolated worktree; 9.64 then builds the private playtest package. The revised three-hour window defers map 9.61. Owner: timer implementation milestone. Outcome: the opt-in per-match timer and associated header follow [the design](../Project/EXPERIMENTAL_FEATURES.md), including icon-only clocks and Director Undo restoration. Dependencies: completed 9.58 on this worktree baseline. The 2026-10-05 amendment permits parallel implementation before 9.54; gate the available roster here, then re-audit final NebKher choices and reconcile snapshot/wire changes before a merged release containing him. Closing checks: the design's complete choice/timing matrix, timeout-progress and restoration/network checks, timer-off standard-game regression, and fixed-size header visual checks, at the highest tier selected by [testing policy](../Testing/POLICY.md). No new experiments or expanded player Undo rights. Source seams and proposed implementation/test contracts are in [the preparation packet](../Project/EXPERIMENTAL_PREPARATION.md).

Historical checkpoint, superseded by the resumed gate below: implementation checkpointed at `b51c2114`: host-authoritative allowances/banks, timeout selection, readiness/pause transport, snapshot/Director Undo checkpoints, shared-board voting and the fixed-size header are implemented. Existing focused regressions passed after correcting status layout, restored ballots and reconnect readiness. Full Unity breadth, final screenshots and the final offline rerun are incomplete; broad runs were stopped at the user's explicit request to build without further testing. This is an unvalidated timer candidate, not a completed feature gate. NebKher remains excluded and requires the merge audit. Remaining acceptance stays visible here; no continuation is authorized after the requested 9.64 package.

Completed 2026-10-05 for the available 31-hero roster: timing/timeout, restoration and network checks, full offline and Unity breadth, supported-size visual review, and all observed-failure reruns are complete. Nested Defense responses, atomic rejection of duplicate restored voters and board-marker window layering were corrected. Twelve stale protocol assertions were corrected and all affected fixtures rerun; no failures were quarantined. NebKher remains excluded and requires the final-content merge audit. Built-player checks follow at 9.64c.

Historical evidence: [9.56 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-956-experimental-timer-gate-evidence).

## Teammate coordination design, documentation only

User gate amendment: board highlights and their owner tokens must never paint over game windows, as shown by the spellbook screenshot. Correct the shared signal layer, with an overlapping-window regression at both supported sizes; include this reported acceptance defect in 9.56 before broad validation.

Gate review findings, 2026-10-05: real Parry Defense text returning its discard choice to the attacker used Resolution instead of Response; a duplicated board voter in an experimental snapshot threw during post-Core restore. Closing checks are `DefenseTextReturningToTheAttackerUsesAResponseClock` (including preserved parent budgets, snapshot and timeout completion) and `SnapshotWithDuplicateBoardVotersIsRejectedWithoutChangingTheMatch` (atomic refusal). Both reproduced before correction and passed their focused rerun. The overlapping spellbook-marker regression also failed on the previous layer order and passed after moving board signals beneath the HUD. The available-roster gate and observed-failure reruns are complete; built-player validation follows at 9.64c.

### [x] 9.57 Record the experimental teammate coordination design

Authorized 2026-10-05 after the user reviewed the mockups and directed the feature to be written up as a host Experimental toggle. Scope: allied hand inspection, right-click hex/card suggestions in every phase, reuse of hero-tray identity tokens, and coexistence with existing effect highlights. This is documentation only; it does not authorize runtime implementation or change the active hero sequence. T0 closing checks: conversation/design review, living-documentation validator, `git diff --check`, and a local documentation commit with archive index.

Completed 2026-10-05: [the design](../Project/EXPERIMENTAL_FEATURES.md#teammate-coordination) records the reviewed interaction and visual decisions with unexecuted implementation checks.

Historical evidence: [9.57 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-957-teammate-coordination-design-evidence).

### [x] 9.58 Implement and gate experimental teammate coordination

Amended 2026-10-05: implement now in the isolated `codex/experimental-playtest` worktree, first in 9.58 -> 9.56. The user lifted the wait for the final hero to meet the six-hour playtest target; main-checkout hero work continues separately. Owner: teammate coordination implementation milestone. Outcome: the independent host toggle, allied card inspection and right-click markers follow [the canonical design](../Project/EXPERIMENTAL_FEATURES.md#teammate-coordination). Dependencies: the shared Experimental menu (introduce that surface in this milestone if it has not shipped; the timer itself is not a prerequisite). Closing checks: the design's host/join privacy, all-phase input, marker lifecycle, asset-reuse and overlapping-highlight checks, with the option off preserving existing behavior, at the highest tier selected by [testing policy](../Testing/POLICY.md). No teammate gameplay control, enemy-hand disclosure or timer implementation. Reuse [the preparation packet](../Project/EXPERIMENTAL_PREPARATION.md) after checking its post-hero source delta.

Completed 2026-10-05: host options, team-filtered allied hands, right-click signals, shared portrait overlays and wire 37 are implemented and gated. Focused authority/network/format checks, unfiltered offline and Unity suites, and the corrected real-player UI journey passed. Review covered privacy, authority, visual layering, lifecycle and compatibility. The pending request to overlap milestones is obsolete: gates completed serially. Timer 9.56 follows now; the final hero is excluded and requires the documented merge audit.

Historical evidence: [9.58 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-958-teammate-coordination-evidence).

## Experimental implementation preparation

### [x] 9.59 Prepare the experimental features for implementation

Authorized 2026-10-05 when the user requested as much preparation as possible while NebKher is being implemented. Outcome: a source-grounded implementation packet covering host options, teammate coordination, clocks/timeout ownership, persistence, input/rendering reuse, concrete test cases and the post-hero handoff. Scope is preparation and documentation only; runtime implementation 9.58/9.56 stays queued and the active hero route is unchanged. T0 closing checks: verify referenced source symbols/test fixtures, review proposed contracts against the approved design and architecture, run living-documentation validation and `git diff --check`, then commit locally with evidence and archive index.

Completed 2026-10-05: [the preparation packet](../Project/EXPERIMENTAL_PREPARATION.md) records source seams, proposed technical contracts, numerical/behavioral cases, remaining dispositions and existing regression owners. No executable changes or gameplay tests.

Historical evidence: [9.59 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-959-experimental-preparation-evidence).

## Future map references

### [x] 9.60 Preserve the future map references

Authorized 2026-10-05 when the user asked to save the first map for future 2v2/3v3 implementation, then supplied a second for 4v4/5v5 with two independently pushed Battle Zones and whole-match victory when either reaches the opposing Throne. Amended by the later preferred-image uploads and the correction that the last lava/bridge map is also two-lane 4v4/5v5. Scope: preserve all five uploads for three distinct maps privately, record their exact formats and shared-turn/two-lane semantics, and queue implementation. No map transcription or runtime change. T0 closing checks: source/copy hash equality, ignored/private asset status, documentation validation and `git diff --check`; local documentation commit and archive index.

Completed 2026-10-05: the [reference records](../Maps/README.md#future-map-references) identify all private originals/preferred copies, the three maps' exact formats and the shared two-lane rules.

Historical evidence: [9.60 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960-alternate-map-reference-evidence).

### [x] 9.60a Replace tower markers in private map artwork

Authorized 2026-10-05: replace the two tower glyphs on the small 2v2/3v3 reference with same-team ranged minion glyphs, and the four on each large 4v4/5v5 reference with same-team melee glyphs. Reuse cropped artwork with matching terrain; preserve the original uploads and save private edited siblings. Scope is reference artwork only, not runtime implementation. Closing checks: inspect all ten replacements, unchanged dimensions and pixels outside bounded patches, preserved source hashes, ignored output status, living-documentation validation and diff check; local documentation commit.

Completed 2026-10-05: all ten glyphs replaced in preserved private siblings; [working artwork references](../Maps/README.md#user-edited-spawn-artwork).

Historical evidence: [9.60a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960a-map-spawn-artwork-evidence).

### [x] 9.60b Clean and centre the replacement map glyphs

Authorized 2026-10-05 after the user identified off-centre symbols and old artwork bleeding through 9.60a's patches. Reinspect all ten edited hexes, replace tower/shadow footprints with clean matching terrain and centre the existing same-team glyphs; preserve original uploads and the first edited versions. Scope remains private reference artwork only. Closing checks: before/after close-ups of all ten hexes and full maps, opaque coverage of old tower footprints, centred glyph placement, unchanged dimensions and pixels outside bounded patches, original hashes and ignored outputs, documentation validation and diff check; local completion commit.

Completed 2026-10-05: all ten hexes reinspected and corrected; [current private artwork](../Maps/README.md#user-edited-spawn-artwork).

Historical evidence: [9.60b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960b-map-artwork-cleanup-evidence).

### [x] 9.60c Align replacement glyphs with their lanes

Authorized 2026-10-05: recheck all ten replacement glyphs for visual centring and match their orientation to same-team minions in the respective lane. Preserve the cleaned versions and original uploads; edit private reference artwork only. Closing checks: lane-reference orientation comparison, enlarged hex-centre inspection, full-map review, bounded-pixel and preservation checks, documentation validation and diff check; local completion commit. Runtime implementation remains queued.

Completed 2026-10-05: all ten markers checked for centring and lane-local orientation; [private working copies](../Maps/README.md#user-edited-spawn-artwork).

Historical evidence: [9.60c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960c-map-glyph-alignment-evidence).

### [ ] 9.61 Implement the alternate map for 2v2 and 3v3

Originally conditionally authorized 2026-10-05 after 9.58 -> 9.56; deferred from this playtest scope when the remaining window became three hours including the build. Retain preparation for later. Owner: alternate-map implementation milestone. Outcome: the exact pictured map is available as an additional map choice for 2v2 and 3v3, with a standard map selector directly on the host screen, outside Experimental, and the existing map still available. The user explicitly confirmed this separation after 9.63; verify selection with every experiment off. Reference and scope: [map reference record](../Maps/README.md#future-alternate-map-for-2v2-and-3v3). Dependencies: completed experimental sequence, 9.63 preparation, reviewed hex/terrain/zone/spawn transcription and applicable setup/rules, with artwork usage remaining within authorized private use. Closing checks: canonical map specification/resource parity, map selection and both team sizes, legal movement/targeting/spawning and lane progression under the confirmed map rules, snapshot/replay/network map identity, supported-viewport artwork alignment, and existing-map regressions at the applicable testing tier. The earlier 5v5/two-lane discussion does not define this image's topology or authorize extra formats.

### [ ] 9.62 Implement the two-lane maps for 4v4 and 5v5

Queued only; not authorized. Owner: future two-lane map implementation milestone. Outcome: both [saved two-lane maps](../Maps/README.md#future-two-lane-maps-for-4v4-and-5v5) support eight or ten players in one shared initiative order, with two independently pushed active Battle Zones; either lane reaching the opposing Throne ends the whole match. Dependencies: explicit implementation approval, reviewed map/spawn specification, applicable player-count setup values, and audit of lane-specific rules/hero effects without unrequested changes. Closing checks on each map and player count: 4v4/5v5 lobby and seat admission, independent zone progression, Minion Battles/Lane Pushes and immediate whole-match victory from either lane, shared turns, relevant hero effects, restore/Undo/replay/network identity, readable HUD/artwork alignment, and existing-map/format regressions at the highest applicable testing tier. The first image remains a separate 2v2/3v3 map request.

## Small-map preparation

### [x] 9.63 Prepare the alternate 2v2/3v3 map

Authorized 2026-10-05 while the final hero is being implemented. Prepare an explicit axial cell inventory, terrain and zone membership, spawn types, artwork registration, annotated review views, and source-grounded host-selection/setup/restore integration plan. Preserve private images and the existing runtime. Record ambiguous cells as review items rather than inventing rules. Closing checks: unique coordinates and IDs, complete zone/spawn references, connectivity and lane order, geometric overlay review, original-image preservation, source-seam verification, living-documentation validation and diff check; local commit and archive row. No Unity or runtime import during preparation. The active hero milestone remains 9.52; implementation follows the approved 9.54 -> 9.58 -> 9.56 -> 9.61 route.

Completed 2026-10-05: [small-map packet](../Maps/ALTERNATE_SMALL_PREPARATION.md) with explicit candidate cells, spawns, registration and private review sheets; the outer-perimeter review limitation remains a promotion check at 9.61. No runtime change.

Historical evidence: [9.63 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-963-small-map-preparation-evidence).


### [x] 9.63a Complete the small-map terrain border

Authorized 2026-10-05 after the user identified missing outer borders around the spawn zones and confirmed that these must be terrain hexes. Add the 22 missing neighbour cells (11 around each Throne) to the preparation candidate; preserve all existing cells, zones, spawns and source artwork. Update registration metadata, counts, adjacency and private overlays. Closing check: a complete-neighbour regression over every playable cell must fail on the 9.63 candidate and pass after correction; existing map invariants, symmetry, unchanged playable/spawn data, visual border review, documentation validation and diff check must pass. T0 preparation only: no runtime registration, Unity launch or change to active hero work. Commit locally with evidence and archive index.

Completed 2026-10-05: 22 terrain-only cells added, completing all playable-cell neighbours; [candidate and corrected overlays](../Maps/ALTERNATE_SMALL_PREPARATION.md).

Historical evidence: [9.63a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-963a-small-map-terrain-border-evidence).


User amendment, 2026-10-05: "can you give me the build without testing it?" The implemented 9.56 candidate is checkpointed with its existing focused evidence; its full gate remains incomplete. Stop the current broad runs, preserve their evidence, and proceed directly to 9.64. Interrupted runs and omitted final UI checks are not passes. No further tests or player smoke are required for this explicitly untested package. Resume the remaining feature gate only under a later route.

## Experimental playtest package

### [x] 9.64 Build the experimental playtest player

Authorized 2026-10-05 when the user set three remaining hours and required build time to be included. Outcome: a private Windows playtest build containing the validated experimental changes, with the existing single Host/Join entry point. Dependencies: the experimental feature gates; preserve the independent hero checkout and reconcile it only under merge authorization. The original build reservation was 45 minutes before 19:00 UTC; the user subsequently directed shorter build testing, targeting 20–25 minutes with one build and focused launch/Host/Join/experimental-control checks. omit optional map 9.61 from this playtest scope. Closing checks: build result and executable identity, ordinary launch/Host/Join, experimental settings and teammate controls in the built player, applicable player-build/board-visual/format smoke recipes, and no included private logs or source artwork beyond the local build's required resources. Keep the package private; no upload, remote push, or distribution is authorized.

Latest 9.64 amendment: produce the current Windows candidate without launching it or running smoke tests. Closing scope is successful compilation/build, correct private runtime packaging, executable identity and an explicit untested label. This replaces the earlier smoke requirement for this package only; it does not mark the suspended 9.56 gate complete. NebKher and the alternate map remain excluded.

Completed 2026-10-05 under the build-only amendment: `Builds/Playtest-9.64-experimentals-untested.zip` contains the fresh Windows runtime and the single Host/Join launcher, with source/build identity, file manifest and an explicit untested notice. The strict build succeeded. The player was not launched; no smoke or final feature-gate pass is claimed. The optional map and NebKher are excluded. The package remains private.

Historical evidence: [9.64 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964-untested-experimental-package-evidence).

### [x] 9.64a Correct the reported experimental playtest UI issues

Authorized 2026-10-05 from the user's screenshots: make all five duration input values black and vertically centre the clock icon and digits, preserving header dimensions. Rebuild the private candidate with both visual fixes. The first package remains the requested untested delivery. After the user said to take time because that build is available, use focused T2 regression and visual checks for these reported corrections before the replacement build; do not resume the suspended broad 9.56 gate. The user then added two corrections: teammate card highlights/owner markers must follow the hover pop, and Fast Travel must stay selected through clock-only refreshes. The later clock-isolation correction below supersedes the initial symptom-specific approach; real state changes still invalidate local preparation normally. The user also requested slightly larger hearts, a vertically centred Round/Turn + Waves block, and Emmitt Glitch token artwork. Prepare the verified private clone atlas face only; do not alter hero rules. No final timer gate claim. Preserve the preceding package.

9.64a correction: the user explicitly rejected a Fast Travel-only workaround. Replace periodic full projections with a separately sequenced authenticated clock-display frame, preserving gameplay projection identity and all unrelated UI state. A true accepted game command still publishes its ordinary projection. Closing checks must prove clock-only delivery does not advance projection/log revisions, clear command waits, rebuild hands/choices or disturb hover/selection; validate stale frames and reconnect identity. Remove the symptom-specific presenter exception.

Compatibility audit: the initial 9.64 candidate actually retained wire constant 37 although its package notes said 38. The independently stamped initial ZIP is preserved. The corrected clock-only message is version 39 in code and documentation; replacement package identity must use that actual value.

Completed 2026-10-05: source `7ec9fa83` isolates authenticated clock updates from gameplay projections and unrelated UI, and corrects the reported input, alignment, heart, hover-marker and Glitch-artwork issues. Focused boundary/UI checks and supported-size visual review passed. The replacement `Builds/Playtest-9.64a-experimental-fixes.zip` built successfully and passed package integrity checks; the previous ZIP is preserved. No player launch or full timer gate is claimed. Review found no rule, hero-content or Undo-right change. Stop here; 9.56 remains suspended.

Historical evidence: [Completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964a-experimental-corrections-evidence).

### [x] 9.64b Remove the spellbook hover popup

Authorized 2026-10-05 by the user's Gydion spellbook screenshot. Remove the redundant enlarged hover face from cards inside the spellbook drawer; retain the full drawer faces, private/public visibility, ordinary tray previews and spell decisions. T2 closing checks: pointer-hover regression, affected spellbook presentation fixture, focused real-scene hover/close and visual inspection; documentation validation and local commit.

9.64b completed: removed the spellbook-only hover callback and obsolete spell-preview members. Full faces and normal tray previews remain. The real-pointer regression failed on the prior behavior, then passed with the affected Edit/Play checks; the screenshot was reviewed.

Historical evidence: [9.64b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964b-spellbook-hover-correction-evidence).

### [x] 9.64c Validate and package the corrected experimental player

Authorized 2026-10-05 by "finish the skipped test then", after 9.64b and resumed 9.56. Run the skipped player-build, board visual and multi-client smoke checks including experimental controls, then create a separately named private replacement ZIP. Preserve previous packages. This bounded sequence excludes new maps, NebKher implementation/merge and remote publication.

Sequence amendment 2026-10-05: **9.64b -> 9.56 -> 9.64c** is now authorized. Resume the skipped full offline and Unity Edit/Play timer gates after the popup correction, resolving observed in-scope failures. Reuse current focused evidence until affected; review timeout progress, pauses, responses, voting, snapshots and Undo. Gate the available roster only; final NebKher choices remain a merge prerequisite.

9.64c gate finding: the playing smoke host exited after its own witness, starving slower clients of the experimental suggestion round trip. Keep the smoke authority alive until both captured human peers depart; the runner still requires each peer PASS and clean exit. Closing checks: `SmokeHostWaitsForBothRemoteWitnessesToLeaveBeforeExiting`, rehearsal contract regressions, and fresh standard/experimental multi-client player runs. The initial experimental run failed; ordinary gameplay is outside this smoke-only lifecycle correction.

Completed 2026-10-06: the corrected player was built, exercised through its scripted two-round journey, rendered-board check and host-plus-two-client lobby checks with experiments off/on, then packaged separately as `Builds/Playtest-9.64c-experimental-validated.zip`. The smoke host shutdown race was corrected and its unit/real-process closing checks passed. Private artwork and package integrity checks passed; previous ZIPs are preserved. Review found no additional game-rule, content or compatibility change. An extra manual packaged-launcher inspection was unavailable because the computer-use helper could not initialize; no manual or new internet/fault rehearsal is claimed. The approved sequence is complete; stop here.

Historical evidence: [9.64c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964c-validated-experimental-package-evidence).

### [!] 9.64d Add the shared spellbook hover bump

Authorized 2026-10-06: visible spellbook faces get the existing subtle hover bump, without a detached popup. T2 closing checks: spellbook presentation regression, real-pointer hover/leave/close at both supported sizes, existing window-layer regression, visual inspection, documentation validation and local commit. Preserve layout, privacy and ordinary hand behavior. A first Unity attempt was refused while the separate hero gate occupied the serial Editor slot; no gameplay check is yet claimed. Existing ZIP stays unchanged.

### [!] 9.64e Add experimental current-card inspection

Authorized 2026-10-06 as the next outcome after 9.64d. Approved sequence: **9.64d -> 9.64e**, source changes and their gates only. Portrait clicks open the hero's current card set across hand/committed/resolved/discarded states plus unlocked Ultimate, with color/tier filters; future upgrades and cards converted to items are excluded. Keep the window to card inspection without additional hidden-state information. The latest user amendment puts allied inspection, Gydion's allied spellbook and all Takahide gold-card faces in the existing Teammate coordination/team-sharing option. Enemy card information, including those exceptions, belongs to its own independent Experimental toggle. Takahide is confirmed; the earlier request to show everyone's spellbook unconditionally is superseded by this team/enemy split. [The design owner](../Project/EXPERIMENTAL_FEATURES.md#current-card-inspection-extension) carries the exact boundaries.

The visibility/toggle request is a separate network/privacy outcome from the hover refinement. Closing checks: current-set membership and combined filters; own/allied/enemy visibility with each option independently on/off; hidden commitment protections; explicit book/gold exceptions; host settings and synchronized/snapshotted options; supported-size real-pointer portrait/filter/close journeys and window layering; affected network compatibility and persistence gates under testing policy. Ordinary tray selection outside portraits and gameplay authority stay intact. No new hero/map implementation or replacement build is included. Depends on 9.64d completion and the serial Unity slot. Implementation resumed on the user's explicit "Ok, implement it?" direction while the independent hero gate still occupies Unity. 9.64d is held only for its focused UI checks; run those before final 9.64e closure.

9.64e implementation checkpoint: state-free definition lists, independent enemy toggle, portrait window/filters and shared spellbook/gold visibility are implemented. The complete offline gate and focused Unity Edit gate are green; [the pending checkpoint](../Archive/Phase-Handoffs/PHASE_9.md) indexes their evidence. Selected tier remains T3 for the experimental snapshot envelope and network-to-UI path. The independent hero task started its full Play Mode run in the main checkout (PID 53728 at the checkpoint), so the serial launcher refused this worktree's focused Play request. Do not interrupt or replace that process.

Resume with the prepared `tmp/unity-validation-request.json` once no Editor is active: focused spellbook-hover, card-inspection, coordination and timer Play tests write `tmp/964de-focused-play.xml` and its log. Inspect the new 1280/1920 captures and fix any acceptance failures. Close 9.64d separately after its hover evidence, then run both unfiltered Unity suites for 9.64e and complete its review/documentation/commit. Reuse unaffected green evidence; do not rebuild or rerun the full offline suite solely because Unity's NUnit assertion syntax was corrected. No completion, new package or publication is claimed.
