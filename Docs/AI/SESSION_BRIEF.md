# Guards Digital - Session Brief

Last updated: 2026-08-15

## Purpose and authority

Read this compact startup route once per new task before planning or changing the workspace. `AGENTS.md` controls process. [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) assigns each subject one canonical home; this brief alone owns current authorization and the exact next milestone route, while carrying only a compact implementation summary.

## Current implementation

The project has completed roadmap milestones through **6.8g**. The configured vertical slice uses a pure match application service, typed action handlers, authoritative projections, deterministic replay/Undo, map-bound snapshots, and passive Unity presentation; schema v9 persists current continuations, a paused effect frame, and placed token and attached marker components. Tokens expose catalog-owned persistent properties as continuous sources, survive independently of their source hero, clear before a validated Spawn uses their covered Spawn point, and can be removed by the general Clear replacement for an Attack. Active effects and token properties now declare typed value modifiers, restrictions, permissions, immunity, and `Counts As`, which the value, offer, targeting, presence, effective-type, and obstacle queries consult explicitly; every Active-effect creator mints per-activation instance identity through one shared contract, and marker attachments mint theirs through its sibling, so a replayed card creates a fresh effect and a fresh attachment while re-execution inside one resolution stays a no-op. Terrain is a passable obstacle distinct from off-board: a Move-Through mover crosses the official map's 82 declared terrain cells at ordinary cost while no unit may end a Movement there and nothing may be placed there. Every rules-consuming seam now takes the live modification set as a required parameter - `RuleModificationSet.Empty` is the named no-modification value - and a reflection conformance rule keeps optional defaults out. Every implemented rule naming a unit type declares its intrinsic-or-effective question in the unit-type question ledger, and the p.13 Heavy-immunity accompaniment is the effective type's first production consumer: a hero counting as a minion in the starting Battle Zone protects its Heavy from being targeted. It includes reload-safe Hero/minion combat and the end-of-round Minion Battle with serializable exact-count, one-at-a-time Heavy-last removal choices, now resolved by a configured player-facing prompt with board cues that forwards only the projected typed invocations; Lane-Push requests are recorded but not yet consumed. The exact subject-oriented inventory of implemented behavior, configured setup, compatibility boundaries, and remaining gaps lives in [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) and is maintained by subject rather than milestone chronology.

## Current authorization and next route

- Current completed milestone: **6.8h**.
- Active authorized milestone: **none**.
- Next proposed milestone: **6.8i**.

Milestone **6.8h** completed on 2026-08-15. Marker attachments now mint their instance identity through `MarkerInstanceIdentity.Mint`, a marker-shaped sibling of the 6.8d effect contract that composes source card, action, node, round, and turn with the holder; the two are separate contracts because their last term differs, and the marker anchors are deliberately unclamped since a give reaches the composition only inside a live round. The failing observation settled what the repeated composition actually did, which was neither of the two outcomes the amendment guessed: the register drops a returned attachment, so no uniqueness guard fired and the later give silently reused the earlier attachment's identity. The handler also gained the 6.8d already-created pre-check, ahead of the supply test because the supply is short of that activation's own copy, so re-execution inside one resolution is a legal no-op instead of an impossible step. Migrate-or-reject decision: accept without migration and no schema bump - the ID is an opaque wire string nothing parses - with `attached-marker-v9` the one of seven pinned payloads re-pinned, its delta read at the byte level, and the aggregate-invariance digests observed unchanged. **6.8i** is proposed next under the corrective amendment below. Evidence lives in the completion commit, addressed from the [Phase 6 archive](Archive/Phase-Handoffs/PHASE_6.md).

The user-approved mechanics-showcase amendment retains the next proposal and continues the dependency-safe route **6.4 -> 6.4a -> 6.4b -> 6.5 -> 6.8b -> 6.5a -> 6.5b -> 6.6 -> 6.6a -> 6.7 -> 6.7a -> 6.9 onward**. Six existing configured cards are converted only after their reusable dependencies: Red Bulwark, converted at 6.8a, sources a `+1 Defense This Turn`, self Fast-Travel restriction `This Round`, or self Move-Through permission `Next Turn`; Blue Feint separately sources self Push immunity under `Next Turn after Playing Cards` or self `Counts As` minion for `This Round`; Red Needle covers Push/Place; Red Bombard covers direct Move/Spawn; Blue Sentinel covers Swap/Replace/token-only Remove; and Blue Longbow covers bounded Repeat. In every delayed mode the hero whose Skill is resolving is the affected rules object and the card is only the stable source. Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain exactly one printed non-Ranged and one Ranged Attack per team. Every conversion preserves its card's Initiative/Secondary Movement/Defense and stable identity under a new content revision, 6.13 closes delayed-effect retrieval/cancellation, and 7.9 gates the complete ten-card matrix. Scheduling is not authorization; each new milestone still requires exact approval.

**Reordering amendment (2026-08-15, user approved).** Milestone **6.8b** moves behind Object Push into the slot before 6.5a, and corrective milestone **6.8g** is added ahead of it, making the route **6.1aq -> 6.8d -> 6.8e -> 6.8f -> 6.8g -> 6.4 -> 6.4a -> 6.4b -> 6.5 -> 6.8b -> 6.5a onward**. Neither of Blue Feint's modes had a consumer where it was scheduled: its Push immunity names an effect 6.4 has not implemented and the only immunity consumer names Attack alone, while its `Counts As` mode reaches no production caller at all. Printing either onto a card a player can hold would repeat 6.8c's defect. **6.8g** gives the effective type its first rules consumer and makes every rule naming a unit type declare the intrinsic-or-effective question it asks. 6.5a keeps its role as the configured-Push prover. Scheduling is not authorization; each still requires exact approval.

**Corrective amendment (2026-08-15, user approved).** Three milestones are scheduled from the out-of-scope findings the 6.8d-6.8g block surfaced, making the route **6.8h -> 6.8i -> 6.8j -> 6.4 onward**; 6.8h is complete. **6.8i** retires the four remaining optional rules parameters 6.8f flagged - the Tie Breaker defaults on set derivation and construction, `IsImmuneTo`'s omitted actor, which silently disables p.18's never-immune-to-your-own-actions exactly where 6.4's Push immunity consumer will read it, and presence's defaulted self-exclusion - and must precede 6.4. **6.8j** fixes the skill-backend fault 6.8g's fixture found: a Skill whose root completes at its own start step reads identity from a null frame and faults, so choice-free Skills are unwritable until it lands. Scheduling is not authorization; each still requires exact approval.

Standing route exceptions: **6.1y** remains unordered but required before 8.1. Exact sequencing lives in [Roadmap/PHASE_6_DOCUMENTATION.md](Roadmap/PHASE_6_DOCUMENTATION.md), [Roadmap/PHASE_6_CORRECTIONS.md](Roadmap/PHASE_6_CORRECTIONS.md), and [Roadmap/PHASE_6.md](Roadmap/PHASE_6.md); scheduling is not authorization.

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
| Product scope and durable constraints | [Project/PRODUCT.md](Project/PRODUCT.md) |
| Core architecture constraints | [Project/ARCHITECTURE.md](Project/ARCHITECTURE.md) |
| Presentation and accessibility direction | [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Workflow, references, terminology | [Project/WORKFLOW_AND_TERMS.md](Project/WORKFLOW_AND_TERMS.md) |
| Rule authority/status/deviations/open questions | [RULES_LEDGER.md](RULES_LEDGER.md), plus exact local rulebook pages when implicated |
| Official map data contract and reviewed inventory | [Maps/README.md](Maps/README.md) |
| Card architecture and effect capability coverage maps | [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md) |
| Per-system rule status, primary sources, and verification notes | [Rules/RULE_SYSTEMS.md](Rules/RULE_SYSTEMS.md) |
| Card core model, resolution/effects, validation/extension | [Card-System/CORE_MODEL.md](Card-System/CORE_MODEL.md), [Card-System/RESOLUTION_AND_EFFECTS.md](Card-System/RESOLUTION_AND_EFFECTS.md), [Card-System/VALIDATION_AND_EXTENSION.md](Card-System/VALIDATION_AND_EXTENSION.md) |
| Effects/queries/modifiers/tokens/timing/transitions | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) matching capability section |
| Validation policy, tiers, evidence rules | [Testing/POLICY.md](Testing/POLICY.md) |
| Exact test recipes | [Testing/RECIPES_FOUNDATION.md](Testing/RECIPES_FOUNDATION.md), [Testing/RECIPES_TURNS.md](Testing/RECIPES_TURNS.md), [Testing/RECIPES_COMBAT_AND_MOVEMENT.md](Testing/RECIPES_COMBAT_AND_MOVEMENT.md), [Testing/PLAYMODE_AND_SUITES.md](Testing/PLAYMODE_AND_SUITES.md) |
| Future gates | [Testing/FUTURE_GATES.md](Testing/FUTURE_GATES.md) |
| Historical outcome/evidence | [Archive/README.md](Archive/README.md); do not load history for routine startup |

Expand beyond targeted leaves only for a genuinely cross-cutting task, a named dependency, or a conflict.
