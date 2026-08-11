# Guards Digital - Roadmap

Last updated: 2026-08-11

## Status legend

- `[x]` Complete and reviewed.
- `[ ]` Proposed and not started.
- `[~]` In progress; at most one item may have this state.
- `[!]` Blocked by an explicit prerequisite or decision.

## Execution rule

This roadmap is a sequence of micro-milestones, not authorization to execute them.

GPT-5.6 Sol is the only permitted model for milestone work. Reasoning effort must be medium or higher: use medium by default, and raise it to high, extra high (`xhigh` in configuration), or ultra when the milestone's complexity, breadth, integration risk, or hard-gate role requires it.

For every item:

1. Confirm that the exact item declares its `Required Codex profile` and that `.codex/config.toml` matches it; this is execution metadata, not authorization.
2. Obtain explicit user approval for that item.
3. Implement only its stated observable outcome.
4. Respect its non-goals.
5. Run the milestone's focused tests, directly affected regressions, and highest applicable validation tier from `Docs/AI/TESTING.md`.
6. Update the project documents and `.codex/config.toml`, including the next proposed milestone's required profile and matching new-task defaults.
7. Create a local Git commit containing the completed milestone and continuity updates.
8. Report the result, the next proposed milestone and its required profile, then stop.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

At each designated phase-gate roadmap audit, reassess the affected unstarted milestones against the evidence produced so far. The audit is a required handoff activity, not a separately numbered milestone, and does not authorize implementation. For each affected milestone, recommend whether to keep it unchanged, merge it with adjacent work that shares the same authority and validation boundary, split independently risky behavior, or defer/remove work that is not required for the private publisher pitch. Record an approximate remaining-effort range and identify the shortest dependency-safe path to the next hard integration outcome. Any resulting roadmap change requires explicit user approval and the applicable continuity updates before the next milestone profile is assigned. An audit must not weaken verified rules, architecture contracts, acceptance evidence, validation gates, or the one-approved-micro-milestone-at-a-time rule.

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

Completion note (2026-08-09): Added pure immutable `Team`, `Player`, and `Hero` participant records with typed nonempty IDs, validated collection uniqueness, collection-backed relationship resolution, and plural player/hero queries by team. The synthetic 1v1 configuration creates one Red player with the existing stable `primitive-hero` and one Blue player with the new stable `blue-test-hero`. Each hero owns a separate two-card copy of the Phase 1 synthetic Movement hand; all four runtime card-instance IDs are globally unique, their shared definition references validate, and every owner ID resolves to the correct hero/player/team. A four-player synthetic fixture proves team APIs return multiple heroes rather than assuming one hero per team. The validation scene places both authoritative test occupancies on adjacent hexes with visibly distinct Red/round and Blue/square primitives. Both miniatures have pointer selectors: clicking either makes it the active director-controlled hero, swaps the panel to that hero's stable owned cards, and permits that hero to resolve the existing Movement offers. Both occupancies remain in the shared adjacent-move/reachability authority, so the opponent blocks its hex without a presentation-owned rule; actor switching is rejected while a resolution is pending. Visually reviewed rulebook printed pages 3 and 5-6 for one hero per player, opposing Red/Blue teams, equal-team setup, and per-player hero cards; the pitch still applies DEV-001 and DEV-003. T2 focused validation passed after the interaction correction: Edit Mode 37/37 (`PrimitiveHeroViewTests`, `HeroSelectionControllerTests`, `MinimalHexGridSceneTests`, `CardDrivenMovementPresentationTests`, and `MatchParticipantsTests`); Play Mode 5/5 (`MinimalHexGridPlayModeTests`); and targeted scene/UI inspection confirmed distinct selectable miniatures and active-owner hand labeling, all with zero skipped or inconclusive cases. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected participant/view/controller/scene path is covered by focused tests. No 3v3 UI, multiplayer behavior, Attack/Defense offers, combat, turns, networking, or animation was added.

### [x] 2.2 Extend the test cards with Attack and Defense action offers

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The Phase 1 placeholder definitions gain non-Ranged Attack and Defense-reaction action definitions, from which the authority derives typed runtime offers owned by the correct test heroes.

Acceptance checks:

- Test-card Attack and Defense values are typed printed values on action definitions and are queried through runtime offers, not UI or scene constants.
- The Attack card has a printed Primary, non-Ranged Attack definition that derives the eligible runtime offer.
- A defending card definition declares an eligible Defense reaction from hand and discard as its cost/state transition, without executing that reaction yet or pretending it is a normal turn action.
- Every card used to defend is associated with exactly one hero's hand.
- The minimal card-state facets support in-hand and discarded locations without pretending to implement the complete turn lifecycle.
- Invalid Attack/Defense values, missing owners, and cross-owner card use are rejected by focused tests.

Non-goals: No Wasp data or text, ranged attacks, card tiers/colors, initiative, items, card effects, Block, turns, or animations.

Completion note (2026-08-09): Extended the two immutable synthetic definitions used by both test heroes with typed Attack/Defense data while preserving their existing Movement definitions and stable card-instance ownership. `Route Drill` now declares a printed Primary non-Ranged Attack 3; both test definitions declare Defense reactions (2 and 3) whose required source state and declared cost/state transition are Hand to Discard Pile. Runtime offers carry the typed Attack-range and source-card transition facts, derive only in their requested normal/reaction windows, and remain bound to the exact owner/source card. The minimal orthogonal state vocabulary now exposes an unplayed discarded state for eligibility tests, but no Defense discard is executed. Definition validation rejects wrong Attack/Defense channels, missing Attack range, non-reaction Defense, missing Defense discard transitions, and negative printed values. Focused tests reject missing owners, cross-owner attempts, and already-discarded Defense cards. The Phase 1 card panel explicitly remains Movement-only, so these new offers are authority data rather than prematurely exposed combat controls. Rulebook printed pages 13-14 were visually verified. T2 validation passed: focused Edit Mode 28/28 (`ActionOfferAndPrintedValueTests`, `CardDefinitionInstanceTests`); directly affected Edit Mode 80/80 (`ActionOfferAndPrintedValueTests`, `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `CardActionResolutionAuthorityTests`, `CardDrivenMovementPresentationTests`, `HeroSelectionControllerTests`, `MinimalHexGridSceneTests`); and focused Play Mode 5/5 (`MinimalHexGridPlayModeTests`), all with zero skipped or inconclusive cases. Complete unfiltered suites were intentionally omitted because 2.2 is not a T3 gate and the affected authority/participant/presentation consumers were enumerated. No Attack target, Defense response window, card mutation, combat result, Wasp content, Ranged Attack, Block, item/modifier, turn, or animation behavior was added.

### [x] 2.3 Initiate a basic adjacent hero Attack

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: One test hero can select its non-Ranged Attack offer and target the adjacent enemy test hero through the shared command and resumable-resolution boundary.

Acceptance checks:

- Only an adjacent enemy hero is a legal target.
- The attacker cannot target itself, a friendly hero, a non-adjacent hero, an empty space, terrain/token, or another non-unit obstacle; heroes/minions remain obstacles for occupancy without ceasing to be unit targets.
- The initial Attack Total comes through the value-query service and equals the selected offer's printed Attack contribution when no modifiers exist.
- Validation failure does not mutate state or consume a card.
- The resolver opens an explicit Defense response choice owned by the targeted hero and emits an ordered Attack-started event for presentation.

Non-goals: No ranged targeting, minions, items, modifiers, Before/After Attack text, Wasp effects, Defense-card selection/resolution, or animation.

Completion note (2026-08-09): Extended the existing pure card-action authority with a typed, restorable non-Ranged Attack path that revalidates the selected normal-play offer, projects only adjacent enemy participant heroes from authoritative unit occupancies, accepts a stable target-choice command, and pauses the same resolution at a defender-owned Defense response choice. Self, friendly, non-adjacent, unknown/empty, terrain/token, and non-unit-obstacle identities are excluded, while hero occupancy continues to block movement without erasing unit target identity. Target validation and stale-board failures emit no events, preserve the exact pending Attack, and leave every card state unchanged. After a legal target, the authority queries the selected offer's printed Attack contribution (total 3 with no modifiers), projects the target hero's currently eligible Defense reaction offers without consuming them, and emits ordered `AttackStarted`, `AttackTotalCalculated`, and `DefenseResponseChoiceRequested` facts. Printed rulebook pages 13-14 were visually verified. The original authority-focused handoff passed Edit Mode 7/7 and 100/100 but omitted a user-testable presentation path. The corrective completion exposes Route Drill's `PRIMARY ATTACK 3` control, highlights exactly the coordinates belonging to the authority-projected legal target hero IDs, routes an adjacent enemy-miniature click through the stable target command, clears the highlight after acceptance, retains the attacker as active actor, and displays the resulting pending Defense response. Corrective T2 validation passed with zero skipped or inconclusive cases: initial presentation Edit Mode 35/35, initial affected Edit Mode 71/71, target-highlight regression Edit Mode 43/43 (`CardDrivenMovementPresentationTests`, `AttackActionResolutionAuthorityTests`, `CardActionResolutionAuthorityTests`, and `HeroSelectionControllerTests`), and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`). Targeted visual inspection confirmed the Phase 2 test hand visibly renders `PRIMARY ATTACK 3` without clipping or overlap and the legal target tile receives a strong yellow highlight. Complete unfiltered suites were intentionally omitted because milestone 2.3 is not a T3 gate and the affected consumers are enumerated. No Defense answer, discard, comparison/result, Ranged Attack, minion, item/modifier, timing text, Wasp effect, card consumption, or animation was added.

### [x] 2.3a Initiate a basic Ranged hero Attack

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: One test hero can select a synthetic Ranged Attack offer and target the enemy test hero within the offer's typed printed Range through the same shared target-query, command, and resumable-resolution boundary introduced by milestone 2.3.

Acceptance checks:

- A synthetic test-card definition declares a printed Primary Ranged Attack with typed Attack and Range values, and the authority derives both through the selected runtime offer rather than UI or scene constants.
- An enemy hero is a legal target when its rulebook hex distance from the attacker is no greater than the current Range Total; Range 1 includes adjacent targets and the printed Range is the complete Range Total when no modifiers exist.
- Rulebook distance and Range are queried explicitly rather than inferred from movement paths. Occupied spaces and other obstacles count normally and do not create an invented line-of-sight restriction.
- The attacker cannot target itself, a friendly hero, an out-of-range hero, an empty space, terrain/token, or another non-unit obstacle.
- Accepted Ranged and non-Ranged hero targets enter the same pending Attack/Defense response flow and emit the same ordered Attack-started event shape; range classification changes target eligibility, not resolution ownership or presentation authority.
- Invalid, forged, stale, or out-of-range attempts do not mutate state, consume a card, or open a Defense response.
- Focused tests cover Range 1 adjacency, a target exactly at maximum Range, a target beyond Range, and an in-range target across occupied or obstacle spaces.

Non-goals: No minion targets, Range modifiers from card text/items/Active effects, Before/After Attack text, Defense-card selection/resolution, Block, full-map combat integration, Wasp data or effects, or animation.

Completion note (2026-08-10): Extended the same typed, restorable Attack authority from milestone 2.3 with an explicit rulebook hex-distance query and shared enemy-hero target projection for both non-Ranged and Ranged classifications. The synthetic Obstacle Course definition now exposes a printed Primary Ranged Attack 2 with printed Range 2; action-definition validation requires exactly one typed Attack and one positive typed Range value for Ranged Attacks, while non-Ranged Attacks remain Attack-only. The authority re-derives the selected runtime offer and its current Range Total at start, target answer, and restoration boundaries. Range 1 includes adjacent targets, the maximum printed Range is inclusive, targets beyond it reject without events or card mutation, and occupied/obstacle spaces do not block or lengthen the geometric distance because the rulebook has no line-of-sight rule. Accepted Ranged targets enter the unchanged `AttackStarted`, `AttackTotalCalculated`, and defender-owned `DefenseResponseChoiceRequested` flow; self, friendly, empty, terrain/token, and non-unit obstacles remain excluded. The Phase 2 test hand visibly exposes `PRIMARY RANGED ATTACK 2 RANGE 2`, routes the existing hero pointer through the same stable target command, and uses the same authority-projected yellow target highlight and Defense prompt. Printed rulebook pages 9 and 13 were visually verified. T2 validation passed with zero skipped or inconclusive cases: final focused Edit Mode 36/36, final affected Edit Mode 114/114 across the Attack, resolver, offer/value, card, participant, movement, occupancy, presentation, controller, and hex fixtures, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`). Targeted visual inspection confirmed the Ranged action label/status are readable without clipping and the accepted target coordinate receives the strong yellow highlight. Complete unfiltered suites were intentionally omitted because milestone 2.3a is not a T3 gate and all affected consumers are enumerated. No Defense answer/discard, comparison/result, minion target, Range modifier, timing text, Wasp data/effect, card consumption, full-map integration, or animation was added.

### [x] 2.4 Defend with one test card or decline

Required Codex profile: GPT-5.6 Sol with medium reasoning.

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

Completion note (2026-08-10): Extended the restorable synthetic Attack resolution with an authoritative defender-owned answer command that either selects exactly one currently legal card from the targeted hero's hand or declines. Acceptance re-derives the pending reaction offer and its printed Defense value, rejects missing, multiple, cross-owner, attacker-selected, already-discarded, stale, forged, and replayed responses without mutation, and only then applies the selected card's declared Hand-to-Discard-Pile transition while preserving its orthogonal played/resolution/orientation facts. A legal discard records only the printed Defense contribution/total in a distinct reaction invocation context; decline and no-legal-card paths produce no Defense Total. Ordered Defense-card-discarded, Defense-total/declined, and response-completed facts resume the same Attack at an explicit comparison checkpoint without comparing totals or resolving consequences. The Phase 2 panel visibly presents only the targeted hero's legal Defense cards plus Decline, blocks actor/action switching while the Attack remains pending, removes the discarded option, and reports the milestone 2.5 comparison checkpoint. Printed rulebook page 14 was verified. T2 validation passed with zero failed, skipped, or inconclusive cases: authority-focused Edit Mode 17/17, final affected Edit Mode 120/120 across Attack, resolver, offer/value, card, participant, movement, occupancy, presentation, controller, and hex fixtures, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`). Targeted visual inspection confirmed the Defense controls and status are readable without clipping and the accepted discard advances cleanly to the comparison checkpoint. Complete unfiltered suites were intentionally omitted because milestone 2.4 is not a T3 gate and the affected consumers are enumerated. No Attack/Defense comparison, result, Block, card text, modifier, defeat consequence, turn, or animation behavior was added.

### [x] 2.5 Compare Attack and Defense and report the test outcome

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The authority completes the basic test Attack/Defense sequence and reports either successfully defended or attack successful.

Acceptance checks:

- Defense Total equal to or greater than Attack Total successfully defends.
- Defense Total below Attack Total results in a successful attack.
- Declining or being unable to defend results in a successful attack.
- Resolution resumes the same pending Attack, closes its Defense window, and emits reproducible ordered events.
- The presentation clearly identifies the result without inventing Life, coin, respawn, or other deferred consequences.

Non-goals: A successful test attack does not yet implement full hero defeat, removal, unresolved-card resolution, active-effect cancellation, markers, coins, assists, Life spending, victory, or respawn.

Completion note (2026-08-10): Completed the same restorable synthetic Attack after its Defense response by adding a no-choice authoritative comparison step at the existing outcome checkpoint. The authority revalidates the selected Attack offer, current legal target, printed Attack Total, comparison step, and any discarded Defense offer/value before comparing; stale completed-Defense state rejects without closing the pending Attack. A Defense Total equal to or greater than Attack Total reports `SuccessfullyDefended`; a lower Defense Total, decline, or no legal Defense reports `AttackSuccessful`. Accepted comparison clears the same pending Attack and emits ordered outcome/completion events carrying the stable resolution, actor, target, totals, Defense source when present, and typed result. The Phase 2 panel immediately performs this deterministic continuation after an accepted Defense answer, reports either `successfully defended` or `attack successful`, and releases card/actor selection without applying defeat, removal, Life, coin, respawn, or any other consequence. Printed rulebook page 14 was visually verified. T2 validation passed with zero failed, skipped, or inconclusive cases: authority-focused Edit Mode 23/23; final affected Edit Mode 126/126 across Attack, resolver, offer/value, card, participant, movement, occupancy, presentation, controller, and hex fixtures; and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`). Targeted visual inspection confirmed the saved scene clearly renders `Attack 3 vs Defense 3: successfully defended` after the Defense card leaves the hand and the Attack closes. Complete unfiltered suites were intentionally omitted because milestone 2.5 is not the Phase 2 T3 gate and all affected consumers are enumerated. No defeat/removal consequence, Block, card text, modifier, timing, turn, or animation behavior was added.

### [x] 2.6 Add Tie Breaker setup state

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: Normal setup chooses the initial Tie Breaker side; internal tests can explicitly select Red or Blue.

Acceptance checks:

- The result is stored in authoritative state and included in later snapshot history.
- No general gameplay RNG subsystem is introduced.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the Phase 2 focused scenarios.

Completion note (2026-08-10): Added immutable authoritative `TieBreakerState` whose revealed side is always Red or Blue. Normal setup performs one localized coin flip through `TieBreakerSetupAuthority`; the internal deterministic path accepts an explicit Red or Blue side and rejects `None`, without adding a reusable gameplay RNG service. The synthetic 1v1 configuration now carries the resulting state so the initial match snapshot introduced at milestone 3.9 can include it, and existing tests that construct that configuration explicitly pin Red unless setup-side selection itself is under test. Printed rulebook page 5 was visually verified. T3 validation passed with zero failed, skipped, or inconclusive cases: focused affected Edit Mode 53/53 (`TieBreakerSetupTests`, `MatchParticipantsTests`, `ActionOfferAndPrintedValueTests`, and `AttackActionResolutionAuthorityTests`); complete unfiltered Edit Mode 160/160; and complete unfiltered Play Mode 7/7.

### [x] 2.6a Display the authoritative Tie Breaker side

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: The validation HUD shows a top-right Tie Breaker coin indicator that clearly identifies whether the current authoritative side is Red or Blue.

Acceptance checks:

- The indicator derives from `TieBreakerState`; presentation does not choose, cache independently, or mutate the side.
- Red and Blue states are visibly distinct through explicit text/symbol treatment rather than color alone.
- Focused Edit Mode and Play Mode tests cover both deterministic sides and the saved-scene presentation path.
- One targeted visual inspection confirms the top-right indicator is readable and does not overlap existing content.
- T3 corrective gate: both complete unfiltered Edit Mode and Play Mode suites pass because this changes the already-passed Phase 2 presentation boundary.

Non-goals: No Initiative ordering, Tie Breaker flip timing, coin animation, production art, sound, or player control.

Completion note (2026-08-10): Added a non-interactive top-right validation-HUD indicator that reads the configured immutable `TieBreakerState` directly and renders a solid side-colored panel with explicit `TIE BREAKER COIN`, `[R] RED`, or `[B] BLUE` text, so identity never depends on color alone. The controller accepts a complete authoritative placeholder configuration for deterministic presentation tests, does not choose or mutate the side, and blocks pointer input from passing through the indicator to the board. Focused Edit Mode 21/21 covered both deterministic sides, label/color/geometry projection, pointer blocking, and the saved-scene path; focused Play Mode 7/7 covered the live validation scene. Readiness-gated Game-view inspection confirmed a readable Red indicator at the top-right with no overlap. The corrective T3 gate passed complete unfiltered Edit Mode 163/163 and complete unfiltered Play Mode 8/8, all with zero failed, skipped, or inconclusive cases. No Initiative rule, Tie Breaker flip, animation, sound, or control was added.

Phase-gate roadmap audit: After milestone 2.6 passes its T3 gate, audit Phase 3 before assigning the next milestone profile. Confirm that its lifecycle, Initiative, turn, round, snapshot, Undo/restart, and event-log boundaries reach one complete deterministic round through the shortest dependency-safe sequence; recommend any justified keep/merge/split/defer/remove decisions and record the remaining-effort range.

Phase 3 entry audit (2026-08-10): Keep milestones 3.1-3.11 as separate, unchanged micro-milestones; no merge, split, defer, or removal is justified. The shortest dependency-safe path is lifecycle/typed Initiative data (3.1), two-hand selection presentation (3.2), simultaneous play/reveal (3.3), Initiative and Tie Breaker ordering (3.4), the minimum Hold turn (3.5), existing Movement and Attack/Defense turn integrations (3.6-3.7), the four-turn round shell (3.8), then the versioned snapshot boundary (3.9), snapshot-backed Undo/restart (3.10), and the read-only event-log projection plus T3 gate (3.11). This ordering reaches one complete deterministic round before adding persistence tools, and places serialization before every tool that restores or rebuilds history. Remaining effort is 11 separately approved micro-milestones, estimated at 7-12 focused implementation days excluding approval latency and environment outages.

Approved content-readiness amendment (2026-08-10): Insert milestone 3.7b after the completed Attack/Defense turn integration and before the four-turn round shell. The current two-card mirrored synthetic fixtures cannot exercise four card selections per hero, hand depletion through Defense discards, or the rulebook-shaped five-card starting hand. Milestone 3.7b expands only synthetic data and its existing presentation/authority consumers; it does not begin verified hero content or add card-text effects. The remaining Phase 3 path is now five separately approved milestones (3.7b and 3.8-3.11), estimated at 4-7 focused implementation days excluding approval latency and environment outages.

Corrective playable-round amendment (2026-08-10): Insert milestone 3.8a after the completed pure four-turn round skeleton and before snapshot serialization. Validation after milestone 3.8 confirmed that the round authority completes four deterministic turns, while the saved-scene controller still constructs and presents only an isolated revealed-card turn flow. Milestone 3.8a closes that integration gap before snapshots, Undo/restart, and the event log depend on the visible round boundary. The remaining Phase 3 path is four separately approved milestones (3.8a and 3.9-3.11), estimated at 3-5 focused implementation days excluding approval latency and environment outages.

Approved six-hero round-history amendment (2026-08-10), rule-corrected 2026-08-10: Insert milestone 3.8b after the completed visible-round integration and before snapshot serialization. The staged commitment authority and `RoundTurnSlot` previously hard-coded one Red and one Blue response/assignment, while the authoritative rule requires each hero's controlling player to commit exactly one hero-owned card from a nonempty hand or Pass with an empty hand. The approved pitch requires three independently controlled heroes per team plus a later UI that shows which hero played which card in each of four turns. Because `MatchParticipants` enforces exactly one hero per player, `HeroId` is the canonical gameplay/assignment owner; `PlayerId` remains the distinct controller/seat identity and player/team are derived through the participant graph instead of duplicated in each assignment. Milestone 3.8b completed that generalization before snapshots freeze the shape. Milestones 8.25 and 8.26 must later populate and visibly project six assignments, and milestone 9.13 must polish that same read-only history. The remaining Phase 3 path is four separately approved milestones (3.8c and 3.9-3.11), estimated at 3-6 focused implementation days excluding approval latency and environment outages.

## Phase 3 - Test-card Initiative, turns, and rounds

### [x] 3.1 Extend the test-card foundation with turn lifecycle

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Synthetic card instances can represent and transition through the lifecycle facts needed to play a turn without using Wasp data.

Acceptance checks:

- Location, played status, unresolved/resolved status, and face orientation remain orthogonal where the rulebook permits overlap.
- Pure-rule tests cover playing a card Facedown, revealing it, resolving it after an action, and discarding a Defense card from hand.
- Synthetic definitions include validated typed Initiative values, with fixtures for both unequal values and ties.
- Resolved status, discard location, and face orientation remain separate facts rather than mutually exclusive states.
- Test definitions are explicitly synthetic fixtures and cannot become authority for Wasp values or text.

Non-goals: No Wasp inventory, printed card effects, upgrades, items, or end-of-round subsystems.

Completion note (2026-08-10): Extended immutable card definitions with a validated positive `InitiativeValue` and explicit `CardContentAuthority`; every Phase 3 placeholder definition is marked `SyntheticTestFixture`, retains its visible `TEST -` label, and advances to a synthetic-only content revision. The two definitions use unequal Initiative 7 and 4, while the separately owned mirrored hands provide deterministic cross-hero ties without claiming Wasp values. Added a pure `CardLifecycleAuthority` that accepts only the current legal source facts for Hand/face-up/unplayed to Turn Area/Facedown/played/unresolved, reveal to Faceup, and post-action resolution to played/resolved, plus the existing orthogonality-preserving Defense Hand-to-Discard transition. Invalid, repeated, null, and out-of-order attempts reject without state mutation; resolved, discarded, and facedown remain independently representable. Printed rulebook pages 7, 9, and 10 were visually verified. T1 validation passed with zero failed, skipped, or inconclusive cases: focused Edit Mode 19/19 (`CardLifecycleAuthorityTests`, `CardDefinitionInstanceTests`) and final directly affected Edit Mode 115/115 across lifecycle, definitions/offers, participants, Movement/Attack resolution, and existing presentation consumers. Play Mode was intentionally omitted because no Unity-facing path changed. No simultaneous card commitment/reveal orchestration, Initiative ordering, Wasp data/text, effect, upgrade, item, turn, round, or end-of-round behavior was added.

### [x] 3.2 Display both director-mode test hands

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Separate Red and Blue synthetic hands are visible and independently selectable by the director-mode controller.

Acceptance checks:

- Every displayed card resolves to one authoritative owned card instance.
- Team identity is not conveyed by color alone.
- Selecting a card does not play it or resolve an action.

Completion note (2026-08-10): The validation HUD now projects both authoritative synthetic hands simultaneously in separate `[R] RED HERO` and `[B] BLUE HERO` rows. Each displayed entry comes from the Red/Blue hand's stable owner-bound runtime card instance and its matching immutable definition; the explicit symbol-and-text labels ensure team identity is never color-only. A director can select either side's card through the controller, which changes only the current selected card/hero presentation context and does not play, reveal, resolve, or emit a card-resolution event. T2 validation passed with zero failed, skipped, or inconclusive cases: focused Edit Mode 23/23 (`DirectorHandPresentationTests`, `CardDrivenMovementPresentationTests`, and `HeroSelectionControllerTests`) and focused Play Mode 8/8 (`MinimalHexGridPlayModeTests`), including the saved-scene Blue-hand selection/no-lifecycle-transition path. Source/layout inspection confirmed the two 32px hand rows fit inside the enlarged bottom panel, retain the top-right Tie Breaker HUD clearance, and use explicit readable team labels. Complete unfiltered suites were intentionally omitted because milestone 3.2 is not a T3 gate and the affected presentation/controller consumers are enumerated. No card commitment, Facedown transition, reveal, Initiative ordering, action resolution, turn, or round behavior was added.

### [x] 3.3 Play and reveal one test card per side

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The controller selects and confirms one card for each side, both cards enter the correct played/unresolved state, and they reveal together.

Acceptance checks:

- Each side can commit only an eligible card from its own hand.
- Neither side's selection is revealed early.
- Reveal changes authoritative card state; it is not a presentation-only flip.
- Passing is represented when a side has no card available.

Non-goals: No Initiative ordering or action execution.

Completion note (2026-08-10): Added a pure two-side turn-card commitment authority over the existing orthogonal lifecycle transitions. Red and Blue may each commit only one currently eligible own-hand card; accepted commitment moves that exact instance to the Turn Area as played/unresolved/Facedown and emits a side-only event that deliberately omits the card ID. After both sides commit or legally pass, every committed card reveals in deterministic Red/Blue event order and becomes authoritatively Faceup; only those reveal events disclose stable card IDs. Pass is accepted only when that side has no eligible card. Missing, cross-side, ineligible, repeated, stale-state, and illegal-pass commands reject without further mutation, and deterministic replay produces identical states/events. The director HUD adds `CONFIRM FACEDOWN`, generic pre-reveal commitment labels, explicit no-card Pass, and definition-backed revealed name/Initiative labels while leaving Initiative ordering and action execution untouched. Printed rulebook page 7 was visually verified. T2 validation passed with zero failed, skipped, or inconclusive cases: focused Edit Mode 35/35; final directly affected Edit Mode 123/123 across commitment/lifecycle, card/participant/offer/resolver, director/controller, and saved-scene fixtures; and focused Play Mode 9/9 (`MinimalHexGridPlayModeTests`), including the saved-scene joint-reveal path. Source/layout inspection confirmed the two 42px hand rows, 190px commitment labels, confirm control, status, and optional pass control fit the existing 820x270 bottom HUD without overlapping the top-right Tie Breaker indicator. Complete unfiltered suites were intentionally omitted because milestone 3.3 is not a T3 gate and all affected consumers are enumerated. No Initiative ordering, Tie Breaker flip timing, action execution, turn completion, or round behavior was added.

### [x] 3.4 Resolve Initiative and tied Initiative

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The authority identifies the next unresolved actor from current Initiative values and applies the Tie Breaker rules without depending on Wasp cards.

Acceptance checks:

- Initiative comes through the typed value/contribution query rather than a turn-controller field or UI value.
- The highest current Initiative among unresolved cards is queried again after every resolution rather than cached as a complete order.
- A cross-team tie uses the current Tie Breaker side and records a deferred flip that occurs only after the selected tied resolver finishes.
- A same-team tie creates an authoritative pending choice for that team/controller and never flips the Tie Breaker.
- Synthetic multi-hero tests cover more than two tied cards even though the current staged fixture uses one hero per team.

Completion note (2026-08-10): Added a pure `InitiativeResolutionAuthority` that derives each face-up unresolved card's current Initiative through a card-wide typed printed-value contribution query and re-runs that query whenever the next actor is requested. The highest current total acts next. A cross-team tie selects the current Tie Breaker side and records the flip as deferred state/event data attached to that selected resolver; acknowledging an unresolved card rejects without mutation, and the coin flips only after the exact selected card has independently reached its authoritative resolved state. A same-team tie creates an immutable team-owned pending choice with stable sorted card candidates and never flips the coin. When more than two highest cards tie across teams, the Tie Breaker first selects the team and that team then chooses among its own tied cards, preserving the deferred flip. Pending answers revalidate current Initiative, ownership, candidate membership, and tie state before acceptance. Printed rulebook pages 5, 7, and 9 were visually verified. T2 validation passed focused and directly affected Edit Mode 62/62 across Initiative, printed values, lifecycle, definitions, commitment, participants, and Tie Breaker setup, with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path changed, milestone 3.4 is not a T3 gate, and the affected pure-authority consumers are enumerated. No action execution, Hold, card resolution mutation, complete turn, round, effect, or Wasp content was added.

### [x] 3.5 Resolve Hold and complete one turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Both unresolved test cards can perform Hold in Initiative order, become resolved, and end the turn.

Acceptance checks:

- Each hero performs no more than one normal action from its played card.
- A deferred cross-team Tie Breaker flip occurs only after the selected tied card resolves.
- The next actor is re-queried from current unresolved state.
- Turn-only effects have an explicit cancellation checkpoint even though no concrete effect is required yet.

Completion note (2026-08-10): Added a pure `TurnResolutionAuthority` that composes the existing Initiative and card-lifecycle authorities around a reusable general-rule Hold offer. Any current selected face-up unresolved played card can perform exactly one Hold as a General Rule Secondary action with source-card text suppressed; accepted Hold records ordered action/resolution facts, resolves only that card, acknowledges the selected actor, consumes any deferred cross-team Tie Breaker flip only after the resolved fact, and immediately re-queries the highest current unresolved Initiative. Same-team tied actors still use the owning team's authoritative choice and never flip the coin. When no unresolved cards remain, the authority records an explicit `TurnOnlyEffectsCancellationCheckpoint` immediately before `TurnEnded`; no concrete Active effect is implemented. Printed rulebook pages 7 and 12 were visually verified, including the distinct no-respawn/defeat resolve-without-action path, which remains unimplemented. T2 validation passed focused Edit Mode 8/8 and directly affected Edit Mode 70/70 across turn resolution, Initiative, general action offers/printed values, lifecycle, definitions, commitment, participants, and Tie Breaker setup, with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path changed, milestone 3.5 is not a T3 gate, and the affected pure-authority consumers are enumerated. No Movement or Attack execution within a turn, resolve-without-action transition, concrete effect cancellation, round, snapshot, or Wasp content was added.

### [x] 3.6 Resolve test-card Movement within a turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A hero can choose its synthetic Movement offer during its Initiative position, complete the Phase 1 movement flow, resolve its card, and yield to the next actor.

Acceptance checks:

- Movement reuses the allowance, chosen-route, obstacle, command/event, and pending-choice authority from Phase 1.
- The turn controller does not mutate occupancy itself.
- Completing Movement resolves exactly the acting card and continues Initiative order.

Completion note (2026-08-10): Added an explicit played-turn-card action-offer query so the current face-up unresolved card can expose its printed Primary/Secondary actions without weakening the existing in-hand offer path. `TurnResolutionAuthority` now delegates selected Movement start and every destination/Stop answer to the existing `CardActionResolutionAuthority`, which continues to own immutable pending choices, allowance, cumulative route, obstacles, adjacent-step commits, and ordered command/events. Partial waypoints keep the same actor/card unresolved and block another normal action; Stop or allowance exhaustion completes Movement, then and only then resolves exactly the source card, acknowledges any deferred Tie Breaker flip, and re-queries current Initiative. Rulebook pages 7, 10, and 11 were visually re-verified. T2 validation passed focused Edit Mode 11/11 and directly affected Edit Mode 134/134 across turn/Initiative/lifecycle/offers, Phase 1 Movement/obstacle authorities, and legacy card-driven presentation consumers, with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no scene or presentation code changed, the affected consumers are enumerated, and milestone 3.6 is not a T3 gate. No Attack/Defense turn integration, resolve-without-action, round, snapshot, effect, or Wasp content was added.

### [x] 3.6a Connect in-turn Movement to the validation controller

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: After both synthetic cards reveal, the validation controller starts Initiative, exposes the current actor's played-card Movement offers, accepts destination/Stop input through the turn authority, moves the authoritative hero occupancy, resolves the acting card, and visibly advances to the next actor.

Acceptance checks:

- Presentation derives the current actor/card and Movement offers from authoritative turn state rather than restoring the standalone in-hand flow.
- Destination and Stop input resume the same turn-owned Movement pending choice; presentation never mutates occupancy or card state.
- Partial waypoint Movement visibly retains the same actor and refreshes legal destinations; completion updates the hero view and next-actor prompt from authoritative state.
- Focused Edit Mode and Play Mode tests cover the saved-scene reveal-to-Movement-to-next-actor path and invalid/stale input preservation.

Non-goals: No Attack/Defense turn integration, automated action choice, new Movement rules, animation, sound, round shell, or Wasp content.

Completion note (2026-08-10): Corrected the playable validation flow by retaining one presentation-owned reference to the pure turn authority after joint reveal, automatically focusing its current Initiative actor and played card, and deriving only that card's available printed Movement offers through the milestone 3.6 query. Movement selection and every destination/Stop command now pass through `TurnResolutionAuthority`; the controller projects its existing pending choice/events, updates the active hero view only from authoritative occupancy, retains the same actor across partial waypoints, and focuses the next unresolved actor only after exact source-card resolution. Manual hero switching is blocked while the visible turn is active, and stale input preserves actor, card, occupancy, and highlights. T2 validation passed focused Edit Mode 27/27, directly affected Edit Mode 149/149, and saved-scene Play Mode 9/9 with zero failed, skipped, or inconclusive cases. Targeted live Game-view inspection confirmed that joint reveal visibly produces the `[R] RED` Initiative prompt, Primary/Secondary Movement buttons, highlighted legal destinations, and Stop control without overlap. Complete unfiltered suites were intentionally omitted because the affected controller, scene, and pure-authority consumers are enumerated and milestone 3.6a is not a T3 gate. No Attack/Defense turn integration, new Movement rule, animation, sound, round, or Wasp content was added.

### [x] 3.7 Resolve test-card Attack and Defense within a turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A hero can perform the Phase 2 test Attack during its Initiative position, pause for the enemy Defense reaction, resume, resolve the attacking card, and continue the turn.

Acceptance checks:

- The Defense reaction does not consume the defender's normal action.
- The nested response resumes the same pending Attack and turn frame.
- Attack completion resolves only the attacker's played card.
- The next unresolved actor is selected from current state after the reaction closes.

Non-goals: No Attack consequences, defeat/removal, automated action choice, new Attack/Defense rules, animation, sound, round shell, snapshots, or Wasp content.

Completion note (2026-08-10): Composed the existing Phase 2 hero Attack/Defense resolver into the selected revealed turn-card frame without duplicating combat rules. Played-turn Attack offers now remain authority-revalidatable through target, defender-owned response, comparison, and completion checkpoints. The turn authority preserves the attacking actor/card while the Defense reaction discards at most one legal in-hand defender card or declines, then resolves only the attacker's played card, consumes any deferred Tie Breaker flip, and re-queries the next unresolved actor from current state. The validation controller exposes Movement and Attack offers for the current card, routes target and Defense input through the turn authority, retains the attacker across the nested reaction, reports the typed outcome, and visibly focuses the next actor; invalid/stale input preserves the same turn/Attack choice. T2 validation passed focused Edit Mode 13/13, directly affected Edit Mode 99/99, and saved-scene Play Mode 10/10 with zero failed, skipped, or inconclusive cases. Source/layout inspection confirmed the existing Attack, Defense, outcome, and next-actor HUD states reuse the already-validated panel geometry; an attempted automated framebuffer capture was unavailable in batch mode and the hidden live launcher blocked on its interactive save boundary, so no separate screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because the affected resolver, controller, and saved-scene consumers are enumerated and milestone 3.7 is not a T3 gate. No Attack consequence, new combat rule, animation, round, snapshot, or Wasp content was added.

### [x] 3.7b Expand both synthetic heroes to five-card playable hands

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Each current test hero owns five distinct synthetic cards and can commit any eligible card, then perform its supported Primary Attack, Secondary Movement, or general Hold during its Initiative position.

Synthetic fixture matrix (test data only; these names and values are not official content or balance claims):

| Owner | Card | Initiative | Primary action | Secondary Movement | Defense reaction |
| --- | --- | ---: | --- | ---: | ---: |
| Red `primitive-hero` | Red Needle | 13 | non-Ranged Attack 2 | 1 | 1 |
| Red `primitive-hero` | Red Bulwark | 11 | non-Ranged Attack 3 | 1 | 3 |
| Red `primitive-hero` | Red Lancer | 9 | Ranged Attack 2, Range 3 | 2 | 1 |
| Red `primitive-hero` | Red Pursuit | 7 | non-Ranged Attack 4 | 2 | 2 |
| Red `primitive-hero` | Red Bombard | 5 | Ranged Attack 3, Range 2 | 1 | 2 |
| Blue `blue-test-hero` | Blue Feint | 12 | non-Ranged Attack 2 | 2 | 2 |
| Blue `blue-test-hero` | Blue Sentinel | 11 | non-Ranged Attack 3 | 1 | 4 |
| Blue `blue-test-hero` | Blue Scout Shot | 8 | Ranged Attack 2, Range 2 | 3 | 1 |
| Blue `blue-test-hero` | Blue Hammer | 6 | non-Ranged Attack 5 | 1 | 1 |
| Blue `blue-test-hero` | Blue Longbow | 4 | Ranged Attack 3, Range 3 | 2 | 2 |

Acceptance checks:

- Each hand contains exactly five immutable definitions and five globally unique owned instances, with no fixed two-card construction or indexing assumption in production code.
- Every synthetic definition has exactly one printed Primary Attack, one printed Secondary Movement, one in-hand Defense reaction, and the general-rule Hold derived separately; the fixture no longer assigns both Primary Movement and Primary Attack to every card.
- All ten definition/value profiles match the approved fixture matrix, remain explicitly marked synthetic, and preserve the Initiative 11 cross-team tie fixture.
- Commitment, reveal, Initiative, Movement, Attack/Defense, lifecycle, and ownership authorities accept any eligible hand slot and continue to reject stale, cross-owner, discarded, played, or resolved cards without partial mutation.
- Discarding a card for Defense removes only that instance from later normal commitment and Defense choices; the reaction still does not consume the defender's played-card action.
- The director HUD presents both five-card hands and all legal Defense choices legibly without overlap, using wrapping, scrolling, or another bounded layout that remains usable at the validation resolution.
- Focused pure, presentation, and saved-scene tests cover five-card construction, unique identities, the value/action matrix, cross-team Initiative tie behavior, selection from every slot, Defense-driven hand depletion, and representative non-Ranged/Ranged turn resolution.

Non-goals: No official hero identity, official card name/value/text/art, Skill action, card-text effect, modifier, marker, token, Push, Place, minion target, Attack consequence, defeat/removal, balancing, card retrieval, round advancement, snapshot, animation, sound, or Wasp content.

Completion note (2026-08-10): Replaced the mirrored two-card placeholder with ten distinct synthetic definitions and globally unique owned instances matching the approved Red/Blue matrix. Every card now exposes exactly one printed Primary Attack, one printed Secondary Movement, and one in-hand Defense reaction; Hold remains a separately derived general-rule offer. The complete configuration carries both definition sets, and controller continuations resolve defender-owned cards through the complete definition collection rather than relying on mirrored IDs. Five-card commitment/action eligibility, Defense-driven depletion, the Initiative 11 cross-team tie, non-Ranged and Ranged turn resolution, and bounded two-row hand/Defense presentation are covered by focused tests. T2 validation passed focused final Edit Mode 20/20, directly affected Edit Mode 150/150, and saved-scene Play Mode 10/10 with zero failed, skipped, or inconclusive cases. A follow-up live Game-view inspection by the user confirmed that both Red and Blue hands visibly show five cards; automated screenshot attempts were not retained because their capture launchers did not produce a reliable saved-scene image. Source/layout inspection confirms five minimum-width card buttons per dedicated 820-pixel-wide hand row and five minimum-width Defense controls fit the expanded bounded panel at the QHD validation view. Complete unfiltered suites were intentionally omitted because milestone 3.7b is not a T3 gate and its data, authority, controller, and saved-scene consumers are enumerated. No official content, card-text effect, Attack consequence, round, snapshot, animation, sound, or Wasp behavior was added.

### [x] 3.8 Complete the four-turn round skeleton

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Four turns advance, cards occupy the correct turn slots, and the round reaches an explicit end-of-round phase.

Acceptance checks:

- A card cannot be replayed from the wrong state.
- Passing behavior is represented when no card remains.
- Turn and round indices are authoritative and deterministic.
- End-of-round entry is explicit but does not yet resolve its subsystems.

Completion note (2026-08-10): Added a pure `RoundResolutionAuthority` that composes a fresh commitment/reveal authority and the existing Initiative/action turn authority for each of exactly four indexed turns. Every revealed commitment is recorded in an immutable round/turn slot with a Red card or explicit Pass and a Blue card or explicit Pass; resolved/discarded/otherwise ineligible cards remain rejected by the existing lifecycle authority, and Pass remains legal only when that side has no eligible hand card. Hold, Movement, Attack/Defense, pending choices, ordered nested events, and deferred Tie Breaker flips continue through the existing authorities; the round exposes the live current-turn Tie Breaker state and carries its final value into the next turn. After turn 4 it enters a distinct end-of-round phase and emits an explicit boundary event without retrieving cards or running any later cleanup, Lane Push, leveling, or progression subsystem. Printed rulebook page 7 was source-verified. T2 validation passed focused Edit Mode 7/7 (`RoundResolutionAuthorityTests`) and directly affected Edit Mode 147/147 across round, turn, Initiative, commitment, lifecycle, offers/values, definitions, participants, Tie Breaker, Movement, Attack/Defense, occupancy, and obstacle authorities, with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path changed, all directly affected pure-authority consumers are enumerated, and milestone 3.8 is not a T3 gate. No card retrieval, new-round start, end-of-round subsystem resolution, snapshot, Undo, event-log UI, resolve-without-action, consequence, presentation, or official content was added.

### [x] 3.8a Connect the complete round to the validation controller

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The saved validation scene uses one authoritative round instance to play all four indexed turns from commitment through action resolution and visibly reaches the explicit end-of-round phase.

Acceptance checks:

- The validation controller creates and retains one `RoundResolutionAuthority`; it does not replace the round with an isolated commitment, Initiative, or turn authority after reveal.
- Red and Blue commitment, legal Pass, joint reveal, current-actor selection, Hold, Movement, Attack/Defense, and every pending answer route through the round's current turn without presentation-owned state mutation.
- After both revealed cards resolve, the completed immutable slot remains recorded and the controller visibly advances to the next indexed turn with only currently eligible hand cards; a Defense-discarded or previously played card cannot reappear as a legal commitment or reaction.
- Tie Breaker state carries visibly and authoritatively between turns, including a deferred flip completed during one turn before the next turn begins.
- After turn 4, the scene clearly displays the explicit end-of-round phase and rejects further commitment or turn commands without mutating round, card, occupancy, or event state.
- Focused Edit Mode and saved-scene Play Mode scenarios exercise one uninterrupted four-turn round containing representative Hold, Movement, Attack/Defense, Defense-driven hand depletion, and stale/invalid input preservation.

Non-goals: No card retrieval, next-round start, end-of-round subsystem resolution, snapshots, Undo/restart, event-log panel, automated action choice, Attack consequences, animation, sound, official content, or production UI.

Completion note (2026-08-10): Replaced the validation controller's disconnected commitment and turn authorities with one retained `RoundResolutionAuthority` for the complete saved-scene round. Red/Blue commitments and legal Pass, joint reveal, Initiative-selected actor focus, general Hold, printed Movement and Attack, target/Defense answers, stale-choice rejection, deferred Tie Breaker flips, immutable completed slots, and turn advancement now route through the round boundary; hero focus changes preserve that same authority and its board resolver. The director HUD labels the authoritative round/turn/phase, exposes Hold beside the printed actions, removes played or Defense-discarded cards from later hand projections, and reports `END OF ROUND` after turn 4. Post-round commands reject without changing slots, events, cards, or occupancy. T2 validation passed focused Edit Mode 25/25 (`CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, and `RoundResolutionAuthorityTests`), directly affected Edit Mode 186/186 across the round/turn/Initiative/commitment/lifecycle/action/value/participant/Tie Breaker/Movement/Attack/controller/scene fixtures, and saved-scene Play Mode 11/11 (`MinimalHexGridPlayModeTests`), with zero failed, skipped, or inconclusive cases. The saved-scene scenario performs one uninterrupted four-turn round containing Attack/Defense with hand depletion, Movement with stale-input preservation, Hold, a deferred Tie Breaker flip, and visible end-of-round rejection. Source/layout inspection confirmed the expanded phase header, status line, and Hold control reuse the bounded validation HUD; the batch test path provides no reliable framebuffer capture, so no screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because milestone 3.8a is T2 rather than the Phase 3 T3 gate and all directly affected consumers are enumerated. No retrieval, next round, end-of-round subsystem, snapshot, Undo, log panel, consequence, animation, sound, official content, or production UI was added.

### [x] 3.8b Generalize turn commitments and round slots for hero ownership

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: For every configured hero, its controlling player must commit exactly one hero-owned card when that hero's hand is nonempty and may Pass only when it is empty; after every hero has a response, each indexed turn slot stores the resulting immutable ordered hero-assignment collection without assuming exactly one hero per team.

Acceptance checks:

- Replace side-owned commitment responses with hero-owned responses keyed by canonical `HeroId`. For each configured hero, a nonempty authoritative hand requires exactly one owned card commitment and an empty hand requires an explicit Pass; hero board presence never permits Pass, and an invalid in-hand lifecycle state rejects as an authority error rather than being treated as an empty hand.
- Joint reveal occurs only after every configured hero has independently received a commitment or Pass response from its controlling player. Commitment and assignment construction validate card ownership and the participant graph, reject unknown or duplicate hero/card identities and responses, and require exactly one response and assignment for every expected hero.
- Replace fixed Red-card/Red-pass and Blue-card/Blue-pass slot members with immutable entries carrying canonical `HeroId` plus exactly one committed card ID or explicit empty-hand Pass outcome. Resolve `PlayerId` and team through `MatchParticipants`; do not duplicate them as independently supplied assignment facts, and reject a broken one-player-to-one-hero mapping.
- The current synthetic 1v1 fixture produces exactly two deterministic responses and assignments per turn while preserving existing reveal, Initiative, action, Tie Breaker, slot-history, and end-of-round behavior.
- Slot and round events expose the collection without mutable aliases or Red/Blue positional assumptions, so milestone 3.9 can serialize it without a known six-player schema replacement.
- Focused and directly affected Edit Mode tests cover mandatory commitment from a nonempty hand, Pass from an empty hand, illegal Pass while any owned card remains in hand, the same requirement for an off-board hero, invalid in-hand state rejection, wait-for-every-player reveal, deterministic ordering, immutability, malformed assignment rejection, four-turn history, and unchanged visible-round controller projections.

Non-goals: No six-player setup, additional heroes or cards, 3v3 turn execution, snapshot serialization, history UI, event-log UI, networking, or production presentation.

Completion note (2026-08-10): Replaced the staged Red/Blue response dictionaries with a pure commitment authority configured from the participant graph and keyed by canonical `HeroId`. Every configured hero now receives exactly one independent response: an owned initial in-hand card is mandatory when available, Pass is legal only when that hero has no card in hand, board presence is not an input, and any malformed in-hand lifecycle fact rejects explicitly instead of authorizing Pass. Unknown heroes/cards, cross-owner cards, duplicate identities/responses, stale commitments, and malformed assignment collections reject without further mutation. Reveal waits for all configured heroes and emits deterministic hero-ordered events. `RoundTurnSlot` now copies an immutable, ordinally ordered collection of `HeroId` plus committed-card-or-empty-hand-Pass assignments, validates exact participant coverage and card ownership, and derives controlling `PlayerId` and team through `MatchParticipants` rather than storing them as assignment inputs. The existing 1v1 controller translates its two visible team hands to their canonical hero IDs and retains the same four-turn Initiative/action/Tie Breaker/end-of-round flow. Printed rulebook page 7 was visually verified. T2 validation passed focused Edit Mode 7/7 (`RoundResolutionAuthorityTests`) and directly affected Edit Mode 82/82 across round, turn, Initiative, commitment, lifecycle, definition/instance, participant, and existing controller projection fixtures, with zero failed, skipped, or inconclusive cases. Play Mode was intentionally omitted because no scene, input, layout, or presentation behavior changed; the affected Unity-facing adapter remains a read-only 1v1 projection. Complete unfiltered suites remain reserved for the Phase 3 gate at milestone 3.11. No six-player setup, new hero/card content, 3v3 execution, snapshots, history/event-log UI, networking, or production presentation was added.

### [x] 3.8c Scaffold end-of-round progression and repeated synthetic rounds

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: After the fourth turn, the synthetic round advances through an explicit resumable end-of-round stage sequence, retrieves eligible cards, preserves the completed round, and starts turn 1 of the next round so the validation scene can play at least two uninterrupted rounds.

Acceptance checks:

- Add one authoritative end-of-round stage cursor whose ordered checkpoint identities match the verified sequence later completed by milestones 6.1-6.8 and 7.5-7.8; pending choices or a game-ending result can pause or stop the cursor without presentation-owned continuation.
- The current visibly test-only fixture declares which later match subsystems are unavailable. A stage may report a typed `NotApplicable` outcome only from authoritative fixture capability/state facts; a missing handler for a subsystem declared available is an authority error, never a silent skip.
- Implement the reusable owner-validated retrieval subset needed by the current fixture: every eligible resolved or Defense-discarded synthetic card returns to its owning hero's hand through explicit lifecycle events, ineligible cards do not move, and the retrieval checkpoint exposes the cancellation boundary even though the fixture has no concrete Active effects.
- After every applicable stage completes, freeze the four indexed turn slots as an immutable completed-round record, increment the authoritative round index, create fresh empty turn slots, retain match-wide state including the current Tie Breaker, and enter turn 1 commitment without reconstructing state from presentation.
- The saved validation scene continues from its visible end-of-round state into the next round and can complete at least two uninterrupted rounds containing representative Hold, Movement, Attack/Defense, legal card reuse after retrieval, and deterministic Tie Breaker carryover.
- Focused pure and saved-scene tests cover stage order, resumable cursor state, explicit unavailable/not-applicable outcomes, missing-required-handler rejection, owner-safe retrieval, prior-round immutability, round-local reset, match-state carryover, and two-round deterministic replay.

Non-goals: No Minion Battle behavior, token/marker cleanup behavior, Lane Push resolution, concrete End of Round effects, leveling/upgrades, Pity Coins, victory implementation, full-map setup, snapshots, Undo/restart, event-log UI, official content, networking, or production presentation. Later milestones populate the established checkpoints rather than redesigning the round boundary.

Completion note (2026-08-10): Added one pure authoritative cursor over the verified rulebook order: Minion Battle, token/marker cleanup, requested Lane Push, card retrieval/Active-effect cancellation, Level Up/upgrades, and Pity Coins. The synthetic fixture explicitly declares only retrieval available; unavailable stages produce typed `NotApplicable` outcomes, while an available stage without a handler rejects without mutation. Handlers can pause or stop on a game result without advancing the cursor. The populated retrieval handler returns only owner-validated resolved played cards and Defense-discarded cards to fresh hand state, emits one lifecycle event per card, and exposes the Active-effect cancellation checkpoint. After all stages terminate, the authority freezes the four ordered slots and stage outcomes in an immutable completed-round record, increments the round, resets round-local cursor/slots, retains Tie Breaker and other match-wide state, and creates the next `HeroId`-owned commitment authority. The validation HUD exposes the current checkpoint and a continuation command; its saved scene completed two uninterrupted rounds with Hold, Movement, Ranged Attack/Defense, retrieved-card reuse, deterministic Tie Breaker carryover, and two immutable history records. Printed rulebook page 8 was visually verified. T2 validation passed focused pure Edit Mode 17/17, focused integrated Edit Mode 36/36, directly affected Edit Mode 193/193, and saved-scene Play Mode 11/11 with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed the continuation control shares the existing action row and does not expand the bounded HUD; the batch path did not provide a reliable framebuffer capture, so no screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because milestone 3.8c is not the Phase 3 gate and the affected consumers are enumerated. No deferred end-of-round subsystem behavior, concrete Active effects, victory logic, snapshots, Undo/restart, history/event-log UI, official content, networking, or production presentation was added.

### [x] 3.9 Serialize and restore authoritative snapshots

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A versioned snapshot round-trips without changing logical state.

Acceptance checks:

- Teams, players, heroes and their enforced one-to-one player/hero relationship, Tie Breaker, current and completed round state including every collection-backed hero card-or-empty-hand-Pass assignment, the end-of-round stage cursor/outcomes, board occupancy, card instances/state facets, and definition references survive round-trip tests.
- A state paused on a pending card choice or Defense reaction resumes to the same result and ordered events.
- Snapshot schema version and card-definition content revision are checked independently; mismatches are rejected clearly until a migration exists.
- Presentation can rebuild from restored state.

Non-goals: No player-facing save slots or cloud storage.

Completion note (2026-08-10): Added a pure schema-versioned authoritative snapshot codec with deterministic Base64 binary serialization. It round-trips the participant graph and enforced player/hero relationship, current Tie Breaker and Initiative state, current and completed round slots with collection-backed hero/card-or-empty-hand-Pass assignments, end-of-round cursor/outcomes, board and obstacle occupancy, card definition references plus every orthogonal card-state facet, and active commitment/turn continuations. Movement and Attack/Defense restoration reuses the existing resolver-owned validators to re-derive legal pending choices against restored board, definitions, instances, participants, and printed values; a paused target choice or Defense reaction resumes to the same ordered result events. Snapshot schema mismatches and card-definition ID/content-revision mismatches reject with distinct typed reasons until migrations exist. The validation controller can capture and restore the authoritative snapshot, replace its synthetic hands with restored instances, reposition hero/obstacle views from authority, and rebuild pending Movement/Attack highlights without presentation-owned continuation. T2 validation passed focused Edit Mode 21/21, directly affected Edit Mode 154/154, and saved-scene Play Mode 11/11 with zero failed, skipped, or inconclusive cases. Complete unfiltered suites were intentionally omitted because this is a new version-1 schema rather than a migration, affected consumers are enumerated, and milestone 3.11 remains the Phase 3 T3 gate. No player-facing save slot, cloud storage, migration, Undo/restart history, or event-log UI was added.

### [x] 3.10 Add director-mode Undo and restart

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Undo restores the snapshot before the last committed command; restart restores the initial scenario.

Acceptance checks:

- Invalid commands do not create undo entries.
- Undo restores authoritative card, turn, Tie Breaker, pending-resolution, and board state plus presentation.

Non-goals: No redo requirement and no competitive availability.

Completion note (2026-08-10): Added full-match director history over the existing deterministic authoritative snapshot serializer. Every accepted commitment, Pass, Initiative tie choice, Hold, Movement/Attack start or pending answer, Defense response, and end-of-round continuation records the exact pre-command snapshot; rejected and presentation-only inputs add no entry. Undo pops and restores that boundary without creating a replacement entry, while restart restores the initial scenario and clears history. Restore rebuilds owned hands, board/obstacle/hero views, the current Initiative actor/card focus, pending Movement or Attack highlights, Defense controls, Tie Breaker, round/turn state, and HUD status. The saved director HUD now exposes disabled-when-unavailable `UNDO` and `RESTART` buttons in its existing header row. T2 validation passed focused Edit Mode 23/23, directly affected Edit Mode 156/156, and saved-scene Play Mode 12/12 with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed the two compact buttons share the existing header row without increasing the bounded panel height. Complete unfiltered suites were intentionally omitted because milestone 3.11 remains the Phase 3 T3 gate and all affected snapshot/authority/controller/presentation consumers are enumerated. No redo or competitive availability was added.

### [x] 3.11 Add the internal rule-event log

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: A primitive internal panel shows accepted ordered rule events and rebuilds consistently after restore, Undo, or restart.

Acceptance checks:

- Each entry shows sequence, event type, stable rules-object references, and the key value/choice needed to diagnose the event.
- Rejected commands do not appear as accepted state-change events; any rejection display is clearly separate.
- Undo removes events after the restored command boundary, restart clears to setup events, and snapshot restore reproduces the same log projection.
- The log is a read-only projection and cannot mutate or continue rules resolution.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No final visual styling, export format, analytics, telemetry, or player-facing match history.

Completion note (2026-08-10): Added a deterministic read-only projection over accepted ordered round, commitment, Initiative, turn, Movement, Attack/Defense, and end-of-round events. Every immutable entry carries a contiguous sequence, the most specific semantic event type, stable round/turn and applicable hero/card/action/resolution references, and the key value, choice, route, total, outcome, or checkpoint needed for diagnosis; Facedown commitments continue to hide their card identity until reveal. Rejected commands remain only in the existing separate status path and never append an accepted event. The unchanged authoritative snapshot serializer is wrapped by a versioned controller envelope containing the projection, so external restore and accepted-command Undo reproduce the exact prior log while restart returns to the initial setup entry; the projection cannot mutate or continue rules resolution. The saved validation HUD exposes a compact `EVENT LOG (N)` / `HIDE EVENT LOG` control and a scrollable primitive internal panel whose expanded bounds block board input. T2 evidence passed focused Edit Mode 24/24, directly affected Edit Mode 157/157, and saved-scene Play Mode 12/12; the T3 Phase 3 gate passed complete unfiltered Edit Mode 228/228 and Play Mode 13/13, all with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed the panel uses unused right-side space when available, falls back above the director panel, remains collapsible, and preserves the existing bounded director panel; two batch framebuffer attempts produced no image because the batch player exposes no capturable end-of-frame/framebuffer, so no screenshot evidence is claimed. No final styling, export, analytics, telemetry, editable history, or player-facing match history was added.

Phase-gate roadmap audit: After milestone 3.11 passes its T3 gate, audit Phases 4-7 before assigning the next milestone profile. Reassess milestone packaging and sequencing around the shortest dependency-safe path to milestone 7.9's complete deterministic synthetic match, while preserving the full-map, general-rule, snapshot, progression, and victory evidence required by that hard gate; recommend any justified keep/merge/split/defer/remove decisions and record the remaining-effort range.

Phase 4-7 audit (2026-08-10): Keep milestones 4.1-4.4, 5.1-5.8, 6.1-6.8, and 7.1-7.9 separate and in their current order. No merge is justified because each item owns a distinct source/data, rules-authority, continuation, presentation, or validation boundary; the deliberately non-executable Lane-Push prefix at 6.5 must remain separate from the live transition at 6.6. No further split is justified now that Phase 3 has proven stable snapshot, Undo, event-log, round-cursor, and pending-choice seams. Nothing can be deferred or removed without weakening milestone 7.9's required full-map Movement/combat, minion/token/Lane-Push, defeat/respawn, progression, victory, or exact end-of-round evidence. The shortest dependency-safe path is therefore the existing sequential path through the Phase 4 map/setup gate, Phase 5 general combat gate, Phase 6 Lane/end-of-round component gate, Phase 7 progression/victory integration, and milestone 7.9 hard gate. Remaining effort is 29 separately approved micro-milestones, estimated at 26-43 focused implementation days excluding approval latency, source ambiguity, and environment outages.

Post-4.1 multi-map readiness correction (2026-08-11): The reviewed inventory is valid, but an independent audit found that the executable board/snapshot state does not yet carry selected-map identity, the data contract explicitly scopes only `SpaceId` rather than every map-owned child identity, and the validator combines reusable structural checks with `official-single-lane`-specific counts and digest checks while omitting rules-relevant zone metadata from that digest. Split the former milestone 4.2 into corrective milestone 4.1a for the general identity/validation boundary, milestone 4.2 for selected-map authoritative loading and snapshot binding, and milestone 4.2a for Unity placeholder presentation. This adds no second playable map: bounded synthetic definitions may reuse coordinates and child IDs only as automated collision/singleton tests. The revised shortest dependency-safe path contains 30 remaining separately approved micro-milestones through 7.9, estimated at 28-46 focused implementation days under the same exclusions.

## Phase 4 - Full official multi-zone board

### [x] 4.1 Inventory the complete single-lane board reference

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Every part of the complete official single-lane board - including all spaces, connections, terrain, zones, spawn points, Thrones, and the starting Battle Zone - has a reviewed data specification.

Acceptance checks:

- Every logical space has a stable ID and coordinate.
- Every zone boundary, zone membership, spawn point, and Throne is checked against the authoritative visual reference.
- The inventory covers the full map rather than a reduced movement-test area.

Non-goals: No production board art or models.

Completion note (2026-08-11): Added the reviewed, schema-versioned, map-scoped `official-single-lane` revision 1 JSON specification from the user-supplied axial-coordinate diagram, with all 292 stable map-local coordinate IDs, 210 playable spaces, 82 terrain cells, seven zone memberships and boundaries, both Throne zones, and the starting Battle Zone. Standard axial touching derives 809 undirected physical connections and 530 playable-to-playable connections. All 40 spawn markers are inventoried at exact coordinates and split symmetrically: each team has three Hero, eleven Melee minion, three Ranged minion, and three Heavy minion points. The identity contract composes `MapId` plus map-local `SpaceId`, keeps zones/spawns owned by the selected map revision, and forbids singleton-map or globally unique-coordinate assumptions so future maps can coexist. Source review used the preserved private diagram, rulebook page 3 terminology, row/color and marker-shape cross-checks, and a regenerated debug view. T0 schema/content validation passed through `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1`, including exact counts, references, connection derivation, team/type symmetry, and reviewed digest `addb4cdea8f2e0733233bd7fc047bc6cdd3b934102b48990699a524cfb7158bb`. Unity suites were intentionally omitted because no executable Unity or presentation path changed. No loader, scene geometry, production art/models, zone adjacency, or ordered Lane relationship was added. The post-completion multi-map audit identified explicit child-identity, reusable-validation, and selected-map snapshot gaps; corrective milestone 4.1a and the split 4.2/4.2a sequence own those follow-ups without invalidating this reviewed inventory.

### [x] 4.1a Harden map-scoped identity and validation contracts

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The map specification and source-validation boundary can admit additional future map definitions without weakening the reviewed `official-single-lane` evidence or relying on globally unique child IDs.

Acceptance checks:

- `SpaceId`, `ZoneId`, and `SpawnPointId` are explicitly map-local; any persisted, cross-map, catalog, or external reference composes the stable `MapId` with the applicable child ID.
- The selected map is identified by an immutable `MapId` plus content revision, and coordinate-based rule/query APIs are explicitly local to that one selected match map rather than globally qualified individually.
- Reusable schema/invariant validation is separated from `official-single-lane` source-review checks; the reusable layer contains no official-map IDs, counts, or digest.
- Official-map validation still enforces every reviewed count/location and protects all rules-relevant zone metadata, including zone identity, kind, team, Throne status, and starting-Battle-Zone status.
- A bounded synthetic validation fixture may reuse coordinates and child IDs under a different synthetic `MapId` to prove scoping, but it is not retained or presented as another playable map.
- T0 documentation/source validation passes; no Unity suite is required because no Unity runtime or presentation path changes.

Non-goals: No authoritative map loader, snapshot schema change, Unity scene geometry, second playable map, zone adjacency, ordered Lane relationship, or production art.

Completion note (2026-08-11): Extended the schema identity contract so `SpaceId`, `ZoneId`, and `SpawnPointId` are explicitly map-local; globally persisted/catalog/external/cross-map child references compose `MapId`, while a match selects one immutable `(MapId, revision)` and keeps coordinate queries local to it. Extracted reusable schema/invariant validation into `Tools/BoardMapSpecification.Validation.ps1`, with no official map ID, reviewed count/location, or digest in that layer. The official validator retains exact reviewed map assertions and now protects zone identity, kind, team, explicit Throne status, starting-Battle-Zone status, space classification/membership, and spawn identity/location/team/type under digest `5b4e3a2a36aeabaaa7db73083088174ca60978fd10d5e09c90e6a67b0a1733f0`. Two bounded in-memory synthetic definitions reuse the same coordinate and all three child IDs under distinct synthetic `MapId` values, proving composed-reference isolation without retaining another playable map. T0 source validation and `git diff --check` passed; Unity suites were intentionally omitted because no Unity runtime or presentation path changed. No loader, snapshot change, scene geometry, second playable map, zone adjacency, Lane order, or production art was added.

### [x] 4.2 Load the selected map definition into authoritative state

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A match loads one selected map definition through a reusable boundary, and authoritative state plus snapshots retain and validate that map's stable identity and content revision.

Acceptance checks:

- A reusable loader/catalog boundary resolves a requested `MapId` and content revision without a singleton instance, fixed official-map resource path in rules code, or map-name branches.
- The loaded immutable map definition owns its spaces, terrain, zones, and spawn points; existing coordinate-based movement/query authorities operate within that selected definition.
- The authoritative match/board exposes its immutable selected-map reference rather than requiring every internal coordinate value to repeat `MapId`.
- The versioned authoritative snapshot serializes `MapId` and map content revision, resolves them through the supplied current map definitions, and rejects missing or mismatched map content with a typed reason.
- Focused pure tests use bounded synthetic definitions that reuse coordinates and map-local child IDs to prove isolation, selection, and snapshot mismatch rejection without adding another playable map.
- Existing movement, Attack, round, snapshot, Undo, restart, and event-log regressions remain green with the official map definition selected.

Non-goals: No Unity scene replacement, second playable map, zone adjacency queries, Fast Travel, lane pushing, setup placement, or production art.

Completion note (2026-08-11): Added immutable pure map identity/content models for map-local spaces, zones, and spawn points plus an exact `(MapId, content revision)` catalog boundary. A generic Unity JSON adapter loads the reviewed `official-single-lane` revision 1 resource without putting an official-map path or map-name branch in rules code; the executable resource is byte-identical to the reviewed specification. `AdjacentMoveAuthority` now owns and exposes the selected immutable map definition/reference and derives its legal coordinate set from that definition's 210 playable spaces, excluding its 82 terrain cells while retaining all 292 owned cells, seven zones, and 40 spawn points. Snapshot schema version 2 writes the selected map ID/revision instead of duplicating map coordinates, resolves that reference only through supplied current definitions, and rejects missing-map and map-revision mismatch cases with distinct typed reasons. Bounded synthetic fixtures reuse the same coordinate, `SpaceId`, `ZoneId`, and `SpawnPointId` under different map IDs to prove selection isolation. Existing temporary-scene geometry remains unchanged, but its authority now selects the official definition; two stale test expectations were corrected so official terrain is no longer treated as playable. T2 validation passed: source map validation and `git diff --check`; focused Edit Mode 6/6 (`BoardMapDefinitionTests`, `AuthoritativeSnapshotSerializerTests`); directly affected Edit Mode 138/138 across map, adjacent movement, reachability/session, Attack/action, turn/round, snapshot, Undo/restart, event-log/controller, and scene fixtures; and focused Play Mode 12/12 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases. Complete unfiltered suites were intentionally omitted because this is not a phase gate or backward snapshot migration and the affected consumers were enumerated. No scene replacement, second playable map, zone adjacency query, Fast Travel, Lane Push, setup placement, or production art was added.

### [x] 4.2a Upgrade the test grid to the full official placeholder map

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The temporary Phases 1-3 test grid is replaced in the main validation scene by placeholder geometry driven by the selected complete official multi-zone single-lane map definition.

Acceptance checks:

- The scene consumes the selected loaded map definition rather than rebuilding topology from a radius or a hard-coded official-map coordinate list.
- Logical adjacency matches the reviewed board specification.
- Terrain and spawn-point classifications are inspectable.
- Every official zone is visibly distinguishable in debug presentation without implying final art.
- The complete map can be framed and inspected under the current fixed-camera test setup.
- Focused Edit Mode and saved-scene Play Mode checks prove presentation rebuilds from the selected map and does not own authoritative topology or map selection.

Non-goals: No second playable map, polished environment, Fast Travel, lane pushing, or production camera controls.

Completion note (2026-08-11): Replaced the radius-2 scene generator with a selected-definition-fed placeholder map view. The validation controller supplies the same immutable `official-single-lane` revision 1 definition used by authoritative movement; the view creates all 292 definition-owned cells at axial-derived positions, exposes stable space/kind/zone metadata, keeps only the definition's 210 playable cells interactive, and renders its 82 terrain cells separately. All seven official zones receive distinct debug-only fills, and 40 typed spawn markers expose their stable spawn/space/team/unit classifications. The markers reproduce the reviewed legend's eight team/type combinations: Red/Blue outline colors paired with Hero circles, Melee diamonds, right-pointing Ranged triangles, and Heavy hexagons. The unchanged fixed top-down camera is automatically centered and orthographically framed from the selected definition's bounds, while the existing heroes, cards, movement, Attack/Defense, round, Undo/restart, and event-log presentation continue to operate. Post-handoff corrections removed the two legacy Phase 1 placeholder obstacles at `(1,0)` and `(0,1)`, replaced the initial shared-diamond spawn rendering with the reviewed four-shape legend, and bound the Red/Blue synthetic hero views through stable `red-hero-01`/`blue-hero-01` references to definition-owned coordinates `(-2,-5)`/`(-3,10)` instead of the obsolete adjacent center cells. Saved-scene regressions require zero placeholder obstacles, all eight marker combinations, and exact team Hero-spawn placement; distant-spawn scene flows use Hold/Movement while pure focused fixtures retain adjacent Attack/Defense evidence. T2 validation passed focused Edit Mode 10/10 (`MinimalHexGridSceneTests`), directly affected Edit Mode 45/45 (`BoardMapDefinitionTests`, `PrimitiveHeroViewTests`, `HeroSelectionControllerTests`, `CardDrivenMovementPresentationTests`, and `MinimalHexGridSceneTests`), and saved-scene Play Mode 12/12 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. The final hero-spawn correction additionally passed combined `PrimitiveHeroViewTests`/`MinimalHexGridSceneTests` Edit Mode 12/12 and the complete saved-scene Play Mode fixture 12/12. Targeted 1600x1200 camera renders were inspected after correction: the complete footprint is framed, every zone and terrain remain distinguishable, all eight spawn types are legible, both hero primitives are centered on their configured team Hero markers, and no legacy obstacle placeholders appear. Complete unfiltered suites were intentionally omitted because milestone 4.4 remains the Phase 4 T3 gate and the affected map/view/controller/scene consumers are enumerated. No second map, authoritative topology/selection in presentation, Fast Travel, Lane Push, production camera control, or polished environment was added.

### [ ] 4.3 Define and validate zones and zone adjacency

Required Codex profile: GPT-5.6 Sol with medium reasoning.

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

- The setup configuration declares its required `MapId` and map content revision.
- Setup creation requires the exact selected map definition and rejects an unavailable or mismatched map identity/revision.
- Every configured occupied space and spawn point resolves within that selected map; map-local IDs are never resolved against another map.
- Three Wave counters and four Life counters per team.
- Standard single-lane minions are created from configuration with stable identity, team, type, and occupied-space data.
- The visible standard minions use the circular artwork in the private reference atlas `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg`: Steam/orange maps to project Red and Crystal/light-blue maps to project Blue; within each team, the round riveted/broad spiked face is Heavy, the narrow helmet with cheek guards/armored shoulders is Melee, and the two-pronged helmet/narrow crystal crest is Ranged. The source manifest incorrectly labels the Crystal melee sprite `Melee_Steam`; implementation must use this reviewed mapping, select art from authoritative team/type data, and never infer rules identity from the image or manifest label alone.
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
- Focused synthetic scenarios integrate and regress both milestone 2.3's non-Ranged adjacent targeting and milestone 2.3a's Ranged targeting through typed Range/distance queries within the complete Attack/Defense order.
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

Observable outcome: The milestone 3.8c retrieval subset is completed and integrated with the full Phase 6 state so each hero retrieves only its own eligible cards and all applicable effects end through authoritative state changes at the established checkpoint.

Acceptance checks:

- Retrieval never takes another hero's card.
- Every eligible card returns regardless of its prior eligible state, while ineligible/set-aside/item cards do not.
- Applicable Active effects, including relevant Next Turn effects, cancel through authoritative events.
- The populated retrieval/cancellation handler replaces the synthetic fixture's limited availability declaration without changing the end-of-round stage identity, continuation, or event-order contract.
- Focused tests cover resolved, discarded, facedown eligible, other-owner, and active-effect cases.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused Phase 6 scenarios.

## Phase 7 - Progression, victory, and the complete synthetic core loop

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

Observable outcome: The milestone 3.8c end-of-round state machine has every required handler populated, executes the completed systems in source order, and arrives at a valid next round or game-ended state without any synthetic unavailable-stage declarations.

Acceptance checks:

- The sequence runs Minion Battle; token removal/marker return; requested Lane Push; card retrieval/cancellation; mandatory leveling/upgrades; and Pity Coin collection at their exact checkpoints.
- Applicable End of Round effects and immediate victory checks occur at rules boundaries rather than presentation timing.
- A game-ending result stops later processing immediately.
- Focused scenarios cover no Lane Push, one Lane Push, level/no-level players, retrieved effects, Pity Coin eligibility, and a game-ending Push.
- The same state and choices produce the same state and ordered events.
- Integration preserves the established resumable cursor, completed-round history, round-local reset, snapshot, Undo, and event-log contracts rather than introducing a second round-transition path.

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

Hard-gate roadmap audit: After milestone 7.9 passes its T3 gate and is reviewed, audit readiness for verified Wasp content before assigning the next milestone profile. Confirm that the synthetic core loop has retired the intended architecture and integration risks, identify unresolved prerequisites that must remain ahead of Wasp implementation, recommend any justified changes to Phase 8's inventory/mapping entry sequence, and record the remaining-effort range. This audit cannot waive milestones 8.1 or 8.2 or authorize Wasp-specific implementation.

## Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work extends proven core systems; it does not replace them. The mirrored Wasp setup identities introduced earlier remain an intermediate configuration for the first complete hero gate, not the final pitch roster. After that gate, five additional user-approved heroes enter through source inventory and capability-delta mapping before the final six-distinct-hero 3v3 integration. Once the complete primitive match and URP baseline are proven, a separate private-online track moves the same authority behind a host/session boundary and reaches an invite-only six-human internet playtest before production presentation begins.

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
- The designated roadmap audit applies the verified inventory and capability matrix to the remaining Wasp milestones through 8.16. It recommends justified keep/merge/split/defer/remove decisions, records an updated remaining-effort range, and keeps dependencies ahead of the first Wasp card that needs them.
- Approved remaining Wasp milestones through 8.16 are reordered or split so dependencies precede the first Wasp card that needs them.
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

### [!] 8.16 Complete the deterministic Wasp mirror integration gate

Observable outcome: A Wasp mirror match can start, play repeated turns/rounds, and end through every victory path with primitive visuals and debug UI.

Acceptance checks:

- Every implemented Wasp rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader source-linked Wasp integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: Additional pitch-hero inventory and implementation do not begin until milestone 8.16 is reviewed and approved. Production visual work remains blocked until the six-hero 3v3 gate at milestone 8.27, the render-pipeline migration gate at milestone 8.28, and the private online playtest gate at milestone 8.36 are reviewed and approved.

### [!] 8.17 Select the five additional pitch heroes

Observable outcome: The user approves five specific heroes that join Wasp as six different heroes divided into three Red and three Blue roster slots for the private pitch.

Acceptance checks:

- The proposed roster contains no duplicate hero and identifies each hero's intended team slot without treating team assignment as a card-rule branch.
- Implementation-grade card, dashboard, token/marker, miniature/presentation, and applicable official-ruling sources are confirmed available or explicitly recorded as blockers.
- Selection considers effect-family and presentation breadth so the roster demonstrates the reusable architecture without claiming equal implementation cost for every hero.
- The exact five-hero choice receives explicit user approval before inventory begins.

Non-goals: No card transcription, effect implementation, production art, balance adjustment, or seventh pitch hero.

### [!] 8.18 Inventory and verify the five additional hero sets

Observable outcome: Every selected hero's cards, tiers, values, action types, text, item possibilities, Ultimate, tokens/markers, and other hero-owned components are traceable to source material.

Acceptance checks:

- Every required face and component is present and readable for all five heroes.
- Structured data is manually checked against source images and applicable official rulings.
- Missing, ambiguous, or conflicting content is recorded in `RULES_LEDGER.md` and blocks the affected hero rather than being guessed.
- No implementation begins from unverified filenames, OCR, display text, or assumed cross-hero similarity.

### [!] 8.19 Map the five heroes to capabilities and re-baseline the 3v3 pitch work

Observable outcome: Every verified action and hero-owned component maps to existing or new typed capabilities, and the remaining hero, 3v3 integration, and presentation milestones are re-baselined from evidence.

Acceptance checks:

- A roster-wide matrix distinguishes reused core/Wasp capabilities, reusable refinements, new primitives, and genuinely unique isolated handlers for every verified hero face and component.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and source-linked tests.
- The designated roadmap audit applies the matrix to milestones 8.20-8.36 and Phase 9, recommending justified keep/merge/split/defer/remove decisions and recording an updated remaining-effort range.
- Approved implementation slices are dependency ordered; a shared capability precedes the first hero that needs it, and no hero-name rule branch or runtime text parsing is scheduled.
- Any schema change is explicit and migration-tested rather than hidden inside a hero milestone.

Non-goals: No effect implementation or production art.

### [!] 8.20 Complete and verify additional pitch hero slot A

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.21 Complete and verify additional pitch hero slot B

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.22 Complete and verify additional pitch hero slot C

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.23 Complete and verify additional pitch hero slot D

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.24 Complete and verify additional pitch hero slot E

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.25 Replace the staged 1v1 setup with the official six-player Quick Game configuration

Observable outcome: Configuration data places three different Red heroes and three different Blue heroes with their own hands and components using the official six-player Quick Game baseline.

Acceptance checks:

- All six stable player/hero identities, ownership relationships, hero spawn positions, hands, dashboards, and hero-owned components resolve without assuming one hero per team.
- Every authoritative turn slot contains exactly six ownership-validated assignments—one for each configured `HeroId`—and snapshot round-trip preserves their deterministic order and committed-card-or-empty-hand-Pass facts; controlling `PlayerId` and team resolve through the participant graph.
- Setup uses three Wave counters, five Life counters per team, the normal Tie Breaker flip, and the verified official board/minion setup.
- Wasp appears once; all six heroes are different and assigned to exactly one team/player.
- DEV-001 and DEV-002 are marked retired; only approved director/debug deviations remain.
- Snapshots round-trip the complete six-player setup deterministically.

Non-goals: No networking, hidden per-player devices, AI, matchmaking, or balance changes.

### [!] 8.26 Integrate six-hero director control and team interactions

Observable outcome: One director can play and resolve a complete turn for all six independently owned heroes while the rules preserve normal 3v3 ownership, Initiative, target, assist, reaction, and team relationships.

Acceptance checks:

- The director can access each hero's correct hand; for every hero with a nonempty hand the director must choose exactly one owned card, while an empty hand records a Pass. The complete turn reveals only after all six heroes have responses, then resolves every played card in Initiative order without transferring ownership.
- Friendly/enemy queries, tied Initiative decisions, assists, reactions, hero-specific pending choices, and actor switching use authoritative team/player/hero relationships.
- Played/revealed cards and the active resolver remain readable without requiring presentation to own turn legality.
- A read-only primitive round-history matrix groups all three Red and all three Blue heroes and shows each hero's authoritative revealed card or empty-hand Pass for turns 1-4; current-turn Facedown commitments remain hidden until the joint reveal.
- Every displayed history entry resolves through its stable assignment and card definition, and played or Defense-discarded cards cannot reappear merely because presentation rebuilds.
- Undo, restart, snapshot restore, and the event log reproduce the same six-hero turn.

Non-goals: No hidden-information enforcement, table-talk enforcement, networking, AI, or six separate input devices.

### [!] 8.27 Complete the deterministic six-hero 3v3 pitch match

Observable outcome: Wasp and the five other distinct verified heroes can start from the official six-player Quick Game setup, play repeated turns and rounds, and end through every victory path with primitive visuals and director/debug UI.

Acceptance checks:

- Every implemented hero rule links to an authoritative source and focused tests; unresolved source or capability entries keep the gate incomplete.
- The complete six-hero roster exercises legal team interactions, combat, minions, Lane Push, progression, defeat/respawn, and victory without card-name/hero-name branches outside isolated approved handlers.
- The official six-player setup and only the remaining approved director/debug deviations are represented.
- Undo, restart, snapshots, event log, and Tie Breaker override remain deterministic across the complete 3v3 loop.
- A broader source-linked six-hero integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The networking architecture decision does not begin until the deterministic 3v3 match is reviewed and approved. Production visual work remains blocked through milestone 8.36.

### [!] 8.28 Render-pipeline migration gate

Observable outcome: The complete primitive six-hero match is ported from Unity's Built-In Render Pipeline to URP, with its existing director/debug presentation and Win64 reference path intact, before production visual assets are introduced.

Planning rationale: Unity deprecated the Built-In Render Pipeline in Unity 6.5, guarantees its availability through at least Unity 6.7 LTS, and has not announced a final removal version. This project can retain Built-In rendering through the primitive-rule phases, but must not begin production presentation work on a pipeline Unity has directed new projects away from. [Unity's 2026 render-pipeline strategy](https://unity.com/topics/render-pipelines-strategy-for-2026).

Acceptance checks:

- URP is the active render pipeline for every configured quality level; the project no longer relies on a Built-In-only default pipeline setting.
- The existing primitive board, miniatures/tokens, card/HUD panels, highlighting, pointer selection, fixed camera, and event-driven presentation remain readable and functional after conversion.
- Materials, shaders, lighting, and post-processing used by the retained primitive presentation are URP-compatible; no Built-In-only visual dependency is carried into Phase 9.
- The authoritative rules assemblies and their deterministic state/event results are unchanged; the migration does not alter game rules, command handling, or timing.
- Focused Play Mode coverage for the affected presentation paths, a targeted visual inspection, a disposable internal Win64 development-build launch smoke, and both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No production board art, hero/minion production models, animation coverage, final lighting/art direction, new game rules, or player-facing build.

Hard gate: The private-online track uses this accepted URP baseline. Phase 9 production visual milestones do not begin until milestone 8.36 is also reviewed and approved.

### [!] 8.29 Select the minimum private-online architecture and re-baseline the track

Observable outcome: A source-backed architecture decision defines the smallest supportable way for six invited Windows players on the internet to join one private 3v3 match, with one authoritative host/session and one independently owned hero per player.

Acceptance checks:

- Current Unity-supported transport, relay/NAT traversal, hosting, cost, licensing, privacy, and operational constraints are compared before a provider or SDK is selected.
- The trust boundary, host authority, seat/hero ownership, client-visible projections, version compatibility, join flow, disconnect policy, and failure model are explicit.
- The track remains private and invite-only; accounts, public matchmaking, public server browsing, telemetry, monetization, and public distribution remain out of scope.
- Milestones 8.30-8.36 receive evidence-based implementation slices, validation commands, and required Codex profiles before the first networking implementation is authorized.

Planning note: This scope amendment adds a major cross-system track rather than one playtest task. The provisional planning range is eight micro-milestones and roughly 4-8 focused engineering weeks, excluding approval latency, external-service outages, and corrective work discovered by real internet testing. Milestone 8.29 must replace this estimate with evidence from the selected architecture.

### [!] 8.30 Move match authority behind a transport-neutral session boundary

Observable outcome: The existing local director adapter and a future network host can submit the same typed commands to, and consume the same ordered events and snapshots from, one authoritative match session without moving rules into Unity views or transport callbacks.

### [!] 8.31 Establish the private host and six player seats

Observable outcome: One host creates an invite-only session and six compatible Windows clients can occupy exactly one stable player/hero seat each, with duplicates, unauthorized seat changes, and incompatible content/schema revisions rejected.

### [!] 8.32 Add owner-scoped projections and command authorization

Observable outcome: Each client receives only the state it is permitted to see and can submit commands only for its owned hero or an explicitly team-owned choice; the host revalidates every command against authoritative state.

### [!] 8.33 Synchronize commands, events, pending choices, and snapshots

Observable outcome: All clients reproduce the host's public match state and their authorized private projection across full turns, nested Defense reactions, tied Initiative choices, and other pending resolutions without client-owned rules authority.

### [!] 8.34 Add disconnect, reconnect, resynchronization, and host-loss policy

Observable outcome: A temporarily disconnected player can return to the same compatible private session and rebuild the correct authorized projection; stale commands, unrecoverable host loss, and version mismatch fail explicitly without corrupting the match.

### [!] 8.35 Add primitive private lobby and online match-flow UI

Observable outcome: Without production styling, invited players can host/join, identify their seat and team, become ready, enter the match, see their own hand and shared public state, answer their legal prompts, and leave or reconnect with clear status feedback.

Non-goals: No account system, friends list, public matchmaking, server browser, text/voice chat, spectator mode, ranking, moderation, or polished production UI.

### [!] 8.36 Validate a private six-human internet 3v3 and run the friends playtest

Observable outcome: Six humans on separate clients complete an invite-only full 3v3 match over the internet using a disposable private Win64 playtest build and primitive UI before production presentation begins.

Acceptance checks:

- The exact build and content/schema revision are recorded and privately distributed only to the invited testers.
- The session proves host/join, six stable seats, private hands, all normal player and team-owned choices, complete turns/rounds, reconnect/resync, match completion, and explicit failure handling across at least two external networks.
- Host and clients converge on the same authoritative public outcome; owner-private projections and command authorization show no cross-player leakage or control transfer.
- Crashes, desynchronization, latency symptoms, confusing flows, unreadable state, and tester feedback are recorded and triaged without changing rules or code during the milestone.
- Any gate-blocking defect becomes a separately approved corrective milestone; the reviewed findings re-baseline Phase 9 HUD, prompt, feedback, and readability work.
- T3 online hard gate: focused network/session tests, a multi-client integration run, the private internet playtest, and both complete unfiltered Edit Mode and Play Mode suites pass at the applicable final revision.

Non-goals: No public release, dedicated production service, accounts, matchmaking, anti-cheat certification, competitive operations, or production presentation.

Hard gate: Phase 9 does not begin until the private online playtest evidence and any gate-blocking corrective milestones are reviewed and approved.

## Phase 9 - Presentation and pitch polish

Every item remains a separate micro-milestone. Phase 9 starts only after milestone 8.28 establishes URP as the accepted rendering baseline and milestone 8.36 proves the private six-human internet flow with primitive UI.

Each visible milestone runs focused Play Mode checks for the affected presentation path plus one targeted visual inspection. The disposable migration-check build from milestone 8.28 and invited-tester online build from milestone 8.36 are not retained as pitch deliverables or publicly distributed; the first retained private pitch player build remains milestone 10.1. Complete unfiltered suites wait for milestone 9.16 unless a T3 risk trigger applies earlier.

### [ ] 9.1 Production board environment

### [ ] 9.2 Six hero miniatures and Red/Blue team treatments

### [ ] 9.3 Minion miniatures

Production minion miniatures preserve the milestone 4.4 atlas-derived circular minion tokens as each unit's 2D representation rather than replacing or redrawing that source.

### [ ] 9.4 Bounded pan, orbit, zoom, and reset camera

### [ ] 9.5 Automatic 3D miniature / 2D token switching

Top-down switching uses the reviewed milestone 4.4 Steam/orange-to-Red and Crystal/light-blue-to-Blue Heavy/Melee/Ranged token mapping from `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg`, including the correction for the manifest's mislabeled Crystal melee sprite, while the same authoritative minion instance continues to own both its 3D and 2D presentation.

### [ ] 9.6 Six-hero idle animation coverage

### [ ] 9.7 Six-hero movement animation coverage

### [ ] 9.8 Six-hero attack animation coverage

### [ ] 9.9 Six-hero hit/defeat animation coverage

### [ ] 9.10 Six-hero respawn animation coverage

### [ ] 9.11 Shared minion animation set

### [ ] 9.12 Rule-event presentation queue, skip, and speed controls

### [ ] 9.13 Final pitch HUD and contextual prompts

Observable outcome: The polished HUD keeps the complete 3v3 match state and active resolver readable, including a compact read-only four-turn history for all six independently owned heroes.

Acceptance checks:

- The round-history treatment groups three Red and three Blue heroes and shows each hero's authoritative revealed card or empty-hand Pass in turns 1-4 without relying on color alone.
- Director mode can inspect the complete history; private online clients receive only the authorized shared played/revealed projection and never another player's hidden hand or pre-reveal card identity.
- History rebuilds identically after snapshot restore, Undo, restart, reconnect, and resynchronization without owning or mutating match state.
- The history, active resolver, six hands, contextual prompts, round/turn, Lives, Waves, hero levels, and coins remain readable at the supported desktop resolutions.

Non-goals: No editable history, replay timeline, analytics, telemetry, or public match-history service.

### [ ] 9.14 Team symbols, outlines, readability, and UI scaling baseline

### [ ] 9.15 Minimal sound-effects pass

### [ ] 9.16 Complete presentation regression and visual-readability sweep

Observable outcome: The finished pitch presentation remains readable and synchronized with authoritative rules across the complete primitive-to-polished match flow.

Acceptance checks:

- A targeted visual sweep covers the board, all six team/hero-grouped hands, played/revealed cards, prompts, event log, six-hero and team differentiation, camera bounds, 3D/2D switching, animations, and affected audio cues.
- Skipping or accelerating presentation produces the same authoritative state and ordered rules events.
- Common reference desktop resolutions preserve readable cards, values, prompts, and team identity.
- T3 pre-build gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The first retained private pitch player build does not begin until milestone 9.16 is reviewed and approved. The disposable render-pipeline smoke from milestone 8.28 and invited-tester online build from milestone 8.36 are pre-production evidence only.

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

## Post-pitch production direction - not scheduled

- Formalize licensed product scope and name.
- Productionize the private-playtest networking architecture for the licensed product's availability, scale, security, privacy, and support requirements.
- Reassess listen-host, relay, dedicated-server, and migration requirements from measured playtest evidence.
- Productionize reconnect and lobby behavior; add matchmaking, accounts, and team communication if authorized.
- Add heroes incrementally; every hero intake starts with a verified effect-family delta audit against the capability catalog before implementation is scheduled.
- Validate macOS/Linux, then touch devices, then closed platforms as separately approved tracks.
