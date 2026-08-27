# Card Validation and Extension Contracts

<!-- operative-claim:F-DOC-009 status:CurrentDemonstrated -->

Canonical active shard. Return to [../CARD_SYSTEM_ARCHITECTURE.md](../CARD_SYSTEM_ARCHITECTURE.md).

## Required test layers

Card work is not complete with a UI demonstration alone. Add focused tests at the layer being changed:

1. Definition-validation tests for valid and malformed content.
2. Card-state transition tests, including legal overlapping states and cancellation hooks.
3. Resolver contract tests for sequence, optionality, mandatory failure, modal choice, repeat, replacement, nested reaction/trigger frames, simultaneous batches, and staged defeat/removal as each concept is introduced.
4. Pending-choice tests proving invalid choices do not mutate state and valid choices resume deterministically.
5. Target-query tests for allegiance, geometry, immunity, presence-versus-targeting, and obstacles as introduced.
6. Value-contribution tests that distinguish printed values, text, items, minions, and active effects.
7. Timing, active-effect, and continuous-source tests for trigger order, required ordering choices, duration, applicability, and correct cancellation/non-cancellation.
8. Serialization round-trip tests for card state, pending resolution, and active effects once snapshots are introduced.
9. One or more source-linked scenario tests for every verified real card and every legal invocation that changes its behavior.
10. Interaction/regression tests whenever two effects expose an ordering or replacement edge case.

Determinism tests should run the same command/choice sequence from the same state and compare resulting state and ordered events.

## Incremental implementation boundary

This contract does not authorize building a complete effect engine in Phase 1.

The first card slice should implement only:

- Immutable placeholder card definitions and mutable card instances with stable IDs.
- Validated printed action definitions, derived runtime Movement offers, and typed Movement values.
- A data-shaped pending resolution/choice protocol.
- The minimum structured Movement action needed to select disclosed destination/route segments, accumulate their already-tested adjacent steps, and Stop or continue within one allowance.

The verified foundation now covers the official map, exact Movement/Fast Travel/Direct Move, Attack/Defense, board operations, effects, progression, and all 108 playable definitions for the six configured heroes. Each hero passes the permanent content-derived player-path gate without name-specific resolver branches. The later six-hero director/full-match and private-online tracks reuse these typed commands, ordered events, serializable pending choices, immutable projections, and versioned snapshots; transport callbacks and client projections never become card-resolution authority.

The foundation is succeeding when:

- Another ordinary Movement card can be added as validated data with no resolver or UI branching.
- Attack and Defense reuse the same action, choice, state, command, and event boundaries.
- A later standard effect adds or composes a small typed handler without changing snapshot shape, UI authority, or turn control.
- A unique effect remains isolated and testable rather than leaking card-specific conditions through the engine.

## Card-extension checklist

Before accepting a real card definition:

1. Verify its image, edition, transcription, and any official correction.
2. Assign stable card-definition, action-definition, and effect-node IDs.
3. Classify every invocation: Primary, Secondary, replacement, reaction, repeated, or text-granted.
4. Record ordered mandatory/optional steps, choices, conditions, targets, values, timing, duration, and cancellation.
5. Reuse existing typed components where their semantics match exactly.
6. Add one focused component or controlled handler when the card introduces a genuinely new concept.
7. Add definition validation and source-linked scenario tests.
8. Test state, timing, replacement, repeat, immunity, and ownership interactions that apply.
9. Confirm no card/hero/display-name branch, scene dependency, or runtime text parsing was introduced.
10. Update `RULES_LEDGER.md` with verification and implementation status.
11. Follow the `CARD_EFFECT_CAPABILITIES.md` index and update the owning capability section or shard whenever the card adds or refines a reusable effect/query contract.

## Deferred decisions

The following are intentionally deferred until a verified content or implementation milestone needs them:

- Final authoring file format and editor tooling.
- The complete catalog of atomic effects and timing windows.
- A corpus-wide effect taxonomy for every hero.
- Networking transport, session operations, and hidden-information projections remain outside the card resolver itself; the private-online track must consume these contracts without duplicating or weakening them.
- Compatibility policy for public saved games or live-service content revisions.

These decisions may extend the architecture but must preserve its deterministic definition/instance, command/event, pending-choice, timing, and serialization boundaries.
