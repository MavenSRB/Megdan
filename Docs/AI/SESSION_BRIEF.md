# Guards Digital - Session Brief

Last updated: 2026-08-12

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current implementation, authorization, and the exact next milestone/profile route.

## Current implementation

The project has completed roadmap milestones through Phase 5 milestone 5.8a. The configured vertical slice now uses the pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound versioned snapshots, and passive Unity presentation, including authority-owned Hero and minion Attack-target selection. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps is maintained in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md); it is updated in place rather than extended as a milestone journal.

The immediate configured defect is a Unity editor/play-mode lifetime failure in the milestone 5.8a minion presentation path: a grid rebuild after backup/domain restoration can replace generated `StandardMinionView` objects while cached board/input bindings retain destroyed components. Proposed corrective milestone 5.8a1 owns the reload-safe binding correction and regression; milestone 5.8b's Defense preview is blocked behind it.

## Current authorization and next route

- Current completed milestone: **5.8a**.
- Active authorized milestone: **none**.
- Next proposed milestone: **5.8a1**.
- Required Codex profile for next milestone: **GPT-5.6 Sol with high reasoning**.
- Corrective milestone 5.8a1 repairs configured minion-view lifetime and its missing editor reload regression. Milestone 5.8b and review 5.8c remain blocked behind the correction. None is authorized.
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
