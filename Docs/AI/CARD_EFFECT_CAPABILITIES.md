# Guards Digital - Card Effect Capabilities

Last updated: 2026-08-09

## Purpose and status

This document is the effect-level companion to `CARD_SYSTEM_ARCHITECTURE.md`. It defines the reusable rules capabilities that card definitions may compose so future cards do not spread special-case logic through the resolver, board model, UI, or presentation.

The baseline catalog is derived from the second-printing rulebook's action execution, card states, actions, progression, concepts, and keywords on printed pages 7-19. Verified card text may override a general rule or introduce a new capability. No hero-specific capability is authoritative until its card source and any applicable official correction are verified in `RULES_LEDGER.md`.

This is an architecture specification, not evidence that these effects are implemented. A roadmap milestone implements only the smallest capabilities it explicitly names. Milestone 1.11 implemented the placeholder printed Movement value query/contribution seam. Milestone 1.12 implemented the pure placeholder Movement pending-choice shell, corrective milestone 1.12a made its authority and continuation self-contained/restorable, corrective milestone 1.12b projects immutable reachable destination/route/cost candidates and commits the selected candidate's adjacent steps through the existing authorities, milestone 1.13 proved that capability through a visible synthetic card/action/pending-choice presentation bridge, and corrective milestone 1.13a lets repeated waypoint segments accumulate inside that same pending Movement action until Stop or allowance exhaustion. Phase 1 did not implement Fast Travel, direct Move, concrete action-start restriction effects, modifiers, card-text effects, modal/repeat execution, reactions, timing effects, card lifecycle transitions, snapshot import/export, or broader mutation handlers.

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

## Spatial and object query capabilities

### Object categories and relationships

Queries support spaces, heroes, minions, units, tokens, markers, cards, zones, terrain, spawn points, and other explicitly modeled game pieces. Each object exposes an immutable intrinsic representation kind and a derived effective rules type.

Relationship filters include self, another, friendly, enemy, owner, controller, and source. `Friendly` excludes self. A hero/minion is both a unit and an obstacle for occupancy, so an obstacle filter must not make a legal unit untargetable.

### Target versus presence

`TargetQuery` evaluates target eligibility for both player-chosen and automatically affected objects and applies immunity in either case. A separate `ChooseTarget` node creates a pending choice among eligible candidates when the action requires a player decision. `PresenceQuery` checks whether an object exists or is adjacent/in range without targeting it; immune units still count for presence checks where the rules say so. Executors must declare which query they use.

### Geometry

Reusable geometry queries include:

- `AdjacentSpaceQuery` and `AdjacentObjectQuery`.
- `RangeQuery` and `RadiusQuery`, with no invented line of sight.
- `DirectionQuery` over the six named hex directions.
- `StraightLineQuery` for spaces or units in one hex direction/file.
- `DistanceQuery`, counting spaces including spaces with obstacles where required.
- `CloserQuery`, `FartherQuery`, `FarthestQuery`, and `NearestQuery`.
- `SameZoneQuery`, `AdjacentZoneQuery`, `BattleZoneQuery`, and ordered-lane queries.
- `EmptySpaceQuery`, `EmptySpawnPointQuery`, occupancy, terrain, obstacle, and token filters.

Movement path, rulebook distance, Range, Radius, straight line, and zone adjacency are different calculations. A capability selects the required query explicitly.

Zone adjacency follows the board rule: two zones are adjacent when there is an adjacent pair of non-terrain spaces, one in each zone. It is not inferred from bounding boxes or art proximity and is verified against authoritative map data.

## Movement, forced displacement, and relocation

### Perform Movement action

`PerformMovementAction` uses the selected action offer's typed Movement value and checks action-start movement limits/restrictions once. The Phase 1 shell creates an immutable pending choice of currently reachable destinations, each bound to one disclosed deterministic adjacent route and cost. Selecting a destination revalidates that candidate, commits each route step through the adjacent-move authority, spends one allowance per step, and completes the `up to` action; Stop selects zero movement. Each step enters an adjacent empty legal space and respects obstacles.

Each segmented destination choice intentionally projects one deterministic shortest route per currently offered destination. After a partial segment, the same action preserves cumulative route facts and ordered step events, subtracts the segment cost, and offers another choice from the new position. Waypoints therefore let the player construct deliberately longer/non-shortest cumulative routes without pre-enumerating every possible complete route. Exact full-map Movement must integrate and source-test this interaction on the official topology and add concrete action-start restriction evaluation; it must not silently replace the recorded route.

Primary/Secondary context controls card-text application. A Movement action may be replaced by Fast Travel when eligible. It emits action, step, and completion events while leaving presentation timing separate.

### Direct Move effect

`MoveEffect` represents movement written inside card text. It uses ordinary movement restrictions unless its text overrides them, but it is not a Movement action:

- It cannot become Fast Travel.
- Preventing a Movement action does not automatically prevent it.
- Exact distance is mandatory unless wording such as `may move` or `up to` makes it optional.
- Its source text determines whether the acting hero or another unit/token moves.

Card wording such as `move a unit towards you` remains `MoveEffect`; `towards` is not promoted into a standalone keyword. Its target/destination contract composes `CloserQuery` so every legal destination is closer to the source than the target's starting space under obstacle-inclusive geometric distance, plus the source's exact/up-to distance policy and any required chooser/tie rules. There is no separate Pull capability or invented Pull keyword.

### Ignore obstacles and Move Through

`IgnoreObstaclesModifier` changes transit legality for the specified effect/action. The unit may enter and leave an obstacle space during transit but cannot end on an obstacle. Traversal does not target objects being traversed, so their immunity does not itself block transit; token traversal effects still apply. `MoveThrough` uses the same transit policy but remains traceable to its source capability/wording.

### Push

`PushEffect` implements the official Push keyword:

- The pushing unit and target unit/token are explicit. A different anchor is legal only when verified text defines that override or a separately named capability.
- The target moves in a straight line directly away from the pushing unit.
- Requested distance is typed and may be fixed or chosen according to source text.
- If full distance is impossible, the target moves as far as possible, including zero.
- Ignore-obstacle/move-through variants still require a legal empty endpoint.
- A source that pushes multiple targets `up to X` records one chosen distance and moves all affected targets simultaneously.
- Push is not a Movement action or Place effect and does not itself trigger a Lane Push merely by displacing the last minion.
- Minions outside the Battle Zone are corrected at the after-action checkpoint, not after each pushed step.

The executor emits `PushDeclared`, a simultaneous `ObjectsPushed` batch where applicable, and occupancy events. The action/system controller alone runs after-action minion correction after all action nodes finish.

### Place

`PlaceEffect` takes a game piece from the board or supply and puts it into a different empty space. It does not count as movement. The contract declares source container, eligible piece type, destination query, chooser, and what happens when no legal empty destination exists.

Placing tokens additionally uses the limited-supply rules in the token section. Placing a board object must not silently use Spawn semantics.

### Spawn and Respawn

`SpawnEffect` takes an object from supply and puts it into a legal empty board space. Unlike Place, it cannot take the object from another board space. It validates supply and the source-defined destination query. Spawn-point type/color constraints apply only when the invoking rule/card requires them. Whenever a unit Spawns on a token-covered Spawn point, the token is removed first.

Respawn is a specialized capability family, not one generic spatial handler:

- `HeroRespawnTransition` uses the optional before-action timing, an empty Spawn point in the hero's Throne Zone, or the rulebook's nearest empty fallback adjacent to a Throne-Zone Spawn point when none are available.
- Declining hero respawn is an explicit authoritative choice/outcome that resolves the selected card without performing an action; it is not treated as failed Spawn.
- `MinionRespawnEffect` uses an eligible off-board minion miniature/pool and the rulebook's type/color spawn-point versus existing-minion condition; it is not casually treated as generic Spawn supply.
- Failure and fallback placement use the exact applicable source rather than a generic Spawn guess.

The two contracts may share supply, destination, and occupancy queries, but keep separate eligibility, timing, events, and tests.

### Swap board objects

`SwapBoardObjectsEffect` exchanges two legal board positions atomically. It is neither movement nor placement. It validates both objects and both resulting occupancies against the same pre-swap state, then emits one semantic swap transaction.

Moving, pushing, or swapping a minion does not cause a Lane Push. Any minion left outside the Battle Zone is handled by the after-action correction system.

### Replace board object

`ReplaceBoardObjectEffect` exchanges an existing object for a different object. Its definition declares where the replacement comes from, where the old object goes, whether properties transfer, and which triggers observe replacement. It is not an alias for Remove followed by Spawn unless verified rules text defines those exact observable stages.

## Removal, defeat, prevention, and battle consequences

### Remove

`RemoveEffect` takes a game piece off the board without defeat rewards. It records the destination (supply, pool, or other defined location) and runs applicable removal/prevention triggers. Removing the last enemy minion in the Battle Zone can trigger an immediate Lane Push; moving, pushing, or swapping it cannot.

### Defeat

`DefeatEffect` is a staged outcome, not deletion. Its contract preserves these distinct required facts/operations without using the list order below as an unverified timing claim:

- Validate that the target can be defeated by this source; friendly units cannot be defeated.
- Record defeat and its reward eligibility.
- Open any triggered choices/effects around the removal attempt and record removed or removal-prevented outcome.
- Award the appropriate defeat/assist rewards even when a verified prevention effect keeps a defeated minion on the board.
- Resolve hero-specific consequences such as unresolved-card resolution, Active-effect cancellation, marker return, Life spending, victory, and later respawn at their exact checkpoints.

The precise stage ordering is capability- and source-tested before implementation. Presentation receives separate defeat, prevention, reward, and removal events.

### Prevent removal and Block

`PreventRemovalEffect` intercepts the removal stage; it does not undo defeat or reward unless exact card text says otherwise.

`BlockResult` is a Defense-specific conditional result override that prevents an Attack according to its condition. It is not a large numeric Defense value and does not share `PreventRemovalEffect` semantics.

### Clear

`ClearAction` is a rule-provided optional replacement for an entire Attack action. Once selected, its controller may choose any subset of tokens adjacent to the acting hero, including none, and removes the chosen tokens through token removal capabilities. It suppresses the replaced Primary Attack text and is not an Attack that happens to target tokens.

## Tokens, markers, obstacles, and terrain

### Token instances and supply

Every placed token has a stable instance ID, token definition ID/revision, owner/controller/source links, occupied space, and persistent property source. Tokens:

- Are obstacles and are not units.
- Use an authoritative limited supply.
- Retain their properties until removed; those properties are continuous sources, not ordinary Active effects.
- Are not removed merely because the hero who placed them is defeated.
- Are normally removed at end of round unless exact text says otherwise.
- Are removed before a unit Spawns on their covered Spawn point.
- May be removed by Clear or another explicit Remove effect.

When a card requests more tokens than remain in supply, its typed token-placement capability follows the rulebook's removal/reuse choice before placement. The pending choice identifies which same-type tokens may be removed and never lets UI create extra copies.

### Token property capabilities

A token definition composes explicit continuous/query/trigger capabilities such as obstacle classification, terrain-like restriction, entry/exit effect, modifier source, immunity interaction, or removal condition. Token art does not define behavior.

Entry/exit effects run from authoritative movement/displacement checkpoints. A unit that ignores obstacles still applies token effects while transiting where the rules require it.

Move, Object Push, Swap, Replace, and Remove may affect a token only when the source definition's target contract permits tokens. The stable token instance and its persistent properties move or change location with the operation; they are never reconstructed from art.

### Markers

Markers are stable reminder/effect attachments, usually assigned to heroes rather than placed on board spaces. `GiveMarkerEffect` records marker definition, owner, holder, source effect, and return condition. `ReturnMarkerEffect` detaches it and returns it to the correct supply/owner.

A marker may provide an attached continuous modifier when verified text requires it. That modifier follows the marker attachment lifetime rather than token or card Active-effect rules.

Markers are not tokens, do not occupy spaces, and do not inherit token removal or obstacle rules. All markers return to supply at end of round, and every marker held by a defeated hero is returned. Exact effects may add an earlier return condition, but cannot weaken those general cleanup checkpoints without verified overriding text.

### Obstacles and terrain

`ObstacleQuery` uses effective characteristics. Units, terrain, tokens, and qualifying miniatures/objects are obstacles, with no more than one obstacle per space.

Terrain is a space classification and obstacle, not a token or unit. Effects that add/remove terrain-like properties do so through explicit continuous characteristics without replacing the underlying space ID.

## Card-state and progression capabilities

Reusable card-state transitions include:

- `PlayCard`, `RevealCard`, `ResolveCard`, and `ResolveWithoutAction`.
- `DiscardCard` from the source-allowed state, including exactly one in-hand Defense card.
- `RetrieveCard` for the correct owner and allowed states.
- `SwapCardStates` as the rulebook-defined state exchange, not two arbitrary list moves.
- `TurnCardFacedown`/`TurnCardFaceup` with the correct resolved/discarded information suppression and Active-effect cancellation.
- `UpgradeCard`, `SetAsideAsItem`, and `UnlockUltimate` using immutable definitions and explicit instance transitions.

Card-state effects retain orthogonal location, played, resolution, and orientation facts. Every transition validates owner, source state, and definition revision.

## Characteristics, immunity, permissions, and restrictions

### Counts As

`CountsAsModifier` replaces all applicable rules and qualities of the object with the specified effective type(s) for its duration. Type-based targeting, occupancy, immunity, rewards, and other rules queries use those effective characteristics. A capability that cites intrinsic miniatures/representation explicitly declares `IntrinsicKindQuery`; for example, the p.18 Battle-Zone correction applies only to minion miniatures and excludes objects that merely count as minions.

### Immunity

`ImmunityModifier` declares immune object, scope (action/effect/type/source), duration, and exceptions. Immunity prevents targeting and being affected but does not remove the object from presence checks. A hero is never immune to their own actions.

### Permissions and prohibitions

Typed permission/restriction capabilities include:

- Prevent or allow a named action.
- Limit Movement distance, checked at Movement action start where required.
- Ignore obstacles or move through named obstacle types.
- Allow otherwise illegal targets or spaces.
- Prevent removal, a named outcome, card-state change, or another exact capability.

These modify capability queries. They do not inject card-name branches into executors.

## Values, resources, and ownership changes

`ModifyValueEffect` references a typed value channel and contribution source, such as Initiative, Attack, Defense, Movement, Range, or Radius. Printed icons, items, card text, minions, and Active effects remain separate contributions.

Other typed state changes include gain/spend coins, spend Lives, flip/remove Wave counters, change hero level, and assign/return ownership or control where verified text permits it. Resource effects declare minimums, failure behavior, recipient, source, and emitted contribution events; they never use negative magic numbers as hidden semantics.

## Timing, Active effects, and continuous sources

`CreateActiveEffect` produces a serializable instance with source card/action, controller, affected scope, trigger window, duration, ordering data, uniqueness key, effect definition, and cancellation conditions.

Supported timing definitions are introduced from verified needs and include:

- Before Attack and After Attack.
- Defense response and Defense/Attack end.
- This Turn and This Round.
- Next Turn and Next Turn after Playing Cards.
- End of Turn and End of Round.
- Action start, effect step, action end, and system checkpoints.

Simultaneous Active effects use the official tie procedure, record required choices, and do not flip the Tie Breaker in that ordering window.

`ContinuousRuleSource` is separate. Ultimates apply only while their hero is on the board; token properties persist while the token remains. Their applicability is queried directly and does not inherit card Active-effect cancellation.

## Control flow and action composition

Reusable structured nodes include:

- `Sequence` for exact printed order.
- `Optional` for `may`, `up to`, and other source-defined optionality.
- `Conditional` and presence/target predicates.
- `Choose` for modal one/multiple options and chooser-owned order.
- `RepeatAction` with explicit maximum, repeat ancestry, no recursive repeat, different modal choices allowed, no replacement of the repeated action, and Active-effect uniqueness.
- `PerformAction` for text that explicitly grants another action, preserving the difference from a direct effect such as `move`.
- `ReplaceAction` for Fast Travel, Clear, or a verified card replacement.
- `SimultaneousBatch` for one shared choice applied to multiple objects atomically.

Control nodes store stable node IDs so a pending resolution can serialize and resume at the exact location.

## System-transition capabilities used by cards

Some card effects can trigger or depend on larger systems. These remain dedicated authoritative transitions:

- `ReturnMinionsToBattleZoneAfterAction` applies only to intrinsic minion miniatures and excludes objects that merely count as minions. Each miniature uses a shortest empty path; equal paths use the official team choice. If no path exists or it cannot move, Place it into a nearest empty Battle-Zone space. When both teams require correction, the team holding the Tie Breaker resolves first without flipping it. The checkpoint runs after the entire action and therefore between separate Repeat instances, not after each atomic displacement.
- `RequestLanePush` is emitted only from a recorded Defeat or successful Remove of the last relevant minion; an ordinary Move, Object Push, Swap, or prevented standalone Remove never emits it. A recorded Defeat remains qualifying if its later removal is prevented because prevention does not erase the Defeat outcome. The plain-data request records lane, winning/losing teams, cause outcome ID, immediate/end-of-round context, checkpoint, and state revision.
- `ResolveLanePushTransition` consumes that request through one ordered system transition: flip one Wave counter and stop on the final-Wave victory; otherwise advance the Battle Zone one Zone toward the losing team's Throne and stop on Throne capture; otherwise remove all minions in the previous Battle Zone and respawn the matching type at every verified new-zone Spawn point.
- During that respawn, remove a token covering a Spawn point before spawning. If a non-token obstacle covers a Spawn point, the owning team chooses a nearest empty Battle-Zone space and Places the minion there; when cross-team order matters, the Tie-Breaker-side team starts without flipping the coin.
- An immediate mid-turn request is consumed before a Repeat instance or later initiative action continues. An end-of-round request is consumed only after token/marker cleanup and before card retrieval.
- `ResolveMinionBattle`, end-of-round cleanup, card retrieval, mandatory leveling, upgrades, and Pity Coin ordering run at system checkpoints.
- Victory evaluation runs immediately at the source-defined Life/Wave/Throne/level condition and can stop later resolution when the game ends.

A card effect requests or triggers these transitions through typed operation facts/checkpoints; it does not duplicate their logic in a card handler. A generic `OccupancyChanged` event is insufficient because Move, Push, Swap, Remove, and Defeat have different systemic consequences.

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
- Phases 3 through 7 implement only the verified general/core capabilities needed to prove the complete match loop with synthetic definitions.
- After that core-loop gate, milestone 8.2 maps verified Wasp content, identifies Wasp-specific capability deltas or refinements, and orders the remaining Wasp work; it does not gate general core capabilities.
- Later heroes extend the catalog with verified reusable capabilities or isolated handlers without rewriting card instances, action offers, pending choices, commands/events, or snapshot foundations.

The foundation is considered successful when Object Push, source-relative direct Move, Place, tokens, or another future effect can be introduced by registering/mapping the exact capability and tests, rather than adding branches across unrelated systems.
