# Guards Digital - Roadmap

Last updated: 2026-08-09

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

This roadmap is a sequence of micro-milestones, not authorization to execute them.

For every item:

1. Obtain explicit user approval for that item.
2. Implement only its stated observable outcome.
3. Respect its non-goals.
4. Run the milestone's focused tests, directly affected regressions, and highest applicable validation tier from `Docs/AI/TESTING.md`.
5. Update the project documents.
6. Create a local Git commit containing the completed milestone and continuity updates.
7. Report the result and stop.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

Acceptance checks define what must be proved; `Docs/AI/TESTING.md` defines the required breadth. Unless a milestone is an explicit gate or qualifies as broad risk, completion does not require both unfiltered Unity suites.

Every card-related milestone must preserve the contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. A narrow milestone implements only the required subset of those contracts; it must not introduce a shortcut that the next card milestone is expected to replace.

## Phase 0 - Shared context and project shell

### [x] 0.1 Establish living documentation

Observable outcome: Future tasks have a stable entry point, agreed product context, a proposed roadmap, and rule traceability structure.

Artifacts:

- `AGENTS.md`
- `Docs/AI/PROJECT_CONTEXT.md`
- `Docs/AI/ROADMAP.md`
- `Docs/AI/RULES_LEDGER.md`

Non-goals: No Unity project, code, assets, or builds.

### [x] 0.2 Create the empty Unity project

Observable outcome: An empty 3D Unity project opens in installed editor `6000.5.7f1` and contains no gameplay.

Acceptance checks:

- Exact editor version is recorded by Unity project metadata.
- Project opens without compilation errors.
- Windows is the active reference build platform.
- Input System choice is recorded.
- A clean version-control ignore policy is present if version control has been authorized.

Non-goals: No board, camera system, game state, card data, or production packages.

Completion note (2026-08-09): Created and batch-opened the empty project with Unity `6000.5.7f1`; selected Win64 as the reference build target; installed Unity Input System `1.19.0` and selected its new-only input backend. Git was authorized later that day; the local `main` repository now includes root `.gitignore` and `.gitattributes` policies, satisfying the conditional version-control check. No gameplay, board, camera, state, or card content was added.

### [x] 0.3 Add the automated test shell

Observable outcome: One trivial Edit Mode test and one minimal Play Mode smoke test can be run independently.

Acceptance checks:

- Focused test commands are documented.
- Tests do not require a full build or a complete game scene.

Non-goals: No game-rule tests yet.

Completion note (2026-08-09): Added Unity Test Framework `1.7.0`, isolated Edit Mode and Play Mode test assemblies, one trivial Edit Mode test, and one Play Mode one-frame smoke test. Each fully qualified test was run independently in batch mode and passed 1/1. Commands and output paths are documented in `Docs/AI/TESTING.md`. Neither test requires a player build, a saved scene, or game-rule code.

Repository note (2026-08-09): Git was authorized after milestone 0.3. The completed 0.1-0.3 baseline is captured together in the initial local repository commit; later roadmap milestones receive individual completion commits.

### [x] 0.4 Establish the card-system architecture contract

Observable outcome: Future card work has a rulebook-grounded implementation contract that separates immutable content from runtime state and defines extensible action, choice, effect, targeting, timing, event, validation, and versioning boundaries.

Artifacts:

- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`
- Aligned card sequencing in `Docs/AI/ROADMAP.md`
- Aligned continuity, source coverage, and milestone-test guidance in `AGENTS.md`, `Docs/AI/PROJECT_CONTEXT.md`, `Docs/AI/RULES_LEDGER.md`, and `Docs/AI/TESTING.md`

Acceptance checks:

- The contract accounts for Primary, Secondary, replacement, and reaction contexts.
- The contract accounts for ordered mandatory/optional steps and serializable pending choices.
- The contract accounts for overlapping card-state facts, typed targets/values, staged and simultaneous effects, Active effects, continuous rules sources, and choice-aware timing.
- The contract defines a controlled extension path for ordinary and genuinely unique future cards.
- Early card and combat milestones prove the architecture in small slices before verified Wasp content is added.

Non-goals: No card code, Wasp inventory, corpus-wide effect taxonomy, Unity content, or gameplay implementation.

Completion note (2026-08-09): Reviewed the second-printing rulebook's complete general card/action rules on printed pages 7-19 and documented the resulting architecture and implementation gates. The design is intended to localize future extensions, not to claim that unknown cards can be implemented without any new typed primitive or isolated handler.

### [x] 0.5 Establish the reusable card-effect capability catalog

Observable outcome: Future cards can reference a source-traceable catalog of typed mutation, query, modifier, control-flow, timing, persistent-source, and system-transition capabilities instead of adding ad hoc effect logic.

Artifacts:

- `Docs/AI/CARD_EFFECT_CAPABILITIES.md`
- Aligned effect gates in `AGENTS.md`, `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`, `Docs/AI/PROJECT_CONTEXT.md`, `Docs/AI/ROADMAP.md`, and `Docs/AI/RULES_LEDGER.md`

Acceptance checks:

- The catalog defines a mandatory registration contract covering roles, parameters, choice ownership, optionality/failure, atomicity, state changes, checkpoints, events, continuation data, validation, and tests; concrete schemas are instantiated only when a verified milestone registers that capability/version.
- Stable effect-node IDs and typed input/outcome bindings preserve actual results for later conditional steps and pending-resolution serialization.
- Object Push, source-relative direct Move, Movement action, Place, Spawn, Respawn, Swap, Replace, Remove, Defeat, Clear, tokens, and markers have distinct contracts.
- Rulebook query/modifier concepts including adjacency, Range/Radius, direction/straight line, distance ordering, zones, targeting versus presence, immunity, `Counts As`, obstacles, and effective type have explicit owners.
- Card-state, value/resource, timing, Active-effect, continuous-source, control-flow, and system-transition capabilities are covered.
- Card text such as `move a unit towards you` is represented by direct Move plus source-relative destination constraints rather than an invented Pull keyword/capability.
- New verified effects have a documented extension and source-linked testing process.

Non-goals: No effect handlers, Wasp inventory, card corpus taxonomy, Unity content, or gameplay implementation.

Completion note (2026-08-09): Re-audited printed rulebook pages 7-19 and expanded the card foundation into an explicit reusable effect/keyword capability catalog. The catalog covers the general rules vocabulary and deliberately leaves hero-specific semantics to verified card inventory.

### [x] 0.6 Re-sequence development around the core match loop

Observable outcome: The roadmap proves a deterministic match with synthetic test cards before the complete Wasp set is inventoried or implemented.

Acceptance checks:

- Configurable multi-space movement is followed immediately by obstacle blocking in milestones 1.8-1.9.
- Test cards prove Tie Breaker state, Initiative ordering, play/reveal, turns, and the four-turn round structure in Phases 2-3.
- The full official map and generic core systems for combat, minions, Lane Push, end-of-round ordering, economy, progression, respawn, and victory are completed in Phases 4-7.
- A deterministic test-card core match is a hard gate before the complete Wasp card inventory or Wasp-specific card implementation begins in Phase 8.
- The Wasp capability matrix identifies Wasp-specific gaps and refinements; it no longer owns or delays general core-rule capabilities.

Non-goals: No gameplay code, Wasp inventory, Unity content, or implementation milestone is authorized by this planning change.

Completion note (2026-08-09): Reordered the roadmap so initiative, Tie Breaker behavior, turns, rounds, and the full synthetic core loop precede the complete Wasp content pass while preserving the smallest-step approval gates.

### [x] 0.7 Adopt proportional risk-tiered validation

Observable outcome: Every milestone uses the smallest test set that proves its behavior and protects affected contracts, while complete Unity suites remain mandatory at integration gates and for broad-risk changes.

Acceptance checks:

- Documentation/source-only changes use diff, reference, milestone-ID/status, traceability, and authority-consistency checks without launching Unity.
- Every executable behavior change has focused automated tests plus directly affected regressions; every bug fix adds a regression test.
- Play Mode and visual checks run only when a Unity-facing path or visible output changes.
- Unfiltered Edit Mode and Play Mode suites run at implementation-phase endpoints, explicit hard gates, before the first player build, after shared test/package/assembly infrastructure changes, or when affected consumers cannot be confidently enumerated.
- Rule-dependent work verifies its authority first and records source-linked scenarios; unresolved ambiguity blocks completion.
- Completion evidence records the chosen tier, exact checks/results, and why any unaffected suite was omitted.

Non-goals: No gameplay, test-runner code, Unity content, source transcription, or build is added by this documentation-only policy milestone.

Completion note (2026-08-09): Replaced the ambiguous requirement to run both complete Unity suites at every micro-milestone with four validation tiers, explicit integration gates, and objective escalation triggers. Focused coverage remains mandatory for executable work; documentation-only checks, targeted visual review, source validation, and build smoke testing now have distinct owners.

## Phase 1 - Smallest board interaction

### [x] 1.1 Define hex coordinates and adjacency

Observable outcome: Pure C# code represents a hex and returns its six neighbors.

Acceptance checks:

- Six unique neighbor directions are tested.
- No Unity scene object is required by the tests.

Non-goals: No rendering, pathfinding, zones, terrain, or units.

Completion note (2026-08-09): Added a no-engine-reference `GuardsDigital.Core` assembly with immutable axial `(q, r)` hex coordinates, six named directions, single-neighbor lookup, and six-neighbor enumeration. Eight focused Edit Mode cases verify every direction, uniqueness and completeness of the neighbor set, and invalid-direction rejection. The complete Edit Mode suite passed 9/9 and the Play Mode suite passed 1/1. No rendering, pathfinding, zones, terrain, units, or scene objects were added.

### [x] 1.2 Render a minimal connected test grid

Observable outcome: A small set of flat placeholder hex meshes appears on the XZ plane under a fixed camera.

Acceptance checks:

- Visual hex positions correspond to logical coordinates.
- No overlaps or gaps caused by coordinate conversion.

Non-goals: No official map, camera movement, selection, or rules.

Completion note (2026-08-09): Added a Unity-specific presentation assembly with pointy-top axial-to-XZ conversion, a regular flat placeholder hex mesh, and coordinate-bearing tile views. The `Milestone1_2_HexGrid` scene renders the origin and its six logical neighbors edge-to-edge under one fixed camera centered directly above the map; one non-semantic light-gray fill and darker gray closed perimeters distinguish tiles without adding layout gaps or uniquely styling the origin. Twelve focused Edit Mode cases verify conversion, geometric center spacing, mesh construction, saved-scene coordinates, uniform fill, darker closed edges, and the fixed top-down camera constraint, while one focused Play Mode case verifies the scene loads with seven visible tile fills and outlines. Visual inspection passed; initial milestone-boundary suites passed Edit Mode 19/19 and Play Mode 2/2, and the final top-down camera regression fixture passed 4/4. No official map data, camera movement, selection, units, or rules were added.

### [x] 1.3 Place one primitive hero

Observable outcome: One placeholder hero occupies one logical hex and appears on that hex.

Acceptance checks:

- Occupancy exists in authoritative data.
- Visual placement is derived from authoritative data.

Non-goals: No teams, movement, animation, cards, or turns.

Completion note (2026-08-09): Added a no-engine-reference primitive hero occupancy model with a stable placeholder hero ID and occupied axial coordinate. Added a Unity presentation view that derives its local position from that occupancy through the existing pointy-top hex layout and builds a simple placeholder mesh at runtime. The validation scene now contains one primitive hero on the origin hex while preserving the seven-tile minimal grid. Focused Edit Mode tests passed 10/10 and focused Play Mode tests passed 2/2. No teams, movement, animation, cards, turns, or gameplay resolution were added.

### [x] 1.4 Select the hero and one adjacent destination

Observable outcome: Pointer input can select the hero and highlight valid adjacent empty hexes.

Acceptance checks:

- Only adjacent empty hexes are highlighted.
- Selection is usable with Unity's pointer abstraction.

Non-goals: The hero does not move yet.

Completion note (2026-08-09): Added a pure adjacent-empty destination query and Unity presentation selection path. The validation scene now uses the Unity Input System pointer abstraction (`<Pointer>/press` and pointer position raycast) to select the primitive hero and highlight only adjacent empty tiles; the current seven-hex scene therefore highlights all six neighbors and not the occupied origin. Focused Edit Mode tests passed 13/13, focused Play Mode tests passed 3/3, and milestone-boundary suites passed Edit Mode 35/35 and Play Mode 4/4. No movement command, occupancy mutation, rule event, teams, cards, turns, or gameplay resolution was added.

### [x] 1.5 Commit one adjacent move through rules authority

Observable outcome: Pure rules code accepts an adjacent-move intent command, validates it, commits authoritative occupancy, and returns an ordered movement rule event.

Acceptance checks:

- The command refers to stable hero identity and explicit origin/destination coordinates.
- A legal adjacent move makes the origin empty and destination occupied.
- An invalid or stale command returns a structured rejection, changes nothing, and emits no accepted state-change event.
- The accepted event contains rules facts only and no animation or scene instruction.
- Focused pure-rule tests cover legal, non-adjacent, occupied, wrong-origin, and unknown-hero requests.

Non-goals: No pointer wiring, presentation movement, movement range, cards, turns, networking, or animation.

Completion note (2026-08-09): Added a pure `AdjacentMoveAuthority` with `AdjacentMoveCommand`, structured rejection reasons, and rules-only `AdjacentMoveEvent` output. A legal adjacent command updates the copied authoritative occupancy list and emits one accepted movement event; unknown-hero, wrong-origin/stale, non-adjacent, occupied-destination, and off-board commands reject without changing occupancy or emitting accepted events. Focused pure-rule Edit Mode tests passed 6/6, and milestone-boundary suites passed Edit Mode 41/41 and Play Mode 4/4. No pointer wiring, presentation movement, movement range, cards, turns, networking, or animation was added.

### [x] 1.6 Connect adjacent selection to the committed move

Observable outcome: Clicking a highlighted destination submits the milestone 1.5 command, then the primitive hero view snaps to the position derived from updated authoritative occupancy and the emitted event.

Acceptance checks:

- UI/pointer code never assigns occupancy directly.
- A rejected command leaves selection and presentation consistent with unchanged authoritative state.
- A legal move clears obsolete highlighting and renders the hero on the authoritative destination.
- Focused Edit Mode and Play Mode tests cover valid and invalid pointer-to-command flows.

Non-goals: No multi-space movement, card, turn, path automation, networking, or animation.

Completion note (2026-08-09): Connected the scene selection flow to the milestone 1.5 adjacent-move authority. Pointer input first tries to select the hero, then treats non-hero tile hits as destination intents; the selection controller submits an `AdjacentMoveCommand`, updates the hero view only after an accepted authority result, and leaves selection/highlights unchanged on rejected commands. T2 validation passed: focused Edit Mode 11/11 and focused Play Mode 3/3. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected Unity-facing path is covered by the focused Play Mode scene test. No multi-space movement, card, turn, path automation, networking, or animation was added.

### [x] 1.7 Expand the placeholder test grid

Observable outcome: The scene contains a larger connected placeholder grid with enough space to test movement of several steps, alternate routes, and later obstacle placement.

Acceptance checks:

- The grid contains pairs of spaces at logical distances 1, 2, and 3 or greater.
- At least one pair of spaces has more than one valid route between it.
- Every rendered center still corresponds to its authoritative axial coordinate.
- Existing seven-hex rendering and occupancy behavior remain valid.

Non-goals: No official map data, movement-rule change, obstacle, zone, terrain, card, turn, or camera movement.

Completion note (2026-08-09): Added a presentation-owned radius-2 placeholder grid expander to the existing validation scene, producing 19 rendered axial spaces while preserving the original origin-plus-six-neighbor cluster, primitive hero at the origin, and adjacent click-to-move behavior through the existing adjacent-move authority. Focused T2 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 19/19 (`MinimalHexGridSceneTests`, `HeroSelectionControllerTests`, `AdjacentMoveAuthorityTests`) and Play Mode 3/3 (`MinimalHexGridPlayModeTests`). Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the changed Unity-facing scene/controller path is covered by the focused Edit Mode and Play Mode tests. No official map data, movement-rule change, obstacle, zone, terrain, card, turn, camera movement, networking, or animation was added.

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

Completion note (2026-08-09): Added a pure `MovementSession` wrapper around the milestone 1.5 adjacent-move authority. Sessions track configured allowance, remaining allowance, explicit route, and the ordered accepted adjacent-move events; rejected or over-allowance steps do not mutate occupancy or add events. The validation scene now starts a default allowance-3 session on primitive hero selection, highlights every empty destination reachable within the remaining allowance, commits the deterministic shortest adjacent route to the clicked destination through the same authority, ends after that one destination click, and stops with zero movement when the tester clicks the selected hero again before choosing a destination. Focused T2 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 29/29 (`MovementSessionTests`, `MovementReachabilityQueryTests`, `HeroSelectionControllerTests`, `AdjacentMoveAuthorityTests`, `MinimalHexGridSceneTests`) and Play Mode 3/3 (`MinimalHexGridPlayModeTests`). Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected pure/session/controller/scene path is covered by the focused tests. No cards, turns, Fast Travel, movement from card text, movement-limiting effects, obstacles, networking, or animation was added.

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

Completion note (2026-08-09): Added pure `ObstacleId` and `ObstacleOccupancy` data plus generic obstacle coordinates in the expanded placeholder grid. `AdjacentMoveAuthority` now rejects occupied destinations when either a unit or placeholder obstacle occupies the space and rejects initial states where an obstacle is off-board, duplicates another obstacle coordinate, or shares a coordinate with a unit. `MovementReachabilityQuery` now treats obstacle coordinates as blocked, excluding blocked destinations and any route that would pass through them while preserving deterministic shortest detours when allowance permits. The validation scene generates two visible placeholder blocker meshes at `(1, 0)` and `(0, 1)`, and the hero selection controller passes those obstacle occupancies into both reachability and route execution. Focused T2 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 42/42 (`ObstacleOccupancyTests`, `PlaceholderObstacleViewTests`, `AdjacentMoveAuthorityTests`, `MovementReachabilityQueryTests`, `MovementSessionTests`, `HeroSelectionControllerTests`, `MinimalHexGridSceneTests`) and Play Mode 3/3 (`MinimalHexGridPlayModeTests`). Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected pure/session/controller/scene path is covered by the focused tests. Targeted presentation verification was covered by scene and Play Mode assertions for visible obstacle renderers and highlighted destination exclusion. No ignore-obstacle behavior, obstacle subtypes, tokens, terrain art/data, other units, path automation, cards, turns, Fast Travel, networking, or animation was added.

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

Completion note (2026-08-09): Added a no-engine-reference `GuardsDigital.Core.Cards` foundation with stable `CardDefinitionId`, `CardInstanceId`, and definition references; immutable placeholder `CardDefinition` data; runtime `CardInstance` data owned by the primitive test hero; and orthogonal `CardStateFacts` for location, played status, resolution status, and face orientation. Initial setup validation accepts unique in-hand instances, including multiple instances referencing one immutable definition, and rejects blank identity/revision/name data, duplicate definition IDs, duplicate instance IDs, stale or unresolved definition references, and invalid initial runtime state. Focused T1 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 12/12 (`CardDefinitionInstanceTests`). Play Mode and complete unfiltered suites were intentionally omitted because this is not a T2/T3 gate and no Unity-facing path, scene, input, presentation, package, assembly, or snapshot schema changed. No action offers, card values, real Wasp data/images, card-state transitions, play/reveal flow, turns, networking, Unity presentation, or animation was added.

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

Completion note (2026-08-09): Extended pure placeholder card definitions with ordered immutable `ActionDefinition` data, stable local action IDs, `ActionKind`, printed/general origin vocabulary, typed printed value definitions, and explicit default card-text/repeat/replacement policy. Added runtime `ActionOffer` and `ActionOfferQuery` projection for in-hand owned card instances, deriving separate printed Primary and printed Secondary Movement offers without mutating instances or copying static values into the offer. Added `PrintedValueQuery` returning printed-icon contribution provenance and totals for the chosen offer, proving distinct Movement allowances for Primary and Secondary offers. Focused T1 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 23/23 (`ActionOfferAndPrintedValueTests`; directly affected `CardDefinitionInstanceTests`). Play Mode and complete unfiltered suites were intentionally omitted because this is not a T2/T3 gate and no Unity-facing path, scene, input, presentation, package, assembly, snapshot schema, action execution, or resolver path changed. No Wasp content, initiative, card color/tier, printed card-text effects, action execution, card-state transitions, turns, networking, Unity presentation, or animation was added.

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

Completion note (2026-08-09): Added a pure `CardActionResolutionAuthority` for placeholder Movement offers. It starts a selected printed Primary or printed Secondary Movement offer, records `PendingCardActionResolution` and `PendingMovementChoice` data with stable source-card, action, actor, invocation-context, resolution, chooser, and step IDs, derives legal adjacent step choices through the existing movement reachability query, and commits accepted step choices through `MovementSession` and `AdjacentMoveAuthority`. Stop choices complete legally at zero movement or before spending the full allowance, spending the allowance completes automatically, and invalid or stale choices reject without changing occupancy or advancing the pending continuation. Focused T2 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 39/39 (`CardActionResolutionAuthorityTests`; directly affected `ActionOfferAndPrintedValueTests`, `MovementSessionTests`, `MovementReachabilityQueryTests`, and `AdjacentMoveAuthorityTests`). Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing scene/input/presentation path changed and this is not the Phase 1 T3 gate. No general-purpose scripting language, Wasp text, modal/repeat behavior, reactions, timing effects, card lifecycle transitions, turns, snapshots, UI hand, networking, or animation was added.

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

Completion note (2026-08-09): Hardened the pure card/action foundation after reviewing the complete local hero-action front corpus for architectural fit. Runtime offers are now explicitly timing-window scoped with non-ordinal origin/context mapping; static repeat origins were removed; definition policies and enum values are validated; and all exposed immutable collections use read-only wrappers. `CardActionResolutionAuthority` now requires current card instances, re-derives the exact normal-play offer, rejects forged/stale/context-mismatched authority and missing actor occupancy, and stores all Movement continuation facts in immutable `MovementSessionState`. Each follow-up reconstructs from that state, while `Restore` validates the pending resolution and exact legal choice against restored board occupancy. Focused T2 validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 55/55 (`CardActionResolutionAuthorityTests`, `ActionOfferAndPrintedValueTests`, `CardDefinitionInstanceTests`, `MovementSessionTests`, `MovementReachabilityQueryTests`, and `AdjacentMoveAuthorityTests`). Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path, package, assembly, or phase gate changed and the affected pure-rule consumers were enumerated. Milestone 1.13 was not started.

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

Completion note (2026-08-09): Replaced adjacent step/stop card-resolution choices with immutable reachable destination/route/cost candidates. `CardActionResolutionAuthority` now re-derives the exact candidate set against current occupancy, validates the selected route on a side-effect-free authority copy, commits every internal adjacent step through the existing movement authority, emits each ordered step event, and completes after the one destination command even when allowance remains. `ReachableMovementDestination` and reachability query results are externally read-only and reject malformed route data. T2 focused validation passed after allowing Unity batchmode access to local licensing/package services: Edit Mode 70/70 (`CardActionResolutionAuthorityTests`, `ActionOfferAndPrintedValueTests`, `CardDefinitionInstanceTests`, `MovementSessionTests`, `MovementReachabilityQueryTests`, `AdjacentMoveAuthorityTests`, and `HeroSelectionControllerTests`). Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path, package, assembly, snapshot schema, or phase gate changed and every affected pure/query/presentation consumer was enumerated. Milestone 1.13 was not started.

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

Completion note (2026-08-09): Added two visibly test-only placeholder Movement cards to the expanded obstacle-map scene. Card selection and action selection remain separate; each card derives a printed Primary Movement 3 offer with card-text application and a printed Secondary Movement 1 offer with card text suppressed. The presentation bridge starts `CardActionResolutionAuthority`, highlights its exact pending destination/route/cost candidates, submits Stop or destination commands, updates the primitive hero only from accepted authoritative occupancy, and clears selected card/action/pending visuals on completion. UI clicks over the card panel are blocked from leaking into board input. Re-reviewed rulebook printed pages 7, 10, and 11 for one-action selection, Primary/Secondary text policy, optional `up to` Movement, and obstacle blocking. T3 validation passed after allowing Unity batchmode access to local licensing/package services: focused Edit Mode 41/41 (`CardDrivenMovementPresentationTests`, `CardActionResolutionAuthorityTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`); focused Play Mode 3/3 (`MinimalHexGridPlayModeTests`); targeted offscreen scene render 1/1 with readable hero/obstacles and correct pending Primary-value-3 destination highlights; complete unfiltered Edit Mode 114/114; and complete unfiltered Play Mode 4/4, all with zero skipped or inconclusive cases. No real Wasp content, printed card-text effects, play/reveal, lifecycle transition, initiative, turn/round, Fast Travel, alternate/non-shortest route selection, snapshot import/export, networking, or animation was added.

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

Completion note (2026-08-09): Changed `CardActionResolutionAuthority` so a partial destination advances the existing pending Movement resolution instead of implicitly stopping it. The authority re-derives candidates from the new occupancy, preserves cumulative `MovementSessionState` route/events, issues a new step-scoped choice, accepts Stop with unused allowance, and completes automatically only when the allowance is exhausted (or no continuation destination exists). `Restore` now accepts and validates pending continuations with already committed steps. The visible controller retains card/action selection, accumulates route presentation, refreshes highlights/status after each segment, and clears only on completion. Re-reviewed rulebook printed page 11: Movement is optional up to the printed value, obstacles block entry/passage, and movement-limiting effects are checked only once at action start. T3 validation passed: focused Edit Mode 42/42 (`CardDrivenMovementPresentationTests`, `CardActionResolutionAuthorityTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`); focused Play Mode 4/4 (`MinimalHexGridPlayModeTests`); complete Edit Mode 115/115; and complete Play Mode 5/5, all with zero skipped or inconclusive cases. No milestone 2.1 or later gameplay was started.

## Phase 2 - Two-hero combat and Tie Breaker state

### [x] 2.1 Introduce teams, players, heroes, and stable IDs

Observable outcome: The model supports collections of teams, players, and heroes while the test configuration creates one Red and one Blue hero.

Acceptance checks:

- No rule API assumes exactly one hero per team.
- Team, player, and hero IDs are nonempty, unique, and resolve through collections.
- The existing primitive hero ID remains stable, and Phase 1 card-instance owner references still resolve after the state is expanded to Red/Blue teams and players.

Non-goals: No 3v3 UI or multiplayer behavior.

Completion note (2026-08-09): Added pure immutable `Team`, `Player`, and `Hero` participant records with typed nonempty IDs, validated collection uniqueness, collection-backed relationship resolution, and plural player/hero queries by team. The synthetic 1v1 configuration creates one Red player with the existing stable `primitive-hero` and one Blue player with the new stable `blue-test-hero`. Each hero owns a separate two-card copy of the Phase 1 synthetic Movement hand; all four runtime card-instance IDs are globally unique, their shared definition references validate, and every owner ID resolves to the correct hero/player/team. A four-player synthetic fixture proves team APIs return multiple heroes rather than assuming one hero per team. The validation scene now places both authoritative test occupancies on adjacent hexes with visibly distinct Red/round and Blue/square primitives. The Red controller passes both occupancies to the existing adjacent-move and reachability authority, so Blue's occupied hex blocks movement without any new presentation-owned blocking rule. Visually reviewed rulebook printed pages 3 and 5-6 for one hero per player, opposing Red/Blue teams, equal-team setup, and per-player hero cards; the pitch still applies DEV-001 and DEV-003. T2 focused validation passed after the scene correction: Edit Mode 36/36 (`PrimitiveHeroViewTests`, `HeroSelectionControllerTests`, `MinimalHexGridSceneTests`, `CardDrivenMovementPresentationTests`, and `MatchParticipantsTests`); Play Mode 4/4 (`MinimalHexGridPlayModeTests`); and one 1024x768 offscreen scene render visually confirmed both colored/shaped miniatures plus the existing obstacles, all with zero skipped or inconclusive cases. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected participant/view/controller/scene path is covered by focused tests. No 3v3 UI, multiplayer behavior, Blue-side scene interaction, Attack/Defense offers, combat, turns, networking, or animation was added.

### [ ] 2.2 Extend the test cards with Attack and Defense action offers

Observable outcome: The Phase 1 placeholder definitions gain non-Ranged Attack and Defense-reaction action definitions, from which the authority derives typed runtime offers owned by the correct test heroes.

Acceptance checks:

- Test-card Attack and Defense values are typed printed values on action definitions and are queried through runtime offers, not UI or scene constants.
- The Attack card has a printed Primary, non-Ranged Attack definition that derives the eligible runtime offer.
- A defending card definition declares an eligible Defense reaction from hand and discard as its cost/state transition, without executing that reaction yet or pretending it is a normal turn action.
- Every card used to defend is associated with exactly one hero's hand.
- The minimal card-state facets support in-hand and discarded locations without pretending to implement the complete turn lifecycle.
- Invalid Attack/Defense values, missing owners, and cross-owner card use are rejected by focused tests.

Non-goals: No Wasp data or text, ranged attacks, card tiers/colors, initiative, items, card effects, Block, turns, or animations.

### [ ] 2.3 Initiate a basic adjacent hero Attack

Observable outcome: One test hero can select its non-Ranged Attack offer and target the adjacent enemy test hero through the shared command and resumable-resolution boundary.

Acceptance checks:

- Only an adjacent enemy hero is a legal target.
- The attacker cannot target itself, a friendly hero, a non-adjacent hero, an empty space, terrain/token, or another non-unit obstacle; heroes/minions remain obstacles for occupancy without ceasing to be unit targets.
- The initial Attack Total comes through the value-query service and equals the selected offer's printed Attack contribution when no modifiers exist.
- Validation failure does not mutate state or consume a card.
- The resolver opens an explicit Defense response choice owned by the targeted hero and emits an ordered Attack-started event for presentation.

Non-goals: No ranged targeting, minions, items, modifiers, Before/After Attack text, Wasp effects, Defense-card selection/resolution, or animation.

### [ ] 2.4 Defend with one test card or decline

Observable outcome: The targeted hero answers the pending Defense reaction by discarding exactly one legal card from its own hand for its Defense value or declining, after which the Attack resolution resumes.

Acceptance checks:

- A Defense card can be chosen only from the targeted hero's current hand.
- Exactly one chosen card moves from its hand location to its discard location while retaining the correct orthogonal state facts.
- A discarded card and an attacker's selected card cannot be reused as the Defense card.
- Declining, having no card, or lacking a legal card produces no Defense Total.
- With no modifiers, the value-query service reports only the discarded card's printed Defense contribution and the Defense Total equals it.
- Defense does not count as the defender's normal turn action, and its action context remains distinct from a played Primary/Secondary action.
- Invalid or stale response commands leave the card and pending Attack unchanged.
- Focused tests reject cross-owner, already discarded, missing, and multiple-card Defense attempts.

Non-goals: No primary Defense card text, items, minion modifiers, Block, Defense/Skill behavior, defeat consequences, turns, or animation.

### [ ] 2.5 Compare Attack and Defense and report the test outcome

Observable outcome: The authority completes the basic test Attack/Defense sequence and reports either successfully defended or attack successful.

Acceptance checks:

- Defense Total equal to or greater than Attack Total successfully defends.
- Defense Total below Attack Total results in a successful attack.
- Declining or being unable to defend results in a successful attack.
- Resolution resumes the same pending Attack, closes its Defense window, and emits reproducible ordered events.
- The presentation clearly identifies the result without inventing Life, coin, respawn, or other deferred consequences.

Non-goals: A successful test attack does not yet implement full hero defeat, removal, unresolved-card resolution, active-effect cancellation, markers, coins, assists, Life spending, victory, or respawn.

### [ ] 2.6 Add Tie Breaker setup state

Observable outcome: Normal setup chooses the initial Tie Breaker side; internal tests can explicitly select Red or Blue.

Acceptance checks:

- The result is stored in authoritative state and included in later snapshot history.
- No general gameplay RNG subsystem is introduced.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the Phase 2 focused scenarios.

## Phase 3 - Test-card Initiative, turns, and rounds

### [ ] 3.1 Extend the test-card foundation with turn lifecycle

Observable outcome: Synthetic card instances can represent and transition through the lifecycle facts needed to play a turn without using Wasp data.

Acceptance checks:

- Location, played status, unresolved/resolved status, and face orientation remain orthogonal where the rulebook permits overlap.
- Pure-rule tests cover playing a card Facedown, revealing it, resolving it after an action, and discarding a Defense card from hand.
- Synthetic definitions include validated typed Initiative values, with fixtures for both unequal values and ties.
- Resolved status, discard location, and face orientation remain separate facts rather than mutually exclusive states.
- Test definitions are explicitly synthetic fixtures and cannot become authority for Wasp values or text.

Non-goals: No Wasp inventory, printed card effects, upgrades, items, or end-of-round subsystems.

### [ ] 3.2 Display both director-mode test hands

Observable outcome: Separate Red and Blue synthetic hands are visible and independently selectable by the director-mode controller.

Acceptance checks:

- Every displayed card resolves to one authoritative owned card instance.
- Team identity is not conveyed by color alone.
- Selecting a card does not play it or resolve an action.

### [ ] 3.3 Play and reveal one test card per side

Observable outcome: The controller selects and confirms one card for each side, both cards enter the correct played/unresolved state, and they reveal together.

Acceptance checks:

- Each side can commit only an eligible card from its own hand.
- Neither side's selection is revealed early.
- Reveal changes authoritative card state; it is not a presentation-only flip.
- Passing is represented when a side has no card available.

Non-goals: No Initiative ordering or action execution.

### [ ] 3.4 Resolve Initiative and tied Initiative

Observable outcome: The authority identifies the next unresolved actor from current Initiative values and applies the Tie Breaker rules without depending on Wasp cards.

Acceptance checks:

- Initiative comes through the typed value/contribution query rather than a turn-controller field or UI value.
- The highest current Initiative among unresolved cards is queried again after every resolution rather than cached as a complete order.
- A cross-team tie uses the current Tie Breaker side and records a deferred flip that occurs only after the selected tied resolver finishes.
- A same-team tie creates an authoritative pending choice for that team/controller and never flips the Tie Breaker.
- Synthetic multi-hero tests cover more than two tied cards even though the pitch setup uses one hero per team.

### [ ] 3.5 Resolve Hold and complete one turn

Observable outcome: Both unresolved test cards can perform Hold in Initiative order, become resolved, and end the turn.

Acceptance checks:

- Each hero performs no more than one normal action from its played card.
- A deferred cross-team Tie Breaker flip occurs only after the selected tied card resolves.
- The next actor is re-queried from current unresolved state.
- Turn-only effects have an explicit cancellation checkpoint even though no concrete effect is required yet.

### [ ] 3.6 Resolve test-card Movement within a turn

Observable outcome: A hero can choose its synthetic Movement offer during its Initiative position, complete the Phase 1 movement flow, resolve its card, and yield to the next actor.

Acceptance checks:

- Movement reuses the allowance, chosen-route, obstacle, command/event, and pending-choice authority from Phase 1.
- The turn controller does not mutate occupancy itself.
- Completing Movement resolves exactly the acting card and continues Initiative order.

### [ ] 3.7 Resolve test-card Attack and Defense within a turn

Observable outcome: A hero can perform the Phase 2 test Attack during its Initiative position, pause for the enemy Defense reaction, resume, resolve the attacking card, and continue the turn.

Acceptance checks:

- The Defense reaction does not consume the defender's normal action.
- The nested response resumes the same pending Attack and turn frame.
- Attack completion resolves only the attacker's played card.
- The next unresolved actor is selected from current state after the reaction closes.

### [ ] 3.8 Complete the four-turn round skeleton

Observable outcome: Four turns advance, cards occupy the correct turn slots, and the round reaches an explicit end-of-round phase.

Acceptance checks:

- A card cannot be replayed from the wrong state.
- Passing behavior is represented when no card remains.
- Turn and round indices are authoritative and deterministic.
- End-of-round entry is explicit but does not yet resolve its subsystems.

### [ ] 3.9 Serialize and restore authoritative snapshots

Observable outcome: A versioned snapshot round-trips without changing logical state.

Acceptance checks:

- Teams, players, heroes, Tie Breaker, turn/round state, board occupancy, card instances/state facets, and definition references survive round-trip tests.
- A state paused on a pending card choice or Defense reaction resumes to the same result and ordered events.
- Snapshot schema version and card-definition content revision are checked independently; mismatches are rejected clearly until a migration exists.
- Presentation can rebuild from restored state.

Non-goals: No player-facing save slots or cloud storage.

### [ ] 3.10 Add director-mode Undo and restart

Observable outcome: Undo restores the snapshot before the last committed command; restart restores the initial scenario.

Acceptance checks:

- Invalid commands do not create undo entries.
- Undo restores authoritative card, turn, Tie Breaker, pending-resolution, and board state plus presentation.

Non-goals: No redo requirement and no competitive availability.

### [ ] 3.11 Add the internal rule-event log

Observable outcome: A primitive internal panel shows accepted ordered rule events and rebuilds consistently after restore, Undo, or restart.

Acceptance checks:

- Each entry shows sequence, event type, stable rules-object references, and the key value/choice needed to diagnose the event.
- Rejected commands do not appear as accepted state-change events; any rejection display is clearly separate.
- Undo removes events after the restored command boundary, restart clears to setup events, and snapshot restore reproduces the same log projection.
- The log is a read-only projection and cannot mutate or continue rules resolution.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No final visual styling, export format, analytics, telemetry, or player-facing match history.

## Phase 4 - Full official multi-zone board

### [ ] 4.1 Inventory the complete single-lane board reference

Observable outcome: Every part of the complete official single-lane board - including all spaces, connections, terrain, zones, spawn points, Thrones, and the starting Battle Zone - has a reviewed data specification.

Acceptance checks:

- Every logical space has a stable ID and coordinate.
- Every zone boundary, zone membership, spawn point, and Throne is checked against the authoritative visual reference.
- The inventory covers the full map rather than a reduced movement-test area.

Non-goals: No production board art or models.

### [ ] 4.2 Upgrade the test grid to the full official placeholder map

Observable outcome: The temporary Phases 1-3 test grid is replaced in the main validation scene by placeholder geometry for the complete official multi-zone single-lane map.

Acceptance checks:

- Logical adjacency matches the reviewed board specification.
- Terrain and spawn-point classifications are inspectable.
- Every official zone is visibly distinguishable in debug presentation without implying final art.
- The complete map can be framed and inspected under the current fixed-camera test setup.

Non-goals: No polished environment, Fast Travel, lane pushing, or production camera controls.

### [ ] 4.3 Define and validate zones and zone adjacency

Observable outcome: Every board space belongs to the correct official zone, and authoritative queries can distinguish the same zone, an adjacent zone, and a non-adjacent zone.

Acceptance checks:

- Red Throne, Blue Throne, the starting Battle Zone, and every intermediate zone have stable IDs.
- Every playable space has the correct zone membership.
- Zone adjacency follows the rulebook definition and is verified against the complete map.
- Focused tests cover same-zone, adjacent-zone, and non-adjacent-zone pairs in both directions.
- The ordered lane relationship between the two Thrones is represented for later lane movement.

Non-goals: No Fast Travel validation, current-Battle-Zone changes, lane pushing, minion respawn, or victory logic.

### [ ] 4.4 Place the 1v1 Quick Game setup

Observable outcome: Red Wasp, Blue Wasp, standard minions, Lives, Waves, and Tie Breaker state are placed from configuration data while their hands still use synthetic test cards.

Acceptance checks:

- Three Wave counters and four Life counters per team.
- Standard single-lane minions are created from configuration with stable identity, team, type, and occupied-space data.
- The initial Tie Breaker result comes from milestone 2.6.
- Only the approved player-count, mirrored-hero, and director-mode deviations are present.
- T3 phase gate: source/data validation, focused setup-scene inspection, and both complete unfiltered Unity suites pass.

Non-goals: No complete Wasp card inventory or Wasp-specific behavior.

## Phase 5 - Core full-map actions and combat

Phase 5 uses verified general rules and synthetic test definitions. It does not depend on the complete Wasp card set, inventory that set, or implement it.

### [ ] 5.1 Integrate and verify exact card-based Movement on the full map

Observable outcome: A synthetic Movement action uses its typed allowance to traverse a chosen legal route on the official placeholder map.

Acceptance checks:

- The action reuses Phase 1 route choice, obstacle blocking, command/event authority, and pending resolution.
- The Phase 1 segmented waypoint interaction is integrated on the official placeholder map so the player can construct any legal cumulative route within the allowance, including a deliberately longer/non-shortest route to a destination.
- Focused source-linked tests prove shortest and longer legal cumulative routes to the same destination commit their selected ordered steps deterministically without enumerating every complete route up front.
- Action-start restrictions are evaluated at the correct checkpoint.
- Up-to movement permits zero movement and early stopping.
- Ordinary Movement remains distinct from direct movement written in card text.

### [ ] 5.2 Implement Fast Travel replacement

Implements only after milestone 5.1 and the Phase 4 zone model are complete.

Acceptance checks:

- Fast Travel works within the same zone and into an adjacent zone.
- A non-adjacent destination zone is rejected.
- Enemy presence in the origin or destination zone blocks eligibility.
- No empty-space path between origin and destination is required.
- Fast Travel replaces the entire Movement action and suppresses Primary card text.

### [ ] 5.3 Add minion obstacle behavior and Defense contributions

Observable outcome: Standard minions are authoritative units/obstacles and contribute their general Defense modifiers through the typed value service.

Acceptance checks:

- The rules reuse the authoritative minion identities, teams, types, and spaces created by milestone 4.4 rather than creating a second representation.
- Hero/minion occupancy follows the one-obstacle-per-space rule.
- Defense contributions retain minion-source provenance.
- Heavy, Ranged, and Melee identity is not inferred from presentation.

### [ ] 5.4 Add generic timing, Active-effect, and continuous-source lifecycles

Observable outcome: Synthetic effects can occupy named timing windows and serializable lifetimes without Wasp-specific logic.

Acceptance checks:

- Before/After Attack, action start/end, turn end, round end, and defeat/card-state cancellation hooks are explicit.
- Active effects and passive/continuous sources remain distinct.
- Simultaneous ordering uses the official tie procedure without flipping the Tie Breaker.
- Presentation timing cannot delay or reorder rules checkpoints.

### [ ] 5.5 Add coin awards and assist-ready accounting

Observable outcome: Defeat outcomes can award coins to the correct hero/team collection through typed resource operations.

Acceptance checks:

- The 1v1 setup awards the acting hero correctly.
- The model supports friendly assist recipients without implementing 3v3 UI.
- Rewards remain distinct from removal so prevented removal does not erase a verified Defeat reward.

Non-goals: Exact hero-Level and lowest-Tier reward amounts remain part of complete hero-defeat integration at milestone 7.4.

### [ ] 5.6 Attack and defeat minions with test cards

Observable outcome: A synthetic Attack can legally target and defeat an enemy minion using the general minion rules.

Acceptance checks:

- Minions do not open a Defense reaction.
- Heavy immunity applies while a friendly non-Heavy minion remains in the Battle Zone.
- Defeat, reward, removal attempt, and removal result remain distinct typed facts.
- Melee/Ranged and Heavy rewards use verified general values.
- Last-minion Lane-Push handling remains a separate Phase 6 integration.

### [ ] 5.7 Return displaced minions to the Battle Zone

Acceptance checks:

- Correction applies only to intrinsic minion miniatures outside the Battle Zone after the whole action.
- A minion follows a shortest empty path; its team chooses among equally short valid paths.
- With no path or legal move, the minion is Placed into a nearest empty Battle-Zone space.
- The fallback implements the minimum general Place capability through typed policy/outcome data and emits Place rather than Movement facts.
- When cross-team order matters, the Tie-Breaker-side team resolves first without flipping the coin.
- End-to-end ordering between separate Repeat instances is deferred until the first verified Repeat implementation.

### [ ] 5.8 Complete the general Hero Attack and Defense order with test cards

Observable outcome: Synthetic hero combat follows the complete general Attack/Defense order while still deferring Wasp-specific text.

Acceptance checks:

- Target validation, Before Attack, Attack Total, Defense response, Block/result comparison, After Attack, and action-end checkpoints occur in verified order.
- Focused synthetic scenarios cover both non-Ranged adjacent targeting and Ranged targeting through typed Range/distance queries.
- Printed, minion, item-ready, card-text-ready, and Active-effect-ready contributions remain distinct even when only synthetic contributions are populated.
- A successful Attack records a staged Defeat outcome for Phase 7 consequences.
- Wasp card text, values, items, and special effects are not used.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused combat scenarios.

## Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Phase 6 uses the complete Phase 4 map and the synthetic turn/round controller. Lane behavior is never validated on the reduced test grid.

### [ ] 6.1 Resolve the end-of-round Minion Battle

Observable outcome: The authority compares Battle-Zone minions, resolves the losing team's choices with Heavy removed last, and records an end-of-round Lane-Push request if a team loses its last minion.

Acceptance checks:

- Equal counts have no effect.
- The losing team removes the exact difference or all remaining minions when it has fewer than required.
- Choice ownership and Heavy-last ordering are authoritative.
- The Lane-Push request is not consumed before token/marker cleanup.

### [ ] 6.2 Add token and marker instances, supplies, placement, and attachment

Observable outcome: Synthetic tokens and markers have stable authoritative identity without requiring Wasp content.

Acceptance checks:

- Tokens use the authoritative finite supply and occupy board spaces as obstacles.
- Markers are attachments rather than board occupants and have explicit return state.
- Token placement/reclamation and marker attachment are authoritative choices/transitions.
- Token and marker lifetime is not inferred from art or UI objects.

### [ ] 6.3 Add core token interactions, persistent properties, and Clear

Observable outcome: Synthetic tokens preserve their properties while supporting the general board interactions needed by the core loop.

Acceptance checks:

- Token properties are continuous sources, not ordinary card Active effects.
- Clear replaces an entire Attack and may remove any chosen subset of adjacent tokens.
- A unit spawning on a token-covered Spawn point removes that token first.
- Token lifetime is defined independently from the source hero's on-board or defeated state.
- Optional card-only Move, Object Push, Swap, and Replace interactions are deferred until verified content requires them.

### [ ] 6.4 Emit an immediate Lane-Push request during a turn

Acceptance checks:

- A recorded Defeat of the last relevant enemy Battle-Zone minion emits the request.
- Ordinary Movement never emits the request; optional direct Move, Object Push, Swap, and standalone Remove conformance is deferred until each verified capability is first implemented.
- A Defeat still emits the request when its later removal is prevented.
- The request reaches the immediate checkpoint before a later Initiative action can continue.
- This milestone does not expose a partial Lane transition.

### [ ] 6.5 Resolve Lane Push Wave and Battle-Zone stages

Builds the non-executable ordered prefix: flip exactly one Wave counter and stop on final-Wave victory; otherwise advance one verified Zone toward the losing team's Throne and stop on Throne capture.

Acceptance checks:

- The incomplete prefix remains internal and cannot consume a live request.
- Wave and Zone stages emit deterministic typed outcomes/events.
- An early game-ending result prevents later stages.

### [ ] 6.6 Complete Lane Push minion respawn and transition

Completes the transition by removing all minions from the previous Battle Zone and respawning the matching type at every verified Spawn point in the new Battle Zone.

Acceptance checks:

- Transition order is Wave/final-Wave check, Zone/Throne check, then old-zone removal and new-zone respawn.
- A token-covered Spawn point removes its token before respawn.
- A non-token obstacle makes the owning team Place that minion in a chosen nearest empty Battle-Zone space.
- When cross-team order matters, the Tie-Breaker-side team starts without flipping the coin.
- An immediate transition completes before later Initiative resolution continues.
- Repeat-boundary integration is deferred until the first verified Repeat implementation.

### [ ] 6.7 Remove tokens and return markers at end of round

Runs the general cleanup checkpoint: remove all board tokens and return all markers to supply except where exact verified text provides an override.

An end-of-round Lane-Push request remains pending until this cleanup finishes.

### [ ] 6.8 Retrieve cards and cancel applicable Active effects

Observable outcome: Each hero retrieves only its own eligible resolved/discarded synthetic cards and applicable effects end through authoritative state changes.

Acceptance checks:

- Retrieval never takes another hero's card.
- Every eligible card returns regardless of its prior eligible state, while ineligible/set-aside/item cards do not.
- Applicable Active effects, including relevant Next Turn effects, cancel through authoritative events.
- Focused tests cover resolved, discarded, facedown eligible, other-owner, and active-effect cases.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused Phase 6 scenarios.

## Phase 7 - Progression, victory, and the complete synthetic core loop

### [ ] 7.1 Add synthetic card tiers and upgrade relationships

Observable outcome: Synthetic non-Basic cards model Tier I-III upgrade choices without mutating definitions or using Wasp data.

Acceptance checks:

- Basic cards have no Tier and are excluded from lowest-Tier and upgrade-candidate queries.
- Every non-Basic definition has an explicit Tier and stable same-color next-Tier candidates where applicable.
- Hand, deck, and set-aside instances retain ownership and definition revision through upgrade transitions.
- Each synthetic hero has authoritative current-Level state initialized to Level 1.
- Typed queries can identify a hero's current lowest non-Basic card Tier for later Life and leveling rules.

Non-goals: No coin spending, level purchase, item bonus, Ultimate, or Wasp definition.

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

### [ ] 7.5 Resolve mandatory end-of-round leveling and upgrades

Observable outcome: Every eligible synthetic hero buys as many consecutive levels as it can afford and completes the required upgrade, item, or Ultimate outcome for each level.

Acceptance checks:

- Level purchases are mandatory, spend the configured cost in order, and cannot skip a level.
- Each ordinary purchase upgrades one chosen lowest-Tier non-Basic card; tied choices and the two same-color next-Tier candidates use authoritative pending choices.
- The chosen upgrade enters the hand, the replaced card returns to the deck, and the unchosen candidate becomes the matching item using milestone 7.2.
- After all non-Basic cards reach Tier III, the next eligible level unlocks the milestone 7.3 Ultimate and no further levels are available.
- Multi-level and multi-hero ordering is deterministic and resumable without Wasp-specific branches.

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

- Final-Wave and Throne outcomes reuse milestone 6.5; final-Life loss reuses milestone 7.4.
- Each path records the winning team and exact reason, stops pending resolution, and prevents later rule steps.
- Focused scenarios prove each path independently and reject duplicate or conflicting winners.

### [ ] 7.8 Integrate the exact end-of-round sequence

Observable outcome: One authoritative state machine executes the completed end-of-round systems in source order and arrives at a valid next round or game-ended state.

Acceptance checks:

- The sequence runs Minion Battle; token removal/marker return; requested Lane Push; card retrieval/cancellation; mandatory leveling/upgrades; and Pity Coin collection at their exact checkpoints.
- Applicable End of Round effects and immediate victory checks occur at rules boundaries rather than presentation timing.
- A game-ending result stops later processing immediately.
- Focused scenarios cover no Lane Push, one Lane Push, level/no-level players, retrieved effects, Pity Coin eligibility, and a game-ending Push.
- The same state and choices produce the same state and ordered events.

### [ ] 7.9 Complete a deterministic test-card core match

Observable outcome: Synthetic test cards can start a match, play legal Movement/Hold/Attack/Defense turns, complete rounds, resolve minions and Lane Pushes, progress, and end through every victory path.

Acceptance checks:

- Every general core rule used by the loop links to its authoritative source and focused tests.
- Initiative, Tie Breaker flips, turns, rounds, immediate/end-of-round Lane Pushes, retrieval, leveling, respawn, and victory work together.
- Snapshots, Undo, restart, and the event log remain deterministic across the complete loop.
- Synthetic definitions are visibly test-only and contain no claim about Wasp values or effects.
- A broader deterministic core-loop suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The complete Wasp card inventory and Wasp-specific card implementation do not begin until milestone 7.9 is reviewed and approved.

## Phase 8 - Verified Wasp content and deterministic 1v1 integration

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work extends proven core systems; it does not replace them. The mirrored Wasp setup identities introduced earlier are configuration placeholders, not card-content implementation.

### [ ] 8.1 Inventory and verify Wasp's complete card set

Observable outcome: Wasp's cards, tiers, values, action types, text, item possibilities, and Ultimate are traceable to source images.

Acceptance checks:

- Every required face is present and readable.
- Structured data is manually checked against the images.
- Missing or ambiguous content is recorded in `RULES_LEDGER.md`.

Non-goals: No other hero inventory or effect implementation.

### [ ] 8.2 Map Wasp cards to capabilities and re-baseline the remaining Phase 8 milestones

Observable outcome: Every verified Wasp action maps to required contexts, ordered nodes, typed bindings/outcomes, timing, state transitions, and reusable or custom capabilities before effect implementation.

Acceptance checks:

- The matrix covers every Wasp card face, tier, item relationship, and Ultimate from milestone 8.1.
- Existing core capabilities are distinguished from Wasp-specific refinements, new reusable primitives, and genuinely unique handlers.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and tests.
- Remaining Phase 8 milestones are reordered or split so dependencies precede the first Wasp card that needs them.
- If the Phase 8 endpoint changes, update the T3 gate in `TESTING.md` and this roadmap in the same re-baseline.
- New reusable contracts update `CARD_EFFECT_CAPABILITIES.md` before implementation.
- Any schema change is explicit and migration-tested rather than hidden inside a card milestone.

Non-goals: No effect implementation or other-hero inventory.

### [ ] 8.3 Add verified Wasp definitions and lifecycle data

Observable outcome: Verified Wasp definitions/instances use the existing lifecycle and turn foundation without placeholder authority.

Acceptance checks:

- Wasp values and text are source-verified structured data.
- Definition revisions and upgrade/replacement relationships are explicit.
- Card images remain presentation/audit assets only.
- Synthetic test values are not used as Wasp authority.

### [ ] 8.4 Load both Wasp hands into the director turn loop

Observable outcome: Red and Blue Wasp hands replace the synthetic hands while using the already-proven play/reveal/Initiative/turn controller.

### [!] 8.5 Implement exact Wasp Movement behavior

### [!] 8.6 Implement verified Wasp spatial effects

Covers only required direct Move, Object Push, Place, Spawn, Respawn, Swap, and Replace slices.

### [!] 8.7 Implement verified Wasp token, marker, obstacle, and persistent effects

### [!] 8.8 Implement verified Wasp card-state and resource effects

### [!] 8.9 Implement verified Wasp modifiers, restrictions, permissions, and characteristics

### [!] 8.10 Implement Wasp Skills and structured control flow

### [!] 8.11 Implement Wasp Attack, range, and minion interactions

### [!] 8.12 Implement complete Wasp Defense behavior

### [!] 8.13 Implement Wasp timing, Active-effect, and continuous behavior

### [!] 8.14 Integrate Wasp leveling, upgrades, items, and Ultimate

Every milestone from 8.5 onward remains blocked until milestone 8.2 re-baselines the exact dependency order and smallest proving slices.

### [!] 8.15 Complete and verify every Wasp card behavior

Observable outcome: Every Wasp definition and legal action at each pitch-relevant tier passes source-linked focused scenarios after its re-baselined dependencies are available.

Acceptance checks:

- Every offer/context from the milestone 8.2 matrix is implemented; a blocked entry keeps this milestone incomplete.
- No placeholder value, display-name branch, runtime text parsing, or UI-owned rule path remains.
- Interaction tests cover each required timing, target, modifier, replacement, repeat, state change, Active effect, and continuous source.
- Any unresolved source question prevents completion rather than being guessed.

### [!] 8.16 Complete the deterministic Wasp 1v1 match

Observable outcome: A Wasp mirror match can start, play repeated turns/rounds, and end through every victory path with primitive visuals and debug UI.

Acceptance checks:

- Every implemented Wasp rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader source-linked Wasp integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: Production visual work does not begin until milestone 8.16 is reviewed and approved.

## Phase 9 - Presentation and pitch polish

Every item remains a separate micro-milestone.

Each visible milestone runs focused Play Mode checks for the affected presentation path plus one targeted visual inspection. No player build is produced before Phase 10, and complete unfiltered suites wait for milestone 9.16 unless a T3 risk trigger applies earlier.

### [ ] 9.1 Production board environment

### [ ] 9.2 Wasp miniature and Red/Blue team treatments

### [ ] 9.3 Minion miniatures

### [ ] 9.4 Bounded pan, orbit, zoom, and reset camera

### [ ] 9.5 Automatic 3D miniature / 2D token switching

### [ ] 9.6 Wasp idle animation

### [ ] 9.7 Wasp movement animation

### [ ] 9.8 Wasp attack animation

### [ ] 9.9 Wasp hit/defeat animation

### [ ] 9.10 Wasp respawn animation

### [ ] 9.11 Shared minion animation set

### [ ] 9.12 Rule-event presentation queue, skip, and speed controls

### [ ] 9.13 Final pitch HUD and contextual prompts

### [ ] 9.14 Team symbols, outlines, readability, and UI scaling baseline

### [ ] 9.15 Minimal sound-effects pass

### [ ] 9.16 Complete presentation regression and visual-readability sweep

Observable outcome: The finished pitch presentation remains readable and synchronized with authoritative rules across the complete primitive-to-polished match flow.

Acceptance checks:

- A targeted visual sweep covers the board, both hands, prompts, event log, team differentiation, camera bounds, 3D/2D switching, animations, and affected audio cues.
- Skipping or accelerating presentation produces the same authoritative state and ordered rules events.
- Common reference desktop resolutions preserve readable cards, values, prompts, and team identity.
- T3 pre-build gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The first player build does not begin until milestone 9.16 is reviewed and approved.

## Phase 10 - Private publisher pitch package

### [ ] 10.1 Windows standalone build

Observable outcome: An unchanged milestone 9.16 revision produces a private Win64 player that launches to the pitch prototype without compilation, startup, or missing-content errors.

Acceptance checks:

- Build settings, version, source commit, output path, and launch-smoke result are recorded.
- The build launches, reaches the prototype, starts a deterministic scenario, and exits cleanly.
- The milestone reuses the unchanged 9.16 T3 result; any intervening code or executable-content change requires T3 again before building.

Non-goals: No installer, launcher, code signing, updater, or public distribution.

### [ ] 10.2 Clean-machine private smoke test

Observable outcome: The exact private Win64 build launches and performs the scripted smoke path on a clean Windows machine without Unity or project dependencies installed.

Acceptance checks:

- The packaged build starts from a clean copy, reaches the prototype, accepts input, completes the scripted smoke path, and exits without an unhandled error.
- Missing files, platform dependencies, logs, machine details, and build identity are recorded.
- Unity Test Framework is not rerun against an unchanged build merely to duplicate the 9.16 evidence.

### [ ] 10.3 Short recorded gameplay walkthrough

### [ ] 10.4 Publisher demonstration checklist

Non-goals: Public distribution, installer, launcher, login, telemetry, store page, or other-platform release.

## Post-authorization direction - not scheduled

- Formalize licensed product scope and name.
- Choose and prototype network transport/hosting based on then-current requirements.
- Move authoritative resolution to a host/server.
- Add per-player hidden hands and six-human online 3v3.
- Add reconnect, lobby, matchmaking, accounts, and team communication.
- Add heroes incrementally; every hero intake starts with a verified effect-family delta audit against the capability catalog before implementation is scheduled.
- Validate macOS/Linux, then touch devices, then closed platforms as separately approved tracks.
