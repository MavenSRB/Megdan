# Phase 6 - Mechanics-showcase card integrations

Canonical active roadmap subshard. It is indexed from [PHASE_6.md](PHASE_6.md); return there for the general capability milestones and the rest of the Phase 6 route. Historical completion evidence will be indexed from the [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md) after each milestone completes.

## Approved scheduling and durable hand shape

**Mechanics-showcase scheduling amendment (2026-08-14, user approved).** Six of the ten configured synthetic cards become a staged mechanics-showcase hand immediately after their reusable dependencies are proven: 6.8a converts Red Bulwark after 6.8; 6.5a and 6.5b convert Red Needle and Red Bombard after 6.4-6.5 and the 6.4a/6.4b correction path; 6.6a converts Blue Sentinel after 6.6; and 6.7a converts Blue Longbow after 6.7.

**Reordering amendment (2026-08-15, user approved).** Milestone 6.8b moves out of its original slot after 6.8a and into the slot immediately before 6.5a, making showcase execution follow **6.3 -> 6.8 -> 6.8a -> 6.4 -> 6.4a -> 6.4b -> 6.5 -> 6.8b -> 6.5a -> 6.5b -> 6.6 -> 6.6a -> 6.7 -> 6.7a -> 6.9 onward**. Neither of Blue Feint's two modes had a consumer in its original slot: Object Push does not exist until 6.4 and the sole implemented immunity consumer names only Attack, while the effective-type query is reached from no production caller at all. The card would therefore have printed two modes a player can select and observe nothing from, which is exactly the defect 6.8c was written to correct. The immunity mode's consumer arrives with 6.4 and the effective-type consumer with the new corrective milestone 6.8g, so both are live before the card declares them; 6.5a keeps its role as the configured-Push prover named in 6.8b's checks. Scheduling is not authorization.

Each conversion replaces only that card's printed Primary Attack, retaining its stable definition/instance identities, Initiative, printed Secondary Movement, and Defense reaction while advancing its content revision. Red Lancer, Red Pursuit, Blue Scout Shot, and Blue Hammer retain exactly one non-Ranged and one Ranged printed Attack per team; general Hold, Fast Travel, and Clear remain derived from their eligible source actions rather than being printed onto a showcase Skill. Red Bulwark is the source card for modifier/restriction/permission effects applied to the acting hero over `This Turn`, `This Round`, and `Next Turn`; Blue Feint is the source card for immunity/`Counts As` effects applied to its acting hero over `This Round` and `Next Turn after Playing Cards`, so the four delayed lifetime forms remain distinct without making either card itself a rules object that receives those effects. Milestone 6.13 closes their retrieval/cancellation interactions and 7.9 gates the complete ten-card matrix. The existing Focus/Cache fixture-only cards remain narrow fixtures until 7.9 determines whether each still has a distinct isolation role.

These integration followers do not move first implementation out of 6.4-6.8 or claim future verified cards need no versioned extension. Scheduling is not authorization: every new milestone still requires exact user approval before execution.

## Milestone definitions in execution order

### [x] 6.8a Convert Red Bulwark into the modifier/restriction/permission showcase Skill

Observable outcome: The configured Red hand's existing Red Bulwark card replaces its printed Primary Attack with one three-mode synthetic Skill whose source is the card and whose affected object is always the acting hero: that hero gets `+1 Defense This Turn`, cannot Fast Travel `This Round`, or may Move Through obstacles during `Next Turn`.

Prerequisite: Milestone 6.8 must be complete, and the integration must reuse its registered typed contracts rather than inventing card-local variants.

Acceptance checks:

- Red Bulwark retains its stable card-definition and instance identity, Initiative 11, printed Secondary Movement 2, Defense 3 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- The immutable definition's three modes instantiate exactly one typed Defense modifier, one Fast-Travel restriction, and one Move-Through permission using the exact contracts and precedence cases already proven by 6.8; every mode binds the affected hero to the action's authoritative actor, and no mode combines two rule-source families merely to reduce card count.
- The modes respectively declare `This Turn`, `This Round`, and `Next Turn`; creation, dormant/active phase, trigger visits, applicability, expiration anchor, early cancellation, and source-card policy remain explicit definition data rather than presenter timers or text parsing.
- The current-turn Defense modifier can affect a later enemy Attack against the acting hero, the round Fast-Travel restriction survives only the current-round boundary, and the next-turn Move-Through permission survives intervening round work but applies only during that hero's declared next turn.
- Restriction/permission precedence and affected action/effect scope use the shared 6.8 queries. Immunity, effective `Counts As`, and `Next Turn after Playing Cards` are deliberately absent and belong to Blue Feint at 6.8b.
- Mode/target choices, pending and live sources, snapshot/restore, replay, retained-prefix Undo/restart, rejection rollback, ordered simultaneous handling, projections, and passive presentation remain deterministic across the delayed boundaries.
- Focused definition, lifecycle, modifier/query, Core/application, presentation, and saved-scene scenarios prove every mode and duration without claiming the synthetic values or text are official content.

Non-goals: No immunity, `Counts As`, `Next Turn after Playing Cards`, Object Push implementation before 6.4, Wasp definition, source-unverified official rule claim, runtime printed-text parsing, card-name/hero-name rule branch, production art, animation, or audio. Milestone 6.8b owns the separated immunity/effective-type card, and 6.13 owns final retrieval/cancellation integration for both cards' delayed sources.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68a-convert-red-bulwark-into-the-modifierrestrictionpermission-showcase-skill-completion-evidence).

### [x] 6.8b Convert Blue Feint into the immunity/effective-type timing showcase Skill

Observable outcome: The configured Blue hand's existing Blue Feint card replaces its printed Primary Attack with one two-mode synthetic Skill whose source is the card and whose affected object is always the acting hero: that hero gains Push immunity under `Next Turn after Playing Cards`, or effectively `Counts As` a minion for `This Round`, separating defensive targeting and effective-type timing from Red Bulwark's action/value rules.

Prerequisites: Milestones 6.8, 6.8a, 6.8d, 6.8g, and 6.4-6.4a must be complete; this milestone reuses the exact registered immunity, effective-characteristic, and delayed-lifetime contracts rather than extending them inside a card definition. The 2026-08-15 reordering amendment in this shard's scheduling section moved this milestone behind Object Push and behind the effective-type consumer so that neither mode is printed onto a configured card before a rule reads it.

Acceptance checks:

- Blue Feint retains its stable card-definition and instance identity, Initiative 12, printed Secondary Movement 3, Defense 2 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- The immunity mode names Object Push as the originating effect family, binds the affected hero to the action's authoritative actor, and uses `Next Turn after Playing Cards` as its distinct activation/lifetime policy. Because 6.4 has implemented the effect, this milestone proves the Push target query rejects that acting hero in a configured match while the source remains Blue Feint's resolved card definition; milestone 6.5a then repeats it from Red Needle's configured Push rather than a synthetic one.
- The `Counts As` mode makes the acting hero effectively a minion for `This Round` while preserving intrinsic hero representation, identity, ownership, occupancy kind, supply rules, and intrinsic-miniature exceptions such as displaced-minion correction.
- `Next Turn after Playing Cards` does not activate or expire as ordinary `Next Turn`; skipped, off-board, restored, replayed, and Undo-truncated paths retain the exact declared phase until the matching checkpoint.
- TargetQuery rejects an immune target while PresenceQuery still observes it where applicable, and effective-type queries see the declared minion characteristic without allowing rules that explicitly require a minion miniature.
- Mode choices, live sources, snapshot/restore, replay, retained-prefix Undo/restart, rejection rollback, ordered simultaneous handling, projections, and passive presentation remain deterministic across both delayed boundaries.
- Focused definition, lifecycle, target/presence, effective-type, Core/application, presentation, and saved-scene scenarios prove both modes without claiming the synthetic values or text are official content.

Non-goals: No value modifier, action restriction, narrow permission, change to the Object Push effect 6.4 implements or to the intrinsic/effective choices 6.8g settles, Wasp definition, source-unverified official rule claim, runtime printed-text parsing, card-name/hero-name rule branch, production art, animation, or audio. Milestone 6.13 owns final retrieval/cancellation integration for the delayed sources.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68b-convert-blue-feint-into-the-immunityeffective-type-timing-showcase-skill-completion-evidence).

### [x] 6.5a Convert Red Needle into the Push/Place showcase Skill

Observable outcome: The configured Red hand's existing Red Needle card replaces its printed Primary Attack with the exact synthetic modal Skill `Choose one: choose another adjacent unit and Push it 3 spaces; or Place your hero in a different empty space within Radius 4`, making the completed Push, Place, Radius, occupancy, and displaced-minion-correction contracts visibly testable through the normal match session.

Prerequisites: Milestones 6.4-6.5 and 6.4a/6.4b must be complete, so neither branch exposes an unimplemented operation or an uncorrected displaced-minion endpoint.

Acceptance checks:

- Red Needle retains its stable card-definition and instance identity, Initiative 13, printed Secondary Movement 2, Defense 1 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- The Push branch targets exactly one other adjacent unit, records the acting hero as the pushing unit and fixed requested distance 3, moves the target directly away as far as possible including zero, and applies permission, immunity, occupancy, and legal-endpoint rules through the shared 6.8/6.4 queries.
- The Place branch takes the acting hero from its current board space to a different empty playable space at inclusive Radius 4 from the actor's coordinate when that branch begins; Radius uses rulebook distance without invented line of sight or path traversal, and the operation emits Place rather than Movement, Fast-Travel, or Spawn facts.
- Mode, target, and destination decisions use the existing authoritative effect-choice command/projection with explicit mandatory/impossible behavior; stale or invalid answers reject without card, occupancy, continuation, or event mutation.
- Pushing a minion reaches the one 6.4a after-action correction boundary, including 6.4b's player-facing route or fallback-Place choice, before the Skill resolves; placing the acting hero does not create a displaced-minion correction of its own.
- Focused definition, Core/application, snapshot/restore, replay, retained-prefix Undo/restart, presentation, and saved-scene scenarios exercise both modes, partial/zero Push, occupied/off-board Place refusal, stale choices, correction, action completion, and passive UI forwarding.

Non-goals: No Direct Move, Spawn, Swap, Replace, Remove, Repeat, Wasp definition, official value/text claim, card-name/hero-name rule branch, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65a-convert-red-needle-into-the-pushplace-showcase-skill-completion-evidence).

### [~] 6.5b Convert Red Bombard into the Direct-Move/Spawn showcase Skill

Observable outcome: The configured Red hand's existing Red Bombard card replaces its printed Primary Attack with a bounded synthetic `Choose one` Skill whose first branch directly Moves another adjacent unit up to 2 spaces and whose second branch Spawns one declared synthetic reserve unit at an eligible empty source-defined Spawn point.

Prerequisites: Milestones 6.4-6.5 and 6.4a/6.4b must be complete, so Direct Move, supply-backed Spawn, token-covered Spawn-point clearance, and displaced-minion correction are available before configured exposure.

Acceptance checks:

- Red Bombard retains its stable card-definition and instance identity, Initiative 5, printed Secondary Movement 2, Defense 2 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- Direct Move declares the other adjacent unit target, chooser, `up to 2` distance policy, path/endpoint rules, ordinary restrictions and permissions, and cannot become a Movement action or Fast Travel; a moved minion reaches the shared after-action correction boundary.
- Spawn uses one clearly test-only reserve identity and supply, declares the eligible piece and destination query, accepts only an empty source-defined Spawn point, and performs milestone 6.3's token-covered Spawn-point clearance inside the atomic Spawn transaction.
- Place and Spawn remain observably distinct in facts, source-container validation, triggers, and failure outcomes; the Spawn branch cannot take an on-board object and the Direct-Move branch cannot consume supply.
- Mode, target, destination, and reserve choices are authoritative pending inputs; rejection, no-candidate behavior, snapshot/restore, replay, retained-prefix Undo/restart, and action completion remain deterministic.
- Focused definition, supply/occupancy, Core/application, presentation, and saved-scene scenarios exercise both modes, different legal destinations, token-covered Spawn, depleted/invalid supply, occupied endpoint, stale input, minion correction, and passive UI forwarding.

Non-goals: No hero/minion Respawn specialization, Swap, Replace, Remove, Repeat, Wasp definition, official reserve setup claim, card-name/hero-name rule branch, production art, animation, or audio.

### [ ] 6.6a Convert Blue Sentinel into the Swap/Replace/Remove showcase Skill

Observable outcome: The configured Blue hand's existing Blue Sentinel card replaces its printed Primary Attack with a synthetic `Choose one` Skill that atomically Swaps the acting hero with another adjacent unit, Replaces an adjacent synthetic Hazard token with a declared alternate synthetic token from supply, or Removes an adjacent token to its declared supply destination.

Prerequisite: Milestone 6.6 must be complete; the Swap branch also consumes 6.4a/6.4b correction, and the Replace branch consumes 6.5's supply and occupancy contracts.

Acceptance checks:

- Blue Sentinel retains its stable card-definition and instance identity, Initiative 11, printed Secondary Movement 2, Defense 4 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- Swap validates the actor and one other adjacent unit plus both resulting occupancies against the same pre-effect state, commits one semantic transaction, emits neither Movement nor Place facts, and runs after-action minion correction when the exchanged unit is an intrinsic minion left outside the Battle Zone.
- Replace declares the adjacent Hazard target, one trusted alternate test-token definition and finite incoming supply, return of the outgoing Hazard to its own supply, no property transfer, and one observable Replace operation rather than a hidden Remove-plus-Spawn sequence.
- Remove targets only an adjacent token in this configured showcase, returns it to its declared supply, preserves prevention/outcome identity, and emits standalone Remove rather than Clear or Defeat. Turn-time last-minion Remove remains unexposed until 6.9 can emit its required immediate Lane-Push request.
- Mode and nested object choices use the shared authoritative effect-choice path; atomic rejection, exhausted replacement supply, stale identity, snapshot/restore, replay, retained-prefix Undo/restart, and action completion preserve exact state and ordered facts.
- Focused definition, component catalog/supply, occupancy, Core/application, presentation, and saved-scene scenarios exercise all three modes, correction, prevention, occupied/conflicting state, stale input, and passive UI forwarding.

Non-goals: No minion-targeting configured Remove before 6.9, card-state Swap, card upgrade replacement, Defeat reward, Wasp definition, official token claim, card-name/hero-name rule branch, production art, animation, or audio.

### [ ] 6.7a Convert Blue Longbow into the bounded-Repeat showcase Skill

Observable outcome: The configured Blue hand's existing Blue Longbow card replaces its printed Primary Attack with the synthetic Skill `Choose another adjacent unit and Push it 1 space; you may Repeat this action once`, visibly proving a separate bounded repeated action instance and the correction boundary between instances.

Prerequisite: Milestone 6.7 must be complete, with milestones 6.4a/6.4b available for each Push instance's possible displaced-minion correction.

Acceptance checks:

- Blue Longbow retains its stable card-definition and instance identity, Initiative 4, printed Secondary Movement 3, Defense 2 reaction, ownership, and five-card-hand slot; only its printed Primary Attack is replaced, and its content revision advances with an explicit compatibility-corpus decision.
- The ordinary instance chooses one other adjacent unit and Pushes it with fixed requested distance 1; after its complete action and any displaced-minion correction, the optional Repeat creates one new instance of the same action under explicit repeated context.
- The repeated instance may choose a different currently legal target, revalidates every target and endpoint against current authority, and cannot expose or execute its own Repeat node recursively.
- Decline, no legal repeated target, partial/zero Push, correction choice, interruption, stale input, snapshot/restore between instances, replay, retained-prefix Undo/restart, and final card resolution produce deterministic state and ordered events without duplicate completion.
- Focused definition, Core/application, timing/order, presentation, and saved-scene scenarios exercise ordinary/declined/repeated paths and prove passive UI forwarding of each authoritative choice.

Non-goals: No unbounded or recursive Repeat, multi-action scripting language, new leaf mutation, Wasp definition, card-name/hero-name rule branch, production art, animation, or audio.
