# Guards Digital - Board Effect Capabilities

Canonical effect-capability shard. Return to the [Card Effect Capabilities index](../CARD_EFFECT_CAPABILITIES.md).

## Spatial and object query capabilities

### Object categories and relationships

Queries support spaces, heroes, minions, units, tokens, markers, cards, zones, terrain, spawn points, and other explicitly modeled game pieces. Each object exposes an immutable intrinsic representation kind and a derived effective rules type.

Relationship filters include self, another, friendly, enemy, owner, controller, and source. `Friendly` excludes self. A hero/minion is both a unit and an obstacle for occupancy, so an obstacle filter must not make a legal unit untargetable.

### Target versus presence

`TargetQuery` evaluates target eligibility for both player-chosen and automatically affected objects and applies immunity in either case. A separate `ChooseTarget` node creates a pending choice among eligible candidates when the action requires a player decision. `PresenceQuery` checks whether an object exists or is adjacent/in range without targeting it; immune units still count for presence checks where the rules say so. Executors must declare which query they use.

Milestone 6.8k implements the disk-shaped `TargetQuery` slice - every board object within a rulebook distance, immunity filtered per activity, tokens exempt because p.19 leaves them outside the unit rules - and realizes `ChooseTarget` as the registered `synthetic-choose-target` capability rather than a new node kind: the selection pauses through the existing capability-choice seam and publishes the chosen object's space as a typed coordinate output, which a later node binds through the ordinary node-output binding. The wider relationship vocabulary (friendly, enemy, owner, controller, source) stays specified here until a verified card needs it.

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

Milestone 2.3a implements only the hero-Attack subset: axial-coordinate rulebook distance and an inclusive Range Total comparison for enemy heroes, with Range 1 including adjacency and occupied/obstacle spaces creating no line-of-sight restriction. Broader object categories, Range modifiers, Radius, straight-line, immunity, and presence interactions remain specified but unimplemented.

Milestone 5.3 binds the existing configured standard-minion state directly into authoritative board occupancy. Minions retain stable identity, team, intrinsic type, map-qualified space, and coordinate; they are unit obstacles for construction, Movement projection, route revalidation, and relocation, with no presentation-derived or parallel rules representation.

Zone adjacency follows the board rule: two zones are adjacent when there is an adjacent pair of non-terrain spaces, one in each zone. It is not inferred from bounding boxes or art proximity and is verified against authoritative map data.

## Movement, forced displacement, and relocation

### Perform Movement action

`PerformMovementAction` uses the selected action offer's typed Movement value and checks action-start movement limits/restrictions once. The Phase 1 shell creates an immutable pending choice of currently reachable destinations, each bound to one disclosed deterministic adjacent route and cost. Selecting a destination revalidates that candidate, commits each route step through the adjacent-move authority, spends one allowance per step, and completes the `up to` action; Stop selects zero movement. Each step enters an adjacent empty legal space and respects obstacles.

Each segmented destination choice intentionally projects one deterministic shortest route per currently offered destination. After a partial segment, the same action preserves cumulative route facts and ordered step events, subtracts the segment cost, and offers another choice from the new position. Waypoints therefore let the player construct deliberately longer/non-shortest cumulative routes without pre-enumerating every possible complete route. Milestone 5.1 integrates and source-tests this interaction on the official topology: a typed action-start evaluator receives the actor, source card, action, invocation context, and typed Movement value exactly once; prevention rejects atomically, while a distance cap becomes the captured session allowance used by every later segment and recovery path. Concrete persistent restriction sources and lifetimes remain deferred.

Primary/Secondary context controls card-text application. Milestone 5.2 implements the Fast Travel replacement as a separate value-free typed action: a current replaceable printed Movement offer supplies the replaced-action identity, while the replacement invocation suppresses source-card text. It atomically relocates to an empty space in the same or a shared-border adjacent zone only when neither endpoint zone contains an enemy hero or minion, requires no empty-space path, and emits ordered replacement/relocation/completion facts. Ordinary Movement continues to emit action, step, and completion events while leaving presentation timing separate.

### Direct Move effect

`MoveEffect` represents movement written inside card text. It uses ordinary movement restrictions unless its text overrides them, but it is not a Movement action:

- It cannot become Fast Travel.
- Preventing a Movement action does not automatically prevent it.
- Exact distance is mandatory unless wording such as `may move` or `up to` makes it optional.
- Its source text determines whether the acting hero or another unit/token moves.

Card wording such as `move a unit towards you` remains `MoveEffect`; `towards` is not promoted into a standalone keyword. Its target/destination contract composes `CloserQuery` so every legal destination is closer to the source than the target's starting space under obstacle-inclusive geometric distance, plus the source's exact/up-to distance policy and any required chooser/tie rules. There is no separate Pull capability or invented Pull keyword.

Milestone 6.4 implements this as the registered `synthetic-direct-move` capability. It takes the mover, distance, and `towardActor` policy as typed inputs, and derives its destinations from the same reachability the Movement action uses rather than a second traversal, which is p.11's "same rules and restrictions" made structural. The exact-versus-`up to` distinction is read from the node's own `EffectFailurePolicy` instead of a parameter of its own, because p.11 ties it to the wording that the four policies already carry: an `up to` node admits every destination at or below the bound, and any other policy admits only the exact number of spaces. `CloserQuery` is composed from that capability rather than existing unread. Milestone 6.8k widened the subject: the mover parameter is a typed coordinate naming whatever stands there - a hero, a minion, or a token - resolved through the displacement port the way Object Push resolves its targets, so p.11's "another unit or a token" is expressible. A definition fills that parameter from a prior target-selection node's published space, never from a role, because roles resolve from the execution frame alone and no role can supply a coordinate; the definition validator refuses one that tries at content load.

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

The executor emits `PushDeclared`, a simultaneous `ObjectsPushed` batch where applicable, and occupancy events. The action/system controller alone runs after-action minion correction after all action nodes finish - implemented at milestone 6.4a: the card-action dispatcher invokes the correction once when a Skill's effect nodes and batches have all finished, derives everything from board position through `BattleZoneQuery`, and holds the source card unresolved until the correction completes. A minion follows a shortest empty path back, its team choosing among equally short returns and among its own displaced minions (the 2026-08-16 correction-order amendment); with no path it is Placed into a nearest empty Battle-Zone space, and with no empty space either it stays where it ended up as a typed left-in-place outcome. Cross-team order is the Tie Breaker's, read but never flipped.

Milestone 6.4 implements this as the registered `synthetic-object-push` capability over a shared `PushPathQuery`. The direction comes from the straight-line query rather than effect-local geometry, and the walk asks the obstacle query p.18's two separate questions: transit legality, which the pushing effect's own ignore-obstacles flag or the target's Move-Through permission can answer, and endpoint legality, which nothing can, so a permitted target crosses an obstacle but still has to reach an empty space. A blocked target is a short push and a target blocked immediately is a zero-space push; both are applied results rather than failures, and a pusher and target sharing no file is the one refusal, kept distinct from a direction that exists and cannot be used. The `up to X` multi-target case is one node with one decision: the shared distance is chosen once, every target is resolved against one pre-effect state before anything commits, and the commits then run farthest-from-the-pusher first so a batch never refuses itself. That discharges the one-batch requirement inside the capability; the general `SimultaneousBatch` control node stays deferred, as the timing shard's status table records. Immunity filters the target set - p.18 makes it a targeting rule - while a presence question over the same spaces would still count the immune unit. A pushed token holds no immunity of its own because p.19 states tokens are not units, so the rules-object vocabulary has no token member and reports the named absence instead.

### Place

`PlaceEffect` takes a game piece from the board or supply and puts it into a different empty space. It does not count as movement. The contract declares source container, eligible piece type, destination query, chooser, and what happens when no legal empty destination exists.

Placing tokens additionally uses the limited-supply rules in the token section. Placing a board object must not silently use Spawn semantics.

Milestone 6.4a implements the minimum correction-owned Place path: the displaced-minion fallback carries typed policy/outcome data - correction kind, candidate destinations from `NearestQuery` over empty Battle-Zone spaces, and a left-in-place outcome for a full zone - and emits Place rather than Movement facts, with no route because Place is not movement. It is an internal authority operation rather than a registered effect capability, because no card text drives Place yet and a registration without a definition binding would be a dead contract; milestone 6.5 migrates this path to, or proves it conformant with, the general `PlaceEffect`.

### Spawn and Respawn

`SpawnEffect` takes an object from supply and puts it into a legal empty board space. Unlike Place, it cannot take the object from another board space. It validates supply and the source-defined destination query. Spawn-point type/color constraints apply only when the invoking rule/card requires them. Whenever a unit Spawns on a token-covered Spawn point, the token is removed first.

Milestone 6.3 implements only that mandatory token-clearance pre-commit: given an already validated configured Spawn point on the selected map, the board removes and returns its covering token before the owning Spawn capability creates an occupant. Supply eligibility, destination validation, occupant creation, events, choices, and the atomic enclosing transaction remain milestone 6.5.

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

Milestone 5.6 implements the verified standard-minion slice in exact order after legal target revalidation: record the minion Defeat, award the acting hero 2 coins for Melee/Ranged or 4 for Heavy through milestone 5.5's idempotent accounting, record a removal attempt, then record the successful removed result and update occupancy. Minions open no Defense frame and do not use the hero-only Attack Total comparison. The Heavy target query rejects while a friendly non-Heavy remains in the current starting Battle Zone. No removal-prevention source, last-minion Lane-Push request, displaced-minion correction, hero consequence, or UI selection path is added.

Milestone 5.8 implements the Hero subset only through a staged Defeat outcome. A successful Block/numeric comparison creates a stable outcome and target identity with defeated team and acting hero, but deliberately carries no reward amounts until milestone 7.4 derives them from verified Hero Level/lowest Tier. Reward accounting rejects that incomplete stage rather than inventing values. Removal attempt/result, unresolved-card resolution, Active-effect/marker consequences, Life, victory, and respawn remain separate later operations.

### Prevent removal and Block

`PreventRemovalEffect` intercepts the removal stage; it does not undo defeat or reward unless exact card text says otherwise.

`BlockResult` is a Defense-specific conditional result override that prevents an Attack according to its condition. It is not a large numeric Defense value and does not share `PreventRemovalEffect` semantics.

### Clear

`ClearAction` is a rule-provided optional replacement for an entire Attack action. Once selected, its controller may choose any subset of tokens adjacent to the acting hero, including none, and removes the chosen tokens through token removal capabilities. It suppresses the replaced Primary Attack text and is not an Attack that happens to target tokens.

Milestone 6.3 implements Clear through the same direct replacement seam as Fast Travel rather than the still-deferred generic `ReplaceAction` node. One immediate typed command carries a canonical unique token-ID subset; Core revalidates the current replaceable Attack offer, actor presence, adjacency, and entire selection before mutation, then emits Clear-specific start/removal/completion facts between resolver-owned action checkpoints and resolves the source card. Clear creates no Attack target, Defense, value, outcome, or pending continuation.

## Contract implementation status

See [CORE_MODEL.md](CORE_MODEL.md#contract-implementation-status) for what this section is and how to maintain it.

Every query contract below is specified but not yet implemented as a named type. Targeting, adjacency, range, and empty-space rules are currently enforced by concrete authorities and backends rather than by a reusable typed query layer. Milestone 6.8 owns the general modifier and query contracts; 6.4 and 6.4a own the displacement queries they require.

| Contract | State | Owning milestone |
| --- | --- | --- |
| `PresenceQuery` | Specified, not implemented | 6.8 |
| `AdjacentSpaceQuery` | Specified, not implemented | 6.4 |
| `AdjacentObjectQuery` | Specified, not implemented | 6.4 |
| `RangeQuery` | Specified, not implemented | 6.8 |
| `RadiusQuery` | Specified, not implemented | 6.8 |
| `DirectionQuery` | Specified, not implemented | 6.4 |
| `DistanceQuery` | Specified, not implemented | 6.4 |
| `FartherQuery` | Specified, not implemented | 6.4 |
| `FarthestQuery` | Specified, not implemented | 6.4 |
| `SameZoneQuery` | Specified, not implemented | 6.8 |
| `AdjacentZoneQuery` | Specified, not implemented | 6.8 |
| `EmptySpaceQuery` | Specified, not implemented | 6.5 |
| `EmptySpawnPointQuery` | Specified, not implemented | 6.5 |
