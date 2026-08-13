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

## Corrective sequence 6.1c-6.1k

Milestones 6.1c through 6.1k are the corrective sequence scheduled by the 2026-08-13 out-of-band foundation audit recorded in [Phase 6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-out-of-band-foundation-audit). They precede 6.1a because several findings concern structures that further presentation work would build on top of.

The audit was not a sanctioned fresh-task independent engineering review. It does not satisfy milestone 6.2b, and its findings do not authorize implementation: each milestone below still requires exact user approval.

Every milestone here names the exact check that closes its findings. Per the mechanical closure rule in [ROADMAP.md](../ROADMAP.md), a finding closes only when its named check has been observed to fail at the pre-fix revision and pass at the post-fix revision, with both observations recorded in the milestone's commit message. Every one of the fourteen audit findings has such a check, so this sequence has no terminal independent verification milestone; the former 6.1n was removed on 2026-08-13 in the same amendment that retired the automatic recursive verification requirement.

**Execution order**, by severity rather than by the order the audit wrote them. IDs are unchanged so that the audit disposition table, the risk register, and existing cross-references stay valid.

| Order | Milestone | Findings | Why here |
| --- | --- | --- | --- |
| 1 | 6.1e | `F613-05`, `F613-04` | The only confirmed live correctness defect |
| 2 | 6.1g | `F613-07` | Silent state loss across thirteen restore back doors (6.1g verified sixteen) |
| 3 | 6.1c | `F613-01` | Deletes ~4,900 lines, shrinking every later milestone's surface. Complete |
| 4 | 6.1d | `F613-02`, `F613-03`, `F613-12` | Merged build and toolchain integrity; its smoke gate is the standing buildability check. Complete |
| 5 | 6.1f | `F613-06` | Largest refactor; benefits from 1-3 landing first. Complete |
| 6 | 6.1i | `F613-08`, `F613-09`, `F613-10` | Diagnosability and measured cost. Complete |
| 7 | 6.1h | `F613-11` | Pure maintainability; lowest risk to defer. Complete |
| 8 | 6.1k | `F613-13` | Must land after 7, whose renames its replacement assertions have to survive. Complete |
| - | 6.1l | `F613-14` | Reinstated 2026-08-13 as `[!]` blocked; executed later that day when the ER2 session's Linux workstation supplied the missing non-Windows host. Complete |

Milestones 6.1j, 6.1l, and 6.1m were merged into 6.1d on 2026-08-13 under the granularity floor in [ROADMAP.md](../ROADMAP.md). Their acceptance checks are preserved verbatim inside it; no check was dropped in the merge.

Later that day, **6.1l was reinstated** with explicit user approval. The merge was sound on granularity grounds but assumed the confirmation was performable; it is not, in the current environment. 6.1d keeps 6.1l's implementation checks and 6.1l keeps its confirmation check, preserved verbatim. 6.1j and 6.1m remain merged.

### [x] 6.1c Retire the legacy HeroSelectionController drill and its fixtures

Observable outcome: The unconfigured legacy controller, its two partial source files, and its pinning fixtures are removed, with any coverage unique to them first re-proved against the configured stack.

Acceptance checks:

- Every behavior asserted only by `HeroSelectionControllerTests` and `CardDrivenMovementPresentationTests` is enumerated before deletion, and each item is either re-proved by a configured-stack fixture or explicitly recorded as already covered elsewhere. Coverage is not lost silently.
- `HeroSelectionController.cs` and `HeroSelectionController.CardDriven.cs` are deleted with their `.meta` files, along with the fixtures that exist only to keep them compiling.
- The composition-root guard that throws on the legacy drill and the cutover assertion that pins its absence are removed or replaced, since a type that no longer exists cannot be found in a scene.
- No remaining source constructs a Core rule authority, seeds board state from `GameObject` transforms, or resolves board positions from world coordinates outside the configured passive presentation boundary.
- The temporary-structure register row for the legacy drill is retired in the same milestone.
- Focused configured-stack Edit Mode fixtures and the saved-scene Play Mode check remain green, proving deletion changed no live behavior.

Closing check for `F613-01`: the validator's `unreachable-component` check. It has already been observed failing on `HeroSelectionController` when its temporary-structure register row is removed while the type still exists, which is the pre-fix half of the transition. The post-fix half is the validator passing after the type, its partial, its fixtures, and its register row are all deleted together. Deleting only the register row must still fail.

Non-goals: No configured-stack behavior change, no new presentation feature, no Minion Battle UI, no refactor of the surviving presenters beyond what deletion requires.

### [x] 6.1d Make the build path explicit, single-sourced, and portable

Merged on 2026-08-13 from former milestones 6.1d, 6.1j, 6.1l, and 6.1m. All four concern one question - does this project build, resolve its resources, and run its toolchain on the platforms it declares - and each was too small to carry its own handoff. Every original acceptance check is preserved verbatim below.

**2026-08-13 amendment, approved by the user.** The former 6.1l cross-platform *confirmation* check is withdrawn from this milestone and returned to a reinstated milestone `6.1l`, because the confirmation cannot be performed in the current environment: WSL reports "not installed", Docker is absent, and PowerShell Core is not present, so there is no non-Windows host on which to run a PowerShell validator. 6.1d still implements the portable path construction and the environment-override discovery; only the per-platform confirmation moves. `F613-14` therefore stays open at 6.1d and is owned by 6.1l. The withdrawn check is preserved verbatim in the 6.1l section rather than deleted.

Observable outcome: Runtime shader resolution is declared rather than incidental, Editor-only code cannot reach production types through an implicit assembly, the board map has one source of truth, the validation toolchain runs on every declared development platform, and a real player build is proven to start and reach a configured match.

Acceptance checks - build path and assembly boundary (former 6.1d):

- Every shader named by a runtime `Shader.Find` call is present in the always-included shader list, so resolution does not depend on an unrelated material happening to be referenced from an enabled scene.
- All runtime `Shader.Find` sites fail loudly on a null result instead of assigning a null material, using the existing explicit-failure pattern already present in `DestinationHighlightMaterialOwner`. The audit counted five; the fifth was inside `HeroSelectionController`, which milestone 6.1c deleted at `2fa2212` before this milestone ran, so the check was met by the four that remain.
- `Assets/Editor` declares an Editor-only assembly definition with explicit references, so a loose Editor script cannot bypass `IMatchSession`.
- An Edit Mode fixture enumerates runtime shader names from source and asserts each resolves and is always-included, so a later deletion of the incidental material fails a test rather than shipping magenta.
- The declared target platforms in `PROJECT_CONTEXT.md` are each considered for stripping behavior, and any platform-specific requirement is recorded rather than assumed.

Acceptance checks - single-source board map (former 6.1j):

- The two byte-identical map copies are reduced to one canonical source with a generated or verified executable copy.
- If two files remain, an automated check asserts byte equality and fails on drift; the check runs in the same validation tier as the existing map specification validator.
- The existing reviewed counts, digests, and map-local identity contracts are unchanged, and the runtime resource remains byte-identical to the reviewed specification.
- The relationship between the two paths is documented once in its canonical home rather than implied by a README sentence.

Acceptance checks - cross-platform toolchain (former 6.1l):

- Path segments are constructed portably rather than with backslash-separated literals passed to path joins.
- The Unity and Hub executable locations are discovered through an environment override plus platform-appropriate probing, with a clear failure message when discovery fails.
- The required launcher contract in `TESTING.md` remains a single fixed parameterless entry point; portability does not become a set of per-platform ad-hoc commands.

The former third check in this group, requiring each script to be confirmed on Windows and on at least one non-Windows platform, moved to milestone `6.1l` under the 2026-08-13 amendment above.

Acceptance checks - player-build smoke gate (former 6.1m):

- A headless player build is produced for the primary desktop target through a fixed project-local entry point consistent with the existing launcher contract.
- The built player is started and verified to reach the configured scene and complete a scripted minimal interaction, with the exact evidence recorded.
- Missing shaders, missing resources, stripped types, and failed scene loads fail the gate rather than degrading silently.
- The gate is defined in `Testing/FUTURE_GATES.md` with its exact command, expected result, and the milestones and phase gates at which it is required.
- The gate is proven to fail when a required shader is deliberately removed from the always-included list, then restored.

Closing checks:

- `F613-02`: the Edit Mode shader fixture, observed failing when a runtime shader name is absent from the always-included list and passing once declared.
- `F613-03`: the existing assembly-boundary tests, observed failing when an Editor script references a production type directly.
- `F613-12`: the map byte-equality check, observed failing on a deliberate one-byte divergence between the two paths.

`F613-14` is no longer closed here; see milestone `6.1l`.

Once the player-build smoke gate passes, the **buildability** review dimension is deleted from the prose list in [ROADMAP.md](../ROADMAP.md), per the rule that a dimension leaves that list only when its check has been observed to fail on a deliberate violation. The **toolchain portability** dimension stays in that list until `6.1l` completes, because no check has yet been observed proving it.

Non-goals: No render-pipeline migration, no production shader or material authoring, no visual change, no map content change, no revision bump, no change to the map schema, loader, or catalog contracts, no continuous-integration service selection or configuration, no change to validation tiers, recipes, or gate definitions, no store packaging, signing, provisioning, distribution, mobile player support, or performance target, and no publication of any build.

### [x] 6.1e Correct editor-time composition hazards

Observable outcome: Edit-mode composition no longer dirties the saved scene with generated objects, and pending-destroy objects cannot enter authoritative board state.

Acceptance checks:

- Generated board, spawn, and minion objects are excluded from scene serialization, or full composition is gated behind an explicit opt-in rather than running on every edit-mode domain reload.
- View collection after a rebuild no longer observes objects awaiting deferred destruction. In play mode, a destroyed-but-not-yet-null object cannot be collected into the authoritative board or occupancy set.
- The stale authored hex tiles left over from the milestone 1.2 naming convention are removed from the saved scene, and the scene contains no generated object that the composition root intends to own.
- A focused regression proves that a rebuild followed immediately by view collection yields exactly the current generated set, with no destroyed, inactive, or duplicate entry.
- Rebuild, Play Mode entry, scene reload, Undo, restart, and restore retain the configured identities without `MissingReferenceException` or duplicate registration.

Closing checks:

- `F613-05`: the focused rebuild-then-collect regression, observed failing against the current deferred-`Destroy` path before the fix and passing after. This is the sequence's only confirmed live correctness defect, so the pre-fix failure must be a genuine reproduction, not a fixture asserting the corrected behavior.
- `F613-04`: a check that the saved scene contains no generated object the composition root intends to own, observed failing against the seven stale authored tiles before removal.

Non-goals: No presentation redesign, no board view-model contract change, no change to authoritative map content or setup.

### [x] 6.1f Establish a single card mutation path and an aggregate root

Observable outcome: Authoritative card state has exactly one mutation path and one transactional commit boundary, replacing four independently mutable graphs reachable through read-only collection wrappers.

Acceptance checks:

- The direct location-transition call in the Attack/Defense backend is routed through the centralized card lifecycle authority, so a duplicated guard cannot drift from the canonical one.
- Card state cannot be mutated outside the lifecycle authority, whether by making the mutating members inaccessible or by replacing instances rather than mutating them. A read-only collection no longer implies read-only elements.
- An aggregate root owns the configuration, board, card-action, and round graphs and exposes one commit boundary. It uses the plan, validate, then commit pattern already proven by `DefeatRewardAccounting.Award`, so a rejected operation cannot leave partial state.
- Ordered rule events are emitted as part of the same commit rather than appended alongside state writes with nothing enforcing agreement.
- Accepted, rejected, stale, restored, replayed, and Undo-truncated scenarios produce byte-identical authoritative snapshots and identical ordered events before and after the change.

Closing check for `F613-06`: a byte-identical snapshot and ordered-event comparison across all six scenarios, captured at the pre-refactor revision and re-run at the post-refactor revision. Because this refactor must change no observable behavior, its closure evidence is the comparison passing, plus a demonstration that mutating card state outside the lifecycle authority no longer compiles - the latter observed failing before the change.

Non-goals: No rule meaning change, no schema-version change unless the refactor requires one with an explicit migrate-or-reject decision, no presentation change, no performance work, which is milestone 6.1i.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61f-establish-a-single-card-mutation-path-and-an-aggregate-root-completion-evidence).

### [x] 6.1g Make snapshot restore compile-enforced

Observable outcome: Adding a persisted field without restoring it becomes a compile error rather than silent state loss.

Acceptance checks:

- The optional and defaulted parameters on the authority restore entry points are replaced by a single required typed restore-state value per authority, so an omitted field cannot compile.
- Adding one persisted field requires exactly one coordinated change per authority rather than four independent edits across the authority, its restore path, and both serializer directions.
- A deliberately incomplete restore fails to compile, and this is demonstrated rather than asserted.
- Existing schema-version behavior, the compatibility corpus, and byte-identical round-trips are unchanged by the refactor.
- `RISK-STATE-001` is reassessed and retired or explicitly accepted in the same milestone.

Closing check for `F613-07`: a deliberately incomplete restore, demonstrated to compile cleanly at the pre-fix revision and to fail compilation at the post-fix revision. The existing acceptance check already requires this to be demonstrated rather than asserted; the pre-fix half is what makes it closure evidence rather than a passing test.

Non-goals: No schema-version bump, no new persisted state, no change to what is persisted, no serializer format change.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61g-make-snapshot-restore-compile-enforced-completion-evidence).

### [x] 6.1h Remove dispatch ceremony and decompose the largest authorities

Observable outcome: Statically-known dispatch is direct, and the largest Core files are decomposed along their real responsibility seams.

Acceptance checks:

- The card-action strategy registry is removed or reduced where every interface has exactly one implementation and every call site passes a statically-known concrete handler type. Extension points that genuinely need indirection are retained and named.
- `RoundResolutionAuthority` and `AuthoritativeSnapshotSerializer` are decomposed so that no single file owns unrelated stage, choice, and serialization responsibilities, and constructor parameter counts fall correspondingly.
- The architecture conformance tests still mechanically prevent action-specific algorithms from accumulating in the outer session, round, and turn dispatch seams.
- Behavior, ordered events, snapshot bytes, and replay determinism are unchanged, proven by byte-identical comparison across the refactor.

Closing check for `F613-11`: the byte-identical behavior/event/snapshot comparison across the refactor, plus the existing architecture conformance tests observed still failing when an action-specific algorithm is deliberately introduced into the outer dispatch seam. A decomposition milestone's risk is silent behavior change, so the comparison is the closure evidence.

Non-goals: No rule change, no new abstraction layer introduced to replace the removed one, no renaming for its own sake, no schema change.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61h-remove-dispatch-ceremony-and-decompose-the-largest-authorities-completion-evidence).

### [x] 6.1i Report errors honestly and gate diagnostic cost

Observable outcome: Genuine defects surface as defects rather than as rule rejections, and debug-grade self-verification no longer runs unconditionally in shipped builds.

Acceptance checks:

- The broad exception handler in the application session no longer reports `NullReferenceException`, missing-key, and single-element failures as `DomainRejected`. Rule rejections and programming defects are distinguishable to both the caller and the log.
- The pre-command atomicity snapshot and its string comparison are compile-gated or diagnostic-mode-gated rather than executing on every command in a player build.
- The rule-event log envelope no longer re-encodes the entire accumulated log on every capture, so per-command serialization cost stops growing with session length.
- After the change, an accepted command performs a measured and recorded number of full serializations, and that number is asserted by a regression fixture rather than left to inspection.
- Loading a snapshot no longer leaves Undo permanently unavailable without saying so: the projection states explicitly that history is unavailable, distinct from having nothing to undo.
- A managed-allocation regression fixture covers the representative accepted-command path, with thresholds recorded as regression evidence rather than production targets.

Closing checks:

- `F613-08`: the per-command serialization-count assertion, recorded at four-per-accepted and six-per-rejected before the fix and at the reduced measured count after, plus the managed-allocation fixture showing envelope cost no longer growing with session length.
- `F613-09`: a test that a deliberately injected `NullReferenceException` in a command path is reported as a programming defect rather than `DomainRejected`, observed failing before the fix.
- `F613-10`: a projection test asserting history-unavailable is distinguishable from nothing-to-undo, observed failing before the fix.

Non-goals: No removal of the atomicity or determinism guarantees themselves, no snapshot format redesign beyond the envelope growth fix, no speculative optimization outside the measured paths.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61i-report-errors-honestly-and-gate-diagnostic-cost-completion-evidence).

### [x] 6.1k Replace brittle structural assertions and deepen the newest coverage

Observable outcome: Architecture tests survive renames that change no behavior, and the newest stateful subsystem carries coverage proportionate to its statefulness.

Acceptance checks:

- Assertions that match raw C# source text, literal scene GUIDs, or private field declarations are replaced by behavioral or reflection-based checks that fail for real regressions and pass through a pure rename.
- The architecture conformance intent is preserved exactly: whatever each brittle assertion was protecting is still protected, and this mapping is stated explicitly rather than assumed.
- `MinionBattleResolution` gains coverage beyond its four cases, including exact-count boundaries, Heavy-last ordering under multiple candidate shapes, and deterministic auto-resolution, using fixed-seed bounded property cases consistent with existing practice.
- No test is weakened or deleted merely because it is brittle; each is replaced by an equivalent or stronger check.

Closing check for `F613-13`: each replacement assertion run twice - once against a deliberately introduced real regression, where it must fail, and once against a pure rename that changes no behavior, where it must pass. The old assertion failing the rename case is the pre-fix half. `MinionBattleResolution`'s added coverage closes on its new cases failing against deliberately broken exact-count and Heavy-last ordering.

Non-goals: No production behavior change, no new subsystem, no reduction in existing coverage, no change to the test assembly split.

### [x] 6.1o Honour the atomicity guarantee in every build, and measure what actually grows

Approved on 2026-08-13 from an independent review of the work completed through 6.1h. The review produced five findings; all five were re-verified against `d093847` before this milestone was written. Two are defects in milestone 6.1i's work, and both share one cause: 6.1i read the rejected-command mutation comparison as debug-grade self-verification, when it is the mechanism that delivers the aggregate's atomicity guarantee rather than a report about it.

The letter is `6.1o` because `6.1j` and `6.1m` were merged into `6.1d`, `6.1n` was removed when review-correction closure became mechanical, and `6.1l` is blocked rather than spent.

Observable outcome: A refused command leaves no partial state in a shipping player build and not only in a development one; per-command cost stops growing with session length at every site rather than at the one an encoded-character measurement could see; and the risk register states what is actually true of both.

Acceptance checks:

- The rejected-command comparison and its rollback execute in every build configuration. No mode, symbol, or constructor overload selects a session that skips them.
- The rollback pre-image captures in constant time. No path from a command to a pre-image copies the accumulated rule-event log or the current round's events.
- The immutable projection stops copying the whole accumulated rule-event log on every refresh.
- The regression fixture for the growth half measures something that a copy of the log would fail. The encoded-character measurement is retained for the encode half it does cover, with its scope stated.
- `RISK-PERF-001` is reopened before it is retired again, and its record says why the first retirement was premature.
- `RISK-STATE-002` receives the reassessment its own row scheduled for 6.1h.
- The living-document validator gains a check for the one-row archive policy adopted on 2026-08-13, and one that includes `README.md` in the completed-milestone status set.

Closing checks:

- Finding 1: `MatchAggregateAtomicityTests` - a plan that mutates the board and then refuses is rolled back and reported as `AtomicityViolation`, on a branch that had no coverage at all beforehand; plus the type-level assertion that no session shape can skip the check. The pre-fix half is a compile transition in the precedent of 6.1g: `new MatchSession(state, MatchDiagnosticsMode.Off)` compiled before and does not exist after, and the cost fixture's `Off` case asserted zero serializations on the rejected path where the corrected fixture asserts one.
- Finding 2: `CapturePreImage_CopiesNothingHoweverLongTheSessionIs`, which fails against the pre-fix `ToArray()` capture at any session length, plus `AppendOnlyEventLogTests` covering the snapshot's behaviour under growth and under the wholesale replacement a restore performs.
- Findings 3, 4, and 5: the two new validator checks observed failing against the pre-fix documents and passing after.

Non-goals: No conversion of the in-place authorities beneath the aggregate to a plan-only shape - that is `RISK-STATE-002`'s deeper subject and stays open. No rewrite of existing archive narratives, which the append-only rule protects; the new check is forward-looking and grandfathers what is already written. No change to the snapshot format, schema version, or the pinned compatibility digests.

### [x] 6.1p Close the three limitations milestone 6.1o declared

Approved on 2026-08-13. Milestone 6.1o closed five review findings and recorded three limitations in its commit body rather than fixing them. None is a defect in what 6.1o delivered; each is a place where the honest record was "this is as far as this milestone goes". This one takes each the rest of the way.

Observable outcome: The commit boundary's contract is asserted by a compile-checked test rather than through reflection; the implementation inventory is under its size threshold by sharding rather than by losing content; and `RISK-STATE-002`'s mitigation is a check that fails rather than a sentence that is read.

Acceptance checks:

- No fixture reaches the aggregate's commit boundary by naming a type or member in a string literal. A rename breaks compilation rather than leaving a test passing against nothing.
- Granting test access does not weaken the rule that keeps Core and Application free of Unity, and this is confirmed by observation rather than asserted.
- `Project/CURRENT_IMPLEMENTATION.md` is below its warning threshold, and no content was deleted to achieve it. The split shard is registered as a canonical home in the ownership matrix, the shard index, and the routing table.
- Adding a field to `MatchAggregate` without deciding whether a rollback preserves it fails a test.
- `RISK-STATE-002` states what the enforcement catches and what it does not, and stays active.

Closing checks:

- Limitation 1: the rewritten `MatchAggregateAtomicityTests` still fails when the rejected-command comparison is disabled, re-running 6.1o's probe. A refactor that quietly stopped testing anything is the specific risk, so the pre-fix half is re-observed rather than assumed to carry over.
- Limitation 2: the living-document validator's `Project/CURRENT_IMPLEMENTATION.md` threshold warning is present before and absent after, with the `RULES_LEDGER.md` warning unchanged in both.
- Limitation 3: a deliberately unclassified field added to `MatchAggregate` is named by the classification test, and removing it restores the pass.

Non-goals: No plan-only conversion of the authorities beneath the aggregate, so `RISK-STATE-002` is not retired. No `InternalsVisibleTo` for `GuardsDigital.Core`. No rewrite of the moved section's content - it is relocated, not restated.

### [x] 6.1l Confirm the validation toolchain on a non-Windows platform

Reinstated on 2026-08-13 with explicit user approval, having previously been merged into 6.1d. It owns the one part of `F613-14` that is evidence rather than code: 6.1d makes the toolchain portable, and this milestone proves it.

**Blocked, and on what.** The confirmation requires a non-Windows host that can execute PowerShell. At the time of the 6.1d amendment the development machine had no such host: `wsl.exe` is present but reports "The Windows Subsystem for Linux is not installed", Docker is absent, and PowerShell Core is not installed. This milestone unblocks when any one of those exists. It is not blocked on a decision or on other roadmap work.

Observable outcome: Every validator script is demonstrated to run on a non-Windows platform, and the exact evidence is recorded.

Acceptance check, preserved verbatim from the former 6.1l:

- Each script is confirmed to run on Windows and on at least one non-Windows platform, and the exact confirmation evidence is recorded.

Closing check for `F613-14`: recorded successful runs of each validator script on Windows and on one non-Windows platform, with the exact commands and outputs. A run that fails on the non-Windows host is the pre-fix half, and whatever correction it forces is part of this milestone.

Once it passes, the **toolchain portability** review dimension is deleted from the prose list in [ROADMAP.md](../ROADMAP.md).

Non-goals: No new validator, no change to what any validator checks, no CI service selection or configuration, no change to the fixed parameterless launcher contract, and no Unity execution on the non-Windows host - the Editor itself is out of scope, only the PowerShell toolchain is confirmed.

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

## ER2-proposed foundation milestones (scheduled 2026-08-13)

These milestones were scheduled by a user-approved roadmap amendment on 2026-08-13 from the [ER2 external review record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2), which owns their findings, evidence, and closing checks. Scheduling is not authorization: each milestone still requires exact user approval before execution. Ordering constraints: 6.1q must complete before 6.2a, 6.1r before 6.2, and 6.1s before 6.4; 6.1t and 6.1u are unordered. 6.1r closes an architectural-direction finding, so per the mechanical-closure rule in [ROADMAP.md](../ROADMAP.md) its closure is verified by the already-scheduled fresh-task review 6.2b rather than by a named check.

### [x] 6.1q Scope Active-effect checkpoints and uniqueness correctly

Observable outcome: An Active effect scoped to a hero is visited at its own checkpoints, including its expiration checkpoint, regardless of which hero is acting, and uniqueness suppression compares the identity scope the definition declares instead of one global key namespace.

Acceptance checks:

- A Hero-scoped effect whose expiration checkpoint is `ActionEnded` ends during another hero's action end rather than outliving its window because `OrderForCheckpoint` filtered it out.
- Uniqueness suppression is scoped to declared controller/affected identity: two controllers activating the same definition coexist unless the verified rule says otherwise, and correctness does not depend on identity strings being embedded in `UniquenessKey` by convention.
- Existing lifecycle, snapshot, restore, replay, and retained-prefix Undo evidence stays green; the serialized format is unchanged or carries an explicit migrate-or-reject decision.

Closing check for `F-ER2-03`: the two focused lifecycle tests named in the ER2 record - cross-hero `ActionEnded` expiration, and same-key different-controller coexistence - observed failing at the pre-fix revision and passing at the post-fix revision.

Non-goals: No concrete card text, no modifier application, no new checkpoint kinds, no 6.2a effect execution.

### [x] 6.1r Design one board-occupancy seam for every occupant kind

Observable outcome: Heroes, minions, and generic obstacles occupy the board through one authoritative occupancy/collision seam that tokens (6.2) join by declaration, rather than each occupant kind owning a parallel collection, constructor collision block, transit scan, serializer section, and projection field.

Acceptance checks:

- One authoritative occupancy index owns per-space occupancy and the one-obstacle-per-space rule for every occupant kind; adjacent-move, reachability, attack-target, and Fast-Travel queries consume it.
- Adding an occupant kind requires no new hand-written cross-collision block in the board authority's constructor and no new per-kind occupancy scan in commit validation.
- Behavior invariance: the existing movement, attack, minion, and obstacle suites pass unchanged, and the snapshot compatibility digests are byte-identical or carry an explicit migrate-or-reject decision.

Closure for `F-ER2-06`: architectural direction; verified by the already-scheduled fresh-task review 6.2b, whose scope names this seam.

Non-goals: No token or marker behavior, no minion movement (6.1s), no rule change, no board-authority rename beyond what the seam itself requires.

### [x] 6.1s Make minion occupancy mutable and fully serialized

Observable outcome: A minion's board position is authoritative mutable state that snapshots round-trip, so Object Push (6.4), Lane Push (6.9-6.11), and minion Respawn can move minions.

Acceptance checks:

- Runtime minion board state is a Core occupancy record distinct from the immutable `QuickGameMinionSetupState`, which returns to being setup data only.
- The snapshot serializes each remaining minion's identity and current coordinate; restore no longer reconstructs positions by filtering setup placements.
- The snapshot schema advances to v7 with an explicit migrate-or-reject decision and refreshed pinned compatibility fixtures per `RISK-COMPAT-001`.
- A validated authority-internal mutation path exists for a minion's position; no player-facing command is added yet.

Closing check for `F-ER2-02`: a snapshot round-trip fixture asserting a restored match preserves a mutated minion coordinate, observed failing at the pre-fix revision (no mutation path exists) and passing at the post-fix revision.

Non-goals: No Object Push, Lane Push, Spawn, or Respawn rules; no balance change; no presentation change beyond reading the authoritative coordinate.

### [x] 6.1t Port the cross-platform dotnet test harness

Observable outcome: The Unity-free Core/Application test majority runs on any OS through the dotnet + NUnit harness preserved on branch `er2-external-review-line`, adapted to this line's assemblies and test files.

Acceptance checks:

- `Tools/DotnetTests/GuardsDigital.DotnetTests.csproj` compiles Core, CardState, Application, and the Unity-free Edit Mode test files unmodified by glob, with an explicit exclude list so a Unity-dependent newcomer breaks the harness build rather than being silently skipped.
- The project file is tracked: the blanket `*.csproj` ignore gains a scoped negation, proven by `git check-ignore` and a fresh-checkout build.
- The map-loader shim parity fixture proves the harness loader and the Unity loader produce the same 292/210/82/7/40 map inventory.
- `dotnet test` passes on the Linux workstation and on the Windows workstation.

Closing check for `F-ER2-12`: the harness's own build tripwire plus `dotnet test` green on Linux from a fresh checkout.

Non-goals: No CI service selection or configuration, no test-behavior changes, no Unity batch-mode launcher port.

### [ ] 6.1u Delete dead code and single-source duplicated resolution lookups

Observable outcome: Code with no production caller is removed, the duplicated action-definition lookup has one implementation, and a wedged Lane-Push advance names its real cause.

Acceptance checks:

- `Core/Board/AdjacentDestinationQuery.cs` and its fixture, `MovementSessionCompletion`, the void `Forward*Intent` overloads in `BoardInteractionUnityView`, and the milestone-0 `TestShell*Tests` are deleted with their `.meta` files; repository-wide search proves zero remaining references.
- `FindActionDefinition` has one shared implementation with a focused test; the three per-file copies are gone.
- Advancing end-of-round with a pending Lane-Push request rejects with a typed reason naming the unimplemented consumption checkpoint instead of `MissingRequiredEndOfRoundHandler`, covered by a focused test.

Closing check for `F-ER2-10`: compilation and the directly affected suites green after deletion, plus the shared-lookup and typed-rejection tests observed failing at the pre-fix revision and passing at the post-fix revision.

Non-goals: No behavior change beyond the rejection reason, no refactor of live code paths, no relocation of coin/modifier content (owned by 7.4).

## Validation-integrity milestones from the 6.1s Windows handoff

These milestones were scheduled by a user-approved roadmap amendment on 2026-08-13. Their source is the 6.1s Windows Unity tier and the complete-suite run that followed it, **not** the ER2 external review; do not attribute them to the ER2 record. Each closes a way the validation route can report green while a directly affected fixture was never executed.

6.1x is corrective and blocks the others: an active fixture is failing on `main`, and under [Testing/POLICY.md](../Testing/POLICY.md) an observed failure blocks the handoff. 6.1v and 6.1w are unordered against each other and against the rest of Phase 6.

### [x] 6.1x Re-pin the invariance digests to schema v7

Observable outcome: The complete Edit Mode suite is green, and `CardAggregateInvarianceTests` pins digests that express the schema the serializer actually writes.

Context: milestone 6.1s advanced `CurrentSchemaVersion` from 6 to 7 and re-pinned the five compatibility-corpus fixtures, but `CardAggregateInvarianceTests` pins nine scenario digests of its own, last set at 6.1f. That fixture is in the dotnet harness exclude list, so the Linux harness never ran it, and it was absent from the focused Windows filter, so 6.1s was marked complete without it ever executing. The complete Edit Mode suite at `4253eb9` reported 370/371 with that fixture failing.

Acceptance checks:

- The nine pinned scenario snapshot digests express schema v7, and the paired event digests are re-verified with their changed-or-unchanged status stated rather than assumed.
- The re-pin is justified before it is applied, at the byte level rather than by inference: the payloads either side of the change are decoded and diffed, and every differing byte is attributed to an intended cause. A re-pin that cannot explain its own delta is indistinguishable from pinning over a regression, which is what the fixture's own "do not update to match a failing run" comment exists to prevent.
- The complete Edit Mode suite is green at the post-fix revision, and the known-good baseline table in [Testing/PLAYMODE_AND_SUITES.md](../Testing/PLAYMODE_AND_SUITES.md) records the new totals with per-milestone delta attribution.
- The record states plainly that 6.1s was marked complete on evidence that omitted a directly affected fixture.

Closing check: `CardAggregateInvarianceTests.SixScenarios_ProduceByteIdenticalSnapshotsAndOrderedEvents` observed failing at `4253eb9` and passing at the post-fix revision, with the complete unfiltered Edit Mode suite green at that same revision.

Recorded outcome: the diff found two causes, both intended by 6.1s and neither an unintended state change. The serialized version field moved 6 to 7, and the board section gained an 8-byte coordinate per remaining minion - 96 bytes across this setup's twelve minions, accounting exactly for the payload's 1972 to 2068 byte growth. An intermediate reading that the version field alone was responsible was wrong, because it assumed these scenarios build a minion-free board; `CreateSession` uses `QuickGameSetupAuthority.Create`, which lays out the official setup's twelve minions. Reverting only the version constant therefore produced a hybrid encoding that never shipped, and the byte-level diff is what settled it.

Non-goals: No schema change, no serializer change, no new fixture, no reopening of the v7 migrate-or-reject decision, no change to what the invariance fixture asserts.

### [x] 6.1v Make reflected member names and digest fixtures checkable

Observable outcome: A test that names a production member by string, or pins a snapshot digest, cannot silently drift out of agreement with the source it describes.

Context: 6.1s renamed a private field `setupState` to `occupancy`. Three fixtures resolve that field reflectively by string name, which no compiler can check; `GetField` would have returned `null` and each test would have died on a null dereference. `StandardMinionViewTests` had its assertion updated to `.Occupancy` while its reflection string was missed, so one file disagreed with itself. The 6.1x finding is the same failure mode in a second form: a fixture whose correctness depends on the schema, selected by no rule.

Acceptance checks:

- A code-truth check in `Tools/Validate-LivingDocumentation.ps1` resolves every reflected member name in the test corpus against the declaring type's source, and fails when the member does not exist. It matches the multi-line call form, not only the single-line one.
- A site that deliberately asserts a member's absence carries an explicit source marker and is exempt; the exemption is stated in the source, never inferred.
- The check fails loudly if it finds no reflection sites at all, matching the existing vacuity guards for declared types and behaviours.
- Reflected private members resolve through a helper that fails naming the type and member, instead of returning `null`.
- Every fixture that pins a snapshot digest is named in the snapshot test route, and the recipe states that a change to `CurrentSchemaVersion` requires running all of them. An enumerated focused filter must not be able to omit one again.
- Validator self-tests cover a resolving member, a missing member, an absence-marked site, a member on a partial class declared in a second file, the multi-line form, and the vacuity guard.

Closing check: the new check run against commit `5676c64`, where the three `setupState` references were genuinely stale, observed reporting them; and run at the post-fix revision, observed passing. Historical evidence rather than a synthetic mutation, because the defect existed at that commit.

Non-goals: No change to what any test asserts, no new test assembly (a shared test-utility assembly would escalate this to T3 under the testing policy), no shrinking of the harness exclude list, no removal of reflection from tests that legitimately need it.

### [x] 6.1w Route the rules-ledger coverage sections to a shard

Observable outcome: `RULES_LEDGER.md` returns below its warning threshold with no content deleted, abbreviated, or weakened.

Context: the ledger is at 7,720 of its 9,000-word hard limit, so the validator warns and governance requires an ownership review. `Docs/AI/Rules/*.md` is already reserved at the same family threshold, so this needs no governance amendment or validator change.

Acceptance checks:

- The `Card architecture coverage` and `Effect capability coverage` sections move verbatim into an indexed `Docs/AI/Rules/` shard carrying the standard backlink line.
- `RULES_LEDGER.md` keeps a short routing summary and stable link for each moved section, never a copy of the narrative.
- The four required `RULES_LEDGER.md` headings are unaffected, and the shard is reachable from the session brief's routing table.

Closing check: the validator reports the `warning-threshold` warning for `RULES_LEDGER.md` at the pre-fix revision and reports no such warning at the post-fix revision, with the moved text byte-identical in its new home.

Non-goals: No rule content change, no re-verification of rule status, no sharding of the `Rule-system ledger` section itself, which is 5,695 words and belongs to its own later milestone.

## Documentation-structure milestone from the 6.1w handoff

Scheduled by a user-approved roadmap amendment on 2026-08-13. Its source is the growth the 6.1w handoff recorded, **not** a review. The approved decision is that hero content is owned per hero rather than held in one ledger section. 6.1y is unordered against the rest of Phase 6 and must complete before 8.1, which is the first milestone that would otherwise write hero content into `RULES_LEDGER.md`.

### [ ] 6.1y Give each hero its own rules-ledger shard

Observable outcome: Hero-specific rule authority lives in one shard per hero under `Docs/AI/Rules/`, starting with Wasp, and `RULES_LEDGER.md` keeps a routing summary and stable link rather than the content.

Context: 6.1w left the `Rule-system ledger` at 5,695 words and recorded that the ledger re-crosses its threshold as Wasp and the six heroes land. Per-hero shards take the larger half of that growth. The `Wasp content ledger` specifies an implementation-grade inventory of roughly twenty fields per card face; seven heroes of that material would exceed the 9,000-word family limit several times over, and no amount of rule-system sharding would hold it. `Docs/AI/Rules/*.md` is already reserved at the same family threshold by `Get-Limit`, so this needs no validator change and no threshold amendment.

Acceptance checks:

- The `Wasp content ledger` section moves verbatim into `Docs/AI/Rules/HERO_WASP.md` carrying the standard backlink line, and `RULES_LEDGER.md` keeps a short routing summary and stable link, never a copy of the narrative.
- The shard states the per-hero contract explicitly: one shard per approved hero, named `Rules/HERO_<NAME>.md`, owning that hero's inventory, capability mapping, source-verification status, hero-scoped deviations, and hero-scoped open questions. Rule systems that span heroes stay in the ledger's `Rule-system ledger`, and a deviation or open question that affects more than one hero stays in the ledger rather than being copied into each shard.
- The four required `RULES_LEDGER.md` headings are unaffected, and the shard is reachable from the session brief's routing table.
- Every roadmap milestone that currently routes hero content to `RULES_LEDGER.md` names the owning hero shard instead: 8.1's missing-or-ambiguous-content check and 8.18's per-hero equivalent. The first hero inventory must not be able to land in the wrong home by following its own acceptance check.

The governance ownership matrix already names `RULES_LEDGER.md` plus its indexed `Rules/` shards as the canonical home for rule authority. That row had named the ledger alone since 6.1w created `Rules/CARD_COVERAGE.md` without updating it, and was corrected on 2026-08-13 at the user's instruction, ahead of this milestone and independently of it. The threshold row already covered any future rules-ledger shard, so the correction amended no limit and this milestone inherits a matrix that admits its shards.

Closing check: the moved text is verified byte-identical against its pre-move revision from Git, no inbound link to a moved anchor breaks, and the validator passes with zero warnings at the post-fix revision. This milestone is preventive, so no failing pre-fix observation exists to record: `RULES_LEDGER.md` sits at 7,049 of 9,000 words and the validator is currently clean. The mechanical failing-then-passing rule governs review findings, and this is not one; claiming a transition here would be inventing evidence.

Non-goals: No sharding of the `Rule-system ledger` section, which remains its own later milestone. No shard for a hero without an approved roster slot - the five heroes joining Wasp are selected at 8.17. No Wasp inventory content, which is 8.1's work. No rule meaning, status, or verification change, no threshold change, and no validator change.

## Phase 6 gameplay milestones (continued)

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

ER2 note: corrective milestone 6.1q must land first so this engine does not build on mis-scoped checkpoints, and per `F-ER2-05` this milestone's design owns resolver-emitted timing checkpoints - replacing `TurnResolutionAuthority.ApplyActionTiming`'s post-hoc event-list rewriting rather than extending it. See the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2).

### [ ] 6.2b Independently verify the Phase 6 action/effect foundation

Observable outcome: A fresh-task independent review determines whether milestones 6.1 and 6.2a actually close `F58C-01` and `F58C-02` before milestone 6.3 can begin.

Acceptance checks:

- Inspect the actual final source, tests, snapshots/compatibility fixtures, replay/Undo paths, and raw validation evidence rather than relying on implementation handoff summaries.
- Trace accepted, rejected, stale, paused, restored, replayed, and Undo-truncated Minion-Battle and synthetic Skill/effect paths through the application, round/turn, action/effect, and serialization boundaries.
- Confirm mechanically that the first real end-of-round choice has no handler-local or presentation-owned continuation, and that adding the production Skill/effect handler did not deepen unrelated action-family branching.
- Classify every observation as a demonstrated defect, measured or plausible risk, or optional improvement; assign the smallest corrective owner and required validation for each actionable finding.
- Reassess the dependency-safe sequence and scope of milestones 6.3 through 6.13. Milestone 6.3 remains blocked until this review closes both findings, or the user approves a documented corrective disposition. Any corrective sequence this review schedules closes on named checks per [ROADMAP.md](../ROADMAP.md); it schedules a further verification milestone only for a finding whose closure cannot be expressed as a check.

Non-goals: No runtime, test, scene, package, assembly, content, or rule change; no implementation of a discovered correction inside the review milestone.

ER2 note: this review's scope also verifies the 6.1r unified board-occupancy seam (`F-ER2-06`), the architectural-direction finding the mechanical-closure rule assigns to fresh-task verification.

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

ER2 note: requires 6.1s (`F-ER2-02`) first - a pushed minion needs mutable, snapshot-serialized occupancy for the displacement to commit and survive restore.

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
