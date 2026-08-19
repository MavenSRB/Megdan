# Guards Digital - Session Brief

Last updated: 2026-08-19

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **7.4**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v20 persists current continuations, every hero's four dashboard item slots, their current Level and their items, every team's remaining Life counters, including effective hero/minion Minion-Battle identities, a paused effect frame, a paused after-action displaced-minion correction and any repeated action instance it owes, an owed mid-turn Lane-Push request, placed token and attached marker components, rulebook p.8's Battle Zone and Wave counters, and a paused system transition down to the minion a step waits on. Active effects and token properties declare typed value modifiers, restrictions, permissions, immunity and `Counts As`, which the value, offer, targeting, presence, effective-type and obstacle queries consult explicitly; [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md) owns that contract and its consumers, per-activation instance identity included. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. One shared immunity decision now combines declared activity immunity with standing Heavy immunity for candidate derivation and final mutation. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, because p.19's Remove is not p.13's Defeat. A successful Hero Attack is, and now runs p.13's whole list inside the Attack and in the page's order: the hero is removed, its unresolved card is resolved without an action, the effects its own actions created are cancelled and its markers returned while the tokens it placed stay, the attacker collects coins equal to its Level and friendly heroes assist coins from its lowest non-Basic card Tier, and its team spends that many Life counters - the last of which ends the match, p.13's third victory path beside p.8's two. Either way the hero receives the optional next-action Hero Respawn or resolve-without-action choice, now over the nearest empty space on the whole map rather than the spawn point's own ring; a returned occupancy reactivates and repositions the retained hero view. A `Choose` offers only options the acting player can perform; a mandatory node whose every option is refused pauses behind an End Turn only the player takes. Every board operation the slice implements is stated once in [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md), which owns what each decides and what it records, the shared removal stage p.13's Defeat now asks included. A target selection declares the node it selects for and offers only the objects that step can act on, judged through the same question the option walk asks and re-read when the answer arrives rather than trusted from the offer. Placed tokens reach presentation as authoritative state carrying their trusted definitions; [Project/CONFIGURED_PRESENTATION.md](Project/CONFIGURED_PRESENTATION.md) owns every configured surface, including how one is drawn and which candidate is lit. A definition can also ask for its own action to happen again, bounded at two instances by construction rather than by a count: the second begins only after p.18's after-action correction finishes, under a context that makes its own repeat ineligible. Every card definition now states a typed colour and either a Tier or rulebook p.15's explicit absence of one, each hand is that page's five colours with the Gold and Silver Basic pair plus the upgrade cards they lead to and p.16's Tier IV Ultimate, one pure query reads all of that together with owner, location and lifecycle while suppressing every printed characteristic of a facedown card, and every hero has an authoritative Level, four dashboard item slots and a set of items. One query decides p.16's +1 for all six icons and composes it into every total the build computes, Radius included since a primary Skill action can print p.9's Radius icon and one named query derives its inclusive disk. Unlocking an Ultimate moves the card from the deck to the left of its dashboard once the stated reading's non-Basic cards are all Tier III, and its printed ability reaches its own hero exactly while that hero is on the board, on the continuous-source seam a placed token's properties use rather than as a cancelable Active effect. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **7.4**.
- Active authorized milestone: **none**.
- Next proposed milestone: **7.5**.

Milestone **7.4** completed on 2026-08-19 and resolved rulebook p.13's "When a Hero is Defeated" list in full. Every line runs inside the Attack that caused it and in the page's printed order, which p.14's numbered Attack/Defense order fixes at its step 6, ahead of the card's After-the-attack text. The three numbers are declared content read from p.13's own table: coins equal to the defeated hero's Level, assist coins and Life counters equal to its lowest non-Basic card Tier, taken over every card it owns rather than over one location. Spending a team's last counter ends the match, the third victory path beside p.8's two, observed once at the round's turn-command boundary. The same milestone discharged ER2 note `F-ER2-09` by moving the standard-minion coin values and p.14's Defense modifier table into declared content with every number unchanged, and widened p.7's respawn fallback to the nearest empty space on the whole map. Schema moves to v20 for the counters alone, measured at +31 bytes per payload. Evidence lives in the completion commit, addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

Milestones 7.1 through 7.3 are complete and addressed by SHA from the same archive: card colour, Tier, upgrade candidates and hero Level; the colour-restricted discard reachable from a card; p.16's items, dashboards and bonus composition; p.9's printed Radius with its total, disk and target/presence boundary, bound to one configured card's Place; and p.16's Ultimate with the continuous ability it supplies.

The remaining route is **7.5 onward**; 7.5 requires exact approval to execute. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone left inside it.

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
