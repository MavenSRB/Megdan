# Phase 9 - Experimental features and alternate maps

Canonical milestone definitions. Return to [ROADMAP.md](../ROADMAP.md). The [session brief](../SESSION_BRIEF.md) owns active authorization.

## Experimental timer design, documentation only

### [x] 9.55 Record the experimental timer and header design

Authorized 2026-10-05 by the user's instruction to write the agreed discussion into docs, include the in-progress hero's choices, and ensure timeout cannot strand a match; amended to restore clocks on Director Undo. Scope: record the per-match Experimental menu, timer/settings/timeout and header decisions, NebKher coverage prerequisite, and progress acceptance checks. No runtime implementation, Unity launch, publication, or change to the existing hero sequence. T0 closing checks: review against the conversation and current choice inventory, living-documentation validator, and `git diff --check`; local documentation commit and archive index.

Completed 2026-10-05: the [canonical design](../Project/EXPERIMENTAL_FEATURES.md) records the user decisions and unexecuted acceptance obligations.

Historical evidence: [9.55 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-955-experimental-timer-design-evidence).

### [!] 9.56 Implement and gate the experimental timer and header

Authorization amended 2026-10-05: follows 9.58 in the isolated worktree; 9.64 then builds the private playtest package. The revised three-hour window defers map 9.61. Owner: timer implementation milestone. Outcome: the opt-in per-match timer and associated header follow [the design](../Project/EXPERIMENTAL_FEATURES.md), including icon-only clocks and Director Undo restoration. Dependencies: completed 9.58 on this worktree baseline. The 2026-10-05 amendment permits parallel implementation before 9.54; gate the available roster here, then re-audit final NebKher choices and reconcile snapshot/wire changes before a merged release containing him. Closing checks: the design's complete choice/timing matrix, timeout-progress and restoration/network checks, timer-off standard-game regression, and fixed-size header visual checks, at the highest tier selected by [testing policy](../Testing/POLICY.md). No new experiments or expanded player Undo rights. Source seams and proposed implementation/test contracts are in [the preparation packet](../Project/EXPERIMENTAL_PREPARATION.md).

Implementation in progress: host-authoritative allowance/bank accounting, timeout selection, readiness/pause transport, snapshot/Director Undo checkpoints, shared-board voting and the fixed-size header are implemented but not yet accepted. Focused checks exposed and corrected clipped status text, a malformed heart glyph and lost restored ballots; final regressions remain required. The full offline baseline passed; Unity breadth and final choice/response checks are running. No timer release claim is made. Keep NebKher out of this candidate, as agreed after the ETA discussion. The user requested a shorter final build check: build once, reuse passing evidence and focus the built-player check on launch, Host/Join and experimental controls (target 20–25 minutes).

## Teammate coordination design, documentation only

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

### [~] 9.64 Build the experimental playtest player

Authorized 2026-10-05 when the user set three remaining hours and required build time to be included. Outcome: a private Windows playtest build containing the validated experimental changes, with the existing single Host/Join entry point. Dependencies: the experimental feature gates; preserve the independent hero checkout and reconcile it only under merge authorization. The original build reservation was 45 minutes before 19:00 UTC; the user subsequently directed shorter build testing, targeting 20–25 minutes with one build and focused launch/Host/Join/experimental-control checks. omit optional map 9.61 from this playtest scope. Closing checks: build result and executable identity, ordinary launch/Host/Join, experimental settings and teammate controls in the built player, applicable player-build/board-visual/format smoke recipes, and no included private logs or source artwork beyond the local build's required resources. Keep the package private; no upload, remote push, or distribution is authorized.

Latest 9.64 amendment: produce the current Windows candidate without launching it or running smoke tests. Closing scope is successful compilation/build, correct private runtime packaging, executable identity and an explicit untested label. This replaces the earlier smoke requirement for this package only; it does not mark the suspended 9.56 gate complete. NebKher and the alternate map remain excluded.
