# Guards Digital - Roadmap

Last updated: 2026-08-14

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

This roadmap is a sequence of micro-milestones, not authorization to execute them.

For every item:

1. Obtain explicit user approval for that item.
2. Implement only its stated observable outcome.
3. Respect its non-goals.
4. Run the milestone's focused tests, directly affected regressions, and highest applicable validation tier from `Docs/AI/TESTING.md`.
5. Update the continuity documents whose subject the milestone actually changed, including the next proposed milestone.
6. Create a local Git commit containing the completed milestone, its status change, and continuity updates. The commit message body carries the milestone's evidence record.
7. Report the result and the next proposed milestone, then stop.

The former step-2 pre-work start transition was retired on 2026-08-13; see `AGENTS.md`. Mark a milestone `[~]` and set the session brief's active field only when work spans sessions or is handed off mid-flight, in which case the validator enforces their exact agreement as before.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

A split has a floor: do not split when the split's fixed handoff cost - approval, status transition, validator run, review pass, evidence record, commit - would exceed the implementation it isolates. Milestones `5.1b`, `5.1c`, and `5.8a1` are the recorded cases where the paperwork was plausibly larger than the change. Splitting still applies whenever the parts carry genuinely independent risk or separate acceptance evidence; the floor bars ceremony, not decomposition.

At each designated phase-gate roadmap audit, reassess the affected unstarted milestones against the evidence produced so far. The audit is a required handoff activity, not a separately numbered milestone, and does not authorize implementation. For each affected milestone, recommend whether to keep it unchanged, merge it with adjacent work that shares the same authority and validation boundary, split independently risky behavior, or defer/remove work that is not required for the private publisher pitch. Record an approximate remaining-effort range and identify the shortest dependency-safe path to the next hard integration outcome. Any resulting roadmap change requires explicit user approval and the applicable continuity updates before the next milestone profile is assigned. An audit must not weaken verified rules, architecture contracts, acceptance evidence, validation gates, or the one-approved-micro-milestone-at-a-time rule.

Separately numbered independent engineering review milestones inspect the implementation accumulated by their preceding gate. Each runs in a fresh task, is analysis-only, and may update only the approved review record and proposed roadmap/continuity documentation; it does not implement a fix. The reviewer must inspect actual source, tests, warnings, runtime paths, and applicable profiling or diagnostic evidence rather than relying on completion notes or assuming earlier decisions were correct. It assesses correctness, dependency direction, determinism and persistence/replay boundaries, performance and resource lifetime, Unity/presentation ownership, test quality, concrete maintainability/style consequences, temporary structures, and roadmap fitness as applicable to that gate.

Some review dimensions are enforced mechanically and are therefore not the reviewer's job. A dimension leaves this prose list only once its check has been observed to fail on a deliberate violation; until then it stays here and the reviewer must state a finding for it, including an explicit "no observation" where that is the conclusion.

Enforced by the living-document validator, and no longer reviewer-owned:

- **Dead and temporary structures.** The `unreachable-component` check reports any `MonoBehaviour`/`ScriptableObject` that is in no scene, prefab, or asset and is never constructed by production code, unless the temporary-structure register of `Docs/AI/TECHNICAL_RISKS.md` names its retirement owner. Retired from this list on 2026-08-13 after being observed to fail on the legacy controller drill and pass once its register row was restored.
- **Documented-contract truth.** The `undeclared-contract-type` and `incoherent-status` checks prove every documented contract resolves to a real type and that the three canonical status statements agree. Retired from this list on 2026-08-13; these checks found `F613-15`.
- **Buildability.** The player-build smoke gate in `Testing/FUTURE_GATES.md` builds the real Windows player, runs it, and requires it to report that it composed a configured match and completed one authoritative command. Retired from the reviewer-owned list at milestone 6.1d, after being observed to fail on a deliberate removal of the Standard shader from the always-included list and to pass once restored. The deliberate break is worth recording precisely: the **build still succeeded**, and only the running player failed, so a build-only check would have reported that violation as a pass.

No dimension is currently reviewer-owned. **Toolchain and platform portability** left this list at milestone `6.1l`, which confirmed every validator script on a non-Windows host (the Linux workstation, PowerShell 7.6.4) after correcting the three portability defects the failing runs exposed; a dimension returns to this list only if its check is retired without replacement.

The implementing agent's review-only milestone handoff pass is a routine self-check and does not satisfy or replace a separately numbered fresh-task independent engineering review.

Every independent engineering review classifies each observation as a confirmed defect, demonstrated performance/resource problem, architectural growth risk, maintainability/test-quality problem, or stylistic preference only. Each actionable finding records evidence, impact, severity/timing, the smallest appropriate correction, and focused validation; it also records inspected areas that should be preserved. Gate-blocking or scheduled fixes become separately approved corrective micro-milestones. The review may recommend keeping, merging, splitting, deferring, or removing future work, but neither its findings nor its proposed roadmap changes authorize implementation. It is acceptable and valid for a review to conclude that no corrective work is justified.

Review-correction closure is mechanical. Every actionable finding names one test or validator check that closes it. A finding closes only when that named check has been observed to **fail at the pre-fix revision and pass at the post-fix revision**, with both observations recorded. A finding marked closed in documentation without that recorded transition is not closed, and a check that has never been observed failing is not evidence: it may be passing vacuously.

A separately numbered fresh-task independent verification review is required only for a finding whose closure cannot be expressed as a check - rule-authority interpretation, architectural direction, or player experience. When a corrective sequence contains at least one such finding, its scheduling amendment must schedule that verification after the last correction, and the gate stays blocked until it completes. When a corrective sequence contains no such finding, it closes on its recorded check transitions and no verification milestone is scheduled. Every corrective and verification milestone still requires exact user approval, and neither a review nor a scheduling amendment authorizes execution.

This replaces the former automatic recursive verification requirement, retired on 2026-08-13. That rule scheduled a fresh-task review after every corrective sequence regardless of what the findings were, and produced no evidence a check could not have produced more cheaply: reviews `4.4i` and `5.8c` each ran the full ceremony and each concluded that no corrective work was justified while fourteen defects were present. Reviewer judgment is the scarce resource; spend it only where a check cannot reach.

Each review record maintains a compact finding-disposition table linking every actionable finding to its severity, approved decision, owning corrective or future milestone, the exact named check that closes it, and current status. A later gate cannot treat the review as closed while a gate-blocking row lacks an approved disposition or its recorded failing-then-passing evidence.

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
- [Phase 6 - Documentation-truth completion block](Roadmap/PHASE_6_DOCUMENTATION.md)
- [Phase 6 - Completed milestone definitions](Roadmap/PHASE_6_COMPLETED.md)
- [Phase 7 - Progression, victory, and the complete synthetic core loop](Roadmap/PHASE_7.md)
- [Phase 8 - Verified hero content, deterministic 3v3, and private online validation](Roadmap/PHASE_8.md)
- [Phase 9 - Presentation and pitch polish](Roadmap/PHASE_9.md)
- [Phase 10 - Private publisher pitch package](Roadmap/PHASE_10.md)
- [Post-pitch production direction - not scheduled](Roadmap/POST_PITCH.md)
