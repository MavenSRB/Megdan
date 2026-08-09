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

Every card-related milestone must preserve the contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`. A narrow milestone implements only the required subset of that contract; it must not introduce a shortcut that the next card milestone is expected to replace.

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

### [ ] 1.5 Commit one adjacent move through rules authority

Observable outcome: Pure rules code accepts an adjacent-move intent command, validates it, commits authoritative occupancy, and returns an ordered movement rule event.

Acceptance checks:

- The command refers to stable hero identity and explicit origin/destination coordinates.
- A legal adjacent move makes the origin empty and destination occupied.
- An invalid or stale command returns a structured rejection, changes nothing, and emits no accepted state-change event.
- The accepted event contains rules facts only and no animation or scene instruction.
- Focused pure-rule tests cover legal, non-adjacent, occupied, wrong-origin, and unknown-hero requests.

Non-goals: No pointer wiring, presentation movement, movement range, cards, turns, networking, or animation.

### [ ] 1.6 Connect adjacent selection to the committed move

Observable outcome: Clicking a highlighted destination submits the milestone 1.5 command, then the primitive hero view snaps to the position derived from updated authoritative occupancy and the emitted event.

Acceptance checks:

- UI/pointer code never assigns occupancy directly.
- A rejected command leaves selection and presentation consistent with unchanged authoritative state.
- A legal move clears obsolete highlighting and renders the hero on the authoritative destination.
- Focused Edit Mode and Play Mode tests cover valid and invalid pointer-to-command flows.

Non-goals: No multi-space movement, card, turn, path automation, networking, or animation.

### [ ] 1.7 Expand the placeholder test grid

Observable outcome: The scene contains a larger connected placeholder grid with enough space to test movement of several steps, alternate routes, and later obstacle placement.

Acceptance checks:

- The grid contains pairs of spaces at logical distances 1, 2, and 3 or greater.
- At least one pair of spaces has more than one valid route between it.
- Every rendered center still corresponds to its authoritative axial coordinate.
- Existing seven-hex rendering and occupancy behavior remain valid.

Non-goals: No official map data, movement-rule change, obstacle, zone, terrain, card, turn, or camera movement.

### [ ] 1.8 Move up to a configurable number of spaces

Observable outcome: A movement session gives the primitive hero a configurable allowance greater than one and lets the tester choose each adjacent step until stopping early or spending the allowance.

Acceptance checks:

- Every committed step is adjacent to the hero's current space.
- Each step consumes exactly one point of movement allowance.
- The hero cannot take more steps than the allowance.
- Moving zero spaces and stopping before the allowance is exhausted are both legal.
- The chosen route is explicit; the system does not silently substitute a different path.
- Every committed step uses the milestone 1.5 command/event authority.
- Focused tests cover zero movement, early stopping, exact allowance, and attempted over-movement.

Non-goals: No cards, turns, Fast Travel, movement from card text, movement-limiting effects, obstacles, or animation.

### [ ] 1.9 Add obstacles and enforce movement blocking

Observable outcome: Placeholder obstacles can occupy spaces on the expanded grid, and multi-step movement can route around them but cannot enter or pass through them.

Acceptance checks:

- An obstacle prevents the hero from entering its space.
- An obstacle cannot be crossed as an intermediate movement step.
- The tester can choose an alternate legal route when the allowance permits it.
- A space can contain at most one obstacle, including a unit.
- Legal-next-step highlighting updates after every step and excludes blocked spaces.
- Focused tests cover blocked destinations, blocked intermediate spaces, legal detours, insufficient allowance for a detour, and occupancy conflicts.

Non-goals: No ability to ignore obstacles, obstacle subtypes, tokens, terrain art, other units, path automation, cards, turns, Fast Travel, or animation.

### [ ] 1.10 Separate card definitions from card instances

Observable outcome: Pure C# code distinguishes immutable, versioned placeholder card content from stable runtime card instances owned by the primitive test hero.

Acceptance checks:

- A `CardDefinition`-equivalent has a nonempty stable definition ID, content revision, and display name.
- A `CardInstance`-equivalent has a different stable instance ID, definition reference, owner ID, and data-shaped runtime state initialized as in hand.
- Two instances may reference one definition without either instance mutating it.
- Runtime card state is represented with extensible orthogonal facts rather than a movement-card subclass or one catch-all lifecycle enum.
- Missing, duplicate, or unresolved IDs and invalid initial state are rejected by focused tests.
- No authoritative type depends on Unity objects, presentation, images, or printed-text parsing.

Non-goals: No action offers, card values, real Wasp data/images, state transitions, play/reveal flow, turns, or animation.

### [ ] 1.11 Represent action definitions, runtime offers, and typed values

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

### [ ] 1.12 Add a resumable card-action resolution shell

Observable outcome: Pure rules code can start one selected placeholder Movement offer in its explicit Primary or Secondary context, produce an authoritative pending movement choice, accept or reject a follow-up choice command, and resume until the action completes.

Acceptance checks:

- The pending resolution records stable source-card instance, action-offer, actor, invocation-context, and current-step identifiers as plain data.
- A pending choice identifies its chooser and exact legal decision without delegates, closures, iterators, Unity coroutines, or UI-owned continuation state.
- Each chosen adjacent step reuses the validated movement/session rules from milestones 1.8-1.9 and the command/event authority from milestone 1.5 rather than duplicating them.
- Stopping at zero or before the allowance is exhausted completes legally; spending the allowance completes automatically.
- An invalid or stale choice does not mutate state or advance the continuation.
- Replaying the same starting state and accepted command/choice sequence produces the same state and ordered events.

Non-goals: No general-purpose scripting language, Wasp text, modal/repeat behavior, reactions, timing effects, card lifecycle transitions, turns, snapshots, UI hand, or animation.

### [ ] 1.13 Select a test card and perform card-driven movement

Observable outcome: A visible placeholder hand lets the tester select one card and one available Movement offer, then complete the resumable card-driven movement on the expanded obstacle map.

Acceptance checks:

- The movement allowance comes only from the selected action offer's structured printed value.
- A value-1 offer reproduces one-adjacent-space behavior and a higher value permits the corresponding explicit steps.
- Primary/Secondary context remains visible to the authority and the Secondary offer does not accidentally opt into Primary-text behavior.
- The tester may move zero spaces or stop early.
- Obstacles block entry and passage exactly as in milestone 1.9.
- Card, action, and pending-choice selection are visually identifiable and clear when resolution ends.
- UI input submits commands and answers pending choices; it never owns card resolution or mutates occupancy.
- Focused tests cover both values, early stopping, stale/invalid choices, over-movement rejection, and obstacle interaction.

Non-goals: No real Wasp cards, printed card text, initiative, facedown/reveal behavior, lifecycle transitions beyond the initial hand state, turns, rounds, Fast Travel, or animation.

## Phase 2 - Two-hero combat, authoritative state, and internal tools

### [ ] 2.1 Introduce teams, players, heroes, and stable IDs

Observable outcome: The model supports collections of teams, players, and heroes while the test configuration creates one Red and one Blue hero.

Acceptance checks:

- No rule API assumes exactly one hero per team.
- Team, player, and hero IDs are nonempty, unique, and resolve through collections.
- The existing primitive hero ID remains stable, and Phase 1 card-instance owner references still resolve after the state is expanded to Red/Blue teams and players.

Non-goals: No 3v3 UI or multiplayer behavior.

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

### [ ] 2.6 Serialize and restore authoritative snapshots

Observable outcome: A versioned snapshot round-trips without changing logical state.

Acceptance checks:

- Teams, players, heroes, board occupancy, card instances/state facets, and definition references survive round-trip tests.
- A state paused on a pending card choice round-trips and resumes to the same result and ordered events.
- Snapshot schema version and card-definition content revision are checked independently; mismatches are rejected clearly until a migration exists.
- Presentation can rebuild from restored state.

Non-goals: No player-facing save slots or cloud storage.

### [ ] 2.7 Add director-mode Undo and restart

Observable outcome: Undo restores the snapshot before the last committed command; restart restores the initial scenario.

Acceptance checks:

- Invalid commands do not create undo entries.
- Undo restores both authoritative state and presentation.

Non-goals: No redo requirement and no competitive availability.

### [ ] 2.8 Add the internal rule-event log

Observable outcome: A primitive internal panel shows the accepted ordered rule events for the current authoritative history and rebuilds consistently after restore, Undo, or restart.

Acceptance checks:

- Each entry shows sequence, event type, stable rules-object references, and the key value/choice needed to diagnose the event.
- Rejected commands do not appear as accepted state-change events; any rejection display is clearly separate.
- Undo removes events after the restored command boundary, restart clears to setup events, and snapshot restore reproduces the same log projection.
- The log is a read-only projection and cannot mutate or continue rules resolution.

Non-goals: No final visual styling, export format, analytics, telemetry, or player-facing match history.

### [ ] 2.9 Add Tie Breaker setup state

Observable outcome: Normal setup chooses the initial Tie Breaker side; internal tests can explicitly select Red or Blue.

Acceptance checks:

- The result is stored in authoritative state and snapshot history.
- No general gameplay RNG subsystem is introduced.

## Phase 3 - Full official multi-zone board

### [ ] 3.1 Inventory the complete single-lane board reference

Observable outcome: Every part of the complete official single-lane board - including all spaces, connections, terrain, zones, spawn points, Thrones, and the starting Battle Zone - has a reviewed data specification.

Acceptance checks:

- Every logical space has a stable ID and coordinate.
- Every zone boundary, zone membership, spawn point, and Throne is checked against the authoritative visual reference.
- The inventory covers the full map rather than a reduced movement-test area.

Non-goals: No production board art or models.

### [ ] 3.2 Upgrade the test grid to the full official placeholder map

Observable outcome: The temporary Phase 1 test grid is replaced in the main validation scene by placeholder geometry for the complete official multi-zone single-lane map.

Acceptance checks:

- Logical adjacency matches the reviewed board specification.
- Terrain and spawn-point classifications are inspectable.
- Every official zone is visibly distinguishable in debug presentation without implying final art.
- The complete map can be framed and inspected under the current fixed-camera test setup.

Non-goals: No polished environment, Fast Travel, lane pushing, or production camera controls.

### [ ] 3.3 Define and validate zones and zone adjacency

Observable outcome: Every board space belongs to the correct official zone, and authoritative queries can distinguish the same zone, an adjacent zone, and a non-adjacent zone.

Acceptance checks:

- Red Throne, Blue Throne, the starting Battle Zone, and every intermediate zone have stable IDs.
- Every playable space has the correct zone membership.
- Zone adjacency follows the rulebook definition and is verified against the complete map.
- Focused tests cover same-zone, adjacent-zone, and non-adjacent-zone pairs in both directions.
- The ordered lane relationship between the two Thrones is represented for later lane movement.

Non-goals: No Fast Travel validation, current-Battle-Zone changes, lane pushing, minion respawn, or victory logic.

### [ ] 3.4 Place the 1v1 Quick Game setup

Observable outcome: Red Wasp, Blue Wasp, standard minions, Lives, Waves, and Tie Breaker state are placed from configuration data.

Acceptance checks:

- Three Wave counters and four Life counters per team.
- Standard single-lane minion setup.
- Only the approved player-count and mirrored-hero deviations are present.

## Phase 4 - Verified Wasp cards and turn skeleton

### [ ] 4.1 Inventory and verify Wasp's complete card set

Observable outcome: Wasp's cards, tiers, values, action types, text, item possibilities, and Ultimate are traceable to source images.

Acceptance checks:

- Every required face is present and readable.
- Structured data is manually checked against the images.
- Missing or ambiguous content is recorded in `RULES_LEDGER.md`.

Non-goals: No other hero inventory.

### [ ] 4.2 Map Wasp cards to engine capabilities and re-baseline Phase 5

Observable outcome: Every verified Wasp action is mapped to its required action contexts, ordered steps, choices, targets, values, timing windows, state transitions, simultaneous batches, Active effects versus continuous sources, and reusable or custom capabilities before effect implementation begins.

Acceptance checks:

- The matrix covers every Wasp card face, tier, item relationship, and Ultimate from milestone 4.1.
- Existing capabilities are distinguished from new reusable primitives and genuinely unique handlers.
- Dependencies identify which targeting, timing, active-effect, progression, minion, and map rules must precede each card.
- Phase 5 is reordered or split so each Wasp card is implemented only after its actual dependencies; active effects do not remain artificially late if an earlier card requires them.
- Any proposed change to the core architecture, definition schema, or snapshot schema is explicit and migration-tested rather than hidden inside a card milestone.

Non-goals: No effect implementation or other-hero corpus inventory.

### [ ] 4.3 Extend the card foundation with verified Wasp values and lifecycle

Observable outcome: Verified Wasp definitions use the Phase 1-2 foundation, and Wasp card instances can represent and transition through the rulebook's applicable overlapping lifecycle facts.

Acceptance checks:

- Location, played status, unresolved/resolved status, and face orientation remain orthogonal where the rulebook allows facts to overlap.
- Pure-rule tests cover play, reveal, resolve, resolve without action, discard, retrieve, and becoming facedown as applicable.
- Tests cover a played-and-resolved card, the rare played-and-discarded representation, rules-information suppression when a resolved/discarded card becomes facedown, and applicable active-effect cancellation hooks even before concrete effects exist.
- Card images remain presentation-only.
- Phase 1 placeholder cards and hard-coded test values are not used as Wasp authority.

Non-goals: No card effects yet.

### [ ] 4.4 Display both director-mode hands

Observable outcome: Red and Blue Wasp hands are visible and separately selectable.

Acceptance checks:

- Team identity is not conveyed by color alone.
- Selecting a card does not yet resolve an action.

### [ ] 4.5 Play, reveal, and order two cards

Observable outcome: The controller selects one card per side, confirms, reveals both, and the authority identifies the correct next unresolved resolver without pre-sorting the rest of the turn.

Acceptance checks:

- The highest current Initiative among unresolved cards is queried again after every later resolution rather than cached as a complete order.
- A cross-team tie selects an eligible tied resolver using the current Tie Breaker side and records any required flip for after that resolver finishes; it does not flip early.
- A same-team tie produces an authoritative pending choice for that team/controller and never flips the Tie Breaker.
- Focused synthetic multi-hero cases cover more than two tied cards even though the pitch setup has one hero per team.

Non-goals: No action execution, including Hold.

### [ ] 4.6 Resolve Hold and end one turn

Observable outcome: Each unresolved card can perform Hold, becomes resolved, and the turn ends after both sides resolve.

Acceptance checks:

- Resolution proceeds one card at a time in initiative order.
- Any deferred cross-team Tie Breaker flip occurs only after the selected tied resolver's card resolves, then the next resolver is queried from current unresolved state.
- Turn-only effects have a defined cancellation boundary even if none exist yet.

### [ ] 4.7 Complete the four-turn round skeleton

Observable outcome: Four turns advance, cards occupy the correct turn slots, and the round reaches an end-of-round phase.

Acceptance checks:

- A card cannot be replayed from the wrong state.
- Passing behavior is represented when no card remains.

Non-goals: End-of-round subsystems are not resolved yet.

## Phase 5 - Provisional Wasp capability groups

Every item below is deliberately blocked until milestone 4.2 produces the verified Wasp capability matrix. These are discovery groups, not an executable order. Reorder, split, rename, and unblock them so map, targeting, timing, active/continuous-effect, minion, economy, and progression dependencies precede the first card that needs them. Split again whenever targeting, validation, state mutation, and presentation can be proven independently.

### [!] 5.1 Exact card-based Movement capability

Includes movement allowance, obstacles, legal endpoints, and Wasp movement text required at the current tier.

### [!] 5.2 Fast Travel replacement capability

Implements only after exact movement works and Phase 3 has supplied the complete map with verified zone membership and adjacency.

Acceptance checks must exercise Fast Travel within the same zone, into an adjacent zone, rejection of a non-adjacent zone, enemy presence in the origin or destination zone, and the rule that no empty-space path is required.

### [!] 5.3 Skill and structured-effect capabilities

Adds only the typed effect primitives required by a verified Wasp card.

### [!] 5.4 Verified Wasp Attack targeting and range capabilities

Builds on the Phase 2 adjacent test Attack with verified Wasp values, Ranged targeting where required, and applicable card behavior. Minion targeting remains separately verified in milestone 5.5.

### [!] 5.5 Minion Attack capability

Includes minion targeting, Heavy immunity, staged defeat/removal, and immediate Push dependencies. Coin rewards remain blocked on milestone 7.1 and must not be implemented ad hoc here.

### [!] 5.6 Complete Hero Defense capability

Builds on the Phase 2 test-card comparison with the detailed attack/defense order, primary/secondary Defense behavior, applicable card text, Block, and verified Wasp requirements. Minion modifiers and item bonuses remain dependent on milestones 6.1 and 7.3 unless milestone 4.2 explicitly reorders their smaller prerequisites.

### [!] 5.7 Hero defeat, Lives, and respawn capability

Includes staged defeat/removal, Life spending, rewards, effect/marker consequences, and legal respawn. It cannot complete before required coin accounting and active/continuous-effect cancellation capabilities exist.

### [!] 5.8 Timing, Active-effect, and continuous-rule capabilities

Implements only verified Wasp requirements, using reusable typed triggers, durations, ordering choices, cancellation, and distinct passive/continuous sources.

## Phase 6 - Minions, lane, and end of round

Phase 6 uses the complete Phase 3 map. Lane-push behavior must never be tested against the reduced Phase 1 movement grid.

### [ ] 6.1 Minion occupancy and defense modifiers

### [ ] 6.2 Minions returning to the Battle Zone

### [ ] 6.3 End-of-round Minion Battle

### [ ] 6.4 Immediate Push trigger during a turn

### [ ] 6.5 Lane Push state transition and respawn

Uses the authoritative ordered lane, current Battle Zone, adjacent next zone toward the losing team's Throne, and that zone's verified minion spawn points.

### [ ] 6.6 Wave counters, Throne capture, and Last Push victory

### [ ] 6.7 Token removal and marker return

Token-owned properties persist through their own authoritative continuous-source lifetime until removal and are not modeled as card Active effects. Defeating the placing hero does not remove tokens unless a verified rule says otherwise.

### [ ] 6.8 Retrieve cards and cancel applicable Active effects

Observable outcome: At the correct end-of-round step, each hero retrieves only its own eligible resolved/discarded cards into hand and all Active effects canceled by those state changes end, including applicable Next Turn effects.

Acceptance checks:

- Retrieval uses explicit card-state transitions and never retrieves another hero's card.
- Every eligible card returns regardless of its prior eligible state, while ineligible/set-aside/item cards do not.
- Applicable Active effects are canceled from authoritative state and emit ordered cancellation events.
- Focused tests cover resolved, discarded, facedown eligible, other-owner, and active-effect cases.

Each milestone must cover its exact rule ordering and focused edge cases before the next begins.

## Phase 7 - Economy and progression

### [ ] 7.1 Coin awards and assist-ready accounting

The 1v1 configuration has no friendly assisting hero, but the model must support team collections without implementing 3v3 UI.

### [ ] 7.2 Mandatory end-of-round leveling

### [ ] 7.3 Card upgrades and item bonuses

### [ ] 7.4 Ultimate access

Ultimate behavior is exposed as a continuous rules source only while its hero is on the board, with its own applicability boundary rather than ordinary card Active-effect cancellation.

### [ ] 7.5 Pity Coin ordering

### [ ] 7.6 All three victory paths

### [ ] 7.7 Integrate the exact end-of-round sequence

Observable outcome: One authoritative end-of-round state machine executes all completed subsystems in the exact source-verified order and arrives at a valid next-round or game-ended state.

Acceptance checks:

- The sequence covers Minion Battle; token removal/marker return; Lane Push; card retrieval and cancellation; mandatory leveling/upgrades; and Pity Coin collection at their exact checkpoints.
- Applicable End of Round Active effects and immediate victory checks occur at their verified boundaries rather than presentation timing.
- A game-ending result stops later processing wherever the rules end the game immediately.
- Focused integration scenarios cover no Push, one Push, level/no-level players, retrieved effects, Pity Coin eligibility, and a game-ending Push.
- The same starting state and choices produce the same state and ordered event sequence.

## Phase 8 - Primitive complete match

### [ ] 8.1 Complete and verify all Wasp card behaviors

Observable outcome: Every Wasp definition and legal action at every pitch-relevant tier, including item/Ultimate and cross-system interactions, passes source-linked focused scenario tests after its Phase 5-7 dependencies are available.

Acceptance checks:

- Every Wasp action offer and invocation context from the milestone 4.2 capability matrix is implemented or explicitly blocked by a recorded source question.
- No placeholder value, display-name branch, runtime text parsing, or UI-owned rule path remains.
- Interaction tests cover each required timing, state, target, modifier, replacement, repeat, Active effect, and continuous source.
- Any unresolved source question prevents this milestone from being marked complete rather than being guessed.

### [ ] 8.2 Complete deterministic 1v1 match

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
