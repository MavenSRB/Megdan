# Phase 6 - Completed milestone definitions

Canonical active roadmap shard holding Phase 6 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_6.md](PHASE_6.md). Historical completion evidence: [PHASE_6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md).

Standing growth pattern, established 2026-08-14: when a phase shard reaches its 85% warning threshold, its completed self-contained definition sections move here verbatim during that same handoff's required ownership review, leaving a routing summary and stable link behind. Definitions remain active authority under the same `Roadmap/` family threshold - this is sharding, not archiving. Completed milestone evidence stays in commit bodies indexed by phase-handoff rows per [DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md).
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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61c-retire-the-legacy-heroselectioncontroller-drill-completion-evidence).

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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61d-make-the-build-path-explicit-single-sourced-and-portable-completion-evidence).

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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61e-correct-editor-time-composition-hazards-completion-evidence).

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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61k-replace-brittle-structural-assertions-and-deepen-the-newest-coverage-completion-evidence).

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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61o-honour-the-atomicity-guarantee-in-every-build-and-measure-what-actually-grows-completion-evidence).

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

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61p-close-the-three-limitations-milestone-61o-declared-completion-evidence).

### [x] 6.1l Confirm the validation toolchain on a non-Windows platform

Reinstated on 2026-08-13 with explicit user approval, having previously been merged into 6.1d. It owns the one part of `F613-14` that is evidence rather than code: 6.1d makes the toolchain portable, and this milestone proves it.

**Blocked, and on what.** The confirmation requires a non-Windows host that can execute PowerShell. At the time of the 6.1d amendment the development machine had no such host: `wsl.exe` is present but reports "The Windows Subsystem for Linux is not installed", Docker is absent, and PowerShell Core is not installed. This milestone unblocks when any one of those exists. It is not blocked on a decision or on other roadmap work.

Observable outcome: Every validator script is demonstrated to run on a non-Windows platform, and the exact evidence is recorded.

Acceptance check, preserved verbatim from the former 6.1l:

- Each script is confirmed to run on Windows and on at least one non-Windows platform, and the exact confirmation evidence is recorded.

Closing check for `F613-14`: recorded successful runs of each validator script on Windows and on one non-Windows platform, with the exact commands and outputs. A run that fails on the non-Windows host is the pre-fix half, and whatever correction it forces is part of this milestone.

Once it passes, the **toolchain portability** review dimension is deleted from the prose list in [ROADMAP.md](../ROADMAP.md).

Non-goals: No new validator, no change to what any validator checks, no CI service selection or configuration, no change to the fixed parameterless launcher contract, and no Unity execution on the non-Windows host - the Editor itself is out of scope, only the PowerShell toolchain is confirmed.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61l-confirm-the-validation-toolchain-on-a-non-windows-platform-completion-evidence).

## Phase 6 gameplay milestones (continued)

The completed milestone 6.4 definition moved here verbatim from [PHASE_6.md](PHASE_6.md) on 2026-08-16 during the required warning-threshold ownership review triggered by the 6.4a correction-order amendment.

### [x] 6.4 Implement reusable direct Move and Object Push effects

Observable outcome: Synthetic effects execute the general direct Move and Object Push contracts on the full authoritative map without depending on Wasp definitions or card-name branches.

Prerequisite: milestone 6.8 has established the reusable targeting, presence, immunity, permission, and obstacle contracts consumed by this milestone's typed movement and push queries. Milestone 6.1s remains the separate mutable, snapshot-serialized minion-occupancy prerequisite.

Acceptance checks:

- Direct Move remains distinct from a Movement action, cannot become Fast Travel, and applies its exact, optional, or `up to` distance and source-relative destination policy through typed queries.
- Object Push moves each legal unit or token in a straight line directly away from its explicit pushing unit, moves as far as possible including zero when blocked, and remains distinct from Lane Push, Movement, and Place.
- An `up to X` multi-target Push records one chosen distance, validates all targets against the same pre-effect state, and commits one simultaneous batch.
- Ordinary-obstacle, Ignore Obstacles, Move Through, legal-endpoint, immunity, and target-versus-presence behavior use reusable query/modifier contracts rather than effect-local geometry.
- Moving or pushing a minion never requests a Lane Push. The effect records the displacement and exposes the after-action checkpoint needed by milestone 6.4a; no configured live action exposes an uncorrected displaced-minion endpoint.
- Focused accepted, rejected, partial, zero-distance, simultaneous, restore, replay, and Undo scenarios preserve deterministic typed outcomes and occupancy.

Non-goals: No Wasp definition, printed-text parsing, invented Pull keyword, card-name/hero-name branch, configured live action exposure before milestone 6.4a, production presentation, or specialized hero/minion Respawn transition.

ER2 note: 6.1s closes `F-ER2-02` - a pushed minion needs mutable, snapshot-serialized occupancy for the displacement to commit and survive restore.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-64-implement-reusable-direct-move-and-object-push-effects-completion-evidence).
