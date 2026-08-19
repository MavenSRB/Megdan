# Guards Digital - Session Brief

Last updated: 2026-08-19

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **7.2c**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v19 persists current continuations, every hero's four dashboard item slots, their current Level and their items, including effective hero/minion Minion-Battle identities, a paused effect frame, a paused after-action displaced-minion correction and any repeated action instance it owes, an owed mid-turn Lane-Push request, placed token and attached marker components, rulebook p.8's Battle Zone and Wave counters, and a paused system transition down to the minion a step waits on. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly; every Active-effect creator mints per-activation instance identity through one shared contract, and marker attachments mint theirs through its sibling, so a replayed card creates a fresh effect and a fresh attachment while re-execution inside one resolution stays a no-op. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. One shared immunity decision now combines declared activity immunity with standing Heavy immunity for candidate derivation and final mutation. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, disappears from the configured board, and receives the optional next-action Hero Respawn or resolve-without-action choice; a returned occupancy reactivates and repositions the retained hero view. A `Choose` offers only the options the acting player can perform, and a mandatory node whose every option is refused pauses with them greyed behind an End Turn only the player takes. Every board operation the slice implements is stated once in [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md), which owns what each decides and what it records: components and both cleanup checkpoints, Place and Spawn, both displacements and the shared statement of optional distance they read, Clear, Swap, Replace, Remove and the shared removal stage, and p.8's complete Lane Push with both of its recorded requests consumed. A target selection declares the node it selects for and offers only the objects that step can act on, judged through the same question the option walk asks and re-read when the answer arrives rather than trusted from the offer. Placed tokens now reach presentation as authoritative state carrying their trusted definitions, the configured scene's five-card hands place one from Blue Feint, and a token is drawn at its coordinate under a retained-view pool, named by what it is wherever a prompt offers it; a destination that carries its own control is lit only while that control is pointed at, while a candidate whose only surface is the board keeps its standing highlight. A definition can also ask for its own action to happen again, and repetition is bounded at two instances by construction rather than by a count: the second begins only after p.18's after-action correction finishes, under a repeated context that makes its own repeat ineligible, so it reads the board and its own legal choices for itself. Every card definition now states a typed colour and either a Tier or rulebook p.15's explicit absence of one, each hand is that page's five colours with the Gold and Silver Basic pair, each non-Basic Tier I card names the two same-colour Tier II cards it upgrades into and they the two Tier III, one pure query reads all of that together with owner, location and lifecycle while suppressing every printed characteristic of a facedown card, and every hero has an authoritative Level that starts at 1 and moves for nothing until milestone 7.5. Every Tier II and Tier III definition also prints p.16's Item icon, every hero declares that page's four dashboard item slots, a set-aside card becomes an item under the slot accepting its icon or is refused, and one query decides p.16's +1 for all six icons and composes it into every total the build computes, Radius included since a primary Skill action can print p.9's Radius icon and one named query derives its inclusive disk. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **7.2c**.
- Active authorized milestone: **7.3**.
- Next proposed milestone: **7.3**.

Milestone **7.2c** completed on 2026-08-19 and put a printed Radius on the one configured card whose text already named one. Red Needle prints p.9's icon at the 4 its 6.5a text stated, and its Place branch binds the composed Radius Total a small publishing capability supplies rather than a number written into the effect - so p.16's matching item widens where the hero may be placed by exactly one ring, while the Push's 3 spaces beside it stays card text and does not move. A card printing no icon publishes nothing rather than a zero. Schema stays v19; the causes are the registered capability and this one card's revision, measured at +28 bytes per payload with the other nine cards untouched. Evidence lives in the completion commit, addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

Milestones 7.1 through 7.2b are complete and addressed by SHA from the same archive: card colour, Tier, upgrade candidates and hero Level; the colour-restricted discard reachable from a card; p.16's items, dashboards and bonus composition; and p.9's printed Radius with its total, disk and target/presence boundary.

The remaining route is **7.3 onward**; 7.3 requires exact approval to execute. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone left inside it.

The user-approved mechanics-showcase amendment is finished; which card carries which modes, and why, lives in [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md). Two constraints still bind: Red Lancer, Red Pursuit, Blue Scout Shot and Blue Hammer keep the printed Attacks holding one non-Ranged and one Ranged per team, now the floor rather than a margin; and a resolving Skill's hero is the affected rules object, the card only the stable source. Milestone 7.9 later gates the ten-card matrix.

Standing route exceptions: none remain. Phase 6's exact sequencing lives in [Roadmap/PHASE_6_DOCUMENTATION.md](Roadmap/PHASE_6_DOCUMENTATION.md), [Roadmap/PHASE_6_CORRECTIONS.md](Roadmap/PHASE_6_CORRECTIONS.md), and [Roadmap/PHASE_6.md](Roadmap/PHASE_6.md), and its per-milestone evidence is addressed by SHA from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md); scheduling is not authorization.

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
| Hero-scoped inventory, capability mapping, and verification status | `Rules/HERO_<NAME>.md`, starting with [Rules/HERO_WASP.md](Rules/HERO_WASP.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
