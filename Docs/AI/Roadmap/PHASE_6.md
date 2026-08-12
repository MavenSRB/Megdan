# Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md).

## Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Phase 6 uses the complete Phase 4 map and the synthetic turn/round controller. Lane behavior is never validated on the reduced test grid.

### [ ] 6.1 Resolve the end-of-round Minion Battle

Observable outcome: The authority compares Battle-Zone minions, resolves the losing team's choices with Heavy removed last, and records an end-of-round Lane-Push request if a team loses its last minion.

Acceptance checks:

- Equal counts have no effect.
- The losing team removes the exact difference or all remaining minions when it has fewer than required.
- Choice ownership and Heavy-last ordering are authoritative.
- The Lane-Push request is not consumed before token/marker cleanup.

### [ ] 6.1a Add player-facing Minion Battle removal-choice UI

Observable outcome: When milestone 6.1's authority pauses for a Minion Battle removal choice, the configured passive presentation lets the authorized chooser select which eligible minions are removed and resumes the same authoritative end-of-round continuation.

Acceptance checks:

- The immutable application projection identifies the authorized chooser and team, required removal count, currently selected stable minion identities, legal candidates, and the exact typed invocation that can complete or advance the pending choice.
- The configured UI presents a contextual Minion Battle prompt and makes every currently legal candidate distinguishable by team and intrinsic minion type without relying on color alone.
- Selecting, deselecting, or confirming forwards only the projected typed invocation through `IMatchSession`; presentation does not compare minion counts, infer Heavy-last ordering, remove occupancy, or advance the end-of-round cursor.
- Equal-count and other no-choice states expose no removal controls, while rejected or stale input preserves the authoritative pending choice and refreshes the visible selection from the returned projection.
- The choice remains usable after restore, replay, and Undo rebuilds because the UI reconstructs itself from authoritative pending state rather than retaining a parallel local continuation.
- Focused application/presenter tests and a configured-scene Play Mode check prove the prompt, legal-candidate updates, exact-count completion, no-choice path, and passive-view authority boundary.

Non-goals: No Minion Battle rule changes, optional decline of a mandatory removal, Lane-Push consumption, token/marker cleanup, production art, animation, or audio.

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

### [ ] 6.4 Implement reusable direct Move and Object Push effects

Observable outcome: Synthetic effects execute the general direct Move and Object Push contracts on the full authoritative map without depending on Wasp definitions or card-name branches.

Acceptance checks:

- Direct Move remains distinct from a Movement action, cannot become Fast Travel, and applies its exact, optional, or `up to` distance and source-relative destination policy through typed queries.
- Object Push moves each legal unit or token in a straight line directly away from its explicit pushing unit, moves as far as possible including zero when blocked, and remains distinct from Lane Push, Movement, and Place.
- An `up to X` multi-target Push records one chosen distance, validates all targets against the same pre-effect state, and commits one simultaneous batch.
- Ordinary-obstacle, Ignore Obstacles, Move Through, legal-endpoint, immunity, and target-versus-presence behavior use reusable query/modifier contracts rather than effect-local geometry.
- Moving or pushing a minion never requests a Lane Push. The effect records the displacement and exposes the after-action checkpoint needed by milestone 6.4a; no configured live action exposes an uncorrected displaced-minion endpoint.
- Focused accepted, rejected, partial, zero-distance, simultaneous, restore, replay, and Undo scenarios preserve deterministic typed outcomes and occupancy.

Non-goals: No Wasp definition, printed-text parsing, invented Pull keyword, card-name/hero-name branch, configured live action exposure before milestone 6.4a, production presentation, or specialized hero/minion Respawn transition.

### [ ] 6.4a Integrate after-action displaced-minion correction with core Push

Observable outcome: A completed synthetic direct Move or Object Push action invokes the general displaced-minion correction once after all of that action's effect nodes and simultaneous batches finish.

Acceptance checks:

- Correction applies only to intrinsic minion miniatures outside the Battle Zone after the whole action.
- A minion follows a shortest empty path; its team chooses among equally short valid paths.
- With no path or legal move, the minion is Placed into a nearest empty Battle-Zone space.
- The fallback implements the minimum general Place capability through typed policy/outcome data and emits Place rather than Movement facts.
- When cross-team order matters, the Tie-Breaker-side team resolves first without flipping the coin.
- Direct Move and single-/multi-target Object Push exercise the same correction authority through configured match-session commands, snapshots, restore, replay, Undo, projections, and ordered events.
- Later core displacement operations reuse this correction authority rather than creating operation-specific return logic.
- End-to-end ordering between separate Repeat instances remains with milestone 6.7, which implements Repeat as separate bounded action instances.

Non-goals: No new displacement operation, Wasp definition, card-name/hero-name branch, Lane Push request, minion Respawn, production presentation, or Repeat implementation.

### [ ] 6.4b Add player-facing displaced-minion correction choices

Observable outcome: When milestone 6.4a's authority pauses for a displaced-minion route or fallback-Place choice, the configured passive presentation reuses milestone 6.1a's general authority-owned minion-choice interface so the authorized team can choose among the exact legal candidates and resume the same authoritative after-action continuation.

Acceptance checks:

- Milestone 6.1a's shared minion-choice presentation contract is the baseline: chooser/team identity, prompt/context, stable subject identity, legal candidates, current selection, confirm/advance invocation, stale refresh, and continuation status use the same application/presenter/view interface rather than a second correction-only prompt framework.
- The immutable correction projection supplies that shared interface with the displaced stable minion identity, correction kind, exact equal-shortest route or equally-nearest fallback-Place candidates, current selection, and typed invocation needed to continue.
- The shared configured UI clearly distinguishes Minion Battle removal from correction and, within correction, a correcting Move from fallback Place. Its correction-specific board adapter makes every legal route endpoint or Place destination selectable without calculating paths, distance, Battle-Zone membership, occupancy, or Tie-Breaker ordering.
- Selecting or confirming uses the same shared minion-choice presenter boundary established by 6.1a and forwards only the projected typed invocation through `IMatchSession`; presentation never moves/removes occupancy, chooses for a team, advances to another minion, or completes the owning continuation.
- Deterministic no-choice corrections expose no unnecessary prompt, while rejected or stale input preserves the authoritative pending choice and refreshes selection/highlights from the returned projection.
- Consecutive cross-team corrections visibly follow authoritative Tie-Breaker-side ordering without presentation flipping or interpreting the coin.
- Restore, replay, Undo, and restart reconstruct the prompt and highlights from authoritative pending state rather than retaining a parallel local continuation.
- Focused application/presenter tests prove both 6.1a removal and 6.4b correction use the same interface without mode leakage; a configured-scene Play Mode check covers removal-to-correction prompt replacement plus route choice, fallback Place choice, no-choice auto-continuation, stale rejection, multi-minion progression, and passive-view authority boundaries.

Non-goals: No correction-rule change, redesign of 6.1a's removal authority, one universal rules-choice framework beyond this minion-choice family, pathfinding or nearest-space calculation in presentation, optional decline, new displacement operation, Wasp definition, production art, animation, or audio.

### [ ] 6.5 Complete reusable Place and Spawn effects

Observable outcome: Synthetic effects use general typed Place and Spawn operations for eligible board pieces and supplies before verified hero content is introduced.

Acceptance checks:

- Place declares its source container, eligible piece type, destination query, chooser, and no-destination policy and never emits Movement or Spawn facts.
- Spawn accepts only an eligible supply object, validates the invoking rule's destination constraints, and removes a token first when a unit legally Spawns on a token-covered Spawn point.
- Unit, minion, token, supply, occupancy, obstacle, and empty-space rules compose shared queries; presentation and art do not determine eligibility.
- Atomic rejection, pending choices, typed outcomes, snapshot state, replay, and Undo are covered with synthetic definitions on the full map.
- Milestone 6.4a's minimum minion-correction Place path is migrated to or proven conformant with the completed general Place capability without changing its verified outcome.

Non-goals: No Wasp definition, hero Respawn eligibility/timing, Lane-Push minion Respawn, card-name/hero-name branch, or production presentation.

### [ ] 6.6 Complete reusable board-object Swap, Replace, and Remove effects

Observable outcome: Synthetic effects atomically exchange, substitute, or remove authoritative board objects through distinct reusable operations before verified hero content is introduced.

Acceptance checks:

- Swap validates both objects and resulting occupancies against one pre-effect state and emits one semantic transaction; it is neither Movement nor Place.
- Replace declares the incoming source, outgoing destination, property-transfer policy, and observable triggers and is not silently decomposed into Remove plus Spawn.
- Remove records its destination and removal/prevention outcome without Defeat rewards and remains distinct from Clear and Defeat.
- Moving, pushing, or swapping the last Battle-Zone minion never qualifies for Lane Push; a successful standalone Remove records a qualifying cause while a prevented Remove does not. Milestone 6.9 owns live request emission.
- Swap and Replace reuse milestone 6.4a's after-action correction authority whenever their completed action leaves an intrinsic minion outside the Battle Zone.
- Synthetic accepted, rejected, prevented, snapshot, replay, and Undo scenarios prove atomic state and deterministic typed outcomes.

Non-goals: No Wasp definition, card-state swap or upgrade replacement, hero Defeat consequences, specialized Respawn, card-name/hero-name branch, or production presentation.

### [ ] 6.7 Implement reusable Skill actions and ordered effect control flow

Observable outcome: Synthetic definitions can offer a typed Skill and execute reusable ordered, optional, modal, and repeated effect structures without a Wasp-specific resolver.

Acceptance checks:

- Skill is a first-class action offer/invocation context with the same authority, lifecycle, command, event, pending-choice, and action-completion boundaries as other card actions; presentation never infers Skill legality from text.
- Ordered mandatory nodes stop the remaining action when an impossible step requires it, while `may`, `up to`, and `if able` policies record explicit choices/outcomes instead of treating every failure alike.
- Modal actions and player/target/option choices retain typed chooser ownership and serializable continuation state.
- Repeat creates a separate bounded action instance with the original definition and explicit repeated context, cannot recursively Repeat, and applies the verified replacement and Active-effect restrictions.
- Each repeated action reaches its own after-action displaced-minion correction boundary before the next Repeat instance begins.
- Accepted, declined, impossible, interrupted, restored, replayed, and Undo-truncated synthetic scenarios produce deterministic state and ordered events.

Non-goals: No Wasp definition or text interpretation, invented scripting language, card-name/hero-name branch, production presentation, or source-unverified special case.

### [ ] 6.8 Implement reusable modifiers, permissions, immunity, and effective characteristics

Observable outcome: Synthetic effects and continuous sources apply the general typed modifier, restriction, permission, immunity, and effective-characteristic contracts before verified hero content is introduced.

Acceptance checks:

- Typed value modifiers retain source and channel provenance and compose deterministically with printed, minion, item-ready, and Active/continuous contributions.
- Restrictions and permissions identify their affected actor, action/effect/query, scope, lifetime, and precedence; a narrow permission does not silently disable unrelated restrictions.
- Immunity rejects targeting and automatically affected target queries while remaining visible to presence queries where the rule requires it.
- `Counts As` changes effective rules characteristics without erasing intrinsic representation, identity, supply, correction, or exception behavior.
- Serializable Active/continuous sources, simultaneous ordering, snapshot compatibility, replay, Undo, and focused accepted/rejected interaction matrices are covered with synthetic definitions.

Non-goals: No Wasp definition, source-unverified modifier, card-name/hero-name branch, production presentation, or claim that future verified cards cannot require a versioned extension.

### [ ] 6.9 Emit an immediate Lane-Push request during a turn

Acceptance checks:

- A recorded Defeat or successful standalone Remove of the last relevant enemy Battle-Zone minion emits the request.
- Ordinary Movement, direct Move, Object Push, Place, Spawn, Swap, Replace, and prevented Remove never emit the request; conformance reuses the implemented core capabilities rather than deferred assumptions.
- A Defeat still emits the request when its later removal is prevented.
- The request reaches the immediate checkpoint before a later Initiative action can continue.
- This milestone does not expose a partial Lane transition.

### [ ] 6.10 Resolve Lane Push Wave and Battle-Zone stages

Builds the non-executable ordered prefix: flip exactly one Wave counter and stop on final-Wave victory; otherwise advance one verified Zone toward the losing team's Throne and stop on Throne capture.

Acceptance checks:

- The incomplete prefix remains internal and cannot consume a live request.
- Wave and Zone stages emit deterministic typed outcomes/events.
- An early game-ending result prevents later stages.

### [ ] 6.11 Complete Lane Push minion respawn and transition

Completes the transition by removing all minions from the previous Battle Zone and respawning the matching type at every verified Spawn point in the new Battle Zone.

Acceptance checks:

- Transition order is Wave/final-Wave check, Zone/Throne check, then old-zone removal and new-zone respawn.
- A token-covered Spawn point removes its token before respawn.
- A non-token obstacle makes the owning team Place that minion in a chosen nearest empty Battle-Zone space.
- When cross-team order matters, the Tie-Breaker-side team starts without flipping the coin.
- An immediate transition completes before later Initiative resolution continues.
- Repeat-boundary integration is deferred until the first verified Repeat implementation.

### [ ] 6.12 Remove tokens and return markers at end of round

Runs the general cleanup checkpoint: remove all board tokens and return all markers to supply except where exact verified text provides an override.

An end-of-round Lane-Push request remains pending until this cleanup finishes.

### [ ] 6.13 Retrieve cards and cancel applicable Active effects

Observable outcome: The milestone 3.8c retrieval subset is completed and integrated with the full Phase 6 state so each hero retrieves only its own eligible cards and all applicable effects end through authoritative state changes at the established checkpoint.

Acceptance checks:

- Retrieval never takes another hero's card.
- Every eligible card returns regardless of its prior eligible state, while ineligible/set-aside/item cards do not.
- Applicable Active effects, including relevant Next Turn effects, cancel through authoritative events.
- The populated retrieval/cancellation handler replaces the synthetic fixture's limited availability declaration without changing the end-of-round stage identity, continuation, or event-order contract.
- Focused tests cover resolved, discarded, facedown eligible, other-owner, and active-effect cases.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused Phase 6 scenarios.
