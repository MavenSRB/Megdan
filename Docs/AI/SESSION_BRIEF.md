# Guards Digital - Session Brief

Last updated: 2026-09-03

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The route has completed approved local HUD, rules, lifecycle, six-seat, and private-online work through **8.35z20**. **The default match is rulebook p.5's six-player Quick Game**: six heroes, three Waves, five Lives a side, the Tie Breaker, twelve minions, 108 cards - with pp.5-6's 2v2 and both lengths selectable since **8.35z16**. Its director and six authorized local seat clients prove all victory causes plus replay, restore, retained-prefix Undo, restart, snapshots, and event logs. Unity 6000.5.9f1 with URP 17.5 is the checked-in baseline; the board/HUD and Win64 smoke path are intact. All six heroes pass the permanent player-path gate, all 114 roster fronts are source-verified, and schema v36 persists every pause and pins the content contract set. `RISK-SCALE-001` is retired; `RISK-ONLINE-001` stays active through the invited-internet gate. Exact behavior and gaps live in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards below.

## Current authorization and next route

- Current completed milestone: **8.35z20**.
- Active authorized milestone: **8.36**, readiness recorded and the invited playtest outstanding.
- Next proposed milestone: **8.36**.

Milestone **8.36** was authorized 2026-09-03; readiness is recorded: the two-network protocol and host runbook in [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md), the tester text in `Tools/PlaytestTesterInstructions.txt`, and the gate battery. Its package is owed a fresh cut: `Playtest-8.35z14-wave.zip` speaks `/8` against the current `/9`. What remains is human: six testers across two networks completing one match, the bundle, triage, and completion.

Correctives **8.35d** through **8.35z14** completed from the user's live multiplayer findings: the host runs p.8's end-of-round sequence, pausing only for an owned decision; p.15's Upgrade a Card is answerable only by the seat whose hero levelled; every seat renders the Minion Battle's agreement stage; each seat's own UNDO is bounded to its turn's resolution, walled above a chosen Attack target; and a finished turn waits for its player's END TURN, one **8.35v** press cancelling and releasing together. **8.35h** isolates its presenter fan-out; **8.35j** and **8.35m** light the top bar and every awaited tray and card; **8.35k** returns a relaunched player to their reserved seat; **8.35n** narrates the acting hero; **8.35p** moves a charge before its attack resolves. **8.35z11**-**8.35z14** are the wave-one integration: one conductor paces every motion, an attack opens a spotlight, practice bots attack, and a fizzle names what blocked it - advancing the wire to `/8`. **8.35z15** stops a card-text repeat with no legal subject opening p.10's confirm, on all four cards printing one. **8.35z16**-**8.35z19** are the wave-two integration: pp.5-6's printed 2v2 joins the 3v3 at both game lengths, launch-flag chosen and carried as its own admission element - advancing the wire to `guards-private-online/9`; a Director-only sandbox stages an arbitrary board and plays it under the ordinary rules, Undo and restart intact, refused beside every networked role by the parser; and a runtime tooltip layer makes all 29 hover strings visible in a Player build at last.

Milestones **8.35a1**, **8.35c** and **8.35b** completed 2026-09-02 to 09-03: hosting from the lobby refuses an unkeyed bind off loopback; one seatless host and six client processes of one disposable Win64 build played a complete loopback match through the primitive lobby, with both agreement flows, a mid-match reconnect and a terminal host-loss cohort, converging on one outcome; and that lobby hands an admitted seat to the owner-scoped HUD. A same-machine rehearsal proves no relay, NAT, latency, or real-internet behavior.

Milestones **8.31** through **8.35a**, with correctives **8.31a** and **8.31b**, built the stack beneath that under the 2026-09-01 user-directed transport amendment: one dedicated seatless host over direct TCP admitting six Windows clients on the full compatibility tuple and a session-key proof, every socket confined to the deletable Unity-free `GuardsDigital.Network` adapter; the pre-shared-key AEAD frame layer paying 8.29's owed encryption decision; owner-scoped projections converging under deterministic fault injection with a bounded resync and measured idempotency window; reserved-seat reconnect under a rotated epoch with terminal host loss; and the primitive lobby with a host-derived one-way start. Current behaviour lives in [Project/PRIVATE_ONLINE_ARCHITECTURE.md](Project/PRIVATE_ONLINE_ARCHITECTURE.md) and exact evidence in the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md).

Completed late-HUD work through **8.30n2** keeps the map-first choices and milestone **8.30n**'s public unanimous teammate agreement for tied Initiative and Minion Battle confirmation while Director mode stays direct. Exact behavior, down to each banner, symbol and status slot, lives in [Project/MATCH_HUD.md](Project/MATCH_HUD.md).

Corrective **8.29c** was deferred in 2026-08, its Director Undo symptom unreproducible, pending a recurrence; **8.35f**'s rewind sweep supplied one, and it was a shared-authority defect rather than the HUD path 8.29c assumed: a retained checkpoint of a choice paused inside an Attack no longer restored, refusing both the Director's Undo and a seat's rewind while mutating nothing. **8.35i** fixed that at its cause; 8.29c stays deferred, its symptom unreproduced. The completed correctives around it - **8.29b1/8.29b1b**, **8.29d**, **8.29e** and **8.29f** - are defined in [Roadmap/PHASE_8_HANDS_ON_CORRECTIONS.md](Roadmap/PHASE_8_HANDS_ON_CORRECTIONS.md).

Milestone **8.29** selected one serialized host authority; its 2026-09-01 user-directed amendment replaced the player-hosted UGS/Relay stack with the dedicated seatless host over direct TCP, invite-only by pre-shared session key. Accounts, matchmaking, telemetry, public distribution and package installation remain excluded, and `RISK-ONLINE-001` stays active through the invited-internet gate.

Earlier Phase 8 milestones established the verified six-hero roster, typed effect/query and pending-decision contracts, official 3v3 setup, the deterministic complete-match gate, source-ratio card faces, production-sized HUD geometry, the URP baseline, and governance routing. The [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md) owns exact completion evidence and revisions. The current complete Unity gates are 2,576/2,576 Edit Mode and 279/279 Play Mode. **Phase 7 is complete** and its exact record is addressed by SHA from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md); the finished mechanics-showcase amendment is owned by [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md).

Roadmap amendments through 2026-09-03: completed late-HUD work through **8.30n2** and the amended private-online implementation through **8.35c** are summarized above. On 2026-09-03 the user also approved corrective **8.35a1**, from milestone 8.36's readiness review, and correctives **8.35d**-**8.35g** from live multiplayer testing. **8.36**'s exact checks live in [the Phase 8 shard](Roadmap/PHASE_8.md), and the completed late-HUD checks live in [the late-HUD roadmap shard](Roadmap/PHASE_8_LATE_HUD_CORRECTIONS.md).

Standing route exceptions: none remain. Phase 6's sequencing lives in its three [Roadmap/](Roadmap/) shards and its evidence is addressed by SHA from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md); scheduling is not authorization.

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
| Selected private-online provider, protocol, trust/failure model, and track re-baseline | [Project/PRIVATE_ONLINE_ARCHITECTURE.md](Project/PRIVATE_ONLINE_ARCHITECTURE.md) |
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
| Private-online focused, fault, reconnect, and multi-client recipes | [Testing/PRIVATE_ONLINE.md](Testing/PRIVATE_ONLINE.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
