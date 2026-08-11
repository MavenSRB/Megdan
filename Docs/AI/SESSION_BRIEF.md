# Guards Digital - Session Brief

Last updated: 2026-08-11

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current implementation, authorization, and the exact next milestone/profile route.

## Current implementation

The project has completed roadmap milestones through the primitive complete-map foundation, including all Phase 0-3 milestones and corrective inserts, deterministic command-replay Director Undo, the internal rule-event log, the reviewed `official-single-lane` revision 1 map specification, selected-map authoritative loading/snapshot binding, and complete placeholder map presentation through milestone 4.2a. Documentation-governance milestone 3.11a is complete.

Authoritative pure C# state currently covers map-scoped identity, the immutable selected map revision, participant `PlayerId`/`HeroId` relationships, initial Tie Breaker, five-card synthetic hands, owned commitment or empty-hand Pass for every configured hero, joint reveal, Initiative and tied-Initiative choice, Hold, printed Movement with cumulative waypoint routes and Stop, adjacent/Ranged Attack with one legal Defense discard or decline, immutable four-turn assignment history, and a resumable end-of-round cursor whose test fixture implements retrieval and emits typed `NotApplicable` outcomes for unavailable stages. Eligible cards return to canonical owners; two uninterrupted synthetic rounds and reuse are visible in the saved validation scene.

Schema-versioned snapshots cover participants, selected map ID/revision, board occupancy, card facets, Initiative/Tie Breaker, current/completed assignments, end-of-round state, and pending Movement or Attack/Defense continuations. Director Undo retains immutable initial scenario inputs plus accepted typed commands, rebuilds fresh authority, replays the retained prefix through live command paths, and verifies byte-identical state/events across two rebuilds. Snapshots remain the separate persistence/diagnostics/resynchronization boundary. The read-only HUD exposes Tie Breaker, Undo, Restart, and the rule-event log.

The project does not yet implement zone adjacency/ordered lane authority, the configured official Quick Game setup, resolve-without-action, real Wasp content, general card-text effects, Attack consequences, concrete end-of-round subsystems beyond retrieval, networking, production animation/audio, or build/distribution work.

## Current authorization and next route

- Current completed milestone: **3.11a**.
- Next proposed milestone: **4.3**.
- Required Codex profile for next milestone: **GPT-5.6 Sol with medium reasoning**.
- Milestone 4.3 is proposed only. Do not begin it or any later gameplay, presentation, card, online, or build milestone without explicit approval of that exact micro-milestone.
- The local repository is on `main`; no remote is configured or authorized.

## Durable boundaries

- The private target remains an official-setup 3v3 pitch match with Wasp plus five later approved, source-verified heroes, retaining director mode and adding invite-only six-human Windows internet validation before production presentation. Accounts, public matchmaking, public distribution, and production live-service scope remain excluded.
- Rules follow the second-printing rulebook, verified card data, and official rulings under [RULES_LEDGER.md](RULES_LEDGER.md). No rule may be invented, simplified, rebalanced, or silently reinterpreted.
- Pure rules/state remain independent of Unity presentation. Preserve definition/instance, action-offer, command/event, pending-choice, timing, versioning, typed effect/query, map-scoped identity, snapshot, and deterministic replay boundaries.
- The rulebook, extracted card corpus, project assets, builds, and project remain private. Do not add excluded references to Git or publish/upload/distribute anything without explicit authorization and applicable rights.
- Work on one approved micro-milestone, run its routed validation, perform the required four-perspective review-only pass, update canonical continuity and history, create the local milestone commit, report the next proposal/profile, and stop.

## Targeted routing

Start from the compact index, then read only the linked active leaf and dependencies named by that leaf:

| Need | Canonical route |
| --- | --- |
| Ownership, thresholds, archives, active risks | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md), then [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) when applicable |
| Milestone approval/implementation | [ROADMAP.md](ROADMAP.md) `Execution rule`, then the exact indexed phase shard |
| Product, architecture, presentation, workflow, terminology | [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md), then the indexed subject shard |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Card/action/turn/snapshot contracts | [CARD_SYSTEM_ARCHITECTURE.md](CARD_SYSTEM_ARCHITECTURE.md), then the indexed contract shard |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy or commands | [TESTING.md](TESTING.md), then the applicable policy/recipe shard |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict. At every handoff run the living-document validator and `git diff --check`; Unity validation remains controlled solely by the applicable testing tier.
