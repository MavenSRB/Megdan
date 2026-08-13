# Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md).

## Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Phase 6 uses the complete Phase 4 map and the synthetic turn/round controller. Lane behavior is never validated on the reduced test grid.

### [x] 6.1 Resolve the end-of-round Minion Battle

Observable outcome: The authority compares Battle-Zone minions, resolves the losing team's choices with Heavy removed last, and records an end-of-round Lane-Push request if a team loses its last minion.

Acceptance checks:

- Equal counts have no effect.
- The losing team removes the exact difference or all remaining minions when it has fewer than required.
- Choice ownership and Heavy-last ordering are authoritative. A mandatory removal choice is round-owned typed pending state with stable resolution/choice identity, authorized chooser and team, required removal count, legal and currently selected stable `MinionId` values, and the exact typed select/deselect/confirm invocation that resumes the same end-of-round continuation.
- Rejected, stale, under-count, over-count, ineligible, and wrong-chooser input is atomic and preserves the pending choice, cursor, occupancy, events, and command history. Accepted confirmation removes exactly the authoritative selection and advances the owning stage once.
- The pending Minion-Battle choice is not retained in a handler-local flag or reconstructed from presentation. Its stage cursor, identities, candidates, selection, and continuation round-trip through the authoritative snapshot boundary with an explicit schema migrate-or-reject decision and reconstruct equivalently after restore, replay, and retained-prefix Undo.
- The application projection exposes the same immutable pending choice required by milestone 6.1a without acquiring rule ownership.
- Focused authority, session, snapshot-compatibility, restore, replay, and Undo tests prove equal-count/no-choice, deterministic auto-resolution, choice creation, accepted/rejected/stale input, exact resume, and byte-equivalent rebuilt pending state.
- The Lane-Push request is not consumed before token/marker cleanup.

Non-goals: No player-facing removal-choice UI, optional decline of a mandatory removal, Lane-Push consumption, token/marker cleanup, generic all-rules choice framework, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61-resolve-the-end-of-round-minion-battle-completion-evidence).

## Corrective sequence 6.1c-6.1n

Milestones 6.1c through 6.1n are the corrective sequence scheduled by the 2026-08-13 out-of-band foundation audit recorded in [Phase 6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-out-of-band-foundation-audit). They precede 6.1a because several findings concern structures that further presentation work would build on top of.

The audit was not a sanctioned fresh-task independent engineering review. It does not satisfy milestone 6.2b, and its findings do not authorize implementation: each milestone below still requires exact user approval. Milestone 6.1n is the terminal independent verification required by the recursive review-correction closure rule in [ROADMAP.md](../ROADMAP.md).

### [ ] 6.1c Retire the legacy HeroSelectionController drill and its fixtures

Observable outcome: The unconfigured legacy controller, its two partial source files, and its pinning fixtures are removed, with any coverage unique to them first re-proved against the configured stack.

Acceptance checks:

- Every behavior asserted only by `HeroSelectionControllerTests` and `CardDrivenMovementPresentationTests` is enumerated before deletion, and each item is either re-proved by a configured-stack fixture or explicitly recorded as already covered elsewhere. Coverage is not lost silently.
- `HeroSelectionController.cs` and `HeroSelectionController.CardDriven.cs` are deleted with their `.meta` files, along with the fixtures that exist only to keep them compiling.
- The composition-root guard that throws on the legacy drill and the cutover assertion that pins its absence are removed or replaced, since a type that no longer exists cannot be found in a scene.
- No remaining source constructs a Core rule authority, seeds board state from `GameObject` transforms, or resolves board positions from world coordinates outside the configured passive presentation boundary.
- The temporary-structure register row for the legacy drill is retired in the same milestone.
- Focused configured-stack Edit Mode fixtures and the saved-scene Play Mode check remain green, proving deletion changed no live behavior.

Non-goals: No configured-stack behavior change, no new presentation feature, no Minion Battle UI, no refactor of the surviving presenters beyond what deletion requires.

### [ ] 6.1d Make the build path explicit and assembly-bounded

Observable outcome: Runtime shader resolution is declared rather than incidental, and Editor-only code cannot reach production types through an implicit assembly.

Acceptance checks:

- Every shader named by a runtime `Shader.Find` call is present in the always-included shader list, so resolution does not depend on an unrelated material happening to be referenced from an enabled scene.
- All five runtime `Shader.Find` sites fail loudly on a null result instead of assigning a null material, using the existing explicit-failure pattern already present in `DestinationHighlightMaterialOwner`.
- `Assets/Editor` declares an Editor-only assembly definition with explicit references, so a loose Editor script cannot bypass `IMatchSession`.
- An Edit Mode fixture enumerates runtime shader names from source and asserts each resolves and is always-included, so a later deletion of the incidental material fails a test rather than shipping magenta.
- The declared target platforms in `PROJECT_CONTEXT.md` are each considered for stripping behavior, and any platform-specific requirement is recorded rather than assumed.

Non-goals: No render-pipeline migration, no production shader or material authoring, no visual change, no player-build automation, which is milestone 6.1m.

### [ ] 6.1e Correct editor-time composition hazards

Observable outcome: Edit-mode composition no longer dirties the saved scene with generated objects, and pending-destroy objects cannot enter authoritative board state.

Acceptance checks:

- Generated board, spawn, and minion objects are excluded from scene serialization, or full composition is gated behind an explicit opt-in rather than running on every edit-mode domain reload.
- View collection after a rebuild no longer observes objects awaiting deferred destruction. In play mode, a destroyed-but-not-yet-null object cannot be collected into the authoritative board or occupancy set.
- The stale authored hex tiles left over from the milestone 1.2 naming convention are removed from the saved scene, and the scene contains no generated object that the composition root intends to own.
- A focused regression proves that a rebuild followed immediately by view collection yields exactly the current generated set, with no destroyed, inactive, or duplicate entry.
- Rebuild, Play Mode entry, scene reload, Undo, restart, and restore retain the configured identities without `MissingReferenceException` or duplicate registration.

Non-goals: No presentation redesign, no board view-model contract change, no change to authoritative map content or setup.

### [ ] 6.1f Establish a single card mutation path and an aggregate root

Observable outcome: Authoritative card state has exactly one mutation path and one transactional commit boundary, replacing four independently mutable graphs reachable through read-only collection wrappers.

Acceptance checks:

- The direct location-transition call in the Attack/Defense backend is routed through the centralized card lifecycle authority, so a duplicated guard cannot drift from the canonical one.
- Card state cannot be mutated outside the lifecycle authority, whether by making the mutating members inaccessible or by replacing instances rather than mutating them. A read-only collection no longer implies read-only elements.
- An aggregate root owns the configuration, board, card-action, and round graphs and exposes one commit boundary. It uses the plan, validate, then commit pattern already proven by `DefeatRewardAccounting.Award`, so a rejected operation cannot leave partial state.
- Ordered rule events are emitted as part of the same commit rather than appended alongside state writes with nothing enforcing agreement.
- Accepted, rejected, stale, restored, replayed, and Undo-truncated scenarios produce byte-identical authoritative snapshots and identical ordered events before and after the change.

Non-goals: No rule meaning change, no schema-version change unless the refactor requires one with an explicit migrate-or-reject decision, no presentation change, no performance work, which is milestone 6.1i.

### [ ] 6.1g Make snapshot restore compile-enforced

Observable outcome: Adding a persisted field without restoring it becomes a compile error rather than silent state loss.

Acceptance checks:

- The optional and defaulted parameters on the authority restore entry points are replaced by a single required typed restore-state value per authority, so an omitted field cannot compile.
- Adding one persisted field requires exactly one coordinated change per authority rather than four independent edits across the authority, its restore path, and both serializer directions.
- A deliberately incomplete restore fails to compile, and this is demonstrated rather than asserted.
- Existing schema-version behavior, the compatibility corpus, and byte-identical round-trips are unchanged by the refactor.
- `RISK-STATE-001` is reassessed and retired or explicitly accepted in the same milestone.

Non-goals: No schema-version bump, no new persisted state, no change to what is persisted, no serializer format change.

### [ ] 6.1h Remove dispatch ceremony and decompose the largest authorities

Observable outcome: Statically-known dispatch is direct, and the largest Core files are decomposed along their real responsibility seams.

Acceptance checks:

- The card-action strategy registry is removed or reduced where every interface has exactly one implementation and every call site passes a statically-known concrete handler type. Extension points that genuinely need indirection are retained and named.
- `RoundResolutionAuthority` and `AuthoritativeSnapshotSerializer` are decomposed so that no single file owns unrelated stage, choice, and serialization responsibilities, and constructor parameter counts fall correspondingly.
- The architecture conformance tests still mechanically prevent action-specific algorithms from accumulating in the outer session, round, and turn dispatch seams.
- Behavior, ordered events, snapshot bytes, and replay determinism are unchanged, proven by byte-identical comparison across the refactor.

Non-goals: No rule change, no new abstraction layer introduced to replace the removed one, no renaming for its own sake, no schema change.

### [ ] 6.1i Report errors honestly and gate diagnostic cost

Observable outcome: Genuine defects surface as defects rather than as rule rejections, and debug-grade self-verification no longer runs unconditionally in shipped builds.

Acceptance checks:

- The broad exception handler in the application session no longer reports `NullReferenceException`, missing-key, and single-element failures as `DomainRejected`. Rule rejections and programming defects are distinguishable to both the caller and the log.
- The pre-command atomicity snapshot and its string comparison are compile-gated or diagnostic-mode-gated rather than executing on every command in a player build.
- The rule-event log envelope no longer re-encodes the entire accumulated log on every capture, so per-command serialization cost stops growing with session length.
- After the change, an accepted command performs a measured and recorded number of full serializations, and that number is asserted by a regression fixture rather than left to inspection.
- Loading a snapshot no longer leaves Undo permanently unavailable without saying so: the projection states explicitly that history is unavailable, distinct from having nothing to undo.
- A managed-allocation regression fixture covers the representative accepted-command path, with thresholds recorded as regression evidence rather than production targets.

Non-goals: No removal of the atomicity or determinism guarantees themselves, no snapshot format redesign beyond the envelope growth fix, no speculative optimization outside the measured paths.

### [ ] 6.1j Single-source the board map specification

Observable outcome: The authoring specification and the runtime resource cannot drift apart silently.

Acceptance checks:

- The two byte-identical map copies are reduced to one canonical source with a generated or verified executable copy.
- If two files remain, an automated check asserts byte equality and fails on drift; the check runs in the same validation tier as the existing map specification validator.
- The existing reviewed counts, digests, and map-local identity contracts are unchanged, and the runtime resource remains byte-identical to the reviewed specification.
- The relationship between the two paths is documented once in its canonical home rather than implied by a README sentence.

Non-goals: No map content change, no revision bump, no change to the map schema, loader, or catalog contracts.

### [ ] 6.1k Replace brittle structural assertions and deepen the newest coverage

Observable outcome: Architecture tests survive renames that change no behavior, and the newest stateful subsystem carries coverage proportionate to its statefulness.

Acceptance checks:

- Assertions that match raw C# source text, literal scene GUIDs, or private field declarations are replaced by behavioral or reflection-based checks that fail for real regressions and pass through a pure rename.
- The architecture conformance intent is preserved exactly: whatever each brittle assertion was protecting is still protected, and this mapping is stated explicitly rather than assumed.
- `MinionBattleResolution` gains coverage beyond its four cases, including exact-count boundaries, Heavy-last ordering under multiple candidate shapes, and deterministic auto-resolution, using fixed-seed bounded property cases consistent with existing practice.
- No test is weakened or deleted merely because it is brittle; each is replaced by an equivalent or stronger check.

Non-goals: No production behavior change, no new subsystem, no reduction in existing coverage, no change to the test assembly split.

### [ ] 6.1l Make the validation toolchain cross-platform

Observable outcome: The documentation and specification validators run on every declared development platform, and Unity discovery is not hardcoded to one installation path.

Acceptance checks:

- Path segments are constructed portably rather than with backslash-separated literals passed to path joins.
- The Unity and Hub executable locations are discovered through an environment override plus platform-appropriate probing, with a clear failure message when discovery fails.
- Each script is confirmed to run on Windows and on at least one non-Windows platform, and the exact confirmation evidence is recorded.
- The required launcher contract in `TESTING.md` remains a single fixed parameterless entry point; portability does not become a set of per-platform ad-hoc commands.

Non-goals: No continuous-integration service selection or configuration, no build automation, no change to validation tiers, recipes, or gate definitions.

### [ ] 6.1m Add a player-build smoke gate

Observable outcome: A defined gate produces a real player build and proves it starts and reaches a configured match, so editor-only regressions cannot pass unnoticed.

Acceptance checks:

- A headless player build is produced for the primary desktop target through a fixed project-local entry point consistent with the existing launcher contract.
- The built player is started and verified to reach the configured scene and complete a scripted minimal interaction, with the exact evidence recorded.
- Missing shaders, missing resources, stripped types, and failed scene loads fail the gate rather than degrading silently.
- The gate is defined in `Testing/FUTURE_GATES.md` with its exact command, expected result, and the milestones and phase gates at which it is required.
- The gate is proven to fail when a required shader is deliberately removed from the always-included list, then restored.

Non-goals: No store packaging, signing, provisioning, distribution, mobile player support, or performance target; no publication of any build.

### [ ] 6.1n Independently verify the corrective sequence

Observable outcome: A fresh-task independent review determines whether milestones 6.1c through 6.1m actually closed the 2026-08-13 audit findings before Phase 6 feature work resumes.

Acceptance checks:

- Inspect the actual final source, tests, scene, tooling, and raw validation evidence rather than relying on milestone handoff summaries.
- Confirm each audit finding's disposition individually against the code, and record any finding that was closed in documentation but not in fact.
- Apply the four mandatory review dimensions in `ROADMAP.md`: buildability on every declared platform, dead and temporary structures, documented-contract truth, and toolchain portability.
- Verify that the three code-truth validator checks are enforcing rather than passing vacuously, by confirming each has been observed to fail on a deliberate violation.
- Classify every observation as a demonstrated defect, a measured or plausible risk, or an optional improvement, and assign the smallest corrective owner and required validation for each actionable finding.
- Milestone 6.1a remains blocked until this review closes the sequence or the user approves a documented corrective disposition with a fresh terminal verification owner.

Non-goals: No runtime, test, scene, package, assembly, content, or rule change; no implementation of a discovered correction inside the review milestone.

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

### [x] 6.1b Correct Defense contribution cue proportions and orientation

Observable outcome: The configured board-facing `+1` and `-1` minion Defense contribution cues use smaller readable numbers, vertically oval dark-grey backings that fit the signed values, and text mirrored around its local vertical axis to correct the inherited token orientation.

Acceptance checks:

- Both signed cues retain their exact projected `+1`/`-1` meaning and existing supporting/hindering color distinction while reducing the number size from the milestone 5.8b presentation.
- The backing is vertically oval rather than circular, uses dark grey rather than near-black, and frames the signed value without crowding.
- Only the number text is inverted around its local vertical axis; the backing remains symmetric and the resulting cue faces the configured top-down board camera correctly.
- Focused view and configured-scene Play Mode coverage proves the exact text, smaller character size, vertical oval proportions, dark-grey backing, horizontal centering plus the reviewed downward optical correction, horizontal inversion, unchanged cue lifetime, and unchanged pointer-pass-through presentation boundary for both signs.

Non-goals: No Defense calculation, contribution projection, cue identity/lifetime, target highlighting, input, Minion Battle UI, production art, animation, or audio changes.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61b-correct-defense-contribution-cue-proportions-and-orientation-completion-evidence).

### [ ] 6.2 Add token and marker instances, supplies, placement, and attachment

Observable outcome: Synthetic tokens and markers have stable authoritative identity without requiring Wasp content.

Acceptance checks:

- Tokens use the authoritative finite supply and occupy board spaces as obstacles.
- Markers are attachments rather than board occupants and have explicit return state.
- Token placement/reclamation and marker attachment are authoritative choices/transitions.
- Token and marker lifetime is not inferred from art or UI objects.

### [ ] 6.2a Establish typed Skill and serializable effect execution

Observable outcome: A bounded synthetic definition can offer and execute a first-class Skill through a stable-node, serializable non-Repeat effect frame before Clear or displacement effects extend the production action seam.

Acceptance checks:

- Skill is a first-class typed action offer and invocation with the same authoritative source, actor, lifecycle, command, event, timing, completion, and stale-input boundaries as existing card actions; presentation never infers Skill legality from printed text.
- Stable-node `Sequence`, `Optional`, `Conditional`, and `Choose` structures preserve typed node identity, chooser ownership, inputs, outcomes, and the exact continuation required to resume. Mandatory failure, `may`, `up to`, and `if able` policies remain explicit rather than collapsing into one failure behavior.
- One bounded synthetic capability fixture executes end to end through `IMatchSession`, the round/turn authorities, the registered action/effect dispatch seam, snapshots, restore, replay, and retained-prefix Undo. Rejected input is atomic and accepted execution emits deterministic ordered facts.
- Adding the Skill/effect handler does not require editing Movement, Attack/Defense, or Fast-Travel backend algorithms. Architecture tests mechanically prevent action-specific algorithms or effect-node switches from accumulating in the outer session/round/turn dispatcher seams.
- Pending effect state and stable node/choice identities cross the versioned snapshot boundary with an explicit migrate-or-reject decision and reconstruct equivalently after restore, replay, and Undo.

Non-goals: No Repeat, Clear, direct Move, Object Push, Place, Spawn, Swap, Replace, Remove, Wasp definition, production presentation, printed-text parser, universal scripting language, or source-unverified special case.

### [ ] 6.2b Independently verify the Phase 6 action/effect foundation

Observable outcome: A fresh-task independent review determines whether milestones 6.1 and 6.2a actually close `F58C-01` and `F58C-02` before milestone 6.3 can begin.

Acceptance checks:

- Inspect the actual final source, tests, snapshots/compatibility fixtures, replay/Undo paths, and raw validation evidence rather than relying on implementation handoff summaries.
- Trace accepted, rejected, stale, paused, restored, replayed, and Undo-truncated Minion-Battle and synthetic Skill/effect paths through the application, round/turn, action/effect, and serialization boundaries.
- Confirm mechanically that the first real end-of-round choice has no handler-local or presentation-owned continuation, and that adding the production Skill/effect handler did not deepen unrelated action-family branching.
- Classify every observation as a demonstrated defect, measured or plausible risk, or optional improvement; assign the smallest corrective owner and required validation for each actionable finding.
- Reassess the dependency-safe sequence and scope of milestones 6.3 through 6.13. Milestone 6.3 remains blocked until this review closes both findings or the user approves a documented corrective disposition with a fresh terminal verification owner.

Non-goals: No runtime, test, scene, package, assembly, content, or rule change; no implementation of a discovered correction inside the review milestone.

### [ ] 6.3 Add core token interactions, persistent properties, and Clear

Observable outcome: Synthetic tokens preserve their properties while supporting the general board interactions needed by the core loop.

Prerequisite: milestone 6.2b has independently closed the post-5.8c action/effect foundation findings or recorded an approved correction-plus-verification disposition.

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

### [ ] 6.7 Add bounded Repeat action control flow

Observable outcome: Synthetic definitions can Repeat an eligible action as a separate bounded action instance through milestone 6.2a's typed effect frame, with each instance completing displaced-minion correction before the next begins.

Acceptance checks:

- Repeat creates a separate bounded action instance with the original definition and explicit repeated context, cannot recursively Repeat, and applies the verified replacement and Active-effect restrictions.
- Each repeated action reaches its own after-action displaced-minion correction boundary before the next Repeat instance begins.
- A repeated action uses the same legality, typed choices, timing, completion, and effect outcomes as an ordinary instance; later instances may make different legal choices without mutating the original action definition.
- Declined, impossible, prevented, interrupted, restored, replayed, and Undo-truncated synthetic scenarios produce deterministic state and ordered events without recursive execution or duplicate completion.
- Focused integration tests cover Repeat across direct Move/Object Push displacement, the milestone 6.4a correction checkpoint, pending milestone 6.4b player choice, snapshot restore, replay, and retained-prefix Undo.

Non-goals: No new Skill, Sequence, Optional, Conditional, or Choose foundation; no new leaf effect merely to exercise Repeat; no Wasp definition or text interpretation, invented scripting language, card-name/hero-name branch, production presentation, or source-unverified special case.

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
- The Phase 6 gate performs a read-only folder/file housekeeping audit covering stale generated or temporary outputs, empty or misplaced directories, duplicate artifacts, and tracked-file reference integrity. It records a bounded proposed action list; deletion, broad reorganization, and structural moves remain separately approved work, and excluded rulebook/card-corpus sources plus Unity asset/`.meta` identity must be preserved.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused Phase 6 scenarios.
