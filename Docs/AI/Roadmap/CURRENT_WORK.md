# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [!] W-033 PvE enemy target categories

Approved latest clarification: Heavy counts as a hero; Melee and Ranged count as minions for targeting and effects. Change enemy types only; no card definition changes. Preserve combat, Defense, defeat, decks and turns. PvE only; one logical identity, no double counting. W-032 is complete. Closing checks: hero-only, minion-only and combined queries/effects, ownership, combat preservation, restore/replay and existing-mode isolation; T2 focused rules and PvE integration coverage.

Prerequisite: approval to extend shared effect identity/consumption contracts beyond an enemy-type field while keeping every card unchanged. The category-only prototype handled attack narrowing, presence and object/hero selections, but cannot satisfy minion-identity-only chains: `WaspEffectContent.HighVoltage` binds a Minion output to DefeatMinion and then adjacency; `SyntheticTargetingCapabilities`' minion selection filters intrinsic Minion identities. Coordinate-only adaptation is insufficient, and a fabricated MinionId would not preserve identity contracts. Active radius/token scopes also need category-aware derivation. Do not ship the partial prototype. Its patch is preserved in `tmp/w033-category-prototype.patch`; working executable source is restored to W-032. No active-test failure was observed; acceptance is incomplete. On approval, cover real unchanged minion card execution, Heavy exclusion, typed output consumers, effect scopes, pending restore/replay and co-op identity, then reassess validation tier before implementation. Evidence for the bounded investigation lives in its local checkpoint commit.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result for approval. W-013's later explicit as-is build request authorizes packaging the candidate without closing this separate design-approval gate.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; W013 includes the candidate under the later as-is request, with no further package authorized. Exact evidence lives in the candidate commit body.
