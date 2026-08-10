# Guards Digital - Card System Architecture

Last updated: 2026-08-10

## Purpose and status

This document is the implementation contract for card-related code. It exists so the small Phase 1 test hand can grow into the complete Wasp set, then five additional distinct heroes for the private 3v3 pitch, and eventually an authorized roster of roughly 40 heroes without putting card rules in Unity scenes, UI callbacks, or card-name conditionals.

`CARD_EFFECT_CAPABILITIES.md` is the mandatory effect-level companion. It defines the reusable contracts for Object Push, source-relative direct Move, Place, Spawn, tokens, markers, board-object changes, keyword queries, modifiers, timing, and other card-driven operations.

Milestone 1.5 proved the first narrow command/event authority slice with a primitive adjacent-move command. Milestone 1.10 proved the first pure card model slice by separating immutable placeholder definitions from owned runtime card instances with orthogonal initial state facts. Milestone 1.11 proved placeholder action definitions, context-bound runtime offers, and typed printed Movement value queries. Milestone 1.12 proved the first pure resumable action-resolution shell, corrective milestone 1.12a hardened its authority and continuation, and corrective milestone 1.12b changed each player answer from one adjacent step to one immutable reachable destination/route/cost candidate. Milestone 1.13 proved the boundary end to end with a visible synthetic hand. Corrective milestone 1.13a now keeps the same resolution pending after a partial candidate: the authority accumulates committed route/events, recalculates candidates from the new occupancy and remaining allowance, and completes on Stop or exhausted allowance. Presentation retains card/action selection between segments and never owns resolution or occupancy. Milestone 2.1 adds the first collection-backed team/player/hero ownership graph: Red and Blue test heroes each own distinct synthetic Phase 1 Movement card instances, every owner reference resolves through stable participant IDs without assuming one hero per team, and the director presentation projects only the active hero's owned hand while retaining both occupancies in authority. Actor switching is rejected while a resolution is pending. Milestone 2.2 extends those definitions/offers with a typed printed Primary non-Ranged Attack and owner-bound Defense reactions whose definitions and offers declare Hand-to-Discard-Pile cost/state-transition facts. Milestone 2.3 reuses the same authority, stable command IDs, pending-state, typed value, and ordered event boundaries to project adjacent enemy hero targets, accept one target, record the unmodified printed Attack Total, and pause in a restorable response choice owned by that defender. Its presentation bridge exposes the derived Attack offer, routes the clicked adjacent enemy hero through the stable target command, and renders the resulting pending Defense prompt without duplicating target legality in UI code. Milestone 2.3a extends that shared query and continuation boundary with a typed printed Range contribution and explicit rulebook hex-distance query: Ranged targets are enemy heroes at distance no greater than the selected offer's current Range Total, occupied/obstacle spaces do not create line of sight, and both range classifications enter the same target command, ordered events, and defender-owned pending response. Milestone 2.4 answers that response through the same authority and pending Attack: it accepts exactly one revalidated defender-owned in-hand offer or decline, executes the selected card's declared Hand-to-Discard-Pile transition while preserving orthogonal state facts, records only its printed Defense contribution in a reaction invocation context, emits ordered response facts, and resumes at an explicit comparison checkpoint. Milestone 2.5 revalidates that checkpoint, reports successfully defended when Defense is equal to or above Attack and attack successful otherwise, emits typed outcome/completion facts, and closes the same Attack without applying deferred consequences. Milestone 3.1 adds validated typed Initiative and explicit synthetic/verified content authority to definitions, then proves pure guarded play-Facedown, reveal, post-action resolve, and Defense-discard transitions while retaining orthogonal location, played, resolution, and orientation facts. Milestone 3.3 composes the simultaneous two-side commitment and joint reveal boundary. Milestone 3.4 adds card-wide typed Initiative contributions, current highest-unresolved selection, team-owned same-team choices, and exact deferred cross-team Tie Breaker flips without executing an action. Milestone 3.5 adds the general-rule Hold definition/offer plus a pure turn authority that resolves each selected card after Hold, consumes deferred flips only afterward, re-queries current Initiative, and records the turn-only-effect cancellation checkpoint before turn end. A fit audit of all 819 local hero-action fronts found no need to abandon these definition/instance, offer/context, pending-choice, typed-capability, or snapshot boundaries; specialized future cards will add verified typed capabilities or isolated handlers at the documented extension seams. These slices do not integrate Movement or Attack into the turn authority, implement resolve-without-action, verified Wasp text, concrete action-start restriction effects, Fast Travel, direct Move, Range or Defense modifiers, modal/repeat behavior, Attack consequences, concrete timing effects, rounds, snapshot import/export, or any broader effect handler.

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
- Hero/content-set ID, display name, color/slot, tier, and Basic/non-Basic/Ultimate classification where applicable.
- Printed Initiative, item icon, and printed action/value icons.
- Ordered action definitions with stable IDs local to the definition.
- Structured conditions, target specifications, values, and effect definitions required by those action definitions.
- Exact transcribed rules text and reminder text for display and audit only.
- Source reference and verification status outside runtime gameplay logic.

The runtime format may later be authored through JSON, ScriptableObjects, or another content tool, but authoring objects must compile into validated immutable rules definitions. A ScriptableObject must never become live authoritative match state.

### Card instances and state

Every physical card in a match has a stable instance ID, a definition ID/revision, an owner, and an explicit current controller when rules require that distinction. Runtime state should represent at least these orthogonal concerns rather than one catch-all state value:

- Physical/logical location, such as hand, turn area/dashboard, discard pile, deck/set-aside area, or item slot.
- Whether the card has been played.
- Resolution status when applicable: unresolved or resolved.
- Face orientation and whether rules-facing information is currently available.
- Any source links needed by an active effect or pending resolution.

Legal combinations and transitions are validated centrally. For example, resolved implies played, but played and discarded may coexist in the rare rulebook case. When a resolved or discarded card becomes facedown, its type, color, and actions are unavailable to relevant rules queries; applicable active effects are canceled. A newly played unresolved card's facedown orientation is handled by the separate hidden play/reveal flow rather than this resolved/discarded suppression rule. Swapping cards transfers the rules states specified by the rulebook rather than merely exchanging two container indexes.

Milestone 3.1 implements the narrow pure transition foundation: an initial in-hand card may become played/unresolved in the turn area Facedown, reveal Faceup, and become resolved after its action. Defense discard remains a Hand-to-Discard location transition that does not set played or resolved. Milestone 3.3 composes those transitions through a two-side authority: each side may commit one eligible owned card or pass only when no eligible card exists; accepted pre-reveal events hide the card ID, and all committed cards reveal through authoritative Faceup transitions only after both sides respond. Milestone 3.4 queries those revealed unresolved cards for the next actor and acknowledges an independently completed lifecycle resolution. Milestone 3.5 performs the first normal action, Hold, through a turn authority and applies the existing post-action resolve transition only to the selected source card; resolve-without-action remains a distinct unimplemented transition.

State changes such as play, reveal, resolve, discard, retrieve, swap, set aside, equip as an item, and become facedown are explicit rules transitions. UI code cannot assign these properties directly.

## Action offers and invocation context

An action is selected from runtime offers produced by rules, not inferred from card art or one permanent `ActionType` field.

An immutable action definition identifies its action kind, printed/general-rule origin, base classifications and values, target/effect definitions, and default text-application/repeat/replacement policies. A runtime action offer binds that definition to current state and identifies:

- Action kind, such as Movement, Attack, Skill, Defense, or Hold.
- Origin: printed Primary, printed Secondary, general-rule Secondary, replacement, reaction, or an action granted by card text.
- Trigger/window and required card state.
- Acting hero/player and source card.
- Whether and which card text applies.
- The applicable printed values, target specification, and effect root from its definition.
- Costs and state transitions, such as discarding exactly one defending card.
- Repeat and replacement eligibility.

The available-offer query combines printed action definitions with general game rules. Hold does not need to be copied into every card definition. Milestone 3.5 supplies one immutable general-rule Hold definition and derives a source-card-bound offer only for the currently selected face-up unresolved played card; it is a General Rule Secondary action, suppresses source-card text, and cannot be repeated or replaced. Fast Travel and Clear are rule-provided replacement definitions whose runtime availability depends on the action being replaced and current state. Defense is a reaction offer derived from an eligible card in hand during the Defense window.

Rules-facing card and action classification must be queried in context. A Defense/Skill card counts as both types while in hand or discarded but only as Skill while played. Every action on a Basic card, including a Secondary action, counts as Basic. Do not cache one context-free type string and use it everywhere.

Defense/Skill text application is also context-sensitive: the same printed card can apply its appropriate text when played as Skill or discarded as Defense, while ordinary Secondary Defense ignores Primary-only text. This is represented by the chosen offer's policy, not an exception in UI code.

Replacement and repeat are separate concepts. A replacement replaces the entire eligible action and uses its own text-application policy. A repeated action is a new instance of the original action, cannot itself be repeated, may make different modal choices, and cannot be replaced with another action such as Fast Travel or Clear unless later verified overriding card text explicitly says otherwise.

## Deterministic, resumable resolution

The resolver is a state machine that advances until it either completes, fails at a mandatory step, or needs an authoritative choice.

At a high level it:

1. Validates the requested action offer against current state.
2. Creates a `PendingResolution` with stable source, actor, action, and effect-node identifiers.
3. Advances ordered effect steps and timing windows.
4. Emits a `PendingChoice` when player input is required and stops without guessing.
5. Accepts a follow-up choice command, revalidates it against current state, records it, and resumes.
6. Stops the remaining sequence when a mandatory step cannot be completed; optional steps may be declined or skipped according to their wording.
7. Finalizes action/card state and emits ordered rule events.

The resolver also runs explicit rules checkpoints at the correct boundaries. A checkpoint may produce immediate deterministic consequences that change later targets or repeats; it cannot be deferred until animation finishes. Movement restrictions whose rules are checked only at action start use an explicit action-start snapshot/context rather than being re-evaluated accidentally on every step.

Pending resolution data must contain enough information to reproduce and later serialize the exact continuation: source instance, definition revision, chosen offer, actor/chooser, current effect-node path, repeat depth, completed choices, typed capability outcomes/result bindings, timing stack, and pending choice. The early phases do not need snapshot import/export yet, but they must not choose a representation that prevents it at milestone 3.9.

Nested rules work, such as an Attack awaiting a Defense reaction, is represented by explicit resolution frames rather than a second disconnected resolver. The outer action resumes after the reaction closes, preserving exact Attack/Defense ordering and allowing the same hero to defend again later if another legal attack occurs.

Defeat is likewise staged rather than treated as one deletion call. Defeated status/outcome, reward eligibility/award, the removal attempt and its triggered prevention choices, and the final removed/not-removed result remain distinct facts/operations; their order is supplied and source-tested rather than implied by this list. A prevention effect can therefore leave a defeated minion on the board without erasing the attacker's defeat reward.

Presentation observes choices and events. It may animate, skip, or replay them, but it never owns the continuation.

For the Phase 1 segmented-destination Movement shell, each pending destination candidate owns the exact deterministic adjacent route and cost that selecting it will commit for that segment. The player therefore chooses disclosed destination/route data rather than the resolver calculating an undisclosed route after the command. The candidate set is re-derived before every mutation, every route step still passes through the adjacent-move authority and emits its own event, and a partial segment advances the same resolution to a fresh choice from the new occupancy. The player may Stop with unused allowance or continue through waypoints, which can construct a deliberately longer cumulative route without enumerating every complete route to the final destination. Exhausting the allowance completes automatically. Action-start-only restrictions belong to the single outer Movement action and must not be reevaluated merely because a new segment choice is requested.

## Effect composition and extension

The common resolver vocabulary should stay small and typed. Add only what verified rules or cards require.

Every leaf mutation, query, modifier, timing source, and system transition follows the capability contract and coverage catalog in `CARD_EFFECT_CAPABILITIES.md`. A card definition references stable capability IDs and typed parameters rather than relying on this document's examples as implicit behavior.

Control-flow components are expected to include, as required:

- Ordered sequence.
- Optional step.
- Conditional branch.
- Modal choice of one or more options.
- Repeat with an explicit maximum and repeat-depth rules.
- Target, card, value, direction, or destination choice.
- Stop-on-mandatory-failure behavior.

Atomic effects are domain operations such as move, perform a Movement action, attack, modify a value, discard, retrieve, place, push, remove, replace, spawn, respawn, swap, create/cancel an active effect, spend/gain a resource, or change card state. These are added only when a verified milestone needs them.

Names that are distinct in the rules remain distinct operations. Direct card-text `move` is not a Movement action; place, spawn, push, and swap are not movement; remove is not defeat; and resolving without action is not Hold. They may share low-level occupancy helpers, but they retain separate validation, triggers, rewards, replacements, and events.

Rules-property replacement such as `Counts As` is an effective-characteristic replacement modifier/overlay consumed by typed queries, not a permanent extra tag on the source definition. Limited supplies such as tokens are authoritative resources, so place/remove handlers validate and update the supply rather than creating presentation objects freely.

Rules objects expose both intrinsic representation kind and effective rules characteristics. Most type-based targeting, immunity, rewards, and other rules queries use effective characteristics after `Counts As`; rules that explicitly refer to a physical representation, such as the p.18 correction for minion miniatures outside the Battle Zone, use intrinsic kind and exclude objects that merely count as minions. Every capability declares which axis it queries rather than reading one global type field.

Effects that the rules resolve simultaneously use a batch contract. The resolver validates shared choices and all affected objects against the appropriate pre-effect state, then applies the batch as one authoritative transaction. For example, a multi-target Push uses one chosen distance and cannot become an order-dependent loop of single-target pushes.

Effect definitions use stable capability-ID/schema-version discriminators and validated typed parameters; each occurrence inside one definition also has its own effect-node ID. A registry maps each capability/version to a pure rules handler. Avoid a single giant `ResolveCard` method and avoid a speculative general-purpose scripting language.

If a verified card cannot be expressed cleanly with existing components, first decide whether it reveals a reusable rules concept. If yes, add a focused primitive. If it is genuinely unique, add a card-specific pure handler behind the registry. That handler still cannot bypass command validation, pending choices, timing, events, state transitions, or serialization. Its discriminator is stable and its behavior has direct scenario tests.

## Targeting and presence queries

Target selection is a reusable rules service separated from effect mutation. A target specification may constrain:

- Object category: hero, minion, unit, token, space, card, zone, or another defined rules object.
- Relationship: self, friendly, enemy, owned, or another explicit relation. "Friendly" does not silently include self.
- Geometry: adjacent, range, radius, same/adjacent zone, straight line, nearer/farther/nearest, or an exact set.
- Occupancy, terrain, spawn-point, and obstacle requirements.
- Immunity and action/effect type.
- Whether the rule targets an object or merely checks for its presence. Presence checks do not become targeting and must interact with immunity accordingly.
- Choice owner and number of choices.

Distance and obstruction policies are explicit. Do not reuse movement-path code as a substitute for rulebook distance, Range, Radius, straight-line, or zone queries when their rules differ. Do not invent line of sight: obstacles do not block Range/Radius unless a verified rule or card explicitly says otherwise.

Milestone 2.3a implements the first narrow `DistanceQuery`/`RangeQuery` slice for hero Attacks. It calculates axial hex distance directly from coordinates, includes occupied and obstacle spaces normally, and compares that distance inclusively against the typed Range Total re-derived from the selected runtime offer. This implementation does not yet provide general Range modifiers, Radius, straight-line, full-map, minion, immunity, or presence-query behavior.

## Values and modifiers

Printed icon values remain distinguishable from calculated totals and text-created values. A value calculation produces a deterministic contribution list, not just a final integer. This allows tests and the event log to explain, for example:

- Printed Attack or Defense value.
- Item bonuses that apply only to matching printed icons.
- Card-text modifiers.
- Minion Defense modifiers.
- Active-effect modifiers and conditional bonuses.

Non-numeric outcomes such as a successful Block are explicit result overrides, not magic Attack/Defense totals.

Typed value specifications must support fixed values and the verified variable/conditional forms without using magic sentinel numbers. Validation rejects impossible values or missing requirements at content-load time rather than during a match.

## Timing, active effects, and continuous rules

Timing is an explicit ordered rules service, not language-level event subscription order. Windows are introduced as verified requirements, including Before Attack, Defense response, After Attack, action end, turn end, round end, next turn, and next turn after playing cards. Before Attack occurs at its rulebook checkpoint even if the eventual attack cannot connect; it is not a callback emitted only after target success.

The turn controller re-evaluates the highest-Initiative unresolved card after each resolution rather than caching the whole turn order. Tie resolution and any simultaneous effect ordering use explicit authoritative choices/Tie Breaker state, never collection iteration order.

Milestone 3.4 implements that narrow Initiative boundary with immutable pending same-team candidate data and selected-actor/deferred-flip state. Cross-team ties first select the current Tie Breaker side; if multiple cards on that side share the highest total, that side chooses among them. The deferred flip is consumed only after the exact selected card is observed in its authoritative resolved state. Same-team-only ties use the same choice shape without a flip. Action selection/execution and turn completion remain outside this authority.

Milestone 3.5 composes that boundary without moving its responsibilities: accepted Hold records the general-rule action fact, transitions only the selected source card to resolved, then asks Initiative authority to acknowledge it and consume any deferred flip before selecting the next current unresolved actor. The final no-unresolved result produces an explicit turn-only-effect cancellation checkpoint immediately before turn end.

Milestone 3.6 composes the existing segmented Movement boundary in the same turn frame. A distinct played-turn-card offer query derives printed Primary/Secondary offers only from the selected face-up unresolved card; it does not broaden the legacy in-hand normal-play query. The turn authority delegates Movement start and every destination/Stop command to the card-action authority, observes its existing pending resolution/choice and semantic events, and does not mutate occupancy. Only a completed Movement transitions exactly that source card to resolved, after which Initiative consumes any deferred flip and re-queries the next actor. A partial waypoint retains the selected actor and blocks another normal action. No concrete effect collection or cancellation mutation exists yet, and Attack remains outside the turn controller until milestone 3.7.

An active effect instance records:

- Stable effect-instance and source-card IDs.
- Controller/affected objects and scope.
- Trigger window and deterministic ordering data.
- Duration and expiration boundary.
- Cancellation conditions, including source card state changes and hero defeat where applicable.
- Structured effect or registered handler reference.

Only one instance of a given active effect from one card may remain active where the rulebook requires it. Radius attached to an active effect is evaluated from its current source position. Simultaneous active effects use the official tie procedure rather than collection or registration order; the resolver records any required player/team choice as a pending choice and never flips the Tie Breaker for this active-effect ordering window.

Continuous rules sources are queried separately. An Ultimate applies while its hero is on the board, and token properties persist while the token remains, according to their own source rules. They do not inherit ordinary Active-effect cancellation merely because a card changes state or a hero is defeated. Each source declares its applicability and removal boundary explicitly.

## Commands, events, and transactions

Card-related input uses intent commands such as select/play card, choose action, choose target, choose option, choose defense card, decline, or confirm movement. Commands refer only to stable IDs and explicit choices.

Validation is side-effect free. An accepted command advances one authoritative transaction and emits ordered rule events. A rejected command changes nothing. If resolution pauses, the resulting pending choice is authoritative state and is visible through an appropriate rules projection.

Rule events should describe facts such as card played, action selected, choice requested, unit moved, attack opened, defense card discarded, value contribution applied, active effect created/canceled, card resolved, or action ended. They are not animation instructions.

## Definition validation and versioning

Card definitions are validated before a match can start. At minimum, validation covers:

- Unique, nonempty stable IDs and supported schema/content revisions.
- Valid hero, tier, color, classification, icon, and action references.
- Unique stable effect-node and action-definition IDs within a definition.
- Required values, target specifications, conditions, and handler registrations.
- Valid action-origin/text-application combinations.
- Valid static action definitions and derivation rules for context-bound runtime offers.
- Repeat bounds and structurally reachable continuations.
- No presentation or asset dependency for authoritative values.
- A defined positive typed Initiative value and explicit synthetic-fixture or verified-official content authority.
- Source-verification metadata for production content.

Definition schema version and match-snapshot schema version are separate. A card instance references a stable definition ID and content revision so a restored snapshot cannot silently resolve against different card rules. Any incompatible change requires an explicit migration or a clear load rejection; it must never silently reinterpret saved state.

Upgrading replaces the applicable runtime card with a new definition-backed instance/state transition; it does not mutate the old immutable definition. Set-aside item cards and the Ultimate remain traceable to their own definitions and distinct rules roles.

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

Phase 2 then proves that the same foundation can open an Attack, pause for a Defense reaction from the correct hand, discard exactly one card or decline, calculate the unmodified result, and resume. Phase 3 proves generic lifecycle, Initiative, turns, rounds, snapshots, and internal tools with synthetic definitions. Phases 4 through 7 extend that synthetic foundation through the official map and the complete general core match loop. The complete Wasp card inventory and Wasp-specific card implementation begin only in Phase 8 after the milestone 7.9 core-match gate; Wasp verification may add localized capability deltas but does not own or block the general rules foundation. After the Wasp integration gate, five additional user-approved heroes each enter through source inventory and capability-delta mapping before the six distinct heroes are integrated into the official-setup director-mode 3v3 pitch match.

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
- Networking transport and hidden-information projections.
- Compatibility policy for public saved games or live-service content revisions.

These decisions may extend the architecture but must preserve its deterministic definition/instance, command/event, pending-choice, timing, and serialization boundaries.
