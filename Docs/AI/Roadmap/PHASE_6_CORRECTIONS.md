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

### [ ] 6.8e Classify terrain as a passable obstacle rather than as off-board

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

### [ ] 6.8f Require the live modification set at every rules-consuming seam

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

### [ ] 6.8g Give the `Counts As` effective type its first rules consumer

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

