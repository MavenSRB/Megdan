# Guards Digital - Roadmap

Last updated: 2026-08-12

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

This roadmap is a sequence of micro-milestones, not authorization to execute them.

GPT-5.6 Sol is the only permitted model for milestone work. Reasoning effort must be medium or higher: use medium by default, and raise it to high, extra high (`xhigh` in configuration), or ultra when the milestone's complexity, breadth, integration risk, or hard-gate role requires it.

For every item:

1. Confirm that the exact item declares its `Required Codex profile` and that `.codex/config.toml` matches it; this is execution metadata, not authorization.
2. Obtain explicit user approval for that item.
3. Before substantive milestone analysis, implementation, review, or test execution, mark the exact roadmap item `[~]`, record that same ID as the active authorized milestone in `Docs/AI/SESSION_BRIEF.md`, retain the canonical next-milestone/profile route for the active ID, and run the living-document validator. Do not proceed until the start transition validates; this rule includes analysis-only and independent-review milestones.
4. Implement only its stated observable outcome.
5. Respect its non-goals.
6. Run the milestone's focused tests, directly affected regressions, and highest applicable validation tier from `Docs/AI/TESTING.md`.
7. Update the project documents and `.codex/config.toml`, including the next proposed milestone's required profile and matching new-task defaults.
8. Create a local Git commit containing the completed milestone and continuity updates.
9. Report the result, the next proposed milestone and its required profile, then stop.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

At each designated phase-gate roadmap audit, reassess the affected unstarted milestones against the evidence produced so far. The audit is a required handoff activity, not a separately numbered milestone, and does not authorize implementation. For each affected milestone, recommend whether to keep it unchanged, merge it with adjacent work that shares the same authority and validation boundary, split independently risky behavior, or defer/remove work that is not required for the private publisher pitch. Record an approximate remaining-effort range and identify the shortest dependency-safe path to the next hard integration outcome. Any resulting roadmap change requires explicit user approval and the applicable continuity updates before the next milestone profile is assigned. An audit must not weaken verified rules, architecture contracts, acceptance evidence, validation gates, or the one-approved-micro-milestone-at-a-time rule.

Separately numbered independent engineering review milestones inspect the implementation accumulated by their preceding gate. Each runs in a fresh Codex task, is analysis-only, and may update only the approved review record and proposed roadmap/continuity documentation; it does not implement a fix. The reviewer must inspect actual source, tests, warnings, runtime paths, and applicable profiling or diagnostic evidence rather than relying on completion notes or assuming earlier decisions were correct. It assesses correctness, dependency direction, determinism and persistence/replay boundaries, performance and resource lifetime, Unity/presentation ownership, test quality, concrete maintainability/style consequences, temporary structures, and roadmap fitness as applicable to that gate.

The implementing agent's review-only milestone handoff pass is a routine self-check and does not satisfy or replace a separately numbered fresh-task independent engineering review.

Every independent engineering review classifies each observation as a confirmed defect, demonstrated performance/resource problem, architectural growth risk, maintainability/test-quality problem, or stylistic preference only. Each actionable finding records evidence, impact, severity/timing, the smallest appropriate correction, and focused validation; it also records inspected areas that should be preserved. Gate-blocking or scheduled fixes become separately approved corrective micro-milestones. The review may recommend keeping, merging, splitting, deferring, or removing future work, but neither its findings nor its proposed roadmap changes authorize implementation. It is acceptable and valid for a review to conclude that no corrective work is justified.

Review-correction closure is recursive. When an independent review concludes that one or more corrective micro-milestones must complete before the affected downstream gate, the proposed corrective roadmap sequence must also include a separately numbered fresh-task independent verification review after the last required correction and before downstream implementation. An approved corrective-roadmap scheduling amendment must include that terminal verification, but neither the scheduling amendment nor the review authorizes execution: every corrective and verification milestone still requires exact user approval. The verification inspects the corrected implementation, actual runtime paths, tests, evidence, and original finding closures; implementing agents' handoff self-reviews do not satisfy it. If that verification requires further pre-gate corrective milestones, the same correction-plus-verification rule repeats. The gate remains blocked until the latest required independent verification completes, every gate-blocking finding has an approved disposition and required evidence, and no further pre-gate corrective milestone is required. A deferred non-blocking correction may instead name an already-scheduled later independent review as its verification owner only when the finding-disposition table records that owner and the user explicitly approves the timing.

Each review record maintains a compact finding-disposition table linking every actionable finding to its severity, approved decision, owning corrective or future milestone, validation requirement, and current status. A later gate cannot treat the review as closed while a gate-blocking row lacks an approved disposition or required completion evidence.

Acceptance checks define what must be proved; `Docs/AI/TESTING.md` defines the required breadth. Unless a milestone is an explicit gate or qualifies as broad risk, completion does not require both unfiltered Unity suites.

Every card-related milestone must preserve the contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. A narrow milestone implements only the required subset of those contracts; it must not introduce a shortcut that the next card milestone is expected to replace.

## Active milestone definition index

Milestone definitions and their exact acceptance/non-goal checks are authoritative in the indexed phase shards below. Completed milestone evidence is preserved verbatim in the linked phase handoff archives and is not routine startup context.

- [Phase 0 - Shared context and project shell](Roadmap/PHASE_0.md)
- [Phase 1 - Smallest board interaction](Roadmap/PHASE_1.md)
- [Phase 2 - Two-hero combat and Tie Breaker state](Roadmap/PHASE_2.md)
- [Phase 3 - Test-card Initiative, turns, and rounds](Roadmap/PHASE_3.md)
- [Phase 4 - Full official multi-zone board](Roadmap/PHASE_4.md)
- [Phase 5 - Core full-map actions and combat](Roadmap/PHASE_5.md)
- [Phase 6 - Tokens, minions, Lane Push, and end-of-round components](Roadmap/PHASE_6.md)
- [Phase 7 - Progression, victory, and the complete synthetic core loop](Roadmap/PHASE_7.md)
- [Phase 8 - Verified hero content, deterministic 3v3, and private online validation](Roadmap/PHASE_8.md)
- [Phase 9 - Presentation and pitch polish](Roadmap/PHASE_9.md)
- [Phase 10 - Private publisher pitch package](Roadmap/PHASE_10.md)
- [Post-pitch production direction - not scheduled](Roadmap/POST_PITCH.md)
