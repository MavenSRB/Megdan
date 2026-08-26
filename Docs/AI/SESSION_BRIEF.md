# Guards Digital - Session Brief

Last updated: 2026-08-26

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **8.25b**. **The match a player loads is rulebook p.5's official six-player Quick Game setup**: three Red heroes and three Blue on the six declared spawn points, three Waves, five Lives a side, the normal Tie Breaker flip, setup step 10's twelve minions and 108 card instances of 108 definitions, no two seats sharing a printed set. Each of the six passes the permanent hero player-path completion gate, whose content-derived matrix binds every printed interaction shape to a real-input Play Mode journey and every verified face to one shared card footprint. The status line names whatever the match is asking, and each hero's full-height side rail carries their coins, p.16's items and held p.18 markers. The roster's shared query/value/state vocabulary and its persistent and pausing rule sources compose through the existing effect frame over authoritative state, and every card-text, pre-action and interrupted continuation remains resumable. An action an ability may repeat is held open across that decision, so p.10's second instance runs on the same card under the same hero and every offered decision can be answered. The pure application service, typed handlers, deterministic replay/Undo, map-bound snapshots and passive Unity presentation publish accepted state, events, history, checkpoints and one immutable projection atomically. The current schema v34 persists every pause, typed round record and bound-actor reading, and pins the registered contract set a restore is read back against. The official match and the named staged boards alike play p.7 turns, p.8 rounds, p.13 consequences, p.15 progression and all derived endings through one UI Toolkit panel. The exact behavior, setup, boundaries and gaps live in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the routed shards below.

## Current authorization and next route

- Current completed milestone: **8.25b**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.26**.

Milestone **8.25b** completed on 2026-08-26, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). The configuration types are named for what they compose - `MatchConfiguration`, `StagedBoardParticipants`, `MatchConfigurationRestoreState` - and nothing else moved: no behaviour, content, digest or schema.

Milestone **8.25a** completed on 2026-08-26, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). A pending after-action repeat can no longer outlive its actor's Initiative: the turn holds the action's card and its Initiative acknowledgement across the decision an ability that may repeat it opens, and the answer starts the second instance there and then. Observed failing at `49ddb9ac` on both boards and passing here, over a seeded sweep. Running that arm uncovered two shipped defects, both about a repeated Attack's invocation context, and left one residual on the archive row; a refused card-action command now reports its backend's reason. Schema stays **v34** and no digest moves; new baselines Edit Mode **1917**, Play Mode **227**.

Milestone **8.25** completed on 2026-08-26, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). The staged one-hero-per-team default is replaced by p.5's six-player configuration, with the hero-to-seat and hero-to-spawn pairings stated rather than drawn: this build's seats are indistinguishable, so a draw changes nothing a rule can ask. Schema stayed **v34** with every compatibility digest byte-identical. `RISK-SCALE-001`'s payload half is measured and closed at **24,733 B decoded**, crossing nothing. Rail arrangement was the user's decision taken here: full-height columns with the tray between them. Its review proposed **8.25b** (the configuration types now owe a rename) alongside 8.25a; the user directed **8.25c** the same day, which scales the HUD with the viewport rather than at fixed device pixels and replaces each rail header's circled initial with that hero's portrait.

Milestones **8.23-8.24** dealt the fifth and sixth heroes and drew rulebook p.18's held markers, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md). Tigerclaw the Cutpurse at `tigerclaw-8-23-rev-1` brought four shared foundations, p.10's Repeat among them, and closing him found seven shipped defects; 8.23a drew a held marker on its holder's rail. Xargatha the Changed at `xargatha-8-24-rev-1` closes the roster, so **all six pitch heroes pass the permanent player-path gate**. Schema moved **v32**, **v33** then **v34**.

Milestones **8.19-8.22** built the shared roster foundations, dealt Arien the Tidemaster, Brogan and Sabina, closed the permanent gate on them and on Wasp, and gave every face one fixed `CardFootprint`, addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md); 8.19 mapped the newcomers in the [roster capability matrix](Rules/ROSTER_CAPABILITY_MATRIX.md) and 8.19a-8.19c1 made the four continuations resumable. Schema moved **v26** through **v31**.

Milestones **8.16a-8.18e** source-verified the roster into hero shards. [RULES_LEDGER.md](RULES_LEDGER.md) owns the answered readings and the remaining W-02; S-01 is answered, the epithet `Sabina the Gunslinger`.

Milestones **8.1-8.16** dealt all nineteen verified Wasp faces through ordinary typed capabilities, completed her Attacks/Blocks, official-board presentation, board-addressable choices, Ultimate and p.15 progression, made command acceptance atomic, and closed the deterministic verified-mirror gate. [Rules/HERO_WASP.md](Rules/HERO_WASP.md) owns the card record.

**Phase 7 is complete and addressed by SHA from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md)**, which owns the record: 7.1-7.8e built progression, items, p.13's Defeat list, p.8's end of round, the victory outcome and the runtime UI Toolkit HUD; 7.9 closed the T3 gate, 7.9b retired `RISK-PERF-001`, and 7.9c/7.9d closed the 7.9a review's findings.

The remaining **Phase 8** route continues with six-hero director control at 8.26 and the retained 3v3/URP/online gates; 8.16b and the proposed 8.25c-8.25f await scheduling. Each implementation requires exact approval. Phase 6 is complete at its 6.13 T3 gate, with 6.1y discharged.

The mechanics-showcase amendment is finished; [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md) owns which card carries which modes.

Roadmap amendments, 2026-08-26, at the user's direction after auditing 8.25a: **8.25d** - a card-text Attack repeat still reports itself as printed, so Xargatha's "on a different enemy hero" narrowing has never fired - and **8.25e** - a repeat can be offered, taken, and do nothing, on all four arms of one switch: Hold, Fast Travel and Clear are never attempted, and an attempted one that cannot start is dropped in silence.

Roadmap amendment, 2026-08-26, at the user's direction: **8.25f** replaces every complete generated card face with its verified private front when locally prepared, retains the generated clean-checkout fallback and identity-free facedown commitment, and keeps private pixels ignored and outside rules authority. It awaits scheduling and does not displace 8.26 as the next proposed milestone.

Roadmap amendment, 2026-08-25: heroes beyond this roster are taken **in batches of about five rather than one at a time**, and [Roadmap/POST_PITCH.md](Roadmap/POST_PITCH.md) owns why and what a batch owes first. It changed nothing inside this roster.

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
