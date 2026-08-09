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
4. Run focused acceptance checks.
5. Update the project documents.
6. Create a local Git commit containing the completed milestone and continuity updates.
7. Report the result and stop.

Milestones may be split further if implementation reveals more than one independently testable behavior. They must not be silently combined.

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

### [ ] 1.4 Select the hero and one adjacent destination

Observable outcome: Pointer input can select the hero and highlight valid adjacent empty hexes.

Acceptance checks:

- Only adjacent empty hexes are highlighted.
- Selection is usable with Unity's pointer abstraction.

Non-goals: The hero does not move yet.

### [ ] 1.5 Commit one adjacent move

Observable outcome: Clicking a highlighted destination updates authoritative occupancy and snaps the primitive hero to it.

Acceptance checks:

- Origin becomes empty and destination becomes occupied.
- Invalid destinations do not change state.
- Focused tests cover valid and invalid moves.

Non-goals: No movement range, card, turn, path, or animation.

## Phase 2 - Authoritative state and internal tools

### [ ] 2.1 Introduce teams, players, heroes, and stable IDs

Observable outcome: The model supports collections of teams, players, and heroes while the test configuration creates one Red and one Blue hero.

Acceptance checks:

- No rule API assumes exactly one hero per team.
- IDs survive state serialization.

Non-goals: No 3v3 UI or multiplayer behavior.

### [ ] 2.2 Establish command validation and rule events

Observable outcome: Movement is requested as a command, validated by local authority, and expressed as ordered rule events.

Acceptance checks:

- Invalid commands return a structured rejection and do not mutate state.
- Presentation responds to emitted events rather than directly changing rules state.

Non-goals: No networking library or remote server.

### [ ] 2.3 Serialize and restore authoritative snapshots

Observable outcome: A versioned snapshot round-trips without changing logical state.

Acceptance checks:

- Teams, players, heroes, board occupancy, and IDs survive round-trip tests.
- Presentation can rebuild from restored state.

Non-goals: No player-facing save slots or cloud storage.

### [ ] 2.4 Add director-mode Undo and restart

Observable outcome: Undo restores the snapshot before the last committed command; restart restores the initial scenario.

Acceptance checks:

- Invalid commands do not create undo entries.
- Undo restores both authoritative state and presentation.

Non-goals: No redo requirement and no competitive availability.

### [ ] 2.5 Add Tie Breaker setup state

Observable outcome: Normal setup chooses the initial Tie Breaker side; internal tests can explicitly select Red or Blue.

Acceptance checks:

- The result is stored in authoritative state and snapshot history.
- No general gameplay RNG subsystem is introduced.

## Phase 3 - Official board data

### [ ] 3.1 Inventory the single-lane board reference

Observable outcome: The official board's spaces, adjacency, terrain, zones, spawn points, and Thrones have a reviewed data specification.

Acceptance checks:

- Every logical space has a stable ID and coordinate.
- Zone boundaries and spawn points are checked against the authoritative visual reference.

Non-goals: No production board art or models.

### [ ] 3.2 Render the official board as placeholders

Observable outcome: Placeholder geometry renders the complete official single-lane logical board.

Acceptance checks:

- Logical adjacency matches the reviewed board specification.
- Terrain and spawn-point classifications are inspectable.

Non-goals: No polished environment.

### [ ] 3.3 Place the 1v1 Quick Game setup

Observable outcome: Red Wasp, Blue Wasp, standard minions, Lives, Waves, and Tie Breaker state are placed from configuration data.

Acceptance checks:

- Three Wave counters and four Life counters per team.
- Standard single-lane minion setup.
- Only the approved player-count and mirrored-hero deviations are present.

## Phase 4 - Card and turn skeleton

### [ ] 4.1 Inventory and verify Wasp's complete card set

Observable outcome: Wasp's cards, tiers, values, action types, text, item possibilities, and Ultimate are traceable to source images.

Acceptance checks:

- Every required face is present and readable.
- Structured data is manually checked against the images.
- Missing or ambiguous content is recorded in `RULES_LEDGER.md`.

Non-goals: No other hero inventory.

### [ ] 4.2 Represent card identity, values, and states

Observable outcome: Wasp card data can exist in hand, played, unresolved, resolved, discarded, and facedown states as applicable.

Acceptance checks:

- State transitions are pure-rule tests.
- Card images remain presentation-only.

Non-goals: No card effects yet.

### [ ] 4.3 Display both director-mode hands

Observable outcome: Red and Blue Wasp hands are visible and separately selectable.

Acceptance checks:

- Team identity is not conveyed by color alone.
- Selecting a card does not yet resolve an action.

### [ ] 4.4 Play, reveal, and order two cards

Observable outcome: The controller selects one card per side, confirms, reveals both, and receives the correct initiative order.

Acceptance checks:

- Highest Initiative resolves first.
- Ties use the current Tie Breaker side and flip it only as required by the rules.

Non-goals: No action execution beyond Hold.

### [ ] 4.5 Resolve Hold and end one turn

Observable outcome: Each unresolved card can perform Hold, becomes resolved, and the turn ends after both sides resolve.

Acceptance checks:

- Resolution proceeds one card at a time in initiative order.
- Turn-only effects have a defined cancellation boundary even if none exist yet.

### [ ] 4.6 Complete the four-turn round skeleton

Observable outcome: Four turns advance, cards occupy the correct turn slots, and the round reaches an end-of-round phase.

Acceptance checks:

- A card cannot be replayed from the wrong state.
- Passing behavior is represented when no card remains.

Non-goals: End-of-round subsystems are not resolved yet.

## Phase 5 - Core actions and Wasp effects

Each action below should be split further whenever targeting, validation, state mutation, and presentation can be proven independently.

### [ ] 5.1 Exact card-based Movement action

Includes movement allowance, obstacles, legal endpoints, and Wasp movement text required at the current tier.

### [ ] 5.2 Fast Travel replacement action

Implements only after exact movement works.

### [ ] 5.3 Skill action framework

Adds only the typed effect primitives required by a verified Wasp card.

### [ ] 5.4 Attack targeting and range

Validates legal hero and minion targets without resolving defense yet.

### [ ] 5.5 Minion attacks and rewards

Implements minion type values, Heavy immunity, defeat, and coins according to the rules.

### [ ] 5.6 Hero Defense and card discard

Implements the detailed attack/defense order and primary/secondary Defense behavior required by Wasp.

### [ ] 5.7 Hero defeat, Lives, and respawn

Implements removal, Life spending, rewards, effect cancellation, and legal respawn.

### [ ] 5.8 Active effects and timing windows

Implements only verified Wasp requirements, using reusable typed triggers and durations.

### [ ] 5.9 Complete all Wasp card behaviors

Observable outcome: Every Wasp card and legal action at every pitch-relevant tier is verified against focused scenario tests.

## Phase 6 - Minions, lane, and end of round

### [ ] 6.1 Minion occupancy and defense modifiers

### [ ] 6.2 Minions returning to the Battle Zone

### [ ] 6.3 End-of-round Minion Battle

### [ ] 6.4 Immediate Push trigger during a turn

### [ ] 6.5 Lane Push state transition and respawn

### [ ] 6.6 Wave counters, Throne capture, and Last Push victory

### [ ] 6.7 Token removal and marker return

Each milestone must cover its exact rule ordering and focused edge cases before the next begins.

## Phase 7 - Economy and progression

### [ ] 7.1 Coin awards and assist-ready accounting

The 1v1 configuration has no friendly assisting hero, but the model must support team collections without implementing 3v3 UI.

### [ ] 7.2 Mandatory end-of-round leveling

### [ ] 7.3 Card upgrades and item bonuses

### [ ] 7.4 Ultimate access

### [ ] 7.5 Pity Coin ordering

### [ ] 7.6 All three victory paths

## Phase 8 - Primitive complete match

### [ ] 8.1 Complete deterministic 1v1 match

Observable outcome: A match can start, play through repeated turns and rounds, and end through each official victory path using primitive visuals and debug UI.

Acceptance checks:

- Every implemented rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader milestone suite passes.

Hard gate: Production visual work does not begin until this milestone is reviewed and approved.

## Phase 9 - Presentation and pitch polish

Every item remains a separate micro-milestone.

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

## Phase 10 - Private publisher pitch package

### [ ] 10.1 Windows standalone build

### [ ] 10.2 Clean-machine private smoke test

### [ ] 10.3 Short recorded gameplay walkthrough

### [ ] 10.4 Publisher demonstration checklist

Non-goals: Public distribution, installer, launcher, login, telemetry, store page, or other-platform release.

## Post-authorization direction - not scheduled

- Formalize licensed product scope and name.
- Choose and prototype network transport/hosting based on then-current requirements.
- Move authoritative resolution to a host/server.
- Add per-player hidden hands and six-human online 3v3.
- Add reconnect, lobby, matchmaking, accounts, and team communication.
- Add heroes incrementally using the verified card-effect architecture.
- Validate macOS/Linux, then touch devices, then closed platforms as separately approved tracks.
