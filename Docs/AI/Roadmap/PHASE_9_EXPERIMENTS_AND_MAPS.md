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

### [x] 9.61 Implement the alternate map for 2v2 and 3v3

Authorized now, 2026-10-06: implement the prepared small map and ordinary host-screen selector in the experimental worktree while the hero finishes. This supersedes the prior playtest deferral and allows source work before the pending 9.64d/e Unity gates close. Keep the later morning hero-branch merge and build separate. Owner: alternate-map implementation milestone. Outcome: the exact pictured map is available as an additional map choice for 2v2 and 3v3, with a standard map selector directly on the host screen, outside Experimental, and the existing map still available. The user explicitly confirmed this separation after 9.63; verify selection with every experiment off. Reference and scope: [map reference record](../Maps/README.md#future-alternate-map-for-2v2-and-3v3). Dependencies: completed experimental sequence, 9.63 preparation, reviewed hex/terrain/zone/spawn transcription and applicable setup/rules, with artwork usage remaining within authorized private use. Closing checks: canonical map specification/resource parity, map selection and both team sizes, legal movement/targeting/spawning and lane progression under the confirmed map rules, snapshot/replay/network map identity, supported-viewport artwork alignment, and existing-map regressions at the applicable testing tier. The earlier 5v5/two-lane discussion does not define this image's topology or authorize extra formats.

Completed 2026-10-06: both catalogs load the promoted map; map-aware setup preserves the original board and uses each map's own spawns. The standard host selector, lobby label, typed single-retry map/format inheritance and private artwork registration are implemented. T3 applies to the playable-map gate and real-TCP Play test assembly boundary. Full offline, focused Unity and both unfiltered Unity suites pass. Map checks cover both team sizes/all Quick/Long setups, experiments-off hosting, real TCP inheritance, repeated timeout progress, movement/Fast Travel/respawn, lane spawning and both throne captures, persistence/Undo/reconnect and original-map compatibility. Supported-size artwork, spawn alignment and the complete border overlay were reviewed. Pure rules/map identity and private asset boundaries are preserved; no further acceptance defect was found. No hero merge, build or publication occurred. The bounded morning integration is 9.65.

Historical evidence: [9.61 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-961-completion).

### [!] 9.62 Implement the two-lane maps for 4v4 and 5v5

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

### [x] 9.64d Add the shared spellbook hover bump

Authorized 2026-10-06: visible spellbook faces get the existing subtle hover bump, without a detached popup. T2 closing checks: spellbook presentation regression, real-pointer hover/leave/close at both supported sizes, existing window-layer regression, visual inspection, documentation validation and local commit. Preserve layout, privacy and ordinary hand behavior. The initial serial-Editor hold is resolved. Completed 2026-10-06: focused Edit/Play checks and real-pointer hover/leave/close passed; both supported-size captures show the shared subtle bump, no detached popup and preserved window layering. Review found no rule, privacy or hand-behavior change. Evidence is indexed in the phase archive. Existing ZIP stays unchanged.

Historical evidence: [9.64d completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964d-completion).

### [x] 9.64e Add experimental current-card inspection

Authorized 2026-10-06 as the next outcome after 9.64d. Approved sequence: **9.64d -> 9.64e**, source changes and their gates only. Portrait clicks open the hero's current card set across hand/committed/resolved/discarded states plus unlocked Ultimate, with color/tier filters; future upgrades and cards converted to items are excluded. Keep the window to card inspection without additional hidden-state information. The latest user amendment puts allied inspection, Gydion's allied spellbook and all Takahide gold-card faces in the existing Teammate coordination/team-sharing option. Enemy card information, including those exceptions, belongs to its own independent Experimental toggle. Takahide is confirmed; the earlier request to show everyone's spellbook unconditionally is superseded by this team/enemy split. [The design owner](../Project/EXPERIMENTAL_FEATURES.md#current-card-inspection-extension) carries the exact boundaries.

The visibility/toggle request is a separate network/privacy outcome from the hover refinement. Closing checks: current-set membership and combined filters; own/allied/enemy visibility with each option independently on/off; hidden commitment protections; explicit book/gold exceptions; host settings and synchronized/snapshotted options; supported-size real-pointer portrait/filter/close journeys and window layering; affected network compatibility and persistence gates under testing policy. Ordinary tray selection outside portraits and gameplay authority stay intact. No new hero/map implementation or replacement build is included. Depends on 9.64d completion and the serial Unity slot. Implementation resumed on the user's explicit "Ok, implement it?" direction while the independent hero gate occupied Unity. The earlier 9.64d hold is resolved; its gate completed before this closure.

Completed 2026-10-06: state-free current-card definition lists, independent enemy toggle, portrait window/filters and permitted spellbook/gold visibility are implemented. The T3 gate passed: full offline, focused Unity, both unfiltered Unity suites and supported-size visual review. The earlier serial-Editor hold is resolved; 9.64d is complete too.

Visual review found the hand covering the inspection window's bottom card. Its real-pointer regression failed, then passed after raising inspection above hand layers and blocking board clicks inside its rectangle; the title encoding was corrected too. Filters, close, clock/marker retention and decision-window precedence pass. Review found no additional authority/privacy expansion or acceptance defect. No new build, merge or publication.

Historical evidence: [9.64e completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964e-completion).

### [x] 9.64f Isolate teammate-highlight updates from gameplay UI

Authorized 2026-10-06 when the user reported windows resetting on highlights and said to carry on. Approved sequence: **9.64f -> 9.61**, with the existing 9.64d/e validation obligations retained. A right-click hex/card suggestion must update only communication overlays; preserve current game projection, open windows, scroll, hover, prepared selection and pending command acknowledgement. Trace and remove both local full refreshes and remote full-projection publications for marker-only changes. Keep team scoping, stale/duplicate rejection, reconnect and ordinary gameplay refreshes. Closing checks: failing-then-passing marker-only wire/read-model identity, outstanding-command and stale-frame regressions; actual open-window/hover/selection retention with overlay changes; affected network/presentation regressions and the required tier. Merge/build remain later work.

Completed 2026-10-06: marker-only wire updates and local card/hex overlays preserve gameplay projection identity and repaint only decorations. Failing-then-passing identity checks, stale/duplicate/version/generation rejection, pending acknowledgements, reconnect and affected offline/Unity checks are green. Actual pointer journeys preserve the card instance, hover and open inspection window while signals change; both supported-size captures were reviewed. T2 review found no authority or privacy expansion. The map advances protocol 41 to 42 separately. The 9.61 and 9.64e gates close separately; no merge or new build is claimed.

Historical evidence: [9.64f completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964f-completion).

### [x] 9.64g Simplify portrait inspection to an undifferentiated current set

Authorized 2026-10-06: remove color/tier filters and their count from portrait inspection. Show all currently permitted card faces with no lifecycle labels, grouping, styling or state-driven reordering/rebuild. Preserve current-set membership, gold/book exceptions and independent visibility toggles. T2 closing checks: no-filter and hidden-commitment face-retention regression; current-set/privacy checks; real-pointer inspection/hover/close, clock/marker retention and layering at both supported sizes; visual review, documentation validation and local commit. No protocol, rules, hero integration or replacement build change. This supersedes the filter UI accepted at 9.64e.

Completed 2026-10-06: filters and count removed. Portrait faces omit shared hand lifecycle tooltips and generated captions; definition-only ordering and unchanged-face refresh preserve the complete set across a hidden commitment. Focused T2 checks and both viewport reviews passed, covering private current-set membership, artwork/fallback faces, portrait interaction, retained clock/marker updates and window layering. Review found no rules, visibility or compatibility change. Existing build and hero worktree are unchanged.

Historical evidence: [9.64g completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964g-completion).

### [x] 9.64h Add a spellbook shortcut above the hand

Authorized 2026-10-06: confirm that experimental book sharing opens the existing owner-style book, then add an above-hand SPELLBOOK shortcut whenever the viewed hero has a book. Reuse the same read-only drawer and projected permissions. Keep it clear of the hand, Director controls, minimized prompt and adjacent action controls at both supported sizes. T2 closing checks: shared book parity/visibility without casting authority; real-pointer shortcut open/close and hover; supported-size coexistence and input blocking; directly affected HUD/spellbook regressions, visual review, documentation validation and local commit. No gameplay, protocol, hero integration or build change.

Completed 2026-10-06: the shortcut opens the existing projected book; independent sharing options preserve owner-view parity without granting spell actions. Hosts see SPELLBOOK beside Director; other players get its position. A measured collision with the third hero tray required the book control row to follow the tray bottom, with confirmation placed alongside when its usual lower lane cannot fit. Hero switches update only this row. Failing-then-passing visibility/clearance checks, real-pointer open/close/commit, affected HUD/spellbook checks and both-size captures passed. Review found no rules, content, protocol or privacy expansion. Existing packages are unchanged; no hero merge or publication.

Historical evidence: [9.64h completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-964h-completion).

### [x] 9.65 Integrate the completed hero and prepare the next private build

Requested as the later morning follow-up, not part of this overnight implementation run. Owner: merge/build integration. Dependencies: completed 9.54 in the hero checkout, 9.61 and 9.64e; preserve both worktrees and earlier packages. Outcome: merge the completed hero with the experimental/map work, reconcile protocol/snapshot/composition changes, and create the next private playtest package. Closing checks: explicitly audit final NebKher decisions for timer ownership, every timeout fallback and no-strand progress; verify his board interactions on both maps; run merge-affected regressions and the highest applicable gate; copy/check the approved private map artwork with `Tools/Prepare-AlternateBoardArtwork.ps1`; build once and complete required player launch, board and host/client checks with experiments off/on. No two-lane map, new gameplay rules, remote push or upload. Resume only after the hero gate and the agreed morning handoff.

Completed 2026-10-06: merged the completed thirty-two-hero line with the experimental/map line, retaining snapshot schema 59 and advancing the integrated private wire to 42. The merge gate found and corrected the alternate-map Fast Travel fixture's missing neutral rule-modification argument. Full T3 offline/Edit/Play breadth, merge-focused hero/map/experimental checks, assembly/map/private-art checks and review passed. The exact Windows player passed launch, board-render and host-plus-two-client checks with experiments off and on; the separately named private package is `Builds/Playtest-9.65-integrated.zip` (184 files, SHA-256 `e43cc11f048fdd54982057e823c74750d3c40e6067bb2b5fe5da39dbe2eb7830`). ZIP bytes match the loose package and the packaged executable passed its scripted smoke. No two-lane map, rule change, remote push or publication occurred.

Historical evidence: [9.65 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-965-completion).

### [x] 9.67 Collapse simultaneous allowance clocks into one gold countdown

Authorized 2026-10-07 by the user's screenshot and direction: "Why 4 timers instead of just
gold... they are not the same time lol." Outcome: when more than one current non-bank allowance
clock is visible for a simultaneous decision such as card commitment, the header shows one gold
countdown rather than one badge per distinct remaining second/team. The displayed value is the
longest remaining allowance, because the simultaneous window remains open until its last current
participant leaves it; individual authoritative clocks and timeout behavior remain unchanged.
A single-owner allowance retains its team colour, and active team-bank clocks remain separately
coloured because they are distinct team resources. No clock authority, readiness, timeout, wire,
snapshot, or header-dimension change. Closing check: a four-seat commitment display with deliberately
different remaining values fails on the current renderer with four badges and passes with one gold
badge showing the latest expiry; retain the existing single-clock, mixed allowance/bank, two-bank,
clock-only refresh, and supported-viewport checks. T2.

Completed 2026-10-07: the header now treats every set of simultaneous current non-bank allowances
as one shared display. It shows the latest remaining allowance once in gold, while a lone allowance
retains its team's colour and active team banks remain separate. The four-seat staggered commitment
regression failed before the fix with four main clock badges ("Expected: 1; But was: 4") and passed
after with one gold 01:51 badge; the existing single allowance, mixed allowance/bank, two-bank,
clock-only refresh, unchanged 96-unit header and 1280x720/1920x1080 checks passed in the same focused
Play Mode fixture (1/1). The final 1920x1080 capture was inspected: one centred gold main clock is
visible, with the red and blue bank previews remaining at their sides. Authority clocks, timeouts,
wire 42, snapshots and the existing package are unchanged.

Historical evidence: [9.67 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-967-shared-allowance-clock-completion).

### [x] 9.69 Add an experimental Minion Battle removal cap

Authorized 2026-10-07 by the user's request for a changeable Minion Battle removal cap that may be
zero. Outcome: the host's Experimental menu exposes an independent opt-in whole-number cap. When
enabled, one Minion Battle removes at most that many effective losing units in total; `0` removes
none. The ordinary count difference, losing-side candidates, Heavy-last ordering and chooser remain
authoritative below the cap. Disabled preserves the official rules. The immutable match setting must
survive restart, replay, retained-prefix Undo, snapshot restore and joined-seat projection; a setting
change is a compatibility boundary, not mutable mid-match state. Advance the experimental snapshot
envelope and private wire rather than best-effort reading older settings. Closing checks: negative
values reject while zero is accepted; disabled, zero, below-difference and non-binding caps; automatic
and choice removals; Heavy-last and counts-as-minion regressions; Lane Push only when the capped
removals actually empty the losing side; replay/Undo/snapshot equivalence and mismatched-setting
atomic refusal; host-menu reset/input validation; options wire round-trip and prior-wire rejection.
Run focused Unity Edit Mode for the owning Core/Application/presentation/network fixtures and directly
affected Minion Battle/Lane Push regressions, then the applicable broader tier selected by testing
policy. No two-lane map, timer, hero-content, official-rule or package change. T3 because the immutable
gameplay option changes authoritative resolution, snapshot validation and the private protocol.

Completed 2026-10-07: the host's independent Experimental toggle exposes a non-negative cap, with
zero removing nothing and a default enabled value of one. The cap is applied once to the ordinary
derived removal count before forced or chosen Heavy-last resolution; Lane Push follows the resulting
board. Immutable options reach restart/replay/Undo and capped pending-choice restore, use experimental
snapshot envelope 4, and advance the private wire to 43. The T3 offline, focused Unity, complete Edit
and complete Play gates passed. Review found no official-rule, timer, coordination, hero, map or
package change; the 9.65 package remains unchanged.

Historical evidence: [9.69 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-969-minion-battle-removal-cap-completion).

### [x] 9.70 Replace game-length modes with configurable Waves and Lives

Authorized 2026-10-07 as the first outcome in the user's explicit **9.70 -> 9.71** sequence,
after 9.69 completes. Remove the player-facing Short Game and Long Game choices while retaining
2v2 and 3v3. The host screen exposes positive bounded whole-number starting Wave counters and
starting Life counters per team. Fresh 2v2 defaults to 3 Waves/4 Lives and fresh 3v3 to 3 Waves/5
Lives; the printed Quick/Long rows remain source-reference fixtures, not player-facing modes. The
chosen values are immutable composition inputs across both maps, hero selection/recomposition,
restart, replay, retained-prefix Undo, snapshot restore, private-session compatibility and joined
client inheritance. Closing checks: lower/upper/input bounds; both team sizes and maps; default and
custom setup composition; host-before-listener identity; format mismatch and automatic inheritance;
restart/replay/Undo/snapshot equivalence; Waves/Lives HUD and all three victory causes; removal of
the Short/Long controls with 2v2/3v3 still directly selectable. Advance compatibility only where
wire or serialized shape changes. T3 because setup identity, session admission and the complete
match loop change together. No two-lane map, hero rule, Experimental behavior, package or
publication change.

Completed 2026-10-07: the host screen retains direct 2v2/3v3 selection and replaces Short/Long
with bounded positive Waves and Lives-per-team fields. Former Quick values are the defaults;
official Quick/Long rows remain reference fixtures. Custom values receive canonical admission
identities and survive both maps, automatic join inheritance, hero recomposition, projections,
restart, snapshot restore and retained-prefix Undo. Existing serialized Wave/Life values and the
format string changed value rather than shape, so wire 43, snapshot schema 59 and experimental
envelope 4 remain current. Focused and complete T3 offline/Edit/Play gates passed, including the
real-TCP custom inheritance journey and reviewed 1920x1080 host capture. No Experimental rule,
hero, two-lane map, package or publication changed.

Historical evidence: [9.70 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-970-configurable-setup-completion).

### [x] 9.71 Add experimental one-Life hero base cost

Authorized 2026-10-07 as the second outcome in the user's explicit **9.70 -> 9.71** sequence.
Depends on completed 9.70. Add an independent host Experimental toggle. Enabled replaces p.13's
Tier-derived base Life cost for a defeated hero with 1, then applies every existing additional-Life
modifier normally: Bounty or Ursafar's Tear therefore makes the final cost 2. Assist coins remain
the defeated hero's lowest-card-Tier value and the defeating hero's own coins remain the defeated
hero's exact Level. Hero-tray worth badges retain their current Tier-derived 1/2/3 number but become
yellow-only while enabled, because they then communicate assist coins and no longer Life loss;
situational Life modifiers do not change that badge. Disabled preserves official behavior. The
immutable option survives restart, replay, retained-prefix Undo, snapshot restore, private wire and
reconnect. Closing checks: Tier I/II/III disabled/enabled costs; additional attacker and defeated-
holder modifiers; unchanged ordinary/tripled assists and exact-Level defeat coins; yellow-only
Tier-number tray presentation at supported viewports; option reset, round-trip, mismatch refusal,
restore/replay/Undo/reconnect; official option-off regressions. T3 because authoritative defeat,
snapshot compatibility, network options and visible HUD presentation change together. No package,
map, timer, Minion Battle, hero-content, remote or publication work.

Completed 2026-10-07: the independent option is immutable per match and changes a defeated hero's
base Life cost to 1 before every existing additional-Life modifier. Assist coins remain Tier-derived,
the defeating hero still earns the defeated hero's exact Level, and tray badges retain their Tier
number while becoming yellow-only. Restart, replay, retained-prefix Undo, restore, projections,
reconnect and both host/join views retain the option. Its added serialized field advances private wire
to 44 and the experimental snapshot envelope to 5; Core snapshot schema 59 is unchanged. Focused and
complete T3 offline/Edit/Play gates passed, and the enabled match plus host control were reviewed at
1920x1080. Disabled official behavior is unchanged; no package or publication was produced.

Historical evidence: [9.71 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-971-experimental-one-life-hero-base-completion).
