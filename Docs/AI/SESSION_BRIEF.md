# Guards Digital - Session Brief

Last updated: 2026-08-16

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **6.8l**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v11 persists current continuations, including effective hero/minion Minion-Battle identities, a paused effect frame, a paused after-action displaced-minion correction, and placed token and attached marker components. Tokens expose catalog-owned persistent properties as continuous sources, survive independently of their source hero, clear before a validated Spawn uses their covered Spawn point, and can be removed by the general Clear replacement for an Attack. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly; every Active-effect creator mints per-activation instance identity through one shared contract, and marker attachments mint theirs through its sibling, so a replayed card creates a fresh effect and a fresh attachment while re-execution inside one resolution stays a no-op. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. One shared immunity decision now combines declared activity immunity with standing Heavy immunity for candidate derivation and final mutation. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, disappears from the configured board, and receives the optional next-action Hero Respawn or resolve-without-action choice; a returned occupancy reactivates and repositions the retained hero view. Both displacements read one statement of how much of a declared distance is optional, so `up to X` admits no distance at all - a direct Move onto the mover's own space, or a zero-space push - as an applied result rather than a decline. A `Choose` offers only the options the acting player can perform: every capability declares what it would report without performing anything, an option proven to apply nothing is projected greyed and carries no invocation, and a mandatory node whose every option is refused pauses with all of them greyed and offers End Turn as the one legal command, which only the player takes. Board objects also Swap, Replace, and Remove through a fifth board port none of them could reach through the displacement commit: a Swap exchanges two occupied spaces in one transaction validated against one pre-effect state, a Replace substitutes a different object in a space that is never empty between them under a declaration stating its source, destination, property transfer and observers, and a Remove takes a piece to its declared supply through a named stage reporting removed or removal-prevented, awarding nothing and admitting no hero. Only a completed Remove asks whether the last effective Battle-Zone minion of a side has gone, so end-of-round Lane-Push requests and that mid-turn cause are both recorded but not yet consumed. A target selection declares the node it selects for and offers only the objects that step can act on, judged through the same question the option walk asks and re-read when the answer arrives rather than trusted from the offer. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **6.8l**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.6b**.

Corrective milestone **6.8l** completed on 2026-08-16: a target selection now declares the node whose eligibility it selects for, so it offers only the objects that step can act on and a branch behind one none of its candidates would serve is refused before it is chosen. This closes the last place the offer-a-mode-that-does-nothing defect could still occur, and the blind spot was the intersection of two earlier decisions rather than a fault in either - 6.5ac's walk deliberately errs toward offering what it cannot read, and 6.8k made every named subject arrive through a selection. The declaration is an ordinary literal input binding naming another node of the same definition, so the parameter type-match the validator already runs does most of the checking; a node reference is a definition-time address that no role supplies, no capability publishes, and no payload carries. The conservative reading is kept by construction wherever the pairing still cannot be read, which is why the synthetic Skill's Swap branch is deliberately left as it was. **6.6b** is proposed next. Evidence lives in the completion commit, addressed from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md).

**Corrective amendment (2026-08-16, user approved).** Two findings from 6.6's handoff became milestones in the [corrections subshard](Roadmap/PHASE_6_CORRECTIONS.md), and the route is now **6.6b -> 6.6a -> 6.7 -> 6.7a -> 6.9 onward**. **6.6b** closes the disagreement 6.6 exposed between the two paths that remove a minion: `DefeatRemovalResultKind.RemovalPrevented` has had no production producer since 5.6 while the new standalone Remove reports it, and p.13's own Bolster example states the intended behaviour - the removal is prevented and the coins are still collected. Scheduling is not authorization.

The user-approved mechanics-showcase amendment continues the dependency-safe route after those two. Six existing configured cards are converted only after their reusable dependencies: Red Bulwark, converted at 6.8a, sources a `+1 Defense This Turn`, self Fast-Travel restriction `This Round`, or self Move-Through permission `Next Turn`; Blue Feint separately sources self Push immunity under `Next Turn after Playing Cards` or self `Counts As` minion for `This Round`; Red Needle covers Push/Place; Red Bombard covers direct Move/Spawn; Blue Sentinel covers Swap/Replace/token-only Remove; and Blue Longbow covers bounded Repeat. In every delayed mode the hero whose Skill is resolving is the affected rules object and the card is only the stable source. Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain exactly one printed non-Ranged and one Ranged Attack per team. Every conversion preserves its card's Initiative/Secondary Movement/Defense and stable identity under a new content revision, 6.13 closes delayed-effect retrieval/cancellation, and 7.9 gates the complete ten-card matrix. Scheduling is not authorization; each new milestone still requires exact approval.

Standing route exceptions: **6.1y** remains unordered but required before 8.1. Exact sequencing lives in [Roadmap/PHASE_6_DOCUMENTATION.md](Roadmap/PHASE_6_DOCUMENTATION.md), [Roadmap/PHASE_6_CORRECTIONS.md](Roadmap/PHASE_6_CORRECTIONS.md), and [Roadmap/PHASE_6.md](Roadmap/PHASE_6.md); scheduling is not authorization.

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
| Implemented modifier/restriction/permission/immunity/`Counts As` contract and its consumers | [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md) |
| Implemented token/marker, displacement, Clear, Place and Spawn operations | [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md) |
| Product scope and durable constraints | [Project/PRODUCT.md](Project/PRODUCT.md) |
| Core architecture constraints | [Project/ARCHITECTURE.md](Project/ARCHITECTURE.md) |
| Presentation and accessibility direction | [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Workflow, references, terminology | [Project/WORKFLOW_AND_TERMS.md](Project/WORKFLOW_AND_TERMS.md) |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Official map data contract and reviewed inventory | [Maps/README.md](Maps/README.md) |
| Card architecture and effect capability coverage maps | [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md) |
| Per-system rule status, primary sources, and verification notes | [Rules/RULE_SYSTEMS.md](Rules/RULE_SYSTEMS.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
