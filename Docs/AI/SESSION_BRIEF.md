# Guards Digital - Session Brief

Last updated: 2026-08-20

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **7.8a1**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v21 persists every continuation the slice can pause on - a paused effect frame, an after-action displaced-minion correction and any repeated action it owes, an owed mid-turn Lane-Push request, and a system transition down to the item a step waits on - beside every hero's Level, dashboard slots and items, which heroes levelled this round, every team's Life counters, effective Minion-Battle identities, placed tokens and attached markers, and rulebook p.8's Battle Zone and Wave counters. Active effects and token properties declare typed value modifiers, restrictions, permissions, immunity and `Counts As`, which the value, offer, targeting, presence, effective-type and obstacle queries consult explicitly; [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md) owns that contract and its consumers, per-activation instance identity included. Terrain is a passable obstacle distinct from off-board: the official map's 82 declared terrain cells cost a Move-Through mover nothing extra, and no unit may end a Movement or be placed there. Minion Battle counts effective minions; a removed counts-as hero leaves the board without Defeat/reward/Life consequences, because p.19's Remove is not p.13's Defeat. A successful Hero Attack is, and runs p.13's whole list inside the Attack in that page's order, down to the coins the attacker and its friendly heroes collect and the Life counters the defeated team spends - the last of which ends the match, through the one contract p.8's Final Wave and Throne capture also end it by. Either way the hero receives the optional next-action Hero Respawn or resolve-without-action choice over the nearest empty space on the whole map; a returned occupancy reactivates and repositions the retained hero view. Every board operation the slice implements is stated once in [Project/BOARD_OPERATIONS.md](Project/BOARD_OPERATIONS.md), which owns what each decides and what it records. [Project/CONFIGURED_PRESENTATION.md](Project/CONFIGURED_PRESENTATION.md) owns every configured board surface, including how a placed token is drawn and which candidate is lit. A definition can also ask for its own action to happen again, bounded at two instances by construction rather than by a count: the second begins only after p.18's after-action correction finishes. Every card definition states a typed colour and either a Tier or rulebook p.15's explicit absence of one, and each hero now holds p.6's whole eighteen-card deck, read by one pure query together with owner, location and lifecycle while a facedown card's printed characteristics stay suppressed. Every hero also has an authoritative Level, four dashboard item slots and a set of items. p.8's Level Up step is mandatory and runs at every round's end: a hero buys as many consecutive levels as p.15's table lets it afford from p.13's coins, and each purchase either upgrades one chosen lowest-Tier card into a next-Tier card and an item, or unlocks the Ultimate once every non-Basic card is Tier III. The whole transaction is one intra-stage system transition, so the single choice it asks survives snapshot, replay and Undo. p.8's last step then pays one Pity Coin to every hero that did not level up that round. A player now sees all of it: [Project/MATCH_HUD.md](Project/MATCH_HUD.md) owns the UI Toolkit panel, the colour-coded current-round card tray, the two hero overview rails, the `DIRECTOR` drawer, and the one contextual region that now also asks p.15's upgrade and p.8's blocked-Spawn Place. One query decides p.16's +1 for all six icons and composes it into every total the build computes, Radius included: a primary Skill action can print p.9's icon, and one named query derives its inclusive disk. An unlocked Ultimate's printed ability reaches its own hero exactly while that hero is on the board, on the continuous-source seam a placed token's properties use rather than as a cancelable Active effect. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md).

## Current authorization and next route

- Current completed milestone: **7.8a1**.
- Active authorized milestone: **none**.
- Next proposed milestone: **7.8b**.

Milestone **7.8a1** completed on 2026-08-20 and gave an Attack the decided availability a Skill has had since milestone 6.5af, asked of the same target query the start uses so the two cannot disagree. The start still refuses, atomically. A Clear is no longer gated on it, because p.13 gives a Clear its own legality.

Milestone **7.8a** completed on 2026-08-20 and gave the configured match a player-facing UI, on one runtime UI Toolkit panel by the user's recorded decision. The migration stopped at the `I*View` seam, so every presenter, view model and typed invocation is the one that was there before. [Project/MATCH_HUD.md](Project/MATCH_HUD.md) owns what a player now sees and why. It also draws the pause no view drew: rulebook p.15's Upgrade a Card and p.8's blocked-Spawn Place describe themselves as typed facts through `IDescribingSystemTransitionStepRules`, so a player reads which card is returned, which is taken, which is set aside and what item it becomes, and answers with the invocation the continuation itself projected. Evidence for both lives in their completion commits, addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

Milestones 7.1 through 7.8 are complete and addressed by SHA from the same archive: card colour, Tier, upgrade candidates and hero Level; the colour-restricted discard reachable from a card; p.16's items, dashboards and bonus composition; p.9's printed Radius with its total, disk and target/presence boundary, bound to one configured card's Place; p.16's Ultimate with the continuous ability it supplies; p.13's whole Defeat list, with its coins and its Life counters; and p.8's whole end of round, from p.15's mandatory Level Up over the deck p.6 leaves it to search to the Pity Coin every hero that did not level collects; and the three victory sentences under one derived outcome naming winner, loser and cause.

The remaining route is **7.8b onward**; each item requires exact approval. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone inside it.

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
| Configured passive views, presenters, and board cues | [Project/CONFIGURED_PRESENTATION.md](Project/CONFIGURED_PRESENTATION.md) |
| Player-facing HUD: panel, status strip, hero rails, Director drawer, card tray, contextual region | [Project/MATCH_HUD.md](Project/MATCH_HUD.md) |
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
