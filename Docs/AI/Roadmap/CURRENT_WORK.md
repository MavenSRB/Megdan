# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

W-020 to W-024 form one approved serial sequence (user, 2026-10-10): record the PvE decisions, then skeleton, rules, presentation and package. Each item closes with its own local commit; nothing is pushed.

### [~] W-021 PvE skeleton executable and static encounter

Approved: a separate PvE executable that composes a static encounter (chosen heroes on the placement spaces, enemies and creeps placed, no enemy decisions) on the existing match scene, with existing-mode behavior unchanged.
Outcome: PvE assemblies outside `Assets/GuardsDigital`, enemy and encounter content with validation, composer, launcher scene and PvE build entry; additive result/configuration factories, one-shot external composition and a per-side accent palette.
Scope: no PvE rules hooks, enemy decisions or overlays yet; existing scene, build settings, snapshot v59, wire /52 and package unchanged.
Closing checks: existing-mode seeded snapshot golden captured before any shared change and unchanged after; focused composer/content/assembly-boundary/palette checks; PvE launcher Play Mode composition; existing player smoke; PvE build smoke; PvE assemblies absent from the existing build.

### [ ] W-022 PvE rules and engine hooks

Approved: implement PVE-D03 to PVE-D23 rules over additive default-off engine hooks.
Outcome: restore-stable mode rules; end-of-round step availability; coin suppression; placement-space respawn; placement and level-grant system transitions on existing commands; discard-or-defeat policy; plan derivation, enemy controller, tie prompts, encounter session guard/Undo and loss detection.
Scope: rules and session layer; no new command kinds or wire change; no presentation beyond what tests need.
Closing checks: per hook a default-off regression, rules-on behavior and restore/rollback/checkpoint-Undo survival; deterministic headless encounter; existing-mode golden, codec, seat and pinned constants; full Edit Mode and Play Mode gate.

### [ ] W-023 PvE presentation

Approved: player-facing PvE journey.
Outcome: setup screen, placement and level-grant panel, enemy plan strips with card hover, placeholder enemy tokens, tie prompts, paced enemy actions and loss screen; HUD rail and status options for more than three heroes.
Scope: PvE assemblies plus additive, default-off presentation hooks.
Closing checks: focused Play Mode journey, non-batch visual capture, existing HUD layout regressions with options off, full Play Mode gate.

### [ ] W-024 PvE test package

Approved: package and smoke the PvE executable.
Outcome: verified private PvE ZIP with content data beside the executable.
Scope: no existing-mode package.
Closing checks: one PvE build with its smoke markers, exact-artifact launch, per-file and ZIP SHA-256; existing player smoke and assembly-absence check.

### [!] W-004 Compact player-name banner for approval

Approved: restore original slim tray geometry and replace the expanded name row with a small top-border banner. Show the rendered result for approval. W-013's later explicit as-is build request authorizes packaging the candidate without closing this separate design-approval gate.
Scope: nameplate styling only; no gameplay, spell-preview or package changes.
Closing checks: focused nameplate geometry/render test at 1280/1920/2560 widths, original 12-unit top padding, banner clear of hero text/portrait (border overlap allowed), long/empty names; captured preview and user visual approval. T2, no unrelated suites. Human approval is the final prerequisite.

The user's second revision is implemented: two more reference units lower, font 11 to 12, unchanged tray dimensions. The focused render check passed at 1280/1920/2560 widths, including hero text/portrait clearance. Revised preview: `tmp/w004-banner-larger-preview.png`; full views: `tmp/w004-larger-banner-1280.png`, `tmp/w004-larger-banner-1920.png`, `tmp/w004-larger-banner-2560.png`. Visual approval remains outstanding. Prior candidates (`43aa907d`, `bb06f8c3`) and captures remain preserved; W013 includes the candidate under the later as-is request, with no further package authorized. Exact evidence lives in the candidate commit body.
