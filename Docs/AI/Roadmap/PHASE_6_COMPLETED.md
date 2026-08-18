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

### [x] 6.3 Add core token interactions, persistent properties, and Clear

Observable outcome: Synthetic tokens preserve their properties while supporting the general board interactions needed by the core loop.

Prerequisite: milestone 6.2b has independently closed the post-5.8c action/effect foundation findings or recorded an approved correction-plus-verification disposition.

Acceptance checks:

- Token properties are continuous sources, not ordinary card Active effects.
- Clear replaces an entire Attack and may remove any chosen subset of adjacent tokens.
- A unit spawning on a token-covered Spawn point removes that token first.
- Token lifetime is defined independently from the source hero's on-board or defeated state.

Closing check: acceptance evidence rather than a failing-then-passing defect pair. `TokenAndMarkerComponentTests` proves catalog-owned token properties project as `TokenProperty` continuous sources only while the stable token instance remains placed, never enter `ActiveEffectLifecycle`, survive restoration without the source hero on the board, and are removed before a validated unit Spawn uses their covered Spawn point. `ClearActionTests` proves the value-free, text-suppressing replacement revalidates one current replaceable Attack, accepts any adjacent-token subset including none, rejects duplicate/non-adjacent/stale selections atomically, removes exactly the chosen stable identities, and emits Clear rather than Attack facts between resolver-owned action checkpoints. `MatchSessionComponentDispatchTests` carries the typed `PerformClear` command through turn/card resolution, snapshot restore, retained-prefix Undo, and byte-identical replay.

Compatibility decision: schema remains v9. The synthetic Hazard definition moves from revision 1 to revision 2 because its trusted catalog content now owns the persistent property declaration. Existing v9 payloads naming the unavailable revision 1 are rejected rather than silently acquiring revision 2 semantics; no persisted state exists outside the test corpus. The seven compatibility-corpus hashes and nine aggregate-invariance pairs are explicitly re-pinned to that one content-registration delta, with their mutation/restore/replay equality groups unchanged.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-63-add-core-token-interactions-persistent-properties-and-clear-completion-evidence).

Non-goals: No generic `ReplaceAction` effect node, pending Clear continuation, token projection or player-facing Clear UI, Wasp definition, printed-text parsing, displacement, general Place/Spawn, Swap/Replace/Remove, end-of-round cleanup, presentation, art, animation, or audio. The Spawn work is only the mandatory token-removal pre-commit seam; milestone 6.5 still owns the complete reusable Spawn capability.

### [x] 6.5 Complete reusable Place and Spawn effects

Observable outcome: Synthetic effects use general typed Place and Spawn operations for eligible board pieces and supplies before verified hero content is introduced.

Acceptance checks:

- Place declares its source container, eligible piece type, destination query, chooser, and no-destination policy and never emits Movement or Spawn facts.
- Spawn accepts only an eligible supply object, validates the invoking rule's destination constraints, and removes a token first when a unit legally Spawns on a token-covered Spawn point.
- Unit, minion, token, supply, occupancy, obstacle, and empty-space rules compose shared queries; presentation and art do not determine eligibility.
- Atomic rejection, pending choices, typed outcomes, snapshot state, replay, and Undo are covered with synthetic definitions on the full map.
- Milestone 6.4a's minimum minion-correction Place path is migrated to or proven conformant with the completed general Place capability without changing its verified outcome.

Non-goals: No Wasp definition, hero Respawn eligibility/timing, Lane-Push minion Respawn, card-name/hero-name branch, or production presentation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65-complete-reusable-place-and-spawn-effects-completion-evidence).

### [x] 6.6 Complete reusable board-object Swap, Replace, and Remove effects

Observable outcome: Synthetic effects atomically exchange, substitute, or remove authoritative board objects through distinct reusable operations before verified hero content is introduced.

Acceptance checks:

- Swap validates both objects and resulting occupancies against one pre-effect state and emits one semantic transaction; it is neither Movement nor Place.
- Replace declares the incoming source, outgoing destination, property-transfer policy, and observable triggers and is not silently decomposed into Remove plus Spawn.
- Remove records its destination and removal/prevention outcome without Defeat rewards and remains distinct from Clear and Defeat.
- Moving, pushing, or swapping the last Battle-Zone minion never qualifies for Lane Push; a successful standalone Remove records a qualifying cause while a prevented Remove does not. Milestone 6.9 owns live request emission.
- Swap and Replace reuse milestone 6.4a's after-action correction authority whenever their completed action leaves an intrinsic minion outside the Battle Zone.
- Synthetic accepted, rejected, prevented, snapshot, replay, and Undo scenarios prove atomic state and deterministic typed outcomes.

Non-goals: No Wasp definition, card-state swap or upgrade replacement, hero Defeat consequences, specialized Respawn, card-name/hero-name branch, or production presentation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-66-complete-reusable-board-object-swap-replace-and-remove-effects-completion-evidence).

## Completed 6.8c-6.8k corrective sequence

Moved verbatim from [PHASE_6_CORRECTIONS.md](PHASE_6_CORRECTIONS.md#completed-68c-68k-corrective-sequence) on 2026-08-17. That shard states the family these belong to: a general seam repaired once rather than a symptom patched on the card that revealed it. Every one is complete.

### [x] 6.8c Give the Move-Through permission its Movement consumer

**Corrective amendment (2026-08-14, user approved).** Milestone 6.8 implemented the Move-Through permission and `ObstacleQuery`'s p.18 transit/endpoint split, then deferred the Movement consumer to "the milestone that owns route commitment" without naming one. Milestone 6.8a then printed that permission onto Red Bulwark as one of three configured modes. The result is a mode a player can select and observe nothing from: `ObstacleQuery` has no production caller, `MovementReachabilityQuery` accepts no modification set and drops every occupied neighbour before enqueuing it, and `MovementCardActionStrategyBackend` never passes the live set. This milestone closes that gap rather than leaving inert configured content in the hand.

Observable outcome: A hero holding the Move-Through permission can select and complete an ordinary Movement whose route passes through an occupied space, while no unit of either kind may end a Movement on one.

Prerequisites: Milestones 6.8 and 6.8a are complete. This milestone consumes their existing contracts and adds no new modification family.

Acceptance checks:

- `MovementReachabilityQuery` takes the authoritative modification set and the moving unit, and consults `ObstacleQuery` for both questions it already separates: an occupied space is traversable only for a mover holding `MoveThroughObstacles`, and is never emitted as a destination for any mover. A space outside the playable board keeps blocking both, permission or not.
- Passing through an occupied space costs the same movement as any other space entered, so the permission changes which routes exist rather than what they cost.
- Route commitment asserts occupancy only at the endpoint: a validated route moves its unit from origin to destination without any intermediate space being occupied, and the one-obstacle-per-space index is never transiently violated.
- The existing fork-based whole-route validation still refuses a route before any mutation, and a rejected route leaves board, session, card, continuation, and event state untouched.
- Without the permission every current Movement result is unchanged, proved against the same fixtures rather than asserted.
- Red Bulwark's `Next Turn` mode is the configured consumer: after that mode resolves, the hero's next turn offers a destination beyond an occupied space, completes it, and the offer disappears again once the effect expires.
- Projections and passive presentation forward the changed destination set without presentation deriving obstacle rules; destination highlights follow the projected legal endpoints.
- Snapshot restore, replay, retained-prefix Undo, and rejection rollback stay deterministic across a through-route commit.

Non-goals: No Object Push, direct Move, Place, or Swap displacement (6.4-6.6 own those), no new permission kind, no Fast-Travel change, no pathfinding cost model beyond one space per space entered, no Wasp definition, and no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68c-give-the-move-through-permission-its-movement-consumer-completion-evidence).

### [x] 6.8d Give every Active effect a per-activation instance identity

**Corrective amendment (2026-08-15, user approved).** Playing a card that creates an Active effect a second time silently does nothing. Each capability composes its own instance ID from source card, node, and definition, all of which repeat on a replay; `ActiveEffectLifecycle.Create` rejects a duplicate ID including a spent instance, so each handler skips creation and reads a finished effect as "already applied". Reproduced against all three Red Bulwark modes, so it is the mechanism, not one mode: `ModificationEffectHandler` serves all five 6.8 capabilities and 6.2a's `MarkActorHandler` repeats the pattern. Blue Feint would inherit it at 6.8b.

Observable outcome: A card that creates an Active effect creates a new one every time it is played, and the same activation re-executed inside its own resolution remains a no-op.

Prerequisite: none beyond 6.8. This milestone precedes 6.8b, which would otherwise ship two more modes with the defect.

Acceptance checks:

- Active-effect instance identity is minted through one shared contract rather than composed as a string inside each capability, and both existing creators use it. The identity distinguishes activations, not merely definitions.
- Two activations of the same mode from the same card in different rounds produce two distinct instances, and the second is not suppressed by the first's spent instance. Proved for every affected capability rather than one representative.
- Re-executing one activation inside its own resolution stays a legal no-op, and the lifecycle's existing live-duplicate suppression by declared scope identity is unchanged.
- A conformance rule proves no capability composes an Active-effect ID outside the shared contract, shown against a deliberate violator and a compliant twin.
- Instance IDs cross the snapshot boundary, so any change to their shape carries an explicit migrate-or-reject decision, and the pinned corpus and aggregate-invariance fixtures are observed before being re-pinned.
- Snapshot restore, replay, retained-prefix Undo, and simultaneous ordering stay deterministic across a second activation.

Non-goals: No new capability, effect family, duration, or rules content; no change to what any effect does; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68d-give-every-active-effect-a-per-activation-instance-identity-completion-evidence).

### [x] 6.8e Classify terrain as a passable obstacle rather than as off-board

**Corrective amendment (2026-08-15, user approved).** Rulebook p.18 makes terrain an obstacle like a unit or token, and a unit ignoring obstacles may enter and leave any obstacle's space while moving. The board models terrain as a space kind and the official map declares 82, but `AdjacentMoveAuthority` builds its board from playable coordinates alone, so `ObstacleQuery` cannot tell terrain from a coordinate off the map and refuses both unconditionally. The same conflation reaches Object Push at 6.4.

Observable outcome: A unit holding the Move-Through permission can complete a Movement whose route crosses a terrain space, while no unit of any kind may end a Movement on terrain and nothing may be placed there.

Prerequisite: milestone 6.8c has established route commitment and the reachability consumer this milestone extends.

Acceptance checks:

- The movement board distinguishes a terrain space, which exists and always blocks, from a coordinate that is not on the map at all. Terrain is not an occupant and does not enter the occupancy index.
- `ObstacleQuery` classifies terrain as an obstacle: it blocks every endpoint for every mover, and blocks transit only for a mover without the permission. A coordinate off the map blocks both for everyone, permission or not.
- Reachability builds a route across terrain for a permitted mover, never offers terrain as a destination, and charges the same movement for entering it as for any other space.
- Route commitment validates a terrain space for transit and refuses it as an endpoint, and a refused route mutates nothing.
- Terrain remains outside zone membership, Spawn-point eligibility, Place destinations, token placement, and every occupancy consumer; this milestone changes what may be crossed, not what may be occupied.
- Without the permission every current Movement, Fast-Travel, adjacency, reachability, and occupancy result is unchanged, proved against the same fixtures rather than asserted.
- The official map's declared terrain is the fixture for at least one case, so the rule is proved against real map data and not only a synthetic board.

Non-goals: No new terrain type, map-data change, map revision, zone rule, or placement rule; no Object Push or displacement behaviour (6.4 owns those); no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68e-classify-terrain-as-a-passable-obstacle-rather-than-as-off-board-completion-evidence).

### [x] 6.8f Require the live modification set at every rules-consuming seam

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8's review recorded that the modification parameter is optional and defaults to no modifications, so a consumer that forgets it loses rules silently and no mechanical check catches it. That is how the Move-Through permission reached a configured card with no consumer, and 6.8c added further optional parameters rather than closing the hazard.

Observable outcome: A rules-consuming entry point cannot silently resolve against no modifications; supplying none is an explicit, named decision.

Prerequisite: milestones 6.8c and 6.8d, so the movement and capability seams this rule governs are already in their corrected shape.

Acceptance checks:

- Every command and query path whose outcome a modification can change takes the live set explicitly. Where a genuinely rule-free context exists, it passes a named empty set rather than relying on an implicit default.
- A conformance rule proves no such entry point declares an implicit default, shown against a deliberate violator and a compliant twin.
- The turn authority remains the single place the live set is derived per command, and no consumer caches it across commands.
- Test and fixture call sites are updated with the set they actually mean, and no production default is retained merely to avoid touching them.
- No behaviour changes: every current result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family, query, or rules content; no change to how the live set is derived; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68f-require-the-live-modification-set-at-every-rules-consuming-seam-completion-evidence).

### [x] 6.8g Give the `Counts As` effective type its first rules consumer

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8 implemented the `Counts As` family and the deliberate split between `IntrinsicKindQuery` and `EffectiveUnitKindQuery`, then left the effective side without a production caller: `EffectiveUnitKindQuery` is reached only from `UnitPresenceQuery`, whose only callers are three assertions in `RuleModificationTests`. Every implemented rule that names a unit type - Attack target assembly, Heavy immunity, Minion Battle membership, minion supply, and displaced-minion correction - reads the intrinsic registers instead, and none of them declares that choice where a reader can see it. Milestone 6.8b would print a `Counts As` minion mode onto Blue Feint that no rule reads, which is 6.8c's defect repeated in the effective-type family.

Observable outcome: Every implemented rule that names a unit type declares whether it asks the intrinsic or the effective question, and at least one production rule consults the effective query, so a `Counts As` declaration changes a match outcome rather than only a query-level assertion.

Prerequisite: milestone 6.8f, so the seams this milestone audits already take the live modification set explicitly rather than defaulting to none.

Acceptance checks:

- Each implemented rule naming a unit type records its intrinsic-or-effective choice together with the rulebook clause that settles it. p.18's displaced-minion correction is intrinsic by explicit citation; any rule whose clause does not settle the question is recorded as an open rules question rather than answered by implementation convenience.
- At least one production rule consults `EffectiveUnitKindQuery`, and a hero holding a `Counts As` minion declaration changes that rule's outcome in a configured match rather than in a query test alone. If the audit finds that no implemented rule may ask the effective question under its own clause, this milestone reports that instead, and 6.8b's `Counts As` mode is rescheduled behind the rule that will consume it rather than shipped inert.
- Intrinsic representation, identity, ownership, occupancy kind, minion supply, and the p.18 correction exception are unchanged by any `Counts As` declaration, proved against the same fixtures rather than asserted.
- A conformance rule proves no rule naming a unit type reads a kind without going through one of the two queries, shown against a deliberate violator and a compliant twin.
- `UnitPresenceQuery` either gains its production caller here or is recorded as a declared future seam naming the milestone that will consume it; it does not remain a test-only production type by default.
- Without any `Counts As` declaration every current targeting, presence, battle, supply, and correction result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family, effective type, or unit kind; no new rule that names a unit type; no Minion Battle, Heavy, supply, or correction rule change beyond declaring which question it asks; no source-unverified official rule claim; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68g-give-the-counts-as-effective-type-its-first-rules-consumer-completion-evidence).

### [x] 6.8h Give marker attachments a per-activation instance identity

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8d fixed the Active-effect family's replay collision and recorded that the marker family carries the same latent compositional shape: the marker capability composes its instance ID as `{card}:{node}:{holder}`, with no activation anchor, so a marker given, returned to the supply, and given again to the same holder by replaying the same card composes an identical instance ID. The token family already carries round and turn in its composition (the 6.2 precedent the effect fix followed); markers are the one component family left behind.

Observable outcome: A card that gives a marker gives a fresh attachment every time it is played, including to a holder who returned that card's earlier marker, and re-executing one activation inside its own resolution remains a no-op.

Prerequisite: milestone 6.8d, whose shared-contract pattern and activation anchors this milestone extends to the marker family.

Acceptance checks:

- Marker instance identity is composed through one shared contract that distinguishes activations - the 6.8d anchor set of source card, action, node, round, and turn, plus the holder - rather than as a string inside the capability. Whether it reuses the effect contract's composition or a marker-shaped sibling is an implementation decision recorded in the commit.
- Give, return, and give again across rounds produces two distinct attachments with distinct instance IDs, and the returned instance never suppresses the later give. The failing observation records what the repeated composition does today - silent no-op or fault - rather than assuming it.
- Re-executing one activation inside its own resolution stays a legal no-op, and token instance identity is unchanged.
- A conformance rule proves no capability composes a marker instance ID outside the shared contract, shown against a deliberate violator and a compliant twin.
- Marker instance IDs cross the snapshot boundary, so any change to their shape carries an explicit migrate-or-reject decision, and the pinned corpus and aggregate-invariance fixtures are observed before being re-pinned.
- Snapshot restore, replay, and retained-prefix Undo stay deterministic across a second give.

Non-goals: No new marker rule, return condition, or holder semantics; no end-of-round or defeat return wiring (6.12 owns both); no change to what any marker does; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68h-give-marker-attachments-a-per-activation-instance-identity-completion-evidence).

### [x] 6.8i Retire the remaining optional rules parameters

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8f made the modification set a required parameter everywhere and flagged four survivors of the same hazard class, optional parameters whose omission silently changes a rules answer: `RuleModificationQuery.Build` and the `RuleModificationSet` constructor default their Tie Breaker state, which orders p.17's simultaneous modifications; `RuleModificationSet.IsImmuneTo` defaults its actor, and an omitted actor silently disables p.18's "you are never immune to your own actions" - exactly the clause 6.4's Object Push immunity consumer will depend on; and `UnitPresenceQuery.IsPresentWithin` defaults its self-exclusion.

Observable outcome: Ordering state, the acting object, and the self-exclusion are explicit call-site decisions at every rules seam; a genuinely absent value is written at the call site rather than supplied by a parameter default.

Prerequisite: milestone 6.8f, whose required-parameter regime and conformance rule this milestone completes; must precede 6.4, whose Push immunity consumer reads `IsImmuneTo`'s actor semantics.

Acceptance checks:

- All four parameters are required. Call sites state the value they mean; where a genuinely actorless or self-less context exists, the call site writes the undefined object explicitly rather than inheriting it from a default.
- The 6.8f conformance rule widens to prove no seam of the modification query surface declares an optional parameter of a rule-affecting type, shown against a deliberate violator and a compliant twin, and its subject-count guard still holds.
- The p.18 self-immunity exception keeps its existing focused coverage, and the actor is stated at every immunity call site that has one.
- The turn authority remains the single place the live set is derived per command; how the set is derived does not change.
- No behaviour changes: every current result is identical, proved against the existing fixtures and the pinned digests.

Non-goals: No new modification family or query; no immunity-consumer wiring (6.4 and 6.8b own Push immunity consumption); no change to how the live set is derived; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68i-retire-the-remaining-optional-rules-parameters-completion-evidence).

### [x] 6.8j Let a Skill complete at its own start step

**Corrective amendment (2026-08-15, user approved).** Milestone 6.8g's fixture found a latent skill-backend fault: a Skill whose effect root resolves to completion during the start itself - no choice ever opens - reads its completing identity from `step.Frame ?? pendingResolution` in `SkillCardActionStrategyBackend.Complete`, and at start-completion both are null, so the session reports an engine fault. Every configured and synthetic Skill today opens a choice, so no player can reach it, but the fault makes choice-free Skills unwritable: any future card whose Skill simply applies its text would fault at start, and the 6.8g fixture had to adopt a trailing `may` step it did not otherwise need.

Observable outcome: A Skill whose effect completes without ever pausing resolves successfully at its start, emitting the same started/node/completed fact and checkpoint stream shape as any completed Skill.

Prerequisite: none beyond 6.2a's effect frame; independent of the other corrective milestones.

Acceptance checks:

- At start-completion the completing facts read identity from the started resolution and offer rather than from a null frame, and the ordered stream keeps its shape: Action Started, the Skill started fact, node facts with their declared checkpoints, the Skill completed fact, Action Ended.
- The failing observation is a regression fixture whose Skill root completes at start - reproducing the engine fault at the pre-fix revision through the configured `MatchSession` path - and completing successfully post-fix, with the source card resolved and the turn advancing normally.
- A Skill that pauses on a choice behaves exactly as before, proved against the existing skill fixtures rather than asserted.
- Snapshot restore, replay, and retained-prefix Undo stay deterministic across a start-completing Skill.
- The pinned corpus and aggregate-invariance digests are observed unchanged.

Non-goals: No new capability, effect family, or rules content; no Repeat semantics (6.5's family owns bounded Repeat); no change to choice-opening Skills or to the 6.8g fixture's card; no Wasp definition; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68j-let-a-skill-complete-at-its-own-start-step-completion-evidence).

### [x] 6.8k Give effect definitions a typed target-selection node

**Corrective amendment (2026-08-15, user approved).** Milestone 6.4a's subject widening cannot land inside 6.4a. Direct Move must be able to name a minion or a token to displace one, and a definition has no legal way to express that today: role bindings resolve from the execution frame, which holds no board reference, so there is no actor's-space role to bind, and the coordinate identity Object Push already uses for its targets can only reach a node through a prior node's output. The missing piece is a target-selection node, which is reusable targeting machinery - milestone 6.8's declared subject - rather than displacement machinery, so the user directed it here rather than into 6.4a. 6.4a's port and reachability were already widened at `44c256e`; only the capability parameter waits on this.

This is also the seam 6.5b and 6.6a need. Every conversion whose text targets another object - Red Needle's Push, Red Bombard's direct Move of another adjacent unit, Blue Sentinel's Swap - has to name that object somehow, and each inventing its own binding is the divergence this milestone prevents.

Observable outcome: An effect definition can name another board object as a typed selection, and the displacement capabilities accept any displaceable subject through it.

Acceptance checks:

- One registered capability offers board objects as an authoritative chooser-owned selection and publishes the chosen space as a typed coordinate output, so a later node binds it through the existing node-output binding rather than a new binding kind.
- Its candidate set is declared through the reusable targeting contracts rather than effect-local geometry, and an immune object is excluded from selection while a presence question over the same spaces still counts it.
- The direct Move capability's subject parameter accepts that coordinate, so a hero, a minion, and a token are all nameable; the reachability it consults is unchanged, since the port already serves every kind.
- A token subject holds no Move-Through permission of its own and reaches the transit predicate as the named absence, because p.19 states tokens are not units.
- The definition validator refuses a subject binding whose type or source node cannot supply a coordinate, at content load rather than mid-resolution.
- Focused capability and end-to-end session scenarios cover selection, rejection of a stale or illegal selection, snapshot restore, replay, and retained-prefix Undo.

Non-goals: No displaced-minion correction (6.4a owns it), no new displacement operation, no Place/Spawn/Swap/Replace, no Wasp definition, no card-name/hero-name branch, no configured live exposure, and no production presentation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68k-give-effect-definitions-a-typed-target-selection-node-completion-evidence).

### [x] 6.1ap Emit timing checkpoints from resolver steps

Observable outcome: Timing checkpoints originate from the resolution step that raises them rather than from a post-hoc rescan of the produced event list.

Context: `TurnResolutionAuthority.ApplyActionTiming` takes the event list a card action already produced, reads each entry's `CardActionResolutionEventKind`, and splices `ActionStarted`, `BeforeAttack`, `DefenseResponse`, `AfterAttack`, and `ActionEnded` around it. Timing is therefore inferred by the turn dispatcher from what a resolver happened to emit, rather than declared by the resolver at the moment it acts. An ordered-step effect engine cannot work that way: an effect node's checkpoints are declared by its capability contract and are not recoverable from an event kind, so 6.2a would either extend the rewriter with effect-shaped arms or grow a second parallel mechanism. Both are barred by the `Execution rule`'s prohibition on a shortcut the next card milestone is expected to replace.

Acceptance checks:

- Each card-action backend emits one ordered stream of facts and checkpoints, and the turn authority materializes that stream without inspecting any fact's kind. `ApplyActionTiming` is deleted rather than extended, and no replacement branches on `CardActionResolutionEventKind`.
- A compiled-member conformance test in the 6.1k style proves checkpoint events originate from resolver step members rather than a post-hoc rescan, paired with a positive rule proving each backend actually references the checkpoint emitter so the negative rule cannot pass vacuously, and with the deliberate-violator and compliant-twin samples the existing conformance fixture requires of every rule.
- The emitted checkpoint order for Movement, Attack with a Defense response, Attack against a minion, and Fast Travel is pinned by a fixture running on a session that actually carries Active effects, and is unchanged across the correction. A fixture on the placeholder configuration alone would be vacuous, because that configuration creates no Active effect and the checkpoint call returns nothing either way.
- The Active-effect lifecycle mutation order changes deliberately and is asserted: action end now runs before source-card cancellation, which is the order [RESOLUTION_AND_EFFECTS.md](../Card-System/RESOLUTION_AND_EFFECTS.md) already specifies and the rewriter's late placement inverted.
- The wire format is unchanged, asserted rather than assumed: `CurrentSchemaVersion` stays at 7 and all fifteen pinned digests pass unmoved.

Closing check: two observed failing-then-passing transitions against genuine pre-fix defects. `ResolverEmittedCheckpointConformanceTests` at `2e47c97` reported exactly one subject, `TurnResolutionAuthority.ApplyActionTiming` reaching `CardActionResolutionEvent.get_Kind`, and reports none after. `TurnTimingCheckpointOrderTests.ActionEndCheckpoint_ReachesAnEffectBeforeItsSourceCardCancellation` was observed at `2e47c97` failing with the effect already cancelled - `Triggered@TurnStarted, Triggered@ActionStarted, Cancelled@SourceCardStateChanged` and no action-end trigger - and passing after; the four order pins and the vacuity guard in that same fixture pass at both revisions, which is what makes the pair evidence that only the lifecycle application order moved.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Non-goals: No Skill action, effect node, capability catalog, or execution frame, which 6.2a owns. No schema version, wire-format, or migration change. No new `TimingCheckpoint` member and no change to which checkpoints exist or what they mean. No change to Movement, Attack/Defense, or Fast-Travel rules behavior beyond where a checkpoint is raised, and no change to the harness exclude list.

## Phase 6 gameplay milestones (continued)

**Sequencing amendment (2026-08-14, user approved).** Milestone 6.2a completes before 6.2, and 6.2 completes before 6.2b. The source is this sequencing finding, **not** the ER2 external review and **not** review 5.8c; do not attribute it to either record. The existing 6.1r-before-6.2 and 6.1q-before-6.2a constraints are unchanged, and 6.3 stays gated on 6.2b. Scheduling is not authorization: each milestone still requires exact user approval before execution.

6.2's third acceptance check makes token placement, reclamation, and marker attachment authoritative choices, and a stable-node serializable `Choose` frame is exactly what 6.2a builds. Under the former order those choices had nowhere to live. The source holds five bespoke pending-choice families and no shared contract, `DirectorCommandKind` is a closed enum over a fixed field union, and the pending card-action discriminator is `None | Movement | Attack`. Tokens would have bought a sixth pending-choice class, new command kinds, a new projection field, and a fourth discriminator arm - four shortcuts 6.2a is chartered to replace, and the `Execution rule` in [ROADMAP.md](../ROADMAP.md) already bars a narrow milestone from introducing a shortcut the next card milestone is expected to replace. The occupancy half was never at risk: 6.1r's index admits a token occupant by declaration.

This supersedes the accepted dependency order recorded in the [post-5.8c amendment](../Archive/Phase-Handoffs/PHASE_5.md#post-58c-approved-phase-6-roadmap-amendment) - 6.1, 6.1a, 6.2, 6.2a, 6.2b, then 6.3 onward. That review judged 6.2 unblocked by `F58C-02` and was right about the board state; the ground for revisiting it is acceptance check 3, which its disposition did not weigh. The archive is append-only and is not edited, so this paragraph is where the supersession is recorded.

6.2 precedes 6.2b rather than following it because `F-ER2-06`'s wording is that tokens would add a fourth per-kind collection and markers a fifth. A review closing that finding before any token exists verifies the prediction; one running after 6.2 verifies the outcome.

Milestone IDs are unchanged and 6.2 keeps its position in numeric order below, so the ER2 disposition table, the risk register, and existing cross-references stay valid. Execution order is carried by this paragraph, as it is for the 6.1c-6.1k corrective sequence above.

**Sequencing amendment (2026-08-14, user approved).** After milestone 6.2b closes its gate, the dependency-safe gameplay order begins **6.3 -> 6.8 -> 6.4**. Milestone 6.4 consumes the targeting, presence, immunity, permission, and obstacle contracts owned by 6.8, so 6.8 is an explicit prerequisite of 6.4 rather than later work. Numeric placement below is unchanged; execution follows these prerequisite declarations. Scheduling is not authorization: each milestone still requires exact user approval before execution.

**Scheduling amendment (2026-08-14, user approved).** Review 6.2b's two proposals are scheduled. Corrective micro-milestone 6.2c closes findings `F62B-01` through `F62B-04` on the named checks in its block; it does not gate 6.3, and it must complete before milestone 6.4 begins, because `F62B-04` corrects the restore re-derivation context that 6.4's input-dependent choices would expose and `F62B-03` closes the Core-seam gap that 6.7's second caller would widen. Milestone 6.10a splits the reusable system-transition continuation contract out of 6.11 per the review's determination, sitting between 6.10's durable Battle-Zone state and the respawn transition; 6.11 keeps its observable outcome and gains the corresponding prerequisite. Every closure in this sequence is a named check or 6.2a-style acceptance evidence, so no further verification milestone is scheduled. Scheduling is not authorization: each milestone still requires exact user approval before execution.

**Mechanics-showcase scheduling amendment (2026-08-14, user approved).** Six existing configured cards become staged Skill integrations after their reusable dependencies. The exact execution order, durable hand shape, acceptance checks, and non-goals for milestones 6.8a, 6.8b, 6.5a, 6.5b, 6.6a, and 6.7a live in the indexed [mechanics-showcase subshard](PHASE_6_SHOWCASE.md). The next proposed milestone remains 6.8, and scheduling still does not authorize any showcase implementation.

### [x] 6.1ad Reject undefined enum values at the snapshot decode boundary

Scheduled and executed on 2026-08-14 from an engineering-review finding against snapshot enum decoding - **not** ER2, **not** review 5.8c, and **not** the documentation-truth finding behind 6.1z through 6.1ac. Unordered against all of them; it adds a check to the same validator script, so whichever lands second takes the next unused number.

Observable outcome: A snapshot field holding an integer that is not a declared member of its enum is refused where it is read, under its own rejection reason, and no reader in the decoder can accept one.

Context: the decoder cast every payload enum straight out of the stream at 37 sites across 21 enums. Whether an undefined value was caught depended on which constructor consumed it: about half reached an `Enum.IsDefined` guard and surfaced late as `MalformedPayload`, indistinguishable from a garbled byte stream, and the rest built invalid state and carried on. `MatchAggregate.Rollback` runs this decoder on every refused command, and [PHASE_8.md](PHASE_8.md) requires reconnect to converge or enter one explicit failure state, which a decoder that cannot say why it refused cannot support.

Acceptance checks:

- `SnapshotEnumReader.Read<T>` rejects any value that is not a declared member under the new `SnapshotLoadRejectionReason.UnrecognizedEnumValue`, naming the field, the type, and the offending value; `ReadNullable<T>` does the same behind a presence flag.
- All 37 raw casts are converted, and the `AttackTargetKind` ternary becomes a switch mirroring its writer - that ternary's `default` both produced an empty target and skipped the `ReadString` a Hero or Minion target consumes, misaligning every field after it.
- The boundary guarantees membership only. Rejecting `None`, or a member illegal in a given position, stays with the domain constructors that own those rules rather than moving rule meaning into the wire layer.
- Declared members are cached per enum type rather than routed through `Enum.IsDefined`, which is reflection-backed and boxes its argument, because this decoder runs on every refused command.
- Code-truth check 6 fails on any `(<DeclaredEnum>)reader.Read…` under `Core/Snapshots/` in the single-line, multi-line, and nullable forms, with vacuity guards for an empty decoder, an empty enum set, and a decoder containing no typed read.
- The wire format is unchanged, asserted rather than assumed: `CurrentSchemaVersion` stays at 7 and all fifteen pinned digests pass unmoved. A moved digest would mean something was written differently and the milestone was wrong.

Closing check: two observed failing-then-passing transitions against genuine pre-fix defects. Check 6 at `7c65dfa` reported all 37 casts and reports none after. `AuthoritativeSnapshotSerializerTests.Deserialize_RejectsAnUndefinedEnumValueInsteadOfRestoringIt` was observed at `7c65dfa` failing `Expected: UnrecognizedEnumValue, But was: MalformedPayload`, and passing after; it demonstrates the wrong-reason half specifically, since `TeamSide` is one of the sites a constructor did catch.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Non-goals: No schema version change, wire-format change, or migration path, and no reopening of the v7 migrate-or-reject decision. No semantic validation moved into the serializer. No terminal `else` added to `ActiveEffectDefinition.ValidateDuration` and no repair of the unvalidated `SelectedInitiativeActor` constructor: this closes the path a snapshot uses to reach them, and hardening them against in-memory callers is separate work. No change to how `MatchSession.RestoreSnapshot` flattens the typed reason, which is Phase 8 work. No change to the harness exclude list, and no documentation-truth validator work, which 6.1z through 6.1ac own.

## Resolver-timing milestone from the 2026-08-14 milestone 6.2a planning finding

**Sequencing amendment (2026-08-14, user approved).** New milestone 6.1ap completes before 6.2a. The source is the milestone 6.2a planning pass recorded here, **not** the ER2 external review and **not** review 5.8c; ER2 contributed the underlying finding `F-ER2-05`, but the decision to give it its own milestone is this pass's. Ownership of `F-ER2-05` moves from 6.2a to 6.1ap, and the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2)'s disposition row carries the new owner. Every other 6.2a prerequisite is unchanged. Scheduling is not authorization: each milestone still requires exact user approval before execution.

The ground for splitting is that the correction is behavior-preserving and 6.2a is not. `F-ER2-05` closes on its own named conformance check and its own evidence - that every pinned digest stays byte-identical and the schema version does not move. Milestone 6.2a's fifth acceptance check bumps the snapshot schema and re-pins fourteen digests. Landed together, a moved digest has two candidate causes and neither observation proves anything about the other. Landed apart, the timing correction proves the digests unmoved first, and 6.2a's re-pinning then has exactly one cause to explain. This is the `Execution rule` split test in [ROADMAP.md](../ROADMAP.md): genuinely independent risk and separate acceptance evidence, above the handoff-cost floor.

### [x] 6.1w Route the rules-ledger coverage sections to a shard

Observable outcome: `RULES_LEDGER.md` returns below its warning threshold with no content deleted, abbreviated, or weakened.

Context: the ledger is at 7,720 of its 9,000-word hard limit, so the validator warns and governance requires an ownership review. `Docs/AI/Rules/*.md` is already reserved at the same family threshold, so this needs no governance amendment or validator change.

Acceptance checks:

- The `Card architecture coverage` and `Effect capability coverage` sections move verbatim into an indexed `Docs/AI/Rules/` shard carrying the standard backlink line.
- `RULES_LEDGER.md` keeps a short routing summary and stable link for each moved section, never a copy of the narrative.
- The four required `RULES_LEDGER.md` headings are unaffected, and the shard is reachable from the session brief's routing table.

Closing check: the validator reports the `warning-threshold` warning for `RULES_LEDGER.md` at the pre-fix revision and reports no such warning at the post-fix revision, with the moved text byte-identical in its new home.

Non-goals: No rule content change, no re-verification of rule status, no sharding of the `Rule-system ledger` section itself, which is 5,695 words and belongs to its own later milestone.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61w-route-the-rules-ledger-coverage-sections-to-a-shard-completion-evidence).

## Documentation-structure milestone from the 6.1w handoff

Scheduled by a user-approved roadmap amendment on 2026-08-13. Its source is the growth the 6.1w handoff recorded, **not** a review. The approved decision is that hero content is owned per hero rather than held in one ledger section. 6.1y is unordered against the rest of Phase 6 and must complete before 8.1, which is the first milestone that would otherwise write hero content into `RULES_LEDGER.md`.

### [x] 6.1u Delete dead code and single-source duplicated resolution lookups

Observable outcome: Code with no production caller is removed, the duplicated action-definition lookup has one implementation, and a wedged Lane-Push advance names its real cause.

Acceptance checks:

- `Core/Board/AdjacentDestinationQuery.cs` and its fixture, `MovementSessionCompletion`, the void `Forward*Intent` overloads in `BoardInteractionUnityView`, and the milestone-0 `TestShell*Tests` are deleted with their `.meta` files; repository-wide search proves zero remaining references.
- `FindActionDefinition` has one shared implementation with a focused test; the three per-file copies are gone.
- Advancing end-of-round with a pending Lane-Push request rejects with a typed reason naming the unimplemented consumption checkpoint instead of `MissingRequiredEndOfRoundHandler`, covered by a focused test.

Closing check for `F-ER2-10`: compilation and the directly affected suites green after deletion, plus the shared-lookup and typed-rejection tests observed failing at the pre-fix revision and passing at the post-fix revision.

Non-goals: No behavior change beyond the rejection reason, no refactor of live code paths, no relocation of coin/modifier content (owned by 7.4).

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61u-delete-dead-code-and-single-source-duplicated-resolution-lookups-completion-evidence).

## Validation-integrity milestones from the 6.1s Windows handoff

These milestones were scheduled by a user-approved roadmap amendment on 2026-08-13. Their source is the 6.1s Windows Unity tier and the complete-suite run that followed it, **not** the ER2 external review; do not attribute them to the ER2 record. Each closes a way the validation route can report green while a directly affected fixture was never executed.

6.1x is corrective and blocks the others: an active fixture is failing on `main`, and under [Testing/POLICY.md](../Testing/POLICY.md) an observed failure blocks the handoff. 6.1v and 6.1w are unordered against each other and against the rest of Phase 6.
