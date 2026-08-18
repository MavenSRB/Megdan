# Guards Digital - Session Brief

Last updated: 2026-08-18

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **7.1**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v17 persists current continuations and every hero's current Level, including effective hero/minion Minion-Battle identities, a paused effect frame, a paused after-action displaced-minion correction and any repeated action instance it owes, an owed mid-turn Lane-Push request, placed token and attached marker components, rulebook p.8's Battle Zone and Wave counters, and a paused system transition down to the minion a step waits on. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly; every Active-effect creator mints per-activation instance identity through one shared contract, and marker attachments mint theirs through its sibling, so a replayed card creates a fresh effect and a fresh attachment while re-execution inside one resolution stays a no-op. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. One shared immunity decision now combines declared activity immunity with standing Heavy immunity for candidate derivation and final mutation. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, disappears from the configured board, and receives the optional next-action Hero Respawn or resolve-without-action choice; a returned occupancy reactivates and repositions the retained hero view. A `Choose` offers only the options the acting player can perform: every capability declares what it would report without performing anything, an option proven to apply nothing is projected greyed and carries no invocation, and a mandatory node whose every option is refused pauses with all of them greyed and offers End Turn as the one legal command, which only the player takes. Every board operation the slice implements is stated once in [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md), which owns what each decides and what it records: components and both cleanup checkpoints, Place and Spawn, both displacements and the shared statement of optional distance they read, Clear, Swap, Replace, Remove and the shared removal stage, and p.8's complete Lane Push with both of its recorded requests consumed. A target selection declares the node it selects for and offers only the objects that step can act on, judged through the same question the option walk asks and re-read when the answer arrives rather than trusted from the offer. Placed tokens now reach presentation as authoritative state carrying their trusted definitions, the configured scene's five-card hands place one from Blue Feint, and a token is drawn at its coordinate under a retained-view pool, named by what it is wherever a prompt offers it; a destination that carries its own control is lit only while that control is pointed at, while a candidate whose only surface is the board keeps its standing highlight. A definition can also ask for its own action to happen again, and repetition is bounded at two instances by construction rather than by a count: the second begins only after p.18's after-action correction finishes, under a repeated context that makes its own repeat ineligible, so it reads the board and its own legal choices for itself. Every card definition now states a typed colour and either a Tier or rulebook p.15's explicit absence of one, each hand is that page's five colours with the Gold and Silver Basic pair, each non-Basic Tier I card names the two same-colour Tier II cards it upgrades into and they the two Tier III, one pure query reads all of that together with owner, location and lifecycle while suppressing every printed characteristic of a facedown card, and every hero has an authoritative Level that starts at 1 and moves for nothing until milestone 7.5. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **7.1**.
- Active authorized milestone: **none**.
- Next proposed milestone: **7.2**.

Milestone **7.1** completed on 2026-08-18 and opened Phase 7: synthetic cards now carry rulebook p.15's progression content, and every hero has a Level. Each of the ten configured definitions states a typed `CardColor`, a Basic or non-Basic classification, and either a Tier or the page's explicit absence of one, so all ten advance to `phase-7-1-rev-1`; each hand is p.15's five distinct colours with the Gold and Silver Basic pair, and the assignment is deliberately unreproducible from team, name, hand slot or printed icon, because both teams hold a Red-coloured and a Blue-coloured card. Each of the six non-Basic Tier I cards supplies the four same-colour cards p.15 has it upgrade into - two Tier II, and the two Tier III both of those lead to - as definitions the hand states and does not deal, since the page finds them in the player's own deck and milestone 7.5 is the first that moves one. `CardCharacteristicQuery` is the one place a colour is read, `CardProgressionValidator` checks every stated upgrade against the colour and Tier rules over the whole set so the relationship stays explicit content, and `ColorRestrictedDiscard` composes the query with the effect frame's own typed pending choice and with `DiscardCard`, renamed from its Defense-specific name and deciding nothing about colour, ownership or chooser. Hero Level is authoritative state initialized to 1, persisted in schema v17 with exact per-hero coverage on restore, and moved by nothing until 7.5 supplies the purchase. Evidence lives in the completion commit, addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

The remaining route is **7.2 onward**; 7.2 requires exact approval to execute. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone left inside it.

The user-approved mechanics-showcase amendment is finished: all six configured cards are converted, each after its reusable dependencies, and every one preserved its Initiative, Secondary Movement, Defense, and stable identity under a new content revision. Which card carries which modes, and why, lives in [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md). Two constraints still bind: Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain the printed Attacks that keep one non-Ranged and one Ranged per team, which is now the floor rather than a margin, and the hero whose Skill is resolving is the affected rules object while the card is only the stable source. Milestone 7.9 later gates the complete ten-card matrix.

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
