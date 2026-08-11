# Guards Digital - Verification and Testing

Last updated: 2026-08-11

## Scope

The Unity Test Framework shell contains one trivial Edit Mode test and one minimal Play Mode smoke test. Milestone 1.1 adds focused Edit Mode coverage for the pure C# hex-coordinate foundation. Milestone 1.2 adds Edit Mode layout, mesh, and saved-scene coverage plus a Play Mode scene-loading check. Milestone 1.3 adds primitive hero occupancy and placement coverage. Milestone 1.4 adds adjacent-empty destination query, selection highlighting, Input System pointer-path, saved-scene, and Play Mode coverage. Milestone 1.5 adds pure adjacent-move command/event authority coverage. Milestone 1.6 adds pointer-to-command hookup coverage for accepted and rejected adjacent destination flows. Milestone 1.7 expands the placeholder validation grid and adds scene coverage for radius-2 coordinates, distance coverage, alternate routes, and preserved click-to-move behavior. Milestone 1.8 adds pure movement-session coverage for zero movement, early stopping, exact allowance, and over-movement, plus reachable-destination query, controller, and Play Mode pointer coverage for one-click destination movement that records adjacent step routes. Milestone 1.9 adds pure obstacle occupancy, adjacent-move blocking, reachability detours around blocked spaces, visible placeholder obstacle views, saved-scene obstacle wiring, and Play Mode coverage. Milestone 1.10 adds pure card definition/instance coverage for stable identities, content revisions, definition references, primitive-hero ownership, orthogonal initial state facts, shared immutable definitions, and initial setup validation. Milestone 1.11 adds pure action definition, runtime offer, invocation-context vocabulary, and typed printed Movement value query coverage. Milestone 1.12 adds the pure card-action resolution shell, corrective milestone 1.12a hardens offer/continuation authority, and corrective milestone 1.12b covers immutable destination/route/cost candidates, one-command segment commitment, zero/early Stop, obstacle detours, changed-board rejection without partial mutation, restoration, and deterministic replay. Milestone 1.13 adds visible synthetic hand/card/action/pending-choice integration and the Phase 1 unfiltered gate. Corrective milestone 1.13a adds pure, presentation, and saved-scene regressions for partial segments remaining pending, cumulative waypoint routes, refreshed choices, stale prior choices, Stop with unused allowance, allowance exhaustion, and restore after committed steps, then re-runs the complete Phase 1 gate. Milestone 2.1 adds participant-collection coverage for Red/Blue team, player, and hero IDs and references, plural multi-hero team queries, the stable Phase 1 Red hero ID, distinct owned synthetic Movement hands for both test heroes, two saved-scene hero occupancies with visually distinct primitives, and Edit/Play Mode regressions proving either hero can be pointer-selected, sees only its own stable card instances, resolves Movement through the shared authority, treats the opponent as occupied, and cannot transfer a pending resolution to the other actor. Milestone 2.2 adds definition/offer/value coverage for a printed Primary non-Ranged Attack and Hand-to-Discard-Pile Defense reactions, including requested-window, owner, discarded-state, missing-owner, cross-owner, invalid-value/channel/range/origin/transition, and Movement-only presentation regressions. Milestone 2.3 adds adjacent-enemy hero target projection, stable target commands, printed Attack Total calculation, ordered Attack/Defense-request events, restorable target/response choices, stale-board and invalid-target rejection without card mutation, plus visible Attack-offer, hero-pointer target, pending-Defense prompt, saved-scene, and directly affected resolver/event-consumer regressions. Milestone 2.4 adds exactly-one-card-or-decline Defense answers, authoritative Hand-to-Discard-Pile mutation with orthogonal-fact preservation, printed Defense contribution/total and reaction-context coverage, response restoration/replay/stale rejection, ordered response events, visible legal-card/decline controls, comparison-checkpoint status, and actor/action lockout regressions. Milestone 2.5 adds equal/greater and lower Defense comparison boundaries, decline/no-legal-card success, stale comparison rejection, typed deterministic outcome/completion events, pending-Attack closure, and visible result/released-selection coverage. These tests do not mutate the attacking card, apply defeat/removal consequences, implement concrete action-start restrictions, Fast Travel, direct Move, or the complete official game loop.

Milestone 2.3a adds typed printed Range validation/query coverage, explicit axial rulebook-distance coverage, inclusive Range 1/maximum/beyond-range cases, targeting through occupied and obstacle spaces without line of sight, Ranged target/Defense continuation restoration, unchanged ordered Attack events, and visible Ranged-offer/pointer/highlight integration through the shared authority. Milestone 3.2 adds focused director-hand projection coverage for both explicit Red/Blue hand labels, authoritative instance ownership, selecting either side without lifecycle mutation or resolution, and the saved-scene Blue-hand path. Milestone 3.3 adds eligible own-side commitment, hidden Facedown state/event projection, legal no-card Pass, joint authoritative reveal, stale/repeated rejection without further mutation, deterministic replay, director confirmation/status controls, and the saved-scene reveal path. Milestone 4.2a replaces radius-generated scene coverage with selected-definition presentation checks for all 292 cells, 210 playable and 82 terrain classifications, seven distinguishable zones, zero legacy placeholder obstacles, all eight Red/Blue × Hero/Melee/Ranged/Heavy spawn-marker combinations using the reviewed circle/diamond/triangle/hexagon legend, stable definition-resolved Red/Blue test-hero spawn references, definition-derived camera framing, and unchanged applicable saved-scene interaction paths.

Run commands from PowerShell. Each focused Unity command opens the project, runs only its named test fixture or case, writes NUnit XML plus a Unity log under `tmp/`, and exits without producing a player build. Scene-dependent filters load the saved validation scene.

## Risk-tiered validation policy

Choose the highest applicable tier from the behavior and boundaries changed, not from diff size or file extension. A broad suite never replaces focused evidence.

| Tier | Applies when | Required evidence |
| --- | --- | --- |
| T0 - Documentation/source | Only Markdown, roadmap status/sequencing, source traceability, or other non-executable documentation changes | Review the diff; run `git diff --check`; verify paths/links, milestone IDs/status, cross-document references, and changed rule claims against their authority and `RULES_LEDGER.md`. Do not launch Unity. |
| T1 - Local behavior | One bounded pure-C# rule/query/validator or executable content definition changes without a Unity-facing path | Focused Edit Mode tests for the behavior and directly impacted regressions; cover valid, invalid/stale, no-op, and boundary cases as applicable. No Play Mode. |
| T2 - Boundary/integration | Shared capability/resolver/command-event/snapshot interaction changes, or a Unity scene/input/UI/presentation adapter changes | T1 evidence as applicable, dependent Edit Mode fixtures, and determinism/round-trip/ordering/source-linked scenarios where relevant. Run focused Play Mode only for affected Unity-facing paths; visually inspect only changed output. |
| T3 - Gate/broad risk | Final implementation milestone of a phase, explicit hard gate, shared package/assembly/test infrastructure change, snapshot-schema migration, cross-system loop gate, pre-build regression gate, or blast radius that cannot be enumerated | T2 evidence as applicable, then both unfiltered Edit Mode and Play Mode suites. Add gate-specific end-to-end, determinism, visual, source, and build-smoke evidence where relevant. |

During implementation, run the smallest focused filter first. Before completion, run new tests and directly affected regressions, then the selected tier's required breadth. Promote to a higher tier whenever affected consumers cannot be listed confidently. Executable JSON, ScriptableObject, card, or map data is not T0 merely because no C# changed.

Unity test processes for this project run serially. Never open parallel batchmode test instances against the same project.

### Unity process and licensing resilience

#### Required agent entry point

All agent-started Unity test validation uses the fixed, parameterless project-local launcher `Tools/Invoke-UnityValidation.ps1`. Do not issue direct `Start-Process` commands for Unity, Unity Hub, or the licensing client, and do not vary the launcher invocation between runs. This keeps every configurable input and output in the project while allowing one narrow reusable system approval for the process boundary Unity requires.

Before invoking it, write `tmp/unity-validation-request.json` with this shape:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.ExampleTests",
  "results": "tmp/example-results.xml",
  "log": "tmp/example.log"
}
```

`platform` must be `EditMode` or `PlayMode`. `filter` may be an empty string for an unfiltered suite. Both output paths must be relative paths beneath the project's `tmp` directory. Invoke the launcher with this exact command and request a persistent approval for this exact project-local entry point if no matching approval exists:

```powershell
& 'C:\Projects\Guards Digital\Guards 0.1\Tools\Invoke-UnityValidation.ps1'
```

The older command blocks below remain test-filter and historical-output references only. Agents translate the applicable values into the request file and must not execute those direct `Start-Process` examples.

The PowerShell examples below intentionally use `Start-Process -Wait -PassThru`; the calling command runner must allow at least 15 minutes for Unity, or yield the still-running command and poll it without terminating the process. Do not wrap a Unity run in a two-minute timeout. Unity licensing reconnects wait in 60-second increments, so a healthy test invocation can outlive a short command wrapper before test discovery begins.

Every Unity Editor, Unity licensing-client, and Unity Hub process used by an agent for validation must be launched outside the workspace sandbox with the tool's required elevated/normal-user process context. A sandboxed Editor can open the project while its child licensing client cannot access the user's persistent license data or create its named-pipe channel; Unity then misleadingly treats every package as unlicensed. Merely seeing an open Hub window or a live `Unity.Licensing.Client` process is not a licensing preflight.

Before the first Unity validation sequence in a task, the agent performs this preflight itself:

1. Check for active `Unity` processes. If another Unity process is using this project, monitor it and its designated outputs; do not restart Hub, licensing, or the Editor and do not start another Unity process.
2. When no Unity process is active, check for Unity Hub and `Unity.Licensing.Client`. On Windows, the installed Hub executable reports the PowerShell process name `Unity Hub` (with a space), so use `Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue`; do not use the nonexistent `UnityHub` process name. If Hub is absent, or if the current task already observed a refused/dropped licensing channel, fully restart Hub and its licensing client in the elevated/normal-user context, then wait until both `Get-Process -Name 'Unity Hub'` and `Get-Process -Name 'Unity.Licensing.Client'` return live processes. Restarting these support processes is allowed only while no Unity process is active.
3. Launch the focused Unity test in the same elevated/normal-user context. Monitor its designated log early enough to distinguish licensing startup from compilation or test execution.
4. Continue normally when the log resolves entitlements and registers a nonzero package set. For the current manifest, the healthy baseline is `Registered 39 packages`; future intentional package changes may change that count.

The agent, not the user, owns this preflight and safe recovery. Do not ask the user to leave Hub open or restart it unless the automated restart cannot produce a live Hub/licensing-client pair, sign-in or license activation requires user interaction, or the one permitted post-recovery Unity retry still fails.

If the command runner yields or reaches its own observation limit, first check the PID returned by the original launch and the run's designated log/XML files. While that Unity PID remains active, continue waiting on it; do not launch the same or another test process against this project. A command-runner timeout or yield is not Unity's exit code and is not test evidence.

One initial `Channel LicenseClient-<user> doesn't exist` message is normal when Unity launches its licensing client. `Code 10` client-signature validation or `Access token is unavailable` is also non-decisive by itself because successful local runs can recover immediately afterward. Classify startup as a licensing environment failure when the log shows repeated 60-second initialization/channel timeouts, refused or lost-client reconnect failures, `Registered 0 packages`, packages rejected as unlicensed, or missing headless entitlement and the process exits without valid fresh NUnit XML.

After confirming the failed Unity process has exited, perform the preflight's Hub/licensing restart and allow one serial Unity retry. Do not count a retry made without that recovery as the permitted retry. If the recovered retry shows the same licensing cycle, stop test retries, preserve the logs, and ask the user to verify sign-in/license state or repair the Hub/Editor installation. Never kill a live Unity test merely because no XML exists yet; Unity writes the result after test execution begins.

### Minimum by change type

| Change type | Minimum validation |
| --- | --- |
| Pure core model, rules authority, card definitions/effects/queries, or validators | T1 focused Edit Mode |
| Scene, prefab, Input System, controller, UI, or presentation hookup | T2 with relevant focused Edit Mode and Play Mode |
| Visible result | Applicable automated checks plus one targeted visual inspection of the affected view |
| Official map or card transcription | Source/manual verification plus schema/content validation; add source-linked focused scenarios when executable |
| Bug fix | An automated regression that reproduces the failure when practical, plus the tier required by the affected area |
| Player build/package | Required regression gate, then build-and-launch smoke; clean-machine smoke only at milestone 10.2 |

### Escalation triggers

Use at least T2, and T3 when the change crosses Edit/Play boundaries, for changes to:

- Authoritative state schema, snapshot serialization, migrations, Undo/restore, or deterministic replay.
- Shared command/event, resolver, pending-choice, timing, turn/round, or capability-registration infrastructure.
- Assembly definitions, packages, project settings, test infrastructure, scene bootstrap, or broadly reused saved-scene foundations.
- A refactor with unexpectedly broad diffs, an unexplained failure outside the focused fixture, or a bug with uncertain blast radius.

The tier may escalate during implementation; it is never lowered to avoid a failing relevant test.

### Explicit integration gates

The current T3 phase/hard gates are milestones `1.13`, `2.6`, `3.11`, `4.4d`, `5.8`, `6.8`, `7.9`, `8.16`, `8.27`, `8.28`, `8.36`, and `9.16`. Milestone 4.4 proves the complete configured Phase 4 setup with focused and directly affected evidence; the complete Phase 4 suites run at 4.4d after the behavior-preserving application/session, action-strategy, and MVP refactors. A corrective milestone that changes an already-passed gate boundary, such as `1.13a`, re-runs that gate's complete suites. Milestones `8.2`, `8.19`, and `8.29` must update this list if they re-baseline the Wasp, six-hero 3v3, render-pipeline migration, or private-online endpoints.

Milestone `8.28` performs a disposable internal Win64 development-build launch smoke after the URP migration. Milestone `8.36` produces a separately identified disposable private Win64 build for the invited six-human internet gate. Those artifacts are validation/playtest-only and are not publicly distributed or retained as the pitch deliverable; milestone `10.1` remains the first retained private pitch player build.

Milestone `8.29` must add the exact focused network/session and multi-client integration commands once the provider, SDK, process topology, and test harness are selected. Milestone `8.36` then runs those focused checks, a six-client integration path, the recorded multi-network private playtest, and both unfiltered Unity suites at the applicable final revision. A successful local multi-process run alone is not evidence that relay/NAT traversal, reconnect, latency handling, or internet failure behavior works.

Milestone `10.1` reuses the unchanged `9.16` result and adds build-and-launch smoke; rerun T3 if code or executable content changed after `9.16`. Milestone `10.2` runs the clean-machine private smoke test without redundantly rerunning Unity Test Framework against an unchanged build.

Risk triggers can require an earlier T2/T3 run. Passing an earlier gate never excuses a later gate after intervening changes.

## Rule and content verification

Tests prove that code matches the selected interpretation; they do not establish that the interpretation is authoritative. Before a rule-dependent milestone, verify the exact rulebook, card, or official-ruling source and update `RULES_LEDGER.md`, then add source-linked positive, negative, boundary, failure/prevention, and ordering scenarios relevant to that slice. Do not encode an unresolved ambiguity as a test expectation; block completion instead.

Rules tests use the smallest deterministic authoritative state that exposes the behavior. They assert accepted/rejected commands, exact state changes, ordered events, pending choices, and relevant checkpoints. Add serialization and repeatability cases when applicable. Tests explicitly choose Tie Breaker state unless setup-side selection itself is under test; do not introduce generalized gameplay randomness.

Verified real cards require definition/schema validation plus a scenario for every behaviorally distinct legal invocation, capability-handler conformance, and all relevant target, state, timing, modifier, replacement, repeat, Active-effect, and continuous-source interactions. Synthetic fixtures remain visibly test-only.

## Regression ownership

- For a bug fix, reproduce the failure in an automated regression test when practical, then keep the test permanently.
- Changing a shared capability runs its conformance fixture and every known directly dependent scenario; once Wasp exists, include its source-linked consumer scenarios.
- A complete suite does not compensate for a missing focused regression.

## Evidence and pass criteria

A Unity run counts only when the process exits successfully, a fresh NUnit XML exists, the requested filter discovers at least one test, the XML reports zero failures/errors and no unexpected skipped/inconclusive tests, and the log contains no compile/import exception.

Completion reports record the tier, exact focused filters, result totals, directly affected regressions, any complete-suite result, and source/visual/manual/build checks. They also record any unaffected suite intentionally omitted and why. Exact totals in historical `ROADMAP.md` completion notes are evidence from those runs, not permanent expected counts.

## Living-document governance validation

Milestone 3.11a is documentation/tooling-only T0 work. It must add and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1
```

The validator must fail when an active leaf file exceeds its approved hard word/byte routing threshold, and warn at or above 85 percent. Thresholds are per file, not an aggregate documentation cap: exact current content may move to indexed authoritative shards, and completed history may move losslessly to an indexed archive. It must also verify required headings, exactly one current/next milestone and profile route, `ROADMAP.md`/`SESSION_BRIEF.md`/`.codex/config.toml` agreement, indexed active shards and archive backlinks, mechanically checkable internal paths/anchors, migration-manifest destinations and declared integrity hashes, preservation-inventory coverage, and the absence of duplicated completion-note bodies in noncanonical active documents. The implementation must use stable text/heading rules rather than heuristic semantic rewriting and must not become passable merely by deleting an oversized section. Its own fixtures must cover valid structure, warning threshold, hard-threshold failure, missing route, mismatched profile, broken archive/shard link, duplicate historical body, missing manifest destination, bad verbatim hash, and a missing preservation-inventory item.

Manual T0 review complements the mechanical check. First preserve every governed source document unchanged under `Docs/AI/Archive/Pre-Governance/` and record its SHA-256 digest. Then compare every moved or rewritten section against that baseline and its migration-manifest entry. Confirm that milestone IDs/statuses/profiles and exact acceptance/non-goal checks; rule entries/citations/statuses/deviations/open questions; card/effect contracts; active exact recipes and gate definitions; historical results/failure evidence; and rights/privacy boundaries all remain exact in a canonical active shard or lossless archive as appropriate. Only non-authoritative overview and routing prose may be summarized. Run `git diff --check`, inspect all active/archive indexes and backlinks, verify the preservation inventory, and record before/after word/byte measurements for every governed active leaf file. Do not launch Unity.

After 3.11a, run the validator at every milestone handoff and phase-gate audit. A warning threshold requires the same handoff to review ownership: archive duplicated/completed material losslessly or shard irreducible current material authoritatively. A hard-threshold failure blocks handoff unless routing is corrected or the user explicitly approves a governance amendment and corresponding validator update; deleting or weakening precise content is never a valid correction.

## Official board-map specification validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

Expected result: the validator exits with code `0` and reports that reusable invariants and synthetic map-local identity isolation pass, followed by 292 cells, 210 playable spaces, 82 terrain cells, seven zones, 809 physical connections, 530 playable connections, and 40 spawn points split evenly between Red and Blue. The reusable layer verifies map-local `SpaceId`, `ZoneId`, and `SpawnPointId` contracts, immutable `(MapId, revision)` selection, selected-map-local coordinate queries, references, and structural invariants without official-map constants. Bounded in-memory fixtures reuse all three child IDs and their coordinate under different synthetic `MapId` values. The official layer separately verifies every reviewed count/location and the zone/space/spawn digest, including all rules-relevant zone metadata.

This is the T0 schema/content check for milestones 4.1, 4.1a, and the source half of 4.2. Milestones 4.1-4.1a do not change a Unity runtime or presentation path, so their Unity suites are intentionally omitted.

## Selected-map authoritative loading and snapshot tests

For milestone 4.2, run focused Edit Mode `BoardMapDefinitionTests;AuthoritativeSnapshotSerializerTests` and write `tmp/milestone-4-2-focused-results.xml` plus `tmp/milestone-4-2-focused.log`. Run directly affected Edit Mode `BoardMapDefinitionTests;AdjacentDestinationQueryTests;AdjacentMoveAuthorityTests;MovementReachabilityQueryTests;MovementSessionTests;CardActionResolutionAuthorityTests;AttackActionResolutionAuthorityTests;RoundResolutionAuthorityTests;TurnResolutionAuthorityTests;AuthoritativeSnapshotSerializerTests;CardDrivenMovementPresentationTests;DirectorHandPresentationTests;HeroSelectionControllerTests;MinimalHexGridSceneTests` and write `tmp/milestone-4-2-affected-results.xml` plus `tmp/milestone-4-2-affected.log`. Because the saved-scene controller now constructs its authority from the selected definition, run Play Mode `MinimalHexGridPlayModeTests` and write `tmp/milestone-4-2-play-results.xml` plus `tmp/milestone-4-2-play.log`.

Expected result: the catalog resolves exact map ID/revision pairs while bounded synthetic definitions reuse coordinates and every child ID without collision; the official resource loads 292 owned cells, 210 playable spaces, 82 terrain cells, seven zones, and 40 spawn points; the authoritative board exposes the immutable selected reference and uses only playable coordinates; snapshot schema version 2 round-trips deterministically and rejects missing map content separately from revision mismatch; Movement, Attack, turn/round, Undo/restart, event-log, controller, and saved-scene behavior remains green with the official definition selected. Milestone 4.2 T2 evidence passed focused Edit Mode 6/6, directly affected Edit Mode 138/138, and focused Play Mode 12/12, all with zero failed, skipped, or inconclusive cases. Complete unfiltered suites were intentionally omitted because this is not a phase gate or backward snapshot migration and the affected consumers were enumerated.

## Post-setup application/session boundary tests

For milestone 4.4a, add a pure `MatchSessionTests`-equivalent fixture and run it with the milestone 3.10a command-history/replay fixture, `RoundResolutionAuthorityTests`, `TurnResolutionAuthorityTests`, `CardActionResolutionAuthorityTests`, `AttackActionResolutionAuthorityTests`, `AuthoritativeSnapshotSerializerTests`, and `RuleEventLogProjectionTests`-equivalent coverage; write `tmp/milestone-4-4a-focused-results.xml` and `tmp/milestone-4-4a-focused.log`. Run the directly affected existing presentation fixtures and `MinimalHexGridPlayModeTests` only after the saved-scene adapter has been redirected through the new session port; write `tmp/milestone-4-4a-affected-results.xml`, `tmp/milestone-4-4a-affected.log`, `tmp/milestone-4-4a-play-results.xml`, and `tmp/milestone-4-4a-play.log`.

Expected result: every supported live command and retained replay command crosses the same application-service dispatcher; accepted/rejected state, ordered events, pending choices, authoritative snapshot bytes, event-log projection, Undo/restart, and immutable projection versions match the pre-refactor behavior. The pure application assembly has no Unity reference, presentation no longer coordinates low-level authorities, and a rejected command exposes no partial state or projection. Milestone 4.4a is T2 unless implementation unexpectedly broadens the scene/bootstrap or shared test-infrastructure blast radius.

## Typed card-action strategy tests

For milestone 4.4b, add handler-registry and dispatcher-contract fixtures and run them with `CardActionResolutionAuthorityTests`, `AttackActionResolutionAuthorityTests`, `MovementSessionTests`, `MovementReachabilityQueryTests`, `TurnResolutionAuthorityTests`, `RoundResolutionAuthorityTests`, the milestone 3.10a replay fixture, and `AuthoritativeSnapshotSerializerTests`; write `tmp/milestone-4-4b-focused-results.xml` and `tmp/milestone-4-4b-focused.log`. Run directly affected presentation/session fixtures only if public adapter code changes; no Play Mode is required for a pure handler extraction unless a Unity-facing path changes.

Expected result: the dispatcher preserves one active stable resolution, offer revalidation, atomic rejection, serializable pending choices, restoration validation, and event order while typed Movement and Attack/Defense handlers reproduce all accepted and rejected pre-refactor scenarios. A bounded synthetic registration fixture proves extension without dispatcher branching. Identical setup plus command sequence produces byte-identical authoritative snapshots and event-log projection before and after extraction.

## Feature MVP presentation tests

For milestone 4.4c, add pure presenter/view-port tests for board interaction, director hands/commitments, round/actions, Undo/restart, and diagnostics. Run those with `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests` or their explicit replacement fixtures, `PrimitiveHeroViewTests`, `MinimalHexGridSceneTests`, the application-session tests, and the milestone 3.10a replay tests; write `tmp/milestone-4-4c-focused-edit-results.xml` and `tmp/milestone-4-4c-focused-edit.log`. Run `MinimalHexGridPlayModeTests` for the complete saved-scene interaction path and write `tmp/milestone-4-4c-focused-play-results.xml` and `tmp/milestone-4-4c-focused-play.log`.

Expected result: passive views forward intent and render immutable view models; presenters depend only on the match-session/view ports and do not call each other; one composition root owns scene discovery/wiring; and no replacement god-object owns session, history, input, projection, and rendering. Commitment/reveal, tied Initiative, Hold, segmented Movement/Stop, Attack target/Defense, end-of-round continuation, repeated rounds, snapshot restore, Undo/restart, event log, input blocking, highlights, and visible status/layout remain behaviorally equivalent.

## Phase 4 runtime-hygiene and architecture gate

For milestone 4.4d, first run focused resource-lifecycle, unchanged-HUD projection/allocation, supported-object-discovery, malformed rule-event-log envelope, application/session, handler-registry, presenter/view, map/setup, snapshot, and replay fixtures; write `tmp/milestone-4-4d-focused-edit-results.xml` and `tmp/milestone-4-4d-focused-edit.log`. Run `MinimalHexGridPlayModeTests` and any new scene-lifecycle/performance fixture, then both complete unfiltered suites; write `tmp/milestone-4-4d-focused-play-results.xml`, `tmp/milestone-4-4d-focused-play.log`, `tmp/milestone-4-4d-complete-edit-results.xml`, `tmp/milestone-4-4d-complete-edit.log`, `tmp/milestone-4-4d-complete-play-results.xml`, and `tmp/milestone-4-4d-complete-play.log`.

Record one development-Editor Unity Profiler or `ProfilerRecorder` baseline for the complete 292-cell map and a representative two-round director flow. Report frame-time and managed-allocation observations, repeated map-rebuild/native-object counts, test environment, capture duration, and whether project-owned unchanged-HUD code creates styles, collections, definitions/offers, or labels per event. This is a regression baseline, not a production performance target.

Expected result: generated Unity/native resources are reused or destroyed through explicit ownership; repeated rebuild/lifecycle checks do not show monotonic owned-object growth; malformed diagnostic envelopes reject through the documented safe contract without partial restore; project-authored runtime and tests are clear of the known Unity 6.5 `CS0618` object-discovery warnings; focused and complete suites pass; and targeted visual inspection confirms the complete configured Phase 4 setup and validation controls remain readable. This is the final Phase 4 T3 gate; no unaffected Unity suite is omitted.

## Edit Mode shell test

```powershell
$editModeArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TestShellEditModeTests.TestRunner_CanExecuteEditModeTest',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\edit-mode-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\edit-mode-tests.log"'
)

$editModeProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $editModeArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($editModeProcess.ExitCode -ne 0) {
    throw "Edit Mode tests failed with exit code $($editModeProcess.ExitCode)."
}
```

Expected result: the shell fixture discovers at least one test, all discovered cases pass, and the process exits with code `0`.

## Hex-coordinate focused tests

```powershell
$hexCoordinateArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\hex-coordinate-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\hex-coordinate-tests.log"'
)

$hexCoordinateProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $hexCoordinateArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($hexCoordinateProcess.ExitCode -ne 0) {
    throw "Hex-coordinate tests failed with exit code $($hexCoordinateProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering all six directions, the complete unique neighbor set, and invalid-direction rejection.

## Placeholder-grid focused Edit Mode tests

```powershell
$minimalGridEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.HexWorldLayoutTests;GuardsDigital.Tests.EditMode.FlatHexMeshBuilderTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\placeholder-grid-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\placeholder-grid-edit-tests.log"'
)

$minimalGridEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $minimalGridEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($minimalGridEditProcess.ExitCode -ne 0) {
    throw "Placeholder-grid Edit Mode tests failed with exit code $($minimalGridEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering coordinate conversion, edge-to-edge center spacing, mesh geometry, expanded radius-2 scene placement, distance-1/2/3 coordinate pairs, alternate-route spaces, uniform light-gray fills, darker closed tile edges, and the fixed top-down camera.

## Primitive-hero focused Edit Mode tests

```powershell
$primitiveHeroEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.HeroOccupancyTests;GuardsDigital.Tests.EditMode.PrimitiveHeroViewTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-3-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-3-edit-tests.log"'
)

$primitiveHeroEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $primitiveHeroEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($primitiveHeroEditProcess.ExitCode -ne 0) {
    throw "Primitive-hero Edit Mode tests failed with exit code $($primitiveHeroEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering primitive hero ID validation, authoritative occupied coordinate storage, view placement from occupancy, placeholder mesh creation, and saved-scene placement.

## Hero-selection focused Edit Mode tests

```powershell
$heroSelectionEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AdjacentDestinationQueryTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-4-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-4-edit-tests.log"'
)

$heroSelectionEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $heroSelectionEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($heroSelectionEditProcess.ExitCode -ne 0) {
    throw "Hero-selection Edit Mode tests failed with exit code $($heroSelectionEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering adjacent-empty destination filtering, presentation highlighting, Input System pointer-selection components, and saved-scene wiring.

## Adjacent-move authority focused Edit Mode tests

```powershell
$adjacentMoveEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-5-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-5-edit-tests.log"'
)

$adjacentMoveEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $adjacentMoveEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($adjacentMoveEditProcess.ExitCode -ne 0) {
    throw "Adjacent-move authority Edit Mode tests failed with exit code $($adjacentMoveEditProcess.ExitCode)."
}
```

Expected result: six cases pass, covering legal adjacent movement plus non-adjacent, occupied, wrong-origin, unknown-hero, and off-board rejections. Rejected commands change no occupancy and emit no accepted movement event.

## Adjacent click-to-move focused Edit Mode tests

```powershell
$adjacentClickMoveEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-6-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-6-edit-tests.log"'
)

$adjacentClickMoveEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $adjacentClickMoveEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($adjacentClickMoveEditProcess.ExitCode -ne 0) {
    throw "Adjacent click-to-move Edit Mode tests failed with exit code $($adjacentClickMoveEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering accepted destination commits through the selection controller, rejected pointer-to-command flow, preserved selection after rejection, and the adjacent-move authority regressions.

## Movement-session focused Edit Mode tests

```powershell
$movementSessionEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-8-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-8-edit-tests.log"'
)

$movementSessionEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $movementSessionEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($movementSessionEditProcess.ExitCode -ne 0) {
    throw "Movement-session Edit Mode tests failed with exit code $($movementSessionEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering configurable allowance, zero movement, early stopping, exact allowance spending, attempted over-movement rejection, reachable-destination highlighting, deterministic route/event recording, unreachable destination preservation, and adjacent-move authority regressions.

## Obstacle-blocking focused Edit Mode tests

```powershell
$obstacleBlockingEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.PlaceholderObstacleViewTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-9-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-9-edit-tests.log"'
)

$obstacleBlockingEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $obstacleBlockingEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($obstacleBlockingEditProcess.ExitCode -ne 0) {
    throw "Obstacle-blocking Edit Mode tests failed with exit code $($obstacleBlockingEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering placeholder obstacle identity/occupancy, visible obstacle view placement, occupied-destination rejections, one-obstacle-per-space conflicts including units, reachable-destination exclusion for blockers, legal detours, insufficient allowance for detours, and saved-scene obstacle wiring.

## Card definition/instance focused Edit Mode tests

```powershell
$cardDefinitionInstanceEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-10-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-10-edit-tests.log"'
)

$cardDefinitionInstanceEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardDefinitionInstanceEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardDefinitionInstanceEditProcess.ExitCode -ne 0) {
    throw "Card definition/instance Edit Mode tests failed with exit code $($cardDefinitionInstanceEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering stable card definition IDs, content revisions, display names, definition references, validated typed Initiative and synthetic-fixture authority, unequal and tied synthetic Initiative fixtures, distinct card instance IDs, primitive-hero ownership, initial in-hand runtime state, multiple instances sharing one immutable definition, orthogonal state facts, and rejection of blank, duplicate, unresolved, stale, or invalid initial card data.

## Card turn-lifecycle foundation focused Edit Mode tests

```powershell
$cardLifecycleEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-1-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-1-focused.log"'
)

$cardLifecycleEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardLifecycleEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardLifecycleEditProcess.ExitCode -ne 0) {
    throw "Card lifecycle Edit Mode tests failed with exit code $($cardLifecycleEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering legal Facedown play, reveal, post-action resolution, and Defense discard; rejection without mutation for null, repeated, and out-of-order attempts; orthogonal resolved/discarded/orientation facts; positive typed Initiative validation; unequal and cross-hero tie fixtures; and explicit synthetic-test-fixture authority. Milestone 3.1 T1 evidence passed focused Edit Mode 19/19 and final directly affected Edit Mode 115/115 across lifecycle, definitions/offers, participants, Movement/Attack resolution, and existing presentation consumers, with zero failed, skipped, or inconclusive cases. Play Mode was intentionally omitted because no Unity-facing path changed.

## Match-participant focused Edit Mode tests

```powershell
$matchParticipantArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.HeroOccupancyTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-1-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-1-edit-tests.log"'
)

$matchParticipantProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $matchParticipantArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($matchParticipantProcess.ExitCode -ne 0) {
    throw "Match-participant Edit Mode tests failed with exit code $($matchParticipantProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering nonempty unique team/player/hero IDs, collection-backed references, one hero per player, plural multi-hero team queries, one Red and one Blue test hero, preservation of the stable `primitive-hero` ID, globally unique card instances in separately owned Red/Blue synthetic Movement hands, and the directly affected Phase 1 card/offer/hero identity regressions.

## Tie Breaker setup focused Edit Mode tests

```powershell
$tieBreakerSetupEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TieBreakerSetupTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6-focused.log"'
)

$tieBreakerSetupEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $tieBreakerSetupEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($tieBreakerSetupEditProcess.ExitCode -ne 0) {
    throw "Tie Breaker setup Edit Mode tests failed with exit code $($tieBreakerSetupEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering a normal setup result that is always Red or Blue; exact deterministic internal Red and Blue overrides; undefined-side rejection; immutable value equality suitable for later snapshot reconstruction; authoritative state carried by the synthetic configuration; explicit Tie Breaker selection in unrelated tests; and directly affected participant, offer/value, and Attack-resolution regressions. Milestone 2.6 T3 evidence passed focused affected Edit Mode 53/53, complete unfiltered Edit Mode 160/160, and complete unfiltered Play Mode 7/7, all with zero failed, skipped, or inconclusive cases.

## Visible Tie Breaker indicator focused tests

```powershell
$tieBreakerIndicatorEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6a-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6a-focused-edit.log"'
)

$tieBreakerIndicatorEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $tieBreakerIndicatorEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($tieBreakerIndicatorEditProcess.ExitCode -ne 0) {
    throw "Visible Tie Breaker indicator Edit Mode tests failed with exit code $($tieBreakerIndicatorEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering deterministic Red and Blue authoritative configurations; explicit `[R] RED` and `[B] BLUE` labels plus distinct side tinting; top-right geometry; pointer blocking over the indicator; unchanged card-driven presentation behavior; and the saved-scene controller path. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command to cover the live scene. Milestone 2.6a corrective T3 evidence passed focused Edit Mode 21/21, focused Play Mode 7/7, complete unfiltered Edit Mode 163/163, and complete unfiltered Play Mode 8/8, all with zero failed, skipped, or inconclusive cases. Readiness-gated Game-view inspection confirmed the solid Red `TIE BREAKER COIN [R] RED` indicator is readable at the top-right without overlapping the board or bottom action HUD.

## Director-mode hands focused tests

```powershell
$directorHandsEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-2-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-2-focused-edit.log"'
)

$directorHandsEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $directorHandsEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($directorHandsEditProcess.ExitCode -ne 0) {
    throw "Director-mode hands Edit Mode tests failed with exit code $($directorHandsEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly two explicit `[R] RED HERO` and `[B] BLUE HERO` hand labels, unique authoritative owner-bound instances for both synthetic hands, each visible card button's definition-backed Initiative label, director selection of either side's card, and selection without a card lifecycle transition or action resolution. A discarded card is no longer projected in its owner's hand or selectable as a hand card. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command to cover the saved-scene projection and the Blue-hand selection path. Milestone 3.2 T2 evidence passed focused Edit Mode 23/23 (`DirectorHandPresentationTests`, `CardDrivenMovementPresentationTests`, and `HeroSelectionControllerTests`) and focused Play Mode 8/8 (`MinimalHexGridPlayModeTests`), with zero failed, skipped, or inconclusive cases. The 2026-08-10 discarded-card correction and Initiative-label addition each reran the same focused filters: Edit Mode 23/23 and Play Mode 8/8, also with zero failed, skipped, or inconclusive cases. Complete unfiltered suites were omitted because this is not a T3 gate and the affected presentation/controller consumers are enumerated.

## Turn-card commitment and reveal focused tests

```powershell
$turnCardCommitmentEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-3-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-3-focused-edit.log"'
)

$turnCardCommitmentEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $turnCardCommitmentEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($turnCardCommitmentEditProcess.ExitCode -ne 0) {
    throw "Turn-card commitment Edit Mode tests failed with exit code $($turnCardCommitmentEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly one eligible own-hand commitment per Red/Blue side; Facedown played/unresolved mutation; no disclosed card ID before reveal; deterministic Red/Blue reveal events and authoritative Faceup state only after both sides respond; Pass only with no eligible card; missing, cross-side, ineligible, repeated, stale-state, and illegal-pass rejection without further mutation; deterministic replay; generic director commitment/pass labels; definition-backed revealed name/Initiative labels; and unchanged card-action/controller regressions. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command for the saved-scene joint-reveal path. Milestone 3.3 T2 evidence passed focused Edit Mode 35/35, final directly affected Edit Mode 123/123, and focused Play Mode 9/9, with zero failed, skipped, or inconclusive cases. Source/layout inspection confirmed the commitment controls and labels fit the existing bottom HUD. Complete unfiltered suites were omitted because milestone 3.3 is not a T3 gate and all affected consumers are enumerated.

## Initiative and tied-Initiative focused Edit Mode tests

```powershell
$initiativeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-4-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-4-focused-edit.log"'
)

$initiativeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $initiativeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($initiativeEditProcess.ExitCode -ne 0) {
    throw "Initiative Edit Mode tests failed with exit code $($initiativeEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering card-wide typed printed-Initiative contributions; current highest-unresolved re-query after every resolution; Red and Blue cross-team Tie Breaker selection with a recorded flip deferred until the selected card is authoritatively resolved; same-team pending choices without a flip; multi-hero ties with more than two cards; stale-choice rejection without mutation; deterministic candidate ordering; and unchanged printed action values, lifecycle, commitment, participant ownership, card definitions, and setup-side behavior. Milestone 3.4 is T2 because it introduces shared Initiative/pending-choice/turn-boundary infrastructure. Play Mode is omitted because no Unity-facing or presentation path changes. Milestone 3.4 T2 evidence passed focused and directly affected Edit Mode 62/62 with zero failed, skipped, or inconclusive cases; Play Mode and unfiltered suites were intentionally omitted because the affected pure-authority consumers are enumerated and this is not a T3 gate.

## Hold turn-resolution focused Edit Mode tests

```powershell
$holdTurnEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-5-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-5-affected.log"'
)

$holdTurnEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $holdTurnEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($holdTurnEditProcess.ExitCode -ne 0) {
    throw "Hold turn Edit Mode tests failed with exit code $($holdTurnEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering a general-rule Hold offer for any current selected face-up unresolved played card; exactly one normal Hold per selected card; Initiative-ordered resolution of both synthetic cards; resolve-before-deferred-flip-before-re-query ordering; same-team choices without a flip; rejection without mutation for wrong, repeated, or post-turn Hold attempts; defined semantic event payloads; and exactly one turn-only-effect cancellation checkpoint immediately before turn end. Milestone 3.5 is T2 because it introduces shared turn/timing infrastructure. Milestone 3.5 T2 evidence passed focused Edit Mode 8/8 and directly affected Edit Mode 70/70 with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path changed, the affected pure-authority consumers are enumerated, and milestone 3.5 is not a T3 gate.

## In-turn Movement resolution focused Edit Mode tests

```powershell
$turnMovementEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6-affected.log"'
)

$turnMovementEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $turnMovementEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($turnMovementEditProcess.ExitCode -ne 0) {
    throw "In-turn Movement Edit Mode tests failed with exit code $($turnMovementEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering offers derived only from the current face-up unresolved played card; delegation to the Phase 1 allowance, cumulative waypoint route, obstacle, adjacent-step, pending-choice, and command/event authorities; retained unresolved card/actor state during partial Movement; rejection without mutation for wrong actor, stale choice, or another normal action while Movement is pending; exact source-card resolution on Stop or allowance exhaustion; and resolve-before-deferred-flip-before-current-Initiative-re-query ordering. Milestone 3.6 is T2 because it composes shared turn and resolver infrastructure. Milestone 3.6 evidence passed focused Edit Mode 11/11 and directly affected Edit Mode 134/134 with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no scene or presentation code changed, the affected consumers are enumerated, and milestone 3.6 is not a T3 gate.

## Visible in-turn Movement corrective focused tests

```powershell
$visibleTurnMovementEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-edit.log"'
)

$visibleTurnMovementPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-play.log"'
)
```

Run each argument set through the standard `Start-Process -Wait -PassThru` Unity invocation used above. Expected result: all discovered cases pass, covering automatic current-actor/card focus after joint reveal; played-card Movement-only offers; pointer destination and Stop submission through the turn authority; partial waypoint retention and refreshed highlights; exact source-card resolution followed by visible next-actor focus; and stale input preserving the actor, card, pending choice, occupancy, and highlights. Corrective milestone 3.6a is T2 because it changes the validation controller and saved-scene presentation boundary. Evidence passed focused Edit Mode 27/27, directly affected Edit Mode 149/149, and saved-scene Play Mode 9/9 with zero failed, skipped, or inconclusive cases. Targeted live Game-view inspection confirmed the readable `[R] RED` Initiative prompt, Movement buttons, legal highlights, and Stop control without overlap. Complete unfiltered suites were intentionally omitted because the affected consumers are enumerated and milestone 3.6a is not a T3 gate.

## In-turn Attack and Defense focused tests

```powershell
$turnAttackEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-affected.log"'
)

$turnAttackPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-focused-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-focused-play.log"'
)
```

Run each argument set through the standard `Start-Process -Wait -PassThru` Unity invocation used above. Expected result: all discovered cases pass, covering played-card Attack offer revalidation; target, Defense, comparison, and outcome continuation inside the same turn frame; the defender's reaction leaving its played turn card unresolved and actionable; exact attacking-card resolution; resolve-before-deferred-flip-before-current-Initiative-re-query ordering; stale/forged input preservation; visible target/Defense/outcome/next-actor projection; and the saved-scene pointer path. Milestone 3.7 is T2 because it composes the shared turn/resolver/pending-choice boundary and changes the validation controller. Evidence passed focused `TurnResolutionAuthorityTests` 13/13, directly affected Edit Mode 99/99, and saved-scene Play Mode 10/10 with zero failed, skipped, or inconclusive cases. Source/layout inspection confirmed the existing Attack, Defense, outcome, and next-actor HUD states reuse the already-validated panel geometry. An automated batch framebuffer capture was unavailable, and a hidden live launcher capture blocked at its interactive save boundary, so no screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because the affected consumers are enumerated and milestone 3.7 is not a T3 gate.

## Five-card synthetic hands focused tests

Use the milestone 3.7 directly affected Edit Mode filter above, adding `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `TurnCardCommitmentAuthorityTests`, `InitiativeResolutionAuthorityTests`, `DirectorHandPresentationTests`, and `MinimalHexGridSceneTests`; write results to `tmp/milestone-3-7b-final-affected-edit-results.xml`. Run `MinimalHexGridPlayModeTests` with the existing saved-scene Play Mode command and write results to `tmp/milestone-3-7b-final-play-results.xml`.

Expected result: all ten synthetic definition/value profiles match the approved matrix; each hero owns five globally unique in-hand instances; every slot can commit and exposes its Primary Attack, Secondary Movement, and general Hold when revealed; the Initiative 11 cross-team tie remains; Defense discard removes only the chosen instance from later commitment/reaction choices; representative non-Ranged and Ranged turn actions resolve; and both five-card hands remain usable in the bounded director HUD. Milestone 3.7b is T2 because executable card data and its shared controller/presentation consumers change. Evidence passed focused final Edit Mode 20/20, directly affected Edit Mode 150/150, and saved-scene Play Mode 10/10 with zero failed, skipped, or inconclusive cases. A follow-up live Game-view inspection by the user confirmed that both Red and Blue hands visibly show five cards; automated screenshot attempts were not retained because their capture launchers did not produce a reliable saved-scene image. Source/layout inspection confirms five minimum-width hand buttons and five minimum-width Defense buttons fit their dedicated bounded rows at the QHD validation view. Complete unfiltered suites were intentionally omitted because the affected consumers are enumerated and milestone 3.7b is not a T3 gate.

## Four-turn round skeleton focused Edit Mode tests

Run `RoundResolutionAuthorityTests` alone and write its results/log to `tmp/milestone-3-8-focused-results.xml` and `tmp/milestone-3-8-focused.log`. Then run this directly affected Edit Mode filter and write its outputs to `tmp/milestone-3-8-affected-results.xml` and `tmp/milestone-3-8-affected.log`:

```text
GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;
GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;
GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;
GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;
GuardsDigital.Tests.EditMode.MatchParticipantsTests;
GuardsDigital.Tests.EditMode.TieBreakerSetupTests;
GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.MovementSessionTests;
GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;
GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;
GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;
GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests
```

Pass the semicolon-joined value above as the standard Edit Mode command's `-testFilter`. Expected result: exactly four deterministic indexed turns record immutable Red/Blue card-or-Pass slots; resolved or otherwise ineligible cards cannot replay; Pass is accepted only when no eligible card remains; Hold, Movement, Attack/Defense, pending choices, and deferred Tie Breaker state compose through successive turns; and turn 4 enters an explicit end-of-round phase without retrieving cards or running deferred subsystems. Milestone 3.8 is T2 because it adds shared round/turn/event infrastructure. Evidence passed focused Edit Mode 7/7 and directly affected Edit Mode 147/147 with zero failed, skipped, or inconclusive cases. Play Mode and complete unfiltered suites were intentionally omitted because no Unity-facing path changed, the affected pure-authority consumers are enumerated, and milestone 3.8 is not a T3 gate.

## Complete visible round controller focused tests

Run `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, and `RoundResolutionAuthorityTests` together and write their outputs to `tmp/milestone-3-8a-focused-edit-results.xml` and `tmp/milestone-3-8a-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.8 filter above and add `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-8a-affected-edit-results.xml` and `tmp/milestone-3-8a-affected-edit.log`. Run `MinimalHexGridPlayModeTests` through the saved-scene Play Mode command and write outputs to `tmp/milestone-3-8a-focused-play-results.xml` and `tmp/milestone-3-8a-focused-play.log`.

Expected result: the controller retains one authoritative round across hero-focus changes and all four indexed turns; commitment/Pass, reveal, Hold, Movement, Attack/Defense, pending answers, stale rejection, deferred Tie Breaker flips, hand depletion, completed slots, and turn advancement all route through it; and the saved scene visibly projects round/turn/phase plus explicit end-of-round while post-round commands preserve state. Milestone 3.8a is T2 because it changes the shared validation controller and saved-scene presentation boundary but is not the Phase 3 T3 gate. Evidence passed focused Edit Mode 25/25, directly affected Edit Mode 186/186, and saved-scene Play Mode 11/11 with zero failed, skipped, or inconclusive cases. Source/layout inspection confirmed the expanded phase header, status line, and Hold control remain within the existing bounded HUD. The batch path did not provide a reliable framebuffer capture, so no screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because the affected consumers are enumerated and milestone 3.8a is not a T3 gate.

## Collection-backed round assignments focused Edit Mode tests

Run `RoundResolutionAuthorityTests` alone and write its outputs to `tmp/milestone-3-8b-focused-results.xml` and `tmp/milestone-3-8b-focused.log`. Then run `RoundResolutionAuthorityTests`, `TurnResolutionAuthorityTests`, `InitiativeResolutionAuthorityTests`, `TurnCardCommitmentAuthorityTests`, `CardLifecycleAuthorityTests`, `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `CardDrivenMovementPresentationTests`, and `DirectorHandPresentationTests` together; write outputs to `tmp/milestone-3-8b-affected-results.xml` and `tmp/milestone-3-8b-affected.log`.

Expected result: commitment and Pass are owned by canonical `HeroId` rather than team side; for every configured hero, its controlling player must commit exactly one hero-owned card from a nonempty hand, only an empty hand permits Pass, off-board state does not relax that obligation, invalid in-hand lifecycle state rejects rather than authorizing Pass, and joint reveal waits for every configured hero response. Every turn slot exposes an immutable deterministically ordered collection of ownership-validated `HeroId`/committed-card-or-empty-hand-Pass assignments; controlling `PlayerId` and team derive through `MatchParticipants` and are not duplicated as assignment inputs; malformed one-player-to-one-hero relationships plus unknown, duplicate, missing, and cross-owner entries reject. The staged fixture records exactly two assignments in each of four slots, and Initiative, action, Tie Breaker, event, controller, and end-of-round behavior remains unchanged. Milestone 3.8b is T2 because it changes shared commitment/round/event state immediately before snapshot serialization. Play Mode is omitted unless implementation unexpectedly changes a Unity-facing path; the planned presentation remains a read-only projection. Complete unfiltered suites remain reserved for the Phase 3 gate at milestone 3.11 unless a risk trigger promotes the change.

Milestone 3.8b evidence passed focused Edit Mode 7/7 and directly affected Edit Mode 82/82 with zero failed, skipped, or inconclusive cases. The final runs registered the healthy 39-package baseline. Play Mode was intentionally omitted because no scene, input, layout, or presentation behavior changed; the existing controller adapter only translates each current 1v1 team hand to its canonical hero ID. Complete unfiltered suites remain reserved for milestone 3.11.

## Repeated-round and end-of-round progression tests

Run `RoundResolutionAuthorityTests` and `CardLifecycleAuthorityTests` together and write their outputs to `tmp/milestone-3-8c-focused-results.xml` and `tmp/milestone-3-8c-focused.log`. Run `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `RoundResolutionAuthorityTests`, and `CardLifecycleAuthorityTests` together and write their outputs to `tmp/milestone-3-8c-focused-edit-results.xml` and `tmp/milestone-3-8c-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.8 filter above and add `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-8c-affected-edit-results.xml` and `tmp/milestone-3-8c-affected-edit.log`. Run `MinimalHexGridPlayModeTests` through the saved-scene Play Mode command and write outputs to `tmp/milestone-3-8c-focused-play-results.xml` and `tmp/milestone-3-8c-focused-play.log`.

Expected result: one authoritative resumable cursor records the ordered Minion Battle, token/marker cleanup, requested Lane Push, retrieval/cancellation, Level Up/upgrade, and Pity Coin checkpoints. Explicit fixture capability facts produce typed `NotApplicable`; an available stage without a handler rejects; a paused handler preserves its cursor. The populated retrieval stage returns only owner-validated resolved played and Defense-discarded synthetic cards through lifecycle events and emits the Active-effect cancellation checkpoint. Completion freezes immutable slots and terminal outcomes, resets round-local state, retains Tie Breaker, and begins the next commitment. The saved scene completes at least two deterministic rounds with Hold, Movement, Attack/Defense, Defense-driven discard/retrieval, and legal card reuse. Milestone 3.8c is T2 because it changes shared round/event state and the validation controller, but it is not the Phase 3 T3 gate.

Milestone 3.8c evidence passed focused pure Edit Mode 17/17, focused integrated Edit Mode 36/36, directly affected Edit Mode 193/193, and saved-scene Play Mode 11/11 with zero failed, skipped, or inconclusive cases. The final runs registered the healthy 39-package baseline. Source/layout inspection confirmed the continuation control shares the existing action row inside the bounded HUD; the batch path did not provide a reliable framebuffer capture, so no screenshot evidence is claimed. Complete unfiltered suites were intentionally omitted because the affected consumers are enumerated and milestone 3.11 remains the Phase 3 gate.

## Authoritative snapshot serialization and restore tests

Run `AuthoritativeSnapshotSerializerTests` and `CardDrivenMovementPresentationTests` together and write outputs to `tmp/milestone-3-9-focused-results.xml` and `tmp/milestone-3-9-focused.log`. For directly affected Edit Mode coverage, run `AuthoritativeSnapshotSerializerTests`, `RoundResolutionAuthorityTests`, `TurnResolutionAuthorityTests`, `InitiativeResolutionAuthorityTests`, `TurnCardCommitmentAuthorityTests`, `CardLifecycleAuthorityTests`, `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `CardActionResolutionAuthorityTests`, `AttackActionResolutionAuthorityTests`, `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-9-affected-results.xml` and `tmp/milestone-3-9-affected.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-9-play-results.xml` and `tmp/milestone-3-9-play.log`.

Expected result: deterministic schema-versioned round trips preserve participants, the enforced one-player/one-hero relationship, Tie Breaker/Initiative state, current and completed collection-backed round assignments, end-of-round cursor/outcomes, board/obstacle occupancy, definition references, every card-state facet, partial commitments, and pending Movement or Attack/Defense continuations. Unchanged restored state serializes identically; schema and definition content-revision mismatches reject with distinct reasons. Resumed target and Defense commands produce the same ordered events, and the validation controller rebuilds restored hands, occupancies, pending highlights, and status from authority.

Milestone 3.9 T2 evidence passed focused Edit Mode 21/21, directly affected Edit Mode 154/154, and saved-scene Play Mode 11/11 with zero failed, skipped, or inconclusive cases. The healthy 39-package baseline was registered. Complete unfiltered suites were intentionally omitted because this introduces schema version 1 rather than migrating an existing schema, affected consumers are enumerated, and milestone 3.11 remains the Phase 3 T3 gate.

## Historical snapshot-backed Director Undo and restart tests

Run `CardDrivenMovementPresentationTests` and `AuthoritativeSnapshotSerializerTests` together and write outputs to `tmp/milestone-3-10-focused-edit-results.xml` and `tmp/milestone-3-10-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter above and write outputs to `tmp/milestone-3-10-affected-edit-results.xml` and `tmp/milestone-3-10-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-10-focused-play-results.xml` and `tmp/milestone-3-10-focused-play.log`.

Expected result: every accepted director commitment, Pass, Initiative choice, Hold, action start, pending answer, and end-of-round continuation records its exact pre-command authoritative snapshot; rejected and presentation-only inputs add no entry. Undo pops and restores that boundary without recording itself, restart restores the initial scenario and clears history, and restored card/round/Tie Breaker/pending/board state rebuilds active actor/card focus, hero/obstacle views, pending highlights, Defense controls, and the compact enabled/disabled HUD buttons. Milestone 3.10 T2 evidence passed focused Edit Mode 23/23, directly affected Edit Mode 156/156, and saved-scene Play Mode 12/12 with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed `UNDO` and `RESTART` share the existing header row without increasing the bounded panel height. Complete unfiltered suites were intentionally omitted because milestone 3.11 remains the Phase 3 T3 gate and the affected snapshot/authority/controller/presentation consumers are enumerated.

The preceding result is historical evidence for the currently implemented milestone 3.10 behavior. Corrective milestone 3.10a supersedes its Undo-history target without invalidating snapshot serialization tests.

## Deterministic command-replay Director Undo corrective tests

For milestone 3.10a, run `CardDrivenMovementPresentationTests` plus the new pure command-history/replay fixture and `AuthoritativeSnapshotSerializerTests` together; write outputs to `tmp/milestone-3-10a-focused-edit-results.xml` and `tmp/milestone-3-10a-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter above plus every new command-envelope, initial-scenario, replay, and event-log fixture; write outputs to `tmp/milestone-3-10a-affected-edit-results.xml` and `tmp/milestone-3-10a-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-10a-focused-play-results.xml` and `tmp/milestone-3-10a-focused-play.log`.

Expected result: accepted typed commands and explicit choices append exactly once; rejected and presentation-only inputs append nothing. Reconstructing from immutable initial scenario inputs and replaying every retained command prefix through normal authority paths produces the same serialized authoritative state and ordered event projection as live execution across commitment/reveal, Initiative, Hold, multi-step Movement/Stop, Attack target/Defense, end-of-round continuation/retrieval, and repeated-round entry. Undo truncates one command and atomically exposes the reconstructed prefix; restart clears the command log and reconstructs setup. Replay rejection or a deterministic state/event mismatch exposes no partial rebuild and reports a clear failure. No per-command authoritative snapshot is retained, while standalone snapshot capture/restore regressions remain green.

## Internal rule-event log and Phase 3 T3 gate

Run `CardDrivenMovementPresentationTests` and `AuthoritativeSnapshotSerializerTests` together and write outputs to `tmp/milestone-3-11-focused-edit-final-results.xml` and `tmp/milestone-3-11-focused-edit-final.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter and add the current controller/presentation consumers; write outputs to `tmp/milestone-3-11-affected-edit-results.xml` and `tmp/milestone-3-11-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-11-focused-play-results.xml` and `tmp/milestone-3-11-focused-play.log`. Finally run both unfiltered suites through the fixed launcher with empty filters and write outputs to `tmp/milestone-3-11-complete-edit-results.xml`, `tmp/milestone-3-11-complete-edit.log`, `tmp/milestone-3-11-complete-play-results.xml`, and `tmp/milestone-3-11-complete-play.log`.

Expected result: accepted ordered setup, commitment, Initiative, turn, action, and end-of-round events project to contiguous immutable rows containing semantic type, stable rules-object references, and diagnostic values. Facedown commitment rows do not reveal card identity; rejected commands remain outside the accepted log. Snapshot restore and Undo reproduce the same projection, restart returns to setup rows, expanding/collapsing the panel does not alter the authoritative snapshot, and expanded bounds block board input. Milestone 3.11 evidence passed focused Edit Mode 24/24, directly affected Edit Mode 157/157, saved-scene Play Mode 12/12, complete unfiltered Edit Mode 228/228, and complete unfiltered Play Mode 13/13 with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed the scrollable panel's right-side and above-HUD fallback bounds; two one-off batch framebuffer attempts produced no image because batch mode exposed no capturable end-of-frame/framebuffer, so no screenshot evidence is claimed. No unaffected suite was omitted from the final T3 gate.

## Action-offer and printed-value focused Edit Mode tests

```powershell
$actionOfferValueEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-2-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-2-focused.log"'
)

$actionOfferValueEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $actionOfferValueEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($actionOfferValueEditProcess.ExitCode -ne 0) {
    throw "Action-offer and printed-value Edit Mode tests failed with exit code $($actionOfferValueEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering ordered immutable action definitions; printed Primary/Secondary Movement plus printed Primary non-Ranged and Ranged Attack offers; Hand-to-Discard-Pile Defense reaction declarations; actor/source/window binding; typed Movement, Attack, Defense, and Range printed-value contributions/totals; discarded/cross-owner/missing-owner rejection; invalid value/channel/range/origin/transition rejection; unresolved offer references; and the directly affected card definition/instance regressions.

## Basic adjacent hero Attack focused Edit Mode tests

```powershell
$attackInitiationEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3-all-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3-all-affected.log"'
)

$attackInitiationEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $attackInitiationEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($attackInitiationEditProcess.ExitCode -ne 0) {
    throw "Basic adjacent hero Attack Edit Mode tests failed with exit code $($attackInitiationEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering only-adjacent-enemy hero target projection; self/friendly/non-adjacent/empty/terrain/token/non-unit-obstacle exclusion; stable target commands; stale-board, forged-offer, no-target, and overlapping-resolution rejection without card mutation; typed printed Attack contribution/total; ordered Attack-start/value/Defense-request events; defender-owned eligible reaction projection; exact pending-choice restoration; deterministic replay; visible Attack offer, exact legal-target highlighting, highlight clearing after acceptance, and pointer target submission; and directly affected Movement, offer/value, participant, occupancy, resolver, event-consumer, controller, and scene regressions. The milestone 2.3 presentation correction is T2: its initial final affected Edit Mode filter passed 71/71, the target-highlight regression filter passed 43/43 (`CardDrivenMovementPresentationTests`, `AttackActionResolutionAuthorityTests`, `CardActionResolutionAuthorityTests`, and `HeroSelectionControllerTests`), focused Play Mode passed 6/6 (`MinimalHexGridPlayModeTests`), and targeted visual inspection confirmed both a readable `PRIMARY ATTACK 3` control and a strong yellow legal-target highlight. Complete unfiltered suites remain unnecessary because this is not a T3 gate and the affected consumers are enumerated.

## Basic Ranged hero Attack focused Edit Mode tests

```powershell
$rangedAttackEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3a-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3a-final-affected.log"'
)

$rangedAttackEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $rangedAttackEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($rangedAttackEditProcess.ExitCode -ne 0) {
    throw "Basic Ranged hero Attack Edit Mode tests failed with exit code $($rangedAttackEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering typed Ranged Attack/Range definition shape and offer-derived totals; Range 1 adjacency; inclusive maximum and beyond-Range rejection; explicit axial distance through occupied/obstacle spaces without line of sight; self/friendly/non-unit exclusions; forged/stale/invalid attempts without mutation; shared target-command, ordered Attack events, defender-owned response, restoration, and deterministic boundaries for both range classifications; visible Ranged offer/status/highlight/pointer integration; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.3a T2 evidence passed final focused Edit Mode 36/36, final affected Edit Mode 114/114, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero skipped or inconclusive cases. Targeted visual inspection confirmed `PRIMARY RANGED ATTACK 2 RANGE 2`, its status text, and the strong yellow legal-target highlight render without clipping or overlap. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected consumers are enumerated.

## Basic Defense response focused Edit Mode tests

```powershell
$defenseResponseEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-4-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-4-final-affected.log"'
)

$defenseResponseEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $defenseResponseEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($defenseResponseEditProcess.ExitCode -ne 0) {
    throw "Basic Defense response Edit Mode tests failed with exit code $($defenseResponseEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly one currently legal defender-owned in-hand Defense selection or decline; Hand-to-Discard-Pile mutation with other state facts preserved; distinct reaction invocation context; printed Defense contribution/total only on discard; no total on decline or no legal card; missing, multiple, cross-owner, attacker-selected, discarded, stale, forged, and replayed answer rejection without mutation; restoration at both response and comparison checkpoints; ordered discard/total-or-decline/completion events; visible legal Defense/Decline controls and comparison-pending status; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.4 T2 evidence passed authority-focused Edit Mode 17/17, final affected Edit Mode 120/120, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases. Targeted visual inspection confirmed readable Defense controls/status, removal of the discarded choice, and clean advancement to the milestone 2.5 comparison checkpoint. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and all affected consumers are enumerated.

## Basic Attack outcome focused Edit Mode tests

```powershell
$attackOutcomeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-5-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-5-final-affected.log"'
)

$attackOutcomeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $attackOutcomeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($attackOutcomeEditProcess.ExitCode -ne 0) {
    throw "Basic Attack outcome Edit Mode tests failed with exit code $($attackOutcomeEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering equal and greater Defense as successfully defended; lower Defense, decline, and no legal Defense as attack successful; stable same-resolution continuation; comparison revalidation and stale-state rejection without closing the Attack; typed outcome/completion events with actor, target, totals, and Defense source; deterministic replay; visible result text; pending-Attack closure; released card/actor selection; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.5 T2 evidence passed authority-focused Edit Mode 23/23, final affected Edit Mode 126/126, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases. Targeted visual inspection confirmed `Attack 3 vs Defense 3: successfully defended` is readable after the discarded card leaves the hand and the Attack closes. Complete unfiltered suites were intentionally omitted because milestone 2.5 is not the Phase 2 T3 gate and all affected consumers are enumerated.

## Card-foundation authority hardening focused Edit Mode tests

```powershell
$cardFoundationHardeningArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\card-foundation-hardening-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\card-foundation-hardening.log"'
)

$cardFoundationHardeningProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardFoundationHardeningArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardFoundationHardeningProcess.ExitCode -ne 0) {
    throw "Card-foundation hardening Edit Mode tests failed with exit code $($cardFoundationHardeningProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering externally immutable definition/value/choice/continuation collections, strict origin/policy validation, timing-window-scoped offer projection, authoritative forged/stale offer rejection, structured missing-actor rejection, exact pending Movement continuation data, restoration through a fresh authority, deterministic choice resumption, and the directly affected definition, value, movement, reachability, and adjacent-move regressions.

## Card-resolution Movement focused Edit Mode tests

```powershell
$oneClickCardMovementArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-12b-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-12b-edit.log"'
)

$oneClickCardMovementProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $oneClickCardMovementArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($oneClickCardMovementProcess.ExitCode -ne 0) {
    throw "One-click card-resolution Movement Edit Mode tests failed with exit code $($oneClickCardMovementProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering immutable and structurally valid destination/route/cost candidates, typed Primary/Secondary allowance, partial segments that continue inside one resolution, cumulative waypoint routes, Stop with unused allowance, automatic exhaustion, ordered internal step/choice events, obstacle exclusion and legal detours, stale/invalid/changed-board rejection without unintended partial actor movement, exact restored choices after zero or more committed steps, deterministic replay, and the directly affected card, movement, reachability, adjacent-authority, and controller regressions.

## Visible card-driven Movement focused Edit Mode tests

```powershell
$visibleCardMovementArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-13-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-13-focused-edit.log"'
)

$visibleCardMovementProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $visibleCardMovementArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($visibleCardMovementProcess.ExitCode -ne 0) {
    throw "Visible card-driven Movement Edit Mode tests failed with exit code $($visibleCardMovementProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering two visibly test-only hand cards; separate card/action selection; typed Primary Movement 3 and Secondary Movement 1 allowances; explicit Primary/Secondary text policy; exact pending destination/route/cost projection; partial waypoint continuation with refreshed highlights; zero and explicit early Stop; stale UI-choice rejection; blocked destinations and legal obstacle detours; pointer-submitted destination commands; retained selection while pending; cleared selection after completion; and saved-scene card-driven wiring.

## Placeholder-grid focused Play Mode test

```powershell
$minimalGridPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-play-tests.log"'
)

$minimalGridPlayProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $minimalGridPlayArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($minimalGridPlayProcess.ExitCode -ne 0) {
    throw "Minimal-grid Play Mode test failed with exit code $($minimalGridPlayProcess.ExitCode)."
}
```

Expected result: all discovered cases pass after loading the saved scene with 19 visible tile renderers, visible placeholder obstacle renderers, one fixed camera, one visible primitive hero placeholder, selectable reachable-destination highlighting that excludes blockers, accepted one-click movement to a non-adjacent reachable destination by legal detour, two partial waypoint clicks that preserve the pending action and remaining allowance, explicit Stop, and cleared highlights only after completion.

## Play Mode shell test

```powershell
$playModeArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.TestShellPlayModeTests.TestRunner_CanEnterPlayModeAndAdvanceOneFrame',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\play-mode-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\play-mode-tests.log"'
)

$playModeProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $playModeArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($playModeProcess.ExitCode -ne 0) {
    throw "Play Mode tests failed with exit code $($playModeProcess.ExitCode)."
}
```

Expected result: the shell fixture discovers at least one test, all discovered cases pass after entering Play Mode and advancing one frame, and the process exits with code `0`.

## Complete gate suites

Run both unfiltered suites only for T3 validation: a final implementation milestone of a phase, an explicit hard gate, shared test/package/assembly infrastructure, the pre-build regression gate, or a change promoted because its blast radius cannot be enumerated. Resolve focused failures before spending time on these suites. These commands intentionally omit `-testFilter`.

```powershell
$completeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-edit-tests.log"'
)

$completeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $completeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($completeEditProcess.ExitCode -ne 0) {
    throw "Complete Edit Mode suite failed with exit code $($completeEditProcess.ExitCode)."
}

$completePlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-play-tests.log"'
)

$completePlayProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $completePlayArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($completePlayProcess.ExitCode -ne 0) {
    throw "Complete Play Mode suite failed with exit code $($completePlayProcess.ExitCode)."
}
```

Expected result at a T3 gate: every currently discovered Edit Mode and Play Mode test passes, each process exits with code `0`, and fresh complete NUnit XML files record the gate result with zero unexpected skipped or inconclusive tests.

## Test ownership

- `Assets/Tests/EditMode/` contains tests that run without entering Play Mode.
- `Assets/Tests/PlayMode/` contains tests that require Play Mode.
- Future milestones should add the narrowest relevant tests to these assemblies or create a purpose-specific test assembly when isolation requires it.
- T0 non-code validation is recorded in the milestone handoff rather than represented by an empty or unrelated Unity test run.
