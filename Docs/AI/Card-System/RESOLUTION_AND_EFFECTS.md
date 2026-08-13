# Card Resolution and Effect Contracts

Canonical active shard. Return to [../CARD_SYSTEM_ARCHITECTURE.md](../CARD_SYSTEM_ARCHITECTURE.md).

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

Player-facing presentation may consolidate distinct legal turn-ending invocations behind the contextual **End Turn** label only when the immutable authoritative projection supplies the exact typed invocation to submit. In the current synthetic loop this means the Hold action offer during ordinary unresolved-card action selection and the Stop pending-choice command during an optional Movement continuation. Hold and Movement Stop remain separate rule operations, command/history/event identities, snapshot/replay semantics, and validation paths; this label does not introduce a generic `EndTurn` domain command. A mandatory pending choice exposes no turn-ending invocation and cannot be bypassed. Future functionality reuses the presentation affordance by projecting its own typed legal invocation rather than adding UI branches on action kind, card/hero name, printed text, or presentation-owned phase state.

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

Pending resolution data contains enough information to serialize and reproduce the exact currently implemented continuation. Milestone 3.9 serializes the current synthetic Movement and Attack/Defense frames and reuses their existing restore validators to re-derive legal pending choices against restored state. Future effect nodes extend the same contract with source instance, definition revision, chosen offer, actor/chooser, current effect-node path, repeat depth, completed choices, typed capability outcomes/result bindings, timing stack, and pending choice rather than inventing another snapshot shape.

Nested rules work, such as an Attack awaiting a Defense reaction, is represented by explicit resolution frames rather than a second disconnected resolver. The outer action resumes after the reaction closes, preserving exact Attack/Defense ordering and allowing the same hero to defend again later if another legal attack occurs.

Defeat is likewise staged rather than treated as one deletion call. Defeated status/outcome, reward eligibility/award, the removal attempt and its triggered prevention choices, and the final removed/not-removed result remain distinct facts/operations; their order is supplied and source-tested rather than implied by this list. A prevention effect can therefore leave a defeated minion on the board without erasing the attacker's defeat reward.

Milestone 5.5 implements the pure reward-accounting stage. A stable Defeat outcome names its target/team, acting hero, and already-verified Defeat/Assist amounts; one atomic accounting batch emits source-linked per-hero gain operations for the actor and other friendly participants. Outcome identity makes the award idempotent. Removal or removal-prevented is a separate typed fact linked to the same outcome/target and has no path that subtracts or duplicates the earned coins. Milestone 5.6 attaches that seam only to minion Attack: a legal enemy minion target skips Defense and hero-only Attack Total comparison, records Defeat, the verified 2-coin Melee/Ranged or 4-coin Heavy reward, removal attempt, and removed result as distinct ordered facts, then updates authoritative occupancy. Milestone 5.8 makes successful Hero Attack record a reward-deferred typed Defeat outcome without removal or systemic consequences; reward accounting rejects it until milestone 7.4 supplies verified Level/Tier amounts. Remaining-minion identities, balances, and awarded-outcome identity persist under current snapshot schema v5; v4 rejects after the combat timing/value discriminator extension.

Presentation observes choices and events. It may animate, skip, or replay them, but it never owns the continuation.

For the Phase 1 segmented-destination Movement shell, each pending destination candidate owns the exact deterministic adjacent route and cost that selecting it will commit for that segment. The player therefore chooses disclosed destination/route data rather than the resolver calculating an undisclosed route after the command. The candidate set is re-derived before every mutation, every route step still passes through the adjacent-move authority and emits its own event, and a partial segment advances the same resolution to a fresh choice from the new occupancy. The player may Stop with unused allowance or continue through waypoints, which can construct a deliberately longer cumulative route without enumerating every complete route to the final destination. Exhausting the allowance completes automatically. Action-start-only restrictions belong to the single outer Movement action and must not be reevaluated merely because a new segment choice is requested.

Milestone 5.1 integrates that contract through the configured session on `official-single-lane` revision 1. After offer and actor revalidation, the Movement backend supplies stable actor/source/action/context plus the typed Movement value to one action-start restriction evaluator. Prevention rejects without pending state, events, or occupancy mutation; otherwise any distance cap produces the effective allowance stored in `MovementSessionState`. Later waypoint choices, snapshot restoration, deterministic replay, and retained-prefix Undo consume that captured allowance and do not reopen the action-start checkpoint. Concrete persistent restriction sources and their lifetimes remain owned by later effect milestones.

Milestone 5.2 implements Fast Travel through a separate stable `fast-travel-v1` strategy and value-free general-rule replacement definition. The typed application command identifies the replaced printed Movement definition and chosen destination in one atomic transaction. Revalidation requires a current replaceable Movement offer, an empty playable destination in the origin or shared-border adjacent zone, and no enemy hero or minion in either endpoint zone. It does not calculate or disclose a path and does not invoke adjacent-step authority. Acceptance emits ordered replacement-start, relocation, and completion facts with both action identities and endpoint zones, resolves the source card, and suppresses its text; rejection emits nothing and leaves occupancy/card state unchanged. Milestone 5.3 binds the same configured minion records into authoritative board occupancy so destination emptiness, movement blocking, and Defense modifiers share one minion identity/type/space source.

The application projection derives each currently available Fast Travel replacement and its immutable legal-destination set from that same query only when no action or mandatory choice is pending. The configured round view displays an eligible replacement as an explicit Fast Travel mode button while normal actions and an unhighlighted board remain the default interaction state. Selecting that button publishes only the replacement's projected coordinates to the board view; `BoardInteractionPresenter` then forwards a selected coordinate as `PerformFastTravel` through `IMatchSession`. Presentation neither recalculates eligibility nor constructs alternate destinations; a projection change clears the presentation-only selection, and stale or forged commands are revalidated by the backend before mutation.

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

Milestone 5.8 implements `DefenseBlockResult` as that explicit comparison override while current synthetic definitions populate `None`. The completed Hero comparison retains Attack Total, optional Defense Total, Block result, and final outcome as one validated typed fact. Printed-icon, minion, item-card, card-text/action, and Active-effect-instance contributions have separate discriminators and source identities; only verified synthetic printed/minion producers are currently populated.

Typed value specifications must support fixed values and the verified variable/conditional forms without using magic sentinel numbers. Validation rejects impossible values or missing requirements at content-load time rather than during a match.

## Timing, active effects, and continuous rules

Timing is an explicit ordered rules service, not language-level event subscription order. Windows are introduced as verified requirements, including Before Attack, Defense response, Attack-result comparison, After Attack, action end, turn end, round end, next turn, and next turn after playing cards. Milestone 5.8 integrates the complete current Hero-combat sequence: accepted target validation, Action Start/Before Attack, Attack Total, Defense response, typed comparison, outcome/staged Defeat, After Attack, Attack completion, action end, then source-card resolution/cancellation. Rejected or stale input visits no checkpoint. Presentation cannot delay or reorder these boundaries.

The turn controller re-evaluates the highest-Initiative unresolved card after each resolution rather than caching the whole turn order. Tie resolution and any simultaneous effect ordering use explicit authoritative choices/Tie Breaker state, never collection iteration order.

Milestone 3.4 implements that narrow Initiative boundary with immutable pending same-team candidate data and selected-actor/deferred-flip state. Cross-team ties first select the current Tie Breaker side; if multiple cards on that side share the highest total, that side chooses among them. The deferred flip is consumed only after the exact selected card is observed in its authoritative resolved state. Same-team-only ties use the same choice shape without a flip. Action selection/execution and turn completion remain outside this authority.

Milestone 3.5 composes that boundary without moving its responsibilities: accepted Hold records the general-rule action fact, transitions only the selected source card to resolved, then asks Initiative authority to acknowledge it and consume any deferred flip before selecting the next current unresolved actor. The final no-unresolved result produces an explicit turn-only-effect cancellation checkpoint immediately before turn end.

Milestone 3.6 composes the existing segmented Movement boundary in the same turn frame. A distinct played-turn-card offer query derives printed Primary/Secondary offers only from the selected face-up unresolved card; it does not broaden the legacy in-hand normal-play query. The turn authority delegates Movement start and every destination/Stop command to the card-action authority, observes its existing pending resolution/choice and semantic events, and does not mutate occupancy. Only a completed Movement transitions exactly that source card to resolved, after which Initiative consumes any deferred flip and re-queries the next actor. A partial waypoint retains the selected actor and blocks another normal action. No concrete effect collection or cancellation mutation exists yet.

Corrective milestone 3.6a connects presentation without relocating authority: after joint reveal the controller retains the pure turn frame, focuses the selected Initiative actor/card, and queries that played card's Movement offers. Destination and Stop UI intents resume the turn-owned pending choice; presentation updates highlights and hero transforms only from the returned projection and authoritative occupancy. Completion focus comes from the newly re-queried selected actor, while invalid/stale input leaves the visible actor, card, occupancy, and choice unchanged. The controller does not choose Initiative, cache a full order, mutate occupancy/card state, or substitute its earlier standalone in-hand resolution path.

Milestone 3.7 composes the existing Phase 2 hero Attack/Defense resolver into that same selected turn frame. Played-turn Attack offers use a distinct start path but remain revalidated from the selected face-up unresolved source card at every target, Defense, and comparison continuation. The outer turn retains the attacker and attacking card while the defender-owned reaction frame accepts exactly one legal in-hand Defense discard or Decline; that reaction does not resolve or consume the defender's played turn card. After the same Attack reports its outcome and closes, only the attacker's source card resolves, then Initiative consumes any deferred flip and queries the next actor from current card state. Presentation forwards target and Defense commands through this authority, projects its existing semantic events, and never substitutes a disconnected standalone resolver. Attack consequences, defeat/removal, and concrete Before/After Attack effects remain outside this milestone.

Milestone 3.7b expands that same synthetic boundary to five distinct cards per current hero. Each test definition contains one printed Primary Attack, one printed Secondary Movement, and one in-hand Defense reaction; general-rule Hold remains derived separately. The complete test configuration owns all ten unique definitions and instances, and any cross-owner continuation queries the complete definition collection instead of relying on mirrored definition identities. Defense discard changes only the selected defending instance, so its later normal commitment and reaction offers disappear without resolving or consuming the defender's separately played turn card. This remains synthetic test content and introduces no official card text or new capability.

Milestone 3.8 adds the outer pure round boundary for exactly four indexed turns, delegates tied-Initiative and Hold/Movement/Attack commands through the current turn authority, and carries live Tie Breaker state between turns. Milestone 3.8b configures each fresh commitment authority from `MatchParticipants`, requires one mandatory `HeroId`-owned response per configured hero, reveals only after every response, and records a copied deterministic assignment collection in the indexed slot. Commitment and slot construction validate participant coverage, unique hero/card identities, ownership, and lifecycle state; round events expose the same collection without Red/Blue positional assumptions. Only the current turn authority can finish a turn. Milestone 3.8c implements the post-fourth-turn boundary as one authoritative resumable cursor ordered over Minion Battle, token/marker cleanup, requested Lane Push, card retrieval/Active-effect cancellation, leveling/upgrades, and Pity Coins. Milestone 6.1 populates Minion Battle on configured full-map sessions: one round-owned typed choice supports exact one-at-a-time select/deselect/confirm commands, Heavy-last legality, atomic rejection, ordered removals, and a deferred typed Lane-Push request. The synthetic fixture still declares unavailable future subsystems explicitly; `NotApplicable` derives from authoritative fixture capability facts, while a missing handler for a declared-available subsystem rejects instead of silently advancing. Owner-validated retrieval remains populated. After every stage terminates, the authority freezes the four slots and terminal stage outcomes in an immutable completed-round record, increments the round index, creates fresh round-local slots/cursor state, retains match-wide state including Tie Breaker, and begins the next commitment. Schema-v6 serialization now includes the complete Minion-Battle choice and Lane-Push request beside current/completed round records, cursor/outcomes, hero assignments, card state, board occupancy, and other pending authorities; presentation clears transient projections and rebuilds from restored authority.

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

## Application orchestration and presentation boundary

Rules authorities do not serve directly as Unity controllers or network endpoints. The configured match now uses one pure C# `GuardsDigital.Application` match-session port for live director input, deterministic command replay, and later host transports. It owns the current authority composition, accepts typed commands and explicit choices, and publishes accepted/rejected results, ordered events, and an immutable versioned match projection without duplicating card legality or resolution rules. Retained unconfigured card-action presentation drills are bounded compatibility fixtures; the configured scene no longer uses them as a UI or orchestration path, and they are not a second configured-match boundary.

Live play, replay, Undo, and restart must use the same command-dispatch path. The application service may coordinate command history, snapshot capture/restore, and diagnostic projections, but those remain separate collaborators with explicit ownership; they do not become card-effect handlers or presentation state. Rejection is atomic and never publishes a partially advanced projection.

Unity presentation uses feature Model-View-Presenter over that port:

- Presenters translate user intent into typed commands and translate immutable match projections into bounded immutable view models.
- Passive views render view models and forward callbacks. They never query low-level authorities, decide action legality, own pending choices, create snapshots, or continue card resolution.
- One composition root performs scene discovery and dependency wiring. Presenters do not call one another directly, and the architecture does not use a singleton, service locator, or global event bus.
- Board interaction, director hands/commitments, round/action flow, Undo/restart, and diagnostics remain separate presenter/view responsibilities.

Milestone 5.8b extends only that projection/presentation boundary for a pending Hero Defense response. The application projection uses the same pure Defense-value query and typed Attack comparison semantics as command resolution to publish complete ordered Attack and per-choice Defense facts, explicit Block/outcome meaning, readable typed-source labels, and the exact signed contributing-minion identities. The round presenter formats an opaque hover/focus breakdown while the passive board renders nonselectable signed source cues; neither layer re-queries modifiers, geometry, allegiance, minion type, Block, or outcome. The existing resolution/choice-qualified response command remains the sole mutation path.

Within card resolution, one dispatcher routes each supported action kind to a distinct concrete backend. Distinct Movement, Attack/Defense, and Fast-Travel backends own their respective pending state, validation, restoration, mutation, and typed results; the dispatcher owns none of their algorithms. It retains stable resolution/step identity, authoritative offer revalidation, one active resolution, atomic rejection, serializable pending-choice publication, restoration coordination, and ordered event publication. Movement, nested Attack/Defense, and Fast-Travel entry and continuation paths reach their backend without reflection discovery, display-name branches, a service locator, or an untyped universal payload.

Milestone 4.4b originally expressed that routing as a typed strategy registry with stable `movement-v1`, `attack-defense-v1`, and later `fast-travel-v1` identities, dictionary lookup, and generic typed dispatch; 4.4g completed the ownership split behind it, and 5.2 registered Fast Travel at the same seam. **Milestone 6.1h removed the registry.** Every interface it indexed had exactly one implementation and every call site already named a statically-known concrete handler, so the lookup, the cast, and the delegate invocation stood between the dispatcher and backends it already held as fields. The dispatcher now calls them directly. The strategy identities went with it, having no consumer beyond the registry.

The ownership boundary is unchanged and is still enforced mechanically rather than by convention: `Phase4ArchitectureConformanceTests` rejects an action-specific algorithm appearing in the dispatcher's source. 6.1h re-pointed that rule from the deleted backend-interface names to the algorithm entry points the backends actually call, and its limits are recorded with the rule. A later verified capability adds a concrete backend and one direct call, rather than a registration or new action-kind control flow across unrelated handlers.

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

Definition schema version and match-snapshot schema version are separate. A card instance references a stable definition ID and content revision so a restored snapshot cannot silently resolve against different card rules. The snapshot also carries the match's immutable selected `MapId` and map content revision; restore resolves that reference through supplied current map definitions and rejects a missing map or revision mismatch distinctly rather than serializing a supposedly global coordinate set. Round history serializes collection-backed `HeroId` assignments rather than fixed team positions or duplicated player/team facts, preserving stable hero ownership and committed-card-or-empty-hand-Pass facts for both the staged fixture and later six-hero configuration. Controlling `PlayerId` and team resolve through the snapshot's participant graph. Any incompatible change requires an explicit migration or a clear load rejection; it must never silently reinterpret saved state.

Milestone 5.4 advances the match snapshot to schema version 3 so serializable Active-effect definitions/instances and separate continuous sources survive restart, replay, and retained-prefix Undo without rebuilding lifecycle meaning from card text. Schema version 2 is rejected explicitly; no prototype-save migration is claimed.

Milestone 5.6 advances the snapshot to schema version 4 for mutable remaining-minion identity plus Defeat-reward balances and awarded-outcome idempotency. Schema version 3 is explicitly rejected rather than silently treating every configured minion as still present or resetting earned coins; no prototype-save migration is claimed.

Milestone 5.8 advanced the snapshot to schema version 5 for durable combat timing/value discriminators. Milestone 6.1 advances it to schema version 6 because the durable round vocabulary now includes stable Minion-Battle resolution/choice identity, typed candidates/selection/invocations, and a pending end-of-round Lane-Push request. Schema version 5 is explicitly rejected rather than silently reconstructing or omitting continuation meaning. The setup, active-turn, Movement-choice, end-of-round, and selected Minion-Battle-choice v6 fixtures round-trip byte-identically; no prototype-save migration is claimed.

Upgrading replaces the applicable runtime card with a new definition-backed instance/state transition; it does not mutate the old immutable definition. Set-aside item cards and the Ultimate remain traceable to their own definitions and distinct rules roles.

## Contract implementation status

See [CORE_MODEL.md](CORE_MODEL.md#contract-implementation-status) for what this section is and how to maintain it.

| Contract | State | Owning milestone |
| --- | --- | --- |
| `PendingResolution` | Specified, not implemented as a general frame | 6.2a |
| `DistanceQuery` | Specified, not implemented | 6.4 |
| `RangeQuery` | Specified, not implemented | 6.8 |
