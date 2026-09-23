# Guards Digital - Roadmap

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

This roadmap is a sequence of micro-milestones, not authorization to execute them. A user may explicitly approve a bounded sequence of outcomes in one instruction; map it to exact IDs in the owning shard before execution. Approval persists across separate commits. Execute one milestone at a time and stop at the end of the authorized sequence. Queued and parked work remains unauthorized.

For every item:

1. Confirm approval, individually or within the recorded bounded sequence.
2. Implement only its stated observable outcome.
3. Respect its non-goals.
4. Run the milestone's focused tests, directly affected regressions, and highest applicable validation tier from `Docs/AI/TESTING.md`.
5. Update the continuity documents whose subject the milestone actually changed, including the next proposed milestone.
6. Create a local Git commit containing the completed milestone, its status change, and continuity updates. The commit message body carries the milestone's evidence record.
7. Report the result and next route; stop unless that next item is already in the authorized sequence.

Mark a milestone `[~]` and set the session brief's active field only when work spans tasks or is handed off mid-flight; the validator requires their exact agreement and permits at most one in-progress milestone.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

A split has a floor: do not split when its fixed handoff cost would exceed the implementation it isolates. Split whenever parts carry genuinely independent risk or separate acceptance evidence; the floor bars ceremony, not decomposition. Recorded cases are `5.1b`, `5.1c`, and `5.8a1`.

At each designated phase-gate roadmap audit, reassess the affected unstarted milestones against the evidence produced so far. The audit is a required handoff activity, not a separately numbered milestone, and does not authorize implementation. For each affected milestone, recommend whether to keep it unchanged, merge it with adjacent work that shares the same authority and validation boundary, split independently risky behavior, or defer/remove work that is not required for the private publisher pitch. Record an approximate remaining-effort range and identify the shortest dependency-safe path to the next hard integration outcome. Any resulting roadmap change requires explicit user approval and the applicable continuity updates before the next milestone proceeds. An audit must not weaken verified rules, architecture contracts, acceptance evidence, validation gates, or the one-approved-micro-milestone-at-a-time rule.

Separately numbered independent engineering review milestones inspect the implementation accumulated by their preceding gate. Each runs in a fresh task, is analysis-only, and may update only the approved review record and proposed roadmap/continuity documentation; it does not implement a fix. The reviewer must inspect actual source, tests, warnings, runtime paths, and applicable profiling or diagnostic evidence rather than relying on completion notes or assuming earlier decisions were correct. It assesses correctness, dependency direction, determinism and persistence/replay boundaries, performance and resource lifetime, Unity/presentation ownership, test quality, concrete maintainability/style consequences, temporary structures, and roadmap fitness as applicable to that gate.

A dimension becomes mechanical only after its check has been observed failing on a deliberate violation; until then the reviewer still assesses it and records either a finding or an explicit no-observation result. The following dimensions have met that proof condition and need not be re-audited semantically unless their check is retired without replacement or evidence identifies a gap:

- **Dead and temporary structures:** `unreachable-component` plus the temporary-structure register in `Docs/AI/TECHNICAL_RISKS.md`.
- **Documented-contract truth:** `undeclared-contract-type` and `incoherent-status`.
- **Buildability:** the build-and-launch player smoke gate in `Testing/FUTURE_GATES.md` when its trigger surfaces change.
- **Toolchain and platform portability:** the cross-platform validator and harness checks routed from `Docs/AI/TESTING.md`.

The implementing agent's review-only milestone handoff pass is a routine self-check and does not satisfy or replace a separately numbered fresh-task independent engineering review.

Every independent engineering review classifies each observation as a confirmed defect, demonstrated performance/resource problem, architectural growth risk, maintainability/test-quality problem, or stylistic preference only. Each actionable finding records evidence, impact, severity/timing, the smallest appropriate correction, and focused validation; it also records inspected areas that should be preserved. Gate-blocking or scheduled fixes become separately approved corrective micro-milestones. The review may recommend keeping, merging, splitting, deferring, or removing future work, but neither its findings nor its proposed roadmap changes authorize implementation. It is acceptable and valid for a review to conclude that no corrective work is justified.

Review-correction closure is mechanical. Every actionable finding names one test or validator check that closes it. A finding closes only when that named check has been observed to **fail at the pre-fix revision and pass at the post-fix revision**, with both observations recorded. A finding marked closed in documentation without that recorded transition is not closed, and a check that has never been observed failing is not evidence: it may be passing vacuously.

A separately numbered fresh-task independent verification review is required only for a finding whose closure cannot be expressed as a check - rule-authority interpretation, architectural direction, or player experience. When a corrective sequence contains at least one such finding, its scheduling amendment must schedule that verification after the last correction, and the gate stays blocked until it completes. When a corrective sequence contains no such finding, it closes on its recorded check transitions and no verification milestone is scheduled. Every corrective and verification milestone still requires exact user approval, and neither a review nor a scheduling amendment authorizes execution.

Each review record maintains a compact finding-disposition table linking every actionable finding to its severity, approved decision, owning corrective or future milestone, the exact named check that closes it, and current status. A later gate cannot treat the review as closed while a gate-blocking row lacks an approved disposition or its recorded failing-then-passing evidence.

Acceptance checks define what must be proved; `Docs/AI/TESTING.md` defines the required breadth. Unless a milestone is an explicit gate or qualifies as broad risk, completion does not require both unfiltered Unity suites.

Every card-related milestone must preserve the contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. A narrow milestone implements only the required subset of those contracts; it must not introduce a shortcut that the next card milestone is expected to replace.

## Active milestone definition index

Milestone definitions and their exact acceptance/non-goal checks are authoritative in the indexed phase shards below. Each linked phase-handoff row indexes the completed milestone's detailed commit-body evidence; neither is routine startup context.

- [Phase 0 - Shared context and project shell](Roadmap/PHASE_0.md)
- [Phase 1 - Smallest board interaction](Roadmap/PHASE_1.md)
- [Phase 2 - Two-hero combat and Tie Breaker state](Roadmap/PHASE_2.md)
- [Phase 3 - Test-card Initiative, turns, and rounds](Roadmap/PHASE_3.md)
- [Phase 4 - Full official multi-zone board](Roadmap/PHASE_4.md)
- [Phase 5 - Core full-map actions and combat](Roadmap/PHASE_5.md)
- [Phase 6 - Tokens, minions, Lane Push, and end-of-round components](Roadmap/PHASE_6.md)
- [Phase 6 - Mechanics-showcase card integrations](Roadmap/PHASE_6_SHOWCASE.md)
- [Phase 6 - Corrective milestones from proving the showcase cards](Roadmap/PHASE_6_CORRECTIONS.md)
- [Phase 6 - Documentation-truth completion block](Roadmap/PHASE_6_DOCUMENTATION.md)
- [Phase 6 - Completed milestone definitions](Roadmap/PHASE_6_COMPLETED.md)
- [Phase 7 - Progression, victory, and the complete synthetic core loop](Roadmap/PHASE_7.md)
- [Phase 8 - Verified hero content, deterministic 3v3, and private online validation](Roadmap/PHASE_8.md)
- [Phase 8 - Independent private-online review and finding dispositions](Roadmap/PHASE_8_PRIVATE_ONLINE_REVIEW.md)
- [Phase 8 - Approved private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md)
- [Phase 8 - Authorized cleanup and consolidated follow-up backlog](Roadmap/PHASE_8_CLEANUP.md)
- [Phase 8 - Thirteenth-hero dealing: Swift the Sharpshooter](Roadmap/PHASE_8_THIRTEENTH_HERO.md)
- [Phase 8 - Fourteenth-hero dealing: Cutter the Sky Pirate](Roadmap/PHASE_8_FOURTEENTH_HERO.md)
- [Phase 8 - Fifteenth-hero dealing: Bain the Bounty Hunter](Roadmap/PHASE_8_FIFTEENTH_HERO.md)
- [Phase 8 - Sixteenth-hero dealing: Garrus the Gladiator](Roadmap/PHASE_8_SIXTEENTH_HERO.md)
- [Phase 8 - Seventeenth-hero dealing: Widget and Pyro](Roadmap/PHASE_8_SEVENTEENTH_HERO.md)
- [Phase 8 - Eighteenth-hero dealing: Rowenna](Roadmap/PHASE_8_EIGHTEENTH_HERO.md)
- [Phase 8 - Nineteenth-hero dealing: Min the Dragonmonk](Roadmap/PHASE_8_NINETEENTH_HERO.md)
- [Phase 8 - Twentieth-hero dealing: Brynn the Seeker](Roadmap/PHASE_8_TWENTIETH_HERO.md)
- [Phase 8 - Twenty-first-hero dealing: Hanu the Trickster](Roadmap/PHASE_8_TWENTY_FIRST_HERO.md)
- [Phase 8 - Twenty-second-hero dealing: Ignatia the Mad](Roadmap/PHASE_8_TWENTY_SECOND_HERO.md)
- [Phase 8 - Playtest refinements](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md)
- [Phase 8 - The decision window](Roadmap/PHASE_8_DECISION_WINDOW.md)
- [Phase 8 - Six-hero roster re-baseline and shared foundations](Roadmap/PHASE_8_ROSTER_REBASE.md)
- [Phase 8 - Roster-complete stabilization and corrective route](Roadmap/PHASE_8_STABILIZATION.md)
- [Phase 8 - Hands-on corrections before private online work](Roadmap/PHASE_8_HANDS_ON_CORRECTIONS.md)
- [Phase 8 - Late HUD and card-interaction corrections](Roadmap/PHASE_8_LATE_HUD_CORRECTIONS.md)
- [Phase 8 - Completed milestone definitions](Roadmap/PHASE_8_COMPLETED.md)
- [Phase 8 - Additional completed milestone definitions](Roadmap/PHASE_8_COMPLETED_2.md)
- [Phase 8 - Further completed milestone definitions](Roadmap/PHASE_8_COMPLETED_3.md)
- [Phase 8 - Yet more completed milestone definitions](Roadmap/PHASE_8_COMPLETED_4.md)
- [Phase 8 - Still more completed milestone definitions](Roadmap/PHASE_8_COMPLETED_5.md)
- [Phase 8 - Sixth shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_6.md)
- [Phase 8 - Seventh shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_7.md)
- [Phase 8 - Eighth shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_8.md)
- [Phase 8 - Ninth shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_9.md)
- [Phase 8 - Tenth shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_10.md)
- [Phase 8 - Eleventh shard of completed milestone definitions](Roadmap/PHASE_8_COMPLETED_11.md)
- [Phase 9 - Presentation and pitch polish](Roadmap/PHASE_9.md)
- [Phase 10 - Private publisher pitch package](Roadmap/PHASE_10.md)
- [Post-pitch production direction - not scheduled](Roadmap/POST_PITCH.md)
