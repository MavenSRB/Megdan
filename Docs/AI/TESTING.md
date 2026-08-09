# Guards Digital - Automated Testing

Last updated: 2026-08-09

## Scope

The Unity Test Framework shell contains one trivial Edit Mode test and one minimal Play Mode smoke test. Milestone 1.1 adds focused Edit Mode coverage for the pure C# hex-coordinate foundation. Milestone 1.2 adds Edit Mode layout, mesh, and saved-scene coverage plus a Play Mode scene-loading check. Milestone 1.3 adds primitive hero occupancy and placement coverage. Milestone 1.4 adds adjacent-empty destination query, selection highlighting, Input System pointer-path, saved-scene, and Play Mode coverage. Milestone 1.5 adds pure adjacent-move command/event authority coverage. These tests do not implement or verify an official game rule.

Run commands from PowerShell. Each focused command opens the project, runs only its named test fixture or case, writes NUnit XML plus a Unity log under `tmp/`, and exits without producing a player build or requiring a project scene.

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

Expected result: one test passes and the process exits with code `0`.

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

Expected result: eight cases pass, covering all six directions, the complete unique neighbor set, and invalid-direction rejection.

## Minimal-grid focused Edit Mode tests

```powershell
$minimalGridEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.HexWorldLayoutTests;GuardsDigital.Tests.EditMode.FlatHexMeshBuilderTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-edit-tests.log"'
)

$minimalGridEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $minimalGridEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($minimalGridEditProcess.ExitCode -ne 0) {
    throw "Minimal-grid Edit Mode tests failed with exit code $($minimalGridEditProcess.ExitCode)."
}
```

Expected result: twelve cases pass, covering coordinate conversion, edge-to-edge center spacing, mesh geometry, saved-scene placement, uniform light-gray fills, darker closed tile edges, and the fixed top-down camera.

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

Expected result: ten cases pass, covering primitive hero ID validation, authoritative occupied coordinate storage, view placement from occupancy, placeholder mesh creation, and saved-scene placement.

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

Expected result: thirteen cases pass, covering adjacent-empty destination filtering, presentation highlighting, Input System pointer-selection components, and saved-scene wiring.

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

## Minimal-grid focused Play Mode test

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

Expected result: three cases pass after loading the saved scene with seven visible tile renderers, one fixed camera, one visible primitive hero placeholder, and selectable adjacent-destination highlighting.

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

Expected result: one test passes after entering Play Mode and advancing one frame; the process exits with code `0`.

## Complete milestone-boundary suites

Run both unfiltered suites after the focused checks pass and before marking a roadmap milestone complete. These commands intentionally omit `-testFilter`.

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

Expected result: every currently discovered Edit Mode and Play Mode test passes, each process exits with code `0`, and the two complete NUnit XML files record the milestone-boundary result.

## Test ownership

- `Assets/Tests/EditMode/` contains tests that run without entering Play Mode.
- `Assets/Tests/PlayMode/` contains tests that require Play Mode.
- Future milestones should add the narrowest relevant tests to these assemblies or create a purpose-specific test assembly when isolation requires it.
