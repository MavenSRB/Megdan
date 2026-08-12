# Guards Digital - Timing, Control Flow, and System Transitions

Canonical effect-capability shard. Return to the [Card Effect Capabilities index](../CARD_EFFECT_CAPABILITIES.md).

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

Milestone 5.4 implements named pure checkpoints and definition-owned trigger, duration, cross-round, uniqueness, and early-cancellation data. Instances retain phase, source card/action, creation order, activation anchors, and ending checkpoint. The four named durations remain distinct; cancellation/expiry are idempotent, and activation does not also trigger the effect in the same visit. Retrieval, source-card-state change, applicable defeat, and explicit cancellation are available boundaries invoked by their later owning systems.

Milestone 5.8 integrates Defense response and Attack-result comparison as durable timing checkpoints. In the turn authority, an accepted Hero target visits Action Start and Before Attack before Attack Total, opens Defense response before the defender choice, visits result comparison only after an accepted discard/decline, then visits After Attack before Attack completion and action end before source-card resolution cancellation. Rejected or stale commands visit none of these checkpoints. Snapshot schema v5 rejects v4 so older code cannot silently deserialize the new timing/value discriminators without executing their meaning.

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

The implemented early repeated-round scaffold uses one serializable end-of-round stage cursor over these stable checkpoint identities. A handler can complete, pause on a typed pending choice, stop on a game result, or report `NotApplicable` from authoritative state. Test-only fixture capability declarations may make a future subsystem explicitly unavailable, but a subsystem declared available without its required handler is an authority error. The scaffold initially populates only owner-validated eligible-card retrieval and its Active-effect cancellation boundary; later Phase 6-7 work fills the remaining handlers without replacing the cursor, continuation, event-order, or snapshot contract. The next round begins only after all applicable stages complete, at which point the prior round's four slots and terminal stage outcomes become immutable history and only round-local state resets.
