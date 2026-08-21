# Guards Digital - Session Brief

Last updated: 2026-08-21

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **8.5**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v21 persists every continuation the slice can pause on, together with every hero's progression, every team's counters, and the board's own components. It plays rulebook p.7's turn inside p.8's round: cards commit and reveal under Initiative, a turn spends one action or Holds, an Attack runs p.13's whole consequence list inside the Attack, the round closes through p.8's six ordered steps, and the match ends by one derived outcome naming winner, loser and cause. It is dealt from verified content: both mirrored seats hold Wasp's printed cards, and a printed primary whose text has no composition yet is offered greyed and refused rather than performed without it. A player sees all of it on one runtime UI Toolkit panel. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and the shards the routing table below names; every statement this summary compresses is stated once by the shard that owns it.

## Current authorization and next route

- Current completed milestone: **8.5**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.6**.

Milestone **8.5** completed on 2026-08-21, opening the general card-text seam and closing three 7.9a findings on their named checks. A printed Attack or Defense may now declare an `EffectRoot`, and because that root is card text rather than the action itself it names which of rulebook p.14's numbered moments applies it - the attacker's at step 1, the defender's at step 4, the attacker's rider at step 7 after the target has defended or been removed (F-CARD-01). Every printed Attack carries a `PrintedAttackTargetPolicy`, so p.13's reach and its heroes-only narrowing are content rather than an inference from the subtype label. `EffectCapabilityContext` carries the enclosing action's kind, so p.18's immunity stopped being asked as `RuleActivity.Skill` from inside an Attack (F-BOARD-03), and a `Conditional` publishes its predicate's typed facts and routes a non-Applied result instead of reading it as false (F-CARD-07). **Card text inside an Attack cannot pause**: the validator refuses a composition that could ask a question, at content load, and the slice that first transcribes such a rider owns the nesting and the schema move it costs. Proven on new synthetic cards; no Wasp card composed anything, and schema stays v21 with every pinned digest byte-identical.

Milestone **8.4** completed on 2026-08-21: the configured session and the saved scene deal `WaspCardContent` - both mirrored seats holding p.5's starting five over p.6's remaining thirteen, one printed set with thirty-six per-seat instances, the Handicap card instantiated nowhere. Dealing verified cards before their text exists is made honest by one rule: a printed primary whose `PrintedTextComposition` is still owed is drawn greyed and refuses a start atomically, because performing it would carry out the printed values and silently drop the printed sentence, while p.12's Hold and p.13's replacements keep the card spendable. It is `DEV-006`. `PlaceholderMovementHand` retired into `ConfiguredHand` plus `Synthetic/SyntheticShowcaseHand`. **The predicted schema move did not exist** - a definition is an id and a revision on the wire - so schema stayed v21 and the corpus gained `verified-hand-setup-v21`.

Milestone **8.3** completed on 2026-08-21: Wasp's nineteen printed faces are `VerifiedOfficial` definitions in `WaspCardContent` at revision `wasp-8-3-rev-1` - the project's first verified content - with p.9's supertype, p.15's upgrade pairs, p.16's item icons, and each face's words carried verbatim as an inert `CardSourceContent` no rule may read. **No definition carries an `EffectRoot`**: each printed primary names the 8.6-8.13 slice that owes its composition. It closed three 7.9a findings on their named checks, each observed failing first - F-CARD-08, F-CARD-06 and F-CARD-05 - and kept schema v21.

Milestone **8.28b** completed on 2026-08-21, closing `F-ER2-04`: normal setup's coin flip derives from a recorded seed, so setup can be performed again rather than only remembered, and `SetupRandomnessAuthority.DrawSeed` is the engine's one unseeded draw, held by an IL conformance rule.

Milestones **8.1** and **8.2** opened Phase 8 on 2026-08-21 and are addressed by SHA from the [Phase 8 archive](Archive/Phase-Handoffs/PHASE_8.md): Wasp's nineteen faces are source-verified in [Rules/HERO_WASP.md](Rules/HERO_WASP.md), which also carries the capability matrix mapping every verified action onto the implemented contracts - no custom handler anywhere in her set - and the two unreadable faces stay recorded as questions. 8.2's designated audit re-baselined 8.5-8.14 with user approval, and answered ledger question 12: an item's +1 reaches an Ultimate's printed icon. Remaining Wasp-block effort: 5-14 focused days.

**Phase 7 is complete and addressed by SHA from the [Phase 7 archive](Archive/Phase-Handoffs/PHASE_7.md)**, which owns its full record: 7.1 through 7.8e built card colour, Tier and upgrade candidates, hero Level and items, p.9's printed Radius, p.13's whole Defeat list, p.8's whole end of round, the three victory sentences under one derived outcome, and the runtime UI Toolkit HUD; 7.9 closed the T3 hard gate; 7.9b retired `RISK-PERF-001`; and 7.9a's independent review, whose four defects and five guard gaps 7.9c and 7.9d closed, found the core deterministic, performant and not yet certifiably rules-correct. **The first of the three general seams it identified closed at 8.5; the other two are 8.7 and 8.12**, and its remaining findings are dispositioned in that record.

The remaining route is **Phase 8**; each item requires exact approval. The Phase 8 hard gate is open, and the 8.2 re-baseline sequenced the route from Wasp's verified text: 8.4 loaded the verified hands into the director turn loop and 8.5 opened the general card-text seam, so the Wasp slices can begin: 8.6 is unblocked and proposed next, while 8.7 and 8.12 land ahead of the slices that consume them, and the mirror gate is 8.16. Phase 6 is complete: its implementation closed at 6.13's T3 gate, and 6.1y discharged the last milestone inside it.

The user-approved mechanics-showcase amendment is finished and milestone 7.9 gated its ten-card matrix; which card carries which modes lives in [Roadmap/PHASE_6_SHOWCASE.md](Roadmap/PHASE_6_SHOWCASE.md). Two constraints still bind: the four printed Attacks hold one non-Ranged and one Ranged per team, now the floor rather than a margin; and a resolving Skill's hero is the affected rules object, the card only the stable source.

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
