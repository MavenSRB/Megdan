# Guards Digital - Session Brief

Last updated: 2026-08-13

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current implementation, authorization, and the exact next milestone route.

## Current implementation

The project has completed roadmap milestones through **6.1i**. The configured vertical slice uses the pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound versioned snapshots, and passive Unity presentation, including reload-safe Hero/minion combat plus the end-of-round Minion Battle. That subsystem owns exact-count, one-at-a-time Heavy-last removal choices and records but does not yet consume a Lane-Push request; schema v6 persists both continuations. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps is maintained in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md); it is updated in place rather than extended as a milestone journal.

Configured minion views retain serialized stable binding identity across editor backup/domain restoration, and the grid atomically rebinds the passive board, pointer input, and all selectors whenever it replaces the generated view set. During a pending Hero Defense response, the immutable session projection supplies ordered Attack contributions and totals, per-card authoritative Defense previews, explicit Block/outcome meaning, readable sources, and exact signed minion identities. Hover/focus presentation is opaque, pointer-pass-through, viewport-bounded, and keeps the existing one-click typed response commands. Review 5.8c found no current gameplay or performance defect. Its approved Phase 6 disposition assigns typed serializable Minion-Battle continuation ownership to 6.1, inserts typed Skill/non-Repeat effect foundation milestone 6.2a and fresh independent verification 6.2b before 6.3, and narrows 6.7 to bounded Repeat integration.

## Current authorization and next route

- Current completed milestone: **6.1i**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.1h**.
- Milestone 6.1i made the per-command cost measured and bounded. An accepted command now performs one authoritative serialization and no rule-event envelope encode; a refused command performs one with self-verification on and none with it off, each asserted exactly by `MatchSessionDiagnosticCostTests` rather than left to inspection. The aggregate memoizes its authoritative snapshot, the projection's `DiagnosticSnapshot` is computed on demand from inputs it already holds immutably, and the rollback pre-image carries the two rule-event streams as in-memory arrays, so nothing per-command grows with session length. A typed `MatchDiagnosticsMode` gates the self-verification, chosen by the composition root; a thrown defect reports as `EngineFault` naming its exception type instead of `DomainRejected`; and `Projection.IsHistoryAvailable` distinguishes a session that loaded a snapshot from one that simply has nothing to undo. `RISK-PERF-001` is retired.
- Milestone 6.1f is complete across three commits. Card state cannot be mutated outside `CardLifecycleAuthority`, enforced by moving `CardInstance` and that authority into the `GuardsDigital.Core.CardState` assembly below Core, and the Attack/Defense backend's direct location transition now goes through it. `MatchAggregate` in `GuardsDigital.Application` owns the configuration, board, card-action, and round graphs plus the whole-match rule-event log and the current round's ordered events, installs the four graphs at one assignment site, and exposes one commit boundary at which an operation's state and its events are kept or discarded together. All nine pinned `CardAggregateInvarianceTests` digests are byte-identical to the pre-refactor revision.
- One limitation is stated rather than implied: `DefeatRewardAccounting.Award` plans into a copied dictionary and writes real state only at the end, whereas the authorities behind this aggregate mutate in place, so the pre-image is a serialized snapshot restored on rejection. The guarantee to the caller is the same - no partial state - but the mechanism is a rollback, not a deferred write, and it therefore protects only state the snapshot round-trips. `RISK-STATE-002` in [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) owns that residual; 6.1i owns the pre-image's cost.
- Milestones 6.1c through 6.1k are an approved corrective sequence scheduled by the 2026-08-13 out-of-band foundation audit. They precede 6.1a, which stays proposed behind them. The shard defines their execution order by severity; 6.1e, 6.1g, 6.1c, 6.1d, 6.1f, and 6.1i are complete and 6.1h is next. Former 6.1j and 6.1m are merged into 6.1d, and former 6.1n was removed when review-correction closure became mechanical.
- Milestone **6.1l** was reinstated on 2026-08-13 and is `[!]` blocked: confirming the validation toolchain on a non-Windows platform needs a non-Windows host, and this machine has no WSL distribution, no Docker, and no PowerShell Core. It owns `F613-14`, which 6.1d did not close.
- The **player-build smoke gate** is now a standing check. Run `Tools/Invoke-PlayerBuildSmoke.ps1` whenever the always-included shader list, the enabled scene list, the render pipeline, `Assets/Resources`, an assembly definition, or a stripping setting changes; see [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md).
- The local repository is on `main` with no Git remote configured. On 2026-08-13, the user explicitly requested removal of the prior `origin` configuration; local history and branches remain intact.

## Durable boundaries

- The private target remains an official-setup 3v3 pitch match with Wasp plus five later approved, source-verified heroes, retaining director mode and adding invite-only six-human internet validation before production presentation. Accounts, public matchmaking, public distribution, and production live-service scope remain excluded.
- Declared target platforms are Windows, macOS, and iOS as of 2026-08-13. Windows remains the active reference build target; macOS and iOS are declared, not supported, and iOS player work is unscheduled phase-scale work. See [Project/PRODUCT.md](Project/PRODUCT.md).
- Rules follow the second-printing rulebook, verified card data, and official rulings under [RULES_LEDGER.md](RULES_LEDGER.md). No rule may be invented, simplified, rebalanced, or silently reinterpreted.
- Pure rules/state remain independent of Unity presentation. Preserve definition/instance, action-offer, command/event, pending-choice, timing, versioning, typed effect/query, map-scoped identity, snapshot, and deterministic replay boundaries.
- The rulebook, extracted card corpus, project assets, builds, and project remain private. Do not add excluded references to Git or publish/upload/distribute anything without explicit authorization and applicable rights.
- Work on one approved micro-milestone, run its routed validation, perform the required four-perspective review-only pass, update the continuity documents its subject touched, create the local milestone commit carrying the evidence record, report the next proposal, and stop.

## Targeted routing

Go straight to the shard that owns the subject. The index files still exist and remain valid link targets, but reading one only to reach a shard named below is a wasted hop.

| Need | Canonical route |
| --- | --- |
| Ownership, thresholds, archives | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) |
| Active technical risks, temporary-structure register | [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) |
| Milestone approval/implementation | [ROADMAP.md](ROADMAP.md) `Execution rule`, then the exact phase shard in [Roadmap/](Roadmap/) |
| Current implementation inventory | [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) |
| Product scope and durable constraints | [Project/PRODUCT.md](Project/PRODUCT.md) |
| Core architecture constraints | [Project/ARCHITECTURE.md](Project/ARCHITECTURE.md) |
| Presentation and accessibility direction | [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Workflow, references, terminology | [Project/WORKFLOW_AND_TERMS.md](Project/WORKFLOW_AND_TERMS.md) |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict. At every handoff run the living-document validator and `git diff --check`; Unity validation remains controlled solely by the applicable testing tier.
