# Guards Digital - Session Brief

Last updated: 2026-08-09

## Purpose and authority

This is the compact startup document for a new Codex task or run. It summarizes current state, authorization, hard boundaries, and document routing so agents do not load the complete continuity set by default.

This brief is not a substitute for authoritative sources. `AGENTS.md` controls the working process, and the detailed living documents control their respective project contracts. If this brief appears stale or conflicts with one of those sources, stop and verify the relevant source section before acting.

## Current state and authorization

- Roadmap milestones 0.1 through 0.7, 1.1 through 1.13, and 2.1 through 2.2 are marked complete, including corrective milestones 1.12a, 1.12b, and 1.13a.
- The project has living documentation, a Unity/test shell, card-system and effect-capability architecture contracts, risk-tiered validation, hex foundations, a primitive obstacle map, placeholder card definitions/instances and offers, typed printed values, and authoritative visible card-driven Movement with cumulative waypoint routes and explicit Stop.
- Phase 1 does not implement broader card lifecycle, real Wasp content, card-text effects, attacks, turns, rounds, snapshots, networking, animation, or later gameplay systems.
- Milestone 2.3, initiating a basic adjacent hero Attack, is proposed but **not authorized**.
- Do not begin milestone 2.3 or any later gameplay, presentation, card, or build milestone until the user explicitly approves that exact micro-milestone.
- Required Codex profile for milestone 2.3: **GPT-5.6 Sol with medium reasoning**.
- A local Git repository exists on `main`. No remote is configured or authorized.

## Working boundaries

- All non-negotiable rules in `AGENTS.md` remain binding; this brief does not repeat them.
- Work on exactly one explicitly approved micro-milestone at a time and stop after its required handoff and local milestone commit.
- Game-rule authority, architecture contracts, validation requirements, rights restrictions, and preservation rules remain in their routed source documents.

## Targeted document routing

Use `rg` to locate headings, milestone IDs, rule-system names, capability IDs, and test names. Read the matching section plus enough neighboring context to understand prerequisites and exceptions. Do not dump an entire large document into context when a targeted section is sufficient.

| Task need | Read first | Expand only when |
| --- | --- | --- |
| Any new task or run | This brief and `AGENTS.md` | The task routes to a detailed source below |
| Approve, plan, or implement a milestone | `ROADMAP.md`: `Execution rule` and the exact milestone section | The milestone references prerequisites or its status conflicts with this brief |
| Product scope, architecture, platform, presentation, or terminology | Matching `PROJECT_CONTEXT.md` headings | The change is genuinely cross-cutting or dependencies remain unclear |
| Game-rule behavior | `RULES_LEDGER.md`: `Authority hierarchy`, matching ledger rows, relevant deviations/open questions | The task needs another rule family or a conflict must be resolved |
| Rulebook-dependent behavior | Exact relevant pages of the local second-printing rulebook | Cross-page definitions or exceptions are explicitly implicated |
| Cards, actions, combat, turns, progression, snapshots, or rules authority | Matching `CARD_SYSTEM_ARCHITECTURE.md` headings | The selected contract references another architecture boundary |
| Effects, keywords, queries, modifiers, tokens, markers, board objects, timing, or transitions | Matching `CARD_EFFECT_CAPABILITIES.md` capability sections | A dependency or shared contract is referenced |
| Automated tests | `TESTING.md`: applicable tier and exact focused commands | Test infrastructure, packages, assemblies, or a full-suite gate changes |

Read a complete detailed document only for a broad audit, a cross-cutting redesign, an unresolved documentation conflict, or when targeted reading cannot establish the required contract. Within the same task, reread only material that changed or became newly relevant.

## Model routing

The exact current or proposed milestone carries one required profile. `.codex/config.toml` mirrors it as this trusted project's default for a newly created task. Normally the user only needs to create a new task in this project and explicitly approve the milestone by ID.

- GPT-5.6 Terra with medium reasoning: bounded documentation, data, presentation, test execution, and small local changes.
- GPT-5.6 Sol with medium reasoning: game rules, card architecture, authoritative resolution, snapshots, or cross-system changes.
- GPT-5.6 Sol with high reasoning: explicit integration gates, hard gates, or unusually broad/high-risk changes.

During each milestone handoff, assign the next proposed milestone's exact profile in its roadmap section and this brief, then update `.codex/config.toml` to the same model, reasoning effort, and milestone ID before committing. Do not pre-label the distant roadmap; model names and task complexity may change before those milestones begin.

Repository files cannot switch an already-open task. If its active profile differs, the agent states the exact `/model` and `/reasoning` change and waits before implementation.

## Continuity responsibility

After an approved decision or completed milestone, update this brief when current implementation status, authorization, the next milestone, its required Codex profile, or startup routing changes. Keep detailed evidence, completion notes, rule traceability, and architecture contracts in their existing authoritative documents rather than expanding this brief into another history file.
