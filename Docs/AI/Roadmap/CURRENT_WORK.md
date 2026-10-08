# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result to the user for approval before any new build.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, no name/identity overlap, long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

Implementation and focused check are complete; captured preview is `tmp/w004-banner-approval.png` (full views `tmp/w004-name-banner-1280.png`, `tmp/w004-name-banner-1920.png`, `tmp/w004-name-banner-2560.png`). The user owns the outstanding visual approval; no new package until separately authorized. Exact validation evidence is in the local candidate commit body. Keep this item open until the user approves or requests revision.
