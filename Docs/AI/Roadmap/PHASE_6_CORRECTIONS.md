# Phase 6 - Corrective milestones from proving the showcase cards

Canonical active roadmap subshard. It is indexed from [PHASE_6.md](PHASE_6.md); return there for the general capability milestones and the rest of the Phase 6 route. Historical completion evidence is indexed from the [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md) after each milestone completes.

Milestones here correct mechanisms rather than add capabilities. Each exists because putting a reusable contract onto a card a player can hold exposed something the contract's own milestone had left unfinished or unowned, so their common shape is a general seam repaired once rather than a symptom patched on the card that revealed it.

## Milestone definitions in execution order

### [x] 6.8c Give the Move-Through permission its Movement consumer

**Corrective amendment (2026-08-14, user approved).** Milestone 6.8 implemented the Move-Through permission and `ObstacleQuery`'s p.18 transit/endpoint split, then deferred the Movement consumer to "the milestone that owns route commitment" without naming one. Milestone 6.8a then printed that permission onto Red Bulwark as one of three configured modes. The result is a mode a player can select and observe nothing from: `ObstacleQuery` has no production caller, `MovementReachabilityQuery` accepts no modification set and drops every occupied neighbour before enqueuing it, and `MovementCardActionStrategyBackend` never passes the live set. This milestone closes that gap rather than leaving inert configured content in the hand.

Observable outcome: A hero holding the Move-Through permission can select and complete an ordinary Movement whose route passes through an occupied space, while no unit of either kind may end a Movement on one.

Prerequisites: Milestones 6.8 and 6.8a are complete. This milestone consumes their existing contracts and adds no new modification family.

Acceptance checks:

- `MovementReachabilityQuery` takes the authoritative modification set and the moving unit, and consults `ObstacleQuery` for both questions it already separates: an occupied space is traversable only for a mover holding `MoveThroughObstacles`, and is never emitted as a destination for any mover. A space outside the playable board keeps blocking both, permission or not.
- Passing through an occupied space costs the same movement as any other space entered, so the permission changes which routes exist rather than what they cost.
- Route commitment asserts occupancy only at the endpoint: a validated route moves its unit from origin to destination without any intermediate space being occupied, and the one-obstacle-per-space index is never transiently violated.
- The existing fork-based whole-route validation still refuses a route before any mutation, and a rejected route leaves board, session, card, continuation, and event state untouched.
- Without the permission every current Movement result is unchanged, proved against the same fixtures rather than asserted.
- Red Bulwark's `Next Turn` mode is the configured consumer: after that mode resolves, the hero's next turn offers a destination beyond an occupied space, completes it, and the offer disappears again once the effect expires.
- Projections and passive presentation forward the changed destination set without presentation deriving obstacle rules; destination highlights follow the projected legal endpoints.
- Snapshot restore, replay, retained-prefix Undo, and rejection rollback stay deterministic across a through-route commit.

Non-goals: No Object Push, direct Move, Place, or Swap displacement (6.4-6.6 own those), no new permission kind, no Fast-Travel change, no pathfinding cost model beyond one space per space entered, no Wasp definition, and no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68c-give-the-move-through-permission-its-movement-consumer-completion-evidence).

### [x] 6.8d Give every Active effect a per-activation instance identity

**Corrective amendment (2026-08-15, user approved).** Playing a card that creates an Active effect a second time silently does nothing. Each capability composes its own instance ID from source card, node, and definition, all of which repeat on a replay; `ActiveEffectLifecycle.Create` rejects a duplicate ID including a spent instance, so each handler skips creation and reads a finished effect as "already applied". Reproduced against all three Red Bulwark modes, so it is the mechanism, not one mode: `ModificationEffectHandler` serves all five 6.8 capabilities and 6.2a's `MarkActorHandler` repeats the pattern. Blue Feint would inherit it at 6.8b.

Observable outcome: A card that creates an Active effect creates a new one every time it is played, and the same activation re-executed inside its own resolution remains a no-op.

Prerequisite: none beyond 6.8. This milestone precedes 6.8b, which would otherwise ship two more modes with the defect.

Acceptance checks:

- Active-effect instance identity is minted through one shared contract rather than composed as a string inside each capability, and both existing creators use it. The identity distinguishes activations, not merely definitions.
- Two activations of the same mode from the same card in different rounds produce two distinct instances, and the second is not suppressed by the first's spent instance. Proved for every affected capability rather than one representative.
- Re-executing one activation inside its own resolution stays a legal no-op, and the lifecycle's existing live-duplicate suppression by declared scope identity is unchanged.
- A conformance rule proves no capability composes an Active-effect ID outside the shared contract, shown against a deliberate violator and a compliant twin.
- Instance IDs cross the snapshot boundary, so any change to their shape carries an explicit migrate-or-reject decision, and the pinned corpus and aggregate-invariance fixtures are observed before being re-pinned.
- Snapshot restore, replay, retained-prefix Undo, and simultaneous ordering stay deterministic across a second activation.

Non-goals: No new capability, effect family, duration, or rules content; no change to what any effect does; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68d-give-every-active-effect-a-per-activation-instance-identity-completion-evidence).

### [x] 6.8e Classify terrain as a passable obstacle rather than as off-board

**Corrective amendment (2026-08-15, user approved).** Rulebook p.18 makes terrain an obstacle like a unit or token, and a unit ignoring obstacles may enter and leave any obstacle's space while moving. The board models terrain as a space kind and the official map declares 82, but `AdjacentMoveAuthority` builds its board from playable coordinates alone, so `ObstacleQuery` cannot tell terrain from a coordinate off the map and refuses both unconditionally. The same conflation reaches Object Push at 6.4.

Observable outcome: A unit holding the Move-Through permission can complete a Movement whose route crosses a terrain space, while no unit of any kind may end a Movement on terrain and nothing may be placed there.

Prerequisite: milestone 6.8c has established route commitment and the reachability consumer this milestone extends.

Acceptance checks:

- The movement board distinguishes a terrain space, which exists and always blocks, from a coordinate that is not on the map at all. Terrain is not an occupant and does not enter the occupancy index.
- `ObstacleQuery` classifies terrain as an obstacle: it blocks every endpoint for every mover, and blocks transit only for a mover without the permission. A coordinate off the map blocks both for everyone, permission or not.
- Reachability builds a route across terrain for a permitted mover, never offers terrain as a destination, and charges the same movement for entering it as for any other space.
- Route commitment validates a terrain space for transit and refuses it as an endpoint, and a refused route mutates nothing.
- Terrain remains outside zone membership, Spawn-point eligibility, Place destinations, token placement, and every occupancy consumer; this milestone changes what may be crossed, not what may be occupied.
- Without the permission every current Movement, Fast-Travel, adjacency, reachability, and occupancy result is unchanged, proved against the same fixtures rather than asserted.
- The official map's declared terrain is the fixture for at least one case, so the rule is proved against real map data and not only a synthetic board.

Non-goals: No new terrain type, map-data change, map revision, zone rule, or placement rule; no Object Push or displacement behaviour (6.4 owns those); no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68e-classify-terrain-as-a-passable-obstacle-rather-than-as-off-board-completion-evidence).

### [x] 6.8f Require the live modification set at every rules-consuming seam

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8's review recorded that the modification parameter is optional and defaults to no modifications, so a consumer that forgets it loses rules silently and no mechanical check catches it. That is how the Move-Through permission reached a configured card with no consumer, and 6.8c added further optional parameters rather than closing the hazard.

Observable outcome: A rules-consuming entry point cannot silently resolve against no modifications; supplying none is an explicit, named decision.

Prerequisite: milestones 6.8c and 6.8d, so the movement and capability seams this rule governs are already in their corrected shape.

Acceptance checks:

- Every command and query path whose outcome a modification can change takes the live set explicitly. Where a genuinely rule-free context exists, it passes a named empty set rather than relying on an implicit default.
- A conformance rule proves no such entry point declares an implicit default, shown against a deliberate violator and a compliant twin.
- The turn authority remains the single place the live set is derived per command, and no consumer caches it across commands.
- Test and fixture call sites are updated with the set they actually mean, and no production default is retained merely to avoid touching them.
- No behaviour changes: every current result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family, query, or rules content; no change to how the live set is derived; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68f-require-the-live-modification-set-at-every-rules-consuming-seam-completion-evidence).

### [x] 6.8g Give the `Counts As` effective type its first rules consumer

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8 implemented the `Counts As` family and the deliberate split between `IntrinsicKindQuery` and `EffectiveUnitKindQuery`, then left the effective side without a production caller: `EffectiveUnitKindQuery` is reached only from `UnitPresenceQuery`, whose only callers are three assertions in `RuleModificationTests`. Every implemented rule that names a unit type - Attack target assembly, Heavy immunity, Minion Battle membership, minion supply, and displaced-minion correction - reads the intrinsic registers instead, and none of them declares that choice where a reader can see it. Milestone 6.8b would print a `Counts As` minion mode onto Blue Feint that no rule reads, which is 6.8c's defect repeated in the effective-type family.

Observable outcome: Every implemented rule that names a unit type declares whether it asks the intrinsic or the effective question, and at least one production rule consults the effective query, so a `Counts As` declaration changes a match outcome rather than only a query-level assertion.

Prerequisite: milestone 6.8f, so the seams this milestone audits already take the live modification set explicitly rather than defaulting to none.

Acceptance checks:

- Each implemented rule naming a unit type records its intrinsic-or-effective choice together with the rulebook clause that settles it. p.18's displaced-minion correction is intrinsic by explicit citation; any rule whose clause does not settle the question is recorded as an open rules question rather than answered by implementation convenience.
- At least one production rule consults `EffectiveUnitKindQuery`, and a hero holding a `Counts As` minion declaration changes that rule's outcome in a configured match rather than in a query test alone. If the audit finds that no implemented rule may ask the effective question under its own clause, this milestone reports that instead, and 6.8b's `Counts As` mode is rescheduled behind the rule that will consume it rather than shipped inert.
- Intrinsic representation, identity, ownership, occupancy kind, minion supply, and the p.18 correction exception are unchanged by any `Counts As` declaration, proved against the same fixtures rather than asserted.
- A conformance rule proves no rule naming a unit type reads a kind without going through one of the two queries, shown against a deliberate violator and a compliant twin.
- `UnitPresenceQuery` either gains its production caller here or is recorded as a declared future seam naming the milestone that will consume it; it does not remain a test-only production type by default.
- Without any `Counts As` declaration every current targeting, presence, battle, supply, and correction result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family, effective type, or unit kind; no new rule that names a unit type; no Minion Battle, Heavy, supply, or correction rule change beyond declaring which question it asks; no source-unverified official rule claim; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68g-give-the-counts-as-effective-type-its-first-rules-consumer-completion-evidence).

### [x] 6.8h Give marker attachments a per-activation instance identity

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8d fixed the Active-effect family's replay collision and recorded that the marker family carries the same latent compositional shape: the marker capability composes its instance ID as `{card}:{node}:{holder}`, with no activation anchor, so a marker given, returned to the supply, and given again to the same holder by replaying the same card composes an identical instance ID. The token family already carries round and turn in its composition (the 6.2 precedent the effect fix followed); markers are the one component family left behind.

Observable outcome: A card that gives a marker gives a fresh attachment every time it is played, including to a holder who returned that card's earlier marker, and re-executing one activation inside its own resolution remains a no-op.

Prerequisite: milestone 6.8d, whose shared-contract pattern and activation anchors this milestone extends to the marker family.

Acceptance checks:

- Marker instance identity is composed through one shared contract that distinguishes activations - the 6.8d anchor set of source card, action, node, round, and turn, plus the holder - rather than as a string inside the capability. Whether it reuses the effect contract's composition or a marker-shaped sibling is an implementation decision recorded in the commit.
- Give, return, and give again across rounds produces two distinct attachments with distinct instance IDs, and the returned instance never suppresses the later give. The failing observation records what the repeated composition does today - silent no-op or fault - rather than assuming it.
- Re-executing one activation inside its own resolution stays a legal no-op, and token instance identity is unchanged.
- A conformance rule proves no capability composes a marker instance ID outside the shared contract, shown against a deliberate violator and a compliant twin.
- Marker instance IDs cross the snapshot boundary, so any change to their shape carries an explicit migrate-or-reject decision, and the pinned corpus and aggregate-invariance fixtures are observed before being re-pinned.
- Snapshot restore, replay, and retained-prefix Undo stay deterministic across a second give.

Non-goals: No new marker rule, return condition, or holder semantics; no end-of-round or defeat return wiring (6.12 owns both); no change to what any marker does; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68h-give-marker-attachments-a-per-activation-instance-identity-completion-evidence).

### [x] 6.8i Retire the remaining optional rules parameters

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8f made the modification set a required parameter everywhere and flagged four survivors of the same hazard class, optional parameters whose omission silently changes a rules answer: `RuleModificationQuery.Build` and the `RuleModificationSet` constructor default their Tie Breaker state, which orders p.17's simultaneous modifications; `RuleModificationSet.IsImmuneTo` defaults its actor, and an omitted actor silently disables p.18's "you are never immune to your own actions" - exactly the clause 6.4's Object Push immunity consumer will depend on; and `UnitPresenceQuery.IsPresentWithin` defaults its self-exclusion.

Observable outcome: Ordering state, the acting object, and the self-exclusion are explicit call-site decisions at every rules seam; a genuinely absent value is written at the call site rather than supplied by a parameter default.

Prerequisite: milestone 6.8f, whose required-parameter regime and conformance rule this milestone completes; must precede 6.4, whose Push immunity consumer reads `IsImmuneTo`'s actor semantics.

Acceptance checks:

- All four parameters are required. Call sites state the value they mean; where a genuinely actorless or self-less context exists, the call site writes the undefined object explicitly rather than inheriting it from a default.
- The 6.8f conformance rule widens to prove no seam of the modification query surface declares an optional parameter of a rule-affecting type, shown against a deliberate violator and a compliant twin, and its subject-count guard still holds.
- The p.18 self-immunity exception keeps its existing focused coverage, and the actor is stated at every immunity call site that has one.
- The turn authority remains the single place the live set is derived per command; how the set is derived does not change.
- No behaviour changes: every current result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family or query; no immunity-consumer wiring (6.4 and 6.8b own Push immunity consumption); no change to how the live set is derived; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68i-retire-the-remaining-optional-rules-parameters-completion-evidence).

### [x] 6.8j Let a Skill complete at its own start step

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8g's fixture found a latent skill-backend fault: a Skill whose effect root resolves to completion during the start itself - no choice ever opens - reads its completing identity from `step.Frame ?? pendingResolution` in `SkillCardActionStrategyBackend.Complete`, and at start-completion both are null, so the session reports an engine fault. Every configured and synthetic Skill today opens a choice, so no player can reach it, but the fault makes choice-free Skills unwritable: any future card whose Skill simply applies its text would fault at start, and the 6.8g fixture had to adopt a trailing `may` step it did not otherwise need.

Observable outcome: A Skill whose effect completes without ever pausing resolves successfully at its start, emitting the same started/node/completed fact and checkpoint stream shape as any completed Skill.

Prerequisite: none beyond 6.2a's effect frame; independent of the other corrective milestones.

Acceptance checks:

- At start-completion the completing facts read identity from the started resolution and offer rather than from a null frame, and the ordered stream keeps its shape: Action Started, the Skill started fact, node facts with their declared checkpoints, the Skill completed fact, Action Ended.
- The failing observation is a regression fixture whose Skill root completes at start - reproducing the engine fault at the pre-fix revision through the configured `MatchSession` path - and completing successfully post-fix, with the source card resolved and the turn advancing normally.
- A Skill that pauses on a choice behaves exactly as before, proved against the existing skill fixtures rather than asserted.
- Snapshot restore, replay, and retained-prefix Undo stay deterministic across a start-completing Skill.
- The pinned corpus and aggregate-invariance digests are observed unchanged.

Non-goals: No new capability, effect family, or rules content; no Repeat semantics (6.5's family owns bounded Repeat); no change to choice-opening Skills or to the 6.8g fixture's card; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68j-let-a-skill-complete-at-its-own-start-step-completion-evidence).

### [x] 6.8k Give effect definitions a typed target-selection node

**Corrective amendment (2026-08-15, user approved).** Milestone 6.4a's subject widening cannot land inside 6.4a. Direct Move must be able to name a minion or a token to displace one, and a definition has no legal way to express that today: role bindings resolve from the execution frame, which holds no board reference, so there is no actor's-space role to bind, and the coordinate identity Object Push already uses for its targets can only reach a node through a prior node's output. The missing piece is a target-selection node, which is reusable targeting machinery - milestone 6.8's declared subject - rather than displacement machinery, so the user directed it here rather than into 6.4a. 6.4a's port and reachability were already widened at `44c256e`; only the capability parameter waits on this.

This is also the seam 6.5b and 6.6a need. Every conversion whose text targets another object - Red Needle's Push, Red Bombard's direct Move of another adjacent unit, Blue Sentinel's Swap - has to name that object somehow, and each inventing its own binding is the divergence this milestone prevents.

Observable outcome: An effect definition can name another board object as a typed selection, and the displacement capabilities accept any displaceable subject through it.

Acceptance checks:

- One registered capability offers board objects as an authoritative chooser-owned selection and publishes the chosen space as a typed coordinate output, so a later node binds it through the existing node-output binding rather than a new binding kind.
- Its candidate set is declared through the reusable targeting contracts rather than effect-local geometry, and an immune object is excluded from selection while a presence question over the same spaces still counts it.
- The direct Move capability's subject parameter accepts that coordinate, so a hero, a minion, and a token are all nameable; the reachability it consults is unchanged, since the port already serves every kind.
- A token subject holds no Move-Through permission of its own and reaches the transit predicate as the named absence, because p.19 states tokens are not units.
- The definition validator refuses a subject binding whose type or source node cannot supply a coordinate, at content load rather than mid-resolution.
- Focused capability and end-to-end session scenarios cover selection, rejection of a stale or illegal selection, snapshot restore, replay, and retained-prefix Undo.

Non-goals: No displaced-minion correction (6.4a owns it), no new displacement operation, no Place/Spawn/Swap/Replace, no Wasp definition, no card-name/hero-name branch, no configured live exposure, and no production presentation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68k-give-effect-definitions-a-typed-target-selection-node-completion-evidence).

### [x] 6.5c Give the Red Needle Push and Place branches their player-facing choice surfaces

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5a proved both branches of the converted Skill through the ordinary match session, but neither branch reached a surface a player can read. A value-shaped effect-choice candidate carries no option node to name, so `EffectChoiceLabels` falls back to `OPTION #n` and both branches present as anonymous buttons; the board lights nothing for either, though the same session already lights Movement, Fast Travel, and correction spaces; and the coordinate-addressed submitter 6.4b built for this consumer has no board caller at all. The defect is the shared one this shard collects: 6.4b gave the correction its surfaces and 6.8k deferred value addressing to its first consumer, and the consumer arrived without either being finished. Fixing it on the value/option split rather than on Red Needle is what makes 6.5b, 6.6a, and 6.7a inherit both surfaces.

Observable outcome: The Place branch offers no prompt control at all - its legal spaces are lit on the board exactly as Fast Travel's are and one click answers the choice - while the Push branch offers one named control per target that, when pointed at or focused, marks that target on the board and lights the space the push would leave it on.

Prerequisites: Milestones 6.5a, 6.4b, and 6.8k must be complete. This milestone consumes their projections, prompt vocabulary, and value addressing, and changes no rule, geometry, or candidate set.

Acceptance checks:

- An authority-computed preview accompanies a pending effect choice the way milestone 5.8b's Defense preview accompanies a Defense choice: for each value candidate it carries the board object standing on that space when one does, and the space that candidate's consuming operation would leave that object on when the operation is a Push. Presentation derives no direction, distance, occupancy, or obstacle result of its own.
- The preview finds that consuming operation and its declared distance by reading the paused frame's own definition graph and the registered capability contract, so no card, hero, or node name is branched on and any later definition of the same shape is previewed by the same code.
- A value candidate on an empty space carries no prompt control. The board lights every such candidate with the destination highlight Movement, Fast Travel, and corrections already share, and clicking a lit space forwards only that candidate's projected invocations, in the order the authority offers them, so an exactly-one choice completes in one click without presentation inventing a command.
- A value candidate naming a board object carries one control, labelled from projected occupancy - team, intrinsic type, and projected ordinal for a minion, configured display name for a hero, candidate ordinal for a token - and never from printed card text. The token form was corrected during implementation from "catalog name", which milestone 6.2's still-binding decision that presentation receives no token projection makes unavailable; the ordinal is everything presentation is honestly given about one.
- Pointing at or focusing such a control marks its target with the choice cue milestone 6.1ao settled, extended from minions to whichever object kind stands on the space, and lights that candidate's projected resulting space with a second highlight whose color is distinct from the destination one, because a resulting space is a prediction rather than something selectable.
- Both cues are anchored to the projection they were read from, so an accepted command, refusal, restore, replay, or Undo drops them; a zero-space push marks the target's own space; and a control whose target is no longer projected submits nothing and rebuilds from the authoritative choice.
- Mode choices keep their existing controls, and the option/value distinction is the only thing deciding which surface a candidate reaches.
- Focused presenter, board-adapter, projection, and saved-scene scenarios cover both branches plus stale controls, refusal, restore, replay, and retained-prefix Undo.

Non-goals: No change to the Skill's rules, geometry, candidate sets, immunity filtering, or the 6.4a correction path; no board-click selection of a Push target, since the named control is that selection; no new pending-choice family or prompt framework; no authority-side change to how a choice confirms; no Wasp definition, printed-text parsing, card-name/hero-name branch, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65c-give-the-red-needle-push-and-place-branches-their-player-facing-choice-surfaces-completion-evidence).

### [x] 6.5aa Correct shared immunity, effect-target legality, and Counts As Minion Battle

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65aa-correct-shared-immunity-effect-target-legality-and-counts-as-minion-battle-completion-evidence).

**Corrective amendment (2026-08-16, user approved).** Hands-on validation of Blue Feint and Red Needle exposed three shared-mechanics defects rather than card-definition defects. The standing p.13 Heavy immunity is consulted only by Attack targeting, so a protected Heavy can still be offered to and affected by Push; Blue Feint's activity-scoped Push immunity is revalidated by the Push handler but the earlier target-selection node hard-codes generic Skill activity, so the opponent is offered an illegal choice that later stops; and Minion Battle deliberately counts intrinsic miniatures despite p.17's `Counts As` replacement, so Blue Feint produces no visible extra-minion outcome. The user settled open question 7 for the synthetic slice: a hero counting as a minion participates fully in the Minion Battle count and removal choice; if removed as a minion, the hero is not Defeated, awards no coins, spends no Life, and uses the ordinary p.7 optional Hero Respawn procedure at its next action opportunity.

Observable outcome: One authoritative immunity decision protects a standing Heavy and every declared immune unit from both selection and application by the exact activity affecting it, while Minion Battle counts effective minions, may remove a counts-as hero under the same exact-count/Heavy-last rules, and an off-board removed hero receives the ordinary optional next-turn respawn choice.

Prerequisites: Milestones 6.8b, 6.5a, and 6.5c must be complete, so both configured effects, their production consumers, and their player-facing value-choice surfaces exist before the correction.

Acceptance checks:

- A shared immunity authority composes board-state Heavy immunity with live declared immunity. A protected intrinsic Heavy cannot be targeted or affected by any friendly or enemy action, including Attack, direct Move, Push, and Place, while presence and non-action system transitions still observe it; it becomes eligible immediately when no qualifying friendly non-Heavy effective minion remains in its Battle Zone.
- Every currently implemented unit-affecting capability declares the activity it performs and checks the shared immunity authority both when candidates are derived and immediately before mutation. Automatically affected and multi-target units use the same contract, and a conformance check prevents a registered unit-affecting capability from omitting it.
- Target selection evaluates the exact consuming activity rather than hard-coded generic Skill activity. Blue Feint's Push-immune hero is absent from Red Needle's target controls and board cues, an unrelated immunity leaves it present, and a stale target that becomes immune is rejected atomically and refreshed out of the projection.
- Minion Battle derives each team's count from effective minion presence in the Battle Zone. Blue Feint's affected hero contributes one generic non-Heavy minion without changing intrinsic hero identity, occupancy kind, ownership, supply, or displaced-minion-correction eligibility.
- The losing team may select that counts-as hero alongside intrinsic minions under the existing exact-count, one-at-a-time, non-Heavy-before-Heavy contract. Removing it is a typed Minion Battle Remove rather than Defeat: the hero leaves the board, no coins or Life change, no Attack-defeat consequence runs, and stable hero/card/effect identity is preserved.
- At that hero's next action opportunity, the authority offers the p.7 choice to respawn on any empty Spawn point in its Throne Zone; when none is empty it offers the equally nearest empty spaces adjacent to those Spawn points. The player may decline and resolve the card without an action. Presentation projects one button per authoritative destination plus the decline button; hovering or keyboard-focusing a destination button highlights that exact board space with the same material Movement uses.
- When a temporary effective-minion identity ceases and its team then has no effective Battle-Zone minion, authority records the appropriate Lane-Push request without consuming it; milestones 6.9-6.11 retain ownership of consumption, Battle-Zone transition, and minion respawn.
- Focused failing-then-passing regressions cover protected-Heavy selection/application under friendly and enemy actions, Blue Feint's leaked Push choice, stale activation, effective Minion Battle count/winner/removal, no reward/Life consequence, optional Hero Respawn destinations/decline, passive presentation, snapshot restore, replay, retained-prefix Undo/restart, and ordered events. Because the correction crosses shared targeting, effect mutation, round state, hero occupancy/respawn, persistence, and configured presentation, it completes at T3 with both unfiltered suites.

Non-goals: No Red Needle or Blue Feint printed rule/value change; no card-name/hero-name branch; no general Hero Attack-Defeat removal, reward, Life, or victory consequence; no Lane-Push consumption, Battle-Zone transition, or minion respawn; no Swap/Replace/standalone Remove/Repeat implementation; no Wasp definition, production art, animation, or audio.

### [x] 6.5ab Hide removed heroes in configured board presentation

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65ab-hide-removed-heroes-in-configured-board-presentation-completion-evidence).

**Corrective amendment (2026-08-16, user approved).** Hands-on validation of 6.5aa showed that Minion Battle authoritatively removed Blue's counts-as hero and minion in the correct order, but only the minion disappeared from the configured board. `BoardInteractionUnityView` retained every hero object and reconfigured it when occupancy existed, but had no absent-occupancy branch corresponding to its minion path, so the removed hero remained visible and selectable.

Observable outcome: The configured board shows exactly the authoritative hero occupancies. A removed hero disappears immediately; a later Respawn, Undo, restart, or restore reactivates and repositions the same retained hero view.

Prerequisite: Corrective milestone 6.5aa must be complete so an authoritative off-board hero and its respawn path exist.

Acceptance checks:

- Rendering a projection without a configured hero's occupancy clears its choice cue and deactivates that hero GameObject, including its pointer collider; no hero-removal rule is inferred by presentation.
- Rendering a later projection containing that stable hero identity reactivates the retained view and configures its authoritative coordinate, covering Respawn, Undo, restart, and restore without rebuilding the configured hero set.
- The regression is observed failing before the production change and passing afterward. A saved-scene Play Mode case verifies disappearance and reappearance, and the complete directly affected board-interaction Edit Mode and saved-scene Play Mode fixtures remain green at T2.

Non-goals: No Minion-Battle count, selection, removal, timing, event, snapshot, or respawn-rule change; no minion presentation change; no new scene object, prefab, identity, pointer framework, art, animation, or audio.

### [x] 6.5ac Refuse an effect option the actor cannot perform, and say why

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65ac-refuse-an-effect-option-the-actor-cannot-perform-and-say-why-completion-evidence).

**Corrective amendment (2026-08-16, user approved).** Hands-on validation of 6.5b found that Red Bombard's Spawn mode does nothing when chosen, and the cause is general rather than card-specific: a `Choose` node offers every child unconditionally, so a mode that can do nothing is selectable, and choosing it runs the branch to a mandatory stop that spends the card, ends the turn, changes nothing on the board, and explains nothing. The only record is a `SkillEffectNodeStopped` fact in the diagnostic log. This is the third appearance of the shape 6.8c and 6.5c already corrected - a rule printed onto a configured card that a player can select and observe nothing from - and 6.5b's own commit body wrongly recorded the silence as a Play Mode fixture limitation and pinned it as correct. The user settled the rules question this raises, recorded as open question 11 in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions): an option the actor cannot perform is refused rather than offered, and the player is told it cannot be done. Correcting it on the option contract rather than on Red Bombard is what makes 6.6a and 6.7a inherit it.

Observable outcome: A pending `Choose` offers only the options the acting player can actually perform; an option that can do nothing is still shown, greyed out and not selectable, so no player spends a card on a mode that was never going to do anything.

Prerequisites: Milestones 6.5b and 6.5c must be complete - 6.5b is the conversion that exposed the defect and supplies both proving cases, and 6.5c owns the option/value split this surface extends.

Acceptance checks:

- Option feasibility is authority-computed and derived from the branch's own declared contract rather than from any card, hero, or node name, so every existing and later definition of the same shape is judged by the same code.
- The feasibility question is answered without performing anything. A capability's `Execute` may apply rather than pause - marking an actor, creating an Active effect, placing a token - so a probe that calls it would perform the effect it was asked about; the capability contract instead answers feasibility separately, every registered handler answers it from the candidate derivation it already has, and a conformance check prevents a registered capability from omitting it, in the shape milestone 6.5aa used for the shared immunity check. A probe mutates no board, card, effect, component, supply, timing, or identity state, and mints nothing.
- An option reported unperformable carries no select invocation in the projection, and a stale or forged select naming it is rejected atomically without touching card, occupancy, continuation, or events.
- That option is still projected and still readable, and presentation renders it greyed rather than hiding it, so a player sees the mode exists and that it cannot be taken. The unavailability is projected by the authority and never inferred by presentation, but no reason text accompanies it: greying alone is the surface this milestone owes, by the 2026-08-16 user decision that it is obvious enough for now. Performable options keep the controls, deselect, and explicit confirm milestone 6.8a gave them.
- A greyed option carries no submittable command, so a click on one cannot reach the session by any route. `RoundActionPresenter.Submit` validates against the lists it projects, so whatever shape carries an unavailable option joins that legality set rather than sitting outside it - the same requirement every view-model list added since 6.8a has had to meet.
- Feasibility is never written. It is re-derived with the candidate set on snapshot restore, replay, and retained-prefix Undo, and after every accepted command, so a mode that becomes performable within a frame becomes selectable and one that stops being performable stops being offered. No schema change is expected; the pinned digests are measured rather than assumed.
- A definition whose options include an unperformable one remains legal at content load: the existing rule that a `Choose` declares at least two options governs the definition, never the runtime selectable set. When no option can be performed, the node reports itself impossible and the enclosing failure policy decides what that means; the player sees every option greyed before that happens rather than meeting the stop with no warning at all.
- The two converted cards are the proving pair. From the saved scene's starting position both of Red Bombard's modes are unperformable - the unit supply is empty because every declared miniature stands, and nothing is adjacent to the acting hero - so both are greyed and neither can be chosen; once a Red miniature leaves the board the Spawn mode becomes selectable in the same session. Red Needle's two branches stay selectable throughout, which is the control showing the check measures feasibility rather than disabling whatever it does not understand.
- Focused authority, projection, presenter, and saved-scene scenarios cover both proving cases, the all-unperformable node, stale and forged selects, restore, replay, and retained-prefix Undo. The silent-stop path receives a failing-then-passing regression.

Non-goals: No change to what any card declares, to any capability's rules, geometry, candidate sets, immunity filtering, or the 6.4a correction path; no automatic selection of a sole remaining performable option, because refusing an unperformable option is not choosing on the player's behalf; no per-option reason text, tooltip, or explanatory popup, which the 2026-08-16 user decision defers past greying; no new pending-choice family or prompt framework; no change to how a choice confirms; no change to the separate open finding that `up to` admits distance 0 for Push but not for direct Move; no Wasp definition, printed-text parsing, card-name/hero-name branch, production art, animation, or audio.

### [x] 6.5ad Read one optional distance the same way for every displacement

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65ad-read-one-optional-distance-the-same-way-for-every-displacement-completion-evidence).

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5b reported, and did not settle, that the two displacement capabilities read `up to X spaces` differently: Object Push offers distances `0..X` and calls zero a legal choice, while direct Move offers only `1..X`, because its candidate set is the Movement action's own reachability walk and that walk never emits the mover's own space. The question was recorded as unsettled on the assumption that the rulebook could not be searched. It can: `pdftotext` extracts the second-printing PDF in this environment even though page rendering does not work, and the text answers it. Page 10 lists `up to` beside `you may` and `if able` as non-mandatory wording, and page 11's movement-in-the-card-text clause requires the exact number of spaces only where the distance is *not* optional. No clause sets a floor of one space, so `up to 2 spaces` admits zero and the Push's reading is the correct one. This is therefore a defect in one capability rather than an open rules question, and it is scheduled as a rules correction rather than folded into a card conversion, because it belongs to milestone 6.4's capabilities and settling it inside a card would be a rule decided by a card.

Observable outcome: A card-text direct Move of `up to X spaces` lets the acting player choose to move the unit no spaces at all, the same way a Push of `up to X spaces` already lets them choose distance zero. A mandatory exact distance is unchanged: `Move 2 spaces` still means two or p.10's stop.

Prerequisite: None beyond milestone 6.4, which owns both capabilities. It is independent of the showcase route and of corrective milestone 6.5ae.

Acceptance checks:

- The optional-distance reading is stated once and cited to pages 10 and 11, and both displacement capabilities are shown to consult it rather than each carrying its own answer. A conformance-style or shared-derivation check prevents the two from drifting apart again.
- Under `up to`, a direct Move admits the mover's own space as a candidate destination, and choosing it relocates nothing, publishes the mover's unchanged space as the node's output, and reports a legal applied result rather than a decline or a failure - the same distinction p.18's zero-space push already draws.
- Under every other failure policy the admitted set is unchanged, and the exact-distance case still stops the action when the exact number cannot be walked.
- Choosing zero raises no movement, displacement, correction, or Lane-Push consequence that moving one space would not have raised, and a minion that never left the Battle Zone opens no after-action correction.
- Milestone 6.5ac's option walk reads the widened candidate set without change: a direct Move branch whose subject can only stand still is performable rather than refused, which is the behaviour difference a player sees.
- The regression is observed failing before the production change and passing afterward, at the capability level and through the configured Red Bombard branch. Because the change crosses candidate derivation, effect resolution, and the pinned corpus, it completes at T3 with both unfiltered suites, and any digest movement is measured against the pre-change revision rather than assumed.

Non-goals: No change to the Movement action's own reachability, allowance, or Fast Travel; no change to Push geometry, the simultaneous batch, or immunity; no new capability, revision, or parameter; no card text, content revision, or card-name branch; no schema change is expected, and one is not to be introduced to carry a distance.

### [x] 6.5ae Let the player close an action whose every option is refused

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5ac delivered every acceptance check but one, and recorded the gap rather than dropping it: the player does not see the options greyed when *no* option of a `Choose` can be performed, because the node reports itself impossible without opening a prompt. The reason was that pausing there left the match with no legal command - a mandatory `Choose` offers no decline, and the round presenter withholds every action, Fast-Travel offer, and End Turn while an effect choice stands - so the alternatives looked like a frame escapable only through Undo or a new prompt family that milestone's non-goals forbade. The user settled it on 2026-08-16: in that position End Turn is the one legal command, it is offered rather than performed, and the player clicks it. That is not a new prompt family; it is the stop affordance a paused Movement has carried since Phase 1, where `PendingMovementChoice.CanStop` projects the same End Turn control and `StopMovement` closes the action.

Observable outcome: A `Choose` whose every option is refused pauses with all of them shown greyed, and the only control offered is End Turn. Clicking it stops the action exactly as p.10's stop already does - the same card state, the same facts, the same turn hand-off the silent stop produced - and nothing stops the action until the player asks for it.

Prerequisites: Corrective milestone 6.5ac must be complete, since it owns the refusal this surfaces.

Acceptance checks:

- The pending effect choice carries an authority-projected stop invocation, in the shape `PendingMovementChoice.CanStop` established, offered only where no option can be performed. Presentation renders it as the existing End Turn control and never submits it on the player's behalf, on any timer, or as a consequence of any other command.
- Accepting that stop produces exactly the outcome the unprompted stop produced: the same node result, the same ordered facts, the same card resolution status, and the same turn continuation, so the correction changes when the action ends and not what ending it means.
- While such a choice stands, every other control remains withheld, and a command other than the stop is rejected atomically without touching card, occupancy, continuation, or events - including a select naming any of the greyed options.
- Every option is projected and greyed, so the player reads which modes exist and that none can be taken, with no reason text, by the same 2026-08-16 decision that governs the mixed case.
- The stop is never written to a snapshot as an answer: the pause, the refusals, and the stop invocation are re-derived on restore, replay, and retained-prefix Undo from the state as it then stands, so a frame restored after the last refusal lifts offers the option again instead of the stop.
- An optional `Choose` is unaffected: where its own policy already declines, it continues to decline without pausing, because there is nothing for a player to close.
- The regression is observed failing before the production change and passing afterward, including the saved-scene position where Red Bombard's two modes are both unperformable, which is where the defect was found. Because the change crosses effect resolution, the projected command set, and configured presentation, it completes at T3 with both unfiltered suites.

Non-goals: No change to which options are refused or how feasibility is judged; no automatic stop, no automatic selection, and no automatic End Turn anywhere; no per-option reason text; no new pending-choice family, prompt framework, or director command kind beyond the stop this reuses; no change to how a performable choice confirms; no Wasp definition, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65ae-let-the-player-close-an-action-whose-every-option-is-refused-completion-evidence).

### [ ] 6.5af Refuse a Skill that can do nothing before the turn is spent on it

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5ae gave the all-refused `Choose` a way out, but it did not stop the player being walked into it. Nothing judges a Skill when its action is *offered*: `ActionOfferQuery` derives printed offers from card state alone - definitions, instances, and the actor - and always reports `IsAvailable`, so a Skill whose effect can apply nothing is offered exactly like one that can. The player starts it, and only then learns it was inert.

Rulebook p.12 is explicit that this is what the Hold action is for: it names performing any other action being not possible as the common reason to Hold, and states that because any card can Hold, a player is never forced to perform an action they do not want to. The correction is therefore sourced rather than invented - the rules assume the player can see that an action is impossible before choosing it.

What the player loses is not the Hold, which stays available, and not the outcome, which milestone 6.5ae already made identical. It is the *other* action on the same card. Red Bombard prints a Secondary Movement 2 beside the Skill this defect was found on, so in that position starting the inert Skill spends a turn that could have moved two spaces, and only Undo takes it back.

Observable outcome: A Skill whose effect can apply nothing is shown but cannot be started, so the player picks another action on the card - or Hold - while it is still theirs to pick. Every other action on the card is offered unchanged, and a Skill that can do something is offered exactly as before.

Prerequisites: Corrective milestones 6.5ac, which owns the feasibility question, and 6.5ae, which owns what happens when a paused choice has nothing in it.

Acceptance checks:

- The feasibility question 6.5ac built is asked of a whole Skill's effect root through the same walk and the same `Anticipate` contract, against the execution frame a start *would* create rather than one it created. There is no second copy of the rule, no probe performs anything, and 6.5ac's two conformance rules cover the new caller unchanged because it reaches the same members.
- An inert Skill is projected as an offer that cannot be started: it keeps its label and its place in the offer order, carries no invocation, and the command path refuses it atomically, so a start naming it - forged, stale, or left over from an older model - touches no card, occupancy, continuation, or event.
- Every other action the same card offers is unaffected and still startable, and Hold remains available throughout. Nothing performs Hold, selects an action, or ends a turn on the player's behalf.
- The judgement is re-derived per projection rather than stored, so a Skill that becomes performable inside the same turn becomes startable, and one that stops being performable stops being startable.
- Nothing about the judgement reaches a snapshot: restore, replay, and retained-prefix Undo re-derive it from the state as it then stands, for the reason 6.5ac's refusals and 6.5ae's stop are re-derived.
- The projection cost of asking it is measured rather than assumed, because the offer set is rebuilt on every command while a paused choice is built only at a pause.
- The regression is observed failing before the production change and passing afterward, including the saved-scene position where Red Bombard's Skill is inert and its Secondary Movement is not. Because the change crosses offer derivation, the command path, and configured presentation, it completes at T3 with both unfiltered suites.

Non-goals: No change to Movement, Attack, Defense, Fast Travel, Clear, or Hold availability, or to any general-rule or reaction offer; no change to what feasibility means or to which options milestone 6.5ac refuses; no per-offer reason text, tooltip, or explanation, by the same 2026-08-16 user decision that governs a greyed option; no automatic Hold, automatic action selection, or automatic End Turn anywhere; no new pending-choice family, prompt framework, or director command kind; no card text, content revision, or card-name branch; no schema change, and one is not to be introduced to carry a judgement that is derived.
