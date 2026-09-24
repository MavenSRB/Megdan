# Phase 8 - Twenty-third hero: Trinkets the Scavenger

Return to [roadmap](../ROADMAP.md). [The session brief](../SESSION_BRIEF.md) owns the active and next route.

## Authorization and scope

On 2026-09-24 the user chose Trinkets from the ten confirmed undealt heroes: "Trinkets is approved." This authorizes the bounded sequence **8.92 -> 8.92a -> 8.92b -> 8.92c -> 8.92d -> 8.92e -> 8.92f -> 8.92g** through a fully selectable, artworked Trinkets, every dealt printed face, and the permanent player-path gate. Execute in order with separate validation and local commits. A demonstrated defect necessary to complete a check belongs to its finding milestone; a new rule interpretation outside the settled facts below requires a recorded decision. No other hero, package, or remote publication is included.

[Trinkets' rules shard](../Rules/HERO_TRINKETS.md) owns source facts, the printed inventory, capability mapping, and rule questions. The user settled the Barrier supply at **three** and supplied the Turret contract: it is one obstacle only, never terrain, a token, or a unit; placing it again repositions it; its properties persist across card-zone changes, rounds, and Trinkets' defeat; independent range from Trinkets and the Turret is required where printed. Trinkets remains the actor and targeting source. The Turret and Barrier are different components and supplies. The user also supplied a designer answer settling the Blue pre-Defense discard timing. The p.20 Handicap, Ramshackle Turret, is inventoried but boxed at setup.

### [x] 8.92 Inventory Trinkets and map the implementation sequence

Outcome: verify all nineteen fronts, the profile, dashboard and component evidence; record printed values and exact text, the user's Turret and Barrier facts, rulebook intersections, existing-capability matches, new contracts, and any unresolved interpretation with its affected block. No executable content is registered.

Acceptance: nineteen distinct card fronts and one shared back accounted for; selection roles and complexity, the dashboard, items, stat bars, Turret and Barrier components inspected; the capability map probes the actual engine seams; every unresolved rule question has an owner and blocks only its dependent milestone. T0 documentation validation.

Non-goals: gameplay code, assets in the build, a package.

Completed 2026-09-24. The nineteen-front inventory, dashboard/profile, component identities, user-supplied supply and Turret contract, designer timing answer, and capability-gap probe are in [Trinkets' rules shard](../Rules/HERO_TRINKETS.md). Three stars; Sniper, Damager, Pusher, Tokens; items Attack/Defense/Initiative x3 and Movement/Range/Radius x1. The Turret is a persistent generic obstacle only, which existing dynamic-token handlers cannot stand in for; Defense offers currently derive before the `DefenseResponse` checkpoint and must be refreshed after the designer-ordered discard. The six implementation blocks and final gate are bounded above. T0.

Historical evidence: [8.92 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-892-trinkets-inventory-completion-evidence).

### [x] 8.92a Implement the persistent Turret and finite Barrier components

Outcome: one repositionable non-token Turret obstacle with independent identity and three Barrier tokens can be placed, removed, projected and restored under authoritative rules; synthetic scenarios prove the components before any printed face is composed.

Acceptance: separate supplies and identities; the Turret's occupied-space, non-terrain, non-token, movement, re-placement, removal, round, defeat, snapshot, replay, Undo and owner-scoped projection cases; placement and removal refuse illegal or stale choices atomically; Barrier placement's printed count, adjacency to the Turret, finite-supply and Defense contribution cases; affected existing token and terrain regressions. T2 minimum.

Non-goals: printed face composition, dealing Trinkets.

Completed 2026-09-24. A narrow board port places, repositions and removes the one actor-bound generic obstacle; a read-only effect fact publishes its live location. It is neither terrain nor a token, and remains in board state through round cleanup and owner absence. Barrier is a separate three-token definition with per-token +1 Defense for adjacent friendly heroes. A first-placement handler rechecks adjacency to the live Turret after a paused answer; later placements use the ordinary printed-Radius token handler. Synthetic cases cover supply exhaustion, stale choices, obstacle blocking/classification, owner absence, preimage replay/restore, authoritative snapshot and both seat projections. The new capabilities and component definition remain unregistered until a dealt card can reach them. Focused T2 evidence is in the milestone commit; no Unity-facing presentation changed.

Historical evidence: [8.92a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-892a-trinkets-component-foundation-completion-evidence).

### [~] 8.92b Compose the Turret-linked Attacks

Outcome: the five Red faces apply their printed targeting, line bonus and repeat clauses, including independent Range from Trinkets and the Turret.

Acceptance: every face's legal, refused, boundary and item-modified target sets; line bonus from both origins; Steam Discharge and Flame Belcher's different-unit repeat exclusions; Turret removal/reposition between offer and answer; Trinkets as actor and targeting source (including Spell Break and Smoke Bomb interaction), Attack, Defense and deterministic continuation regressions. T2 minimum.

Non-goals: Blue and Green faces, dealing.

### [ ] 8.92c Compose disruption and Barrier support

Outcome: Disruptor Jolt, Pulse and Grid run their timed pre-action discard or defeat effects from the Turret's live Radius; Deployable Barrier and Bastion place their printed up-to counts and grant the printed adjacent Barrier Defense.

Acceptance: discard versus defeat ordering, one-use deactivation, radius and item changes, Turret removal/reposition, immunity, insufficient cards, zero-to-maximum placements, adjacency to at least one Barrier, effect lifetime, restore/replay/Undo, and directly affected timing/value regressions. A focused p.14 case proves the designer's answer: a defender's sole eligible Defense card with a Defense primary is discarded by the Blue effect before step 3 and cannot defend. T2 minimum.

Non-goals: Green faces, dealing.

### [ ] 8.92d Compose swap, Place and Turret removal

Outcome: Early Prototype, Updated Design, Self-Destruct, Perfected Design and Emergency Protocol run their exact conditions, chooser options and endings.

Acceptance: self in Turret Radius; legal unit/token Swap and self Place; Early Prototype's required Turret removal versus Updated Design's persistence; up-to-two enemy heroes, discard-or-defeat, optional candidate count, immunity and lack of a valid candidate; final Turret removal; snapshot/replay/Undo on each pending decision; affected swap, placement and card-discard regressions. T2 minimum.

Non-goals: Ultimate, dealing.

### [ ] 8.92e Compose Unlimited Firepower and reconcile the complete set

Outcome: the unlocked Ultimate grants its printed +1 Range and +1 Radius while Trinkets stands; all eighteen dealt faces plus the boxed Handicap have complete authoritative definitions behind the registration gate.

Acceptance: item and Ultimate contributions compose once at every actor- and Turret-centred query; all nineteen definitions validate and all card text is accounted for; source-to-definition and shared-foundation claims reconciled; focused full-set tests and directly affected regressions. T2 minimum.

Non-goals: registration, pick screen, artwork or player-path gate.

### [ ] 8.92f Deal Trinkets and present his artwork

Outcome: register eighteen playable definitions, the hero profile and physical components; add Trinkets to every legal picker and configuration route with the printed stars and roles, hero and component artwork, and the card hover/face catalog.

Acceptance: stable ids and measured pins; staged hand and upgrade tree; setup, selection, filters, spawn, projection, visible Turret/Barrier pieces and component returns; private source artwork stays private; schema and wire bump only if measured necessary; focused Unity input and visual checks plus affected regression tier. T2 minimum.

Non-goals: a new distributable package or another hero.

### [ ] 8.92g Gate Trinkets through the complete player path

Outcome: a content-derived matrix and real-input journeys cover every dealt face and component, with no parked or skipped required journey.

Acceptance: all declared interaction shapes mapped exactly to named journeys; attacks, repeats, both Turret placements, three-Barrier supply, disruption, Swaps, self Place, Turret removal, Ultimate and items, progression, defeat/respawn, round persistence, stale input, snapshot/restore, retained-prefix Undo, network projection agreement and whole-face footprint; complete unfiltered Edit Mode and Play Mode suites green with zero new skips and no required skip, plus the permanent gate's zero-skip rule. T3.

Non-goals: a package or the next hero.
