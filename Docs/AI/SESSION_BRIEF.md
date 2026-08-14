# Guards Digital - Session Brief

Last updated: 2026-08-15

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **6.8c**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v9 persists current continuations, a paused effect frame, and placed token and attached marker components. Tokens expose catalog-owned persistent properties as continuous sources, survive independently of their source hero, clear before a validated Spawn uses their covered Spawn point, and can be removed by the general Clear replacement for an Attack. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly. It includes reload-safe Hero/minion combat and the end-of-round Minion Battle with serializable exact-count, one-at-a-time Heavy-last removal choices, now resolved by a configured player-facing prompt with board cues that forwards only the projected typed invocations; Lane-Push requests are recorded but not yet consumed. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **6.1aq**.
- Active authorized milestone: **6.1am**, in progress.
- Next proposed milestone: **6.1am**.

Milestone **6.1aq** completed on 2026-08-15. The implementation inventory is under its threshold for the first time in four milestones - 3,327 of 4,500 words - with nothing compressed to get there. Seven whole paragraphs of configured presentation surfaces moved verbatim into the new [Project/CONFIGURED_PRESENTATION.md](Project/CONFIGURED_PRESENTATION.md): HUD card panel and Defense controls, the Defense-preview popup, signed minion Defense cues, Attack-target highlighting, reload-safe minion views, and the Minion Battle removal-choice pair. The boundary is the one the inventory already drew - the passive-presentation contract stays, the surfaces built on it move - so no fact is stated twice, and two paragraphs filed under match state by chronology moved to their actual subject. `Get-Limit` already governs `Project/*.md`, so the leaf count rose from 47 to 48 with no validator change. The closing check was observed both ways: the `warning-threshold` issue was present at 4,270 words before and the run reports zero warnings after. **6.1am** is proposed next, executing on its explicit-instruction trigger rather than its ledger-warning one. Evidence lives in the completion commit, addressed from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md).

The user-approved mechanics-showcase amendment retains the next proposal and continues the dependency-safe route **6.4 -> 6.4a -> 6.4b -> 6.5 -> 6.8b -> 6.5a -> 6.5b -> 6.6 -> 6.6a -> 6.7 -> 6.7a -> 6.9 onward**. Six existing configured cards are converted only after their reusable dependencies: Red Bulwark, converted at 6.8a, sources a `+1 Defense This Turn`, self Fast-Travel restriction `This Round`, or self Move-Through permission `Next Turn`; Blue Feint separately sources self Push immunity under `Next Turn after Playing Cards` or self `Counts As` minion for `This Round`; Red Needle covers Push/Place; Red Bombard covers direct Move/Spawn; Blue Sentinel covers Swap/Replace/token-only Remove; and Blue Longbow covers bounded Repeat. In every delayed mode the hero whose Skill is resolving is the affected rules object and the card is only the stable source. Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain exactly one printed non-Ranged and one Ranged Attack per team. Every conversion preserves its card's Initiative/Secondary Movement/Defense and stable identity under a new content revision, 6.13 closes delayed-effect retrieval/cancellation, and 7.9 gates the complete ten-card matrix. Scheduling is not authorization; each new milestone still requires exact approval.

**Corrective amendment (2026-08-15, user approved).** Four milestones are scheduled from defects and thresholds found while proving 6.8a and 6.8c, making the route **6.1aq -> 6.8d -> 6.8e -> 6.8f -> 6.8g -> 6.4 onward**, with 6.8b reordered behind 6.4 by the amendment below. **6.8d** gives every Active effect a per-activation instance identity: a card played a second time currently creates nothing, because each capability composes an instance ID that repeats exactly and the lifecycle refuses a duplicate including a spent one. It is the mechanism rather than one card, reproduced against all three Red Bulwark modes, and it must precede **6.8b** or Blue Feint ships with it. **6.8e** classifies terrain as a passable obstacle rather than as off-board, so p.18's Move-Through permission can carry a unit across the 82 terrain spaces the official map declares while nobody may end there. **6.8f** requires the live modification set at every rules-consuming seam, closing the optional-default hazard 6.8's review recorded and 6.8c widened. **6.1aq** shards the implementation inventory, whose warning threshold three consecutive milestones have now deferred. `RISK-VALIDATION-001` records the harness/Unity assertion divergence 6.8c hit. Scheduling is not authorization; each still requires exact approval.

**Reordering amendment (2026-08-15, user approved).** Milestone **6.8b** moves behind Object Push into the slot before 6.5a, and corrective milestone **6.8g** is added ahead of it, making the route **6.1aq -> 6.8d -> 6.8e -> 6.8f -> 6.8g -> 6.4 -> 6.4a -> 6.4b -> 6.5 -> 6.8b -> 6.5a onward**. Neither of Blue Feint's modes had a consumer where it was scheduled: its Push immunity names an effect 6.4 has not implemented and the only immunity consumer names Attack alone, while its `Counts As` mode reaches no production caller at all. Printing either onto a card a player can hold would repeat 6.8c's defect. **6.8g** gives the effective type its first rules consumer and makes every rule naming a unit type declare the intrinsic-or-effective question it asks. 6.5a keeps its role as the configured-Push prover. Scheduling is not authorization; each still requires exact approval.

Standing route exceptions: trigger-only milestone **6.1am** remains unordered until the rules ledger reaches its warning threshold, and **6.1y** remains unordered but required before 8.1. Exact sequencing lives in [Roadmap/PHASE_6_DOCUMENTATION.md](Roadmap/PHASE_6_DOCUMENTATION.md), [Roadmap/PHASE_6_CORRECTIONS.md](Roadmap/PHASE_6_CORRECTIONS.md), and [Roadmap/PHASE_6.md](Roadmap/PHASE_6.md); scheduling is not authorization.

## Durable boundaries

- The private target remains an official-setup 3v3 pitch match with Wasp plus five later approved, source-verified heroes, retaining director mode and adding invite-only six-human internet validation before production presentation. Accounts, public matchmaking, public distribution, and production live-service scope remain excluded.
- Declared target platforms are Windows, macOS, and iOS as of 2026-08-13. Windows remains the active reference build target; macOS and iOS are declared, not supported, and iOS player work is unscheduled phase-scale work. See [Project/PRODUCT.md](Project/PRODUCT.md).
- Rules follow the second-printing rulebook, verified card data, and official rulings under [RULES_LEDGER.md](RULES_LEDGER.md). No rule may be invented, simplified, rebalanced, or silently reinterpreted.
- Pure rules/state remain independent of Unity presentation. Preserve definition/instance, action-offer, command/event, pending-choice, timing, versioning, typed effect/query, map-scoped identity, snapshot, and deterministic replay boundaries.
- The public source repository and rulebook are authorized under the user's 2026-08-13 permission confirmation. The extracted card corpus, map references, builds, and untracked material remain private; do not add or distribute them without separate explicit authorization and applicable rights.

## Targeted routing

Go straight to the shard that owns the subject when this table names it. When the owner is unknown or the task is cross-cutting, follow the index files per `AGENTS.md`; the hop is only wasted when the destination is already known.

| Need | Canonical route |
| --- | --- |
| Ownership, thresholds, archives | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) |
| Active technical risks, temporary-structure register | [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) |
| Milestone approval/implementation | [ROADMAP.md](ROADMAP.md) `Execution rule`, then the exact phase shard in [Roadmap/](Roadmap/) |
| Current implementation inventory | [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) |
| Foundation audit findings, their corrective milestones, governance state | [Project/FOUNDATION_CORRECTIONS.md](Project/FOUNDATION_CORRECTIONS.md) |
| Configured passive views, presenters, HUD/board surfaces, and board cues | [Project/CONFIGURED_PRESENTATION.md](Project/CONFIGURED_PRESENTATION.md) |
| Product scope and durable constraints | [Project/PRODUCT.md](Project/PRODUCT.md) |
| Core architecture constraints | [Project/ARCHITECTURE.md](Project/ARCHITECTURE.md) |
| Presentation and accessibility direction | [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Workflow, references, terminology | [Project/WORKFLOW_AND_TERMS.md](Project/WORKFLOW_AND_TERMS.md) |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Official map data contract and reviewed inventory | [Maps/README.md](Maps/README.md) |
| Card architecture and effect capability coverage maps | [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
