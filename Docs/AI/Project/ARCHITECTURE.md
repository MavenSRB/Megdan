# Core Architecture Constraints

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Core architecture constraints

### Map progression

- Phases 1 through 3 use an expanded temporary test grid only for foundational movement, obstacles, cards, two-hero combat, Initiative, turns, and rounds.
- Phase 4 replaces that grid in the main validation scene with the complete official multi-zone single-lane map.
- The full map model includes every official space, terrain classification, zone, zone boundary, zone adjacency, spawn point, Throne, starting Battle Zone, and ordered lane relationship.
- Map content is selected once per match by stable `MapId` plus content revision. `SpaceId`, `ZoneId`, and `SpawnPointId` are map-local; persisted, catalog, external, or cross-map references compose `MapId` with the applicable child ID. Internal coordinate-based rules and queries may remain local to the match's selected immutable map definition, while loaders, snapshots, and presentation must carry or derive that selection and must not assume one static map or make a bare coordinate globally unique.
- Fast Travel cannot begin until same-zone, adjacent-zone, and non-adjacent-zone queries are verified on the full map.
- Lane pushing cannot begin until the full ordered lane, movable Battle Zone, Thrones, and per-zone minion spawn points are verified.
- Do not use a reduced or invented map as evidence that Fast Travel or lane pushing is correct.

### Authoritative state

- Rules state is plain, deterministic game data, not Unity scene state.
- Logical hexes use integer axial `(q, r)` coordinates; generic adjacency is the six standard axial offsets exposed as named directions.
- The model uses collections and stable IDs for teams, players, heroes, cards, units, spaces, zones, and effects.
- Executable setup configuration declares its required immutable `MapId` plus content revision. Every configured external space/spawn reference composes that `MapId` with the map-local child ID, and setup authority resolves all placements only against the exact selected definition before exposing immutable team resources and unit state.
- The staged mirrored-Wasp 1v1 Quick Game setup creates reusable stable minion identity, team, type, spawn, occupied-space, and coordinate data. Later minion obstacle, Defense, battle, removal, respawn, and Lane behavior extends that one representation rather than creating parallel setup-only units.
- The model must support exactly three independently owned heroes per team for the pitch without hard-coding that team size into general rule APIs.
- Authoritative state must be serializable and schema-versioned.

### Commands and events

- The UI submits commands such as play card, choose action, select target, or confirm movement.
- A rules authority validates commands and produces ordered state-change events.
- Multi-step actions pause on authoritative, data-shaped pending choices and resume through follow-up commands; UI callbacks never own the continuation.
- Director-mode authority runs locally in-process.
- Private online authority runs in one host/session process behind the same typed command, ordered-event, pending-choice, and versioned-snapshot contracts; clients never own rules resolution.
- Randomness must not be generalized beyond the initial Tie Breaker setup decision.

### Application service and presentation

- The configured match uses one pure C# `GuardsDigital.Application` match-session port as its live/replay orchestration boundary for director presentation and future transports. It owns the current authoritative match composition but does not absorb or duplicate domain rules. No unconfigured presentation drill exists alongside it: milestone 6.1c deleted the last one.
- The small composition root creates one bounded non-`MonoBehaviour` session runtime, wires the five peer feature presenters and passive views, configures passive board-view callbacks for hero and destination pointer intent, and gives GUI drawing to an authority-free HUD renderer. The pointer adapters have exactly one wiring path each, through the passive board view; they hold no session-side type and offer no second, non-passive way to be configured. There is no legacy controller for the composition root to reject.
- The application service accepts typed commands and explicit choices by stable rules identity, delegates to the existing authorities, and publishes accepted/rejected results, ordered events, and one immutable versioned match projection. Live play, deterministic replay, Undo, and restart use the same command path.
- Unity presentation follows Model-View-Presenter inside a ports-and-adapters boundary. Feature presenters translate view intent and immutable session projections; passive views render immutable view models and never own legality, pending continuations, snapshots, or authoritative state.
- One small scene composition root performs Unity object discovery and dependency wiring. Feature components do not use a singleton, service locator, global event bus, or direct cross-presenter calls.
- Board interaction, director hands/commitments, round/action state, Undo/restart controls, and diagnostic log projection remain separate presenter/view responsibilities. No replacement component may combine all session ownership, history, input, projection, and rendering responsibilities.
- Unity-native resources created by presentation have one explicit owner, are reused while that owner lives, and are destroyed or disposed with it. Saved asset references are never destroyed as if generated. Repeated configured-map rebuilds reuse stable tiles, markers, minion views, materials, and fallback meshes rather than creating monotonic native-object growth.
- The destination-highlight Material uses an explicit generated-versus-assigned ownership contract. One generated instance is reused for the owning configured scene or unconfigured drill lifetime and only that generated instance is destroyed at teardown; an injected or saved Material is never destroyed by presentation.
- Unchanged feature refresh/repaint paths reuse immutable view models, definitions, offers, lookup collections, labels, and GUI styles. Mechanical architecture tests protect pure-assembly Unity independence, session-port/presenter/view dependency direction, composition-root-only discovery, supported Unity discovery APIs, and the prohibition on rules branching or parsing by display name/printed text.
- The future private-online host/session boundary and owner-authorized projections build on this application port; network callbacks do not become rules authorities, and local director-only tools remain outside competitive client projections.

### Undo and diagnostics

- Director-mode Undo must retain one immutable initial-scenario description plus a typed log of accepted gameplay commands and explicit choices. It removes the latest command, reconstructs fresh authority from setup, and deterministically replays the retained prefix through the normal command paths.
- Undo does not store per-command snapshots and does not require inverse-action handlers. It rebuilds presentation and the read-only diagnostic event log from the reconstructed authority and replayed accepted events.
- Full-match undo history is acceptable for the small internal prototype.
- Undo, hidden-state inspection, and Tie Breaker override are internal tools and unavailable in competitive online matches.
- The implemented history contains typed accepted commands only. Invalid and presentation-only inputs create no entry; restart clears the command log and rebuilds the immutable initial scenario. Redo is intentionally absent.
- A player-facing save system is not part of the pitch.
- Internal authoritative snapshot capture/restore and the collapsible read-only event log remain implemented. Snapshots remain available for persistence, diagnostics, and later network resynchronization, while Undo/restart uses command replay. Planned tools still include animation-speed control.
- Diagnostic snapshot envelopes use bounded, non-throwing typed rejection and never partially restore malformed input. The durable schema-version-16 compatibility corpus pins representative setup, active-turn, Movement-choice, end-of-round, hero-or-minion selected Minion-Battle-choice, placed-token, attached-marker, and pending-correction meanings, the last including whether that correction still owes a repeated action instance, including each remaining minion's current board coordinate, mutable remaining-minion identities, Defeat-reward balances/outcome idempotency, combat timing/value discriminators, and the round-owned Minion-Battle/Lane-Push continuation vocabulary. Each superseded schema is explicitly rejected rather than reconstructed. Completed-match data remains explicitly unsupported until its authority exists, and every incompatible durable schema/content-identity change requires a same-milestone migrate-or-reject decision.

### Card system

- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` is the required card/resolver contract. The `Docs/AI/CARD_EFFECT_CAPABILITIES.md` index and its routed shards are the mandatory effect/keyword companion for all mutation, query, modifier, token, marker, timing, and system-transition work.
- The architecture must support the future hero catalog without claiming that unknown cards require zero extensions.
- Immutable, revisioned `CardDefinition`-equivalent content is separate from owned runtime `CardInstance`-equivalent state. Instances refer to definitions; definitions are never mutated into match state or upgrades.
- Runtime card lifecycle is represented through orthogonal facts such as location, played status, resolution status, and face orientation because the rulebook permits overlapping states.
- A card and an action are not the same selection. Immutable action definitions hold structured printed/general rules content; runtime action offers bind a definition to the current actor, source card, state, and invocation context. Offers explicitly distinguish printed Primary, printed Secondary, general-rule Secondary, replacement, reaction, repeated, and card-text-granted use, including whether card text applies.
- Printed icon values are queried through typed value/contribution services so later items, card text, minions, and active effects can modify totals without replacing early card models.
- Ordered action execution is deterministic and resumable. Mandatory/optional steps, target and modal choices, repeats, nested Defense, timing windows, and active effects use serializable rules data rather than delegates, coroutines, or UI-owned state.
- Targeting, presence checks, value calculation, card-state transitions, timing, and active-effect lifetime are reusable rule services rather than card-specific UI logic.
- Passive/continuous rules sources, including Ultimates and persistent token properties, remain distinct from ordinary cancelable Active effects.
- Common effects compose small typed definitions/handlers. A genuinely unique verified effect may register one isolated pure-code handler, but it must use the same validation, pending-choice, event, timing, state, and serialization boundaries.
- The shared action dispatcher owns cross-action resolution identity, authoritative offer revalidation, atomic rejection, serializable pending-choice publication, restoration coordination, and ordered-event publication without implementing a backend algorithm. Milestone 6.1h removed the explicit typed strategy registry the dispatcher had been decomposed behind - every interface had exactly one implementation and no consumer existed beyond the registry - so the dispatcher calls the distinct concrete Movement and Attack/Defense backends directly; they own their handler-specific pending state, validation, restoration, mutation, and typed results. The registered action/effect dispatch seam that later verified capabilities enter through is milestone 6.2a's design, not a structure the code has today.
- Each reusable effect/query has a stable versioned capability ID and typed contract covering source/actor/chooser/target roles, optionality, atomicity, state changes, checkpoints, events, continuation data, validation, and tests.
- Every effect node has its own stable node ID and typed input/output bindings. Serializable outcomes record actual targets, path/distance, stopping reason, defeat/removal/prevention, resource changes, and other facts so later text can branch on real results without callbacks or a universal expression language.
- Object Push, direct Move, Place, Spawn, Respawn, Swap, Replace, Remove, and Defeat remain distinct public capabilities even when they share spatial helpers. Tokens and markers have distinct instance, supply, occupancy/attachment, and lifetime rules.
- Card text such as `move a unit towards you` uses direct Move plus source-relative `Closer` destination constraints, not a separate Pull capability.
- Rules objects preserve both intrinsic representation kind and derived effective type. Each capability declares which it queries, including the rulebook exception that after-action Battle-Zone correction applies to minion miniatures rather than objects that merely count as minions.
- System consequences use typed operation facts/checkpoints: only Defeat/Remove can immediately advance the Lane, while Move/Object Push/Swap cannot; displaced-minion correction runs after the whole action.
- Never branch rules on card display name, hero display name, image filename, or printed text. Do not build a runtime natural-language parser or speculative universal scripting language.
- Phase 1 proves this foundation with placeholder definitions/instances, explicit Movement offers and typed values, command/events, a resumable choice shell, and visible card-driven movement.
- Phase 2 proves extension by opening a basic Attack, pausing for the target hero's Defense reaction, discarding one owned card or declining, and resuming the unmodified comparison through the same authority.
- Phase 3 proves generic card lifecycle, director hands, play/reveal, Initiative and tied Initiative, Hold, Movement and Attack inside turns, the four-turn round skeleton, an extensible repeated-round transition with limited synthetic retrieval, snapshots, Undo/restart, and event logging with synthetic definitions.
- Phases 4 through 7 prove the official map, core full-map actions and combat, minions, tokens, Lane Push, exact end-of-round order, defeat/respawn, progression, victory, and a deterministic complete match with synthetic definitions.
- The complete Wasp card inventory and Wasp-specific card implementation wait until Phase 8, after milestone 7.9 proves the synthetic core match. Milestone 8.2 may re-baseline Wasp-specific work and add verified capability deltas, but it does not gate or redesign the already-proven general core loop.
- Complete Wasp verification remains an intermediate integration gate. Only afterward are five additional heroes selected, inventoried, capability-mapped, implemented, and integrated into the official-setup 3v3 pitch match.
- Add reusable primitives only when a verified rule or card requires them. Later discoveries may require a localized capability, handler, or explicit schema migration, but must not silently rewrite ownership, action context, UI authority, or continuation semantics.
