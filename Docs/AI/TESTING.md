# Guards Digital - Verification and Testing

Last updated: 2026-08-09

## Scope

The Unity Test Framework shell contains one trivial Edit Mode test and one minimal Play Mode smoke test. Milestone 1.1 adds focused Edit Mode coverage for the pure C# hex-coordinate foundation. Milestone 1.2 adds Edit Mode layout, mesh, and saved-scene coverage plus a Play Mode scene-loading check. Milestone 1.3 adds primitive hero occupancy and placement coverage. Milestone 1.4 adds adjacent-empty destination query, selection highlighting, Input System pointer-path, saved-scene, and Play Mode coverage. Milestone 1.5 adds pure adjacent-move command/event authority coverage. Milestone 1.6 adds pointer-to-command hookup coverage for accepted and rejected adjacent destination flows. Milestone 1.7 expands the placeholder validation grid and adds scene coverage for radius-2 coordinates, distance coverage, alternate routes, and preserved click-to-move behavior. Milestone 1.8 adds pure movement-session coverage for zero movement, early stopping, exact allowance, and over-movement, plus controller and Play Mode pointer coverage for multi-step session behavior. These tests do not implement or verify an official game rule.

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

The current T3 phase/hard gates are milestones `1.13`, `2.6`, `3.11`, `4.4`, `5.8`, `6.8`, `7.9`, `8.16`, and `9.16`. Milestone `8.2` must update this list if it re-baselines the end of Phase 8.

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
    '-testFilter', 'GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
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

Expected result: all discovered cases pass, covering configurable allowance, zero movement, early stopping, exact allowance spending, attempted over-movement rejection, explicit route/event recording, refreshed next-step highlighting, and adjacent-move authority regressions.

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

Expected result: all discovered cases pass after loading the saved scene with 19 visible tile renderers, one fixed camera, one visible primitive hero placeholder, selectable adjacent-destination highlighting, accepted click-to-move presentation hookup, and click-the-hero early stopping for an active movement session.

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
