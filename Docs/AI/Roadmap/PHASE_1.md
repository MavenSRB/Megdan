# Phase 1 - Smallest board interaction

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_1 handoffs](../Archive/Phase-Handoffs/PHASE_1.md).

## Phase 1 - Smallest board interaction

### [x] 1.1 Define hex coordinates and adjacency

Observable outcome: Pure C# code represents a hex and returns its six neighbors.

Acceptance checks:

- Six unique neighbor directions are tested.
- No Unity scene object is required by the tests.

Non-goals: No rendering, pathfinding, zones, terrain, or units.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-11-define-hex-coordinates-and-adjacency-completion-evidence).

### [x] 1.2 Render a minimal connected test grid

Observable outcome: A small set of flat placeholder hex meshes appears on the XZ plane under a fixed camera.

Acceptance checks:

- Visual hex positions correspond to logical coordinates.
- No overlaps or gaps caused by coordinate conversion.

Non-goals: No official map, camera movement, selection, or rules.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-12-render-a-minimal-connected-test-grid-completion-evidence).

### [x] 1.3 Place one primitive hero

Observable outcome: One placeholder hero occupies one logical hex and appears on that hex.

Acceptance checks:

- Occupancy exists in authoritative data.
- Visual placement is derived from authoritative data.

Non-goals: No teams, movement, animation, cards, or turns.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-13-place-one-primitive-hero-completion-evidence).

### [x] 1.4 Select the hero and one adjacent destination

Observable outcome: Pointer input can select the hero and highlight valid adjacent empty hexes.

Acceptance checks:

- Only adjacent empty hexes are highlighted.
- Selection is usable with Unity's pointer abstraction.

Non-goals: The hero does not move yet.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-14-select-the-hero-and-one-adjacent-destination-completion-evidence).

### [x] 1.5 Commit one adjacent move through rules authority

Observable outcome: Pure rules code accepts an adjacent-move intent command, validates it, commits authoritative occupancy, and returns an ordered movement rule event.

Acceptance checks:

- The command refers to stable hero identity and explicit origin/destination coordinates.
- A legal adjacent move makes the origin empty and destination occupied.
- An invalid or stale command returns a structured rejection, changes nothing, and emits no accepted state-change event.
- The accepted event contains rules facts only and no animation or scene instruction.
- Focused pure-rule tests cover legal, non-adjacent, occupied, wrong-origin, and unknown-hero requests.

Non-goals: No pointer wiring, presentation movement, movement range, cards, turns, networking, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-15-commit-one-adjacent-move-through-rules-authority-completion-evidence).

### [x] 1.6 Connect adjacent selection to the committed move

Observable outcome: Clicking a highlighted destination submits the milestone 1.5 command, then the primitive hero view snaps to the position derived from updated authoritative occupancy and the emitted event.

Acceptance checks:

- UI/pointer code never assigns occupancy directly.
- A rejected command leaves selection and presentation consistent with unchanged authoritative state.
- A legal move clears obsolete highlighting and renders the hero on the authoritative destination.
- Focused Edit Mode and Play Mode tests cover valid and invalid pointer-to-command flows.

Non-goals: No multi-space movement, card, turn, path automation, networking, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-16-connect-adjacent-selection-to-the-committed-move-completion-evidence).

### [x] 1.7 Expand the placeholder test grid

Observable outcome: The scene contains a larger connected placeholder grid with enough space to test movement of several steps, alternate routes, and later obstacle placement.

Acceptance checks:

- The grid contains pairs of spaces at logical distances 1, 2, and 3 or greater.
- At least one pair of spaces has more than one valid route between it.
- Every rendered center still corresponds to its authoritative axial coordinate.
- Existing seven-hex rendering and occupancy behavior remain valid.

Non-goals: No official map data, movement-rule change, obstacle, zone, terrain, card, turn, or camera movement.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-17-expand-the-placeholder-test-grid-completion-evidence).

### [x] 1.8 Move up to a configurable number of spaces

Observable outcome: A movement session gives the primitive hero a configurable allowance greater than one, highlights every empty destination reachable within that allowance, and lets the tester move to one chosen destination with one click.

Acceptance checks:

- Every internally committed step is adjacent to the hero's current space.
- Each step consumes exactly one point of movement allowance.
- The hero cannot take more steps than the allowance.
- Moving zero spaces by stopping before a destination click is legal.
- The chosen destination is explicit; the deterministic internal route is recorded as ordered adjacent steps/events.
- Every committed step uses the milestone 1.5 command/event authority.
- Focused tests cover zero movement, exact allowance, attempted over-movement, reachable-destination highlighting, deterministic route recording, and unreachable destination rejection.

Non-goals: No cards, turns, Fast Travel, movement from card text, movement-limiting effects, obstacles, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-18-move-up-to-a-configurable-number-of-spaces-completion-evidence).

### [x] 1.9 Add obstacles and enforce movement blocking

Observable outcome: Placeholder obstacles can occupy spaces on the expanded grid, and multi-step movement can route around them but cannot enter or pass through them.

Acceptance checks:

- An obstacle prevents the hero from entering its space.
- An obstacle cannot be crossed as an intermediate movement step.
- The tester can choose a reachable legal destination when the allowance permits it.
- A space can contain at most one obstacle, including a unit.
- Reachable-destination highlighting excludes blocked spaces and spaces that require passing through blocked spaces.
- Focused tests cover blocked destinations, blocked intermediate spaces, legal detours, insufficient allowance for a detour, and occupancy conflicts.

Non-goals: No ability to ignore obstacles, obstacle subtypes, tokens, terrain art, other units, path automation, cards, turns, Fast Travel, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-19-add-obstacles-and-enforce-movement-blocking-completion-evidence).

### [x] 1.10 Separate card definitions from card instances

Observable outcome: Pure C# code distinguishes immutable, versioned placeholder card content from stable runtime card instances owned by the primitive test hero.

Acceptance checks:

- A `CardDefinition`-equivalent has a nonempty stable definition ID, content revision, and display name.
- A `CardInstance`-equivalent has a different stable instance ID, definition reference, owner ID, and data-shaped runtime state initialized as in hand.
- Two instances may reference one definition without either instance mutating it.
- Runtime card state is represented with extensible orthogonal facts rather than a movement-card subclass or one catch-all lifecycle enum.
- Missing, duplicate, or unresolved IDs and invalid initial state are rejected by focused tests.
- No authoritative type depends on Unity objects, presentation, images, or printed-text parsing.

Non-goals: No action offers, card values, real Wasp data/images, state transitions, play/reveal flow, turns, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-110-separate-card-definitions-from-card-instances-completion-evidence).

### [x] 1.11 Represent action definitions, runtime offers, and typed values

Observable outcome: A small placeholder hand contains immutable Movement action definitions, and pure rules queries derive context-bound runtime offers with different typed printed values and explicit Primary/Secondary use.

Acceptance checks:

- A card definition owns ordered immutable action definitions rather than being a `MovementCard` subclass or exposing one context-free action field.
- Every action definition has a stable local ID, action kind, printed/general origin, base values, and explicit default card-text/repeat/replacement policy.
- A runtime action offer binds one definition to the current card instance, actor, availability, and invocation context without duplicating static content.
- The fixtures derive at least one printed Primary and one printed Secondary Movement offer so card selection and action selection cannot be conflated.
- Movement allowance is obtained through a typed value query for the chosen runtime offer, not a scene/UI constant, raw card-wide field, or number parsed from text.
- The value query returns an inspectable printed-value contribution and total even though modifiers are not introduced yet.
- The model has explicit vocabulary for printed Primary, printed Secondary, general-rule Secondary, replacement, reaction, repeated, and text-granted contexts; no offer resolves in this milestone.
- Invalid/duplicate definition IDs, unsupported values, and invalid origin/policy combinations are rejected by focused tests.
- Card images remain optional presentation and never authoritative data.

Non-goals: No Wasp content, initiative, card color/tier, printed card-text effects, action execution, card-state transitions, or turns.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-111-represent-action-definitions-runtime-offers-and-typed-values-completion-evidence).

### [x] 1.12 Add a resumable card-action resolution shell

Observable outcome: Pure rules code can start one selected placeholder Movement offer in its explicit Primary or Secondary context, produce an authoritative pending movement choice, accept or reject a follow-up choice command, and resume until the action completes.

Acceptance checks:

- The pending resolution records stable source-card instance, action-offer, actor, invocation-context, and current-step identifiers as plain data.
- A pending choice identifies its chooser and exact legal decision without delegates, closures, iterators, Unity coroutines, or UI-owned continuation state.
- Each chosen adjacent step reuses the validated movement/session rules from milestones 1.8-1.9 and the command/event authority from milestone 1.5 rather than duplicating them.
- Stopping at zero or before the allowance is exhausted completes legally; spending the allowance completes automatically.
- An invalid or stale choice does not mutate state or advance the continuation.
- Replaying the same starting state and accepted command/choice sequence produces the same state and ordered events.

Non-goals: No general-purpose scripting language, Wasp text, modal/repeat behavior, reactions, timing effects, card lifecycle transitions, turns, snapshots, UI hand, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-112-add-a-resumable-card-action-resolution-shell-completion-evidence).

### [x] 1.12a Harden the card-foundation authority and continuation contracts

Observable outcome: The completed milestones 1.10-1.12 foundation rejects caller-forged action authority, keeps immutable content and choices externally immutable, scopes offers by invocation window, and can resume a pending Movement action from its plain continuation data.

Acceptance checks:

- Starting resolution re-derives the exact offer from current definitions, instances, ownership, location, played state, action origin, actor, and invocation context.
- Forged, stale, mismatched-context, or otherwise unavailable offers reject without creating pending state; an actor absent from authoritative occupancy rejects structurally instead of throwing.
- Static action definitions reject invalid enum/policy states, printed/general Secondary and replacement definitions suppress source-card text by default, and repeat remains an invocation context rather than a static origin.
- Normal play, general-rule, replacement, reaction, and text-granted offers are derived only in their requested timing window.
- Definition actions, base values, value contributions, legal destinations, route data, and committed movement events cannot be mutated through exposed collection interfaces.
- Pending Movement data contains route, committed adjacent-move events, starting/remaining allowance, and completion position; a fresh authority validates it against board occupancy and resumes deterministically.

Non-goals: No milestone 1.13 UI, Wasp content, action text/effects, card lifecycle transition, modal/repeat execution, reaction resolution, timing engine, turn, snapshot import/export, networking, or presentation work.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-112a-harden-the-card-foundation-authority-and-continuation-contracts-completion-evidence).

### [x] 1.12b Make card-resolution Movement a one-click destination choice

Observable outcome: A pending placeholder Movement action offers every currently reachable destination with its exact deterministic route and cost, then one destination command commits that route and completes the `up to` action.

Acceptance checks:

- Pending Movement choices expose immutable, unique destination/route/cost candidates for the complete remaining allowance rather than adjacent coordinates only.
- Each candidate route starts at the authoritative actor coordinate, ends at its destination, contains only adjacent steps, and costs no more than the remaining allowance.
- A destination command re-derives the exact candidates against current occupancy and obstacles before accepting the selected destination.
- The selected route is validated without side effects, then every step commits through `MovementSession` and `AdjacentMoveAuthority` and emits its ordered movement event.
- Selecting any reachable destination implicitly stops and completes the `up to` Movement action; Stop still completes with zero movement.
- Blocked destinations and intermediate spaces are excluded, deterministic legal detours remain selectable, and a changed board rejects without partial actor movement.
- Invalid, stale, forged, malformed, or restored-mismatch choices do not advance the continuation; identical starting state and command replay produce identical final occupancy and ordered events.

Non-goals: No Unity card UI, visible hand, milestone 1.13 presentation hookup, alternate/non-shortest route selection, Fast Travel, movement from card text, movement-limit effects, concrete terrain/tokens/minions, Wasp content, turns, networking, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-112b-make-card-resolution-movement-a-one-click-destination-choice-completion-evidence).

### [x] 1.13 Select a test card and perform card-driven movement

Observable outcome: A visible placeholder hand lets the tester select one card and one available Movement offer, then complete the resumable card-driven movement on the expanded obstacle map.

Acceptance checks:

- The movement allowance comes only from the selected action offer's structured printed value.
- A value-1 offer reproduces one-adjacent-space behavior and a higher value offers all corresponding reachable destinations with their projected routes.
- Primary/Secondary context remains visible to the authority and the Secondary offer does not accidentally opt into Primary-text behavior.
- The tester may move zero spaces with Stop or stop early by selecting a destination whose route costs less than the allowance.
- Obstacles block entry and passage exactly as in milestone 1.9.
- Card, action, and pending-choice selection are visually identifiable and clear when resolution ends.
- UI input submits commands and answers pending choices; it never owns card resolution or mutates occupancy.
- Focused tests cover both values, early stopping, stale/invalid choices, over-movement rejection, and obstacle interaction.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No real Wasp cards, printed card text, initiative, facedown/reveal behavior, lifecycle transitions beyond the initial hand state, turns, rounds, Fast Travel, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-113-select-a-test-card-and-perform-card-driven-movement-completion-evidence).

### [x] 1.13a Keep partial Movement pending and build cumulative waypoint routes

Observable outcome: If a Movement 3 action commits a route costing 2, the same action remains pending with 1 allowance and offers another destination choice or Stop from the hero's new authoritative position.

Acceptance checks:

- A destination route that costs less than the remaining allowance commits its adjacent steps but does not complete the Movement action.
- The continuation preserves the resolution/source/action/actor/context, advances its step/choice ID, accumulates the full route and ordered events, and emits another choice request without emitting another action-start event.
- Legal destination candidates and highlights are recalculated from the new authoritative occupancy using only the remaining allowance.
- Waypoint choices can form a deliberately longer cumulative route, including revisiting a previously occupied coordinate, without enumerating multiple complete routes for one destination.
- Stop completes at any point with unused allowance; exhausting the allowance completes automatically.
- An old choice becomes stale after a partial segment, and restored pending state with committed steps resumes deterministically.
- Presentation retains the selected card/action between partial segments and clears them only when resolution ends.
- T3 corrective phase gate: focused affected Edit/Play checks and both complete unfiltered suites pass.

Non-goals: No official full map, concrete movement-limiting effects, Fast Travel, direct card-text Move, real Wasp data/text, play/reveal, lifecycle transitions, turns, networking, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_1.md#milestone-113a-keep-partial-movement-pending-and-build-cumulative-waypoint-routes-completion-evidence).
