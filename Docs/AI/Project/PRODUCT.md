# Product Scope and Durable Constraints

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Mission

Maintain and improve a private, noncommercial game for the user and friends. The current plan is to iterate, playtest and fix the existing game while designing PvE. Implementation will follow bounded approved design slices. The configured roster and shipped behavior are owned by [CURRENT_IMPLEMENTATION.md](CURRENT_IMPLEMENTATION.md).

Use [PvE design](PVE_DESIGN.md) for original-mode decisions and [workflow](../ROADMAP.md) for delivery. Publisher-pitch, licensed-product expansion, public services and platform expansion remain parked. Prior direction is preserved in the [pre-transition record](../Archive/PRODUCT_BEFORE_ITERATION_2026_10_08.txt).

## Rights and distribution boundary

- On 2026-08-13, the user stated that they have relevant permission to publish this source repository and the included rulebook. This records the user's authorization for those artifacts; it is not a legal determination or authorization for builds, marketing, monetization, or excluded reference material.
- On 2026-10-11, the user confirmed permission to publish the remaining prepared runtime board, hero, hero-selection, marker and token artwork to the private `maven/guards-digital` Gitea repository. Raw card/map reference corpora, source ZIPs, screenshots, builds, logs and other excluded material remain outside that authorization.
- The game remains private and noncommercial, for the user and friends.
- "Guards Digital" is a working title. Final naming is gated on authorization and publisher agreement.
- Do not create a public build download, marketing campaign, monetization, telemetry, or store listing before separate authorization. The public source repository and tracked rulebook are the current bounded exception.
- Windows playtest packages support iteration; no pitch deliverable is scheduled.
- Disposable online playtest builds may be shared only with specifically invited private testers and are not public releases.
- Builds for all platforms remain internal unless specifically authorized.

This is a project boundary, not a legal determination.

## Product tracks

- Smart Start preparation: a separate local documentation branch starts from the self-funded functional prototype and seeks support for new heroes/mechanics, 2D/3D art and animations, with 3D/free-camera direction. [Preparation owner](SMART_START.md) records unresolved Fund fit and exact scope. No mandatory six-hero pre-application demo. This does not authorize commercial reuse of legacy content, implementation or submission; existing private-game boundaries remain intact.

- Existing game: feedback, fixes, UI improvements and requested packages, prioritized through the shared backlog. Large maps remain a planned expansion, including the existing 4v4/5v5 two-lane proposal; select a bounded implementation slice once its map/setup/rule prerequisites are settled. PvE does not replace this direction.
- PvE: design the first playable encounter, approve its rules and scope, implement and playtest, then decide what to expand.
- Platform/pitch/production-service programs remain unscheduled and are not prerequisites for private play.

## Approved prototype deviations

For existing-mode behavior, the rules ledger's complete deviation register is authoritative; the historical foundation examples below are not an exhaustive current list. Original PvE rules follow approved decisions in PVE_DESIGN.md and never silently alter existing play.

1. **Retired at milestone 8.25**: staged development configurations used one hero per team and a mirrored Wasp before the final pitch integration. The setup a player loads is the official six-player one, and the one-hero-per-team boards survive only as fixtures asked for by name.
2. One controller sees and operates all six heroes in director mode.
3. Internal debug tools may expose hidden state, undo commands, restart matches, import/export snapshots, change animation speed, and override the initial Tie Breaker side.

Director mode is an internal/demo path. The private online playtest uses normal one-player/one-hero ownership and does not expose other players' private hands or competitive debug controls.

These deviations are detailed and tracked in `RULES_LEDGER.md`.

## Rules fidelity

- Card images are presentation assets only.
- All rules-relevant card values and effects must be transcribed into structured data and verified against the source images.
- Never parse card pixels or printed text at runtime.
- Do not add convenience rules, balance changes, or digital-only behavior without explicit approval.
- When a source is ambiguous, follow the authority hierarchy in `RULES_LEDGER.md` and record any temporary interpretation.
- Existing tabletop-derived gameplay adds no new randomness after setup. PvE randomness is undecided; any approved addition must be explicit and replayable.
- Normal play randomizes the initial Tie Breaker side. Internal tests may explicitly choose Red or Blue.

## Technology and platforms

- Engine: Unity.
- Installed and selected editor: Unity `6000.5.9f1`.
- Rendering pipeline: URP 17.5 is the accepted baseline, established at 8.28.
- Reference development platform: Windows desktop and Unity Editor.
- Active reference build target: Windows 64-bit standalone (`Win64`).
- Input: Unity Input System package `1.19.0`, with **Input System Package (New)** as the only active input handler. The legacy Input Manager is not selected for gameplay input.
- Automated testing: Unity Test Framework `1.7.0`, with independent Edit Mode and Play Mode assemblies. Validation tiers, focused commands, and full-suite gates are documented in `Docs/AI/TESTING.md`.
- Version control: Local Git repository on branch `main`, with root `.gitignore` and `.gitattributes` policies; no remote is configured. The previously authorized `maven/guards-digital` Gitea `origin` was removed at the user's direction on 2026-08-15. The rulebook is tracked through Git LFS under the user's stated distribution permission, with its object present in local LFS storage. Unity-generated data, local validation output, the extracted card corpus, and map references remain ignored. Completed roadmap milestones require a local commit after acceptance and continuity updates; configuring a remote or pushing requires explicit user authorization.
- Declared target platforms: Windows, macOS, and iOS. The user set this scope on 2026-08-13, replacing the previous Windows-only target. Windows remains the active reference build target; macOS and iOS are declared targets, not yet supported ones.
- Platform rollout direction: Windows first, then macOS, then iOS. Linux and closed platforms remain unscheduled.
- iOS support is phase-scale work that is not yet scheduled as milestones. It requires at minimum: touch input replacing the current pointer-driven interaction, an IL2CPP and AOT audit of any reflection-dependent runtime path, ARM64 build configuration, a mobile allocation budget measured at the first mobile player build (the desktop growth work closed with `RISK-PERF-001`'s 6.1o retirement, which recorded the mobile cost as exactly this measurement gap), shader stripping verified by extending the standing player-build smoke gate to the mobile target (the desktop hazard closed with `RISK-BUILD-001`'s 6.1d retirement), and provisioning. These are new platform-bring-up gates, not surviving states of retired prototype risks. Do not treat a passing Windows build as evidence of iOS viability.
- The corrective sequence prepares the ground for the added platforms without implementing them: 6.1d declares shader dependencies, makes the validation toolchain cross-platform, and adds a player-build smoke gate, while 6.1i removes the per-command allocation growth. Mobile player work is scheduled only after those complete.
- Use Unity's Input System and keep interactions adaptable across mouse and touch.
- Private networking follows [PRIVATE_ONLINE_ARCHITECTURE.md](PRIVATE_ONLINE_ARCHITECTURE.md). Provider/SDK changes need a scoped compatibility, cost, privacy and operating assessment.
