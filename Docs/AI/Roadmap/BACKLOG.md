# Shared backlog

Return to [workflow](../ROADMAP.md). This is the single intake and prioritization index for both tracks. Selection and authorization live in the [brief](../SESSION_BRIEF.md); approved unfinished plans live in [current work](CURRENT_WORK.md). Existing IDs retain their precise definitions at the linked owners.

Record new reports here with status, observable outcome, evidence/reproduction, dependencies and closing check. Proposed, parked and blocked work is not authorization. On selection, allocate a W-NNN record and retain the original issue link; on completion move the record to [history](../Archive/WORK_LOG.md). Do not keep completed batches in startup context.

## Current direction

| Item | Status | Outcome | Dependencies / closing check |
| --- | --- | --- | --- |
| Existing game playtest feedback | Intake | Iterate, test and fix the private game | Concrete report/request; reproduce, fix and verify affected paths. No unspecific repair campaign. |
| Large maps | Planned expansion; not yet selected for implementation | Add large maps to the existing game | Existing 9.62 plan below; reviewed setup and lane rules, then bounded implementation and map/player-count gates. |
| PvE design | First slice approved (W-020); later questions open | Approved [decisions](../Project/PVE_DESIGN.md) for the single-player test encounter | Open questions in the design owner need user decisions before implementation. |
| PvE implementation | Approved sequence W-021 to W-024 | Separate PvE test executable | [Current work](CURRENT_WORK.md) records; [delivery stages](PVE.md). |
| PvE level lowering | Deferred by user 2026-10-10 | Lower a hero's level between rounds (the "-1" test control) | Needs a state-derivable revert of upgrade, item and Ultimate placement; refused when inconsistent. Focused progression, snapshot and Undo checks. |
| PvE later content | Proposed; not authorized | Win condition, real enemy content and balance, co-op, boss cards, enemy skills, Vorty's truncated note | User decisions recorded in the design owner first. |
| Living-document hard thresholds | Pre-existing at `work/W-019` (found 2026-10-10); not yet scheduled | Living-document validation passes again | `CURRENT_IMPLEMENTATION.md`, `EXPERIMENTAL_FEATURES.md`, `MATCH_HUD.md` and `PRIVATE_ONLINE_ARCHITECTURE.md` exceed the 4,500-word project-leaf limit. Closing check: move precise authority into indexed shards per governance (no compression) until the validator reports no hard-threshold error. |
| `HeroDefeatBaseLifeCostIsOne` restore loss | Defect found 2026-10-10; not yet scheduled | The experiment survives rollback and checkpoint Undo | It is bound only on fresh composition and string restore (`MatchAggregate` lines ~346 and ~402), not on authoritative deserialization. Closing check: an experiment match keeps base Life cost 1 after rollback and after Undo past the checkpoint interval. |

## Carried-forward work

The 2026-10-08 phase closure preserves these dispositions. Summaries route to exact acceptance and prerequisites; they do not replace them. Apply current change-based testing policy when an item is selected, while retaining substantive acceptance.

| Item / exact owner | Disposition | Outcome and dependency | Closing check |
| --- | --- | --- | --- |
| [8.29c](PHASE_8_HANDS_ON_CORRECTIONS.md#829c-restore-the-director-undo-player-path) | Proposed; needs current-state triage | Director Undo path; determine whether later Undo fixes discharge this old report before scheduling | Named real-pointer Undo journey and retained-prefix oracle in owner. |
| [8.35z87](PHASE_8_CLEANUP.md#835z87-retire-the-hero-local-selections-889-and-891-proved-equivalent) | Parked, opportunistic only | Retire equivalent hero-local selections when an affected hero is otherwise changed | Differential/card family proof, affected printed-set and journey gates; preserve compatibility. |
| [8.35z75](PHASE_8_CLEANUP.md#835z75-card-authoring-workbench) | Parked | Authoring workbench; product and content/privacy decision | Approved authored variant validates and runs. |
| [8.35z76](PHASE_8_CLEANUP.md#835z76-card-zoom-and-inspection) | Parked; triage against later inspection work | Small-display inspection; exact interaction scope | Real supported viewport interaction without input regression. |
| [8.35z77](PHASE_8_CLEANUP.md#835z77-additional-sandbox-authoring-controls) | Parked | Director-only authoring controls; selection/rule semantics | Placement workflow and network-role exclusion. |
| [8.35z78](PHASE_8_CLEANUP.md#835z78-stronger-practice-bot-tier) | Historical route, not a duplicate task | Implemented tactics belong to 8.36g; further planning belongs to 8.36i-k | Preserve those owners; do not reopen this old entry. |
| [8.35z79](PHASE_8_CLEANUP.md#835z79-resolve-the-prepared-designer-question-list) | Blocked | Reconcile private designer questions; user/designer answers required | Source, answer/disposition and affected capability mapping. No sending messages authorized. |
| [8.36i-k](PHASE_8_PRIVATE_GAME_CORRECTIONS.md#future-four-round-lobby-bot-planning-parked-not-authorized) | Parked | Measured full-turn prototype, four-round planning, then five-bot hardware gate | Explicit scope, prototype go/no-go, permitted information, held-out decision evidence and agreed hardware budgets. PvE does not automatically reopen this. |
| [9.62](PHASE_9_EXPERIMENTS_AND_MAPS.md#962-implement-the-two-lane-maps-for-4v4-and-5v5) | Planned; prerequisites unresolved | Two-lane 4v4/5v5; reviewed maps, setup and lane rules. User reaffirmed large-map intent during W-001 | Both player counts, independent pushes, whole-match victory, relevant heroes and persistence/network/UI checks. |
| [9.80](PHASE_9_EXPERIMENTS_AND_MAPS.md#980-cut-the-current-private-windows-playtest-build) | Paused by user | Historical package request; newer packages exist | Preserve the pause and unearned checks. Resume only if specifically selected; use current build policy, not its obsolete automatic T3 instruction. |
| [Phase 10](PHASE_10.md), [post-pitch](POST_PITCH.md) | Parked | Pitch/licensed-product work is outside current direction | Requires a new product decision; historical checks retained. |
| [Active risks](../TECHNICAL_RISKS.md) and their coverage gaps | Retained, trigger-driven | Risk reassessment stays with the canonical register | Use each entry's evidence/trigger; do not silently close stale-looking claims. |

Other completed phase work remains historical. The inventory includes every open/blocked heading found in the phase roadmap at transition; routing-only duplicate 9.62 is not a second item.
