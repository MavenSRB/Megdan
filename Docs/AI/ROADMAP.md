# Guards Digital - Roadmap

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

The project has two tracks: iterate, playtest and repair the existing private game, with large maps retained as a planned expansion; and design PvE, followed by separately scoped implementation. Phase 9 is closed as a development container after 9.100. Unfinished proposals and paused gates remain visible in the [shared backlog](Roadmap/BACKLOG.md); closure does not mark them passed or authorize them.

A user request authorizes its stated outcome, including normal implementation and verification needed to finish it. Record that scope in [current work](Roadmap/CURRENT_WORK.md), using a stable W-NNN identifier. Do not ask again for an already approved step. Proposed/parked items are not authorization. One implementation agent and one active outcome are the default.

For ordinary fixes or a related playtest batch, record the request and scope, observable result, affected area, closing checks and status. Do not create a milestone for each control, test, file or commit. Independent fixes may have separate commits without another project-direction update. Split when authority, risk or acceptance differs materially. Stop for an unresolved rule/product decision or work outside the request, not routine implementation choices.

Substantial feature programs use milestones at meaningful playable/integration outcomes. The [PvE plan](Roadmap/PVE.md) starts with design decisions and then a smallest playable encounter; downstream stages stay provisional until playtesting supplies evidence. Design approval and implementation approval are distinct.

For each approved outcome:

1. Load affected subject owners and establish acceptance.
2. Implement within scope, run focused checks and affected regressions under [testing policy](Testing/POLICY.md).
3. Review player experience, rules authority, maintainability and project risk; fix in-scope acceptance defects.
4. Update changed subject facts and the current route. Preserve exact validation evidence once in the commit body.
5. Move the finished work record to [work history](Archive/WORK_LOG.md), retaining request, acceptance and commit/tag locator. Keep current planning small.
6. Run living-document validation and diff checks, create a local commit, report outcome/limitations and continue remaining approved work.

Mark active work [~] immediately and keep it in exact agreement with the brief. Completed phase definitions retain their IDs, links, acceptance and evidence; they are historical records, not current product policy or recurring gates. Never renumber old records.

At integration gates, reassess affected pending work and risks against the shortest dependency-safe path to the approved playable outcome. Routine decomposition within scope is an agent decision; changing product scope, rules or explicit gates requires the corresponding user decision. A build, phase closure or work-record completion does not itself require full suites.

Explicitly scheduled independent engineering reviews inspect the implementation accumulated by their preceding gate. Each runs in a fresh task, is analysis-only, and may update only the approved review record and proposed roadmap/continuity documentation; it does not implement a fix. The reviewer must inspect actual source, tests, warnings, runtime paths, and applicable profiling or diagnostic evidence rather than relying on completion notes or assuming earlier decisions were correct. It assesses correctness, dependency direction, determinism and persistence/replay boundaries, performance and resource lifetime, Unity/presentation ownership, test quality, concrete maintainability/style consequences, temporary structures, and roadmap fitness as applicable to that gate.

A dimension becomes mechanical only after its check has been observed failing on a deliberate violation; until then the reviewer still assesses it and records either a finding or an explicit no-observation result. The following dimensions have met that proof condition and need not be re-audited semantically unless their check is retired without replacement or evidence identifies a gap:

- **Dead and temporary structures:** `unreachable-component` plus the temporary-structure register in `Docs/AI/TECHNICAL_RISKS.md`.
- **Documented-contract truth:** `undeclared-contract-type` and `incoherent-status`.
- **Buildability:** the build-and-launch player smoke gate in `Testing/FUTURE_GATES.md` when its trigger surfaces change.
- **Toolchain and platform portability:** the cross-platform validator and harness checks routed from `Docs/AI/TESTING.md`.

The implementing agent's review-only milestone handoff pass is a routine self-check and does not satisfy or replace a fresh-task independent engineering review.

Every independent engineering review classifies each observation as a confirmed defect, demonstrated performance/resource problem, architectural growth risk, maintainability/test-quality problem, or stylistic preference only. Each actionable finding records evidence, impact, severity/timing, the smallest appropriate correction, and focused validation; it also records inspected areas that should be preserved. Gate-blocking or scheduled fixes become corrective work within an approved scope. The review may recommend keeping, merging, splitting, deferring, or removing future work, but neither its findings nor its proposed roadmap changes authorize implementation. It is acceptable and valid for a review to conclude that no corrective work is justified.

Review-correction closure is mechanical. Every actionable finding names one test or validator check that closes it. A finding closes only when that named check has been observed to **fail at the pre-fix revision and pass at the post-fix revision**, with both observations recorded. A finding marked closed in documentation without that recorded transition is not closed, and a check that has never been observed failing is not evidence: it may be passing vacuously.

A fresh-task independent verification review is required only for a finding whose closure cannot be expressed as a check - rule-authority interpretation, architectural direction, or player experience. When a corrective sequence contains at least one such finding, its scheduling amendment must schedule that verification after the last correction, and the gate stays blocked until it completes. When a corrective sequence contains no such finding, it closes on its recorded check transitions and no verification milestone is scheduled. Corrective work and verification must be covered by the approved scope; do not ask again for a correction already included. A review-only request does not authorize implementation.

Each review record maintains a compact finding-disposition table linking every actionable finding to its severity, approved decision, owning corrective or future milestone, the exact named check that closes it, and current status. A later gate cannot treat the review as closed while a gate-blocking row lacks an approved disposition or its recorded failing-then-passing evidence.

Acceptance checks define what must be proved; `Docs/AI/TESTING.md` defines the required breadth. Unless a milestone is an explicit gate or qualifies as broad risk, completion does not require both unfiltered Unity suites.

Every card-related milestone must preserve the contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. A narrow milestone implements only the required subset of those contracts; it must not introduce a shortcut that the next card milestone is expected to replace.

## Active milestone definition index

Current planning: [backlog](Roadmap/BACKLOG.md), [approved work](Roadmap/CURRENT_WORK.md), [PvE delivery plan](Roadmap/PVE.md). Current authority follows [subject owners](PROJECT_CONTEXT.md).

Legacy definitions below preserve historical acceptance/non-goal checks and evidence links. Unfinished work is routed from the backlog. Historical policy text does not override this workflow or current testing policy. Keep paths stable instead of moving files merely to satisfy a size warning. Completed-only legacy definitions are historical retention, not active planning leaves.

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
- [Phase 8 - Twenty-third-hero dealing: Trinkets the Scavenger](Roadmap/PHASE_8_TWENTY_THIRD_HERO.md)
- [Phase 8 - Twenty-fourth-hero dealing: Snorri the Runescribe](Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md)
- [Phase 8 - Twenty-fifth-hero dealing: Mrak the Rockshaper](Roadmap/PHASE_8_TWENTY_FIFTH_HERO.md)
- [Phase 8 - Twenty-sixth-hero dealing: Wuk the Grove Keeper](Roadmap/PHASE_8_TWENTY_SIXTH_HERO.md)
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
- [Phase 9 - Rules fidelity and hero intake hardening](Roadmap/PHASE_9.md)
- [Phase 9 - Experimental features and alternate maps](Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md)
- [Phase 9 - First shard of completed milestone definitions](Roadmap/PHASE_9_COMPLETED.md)
- [Phase 9 - Completed presentation milestone definitions](Roadmap/PHASE_9_PRESENTATION_COMPLETED.md)
- [Phase 9 - Completed expected-outcome milestone definitions](Roadmap/PHASE_9_EXPECTED_OUTCOMES_COMPLETED.md)
- [Phase 9 - Twenty-seventh-hero dealing: Mortimer the Awakener](Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md)
- [Phase 9 - Twenty-eighth-hero dealing: Razzle](Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md)
- [Phase 9 - Twenty-ninth-hero dealing: Gydion the Archwizard](Roadmap/PHASE_9_TWENTY_NINTH_HERO.md)
- [Phase 9 - Thirtieth-hero dealing: Takahide the Warlord](Roadmap/PHASE_9_THIRTIETH_HERO.md)
- [Phase 9 - Final two heroes: Emmitt the Traveller and NebKher the Harbinger](Roadmap/PHASE_9_FINAL_TWO_HEROES.md)
- [Phase 10 - Private publisher pitch package](Roadmap/PHASE_10.md)
- [Post-pitch production direction - not scheduled](Roadmap/POST_PITCH.md)
