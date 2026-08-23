# Guards Digital - Session Brief

Last updated: 2026-08-23

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **8.14**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; accepted commands, restore, Undo, and restart now publish authority, events, history, version/checkpoints, and their one immutable projection atomically. The current schema v24 persists every continuation the slice can pause on, together with every hero's progression, every team's counters, and the board's own components. It plays rulebook p.7's turn inside p.8's round: cards commit and reveal under Initiative, a turn spends one action or Holds, an Attack runs p.13's whole consequence list, the round closes through p.8's six ordered steps, and the match ends by one derived outcome naming winner, loser and cause. Active-effect and continuous sources can now declare a typed Action-Ended effect, which executes through the ordinary frame and continuation in p.17's Tie-Breaker-side order. It is dealt from verified content: both mirrored seats hold Wasp's printed cards, a printed primary whose text has no composition yet is offered greyed and refused rather than performed without it, and p.15's mandatory leveling walks that printed deck's own upgrade pairs to p.16's items and the Ultimate unlock. A player sees all of it on one runtime UI Toolkit panel. The exact inventory of implemented behavior, configured setup, compatibility boundaries and gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards the routing table below names; every statement this summary compresses is stated once by the shard that owns it.

## Current authorization and next route

- Current completed milestone: **8.14**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.15**.

Milestone **8.14** completed on 2026-08-23 and is addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). p.15's mandatory leveling now runs over Wasp's printed eighteen-card deck: every verified upgrade pair takes one card and files the other under the icon that card prints, six items reach all four dashboard slots, and the seventh level unlocks High Voltage beside the dashboard, where 8.13's ability fires in an ordinary match with nothing staged. An item's +1 reaches the Ultimate's own printed icon, per ledger question 12. p.16's item step now takes only the cards the leveling that just ran actually set aside, closing **F-PROG-08**. **Schema stays v24 and its digests are unchanged.**

Milestone **8.13** completed on 2026-08-23 and is addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). High Voltage now derives an on-board continuous trigger after Wasp performs a basic Skill, offers its optional enemy minion in printed Radius with Heavy immunity, stages the ordinary minion Defeat rewards/removal/Lane Push, and reads the enemy hero's adjacency before removal for the `if able` discard. Its questions use the existing effect continuation across restore. **Schema stays v24; trusted registration/content digests were deliberately re-pinned.**

Milestone **8.11e** completed on 2026-08-23 and is addressed by SHA from the same archive. A command is not accepted until its immutable projection has been built; a projection fault is an `EngineFault` that rolls authority, both event streams, accepted history, version/checkpoints and the exact previous public projection back together, and the same boundary protects valid restore, retained-prefix Undo and restart adoption. **Schema v24 and its digests are unchanged.**

Milestones **8.11a-8.11d** completed on 2026-08-22 and are addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). They supplied the official-board underlay, official private Wasp tokens, full Defense-card and rail visuals, board-addressable identity choices, and fitted typed target cues; the private artwork remains ignored.

Milestone **8.11** completed the three printed Block cards and their defender-anchored riders; milestone **8.10** completed all five Attacks. Both are addressed by SHA from the same archive. All configured Attack, Defense, and Skill faces now resolve their complete text through ordinary typed effects and shared staged consequences.



Milestones **8.1** through **8.9**, **8.28b**, **8.8a** and **8.8b** completed on 2026-08-21 and 2026-08-22 and are addressed by SHA from the same archive; [Rules/HERO_WASP.md](Rules/HERO_WASP.md) owns the per-card record. Nineteen verified faces are dealt from a capability matrix with no custom handler; the card-text seam sits on p.14 with its two tray surfaces; the first verified official card text runs on p.19's straight line; the declared modifier vocabulary has production consumers or named exclusions; each rail marks the card in play; the two kinetic Skills run and answered ledger question **W-04**; and the two basic cards run, each one capability node creating an Active effect whose disk is p.9's **printed icon**. p.15's Upgrade a Card presents its six choices as the tray's own card faces reading Blue, Green, Red; and a Clear with no adjacent token is drawn greyed and refused - **`DEV-007`** on the availability rule 6.5af began, because p.13 permits one that removes nothing.

**Phase 7 is complete and addressed by SHA from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md)**, which owns the record: 7.1 through 7.8e built progression, items, p.13's Defeat list, p.8's end of round, the victory outcome and the runtime UI Toolkit HUD; 7.9 closed the T3 hard gate, 7.9b retired `RISK-PERF-001`, and 7.9c and 7.9d closed the defects and gaps its 7.9a review found. **All three general seams from that review are now closed: 8.5, 8.7, and 8.12.**

The remaining route is **Phase 8**: 8.15, completing and verifying every Wasp card behavior, is next proposed; the mirror gate is 8.16. Each otherwise proposed item requires exact approval. Phase 6 is complete at its 6.13 T3 gate, with 6.1y discharged.

**User-approved amendment, 2026-08-22:** Wasp question **W-05** is answered from source, generalised into [ledger question 13](RULES_LEDGER.md#open-source-and-rules-questions), and 8.10 unblocked. "Discards a card, or is defeated" is one mandatory step with two ways to complete, not a choice for either player; 8.2's recorded reasoning was withdrawn as non-discriminating, and the construction proved not to be Wasp's, which is why the rule left the hero shard. The same day inserted **8.8b**, now complete above.

The mechanics-showcase amendment is finished and 7.9 gated its ten-card matrix; [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md) owns which card carries which modes and the two constraints that bind it.

Standing route exceptions: none remain. Phase 6's exact sequencing lives in its three [Roadmap/](Roadmap/) shards and its per-milestone evidence is addressed by SHA from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md); scheduling is not authorization.

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
