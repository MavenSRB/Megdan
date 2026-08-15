# Product Scope and Durable Constraints

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Mission

Create a polished private digital prototype of *Guards of Atlantis II* that can be demonstrated to its publisher as the basis for a possible licensed, cross-platform adaptation.

The private pitch prototype is a complete 3v3 match with six different heroes: Wasp plus five later user-approved, source-verified heroes. It retains single-controller director mode for development and demonstration, and now also targets an invite-only pre-production internet playtest in which six humans each control their independently owned hero before production presentation begins.

## Rights and distribution boundary

- On 2026-08-13, the user stated that they have relevant permission to publish this source repository and the included rulebook. This records the user's authorization for those artifacts; it is not a legal determination or authorization for builds, marketing, monetization, or excluded reference material.
- The playable prototype remains private, noncommercial, and intended for a direct publisher pitch.
- "Guards Digital" is a working title. Final naming is gated on authorization and publisher agreement.
- Do not create a public build download, marketing campaign, monetization, telemetry, or store listing before separate authorization. The public source repository and tracked rulebook are the current bounded exception.
- The initial pitch deliverable is a private Windows standalone build plus a short recorded walkthrough as backup.
- Disposable online playtest builds may be shared only with specifically invited private testers and are not public releases.
- Builds for all platforms remain internal unless specifically authorized.

This is a project boundary, not a legal determination.

## Product horizons

### Horizon 1 - Private pitch prototype

A complete 3v3 match with both director and private online validation modes:

- Three Red heroes and three Blue heroes, all different: Wasp plus five later user-approved, source-verified heroes.
- Wasp is implemented and integrated first; a mirrored Wasp 1v1 remains an intermediate development gate, not the pitch endpoint.
- One human controls all six heroes in director mode.
- All six hero hands are available to the controller through team/hero grouping; each nonempty hero hand requires exactly one facedown commitment, each empty hand requires Pass, reveal waits for all six hero responses, and a read-only four-turn history identifies every hero's authoritative revealed card or empty-hand Pass while current Facedown commitments and private hands remain protected by the applicable projection.
- The controller selects exactly one owned card for every hero with a nonempty hand and records Pass for every empty hand, regardless of whether that hero is on the board; after all six heroes have responses, it reveals and resolves the played cards through normal Initiative order.
- Before production presentation begins, six invited humans can connect over the internet from Windows clients, each control exactly one owned hero, see only their authorized private projection, and complete a full match through one authoritative host/session.
- Full official single-lane board structure and standard minion setup.
- Six-player Quick Game setup values: three Wave counters and five Life counters per team.
- Full verified progression for all six heroes, including relevant card tiers, items, and Ultimate access.
- Complete applicable game loop: cards, initiative, actions, combat, defense, minions, rounds, lane pushing, economy, leveling, respawn, and victory conditions.
- No opponent AI, pass-and-play, accounts, public matchmaking, progression meta, or public live-service operations.

### Horizon 2 - Licensed product direction

Post-authorization only:

- Production-ready online 3v3 as the primary mode, building on the private pre-production validation track.
- Six humans, each controlling one hero.
- Cross-platform release, developed and validated in phases.
- Larger hero roster, eventually expected to approach roughly 40 heroes.
- Production-scale hosting, matchmaking, accounts, platform services, moderation, anti-cheat, and operational support remain separate future work.
- No AI-player requirement is currently planned.

## Approved prototype deviations

Everything not listed here must behave exactly like the authoritative physical game sources.

1. Before the final pitch integration, staged development configurations may use one hero per team and mirrored Wasp; both temporary deviations must be retired by the official six-player setup gate.
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
- The game has no randomness after setup. The only random setup result is the starting side of the Tie Breaker coin.
- Normal play randomizes the initial Tie Breaker side. Internal tests may explicitly choose Red or Blue.

## Technology and platforms

- Engine: Unity.
- Installed and selected editor: Unity `6000.5.7f1`.
- Rendering pipeline: Built-In Render Pipeline is retained for the current primitive-rule phases. Before production presentation begins, milestone 8.28 migrates the validated primitive match to URP and makes URP the accepted rendering baseline; do not introduce Built-In-only production visual dependencies beforehand.
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
- Do not choose a networking provider or SDK before milestone 8.29 compares current support, relay/hosting, cost, licensing, privacy, and operational constraints.
