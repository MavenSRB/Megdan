# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [ ] W-033 PvE enemy target categories

Approved superseding clarification from the side conversation: Heavy counts as a hero only; Melee and Ranged count as BOTH heroes and minions for targeting/effects. Combined queries/effects count or affect each enemy once. Shared targeting/effect engine support is explicitly authorized, including unchanged minion-only cards such as High Voltage. No card definitions change for this category work. Preserve combat, Defense, defeat, decks and turns. Execute after W-034. Closing checks: hero-only, minion-only and combined queries/effects, real unchanged card execution, ownership, combat preservation, pending restore/replay, co-op identity and existing-mode isolation; reassess tier for the selected identity implementation.

Prior investigation: the minion-only category prototype cannot satisfy intrinsic-minion output/Defeat/adjacency contracts; its private patch `tmp/w033-category-prototype.patch` was removed from executable source. Shared-engine scope approval is now resolved; the earlier mapping is superseded. Reuse only applicable investigation evidence, not the old mapping or a claim of complete coverage. Exact prior evidence lives in checkpoint `f92f3f33`.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result for approval. W-013's later explicit as-is build request authorizes packaging the candidate without closing this separate design-approval gate.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; W013 includes the candidate under the later as-is request, with no further package authorized. Exact evidence lives in the candidate commit body.
