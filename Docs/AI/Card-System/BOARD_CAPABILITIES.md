# Guards Digital - Board Effect Capabilities

<!-- operative-claim:F-DOC-004 status:CurrentDemonstrated -->

Canonical effect-capability shard. Return to the [Card Effect Capabilities index](../CARD_EFFECT_CAPABILITIES.md).

## Spatial and object query capabilities

### Object categories and relationships

Queries support spaces, heroes, minions, units, tokens, markers, cards, zones, terrain, spawn points, and other explicitly modeled game pieces. Each object exposes an immutable intrinsic representation kind and a derived effective rules type.

Relationship filters include self, another, friendly, enemy, owner, controller, and source. `Friendly` excludes self. A hero/minion is both a unit and an obstacle for occupancy, so an obstacle filter must not make a legal unit untargetable.

### Target versus presence

`TargetQuery` evaluates target eligibility for both player-chosen and automatically affected objects and applies immunity in either case. A separate `ChooseTarget` node creates a pending choice among eligible candidates when the action requires a player decision. `PresenceQuery` checks whether an object exists or is adjacent/in range without targeting it; immune units still count for presence checks where the rules say so. Executors must declare which query they use.

Milestone 6.8k implements the disk-shaped `TargetQuery` slice - every board object within a rulebook distance, immunity filtered per activity, tokens exempt because p.19 leaves them outside the unit rules - and realizes `ChooseTarget` as the registered `synthetic-choose-target` capability rather than a new node kind: the selection pauses through the existing capability-choice seam and publishes the chosen object's space as a typed coordinate output, which a later node binds through the ordinary node-output binding. Milestone 8.8 adds the first arm of the relationship vocabulary, because Wasp's kinetic Skills print "enemy units": `TargetQuery.StandsInRelationTo` answers which side of a source a board object stands on, reusing `SourceTeamRelation` rather than minting a fourth team enum, reading a hero's side from the match and a minion's from its own team and leaving a token on nobody's. The rest (friendly, owner, controller, source) stays specified here until a verified card needs it.

Milestone 8.19b implements the roster-specific reusable layer without pretending it is every future target language. `choose-target@2` declares effective Hero/Minion/Unit kind, source relation, origin, distance, exclusions, and the exact activity/immunity override; `choose-target-set@1` records one canonical unique bounded set; and `choose-target-pair@1` records one canonical unordered pair of distinct eligible objects. `RosterPresenceQuery` shares effective-kind, relationship, distance, exclusion, and canonical-order calculations but deliberately reads no immunity. Pure feasibility and answer/restore re-derivation call the same candidate routines, and the exact recorded chooser owns every pause. The handlers are exercised through bounded synthetic catalogs until a later verified hero definition makes default-catalog registration reachable.

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

Milestone 2.3a implements only the hero-Attack subset: axial-coordinate rulebook distance and an inclusive Range Total comparison for enemy heroes, with Range 1 including adjacency and occupied/obstacle spaces creating no line-of-sight restriction. Broader object categories, Range modifiers, straight-line queries, and the wider relationship vocabulary remain specified but unimplemented.

Milestone 7.2b implements `RadiusQuery` against rulebook p.9, which states both the shape and its one exclusion: "the value on the Range/Radius icon determines the maximum Range/Radius of your action", "with a Range/Radius value of 3, your primary action can affect targets up to 3 spaces away", and "there are no 'line of sight' rules. You can target units through obstacles, as long as they are in range/radius." It takes the composed Radius Total rather than an integer and refuses a total of any other channel, so a disk cannot be derived from a Range, an Attack value, or a number an effect's text carried. Its three questions stay separate because the rulebook separates them: the spaces alone, the objects a stated activity may target - immunity filtered through `TargetQuery` - and the units present in the same disk, which count immune units. The targeting answer is the complete eligible set an effect that affects every unit in Radius needs, in a canonical order that is deterministic and is explicitly not a mutation or simultaneous-resolution order; each consuming effect owns its own timing and commit policy.

Milestone 8.19b adds the matrix's source-oriented shapes over those primitives. `behind-target-line@1` derives empty spaces on the actor-to-target file behind the target. `choose-charge-line@1` binds an empty exact-distance endpoint and the adjacent enemy targets available there; `choose-through-target-line@1` binds the crossed enemy and empty exact endpoint. Both are selectors only - the later direct-Move node owns relocation. `attacked-target-space@1` and `pre-consequence-adjacency@1` read the target space retained by the enclosing Attack, while presence/count and adjacent-terrain predicates read the live board. Every list and composite option has a stable coordinate/object ordering; none uses Movement-path reachability as a substitute for straight-line geometry.

Milestone 5.3 binds the existing configured standard-minion state directly into authoritative board occupancy. Minions retain stable identity, team, intrinsic type, map-qualified space, and coordinate; they are unit obstacles for construction, Movement projection, route revalidation, and relocation, with no presentation-derived or parallel rules representation.

Zone adjacency follows the board rule: two zones are adjacent when there is an adjacent pair of non-terrain spaces, one in each zone. It is not inferred from bounding boxes or art proximity and is verified against authoritative map data.

## Movement, forced displacement, and relocation

### Perform Movement action

`PerformMovementAction` uses the selected action offer's typed Movement value and checks action-start movement limits/restrictions once. The Phase 1 shell creates an immutable pending choice of currently reachable destinations, each bound to one disclosed deterministic adjacent route and cost. Selecting a destination revalidates that candidate, commits each route step through the adjacent-move authority, spends one allowance per step, and completes the `up to` action; Stop selects zero movement. Each step enters an adjacent empty legal space and respects obstacles.

Each segmented destination choice projects one deterministic shortest route per offered destination. After a partial segment, the same action preserves cumulative route facts and ordered step events, subtracts the segment cost, and offers another choice from the new position, so waypoints can construct longer cumulative routes without pre-enumeration. The action-start evaluator receives actor, source card, action, invocation context, and typed Movement value exactly once; prevention rejects atomically, while a distance cap becomes the captured allowance used by later segments and recovery. Configured persistent restriction sources and lifetimes feed this evaluator, including action-start Movement limits and actor-qualified restrictions.

Primary/Secondary context controls card-text application. Milestone 5.2 implements the Fast Travel replacement as a separate value-free typed action: a current replaceable printed Movement offer supplies the replaced-action identity, while the replacement invocation suppresses source-card text. It atomically relocates to an empty space in the same or a shared-border adjacent zone only when neither endpoint zone contains an enemy hero or minion, requires no empty-space path, and emits ordered replacement/relocation/completion facts. Ordinary Movement continues to emit action, step, and completion events while leaving presentation timing separate.

### Direct Move effect

`MoveEffect` represents movement written inside card text. It uses ordinary movement restrictions unless its text overrides them, but it is not a Movement action:

- It cannot become Fast Travel.
- Preventing a Movement action does not automatically prevent it.
- Exact distance is mandatory unless wording such as `may move` or `up to` makes it optional.
- Its source text determines whether the acting hero or another unit/token moves.

Card wording such as `move a unit towards you` remains `MoveEffect`; `towards` is not promoted into a standalone keyword. Its target/destination contract composes `CloserQuery` so every legal destination is closer to the source than the target's starting space under obstacle-inclusive geometric distance, plus the source's exact/up-to distance policy and any required chooser/tie rules. There is no separate Pull capability or invented Pull keyword.

Milestone 6.4 implements this as the registered `synthetic-direct-move` capability. It takes the mover, distance, and destination policy as typed inputs (a boolean until milestone 8.6 declared `DirectMoveDestinationPolicy` and retired the capability to revision 3), and derives its destinations from the same reachability the Movement action uses rather than a second traversal, which is p.11's "same rules and restrictions" made structural. The exact-versus-`up to` distinction is read from the node's own `EffectFailurePolicy` instead of a parameter of its own, because p.11 ties it to the wording that the four policies already carry. What that wording admits is stated once for every displacement in `AdmittedDistances` and consulted here rather than answered again: an `up to` node admits every destination at or below the bound **and the mover's own space** by default, since p.10 lists `up to` among the non-mandatory wordings and p.11 requires the exact number of spaces only where the distance is not optional. Milestone 8.25e5f1 adds an optional typed least-distance floor to the same compatible revision: absence remains zero for every earlier declaration, while Siren's Call declares one because its mandatory target starts non-adjacent and standing still cannot satisfy the separate requirement to finish adjacent. Its `if able` is an enclosing optional node and its move remains `UpTo`, so infeasibility and distance are not conflated. Milestone 6.5ad corrected the original reading, which had been the reachability walk's answer rather than the rulebook's - a Movement destination costs at least one point, so the walk never emits the origin. Choosing the mover's own space where the declared floor remains zero is an applied result that relocates nothing and publishes the unchanged space, the distinction p.18's zero-space push already draws, and it is admitted before the destination policy filters rather than through it, because declining the distance is a property of the wording while `Closer` is a property of the destination. `CloserQuery` is composed from that capability rather than existing unread. Milestone 6.8k widened the subject: the mover parameter is a typed coordinate naming whatever stands there - a hero, a minion, or a token - resolved through the displacement port the way Object Push resolves its targets, so p.11's "another unit or a token" is expressible. A definition fills that parameter from a prior target-selection node's published space, never from a role, because roles resolve from the execution frame alone and no role can supply a coordinate; the definition validator refuses one that tries at content load.

Milestone 8.19b gives direct Move its own `RuleActivity.DirectMove` immunity identity. A node may carry a trusted Heavy-immunity override for exactly that activity; selection and final commit both ask it, and it bypasses only standing Heavy immunity. Source-declared immunity for the enclosing action or direct Move itself remains decisive.

### Ignore obstacles and Move Through

The deferred `move-adjacent-to-enemy-hero-for-push@1` (8.41b) shares the ordinary direct-Move executor and narrows its destinations to spaces beside an eligible opposing hero for a following Push. It moves only the actor. Reachability, distance policy, mover immunity, stale-answer validation and canonical coordinate ordering remain shared; endpoint qualification asks the existing Push immunity query. An adjacent zero-space choice is applied under Garrus GA-Q1, whereas declining the enclosing optional clause skips both Move and Push. The following ordinary target choice names one qualifying hero explicitly, independently of the earlier Attack target. No new persisted field or production registration is required until dealing.

`IgnoreObstaclesModifier` changes transit legality for the specified effect/action. The unit may enter and leave an obstacle space during transit but cannot end on an obstacle. Traversal does not target objects being traversed, so their immunity does not itself block transit; token traversal effects still apply. `MoveThrough` uses the same transit policy but remains traceable to its source capability/wording.

### Push

`PushEffect` implements the official Push keyword:

- The pushing unit and target unit/token are explicit. A different anchor is legal only when verified text defines that override or a separately named capability.
- The target moves in a straight line directly away from the pushing unit.
- Requested distance is typed and may be fixed or chosen according to source text; where it is chosen, the choosable range is the same `AdmittedDistances` reading direct Move uses, so `up to X` offers zero through X on both.
- If full distance is impossible, the target moves as far as possible, including zero.
- Ignore-obstacle/move-through variants still require a legal empty endpoint.
- A source that pushes multiple targets `up to X` records one chosen distance and moves all affected targets simultaneously.
- Push is not a Movement action or Place effect and does not itself trigger a Lane Push merely by displacing the last minion.
- Minions outside the Battle Zone are corrected at the after-action checkpoint, not after each pushed step.

The executor emits `PushDeclared`, a simultaneous `ObjectsPushed` batch where applicable, and occupancy events. The action/system controller alone runs after-action minion correction after all action nodes finish - implemented at milestone 6.4a: the card-action dispatcher invokes the correction once when a Skill's effect nodes and batches have all finished, derives everything from board position through `BattleZoneQuery`, and holds the source card unresolved until the correction completes. A minion follows a shortest empty path back, its team choosing among equally short returns and among its own displaced minions (the 2026-08-16 correction-order amendment); with no path it is Placed into a nearest empty Battle-Zone space, and with no empty space either it stays where it ended up as a typed left-in-place outcome. Cross-team order is the Tie Breaker's, read but never flipped.

Milestone 6.4 implements this as the registered `synthetic-object-push` capability over a shared `PushPathQuery`. The direction comes from the straight-line query rather than effect-local geometry, and the walk asks the obstacle query p.18's two separate questions: transit legality, which the pushing effect's own ignore-obstacles flag or the target's Move-Through permission can answer, and endpoint legality, which nothing can, so a permitted target crosses an obstacle but still has to reach an empty space. A blocked target is a short push and a target blocked immediately is a zero-space push; both are applied results rather than failures, and a pusher and target sharing no file is the one refusal, kept distinct from a direction that exists and cannot be used. The `up to X` multi-target case is one node with one decision: the shared distance is chosen once, every target is resolved against one pre-effect state before anything commits, and the commits then run farthest-from-the-pusher first so a batch never refuses itself. That discharges the one-batch requirement inside the capability; the general `SimultaneousBatch` control node stays deferred, as the timing shard's status table records. Immunity filters the target set - p.18 makes it a targeting rule - while a presence question over the same spaces would still count the immune unit. A pushed token holds no immunity of its own because p.19 states tokens are not units, so the rules-object vocabulary has no token member and reports the named absence instead. Milestone 8.8 adds the three things a card can say that this declaration could not, and adds them because Wasp's two kinetic Skills print all three in one sentence: the fewest spaces the card itself names, read through `AdmittedDistances` beside the declared distance so `up to 2 enemy units ... 3 spaces` can make the count optional while the spaces stay exact and `3 or 4 spaces` can name two numbers; a maximum number of targets, whose presence is what makes which units are pushed a chooser's decision, since a declaration naming none is the plain batch over every adjacent object that every declaration before it meant; and a `SourceTeamRelation` narrowing the set to one side of the board, read through `TargetQuery.StandsInRelationTo` so the offer and the operation cannot part company over what `enemy` means. Both Push declarations moved to revision 3 together because both resolve through one routine, and only the distance reading moved to both - a named push is handed its object by a selection node that has already answered the other two. The batch therefore asks two questions where a card names more than one distance, which is p.18's own order (`you choose the distance and then push all those units`); `RESOLUTION_AND_EFFECTS.md` owns what lets one capability do that. Milestone 6.4c adds the second targeting question beside immunity: p.18's heading and its first sentence both admit a unit or a token, so a definition declares which of those its own text means, and the declaration is read in the one place both the adjacency-derived and the target-named forms produce their target sets. Whether a piece may be pushed at all and whether this card's text names it are asked separately, because a card narrowing to unit is stating what it says rather than a rule about what may be affected.

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

Milestone 6.6 implements this as the registered `synthetic-swap` capability over a fifth board port. Both objects arrive as the spaces they stand on, the identity one-obstacle-per-space already makes unambiguous for every kind at once, and both positions are read from one state before anything is asked or committed - resolving the second after relocating the first would resolve it to the space the first just vacated, which is how a Swap written as two moves silently becomes a move onto itself. The resulting occupancies need no legality question of their own, and that is a property rather than an omission: each object enters a space the other stands in, so both are playable and each is vacated by exactly the piece arriving in the other. That is also why this cannot reuse the displacement commit, which requires an empty destination. Immunity is asked about both objects, because p.18 makes it a targeting rule and a Swap affects each as much as the other; asking only about the one a definition names second would let an immune hero be dragged out of its space by naming it first. The typed outcome carries no route, distance, movement, or placement vocabulary, and no Lane-Push qualification at all.

Milestone 8.19b threads the exact `RuleActivity.Swap` Heavy-immunity override through both the pair selector and the two Swap capability declarations. Both objects are still revalidated immediately before the one atomic exchange, and a separately declared immunity remains in force.

### Replace board object

`ReplaceBoardObjectEffect` exchanges an existing object for a different object. Its definition declares where the replacement comes from, where the old object goes, whether properties transfer, and which triggers observe replacement. It is not an alias for Remove followed by Spawn unless verified rules text defines those exact observable stages.

Milestone 6.6 implements this as the registered `synthetic-replace-token` capability over one immutable `ReplacePolicy` that refuses construction without any of those four declarations, exactly as `PlacePolicy` does for placement. The observation policy is what makes the last of them enforceable rather than decorative: the shared operation implements the single observable replacement and refuses a policy promising a removal and a placement, instead of quietly satisfying a two-stage declaration with a one-stage commit. The commit substitutes the occupant in place, so the space is never empty between the two objects and nothing - another placement, a Spawn looking for an empty point, a displacement - can take it in between. p.19 requires "a different object", so a substitution between two instances of one definition is refused, which is why this milestone adds a second synthetic token definition. Property transfer is declared and set to none, which is observable rather than nominal: p.19 ties a token's properties to the instance, so replacing a token that supplies a continuous source ends that source rather than handing it to an object whose own definition never declared it. The implemented arm is token-for-token, because a miniature substitute would need the unit reserve and p.18's Respawn eligibility, which stays unimplemented.

## Removal, defeat, prevention, and battle consequences

### Remove

`RemoveEffect` takes a game piece off the board without defeat rewards. It records the destination (supply, pool, or other defined location) and runs applicable removal/prevention triggers. Removing the last enemy minion in the Battle Zone can trigger an immediate Lane Push; moving, pushing, or swapping it cannot.

Milestone 6.6 implements this as the registered `synthetic-remove` capability over a `RemovePolicy` declaring its eligible piece kind and its destination. It runs in three named stages, which is what keeps it distinct from the two operations it is most easily confused with: the declared piece is resolved and checked for eligibility, the removal is attempted and can be stopped there, and only a removal that actually happened asks p.8's question. p.13's Clear is a caller of that same removal rather than a mode of it, and p.13's Defeat is a different staged outcome that awards coins and opens reward eligibility before its own removal stage; nothing in the Remove outcome can be mistaken for a Defeat that skipped its rewards. No declared eligible kind admits a hero and the board port refuses one as well, because p.13 takes a hero off the board through Defeat and its consequences. The prevented arm is reached today through the immunity decision cross-cutting rule 6 already requires immediately before a mutation, so a target that was a legal candidate when a choice opened and is immune when the answer arrives is attempted and not removed. The general `PreventRemovalEffect` source family is a different producer of that same outcome and stays unimplemented: p.13's own example of it is card text, no verified text is available, and the declared modification vocabulary reaches heroes alone while `TokenPropertyDefinition` carries a recorded decision against widening its affected object ahead of such text.

p.8's mid-turn cause is asked in one query, from the removal stage alone, over the same effective Battle-Zone minion set the Minion Battle counts - p.8 pushes the lane "in the same way as it would during a Minion Battle", so counting a different set here would make one phrase mean two things in adjacent rules. A conformance rule holds that single-caller property, which is how "moving, pushing, or swapping the last Battle-Zone minion never qualifies" stays true for displacements written later rather than only for the three that exist today. What is recorded is a cause naming which of p.8's two sentences it answers, the emptied side, and its Battle Zone, never a request; milestone 6.9 owns emitting and resolving the push. Milestone 6.6 produced the removal cause alone, because p.13's Defeat then decided its own removal and asked p.8 nothing; milestone 6.6b routed that stage through the same decision and so gave the Defeat cause its producer. The caller declares which cause it would record rather than the query inferring it, since nothing about an emptied zone tells a Defeat from a removal.

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

Milestone 6.6b gives the minion removal stage its prevention consumer. Until then the Attack recorded the attempt and wrote the removed result down as a constant, so `DefeatRemovalResultKind.RemovalPrevented` had no producer while milestone 6.6's standalone Remove reported the same outcome against live state - two paths taking a minion off the board disagreeing about whether a removal can be stopped at all, which is more dangerous than the rule being absent, because a protected minion surviving a Skill's Remove still died to an Attack. Both paths now ask milestone 6.6's one `RemovalStageDecision`, and a conformance rule derived from the contract holds every producer of the staged Defeat outcome to reaching it, so a second producer written later joins the rule by existing. A prevented removal is a complete Defeat and not a failed one: p.13's own Bolster example prevents the removal and collects the coins in the same breath, so the outcome and the reward stay recorded and accepted, the attempt stays recorded, no removed occupancy is required, and the ordered facts state the prevented outcome distinctly rather than one fact going missing. The reward is awarded before the removal stage runs and a later attempt on the same outcome re-awards nothing, which is milestone 5.5's outcome identity doing the work it was built for. Board occupancy, the minion reserve, and the Attack's own completion are untouched by a prevented removal, and nothing about the decision is stored, so restore, replay, and retained-prefix Undo re-derive it. The prevented arm is not reachable through ordinary play and this milestone deliberately does not make it so: the only prevention the rules can currently express is p.18's immunity, and an Attack filters an immune minion out of its legal targets and revalidates that filter immediately before the removal runs, so no legal Attack target can be immune at its own removal stage. What the sharing guarantees is that the general prevent-removal family, when it arrives with verified card text, is honoured by both removal paths rather than by whichever one remembered to ask.

### Prevent removal and Block

`PreventRemovalEffect` intercepts the one shared removal stage used by p.19 Remove and p.13 Defeat; it does not undo Defeat or reward unless exact card text says otherwise. Brogan's verified Bolster definitions supply the configured minion-scoped prevention source and immutable payment-or-decline decision at both entry altitudes.

`BlockResult` is a Defense-specific conditional result override that prevents an Attack according to its condition. It is not a large numeric Defense value and does not share `PreventRemovalEffect` semantics.

### Clear

`ClearAction` is a rule-provided optional replacement for an entire Attack action. Once selected, its controller may choose any subset of tokens adjacent to the acting hero, including none, and removes the chosen tokens through token removal capabilities. It suppresses the replaced Primary Attack text and is not an Attack that happens to target tokens.

Milestone 6.3 implements Clear through the same direct replacement seam as Fast Travel rather than the still-deferred generic `ReplaceAction` node. One immediate typed command carries a canonical unique token-ID subset; Core revalidates the current replaceable Attack offer, actor presence, adjacency, and entire selection before mutation, then emits Clear-specific start/removal/completion facts between resolver-owned action checkpoints and resolves the source card. Clear creates no Attack target, Defense, value, outcome, or pending continuation.

## Contract implementation status

See [CORE_MODEL.md](CORE_MODEL.md#contract-implementation-status) for what this section is and how to maintain it.

The table names broad reusable query contracts. A row can remain broader than an implemented bounded slice: milestone 8.19b deliberately adds `RosterPresenceQuery` plus typed target/set/pair and line handlers for the verified roster matrix, without claiming a universal query language. Other targeting, adjacency, range, and empty-space rules may still be enforced by concrete authorities and backends when no named general type exists.

| Contract | State | Owning milestone |
| --- | --- | --- |
| `PresenceQuery` | Roster-specific effective-unit/relation/distance/count slice implemented as `RosterPresenceQuery` at 8.19b; broader contract remains specified | 8.19b bounded slice |
| `AdjacentSpaceQuery` | Implemented by shared board adjacency derivations; no universal type with this exact name is required | 6.4 onward |
| `AdjacentObjectQuery` | Implemented by typed board-object candidate derivations | 6.4 onward |
| `RangeQuery` | Implemented across offer-bound target queries; 8.19b adds live dynamic-Range contributions before target derivation, with no universal type carrying this contract name | 6.8 and 8.19b |
| `RadiusQuery` | Implemented at 7.2b | 7.2b |
| `DirectionQuery` | Implemented through straight-line and displacement derivations | 6.4 onward |
| `DistanceQuery` | Implemented through board distance and Range/Radius derivations | 6.4 onward |
| `FartherQuery` | Implemented by source-relative destination policy | 6.4 onward |
| `FarthestQuery` | Implemented by bounded roster target derivation | 8.19b onward |
| `SameZoneQuery` | Implemented against official map zones | 4.3 onward |
| `AdjacentZoneQuery` | Implemented against official map borders | 4.3 onward |
| `EmptySpaceQuery` | Implemented by displacement, Place, Spawn, and respawn derivations | 6.5 onward |
| `EmptySpawnPointQuery` | Implemented by Spawn and hero/minion respawn derivations | 6.5 onward |
| `StraightLineQuery` as a reusable typed query | Implemented at 8.6. p.19's sentence is now stated whole - `IsStraightLine` accepts an ordered file and the single space the page calls out by name, and `AreInStraightLine` answers true for a space and itself, which `TryGetDirection` deliberately does not: membership of a file and which of the six files to walk are different questions, and a line of one space names no file for p.18's Push. `FilterOutStraightLine` is the complement, and it drops the origin for exactly that reason. Consumed by the printed-Range selection at 8.6 for Telekinesis and Mass Telekinesis; both Boomerangs compose the same filter at 8.10. | Implemented |
| Equidistant (orbit) destination policy on Direct Move | Implemented at 8.6 as `DirectMoveDestinationPolicy`, which replaced milestone 6.4's `towardActor` boolean with three declared readings and retired direct Move to revision 3. `EquidistantFromActor` admits only spaces neither closer to nor farther from the acting hero than the mover's own, and `EquidistantQuery` gets there by asking `CloserQuery` twice - once each way round - so the obstacle-inclusive distance stays stated once. Both narrowings need the actor's position and report impossible without one; the unrestricted reading does not. | Implemented |
| Board-sourced `PlacePolicy` declaration | Implemented at 8.6 over the existing `PlaceOperation`, which already had p.18's board arm. What the card was missing is now declared: `PlaceEligiblePieceKind.UnitOrToken` for its own words rather than the page's `game piece`, and `PlaceDestinationAnchor` so `into a space adjacent to you` is measured from the acting hero rather than from the piece. The anchor is checked against the declaration rather than trusted, so a caller measuring from the wrong space is refused as `DestinationAnchorMismatch`; the supply arm refuses a placed-piece anchor outright, because a piece from the supply came off no space. The source filter is the selection's, not the Place's. | Implemented |
| Push stop-cause facts consumed by a rider | Implemented at 8.8. The batch publishes one pair of facts per hero it moved - whether an obstacle stopped it, and which hero - in the target set's own order, and a `Conditional` reads the boolean per hero. Two pairs, because the verified text pushes up to two; a card pushing more and reading the fact for all of them needs a third name declared, which is visible at content load rather than silently narrowed. The facts are heroes' because the typed value is a hero and a minion has no hand to answer a rider with. The W-04 board-edge reading is answered on `PushPathResult.StoppedByAnObstacle`, which admits both the blocked and the left-the-board arms while `ObstacleQuery` keeps its three-way classification for the Move-Through permission; `Rules/HERO_WASP.md` owns the reasoning. | Implemented |
