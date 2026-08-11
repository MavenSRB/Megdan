# Phase 0 - Shared context and project shell - Historical Handoffs

Lossless completed-milestone evidence archived from [the active phase shard](../../Roadmap/PHASE_0.md).

## Milestone 0.2 Create the empty Unity project completion evidence

Completion note (2026-08-09): Created and batch-opened the empty project with Unity `6000.5.7f1`; selected Win64 as the reference build target; installed Unity Input System `1.19.0` and selected its new-only input backend. Git was authorized later that day; the local `main` repository now includes root `.gitignore` and `.gitattributes` policies, satisfying the conditional version-control check. No gameplay, board, camera, state, or card content was added.

## Milestone 0.3 Add the automated test shell completion evidence

Completion note (2026-08-09): Added Unity Test Framework `1.7.0`, isolated Edit Mode and Play Mode test assemblies, one trivial Edit Mode test, and one Play Mode one-frame smoke test. Each fully qualified test was run independently in batch mode and passed 1/1. Commands and output paths are documented in `Docs/AI/TESTING.md`. Neither test requires a player build, a saved scene, or game-rule code.

## Milestone 0.4 Establish the card-system architecture contract completion evidence

Completion note (2026-08-09): Reviewed the second-printing rulebook's complete general card/action rules on printed pages 7-19 and documented the resulting architecture and implementation gates. The design is intended to localize future extensions, not to claim that unknown cards can be implemented without any new typed primitive or isolated handler.

## Milestone 0.5 Establish the reusable card-effect capability catalog completion evidence

Completion note (2026-08-09): Re-audited printed rulebook pages 7-19 and expanded the card foundation into an explicit reusable effect/keyword capability catalog. The catalog covers the general rules vocabulary and deliberately leaves hero-specific semantics to verified card inventory.

## Milestone 0.6 Re-sequence development around the core match loop completion evidence

Completion note (2026-08-09): Reordered the roadmap so initiative, Tie Breaker behavior, turns, rounds, and the full synthetic core loop precede the complete Wasp content pass while preserving the smallest-step approval gates.

## Milestone 0.7 Adopt proportional risk-tiered validation completion evidence

Completion note (2026-08-09): Replaced the ambiguous requirement to run both complete Unity suites at every micro-milestone with four validation tiers, explicit integration gates, and objective escalation triggers. Focused coverage remains mandatory for executable work; documentation-only checks, targeted visual review, source validation, and build smoke testing now have distinct owners.
