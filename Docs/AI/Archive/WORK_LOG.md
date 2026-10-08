# Work completion index

Return to [archive index](README.md). Post-phase work records live here after completion; current plans live in [current work](../Roadmap/CURRENT_WORK.md). Preserve request, acceptance and outcome once here; exact validation evidence lives in the linked commit body. Tags are immutable local work/ID tags.

| ID | Outcome | Commit |
| --- | --- | --- |
| W-001 | Adopted iteration work records, PvE design routing and change-based build validation. | `work/W-001` |
| W-002 | Side-column inspection/pip/rail spell previews, bounded top-tray placement and unclipped player names. | `work/W-002` |
| W-003 | Private Windows package with W-002 fixes; one build, exact-artifact smokes and verified ZIP. | `work/W-003` |
| W-005 | Restored both whole blue side zones on the alternate map; revision-2 identity, movement/travel and rendered-map regressions. | `work/W-005` |
| W-006 | Simplified tied Initiative to candidate card faces with per-player ping accents and hero-token preference markers. | `work/W-006` |
| W-007 | Retained `WHO ACTS FIRST?` and centred each selector's hero token above their highlighted card. | `work/W-007` |

### [x] W-007 Tied-Initiative title and centred selector token

Approved by the user's follow-up to W-006: retain the title `WHO ACTS FIRST?` and place each selecting hero token in the middle above its chosen card.
Outcome: the title is the tied-card screen's only upper text. Each public preference retains the W-006 player-colour ping accent, while its hero token now occupies a stable centred row over the selected card. Eyebrow, explanatory and participant sentences, button captions and viewer-only selection treatment remain absent; candidate hover still uses the standing board/plaque identity channel.
Scope: W-006 tied-Initiative layout correction and focused coverage only. No Initiative rules, agreement semantics, other decision families, commands, wire/snapshot contract or package changed.
Closing checks: T2 focused decision-window Edit Mode regressions; affected real-input Play Mode journey; non-batch 2560x1440 capture with title, token/card centring and unclipped-panel geometry checks plus visual review; living-document validation and diff review. No broad-risk trigger or unrelated full suites. W-004 remains at its independent visual-approval gate.

Historical evidence: completion commit addressed by immutable `work/W-007`; exact checks, results and limitations live in its body.

### [x] W-006 Tied-Initiative card selection markers

Approved by the user's 2026-10-09 instruction to remove the upper text from the `WHO ACTS FIRST?` screen, clarified to show only what each participant selected using the pinged-hex effect, player colour and hero token above the card.
Outcome: the expanded tied-Initiative window shows only the candidate card faces. Each stated preference paints that participant's stable player accent around the selected face with the established steady-plus-pulse timing and places their small hero portrait above it; simultaneous different preferences remain visible and revising a statement moves the identity marker. The prior title, eyebrow, explanation, participant sentences, button captions and viewer-only selection outline are absent. Candidate hover still links to the standing board/plaque identity cue, and rebuilding after a click clears removed-control focus so that cue cannot stick.
Scope: tied-Initiative presentation and focused coverage only. No Initiative rules, agreement semantics, other decision families, coordination commands, wire/snapshot contract or package changed.
Closing checks: T2 focused presenter/view Edit Mode regressions; real-input seat-scoped Play Mode journey; non-batch 2560x1440 capture with token/card/panel geometry checks and visual review; living-document validation and diff review. No broad-risk trigger or unrelated full suites. W-004 remains at its independent visual-approval gate.

Historical evidence: completion commit addressed by immutable `work/W-006`; exact checks, results and limitations live in its body.

### [x] W-005 Restore both alternate-map blue side zones

Approved by the user's 2026-10-09 instruction to fix the entire lower-left and upper-right blue regions omitted from movement. The original import incorrectly classified their 30 fully gridded spaces as blocked water. Printed rulebook p.19 and the user-confirmed zones govern the correction.
Outcome: two neutral 15-space playable off-lane zones; original lane, spawns, rocks, artwork and coordinate inventory preserved. Runtime and offline catalogs select map revision 2 and reject obsolete revision 1. Existing package remains unchanged.
Closing checks: both movement regressions failed before correction and passed afterward; focused map, movement, Fast Travel, setup, Lane Push, snapshot/Undo, revision rejection, rendered tile classification and host/join checks; map/resource and living-document validation, diff review. T2. No hero-specific behavior, new build or unrelated full suites. W-004 remains at its independent visual-approval gate.
Historical evidence: completion commit addressed by immutable `work/W-005`; exact checks, results and limitations live in its body.

### [x] W-001 Transition to iteration and PvE design

Approved by the user's 2026-10-08 request to update documents and agent workflow, including the instruction that new builds must not rerun hours of tests for untouched behavior. The user also reaffirmed large maps as planned existing-game work; the 9.62 proposal and prerequisites are carried forward, not cancelled or implicitly started.

Outcome: compact startup context, shared backlog, separate PvE design/delivery track, preserved history and rules boundaries, and change-based package validation.
Scope: governing documents and documentation validator support. No gameplay changes, PvE mechanics, builds or publication.
Closing checks: ownership and pending-work review; preserved superseded text and links; validator positive/negative cases for work routes and archive evidence; living-document validation and git diff check. No Unity.

Historical evidence: completion commit addressed by immutable `work/W-001`; exact checks/results and limitations live in its body.

### [x] W-002 Spell previews on inspection and token pips

Approved by the user's request to connect casting-card spell hover to current-card inspection and the little card pips, plus verification of top-position tray clipping.
Outcome: inspection and Shift-hover pip cards show their typed spell faces, with public removed spells dimmed. Non-hand inspection uses a side column; the player's own hand retains the above-card column. Top-tray previews remain within available space. The user's follow-up also authorizes fixing player names clipped behind hero identity panels, with focused layout regression coverage.
Scope: presentation only, reuse the visibility-safe spell model and artwork. No commands, rule, content, wire or snapshot changes; no new package requested.
Closing checks: focused preview/visibility/placement Edit Mode regressions, real-pointer inspection and Shift-pip Play Mode journeys, top-rail and short-viewport bounds, dismissal/refresh and no-command checks; targeted rendered inspection. T2; no unrelated suites.

Completed: requested preview surfaces share the typed spell column; own-hand layout remains above. Player names have a reserved non-overlapping line. Focused checks and rendered review passed; no package built.

Historical evidence: completion commit addressed by immutable `work/W-002`; exact results, initial failure/correction and limitations live in its body.

### [x] W-003 Package the completed inspection fixes

Approved by the user's request for a new build after W-002. Outcome: a private Windows package containing the completed spell-preview and player-name fixes, with a distinct build stamp and launch instructions.
Scope: package identity, build once, artifact validation and local packaging; no gameplay changes or publication. Preserve prior packages.
Closing checks: reuse W-002's 46 Edit/6 Play green checks and visual evidence; compare against 9.100; fresh player interaction, host-plus-two-client and board-render smokes on one binary; package file/ZIP hashes and packaged launch; documentation validation. No broad-risk trigger or full-suite run.

Completed: `Builds/Playtest-W003-inspection-fixes.zip`; all artifact checks passed and prior packages preserved.

Historical evidence: completion commit addressed by immutable `work/W-003`; exact artifact identity, checks and limitations live in its body.
