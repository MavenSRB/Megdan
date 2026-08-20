# Guards Digital - Session Brief

Last updated: 2026-08-20

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **7.9b**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v21 persists every continuation the slice can pause on, together with every hero's progression, every team's counters, and the board's own components. It plays rulebook p.7's turn inside p.8's round: cards commit and reveal under Initiative, a turn spends one action or Holds, an Attack runs p.13's whole consequence list inside the Attack, the round closes through p.8's six ordered steps, and the match ends by one derived outcome naming winner, loser and cause. A player sees all of it on one runtime UI Toolkit panel. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards the routing table below names; every statement this summary compresses is stated once by the shard that owns it.

## Current authorization and next route

- Current completed milestone: **7.9b**.
- Active authorized milestone: **7.9a**.
- Next proposed milestone: **7.9a**.

Milestone **7.9** completed on 2026-08-20 and closed Phase 7's T3 hard gate. The configured ten-card hands now play complete matches under fixed reported seeds, reaching all three of the rulebook's victory paths by playing rather than by staging, and a bounded eight-seed soak plays the ordinary configured match without stalling. It found two defects nothing else could - a Defense preview that threw on a placed token's own contribution, and a built player whose text shaper crashed because a runtime-created panel carries no ICU data - and it measured `RISK-PERF-001` and `F-ER2-08` for the first time, which is what schedules 7.9b.

Milestone **7.8e** completed on 2026-08-20 and moved authority out of the three documents that had been living at their warning line, instead of compressing them a fourth time. Four new indexed shards - [Project/COMBAT_AND_CONSEQUENCES.md](Project/COMBAT_AND_CONSEQUENCES.md), [Project/HERO_PROGRESSION.md](Project/HERO_PROGRESSION.md), [Rules/BOARD_OBJECT_SYSTEMS.md](Rules/BOARD_OBJECT_SYSTEMS.md) and [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md) - take precise active authority verbatim, and superseded baseline narrative moved to the testing archive. This brief's own implementation paragraph is a summary again rather than a second inventory. The validator reports zero warnings.

Milestone **7.8d** completed on 2026-08-20 and renamed the coin ledger for what it now is. `DefeatRewardAccounting` is `HeroCoinLedger`, its restore-state types and every call site followed, and the Defeat vocabulary stayed where a Defeat is still the subject. Schema stays v21 with every pinned digest asserted unchanged rather than re-pinned, which is what makes it a rename.

Milestone **7.8a1** completed on 2026-08-20 and gave an Attack the decided availability a Skill has had since milestone 6.5af, asked of the same target query the start uses so the two cannot disagree. The start still refuses, atomically. A Clear is no longer gated on it, because p.13 gives a Clear its own legality.

Milestone **7.8a** completed on 2026-08-20 and gave the configured match a player-facing UI on one runtime UI Toolkit panel, by the user's recorded decision, stopping at the `I*View` seam so every presenter and typed invocation is the one that was there before. It also draws the pause no view drew, the pausing rules describing themselves through `IDescribingSystemTransitionStepRules`. [Project/MATCH_HUD.md](Project/MATCH_HUD.md) owns both. Evidence for all three lives in their completion commits, addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

Milestones 7.1 through 7.8 are complete and addressed by SHA from the same archive: card colour, Tier, upgrade candidates and hero Level; the colour-restricted discard reachable from a card; p.16's items, dashboards and bonus composition; p.9's printed Radius with its total, disk and target/presence boundary, bound to one configured card's Place; p.16's Ultimate with the continuous ability it supplies; p.13's whole Defeat list, with its coins and its Life counters; and p.8's whole end of round, from p.15's mandatory Level Up over the deck p.6 leaves it to search to the Pity Coin every hero that did not level collects; and the three victory sentences under one derived outcome naming winner, loser and cause.

Milestone **7.9b** completed on 2026-08-20 and closed both crossings 7.9 measured, by removing the mechanisms rather than widening the lines. An accepted command no longer copies or re-encodes any growing history - the projection shares the append-only logs' snapshots, and the pre-image capture shares each completed round's serialized fragment from the round it froze - and an Undo reconstructs from a retained point kept every 32 commands, byte-identical to a full-prefix replay. Growth fell from +105,584 B to +21,068 B of bounded state residual, Undo from 429 ms to 30.9 ms on the tier that had crossed, both lines were tightened against the pre-fix revision, and `RISK-PERF-001` is retired with its recorded bound.

The remaining route is **7.9a onward**; each item requires exact approval. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone inside it.

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
| Implemented minions, timing, coin ledger, Attack/Defense, Defeat and the endings | [Project/COMBAT_AND_CONSEQUENCES.md](Project/COMBAT_AND_CONSEQUENCES.md) |
| Implemented colours, Tiers, decks, Level, items, Level Up and the Ultimate | [Project/HERO_PROGRESSION.md](Project/HERO_PROGRESSION.md) |
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
| Rule status for targeting, obstacles, Push, Place, Spawn, Swap/Replace, Remove, tokens and markers | [Rules/BOARD_OBJECT_SYSTEMS.md](Rules/BOARD_OBJECT_SYSTEMS.md) |
| Which implemented rule asks the intrinsic unit-type question and which the effective one | [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md) |
| Hero-scoped inventory, capability mapping, and verification status | `Rules/HERO_<NAME>.md`, starting with [Rules/HERO_WASP.md](Rules/HERO_WASP.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
