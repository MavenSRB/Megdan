# Work completion index

Return to [archive index](README.md). Post-phase work records live here after completion; current plans live in [current work](../Roadmap/CURRENT_WORK.md). Preserve request, acceptance and outcome once here; exact validation evidence lives in the linked commit body. Tags are immutable local work/ID tags.

| ID | Outcome | Commit |
| --- | --- | --- |
| W-001 | Adopted iteration work records, PvE design routing and change-based build validation. | `work/W-001` |
| W-002 | Side-column inspection/pip/rail spell previews, bounded top-tray placement and unclipped player names. | `work/W-002` |

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
