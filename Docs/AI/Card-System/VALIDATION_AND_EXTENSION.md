# Card Validation and Extension Contracts

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

Phase 2 then proves that the same foundation can open an Attack, pause for a Defense reaction from the correct hand, discard exactly one card or decline, calculate the unmodified result, and resume. Phase 3 proves generic lifecycle, Initiative, turns, rounds, snapshots, and internal tools with synthetic definitions. Phase 4 establishes the complete configured map/setup shape; milestone 4.4a interposes the application-service port, milestone 4.4b routes Movement and Attack/Defense through stable typed strategy identities and one generic registry dispatch seam, and corrective milestone 4.4g assigns production Movement and Attack/Defense algorithms plus pending state to distinct concrete backends while retaining cross-action invariants in the dispatcher. Milestones 4.4c-4.4d complete passive-view MVP and the architecture/runtime gate before Phase 5 expands action/effect behavior. Phases 5 through 7 extend that synthetic foundation through the complete general core match loop. The complete Wasp card inventory and Wasp-specific card implementation begin only in Phase 8 after the milestone 7.9 core-match gate; Wasp verification may add localized capability deltas but does not own or block the general rules foundation. After the Wasp integration gate, five additional user-approved heroes each enter through source inventory and capability-delta mapping before the six distinct heroes are integrated into the official-setup director-mode 3v3 pitch match. The later private-online track transports the same typed commands, ordered events, serializable pending choices, immutable projections, and versioned snapshots through a host-authoritative session; transport callbacks and client projections do not become card-resolution authority.

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
11. Update `CARD_EFFECT_CAPABILITIES.md` whenever the card adds or refines a reusable effect/query contract.

## Deferred decisions

The following are intentionally deferred until a verified content or implementation milestone needs them:

- Final authoring file format and editor tooling.
- The complete catalog of atomic effects and timing windows.
- A corpus-wide effect taxonomy for every hero.
- Networking transport, session operations, and hidden-information projections remain outside the card resolver itself; the private-online track must consume these contracts without duplicating or weakening them.
- Compatibility policy for public saved games or live-service content revisions.

These decisions may extend the architecture but must preserve its deterministic definition/instance, command/event, pending-choice, timing, and serialization boundaries.
