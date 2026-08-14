# Guards Digital - Session Brief

Last updated: 2026-08-14

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current implementation, authorization, and the exact next milestone route.

## Current implementation

The project has completed roadmap milestones through **6.1ai**. The configured vertical slice uses the pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound versioned snapshots, and passive Unity presentation, including reload-safe Hero/minion combat plus the end-of-round Minion Battle. That subsystem owns exact-count, one-at-a-time Heavy-last removal choices and records but does not yet consume a Lane-Push request; schema v7 persists both continuations. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps is maintained in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md); it is updated in place rather than extended as a milestone journal.

Configured minion views retain serialized stable binding identity across editor backup/domain restoration, and the grid atomically rebinds the passive board, pointer input, and all selectors whenever it replaces the generated view set. During a pending Hero Defense response, the immutable session projection supplies ordered Attack contributions and totals, per-card authoritative Defense previews, explicit Block/outcome meaning, readable sources, and exact signed minion identities. Hover/focus presentation is opaque, pointer-pass-through, viewport-bounded, and keeps the existing one-click typed response commands. Review 5.8c found no current gameplay or performance defect. Its approved Phase 6 disposition assigns typed serializable Minion-Battle continuation ownership to 6.1, inserts typed Skill/non-Repeat effect foundation milestone 6.2a and fresh independent verification 6.2b before 6.3, and narrows 6.7 to bounded Repeat integration.

## Current authorization and next route

- Current completed milestone: **6.1ai**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.1aj**.
- Milestone **6.1ai** completed on 2026-08-14. The card-system shards distinguish implemented contracts from specified ones everywhere they name one: the 2.3a distance slice no longer presents `DistanceQuery`/`RangeQuery` as shipped, the capability document states the 6.1h removal and 6.2a planned seam in its extension guidance, `CORE_MODEL.md` states the contract check's actual suffix-matched reach, the timing shard gained its own status section, and `CardActionResolutionAuthority` is named as the dispatcher's carrier.
- On 2026-08-14 the user approved a roadmap amendment defining milestones **6.1ae**-**6.1am** in [Roadmap/PHASE_6_DOCUMENTATION.md](Roadmap/PHASE_6_DOCUMENTATION.md), which carries the documentation block's execution order (6.1z first) and the user's approval of the block's sequential execution; the same handoff's required ownership review moved the completed corrective-sequence definitions to [Roadmap/PHASE_6_COMPLETED.md](Roadmap/PHASE_6_COMPLETED.md). Gameplay resumes after the block as 6.1u, 6.1a, then 6.2a.
- On 2026-08-14 the user approved a roadmap amendment ordering **6.2a** before **6.2**, and 6.2 before **6.2b**. 6.2 needs the serializable `Choose` frame 6.2a builds; 6.2b then verifies the 6.1r occupancy seam against real tokens. Scheduling is not authorization.
- On 2026-08-14 the user approved a roadmap amendment scheduling **6.1z**, **6.1aa**, **6.1ab**, and **6.1ac** from a documentation-truth finding, not a review: a green validator run proves link hygiene, not governing-document truth. Scheduling is not authorization.
- On 2026-08-13 the user approved a roadmap amendment scheduling **6.1y**: each hero owns a rules-ledger shard under `Rules/`, starting with Wasp. Unordered within Phase 6; must complete before 8.1. Scheduling is not authorization.
- On 2026-08-13 the user approved a roadmap amendment scheduling **6.1x**, **6.1v**, and **6.1w** from the 6.1s Windows handoff and the complete-suite run that followed it - not from the ER2 review. Each closes a way the validation route can report green while a directly affected fixture never executed.
- The corrective sequence 6.1c through 6.1p is complete and its account - what the 2026-08-13 audit found, what closed each finding, and which earlier claims were corrected - is maintained in [Project/FOUNDATION_CORRECTIONS.md](Project/FOUNDATION_CORRECTIONS.md). Per-milestone evidence is in each commit body, addressed by the SHA in the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md). `RISK-STATE-002` in [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) owns the one residual that sequence did not close: the commit boundary rolls back from a serialized pre-image rather than withholding a deferred write, so it protects only state the snapshot round-trips.
- The **player-build smoke gate** is now a standing check. Run `Tools/Invoke-PlayerBuildSmoke.ps1` whenever the always-included shader list, the enabled scene list, the render pipeline, `Assets/Resources`, an assembly definition, or a stripping setting changes; see [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md).
- The local repository is on `main`. On 2026-08-13, the user explicitly authorized the public `maven/guards-digital` Gitea repository as `origin` and confirmed relevant permission to publish the tracked source and rulebook there; excluded reference corpora, builds, and untracked material remain outside that authorization.
- On 2026-08-13 a second external engineering review (ER2, by Claude on the Linux workstation) was recorded in the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2) after the user confirmed this line as canonical over the superseded external ER1/ER2 line, which is preserved on branch `er2-external-review-line`. The user approved, in this order: its documentation-truth corrections, executing milestone 6.1l, the roadmap scheduling amendment that created milestones 6.1q/6.1r/6.1s/6.1t/6.1u/8.28b, and then explicit execution of **6.1t, 6.1q, 6.1r, and 6.1s in that order** in the same supervised external session. Milestones 6.1u and 8.28b remain unauthorized proposals.

## Durable boundaries

- The private target remains an official-setup 3v3 pitch match with Wasp plus five later approved, source-verified heroes, retaining director mode and adding invite-only six-human internet validation before production presentation. Accounts, public matchmaking, public distribution, and production live-service scope remain excluded.
- Declared target platforms are Windows, macOS, and iOS as of 2026-08-13. Windows remains the active reference build target; macOS and iOS are declared, not supported, and iOS player work is unscheduled phase-scale work. See [Project/PRODUCT.md](Project/PRODUCT.md).
- Rules follow the second-printing rulebook, verified card data, and official rulings under [RULES_LEDGER.md](RULES_LEDGER.md). No rule may be invented, simplified, rebalanced, or silently reinterpreted.
- Pure rules/state remain independent of Unity presentation. Preserve definition/instance, action-offer, command/event, pending-choice, timing, versioning, typed effect/query, map-scoped identity, snapshot, and deterministic replay boundaries.
- The public source repository and rulebook are authorized under the user's 2026-08-13 permission confirmation. The extracted card corpus, map references, builds, and untracked material remain private; do not add or distribute them without separate explicit authorization and applicable rights.
- Work on one approved micro-milestone, run its routed validation, perform the required four-perspective review-only pass, update the continuity documents its subject touched, create the local milestone commit carrying the evidence record, report the next proposal, and stop.

## Targeted routing

Go straight to the shard that owns the subject. The index files still exist and remain valid link targets, but reading one only to reach a shard named below is a wasted hop.

| Need | Canonical route |
| --- | --- |
| Ownership, thresholds, archives | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) |
| Active technical risks, temporary-structure register | [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) |
| Milestone approval/implementation | [ROADMAP.md](ROADMAP.md) `Execution rule`, then the exact phase shard in [Roadmap/](Roadmap/) |
| Current implementation inventory | [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) |
| Foundation audit findings, their corrective milestones, governance state | [Project/FOUNDATION_CORRECTIONS.md](Project/FOUNDATION_CORRECTIONS.md) |
| Product scope and durable constraints | [Project/PRODUCT.md](Project/PRODUCT.md) |
| Core architecture constraints | [Project/ARCHITECTURE.md](Project/ARCHITECTURE.md) |
| Presentation and accessibility direction | [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Workflow, references, terminology | [Project/WORKFLOW_AND_TERMS.md](Project/WORKFLOW_AND_TERMS.md) |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Card architecture and effect capability coverage maps | [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict. At every handoff run the living-document validator and `git diff --check`; Unity validation remains controlled solely by the applicable testing tier.
