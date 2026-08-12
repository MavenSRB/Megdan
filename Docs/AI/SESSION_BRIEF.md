# Guards Digital - Session Brief

Last updated: 2026-08-12

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current implementation, authorization, and the exact next milestone/profile route.

## Current implementation

The project has completed roadmap milestones through 6.1. The configured vertical slice uses the pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound versioned snapshots, and passive Unity presentation, including reload-safe Hero/minion combat plus the end-of-round Minion Battle. That subsystem owns exact-count, one-at-a-time Heavy-last removal choices and records but does not yet consume a Lane-Push request; schema v6 persists both continuations. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps is maintained in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md); it is updated in place rather than extended as a milestone journal.

Configured minion views retain serialized stable binding identity across editor backup/domain restoration, and the grid atomically rebinds the passive board, pointer input, and all selectors whenever it replaces the generated view set. During a pending Hero Defense response, the immutable session projection supplies ordered Attack contributions and totals, per-card authoritative Defense previews, explicit Block/outcome meaning, readable sources, and exact signed minion identities. Hover/focus presentation is opaque, pointer-pass-through, viewport-bounded, and keeps the existing one-click typed response commands. Review 5.8c found no current gameplay or performance defect. Its approved Phase 6 disposition assigns typed serializable Minion-Battle continuation ownership to 6.1, inserts typed Skill/non-Repeat effect foundation milestone 6.2a and fresh independent verification 6.2b before 6.3, and narrows 6.7 to bounded Repeat integration.

## Current authorization and next route

- Current completed milestone: **6.1b**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.1a**.
- Required Codex profile for next milestone: **GPT-5.6 Sol with medium reasoning**.
- Milestone 6.1a is proposed and remains unauthorized until separately approved.
- The local repository is on `main`; `origin` is configured to the public Gitea repository `http://192.168.2.3:3000/maven/guards-digital.git`. On 2026-08-12, the user explicitly confirmed the required authorization to publish the tracked project content to this remote.

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
