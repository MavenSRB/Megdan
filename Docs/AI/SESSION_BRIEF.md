# Guards Digital - Session Brief

Last updated: 2026-08-21

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **8.1**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v21 persists every continuation the slice can pause on, together with every hero's progression, every team's counters, and the board's own components. It plays rulebook p.7's turn inside p.8's round: cards commit and reveal under Initiative, a turn spends one action or Holds, an Attack runs p.13's whole consequence list inside the Attack, the round closes through p.8's six ordered steps, and the match ends by one derived outcome naming winner, loser and cause. A player sees all of it on one runtime UI Toolkit panel. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards the routing table below names; every statement this summary compresses is stated once by the shard that owns it.

## Current authorization and next route

- Current completed milestone: **8.1**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.2**.

Milestone **8.1** completed on 2026-08-21 and opened Phase 8: Wasp's complete printed content is source-verified from the extracted card faces and recorded in [Rules/HERO_WASP.md](Rules/HERO_WASP.md), which owns it - nineteen faces, being the eighteen-card deck plus the Handicap card, with every colour, Tier, value, icon, action type and verbatim text checked twice against the images. Three of the four Wasp cards the rulebook illustrates reproduce the faces exactly and the fourth differs only in reminder text, which answers the ledger's edition question for Wasp alone. Two faces are unreadable and are recorded rather than guessed: the dashboard back that would name any Wasp tokens, and the profile card's stat bars.

Milestone **7.9** completed on 2026-08-20 and closed Phase 7's T3 hard gate: the configured ten-card hands play complete matches under fixed reported seeds, reaching all three victory paths by playing rather than by staging, with a bounded eight-seed soak beside them. It found two defects nothing else could - a Defense preview that threw on a placed token's contribution, and a built player whose text shaper crashed for want of ICU data - and measured `RISK-PERF-001` and `F-ER2-08` for the first time, which is what scheduled 7.9b.

Milestones **7.8a**, **7.8a1**, **7.8d** and **7.8e** completed on 2026-08-20: the player-facing runtime UI Toolkit match HUD that [Project/MATCH_HUD.md](Project/MATCH_HUD.md) owns, the decided Attack availability asked of the same target query the start uses, the `HeroCoinLedger` rename with every pinned digest asserted unchanged, and the four indexed shards that took precise authority out of three documents at their warning line - [Project/COMBAT_AND_CONSEQUENCES.md](Project/COMBAT_AND_CONSEQUENCES.md), [Project/HERO_PROGRESSION.md](Project/HERO_PROGRESSION.md), [Rules/BOARD_OBJECT_SYSTEMS.md](Rules/BOARD_OBJECT_SYSTEMS.md) and [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md).

Milestones 7.1 through 7.8 are complete and addressed by SHA from the same archive: card colour, Tier, upgrade candidates and hero Level; the colour-restricted discard reachable from a card; p.16's items, dashboards, bonus composition and the Ultimate with the continuous ability it supplies; p.9's printed Radius with its total, disk and target/presence boundary; p.13's whole Defeat list with its coins and its Life counters; p.8's whole end of round, from p.15's mandatory Level Up to the Pity Coin every hero that did not level collects; and the three victory sentences under one derived outcome naming winner, loser and cause.

Milestone **7.9b** completed on 2026-08-20 and closed both crossings 7.9 measured by removing the mechanisms rather than widening the lines: an accepted command copies and re-encodes no growing history, and an Undo reconstructs from a retained point kept every 32 commands, byte-identical to a full-prefix replay. Growth fell to +21,068 B and Undo to 30.9 ms on the tier that had crossed, both lines were tightened against the pre-fix revision, and `RISK-PERF-001` is retired with its recorded bound.

Milestone **7.9a** completed on 2026-08-20: the fresh-task analysis-only independent review of the complete synthetic core. Its verdict - deterministic, performant for the prototype, architecturally strong, and not yet certifiably rules-correct - confirmed four defects, proposed the two correctives that followed, and identified three general seams still awaiting first implementation, all addressed from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md).

Milestones **7.9c** and **7.9d** completed on 2026-08-20, each check observed failing before its fix, and closed the corrective sequence on the full sweep the user's decision placed at 7.9d: Edit Mode 1333/1333 and Play Mode 37/37 unfiltered, harness 1167/1167, schema still v21. 7.9c closed the four rules-surface defects 7.9a confirmed - the dropped mid-turn Lane Push and its missing counts-as guard, the two production system transitions a raw director command could begin, and the END TURN control drawn beside a mandatory choice. 7.9d closed five guard gaps, led by the cross-runtime digest pin that now runs on the harness and proved its nine digests byte-identical between Mono and .NET, and by the effect-definition validator gaining its first production caller (F62B-05 closed).

The remaining route is **Phase 8**; each item requires exact approval. The Phase 8 hard gate is open: milestone 7.9a's review is complete and both of its corrective milestones are closed on recorded check transitions. The three general card-text seams the review identified (F-CARD-01, F-CARD-02, F-CARD-03) remain owed as general core milestones ahead of the hero milestones that would consume them, sequenced at milestone 8.2's designated audit, which now has milestone 8.1's verified Wasp text to work from. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone inside it.

The user-approved mechanics-showcase amendment is finished and milestone 7.9 gated its ten-card matrix; which card carries which modes, and why, lives in [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md). Two constraints still bind: Red Lancer, Red Pursuit, Blue Scout Shot and Blue Hammer keep the printed Attacks holding one non-Ranged and one Ranged per team, now the floor rather than a margin; and a resolving Skill's hero is the affected rules object, the card only the stable source.

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
