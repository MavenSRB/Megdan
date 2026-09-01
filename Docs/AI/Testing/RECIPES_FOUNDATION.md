# Foundation Focused Test Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md). The raw `Start-Process` blocks in this shard are test-filter and historical-output references only; write `tmp/unity-validation-request.json` and invoke the fixed launcher per [POLICY.md](POLICY.md). When affectedness selects multiple compatible fixtures, place their exact names in one ordered `filters` array and run one request; platform, batch/non-batch, and process-global boundaries remain separate.

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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-4-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-4-edit-tests.log"'
)

$heroSelectionEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
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
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe' `
    -ArgumentList $obstacleBlockingEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($obstacleBlockingEditProcess.ExitCode -ne 0) {
    throw "Obstacle-blocking Edit Mode tests failed with exit code $($obstacleBlockingEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering placeholder obstacle identity/occupancy, visible obstacle view placement, occupied-destination rejections, one-obstacle-per-space conflicts including units, reachable-destination exclusion for blockers, legal detours, insufficient allowance for detours, and saved-scene obstacle wiring.

## Architecture-conformance and Minion-Battle coverage tests

For milestone 6.1k, run `ILConformanceScannerTests`, `Phase4ArchitectureConformanceTests`, `ConfiguredMatchCutoverArchitectureTests`, `RuntimeShaderResolutionTests`, `ConfiguredSceneSerializationTests`, `DestinationHighlightMaterialOwnershipTests`, and `MinionBattleResolutionTests`; write outputs to `tmp/milestone-6-1k-focused-results.xml` and `tmp/milestone-6-1k-focused.log`. Milestone 6.1k is T3 because it changes shared test infrastructure and adds a Play Mode fixture, so also run both unfiltered suites and write `tmp/milestone-6-1k-complete-edit-results.xml`, `tmp/milestone-6-1k-complete-edit.log`, `tmp/milestone-6-1k-complete-play-results.xml`, and `tmp/milestone-6-1k-complete-play.log`.

Expected result: the architecture rules read compiled code rather than source text. `ILConformanceScannerTests` proves the instrument before anything depends on it - zero unresolved IL tokens across all four production assemblies, and a forbidden call found whether written directly, fully qualified, inside a lambda, or as a constructed generic. `Phase4ArchitectureConformanceTests` reports no violation of the nine boundaries and its self-check fires every rule against a deliberately violating sample type and stays silent on its compliant twin. `ConfiguredMatchCutoverArchitectureTests` derives the scene's script GUIDs from the asset database and reports no authored scene key without a matching serialized field. `RuntimeShaderResolutionTests` discovers each runtime shader name from the `ldstr` operand feeding `Shader.Find` and requires a throw before any `Material` is built from it. `MinionBattleResolutionTests` covers exact-count boundaries, Heavy-last ordering under multi-Heavy shapes the configured setup cannot produce, fixed-seed bounded property cases, and deterministic auto-resolution over a synthetic Battle Zone.

## Resolver-emitted timing checkpoint tests

For milestone 6.1ap, run `ResolverEmittedCheckpointConformanceTests` and `TurnTimingCheckpointOrderTests`; write outputs to `tmp/milestone-6-1ap-focused-results.xml` and `tmp/milestone-6-1ap-focused.log`. Both are Unity-free, so `dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --filter "FullyQualifiedName~ResolverEmittedCheckpointConformanceTests"` covers them on any OS. The milestone is T2, but it also runs `CardAggregateInvarianceTests` on the Windows tier, because that fixture holds nine of the fifteen pinned digests and the dotnet harness cannot reach it.

Expected result: the conformance fixture reports no turn-authority read of a produced card-action fact's kind, proves each of the three backends still references `ResolutionStep.AtCheckpoint` so the negative rule cannot pass by checkpoints having disappeared, and discriminates a deliberately violating sample from its compliant twin. The order fixture pins four sequences - Movement and Fast Travel as `TurnStarted -> ActionStarted -> ActionEnded -> SourceCardStateChanged`, Attack with a Defense response adding `BeforeAttack`, `DefenseResponse`, `AttackResultComparison`, and `AfterAttack`, and Attack against a minion omitting the two Defense-dependent ones - and asserts action end reaches the Active-effect lifecycle before source-card cancellation. Each scenario runs on a session carrying a live Active effect; `ObservingSession_ActuallyCarriesALiveActiveEffect` fails if that ever stops being true, because the ordinary configuration creates no Active effect and the whole fixture would otherwise pass on empty sequences. Measured sequences are appended to `tmp/milestone-6-1ap-measured-checkpoints.txt` for diffing.

**Re-proving a conformance rule is not optional when you change one.** These rules are only worth their cost while they still fail on a real regression. A change to any of them repeats the 6.1k protocol: apply a deliberate violation, observe the rule fail; revert, apply a pure rename of a private symbol, observe it pass. Both observations belong in that milestone's evidence record. A rule that has never been seen failing may be passing vacuously.
