# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [~] W-010 Gydion starts with all spells discarded

Approved: Gydion starts with every spell discarded. In the existing spell lifecycle this means all twenty-two start faceup in `RemovedFromSpellbook`, not facedown in `Spellbook`; Prepare Spells remains the existing route that returns them to the book.
Scope: supersede GY-Q1 at standard Gydion setup and its faithful staged configurations, update current rule/content authority and affected setup/UI expectations; preserve casting, refill, visibility, definition/instance, command, wire/snapshot and package contracts.
Closing checks: failing-then-passing setup regressions for 0 in book / 22 removed and public faceup visibility, affected Gydion dealing/UI/refill paths, rule-authority review, living-document validation and diff review. T2; no unrelated suites.

### [ ] W-011 Final-ten-second map countdown

Approved next in sequence: during the last ten seconds of the active turn allowance or team bank, show `10` through `1` in the existing large red phase-banner typography, centred across the map with no background.
Scope: presentation over the existing authoritative clock projection only. The full-screen layer and descendants ignore picking so pointer hover, click, drag and commands behave as if it is absent; no clock duration, readiness, expiry, command, wire/snapshot or package changes.
Closing checks: focused clock/view regressions for normal and bank countdowns, integer/visibility boundaries, pointer transparency and ordinary command passage, plus a non-batch visual capture. T2; no unrelated suites.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result to the user for approval before any new build.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; no new package is authorized. Exact evidence lives in the candidate commit body.
