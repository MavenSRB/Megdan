# Guards Digital - Card Effect Capabilities

## Purpose and status

This document is the effect-level companion to `CARD_SYSTEM_ARCHITECTURE.md`. It defines the reusable rules capabilities that card definitions may compose so future cards do not spread special-case logic through the resolver, board model, UI, or presentation.

<!-- operative-claim:F-DOC-001 status:CurrentDemonstrated -->

The baseline catalog is derived from the second-printing rulebook's action execution, card states, actions, progression, concepts, and keywords on printed pages 7-19. Verified card text may override a general rule or introduce a new capability. No hero-specific capability is authoritative until its card source and any applicable official correction are verified in `RULES_LEDGER.md`.

This catalog distinguishes implemented contracts from deliberately specified extension points. The eight dealt heroes now compose their complete 144-card playable inventory through the typed capability, continuation, query, state, timing, and transition contracts recorded below; `Rules/ROSTER_CAPABILITY_MATRIX.md` owns their exact mapping. A row marked specified or deferred remains outside that implemented set and names its live trigger.

The registered contract is identity-complete and domain-safe. Every integer used as an enum-like closed vocabulary keeps its raw runtime value while its binding carries vocabulary-type evidence and its capability parameter declares an immutable legal domain validated at content load. An independent six-owner oracle covers reachable action, Ultimate, triggered, capability-reference, and active-effect semantics. The finite relation inventory has one explicit unsupported arm: `RestrictedActorRelation.HeroesAlliedWithSource` is admitted only when verified content names friendly or allied actors barred from an activity.

## Capability shard routing

- [Timing, control flow, and system transitions](Card-System/TIMING_AND_TRANSITIONS.md) owns Active/continuous lifecycles, resumable composition nodes, and card-triggered subsystem transitions.
- [Board capabilities](Card-System/BOARD_CAPABILITIES.md) owns spatial/object queries, movement and relocation, removal/Defeat, and prevention.
- [State capabilities](Card-System/STATE_CAPABILITIES.md) owns tokens/markers, card-state and progression operations, characteristics and immunity, values, resources, and ownership changes.
- This index retains the shared registration/execution contract, identity matrix, definition validation, test/coverage index, and extension workflow.

## Capability categories

Not every rulebook keyword is an effect that mutates state. The architecture keeps these categories distinct:

| Category | Responsibility | Examples |
| --- | --- | --- |
| Mutation capability | Performs one typed authoritative state transition | Move, Push, Place, Spawn, Swap, Remove, Discard |
| Query capability | Calculates legal objects or rules facts without mutation | Adjacent, Range, Radius, Nearest, Empty, Zone, Target |
| Modifier/restriction capability | Changes a derived value, permission, or effective characteristic | Attack bonus, Block, Immune, Counts As, ignore obstacles |
| Control-flow capability | Composes ordered effect nodes and choices | Sequence, Optional, Conditional, Choose, Repeat |
| Timing capability | Opens or reacts at a named rules checkpoint | Before Attack, Defense response, After Attack, End of Turn |
| Persistent-source capability | Supplies rules while a source remains applicable | Active effect, Ultimate, token-owned property |
| System-transition capability | Resolves a game subsystem rather than one ordinary leaf effect | Lane Push, Minion Battle, card retrieval, level up |

An `Adjacent` or `Target` node must not masquerade as a mutation handler. A `Push` handler may use Direction, Straight Line, occupancy, and obstacle queries, but those queries remain independently testable.

## Contract required for every capability

Each registered capability has an immutable definition contract and a pure rules executor. The contract records:

- Stable capability ID and schema version. This identifies the shared handler contract; it is distinct from the unique effect-node ID for one occurrence inside a card definition.
- Category and authoritative rule/card sources.
- Typed parameter schema, including required and optional fields.
- Legal source, actor, controller, chooser, and target roles.
- Invocation contexts in which it may appear.
- Preconditions and effective-characteristic queries.
- Choice schema and choice owner, if input is required.
- A feasibility answer that reports which result the executor would return here without performing anything, derived from the same candidate derivation the executor uses. A control node asks it to refuse an option the actor cannot perform; it never mutates board, card, effect, component, supply, timing, or identity state, and mints nothing.
- Exact versus `up to`, `may`, and `if able` failure behavior.
- Atomicity: single target, ordered multi-target, or simultaneous batch.
- State read set and state changes.
- Timing windows and system checkpoints opened before, during, and after it.
- Follow-up triggers and cancellation rules.
- Ordered rule-event types and payloads.
- Pending-resolution data needed to pause and resume.
- Definition validation and focused contract/scenario tests.

The runtime execution frame binds the immutable capability definition to stable source/actor/target IDs, the selected action offer, prior choices, current state revision, and continuation location. Where a roster capability needs them, `EffectRosterState` supplies live references to the existing turn slots, current-round hero-Defeat record, and coin ledger; its named absence throws instead of answering as empty. It contains no Unity object, delegate, coroutine, hidden UI state, copied candidate set, or second snapshot authority.

## Effect node bindings and outcomes

Every structured effect node has a stable node ID, capability ID/version, typed input bindings, typed output bindings, and failure/batch policy. A handler may return only the exact output names that node declared, even when its registered capability schema permits more; `EffectCapabilityContext.Publishes` carries that per-node set, so an earlier node shape cannot write a paused outcome its own trusted definition refuses to read.

Inputs may bind to explicit roles and prior results, such as `Actor`, `SourceCard`, `ChosenTarget`, `ChosenSpace`, `ChosenDirection`, a literal typed value, or the output of an earlier stable node. A literal may also name another node of the same definition, which is how a target selection says what it selects for; that is the one input kind with no runtime source, so nothing resolves it from a role and nothing publishes it. Outputs may expose target IDs, chosen path, intended/actual distance, stopping reason, placed/removed instance IDs, defeat/removal outcome, resource delta, computed value, or another capability-specific fact.

This lets later text express conditions such as `if that unit moved`, `if the target was removed`, or `if you gained a coin` without closures or a general expression language. The definition validator type-checks every binding and rejects missing, circular, incompatible, or cross-action references.

A handler returns one explicit execution result:

- `Applied`, including a legal rules no-op such as Push moving zero spaces.
- `NeedsChoice`, with a serializable pending-choice definition.
- `ImpossibleMandatory`, which stops the remaining action after prior completed steps.
- `DeclinedOptional`, which records the choice and continues according to the control node.

The result includes ordered semantic events and typed checkpoint facts. Pending resolution stores both accepted bindings and result facts so snapshot/Undo/network authority can resume exactly.

If a later mandatory node returns `ImpossibleMandatory`, earlier `Applied` nodes and their bound outcomes remain committed; the action stops but does not roll them back.

## Cross-cutting execution rules

1. Distinct rule terms keep distinct public capability IDs even when they share low-level occupancy helpers.
2. Validation is side-effect free. A rejected or stale command does not partially apply an effect.
3. A mandatory impossible step stops the remaining ordered action after preserving earlier completed steps. Optional wording follows its exact source semantics, and since milestone 8.29e an optional control node whose branch can apply nothing is skipped rather than offered.
4. All player decisions become authoritative pending choices. The resolver never chooses an undisclosed target, distance, route, direction, mode, or token to remove on the player's behalf unless the rules make that choice automatic. A candidate may bind multiple disclosed facts as one choice; the Phase 1 Movement destination candidate explicitly includes the exact route and cost selected by its one destination command.
5. Capabilities declare whether they query intrinsic representation kind or effective rules characteristics. Type-based rules normally apply `Counts As`, immunity, and continuous modifiers; explicit miniature/representation rules use intrinsic kind.
6. A unit-affecting capability declares its exact `RuleActivity` and consults the shared immunity decision both when deriving candidates and immediately before mutation; automatically affected and multi-target units use the same contract.
7. Simultaneous effects validate against the correct pre-effect state and commit as one batch. They are not converted into order-dependent single-target loops.
8. Every capability emits semantic rule events, not animation commands. Presentation decides how `UnitPushed`, `TokenPlaced`, or `ObjectsSwapped` looks.
9. System checkpoints run at their rulebook boundary even if presentation is still animating prior events.
10. Adding a card never authorizes a display-name, hero-name, or image-name switch. The definition references capability IDs and typed parameters.
11. If existing parameters cannot express a verified effect exactly, add a new versioned capability or a controlled custom handler. Do not overload a similar term with different semantics.
12. A control node offers only the options the chooser can perform. An option is judged by walking its own branch in execution order and asking each capability what it would report; an option proven to apply nothing is projected, refused, and shown greyed rather than hidden, and one that cannot be judged - a required input a later step publishes, or a predicate that would have to be executed - is offered. When no option can be performed the node's own failure policy decides what that means: an optional node declines without pausing, and a mandatory one pauses with every option greyed and offers the chooser a stop, which produces the same node result and the same ordered facts an unasked stop would have. The stop is derived from the refusals and that policy at every pause rather than stored, so it never reaches a snapshot.

14. The same walk answers whether a whole effect could apply anything, asked of the frame a start would begin from rather than one it began, so an action whose effect can be proven empty is refused before it is offered. It is one question at two altitudes: a control node's children, and a definition's root. Because the walk short-circuits at the first thing that can apply, an effect whose earlier step works is offered without its later nodes being judged, which is exactly why rule 12's pause is still reached - the offer declines what can be proven empty up front, and the pause closes what only turns out to be empty later.
15. A target selection declares the node it selects for, as a typed literal binding naming another node of the same definition, and offers only the objects that node can act on. The consumer answers through the same contract rule 12 asks every capability, once per candidate with that candidate standing in for the output it binds, and nothing is performed to find out; a branch behind a selection none of whose candidates the consumer accepts is refused exactly as rule 12 refuses any other. Declaring a consumer is optional and rule 12's conservative reading governs everything that does not: a selection naming none, a consumer this build cannot load, and a consumer whose other required input a later step publishes are all left offered. The declaration is a definition-time address and never a produced fact - no role supplies one, no capability publishes one, and none reaches a payload - and the definition validator rejects one naming a node this definition does not declare, which is also how it rejects one naming a node in another action.
13. A capability that moves something a declared number of spaces reads how much of that number is optional from one shared statement of pages 10 and 11, never from its own arithmetic. `up to X` admits every distance from none at all through X, because p.10 lists `up to` among the non-mandatory wordings and p.11 requires the exact number of spaces only where the distance is not optional; every other wording admits X alone, leaving the step optional without making the distance optional. Travelling no distance is an applied result that relocates nothing, not a decline or a failure, and it is admitted by the wording rather than by any geometry, so a destination filter narrows what a mover may travel to and never removes the option of staying put. A conformance rule holds every capability declaring a distance to that one statement.

16. An Attack whose target policy says mandatory card text binds its target is available only when a side-effect-free walk of that typed root can complete a mandatory path to the target-binding capability. Mandatory blocked prerequisites stop that path; optional subtrees do not count as the binding; and a modal or conditional node succeeds when any legal branch binds. The turn start asks the same live question again, so a stale projection cannot begin a now-impossible partial continuation.

## Core mutation identity matrix

| Public capability | Identity that must be preserved |
| --- | --- |
| Perform Movement action | Hero action with allowance, action-start restrictions, route choices, and possible Fast Travel replacement |
| Direct Move | Card-text relocation under movement restrictions, but not a Movement action and not Fast Travel eligible |
| Object Push | Official straight-line forced displacement directly away from the pushing unit; not Lane Push |
| Place | Board-or-supply piece to a different empty space; not movement |
| Spawn | Supply-only piece enters the board |
| Respawn | Hero/minion specialized eligibility and timing around Spawn/Place behavior |
| Swap board objects | Atomic position exchange; neither movement nor placement |
| Replace board object | Atomic substitution of one object for a different object |
| Remove | Piece leaves board without defeat rewards |
| Defeat | Enemy outcome with rewards and a potentially preventable removal stage |
| Clear | Replacement for an entire Attack that removes adjacent tokens |
| Discard/Retrieve/Swap card state | Card lifecycle operations, not board-object operations |
| Give/Return marker | Attachment and supply operations, not token placement; the attachment's identity is minted per activation since 6.8h |
| Gain/Spend resource | Typed economy/Life/Wave/level state change |
| Create/Cancel Active effect | Serializable timed source with ordinary cancellation semantics |
| Add/Remove continuous rule source | Persistent permission, restriction, modifier, token, marker, or Ultimate source with its own lifetime |

Three different meanings of `replace` remain separate: replace an action, replace a board object, and replace/upgrade a card definition/instance.

## Capability definition validation

Definitions are rejected before a match starts when any of these checks fail:

- Capability ID/version is unknown or its pure handler is not registered.
- Typed input/output binding is missing, incompatible, circular, or references an unreachable node/result.
- Actor, source, chooser, target category, allegiance, or intrinsic/effective-type query is invalid.
- Source container, destination, occupancy, supply, or endpoint policy is incomplete for the operation.
- Exact/up-to/as-far/optional/mandatory behavior is ambiguous.
- Shared-distance or simultaneous-batch requirements are missing or conflict with ordered execution.
- Timing window, Active-effect duration/cancellation, or continuous-source lifetime is incomplete.
- A system checkpoint can be requested by an operation that is not allowed to trigger it.
- Definition tries to treat Movement, direct Move, Object Push, Place, Spawn, Swap, Replace, Remove, Defeat, or Lane Push as interchangeable.
- Authoritative values depend on presentation, art, Unity objects, runtime text parsing, or display-name branching.
- An immunity override omits its supported printed reason, names an activity the exact selection/operation cannot consume, or attempts to act as a general permission.

## Required capability tests

Each capability receives pure conformance tests when first implemented, plus source-linked scenarios for every card that uses it.

Minimum regression pairs include:

- Movement action versus direct Move, including Fast Travel eligibility and action-start restriction checks.
- Push full distance, partial distance, zero distance, ordinary/ignored obstacles, and one shared-distance simultaneous batch.
- Source-relative direct Move behavior, including `move toward` destination constraints, chooser/tie rules, exact/up-to distance, and ordinary movement restrictions.
- Place versus Spawn versus Hero/Minion Respawn, including occupied destinations, different-space requirement, supply origin, hero decline resolving without action, spawn-over-token, and absence of movement triggers.
- Atomic Swap and Replace behavior under occupancy conflicts.
- Remove versus Defeat rewards, removal prevention after defeat, and staged outcome events.
- Lane-Push request after last-minion Defeat/successful Remove, including Defeat with prevented removal, and no request after Move/Object Push/Swap or a prevented standalone Remove.
- Lane-Push transition order, early game-ending stops, token-covered Spawn points, non-token-obstacle nearest-space choice, Tie-Breaker-side ordering without a flip, immediate-before-Repeat timing, and end-of-round cleanup-before-Push timing.
- After-action minion-miniature correction, including intrinsic kind versus objects that merely count as minions, shortest-path team choice, no-path Place fallback, Tie-Breaker-side ordering without a flip, and ordering between Repeat instances.
- Token finite supply/reclamation choice, persistent properties, movement/displacement, Clear selecting any adjacent-token subset, spawn-over-token, end-round removal, and survival of the placing hero's defeat.
- Marker attachment, continuous modifier lifetime, defeat/end-round return, and separation from token occupancy.
- Target versus presence, self versus friendly, Range/Radius with no line of sight, and shortest-distance queries through obstacle spaces.
- Immunity for both chosen and automatically affected targets by originating action/effect type, presence checks, self-action exception, and verified explicit overrides.
- Canonical single/set/pair and behind/charge/through-line candidates, including pure feasibility, obstacle/occupancy boundaries, exact chooser ownership, stale answer/restore re-derivation, and no selector-owned movement.
- Retrieval and resource grant/transfer ownership, lifecycle, insufficient-balance, overflow, stale-choice, wrong-chooser, and all-or-nothing debit/credit cases.
- `Counts As` effective type versus intrinsic representation exceptions.
- Every handler result (`Applied`, legal no-op, `NeedsChoice`, `ImpossibleMandatory`, `DeclinedOptional`) and typed result binding used by a later node.
- The feasibility answer against each result the executor can reach, that it performs nothing, and that a refused option is projected, unselectable, and rejected atomically when a stale or forged selection names it.
- Pending choice/resolution serialization, stale-choice rejection, determinism, and ordered semantic events/checkpoint facts.

## General rulebook coverage index

| Terms | Architecture owner |
| --- | --- |
| Active Effects; This Turn/Round; Next Turn; End of Turn/Round | Timing and persistent-source capabilities |
| Adjacent; Range; Radius; Direction; Straight Line | Spatial query capabilities |
| Closer; Farther/Farthest; Nearest | Distance/order query capabilities |
| Battle Zone; Zone; Empty; Spawn Point; Terrain; Space | Map/space query capabilities |
| Friendly; Hero; Minion; Unit; Token; Target; Immune | Object relationship, target/presence, and effective-characteristic queries |
| Counts As | Effective-characteristic replacement modifier |
| Move; Move Through; Ignoring Obstacles | Movement/direct-move and permission capabilities |
| Place; Spawn; Respawn | Relocation and supply capabilities |
| Push | Official forced-displacement capability |
| Swap/Replace board objects | Atomic board-object exchange/substitution capabilities |
| Swap card states; replace/upgrade a card | Distinct card-state and immutable-definition transition capabilities |
| Remove; Defeat; Block | Staged outcome, prevention, and result capabilities |
| Tokens; Markers; Obstacles | Stable instances, supply, occupancy, attachment, and continuous-property capabilities |
| Discard; Retrieve; Resolved; Unresolved | Orthogonal card-state transition capabilities |
| Choose; If Able; Repeat | Resumable control-flow capabilities |

## Adding a new effect from a verified card

1. Transcribe and verify the exact card text, values, and corrections.
2. Split the text into ordered control nodes, queries, modifiers, mutations, timing, and system checkpoints.
3. Map each node to an existing capability only when its semantics match exactly.
4. Record all actor/source/chooser/target roles and optional/mandatory wording.
5. Record atomicity, shared choices, failure behavior, triggers, and cancellation.
6. Add a versioned reusable capability if the card reveals a general rule concept.
7. Use a controlled custom pure handler only when the behavior is genuinely unique.
8. Declare exact domains for enum-like integer parameters and move the owning content identity whenever any reachable semantic changes.
9. Add definition validation, capability contract tests, and source-linked card scenarios.
10. Add interaction tests for relevant immunity, Counts As, obstacles, tokens, timing, replacement, repeat, and state changes.
11. Update this catalog and `RULES_LEDGER.md` before marking the card implemented.

## Implementation boundary

The catalog is intentionally broader than the current code, but it is not authorization to build every handler early.

- Phases 1 and 2 implemented only the action/choice/value seams, the minimum segmented-destination Movement capability, and nested Attack/Defense reaction over the same capability contracts.
- Phases 3 through 7 implement only the verified general/core capabilities needed to prove the complete match loop with synthetic definitions. The interim strategy-handler registry established at 4.4b was removed at 6.1h once every interface had exactly one implementation; 6.2a implemented the registered effect-capability seam while preserving direct action-backend composition, and 6.3 added Clear as a direct backend plus token-owned continuous sources. A new handler must not enlarge unrelated action-kind control flow.
- The five heroes joining Wasp are inventoried, mapped, composed, registered, and dealt. Their shared handlers are reachable through the verified production catalogs, and One Man Army's typed Minion-Battle replacement remains the one intentionally isolated roster handler. No roster implementation rewrites card instances, UI authority, commands/events, or the one continuation protocol.

The foundation is considered successful when Object Push, source-relative direct Move, Place, tokens, or another future effect can be introduced by registering/mapping the exact capability and tests, rather than adding branches across unrelated systems.

Handler composition is explicit and typed. Concrete action backends are composed directly, while effect dispatch registers stable capability identity and declared input/output contracts because the capability reference arrives as definition data. The generic Core factory accepts content handlers without naming a verified hero, and `MatchContentCatalogs` supplies the verified declarations at the one build composition root in deterministic order. Clear is a statically known direct backend, while token property meaning stays trusted catalog content rather than presentation or payload data. Neither form may use reflection discovery, display-name branches, a service locator, an untyped dictionary payload, or a universal expression language. Handlers remain pure rules collaborators and never reference match-session presenters, Unity views, transport callbacks, or animation state.

## Contract implementation status

| Contract | Status | Owner |
| --- | --- | --- |
| Card text on printed Attack and Defense actions | Implemented at milestone 8.5, closing the seam the 7.9a review recorded as F-CARD-01. A printed Attack or Defense declares an `EffectRoot` and the `CardTextTiming` naming which of rulebook p.14's numbered moments applies it - step 1's "Before the attack", step 4's Defense action text, or step 7's "After the attack:". `ActionDefinition` refuses the statement in each way it could be false: a root with no moment, a moment with no root, a moment belonging to the other action kind, and one on an action whose source-card text p.13 suppresses. Every printed Attack also carries a `PrintedAttackTargetPolicy` - p.13's default of enemy units at adjacency or the printed Range, narrowable to heroes alone - so the reach is a rule about the card rather than an inference from its subtype label. `EffectCapabilityContext` carries the enclosing action's kind, so immunity is asked about the action a capability is running inside (F-BOARD-03), and `Conditional` publishes its predicate's typed facts and routes a non-Applied result rather than reading it as false (F-CARD-07). Card text inside an Attack could not pause at 8.5; the next row retired that rule. | 8.5 |
| Resumable card-text and pre-action continuations | **Implemented at milestone 8.19a**, where the last of rulebook p.14's three card-text moments stopped being unable to hold a question. Step 1's remainder is the rest of the page rather than a list of facts, so one generator produces everything after it against live state, called inline when nothing pauses and by the card-action authority when the text finishes. Four identities follow. `attack-target@2` is a printed Attack's declaration that its own text names its target, so the action starts with no target choice; `set-attack-target@1` names one, publishing a typed fact the resolver reads off the completed frame and re-derives against the board the text just changed. `before-action-trigger@1` is a continuous source's declaration that it runs before its controller performs a named action kind at a stated frequency - a declaration rather than a checkpoint, because the lifecycle's checkpoints are moments inside an action and this precedes the first of them. `around-primary-action@1` publishes which side of a primary action the rest of its text belongs to, declaring a two-node root entered once per side, with any other shape refused at content load. All four compose through the one action/effect frame: no second resolver, no UI-owned resolution, no new pending-choice protocol, and the persisted pre-action frame rides beside the card-action continuation rather than as one of its kinds. | 8.19a |
| Played-Attack-card and current-round hero-Defeat rules history | **Implemented as the typed foundation at milestone 8.19a1.** `PlayedAttackCardThisTurnQuery` reads the exact `RoundTurnSlot` assignment and the committed definition's `PrintedPrimary` action rather than asking which action the hero performed, so Hold, a Secondary action, Defeat before acting, and not-yet-acted all preserve the fact p.9-p.10 establish. `RoundHeroDefeatHistory` is genuinely new round-owned authority: the one shared p.13 hero-Defeat seam appends each completed outcome once, ordinary or effect-invoked and independently of whether removal completes, while defended and refused paths append nothing and the next-round boundary clears it. The snapshot stores each opaque `DefeatOutcomeId` beside the typed defeated `HeroId`, because diagnostic events are not rule state and cumulative coin/Life records cannot reconstruct round scope. Milestone 8.19b supplies the bounded consumers over these exact authorities. | 8.19a1 and 8.19b consumers |
| Roster derived-query, value, and state-operation vocabulary | **Implemented at milestone 8.19b.** Typed handlers cover `choose-target@2`, bounded canonical set/pair choice, behind/charge/through-line candidate binding, attacked-target space, the two 8.19a1 history consumers, effective presence/count, pre-consequence adjacency, adjacent terrain, owned lifecycle-safe retrieval, positive coin grants, and atomic transfers. `RosterValueContributionDefinition` feeds live Attack/Range totals from typed history/presence predicates; definitions can suppress all minion Defense contributions or forbid only `PrintedPrimary` Defense. Exact `DirectMove`/`Swap` Heavy overrides bypass only standing immunity while declared immunity wins. Feasibility is pure and choices rederive on answer/restore under the exact chooser. | 8.19b |
| Roster persistent rule sources and the isolated Minion-Battle replacement | **Implemented at milestone 8.19c.** Three modification families arrive together - an immunity naming several activities and the actor classes it protects against, a sixth kind that adds an effective type instead of replacing one with `EffectiveUnitKind.Terrain` as its first member, and a value modifier counted per match from a named derived query or qualified to the action class it names - each with its own directed asker and owning query, all maintained in [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md). `after-action-trigger@2` reaches every completed basic action while 8.12's basic-Skill condition keeps its narrower reading. `one-man-army-minion-battle@1` is the one isolated handler: a declaration names the rule identity, `MinionBattleRuleCatalog` answers, and p.8's battle asks it for Heavy strength and for the removal it replaces with `PushLost` plus the ordinary Lane-Push request, with no Defeat, reward or Life consequence and no hero or card name anywhere. | 8.19c |
| Roster pausing rule sources | **Implemented at milestone 8.19c1.** `before-removal-prevention@1` is a seventh modification kind: a source declares a typed card price, `RemovalPreventionQuery` is its one directed asker, and the shared `RemovalStageDecision` asks the offer so rulebook p.13's two minion-Defeat paths honour it or neither does. The Defeat and its reward stand while the decision is open, paying the price discards the named card and publishes p.13's own `RemovalPrevented`, declining lets the removal happen, and the offer's candidates are re-derived at the answer rather than trusted from the pause. A source covering a hero is inert and says why: p.13 removes a defeated hero before it pays the coins, so a hero has no moment for the family to stand in. An Active effect declaring `EndOfRoundBeforeMinionBattle` executes before p.8's battle is planned, may stop on its own chooser, and resumes into the same battle. `after-push-trigger@1` is an Ultimate declaration read by `AfterPushTriggerQuery`: it runs once per object one of its controller's Pushes actually displaced, and a push that moved nothing reaches no trigger. The snapshot persists all three. | 8.19c1 |
| Modifier-vocabulary consumption and Wasp's two persistent-rule shapes | Implemented at milestone 8.7, closing F-CARD-03 by conformance rather than speculation. `ModificationSeamConformanceTests.EveryDeclarableModification_HasAProductionConsumerOrANamedExclusion` enumerates the space from the enums themselves - value channels, restriction and immunity activities, permission kinds, effective types, and the overlay arms - and requires each member either to carry a probe whose named production seam answers differently when it is declared, or to be excluded by name with its reason and the seam its consumer would go in. The probes read seams rather than the set, because the set answers everything by construction and the question is whether any rule asks. Restrictions gained the consumer they lacked: `ActionRestrictionQuery` answers one question for every action kind through `RuleActivityQuery.ForAction`, and the turn's offer set, the three general-rule offers and p.14's Defense response all ask it, so a card printing "cannot Attack" is obeyed without new code. Two declaration shapes came from Wasp's verified text: an actor-qualified restriction naming *who* is barred, and a `ConditionalObstacleOverlay` making spaces count as obstacles while a qualifying hero acts, consumed through `ObstacleQuery` - which p.18 makes the one owner of the classification - for movement, push and place legality alike. Their exact declarations, live bindings and consumers are maintained in [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md). The value channels no source modifies stay excluded by name, each pointing at the single total query that owns it. | 8.7 |
| Effect-invoked staged Defeat and the discard-else-defeated escalation | **Implemented for heroes at 8.10 and minions at 8.13.** Registered Defeat capabilities publish typed requests from a completed frame; the card-action authority consumes each once through the shared reward, Life/retrieval/cancellation where applicable, removal-decision, and mid-turn Lane-Push stages. High Voltage's generic Radius-minion selection filters immunity, declares Defeat as its consumer for removal preview, and its generic hero-adjacent-to-minion query runs before staged removal so `was adjacent` is an occupancy fact. Ledger question 13's discard-or-defeat escalation remains a branch with no player election. | 8.10 and 8.13 |
| Block resolution and defender-anchored riders | **Implemented at 8.11**, on the seam 5.8 reserved and as 8.2 specified it: `DefenseBlockResult` is evaluated at p.14's step 4 through `synthetic-attack-is-ranged` against the Attack's typed subtype facts, a fulfilled condition defends successfully regardless of totals, an unfulfilled one defeats the defender through 8.10's `synthetic-defeat-hero`, and both on-success riders compose the defender-anchored Range channel from the Defense card's own printed Range icon through `synthetic-range-hero-target` - one factory, differing only in the `excludeTheAttacker` parameter Deflect prints. | 8.11 |
| Registered action/effect dispatch seam with typed handler registration | Implemented at milestone 6.2a, on two deliberately different mechanisms. Action-kind dispatch stays direct concrete composition, which is what milestone 6.1h established and what a statically known collaborator calls for. Capability dispatch is data-driven lookup through `EffectCapabilityCatalog`, because the capability reference arrives as data inside the definition the resolver is walking and cannot be named at compile time. | 6.2a |
| Reusable modifier, restriction, permission, immunity, and effective-characteristic contracts | Implemented at milestone 6.8. One typed `RuleModification` declaration is definition content on an Active-effect definition or a token property; `RuleModificationQuery` derives the live set from active instances and placed tokens per command, and the value, offer, targeting, presence, effective-type, and obstacle queries consult it explicitly. No snapshot field carries a rule, so a restored effect or token rebinds its meaning from the trusted catalog. The contract's consumers are complete - the Move-Through permission, per-activation instance identity, the named absences every rule-affecting parameter states, and the effective-type query's production consumer - and are maintained with the unit-type question ledger in [Project/RULE_MODIFICATIONS.md](Project/RULE_MODIFICATIONS.md). | 6.8, consumers completed 6.8c-6.8g |
| Two declarations of p.18's Push over one implementation | Added at milestone 6.5a under rule 10 above. Object Push derives its target set from adjacency, a synthetic bound standing in until a card names its targets; a card that does name one cannot be expressed by adding a coordinate parameter to it, because that parameter would say nothing in the batch case. So a second capability declares the target-named form and the two share one routine for the geometry, the immunity filter, the one-pre-effect-state resolution, the farthest-first commit and the zero-space result. Both stay registered because both declarations are live content, which is what separates this from a revision: a revision replaces. | 6.5a |
| Shared Place operation and unit Spawn | Implemented at milestone 6.5. One immutable `PlacePolicy` declares the source container, eligible piece kind, destination query, chooser, and no-destination policy and refuses to exist without any of them, so an incomplete placement declaration cannot reach a match. `PlaceDestinationQuery` answers geometry rather than reachability, because p.18 states that placing is not movement, and the shared `PlaceOperation` re-derives the admitted destinations on the answer, commits, and reports a typed outcome carrying no movement vocabulary. The general capability, milestone 6.2's token placement, and milestone 6.4a's after-action correction fallback are three declarations against that one operation. Spawn is a separate capability and a fourth board port, because p.18 defines it by ruling out the board arm: its supply is the declared miniatures not currently standing, derived rather than stored, and it performs p.19's token clearance inside its own commit. p.18's Respawn is implemented on these same ports: hero Respawn as p.7's typed pending choice in `HeroRespawn`, and minion Respawn as the two registered capabilities dealt at 8.35z34. | 6.5 |
| Board-object Swap, Replace, and Remove over a fifth board port | Implemented at milestone 6.6. p.19 defines three operations, and each has its own capability ID, its own declaration or typed outcome, and no vocabulary belonging to another; treating any of them as interchangeable is a content-load rejection above. None can reach the displacement commit, which requires an empty destination: a Swap has two occupied spaces, a Replace never empties the one it works in, and a Remove has no board destination at all - which is why the board gains a fifth named port rather than a wider fourth. `SwapOperation` reads both positions from one pre-effect state and exchanges them in one commit that never leaves the occupancy index short an occupant. `ReplacePolicy` declares incoming source, outgoing destination, property transfer and observed triggers, refuses construction without any, and refuses a policy promising two observable stages. `RemovePolicy` declares its eligible piece kind and destination, admits no hero under any kind, and runs a named removal stage reporting removed or removal-prevented. p.8's mid-turn Lane-Push cause is recorded by that stage alone, over the same effective Battle-Zone set the Minion Battle counts, and a conformance rule keeps it the only caller. That stage is shared rather than this capability's own since 6.6b, so p.13's Defeat asks the same decision before taking its target off the board. The two operations p.19 grants a breadth declare it per node since 6.6a, so the eligibility reading lives on the declaration where the operation and the calling capability's probe cannot disagree. The Swap carries a second registered declaration whose first object is the acting hero, because a first object published by an earlier node cannot be read while an option is only being judged. | 6.6, 6.6b, 6.6a |
| Token-owned property sources and Clear replacement | Implemented at milestone 6.3. Trusted token-definition properties project as placement-lifetime continuous sources, and Clear uses one direct immediate replacement backend over a Core-revalidated Attack and chosen adjacent-token subset. Generic `ReplaceAction`, concrete property consumers, displacement, and general Spawn remain with their named triggers/milestones. | 6.3 subset implemented |
| Ultimate continuous source | Implemented at milestone 7.3 on the same seam. An unlocked Ultimate is read from the card rulebook p.16 places beside its owner's dashboard, and the definition there declares one typed `RuleModification` the same query composes with Active-effect and token-owned ones. Its applicability is not per-definition content, because p.16 states one applicability for every Ultimate - the ability is in effect exactly while its hero is on the board - so the source carries that named question and `RuleModificationQuery` asks it of the board rather than of any defeat or removal record. Nothing is stored and nothing is cancelled: a hero leaving the board suppresses the rule without touching the card, a legal return restores it, and every Active-effect cancellation checkpoint passes it by. | 7.3 |
| Modal multi-sequence Attack ("choose one, or both") | **Implemented at milestone 8.35z27**, as a contract wholly distinct from rulebook p.10's Repeat. A printed Attack declaring `attack-target@2` whose step-1 text is a chooser-owned `Choose` over `attack-sequence@1` leaves runs one complete p.14 order per chosen bullet, and Ruling 5 makes all of them ONE action. Each leaf performs nothing: it publishes its bullet's printed targeting SENTENCE as a `PrintedAttackTargetPolicy` plus the card's "(on different targets)" clause, and the card-action authority reads the ordered facts off the completed frame - the same transport 6.9's Lane-Push cause uses - so the plan survives the modal's own pause, a snapshot restore, and a replay. Each sequence then binds its own target against the board as it THEN stands (Ruling 19), takes its own Defense window, announces its own Attack Total with its own per-target minion modifiers, and runs its own p.13 consequences under a resolution identity of its own, because a Defeat outcome pays once per identity. Externally the action is one: `ActionStarted` and `ActionEnded` are raised once, "you performed an Attack action" is recorded once and never as a repeated instance, p.8's mid-turn Lane Push resolves at the end of the whole action rather than between sequences, and nothing about the chain is spelled "repeat" - which is the entire reason it is a separate contract, because Enfeeblement's "cannot repeat actions" reads that label (Ruling 14) and would otherwise shut down a card that never prints the word. The snapshot persists the plan, the started count and the units the earlier sequences bound. **Milestone 8.35z45 opened that extension point and composed Middlefinger of Death.** `AttackSequenceBullet` gains a closed `SubjectNarrowing` enum, evaluated per candidate at that bullet's sequence start over the set `FindLegalEnemyTargets` already admits, so p.13's reach and p.18's immunities are asked once and unchanged while the card's extra sentence narrows the answer. A capability REFERENCE was refused for it: a persisted payload may name identity and content, never carry semantics that choose which filter code runs. Schema v38 to v40, reject rather than migrate. | 8.35z27 |
| Declared-but-deferred hero contracts | **Declared at milestones 8.35z28 through 8.35z33**, the six-hero content wave, and registered by NONE of them. Each composed hero mints the contracts its own faces name and leaves them in its file's single `DeferredHandlers` list: milestone 7.9's gate refuses a registration no dealt card can start, and the registered list is SERIALIZED, so registering early moves the pinned list, every snapshot's bytes and every corpus digest while the content never moved. The wave therefore held the registered-handler count at 53 exactly. Each hero's dealing milestone moves its own lines across, with the showcase consumers the gate's second half requires and an identity pin measured there rather than predicted here. Two contracts are shared rather than per-hero and exist exactly once: the straight-line destination, kept as the superset whose optional obstacle-crossing parameter the other minter's nodes bind, and the empty-minion-spawn-point count, whose two handlers were merged because neither was a superset of the other. Dodger's dealing at 8.35z34 and Misa's at 8.35z38 each moved their own hero's contracts across, taking the registered-handler count to 61; each dealing's arithmetic - declarations against new handler TYPES, several sharing one handler - lives in its own completion evidence. Registering a contract a second hero also names is not registering on her behalf: the gate asks whether a DEALT card starts it, so a shared contract is deferred only until the FIRST of its sharers is dealt (ledger row 25). `choose-line-destination@1` accordingly registered with Misa because her dealt Blues start it, and Silverarrow's phase-one claim to it expired at that moment. Four heroes' contracts remain deferred. | 8.35z28-8.35z38 |
