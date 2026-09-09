# Phase 10 - Private publisher pitch package

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_10 handoffs](../Archive/Phase-Handoffs/PHASE_10.md).

## Phase 10 - Private publisher pitch package

Publisher-pitch work is parked by the user's 2026-09-09 [private-game scope amendment](../Project/PRODUCT.md). These retained definitions do not block separately authorized builds for the user and friends.

### [ ] 10.1 Windows standalone build

Observable outcome: An unchanged milestone 9.16 revision produces a private Win64 player that launches to the pitch prototype without compilation, startup, or missing-content errors.

Acceptance checks:

- Build settings, version, source commit, output path, and launch-smoke result are recorded.
- The build launches, reaches the prototype, starts a deterministic scenario, and exits cleanly.
- The milestone reuses the unchanged 9.16 T3 result; any intervening code or executable-content change requires T3 again before building.

Non-goals: No installer, launcher, code signing, updater, or public distribution.

### [ ] 10.2 Clean-machine private smoke test

Observable outcome: The exact private Win64 build launches and performs the scripted smoke path on a clean Windows machine without Unity or project dependencies installed.

Acceptance checks:

- The packaged build starts from a clean copy, reaches the prototype, accepts input, completes the scripted smoke path, and exits without an unhandled error.
- Missing files, platform dependencies, logs, machine details, and build identity are recorded.
- Unity Test Framework is not rerun against an unchanged build merely to duplicate the 9.16 evidence.

### [ ] 10.3 Short recorded gameplay walkthrough

### [ ] 10.4 Publisher demonstration checklist

Definition owner (2026-08-14, user approved): milestones 10.3 and 10.4 are titles, not yet authorizable definitions. The Phase 9 to Phase 10 transition's designated roadmap audit writes their observable outcomes, acceptance checks, and non-goals from the retained 9.16 pitch build - mirroring how milestone 8.19's audit owns the Phase 9 definitions - and the resulting definitions require explicit user approval before either milestone is proposed for execution.

Shared non-goals for 10.3 and 10.4 until then: public distribution, installer, launcher, login, telemetry, store page, or other-platform release.
