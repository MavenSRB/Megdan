# Guards Digital - Automated Testing

Last updated: 2026-08-09

## Scope

The Unity Test Framework shell contains one trivial Edit Mode test and one minimal Play Mode smoke test. Milestone 1.1 adds focused Edit Mode coverage for the pure C# hex-coordinate foundation. Milestone 1.2 adds Edit Mode layout, mesh, and saved-scene coverage plus a Play Mode scene-loading check. Milestone 1.3 adds primitive hero occupancy and placement coverage. These tests do not implement or verify a game rule.

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

Expected result: two cases pass after loading the saved scene with seven visible tile renderers, one fixed camera, and one visible primitive hero placeholder.

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

## Test ownership

- `Assets/Tests/EditMode/` contains tests that run without entering Play Mode.
- `Assets/Tests/PlayMode/` contains tests that require Play Mode.
- Future milestones should add the narrowest relevant tests to these assemblies or create a purpose-specific test assembly when isolation requires it.
