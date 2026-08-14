# Guards Digital - Card Effect Capabilities

Last updated: 2026-08-12

## Purpose and status

This document is the effect-level companion to `CARD_SYSTEM_ARCHITECTURE.md`. It defines the reusable rules capabilities that card definitions may compose so future cards do not spread special-case logic through the resolver, board model, UI, or presentation.

The baseline catalog is derived from the second-printing rulebook's action execution, card states, actions, progression, concepts, and keywords on printed pages 7-19. Verified card text may override a general rule or introduce a new capability. No hero-specific capability is authoritative until its card source and any applicable official correction are verified in `RULES_LEDGER.md`.

This catalog specifies extension contracts; only named roadmap slices are implemented. Milestones 1.11-1.13a establish typed printed Movement and resumable segmented choices. Milestones 2.2-2.5 add printed Attack/Defense, hero targeting, one-card-or-decline Defense, and outcome facts without consequences. Milestones 3.5 and 3.7-3.9 add Hold, turn composition, round checkpoints, and serialized continuations. Milestones 5.1-5.8 add exact-map Movement, typed Fast Travel, authoritative minions, timing, rewards, and complete general synthetic combat. Milestone 6.1 implements the end-of-round Minion-Battle system transition, its round-owned typed removal choice, and deferred Lane-Push request. Direct Move, resolve-without-action, Hero systemic consequences, concrete modifier producers, concrete Wasp effects, modal/repeat execution, Lane-Push consumption, broader lifecycle transitions, and broader mutation handlers remain unimplemented.

## Capability shard routing

- [Timing, control flow, and system transitions](Card-System/TIMING_AND_TRANSITIONS.md) owns Active/continuous lifecycles, resumable composition nodes, and card-triggered subsystem transitions.
- [Board capabilities](Card-System/BOARD_CAPABILITIES.md) owns spatial/object queries, movement and relocation, removal/Defeat, and prevention.
- [State capabilities](Card-System/STATE_CAPABILITIES.md) owns tokens/markers, card-state and progression operations, characteristics and immunity, values, resources, and ownership changes.
- This index retains the shared registration/execution contract, identity matrix, definition validation, test/coverage index, and extension workflow.

## Capability categories

Not every rulebook keyword is an effect that mutates state. The architecture keeps these categories distinct:

| Category | Responsibility | Examples |
| --- | --- | --- |
| Mutation capability | Performs one typed authoritative state transition | Move, Push, Place, Spawn, Swap, Remove, Discard |
| Query capability | Calculates legal objects or rules facts without mutation | Adjacent, Range, Radius, Nearest, Empty, Zone, Target |
| Modifier/restriction capability | Changes a derived value, permission, or effective characteristic | Attack bonus, Block, Immune, Counts As, ignore obstacles |
| Control-flow capability | Composes ordered effect nodes and choices | Sequence, Optional, Conditional, Choose, Repeat |
| Timing capability | Opens or reacts at a named rules checkpoint | Before Attack, Defense response, After Attack, End of Turn |
| Persistent-source capability | Supplies rules while a source remains applicable | Active effect, Ultimate, token-owned property |
| System-transition capability | Resolves a game subsystem rather than one ordinary leaf effect | Lane Push, Minion Battle, card retrieval, level up |

An `Adjacent` or `Target` node must not masquerade as a mutation handler. A `Push` handler may use Direction, Straight Line, occupancy, and obstacle queries, but those queries remain independently testable.

## Contract required for every capability

Each registered capability has an immutable definition contract and a pure rules executor. The contract records:

- Stable capability ID and schema version. This identifies the shared handler contract; it is distinct from the unique effect-node ID for one occurrence inside a card definition.
- Category and authoritative rule/card sources.
- Typed parameter schema, including required and optional fields.
- Legal source, actor, controller, chooser, and target roles.
- Invocation contexts in which it may appear.
- Preconditions and effective-characteristic queries.
- Choice schema and choice owner, if input is required.
- Exact versus `up to`, `may`, and `if able` failure behavior.
- Atomicity: single target, ordered multi-target, or simultaneous batch.
- State read set and state changes.
- Timing windows and system checkpoints opened before, during, and after it.
- Follow-up triggers and cancellation rules.
- Ordered rule-event types and payloads.
- Pending-resolution data needed to pause and resume.
- Definition validation and focused contract/scenario tests.

The runtime execution frame binds the immutable capability definition to stable source/actor/target IDs, the selected action offer, prior choices, current state revision, and continuation location. It contains no Unity object, delegate, coroutine, or hidden UI state.

## Effect node bindings and outcomes

Every structured effect node has a stable node ID, capability ID/version, typed input bindings, typed output bindings, and failure/batch policy.

Inputs may bind to explicit roles and prior results, such as `Actor`, `SourceCard`, `ChosenTarget`, `ChosenSpace`, `ChosenDirection`, a literal typed value, or the output of an earlier stable node. Outputs may expose target IDs, chosen path, intended/actual distance, stopping reason, placed/removed instance IDs, defeat/removal outcome, resource delta, computed value, or another capability-specific fact.

This lets later text express conditions such as `if that unit moved`, `if the target was removed`, or `if you gained a coin` without closures or a general expression language. The definition validator type-checks every binding and rejects missing, circular, incompatible, or cross-action references.

A handler returns one explicit execution result:

- `Applied`, including a legal rules no-op such as Push moving zero spaces.
- `NeedsChoice`, with a serializable pending-choice definition.
- `ImpossibleMandatory`, which stops the remaining action after prior completed steps.
- `DeclinedOptional`, which records the choice and continues according to the control node.

The result includes ordered semantic events and typed checkpoint facts. Pending resolution stores both accepted bindings and result facts so snapshot/Undo/network authority can resume exactly.

If a later mandatory node returns `ImpossibleMandatory`, earlier `Applied` nodes and their bound outcomes remain committed; the action stops but does not roll them back.

## Cross-cutting execution rules

1. Distinct rule terms keep distinct public capability IDs even when they share low-level occupancy helpers.
2. Validation is side-effect free. A rejected or stale command does not partially apply an effect.
3. A mandatory impossible step stops the remaining ordered action after preserving earlier completed steps. Optional wording follows its exact source semantics.
4. All player decisions become authoritative pending choices. The resolver never chooses an undisclosed target, distance, route, direction, mode, or token to remove on the player's behalf unless the rules make that choice automatic. A candidate may bind multiple disclosed facts as one choice; the Phase 1 Movement destination candidate explicitly includes the exact route and cost selected by its one destination command.
5. Capabilities declare whether they query intrinsic representation kind or effective rules characteristics. Type-based rules normally apply `Counts As`, immunity, and continuous modifiers; explicit miniature/representation rules use intrinsic kind.
6. Simultaneous effects validate against the correct pre-effect state and commit as one batch. They are not converted into order-dependent single-target loops.
7. Every capability emits semantic rule events, not animation commands. Presentation decides how `UnitPushed`, `TokenPlaced`, or `ObjectsSwapped` looks.
8. System checkpoints run at their rulebook boundary even if presentation is still animating prior events.
9. Adding a card never authorizes a display-name, hero-name, or image-name switch. The definition references capability IDs and typed parameters.
10. If existing parameters cannot express a verified effect exactly, add a new versioned capability or a controlled custom handler. Do not overload a similar term with different semantics.

## Core mutation identity matrix

| Public capability | Identity that must be preserved |
| --- | --- |
| Perform Movement action | Hero action with allowance, action-start restrictions, route choices, and possible Fast Travel replacement |
| Direct Move | Card-text relocation under movement restrictions, but not a Movement action and not Fast Travel eligible |
| Object Push | Official straight-line forced displacement directly away from the pushing unit; not Lane Push |
| Place | Board-or-supply piece to a different empty space; not movement |
| Spawn | Supply-only piece enters the board |
| Respawn | Hero/minion specialized eligibility and timing around Spawn/Place behavior |
| Swap board objects | Atomic position exchange; neither movement nor placement |
| Replace board object | Atomic substitution of one object for a different object |
| Remove | Piece leaves board without defeat rewards |
| Defeat | Enemy outcome with rewards and a potentially preventable removal stage |
| Clear | Replacement for an entire Attack that removes adjacent tokens |
| Discard/Retrieve/Swap card state | Card lifecycle operations, not board-object operations |
| Give/Return marker | Attachment and supply operations, not token placement |
| Gain/Spend resource | Typed economy/Life/Wave/level state change |
| Create/Cancel Active effect | Serializable timed source with ordinary cancellation semantics |
| Add/Remove continuous rule source | Persistent permission, restriction, modifier, token, marker, or Ultimate source with its own lifetime |

Three different meanings of `replace` remain separate: replace an action, replace a board object, and replace/upgrade a card definition/instance.

## Capability definition validation

Definitions are rejected before a match starts when any of these checks fail:

- Capability ID/version is unknown or its pure handler is not registered.
- Typed input/output binding is missing, incompatible, circular, or references an unreachable node/result.
- Actor, source, chooser, target category, allegiance, or intrinsic/effective-type query is invalid.
- Source container, destination, occupancy, supply, or endpoint policy is incomplete for the operation.
- Exact/up-to/as-far/optional/mandatory behavior is ambiguous.
- Shared-distance or simultaneous-batch requirements are missing or conflict with ordered execution.
- Timing window, Active-effect duration/cancellation, or continuous-source lifetime is incomplete.
- A system checkpoint can be requested by an operation that is not allowed to trigger it.
- Definition tries to treat Movement, direct Move, Object Push, Place, Spawn, Swap, Replace, Remove, Defeat, or Lane Push as interchangeable.
- Authoritative values depend on presentation, art, Unity objects, runtime text parsing, or display-name branching.

## Required capability tests

Each capability receives pure conformance tests when first implemented, plus source-linked scenarios for every card that uses it.

Minimum regression pairs include:

- Movement action versus direct Move, including Fast Travel eligibility and action-start restriction checks.
- Push full distance, partial distance, zero distance, ordinary/ignored obstacles, and one shared-distance simultaneous batch.
- Source-relative direct Move behavior, including `move toward` destination constraints, chooser/tie rules, exact/up-to distance, and ordinary movement restrictions.
- Place versus Spawn versus Hero/Minion Respawn, including occupied destinations, different-space requirement, supply origin, hero decline resolving without action, spawn-over-token, and absence of movement triggers.
- Atomic Swap and Replace behavior under occupancy conflicts.
- Remove versus Defeat rewards, removal prevention after defeat, and staged outcome events.
- Lane-Push request after last-minion Defeat/successful Remove, including Defeat with prevented removal, and no request after Move/Object Push/Swap or a prevented standalone Remove.
- Lane-Push transition order, early game-ending stops, token-covered Spawn points, non-token-obstacle nearest-space choice, Tie-Breaker-side ordering without a flip, immediate-before-Repeat timing, and end-of-round cleanup-before-Push timing.
- After-action minion-miniature correction, including intrinsic kind versus objects that merely count as minions, shortest-path team choice, no-path Place fallback, Tie-Breaker-side ordering without a flip, and ordering between Repeat instances.
- Token finite supply/reclamation choice, persistent properties, movement/displacement, Clear selecting any adjacent-token subset, spawn-over-token, end-round removal, and survival of the placing hero's defeat.
- Marker attachment, continuous modifier lifetime, defeat/end-round return, and separation from token occupancy.
- Target versus presence, self versus friendly, Range/Radius with no line of sight, and shortest-distance queries through obstacle spaces.
- Immunity for both chosen and automatically affected targets by originating action/effect type, presence checks, self-action exception, and verified explicit overrides.
- `Counts As` effective type versus intrinsic representation exceptions.
- Every handler result (`Applied`, legal no-op, `NeedsChoice`, `ImpossibleMandatory`, `DeclinedOptional`) and typed result binding used by a later node.
- Pending choice/resolution serialization, stale-choice rejection, determinism, and ordered semantic events/checkpoint facts.

## General rulebook coverage index

| Terms | Architecture owner |
| --- | --- |
| Active Effects; This Turn/Round; Next Turn; End of Turn/Round | Timing and persistent-source capabilities |
| Adjacent; Range; Radius; Direction; Straight Line | Spatial query capabilities |
| Closer; Farther/Farthest; Nearest | Distance/order query capabilities |
| Battle Zone; Zone; Empty; Spawn Point; Terrain; Space | Map/space query capabilities |
| Friendly; Hero; Minion; Unit; Token; Target; Immune | Object relationship, target/presence, and effective-characteristic queries |
| Counts As | Effective-characteristic replacement modifier |
| Move; Move Through; Ignoring Obstacles | Movement/direct-move and permission capabilities |
| Place; Spawn; Respawn | Relocation and supply capabilities |
| Push | Official forced-displacement capability |
| Swap/Replace board objects | Atomic board-object exchange/substitution capabilities |
| Swap card states; replace/upgrade a card | Distinct card-state and immutable-definition transition capabilities |
| Remove; Defeat; Block | Staged outcome, prevention, and result capabilities |
| Tokens; Markers; Obstacles | Stable instances, supply, occupancy, attachment, and continuous-property capabilities |
| Discard; Retrieve; Resolved; Unresolved | Orthogonal card-state transition capabilities |
| Choose; If Able; Repeat | Resumable control-flow capabilities |

## Adding a new effect from a verified card

1. Transcribe and verify the exact card text, values, and corrections.
2. Split the text into ordered control nodes, queries, modifiers, mutations, timing, and system checkpoints.
3. Map each node to an existing capability only when its semantics match exactly.
4. Record all actor/source/chooser/target roles and optional/mandatory wording.
5. Record atomicity, shared choices, failure behavior, triggers, and cancellation.
6. Add a versioned reusable capability if the card reveals a general rule concept.
7. Use a controlled custom pure handler only when the behavior is genuinely unique.
8. Add definition validation, capability contract tests, and source-linked card scenarios.
9. Add interaction tests for relevant immunity, Counts As, obstacles, tokens, timing, replacement, repeat, and state changes.
10. Update this catalog and `RULES_LEDGER.md` before marking the card implemented.

## Implementation boundary

The catalog is intentionally broader than the current code, but it is not authorization to build every handler early.

- Phase 1 implements only the action/choice/value seams and the minimum segmented-destination Movement capability needed by its approved card-driven movement slice.
- Phase 2 proves nested Attack/Defense reaction using the same capability contracts.
- Phases 3 through 7 implement only the verified general/core capabilities needed to prove the complete match loop with synthetic definitions. Milestone 4.4b established the stable typed Movement and Attack/Defense strategy-handler registry while preserving shared dispatcher invariants; milestone 6.1h removed that registry once every interface had exactly one implementation, so today a later handler is a concrete backend composed directly, and the registered action/effect dispatch seam future capabilities extend is milestone 6.2a's design. Either way, a new handler must not enlarge unrelated action-kind control flow.
- After that core-loop gate, milestone 8.2 maps verified Wasp content, identifies Wasp-specific capability deltas or refinements, and orders the remaining Wasp work; it does not gate general core capabilities.
- After Wasp is complete, the five additional pitch heroes are selected and inventoried before a roster-wide delta map re-baselines their implementation. Each hero extends the catalog with verified reusable capabilities or isolated handlers without rewriting card instances, action offers, pending choices, commands/events, or snapshot foundations.

The foundation is considered successful when Object Push, source-relative direct Move, Place, tokens, or another future effect can be introduced by registering/mapping the exact capability and tests, rather than adding branches across unrelated systems.

Handler composition is explicit and typed. Milestone 6.1h removed the interim strategy registry, so the concrete backends are composed directly today; when milestone 6.2a lands the registered action/effect dispatch seam, registration uses stable action/capability identity and the capability's declared input/output contract. Neither form may use reflection discovery, display-name branches, a service locator, an untyped dictionary payload, or a universal expression language. Handlers remain pure rules collaborators and never reference match-session presenters, Unity views, transport callbacks, or animation state.

## Contract implementation status

| Contract | Status | Owner |
| --- | --- | --- |
| Registered action/effect dispatch seam with typed handler registration | Planned, not implemented; the concrete backends are composed directly since milestone 6.1h removed the interim registry | 6.2a |
