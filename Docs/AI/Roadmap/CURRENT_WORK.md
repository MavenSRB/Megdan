# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result to the user for approval before any new build.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's follow-up lowering is implemented: four reference units lower, unchanged tray dimensions. The focused render check passed at all three widths; revised preview: `tmp/w004-banner-lowered-preview.png`, full views `tmp/w004-lowered-banner-1280.png`, `tmp/w004-lowered-banner-1920.png`, `tmp/w004-lowered-banner-2560.png`. The user owns the outstanding visual approval; no new package until separately authorized. Exact checks are in the candidate commit body. Prior candidate `43aa907d` and `tmp/w004-banner-approval.png` remain preserved.
