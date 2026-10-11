# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [ ] W-033 PvE enemy target categories

Approved latest clarification (supersedes the earlier both-categories request): Heavy counts as a hero; Melee and Ranged count as minions for targeting and effects. Preserve current combat, Defense and defeat rules. PvE only; one logical identity, no double counting. Execute after W-032. Record exact approved rule in PvE design before implementation; inspect typed target/query/effect boundaries and verify hero-only, minion-only and combined effects, ownership, combat preservation, replay and existing-mode isolation at the selected risk tier.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result for approval. W-013's later explicit as-is build request authorizes packaging the candidate without closing this separate design-approval gate.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; W013 includes the candidate under the later as-is request, with no further package authorized. Exact evidence lives in the candidate commit body.
