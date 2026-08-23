# Guards Digital - Session Brief

Last updated: 2026-08-23

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **8.19**; executable behavior remains unchanged from 8.18e because 8.19 was a documentation/architecture re-baseline. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; accepted commands, restore, Undo, and restart now publish authority, events, history, version/checkpoints, and their one immutable projection atomically. The current schema v24 persists every continuation the slice can pause on, together with every hero's progression, every team's counters, and the board's own components. It plays rulebook p.7's turn inside p.8's round: cards commit and reveal under Initiative, an Attack runs p.13's whole consequence list, the round closes through p.8's six ordered steps, and the match ends by one derived outcome. Active-effect and continuous sources can now declare a typed Action-Ended effect, which executes through the ordinary frame and continuation in p.17's Tie-Breaker-side order. It is dealt from verified content: both mirrored seats hold Wasp's printed cards, an uncomposed printed primary is offered greyed and refused, and p.15's mandatory leveling walks that deck's own upgrade pairs to p.16's items and the Ultimate unlock. A player sees all of it on one runtime UI Toolkit panel, with blocked Lane-Push respawn destinations lit and answered on the board rather than duplicated as HUD buttons. The exact inventory of implemented behavior, configured setup, compatibility boundaries and gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards the routing table below names; every statement this summary compresses is stated once by the shard that owns it.

## Current authorization and next route

- Current completed milestone: **8.19**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.19a**.

Milestone **8.19** completed on 2026-08-23, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). The [roster capability matrix](Rules/ROSTER_CAPABILITY_MATRIX.md) maps every newcomer face/component to typed bindings, policies, atomicity, checkpoints and source-linked scenarios. It separates reuse, refinements, primitives and One Man Army's isolated Minion-Battle handler without hero-name branches or text parsing. Shared work is split into schema-v25 continuations (8.19a), derived operations (8.19b), and persistent/system sources (8.19c), before hero slices 8.20-8.24. The later gates remain separate; the path to 8.27 is estimated at 37-65 focused engineering days.

Milestones **8.18a-8.18d**, same day, source-verified Arien, Brogan, Sabina and Tigerclaw into their own shards. Tigerclaw alone owns a roster marker, one Poison marker defined at two strengths; Brogan's Green trio corroborates colour versus supertype and his Ultimate's "lose the push" runs p.8's whole push. Sabina alone conflicts on her epithet - dashboard `COMMANDER`, rulebook `Gunslinger` - so S-01 blocks only her display name and her edition is answered by convention. The user answered A-01/A-02/A-03, B-02 and ledger 6/7/10/14: faces define hero components and a counts-as-minion hero states his consequence. [RULES_LEDGER.md](RULES_LEDGER.md) and the shards own the readings; W-02 and ledger 2 stay open. No 8.18 slice claims a capability; 8.19 does.

Milestones **8.17**, **8.16c** and **8.16a** completed on 2026-08-23, same archive. The approved roster is the rulebook's own recommended first-game line-up - Red: Wasp, Xargatha, Tigerclaw; Blue: Arien, Brogan, Sabina - team slots printed by the source, now ledger question 6. A blocked-Spawn-point Lane Push is now answered only on the board, the HUD drawing no `PLACE AT ...` buttons. The fresh review found **no confirmed defect and nothing gate-blocking**; corrective **8.16b** awaits scheduling, three findings owed before hero implementation.

Milestones **8.13** through **8.16** completed on 2026-08-23, same archive. High Voltage derives an on-board continuous trigger after Wasp's basic Skill; p.15's mandatory leveling then runs over her printed deck until six items fill the four slots and the seventh level unlocks the Ultimate, whose printed icon an item's +1 reaches (ledger question 12). Twenty-one Play Mode journeys click the live HUD and board through the camera ray at every p.15 stage. **8.16** drove that verified mirror to all three endings with determinism, replay, restore, Undo, restart, event-log and Tie Breaker evidence, fixing two mid-turn Lane-Push ending/restore defects. **T3: Edit Mode 1599, Play Mode 66, harness 1365; schema v24 and digests unchanged.**

Milestones **8.1** through **8.11e** completed 2026-08-21 through 2026-08-23 and are addressed by SHA from the same archive; [Rules/HERO_WASP.md](Rules/HERO_WASP.md) owns the per-card record. Nineteen verified faces are dealt from a capability matrix with no custom handler: the card-text seam sits on p.14, the declared modifier vocabulary has production consumers or named exclusions, and **8.10** and **8.11** completed all five Attacks and the three printed Blocks, so every configured face resolves its complete text through ordinary typed effects. **8.11a-8.11d** supplied the official-board underlay, Wasp's visuals, and board-addressable choices with fitted typed cues, with a Clear that has no adjacent token greyed and refused (**`DEV-007`**). **8.11e** made acceptance atomic - no command is accepted until its immutable projection is built, a projection fault rolling authority, events, history and checkpoints back as one `EngineFault`.

**Phase 7 is complete and addressed by SHA from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md)**, which owns the record: 7.1 through 7.8e built progression, items, p.13's Defeat list, p.8's end of round, the victory outcome and the runtime UI Toolkit HUD; 7.9 closed the T3 gate, 7.9b retired `RISK-PERF-001`, and 7.9c/7.9d closed its 7.9a review's findings.

The remaining **Phase 8** route continues with 8.19a's resumable card-text/pre-action continuation and schema-v25 foundation, then 8.19b-8.19c and complete hero slices 8.20-8.24 before the retained 3v3/URP/online gates; 8.16b's corrections await scheduling. Each implementation requires exact approval. Phase 6 is complete at its 6.13 T3 gate, with 6.1y discharged.

**User-approved amendments, 2026-08-23:** milestone **8.15a** is inserted from the user's direction on 8.15's result, and 8.14's `SetAside`-scoping check is withdrawn as unreachable and mis-scoped, a conformance rule fencing the single writer instead; both roadmap blocks carry the reasoning.

**User-approved amendment, 2026-08-22:** Wasp question **W-05** is answered from source and generalised into [ledger question 13](RULES_LEDGER.md#open-source-and-rules-questions) - "discards a card, or is defeated" is one mandatory step with two ways to complete - unblocking 8.10; the cross-hero rule left the hero shard; the same day inserted **8.8b**.

The mechanics-showcase amendment is finished; [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md) owns which card carries which modes and its binding constraints.

Standing route exceptions: none remain. Phase 6's exact sequencing lives in its three [Roadmap/](Roadmap/) shards and its per-milestone evidence is addressed by SHA from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md); scheduling is not authorization.

## Durable boundaries

- The private target remains an official-setup 3v3 pitch match with the approved roster - Wasp, Xargatha and Tigerclaw against Arien, Brogan and Sabina - whose six inventories are now complete under the shared 8.18 contract, retaining director mode and adding invite-only six-human internet validation before production presentation. Accounts, public matchmaking, public distribution, and production live-service scope remain excluded.
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
