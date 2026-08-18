# Guards Digital - Session Brief

Last updated: 2026-08-18

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **6.10a**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v15 persists current continuations, including effective hero/minion Minion-Battle identities, a paused effect frame, a paused after-action displaced-minion correction and any repeated action instance it owes, a mid-turn Lane-Push request a completed action left owing, placed token and attached marker components, rulebook p.8's current Battle Zone, and a paused multi-step system transition. Tokens expose catalog-owned persistent properties as continuous sources, survive independently of their source hero, clear before a validated Spawn uses their covered Spawn point, and can be removed by the general Clear replacement for an Attack. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly; every Active-effect creator mints per-activation instance identity through one shared contract, and marker attachments mint theirs through its sibling, so a replayed card creates a fresh effect and a fresh attachment while re-execution inside one resolution stays a no-op. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. One shared immunity decision now combines declared activity immunity with standing Heavy immunity for candidate derivation and final mutation. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, disappears from the configured board, and receives the optional next-action Hero Respawn or resolve-without-action choice; a returned occupancy reactivates and repositions the retained hero view. Both displacements read one statement of how much of a declared distance is optional, so `up to X` admits no distance at all - a direct Move onto the mover's own space, or a zero-space push - as an applied result rather than a decline. A `Choose` offers only the options the acting player can perform: every capability declares what it would report without performing anything, an option proven to apply nothing is projected greyed and carries no invocation, and a mandatory node whose every option is refused pauses with all of them greyed and offers End Turn as the one legal command, which only the player takes. Board objects also Swap, Replace, and Remove through their own board port, each declaring which kind of piece it may take, and one shared removal stage decides whether a minion leaves the board for that Remove and for p.13's Defeat alike. p.8's Battle Zone is now authoritative map-scoped state rather than a reading of the map's flag: it opens at the flagged zone, is what the Minion Battle, Heavy immunity, the displaced-minion correction and the Lane-Push cause all read, and moves only through the ordered Lane-Push prefix, which flips one shared Wave counter and stops on the final one, otherwise advances the zone one verified step towards the losing team's Throne and stops on its capture, and reports the page's third step unimplemented. Nothing in a live match reaches that prefix, so both Lane-Push requests are still recorded and refused at their own checkpoints. What every one of those operations decides, and what it records, lives in [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md). A target selection declares the node it selects for and offers only the objects that step can act on, judged through the same question the option walk asks and re-read when the answer arrives rather than trusted from the offer. Placed tokens now reach presentation as authoritative state carrying their trusted definitions, the configured scene deals the card that can place one, and a token is drawn at its coordinate under a retained-view pool, named by what it is wherever a prompt offers it; a destination that carries its own control is lit only while that control is pointed at, while a candidate whose only surface is the board keeps its standing highlight. A definition can also ask for its own action to happen again: the request performs nothing, the instance that made it ends normally, p.18's after-action correction finishes, and only then does a second instance begin under an explicit repeated context that makes its own repeat ineligible, so repetition is bounded at two instances by construction rather than by a count. A configured card prints exactly that, and because the correction runs between them, the second instance reads the board, the modifications and its own legal choices for itself. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **6.10a**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.11**.

Milestone **6.10a** completed on 2026-08-18: a multi-step authoritative system transition can now stop in the middle of itself, on a player, and resume exactly where it stopped. The continuation is authority-owned serializable state rather than a handler-local callback - it records the owning run and definition, the stable current step, the chooser, the legal options with the exact commands that answer them, and the completed steps that are its resume location - and a conformance rule keeps any driver from holding a second record of that position. Accepted, rejected, stale, restored, replayed and Undo-truncated resumptions cannot skip, repeat or restart a completed step. Definitions are catalog content the payload names rather than describes, so a restore rebinds them and refuses a transition this build no longer supplies. Its first consumer is a bounded synthetic transition; 6.11's Lane-Push respawn and 7.5's progression are the named production consumers and are not built. Schema moves to v15 and rejects v14. Evidence lives in the completion commit, addressed from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md).

The remaining route is **6.11 onward**; 6.11 still requires exact approval to execute.

The user-approved mechanics-showcase amendment is finished: all six configured cards are converted, each after its reusable dependencies, and every one preserved its Initiative, Secondary Movement, Defense, and stable identity under a new content revision. Which card carries which modes, and why, lives in [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md). Two constraints still bind: Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain the printed Attacks that keep one non-Ranged and one Ranged per team, which is now the floor rather than a margin, and the hero whose Skill is resolving is the affected rules object while the card is only the stable source. Milestone 6.13 closes delayed-effect retrieval/cancellation and 7.9 gates the complete ten-card matrix.

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
