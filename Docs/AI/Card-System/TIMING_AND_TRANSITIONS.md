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
- `ResolveMinionBattle` is implemented at its end-of-round checkpoint. It compares only minions in the declared current starting Battle Zone, completes ties without effect, removes the exact difference or all remaining losing minions, and enforces one-at-a-time Heavy-last selection through round-owned serializable choice state. Removing the losing team's last minion records an end-of-round `RequestLanePush` that remains unconsumed through cleanup. End-of-round cleanup, card retrieval, mandatory leveling, upgrades, and Pity Coin ordering remain separate system checkpoints.
- Victory evaluation runs immediately at the source-defined Life/Wave/Throne/level condition and can stop later resolution when the game ends.

A card effect requests or triggers these transitions through typed operation facts/checkpoints; it does not duplicate their logic in a card handler. A generic `OccupancyChanged` event is insufficient because Move, Push, Swap, Remove, and Defeat have different systemic consequences.

The implemented repeated-round scaffold uses one serializable end-of-round stage cursor over these stable checkpoint identities. A handler can complete, pause on a typed pending choice, stop on a game result, or report `NotApplicable` from authoritative state. Test-only fixture capability declarations may make a future subsystem explicitly unavailable, but a subsystem declared available without its required handler is an authority error. Minion Battle and owner-validated eligible-card retrieval now populate the cursor without replacing its continuation or event-order contract. The Minion-Battle choice is owned by the round rather than its stage handler: stable identities, losing team/side, required count, intrinsic candidate types, current selection, and exact invocations serialize directly and are rederived/validated after restore. A recorded Lane-Push request prevents the incomplete transition from being silently skipped when its checkpoint arrives. The next round begins only after all applicable stages complete, at which point the prior round's four slots and terminal stage outcomes become immutable history and only round-local state resets.

## Contract implementation status

The names in this shard are specification vocabulary rather than declared code types, so the validator's suffix-matched contract check cannot reach them; this section marks their status itself. Remove a row when its capability is implemented; do not add a row to soften an accidental gap.

| Capability | Status | Owning milestone |
| --- | --- | --- |
| Active-effect lifecycle, checkpoints, and continuous sources | Implemented. Milestone 5.4 built the lifecycle, 6.1q corrected checkpoint scoping, 6.1ap made checkpoints resolver-emitted, 6.2a split definition semantics onto the catalog from runtime bindings on the instance and added the first concrete Active-effect producer, 6.3 added token-owned continuous sources whose lifetime is placement rather than Active-effect cancellation, and 6.8 gave both source families typed rules to supply: an `ActiveEffectDefinition` and a `TokenPropertyDefinition` each declare `RuleModification` content, and only an instance in its active phase or a currently placed token contributes. | Implemented |
| `Sequence`, `Optional`, `Conditional`, `Choose` control flow | Implemented at 6.2a on a stable flat node table, with `Mandatory`, `may`, `up to`, and `if able` kept as four distinct behaviors | Implemented |
| `PerformAction`, `ReplaceAction`, `SimultaneousBatch` control flow | Specified, not implemented as control nodes. Milestone 6.2a built only the four control nodes its acceptance check names; these three were never in its scope. `PerformAction` needs a resolution stack the dispatcher does not have. Milestone 6.3 implemented Clear through the existing Fast-Travel-style direct replacement seam without generalising it into `ReplaceAction`, which remains deferred to the first verified-card replacement. Milestone 6.4 has now discharged the one-batch commit its acceptance named **inside the Object Push capability** rather than as a general control node, on the same reasoning: an `up to X` multi-target Push is one node with one decision - the shared distance - which resolves every target against one pre-effect state and commits them together, so no shared choice crosses node boundaries and there is nothing for a control node to coordinate. `SimultaneousBatch` therefore remains deferred to the first verified card whose shared choice spans separate nodes, which is the only shape the capability-local batch cannot express. | Named triggers (`SimultaneousBatch`, `ReplaceAction`, `PerformAction`) |
| `RepeatAction` control flow | Specified, not implemented | 6.7 |
| `RequestLanePush` recording | Implemented at 6.1; consumption not implemented | 6.9 |
| Lane Push Wave/Battle-Zone stages, respawn, and transition (`ResolveLanePushTransition`, `MinionRespawnEffect` vocabulary) | Specified, not implemented | 6.10-6.11 |
| End-of-round token/marker cleanup and retrieval-time Active-effect cancellation | Retrieval and cancellation checkpoint implemented (3.8c); token/marker cleanup specified only | 6.12-6.13 |
