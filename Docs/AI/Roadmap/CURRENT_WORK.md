# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

W-020 to W-024 form one approved serial sequence (user, 2026-10-10): record the PvE decisions, then skeleton, rules, presentation and package. Each item closes with its own local commit; nothing is pushed.

### [~] W-024 PvE test package

Depends on W-026: the 2026-10-11 candidate package (`Builds/Playtest-W024-pve-test.zip`) was superseded by the playtest report and is rebuilt after W-026.
Approved: package and smoke the PvE executable.
Outcome: verified private PvE ZIP with content data beside the executable.
Scope: no existing-mode package.
Closing checks: one PvE build with its smoke markers, exact-artifact launch, per-file and ZIP SHA-256; existing player smoke and assembly-absence check.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result for approval. W-013's later explicit as-is build request authorizes packaging the candidate without closing this separate design-approval gate.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; W013 includes the candidate under the later as-is request, with no further package authorized. Exact evidence lives in the candidate commit body.
