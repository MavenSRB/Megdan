# Phase 7 - Progression, victory, and the complete synthetic core loop

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_7 handoffs](../Archive/Phase-Handoffs/PHASE_7.md).

## Phase 7 - Progression, victory, and the complete synthetic core loop

### Phase-wide durability rule

Every Phase 7 milestone, including milestones 7.1-7.7, that adds authoritative state or changes the durable meaning, identity, lifecycle, or continuation of existing state must close that persistence boundary in the same milestone; a later integration gate is not substitute evidence. Its focused acceptance evidence must reach the new state and every new pending continuation, then prove:

- deterministic snapshot round-trip plus restore-and-continue through the same public command path, with equivalent authoritative state, ordered events, accepted history, and reconstructed projections;
- fresh deterministic replay and retained-prefix Undo/restart reconstruction, including truncation before and after the new state transition, without duplicated, lost, or projection-only state;
- explicit classification of every added aggregate field under the existing rollback contract, plus refused/faulted-command coverage proving rollback preserves the new state and both event streams atomically;
- projection reconstruction from restored and replayed authority rather than serialized or presentation-owned projection state; and
- explicit compatibility handling in that milestone: every incompatible snapshot schema, stable identity, or content-revision change receives a typed migrate-or-reject decision and corresponding compatibility-corpus update, while a compatible change proves the maintained corpus still restores and round-trips with unchanged meaning.

### [ ] 7.1 Add synthetic card colors, tiers, and upgrade relationships

Observable outcome: Synthetic cards expose authoritative typed `CardColor` and Tier data for rules queries, while synthetic non-Basic cards model Tier I-III same-color upgrade choices without mutating definitions or using Wasp data.

Acceptance checks:

- Basic cards have no Tier and are excluded from lowest-Tier and upgrade-candidate queries.
- Every definition explicitly represents its applicable typed `CardColor` or verified absence; card color is distinct from Red/Blue team identity and any runtime or UI slot, and is never inferred from art, tint, display name, hero identity, or UI grouping.
- Pure context-aware queries derive deterministic card candidates by color together with owner/controller, location, lifecycle state, and rules-facing information availability for later choices, costs, conditions, counts, and effects.
- Color-filtered card queries compose with typed pending choices and lifecycle mutations such as `DiscardCard`; the mutation cannot guess color eligibility, ownership, or chooser policy.
- Every non-Basic definition has an explicit Tier and stable same-color next-Tier candidates where applicable; color constrains progression, while valid upgrade relationships remain explicit rather than being inferred from color alone.
- Hand, deck, and set-aside instances retain ownership and definition revision through upgrade transitions.
- Each synthetic hero has authoritative current-Level state initialized to Level 1.
- Typed queries can identify a hero's current lowest non-Basic card Tier for later Life and leveling rules.
- Focused tests cover same-color upgrade candidates, color-filtered legal-card candidates, team/color separation, state and ownership filtering, information suppression, and deterministic empty-candidate behavior.

Non-goals: No coin spending, level purchase, item bonus, Ultimate, Wasp definition, verified hero-specific color effect, or production card-color presentation.

### [ ] 7.2 Add generic item state and printed-icon bonuses

Observable outcome: A synthetic set-aside card can become an item in the matching dashboard slot and contribute through typed printed-value queries.

Acceptance checks:

- Item ownership, source card, slot, and order are authoritative data; multiple items may occupy the same slot.
- Each item adds `+1` only to the matching printed Initiative, Attack, Defense, Movement, Range, or Radius icon.
- Items never modify numbers in card text, and every contribution retains item provenance.

Non-goals: No leveling orchestrator, Wasp item values, or final item presentation.

### [ ] 7.3 Add Ultimate continuous-source behavior

Observable outcome: A synthetic Ultimate can be unlocked as persistent state and supplies its rule only while its hero is on the board.

Acceptance checks:

- The Ultimate remains distinct from ordinary cancelable Active effects and ordinary hand/deck cards.
- Defeat/off-board state suppresses its applicability without deleting the unlocked source; legal respawn restores applicability.
- Unlock eligibility can require completion of the Tier III upgrade state without hard-coding a hero name.

Non-goals: No Wasp Ultimate text or final visual treatment.

### [ ] 7.4 Resolve hero defeat, Lives, and respawn

Observable outcome: A successful synthetic Hero Attack resolves the complete general defeat consequences and later legal respawn.

Acceptance checks:

- Defeat, rewards, removal/prevention, unresolved-card resolution, effect cancellation, marker return, Life spending, and victory checks occur at verified checkpoints.
- The attacker gains coins equal to the defeated hero's current Level; eligible friendly heroes gain assist coins from the defeated hero's lowest card Tier.
- Life spending reads the defeated hero's current lowest non-Basic card Tier through the milestone 7.1 query.
- Defeating a token's source hero does not remove that token; markers held by the defeated hero return as required.
- Optional before-action respawn and resolving without action after declining are supported.
- Spawn-point and nearest-empty fallback use the full Phase 4 map.
- The Defense exception remains distinct.

ER2 note (`F-ER2-09`): when this milestone supplies verified reward derivation, relocate the hard-coded Melee/Ranged/Heavy coin literals and the standard-minion Defense modifier table out of the combat backend and defense query into configured content carrying their source citations; see the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2).

### [ ] 7.5 Resolve mandatory end-of-round leveling and upgrades

Observable outcome: Every eligible synthetic hero buys as many consecutive levels as it can afford and completes the required upgrade, item, or Ultimate outcome for each level.

Acceptance checks:

- Level purchases are mandatory, spend the configured cost in order, and cannot skip a level.
- Each ordinary purchase upgrades one chosen lowest-Tier non-Basic card; tied choices and the two same-color next-Tier candidates use authoritative pending choices.
- The chosen upgrade enters the hand, the replaced card returns to the deck, and the unchosen candidate becomes the matching item using milestone 7.2.
- After all non-Basic cards reach Tier III, the next eligible level unlocks the milestone 7.3 Ultimate and no further levels are available.
- Multi-level and multi-hero ordering uses milestone 6.11's typed, serializable intra-stage system-transition continuation shape, with stable hero/level/choice identity and exact resume location; restore, replay, Undo, rejection, and stale input cannot repeat purchases, costs, upgrades, items, or Ultimate unlocks.
- Leveling adds no progression-specific callback, presentation-owned continuation, or Wasp-specific branch.

Non-goals: No Wasp costs, card identities, item art, or Ultimate text.

### [ ] 7.6 Resolve Pity Coin ordering

Observable outcome: After all leveling finishes, each hero that did not level this round receives exactly one Pity Coin.

Acceptance checks:

- Eligibility is tracked authoritatively for the current round and is independent per hero.
- A hero that bought one or more levels receives no Pity Coin.
- A Pity Coin that raises a hero to a level threshold cannot be spent until a later round.

### [ ] 7.7 Integrate and verify all three victory paths

Observable outcome: Final Wave, Throne capture, and loss of the final Life all end the match through one authoritative game-ending contract.

Acceptance checks:

- Final-Wave and Throne outcomes reuse milestone 6.10; final-Life loss reuses milestone 7.4.
- Each path records the winning team and exact reason, stops pending resolution, and prevents later rule steps.
- Focused scenarios prove each path independently and reject duplicate or conflicting winners.

### [ ] 7.8 Integrate the exact end-of-round sequence

Observable outcome: The milestone 3.8c end-of-round state machine has every required handler populated, executes the completed systems in source order, and arrives at a valid next round or game-ended state without any synthetic unavailable-stage declarations.

Acceptance checks:

- The sequence runs Minion Battle; token removal/marker return; requested Lane Push; card retrieval/cancellation; mandatory leveling/upgrades; and Pity Coin collection at their exact checkpoints.
- Applicable End of Round effects and immediate victory checks occur at rules boundaries rather than presentation timing.
- A game-ending result stops later processing immediately.
- Focused scenarios cover no Lane Push, one Lane Push, level/no-level players, retrieved effects, Pity Coin eligibility, and a game-ending Push.
- The same state and choices produce the same state and ordered events.
- A focused two-round regression proves that completing a round removes that round's events from the current-round projection while retaining them in the whole-match event log and completed-round history; events accepted in the new round then append to a fresh current-round sequence. Restore, replay, and retained-prefix Undo reproduce the same boundary.
- Integration preserves the established resumable cursor, completed-round history, round-local reset, snapshot, Undo, and event-log contracts rather than introducing a second round-transition path.

### [ ] 7.9 Complete a deterministic test-card core match

Observable outcome: The configured ten-card mechanics-showcase hands can start a match, play legal Movement/Hold/Attack/Defense/Skill turns, exercise every source-reviewed reusable core operation and delayed lifetime, complete rounds, resolve minions and Lane Pushes, progress, and end through every victory path.

Acceptance checks:

- Every general core rule used by the loop links to its authoritative source and focused tests.
- The exact configured primary-action matrix is Red Needle Push/Place Skill, Red Bulwark timing/rules-source Skill, Red Lancer Ranged Attack, Red Pursuit non-Ranged Attack, Red Bombard Direct-Move/Spawn Skill, Blue Feint non-Ranged Attack, Blue Sentinel Swap/Replace/Remove Skill, Blue Scout Shot Ranged Attack, Blue Hammer non-Ranged Attack, and Blue Longbow bounded-Repeat Skill. All ten retain their stable definition/instance identities, approved Initiative values, printed Secondary Movement, Defense reactions, ownership, and five-card-per-hero shape under explicit current content revisions.
- General Hold remains derived from a selected unresolved card, Fast Travel remains a replacement only for an eligible Movement offer, and Clear remains a replacement only for an eligible Attack; none becomes invented printed text on a showcase Skill. At least one non-Ranged and one Ranged printed Attack remains configured for each team.
- Initiative, Tie Breaker flips, turns, rounds, immediate/end-of-round Lane Pushes, retrieval, leveling, respawn, and victory work together.
- Synthetic effect scenarios exercise direct Move, Object Push, Place, Spawn, board-object Swap/Replace, standalone Remove, Defeat, and Clear through the same command, timing, pending-choice, snapshot, replay, Undo/restart, and event-log boundaries used by the match; Object Push and Lane Push remain observably distinct.
- Synthetic Skill, ordered/optional/modal/Repeat control flow, modifiers, restrictions, permissions, immunity, and effective-characteristic scenarios prove that no source-reviewed general keyword or reusable core capability is deferred for first implementation to a verified hero milestone.
- Red Bulwark's configured delayed sources exercise `This Turn`, `This Round`, `Next Turn`, and `Next Turn after Playing Cards` through creation, dormant/active applicability, triggering, expiration, source-card cancellation/retrieval, defeat/off-board interaction where declared, simultaneous ordering, snapshot/restore, replay, and retained-prefix Undo. The gate proves these are distinct authoritative lifetimes rather than presenter timers or one generic delayed flag.
- Synthetic Phase 7 colors, tiers, upgrade relationships, items, and Ultimate sources layer onto the same showcase definitions/instances without replacing their primary-action identities or inferring progression from display names, art, team, or hand slots.
- Snapshots, Undo, restart, and the event log remain deterministic across the complete loop.
- The deterministic replay oracle covers representative complete matches and bounded multi-round generated/soak scenarios with fixed reported seeds; live, replayed, restored-and-continued, and Undo-truncated paths produce matching command/event/projection/snapshot/history evidence.
- The complete-match runtime capture samples accepted-command projection refresh time and managed allocation at fixed reported command-history lengths, including an empty/short history and the complete-match history. It records the samples and growth against history length, and compares the complete-match result with the standing 25 ms refresh-time and 64 KiB managed-allocation investigation thresholds in `Testing/FUTURE_GATES.md`.
- The maintained snapshot compatibility corpus covers the complete synthetic-loop state, and every schema/content incompatibility remains an explicit typed migration or rejection decision.
- A disposable Win64 development build of the exact gate revision launches outside the Editor, starts and completes a deterministic synthetic-match smoke path, writes usable diagnostics, and exits cleanly. The artifact is validation-only and is neither retained as the pitch build nor distributed publicly.
- Synthetic definitions are visibly test-only and contain no claim about Wasp values or effects.
- The extra Focus/Cache fixture-only cards either retain a documented distinct isolation role for a lower-level effect/component contract or are retired with their dependent tests migrated to the configured showcase; they do not enlarge either normal hand beyond five or remain as unexplained duplicate coverage.
- A broader deterministic core-loop suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

ER2 note (`F-ER2-08`): `Undo`/`Restart` reconstruct by full-prefix replay, so their cost grows with match length; this gate's runtime baseline must measure Undo at complete-match length and record a bound. `RISK-PERF-001` is also open because projection construction copies growing command/current-round histories. Neither mechanism schedules optimization by inspection: milestone 7.9 records the pitch-scale measurements, and a separately approved corrective milestone is proposed only for a measured threshold crossing.

### [!] 7.9a Independently review the complete synthetic core

Blocked by: Milestone 7.9 must pass its deterministic synthetic-match T3 gate.

Observable outcome: A fresh analysis-only review determines whether the complete synthetic rules loop is correct, maintainable, deterministic, performant enough for the prototype, and architecturally ready for verified hero content.

Acceptance checks:

- The review follows complete-match paths across setup, turns, pending choices, combat, reusable direct Move/Object Push/Place/Spawn/Swap/Replace/Remove/Clear operations, minions, tokens/markers, Lane Push, progression, victory, snapshots, deterministic replay, Undo/restart, and the event log using actual source and gate evidence.
- It checks for state duplication, hidden coupling, temporary Phase 1-7 structures that outlived their purpose, rules or presentation authority leakage, persistence/replay divergence, complete-match allocation or resource-growth risks, and test suites that could provide false confidence.
- It explicitly assesses whether the generic card/effect contracts remain suitable for real verified content without claiming that unknown cards require no extension.
- It identifies any source-reviewed general keyword or reusable capability still awaiting first implementation; such a gap blocks verified hero content rather than being reassigned to Phase 8.
- Findings and preserved strengths follow the common independent-review evidence contract; fixes become separately approved corrective milestones and are not implemented here.
- The existing hard-gate roadmap audit of Phase 8 entry is completed as a distinct section of the same review record, including remaining-effort range and shortest dependency-safe path.

Non-goals: No Wasp inventory, source interpretation, card implementation, runtime fix, architecture rewrite, production optimization, or weakening of the synthetic-core gate.

Hard gate: The complete Wasp inventory and Wasp-specific implementation do not begin until milestone 7.9a and every gate-blocking corrective milestone are reviewed and approved.

Hard-gate roadmap audit: As part of milestone 7.9a, audit readiness for verified Wasp content before authorizing the next milestone. Confirm that the synthetic core loop has retired the intended architecture and integration risks, identify unresolved prerequisites that must remain ahead of Wasp implementation, recommend any justified changes to Phase 8's inventory/mapping entry sequence, and record the remaining-effort range. This audit cannot waive milestones 8.1 or 8.2 or authorize Wasp-specific implementation.
