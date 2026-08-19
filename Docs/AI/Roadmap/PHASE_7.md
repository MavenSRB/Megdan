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

### [x] 7.1 Add synthetic card colors, tiers, and upgrade relationships

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

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-71-add-synthetic-card-colors-tiers-and-upgrade-relationships-completion-evidence).

### [x] 7.1a Reach card state from an effect definition

Observable outcome: A synthetic Skill requires another hero to discard one card of their own choosing; the choice is owned by that hero, declines without failing the action when they have none, and survives snapshot, replay, and retained-prefix Undo.

Context: milestone 7.1 built the color-restricted discard out of the three pieces the card-system contract names - the characteristic query, the effect frame's own typed pending choice, and the in-hand discard mutation - and found it unreachable from a card. The effect frame carries card definitions but not card instances once a Skill has started, so the choice-answer, repeated-skill and option-feasibility paths cannot re-derive card candidates, and neither can snapshot restore. No configured card needed it, so 7.1 recorded the gap rather than widening the resolver in the same milestone that moved the snapshot schema. The approved hero's own starting content settles the schedule: its Tier I card of one color, and two of the four cards that color upgrades into, each require an effect to make another hero discard a card of their own choosing under an `if able` policy, with the Tier III variant offering defeat as the alternative to discarding. No card text, value, or identity is reproduced here or anywhere in this repository; the hero's inventory remains milestone 8.1's, and the requirement is recorded in general terms because the extracted corpus is private. Milestone 7.9 requires proof that no source-reviewed general keyword or reusable core capability is deferred for first implementation to a verified hero milestone, so this must precede it. It is scheduled as 7.1a because that is where the gap was found, and it is not a prerequisite for 7.2 through 7.8: their card movement runs through milestone 6.11's system-transition seam rather than through the effect frame. Compatibility is expected to be a registration cause rather than a schema one - registering a capability moves every payload's registration list, as at 6.4 and 6.8k, while `EffectValueKind.CardInstance` already serializes in both directions and candidates are re-derived rather than written - but the phase-wide durability rule governs the outcome either way.

Acceptance checks:

- Card instances reach every point at which a capability runs or is re-asked: Skill start, repeated-Skill start, the option-feasibility walk, the choice-answer path, and snapshot restore's candidate re-derivation. A capability that reads cards answers identically at all five, and no path silently substitutes an empty set for cards it was not given.
- One registered card-state capability derives its candidates through `CardCharacteristicQuery` and commits through the existing in-hand discard mutation. The mutation still decides no color eligibility, ownership, or chooser policy, and no second candidate derivation exists beside the query's.
- The chooser may be a hero other than the actor. The pending choice records whose decision it is, an answer submitted for any other hero is refused atomically with the choice still standing, and the candidates are derived in the chooser's own information context rather than the actor's.
- `if able` is honored at the node: a chooser with no legal card declines without failing the enclosing action, and that outcome is distinguishable from a chooser who declines an optional step they could have taken.
- Candidates are re-derived rather than persisted, and re-read when the answer arrives rather than trusted from the offer: a card that left the chooser's hand between the offer and the answer is refused.
- The pending choice, its chooser, and its accepted value reconstruct equivalently through snapshot round-trip, restore-and-continue through the same public command path, fresh deterministic replay, and retained-prefix Undo truncated both before and after the discard, with every added aggregate field classified under the existing rollback contract and refused-command coverage proving the discard is atomic.
- No orphan composition remains: the milestone 7.1 color-restricted discard either gains this capability as its production consumer or is folded into it.
- Focused tests cover the five reach points, the opposing chooser, the wrong-hero refusal, the empty-candidate decline, the stale answer, and the persistence round trip.

Non-goals: No verified hero definition, card text, or values; the hero inventory remains milestone 8.1's. No card-state mutation beyond the in-hand discard - retrieve, swap, set aside, and upgrade from an effect stay with their owning milestones. No configured showcase card gains new printed text, so the ten-card matrix milestone 7.9 gates is untouched and the capability is proved on a fixture Skill card, as milestones 6.2, 6.4, and 6.7 proved theirs before any configured card exposed them. No per-player information filtering or hidden-hand presentation; the online track owns that, and this milestone requires only that the choice records whose it is. No change to the leveling, item, or system-transition seams.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-71a-reach-card-state-from-an-effect-definition-completion-evidence).

### [x] 7.2 Add generic item state and printed-icon bonuses

Observable outcome: A synthetic set-aside card can become an item in the matching dashboard slot and contribute through typed printed-value queries.

Acceptance checks:

- Item ownership, source card, slot, and order are authoritative data; multiple items may occupy the same slot.
- Each item adds `+1` only to the matching printed Initiative, Attack, Defense, Movement, Range, or Radius icon.
- Items never modify numbers in card text, and every contribution retains item provenance.

Non-goals: No leveling orchestrator, Wasp item values, or final item presentation.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-72-add-generic-item-state-and-printed-icon-bonuses-completion-evidence).


### [ ] 7.2a Compose the item bonus into every implemented total, and give each hero its dashboard item slots

Observable outcome: A hero's items raise every printed icon this build computes a total for, and each hero's dashboard declares the four item slots rulebook p.16 places items under.

Context: milestone 7.2 built p.16's Item Bonus as one query answering for all six icons and composed it into the Defense total alone, on the ground that Defense was the build's only value-composition point. That was the wrong boundary rather than a missing one. Four further channels already have authoritative consumers: Initiative orders the turn, the Attack Total drives the outcome comparison, the Movement Total gates the action and its reachability, and a Ranged Attack's Range Total decides its legal targets. Each reaches its printed value through the same query, and each therefore composes the bonus by consulting the item state the round already owns rather than by any new derivation; the attack and defense backend already holds it, so two of the four are a binding away and the other two extend the same binding. Radius is the single channel with no current total or printed-value consumer. It is scheduled immediately afterward as 7.2b because adding its spatial query and affected-set boundary carries independent implementation and validation risk from this milestone's snapshot-schema and dashboard work. Milestone 7.2 also left the dashboard unmodelled, recording which slot an item occupies without anything that states which slots a hero has. p.16 states that an Item icon "matches the icon on one of four slots on the top of your hero dashboard", which is content this build can state now for its synthetic heroes; deferring it to milestone 8.1 would leave the placement unable to refuse an item its owner has nowhere to put. The user approved both parts on 2026-08-19, and chose that the slots ride the snapshot rather than being rebound from configuration, so that a restore refuses a build whose dashboards differ the way it already refuses one whose card content differs.

That approval also settled a rules question the page's own scan could not, and it retires a decision milestone 7.2 recorded. The four slots are Attack, Defense, Initiative, and a fourth carrying the Range, Movement and Radius icons together; the page confirms the first and third itself, magnifying the Attack slot as "the first item slot" and placing an Initiative item "under the third slot", while the dashboard's label strip carries six icons above four tucked-card positions. **A slot is therefore not an icon.** Milestone 7.2 recorded that "the slot is named by its icon rather than by a position index, because that is what p.16 matches on", which is true of the first three slots and false of the fourth, where three icons share one. The consequence is concrete rather than cosmetic: an item under the fourth slot may be a +1 Range, a +1 Movement or a +1 Radius item, so an item has to record both where it sits and what it gives, and the bonus has to read the icon. Phase 7's phase-wide durability rule governs the outcome.

Acceptance checks:

- Every value total this build computes composes the item bonus through milestone 7.2's single query: Initiative, Attack, Defense, Movement, and a Ranged Attack's Range. No consumer derives the bonus itself, and no second composition exists beside the query's.
- Each hero declares rulebook p.16's four dashboard item slots as authoritative content: Attack, Defense, Initiative, and a fourth accepting the Range, Movement and Radius icons together. A slot is a distinct thing from an icon, declares which icons it accepts, and is never interchangeable with one; nothing may derive a slot from an icon by identity.
- An item records both the slot it occupies and the icon it supplies, and the Item Bonus reads the icon. Two items under the same fourth slot supplying different icons raise different totals, which is the case milestone 7.2's slot-as-icon model could not represent.
- The four slots survive snapshot round-trip, restore-and-continue, replay, and retained-prefix Undo, with the migrate-or-reject decision and compatibility-corpus update their schema change requires.
- A set-aside card whose Item icon no slot on its owner's dashboard accepts is refused, distinguishably from a card that prints no Item icon at all, and neither refusal moves the card or records an item.
- Setup validation refuses content in which a hero can reach a Tier II or Tier III card whose Item icon no slot on that hero's dashboard accepts, so the p.16 correspondence is a checked property of the content rather than an assumption.
- Focused tests cover each composed channel with and without a matching item, a card that prints no icon of a channel gaining nothing there, the four-slot declaration, two differently-iconed items sharing the fourth slot, the unaccepted-icon refusal, and the persistence round trip.

Non-goals: No leveling orchestrator, Wasp dashboards, Wasp item values, or item/dashboard presentation. Printed Radius and its consumer are isolated in milestone 7.2b rather than silently folded into this snapshot-schema milestone.

### [ ] 7.2b Add the missing Radius total and make Radius items observable

Observable outcome: A synthetic card's printed Radius determines its inclusive rules-distance scope, and each matching Radius item expands that scope by one through the same Item Bonus query as every other printed icon.

Context: milestone 7.2 deliberately made `ItemBonusQuery` answer all six p.16 channels, including Radius, but this build has no printed Radius value or Radius Total for that answer to reach. Storing a Radius item without a consumer would make a valid item mechanically inert. Fixed integers already used by synthetic effect text are not a substitute: p.16 modifies a printed Radius icon and never a number in card text. This milestone therefore gives Radius its missing value-consumption boundary after 7.2a closes dashboard validity, and before leveling can create real items. It also closes the query side needed by cards that choose or automatically affect units in Radius, while leaving each verified card's concrete mutation and timing to its source-reviewed content milestone.

Acceptance checks:

- A card definition can declare a typed printed Radius value, and one authoritative Radius Total composes that printed contribution with matching items exclusively through milestone 7.2's `ItemBonusQuery`; every contribution retains its printed or item provenance.
- A card with no printed Radius icon gains no Radius from an item, and a fixed Radius number written in effect text remains unchanged by items.
- A named `RadiusQuery` derives the inclusive disk from the current Radius Total using rulebook hex distance, not movement reachability, path length, occupancy, or invented line of sight.
- Radius-scoped object queries explicitly distinguish targeting from presence. A chosen or automatically affected unit is a target and is filtered by the applicable immunity; a presence check over the same spaces still counts an immune unit.
- The Radius query can return the complete deterministic eligible set needed by an effect that affects every unit in Radius, without making query enumeration order into mutation or simultaneous-resolution order. Each consuming effect remains responsible for its sourced timing and commit policy.
- At least one synthetic printed-Radius consumer uses the shared total end to end. Focused tests prove the boundary case in which a matching item admits a space or eligible object exactly one step beyond the unmodified Radius, while a nonmatching item, absent printed icon, and card-text integer do not.
- Any definition revision, registration, or persisted continuation change receives the phase-wide compatibility, snapshot, replay, Undo, rollback, and projection evidence applicable to what the implementation actually changes.

Non-goals: No Wasp or other verified hero content, no invention of a card-specific effect or ordering rule, no general simultaneous-batch control node, and no production Radius presentation. A later source-reviewed card may consume the complete affected set, but may not reimplement Radius geometry or item composition.

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
- The exact configured primary-action matrix is Red Needle Push/Place Skill, Red Bulwark modifier/restriction/permission Skill, Red Lancer Ranged Attack, Red Pursuit non-Ranged Attack, Red Bombard Direct-Move/Spawn Skill, Blue Feint immunity/effective-type timing plus place-token and give/return marker Skill, Blue Sentinel Swap/Replace/Remove Skill, Blue Scout Shot Ranged Attack, Blue Hammer non-Ranged Attack, and Blue Longbow bounded-Repeat Skill. All ten retain their stable definition/instance identities, approved Initiative values, printed Secondary Movement, Defense reactions, ownership, and five-card-per-hero shape under explicit current content revisions. Blue Feint's last three modes are milestone 6.2's component capabilities, which the configured scene reached through a sixth card between milestones 6.2d and the five-card scene; the gate proves them from the hand rather than from a fixture, and proves that Feint's placement is what supplies the Hazard Blue Sentinel's Replace and Remove take within the same round, which is why the placement is not on Sentinel itself.
- General Hold remains derived from a selected unresolved card, Fast Travel remains a replacement only for an eligible Movement offer, and Clear remains a replacement only for an eligible Attack; none becomes invented printed text on a showcase Skill. At least one non-Ranged and one Ranged printed Attack remains configured for each team.
- Initiative, Tie Breaker flips, turns, rounds, immediate/end-of-round Lane Pushes, retrieval, leveling, respawn, and victory work together.
- Synthetic effect scenarios exercise direct Move, Object Push, Place, Spawn, board-object Swap/Replace, standalone Remove, Defeat, and Clear through the same command, timing, pending-choice, snapshot, replay, Undo/restart, and event-log boundaries used by the match; Object Push and Lane Push remain observably distinct.
- Synthetic Skill, ordered/optional/modal/Repeat control flow, modifiers, restrictions, permissions, immunity, and effective-characteristic scenarios prove that no source-reviewed general keyword or reusable core capability is deferred for first implementation to a verified hero milestone.
- The actor-affecting modifier/restriction/permission sources created by Red Bulwark's Skill exercise `This Turn`, `This Round`, and `Next Turn`, while the actor-affecting immunity/effective-type sources created by Blue Feint's Skill exercise `This Round` and `Next Turn after Playing Cards`; together they prove all four lifetime forms through creation, dormant/active applicability, triggering, expiration, source-card cancellation/retrieval, defeat/off-board interaction where declared, simultaneous ordering, snapshot/restore, replay, and retained-prefix Undo. The gate proves the card remains source identity, the hero remains the affected rules object, and the lifetimes are authoritative rather than presenter timers or one generic delayed flag.
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
