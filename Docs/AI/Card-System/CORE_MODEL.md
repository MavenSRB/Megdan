# Card-System Core Model

Canonical active shard. Return to [../CARD_SYSTEM_ARCHITECTURE.md](../CARD_SYSTEM_ARCHITECTURE.md).

## Purpose and status

This document is the implementation contract for card-related code. It exists so the small Phase 1 test hand can grow into the complete Wasp set, then five additional distinct heroes for the private 3v3 pitch, and eventually an authorized roster of roughly 40 heroes without putting card rules in Unity scenes, UI callbacks, or card-name conditionals.

`CARD_EFFECT_CAPABILITIES.md` is the mandatory effect-level companion. It defines the reusable contracts for Object Push, source-relative direct Move, Place, Spawn, tokens, markers, board-object changes, keyword queries, modifiers, timing, and other card-driven operations.

Milestones 1.5-1.13a establish pure command/event movement, immutable card definitions and owned runtime instances, typed offers/values, resumable Movement choices, and their visible synthetic presentation. Milestones 2.1-2.5 add the participant ownership graph plus typed Attack, target, Defense reaction, and comparison contracts. Milestones 3.1-3.8c add guarded lifecycle transitions, `HeroId`-owned reveal/assignments, Initiative and ties, Hold/Movement/Attack turn composition, repeated four-turn rounds, immutable completed history, and the resumable end-of-round cursor. Milestone 3.9 adds deterministic schema-versioned authoritative serialization/restoration over those boundaries and presentation reconstruction from restored state. Corrective milestone 3.10a replaces snapshot-backed Undo history with immutable initial scenario inputs plus typed accepted commands: Undo truncates the latest command, reconstructs fresh authority, replays the retained prefix through normal rules paths, and regenerates the milestone 3.11 event projection from accepted events without granting that projection rules authority. Snapshots remain an orthogonal persistence, diagnostics, and future resynchronization boundary rather than per-command Undo entries. A fit audit of all 819 local hero-action fronts found no need to abandon these definition/instance, offer/context, pending-choice, typed-capability, command/event, or snapshot boundaries; specialized future cards will add verified typed capabilities or isolated handlers at the documented extension seams. These slices do not implement resolve-without-action, verified Wasp text, concrete action-start restriction effects, Fast Travel, direct Move, Range or Defense modifiers, modal/repeat behavior, Attack consequences, concrete timing effects, deferred end-of-round subsystems, snapshot migrations, redo, or any broader effect handler.

The contract is based on the second-printing rulebook's game structure, Action Card anatomy, action execution, card states, Movement, Skill, Hold, Attack, Defense, leveling, items, active effects, and keywords on printed pages 7-19. Exact hero content still requires the source-verification milestones in `ROADMAP.md` and `RULES_LEDGER.md`.

This is an architecture specification, not evidence that any card rule is implemented. Implement only the subset named by the currently approved micro-milestone.

## Rule-driven design pressures

The card system is more than a collection of numeric values:

- A played card offers one Primary action and one or more Secondary actions, but exactly one action is chosen. Hold is generally available even without a printed icon.
- Fast Travel and Clear replace an entire action. Defense is a reaction from a card in hand and is an exception to normal turn action selection.
- Card text applies in some invocation contexts and not others. In particular, printed Primary action text, Secondary actions, replacement actions, and Defense responses cannot be flattened into one generic "use card" operation.
- Actions contain ordered mandatory and optional steps, target choices, modal choices, conditions, repeats, and early termination when a mandatory step cannot be completed.
- Resolution can pause while a player chooses a target, option, distance, card, or whether to use an optional step, then resume without changing the outcome.
- Card states overlap. A resolved card is still played; a rare discarded card may also be played; facedown is an information and rules property that can cancel an active effect.
- Attacks open a Defense response and have explicit before/after timing. Active effects can last for a turn, round, next turn, or another defined window and can be canceled by state changes or defeat.
- Ultimates and token properties can be continuous rules sources rather than ordinary Active effects, with different lifetime and cancellation rules.
- Hold and resolving a card without performing an action are different rules paths even though neither executes printed card text.
- Some systemic consequences happen during or immediately after an action. Defeating/removing the last Battle-Zone minion can Push the lane before a repeated action continues, while displaced minions return to the Battle Zone only after the action.
- Targets can be heroes, minions, units, tokens, spaces, cards, or other rules objects. Range, Radius, adjacency, straight lines, zones, allegiance, immunity, and presence checks are distinct concepts.
- Printed icon values, card-text modifiers, items, minion modifiers, and active effects contribute differently. Items modify matching printed icons, not arbitrary numbers mentioned in text.
- Upgrades replace card definitions while items and Ultimates occupy different rules roles.

These pressures require explicit action context, resumable resolution, orthogonal card state, typed targeting, and deterministic timing from the beginning.

## Non-negotiable invariants

1. Authoritative card rules are plain deterministic C# with no dependency on `GameObject`, `MonoBehaviour`, animation, scene objects, UI state, or wall-clock timing.
2. Static card content and mutable match state are different models. A runtime card instance never mutates its card definition.
3. Rules code never switches on a display name, image filename, or hero name to decide behavior.
4. Printed text is stored for display and source auditing but is never parsed at runtime. Structured definitions are the authority used by the resolver.
5. Every player decision enters through a validated command. Every accepted state change produces ordered rule events.
6. Any resolution that awaits a choice is represented by serializable authoritative data, not a delegate, iterator, Unity coroutine, closure, or UI callback.
7. Action origin and invocation context are explicit, so Primary, Secondary, replacement, reaction, repeated, and card-text-created actions cannot accidentally share the wrong rules.
8. Card state is not forced into one mutually exclusive enum when the rulebook permits multiple simultaneous facts.
9. Common behavior is composed from small typed rule components. A genuinely unique card may use a controlled pure-code handler, but it uses the same validation, choice, event, timing, and serialization boundaries.
10. New content extends the system locally. A normal new card should add definitions and scenario tests; a truly new mechanic should add one isolated typed primitive or handler and its tests, not branches throughout the turn engine, UI, or state model.
11. Card definitions and effect handlers have no generic randomness primitive. The only randomized game input is the separately owned initial Tie Breaker setup result.

## Authoritative conceptual model

Names below are conceptual contracts. Exact C# names may change, but the responsibilities and boundaries must remain.

| Model | Responsibility | Must not contain |
| --- | --- | --- |
| `CardDefinition` | Immutable, versioned rules content shared by every copy of a card | Match ownership, current state, scene references, mutable counters |
| `CardInstance` | Stable runtime identity, owner, definition reference, and current card-state facts | Parsed text, duplicated rules content, presentation state |
| `ActionDefinition` | Immutable structured content for one printed or general-rule action | Runtime actor/source, current availability, UI widgets |
| `ActionOffer` | One currently legal context-bound use derived from an action definition and authoritative state | Duplicated card content, UI widgets, direct state mutation |
| `EffectDefinition` | Structured ordered behavior for an action or active effect | Arbitrary executable strings or scene operations |
| `CapabilityOutcome` | Serializable typed facts from one effect node, available to later nodes and checkpoints | Presentation-only state, untyped dictionaries, hidden callbacks |
| `PendingResolution` | Serializable progress through one action, including prior choices and current timing position | Delegates, coroutines, hidden UI-only choices |
| `PendingChoice` | The exact legal decision currently required, its chooser, candidates/constraints, and continuation location | An already-assumed answer |
| `ActiveEffectInstance` | A live effect's source, controller, scope, trigger, duration, and cancellation conditions | Presentation-only duration or animation state |
| `ContinuousRuleSource` | A persistent rules-property provider such as an Ultimate or token property, with its own applicability lifetime | Accidental Active-effect/card-state cancellation semantics |
| `RuleEvent` | An ordered fact emitted after accepted rules changes for logs and presentation | The authority to decide another rules outcome |

### Card definitions

A definition needs stable identity and enough structured metadata to reproduce the printed card without reading its image:

- Stable definition ID and explicit content revision.
- Hero/content-set ID, display name, typed `CardColor`, tier, and Basic/non-Basic/Ultimate classification where applicable.
- Printed Initiative, item icon, and printed action/value icons.
- Ordered action definitions with stable IDs local to the definition.
- Structured conditions, target specifications, values, and effect definitions required by those action definitions.
- Exact transcribed rules text and reminder text for display and audit only.
- Source reference and verification status outside runtime gameplay logic.

`CardColor` is a typed rules-facing characteristic, not a visual style, generic slot, or Red/Blue team identity. Rules queries may combine it with owner/controller, location, lifecycle state, face/information availability, and other verified predicates to derive legal card candidates for choices, costs, conditions, counts, or effects such as a color-restricted discard. Same-color progression constrains upgrade candidates, but each valid next-Tier relationship remains explicit rather than being inferred from color alone. No authority may infer card color from artwork, tint, display name, hero identity, or UI grouping.

The runtime format may later be authored through JSON, ScriptableObjects, or another content tool, but authoring objects must compile into validated immutable rules definitions. A ScriptableObject must never become live authoritative match state.

### Card instances and state

Every physical card in a match has a stable instance ID, a definition ID/revision, an owner, and an explicit current controller when rules require that distinction. Runtime state should represent at least these orthogonal concerns rather than one catch-all state value:

- Physical/logical location, such as hand, turn area/dashboard, discard pile, deck/set-aside area, or item slot.
- Whether the card has been played.
- Resolution status when applicable: unresolved or resolved.
- Face orientation and whether rules-facing information is currently available.
- Any source links needed by an active effect or pending resolution.

Legal combinations and transitions are validated centrally. For example, resolved implies played, but played and discarded may coexist in the rare rulebook case. When a resolved or discarded card becomes facedown, its type, color, and actions are unavailable to relevant rules queries; applicable active effects are canceled. A newly played unresolved card's facedown orientation is handled by the separate hidden play/reveal flow rather than this resolved/discarded suppression rule. Swapping cards transfers the rules states specified by the rulebook rather than merely exchanging two container indexes.

Milestone 3.1 implements the narrow pure transition foundation: an initial in-hand card may become played/unresolved in the turn area Facedown, reveal Faceup, and become resolved after its action. Defense discard remains a Hand-to-Discard location transition that does not set played or resolved. Milestone 3.3 initially composed those transitions through a staged two-side authority. `MatchParticipants` retains distinct `PlayerId` controller/seat and `HeroId` game-entity identities while enforcing exactly one hero per player. Milestone 3.4 queries revealed unresolved cards for the next actor, milestones 3.5-3.7 perform Hold, Movement, and Attack/Defense through a turn authority, and milestone 3.8 retains resolved cards in indexed turn slots through the explicit end-of-round boundary. Milestone 3.8b replaces the staged side approximation with canonical `HeroId` commitment/assignment ownership: every configured hero's controlling player must commit exactly one hero-owned card from a nonempty valid hand, only an empty hand permits Pass, board presence is irrelevant, invalid in-hand lifecycle state rejects rather than authorizing Pass, and joint reveal waits for every hero response. Each immutable assignment carries canonical `HeroId` plus exactly one committed card ID or explicit empty-hand Pass outcome; controlling `PlayerId` and team are derived and validated through `MatchParticipants`, not duplicated as independently supplied assignment facts. The current fixture supplies two assignments; the official 3v3 configuration later supplies six without replacing the snapshot shape or allowing presentation to infer ownership. Card retrieval, new-round start, and resolve-without-action remain distinct unimplemented transitions.

State changes such as play, reveal, resolve, discard, retrieve, swap, set aside, equip as an item, and become facedown are explicit rules transitions. UI code cannot assign these properties directly.
