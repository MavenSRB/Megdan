# Foundation Focused Test Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

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
    '-testFilter', 'GuardsDigital.Tests.EditMode.AdjacentDestinationQueryTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests',
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.PlaceholderObstacleViewTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
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
